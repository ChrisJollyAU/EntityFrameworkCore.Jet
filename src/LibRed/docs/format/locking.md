# Locking — the lock file and the locking protocol

> Part of the [LibRed Jet / ACE file-format reference](README.md). This is a cross-cutting topic: it describes the **side file** ACE keeps beside a database and the byte-range locks taken on it, not a page type. The page-0 commit-slot table it works with is [page-00 §2.2](page-00-database.md#22-the-user-commit-byte-table-0xe00-0xfff).

Everything here is **measured** against ACE 16 (`Microsoft.ACE.OLEDB.16.0`, x64) over OLE DB, with a shared user, a second shared user, and exclusive and read-only opens. What was not measured is listed under [Not known](#not-known) and in `verification-backlog.md`. The lock layout is implemented as `JetLockLayout` (`LibRed.Core/IO`) and decoded by `tools/JetLockTrace`; both state the layout in code and are kept consistent with this file.

**What LibRed does.** On Windows a shared or read-only open registers as a user as §1 and §3 describe — the lock file, the user's group-0 and group-1 bytes, its record — once per process per file, and the last close deregisters and deletes the file when alone (the page-1 lock ACE takes at close aside). A writer writes **its own commit slot** around every batch of page writes in the order and with the values of §6 — slot `n` as shared user `n`, slot 0 holding the file exclusively, none when it is neither (off Windows, or when the lock file cannot be opened) — and a page-0 write never carries the slot table. A registered user reads the commit table at the start of each statement and of each commit, and when another user's slot has changed since it last looked, drops every cached page and re-reads the catalog (§6). It takes **ACE's page write lock** on every existing page it writes, honours another user's, and runs a statement again when it got a page only after another user committed (§4). It takes **ACE's table locks** (regions 4–6) for every table a statement reads or writes and for every design change, honours another user's, and plans a statement again when it got a table only after another user committed (§4). It takes **no allocation lock and no long-value read lock**, so it is not a safe concurrent writer beside ACE for every statement: what is and is not covered is in §4 ([transactions design](../design/transactions.md)).

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

`CREATE TABLE` and `DROP TABLE` also write-lock the catalog pages they change (the `MSysObjects` and `MSysACEs` TDEF pages, their index pages and data pages). An `INSERT` locks page 1 and the table's TDEF page exclusively while it allocates.

**Row-level locking mode** (`Jet OLEDB:Database Locking Mode=1` on both users) changes none of these shapes for SQL `UPDATE` / `INSERT`: the whole page is locked exclusively and another user's `UPDATE` of a different row on the same page is refused. (Recordset edits: not measured.)

**What LibRed does with page locks.** A registered user (§3) that is not read-only takes the exclusive region-2 lock above on every page it writes that is already in the file — data, index, long-value, usage-map, TDEF and page 0 alike (page 0 unmeasured: no ACE write of it was seen under a shared open) — and none on a page it appends. Inside a transaction the lock is taken when the page is first written, before its committed image is read as the commit's baseline, and held until the commit's batch has ended with its slot write, or until the transaction, or the savepoint frame that first wrote the page, is rolled back. Outside one it is taken and released within the one-page batch. One process is one user: its channels share the lock, which goes when the last of them lets go, and they stay optimistic among themselves (the commit-time baseline check). A refused lock is retried as §7 describes, and then refused with ACE's 3218 text. A write that obtains a lock only after another user's slot has moved since its statement began has been working on pages that may since have changed: LibRed drops its cache, rolls the statement back and runs it again (up to three runs, then 3218) — through the SQL engine; a write made through the Core API is refused instead. Measured against ACE 16: each refuses the other's page with 3218, goes ahead once the other lets go, and neither loses the other's change on that page.

**What LibRed does with table locks.** A registered user takes the table locks of the table above on the table's TDEF page:

- every table a statement reads — every table it opens for its rows, not the catalog tables LibRed reads for itself — its shared byte in region 4; every table it writes, that and, under a momentary exclusive lock in region 6, its shared byte in region 5. Held for the statement, released when it returns; inside a transaction, until `COMMIT` or `ROLLBACK`, a rolled-back savepoint frame (a failed statement's included) letting go of those it first took;
- `ALTER TABLE` in every form, `CREATE INDEX`, `DROP INDEX` and `DROP TABLE`: region 4 exclusively, on the table they name (and on a foreign key's referenced table); `CREATE TABLE`: region 4 exclusively on the new table's TDEF page once allocated, and `MSysComplexColumns` opened for writing. Held as the read and write locks are: a lone statement's go when it returns, as ACE's DDL locks do; inside an explicit transaction they are held until `COMMIT` or `ROLLBACK` (ACE's own lifetime there is measured only for `ALTER TABLE … ADD CONSTRAINT … FOREIGN KEY`, whose two tables it keeps another user from deleting in until the transaction ends).

A read-only user takes the read locks only. A lock another user holds is retried as §7 describes, and then refused with ACE's 3211 naming the table — a refused shared byte (another user mid-design-change) and a refused region-5 or region-6 lock included, whose ACE errors are not measured. A table lock taken only after another user's slot has moved since the statement's scope began means the table's design may have changed under the plan: the statement is rolled back, and bound, planned and run again on the re-read catalog (up to three runs, then 3211).

One process is one user: its connections hold one OS lock per window, by count, and never wait for each other. Since Windows refuses an exclusive range overlapping a byte the same handle holds, a region-4 exclusive lock is taken as ACE takes an upgrade — the process's own shared byte, then the other users' bytes (bytes 1–256 of the window but its own; `window + 2`, 255 bytes for user 1, as measured; the shape for a higher user number is that rule carried on) — whether or not the process held the byte before. So a connection can change the design of a table another connection of the process (or itself) has in an open transaction, as before, while another user is still kept out; and to every other user's lock it conflicts as ACE's single range does.

Measured against ACE 16 with LibRed user 1: while ACE holds a table in a transaction (an `UPDATE` in it), LibRed's `ALTER TABLE … ADD COLUMN` and `CREATE INDEX` are refused, and fail with 3211 once LibRed's retries are spent; after ACE commits both succeed and ACE reads the new column on its next statement. LibRed holding a table in a transaction (an `UPDATE` in it) refuses ACE's `ALTER TABLE`, which fails with ACE's own 3211 after about 2.2 s; after LibRed commits it succeeds, and LibRed's next statement reads the new column. ACE reading a table in a loop while LibRed adds columns to it one statement at a time: no statement of either fails, and ACE reads each new column on the statement after it. A table LibRed creates appears to ACE within its 5-second refresh, not on its next statement (ACE gave 3078 meanwhile); one LibRed drops is gone on ACE's next statement (3011, *"could not find the object"*); one ACE creates or drops, on LibRed's next statement. After a LibRed `CREATE INDEX` that appends the index's root page, ACE refuses every read of that table with **3159** *"Not a valid bookmark."* until its 5-second refresh; after ACE's own `CREATE INDEX` from another process (which appends a page too), the other ACE reads the table at once. The cause is not known.

LibRed does **not** take, and so does not honour: page 1 and the TDEF page as ACE's `INSERT` locks them *for allocation*, as a read-modify-write held across the allocation — LibRed locks them only as pages it writes, from its first write of them, so an allocation decision it made on images read before the lock is not re-checked unless another user's slot moved meanwhile; the shared byte on a long-value page read (§4 table); a table lock for rows read after the statement returned (a query's rows are read lazily, and its locks went with the statement), or for a table read or written through the Core API outside a statement scope and a transaction. Each is listed in `verification-backlog.md`.

## 5. Lifetimes

| Case | When the locks are released |
| --- | --- |
| Autocommit `UPDATE` / `INSERT` | page locks when the pages are written — about **500 ms** after the statement returns, or at once if another statement follows immediately — and after the commit-slot write. Table bytes (regions 4/5) at statement end |
| Long-value read lock | about 500 ms after the statement |
| Explicit transaction | data and index page locks and the table bytes (regions 4/5) are **held until `COMMIT` / `ROLLBACK`**. Exception: an `INSERT`'s exclusive locks on the table's TDEF page and on page 1 are released about 500 ms after the `INSERT`, with those two pages written then; the TDEF page is locked again briefly at commit |
| DDL (`ALTER TABLE`, `CREATE INDEX`, `CREATE TABLE`, `DROP TABLE`) | every lock is released inside the statement |
| Idle | no locks are taken or refreshed |

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

**The exclusive holder's slot 0** can only be read before and after a session, since nobody else can open the file meanwhile. It is a little-endian counter like the others: from `01 01` (257) on the Northwind fixture, an exclusive open closed at once leaves it at 257, a `SELECT` leaves it, one `INSERT` raises it by one, and `CREATE TABLE` followed by three `INSERT`s raises it by five; slot 1 is untouched throughout. Which writes in the five-batch session are the extra batch, and the slot's value during a batch, are not known.

**Another user's commit.** With LibRed (user 1) and ACE (user 2) in one file and their writes taking turns, each reads the other's rows: ACE within its page refresh (under 7 s), and LibRed on its first statement after ACE's slot moves. ACE's slot rose by exactly its number of `INSERT`s, each landed before LibRed read it, and by nothing at close. A file LibRed has written its slot in opens, reads and writes in ACE.

**What LibRed does with the slot.** A batch is one publication: a transaction's commit, or a page write outside a transaction. Appended pages are written first (the file grown by `SetLength` when the page lies past its end, not by ACE's 1-byte write); then, with two or more existing pages, the slot (`00 00`); the existing pages; and the slot (counter + 1). The counter is the slot as the file holds it, read through the page cache, whose page-0 image every slot write updates. The slot is a 2-byte write at its own offset, handed to the OS at once. Page 0 itself is written only up to `0xE00`, and its cached image takes the table back from the file, so a page-0 write — a format-version raise, a password change — leaves every other user's slot as it is on disk. The commit-time conflict check ignores a difference confined to the table. If a publication fails part-way and every page written is put back, the slot is put back to its value before the batch; if a page cannot be put back, the slot is left as the batch left it (`00 00` after two or more existing pages).

**Noticing another user's commit (LibRed).** A registered user reads the 512-byte table at the start of each outermost statement scope and each commit — one positional read on a handle of its own, no allocation — and compares every slot but its own with what it last saw. On a change it drops every cached page image and parse, measures the file's length again and marks the schema changed, so the catalog and the format version are re-read. An open transaction keeps its own pages; its commit then compares them with the file and refuses on a page another user changed. A page another user changes without moving its slot stays cached and is served stale. An exclusive or unregistered channel does not look.

## 7. Conflicts and errors

A refused lock is retried: **21 attempts in all, about 108 ms apart (~2.2 s)**, then the statement fails. LibRed retries a refused page write lock or table lock the same way — 21 attempts, sleeping 100 ms between them — holding its statement's publish gate meanwhile, so the process's other statements on the file wait with it (only its writers, when the waiting statement only reads); it does not try the holder's user byte.

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
- Multi-user page allocation beyond the `INSERT` locks above.
- What refusing a long-value read's shared byte does to a reader (whether it retries, and what it reports).
- What ACE does to a page lock it is refused partway through a statement that already holds others — whether it lets those go before retrying.
- Why the deferral above is held to the next statement, and for how long an idle connection would hold it.
- Table locks: what a user's region-6 exclusive attempt does when it collides with another user's momentary one or a DAO deny-write holder; what ACE reports when its shared byte in region 4 or 5 is refused (another user mid-design-change); the locks of `DROP INDEX`, of `ALTER TABLE` beyond its first lock and of `DROP TABLE` beyond its first; whether a foreign key's referenced table is locked by `ALTER TABLE … ADD CONSTRAINT` or `CREATE TABLE … REFERENCES`; table locks under DAO deny-read / deny-write opens and for a read-only user; whether a design change inside an explicit transaction keeps its exclusive lock to `COMMIT`; how long a recordset's table byte is held while its rows are read; which other statements probe the other users' bytes.
- Why ACE refuses reads with 3159 after another user's `CREATE INDEX` appended a page when that user is LibRed and not when it is ACE, and why a table LibRed creates appears to ACE only at its 5-second refresh.
