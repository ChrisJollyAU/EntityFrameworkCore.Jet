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

    // An INSERT inside a transaction, as ACE's (measured, locking.md §5): the table's definition page is written with the
    // AutoNumber drawn and nothing else — not the row count, not the index counts — one more on the slot, and let go,
    // before COMMIT; the row's data page stays held. LibRed writes it as the statement returns, where ACE writes it about
    // 500 ms after. A ROLLBACK then leaves the file as it was but for that AutoNumber, as ACE's (locking.md §4).
    [Fact]
    public void In_a_transaction_an_INSERT_writes_its_AutoNumber_and_lets_the_definition_page_go_and_ROLLBACK_keeps_it()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-alloclock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        (int tdef, int data) = CreateTwoRows(engine, database);
        JetFormatBase format = FormatOf(copy.Path);
        byte[] before = ReadRaw(copy.Path, tdef, format);
        int slot = Slot(copy.Path, LibRedUser);
        using JetLockFile other = OtherUserOf(copy.Path);

        engine.ExecuteNonQuery("BEGIN TRANSACTION");
        engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('three')");
        AssertOnlyAutoNumberMoved(before, ReadRaw(copy.Path, tdef, format), 3, format);
        Assert.Equal(slot + 1, Slot(copy.Path, LibRedUser));
        Assert.True(ExclusiveIsFree(other, tdef), "The definition page is still locked.");
        Assert.False(ExclusiveIsFree(other, data), "The row's data page was let go before ROLLBACK.");

        engine.ExecuteNonQuery("ROLLBACK");
        AssertNothingLocked(other, copy.Path);
        AssertOnlyAutoNumberMoved(before, ReadRaw(copy.Path, tdef, format), 3, format);
        Assert.Equal([1, 2], Ids(engine));

        Assert.Equal(1, engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('four')"));
        Assert.Equal([1, 2, 4], Ids(engine));
        LibRed.Core.Tests.TestDatabases.AssertPageOwnership(database);
    }

    // Another user draws AutoNumbers, and deletes a row, while LibRed's transaction that inserted is open — as ACE lets
    // another user do beside its own (measured, locking.md §5): LibRed's next INSERT in the transaction draws after
    // theirs, and its COMMIT adds its row count to the one the file then has and keeps their AutoNumber, as ACE's COMMIT
    // does. "The other user" takes the definition page's lock to write it, as ACE does.
    [Fact]
    public void After_the_early_write_another_users_AutoNumbers_and_row_count_stay_through_the_INSERT_and_COMMIT_that_follow()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-alloclock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        (int tdef, _) = CreateTwoRows(engine, database);
        JetFormatBase format = FormatOf(copy.Path);
        using JetLockFile other = OtherUserOf(copy.Path);

        engine.ExecuteNonQuery("BEGIN TRANSACTION");
        engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('three')");
        OtherUserCounts(other, copy.Path, tdef, lastAutoNumber: 10, rowsChange: -1);
        engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('four')");
        Assert.Equal(11, LastAutoNumber(copy.Path, tdef));
        Assert.Equal(1, RowCount(copy.Path, tdef));   // the other user's count; LibRed's two rows wait for COMMIT
        OtherUserCounts(other, copy.Path, tdef, lastAutoNumber: 20, rowsChange: 0);
        engine.ExecuteNonQuery("COMMIT");

        AssertNothingLocked(other, copy.Path);
        Assert.Equal(20, LastAutoNumber(copy.Path, tdef));
        Assert.Equal(3, RowCount(copy.Path, tdef));
        Assert.Equal([1, 2, 3, 11], Ids(engine));
        Assert.Equal(1, engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('five')"));
        Assert.Equal(21, Ids(engine)[^1]);
    }

    // A design change to the table after the early write and another user's statement on it: the ALTER TABLE lays the
    // definition page out afresh from the page as the file has it, so neither their AutoNumber nor their count is lost,
    // and the transaction's own rows are counted onto theirs at COMMIT.
    [Fact]
    public void An_ALTER_TABLE_after_the_early_write_keeps_another_users_AutoNumber_and_row_count()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-alloclock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        (int tdef, _) = CreateTwoRows(engine, database);
        using JetLockFile other = OtherUserOf(copy.Path);

        engine.ExecuteNonQuery("BEGIN TRANSACTION");
        engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('three')");
        OtherUserCounts(other, copy.Path, tdef, lastAutoNumber: 10, rowsChange: -1);
        engine.ExecuteNonQuery("ALTER TABLE T ADD COLUMN B LONG");
        engine.ExecuteNonQuery("INSERT INTO T (A, B) VALUES ('four', 4)");
        engine.ExecuteNonQuery("COMMIT");

        AssertNothingLocked(other, copy.Path);
        Assert.Equal(11, LastAutoNumber(copy.Path, tdef));
        Assert.Equal(3, RowCount(copy.Path, tdef));
        Assert.Equal([1, 2, 3, 11], Ids(engine));
        Assert.Equal(4, Convert.ToInt32(engine.ExecuteQuery("SELECT B FROM T WHERE Id = 11").Rows.Single()[0]));
        LibRed.Core.Tests.TestDatabases.AssertPageOwnership(database);
    }

    // A ROLLBACK after another user drew an AutoNumber beside the transaction: the file keeps theirs — the transaction's
    // own was written before, and nothing of it is written again over theirs.
    [Fact]
    public void A_ROLLBACK_after_another_user_drew_meanwhile_keeps_their_AutoNumber()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-alloclock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        (int tdef, _) = CreateTwoRows(engine, database);
        JetFormatBase format = FormatOf(copy.Path);
        byte[] before = ReadRaw(copy.Path, tdef, format);
        using JetLockFile other = OtherUserOf(copy.Path);

        engine.ExecuteNonQuery("BEGIN TRANSACTION");
        engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('three')");
        OtherUserCounts(other, copy.Path, tdef, lastAutoNumber: 10, rowsChange: 0);
        engine.ExecuteNonQuery("ROLLBACK");

        AssertNothingLocked(other, copy.Path);
        AssertOnlyAutoNumberMoved(before, ReadRaw(copy.Path, tdef, format), 10, format);
        Assert.Equal(1, engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('four')"));
        Assert.Equal([1, 2, 11], Ids(engine));
    }

    // A nested transaction rolled back after its INSERT's AutoNumber went out early, and another user drew one meanwhile:
    // the rollback to its savepoint keeps both, and writes nothing over theirs.
    [Fact]
    public void A_nested_ROLLBACK_after_another_user_drew_meanwhile_keeps_their_AutoNumber()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-alloclock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        (int tdef, _) = CreateTwoRows(engine, database);
        JetFormatBase format = FormatOf(copy.Path);
        byte[] before = ReadRaw(copy.Path, tdef, format);
        using JetLockFile other = OtherUserOf(copy.Path);

        engine.ExecuteNonQuery("BEGIN TRANSACTION");
        engine.ExecuteNonQuery("BEGIN TRANSACTION");
        engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('three')");
        OtherUserCounts(other, copy.Path, tdef, lastAutoNumber: 10, rowsChange: 0);
        engine.ExecuteNonQuery("ROLLBACK");
        engine.ExecuteNonQuery("COMMIT");

        AssertNothingLocked(other, copy.Path);
        AssertOnlyAutoNumberMoved(before, ReadRaw(copy.Path, tdef, format), 10, format);
        Assert.Equal(1, engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('four')"));
        Assert.Equal([1, 2, 11], Ids(engine));
        LibRed.Core.Tests.TestDatabases.AssertPageOwnership(database);
    }

    // A failed statement after the early write and another user's draw, as ACE (measured on this table: another ACE
    // user's refused INSERT drew 4, the failed INSERT then 5, and the COMMIT left 5 and counted its one row onto the
    // file's): the failed INSERT locks the definition page again, draws after their AutoNumber and writes its own early;
    // the COMMIT keeps it and counts the transaction's row onto the file's count.
    [Fact]
    public void A_failed_INSERT_after_another_user_drew_meanwhile_draws_after_them_and_COMMIT_keeps_it()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-alloclock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        (int tdef, _) = CreateTwoRows(engine, database);
        using JetLockFile other = OtherUserOf(copy.Path);

        engine.ExecuteNonQuery("BEGIN TRANSACTION");
        engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('three')");
        // Their statement also took a row from the count (as their DELETE would, measured beside ACE's transaction).
        OtherUserCounts(other, copy.Path, tdef, lastAutoNumber: 10, rowsChange: -1);
        Assert.ThrowsAny<Exception>(() => engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('abcdefghijklmnopqrstuvwxyz')"));
        Assert.Equal(11, LastAutoNumber(copy.Path, tdef));
        Assert.Equal(1, RowCount(copy.Path, tdef));
        Assert.True(ExclusiveIsFree(other, tdef), "The definition page is still locked.");
        engine.ExecuteNonQuery("COMMIT");

        AssertNothingLocked(other, copy.Path);
        Assert.Equal(11, LastAutoNumber(copy.Path, tdef));
        Assert.Equal(2, RowCount(copy.Path, tdef));
        Assert.Equal([1, 2, 3], Ids(engine));
        Assert.Equal(1, engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('four')"));
        Assert.Equal(12, Ids(engine)[^1]);
        LibRed.Core.Tests.TestDatabases.AssertPageOwnership(database);
    }

    // A failed INSERT in a transaction, after one that succeeded, as ACE's (measured on this table and data: a value too
    // long for its column): the AutoNumber it drew is written to the definition page ahead of COMMIT, with nothing else,
    // and the page let go; the COMMIT counts the one row that stands.
    [Fact]
    public void A_failed_INSERT_in_a_transaction_writes_the_AutoNumber_it_drew_and_lets_the_definition_page_go()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-alloclock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        (int tdef, int data) = CreateTwoRows(engine, database);
        JetFormatBase format = FormatOf(copy.Path);
        byte[] before = ReadRaw(copy.Path, tdef, format);
        using JetLockFile other = OtherUserOf(copy.Path);

        engine.ExecuteNonQuery("BEGIN TRANSACTION");
        engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('three')");
        Assert.ThrowsAny<Exception>(() => engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('abcdefghijklmnopqrstuvwxyz')"));
        AssertOnlyAutoNumberMoved(before, ReadRaw(copy.Path, tdef, format), 4, format);
        Assert.True(ExclusiveIsFree(other, tdef), "The definition page is still locked.");
        Assert.False(ExclusiveIsFree(other, data), "The row's data page was let go before COMMIT.");
        engine.ExecuteNonQuery("COMMIT");
        AssertNothingLocked(other, copy.Path);
        Assert.Equal([1, 2, 3], Ids(engine));
        Assert.Equal(4, LastAutoNumber(copy.Path, tdef));
        Assert.Equal(3, RowCount(copy.Path, tdef));
        Assert.Equal(1, engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('five')"));
        Assert.Equal(5, Ids(engine)[^1]);
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

    // An INSERT inside a transaction that takes a new run of pages: as ACE (measured, locking.md §5), page 1 is written
    // with the run taken — bits cleared, nothing else — and one more on the slot, and let go, before COMMIT; the row's
    // data page stays held to COMMIT. LibRed writes it as the statement returns, where ACE writes it about 500 ms after.
    [Fact]
    public void In_a_transaction_an_INSERT_that_takes_a_run_writes_page_1_and_lets_it_go_before_COMMIT()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-alloclock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        (int tdef, int data) = CreateTwoRowsWithMemo(engine, database);
        JetFormatBase format = FormatOf(copy.Path);
        int holder = MapHolder(copy.Path, format);
        byte[] before = ReadRaw(copy.Path, holder, format);
        int slot = Slot(copy.Path, LibRedUser);

        engine.ExecuteNonQuery("BEGIN TRANSACTION");
        engine.ExecuteNonQuery("INSERT INTO T (A, M) VALUES ('three', @m)", LongMemo);

        byte[] early = ReadRaw(copy.Path, holder, format);
        Assert.False(early.AsSpan().SequenceEqual(before), "Page 1 was not written before COMMIT.");
        for (int i = 0; i < early.Length; i++)
            Assert.True((early[i] & ~before[i]) == 0, $"Page 1 byte 0x{i:X3} gained a bit: {before[i]:X2} -> {early[i]:X2}.");
        Assert.Equal(slot + 1, Slot(copy.Path, LibRedUser));
        using JetLockFile other = OtherUserOf(copy.Path);
        Assert.True(ExclusiveIsFree(other, holder), "Page 1 is still locked.");
        Assert.False(ExclusiveIsFree(other, data), "The row's data page was let go before COMMIT.");
        // Written with the AutoNumber drawn and let go too, in the same batch, as ACE's (locking.md §5).
        Assert.True(ExclusiveIsFree(other, tdef), "The definition page is still locked.");

        engine.ExecuteNonQuery("COMMIT");
        AssertNothingLocked(other, copy.Path);
        Assert.Equal([1, 2, 3], Ids(engine));
        Assert.Equal(MemoLength, MemoLengthOf(engine, 3));
        LibRed.Core.Tests.TestDatabases.AssertPageOwnership(database);
    }

    // Page 1 let go after the INSERT, another user reserves a run of its own and writes its pages, while LibRed's
    // transaction is open: LibRed's next INSERT in it reads the maps again under the lock, takes none of those pages,
    // and its COMMIT writes over none of them nor gives back what the other user took.
    [Fact]
    public void Another_users_run_reserved_while_the_transaction_is_open_is_never_handed_out_or_given_back()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-alloclock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        CreateTwoRowsWithMemo(engine, database);
        JetFormatBase format = FormatOf(copy.Path);
        int holder = MapHolder(copy.Path, format);

        engine.ExecuteNonQuery("BEGIN TRANSACTION");
        engine.ExecuteNonQuery("INSERT INTO T (A, M) VALUES ('three', @m)", LongMemo);

        // The other user: page 1 locked, its lowest 16 free pages taken and written, page 1 written, its slot moved.
        using JetLockFile other = OtherUserOf(copy.Path);
        HoldExclusive(other, holder);
        byte[] map = ReadRaw(copy.Path, holder, format);
        (int start, int offset, int length) = FreeRecord(map, copy.Path, format);
        Span<byte> bits = Storage.UsageMap.InlineBits(map.AsSpan(offset, length), format);
        var theirs = new List<int>();
        for (int bit = 0; bit < bits.Length * 8 && theirs.Count < 16; bit++)
            if (Storage.BitmapBits.Get(bits, bit) && start + bit > holder)
            {
                theirs.Add(start + bit);
                Storage.BitmapBits.Set(bits, bit, false);
            }
        Assert.Equal(16, theirs.Count);
        foreach (int page in theirs) WriteRaw(copy.Path, page, Marked(page, format), format);
        WriteRaw(copy.Path, holder, map, format);
        WriteSlot(copy.Path, OtherUser, (ushort)(Slot(copy.Path, OtherUser) + 1));
        ReleaseExclusive(other, holder);

        engine.ExecuteNonQuery("INSERT INTO T (A, M) VALUES ('four', @m)", LongMemo);
        engine.ExecuteNonQuery("COMMIT");

        Assert.Equal([1, 2, 3, 4], Ids(engine));
        Assert.Equal(MemoLength, MemoLengthOf(engine, 4));
        foreach (int page in theirs)
            Assert.True(ReadRaw(copy.Path, page, format).AsSpan().SequenceEqual(Marked(page, format)),
                $"Page {page}, which the other user reserved and wrote, was written over.");
        map = ReadRaw(copy.Path, holder, format);
        (start, offset, length) = FreeRecord(map, copy.Path, format);
        foreach (int page in theirs)
            Assert.False(Storage.BitmapBits.Get(Storage.UsageMap.InlineBits(map.AsSpan(offset, length), format), page - start),
                $"Page {page}, which the other user reserved, is free again.");
        LibRed.Core.Tests.TestDatabases.AssertPageOwnership(database);
    }

    // A ROLLBACK after page 1 went out with the run taken: the pages the undone INSERT took go back to the free map, and
    // the rest of the run at close, so the file's maps end as a session that never ran the transaction leaves them.
    [Fact]
    public void A_ROLLBACK_after_the_run_was_written_early_gives_back_every_page_of_it()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var control = TemporaryDatabase.CopyOf(Northwind, "libred-alloclock-control-");
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-alloclock-");
        using (JetDatabase database = JetDatabase.Open(control.Path, readOnly: false))
            CreateTwoRowsWithMemo(new QueryEngine(database), database);
        int slot;
        using (JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false))
        {
            var engine = new QueryEngine(database);
            CreateTwoRowsWithMemo(engine, database);
            engine.ExecuteNonQuery("BEGIN TRANSACTION");
            engine.ExecuteNonQuery("INSERT INTO T (A, M) VALUES ('three', @m)", LongMemo);
            slot = Slot(copy.Path, LibRedUser);
            engine.ExecuteNonQuery("ROLLBACK");
            Assert.Equal([1, 2], Ids(engine));
        }

        JetFormatBase format = FormatOf(copy.Path);
        int holder = MapHolder(copy.Path, format);
        Assert.Equal(ReadRaw(control.Path, holder, format), ReadRaw(copy.Path, holder, format));
        Assert.Equal(new FileInfo(control.Path).Length, new FileInfo(copy.Path).Length);
        Assert.True(Slot(copy.Path, LibRedUser) > slot);
        using JetDatabase reopened = JetDatabase.Open(copy.Path, readOnly: false);
        LibRed.Core.Tests.TestDatabases.AssertPageOwnership(reopened);
        Assert.Equal(1, new QueryEngine(reopened).ExecuteNonQuery("INSERT INTO T (A, M) VALUES ('four', @m)", LongMemo));
    }

    // A failed INSERT after one whose run went out early: the savepoint's rollback gives back only what the failed
    // statement took, so the file's maps end as a session that never ran the failed statement leaves them.
    [Fact]
    public void A_failed_INSERT_after_a_run_was_written_early_gives_back_only_its_own_pages()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var control = TemporaryDatabase.CopyOf(Northwind, "libred-alloclock-control-");
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-alloclock-");
        foreach ((string path, bool fail) in new[] { (control.Path, false), (copy.Path, true) })
        {
            using JetDatabase database = JetDatabase.Open(path, readOnly: false);
            var engine = new QueryEngine(database);
            CreateTwoRowsWithMemo(engine, database);
            engine.ExecuteNonQuery("BEGIN TRANSACTION");
            engine.ExecuteNonQuery("INSERT INTO T (A, M) VALUES ('three', @m)", LongMemo);
            if (fail)
                Assert.ThrowsAny<Exception>(() => engine.ExecuteNonQuery("INSERT INTO T (Id, A, M) VALUES (1, 'duplicate', @m)", LongMemo));
            engine.ExecuteNonQuery("COMMIT");
            Assert.Equal([1, 2, 3], Ids(engine));
        }

        JetFormatBase format = FormatOf(copy.Path);
        int holder = MapHolder(copy.Path, format);
        Assert.Equal(ReadRaw(control.Path, holder, format), ReadRaw(copy.Path, holder, format));
        using JetDatabase reopened = JetDatabase.Open(copy.Path, readOnly: false);
        LibRed.Core.Tests.TestDatabases.AssertPageOwnership(reopened);
    }

    // A nested transaction rolled back after its INSERT's run went out early: the rollback to its savepoint gives back
    // the pages the INSERT took, and the outer transaction's COMMIT and the close the rest, so the file's maps end as a
    // session that never ran the nested transaction leaves them.
    [Fact]
    public void A_nested_ROLLBACK_after_the_run_was_written_early_gives_back_every_page_of_it()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var control = TemporaryDatabase.CopyOf(Northwind, "libred-alloclock-control-");
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-alloclock-");
        foreach ((string path, bool nested) in new[] { (control.Path, false), (copy.Path, true) })
        {
            using JetDatabase database = JetDatabase.Open(path, readOnly: false);
            var engine = new QueryEngine(database);
            CreateTwoRowsWithMemo(engine, database);
            engine.ExecuteNonQuery("BEGIN TRANSACTION");
            engine.ExecuteNonQuery("UPDATE T SET A = 'one!' WHERE Id = 1");
            if (nested)
            {
                engine.ExecuteNonQuery("BEGIN TRANSACTION");
                engine.ExecuteNonQuery("INSERT INTO T (A, M) VALUES ('three', @m)", LongMemo);
                engine.ExecuteNonQuery("ROLLBACK");
            }
            engine.ExecuteNonQuery("COMMIT");
            Assert.Equal([1, 2], Ids(engine));
        }

        JetFormatBase format = FormatOf(copy.Path);
        int holder = MapHolder(copy.Path, format);
        Assert.Equal(ReadRaw(control.Path, holder, format), ReadRaw(copy.Path, holder, format));
        Assert.Equal(new FileInfo(control.Path).Length, new FileInfo(copy.Path).Length);
        using JetDatabase reopened = JetDatabase.Open(copy.Path, readOnly: false);
        LibRed.Core.Tests.TestDatabases.AssertPageOwnership(reopened);
    }

    private const int LibRedUser = 1;

    // Long enough to need more pages than one run of 32 holds, so the INSERT reserves a new run inside the transaction.
    private const int MemoLength = 160_000;

    private static Dictionary<string, object?> LongMemo => new() { ["@m"] = new string('m', MemoLength) };

    private static int MemoLengthOf(QueryEngine engine, int id) =>
        ((string)engine.ExecuteQuery($"SELECT M FROM T WHERE Id = {id}").Rows.Single()[0]!).Length;

    // As CreateTwoRows, with a long-value column.
    private static (int Tdef, int Data) CreateTwoRowsWithMemo(QueryEngine engine, JetDatabase database)
    {
        engine.ExecuteNonQuery("CREATE TABLE T (Id AUTOINCREMENT PRIMARY KEY, A TEXT(20), M MEMO)");
        engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('one')");
        engine.ExecuteNonQuery("INSERT INTO T (A) VALUES ('two')");
        return database.ReadConsistent(() =>
        {
            var table = database.OpenTable("T");
            return (table.Definition.DefinitionPage, table.Rows().WithIds().Select(r => r.Id.Page).Distinct().Single());
        });
    }

    // The page holding the global free-pages map (page 1, as ACE creates it).
    private static int MapHolder(string path, JetFormatBase format) =>
        Pages.DatabaseDefinitionPage.ReadMapPointer(ReadRaw(path, 0, format), format.FreePagesMapPointerOffset, format).Page;

    // The global free-pages map's record on its holder page: its first page, and where it lies on the page.
    private static (int Start, int Offset, int Length) FreeRecord(byte[] holder, string path, JetFormatBase format)
    {
        (int row, int page) = Pages.DatabaseDefinitionPage.ReadMapPointer(ReadRaw(path, 0, format), format.FreePagesMapPointerOffset, format);
        var parsed = new Pages.DataPage();
        parsed.Read(new PageBuffer(holder, page), format);
        Pages.DataPage.RowSlot slot = parsed.Rows[row];
        Assert.Equal(UsageMapType.Inline, Storage.UsageMap.RecordType(holder.AsSpan(slot.Offset, slot.Length)));
        return (Storage.UsageMap.StartPage(holder.AsSpan(slot.Offset, slot.Length), format), slot.Offset, slot.Length);
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

    private static int RowCount(string path, int tdef)
    {
        JetFormatBase format = FormatOf(path);
        return BinaryPrimitives.ReadInt32LittleEndian(ReadRaw(path, tdef, format).AsSpan(format.TdefRowCountOffset));
    }

    // The other user's statement on the table, as ACE writes one: the definition page locked, its AutoNumber counter set
    // and its row count moved, its slot moved, the page let go.
    private static void OtherUserCounts(JetLockFile other, string path, int tdef, int lastAutoNumber, int rowsChange)
    {
        HoldExclusive(other, tdef);
        JetFormatBase format = FormatOf(path);
        byte[] page = ReadRaw(path, tdef, format);
        BinaryPrimitives.WriteInt32LittleEndian(page.AsSpan(format.TdefLastAutoNumberOffset), lastAutoNumber);
        BinaryPrimitives.WriteInt32LittleEndian(page.AsSpan(format.TdefRowCountOffset),
            BinaryPrimitives.ReadInt32LittleEndian(page.AsSpan(format.TdefRowCountOffset)) + rowsChange);
        WriteRaw(path, tdef, page, format);
        WriteSlot(path, OtherUser, (ushort)(Slot(path, OtherUser) + 1));
        ReleaseExclusive(other, tdef);
    }

    // The definition page as the file has it is the page as it was but for the AutoNumber counter, now at
    // lastAutoNumber: no row count, no index count, nothing else.
    private static void AssertOnlyAutoNumberMoved(byte[] before, byte[] after, int lastAutoNumber, JetFormatBase format)
    {
        Assert.Equal(lastAutoNumber, BinaryPrimitives.ReadInt32LittleEndian(after.AsSpan(format.TdefLastAutoNumberOffset)));
        byte[] expected = (byte[])before.Clone();
        BinaryPrimitives.WriteInt32LittleEndian(expected.AsSpan(format.TdefLastAutoNumberOffset), lastAutoNumber);
        Assert.Equal(expected, after);
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
