﻿// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using EntityFrameworkCore.Jet.FunctionalTests.TestUtilities;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;
using Xunit.Abstractions;

namespace EntityFrameworkCore.Jet.FunctionalTests.Query
{
    public class NorthwindJoinQueryJetTest : NorthwindJoinQueryRelationalTestBase<NorthwindQueryJetFixture<NoopModelCustomizer>>
    {
        public NorthwindJoinQueryJetTest(
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

        public override async Task Join_customers_orders_projection(bool isAsync)
        {
            await base.Join_customers_orders_projection(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`ContactName`, `o`.`OrderID`
                    FROM `Customers` AS `c`
                    INNER JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
                    """);
        }

        public override async Task Join_customers_orders_entities(bool isAsync)
        {
            await base.Join_customers_orders_entities(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
                    FROM `Customers` AS `c`
                    INNER JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
                    WHERE `c`.`CustomerID` LIKE 'F%'
                    """);
        }

        public override async Task Join_select_many(bool isAsync)
        {
            await base.Join_select_many(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
                    FROM `Customers` AS `c`
                    INNER JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`,
                    `Employees` AS `e`
                    WHERE `c`.`CustomerID` LIKE 'F%'
                    """);
        }

        public override async Task Client_Join_select_many(bool isAsync)
        {
            await base.Client_Join_select_many(isAsync);

            AssertSql();
        }

        public override async Task Join_customers_orders_select(bool isAsync)
        {
            await base.Join_customers_orders_select(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`ContactName`, `o`.`OrderID`
                    FROM `Customers` AS `c`
                    INNER JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
                    """);
        }

        public override async Task Join_customers_orders_with_subquery(bool isAsync)
        {
            await base.Join_customers_orders_with_subquery(isAsync);

            AssertSql(
                """
SELECT `c`.`ContactName`, `o`.`OrderID`
FROM `Customers` AS `c`
INNER JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
WHERE `o`.`CustomerID` = 'ALFKI'
""");
        }

        public override async Task Join_customers_orders_with_subquery_with_take(bool isAsync)
        {
            await base.Join_customers_orders_with_subquery_with_take(isAsync);

            AssertSql(
                """
SELECT `c`.`ContactName`, `o0`.`OrderID`
FROM `Customers` AS `c`
INNER JOIN (
    SELECT TOP @p `o`.`OrderID`, `o`.`CustomerID`
    FROM `Orders` AS `o`
    ORDER BY `o`.`OrderID`
) AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE `o0`.`CustomerID` = 'HANAR'
""");
        }

        public override async Task Join_customers_orders_with_subquery_anonymous_property_method(bool isAsync)
        {
            await base.Join_customers_orders_with_subquery_anonymous_property_method(isAsync);

            AssertSql(
                """
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Customers` AS `c`
INNER JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
WHERE `o`.`CustomerID` = 'ALFKI'
""");
        }

        public override async Task Join_customers_orders_with_subquery_anonymous_property_method_with_take(bool isAsync)
        {
            await base.Join_customers_orders_with_subquery_anonymous_property_method_with_take(isAsync);

            AssertSql(
                """
SELECT `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM `Customers` AS `c`
INNER JOIN (
    SELECT TOP @p `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
    FROM `Orders` AS `o`
    ORDER BY `o`.`OrderID`
) AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE `o0`.`CustomerID` = 'HANAR'
""");
        }

        public override async Task Join_customers_orders_with_subquery_predicate(bool isAsync)
        {
            await base.Join_customers_orders_with_subquery_predicate(isAsync);

            AssertSql(
                """
SELECT `c`.`ContactName`, `o0`.`OrderID`
FROM `Customers` AS `c`
INNER JOIN (
    SELECT `o`.`OrderID`, `o`.`CustomerID`
    FROM `Orders` AS `o`
    WHERE `o`.`OrderID` > 0
) AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE `o0`.`CustomerID` = 'ALFKI'
""");
        }

        public override async Task Join_customers_orders_with_subquery_predicate_with_take(bool isAsync)
        {
            await base.Join_customers_orders_with_subquery_predicate_with_take(isAsync);

            AssertSql(
                """
SELECT `c`.`ContactName`, `o0`.`OrderID`
FROM `Customers` AS `c`
INNER JOIN (
    SELECT TOP @p `o`.`OrderID`, `o`.`CustomerID`
    FROM `Orders` AS `o`
    WHERE `o`.`OrderID` > 0
    ORDER BY `o`.`OrderID`
) AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE `o0`.`CustomerID` = 'HANAR'
""");
        }

        public override async Task Join_composite_key(bool isAsync)
        {
            await base.Join_composite_key(isAsync);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Customers` AS `c`
INNER JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
WHERE `c`.`CustomerID` LIKE 'F%'
""");
        }

        public override async Task Join_complex_condition(bool isAsync)
        {
            await base.Join_complex_condition(isAsync);

            AssertSql(
                """
SELECT `c`.`CustomerID`
FROM `Customers` AS `c`,
(
    SELECT 1
    FROM `Orders` AS `o`
    WHERE `o`.`OrderID` < 10250
) AS `o0`
WHERE `c`.`CustomerID` = 'ALFKI'
""");
        }

        public override async Task Join_same_collection_multiple(bool isAsync)
        {
            await base.Join_same_collection_multiple(isAsync);

            AssertSql(
                $"""
                    SELECT `c1`.`CustomerID`, `c1`.`Address`, `c1`.`City`, `c1`.`CompanyName`, `c1`.`ContactName`, `c1`.`ContactTitle`, `c1`.`Country`, `c1`.`Fax`, `c1`.`Phone`, `c1`.`PostalCode`, `c1`.`Region`
                    FROM (`Customers` AS `c`
                    INNER JOIN `Customers` AS `c0` ON `c`.`CustomerID` = `c0`.`CustomerID`)
                    INNER JOIN `Customers` AS `c1` ON `c`.`CustomerID` = `c1`.`CustomerID`
                    """);
        }

        public override async Task Join_same_collection_force_alias_uniquefication(bool isAsync)
        {
            await base.Join_same_collection_force_alias_uniquefication(isAsync);

            AssertSql(
"""
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM `Orders` AS `o`
INNER JOIN `Orders` AS `o0` ON `o`.`CustomerID` = `o0`.`CustomerID`
WHERE `o`.`CustomerID` LIKE 'F%'
""");
        }

        public override async Task LeftJoin(bool async)
        {
            await base.LeftJoin(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Customers` AS `c`
LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
""");
        }

        public override async Task RightJoin(bool async)
        {
            await base.RightJoin(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Customers` AS `c`
RIGHT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
""");
        }

        public override async Task GroupJoin_simple(bool isAsync)
        {
            await base.GroupJoin_simple(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
                    FROM `Customers` AS `c`
                    INNER JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
                    WHERE `c`.`CustomerID` LIKE 'F%'
                    """);
        }

        public override async Task GroupJoin_simple2(bool isAsync)
        {
            await base.GroupJoin_simple2(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    INNER JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
                    """);
        }

        public override async Task GroupJoin_simple3(bool isAsync)
        {
            await base.GroupJoin_simple3(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`OrderID`
                    FROM `Customers` AS `c`
                    INNER JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
                    """);
        }

        public override async Task GroupJoin_simple_ordering(bool isAsync)
        {
            await base.GroupJoin_simple_ordering(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
                    FROM `Customers` AS `c`
                    INNER JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
                    WHERE `c`.`CustomerID` LIKE 'F%'
                    ORDER BY `c`.`City`
                    """);
        }

        public override async Task GroupJoin_simple_subquery(bool isAsync)
        {
            await base.GroupJoin_simple_subquery(isAsync);

            AssertSql(
                """
SELECT `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM `Customers` AS `c`
INNER JOIN (
    SELECT TOP @p `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
    FROM `Orders` AS `o`
    ORDER BY `o`.`OrderID`
) AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
""");
        }

        public override async Task GroupJoin_as_final_operator(bool async)
        {
            await base.GroupJoin_as_final_operator(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Customers` AS `c`
LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
WHERE `c`.`CustomerID` LIKE 'F%'
ORDER BY `c`.`CustomerID`
""");
        }

        public override async Task Unflattened_GroupJoin_composed(bool async)
        {
            await base.Unflattened_GroupJoin_composed(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Customers` AS `c`
LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
WHERE (`c`.`CustomerID` LIKE 'F%') AND `c`.`City` = 'Lisboa'
ORDER BY `c`.`CustomerID`
""");
        }

        public override async Task Unflattened_GroupJoin_composed_2(bool async)
        {
            await base.Unflattened_GroupJoin_composed_2(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `c1`.`CustomerID`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `c1`.`Address`, `c1`.`City`, `c1`.`CompanyName`, `c1`.`ContactName`, `c1`.`ContactTitle`, `c1`.`Country`, `c1`.`Fax`, `c1`.`Phone`, `c1`.`PostalCode`, `c1`.`Region`
FROM (`Customers` AS `c`
INNER JOIN (
    SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
    FROM `Customers` AS `c0`
    WHERE `c0`.`City` = 'Lisboa'
) AS `c1` ON `c`.`CustomerID` = `c1`.`CustomerID`)
LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
WHERE `c`.`CustomerID` LIKE 'F%'
ORDER BY `c`.`CustomerID`, `c1`.`CustomerID`
""");
        }

        public override async Task GroupJoin_DefaultIfEmpty(bool isAsync)
        {
            await base.GroupJoin_DefaultIfEmpty(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
                    FROM `Customers` AS `c`
                    LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
                    WHERE `c`.`CustomerID` LIKE 'F%'
                    """);
        }

        public override async Task GroupJoin_DefaultIfEmpty_multiple(bool isAsync)
        {
            await base.GroupJoin_DefaultIfEmpty_multiple(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
                    FROM (`Customers` AS `c`
                    LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`)
                    LEFT JOIN `Orders` AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
                    WHERE `c`.`CustomerID` LIKE 'F%'
                    """);
        }

        public override async Task GroupJoin_DefaultIfEmpty2(bool isAsync)
        {
            await base.GroupJoin_DefaultIfEmpty2(isAsync);

            AssertSql(
                """
SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM `Employees` AS `e`
LEFT JOIN (
    SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
    FROM `Orders` AS `o`
    WHERE `o`.`CustomerID` LIKE 'F%'
) AS `o0` ON `e`.`EmployeeID` = `o0`.`EmployeeID`
""");
        }

        public override async Task GroupJoin_DefaultIfEmpty3(bool isAsync)
        {
            await base.GroupJoin_DefaultIfEmpty3(isAsync);

            AssertSql(
                """
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM (
    SELECT TOP @p `c`.`CustomerID`
    FROM `Customers` AS `c`
    ORDER BY `c`.`CustomerID`
) AS `c0`
LEFT JOIN `Orders` AS `o` ON `c0`.`CustomerID` = `o`.`CustomerID`
ORDER BY `c0`.`CustomerID`
""");
        }

        public override async Task GroupJoin_Where(bool isAsync)
        {
            await base.GroupJoin_Where(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
                    FROM `Customers` AS `c`
                    INNER JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
                    WHERE `o`.`CustomerID` = 'ALFKI'
                    """);
        }

        public override async Task GroupJoin_Where_OrderBy(bool isAsync)
        {
            await base.GroupJoin_Where_OrderBy(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
                    FROM `Customers` AS `c`
                    INNER JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
                    WHERE `o`.`CustomerID` = 'ALFKI' OR `c`.`CustomerID` = 'ANATR'
                    ORDER BY `c`.`City`
                    """);
        }

        public override async Task GroupJoin_DefaultIfEmpty_Where(bool isAsync)
        {
            await base.GroupJoin_DefaultIfEmpty_Where(isAsync);

            AssertSql(
"""
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Customers` AS `c`
LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
WHERE `o`.`OrderID` IS NOT NULL AND `o`.`CustomerID` = 'ALFKI'
""");
        }

        public override async Task Join_GroupJoin_DefaultIfEmpty_Where(bool isAsync)
        {
            await base.Join_GroupJoin_DefaultIfEmpty_Where(isAsync);

            AssertSql(
"""
SELECT `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM (`Customers` AS `c`
INNER JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`)
LEFT JOIN `Orders` AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE `o0`.`OrderID` IS NOT NULL AND `o0`.`CustomerID` = 'ALFKI'
""");
        }

        public override async Task GroupJoin_DefaultIfEmpty_Project(bool isAsync)
        {
            await base.GroupJoin_DefaultIfEmpty_Project(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`OrderID`
                    FROM `Customers` AS `c`
                    LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
                    """);
        }

        public override async Task GroupJoin_SelectMany_subquery_with_filter(bool isAsync)
        {
            await base.GroupJoin_SelectMany_subquery_with_filter(isAsync);

            AssertSql(
                """
SELECT `c`.`ContactName`, `o0`.`OrderID`
FROM `Customers` AS `c`
INNER JOIN (
    SELECT `o`.`OrderID`, `o`.`CustomerID`
    FROM `Orders` AS `o`
    WHERE `o`.`OrderID` > 5
) AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
""");
        }

        public override async Task GroupJoin_SelectMany_subquery_with_filter_orderby(bool isAsync)
        {
            await base.GroupJoin_SelectMany_subquery_with_filter_orderby(isAsync);

            AssertSql(
                """
SELECT `c`.`ContactName`, `o0`.`OrderID`
FROM `Customers` AS `c`
INNER JOIN (
    SELECT `o`.`OrderID`, `o`.`CustomerID`
    FROM `Orders` AS `o`
    WHERE `o`.`OrderID` > 5
) AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
""");
        }

        public override async Task GroupJoin_SelectMany_subquery_with_filter_and_DefaultIfEmpty(bool isAsync)
        {
            await base.GroupJoin_SelectMany_subquery_with_filter_and_DefaultIfEmpty(isAsync);

            AssertSql(
                """
SELECT `c`.`ContactName`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM `Customers` AS `c`
LEFT JOIN (
    SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
    FROM `Orders` AS `o`
    WHERE `o`.`OrderID` > 5
) AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE `c`.`CustomerID` LIKE 'F%'
""");
        }

        public override async Task GroupJoin_SelectMany_subquery_with_filter_orderby_and_DefaultIfEmpty(bool isAsync)
        {
            await base.GroupJoin_SelectMany_subquery_with_filter_orderby_and_DefaultIfEmpty(isAsync);

            AssertSql(
                """
SELECT `c`.`ContactName`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM `Customers` AS `c`
LEFT JOIN (
    SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
    FROM `Orders` AS `o`
    WHERE `o`.`OrderID` > 5
) AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE `c`.`CustomerID` LIKE 'F%'
""");
        }

        public override async Task GroupJoin_Subquery_with_Take_Then_SelectMany_Where(bool isAsync)
        {
            await base.GroupJoin_Subquery_with_Take_Then_SelectMany_Where(isAsync);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `o1`.`OrderID`
FROM `Customers` AS `c`
INNER JOIN (
    SELECT `o0`.`OrderID`, `o0`.`CustomerID`
    FROM (
        SELECT TOP @p `o`.`OrderID`, `o`.`CustomerID`
        FROM `Orders` AS `o`
        ORDER BY `o`.`OrderID`
    ) AS `o0`
    WHERE `o0`.`CustomerID` LIKE 'A%'
) AS `o1` ON `c`.`CustomerID` = `o1`.`CustomerID`
""");
        }

        public override async Task GroupJoin_aggregate_anonymous_key_selectors(bool async)
        {
            await base.GroupJoin_aggregate_anonymous_key_selectors(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, (
    SELECT IIF(SUM(IIF(LEN(`o`.`CustomerID`) IS NULL, NULL, CLNG(LEN(`o`.`CustomerID`)))) IS NULL, 0, SUM(IIF(LEN(`o`.`CustomerID`) IS NULL, NULL, CLNG(LEN(`o`.`CustomerID`)))))
    FROM `Orders` AS `o`
    WHERE `c`.`City` IS NOT NULL AND `c`.`CustomerID` = `o`.`CustomerID` AND `c`.`City` = 'London') AS `Sum`
FROM `Customers` AS `c`
""");
        }

        public override async Task GroupJoin_aggregate_anonymous_key_selectors2(bool async)
        {
            await base.GroupJoin_aggregate_anonymous_key_selectors2(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, (
    SELECT IIF(SUM(IIF(LEN(`o`.`CustomerID`) IS NULL, NULL, CLNG(LEN(`o`.`CustomerID`)))) IS NULL, 0, SUM(IIF(LEN(`o`.`CustomerID`) IS NULL, NULL, CLNG(LEN(`o`.`CustomerID`)))))
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` = `o`.`CustomerID` AND 1996 = DATEPART('yyyy', `o`.`OrderDate`)) AS `Sum`
FROM `Customers` AS `c`
""");
        }

        public override async Task GroupJoin_aggregate_anonymous_key_selectors_one_argument(bool async)
        {
            await base.GroupJoin_aggregate_anonymous_key_selectors_one_argument(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, (
    SELECT IIF(SUM(IIF(LEN(`o`.`CustomerID`) IS NULL, NULL, CLNG(LEN(`o`.`CustomerID`)))) IS NULL, 0, SUM(IIF(LEN(`o`.`CustomerID`) IS NULL, NULL, CLNG(LEN(`o`.`CustomerID`)))))
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` = `o`.`CustomerID`) AS `Sum`
FROM `Customers` AS `c`
""");
        }

        public override async Task GroupJoin_aggregate_nested_anonymous_key_selectors(bool async)
        {
            await base.GroupJoin_aggregate_nested_anonymous_key_selectors(async);

            AssertSql();
        }

        public override async Task Inner_join_with_tautology_predicate_converts_to_cross_join(bool async)
        {
            await base.Inner_join_with_tautology_predicate_converts_to_cross_join(async);

            AssertSql(
                """
SELECT `c0`.`CustomerID`, `o0`.`OrderID`
FROM (
    SELECT TOP @p `c`.`CustomerID`
    FROM `Customers` AS `c`
    ORDER BY `c`.`CustomerID`
) AS `c0`,
(
    SELECT TOP @p `o`.`OrderID`
    FROM `Orders` AS `o`
    ORDER BY `o`.`OrderID`
) AS `o0`
ORDER BY `c0`.`CustomerID`
""");
        }

        public override async Task Left_join_with_tautology_predicate_doesnt_convert_to_cross_join(bool async)
        {
            await base.Left_join_with_tautology_predicate_doesnt_convert_to_cross_join(async);

            AssertSql(
                """
@__p_0='10'

SELECT [t].[CustomerID], [t0].[OrderID]
FROM (
    SELECT TOP(@__p_0) [c].[CustomerID]
    FROM [Customers] AS [c]
    ORDER BY [c].[CustomerID]
) AS [t]
LEFT JOIN (
    SELECT TOP(@__p_0) [o].[OrderID]
    FROM [Orders] AS [o]
    ORDER BY [o].[OrderID]
) AS [t0] ON 1 = 1
ORDER BY [t].[CustomerID]
""");
        }

        public override async Task SelectMany_with_client_eval(bool async)
        {
            await base.SelectMany_with_client_eval(async);

            AssertSql(
                """
SELECT [t].[OrderID], [t].[CustomerID], [t].[EmployeeID], [t].[OrderDate], [t].[ContactName]
FROM [Customers] AS [c]
CROSS APPLY (
    SELECT [o].[OrderID], [o].[CustomerID], [o].[EmployeeID], [o].[OrderDate], [c].[ContactName]
    FROM [Orders] AS [o]
    WHERE [c].[CustomerID] = [o].[CustomerID]
) AS [t]
WHERE [c].[CustomerID] LIKE N'F%'
""");
        }

        public override async Task SelectMany_with_client_eval_with_collection_shaper(bool async)
        {
            await base.SelectMany_with_client_eval_with_collection_shaper(async);

            AssertSql(
                """
SELECT [t].[OrderID], [t].[CustomerID], [t].[EmployeeID], [t].[OrderDate], [c].[CustomerID], [o0].[OrderID], [o0].[ProductID], [o0].[Discount], [o0].[Quantity], [o0].[UnitPrice], [t].[ContactName]
FROM [Customers] AS [c]
CROSS APPLY (
    SELECT [o].[OrderID], [o].[CustomerID], [o].[EmployeeID], [o].[OrderDate], [c].[ContactName]
    FROM [Orders] AS [o]
    WHERE [c].[CustomerID] = [o].[CustomerID]
) AS [t]
LEFT JOIN [Order Details] AS [o0] ON [t].[OrderID] = [o0].[OrderID]
WHERE [c].[CustomerID] LIKE N'F%'
ORDER BY [c].[CustomerID], [t].[OrderID], [o0].[OrderID]
""");
        }

        public override async Task SelectMany_with_client_eval_with_collection_shaper_ignored(bool async)
        {
            await base.SelectMany_with_client_eval_with_collection_shaper_ignored(async);

            AssertSql(
                """
SELECT [t].[OrderID], [t].[CustomerID], [t].[EmployeeID], [t].[OrderDate], [t].[ContactName]
FROM [Customers] AS [c]
CROSS APPLY (
    SELECT [o].[OrderID], [o].[CustomerID], [o].[EmployeeID], [o].[OrderDate], [c].[ContactName]
    FROM [Orders] AS [o]
    WHERE [c].[CustomerID] = [o].[CustomerID]
) AS [t]
WHERE [c].[CustomerID] LIKE N'F%'
""");
        }

        public override async Task SelectMany_with_client_eval_with_constructor(bool async)
        {
            await base.SelectMany_with_client_eval_with_constructor(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`City`, `s`.`OrderID`, `s`.`ProductID`, `s`.`OrderID0`
FROM `Customers` AS `c`
LEFT JOIN (
    SELECT `o1`.`OrderID`, `o1`.`ProductID`, `o`.`OrderID` AS `OrderID0`, `o`.`CustomerID`
    FROM `Orders` AS `o`
    INNER JOIN (
        SELECT `o0`.`OrderID`, `o0`.`ProductID`
        FROM `Order Details` AS `o0`
        WHERE `o0`.`OrderID` < 11000
    ) AS `o1` ON `o`.`OrderID` = `o1`.`OrderID`
) AS `s` ON `c`.`CustomerID` = `s`.`CustomerID`
WHERE `c`.`CustomerID` LIKE 'A%'
ORDER BY `c`.`CustomerID`, `s`.`OrderID0`, `s`.`OrderID`
""");
        }

        public override async Task SelectMany_with_selecting_outer_entity(bool async)
        {
            await base.SelectMany_with_selecting_outer_entity(async);

            AssertSql(
                """
SELECT [t].[CustomerID], [t].[Address], [t].[City], [t].[CompanyName], [t].[ContactName], [t].[ContactTitle], [t].[Country], [t].[Fax], [t].[Phone], [t].[PostalCode], [t].[Region]
FROM [Customers] AS [c]
CROSS APPLY (
    SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region]
    FROM [Orders] AS [o]
    WHERE [c].[CustomerID] = [o].[CustomerID]
) AS [t]
""");
        }

        public override async Task SelectMany_with_selecting_outer_element(bool async)
        {
            await base.SelectMany_with_selecting_outer_element(async);

            AssertSql(
                """
SELECT [t].[c]
FROM [Customers] AS [c]
CROSS APPLY (
    SELECT [c].[CustomerID] + COALESCE([c].[City], N'') AS [c]
    FROM [Orders] AS [o]
    WHERE [c].[CustomerID] = [o].[CustomerID]
) AS [t]
""");
        }

        public override async Task SelectMany_with_selecting_outer_entity_column_and_inner_column(bool async)
        {
            await base.SelectMany_with_selecting_outer_entity_column_and_inner_column(async);

            AssertSql(
                """
SELECT [t].[City], [t].[OrderDate]
FROM [Customers] AS [c]
CROSS APPLY (
    SELECT [c].[City], [o].[OrderDate]
    FROM [Orders] AS [o]
    WHERE [c].[CustomerID] = [o].[CustomerID]
    ORDER BY [o].[OrderID]
    OFFSET 0 ROWS
) AS [t]
ORDER BY [c].[CustomerID]
""");
        }

        public override async Task SelectMany_correlated_subquery_take(bool async)
        {
            await base.SelectMany_correlated_subquery_take(async);

            AssertSql(
                """
SELECT [t0].[CustomerID], [t0].[Address], [t0].[City], [t0].[CompanyName], [t0].[ContactName], [t0].[ContactTitle], [t0].[Country], [t0].[Fax], [t0].[Phone], [t0].[PostalCode], [t0].[Region]
FROM [Customers] AS [c]
INNER JOIN (
    SELECT [t].[CustomerID], [t].[Address], [t].[City], [t].[CompanyName], [t].[ContactName], [t].[ContactTitle], [t].[Country], [t].[Fax], [t].[Phone], [t].[PostalCode], [t].[Region]
    FROM (
        SELECT [c0].[CustomerID], [c0].[Address], [c0].[City], [c0].[CompanyName], [c0].[ContactName], [c0].[ContactTitle], [c0].[Country], [c0].[Fax], [c0].[Phone], [c0].[PostalCode], [c0].[Region], ROW_NUMBER() OVER(PARTITION BY [c0].[CustomerID] ORDER BY [c0].[CustomerID] + COALESCE([c0].[City], N'')) AS [row]
        FROM [Customers] AS [c0]
    ) AS [t]
    WHERE [t].[row] <= 2
) AS [t0] ON [c].[CustomerID] = [t0].[CustomerID]
""");
        }

        public override async Task Distinct_SelectMany_correlated_subquery_take(bool async)
        {
            await base.Distinct_SelectMany_correlated_subquery_take(async);

            AssertSql(
                """
SELECT [t0].[CustomerID], [t0].[Address], [t0].[City], [t0].[CompanyName], [t0].[ContactName], [t0].[ContactTitle], [t0].[Country], [t0].[Fax], [t0].[Phone], [t0].[PostalCode], [t0].[Region]
FROM (
    SELECT DISTINCT [c].[CustomerID]
    FROM [Customers] AS [c]
) AS [t]
INNER JOIN (
    SELECT [t1].[CustomerID], [t1].[Address], [t1].[City], [t1].[CompanyName], [t1].[ContactName], [t1].[ContactTitle], [t1].[Country], [t1].[Fax], [t1].[Phone], [t1].[PostalCode], [t1].[Region]
    FROM (
        SELECT [c0].[CustomerID], [c0].[Address], [c0].[City], [c0].[CompanyName], [c0].[ContactName], [c0].[ContactTitle], [c0].[Country], [c0].[Fax], [c0].[Phone], [c0].[PostalCode], [c0].[Region], ROW_NUMBER() OVER(PARTITION BY [c0].[CustomerID] ORDER BY [c0].[CustomerID] + COALESCE([c0].[City], N'')) AS [row]
        FROM [Customers] AS [c0]
    ) AS [t1]
    WHERE [t1].[row] <= 2
) AS [t0] ON [t].[CustomerID] = [t0].[CustomerID]
""");
        }

        public override async Task Distinct_SelectMany_correlated_subquery_take_2(bool async)
        {
            await base.Distinct_SelectMany_correlated_subquery_take_2(async);

            AssertSql(
                """
SELECT [t0].[CustomerID], [t0].[Address], [t0].[City], [t0].[CompanyName], [t0].[ContactName], [t0].[ContactTitle], [t0].[Country], [t0].[Fax], [t0].[Phone], [t0].[PostalCode], [t0].[Region]
FROM (
    SELECT DISTINCT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region]
    FROM [Customers] AS [c]
) AS [t]
INNER JOIN (
    SELECT [t1].[CustomerID], [t1].[Address], [t1].[City], [t1].[CompanyName], [t1].[ContactName], [t1].[ContactTitle], [t1].[Country], [t1].[Fax], [t1].[Phone], [t1].[PostalCode], [t1].[Region]
    FROM (
        SELECT [c0].[CustomerID], [c0].[Address], [c0].[City], [c0].[CompanyName], [c0].[ContactName], [c0].[ContactTitle], [c0].[Country], [c0].[Fax], [c0].[Phone], [c0].[PostalCode], [c0].[Region], ROW_NUMBER() OVER(PARTITION BY [c0].[CustomerID] ORDER BY [c0].[CustomerID] + COALESCE([c0].[City], N'')) AS [row]
        FROM [Customers] AS [c0]
    ) AS [t1]
    WHERE [t1].[row] <= 2
) AS [t0] ON [t].[CustomerID] = [t0].[CustomerID]
""");
        }

        public override async Task Take_SelectMany_correlated_subquery_take(bool async)
        {
            await base.Take_SelectMany_correlated_subquery_take(async);

            AssertSql(
                """
@__p_0='2'

SELECT [t0].[CustomerID], [t0].[Address], [t0].[City], [t0].[CompanyName], [t0].[ContactName], [t0].[ContactTitle], [t0].[Country], [t0].[Fax], [t0].[Phone], [t0].[PostalCode], [t0].[Region]
FROM (
    SELECT TOP(@__p_0) [c].[CustomerID]
    FROM [Customers] AS [c]
    ORDER BY [c].[CustomerID]
) AS [t]
INNER JOIN (
    SELECT [t1].[CustomerID], [t1].[Address], [t1].[City], [t1].[CompanyName], [t1].[ContactName], [t1].[ContactTitle], [t1].[Country], [t1].[Fax], [t1].[Phone], [t1].[PostalCode], [t1].[Region]
    FROM (
        SELECT [c0].[CustomerID], [c0].[Address], [c0].[City], [c0].[CompanyName], [c0].[ContactName], [c0].[ContactTitle], [c0].[Country], [c0].[Fax], [c0].[Phone], [c0].[PostalCode], [c0].[Region], ROW_NUMBER() OVER(PARTITION BY [c0].[CustomerID] ORDER BY [c0].[CustomerID] + COALESCE([c0].[City], N'')) AS [row]
        FROM [Customers] AS [c0]
    ) AS [t1]
    WHERE [t1].[row] <= 2
) AS [t0] ON [t].[CustomerID] = [t0].[CustomerID]
ORDER BY [t].[CustomerID]
""");
        }

        public override async Task Take_in_collection_projection_with_FirstOrDefault_on_top_level(bool async)
        {
            await base.Take_in_collection_projection_with_FirstOrDefault_on_top_level(async);

            AssertSql(
                """
SELECT [t].[CustomerID], [t0].[Title], [t0].[OrderID], [t0].[CustomerID]
FROM (
    SELECT TOP(1) [c].[CustomerID]
    FROM [Customers] AS [c]
) AS [t]
OUTER APPLY (
    SELECT CASE
        WHEN [t1].[CustomerID] = [c0].[City] OR ([t1].[CustomerID] IS NULL AND [c0].[City] IS NULL) THEN N'A'
        ELSE N'B'
    END AS [Title], [t1].[OrderID], [c0].[CustomerID], [t1].[OrderDate]
    FROM (
        SELECT TOP(1) [o].[OrderID], [o].[CustomerID], [o].[OrderDate]
        FROM [Orders] AS [o]
        WHERE [t].[CustomerID] = [o].[CustomerID]
        ORDER BY [o].[OrderDate]
    ) AS [t1]
    LEFT JOIN [Customers] AS [c0] ON [t1].[CustomerID] = [c0].[CustomerID]
) AS [t0]
ORDER BY [t].[CustomerID], [t0].[OrderDate], [t0].[OrderID]
""");
        }

        public override async Task Condition_on_entity_with_include(bool async)
        {
            await base.Condition_on_entity_with_include(async);

            AssertSql(
                """
SELECT IIF(`o`.`OrderID` IS NOT NULL, `o`.`OrderID`, -1) AS `a`
FROM `Customers` AS `c`
LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
WHERE `c`.`CustomerID` LIKE 'F%'
""");
        }

        public override async Task Join_customers_orders_entities_same_entity_twice(bool async)
        {
            await base.Join_customers_orders_entities_same_entity_twice(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
INNER JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
""");
        }

        public override async Task Join_local_collection_int_closure_is_cached_correctly(bool async)
        {
            await base.Join_local_collection_int_closure_is_cached_correctly(async);

            AssertSql(
                """
@__p_0='[1,2]' (Size = 4000)

SELECT [e].[EmployeeID]
FROM [Employees] AS [e]
INNER JOIN OPENJSON(@__p_0) WITH ([value] int '$') AS [p] ON [e].[EmployeeID] = [p].[value]
""",
                //
                """
@__p_0='[3]' (Size = 4000)

SELECT [e].[EmployeeID]
FROM [Employees] AS [e]
INNER JOIN OPENJSON(@__p_0) WITH ([value] int '$') AS [p] ON [e].[EmployeeID] = [p].[value]
""");
        }

        public override async Task Join_local_string_closure_is_cached_correctly(bool async)
        {
            await base.Join_local_string_closure_is_cached_correctly(async);

            AssertSql();
        }

        public override async Task Join_local_bytes_closure_is_cached_correctly(bool async)
        {
            await base.Join_local_bytes_closure_is_cached_correctly(async);

            AssertSql();
        }

        public override async Task GroupJoin_customers_employees_shadow(bool async)
        {
            await base.GroupJoin_customers_employees_shadow(async);

            AssertSql(
                """
SELECT `e`.`Title`, `e`.`EmployeeID` AS `Id`
FROM `Customers` AS `c`
INNER JOIN `Employees` AS `e` ON `c`.`City` = `e`.`City`
""");
        }

        public override async Task GroupJoin_customers_employees_subquery_shadow(bool async)
        {
            await base.GroupJoin_customers_employees_subquery_shadow(async);

            AssertSql(
                """
SELECT `e`.`Title`, `e`.`EmployeeID` AS `Id`
FROM `Customers` AS `c`
INNER JOIN `Employees` AS `e` ON `c`.`City` = `e`.`City`
""");
        }

        public override async Task GroupJoin_customers_employees_subquery_shadow_take(bool async)
        {
            await base.GroupJoin_customers_employees_subquery_shadow_take(async);

            AssertSql(
                """
SELECT `e0`.`Title`, `e0`.`EmployeeID` AS `Id`
FROM `Customers` AS `c`
INNER JOIN (
    SELECT TOP @p `e`.`EmployeeID`, `e`.`City`, `e`.`Title`
    FROM `Employees` AS `e`
    ORDER BY `e`.`City`
) AS `e0` ON `c`.`City` = `e0`.`City`
""");
        }

        public override async Task GroupJoin_projection(bool async)
        {
            await base.GroupJoin_projection(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Customers` AS `c`
INNER JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
WHERE `c`.`CustomerID` LIKE 'F%'
""");
        }

        public override async Task GroupJoin_subquery_projection_outer_mixed(bool async)
        {
            await base.GroupJoin_subquery_projection_outer_mixed(async);

            AssertSql(
                """
SELECT [c].[CustomerID] AS [A], [t].[CustomerID] AS [B], [o0].[CustomerID] AS [C]
FROM [Customers] AS [c]
CROSS JOIN (
    SELECT TOP(1) [o].[CustomerID]
    FROM [Orders] AS [o]
    ORDER BY [o].[OrderID]
) AS [t]
INNER JOIN [Orders] AS [o0] ON [c].[CustomerID] = [o0].[CustomerID]
""");
        }

        public override async Task Join_with_key_selectors_being_nested_anonymous_objects(bool async)
        {
            await base.Join_with_key_selectors_being_nested_anonymous_objects(async);

            AssertSql();
        }

        private void AssertSql(params string[] expected)
            => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);

        protected override void ClearLog()
            => Fixture.TestSqlLoggerFactory.Clear();
    }
}
