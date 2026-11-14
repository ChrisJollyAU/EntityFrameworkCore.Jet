# LibRed transaction & concurrency design

Status: **draft / accepted direction** · Date: 2026-07-18

> **Implementation note (2026-07-24) — isolation is a deferred-write overlay, not undo + strict 2PL.**
> Isolation shipped by a simpler route than the write-through-undo-under-held-locks sketch in §3 L1 / §4.
> `PageChannel` now buffers a transaction's writes into a **private per-connection overlay**
> (`page → uncommitted bytes`) instead of writing them through to disk and the shared page cache. Reads on the
> owning channel see the overlay (read-your-writes); every other channel on the file sees only committed
> state — so a concurrent reader never observes an uncommitted page. **Commit** replays the overlay to
> disk + shared cache in ascending page order; **rollback** simply discards it (nothing to restore, truncate,
> or flush); **savepoints** snapshot the prior overlay state per frame (`Transaction`). This gives
> **read-committed** isolation **without holding exclusive locks for the transaction's lifetime** — so the
> writer-serialization / reader-blocking of strict 2PL (§4) is not needed for isolation, and EF's parallel
> shared-store tests (each mutating inside a rolled-back transaction) no longer leak into concurrent readers.
> The exclusive page lock is now held only for the duration of an individual committed page write, not the
> whole transaction. Before publishing, commit compares every page's committed plaintext image with the image
> from which that overlay page was first derived, under a per-file publish gate. Writers touching disjoint pages
> may both commit; if another connection changed an overlapping data/index/TDEF/usage-map page, the stale commit
> fails with a write conflict and remains open for rollback. This prevents silent lost updates without strict
> two-phase locking. If publication itself fails after writing some pages, that prefix is restored from the
> validated baselines (and appended tail pages are truncated), leaving the transaction open and rollbackable.
> **A page check cannot see what a transaction only read**, which is where write skew gets in: an
> `INSERT` checks its foreign key's parent row and writes no page for it, so a concurrent connection may delete
> that parent, and each transaction's precondition holds at the moment it is checked while the pair of them
> leaves a child referencing nothing. So a transaction also carries a **read set** of the conditions its writes
> depend on (`PageChannel.DependOn`), each re-evaluated under the publication gate before anything is
> published; a commit whose condition has stopped holding is refused like a write conflict. The conditions are
> semantic rather than physical — the FK's parent is sought again, not its page compared — so an unrelated row
> on the same page cannot produce a false conflict. A missing parent only refuses the commit while it is still
> needed: while the relationship survives and a child row still holds the key, so a transaction that later
> deleted or moved its child, or dropped the relationship, commits. The relationship is identified by the
> definition pages of its tables and the ids of its columns, so renames inside the transaction do not lose it,
> and one dropped and added again under the same name over other columns is a different relationship. The same
> hole exists from the parent's end: a `DELETE` of a parent row, or a change to its key, that found no child
> (or cascaded or nulled the ones it found) writes nothing another connection's child insert would conflict
> with, and that insert finds the parent it has not seen deleted. So the parent side registers the same
> condition for the key it removed — no child row may hold it without a parent row holding it too. It is one
> condition from either end, so a condition registered under a key the transaction already holds (the same
> relationship and key) is held once. `ALTER TABLE … ADD FOREIGN KEY` reads the same way, checking every existing child row against its
> parent, so it registers one condition for the relationship: while it survives, every child row still has its
> parent. ACE gets the same guarantee by holding both tables exclusively until the transaction ends. A savepoint rollback drops the conditions recorded after it, and their keys, along with the writes that
> needed them.
> Schema-changing commits also advance a shared per-file catalog generation; other open
> connections invalidate their parsed table/relationship/view caches on the next catalog access, while ordinary
> DML does not force a catalog reload. The undo log described below is gone, and with it the before-image
> `SavepointStack`: savepoint frames live in `Transaction` and snapshot overlay state. Everything else here — the
> lock-manager layering (L0), the ACE co-residency constraint (§2), commit-byte / cross-process protocol — still
> stands as the roadmap. Of the cascade design in §3 L3, the **delete** cascade is the bounded worklist; an
> **ON UPDATE** cascade recurses one call per level instead (a rewritten child applies its own relationships), and
> terminates because a row already holding the new key is not rewritten again. See `TransactionIsolationTests`.

> **Deferred (2026-09-24) — the commit-byte protocol and the Jet lock manager (§3 L0, phases 7–8).** The two
> are one job, not two: a writer's commit slot is `0xE00 + 2n` for *its own* user number *n*, and that number
> comes from the position it takes in the `.laccdb` — so there is no writing the commit table without first
> doing the lock-file registration. The lock side has since been **measured** (ACE 16, two users): the lock
> offsets and shapes, the `.laccdb` record, its lifecycle, the lock lifetimes, the retry and error behaviour, the
> idle 5-second read of the commit table and the order of a user's own slot writes around a batch of page writes
> are all in [locking.md](../format/locking.md) and [page-00 §2.2](../format/page-00-database.md). Page 0 is
> **not** under a byte-range lock (no byte-range lock is taken on the database file at all). What is still
> missing is what the 5-second read compares, the slot of an exclusive holder and of a second concurrent user
> during a multi-page batch, and what a kill while a slot holds `00 00` leaves for the next open. The values
> themselves are measured: a multi-page batch writes `00 00`, the pages, then the user's counter plus one; a
> one-page batch writes the page then counter plus one only. LibRed now does the **registration** (on Windows):
> a shared or read-only open takes a user number in the `.laccdb` exactly as ACE does — the group-0 and group-1
> bytes, the 64-byte record — and the last close in the process deregisters and deletes the file when nobody
> else is connected (`JetUserRegistration`, held by the per-file `PageCache` so every channel in a process is
> one user). And it does the **commit slots**: `PageChannel.PublishBatch`, which every publication goes through —
> a commit's overlay and a write outside a transaction alike — writes appended pages, then `00 00` before two or
> more existing pages, the pages, and the counter plus one, to slot *n* as shared user *n* or slot 0 holding the
> file exclusively; page 0 is written only up to the table. A registered user's `PageCache` reads the table at
> the start of each outermost read or write scope (each statement, each commit) and, when another user's slot has
> moved, drops every cached page and marks the schema changed. The exclusive holder's use of slot 0 follows the
> shared rule unmeasured — its values cannot be sampled while it holds the file. And it takes **ACE's page write
> locks**: a registered user locks each page already in the file when it is first written — in a transaction, at
> the point its commit baseline is recorded, so the baseline is read under the lock; outside one, inside the
> one-page publication — with ACE's exclusive range on the `.laccdb` handle, and lets go after the batch that
> writes it has ended with its slot write, or when the transaction, or the savepoint frame that first wrote it, is
> rolled back. The baseline map is the record of what is held (a page with a baseline is a locked page). The OS
> lock belongs to the process: `JetUserRegistration` counts the channels holding each page, under a lock of its
> own, takes the range when the count leaves zero and releases it when it returns there — so channels of one
> process stay optimistic among themselves exactly as before. A refused lock is retried as ACE retries it (21
> attempts, 100 ms apart, then ACE's 3218 *"Could not update; currently locked."*), with the statement's publish
> gate held throughout: in-process readers and writers of the file wait with it, up to about 2 s. A lock newly
> taken after another user's slot has moved since the statement's scope looked throws `StaleReadException` with
> the cache already dropped; `QueryEngine` rolls the statement back (its implicit transaction or savepoint frame,
> keeping nothing a failure keeps) and runs it again, up to three runs. A caller writing through the Core API
> cannot be re-run and sees the exception. And it takes **ACE's table locks**: `JetDatabase.OpenTable` — where
> every table a statement reads or writes is opened, and the catalog's own reads are not — takes the region-4
> shared byte, and `Table`'s write path the region-5 byte under a momentary region-6 lock; the schema editor's
> table resolution (`DesignTable`, and `DropTable`, `RenameTable`, `Create`) takes region 4 exclusively.
> `PageChannel.LockTable` decides the owner: every table lock belongs to the open transaction — a lone writing
> statement's implicit one included, so its DDL locks go inside it — released at commit and rollback, a savepoint
> rollback truncating them to the frame's count (the per-frame counts `Transaction` already keeps for releases and
> dependencies); outside one, to the statement, the outermost `ReadConsistent`/`WriteExclusive` scope. The
> process's lock table
> (`JetUserRegistration`) carries every lock by region and page with its shared and exclusive holder counts, and
> takes a table's exclusive lock over the process's own shared byte, as ACE's upgrade, so the process's
> connections never stand in each other's way. A table lock newly taken after another user's slot moved throws
> `StaleReadException` too, and `QueryEngine` re-runs every statement — reads included — in a new scope, bound
> and planned afresh on the re-read catalog. A page lock newly taken after another user's commit is narrower:
> `PageCache.RefreshAtLock` sets the dropped images aside for the rest of the writing scope rather than discarding
> them, and `LockForWrite` re-runs the statement only when the locked page, read again, differs from the image the
> process held before (or that image is unknowable: an eviction since the scope began, a read-only scope) — ACE's
> own discipline of lock, re-read, change; without it a statement taking several locks starves while another user
> commits steadily. **Counters and allocation are locked read-modify-writes:** `PageChannel.LockForUpdate` — the
> lock-and-baseline step `WritePage` takes before a page's first write in a transaction, split out — is called
> before the read wherever a page's current contents decide the change: `RowInserter` locks the table's TDEF page
> before it reads the counters (an `INSERT` before anything else), and `PageAllocator.ReadGlobalMaps`, the
> allocator's one read of the global maps, locks their holder page first for every caller that may change them
> (a take from the run already reserved changes no map and locks nothing). Held to the end of the transaction —
> so in an explicit transaction to `COMMIT`, where ACE writes them and lets them go about 500 ms after the
> `INSERT`; doing that needs the overlay to keep those two pages' changes as deltas applied at commit (see
> `verification-backlog.md`). A rollback's frees and the close-time merge lock page 1 up front and read it under
> the lock, never refusing. Pages appended past the end of the file are the user's own (the map gave them); a
> zero page appended only to keep the file contiguous is not written once the file reaches it, and growth and a
> failed publication's cut-back are measured against the file. **A long-value read takes no shared byte** — see
> [locking.md §4](../format/locking.md) for what that leaves unsafe. What ships today is the deferred-write
> overlay, the write-conflict check on commit, process-local page locks, the user registration, the commit slots,
> ACE's page write locks, its table locks, and its TDEF-first and page-1 discipline for counters and allocation.

This is the ground-up design for LibRed's transaction support and the concurrency
infrastructure it sits on. It replaced the ad-hoc page-level undo log that used to be in
`PageChannel`, and is written so the localized atomicity gaps the production audit
found (`LIBRED_AUDIT_REPORT.md`, the P0s) are closed by *one* model rather than
patched site by site.

## 1. Goals and non-goals

**Goals**
- **Statement atomicity.** Every statement is all-or-nothing, even with no explicit
  user transaction. A late constraint/I/O failure never leaves a half-written row,
  index, usage map, LVAL chain, TDEF, or catalog entry.
- **Explicit transactions.** `BEGIN`/`COMMIT`/`ROLLBACK` (and the ADO
  `LibRedTransaction`) group many statements atomically, with **savepoints** (nesting).
- **Cascade correctness.** Cascade delete runs as a bounded worklist inside the
  transaction — no unbounded recursion, no double-mutate on diamond graphs. (Cascade
  update recurses per level; see the implementation note above.)
- **Concurrency infrastructure, built in from day one.** Every read/write flows through
  explicit lock-acquisition seams and per-connection transaction context, so the
  real lock manager is a *fill-in*, not a later rewrite.

**Non-goals (deferred, but not precluded)**
- **Byte-exact ACE lock interop.** The initial lock manager is **self-consistent**
  (LibRed↔LibRed): a correct page-locking protocol using our own offsets. Reproducing
  Jet's exact `LockFileEx` offset bands, the `.laccdb` per-user records, and the
  page-0 commit-byte polling — so a live `MSACCESS.EXE` can share the file — is a later
  swap of *values and detection*, not of *structure*.
- **Crash durability beyond Jet's.** We match Jet's model: detect-and-repair, not a
  write-ahead log. A WAL is actively incompatible with ACE co-residency (§5).
- **Record-level locking.** Page-level only. ACCDB per-page encryption already forces
  whole-page granularity — you cannot sub-page lock what you decrypt as a unit.
- **Multi-writer performance.** Writers serialize for now (priority order:
  correctness → speed → concurrency).

## 2. Why not a WAL / journal (the ACE-co-residency constraint)

The hard requirement is that Access and LibRed can hold the **same file open at the
same time**. That rules out any LibRed-private durability side-channel:

- Access writes pages under *its* protocol and never touches a LibRed journal, so our
  recovery would roll its committed work back — corruption, not safety.
- Jet itself has no WAL. Durability = the page-0 **commit-byte table** (`0xE00`–`0xFFF`,
  256×2 bytes: idle `00 01`, **mid-write `00 00`** (the white paper's meaning; ACE 16 writes it as a multi-page batch's leading slot write), corrupt `01 00`) plus the OS
  byte-range locks. A `00 00` with no matching `.ldb` lock after a crash ⇒ Jet marks the
  DB suspect and repairs.

So LibRed **matches Jet's on-file commit protocol** and provides atomicity/rollback
**in-process** (while the process lives) via the deferred-write overlay (originally an undo log). Cross-process consistency
comes from the lock protocol; crash consistency is Jet's (weak-by-design) detect-and-repair.
We may be *more disciplined* about flush ordering within that protocol, but we add no
structure to the file that Access doesn't understand.

## 3. Layered architecture

```
 L4  ADO surface          LibRedTransaction / LibRedCommand enforce against L2
 L3  Statement layer      QueryEngine: implicit per-statement txn; cascade worklist
 L2  Transaction manager  per-connection Transaction: begin/commit/rollback + savepoints
 L1  PageChannel          write choke point: lock seams + per-transaction write overlay
 L0  Lock manager         page locks, commit-byte map, .ldb — self-consistent now, Jet later
```

The invariant that makes L0 a later fill-in: **L1 already calls
`EnterShared(page)` / `EnterExclusive(page)` (and the matching `Exit…`) around every read/write and threads a
per-connection transaction context.** Today those calls resolve to the process-local `MonitorLockManager`; the
Jet coordinator is dropped in behind the same interface.

### L0 — Lock manager (`ILockManager`)

Owns cross-connection/cross-process coordination. The interface as it ships:

```
public interface ILockManager
{
    void EnterShared(int page);      // read lock; multiple readers
    void ExitShared(int page);
    void EnterExclusive(int page);   // write lock; single writer, excludes readers
    void ExitExclusive(int page);
}
```

The interface stays the **in-process** seam: `MonitorLockManager` behind it, and the test doubles. None of the
cross-process work went into it. Claiming and releasing a user index is `JetUserRegistration`, whose number
`PageChannel.UserNumber` exposes; the commit slot is part of publishing a batch (`PageChannel.PublishBatch`), and
watching the other users' slots is part of entering a scope (`PageCache.ReadConsistent` / `PublishLocked`); the
page write locks and the table locks are `JetUserRegistration.Lock` / `Unlock` — the page locks taken by
`PageChannel.LockForWrite` when a page is first written and let go at commit, rollback, savepoint rollback and the
end of a write outside a transaction, the table locks by `PageChannel.LockTable` and let go at the end of the
transaction or the statement.
See the deferral note above.

- **`SelfConsistentLockManager` (initial):** page locks via `FileStream.Lock` on the
  main handle at *our own* deterministic offset band (`page → base + page*stride`);
  a simple in-file or side-file presence map. Correct LibRed↔LibRed, Windows-and-Unix
  where `FileStream.Lock` is supported; a `MonitorLockManager` (process-local
  `ReaderWriterLockSlim` per page) covers single-process / cross-platform.
- **`JetLockManager` (later):** the exact Jet 4/ACE offsets (`(region << 28) | (page << 9) | byte`, regions 1, 2
  and 4–6, on the `.laccdb` handle — [locking.md](../format/locking.md)), the page-0 commit-byte table
  registration/polling, the `.laccdb` 32+32-byte identity records. Windows-only. This is a values+detection swap; the L1
  call sites and L2 semantics do not change.

### L1 — PageChannel (write choke point)

> **Superseded — kept as the original sketch.** The before-image undo and held-to-commit exclusive locks below
> were replaced by the deferred-write overlay (see the implementation note at the top): writes buffer per
> transaction, and the exclusive page lock is held only while a committed page is written.

- `ReadPage(p)`: `using (locks.AcquireShared(p))` → decode/return (existing parsed-page
  cache unchanged).
- `WritePage(p, bytes)` under a transaction:
  1. `AcquireExclusive(p)`,
  2. snapshot the **before-image** into the active transaction's undo set (once per page
     per savepoint frame),
  3. `MarkCommitPending()` on first dirty page of the txn,
  4. write the page (write-through, as today),
  5. keep the exclusive lock until commit/rollback (strict two-phase, §4).
- The undo store moves **off** `PageChannel` (no more single global `_undo`) into the
  per-connection `Transaction` (§4). `PageChannel` becomes stateless w.r.t. transactions
  beyond holding the file/lock handles.

### L2 — Transaction manager

> **Superseded in part.** `Transaction` is per connection as designed, but it holds overlay state rather than
> before-images: a savepoint frame snapshots the overlay, rollback discards it, and there is no truncation or
> lock release to do. The frame stack below is the original sketch.

`Transaction` is **per connection** (EF holds several connections on one shared
`PageChannel`; a single global undo log was the original bug). Contents:

- `SavepointStack` — each frame holds its own `Dictionary<int, byte[]>` of before-images
  and the set of exclusive locks first taken in that frame. `Begin` pushes; `Release`
  merges a frame down; `RollbackTo` restores that frame's before-images in reverse and
  releases its locks.
- **Commit:** flush (§5 ordering), `MarkCommitDone()`, release all locks, discard undo.
- **Rollback:** restore before-images newest→oldest, truncate to the transaction's
  original page count (pages allocated in the txn are discarded), `MarkCommitDone()`,
  release locks.
- **Writer serialization:** a transaction that takes its first exclusive lock is the
  writer; others block (or fail-fast per isolation policy) until it ends. Readers proceed
  under shared locks.

### L3 — Statement + cascade layer

- **Implicit transaction:** `QueryEngine.Route` runs each statement inside
  `txn ??= connection.BeginImplicit()`; on success it commits the implicit txn, on any
  throw it rolls back. If an explicit user transaction is open, statements are savepoints
  within it instead. This single change closes the statement-atomicity P0 for *all*
  writers (row insert, DDL, view create, LVAL) without per-site edits.
- **Cascade as a worklist:** delete collects affected child rows into a stack with
  a `visited` (in-progress) set; cycles terminate, diamonds mutate once. An update cascade
  rewrites each child through the same code as a directly updated row, which applies that
  child's own relationships in turn. All mutations are in the one transaction, so a
  mid-cascade failure rolls the whole thing back.

### L4 — ADO surface

- `LibRedConnection.BeginTransaction()` → creates the connection's explicit `Transaction`;
  `LibRedTransaction.Commit/Rollback` drive L2.
- `LibRedCommand` executes inside its connection's active transaction (explicit or the
  per-statement implicit one). The "stored but unenforced transaction" gap is closed:
  a command with a foreign/stale transaction is rejected.

## 4. Transaction semantics

- **Atomicity unit:** the statement (implicit) or the explicit `BEGIN…COMMIT` span.
- **Isolation:** read-committed, from the deferred-write overlay — see the implementation
  note at the top. (The original plan was strict two-phase page locking with exclusive
  locks held to commit; the overlay made that unnecessary.) MVCC/snapshot is a later
  concurrency-phase option and is not designed in here beyond "don't preclude".
- **Savepoints:** nesting via the frame stack; an inner statement inside an explicit txn
  is a frame, so its failure rolls back just that statement, not the user's transaction.
- **A read-only database** begins, commits and rolls back transactions like any other — ACE lets a read-only
  connection `BEGIN` and read inside it (verified) — and nothing can enter the overlay, because a write is refused
  before it reaches a page: each writer refuses where ACE does in the order of its checks, with ACE's message for
  its kind (`PageChannel.RefuseIfReadOnly`), and the page writer refuses anything that gets past that.
- **Nested transactions** (SQL `BEGIN`/`COMMIT`/`ROLLBACK`, and any caller that nests) map
  onto the one savepoint stack — there is a single physical transaction, never truly nested
  ones. A per-connection **transaction controller** holds a depth counter shared by *both*
  front doors (the ADO API and SQL statements), so they can't open parallel transactions:
  - **BEGIN** at depth 0 opens the real L2 transaction; at depth ≥ 1 it pushes a savepoint.
    Depth increments.
  - **COMMIT** at depth 1 commits the real transaction; at depth ≥ 2 it *releases* the
    innermost savepoint (merges its work into the enclosing level). Depth decrements.
  - **ROLLBACK** at depth 1 rolls the transaction back and closes it; at depth ≥ 2 it rolls
    back to the innermost savepoint (undoing just that level). Depth decrements.
  - This follows **Jet/DAO nested semantics** — commit/rollback act on the *innermost* level
    — which is our compatibility target, *not* SQL Server's "unqualified ROLLBACK unwinds
    all levels". A named `SAVE`/`ROLLBACK TRANSACTION <name>` addresses a specific frame.
  No new mechanism is needed: nesting is the Phase-1 savepoint stack, driven by the controller.
- **Durability:** commit writes dirty pages out (to the OS, as ACE does — §5) then clears
  the commit-byte; a crash before
  the clear leaves the Jet "suspect" signal (later, with `JetLockManager`) → repair path. (Measured:
  a user killed inside ACE's ~500 ms deferral after an autocommit `UPDATE` had written nothing — the
  update is lost and the file is not suspect. A kill between the two slot writes of a multi-page
  batch leaves the slot at `00 00`; what the next open does is not measured.)
  With the self-consistent manager, recovery is process-local (no cross-process crash
  interop claimed yet).

## 5. Flush / commit ordering (matching Jet, staying ACE-safe)

**What ACE does (measured: ACE 16 over OLE DB, Process Monitor).** ACE never forces the OS
cache to disk — no `FlushFileBuffers` on a statement, on an explicit `COMMIT`, or at close —
and it opens the file without write-through. Its durability is the OS file cache plus the
commit-byte protocol, nothing more. What its settings change is only *when* the write
reaches the OS:

| Setting (registry default) | Effect |
| --- | --- |
| `ImplicitCommitSync` = **no** | A lone statement's writes are issued *after* the statement returns — deferred, and flushed to the OS when the next statement starts or the timeout fires |
| `UserCommitSync` = **yes** | An explicit `BEGIN…COMMIT` writes its pages inside `COMMIT`, before it returns |
| `FlushTransactionTimeout` = **500 ms** | How long a deferred write waits; overrides `SharedAsyncDelay` (50) and `ExclusiveAsyncDelay` (2000) |
| `PageTimeout` = **5000 ms** | How long another user's cached pages may stay stale |

Two orderings hold in every mode, and they are the ones that matter for concurrency:

- **Pages reach the OS before the lock is released.** A page's `.laccdb` lock is released
  only after that page's `WriteFile`, never before — and the batch's commit-slot write
  also precedes the release. Cross-connection visibility therefore
  rests on the write reaching the OS — which every handle on the machine reads through —
  not on it reaching the disk.
- **Every batch ends with a write of the connection's own slot** (`0xE00 + 2n` for user
  *n*). A batch of one existing page is the page then the slot (counter + 1); a batch of two or more is
  the slot (`00 00`), the pages, the slot (counter + 1) ([page-00 §2.2](../format/page-00-database.md)). An
  explicit transaction writes nothing at all until its commit — apart from an `INSERT`'s
  TDEF page and page 1, written about 500 ms after the `INSERT`
  ([locking.md](../format/locking.md)).
- File growth is a 1-byte write at the last byte of the new page, then the page itself;
  newly appended pages are written before a multi-page batch's leading slot write.

**What LibRed does.** The same, deliberately: a commit publishes every overlay page to the
OS under the publish lock, and neither a commit nor a close forces them to disk. An
autocommit statement writes its pages immediately rather than deferring them — stricter
than ACE's default, and the deferral is what the engine-settings work would add. Each publication is one
batch with ACE's slot writes around it (`PageChannel.PublishBatch`, [locking.md §6](../format/locking.md)):
the closing slot write goes to the OS at once, and with it everything the batch wrote. Never
leave a header pointing at pages the OS has not been given. No structure is written that
Access cannot parse — the commit-byte table and lock offsets are the only
concurrency-visible state, exactly as Jet uses them.

## 6. Phased implementation plan

Build correctness first with lock seams stubbed; drop the Jet lock manager in last.

1. **L2 core.** ✅ done, then superseded by the overlay (see the implementation note): the
   before-image undo and `SavepointStack` shipped first and were replaced — a transaction's
   writes now buffer in the overlay, savepoint frames live in `Transaction`, and rollback
   discards pages rather than restoring and truncating them.
2. **L3 statement atomicity.** ✅ done. Wrap every `QueryEngine` statement in an implicit txn;
   convert the audit's non-atomic writers (`RowInserter`, `SchemaEditor`, `JetCatalog`,
   usage-map/LVAL) to rely on it. Regression: inject a late failure mid-statement, assert
   no partial state.
3. **Cascade worklist.** ✅ done for DELETE. Replace recursive cascade delete with the
   stack+visited worklist inside the txn. Tests: cyclic FK, diamond FK, deep chain (former
   stack-overflow). An update cascade recurses per level.
4. **L1 lock seams + `MonitorLockManager`.** ✅ done. Introduce `ILockManager`, route
   read/write through it (cache-hit reads stay lock-free via copy-on-write `Store`), ship
   the process-local monitor implementation.
5. **L4 ADO enforcement.** ✅ done. Wire `LibRedTransaction`/`LibRedCommand` to L2; reject
   stale/foreign transactions; EF savepoint support (`SupportsSavepoints`).
6. **SQL transaction-control statements (with nesting).** ✅ done. Engine-native `BEGIN`/`COMMIT`/
   `ROLLBACK [TRANSACTION|WORK]`. Parse to AST → a new `QueryEngine.Route` branch that drives a
   per-connection **transaction controller** (the §4 depth counter) on the *same* L2 as the
   ADO front door — so a SQL `BEGIN` and an ADO `BeginTransaction` can't open parallel
   transactions, and the controller is the single source of `InTransaction`. Two must-haves:
   (a) transaction-control statements are **exempt from the Phase-2 implicit wrap** — they
   manage the transaction rather than run inside one; (b) nesting reuses the Phase-1 savepoint
   stack (BEGIN→savepoint at depth ≥ 1; COMMIT→release; ROLLBACK→rollback-to). This is the
   audit's deferred "ownership/liveness" item — the controller is where ADO and SQL reconcile.
   Relevant to executing generated migration scripts and raw `BEGIN…COMMIT` batches; the EF
   runtime path keeps using the ADO API. Tests: nested BEGIN/COMMIT/ROLLBACK depth behaviour,
   SQL-then-ADO consistency, control statements not self-wrapped.
7. **L0 `SelfConsistentLockManager`.** Real byte-range page locks (our offsets) + presence
   map; multi-*process* single-writer LibRed↔LibRed.
8. **(Concurrency phase) Jet-exact locking.** Jet-exact offsets, commit-byte polling,
   `.laccdb` records → live co-residency with `MSACCESS.EXE`. The lock bands and shapes, the
   `.laccdb` record, the poll interval (5 s) and the slot write order and values are measured
   ([locking.md](../format/locking.md)). Done: the registration, the commit slots, the page write locks and the
   table locks (see the deferral note). Left: page 1 and the TDEF page as an allocation /
   counter lock, the long-value shared byte, the released-pages handling at close; and the unknowns — what the
   poll compares, the slot of an exclusive holder and of a second user during a multi-page batch,
   the `.ldb` of an `.mdb`, and what the next open does after a kill while a slot holds `00 00`.

Steps 1–3 close every transaction-related P0 in the audit (✅). Step 5 completes the
in-process transaction contract (✅), and step 6 adds the SQL front door onto it. Steps 7–8
are the cross-process concurrency ladder and land independently — the seams from step 4
don't move.

## 7. Open questions

- **Reader isolation while a writer commits:** ✅ every statement holds the shared cache's publication gate for
  its complete duration, so a reader sees either the complete pre-commit or complete post-commit page set, never
  a torn mixture while an overlay is being published. The gate is a **reader/writer** lock, not a mutex: a
  statement that cannot write (`SELECT`, a set operation, a system-variable select) takes it **shared**, so
  concurrent readers on one file still run together; everything else takes it **exclusive** for the whole
  statement, which is what makes a multi-page write atomic to readers. Anything not provably read-only takes the
  exclusive scope — the shared scope cannot be upgraded and says so rather than deadlocking. Parsing happens
  before the scope is taken (it touches no pages). See `ReaderWriterIsolationTests`.
- **Implicit-txn cost:** per-statement begin/commit must be cheap for read-only statements
  (no dirty pages ⇒ commit is just lock release). A read-only statement, and a commit with
  nothing in its overlay, publishes no batch and so never touches the commit slot; nor does a commit whose pages
  all hold what is committed (`PublishBatch` writes only a page it changes). A rollback's own writes — the counters
  it keeps, the global maps put right — are gathered in a transaction of their own and published as at most two
  batches, the AutoNumbers drawn first, as ACE's failed statements write them ([locking.md §6](../format/locking.md)).
- **Deadlock policy:** within a process there is a global writer lock (one writing statement at a time per
  file), so no page-order deadlock exists between LibRed's own connections. Against another user, two writers
  each holding a page the other wants do not deadlock forever: each waits out ACE's retries (about 2 s) and fails
  with 3218, as ACE itself does.
- **`.ldb`/`.laccdb` lifecycle** — measured for the `.laccdb` (created by the first shared user,
  deleted by a user that closes cleanly and finds no other; left behind by a kill and reused —
  [locking.md](../format/locking.md)); the `.ldb` of an `.mdb` is not measured. LibRed follows the
  `.laccdb` lifecycle on Windows (a shared open creates or reuses it, the last close deletes it when alone, an
  exclusive open creates none), and takes the `.ldb` beside an `.mdb` the same way, unmeasured. When the lock file
  cannot be created or opened it carries on unregistered — what ACE does then is not measured.
