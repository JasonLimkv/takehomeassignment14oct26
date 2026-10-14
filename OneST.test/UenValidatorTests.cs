using OneST;
using Xunit;

namespace OneST.Tests;

public class UenValidatorTests
{
    [Theory]
    [InlineData("12345678A", true)]
    [InlineData("199012345X", true)]
    [InlineData("T09LL0001B", true)]
    [InlineData(" t09ll0001b ", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("12345678", false)]
    [InlineData("123456789", false)]
    [InlineData("T09ZZ0001B", false)]
    [InlineData("T09LL00A1B", false)]
    [InlineData("R18LL0001A", true)]
    [InlineData("S99LL0001B", true)]
    [InlineData("T09LP0001A", true)]
    [InlineData("   ", false)]
    [InlineData("1234567AA", false)]
    [InlineData("1234567890A", false)]
    [InlineData("U09LL0001B", false)]
    [InlineData("T0ALL0001B", false)]
    [InlineData("1234 678A", false)]
    [InlineData("\uFF112345678A", false)]
    public void IsValid_ReturnsExpectedResult(string? input, bool expected)
    {
        var actual = UenValidator.IsValid(input);
        Assert.Equal(expected, actual);
    }
}