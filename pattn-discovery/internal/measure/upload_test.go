package measure

import (
	"context"
	"crypto/x509"
	"io"
	"net"
	"net/http"
	"net/http/httptest"
	"net/netip"
	"testing"
	"time"
)

func TestProbeUploadReportsBodyConsumptionAndResponse(t *testing.T) {
	var received int
	server := httptest.NewTLSServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		body, err := io.ReadAll(r.Body)
		if err != nil {
			t.Fatal(err)
		}
		received = len(body)
		w.WriteHeader(http.StatusNoContent)
	}))
	defer server.Close()

	// The production helper requires HTTPS but blocks private destinations. Tests opt into
	// loopback explicitly so the network behavior remains deterministic and self-contained.
	roots := x509.NewCertPool()
	roots.AddCert(server.Certificate())
	result, err := ProbeUpload(context.Background(), UploadProbeOptions{
		URL: server.URL,
		TotalBytes: 16 * 1024,
		Chunks: 8,
		Timeout: 5 * time.Second,
		AllowPrivate: true,
		RootCAs: roots,
	})
	if err != nil {
		t.Fatal(err)
	}
	if result.Error != "" {
		t.Fatalf("probe error: %s", result.Error)
	}
	if !result.ResponseReceived || !result.BodyFullyRead {
		t.Fatalf("unexpected result: %+v", result)
	}
	if received != 16*1024 {
		t.Fatalf("received %d bytes", received)
	}
}

func TestProbeUploadRejectsHTTPAndOversizedPayload(t *testing.T) {
	if _, err := ProbeUpload(context.Background(), UploadProbeOptions{
		URL: "http://example.com/upload",
		TotalBytes: 1024,
		Chunks: 8,
		Timeout: time.Second,
	}); err == nil {
		t.Fatal("expected http URL rejection")
	}
	if _, err := ProbeUpload(context.Background(), UploadProbeOptions{
		URL: "https://example.com/upload",
		TotalBytes: MaxUploadProbeBytes + 1,
		Chunks: 8,
		Timeout: time.Second,
	}); err == nil {
		t.Fatal("expected oversized payload rejection")
	}
}


func TestPublicProbeAddressRejectsSpecialUseRanges(t *testing.T) {
	rejected := []string{
		"0.1.2.3",
		"10.0.0.1",
		"100.64.0.1",
		"127.0.0.1",
		"169.254.1.1",
		"192.0.2.1",
		"198.18.0.1",
		"198.51.100.1",
		"203.0.113.1",
		"240.0.0.1",
		"::1",
		"fc00::1",
		"fe80::1",
		"100::1",
		"2001:db8::1",
	}
	for _, raw := range rejected {
		if isPublicProbeAddress(netip.MustParseAddr(raw)) {
			t.Fatalf("expected special-use address to be rejected: %s", raw)
		}
	}

	accepted := []string{"1.1.1.1", "8.8.8.8", "2606:4700:4700::1111"}
	for _, raw := range accepted {
		if !isPublicProbeAddress(netip.MustParseAddr(raw)) {
			t.Fatalf("expected public address to be accepted: %s", raw)
		}
	}
}

// TestProbeUploadDoesNotResolveIPLiteral hosts the fix for finding F-07: an
// IP-literal probe target used to go through net.DefaultResolver twice, so the
// probe leaked its destination to the system resolver and failed completely on
// the exact link it is meant to measure (DNS down).
func TestProbeUploadDoesNotResolveIPLiteral(t *testing.T) {
	var queries int
	restore := useStubResolver(t, func(ctx context.Context, network, host string) ([]netip.Addr, error) {
		queries++
		return nil, &net.DNSError{Err: "stub resolver must not be used", Name: host}
	})
	defer restore()

	server := httptest.NewTLSServer(http.HandlerFunc(func(w http.ResponseWriter, _ *http.Request) {
		w.WriteHeader(http.StatusNoContent)
	}))
	defer server.Close()

	// server.URL is https://127.0.0.1:port -- a literal address.
	private, err := ProbeUpload(context.Background(), UploadProbeOptions{
		URL:        server.URL,
		TotalBytes: 4096,
		Chunks:     4,
		Timeout:    3 * time.Second,
	})
	if err != nil {
		t.Fatal(err)
	}
	if queries != 0 {
		t.Fatalf("resolver was queried %d times for an IP-literal host", queries)
	}
	if private.Error == "" || private.Connected {
		t.Fatalf("non-public literal must be refused without dialing, got %+v", private)
	}
}

func TestProbeUploadQueriesResolverOnceForNamedHost(t *testing.T) {
	var hosts []string
	restore := useStubResolver(t, func(ctx context.Context, network, host string) ([]netip.Addr, error) {
		hosts = append(hosts, host)
		return []netip.Addr{netip.MustParseAddr("127.0.0.1")}, nil
	})
	defer restore()

	server := httptest.NewTLSServer(http.HandlerFunc(func(w http.ResponseWriter, _ *http.Request) {}))
	defer server.Close()

	if _, err := ProbeUpload(context.Background(), UploadProbeOptions{
		URL:          "https://uplink.invalid/probe",
		TotalBytes:   4096,
		Chunks:       4,
		Timeout:      3 * time.Second,
		AllowPrivate: true,
	}); err != nil {
		t.Fatal(err)
	}
	if len(hosts) != 1 || hosts[0] != "uplink.invalid" {
		t.Fatalf("unexpected resolver queries: %v", hosts)
	}
}

func useStubResolver(t *testing.T, lookup func(context.Context, string, string) ([]netip.Addr, error)) func() {
	t.Helper()
	previous := probeResolver
	probeResolver = stubResolver{lookup: lookup}
	return func() { probeResolver = previous }
}

type stubResolver struct {
	lookup func(context.Context, string, string) ([]netip.Addr, error)
}

// LookupNetIP matches net.Resolver's three-argument signature exactly; anything wider and the stub
// stops satisfying netResolver.
func (r stubResolver) LookupNetIP(ctx context.Context, network, host string) ([]netip.Addr, error) {
	return r.lookup(ctx, network, host)
}
