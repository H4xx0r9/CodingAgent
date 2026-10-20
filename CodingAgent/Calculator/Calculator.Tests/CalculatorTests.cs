using Xunit;

namespace Calculator.Tests;

public class CalculatorTests
{
    private readonly Calculator _calc = new();

    [Theory]
    [InlineData(2, 3, 5)]
    [InlineData(0, 0, 0)]
    [InlineData(-2, 3, 1)]
    [InlineData(-5, -5, -10)]
    public void Add_ReturnsSum(int a, int b, int expected)
    {
        Assert.Equal(expected, _calc.Add(a, b));
    }

    [Theory]
    [InlineData(5, 3, 2)]
    [InlineData(0, 0, 0)]
    [InlineData(3, 5, -2)]
    [InlineData(-5, -5, 0)]
    public void Subtract_ReturnsDifference(int a, int b, int expected)
    {
        Assert.Equal(expected, _calc.Subtract(a, b));
    }

    [Fact]
    public void AddThenSubtract_SameValue_ReturnsOriginal()
    {
        int result = _calc.Subtract(_calc.Add(10, 7), 7);

        Assert.Equal(10, result);
    }
}
