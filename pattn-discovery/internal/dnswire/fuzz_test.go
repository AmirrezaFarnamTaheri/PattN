package dnswire

import (
	"encoding/binary"
	"encoding/hex"
	"testing"
)

func FuzzParseMessage(f *testing.F) {
	seed, err := hex.DecodeString("123484000001000100000000076578616d706c6503636f6d0000010001c00c000100010000012c00045db8d822")
	if err != nil {
		f.Fatal(err)
	}
	f.Add(seed)
	f.Add([]byte{})
	f.Add([]byte{0, 1, 0x80})

	f.Fuzz(func(t *testing.T, packet []byte) {
		if len(packet) > 128*1024 {
			t.Skip()
		}
		var id uint16
		if len(packet) >= 2 {
			id = binary.BigEndian.Uint16(packet[:2])
		}
		message, err := ParseMessage(packet, id, "example.com", TypeA)
		if err == nil {
			if message.Header.ID != id {
				t.Fatalf("successful parse changed transaction ID: got %d want %d", message.Header.ID, id)
			}
			if len(message.Answers) != int(message.Header.ANCount) {
				t.Fatalf("answer count mismatch: parsed=%d header=%d", len(message.Answers), message.Header.ANCount)
			}
			if len(message.Authorities) != int(message.Header.NSCount) {
				t.Fatalf("authority count mismatch: parsed=%d header=%d", len(message.Authorities), message.Header.NSCount)
			}
			if len(message.Additionals) != int(message.Header.ARCount) {
				t.Fatalf("additional count mismatch: parsed=%d header=%d", len(message.Additionals), message.Header.ARCount)
			}
		}
	})
}

func FuzzReadName(f *testing.F) {
	f.Add([]byte{0}, uint16(0))
	f.Add([]byte{3, 'w', 'w', 'w', 0}, uint16(0))
	f.Add([]byte{0xc0, 0x00}, uint16(0))
	f.Add([]byte{0x3f}, uint16(0))

	f.Fuzz(func(t *testing.T, packet []byte, rawOffset uint16) {
		if len(packet) > 64*1024 {
			t.Skip()
		}
		if len(packet) == 0 {
			_, _, _ = readName(packet, 0)
			return
		}
		offset := int(rawOffset) % (len(packet) + 1)
		_, next, err := readName(packet, offset)
		if err == nil && (next <= offset || next > len(packet)) {
			t.Fatalf("successful name parse returned invalid next offset: start=%d next=%d len=%d", offset, next, len(packet))
		}
	})
}
