package measure

import (
	"context"
	"crypto/tls"
	"crypto/x509"
	"errors"
	"fmt"
	"io"
	"net"
	"net/http"
	"net/http/httptrace"
	"net/netip"
	"net/url"
	"strconv"
	"strings"
	"sync/atomic"
	"time"
)

const (
	MaxUploadProbeBytes  = 1 << 20
	MaxUploadProbeChunks = 64
)

var nonPublicProbePrefixes = []netip.Prefix{
	netip.MustParsePrefix("0.0.0.0/8"),
	netip.MustParsePrefix("100.64.0.0/10"),
	netip.MustParsePrefix("192.0.0.0/24"),
	netip.MustParsePrefix("192.0.2.0/24"),
	netip.MustParsePrefix("192.88.99.0/24"),
	netip.MustParsePrefix("198.18.0.0/15"),
	netip.MustParsePrefix("198.51.100.0/24"),
	netip.MustParsePrefix("203.0.113.0/24"),
	netip.MustParsePrefix("240.0.0.0/4"),
	netip.MustParsePrefix("100::/64"),
	netip.MustParsePrefix("64:ff9b:1::/48"),
	netip.MustParsePrefix("2001:2::/48"),
	netip.MustParsePrefix("2001:db8::/32"),
	netip.MustParsePrefix("2001:10::/28"),
	netip.MustParsePrefix("2001:20::/28"),
}

// probeResolver is the only DNS path the probe uses. Production keeps
// net.DefaultResolver; tests substitute a stub to prove that an IP-literal
// destination is dialed without any resolver round-trip (finding F-07).
var probeResolver netResolver = net.DefaultResolver

// netResolver is the resolution surface the probe needs (net.Resolver satisfies it).
type netResolver interface {
	LookupNetIP(ctx context.Context, network, host string, opts *net.LookupOptions) ([]netip.Addr, error)
}

type UploadProbeOptions struct {
	URL             string
	TotalBytes      int
	Chunks          int
	Timeout         time.Duration
	InterChunkDelay time.Duration
	AllowPrivate    bool
	RootCAs         *x509.CertPool
	// UseSystemProxy opts into HTTP(S)_PROXY handling. It stays false by default:
	// an uplink-stall measurement taken through a corporate proxy measures the
	// proxy, not the path, and would be recorded as link evidence.
	UseSystemProxy bool
}

type UploadProbeResult struct {
	Connected          bool    `json:"connected"`
	TLSHandshakeSucceeded bool `json:"tlsHandshakeSucceeded"`
	BytesPlanned       int     `json:"bytesPlanned"`
	BytesReadByClient  int64   `json:"bytesReadByClient"`
	ChunksEmitted      int64   `json:"chunksEmitted"`
	BodyFullyRead      bool    `json:"bodyFullyRead"`
	ResponseReceived   bool    `json:"responseReceived"`
	StatusCode         int     `json:"statusCode"`
	DurationMs         float64 `json:"durationMs"`
	Error              string  `json:"error,omitempty"`
}

func ProbeUpload(ctx context.Context, options UploadProbeOptions) (UploadProbeResult, error) {
	if options.TotalBytes <= 0 || options.TotalBytes > MaxUploadProbeBytes {
		return UploadProbeResult{}, fmt.Errorf("totalBytes must be between 1 and %d", MaxUploadProbeBytes)
	}
	if options.Chunks < 2 || options.Chunks > MaxUploadProbeChunks {
		return UploadProbeResult{}, fmt.Errorf("chunks must be between 2 and %d", MaxUploadProbeChunks)
	}
	if options.Timeout <= 0 || options.Timeout > 60*time.Second {
		return UploadProbeResult{}, errors.New("timeout must be between 1ns and 60s")
	}
	if options.InterChunkDelay < 0 || options.InterChunkDelay > time.Second {
		return UploadProbeResult{}, errors.New("interChunkDelay must be between 0 and 1s")
	}

	parsed, err := url.Parse(strings.TrimSpace(options.URL))
	if err != nil || parsed.Hostname() == "" {
		return UploadProbeResult{}, errors.New("a valid upload probe URL is required")
	}
	if !strings.EqualFold(parsed.Scheme, "https") {
		return UploadProbeResult{}, errors.New("upload probe URL must use https")
	}
	if parsed.User != nil {
		return UploadProbeResult{}, errors.New("upload probe URL must not contain userinfo")
	}

	probeCtx, cancel := context.WithTimeout(ctx, options.Timeout)
	defer cancel()

	body := &pacedUploadBody{
		ctx: probeCtx,
		total: options.TotalBytes,
		chunks: options.Chunks,
		delay: options.InterChunkDelay,
	}
	request, err := http.NewRequestWithContext(probeCtx, http.MethodPost, parsed.String(), body)
	if err != nil {
		return UploadProbeResult{}, err
	}
	var connected atomic.Bool
	var tlsSucceeded atomic.Bool
	trace := &httptrace.ClientTrace{
		GotConn: func(httptrace.GotConnInfo) {
			connected.Store(true)
		},
		TLSHandshakeDone: func(_ tls.ConnectionState, err error) {
			tlsSucceeded.Store(err == nil)
		},
	}
	request = request.WithContext(httptrace.WithClientTrace(request.Context(), trace))
	request.ContentLength = int64(options.TotalBytes)
	request.Header.Set("Content-Type", "application/octet-stream")
	request.Header.Set("User-Agent", "pattn-discovery/uplink-probe")

	dialer := &net.Dialer{Timeout: options.Timeout}
	// The dial target may be a pinned address, so pin the certificate name to the
	// URL host instead of to the socket address (a bare IP would otherwise be
	// compared against a DNS-name certificate and fail).
	tlsConfig := &tls.Config{
		MinVersion: tls.VersionTLS12,
		RootCAs:    options.RootCAs,
		// With a custom DialContext, http.Transport ignores ForceAttemptHTTP2 and
		// negotiates HTTP/2 only through ALPN.
		NextProtos: []string{"h2", "http/1.1"},
	}
	if host := parsed.Hostname(); netip.ParseAddr(host) == (netip.Addr{}) {
		tlsConfig.ServerName = host
	}
	transport := &http.Transport{
		TLSClientConfig: tlsConfig,
		DialContext: func(ctx context.Context, network, address string) (net.Conn, error) {
			host, port, err := net.SplitHostPort(address)
			if err != nil {
				return nil, err
			}
			if literal, perr := netip.ParseAddr(strings.TrimSuffix(strings.TrimPrefix(host, "["), "]")); perr == nil {
				// The destination is already an address: dialing it must not cost a
				// resolver query (which would leak the probe target to the system
				// resolver, and fail outright where DNS is the thing being measured).
				if !isPublicProbeAddress(literal.Unmap()) && !options.AllowPrivate {
					return nil, errors.New("upload probe destination is not a public address")
				}
				return dialer.DialContext(ctx, network, address)
			}
			if options.AllowPrivate {
				return dialer.DialContext(ctx, network, address)
			}
			addrs, err := probeResolver.LookupNetIP(ctx, "ip", host)
			if err != nil {
				return nil, err
			}
			for _, resolved := range addrs {
				addr, ok := netip.AddrFromSlice(resolved.AsSlice())
				if !ok {
					continue
				}
				addr = addr.Unmap()
				if !isPublicProbeAddress(addr) {
					continue
				}
				return dialer.DialContext(ctx, network, net.JoinHostPort(addr.String(), port))
			}
			return nil, errors.New("upload probe destination resolved only to private/special addresses")
		},
	}
	if options.UseSystemProxy {
		// Proxy is applied by the transport before DialContext, so the pinned-address
		// path above is bypassed on purpose: an operator who opts into the system
		// proxy accepts that the proxy resolves the name.
		transport.Proxy = http.ProxyFromEnvironment
	}
	defer transport.CloseIdleConnections()

	client := &http.Client{
		Transport: transport,
		Timeout: options.Timeout,
		CheckRedirect: func(_ *http.Request, _ []*http.Request) error {
			return errors.New("upload probe redirects are disabled")
		},
	}

	start := time.Now()
	response, requestErr := client.Do(request)
	result := UploadProbeResult{
		Connected:         connected.Load(),
		TLSHandshakeSucceeded: tlsSucceeded.Load(),
		BytesPlanned:      options.TotalBytes,
		BytesReadByClient: body.bytesRead.Load(),
		ChunksEmitted:     body.chunksEmitted.Load(),
		BodyFullyRead:     body.bytesRead.Load() == int64(options.TotalBytes),
		DurationMs:        float64(time.Since(start)) / float64(time.Millisecond),
	}
	if requestErr != nil {
		result.Error = requestErr.Error()
		return result, nil
	}
	defer response.Body.Close()
	_, _ = io.Copy(io.Discard, io.LimitReader(response.Body, 64<<10))
	result.Connected = connected.Load()
	result.TLSHandshakeSucceeded = tlsSucceeded.Load()
	result.ResponseReceived = true
	result.StatusCode = response.StatusCode
	result.BytesReadByClient = body.bytesRead.Load()
	result.ChunksEmitted = body.chunksEmitted.Load()
	result.BodyFullyRead = result.BytesReadByClient == int64(options.TotalBytes)
	return result, nil
}

type pacedUploadBody struct {
	ctx           context.Context
	total         int
	chunks        int
	delay         time.Duration
	offset        int
	chunkIndex    int
	bytesRead     atomic.Int64
	chunksEmitted atomic.Int64
}

func (b *pacedUploadBody) Read(p []byte) (int, error) {
	if b.offset >= b.total {
		return 0, io.EOF
	}
	if b.chunkIndex > 0 && b.delay > 0 {
		timer := time.NewTimer(b.delay)
		defer timer.Stop()
		select {
		case <-b.ctx.Done():
			return 0, b.ctx.Err()
		case <-timer.C:
		}
	}

	base := b.total / b.chunks
	extra := b.total % b.chunks
	next := base
	if b.chunkIndex < extra {
		next++
	}
	if next <= 0 {
		next = 1
	}
	remaining := b.total - b.offset
	if next > remaining {
		next = remaining
	}
	if next > len(p) {
		next = len(p)
	}
	for i := 0; i < next; i++ {
		p[i] = byte((b.offset + i) % 251)
	}
	b.offset += next
	if next > 0 {
		b.bytesRead.Add(int64(next))
		b.chunksEmitted.Add(1)
		b.chunkIndex++
	}
	return next, nil
}

func (b *pacedUploadBody) Close() error { return nil }

func isPublicProbeAddress(addr netip.Addr) bool {
	if !addr.IsValid()
		|| !addr.IsGlobalUnicast()
		|| addr.IsUnspecified()
		|| addr.IsLoopback()
		|| addr.IsPrivate()
		|| addr.IsLinkLocalUnicast()
		|| addr.IsLinkLocalMulticast()
		|| addr.IsMulticast() {
		return false
	}
	for _, prefix := range nonPublicProbePrefixes {
		if prefix.Contains(addr) {
			return false
		}
	}
	return true
}

func ParseUploadProbeTimeout(milliseconds int) (time.Duration, error) {
	if milliseconds < 1 || milliseconds > 60_000 {
		return 0, errors.New("timeoutMs must be between 1 and 60000")
	}
	return time.Duration(milliseconds) * time.Millisecond, nil
}

func ParseUploadProbeDelay(milliseconds int) (time.Duration, error) {
	if milliseconds < 0 || milliseconds > 1000 {
		return 0, errors.New("interChunkDelayMs must be between 0 and 1000")
	}
	return time.Duration(milliseconds) * time.Millisecond, nil
}

func DefaultUploadProbeURL(host string, port int) string {
	if port <= 0 {
		port = 443
	}
	return "https://" + net.JoinHostPort(host, strconv.Itoa(port)) + "/"
}
