import unittest

from shopping_cart import ShoppingCart


class TestShoppingCart(unittest.TestCase):
    def setUp(self):
        self.cart = ShoppingCart()

    def test_new_cart_is_empty(self):
        self.assertTrue(self.cart.is_empty())
        self.assertEqual(self.cart.items(), {})
        self.assertEqual(self.cart.total(), 0)

    def test_add_single_fruit_defaults_to_one(self):
        self.cart.add("apple")
        self.assertEqual(self.cart.quantity("apple"), 1)

    def test_add_multiple_quantity(self):
        self.cart.add("banana", 4)
        self.assertEqual(self.cart.quantity("banana"), 4)

    def test_add_same_fruit_accumulates(self):
        self.cart.add("apple", 2)
        self.cart.add("apple", 3)
        self.assertEqual(self.cart.quantity("apple"), 5)

    def test_add_unknown_fruit_raises(self):
        with self.assertRaises(ValueError):
            self.cart.add("pineapple")

    def test_add_invalid_quantity_raises(self):
        for bad in (0, -1, 1.5, "2"):
            with self.subTest(quantity=bad):
                with self.assertRaises(ValueError):
                    self.cart.add("apple", bad)

    def test_remove_partial_quantity(self):
        self.cart.add("orange", 5)
        self.cart.remove("orange", 2)
        self.assertEqual(self.cart.quantity("orange"), 3)

    def test_remove_default_quantity_is_one(self):
        self.cart.add("mango", 2)
        self.cart.remove("mango")
        self.assertEqual(self.cart.quantity("mango"), 1)

    def test_remove_all_deletes_item(self):
        self.cart.add("grape", 2)
        self.cart.remove("grape", 2)
        self.assertEqual(self.cart.quantity("grape"), 0)
        self.assertNotIn("grape", self.cart.items())

    def test_remove_more_than_present_deletes_item(self):
        self.cart.add("grape", 2)
        self.cart.remove("grape", 10)
        self.assertNotIn("grape", self.cart.items())
        self.assertTrue(self.cart.is_empty())

    def test_remove_fruit_not_in_cart_raises(self):
        with self.assertRaises(KeyError):
            self.cart.remove("apple")

    def test_remove_invalid_quantity_raises(self):
        self.cart.add("apple", 2)
        for bad in (0, -3):
            with self.subTest(quantity=bad):
                with self.assertRaises(ValueError):
                    self.cart.remove("apple", bad)
        self.assertEqual(self.cart.quantity("apple"), 2)

    def test_total_calculation(self):
        self.cart.add("apple", 3)    # 3 * 0.50 = 1.50
        self.cart.add("banana", 2)   # 2 * 0.25 = 0.50
        self.cart.add("grape")       # 1 * 2.00 = 2.00
        self.assertEqual(self.cart.total(), 4.00)

    def test_total_updates_after_remove(self):
        self.cart.add("apple", 3)
        self.cart.remove("apple", 1)
        self.assertEqual(self.cart.total(), 1.00)

    def test_items_returns_copy(self):
        self.cart.add("apple", 1)
        snapshot = self.cart.items()
        snapshot["apple"] = 100
        self.assertEqual(self.cart.quantity("apple"), 1)

    def test_clear_empties_cart(self):
        self.cart.add("apple", 2)
        self.cart.add("banana", 1)
        self.cart.clear()
        self.assertTrue(self.cart.is_empty())
        self.assertEqual(self.cart.total(), 0)

    def test_custom_prices(self):
        cart = ShoppingCart(prices={"kiwi": 3.00})
        cart.add("kiwi", 2)
        self.assertEqual(cart.total(), 6.00)
        with self.assertRaises(ValueError):
            cart.add("apple")


if __name__ == "__main__":
    unittest.main()
