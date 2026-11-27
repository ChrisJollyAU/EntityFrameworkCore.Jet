# jetlocktrace

Decodes the extended byte-range locks Jet/ACE places while working with a database, from a Process Monitor
trace. Turns raw offsets into pages, users and lock kinds so a trace can be read (and diffed) directly.

```
dotnet run --project tools/JetLockTrace -- trace.csv
```

| Option | |
|---|---|
| `--page-size <n>` | Database page size. Default 4096 (Jet 4 / ACE); 2048 for Jet 3. |
| `--canonical` | Omit the time, process and file columns, so traces diff cleanly. Failed operations keep their result text, and lock rows keep `[failimm]`/`[wait]`, because both are behaviour. |
| `--file <substr>` | Only rows whose path contains `<substr>`. |
| `--process <name-or-pid>` | Only rows from that process: a case-insensitive name match, or an exact PID. |
| `--locks-only` | Only lock/unlock rows; drop reads and writes, so two traces diff without cache-driven I/O noise. |
| `--held` | Replay the locks in order instead of listing rows (it replaces the row output; `--file`, `--process` and `--locks-only` still filter what is replayed). At each scenario `end` marker it prints the locks held at that moment, per process, under the marker heading. At the end it prints, per process (PID) and file, the locks still held, decoded as usual, then any unlock that matched no held lock. Locks and unlocks are paired on file, offset and length. Rows whose result is not `SUCCESS` are ignored: a failed lock was never taken and a failed unlock released nothing. |
| `--compare <a> <b>` | A different mode: see [Comparing two engines statement by statement](#comparing-two-engines-statement-by-statement). Replaces `--file` and `--held`; takes `--process` and `--out` (default name `<trace>.compare.txt`). |
| *(markers)* | Not an option: see [Scenario markers](#scenario-markers). |
| `--out [path]` | Write to a file rather than stdout. With no path, writes next to the CSV as `<trace>.locks.txt`, or `<trace>.canonical.txt` under `--canonical`. |

Output goes to stdout by default. `--out` avoids shell redirection — worth using, because `dotnet run`
can emit build output onto stdout and contaminate a redirected file (pass `--no-build` if you do redirect).
The "writing to…" confirmation goes to stderr, so it never ends up in the output.

Comparing two scenarios:

```
jetlocktrace open.CSV   --canonical --out
jetlocktrace update.CSV --canonical --out
diff open.canonical.txt update.canonical.txt
```

Without `--canonical`, each line is prefixed with the time of day and `process(pid)` when the CSV has those columns.

**Every lock is OS-exclusive.** ACE takes only `LockFileEx` exclusive, fail-immediately locks; Jet's "shared" and
"exclusive" are different byte ranges, not different lock modes (see [What it decodes](#what-it-decodes)). The one
thing a line is marked `!!` for is an OS-shared lock (ProcMon `Exclusive: False`), which is not expected.
`FailImmediately` is shown as `[failimm]` / `[wait]`.

A result other than `SUCCESS` is appended to the line: `NOT GRANTED` for a lock that was refused, `RANGE NOT LOCKED`
for an unlock of nothing held.

## Live lock files: `probe` and `hold`

Two modes work on a live lock file instead of a trace. Both are **Windows only** for now (they take real
`LockFileEx` locks, the kind Access takes), neither writes to the database or the lock file, and neither creates a
lock file that is missing. They lock through LibRed.Core's `JetLockFile`, at offsets from `JetLockLayout` — the same
copy of the layout the decoder below reads with. `JetLockFile` opens the lock file as ACE does (read/write, shared for
read and write but not delete), so while `probe` or `hold` has it open, a user closing last cannot delete it and the
file stays behind until the next user's close.

```
jetlocktrace probe <database>
```

Lists who is connected. The lock file is found beside the database (`.laccdb` for `.accdb`/`.accde`, `.ldb` for
`.mdb`/`.mdw`); if there is none, it says so. For every user number 1–255 it tries a 1-byte lock on that user's
group-0 byte (`0x10000000 + user`) and on its group-1 session byte (`0x10000100 + user - 1`), and releases each at once;
a conflict on either means the user is connected. For each one it prints which of the two bytes are locked, the user's
page-0 commit slot (hex, and as the little-endian counter), and the user's lock-file record (index `user - 1`:
32-byte computer name + 32-byte security name, each NUL-terminated and space-padded). Commit slot 0, the
exclusive-mode slot, is always printed. A LibRed process with the database open shared shows up here like any ACE
user.

```
jetlocktrace hold <database> --region <1-6> (--page <n> | --user-slot <n>) [--shared] [--as-user <n>]
```

Takes one lock, reports whether it got it, holds it until Enter is pressed, then releases it — for provoking lock
conflicts in Access on purpose.

| Option | |
|---|---|
| `--region <1-6>` | The region (table below). |
| `--page <n>` | The page whose window is locked (regions 2–6). |
| `--user-slot <n>` | A group-0 user byte instead (region 1 only): 1 byte. Takes neither `--shared` nor `--as-user`. |
| `--shared` | A Jet "shared" lock: the one byte of the page at the `--as-user` number. Without it, an exclusive range: from byte 1, `256 + user` bytes long. |
| `--as-user <n>` | The user the page lock is taken as. Default **255** — a number an Access session only gets when 255 users are connected, so Access's conflict report cannot be mistaken for a real session. |

Numbers may be decimal or `0x`-prefixed hex. Exit code `2` means the lock conflicted and nothing was taken. To
hold a lock from a script, feed it stdin: `(sleep 30; echo) | jetlocktrace hold …`.

## Sampling the commit slots: `watch`

Process Monitor shows that ACE writes a page-0 commit slot but not the bytes. `watch` samples them instead: one
positional read (`RandomAccess.Read`, no buffering of our own, no memory mapping) of the requested slots per
iteration, many thousands a second, so it can land inside the ~1.5 ms window between ACE writing its slot and
writing it again. **It never writes to the database**; it opens it read-only with `FileShare.ReadWrite | Delete`.
The slot table's offset and slot size come from the database's own format (`JetFormatBase`), as in `probe`.

```
jetlocktrace watch <database> [--slots <first>[-<last>]] [--seconds <n>] [--out <path>]
```

`--slots` defaults to `0-3`; `--seconds` to 60. Transitions are held in memory and printed only after sampling
ends, so console I/O never slows the loop. Stop early with **Ctrl+C** (the report still prints) or by creating the
file `<database>.watch.stop` (checked every 10,000 samples).

The report has one line per transition: seconds since start, how long the previous value was held (microseconds
and samples that saw it), then each slot as hex and little-endian number, `*` marking the slots that changed. The
summary gives samples, samples per second, mean and worst gap between samples, and each slot's distinct values with
how often a transition entered them and their shortest and longest completed hold. Any value below 256 (`00 00`,
`01 00`) is flagged `RESERVED`. A torn read is possible in principle; what was read is reported as read.

## Capturing a trace

In Process Monitor, filter to `Path contains <your database name>` (and `Process Name is MSACCESS.EXE` for a
single-process trace), enable **Filter > Drop Filtered Events**, then **File > Save > Comma-Separated Values** with
the default columns. `Operation`, `Path` and `Detail` are required; `Time of Day`, `Process Name`, `PID` and
`Result` are used when present (they are in the default export) and optional, so older CSVs still load.

## Scenario markers

`tools/JetLockScenario` drives ACE one command at a time and, before and after each command, probes a file that does
not exist: `<database>.mark.<name>.<seq>.<begin|end>.<command>`. ProcMon records that as a `CreateFile` (or
`QueryOpen`) with `NAME NOT FOUND` on a path containing the database name, so it lands in the same capture as the
locks. This tool keeps every row whose path contains `.mark.` whatever the operation, collapses the several rows one
probe produces into one, and prints it as a separator:

```
--- A 0007 begin update_5 ---
lock   write/read  page 123    exclusive, user 1   (257)  [0x…]  [failimm]
--- A 0008 end update_5 ---
```

Markers are never dropped by the database/lock-file path filter or by `--locks-only`, and they respect `--file` and
`--process` (a marker row carries the process that made it). With `--held`, each `end` marker prints the locks held at
that moment, per process, so you can see which locks persist after each step:

```
jetlocktrace both.CSV --held --process jetlockscenario.exe
```

## Comparing two processes

To compare Access with our own engine on the same scenario, run both against identical copies of one fixture in
the same capture (or two captures), then split by process and diff:

```
jetlocktrace both.CSV --process msaccess.exe --locks-only --canonical --out access.txt
jetlocktrace both.CSV --process 4242         --locks-only --canonical --out ours.txt
diff access.txt ours.txt
```

`--locks-only` removes read/write noise, and canonical output carries no time, PID or file, so only behaviour
differs: lock kind, page, user, shared or exclusive, whether it fails immediately, and any conflict. `--held` on each process
shows what it still holds when the scenario ends, which catches locks that were taken and never released.

For traces to be comparable across experiments, always start from an *identical copy* of one fixture file —
page numbers must mean the same thing in every run.

## Comparing two engines statement by statement

Whole-run counts say two files were locked differently, not by which statements. `--compare` aligns two files of
one capture by scenario marker and reports per statement:

```
jetlocktrace wholelocks.CSV --compare wholefile-ace wholefile-libred --out wholelocks.compare.txt
```

`<a>` and `<b>` are path substrings, as `--file` takes (a row goes to each side it matches, so one substring twice
compares a file with itself). The statements are the markers' command labels (`s0123`, `open`, `close`); the same
label seen twice on a side is `label#2`. `test/LibRed.Engine.AccessTests`' `WholeFileParityProbeTest` writes
`<database>.mark.ace.…` and `<database>.mark.libred.…` markers around each statement of both engines' runs, labelled
by the statement's 1-based index and bracketing the open and the close of each session. Capture it in ProcMon
filtered to `wholefile`.

Only lock calls count, as multisets: unlocks and order are ignored. A lock is (kind, where, mode): the region (or
`user-lock/<what>` — presence probes, session bytes, connection control — as kinds of their own), the page (or the
group and bytes), and shared / exclusive / upgrade (`… REFUSED` when the lock was not granted). Each statement is one
block, every line starting with its label so a grep keeps its context:

```
=== s0001 kinds  A: 3 locks (after end 1)  B: 1 locks (after end 0)
s0001  only A     1x  user-lock/group-1-presence-probe       group 1 bytes 2..255     -
s0001  only A     1x  write/read                             page 1                   exclusive
s0001  only A     1x  write/read                             page 3                   exclusive  [after end: 1 of this side's 1]
s0001  only B     1x  write/read                             page 2                   exclusive
s0001  by kind (page numbers ignored):
s0001  only A     1x  user-lock/group-1-presence-probe         -
s0001  only A     1x  write/read                               exclusive
=== s0002 same  A: 1 locks (after end 0)  B: 1 locks (after end 0)
```

The status is `same` (identical multisets), `pages` (the same kinds and modes in the same numbers, on other pages:
the two files' page numbers drift apart during a run) or `kinds` (the kind counts differ; the second view, which
ignores `where`, is printed only then). Locks between a statement's `end` marker and the next `begin` — ACE writes and
locks about half a second after a command returns — are counted with the statement before and flagged
`[after end: n …]`, and the header gives each side's number of them. Locks before the first `begin` are the statement
`(unmarked)`; a statement with no marker on one side shows `(no marker)` there. The block list ends in a summary:
statements identical / differing only in page numbers / differing in kind counts, and lock calls per kind and mode for
each side. Statements come in side A's order, then any only side B has.

## What it decodes

Lock offsets pack three fields (the layout lives in LibRed.Core as `JetLockLayout`; the tool decodes through it):

```
offset = (region << 28) | (page << 9) | byteInWindow
```

The `<< 9` gives each page a 512-byte window in lock space. It is **not** the page size — Jet 3.5 used 2 KB pages
with the same shift, and ACE uses 4 KB. Byte 0 of a window is not used.

Every lock ACE takes is an operating-system **exclusive** lock taken without waiting, on the lock file
(`.laccdb`); none is taken on the database. Jet's "shared" and "exclusive" are told apart by which bytes are locked:

- A **shared** lock is **one byte at the holder's own user number**: `window + user`. Users share a page because
  each locks a different byte — user 1 locks `0x40026C01` where user 2 locks `0x40026C02`.
- An **exclusive** lock starts at **byte 1** and is `256 + user` bytes long: 257 for user 1, 258 for user 2, both
  from `0x2002CA01`. It covers every user's shared byte (1..255), so it conflicts with all of them, and **where it
  ends names the holder** — which is what the white paper means by "take the last two digits of the ending lock
  range".
- An **upgrade** was seen too: a user holding its own byte 1 then locks `window + 2`, 255 bytes (bytes 2..256) —
  every other user's byte. It is shown as `upgrade`.

The locks sit beyond end-of-file, so no database bytes are ever really locked; the ranges are pure semaphores.

| Region | Lock kind | Target |
|---|---|---|
| `0x1` | User | groups of 256 bytes, one byte per user (below) |
| `0x2` | Write (exclusive) / Read (shared) | data, index and long-value pages |
| `0x3` | Read / Commit — Jet 2.x only | index and long-value pages |
| `0x4` | Table-read | table header page (TBH) |
| `0x5` | Table-write | table header page |
| `0x6` | Table deny-write | table header page |

Region 1 groups (`0x10000000 + group * 256 + byte`):

| Group | What |
|---|---|
| 0 | User `n` holds byte `n` for the whole session. |
| 1 | The session byte: user `n` holds byte `n - 1` for the whole session (`0x10000100` for user 1, `0x10000101` for user 2). |
| 5 | Connection control. User `n` barring new connections (`Jet OLEDB:Connection Control` = 1) locks and releases all 256 bytes (`0x10000500`), then holds byte `n` (`0x10000501` for user 1) until it allows them again or closes. Every user connecting tries all 256 bytes and lets them go; refused, it looks for the holder (below). Shown as `connection control, whole group` and `connection control, user n byte`. |
| 6 | Tried only by a user group 5 refused: byte 0 (`0x10000600`), and when that is refused too, the byte `n` of each user number that has a record in the lock file, highest down to 1, its own included. What it is for is not known. |

A user connecting while group 5 is held (measured with user 1 connected, the newcomer user 2): refused the whole of
group 5, it locks and releases `0x10000600`, then tries the group-5 byte of each connected user, user 1's first
(`0x10000501`). That one refused, it reads that user's record, releases its own `0x101` and `0x2`, and fails with 3734
naming the record's security and computer names. Granted (the byte held is not a connected user's), it lets it go and
starts again from the whole of group 5 — ten rounds, then 3733, "an unknown user". With `0x10000600` held as well, it
is refused that too and tries no group-5 byte: it locks and releases the group-6 byte of each user number with a
record, highest down to 1, its own included. Another user's byte refused, it fails with 3734 naming that user; its own
refused, it ends the round naming no one; none refused, the next round. Ten rounds, then 3733. `0x10000600` held alone
blocks nothing: group 5 is granted and group 6 never tried. A user setting `Jet OLEDB:Connection Control` to 1 while
another already bars takes and holds nothing and reports no error, so only the first setter bars connections.

On opening, ACE first tries the whole of group 0 (`0x10000001`, 255 bytes), which succeeds only when the user is
alone, and releases it. A later user is refused there, refused on `0x100` (255 bytes), `0x100` and `0x1`, then
takes `0x101` and `0x2`. A user also locks and releases every *other* user's byte in group 0 as a presence probe:
user 1 `0x2` (254 bytes), user 2 `0x1` (1 byte) and `0x3` (253 bytes). These show as `presence probe`.

User `n`'s record in the lock file is the `n - 1`th 64-byte record (user 1 writes at offset 0, user 2 at offset 64),
and its commit slot is page 0 `0xE00 + 2 * n`.

Measured with users 1 and 2 only. The upper bound of 255 users is the white paper's.

It also decodes I/O against the database file — page numbers, sub-page field writes, and the **commit-byte
table** at page 0 `0xE00`–`0xFFF` (256 users × 2 bytes), which Access writes immediately before and after a
batch of page writes. Each slot is one little-endian 16-bit value: the bytes `00 00` (mid-write) or `01 00`
(accessed a corrupted page) with no matching user lock are what make Access declare the file suspect and demand
a repair. The idle value is `00 01` (256), and above that the slot counts the user's committed writes — see
[page-00 §2.2](../../src/LibRed/docs/format/page-00-database.md).

Region names are the Jet development team's own, from `docs/JetWhitePapers_UPDATE1/Jetlock.docx`.

## Open questions

Things the tool deliberately reports rather than explains, pending experiments:

- **What region 1 group 6 is for.** It is tried only by a user connection control refused; what holds it in ACE is
  not known.
- **The commit-slot values.** What a user writes to its page-0 commit slot, and when, is not decoded here.
- **Row-level locking mode.** How, or whether, locks are placed per row in that mode is not measured: the layout
  above is the page-level one.
- **User numbers above 2.** Only users 1 and 2 were observed; the upper bound of 255 and the shape of an upgrade by
  any user other than 1 are not measured.
