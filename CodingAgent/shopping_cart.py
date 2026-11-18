"""A simple shopping cart for fruit items."""

# Price per unit, in dollars
FRUIT_PRICES = {
    "apple": 0.50,
    "banana": 0.25,
    "orange": 0.75,
    "grape": 2.00,
    "mango": 1.50,
}


class ShoppingCart:
    def __init__(self, prices=None):
        self._prices = dict(prices) if prices is not None else dict(FRUIT_PRICES)
        self._quantities = {}

    def add(self, fruit, quantity=1):
        """Add `quantity` of `fruit` to the cart."""
        if fruit not in self._prices:
            raise ValueError(f"Unknown fruit: {fruit!r}")
        if not isinstance(quantity, int) or quantity <= 0:
            raise ValueError("Quantity must be a positive integer")
        self._quantities[fruit] = self._quantities.get(fruit, 0) + quantity

    def remove(self, fruit, quantity=1):
        """Remove `quantity` of `fruit` from the cart.

        If quantity is at least the amount in the cart, the item is removed entirely.
        """
        if fruit not in self._quantities:
            raise KeyError(f"{fruit!r} is not in the cart")
        if not isinstance(quantity, int) or quantity <= 0:
            raise ValueError("Quantity must be a positive integer")
        remaining = self._quantities[fruit] - quantity
        if remaining > 0:
            self._quantities[fruit] = remaining
        else:
            del self._quantities[fruit]

    def quantity(self, fruit):
        """Return how many of `fruit` are in the cart (0 if none)."""
        return self._quantities.get(fruit, 0)

    def items(self):
        """Return a dict of fruit -> quantity for everything in the cart."""
        return dict(self._quantities)

    def total(self):
        """Return the total price of the cart."""
        return round(sum(self._prices[f] * q for f, q in self._quantities.items()), 2)

    def is_empty(self):
        return not self._quantities

    def clear(self):
        self._quantities.clear()


if __name__ == "__main__":
    cart = ShoppingCart()
    cart.add("apple", 3)
    cart.add("banana", 2)
    cart.add("grape")
    print("Cart:", cart.items())
    print(f"Total: ${cart.total():.2f}")
    cart.remove("apple", 1)
    print("After removing 1 apple:", cart.items())
    print(f"Total: ${cart.total():.2f}")
