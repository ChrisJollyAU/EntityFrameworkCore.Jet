using System.Data.OleDb;
using System.Globalization;
using Xunit;

namespace LibRed.Engine.Tests;

/// <summary>
/// A query asked of ACE over OLE DB and of LibRed's engine, over a copy of Northwind with the test's tables made and
/// filled through ACE. Each result is written out as its column types and values, and the two must agree; a refusal
/// matches a refusal.
/// </summary>
/// <remarks>ACE writes a date out in the Windows user locale, so LibRed runs under that culture here; the rest of the
/// suite pins en-US.</remarks>
public abstract class AceQueryParityTest(ITestOutputHelper output) : TempDatabaseTest
{
    protected ITestOutputHelper Output => output;

    protected void Matches(string prefix, string[] setup, string[] queries)
    {
        string path = Seeded(prefix, setup);
        try
        {
            var differences = new List<string>();
            foreach (string query in queries)
            {
                string ace;
                using (OleDbConnection connection = AceTestDatabase.Open(path))
                using (OleDbCommand command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    try
                    {
                        using OleDbDataReader reader = command.ExecuteReader();
                        var types = Enumerable.Range(0, reader.FieldCount).Select(reader.GetFieldType).ToList();
                        var rows = new List<object?[]>();
                        while (reader.Read())
                            rows.Add([.. Enumerable.Range(0, reader.FieldCount).Select(i => reader.IsDBNull(i) ? null : reader.GetValue(i))]);
                        ace = Describe(types, rows);
                    }
                    catch (OleDbException) { ace = "refused"; }
                }

                string libred = UnderUserCulture(() =>
                {
                    using var database = JetDatabase.Open(path, readOnly: true);
                    try
                    {
                        var result = new QueryEngine(database).ExecuteQuery(query);
                        return Describe(result.ColumnTypes, result.Rows);
                    }
                    catch (Exception e) when (e is InvalidOperationException or ArgumentException or InvalidCastException or FormatException
                        or OverflowException or DivideByZeroException or NotSupportedException or LibRed.Sql.Binding.SqlBindException
                        or LibRed.Sql.Parsing.SqlParseException) { return "refused"; }
                });

                if (ace != libred) differences.Add($"{query}\n    ACE    {ace}\n    LibRed {libred}");
            }
            foreach (string difference in differences) output.WriteLine(difference);
            Assert.True(differences.Count == 0, string.Join(Environment.NewLine, differences));
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    /// <summary>A copy of Northwind with <paramref name="setup"/> run through ACE.</summary>
    protected static string Seeded(string prefix, string[] setup)
    {
        string path = TemporaryDatabase.CopyPath(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), prefix);
        using OleDbConnection connection = AceTestDatabase.Open(path);
        foreach (string statement in setup)
        {
            using OleDbCommand command = connection.CreateCommand();
            command.CommandText = statement;
            command.ExecuteNonQuery();
        }
        return path;
    }

    /// <summary>Runs LibRed under the Windows user's own locale and formats, which ACE writes a date out in.</summary>
    protected static T UnderUserCulture<T>(Func<T> run)
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture =
            Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\International")?.GetValue("LocaleName") is string name
                ? new CultureInfo(name, useUserOverride: true)
                : CultureInfo.InstalledUICulture;
        try { return run(); }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    private static string Describe(IReadOnlyList<Type> types, IEnumerable<object?[]> rows) =>
        $"[{string.Join(", ", types.Select(t => t.Name))}] " + Rows(rows);

    protected static string Rows(IEnumerable<object?[]> rows) =>
        string.Join(" | ", rows.Select(row => string.Join(", ", row.Select(Value))));

    private static string Value(object? value) => value switch
    {
        null => "NULL",
        byte[] bytes => $"{Convert.ToHexString(bytes)}:Byte[]",
        DateTime date => $"{date:o}:DateTime",
        double d => $"{d.ToString("R", CultureInfo.InvariantCulture)}:Double",
        // A Decimal's scale is not compared: LibRed's Currency keeps .NET's (21.0) where ACE's does not (21).
        decimal m => $"{(m / 1.0000000000000000000000000000m).ToString(CultureInfo.InvariantCulture)}:Decimal",
        _ => $"{Convert.ToString(value, CultureInfo.InvariantCulture)}:{value.GetType().Name}",
    };
}
