using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Globalization;
using LibRed.Data;
using Xunit;

namespace LibRed.Engine.Tests;

/// <summary>
/// A parameter reaches the engine converted to the DbType it is declared with, and LibRed's ADO.NET provider must hand
/// its engine what OLE DB hands ACE: a Boolean under a numeric DbType is 1, not -1, and under a text one "True"; the
/// DbTypes OLE DB will not carry a Boolean as are refused, and Decimal and the integer types Access has no column for
/// arrive as the text "1" or "0". An undeclared Boolean is the Int16 -1 or 0, and an undeclared Null is Null.
/// </summary>
/// <remarks>
/// Each case runs the same statements through ACE (OLE DB) and through LibRed (ADO) on copies of Northwind, the
/// parameter bound to the statements under test: every statement's outcome — the rows it affected, or its refusal; the
/// two engines word a refusal differently — and then table T, or the rows the query selects, as each engine's own
/// reader reads them must agree.
/// </remarks>
[Collection(AceCollection.Name)]
public class ParameterDbTypeAccessTests : TempDatabaseTest
{
    private static readonly string[] Types = ["BYTE", "SHORT", "LONG", "SINGLE", "DOUBLE", "CURRENCY", "DECIMAL(18,2)", "TEXT(20)", "MEMO", "DATETIME"];

    private static readonly DbType[] Numbers = [DbType.Byte, DbType.SByte, DbType.Int16, DbType.UInt16, DbType.Int32, DbType.UInt32,
        DbType.UInt64, DbType.Single, DbType.Double, DbType.Decimal, DbType.Currency];

    private static readonly DbType[] Texts = [DbType.String, DbType.AnsiString, DbType.StringFixedLength, DbType.AnsiStringFixedLength, DbType.Object];

    // The DbTypes OLE DB refuses a Boolean under, and those it carries one as text under.
    private static readonly DbType[] Refused = [DbType.Binary, DbType.Date, DbType.DateTime, DbType.DateTime2, DbType.DateTimeOffset, DbType.Guid,
        DbType.Int64, DbType.Time, DbType.VarNumeric, DbType.Xml];

    private static readonly DbType[] AsText = [DbType.Decimal, DbType.SByte, DbType.UInt16, DbType.UInt32, DbType.UInt64];

    public static TheoryData<string, bool, object?, DbType?> Written
    {
        get
        {
            var data = new TheoryData<string, bool, object?, DbType?>();
            foreach (bool value in (bool[])[true, false])
            {
                // Undeclared or declared Boolean, into every column type, indexed or not.
                foreach (string type in Types)
                    foreach (bool indexed in (bool[])[false, true])
                    {
                        data.Add(type, indexed, value, null);
                        data.Add(type, indexed, value, DbType.Boolean);
                    }
                // Under every numeric and text DbType.
                foreach (string type in (string[])["SHORT", "LONG", "DOUBLE", "TEXT(20)"])
                    foreach (DbType declared in (DbType[])[.. Numbers, .. Texts])
                        data.Add(type, false, value, declared);
                // Under the DbTypes OLE DB refuses or carries as text.
                foreach (string type in (string[])["SHORT", "TEXT(20)"])
                    foreach (DbType declared in Refused)
                        data.Add(type, false, value, declared);
                foreach (string type in (string[])["DATETIME", "BINARY(4)", "GUID"])
                    foreach (DbType declared in (DbType[])[.. Refused, .. AsText])
                        data.Add(type, false, value, declared);
            }
            // An undeclared value of each kind into a binary or GUID column: its own bytes, or a GUID's first four.
            foreach (string type in (string[])["BINARY(4)", "BINARY(8)", "VARBINARY(8)", "LONGBINARY"])
                foreach (object value in (object[])[true, false, (short)-1, 1, (byte)1, 1f, 1d, "1"])
                    data.Add(type, false, value, null);
            foreach (object value in (object[])[true, (short)-1, 1, (byte)1, 1f, 1d, "1", "True", new Guid("00000001-0002-0003-0004-000000000005")])
                data.Add("GUID", false, value, null);
            // Text holding a whole number past a Double's 2^53, into a Large Number or a Decimal wide enough: read exactly.
            foreach (string type in (string[])["BIGINT", "DECIMAL(20,0)"])
                foreach (string value in (string[])["9007199254740993", "9223372036854775807", "-9223372036854775808",
                             "2.5", "2.5000000000000001", "0.5000000000000000000000000001", "9007199254740993.4"])
                    data.Add(type, false, value, null);
            return data;
        }
    }

    // Written by INSERT ... VALUES, and by UPDATE onto a row holding no value; into a number, text or date column, which
    // rows read as True, so an index's key agrees with the row.
    [Theory]
    [MemberData(nameof(Written))]
    public void A_parameter_written_under_a_DbType_is_stored_as_ACE_stores_it(string type, bool indexed, object? value, DbType? declared)
    {
        if (type == "BIGINT")
            Assert.SkipUnless(
                AceTestDatabase.SupportsColumnType(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "BIGINT"),
                AceTestDatabase.UnsupportedColumnTypeReason("BIGINT"));

        string[] setup = [$"CREATE TABLE T (ID LONG PRIMARY KEY, V {type})", .. indexed ? (string[])["CREATE INDEX IX_T ON T (V)"] : [],
            "INSERT INTO T (ID) VALUES (2)"];
        string[] bound = ["INSERT INTO T (ID, V) VALUES (1, @p)", "UPDATE T SET V = @p WHERE ID = 2", "UPDATE T SET V = @p WHERE ID = 1"];
        string[] reads = ["SELECT * FROM T ORDER BY ID", .. Types.Contains(type) ? (string[])["SELECT COUNT(*) FROM T WHERE V = True"] : []];
        Assert.Equal(Describe(setup, bound, reads, value, declared, AceWrite), Describe(setup, bound, reads, value, declared, LibRedWrite));
    }

    // A Null into a Yes/No column, declared or not, is False — a required column's too, by INSERT and by UPDATE of a
    // row holding True.
    [Theory]
    [InlineData("YESNO", false)]
    [InlineData("YESNO", true)]
    [InlineData("YESNO NOT NULL", false)]
    [InlineData("YESNO NOT NULL", true)]
    public void A_Null_parameter_written_to_a_YesNo_column_is_stored_as_ACE_stores_it(string type, bool declaredBoolean)
    {
        string[] setup = [$"CREATE TABLE T (ID LONG PRIMARY KEY, V {type})", "INSERT INTO T (ID, V) VALUES (2, True)"];
        string[] bound = ["INSERT INTO T (ID, V) VALUES (1, @p)", "UPDATE T SET V = @p WHERE ID = 2"];
        string[] reads = ["SELECT * FROM T ORDER BY ID", "SELECT COUNT(*) FROM T WHERE V IS NULL"];
        DbType? declared = declaredBoolean ? DbType.Boolean : null;
        Assert.Equal(Describe(setup, bound, reads, null, declared, AceWrite), Describe(setup, bound, reads, null, declared, LibRedWrite));
    }

    public static TheoryData<string, bool> TypesIndexedOrNot => new()
    {
        { "BYTE", false }, { "BYTE", true }, { "SHORT", false }, { "SHORT", true }, { "LONG", false }, { "LONG", true },
        { "SINGLE", false }, { "SINGLE", true }, { "DOUBLE", false }, { "DOUBLE", true }, { "CURRENCY", false }, { "CURRENCY", true },
        { "DECIMAL(18,2)", false }, { "DECIMAL(18,2)", true }, { "TEXT(20)", false }, { "TEXT(20)", true }, { "MEMO", false },
        { "MEMO", true }, { "DATETIME", false }, { "DATETIME", true },
    };

    // An INSERT ... SELECT of a parameter: the Boolean is the Int16 -1 or 0 there too.
    [Theory]
    [MemberData(nameof(TypesIndexedOrNot))]
    public void A_Boolean_parameter_selected_into_a_column_is_stored_as_ACE_stores_it(string type, bool indexed)
    {
        string[] setup =
        [
            "CREATE TABLE S (ID LONG PRIMARY KEY)", "INSERT INTO S VALUES (1)", "INSERT INTO S VALUES (2)",
            $"CREATE TABLE T (ID LONG PRIMARY KEY, V {type})", .. indexed ? (string[])["CREATE INDEX IX_T ON T (V)"] : [],
        ];
        string[] bound = ["INSERT INTO T (ID, V) SELECT ID, @p FROM S"];
        string[] reads = ["SELECT * FROM T ORDER BY ID", "SELECT COUNT(*) FROM T WHERE V = True"];
        Assert.Equal(Describe(setup, bound, reads, true, null, AceWrite), Describe(setup, bound, reads, true, null, LibRedWrite));
    }

    public static TheoryData<string, bool, object?, DbType?> Compared
    {
        get
        {
            var data = new TheoryData<string, bool, object?, DbType?>();
            foreach (bool indexed in (bool[])[false, true])
                foreach (bool value in (bool[])[true, false])
                {
                    // Undeclared, the Boolean is YesNoComparisonAccessTests'.
                    foreach (string type in Types)
                        data.Add(type, indexed, value, DbType.Boolean);
                    // A numeric DbType finds the row holding 1, not the one holding -1; a text one the text "True".
                    foreach (DbType declared in Numbers)
                    {
                        data.Add("SHORT", indexed, value, declared);
                        data.Add("TEXT(20)", indexed, value, declared);
                    }
                    foreach (DbType declared in Texts)
                        data.Add("TEXT(20)", indexed, value, declared);
                    // Text against a number, date or BIGINT column.
                    foreach (string type in (string[])["BYTE", "SHORT", "LONG", "SINGLE", "DOUBLE", "CURRENCY", "DECIMAL(18,2)", "DATETIME", "BIGINT"])
                        foreach (DbType declared in Texts)
                            data.Add(type, indexed, value, declared);
                    foreach (string type in (string[])["DATETIME", "BIGINT", "BINARY(4)", "GUID"])
                        foreach (DbType declared in AsText)
                            data.Add(type, indexed, value, declared);
                    // Under the DbTypes OLE DB refuses.
                    foreach (string type in (string[])["SHORT", "TEXT(20)", "DATETIME", "BINARY(4)", "GUID"])
                        foreach (DbType declared in Refused)
                            data.Add(type, indexed, value, declared);
                }
            // A Currency parameter against a binary column holding Currency 1 and the text '1' (undeclared, MixedKindComparisonAccessTests').
            data.Add("BINARY(8)", false, 1m, DbType.Currency);
            // A Byte parameter, undeclared and declared, against a binary or GUID column, through an index and not.
            foreach (string type in (string[])["VARBINARY(8)", "BINARY(4)", "GUID"])
                foreach (bool indexed in (bool[])[false, true])
                {
                    data.Add(type, indexed, (byte)1, null);
                    data.Add(type, indexed, (byte)1, DbType.Byte);
                }
            // And the other numbers a parameter can be, against a binary column, the Int64 through an index too.
            foreach (object value in (object[])[(short)1, 1, 1L, 1f, 1d, true, false])
                data.Add("VARBINARY(8)", false, value, null);
            data.Add("VARBINARY(8)", true, 1L, null);
            // Text against a Decimal holding 2^53 and 2^53 + 1, through an index and not: the text is read exactly.
            foreach (bool indexed in (bool[])[false, true])
                foreach (string value in (string[])["9007199254740993", "9007199254740992", "9223372036854775807"])
                {
                    data.Add("DECIMAL(20,0)", indexed, value, null);
                    data.Add("BIGINT", indexed, value, null);
                }
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Compared))]
    public void A_parameter_compared_under_a_DbType_selects_the_rows_ACE_selects(string type, bool indexed, object? value, DbType? declared)
    {
        if (type == "BIGINT")
            Assert.SkipUnless(
                AceTestDatabase.SupportsColumnType(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "BIGINT"),
                AceTestDatabase.UnsupportedColumnTypeReason("BIGINT"));

        string[] seed = type switch
        {
            "BYTE" => ["255", "1", "0", "NULL", "2"],
            "TEXT(20)" or "MEMO" => ["'-1'", "'1'", "'0'", "'True'", "'False'", "NULL"],
            "DATETIME" => ["#12/29/1899#", "#12/31/1899#", "#12/30/1899#", "NULL"],
            "BINARY(4)" => ["0x01000000", "0xFFFF0000", "NULL"],
            "VARBINARY(8)" => ["0x01", "0x0100", "0x01000000", "0x31000000", "0x3100", "0x2D003100", "0x30000000", "NULL"],
            "BINARY(8)" => ["0x1027000000000000", "0x3100000000000000"],
            "GUID" => ["{guid {00000001-0000-0000-0000-000000000000}}", "{guid {0000FFFF-0000-0000-0000-000000000000}}", "NULL"],
            "DECIMAL(20,0)" or "BIGINT" => ["9007199254740992", "9007199254740993", "9223372036854775807", "-9223372036854775808", "NULL"],
            _ => ["-1", "1", "0", "NULL", "2"],
        };
        string[] setup =
        [
            $"CREATE TABLE T (ID LONG PRIMARY KEY, V {type})", .. indexed ? (string[])["CREATE INDEX IX_T ON T (V)"] : [],
            .. seed.Select((v, i) => $"INSERT INTO T (ID, V) VALUES ({i + 1}, {v})"),
        ];
        string[] queries = ["SELECT ID FROM T WHERE V = @p ORDER BY ID"];
        Assert.Equal(Describe(setup, [], queries, value, declared, AceQuery), Describe(setup, [], queries, value, declared, LibRedQuery));
    }

    public static TheoryData<string, bool, DbType> StoredThenCompared
    {
        get
        {
            var data = new TheoryData<string, bool, DbType>();
            foreach (string type in (string[])["SHORT", "TEXT(20)", "DATETIME", "BINARY(4)", "GUID"])
                foreach (DbType declared in (DbType[])[.. Refused, .. AsText])
                    foreach (bool value in (bool[])[true, false])
                        data.Add(type, value, declared);
            return data;
        }
    }

    // A Boolean stored under a DbType OLE DB refuses or carries as text, then compared under it: the row it stored, if
    // it stored one, is the row it finds.
    [Theory]
    [MemberData(nameof(StoredThenCompared))]
    public void A_parameter_stored_under_a_DbType_finds_the_row_it_stored_as_ACE_finds_it(string type, bool value, DbType declared)
    {
        string[] setup = [$"CREATE TABLE T (ID LONG PRIMARY KEY, V {type})"];
        string[] bound = ["INSERT INTO T (ID, V) VALUES (1, @p)"];
        string[] queries = ["SELECT V FROM T", "SELECT COUNT(*) FROM T WHERE V = @p"];
        Assert.Equal(Describe(setup, bound, queries, value, declared, AceQuery), Describe(setup, bound, queries, value, declared, LibRedQuery));
    }

    /// <summary>The setup's outcomes through ACE, then each bound statement's, then what the reads read, as ACE's reader
    /// reads it. An outcome is the rows an INSERT, UPDATE or DELETE affected, "ok" for any other statement, or
    /// "refused" — by ACE, or by OLE DB, which refuses some DbTypes a Boolean is declared as before ACE sees it.</summary>
    private static string AceWrite(string path, string[] setup, string[] bound, string[] reads, object? value, DbType? declared)
    {
        using OleDbConnection connection = AceTestDatabase.Open(path);
        var outcome = AceSetup(connection, setup);
        foreach (string s in bound)
        {
            using OleDbCommand command = connection.CreateCommand();
            command.CommandText = s;
            try
            {
                command.Parameters.Add(AceParameter(value, declared));
                outcome.Add(Affected(s, command.ExecuteNonQuery()));
            }
            catch (Exception e) when (IsAceRefusal(e)) { outcome.Add("refused"); }
        }
        foreach (string query in reads)
        {
            using OleDbCommand command = connection.CreateCommand();
            command.CommandText = query;
            outcome.Add(AceRows(command));
        }
        return string.Join(" | ", outcome);
    }

    /// <summary>The setup's outcomes through ACE, then each bound statement's, then the rows each query selects with the
    /// parameter bound.</summary>
    private static string AceQuery(string path, string[] setup, string[] bound, string[] queries, object? value, DbType? declared)
    {
        using OleDbConnection connection = AceTestDatabase.Open(path);
        var outcome = AceSetup(connection, setup);
        foreach (string s in bound)
        {
            using OleDbCommand command = connection.CreateCommand();
            command.CommandText = s;
            try
            {
                command.Parameters.Add(AceParameter(value, declared));
                outcome.Add(Affected(s, command.ExecuteNonQuery()));
            }
            catch (Exception e) when (IsAceRefusal(e)) { outcome.Add("refused"); }
        }
        foreach (string query in queries)
        {
            using OleDbCommand command = connection.CreateCommand();
            command.CommandText = query;
            try
            {
                command.Parameters.Add(AceParameter(value, declared));
                using OleDbDataReader reader = command.ExecuteReader();
                outcome.Add(Rows(reader));
            }
            catch (Exception e) when (IsAceRefusal(e)) { outcome.Add("refused"); }
        }
        return string.Join(" | ", outcome);
    }

    // OLE DB refuses a value it cannot convert to the declared DbType itself: an InvalidCastException or an
    // InvalidOperationException when the command binds it, an ArgumentException for a DbType it has no OleDbType for.
    private static bool IsAceRefusal(Exception e) => e is OleDbException or InvalidCastException or InvalidOperationException or ArgumentException;

    private static List<string> AceSetup(OleDbConnection connection, string[] setup)
    {
        var outcome = new List<string>();
        foreach (string s in setup)
        {
            using OleDbCommand command = connection.CreateCommand();
            command.CommandText = s;
            try { outcome.Add(Affected(s, command.ExecuteNonQuery())); }
            catch (OleDbException) { outcome.Add("refused"); }
        }
        return outcome;
    }

    // ACE's OLE DB provider carries no value in a BigInt parameter (the engine is not at fault), so an undeclared Int64
    // goes as Numeric, as JetLongTypeMapping sends it.
    private static OleDbParameter AceParameter(object? value, DbType? declared)
    {
        var parameter = new OleDbParameter { ParameterName = "@p", Value = value ?? DBNull.Value };
        if (declared is { } type) parameter.DbType = type;
        else if (value is long) parameter.OleDbType = OleDbType.Numeric;
        return parameter;
    }

    private static string AceRows(OleDbCommand command)
    {
        try
        {
            using OleDbDataReader reader = command.ExecuteReader();
            return Rows(reader);
        }
        catch (OleDbException) { return "refused"; }
    }

    /// <summary>The same as <see cref="AceWrite"/> through LibRed's ADO.NET provider. A refusal is one of the exceptions
    /// LibRed refuses a statement with; any other is a fault, and fails the test.</summary>
    private static string LibRedWrite(string path, string[] setup, string[] bound, string[] reads, object? value, DbType? declared)
    {
        using var connection = new LibRedConnection($"Data Source={path}");
        connection.Open();
        var outcome = LibRedSetup(connection, setup);
        foreach (string s in bound)
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = s;
            try
            {
                command.Parameters.Add(LibRedParameter(value, declared));
                outcome.Add(Affected(s, command.ExecuteNonQuery()));
            }
            catch (Exception e) when (IsLibRedRefusal(e)) { outcome.Add("refused"); }
        }
        foreach (string query in reads)
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = query;
            outcome.Add(LibRedRows(command));
        }
        return string.Join(" | ", outcome);
    }

    private static string LibRedQuery(string path, string[] setup, string[] bound, string[] queries, object? value, DbType? declared)
    {
        using var connection = new LibRedConnection($"Data Source={path}");
        connection.Open();
        var outcome = LibRedSetup(connection, setup);
        foreach (string s in bound)
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = s;
            try
            {
                command.Parameters.Add(LibRedParameter(value, declared));
                outcome.Add(Affected(s, command.ExecuteNonQuery()));
            }
            catch (Exception e) when (IsLibRedRefusal(e)) { outcome.Add("refused"); }
        }
        foreach (string query in queries)
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = query;
            try
            {
                command.Parameters.Add(LibRedParameter(value, declared));
                using DbDataReader reader = command.ExecuteReader();
                outcome.Add(Rows(reader));
            }
            catch (Exception e) when (IsLibRedRefusal(e)) { outcome.Add("refused"); }
        }
        return string.Join(" | ", outcome);
    }

    private static List<string> LibRedSetup(LibRedConnection connection, string[] setup)
    {
        var outcome = new List<string>();
        foreach (string s in setup)
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = s;
            try { outcome.Add(Affected(s, command.ExecuteNonQuery())); }
            catch (Exception e) when (IsLibRedRefusal(e)) { outcome.Add("refused"); }
        }
        return outcome;
    }

    private static LibRedParameter LibRedParameter(object? value, DbType? declared)
    {
        var parameter = new LibRedParameter { ParameterName = "@p", Value = value ?? DBNull.Value };
        if (declared is { } type) parameter.DbType = type;
        return parameter;
    }

    private static string LibRedRows(DbCommand command)
    {
        try
        {
            using DbDataReader reader = command.ExecuteReader();
            return Rows(reader);
        }
        catch (Exception e) when (IsLibRedRefusal(e)) { return "refused"; }
    }

    private static bool IsLibRedRefusal(Exception e) =>
        e is DbException or InvalidOperationException or ArgumentException or InvalidCastException or FormatException or OverflowException
            or DivideByZeroException or NotSupportedException or LibRed.Sql.Binding.SqlBindException or LibRed.Sql.Parsing.SqlParseException;

    private static string Describe(string[] setup, string[] bound, string[] reads, object? value, DbType? declared,
        Func<string, string[], string[], string[], object?, DbType?, string> run)
    {
        string path = TemporaryDatabase.CopyPath(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "dbtype-");
        try { return run(path, setup, bound, reads, value, declared); }
        finally
        {
            OleDbConnection.ReleaseObjectPool();
            TemporaryDatabase.Delete(path);
        }
    }

    private static string Affected(string statement, int rows) =>
        statement.StartsWith("INSERT", StringComparison.Ordinal) || statement.StartsWith("UPDATE", StringComparison.Ordinal)
            || statement.StartsWith("DELETE", StringComparison.Ordinal)
            ? rows.ToString(CultureInfo.InvariantCulture) : "ok";

    private static string Rows(DbDataReader reader)
    {
        var rows = new List<string>();
        while (reader.Read())
            rows.Add(string.Join(",", Enumerable.Range(0, reader.FieldCount).Select(i => Format(reader.GetValue(i)))));
        return string.Join(" ", rows);
    }

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
