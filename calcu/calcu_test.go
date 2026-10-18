package calcu

import "testing"

func TestAdd(t *testing.T) {
	tests := []struct {
		a, b, want int
	}{
		{2, 3, 5},
		{0, 0, 0},
		{-1, 1, 0},
		{-5, -5, -10},
	}
	for _, tc := range tests {
		if got := Add(tc.a, tc.b); got != tc.want {
			t.Errorf("Add(%d, %d) = %d; want %d", tc.a, tc.b, got, tc.want)
		}
	}
}

func TestSub(t *testing.T) {
	tests := []struct {
		a, b, want int
	}{
		{5, 3, 2},
		{0, 0, 0},
		{3, 5, -2},
		{-5, -5, 0},
	}
	for _, tc := range tests {
		if got := Sub(tc.a, tc.b); got != tc.want {
			t.Errorf("Sub(%d, %d) = %d; want %d", tc.a, tc.b, got, tc.want)
		}
	}
}
