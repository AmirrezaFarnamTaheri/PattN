package measure

import (
	"context"
	"io"
	"net/http"
	"net/http/httptest"
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
	result, err := ProbeUpload(context.Background(), UploadProbeOptions{
		URL: server.URL,
		TotalBytes: 16 * 1024,
		Chunks: 8,
		Timeout: 5 * time.Second,
		AllowPrivate: true,
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
