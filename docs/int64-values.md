# Int64 values through OLE DB, ODBC and DAO

How a 64-bit integer gets into and out of an Access database through each of ACE's three interfaces, which
bindings work, which fail, and where the engine versions differ. Everything here was measured; the engines and
the method are at the end.

**Short version.**

- **Write** with **`Numeric`**: `OleDbType.Numeric` (131) over OLE DB, `OdbcType.Numeric` (7) over ODBC. It
  carries the whole Int64 range exactly on every engine measured. Through DAO, pass a `decimal` (`VT_DECIMAL`)
  or the value's text to an undeclared parameter.
- **Never bind it as an Int64.** `DbType.Int64`, `OleDbType.BigInt`, `OdbcType.BigInt` and an undeclared .NET
  `long` (which binds as BigInt) are refused by ODBC on every engine and by OLE DB on Microsoft 365. Only ACE 2010's
  OLE DB provider takes them, and it carries them as text.
- **Never use `Double`** for values that matter. It is exact only up to 2⁵³ and overflows a BIGINT column near ±2⁶³.
- **Store** in a `BIGINT` (Large Number) column only where every reader is ACE 16+ and is not ODBC. Otherwise
  store in **`DECIMAL(20,0)`**, which every engine and interface reads and writes exactly. EFCore.Jet maps `long`
  to `decimal(20,0)` and binds `Numeric` for exactly these reasons (`JetLongTypeMapping`).
- **Reading a BIGINT column through ODBC** returns its 8 bytes as a 4-character string. Select `CStr(col)`
  instead.
- **Comparing a parameter against a BIGINT column through OLE DB** never matches with an exact binding. Compare
  `CStr(col) = ?` instead.
- **`CDec` cannot be used in a query** through any interface: ACE reports *"Wrong number of arguments used with
  function in query expression"*.

## Engines measured

| | Microsoft 365 (x64) | ACE 2010 (x86) | ACE 2016 redistributable |
| --- | --- | --- | --- |
| OLE DB | `Microsoft.ACE.OLEDB.16.0` | `Microsoft.ACE.OLEDB.12.0` | CI only, see below |
| ODBC | `ACEODBC.DLL` 16.00 | `ACEODBC.DLL` 12.00 | not measured |
| DAO | `DAO.DBEngine.120`, version 16.0 | `DAO.DBEngine.120`, version 14.0 | not measured |
| `BIGINT` column | yes | no | no |

The ODBC driver reports the *file's* version as `ServerVersion`, not its own: the same driver says 12.00 on a
file ACE 12 wrote and 16.00 on one ACE 16 raised to create a BIGINT column.

ACE 2016 is known only from CI's 2016 redistributable leg (`LibRedAccess (2016)`, `ParameterDbTypeAccessTests`).
There, a Boolean bound as `DbType.Int64` through OLE DB is accepted and carried as the text `"1"`/`"0"`, exactly
as on ACE 2010. The rest of this document's ACE 2016 behaviour is unmeasured. The 2016 redistributable has no
BIGINT.

The values used throughout were 5, -1, 5 000 000 000, 9 007 199 254 740 993 (2⁵³ + 1, the first integer a
Double cannot hold), `long.MaxValue` and `long.MinValue`.

## SQL literals

A literal works everywhere, through all three interfaces and on both engines:

| Target column | Result |
| --- | --- |
| `BIGINT` | exact, full range |
| `DECIMAL(20,0)` | exact, full range |
| `TEXT` | exact, full range |
| `DOUBLE` | rounded past 2⁵³ (`CStr` reads `9.00719925474099E+15`) |
| `LONG` | type mismatch past Int32 |
| `CURRENCY` | type mismatch past ±922 337 203 685 477 |

## Writing through a parameter

"Exact" means the stored value, read back with `CStr`, equals the value sent. The `LONG`, `CURRENCY` and `DOUBLE`
columns behave as for literals in every row that works at all: overflow (or a type mismatch) past their range,
and rounding past 2⁵³ in a Double. The table therefore shows the `BIGINT` and `DECIMAL(20,0)` targets.

### OLE DB

| Binding | Microsoft 365 | ACE 2010 |
| --- | --- | --- |
| `DbType.Int64`, `OleDbType.BigInt`, undeclared `long` | **refused** by the provider before ACE runs: *"data value could not be converted for reasons other than sign mismatch or data overflow"*, into every column type, zero included | exact; carried as **text** (`SELECT ?` returns `String`) |
| `OleDbType.Numeric` (with or without precision 20, scale 0) | exact, BIGINT and DECIMAL | exact |
| `OleDbType.Decimal`, `OleDbType.Variant` | exact | exact |
| `OleDbType.VarWChar` holding the value's text | exact | exact |
| `OleDbType.Double` | rounded past 2⁵³; **overflow** into BIGINT at ±2⁶³ | rounded past 2⁵³ |

### ODBC

| Binding | Microsoft 365 | ACE 2010 |
| --- | --- | --- |
| `DbType.Int64`, `OdbcType.BigInt`, undeclared `long` | **refused**: `HYC00` *"Optional feature not implemented"* | **refused**, the same |
| `OdbcType.Numeric` (with or without precision 20, scale 0) | exact, BIGINT and DECIMAL | exact |
| `OdbcType.Decimal` | exact, except see below | exact, except see below |
| `OdbcType.NVarChar` holding the value's text | exact | exact |
| `OdbcType.Double` | rounded past 2⁵³; overflow into BIGINT at ±2⁶³ | rounded past 2⁵³ |

`OdbcType.Decimal` failed once on each engine, with `07006` *"Restricted data type attribute violation"*: on the
value 5, the first value written with that binding (into BIGINT on Microsoft 365, into `DECIMAL(20,0)` on ACE
2010). Every later value with the same binding was stored exactly. Whether the value or the first use is the
trigger was not isolated, so prefer `Numeric`.

### DAO

A DAO parameter's type comes from the query's `PARAMETERS` clause. An undeclared parameter is **Text**
(`Parameter.Type` = 10), so whatever is passed reaches the engine as text. The value goes in as a VARIANT: a .NET
`long` arrives as `VT_I8`, a `decimal` as `VT_DECIMAL`, a `double` as `VT_R8`, a string as `VT_BSTR`.

| Declaration and value | Microsoft 365 (DAO 16.0) | ACE 2010 (DAO 14.0) |
| --- | --- | --- |
| any `VT_I8` value, any declaration | accepted, converted as below | **refused**: *"Insufficient memory to continue the execution of the program"* |
| undeclared, `VT_I8` / `VT_DECIMAL` / `VT_BSTR` | exact | exact (`VT_I8` refused, above) |
| `PARAMETERS p TEXT(30)`, the same | exact | exact |
| `PARAMETERS p BIGINT`, any value | **stores a value unrelated to the parameter** (`23644185811025996`, `16044266946035752`, `19422134174810188` in different runs), whatever is passed, True included | no BIGINT |
| `PARAMETERS p DECIMAL` | *"Syntax error in PARAMETER clause"* | the same |
| `PARAMETERS p LONG` | exact within Int32, then *"Data type conversion error"* | the same |
| `PARAMETERS p DOUBLE`, or any `VT_R8` | rounded past 2⁵³; overflow into BIGINT at ±2⁶³ | rounded past 2⁵³ |
| `PARAMETERS p CURRENCY` into BIGINT | **wrong**: 5 stores 5, -1 stores **-2**, 5 000 000 000 stores **5 000 000 001**; conversion error past Currency's range | no BIGINT |
| `PARAMETERS p CURRENCY` into `DECIMAL(20,0)` | exact within Currency's range, then conversion error | the same |

## Comparing a parameter against a column

`WHERE column = ?` against a row holding the value; 1 means the row was found. Bindings that are refused for
writing are refused here too.

| Binding | `BIGINT` column | `DECIMAL(20,0)` | `DOUBLE` | `TEXT` |
| --- | --- | --- | --- | --- |
| OLE DB `Numeric`, `Decimal`, `Variant`, `VarWChar` | **0, never found** | 1 | 1 | 1 |
| OLE DB `Double` | 1 (as Doubles, so neighbours past 2⁵³ match too) | 0 past 2⁵³ | 1 | 0 past 2⁵³ |
| ODBC `Numeric` | **1** | 1 | 0 past 2⁵³ | 1 |
| ODBC `Decimal`, `NVarChar` | 0, never found | 1 | 1 | 1 |
| ODBC `Double` | 1 (as Doubles) | 0 past 2⁵³ | 1 | 0 past 2⁵³ |
| DAO undeclared (`VT_I8`, `VT_DECIMAL`, `VT_BSTR`) | 0, never found | 1 | 1 | — |
| DAO `PARAMETERS p BIGINT` | 0 | 0 | 0 | — |
| DAO `PARAMETERS p DOUBLE` | 1 (as Doubles) | 0 past 2⁵³ | 1 | — |
| DAO `PARAMETERS p LONG` | 1 within Int32, then conversion error | the same | the same | — |
| DAO `PARAMETERS p TEXT(30)` | type mismatch | type mismatch | type mismatch | — |

BIGINT and `TEXT` columns were measured on Microsoft 365 only. `DECIMAL(20,0)` and `DOUBLE` gave the same answers
on ACE 2010, plus the ACE 2010 OLE DB `Int64`/`BigInt`/undeclared bindings, which found the row (1) in all three.

**The workaround against BIGINT** is to compare its text: `WHERE CStr(col) = ?` with a `Numeric`, `Decimal`,
`Variant` or text binding found every row through OLE DB, and through ODBC with every binding that is accepted at
all. `WHERE col = CDec(?)` and `WHERE CDec(col) = ?` fail as described at the top.

## Reading

| Column | OLE DB | ODBC | DAO (`Field.Value`) |
| --- | --- | --- | --- |
| `BIGINT` | `Int64`, exact; `GetInt64` works | **`String` of 4 characters: the 8 stored bytes as UTF-16** (5 reads `"\u0005\0\0\0"`); `GetInt64` throws. Also for `col + 0` | `Int64`, exact (field type 16) |
| `CStr(bigint)` | `String`, exact | `String`, exact; `GetInt64` parses it | `String`, exact |
| `DECIMAL(20,0)` | `Decimal`, exact; **`GetInt64` throws** *"Specified cast is not valid"* | `Decimal`, exact; `GetInt64` works | `Decimal`, exact (field type 20) |
| `DOUBLE` | `Double`, 2⁵³ + 1 reads 9 007 199 254 740 992 | the same; `GetInt64` throws | the same (field type 7) |

The ODBC driver's description of a BIGINT column, and the matching OLE DB and DAO metadata, are in the
driver footnote of
[`src/LibRed/docs/format/data-types.md`](../src/LibRed/docs/format/data-types.md#footnote--what-the-drivers-know-about-bigint-and-datetime2).

**`SELECT ?` with a `Numeric` parameter, over ODBC,** returns a `Decimal` that has nothing to do with the value
(`0.0000000000000000000000580293`, `35406336773885454357985060753`, different from run to run). Stored values and
comparisons are unaffected. Only the parameter echoed back as a result column is garbage.

## What this means for EFCore.Jet and LibRed

- `JetLongTypeMapping` maps `long` to `decimal(20,0)` and rebinds every `long` parameter as `Numeric` (ODBC 7,
  OLE DB 131) in `ConfigureParameter`. By the tables above that is the one combination that is exact through both
  drivers on every engine measured. `JetDecimalTypeMapping` binds `Numeric` the same way.
- `ParameterDbTypeAccessTests` binds `DbType.Int64` directly on purpose, to pin what OLE DB does with each DbType.
  That is the one place the engines disagree: Microsoft 365 refuses it, while ACE 2010 and the 2016 redistributable
  carry it as text. A Boolean bound that way becomes `"1"`/`"0"`, like the other DbTypes Access has no column for.
- LibRed reads and writes the file directly, so none of the driver defects above apply to it. They matter only
  when its output is cross-checked against ACE.

## Method

A standalone C# program made a copy of the Northwind fixture per interface. It created the tables and seeded
rows through OLE DB, since ODBC's and DAO's DDL accept no `DECIMAL` precision. Through the interface under test
it then:

1. wrote each value as a literal and through each binding into `BIGINT` (where supported), `DECIMAL(20,0)`,
   `DOUBLE`, `LONG`, `TEXT(30)` and `CURRENCY` columns, and read each back with `CStr`;
2. compared each binding against the seeded rows;
3. read the seeded rows back through the interface's own reader.

It ran as an x64 process (Microsoft 365) and an x86 process (ACE 2010) on the same machine.
