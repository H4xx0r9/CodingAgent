"""VERSION 2 tests for shopping_cart_v2.

Changes from Version 1:
- Table-driven cases with subTest to reduce repetition.
- Rounding and large-cart total tests.
- Constructor price validation tests.
"""

import unittest
from decimal import Decimal

from shopping_cart_v2 import FRUIT_PRICES, ShoppingCart

# (fruit, quantity, expected_total) for single-fruit totals.
SINGLE_FRUIT_TOTALS = [
    ("apple", 3, Decimal("1.50")),
    ("banana", 2, Decimal("0.50")),
    ("orange", 4, Decimal("3.00")),
    ("grape", 1, Decimal("2.00")),
    ("mango", 10, Decimal("15.00")),
]


class TestAddRemove(unittest.TestCase):
    def setUp(self) -> None:
        self.cart = ShoppingCart()

    def test_new_cart_is_empty(self) -> None:
        self.assertTrue(self.cart.is_empty())
        self.assertEqual(self.cart.items(), {})
        self.assertEqual(self.cart.total(), Decimal("0.00"))

    def test_add_default_quantity_is_one(self) -> None:
        self.cart.add("apple")
        self.assertEqual(self.cart.quantity("apple"), 1)

    def test_add_same_fruit_accumulates(self) -> None:
        self.cart.add("apple", 2)
        self.cart.add("apple", 3)
        self.assertEqual(self.cart.quantity("apple"), 5)

    def test_add_unknown_fruit_raises(self) -> None:
        with self.assertRaises(ValueError):
            self.cart.add("pineapple")

    def test_add_invalid_quantity_raises(self) -> None:
        for bad in (0, -1, 1.5, "2", True):
            with self.subTest(quantity=bad):
                with self.assertRaises(ValueError):
                    self.cart.add("apple", bad)
        self.assertTrue(self.cart.is_empty())

    def test_remove_partial_quantity(self) -> None:
        self.cart.add("orange", 5)
        self.cart.remove("orange", 2)
        self.assertEqual(self.cart.quantity("orange"), 3)

    def test_remove_default_quantity_is_one(self) -> None:
        self.cart.add("mango", 2)
        self.cart.remove("mango")
        self.assertEqual(self.cart.quantity("mango"), 1)

    def test_remove_all_deletes_item(self) -> None:
        self.cart.add("grape", 2)
        self.cart.remove("grape", 2)
        self.assertNotIn("grape", self.cart.items())
        self.assertTrue(self.cart.is_empty())

    def test_remove_more_than_present_deletes_item(self) -> None:
        self.cart.add("grape", 2)
        self.cart.remove("grape", 10)
        self.assertTrue(self.cart.is_empty())

    def test_remove_fruit_not_in_cart_raises(self) -> None:
        with self.assertRaises(KeyError):
            self.cart.remove("apple")

    def test_remove_invalid_quantity_raises(self) -> None:
        self.cart.add("apple", 2)
        for bad in (0, -3, 0.5, None):
            with self.subTest(quantity=bad):
                with self.assertRaises(ValueError):
                    self.cart.remove("apple", bad)
        self.assertEqual(self.cart.quantity("apple"), 2)

    def test_items_returns_copy(self) -> None:
        self.cart.add("apple", 1)
        snapshot = self.cart.items()
        snapshot["apple"] = 100
        self.assertEqual(self.cart.quantity("apple"), 1)

    def test_clear_empties_cart(self) -> None:
        self.cart.add("apple", 2)
        self.cart.add("banana", 1)
        self.cart.clear()
        self.assertTrue(self.cart.is_empty())
        self.assertEqual(self.cart.total(), Decimal("0.00"))


class TestTotals(unittest.TestCase):
    def setUp(self) -> None:
        self.cart = ShoppingCart()

    def test_single_fruit_totals(self) -> None:
        for fruit, qty, expected in SINGLE_FRUIT_TOTALS:
            with self.subTest(fruit=fruit, quantity=qty):
                cart = ShoppingCart()
                cart.add(fruit, qty)
                self.assertEqual(cart.total(), expected)

    def test_mixed_total(self) -> None:
        self.cart.add("apple", 3)    # 1.50
        self.cart.add("banana", 2)   # 0.50
        self.cart.add("grape")       # 2.00
        self.assertEqual(self.cart.total(), Decimal("4.00"))

    def test_total_updates_after_remove(self) -> None:
        self.cart.add("apple", 3)
        self.cart.remove("apple", 1)
        self.assertEqual(self.cart.total(), Decimal("1.00"))

    def test_total_is_exact_for_repeated_decimal_prices(self) -> None:
        # With floats, 0.1 * 3 is not exactly 0.3. Decimal must give exactly 0.30.
        cart = ShoppingCart(prices={"kiwi": 0.1})
        cart.add("kiwi", 3)
        self.assertEqual(cart.total(), Decimal("0.30"))

    def test_total_rounds_half_up_to_cents(self) -> None:
        cart = ShoppingCart(prices={"x": "0.005"})
        cart.add("x", 1)
        self.assertEqual(cart.total(), Decimal("0.01"))

    def test_total_rounds_down_when_below_half_cent(self) -> None:
        cart = ShoppingCart(prices={"x": "0.004"})
        cart.add("x", 1)
        self.assertEqual(cart.total(), Decimal("0.00"))

    def test_total_rounding_across_multiple_units(self) -> None:
        # 0.333 * 3 = 0.999 -> 1.00
        cart = ShoppingCart(prices={"x": "0.333"})
        cart.add("x", 3)
        self.assertEqual(cart.total(), Decimal("1.00"))

    def test_large_cart_total(self) -> None:
        # 1000 apples + 500 mangoes = 500.00 + 750.00
        self.cart.add("apple", 1000)
        self.cart.add("mango", 500)
        self.assertEqual(self.cart.total(), Decimal("1250.00"))

    def test_many_small_adds_match_single_add(self) -> None:
        for _ in range(250):
            self.cart.add("banana")
        bulk = ShoppingCart()
        bulk.add("banana", 250)
        self.assertEqual(self.cart.total(), bulk.total())
        self.assertEqual(self.cart.total(), Decimal("62.50"))


class TestCustomPrices(unittest.TestCase):
    def test_custom_prices_replace_defaults(self) -> None:
        cart = ShoppingCart(prices={"kiwi": 3})
        cart.add("kiwi", 2)
        self.assertEqual(cart.total(), Decimal("6.00"))
        with self.assertRaises(ValueError):
            cart.add("apple")

    def test_default_prices_are_not_modified(self) -> None:
        ShoppingCart(prices={"apple": 9})
        self.assertEqual(FRUIT_PRICES["apple"], Decimal("0.50"))

    def test_invalid_price_types_raise_type_error(self) -> None:
        for bad in (None, [1], {"a": 1}, True):
            with self.subTest(price=bad):
                with self.assertRaises(TypeError):
                    ShoppingCart(prices={"kiwi": bad})

    def test_invalid_price_values_raise_value_error(self) -> None:
        for bad in ("abc", "-1", "NaN", "Infinity", -0.5, float("nan"), float("inf")):
            with self.subTest(price=bad):
                with self.assertRaises(ValueError):
                    ShoppingCart(prices={"kiwi": bad})

    def test_zero_price_is_allowed(self) -> None:
        cart = ShoppingCart(prices={"freebie": 0})
        cart.add("freebie", 5)
        self.assertEqual(cart.total(), Decimal("0.00"))

    def test_blank_fruit_name_raises(self) -> None:
        for bad in ("", "   "):
            with self.subTest(name=bad):
                with self.assertRaises(ValueError):
                    ShoppingCart(prices={bad: 1})


if __name__ == "__main__":
    unittest.main()
