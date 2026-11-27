using LibRed.Catalog;
using LibRed.Formats;
using LibRed.IO;
using LibRed.Pages;
using LibRed.Storage;
using Xunit;

namespace LibRed.Core.Tests;

// ACE holds the pages a session frees — a deleted row's packed long value, a dropped index, a dropped table — until
// the session closes, then returns them and anything in the global released-pages map to the global free map and
// clears the released map, lengthening it to cover the highest page released (docs/format/page-05-usage-maps.md
// §9.1). Two kinds are freed sooner: a chained long value an UPDATE replaces or a DELETE removes is free at once, and
// a rewritten definition's old continuation pages are free when the statement commits. Both maps are found through
// page 0's pointers; Northwind's free map has 310 and 329 inside the file, and its released map is empty.
public class ReleaseAtCloseTests
{
    private static readonly JetFormatBase Format = TestDatabases.FormatOf(TestDatabases.NorthwindAccdb);

    [Fact]
    public void Released_pages_stay_unreusable_until_close_then_become_free()
    {
        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "release-close-");
        try
        {
            using (var db = JetDatabase.Open(path, readOnly: false))
            {
                PageChannel channel = db.OpenTable("MSysObjects").Channel;
                var allocator = channel.Allocator;
                allocator.Release(300);
                allocator.Release(301);

                Assert.False(FreeMapBit(channel,300));
                Assert.False(FreeMapBit(channel,301));
                Assert.Equal(310, allocator.Allocate());   // the free pages, never the released ones
                Assert.Equal(329, allocator.Allocate());
                Assert.Equal(353, allocator.Allocate());
            }

            using (var db = JetDatabase.Open(path))
            {
                PageChannel channel = db.OpenTable("MSysObjects").Channel;
                Assert.True(FreeMapBit(channel,300));
                Assert.True(FreeMapBit(channel,301));
                Assert.False(FreeMapBit(channel,310));
                Assert.All(ReleasedBits(channel), b => Assert.Equal(0, b));
            }
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    [Fact]
    public void A_rolled_back_release_frees_nothing_and_a_savepoint_rollback_keeps_the_earlier_ones()
    {
        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "release-rollback-");
        try
        {
            using (var db = JetDatabase.Open(path, readOnly: false))
            {
                PageChannel channel = db.OpenTable("MSysObjects").Channel;
                var allocator = channel.Allocator;

                channel.BeginTransaction();
                allocator.Release(300);
                channel.RollbackTransaction();

                channel.BeginTransaction();
                allocator.Release(301);
                Savepoint savepoint = channel.CreateSavepoint();
                allocator.Release(302);
                channel.RollbackToSavepoint(savepoint);
                channel.CommitTransaction();

                channel.BeginTransaction();
                allocator.Release(303);
                // left open: the close discards it
            }

            using (var db = JetDatabase.Open(path))
            {
                PageChannel channel = db.OpenTable("MSysObjects").Channel;
                Assert.False(FreeMapBit(channel,300));
                Assert.True(FreeMapBit(channel,301));
                Assert.False(FreeMapBit(channel,302));
                Assert.False(FreeMapBit(channel,303));
            }
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    [Fact]
    public void The_close_lengthens_the_released_map_to_cover_the_highest_page_released()
    {
        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "release-length-");
        try
        {
            int before, highest;
            using (var db = JetDatabase.Open(path, readOnly: false))
            {
                PageChannel channel = db.OpenTable("MSysObjects").Channel;
                var allocator = channel.Allocator;
                before = ReleasedBits(channel).Length;
                for (int i = 0; i < 400; i++) allocator.Allocate();
                highest = channel.PageCount - 1;
                allocator.Release(highest);
            }

            using (var db = JetDatabase.Open(path))
            {
                PageChannel channel = db.OpenTable("MSysObjects").Channel;
                // 5-byte header, then the bitmap to the highest page in whole 4-byte words.
                int growth = Format.UsageMapInlineGrowthSize;
                int expected = (BitmapBits.ByteCount(highest + 1) + growth - 1) / growth * growth;
                Assert.True(expected > before);
                Assert.Equal(expected, ReleasedBits(channel).Length);
                Assert.All(ReleasedBits(channel), b => Assert.Equal(0, b));
                Assert.True(FreeMapBit(channel,highest));
            }
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // A released map whose start page is -2,147,483,648 (its high byte damaged), every bit clear: ACE's close leaves
    // it as it was and merges the pages a session released into the free map exactly as on the undamaged file
    // (measured, releasing a picture's three long-value pages).
    [Fact]
    public void The_close_leaves_a_released_map_starting_below_page_zero_as_it_is_and_frees_the_pages()
    {
        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "release-negative-");
        try
        {
            int holderPage;
            byte[] holder, record;
            using (var channel = PageChannel.Open(path, readOnly: true))
            {
                DataPage.RowSlot slot;
                (_, holderPage, holder, slot) = TestDatabases.GlobalMap(channel, Format.ReleasedPagesMapPointerOffset);
                System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
                    InlineMap(holder, slot)[Format.UsageMapStartPageOffset..], int.MinValue);
                record = InlineMap(holder, slot).ToArray();
            }
            TestDatabases.WritePage(path, holderPage, holder);

            using (var db = JetDatabase.Open(path, readOnly: false))
            {
                var allocator = db.OpenTable("MSysObjects").Channel.Allocator;
                allocator.Release(300);
                allocator.Release(301);
            }

            using (var channel = PageChannel.Open(path, readOnly: true))
            {
                Assert.True(FreeMapBit(channel, 300));
                Assert.True(FreeMapBit(channel, 301));
                (_, _, byte[] after, DataPage.RowSlot slot) = TestDatabases.GlobalMap(channel, Format.ReleasedPagesMapPointerOffset);
                Assert.Equal(record, InlineMap(after, slot).ToArray());
            }
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // A close whose release cannot run — here a released map turned reference form, which needs a bitmap page, and
    // a free map with no free page and a nonzero start page, which LibRed does not grow — does not throw out of
    // Dispose. The release runs in its own transaction, so it writes nothing, and the released page stays released.
    [Fact]
    public void A_close_whose_release_cannot_run_writes_nothing_and_does_not_throw()
    {
        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "release-cannot-");
        try
        {
            int holderPage;
            byte[] holder;
            using (var channel = PageChannel.Open(path, readOnly: true))
            {
                DataPage.RowSlot free, released;
                (_, holderPage, holder, free) = TestDatabases.GlobalMap(channel, Format.FreePagesMapPointerOffset);
                (_, _, _, released) = TestDatabases.GlobalMap(channel, Format.ReleasedPagesMapPointerOffset);
                Span<byte> freeMap = InlineMap(holder, free);
                System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(freeMap[Format.UsageMapStartPageOffset..], 8);
                UsageMap.InlineBits(freeMap, Format).Clear();
                Assert.Equal(Format.UsageMapReferenceRecordSize, released.Length);
                Assert.All(UsageMap.InlineBits(InlineMap(holder, released), Format).ToArray(), b => Assert.Equal(0, b));
                holder[released.Offset] = (byte)UsageMapType.Reference;
            }
            TestDatabases.WritePage(path, holderPage, holder);

            var db = JetDatabase.Open(path, readOnly: false);
            PageAllocator allocator = db.OpenTable("MSysObjects").Channel.Allocator;
            Assert.Throws<NotSupportedException>(() => allocator.Allocate());   // the free map LibRed cannot grow
            allocator.Release(300);
            db.Dispose();

            Assert.Equal(holder, TestDatabases.ReadPage(path, holderPage));
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // ACE's open and close of this very file — page 300 set in its released map, nothing else done — sets 300 free and
    // clears the map, writing page 1 (measured, ACE 16 over OLE DB, a shared open alone). LibRed's read-only close
    // writes nothing.
    [Fact]
    public void Pages_already_in_the_released_map_are_merged_by_a_close_alone_an_idle_one_included()
    {
        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "release-merge-");
        try
        {
            int holderPage;
            byte[] holder;
            using (var channel = PageChannel.Open(path, readOnly: true))
            {
                (_, holderPage, holder, DataPage.RowSlot slot) = TestDatabases.GlobalMap(channel, Format.ReleasedPagesMapPointerOffset);
                Span<byte> record = InlineMap(holder, slot);
                int bit = 300 - UsageMap.StartPage(record, Format);
                Span<byte> bits = UsageMap.InlineBits(record, Format);
                Assert.InRange(bit, 0, bits.Length * 8 - 1);
                BitmapBits.Set(bits, bit, true);
            }
            TestDatabases.WritePage(path, holderPage, holder);

            using (JetDatabase.Open(path)) { }
            Assert.Equal(holder, TestDatabases.ReadPage(path, holderPage));   // a read-only close changes nothing

            using (JetDatabase.Open(path, readOnly: false)) { }

            using (var db = JetDatabase.Open(path))
            {
                PageChannel channel = db.OpenTable("MSysObjects").Channel;
                Assert.True(FreeMapBit(channel,300));
                Assert.All(ReleasedBits(channel), b => Assert.Equal(0, b));
            }
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // ACE beside a second, idle ACE user, deleting a row whose memo is a chained long value (measured, ACE 16): its probe
    // of the other users' group-0 bytes is refused, so it neither locks nor writes page 1, and the chain's pages are in no
    // map at all; its close puts them in the released map and leaves the free map alone; the last user's close — an idle
    // one — moves them into the free map and clears the released map. The other user here is a second JetLockFile handle
    // holding user 1's bytes, so LibRed is user 2, as ACE's deleting user was.
    [Fact]
    public void Beside_another_user_a_deleted_chain_is_held_and_its_close_puts_it_in_the_released_map()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "release-company-");
        try
        {
            int[] chain = CreateChainedRow(path);
            using (JetLockFile other = OtherUser(path))
            {
                using (var db = JetDatabase.Open(path, readOnly: false))
                {
                    Assert.Equal(2, db.Channel.UserNumber);
                    Table table = db.OpenTable("Doc");
                    byte[] page1 = TestDatabases.ReadPage(path, 1);
                    table.Delete(table.Rows().WithIds().Single().Id);
                    Assert.Equal(page1, TestDatabases.ReadPage(path, 1));
                    (int Row, int Page) owned = Assert.Single(table.Definition.LongValueOwnedMaps.Values);
                    Assert.Empty(new UsageMap(table.Channel, table.Definition).PagesInMap(owned.Row, owned.Page).Intersect(chain));
                    Assert.All(chain, page => Assert.False(FreeMapBit(table.Channel, page)));
                    Assert.Empty(MapPages(table.Channel, Format.ReleasedPagesMapPointerOffset).Intersect(chain));
                }

                using (var db = JetDatabase.Open(path))
                {
                    Assert.All(chain, page => Assert.False(FreeMapBit(db.Channel, page)));
                    Assert.Superset(chain.ToHashSet(), MapPages(db.Channel, Format.ReleasedPagesMapPointerOffset));
                }
            }

            using (JetDatabase.Open(path, readOnly: false)) { }
            using (var db = JetDatabase.Open(path))
            {
                Assert.All(chain, page => Assert.True(FreeMapBit(db.Channel, page)));
                Assert.All(ReleasedBits(db.Channel), b => Assert.Equal(0, b));
            }
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // Alone, the same DELETE frees the chain at once and writes page 1 — ACE's probe granted (measured, as above).
    [Fact]
    public void Alone_a_deleted_chain_is_free_at_once_and_page_1_is_written()
    {
        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "release-alone-");
        try
        {
            int[] chain = CreateChainedRow(path);
            using var db = JetDatabase.Open(path, readOnly: false);
            Table table = db.OpenTable("Doc");
            byte[] page1 = TestDatabases.ReadPage(path, 1);
            table.Delete(table.Rows().WithIds().Single().Id);
            Assert.NotEqual(page1, TestDatabases.ReadPage(path, 1));
            Assert.All(chain, page => Assert.True(FreeMapBit(table.Channel, page)));
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // ACE alone, the same DELETE inside a transaction, committed (measured): the COMMIT writes no page 1 — the chain is in
    // neither map — and the close sets it in the free map. The engine runs a statement inside a user's transaction under
    // a savepoint, as here.
    [Fact]
    public void A_chain_deleted_inside_a_users_transaction_is_held_to_close_even_alone()
    {
        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "release-transaction-");
        try
        {
            int[] chain = CreateChainedRow(path);
            using (var db = JetDatabase.Open(path, readOnly: false))
            {
                Table table = db.OpenTable("Doc");
                db.BeginTransaction();
                Savepoint statement = db.CreateSavepoint();
                table.Delete(table.Rows().WithIds().Single().Id);
                db.ReleaseSavepoint(statement);
                db.Commit(flush: false);
                Assert.All(chain, page => Assert.False(FreeMapBit(table.Channel, page)));
            }

            using (var db = JetDatabase.Open(path))
                Assert.All(chain, page => Assert.True(FreeMapBit(db.Channel, page)));
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // A chain freed beside another user in a transaction that rolls back is neither held nor released: the rollback puts
    // the row and its chain back, and the close has nothing to put in the released map (ACE: a rolled-back DELETE of a
    // chained value changes nothing, alone or beside another user — measured).
    [Fact]
    public void Beside_another_user_a_chain_freed_in_a_rolled_back_transaction_stays_the_rows()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "release-company-rollback-");
        try
        {
            int[] chain = CreateChainedRow(path);
            byte[] released;
            using (JetLockFile other = OtherUser(path))
            {
                using (var db = JetDatabase.Open(path, readOnly: false))
                {
                    Table table = db.OpenTable("Doc");
                    released = ReleasedBits(table.Channel);
                    db.BeginTransaction();
                    table.Delete(table.Rows().WithIds().Single().Id);
                    db.Rollback();
                }

                using (var db = JetDatabase.Open(path))
                {
                    Assert.Equal(released, ReleasedBits(db.Channel));
                    Assert.All(chain, page => Assert.False(FreeMapBit(db.Channel, page)));
                    Table table = db.OpenTable("Doc");
                    Assert.Equal(20000, ((string)table.Rows().Single()[1]!).Length);
                    (int Row, int Page) owned = Assert.Single(table.Definition.LongValueOwnedMaps.Values);
                    Assert.Superset(chain.ToHashSet(), new UsageMap(db.Channel, table.Definition).PagesInMap(owned.Row, owned.Page).ToHashSet());
                }
            }
        }
        finally
        {
            using (JetDatabase.Open(path, readOnly: false)) { }   // the last close, which deletes the lock file
            TemporaryDatabase.Delete(path);
        }
    }

    // Two connections of one process are two Jet users, and each finds the other through the probe: the second's DELETE
    // holds the chain, its close puts it in the released map, and the first — the last to close, idle — merges it.
    [Fact]
    public void Two_connections_in_one_process_find_each_other_through_the_probe()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "release-two-");
        try
        {
            int[] chain = CreateChainedRow(path);
            JetDatabase first = JetDatabase.Open(path, readOnly: false);
            try
            {
                using (var second = JetDatabase.Open(path, readOnly: false))
                {
                    Assert.Equal(2, second.Channel.UserNumber);
                    Table table = second.OpenTable("Doc");
                    table.Delete(table.Rows().WithIds().Single().Id);
                    Assert.All(chain, page => Assert.False(FreeMapBit(table.Channel, page)));
                }
                Assert.All(chain, page => Assert.False(FreeMapBit(first.Channel, page)));
                Assert.Superset(chain.ToHashSet(), MapPages(first.Channel, Format.ReleasedPagesMapPointerOffset));
            }
            finally { first.Dispose(); }

            using var db = JetDatabase.Open(path);
            Assert.All(chain, page => Assert.True(FreeMapBit(db.Channel, page)));
            Assert.All(ReleasedBits(db.Channel), b => Assert.Equal(0, b));
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // The pages another user's close parked in the released map are that user's, until the last close: a close beside
    // another user, writing and holding pages of its own, adds its own and leaves theirs (measured: with three pages
    // parked by one ACE user's close, a second ACE user deleting a chained memo and closing beside a third added the
    // chain's two pages and left the three; the third's close moved all five to the free map).
    [Fact]
    public void A_close_beside_another_user_leaves_the_pages_already_in_the_released_map()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "release-parked-");
        try
        {
            int[] chain = CreateChainedRow(path);
            int holderPage;
            byte[] holder;
            using (var channel = PageChannel.Open(path, readOnly: true))
            {
                (_, holderPage, holder, DataPage.RowSlot slot) = TestDatabases.GlobalMap(channel, Format.ReleasedPagesMapPointerOffset);
                Span<byte> record = InlineMap(holder, slot);
                BitmapBits.Set(UsageMap.InlineBits(record, Format), 300 - UsageMap.StartPage(record, Format), true);
            }
            TestDatabases.WritePage(path, holderPage, holder);

            using (JetLockFile other = OtherUser(path))
            {
                using (var db = JetDatabase.Open(path, readOnly: false))
                {
                    Table table = db.OpenTable("Doc");
                    table.Delete(table.Rows().WithIds().Single().Id);
                }

                using var reopened = JetDatabase.Open(path);
                HashSet<int> released = MapPages(reopened.Channel, Format.ReleasedPagesMapPointerOffset);
                Assert.Equal(chain.Append(300).Order(), released.Order());
                Assert.False(FreeMapBit(reopened.Channel, 300));
            }
        }
        finally
        {
            using (JetDatabase.Open(path, readOnly: false)) { }
            TemporaryDatabase.Delete(path);
        }
    }

    [Fact]
    public void Dropping_an_index_holds_its_root_but_a_memo_update_frees_the_old_chain_at_once()
    {
        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "release-paths-");
        try
        {
            using var db = JetDatabase.Open(path, readOnly: false);
            db.CreateTable("Paths",
                [new("Id", JetDataType.Int32, 4, IsFixedLength: true),
                 new("M", JetDataType.Memo, 0, IsFixedLength: false)],
                primaryKey: ["Id"]);
            Table table = db.OpenTable("Paths");
            table.Insert([1, new string('a', 20000)]);
            PageChannel channel = table.Channel;

            int root = db.Catalog.FindTable("Paths")!.Indexes.Single().RootPage;
            db.DropIndex("Paths", db.Catalog.FindTable("Paths")!.Indexes.Single().Name);
            Assert.False(FreeMapBit(channel,root));

            table = db.OpenTable("Paths");
            (RowId id, object?[] values) = table.Rows().WithIds().Single();
            // The old chain is freed after the new value is written, as ACE does, so this update grows the file...
            int pagesBefore = channel.PageCount;
            values[1] = new string('b', 20000);
            table.Update(id, values, new HashSet<int> { 1 });
            Assert.True(channel.PageCount > pagesBefore);

            // ...but it is free at once, not held to close: the next update's chain starts on the old chain's first page,
            // 354, and then continues through the session's shared reservation, growing the file as well — measured
            // through ACE on this sequence: 375 pages after the first update, 384 after the second, the chain on 354
            // and 375-383.
            Assert.Equal(375, channel.PageCount);
            values[1] = new string('c', 20000);
            table.Update(id, values, new HashSet<int> { 1 });
            Assert.Equal(384, channel.PageCount);
            (int Row, int Page) chain = Assert.Single(table.Definition.LongValueOwnedMaps.Values);
            Assert.Contains(354, new UsageMap(channel, table.Definition).PagesInMap(chain.Row, chain.Page));
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    [Fact]
    public void A_deleted_rows_chained_long_value_is_free_at_once_and_a_packed_one_is_held()
    {
        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "release-delete-");
        try
        {
            int packedPage;
            int[] chain;
            using (var db = JetDatabase.Open(path, readOnly: false))
            {
                db.CreateTable("Doc",
                    [new("Id", JetDataType.Int32, 4, IsFixedLength: true),
                     new("M", JetDataType.Memo, 0, IsFixedLength: false)]);
                Table table = db.OpenTable("Doc");
                PageChannel channel = table.Channel;
                table.Insert([1, new string('a', 20000)]);
                (int Row, int Page) owned = Assert.Single(table.Definition.LongValueOwnedMaps.Values);
                chain = [.. new UsageMap(channel, table.Definition).PagesInMap(owned.Row, owned.Page)];
                table.Insert([2, new string('b', 1000)]);
                packedPage = new UsageMap(channel, table.Definition).PagesInMap(owned.Row, owned.Page).Except(chain).Single();

                List<(RowId Id, object?[] Values)> rows = [.. table.Rows().WithIds()];
                table.Delete(rows.Single(r => (int)r.Values[0]! == 1).Id);
                Assert.All(chain, page => Assert.True(FreeMapBit(channel, page)));

                table.Delete(rows.Single(r => (int)r.Values[0]! == 2).Id);
                Assert.False(FreeMapBit(channel, packedPage));
            }

            using (var db = JetDatabase.Open(path))
                Assert.True(FreeMapBit(db.OpenTable("MSysObjects").Channel, packedPage));
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // An UPDATE replacing a chained value with one that does not chain takes the long-value page the next request
    // would get, before writing anything, and frees it unwritten with the old chain; an UPDATE emptying a packed
    // value's page holds it, as a delete does. Measured through ACE: on one connection, after a 5000-character value
    // went to pages 45-47 of a 48-page file, setting it to 'small' freed 45-48 and left the file at 48 pages.
    [Fact]
    public void Replacing_a_chained_value_frees_an_unwritten_page_and_emptying_a_packed_page_holds_it()
    {
        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "release-update-");
        try
        {
            using var db = JetDatabase.Open(path, readOnly: false);
            db.CreateTable("Doc",
                [new("Id", JetDataType.Int32, 4, IsFixedLength: true),
                 new("M", JetDataType.Memo, 0, IsFixedLength: false)]);
            Table table = db.OpenTable("Doc");
            PageChannel channel = table.Channel;
            (int Row, int Page) Owned() => Assert.Single(db.OpenTable("Doc").Definition.LongValueOwnedMaps.Values);

            table.Insert([1, new string('a', 5000)]);
            int[] chain = [.. new UsageMap(channel, table.Definition).PagesInMap(Owned().Row, Owned().Page)];
            int pages = channel.PageCount;
            (RowId id, object?[] values) = table.Rows().WithIds().Single();
            values[1] = "small";
            table.Update(id, values, new HashSet<int> { 1 });
            Assert.Equal(pages, channel.PageCount);                    // the page taken is not written
            Assert.All(chain, page => Assert.True(FreeMapBit(channel, page)));
            Assert.True(FreeMapBit(channel, pages));                   // the next page, taken and freed unwritten

            table.Insert([2, new string('b', 1000)]);
            int packed = new UsageMap(channel, table.Definition).PagesInMap(Owned().Row, Owned().Page).Except(chain).Single();
            (id, values) = table.Rows().WithIds().Single(r => (int)r.Values[0]! == 2);
            values[1] = null;
            table.Update(id, values, new HashSet<int> { 1 });
            Assert.False(FreeMapBit(channel, packed));
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    [Fact]
    public void A_rewritten_definitions_old_continuation_pages_are_free_when_the_statement_commits()
    {
        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "release-rewrite-");
        try
        {
            using var db = JetDatabase.Open(path, readOnly: false);
            var specs = new List<ColumnSpec> { new("Id", JetDataType.Int32, 4, IsFixedLength: true) };
            for (int i = 1; i <= 252; i++) specs.Add(new($"C{i:D3}", JetDataType.Text, 40, IsFixedLength: false));
            db.CreateTable("Wide", specs);
            PageChannel channel = db.OpenTable("Wide").Channel;
            int[] Continuations() =>
                [.. TableDefinition.ReadChain(channel, db.Catalog.FindTable("Wide")!.DefinitionPage).ContinuationPages];

            // Rolled back, the rewrite frees nothing.
            int[] original = Continuations();
            Assert.NotEmpty(original);
            db.BeginTransaction();
            Assert.True(db.DropColumn("Wide", "C005"));
            db.Rollback();
            Assert.Equal(original, Continuations());
            Assert.All(original, page => Assert.False(FreeMapBit(channel, page)));

            // Committed, the old continuations are held through the statement and free once it commits.
            db.BeginTransaction();
            Assert.True(db.DropColumn("Wide", "C005"));
            Assert.All(original, page => Assert.False(FreeMapBit(channel, page)));
            Assert.Empty(Continuations().Intersect(original));
            db.Commit(flush: false);
            Assert.All(original, page => Assert.True(FreeMapBit(channel, page)));
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // ---------------------------------------------------------------- helpers

    private const string WindowsOnly = "Lock-file registration is Windows-only for now.";

    /// <summary>Creates table Doc with one row whose 20,000-character memo is a chained long value, on a connection
    /// alone that is then closed; returns the chain's pages.</summary>
    private static int[] CreateChainedRow(string path)
    {
        using var db = JetDatabase.Open(path, readOnly: false);
        db.CreateTable("Doc",
            [new("Id", JetDataType.Int32, 4, IsFixedLength: true),
             new("M", JetDataType.Memo, 0, IsFixedLength: false)]);
        Table table = db.OpenTable("Doc");
        table.Insert([1, new string('a', 20000)]);
        (int Row, int Page) owned = Assert.Single(table.Definition.LongValueOwnedMaps.Values);
        int[] chain = [.. new UsageMap(table.Channel, table.Definition).PagesInMap(owned.Row, owned.Page)];
        Assert.True(chain.Length > 1);
        return chain;
    }

    /// <summary>Another Jet user of the database: a lock-file handle of its own holding user 1's session and user
    /// bytes, as an ACE connection that opened first holds them.</summary>
    private static JetLockFile OtherUser(string path)
    {
        JetLockFile other = JetLockFile.Open(JetLockFile.PathFor(path)!, createIfMissing: true);
        Assert.True(other.TryLock(JetLockLayout.SessionByte(1), 1));
        Assert.True(other.TryLock(JetLockLayout.UserByte(1), 1));
        return other;
    }

    /// <summary>The pages set in the global map whose page-0 pointer is at <paramref name="pointerOffset"/>.</summary>
    private static HashSet<int> MapPages(PageChannel channel, int pointerOffset)
    {
        (_, _, byte[] holder, DataPage.RowSlot slot) = TestDatabases.GlobalMap(channel, pointerOffset);
        return [.. UsageMap.PagesInRecord(channel, holder.AsSpan(slot.Offset, slot.Length), null, "A global map")];
    }

    /// <summary>A global map's record on its holder page, which these tests expect in inline form.</summary>
    private static Span<byte> InlineMap(byte[] holder, DataPage.RowSlot slot)
    {
        Span<byte> record = holder.AsSpan(slot.Offset, slot.Length);
        Assert.Equal(UsageMapType.Inline, UsageMap.RecordType(record));
        return record;
    }

    private static bool FreeMapBit(PageChannel channel, int page)
    {
        (_, _, byte[] holder, DataPage.RowSlot slot) = TestDatabases.GlobalMap(channel, Format.FreePagesMapPointerOffset);
        Span<byte> record = InlineMap(holder, slot);
        int bit = page - UsageMap.StartPage(record, Format);
        Span<byte> bits = UsageMap.InlineBits(record, Format);
        if (bit < 0 || bit / 8 >= bits.Length) return false;
        return BitmapBits.Get(bits, bit);
    }

    private static byte[] ReleasedBits(PageChannel channel)
    {
        (_, _, byte[] holder, DataPage.RowSlot slot) = TestDatabases.GlobalMap(channel, Format.ReleasedPagesMapPointerOffset);
        return UsageMap.InlineBits(InlineMap(holder, slot), Format).ToArray();
    }
}