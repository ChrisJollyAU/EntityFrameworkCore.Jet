using System.Buffers.Binary;
using System.Diagnostics;
using LibRed.Formats;
using LibRed.IO;
using Xunit;

namespace LibRed.Engine.Tests;

// An INSERT's counters and the global maps as ACE's locks keep them (docs/format/locking.md §4): the table's definition
// page is locked before anything else the INSERT touches and read under the lock, so two users never draw one
// AutoNumber; the global maps are changed only under page 1's lock, from an image read under it, so a page another user
// reserved is never handed out. "The other user" is a second JetLockFile handle as user 2, and its writes are made to
// the file directly, with its commit slot moved after them as ACE moves it.
public class AllocationLockStatementTests
{
    private const string WindowsOnly = "Lock-file locking is Windows-only for now.";
    private const string CurrentlyLocked = "Could not update; currently locked.";
    private const int OtherUser = 2;

    private static string Northwind => Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb");

    [Fact]
    public void An_INSERT_locks_the_tables_definition_page_before_any_other_page()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-alloclock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        (int tdef, int data) = CreateTwoRows(engine, database);

        using JetLockFile other = OtherUserOf(copy.Path);
        HoldExclusive(other, tdef);
        int affected = -1;
        Thread insert = OnOwnThread(() => affected = engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('three')"));

        // While it waits for the definition page it holds no other page: not the data page the row goes on, nor any.
        Thread.Sleep(400);
        Assert.True(insert.IsAlive);
        for (int page = 0; page < PageCount(copy.Path); page++)
            if (page != tdef) Assert.True(ExclusiveIsFree(other, page), $"Page {page} is locked while the INSERT waits.");
        Assert.True(ExclusiveIsFree(other, data));

        ReleaseExclusive(other, tdef);
        insert.Join();
        Assert.Equal(1, affected);
        Assert.Equal([1, 2, 3], Ids(engine));
        AssertNothingLocked(other, copy.Path);
    }

    [Fact]
    public void An_INSERT_after_another_users_draws_the_next_AutoNumber_after_theirs()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-alloclock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        (int tdef, _) = CreateTwoRows(engine, database);

        // The other user's INSERTs: its definition page write (the counter at 0x14 now 7), then its slot.
        WriteLastAutoNumber(copy.Path, tdef, 7);
        WriteSlot(copy.Path, OtherUser, (ushort)(Slot(copy.Path, OtherUser) + 1));

        Assert.Equal(1, engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('three')"));
        Assert.Equal([1, 2, 8], Ids(engine));
        Assert.Equal(8, LastAutoNumber(copy.Path, tdef));
    }

    [Fact]
    public void An_INSERT_waiting_for_the_definition_page_draws_after_the_insert_the_other_user_made_meanwhile()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-alloclock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        (int tdef, _) = CreateTwoRows(engine, database);

        using JetLockFile other = OtherUserOf(copy.Path);
        HoldExclusive(other, tdef);
        int affected = -1;
        Thread insert = OnOwnThread(() => affected = engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('three')"));

        // LibRed waits for the page; the other user's INSERT lands — its counter, then its slot — and it lets go.
        Thread.Sleep(400);
        Assert.True(insert.IsAlive);
        WriteLastAutoNumber(copy.Path, tdef, 20);
        WriteSlot(copy.Path, OtherUser, (ushort)(Slot(copy.Path, OtherUser) + 1));
        ReleaseExclusive(other, tdef);

        insert.Join();
        Assert.Equal(1, affected);
        Assert.Equal([1, 2, 21], Ids(engine));
        Assert.Equal(21, LastAutoNumber(copy.Path, tdef));
        AssertNothingLocked(other, copy.Path);
    }

    [Fact]
    public void An_INSERT_refused_a_page_another_user_holds_keeps_the_AutoNumber_it_drew()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-alloclock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        (int tdef, int data) = CreateTwoRows(engine, database);

        using JetLockFile other = OtherUserOf(copy.Path);
        HoldExclusive(other, data);
        var wait = Stopwatch.StartNew();
        var refused = Assert.Throws<InvalidOperationException>(() => engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('three')"));
        wait.Stop();
        Assert.Equal(CurrentlyLocked, refused.Message);
        Assert.InRange(wait.ElapsedMilliseconds, 1900, 10_000);
        ReleaseExclusive(other, data);

        // The id it drew is on the definition page in the file, and is not drawn again (ACE: the refused INSERT still
        // writes its definition page).
        Assert.Equal(3, LastAutoNumber(copy.Path, tdef));
        AssertNothingLocked(other, copy.Path);
        Assert.Equal(1, engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('four')"));
        Assert.Equal([1, 2, 4], Ids(engine));
    }

    [Fact]
    public void In_a_transaction_the_definition_page_is_held_to_ROLLBACK_and_the_AutoNumber_drawn_stays_drawn()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-alloclock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        (int tdef, _) = CreateTwoRows(engine, database);
        using JetLockFile other = OtherUserOf(copy.Path);

        engine.ExecuteNonQuery("BEGIN TRANSACTION");
        engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('three')");
        Assert.False(ExclusiveIsFree(other, tdef));
        engine.ExecuteNonQuery("ROLLBACK");
        AssertNothingLocked(other, copy.Path);
        Assert.Equal(3, LastAutoNumber(copy.Path, tdef));

        Assert.Equal(1, engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('four')"));
        Assert.Equal([1, 2, 4], Ids(engine));
    }

    [Fact]
    public void A_failed_INSERT_in_a_transaction_keeps_the_definition_page_an_earlier_INSERT_locked_until_COMMIT()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-alloclock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        (int tdef, _) = CreateTwoRows(engine, database);
        using JetLockFile other = OtherUserOf(copy.Path);

        engine.ExecuteNonQuery("BEGIN TRANSACTION");
        engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('three')");
        Assert.ThrowsAny<Exception>(() => engine.ExecuteNonQuery("INSERT INTO T (Id, A) VALUES (1, 'duplicate')"));
        Assert.False(ExclusiveIsFree(other, tdef));
        engine.ExecuteNonQuery("COMMIT");
        AssertNothingLocked(other, copy.Path);
        Assert.Equal([1, 2, 3], Ids(engine));
    }

    [Fact]
    public void A_failed_first_INSERT_in_a_transaction_lets_the_definition_page_go_with_its_savepoint()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-alloclock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        CreateTwoRows(engine, database);
        using JetLockFile other = OtherUserOf(copy.Path);

        engine.ExecuteNonQuery("BEGIN TRANSACTION");
        Assert.ThrowsAny<Exception>(() => engine.ExecuteNonQuery("INSERT INTO T (Id, A) VALUES (1, 'duplicate')"));
        engine.ExecuteNonQuery("UPDATE T SET A = 'one' WHERE Id = 1");
        // The failed INSERT's definition page went with it, but for its counters: the frame's rollback wrote them back
        // into the transaction, which holds the page again for that.
        engine.ExecuteNonQuery("COMMIT");
        AssertNothingLocked(other, copy.Path);
        Assert.Equal([1, 2], Ids(engine));
        Assert.Equal(1, engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('three')"));
        Assert.Equal(3, Ids(engine)[^1]);
    }

    [Fact]
    public void Pages_another_user_has_reserved_and_written_are_never_handed_out()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-alloclock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        engine.ExecuteNonQuery("CREATE TABLE T (Id AUTOINCREMENT PRIMARY KEY, A MEMO)");
        JetFormatBase format = FormatOf(copy.Path);

        // The other user reserves every page the global free map shows free — inside the file and past its end — and
        // writes each of them, growing the file; then its slot. Page 1 is the map's holder, as ACE creates it.
        (int row, int holderPage) = Pages.DatabaseDefinitionPage.ReadMapPointer(
            ReadRaw(copy.Path, 0, format), format.FreePagesMapPointerOffset, format);
        byte[] holder = ReadRaw(copy.Path, holderPage, format);
        var reserved = new List<int>();
        var parsed = new Pages.DataPage();
        parsed.Read(new PageBuffer(holder, holderPage), format);
        Span<byte> record = holder.AsSpan(parsed.Rows[row].Offset, parsed.Rows[row].Length);
        Assert.Equal(UsageMapType.Inline, Storage.UsageMap.RecordType(record));
        int start = Storage.UsageMap.StartPage(record, format);
        Span<byte> bits = Storage.UsageMap.InlineBits(record, format);
        for (int bit = 0; bit < bits.Length * 8; bit++)
            if (Storage.BitmapBits.Get(bits, bit))
            {
                reserved.Add(start + bit);
                Storage.BitmapBits.Set(bits, bit, false);
            }
        Assert.NotEmpty(reserved);
        Assert.Contains(reserved, page => page >= PageCount(copy.Path));
        WriteRaw(copy.Path, holderPage, holder, format);
        foreach (int page in reserved) WriteRaw(copy.Path, page, Marked(page, format), format);
        WriteSlot(copy.Path, OtherUser, (ushort)(Slot(copy.Path, OtherUser) + 1));

        // LibRed's INSERTs allocate data and long-value pages, and grow the map past what it covered.
        string memo = string.Join(" & ", Enumerable.Repeat($"'{new string('m', 250)}'", 12));   // 3000 characters
        for (int i = 0; i < 30; i++)
            Assert.Equal(1, engine.ExecuteNonQuery($"INSERT INTO T (A) VALUES ({memo})"));
        Assert.Equal(30, Ids(engine).Length);

        foreach (int page in reserved)
            Assert.True(ReadRaw(copy.Path, page, format).AsSpan().SequenceEqual(Marked(page, format)),
                $"Page {page}, which the other user reserved and wrote, was written over.");
    }

    // A page's bytes as the other user wrote them: its own number, repeated.
    private static byte[] Marked(int page, JetFormatBase format)
    {
        byte[] bytes = new byte[format.PageSize];
        for (int i = 0; i + 4 <= bytes.Length; i += 4) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i), page | 0x5A000000);
        return bytes;
    }

    // CREATE TABLE T with an AutoNumber key and two rows, which share a data page; returns its definition page and that
    // data page.
    private static (int Tdef, int Data) CreateTwoRows(QueryEngine engine, JetDatabase database)
    {
        engine.ExecuteNonQuery("CREATE TABLE T (Id AUTOINCREMENT PRIMARY KEY, A TEXT(20))");
        engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('one')");
        engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('two')");
        return database.ReadConsistent(() =>
        {
            var table = database.OpenTable("T");
            return (table.Definition.DefinitionPage, table.Rows().WithIds().Select(r => r.Id.Page).Distinct().Single());
        });
    }

    private static int[] Ids(QueryEngine engine) =>
        [.. engine.ExecuteQuery("SELECT Id FROM T ORDER BY Id").Rows.Select(r => (int)r[0]!)];

    private static Thread OnOwnThread(Action work)
    {
        var thread = new Thread(() => work()) { IsBackground = true };
        thread.Start();
        return thread;
    }

    private static JetLockFile OtherUserOf(string databasePath) =>
        JetLockFile.Open(JetLockFile.PathFor(databasePath)!, createIfMissing: false);

    private static void HoldExclusive(JetLockFile other, int page)
    {
        (long offset, long length) = JetLockLayout.ExclusiveRange(JetLockRegion.WriteRead, page, OtherUser);
        Assert.True(other.TryLock(offset, length));
    }

    private static void ReleaseExclusive(JetLockFile other, int page)
    {
        (long offset, long length) = JetLockLayout.ExclusiveRange(JetLockRegion.WriteRead, page, OtherUser);
        other.Unlock(offset, length);
    }

    private static bool ExclusiveIsFree(JetLockFile other, int page)
    {
        (long offset, long length) = JetLockLayout.ExclusiveRange(JetLockRegion.WriteRead, page, OtherUser);
        if (!other.TryLock(offset, length)) return false;
        other.Unlock(offset, length);
        return true;
    }

    private static void AssertNothingLocked(JetLockFile other, string path)
    {
        for (int page = 0; page < PageCount(path); page++)
            Assert.True(ExclusiveIsFree(other, page), $"Page {page} is still locked.");
    }

    private static int PageCount(string path) => (int)(new FileInfo(path).Length / FormatOf(path).PageSize);

    private static JetFormatBase FormatOf(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return JetFormatBase.Detect(stream);
    }

    private static byte[] ReadRaw(string path, int page, JetFormatBase format)
    {
        byte[] bytes = new byte[format.PageSize];
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        stream.Seek((long)page * format.PageSize, SeekOrigin.Begin);
        stream.ReadExactly(bytes);
        return bytes;
    }

    private static void WriteRaw(string path, int page, byte[] bytes, JetFormatBase format)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
        stream.Seek((long)page * format.PageSize, SeekOrigin.Begin);
        stream.Write(bytes);
    }

    private static int LastAutoNumber(string path, int tdef)
    {
        JetFormatBase format = FormatOf(path);
        return BinaryPrimitives.ReadInt32LittleEndian(ReadRaw(path, tdef, format).AsSpan(format.TdefLastAutoNumberOffset));
    }

    private static void WriteLastAutoNumber(string path, int tdef, int value)
    {
        JetFormatBase format = FormatOf(path);
        byte[] page = ReadRaw(path, tdef, format);
        BinaryPrimitives.WriteInt32LittleEndian(page.AsSpan(format.TdefLastAutoNumberOffset), value);
        WriteRaw(path, tdef, page, format);
    }

    private static int Slot(string path, int user)
    {
        JetFormatBase format = FormatOf(path);
        return BinaryPrimitives.ReadUInt16LittleEndian(
            ReadRaw(path, 0, format).AsSpan(format.CommitByteTableOffset + user * format.CommitByteSlotSize));
    }

    private static void WriteSlot(string path, int user, ushort value)
    {
        JetFormatBase format = FormatOf(path);
        byte[] bytes = new byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
        stream.Seek(format.CommitByteTableOffset + user * format.CommitByteSlotSize, SeekOrigin.Begin);
        stream.Write(bytes);
    }
}
