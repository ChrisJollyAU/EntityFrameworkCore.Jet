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

    // A Currency stored into a binary column is its eight-byte scaled integer, whether it is written, computed, read
    // from a CURRENCY column, retyped or a DEFAULT; a GUID column refuses it.
    [Fact]
    public void A_Currency_stored_into_a_binary_or_GUID_column_is_its_eight_bytes_as_ACE_stores_it()
    {
        var script = new List<Step>();
        Statement(script, "CREATE TABLE CS (ID LONG, C CURRENCY)");
        Statement(script, "INSERT INTO CS VALUES (1, 1)");
        Statement(script, "CREATE TABLE ONE (ID LONG)");
        Statement(script, "INSERT INTO ONE VALUES (1)");
        int n = 0;
        foreach (string sql in (string[])["BINARY(4)", "BINARY(8)", "VARBINARY(8)", "LONGBINARY", "GUID"])
        {
            string t = "Y" + n++;
            Statement(script, $"CREATE TABLE {t} (ID LONG, V {sql})");
            foreach (string literal in (string[])["CCur(1)", "CCur(-1.5)", "CCur(1)+CCur(1)", "CCur(12345.67)"])
            {
                Statement(script, $"DELETE FROM {t}");
                Statement(script, $"INSERT INTO {t} (ID, V) VALUES (1, {literal})");
                Rows(script, $"SELECT V FROM {t}");
                Statement(script, $"DELETE FROM {t}");
                Statement(script, $"INSERT INTO {t} (ID) VALUES (1)");
                Statement(script, $"UPDATE {t} SET V = {literal}");
                Rows(script, $"SELECT V FROM {t}");
            }
            foreach (string select in (string[])["SELECT 1, C FROM CS", "SELECT 1, CCur(1) FROM ONE"])
            {
                Statement(script, $"DELETE FROM {t}");
                Statement(script, $"INSERT INTO {t} (ID, V) {select}");
                Rows(script, $"SELECT V FROM {t}");
            }
            foreach (string join in (string[])["CS", "(SELECT ID, C FROM CS) AS D"])
            {
                string alias = join == "CS" ? "CS" : "D";
                Statement(script, $"DELETE FROM {t}");
                Statement(script, $"INSERT INTO {t} (ID) VALUES (1)");
                Statement(script, $"UPDATE {t} INNER JOIN {join} ON {t}.ID = {alias}.ID SET {t}.V = {alias}.C");
                Rows(script, $"SELECT V FROM {t}");
            }

            string a = "Y" + n++;
            Statement(script, $"CREATE TABLE {a} (ID LONG, V CURRENCY)");
            Statement(script, $"INSERT INTO {a} VALUES (1, 1)");
            Statement(script, $"ALTER TABLE {a} ALTER COLUMN V {sql}");
            Rows(script, $"SELECT V FROM {a}");
        }

        string c = "Y" + n++, d = "Y" + n++, s = "Y" + n++;
        Statement(script, $"CREATE TABLE {c} (ID LONG, V BINARY(8) DEFAULT CCur(1))");
        Statement(script, $"INSERT INTO {c} (ID) VALUES (1)");
        Rows(script, $"SELECT V FROM {c}");
        Statement(script, $"CREATE TABLE {d} (ID LONG)");
        Statement(script, $"ALTER TABLE {d} ADD COLUMN V BINARY(8) DEFAULT CCur(1)");
        Statement(script, $"INSERT INTO {d} (ID) VALUES (1)");
        Rows(script, $"SELECT * FROM {d}");
        Statement(script, $"CREATE TABLE {s} (ID LONG, V BINARY(8))");
        Statement(script, $"ALTER TABLE {s} ALTER COLUMN V SET DEFAULT CCur(1)");
        Statement(script, $"INSERT INTO {s} (ID) VALUES (1)");
        Rows(script, $"SELECT V FROM {s}");

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

    // A DEFAULT wider than a BINARY, VARBINARY or BIGBINARY column is cut to its width, and a LONGBINARY keeps all of
    // it; one too wide for a DECIMAL column's precision stores Null; and a Yes/No column's is checked as an Integer
    // (Int16) when it is declared, which retyping a column to Yes/No does not do.
    [Fact]
    public void A_DEFAULT_is_cut_nulled_or_refused_as_ACE_does_it()
    {
        var script = new List<Step>();
        int n = 0;
        foreach ((string sql, string[] defaults) in ((string, string[])[])
            [
                ("BINARY(4)", ["1.5", "'abc'", "CCur(1)"]),
                ("VARBINARY(4)", ["1.5"]),
                ("BIGBINARY(10)", ["'abcdefghij'"]),
                ("LONGBINARY", ["'" + new string('a', 100) + "'"]),
                ("DECIMAL(4,1)", ["70000", "3000000000", "#1/1/2000#", "12345.67", "1000", "1.5", "999.9", "999.95", "-999.9", "'12.5'", "Now()"]),
                ("YESNO", ["32767", "32768", "-32768", "-32769", "70000", "3000000000", "#1/1/2000#", "CDbl(70000)", "CLng(70000)", "1.5", "12345.67", "'5'", "'abc'", "Now()"]),
            ])
            foreach (string value in defaults)
            {
                string c = "E" + n++, a = "E" + n++, s = "E" + n++;
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

        foreach ((string source, string value) in ((string, string)[])[("LONG", "70000"), ("DATETIME", "#1/1/2000#")])
        {
            string t = "E" + n++;
            Statement(script, $"CREATE TABLE {t} (ID LONG, V {source})");
            Statement(script, $"INSERT INTO {t} VALUES (1, {value})");
            Statement(script, $"ALTER TABLE {t} ALTER COLUMN V YESNO");
            Rows(script, $"SELECT V FROM {t}");
        }

        SameThroughBothEngines(script);
    }

    // What a value becomes stored into a binary or GUID column - its bytes, or a refusal - by every statement that
    // stores one: INSERT ... VALUES, INSERT ... SELECT (the value written and read from a column of its own type),
    // UPDATE (written and through a join), a DEFAULT an insert applies, and ALTER COLUMN from a column of its type.
    [Fact]
    public void A_value_stored_into_a_binary_or_GUID_column_is_what_ACE_stores_by_every_statement_that_stores_it()
    {
        var script = new List<Step>();
        const string guid = "{guid {00000001-0002-0003-0004-000000000005}}";
        int n = 0;
        foreach (string target in (string[])["BINARY(4)", "BINARY(16)", "VARBINARY(16)", "GUID"])
            foreach ((string expression, string column, string seed) in ((string, string, string)[])
                [
                    (guid, "GUID", guid), ("CLng(1)", "LONG", "1"), ("CCur(1)", "CURRENCY", "1"), ("1.5", "DECIMAL(18,1)", "1.5"),
                    ("CDbl(1.5)", "DOUBLE", "1.5"), ("True", "YESNO", "True"), ("'ab'", "TEXT(10)", "'ab'"),
                ])
            {
                string s = "Q" + n++, t = "Q" + n++, d = "Q" + n++;
                Statement(script, $"CREATE TABLE {s} (ID LONG, X {column})");
                Statement(script, $"INSERT INTO {s} VALUES (1, {seed})");
                Statement(script, $"CREATE TABLE {t} (ID LONG, V {target})");

                Statement(script, $"INSERT INTO {t} (ID, V) VALUES (1, {expression})");
                Rows(script, $"SELECT V FROM {t}");
                foreach (string select in (string[])[$"SELECT ID, X FROM {s}", $"SELECT ID, {expression} FROM {s}"])
                {
                    Statement(script, $"DELETE FROM {t}");
                    Statement(script, $"INSERT INTO {t} (ID, V) {select}");
                    Rows(script, $"SELECT V FROM {t}");
                }

                Statement(script, $"DELETE FROM {t}");
                Statement(script, $"INSERT INTO {t} (ID) VALUES (1)");
                Statement(script, $"UPDATE {t} SET V = {expression}");
                Rows(script, $"SELECT V FROM {t}");
                Statement(script, $"UPDATE {t} SET V = NULL");
                Statement(script, $"UPDATE {t} INNER JOIN {s} ON {t}.ID = {s}.ID SET {t}.V = {s}.X");
                Rows(script, $"SELECT V FROM {t}");

                // ACE does not take a GUID literal in a DEFAULT (a syntax error); LibRed's SQL is a superset.
                if (expression != guid)
                {
                    Statement(script, $"CREATE TABLE {d} (ID LONG, V {target} DEFAULT {expression})");
                    Statement(script, $"INSERT INTO {d} (ID) VALUES (1)");
                    Rows(script, $"SELECT V FROM {d}");
                }

                Statement(script, $"ALTER TABLE {s} ALTER COLUMN X {target}");
                Rows(script, $"SELECT X FROM {s}");
            }

        // A Decimal or binary value's bytes are a GUID when there are sixteen of them, and too few are refused as too
        // small when a row is written - which declaring a DEFAULT does not check, where it does refuse a Currency.
        Statement(script, "CREATE TABLE QG (ID LONG, V GUID)");
        Statement(script, "INSERT INTO QG VALUES (1, 1.234567)");
        Statement(script, "INSERT INTO QG VALUES (2, 0x0100000002000300000400000000000F)");
        Rows(script, "SELECT V FROM QG ORDER BY ID");
        foreach ((string column, string seed) in ((string, string)[])[("DECIMAL(18,6)", "1.234567"), ("BINARY(16)", "0x0100000002000300000400000000000F")])
        {
            string a = "Q" + n++;
            Statement(script, $"CREATE TABLE {a} (ID LONG, X {column})");
            Statement(script, $"INSERT INTO {a} VALUES (1, {seed})");
            Statement(script, $"ALTER TABLE {a} ALTER COLUMN X GUID");
            Rows(script, $"SELECT X FROM {a}");
        }
        foreach (string value in (string[])["1.234567", "1.5", "12345678.1", "CCur(1234567)", "CCur(1)", "CDbl(1.5)"])
        {
            string c = "Q" + n++, a = "Q" + n++, s = "Q" + n++;
            Statement(script, $"CREATE TABLE {c} (ID LONG, V GUID DEFAULT {value})");
            Statement(script, $"INSERT INTO {c} (ID) VALUES (1)");
            Rows(script, $"SELECT V FROM {c}");
            Statement(script, $"CREATE TABLE {a} (ID LONG)");
            Statement(script, $"ALTER TABLE {a} ADD COLUMN V GUID DEFAULT {value}");
            Statement(script, $"INSERT INTO {a} (ID) VALUES (1)");
            Statement(script, $"CREATE TABLE {s} (ID LONG, V GUID)");
            Statement(script, $"ALTER TABLE {s} ALTER COLUMN V SET DEFAULT {value}");
            Statement(script, $"INSERT INTO {s} (ID) VALUES (1)");
        }

        // A Decimal whose text is wider than the column: refused when it is stored, cut as a DEFAULT, whole in a UNION.
        foreach (string target in (string[])["BINARY(4)", "BINARY(16)", "VARBINARY(16)"])
        {
            string s = "Q" + n++, t = "Q" + n++, d = "Q" + n++;
            Statement(script, $"CREATE TABLE {s} (ID LONG, X DECIMAL(18,3))");
            Statement(script, $"INSERT INTO {s} VALUES (1, 1234567.891)");
            Statement(script, $"CREATE TABLE {t} (ID LONG, V {target})");
            Statement(script, $"INSERT INTO {t} (ID, V) VALUES (1, 1234567.891)");
            Rows(script, $"SELECT V FROM {t}");
            foreach (string value in (string[])["X", "X + 0"])
            {
                Statement(script, $"DELETE FROM {t}");
                Statement(script, $"INSERT INTO {t} (ID, V) SELECT ID, {value} FROM {s}");
                Rows(script, $"SELECT V FROM {t}");
            }
            Statement(script, $"DELETE FROM {t}");
            Statement(script, $"INSERT INTO {t} (ID) VALUES (1)");
            Statement(script, $"UPDATE {t} SET V = 1234567.891");
            Rows(script, $"SELECT V FROM {t}");
            Statement(script, $"UPDATE {t} SET V = NULL");
            Statement(script, $"UPDATE {t} INNER JOIN {s} ON {t}.ID = {s}.ID SET {t}.V = {s}.X");
            Rows(script, $"SELECT V FROM {t}");
            Statement(script, $"CREATE TABLE {d} (ID LONG, V {target} DEFAULT 1234567.891)");
            Statement(script, $"INSERT INTO {d} (ID) VALUES (1)");
            Rows(script, $"SELECT V FROM {d}");
            Rows(script, $"SELECT V FROM {t} UNION ALL SELECT X FROM {s}");
            Rows(script, $"SELECT X FROM {s} UNION ALL SELECT V FROM {t}");
            Statement(script, $"ALTER TABLE {s} ALTER COLUMN X {target}");
            Rows(script, $"SELECT X FROM {s}");
        }

        // A binary column compared with a GUID column's value, with an index on the binary column and without.
        Statement(script, "CREATE TABLE QGS (ID LONG, G GUID)");
        Statement(script, $"INSERT INTO QGS VALUES (1, {guid})");
        Statement(script, "INSERT INTO QGS VALUES (2, {guid {00000001-0000-0000-0000-000000000000}})");
        foreach (bool indexed in (bool[])[false, true])
        {
            string t = "Q" + n++;
            Statement(script, $"CREATE TABLE {t} (ID LONG, B VARBINARY(50))");
            if (indexed) Statement(script, $"CREATE INDEX IX_{t} ON {t} (B)");
            Statement(script, $"INSERT INTO {t} (ID, B) SELECT ID, G FROM QGS");
            Statement(script, $"INSERT INTO {t} VALUES (20, 0x01000000)");
            foreach (string condition in (string[])["t.B = s.G", "s.G = t.B"])
                Rows(script, $"SELECT t.ID, s.ID FROM {t} AS t, QGS AS s WHERE {condition} ORDER BY t.ID, s.ID");
        }

        SameThroughBothEngines(script);
    }

    // The words of ACE's own refusal, which the parity tests reduce to a bare refusal: a GUID column refuses bytes too
    // few for a GUID as too small for the field and a number of another type as a type mismatch, and a Yes/No
    // column's DEFAULT is refused when it is declared with an overflow, or a type mismatch for one beyond a Long.
    [Fact]
    public void ACE_words_a_refused_GUID_value_and_a_refused_YesNo_or_GUID_DEFAULT_as_the_spec_states()
    {
        string source = Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb");
        bool bigint = AceTestDatabase.SupportsColumnType(source, "BIGINT");
        var cases = new List<(string Setup, string Sql, string Words)>();
        int n = 0;
        foreach ((string value, string words) in ((string, string)[])
            [
                ("1.5", "field is too small"), ("CInt(1)", "Data type mismatch"), ("True", "Data type mismatch"),
                ("CSng(1.5)", "Data type mismatch"), ("CDbl(1.5)", "Data type mismatch"), ("CCur(1)", "Data type mismatch"),
            ])
        {
            string t = "W" + n++;
            cases.Add(($"CREATE TABLE {t} (ID LONG, V GUID)", $"INSERT INTO {t} VALUES (1, {value})", words));
        }
        if (bigint)
        {
            string s = "W" + n++, t = "W" + n++;
            cases.Add(($"CREATE TABLE {s} (ID LONG, X BIGINT)", $"INSERT INTO {s} VALUES (1, 1)", ""));
            cases.Add(($"CREATE TABLE {t} (ID LONG, V GUID)", $"INSERT INTO {t} (ID, V) SELECT ID, X FROM {s}", "field is too small"));
        }
        foreach ((string sql, string value, string words) in ((string, string, string)[])
            [
                ("YESNO", "32768", "Overflow"), ("YESNO", "-32769", "Overflow"), ("YESNO", "70000", "Overflow"),
                ("YESNO", "CLng(70000)", "Overflow"), ("YESNO", "CDbl(70000)", "Overflow"), ("YESNO", "#1/1/2000#", "Overflow"),
                ("YESNO", "Now()", "Overflow"), ("YESNO", "3000000000", "Type mismatch"),
                ("GUID", "CCur(1)", "Type mismatch"), ("GUID", "CDbl(1.5)", "Type mismatch"), ("GUID", "True", "Type mismatch"),
            ])
        {
            string c = "W" + n++, a = "W" + n++, s = "W" + n++;
            cases.Add(("", $"CREATE TABLE {c} (ID LONG, V {sql} DEFAULT {value})", words));
            cases.Add(($"CREATE TABLE {a} (ID LONG)", $"ALTER TABLE {a} ADD COLUMN V {sql} DEFAULT {value}", words));
            cases.Add(($"CREATE TABLE {s} (ID LONG, V {sql})", $"ALTER TABLE {s} ALTER COLUMN V SET DEFAULT {value}", words));
        }

        string path = TemporaryDatabase.CopyPath(source, "boolwords-ace-");
        try
        {
            var wrong = new List<string>();
            using (OleDbConnection c = AceTestDatabase.Open(path))
                foreach ((string setup, string sql, string words) in cases)
                {
                    if (setup.Length > 0) Exec(c, setup);
                    string outcome = Exec(c, sql);
                    bool refusal = outcome.StartsWith("ERR", StringComparison.Ordinal);
                    if (words.Length == 0 ? refusal : !refusal || !outcome.Contains(words, StringComparison.Ordinal))
                        wrong.Add($"{sql}\n    expected {(words.Length == 0 ? "ok" : "\"" + words + "\"")}, ACE: {outcome}");
                }
            OleDbConnection.ReleaseObjectPool();
            Assert.True(wrong.Count == 0, $"{wrong.Count} of {cases.Count} statements are not worded as stated:\n" + string.Join("\n", wrong));
        }
        finally
        {
            TemporaryDatabase.Delete(path);
        }
    }

    // A Large Number (BIGINT) stored into a binary column is the bytes of its text, and into a GUID column those bytes,
    // too few for a GUID - by INSERT ... SELECT, UPDATE through a join and ALTER COLUMN, as a UNION beside a binary
    // column gives it.
    [Fact]
    public void A_BIGINT_stored_into_a_binary_or_GUID_column_is_what_ACE_stores_by_every_statement_that_stores_it()
    {
        Assert.SkipUnless(
            AceTestDatabase.SupportsColumnType(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "BIGINT"),
            AceTestDatabase.UnsupportedColumnTypeReason("BIGINT"));

        var script = new List<Step>();
        Statement(script, "CREATE TABLE QL (ID LONG, X BIGINT)");
        Statement(script, "INSERT INTO QL VALUES (1, 1)");
        int n = 0;
        foreach (string target in (string[])["BINARY(4)", "BINARY(16)", "VARBINARY(16)", "GUID"])
        {
            string t = "QL" + n++, a = "QL" + n++;
            Statement(script, $"CREATE TABLE {t} (ID LONG, V {target})");
            foreach (string value in (string[])["X", "X * 2"])
            {
                Statement(script, $"DELETE FROM {t}");
                Statement(script, $"INSERT INTO {t} (ID, V) SELECT ID, {value} FROM QL");
                Rows(script, $"SELECT V FROM {t}");
            }
            Statement(script, $"DELETE FROM {t}");
            Statement(script, $"INSERT INTO {t} (ID) VALUES (1)");
            Statement(script, $"UPDATE {t} INNER JOIN QL ON {t}.ID = QL.ID SET {t}.V = QL.X");
            Rows(script, $"SELECT V FROM {t}");
            Statement(script, $"CREATE TABLE {a} (ID LONG, X BIGINT)");
            Statement(script, $"INSERT INTO {a} VALUES (1, 1)");
            Statement(script, $"ALTER TABLE {a} ALTER COLUMN X {target}");
            Rows(script, $"SELECT X FROM {a}");
            if (target != "GUID")
                Rows(script, $"SELECT V FROM {t} UNION ALL SELECT X FROM QL");
        }

        // Values whose text is wider than the column: refused by every statement that stores one.
        foreach (string seed in (string[])["123456789", "-123456789"])
            foreach (string target in (string[])["BINARY(4)", "BINARY(16)", "VARBINARY(16)"])
            {
                string s = "QL" + n++, t = "QL" + n++;
                Statement(script, $"CREATE TABLE {s} (ID LONG, X BIGINT)");
                Statement(script, $"INSERT INTO {s} VALUES (1, {seed})");
                Statement(script, $"CREATE TABLE {t} (ID LONG, V {target})");
                foreach (string value in (string[])["X", "X + 0"])
                {
                    Statement(script, $"DELETE FROM {t}");
                    Statement(script, $"INSERT INTO {t} (ID, V) SELECT ID, {value} FROM {s}");
                    Rows(script, $"SELECT V FROM {t}");
                }
                Statement(script, $"DELETE FROM {t}");
                Statement(script, $"INSERT INTO {t} (ID) VALUES (1)");
                Statement(script, $"UPDATE {t} INNER JOIN {s} ON {t}.ID = {s}.ID SET {t}.V = {s}.X");
                Rows(script, $"SELECT V FROM {t}");
                Statement(script, $"ALTER TABLE {s} ALTER COLUMN X {target}");
                Rows(script, $"SELECT X FROM {s}");
            }

        // A binary column compared with a BIGINT column's value, as it is and computed, with an index on the binary
        // column and without.
        Statement(script, "CREATE TABLE QLS (ID LONG, L BIGINT)");
        Statement(script, "INSERT INTO QLS VALUES (1, 123456789)");
        Statement(script, "INSERT INTO QLS VALUES (2, 1)");
        foreach (bool indexed in (bool[])[false, true])
        {
            string t = "QL" + n++;
            Statement(script, $"CREATE TABLE {t} (ID LONG, B VARBINARY(50))");
            if (indexed) Statement(script, $"CREATE INDEX IX_{t} ON {t} (B)");
            Statement(script, $"INSERT INTO {t} (ID, B) SELECT ID, L FROM QLS");
            Statement(script, $"INSERT INTO {t} VALUES (20, 0x01000000)");
            Statement(script, $"INSERT INTO {t} VALUES (21, 0x15CD5B0700000000)");
            foreach (string condition in (string[])["t.B = s.L", "t.B = s.L + 0", "s.L = t.B", "s.L + 0 = t.B"])
                Rows(script, $"SELECT t.ID, s.ID FROM {t} AS t, QLS AS s WHERE {condition} ORDER BY t.ID, s.ID");
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

    // S has no index beside its primary key; SI has one on Y, SC on (Y, ID), SK on (ID, Y), SN on (N, Y), SX one on Y
    // and one on X. TV holds the values looked up in them.
    private static readonly string[] IndexedTables = ["S", "SI", "SC", "SK", "SN", "SX"];

    private static void IndexedYesNoTables(List<Step> script)
    {
        foreach (string s in IndexedTables)
        {
            Statement(script, $"CREATE TABLE {s} (ID LONG PRIMARY KEY, Y YESNO, N LONG, X LONG, T TEXT(10), D DATETIME)");
            Statement(script, $"INSERT INTO {s} VALUES (1, True, 1, 1, 'a', #1/1/2000#)");
            Statement(script, $"INSERT INTO {s} VALUES (2, False, 2, 2, NULL, NULL)");
        }
        Statement(script, "CREATE INDEX IX_SI ON SI (Y)");
        Statement(script, "CREATE INDEX IX_SC ON SC (Y, ID)");
        Statement(script, "CREATE INDEX IX_SK ON SK (ID, Y)");
        Statement(script, "CREATE INDEX IX_SN ON SN (N, Y)");
        Statement(script, "CREATE INDEX IX_SXY ON SX (Y)");
        Statement(script, "CREATE INDEX IX_SXX ON SX (X)");
        Statement(script, "CREATE TABLE TV (ID LONG PRIMARY KEY, V LONG)");
        foreach ((int id, string v) in ((int, string)[])[(1, "-1"), (2, "1"), (3, "0"), (4, "NULL"), (5, "2")])
            Statement(script, $"INSERT INTO TV VALUES ({id}, {v})");
    }

    [Fact]
    public void A_written_Null_on_the_right_of_unequal_selects_the_rows_ACE_selects()
    {
        var script = new List<Step>();
        IndexedYesNoTables(script);
        foreach (string column in (string[])["Y", "N", "T", "D", "ID"])
        {
            Rows(script, $"SELECT ID FROM S WHERE {column} <> NULL ORDER BY ID");
            Rows(script, $"SELECT ID FROM S WHERE NULL <> {column} ORDER BY ID");
            Rows(script, $"SELECT ID FROM S WHERE NOT ({column} <> NULL) ORDER BY ID");
            Rows(script, $"SELECT ID FROM S WHERE {column} <> @p ORDER BY ID", new P(DBNull.Value, null));
            Rows(script, $"SELECT ID FROM S WHERE {column} = NULL ORDER BY ID");
        }

        SameThroughBothEngines(script);
    }

    [Fact]
    public void A_YesNo_column_compared_beside_other_conditions_selects_the_rows_ACE_selects_whichever_index_it_has()
    {
        var script = new List<Step>();
        IndexedYesNoTables(script);
        foreach (string s in IndexedTables)
        {
            foreach (string where in (string[])[
                "ID = 1 AND Y >= 2", "ID < 3 AND Y >= 2", "N = 1 AND Y >= 2", "N = 1 AND ID = 1 AND Y >= 2", "X = 1 AND Y >= 2", "Y = 2 AND ID = 1",
                "Y = 2 OR N = 99", "Y = 2 OR ID = 5", "Y = 2 OR Y = 0", "(Y = 2 OR N = 99) AND ID = 1", "Y = 2 OR X = 99", "Y >= 2 OR N = 99",
                "Y IN (2) OR N = 99", "NOT (Y <> 2)", "Y = 2 AND (N = 1 OR N = 99)", "Y = 2 OR (ID = 5 AND N = 5)"])
                Rows(script, $"SELECT ID FROM {s} WHERE {where} ORDER BY ID");

            foreach (string inner in (string[])[
                "s.Y = TV.V", "s.Y >= TV.V", "s.Y = 2", "s.Y = 2 AND s.ID = 1",
                "s.ID = 1 AND s.Y = TV.V", "s.ID = 1 AND s.Y >= TV.V", "s.ID < 3 AND s.Y = TV.V", "s.ID < 3 AND s.Y >= TV.V",
                "s.N = 1 AND s.Y = TV.V", "s.N = 1 AND s.ID = 1 AND s.Y = TV.V", "s.X = 1 AND s.Y = TV.V", "s.X < 3 AND s.Y >= TV.V",
                "s.ID = TV.ID AND s.Y = 2", "s.N = TV.ID AND s.Y = 2", "s.X = TV.ID AND s.Y = 2", "s.ID = TV.V AND s.Y = 2",
                "s.ID = TV.ID AND s.Y >= 2", "s.ID = TV.ID AND s.Y = TV.V", "s.N = TV.ID AND s.Y = TV.V", "s.ID = TV.ID AND s.Y >= TV.V",
                "s.ID < TV.ID + 9 AND s.Y = 2", "s.ID < TV.ID + 9 AND s.Y = TV.V", "s.N = 1 AND s.ID = TV.ID AND s.Y = 2"])
            {
                string body = inner.Replace("s.", s + ".", StringComparison.Ordinal);
                Rows(script, $"SELECT TV.ID, (SELECT COUNT(*) FROM {s} WHERE {body}) FROM TV ORDER BY TV.ID");
                Rows(script, $"SELECT TV.ID FROM TV WHERE EXISTS (SELECT * FROM {s} WHERE {body}) ORDER BY TV.ID");
                Rows(script, $"SELECT TV.ID FROM TV WHERE NOT EXISTS (SELECT * FROM {s} WHERE {body}) ORDER BY TV.ID");
            }
        }

        SameThroughBothEngines(script);
    }

    // The one row of SL a subquery can match is the last outer row read, 20,000 rows in: by then LibRed has stopped
    // reading the subquery for each outer row and reads it once for them all, and the two readings must agree.
    [Fact]
    public void A_correlated_subquery_over_a_YesNo_column_answers_as_ACE_answers_it_over_a_large_outer_table()
    {
        var script = new List<Step>();
        Statement(script, "CREATE TABLE DG (D LONG)");
        for (int d = 0; d < 10; d++)
            Statement(script, $"INSERT INTO DG VALUES ({d})");
        Statement(script, "CREATE TABLE TL (ID LONG PRIMARY KEY, V LONG)");
        Statement(script, "INSERT INTO TL (ID, V) SELECT q.K, q.K FROM (SELECT a.D + 10 * b.D + 100 * c.D + 1000 * d.D + 10000 * e.D + 1 AS K"
            + " FROM DG AS a, DG AS b, DG AS c, DG AS d, DG AS e) AS q WHERE q.K <= 20000");
        Statement(script, "CREATE TABLE SL (ID LONG PRIMARY KEY, Y YESNO, N LONG)");
        Statement(script, "CREATE INDEX IX_SL ON SL (Y)");
        Statement(script, "INSERT INTO SL VALUES (20000, True, 20000)");
        Statement(script, "INSERT INTO SL VALUES (2, False, 2)");
        foreach (string body in (string[])["SL.ID = TL.ID AND SL.Y = 2", "SL.N = TL.ID AND SL.Y = 2", "SL.ID = TL.ID AND SL.Y = True", "SL.ID = TL.ID AND SL.N > 0"])
            foreach (string outer in (string[])["TL.ID > 19990", "TL.ID > 0"])
            {
                Rows(script, $"SELECT COUNT(*) FROM TL WHERE {outer} AND NOT EXISTS (SELECT * FROM SL WHERE {body})");
                Rows(script, $"SELECT COUNT(*) FROM TL WHERE {outer} AND (SELECT COUNT(*) FROM SL WHERE {body}) > 0");
            }

        SameThroughBothEngines(script);
    }

    // A correlated count over a Yes/No column, with an index and without, compared with the outer row's number or Yes/No
    // value, and the same column on an outer join's unmatched side. Over TL, 20,000 rows, LibRed stops reading the
    // count for each outer row partway and reads it once for them all; the two readings must agree with ACE.
    [Fact]
    public void A_correlated_count_over_a_YesNo_column_answers_as_ACE_answers_it_over_a_small_and_a_large_outer_table()
    {
        var script = new List<Step>();
        Statement(script, "CREATE TABLE DG (D LONG)");
        for (int d = 0; d < 10; d++)
            Statement(script, $"INSERT INTO DG VALUES ({d})");
        Statement(script, "CREATE TABLE TL (ID LONG PRIMARY KEY, V LONG, B YESNO)");
        Statement(script, "INSERT INTO TL (ID, V, B) SELECT q.K, IIf(q.K Mod 5 = 0, Null, (q.K Mod 4) - 1), (q.K Mod 2 = 0) FROM (SELECT a.D"
            + " + 10 * b.D + 100 * c.D + 1000 * d.D + 10000 * e.D + 1 AS K FROM DG AS a, DG AS b, DG AS c, DG AS d, DG AS e) AS q WHERE q.K <= 20000");
        Statement(script, "CREATE TABLE TS (ID LONG PRIMARY KEY, V LONG, B YESNO)");
        foreach (string row in (string[])["1, 0, False", "2, 1, True", "3, 2, True", "4, -1, True", "5, Null, False"])
            Statement(script, $"INSERT INTO TS VALUES ({row})");
        foreach (string s in (string[])["SI", "SU"])
        {
            Statement(script, $"CREATE TABLE {s} (ID LONG PRIMARY KEY, Y YESNO)");
            Statement(script, $"INSERT INTO {s} VALUES (1, True)");
            Statement(script, $"INSERT INTO {s} VALUES (2, False)");
        }
        Statement(script, "CREATE INDEX IX_SI ON SI (Y)");
        Statement(script, "CREATE TABLE A (ID LONG)");
        foreach (int id in (int[])[1, 2, 3])
            Statement(script, $"INSERT INTO A VALUES ({id})");

        foreach (string s in (string[])["SI", "SU"])
            foreach (string column in (string[])["V", "B"])
                foreach (string from in (string[])[s, $"A LEFT JOIN {s} ON A.ID = {s}.ID"])
                {
                    Rows(script, $"SELECT TS.ID, (SELECT COUNT(*) FROM {from} WHERE {s}.Y = TS.{column}) FROM TS ORDER BY TS.ID");
                    foreach (string test in (string[])["> 0", "= 0"])
                        Rows(script, $"SELECT TL.{column}, COUNT(*) FROM TL WHERE (SELECT COUNT(*) FROM {from} WHERE {s}.Y = TL.{column}) {test}"
                            + $" GROUP BY TL.{column} ORDER BY TL.{column}");
                }

        SameThroughBothEngines(script);
    }

    [Fact]
    public void A_HAVING_condition_on_a_grouping_YesNo_column_keeps_the_groups_ACE_keeps()
    {
        var script = new List<Step>();
        IndexedYesNoTables(script);
        foreach (string s in IndexedTables)
        {
            foreach (string having in (string[])[
                "Y = 2", "Y = 2 AND COUNT(*) > 0", "COUNT(*) > 0 AND Y = 2", "Y = CLng(2)", "Y <> 2", "Y = 2 OR COUNT(*) > 99", "2 = Y", "Y IN (2)",
                "MAX(ID) > 0 AND Y = 2", "Y >= 2", "Y BETWEEN 2 AND 3", "NOT (Y = 2)", "Y = 2 OR Y = 0", "Y + 0 = 2"])
                Rows(script, $"SELECT Y, COUNT(*) FROM {s} GROUP BY Y HAVING {having}");
            foreach (string having in (string[])["Y = 2", "Y = 2 AND N = 1", "(Y = 2 OR N = 99)", "Y = 2 AND SUM(N) > 0 AND N > 0"])
                Rows(script, $"SELECT Y, N, COUNT(*) FROM {s} GROUP BY Y, N HAVING {having}");
            Rows(script, $"SELECT Y, COUNT(*) FROM {s} GROUP BY Y HAVING Y = @p", new P(2, null));
            Rows(script, $"SELECT Y, COUNT(*) FROM {s} WHERE ID = 1 GROUP BY Y HAVING Y = 2");
            Rows(script, $"SELECT Y, COUNT(*) FROM {s} WHERE N > 0 GROUP BY Y HAVING Y = 2");
            Rows(script, $"SELECT COUNT(*) FROM {s} GROUP BY Y HAVING Y = 2");
            Rows(script, $"SELECT Y, COUNT(*) FROM {s} GROUP BY Y, ID + 0 HAVING Y = 2");
            Rows(script, $"SELECT d.Y, d.C FROM (SELECT Y, COUNT(*) AS C FROM {s} GROUP BY Y) AS d WHERE d.Y = 2");
            Rows(script, $"SELECT d.Y FROM (SELECT DISTINCT Y FROM {s}) AS d WHERE d.Y = 2");
            Rows(script, $"SELECT TV.ID, (SELECT COUNT(*) FROM (SELECT Y FROM {s} GROUP BY Y HAVING Y = 2) AS g) FROM TV WHERE TV.ID = 1");
        }

        SameThroughBothEngines(script);
    }

    [Fact]
    public void A_HAVING_column_written_with_or_without_its_table_matches_the_grouping_column_as_ACE_matches_it()
    {
        var script = new List<Step>();
        IndexedYesNoTables(script);
        foreach (string s in IndexedTables)
        {
            Rows(script, $"SELECT {s}.Y, COUNT(*) FROM {s} GROUP BY {s}.Y HAVING Y = 2");
            Rows(script, $"SELECT Y, COUNT(*) FROM {s} GROUP BY Y HAVING {s}.Y = 2");
            Rows(script, $"SELECT a.Y, COUNT(*) FROM {s} AS a GROUP BY a.Y HAVING Y = 2");
            Rows(script, $"SELECT Y, COUNT(*) FROM {s} AS a GROUP BY Y HAVING a.Y = 2");
            Rows(script, $"SELECT {s}.Y, COUNT(*) FROM {s} GROUP BY {s}.Y HAVING {s}.Y = 2");
            Rows(script, $"SELECT {s}.Y, COUNT(*) FROM {s} INNER JOIN TV ON {s}.N = TV.ID GROUP BY {s}.Y HAVING Y = 2");
        }

        SameThroughBothEngines(script);
    }

    // A value beside a binary column in a UNION is its own bytes, as a binary column stores it, and a parameter of a
    // type Access has no column for is its text.
    [Fact]
    public void A_value_in_a_UNION_with_a_binary_column_is_its_bytes_as_ACE_writes_them()
    {
        var script = new List<Step>();
        Statement(script, "CREATE TABLE CXB (ID LONG, V BINARY(4))");
        Statement(script, "INSERT INTO CXB VALUES (1, 0x01000000)");
        Statement(script, "CREATE TABLE ONE (ID LONG)");
        Statement(script, "INSERT INTO ONE VALUES (1)");
        foreach (string x in (string[])["CInt(-1)", "CLng(1)", "CSng(1)", "CDbl(1)", "#1/1/2000#", "'A'", "True", "CCur(1)", "1.5",
            "3000000000", "{guid {00000001-0002-0003-0004-000000000005}}"])
            Rows(script, $"SELECT V FROM CXB UNION ALL SELECT {x} FROM ONE");
        foreach (object parameter in (object[])[(sbyte)1, (ushort)1, 1u, 1ul, 'A'])
            Rows(script, "SELECT V FROM CXB UNION ALL SELECT @p FROM ONE", new P(parameter, null));

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

    // Each column of a reader's schema: its name, and N (AllowDBNull) or R (not). The type is left out, so that ACE's
    // Integer (Int16) for a comparison against LibRed's Boolean does not decide the row.
    internal static string ReaderNullability(DbConnection c, string sql, CommandBehavior behavior)
    {
        try
        {
            using DbCommand cmd = Command(c, sql, null);
            using DbDataReader r = cmd.ExecuteReader(behavior);
            return string.Join(" ", r.GetSchemaTable()!.Rows.Cast<DataRow>()
                .Select(row => $"{row["ColumnName"]}:{((bool)row["AllowDBNull"] ? "N" : "R")}"));
        }
        catch (Exception ex) { return Err(ex); }
    }

    // Without KeyInfo (Default, SchemaOnly) ACE's reader calls every column nullable, whatever the column, the
    // expression or the query. Under KeyInfo it calls a column not nullable only when every output reads stored
    // columns of one table, and the outputs reading exactly one stored column cover that table's primary key: the
    // first such output for each key column.
    [Fact]
    public void A_reader_schema_calls_every_column_nullable_but_a_single_tables_key_under_KeyInfo_as_ACE_does()
    {
        var script = new List<Step>();
        Statement(script, "CREATE TABLE YS (ID LONG PRIMARY KEY, F YESNO, FR YESNO NOT NULL, FD YESNO DEFAULT True, T TEXT(10), TR TEXT(10) NOT NULL, C COUNTER)");
        Statement(script, "INSERT INTO YS (ID, F, FR, FD, T, TR) VALUES (1, True, True, True, 'a', 'b')");
        Statement(script, "INSERT INTO YS (ID, F, FR, FD, T, TR) VALUES (2, False, False, False, NULL, 'b')");
        Statement(script, "CREATE VIEW VY AS SELECT ID, F, FR, FD, T, TR FROM YS");
        Statement(script, "CREATE TABLE L (ID LONG PRIMARY KEY)");
        foreach (int id in (int[])[1, 2, 3])
            Statement(script, $"INSERT INTO L VALUES ({id})");
        Statement(script, "CREATE TABLE K2 (A LONG, B LONG, U LONG NOT NULL, V LONG, CONSTRAINT PK_K2 PRIMARY KEY (A, B))");
        Statement(script, "CREATE UNIQUE INDEX UX_K2 ON K2 (U)");
        Statement(script, "INSERT INTO K2 VALUES (1, 1, 1, 1)");
        Statement(script, "CREATE TABLE K3 (U LONG NOT NULL, V LONG)");
        Statement(script, "CREATE UNIQUE INDEX UX_K3 ON K3 (U)");
        Statement(script, "INSERT INTO K3 VALUES (1, 1)");

        string[] queries =
        [
            "SELECT * FROM YS",
            "SELECT * FROM VY",
            "SELECT F, FR, NOT F AS NF, (ID = 1) AS E, IIF(F, 1, 0) AS I, T, TR FROM YS",
            "SELECT ID, 1 AS K, 'x' AS S, (1 = 1) AS B, NULL AS Z, ID + 1 AS P, -ID AS NI, ID & '' AS Q, (ID = C) AS EC, C + 1 AS PC, IIF(ID = 1, 1, 0) AS I1 FROM YS",
            "SELECT ID, COUNT(*) AS N, MAX(ID) AS M FROM YS GROUP BY ID",
            "SELECT DISTINCT ID FROM YS",
            "SELECT A, B, U, V, A + B AS AB FROM K2",
            "SELECT U, V FROM K3",
            "SELECT YS.ID, L.ID FROM L INNER JOIN YS ON L.ID = YS.ID",
            "SELECT YS.ID, L.ID FROM L LEFT JOIN YS ON L.ID = YS.ID",
            "SELECT D.ID FROM (SELECT ID FROM YS) AS D",
            "SELECT o.OrderID, o.CustomerID AS Cust, c.CompanyName, o.Freight * 2 AS Doubled FROM Orders AS o INNER JOIN Customers AS c ON o.CustomerID = c.CustomerID",
            "SELECT (ID = 1) AS E FROM YS",
            "SELECT (ID = 1) AS E, ID FROM YS",
            "SELECT ID, (ID = 1) AS E FROM YS",
            "SELECT ID + 1 AS P FROM YS",
            "SELECT (ID = C) AS EC FROM YS",
            "SELECT (C = ID) AS CE FROM YS",
            "SELECT A + 1 AS A1 FROM K2",
            "SELECT ID AS A1, ID AS A2 FROM YS",
            "SELECT YS.ID FROM L INNER JOIN YS ON L.ID = YS.ID",
            "SELECT ID FROM YS UNION SELECT ID FROM YS",
            "SELECT ID FROM YS WHERE EXISTS (SELECT 1 FROM L WHERE L.ID = YS.ID)",
        ];
        foreach (CommandBehavior behavior in (CommandBehavior[])[CommandBehavior.Default, CommandBehavior.SchemaOnly,
            CommandBehavior.KeyInfo, CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo])
            foreach (string sql in queries)
                script.Add(new($"Reader nullability of {sql} [{behavior}]", c => ReaderNullability(c, sql, behavior)));

        SameThroughBothEngines(script);
    }

    // Each statement runs on a freshly made and seeded table with an index on V: a Long (Int32) column holding -1, 1,
    // 0, Null and 2, a BINARY(4) one holding 0x01000000, 0xFFFF0000 and Null, or a Yes/No one holding True and False.
    private static readonly (string Columns, string[] Seed) IndexedLong = ("ID LONG PRIMARY KEY, V LONG", ["-1", "1", "0", "NULL", "2"]);
    private static readonly (string Columns, string[] Seed) IndexedBinary = ("ID LONG, V BINARY(4)", ["0x01000000", "0xFFFF0000", "NULL"]);
    private static readonly (string Columns, string[] Seed) IndexedYesNo = ("ID LONG PRIMARY KEY, V YESNO", ["True", "False"]);

    private static void OnFreshIndexedTable(List<Step> script, ref int n, (string Columns, string[] Seed) table, Func<string, string> sql, P? p = null)
    {
        string t = "DM" + n++;
        Statement(script, $"CREATE TABLE {t} ({table.Columns})");
        Statement(script, $"CREATE INDEX IX_{t} ON {t} (V)");
        for (int i = 0; i < table.Seed.Length; i++) Statement(script, $"INSERT INTO {t} VALUES ({i + 1}, {table.Seed[i]})");
        string statement = sql(t);
        if (statement.StartsWith("SELECT", StringComparison.Ordinal))
            Rows(script, statement, p);
        else
        {
            Statement(script, statement, p);
            Rows(script, $"SELECT * FROM {t} ORDER BY ID");
        }
    }

    // UPDATE and DELETE find their rows through the index as a SELECT does: a value the column cannot hold finds no
    // row, and a single-table UPDATE changes only the rows that also pass its WHERE as written.
    [Fact]
    public void A_DML_WHERE_seeks_as_the_SELECT_with_the_same_WHERE_does()
    {
        var script = new List<Step>();
        int n = 0;
        OnFreshIndexedTable(script, ref n, IndexedLong, t => $"SELECT ID FROM {t} WHERE V = 3000000000 ORDER BY ID");
        OnFreshIndexedTable(script, ref n, IndexedLong, t => $"UPDATE {t} SET ID = ID WHERE V = 3000000000");
        OnFreshIndexedTable(script, ref n, IndexedLong, t => $"DELETE FROM {t} WHERE V = 3000000000");

        OnFreshIndexedTable(script, ref n, IndexedBinary, t => $"UPDATE {t} SET ID = ID + 10 WHERE V = CLng(1)");
        OnFreshIndexedTable(script, ref n, IndexedYesNo, t => $"UPDATE {t} SET ID = ID + 10 WHERE V = CLng(1)");
        OnFreshIndexedTable(script, ref n, IndexedYesNo, t => $"UPDATE {t} SET ID = ID + 10 WHERE V = 2");
        OnFreshIndexedTable(script, ref n, IndexedLong, t => $"UPDATE {t} SET ID = ID + 10 WHERE V = CLng(1)");
        foreach (var table in ((string, string[])[])[IndexedBinary, IndexedYesNo, IndexedLong])
        {
            OnFreshIndexedTable(script, ref n, table, t => $"UPDATE {t} SET ID = ID WHERE V >= @p", new P(1, null));
            OnFreshIndexedTable(script, ref n, table, t => $"UPDATE {t} SET ID = ID WHERE V IN (CLng(1))");
        }
        OnFreshIndexedTable(script, ref n, IndexedLong, t => $"UPDATE {t} SET ID = ID WHERE V = @p", new P("1.5", null));

        SameThroughBothEngines(script);
    }

    private static readonly DbType[] TextDbTypes =
        [DbType.AnsiString, DbType.Object, DbType.String, DbType.AnsiStringFixedLength, DbType.StringFixedLength];

    // Text a parameter or function gives, compared with a number or date column ACE answers from an index, is taken as
    // storing it in the column takes it: text that cannot be stored there finds no row under =, and is a type mismatch
    // as a range bound. Without the index the comparison reads the text as a number.
    [Fact]
    public void A_text_compared_with_an_indexed_number_or_date_column_finds_what_the_index_finds()
    {
        var script = new List<Step>();
        foreach ((string name, string sql) in ((string, string)[])[("Byte", "BYTE"), ("Short", "SHORT"), ("Long", "LONG"), ("Single", "SINGLE"),
            ("Double", "DOUBLE"), ("Cur", "CURRENCY"), ("Dec", "DECIMAL(18,2)"), ("Date", "DATETIME")])
            foreach (bool indexed in (bool[])[false, true])
            {
                // The unindexed date column takes text by other rules, not these.
                if (name == "Date" && !indexed) continue;
                string t = (indexed ? "I_" : "U_") + name;
                Statement(script, $"CREATE TABLE {t} (ID LONG PRIMARY KEY, V {sql})");
                if (indexed) Statement(script, $"CREATE INDEX IX_{t} ON {t} (V)");
                string[] seed = name switch
                {
                    "Byte" => ["255", "1", "0", "NULL", "2"],
                    "Date" => ["#12/29/1899#", "#12/31/1899#", "#12/30/1899#", "NULL"],
                    _ => ["-1", "1", "0", "NULL", "2"],
                };
                for (int i = 0; i < seed.Length; i++)
                    Statement(script, $"INSERT INTO {t} (ID, V) VALUES ({i + 1}, {seed[i]})");

                Rows(script, $"SELECT ID FROM {t} WHERE V = @p ORDER BY ID", new P("True", null));
                foreach (bool value in (bool[])[true, false])
                    foreach (DbType declared in name == "Date"
                        ? [.. TextDbTypes, DbType.Decimal, DbType.SByte, DbType.UInt16, DbType.UInt32, DbType.UInt64]
                        : TextDbTypes)
                        Rows(script, $"SELECT ID FROM {t} WHERE V = @p ORDER BY ID", new P(value, declared));
                if (name == "Date")
                    Rows(script, $"SELECT ID FROM {t} WHERE V = @p ORDER BY ID", new P("1", null));

                if (name is not ("Byte" or "Long" or "Double" or "Date"))
                    continue;
                foreach (string op in (string[])["<", ">="])
                    foreach (string text in (string[])["True", "abc", "1.5", "1"])
                    {
                        // The row a '1.5' bound finds is re-tested against 1.5 rather than the date it was read as.
                        if (name == "Date" && text == "1.5") continue;
                        Rows(script, $"SELECT ID FROM {t} WHERE V {op} @p ORDER BY ID", new P(text, null));
                    }
                foreach (string text in (string[])["abc", " 1 ", "1.5", ""])
                {
                    // The row '1.5' rounds to is re-tested against 1.5.
                    if (indexed && text == "1.5" && name is "Byte" or "Long") continue;
                    Rows(script, $"SELECT ID FROM {t} WHERE V = @p ORDER BY ID", new P(text, null));
                }
                if (name is "Long" or "Date")
                {
                    Rows(script, $"SELECT ID FROM {t} WHERE V = CStr(1) ORDER BY ID");
                    Rows(script, $"SELECT ID FROM {t} WHERE V = 'Tr' & 'ue' ORDER BY ID");
                }
                if (name == "Long")
                {
                    Rows(script, $"SELECT ID FROM {t} WHERE V = @p & '' ORDER BY ID", new P("1", null));
                    Rows(script, $"SELECT ID FROM {t} WHERE V = '' & @p ORDER BY ID", new P("1", null));
                }
                if (name == "Date")
                {
                    foreach (string text in (string[])["36526", "2000-01-01"])
                        Rows(script, $"SELECT ID FROM {t} WHERE V = @p ORDER BY ID", new P(text, null));
                    Rows(script, $"SELECT ID FROM {t} WHERE V > @p ORDER BY ID", new P("12/31/1899", null));
                }
            }

        int n = 0;
        OnFreshIndexedTable(script, ref n, IndexedLong, t => $"SELECT ID FROM {t} WHERE V = @p ORDER BY ID", new P("True", null));
        OnFreshIndexedTable(script, ref n, IndexedLong, t => $"UPDATE {t} SET ID = ID WHERE V = @p", new P("True", null));
        OnFreshIndexedTable(script, ref n, IndexedLong, t => $"DELETE FROM {t} WHERE V = @p", new P("True", null));

        SameThroughBothEngines(script);
    }

    // A number or Boolean the statement computes or is passed, compared with a GUID or binary column ACE answers from an
    // index, is taken as storing it in the column takes it - a Long (Int32) a GUID's first four bytes, a number its own
    // bytes without a fixed column's padding, a Currency its eight-byte integer - and one the column refuses finds no
    // row, or is a type mismatch as a range bound. Without the index such a value sorts below every GUID or binary.
    [Fact]
    public void A_number_compared_with_an_indexed_GUID_or_binary_column_is_taken_as_the_column_stores_it()
    {
        var script = new List<Step>();
        YesNoSource(script);
        foreach ((string name, string sql, string one, string minus) in ((string, string, string, string)[])
            [
                ("BIN4", "BINARY(4)", "0x01000000", "0xFFFF0000"), ("BIN8", "BINARY(8)", "0x01000000", "0xFFFF0000"),
                ("VARBIN8", "VARBINARY(8)", "0x01000000", "0xFFFF0000"),
                ("GUID", "GUID", "{guid {00000001-0000-0000-0000-000000000000}}", "{guid {0000FFFF-0000-0000-0000-000000000000}}"),
            ])
            foreach (bool indexed in (bool[])[false, true])
            {
                string t = $"CX_{name}_{(indexed ? "I" : "U")}";
                Statement(script, $"CREATE TABLE {t} (ID LONG, V {sql})");
                if (indexed) Statement(script, $"CREATE INDEX IX_{t} ON {t} (V)");
                Statement(script, $"INSERT INTO {t} VALUES (1, {one})");
                Statement(script, $"INSERT INTO {t} VALUES (2, {minus})");
                Statement(script, $"INSERT INTO {t} VALUES (3, NULL)");
                foreach (string condition in (string[])["V = 1", "V = -1", "V = True", "V = False", "V <> True", "V = CInt(True)", "V = (1=1)",
                    "V > True", "V >= 1", "V IN (True)", "V = '1'", "V <> CInt(True)", "V <> (1=0)", "V < CInt(True)", "V >= (1=1)",
                    "V IN (CInt(True))", "V = CLng(1)", "V = CDbl(1)", "NOT (V = CInt(True))", "(V = CInt(True)) IS NULL",
                    "V BETWEEN CLng(0) AND CLng(1)", "V IN (CLng(1), CInt(True))", "V > CLng(1)", "V < False", "V >= False", "V = CCur(1)"])
                    Rows(script, $"SELECT ID FROM {t} WHERE {condition} ORDER BY ID");
                foreach (object parameter in (object[])[1, (short)-1, true])
                    foreach (string op in (string[])["=", "<>", "<", ">=", ">"])
                        Rows(script, $"SELECT ID FROM {t} WHERE V {op} @p ORDER BY ID", new P(parameter, null));
                if (name == "BIN8")
                    foreach (byte[] bytes in (byte[][])[[1, 0, 0, 0], [1, 0, 0, 0, 0, 0, 0, 0]])
                        Rows(script, $"SELECT ID FROM {t} WHERE V = @p ORDER BY ID", new P(bytes, null));
            }
        Rows(script, "SELECT S.ID FROM S WHERE EXISTS (SELECT 1 FROM CX_GUID_I WHERE CX_GUID_I.V = CLng(S.ID)) ORDER BY S.ID");

        // Currency 1 stored into a BINARY(8), and the text '1'.
        Statement(script, "CREATE TABLE CY8_I (ID LONG, V BINARY(8))");
        Statement(script, "CREATE INDEX IX_CY8_I ON CY8_I (V)");
        Statement(script, "INSERT INTO CY8_I VALUES (1, 0x1027000000000000)");
        Statement(script, "INSERT INTO CY8_I VALUES (2, 0x3100000000000000)");
        Rows(script, "SELECT ID FROM CY8_I WHERE V = CCur(1) ORDER BY ID");
        Rows(script, "SELECT ID FROM CY8_I WHERE V >= CCur(1) ORDER BY ID");
        Rows(script, "SELECT ID FROM CY8_I WHERE V = @p ORDER BY ID", new P(1m, null));

        int n = 0;
        OnFreshIndexedTable(script, ref n, IndexedBinary, t => $"SELECT ID FROM {t} WHERE V = @p ORDER BY ID", new P(1, null));
        OnFreshIndexedTable(script, ref n, IndexedBinary, t => $"SELECT ID FROM {t} WHERE V = CLng(1) ORDER BY ID");
        OnFreshIndexedTable(script, ref n, IndexedBinary, t => $"UPDATE {t} SET ID = ID WHERE V = @p", new P(1, null));
        OnFreshIndexedTable(script, ref n, IndexedBinary, t => $"UPDATE {t} SET ID = ID WHERE V = CLng(1)");

        SameThroughBothEngines(script);
    }

    // The rows an index finds for text it rounds or reads as a date are the rows returned: they are not tested again
    // against the text as written.
    [Fact]
    public void A_row_an_index_finds_for_converted_text_is_not_rejected_by_the_text_as_written()
    {
        var script = new List<Step>();
        foreach ((string t, string sql, string[] seed) in ((string, string, string[])[])
            [
                ("I_Byte", "BYTE", ["255", "1", "0", "NULL", "2"]), ("I_Long", "LONG", ["-1", "1", "0", "NULL", "2"]),
                ("I_Date", "DATETIME", ["#12/29/1899#", "#12/31/1899#", "#12/30/1899#", "NULL"]),
            ])
        {
            Statement(script, $"CREATE TABLE {t} (ID LONG PRIMARY KEY, V {sql})");
            Statement(script, $"CREATE INDEX IX_{t} ON {t} (V)");
            for (int i = 0; i < seed.Length; i++)
                Statement(script, $"INSERT INTO {t} (ID, V) VALUES ({i + 1}, {seed[i]})");
        }
        Rows(script, "SELECT ID FROM I_Byte WHERE V = @p ORDER BY ID", new P("1.5", null));
        Rows(script, "SELECT ID FROM I_Long WHERE V = @p ORDER BY ID", new P("1.5", null));
        foreach (string op in (string[])["<", ">="])
            Rows(script, $"SELECT ID FROM I_Date WHERE V {op} @p ORDER BY ID", new P("1.5", null));
        foreach (string value in (string[])["@p", "@p & ''", "'' & @p"])
            Rows(script, $"SELECT ID FROM I_Date WHERE V = {value} ORDER BY ID", new P("12/31/1899", null));
        Rows(script, "SELECT ID FROM I_Date WHERE V = CStr(#12/31/1899#) ORDER BY ID");

        SameThroughBothEngines(script);
    }

    [Fact]
    public void A_text_compared_with_an_indexed_BIGINT_column_finds_what_the_index_finds()
    {
        Assert.SkipUnless(
            AceTestDatabase.SupportsColumnType(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "BIGINT"),
            AceTestDatabase.UnsupportedColumnTypeReason("BIGINT"));

        var script = new List<Step>();
        Statement(script, "CREATE TABLE I_BigInt (ID LONG PRIMARY KEY, V BIGINT)");
        Statement(script, "CREATE INDEX IX_I_BigInt ON I_BigInt (V)");
        string[] seed = ["-1", "1", "0", "NULL", "2"];
        for (int i = 0; i < seed.Length; i++)
            Statement(script, $"INSERT INTO I_BigInt (ID, V) VALUES ({i + 1}, {seed[i]})");

        foreach (string text in (string[])["2", "-1", "abc", "", "True"])
            Rows(script, "SELECT ID FROM I_BigInt WHERE V = @p ORDER BY ID", new P(text, null));
        Rows(script, "SELECT ID FROM I_BigInt WHERE V > @p ORDER BY ID", new P("1", null));
        Rows(script, "SELECT ID FROM I_BigInt WHERE V = CStr(1) ORDER BY ID");
        Rows(script, "SELECT ID FROM I_BigInt WHERE V = @p & '' ORDER BY ID", new P("1", null));
        Rows(script, "SELECT ID FROM I_BigInt WHERE V = '' & @p ORDER BY ID", new P("1", null));
        foreach (bool value in (bool[])[true, false])
            foreach (DbType declared in TextDbTypes)
                Rows(script, "SELECT ID FROM I_BigInt WHERE V = @p ORDER BY ID", new P(value, declared));

        SameThroughBothEngines(script);
    }

    // Text a parameter or function gives, compared with a date column no index answers, is read as date text, and text
    // that is not a date is a type mismatch. Text written in the statement, and an expression over the column, are not.
    [Fact]
    public void A_text_compared_with_an_unindexed_date_column_is_read_as_a_date()
    {
        var script = new List<Step>();
        Statement(script, "CREATE TABLE U_Date (ID LONG PRIMARY KEY, V DATETIME)");
        string[] seed = ["#12/29/1899#", "#12/31/1899#", "#12/30/1899#", "NULL"];
        for (int i = 0; i < seed.Length; i++)
            Statement(script, $"INSERT INTO U_Date (ID, V) VALUES ({i + 1}, {seed[i]})");

        foreach (string op in (string[])["<", ">="])
            foreach (string text in (string[])["True", "abc", "1.5", "1"])
                Rows(script, $"SELECT ID FROM U_Date WHERE V {op} @p ORDER BY ID", new P(text, null));
        foreach (string text in (string[])["abc", " 1 ", "1.5", "", "1", "True", "36526", "2000-01-01", "12/31/1899"])
            Rows(script, "SELECT ID FROM U_Date WHERE V = @p ORDER BY ID", new P(text, null));
        Rows(script, "SELECT ID FROM U_Date WHERE V > @p ORDER BY ID", new P("12/31/1899", null));
        Rows(script, "SELECT ID FROM U_Date WHERE V = CStr(1) ORDER BY ID");
        Rows(script, "SELECT ID FROM U_Date WHERE V = 'Tr' & 'ue' ORDER BY ID");
        Rows(script, "SELECT ID FROM U_Date WHERE V = CStr(#12/31/1899#) ORDER BY ID");
        foreach (string value in (string[])["@p & ''", "'' & @p"])
            Rows(script, $"SELECT ID FROM U_Date WHERE V = {value} ORDER BY ID", new P("12/31/1899", null));
        foreach (bool value in (bool[])[true, false])
            foreach (DbType declared in (DbType[])[.. TextDbTypes, DbType.Decimal, DbType.SByte, DbType.UInt16, DbType.UInt32, DbType.UInt64])
                Rows(script, "SELECT ID FROM U_Date WHERE V = @p ORDER BY ID", new P(value, declared));

        // A single-table UPDATE re-tests the rows its index finds against the WHERE, which reads the text as a date too.
        int n = 0;
        OnFreshIndexedTable(script, ref n, ("ID LONG PRIMARY KEY, V DATETIME", seed),
            t => $"UPDATE {t} SET ID = ID WHERE V = @p", new P("12/31/1899", null));

        SameThroughBothEngines(script);
    }

    // Text a parameter or function gives, compared with a BIGINT column no index answers, reads as 0, whatever it holds;
    // compared with an expression over the column it reads as the number it is.
    [Fact]
    public void A_text_compared_with_an_unindexed_BIGINT_column_reads_as_0()
    {
        Assert.SkipUnless(
            AceTestDatabase.SupportsColumnType(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "BIGINT"),
            AceTestDatabase.UnsupportedColumnTypeReason("BIGINT"));

        var script = new List<Step>();
        Statement(script, "CREATE TABLE U_BigInt (ID LONG PRIMARY KEY, V BIGINT)");
        string[] seed = ["-1", "1", "0", "NULL", "2"];
        for (int i = 0; i < seed.Length; i++)
            Statement(script, $"INSERT INTO U_BigInt (ID, V) VALUES ({i + 1}, {seed[i]})");

        foreach (string text in (string[])["2", "-1", "abc", "", "1.5", "1", "True"])
            Rows(script, "SELECT ID FROM U_BigInt WHERE V = @p ORDER BY ID", new P(text, null));
        Rows(script, "SELECT ID FROM U_BigInt WHERE V > @p ORDER BY ID", new P("1", null));
        Rows(script, "SELECT ID FROM U_BigInt WHERE V = CStr(1) ORDER BY ID");
        Rows(script, "SELECT ID, IIF(V = @p, 'T', 'F') FROM U_BigInt ORDER BY ID", new P("1", null));
        foreach (string value in (string[])["@p & ''", "'' & @p"])
            Rows(script, $"SELECT ID FROM U_BigInt WHERE V = {value} ORDER BY ID", new P("1", null));
        Rows(script, "SELECT ID FROM U_BigInt WHERE V + 0 = @p ORDER BY ID", new P("1", null));
        foreach (bool value in (bool[])[true, false])
            foreach (DbType declared in (DbType[])[.. TextDbTypes, DbType.Decimal, DbType.SByte, DbType.UInt16, DbType.UInt32, DbType.UInt64])
                Rows(script, "SELECT ID FROM U_BigInt WHERE V = @p ORDER BY ID", new P(value, declared));

        SameThroughBothEngines(script);
    }

    // A text or Double parameter compared with a CURRENCY column is rounded to a Currency's four places first, with an
    // index or without; a DECIMAL(18,2) column takes it as it is.
    [Fact]
    public void A_text_or_Double_parameter_compared_with_a_Currency_column_is_rounded_to_a_Currency()
    {
        var script = new List<Step>();
        foreach ((string name, string sql, string[] seed, object[] values) in ((string, string, string[], object[])[])
            [
                ("Cur", "CURRENCY", ["1.2345", "1.2346", "2"], ["1.23455", "1.23456", "1.234549", 1.23455]),
                ("Dec", "DECIMAL(18,2)", ["1.23", "1.24", "2"], ["1.234", "1.235", "1.236", 1.235]),
            ])
            foreach (bool indexed in (bool[])[true, false])
            {
                string t = (indexed ? "I_" : "U_") + name;
                Statement(script, $"CREATE TABLE {t} (ID LONG PRIMARY KEY, V {sql})");
                if (indexed) Statement(script, $"CREATE INDEX IX_{t} ON {t} (V)");
                for (int i = 0; i < seed.Length; i++)
                    Statement(script, $"INSERT INTO {t} (ID, V) VALUES ({i + 1}, {seed[i]})");
                foreach (object value in values)
                    Rows(script, $"SELECT ID FROM {t} WHERE V = @p ORDER BY ID", new P(value, null));
            }

        SameThroughBothEngines(script);
    }

    // A Currency value or a Decimal parameter compared with a binary column no index answers compares as text: the
    // number's text against the binary read as UTF-16.
    [Fact]
    public void A_Currency_or_Decimal_compared_with_an_unindexed_binary_column_compares_as_text()
    {
        var script = new List<Step>();
        // Currency 1 stored into a BINARY(8), and the text '1'.
        Statement(script, "CREATE TABLE CY8_U (ID LONG, V BINARY(8))");
        Statement(script, "INSERT INTO CY8_U VALUES (1, 0x1027000000000000)");
        Statement(script, "INSERT INTO CY8_U VALUES (2, 0x3100000000000000)");
        Rows(script, "SELECT ID FROM CY8_U WHERE V = CCur(1) ORDER BY ID");
        Rows(script, "SELECT ID FROM CY8_U WHERE V = @p ORDER BY ID", new P(1m, DbType.Currency));
        Rows(script, "SELECT ID FROM CY8_U WHERE V = @p ORDER BY ID", new P(1m, null));
        Rows(script, "SELECT ID FROM CY8_U WHERE V >= CCur(1) ORDER BY ID");

        foreach ((string name, string sql) in ((string, string)[])[("BIN4", "BINARY(4)"), ("BIN8", "BINARY(8)"), ("VARBIN8", "VARBINARY(8)")])
        {
            string t = $"CX_{name}_U";
            Statement(script, $"CREATE TABLE {t} (ID LONG, V {sql})");
            Statement(script, $"INSERT INTO {t} VALUES (1, 0x01000000)");
            Statement(script, $"INSERT INTO {t} VALUES (2, 0xFFFF0000)");
            Statement(script, $"INSERT INTO {t} VALUES (3, NULL)");
            Rows(script, $"SELECT ID FROM {t} WHERE V = CCur(1) ORDER BY ID");
        }

        SameThroughBothEngines(script);
    }

    // On columns no index answers, the value a comparison is given is taken the same way by =, an IN list of one or
    // two items and BETWEEN: text or a Double parameter beside a CURRENCY column is rounded to a Currency, and CCur(1)
    // beside a binary column compares as text.
    [Fact]
    public void A_value_compared_with_an_unindexed_column_is_taken_alike_by_every_comparison_form()
    {
        var script = new List<Step>();
        Statement(script, "CREATE TABLE UC (ID LONG PRIMARY KEY, C CURRENCY)");
        Statement(script, "INSERT INTO UC VALUES (1, 1.2345)");
        Statement(script, "INSERT INTO UC VALUES (2, 1.2346)");
        foreach (object value in (object[])["1.23455", "1.234549", 1.23455])
            foreach (string where in (string[])["C = @p", "C IN (@p)", "C IN (@p, @p)", "C BETWEEN @p AND @p"])
                Rows(script, $"SELECT ID FROM UC WHERE {where} ORDER BY ID", new P(value, null));

        // Currency 1 stored into a BINARY(8), and the text '1'.
        Statement(script, "CREATE TABLE UB (ID LONG, V BINARY(8))");
        Statement(script, "INSERT INTO UB VALUES (1, 0x1027000000000000)");
        Statement(script, "INSERT INTO UB VALUES (2, 0x3100000000000000)");
        foreach (string where in (string[])["V = CCur(1)", "V IN (CCur(1))", "V IN (CCur(1), CCur(1))", "V BETWEEN CCur(1) AND CCur(1)"])
            Rows(script, $"SELECT ID FROM UB WHERE {where} ORDER BY ID");

        SameThroughBothEngines(script);
    }

    // Each comparison an index answers, run as a SELECT, a single-table UPDATE and DELETE, and an UPDATE and DELETE
    // through a join, each on a freshly made indexed table. "%" stands for the compared column.
    private static void EveryStatementForm(List<Step> script, ref int n, (string Columns, string[] Seed) table, string condition, P? p = null)
    {
        OnFreshIndexedTable(script, ref n, table, t => $"SELECT ID FROM {t} WHERE {condition.Replace("%", "V", StringComparison.Ordinal)} ORDER BY ID", p);
        OnFreshIndexedTable(script, ref n, table, t => $"UPDATE {t} SET ID = ID + 10 WHERE {condition.Replace("%", "V", StringComparison.Ordinal)}", p);
        OnFreshIndexedTable(script, ref n, table, t => $"DELETE FROM {t} WHERE {condition.Replace("%", "V", StringComparison.Ordinal)}", p);
        OnFreshIndexedTable(script, ref n, table,
            t => $"UPDATE {t} INNER JOIN NJ ON {t}.ID = NJ.ID SET {t}.ID = {t}.ID + 10 WHERE {condition.Replace("%", t + ".V", StringComparison.Ordinal)}", p);
        OnFreshIndexedTable(script, ref n, table,
            t => $"DELETE {t}.* FROM {t} INNER JOIN NJ ON {t}.ID = NJ.ID WHERE {condition.Replace("%", t + ".V", StringComparison.Ordinal)}", p);
    }

    // UPDATE and DELETE, of one table or through a join, take a comparison an index answers as the SELECT with the same
    // WHERE takes it: a Yes/No column by truth, text as a number, Currency or date column stores it, a number as a GUID
    // or binary column stores it, and a value the column cannot hold as no row, or an error as a bound.
    [Fact]
    public void Every_statement_takes_a_comparison_an_index_answers_as_the_SELECT_takes_it()
    {
        var script = new List<Step>();
        Statement(script, "CREATE TABLE NJ (ID LONG PRIMARY KEY)");
        for (int i = 1; i <= 5; i++) Statement(script, $"INSERT INTO NJ VALUES ({i})");
        int n = 0;

        foreach (string condition in (string[])["% = 2", "% = CLng(1)", "% IN (CLng(1))", "% BETWEEN CLng(1) AND CLng(2)"])
            EveryStatementForm(script, ref n, IndexedYesNo, condition);
        EveryStatementForm(script, ref n, IndexedYesNo, "% >= @p", new P(1, null));

        foreach (object text in (object[])["True", "1.5", "abc"])
            EveryStatementForm(script, ref n, IndexedLong, "% = @p", new P(text, null));
        foreach (object text in (object[])["1.5", "abc"])
            EveryStatementForm(script, ref n, IndexedLong, "% >= @p", new P(text, null));
        EveryStatementForm(script, ref n, IndexedLong, "% = 3000000000");
        EveryStatementForm(script, ref n, IndexedLong, "% > CDbl(3000000000)");
        EveryStatementForm(script, ref n, IndexedLong, "% IN (CLng(1), CInt(True))");

        (string, string[]) currency = ("ID LONG PRIMARY KEY, V CURRENCY", ["1.2345", "1.2346", "2"]);
        foreach (object value in (object[])["1.23455", 1.23455])
            EveryStatementForm(script, ref n, currency, "% = @p", new P(value, null));

        (string, string[]) date = ("ID LONG PRIMARY KEY, V DATETIME", ["#12/29/1899#", "#12/31/1899#", "#12/30/1899#", "NULL"]);
        foreach (string text in (string[])["12/31/1899", "1.5"])
            EveryStatementForm(script, ref n, date, "% = @p", new P(text, null));
        EveryStatementForm(script, ref n, date, "% < @p", new P("1.5", null));
        EveryStatementForm(script, ref n, date, "% >= @p", new P("1", null));

        foreach (string condition in (string[])["% = CLng(1)", "% IN (CLng(1))", "% BETWEEN CLng(0) AND CLng(1)"])
            EveryStatementForm(script, ref n, IndexedBinary, condition);
        EveryStatementForm(script, ref n, IndexedBinary, "% = @p", new P(1, null));
        EveryStatementForm(script, ref n, IndexedBinary, "% >= @p", new P(1, null));
        EveryStatementForm(script, ref n, IndexedBinary, "% < @p", new P((short)-1, null));

        (string, string[]) guid = ("ID LONG, V GUID",
            ["{guid {00000001-0000-0000-0000-000000000000}}", "{guid {0000FFFF-0000-0000-0000-000000000000}}", "NULL"]);
        EveryStatementForm(script, ref n, guid, "% = CLng(1)");
        EveryStatementForm(script, ref n, guid, "% = @p", new P(1, null));
        EveryStatementForm(script, ref n, guid, "% >= @p", new P(true, null));

        // An unqualified column in a WHERE over a join.
        OnFreshIndexedTable(script, ref n, IndexedBinary, t => $"DELETE {t}.* FROM {t} INNER JOIN NJ ON {t}.ID = NJ.ID WHERE V = CLng(1)");
        OnFreshIndexedTable(script, ref n, IndexedYesNo, t => $"UPDATE {t} INNER JOIN NJ ON {t}.ID = NJ.ID SET {t}.ID = {t}.ID + 10 WHERE V = 2");

        SameThroughBothEngines(script);
    }

    [Fact]
    public void Every_statement_takes_text_compared_with_an_indexed_BIGINT_column_as_the_SELECT_takes_it()
    {
        Assert.SkipUnless(
            AceTestDatabase.SupportsColumnType(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "BIGINT"),
            AceTestDatabase.UnsupportedColumnTypeReason("BIGINT"));

        var script = new List<Step>();
        Statement(script, "CREATE TABLE NJ (ID LONG PRIMARY KEY)");
        for (int i = 1; i <= 5; i++) Statement(script, $"INSERT INTO NJ VALUES ({i})");
        int n = 0;
        (string, string[]) bigint = ("ID LONG PRIMARY KEY, V BIGINT", ["-1", "1", "0", "NULL", "2"]);
        foreach (string text in (string[])["2", "abc", "1.5"])
            EveryStatementForm(script, ref n, bigint, "% = @p", new P(text, null));
        EveryStatementForm(script, ref n, bigint, "% > @p", new P("1", null));
        EveryStatementForm(script, ref n, bigint, "% = CStr(1)");

        SameThroughBothEngines(script);
    }

    // A column written without its table in a WHERE over a join is the column of the one table that has it, and a
    // comparison an index answers takes it as that table's column written with its table: as a SELECT, as an UPDATE
    // and as a DELETE through the join.
    [Fact]
    public void An_unqualified_column_over_a_join_is_taken_as_the_column_of_its_one_table()
    {
        var script = new List<Step>();
        Statement(script, "CREATE TABLE NJ (ID LONG PRIMARY KEY)");
        for (int i = 1; i <= 5; i++) Statement(script, $"INSERT INTO NJ VALUES ({i})");
        int n = 0;

        (string, string[]) guid = ("ID LONG, V GUID",
            ["{guid {00000001-0000-0000-0000-000000000000}}", "{guid {0000FFFF-0000-0000-0000-000000000000}}", "NULL"]);
        foreach (((string, string[]) table, string condition, P? p) in (((string, string[]), string, P?)[])[
            (IndexedYesNo, "V = 2", null), (IndexedYesNo, "V = CLng(1)", null), (IndexedYesNo, "V IN (CLng(1))", null),
            (IndexedYesNo, "V >= @p", new P(1, null)),
            (IndexedLong, "V = @p", new P("True", null)), (IndexedLong, "V = @p", new P("1.5", null)),
            (IndexedLong, "V >= @p", new P("abc", null)),
            (IndexedBinary, "V = CLng(1)", null), (IndexedBinary, "V = @p", new P(1, null)), (IndexedBinary, "V >= @p", new P(1, null)),
            (guid, "V = CLng(1)", null), (guid, "V = @p", new P(1, null)), (guid, "V >= @p", new P(true, null))])
        {
            OnFreshIndexedTable(script, ref n, table, t => $"SELECT {t}.ID FROM {t} INNER JOIN NJ ON {t}.ID = NJ.ID WHERE {condition} ORDER BY {t}.ID", p);
            OnFreshIndexedTable(script, ref n, table, t => $"SELECT {t}.ID FROM {t}, NJ WHERE {t}.ID = NJ.ID AND {condition} ORDER BY {t}.ID", p);
            OnFreshIndexedTable(script, ref n, table, t => $"UPDATE {t} INNER JOIN NJ ON {t}.ID = NJ.ID SET {t}.ID = {t}.ID + 10 WHERE {condition}", p);
            OnFreshIndexedTable(script, ref n, table, t => $"DELETE {t}.* FROM {t} INNER JOIN NJ ON {t}.ID = NJ.ID WHERE {condition}", p);
        }

        SameThroughBothEngines(script);
    }

    // A column written with its table in a WHERE over a join, compared as a SELECT through the join takes it.
    [Fact]
    public void A_qualified_column_over_a_join_is_compared_as_the_SELECT_through_the_join_compares_it()
    {
        var script = new List<Step>();
        Statement(script, "CREATE TABLE NJ (ID LONG PRIMARY KEY)");
        for (int i = 1; i <= 5; i++) Statement(script, $"INSERT INTO NJ VALUES ({i})");
        int n = 0;

        foreach (((string, string[]) table, string condition, P? p) in (((string, string[]), string, P?)[])[
            (IndexedYesNo, "% = 2", null), (IndexedYesNo, "% = CLng(1)", null), (IndexedYesNo, "% >= @p", new P(1, null)),
            (IndexedLong, "% = @p", new P("True", null)), (IndexedLong, "% = @p", new P("1.5", null)),
            (IndexedBinary, "% = CLng(1)", null)])
        {
            OnFreshIndexedTable(script, ref n, table,
                t => $"SELECT {t}.ID FROM {t} INNER JOIN NJ ON {t}.ID = NJ.ID WHERE {condition.Replace("%", t + ".V", StringComparison.Ordinal)} ORDER BY {t}.ID", p);
            OnFreshIndexedTable(script, ref n, table,
                t => $"SELECT {t}.ID FROM {t}, NJ WHERE {t}.ID = NJ.ID AND {condition.Replace("%", t + ".V", StringComparison.Ordinal)} ORDER BY {t}.ID", p);
            OnFreshIndexedTable(script, ref n, table,
                t => $"SELECT NJ.ID FROM NJ INNER JOIN {t} ON NJ.ID = {t}.ID WHERE {condition.Replace("%", t + ".V", StringComparison.Ordinal)} ORDER BY NJ.ID", p);
        }

        SameThroughBothEngines(script);
    }

    // Conditions on one table written with and without its name, both columns indexed (ID is the primary key, V has an
    // index); a name both tables of a join have; and a column of the unmatched side of a LEFT JOIN. "#" stands for the
    // freshly made indexed table.
    [Fact]
    public void Qualified_and_unqualified_columns_ambiguous_names_and_outer_join_columns_are_taken_as_ACE_takes_them()
    {
        var script = new List<Step>();
        Statement(script, "CREATE TABLE NJ (ID LONG PRIMARY KEY)");
        for (int i = 1; i <= 5; i++) Statement(script, $"INSERT INTO NJ VALUES ({i})");
        int n = 0;

        foreach ((string condition, P? p) in ((string, P?)[])[
            ("#.ID >= 1 AND V = 2", null), ("ID >= 1 AND #.V = 2", null), ("#.ID = 1 AND V = CLng(1)", null),
            ("ID <= 2 AND #.V >= @p", new P(1, null)), ("V = 2 AND #.ID < 2", null)])
        {
            OnFreshIndexedTable(script, ref n, IndexedYesNo, t => $"SELECT ID FROM {t} WHERE {condition.Replace("#", t, StringComparison.Ordinal)} ORDER BY ID", p);
            OnFreshIndexedTable(script, ref n, IndexedYesNo, t => $"UPDATE {t} SET ID = ID + 10 WHERE {condition.Replace("#", t, StringComparison.Ordinal)}", p);
            OnFreshIndexedTable(script, ref n, IndexedYesNo, t => $"DELETE FROM {t} WHERE {condition.Replace("#", t, StringComparison.Ordinal)}", p);
        }
        foreach (string condition in (string[])["#.ID >= 1 AND V = 2", "V = CLng(1) AND #.ID <= 2"])
        {
            OnFreshIndexedTable(script, ref n, IndexedYesNo,
                t => $"SELECT {t}.ID FROM {t} INNER JOIN NJ ON {t}.ID = NJ.ID WHERE {condition.Replace("#", t, StringComparison.Ordinal)} ORDER BY {t}.ID");
            OnFreshIndexedTable(script, ref n, IndexedYesNo,
                t => $"UPDATE {t} INNER JOIN NJ ON {t}.ID = NJ.ID SET {t}.ID = {t}.ID + 10 WHERE {condition.Replace("#", t, StringComparison.Ordinal)}");
            OnFreshIndexedTable(script, ref n, IndexedYesNo,
                t => $"DELETE {t}.* FROM {t} INNER JOIN NJ ON {t}.ID = NJ.ID WHERE {condition.Replace("#", t, StringComparison.Ordinal)}");
        }

        foreach (string condition in (string[])["ID = 1", "ID = 1 AND V = 2"])
        {
            OnFreshIndexedTable(script, ref n, IndexedYesNo, t => $"SELECT {t}.ID FROM {t} INNER JOIN NJ ON {t}.ID = NJ.ID WHERE {condition} ORDER BY {t}.ID");
            OnFreshIndexedTable(script, ref n, IndexedYesNo, t => $"UPDATE {t} INNER JOIN NJ ON {t}.ID = NJ.ID SET {t}.V = False WHERE {condition}");
            OnFreshIndexedTable(script, ref n, IndexedYesNo, t => $"DELETE {t}.* FROM {t} INNER JOIN NJ ON {t}.ID = NJ.ID WHERE {condition}");
        }

        foreach (((string, string[]) table, string condition) in (((string, string[]), string)[])[
            (IndexedYesNo, "V = 2"), (IndexedYesNo, "V = CLng(1)"), (IndexedBinary, "V = CLng(1)")])
        {
            OnFreshIndexedTable(script, ref n, table, t => $"SELECT NJ.ID, {t}.ID FROM NJ LEFT JOIN {t} ON NJ.ID = {t}.ID WHERE {condition} ORDER BY NJ.ID");
            OnFreshIndexedTable(script, ref n, table, t => $"UPDATE NJ LEFT JOIN {t} ON NJ.ID = {t}.ID SET {t}.ID = {t}.ID + 10 WHERE {condition}");
            OnFreshIndexedTable(script, ref n, table, t => $"DELETE {t}.* FROM NJ LEFT JOIN {t} ON NJ.ID = {t}.ID WHERE {condition}");
        }

        SameThroughBothEngines(script);
    }

    // Which table of a join ACE reads first decides how a Yes/No restriction on it is compared: read first, JS.Y = 2 is
    // answered from the index (the True row); looked up for each row of the other table, it is tested on the fetched row
    // as written (no row). JS, of 2 rows, joins tables of 30, 60 and 200 rows, selecting its own column and then the other
    // table's, which moves the size at which JS is read first; and with the other table's primary key held to a value too.
    [Fact]
    public void Which_table_of_a_join_is_read_first_decides_how_a_YesNo_restriction_on_it_is_compared()
    {
        var script = new List<Step>();
        Statement(script, "CREATE TABLE DG (D LONG)");
        for (int d = 0; d < 10; d++)
            Statement(script, $"INSERT INTO DG VALUES ({d})");
        Statement(script, "CREATE TABLE JS (ID LONG PRIMARY KEY, Y YESNO)");
        Statement(script, "CREATE INDEX IX_JS ON JS (Y)");
        Statement(script, "INSERT INTO JS VALUES (1, True)");
        Statement(script, "INSERT INTO JS VALUES (2, False)");
        foreach (int size in (int[])[30, 60, 200])
        {
            string t = "JT" + size;
            Statement(script, $"CREATE TABLE {t} (ID LONG PRIMARY KEY)");
            Statement(script, $"INSERT INTO {t} (ID) SELECT q.K FROM (SELECT a.D + 10 * b.D + 100 * c.D + 1 AS K"
                + $" FROM DG AS a, DG AS b, DG AS c) AS q WHERE q.K <= {size}");
            Rows(script, $"SELECT JS.ID FROM JS INNER JOIN {t} ON JS.ID = {t}.ID WHERE JS.Y = 2 ORDER BY JS.ID");
            Rows(script, $"SELECT {t}.ID FROM JS INNER JOIN {t} ON JS.ID = {t}.ID WHERE JS.Y = 2 ORDER BY {t}.ID");
            Rows(script, $"SELECT {t}.ID FROM JS INNER JOIN {t} ON JS.ID = {t}.ID WHERE JS.Y = 2 AND {t}.ID = 1");
        }

        SameThroughBothEngines(script);
    }

    // A comparison with a written True on a Long (Int32) column, with a written False on a text column, and
    // NOT (x <> Null), on V (indexed) and U (the same values, unindexed): as a SELECT, an UPDATE and a DELETE of one
    // table and of a join, and as a SELECT through a join. Then an UPDATE through a join onto a bracketed join, whose ON
    // is keyed by the first table.
    [Fact]
    public void A_truth_test_and_a_written_Null_select_the_same_rows_with_an_index_or_without()
    {
        var script = new List<Step>();
        Statement(script, "CREATE TABLE NJ (ID LONG PRIMARY KEY)");
        for (int i = 1; i <= 5; i++) Statement(script, $"INSERT INTO NJ VALUES ({i})");
        int n = 0;

        foreach (((string, string[]) table, string condition) in (((string, string[]), string)[])[
            (("ID LONG PRIMARY KEY, V LONG, U LONG", ["-1, -1", "1, 1", "2, 2"]), "# = True"),
            (("ID LONG PRIMARY KEY, V TEXT(10), U TEXT(10)", ["'0', '0'", "'-1', '-1'", "'False', 'False'"]), "# = False"),
            (("ID LONG PRIMARY KEY, V LONG, U LONG", ["1, 1", "NULL, NULL", "2, 2"]), "NOT (# <> Null)")])
            foreach (string column in (string[])["%", "U"])
            {
                string compared = condition.Replace("#", column, StringComparison.Ordinal);
                EveryStatementForm(script, ref n, table, compared);
                OnFreshIndexedTable(script, ref n, table, t =>
                    $"SELECT {t}.ID FROM {t} INNER JOIN NJ ON {t}.ID = NJ.ID WHERE {compared.Replace("%", t + ".V", StringComparison.Ordinal)} ORDER BY {t}.ID");
            }

        Statement(script, "CREATE TABLE NK (ID LONG PRIMARY KEY, W LONG)");
        for (int i = 1; i <= 3; i++) Statement(script, $"INSERT INTO NK VALUES ({i}, {i})");
        OnFreshIndexedTable(script, ref n, IndexedLong,
            t => $"UPDATE NJ INNER JOIN ({t} INNER JOIN NK ON {t}.ID = NK.W) ON NJ.ID = NK.ID SET {t}.V = {t}.V + 10");

        SameThroughBothEngines(script);
    }

    // A correlated subquery whose WHERE holds, beside the condition tying it to the outer row, a comparison of an
    // indexed column written without its table: as EXISTS and as a count, in a SELECT, an UPDATE and a DELETE.
    // UY has an index on its Yes/No column Y.
    [Fact]
    public void An_unqualified_column_beside_a_correlation_in_a_subquery_is_taken_as_ACE_takes_it()
    {
        var script = new List<Step>();
        Statement(script, "CREATE TABLE NJ (ID LONG PRIMARY KEY)");
        for (int i = 1; i <= 5; i++) Statement(script, $"INSERT INTO NJ VALUES ({i})");
        Statement(script, "CREATE TABLE UY (ID LONG PRIMARY KEY, Y YESNO)");
        Statement(script, "CREATE INDEX IX_UY ON UY (Y)");
        Statement(script, "INSERT INTO UY VALUES (1, True)");
        Statement(script, "INSERT INTO UY VALUES (2, False)");
        Statement(script, "INSERT INTO UY VALUES (3, True)");
        int n = 0;

        foreach (string condition in (string[])["V = 2", "V = CLng(1)", "V >= 2"])
        {
            OnFreshIndexedTable(script, ref n, IndexedYesNo,
                t => $"SELECT NJ.ID FROM NJ WHERE EXISTS (SELECT * FROM {t} WHERE {t}.ID = NJ.ID AND {condition}) ORDER BY NJ.ID");
            OnFreshIndexedTable(script, ref n, IndexedYesNo,
                t => $"SELECT NJ.ID FROM NJ WHERE NOT EXISTS (SELECT * FROM {t} WHERE {condition} AND {t}.ID = NJ.ID) ORDER BY NJ.ID");
            OnFreshIndexedTable(script, ref n, IndexedYesNo,
                t => $"SELECT NJ.ID FROM NJ WHERE (SELECT COUNT(*) FROM {t} WHERE {t}.ID = NJ.ID AND {condition}) > 0 ORDER BY NJ.ID");
        }

        foreach (string condition in (string[])["Y = 2", "Y = CLng(1)", "Y >= 2"])
        {
            OnFreshIndexedTable(script, ref n, IndexedLong,
                t => $"SELECT ID FROM {t} WHERE EXISTS (SELECT * FROM UY WHERE UY.ID = {t}.ID AND {condition}) ORDER BY ID");
            OnFreshIndexedTable(script, ref n, IndexedLong,
                t => $"SELECT ID FROM {t} WHERE (SELECT COUNT(*) FROM UY WHERE UY.ID = {t}.ID AND {condition}) > 0 ORDER BY ID");
            OnFreshIndexedTable(script, ref n, IndexedLong,
                t => $"UPDATE {t} SET V = V + 10 WHERE EXISTS (SELECT * FROM UY WHERE UY.ID = {t}.ID AND {condition})");
            OnFreshIndexedTable(script, ref n, IndexedLong,
                t => $"UPDATE {t} SET V = V + 10 WHERE (SELECT COUNT(*) FROM UY WHERE UY.ID = {t}.ID AND {condition}) > 0");
            OnFreshIndexedTable(script, ref n, IndexedLong,
                t => $"DELETE FROM {t} WHERE EXISTS (SELECT * FROM UY WHERE {condition} AND UY.ID = {t}.ID)");
            OnFreshIndexedTable(script, ref n, IndexedLong,
                t => $"DELETE FROM {t} WHERE (SELECT COUNT(*) FROM UY WHERE UY.ID = {t}.ID AND {condition}) > 0");
        }

        SameThroughBothEngines(script);
    }
}
