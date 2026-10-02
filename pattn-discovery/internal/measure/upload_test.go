package measure

import (
	"context"
	"crypto/x509"
	"io"
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
