using System.Data.OleDb;
using System.Text.RegularExpressions;
using LibRed;
using Xunit;

namespace LibRed.Engine.Tests;

/// <summary>
/// ACE validates a CHECK when the constraint is created, in CREATE TABLE and ALTER TABLE ADD CONSTRAINT alike, not
/// when a row first meets it: an expression that does not parse, that names a column the table does not have — in
/// CREATE TABLE, one not declared before the constraint — even in a branch that never runs, that uses an aggregate,
/// or whose subquery reads a table that does not exist, is refused with "Syntax error in CHECK constraint clause.",
/// leaving nothing behind. LibRed refuses the same statements with the same message. A column-level CHECK is not ACE
/// syntax at all.
/// </summary>
[Collection(AceCollection.Name)]
public class CheckConstraintCreationAccessTests(ITestOutputHelper output)
{
    [Theory]
    // Accepted, and enforced as ACE enforces them.
    [InlineData("CREATE TABLE T (A LONG, CONSTRAINT ck CHECK (A > 0))", "INSERT INTO T VALUES (1)", "INSERT INTO T VALUES (-1)")]
    [InlineData("CREATE TABLE T (A LONG, CONSTRAINT ck CHECK ((SELECT COUNT(*) FROM Employees) > 0))", "INSERT INTO T VALUES (1)")]
    [InlineData("CREATE TABLE T (A LONG, CONSTRAINT ck CHECK (A IN (SELECT EmployeeID FROM Employees)))", "INSERT INTO T VALUES (1)", "INSERT INTO T VALUES (999)")]
    // A name the row does not have: unknown, declared after the constraint, another table's, or bracketed like a
    // parameter — and one in an IIf branch that never runs.
    [InlineData("CREATE TABLE T (B LONG, CONSTRAINT ck CHECK (Z > 0))")]
    [InlineData("CREATE TABLE T (CONSTRAINT ck CHECK (B > 0), B LONG)")]
    [InlineData("CREATE TABLE T (A LONG, CONSTRAINT ck CHECK (A > Employees.EmployeeID))")]
    [InlineData("CREATE TABLE T (A LONG, CONSTRAINT ck CHECK (A > [Enter value]))")]
    [InlineData("CREATE TABLE T (A LONG, CONSTRAINT ck CHECK (IIF(True, 1, Z) = 1))")]
    // An expression that does not parse.
    [InlineData("CREATE TABLE T (A LONG, CONSTRAINT ck CHECK (A >))")]
    [InlineData("CREATE TABLE T (ID LONG PRIMARY KEY, Amount DOUBLE, CONSTRAINT CheckAmount CHECK (Amount > 0 unexpected))")]
    // An aggregate, and a subquery over a table that does not exist.
    [InlineData("CREATE TABLE T (A LONG, CONSTRAINT ck CHECK (Sum(A) > 0))")]
    [InlineData("CREATE TABLE T (A LONG, CONSTRAINT ck CHECK ((SELECT COUNT(*) FROM Nope) > 0))")]
    // The same when the constraint is added to an existing table.
    [InlineData("CREATE TABLE T (A LONG)", "ALTER TABLE T ADD CONSTRAINT ck CHECK (Z > 0)")]
    [InlineData("CREATE TABLE T (A LONG)", "ALTER TABLE T ADD CONSTRAINT ck CHECK (A >)")]
    public void Libred_validates_a_check_when_it_is_created_as_ace_does(params string[] steps)
    {
        (string ace, string libred) = Outcomes(steps, "T");
        Assert.Equal(ace, libred);
    }

    [Theory]
    [InlineData("CREATE TABLE T (A LONG CHECK (A > 0))")]
    [InlineData("CREATE TABLE T (A LONG CONSTRAINT ck CHECK (A > 0))")]
    [InlineData("CREATE TABLE T (B LONG, A LONG CONSTRAINT ck CHECK (B > 0))")]
    [InlineData("CREATE TABLE T (A LONG CONSTRAINT ck CHECK (A IN (SELECT EmployeeID FROM Employees)))")]
    [InlineData("CREATE TABLE `T` (`Id` INTEGER PRIMARY KEY, `Age` INTEGER CONSTRAINT `CK_Age` CHECK (`Age` > 0 AND `Age` < 200))")]
    [InlineData("CREATE TABLE T (A LONG)", "ALTER TABLE T ADD COLUMN B LONG CONSTRAINT ck CHECK (B > 0)")]
    public void Libred_refuses_a_column_level_check_as_ace_does(params string[] steps)
    {
        (string ace, string libred) = Outcomes(steps, "T");
        string refused = $"refused at step {steps.Length}: ";
        Assert.StartsWith(refused + "Syntax error in field definition.", ace);
        Assert.StartsWith(refused, libred);
        Assert.Equal(ace.Split(" | ")[1], libred.Split(" | ")[1]);
    }

    /// <summary>What a CHECK reads is held: ACE refuses to drop a table another table's check reads, or to drop or
    /// alter a column any check reads, naming the earliest-created such check (twice). LibRed finds them from the
    /// reads each check's MSysObjects row records.</summary>
    [Theory]
    // Another table's check reads Src.Col: the table and that column are held, Src.Other is not.
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "DROP TABLE Src")]
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A < (SELECT MAX(Col) FROM Src)))", "DROP TABLE Src")]
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG)", "ALTER TABLE Owner ADD CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src))", "DROP TABLE Src")]
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "ALTER TABLE Src DROP COLUMN Col")]
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A < (SELECT MAX(Col) FROM Src)))", "ALTER TABLE Src DROP COLUMN Col")]
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "ALTER TABLE Src DROP COLUMN Other")]
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "ALTER TABLE Src ALTER COLUMN Col TEXT(10)")]
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "ALTER TABLE Src ALTER COLUMN Col LONG")]
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "ALTER TABLE Src ALTER COLUMN Col COUNTER")]
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "ALTER TABLE Src ALTER COLUMN Other TEXT(10)")]
    // Read for no column: the table is held, none of its columns.
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK ((SELECT COUNT(*) FROM Src) < 10))", "DROP TABLE Src")]
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK ((SELECT COUNT(*) FROM Src) < 10))", "ALTER TABLE Src DROP COLUMN Col")]
    // The check's own table: its read columns are held, the table itself is not.
    [InlineData("CREATE TABLE Owner (A LONG, B LONG, CONSTRAINT chkX CHECK (A < B))", "ALTER TABLE Owner DROP COLUMN B")]
    [InlineData("CREATE TABLE Owner (A LONG, B LONG, C LONG, CONSTRAINT chkX CHECK (A < B))", "ALTER TABLE Owner DROP COLUMN C")]
    [InlineData("CREATE TABLE Owner (A LONG, B LONG, CONSTRAINT chkX CHECK (A < B))", "ALTER TABLE Owner ALTER COLUMN B TEXT(10)")]
    [InlineData("CREATE TABLE Owner (A LONG, B LONG, CONSTRAINT chkX CHECK (A < B))", "DROP TABLE Owner")]
    // Released once the check, or its table, is gone.
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "ALTER TABLE Owner DROP CONSTRAINT chkX", "DROP TABLE Src")]
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "DROP TABLE Owner", "DROP TABLE Src")]
    // Several checks: the earliest-created is named, whichever table it is on and whatever its name.
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "CREATE TABLE Owner2 (A LONG, CONSTRAINT chkY CHECK (A IN (SELECT Col FROM Src)))", "DROP TABLE Src")]
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner2 (A LONG, CONSTRAINT chkY CHECK (A IN (SELECT Col FROM Src)))", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "DROP TABLE Src")]
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner2 (A LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "ALTER TABLE Owner2 ADD CONSTRAINT chkY CHECK (A IN (SELECT Col FROM Src))", "DROP TABLE Src")]
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkZ CHECK (A > 0))", "ALTER TABLE Owner ADD CONSTRAINT chkY CHECK (A IN (SELECT Col FROM Src))", "ALTER TABLE Owner ADD CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src))", "DROP TABLE Src")]
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Other FROM Src)))", "CREATE TABLE Owner2 (A LONG, CONSTRAINT chkY CHECK (A IN (SELECT Col FROM Src)))", "ALTER TABLE Src DROP COLUMN Col")]
    // A relationship's refusal comes first.
    [InlineData("CREATE TABLE Src (Col LONG PRIMARY KEY, Other LONG)", "CREATE TABLE Owner (A LONG REFERENCES Src (Col), CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "ALTER TABLE Src ALTER COLUMN Col TEXT(10)")]
    // A default is not held; a re-declaration carrying NOT NULL or a DEFAULT is.
    [InlineData("CREATE TABLE Src (Col LONG DEFAULT 1, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "ALTER TABLE Src ALTER COLUMN Col SET DEFAULT 5")]
    [InlineData("CREATE TABLE Src (Col LONG DEFAULT 1, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "ALTER TABLE Src ALTER COLUMN Col DROP DEFAULT")]
    [InlineData("CREATE TABLE Owner (A LONG, B LONG DEFAULT 1, CONSTRAINT chkX CHECK (A < B))", "ALTER TABLE Owner ALTER COLUMN B SET DEFAULT 5", "ALTER TABLE Owner ALTER COLUMN B DROP DEFAULT")]
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "ALTER TABLE Src ALTER COLUMN Col LONG NOT NULL")]
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "ALTER TABLE Src ALTER COLUMN Col LONG DEFAULT 5")]
    // Renames (ACE through DAO): what a check reads, and any table with a check of its own, keep their names —
    // to the same name too; a column no check reads does not.
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "ALTER TABLE Src RENAME TO Src2")]
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK ((SELECT COUNT(*) FROM Src) < 10))", "ALTER TABLE Src RENAME TO Src2")]
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "ALTER TABLE Src RENAME TO Src")]
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "ALTER TABLE Src RENAME TO Owner")]
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "ALTER TABLE Src RENAME COLUMN Col TO Col2")]
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "ALTER TABLE Src RENAME COLUMN Col TO Col")]
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "ALTER TABLE Src RENAME COLUMN Other TO Other2")]
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "ALTER TABLE Src RENAME COLUMN Other TO OTHER")]
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "ALTER TABLE Owner RENAME TO Owner2")]
    [InlineData("CREATE TABLE Owner (A LONG, B LONG, C LONG, CONSTRAINT chkX CHECK (A < B))", "ALTER TABLE Owner RENAME COLUMN B TO B2")]
    [InlineData("CREATE TABLE Owner (A LONG, B LONG, C LONG, CONSTRAINT chkX CHECK (A < B))", "ALTER TABLE Owner RENAME COLUMN C TO C2")]
    [InlineData("CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (1 > 0))", "ALTER TABLE Owner RENAME TO Owner2")]
    public void Libred_holds_what_a_check_reads_as_ace_does(params string[] steps)
    {
        (string ace, string libred) = Outcomes(steps, "Src", "Owner", "Owner2");
        Assert.Equal(ace, libred);
    }

    /// <summary>Where another refusal comes ahead of the check's, LibRed gives that one too. Its own messages for
    /// these differ from ACE's, so only which refusal is asserted: not the check's, at the same step.</summary>
    [Theory]
    // An indexed column: the index refusal first.
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE INDEX ix ON Src (Col)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "ALTER TABLE Src DROP COLUMN Col")]
    // A relationship's parent: the relationship refusal first.
    [InlineData("CREATE TABLE Src (Col LONG PRIMARY KEY, Other LONG)", "CREATE TABLE Child (P LONG REFERENCES Src (Col))", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "DROP TABLE Src")]
    // A column renamed to a name another column has: that refusal first.
    [InlineData("CREATE TABLE Src (Col LONG, Other LONG)", "CREATE TABLE Owner (A LONG, CONSTRAINT chkX CHECK (A IN (SELECT Col FROM Src)))", "ALTER TABLE Src RENAME COLUMN Col TO Other")]
    public void Libred_gives_the_refusal_ace_gives_ahead_of_a_checks(params string[] steps)
    {
        (string ace, string libred) = Outcomes(steps, "Src", "Owner");
        string refused = $"refused at step {steps.Length}: ";
        Assert.StartsWith(refused, ace);
        Assert.StartsWith(refused, libred);
        Assert.DoesNotContain("referenced by constraint", ace.Split(" | ")[0]);
        Assert.DoesNotContain("referenced by constraint", libred.Split(" | ")[0]);
        Assert.Equal(ace[(ace.IndexOf(" | ", StringComparison.Ordinal) + 3)..], libred[(libred.IndexOf(" | ", StringComparison.Ordinal) + 3)..]);
    }

    private (string Ace, string Libred) Outcomes(string[] steps, params string[] tables)
    {
        string northwind = Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb");
        string ace = TemporaryDatabase.CopyPath(northwind, "chkcreate-ace-");
        string libred = TemporaryDatabase.CopyPath(northwind, "chkcreate-lib-");
        try
        {
            string aceOutcome = Run(steps, sql =>
            {
                if (Rename.Match(sql) is { Success: true } rename)
                {
                    RenameThroughDao(ace, rename);
                    return;
                }
                using OleDbConnection connection = AceTestDatabase.Open(ace);
                using OleDbCommand command = connection.CreateCommand();
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }) + " | " + Left(ace, tables);
            string libredOutcome = Run(steps, sql =>
            {
                using var db = JetDatabase.Open(libred, readOnly: false);
                new QueryEngine(db).ExecuteNonQuery(sql);
            }) + " | " + Left(libred, tables);

            output.WriteLine($"ACE    {aceOutcome}\nLibRed {libredOutcome}");
            return (aceOutcome, libredOutcome);
        }
        finally
        {
            TemporaryDatabase.Delete(ace);
            TemporaryDatabase.Delete(libred);
        }
    }

    /// <summary>LibRed's <c>ALTER TABLE … RENAME [COLUMN …] TO …</c>, which Access SQL does not have.</summary>
    private static readonly Regex Rename = new(@"^ALTER TABLE (\w+) RENAME (?:COLUMN (\w+) )?TO (\w+)$");

    /// <summary>The rename ACE makes through DAO's <c>TableDef.Name</c> or <c>Field.Name</c>.</summary>
    private static void RenameThroughDao(string path, Match rename)
    {
        dynamic engine = AceTestDatabase.CreateDaoEngine() ?? throw new InvalidOperationException("DAO is not registered.");
        dynamic database = engine.OpenDatabase(path);
        try
        {
            dynamic table = database.TableDefs[rename.Groups[1].Value];
            if (rename.Groups[2].Success) table.Fields[rename.Groups[2].Value].Name = rename.Groups[3].Value;
            else table.Name = rename.Groups[3].Value;
        }
        finally
        {
            database.Close();
        }
    }

    /// <summary>Runs the steps until one is refused: which step, and the refusal's message.</summary>
    private static string Run(string[] steps, Action<string> execute)
    {
        for (int step = 0; step < steps.Length; step++)
        {
            try { execute(steps[step]); }
            catch (Exception e) { return $"refused at step {step + 1}: {e.GetBaseException().Message}"; }
        }
        return "accepted";
    }

    /// <summary>What the statements left: each named table's columns and CHECK constraints, or its absence.</summary>
    private static string Left(string path, string[] tables)
    {
        using var db = JetDatabase.Open(path);
        return string.Join(" | ", tables.Select(name => db.Catalog.FindTable(name) is not { } table ? $"no table {name}"
            : $"{name} [{string.Join(",", table.Columns.Select(c => c.Name))}] checks [{string.Join(", ", table.CheckConstraints.Select(c => $"{c.Name}={c.Expression}"))}]"));
    }
}
