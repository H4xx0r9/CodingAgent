"""VERSION 2 - A simple shopping cart for fruit items.

Changes from Version 1:
- Type hints and full docstrings added.
- Custom prices are validated in the constructor (type, finite, non-negative).
- Prices are stored as Decimal instead of float, so money arithmetic is exact
  and totals are rounded to cents with ROUND_HALF_UP.
"""

from decimal import ROUND_HALF_UP, Decimal, InvalidOperation
from typing import Any, Dict, Mapping, Optional, Union

PriceInput = Union[int, float, str, Decimal]

CENT = Decimal("0.01")

# Default price per unit, in dollars.
FRUIT_PRICES: Dict[str, Decimal] = {
    "apple": Decimal("0.50"),
    "banana": Decimal("0.25"),
    "orange": Decimal("0.75"),
    "grape": Decimal("2.00"),
    "mango": Decimal("1.50"),
}


def _validate_price(fruit: Any, value: Any) -> Decimal:
    """Convert and validate a single price.

    Accepts int, float, str, or Decimal. Booleans are rejected even though
    they are a subclass of int. The price must be finite and non-negative.
    """
    if not isinstance(fruit, str) or not fruit.strip():
        raise ValueError(f"Fruit name must be a non-empty string, got {fruit!r}")
    if isinstance(value, bool) or not isinstance(value, (int, float, str, Decimal)):
        raise TypeError(f"Price for {fruit!r} must be a number, got {type(value).__name__}")
    try:
        # str() on a float gives the shortest exact repr, e.g. 0.1 -> "0.1".
        price = value if isinstance(value, Decimal) else Decimal(str(value))
    except InvalidOperation as exc:
        raise ValueError(f"Price for {fruit!r} is not a valid number: {value!r}") from exc
    if not price.is_finite():
        raise ValueError(f"Price for {fruit!r} must be finite, got {value!r}")
    if price < 0:
        raise ValueError(f"Price for {fruit!r} cannot be negative, got {value!r}")
    return price


def _validate_quantity(quantity: Any) -> None:
    """Raise ValueError unless quantity is a positive integer (not a bool)."""
    if isinstance(quantity, bool) or not isinstance(quantity, int) or quantity <= 0:
        raise ValueError(f"Quantity must be a positive integer, got {quantity!r}")


class ShoppingCart:
    """A shopping cart that holds quantities of fruit.

    Items are added and removed by name. Prices come from ``FRUIT_PRICES``
    unless a custom mapping is supplied to the constructor.

    Example:
        >>> cart = ShoppingCart()
        >>> cart.add("apple", 3)
        >>> cart.total()
        Decimal('1.50')
    """

    def __init__(self, prices: Optional[Mapping[str, PriceInput]] = None) -> None:
        """Create an empty cart.

        Args:
            prices: Optional mapping of fruit name to unit price. If omitted,
                ``FRUIT_PRICES`` is used. Every price is validated.

        Raises:
            TypeError: If a price is not a number.
            ValueError: If a fruit name is invalid or a price is negative,
                NaN, or infinite.
        """
        source = FRUIT_PRICES if prices is None else prices
        self._prices: Dict[str, Decimal] = {
            fruit: _validate_price(fruit, value) for fruit, value in source.items()
        }
        self._quantities: Dict[str, int] = {}

    def add(self, fruit: str, quantity: int = 1) -> None:
        """Add ``quantity`` units of ``fruit`` to the cart.

        Raises:
            ValueError: If the fruit is unknown or the quantity is not a
                positive integer.
        """
        if fruit not in self._prices:
            raise ValueError(f"Unknown fruit: {fruit!r}")
        _validate_quantity(quantity)
        self._quantities[fruit] = self._quantities.get(fruit, 0) + quantity

    def remove(self, fruit: str, quantity: int = 1) -> None:
        """Remove ``quantity`` units of ``fruit`` from the cart.

        If ``quantity`` is at least the amount in the cart, the item is removed
        entirely.

        Raises:
            KeyError: If the fruit is not in the cart.
            ValueError: If the quantity is not a positive integer.
        """
        if fruit not in self._quantities:
            raise KeyError(f"{fruit!r} is not in the cart")
        _validate_quantity(quantity)
        remaining = self._quantities[fruit] - quantity
        if remaining > 0:
            self._quantities[fruit] = remaining
        else:
            del self._quantities[fruit]

    def quantity(self, fruit: str) -> int:
        """Return how many units of ``fruit`` are in the cart (0 if none)."""
        return self._quantities.get(fruit, 0)

    def items(self) -> Dict[str, int]:
        """Return a copy of the cart as a mapping of fruit name to quantity."""
        return dict(self._quantities)

    def total(self) -> Decimal:
        """Return the total price, rounded to the nearest cent."""
        subtotal = sum(
            (self._prices[fruit] * qty for fruit, qty in self._quantities.items()),
            Decimal("0"),
        )
        return subtotal.quantize(CENT, rounding=ROUND_HALF_UP)

    def is_empty(self) -> bool:
        """Return True if the cart holds no items."""
        return not self._quantities

    def clear(self) -> None:
        """Remove all items from the cart."""
        self._quantities.clear()


if __name__ == "__main__":
    print("VERSION 2 demo")
    cart = ShoppingCart()
    cart.add("apple", 3)
    cart.add("banana", 2)
    cart.add("grape")
    print("Cart:", cart.items())
    print(f"Total: ${cart.total()}")
    cart.remove("apple", 1)
    print("After removing 1 apple:", cart.items())
    print(f"Total: ${cart.total()}")
