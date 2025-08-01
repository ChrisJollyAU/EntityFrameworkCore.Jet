﻿// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using EntityFrameworkCore.Jet.FunctionalTests.TestUtilities;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestModels.Northwind;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;
using Xunit.Abstractions;

namespace EntityFrameworkCore.Jet.FunctionalTests.Query
{
    public class NorthwindGroupByQueryJetTest : NorthwindGroupByQueryRelationalTestBase<NorthwindQueryJetFixture<NoopModelCustomizer>>
    {
        // ReSharper disable once UnusedParameter.Local
        public NorthwindGroupByQueryJetTest(NorthwindQueryJetFixture<NoopModelCustomizer> fixture, ITestOutputHelper testOutputHelper)
            : base(fixture)
        {
            Fixture.TestSqlLoggerFactory.Clear();
            Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
        }

        [ConditionalFact]
        public virtual void Check_all_tests_overridden()
            => TestHelpers.AssertAllMethodsOverridden(GetType());

        public override async Task GroupBy_Property_Select_Average(bool isAsync)
        {
            await base.GroupBy_Property_Select_Average(isAsync);

            AssertSql(
                $"""
                    SELECT AVG(CDBL(`o`.`OrderID`))
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);

            // Validating that we don't generate warning when translating GroupBy. See Issue#11157
            Assert.DoesNotContain(
                "The LINQ expression 'GroupBy(`o`.CustomerID, `o`)' could not be translated and will be evaluated locally.",
                Fixture.TestSqlLoggerFactory.Log.Select(l => l.Message));
        }

        public override async Task GroupBy_Property_Select_Average_with_group_enumerable_projected(bool async)
        {
            await base.GroupBy_Property_Select_Average_with_group_enumerable_projected(async);

            AssertSql();
        }

        public override async Task GroupBy_Property_Select_Count(bool isAsync)
        {
            await base.GroupBy_Property_Select_Count(isAsync);

            AssertSql(
                $"""
                    SELECT COUNT(*)
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_Select_LongCount(bool isAsync)
        {
            await base.GroupBy_Property_Select_LongCount(isAsync);

            AssertSql(
                $"""
                    SELECT COUNT(*)
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_Select_Count_with_nulls(bool async)
        {
            await base.GroupBy_Property_Select_Count_with_nulls(async);

            AssertSql(
                """
SELECT `c`.`City`, COUNT(*) AS `Faxes`
FROM `Customers` AS `c`
GROUP BY `c`.`City`
""");
        }

        public override async Task GroupBy_Property_Select_LongCount_with_nulls(bool async)
        {
            await base.GroupBy_Property_Select_LongCount_with_nulls(async);

            AssertSql(
                """
SELECT `c`.`City`, COUNT(*) AS `Faxes`
FROM `Customers` AS `c`
GROUP BY `c`.`City`
""");
        }

        public override async Task GroupBy_Property_Select_Max(bool isAsync)
        {
            await base.GroupBy_Property_Select_Max(isAsync);

            AssertSql(
                $"""
                    SELECT MAX(`o`.`OrderID`)
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_Select_Min(bool isAsync)
        {
            await base.GroupBy_Property_Select_Min(isAsync);

            AssertSql(
                $"""
                    SELECT MIN(`o`.`OrderID`)
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_Select_Sum(bool isAsync)
        {
            await base.GroupBy_Property_Select_Sum(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`))
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_Select_Sum_Min_Max_Avg(bool isAsync)
        {
            await base.GroupBy_Property_Select_Sum_Min_Max_Avg(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `Sum`, MIN(`o`.`OrderID`) AS `Min`, MAX(`o`.`OrderID`) AS `Max`, AVG(CDBL(`o`.`OrderID`)) AS `Avg`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_Select_Key_Average(bool isAsync)
        {
            await base.GroupBy_Property_Select_Key_Average(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`CustomerID` AS `Key`, AVG(CDBL(`o`.`OrderID`)) AS `Average`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_Select_Key_Count(bool isAsync)
        {
            await base.GroupBy_Property_Select_Key_Count(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`CustomerID` AS `Key`, COUNT(*) AS `Count`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_Select_Key_LongCount(bool isAsync)
        {
            await base.GroupBy_Property_Select_Key_LongCount(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`CustomerID` AS `Key`, COUNT(*) AS `LongCount`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_Select_Key_Max(bool isAsync)
        {
            await base.GroupBy_Property_Select_Key_Max(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`CustomerID` AS `Key`, MAX(`o`.`OrderID`) AS `Max`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_Select_Key_Min(bool isAsync)
        {
            await base.GroupBy_Property_Select_Key_Min(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`CustomerID` AS `Key`, MIN(`o`.`OrderID`) AS `Min`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_Select_Key_Sum(bool isAsync)
        {
            await base.GroupBy_Property_Select_Key_Sum(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`CustomerID` AS `Key`, IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `Sum`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_Select_Key_Sum_Min_Max_Avg(bool isAsync)
        {
            await base.GroupBy_Property_Select_Key_Sum_Min_Max_Avg(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`CustomerID` AS `Key`, IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `Sum`, MIN(`o`.`OrderID`) AS `Min`, MAX(`o`.`OrderID`) AS `Max`, AVG(CDBL(`o`.`OrderID`)) AS `Avg`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_Select_Sum_Min_Key_Max_Avg(bool isAsync)
        {
            await base.GroupBy_Property_Select_Sum_Min_Key_Max_Avg(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `Sum`, MIN(`o`.`OrderID`) AS `Min`, `o`.`CustomerID` AS `Key`, MAX(`o`.`OrderID`) AS `Max`, AVG(CDBL(`o`.`OrderID`)) AS `Avg`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_Select_key_multiple_times_and_aggregate(bool isAsync)
        {
            await base.GroupBy_Property_Select_key_multiple_times_and_aggregate(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`CustomerID` AS `Key1`, IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `Sum`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_Select_Key_with_constant(bool isAsync)
        {
            await base.GroupBy_Property_Select_Key_with_constant(isAsync);

            AssertSql(
                """
SELECT `o0`.`Name`, `o0`.`CustomerID` AS `Value`, COUNT(*) AS `Count`
FROM (
    SELECT `o`.`CustomerID`, 'CustomerID' AS `Name`
    FROM `Orders` AS `o`
) AS `o0`
GROUP BY `o0`.`Name`, `o0`.`CustomerID`
""");
        }

        public override async Task GroupBy_aggregate_projecting_conditional_expression(bool isAsync)
        {
            await base.GroupBy_aggregate_projecting_conditional_expression(isAsync);

            AssertSql(
                """
SELECT `o`.`OrderDate` AS `Key`, IIF(COUNT(*) = 0, 1, IIF(SUM(IIF((`o`.`OrderID` MOD 2) = 0, 1, 0)) IS NULL, 0, SUM(IIF((`o`.`OrderID` MOD 2) = 0, 1, 0))) \ COUNT(*)) AS `SomeValue`
FROM `Orders` AS `o`
GROUP BY `o`.`OrderDate`
""");
        }

        public override async Task GroupBy_aggregate_projecting_conditional_expression_based_on_group_key(bool isAsync)
        {
            await base.GroupBy_aggregate_projecting_conditional_expression_based_on_group_key(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(`o`.`OrderDate` IS NULL, 'is null', 'is not null') AS `Key`, IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `Sum`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`OrderDate`
                    """);
        }

        public override async Task GroupBy_anonymous_Select_Average(bool isAsync)
        {
            await base.GroupBy_anonymous_Select_Average(isAsync);

            AssertSql(
                $"""
                    SELECT AVG(CDBL(`o`.`OrderID`))
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_anonymous_Select_Count(bool isAsync)
        {
            await base.GroupBy_anonymous_Select_Count(isAsync);

            AssertSql(
                $"""
                    SELECT COUNT(*)
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_anonymous_Select_LongCount(bool isAsync)
        {
            await base.GroupBy_anonymous_Select_LongCount(isAsync);

            AssertSql(
                $"""
                    SELECT COUNT(*)
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_anonymous_Select_Max(bool isAsync)
        {
            await base.GroupBy_anonymous_Select_Max(isAsync);

            AssertSql(
                $"""
                    SELECT MAX(`o`.`OrderID`)
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_anonymous_Select_Min(bool isAsync)
        {
            await base.GroupBy_anonymous_Select_Min(isAsync);

            AssertSql(
                $"""
                    SELECT MIN(`o`.`OrderID`)
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_anonymous_Select_Sum(bool isAsync)
        {
            await base.GroupBy_anonymous_Select_Sum(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`))
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_anonymous_Select_Sum_Min_Max_Avg(bool isAsync)
        {
            await base.GroupBy_anonymous_Select_Sum_Min_Max_Avg(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `Sum`, MIN(`o`.`OrderID`) AS `Min`, MAX(`o`.`OrderID`) AS `Max`, AVG(CDBL(`o`.`OrderID`)) AS `Avg`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_anonymous_with_alias_Select_Key_Sum(bool isAsync)
        {
            await base.GroupBy_anonymous_with_alias_Select_Key_Sum(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`CustomerID` AS `Key`, IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `Sum`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Composite_Select_Average(bool isAsync)
        {
            await base.GroupBy_Composite_Select_Average(isAsync);

            AssertSql(
                $"""
                    SELECT AVG(CDBL(`o`.`OrderID`))
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`, `o`.`EmployeeID`
                    """);
        }

        public override async Task GroupBy_Composite_Select_Count(bool isAsync)
        {
            await base.GroupBy_Composite_Select_Count(isAsync);

            AssertSql(
                $"""
                    SELECT COUNT(*)
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`, `o`.`EmployeeID`
                    """);
        }

        public override async Task GroupBy_Composite_Select_LongCount(bool isAsync)
        {
            await base.GroupBy_Composite_Select_LongCount(isAsync);

            AssertSql(
                $"""
                    SELECT COUNT(*)
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`, `o`.`EmployeeID`
                    """);
        }

        public override async Task GroupBy_Composite_Select_Max(bool isAsync)
        {
            await base.GroupBy_Composite_Select_Max(isAsync);

            AssertSql(
                $"""
                    SELECT MAX(`o`.`OrderID`)
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`, `o`.`EmployeeID`
                    """);
        }

        public override async Task GroupBy_Composite_Select_Min(bool isAsync)
        {
            await base.GroupBy_Composite_Select_Min(isAsync);

            AssertSql(
                $"""
                    SELECT MIN(`o`.`OrderID`)
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`, `o`.`EmployeeID`
                    """);
        }

        public override async Task GroupBy_Composite_Select_Sum(bool isAsync)
        {
            await base.GroupBy_Composite_Select_Sum(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`))
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`, `o`.`EmployeeID`
                    """);
        }

        public override async Task GroupBy_Composite_Select_Sum_Min_Max_Avg(bool isAsync)
        {
            await base.GroupBy_Composite_Select_Sum_Min_Max_Avg(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `Sum`, MIN(`o`.`OrderID`) AS `Min`, MAX(`o`.`OrderID`) AS `Max`, AVG(CDBL(`o`.`OrderID`)) AS `Avg`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`, `o`.`EmployeeID`
                    """);
        }

        public override async Task GroupBy_Composite_Select_Key_Average(bool isAsync)
        {
            await base.GroupBy_Composite_Select_Key_Average(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`CustomerID`, `o`.`EmployeeID`, AVG(CDBL(`o`.`OrderID`)) AS `Average`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`, `o`.`EmployeeID`
                    """);
        }

        public override async Task GroupBy_Composite_Select_Key_Count(bool isAsync)
        {
            await base.GroupBy_Composite_Select_Key_Count(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`CustomerID`, `o`.`EmployeeID`, COUNT(*) AS `Count`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`, `o`.`EmployeeID`
                    """);
        }

        public override async Task GroupBy_Composite_Select_Key_LongCount(bool isAsync)
        {
            await base.GroupBy_Composite_Select_Key_LongCount(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`CustomerID`, `o`.`EmployeeID`, COUNT(*) AS `LongCount`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`, `o`.`EmployeeID`
                    """);
        }

        public override async Task GroupBy_Composite_Select_Key_Max(bool isAsync)
        {
            await base.GroupBy_Composite_Select_Key_Max(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`CustomerID`, `o`.`EmployeeID`, MAX(`o`.`OrderID`) AS `Max`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`, `o`.`EmployeeID`
                    """);
        }

        public override async Task GroupBy_Composite_Select_Key_Min(bool isAsync)
        {
            await base.GroupBy_Composite_Select_Key_Min(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`CustomerID`, `o`.`EmployeeID`, MIN(`o`.`OrderID`) AS `Min`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`, `o`.`EmployeeID`
                    """);
        }

        public override async Task GroupBy_Composite_Select_Key_Sum(bool isAsync)
        {
            await base.GroupBy_Composite_Select_Key_Sum(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`CustomerID`, `o`.`EmployeeID`, IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `Sum`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`, `o`.`EmployeeID`
                    """);
        }

        public override async Task GroupBy_Composite_Select_Key_Sum_Min_Max_Avg(bool isAsync)
        {
            await base.GroupBy_Composite_Select_Key_Sum_Min_Max_Avg(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`CustomerID`, `o`.`EmployeeID`, IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `Sum`, MIN(`o`.`OrderID`) AS `Min`, MAX(`o`.`OrderID`) AS `Max`, AVG(CDBL(`o`.`OrderID`)) AS `Avg`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`, `o`.`EmployeeID`
                    """);
        }

        public override async Task GroupBy_Composite_Select_Sum_Min_Key_Max_Avg(bool isAsync)
        {
            await base.GroupBy_Composite_Select_Sum_Min_Key_Max_Avg(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `Sum`, MIN(`o`.`OrderID`) AS `Min`, `o`.`CustomerID`, `o`.`EmployeeID`, MAX(`o`.`OrderID`) AS `Max`, AVG(CDBL(`o`.`OrderID`)) AS `Avg`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`, `o`.`EmployeeID`
                    """);
        }

        public override async Task GroupBy_Composite_Select_Sum_Min_Key_flattened_Max_Avg(bool isAsync)
        {
            await base.GroupBy_Composite_Select_Sum_Min_Key_flattened_Max_Avg(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `Sum`, MIN(`o`.`OrderID`) AS `Min`, `o`.`CustomerID`, `o`.`EmployeeID`, MAX(`o`.`OrderID`) AS `Max`, AVG(CDBL(`o`.`OrderID`)) AS `Avg`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`, `o`.`EmployeeID`
                    """);
        }

        public override async Task GroupBy_Dto_as_key_Select_Sum(bool isAsync)
        {
            await base.GroupBy_Dto_as_key_Select_Sum(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `Sum`, `o`.`CustomerID`, `o`.`EmployeeID`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`, `o`.`EmployeeID`
                    """);
        }

        public override async Task GroupBy_Dto_as_element_selector_Select_Sum(bool isAsync)
        {
            await base.GroupBy_Dto_as_element_selector_Select_Sum(isAsync);

            AssertSql(
                """
    SELECT IIF(SUM(IIF(`o`.`EmployeeID` IS NULL, NULL, CLNG(`o`.`EmployeeID`))) IS NULL, 0, SUM(IIF(`o`.`EmployeeID` IS NULL, NULL, CLNG(`o`.`EmployeeID`)))) AS `Sum`, `o`.`CustomerID` AS `Key`
    FROM `Orders` AS `o`
    GROUP BY `o`.`CustomerID`
    """);
        }

        public override async Task GroupBy_Composite_Select_Dto_Sum_Min_Key_flattened_Max_Avg(bool isAsync)
        {
            await base.GroupBy_Composite_Select_Dto_Sum_Min_Key_flattened_Max_Avg(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `Sum`, MIN(`o`.`OrderID`) AS `Min`, `o`.`CustomerID` AS `CustomerId`, `o`.`EmployeeID` AS `EmployeeId`, MAX(`o`.`OrderID`) AS `Max`, AVG(CDBL(`o`.`OrderID`)) AS `Avg`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`, `o`.`EmployeeID`
                    """);
        }

        public override async Task GroupBy_Composite_Select_Sum_Min_part_Key_flattened_Max_Avg(bool isAsync)
        {
            await base.GroupBy_Composite_Select_Sum_Min_part_Key_flattened_Max_Avg(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `Sum`, MIN(`o`.`OrderID`) AS `Min`, `o`.`CustomerID`, MAX(`o`.`OrderID`) AS `Max`, AVG(CDBL(`o`.`OrderID`)) AS `Avg`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`, `o`.`EmployeeID`
                    """);
        }

        public override async Task GroupBy_Constant_Select_Sum_Min_Key_Max_Avg(bool isAsync)
        {
            await base.GroupBy_Constant_Select_Sum_Min_Key_Max_Avg(isAsync);

            AssertSql(
                """
SELECT IIF(SUM(`o0`.`OrderID`) IS NULL, 0, SUM(`o0`.`OrderID`)) AS `Sum`, MIN(`o0`.`OrderID`) AS `Min`, `o0`.`Key`, MAX(`o0`.`OrderID`) AS `Max`, AVG(CDBL(`o0`.`OrderID`)) AS `Avg`
FROM (
    SELECT `o`.`OrderID`, 2 AS `Key`
    FROM `Orders` AS `o`
) AS `o0`
GROUP BY `o0`.`Key`
""");
        }

        public override async Task GroupBy_Constant_with_element_selector_Select_Sum(bool isAsync)
        {
            await base.GroupBy_Constant_with_element_selector_Select_Sum(isAsync);

            AssertSql(
                """
SELECT IIF(SUM(`o0`.`OrderID`) IS NULL, 0, SUM(`o0`.`OrderID`)) AS `Sum`
FROM (
    SELECT `o`.`OrderID`, 2 AS `Key`
    FROM `Orders` AS `o`
) AS `o0`
GROUP BY `o0`.`Key`
""");
        }

        public override async Task GroupBy_Constant_with_element_selector_Select_Sum2(bool isAsync)
        {
            await base.GroupBy_Constant_with_element_selector_Select_Sum2(isAsync);

            AssertSql(
                """
SELECT IIF(SUM(`o0`.`OrderID`) IS NULL, 0, SUM(`o0`.`OrderID`)) AS `Sum`
FROM (
    SELECT `o`.`OrderID`, 2 AS `Key`
    FROM `Orders` AS `o`
) AS `o0`
GROUP BY `o0`.`Key`
""");
        }

        public override async Task GroupBy_Constant_with_element_selector_Select_Sum3(bool isAsync)
        {
            await base.GroupBy_Constant_with_element_selector_Select_Sum3(isAsync);

            AssertSql(
                """
SELECT IIF(SUM(`o0`.`OrderID`) IS NULL, 0, SUM(`o0`.`OrderID`)) AS `Sum`
FROM (
    SELECT `o`.`OrderID`, 2 AS `Key`
    FROM `Orders` AS `o`
) AS `o0`
GROUP BY `o0`.`Key`
""");
        }

        public override async Task GroupBy_after_predicate_Constant_Select_Sum_Min_Key_Max_Avg(bool isAsync)
        {
            await base.GroupBy_after_predicate_Constant_Select_Sum_Min_Key_Max_Avg(isAsync);

            AssertSql(
                """
SELECT IIF(SUM(`o0`.`OrderID`) IS NULL, 0, SUM(`o0`.`OrderID`)) AS `Sum`, MIN(`o0`.`OrderID`) AS `Min`, `o0`.`Key` AS `Random`, MAX(`o0`.`OrderID`) AS `Max`, AVG(CDBL(`o0`.`OrderID`)) AS `Avg`
FROM (
    SELECT `o`.`OrderID`, 2 AS `Key`
    FROM `Orders` AS `o`
    WHERE `o`.`OrderID` > 10500
) AS `o0`
GROUP BY `o0`.`Key`
""");
        }

        public override async Task GroupBy_Constant_with_element_selector_Select_Sum_Min_Key_Max_Avg(bool isAsync)
        {
            await base.GroupBy_Constant_with_element_selector_Select_Sum_Min_Key_Max_Avg(isAsync);

            AssertSql(
                """
SELECT IIF(SUM(`o0`.`OrderID`) IS NULL, 0, SUM(`o0`.`OrderID`)) AS `Sum`, `o0`.`Key`
FROM (
    SELECT `o`.`OrderID`, 2 AS `Key`
    FROM `Orders` AS `o`
) AS `o0`
GROUP BY `o0`.`Key`
""");
        }

        public override async Task GroupBy_constant_with_where_on_grouping_with_aggregate_operators(bool async)
        {
            await base.GroupBy_constant_with_where_on_grouping_with_aggregate_operators(async);

            AssertSql(
                """
SELECT MIN(IIF(1 = `o0`.`Key`, `o0`.`OrderDate`, NULL)) AS `Min`, MAX(IIF(1 = `o0`.`Key`, `o0`.`OrderDate`, NULL)) AS `Max`, IIF(SUM(IIF(1 = `o0`.`Key`, `o0`.`OrderID`, NULL)) IS NULL, 0, SUM(IIF(1 = `o0`.`Key`, `o0`.`OrderID`, NULL))) AS `Sum`, AVG(IIF(1 = `o0`.`Key`, CDBL(`o0`.`OrderID`), NULL)) AS `Average`
FROM (
    SELECT `o`.`OrderID`, `o`.`OrderDate`, 1 AS `Key`
    FROM `Orders` AS `o`
) AS `o0`
GROUP BY `o0`.`Key`
ORDER BY `o0`.`Key`
""");
        }

        public override async Task GroupBy_param_Select_Sum_Min_Key_Max_Avg(bool isAsync)
        {
            await base.GroupBy_param_Select_Sum_Min_Key_Max_Avg(isAsync);

            AssertSql(
                """
@a='2'

SELECT IIF(SUM(`o0`.`OrderID`) IS NULL, 0, SUM(`o0`.`OrderID`)) AS `Sum`, MIN(`o0`.`OrderID`) AS `Min`, `o0`.`Key`, MAX(`o0`.`OrderID`) AS `Max`, AVG(CDBL(`o0`.`OrderID`)) AS `Avg`
FROM (
    SELECT `o`.`OrderID`, CLNG(@a) AS `Key`
    FROM `Orders` AS `o`
) AS `o0`
GROUP BY `o0`.`Key`
""");
        }

        public override async Task GroupBy_param_with_element_selector_Select_Sum(bool isAsync)
        {
            await base.GroupBy_param_with_element_selector_Select_Sum(isAsync);

            AssertSql(
                """
@a='2'

SELECT IIF(SUM(`o0`.`OrderID`) IS NULL, 0, SUM(`o0`.`OrderID`)) AS `Sum`
FROM (
    SELECT `o`.`OrderID`, CLNG(@a) AS `Key`
    FROM `Orders` AS `o`
) AS `o0`
GROUP BY `o0`.`Key`
""");
        }

        public override async Task GroupBy_param_with_element_selector_Select_Sum2(bool isAsync)
        {
            await base.GroupBy_param_with_element_selector_Select_Sum2(isAsync);

            AssertSql(
                """
@a='2'

SELECT IIF(SUM(`o0`.`OrderID`) IS NULL, 0, SUM(`o0`.`OrderID`)) AS `Sum`
FROM (
    SELECT `o`.`OrderID`, CLNG(@a) AS `Key`
    FROM `Orders` AS `o`
) AS `o0`
GROUP BY `o0`.`Key`
""");
        }

        public override async Task GroupBy_param_with_element_selector_Select_Sum3(bool isAsync)
        {
            await base.GroupBy_param_with_element_selector_Select_Sum3(isAsync);

            AssertSql(
                """
@a='2'

SELECT IIF(SUM(`o0`.`OrderID`) IS NULL, 0, SUM(`o0`.`OrderID`)) AS `Sum`
FROM (
    SELECT `o`.`OrderID`, CLNG(@a) AS `Key`
    FROM `Orders` AS `o`
) AS `o0`
GROUP BY `o0`.`Key`
""");
        }

        public override async Task GroupBy_param_with_element_selector_Select_Sum_Min_Key_Max_Avg(bool isAsync)
        {
            await base.GroupBy_param_with_element_selector_Select_Sum_Min_Key_Max_Avg(isAsync);

            AssertSql(
                """
@a='2'

SELECT IIF(SUM(`o0`.`OrderID`) IS NULL, 0, SUM(`o0`.`OrderID`)) AS `Sum`, `o0`.`Key`
FROM (
    SELECT `o`.`OrderID`, CLNG(@a) AS `Key`
    FROM `Orders` AS `o`
) AS `o0`
GROUP BY `o0`.`Key`
""");
        }

        public override async Task GroupBy_anonymous_key_type_mismatch_with_aggregate(bool isAsync)
        {
            await base.GroupBy_anonymous_key_type_mismatch_with_aggregate(isAsync);

            AssertSql(
                """
SELECT COUNT(*) AS `I0`, `o0`.`I0` AS `I1`
FROM (
    SELECT DATEPART('yyyy', `o`.`OrderDate`) AS `I0`
    FROM `Orders` AS `o`
) AS `o0`
GROUP BY `o0`.`I0`
ORDER BY `o0`.`I0`
""");
        }

        public override async Task GroupBy_Property_scalar_element_selector_Average(bool isAsync)
        {
            await base.GroupBy_Property_scalar_element_selector_Average(isAsync);

            AssertSql(
                $"""
                    SELECT AVG(CDBL(`o`.`OrderID`))
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_scalar_element_selector_Count(bool isAsync)
        {
            await base.GroupBy_Property_scalar_element_selector_Count(isAsync);

            AssertSql(
                $"""
                    SELECT COUNT(*)
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_scalar_element_selector_LongCount(bool isAsync)
        {
            await base.GroupBy_Property_scalar_element_selector_LongCount(isAsync);

            AssertSql(
                $"""
                    SELECT COUNT(*)
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_scalar_element_selector_Max(bool isAsync)
        {
            await base.GroupBy_Property_scalar_element_selector_Max(isAsync);

            AssertSql(
                $"""
                    SELECT MAX(`o`.`OrderID`)
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_scalar_element_selector_Min(bool isAsync)
        {
            await base.GroupBy_Property_scalar_element_selector_Min(isAsync);

            AssertSql(
                $"""
                    SELECT MIN(`o`.`OrderID`)
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_scalar_element_selector_Sum(bool isAsync)
        {
            await base.GroupBy_Property_scalar_element_selector_Sum(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`))
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_scalar_element_selector_Sum_Min_Max_Avg(bool isAsync)
        {
            await base.GroupBy_Property_scalar_element_selector_Sum_Min_Max_Avg(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `Sum`, MIN(`o`.`OrderID`) AS `Min`, MAX(`o`.`OrderID`) AS `Max`, AVG(CDBL(`o`.`OrderID`)) AS `Avg`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_anonymous_element_selector_Average(bool isAsync)
        {
            await base.GroupBy_Property_anonymous_element_selector_Average(isAsync);

            AssertSql(
                $"""
                    SELECT AVG(CDBL(`o`.`OrderID`))
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_anonymous_element_selector_Count(bool isAsync)
        {
            await base.GroupBy_Property_anonymous_element_selector_Count(isAsync);

            AssertSql(
                $"""
                    SELECT COUNT(*)
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_anonymous_element_selector_LongCount(bool isAsync)
        {
            await base.GroupBy_Property_anonymous_element_selector_LongCount(isAsync);

            AssertSql(
                $"""
                    SELECT COUNT(*)
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_anonymous_element_selector_Max(bool isAsync)
        {
            await base.GroupBy_Property_anonymous_element_selector_Max(isAsync);

            AssertSql(
                $"""
                    SELECT MAX(`o`.`OrderID`)
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_anonymous_element_selector_Min(bool isAsync)
        {
            await base.GroupBy_Property_anonymous_element_selector_Min(isAsync);

            AssertSql(
                $"""
                    SELECT MIN(`o`.`OrderID`)
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_anonymous_element_selector_Sum(bool isAsync)
        {
            await base.GroupBy_Property_anonymous_element_selector_Sum(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`))
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_anonymous_element_selector_Sum_Min_Max_Avg(bool isAsync)
        {
            await base.GroupBy_Property_anonymous_element_selector_Sum_Min_Max_Avg(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `Sum`, MIN(`o`.`EmployeeID`) AS `Min`, MAX(`o`.`EmployeeID`) AS `Max`, AVG(CDBL(`o`.`OrderID`)) AS `Avg`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_element_selector_complex_aggregate(bool isAsync)
        {
            await base.GroupBy_element_selector_complex_aggregate(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(`o`.`OrderID` + 1) IS NULL, 0, SUM(`o`.`OrderID` + 1))
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_element_selector_complex_aggregate2(bool isAsync)
        {
            await base.GroupBy_element_selector_complex_aggregate2(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(`o`.`OrderID` + 1) IS NULL, 0, SUM(`o`.`OrderID` + 1))
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_element_selector_complex_aggregate3(bool isAsync)
        {
            await base.GroupBy_element_selector_complex_aggregate3(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(`o`.`OrderID` + 1) IS NULL, 0, SUM(`o`.`OrderID` + 1))
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_element_selector_complex_aggregate4(bool isAsync)
        {
            await base.GroupBy_element_selector_complex_aggregate4(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(`o`.`OrderID` + 1) IS NULL, 0, SUM(`o`.`OrderID` + 1))
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task Element_selector_with_case_block_repeated_inside_another_case_block_in_projection(bool async)
        {
            await base.Element_selector_with_case_block_repeated_inside_another_case_block_in_projection(async);

            AssertSql(
                """
SELECT `o`.`OrderID`, IIF(SUM(IIF(`o`.`CustomerID` = 'ALFKI', IIF(`o`.`OrderID` > 1000, `o`.`OrderID`, -`o`.`OrderID`), -IIF(`o`.`OrderID` > 1000, `o`.`OrderID`, -`o`.`OrderID`))) IS NULL, 0, SUM(IIF(`o`.`CustomerID` = 'ALFKI', IIF(`o`.`OrderID` > 1000, `o`.`OrderID`, -`o`.`OrderID`), -IIF(`o`.`OrderID` > 1000, `o`.`OrderID`, -`o`.`OrderID`)))) AS `Aggregate`
FROM `Orders` AS `o`
GROUP BY `o`.`OrderID`
""");
        }

        public override async Task GroupBy_conditional_properties(bool async)
        {
            await base.GroupBy_conditional_properties(async);

            AssertSql(
                """
SELECT `o0`.`OrderMonth`, `o0`.`CustomerID` AS `Customer`, COUNT(*) AS `Count`
FROM (
    SELECT `o`.`CustomerID`, CVar(NULL) AS `OrderMonth`
    FROM `Orders` AS `o`
) AS `o0`
GROUP BY `o0`.`OrderMonth`, `o0`.`CustomerID`
""");
        }

        public override async Task GroupBy_empty_key_Aggregate(bool isAsync)
        {
            await base.GroupBy_empty_key_Aggregate(isAsync);

            AssertSql(
                """
SELECT IIF(SUM(`o0`.`OrderID`) IS NULL, 0, SUM(`o0`.`OrderID`))
FROM (
    SELECT `o`.`OrderID`, 1 AS `Key`
    FROM `Orders` AS `o`
) AS `o0`
GROUP BY `o0`.`Key`
""");
        }

        public override async Task GroupBy_empty_key_Aggregate_Key(bool isAsync)
        {
            await base.GroupBy_empty_key_Aggregate_Key(isAsync);

            AssertSql(
                """
SELECT IIF(SUM(`o0`.`OrderID`) IS NULL, 0, SUM(`o0`.`OrderID`)) AS `Sum`
FROM (
    SELECT `o`.`OrderID`, 1 AS `Key`
    FROM `Orders` AS `o`
) AS `o0`
GROUP BY `o0`.`Key`
""");
        }

        public override async Task OrderBy_GroupBy_Aggregate(bool isAsync)
        {
            await base.OrderBy_GroupBy_Aggregate(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`))
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task OrderBy_Skip_GroupBy_Aggregate(bool isAsync)
        {
            await base.OrderBy_Skip_GroupBy_Aggregate(isAsync);

            AssertSql(
                $"""
                    SELECT AVG(IIF(`t`.`OrderID` IS NULL, NULL, CDBL(`t`.`OrderID`)))
                    FROM (
                        SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
                        FROM `Orders` AS `o`
                        ORDER BY `o`.`OrderID`
                        SKIP 80
                    ) AS `t`
                    GROUP BY `t`.`CustomerID`
                    """);
        }

        public override async Task OrderBy_Take_GroupBy_Aggregate(bool isAsync)
        {
            await base.OrderBy_Take_GroupBy_Aggregate(isAsync);

            AssertSql(
                """
SELECT MIN(`o0`.`OrderID`)
FROM (
    SELECT TOP @p `o`.`OrderID`, `o`.`CustomerID`
    FROM `Orders` AS `o`
    ORDER BY `o`.`OrderID`
) AS `o0`
GROUP BY `o0`.`CustomerID`
""");
        }

        public override async Task OrderBy_Skip_Take_GroupBy_Aggregate(bool isAsync)
        {
            await base.OrderBy_Skip_Take_GroupBy_Aggregate(isAsync);

            AssertSql(
                """
SELECT MAX(`o0`.`OrderID`)
FROM (
    SELECT `o2`.`OrderID`, `o2`.`CustomerID`
    FROM (
        SELECT TOP @p0 `o1`.`OrderID`, `o1`.`CustomerID`
        FROM (
            SELECT TOP @p + @p0 `o`.`OrderID`, `o`.`CustomerID`
            FROM `Orders` AS `o`
            ORDER BY `o`.`OrderID`
        ) AS `o1`
        ORDER BY `o1`.`OrderID` DESC
    ) AS `o2`
    ORDER BY `o2`.`OrderID`
) AS `o0`
GROUP BY `o0`.`CustomerID`
""");
        }

        public override async Task Distinct_GroupBy_Aggregate(bool isAsync)
        {
            await base.Distinct_GroupBy_Aggregate(isAsync);

            AssertSql(
                """
SELECT `o0`.`CustomerID` AS `Key`, COUNT(*) AS `c`
FROM (
    SELECT DISTINCT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
    FROM `Orders` AS `o`
) AS `o0`
GROUP BY `o0`.`CustomerID`
""");
        }

        public override async Task Anonymous_projection_Distinct_GroupBy_Aggregate(bool isAsync)
        {
            await base.Anonymous_projection_Distinct_GroupBy_Aggregate(isAsync);

            AssertSql(
                """
SELECT `o0`.`EmployeeID` AS `Key`, COUNT(*) AS `c`
FROM (
    SELECT DISTINCT `o`.`OrderID`, `o`.`EmployeeID`
    FROM `Orders` AS `o`
) AS `o0`
GROUP BY `o0`.`EmployeeID`
""");
        }

        public override async Task SelectMany_GroupBy_Aggregate(bool isAsync)
        {
            await base.SelectMany_GroupBy_Aggregate(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`EmployeeID` AS `Key`, COUNT(*) AS `c`
                    FROM `Customers` AS `c`
                    INNER JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
                    GROUP BY `o`.`EmployeeID`
                    """);
        }

        public override async Task Join_GroupBy_Aggregate(bool isAsync)
        {
            await base.Join_GroupBy_Aggregate(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID` AS `Key`, AVG(CDBL(`o`.`OrderID`)) AS `Count`
                    FROM `Orders` AS `o`
                    INNER JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
                    GROUP BY `c`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_required_navigation_member_Aggregate(bool isAsync)
        {
            await base.GroupBy_required_navigation_member_Aggregate(isAsync);

            AssertSql(
                $"""
                    SELECT `o0`.`CustomerID` AS `CustomerId`, COUNT(*) AS `Count`
                    FROM `Order Details` AS `o`
                    INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`
                    GROUP BY `o0`.`CustomerID`
                    """);
        }

        public override async Task Join_complex_GroupBy_Aggregate(bool isAsync)
        {
            await base.Join_complex_GroupBy_Aggregate(isAsync);

            AssertSql(
                """
SELECT `c0`.`CustomerID` AS `Key`, AVG(CDBL(`o0`.`OrderID`)) AS `Count`
FROM (
    SELECT TOP @p `o`.`OrderID`, `o`.`CustomerID`
    FROM `Orders` AS `o`
    WHERE `o`.`OrderID` < 10400
    ORDER BY `o`.`OrderDate`
) AS `o0`
INNER JOIN (
    SELECT `c2`.`CustomerID`
    FROM (
        SELECT TOP @p1 `c1`.`CustomerID`, `c1`.`City`
        FROM (
            SELECT TOP @p0 + @p1 `c`.`CustomerID`, `c`.`City`
            FROM `Customers` AS `c`
            WHERE `c`.`CustomerID` NOT IN ('DRACD', 'FOLKO')
            ORDER BY `c`.`City`
        ) AS `c1`
        ORDER BY `c1`.`City` DESC
    ) AS `c2`
    ORDER BY `c2`.`City`
) AS `c0` ON `o0`.`CustomerID` = `c0`.`CustomerID`
GROUP BY `c0`.`CustomerID`
""");
        }

        public override async Task GroupJoin_GroupBy_Aggregate(bool isAsync)
        {
            await base.GroupJoin_GroupBy_Aggregate(isAsync);

            AssertSql(
                """
    SELECT `o`.`CustomerID` AS `Key`, AVG(IIF(`o`.`OrderID` IS NULL, NULL, CDBL(`o`.`OrderID`))) AS `Average`
    FROM `Customers` AS `c`
    LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
    WHERE `o`.`OrderID` IS NOT NULL
    GROUP BY `o`.`CustomerID`
    """);
        }

        public override async Task GroupJoin_GroupBy_Aggregate_2(bool isAsync)
        {
            await base.GroupJoin_GroupBy_Aggregate_2(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID` AS `Key`, MAX(`c`.`City`) AS `Max`
                    FROM `Customers` AS `c`
                    LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
                    GROUP BY `c`.`CustomerID`
                    """);
        }

        public override async Task GroupJoin_GroupBy_Aggregate_3(bool isAsync)
        {
            await base.GroupJoin_GroupBy_Aggregate_3(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`CustomerID` AS `Key`, AVG(CDBL(`o`.`OrderID`)) AS `Average`
                    FROM `Orders` AS `o`
                    LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupJoin_GroupBy_Aggregate_4(bool isAsync)
        {
            await base.GroupJoin_GroupBy_Aggregate_4(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID` AS `Value`, MAX(`c`.`City`) AS `Max`
                    FROM `Customers` AS `c`
                    LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
                    GROUP BY `c`.`CustomerID`
                    """);
        }

        public override async Task GroupJoin_GroupBy_Aggregate_5(bool isAsync)
        {
            await base.GroupJoin_GroupBy_Aggregate_5(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`OrderID` AS `Value`, AVG(CDBL(`o`.`OrderID`)) AS `Average`
                    FROM `Orders` AS `o`
                    LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
                    GROUP BY `o`.`OrderID`
                    """);
        }

        public override async Task GroupBy_optional_navigation_member_Aggregate(bool isAsync)
        {
            await base.GroupBy_optional_navigation_member_Aggregate(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`Country`, COUNT(*) AS `Count`
                    FROM `Orders` AS `o`
                    LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
                    GROUP BY `c`.`Country`
                    """);
        }

        public override async Task GroupJoin_complex_GroupBy_Aggregate(bool isAsync)
        {
            await base.GroupJoin_complex_GroupBy_Aggregate(isAsync);

            AssertSql(
                """
SELECT `o0`.`CustomerID` AS `Key`, AVG(CDBL(`o0`.`OrderID`)) AS `Count`
FROM (
    SELECT `c2`.`CustomerID`
    FROM (
        SELECT TOP @p0 `c1`.`CustomerID`, `c1`.`City`
        FROM (
            SELECT TOP @p + @p0 `c`.`CustomerID`, `c`.`City`
            FROM `Customers` AS `c`
            WHERE `c`.`CustomerID` NOT IN ('DRACD', 'FOLKO')
            ORDER BY `c`.`City`
        ) AS `c1`
        ORDER BY `c1`.`City` DESC
    ) AS `c2`
    ORDER BY `c2`.`City`
) AS `c0`
INNER JOIN (
    SELECT TOP @p1 `o`.`OrderID`, `o`.`CustomerID`
    FROM `Orders` AS `o`
    WHERE `o`.`OrderID` < 10400
    ORDER BY `o`.`OrderDate`
) AS `o0` ON `c0`.`CustomerID` = `o0`.`CustomerID`
WHERE `o0`.`OrderID` > 10300
GROUP BY `o0`.`CustomerID`
""");
        }

        public override async Task Self_join_GroupBy_Aggregate(bool isAsync)
        {
            await base.Self_join_GroupBy_Aggregate(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`CustomerID` AS `Key`, AVG(CDBL(`o0`.`OrderID`)) AS `Count`
                    FROM `Orders` AS `o`
                    INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`
                    WHERE `o`.`OrderID` < 10400
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_multi_navigation_members_Aggregate(bool isAsync)
        {
            await base.GroupBy_multi_navigation_members_Aggregate(isAsync);

            AssertSql(
                $"""
                    SELECT `o0`.`CustomerID`, `p`.`ProductName`, COUNT(*) AS `Count`
                    FROM (`Order Details` AS `o`
                    INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`)
                    INNER JOIN `Products` AS `p` ON `o`.`ProductID` = `p`.`ProductID`
                    GROUP BY `o0`.`CustomerID`, `p`.`ProductName`
                    """);
        }

        public override async Task Union_simple_groupby(bool isAsync)
        {
            await base.Union_simple_groupby(isAsync);

            AssertSql(
                """
SELECT `u`.`City` AS `Key`, COUNT(*) AS `Total`
FROM (
    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    WHERE `c`.`ContactTitle` = 'Owner'
    UNION
    SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
    FROM `Customers` AS `c0`
    WHERE `c0`.`City` = 'México D.F.'
) AS `u`
GROUP BY `u`.`City`
""");
        }

        public override async Task Select_anonymous_GroupBy_Aggregate(bool isAsync)
        {
            await base.Select_anonymous_GroupBy_Aggregate(isAsync);

            AssertSql(
                $"""
                    SELECT MIN(`o`.`OrderDate`) AS `Min`, MAX(`o`.`OrderDate`) AS `Max`, IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `Sum`, AVG(CDBL(`o`.`OrderID`)) AS `Avg`
                    FROM `Orders` AS `o`
                    WHERE `o`.`OrderID` < 10300
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_principal_key_property_optimization(bool isAsync)
        {
            await base.GroupBy_principal_key_property_optimization(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID` AS `Key`, COUNT(*) AS `Count`
                    FROM `Orders` AS `o`
                    LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
                    GROUP BY `c`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_after_anonymous_projection_and_distinct_followed_by_another_anonymous_projection(bool async)
        {
            await base.GroupBy_after_anonymous_projection_and_distinct_followed_by_another_anonymous_projection(async);

            AssertSql(
                """
SELECT `o0`.`CustomerID` AS `Key`, COUNT(*) AS `Count`
FROM (
    SELECT DISTINCT `o`.`CustomerID`, `o`.`OrderID`
    FROM `Orders` AS `o`
) AS `o0`
GROUP BY `o0`.`CustomerID`
""");
        }

        public override async Task GroupBy_complex_key_aggregate(bool async)
        {
            await base.GroupBy_complex_key_aggregate(async);

            AssertSql(
                """
SELECT `s`.`Key`, COUNT(*) AS `Count`
FROM (
    SELECT MID(`c`.`CustomerID`, 0 + 1, 1) AS `Key`
    FROM `Orders` AS `o`
    LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
) AS `s`
GROUP BY `s`.`Key`
""");
        }

        public override async Task GroupBy_complex_key_aggregate_2(bool async)
        {
            await base.GroupBy_complex_key_aggregate_2(async);

            AssertSql(
                """
SELECT [t].[Key] AS [Month], COALESCE(SUM([t].[OrderID]), 0) AS [Total], (
    SELECT COALESCE(SUM([o0].[OrderID]), 0)
    FROM [Orders] AS [o0]
    WHERE DATEPART(month, [o0].[OrderDate]) = [t].[Key] OR ([o0].[OrderDate] IS NULL AND [t].[Key] IS NULL)) AS [Payment]
FROM (
    SELECT [o].[OrderID], DATEPART(month, [o].[OrderDate]) AS [Key]
    FROM [Orders] AS [o]
) AS [t]
GROUP BY [t].[Key]
""");
        }

        public override async Task Select_collection_of_scalar_before_GroupBy_aggregate(bool async)
        {
            await base.Select_collection_of_scalar_before_GroupBy_aggregate(async);

            AssertSql(
                """
SELECT `c`.`City` AS `Key`, COUNT(*) AS `Count`
FROM `Customers` AS `c`
GROUP BY `c`.`City`
""");
        }

        public override async Task GroupBy_OrderBy_key(bool isAsync)
        {
            await base.GroupBy_OrderBy_key(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`CustomerID` AS `Key`, COUNT(*) AS `c`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    ORDER BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_OrderBy_count(bool isAsync)
        {
            await base.GroupBy_OrderBy_count(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`CustomerID` AS `Key`, COUNT(*) AS `Count`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    ORDER BY COUNT(*), `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_OrderBy_count_Select_sum(bool isAsync)
        {
            await base.GroupBy_OrderBy_count_Select_sum(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`CustomerID` AS `Key`, IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `Sum`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    ORDER BY COUNT(*), `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_aggregate_Contains(bool isAsync)
        {
            await base.GroupBy_aggregate_Contains(isAsync);

            AssertSql(
"""
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Orders` AS `o`
WHERE EXISTS (
    SELECT 1
    FROM `Orders` AS `o0`
    GROUP BY `o0`.`CustomerID`
    HAVING COUNT(*) > 30 AND (`o0`.`CustomerID` = `o`.`CustomerID` OR (`o0`.`CustomerID` IS NULL AND `o`.`CustomerID` IS NULL)))
""");
        }

        public override async Task GroupBy_aggregate_Pushdown(bool isAsync)
        {
            await base.GroupBy_aggregate_Pushdown(isAsync);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@__p_0='20'")}
                    
                    {AssertSqlHelper.Declaration("@__p_1='4'")}
                    
                    SELECT `t`.`CustomerID`
                    FROM (
                        SELECT TOP {AssertSqlHelper.Parameter("@__p_0")} `o`.`CustomerID`
                        FROM `Orders` AS `o`
                        GROUP BY `o`.`CustomerID`
                        HAVING COUNT(*) > 10
                        ORDER BY `o`.`CustomerID`
                    ) AS `t`
                    ORDER BY `t`.`CustomerID`
                    SKIP {AssertSqlHelper.Parameter("@__p_1")}
                    """);
        }

        public override async Task GroupBy_aggregate_using_grouping_key_Pushdown(bool async)
        {
            await base.GroupBy_aggregate_using_grouping_key_Pushdown(async);

            AssertSql(
                """
@__p_0='20'
@__p_1='4'

SELECT [t].[Key], [t].[Max]
FROM (
    SELECT TOP(@__p_0) [o].[CustomerID] AS [Key], MAX([o].[CustomerID]) AS [Max]
    FROM [Orders] AS [o]
    GROUP BY [o].[CustomerID]
    HAVING COUNT(*) > 10
    ORDER BY [o].[CustomerID]
) AS [t]
ORDER BY [t].[Key]
OFFSET @__p_1 ROWS
""");
        }

        public override async Task GroupBy_aggregate_Pushdown_followed_by_projecting_Length(bool isAsync)
        {
            await base.GroupBy_aggregate_Pushdown_followed_by_projecting_Length(isAsync);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@__p_0='20'")}
                    
                    {AssertSqlHelper.Declaration("@__p_1='4'")}
                    
                    SELECT CAST(LEN(`t`.`CustomerID`) AS int)
                    FROM (
                        SELECT TOP {AssertSqlHelper.Parameter("@__p_0")} `o`.`CustomerID`
                        FROM `Orders` AS `o`
                        GROUP BY `o`.`CustomerID`
                        HAVING COUNT(*) > 10
                        ORDER BY `o`.`CustomerID`
                    ) AS `t`
                    ORDER BY `t`.`CustomerID`
                    SKIP {AssertSqlHelper.Parameter("@__p_1")}
                    """);
        }

        public override async Task GroupBy_aggregate_Pushdown_followed_by_projecting_constant(bool isAsync)
        {
            await base.GroupBy_aggregate_Pushdown_followed_by_projecting_constant(isAsync);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@__p_0='20'")}
                    
                    {AssertSqlHelper.Declaration("@__p_1='4'")}
                    
                    SELECT 5
                    FROM (
                        SELECT TOP {AssertSqlHelper.Parameter("@__p_0")} `o`.`CustomerID`
                        FROM `Orders` AS `o`
                        GROUP BY `o`.`CustomerID`
                        HAVING COUNT(*) > 10
                        ORDER BY `o`.`CustomerID`
                    ) AS `t`
                    ORDER BY `t`.`CustomerID`
                    SKIP {AssertSqlHelper.Parameter("@__p_1")}
                    """);
        }

        public override async Task GroupBy_filter_key(bool isAsync)
        {
            await base.GroupBy_filter_key(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`CustomerID` AS `Key`, COUNT(*) AS `c`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    HAVING `o`.`CustomerID` = 'ALFKI'
                    """);
        }

        public override async Task GroupBy_filter_count(bool isAsync)
        {
            await base.GroupBy_filter_count(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`CustomerID` AS `Key`, COUNT(*) AS `Count`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    HAVING COUNT(*) > 4
                    """);
        }

        public override async Task GroupBy_count_filter(bool async)
        {
            await base.GroupBy_count_filter(async);

            AssertSql(
                """
SELECT `o0`.`Key` AS `Name`, COUNT(*) AS `Count`
FROM (
    SELECT 'Order' AS `Key`
    FROM `Orders` AS `o`
) AS `o0`
GROUP BY `o0`.`Key`
HAVING COUNT(*) > 0
""");
        }

        public override async Task GroupBy_filter_count_OrderBy_count_Select_sum(bool isAsync)
        {
            await base.GroupBy_filter_count_OrderBy_count_Select_sum(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`CustomerID` AS `Key`, COUNT(*) AS `Count`, IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `Sum`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    HAVING COUNT(*) > 4
                    ORDER BY COUNT(*), `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Aggregate_Join(bool isAsync)
        {
            await base.GroupBy_Aggregate_Join(isAsync);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o1`.`OrderID`, `o1`.`CustomerID`, `o1`.`EmployeeID`, `o1`.`OrderDate`
FROM ((
    SELECT `o`.`CustomerID`, MAX(`o`.`OrderID`) AS `LastOrderID`
    FROM `Orders` AS `o`
    GROUP BY `o`.`CustomerID`
    HAVING COUNT(*) > 5
) AS `o0`
INNER JOIN `Customers` AS `c` ON `o0`.`CustomerID` = `c`.`CustomerID`)
INNER JOIN `Orders` AS `o1` ON `o0`.`LastOrderID` = `o1`.`OrderID`
""");
        }

        public override async Task GroupBy_Aggregate_Join_converted_from_SelectMany(bool async)
        {
            await base.GroupBy_Aggregate_Join_converted_from_SelectMany(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
INNER JOIN (
    SELECT `o`.`CustomerID`
    FROM `Orders` AS `o`
    GROUP BY `o`.`CustomerID`
    HAVING COUNT(*) > 5
) AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
""");
        }

        public override async Task GroupBy_Aggregate_LeftJoin_converted_from_SelectMany(bool async)
        {
            await base.GroupBy_Aggregate_LeftJoin_converted_from_SelectMany(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
LEFT JOIN (
    SELECT `o`.`CustomerID`
    FROM `Orders` AS `o`
    GROUP BY `o`.`CustomerID`
    HAVING COUNT(*) > 5
) AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
""");
        }

        public override async Task Join_GroupBy_Aggregate_multijoins(bool isAsync)
        {
            await base.Join_GroupBy_Aggregate_multijoins(isAsync);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o1`.`OrderID`, `o1`.`CustomerID`, `o1`.`EmployeeID`, `o1`.`OrderDate`
FROM (`Customers` AS `c`
INNER JOIN (
    SELECT `o`.`CustomerID`, MAX(`o`.`OrderID`) AS `LastOrderID`
    FROM `Orders` AS `o`
    GROUP BY `o`.`CustomerID`
    HAVING COUNT(*) > 5
) AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`)
LEFT JOIN `Orders` AS `o1` ON `o0`.`LastOrderID` = `o1`.`OrderID`
WHERE `o0`.`LastOrderID` IS NOT NULL AND `o1`.`OrderID` IS NOT NULL
""");
        }

        public override async Task Join_GroupBy_Aggregate_single_join(bool isAsync)
        {
            await base.Join_GroupBy_Aggregate_single_join(isAsync);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o0`.`LastOrderID`
FROM `Customers` AS `c`
INNER JOIN (
    SELECT `o`.`CustomerID`, MAX(`o`.`OrderID`) AS `LastOrderID`
    FROM `Orders` AS `o`
    GROUP BY `o`.`CustomerID`
    HAVING COUNT(*) > 5
) AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
""");
        }

        public override async Task Join_GroupBy_Aggregate_with_another_join(bool isAsync)
        {
            await base.Join_GroupBy_Aggregate_with_another_join(isAsync);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o0`.`LastOrderID`, `o1`.`OrderID`
FROM (`Customers` AS `c`
INNER JOIN (
    SELECT `o`.`CustomerID`, MAX(`o`.`OrderID`) AS `LastOrderID`
    FROM `Orders` AS `o`
    GROUP BY `o`.`CustomerID`
    HAVING COUNT(*) > 5
) AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`)
INNER JOIN `Orders` AS `o1` ON `c`.`CustomerID` = `o1`.`CustomerID`
""");
        }

        public override async Task Join_GroupBy_Aggregate_distinct_single_join(bool async)
        {
            await base.Join_GroupBy_Aggregate_distinct_single_join(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o1`.`LastOrderID`
FROM `Customers` AS `c`
INNER JOIN (
    SELECT DISTINCT `o0`.`CustomerID`, MAX(`o0`.`OrderID`) AS `LastOrderID`
    FROM (
        SELECT `o`.`OrderID`, `o`.`CustomerID`, DATEPART('yyyy', `o`.`OrderDate`) AS `Year`
        FROM `Orders` AS `o`
    ) AS `o0`
    GROUP BY `o0`.`CustomerID`, `o0`.`Year`
    HAVING COUNT(*) > 5
) AS `o1` ON `c`.`CustomerID` = `o1`.`CustomerID`
""");
        }

        public override async Task Join_GroupBy_Aggregate_with_left_join(bool async)
        {
            await base.Join_GroupBy_Aggregate_with_left_join(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o0`.`LastOrderID`
FROM `Customers` AS `c`
LEFT JOIN (
    SELECT `o`.`CustomerID`, MAX(`o`.`OrderID`) AS `LastOrderID`
    FROM `Orders` AS `o`
    GROUP BY `o`.`CustomerID`
    HAVING COUNT(*) > 5
) AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE `c`.`CustomerID` LIKE 'A%'
""");
        }

        public override async Task Join_GroupBy_Aggregate_in_subquery(bool isAsync)
        {
            await base.Join_GroupBy_Aggregate_in_subquery(isAsync);

            AssertSql(
                """
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `s`.`CustomerID`, `s`.`Address`, `s`.`City`, `s`.`CompanyName`, `s`.`ContactName`, `s`.`ContactTitle`, `s`.`Country`, `s`.`Fax`, `s`.`Phone`, `s`.`PostalCode`, `s`.`Region`
FROM `Orders` AS `o`
INNER JOIN (
    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    INNER JOIN (
        SELECT `o0`.`CustomerID`
        FROM `Orders` AS `o0`
        GROUP BY `o0`.`CustomerID`
        HAVING COUNT(*) > 5
    ) AS `o1` ON `c`.`CustomerID` = `o1`.`CustomerID`
) AS `s` ON `o`.`CustomerID` = `s`.`CustomerID`
WHERE `o`.`OrderID` < 10400
""");
        }

        public override async Task Join_GroupBy_Aggregate_on_key(bool isAsync)
        {
            await base.Join_GroupBy_Aggregate_on_key(isAsync);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o0`.`LastOrderID`
FROM `Customers` AS `c`
INNER JOIN (
    SELECT `o`.`CustomerID` AS `Key`, MAX(`o`.`OrderID`) AS `LastOrderID`
    FROM `Orders` AS `o`
    GROUP BY `o`.`CustomerID`
    HAVING COUNT(*) > 5
) AS `o0` ON `c`.`CustomerID` = `o0`.`Key`
""");
        }

        public override async Task GroupBy_with_result_selector(bool isAsync)
        {
            await base.GroupBy_with_result_selector(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `Sum`, MIN(`o`.`OrderID`) AS `Min`, MAX(`o`.`OrderID`) AS `Max`, AVG(CDBL(`o`.`OrderID`)) AS `Avg`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Sum_constant(bool isAsync)
        {
            await base.GroupBy_Sum_constant(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(1) IS NULL, 0, SUM(1))
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Sum_constant_cast(bool isAsync)
        {
            await base.GroupBy_Sum_constant_cast(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(SUM(1) IS NULL, 0, SUM(1))
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task Distinct_GroupBy_OrderBy_key(bool isAsync)
        {
            await base.Distinct_GroupBy_OrderBy_key(isAsync);

            AssertSql(
                """
SELECT `o0`.`CustomerID` AS `Key`, COUNT(*) AS `c`
FROM (
    SELECT DISTINCT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
    FROM `Orders` AS `o`
) AS `o0`
GROUP BY `o0`.`CustomerID`
ORDER BY `o0`.`CustomerID`
""");
        }

        public override async Task Select_nested_collection_with_groupby(bool isAsync)
        {
            await base.Select_nested_collection_with_groupby(isAsync);

            AssertSql(
                $"""
                    SELECT (
                        SELECT CASE
                            WHEN EXISTS (
                                SELECT 1
                                FROM `Orders` AS `o0`
                                WHERE `c`.`CustomerID` = `o0`.`CustomerID`)
                            THEN True ELSE False
                        END
                    ), `c`.`CustomerID`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` LIKE 'A' & '%'
                    """,
                //
                $"""
                    {AssertSqlHelper.Declaration("@_outer_CustomerID='ALFKI' (Size = 5)")}
                    
                    SELECT `o1`.`OrderID`
                    FROM `Orders` AS `o1`
                    WHERE {AssertSqlHelper.Parameter("@_outer_CustomerID")} = `o1`.`CustomerID`
                    ORDER BY `o1`.`OrderID`
                    """,
                //
                $"""
                    {AssertSqlHelper.Declaration("@_outer_CustomerID='ANATR' (Size = 5)")}
                    
                    SELECT `o1`.`OrderID`
                    FROM `Orders` AS `o1`
                    WHERE {AssertSqlHelper.Parameter("@_outer_CustomerID")} = `o1`.`CustomerID`
                    ORDER BY `o1`.`OrderID`
                    """,
                //
                $"""
                    {AssertSqlHelper.Declaration("@_outer_CustomerID='ANTON' (Size = 5)")}
                    
                    SELECT `o1`.`OrderID`
                    FROM `Orders` AS `o1`
                    WHERE {AssertSqlHelper.Parameter("@_outer_CustomerID")} = `o1`.`CustomerID`
                    ORDER BY `o1`.`OrderID`
                    """,
                //
                $"""
                    {AssertSqlHelper.Declaration("@_outer_CustomerID='AROUT' (Size = 5)")}
                    
                    SELECT `o1`.`OrderID`
                    FROM `Orders` AS `o1`
                    WHERE {AssertSqlHelper.Parameter("@_outer_CustomerID")} = `o1`.`CustomerID`
                    ORDER BY `o1`.`OrderID`
                    """);
        }

        public override async Task Select_uncorrelated_collection_with_groupby_works(bool async)
        {
            await base.Select_uncorrelated_collection_with_groupby_works(async);

            AssertSql(
                """
SELECT [c].[CustomerID], [t].[OrderID]
FROM [Customers] AS [c]
OUTER APPLY (
    SELECT [o].[OrderID]
    FROM [Orders] AS [o]
    GROUP BY [o].[OrderID]
) AS [t]
WHERE [c].[CustomerID] LIKE N'A%'
ORDER BY [c].[CustomerID]
""");
        }

        public override async Task Select_uncorrelated_collection_with_groupby_multiple_collections_work(bool async)
        {
            await base.Select_uncorrelated_collection_with_groupby_multiple_collections_work(async);

            AssertSql(
                """
SELECT [o].[OrderID], [t].[ProductID], [t0].[c], [t0].[ProductID]
FROM [Orders] AS [o]
OUTER APPLY (
    SELECT [p].[ProductID]
    FROM [Products] AS [p]
    GROUP BY [p].[ProductID]
) AS [t]
OUTER APPLY (
    SELECT COUNT(*) AS [c], [p0].[ProductID]
    FROM [Products] AS [p0]
    GROUP BY [p0].[ProductID]
) AS [t0]
WHERE [o].[CustomerID] LIKE N'A%'
ORDER BY [o].[OrderID], [t].[ProductID]
""");
        }

        public override async Task Select_GroupBy_All(bool isAsync)
        {
            await base.Select_GroupBy_All(isAsync);

            AssertSql(
"""
SELECT IIF(NOT EXISTS (
        SELECT 1
        FROM `Orders` AS `o`
        GROUP BY `o`.`CustomerID`
        HAVING `o`.`CustomerID` <> 'ALFKI' OR `o`.`CustomerID` IS NULL), TRUE, FALSE)
FROM (SELECT COUNT(*) FROM `#Dual`)
""");
        }

        public override async Task GroupBy_Where_Average(bool async)
        {
            await base.GroupBy_Where_Average(async);

            AssertSql(
                """
SELECT AVG(IIF(`o`.`OrderID` < 10300, CDBL(`o`.`OrderID`), NULL))
FROM `Orders` AS `o`
GROUP BY `o`.`CustomerID`
""");
        }

        public override async Task GroupBy_Where_Count(bool async)
        {
            await base.GroupBy_Where_Count(async);

            AssertSql(
                """
SELECT COUNT(IIF(`o`.`OrderID` < 10300, 1, NULL))
FROM `Orders` AS `o`
GROUP BY `o`.`CustomerID`
""");
        }

        public override async Task GroupBy_Where_LongCount(bool async)
        {
            await base.GroupBy_Where_LongCount(async);

            AssertSql(
                """
SELECT COUNT(IIF(`o`.`OrderID` < 10300, 1, NULL))
FROM `Orders` AS `o`
GROUP BY `o`.`CustomerID`
""");
        }

        public override async Task GroupBy_Where_Max(bool async)
        {
            await base.GroupBy_Where_Max(async);

            AssertSql(
                """
SELECT MAX(IIF(`o`.`OrderID` < 10300, `o`.`OrderID`, NULL))
FROM `Orders` AS `o`
GROUP BY `o`.`CustomerID`
""");
        }

        public override async Task GroupBy_Where_Min(bool async)
        {
            await base.GroupBy_Where_Min(async);

            AssertSql(
                """
SELECT MIN(IIF(`o`.`OrderID` < 10300, `o`.`OrderID`, NULL))
FROM `Orders` AS `o`
GROUP BY `o`.`CustomerID`
""");
        }

        public override async Task GroupBy_Where_Sum(bool async)
        {
            await base.GroupBy_Where_Sum(async);

            AssertSql(
                """
SELECT IIF(SUM(IIF(`o`.`OrderID` < 10300, `o`.`OrderID`, NULL)) IS NULL, 0, SUM(IIF(`o`.`OrderID` < 10300, `o`.`OrderID`, NULL)))
FROM `Orders` AS `o`
GROUP BY `o`.`CustomerID`
""");
        }

        public override async Task GroupBy_Where_Count_with_predicate(bool async)
        {
            await base.GroupBy_Where_Count_with_predicate(async);

            AssertSql(
                """
SELECT COUNT(IIF(`o`.`OrderID` < 10300 AND `o`.`OrderDate` IS NOT NULL AND DATEPART('yyyy', `o`.`OrderDate`) = 1997, 1, NULL))
FROM `Orders` AS `o`
GROUP BY `o`.`CustomerID`
""");
        }

        public override async Task GroupBy_Where_Where_Count(bool async)
        {
            await base.GroupBy_Where_Where_Count(async);

            AssertSql(
                """
SELECT COUNT(IIF(`o`.`OrderID` < 10300 AND `o`.`OrderDate` IS NOT NULL AND DATEPART('yyyy', `o`.`OrderDate`) = 1997, 1, NULL))
FROM `Orders` AS `o`
GROUP BY `o`.`CustomerID`
""");
        }

        public override async Task GroupBy_Where_Select_Where_Count(bool async)
        {
            await base.GroupBy_Where_Select_Where_Count(async);

            AssertSql(
                """
SELECT COUNT(IIF(`o`.`OrderID` < 10300 AND `o`.`OrderDate` IS NOT NULL AND DATEPART('yyyy', `o`.`OrderDate`) = 1997, 1, NULL))
FROM `Orders` AS `o`
GROUP BY `o`.`CustomerID`
""");
        }

        public override async Task GroupBy_Where_Select_Where_Select_Min(bool async)
        {
            await base.GroupBy_Where_Select_Where_Select_Min(async);

            AssertSql(
                """
SELECT MIN(IIF(`o`.`OrderID` < 10300 AND `o`.`OrderDate` IS NOT NULL AND DATEPART('yyyy', `o`.`OrderDate`) = 1997, `o`.`OrderID`, NULL))
FROM `Orders` AS `o`
GROUP BY `o`.`CustomerID`
""");
        }

        public override async Task GroupBy_multiple_Count_with_predicate(bool async)
        {
            await base.GroupBy_multiple_Count_with_predicate(async);

            AssertSql(
                """
SELECT `o`.`CustomerID`, COUNT(*) AS `All`, COUNT(IIF(`o`.`OrderID` < 11000, 1, NULL)) AS `TenK`, COUNT(IIF(`o`.`OrderID` < 12000, 1, NULL)) AS `EleventK`
FROM `Orders` AS `o`
GROUP BY `o`.`CustomerID`
""");
        }

        public override async Task GroupBy_multiple_Sum_with_conditional_projection(bool async)
        {
            await base.GroupBy_multiple_Sum_with_conditional_projection(async);

            AssertSql(
                """
SELECT `o`.`CustomerID`, IIF(SUM(IIF(`o`.`OrderID` < 11000, `o`.`OrderID`, 0)) IS NULL, 0, SUM(IIF(`o`.`OrderID` < 11000, `o`.`OrderID`, 0))) AS `TenK`, IIF(SUM(IIF(`o`.`OrderID` >= 11000, `o`.`OrderID`, 0)) IS NULL, 0, SUM(IIF(`o`.`OrderID` >= 11000, `o`.`OrderID`, 0))) AS `EleventK`
FROM `Orders` AS `o`
GROUP BY `o`.`CustomerID`
""");
        }

        public override async Task GroupBy_multiple_Sum_with_Select_conditional_projection(bool async)
        {
            await base.GroupBy_multiple_Sum_with_Select_conditional_projection(async);

            AssertSql(
                """
SELECT `o`.`CustomerID`, IIF(SUM(IIF(`o`.`OrderID` < 11000, `o`.`OrderID`, 0)) IS NULL, 0, SUM(IIF(`o`.`OrderID` < 11000, `o`.`OrderID`, 0))) AS `TenK`, IIF(SUM(IIF(`o`.`OrderID` >= 11000, `o`.`OrderID`, 0)) IS NULL, 0, SUM(IIF(`o`.`OrderID` >= 11000, `o`.`OrderID`, 0))) AS `EleventK`
FROM `Orders` AS `o`
GROUP BY `o`.`CustomerID`
""");
        }

        public override async Task GroupBy_Key_as_part_of_element_selector(bool isAsync)
        {
            await base.GroupBy_Key_as_part_of_element_selector(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`OrderID` AS `Key`, AVG(CDBL(`o`.`OrderID`)) AS `Avg`, MAX(`o`.`OrderDate`) AS `Max`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`OrderID`
                    """);
        }

        public override async Task GroupBy_composite_Key_as_part_of_element_selector(bool isAsync)
        {
            await base.GroupBy_composite_Key_as_part_of_element_selector(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`OrderID`, `o`.`CustomerID`, AVG(CDBL(`o`.`OrderID`)) AS `Avg`, MAX(`o`.`OrderDate`) AS `Max`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`OrderID`, `o`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_with_aggregate_through_navigation_property(bool isAsync)
        {
            await base.GroupBy_with_aggregate_through_navigation_property(isAsync);

            AssertSql(
"""
SELECT (
    SELECT MAX(`c`.`Region`)
    FROM `Orders` AS `o0`
    LEFT JOIN `Customers` AS `c` ON `o0`.`CustomerID` = `c`.`CustomerID`
    WHERE `o`.`EmployeeID` = `o0`.`EmployeeID` OR (`o`.`EmployeeID` IS NULL AND `o0`.`EmployeeID` IS NULL)) AS `max`
FROM `Orders` AS `o`
GROUP BY `o`.`EmployeeID`
""");
        }

        public override async Task GroupBy_with_aggregate_containing_complex_where(bool async)
        {
            await base.GroupBy_with_aggregate_containing_complex_where(async);

            AssertSql(
                """
SELECT [o].[EmployeeID] AS [Key], (
    SELECT MAX([o0].[OrderID])
    FROM [Orders] AS [o0]
    WHERE CAST([o0].[EmployeeID] AS bigint) = CAST(MAX([o].[OrderID]) * 6 AS bigint) OR ([o0].[EmployeeID] IS NULL AND MAX([o].[OrderID]) IS NULL)) AS [Max]
FROM [Orders] AS [o]
GROUP BY [o].[EmployeeID]
""");
        }

        public override async Task GroupBy_Shadow(bool isAsync)
        {
            await base.GroupBy_Shadow(isAsync);

            AssertSql(
"""
SELECT (
    SELECT TOP 1 `e0`.`Title`
    FROM `Employees` AS `e0`
    WHERE `e0`.`Title` = 'Sales Representative' AND `e0`.`EmployeeID` = 1 AND (`e`.`Title` = `e0`.`Title` OR (`e`.`Title` IS NULL AND `e0`.`Title` IS NULL)))
FROM `Employees` AS `e`
WHERE `e`.`Title` = 'Sales Representative' AND `e`.`EmployeeID` = 1
GROUP BY `e`.`Title`
""");
        }

        public override async Task GroupBy_Shadow2(bool isAsync)
        {
            await base.GroupBy_Shadow2(isAsync);

            AssertSql(
                $"""
                    SELECT `t0`.`EmployeeID`, `t0`.`City`, `t0`.`Country`, `t0`.`FirstName`, `t0`.`ReportsTo`, `t0`.`Title`
                    FROM (
                        SELECT `e`.`Title`
                        FROM `Employees` AS `e`
                        WHERE `e`.`Title` = 'Sales Representative' AND `e`.`EmployeeID` = 1
                        GROUP BY `e`.`Title`
                    ) AS `t`
                    LEFT JOIN (
                        SELECT `t1`.`EmployeeID`, `t1`.`City`, `t1`.`Country`, `t1`.`FirstName`, `t1`.`ReportsTo`, `t1`.`Title`
                        FROM (
                            SELECT `e0`.`EmployeeID`, `e0`.`City`, `e0`.`Country`, `e0`.`FirstName`, `e0`.`ReportsTo`, `e0`.`Title`, ROW_NUMBER() OVER(PARTITION BY `e0`.`Title` ORDER BY `e0`.`EmployeeID`) AS `row`
                            FROM `Employees` AS `e0`
                            WHERE `e0`.`Title` = 'Sales Representative' AND `e0`.`EmployeeID` = 1
                        ) AS `t1`
                        WHERE `t1`.`row` <= 1
                    ) AS `t0` ON `t`.`Title` = `t0`.`Title`
                    """);
        }

        public override async Task GroupBy_Shadow3(bool isAsync)
        {
            await base.GroupBy_Shadow3(isAsync);

            AssertSql(
                $"""
                    SELECT (
                        SELECT TOP 1 `e0`.`Title`
                        FROM `Employees` AS `e0`
                        WHERE `e0`.`EmployeeID` = 1 AND `e`.`EmployeeID` = `e0`.`EmployeeID`)
                    FROM `Employees` AS `e`
                    WHERE `e`.`EmployeeID` = 1
                    GROUP BY `e`.`EmployeeID`
                    """);
        }

        public override async Task GroupBy_select_grouping_list(bool async)
        {
            await base.GroupBy_select_grouping_list(async);

            AssertSql(
                """
SELECT `c1`.`City`, `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
FROM (
    SELECT `c`.`City`
    FROM `Customers` AS `c`
    GROUP BY `c`.`City`
) AS `c1`
LEFT JOIN `Customers` AS `c0` ON `c1`.`City` = `c0`.`City`
ORDER BY `c1`.`City`
""");
        }

        public override async Task GroupBy_select_grouping_array(bool async)
        {
            await base.GroupBy_select_grouping_array(async);

            AssertSql(
                """
SELECT `c1`.`City`, `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
FROM (
    SELECT `c`.`City`
    FROM `Customers` AS `c`
    GROUP BY `c`.`City`
) AS `c1`
LEFT JOIN `Customers` AS `c0` ON `c1`.`City` = `c0`.`City`
ORDER BY `c1`.`City`
""");
        }

        public override async Task GroupBy_select_grouping_composed_list(bool async)
        {
            await base.GroupBy_select_grouping_composed_list(async);

            AssertSql(
                """
SELECT `c1`.`City`, `c2`.`CustomerID`, `c2`.`Address`, `c2`.`City`, `c2`.`CompanyName`, `c2`.`ContactName`, `c2`.`ContactTitle`, `c2`.`Country`, `c2`.`Fax`, `c2`.`Phone`, `c2`.`PostalCode`, `c2`.`Region`
FROM (
    SELECT `c`.`City`
    FROM `Customers` AS `c`
    GROUP BY `c`.`City`
) AS `c1`
LEFT JOIN (
    SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
    FROM `Customers` AS `c0`
    WHERE `c0`.`CustomerID` LIKE 'A%'
) AS `c2` ON `c1`.`City` = `c2`.`City`
ORDER BY `c1`.`City`
""");
        }

        public override async Task GroupBy_select_grouping_composed_list_2(bool async)
        {
            await base.GroupBy_select_grouping_composed_list_2(async);

            AssertSql(
                """
SELECT `c1`.`City`, `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
FROM (
    SELECT `c`.`City`
    FROM `Customers` AS `c`
    GROUP BY `c`.`City`
) AS `c1`
LEFT JOIN `Customers` AS `c0` ON `c1`.`City` = `c0`.`City`
ORDER BY `c1`.`City`, `c0`.`CustomerID`
""");
        }

        public override async Task Select_GroupBy_SelectMany(bool isAsync)
        {
            await base.Select_GroupBy_SelectMany(isAsync);

            AssertSql();
        }

        public override async Task Count_after_GroupBy_aggregate(bool isAsync)
        {
            await base.Count_after_GroupBy_aggregate(isAsync);

            AssertSql(
                """
SELECT COUNT(*)
FROM (
    SELECT 1
    FROM `Orders` AS `o`
    GROUP BY `o`.`CustomerID`
) AS `o0`
""");
        }

        public override async Task LongCount_after_GroupBy_aggregate(bool async)
        {
            await base.LongCount_after_GroupBy_aggregate(async);

            AssertSql(
                """
SELECT COUNT(*)
FROM (
    SELECT 1
    FROM `Orders` AS `o`
    GROUP BY `o`.`CustomerID`
) AS `o0`
""");
        }

        public override async Task GroupBy_Select_Distinct_aggregate(bool async)
        {
            await base.GroupBy_Select_Distinct_aggregate(async);

            AssertSql(
                """
SELECT [o].[CustomerID] AS [Key], AVG(DISTINCT (CAST([o].[OrderID] AS float))) AS [Average], COUNT(DISTINCT ([o].[EmployeeID])) AS [Count], COUNT_BIG(DISTINCT ([o].[EmployeeID])) AS [LongCount], MAX(DISTINCT ([o].[OrderDate])) AS [Max], MIN(DISTINCT ([o].[OrderDate])) AS [Min], COALESCE(SUM(DISTINCT ([o].[OrderID])), 0) AS [Sum]
FROM [Orders] AS [o]
GROUP BY [o].[CustomerID]
""");
        }

        public override async Task GroupBy_group_Distinct_Select_Distinct_aggregate(bool async)
        {
            await base.GroupBy_group_Distinct_Select_Distinct_aggregate(async);

            AssertSql(
                """
SELECT `o`.`CustomerID` AS `Key`, MAX(`o`.`OrderDate`) AS `Max`
FROM `Orders` AS `o`
GROUP BY `o`.`CustomerID`
""");
        }

        public override async Task GroupBy_group_Where_Select_Distinct_aggregate(bool async)
        {
            await base.GroupBy_group_Where_Select_Distinct_aggregate(async);

            AssertSql(
                """
SELECT `o`.`CustomerID` AS `Key`, MAX(IIF(`o`.`OrderDate` IS NOT NULL, `o`.`OrderDate`, NULL)) AS `Max`
FROM `Orders` AS `o`
GROUP BY `o`.`CustomerID`
""");
        }

        public override async Task MinMax_after_GroupBy_aggregate(bool isAsync)
        {
            await base.MinMax_after_GroupBy_aggregate(isAsync);

            AssertSql(
                """
SELECT MIN(`o0`.`c`)
FROM (
    SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `c`
    FROM `Orders` AS `o`
    GROUP BY `o`.`CustomerID`
) AS `o0`
""",
                //
                """
SELECT MAX(`o0`.`c`)
FROM (
    SELECT IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `c`
    FROM `Orders` AS `o`
    GROUP BY `o`.`CustomerID`
) AS `o0`
""");
        }

        public override async Task All_after_GroupBy_aggregate(bool isAsync)
        {
            await base.All_after_GroupBy_aggregate(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(NOT EXISTS (
                            SELECT 1
                            FROM `Orders` AS `o`
                            GROUP BY `o`.`CustomerID`
                            HAVING 0 = 1), TRUE, FALSE)
                    FROM (SELECT COUNT(*) FROM `#Dual`)
                    """);
        }

        public override async Task All_after_GroupBy_aggregate2(bool isAsync)
        {
            await base.All_after_GroupBy_aggregate2(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(NOT EXISTS (
                            SELECT 1
                            FROM `Orders` AS `o`
                            GROUP BY `o`.`CustomerID`
                            HAVING IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) < 0), TRUE, FALSE)
                    FROM (SELECT COUNT(*) FROM `#Dual`)
                    """);
        }

        public override async Task Any_after_GroupBy_aggregate(bool isAsync)
        {
            await base.Any_after_GroupBy_aggregate(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(EXISTS (
                            SELECT 1
                            FROM `Orders` AS `o`
                            GROUP BY `o`.`CustomerID`), TRUE, FALSE)
                    FROM (SELECT COUNT(*) FROM `#Dual`)
                    """);
        }

        public override async Task Count_after_GroupBy_without_aggregate(bool isAsync)
        {
            await base.Count_after_GroupBy_without_aggregate(isAsync);

            AssertSql(
                """
SELECT COUNT(*)
FROM (
    SELECT 1
    FROM `Orders` AS `o`
    GROUP BY `o`.`CustomerID`
) AS `o0`
""");
        }

        public override async Task Count_with_predicate_after_GroupBy_without_aggregate(bool async)
        {
            await base.Count_with_predicate_after_GroupBy_without_aggregate(async);

            AssertSql(
                """
SELECT COUNT(*)
FROM (
    SELECT 1
    FROM `Orders` AS `o`
    GROUP BY `o`.`CustomerID`
    HAVING COUNT(*) > 1
) AS `o0`
""");
        }

        public override async Task LongCount_after_GroupBy_without_aggregate(bool isAsync)
        {
            await base.LongCount_after_GroupBy_without_aggregate(isAsync);

            AssertSql(
                """
SELECT COUNT(*)
FROM (
    SELECT 1
    FROM `Orders` AS `o`
    GROUP BY `o`.`CustomerID`
) AS `o0`
""");
        }

        public override async Task LongCount_with_predicate_after_GroupBy_without_aggregate(bool async)
        {
            await base.LongCount_with_predicate_after_GroupBy_without_aggregate(async);

            AssertSql(
                """
SELECT COUNT(*)
FROM (
    SELECT 1
    FROM `Orders` AS `o`
    GROUP BY `o`.`CustomerID`
    HAVING COUNT(*) > 1
) AS `o0`
""");
        }

        public override async Task Any_after_GroupBy_without_aggregate(bool async)
        {
            await base.Any_after_GroupBy_without_aggregate(async);

            AssertSql(
                """
SELECT IIF(EXISTS (
        SELECT 1
        FROM `Orders` AS `o`
        GROUP BY `o`.`CustomerID`), TRUE, FALSE)
FROM (SELECT COUNT(*) FROM `#Dual`)
""");
        }

        public override async Task Any_with_predicate_after_GroupBy_without_aggregate(bool async)
        {
            await base.Any_with_predicate_after_GroupBy_without_aggregate(async);

            AssertSql(
                """
SELECT IIF(EXISTS (
        SELECT 1
        FROM `Orders` AS `o`
        GROUP BY `o`.`CustomerID`
        HAVING COUNT(*) > 1), TRUE, FALSE)
FROM (SELECT COUNT(*) FROM `#Dual`)
""");
        }

        public override async Task All_with_predicate_after_GroupBy_without_aggregate(bool async)
        {
            await base.All_with_predicate_after_GroupBy_without_aggregate(async);

            AssertSql(
                """
SELECT IIF(NOT EXISTS (
        SELECT 1
        FROM `Orders` AS `o`
        GROUP BY `o`.`CustomerID`
        HAVING COUNT(*) <= 1), TRUE, FALSE)
FROM (SELECT COUNT(*) FROM `#Dual`)
""");
        }

        public override async Task GroupBy_aggregate_followed_by_another_GroupBy_aggregate(bool async)
        {
            await base.GroupBy_aggregate_followed_by_another_GroupBy_aggregate(async);

            AssertSql(
                """
SELECT `o1`.`Key0` AS `Key`, IIF(SUM(`o1`.`Count`) IS NULL, 0, SUM(`o1`.`Count`)) AS `Count`
FROM (
    SELECT `o0`.`Count`, 1 AS `Key0`
    FROM (
        SELECT COUNT(*) AS `Count`
        FROM `Orders` AS `o`
        GROUP BY `o`.`CustomerID`
    ) AS `o0`
) AS `o1`
GROUP BY `o1`.`Key0`
""");
        }

        public override async Task GroupBy_Count_in_projection(bool async)
        {
            await base.GroupBy_Count_in_projection(async);

            AssertSql(
                """
SELECT [o].[OrderID], [o].[OrderDate], CASE
    WHEN EXISTS (
        SELECT 1
        FROM [Order Details] AS [o0]
        WHERE [o].[OrderID] = [o0].[OrderID] AND [o0].[ProductID] < 25) THEN CAST(1 AS bit)
    ELSE CAST(0 AS bit)
END AS [HasOrderDetails], CASE
    WHEN (
        SELECT COUNT(*)
        FROM (
            SELECT [p].[ProductName]
            FROM [Order Details] AS [o1]
            INNER JOIN [Products] AS [p] ON [o1].[ProductID] = [p].[ProductID]
            WHERE [o].[OrderID] = [o1].[OrderID] AND [o1].[ProductID] < 25
            GROUP BY [p].[ProductName]
        ) AS [t]) > 1 THEN CAST(1 AS bit)
    ELSE CAST(0 AS bit)
END AS [HasMultipleProducts]
FROM [Orders] AS [o]
WHERE [o].[OrderDate] IS NOT NULL
""");
        }

        public override async Task GroupBy_nominal_type_count(bool async)
        {
            await base.GroupBy_nominal_type_count(async);

            AssertSql(
                """
SELECT COUNT(*)
FROM (
    SELECT 1
    FROM `Orders` AS `o`
    GROUP BY `o`.`CustomerID`
) AS `o0`
""");
        }

        public override async Task GroupBy_based_on_renamed_property_simple(bool isAsync)
        {
            await base.GroupBy_based_on_renamed_property_simple(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`City` AS `Renamed`, COUNT(*) AS `Count`
                    FROM `Customers` AS `c`
                    GROUP BY `c`.`City`
                    """);
        }

        public override async Task GroupBy_based_on_renamed_property_complex(bool isAsync)
        {
            await base.GroupBy_based_on_renamed_property_complex(isAsync);

            AssertSql(
                """
SELECT `c0`.`Renamed` AS `Key`, COUNT(*) AS `Count`
FROM (
    SELECT DISTINCT `c`.`City` AS `Renamed`, `c`.`CustomerID`
    FROM `Customers` AS `c`
) AS `c0`
GROUP BY `c0`.`Renamed`
""");
        }

        public override async Task Join_groupby_anonymous_orderby_anonymous_projection(bool async)
        {
            await base.Join_groupby_anonymous_orderby_anonymous_projection(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `o`.`OrderDate`
FROM `Customers` AS `c`
INNER JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
GROUP BY `c`.`CustomerID`, `o`.`OrderDate`
ORDER BY `o`.`OrderDate`
""");
        }

        public override async Task Odata_groupby_empty_key(bool async)
        {
            await base.Odata_groupby_empty_key(async);

            AssertSql(
                """
SELECT N'TotalAmount' AS [Name], COALESCE(SUM(CAST([t].[OrderID] AS decimal(18,2))), 0.0) AS [Value]
FROM (
    SELECT [o].[OrderID], 1 AS [Key]
    FROM [Orders] AS [o]
) AS [t]
GROUP BY [t].[Key]
""");
        }

        public override async Task GroupBy_with_group_key_access_thru_navigation(bool isAsync)
        {
            await base.GroupBy_with_group_key_access_thru_navigation(isAsync);

            AssertSql(
                $"""
                    SELECT `o0`.`CustomerID` AS `Key`, IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `Aggregate`
                    FROM `Order Details` AS `o`
                    INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`
                    GROUP BY `o0`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_with_group_key_access_thru_nested_navigation(bool isAsync)
        {
            await base.GroupBy_with_group_key_access_thru_nested_navigation(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`Country` AS `Key`, IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `Aggregate`
                    FROM (`Order Details` AS `o`
                    INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`)
                    LEFT JOIN `Customers` AS `c` ON `o0`.`CustomerID` = `c`.`CustomerID`
                    GROUP BY `c`.`Country`
                    """);
        }

        public override async Task GroupBy_with_group_key_being_navigation(bool isAsync)
        {
            await base.GroupBy_with_group_key_being_navigation(isAsync);

            AssertSql(
                $"""
                    SELECT `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`, IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `Aggregate`
                    FROM `Order Details` AS `o`
                    INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`
                    GROUP BY `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
                    """);
        }

        public override async Task GroupBy_with_group_key_being_nested_navigation(bool isAsync)
        {
            await base.GroupBy_with_group_key_being_nested_navigation(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, IIF(SUM(`o`.`OrderID`) IS NULL, 0, SUM(`o`.`OrderID`)) AS `Aggregate`
                    FROM (`Order Details` AS `o`
                    INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`)
                    LEFT JOIN `Customers` AS `c` ON `o0`.`CustomerID` = `c`.`CustomerID`
                    GROUP BY `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    """);
        }

        public override async Task GroupBy_with_group_key_being_navigation_with_entity_key_projection(bool isAsync)
        {
            await base.GroupBy_with_group_key_being_navigation_with_entity_key_projection(isAsync);

            AssertSql(
                $"""
                    SELECT `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
                    FROM `Order Details` AS `o`
                    INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`
                    GROUP BY `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
                    """);
        }

        public override async Task GroupBy_with_group_key_being_navigation_with_complex_projection(bool isAsync)
        {
            await base.GroupBy_with_group_key_being_navigation_with_complex_projection(isAsync);

            AssertSql(
                $@"");
        }

        public override async Task GroupBy_with_order_by_skip_and_another_order_by(bool isAsync)
        {
            await base.GroupBy_with_order_by_skip_and_another_order_by(isAsync);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@__p_0='80'")}
                    
                    SELECT SUM(`t`.`OrderID`)
                    FROM (
                        SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
                        FROM `Orders` AS `o`
                        ORDER BY `o`.`CustomerID`, `o`.`OrderID`
                        SKIP {AssertSqlHelper.Parameter("@__p_0")}
                    ) AS `t`
                    GROUP BY `t`.`CustomerID`
                    """);
        }

        public override async Task GroupBy_Property_Select_Count_with_predicate(bool async)
        {
            await base.GroupBy_Property_Select_Count_with_predicate(async);

            AssertSql(
                """
SELECT COUNT(IIF(`o`.`OrderID` < 10300, 1, NULL))
FROM `Orders` AS `o`
GROUP BY `o`.`CustomerID`
""");
        }

        public override async Task GroupBy_Property_Select_LongCount_with_predicate(bool async)
        {
            await base.GroupBy_Property_Select_LongCount_with_predicate(async);

            AssertSql(
                """
SELECT COUNT(IIF(`o`.`OrderID` < 10300, 1, NULL))
FROM `Orders` AS `o`
GROUP BY `o`.`CustomerID`
""");
        }

        public override async Task GroupBy_orderby_projection_with_coalesce_operation(bool async)
        {
            await base.GroupBy_orderby_projection_with_coalesce_operation(async);

            AssertSql(
                """
SELECT IIF(`c`.`City` IS NULL, 'Unknown', `c`.`City`) AS `Locality`, COUNT(*) AS `Count`
FROM `Customers` AS `c`
GROUP BY `c`.`City`
ORDER BY COUNT(*) DESC, `c`.`City`
""");
        }

        public override async Task GroupBy_let_orderby_projection_with_coalesce_operation(bool async)
        {
            await base.GroupBy_let_orderby_projection_with_coalesce_operation(async);

            AssertSql();
        }

        public override async Task GroupBy_Min_Where_optional_relationship(bool async)
        {
            await base.GroupBy_Min_Where_optional_relationship(async);

            AssertSql(
                """
SELECT `c`.`CustomerID` AS `Key`, COUNT(*) AS `Count`
FROM `Orders` AS `o`
LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
GROUP BY `c`.`CustomerID`
HAVING COUNT(*) <> 2
""");
        }

        public override async Task GroupBy_Min_Where_optional_relationship_2(bool async)
        {
            await base.GroupBy_Min_Where_optional_relationship_2(async);

            AssertSql(
                """
SELECT `c`.`CustomerID` AS `Key`, COUNT(*) AS `Count`
FROM `Orders` AS `o`
LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
GROUP BY `c`.`CustomerID`
HAVING COUNT(*) < 2 OR COUNT(*) > 2
""");
        }

        public override async Task GroupBy_aggregate_over_a_subquery(bool async)
        {
            await base.GroupBy_aggregate_over_a_subquery(async);

            AssertSql(
                """
SELECT `o`.`CustomerID` AS `Key`, (
    SELECT COUNT(*)
    FROM `Customers` AS `c`
    WHERE `c`.`CustomerID` = `o`.`CustomerID`) AS `Count`
FROM `Orders` AS `o`
GROUP BY `o`.`CustomerID`
""");
        }

        public override async Task GroupBy_aggregate_join_with_grouping_key(bool async)
        {
            await base.GroupBy_aggregate_join_with_grouping_key(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o0`.`Count`
FROM (
    SELECT `o`.`CustomerID` AS `Key`, COUNT(*) AS `Count`
    FROM `Orders` AS `o`
    GROUP BY `o`.`CustomerID`
) AS `o0`
INNER JOIN `Customers` AS `c` ON `o0`.`Key` = `c`.`CustomerID`
""");
        }

        public override async Task GroupBy_aggregate_join_with_group_result(bool async)
        {
            await base.GroupBy_aggregate_join_with_group_result(async);

            AssertSql(
                """
SELECT `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM (
    SELECT `o`.`CustomerID` AS `Key`, MAX(`o`.`OrderDate`) AS `LastOrderDate`
    FROM `Orders` AS `o`
    GROUP BY `o`.`CustomerID`
) AS `o1`
INNER JOIN `Orders` AS `o0` ON (`o1`.`Key` = `o0`.`CustomerID` OR (`o1`.`Key` IS NULL AND `o0`.`CustomerID` IS NULL)) AND (`o1`.`LastOrderDate` = `o0`.`OrderDate` OR (`o1`.`LastOrderDate` IS NULL AND `o0`.`OrderDate` IS NULL))
""");
        }

        public override async Task GroupBy_aggregate_from_right_side_of_join(bool async)
        {
            await base.GroupBy_aggregate_from_right_side_of_join(async);

            AssertSql(
                """
SELECT `s0`.`CustomerID`, `s0`.`Address`, `s0`.`City`, `s0`.`CompanyName`, `s0`.`ContactName`, `s0`.`ContactTitle`, `s0`.`Country`, `s0`.`Fax`, `s0`.`Phone`, `s0`.`PostalCode`, `s0`.`Region`, `s0`.`Max`
FROM (
    SELECT TOP @p `s`.`CustomerID`, `s`.`Address`, `s`.`City`, `s`.`CompanyName`, `s`.`ContactName`, `s`.`ContactTitle`, `s`.`Country`, `s`.`Fax`, `s`.`Phone`, `s`.`PostalCode`, `s`.`Region`, `s`.`Max`
    FROM (
        SELECT TOP @p + @p `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o0`.`Max`
        FROM `Customers` AS `c`
        INNER JOIN (
            SELECT `o`.`CustomerID` AS `Key`, MAX(`o`.`OrderDate`) AS `Max`
            FROM `Orders` AS `o`
            GROUP BY `o`.`CustomerID`
        ) AS `o0` ON `c`.`CustomerID` = `o0`.`Key`
        ORDER BY `o0`.`Max`, `c`.`CustomerID`
    ) AS `s`
    ORDER BY `s`.`Max` DESC, `s`.`CustomerID` DESC
) AS `s0`
ORDER BY `s0`.`Max`, `s0`.`CustomerID`
""");
        }

        public override async Task GroupBy_aggregate_join_another_GroupBy_aggregate(bool async)
        {
            await base.GroupBy_aggregate_join_another_GroupBy_aggregate(async);

            AssertSql(
                """
SELECT `o1`.`Key`, `o1`.`Total`, `o2`.`ThatYear`
FROM (
    SELECT `o`.`CustomerID` AS `Key`, COUNT(*) AS `Total`
    FROM `Orders` AS `o`
    GROUP BY `o`.`CustomerID`
) AS `o1`
INNER JOIN (
    SELECT `o0`.`CustomerID` AS `Key`, COUNT(*) AS `ThatYear`
    FROM `Orders` AS `o0`
    WHERE DATEPART('yyyy', `o0`.`OrderDate`) = 1997
    GROUP BY `o0`.`CustomerID`
) AS `o2` ON `o1`.`Key` = `o2`.`Key`
""");
        }

        public override async Task GroupBy_aggregate_after_skip_0_take_0(bool async)
        {
            await base.GroupBy_aggregate_after_skip_0_take_0(async);

            AssertSql(
                """
SELECT `o0`.`CustomerID` AS `Key`, COUNT(*) AS `Total`
FROM (
    SELECT `o2`.`CustomerID`
    FROM (
        SELECT `o1`.`CustomerID`
        FROM (
            SELECT `o`.`CustomerID`
            FROM `Orders` AS `o`
            WHERE 0 = 1
        ) AS `o1`
        WHERE 0 = 1
    ) AS `o2`
) AS `o0`
GROUP BY `o0`.`CustomerID`
""");
        }

        public override async Task GroupBy_skip_0_take_0_aggregate(bool async)
        {
            await base.GroupBy_skip_0_take_0_aggregate(async);

            AssertSql(
                """
SELECT `o1`.`Key`, `o1`.`Total`
FROM (
    SELECT `o0`.`Key`, `o0`.`Total`
    FROM (
        SELECT `o`.`CustomerID` AS `Key`, COUNT(*) AS `Total`
        FROM `Orders` AS `o`
        WHERE `o`.`OrderID` > 10500
        GROUP BY `o`.`CustomerID`
        HAVING 0 = 1
    ) AS `o0`
    WHERE 0 = 1
) AS `o1`
""");
        }

        public override async Task GroupBy_aggregate_followed_another_GroupBy_aggregate(bool async)
        {
            await base.GroupBy_aggregate_followed_another_GroupBy_aggregate(async);

            AssertSql(
                """
SELECT `o1`.`CustomerID` AS `Key`, COUNT(*) AS `Count`
FROM (
    SELECT `o0`.`CustomerID`
    FROM (
        SELECT `o`.`CustomerID`, DATEPART('yyyy', `o`.`OrderDate`) AS `Year`
        FROM `Orders` AS `o`
    ) AS `o0`
    GROUP BY `o0`.`CustomerID`, `o0`.`Year`
) AS `o1`
GROUP BY `o1`.`CustomerID`
""");
        }

        public override async Task GroupBy_aggregate_without_selectMany_selecting_first(bool async)
        {
            await base.GroupBy_aggregate_without_selectMany_selecting_first(async);

            AssertSql(
                """
SELECT `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM (
    SELECT MIN(`o`.`OrderID`) AS `c`
    FROM `Orders` AS `o`
    GROUP BY `o`.`CustomerID`
) AS `o1`,
`Orders` AS `o0`
WHERE `o0`.`OrderID` = `o1`.`c`
""");
        }

        public override async Task GroupBy_aggregate_left_join_GroupBy_aggregate_left_join(bool async)
        {
            await base.GroupBy_aggregate_left_join_GroupBy_aggregate_left_join(async);

            AssertSql(
                """
SELECT [o0].[OrderID], [o0].[CustomerID], [o0].[EmployeeID], [o0].[OrderDate]
FROM (
    SELECT MIN([o].[OrderID]) AS [c]
    FROM [Orders] AS [o]
    GROUP BY [o].[CustomerID]
) AS [t]
CROSS JOIN [Orders] AS [o0]
WHERE [o0].[OrderID] = [t].[c]
""");
        }

        public override async Task GroupBy_selecting_grouping_key_list(bool async)
        {
            await base.GroupBy_selecting_grouping_key_list(async);

            AssertSql(
                """
SELECT `o1`.`CustomerID`, `o0`.`CustomerID`, `o0`.`OrderID`
FROM (
    SELECT `o`.`CustomerID`
    FROM `Orders` AS `o`
    GROUP BY `o`.`CustomerID`
) AS `o1`
LEFT JOIN `Orders` AS `o0` ON `o1`.`CustomerID` = `o0`.`CustomerID`
ORDER BY `o1`.`CustomerID`
""");
        }

        public override async Task GroupBy_with_grouping_key_using_Like(bool isAsync)
        {
            await base.GroupBy_with_grouping_key_using_Like(isAsync);

            AssertSql(
                """
SELECT `o0`.`Key`, COUNT(*) AS `Count`
FROM (
    SELECT IIF((`o`.`CustomerID` LIKE 'A%') AND `o`.`CustomerID` IS NOT NULL, TRUE, FALSE) AS `Key`
    FROM `Orders` AS `o`
) AS `o0`
GROUP BY `o0`.`Key`
""");
        }

        public override async Task GroupBy_with_grouping_key_DateTime_Day(bool isAsync)
        {
            await base.GroupBy_with_grouping_key_DateTime_Day(isAsync);

            AssertSql(
                """
SELECT `o0`.`Key`, COUNT(*) AS `Count`
FROM (
    SELECT DATEPART('d', `o`.`OrderDate`) AS `Key`
    FROM `Orders` AS `o`
) AS `o0`
GROUP BY `o0`.`Key`
""");
        }

        public override async Task GroupBy_with_cast_inside_grouping_aggregate(bool isAsync)
        {
            await base.GroupBy_with_cast_inside_grouping_aggregate(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`CustomerID` AS `Key`, COUNT(*) AS `Count`, IIF(SUM(CLNG(`o`.`OrderID`)) IS NULL, 0, SUM(CLNG(`o`.`OrderID`))) AS `Sum`
                    FROM `Orders` AS `o`
                    GROUP BY `o`.`CustomerID`
                    """);
        }

        public override async Task Complex_query_with_groupBy_in_subquery1(bool async)
        {
            await base.Complex_query_with_groupBy_in_subquery1(async);

            AssertSql(
                """
SELECT [c].[CustomerID], [t].[Sum], [t].[CustomerID]
FROM [Customers] AS [c]
OUTER APPLY (
    SELECT COALESCE(SUM([o].[OrderID]), 0) AS [Sum], [o].[CustomerID]
    FROM [Orders] AS [o]
    WHERE [c].[CustomerID] = [o].[CustomerID]
    GROUP BY [o].[CustomerID]
) AS [t]
ORDER BY [c].[CustomerID]
""");
        }

        public override async Task Complex_query_with_groupBy_in_subquery2(bool async)
        {
            await base.Complex_query_with_groupBy_in_subquery2(async);

            AssertSql(
                """
SELECT [c].[CustomerID], [t].[Max], [t].[Sum], [t].[CustomerID]
FROM [Customers] AS [c]
OUTER APPLY (
    SELECT MAX(CAST(LEN([o].[CustomerID]) AS int)) AS [Max], COALESCE(SUM([o].[OrderID]), 0) AS [Sum], [o].[CustomerID]
    FROM [Orders] AS [o]
    WHERE [c].[CustomerID] = [o].[CustomerID]
    GROUP BY [o].[CustomerID]
) AS [t]
ORDER BY [c].[CustomerID]
""");
        }

        public override async Task Complex_query_with_groupBy_in_subquery3(bool async)
        {
            await base.Complex_query_with_groupBy_in_subquery3(async);

            AssertSql(
                """
SELECT [c].[CustomerID], [t].[Max], [t].[Sum], [t].[CustomerID]
FROM [Customers] AS [c]
OUTER APPLY (
    SELECT MAX(CAST(LEN([o].[CustomerID]) AS int)) AS [Max], COALESCE(SUM([o].[OrderID]), 0) AS [Sum], [o].[CustomerID]
    FROM [Orders] AS [o]
    GROUP BY [o].[CustomerID]
) AS [t]
ORDER BY [c].[CustomerID]
""");
        }

        public override async Task Group_by_with_projection_into_DTO(bool async)
        {
            await base.Group_by_with_projection_into_DTO(async);

            AssertSql(
                """
SELECT CLNG(`o`.`OrderID`) AS `Id`, COUNT(*) AS `Count`
FROM `Orders` AS `o`
GROUP BY `o`.`OrderID`
""");
        }

        public override async Task Where_select_function_groupby_followed_by_another_select_with_aggregates(bool async)
        {
            await base.Where_select_function_groupby_followed_by_another_select_with_aggregates(async);

            AssertSql(
                """
SELECT `o`.`CustomerID` AS `Key`, IIF(SUM(IIF((2020 - DATEPART('yyyy', `o`.`OrderDate`)) <= 30, `o`.`OrderID`, 0)) IS NULL, 0, SUM(IIF((2020 - DATEPART('yyyy', `o`.`OrderDate`)) <= 30, `o`.`OrderID`, 0))) AS `Sum1`, IIF(SUM(IIF((2020 - DATEPART('yyyy', `o`.`OrderDate`)) > 30 AND (2020 - DATEPART('yyyy', `o`.`OrderDate`)) <= 60, `o`.`OrderID`, 0)) IS NULL, 0, SUM(IIF((2020 - DATEPART('yyyy', `o`.`OrderDate`)) > 30 AND (2020 - DATEPART('yyyy', `o`.`OrderDate`)) <= 60, `o`.`OrderID`, 0))) AS `Sum2`
FROM `Orders` AS `o`
WHERE `o`.`CustomerID` LIKE 'A%'
GROUP BY `o`.`CustomerID`
""");
        }

        public override async Task Group_by_column_project_constant(bool async)
        {
            await base.Group_by_column_project_constant(async);

            AssertSql(
                """
SELECT 42
FROM `Orders` AS `o`
GROUP BY `o`.`CustomerID`
ORDER BY `o`.`CustomerID`
""");
        }

        public override async Task Key_plus_key_in_projection(bool async)
        {
            await base.Key_plus_key_in_projection(async);

            AssertSql(
                """
SELECT `o`.`OrderID` + `o`.`OrderID` AS `Value`, AVG(CDBL(`o`.`OrderID`)) AS `Average`
FROM `Orders` AS `o`
LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
GROUP BY `o`.`OrderID`
""");
        }

        public override async Task Group_by_with_arithmetic_operation_inside_aggregate(bool isAsync)
        {
            await base.Group_by_with_arithmetic_operation_inside_aggregate(isAsync);

            AssertSql(
                """
    SELECT `o`.`CustomerID` AS `Key`, IIF(SUM(`o`.`OrderID` + IIF(LEN(`o`.`CustomerID`) IS NULL, NULL, CLNG(LEN(`o`.`CustomerID`)))) IS NULL, 0, SUM(`o`.`OrderID` + IIF(LEN(`o`.`CustomerID`) IS NULL, NULL, CLNG(LEN(`o`.`CustomerID`))))) AS `Sum`
    FROM `Orders` AS `o`
    GROUP BY `o`.`CustomerID`
    """);
        }

        public override async Task GroupBy_scalar_subquery(bool async)
        {
            await base.GroupBy_scalar_subquery(async);

            AssertSql(
                """
SELECT `o0`.`Key`, COUNT(*) AS `Count`
FROM (
    SELECT (
        SELECT TOP 1 `c`.`ContactName`
        FROM `Customers` AS `c`
        WHERE `c`.`CustomerID` = `o`.`CustomerID`) AS `Key`
    FROM `Orders` AS `o`
) AS `o0`
GROUP BY `o0`.`Key`
""");
        }

        public override async Task AsEnumerable_in_subquery_for_GroupBy(bool async)
        {
            await base.AsEnumerable_in_subquery_for_GroupBy(async);

            AssertSql(
                """
SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region], [t2].[OrderID], [t2].[CustomerID], [t2].[EmployeeID], [t2].[OrderDate], [t2].[CustomerID0]
FROM [Customers] AS [c]
OUTER APPLY (
    SELECT [t0].[OrderID], [t0].[CustomerID], [t0].[EmployeeID], [t0].[OrderDate], [t].[CustomerID] AS [CustomerID0]
    FROM (
        SELECT [o].[CustomerID]
        FROM [Orders] AS [o]
        WHERE [o].[CustomerID] = [c].[CustomerID]
        GROUP BY [o].[CustomerID]
    ) AS [t]
    LEFT JOIN (
        SELECT [t1].[OrderID], [t1].[CustomerID], [t1].[EmployeeID], [t1].[OrderDate]
        FROM (
            SELECT [o0].[OrderID], [o0].[CustomerID], [o0].[EmployeeID], [o0].[OrderDate], ROW_NUMBER() OVER(PARTITION BY [o0].[CustomerID] ORDER BY [o0].[OrderDate] DESC) AS [row]
            FROM [Orders] AS [o0]
            WHERE [o0].[CustomerID] = [c].[CustomerID]
        ) AS [t1]
        WHERE [t1].[row] <= 1
    ) AS [t0] ON [t].[CustomerID] = [t0].[CustomerID]
) AS [t2]
WHERE [c].[CustomerID] LIKE N'F%'
ORDER BY [c].[CustomerID], [t2].[CustomerID0]
""");
        }

        public override async Task GroupBy_aggregate_from_multiple_query_in_same_projection(bool async)
        {
            await base.GroupBy_aggregate_from_multiple_query_in_same_projection(async);

            AssertSql(
                """
SELECT [t].[CustomerID], [t0].[Key], [t0].[C], [t0].[c0]
FROM (
    SELECT [o].[CustomerID]
    FROM [Orders] AS [o]
    GROUP BY [o].[CustomerID]
) AS [t]
OUTER APPLY (
    SELECT TOP(1) [e].[City] AS [Key], COUNT(*) + (
        SELECT COUNT(*)
        FROM [Orders] AS [o0]
        WHERE [t].[CustomerID] = [o0].[CustomerID] OR ([t].[CustomerID] IS NULL AND [o0].[CustomerID] IS NULL)) AS [C], 1 AS [c0]
    FROM [Employees] AS [e]
    WHERE [e].[City] = N'Seattle'
    GROUP BY [e].[City]
    ORDER BY (SELECT 1)
) AS [t0]
""");
        }

        public override async Task GroupBy_aggregate_from_multiple_query_in_same_projection_2(bool async)
        {
            await base.GroupBy_aggregate_from_multiple_query_in_same_projection_2(async);

            AssertSql(
                """
SELECT [o].[CustomerID] AS [Key], COALESCE((
    SELECT TOP(1) COUNT(*) + MIN([o].[OrderID])
    FROM [Employees] AS [e]
    WHERE [e].[City] = N'Seattle'
    GROUP BY [e].[City]
    ORDER BY (SELECT 1)), 0) AS [A]
FROM [Orders] AS [o]
GROUP BY [o].[CustomerID]
""");
        }

        public override async Task GroupBy_aggregate_from_multiple_query_in_same_projection_3(bool async)
        {
            await base.GroupBy_aggregate_from_multiple_query_in_same_projection_3(async);

            AssertSql(
                """
SELECT [o].[CustomerID] AS [Key], COALESCE((
    SELECT TOP(1) COUNT(*) + (
        SELECT COUNT(*)
        FROM [Orders] AS [o0]
        WHERE [o].[CustomerID] = [o0].[CustomerID] OR ([o].[CustomerID] IS NULL AND [o0].[CustomerID] IS NULL))
    FROM [Employees] AS [e]
    WHERE [e].[City] = N'Seattle'
    GROUP BY [e].[City]
    ORDER BY COUNT(*) + (
        SELECT COUNT(*)
        FROM [Orders] AS [o0]
        WHERE [o].[CustomerID] = [o0].[CustomerID] OR ([o].[CustomerID] IS NULL AND [o0].[CustomerID] IS NULL))), 0) AS [A]
FROM [Orders] AS [o]
GROUP BY [o].[CustomerID]
""");
        }

        public override async Task GroupBy_scalar_aggregate_in_set_operation(bool async)
        {
            await base.GroupBy_scalar_aggregate_in_set_operation(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, 0 AS `Sequence`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` LIKE 'F%'
UNION
SELECT `o`.`CustomerID`, 1 AS `Sequence`
FROM `Orders` AS `o`
GROUP BY `o`.`CustomerID`
""");
        }

        public override async Task Select_uncorrelated_collection_with_groupby_when_outer_is_distinct(bool async)
        {
            await base.Select_uncorrelated_collection_with_groupby_when_outer_is_distinct(async);

            AssertSql(
                """
SELECT [t].[City], [t0].[ProductID], [t1].[c], [t1].[ProductID]
FROM (
    SELECT DISTINCT [c].[City]
    FROM [Orders] AS [o]
    LEFT JOIN [Customers] AS [c] ON [o].[CustomerID] = [c].[CustomerID]
    WHERE [o].[CustomerID] LIKE N'A%'
) AS [t]
OUTER APPLY (
    SELECT [p].[ProductID]
    FROM [Products] AS [p]
    GROUP BY [p].[ProductID]
) AS [t0]
OUTER APPLY (
    SELECT COUNT(*) AS [c], [p0].[ProductID]
    FROM [Products] AS [p0]
    GROUP BY [p0].[ProductID]
) AS [t1]
ORDER BY [t].[City], [t0].[ProductID]
""");
        }

        public override async Task Select_correlated_collection_after_GroupBy_aggregate_when_identifier_does_not_change(bool async)
        {
            await base.Select_correlated_collection_after_GroupBy_aggregate_when_identifier_does_not_change(async);

            AssertSql(
                """
SELECT `c0`.`CustomerID`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM (
    SELECT `c`.`CustomerID`
    FROM `Customers` AS `c`
    GROUP BY `c`.`CustomerID`
    HAVING `c`.`CustomerID` LIKE 'F%'
) AS `c0`
LEFT JOIN `Orders` AS `o` ON `c0`.`CustomerID` = `o`.`CustomerID`
ORDER BY `c0`.`CustomerID`
""");
        }

        public override async Task Select_correlated_collection_after_GroupBy_aggregate_when_identifier_changes(bool async)
        {
            await base.Select_correlated_collection_after_GroupBy_aggregate_when_identifier_changes(async);

            AssertSql(
                """
SELECT `o1`.`CustomerID`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM (
    SELECT `o`.`CustomerID`
    FROM `Orders` AS `o`
    GROUP BY `o`.`CustomerID`
    HAVING `o`.`CustomerID` LIKE 'F%'
) AS `o1`
LEFT JOIN `Orders` AS `o0` ON `o1`.`CustomerID` = `o0`.`CustomerID`
ORDER BY `o1`.`CustomerID`
""");
        }

        public override async Task Select_correlated_collection_after_GroupBy_aggregate_when_identifier_changes_to_complex(bool async)
            => await base.Select_correlated_collection_after_GroupBy_aggregate_when_identifier_changes_to_complex(async);

        //AssertSql(" ");
        public override async Task Complex_query_with_group_by_in_subquery5(bool async)
        {
            await base.Complex_query_with_group_by_in_subquery5(async);

            AssertSql(
                """
SELECT [t].[c], [t].[ProductID], [t0].[CustomerID], [t0].[City]
FROM (
    SELECT COALESCE(SUM([o].[ProductID] + [o].[OrderID] * 1000), 0) AS [c], [o].[ProductID], MIN([o].[OrderID] / 100) AS [c0]
    FROM [Order Details] AS [o]
    INNER JOIN [Orders] AS [o0] ON [o].[OrderID] = [o0].[OrderID]
    LEFT JOIN [Customers] AS [c] ON [o0].[CustomerID] = [c].[CustomerID]
    WHERE [c].[CustomerID] = N'ALFKI'
    GROUP BY [o].[ProductID]
) AS [t]
OUTER APPLY (
    SELECT [c0].[CustomerID], [c0].[City]
    FROM [Customers] AS [c0]
    WHERE CAST(LEN([c0].[CustomerID]) AS int) < [t].[c0]
) AS [t0]
ORDER BY [t].[ProductID], [t0].[CustomerID]
""");
        }

        public override async Task Complex_query_with_groupBy_in_subquery4(bool async)
        {
            await base.Complex_query_with_groupBy_in_subquery4(async);

            AssertSql(
                """
SELECT [c].[CustomerID], [t1].[Sum], [t1].[Count], [t1].[Key]
FROM [Customers] AS [c]
OUTER APPLY (
    SELECT COALESCE(SUM([t].[OrderID]), 0) AS [Sum], (
        SELECT COUNT(*)
        FROM (
            SELECT [o0].[OrderID], [o0].[CustomerID], [o0].[EmployeeID], [o0].[OrderDate], [c1].[CustomerID] AS [CustomerID0], [c1].[Address], [c1].[City], [c1].[CompanyName], [c1].[ContactName], [c1].[ContactTitle], [c1].[Country], [c1].[Fax], [c1].[Phone], [c1].[PostalCode], [c1].[Region], COALESCE([c1].[City], N'') + COALESCE([o0].[CustomerID], N'') AS [Key]
            FROM [Orders] AS [o0]
            LEFT JOIN [Customers] AS [c1] ON [o0].[CustomerID] = [c1].[CustomerID]
            WHERE [c].[CustomerID] = [o0].[CustomerID]
        ) AS [t0]
        LEFT JOIN [Customers] AS [c0] ON [t0].[CustomerID] = [c0].[CustomerID]
        WHERE ([t].[Key] = [t0].[Key] OR ([t].[Key] IS NULL AND [t0].[Key] IS NULL)) AND COALESCE([c0].[City], N'') + COALESCE([t0].[CustomerID], N'') LIKE N'Lon%') AS [Count], [t].[Key]
    FROM (
        SELECT [o].[OrderID], COALESCE([c2].[City], N'') + COALESCE([o].[CustomerID], N'') AS [Key]
        FROM [Orders] AS [o]
        LEFT JOIN [Customers] AS [c2] ON [o].[CustomerID] = [c2].[CustomerID]
        WHERE [c].[CustomerID] = [o].[CustomerID]
    ) AS [t]
    GROUP BY [t].[Key]
) AS [t1]
ORDER BY [c].[CustomerID]
""");
        }

        public override async Task GroupBy_aggregate_SelectMany(bool async)
        {
            await base.GroupBy_aggregate_SelectMany(async);

            AssertSql();
        }

        public override async Task Final_GroupBy_property_entity(bool async)
        {
            await base.Final_GroupBy_property_entity(async);

            AssertSql(
                """
SELECT `c`.`City`, `c`.`CustomerID`, `c`.`Address`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
ORDER BY `c`.`City`
""");
        }

        public override async Task Final_GroupBy_entity(bool async)
        {
            await base.Final_GroupBy_entity(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Orders` AS `o`
LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
WHERE `o`.`OrderID` < 10500
ORDER BY `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
""");
        }

        public override async Task Final_GroupBy_property_entity_non_nullable(bool async)
        {
            await base.Final_GroupBy_property_entity_non_nullable(async);

            AssertSql(
                """
SELECT `o`.`OrderID`, `o`.`ProductID`, `o`.`Discount`, `o`.`Quantity`, `o`.`UnitPrice`
FROM `Order Details` AS `o`
WHERE `o`.`OrderID` < 10500
ORDER BY `o`.`OrderID`
""");
        }

        public override async Task Final_GroupBy_property_anonymous_type(bool async)
        {
            await base.Final_GroupBy_property_anonymous_type(async);

            AssertSql(
                """
SELECT `c`.`City`, `c`.`ContactName`, `c`.`ContactTitle`
FROM `Customers` AS `c`
ORDER BY `c`.`City`
""");
        }

        public override async Task Final_GroupBy_multiple_properties_entity(bool async)
        {
            await base.Final_GroupBy_multiple_properties_entity(async);

            AssertSql(
                """
SELECT `c`.`City`, `c`.`Region`, `c`.`CustomerID`, `c`.`Address`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`
FROM `Customers` AS `c`
ORDER BY `c`.`City`, `c`.`Region`
""");
        }

        public override async Task Final_GroupBy_complex_key_entity(bool async)
        {
            await base.Final_GroupBy_complex_key_entity(async);

            AssertSql(
                """
SELECT `c0`.`City`, `c0`.`Region`, `c0`.`Constant`, `c0`.`CustomerID`, `c0`.`Address`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`
FROM (
    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, 1 AS `Constant`
    FROM `Customers` AS `c`
) AS `c0`
ORDER BY `c0`.`City`, `c0`.`Region`, `c0`.`Constant`
""");
        }

        public override async Task Final_GroupBy_nominal_type_entity(bool async)
        {
            await base.Final_GroupBy_nominal_type_entity(async);

            AssertSql(
                """
SELECT `c0`.`City`, `c0`.`Constant`, `c0`.`CustomerID`, `c0`.`Address`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
FROM (
    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, 1 AS `Constant`
    FROM `Customers` AS `c`
) AS `c0`
ORDER BY `c0`.`City`, `c0`.`Constant`
""");
        }

        public override async Task Final_GroupBy_property_anonymous_type_element_selector(bool async)
        {
            await base.Final_GroupBy_property_anonymous_type_element_selector(async);

            AssertSql(
                """
SELECT `c`.`City`, `c`.`ContactName`, `c`.`ContactTitle`
FROM `Customers` AS `c`
ORDER BY `c`.`City`
""");
        }

        public override async Task Final_GroupBy_property_entity_Include_collection(bool async)
        {
            await base.Final_GroupBy_property_entity_Include_collection(async);

            AssertSql(
                """
SELECT `c`.`City`, `c`.`CustomerID`, `c`.`Address`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Customers` AS `c`
LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
WHERE `c`.`Country` = 'USA'
ORDER BY `c`.`City`, `c`.`CustomerID`
""");
        }

        public override async Task Final_GroupBy_property_entity_projecting_collection(bool async)
        {
            await base.Final_GroupBy_property_entity_projecting_collection(async);

            AssertSql(
                """
SELECT `c`.`City`, `c`.`CustomerID`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Customers` AS `c`
LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
WHERE `c`.`Country` = 'USA'
ORDER BY `c`.`City`, `c`.`CustomerID`
""");
        }

        public override async Task Final_GroupBy_property_entity_projecting_collection_composed(bool async)
        {
            await base.Final_GroupBy_property_entity_projecting_collection_composed(async);

            AssertSql(
                """
SELECT `c`.`City`, `c`.`CustomerID`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM `Customers` AS `c`
LEFT JOIN (
    SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
    FROM `Orders` AS `o`
    WHERE `o`.`OrderID` < 11000
) AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE `c`.`Country` = 'USA'
ORDER BY `c`.`City`, `c`.`CustomerID`
""");
        }

        public override async Task Final_GroupBy_property_entity_projecting_collection_and_single_result(bool async)
        {
            await base.Final_GroupBy_property_entity_projecting_collection_and_single_result(async);

            AssertSql(
                """
SELECT [c].[City], [c].[CustomerID], [t].[OrderID], [t].[CustomerID], [t].[EmployeeID], [t].[OrderDate], [t0].[OrderID], [t0].[CustomerID], [t0].[EmployeeID], [t0].[OrderDate]
FROM [Customers] AS [c]
LEFT JOIN (
    SELECT [o].[OrderID], [o].[CustomerID], [o].[EmployeeID], [o].[OrderDate]
    FROM [Orders] AS [o]
    WHERE [o].[OrderID] < 11000
) AS [t] ON [c].[CustomerID] = [t].[CustomerID]
LEFT JOIN (
    SELECT [t1].[OrderID], [t1].[CustomerID], [t1].[EmployeeID], [t1].[OrderDate]
    FROM (
        SELECT [o0].[OrderID], [o0].[CustomerID], [o0].[EmployeeID], [o0].[OrderDate], ROW_NUMBER() OVER(PARTITION BY [o0].[CustomerID] ORDER BY [o0].[OrderDate] DESC) AS [row]
        FROM [Orders] AS [o0]
    ) AS [t1]
    WHERE [t1].[row] <= 1
) AS [t0] ON [c].[CustomerID] = [t0].[CustomerID]
WHERE [c].[Country] = N'USA'
ORDER BY [c].[City], [c].[CustomerID]
""");
        }

        public override async Task Final_GroupBy_TagWith(bool async)
        {
            await base.Final_GroupBy_TagWith(async);

            AssertSql(
                """
-- foo

SELECT `c`.`City`, `c`.`CustomerID`, `c`.`Address`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
ORDER BY `c`.`City`
""");
        }

        public override async Task GroupBy_Where_with_grouping_result(bool async)
        {
            await base.GroupBy_Where_with_grouping_result(async);

            AssertSql();
        }

        public override async Task GroupBy_OrderBy_with_grouping_result(bool async)
        {
            await base.GroupBy_OrderBy_with_grouping_result(async);

            AssertSql();
        }

        public override async Task GroupBy_SelectMany(bool async)
        {
            await base.GroupBy_SelectMany(async);

            AssertSql();
        }

        public override async Task OrderBy_GroupBy_SelectMany(bool async)
        {
            await base.OrderBy_GroupBy_SelectMany(async);

            AssertSql();
        }

        public override async Task OrderBy_GroupBy_SelectMany_shadow(bool async)
        {
            await base.OrderBy_GroupBy_SelectMany_shadow(async);

            AssertSql();
        }

        public override async Task GroupBy_with_orderby_take_skip_distinct_followed_by_group_key_projection(bool async)
        {
            await base.GroupBy_with_orderby_take_skip_distinct_followed_by_group_key_projection(async);

            AssertSql();
        }

        public override async Task GroupBy_Distinct(bool async)
        {
            await base.GroupBy_Distinct(async);

            AssertSql();
        }

        public override async Task GroupBy_complex_key_without_aggregate(bool async)
        {
            await base.GroupBy_complex_key_without_aggregate(async);

            AssertSql(
                """
SELECT [t0].[Key], [t1].[OrderID], [t1].[CustomerID], [t1].[EmployeeID], [t1].[OrderDate], [t1].[CustomerID0]
FROM (
    SELECT [t].[Key]
    FROM (
        SELECT SUBSTRING([c].[CustomerID], 0 + 1, 1) AS [Key]
        FROM [Orders] AS [o]
        LEFT JOIN [Customers] AS [c] ON [o].[CustomerID] = [c].[CustomerID]
    ) AS [t]
    GROUP BY [t].[Key]
) AS [t0]
LEFT JOIN (
    SELECT [t2].[OrderID], [t2].[CustomerID], [t2].[EmployeeID], [t2].[OrderDate], [t2].[CustomerID0], [t2].[Key]
    FROM (
        SELECT [t3].[OrderID], [t3].[CustomerID], [t3].[EmployeeID], [t3].[OrderDate], [t3].[CustomerID0], [t3].[Key], ROW_NUMBER() OVER(PARTITION BY [t3].[Key] ORDER BY [t3].[OrderID], [t3].[CustomerID0]) AS [row]
        FROM (
            SELECT [o0].[OrderID], [o0].[CustomerID], [o0].[EmployeeID], [o0].[OrderDate], [c0].[CustomerID] AS [CustomerID0], SUBSTRING([c0].[CustomerID], 0 + 1, 1) AS [Key]
            FROM [Orders] AS [o0]
            LEFT JOIN [Customers] AS [c0] ON [o0].[CustomerID] = [c0].[CustomerID]
        ) AS [t3]
    ) AS [t2]
    WHERE 1 < [t2].[row] AND [t2].[row] <= 3
) AS [t1] ON [t0].[Key] = [t1].[Key]
ORDER BY [t0].[Key], [t1].[OrderID]
""");
        }

        private void AssertSql(params string[] expected)
            => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);

        protected override void ClearLog()
            => Fixture.TestSqlLoggerFactory.Clear();
    }
}
