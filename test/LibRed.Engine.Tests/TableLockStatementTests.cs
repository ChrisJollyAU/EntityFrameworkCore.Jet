using System.Buffers.Binary;
using System.Diagnostics;
using LibRed.Engine.Execution;
using LibRed.IO;
using Xunit;

namespace LibRed.Engine.Tests;

// Statements and ACE's table locks (docs/format/locking.md §2, §4, §5, §7): a statement holds its shared byte in the
// table-read region on every table it reads, and in the table-write region on every table it writes — for itself, or
// for the transaction it runs in — and a design change locks the table-read region exclusively, refused while another
// user has the table in use. "The other user" is a second JetLockFile handle as user 2, which conflicts with LibRed's
// own exactly as another process's would.
public class TableLockStatementTests
{
    private const string WindowsOnly = "Lock-file locking is Windows-only for now.";
    private const int OtherUser = 2;

    private static string Northwind => Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb");

    private static string InUse(string table) =>
        $"The database engine could not lock table '{table}' because it is already in use by another person or process.";

    // A query holds its table's read byte until its rows have been read — the result disposed, or read to the end — and
    // no longer, inside a transaction too: ACE's reader holds it until it is closed, whether or not every row has been
    // fetched, and lets it go then, before COMMIT (measured on this table and query, autocommit and in a transaction).
    [Fact]
    public void A_query_holds_its_table_against_a_design_lock_until_its_rows_are_read_inside_a_transaction_too()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-tablelock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        int tdef = CreateTable(engine, database, "T");
        using JetLockFile other = OtherUserOf(copy.Path);

        foreach (string? end in (string?[])[null, "COMMIT", "ROLLBACK"])
        {
            if (end is not null) engine.ExecuteNonQuery("BEGIN TRANSACTION");
            ResultSet result = engine.ExecuteQuery("SELECT A FROM T WHERE Id = 1");
            using (IEnumerator<object?[]> rows = result.Rows.GetEnumerator())
            {
                Assert.True(rows.MoveNext());
                Assert.False(ExclusiveIsFree(other, JetLockRegion.TableRead, tdef));
                Assert.False(other.TryLock(LibRedsByte(database, JetLockRegion.TableRead, tdef), 1));
                // Another reader is not in its way: the other user's own byte is free.
                Assert.True(SharedIsFree(other, JetLockRegion.TableRead, tdef));
                Assert.True(ExclusiveIsFree(other, JetLockRegion.TableWrite, tdef));
                // Every row read, the enumeration not yet disposed: still held, as ACE's reader holds it until closed.
                Assert.False(rows.MoveNext());
                Assert.False(ExclusiveIsFree(other, JetLockRegion.TableRead, tdef));
            }
            AssertTableFree(other, tdef);

            // Read by a caller that disposes as it finishes, it is let go as well.
            Assert.Single(engine.ExecuteQuery("SELECT A FROM T WHERE Id = 1").Rows.ToList());
            AssertTableFree(other, tdef);

            if (end is not null) engine.ExecuteNonQuery(end);
            AssertTableFree(other, tdef);
        }
    }

    // A result whose rows are never read holds its table until the connection's next statement begins, and one being read
    // until it is disposed or the database closes — another user's design change is refused meanwhile, as ACE's open
    // reader refuses it (measured: 3211 until the reader is closed).
    [Fact]
    public void A_result_holds_its_table_until_read_and_one_never_read_until_the_next_statement()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-tablelock-");
        JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        int tdef = CreateTable(engine, database, "T");
        using JetLockFile other = OtherUserOf(copy.Path);

        ResultSet unread = engine.ExecuteQuery("SELECT A FROM T WHERE Id = 1");
        Assert.False(ExclusiveIsFree(other, JetLockRegion.TableRead, tdef));
        (long offset, long length) = JetLockLayout.ExclusiveRange(JetLockRegion.TableRead, tdef, OtherUser);
        Assert.False(other.TryLock(offset, length));   // another user's design change is refused
        engine.ExecuteNonQuery("UPDATE T SET A = 'two' WHERE Id = 1");
        AssertTableFree(other, tdef);
        // Read after all, it reads under a lock of its own.
        Assert.Single(unread.Rows.ToList());
        AssertTableFree(other, tdef);

        // A non-query statement's rows are not kept.
        Assert.Equal(-1, engine.ExecuteNonQuery("SELECT A FROM T"));
        AssertTableFree(other, tdef);

        // Begun and abandoned, the result holds the table through later statements until the database closes.
        IEnumerator<object?[]> abandoned = engine.ExecuteQuery("SELECT A FROM T").Rows.GetEnumerator();
        Assert.True(abandoned.MoveNext());
        Assert.Single(engine.ExecuteQuery("SELECT A FROM T").Rows.ToList());
        Assert.False(ExclusiveIsFree(other, JetLockRegion.TableRead, tdef));
        database.Dispose();
        Assert.True(other.TryLock(offset, length));
        other.Unlock(offset, length);
    }

    [Fact]
    public void A_write_holds_the_table_write_byte_for_the_transaction_and_leaves_deny_write_free()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-tablelock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        int tdef = CreateTable(engine, database, "T");
        using JetLockFile other = OtherUserOf(copy.Path);

        Assert.Equal(1, engine.ExecuteNonQuery("UPDATE T SET A = 'x' WHERE Id = 1"));
        AssertTableFree(other, tdef);

        engine.ExecuteNonQuery("BEGIN TRANSACTION");
        Assert.Equal(1, engine.ExecuteNonQuery("UPDATE T SET A = 'y' WHERE Id = 1"));
        Assert.False(other.TryLock(LibRedsByte(database, JetLockRegion.TableWrite, tdef), 1));
        Assert.False(other.TryLock(LibRedsByte(database, JetLockRegion.TableRead, tdef), 1));
        Assert.True(ExclusiveIsFree(other, JetLockRegion.TableDenyWrite, tdef));
        Assert.True(SharedIsFree(other, JetLockRegion.TableWrite, tdef));

        engine.ExecuteNonQuery("COMMIT");
        AssertTableFree(other, tdef);
    }

    [Theory]
    [InlineData("ALTER TABLE T ADD COLUMN C INTEGER")]
    [InlineData("CREATE INDEX IxNew ON T (A)")]
    [InlineData("DROP INDEX IxA ON T")]
    [InlineData("DROP TABLE T")]
    public void A_design_change_on_a_table_another_user_has_in_use_fails_with_3211_and_succeeds_once_it_lets_go(string ddl)
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-tablelock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        int tdef = CreateTable(engine, database, "T");
        engine.ExecuteNonQuery("CREATE INDEX IxA ON T (A)");
        using JetLockFile other = OtherUserOf(copy.Path);

        long shared = JetLockLayout.SharedByte(JetLockRegion.TableRead, tdef, OtherUser);
        Assert.True(other.TryLock(shared, 1));
        var wait = Stopwatch.StartNew();
        var refused = Assert.Throws<InvalidOperationException>(() => engine.ExecuteNonQuery(ddl));
        wait.Stop();
        Assert.Equal(InUse("T"), refused.Message);
        Assert.InRange(wait.ElapsedMilliseconds, 1900, 10_000);
        AssertTableFree(other, tdef, exceptOwnByte: true);
        other.Unlock(shared, 1);

        // Let go part-way through the retries, it goes ahead.
        Assert.True(other.TryLock(shared, 1));
        Thread release = OnOwnThread(() =>
        {
            Thread.Sleep(500);
            other.Unlock(shared, 1);
        });
        engine.ExecuteNonQuery(ddl);
        release.Join();
        AssertTableFree(other, tdef);
    }

    [Fact]
    public void A_SELECT_waiting_for_a_table_another_user_is_altering_runs_on_the_new_definition()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-tablelock-");
        string altered = AlteredByAnotherUser(copy.Path);
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        AssertColumnsReadingTheRows(engine, ["Id", "A"]);
        int tdef = database.Catalog.FindTable("T")!.DefinitionPage;

        using JetLockFile other = OtherUserOf(copy.Path);
        (long offset, long length) = JetLockLayout.ExclusiveRange(JetLockRegion.TableRead, tdef, OtherUser);
        Assert.True(other.TryLock(offset, length));
        IReadOnlyList<string>? columns = null;
        List<object?[]>? rows = null;
        Thread select = OnOwnThread(() =>
        {
            ResultSet result = engine.ExecuteQuery("SELECT * FROM T");
            (columns, rows) = (result.ColumnNames, result.Rows.ToList());
        });

        // LibRed has planned against the old definition and is waiting for the table. The other user's design
        // change lands — its pages, then its slot — and it lets go.
        Thread.Sleep(400);
        Assert.True(select.IsAlive);
        LandPagesOf(altered, copy.Path);
        WriteSlot(copy.Path, OtherUser, (ushort)(Slot(copy.Path, OtherUser) + 1));
        other.Unlock(offset, length);

        select.Join();
        Assert.Equal(["Id", "A", "C"], columns);
        Assert.Equal(new object?[] { 1, "one", null }, Assert.Single(rows!));
        other.Dispose();   // before LibRed's last close, which deletes the lock file only when alone
    }

    [Fact]
    public void An_UPDATE_waiting_for_a_table_another_user_is_altering_runs_on_the_new_definition()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-tablelock-");
        string altered = AlteredByAnotherUser(copy.Path);
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        AssertColumnsReadingTheRows(engine, ["Id", "A"]);
        int tdef = database.Catalog.FindTable("T")!.DefinitionPage;

        using JetLockFile other = OtherUserOf(copy.Path);
        (long offset, long length) = JetLockLayout.ExclusiveRange(JetLockRegion.TableRead, tdef, OtherUser);
        Assert.True(other.TryLock(offset, length));
        int affected = -1;
        Thread update = OnOwnThread(() => affected = engine.ExecuteNonQuery("UPDATE T SET A = 'two' WHERE Id = 1"));

        Thread.Sleep(400);
        Assert.True(update.IsAlive);
        LandPagesOf(altered, copy.Path);
        WriteSlot(copy.Path, OtherUser, (ushort)(Slot(copy.Path, OtherUser) + 1));
        other.Unlock(offset, length);

        update.Join();
        Assert.Equal(1, affected);
        ResultSet after = engine.ExecuteQuery("SELECT * FROM T");
        Assert.Equal(["Id", "A", "C"], after.ColumnNames);
        Assert.Equal(new object?[] { 1, "two", null }, Assert.Single(after.Rows.ToList()));
        AssertTableFree(other, tdef);
        other.Dispose();

        // And so the file says, to a reader that has never seen it.
        database.Dispose();
        using JetDatabase fresh = JetDatabase.Open(copy.Path);
        Assert.Equal(new object?[] { 1, "two", null }, Assert.Single(new QueryEngine(fresh).ExecuteQuery("SELECT * FROM T").Rows.ToList()));
    }

    // Two connections of one process are two Jet users, as ACE's are. ACE, this sequence on this table (measured, two
    // OLE DB connections in one process): with A's reader open in its transaction, B's ALTER TABLE is refused with
    // 3211 after about 2.2 s, and A's own CREATE INDEX with 3211 at once; with the reader closed A's CREATE INDEX goes
    // ahead and holds the table for its transaction; after A's COMMIT, B's ALTER TABLE goes ahead.
    [Fact]
    public void A_design_change_is_refused_while_another_connection_reads_the_table_and_a_connections_own_at_once()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-tablelock-");
        using JetDatabase first = JetDatabase.Open(copy.Path, readOnly: false);
        using JetDatabase second = JetDatabase.Open(copy.Path, readOnly: false);
        var a = new QueryEngine(first);
        var b = new QueryEngine(second);
        int tdef = CreateTable(a, first, "T");
        Assert.Equal((1, 2), (first.Channel.UserNumber, second.Channel.UserNumber));

        a.ExecuteNonQuery("BEGIN TRANSACTION");
        IEnumerator<object?[]> reading = a.ExecuteQuery("SELECT A FROM T WHERE Id = 1").Rows.GetEnumerator();
        Assert.True(reading.MoveNext());

        var wait = Stopwatch.StartNew();
        var refused = Assert.Throws<InvalidOperationException>(() => b.ExecuteNonQuery("ALTER TABLE T ADD COLUMN C INTEGER"));
        wait.Stop();
        Assert.Equal(InUse("T"), refused.Message);
        Assert.InRange(wait.ElapsedMilliseconds, 1900, 10_000);

        wait.Restart();
        refused = Assert.Throws<InvalidOperationException>(() => a.ExecuteNonQuery("CREATE INDEX IxA ON T (Id)"));
        Assert.Equal(InUse("T"), refused.Message);
        Assert.True(wait.ElapsedMilliseconds < 1000, $"A's own design change waited {wait.ElapsedMilliseconds} ms.");
        // Its own INSERT is not a design change, and goes ahead beside its reader (measured).
        Assert.Equal(1, a.ExecuteNonQuery("INSERT INTO T (Id, A) VALUES (2, 'two')"));

        reading.Dispose();
        // The transaction has now written T: its own design change is refused still (measured: INSERT, then ALTER TABLE).
        refused = Assert.Throws<InvalidOperationException>(() => a.ExecuteNonQuery("CREATE INDEX IxA ON T (Id)"));
        Assert.Equal(InUse("T"), refused.Message);
        a.ExecuteNonQuery("COMMIT");

        using (JetLockFile other = OtherUserOf(copy.Path))
        {
            long window = JetLockLayout.PageWindow(JetLockRegion.TableRead, tdef);
            Assert.True(other.TryLock(window + 1, JetLockLayout.PageWindowSize - 1), "A table-read byte is still held.");
        }
        b.ExecuteNonQuery("ALTER TABLE T ADD COLUMN C INTEGER");
        a.ExecuteNonQuery("CREATE INDEX IxA ON T (Id)");
        Assert.Equal(["Id", "A", "C"], b.ExecuteQuery("SELECT * FROM T").ColumnNames);
    }

    // A connection's own design change of a table it has in use is refused at once, with 3211 — on an exclusive open
    // too, where there is no lock file: it is the engine's rule, not a lock's. ACE on this table (measured, shared and
    // Mode=Share Exclusive alike): refused while the connection's own reader is open on it, its own INSERT beside the
    // reader going ahead; refused after the transaction's own UPDATE, INSERT or DELETE of it — one matching no row
    // included; not refused after a SELECT whose reader is closed, after the table was only the source of an
    // INSERT … SELECT, after an autocommit UPDATE, or once the transaction has itself changed the table's design.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_connections_own_design_change_of_a_table_it_has_in_use_is_refused_at_once(bool exclusive)
    {
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-tablelock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false, exclusive: exclusive);
        var engine = new QueryEngine(database);
        CreateTable(engine, database, "T");
        CreateTable(engine, database, "S");
        int column = 0;
        void Alter() => engine.ExecuteNonQuery($"ALTER TABLE T ADD COLUMN C{++column} INTEGER");
        void AlterRefused()
        {
            var wait = Stopwatch.StartNew();
            var refused = Assert.Throws<InvalidOperationException>(Alter);
            Assert.Equal(InUse("T"), refused.Message);
            Assert.True(wait.ElapsedMilliseconds < 1000, $"The refusal took {wait.ElapsedMilliseconds} ms.");
        }

        // Its own open reader.
        using (IEnumerator<object?[]> reading = engine.ExecuteQuery("SELECT A FROM T").Rows.GetEnumerator())
        {
            Assert.True(reading.MoveNext());
            AlterRefused();
            Assert.Throws<InvalidOperationException>(() => engine.ExecuteNonQuery("CREATE INDEX IxA ON T (A)"));
            Assert.Throws<InvalidOperationException>(() => engine.ExecuteNonQuery("DROP TABLE T"));
            Assert.Equal(1, engine.ExecuteNonQuery("INSERT INTO T (Id, A) VALUES (2, 'two')"));
        }
        Alter();   // the reader closed, and the INSERT committed on its own

        // Its own transaction, having written the table.
        foreach (string write in (string[])["UPDATE T SET A = 'z' WHERE Id = 1", "UPDATE T SET A = 'z' WHERE Id = 99",
                     "INSERT INTO T (Id, A) VALUES (3, 'three')", "DELETE FROM T WHERE Id = 2"])
        {
            engine.ExecuteNonQuery("BEGIN TRANSACTION");
            engine.ExecuteNonQuery(write);
            AlterRefused();
            engine.ExecuteNonQuery("ALTER TABLE S ADD COLUMN C" + ++column + " INTEGER");   // another table is not in use
            engine.ExecuteNonQuery("ROLLBACK");
        }

        // What does not have the table in use.
        engine.ExecuteNonQuery("BEGIN TRANSACTION");
        Assert.Single(engine.ExecuteQuery("SELECT A FROM T WHERE Id = 1").Rows.ToList());
        engine.ExecuteNonQuery("INSERT INTO S (Id, A) SELECT Id + 10, A FROM T");
        Alter();
        // And having changed its design, the transaction writes it and changes it again.
        Assert.Equal(1, engine.ExecuteNonQuery("UPDATE T SET A = 'z' WHERE Id = 1"));
        Alter();
        engine.ExecuteNonQuery("COMMIT");

        Assert.Equal(1, engine.ExecuteNonQuery("UPDATE T SET A = 'y' WHERE Id = 1"));
        Alter();
    }

    // ACE, this sequence on these tables (measured, another connection's ALTER TABLE as the probe): T1 and T2 are both
    // refused it — 3211 — while the nested transaction is open, after it is rolled back, and after the failed INSERT;
    // both go ahead once the outer transaction has committed. An UPDATE that changes nothing holds its table too.
    [Fact]
    public void A_savepoint_rollback_and_a_failed_statement_keep_the_tables_they_wrote_until_the_transaction_ends()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-tablelock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        int first = CreateTable(engine, database, "T1");
        int second = CreateTable(engine, database, "T2");
        using JetLockFile other = OtherUserOf(copy.Path);

        // T1 written before the savepoint, so the transaction holds it: a query's read lock would be its result's, let go
        // once read, as ACE's reader lets go before COMMIT (measured).
        engine.ExecuteNonQuery("BEGIN TRANSACTION");
        Assert.Equal(1, engine.ExecuteNonQuery("UPDATE T1 SET A = 'one' WHERE Id = 1"));
        engine.ExecuteNonQuery("BEGIN TRANSACTION");
        Assert.Single(engine.ExecuteQuery("SELECT A FROM T1 WHERE Id = 1").Rows.ToList());
        Assert.Equal(1, engine.ExecuteNonQuery("UPDATE T2 SET A = 'x' WHERE Id = 1"));
        Assert.False(ExclusiveIsFree(other, JetLockRegion.TableRead, first));
        Assert.False(ExclusiveIsFree(other, JetLockRegion.TableRead, second));

        engine.ExecuteNonQuery("ROLLBACK");
        Assert.False(ExclusiveIsFree(other, JetLockRegion.TableRead, first));
        Assert.False(ExclusiveIsFree(other, JetLockRegion.TableRead, second));

        // A statement that fails keeps its table too.
        Assert.ThrowsAny<Exception>(() => engine.ExecuteNonQuery("INSERT INTO T2 (Id, A) VALUES (1, 'dup')"));
        Assert.False(ExclusiveIsFree(other, JetLockRegion.TableRead, second));
        Assert.False(ExclusiveIsFree(other, JetLockRegion.TableRead, first));

        engine.ExecuteNonQuery("COMMIT");
        AssertTableFree(other, first);
        AssertTableFree(other, second);
        Assert.Equal("one", engine.ExecuteQuery("SELECT A FROM T2 WHERE Id = 1").Rows.Single()[0]);
    }

    // T's columns, its rows read and the result let go — an unread result holds the table, as an unclosed ACE reader does
    // (measured), and these tests then need another user to take it.
    private static void AssertColumnsReadingTheRows(QueryEngine engine, string[] expected)
    {
        ResultSet result = engine.ExecuteQuery("SELECT * FROM T");
        _ = result.Rows.ToList();
        Assert.Equal(expected, result.ColumnNames);
    }

    // CREATE TABLE <name> with one row (Id 1, A 'one'); returns its definition page.
    private static int CreateTable(QueryEngine engine, JetDatabase database, string name)
    {
        engine.ExecuteNonQuery($"CREATE TABLE {name} (Id INTEGER PRIMARY KEY, A TEXT(20))");
        engine.ExecuteNonQuery($"INSERT INTO {name} (Id, A) VALUES (1, 'one')");
        return database.Catalog.FindTable(name)!.DefinitionPage;
    }

    // Table T with one row, made in <path>; returns a copy of the file in which another user — a LibRed of its own —
    // has then added column C.
    private static string AlteredByAnotherUser(string path)
    {
        using (JetDatabase setup = JetDatabase.Open(path, readOnly: false))
            CreateTable(new QueryEngine(setup), setup, "T");
        string altered = TemporaryDatabase.CopyPath(path, "libred-tablelock-");
        using (JetDatabase alter = JetDatabase.Open(altered, readOnly: false))
            new QueryEngine(alter).ExecuteNonQuery("ALTER TABLE T ADD COLUMN C INTEGER");
        return altered;
    }

    // Writes every page of <from> that differs from <to> into <to>, page 0 only up to its commit-slot table — what
    // another user's committed batch leaves in the file.
    private static void LandPagesOf(string from, string to)
    {
        byte[] source = File.ReadAllBytes(from);
        Formats.JetFormatBase format;
        using (var read = new FileStream(to, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            format = Formats.JetFormatBase.Detect(read);
        using var stream = new FileStream(to, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        byte[] page = new byte[format.PageSize];
        for (int at = 0; at < source.Length; at += format.PageSize)
        {
            int length = at == 0 ? format.CommitByteTableOffset : format.PageSize;
            if (at + format.PageSize <= stream.Length)
            {
                stream.Seek(at, SeekOrigin.Begin);
                stream.ReadExactly(page);
                if (page.AsSpan(0, length).SequenceEqual(source.AsSpan(at, length))) continue;
            }
            stream.Seek(at, SeekOrigin.Begin);
            stream.Write(source, at, length);
        }
    }

    // A thread of its own rather than the pool's: a statement waiting for a lock blocks its thread, and under the
    // suite's parallel load a pool work item can wait longer than ACE's retries to be started.
    private static Thread OnOwnThread(Action work)
    {
        var thread = new Thread(() => work()) { IsBackground = true };
        thread.Start();
        return thread;
    }

    private static JetLockFile OtherUserOf(string databasePath) =>
        JetLockFile.Open(JetLockFile.PathFor(databasePath)!, createIfMissing: false);

    private static long LibRedsByte(JetDatabase database, JetLockRegion region, int tdef) =>
        JetLockLayout.SharedByte(region, tdef, database.Channel.UserNumber!.Value);

    private static bool ExclusiveIsFree(JetLockFile other, JetLockRegion region, int page)
    {
        (long offset, long length) = JetLockLayout.ExclusiveRange(region, page, OtherUser);
        if (!other.TryLock(offset, length)) return false;
        other.Unlock(offset, length);
        return true;
    }

    private static bool SharedIsFree(JetLockFile other, JetLockRegion region, int page)
    {
        long shared = JetLockLayout.SharedByte(region, page, OtherUser);
        if (!other.TryLock(shared, 1)) return false;
        other.Unlock(shared, 1);
        return true;
    }

    // LibRed holds nothing on the table in any of the three regions: user 2 can take each exclusively — or, while it
    // holds its own table-read byte, every other byte of that window.
    private static void AssertTableFree(JetLockFile other, int tdef, bool exceptOwnByte = false)
    {
        foreach (JetLockRegion region in (JetLockRegion[])[JetLockRegion.TableRead, JetLockRegion.TableWrite, JetLockRegion.TableDenyWrite])
        {
            if (exceptOwnByte && region == JetLockRegion.TableRead)
            {
                (long byte1, _) = JetLockLayout.ExclusiveRange(region, tdef, OtherUser);
                Assert.True(other.TryLock(byte1, OtherUser - 1), "LibRed still holds a byte of the table-read window.");
                other.Unlock(byte1, OtherUser - 1);
                Assert.True(other.TryLock(byte1 + OtherUser, JetLockLayout.ExclusiveLengthBase), "LibRed still holds a byte of the table-read window.");
                other.Unlock(byte1 + OtherUser, JetLockLayout.ExclusiveLengthBase);
                continue;
            }
            Assert.True(ExclusiveIsFree(other, region, tdef), $"LibRed still holds the table in region {(int)region}.");
        }
    }

    // The table is held by a reader and by nothing else: LibRed's table-read byte, and none of its design lock.
    private static void AssertHeldForReadOnly(JetLockFile other, JetDatabase database, int tdef)
    {
        Assert.False(other.TryLock(LibRedsByte(database, JetLockRegion.TableRead, tdef), 1));
        Assert.True(SharedIsFree(other, JetLockRegion.TableRead, tdef));
        Assert.True(ExclusiveIsFree(other, JetLockRegion.TableWrite, tdef));
    }

    private static int SlotOffset(string path, int user)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        Formats.JetFormatBase format = Formats.JetFormatBase.Detect(stream);
        return format.CommitByteTableOffset + user * format.CommitByteSlotSize;
    }

    private static int Slot(string path, int user)
    {
        byte[] slot = new byte[sizeof(ushort)];
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        stream.Seek(SlotOffset(path, user), SeekOrigin.Begin);
        stream.ReadExactly(slot);
        return BinaryPrimitives.ReadUInt16LittleEndian(slot);
    }

    private static void WriteSlot(string path, int user, ushort value)
    {
        byte[] bytes = new byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
        int offset = SlotOffset(path, user);
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
        stream.Seek(offset, SeekOrigin.Begin);
        stream.Write(bytes);
    }
}
