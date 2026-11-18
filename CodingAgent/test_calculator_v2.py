# test_calculator_v2.py - VERSION 2
import unittest

from calculator_v2 import Calculator, Calculation
from cli_v2 import parse_expression, parse_number


class TestCalculator(unittest.TestCase):
    def setUp(self):
        self.calc = Calculator()

    def test_add(self):
        self.assertEqual(self.calc.add(2, 3), 5)

    def test_subtract(self):
        self.assertEqual(self.calc.subtract(2, 5), -3)

    def test_multiply(self):
        self.assertEqual(self.calc.multiply(4, -3), -12)

    def test_divide_returns_float(self):
        self.assertEqual(self.calc.divide(10, 4), 2.5)

    def test_power(self):
        self.assertEqual(self.calc.power(2, 8), 256)

    def test_modulus(self):
        self.assertEqual(self.calc.modulus(10, 3), 1)

    def test_divide_by_zero_raises(self):
        with self.assertRaises(ZeroDivisionError):
            self.calc.divide(1, 0)

    def test_modulus_by_zero_raises(self):
        with self.assertRaises(ZeroDivisionError):
            self.calc.modulus(1, 0)

    def test_zero_to_negative_power_raises(self):
        with self.assertRaises(ZeroDivisionError):
            self.calc.power(0, -1)

    def test_unknown_operator_raises(self):
        with self.assertRaises(ValueError):
            self.calc.calculate(1, "?", 2)

    def test_history_records_successful_calculations(self):
        self.calc.add(1, 2)
        self.calc.multiply(3, 4)
        self.assertEqual(
            self.calc.history,
            [Calculation(1, "+", 2, 3), Calculation(3, "*", 4, 12)],
        )

    def test_failed_calculation_not_recorded(self):
        with self.assertRaises(ZeroDivisionError):
            self.calc.divide(1, 0)
        self.assertEqual(self.calc.history, [])

    def test_history_returns_copy(self):
        self.calc.add(1, 1)
        self.calc.history.clear()
        self.assertEqual(len(self.calc.history), 1)

    def test_clear_history(self):
        self.calc.add(1, 1)
        self.calc.clear_history()
        self.assertEqual(self.calc.history, [])


class TestParsing(unittest.TestCase):
    def test_integer_stays_int(self):
        self.assertIsInstance(parse_number("5"), int)
        self.assertEqual(parse_number("-12"), -12)

    def test_decimal_becomes_float(self):
        self.assertIsInstance(parse_number("3.5"), float)

    def test_parse_simple_expression(self):
        self.assertEqual(parse_expression("5 + 3"), (5, "+", 3))

    def test_parse_negative_left_operand(self):
        self.assertEqual(parse_expression("-5 + 3"), (-5, "+", 3))

    def test_parse_negative_right_operand(self):
        self.assertEqual(parse_expression("5 - -3"), (5, "-", -3))

    def test_wrong_token_count_raises(self):
        with self.assertRaises(ValueError):
            parse_expression("5 +")

    def test_non_numeric_raises(self):
        with self.assertRaises(ValueError):
            parse_expression("a + 3")

    def test_unknown_operator_raises(self):
        with self.assertRaises(ValueError):
            parse_expression("5 ? 3")


if __name__ == "__main__":
    unittest.main()
