package main

import "testing"

func TestAdd(t *testing.T) {
	cases := []struct {
		a, b, want int
	}{
		{2, 3, 5},
		{-1, 1, 0},
		{0, 0, 0},
		{-4, -6, -10},
	}
	for _, c := range cases {
		if got := Add(c.a, c.b); got != c.want {
			t.Errorf("Add(%d, %d) = %d; want %d", c.a, c.b, got, c.want)
		}
	}
}

func TestSubtract(t *testing.T) {
	cases := []struct {
		a, b, want int
	}{
		{5, 3, 2},
		{3, 5, -2},
		{0, 0, 0},
		{-4, -6, 2},
	}
	for _, c := range cases {
		if got := Subtract(c.a, c.b); got != c.want {
			t.Errorf("Subtract(%d, %d) = %d; want %d", c.a, c.b, got, c.want)
		}
	}
}
