using System.Data.OleDb;
using System.Globalization;
using Xunit;

namespace LibRed.Engine.Tests;

/// <summary>
/// UPDATE and DELETE, of one table or through a join, take a comparison an index answers as the SELECT with the same
/// WHERE takes it — a Yes/No column by truth, text as the number, Currency or date column stores it, a number as a GUID
/// or binary column stores it, a value the column cannot hold as no row — and a column written with or without its
/// table, over a join or beside a correlation, is taken as ACE takes it.
/// </summary>
/// <remarks>
/// Each case is one statement over a fresh table T, indexed on V; NJ holds the IDs 1 to 5 the joins meet, NK pairs
/// them for a bracketed join, and UY has an index on its Yes/No column Y. The setup and the statement run through ACE
/// and through LibRed on copies of Northwind: every statement's outcome — the rows a query returns or a write affected,
/// or its refusal; the two engines word a refusal differently — and then table T as each engine reads its own file
/// must agree.
/// </remarks>
[Collection(AceCollection.Name)]
public class StatementSeekAccessTests : TempDatabaseTest
{
    private static readonly string[] Common =
    [
        "CREATE TABLE NJ (ID LONG PRIMARY KEY)", "INSERT INTO NJ VALUES (1)", "INSERT INTO NJ VALUES (2)", "INSERT INTO NJ VALUES (3)",
        "INSERT INTO NJ VALUES (4)", "INSERT INTO NJ VALUES (5)",
        "CREATE TABLE NK (ID LONG PRIMARY KEY, W LONG)", "INSERT INTO NK VALUES (1, 1)", "INSERT INTO NK VALUES (2, 2)", "INSERT INTO NK VALUES (3, 3)",
        "CREATE TABLE UY (ID LONG PRIMARY KEY, Y YESNO)", "CREATE INDEX IX_UY ON UY (Y)",
        "INSERT INTO UY VALUES (1, True)", "INSERT INTO UY VALUES (2, False)", "INSERT INTO UY VALUES (3, True)",
    ];

    // T holds the Long (Int32) values -1, 1, 0, Null and 2.
    [Theory]
    // A value the column cannot hold finds no row, and a single-table UPDATE changes only the rows that also pass its
    // WHERE as written.
    [InlineData("UPDATE T SET ID = ID WHERE V = 3000000000", null)]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V = CLng(1)", null)]
    [InlineData("UPDATE T SET ID = ID WHERE V >= @p", 1)]
    [InlineData("UPDATE T SET ID = ID WHERE V IN (CLng(1))", null)]
    [InlineData("UPDATE T SET ID = ID WHERE V = @p", "1.5")]
    [InlineData("UPDATE T SET ID = ID WHERE V = @p", "True")]
    // Each comparison as a SELECT, an UPDATE and DELETE of one table, and an UPDATE and DELETE through a join.
    [InlineData("SELECT ID FROM T WHERE V = @p ORDER BY ID", "True")]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V = @p", "True")]
    [InlineData("DELETE FROM T WHERE V = @p", "True")]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V = @p", "True")]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V = @p", "True")]
    [InlineData("SELECT ID FROM T WHERE V = @p ORDER BY ID", "1.5")]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V = @p", "1.5")]
    [InlineData("DELETE FROM T WHERE V = @p", "1.5")]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V = @p", "1.5")]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V = @p", "1.5")]
    [InlineData("SELECT ID FROM T WHERE V = @p ORDER BY ID", "abc")]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V = @p", "abc")]
    [InlineData("DELETE FROM T WHERE V = @p", "abc")]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V = @p", "abc")]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V = @p", "abc")]
    [InlineData("SELECT ID FROM T WHERE V >= @p ORDER BY ID", "1.5")]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V >= @p", "1.5")]
    [InlineData("DELETE FROM T WHERE V >= @p", "1.5")]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V >= @p", "1.5")]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V >= @p", "1.5")]
    [InlineData("SELECT ID FROM T WHERE V >= @p ORDER BY ID", "abc")]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V >= @p", "abc")]
    [InlineData("DELETE FROM T WHERE V >= @p", "abc")]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V >= @p", "abc")]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V >= @p", "abc")]
    [InlineData("SELECT ID FROM T WHERE V = 3000000000 ORDER BY ID", null)]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V = 3000000000", null)]
    [InlineData("DELETE FROM T WHERE V = 3000000000", null)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V = 3000000000", null)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V = 3000000000", null)]
    [InlineData("SELECT ID FROM T WHERE V > CDbl(3000000000) ORDER BY ID", null)]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V > CDbl(3000000000)", null)]
    [InlineData("DELETE FROM T WHERE V > CDbl(3000000000)", null)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V > CDbl(3000000000)", null)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V > CDbl(3000000000)", null)]
    [InlineData("SELECT ID FROM T WHERE V IN (CLng(1), CInt(True)) ORDER BY ID", null)]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V IN (CLng(1), CInt(True))", null)]
    [InlineData("DELETE FROM T WHERE V IN (CLng(1), CInt(True))", null)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V IN (CLng(1), CInt(True))", null)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V IN (CLng(1), CInt(True))", null)]
    // An UPDATE through a join onto a bracketed join, whose ON is keyed by the first table.
    [InlineData("UPDATE NJ INNER JOIN (T INNER JOIN NK ON T.ID = NK.W) ON NJ.ID = NK.ID SET T.V = T.V + 10", null)]
    // A column written without its table over a join is the column of the one table that has it.
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V = @p ORDER BY T.ID", "True")]
    [InlineData("SELECT T.ID FROM T, NJ WHERE T.ID = NJ.ID AND V = @p ORDER BY T.ID", "True")]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE V = @p", "True")]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V = @p", "True")]
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V = @p ORDER BY T.ID", "1.5")]
    [InlineData("SELECT T.ID FROM T, NJ WHERE T.ID = NJ.ID AND V = @p ORDER BY T.ID", "1.5")]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE V = @p", "1.5")]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V = @p", "1.5")]
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V >= @p ORDER BY T.ID", "abc")]
    [InlineData("SELECT T.ID FROM T, NJ WHERE T.ID = NJ.ID AND V >= @p ORDER BY T.ID", "abc")]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE V >= @p", "abc")]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V >= @p", "abc")]
    // A column written with its table over a join, compared as a SELECT through the join compares it.
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V = @p ORDER BY T.ID", "True")]
    [InlineData("SELECT T.ID FROM T, NJ WHERE T.ID = NJ.ID AND T.V = @p ORDER BY T.ID", "True")]
    [InlineData("SELECT NJ.ID FROM NJ INNER JOIN T ON NJ.ID = T.ID WHERE T.V = @p ORDER BY NJ.ID", "True")]
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V = @p ORDER BY T.ID", "1.5")]
    [InlineData("SELECT T.ID FROM T, NJ WHERE T.ID = NJ.ID AND T.V = @p ORDER BY T.ID", "1.5")]
    [InlineData("SELECT NJ.ID FROM NJ INNER JOIN T ON NJ.ID = T.ID WHERE T.V = @p ORDER BY NJ.ID", "1.5")]
    // An unqualified Yes/No column of UY beside the condition tying a correlated subquery to T's row: as EXISTS and as
    // a count, in a SELECT, an UPDATE and a DELETE.
    [InlineData("SELECT ID FROM T WHERE EXISTS (SELECT * FROM UY WHERE UY.ID = T.ID AND Y = 2) ORDER BY ID", null)]
    [InlineData("SELECT ID FROM T WHERE (SELECT COUNT(*) FROM UY WHERE UY.ID = T.ID AND Y = 2) > 0 ORDER BY ID", null)]
    [InlineData("UPDATE T SET V = V + 10 WHERE EXISTS (SELECT * FROM UY WHERE UY.ID = T.ID AND Y = 2)", null)]
    [InlineData("UPDATE T SET V = V + 10 WHERE (SELECT COUNT(*) FROM UY WHERE UY.ID = T.ID AND Y = 2) > 0", null)]
    [InlineData("DELETE FROM T WHERE EXISTS (SELECT * FROM UY WHERE Y = 2 AND UY.ID = T.ID)", null)]
    [InlineData("DELETE FROM T WHERE (SELECT COUNT(*) FROM UY WHERE UY.ID = T.ID AND Y = 2) > 0", null)]
    [InlineData("SELECT ID FROM T WHERE EXISTS (SELECT * FROM UY WHERE UY.ID = T.ID AND Y = CLng(1)) ORDER BY ID", null)]
    [InlineData("SELECT ID FROM T WHERE (SELECT COUNT(*) FROM UY WHERE UY.ID = T.ID AND Y = CLng(1)) > 0 ORDER BY ID", null)]
    [InlineData("UPDATE T SET V = V + 10 WHERE EXISTS (SELECT * FROM UY WHERE UY.ID = T.ID AND Y = CLng(1))", null)]
    [InlineData("UPDATE T SET V = V + 10 WHERE (SELECT COUNT(*) FROM UY WHERE UY.ID = T.ID AND Y = CLng(1)) > 0", null)]
    [InlineData("DELETE FROM T WHERE EXISTS (SELECT * FROM UY WHERE Y = CLng(1) AND UY.ID = T.ID)", null)]
    [InlineData("DELETE FROM T WHERE (SELECT COUNT(*) FROM UY WHERE UY.ID = T.ID AND Y = CLng(1)) > 0", null)]
    [InlineData("SELECT ID FROM T WHERE EXISTS (SELECT * FROM UY WHERE UY.ID = T.ID AND Y >= 2) ORDER BY ID", null)]
    [InlineData("SELECT ID FROM T WHERE (SELECT COUNT(*) FROM UY WHERE UY.ID = T.ID AND Y >= 2) > 0 ORDER BY ID", null)]
    [InlineData("UPDATE T SET V = V + 10 WHERE EXISTS (SELECT * FROM UY WHERE UY.ID = T.ID AND Y >= 2)", null)]
    [InlineData("UPDATE T SET V = V + 10 WHERE (SELECT COUNT(*) FROM UY WHERE UY.ID = T.ID AND Y >= 2) > 0", null)]
    [InlineData("DELETE FROM T WHERE EXISTS (SELECT * FROM UY WHERE Y >= 2 AND UY.ID = T.ID)", null)]
    [InlineData("DELETE FROM T WHERE (SELECT COUNT(*) FROM UY WHERE UY.ID = T.ID AND Y >= 2) > 0", null)]
    public void A_statement_over_an_indexed_Long_column_takes_its_comparison_as_ACE_takes_it(string statement, object? parameter) =>
        Matches([.. Common, "CREATE TABLE T (ID LONG PRIMARY KEY, V LONG)", "CREATE INDEX IX_T ON T (V)", "INSERT INTO T VALUES (1, -1)",
            "INSERT INTO T VALUES (2, 1)", "INSERT INTO T VALUES (3, 0)", "INSERT INTO T VALUES (4, NULL)", "INSERT INTO T VALUES (5, 2)"],
            statement, parameter);

    // T holds the BINARY(4) values 0x01000000, 0xFFFF0000 and Null, and has no primary key.
    [Theory]
    [InlineData("UPDATE T SET ID = ID WHERE V >= @p", 1)]
    [InlineData("UPDATE T SET ID = ID WHERE V IN (CLng(1))", null)]
    [InlineData("UPDATE T SET ID = ID WHERE V = @p", 1)]
    [InlineData("UPDATE T SET ID = ID WHERE V = CLng(1)", null)]
    [InlineData("SELECT ID FROM T WHERE V = CLng(1) ORDER BY ID", null)]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V = CLng(1)", null)]
    [InlineData("DELETE FROM T WHERE V = CLng(1)", null)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V = CLng(1)", null)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V = CLng(1)", null)]
    [InlineData("SELECT ID FROM T WHERE V IN (CLng(1)) ORDER BY ID", null)]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V IN (CLng(1))", null)]
    [InlineData("DELETE FROM T WHERE V IN (CLng(1))", null)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V IN (CLng(1))", null)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V IN (CLng(1))", null)]
    [InlineData("SELECT ID FROM T WHERE V BETWEEN CLng(0) AND CLng(1) ORDER BY ID", null)]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V BETWEEN CLng(0) AND CLng(1)", null)]
    [InlineData("DELETE FROM T WHERE V BETWEEN CLng(0) AND CLng(1)", null)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V BETWEEN CLng(0) AND CLng(1)", null)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V BETWEEN CLng(0) AND CLng(1)", null)]
    [InlineData("SELECT ID FROM T WHERE V = @p ORDER BY ID", 1)]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V = @p", 1)]
    [InlineData("DELETE FROM T WHERE V = @p", 1)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V = @p", 1)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V = @p", 1)]
    [InlineData("SELECT ID FROM T WHERE V >= @p ORDER BY ID", 1)]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V >= @p", 1)]
    [InlineData("DELETE FROM T WHERE V >= @p", 1)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V >= @p", 1)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V >= @p", 1)]
    [InlineData("SELECT ID FROM T WHERE V < @p ORDER BY ID", (short)-1)]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V < @p", (short)-1)]
    [InlineData("DELETE FROM T WHERE V < @p", (short)-1)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V < @p", (short)-1)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V < @p", (short)-1)]
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V = CLng(1) ORDER BY T.ID", null)]
    [InlineData("SELECT T.ID FROM T, NJ WHERE T.ID = NJ.ID AND V = CLng(1) ORDER BY T.ID", null)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE V = CLng(1)", null)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V = CLng(1)", null)]
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V = @p ORDER BY T.ID", 1)]
    [InlineData("SELECT T.ID FROM T, NJ WHERE T.ID = NJ.ID AND V = @p ORDER BY T.ID", 1)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE V = @p", 1)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V = @p", 1)]
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V >= @p ORDER BY T.ID", 1)]
    [InlineData("SELECT T.ID FROM T, NJ WHERE T.ID = NJ.ID AND V >= @p ORDER BY T.ID", 1)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE V >= @p", 1)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V >= @p", 1)]
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V = CLng(1) ORDER BY T.ID", null)]
    [InlineData("SELECT T.ID FROM T, NJ WHERE T.ID = NJ.ID AND T.V = CLng(1) ORDER BY T.ID", null)]
    [InlineData("SELECT NJ.ID FROM NJ INNER JOIN T ON NJ.ID = T.ID WHERE T.V = CLng(1) ORDER BY NJ.ID", null)]
    // A column of the unmatched side of a LEFT JOIN.
    [InlineData("SELECT NJ.ID, T.ID FROM NJ LEFT JOIN T ON NJ.ID = T.ID WHERE V = CLng(1) ORDER BY NJ.ID", null)]
    [InlineData("UPDATE NJ LEFT JOIN T ON NJ.ID = T.ID SET T.ID = T.ID + 10 WHERE V = CLng(1)", null)]
    [InlineData("DELETE T.* FROM NJ LEFT JOIN T ON NJ.ID = T.ID WHERE V = CLng(1)", null)]
    public void A_statement_over_an_indexed_binary_column_takes_its_comparison_as_ACE_takes_it(string statement, object? parameter) =>
        Matches([.. Common, "CREATE TABLE T (ID LONG, V BINARY(4))", "CREATE INDEX IX_T ON T (V)", "INSERT INTO T VALUES (1, 0x01000000)",
            "INSERT INTO T VALUES (2, 0xFFFF0000)", "INSERT INTO T VALUES (3, NULL)"],
            statement, parameter);

    // T holds the Yes/No values True and False.
    [Theory]
    [InlineData("UPDATE T SET ID = ID WHERE V >= @p", 1)]
    [InlineData("UPDATE T SET ID = ID WHERE V IN (CLng(1))", null)]
    [InlineData("SELECT ID FROM T WHERE V = 2 ORDER BY ID", null)]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V = 2", null)]
    [InlineData("DELETE FROM T WHERE V = 2", null)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V = 2", null)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V = 2", null)]
    [InlineData("SELECT ID FROM T WHERE V = CLng(1) ORDER BY ID", null)]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V = CLng(1)", null)]
    [InlineData("DELETE FROM T WHERE V = CLng(1)", null)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V = CLng(1)", null)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V = CLng(1)", null)]
    [InlineData("SELECT ID FROM T WHERE V IN (CLng(1)) ORDER BY ID", null)]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V IN (CLng(1))", null)]
    [InlineData("DELETE FROM T WHERE V IN (CLng(1))", null)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V IN (CLng(1))", null)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V IN (CLng(1))", null)]
    [InlineData("SELECT ID FROM T WHERE V BETWEEN CLng(1) AND CLng(2) ORDER BY ID", null)]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V BETWEEN CLng(1) AND CLng(2)", null)]
    [InlineData("DELETE FROM T WHERE V BETWEEN CLng(1) AND CLng(2)", null)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V BETWEEN CLng(1) AND CLng(2)", null)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V BETWEEN CLng(1) AND CLng(2)", null)]
    [InlineData("SELECT ID FROM T WHERE V >= @p ORDER BY ID", 1)]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V >= @p", 1)]
    [InlineData("DELETE FROM T WHERE V >= @p", 1)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V >= @p", 1)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V >= @p", 1)]
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V = 2 ORDER BY T.ID", null)]
    [InlineData("SELECT T.ID FROM T, NJ WHERE T.ID = NJ.ID AND V = 2 ORDER BY T.ID", null)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE V = 2", null)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V = 2", null)]
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V = CLng(1) ORDER BY T.ID", null)]
    [InlineData("SELECT T.ID FROM T, NJ WHERE T.ID = NJ.ID AND V = CLng(1) ORDER BY T.ID", null)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE V = CLng(1)", null)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V = CLng(1)", null)]
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V IN (CLng(1)) ORDER BY T.ID", null)]
    [InlineData("SELECT T.ID FROM T, NJ WHERE T.ID = NJ.ID AND V IN (CLng(1)) ORDER BY T.ID", null)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE V IN (CLng(1))", null)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V IN (CLng(1))", null)]
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V >= @p ORDER BY T.ID", 1)]
    [InlineData("SELECT T.ID FROM T, NJ WHERE T.ID = NJ.ID AND V >= @p ORDER BY T.ID", 1)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE V >= @p", 1)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V >= @p", 1)]
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V = 2 ORDER BY T.ID", null)]
    [InlineData("SELECT T.ID FROM T, NJ WHERE T.ID = NJ.ID AND T.V = 2 ORDER BY T.ID", null)]
    [InlineData("SELECT NJ.ID FROM NJ INNER JOIN T ON NJ.ID = T.ID WHERE T.V = 2 ORDER BY NJ.ID", null)]
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V = CLng(1) ORDER BY T.ID", null)]
    [InlineData("SELECT T.ID FROM T, NJ WHERE T.ID = NJ.ID AND T.V = CLng(1) ORDER BY T.ID", null)]
    [InlineData("SELECT NJ.ID FROM NJ INNER JOIN T ON NJ.ID = T.ID WHERE T.V = CLng(1) ORDER BY NJ.ID", null)]
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V >= @p ORDER BY T.ID", 1)]
    [InlineData("SELECT T.ID FROM T, NJ WHERE T.ID = NJ.ID AND T.V >= @p ORDER BY T.ID", 1)]
    [InlineData("SELECT NJ.ID FROM NJ INNER JOIN T ON NJ.ID = T.ID WHERE T.V >= @p ORDER BY NJ.ID", 1)]
    // Conditions on one table written with and without its name, both columns indexed (ID is the primary key).
    [InlineData("SELECT ID FROM T WHERE T.ID >= 1 AND V = 2 ORDER BY ID", null)]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE T.ID >= 1 AND V = 2", null)]
    [InlineData("DELETE FROM T WHERE T.ID >= 1 AND V = 2", null)]
    [InlineData("SELECT ID FROM T WHERE ID >= 1 AND T.V = 2 ORDER BY ID", null)]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE ID >= 1 AND T.V = 2", null)]
    [InlineData("DELETE FROM T WHERE ID >= 1 AND T.V = 2", null)]
    [InlineData("SELECT ID FROM T WHERE T.ID = 1 AND V = CLng(1) ORDER BY ID", null)]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE T.ID = 1 AND V = CLng(1)", null)]
    [InlineData("DELETE FROM T WHERE T.ID = 1 AND V = CLng(1)", null)]
    [InlineData("SELECT ID FROM T WHERE ID <= 2 AND T.V >= @p ORDER BY ID", 1)]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE ID <= 2 AND T.V >= @p", 1)]
    [InlineData("DELETE FROM T WHERE ID <= 2 AND T.V >= @p", 1)]
    [InlineData("SELECT ID FROM T WHERE V = 2 AND T.ID < 2 ORDER BY ID", null)]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V = 2 AND T.ID < 2", null)]
    [InlineData("DELETE FROM T WHERE V = 2 AND T.ID < 2", null)]
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.ID >= 1 AND V = 2 ORDER BY T.ID", null)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.ID >= 1 AND V = 2", null)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.ID >= 1 AND V = 2", null)]
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V = CLng(1) AND T.ID <= 2 ORDER BY T.ID", null)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE V = CLng(1) AND T.ID <= 2", null)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V = CLng(1) AND T.ID <= 2", null)]
    // A name both tables of a join have.
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE ID = 1 ORDER BY T.ID", null)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.V = False WHERE ID = 1", null)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE ID = 1", null)]
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE ID = 1 AND V = 2 ORDER BY T.ID", null)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.V = False WHERE ID = 1 AND V = 2", null)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE ID = 1 AND V = 2", null)]
    // A column of the unmatched side of a LEFT JOIN.
    [InlineData("SELECT NJ.ID, T.ID FROM NJ LEFT JOIN T ON NJ.ID = T.ID WHERE V = 2 ORDER BY NJ.ID", null)]
    [InlineData("UPDATE NJ LEFT JOIN T ON NJ.ID = T.ID SET T.ID = T.ID + 10 WHERE V = 2", null)]
    [InlineData("DELETE T.* FROM NJ LEFT JOIN T ON NJ.ID = T.ID WHERE V = 2", null)]
    [InlineData("SELECT NJ.ID, T.ID FROM NJ LEFT JOIN T ON NJ.ID = T.ID WHERE V = CLng(1) ORDER BY NJ.ID", null)]
    [InlineData("UPDATE NJ LEFT JOIN T ON NJ.ID = T.ID SET T.ID = T.ID + 10 WHERE V = CLng(1)", null)]
    [InlineData("DELETE T.* FROM NJ LEFT JOIN T ON NJ.ID = T.ID WHERE V = CLng(1)", null)]
    // An unqualified column of T beside the condition tying a correlated subquery to NJ's row.
    [InlineData("SELECT NJ.ID FROM NJ WHERE EXISTS (SELECT * FROM T WHERE T.ID = NJ.ID AND V = 2) ORDER BY NJ.ID", null)]
    [InlineData("SELECT NJ.ID FROM NJ WHERE NOT EXISTS (SELECT * FROM T WHERE V = 2 AND T.ID = NJ.ID) ORDER BY NJ.ID", null)]
    [InlineData("SELECT NJ.ID FROM NJ WHERE (SELECT COUNT(*) FROM T WHERE T.ID = NJ.ID AND V = 2) > 0 ORDER BY NJ.ID", null)]
    [InlineData("SELECT NJ.ID FROM NJ WHERE EXISTS (SELECT * FROM T WHERE T.ID = NJ.ID AND V = CLng(1)) ORDER BY NJ.ID", null)]
    [InlineData("SELECT NJ.ID FROM NJ WHERE NOT EXISTS (SELECT * FROM T WHERE V = CLng(1) AND T.ID = NJ.ID) ORDER BY NJ.ID", null)]
    [InlineData("SELECT NJ.ID FROM NJ WHERE (SELECT COUNT(*) FROM T WHERE T.ID = NJ.ID AND V = CLng(1)) > 0 ORDER BY NJ.ID", null)]
    [InlineData("SELECT NJ.ID FROM NJ WHERE EXISTS (SELECT * FROM T WHERE T.ID = NJ.ID AND V >= 2) ORDER BY NJ.ID", null)]
    [InlineData("SELECT NJ.ID FROM NJ WHERE NOT EXISTS (SELECT * FROM T WHERE V >= 2 AND T.ID = NJ.ID) ORDER BY NJ.ID", null)]
    [InlineData("SELECT NJ.ID FROM NJ WHERE (SELECT COUNT(*) FROM T WHERE T.ID = NJ.ID AND V >= 2) > 0 ORDER BY NJ.ID", null)]
    public void A_statement_over_an_indexed_YesNo_column_takes_its_comparison_as_ACE_takes_it(string statement, object? parameter) =>
        Matches([.. Common, "CREATE TABLE T (ID LONG PRIMARY KEY, V YESNO)", "CREATE INDEX IX_T ON T (V)", "INSERT INTO T VALUES (1, True)",
            "INSERT INTO T VALUES (2, False)"],
            statement, parameter);

    // T holds a GUID whose first four bytes are 1, one whose first four are 0xFFFF, and Null, and has no primary key.
    [Theory]
    [InlineData("SELECT ID FROM T WHERE V = CLng(1) ORDER BY ID", null)]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V = CLng(1)", null)]
    [InlineData("DELETE FROM T WHERE V = CLng(1)", null)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V = CLng(1)", null)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V = CLng(1)", null)]
    [InlineData("SELECT ID FROM T WHERE V = @p ORDER BY ID", 1)]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V = @p", 1)]
    [InlineData("DELETE FROM T WHERE V = @p", 1)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V = @p", 1)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V = @p", 1)]
    [InlineData("SELECT ID FROM T WHERE V >= @p ORDER BY ID", true)]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V >= @p", true)]
    [InlineData("DELETE FROM T WHERE V >= @p", true)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V >= @p", true)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V >= @p", true)]
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V = CLng(1) ORDER BY T.ID", null)]
    [InlineData("SELECT T.ID FROM T, NJ WHERE T.ID = NJ.ID AND V = CLng(1) ORDER BY T.ID", null)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE V = CLng(1)", null)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V = CLng(1)", null)]
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V = @p ORDER BY T.ID", 1)]
    [InlineData("SELECT T.ID FROM T, NJ WHERE T.ID = NJ.ID AND V = @p ORDER BY T.ID", 1)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE V = @p", 1)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V = @p", 1)]
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V >= @p ORDER BY T.ID", true)]
    [InlineData("SELECT T.ID FROM T, NJ WHERE T.ID = NJ.ID AND V >= @p ORDER BY T.ID", true)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE V >= @p", true)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE V >= @p", true)]
    public void A_statement_over_an_indexed_GUID_column_takes_its_comparison_as_ACE_takes_it(string statement, object? parameter) =>
        Matches([.. Common, "CREATE TABLE T (ID LONG, V GUID)", "CREATE INDEX IX_T ON T (V)",
            "INSERT INTO T VALUES (1, {guid {00000001-0000-0000-0000-000000000000}})",
            "INSERT INTO T VALUES (2, {guid {0000FFFF-0000-0000-0000-000000000000}})", "INSERT INTO T VALUES (3, NULL)"],
            statement, parameter);

    // T holds the Currency values 1.2345, 1.2346 and 2.
    [Theory]
    [InlineData("SELECT ID FROM T WHERE V = @p ORDER BY ID", "1.23455")]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V = @p", "1.23455")]
    [InlineData("DELETE FROM T WHERE V = @p", "1.23455")]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V = @p", "1.23455")]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V = @p", "1.23455")]
    [InlineData("SELECT ID FROM T WHERE V = @p ORDER BY ID", 1.23455)]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V = @p", 1.23455)]
    [InlineData("DELETE FROM T WHERE V = @p", 1.23455)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V = @p", 1.23455)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V = @p", 1.23455)]
    public void A_statement_over_an_indexed_Currency_column_takes_its_comparison_as_ACE_takes_it(string statement, object? parameter) =>
        Matches([.. Common, "CREATE TABLE T (ID LONG PRIMARY KEY, V CURRENCY)", "CREATE INDEX IX_T ON T (V)", "INSERT INTO T VALUES (1, 1.2345)",
            "INSERT INTO T VALUES (2, 1.2346)", "INSERT INTO T VALUES (3, 2)"],
            statement, parameter);

    // T holds the dates 12/29/1899, 12/31/1899, 12/30/1899 and Null.
    [Theory]
    // A single-table UPDATE re-tests the rows its index finds against the WHERE, which reads the text as a date.
    [InlineData("UPDATE T SET ID = ID WHERE V = @p", "12/31/1899")]
    [InlineData("SELECT ID FROM T WHERE V = @p ORDER BY ID", "12/31/1899")]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V = @p", "12/31/1899")]
    [InlineData("DELETE FROM T WHERE V = @p", "12/31/1899")]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V = @p", "12/31/1899")]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V = @p", "12/31/1899")]
    [InlineData("SELECT ID FROM T WHERE V = @p ORDER BY ID", "1.5")]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V = @p", "1.5")]
    [InlineData("DELETE FROM T WHERE V = @p", "1.5")]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V = @p", "1.5")]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V = @p", "1.5")]
    [InlineData("SELECT ID FROM T WHERE V < @p ORDER BY ID", "1.5")]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V < @p", "1.5")]
    [InlineData("DELETE FROM T WHERE V < @p", "1.5")]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V < @p", "1.5")]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V < @p", "1.5")]
    [InlineData("SELECT ID FROM T WHERE V >= @p ORDER BY ID", "1")]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V >= @p", "1")]
    [InlineData("DELETE FROM T WHERE V >= @p", "1")]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V >= @p", "1")]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V >= @p", "1")]
    public void A_statement_over_an_indexed_date_column_takes_its_comparison_as_ACE_takes_it(string statement, object? parameter) =>
        Matches([.. Common, "CREATE TABLE T (ID LONG PRIMARY KEY, V DATETIME)", "CREATE INDEX IX_T ON T (V)", "INSERT INTO T VALUES (1, #12/29/1899#)",
            "INSERT INTO T VALUES (2, #12/31/1899#)", "INSERT INTO T VALUES (3, #12/30/1899#)", "INSERT INTO T VALUES (4, NULL)"],
            statement, parameter);

    // T holds the Large Number (BIGINT) values -1, 1, 0, Null and 2.
    [Theory]
    [InlineData("SELECT ID FROM T WHERE V = @p ORDER BY ID", "2")]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V = @p", "2")]
    [InlineData("DELETE FROM T WHERE V = @p", "2")]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V = @p", "2")]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V = @p", "2")]
    [InlineData("SELECT ID FROM T WHERE V = @p ORDER BY ID", "abc")]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V = @p", "abc")]
    [InlineData("DELETE FROM T WHERE V = @p", "abc")]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V = @p", "abc")]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V = @p", "abc")]
    [InlineData("SELECT ID FROM T WHERE V = @p ORDER BY ID", "1.5")]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V = @p", "1.5")]
    [InlineData("DELETE FROM T WHERE V = @p", "1.5")]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V = @p", "1.5")]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V = @p", "1.5")]
    [InlineData("SELECT ID FROM T WHERE V > @p ORDER BY ID", "1")]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V > @p", "1")]
    [InlineData("DELETE FROM T WHERE V > @p", "1")]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V > @p", "1")]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V > @p", "1")]
    [InlineData("SELECT ID FROM T WHERE V = CStr(1) ORDER BY ID", null)]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V = CStr(1)", null)]
    [InlineData("DELETE FROM T WHERE V = CStr(1)", null)]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V = CStr(1)", null)]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V = CStr(1)", null)]
    public void A_statement_over_an_indexed_BIGINT_column_takes_its_comparison_as_ACE_takes_it(string statement, object? parameter)
    {
        Assert.SkipUnless(
            AceTestDatabase.SupportsColumnType(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "BIGINT"),
            AceTestDatabase.UnsupportedColumnTypeReason("BIGINT"));
        Matches([.. Common, "CREATE TABLE T (ID LONG PRIMARY KEY, V BIGINT)", "CREATE INDEX IX_T ON T (V)", "INSERT INTO T VALUES (1, -1)",
            "INSERT INTO T VALUES (2, 1)", "INSERT INTO T VALUES (3, 0)", "INSERT INTO T VALUES (4, NULL)", "INSERT INTO T VALUES (5, 2)"],
            statement, parameter);
    }

    // A written True on a Long (Int32) column, on V (indexed) and U (the same values, unindexed): as a SELECT, an UPDATE
    // and a DELETE of one table and of a join, and as a SELECT through a join. T holds -1, 1 and 2 in both.
    [Theory]
    [InlineData("SELECT ID FROM T WHERE V = True ORDER BY ID")]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V = True")]
    [InlineData("DELETE FROM T WHERE V = True")]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V = True")]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V = True")]
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V = True ORDER BY T.ID")]
    [InlineData("SELECT ID FROM T WHERE U = True ORDER BY ID")]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE U = True")]
    [InlineData("DELETE FROM T WHERE U = True")]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE U = True")]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE U = True")]
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE U = True ORDER BY T.ID")]
    public void A_written_True_compared_with_a_Long_column_selects_the_same_rows_with_an_index_or_without(string statement) =>
        Matches([.. Common, "CREATE TABLE T (ID LONG PRIMARY KEY, V LONG, U LONG)", "CREATE INDEX IX_T ON T (V)", "INSERT INTO T VALUES (1, -1, -1)",
            "INSERT INTO T VALUES (2, 1, 1)", "INSERT INTO T VALUES (3, 2, 2)"],
            statement, null);

    // A written False on a text column, the same way. T holds '0', '-1' and 'False' in both.
    [Theory]
    [InlineData("SELECT ID FROM T WHERE V = False ORDER BY ID")]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE V = False")]
    [InlineData("DELETE FROM T WHERE V = False")]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE T.V = False")]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V = False")]
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE T.V = False ORDER BY T.ID")]
    [InlineData("SELECT ID FROM T WHERE U = False ORDER BY ID")]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE U = False")]
    [InlineData("DELETE FROM T WHERE U = False")]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE U = False")]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE U = False")]
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE U = False ORDER BY T.ID")]
    public void A_written_False_compared_with_a_text_column_selects_the_same_rows_with_an_index_or_without(string statement) =>
        Matches([.. Common, "CREATE TABLE T (ID LONG PRIMARY KEY, V TEXT(10), U TEXT(10))", "CREATE INDEX IX_T ON T (V)",
            "INSERT INTO T VALUES (1, '0', '0')", "INSERT INTO T VALUES (2, '-1', '-1')", "INSERT INTO T VALUES (3, 'False', 'False')"],
            statement, null);

    // NOT (x <> Null), the same way. T holds 1, Null and 2 in both.
    [Theory]
    [InlineData("SELECT ID FROM T WHERE NOT (V <> Null) ORDER BY ID")]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE NOT (V <> Null)")]
    [InlineData("DELETE FROM T WHERE NOT (V <> Null)")]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE NOT (T.V <> Null)")]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE NOT (T.V <> Null)")]
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE NOT (T.V <> Null) ORDER BY T.ID")]
    [InlineData("SELECT ID FROM T WHERE NOT (U <> Null) ORDER BY ID")]
    [InlineData("UPDATE T SET ID = ID + 10 WHERE NOT (U <> Null)")]
    [InlineData("DELETE FROM T WHERE NOT (U <> Null)")]
    [InlineData("UPDATE T INNER JOIN NJ ON T.ID = NJ.ID SET T.ID = T.ID + 10 WHERE NOT (U <> Null)")]
    [InlineData("DELETE T.* FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE NOT (U <> Null)")]
    [InlineData("SELECT T.ID FROM T INNER JOIN NJ ON T.ID = NJ.ID WHERE NOT (U <> Null) ORDER BY T.ID")]
    public void A_written_Null_under_NOT_unequal_selects_the_same_rows_with_an_index_or_without(string statement) =>
        Matches([.. Common, "CREATE TABLE T (ID LONG PRIMARY KEY, V LONG, U LONG)", "CREATE INDEX IX_T ON T (V)", "INSERT INTO T VALUES (1, 1, 1)",
            "INSERT INTO T VALUES (2, NULL, NULL)", "INSERT INTO T VALUES (3, 2, 2)"],
            statement, null);

    private static void Matches(string[] setup, string statement, object? parameter) =>
        Assert.Equal(Describe(setup, statement, parameter, AceRun), Describe(setup, statement, parameter, LibRedRun));

    /// <summary>The setup's outcomes through ACE, then the statement's — the rows a query returns, or the rows a write
    /// affected — with the parameter bound when there is one, then table T as ACE reads it. Any refusal is
    /// "refused".</summary>
    private static string AceRun(string path, string[] setup, string statement, object? parameter)
    {
        var outcome = new List<string>();
        using OleDbConnection connection = AceTestDatabase.Open(path);
        foreach (string s in setup)
        {
            using OleDbCommand command = connection.CreateCommand();
            command.CommandText = s;
            try { outcome.Add(Affected(s, command.ExecuteNonQuery())); }
            catch (OleDbException) { outcome.Add("refused"); }
        }
        using (OleDbCommand command = connection.CreateCommand())
        {
            command.CommandText = statement;
            if (parameter is not null) command.Parameters.AddWithValue("@p", parameter);
            try
            {
                if (statement.StartsWith("SELECT", StringComparison.Ordinal))
                {
                    using OleDbDataReader reader = command.ExecuteReader();
                    outcome.Add(Rows(Read(reader)));
                }
                else outcome.Add(Affected(statement, command.ExecuteNonQuery()));
            }
            catch (OleDbException) { outcome.Add("refused"); }
        }
        using OleDbCommand read = connection.CreateCommand();
        read.CommandText = "SELECT * FROM T ORDER BY ID";
        using (OleDbDataReader reader = read.ExecuteReader())
            outcome.Add(Rows(Read(reader)));
        return string.Join(" | ", outcome);
    }

    private static List<object?[]> Read(OleDbDataReader reader)
    {
        var rows = new List<object?[]>();
        while (reader.Read())
            rows.Add([.. Enumerable.Range(0, reader.FieldCount).Select(reader.GetValue)]);
        return rows;
    }

    /// <summary>The same through LibRed's engine. A refusal is one of the exceptions LibRed refuses a statement with; any
    /// other is a fault, and fails the test.</summary>
    private static string LibRedRun(string path, string[] setup, string statement, object? parameter)
    {
        var outcome = new List<string>();
        using var database = JetDatabase.Open(path, readOnly: false);
        var engine = new QueryEngine(database);
        foreach (string s in setup)
        {
            try { outcome.Add(Affected(s, engine.ExecuteNonQuery(s))); }
            catch (Exception e) when (IsLibRedRefusal(e)) { outcome.Add("refused"); }
        }
        Dictionary<string, object?>? parameters = parameter is null ? null : new() { ["p"] = parameter };
        try
        {
            outcome.Add(statement.StartsWith("SELECT", StringComparison.Ordinal)
                ? Rows(engine.ExecuteQuery(statement, parameters).Rows)
                : Affected(statement, engine.ExecuteNonQuery(statement, parameters)));
        }
        catch (Exception e) when (IsLibRedRefusal(e)) { outcome.Add("refused"); }
        outcome.Add(Rows(engine.ExecuteQuery("SELECT * FROM T ORDER BY ID").Rows));
        return string.Join(" | ", outcome);
    }

    private static bool IsLibRedRefusal(Exception e) =>
        e is InvalidOperationException or ArgumentException or InvalidCastException or FormatException or OverflowException
            or DivideByZeroException or NotSupportedException or LibRed.Sql.Binding.SqlBindException or LibRed.Sql.Parsing.SqlParseException;

    private static string Describe(string[] setup, string statement, object? parameter, Func<string, string[], string, object?, string> run)
    {
        string path = TemporaryDatabase.CopyPath(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "seek-");
        try { return run(path, setup, statement, parameter); }
        finally
        {
            OleDbConnection.ReleaseObjectPool();
            TemporaryDatabase.Delete(path);
        }
    }

    private static string Affected(string statement, int rows) =>
        statement.StartsWith("INSERT", StringComparison.Ordinal) || statement.StartsWith("UPDATE", StringComparison.Ordinal)
            || statement.StartsWith("DELETE", StringComparison.Ordinal)
            ? rows.ToString(CultureInfo.InvariantCulture) : "ok";

    private static string Rows(IEnumerable<object?[]> rows) =>
        string.Join(" ", rows.Select(row => string.Join(",", row.Select(Format))));

    private static string Format(object? value) => value switch
    {
        null or DBNull => "NULL",
        byte[] bytes => "Byte[]:" + Convert.ToHexString(bytes),
        // By value, not scale: the two readers scale a CURRENCY zero differently whatever wrote it.
        decimal m => "Decimal:" + m.ToString("G29", CultureInfo.InvariantCulture),
        DateTime date => "DateTime:" + date.ToString("o", CultureInfo.InvariantCulture),
        _ => $"{value.GetType().Name}:{Convert.ToString(value, CultureInfo.InvariantCulture)}",
    };
}
