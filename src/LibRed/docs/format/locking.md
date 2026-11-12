# Locking — the lock file and the locking protocol

> Part of the [LibRed Jet / ACE file-format reference](README.md). This is a cross-cutting topic: it describes the **side file** ACE keeps beside a database and the byte-range locks taken on it, not a page type. The page-0 commit-slot table it works with is [page-00 §2.2](page-00-database.md#22-the-user-commit-byte-table-0xe00-0xfff).

Everything here is **measured** against ACE 16 (`Microsoft.ACE.OLEDB.16.0`, x64) over OLE DB, with a shared user, a second shared user, and exclusive and read-only opens. What was not measured is listed under [Not known](#not-known) and in `verification-backlog.md`. The lock layout is implemented as `JetLockLayout` (`LibRed.Core/IO`) and decoded by `tools/JetLockTrace`; both state the layout in code and are kept consistent with this file.

**LibRed does not take any of these locks, write a lock file or maintain the commit table.** It is a single-writer engine ([transactions design](../design/transactions.md)).

## 1. The lock file

A shared or read-only user locks a side file next to the database: **`<name>.laccdb`** for an `.accdb`. (Other extensions — the `.ldb` of an `.mdb` — are not measured.) **Every byte-range lock is on the lock file; none is on the database file.**

| | |
| --- | --- |
| Database file | opened read/write, share mode Read\|Write |
| Lock file | created if missing, read/write, share mode Read\|Write |
| Content | one **64-byte record per user**, at offset `(userNumber − 1) × 64`: computer name then security name, 32 bytes each. The split into two names is from Microsoft's Jet locking white paper; only a 64-byte write at that offset was measured. A user writes its record right after taking its user locks |

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

## 5. Lifetimes

| Case | When the locks are released |
| --- | --- |
| Autocommit `UPDATE` / `INSERT` | page locks when the pages are written — about **500 ms** after the statement returns, or at once if another statement follows immediately — and after the commit-slot write. Table bytes (regions 4/5) at statement end |
| Long-value read lock | about 500 ms after the statement |
| Explicit transaction | data and index page locks and the table bytes (regions 4/5) are **held until `COMMIT` / `ROLLBACK`**. Exception: an `INSERT`'s exclusive locks on the table's TDEF page and on page 1 are released about 500 ms after the `INSERT`, with those two pages written then; the TDEF page is locked again briefly at commit |
| DDL (`ALTER TABLE`, `CREATE INDEX`, `CREATE TABLE`, `DROP TABLE`) | every lock is released inside the statement |
| Idle | no locks are taken or refreshed |

**Idle poll.** Each shared user reads the whole 512-byte commit table at `0xE00` every **5 seconds**.

**A kill inside the deferral.** A user killed right after an autocommit `UPDATE` returned (inside the ~500 ms) had written nothing: the update is lost, the file is not left suspect, and later opens succeed.

## 6. Commit slots and page writes

A user writes only **its own** 2-byte slot ([page-00 §2.2](page-00-database.md#22-the-user-commit-byte-table-0xe00-0xfff)): `0xE00 + 2n` when shared, `0xE00` when exclusive. For each batch of page writes:

- **One existing page:** the page, then the slot (counter + 1). `00 00` never appears.
- **Two or more existing pages:** the slot (`00 00`), the pages, the slot (counter + 1). The slot reads `00 00` for exactly as long as the pages are being written — the white paper's "mid-write to disk" state.
- **Pages newly appended to the file** are written first — a 1-byte write at the last byte of the new page, then the full page(s) — before the leading slot write (`00 00`).
- Every batch **ends with a slot write** storing the user's commit counter plus one (little-endian 16-bit); the page locks are released after it. This holds for a lone statement, an explicit transaction's commit and closing the connection (a one-page batch: counter + 1 only).
- The counter rises by exactly one per batch; the other users' slots and slot 0 are untouched.

## 7. Conflicts and errors

A refused lock is retried: **21 attempts in all, about 108 ms apart (~2.2 s)**, then the statement fails.

| Situation | Error |
| --- | --- |
| `UPDATE` / `INSERT` needing a page another user holds exclusively | *"Could not update; currently locked."* — native error **3218**. No holder is named and the lock-file record is not read. During the retries the refused user tries the holder's group-0 user byte once |
| `ALTER TABLE` / `CREATE INDEX` on a table another user holds (its shared byte on the TDEF page, e.g. inside a transaction) | refused 21 times on the exclusive region-4 lock, then *"The database engine could not lock table '\<name\>' because it is already in use by another person or process."* — **3211**. After the other user commits, the same DDL succeeds |
| Any open (shared or read-only) while a user holds the file exclusively | OS sharing violation → *"Could not use '\<path\>'; file already in use."* — **3045**. The refused process creates and deletes a lock file while trying |
| Exclusive open while a shared user is connected | *"You attempted to open a database that is already opened by user '\<security name\>' on machine '\<computer name\>'. Try again when the database is available."* — **3704**, the names read from that user's lock-file record |

Another user sees a table or column created or dropped by DDL on its next statement.

## Not known

Unmeasured, and so not stated as fact above:

- The slot of an exclusive holder (slot 0), and of a second concurrent user, during a multi-page batch; the `01 00` cases beyond those in page-00 §2.2.
- What group 5 of region 1 is for; whether group 1 means anything beyond being the session byte.
- User numbers above 2 and the bound of 255; lock-file names for extensions other than `.accdb`.
- Row-level locking for recordset edits (DAO / ADO cursors); DAO deny-read / deny-write table opens.
- A kill landing between the leading and closing slot writes of a multi-page batch: the slot then holds `00 00`; what the next open does is not known.
- Which process's 5-second poll leads to a cache refresh, and what it compares.
- Multi-user page allocation beyond the `INSERT` locks above.
