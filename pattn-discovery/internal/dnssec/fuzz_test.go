package dnssec

import (
	"encoding/base64"
	"encoding/binary"
	"encoding/hex"
	"testing"

	"pattn-discovery/internal/dnswire"
)

func FuzzParseDNSSECRData(f *testing.F) {
	f.Add(uint16(dnswire.TypeDS), []byte{0x4f, 0x66, 0x08, 0x02})
	f.Add(uint16(dnswire.TypeDNSKEY), []byte{0x01, 0x01, 0x03, 0x08})
	f.Add(uint16(dnswire.TypeRRSIG), make([]byte, 19))
	f.Add(uint16(dnswire.TypeNSEC), []byte{0})
	f.Add(uint16(dnswire.TypeNSEC3), []byte{1, 0, 0, 0, 0})

	f.Fuzz(func(t *testing.T, rrType uint16, raw []byte) {
		if len(raw) > 128*1024 {
			t.Skip()
		}

		record := dnswire.ResourceRecord{
			Name:    "example.com",
			Type:    rrType,
			Class:   dnswire.ClassIN,
			RawData: append([]byte(nil), raw...),
		}

		switch rrType {
		case dnswire.TypeDS:
			value, err := ParseDS(record)
			if err == nil {
				if len(raw) < 4 {
					t.Fatal("DS parser accepted undersized RDATA")
				}
				if value.KeyTag != binary.BigEndian.Uint16(raw[:2]) ||
					value.Algorithm != raw[2] ||
					value.DigestType != raw[3] ||
					value.Digest != hex.EncodeToString(raw[4:]) {
					t.Fatalf("DS parser did not preserve wire fields: %#v", value)
				}
			}
		case dnswire.TypeDNSKEY:
			value, err := ParseDNSKEY(record)
			if err == nil {
				if len(raw) < 4 {
					t.Fatal("DNSKEY parser accepted undersized RDATA")
				}
				if value.Flags != binary.BigEndian.Uint16(raw[:2]) ||
					value.Protocol != raw[2] ||
					value.Algorithm != raw[3] ||
					value.PublicKey != base64.StdEncoding.EncodeToString(raw[4:]) ||
					value.KeyTag != keyTag(raw) {
					t.Fatalf("DNSKEY parser did not preserve wire fields: %#v", value)
				}
			}
		case dnswire.TypeRRSIG:
			_, _ = ParseRRSIG(record)
		case dnswire.TypeNSEC:
			_, _ = ParseNSEC(record)
		case dnswire.TypeNSEC3:
			_, _ = ParseNSEC3(record)
		default:
			_, _ = ParseDS(record)
			_, _ = ParseDNSKEY(record)
			_, _ = ParseRRSIG(record)
			_, _ = ParseNSEC(record)
			_, _ = ParseNSEC3(record)
		}
	})
}
