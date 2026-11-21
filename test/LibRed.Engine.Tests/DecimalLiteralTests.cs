using System.Globalization;
using LibRed.Engine;
using Xunit;

namespace LibRed.Engine.Tests;

/// <summary>
/// What a number written with a decimal point or an exponent is: a whole number a Long holds is a Long, a number a
/// Decimal holds exactly is that Decimal, and anything else is a Double — a number written with a point and no
/// exponent whose whole part ends in a zero among them. Every expectation is what ACE returned, through VarType and
/// the value's type.
/// </summary>
public class DecimalLiteralTests(DecimalLiteralTests.Database database)
    : TempDatabaseTest, IClassFixture<DecimalLiteralTests.Database>
{
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    private static readonly string[] Setup =
    [
        "CREATE TABLE T (Id LONG)",
        "INSERT INTO T (Id) VALUES (1)",
    ];

    public sealed class Database() : SharedDatabase("decimal-literal-", Setup);

    private object? Scalar(string expression) => database.Scalar($"SELECT {expression} FROM T", EnUs);

    [Theory]
    [InlineData("1.5", "1.5")]
    [InlineData("1.5E0", "1.5")]
    [InlineData("15E-1", "1.5")]
    [InlineData(".5", "0.5")]
    [InlineData("-1.5", "-1.5")]
    [InlineData("1.50", "1.5")]
    [InlineData("1E-28", "0.0000000000000000000000000001")]
    [InlineData("1.23456E-20", "0.0000000000000000000123456")]
    [InlineData("3E9", "3000000000")]
    [InlineData("1E-1", "0.1")]
    [InlineData("1.10", "1.1")]
    [InlineData("10.5", "10.5")]
    [InlineData("123.456", "123.456")]
    // Whole and past a Long, its whole part not ending in a zero.
    [InlineData("2147483648.0", "2147483648")]
    [InlineData("3000000001.0", "3000000001")]
    public void A_number_a_decimal_holds_exactly_is_a_decimal(string literal, string expected)
    {
        Assert.Equal(14, Convert.ToInt32(Scalar($"VarType({literal})"), CultureInfo.InvariantCulture));
        Assert.Equal(expected, Scalar($"CStr({literal})"));
    }

    [Theory]
    [InlineData("1E5", 100000)]
    [InlineData("3.0", 3)]
    [InlineData("0.", 0)]
    [InlineData("00.0", 0)]
    [InlineData("1.000", 1)]
    [InlineData("11.0", 11)]
    [InlineData("-11.0", -11)]
    [InlineData("105.0", 105)]
    [InlineData("1001.0", 1001)]
    [InlineData("2147483647.0", 2147483647)]
    // With an exponent, a whole part ending in a zero is a Long too.
    [InlineData("1E1", 10)]
    [InlineData("10E1", 100)]
    [InlineData("2.0E1", 20)]
    public void A_whole_number_a_long_holds_is_a_long(string literal, int expected) =>
        Assert.Equal(expected, Assert.IsType<int>(Scalar(literal)));

    [Theory]
    [InlineData("5E-324", 5E-324)]
    [InlineData("1.5E-28", 1.5E-28)]
    [InlineData("1E-29", 1E-29)]
    [InlineData("1E29", 1E29)]
    [InlineData("1E300", 1E300)]
    [InlineData("3000000000.0", 3000000000d)]
    [InlineData("1E30", 1E30)]
    // Written with a point and no exponent, a whole part ending in a zero.
    [InlineData("10.0", 10d)]
    [InlineData("10.", 10d)]
    [InlineData("10.00", 10d)]
    [InlineData("010.0", 10d)]
    [InlineData("-10.0", -10d)]
    [InlineData("20.0", 20d)]
    [InlineData("1000.0", 1000d)]
    [InlineData("9990.0", 9990d)]
    [InlineData("2147483650.0", 2147483650d)]
    public void Anything_else_is_a_double(string literal, double expected) =>
        Assert.Equal(expected, Assert.IsType<double>(Scalar(literal)));
}
