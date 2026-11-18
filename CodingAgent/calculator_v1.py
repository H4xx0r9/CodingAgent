# calculator_v1.py - VERSION 1


class Calculator:
    """A simple calculator class supporting basic arithmetic operations."""

    def __init__(self):
        self.history = []

    def _record(self, expression, result):
        self.history.append(f"{expression} = {result}")
        return result

    def add(self, a, b):
        return self._record(f"{a} + {b}", a + b)

    def subtract(self, a, b):
        return self._record(f"{a} - {b}", a - b)

    def multiply(self, a, b):
        return self._record(f"{a} * {b}", a * b)

    def divide(self, a, b):
        if b == 0:
            raise ZeroDivisionError("Cannot divide by zero.")
        return self._record(f"{a} / {b}", a / b)

    def power(self, a, b):
        return self._record(f"{a} ^ {b}", a ** b)

    def modulus(self, a, b):
        if b == 0:
            raise ZeroDivisionError("Cannot modulo by zero.")
        return self._record(f"{a} % {b}", a % b)

    def show_history(self):
        if not self.history:
            print("No history yet.")
        for entry in self.history:
            print(entry)

    def clear_history(self):
        self.history.clear()


def main():
    calc = Calculator()
    operations = {
        "+": calc.add,
        "-": calc.subtract,
        "*": calc.multiply,
        "/": calc.divide,
        "^": calc.power,
        "%": calc.modulus,
    }

    print("Simple Calculator v1")
    print("Enter: number operator number (e.g. 5 + 3)")
    print("Type 'history' to see past calculations, 'clear' to reset history, or 'quit' to exit.")

    while True:
        user_input = input("\n> ").strip()

        if user_input.lower() == "quit":
            print("Goodbye!")
            break
        if user_input.lower() == "history":
            calc.show_history()
            continue
        if user_input.lower() == "clear":
            calc.clear_history()
            print("History cleared.")
            continue

        parts = user_input.split()
        if len(parts) != 3:
            print("Invalid input. Use the format: number operator number")
            continue

        try:
            a, op, b = float(parts[0]), parts[1], float(parts[2])
        except ValueError:
            print("Invalid numbers. Please enter numeric values.")
            continue

        if op not in operations:
            print(f"Unknown operator '{op}'. Use one of: {', '.join(operations)}")
            continue

        try:
            result = operations[op](a, b)
            print(f"Result: {result}")
        except ZeroDivisionError as e:
            print(f"Error: {e}")


if __name__ == "__main__":
    main()
