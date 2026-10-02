package targets

import "testing"

func TestParseMappedIPv6PrefixUsesNativeIPv4Family(t *testing.T) {
	r, err := Parse("::ffff:192.0.2.0/120")
	if err != nil {
		t.Fatal(err)
	}
	if !r.Start.Is4() || !r.End.Is4() {
		t.Fatalf("range families: %v-%v", r.Start, r.End)
	}
	if r.Start.String() != "192.0.2.0" || r.End.String() != "192.0.2.255" || r.Count().String() != "256" {
		t.Fatalf("range=%s count=%s", r.String(), r.Count())
	}
}
