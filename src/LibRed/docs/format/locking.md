# Locking — the lock file and the locking protocol

> Part of the [LibRed Jet / ACE file-format reference](README.md). This is a cross-cutting topic: it describes the **side file** ACE keeps beside a database and the byte-range locks taken on it, not a page type. The page-0 commit-slot table it works with is [page-00 §2.2](page-00-database.md#22-the-user-commit-byte-table-0xe00-0xfff).

Everything here is **measured** against ACE 16 (`Microsoft.ACE.OLEDB.16.0`, x64) over OLE DB, with a shared user, a second shared user, and exclusive and read-only opens. What was not measured is listed under [Not known](#not-known) and in `verification-backlog.md`. The lock layout is implemented as `JetLockLayout` (`LibRed.Core/IO`) and decoded by `tools/JetLockTrace`; both state the layout in code and are kept consistent with this file.

**What LibRed does.** On Windows a shared or read-only open registers as a user as §1 and §3 describe — the lock file, the user's group-0 and group-1 bytes, its record — once per process per file, and the last close deregisters and deletes the file when alone (the page-1 lock ACE takes at close aside). A writer writes **its own commit slot** around every batch of page writes in the order and with the values of §6 — slot `n` as shared user `n`, slot 0 holding the file exclusively, none when it is neither (off Windows, or when the lock file cannot be opened) — and a page-0 write never carries the slot table. A registered user reads the commit table at the start of each statement and of each commit, and when another user's slot has changed since it last looked, drops every cached page and re-reads the catalog (§6). It takes **ACE's page write lock** on every existing page it writes, honours another user's, and runs a statement again when a page it got only after another user committed is not what it had read (§4). It takes **ACE's table locks** (regions 4–6) for every table a statement reads or writes and for every design change, honours another user's, and plans a statement again when it got a table only after another user committed (§4). An `INSERT` locks the table's TDEF page before anything else and reads its counters under the lock, and every change to the global maps locks page 1 before reading them (§4). Inside an explicit transaction it writes the pages a statement took out of the global free map and the AutoNumbers it drew, and lets page 1 and the table's TDEF page go, when the statement returns, as ACE does about 500 ms after it; its `COMMIT` locks the TDEF page again and adds its row and index counts to the page as the file has it then (§5). It takes **no long-value read lock**, so it is not a safe concurrent writer beside ACE for every statement: what is and is not covered is in §4 ([transactions design](../design/transactions.md)).

## 1. The lock file

A shared or read-only user locks a side file next to the database: **`<name>.laccdb`** for an `.accdb`. (Other extensions — the `.ldb` of an `.mdb` — are not measured.) **Every byte-range lock is on the lock file; none is on the database file.**

| | |
| --- | --- |
| Database file | opened read/write, share mode Read\|Write |
| Lock file | created if missing, read/write, share mode Read\|Write |
| Content | one **64-byte record per user**, at offset `(userNumber − 1) × 64`: computer name then security name, 32 bytes each. A user writes its record right after taking its user locks, in one 64-byte write, over whatever a dead user left there; records of other numbers are untouched |

**The record.** Each name is single-byte text **ended by a `00` and padded with spaces (`20`)** to 32 bytes. The computer name is the machine's name (on the measuring machine, `DESKTOP-D1RBTPV`, which is both its NetBIOS name and its host name, so which of the two ACE writes is not known); the security name is **`Admin`** for a database opened without a workgroup file. A read-only user writes the same record. User 1 on machine `DESKTOP-D1RBTPV`:

```
00  44 45 53 4B 54 4F 50 2D 44 31 52 42 54 50 56 00   DESKTOP-D1RBTPV.
10  20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20
20  41 64 6D 69 6E 00 20 20 20 20 20 20 20 20 20 20   Admin.
30  20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20
```

These names are what an exclusive open refused by a connected user reports (§7). How a name longer than 31 bytes, or with characters outside ASCII, is written is not known.

An **exclusive** open (`Mode=Share Exclusive`) is different: the holder opens the database file with share mode None, **creates no lock file, takes no byte-range lock**, and writes commit slot 0 (`0xE00`).

### Lifecycle

- The lock file is created by the first shared user.
- A user that closes cleanly and finds no other user **deletes** it.
- A user killed without closing loses its locks with the process; the lock file stays. The next user reuses it, and the last clean closer deletes it.

## 2. Lock offsets

Every lock is an operating-system **exclusive** lock, taken **fail-immediately**. No OS-shared lock is ever taken; Jet's "shared" and "exclusive" are different byte ranges, not different lock modes. The locks lie far past the end of the lock file, so they are pure semaphores.

```
offset = (region << 28) | (page << 9) | byteInWindow
```

`(region << 28) | (page << 9)` is the page's **window**; byte 0 of a window is unused. The `<< 9` is not the page size.

| Region | Kind | Target |
| --- | --- | --- |
| `1` | User | not page-based: groups of 256 bytes (§3) |
| `2` | Page write / read | data, index, long-value, usage-map and TDEF pages |
| `4` | Table read | the table's TDEF page |
| `5` | Table write | the table's TDEF page |
| `6` | Table deny-write | the table's TDEF page |

Region `3` is the white paper's Jet 2.x region; ACE 16 takes no lock in it.

**Shapes on a page window** (user number `n`):

- A **shared** lock is **one byte** at `window + n`.
- An **exclusive** lock starts at `window + 1` and is **`256 + n` bytes** long (257 for user 1, 258 for user 2): it covers every user's shared byte, and where it ends names the holder.
- An **upgrade** (measured for user 1 only): holding its own byte, the user locks `window + 2`, length 255.

## 3. User registration (region 1)

Region 1 is in 256-byte groups (`0x10000000 + group × 256 + byte`). User `n` holds, for its whole session:

- byte `0x10000000 + n` (group 0), and
- byte `0x10000100 + (n − 1)` (group 1).

**Connecting.** The user first tries `0x10000001`, length 255.

- *Alone*, that succeeds; the user releases it, takes `0x10000100` then `0x10000001`, and writes record 1.
- *With user 1 present*, the newcomer is refused on `0x10000001`/255, `0x10000100`/255, `0x10000100`/1 and `0x10000001`/1, then takes `0x10000101` and `0x10000002`, and writes record 2.

Every user then locks and releases `0x10000500`, length 256 (group 5). Its purpose is unknown.

**User numbers.** Only user numbers 1 and 2 are measured. The next user to connect takes the lowest free number: a dead user's number is reused and its record overwritten.

**Closing.** The user locks page 1 exclusively (region 2); tries to lock every *other* user's group-0 byte (user 1: `0x10000002`, length 254; user 2: `0x10000001`, length 1 and `0x10000003`, length 253); releases; then releases its group-1 and group-0 bytes. Some `CREATE TABLE` / `DROP TABLE` statements make the same probe of the other users' bytes; which ones is not known.

## 4. Page and table locks

| Operation | Locks taken |
| --- | --- |
| Reading data or index pages | **none** — a `SELECT` reads a data page another user holds an exclusive lock on, and is not refused |
| Reading a long-value page | the user's shared byte in region 2, one per LVAL page read |
| Opening a table for reading | shared byte in region 4 on the table's TDEF page (taken, released, taken again at statement start) |
| Opening a table for writing | additionally: exclusive lock in region 6 on the TDEF page, shared byte in region 5, then the region-6 lock released at once |
| Writing a page | exclusive lock in region 2 on that page — data, index, long-value, usage-map/global-map page 1 and TDEF pages alike |
| `ALTER TABLE`, `CREATE INDEX` | first an **exclusive region-4** lock on the table's TDEF page |
| `CREATE TABLE` | exclusive region-4 lock on the new table's TDEF page; `MSysComplexColumns` opened as a table for writing |
| `DROP TABLE` | exclusive region-4 lock on the dropped table's TDEF page |

`CREATE TABLE` and `DROP TABLE` also write-lock the catalog pages they change (the `MSysObjects` and `MSysACEs` TDEF pages, their index pages and data pages).

**Lock, read again, change.** After taking a page's exclusive write lock ACE reads that page from the file again before it changes it — page 1 (the global maps), a table's TDEF page, index pages and catalog data pages alike.

**An `INSERT`'s order.** After the table-open locks (regions 4, 6, 5 above), an `INSERT` takes the table's **TDEF page write lock first**, before any data, index or map page: it locks the TDEF page, reads it again, then locks the other pages it writes. The TDEF page is written on every `INSERT` (its counters). **Page 1** is write-locked only when the `INSERT` reserves a new run of pages — not on every `INSERT`, and more often when two users insert at once than when one inserts alone. Each user appends into a run of its own: two users inserting at once append into separate runs, with unwritten pages between them.

**Two users inserting into one table at once.** Every `INSERT` succeeds, and the AutoNumber values are distinct and contiguous. The only lock ever refused is the table's TDEF page write lock — page 1, index pages and data pages are not — so the TDEF lock serialises inserts into a table. A refused attempt is retried about 110 ms apart (§7). The holder lets the TDEF page go and takes it again within a millisecond for its next statement, so a waiting user can lose several rounds before it is granted. While both insert, the waiting user also tries the other user's group-0 user byte (§3) about twice per `INSERT` — once after letting its page locks go, once just after taking the TDEF lock — and is refused each time (what for is not known).

**A refused `INSERT`** — one refused a page another user holds, once the retries are spent — still writes the table's TDEF page and its own commit slot before it fails, so the AutoNumber it drew is not issued again.

**A rolled-back `INSERT`.** Begin, one `INSERT` into an AutoNumber table, `ROLLBACK`: the file is left as it was but for the table's AutoNumber counter (`0x14` on the TDEF page, the id drawn) and the user's commit slot, one higher — the same whether the `ROLLBACK` follows at once or two seconds after the `INSERT`. The row count, page 1 and the table's other pages are as before. LibRed leaves the same bytes.

**Another user's `CREATE INDEX`.** A user reading a table after another user's `CREATE INDEX` on it is refused with **3159** *"Not a valid bookmark."*: it takes its table-read byte, reads only the table's TDEF page again, and fails; it fails so until its periodic 5-second refresh. That holds between two ACE processes, so it is ACE's own behaviour, not a reaction to anything LibRed writes.

**Row-level locking mode** (`Jet OLEDB:Database Locking Mode=1` on both users) changes none of these shapes for SQL `UPDATE` / `INSERT`: the whole page is locked exclusively and another user's `UPDATE` of a different row on the same page is refused. (Recordset edits: not measured.)

**What LibRed does with page locks.** A registered user (§3) that is not read-only takes the exclusive region-2 lock above on every page it writes that is already in the file — data, index, long-value, usage-map, TDEF and page 0 alike (page 0 unmeasured: no ACE write of it was seen under a shared open) — and none on a page it appends. Inside a transaction the lock is taken when the page is first written, before its committed image is read as the commit's baseline, and held until the commit's batch has ended with its slot write, or until the transaction, or the savepoint frame that first wrote the page, is rolled back. Outside one it is taken and released within the one-page batch. One process is one user: its channels share the lock, which goes when the last of them lets go, and they stay optimistic among themselves (the commit-time baseline check). A refused lock is retried as §7 describes, and then refused with ACE's 3218 text. A write that obtains a lock only after another user's slot has moved since its statement began may have been working on an image of the page older than the file: LibRed drops its cache and reads the page again, as ACE does; when it differs from the image the process held before (or that image cannot be known — below), it rolls the statement back and runs it again (up to three runs, then 3218) — through the SQL engine; a write made through the Core API is refused instead. Measured against ACE 16: each refuses the other's page with 3218, goes ahead once the other lets go, and neither loses the other's change on that page.

**What LibRed does with table locks.** A registered user takes the table locks of the table above on the table's TDEF page:

- every table a statement reads — every table it opens for its rows, not the catalog tables LibRed reads for itself — its shared byte in region 4; every table it writes, that and, under a momentary exclusive lock in region 6, its shared byte in region 5. Held for the statement, released when it returns; inside a transaction, until `COMMIT` or `ROLLBACK`, a rolled-back savepoint frame (a failed statement's included) letting go of those it first took;
- `ALTER TABLE` in every form, `CREATE INDEX`, `DROP INDEX` and `DROP TABLE`: region 4 exclusively, on the table they name (and on a foreign key's referenced table); `CREATE TABLE`: region 4 exclusively on the new table's TDEF page once allocated, and `MSysComplexColumns` opened for writing. Held as the read and write locks are: a lone statement's go when it returns, as ACE's DDL locks do; inside an explicit transaction they are held until `COMMIT` or `ROLLBACK` (ACE's own lifetime there is measured only for `ALTER TABLE … ADD CONSTRAINT … FOREIGN KEY`, whose two tables it keeps another user from deleting in until the transaction ends).

A read-only user takes the read locks only. A lock another user holds is retried as §7 describes, and then refused with ACE's 3211 naming the table — a refused shared byte (another user mid-design-change) and a refused region-5 or region-6 lock included, whose ACE errors are not measured. A table lock taken only after another user's slot has moved since the statement's scope began means the table's design may have changed under the plan: the statement is rolled back, and bound, planned and run again on the re-read catalog (up to three runs, then 3211).

One process is one user: its connections hold one OS lock per window, by count, and never wait for each other. Since Windows refuses an exclusive range overlapping a byte the same handle holds, a region-4 exclusive lock is taken as ACE takes an upgrade — the process's own shared byte, then the other users' bytes (bytes 1–256 of the window but its own; `window + 2`, 255 bytes for user 1, as measured; the shape for a higher user number is that rule carried on) — whether or not the process held the byte before. So a connection can change the design of a table another connection of the process (or itself) has in an open transaction, as before, while another user is still kept out; and to every other user's lock it conflicts as ACE's single range does.

Measured against ACE 16 with LibRed user 1: while ACE holds a table in a transaction (an `UPDATE` in it), LibRed's `ALTER TABLE … ADD COLUMN` and `CREATE INDEX` are refused, and fail with 3211 once LibRed's retries are spent; after ACE commits both succeed and ACE reads the new column on its next statement. LibRed holding a table in a transaction (an `UPDATE` in it) refuses ACE's `ALTER TABLE`, which fails with ACE's own 3211 after about 2.2 s; after LibRed commits it succeeds, and LibRed's next statement reads the new column. ACE reading a table in a loop while LibRed adds columns to it one statement at a time: no statement of either fails, and ACE reads each new column on the statement after it. A table LibRed creates appears to ACE within its 5-second refresh, not on its next statement (ACE gave 3078 meanwhile); one LibRed drops is gone on ACE's next statement (3011, *"could not find the object"*); one ACE creates or drops, on LibRed's next statement. After a LibRed `CREATE INDEX`, ACE refuses reads of that table with 3159 until its 5-second refresh, as it does after another ACE user's (above).

**What LibRed does with counters and allocation.** A registered user that is not read-only, inside a transaction (every SQL statement runs in one):

- **The TDEF page first.** An `INSERT` locks the table's TDEF page before it reads the counters it draws from and advances, and before it touches any other page of the row; so do a `DELETE`, a key-moving `UPDATE` and an emptying `DELETE`, before they read the counts they change. The lock is the page write lock above, taken ahead of the page's first write, and held as that is — to the end of the statement's transaction, or of the savepoint frame that took it — but in an explicit transaction it goes when the statement returns (below).
- **Page 1 before the maps are read.** Every change to the global maps — reserving a new run, giving pages back, the frees of a rollback, the merge of released pages at close, growing a map — locks the maps' holder page (page 1 where ACE creates them) before it reads them, through the allocator's one read of the maps. A page taken from the run the user already holds changes no map and takes no lock, as ACE's does not.
- **The maps written early in an explicit transaction.** A statement that succeeds inside an explicit transaction and has changed the maps only by taking pages — bits cleared, in place — writes those pages of the maps as a batch of its own as it returns, with the slot one higher, and lets them go (§5): the run it reserved is then taken for every other user, who can reserve a run of its own meanwhile. The next change to the maps in the transaction locks page 1 again and reads it as the file has it then. Maps the transaction has freed a page in, grown or converted are held to `COMMIT` as before; so are they after a statement that failed, until the transaction's next statement that succeeds. A rollback, of the transaction or to a savepoint, gives back the pages its undone writes had taken in such a write, in a batch of its own, and keeps the rest of the run reserved until close.
- **The TDEF page written early in an explicit transaction.** As a statement in an explicit transaction returns — one that failed included, once its own writes are undone — every TDEF page the transaction has changed only in its counters (row count, index counts, AutoNumbers) is written with the AutoNumbers it has drawn (`0x14`; and the complex id at `0x1C`, which ACE's early write is not measured with) and nothing else, in the batch with the maps above, and let go (§5). Its row and index counts stay with the transaction. Its next lock of the page — the next `INSERT`, `DELETE` or design change on the table — and its `COMMIT` read the page again under the lock and put the transaction's counting on top of it: each counter it changed as its change added to the file's value, an AutoNumber another user drew meanwhile kept. A rollback, of the transaction or to a savepoint, writes nothing of the AutoNumbers written early. A TDEF page the transaction changed in more than its counters (a design change), and a catalog table's (whose counts every rollback keeps), are held to `COMMIT`.
- **Read again under the lock.** A lock newly taken after another user's slot has moved drops every cached image, and the page is read again from the file. Only when it differs from the image the process held of it before the drop — what the statement may have read — is the statement run again (through the SQL engine; through the Core API it is refused); another user's commit that left the page alone does not stop it. Where that image cannot be known — a page evicted from the cache since the statement began, or a lock taken inside a read-only scope — the statement is run again, as it is for every newly taken table lock after another user's commit. The allocator's own reads made after taking the lock (a rollback's, the close's) are never refused: they read the page again and go on.
- **Appending.** A page this user takes past the end of the file is one the global map gave it, so no other user writes it. The pages below it that only keep the file contiguous are written as zeros only while they lie past the file's end: one another user has since written (into a run of its own) is left as that user wrote it. The file is grown, and a failed publication cut back, against its length as measured at that moment, never below a page another user has appended.

A refused `INSERT` keeps the AutoNumber it drew, as ACE's does: the statement's rollback writes it back onto the TDEF page, in a batch of its own ending with the slot, ahead of the batch with the counting the failure keeps (§6). (What else ACE's `INSERT` refused a locked page writes on that page is not measured; LibRed takes the row count back.)

LibRed does **not**: try the other user's group-0 byte while it waits; take the shared byte on a long-value page read (§4 table); take a table lock for rows read after the statement returned (a query's rows are read lazily, and its locks went with the statement), or for a table read or written through the Core API outside a statement scope and a transaction. Each is listed in `verification-backlog.md`.

## 5. Lifetimes

| Case | When the locks are released |
| --- | --- |
| Autocommit `UPDATE` / `INSERT` | page locks when the pages are written — about **500 ms** after the statement returns, or at once if another statement follows immediately — and after the commit-slot write. Table bytes (regions 4/5) at statement end |
| Long-value read lock | about 500 ms after the statement |
| Explicit transaction | data and index page locks and the table bytes (regions 4/5) are **held until `COMMIT` / `ROLLBACK`**, and it is the data and index page locks that refuse another user's `INSERT` into the table meanwhile. Exception: an `INSERT`'s exclusive locks on the table's TDEF page and on page 1 are released about 500 ms after the `INSERT` — not at `COMMIT` — with those two pages written then (below); the TDEF page is locked again briefly at commit. A `DELETE`'s TDEF page is not held either: another user's `DELETE` beside it writes the page |
| DDL (`ALTER TABLE`, `CREATE INDEX`, `CREATE TABLE`, `DROP TABLE`) | every lock is released inside the statement |
| Idle | no locks are taken or refreshed |

**The early write inside an explicit transaction.** About 500 ms after an `INSERT` into an AutoNumber table inside an explicit transaction, while the transaction is still open, ACE writes one batch:

- the table's TDEF page, carrying **only the AutoNumber counter** (`0x14`, the id drawn) — the row count (`0x10`) and the index entry counts are not written until `COMMIT`;
- page 1, when the statement reserved a run (its bits cleared) or grew the free map's record — and not otherwise;
- its slot, one higher (`00 00` around a two-page batch, as §6).

No data, index, long-value or usage-map page is written early, and no page is appended. INSERTs about 2 s apart are written one batch each; three back to back are written in one batch, about 500 ms after the last, carrying the counter as the last left it. An `INSERT` that fails inside the transaction — a value too long for its column — writes the same about 500 ms after it: the TDEF page with the AutoNumber it drew, the page let go (another user's `INSERT` then draws the next). One that names its AutoNumber value and fails at the primary key writes that value as the counter, as ACE's explicit values set it (LibRed's do not lower it). A transaction that only `UPDATE`s writes nothing early; a `DELETE` writes no TDEF page early, nor holds it; a `DELETE` that reserved a run writes page 1 alone, about 500 ms after it, and the pages it frees are given back at close (page-05 §9.1).

`COMMIT` then writes one batch: the pages appended first, then the TDEF page's row count and index counts, the data, index and usage-map pages, and the slot. It writes the TDEF page as the file has it then: an AutoNumber another user drew meanwhile stays, and the transaction's row count change is **added** to the count the file then has — another user's `DELETE` beside the open transaction took a 400-row table's count to 399, and the `COMMIT` of the transaction's one `INSERT` made it 400, of its one `DELETE` 398. `ROLLBACK` writes nothing: the run stays taken in page 1 until close, when it is given back with the pages the undone writes took, and the file is left as it was but for the AutoNumbers drawn — the transaction's, and another user's meanwhile — and the slot (§4).

**Another user beside the open transaction** (a second ACE process, after the early write): its `INSERT` into the same table is refused with 3218 after about 2.2 s, having drawn the next AutoNumber, which its refused batch writes with the TDEF page and the run it reserved in page 1; its `DELETE` of a row elsewhere in the table is refused with 3218 too where the table has an index; on a table with no index, its `DELETE` of a row on a page the transaction has not written goes ahead, writing the TDEF page's row count, beside a transaction that has inserted a row or deleted one; its `UPDATE` of an unindexed column of a row on a page the transaction has not written goes ahead.

**Another connection in the same process** (two OLE DB connections, each in a transaction, inserting into one AutoNumber table with a unique index, the second straight after the first): the second's `INSERT` draws the id after the first's and is refused with 3218; the first commits; the second, rolled back, draws the next id again.

**What LibRed does.** It writes what a statement took of the global maps and the AutoNumbers it drew, and lets page 1 and the TDEF page go, as the statement returns — sooner than ACE's deferral, as its autocommit write already is; a statement that failed writes its AutoNumbers so, not the maps (§4). Its `COMMIT` and its next lock of the TDEF page put its counting on top of the page as the file has it then, as ACE's `COMMIT` does. A `ROLLBACK` writes the pages its undone writes took back into the free map at once, where ACE leaves them taken until close; the file ends the same after close. Its connections in one process stay optimistic among themselves: a second connection's `INSERT` draws the id after the first's, as in ACE, but goes ahead, and the transaction that commits second is refused at its `COMMIT` with a write conflict on a page both wrote.

**Idle poll.** Each shared user reads the whole 512-byte commit table at `0xE00` every **5 seconds**.

**A deferral held to the next statement.** An autocommit `UPDATE` that ACE starts within milliseconds of another user's commit (no ACE statement between) keeps its page lock, and its write, while the connection is idle — still held 3 s later — and lets go when the connection runs its next statement, a `SELECT` included. With an ACE statement between the other user's commit and the `UPDATE`, or a second's pause, the lock goes about 500 ms after the `UPDATE` as above. Nothing is lost either way: both users then read both changes.

**A kill inside the deferral.** A user killed right after an autocommit `UPDATE` returned (inside the ~500 ms) had written nothing: the update is lost, the file is not left suspect, and later opens succeed.

## 6. Commit slots and page writes

A user writes only **its own** 2-byte slot ([page-00 §2.2](page-00-database.md#22-the-user-commit-byte-table-0xe00-0xfff)): `0xE00 + 2n` when shared, `0xE00` when exclusive. For each batch of page writes:

- **One existing page:** the page, then the slot (counter + 1). `00 00` never appears.
- **Two or more existing pages:** the slot (`00 00`), the pages, the slot (counter + 1). The slot reads `00 00` for exactly as long as the pages are being written — the white paper's "mid-write to disk" state.
- **Pages newly appended to the file** are written first — a 1-byte write at the last byte of the new page, then the full page(s) — before the leading slot write (`00 00`).
- Every batch **ends with a slot write** storing the user's commit counter plus one (little-endian 16-bit); the page locks are released after it. This holds for a lone statement, an explicit transaction's commit and closing the connection (a one-page batch: counter + 1 only).
- The counter rises by exactly one per batch; the other users' slots and slot 0 are untouched.

**A failed autocommit statement** (measured, ACE 16 user 1, on files LibRed built; the slot read at the statement's return, about 2 s later and after close):

| Statement | Batches | What they write |
| --- | --- | --- |
| Single-row `UPDATE` refused at a unique index | none | — |
| Multi-row `UPDATE` refused at a unique index on its second row, no page allocated | one, after the statement returned (nothing at return) | the table's index and data pages, carrying bytes of the undone change (the refused key on the index page) |
| Multi-row `UPDATE` refused because a text value is too long | one, after the statement returned | the table's data page, carrying bytes of the undone row |
| Multi-row `UPDATE` that allocated pages (a 24,000-character memo; 899 index keys lengthened) and then failed — at a unique index, or at a `CHECK` | one after the statement returned, one at close | the first: page 1 (the run it reserved, bits clear), the table's pages, and the pages it appended, which stay in the file; the second: page 1, the run given back |
| `INSERT` into an AutoNumber table refused at a unique index | two: one before the statement returned, one after | the TDEF page: the first with the AutoNumber drawn (`0x14`), the second adding an index count (`0x43` on that table) |
| `INSERT` into an AutoNumber table refused by a `CHECK` | one, before the statement returned | the TDEF page, the AutoNumber drawn (`0x14`) |

A control session (open, `SELECT`, close) writes nothing.

**A `ROLLBACK` of an explicit transaction that reserved a run** (1,290 `INSERT`s; or a 24,000-character memo `UPDATE` and a 28,000-character `INSERT`): one batch inside the transaction, writing page 1 with the run reserved (about 500 ms after the statement that reserved it, §5), none at the `ROLLBACK`, and one at close, page 1 with the run given back. Every page ends as before the session, the slot two higher. LibRed writes page 1 with the run reserved as that statement returns, a batch at the `ROLLBACK` giving back the pages the undone writes took (the rest of the run kept, its bits clear), and the same one as ACE at close: the same bytes at the end.

**The exclusive holder's slot 0** can only be read before and after a session, since nobody else can open the file meanwhile. It is a little-endian counter like the others: from `01 01` (257) on the Northwind fixture, an exclusive open closed at once leaves it at 257, a `SELECT` leaves it, one `INSERT` raises it by one, and `CREATE TABLE` followed by three `INSERT`s raises it by five; slot 1 is untouched throughout. Which writes in the five-batch session are the extra batch, and the slot's value during a batch, are not known.

**Another user's commit.** With LibRed (user 1) and ACE (user 2) in one file and their writes taking turns, each reads the other's rows: ACE within its page refresh (under 7 s), and LibRed on its first statement after ACE's slot moves. ACE's slot rose by exactly its number of `INSERT`s, each landed before LibRed read it, and by nothing at close. A file LibRed has written its slot in opens, reads and writes in ACE.

**What LibRed does with the slot.** A batch is one publication: a transaction's commit, or a page write outside a transaction. Appended pages are written first (the file grown by `SetLength` when the page lies past its end, not by ACE's 1-byte write); then, with two or more existing pages, the slot (`00 00`); the existing pages; and the slot (counter + 1). The counter is the slot as the file holds it, read through the page cache, whose page-0 image every slot write updates. The slot is a 2-byte write at its own offset, handed to the OS at once. Page 0 itself is written only up to `0xE00`, and its cached image takes the table back from the file, so a page-0 write — a format-version raise, a password change — leaves every other user's slot as it is on disk. The commit-time conflict check ignores a difference confined to the table. If a publication fails part-way and every page written is put back, the slot is put back to its value before the batch; if a page cannot be put back, the slot is left as the batch left it (`00 00` after two or more existing pages). A page the batch would leave as committed is not written, and a batch left with none writes nothing, the slot included; a write outside a transaction that changes nothing takes no page lock.

**A failed statement (LibRed)** puts every page back exactly, so it writes only what the rollback must keep, gathered into at most two batches: the AutoNumbers drawn, then the global maps put right (the run reserved kept, its bits clear, the pages the undone writes took from it freed) and the counting a failure keeps. Its slot moves as ACE's does in each case of the table above, but for the two where ACE's one batch carries only bytes of the undone change: LibRed writes nothing there. The batch it writes goes out when the statement fails, not deferred.

**Noticing another user's commit (LibRed).** A registered user reads the 512-byte table at the start of each outermost statement scope and each commit — one positional read on a handle of its own, no allocation — and compares every slot but its own with what it last saw. On a change it drops every cached page image and parse, measures the file's length again and marks the schema changed, so the catalog and the format version are re-read. An open transaction keeps its own pages; its commit then compares them with the file and refuses on a page another user changed. A page another user changes without moving its slot stays cached and is served stale. An exclusive or unregistered channel does not look.

## 7. Conflicts and errors

A refused lock is retried: **21 attempts in all, about 108 ms apart (~2.2 s)**, then the statement fails. A user that lets a lock go and takes it again for its next statement within a millisecond can win every round against such retries (§4). LibRed retries a refused page write lock or table lock the same way — 21 attempts, sleeping 100 ms between them — holding its statement's publish gate meanwhile, so the process's other statements on the file wait with it (only its writers, when the waiting statement only reads); it does not try the holder's user byte.

| Situation | Error |
| --- | --- |
| `UPDATE` / `INSERT` needing a page another user holds exclusively | *"Could not update; currently locked."* — native error **3218**. No holder is named and the lock-file record is not read. During the retries the refused user tries the holder's group-0 user byte once |
| `ALTER TABLE` / `CREATE INDEX` on a table another user holds (its shared byte on the TDEF page, e.g. inside a transaction) | refused 21 times on the exclusive region-4 lock, then *"The database engine could not lock table '\<name\>' because it is already in use by another person or process."* — **3211**. After the other user commits, the same DDL succeeds |
| Any open (shared or read-only) while a user holds the file exclusively | OS sharing violation → *"Could not use '\<path\>'; file already in use."* — **3045**. The refused process creates and deletes a lock file while trying |
| Exclusive open while a shared user is connected | *"You attempted to open a database that is already opened by user '\<security name\>' on machine '\<computer name\>'. Try again when the database is available."* — **3704**, the names read from that user's lock-file record (over OLE DB the number is the error's `SQLState`) |

Another user sees a table or column created or dropped by DDL on its next statement.

## Not known

Unmeasured, and so not stated as fact above:

- The slot of an exclusive holder (slot 0), and of a second concurrent user, during a multi-page batch; the `01 00` cases beyond those in page-00 §2.2. LibRed's exclusive holder writes slot 0 by the shared rule.
- What group 5 of region 1 is for; whether group 1 means anything beyond being the session byte.
- User numbers above 2 and the bound of 255; lock-file names for extensions other than `.accdb`.
- Row-level locking for recordset edits (DAO / ADO cursors); DAO deny-read / deny-write table opens.
- A kill landing between the leading and closing slot writes of a multi-page batch: the slot then holds `00 00`; what the next open does is not known.
- What ACE stores after `FF FF`. LibRed goes on from `00 01` (256, a fresh file's value), never into `00 00` or `01 00`, and counts on from `00 01` when its own slot holds either of those.
- What a reader does on finding another user's slot at `00 00` — mid-batch. LibRed does not wait: it drops its cache and reads, and reads again once the slot moves on.
- What ACE compares in its 5-second read, and which pages it refreshes.
- What ACE does when page 1's write lock itself is refused (assumed: the same retries, then 3218 — LibRed does that); concurrent `DELETE`s that free pages; an `UPDATE` that moves a row to a new page; long-value page allocation under contention; index page splits and DDL that allocates, beside another user's `INSERT`; the close-time release of held pages (page-05 §9.1) with another user connected — LibRed's close merges every bit of the global released-pages map then, as ACE's close does alone.
- Whether ACE locks page 1 when it changes a reference-form global map's bitmap page.
- What the waiting user's tries of the other user's group-0 byte during concurrent `INSERT`s (§4) are for.
- Inside an explicit transaction: whether the early write's ~500 ms runs from the first statement of a burst or the last; whether `COMMIT` adds the index counts to the page as the file has it as it adds the row count (no other user's statement that changed an index count succeeded beside the open transaction); which lock refuses another user's `DELETE` elsewhere in a table with an index; what a failed statement other than an `INSERT` writes early, and whether a failed one writes page 1 early; whether a statement that frees pages in the free map (rather than releasing them) writes page 1 early; whether a key-moving `UPDATE` holds the TDEF page; the complex id (`0x1C`) in the early write; the same-process refusal's wait (whether it retries as between processes).
- What refusing a long-value read's shared byte does to a reader (whether it retries, and what it reports).
- What ACE does to a page lock it is refused partway through a statement that already holds others — whether it lets those go before retrying.
- Why the deferral above is held to the next statement, and for how long an idle connection would hold it.
- Table locks: what a user's region-6 exclusive attempt does when it collides with another user's momentary one or a DAO deny-write holder; what ACE reports when its shared byte in region 4 or 5 is refused (another user mid-design-change); the locks of `DROP INDEX`, of `ALTER TABLE` beyond its first lock and of `DROP TABLE` beyond its first; whether a foreign key's referenced table is locked by `ALTER TABLE … ADD CONSTRAINT` or `CREATE TABLE … REFERENCES`; table locks under DAO deny-read / deny-write opens and for a read-only user; whether a design change inside an explicit transaction keeps its exclusive lock to `COMMIT`; how long a recordset's table byte is held while its rows are read; which other statements probe the other users' bytes.
- Why a table LibRed creates appears to ACE only at its 5-second refresh.
