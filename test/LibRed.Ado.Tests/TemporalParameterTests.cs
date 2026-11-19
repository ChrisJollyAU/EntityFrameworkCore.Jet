using LibRed.Data;
using Xunit;

namespace LibRed.Ado.Tests;

// Jet has no native TimeSpan/TimeOnly/DateOnly/DateTimeOffset — LibRedCommand.Normalize converts each parameter
// to the DateTime the engine stores and compares against (a time on the 1899-12-30 epoch, a date at midnight),
// exactly as the literal path does. So the engine stays DateTime-only and `WHERE timeCol = @timeSpanParam`
// matches. Regression for BuiltInDataTypes Can_query_using_any_data_type / Can_query_using_DateDiffHour.
public class TemporalParameterTests
{
    private static LibRedConnection OpenTemp()
    {
        // Tracked, so the copy is swept at process exit: this helper returns only the connection, so the
        // caller has no path to delete in a finally.
        string path = TemporaryDatabase.CopyPath(
            Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "tpar-");
        var conn = new LibRedConnection($"Data Source={path}");
        conn.Open();
        return conn;
    }

    private static void Exec(LibRedConnection c, string sql, string? name = null, object? value = null)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        if (name is not null)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            cmd.Parameters.Add(p);
        }
        cmd.ExecuteNonQuery();
    }

    [Theory]
    [MemberData(nameof(TemporalCases))]
    public void A_temporal_parameter_matches_the_stored_value(object stored, object queried)
    {
        using var conn = OpenTemp();
        Exec(conn, "CREATE TABLE `T` (`Id` INTEGER PRIMARY KEY, `V` DATETIME)");
        Exec(conn, "INSERT INTO `T` (`Id`, `V`) VALUES (1, @v)", "@v", stored);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT `Id` FROM `T` WHERE `V` = @p";
        var p = cmd.CreateParameter();
        p.ParameterName = "@p";
        p.Value = queried;
        cmd.Parameters.Add(p);

        using var reader = cmd.ExecuteReader();
        Assert.True(reader.Read());   // stored temporal (as an epoch DateTime) equals the queried temporal
        Assert.Equal(1, reader.GetInt32(0));
    }

    // The epoch (OA 0) is a real date, and every accessor returns it as stored, as ACE's OLE DB reader does.
    [Fact]
    public void The_stored_epoch_reads_back_as_the_epoch_on_every_accessor()
    {
        using var conn = OpenTemp();
        Exec(conn, "CREATE TABLE `D` (`Id` INTEGER PRIMARY KEY, `V` DATETIME)");
        Exec(conn, "INSERT INTO `D` (`Id`, `V`) VALUES (1, #12/30/1899#)");   // OA 0 / the epoch

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT `V` FROM `D`";
        using var reader = cmd.ExecuteReader();
        Assert.True(reader.Read());
        var epoch = new DateTime(1899, 12, 30);
        Assert.Equal(epoch, reader.GetValue(0));
        Assert.Equal(epoch, reader.GetDateTime(0));
        Assert.Equal(epoch, reader.GetFieldValue<DateTime>(0));
    }

    // A Boolean bound as text is the text "True" or "False", clipped to the parameter's Size as a string is. Measured
    // with ACE over OLE DB (ParameterSizeAccessTests): VarWChar, Size 2, into a TEXT(10) column stores "Tr" for the
    // string "True" and for true, "Fa" for false, and each such parameter then equals the rows that hold its clip.
    [Fact]
    public void A_Boolean_bound_as_text_is_clipped_to_the_declared_size_as_a_string_is()
    {
        using var conn = OpenTemp();
        Exec(conn, "CREATE TABLE `B` (`Id` LONG PRIMARY KEY, `V` TEXT(10))");

        System.Data.Common.DbCommand Command(string sql, object value)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            var p = cmd.CreateParameter();
            p.ParameterName = "@v";
            p.DbType = System.Data.DbType.String;
            p.Size = 2;
            p.Value = value;
            cmd.Parameters.Add(p);
            return cmd;
        }

        string Stored(int id, object value)
        {
            using (var insert = Command($"INSERT INTO `B` (`Id`, `V`) VALUES ({id}, @v)", value))
                insert.ExecuteNonQuery();
            using var read = conn.CreateCommand();
            read.CommandText = $"SELECT `V` FROM `B` WHERE `Id` = {id}";
            return (string)read.ExecuteScalar()!;
        }

        int Matching(object value)
        {
            using var select = Command("SELECT COUNT(*) FROM `B` WHERE `V` = @v", value);
            return Convert.ToInt32(select.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
        }

        Assert.Equal(
            "Tr Tr Fa | 2 2 1",
            $"{Stored(1, "True")} {Stored(2, true)} {Stored(3, false)} | {Matching("True")} {Matching(true)} {Matching(false)}");
    }

    public static IEnumerable<object[]> TemporalCases() =>
    [
        [new TimeSpan(10, 9, 8), new TimeSpan(10, 9, 8)],
        [new TimeSpan(0, 10, 9, 8, 7), new TimeSpan(0, 10, 9, 8, 7)],     // the millisecond survives storage
        // Sub-millisecond is the boundary: 7.5 ms stored and 7 ms queried are the same value to the store.
        [new TimeSpan(0, 10, 9, 8, 7) + TimeSpan.FromTicks(5000), new TimeSpan(0, 10, 9, 8, 7)],
        [new TimeOnly(12, 30, 45), new TimeOnly(12, 30, 45)],
        [new DateOnly(2020, 3, 1), new DateOnly(2020, 3, 1)],
    ];
}
