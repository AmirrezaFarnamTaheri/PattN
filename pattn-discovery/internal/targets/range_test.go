package targets

import (
	"context"
	"net/netip"
	"reflect"
	"testing"
)

func TestParseAndCount(t *testing.T) {
	cases := []struct {
		input string
		start string
		end   string
		count string
	}{
		{"203.0.113.9", "203.0.113.9", "203.0.113.9", "1"},
		{"203.0.113.0/30", "203.0.113.0", "203.0.113.3", "4"},
		{"203.0.113.10-203.0.113.20", "203.0.113.10", "203.0.113.20", "11"},
		{"2001:db8::/126", "2001:db8::", "2001:db8::3", "4"},
		{"2001:db8::/64", "2001:db8::", "2001:db8::ffff:ffff:ffff:ffff", "18446744073709551616"},
	}
	for _, tc := range cases {
		r, err := Parse(tc.input)
		if err != nil {
			t.Fatalf("%s: %v", tc.input, err)
		}
		if r.Start.String() != tc.start || r.End.String() != tc.end || r.Count().String() != tc.count {
			t.Fatalf("%s => %s..%s count=%s", tc.input, r.Start, r.End, r.Count())
		}
	}
}

func TestParseMappedIPv6PrefixUsesNativeIPv4Family(t *testing.T) {
	r, err := Parse("::ffff:192.0.2.0/120")
	if err != nil {
		t.Fatal(err)
	}
	if !r.Start.Is4() || !r.End.Is4() {
		t.Fatalf("mapped prefix must normalize both bounds to native IPv4: %s..%s", r.Start, r.End)
	}
	if got, want := r.Start.String(), "192.0.2.0"; got != want {
		t.Fatalf("start=%s want=%s", got, want)
	}
	if got, want := r.End.String(), "192.0.2.255"; got != want {
		t.Fatalf("end=%s want=%s", got, want)
	}
	if got, want := r.Count().String(), "256"; got != want {
		t.Fatalf("count=%s want=%s", got, want)
	}

	var got []netip.Addr
	if err := Stream(context.Background(), []Range{r}, 2, func(addr netip.Addr) error {
		got = append(got, addr)
		return nil
	}); err != nil {
		t.Fatal(err)
	}
	want := []netip.Addr{netip.MustParseAddr("192.0.2.0"), netip.MustParseAddr("192.0.2.1")}
	if !reflect.DeepEqual(got, want) {
		t.Fatalf("stream=%v want=%v", got, want)
	}
}

func TestStreamIsLazyAndBounded(t *testing.T) {
	r, err := Parse("10.0.0.0/8")
	if err != nil {
		t.Fatal(err)
	}
	var got []netip.Addr
	if err := Stream(context.Background(), []Range{r}, 3, func(addr netip.Addr) error {
		got = append(got, addr)
		return nil
	}); err != nil {
		t.Fatal(err)
	}
	want := []netip.Addr{netip.MustParseAddr("10.0.0.0"), netip.MustParseAddr("10.0.0.1"), netip.MustParseAddr("10.0.0.2")}
	if !reflect.DeepEqual(got, want) {
		t.Fatalf("got %v want %v", got, want)
	}
}

func TestParseRejectsMixedOrReversedRange(t *testing.T) {
	for _, input := range []string{"203.0.113.2-203.0.113.1", "203.0.113.1-2001:db8::1", "nope"} {
		if _, err := Parse(input); err == nil {
			t.Fatalf("expected %q to fail", input)
		}
	}
}
