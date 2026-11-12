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

    [Fact]
    public void A_transaction_that_read_a_table_holds_it_against_a_design_lock_until_COMMIT_or_ROLLBACK()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-tablelock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        int tdef = CreateTable(engine, database, "T");
        using JetLockFile other = OtherUserOf(copy.Path);

        // An autocommit SELECT holds its byte for the statement alone.
        Assert.Single(engine.ExecuteQuery("SELECT A FROM T WHERE Id = 1").Rows.ToList());
        AssertTableFree(other, tdef);

        foreach (string end in (string[])["COMMIT", "ROLLBACK"])
        {
            engine.ExecuteNonQuery("BEGIN TRANSACTION");
            Assert.Single(engine.ExecuteQuery("SELECT A FROM T WHERE Id = 1").Rows.ToList());
            Assert.False(ExclusiveIsFree(other, JetLockRegion.TableRead, tdef));
            Assert.False(other.TryLock(LibRedsByte(database, JetLockRegion.TableRead, tdef), 1));
            // Another reader is not in its way: the other user's own byte is free.
            Assert.True(SharedIsFree(other, JetLockRegion.TableRead, tdef));
            Assert.True(ExclusiveIsFree(other, JetLockRegion.TableWrite, tdef));

            engine.ExecuteNonQuery(end);
            AssertTableFree(other, tdef);
        }
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
        Assert.Equal(["Id", "A"], engine.ExecuteQuery("SELECT * FROM T").ColumnNames);
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
        Assert.Equal(["Id", "A"], engine.ExecuteQuery("SELECT * FROM T").ColumnNames);
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

    [Fact]
    public void Connections_of_one_process_change_the_design_of_a_table_one_has_in_a_transaction_as_they_always_could()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-tablelock-");
        using JetDatabase first = JetDatabase.Open(copy.Path, readOnly: false);
        using JetDatabase second = JetDatabase.Open(copy.Path, readOnly: false);
        var a = new QueryEngine(first);
        var b = new QueryEngine(second);
        int tdef = CreateTable(a, first, "T");
        using JetLockFile other = OtherUserOf(copy.Path);

        a.ExecuteNonQuery("BEGIN TRANSACTION");
        Assert.Single(a.ExecuteQuery("SELECT A FROM T WHERE Id = 1").Rows.ToList());

        b.ExecuteNonQuery("ALTER TABLE T ADD COLUMN C INTEGER");
        AssertHeldForReadOnly(other, first, tdef);
        b.ExecuteNonQuery("CREATE INDEX IxB ON T (A)");
        AssertHeldForReadOnly(other, first, tdef);
        // A's own design change belongs to its transaction: the other user cannot even read the table until it ends.
        a.ExecuteNonQuery("CREATE INDEX IxA ON T (Id)");
        Assert.False(SharedIsFree(other, JetLockRegion.TableRead, tdef));
        Assert.False(ExclusiveIsFree(other, JetLockRegion.TableRead, tdef));

        a.ExecuteNonQuery("COMMIT");
        AssertTableFree(other, tdef);
        Assert.Equal(["Id", "A", "C"], b.ExecuteQuery("SELECT * FROM T").ColumnNames);
    }

    [Fact]
    public void A_savepoint_rollback_lets_go_of_the_tables_first_used_after_it_and_keeps_the_earlier_ones()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-tablelock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        int first = CreateTable(engine, database, "T1");
        int second = CreateTable(engine, database, "T2");
        using JetLockFile other = OtherUserOf(copy.Path);

        engine.ExecuteNonQuery("BEGIN TRANSACTION");
        Assert.Single(engine.ExecuteQuery("SELECT A FROM T1 WHERE Id = 1").Rows.ToList());
        engine.ExecuteNonQuery("BEGIN TRANSACTION");
        Assert.Single(engine.ExecuteQuery("SELECT A FROM T1 WHERE Id = 1").Rows.ToList());
        Assert.Equal(1, engine.ExecuteNonQuery("UPDATE T2 SET A = 'x' WHERE Id = 1"));
        Assert.False(ExclusiveIsFree(other, JetLockRegion.TableRead, first));
        Assert.False(ExclusiveIsFree(other, JetLockRegion.TableRead, second));

        engine.ExecuteNonQuery("ROLLBACK");
        Assert.False(ExclusiveIsFree(other, JetLockRegion.TableRead, first));
        AssertTableFree(other, second);

        // A statement that fails lets go of what it took, and keeps what the transaction already held.
        Assert.ThrowsAny<Exception>(() => engine.ExecuteNonQuery("INSERT INTO T2 (Id, A) VALUES (1, 'dup')"));
        AssertTableFree(other, second);
        Assert.False(ExclusiveIsFree(other, JetLockRegion.TableRead, first));

        engine.ExecuteNonQuery("COMMIT");
        AssertTableFree(other, first);
        Assert.Equal("one", engine.ExecuteQuery("SELECT A FROM T2 WHERE Id = 1").Rows.Single()[0]);
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
