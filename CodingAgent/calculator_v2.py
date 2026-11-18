# calculator_v2.py - VERSION 2
"""Core Calculator class. Contains no input/output; the UI lives in cli_v2.py."""

import operator
from typing import Callable, Dict, List, NamedTuple, Union

Number = Union[int, float]


class Calculation(NamedTuple):
    """One recorded calculation."""
    a: Number
    op: str
    b: Number
    result: Number

    def __str__(self) -> str:
        return f"{self.a} {self.op} {self.b} = {self.result}"


class Calculator:
    """Performs arithmetic and keeps a history of successful calculations."""

    OPERATIONS: Dict[str, Callable[[Number, Number], Number]] = {
        "+": operator.add,
        "-": operator.sub,
        "*": operator.mul,
        "/": operator.truediv,
        "^": operator.pow,
        "%": operator.mod,
    }

    def __init__(self) -> None:
        self._history: List[Calculation] = []

    def calculate(self, a: Number, op: str, b: Number) -> Number:
        """Apply `op` to a and b, record the calculation, and return the result.

        Raises:
            ValueError: if `op` is not a supported operator.
            ZeroDivisionError: for division or modulus by zero.
        """
        if op not in self.OPERATIONS:
            raise ValueError(
                f"Unknown operator '{op}'. Use one of: {', '.join(self.OPERATIONS)}"
            )
        if op in ("/", "%") and b == 0:
            raise ZeroDivisionError("Cannot divide or take modulus by zero.")

        result = self.OPERATIONS[op](a, b)  # 0 ** negative raises ZeroDivisionError itself
        self._history.append(Calculation(a, op, b, result))
        return result

    def add(self, a: Number, b: Number) -> Number:
        return self.calculate(a, "+", b)

    def subtract(self, a: Number, b: Number) -> Number:
        return self.calculate(a, "-", b)

    def multiply(self, a: Number, b: Number) -> Number:
        return self.calculate(a, "*", b)

    def divide(self, a: Number, b: Number) -> Number:
        return self.calculate(a, "/", b)

    def power(self, a: Number, b: Number) -> Number:
        return self.calculate(a, "^", b)

    def modulus(self, a: Number, b: Number) -> Number:
        return self.calculate(a, "%", b)

    @property
    def history(self) -> List[Calculation]:
        """A copy of the calculation history, oldest first."""
        return list(self._history)

    def clear_history(self) -> None:
        self._history.clear()
