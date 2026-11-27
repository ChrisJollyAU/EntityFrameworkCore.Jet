# ACE's locks and LibRed's over one long session

[locking.md](../format/locking.md) was built up one scenario at a time: a statement, a capture, a rule. This
note records the opposite check — one long single-user session run through both engines, and every lock each
of them took compared. It says how close the two are over a whole workload, and lists what the comparison
found that the scenario-by-scenario work had not.

Measured 2026-11-26 against ACE 16 (x64, OLE DB), LibRed at `e238a179`.

## The run

`WholeFileParityProbeTest.The_whole_run_leaves_libred_and_ace_with_the_same_file` (its default case) runs the
same 479 statements — DDL, DML, transactions, stored queries, a 1000-row bulk insert — through ACE on one copy
of a database and then through LibRed on another. Both opens are shared, so each engine is user 1 of its own
lock file and the lock offsets compare directly.

One ProcMon capture covers both. The two engines run in one process, so the capture is split by path, not by
process:

```
jetlocktrace run.CSV --file wholefile-ace    --locks-only --canonical --out ace.txt
jetlocktrace run.CSV --file wholefile-libred --locks-only --canonical --out libred.txt
```

ProcMon's filter is a single rule, Path contains `wholefile-`, with no process rule.

## The comparison

Lock and unlock calls on the lock file: ACE 9092, LibRed 8302. Lock calls by kind:

| Lock | ACE | LibRed |
|---|---:|---:|
| Connect: presence probe of bytes 1..255, session byte, user byte, connection-control group | 5 each | 5 each |
| Page, exclusive | 2870 | 2542 |
| Page, shared | 24 | 27 |
| Page, shared raised to exclusive | 4 | 4 |
| Table deny-write window, exclusive (taken and let go) | 392 | 386 |
| Table write, shared byte | 392 | 386 |
| Table read, shared byte | 596 | 609 |
| Table read, whole window in one call | 191 | 0 |
| Table read, the other 255 bytes after the shared byte | 0 | 172 |
| Presence probe of bytes 2..255 of the user group, after connecting | 52 | 0 |

Every kind of lock ACE takes, LibRed takes, at the same offsets and lengths, and the five connects are
call-for-call the same. No lock in either trace was refused.

## What differs

**The design-change lock is one call in ACE and two in LibRed.** For a `CREATE TABLE`, and for a design change
to a table that already exists, ACE takes the table-read window exclusive in a single call from byte 1,
`256 + n` long. LibRed takes its shared byte and then bytes 2..256. The bytes held at the end are the same,
so the two exclude each other correctly; LibRed's form has a moment between the calls that ACE's does not.
ACE did not take the two-call form once in this run. With LibRed's 172 first halves taken out, LibRed takes
437 shared table-read bytes to ACE's 596. LibRed has since been changed to take the single call.

**ACE asks whether it is alone, and LibRed did not.** Fifty-two times after connecting, ACE locks and
releases bytes 2..255 of group 0 of the user region — every user's byte but its own. Each one sits beside a
lock of page 1, some as a single probe before the page-1 lock and some in bursts of five or six while page 1
and newly reserved pages are held. LibRed made this probe only as part of connecting. It is ACE asking whether
any other user is connected before it gives pages back: granted, freed pages go to the global free map, refused,
they are held and parked in the released-pages map at close ([page-05 §9.1](../format/page-05-usage-maps.md),
[locking.md §3](../format/locking.md)). LibRed now makes the same probe at the same points — a statement giving
back a chained long value, a rollback giving back pages, a close with pages to give back.

**ACE takes more page locks, in a different order.** ACE takes about 11% more exclusive page locks over the
run: 2870 to LibRed's 2542. Almost all of the 328 are of page 2, MSysObjects' definition page — 294 of them.
The run's first statement is a `CREATE TABLE`, and an ACE session that has added or removed a row of a system
table locks page 2 in every later statement that adds or removes rows, writes two tables or changes a table's
design, and writes it unchanged with the statement's batch: 428 of the 479 statements here, where LibRed then
locked page 2 only in the 145 that changed its bytes ([locking.md §4](../format/locking.md)). LibRed now does
the same. Within a `CREATE TABLE` the order differs too: ACE locks catalog pages 17, 7 and 8 before the table
locks on page 18 and pages 41, 10, 2 and 3 after them; LibRed takes the table locks first and then pages 2, 17,
7, 8, 3, 41, 10.

**The files part company.** In this continuous run the two files do not end the same: ACE's has 2016 pages
and LibRed's 2009, with 281 pages identical. Page numbers in the two traces therefore drift apart during the
run, and a page lock cannot be matched to its counterpart by number alone past that point.

## What this comparison cannot say

It counts over the whole run. The test put no marker in the trace between statements when this run was made,
so the table above does not line the two sequences up statement by statement. The test now writes one around each
statement, and `jetlocktrace --compare` aligns the two engines' locks by them; the page-2 locks above were
attributed that way. The shared table reads and the deny-write pair are not attributed.

Timing is left out on purpose: LibRed lets a statement's locks go as it returns and ACE about half a second
later or at its next statement ([locking.md](../format/locking.md)), so the unlocks fall in different places
in the two traces without either engine having locked anything different.

The run has one user. Nothing here measures a refused lock.
