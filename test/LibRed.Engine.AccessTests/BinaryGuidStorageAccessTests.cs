using System.Data.OleDb;
using System.Globalization;
using Xunit;

namespace LibRed.Engine.Tests;

// A value stored into a binary or GUID column, and a DEFAULT converted to its column's type, against ACE.
//
// A number stored into a binary column is its own bytes — an Int16 two, so True is FFFF; an Int32 and a Byte four; a
// Single four; a Double, a date and a Currency eight — text its UTF-16 bytes, cut to a fixed column's width, and a
// BIGINT the bytes of its text. A whole number stored into a GUID column is its first four bytes, text that is not a
// GUID the zero GUID, and bytes too few for a GUID are refused. A DEFAULT is converted to its column's type when it is
// declared, and one the column cannot take refuses the statement there — CREATE TABLE, ADD COLUMN and ALTER COLUMN SET
// DEFAULT alike.
//
// Each case runs the same statements through ACE and through LibRed on copies of Northwind: every statement's
// outcome — the rows it affected, or its refusal; the two engines word a refusal differently — and then table T as
// each engine reads its own file must agree.
[Collection(AceCollection.Name)]
public class BinaryGuidStorageAccessTests : TempDatabaseTest
{
    private const string Guid5 = "{guid {00000001-0002-0003-0004-000000000005}}";

    // A value written into the column by INSERT ... VALUES and by UPDATE.
    public static TheoryData<string, string> Written => new()
    {
        // A Boolean, a number, text and a date, written and computed.
        { "BINARY(4)", "True" }, { "BINARY(4)", "False" }, { "BINARY(4)", "(1=1)" }, { "BINARY(4)", "CInt(-1)" }, { "BINARY(4)", "1" },
        { "BINARY(4)", "-1" }, { "BINARY(4)", "300" }, { "BINARY(4)", "70000" }, { "BINARY(4)", "CLng(1)" }, { "BINARY(4)", "CSng(1)" },
        { "BINARY(4)", "CDbl(1)" }, { "BINARY(4)", "1.5" }, { "BINARY(4)", "'1'" }, { "BINARY(4)", "'True'" }, { "BINARY(4)", "#1/1/2000#" },
        { "BINARY(4)", "NULL" },
        { "BINARY(8)", "True" }, { "BINARY(8)", "False" }, { "BINARY(8)", "(1=1)" }, { "BINARY(8)", "CInt(-1)" }, { "BINARY(8)", "1" },
        { "BINARY(8)", "-1" }, { "BINARY(8)", "300" }, { "BINARY(8)", "70000" }, { "BINARY(8)", "CLng(1)" }, { "BINARY(8)", "CSng(1)" },
        { "BINARY(8)", "CDbl(1)" }, { "BINARY(8)", "1.5" }, { "BINARY(8)", "'1'" }, { "BINARY(8)", "'True'" }, { "BINARY(8)", "#1/1/2000#" },
        { "BINARY(8)", "NULL" },
        { "VARBINARY(8)", "True" }, { "VARBINARY(8)", "False" }, { "VARBINARY(8)", "(1=1)" }, { "VARBINARY(8)", "CInt(-1)" }, { "VARBINARY(8)", "1" },
        { "VARBINARY(8)", "-1" }, { "VARBINARY(8)", "300" }, { "VARBINARY(8)", "70000" }, { "VARBINARY(8)", "CLng(1)" }, { "VARBINARY(8)", "CSng(1)" },
        { "VARBINARY(8)", "CDbl(1)" }, { "VARBINARY(8)", "1.5" }, { "VARBINARY(8)", "'1'" }, { "VARBINARY(8)", "'True'" },
        { "VARBINARY(8)", "#1/1/2000#" }, { "VARBINARY(8)", "NULL" },
        { "LONGBINARY", "True" }, { "LONGBINARY", "False" }, { "LONGBINARY", "(1=1)" }, { "LONGBINARY", "CInt(-1)" }, { "LONGBINARY", "1" },
        { "LONGBINARY", "-1" }, { "LONGBINARY", "300" }, { "LONGBINARY", "CLng(1)" }, { "LONGBINARY", "CSng(1)" }, { "LONGBINARY", "CDbl(1)" },
        { "LONGBINARY", "1.5" }, { "LONGBINARY", "'1'" }, { "LONGBINARY", "#1/1/2000#" }, { "LONGBINARY", "NULL" },
        { "GUID", "True" }, { "GUID", "False" }, { "GUID", "CInt(-1)" }, { "GUID", "1" }, { "GUID", "-1" }, { "GUID", "300" }, { "GUID", "70000" },
        { "GUID", "CLng(1)" }, { "GUID", "CSng(1)" }, { "GUID", "CDbl(1)" }, { "GUID", "1.5" }, { "GUID", "'1'" }, { "GUID", "'True'" },
        { "GUID", "'{00000001-0002-0003-0004-000000000005}'" }, { "GUID", "NULL" },
        // A Currency is its eight-byte scaled integer; a GUID refuses it.
        { "BINARY(4)", "CCur(1)" }, { "BINARY(4)", "CCur(-1.5)" }, { "BINARY(4)", "CCur(1)+CCur(1)" }, { "BINARY(4)", "CCur(12345.67)" },
        { "BINARY(8)", "CCur(1)" }, { "BINARY(8)", "CCur(-1.5)" }, { "BINARY(8)", "CCur(1)+CCur(1)" }, { "BINARY(8)", "CCur(12345.67)" },
        { "VARBINARY(8)", "CCur(1)" }, { "VARBINARY(8)", "CCur(-1.5)" }, { "VARBINARY(8)", "CCur(1)+CCur(1)" }, { "VARBINARY(8)", "CCur(12345.67)" },
        { "LONGBINARY", "CCur(1)" }, { "LONGBINARY", "CCur(-1.5)" }, { "LONGBINARY", "CCur(1)+CCur(1)" }, { "LONGBINARY", "CCur(12345.67)" },
        { "GUID", "CCur(1)" }, { "GUID", "CCur(-1.5)" }, { "GUID", "CCur(1)+CCur(1)" }, { "GUID", "CCur(12345.67)" },
        // A GUID, a Double and text, into a column of sixteen bytes and fewer.
        { "BINARY(4)", Guid5 }, { "BINARY(4)", "CDbl(1.5)" }, { "BINARY(4)", "'ab'" },
        { "BINARY(16)", Guid5 }, { "BINARY(16)", "CLng(1)" }, { "BINARY(16)", "CCur(1)" }, { "BINARY(16)", "1.5" }, { "BINARY(16)", "CDbl(1.5)" },
        { "BINARY(16)", "True" }, { "BINARY(16)", "'ab'" },
        { "VARBINARY(16)", Guid5 }, { "VARBINARY(16)", "CLng(1)" }, { "VARBINARY(16)", "CCur(1)" }, { "VARBINARY(16)", "1.5" },
        { "VARBINARY(16)", "CDbl(1.5)" }, { "VARBINARY(16)", "True" }, { "VARBINARY(16)", "'ab'" },
        { "GUID", Guid5 }, { "GUID", "CDbl(1.5)" }, { "GUID", "'ab'" },
        // A Decimal whose text is wider than the column.
        { "BINARY(4)", "1234567.891" }, { "BINARY(16)", "1234567.891" }, { "VARBINARY(16)", "1234567.891" },
        // A BigBinary column's declared width, against a value past it.
        { "BIGBINARY(10)", "'abcdefghij'" }, { "BIGBINARY(10)", "0x0102030405060708090A0B0C" },
        // A Decimal or binary value's bytes are a GUID when there are sixteen of them.
        { "GUID", "1.234567" }, { "GUID", "0x0100000002000300000400000000000F" },
        // CByte gives an Integer, not a Byte.
        { "BINARY(4)", "CByte(1)" }, { "BINARY(8)", "CByte(1)" }, { "VARBINARY(8)", "CByte(1)" }, { "LONGBINARY", "CByte(1)" },
        { "GUID", "CByte(1)" },
    };

    [Theory]
    [MemberData(nameof(Written))]
    public void A_value_written_into_a_binary_or_GUID_column_is_stored_as_ACE_stores_it(string target, string value)
    {
        string[] statements =
        [
            $"CREATE TABLE T (ID LONG, V {target})", $"INSERT INTO T (ID, V) VALUES (1, {value})",
            "INSERT INTO T (ID) VALUES (2)", $"UPDATE T SET V = {value} WHERE ID = 2",
        ];
        Assert.Equal(Describe(statements, AceRun), Describe(statements, LibRedRun));
    }

    // A column's value read into the column: by INSERT ... SELECT, by UPDATE through a join and by ALTER COLUMN.
    public static TheoryData<string, string, string> FromColumn => new()
    {
        { "BINARY(4)", "CURRENCY", "1" }, { "BINARY(8)", "CURRENCY", "1" }, { "VARBINARY(8)", "CURRENCY", "1" }, { "LONGBINARY", "CURRENCY", "1" },
        { "GUID", "CURRENCY", "1" },
        { "BINARY(4)", "GUID", Guid5 }, { "BINARY(4)", "LONG", "1" }, { "BINARY(4)", "DECIMAL(18,1)", "1.5" }, { "BINARY(4)", "DOUBLE", "1.5" },
        { "BINARY(4)", "YESNO", "True" }, { "BINARY(4)", "TEXT(10)", "'ab'" },
        { "BINARY(16)", "GUID", Guid5 }, { "BINARY(16)", "LONG", "1" }, { "BINARY(16)", "CURRENCY", "1" }, { "BINARY(16)", "DECIMAL(18,1)", "1.5" },
        { "BINARY(16)", "DOUBLE", "1.5" }, { "BINARY(16)", "YESNO", "True" }, { "BINARY(16)", "TEXT(10)", "'ab'" },
        { "VARBINARY(16)", "GUID", Guid5 }, { "VARBINARY(16)", "LONG", "1" }, { "VARBINARY(16)", "CURRENCY", "1" },
        { "VARBINARY(16)", "DECIMAL(18,1)", "1.5" }, { "VARBINARY(16)", "DOUBLE", "1.5" }, { "VARBINARY(16)", "YESNO", "True" },
        { "VARBINARY(16)", "TEXT(10)", "'ab'" },
        { "GUID", "GUID", Guid5 }, { "GUID", "LONG", "1" }, { "GUID", "DECIMAL(18,1)", "1.5" }, { "GUID", "DOUBLE", "1.5" }, { "GUID", "YESNO", "True" },
        { "GUID", "TEXT(10)", "'ab'" },
        { "BINARY(4)", "DECIMAL(18,3)", "1234567.891" }, { "BINARY(16)", "DECIMAL(18,3)", "1234567.891" },
        { "VARBINARY(16)", "DECIMAL(18,3)", "1234567.891" },
    };

    [Theory]
    [MemberData(nameof(FromColumn))]
    public void A_column_selected_into_a_binary_or_GUID_column_is_stored_as_ACE_stores_it(string target, string column, string seed)
    {
        string[] statements =
        [
            $"CREATE TABLE S (ID LONG, X {column})", $"INSERT INTO S VALUES (1, {seed})",
            $"CREATE TABLE T (ID LONG, V {target})", "INSERT INTO T (ID, V) SELECT ID, X FROM S",
        ];
        Assert.Equal(Describe(statements, AceRun), Describe(statements, LibRedRun));
    }

    [Theory]
    [MemberData(nameof(FromColumn))]
    public void A_column_joined_into_a_binary_or_GUID_column_is_stored_as_ACE_stores_it(string target, string column, string seed)
    {
        string[] statements =
        [
            $"CREATE TABLE S (ID LONG, X {column})", $"INSERT INTO S VALUES (1, {seed})",
            $"CREATE TABLE T (ID LONG, V {target})", "INSERT INTO T (ID) VALUES (1)", "UPDATE T INNER JOIN S ON T.ID = S.ID SET T.V = S.X",
        ];
        Assert.Equal(Describe(statements, AceRun), Describe(statements, LibRedRun));
    }

    // A column of another type retyped into one.
    public static TheoryData<string, string, string> Retyped => new()
    {
        { "BINARY(4)", "BYTE", "1" }, { "BINARY(4)", "SHORT", "-1" }, { "BINARY(4)", "SINGLE", "1" }, { "BINARY(4)", "DOUBLE", "1" },
        { "BINARY(4)", "DATETIME", "#1/1/2000#" }, { "BINARY(4)", "TEXT(10)", "'1'" },
        { "BINARY(8)", "BYTE", "1" }, { "BINARY(8)", "SHORT", "-1" }, { "BINARY(8)", "LONG", "1" }, { "BINARY(8)", "SINGLE", "1" },
        { "BINARY(8)", "DOUBLE", "1" }, { "BINARY(8)", "YESNO", "True" }, { "BINARY(8)", "DATETIME", "#1/1/2000#" }, { "BINARY(8)", "TEXT(10)", "'1'" },
        { "VARBINARY(8)", "BYTE", "1" }, { "VARBINARY(8)", "SHORT", "-1" }, { "VARBINARY(8)", "LONG", "1" }, { "VARBINARY(8)", "SINGLE", "1" },
        { "VARBINARY(8)", "DOUBLE", "1" }, { "VARBINARY(8)", "YESNO", "True" }, { "VARBINARY(8)", "DATETIME", "#1/1/2000#" },
        { "VARBINARY(8)", "TEXT(10)", "'1'" },
        { "LONGBINARY", "BYTE", "1" }, { "LONGBINARY", "SHORT", "-1" }, { "LONGBINARY", "LONG", "1" }, { "LONGBINARY", "SINGLE", "1" },
        { "LONGBINARY", "DOUBLE", "1" }, { "LONGBINARY", "YESNO", "True" }, { "LONGBINARY", "DATETIME", "#1/1/2000#" }, { "LONGBINARY", "TEXT(10)", "'1'" },
        { "GUID", "BYTE", "1" }, { "GUID", "SHORT", "-1" }, { "GUID", "SINGLE", "1" }, { "GUID", "DOUBLE", "1" }, { "GUID", "TEXT(10)", "'1'" },
        { "BIGBINARY(10)", "TEXT(10)", "'abcdefghij'" },
        { "GUID", "DECIMAL(18,6)", "1.234567" }, { "GUID", "BINARY(16)", "0x0100000002000300000400000000000F" },
    };

    [Theory]
    [MemberData(nameof(FromColumn))]
    [MemberData(nameof(Retyped))]
    public void A_column_retyped_to_a_binary_or_GUID_column_is_stored_as_ACE_stores_it(string target, string column, string seed)
    {
        string[] statements = [$"CREATE TABLE T (ID LONG, V {column})", $"INSERT INTO T VALUES (1, {seed})", $"ALTER TABLE T ALTER COLUMN V {target}"];
        Assert.Equal(Describe(statements, AceRun), Describe(statements, LibRedRun));
    }

    // A value computed over a column's, selected into the column.
    public static TheoryData<string, string, string, string> SelectedValue => new()
    {
        { "BINARY(4)", "CCur(1)", "CURRENCY", "1" }, { "BINARY(8)", "CCur(1)", "CURRENCY", "1" }, { "VARBINARY(8)", "CCur(1)", "CURRENCY", "1" },
        { "LONGBINARY", "CCur(1)", "CURRENCY", "1" }, { "GUID", "CCur(1)", "CURRENCY", "1" },
        { "BINARY(4)", Guid5, "GUID", Guid5 }, { "BINARY(4)", "CLng(1)", "LONG", "1" }, { "BINARY(4)", "1.5", "DECIMAL(18,1)", "1.5" },
        { "BINARY(4)", "CDbl(1.5)", "DOUBLE", "1.5" }, { "BINARY(4)", "True", "YESNO", "True" }, { "BINARY(4)", "'ab'", "TEXT(10)", "'ab'" },
        { "BINARY(16)", Guid5, "GUID", Guid5 }, { "BINARY(16)", "CLng(1)", "LONG", "1" }, { "BINARY(16)", "CCur(1)", "CURRENCY", "1" },
        { "BINARY(16)", "1.5", "DECIMAL(18,1)", "1.5" }, { "BINARY(16)", "CDbl(1.5)", "DOUBLE", "1.5" }, { "BINARY(16)", "True", "YESNO", "True" },
        { "BINARY(16)", "'ab'", "TEXT(10)", "'ab'" },
        { "VARBINARY(16)", Guid5, "GUID", Guid5 }, { "VARBINARY(16)", "CLng(1)", "LONG", "1" }, { "VARBINARY(16)", "CCur(1)", "CURRENCY", "1" },
        { "VARBINARY(16)", "1.5", "DECIMAL(18,1)", "1.5" }, { "VARBINARY(16)", "CDbl(1.5)", "DOUBLE", "1.5" },
        { "VARBINARY(16)", "True", "YESNO", "True" }, { "VARBINARY(16)", "'ab'", "TEXT(10)", "'ab'" },
        { "GUID", Guid5, "GUID", Guid5 }, { "GUID", "CLng(1)", "LONG", "1" }, { "GUID", "1.5", "DECIMAL(18,1)", "1.5" },
        { "GUID", "CDbl(1.5)", "DOUBLE", "1.5" }, { "GUID", "True", "YESNO", "True" }, { "GUID", "'ab'", "TEXT(10)", "'ab'" },
        // A Decimal whose text is wider than the column, written and computed from the column.
        { "BINARY(4)", "1234567.891", "DECIMAL(18,3)", "1234567.891" }, { "BINARY(4)", "X + 0", "DECIMAL(18,3)", "1234567.891" },
        { "BINARY(16)", "1234567.891", "DECIMAL(18,3)", "1234567.891" }, { "BINARY(16)", "X + 0", "DECIMAL(18,3)", "1234567.891" },
        { "VARBINARY(16)", "1234567.891", "DECIMAL(18,3)", "1234567.891" }, { "VARBINARY(16)", "X + 0", "DECIMAL(18,3)", "1234567.891" },
    };

    [Theory]
    [MemberData(nameof(SelectedValue))]
    public void A_value_selected_into_a_binary_or_GUID_column_is_stored_as_ACE_stores_it(string target, string value, string column, string seed)
    {
        string[] statements =
        [
            $"CREATE TABLE S (ID LONG, X {column})", $"INSERT INTO S VALUES (1, {seed})",
            $"CREATE TABLE T (ID LONG, V {target})", $"INSERT INTO T (ID, V) SELECT ID, {value} FROM S",
        ];
        Assert.Equal(Describe(statements, AceRun), Describe(statements, LibRedRun));
    }

    // A Currency read through a derived table.
    [Theory]
    [InlineData("BINARY(4)")]
    [InlineData("BINARY(8)")]
    [InlineData("VARBINARY(8)")]
    [InlineData("LONGBINARY")]
    [InlineData("GUID")]
    public void A_Currency_joined_through_a_derived_table_into_a_binary_or_GUID_column_is_stored_as_ACE_stores_it(string target)
    {
        string[] statements =
        [
            "CREATE TABLE CS (ID LONG, C CURRENCY)", "INSERT INTO CS VALUES (1, 1)",
            $"CREATE TABLE T (ID LONG, V {target})", "INSERT INTO T (ID) VALUES (1)",
            "UPDATE T INNER JOIN (SELECT ID, C FROM CS) AS D ON T.ID = D.ID SET T.V = D.C",
        ];
        Assert.Equal(Describe(statements, AceRun), Describe(statements, LibRedRun));
    }

    // A Large Number (BIGINT) stored into a binary column is the bytes of its text, and into a GUID column those
    // bytes, too few for a GUID; one whose text is wider than the column is refused.
    [Theory]
    [InlineData("BINARY(4)", "1")]
    [InlineData("BINARY(16)", "1")]
    [InlineData("VARBINARY(16)", "1")]
    [InlineData("GUID", "1")]
    [InlineData("BINARY(4)", "123456789")]
    [InlineData("BINARY(16)", "123456789")]
    [InlineData("VARBINARY(16)", "123456789")]
    [InlineData("BINARY(4)", "-123456789")]
    [InlineData("BINARY(16)", "-123456789")]
    [InlineData("VARBINARY(16)", "-123456789")]
    public void A_BIGINT_stored_into_a_binary_or_GUID_column_is_stored_as_ACE_stores_it(string target, string seed)
    {
        Assert.SkipUnless(
            AceTestDatabase.SupportsColumnType(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "BIGINT"),
            AceTestDatabase.UnsupportedColumnTypeReason("BIGINT"));

        foreach (string[] statements in (string[][])
            [
                ["CREATE TABLE S (ID LONG, X BIGINT)", $"INSERT INTO S VALUES (1, {seed})", $"CREATE TABLE T (ID LONG, V {target})",
                    "INSERT INTO T (ID, V) SELECT ID, X FROM S"],
                ["CREATE TABLE S (ID LONG, X BIGINT)", $"INSERT INTO S VALUES (1, {seed})", $"CREATE TABLE T (ID LONG, V {target})",
                    "INSERT INTO T (ID, V) SELECT ID, X * 2 FROM S"],
                ["CREATE TABLE S (ID LONG, X BIGINT)", $"INSERT INTO S VALUES (1, {seed})", $"CREATE TABLE T (ID LONG, V {target})",
                    "INSERT INTO T (ID, V) SELECT ID, X + 0 FROM S"],
                ["CREATE TABLE S (ID LONG, X BIGINT)", $"INSERT INTO S VALUES (1, {seed})", $"CREATE TABLE T (ID LONG, V {target})",
                    "INSERT INTO T (ID) VALUES (1)", "UPDATE T INNER JOIN S ON T.ID = S.ID SET T.V = S.X"],
                ["CREATE TABLE T (ID LONG, V BIGINT)", $"INSERT INTO T VALUES (1, {seed})", $"ALTER TABLE T ALTER COLUMN V {target}"],
            ])
            Assert.Equal(Describe(statements, AceRun), Describe(statements, LibRedRun));
    }

    // A DEFAULT, by CREATE TABLE, ADD COLUMN and ALTER COLUMN SET DEFAULT.
    public static TheoryData<string, string> Defaults => new()
    {
        // Into a binary or GUID column.
        { "BINARY(4)", "True" }, { "BINARY(4)", "False" }, { "BINARY(4)", "CInt(-1)" }, { "BINARY(4)", "1" }, { "BINARY(4)", "-1" }, { "BINARY(4)", "'1'" },
        { "BINARY(8)", "True" }, { "BINARY(8)", "False" }, { "BINARY(8)", "CInt(-1)" }, { "BINARY(8)", "1" }, { "BINARY(8)", "-1" }, { "BINARY(8)", "'1'" },
        { "VARBINARY(8)", "True" }, { "VARBINARY(8)", "False" }, { "VARBINARY(8)", "CInt(-1)" }, { "VARBINARY(8)", "1" }, { "VARBINARY(8)", "-1" },
        { "VARBINARY(8)", "'1'" },
        { "LONGBINARY", "True" }, { "LONGBINARY", "False" }, { "LONGBINARY", "CInt(-1)" }, { "LONGBINARY", "1" }, { "LONGBINARY", "-1" },
        { "LONGBINARY", "'1'" },
        { "GUID", "True" }, { "GUID", "False" }, { "GUID", "CInt(-1)" }, { "GUID", "1" }, { "GUID", "-1" }, { "GUID", "'1'" },
        { "BINARY(8)", "CCur(1)" },
        { "BINARY(4)", "CLng(1)" }, { "BINARY(4)", "CCur(1)" }, { "BINARY(4)", "1.5" }, { "BINARY(4)", "CDbl(1.5)" }, { "BINARY(4)", "'ab'" },
        { "BINARY(16)", "True" }, { "BINARY(16)", "CLng(1)" }, { "BINARY(16)", "CCur(1)" }, { "BINARY(16)", "1.5" }, { "BINARY(16)", "CDbl(1.5)" },
        { "BINARY(16)", "'ab'" },
        { "VARBINARY(16)", "True" }, { "VARBINARY(16)", "CLng(1)" }, { "VARBINARY(16)", "CCur(1)" }, { "VARBINARY(16)", "1.5" },
        { "VARBINARY(16)", "CDbl(1.5)" }, { "VARBINARY(16)", "'ab'" },
        { "GUID", "CLng(1)" }, { "GUID", "CCur(1)" }, { "GUID", "1.5" }, { "GUID", "CDbl(1.5)" }, { "GUID", "'ab'" },
        { "GUID", "1.234567" }, { "GUID", "12345678.1" }, { "GUID", "CCur(1234567)" },
        { "BINARY(4)", "1234567.891" }, { "BINARY(16)", "1234567.891" }, { "VARBINARY(16)", "1234567.891" },
        // One the column cannot take, with a Byte's range checked where a write of the same value keeps its low byte.
        { "BYTE", "True" }, { "BYTE", "False" }, { "BYTE", "Yes" }, { "BYTE", "CInt(True)" }, { "BYTE", "-1" }, { "BYTE", "255" }, { "BYTE", "256" },
        { "BYTE", "300" }, { "BYTE", "70000" }, { "BYTE", "3000000000" }, { "BYTE", "1.5" }, { "BYTE", "2.5" }, { "BYTE", "'abc'" }, { "BYTE", "'5'" },
        { "BYTE", "'300'" }, { "BYTE", "#1/1/2000#" }, { "BYTE", "Now()" }, { "BYTE", "CInt(300)" }, { "BYTE", "1+299" }, { "BYTE", "NULL" },
        { "BYTE", "12345.67" },
        { "SHORT", "True" }, { "SHORT", "False" }, { "SHORT", "Yes" }, { "SHORT", "CInt(True)" }, { "SHORT", "-1" }, { "SHORT", "255" },
        { "SHORT", "256" }, { "SHORT", "300" }, { "SHORT", "70000" }, { "SHORT", "3000000000" }, { "SHORT", "1.5" }, { "SHORT", "2.5" },
        { "SHORT", "'abc'" }, { "SHORT", "'5'" }, { "SHORT", "'300'" }, { "SHORT", "#1/1/2000#" }, { "SHORT", "Now()" }, { "SHORT", "CInt(300)" },
        { "SHORT", "1+299" }, { "SHORT", "NULL" }, { "SHORT", "12345.67" },
        { "LONG", "True" }, { "LONG", "False" }, { "LONG", "Yes" }, { "LONG", "CInt(True)" }, { "LONG", "-1" }, { "LONG", "300" }, { "LONG", "70000" },
        { "LONG", "3000000000" }, { "LONG", "1.5" }, { "LONG", "2.5" }, { "LONG", "'abc'" }, { "LONG", "'5'" }, { "LONG", "#1/1/2000#" },
        { "LONG", "CInt(300)" }, { "LONG", "1+299" }, { "LONG", "NULL" }, { "LONG", "12345.67" },
        { "TEXT(3)", "True" }, { "TEXT(3)", "False" }, { "TEXT(3)", "CInt(True)" }, { "TEXT(3)", "-1" }, { "TEXT(3)", "300" }, { "TEXT(3)", "70000" },
        { "TEXT(3)", "1.5" }, { "TEXT(3)", "'abc'" }, { "TEXT(3)", "'abcdef'" }, { "TEXT(3)", "'5'" }, { "TEXT(3)", "NULL" },
        { "GUID", "CInt(True)" }, { "GUID", "CInt(300)" }, { "GUID", "300" }, { "GUID", "#1/1/2000#" }, { "GUID", "NULL" },
        // Cut to a binary column's width, kept whole by a BIGBINARY or a LONGBINARY, Null for a DECIMAL it is too wide
        // for, and checked as an Integer (Int16) for a Yes/No column.
        { "BINARY(4)", "'abc'" }, { "VARBINARY(4)", "1.5" }, { "BIGBINARY(10)", "'abcdefghij'" }, { "LONGBINARY", "'" + new string('a', 100) + "'" },
        { "DECIMAL(4,1)", "70000" }, { "DECIMAL(4,1)", "3000000000" }, { "DECIMAL(4,1)", "#1/1/2000#" }, { "DECIMAL(4,1)", "12345.67" },
        { "DECIMAL(4,1)", "1000" }, { "DECIMAL(4,1)", "1.5" }, { "DECIMAL(4,1)", "999.9" }, { "DECIMAL(4,1)", "999.95" }, { "DECIMAL(4,1)", "-999.9" },
        { "DECIMAL(4,1)", "'12.5'" }, { "DECIMAL(4,1)", "Now()" },
        { "YESNO", "32767" }, { "YESNO", "32768" }, { "YESNO", "-32768" }, { "YESNO", "-32769" }, { "YESNO", "70000" }, { "YESNO", "3000000000" },
        { "YESNO", "#1/1/2000#" }, { "YESNO", "CDbl(70000)" }, { "YESNO", "CLng(70000)" }, { "YESNO", "1.5" }, { "YESNO", "12345.67" },
        { "YESNO", "'5'" }, { "YESNO", "'abc'" }, { "YESNO", "Now()" },
    };

    [Theory]
    [MemberData(nameof(Defaults))]
    public void A_DEFAULT_is_converted_cut_nulled_or_refused_as_ACE_does_it(string type, string value)
    {
        foreach (string[] statements in (string[][])
            [
                [$"CREATE TABLE T (ID LONG, V {type} DEFAULT {value})", "INSERT INTO T (ID) VALUES (1)"],
                ["CREATE TABLE T (ID LONG)", $"ALTER TABLE T ADD COLUMN V {type} DEFAULT {value}", "INSERT INTO T (ID) VALUES (1)"],
                [$"CREATE TABLE T (ID LONG, V {type})", $"ALTER TABLE T ALTER COLUMN V SET DEFAULT {value}", "INSERT INTO T (ID) VALUES (1)"],
            ])
            Assert.Equal(Describe(statements, AceRun), Describe(statements, LibRedRun));
    }

    // A BigBinary DEFAULT wider than its column is stored whole; what then becomes of the row it was written into, when
    // the row is written again, its value is copied, or its table is altered.
    [Theory]
    [InlineData("UPDATE T SET ID = 2")]
    [InlineData("UPDATE T SET V = V")]
    [InlineData("UPDATE T SET ID = 2, V = V")]
    [InlineData("UPDATE T SET V = 0x0102")]
    [InlineData("INSERT INTO T (ID, V) SELECT 2, V FROM T")]
    [InlineData("ALTER TABLE T ALTER COLUMN ID DOUBLE")]
    [InlineData("ALTER TABLE T ALTER COLUMN ID TEXT(20)")]
    [InlineData("ALTER TABLE T ALTER COLUMN V BIGBINARY(10)")]
    [InlineData("ALTER TABLE T ALTER COLUMN V BIGBINARY(15)")]
    [InlineData("ALTER TABLE T ALTER COLUMN V BIGBINARY(20)")]
    [InlineData("ALTER TABLE T ALTER COLUMN V BINARY(10)")]
    [InlineData("ALTER TABLE T ALTER COLUMN V VARBINARY(10)")]
    [InlineData("ALTER TABLE T ALTER COLUMN V LONGBINARY")]
    [InlineData("ALTER TABLE T ADD COLUMN W LONG")]
    public void A_BigBinary_DEFAULT_past_its_width_is_kept_whole_as_ACE_keeps_it(string then)
    {
        string[] statements = ["CREATE TABLE T (ID LONG, V BIGBINARY(10) DEFAULT 'abcdefghij')", "INSERT INTO T (ID) VALUES (1)", then];
        Assert.Equal(Describe(statements, AceRun), Describe(statements, LibRedRun));
    }

    // Retyping a column to Yes/No does not check its values as a Yes/No DEFAULT is checked.
    [Theory]
    [InlineData("LONG", "70000")]
    [InlineData("DATETIME", "#1/1/2000#")]
    public void A_column_retyped_to_YesNo_keeps_a_value_its_DEFAULT_would_refuse(string type, string value)
    {
        string[] statements = [$"CREATE TABLE T (ID LONG, V {type})", $"INSERT INTO T VALUES (1, {value})", "ALTER TABLE T ALTER COLUMN V YESNO"];
        Assert.Equal(Describe(statements, AceRun), Describe(statements, LibRedRun));
    }

    // The words of ACE's own refusal, which the comparisons above reduce to a bare refusal: a GUID column refuses bytes
    // too few for a GUID as too small for the field and a number of another type as a type mismatch, and a Yes/No
    // column's DEFAULT is refused when it is declared with an overflow, or a type mismatch for one beyond a Long.
    [Theory]
    [InlineData("CREATE TABLE T (ID LONG, V GUID)", "INSERT INTO T VALUES (1, 1.5)", "field is too small")]
    [InlineData("CREATE TABLE T (ID LONG, V GUID)", "INSERT INTO T VALUES (1, CInt(1))", "Data type mismatch")]
    [InlineData("CREATE TABLE T (ID LONG, V GUID)", "INSERT INTO T VALUES (1, True)", "Data type mismatch")]
    [InlineData("CREATE TABLE T (ID LONG, V GUID)", "INSERT INTO T VALUES (1, CSng(1.5))", "Data type mismatch")]
    [InlineData("CREATE TABLE T (ID LONG, V GUID)", "INSERT INTO T VALUES (1, CDbl(1.5))", "Data type mismatch")]
    [InlineData("CREATE TABLE T (ID LONG, V GUID)", "INSERT INTO T VALUES (1, CCur(1))", "Data type mismatch")]
    [InlineData("CREATE TABLE T (ID LONG)", "ALTER TABLE T ADD COLUMN V YESNO DEFAULT 32768", "Overflow")]
    [InlineData("CREATE TABLE T (ID LONG)", "ALTER TABLE T ADD COLUMN V YESNO DEFAULT -32769", "Overflow")]
    [InlineData("CREATE TABLE T (ID LONG)", "ALTER TABLE T ADD COLUMN V YESNO DEFAULT 70000", "Overflow")]
    [InlineData("CREATE TABLE T (ID LONG)", "ALTER TABLE T ADD COLUMN V YESNO DEFAULT CLng(70000)", "Overflow")]
    [InlineData("CREATE TABLE T (ID LONG)", "ALTER TABLE T ADD COLUMN V YESNO DEFAULT CDbl(70000)", "Overflow")]
    [InlineData("CREATE TABLE T (ID LONG)", "ALTER TABLE T ADD COLUMN V YESNO DEFAULT #1/1/2000#", "Overflow")]
    [InlineData("CREATE TABLE T (ID LONG)", "ALTER TABLE T ADD COLUMN V YESNO DEFAULT Now()", "Overflow")]
    [InlineData("CREATE TABLE T (ID LONG)", "ALTER TABLE T ADD COLUMN V YESNO DEFAULT 3000000000", "Type mismatch")]
    [InlineData("CREATE TABLE T (ID LONG, V YESNO)", "ALTER TABLE T ALTER COLUMN V SET DEFAULT 32768", "Overflow")]
    [InlineData("CREATE TABLE T (ID LONG, V YESNO)", "ALTER TABLE T ALTER COLUMN V SET DEFAULT 3000000000", "Type mismatch")]
    [InlineData("CREATE TABLE T (ID LONG)", "ALTER TABLE T ADD COLUMN V GUID DEFAULT CCur(1)", "Type mismatch")]
    [InlineData("CREATE TABLE T (ID LONG)", "ALTER TABLE T ADD COLUMN V GUID DEFAULT CDbl(1.5)", "Type mismatch")]
    [InlineData("CREATE TABLE T (ID LONG)", "ALTER TABLE T ADD COLUMN V GUID DEFAULT True", "Type mismatch")]
    [InlineData("CREATE TABLE T (ID LONG, V GUID)", "ALTER TABLE T ALTER COLUMN V SET DEFAULT CCur(1)", "Type mismatch")]
    [InlineData("CREATE TABLE T (ID LONG, V YESNO)", "ALTER TABLE T ALTER COLUMN V SET DEFAULT -32769", "Overflow")]
    [InlineData("CREATE TABLE T (ID LONG, V YESNO)", "ALTER TABLE T ALTER COLUMN V SET DEFAULT 70000", "Overflow")]
    [InlineData("CREATE TABLE T (ID LONG, V YESNO)", "ALTER TABLE T ALTER COLUMN V SET DEFAULT CLng(70000)", "Overflow")]
    [InlineData("CREATE TABLE T (ID LONG, V YESNO)", "ALTER TABLE T ALTER COLUMN V SET DEFAULT CDbl(70000)", "Overflow")]
    [InlineData("CREATE TABLE T (ID LONG, V YESNO)", "ALTER TABLE T ALTER COLUMN V SET DEFAULT #1/1/2000#", "Overflow")]
    [InlineData("CREATE TABLE T (ID LONG, V YESNO)", "ALTER TABLE T ALTER COLUMN V SET DEFAULT Now()", "Overflow")]
    [InlineData("CREATE TABLE T (ID LONG, V GUID)", "ALTER TABLE T ALTER COLUMN V SET DEFAULT CDbl(1.5)", "Type mismatch")]
    [InlineData("CREATE TABLE T (ID LONG, V GUID)", "ALTER TABLE T ALTER COLUMN V SET DEFAULT True", "Type mismatch")]
    [InlineData("CREATE TABLE S (ID LONG)", "CREATE TABLE T (ID LONG, V YESNO DEFAULT 32768)", "Overflow")]
    [InlineData("CREATE TABLE S (ID LONG)", "CREATE TABLE T (ID LONG, V YESNO DEFAULT -32769)", "Overflow")]
    [InlineData("CREATE TABLE S (ID LONG)", "CREATE TABLE T (ID LONG, V YESNO DEFAULT 70000)", "Overflow")]
    [InlineData("CREATE TABLE S (ID LONG)", "CREATE TABLE T (ID LONG, V YESNO DEFAULT CLng(70000))", "Overflow")]
    [InlineData("CREATE TABLE S (ID LONG)", "CREATE TABLE T (ID LONG, V YESNO DEFAULT CDbl(70000))", "Overflow")]
    [InlineData("CREATE TABLE S (ID LONG)", "CREATE TABLE T (ID LONG, V YESNO DEFAULT #1/1/2000#)", "Overflow")]
    [InlineData("CREATE TABLE S (ID LONG)", "CREATE TABLE T (ID LONG, V YESNO DEFAULT Now())", "Overflow")]
    [InlineData("CREATE TABLE S (ID LONG)", "CREATE TABLE T (ID LONG, V YESNO DEFAULT 3000000000)", "Type mismatch")]
    [InlineData("CREATE TABLE S (ID LONG)", "CREATE TABLE T (ID LONG, V GUID DEFAULT CCur(1))", "Type mismatch")]
    [InlineData("CREATE TABLE S (ID LONG)", "CREATE TABLE T (ID LONG, V GUID DEFAULT CDbl(1.5))", "Type mismatch")]
    [InlineData("CREATE TABLE S (ID LONG)", "CREATE TABLE T (ID LONG, V GUID DEFAULT True)", "Type mismatch")]
    public void Ace_words_a_refused_GUID_value_and_a_refused_YesNo_or_GUID_DEFAULT_as_the_spec_states(string setup, string sql, string words)
    {
        string path = TemporaryDatabase.CopyPath(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "guidwords-");
        try
        {
            using OleDbConnection connection = AceTestDatabase.Open(path);
            using (OleDbCommand command = connection.CreateCommand())
            {
                command.CommandText = setup;
                command.ExecuteNonQuery();
            }
            using OleDbCommand refusedCommand = connection.CreateCommand();
            refusedCommand.CommandText = sql;
            OleDbException refused = Assert.Throws<OleDbException>(() => refusedCommand.ExecuteNonQuery());
            Assert.Contains(words, refused.Message, StringComparison.Ordinal);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // A BIGINT's bytes are too few for a GUID, and ACE words the refusal so.
    [Fact]
    public void Ace_words_a_BIGINT_selected_into_a_GUID_column_as_too_small_for_the_field()
    {
        Assert.SkipUnless(
            AceTestDatabase.SupportsColumnType(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "BIGINT"),
            AceTestDatabase.UnsupportedColumnTypeReason("BIGINT"));

        string path = TemporaryDatabase.CopyPath(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "guidwords-");
        try
        {
            using OleDbConnection connection = AceTestDatabase.Open(path);
            foreach (string setup in (string[])["CREATE TABLE S (ID LONG, X BIGINT)", "INSERT INTO S VALUES (1, 1)", "CREATE TABLE T (ID LONG, V GUID)"])
            {
                using OleDbCommand command = connection.CreateCommand();
                command.CommandText = setup;
                command.ExecuteNonQuery();
            }
            using OleDbCommand refusedCommand = connection.CreateCommand();
            refusedCommand.CommandText = "INSERT INTO T (ID, V) SELECT ID, X FROM S";
            OleDbException refused = Assert.Throws<OleDbException>(() => refusedCommand.ExecuteNonQuery());
            Assert.Contains("field is too small", refused.Message, StringComparison.Ordinal);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    /// <summary>Each statement's outcome through ACE — the rows an INSERT, UPDATE or DELETE affected, "ok" for any other,
    /// or "refused" — then table T as ACE reads it.</summary>
    private static string AceRun(string path, string[] statements)
    {
        var outcome = new List<string>();
        using OleDbConnection connection = AceTestDatabase.Open(path);
        foreach (string s in statements)
        {
            using OleDbCommand command = connection.CreateCommand();
            command.CommandText = s;
            try { outcome.Add(Affected(s, command.ExecuteNonQuery())); }
            catch (OleDbException) { outcome.Add("refused"); }
        }
        using OleDbCommand read = connection.CreateCommand();
        read.CommandText = "SELECT * FROM T ORDER BY ID";
        try
        {
            using OleDbDataReader reader = read.ExecuteReader();
            var rows = new List<object?[]>();
            while (reader.Read())
                rows.Add([.. Enumerable.Range(0, reader.FieldCount).Select(reader.GetValue)]);
            outcome.Add(Rows(rows));
        }
        catch (OleDbException) { outcome.Add("refused"); }
        return string.Join(" | ", outcome);
    }

    /// <summary>The same through LibRed's engine. A refusal is one of the exceptions LibRed refuses a statement with; any
    /// other is a fault, and fails the test.</summary>
    private static string LibRedRun(string path, string[] statements)
    {
        var outcome = new List<string>();
        using var database = JetDatabase.Open(path, readOnly: false);
        var engine = new QueryEngine(database);
        foreach (string s in statements)
        {
            try { outcome.Add(Affected(s, engine.ExecuteNonQuery(s))); }
            catch (Exception e) when (IsLibRedRefusal(e)) { outcome.Add("refused"); }
        }
        try { outcome.Add(Rows(engine.ExecuteQuery("SELECT * FROM T ORDER BY ID").Rows)); }
        catch (Exception e) when (IsLibRedRefusal(e)) { outcome.Add("refused"); }
        return string.Join(" | ", outcome);
    }

    private static bool IsLibRedRefusal(Exception e) =>
        e is InvalidOperationException or ArgumentException or InvalidCastException or FormatException or OverflowException
            or DivideByZeroException or NotSupportedException or LibRed.Sql.Binding.SqlBindException or LibRed.Sql.Parsing.SqlParseException;

    private static string Describe(string[] statements, Func<string, string[], string> run)
    {
        string path = TemporaryDatabase.CopyPath(
            Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "binguid-store-");
        try { return run(path, statements); }
        finally { TemporaryDatabase.Delete(path); }
    }

    private static string Affected(string statement, int rows) =>
        statement.StartsWith("INSERT", StringComparison.Ordinal) || statement.StartsWith("UPDATE", StringComparison.Ordinal)
            || statement.StartsWith("DELETE", StringComparison.Ordinal)
            ? rows.ToString(CultureInfo.InvariantCulture) : "ok";

    private static string Rows(IEnumerable<object?[]> rows) =>
        string.Join(" ", rows.Select(row => string.Join(",", row.Select(Format))));

    private static string Format(object? value) => value switch
    {
        null or DBNull => "NULL",
        byte[] bytes => "Byte[]:" + Convert.ToHexString(bytes),
        // By value, not scale: the two readers scale a CURRENCY zero differently whatever wrote it.
        decimal m => "Decimal:" + m.ToString("G29", CultureInfo.InvariantCulture),
        DateTime date => "DateTime:" + date.ToString("o", CultureInfo.InvariantCulture),
        _ => $"{value.GetType().Name}:{Convert.ToString(value, CultureInfo.InvariantCulture)}",
    };
}
