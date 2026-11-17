using System.Data.OleDb;
using LibRed;
using LibRed.Crypto;
using LibRed.Engine;
using Xunit;

namespace LibRed.Engine.Tests;

[Collection(AceCollection.Name)]
public class ConcurrentAllocationTests
{
    [Fact]
    public void Concurrent_autonumber_and_lval_allocations_conflict_then_retry_without_duplicate_ids_or_pages()
    {
        string path = Fresh("concurrent-allocation-");
        try
        {
            CreateTable(path);
            RunConflictAndRetry(path);
            VerifyWithLibRed(path);
            VerifyWithAce(path);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    [Fact]
    public void Encrypted_concurrent_allocations_conflict_then_retry_and_remain_readable_by_ace()
    {
        const string password = "Concurrent-S3cret!";
        string path = Fresh("concurrent-encrypted-");
        try
        {
            CreateTable(path);
            using (var db = JetDatabase.Open(path, readOnly: false, exclusive: true))
                DatabaseEncryption.SetPassword(db, password, AccessEncryption.Agile);

            RunConflictAndRetry(path, password);
            VerifyWithLibRed(path, password);
            VerifyWithAce(path, password);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    private static void RunConflictAndRetry(string path, string? password = null)
    {
        using var firstDb = JetDatabase.Open(path, readOnly: false, password: password);
        using var secondDb = JetDatabase.Open(path, readOnly: false, password: password);
        var first = new QueryEngine(firstDb);
        var second = new QueryEngine(secondDb);
        int committedPages = firstDb.OpenTable("ConcurrentAlloc").Channel.PageCount;
        string firstMemo = new('A', 24000);
        string staleMemo = new('B', 28000);
        string retryMemo = new('C', 32000);

        first.ExecuteNonQuery("BEGIN TRANSACTION");
        second.ExecuteNonQuery("BEGIN TRANSACTION");
        first.ExecuteNonQuery(
            $"INSERT INTO ConcurrentAlloc (K, M) VALUES ('first', '{firstMemo}')");

        // ACE, two connections in one process, this sequence on this table (measured): the first draws 1; the second's
        // INSERT draws 2 — the first's INSERT wrote its id ahead of its COMMIT (locking.md §5) — and is refused with
        // 3218 after ACE's retries, 2.2 to 2.7 s; both then commit, the second nothing; and the retry below gets 3.
        // Each connection is a Jet user of its own, in LibRed as in ACE.
        var wait = System.Diagnostics.Stopwatch.StartNew();
        var refused = Assert.Throws<InvalidOperationException>(() => second.ExecuteNonQuery(
            $"INSERT INTO ConcurrentAlloc (K, M) VALUES ('stale', '{staleMemo}')"));
        wait.Stop();
        Assert.Equal("Could not update; currently locked.", refused.Message);
        Assert.InRange(wait.ElapsedMilliseconds, 1900, 10_000);
        Assert.Equal(1, Identity(first));
        Assert.True(firstDb.OpenTable("ConcurrentAlloc").Channel.PageCount > committedPages);

        first.ExecuteNonQuery("COMMIT");
        second.ExecuteNonQuery("COMMIT");
        Assert.False(secondDb.InTransaction);

        Assert.Single(second.ExecuteQuery("SELECT Id FROM ConcurrentAlloc WHERE K = 'first'").Rows);
        Assert.Empty(second.ExecuteQuery("SELECT Id FROM ConcurrentAlloc WHERE K = 'stale'").Rows);

        // The refused insert's id stays drawn: ACE's retry after the same sequence gets 3 (measured).
        second.ExecuteNonQuery("BEGIN TRANSACTION");
        second.ExecuteNonQuery(
            $"INSERT INTO ConcurrentAlloc (K, M) VALUES ('retry', '{retryMemo}')");
        Assert.Equal(3, Identity(second));
        second.ExecuteNonQuery("COMMIT");

        var rows = first.ExecuteQuery("SELECT Id, K FROM ConcurrentAlloc ORDER BY Id").Rows.ToArray();
        Assert.Equal(2, rows.Length);
        Assert.Equal((1, "first"), (Convert.ToInt32(rows[0][0]), (string)rows[0][1]!));
        Assert.Equal((3, "retry"), (Convert.ToInt32(rows[1][0]), (string)rows[1][1]!));
    }

    private static void CreateTable(string path)
    {
        using var db = JetDatabase.Open(path, readOnly: false);
        var e = new QueryEngine(db);
        e.ExecuteNonQuery("CREATE TABLE ConcurrentAlloc (Id COUNTER PRIMARY KEY, K TEXT(30), M MEMO)");
        e.ExecuteNonQuery("CREATE UNIQUE INDEX UX_ConcurrentAlloc_K ON ConcurrentAlloc (K)");
    }

    private static void VerifyWithLibRed(string path, string? password = null)
    {
        using var db = JetDatabase.Open(path, password: password);
        var e = new QueryEngine(db);
        Assert.Equal(2, Convert.ToInt32(e.ExecuteQuery("SELECT COUNT(*) FROM ConcurrentAlloc").Rows.Single()[0]));
        Assert.Equal(24000, ((string)e.ExecuteQuery("SELECT M FROM ConcurrentAlloc WHERE Id = 1").Rows.Single()[0]!).Length);
        Assert.Equal(32000, ((string)e.ExecuteQuery("SELECT M FROM ConcurrentAlloc WHERE Id = 3").Rows.Single()[0]!).Length);
        Assert.Empty(e.ExecuteQuery("SELECT Id FROM ConcurrentAlloc WHERE K = 'stale'").Rows);
    }

    private static void VerifyWithAce(string path, string? password = null)
    {
        using var connection = AceTestDatabase.Open(path, password);
        AssertScalar(connection, "SELECT COUNT(*) FROM ConcurrentAlloc", 2);
        AssertScalar(connection, "SELECT COUNT(*) FROM ConcurrentAlloc WHERE Id = 1 AND K = 'first'", 1);
        AssertScalar(connection, "SELECT COUNT(*) FROM ConcurrentAlloc WHERE Id = 3 AND K = 'retry'", 1);
        AssertScalar(connection, "SELECT COUNT(*) FROM ConcurrentAlloc WHERE K = 'stale'", 0);
        Assert.Equal(24000, ((string)ExecuteScalar(connection, "SELECT M FROM ConcurrentAlloc WHERE Id = 1")!).Length);
        Assert.Equal(32000, ((string)ExecuteScalar(connection, "SELECT M FROM ConcurrentAlloc WHERE Id = 3")!).Length);
    }

    private static int Identity(QueryEngine engine) =>
        Convert.ToInt32(engine.ExecuteQuery("SELECT @@IDENTITY").Rows.Single()[0]);

    private static string Fresh(string prefix) =>
        TemporaryDatabase.CopyPath(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), prefix);

    private static object? ExecuteScalar(OleDbConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar();
    }

    private static void AssertScalar(OleDbConnection connection, string sql, int expected) =>
        Assert.Equal(expected, Convert.ToInt32(ExecuteScalar(connection, sql)));
}
