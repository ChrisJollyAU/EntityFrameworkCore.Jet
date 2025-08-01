﻿// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EntityFrameworkCore.Jet.Data;
using EntityFrameworkCore.Jet.FunctionalTests.TestUtilities;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestModels.Northwind;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;
using Xunit.Abstractions;

namespace EntityFrameworkCore.Jet.FunctionalTests.Query
{
    public class NorthwindAggregateOperatorsQueryJetTest : NorthwindAggregateOperatorsQueryRelationalTestBase<
        NorthwindQueryJetFixture<NoopModelCustomizer>>
    {
        public NorthwindAggregateOperatorsQueryJetTest(
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

        public override async Task Last_when_no_order_by(bool async)
        {
            await base.Last_when_no_order_by(async);

            AssertSql();
        }

        public override async Task LastOrDefault_when_no_order_by(bool async)
        {
            await base.LastOrDefault_when_no_order_by(async);

            AssertSql();
        }

        public override async Task Contains_with_local_tuple_array_closure(bool async)
            => await AssertTranslationFailed(() => base.Contains_with_local_tuple_array_closure(async));

        public override async Task Array_cast_to_IEnumerable_Contains_with_constant(bool async)
        {
            await base.Array_cast_to_IEnumerable_Contains_with_constant(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` IN ('ALFKI', 'WRONG')
""");
        }

        public override async Task Contains_over_keyless_entity_throws(bool async)
        {
            await base.Contains_over_keyless_entity_throws(async);

            AssertSql(
                """
SELECT TOP 1 `m`.`Address`, `m`.`City`, `m`.`CompanyName`, `m`.`ContactName`, `m`.`ContactTitle`
FROM (
    SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region] FROM [Customers] AS [c]
) AS `m`
""");
        }

        public override async Task Enumerable_min_is_mapped_to_Queryable_1(bool async)
        {
            await base.Enumerable_min_is_mapped_to_Queryable_1(async);

            AssertSql(
                """
SELECT (
    SELECT MIN(CDBL(`o`.`OrderID`))
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` = `o`.`CustomerID`)
FROM `Customers` AS `c`
""");
        }

        public override async Task Enumerable_min_is_mapped_to_Queryable_2(bool async)
        {
            await base.Enumerable_min_is_mapped_to_Queryable_2(async);

            AssertSql(
                """
SELECT (
    SELECT MIN(CDBL(`o`.`OrderID`))
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` = `o`.`CustomerID`)
FROM `Customers` AS `c`
""");
        }

        public override async Task Average_with_unmapped_property_access_throws_meaningful_exception(bool async)
        {
            await base.Average_with_unmapped_property_access_throws_meaningful_exception(async);

            AssertSql();
        }

        public override async Task Sum_over_empty_returns_zero(bool async)
        {
            await base.Sum_over_empty_returns_zero(async);

            AssertSql(
                """
SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`))
FROM `Orders` AS `o`
WHERE `o`.`OrderID` = 42
""");
        }

        public override async Task Average_over_default_returns_default(bool async)
        {
            await base.Average_over_default_returns_default(async);

            AssertSql(
                """
SELECT AVG(CDBL(`o`.`OrderID` - 10248))
FROM `Orders` AS `o`
WHERE `o`.`OrderID` = 10248
""");
        }

        public override async Task Max_over_default_returns_default(bool async)
        {
            await base.Max_over_default_returns_default(async);

            AssertSql(
                """
SELECT MAX(`o`.`OrderID` - 10248)
FROM `Orders` AS `o`
WHERE `o`.`OrderID` = 10248
""");
        }

        public override async Task Min_over_default_returns_default(bool async)
        {
            await base.Min_over_default_returns_default(async);

            AssertSql(
                """
SELECT MIN(`o`.`OrderID` - 10248)
FROM `Orders` AS `o`
WHERE `o`.`OrderID` = 10248
""");
        }

        public override async Task Average_after_default_if_empty_does_not_throw(bool async)
        {
            await base.Average_after_default_if_empty_does_not_throw(async);

            AssertSql(
                """
SELECT AVG(CAST(COALESCE([t].[OrderID], 0) AS float))
FROM (
    SELECT NULL AS [empty]
) AS [e]
LEFT JOIN (
    SELECT [o].[OrderID]
    FROM [Orders] AS [o]
    WHERE [o].[OrderID] = 10243
) AS [t] ON 1 = 1
""");
        }

        public override async Task Max_after_default_if_empty_does_not_throw(bool async)
        {
            await base.Max_after_default_if_empty_does_not_throw(async);

            AssertSql(
                """
SELECT MAX(COALESCE([t].[OrderID], 0))
FROM (
    SELECT NULL AS [empty]
) AS [e]
LEFT JOIN (
    SELECT [o].[OrderID]
    FROM [Orders] AS [o]
    WHERE [o].[OrderID] = 10243
) AS [t] ON 1 = 1
""");
        }

        public override async Task Min_after_default_if_empty_does_not_throw(bool async)
        {
            await base.Min_after_default_if_empty_does_not_throw(async);

            AssertSql(
                """
SELECT MIN(COALESCE([t].[OrderID], 0))
FROM (
    SELECT NULL AS [empty]
) AS [e]
LEFT JOIN (
    SELECT [o].[OrderID]
    FROM [Orders] AS [o]
    WHERE [o].[OrderID] = 10243
) AS [t] ON 1 = 1
""");
        }

        public override async Task Sum_with_no_data_cast_to_nullable(bool async)
        {
            await base.Sum_with_no_data_cast_to_nullable(async);

            AssertSql(
                """
SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`))
FROM `Orders` AS `o`
WHERE `o`.`OrderID` < 0
""");
        }

        public override async Task Sum_with_no_data_nullable(bool async)
        {
            await base.Sum_with_no_data_nullable(async);

            AssertSql(
                """
SELECT IIF(SUM(`p`.`SupplierID`) IS NULL, 0, SUM(`p`.`SupplierID`))
FROM `Products` AS `p`
""");
        }

        public override async Task Sum_with_no_arg_empty(bool async)
        {
            await base.Sum_with_no_arg_empty(async);

            AssertSql(
                """
SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`))
FROM `Orders` AS `o`
WHERE `o`.`OrderID` = 42
""");
        }

        public override async Task Min_no_data(bool async)
        {
            await base.Min_no_data(async);

            AssertSql(
                """
SELECT MIN(`o`.`OrderID`)
FROM `Orders` AS `o`
WHERE `o`.`OrderID` = -1
""");
        }

        public override async Task Min_no_data_nullable(bool async)
        {
            await base.Min_no_data_nullable(async);

            AssertSql(
                """
SELECT MIN(`p`.`SupplierID`)
FROM `Products` AS `p`
WHERE `p`.`SupplierID` = -1
""");
        }

        public override async Task Min_no_data_cast_to_nullable(bool async)
        {
            await base.Min_no_data_cast_to_nullable(async);

            AssertSql(
                """
SELECT MIN(`o`.`OrderID`)
FROM `Orders` AS `o`
WHERE `o`.`OrderID` = -1
""");
        }

        public override async Task Min_no_data_subquery(bool async)
        {
            await base.Min_no_data_subquery(async);

            AssertSql(
                """
SELECT (
    SELECT MIN(`o`.`OrderID`)
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` = `o`.`CustomerID` AND `o`.`OrderID` = -1)
FROM `Customers` AS `c`
""");
        }

        public override async Task Max_no_data(bool async)
        {
            await base.Max_no_data(async);

            AssertSql(
                """
SELECT MAX(`o`.`OrderID`)
FROM `Orders` AS `o`
WHERE `o`.`OrderID` = -1
""");
        }

        public override async Task Max_no_data_nullable(bool async)
        {
            await base.Max_no_data_nullable(async);

            AssertSql(
                """
SELECT MAX(`p`.`SupplierID`)
FROM `Products` AS `p`
WHERE `p`.`SupplierID` = -1
""");
        }

        public override async Task Max_no_data_cast_to_nullable(bool async)
        {
            await base.Max_no_data_cast_to_nullable(async);

            AssertSql(
                """
SELECT MAX(`o`.`OrderID`)
FROM `Orders` AS `o`
WHERE `o`.`OrderID` = -1
""");
        }

        public override async Task Max_no_data_subquery(bool async)
        {
            await base.Max_no_data_subquery(async);

            AssertSql(
                """
SELECT (
    SELECT MAX(`o`.`OrderID`)
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` = `o`.`CustomerID` AND `o`.`OrderID` = -1)
FROM `Customers` AS `c`
""");
        }

        private static readonly IEnumerable<string> StaticIds = new List<string> { "ALFKI", "ANATR" };

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task Contains_with_static_IList(bool async)
        {
            await AssertQuery(
                async,
                ss => ss.Set<Customer>().Where(c => StaticIds.Contains(c.CustomerID)));

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` IN ('ALFKI', 'ANATR')
""");
        }

        public override async Task Average_no_data(bool async)
        {
            await base.Average_no_data(async);

            AssertSql(
                """
SELECT AVG(CDBL(`o`.`OrderID`))
FROM `Orders` AS `o`
WHERE `o`.`OrderID` = -1
""");
        }

        public override async Task Average_no_data_nullable(bool async)
        {
            await base.Average_no_data_nullable(async);

            AssertSql(
                """
SELECT AVG(IIF(`p`.`SupplierID` IS NULL, NULL, CDBL(`p`.`SupplierID`)))
FROM `Products` AS `p`
WHERE `p`.`SupplierID` = -1
""");
        }

        public override async Task Average_no_data_cast_to_nullable(bool async)
        {
            await base.Average_no_data_cast_to_nullable(async);

            AssertSql(
                """
SELECT AVG(CDBL(`o`.`OrderID`))
FROM `Orders` AS `o`
WHERE `o`.`OrderID` = -1
""");
        }

        public override async Task Average_no_data_subquery(bool async)
        {
            await base.Average_no_data_subquery(async);

            AssertSql(
                """
SELECT (
    SELECT AVG(CDBL(`o`.`OrderID`))
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` = `o`.`CustomerID` AND `o`.`OrderID` = -1)
FROM `Customers` AS `c`
""");
        }

        public override async Task Count_with_no_predicate(bool async)
        {
            await base.Count_with_no_predicate(async);

            AssertSql(
                """
SELECT COUNT(*)
FROM `Orders` AS `o`
""");
        }

        public override async Task Count_with_order_by(bool async)
        {
            await base.Count_with_order_by(async);

            AssertSql(
                """
SELECT COUNT(*)
FROM `Orders` AS `o`
""");
        }

        public override async Task Where_OrderBy_Count_client_eval(bool async)
        {
            await base.Where_OrderBy_Count_client_eval(async);

            AssertSql();
        }

        public override async Task OrderBy_Where_Count_client_eval(bool async)
        {
            await base.OrderBy_Where_Count_client_eval(async);

            AssertSql();
        }

        public override async Task OrderBy_Where_Count_client_eval_mixed(bool async)
        {
            await base.OrderBy_Where_Count_client_eval_mixed(async);

            AssertSql();
        }

        public override async Task OrderBy_Count_with_predicate_client_eval(bool async)
        {
            await base.OrderBy_Count_with_predicate_client_eval(async);

            AssertSql();
        }

        public override async Task OrderBy_Count_with_predicate_client_eval_mixed(bool async)
        {
            await base.OrderBy_Count_with_predicate_client_eval_mixed(async);

            AssertSql();
        }

        public override async Task OrderBy_Where_Count_with_predicate_client_eval(bool async)
        {
            await base.OrderBy_Where_Count_with_predicate_client_eval(async);

            AssertSql();
        }

        public override async Task OrderBy_Where_Count_with_predicate_client_eval_mixed(bool async)
        {
            await base.OrderBy_Where_Count_with_predicate_client_eval_mixed(async);

            AssertSql();
        }

        public override async Task OrderBy_client_Take(bool async)
        {
            await base.OrderBy_client_Take(async);

            AssertSql(
                """
SELECT TOP @p `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
FROM `Employees` AS `e`
ORDER BY 1
""");
        }

        public override async Task Single_Throws(bool async)
        {
            await base.Single_Throws(async);

            AssertSql(
                """
SELECT TOP 2 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
""");
        }

        public override async Task Where_Single(bool async)
        {
            await base.Where_Single(async);

            AssertSql(
                """
SELECT TOP 2 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = 'ALFKI'
""");
        }

        public override async Task SingleOrDefault_Throws(bool async)
        {
            await base.SingleOrDefault_Throws(async);

            AssertSql(
                """
SELECT TOP 2 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
""");
        }

        public override async Task SingleOrDefault_Predicate(bool async)
        {
            await base.SingleOrDefault_Predicate(async);

            AssertSql(
                """
SELECT TOP 2 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = 'ALFKI'
""");
        }

        public override async Task Where_SingleOrDefault(bool async)
        {
            await base.Where_SingleOrDefault(async);

            AssertSql(
                """
SELECT TOP 2 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = 'ALFKI'
""");
        }

        public override async Task First(bool async)
        {
            await base.First(async);

            AssertSql(
                """
SELECT TOP 1 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
ORDER BY `c`.`ContactName`
""");
        }

        public override async Task First_Predicate(bool async)
        {
            await base.First_Predicate(async);

            AssertSql(
                """
SELECT TOP 1 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = 'London'
ORDER BY `c`.`ContactName`
""");
        }

        public override async Task Where_First(bool async)
        {
            await base.Where_First(async);

            AssertSql(
                """
SELECT TOP 1 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = 'London'
ORDER BY `c`.`ContactName`
""");
        }

        public override async Task FirstOrDefault(bool async)
        {
            await base.FirstOrDefault(async);

            AssertSql(
                """
SELECT TOP 1 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
ORDER BY `c`.`ContactName`
""");
        }

        public override async Task FirstOrDefault_Predicate(bool async)
        {
            await base.FirstOrDefault_Predicate(async);

            AssertSql(
                """
SELECT TOP 1 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = 'London'
ORDER BY `c`.`ContactName`
""");
        }

        public override async Task Where_FirstOrDefault(bool async)
        {
            await base.Where_FirstOrDefault(async);

            AssertSql(
                """
SELECT TOP 1 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = 'London'
ORDER BY `c`.`ContactName`
""");
        }

        public override async Task Select_All(bool async)
        {
            await base.Select_All(async);

            AssertSql(
                """
SELECT IIF(NOT EXISTS (
        SELECT 1
        FROM `Orders` AS `o`
        WHERE `o`.`CustomerID` <> 'ALFKI' OR `o`.`CustomerID` IS NULL), TRUE, FALSE)
FROM (SELECT COUNT(*) FROM `#Dual`)
""");
        }

        public override async Task Sum_with_no_arg(bool isAsync)
        {
            await base.Sum_with_no_arg(isAsync);

            AssertSql(
                """
SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`))
FROM `Orders` AS `o`
""");
        }

        public override async Task Sum_with_binary_expression(bool isAsync)
        {
            await base.Sum_with_binary_expression(isAsync);

            AssertSql(
                """
SELECT IIF(SUM(`o`.`OrderID` * 2) IS NULL, 0, SUM(`o`.`OrderID` * 2))
FROM `Orders` AS `o`
""");
        }

        public override async Task Sum_with_arg(bool isAsync)
        {
            await base.Sum_with_arg(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`))
                    FROM `Orders` AS `o`
                    """);
        }

        public override async Task Sum_with_arg_expression(bool isAsync)
        {
            await base.Sum_with_arg_expression(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(`o`.`OrderID` + `o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID` + `o`.`OrderID`))
                    FROM `Orders` AS `o`
                    """);
        }

        public override async Task Sum_with_division_on_decimal(bool isAsync)
        {
            await base.Sum_with_division_on_decimal(isAsync);

            AssertSql(
                $"""
                    SELECT SUM(CCUR(`o`.`Quantity`) / 2.09)
                    FROM `Order Details` AS `o`
                    """);
        }

        public override async Task Sum_with_division_on_decimal_no_significant_digits(bool isAsync)
        {
            await base.Sum_with_division_on_decimal_no_significant_digits(isAsync);

            AssertSql(
                $"""
                    SELECT SUM(CCUR(`o`.`Quantity`) / 2.0)
                    FROM `Order Details` AS `o`
                    """);
        }

        public override async Task Sum_with_coalesce(bool isAsync)
        {
            await base.Sum_with_coalesce(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(IIF(`p`.`UnitPrice` IS NULL, 0.0, `p`.`UnitPrice`)) IS NULL, 0.0, SUM(IIF(`p`.`UnitPrice` IS NULL, 0.0, `p`.`UnitPrice`)))
                    FROM `Products` AS `p`
                    WHERE `p`.`ProductID` < 40
                    """);
        }

        public override async Task Sum_over_subquery(bool isAsync)
        {
            await base.Sum_over_subquery(isAsync);

            AssertSql(
"""
SELECT IIF(SUM((
        SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`))
        FROM `Orders` AS `o`
        WHERE `c`.`CustomerID` = `o`.`CustomerID`)) IS NULL, 0, SUM((
        SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`))
        FROM `Orders` AS `o`
        WHERE `c`.`CustomerID` = `o`.`CustomerID`)))
FROM `Customers` AS `c`
""");
        }

        public override async Task Sum_over_nested_subquery(bool isAsync)
        {
            await base.Sum_over_nested_subquery(isAsync);
            AssertSql(
"""
SELECT IIF(SUM((
        SELECT IIF(SUM(5 + (
                SELECT IIF(SUM(`o0`.`ProductID`) IS NULL, 0, SUM(`o0`.`ProductID`))
                FROM `Order Details` AS `o0`
                WHERE `o`.`OrderID` = `o0`.`OrderID`)) IS NULL, 0, SUM(5 + (
                SELECT IIF(SUM(`o0`.`ProductID`) IS NULL, 0, SUM(`o0`.`ProductID`))
                FROM `Order Details` AS `o0`
                WHERE `o`.`OrderID` = `o0`.`OrderID`)))
        FROM `Orders` AS `o`
        WHERE `c`.`CustomerID` = `o`.`CustomerID`)) IS NULL, 0, SUM((
        SELECT IIF(SUM(5 + (
                SELECT IIF(SUM(`o0`.`ProductID`) IS NULL, 0, SUM(`o0`.`ProductID`))
                FROM `Order Details` AS `o0`
                WHERE `o`.`OrderID` = `o0`.`OrderID`)) IS NULL, 0, SUM(5 + (
                SELECT IIF(SUM(`o0`.`ProductID`) IS NULL, 0, SUM(`o0`.`ProductID`))
                FROM `Order Details` AS `o0`
                WHERE `o`.`OrderID` = `o0`.`OrderID`)))
        FROM `Orders` AS `o`
        WHERE `c`.`CustomerID` = `o`.`CustomerID`)))
FROM `Customers` AS `c`
""");
        }

        public override async Task Sum_over_min_subquery(bool isAsync)
        {
            await base.Sum_over_min_subquery(isAsync);
            AssertSql(
"""
SELECT IIF(SUM((
        SELECT IIF(SUM(5 + (
                SELECT MIN(`o0`.`ProductID`)
                FROM `Order Details` AS `o0`
                WHERE `o`.`OrderID` = `o0`.`OrderID`)) IS NULL, 0, SUM(5 + (
                SELECT MIN(`o0`.`ProductID`)
                FROM `Order Details` AS `o0`
                WHERE `o`.`OrderID` = `o0`.`OrderID`)))
        FROM `Orders` AS `o`
        WHERE `c`.`CustomerID` = `o`.`CustomerID`)) IS NULL, 0, SUM((
        SELECT IIF(SUM(5 + (
                SELECT MIN(`o0`.`ProductID`)
                FROM `Order Details` AS `o0`
                WHERE `o`.`OrderID` = `o0`.`OrderID`)) IS NULL, 0, SUM(5 + (
                SELECT MIN(`o0`.`ProductID`)
                FROM `Order Details` AS `o0`
                WHERE `o`.`OrderID` = `o0`.`OrderID`)))
        FROM `Orders` AS `o`
        WHERE `c`.`CustomerID` = `o`.`CustomerID`)))
FROM `Customers` AS `c`
""");
        }

        public override async Task Sum_over_scalar_returning_subquery(bool async)
        {
            await base.Sum_over_scalar_returning_subquery(async);

            AssertSql(
                """
SELECT IIF(SUM((
        SELECT TOP 1 `o`.`OrderID`
        FROM `Orders` AS `o`
        WHERE `c`.`CustomerID` = `o`.`CustomerID`)) IS NULL, 0, SUM((
        SELECT TOP 1 `o`.`OrderID`
        FROM `Orders` AS `o`
        WHERE `c`.`CustomerID` = `o`.`CustomerID`)))
FROM `Customers` AS `c`
""");
        }

        public override async Task Sum_over_Any_subquery(bool async)
        {
            await base.Sum_over_Any_subquery(async);

            AssertSql(
                """
SELECT IIF(SUM(IIF(EXISTS (
            SELECT 1
            FROM `Orders` AS `o`
            WHERE `c`.`CustomerID` = `o`.`CustomerID`), (
            SELECT TOP 1 `o0`.`OrderID`
            FROM `Orders` AS `o0`
            WHERE `c`.`CustomerID` = `o0`.`CustomerID`), 0)) IS NULL, 0, SUM(IIF(EXISTS (
            SELECT 1
            FROM `Orders` AS `o`
            WHERE `c`.`CustomerID` = `o`.`CustomerID`), (
            SELECT TOP 1 `o0`.`OrderID`
            FROM `Orders` AS `o0`
            WHERE `c`.`CustomerID` = `o0`.`CustomerID`), 0)))
FROM `Customers` AS `c`
""");
        }

        public override async Task Sum_over_uncorrelated_subquery(bool async)
        {
            await base.Sum_over_uncorrelated_subquery(async);

            AssertSql(
                """
SELECT IIF(SUM((
        SELECT COUNT(*)
        FROM `Orders` AS `o`
        WHERE `o`.`OrderID` > 10300)) IS NULL, 0, SUM((
        SELECT COUNT(*)
        FROM `Orders` AS `o`
        WHERE `o`.`OrderID` > 10300)))
FROM `Customers` AS `c`
""");
        }

        public override async Task Sum_on_float_column(bool isAsync)
        {
            await base.Sum_on_float_column(isAsync);

            AssertSql(
                $"""
                    SELECT CSNG(IIF(SUM(`o`.`Discount`) IS NULL, 0.0, SUM(`o`.`Discount`)))
                    FROM `Order Details` AS `o`
                    WHERE `o`.`ProductID` = 1
                    """);
        }

        public override async Task Sum_on_float_column_in_subquery(bool isAsync)
        {
            await base.Sum_on_float_column_in_subquery(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`OrderID`, (
                        SELECT CSNG(IIF(SUM(`o0`.`Discount`) IS NULL, 0.0, SUM(`o0`.`Discount`)))
                        FROM `Order Details` AS `o0`
                        WHERE `o`.`OrderID` = `o0`.`OrderID`) AS `Sum`
                    FROM `Orders` AS `o`
                    WHERE `o`.`OrderID` < 10300
                    """);
        }

        public override async Task Average_with_no_arg(bool isAsync)
        {
            await base.Average_with_no_arg(isAsync);

            AssertSql(
                $"""
                    SELECT AVG(CDBL(`o`.`OrderID`))
                    FROM `Orders` AS `o`
                    """);
        }

        public override async Task Average_with_binary_expression(bool isAsync)
        {
            await base.Average_with_binary_expression(isAsync);

            AssertSql(
                $"""
                    SELECT AVG(CDBL(`o`.`OrderID` * 2))
                    FROM `Orders` AS `o`
                    """);
        }

        public override async Task Average_with_arg(bool isAsync)
        {
            await base.Average_with_arg(isAsync);

            AssertSql(
                $"""
                    SELECT AVG(CDBL(`o`.`OrderID`))
                    FROM `Orders` AS `o`
                    """);
        }

        public override async Task Average_with_arg_expression(bool isAsync)
        {
            await base.Average_with_arg_expression(isAsync);

            AssertSql(
                $"""
                    SELECT AVG(CDBL(`o`.`OrderID` + `o`.`OrderID`))
                    FROM `Orders` AS `o`
                    """);
        }

        public override async Task Average_with_division_on_decimal(bool isAsync)
        {
            await base.Average_with_division_on_decimal(isAsync);

            AssertSql(
                $"""
                    SELECT AVG(CCUR(`o`.`Quantity`) / 2.09)
                    FROM `Order Details` AS `o`
                    """);
        }

        public override async Task Average_with_division_on_decimal_no_significant_digits(bool isAsync)
        {
            await base.Average_with_division_on_decimal_no_significant_digits(isAsync);

            AssertSql(
                $"""
                    SELECT AVG(IIF(`o`.`Quantity` IS NULL, NULL, CCUR(`o`.`Quantity`)) / 2.0)
                    FROM `Order Details` AS `o`
                    """);
        }

        public override async Task Average_with_coalesce(bool isAsync)
        {
            await base.Average_with_coalesce(isAsync);

            AssertSql(
                $"""
                    SELECT AVG(IIF(`p`.`UnitPrice` IS NULL, 0.0, `p`.`UnitPrice`))
                    FROM `Products` AS `p`
                    WHERE `p`.`ProductID` < 40
                    """);
        }

        public override async Task Average_over_subquery(bool isAsync)
        {
            await base.Average_over_subquery(isAsync);

            AssertSql(
                """
    SELECT AVG(IIF((
            SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`))
            FROM `Orders` AS `o`
            WHERE `c`.`CustomerID` = `o`.`CustomerID`) IS NULL, NULL, CDBL((
            SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`))
            FROM `Orders` AS `o`
            WHERE `c`.`CustomerID` = `o`.`CustomerID`))))
    FROM `Customers` AS `c`
    """);
        }

        public override async Task Average_over_nested_subquery(bool isAsync)
        {
            await base.Average_over_nested_subquery(isAsync);
            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@__p_0='3'")}
                    
                    SELECT TOP {AssertSqlHelper.Parameter("@__p_0")} `c`.`CustomerID`
                    FROM `Customers` AS `c`
                    ORDER BY `c`.`CustomerID`
                    """);
        }

        public override async Task Average_over_max_subquery(bool isAsync)
        {
            await base.Average_over_max_subquery(isAsync);
            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@__p_0='3'")}
                    
                    SELECT TOP {AssertSqlHelper.Parameter("@__p_0")} `c`.`CustomerID`
                    FROM `Customers` AS `c`
                    ORDER BY `c`.`CustomerID`
                    """);
        }

        public override async Task Average_on_float_column(bool isAsync)
        {
            await base.Average_on_float_column(isAsync);

            AssertSql(
                $"""
                    SELECT CSNG(AVG(`o`.`Discount`))
                    FROM `Order Details` AS `o`
                    WHERE `o`.`ProductID` = 1
                    """);
        }

        public override async Task Average_on_float_column_in_subquery(bool isAsync)
        {
            await base.Average_on_float_column_in_subquery(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`OrderID`, (
                        SELECT CSNG(AVG(`o0`.`Discount`))
                        FROM `Order Details` AS `o0`
                        WHERE `o`.`OrderID` = `o0`.`OrderID`) AS `Sum`
                    FROM `Orders` AS `o`
                    WHERE `o`.`OrderID` < 10300
                    """);
        }

        public override async Task Average_on_float_column_in_subquery_with_cast(bool isAsync)
        {
            await base.Average_on_float_column_in_subquery_with_cast(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`OrderID`, (
                        SELECT CSNG(AVG(`o0`.`Discount`))
                        FROM `Order Details` AS `o0`
                        WHERE `o`.`OrderID` = `o0`.`OrderID`) AS `Sum`
                    FROM `Orders` AS `o`
                    WHERE `o`.`OrderID` < 10300
                    """);
        }

        public override async Task Min_with_no_arg(bool isAsync)
        {
            await base.Min_with_no_arg(isAsync);

            AssertSql(
                $"""
                    SELECT MIN(`o`.`OrderID`)
                    FROM `Orders` AS `o`
                    """);
        }

        public override async Task Min_with_arg(bool isAsync)
        {
            await base.Min_with_arg(isAsync);

            AssertSql(
                $"""
                    SELECT MIN(`o`.`OrderID`)
                    FROM `Orders` AS `o`
                    """);
        }

        public override async Task Min_with_coalesce(bool isAsync)
        {
            await base.Min_with_coalesce(isAsync);

            AssertSql(
                $"""
                    SELECT MIN(IIF(`p`.`UnitPrice` IS NULL, 0.0, `p`.`UnitPrice`))
                    FROM `Products` AS `p`
                    WHERE `p`.`ProductID` < 40
                    """);
        }

        public override async Task Min_over_subquery(bool isAsync)
        {
            await base.Min_over_subquery(isAsync);

            AssertSql(
"""
SELECT MIN((
    SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`))
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` = `o`.`CustomerID`))
FROM `Customers` AS `c`
""");
        }

        public override async Task Min_over_nested_subquery(bool isAsync)
        {
            await base.Min_over_nested_subquery(isAsync);

            AssertSql(
                """
SELECT MIN((
    SELECT MIN(5 + (
        SELECT MIN(`o0`.`ProductID`)
        FROM `Order Details` AS `o0`
        WHERE `o`.`OrderID` = `o0`.`OrderID`))
    FROM `Orders` AS `o`
    WHERE `c0`.`CustomerID` = `o`.`CustomerID`))
FROM (
    SELECT TOP @p `c`.`CustomerID`
    FROM `Customers` AS `c`
    ORDER BY `c`.`CustomerID`
) AS `c0`
""");
        }

        public override async Task Min_over_max_subquery(bool isAsync)
        {
            await base.Min_over_max_subquery(isAsync);

            AssertSql(
                """
SELECT MIN((
    SELECT MIN(5 + (
        SELECT MAX(`o0`.`ProductID`)
        FROM `Order Details` AS `o0`
        WHERE `o`.`OrderID` = `o0`.`OrderID`))
    FROM `Orders` AS `o`
    WHERE `c0`.`CustomerID` = `o`.`CustomerID`))
FROM (
    SELECT TOP @p `c`.`CustomerID`
    FROM `Customers` AS `c`
    ORDER BY `c`.`CustomerID`
) AS `c0`
""");
        }

        public override async Task Max_with_no_arg(bool isAsync)
        {
            await base.Max_with_no_arg(isAsync);

            AssertSql(
                $"""
                    SELECT MAX(`o`.`OrderID`)
                    FROM `Orders` AS `o`
                    """);
        }

        public override async Task Max_with_arg(bool isAsync)
        {
            await base.Max_with_arg(isAsync);

            AssertSql(
                $"""
                    SELECT MAX(`o`.`OrderID`)
                    FROM `Orders` AS `o`
                    """);
        }

        public override async Task Max_with_coalesce(bool isAsync)
        {
            await base.Max_with_coalesce(isAsync);

            AssertSql(
                $"""
                    SELECT MAX(IIF(`p`.`UnitPrice` IS NULL, 0.0, `p`.`UnitPrice`))
                    FROM `Products` AS `p`
                    WHERE `p`.`ProductID` < 40
                    """);
        }

        public override async Task Max_over_subquery(bool isAsync)
        {
            await base.Max_over_subquery(isAsync);

            AssertSql(
"""
SELECT MAX((
    SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`))
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` = `o`.`CustomerID`))
FROM `Customers` AS `c`
""");
        }

        public override async Task Max_over_nested_subquery(bool isAsync)
        {
            await base.Max_over_nested_subquery(isAsync);

            AssertSql(
                """
SELECT MAX((
    SELECT MAX(5 + (
        SELECT MAX(`o0`.`ProductID`)
        FROM `Order Details` AS `o0`
        WHERE `o`.`OrderID` = `o0`.`OrderID`))
    FROM `Orders` AS `o`
    WHERE `c0`.`CustomerID` = `o`.`CustomerID`))
FROM (
    SELECT TOP @p `c`.`CustomerID`
    FROM `Customers` AS `c`
    ORDER BY `c`.`CustomerID`
) AS `c0`
""");
        }

        public override async Task Max_over_sum_subquery(bool isAsync)
        {
            await base.Max_over_sum_subquery(isAsync);

            AssertSql(
                """
SELECT MAX((
    SELECT MAX(5 + (
        SELECT IIF(SUM(`o0`.`ProductID`) IS NULL, 0, SUM(`o0`.`ProductID`))
        FROM `Order Details` AS `o0`
        WHERE `o`.`OrderID` = `o0`.`OrderID`))
    FROM `Orders` AS `o`
    WHERE `c0`.`CustomerID` = `o`.`CustomerID`))
FROM (
    SELECT TOP @p `c`.`CustomerID`
    FROM `Customers` AS `c`
    ORDER BY `c`.`CustomerID`
) AS `c0`
""");
        }

        public override async Task Count_with_predicate(bool isAsync)
        {
            await base.Count_with_predicate(isAsync);

            AssertSql(
                $"""
                    SELECT COUNT(*)
                    FROM `Orders` AS `o`
                    WHERE `o`.`CustomerID` = 'ALFKI'
                    """);
        }

        public override async Task Where_OrderBy_Count(bool isAsync)
        {
            await base.Where_OrderBy_Count(isAsync);

            AssertSql(
                $"""
                    SELECT COUNT(*)
                    FROM `Orders` AS `o`
                    WHERE `o`.`CustomerID` = 'ALFKI'
                    """);
        }

        public override async Task OrderBy_Where_Count(bool isAsync)
        {
            await base.OrderBy_Where_Count(isAsync);

            AssertSql(
                $"""
                    SELECT COUNT(*)
                    FROM `Orders` AS `o`
                    WHERE `o`.`CustomerID` = 'ALFKI'
                    """);
        }

        public override async Task OrderBy_Count_with_predicate(bool isAsync)
        {
            await base.OrderBy_Count_with_predicate(isAsync);

            AssertSql(
                $"""
                    SELECT COUNT(*)
                    FROM `Orders` AS `o`
                    WHERE `o`.`CustomerID` = 'ALFKI'
                    """);
        }

        public override async Task OrderBy_Where_Count_with_predicate(bool isAsync)
        {
            await base.OrderBy_Where_Count_with_predicate(isAsync);

            AssertSql(
                """
SELECT COUNT(*)
FROM `Orders` AS `o`
WHERE `o`.`OrderID` > 10 AND (`o`.`CustomerID` <> 'ALFKI' OR `o`.`CustomerID` IS NULL)
""");
        }

        public override async Task Distinct(bool isAsync)
        {
            await base.Distinct(isAsync);

            AssertSql(
                $"""
                    SELECT DISTINCT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    """);
        }

        public override async Task Distinct_Scalar(bool isAsync)
        {
            await base.Distinct_Scalar(isAsync);

            AssertSql(
                $"""
                    SELECT DISTINCT `c`.`City`
                    FROM `Customers` AS `c`
                    """);
        }

        public override async Task OrderBy_Distinct(bool isAsync)
        {
            await base.OrderBy_Distinct(isAsync);

            // Ordering not preserved by distinct when ordering columns not projected.
            AssertSql(
                $"""
                    SELECT DISTINCT `c`.`City`
                    FROM `Customers` AS `c`
                    """);
        }

        public override async Task Distinct_OrderBy(bool isAsync)
        {
            await base.Distinct_OrderBy(isAsync);

            AssertSql(
                """
SELECT `c0`.`Country`
FROM (
    SELECT DISTINCT `c`.`Country`
    FROM `Customers` AS `c`
) AS `c0`
ORDER BY `c0`.`Country`
""");
        }

        public override async Task Distinct_OrderBy2(bool isAsync)
        {
            await base.Distinct_OrderBy2(isAsync);

            AssertSql(
                """
SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
FROM (
    SELECT DISTINCT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
) AS `c0`
ORDER BY `c0`.`CustomerID`
""");
        }

        public override async Task Distinct_OrderBy3(bool isAsync)
        {
            await base.Distinct_OrderBy3(isAsync);

            AssertSql(
                """
SELECT `c0`.`CustomerID`
FROM (
    SELECT DISTINCT `c`.`CustomerID`
    FROM `Customers` AS `c`
) AS `c0`
ORDER BY `c0`.`CustomerID`
""");
        }

        public override async Task Distinct_Count(bool isAsync)
        {
            await base.Distinct_Count(isAsync);

            AssertSql(
                """
SELECT COUNT(*)
FROM (
    SELECT DISTINCT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
) AS `c0`
""");
        }

        public override async Task Select_Select_Distinct_Count(bool isAsync)
        {
            await base.Select_Select_Distinct_Count(isAsync);

            AssertSql(
                """
SELECT COUNT(*)
FROM (
    SELECT DISTINCT `c`.`City`
    FROM `Customers` AS `c`
) AS `c0`
""");
        }

        public override async Task Single_Predicate(bool isAsync)
        {
            await base.Single_Predicate(isAsync);

            AssertSql(
                $"""
                    SELECT TOP 2 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` = 'ALFKI'
                    """);
        }

        public override async Task FirstOrDefault_inside_subquery_gets_server_evaluated(bool isAsync)
        {
            await base.FirstOrDefault_inside_subquery_gets_server_evaluated(isAsync);

            // issue #15994
            //            AssertSql(
            //                $@"SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
            //FROM `Customers` AS `c`
            //WHERE (`c`.`CustomerID` = 'ALFKI') AND ((
            //    SELECT TOP 1 `o`.`CustomerID`
            //    FROM `Orders` AS `o`
            //    WHERE (`c`.`CustomerID` = `o`.`CustomerID`) AND (`o`.`CustomerID` = 'ALFKI')
            //) = 'ALFKI')");
        }

        public override async Task Multiple_collection_navigation_with_FirstOrDefault_chained(bool isAsync)
        {
            await base.Multiple_collection_navigation_with_FirstOrDefault_chained(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`
                    FROM `Customers` AS `c`
                    ORDER BY `c`.`CustomerID`
                    """,
                //
                $"""
                    {AssertSqlHelper.Declaration("@_outer_CustomerID='ALFKI' (Size = 5)")}
                    
                    SELECT TOP 1 `od`.`OrderID`, `od`.`ProductID`, `od`.`Discount`, `od`.`Quantity`, `od`.`UnitPrice`
                    FROM `Order Details` AS `od`
                    WHERE `od`.`OrderID` = COALESCE((
                        SELECT TOP 1 `o`.`OrderID`
                        FROM `Orders` AS `o`
                        WHERE {AssertSqlHelper.Parameter("@_outer_CustomerID")} = `o`.`CustomerID`
                        ORDER BY `o`.`OrderID`
                    ), 0)
                    ORDER BY `od`.`ProductID`
                    """,
                //
                $"""
                    {AssertSqlHelper.Declaration("@_outer_CustomerID='ANATR' (Size = 5)")}
                    
                    SELECT TOP 1 `od`.`OrderID`, `od`.`ProductID`, `od`.`Discount`, `od`.`Quantity`, `od`.`UnitPrice`
                    FROM `Order Details` AS `od`
                    WHERE `od`.`OrderID` = COALESCE((
                        SELECT TOP 1 `o`.`OrderID`
                        FROM `Orders` AS `o`
                        WHERE {AssertSqlHelper.Parameter("@_outer_CustomerID")} = `o`.`CustomerID`
                        ORDER BY `o`.`OrderID`
                    ), 0)
                    ORDER BY `od`.`ProductID`
                    """);
        }

        public override async Task Multiple_collection_navigation_with_FirstOrDefault_chained_projecting_scalar(bool isAsync)
        {
            await base.Multiple_collection_navigation_with_FirstOrDefault_chained_projecting_scalar(isAsync);

            // issue #15994
            //            AssertSql(
            //                $@"SELECT (
            //    SELECT TOP 1 `od`.`ProductID`
            //    FROM `Order Details` AS `od`
            //    WHERE `od`.`OrderID` = COALESCE((
            //        SELECT TOP 1 `o`.`OrderID`
            //        FROM `Orders` AS `o`
            //        WHERE `c`.`CustomerID` = `o`.`CustomerID`
            //        ORDER BY `o`.`OrderID`
            //    ), 0)
            //    ORDER BY `od`.`ProductID`
            //)
            //FROM `Customers` AS `c`
            //ORDER BY `c`.`CustomerID`");
        }

        public override async Task First_inside_subquery_gets_client_evaluated(bool isAsync)
        {
            await base.First_inside_subquery_gets_client_evaluated(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` = 'ALFKI' AND (
                        SELECT TOP 1 `o`.`CustomerID`
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID` AND `o`.`CustomerID` = 'ALFKI') = 'ALFKI'
                    """);
        }

        public override async Task Last(bool isAsync)
        {
            await base.Last(isAsync);

            AssertSql(
                $"""
                    SELECT TOP 1 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    ORDER BY `c`.`ContactName` DESC
                    """);
        }

        public override async Task Last_Predicate(bool isAsync)
        {
            await base.Last_Predicate(isAsync);

            AssertSql(
                $"""
                    SELECT TOP 1 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`City` = 'London'
                    ORDER BY `c`.`ContactName` DESC
                    """);
        }

        public override async Task Where_Last(bool isAsync)
        {
            await base.Where_Last(isAsync);

            AssertSql(
                $"""
                    SELECT TOP 1 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`City` = 'London'
                    ORDER BY `c`.`ContactName` DESC
                    """);
        }

        public override async Task LastOrDefault(bool isAsync)
        {
            await base.LastOrDefault(isAsync);

            AssertSql(
                $"""
                    SELECT TOP 1 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    ORDER BY `c`.`ContactName` DESC
                    """);
        }

        public override async Task LastOrDefault_Predicate(bool isAsync)
        {
            await base.LastOrDefault_Predicate(isAsync);

            AssertSql(
                $"""
                    SELECT TOP 1 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`City` = 'London'
                    ORDER BY `c`.`ContactName` DESC
                    """);
        }

        public override async Task Where_LastOrDefault(bool isAsync)
        {
            await base.Where_LastOrDefault(isAsync);

            AssertSql(
                """
SELECT TOP 1 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = 'London'
ORDER BY `c`.`ContactName` DESC
""");
        }

        public override async Task Contains_with_subquery(bool isAsync)
        {
            await base.Contains_with_subquery(isAsync);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` IN (
    SELECT `o`.`CustomerID`
    FROM `Orders` AS `o`
)
""");
        }

        public override async Task Contains_with_local_array_closure(bool isAsync)
        {
            await base.Contains_with_local_array_closure(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` IN ('ABCDE', 'ALFKI')
                    """,
                //
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` = 'ABCDE'
                    """);
        }

        public override async Task Contains_with_subquery_and_local_array_closure(bool isAsync)
        {
            await base.Contains_with_subquery_and_local_array_closure(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE EXISTS (
                        SELECT 1
                        FROM `Customers` AS `c0`
                        WHERE `c0`.`City` IN ('London', 'Buenos Aires') AND `c0`.`CustomerID` = `c`.`CustomerID`)
                    """,
                //
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE EXISTS (
                        SELECT 1
                        FROM `Customers` AS `c0`
                        WHERE `c0`.`City` = 'London' AND `c0`.`CustomerID` = `c`.`CustomerID`)
                    """);
        }

        public override async Task Contains_with_local_uint_array_closure(bool isAsync)
        {
            await base.Contains_with_local_uint_array_closure(isAsync);

            AssertSql(
                $"""
                    SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
                    FROM `Employees` AS `e`
                    WHERE `e`.`EmployeeID` IN (0, 1)
                    """,
                //
                $"""
                    SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
                    FROM `Employees` AS `e`
                    WHERE `e`.`EmployeeID` = 0
                    """);
        }

        public override async Task Contains_with_local_nullable_uint_array_closure(bool async)
        {
            await base.Contains_with_local_nullable_uint_array_closure(async);

            AssertSql(
                """
SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
FROM `Employees` AS `e`
WHERE `e`.`EmployeeID` IN (0, 1)
""",
                //
                """
SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
FROM `Employees` AS `e`
WHERE `e`.`EmployeeID` = 0
""");
        }

        public override async Task Contains_with_local_array_inline(bool isAsync)
        {
            await base.Contains_with_local_array_inline(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` IN ('ABCDE', 'ALFKI')
                    """);
        }

        public override async Task Contains_with_local_list_closure(bool isAsync)
        {
            await base.Contains_with_local_list_closure(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` IN ('ABCDE', 'ALFKI')
                    """);
        }

        public override async Task Contains_with_local_object_list_closure(bool isAsync)
        {
            await base.Contains_with_local_object_list_closure(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` IN ('ABCDE', 'ALFKI')
                    """);
        }

        public override async Task Contains_with_local_list_closure_all_null(bool isAsync)
        {
            await base.Contains_with_local_list_closure_all_null(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE 0 = 1
                    """);
        }

        public override async Task Contains_with_local_list_inline(bool isAsync)
        {
            await base.Contains_with_local_list_inline(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` IN ('ABCDE', 'ALFKI')
                    """);
        }

        public override async Task Contains_with_local_list_inline_closure_mix(bool isAsync)
        {
            await base.Contains_with_local_list_inline_closure_mix(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` IN ('ABCDE', 'ALFKI')
                    """,
                //
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` IN ('ABCDE', 'ANATR')
                    """);
        }

        public override async Task Contains_with_local_non_primitive_list_inline_closure_mix(bool isAsync)
        {
            await base.Contains_with_local_non_primitive_list_inline_closure_mix(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` IN ('ABCDE', 'ALFKI')
                    """,
                //
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` IN ('ABCDE', 'ANATR')
                    """);
        }

        public override async Task Contains_with_local_enumerable_closure(bool async)
        {
            await base.Contains_with_local_enumerable_closure(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` IN ('ABCDE', 'ALFKI')
""",
                //
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = 'ABCDE'
""");
        }

        public override async Task Contains_with_local_object_enumerable_closure(bool async)
        {
            await base.Contains_with_local_object_enumerable_closure(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` IN ('ABCDE', 'ALFKI')
""");
        }

        public override async Task Contains_with_local_enumerable_closure_all_null(bool async)
        {
            await base.Contains_with_local_enumerable_closure_all_null(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE 0 = 1
""");
        }

        public override async Task Contains_with_local_enumerable_inline(bool async)
        {
            // Issue #31776
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () =>
                    await base.Contains_with_local_enumerable_inline(async));

            AssertSql();
        }

        public override async Task Contains_with_local_enumerable_inline_closure_mix(bool async)
        {
            // Issue #31776
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () =>
                    await base.Contains_with_local_enumerable_inline_closure_mix(async));

            AssertSql();
        }

        public override async Task Contains_with_local_ordered_enumerable_closure(bool async)
        {
            await base.Contains_with_local_ordered_enumerable_closure(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` IN ('ABCDE', 'ALFKI')
""",
                //
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = 'ABCDE'
""");
        }

        public override async Task Contains_with_local_object_ordered_enumerable_closure(bool async)
        {
            await base.Contains_with_local_object_ordered_enumerable_closure(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` IN ('ABCDE', 'ALFKI')
""");
        }

        public override async Task Contains_with_local_ordered_enumerable_closure_all_null(bool async)
        {
            await base.Contains_with_local_ordered_enumerable_closure_all_null(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE 0 = 1
""");
        }

        public override async Task Contains_with_local_ordered_enumerable_inline(bool async)
        {
            await base.Contains_with_local_ordered_enumerable_inline(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` IN ('ABCDE', 'ALFKI')
""");
        }

        public override async Task Contains_with_local_ordered_enumerable_inline_closure_mix(bool async)
        {
            await base.Contains_with_local_ordered_enumerable_inline_closure_mix(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` IN ('ABCDE', 'ALFKI')
""",
                //
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` IN ('ABCDE', 'ANATR')
""");
        }

        public override async Task Contains_with_local_read_only_collection_closure(bool async)
        {
            await base.Contains_with_local_read_only_collection_closure(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` IN ('ABCDE', 'ALFKI')
""",
                //
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = 'ABCDE'
""");
        }

        public override async Task Contains_with_local_object_read_only_collection_closure(bool async)
        {
            await base.Contains_with_local_object_read_only_collection_closure(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` IN ('ABCDE', 'ALFKI')
""");
        }

        public override async Task Contains_with_local_ordered_read_only_collection_all_null(bool async)
        {
            await base.Contains_with_local_ordered_read_only_collection_all_null(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE 0 = 1
""");
        }

        public override async Task Contains_with_local_read_only_collection_inline(bool async)
        {
            await base.Contains_with_local_read_only_collection_inline(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` IN ('ABCDE', 'ALFKI')
""");
        }

        public override async Task Contains_with_local_read_only_collection_inline_closure_mix(bool async)
        {
            await base.Contains_with_local_read_only_collection_inline_closure_mix(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` IN ('ABCDE', 'ALFKI')
""",
                //
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` IN ('ABCDE', 'ANATR')
""");
        }

        public override async Task Contains_with_local_non_primitive_list_closure_mix(bool isAsync)
        {
            await base.Contains_with_local_non_primitive_list_closure_mix(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` IN ('ABCDE', 'ALFKI')
                    """);
        }

        public override async Task Contains_with_local_collection_false(bool isAsync)
        {
            await base.Contains_with_local_collection_false(isAsync);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` NOT IN ('ABCDE', 'ALFKI')
                    """);
        }

        public override async Task Contains_with_local_collection_complex_predicate_and(bool isAsync)
        {
            await base.Contains_with_local_collection_complex_predicate_and(isAsync);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` IN ('ALFKI', 'ABCDE') AND `c`.`CustomerID` IN ('ABCDE', 'ALFKI')
                    """);
        }

        public override async Task Contains_with_local_collection_complex_predicate_or(bool isAsync)
        {
            await base.Contains_with_local_collection_complex_predicate_or(isAsync);

            // issue #15994
            //            AssertSql(
            //                $@"SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
            //FROM `Customers` AS `c`
            //WHERE `c`.`CustomerID` IN ('ABCDE', 'ALFKI', 'ALFKI', 'ABCDE')");
        }

        public override async Task Contains_with_local_collection_complex_predicate_not_matching_ins1(bool isAsync)
        {
            await base.Contains_with_local_collection_complex_predicate_not_matching_ins1(isAsync);

            // issue #15994
            //            AssertSql(
            //                $@"SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
            //FROM `Customers` AS `c`
            //WHERE `c`.`CustomerID` IN ('ALFKI', 'ABCDE') OR `c`.`CustomerID` NOT IN ('ABCDE', 'ALFKI')");
        }

        public override async Task Contains_with_local_collection_complex_predicate_not_matching_ins2(bool isAsync)
        {
            await base.Contains_with_local_collection_complex_predicate_not_matching_ins2(isAsync);

            // issue #15944
            //            AssertSql(
            //                $@"SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
            //FROM `Customers` AS `c`
            //WHERE `c`.`CustomerID` IN ('ABCDE', 'ALFKI') AND `c`.`CustomerID` NOT IN ('ALFKI', 'ABCDE')");
        }

        public override async Task Contains_with_local_collection_sql_injection(bool isAsync)
        {
            await base.Contains_with_local_collection_sql_injection(isAsync);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` IN ('ALFKI', 'ABC'')); GO; DROP TABLE Orders; GO; --') OR `c`.`CustomerID` IN ('ALFKI', 'ABCDE')
                    """);
        }

        public override async Task Contains_with_local_collection_empty_closure(bool isAsync)
        {
            await base.Contains_with_local_collection_empty_closure(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE 0 = 1
                    """);
        }

        public override async Task Contains_with_local_collection_empty_inline(bool isAsync)
        {
            await base.Contains_with_local_collection_empty_inline(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    """);
        }

        public override async Task Contains_top_level(bool isAsync)
        {
            await base.Contains_top_level(isAsync);

            AssertSql(
                """
@p='ALFKI' (Size = 5)

SELECT IIF(@p IN (
        SELECT `c`.`CustomerID`
        FROM `Customers` AS `c`
    ), TRUE, FALSE)
FROM (SELECT COUNT(*) FROM `#Dual`)
""");
        }

        public override async Task Contains_with_local_anonymous_type_array_closure(bool async)
        {
            await AssertTranslationFailed(() => base.Contains_with_local_anonymous_type_array_closure(async));

            AssertSql();
        }

        public override async Task OfType_Select(bool async)
        {
            await base.OfType_Select(async);

            AssertSql(
                $"""
                    SELECT TOP 1 `c`.`City`
                    FROM `Orders` AS `o`
                    LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
                    ORDER BY `o`.`OrderID`
                    """);
        }

        public override async Task OfType_Select_OfType_Select(bool async)
        {
            await base.OfType_Select_OfType_Select(async);

            AssertSql(
                $"""
                    SELECT TOP 1 `c`.`City`
                    FROM `Orders` AS `o`
                    LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
                    ORDER BY `o`.`OrderID`
                    """);
        }


        public override async Task Average_with_non_matching_types_in_projection_doesnt_produce_second_explicit_cast(bool isAsync)
        {
            await base.Average_with_non_matching_types_in_projection_doesnt_produce_second_explicit_cast(isAsync);

            AssertSql(
"""
SELECT AVG(CDBL(CLNG(`o`.`OrderID`)))
FROM `Orders` AS `o`
WHERE `o`.`CustomerID` LIKE 'A%'
""");
        }

        public override async Task Max_with_non_matching_types_in_projection_introduces_explicit_cast(bool isAsync)
        {
            await base.Max_with_non_matching_types_in_projection_introduces_explicit_cast(isAsync);

            AssertSql(
"""
SELECT MAX(CLNG(`o`.`OrderID`))
FROM `Orders` AS `o`
WHERE `o`.`CustomerID` LIKE 'A%'
""");
        }

        public override async Task Min_with_non_matching_types_in_projection_introduces_explicit_cast(bool isAsync)
        {
            await base.Min_with_non_matching_types_in_projection_introduces_explicit_cast(isAsync);

            AssertSql(
"""
SELECT MIN(CLNG(`o`.`OrderID`))
FROM `Orders` AS `o`
WHERE `o`.`CustomerID` LIKE 'A%'
""");
        }

        public override async Task OrderBy_Take_Last_gives_correct_result(bool isAsync)
        {
            await base.OrderBy_Take_Last_gives_correct_result(isAsync);

            AssertSql(
                """
SELECT TOP 1 `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
FROM (
    SELECT TOP @p `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    ORDER BY `c`.`CustomerID`
) AS `c0`
ORDER BY `c0`.`CustomerID` DESC
""");
        }

        public override async Task OrderBy_Skip_Last_gives_correct_result(bool isAsync)
        {
            await base.OrderBy_Skip_Last_gives_correct_result(isAsync);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@__p_0='20'")}
                    
                    SELECT TOP 1 `t`.`CustomerID`, `t`.`Address`, `t`.`City`, `t`.`CompanyName`, `t`.`ContactName`, `t`.`ContactTitle`, `t`.`Country`, `t`.`Fax`, `t`.`Phone`, `t`.`PostalCode`, `t`.`Region`
                    FROM (
                        SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                        FROM `Customers` AS `c`
                        ORDER BY `c`.`CustomerID`
                        SKIP {AssertSqlHelper.Parameter("@__p_0")}
                    ) AS `t`
                    ORDER BY `t`.`CustomerID` DESC
                    """);
        }

        public override async Task Contains_over_entityType_should_rewrite_to_identity_equality(bool async)
        {
            await base.Contains_over_entityType_should_rewrite_to_identity_equality(async);

            AssertSql(
                """
SELECT TOP 2 `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Orders` AS `o`
WHERE `o`.`OrderID` = 10248
""",
                //
                """
@entity_equality_p_OrderID='10248' (Nullable = true)

SELECT IIF(EXISTS (
        SELECT 1
        FROM `Orders` AS `o`
        WHERE `o`.`CustomerID` = 'VINET' AND `o`.`OrderID` = @entity_equality_p_OrderID), TRUE, FALSE)
FROM (SELECT COUNT(*) FROM `#Dual`)
""");
        }

        public override async Task List_Contains_over_entityType_should_rewrite_to_identity_equality(bool isAsync)
        {
            await base.List_Contains_over_entityType_should_rewrite_to_identity_equality(isAsync);

            AssertSql(
                """
@entity_equality_someOrder_OrderID='10248' (Nullable = true)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE EXISTS (
    SELECT 1
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` = `o`.`CustomerID` AND `o`.`OrderID` = @entity_equality_someOrder_OrderID)
""");
        }

        public override async Task List_Contains_with_constant_list(bool isAsync)
        {
            await base.List_Contains_with_constant_list(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` IN ('ALFKI', 'ANATR')
                    """);
        }

        public override async Task List_Contains_with_parameter_list(bool isAsync)
        {
            await base.List_Contains_with_parameter_list(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` IN ('ALFKI', 'ANATR')
                    """);
        }

        public override async Task Contains_with_parameter_list_value_type_id(bool isAsync)
        {
            await base.Contains_with_parameter_list_value_type_id(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
                    FROM `Orders` AS `o`
                    WHERE `o`.`OrderID` IN (10248, 10249)
                    """);
        }

        public override async Task Contains_with_constant_list_value_type_id(bool isAsync)
        {
            await base.Contains_with_constant_list_value_type_id(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
                    FROM `Orders` AS `o`
                    WHERE `o`.`OrderID` IN (10248, 10249)
                    """);
        }

        public override async Task IImmutableSet_Contains_with_parameter(bool async)
        {
            await base.IImmutableSet_Contains_with_parameter(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = 'ALFKI'
""");
        }

        public override async Task IReadOnlySet_Contains_with_parameter(bool async)
        {
            await base.IReadOnlySet_Contains_with_parameter(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = 'ALFKI'
""");
        }

        public override async Task HashSet_Contains_with_parameter(bool isAsync)
        {
            await base.HashSet_Contains_with_parameter(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` = 'ALFKI'
                    """);
        }

        public override async Task ImmutableHashSet_Contains_with_parameter(bool isAsync)
        {
            await base.ImmutableHashSet_Contains_with_parameter(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` = 'ALFKI'
                    """);
        }

        public override async Task Contains_over_entityType_with_null_should_rewrite_to_false(bool async)
        {
            await base.Contains_over_entityType_with_null_should_rewrite_to_false(async);

            AssertSql(
                """
SELECT FALSE
FROM (SELECT COUNT(*) FROM `#Dual`)
""");
        }

        public override async Task Contains_over_entityType_with_null_should_rewrite_to_identity_equality_subquery(bool async)
        {
            await base.Contains_over_entityType_with_null_should_rewrite_to_identity_equality_subquery(async);

            AssertSql(
                """
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Orders` AS `o`
WHERE 0 = 1
""");
        }

        public override async Task Contains_over_entityType_with_null_in_projection(bool async)
        {
            await base.Contains_over_entityType_with_null_in_projection(async);

            AssertSql(
                """
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Orders` AS `o`
WHERE 0 = 1
""");
        }

        public override async Task Contains_over_scalar_with_null_should_rewrite_to_identity_equality_subquery(bool async)
        {
            await base.Contains_over_scalar_with_null_should_rewrite_to_identity_equality_subquery(async);

            AssertSql(
                """
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Orders` AS `o`
WHERE EXISTS (
    SELECT 1
    FROM `Orders` AS `o0`
    WHERE `o0`.`CustomerID` = 'VINET' AND `o0`.`EmployeeID` IS NULL)
""");
        }

        public override async Task Contains_over_entityType_with_null_should_rewrite_to_identity_equality_subquery_negated(bool async)
        {
            await base.Contains_over_entityType_with_null_should_rewrite_to_identity_equality_subquery_negated(async);

            AssertSql(
                """
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Orders` AS `o`
WHERE NOT EXISTS (
    SELECT 1
    FROM `Orders` AS `o0`
    WHERE `o0`.`CustomerID` = 'VINET' AND `o0`.`EmployeeID` IS NULL)
""");
        }

        public override async Task Contains_over_entityType_with_null_should_rewrite_to_identity_equality_subquery_complex(bool async)
        {
            await base.Contains_over_entityType_with_null_should_rewrite_to_identity_equality_subquery_complex(async);

            AssertSql(
                """
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Orders` AS `o`
WHERE IIF(EXISTS (
        SELECT 1
        FROM `Orders` AS `o0`
        WHERE `o0`.`CustomerID` = 'VINET' AND `o0`.`EmployeeID` IS NULL), TRUE, FALSE) = IIF(EXISTS (
        SELECT 1
        FROM `Orders` AS `o1`
        WHERE (`o1`.`CustomerID` <> 'VINET' OR `o1`.`CustomerID` IS NULL) AND `o1`.`EmployeeID` IS NULL), TRUE, FALSE)
""");
        }

        public override async Task Contains_over_nullable_scalar_with_null_in_subquery_translated_correctly(bool async)
        {
            await base.Contains_over_nullable_scalar_with_null_in_subquery_translated_correctly(async);

            AssertSql(
                """
SELECT IIF(EXISTS (
        SELECT 1
        FROM `Orders` AS `o0`
        WHERE `o0`.`CustomerID` = 'VINET' AND `o0`.`EmployeeID` IS NULL), TRUE, FALSE)
FROM `Orders` AS `o`
""");
        }

        public override async Task Contains_over_non_nullable_scalar_with_null_in_subquery_simplifies_to_false(bool async)
        {
            await base.Contains_over_non_nullable_scalar_with_null_in_subquery_simplifies_to_false(async);

            AssertSql(
                """
SELECT FALSE
FROM `Orders` AS `o`
""");
        }

        public override async Task Contains_over_entityType_should_materialize_when_composite(bool async)
        {
            await base.Contains_over_entityType_should_materialize_when_composite(async);

            AssertSql(
                """
SELECT `o`.`OrderID`, `o`.`ProductID`, `o`.`Discount`, `o`.`Quantity`, `o`.`UnitPrice`
FROM `Order Details` AS `o`
WHERE `o`.`ProductID` = 42 AND EXISTS (
    SELECT 1
    FROM `Order Details` AS `o0`
    WHERE `o0`.`OrderID` = `o`.`OrderID` AND `o0`.`ProductID` = `o`.`ProductID`)
""");
        }

        public override async Task Contains_over_entityType_should_materialize_when_composite2(bool async)
        {
            await base.Contains_over_entityType_should_materialize_when_composite2(async);

            AssertSql(
                """
SELECT `o`.`OrderID`, `o`.`ProductID`, `o`.`Discount`, `o`.`Quantity`, `o`.`UnitPrice`
FROM `Order Details` AS `o`
WHERE `o`.`ProductID` = 42 AND EXISTS (
    SELECT 1
    FROM `Order Details` AS `o0`
    WHERE `o0`.`OrderID` > 42 AND `o0`.`OrderID` = `o`.`OrderID` AND `o0`.`ProductID` = `o`.`ProductID`)
""");
        }

        public override async Task String_FirstOrDefault_in_projection_does_not_do_client_eval(bool async)
        {
            await base.String_FirstOrDefault_in_projection_does_not_do_client_eval(async);

            AssertSql(
                """
SELECT MID(`c`.`CustomerID`, 1, 1)
FROM `Customers` AS `c`
""");
        }

        public override async Task Project_constant_Sum(bool isAsync)
        {
            await base.Project_constant_Sum(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(1) IS NULL, 0, SUM(1))
                    FROM `Employees` AS `e`
                    """);
        }

        public override async Task Where_subquery_any_equals_operator(bool isAsync)
        {
            await base.Where_subquery_any_equals_operator(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` IN ('ABCDE', 'ALFKI', 'ANATR')
                    """);
        }

        public override async Task Where_subquery_any_equals(bool isAsync)
        {
            await base.Where_subquery_any_equals(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` IN ('ABCDE', 'ALFKI', 'ANATR')
                    """);
        }

        public override async Task Where_subquery_any_equals_static(bool isAsync)
        {
            await base.Where_subquery_any_equals_static(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` IN ('ABCDE', 'ALFKI', 'ANATR')
                    """);
        }

        public override async Task Where_subquery_where_any(bool isAsync)
        {
            await base.Where_subquery_where_any(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`City` = 'México D.F.' AND `c`.`CustomerID` IN ('ABCDE', 'ALFKI', 'ANATR')
                    """,
                //
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`City` = 'México D.F.' AND `c`.`CustomerID` IN ('ABCDE', 'ALFKI', 'ANATR')
                    """);
        }

        public override async Task Where_subquery_all_not_equals_operator(bool isAsync)
        {
            await base.Where_subquery_all_not_equals_operator(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` NOT IN ('ABCDE', 'ALFKI', 'ANATR')
                    """);
        }

        public override async Task Where_subquery_all_not_equals(bool isAsync)
        {
            await base.Where_subquery_all_not_equals(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` NOT IN ('ABCDE', 'ALFKI', 'ANATR')
                    """);
        }

        public override async Task Where_subquery_all_not_equals_static(bool isAsync)
        {
            await base.Where_subquery_all_not_equals_static(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` NOT IN ('ABCDE', 'ALFKI', 'ANATR')
                    """);
        }

        public override async Task Where_subquery_where_all(bool isAsync)
        {
            await base.Where_subquery_where_all(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`City` = 'México D.F.' AND `c`.`CustomerID` NOT IN ('ABCDE', 'ALFKI', 'ANATR')
                    """,
                //
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`City` = 'México D.F.' AND `c`.`CustomerID` NOT IN ('ABCDE', 'ALFKI', 'ANATR')
                    """);
        }

        public override async Task Cast_to_same_Type_Count_works(bool isAsync)
        {
            await base.Cast_to_same_Type_Count_works(isAsync);

            AssertSql(
                $"""
                    SELECT COUNT(*)
                    FROM `Customers` AS `c`
                    """);
        }

        public override async Task Cast_before_aggregate_is_preserved(bool isAsync)
        {
            await base.Cast_before_aggregate_is_preserved(isAsync);

            AssertSql(
                $"""
                    SELECT (
                        SELECT AVG(CDBL(`o`.`OrderID`))
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID`)
                    FROM `Customers` AS `c`
                    """);
        }

        public override async Task DefaultIfEmpty_selects_only_required_columns(bool isAsync)
        {
            await base.DefaultIfEmpty_selects_only_required_columns(isAsync);

            AssertSql(
                $"""
                    SELECT `p`.`ProductName`
                    FROM (
                        SELECT NULL AS `empty`
                    ) AS `empty`
                    LEFT JOIN `Products` AS `p` ON 1 = 1
                    """);
        }

        public override async Task Collection_Last_member_access_in_projection_translated(bool isAsync)
        {
            await base.Collection_Last_member_access_in_projection_translated(isAsync);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE (`c`.`CustomerID` LIKE 'F%') AND (
    SELECT TOP 1 `o`.`CustomerID`
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` = `o`.`CustomerID`
    ORDER BY `o`.`OrderID`) = `c`.`CustomerID`
""");
        }

        public override async Task Collection_LastOrDefault_member_access_in_projection_translated(bool isAsync)
        {
            await base.Collection_LastOrDefault_member_access_in_projection_translated(isAsync);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE (`c`.`CustomerID` LIKE 'F%') AND (
    SELECT TOP 1 `o`.`CustomerID`
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` = `o`.`CustomerID`
    ORDER BY `o`.`OrderID`) = `c`.`CustomerID`
""");
        }

        public override async Task Sum_over_explicit_cast_over_column(bool isAsync)
        {
            await base.Sum_over_explicit_cast_over_column(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(CLNG(`o`.`OrderID`)) IS NULL, 0, SUM(CLNG(`o`.`OrderID`)))
                    FROM `Orders` AS `o`
                    """);
        }

        public override async Task Count_on_projection_with_client_eval(bool isAsync)
        {
            await base.Count_on_projection_with_client_eval(isAsync);

            AssertSql(
                $"""
                    SELECT COUNT(*)
                    FROM `Orders` AS `o`
                    """,
                //
                $"""
                    SELECT COUNT(*)
                    FROM `Orders` AS `o`
                    """,
                //
                $"""
                    SELECT COUNT(*)
                    FROM `Orders` AS `o`
                    """);
        }

        public override async Task Average_on_nav_subquery_in_projection(bool async)
        {
            await base.Average_on_nav_subquery_in_projection(async);

            AssertSql(
                """
SELECT (
    SELECT AVG(CDBL(`o`.`OrderID`))
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` = `o`.`CustomerID`) AS `Ave`
FROM `Customers` AS `c`
ORDER BY `c`.`CustomerID`
""");
        }

        public override async Task Count_after_client_projection(bool async)
        {
            await base.Count_after_client_projection(async);

            AssertSql(
                """
SELECT COUNT(*)
FROM (
    SELECT TOP @p 1
    FROM `Orders` AS `o`
) AS `o0`
""");
        }

        public override async Task All_true(bool async)
        {
            await base.All_true(async);

            AssertSql(
                """
SELECT TRUE
FROM (SELECT COUNT(*) FROM `#Dual`)
""");
        }

        public override async Task Not_Any_false(bool async)
        {
            await base.Not_Any_false(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`
FROM `Customers` AS `c`
""");
        }

        public override async Task Contains_inside_aggregate_function_with_GroupBy(bool async)
        {
            await base.Contains_inside_aggregate_function_with_GroupBy(async);

            AssertSql(
                """
SELECT COUNT(IIF(`c`.`City` IN ('London', 'Berlin'), 1, NULL))
FROM `Customers` AS `c`
GROUP BY `c`.`Country`
""");
        }

        public override async Task Contains_inside_Average_without_GroupBy(bool async)
        {
            await base.Contains_inside_Average_without_GroupBy(async);

            AssertSql(
                """
SELECT AVG(IIF(`c`.`City` IN ('London', 'Berlin'), 1.0, 0.0))
FROM `Customers` AS `c`
""");
        }

        public override async Task Contains_inside_Sum_without_GroupBy(bool async)
        {
            await base.Contains_inside_Sum_without_GroupBy(async);

            AssertSql(
                """
SELECT IIF(SUM(IIF(`c`.`City` IN ('London', 'Berlin'), 1, 0)) IS NULL, 0, SUM(IIF(`c`.`City` IN ('London', 'Berlin'), 1, 0)))
FROM `Customers` AS `c`
""");
        }

        public override async Task Contains_inside_Count_without_GroupBy(bool async)
        {
            await base.Contains_inside_Count_without_GroupBy(async);

            AssertSql(
                """
SELECT COUNT(*)
FROM `Customers` AS `c`
WHERE `c`.`City` IN ('London', 'Berlin')
""");
        }

        public override async Task Contains_inside_LongCount_without_GroupBy(bool async)
        {
            await base.Contains_inside_LongCount_without_GroupBy(async);

            AssertSql(
                """
SELECT COUNT(*)
FROM `Customers` AS `c`
WHERE `c`.`City` IN ('London', 'Berlin')
""");
        }

        public override async Task Contains_inside_Max_without_GroupBy(bool async)
        {
            await base.Contains_inside_Max_without_GroupBy(async);

            AssertSql(
                """
SELECT MAX(IIF(`c`.`City` IN ('London', 'Berlin'), 1, 0))
FROM `Customers` AS `c`
""");
        }

        public override async Task Contains_inside_Min_without_GroupBy(bool async)
        {
            await base.Contains_inside_Min_without_GroupBy(async);

            AssertSql(
                """
SELECT MIN(IIF(`c`.`City` IN ('London', 'Berlin'), 1, 0))
FROM `Customers` AS `c`
""");
        }

        public override async Task Return_type_of_singular_operator_is_preserved(bool async)
        {
            await base.Return_type_of_singular_operator_is_preserved(async);

            AssertSql(
                """
SELECT TOP 1 `c`.`CustomerID`, `c`.`City`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = 'ALFKI'
""",
                //
                """
SELECT TOP 1 `c`.`CustomerID`, `c`.`City`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = 'ALFKI'
""",
                //
                """
SELECT TOP 2 `c`.`CustomerID`, `c`.`City`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = 'ALFKI'
""",
                //
                """
SELECT TOP 2 `c`.`CustomerID`, `c`.`City`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = 'ALFKI'
""",
                //
                """
SELECT TOP 1 `c`.`CustomerID`, `c`.`City`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` LIKE 'A%'
ORDER BY `c`.`CustomerID` DESC
""",
                //
                """
SELECT TOP 1 `c`.`CustomerID`, `c`.`City`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` LIKE 'A%'
ORDER BY `c`.`CustomerID` DESC
""");
        }

        public override async Task Type_casting_inside_sum(bool async)
        {
            await base.Type_casting_inside_sum(async);

            AssertSql(
                """
SELECT COALESCE(SUM(CAST([o].[Discount] AS decimal(18,2))), 0.0)
FROM [Order Details] AS [o]
""");
        }

        private void AssertSql(params string[] expected)
            => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);

        protected override void ClearLog()
            => Fixture.TestSqlLoggerFactory.Clear();
    }
}
