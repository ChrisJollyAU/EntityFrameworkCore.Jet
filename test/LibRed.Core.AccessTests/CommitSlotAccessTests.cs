using System.Buffers.Binary;
using System.Data.OleDb;
using System.Diagnostics;
using LibRed.Catalog;
using Xunit;

namespace LibRed.Core.Tests;

// LibRed and ACE in one file at once, through the page-0 commit slots (docs/format/locking.md §6): each ends its batches
// with its own slot, and LibRed's next statement after ACE's slot moves reads what ACE wrote. The writes strictly take
// turns — LibRed takes no page locks yet, so two writers at once are not safe. One ACE connection per test: ACE is
// unstable under connection churn.
[Collection(AceCollection.Name)]
public class CommitSlotAccessTests
{
    private static readonly Formats.JetFormatBase Format = TestDatabases.FormatOf(TestDatabases.NorthwindAccdb);

    // ACE refreshes another user's pages from the file on its own interval (PageTimeout, 5 s by default).
    private static readonly TimeSpan AceSees = TimeSpan.FromSeconds(7);
    // ACE writes an autocommit statement's pages, and the slot that ends them, about 500 ms after it returns.
    private static readonly TimeSpan AceWrites = TimeSpan.FromSeconds(3);

    [Fact]
    public void LibRed_and_Ace_take_turns_writing_and_each_reads_the_others_commits()
    {
        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "libred-commitslot-ace-");
        try
        {
            int libRedBatches = 0, aceInserts = 0;
            int libRedBefore, aceBefore;

            using (JetDatabase database = JetDatabase.Open(path, readOnly: false))
            {
                Assert.Equal(1, database.Channel.UserNumber);
                database.WriteExclusive(() =>
                {
                    database.CreateTable("LA", [
                        new ColumnSpec("Id", JetDataType.Int32, 4, IsFixedLength: true),
                        new ColumnSpec("Who", JetDataType.Text, 20, IsFixedLength: false),
                    ], primaryKey: ["Id"]);
                    return 0;
                });
                libRedBefore = Slot(path, 1);
                aceBefore = Slot(path, 2);

                using (OleDbConnection ace = AceTestDatabase.Open(path))
                {
                    for (int round = 0; round < 3; round++)
                    {
                        int libRedId = 10 * round + 1, aceId = 10 * round + 2;

                        Insert(database, libRedId, "libred");
                        libRedBatches++;
                        Assert.True(Poll(AceSees, () => Count(ace, $"SELECT COUNT(*) FROM LA WHERE Id = {libRedId} AND Who = 'libred'") == 1),
                            $"ACE did not read LibRed's row {libRedId} within {AceSees.TotalSeconds} s.");

                        int aceSlot = Slot(path, 2);
                        Exec(ace, $"INSERT INTO LA (Id, Who) VALUES ({aceId}, 'ace')");
                        aceInserts++;
                        Assert.True(Poll(AceWrites, () => Slot(path, 2) != aceSlot),
                            $"ACE's slot did not move within {AceWrites.TotalSeconds} s of its insert.");

                        // The very next statement, no retry.
                        Assert.Equal("ace", database.ReadConsistent(() => WhoOf(database, aceId)));
                    }
                }

                Assert.Equal(libRedBefore + libRedBatches, Slot(path, 1));
                Assert.Equal(6, database.ReadConsistent(() => database.OpenTable("LA").Rows().Count()));
            }

            byte[] table = Table(path);
            int libRedAfter = Counter(table, 1), aceAfter = Counter(table, 2);
            Assert.NotEqual(0, libRedAfter);
            // And one more as LibRed closes: it hands back the pages its inserts had reserved (page-05 §9.1), a batch
            // like any other.
            Assert.Equal(libRedBefore + libRedBatches + 1, libRedAfter);
            // ACE's own count: one per insert, each landed before LibRed read it, and none at close — it had nothing
            // left to write (measured: 256 -> 259 for three inserts).
            Assert.Equal(aceBefore + aceInserts, aceAfter);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    [Fact]
    public void Ace_opens_and_writes_a_file_LibRed_wrote_its_slot_in()
    {
        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "libred-commitslot-ace-");
        try
        {
            int before = Slot(path, 1);
            using (JetDatabase database = JetDatabase.Open(path, readOnly: false))
            {
                Assert.Equal(1, database.Channel.UserNumber);
                database.WriteExclusive(() =>
                {
                    database.CreateTable("LA", [
                        new ColumnSpec("Id", JetDataType.Int32, 4, IsFixedLength: true),
                        new ColumnSpec("Who", JetDataType.Text, 20, IsFixedLength: false),
                    ], primaryKey: ["Id"]);
                    return 0;
                });
                for (int id = 1; id <= 5; id++) Insert(database, id, "libred");
            }

            int after = Slot(path, 1);
            Assert.NotEqual(0, after);
            Assert.True(after > before, $"LibRed's slot did not move: {before} -> {after}.");

            using OleDbConnection ace = AceTestDatabase.Open(path);
            Assert.Equal(5, Count(ace, "SELECT COUNT(*) FROM LA WHERE Who = 'libred'"));
            Exec(ace, "INSERT INTO LA (Id, Who) VALUES (6, 'ace')");
            Assert.Equal(6, Count(ace, "SELECT COUNT(*) FROM LA"));
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // A statement, as the engine runs one: the exclusive scope, which is where another user's commits are noticed.
    private static void Insert(JetDatabase database, int id, string who) =>
        database.WriteExclusive(() =>
        {
            database.OpenTable("LA").Insert([id, who]);
            return 0;
        });

    private static string? WhoOf(JetDatabase database, int id) =>
        (string?)database.OpenTable("LA").Rows().SingleOrDefault(r => (int)r[0]! == id)?[1];

    private static bool Poll(TimeSpan limit, Func<bool> done)
    {
        var clock = Stopwatch.StartNew();
        while (true)
        {
            if (done()) return true;
            if (clock.Elapsed > limit) return false;
            Thread.Sleep(100);
        }
    }

    private static int Count(OleDbConnection connection, string sql)
    {
        using OleDbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void Exec(OleDbConnection connection, string sql)
    {
        using OleDbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static int Counter(byte[] table, int user) =>
        BinaryPrimitives.ReadUInt16LittleEndian(table.AsSpan(user * Format.CommitByteSlotSize));

    private static int Slot(string path, int user) => Counter(Table(path), user);

    private static byte[] Table(string path)
    {
        byte[] table = new byte[Format.CommitByteTableUsers * Format.CommitByteSlotSize];
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        stream.Seek(Format.CommitByteTableOffset, SeekOrigin.Begin);
        stream.ReadExactly(table);
        return table;
    }
}
