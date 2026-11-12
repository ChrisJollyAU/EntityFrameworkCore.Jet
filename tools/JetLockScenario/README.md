# jetlockscenario

Drives the real Access engine (ACE) over OLE DB one command at a time, so a person can run two copies side by side in
two consoles, interleave steps by hand, and capture how ACE places byte-range locks with Process Monitor. Decode the
capture with [`tools/JetLockTrace`](../JetLockTrace/README.md). Windows only; needs an ACE OLE DB provider
(`Microsoft.ACE.OLEDB.16.0`, else `12.0`).

**Work on a copy.** ACE writes to any file it opens. The tool refuses a path containing `JetProviderExceptionTests`
(where the Northwind fixture lives).

## Build and run

x64 by default; the process bitness must match the installed ACE, so build x86 when that is what you have.

```
dotnet build tools/JetLockScenario                      # x64  -> bin\x64\Debug\net11.0-windows7.0\
dotnet build tools/JetLockScenario -p:Platform=x86      # x86  -> bin\x86\Debug\net11.0-windows7.0\

jetlockscenario <database> [--exclusive] [--read-only] [--locking page|row] [--name <label>]
```

| Option | |
|---|---|
| `--exclusive` | `Mode=Share Exclusive`. |
| `--read-only` | `Mode=Read`. |
| `--locking page\|row` | `Jet OLEDB:Database Locking Mode` 0 (page) or 1 (row). ACE honours it for the first opener of the file only. |
| `--name <label>` | Names this copy in the markers. Default: the process ID. |

It opens **one** `OleDbConnection` and keeps it for the whole session, and prints the provider, the exact connection
string and the process ID. Commands are read from stdin, one per line.

## Commands

| Command | |
|---|---|
| `setup` | Create `LockT (Id AUTOINCREMENT PRIMARY KEY, Grp LONG, Txt TEXT(50), Notes MEMO)` with an index on `Grp` and 200 rows of ~3000-character memos. Skipped if the table exists. |
| `select [table]` | `SELECT *`, reading every row and every column (default `LockT`); prints the row count. |
| `memo <id>` | Read `Notes` of one row. |
| `update <id>` | `UPDATE LockT SET Txt = ... WHERE Id = <id>`. |
| `insert [n]` | `n` single-row INSERTs (default 1); prints `@@IDENTITY`. |
| `delete <id>` | Delete one row. |
| `begin` / `commit` / `rollback` | An `OleDbTransaction`; later commands run inside it until committed or rolled back. |
| `ddl-index` | `CREATE INDEX` on `LockT(Txt)`. |
| `ddl-alter` | `ALTER TABLE LockT ADD COLUMN` (a new column each time). |
| `ddl-drop` | Create `LockScratch` if absent, then `DROP TABLE` it. |
| `sql <statement>` | Any SQL: a reader when it starts with `SELECT`, otherwise `ExecuteNonQuery`. |
| `idle <seconds>` | Sleep. |
| `mark <label>` | Emit a marker only. |
| `kill` | Terminate the process at once, closing nothing: a crash, mid-transaction if one is open. |
| `close` / `quit` | Close the connection and exit. |
| `help` | List the commands. |

Every command prints what it ran and the time it took. An error prints the full exception text and, for an
`OleDbException`, each error's `NativeError`, `SQLState`, `Source` and message and the HRESULT, and does not end the
session: the exact ACE text on a lock conflict is the thing being measured.

## Markers

Before and after every command (not `help`) the tool probes, with `File.Exists`, a file that does not exist:

```
<database full path>.mark.<name>.<seq>.<begin|end>.<command, non-filename characters as _>
```

ProcMon records it as a `CreateFile`/`QueryOpen` with `NAME NOT FOUND` on a path containing the database file name, so
it lands in the same capture as the locks. Nothing is created. `JetLockTrace` turns these rows into separators, and
with `--held` prints the locks still held at each `end` marker. After `kill` there is a `begin` marker and no `end`.

## ProcMon

Filter **Path contains** the database file name (that catches the `.laccdb` lock file and the markers), and include all
processes, or only `jetlockscenario.exe`. Enable **Drop Filtered Events**, run the scenario, then **File > Save >
Comma-Separated Values** with the default columns.

## Worked example: a reader and a writer

Copy a fixture somewhere outside `JetProviderExceptionTests`, start ProcMon with the filter above, then:

```
console 1:  jetlockscenario C:\work\scn.accdb --name A
console 2:  jetlockscenario C:\work\scn.accdb --name B
```

```
A> setup
A> begin
A> update 5          # A now holds whatever an uncommitted update holds
B> select           # does B wait, fail, or read? the error text is printed verbatim
B> update 5         # the same row from the other connection: the conflict, with ACE's own message
A> commit
B> update 5         # now succeeds
A> quit
B> quit
```

Stop the capture, save it as `scn.CSV`, then:

```
dotnet run --project tools/JetLockTrace -- scn.CSV --locks-only --canonical      # step by step
dotnet run --project tools/JetLockTrace -- scn.CSV --held                        # locks still held after each step
dotnet run --project tools/JetLockTrace -- scn.CSV --process jetlockscenario.exe --held
```
