using System.Globalization;
using Xunit;

namespace LibRed.Engine.Tests;

/// <summary>
/// A Currency result is a Currency at every step: four places, half to even, and past ±922,337,203,685,477.5807 an
/// overflow — as CCur works it (verified vs ACE). What counts as a Currency result is ACE's: Currency with Currency or a
/// whole number, and Currency plus or less a Double; not Currency with a written decimal, times a Double, or with a
/// whole number too big for a Long, which ACE reads as a Decimal.
/// </summary>
public class CurrencyArithmeticTests(CurrencyArithmeticTests.Database database)
    : TempDatabaseTest, IClassFixture<CurrencyArithmeticTests.Database>
{
    // A Currency and a Double, each pair a row of P, and what ACE gave for C + D and C - D over that row.
    private static readonly (string C, string D, string Plus, string Minus)[] Pairs =
    [
        ("1", "0.00005", "1.0001", "0.9999"),
        ("1", "1.00005", "2.0001", "-0.0001"),
        ("1", "2.00005", "3", "-1"),
        ("1", "0.03125", "1.0312", "0.9688"),
        ("1", "0.09375", "1.0938", "0.9062"),
        ("1", "-0.00005", "0.9999", "1.0001"),
        ("0.0001", "0.03125", "0.0313", "-0.0311"),
        ("0.0003", "0.03125", "0.0315", "-0.0309"),
        ("0.0001", "0.09375", "0.0939", "-0.0937"),
        ("0.0002", "0.09375", "0.094", "-0.0936"),
        ("0.0001", "-0.03125", "-0.0311", "0.0313"),
        ("100000000000.0001", "0.00005", "100000000000.0002", "100000000000"),
        ("100000000000.0001", "0.00004", "100000000000.0001", "100000000000.0001"),
        ("900000000000000.0001", "0.00007", "900000000000000.0002", "900000000000000"),
        ("1.2345", "1.5", "2.7345", "-0.2655"),
    ];

    public static TheoryData<string, string, string, string> CurrencyAndDouble =>
        new(Pairs);

    // Z is the largest whole Currency.
    private static readonly string[] Setup =
    [
        "CREATE TABLE C (K LONG, A CURRENCY, B CURRENCY, Z CURRENCY, Y CURRENCY, I LONG, D DOUBLE, L BIGINT)",
        "INSERT INTO C (K, A, B, Z, Y, I, D, L) VALUES (1, 1.2345, 0.0003, 922337203685477, 1000000000.5, 3, 1.5, 1000000000000)",
        "CREATE TABLE P (C CURRENCY, D DOUBLE)",
        .. Pairs.Select(p => $"INSERT INTO P (C, D) VALUES (CCUR('{p.C}'), CDBL('{p.D}'))"),
        "CREATE TABLE Q (Id LONG, C CURRENCY, S SINGLE, T TEXT(20))",
        "INSERT INTO Q VALUES (0, CCUR('0.0001'), CSNG('0.03125'), '0.03125')",
        "INSERT INTO Q VALUES (1, CCUR('0.0001'), CSNG('0.09375'), '0.09375')",
        "INSERT INTO Q VALUES (2, CCUR('0.0001'), CSNG('-0.03125'), '-0.03125')",
        "INSERT INTO Q VALUES (3, CCUR('0.0001'), CSNG('1.00005'), '1.00005')",
    ];

    public sealed class Database() : SharedDatabase("currency-arith-", Setup);

    private object? Scalar(string expression) =>
        database.Scalar($"SELECT {expression} FROM C", CultureInfo.InvariantCulture);

    [Theory]
    [InlineData("A * A", "1.524")]
    [InlineData("A * B", "0.0004")]
    [InlineData("(A * A) * 1000", "1524")]          // the product is rounded before it is multiplied
    [InlineData("A * A + B", "1.5243")]
    [InlineData("A * I", "3.7035")]
    [InlineData("A - B", "1.2342")]
    public void A_currency_result_keeps_four_places(string expression, string expected) =>
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), Convert.ToDecimal(Scalar(expression)));

    // The Double is made a Currency first, from its exact binary value, and then added: 0.0001 + 0.03125 is
    // 0.0001 + 0.0312, where the exact sum 0.03135 would round to 0.0314.
    // Text and a Single are made a Currency first too, the text read exactly (Q's C is 0.0001; T and S hold the text).
    [Theory]
    [InlineData(0, "0.03125", "0.0313", "-0.0311", "0.0313", "-0.0311")]
    [InlineData(1, "0.09375", "0.0939", "-0.0937", "0.0939", "-0.0937")]
    [InlineData(2, "-0.03125", "-0.0311", "0.0313", "-0.0311", "0.0313")]
    [InlineData(3, "1.00005", "1.0001", "-0.9999", "1.0001", "-0.9999")]
    public void A_currency_plus_or_less_text_or_a_single_takes_it_as_a_currency_first(
        int id, string text, string plusText, string minusText, string plusSingle, string minusSingle)
    {
        foreach ((string expression, string expected) in new[]
                 {
                     ($"C + '{text}'", plusText), ($"C - '{text}'", minusText), ("C + T", plusText), ("C - T", minusText),
                     ("C + S", plusSingle), ("C - S", minusSingle),
                 })
            Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture),
                Assert.IsType<decimal>(database.Scalar($"SELECT {expression} FROM Q WHERE Id = {id}", CultureInfo.InvariantCulture)));
    }

    [Theory]
    [MemberData(nameof(CurrencyAndDouble))]
    public void A_currency_plus_or_less_a_double_takes_the_double_as_a_currency_first(string c, string d, string plus, string minus)
    {
        string row = $"FROM P WHERE C = CCUR('{c}') AND D = CDBL('{d}')";
        Assert.Equal(decimal.Parse(plus, CultureInfo.InvariantCulture),
            Assert.IsType<decimal>(database.Scalar($"SELECT C + D {row}", CultureInfo.InvariantCulture)));
        Assert.Equal(decimal.Parse(minus, CultureInfo.InvariantCulture),
            Assert.IsType<decimal>(database.Scalar($"SELECT C - D {row}", CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData("A + 0.00005", "1.23455")]                          // with a written decimal it is a Decimal
    [InlineData("Z + 0.5808", "922337203685477.5808")]              // ... so past the Currency range is no overflow
    [InlineData("Z + 1000000000000", "923337203685477")]
    public void A_currency_with_a_decimal_is_not_a_currency(string expression, string expected) =>
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), Convert.ToDecimal(Scalar(expression)));

    // A literal past a Long is a Decimal too, and a Currency times a Decimal is a Double (verified vs ACE).
    [Fact]
    public void A_currency_times_a_large_number_is_a_double() =>
        Assert.Equal(8.64000000432E+20, Assert.IsType<double>(Scalar("Y * 864000000000")));

    [Theory]
    [InlineData("Z + 1")]
    [InlineData("-Z - 1")]
    [InlineData("Z * I")]
    [InlineData("Z + Z")]
    [InlineData("Y * Y")]
    [InlineData("Y * L")]
    [InlineData("L * Y")]
    public void Past_the_currency_range_is_an_overflow(string expression) =>
        Assert.Throws<OverflowException>(() => Scalar(expression));

    // The literal's type decides the places as well: ACE takes 864000000000 / 7 to a Decimal of none.
    [Fact]
    public void A_literal_past_a_long_has_no_places() =>
        Assert.Equal(123428571428m, Convert.ToDecimal(Scalar("864000000000 / 7")));

    // Its value is still the Int64 LibRed reads it as, so MOD and \ stay exact over it.
    [Fact]
    public void A_literal_past_a_long_is_still_an_int64() =>
        Assert.Equal(136000000000L, Scalar("L MOD 864000000000"));
}
