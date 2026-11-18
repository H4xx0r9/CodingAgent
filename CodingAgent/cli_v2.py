# cli_v2.py - VERSION 2
"""Command-line interface for the Calculator. All printing happens here."""

from calculator_v2 import Calculator, Number

OPERATORS = "+-*/^%"


def parse_number(text: str) -> Number:
    """Return an int for integer text, otherwise a float. Raises ValueError if invalid."""
    text = text.strip()
    try:
        return int(text)
    except ValueError:
        return float(text)


def parse_expression(text: str):
    """Split 'a op b' into (a, op, b). Tokens are whitespace-separated, so
    negative numbers such as '-5 + 3' or '5 - -3' work.

    Raises ValueError if the text is not in the expected format.
    """
    parts = text.split()
    if len(parts) != 3:
        raise ValueError("Use the format: number operator number (e.g. 5 + 3)")
    left, op, right = parts
    if op not in OPERATORS:
        raise ValueError(f"Unknown operator '{op}'. Use one of: {', '.join(OPERATORS)}")
    try:
        return parse_number(left), op, parse_number(right)
    except ValueError:
        raise ValueError("Numbers must be valid numeric values.") from None


def run() -> None:
    calc = Calculator()

    print("Simple Calculator v2")
    print("Enter: number operator number (e.g. 5 + 3)")
    print("Commands: 'history', 'clear', 'quit'")

    while True:
        user_input = input("\n> ").strip()
        command = user_input.lower()

        if command == "quit":
            print("Goodbye!")
            break
        if command == "history":
            if not calc.history:
                print("No history yet.")
            for entry in calc.history:
                print(entry)
            continue
        if command == "clear":
            calc.clear_history()
            print("History cleared.")
            continue

        try:
            a, op, b = parse_expression(user_input)
            print(f"Result: {calc.calculate(a, op, b)}")
        except ZeroDivisionError as e:
            print(f"Error: {e}")
        except ValueError as e:
            print(f"Error: {e}")


if __name__ == "__main__":
    run()
