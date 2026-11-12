using System.Buffers.Binary;
using System.Text;
using LibRed.Catalog;
using LibRed.Formats;
using LibRed.IO;
using Xunit;

namespace LibRed.Core.Tests;

// LibRed writes its own page-0 commit slot around every batch of page writes, as ACE does (docs/format/locking.md §6):
// one existing page — the page, then the counter plus one; two or more — 00 00, the pages, the counter plus one; pages
// appended to the file before any of that. And it watches the other users' slots to notice their commits. "Another
// user" here is a raw handle on the file, writing what another process would.
public class CommitSlotProtocolTests
{
    private const string WindowsOnly = "Only a registered user writes a shared slot, and registration is Windows-only for now.";

    private static readonly JetFormatBase Format = TestDatabases.FormatOf(TestDatabases.NorthwindAccdb);
    private static readonly int TableBytes = Format.CommitByteTableUsers * Format.CommitByteSlotSize;

    [Fact]
    public void A_batch_of_two_existing_pages_writes_00_00_then_the_pages_then_the_counter_plus_one()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-commitslot-");
        int before = Slot(copy.Path, 1);
        var locks = new SlotRecorder(copy.Path, 1);

        using (var channel = PageChannel.Open(copy.Path, readOnly: false, locks: locks))
        {
            Assert.Equal(1, channel.UserNumber);
            channel.BeginTransaction();
            WriteMarker(channel, 5, 0x51);
            WriteMarker(channel, 6, 0x61);
            channel.CommitTransaction();

            Assert.Equal([5, 6], locks.Pages);
            Assert.Equal([0, 0], locks.Slots);
            Assert.Equal(before + 1, Slot(copy.Path, 1));
            Assert.Equal(before + 1, SlotInCache(channel, 1));
        }
    }

    [Fact]
    public void A_batch_of_one_existing_page_never_shows_00_00()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-commitslot-");
        int before = Slot(copy.Path, 1);
        var locks = new SlotRecorder(copy.Path, 1);

        using (var channel = PageChannel.Open(copy.Path, readOnly: false, locks: locks))
        {
            WriteMarker(channel, 5, 0x51);   // outside a transaction: a batch of its own
            Assert.Equal([before], locks.Slots);
            Assert.Equal(before + 1, Slot(copy.Path, 1));

            channel.BeginTransaction();
            WriteMarker(channel, 6, 0x61);
            channel.CommitTransaction();
            Assert.Equal([before, before + 1], locks.Slots);
            Assert.Equal(before + 2, Slot(copy.Path, 1));
        }
    }

    [Fact]
    public void Appended_pages_are_written_before_the_slot_first_reads_00_00()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-commitslot-");
        int before = Slot(copy.Path, 1);
        var locks = new SlotRecorder(copy.Path, 1);

        using (var channel = PageChannel.Open(copy.Path, readOnly: false, locks: locks))
        {
            channel.BeginTransaction();
            WriteMarker(channel, 5, 0x51);
            WriteMarker(channel, 6, 0x61);
            int appended = channel.Allocator.Append();
            WriteMarker(channel, appended, 0x71);
            channel.CommitTransaction();

            Assert.Equal([appended, 5, 6], locks.Pages);
            Assert.Equal([before, 0, 0], locks.Slots);
            Assert.Equal(before + 1, Slot(copy.Path, 1));
        }
    }

    [Fact]
    public void N_batches_raise_the_counter_by_N_and_touch_no_other_slot_and_a_read_touches_nothing()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-commitslot-");

        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        database.CreateTable("CS", [new ColumnSpec("Id", JetDataType.Int32, 4, IsFixedLength: true)], primaryKey: ["Id"]);
        byte[] before = Table(copy.Path);

        const int batches = 7;
        Storage.Table table = database.OpenTable("CS");
        for (int i = 0; i < batches; i++) table.Insert([i]);
        byte[] afterWrites = Table(copy.Path);

        Assert.Equal(Counter(before, 1) + batches, Counter(afterWrites, 1));
        AssertOtherSlotsEqual(before, afterWrites, 1);

        Assert.Equal(batches, database.ReadConsistent(() => database.OpenTable("CS").Rows().Count()));
        Assert.Equal(afterWrites, Table(copy.Path));
    }

    [Fact]
    public void Another_users_slot_survives_LibReds_write_of_page_0()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-commitslot-");

        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        _ = database.Channel.ReadPage(0);   // page 0 cached, with user 2's slot as it is now
        WriteSlot(copy.Path, 2, 0xCDAB);   // ... and then user 2 commits

        Assert.True(database.Channel.RaiseFormatVersion((byte)JetVersion.Version16_2016));   // a whole page-0 write

        byte[] page0 = ReadShared(copy.Path)[..Format.PageSize];
        Assert.Equal((byte)JetVersion.Version16_2016, JetFormatBase.ReadVersionByte(page0));
        Assert.Equal(0xCDAB, Slot(copy.Path, 2));
        Assert.Equal(0xCDAB, SlotInCache(database.Channel, 2));
    }

    [Fact]
    public void A_change_to_page_0_confined_to_the_slot_table_is_not_a_write_conflict()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-commitslot-");

        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        PageChannel first = database.Channel;
        using PageChannel second = PageChannel.Open(copy.Path, readOnly: false);

        // The transaction derives its page 0 from the committed image ...
        first.BeginTransaction();
        Assert.True(first.RaiseFormatVersion((byte)JetVersion.Version16_2016));

        // ... which then changes in the slot table only: this process's own slot, by another connection's batch,
        // and another user's, behind it on disk.
        WriteMarker(second, 5, 0x51);
        WriteSlot(copy.Path, 2, 0xCDAB);

        first.CommitTransaction();

        byte[] page0 = ReadShared(copy.Path)[..Format.PageSize];
        Assert.Equal((byte)JetVersion.Version16_2016, JetFormatBase.ReadVersionByte(page0));
        Assert.Equal(0xCDAB, Slot(copy.Path, 2));
    }

    [Fact]
    public void Another_users_commit_is_seen_by_the_next_statement_once_its_slot_moves()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-commitslot-");

        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        database.CreateTable("CS", [
            new ColumnSpec("Id", JetDataType.Int32, 4, IsFixedLength: true),
            new ColumnSpec("T", JetDataType.Text, 40, IsFixedLength: false),
        ], primaryKey: ["Id"]);
        database.OpenTable("CS").Insert([1, "ZQXJZQXJZQXJ"]);
        Assert.Equal("ZQXJZQXJZQXJ", ReadT(database));   // the data page is now cached

        // Another user rewrites the row in place — first without moving its slot.
        RewriteInFile(copy.Path, "ZQXJZQXJZQXJ", "YQXJZQXJZQXJ");

        // A limitation, not a goal: with no slot change there is nothing to notice, and the cached image is served.
        Assert.Equal("ZQXJZQXJZQXJ", ReadT(database));

        // Its batch ends, as every batch does, with its slot.
        WriteSlot(copy.Path, 2, (ushort)(Slot(copy.Path, 2) + 1));
        Assert.Equal("YQXJZQXJZQXJ", ReadT(database));
    }

    [Fact]
    public void A_transaction_open_across_another_users_commit_of_its_page_is_refused_at_commit()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-commitslot-");

        using var channel = PageChannel.Open(copy.Path, readOnly: false);
        channel.BeginTransaction();
        WriteMarker(channel, 5, 0x51);

        // Another user writes page 5 and ends its batch.
        using (var other = new FileStream(copy.Path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
        {
            other.Seek(5L * Format.PageSize + 100, SeekOrigin.Begin);
            other.WriteByte(0x77);
        }
        WriteSlot(copy.Path, 2, (ushort)(Slot(copy.Path, 2) + 1));

        var conflict = Assert.Throws<InvalidOperationException>(() => channel.CommitTransaction());
        Assert.Contains("write conflict on page 5", conflict.Message, StringComparison.Ordinal);
        channel.RollbackTransaction();
        Assert.Equal(0x77, channel.ReadPage(5).Span[100]);
    }

    [Fact]
    public void The_counter_after_FF_FF_is_00_01_never_a_reserved_value()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-commitslot-");
        WriteSlot(copy.Path, 1, 0xFFFF);

        using (var channel = PageChannel.Open(copy.Path, readOnly: false))
            WriteMarker(channel, 5, 0x51);

        Assert.Equal(0x0100, Slot(copy.Path, 1));
    }

    [Fact]
    public void A_failed_publication_puts_the_slot_back()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-commitslot-");
        int before = Slot(copy.Path, 1);
        var locks = new SlotRecorder(copy.Path, 1) { FailOnPage = 6 };

        using (var channel = PageChannel.Open(copy.Path, readOnly: false, locks: locks))
        {
            channel.BeginTransaction();
            WriteMarker(channel, 5, 0x51);
            WriteMarker(channel, 6, 0x61);

            Assert.Throws<IOException>(() => channel.CommitTransaction());
            // Page 5 written under 00 00, page 6 refused, page 5 put back — still under 00 00 — and then the slot.
            Assert.Equal([5, 5], locks.Pages);
            Assert.Equal([0, 0], locks.Slots);
            Assert.True(channel.InTransaction);
            Assert.Equal(before, Slot(copy.Path, 1));

            locks.FailOnPage = null;
            channel.CommitTransaction();
            Assert.Equal(before + 1, Slot(copy.Path, 1));
        }
    }

    [Fact]
    public void An_exclusive_holder_writes_slot_0()
    {
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-commitslot-");
        byte[] before = Table(copy.Path);

        using (var channel = PageChannel.Open(copy.Path, readOnly: false, exclusive: true))
        {
            Assert.Null(channel.UserNumber);
            WriteMarker(channel, 5, 0x51);
            channel.BeginTransaction();
            WriteMarker(channel, 5, 0x52);
            WriteMarker(channel, 6, 0x61);
            channel.CommitTransaction();
        }

        byte[] after = Table(copy.Path);
        Assert.Equal(Counter(before, 0) + 2, Counter(after, 0));
        AssertOtherSlotsEqual(before, after, 0);
    }

    [Fact]
    public void An_unregistered_channel_writes_no_slot()
    {
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-commitslot-");
        // A directory where the lock file goes: the registration cannot open it and carries on unregistered, as it
        // does off Windows.
        string lockPath = JetLockFile.PathFor(copy.Path)!;
        Directory.CreateDirectory(lockPath);
        try
        {
            byte[] before = Table(copy.Path);
            using (var channel = PageChannel.Open(copy.Path, readOnly: false))
            {
                Assert.Null(channel.UserNumber);
                WriteMarker(channel, 5, 0x51);
                channel.BeginTransaction();
                WriteMarker(channel, 5, 0x52);
                WriteMarker(channel, 6, 0x61);
                channel.CommitTransaction();
            }

            Assert.Equal(before, Table(copy.Path));
        }
        finally { Directory.Delete(lockPath); }
    }

    private static string? ReadT(JetDatabase database) =>
        database.ReadConsistent(() => (string?)database.OpenTable("CS").Rows().Single()[1]);

    private static void WriteMarker(PageChannel channel, int page, byte marker)
    {
        byte[] bytes = channel.ReadPage(page).Span.ToArray();
        bytes[100] = marker;
        channel.WritePage(page, bytes);
    }

    // Replaces the text where it is stored in the file, in whichever of its two encodings it was written.
    private static void RewriteInFile(string path, string from, string to)
    {
        byte[] file = ReadShared(path);
        foreach (Encoding encoding in new Encoding[] { Encoding.Latin1, Encoding.Unicode })
        {
            int at = file.AsSpan().IndexOf(encoding.GetBytes(from));
            if (at < 0) continue;
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            stream.Seek(at, SeekOrigin.Begin);
            stream.Write(encoding.GetBytes(to));
            return;
        }
        Assert.Fail($"'{from}' is not in the file.");
    }

    private static int SlotInCache(PageChannel channel, int user) =>
        BinaryPrimitives.ReadUInt16LittleEndian(
            channel.ReadPageShared(0).Span[(Format.CommitByteTableOffset + user * Format.CommitByteSlotSize)..]);

    private static int Counter(byte[] table, int user) =>
        BinaryPrimitives.ReadUInt16LittleEndian(table.AsSpan(user * Format.CommitByteSlotSize));

    private static void AssertOtherSlotsEqual(byte[] before, byte[] after, int user)
    {
        int own = user * Format.CommitByteSlotSize, next = own + Format.CommitByteSlotSize;
        Assert.Equal(before[..own], after[..own]);
        Assert.Equal(before[next..], after[next..]);
    }

    private static int Slot(string path, int user) => Counter(Table(path), user);

    private static byte[] Table(string path) =>
        ReadShared(path).AsSpan(Format.CommitByteTableOffset, TableBytes).ToArray();

    private static void WriteSlot(string path, int user, ushort value)
    {
        byte[] bytes = new byte[Format.CommitByteSlotSize];
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
        stream.Seek(Format.CommitByteTableOffset + user * Format.CommitByteSlotSize, SeekOrigin.Begin);
        stream.Write(bytes);
    }

    private static byte[] ReadShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        byte[] bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);
        return bytes;
    }

    // Records, at each page write of a publication, the page and what the file holds in the user's slot at that
    // moment; FailOnPage makes that page's write fail.
    private sealed class SlotRecorder(string path, int user) : ILockManager
    {
        public List<int> Pages { get; } = [];
        public List<int> Slots { get; } = [];
        public int? FailOnPage { get; set; }

        public void EnterShared(int page) { }
        public void ExitShared(int page) { }
        public void EnterExclusive(int page)
        {
            if (page == FailOnPage) throw new IOException("Injected commit publication failure.");
            Pages.Add(page);
            Slots.Add(Slot(path, user));
        }
        public void ExitExclusive(int page) { }
    }
}
