// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using EntityFrameworkCore.Jet.FunctionalTests.TestUtilities;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestModels.Northwind;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;
using Xunit.Abstractions;

namespace EntityFrameworkCore.Jet.FunctionalTests.Query
{
    public class NorthwindSetOperationsQueryJetTest : NorthwindSetOperationsQueryRelationalTestBase<
        NorthwindQueryJetFixture<NoopModelCustomizer>>
    {
        public NorthwindSetOperationsQueryJetTest(
            NorthwindQueryJetFixture<NoopModelCustomizer> fixture,
            ITestOutputHelper testOutputHelper)
            : base(fixture)
        {
            ClearLog();
            Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
        }

        [ConditionalFact]
        public virtual void Check_all_tests_overridden()
            => TestHelpers.AssertAllMethodsOverridden(GetType());

        public override async Task Union(bool isAsync)
        {
            await base.Union(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`City` = 'Berlin'
                    UNION
                    SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
                    FROM `Customers` AS `c0`
                    WHERE `c0`.`City` = 'London'
                    """);
        }

        public override async Task Concat(bool isAsync)
        {
            await base.Concat(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`City` = 'Berlin'
                    UNION ALL
                    SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
                    FROM `Customers` AS `c0`
                    WHERE `c0`.`City` = 'London'
                    """);
        }

        public override async Task Intersect(bool isAsync)
        {
            await base.Intersect(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`City` = 'London'
                    INTERSECT
                    SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
                    FROM `Customers` AS `c0`
                    WHERE CHARINDEX('Thomas', `c0`.`ContactName`) > 0
                    """);
        }

        public override async Task Except(bool isAsync)
        {
            await base.Except(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`City` = 'London'
                    EXCEPT
                    SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
                    FROM `Customers` AS `c0`
                    WHERE CHARINDEX('Thomas', `c0`.`ContactName`) > 0
                    """);
        }

        public override async Task Union_OrderBy_Skip_Take(bool isAsync)
        {
            await base.Union_OrderBy_Skip_Take(isAsync);

            AssertSql(
                """
SELECT `u1`.`CustomerID`, `u1`.`Address`, `u1`.`City`, `u1`.`CompanyName`, `u1`.`ContactName`, `u1`.`ContactTitle`, `u1`.`Country`, `u1`.`Fax`, `u1`.`Phone`, `u1`.`PostalCode`, `u1`.`Region`
FROM (
    SELECT TOP @p `u0`.`CustomerID`, `u0`.`Address`, `u0`.`City`, `u0`.`CompanyName`, `u0`.`ContactName`, `u0`.`ContactTitle`, `u0`.`Country`, `u0`.`Fax`, `u0`.`Phone`, `u0`.`PostalCode`, `u0`.`Region`
    FROM (
        SELECT TOP @p + @p `u`.`CustomerID`, `u`.`Address`, `u`.`City`, `u`.`CompanyName`, `u`.`ContactName`, `u`.`ContactTitle`, `u`.`Country`, `u`.`Fax`, `u`.`Phone`, `u`.`PostalCode`, `u`.`Region`
        FROM (
            SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
            FROM `Customers` AS `c`
            WHERE `c`.`City` = 'Berlin'
            UNION
            SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
            FROM `Customers` AS `c0`
            WHERE `c0`.`City` = 'London'
        ) AS `u`
        ORDER BY `u`.`ContactName`
    ) AS `u0`
    ORDER BY `u0`.`ContactName` DESC
) AS `u1`
ORDER BY `u1`.`ContactName`
""");
        }

        public override async Task Union_Where(bool isAsync)
        {
            await base.Union_Where(isAsync);

            AssertSql(
                """
SELECT `u`.`CustomerID`, `u`.`Address`, `u`.`City`, `u`.`CompanyName`, `u`.`ContactName`, `u`.`ContactTitle`, `u`.`Country`, `u`.`Fax`, `u`.`Phone`, `u`.`PostalCode`, `u`.`Region`
FROM (
    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    WHERE `c`.`City` = 'Berlin'
    UNION
    SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
    FROM `Customers` AS `c0`
    WHERE `c0`.`City` = 'London'
) AS `u`
WHERE `u`.`ContactName` LIKE '%Thomas%'
""");
        }

        public override async Task Union_Skip_Take_OrderBy_ThenBy_Where(bool isAsync)
        {
            await base.Union_Skip_Take_OrderBy_ThenBy_Where(isAsync);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@__p_0='0'")}
                    
                    SELECT `t0`.`CustomerID`, `t0`.`Address`, `t0`.`City`, `t0`.`CompanyName`, `t0`.`ContactName`, `t0`.`ContactTitle`, `t0`.`Country`, `t0`.`Fax`, `t0`.`Phone`, `t0`.`PostalCode`, `t0`.`Region`
                    FROM (
                        SELECT `t`.`CustomerID`, `t`.`Address`, `t`.`City`, `t`.`CompanyName`, `t`.`ContactName`, `t`.`ContactTitle`, `t`.`Country`, `t`.`Fax`, `t`.`Phone`, `t`.`PostalCode`, `t`.`Region`
                        FROM (
                            SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                            FROM `Customers` AS `c`
                            WHERE `c`.`City` = 'Berlin'
                            UNION
                            SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
                            FROM `Customers` AS `c0`
                            WHERE `c0`.`City` = 'London'
                        ) AS `t`
                        ORDER BY `t`.`Region`, `t`.`City`
                        SKIP {AssertSqlHelper.Parameter("@__p_0")}
                    ) AS `t0`
                    WHERE CHARINDEX('Thomas', `t0`.`ContactName`) > 0
                    ORDER BY `t0`.`Region`, `t0`.`City`
                    """);
        }

        public override async Task Union_Union(bool isAsync)
        {
            await base.Union_Union(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`City` = 'Berlin'
                    UNION
                    SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
                    FROM `Customers` AS `c0`
                    WHERE `c0`.`City` = 'London'
                    UNION
                    SELECT `c1`.`CustomerID`, `c1`.`Address`, `c1`.`City`, `c1`.`CompanyName`, `c1`.`ContactName`, `c1`.`ContactTitle`, `c1`.`Country`, `c1`.`Fax`, `c1`.`Phone`, `c1`.`PostalCode`, `c1`.`Region`
                    FROM `Customers` AS `c1`
                    WHERE `c1`.`City` = 'Mannheim'
                    """);
        }

        public override async Task Union_Intersect(bool isAsync)
        {
            await base.Union_Intersect(isAsync);

            AssertSql(
                $"""
                    (
                        SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                        FROM `Customers` AS `c`
                        WHERE `c`.`City` = 'Berlin'
                        UNION
                        SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
                        FROM `Customers` AS `c0`
                        WHERE `c0`.`City` = 'London'
                    )
                    INTERSECT
                    SELECT `c1`.`CustomerID`, `c1`.`Address`, `c1`.`City`, `c1`.`CompanyName`, `c1`.`ContactName`, `c1`.`ContactTitle`, `c1`.`Country`, `c1`.`Fax`, `c1`.`Phone`, `c1`.`PostalCode`, `c1`.`Region`
                    FROM `Customers` AS `c1`
                    WHERE CHARINDEX('Thomas', `c1`.`ContactName`) > 0
                    """);
        }

        public override async Task Union_inside_Concat(bool async)
        {
            await base.Union_inside_Concat(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = 'Berlin'
UNION ALL
(
    SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
    FROM `Customers` AS `c0`
    WHERE `c0`.`City` = 'London'
    UNION
    SELECT `c1`.`CustomerID`, `c1`.`Address`, `c1`.`City`, `c1`.`CompanyName`, `c1`.`ContactName`, `c1`.`ContactTitle`, `c1`.`Country`, `c1`.`Fax`, `c1`.`Phone`, `c1`.`PostalCode`, `c1`.`Region`
    FROM `Customers` AS `c1`
    WHERE `c1`.`City` = 'Berlin'
)
""");
        }

        public override async Task Union_Take_Union_Take(bool isAsync)
        {
            await base.Union_Take_Union_Take(isAsync);

            AssertSql(
                """
SELECT `u2`.`CustomerID`, `u2`.`Address`, `u2`.`City`, `u2`.`CompanyName`, `u2`.`ContactName`, `u2`.`ContactTitle`, `u2`.`Country`, `u2`.`Fax`, `u2`.`Phone`, `u2`.`PostalCode`, `u2`.`Region`
FROM (
    SELECT TOP @p `u1`.`CustomerID`, `u1`.`Address`, `u1`.`City`, `u1`.`CompanyName`, `u1`.`ContactName`, `u1`.`ContactTitle`, `u1`.`Country`, `u1`.`Fax`, `u1`.`Phone`, `u1`.`PostalCode`, `u1`.`Region`
    FROM (
        SELECT `u0`.`CustomerID`, `u0`.`Address`, `u0`.`City`, `u0`.`CompanyName`, `u0`.`ContactName`, `u0`.`ContactTitle`, `u0`.`Country`, `u0`.`Fax`, `u0`.`Phone`, `u0`.`PostalCode`, `u0`.`Region`
        FROM (
            SELECT TOP @p `u`.`CustomerID`, `u`.`Address`, `u`.`City`, `u`.`CompanyName`, `u`.`ContactName`, `u`.`ContactTitle`, `u`.`Country`, `u`.`Fax`, `u`.`Phone`, `u`.`PostalCode`, `u`.`Region`
            FROM (
                SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                FROM `Customers` AS `c`
                WHERE `c`.`City` = 'Berlin'
                UNION
                SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
                FROM `Customers` AS `c0`
                WHERE `c0`.`City` = 'London'
            ) AS `u`
            ORDER BY `u`.`CustomerID`
        ) AS `u0`
        UNION
        SELECT `c1`.`CustomerID`, `c1`.`Address`, `c1`.`City`, `c1`.`CompanyName`, `c1`.`ContactName`, `c1`.`ContactTitle`, `c1`.`Country`, `c1`.`Fax`, `c1`.`Phone`, `c1`.`PostalCode`, `c1`.`Region`
        FROM `Customers` AS `c1`
        WHERE `c1`.`City` = 'Mannheim'
    ) AS `u1`
) AS `u2`
ORDER BY `u2`.`CustomerID`
""");
        }

        public override async Task Select_Union(bool isAsync)
        {
            await base.Select_Union(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`Address`
                    FROM `Customers` AS `c`
                    WHERE `c`.`City` = 'Berlin'
                    UNION
                    SELECT `c0`.`Address`
                    FROM `Customers` AS `c0`
                    WHERE `c0`.`City` = 'London'
                    """);
        }

        public override async Task Union_Select(bool isAsync)
        {
            await base.Union_Select(isAsync);

            AssertSql(
                """
SELECT `u`.`Address`
FROM (
    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    WHERE `c`.`City` = 'Berlin'
    UNION
    SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
    FROM `Customers` AS `c0`
    WHERE `c0`.`City` = 'London'
) AS `u`
WHERE `u`.`Address` LIKE '%Hanover%'
""");
        }

        public override async Task Union_Select_scalar(bool isAsync)
        {
            await base.Union_Select_scalar(isAsync);

            AssertSql(
                """
SELECT 1
FROM (
    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    UNION
    SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
    FROM `Customers` AS `c0`
) AS `u`
""");
        }

        public override async Task Union_with_anonymous_type_projection(bool isAsync)
        {
            await base.Union_with_anonymous_type_projection(isAsync);

            AssertSql(
                """
SELECT `u`.`CustomerID` AS `Id`
FROM (
    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    WHERE `c`.`CompanyName` LIKE 'A%'
    UNION
    SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
    FROM `Customers` AS `c0`
    WHERE `c0`.`CompanyName` LIKE 'B%'
) AS `u`
""");
        }

        public override async Task Select_Union_unrelated(bool isAsync)
        {
            await base.Select_Union_unrelated(isAsync);

            AssertSql(
                """
SELECT `u`.`CompanyName`
FROM (
    SELECT `c`.`CompanyName`
    FROM `Customers` AS `c`
    UNION
    SELECT `p`.`ProductName` AS `CompanyName`
    FROM `Products` AS `p`
) AS `u`
WHERE `u`.`CompanyName` LIKE 'C%'
ORDER BY `u`.`CompanyName`
""");
        }

        public override async Task Select_Union_different_fields_in_anonymous_with_subquery(bool isAsync)
        {
            await base.Select_Union_different_fields_in_anonymous_with_subquery(isAsync);

            AssertSql(
                """
SELECT `u0`.`Foo`, `u0`.`CustomerID`, `u0`.`Address`, `u0`.`City`, `u0`.`CompanyName`, `u0`.`ContactName`, `u0`.`ContactTitle`, `u0`.`Country`, `u0`.`Fax`, `u0`.`Phone`, `u0`.`PostalCode`, `u0`.`Region`
FROM (
    SELECT TOP @p0 `u1`.`Foo`, `u1`.`CustomerID`, `u1`.`Address`, `u1`.`City`, `u1`.`CompanyName`, `u1`.`ContactName`, `u1`.`ContactTitle`, `u1`.`Country`, `u1`.`Fax`, `u1`.`Phone`, `u1`.`PostalCode`, `u1`.`Region`
    FROM (
        SELECT TOP @p + @p0 `u`.`Foo`, `u`.`CustomerID`, `u`.`Address`, `u`.`City`, `u`.`CompanyName`, `u`.`ContactName`, `u`.`ContactTitle`, `u`.`Country`, `u`.`Fax`, `u`.`Phone`, `u`.`PostalCode`, `u`.`Region`
        FROM (
            SELECT `c`.`City` AS `Foo`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
            FROM `Customers` AS `c`
            WHERE `c`.`City` = 'Berlin'
            UNION
            SELECT `c0`.`Region` AS `Foo`, `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
            FROM `Customers` AS `c0`
            WHERE `c0`.`City` = 'London'
        ) AS `u`
        ORDER BY `u`.`Foo`
    ) AS `u1`
    ORDER BY `u1`.`Foo` DESC
) AS `u0`
WHERE `u0`.`Foo` = 'Berlin'
ORDER BY `u0`.`Foo`
""");
        }

        public override async Task Union_Include(bool isAsync)
        {
            await base.Union_Include(isAsync);

            AssertSql(
                """
SELECT `u`.`CustomerID`, `u`.`Address`, `u`.`City`, `u`.`CompanyName`, `u`.`ContactName`, `u`.`ContactTitle`, `u`.`Country`, `u`.`Fax`, `u`.`Phone`, `u`.`PostalCode`, `u`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM (
    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    WHERE `c`.`City` = 'Berlin'
    UNION
    SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
    FROM `Customers` AS `c0`
    WHERE `c0`.`City` = 'London'
) AS `u`
LEFT JOIN `Orders` AS `o` ON `u`.`CustomerID` = `o`.`CustomerID`
ORDER BY `u`.`CustomerID`
""");
        }

        public override async Task Include_Union(bool isAsync)
        {
            await base.Include_Union(isAsync);

            AssertSql(
                """
SELECT `u`.`CustomerID`, `u`.`Address`, `u`.`City`, `u`.`CompanyName`, `u`.`ContactName`, `u`.`ContactTitle`, `u`.`Country`, `u`.`Fax`, `u`.`Phone`, `u`.`PostalCode`, `u`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM (
    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    WHERE `c`.`City` = 'Berlin'
    UNION
    SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
    FROM `Customers` AS `c0`
    WHERE `c0`.`City` = 'London'
) AS `u`
LEFT JOIN `Orders` AS `o` ON `u`.`CustomerID` = `o`.`CustomerID`
ORDER BY `u`.`CustomerID`
""");
        }

        public override async Task Select_Except_reference_projection(bool isAsync)
        {
            await base.Select_Except_reference_projection(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Orders` AS `o`
                    LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
                    EXCEPT
                    SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
                    FROM `Orders` AS `o0`
                    LEFT JOIN `Customers` AS `c0` ON `o0`.`CustomerID` = `c0`.`CustomerID`
                    WHERE `o0`.`CustomerID` = 'ALFKI'
                    """);
        }

        public override async Task SubSelect_Union(bool isAsync)
        {
            await base.SubSelect_Union(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, (
                        SELECT COUNT(*)
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID`) AS `Orders`
                    FROM `Customers` AS `c`
                    UNION
                    SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`, (
                        SELECT COUNT(*)
                        FROM `Orders` AS `o0`
                        WHERE `c0`.`CustomerID` = `o0`.`CustomerID`) AS `Orders`
                    FROM `Customers` AS `c0`
                    """);
        }

        public override async Task GroupBy_Select_Union(bool isAsync)
        {
            await base.GroupBy_Select_Union(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, COUNT(*) AS `Count`
                    FROM `Customers` AS `c`
                    WHERE `c`.`City` = 'Berlin'
                    GROUP BY `c`.`CustomerID`
                    UNION
                    SELECT `c0`.`CustomerID`, COUNT(*) AS `Count`
                    FROM `Customers` AS `c0`
                    WHERE `c0`.`City` = 'London'
                    GROUP BY `c0`.`CustomerID`
                    """);
        }

        public override async Task Union_over_columns_with_different_nullability(bool isAsync)
        {
            await base.Union_over_columns_with_different_nullability(isAsync);

            AssertSql(
                $"""
                    SELECT 'NonNullableConstant' AS `c`
                    FROM `Customers` AS `c`
                    UNION ALL
                    SELECT NULL AS `c`
                    FROM `Customers` AS `c0`
                    """);
        }

        public override async Task Union_over_column_column(bool async)
        {
            await base.Union_over_column_column(async);

            AssertSql(
"""
SELECT `o`.`OrderID`
FROM `Orders` AS `o`
UNION
SELECT `o0`.`OrderID`
FROM `Orders` AS `o0`
""");
        }

        public override async Task Union_over_column_function(bool async)
        {
            await base.Union_over_column_function(async);

            AssertSql(
"""
SELECT `o`.`OrderID`
FROM `Orders` AS `o`
UNION
SELECT COUNT(*) AS `OrderID`
FROM `Orders` AS `o0`
GROUP BY `o0`.`OrderID`
""");
        }

        public override async Task Union_over_column_constant(bool async)
        {
            await base.Union_over_column_constant(async);

            AssertSql(
"""
SELECT `o`.`OrderID`
FROM `Orders` AS `o`
UNION
SELECT 8 AS `OrderID`
FROM `Orders` AS `o0`
""");
        }

        public override async Task Union_over_column_unary(bool async)
        {
            await base.Union_over_column_unary(async);

            AssertSql(
"""
SELECT `o`.`OrderID`
FROM `Orders` AS `o`
UNION
SELECT -`o0`.`OrderID` AS `OrderID`
FROM `Orders` AS `o0`
""");
        }

        public override async Task Union_over_column_binary(bool async)
        {
            await base.Union_over_column_binary(async);

            AssertSql(
"""
SELECT `o`.`OrderID`
FROM `Orders` AS `o`
UNION
SELECT `o0`.`OrderID` + 1 AS `OrderID`
FROM `Orders` AS `o0`
""");
        }

        public override async Task Union_over_column_scalarsubquery(bool async)
        {
            await base.Union_over_column_scalarsubquery(async);

            AssertSql(
"""
SELECT `o`.`OrderID`
FROM `Orders` AS `o`
UNION
SELECT (
    SELECT COUNT(*)
    FROM `Order Details` AS `o1`
    WHERE `o0`.`OrderID` = `o1`.`OrderID`) AS `OrderID`
FROM `Orders` AS `o0`
""");
        }

        public override async Task Union_over_function_column(bool async)
        {
            await base.Union_over_function_column(async);

            AssertSql(
"""
SELECT COUNT(*) AS `c`
FROM `Orders` AS `o`
GROUP BY `o`.`OrderID`
UNION
SELECT `o0`.`OrderID` AS `c`
FROM `Orders` AS `o0`
""");
        }

        public override async Task Union_over_function_function(bool async)
        {
            await base.Union_over_function_function(async);

            AssertSql(
"""
SELECT COUNT(*) AS `c`
FROM `Orders` AS `o`
GROUP BY `o`.`OrderID`
UNION
SELECT COUNT(*) AS `c`
FROM `Orders` AS `o0`
GROUP BY `o0`.`OrderID`
""");
        }

        public override async Task Union_over_function_constant(bool async)
        {
            await base.Union_over_function_constant(async);

            AssertSql(
"""
SELECT COUNT(*) AS `c`
FROM `Orders` AS `o`
GROUP BY `o`.`OrderID`
UNION
SELECT 8 AS `c`
FROM `Orders` AS `o0`
""");
        }

        public override async Task Union_over_function_unary(bool async)
        {
            await base.Union_over_function_unary(async);

            AssertSql(
"""
SELECT COUNT(*) AS `c`
FROM `Orders` AS `o`
GROUP BY `o`.`OrderID`
UNION
SELECT -`o0`.`OrderID` AS `c`
FROM `Orders` AS `o0`
""");
        }

        public override async Task Union_over_function_binary(bool async)
        {
            await base.Union_over_function_binary(async);

            AssertSql(
"""
SELECT COUNT(*) AS `c`
FROM `Orders` AS `o`
GROUP BY `o`.`OrderID`
UNION
SELECT `o0`.`OrderID` + 1 AS `c`
FROM `Orders` AS `o0`
""");
        }

        public override async Task Union_over_function_scalarsubquery(bool async)
        {
            await base.Union_over_function_scalarsubquery(async);

            AssertSql(
"""
SELECT COUNT(*) AS `c`
FROM `Orders` AS `o`
GROUP BY `o`.`OrderID`
UNION
SELECT (
    SELECT COUNT(*)
    FROM `Order Details` AS `o1`
    WHERE `o0`.`OrderID` = `o1`.`OrderID`) AS `c`
FROM `Orders` AS `o0`
""");
        }

        public override async Task Union_over_constant_column(bool async)
        {
            await base.Union_over_constant_column(async);

            AssertSql(
"""
SELECT 8 AS `c`
FROM `Orders` AS `o`
UNION
SELECT `o0`.`OrderID` AS `c`
FROM `Orders` AS `o0`
""");
        }

        public override async Task Union_over_constant_function(bool async)
        {
            await base.Union_over_constant_function(async);

            AssertSql(
"""
SELECT 8 AS `c`
FROM `Orders` AS `o`
UNION
SELECT COUNT(*) AS `c`
FROM `Orders` AS `o0`
GROUP BY `o0`.`OrderID`
""");
        }

        public override async Task Union_over_constant_constant(bool async)
        {
            await base.Union_over_constant_constant(async);

            AssertSql(
"""
SELECT 8 AS `c`
FROM `Orders` AS `o`
UNION
SELECT 8 AS `c`
FROM `Orders` AS `o0`
""");
        }

        public override async Task Union_over_constant_unary(bool async)
        {
            await base.Union_over_constant_unary(async);

            AssertSql(
"""
SELECT 8 AS `c`
FROM `Orders` AS `o`
UNION
SELECT -`o0`.`OrderID` AS `c`
FROM `Orders` AS `o0`
""");
        }

        public override async Task Union_over_constant_binary(bool async)
        {
            await base.Union_over_constant_binary(async);

            AssertSql(
"""
SELECT 8 AS `c`
FROM `Orders` AS `o`
UNION
SELECT `o0`.`OrderID` + 1 AS `c`
FROM `Orders` AS `o0`
""");
        }

        public override async Task Union_over_constant_scalarsubquery(bool async)
        {
            await base.Union_over_constant_scalarsubquery(async);

            AssertSql(
"""
SELECT 8 AS `c`
FROM `Orders` AS `o`
UNION
SELECT (
    SELECT COUNT(*)
    FROM `Order Details` AS `o1`
    WHERE `o0`.`OrderID` = `o1`.`OrderID`) AS `c`
FROM `Orders` AS `o0`
""");
        }

        public override async Task Union_over_unary_column(bool async)
        {
            await base.Union_over_unary_column(async);

            AssertSql(
"""
SELECT -`o`.`OrderID` AS `c`
FROM `Orders` AS `o`
UNION
SELECT `o0`.`OrderID` AS `c`
FROM `Orders` AS `o0`
""");
        }

        public override async Task Union_over_unary_function(bool async)
        {
            await base.Union_over_unary_function(async);

            AssertSql(
"""
SELECT -`o`.`OrderID` AS `c`
FROM `Orders` AS `o`
UNION
SELECT COUNT(*) AS `c`
FROM `Orders` AS `o0`
GROUP BY `o0`.`OrderID`
""");
        }

        public override async Task Union_over_unary_constant(bool async)
        {
            await base.Union_over_unary_constant(async);

            AssertSql(
"""
SELECT -`o`.`OrderID` AS `c`
FROM `Orders` AS `o`
UNION
SELECT 8 AS `c`
FROM `Orders` AS `o0`
""");
        }

        public override async Task Union_over_unary_unary(bool async)
        {
            await base.Union_over_unary_unary(async);

            AssertSql(
"""
SELECT -`o`.`OrderID` AS `c`
FROM `Orders` AS `o`
UNION
SELECT -`o0`.`OrderID` AS `c`
FROM `Orders` AS `o0`
""");
        }

        public override async Task Union_over_unary_binary(bool async)
        {
            await base.Union_over_unary_binary(async);

            AssertSql(
"""
SELECT -`o`.`OrderID` AS `c`
FROM `Orders` AS `o`
UNION
SELECT `o0`.`OrderID` + 1 AS `c`
FROM `Orders` AS `o0`
""");
        }

        public override async Task Union_over_unary_scalarsubquery(bool async)
        {
            await base.Union_over_unary_scalarsubquery(async);

            AssertSql(
"""
SELECT -`o`.`OrderID` AS `c`
FROM `Orders` AS `o`
UNION
SELECT (
    SELECT COUNT(*)
    FROM `Order Details` AS `o1`
    WHERE `o0`.`OrderID` = `o1`.`OrderID`) AS `c`
FROM `Orders` AS `o0`
""");
        }

        public override async Task Union_over_binary_column(bool async)
        {
            await base.Union_over_binary_column(async);

            AssertSql(
"""
SELECT `o`.`OrderID` + 1 AS `c`
FROM `Orders` AS `o`
UNION
SELECT `o0`.`OrderID` AS `c`
FROM `Orders` AS `o0`
""");
        }

        public override async Task Union_over_binary_function(bool async)
        {
            await base.Union_over_binary_function(async);

            AssertSql(
"""
SELECT `o`.`OrderID` + 1 AS `c`
FROM `Orders` AS `o`
UNION
SELECT COUNT(*) AS `c`
FROM `Orders` AS `o0`
GROUP BY `o0`.`OrderID`
""");
        }

        public override async Task Union_over_binary_constant(bool async)
        {
            await base.Union_over_binary_constant(async);

            AssertSql(
"""
SELECT `o`.`OrderID` + 1 AS `c`
FROM `Orders` AS `o`
UNION
SELECT 8 AS `c`
FROM `Orders` AS `o0`
""");
        }

        public override async Task Union_over_binary_unary(bool async)
        {
            await base.Union_over_binary_unary(async);

            AssertSql(
"""
SELECT `o`.`OrderID` + 1 AS `c`
FROM `Orders` AS `o`
UNION
SELECT -`o0`.`OrderID` AS `c`
FROM `Orders` AS `o0`
""");
        }

        public override async Task Union_over_binary_binary(bool async)
        {
            await base.Union_over_binary_binary(async);

            AssertSql(
"""
SELECT `o`.`OrderID` + 1 AS `c`
FROM `Orders` AS `o`
UNION
SELECT `o0`.`OrderID` + 1 AS `c`
FROM `Orders` AS `o0`
""");
        }

        public override async Task Union_over_binary_scalarsubquery(bool async)
        {
            await base.Union_over_binary_scalarsubquery(async);

            AssertSql(
"""
SELECT `o`.`OrderID` + 1 AS `c`
FROM `Orders` AS `o`
UNION
SELECT (
    SELECT COUNT(*)
    FROM `Order Details` AS `o1`
    WHERE `o0`.`OrderID` = `o1`.`OrderID`) AS `c`
FROM `Orders` AS `o0`
""");
        }

        public override async Task Union_over_scalarsubquery_column(bool async)
        {
            await base.Union_over_scalarsubquery_column(async);

            AssertSql(
"""
SELECT (
    SELECT COUNT(*)
    FROM `Order Details` AS `o0`
    WHERE `o`.`OrderID` = `o0`.`OrderID`) AS `c`
FROM `Orders` AS `o`
UNION
SELECT `o1`.`OrderID` AS `c`
FROM `Orders` AS `o1`
""");
        }

        public override async Task Union_over_scalarsubquery_function(bool async)
        {
            await base.Union_over_scalarsubquery_function(async);

            AssertSql(
"""
SELECT (
    SELECT COUNT(*)
    FROM `Order Details` AS `o0`
    WHERE `o`.`OrderID` = `o0`.`OrderID`) AS `c`
FROM `Orders` AS `o`
UNION
SELECT COUNT(*) AS `c`
FROM `Orders` AS `o1`
GROUP BY `o1`.`OrderID`
""");
        }

        public override async Task Union_over_scalarsubquery_constant(bool async)
        {
            await base.Union_over_scalarsubquery_constant(async);

            AssertSql(
"""
SELECT (
    SELECT COUNT(*)
    FROM `Order Details` AS `o0`
    WHERE `o`.`OrderID` = `o0`.`OrderID`) AS `c`
FROM `Orders` AS `o`
UNION
SELECT 8 AS `c`
FROM `Orders` AS `o1`
""");
        }

        public override async Task Union_over_scalarsubquery_unary(bool async)
        {
            await base.Union_over_scalarsubquery_unary(async);

            AssertSql(
"""
SELECT (
    SELECT COUNT(*)
    FROM `Order Details` AS `o0`
    WHERE `o`.`OrderID` = `o0`.`OrderID`) AS `c`
FROM `Orders` AS `o`
UNION
SELECT -`o1`.`OrderID` AS `c`
FROM `Orders` AS `o1`
""");
        }

        public override async Task Union_over_scalarsubquery_binary(bool async)
        {
            await base.Union_over_scalarsubquery_binary(async);

            AssertSql(
"""
SELECT (
    SELECT COUNT(*)
    FROM `Order Details` AS `o0`
    WHERE `o`.`OrderID` = `o0`.`OrderID`) AS `c`
FROM `Orders` AS `o`
UNION
SELECT `o1`.`OrderID` + 1 AS `c`
FROM `Orders` AS `o1`
""");
        }

        public override async Task Union_over_scalarsubquery_scalarsubquery(bool async)
        {
            await base.Union_over_scalarsubquery_scalarsubquery(async);

            AssertSql(
"""
SELECT (
    SELECT COUNT(*)
    FROM `Order Details` AS `o0`
    WHERE `o`.`OrderID` = `o0`.`OrderID`) AS `c`
FROM `Orders` AS `o`
UNION
SELECT (
    SELECT COUNT(*)
    FROM `Order Details` AS `o2`
    WHERE `o1`.`OrderID` = `o2`.`OrderID`) AS `c`
FROM `Orders` AS `o1`
""");
        }

        public override async Task Union_over_OrderBy_Take1(bool async)
        {
            await base.Union_over_OrderBy_Take1(async);

            AssertSql(
                """
SELECT `o1`.`OrderID`
FROM (
    SELECT TOP @p `o`.`OrderID`
    FROM `Orders` AS `o`
    ORDER BY `o`.`OrderDate`
) AS `o1`
UNION
SELECT `o0`.`OrderID`
FROM `Orders` AS `o0`
""");
        }

        public override async Task Union_over_OrderBy_without_Skip_Take1(bool async)
        {
            await base.Union_over_OrderBy_without_Skip_Take1(async);

            AssertSql(
"""
SELECT `o`.`OrderID`
FROM `Orders` AS `o`
UNION
SELECT `o0`.`OrderID`
FROM `Orders` AS `o0`
""");
        }

        public override async Task Union_over_OrderBy_Take2(bool async)
        {
            await base.Union_over_OrderBy_Take2(async);

            AssertSql(
                """
SELECT `o`.`OrderID`
FROM `Orders` AS `o`
UNION
SELECT `o1`.`OrderID`
FROM (
    SELECT TOP @p `o0`.`OrderID`
    FROM `Orders` AS `o0`
    ORDER BY `o0`.`OrderDate`
) AS `o1`
""");
        }

        public override async Task Union_over_OrderBy_without_Skip_Take2(bool async)
        {
            await base.Union_over_OrderBy_without_Skip_Take2(async);

            AssertSql(
"""
SELECT `o`.`OrderID`
FROM `Orders` AS `o`
UNION
SELECT `o0`.`OrderID`
FROM `Orders` AS `o0`
""");
        }

        public override async Task OrderBy_Take_Union(bool isAsync)
        {
            await base.OrderBy_Take_Union(isAsync);

            AssertSql(
                """
SELECT `c1`.`CustomerID`, `c1`.`Address`, `c1`.`City`, `c1`.`CompanyName`, `c1`.`ContactName`, `c1`.`ContactTitle`, `c1`.`Country`, `c1`.`Fax`, `c1`.`Phone`, `c1`.`PostalCode`, `c1`.`Region`
FROM (
    SELECT TOP @p `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    ORDER BY `c`.`ContactName`
) AS `c1`
UNION
SELECT `c2`.`CustomerID`, `c2`.`Address`, `c2`.`City`, `c2`.`CompanyName`, `c2`.`ContactName`, `c2`.`ContactTitle`, `c2`.`Country`, `c2`.`Fax`, `c2`.`Phone`, `c2`.`PostalCode`, `c2`.`Region`
FROM (
    SELECT TOP @p `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
    FROM `Customers` AS `c0`
    ORDER BY `c0`.`ContactName`
) AS `c2`
""");
        }

        public override async Task Collection_projection_after_set_operation(bool async)
        {
            await base.Collection_projection_after_set_operation(async);

            AssertSql(
                """
SELECT `u`.`CustomerID`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM (
    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    WHERE `c`.`City` = 'Seatte'
    UNION
    SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
    FROM `Customers` AS `c0`
    WHERE `c0`.`CustomerID` LIKE 'F%'
) AS `u`
LEFT JOIN `Orders` AS `o` ON `u`.`CustomerID` = `o`.`CustomerID`
ORDER BY `u`.`CustomerID`
""");
        }

        public override async Task Concat_with_one_side_being_GroupBy_aggregate(bool async)
        {
            await base.Concat_with_one_side_being_GroupBy_aggregate(async);

            AssertSql(
"""
SELECT `o`.`OrderDate`
FROM `Orders` AS `o`
LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
WHERE `c`.`City` = 'Seatte'
UNION
SELECT MAX(`o0`.`OrderDate`) AS `OrderDate`
FROM `Orders` AS `o0`
GROUP BY `o0`.`CustomerID`
""");
        }

        public override async Task Union_on_entity_with_correlated_collection(bool async)
        {
            await base.Union_on_entity_with_correlated_collection(async);

            AssertSql(
                """
SELECT `u`.`CustomerID`, `o1`.`OrderID`, `o1`.`CustomerID`, `o1`.`EmployeeID`, `o1`.`OrderDate`
FROM (
    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Orders` AS `o`
    LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
    WHERE `c`.`City` = 'Seatte'
    UNION
    SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
    FROM `Orders` AS `o0`
    LEFT JOIN `Customers` AS `c0` ON `o0`.`CustomerID` = `c0`.`CustomerID`
    WHERE `o0`.`OrderID` < 10250
) AS `u`
LEFT JOIN `Orders` AS `o1` ON `u`.`CustomerID` = `o1`.`CustomerID`
ORDER BY `u`.`CustomerID`
""");
        }

        public override async Task Union_on_entity_plus_other_column_with_correlated_collection(bool async)
        {
            await base.Union_on_entity_plus_other_column_with_correlated_collection(async);

            AssertSql(
                """
SELECT `u`.`OrderDate`, `u`.`CustomerID`, `o1`.`OrderID`, `o1`.`CustomerID`, `o1`.`EmployeeID`, `o1`.`OrderDate`
FROM (
    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o`.`OrderDate`
    FROM `Orders` AS `o`
    LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
    WHERE `c`.`City` = 'Seatte'
    UNION
    SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`, `o0`.`OrderDate`
    FROM `Orders` AS `o0`
    LEFT JOIN `Customers` AS `c0` ON `o0`.`CustomerID` = `c0`.`CustomerID`
    WHERE `o0`.`OrderID` < 10250
) AS `u`
LEFT JOIN `Orders` AS `o1` ON `u`.`CustomerID` = `o1`.`CustomerID`
ORDER BY `u`.`CustomerID`, `u`.`OrderDate`
""");
        }

        public override async Task Except_non_entity(bool async)
        {
            await base.Except_non_entity(async);

            AssertSql(
    """
SELECT [c].[CustomerID]
FROM [Customers] AS [c]
WHERE [c].[ContactTitle] = N'Owner'
EXCEPT
SELECT [c0].[CustomerID]
FROM [Customers] AS [c0]
WHERE [c0].[City] = N'M�xico D.F.'
""");
        }

        public override async Task Except_simple_followed_by_projecting_constant(bool async)
        {
            await base.Except_simple_followed_by_projecting_constant(async);

            AssertSql(
    """
SELECT 1
FROM (
    SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region]
    FROM [Customers] AS [c]
    EXCEPT
    SELECT [c0].[CustomerID], [c0].[Address], [c0].[City], [c0].[CompanyName], [c0].[ContactName], [c0].[ContactTitle], [c0].[Country], [c0].[Fax], [c0].[Phone], [c0].[PostalCode], [c0].[Region]
    FROM [Customers] AS [c0]
) AS [t]
""");
        }

        public override async Task Except_nested(bool async)
        {
            await base.Except_nested(async);

            AssertSql(
    """
SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region]
FROM [Customers] AS [c]
WHERE [c].[ContactTitle] = N'Owner'
EXCEPT
SELECT [c0].[CustomerID], [c0].[Address], [c0].[City], [c0].[CompanyName], [c0].[ContactName], [c0].[ContactTitle], [c0].[Country], [c0].[Fax], [c0].[Phone], [c0].[PostalCode], [c0].[Region]
FROM [Customers] AS [c0]
WHERE [c0].[City] = N'M�xico D.F.'
EXCEPT
SELECT [c1].[CustomerID], [c1].[Address], [c1].[City], [c1].[CompanyName], [c1].[ContactName], [c1].[ContactTitle], [c1].[Country], [c1].[Fax], [c1].[Phone], [c1].[PostalCode], [c1].[Region]
FROM [Customers] AS [c1]
WHERE [c1].[City] = N'Seattle'
""");
        }

        public override async Task Except_nested2(bool async)
        {
            await base.Except_nested2(async);

            AssertSql(
                """
SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region]
FROM [Customers] AS [c]
EXCEPT
(
    SELECT [c0].[CustomerID], [c0].[Address], [c0].[City], [c0].[CompanyName], [c0].[ContactName], [c0].[ContactTitle], [c0].[Country], [c0].[Fax], [c0].[Phone], [c0].[PostalCode], [c0].[Region]
    FROM [Customers] AS [c0]
    WHERE [c0].[City] = N'Seattle'
    EXCEPT
    SELECT [c1].[CustomerID], [c1].[Address], [c1].[City], [c1].[CompanyName], [c1].[ContactName], [c1].[ContactTitle], [c1].[Country], [c1].[Fax], [c1].[Phone], [c1].[PostalCode], [c1].[Region]
    FROM [Customers] AS [c1]
    WHERE [c1].[City] = N'Seattle'
)
""");
        }

        public override async Task Intersect_non_entity(bool async)
        {
            await base.Intersect_non_entity(async);

            AssertSql(
    """
SELECT [c].[CustomerID]
FROM [Customers] AS [c]
WHERE [c].[City] = N'M�xico D.F.'
INTERSECT
SELECT [c0].[CustomerID]
FROM [Customers] AS [c0]
WHERE [c0].[ContactTitle] = N'Owner'
""");
        }

        public override async Task Intersect_nested(bool async)
        {
            await base.Intersect_nested(async);

            AssertSql(
    """
SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region]
FROM [Customers] AS [c]
WHERE [c].[City] = N'M�xico D.F.'
INTERSECT
SELECT [c0].[CustomerID], [c0].[Address], [c0].[City], [c0].[CompanyName], [c0].[ContactName], [c0].[ContactTitle], [c0].[Country], [c0].[Fax], [c0].[Phone], [c0].[PostalCode], [c0].[Region]
FROM [Customers] AS [c0]
WHERE [c0].[ContactTitle] = N'Owner'
INTERSECT
SELECT [c1].[CustomerID], [c1].[Address], [c1].[City], [c1].[CompanyName], [c1].[ContactName], [c1].[ContactTitle], [c1].[Country], [c1].[Fax], [c1].[Phone], [c1].[PostalCode], [c1].[Region]
FROM [Customers] AS [c1]
WHERE [c1].[Fax] IS NOT NULL
""");
        }

        public override async Task Concat_nested(bool async)
        {
            await base.Concat_nested(async);

            AssertSql(
"""
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = 'México D.F.'
UNION ALL
SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
FROM `Customers` AS `c0`
WHERE `c0`.`City` = 'Berlin'
UNION ALL
SELECT `c1`.`CustomerID`, `c1`.`Address`, `c1`.`City`, `c1`.`CompanyName`, `c1`.`ContactName`, `c1`.`ContactTitle`, `c1`.`Country`, `c1`.`Fax`, `c1`.`Phone`, `c1`.`PostalCode`, `c1`.`Region`
FROM `Customers` AS `c1`
WHERE `c1`.`City` = 'London'
""");
        }

        public override async Task Union_nested(bool async)
        {
            await base.Union_nested(async);

            AssertSql(
"""
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`ContactTitle` = 'Owner'
UNION
SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
FROM `Customers` AS `c0`
WHERE `c0`.`City` = 'México D.F.'
UNION
SELECT `c1`.`CustomerID`, `c1`.`Address`, `c1`.`City`, `c1`.`CompanyName`, `c1`.`ContactName`, `c1`.`ContactTitle`, `c1`.`Country`, `c1`.`Fax`, `c1`.`Phone`, `c1`.`PostalCode`, `c1`.`Region`
FROM `Customers` AS `c1`
WHERE `c1`.`City` = 'London'
""");
        }

        public override async Task Union_non_entity(bool async)
        {
            await base.Union_non_entity(async);

            AssertSql(
"""
SELECT `c`.`CustomerID`
FROM `Customers` AS `c`
WHERE `c`.`ContactTitle` = 'Owner'
UNION
SELECT `c0`.`CustomerID`
FROM `Customers` AS `c0`
WHERE `c0`.`City` = 'México D.F.'
""");
        }

        public override async Task Concat_non_entity(bool async)
        {
            await base.Concat_non_entity(async);

            AssertSql(
"""
SELECT `c`.`CustomerID`
FROM `Customers` AS `c`
WHERE `c`.`City` = 'México D.F.'
UNION ALL
SELECT `c0`.`CustomerID`
FROM `Customers` AS `c0`
WHERE `c0`.`ContactTitle` = 'Owner'
""");
        }

        public override async Task Collection_projection_after_set_operation_fails_if_distinct(bool async)
        {
            await base.Collection_projection_after_set_operation_fails_if_distinct(async);

            AssertSql();
        }

        public override async Task Collection_projection_before_set_operation_fails(bool async)
        {
            await base.Collection_projection_before_set_operation_fails(async);

            AssertSql();
        }

        public override async Task Intersect_on_distinct(bool async)
        {
            await base.Intersect_on_distinct(async);

            AssertSql(
                """
SELECT [c].[CompanyName]
FROM [Customers] AS [c]
WHERE [c].[City] = N'México D.F.'
INTERSECT
SELECT [c0].[CompanyName]
FROM [Customers] AS [c0]
WHERE [c0].[ContactTitle] = N'Owner'
""");
        }

        public override async Task Union_on_distinct(bool async)
        {
            await base.Union_on_distinct(async);

            AssertSql(
                """
SELECT `c`.`CompanyName`
FROM `Customers` AS `c`
WHERE `c`.`City` = 'México D.F.'
UNION
SELECT `c0`.`CompanyName`
FROM `Customers` AS `c0`
WHERE `c0`.`ContactTitle` = 'Owner'
""");
        }

        public override async Task Except_on_distinct(bool async)
        {
            await base.Except_on_distinct(async);

            AssertSql(
                """
SELECT [c].[CompanyName]
FROM [Customers] AS [c]
WHERE [c].[City] = N'México D.F.'
EXCEPT
SELECT [c0].[CompanyName]
FROM [Customers] AS [c0]
WHERE [c0].[ContactTitle] = N'Owner'
""");
        }

        public override async Task Include_Union_only_on_one_side_throws(bool async)
        {
            await base.Include_Union_only_on_one_side_throws(async);

            AssertSql();
        }

        public override async Task Include_Union_different_includes_throws(bool async)
        {
            await base.Include_Union_different_includes_throws(async);

            AssertSql();
        }

        public override async Task Concat_with_pruning(bool async)
        {
            await base.Concat_with_pruning(async);

            AssertSql(
"""
SELECT `c`.`City`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` LIKE 'A%'
UNION ALL
SELECT `c0`.`City`
FROM `Customers` AS `c0`
WHERE `c0`.`CustomerID` LIKE 'B%'
""");
        }

        public override async Task Concat_with_distinct_on_one_source_and_pruning(bool async)
        {
            await base.Concat_with_distinct_on_one_source_and_pruning(async);

            AssertSql(
                """
SELECT `u`.`City`
FROM (
    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    WHERE `c`.`CustomerID` LIKE 'A%'
    UNION ALL
    SELECT DISTINCT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
    FROM `Customers` AS `c0`
    WHERE `c0`.`CustomerID` LIKE 'B%'
) AS `u`
""");
        }

        public override async Task Concat_with_distinct_on_both_source_and_pruning(bool async)
        {
            await base.Concat_with_distinct_on_both_source_and_pruning(async);

            AssertSql(
                """
SELECT `u`.`City`
FROM (
    SELECT DISTINCT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    WHERE `c`.`CustomerID` LIKE 'A%'
    UNION ALL
    SELECT DISTINCT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
    FROM `Customers` AS `c0`
    WHERE `c0`.`CustomerID` LIKE 'B%'
) AS `u`
""");
        }

        public override async Task Nested_concat_with_pruning(bool async)
        {
            await base.Nested_concat_with_pruning(async);

            AssertSql(
"""
SELECT `c`.`City`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` LIKE 'A%'
UNION ALL
SELECT `c0`.`City`
FROM `Customers` AS `c0`
WHERE `c0`.`CustomerID` LIKE 'B%'
UNION ALL
SELECT `c1`.`City`
FROM `Customers` AS `c1`
WHERE `c1`.`CustomerID` LIKE 'A%'
""");
        }

        public override async Task Nested_concat_with_distinct_in_the_middle_and_pruning(bool async)
        {
            await base.Nested_concat_with_distinct_in_the_middle_and_pruning(async);

            AssertSql(
                """
SELECT `u`.`City`
FROM (
    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    WHERE `c`.`CustomerID` LIKE 'A%'
    UNION ALL
    SELECT DISTINCT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
    FROM `Customers` AS `c0`
    WHERE `c0`.`CustomerID` LIKE 'B%'
) AS `u`
UNION ALL
SELECT `c1`.`City`
FROM `Customers` AS `c1`
WHERE `c1`.`CustomerID` LIKE 'A%'
""");
        }

        public override async Task Client_eval_Union_FirstOrDefault(bool async)
        {
            // Client evaluation in projection. Issue #16243.
            Assert.Equal(
                RelationalStrings.SetOperationsNotAllowedAfterClientEvaluation,
                (await Assert.ThrowsAsync<InvalidOperationException>(
                    () => base.Client_eval_Union_FirstOrDefault(async))).Message);

            AssertSql();
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task Union_with_different_store_types_is_fine_if_database_can_translate_it(bool async)
        {
            await AssertQuery(
                async,
                ss => ss.Set<Customer>()
                    .Select(e => e.CompanyName)
                    .Union(ss.Set<Customer>().Select(e => e.ContactName)));

            AssertSql(
"""
SELECT `c`.`CompanyName`
FROM `Customers` AS `c`
UNION
SELECT `c0`.`ContactName` AS `CompanyName`
FROM `Customers` AS `c0`
""");
        }

        [ConditionalTheory] // Issue #29020
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task Union_with_type_mappings_to_same_store_type(bool async)
        {
            await AssertQuery(
                async,
                ss => ss.Set<Customer>()
                    .Select(e => e.ContactName)
                    .Union(ss.Set<Customer>().Select(e => e.ContactTitle)));

            AssertSql(
"""
SELECT `c`.`ContactName`
FROM `Customers` AS `c`
UNION
SELECT `c0`.`ContactTitle` AS `ContactName`
FROM `Customers` AS `c0`
""");
        }

        private void AssertSql(params string[] expected)
            => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);

        protected override void ClearLog()
            => Fixture.TestSqlLoggerFactory.Clear();
    }
}
