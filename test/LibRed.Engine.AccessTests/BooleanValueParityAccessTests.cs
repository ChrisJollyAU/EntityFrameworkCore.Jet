using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Globalization;
using System.Text.RegularExpressions;
using LibRed.Data;
using LibRed.Engine.Schema;
using Xunit;

namespace LibRed.Engine.Tests;

// A Boolean meeting a column that is not Yes/No, and a Null meeting one that is. Each test runs one script through
// ACE (OLE DB) and through LibRed (ADO) on copies of the same file and requires the same answer to every step,
// ACE's being the expected one. A refusal matches a refusal; the two engines word theirs differently.
//
// What ACE does, which these hold LibRed to:
//  - a Boolean bound for a column is the Int16 -1 or 0, and from there exactly that number: 255 in a BYTE, -1 in a
//    number, "-1" in text, 1899-12-29 in a date - on INSERT, UPDATE, INSERT...SELECT, UPDATE...JOIN and ALTER COLUMN;
//  - `col = True` tests the column's truth (every non-zero row), with an index or without;
//  - a Boolean parameter under a declared DbType reaches the engine converted to that type (1, not -1; "True");
//  - a Null written to a Yes/No column is False before a required column, an index or a CHECK sees it, and the
//    Columns rowset calls the column not nullable - while a reader's schema calls it nullable.
[Collection(AceCollection.Name)]
public class BooleanValueParityAccessTests
{
    private sealed record P(object Value, DbType? Type);

    private sealed record Step(string Label, Func<DbConnection, string> Run);

    private static readonly (string Name, string Sql)[] Types =
    [
        ("Byte", "BYTE"), ("Short", "SHORT"), ("Long", "LONG"), ("Single", "SINGLE"), ("Double", "DOUBLE"),
        ("Cur", "CURRENCY"), ("Dec", "DECIMAL(18,2)"), ("Text", "TEXT(20)"), ("Memo", "MEMO"), ("Date", "DATETIME"),
    ];

    private static string Err(Exception ex) => "ERR{{" + ex.Message.ReplaceLineEndings(" ") + "}}";

    private static string Key(string outcome) => Regex.Replace(outcome, @"ERR\{\{.*?\}\}", "ERR");

    private static string F(object? v) => v switch
    {
        null or DBNull => "NULL",
        byte[] b => "x" + Convert.ToHexString(b),
        DateTime d => "#" + d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "#",
        // By value, not scale. The two engines' readers scale a CURRENCY zero differently whatever wrote it
        // (measured with a plain DEFAULT 0: ACE reads 0.0000, LibRed 0), which is not what these tests are about.
        decimal m => "Decimal:" + m.ToString("G29", CultureInfo.InvariantCulture),
        IFormattable f => v.GetType().Name + ":" + f.ToString(null, CultureInfo.InvariantCulture),
        _ => v.GetType().Name + ":" + v,
    };

    private static DbCommand Command(DbConnection c, string sql, P? p)
    {
        DbCommand cmd = c.CreateCommand();
        cmd.CommandText = sql;
        if (p is not null)
        {
            DbParameter prm = cmd.CreateParameter();
            prm.ParameterName = "@p";
            prm.Value = p.Value;
            if (p.Type is { } t) prm.DbType = t;
            cmd.Parameters.Add(prm);
        }
        return cmd;
    }

    private static string Exec(DbConnection c, string sql, P? p = null)
    {
        try
        {
            using DbCommand cmd = Command(c, sql, p);
            int n = cmd.ExecuteNonQuery();
            return sql.StartsWith("INSERT", StringComparison.Ordinal) || sql.StartsWith("UPDATE", StringComparison.Ordinal) ? "ok" + n : "ok";
        }
        catch (Exception ex) { return Err(ex); }
    }

    private static string Query(DbConnection c, string sql, P? p = null)
    {
        try
        {
            using DbCommand cmd = Command(c, sql, p);
            using DbDataReader r = cmd.ExecuteReader();
            var rows = new List<string>();
            while (r.Read())
                rows.Add(string.Join(",", Enumerable.Range(0, r.FieldCount).Select(i => F(r.GetValue(i)))));
            return "[" + string.Join(" ", rows) + "]";
        }
        catch (Exception ex) { return Err(ex); }
    }

    // Each column of the Columns rowset: its name, N (nullable) or R (not), and its DBCOLUMNFLAGS.
    private static string Columns(DbConnection c, string table)
    {
        DataTable t = c.GetSchema("Columns", [null, null, table, null]);
        return string.Join(" ", t.Rows.Cast<DataRow>().OrderBy(r => Convert.ToInt64(r["ORDINAL_POSITION"], CultureInfo.InvariantCulture))
            .Select(r => $"{r["COLUMN_NAME"]}:{((bool)r["IS_NULLABLE"] ? "N" : "R")}{r["COLUMN_FLAGS"]}"));
    }

    private static string ReaderSchema(DbConnection c, string sql, CommandBehavior behavior)
    {
        using DbCommand cmd = Command(c, sql, null);
        using DbDataReader r = cmd.ExecuteReader(behavior);
        return string.Join(" ", r.GetSchemaTable()!.Rows.Cast<DataRow>()
            .Select(row => $"{row["ColumnName"]}:{((bool)row["AllowDBNull"] ? "N" : "R")}:{((Type)row["DataType"]).Name}"));
    }

    private static void SameThroughBothEngines(List<Step> script)
    {
        string source = Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb");
        string acePath = TemporaryDatabase.CopyPath(source, "boolparity-ace-");
        string libredPath = TemporaryDatabase.CopyPath(source, "boolparity-libred-");
        try
        {
            string[] ace, libred;
            using (OleDbConnection c = AceTestDatabase.Open(acePath))
                ace = script.Select(s => s.Run(c)).ToArray();
            OleDbConnection.ReleaseObjectPool();
            using (var c = new LibRedConnection($"Data Source={libredPath}"))
            {
                c.Open();
                libred = script.Select(s => s.Run(c)).ToArray();
            }

            var differences = Enumerable.Range(0, script.Count)
                .Where(i => Key(ace[i]) != Key(libred[i]))
                .Select(i => $"{script[i].Label}\n    ACE:    {ace[i]}\n    LibRed: {libred[i]}")
                .ToList();
            Assert.True(differences.Count == 0, $"{differences.Count} of {script.Count} steps differ from ACE:\n" + string.Join("\n", differences));
        }
        finally
        {
            TemporaryDatabase.Delete(acePath);
            TemporaryDatabase.Delete(libredPath);
        }
    }

    private static void Statement(List<Step> script, string sql, P? p = null) =>
        script.Add(new(sql + (p is null ? "" : $"  [@p = {p.Value} as {p.Type?.ToString() ?? "(unset)"}]"), c => Exec(c, sql, p)));

    private static void Rows(List<Step> script, string sql, P? p = null) =>
        script.Add(new(sql + (p is null ? "" : $"  [@p = {p.Value} as {p.Type?.ToString() ?? "(unset)"}]"), c => Query(c, sql, p)));

    private static void YesNoSource(List<Step> script)
    {
        Statement(script, "CREATE TABLE S (ID LONG PRIMARY KEY, Y YESNO)");
        Statement(script, "INSERT INTO S VALUES (1, True)");
        Statement(script, "INSERT INTO S VALUES (2, False)");
    }

    [Fact]
    public void A_Boolean_written_to_a_column_that_is_not_YesNo_is_stored_as_ACE_stores_it()
    {
        var script = new List<Step>();
        YesNoSource(script);
        foreach ((string name, string sql) in Types)
            foreach (bool indexed in (bool[])[false, true])
            {
                string t = (indexed ? "I_" : "U_") + name;
                Statement(script, $"CREATE TABLE {t} (ID LONG PRIMARY KEY, V {sql})");
                if (indexed) Statement(script, $"CREATE INDEX IX_{t} ON {t} (V)");

                void Written(string write, P? p = null, params string[] seed)
                {
                    Statement(script, $"DELETE FROM {t}");
                    foreach (string s in seed) Statement(script, s);
                    Statement(script, write, p);
                    Rows(script, $"SELECT V FROM {t} ORDER BY ID");
                    Rows(script, $"SELECT COUNT(*) FROM {t} WHERE V = True");   // the index key agrees with the row
                }

                foreach (string literal in (string[])["True", "False", "(1=1)", "(1=0)"])
                {
                    Written($"INSERT INTO {t} (ID, V) VALUES (1, {literal})");
                    Written($"UPDATE {t} SET V = {literal}", null, $"INSERT INTO {t} (ID) VALUES (1)");
                }
                foreach (bool value in (bool[])[true, false])
                    foreach (DbType? declared in (DbType?[])[null, DbType.Boolean])
                    {
                        Written($"INSERT INTO {t} (ID, V) VALUES (1, @p)", new P(value, declared));
                        Written($"UPDATE {t} SET V = @p", new P(value, declared), $"INSERT INTO {t} (ID) VALUES (1)");
                    }
                Written($"INSERT INTO {t} (ID, V) SELECT ID, Y FROM S");
                Written($"INSERT INTO {t} (ID, V) SELECT ID, True FROM S");
                Written($"INSERT INTO {t} (ID, V) SELECT ID, (ID=1) FROM S");
                Written($"INSERT INTO {t} (ID, V) SELECT ID, @p FROM S", new P(true, null));
                Written($"UPDATE {t} INNER JOIN S ON {t}.ID = S.ID SET {t}.V = S.Y", null,
                    $"INSERT INTO {t} (ID) VALUES (1)", $"INSERT INTO {t} (ID) VALUES (2)");

                // A Yes/No column retyped: its True is the same -1, and 255 in a BYTE.
                string a = "A" + t;
                Statement(script, $"CREATE TABLE {a} (ID LONG PRIMARY KEY, V YESNO)");
                if (indexed) Statement(script, $"CREATE INDEX IX_{a} ON {a} (V)");
                Statement(script, $"INSERT INTO {a} VALUES (1, True)");
                Statement(script, $"INSERT INTO {a} VALUES (2, False)");
                Statement(script, $"ALTER TABLE {a} ALTER COLUMN V {sql}");
                Rows(script, $"SELECT V FROM {a} ORDER BY ID");
                Rows(script, $"SELECT COUNT(*) FROM {a} WHERE V = True");
            }

        SameThroughBothEngines(script);
    }

    [Fact]
    public void A_Boolean_compared_with_a_column_selects_the_rows_ACE_selects_with_an_index_or_without()
    {
        var script = new List<Step>();
        foreach ((string name, string sql) in Types)
            foreach (bool indexed in (bool[])[false, true])
            {
                string t = (indexed ? "I_" : "U_") + name;
                Statement(script, $"CREATE TABLE {t} (ID LONG PRIMARY KEY, V {sql})");
                if (indexed) Statement(script, $"CREATE INDEX IX_{t} ON {t} (V)");
                bool text = name is "Text" or "Memo";
                string[] seed = name switch
                {
                    "Byte" => ["255", "1", "0", "NULL", "2"],
                    "Text" or "Memo" => ["'-1'", "'1'", "'0'", "'True'", "'False'", "NULL"],
                    "Date" => ["#12/29/1899#", "#12/31/1899#", "#12/30/1899#", "NULL"],
                    _ => ["-1", "1", "0", "NULL", "2"],
                };
                for (int i = 0; i < seed.Length; i++)
                    Statement(script, $"INSERT INTO {t} (ID, V) VALUES ({i + 1}, {seed[i]})");

                // `= True` and `<> True` test the column's truth; everything else compares it with -1 or 0.
                string[] conditions = text
                    ? ["V = True", "True = V", "V <> True", "V = False", "V <> False", "V < True", "V IN (True)"]
                    : ["V = True", "True = V", "V <> True", "V = False", "V <> False", "V = (1=1)", "V IN (True)",
                       "V < True", "V > True", "V <= True", "V >= True", "V < False", "V > False", "V <= False", "V >= False"];
                foreach (string condition in conditions)
                    Rows(script, $"SELECT ID FROM {t} WHERE {condition} ORDER BY ID");
                foreach (bool value in (bool[])[true, false])
                    foreach (DbType? declared in (DbType?[])[null, DbType.Boolean])
                        Rows(script, $"SELECT ID FROM {t} WHERE V = @p ORDER BY ID", new P(value, declared));
            }

        SameThroughBothEngines(script);
    }

    [Fact]
    public void A_Boolean_parameter_under_a_declared_DbType_is_the_value_ACE_is_handed()
    {
        DbType[] numbers = [DbType.Byte, DbType.SByte, DbType.Int16, DbType.UInt16, DbType.Int32, DbType.UInt32,
            DbType.UInt64, DbType.Single, DbType.Double, DbType.Decimal, DbType.Currency];
        DbType[] texts = [DbType.String, DbType.AnsiString, DbType.StringFixedLength, DbType.AnsiStringFixedLength, DbType.Object];

        var script = new List<Step>();
        foreach ((string t, string sql) in ((string, string)[])[("PS", "SHORT"), ("PL", "LONG"), ("PD", "DOUBLE"), ("PT", "TEXT(20)")])
        {
            Statement(script, $"CREATE TABLE {t} (ID LONG PRIMARY KEY, V {sql})");
            foreach (DbType declared in (DbType[])[.. numbers, .. texts])
                foreach (bool value in (bool[])[true, false])
                {
                    Statement(script, $"DELETE FROM {t}");
                    Statement(script, $"INSERT INTO {t} (ID, V) VALUES (1, @p)", new P(value, declared));
                    Rows(script, $"SELECT V FROM {t}");
                    Statement(script, $"UPDATE {t} SET V = @p", new P(value, declared));
                    Rows(script, $"SELECT V FROM {t}");
                }
        }

        // Compared: a numeric DbType finds the row holding 1, not the one holding -1; a text one the text "True".
        foreach (bool indexed in (bool[])[false, true])
        {
            string n = indexed ? "QNI" : "QN", s = indexed ? "QTI" : "QT";
            Statement(script, $"CREATE TABLE {n} (ID LONG PRIMARY KEY, V SHORT)");
            Statement(script, $"CREATE TABLE {s} (ID LONG PRIMARY KEY, V TEXT(20))");
            if (indexed) Statement(script, $"CREATE INDEX IX_{n} ON {n} (V)");
            if (indexed) Statement(script, $"CREATE INDEX IX_{s} ON {s} (V)");
            string[] numberSeed = ["-1", "1", "0", "NULL", "2"], textSeed = ["'-1'", "'1'", "'0'", "'True'", "'False'", "NULL"];
            for (int i = 0; i < numberSeed.Length; i++) Statement(script, $"INSERT INTO {n} (ID, V) VALUES ({i + 1}, {numberSeed[i]})");
            for (int i = 0; i < textSeed.Length; i++) Statement(script, $"INSERT INTO {s} (ID, V) VALUES ({i + 1}, {textSeed[i]})");
            foreach (bool value in (bool[])[true, false])
            {
                foreach (DbType declared in numbers)
                {
                    Rows(script, $"SELECT ID FROM {n} WHERE V = @p ORDER BY ID", new P(value, declared));
                    Rows(script, $"SELECT ID FROM {s} WHERE V = @p ORDER BY ID", new P(value, declared));
                }
                foreach (DbType declared in texts)
                    Rows(script, $"SELECT ID FROM {s} WHERE V = @p ORDER BY ID", new P(value, declared));
            }
        }

        SameThroughBothEngines(script);
    }

    [Fact]
    public void A_Null_written_to_a_YesNo_column_is_False_to_a_required_column_an_index_and_a_check()
    {
        var script = new List<Step>();
        Statement(script, "CREATE TABLE NS (ID LONG PRIMARY KEY, N LONG)");
        Statement(script, "INSERT INTO NS VALUES (10, NULL)");
        Statement(script, "INSERT INTO NS VALUES (11, 5)");
        Statement(script, "INSERT INTO NS VALUES (12, 0)");

        foreach ((string t, string declared) in ((string, string)[])[("YN", "YESNO"), ("YR", "YESNO NOT NULL")])
        {
            Statement(script, $"CREATE TABLE {t} (ID LONG PRIMARY KEY, F {declared})");
            void Written(string write, P? p = null, string? seed = null)
            {
                Statement(script, $"DELETE FROM {t}");
                if (seed is not null) Statement(script, seed);
                Statement(script, write, p);
                Rows(script, $"SELECT ID, F FROM {t} ORDER BY ID");
                Rows(script, $"SELECT COUNT(*) FROM {t} WHERE F IS NULL");
            }
            Written($"INSERT INTO {t} (ID, F) VALUES (1, NULL)");
            Written($"INSERT INTO {t} (ID) VALUES (1)");
            Written($"INSERT INTO {t} (ID, F) VALUES (1, @p)", new P(DBNull.Value, null));
            Written($"INSERT INTO {t} (ID, F) VALUES (1, @p)", new P(DBNull.Value, DbType.Boolean));
            Written($"UPDATE {t} SET F = NULL", null, $"INSERT INTO {t} (ID, F) VALUES (1, True)");
            Written($"UPDATE {t} SET F = @p", new P(DBNull.Value, null), $"INSERT INTO {t} (ID, F) VALUES (1, True)");
            Written($"INSERT INTO {t} (ID, F) SELECT ID, N FROM NS");
        }
        Statement(script, "CREATE TABLE AY (ID LONG PRIMARY KEY, F YESNO)");
        Statement(script, "ALTER TABLE AY ALTER COLUMN F YESNO NOT NULL");
        Statement(script, "INSERT INTO AY (ID, F) VALUES (1, NULL)");
        Rows(script, "SELECT ID, F FROM AY");

        // An index holds the False the row stores: a second Null is a duplicate, and IGNORE NULL ignores nothing.
        int u = 0;
        foreach (string with in (string[])["", " WITH IGNORE NULL", " WITH DISALLOW NULL", " WITH PRIMARY"])
            foreach (string[] values in (string[][])[["False", "NULL", null!, "True"], ["NULL", "NULL", "False"]])
            {
                string t = $"UQ{u++}";
                Statement(script, $"CREATE TABLE {t} (ID LONG, F YESNO)");
                Statement(script, $"CREATE UNIQUE INDEX UX_{t} ON {t} (F){with}");
                for (int i = 0; i < values.Length; i++)
                    Statement(script, values[i] is null ? $"INSERT INTO {t} (ID) VALUES ({i + 1})" : $"INSERT INTO {t} (ID, F) VALUES ({i + 1}, {values[i]})");
                Rows(script, $"SELECT ID, F FROM {t} ORDER BY ID");
                Rows(script, $"SELECT ID FROM {t} WHERE F = False ORDER BY ID");
            }
        Statement(script, "CREATE TABLE UQC (ID LONG, F YESNO, T TEXT(10))");
        Statement(script, "CREATE UNIQUE INDEX UX_UQC ON UQC (F, T)");
        Statement(script, "INSERT INTO UQC (ID, F, T) VALUES (1, NULL, NULL)");
        Statement(script, "INSERT INTO UQC (ID, F, T) VALUES (2, NULL, NULL)");
        Statement(script, "INSERT INTO UQC (ID, F, T) VALUES (3, NULL, 'x')");
        Statement(script, "INSERT INTO UQC (ID, F, T) VALUES (4, False, 'x')");
        Rows(script, "SELECT ID, F, T FROM UQC ORDER BY ID");
        Statement(script, "CREATE TABLE IGN (ID LONG, F YESNO)");
        Statement(script, "CREATE INDEX IX_IGN ON IGN (F) WITH IGNORE NULL");
        Statement(script, "INSERT INTO IGN (ID, F) VALUES (1, NULL)");
        Statement(script, "INSERT INTO IGN (ID, F) VALUES (2, True)");
        Rows(script, "SELECT ID FROM IGN WHERE F = False ORDER BY ID");

        int k = 0;
        foreach (string check in (string[])["F IS NOT NULL", "F IS NULL", "F = False", "F = True", "F <> True"])
        {
            string t = $"CK{k++}";
            Statement(script, $"CREATE TABLE {t} (ID LONG PRIMARY KEY, F YESNO, CONSTRAINT c_{t} CHECK ({check}))");
            Statement(script, $"INSERT INTO {t} (ID, F) VALUES (1, NULL)");
            Statement(script, $"INSERT INTO {t} (ID) VALUES (2)");
            Statement(script, $"INSERT INTO {t} (ID, F) VALUES (3, True)");
            Statement(script, $"INSERT INTO {t} (ID, F) VALUES (4, False)");
            Rows(script, $"SELECT ID, F FROM {t} ORDER BY ID");
        }

        SameThroughBothEngines(script);
    }

    // A number stored into a binary column is its own bytes (an Int16 two, so True is FFFF; an Int32 and a Byte
    // four; a Single four; a Double and a date eight), and a whole number stored into a GUID column its first four
    // bytes, where text that is not a GUID is the zero GUID - by INSERT, UPDATE, a DEFAULT and ALTER COLUMN alike.
    [Fact]
    public void A_Boolean_or_number_stored_into_a_binary_or_GUID_column_is_stored_as_ACE_stores_it()
    {
        var script = new List<Step>();
        int n = 0;
        foreach ((string sql, string[] literals, object[] parameters, string[] sources) in ((string, string[], object[], string[])[])
            [
                ("BINARY(4)", ["True", "False", "(1=1)", "CInt(-1)", "1", "-1", "300", "70000", "CLng(1)", "CSng(1)", "CDbl(1)", "1.5", "'1'", "'True'", "#1/1/2000#", "NULL"],
                    [true, false, (short)-1, 1, 1f, 1d, "1"], ["BYTE", "SHORT", "LONG", "SINGLE", "DOUBLE", "YESNO", "DATETIME", "TEXT(10)"]),
                ("BINARY(8)", ["True", "False", "(1=1)", "CInt(-1)", "1", "-1", "300", "70000", "CLng(1)", "CSng(1)", "CDbl(1)", "1.5", "'1'", "'True'", "#1/1/2000#", "NULL"],
                    [true, false, (short)-1, 1, 1f, 1d, "1"], ["BYTE", "SHORT", "LONG", "SINGLE", "DOUBLE", "YESNO", "DATETIME", "TEXT(10)"]),
                ("VARBINARY(8)", ["True", "False", "(1=1)", "CInt(-1)", "1", "-1", "300", "70000", "CLng(1)", "CSng(1)", "CDbl(1)", "1.5", "'1'", "'True'", "#1/1/2000#", "NULL"],
                    [true, false, (short)-1, 1, 1f, 1d, "1"], ["BYTE", "SHORT", "LONG", "SINGLE", "DOUBLE", "YESNO", "DATETIME", "TEXT(10)"]),
                ("LONGBINARY", ["True", "False", "(1=1)", "CInt(-1)", "1", "-1", "300", "CLng(1)", "CSng(1)", "CDbl(1)", "1.5", "'1'", "#1/1/2000#", "NULL"],
                    [true, false, (short)-1, 1, 1f, 1d, "1"], ["BYTE", "SHORT", "LONG", "SINGLE", "DOUBLE", "YESNO", "DATETIME", "TEXT(10)"]),
                ("GUID", ["True", "False", "CInt(-1)", "1", "-1", "300", "70000", "CLng(1)", "CSng(1)", "CDbl(1)", "1.5", "'1'", "'True'", "'{00000001-0002-0003-0004-000000000005}'", "NULL"],
                    [true, (short)-1, 1, (byte)1, 1f, 1d, "1", "True", new Guid("00000001-0002-0003-0004-000000000005")], ["BYTE", "SHORT", "LONG", "SINGLE", "DOUBLE", "YESNO", "TEXT(10)"]),
            ])
        {
            string t = "B" + n++;
            Statement(script, $"CREATE TABLE {t} (ID LONG, V {sql})");
            foreach (string literal in literals)
            {
                Statement(script, $"DELETE FROM {t}");
                Statement(script, $"INSERT INTO {t} (ID, V) VALUES (1, {literal})");
                Rows(script, $"SELECT V FROM {t}");
                Statement(script, $"UPDATE {t} SET V = {literal}");
                Rows(script, $"SELECT V FROM {t}");
            }
            foreach (object parameter in parameters)
            {
                Statement(script, $"DELETE FROM {t}");
                Statement(script, $"INSERT INTO {t} (ID, V) VALUES (1, @p)", new P(parameter, null));
                Rows(script, $"SELECT V FROM {t}");
            }
            foreach (string source in sources)
            {
                string a = "B" + n++;
                string value = source switch { "TEXT(10)" => "'1'", "YESNO" => "True", "DATETIME" => "#1/1/2000#", "SHORT" => "-1", _ => "1" };
                Statement(script, $"CREATE TABLE {a} (ID LONG, V {source})");
                Statement(script, $"INSERT INTO {a} VALUES (1, {value})");
                Statement(script, $"ALTER TABLE {a} ALTER COLUMN V {sql}");
                Rows(script, $"SELECT V FROM {a}");
            }
            foreach (string literal in (string[])["True", "False", "CInt(-1)", "1", "-1", "'1'"])
            {
                string d = "B" + n++;
                Statement(script, $"CREATE TABLE {d} (ID LONG, V {sql} DEFAULT {literal})");
                Statement(script, $"INSERT INTO {d} (ID) VALUES (1)");
                Rows(script, $"SELECT V FROM {d}");
            }
        }

        SameThroughBothEngines(script);
    }

    // A text column beside a number or Boolean the statement computes, rather than writes, compares as text - the
    // number as its text, in the column's collation - and a written number is a type mismatch. A GUID or binary
    // column beside a computed number sorts below it.
    [Fact]
    public void A_text_GUID_or_binary_column_compared_with_a_computed_number_or_Boolean_compares_as_ACE_compares_it()
    {
        var script = new List<Step>();
        int n = 0;
        foreach (string sql in (string[])["TEXT(20)", "MEMO"])
            foreach (bool indexed in (bool[])[false, true])
            {
                string t = "X" + n++;
                Statement(script, $"CREATE TABLE {t} (ID LONG, V {sql})");
                if (indexed) Statement(script, $"CREATE INDEX IX_{t} ON {t} (V)");
                string[] seed = ["'-1'", "'1'", "'0'", "'True'", "'False'", "NULL", "'abc'", "'10'", "'2'", "''"];
                for (int i = 0; i < seed.Length; i++) Statement(script, $"INSERT INTO {t} VALUES ({i + 1}, {seed[i]})");

                foreach (string op in (string[])["=", "<>", "<", ">", "<=", ">="])
                    foreach (string operand in (string[])["True", "False", "-1", "0", "1", "2", "1.5", "(1=1)", "(1=0)", "CInt(True)", "CInt(0)", "CLng(1)", "1+0", "Abs(1)", "CDbl(2)", "CCur(1)"])
                        Rows(script, $"SELECT ID FROM {t} WHERE V {op} {operand} ORDER BY ID");
                foreach (string condition in (string[])["V IN (True)", "V IN (False)", "V IN (1)", "V IN (CInt(1))", "V BETWEEN False AND True",
                    "V BETWEEN CInt(0) AND CInt(2)", "True = V", "(1=1) = V", "1 = V", "CInt(1) = V", "V = V + 0", "V & '' = True"])
                    Rows(script, $"SELECT ID FROM {t} WHERE {condition} ORDER BY ID");
                foreach (object parameter in (object[])[true, false, 1, 0, (short)-1, 1.5, 1m])
                    foreach (string op in (string[])["=", "<", ">"])
                        Rows(script, $"SELECT ID FROM {t} WHERE V {op} @p ORDER BY ID", new P(parameter, null));
            }

        foreach ((string sql, string one, string minus) in ((string, string, string)[])
            [
                ("BINARY(4)", "0x01000000", "0xFFFF0000"), ("VARBINARY(8)", "0x01000000", "0xFFFF0000"),
                ("GUID", "{guid {00000001-0000-0000-0000-000000000000}}", "{guid {0000FFFF-0000-0000-0000-000000000000}}"),
            ])
        {
            string t = "X" + n++;
            Statement(script, $"CREATE TABLE {t} (ID LONG, V {sql})");
            Statement(script, $"INSERT INTO {t} VALUES (1, {one})");
            Statement(script, $"INSERT INTO {t} VALUES (2, {minus})");
            Statement(script, $"INSERT INTO {t} VALUES (3, NULL)");
            foreach (string condition in (string[])["V = 1", "V = -1", "V = True", "V = False", "V <> True", "V = CInt(True)", "V = (1=1)", "V > True", "V >= 1",
                "V IN (True)", "V = '1'", "V <> CInt(True)", "V <> (1=0)", "V < CInt(True)", "V >= (1=1)", "V IN (CInt(True))", "V = CLng(1)", "V = CDbl(1)",
                "NOT (V = CInt(True))", "(V = CInt(True)) IS NULL"])
                Rows(script, $"SELECT ID FROM {t} WHERE {condition} ORDER BY ID");
            foreach (object parameter in (object[])[true, 1, (short)-1])
                foreach (string op in (string[])["=", "<>", "<"])
                    Rows(script, $"SELECT ID FROM {t} WHERE V {op} @p ORDER BY ID", new P(parameter, null));
        }

        SameThroughBothEngines(script);
    }

    // A DEFAULT is converted to its column's type when it is declared, and one the column cannot take refuses the
    // statement there - CREATE TABLE, ADD COLUMN and ALTER COLUMN SET DEFAULT alike - with a Byte's range checked
    // where a write of the same value keeps its low byte.
    [Fact]
    public void A_DEFAULT_its_column_cannot_take_is_refused_when_it_is_declared_as_ACE_refuses_it()
    {
        var script = new List<Step>();
        int n = 0;
        foreach ((string sql, string[] defaults) in ((string, string[])[])
            [
                ("BYTE", ["True", "False", "Yes", "CInt(True)", "-1", "255", "256", "300", "70000", "3000000000", "1.5", "2.5", "'abc'", "'5'", "'300'", "#1/1/2000#", "Now()", "CInt(300)", "1+299", "NULL", "12345.67"]),
                ("SHORT", ["True", "False", "Yes", "CInt(True)", "-1", "255", "256", "300", "70000", "3000000000", "1.5", "2.5", "'abc'", "'5'", "'300'", "#1/1/2000#", "Now()", "CInt(300)", "1+299", "NULL", "12345.67"]),
                ("LONG", ["True", "False", "Yes", "CInt(True)", "-1", "300", "70000", "3000000000", "1.5", "2.5", "'abc'", "'5'", "#1/1/2000#", "CInt(300)", "1+299", "NULL", "12345.67"]),
                ("TEXT(3)", ["True", "False", "CInt(True)", "-1", "300", "70000", "1.5", "'abc'", "'abcdef'", "'5'", "NULL"]),
                ("GUID", ["True", "False", "CInt(True)", "CInt(300)", "-1", "300", "#1/1/2000#", "NULL"]),
            ])
            foreach (string value in defaults)
            {
                string c = "D" + n++, a = "D" + n++, s = "D" + n++;
                Statement(script, $"CREATE TABLE {c} (ID LONG, V {sql} DEFAULT {value})");
                Statement(script, $"INSERT INTO {c} (ID) VALUES (1)");
                Rows(script, $"SELECT V FROM {c}");
                Statement(script, $"CREATE TABLE {a} (ID LONG)");
                Statement(script, $"ALTER TABLE {a} ADD COLUMN V {sql} DEFAULT {value}");
                Statement(script, $"INSERT INTO {a} (ID) VALUES (1)");
                Rows(script, $"SELECT * FROM {a}");
                Statement(script, $"CREATE TABLE {s} (ID LONG, V {sql})");
                Statement(script, $"ALTER TABLE {s} ALTER COLUMN V SET DEFAULT {value}");
                Statement(script, $"INSERT INTO {s} (ID) VALUES (1)");
                Rows(script, $"SELECT V FROM {s}");
            }

        SameThroughBothEngines(script);
    }

    // A number or date joined to a Yes/No column: looked up in the Yes/No column - an outer join's optional side,
    // or the table ACE makes the inner one (the only one indexed on its column, else the larger) - it is compared
    // by its truth, so 1 and 2 join True; looked up the other way the Yes/No is -1 or 0. A Decimal is always a number.
    [Fact]
    public void A_column_joined_to_a_YesNo_column_joins_the_rows_ACE_joins()
    {
        var script = new List<Step>();
        YesNoSource(script);
        Statement(script, "CREATE TABLE SI (ID LONG PRIMARY KEY, Y YESNO)");
        Statement(script, "CREATE INDEX IX_SI ON SI (Y)");
        Statement(script, "INSERT INTO SI VALUES (1, True)");
        Statement(script, "INSERT INTO SI VALUES (2, False)");
        Statement(script, "CREATE TABLE SB (ID LONG PRIMARY KEY, Y YESNO)");
        Statement(script, "CREATE INDEX IX_SB ON SB (Y)");
        for (int i = 1; i <= 12; i++) Statement(script, $"INSERT INTO SB VALUES ({i}, {(i % 2 == 1 ? "True" : "False")})");

        int n = 0;
        foreach (string sql in (string[])["BYTE", "SHORT", "LONG", "DOUBLE", "CURRENCY", "DECIMAL(18,2)", "DATETIME", "YESNO"])
            foreach (bool indexed in (bool[])[false, true])
            {
                string t = "J" + n++;
                Statement(script, $"CREATE TABLE {t} (ID LONG PRIMARY KEY, V {sql})");
                if (indexed) Statement(script, $"CREATE INDEX IX_{t} ON {t} (V)");
                string[] seed = sql switch
                {
                    "BYTE" => ["255", "1", "0", "NULL", "2"],
                    "DATETIME" => ["#12/29/1899#", "#12/31/1899#", "#12/30/1899#", "NULL", "#1/1/1900#"],
                    "YESNO" => ["True", "True", "False", "NULL", "True"],
                    _ => ["-1", "1", "0", "NULL", "2"],
                };
                for (int i = 0; i < seed.Length; i++) Statement(script, $"INSERT INTO {t} VALUES ({i + 1}, {seed[i]})");
                foreach (string s in (string[])["S", "SI", "SB"])
                {
                    string columns = $"{t}.ID, {s}.ID", order = $"ORDER BY {t}.ID, {s}.ID";
                    Rows(script, $"SELECT {columns} FROM {t} INNER JOIN {s} ON {t}.V = {s}.Y {order}");
                    Rows(script, $"SELECT {columns} FROM {t} INNER JOIN {s} ON {s}.Y = {t}.V {order}");
                    Rows(script, $"SELECT {columns} FROM {s} INNER JOIN {t} ON {t}.V = {s}.Y {order}");
                    Rows(script, $"SELECT {columns} FROM {t}, {s} WHERE {t}.V = {s}.Y {order}");
                    Rows(script, $"SELECT {columns} FROM {t} LEFT JOIN {s} ON {t}.V = {s}.Y {order}");
                    Rows(script, $"SELECT {columns} FROM {s} LEFT JOIN {t} ON {t}.V = {s}.Y {order}");
                    Rows(script, $"SELECT {columns} FROM {t} INNER JOIN {s} ON {t}.V <> {s}.Y WHERE {s}.ID < 3 {order}");
                }
            }

        SameThroughBothEngines(script);
    }

    // EXISTS, IN and = ANY look the outer value up in the subquery's Yes/No column, so compare it by its truth (1 and
    // 2 find True); NOT IN, <> ALL, a correlated IN and a Boolean the subquery computes compare as numbers, a Decimal
    // always does, and a text column beside a Yes/No column is a type mismatch. NOT EXISTS and a correlated scalar
    // subquery compare as numbers over an unindexed Yes/No column and by truth over one that leads an index, which
    // ACE seeks.
    [Fact]
    public void A_value_looked_up_in_a_YesNo_column_by_a_subquery_finds_the_rows_ACE_finds()
    {
        var script = new List<Step>();
        YesNoSource(script);
        Statement(script, "CREATE TABLE SI (ID LONG PRIMARY KEY, Y YESNO)");
        Statement(script, "CREATE INDEX IX_SI ON SI (Y)");
        Statement(script, "INSERT INTO SI VALUES (1, True)");
        Statement(script, "INSERT INTO SI VALUES (2, False)");
        Statement(script, "CREATE TABLE ST (ID LONG PRIMARY KEY, Y YESNO)");
        Statement(script, "INSERT INTO ST VALUES (1, True)");

        int n = 0;
        foreach (string sql in (string[])["BYTE", "SHORT", "LONG", "DOUBLE", "CURRENCY", "DECIMAL(18,2)", "DATETIME", "YESNO", "TEXT(10)", "BIG"])
            foreach (bool indexed in (bool[])[false, true])
            {
                if (sql == "BIG" && indexed) continue;
                string t = "Q" + n++;
                Statement(script, $"CREATE TABLE {t} (ID LONG PRIMARY KEY, V {(sql == "BIG" ? "LONG" : sql)})");
                if (indexed) Statement(script, $"CREATE INDEX IX_{t} ON {t} (V)");
                string[] seed = sql switch
                {
                    "BYTE" => ["255", "1", "0", "NULL", "2"],
                    "DATETIME" => ["#12/29/1899#", "#12/31/1899#", "#12/30/1899#", "NULL", "#1/1/1900#"],
                    "YESNO" => ["True", "True", "False", "NULL", "True"],
                    "TEXT(10)" => ["'-1'", "'1'", "'0'", "NULL", "'2'"],
                    // Sixty rows, enough for a correlated EXISTS to leave the row-by-row path for the hashed one.
                    "BIG" => [.. Enumerable.Range(1, 60).Select(i => (i % 5) switch { 0 => "-1", 1 => "1", 2 => "0", 3 => "2", _ => "NULL" })],
                    _ => ["-1", "1", "0", "NULL", "2"],
                };
                for (int i = 0; i < seed.Length; i++) Statement(script, $"INSERT INTO {t} VALUES ({i + 1}, {seed[i]})");

                foreach (string s in (string[])["S", "SI", "ST"])
                {
                    Rows(script, $"SELECT {t}.ID FROM {t} WHERE EXISTS (SELECT 1 FROM {s} WHERE {s}.Y = {t}.V) ORDER BY {t}.ID");
                    Rows(script, $"SELECT {t}.ID FROM {t} WHERE EXISTS (SELECT 1 FROM {s} WHERE {t}.V = {s}.Y) ORDER BY {t}.ID");
                    Rows(script, $"SELECT {t}.ID FROM {t} WHERE EXISTS (SELECT 1 FROM {s} WHERE {s}.Y = {t}.V AND {s}.ID = 1) ORDER BY {t}.ID");
                    Rows(script, $"SELECT {t}.ID FROM {t} WHERE EXISTS (SELECT 1 FROM {s} WHERE {s}.Y <> {t}.V) ORDER BY {t}.ID");
                    Rows(script, $"SELECT {t}.ID FROM {t} WHERE {t}.V IN (SELECT Y FROM {s}) ORDER BY {t}.ID");
                    Rows(script, $"SELECT {t}.ID FROM {t} WHERE {t}.V NOT IN (SELECT Y FROM {s}) ORDER BY {t}.ID");
                    Rows(script, $"SELECT {t}.ID FROM {t} WHERE {t}.V IN (SELECT Y FROM {s} WHERE ID = 1) ORDER BY {t}.ID");
                    Rows(script, $"SELECT {t}.ID FROM {t} WHERE {t}.V IN (SELECT (ID = 1) FROM {s}) ORDER BY {t}.ID");
                    Rows(script, $"SELECT {t}.ID FROM {t} WHERE {t}.V IN (SELECT CInt(Y) FROM {s}) ORDER BY {t}.ID");
                    Rows(script, $"SELECT {t}.ID FROM {t} WHERE {t}.V IN (SELECT Y FROM {s} WHERE {s}.ID = {t}.ID) ORDER BY {t}.ID");
                    Rows(script, $"SELECT {t}.ID FROM {t} WHERE {t}.V = ANY (SELECT Y FROM {s}) ORDER BY {t}.ID");
                    Rows(script, $"SELECT {t}.ID FROM {t} WHERE {t}.V <> ALL (SELECT Y FROM {s}) ORDER BY {t}.ID");
                    Rows(script, $"SELECT {s}.ID FROM {s} WHERE EXISTS (SELECT 1 FROM {t} WHERE {t}.V = {s}.Y) ORDER BY {s}.ID");
                    Rows(script, $"SELECT {s}.ID FROM {s} WHERE {s}.Y IN (SELECT V FROM {t}) ORDER BY {s}.ID");
                    Rows(script, $"SELECT {s}.ID, (SELECT COUNT(*) FROM {t} WHERE {t}.V = {s}.Y) FROM {s} ORDER BY {s}.ID");
                    // ACE answers these four by a truth seek of SI's index on Y, and by plain comparison without one.
                    Rows(script, $"SELECT {t}.ID FROM {t} WHERE NOT EXISTS (SELECT 1 FROM {s} WHERE {s}.Y = {t}.V) ORDER BY {t}.ID");
                    Rows(script, $"SELECT {t}.ID, (SELECT COUNT(*) FROM {s} WHERE {s}.Y = {t}.V) FROM {t} ORDER BY {t}.ID");
                    Rows(script, $"SELECT {t}.ID, (SELECT MAX({s}.ID) FROM {s} WHERE {s}.Y = {t}.V) FROM {t} ORDER BY {t}.ID");
                    Rows(script, $"SELECT {t}.ID FROM {t} WHERE (SELECT COUNT(*) FROM {s} WHERE {s}.Y = {t}.V) > 0 ORDER BY {t}.ID");
                }
            }

        SameThroughBothEngines(script);
    }

    // A comparison ACE answers from an index a Yes/No column leads - =, <, >, <=, >=, IN, BETWEEN, alone or under AND
    // and OR - takes the other value by its truth, so Y = 2 finds the True rows; without such an index, under <> or
    // NOT, and for a written Decimal or a Decimal column, it compares as a number. Written text is a type mismatch.
    [Fact]
    public void A_value_compared_with_a_YesNo_column_that_leads_an_index_is_taken_by_its_truth_as_ACE_takes_it()
    {
        var script = new List<Step>();
        // S: no index. SI: an index on Y. SU: a unique one. SC: a composite (Y, ID). SK: (ID, Y), which Y does not lead.
        foreach (string s in (string[])["S", "SI", "SU", "SC", "SK"])
        {
            Statement(script, $"CREATE TABLE {s} (ID LONG PRIMARY KEY, Y YESNO)");
            Statement(script, $"INSERT INTO {s} VALUES (1, True)");
            Statement(script, $"INSERT INTO {s} VALUES (2, False)");
        }
        Statement(script, "CREATE INDEX IX_SI ON SI (Y)");
        Statement(script, "CREATE UNIQUE INDEX IX_SU ON SU (Y)");
        Statement(script, "CREATE INDEX IX_SC ON SC (Y, ID)");
        Statement(script, "CREATE INDEX IX_SK ON SK (ID, Y)");
        Statement(script, "CREATE TABLE SB (ID LONG PRIMARY KEY, Y YESNO)");
        Statement(script, "CREATE INDEX IX_SB ON SB (Y)");
        for (int i = 1; i <= 12; i++) Statement(script, $"INSERT INTO SB VALUES ({i}, {(i % 2 == 1 ? "True" : "False")})");

        string[] tables = ["S", "SI", "SU", "SC", "SK", "SB"];
        foreach (string s in tables)
        {
            foreach (string op in (string[])["=", "<>"])
                foreach (string x in (string[])["2", "1", "-1", "0", "2.5", "0.0", "-1.0", "CLng(2)", "CInt(2)", "CInt(-1)", "CDbl(2)", "CSng(2)", "CCur(2)",
                    "CByte(2)", "#1/1/1900#", "#12/30/1899#", "'2'", "'-1'", "'abc'", "1+1", "(1=1)"])
                    Rows(script, $"SELECT ID FROM {s} WHERE Y {op} {x} ORDER BY ID");
            foreach (string condition in (string[])["Y IN (2)", "Y IN (CLng(2))", "Y BETWEEN 1 AND 3", "Y > 0", "Y < 0", "Y >= CLng(2)", "2 = Y", "CLng(2) = Y",
                "Y = 2 OR ID = 2", "NOT (Y = CLng(2))", "Y + 0 = 2", "CLng(Y) = 2", "Y = NULL"])
                Rows(script, $"SELECT ID FROM {s} WHERE {condition} ORDER BY ID");
            // Where Y only follows ID in an index, ACE still answers this pair from it; LibRed does not.
            if (s != "SK")
            {
                Rows(script, $"SELECT ID FROM {s} WHERE Y = 2 AND ID = 1 ORDER BY ID");
                Rows(script, $"SELECT ID FROM {s} WHERE Y = CLng(2) AND ID = 1 ORDER BY ID");
            }
            foreach (object parameter in (object[])[2, (short)2, (byte)2, 2d, 2f, 2m, 2.5m, -1, 0, true, "2"])
                foreach (string op in (string[])["=", "<>"])
                    Rows(script, $"SELECT ID FROM {s} WHERE Y {op} @p ORDER BY ID", new P(parameter, null));
        }

        int n = 0;
        foreach (string sql in (string[])["BYTE", "LONG", "DOUBLE", "CURRENCY", "DECIMAL(18,2)", "DATETIME", "YESNO"])
        {
            string t = "K" + n++;
            Statement(script, $"CREATE TABLE {t} (ID LONG PRIMARY KEY, V {sql})");
            string[] seed = sql switch
            {
                "BYTE" => ["255", "1", "0", "NULL", "2"],
                "DATETIME" => ["#12/29/1899#", "#12/31/1899#", "#12/30/1899#", "NULL", "#1/1/1900#"],
                "YESNO" => ["True", "True", "False", "NULL", "True"],
                _ => ["-1", "1", "0", "NULL", "2"],
            };
            for (int i = 0; i < seed.Length; i++) Statement(script, $"INSERT INTO {t} VALUES ({i + 1}, {seed[i]})");
            foreach (string s in tables)
            {
                Rows(script, $"SELECT {t}.ID FROM {t} WHERE NOT EXISTS (SELECT 1 FROM {s} WHERE {s}.Y = {t}.V) ORDER BY {t}.ID");
                Rows(script, $"SELECT {t}.ID FROM {t} WHERE NOT EXISTS (SELECT 1 FROM {s} WHERE {t}.V = {s}.Y) ORDER BY {t}.ID");
                Rows(script, $"SELECT {t}.ID, (SELECT COUNT(*) FROM {s} WHERE {s}.Y = {t}.V) FROM {t} ORDER BY {t}.ID");
                Rows(script, $"SELECT {t}.ID, (SELECT COUNT(*) FROM {s} WHERE {t}.V = {s}.Y) FROM {t} ORDER BY {t}.ID");
                Rows(script, $"SELECT {t}.ID, (SELECT MAX({s}.ID) FROM {s} WHERE {s}.Y = {t}.V) FROM {t} ORDER BY {t}.ID");
                Rows(script, $"SELECT {t}.ID, (SELECT TOP 1 {s}.ID FROM {s} WHERE {s}.Y = {t}.V ORDER BY {s}.ID) FROM {t} ORDER BY {t}.ID");
                Rows(script, $"SELECT {t}.ID FROM {t} WHERE (SELECT COUNT(*) FROM {s} WHERE {s}.Y = {t}.V) > 0 ORDER BY {t}.ID");
                Rows(script, $"SELECT {t}.ID, (SELECT COUNT(*) FROM {s} WHERE {s}.Y <> {t}.V) FROM {t} ORDER BY {t}.ID");
                Rows(script, $"SELECT {t}.ID, (SELECT COUNT(*) FROM {s} WHERE {s}.Y = {t}.V + 0) FROM {t} ORDER BY {t}.ID");
                Rows(script, $"SELECT {t}.ID FROM {t} WHERE EXISTS (SELECT 1 FROM {s} WHERE {s}.Y = {t}.V) ORDER BY {t}.ID");
            }
        }

        SameThroughBothEngines(script);
    }

    // The rows a query returns read a Yes/No column from an outer join's unmatched side as False - the column, the
    // expressions over it, ORDER BY and DISTINCT - while WHERE, an aggregate and a row being inserted see the Null,
    // and grouping by the column is refused.
    [Fact]
    public void A_YesNo_column_on_the_unmatched_side_of_an_outer_join_reads_as_ACE_reads_it()
    {
        var script = new List<Step>();
        YesNoSource(script);
        Statement(script, "CREATE TABLE YS (ID LONG PRIMARY KEY, F YESNO, G YESNO, T TEXT(10), N LONG)");
        Statement(script, "INSERT INTO YS VALUES (1, True, False, 'a', 5)");
        Statement(script, "INSERT INTO YS VALUES (2, False, True, NULL, NULL)");
        Statement(script, "CREATE TABLE L (ID LONG PRIMARY KEY)");
        for (int i = 1; i <= 4; i++) Statement(script, $"INSERT INTO L VALUES ({i})");
        Statement(script, "CREATE VIEW VOJ AS SELECT L.ID, YS.F, YS.T FROM L LEFT JOIN YS ON L.ID = YS.ID");

        const string oj = "FROM L LEFT JOIN YS ON L.ID = YS.ID";
        foreach (string query in (string[])
            [
                $"SELECT L.ID, YS.F, YS.G, YS.T, YS.N {oj} ORDER BY L.ID",
                $"SELECT * {oj} ORDER BY L.ID",
                $"SELECT YS.* {oj} ORDER BY L.ID",
                $"SELECT L.ID, IIF(YS.F IS NULL, 'n', 'v') {oj} ORDER BY L.ID",
                $"SELECT L.ID, IIF(YS.F, 'y', 'n') {oj} ORDER BY L.ID",
                $"SELECT L.ID, YS.F & '' {oj} ORDER BY L.ID",
                $"SELECT L.ID, YS.F + 0 {oj} ORDER BY L.ID",
                $"SELECT L.ID {oj} WHERE YS.F IS NULL ORDER BY L.ID",
                $"SELECT L.ID {oj} WHERE YS.F IS NOT NULL ORDER BY L.ID",
                $"SELECT L.ID {oj} WHERE YS.F = False ORDER BY L.ID",
                $"SELECT L.ID {oj} WHERE YS.F = True ORDER BY L.ID",
                $"SELECT L.ID {oj} WHERE YS.F <> True ORDER BY L.ID",
                $"SELECT L.ID {oj} WHERE NOT YS.F ORDER BY L.ID",
                $"SELECT L.ID {oj} WHERE YS.F ORDER BY L.ID",
                $"SELECT L.ID {oj} WHERE IIF(YS.F IS NULL, 1, 0) = 1 ORDER BY L.ID",
                $"SELECT L.ID {oj} WHERE IsNull(YS.F) ORDER BY L.ID",
                $"SELECT L.ID {oj} WHERE YS.F + 0 = 0 ORDER BY L.ID",
                $"SELECT L.ID {oj} WHERE YS.F = YS.G ORDER BY L.ID",
                $"SELECT L.ID {oj} WHERE YS.F IS NULL OR L.ID = 1 ORDER BY L.ID",
                $"SELECT L.ID, YS.F {oj} WHERE YS.F IS NULL ORDER BY L.ID",
                $"SELECT L.ID, YS.F {oj} ORDER BY YS.F, L.ID",
                $"SELECT L.ID, YS.F {oj} ORDER BY YS.F DESC, L.ID",
                $"SELECT L.ID, YS.T {oj} ORDER BY YS.T, L.ID",
                $"SELECT L.ID {oj} ORDER BY YS.F, L.ID DESC",
                $"SELECT L.ID {oj} ORDER BY NOT YS.F, L.ID",
                $"SELECT L.ID {oj} ORDER BY IIF(YS.F IS NULL, 0, 1), L.ID DESC",
                $"SELECT L.ID, YS.F AS X {oj} ORDER BY YS.F, L.ID",
                $"SELECT TOP 1 L.ID {oj} ORDER BY YS.F DESC, L.ID DESC",
                $"SELECT DISTINCT YS.F {oj}",
                $"SELECT DISTINCT YS.F, YS.T {oj}",
                $"SELECT YS.F, COUNT(*) {oj} GROUP BY YS.F",
                $"SELECT YS.G, COUNT(*) {oj} GROUP BY YS.G",
                $"SELECT L.ID, YS.F {oj} GROUP BY L.ID, YS.F ORDER BY L.ID",
                $"SELECT L.ID {oj} GROUP BY L.ID, YS.F HAVING YS.F IS NULL ORDER BY L.ID",
                $"SELECT YS.F, COUNT(*) {oj} WHERE L.ID < 3 GROUP BY YS.F",
                "SELECT YS.F, COUNT(*) FROM L INNER JOIN YS ON L.ID = YS.ID GROUP BY YS.F",
                $"SELECT YS.T, COUNT(*) {oj} GROUP BY YS.T",
                $"SELECT IIF(YS.F, 1, 0), COUNT(*) {oj} GROUP BY IIF(YS.F, 1, 0)",
                $"SELECT COUNT(YS.F), COUNT(YS.T), COUNT(*) {oj}",
                $"SELECT D.ID FROM (SELECT L.ID, YS.F {oj}) AS D WHERE D.F IS NULL ORDER BY D.ID",
                $"SELECT D.ID FROM (SELECT L.ID, YS.F {oj}) AS D WHERE D.F = False ORDER BY D.ID",
                $"SELECT D.ID, D.F FROM (SELECT L.ID, YS.F {oj}) AS D ORDER BY D.ID",
                $"SELECT E.ID, E.F FROM (SELECT D.ID, D.F FROM (SELECT L.ID, YS.F {oj}) AS D) AS E ORDER BY E.ID",
                $"SELECT D.ID FROM (SELECT L.ID, NOT YS.F AS X {oj}) AS D WHERE D.X IS NULL ORDER BY D.ID",
                "SELECT * FROM VOJ ORDER BY ID",
                "SELECT ID FROM VOJ WHERE F IS NULL ORDER BY ID",
                "SELECT ID FROM VOJ WHERE F = False ORDER BY ID",
                "SELECT L.ID, YS.F FROM YS RIGHT JOIN L ON L.ID = YS.ID ORDER BY L.ID",
                "SELECT L.ID FROM YS RIGHT JOIN L ON L.ID = YS.ID WHERE YS.F IS NULL ORDER BY L.ID",
                "SELECT L.ID, YS.F FROM L LEFT JOIN YS ON (L.ID = YS.ID AND YS.F = True) ORDER BY L.ID",
                "SELECT L.ID, S.ID FROM (L LEFT JOIN YS ON L.ID = YS.ID) LEFT JOIN S ON YS.F = S.Y ORDER BY L.ID, S.ID",
                "SELECT L.ID, YS.F, S.ID FROM (L LEFT JOIN YS ON L.ID = YS.ID) LEFT JOIN S ON YS.F = S.Y ORDER BY L.ID, S.ID",
                "SELECT YS.ID, YS.F, L.ID FROM YS LEFT JOIN L ON L.ID = YS.ID + 2 ORDER BY YS.ID",
            ])
            Rows(script, query);

        // Written into a table, the unmatched rows keep the Null a LONG or TEXT column can hold.
        Statement(script, "CREATE TABLE OUTL (ID LONG, V LONG)");
        Statement(script, $"INSERT INTO OUTL (ID, V) SELECT L.ID, YS.F {oj}");
        Rows(script, "SELECT ID, V FROM OUTL ORDER BY ID");
        Statement(script, "CREATE TABLE OUTY (ID LONG, V YESNO)");
        Statement(script, $"INSERT INTO OUTY (ID, V) SELECT L.ID, YS.F {oj}");
        Rows(script, "SELECT ID, V FROM OUTY ORDER BY ID");
        Statement(script, "CREATE TABLE OUTT (ID LONG, V TEXT(10))");
        Statement(script, $"INSERT INTO OUTT (ID, V) SELECT L.ID, YS.F {oj}");
        Rows(script, "SELECT ID, V FROM OUTT ORDER BY ID");
        Statement(script, "CREATE TABLE OUT2 (ID LONG, A LONG, B LONG)");
        Statement(script, $"INSERT INTO OUT2 (ID, A, B) SELECT L.ID, NOT YS.F, YS.F + 0 {oj}");
        Rows(script, "SELECT ID, A, B FROM OUT2 ORDER BY ID");
        Statement(script, $"SELECT L.ID, YS.F INTO OUTNEW {oj}");
        Rows(script, "SELECT ID, F FROM OUTNEW ORDER BY ID");

        SameThroughBothEngines(script);
    }

    // The DbTypes OLE DB will not carry a Boolean as are refused, and Decimal and the integer types Access has no
    // column for arrive as the text "1" or "0".
    [Fact]
    public void A_Boolean_parameter_under_a_DbType_OLE_DB_refuses_or_carries_as_text_is_what_ACE_is_handed()
    {
        DbType[] refused = [DbType.Binary, DbType.Date, DbType.DateTime, DbType.DateTime2, DbType.DateTimeOffset, DbType.Guid,
            DbType.Int64, DbType.Time, DbType.VarNumeric, DbType.Xml];
        DbType[] asText = [DbType.Decimal, DbType.SByte, DbType.UInt16, DbType.UInt32, DbType.UInt64];

        var script = new List<Step>();
        foreach ((string t, string sql) in ((string, string)[])[("RS", "SHORT"), ("RT", "TEXT(20)"), ("RD", "DATETIME"), ("RB", "BINARY(4)"), ("RG", "GUID")])
        {
            Statement(script, $"CREATE TABLE {t} (ID LONG PRIMARY KEY, V {sql})");
            foreach (DbType declared in (DbType[])[.. refused, .. asText])
                foreach (bool value in (bool[])[true, false])
                {
                    Statement(script, $"DELETE FROM {t}");
                    Statement(script, $"INSERT INTO {t} (ID, V) VALUES (1, @p)", new P(value, declared));
                    Rows(script, $"SELECT V FROM {t}");
                    Rows(script, $"SELECT COUNT(*) FROM {t} WHERE V = @p", new P(value, declared));
                }
        }

        SameThroughBothEngines(script);
    }

    [Fact]
    public void A_YesNo_column_is_not_nullable_in_the_Columns_rowset_and_nullable_in_a_reader_schema()
    {
        var script = new List<Step>();
        Statement(script, "CREATE TABLE YS (ID LONG PRIMARY KEY, F YESNO, FR YESNO NOT NULL, FD YESNO DEFAULT True, T TEXT(10), TR TEXT(10) NOT NULL, C COUNTER)");
        Statement(script, "CREATE VIEW VY AS SELECT ID, F, FR, FD, T, TR FROM YS");
        Statement(script, "CREATE TABLE A1 (ID LONG PRIMARY KEY, F LONG)");
        Statement(script, "ALTER TABLE A1 ALTER COLUMN F YESNO");
        Statement(script, "CREATE TABLE A2 (ID LONG PRIMARY KEY, F YESNO NOT NULL)");
        Statement(script, "ALTER TABLE A2 ALTER COLUMN F YESNO");
        Statement(script, "CREATE TABLE A3 (ID LONG PRIMARY KEY)");
        Statement(script, "ALTER TABLE A3 ADD COLUMN F YESNO");
        Statement(script, "ALTER TABLE A3 ADD COLUMN G YESNO NOT NULL");
        foreach (string table in (string[])["YS", "VY", "A1", "A2", "A3"])
            script.Add(new($"Columns rowset of {table}", c => Columns(c, table)));
        foreach (CommandBehavior behavior in (CommandBehavior[])[CommandBehavior.Default, CommandBehavior.KeyInfo, CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo])
            foreach (string from in (string[])["YS", "VY"])
                script.Add(new($"Reader schema of SELECT F, FR, FD FROM {from} [{behavior}]", c => ReaderSchema(c, $"SELECT F, FR, FD FROM {from}", behavior)));

        // What INFORMATION_SCHEMA.COLUMNS and the scaffolder report, against ACE's own IS_NULLABLE for the column.
        script.Add(new("JetStoreType.IsNullable of YS against ACE's IS_NULLABLE", c =>
        {
            if (c is LibRedConnection libred)
            {
                using JetDatabase db = JetDatabase.Open(libred.DataSource, readOnly: true);
                return string.Join(" ", db.Catalog.FindTable("YS")!.Columns.Select(col => col.Name + ":" + (JetStoreType.IsNullable(col) ? "N" : "R")));
            }
            DataTable t = c.GetSchema("Columns", [null, null, "YS", null]);
            return string.Join(" ", t.Rows.Cast<DataRow>().OrderBy(r => Convert.ToInt64(r["ORDINAL_POSITION"], CultureInfo.InvariantCulture))
                .Select(r => $"{r["COLUMN_NAME"]}:{((bool)r["IS_NULLABLE"] ? "N" : "R")}"));
        }));
        script.Add(new("INFORMATION_SCHEMA.COLUMNS of YS against ACE's IS_NULLABLE", c =>
        {
            if (c is LibRedConnection)
                return Query(c, "SELECT COLUMN_NAME, IS_NULLABLE FROM `INFORMATION_SCHEMA.COLUMNS` WHERE TABLE_NAME = 'YS' ORDER BY ORDINAL_POSITION");
            DataTable t = c.GetSchema("Columns", [null, null, "YS", null]);
            return "[" + string.Join(" ", t.Rows.Cast<DataRow>().OrderBy(r => Convert.ToInt64(r["ORDINAL_POSITION"], CultureInfo.InvariantCulture))
                .Select(r => $"String:{r["COLUMN_NAME"]},Boolean:{r["IS_NULLABLE"]}")) + "]";
        }));

        SameThroughBothEngines(script);
    }
}
