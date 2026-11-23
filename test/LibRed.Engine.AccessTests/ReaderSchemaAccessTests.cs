using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Globalization;
using LibRed.Data;
using LibRed.Engine.Schema;
using Xunit;

namespace LibRed.Engine.Tests;

// A column's nullability, against ACE's own answer on the same file: its Columns rowset (IS_NULLABLE and COLUMN_FLAGS),
// which INFORMATION_SCHEMA.COLUMNS and JetStoreType.IsNullable (the scaffolder's source) must also give, and a reader's
// schema under each CommandBehavior. A Yes/No column is not nullable in the Columns rowset however it was declared,
// retyped or added; without KeyInfo a reader calls every column nullable, and under KeyInfo only a single table's key
// is not. ACE reads the file it made, and LibRed reads that file, then the one it made itself.
[Collection(AceCollection.Name)]
public class ReaderSchemaAccessTests(ITestOutputHelper output) : TempDatabaseTest
{
    private static readonly string[] Statements =
    [
        "CREATE TABLE YS (ID LONG PRIMARY KEY, F YESNO, FR YESNO NOT NULL, FD YESNO DEFAULT True, T TEXT(10), TR TEXT(10) NOT NULL, C COUNTER)",
        "INSERT INTO YS (ID, F, FR, FD, T, TR) VALUES (1, True, True, True, 'a', 'b')",
        "INSERT INTO YS (ID, F, FR, FD, T, TR) VALUES (2, False, False, False, NULL, 'b')",
        "CREATE VIEW VY AS SELECT ID, F, FR, FD, T, TR FROM YS",
        "CREATE TABLE A1 (ID LONG PRIMARY KEY, F LONG)", "ALTER TABLE A1 ALTER COLUMN F YESNO",
        "CREATE TABLE A2 (ID LONG PRIMARY KEY, F YESNO NOT NULL)", "ALTER TABLE A2 ALTER COLUMN F YESNO",
        "CREATE TABLE A3 (ID LONG PRIMARY KEY)", "ALTER TABLE A3 ADD COLUMN F YESNO", "ALTER TABLE A3 ADD COLUMN G YESNO NOT NULL",
        "CREATE TABLE L (ID LONG PRIMARY KEY)", "INSERT INTO L VALUES (1)", "INSERT INTO L VALUES (2)", "INSERT INTO L VALUES (3)",
        "CREATE TABLE K2 (A LONG, B LONG, U LONG NOT NULL, V LONG, CONSTRAINT PK_K2 PRIMARY KEY (A, B))",
        "CREATE UNIQUE INDEX UX_K2 ON K2 (U)", "INSERT INTO K2 VALUES (1, 1, 1, 1)",
        "CREATE TABLE K3 (U LONG NOT NULL, V LONG)", "CREATE UNIQUE INDEX UX_K3 ON K3 (U)", "INSERT INTO K3 VALUES (1, 1)",
    ];

    // The queries whose reader schema is compared with its column types too.
    private static readonly string[] Typed = ["SELECT F, FR, FD FROM YS", "SELECT F, FR, FD FROM VY"];

    private static readonly string[] Queries =
    [
        "SELECT F, FR, FD FROM YS",
        "SELECT F, FR, FD FROM VY",
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

    // ACE reads the file it made; LibRed reads that same file, or the one it made itself.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Every_schema_surface_calls_a_column_nullable_as_ACE_does(bool libredReadsItsOwnFile)
    {
        string acePath = TemporaryDatabase.CopyPath(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "rdrschema-ace-");
        string libredPath = TemporaryDatabase.CopyPath(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "rdrschema-lib-");
        try
        {
            AceRun(acePath, Statements);
            OleDbConnection.ReleaseObjectPool();
            if (libredReadsItsOwnFile) LibRedRun(libredPath, Statements);
            string path = libredReadsItsOwnFile ? libredPath : acePath;

            // Each column of the Columns rowset: its name, N (nullable) or R (not), and its DBCOLUMNFLAGS.
            static string ColumnsRowset(DbConnection c, string table) =>
                string.Join(" ", c.GetSchema("Columns", [null, null, table, null]).Rows.Cast<DataRow>()
                    .OrderBy(r => Convert.ToInt64(r["ORDINAL_POSITION"], CultureInfo.InvariantCulture))
                    .Select(r => $"{r["COLUMN_NAME"]}:{((bool)r["IS_NULLABLE"] ? "N" : "R")}{r["COLUMN_FLAGS"]}"));

            // Each column of a reader's schema: its name, N (AllowDBNull) or R (not), and, where asked, its DataType —
            // asked only of the Yes/No columns themselves, since ACE types a comparison Integer (Int16) where LibRed has
            // a Boolean. "refused" when the engine refuses an untyped query; a typed one is never refused.
            static string ReaderSchema(DbConnection c, string sql, CommandBehavior behavior, bool typed, Func<Exception, bool> refusal)
            {
                using var command = c.CreateCommand();
                command.CommandText = sql;
                try
                {
                    using var reader = command.ExecuteReader(behavior);
                    return string.Join(" ", reader.GetSchemaTable()!.Rows.Cast<DataRow>()
                        .Select(row => $"{row["ColumnName"]}:{((bool)row["AllowDBNull"] ? "N" : "R")}"
                            + (typed ? ":" + ((Type)row["DataType"]).Name : "")));
                }
                catch (Exception e) when (!typed && refusal(e)) { return "refused"; }
            }

            static List<string> Answers(DbConnection c, Func<Exception, bool> refusal) =>
            [
                .. ((string[])["YS", "VY", "A1", "A2", "A3"]).Select(table => $"Columns rowset of {table}: {ColumnsRowset(c, table)}"),
                .. ((CommandBehavior[])[CommandBehavior.Default, CommandBehavior.SchemaOnly, CommandBehavior.KeyInfo,
                    CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo])
                    .SelectMany(behavior => Queries.Select(sql => $"{sql} [{behavior}]: {ReaderSchema(c, sql, behavior, Typed.Contains(sql), refusal)}")),
            ];

            List<string> ace, libred;
            string aceRowset;
            using (var connection = AceTestDatabase.Open(acePath))
            {
                ace = Answers(connection, e => e is OleDbException);
                aceRowset = string.Join(" ", connection.GetSchema("Columns", [null, null, "YS", null]).Rows.Cast<DataRow>()
                    .OrderBy(r => Convert.ToInt64(r["ORDINAL_POSITION"], CultureInfo.InvariantCulture))
                    .Select(r => $"{r["COLUMN_NAME"]}:{((bool)r["IS_NULLABLE"] ? "N" : "R")}"));
            }
            OleDbConnection.ReleaseObjectPool();

            string libredInformationSchema, libredStoreType;
            using (var connection = new LibRedConnection($"Data Source={path}"))
            {
                connection.Open();
                libred = Answers(connection, e => e is DbException or InvalidOperationException or ArgumentException or NotSupportedException
                    or LibRed.Sql.Binding.SqlBindException or LibRed.Sql.Parsing.SqlParseException);
                using var command = connection.CreateCommand();
                command.CommandText =
                    "SELECT COLUMN_NAME, IS_NULLABLE FROM `INFORMATION_SCHEMA.COLUMNS` WHERE TABLE_NAME = 'YS' ORDER BY ORDINAL_POSITION";
                using var reader = command.ExecuteReader();
                var rows = new List<string>();
                while (reader.Read()) rows.Add($"{reader.GetString(0)}:{(reader.GetBoolean(1) ? "N" : "R")}");
                libredInformationSchema = string.Join(" ", rows);
            }
            using (var database = JetDatabase.Open(path, readOnly: true))
                libredStoreType = string.Join(" ", database.Catalog.FindTable("YS")!.Columns
                    .Select(c => c.Name + ":" + (JetStoreType.IsNullable(c) ? "N" : "R")));

            output.WriteLine($"  ACE Columns rowset of YS  : {aceRowset}");
            output.WriteLine($"  LibRed INFORMATION_SCHEMA : {libredInformationSchema}");
            output.WriteLine($"  LibRed JetStoreType       : {libredStoreType}");
            Assert.Equal(ace, libred);
            Assert.Equal(aceRowset, libredInformationSchema);
            Assert.Equal(aceRowset, libredStoreType);
        }
        finally
        {
            TemporaryDatabase.Delete(acePath);
            TemporaryDatabase.Delete(libredPath);
        }
    }

    private static void AceRun(string path, string[] statements)
    {
        using OleDbConnection connection = AceTestDatabase.Open(path);
        foreach (string s in statements)
        {
            using OleDbCommand command = connection.CreateCommand();
            command.CommandText = s;
            command.ExecuteNonQuery();
        }
    }

    private static void LibRedRun(string path, string[] statements)
    {
        using var database = JetDatabase.Open(path, readOnly: false);
        var engine = new QueryEngine(database);
        foreach (string s in statements) engine.ExecuteNonQuery(s);
    }
}
