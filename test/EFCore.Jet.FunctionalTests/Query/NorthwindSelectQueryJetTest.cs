﻿// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using EntityFrameworkCore.Jet.FunctionalTests.TestUtilities;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;
using Xunit.Abstractions;

namespace EntityFrameworkCore.Jet.FunctionalTests.Query
{
    public class NorthwindSelectQueryJetTest : NorthwindSelectQueryRelationalTestBase<NorthwindQueryJetFixture<NoopModelCustomizer>>
    {
        public NorthwindSelectQueryJetTest(
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

        public override async Task Projection_when_arithmetic_expression_precedence(bool isAsync)
        {
            await base.Projection_when_arithmetic_expression_precedence(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`OrderID` \ (`o`.`OrderID` \ 2) AS `A`, (`o`.`OrderID` \ `o`.`OrderID`) \ 2 AS `B`
                    FROM `Orders` AS `o`
                    """);
        }

        public override async Task Projection_when_arithmetic_expressions(bool isAsync)
        {
            await base.Projection_when_arithmetic_expressions(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`OrderID`, `o`.`OrderID` * 2 AS `Double`, `o`.`OrderID` + 23 AS `Add`, 100000 - `o`.`OrderID` AS `Sub`, `o`.`OrderID` \ (`o`.`OrderID` \ 2) AS `Divide`, 42 AS `Literal`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
                    FROM `Orders` AS `o`
                    """);
        }

        public override async Task Projection_when_arithmetic_mixed(bool isAsync)
        {
            await base.Projection_when_arithmetic_mixed(isAsync);

            AssertSql(
                """
SELECT CLNG(`e0`.`EmployeeID`) + CLNG(`o0`.`OrderID`) AS `Add`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`, 42 AS `Literal`, `e0`.`EmployeeID`, `e0`.`City`, `e0`.`Country`, `e0`.`FirstName`, `e0`.`ReportsTo`, `e0`.`Title`
FROM (
    SELECT TOP @p `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
    FROM `Orders` AS `o`
    ORDER BY `o`.`OrderID`
) AS `o0`,
(
    SELECT TOP 5 `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
    FROM `Employees` AS `e`
    ORDER BY `e`.`EmployeeID`
) AS `e0`
ORDER BY `o0`.`OrderID`
""");
        }

        public override async Task Projection_when_null_value(bool isAsync)
        {
            await base.Projection_when_null_value(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`Region`
                    FROM `Customers` AS `c`
                    """);
        }

        public override async Task Projection_when_client_evald_subquery(bool isAsync)
        {
            await base.Projection_when_client_evald_subquery(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `o`.`CustomerID`, `o`.`OrderID`
                    FROM `Customers` AS `c`
                    LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
                    ORDER BY `c`.`CustomerID`
                    """);
        }

        public override async Task Project_to_object_array(bool isAsync)
        {
            await base.Project_to_object_array(isAsync);

            AssertSql(
                $"""
                    SELECT `e`.`EmployeeID`, `e`.`ReportsTo`, `e`.`Title`
                    FROM `Employees` AS `e`
                    WHERE `e`.`EmployeeID` = 1
                    """);
        }

        public override async Task Projection_of_entity_type_into_object_array(bool isAsync)
        {
            await base.Projection_of_entity_type_into_object_array(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` LIKE 'A%'
                    ORDER BY `c`.`CustomerID`
                    """);
        }

        public override async Task Projection_of_multiple_entity_types_into_object_array(bool isAsync)
        {
            await base.Projection_of_multiple_entity_types_into_object_array(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Orders` AS `o`
                    LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
                    WHERE `o`.`OrderID` < 10300
                    ORDER BY `o`.`OrderID`
                    """);
        }

        public override async Task Projection_of_entity_type_into_object_list(bool isAsync)
        {
            await base.Projection_of_entity_type_into_object_list(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    ORDER BY `c`.`CustomerID`
                    """);
        }

        public override async Task Project_to_int_array(bool isAsync)
        {
            await base.Project_to_int_array(isAsync);

            AssertSql(
                $"""
                    SELECT `e`.`EmployeeID`, `e`.`ReportsTo`
                    FROM `Employees` AS `e`
                    WHERE `e`.`EmployeeID` = 1
                    """);
        }

        public override async Task Select_bool_closure_with_order_parameter_with_cast_to_nullable(bool isAsync)
        {
            await base.Select_bool_closure_with_order_parameter_with_cast_to_nullable(isAsync);

            AssertSql(
                """
@boolean='False'

SELECT CBOOL(@boolean)
FROM `Customers` AS `c`
""");
        }

        public override async Task Select_scalar(bool isAsync)
        {
            await base.Select_scalar(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`City`
                    FROM `Customers` AS `c`
                    """);
        }

        public override async Task Select_anonymous_one(bool isAsync)
        {
            await base.Select_anonymous_one(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`City`
                    FROM `Customers` AS `c`
                    """);
        }

        public override async Task Select_anonymous_two(bool isAsync)
        {
            await base.Select_anonymous_two(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`City`, `c`.`Phone`
                    FROM `Customers` AS `c`
                    """);
        }

        public override async Task Select_anonymous_three(bool isAsync)
        {
            await base.Select_anonymous_three(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`City`, `c`.`Phone`, `c`.`Country`
                    FROM `Customers` AS `c`
                    """);
        }

        public override async Task Select_anonymous_bool_constant_true(bool isAsync)
        {
            await base.Select_anonymous_bool_constant_true(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, TRUE AS `ConstantTrue`
                    FROM `Customers` AS `c`
                    """);
        }

        public override async Task Select_anonymous_constant_in_expression(bool isAsync)
        {
            await base.Select_anonymous_constant_in_expression(isAsync);

            AssertSql(
                """
    SELECT `c`.`CustomerID`, IIF(LEN(`c`.`CustomerID`) IS NULL, NULL, CLNG(LEN(`c`.`CustomerID`))) + 5 AS `Expression`
    FROM `Customers` AS `c`
    """);
        }

        public override async Task Select_anonymous_conditional_expression(bool isAsync)
        {
            await base.Select_anonymous_conditional_expression(isAsync);

            AssertSql(
                $"""
                    SELECT `p`.`ProductID`, IIF(`p`.`UnitsInStock` > 0, TRUE, FALSE) AS `IsAvailable`
                    FROM `Products` AS `p`
                    """);
        }

        public override async Task Select_constant_int(bool isAsync)
        {
            await base.Select_constant_int(isAsync);

            AssertSql(
                $"""
                    SELECT 0
                    FROM `Customers` AS `c`
                    """);
        }

        public override async Task Select_constant_null_string(bool isAsync)
        {
            await base.Select_constant_null_string(isAsync);

            AssertSql(
                $"""
                    SELECT NULL
                    FROM `Customers` AS `c`
                    """);
        }

        public override async Task Select_local(bool isAsync)
        {
            await base.Select_local(isAsync);

            AssertSql(
                """
@x='10'

SELECT CLNG(@x)
FROM `Customers` AS `c`
""");
        }

        public override async Task Select_scalar_primitive_after_take(bool isAsync)
        {
            await base.Select_scalar_primitive_after_take(isAsync);

            AssertSql(
                """
SELECT TOP @p `e`.`EmployeeID`
FROM `Employees` AS `e`
""");
        }

        public override async Task Select_project_filter(bool isAsync)
        {
            await base.Select_project_filter(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CompanyName`
                    FROM `Customers` AS `c`
                    WHERE `c`.`City` = 'London'
                    """);
        }

        public override async Task Select_project_filter2(bool isAsync)
        {
            await base.Select_project_filter2(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`City`
                    FROM `Customers` AS `c`
                    WHERE `c`.`City` = 'London'
                    """);
        }

        public override async Task Select_nested_collection(bool isAsync)
        {
            await base.Select_nested_collection(isAsync);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `o0`.`OrderID`
FROM `Customers` AS `c`
LEFT JOIN (
    SELECT `o`.`OrderID`, `o`.`CustomerID`
    FROM `Orders` AS `o`
    WHERE DATEPART('yyyy', `o`.`OrderDate`) = 1997
) AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE `c`.`City` = 'London'
ORDER BY `c`.`CustomerID`, `o0`.`OrderID`
""");
        }

        public override async Task Select_nested_collection_multi_level(bool isAsync)
        {
            await base.Select_nested_collection_multi_level(isAsync);

            AssertSql(
                """
SELECT [c].[CustomerID], [t0].[Date], [t0].[OrderID]
FROM [Customers] AS [c]
LEFT JOIN (
    SELECT [t].[Date], [t].[OrderID], [t].[CustomerID]
    FROM (
        SELECT [o].[OrderDate] AS [Date], [o].[OrderID], [o].[CustomerID], ROW_NUMBER() OVER(PARTITION BY [o].[CustomerID] ORDER BY [o].[OrderID]) AS [row]
        FROM [Orders] AS [o]
        WHERE [o].[OrderID] < 10500
    ) AS [t]
    WHERE [t].[row] <= 3
) AS [t0] ON [c].[CustomerID] = [t0].[CustomerID]
WHERE [c].[CustomerID] LIKE N'A%'
ORDER BY [c].[CustomerID], [t0].[CustomerID], [t0].[OrderID]
""");
        }

        public override async Task Select_nested_collection_multi_level2(bool isAsync)
        {
            await base.Select_nested_collection_multi_level2(isAsync);

            AssertSql(
                """
SELECT (
    SELECT TOP 1 `o`.`OrderDate`
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` = `o`.`CustomerID` AND `o`.`OrderID` < 10500
    ORDER BY `o`.`OrderID`) AS `OrderDates`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` LIKE 'A%'
ORDER BY `c`.`CustomerID`
""");
        }

        public override async Task Select_nested_collection_multi_level3(bool isAsync)
        {
            await base.Select_nested_collection_multi_level3(isAsync);

            AssertSql(
                """
SELECT (
    SELECT TOP 1 `o`.`OrderDate`
    FROM `Orders` AS `o`
    WHERE `o`.`OrderID` < 10500 AND `c`.`CustomerID` = `o`.`CustomerID`
    ORDER BY `o`.`OrderID`) AS `OrderDates`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` LIKE 'A%'
ORDER BY `c`.`CustomerID`
""");
        }

        public override async Task Select_nested_collection_multi_level4(bool isAsync)
        {
            await base.Select_nested_collection_multi_level4(isAsync);

            AssertSql(
                $"""
                    SELECT IIF((
                            SELECT TOP 1 (
                                SELECT COUNT(*)
                                FROM `Order Details` AS `o0`
                                WHERE `o`.`OrderID` = `o0`.`OrderID` AND `o0`.`OrderID` > 10)
                            FROM `Orders` AS `o`
                            WHERE `c`.`CustomerID` = `o`.`CustomerID` AND `o`.`OrderID` < 10500
                            ORDER BY `o`.`OrderID`) IS NULL, 0, (
                            SELECT TOP 1 (
                                SELECT COUNT(*)
                                FROM `Order Details` AS `o0`
                                WHERE `o`.`OrderID` = `o0`.`OrderID` AND `o0`.`OrderID` > 10)
                            FROM `Orders` AS `o`
                            WHERE `c`.`CustomerID` = `o`.`CustomerID` AND `o`.`OrderID` < 10500
                            ORDER BY `o`.`OrderID`)) AS `Order`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` LIKE 'A%'
                    ORDER BY `c`.`CustomerID`
                    """);
        }

        public override async Task Select_nested_collection_multi_level5(bool isAsync)
        {
            await base.Select_nested_collection_multi_level5(isAsync);

            AssertSql(
"""
SELECT IIF((
        SELECT TOP 1 IIF((
                SELECT TOP 1 `o0`.`ProductID`
                FROM `Order Details` AS `o0`
                WHERE `o`.`OrderID` = `o0`.`OrderID` AND (`o0`.`OrderID` <> (
                    SELECT COUNT(*)
                    FROM `Orders` AS `o1`
                    WHERE `c`.`CustomerID` = `o1`.`CustomerID`) OR (
                    SELECT COUNT(*)
                    FROM `Orders` AS `o1`
                    WHERE `c`.`CustomerID` = `o1`.`CustomerID`) IS NULL)
                ORDER BY `o0`.`OrderID`, `o0`.`ProductID`) IS NULL, 0, (
                SELECT TOP 1 `o0`.`ProductID`
                FROM `Order Details` AS `o0`
                WHERE `o`.`OrderID` = `o0`.`OrderID` AND (`o0`.`OrderID` <> (
                    SELECT COUNT(*)
                    FROM `Orders` AS `o1`
                    WHERE `c`.`CustomerID` = `o1`.`CustomerID`) OR (
                    SELECT COUNT(*)
                    FROM `Orders` AS `o1`
                    WHERE `c`.`CustomerID` = `o1`.`CustomerID`) IS NULL)
                ORDER BY `o0`.`OrderID`, `o0`.`ProductID`))
        FROM `Orders` AS `o`
        WHERE `c`.`CustomerID` = `o`.`CustomerID` AND `o`.`OrderID` < 10500
        ORDER BY `o`.`OrderID`) IS NULL, 0, (
        SELECT TOP 1 IIF((
                SELECT TOP 1 `o0`.`ProductID`
                FROM `Order Details` AS `o0`
                WHERE `o`.`OrderID` = `o0`.`OrderID` AND (`o0`.`OrderID` <> (
                    SELECT COUNT(*)
                    FROM `Orders` AS `o1`
                    WHERE `c`.`CustomerID` = `o1`.`CustomerID`) OR (
                    SELECT COUNT(*)
                    FROM `Orders` AS `o1`
                    WHERE `c`.`CustomerID` = `o1`.`CustomerID`) IS NULL)
                ORDER BY `o0`.`OrderID`, `o0`.`ProductID`) IS NULL, 0, (
                SELECT TOP 1 `o0`.`ProductID`
                FROM `Order Details` AS `o0`
                WHERE `o`.`OrderID` = `o0`.`OrderID` AND (`o0`.`OrderID` <> (
                    SELECT COUNT(*)
                    FROM `Orders` AS `o1`
                    WHERE `c`.`CustomerID` = `o1`.`CustomerID`) OR (
                    SELECT COUNT(*)
                    FROM `Orders` AS `o1`
                    WHERE `c`.`CustomerID` = `o1`.`CustomerID`) IS NULL)
                ORDER BY `o0`.`OrderID`, `o0`.`ProductID`))
        FROM `Orders` AS `o`
        WHERE `c`.`CustomerID` = `o`.`CustomerID` AND `o`.`OrderID` < 10500
        ORDER BY `o`.`OrderID`)) AS `Order`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` LIKE 'A%'
ORDER BY `c`.`CustomerID`
""");
        }

        public override async Task Select_nested_collection_multi_level6(bool isAsync)
        {
            await base.Select_nested_collection_multi_level6(isAsync);

            AssertSql(
                """
    SELECT IIF((
            SELECT TOP 1 IIF((
                    SELECT TOP 1 `o0`.`ProductID`
                    FROM `Order Details` AS `o0`
                    WHERE `o`.`OrderID` = `o0`.`OrderID` AND `o0`.`OrderID` <> IIF(LEN(`c`.`CustomerID`) IS NULL, NULL, CLNG(LEN(`c`.`CustomerID`)))
                    ORDER BY `o0`.`OrderID`, `o0`.`ProductID`) IS NULL, 0, (
                    SELECT TOP 1 `o0`.`ProductID`
                    FROM `Order Details` AS `o0`
                    WHERE `o`.`OrderID` = `o0`.`OrderID` AND `o0`.`OrderID` <> IIF(LEN(`c`.`CustomerID`) IS NULL, NULL, CLNG(LEN(`c`.`CustomerID`)))
                    ORDER BY `o0`.`OrderID`, `o0`.`ProductID`))
            FROM `Orders` AS `o`
            WHERE `c`.`CustomerID` = `o`.`CustomerID` AND `o`.`OrderID` < 10500
            ORDER BY `o`.`OrderID`) IS NULL, 0, (
            SELECT TOP 1 IIF((
                    SELECT TOP 1 `o0`.`ProductID`
                    FROM `Order Details` AS `o0`
                    WHERE `o`.`OrderID` = `o0`.`OrderID` AND `o0`.`OrderID` <> IIF(LEN(`c`.`CustomerID`) IS NULL, NULL, CLNG(LEN(`c`.`CustomerID`)))
                    ORDER BY `o0`.`OrderID`, `o0`.`ProductID`) IS NULL, 0, (
                    SELECT TOP 1 `o0`.`ProductID`
                    FROM `Order Details` AS `o0`
                    WHERE `o`.`OrderID` = `o0`.`OrderID` AND `o0`.`OrderID` <> IIF(LEN(`c`.`CustomerID`) IS NULL, NULL, CLNG(LEN(`c`.`CustomerID`)))
                    ORDER BY `o0`.`OrderID`, `o0`.`ProductID`))
            FROM `Orders` AS `o`
            WHERE `c`.`CustomerID` = `o`.`CustomerID` AND `o`.`OrderID` < 10500
            ORDER BY `o`.`OrderID`)) AS `Order`
    FROM `Customers` AS `c`
    WHERE `c`.`CustomerID` LIKE 'A%'
    ORDER BY `c`.`CustomerID`
    """);
        }

        public override async Task Select_nested_collection_count_using_anonymous_type(bool isAsync)
        {
            await base.Select_nested_collection_count_using_anonymous_type(isAsync);

            AssertSql(
                $"""
                    SELECT (
                        SELECT COUNT(*)
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID`) AS `Count`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` LIKE 'A%'
                    """);
        }

        public override async Task New_date_time_in_anonymous_type_works(bool isAsync)
        {
            await base.New_date_time_in_anonymous_type_works(isAsync);

            AssertSql(
                $"""
                    SELECT 1
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` LIKE 'A%'
                    """);
        }

        public override async Task Select_non_matching_value_types_int_to_long_introduces_explicit_cast(bool isAsync)
        {
            await base.Select_non_matching_value_types_int_to_long_introduces_explicit_cast(isAsync);

            AssertSql(
                $"""
                    SELECT CLNG(`o`.`OrderID`)
                    FROM `Orders` AS `o`
                    WHERE `o`.`CustomerID` = 'ALFKI'
                    ORDER BY `o`.`OrderID`
                    """);
        }

        public override async Task Select_non_matching_value_types_nullable_int_to_long_introduces_explicit_cast(bool isAsync)
        {
            await base.Select_non_matching_value_types_nullable_int_to_long_introduces_explicit_cast(isAsync);

            AssertSql(
                """
    SELECT IIF(`o`.`EmployeeID` IS NULL, NULL, CLNG(`o`.`EmployeeID`))
    FROM `Orders` AS `o`
    WHERE `o`.`CustomerID` = 'ALFKI'
    ORDER BY `o`.`OrderID`
    """);
        }

        public override async Task Select_non_matching_value_types_nullable_int_to_int_doesnt_introduce_explicit_cast(bool isAsync)
        {
            await base.Select_non_matching_value_types_nullable_int_to_int_doesnt_introduce_explicit_cast(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`EmployeeID`
                    FROM `Orders` AS `o`
                    WHERE `o`.`CustomerID` = 'ALFKI'
                    ORDER BY `o`.`OrderID`
                    """);
        }

        public override async Task Select_non_matching_value_types_int_to_nullable_int_doesnt_introduce_explicit_cast(bool isAsync)
        {
            await base.Select_non_matching_value_types_int_to_nullable_int_doesnt_introduce_explicit_cast(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`OrderID`
                    FROM `Orders` AS `o`
                    WHERE `o`.`CustomerID` = 'ALFKI'
                    ORDER BY `o`.`OrderID`
                    """);
        }

        public override async Task Select_non_matching_value_types_from_binary_expression_introduces_explicit_cast(bool isAsync)
        {
            await base.Select_non_matching_value_types_from_binary_expression_introduces_explicit_cast(isAsync);

            AssertSql(
                $"""
                    SELECT CLNG(`o`.`OrderID` + `o`.`OrderID`)
                    FROM `Orders` AS `o`
                    WHERE `o`.`CustomerID` = 'ALFKI'
                    ORDER BY `o`.`OrderID`
                    """);
        }

        public override async Task Select_non_matching_value_types_from_binary_expression_nested_introduces_top_level_explicit_cast(
            bool isAsync)
        {
            await base.Select_non_matching_value_types_from_binary_expression_nested_introduces_top_level_explicit_cast(isAsync);

            AssertSql(
                $"""
                    SELECT CINT(CLNG(`o`.`OrderID`) + CLNG(`o`.`OrderID`))
                    FROM `Orders` AS `o`
                    WHERE `o`.`CustomerID` = 'ALFKI'
                    ORDER BY `o`.`OrderID`
                    """);
        }

        public override async Task Select_non_matching_value_types_from_unary_expression_introduces_explicit_cast1(bool isAsync)
        {
            await base.Select_non_matching_value_types_from_unary_expression_introduces_explicit_cast1(isAsync);

            AssertSql(
                $"""
                    SELECT CLNG(-`o`.`OrderID`)
                    FROM `Orders` AS `o`
                    WHERE `o`.`CustomerID` = 'ALFKI'
                    ORDER BY `o`.`OrderID`
                    """);
        }

        public override async Task Select_non_matching_value_types_from_unary_expression_introduces_explicit_cast2(bool isAsync)
        {
            await base.Select_non_matching_value_types_from_unary_expression_introduces_explicit_cast2(isAsync);

            AssertSql(
                $"""
                    SELECT -CLNG(`o`.`OrderID`)
                    FROM `Orders` AS `o`
                    WHERE `o`.`CustomerID` = 'ALFKI'
                    ORDER BY `o`.`OrderID`
                    """);
        }

        public override async Task Select_non_matching_value_types_from_length_introduces_explicit_cast(bool isAsync)
        {
            await base.Select_non_matching_value_types_from_length_introduces_explicit_cast(isAsync);

            AssertSql(
                """
    SELECT CLNG(IIF(LEN(`o`.`CustomerID`) IS NULL, NULL, CLNG(LEN(`o`.`CustomerID`))))
    FROM `Orders` AS `o`
    WHERE `o`.`CustomerID` = 'ALFKI'
    ORDER BY `o`.`OrderID`
    """);
        }

        public override async Task Select_non_matching_value_types_from_method_call_introduces_explicit_cast(bool isAsync)
        {
            await base.Select_non_matching_value_types_from_method_call_introduces_explicit_cast(isAsync);

            AssertSql(
                """
    SELECT IIF(ABS(`o`.`OrderID`) IS NULL, NULL, CLNG(ABS(`o`.`OrderID`)))
    FROM `Orders` AS `o`
    WHERE `o`.`CustomerID` = 'ALFKI'
    ORDER BY `o`.`OrderID`
    """);
        }

        public override async Task Select_non_matching_value_types_from_anonymous_type_introduces_explicit_cast(bool isAsync)
        {
            await base.Select_non_matching_value_types_from_anonymous_type_introduces_explicit_cast(isAsync);

            AssertSql(
                $"""
                    SELECT CLNG(`o`.`OrderID`) AS `LongOrder`, CINT(`o`.`OrderID`) AS `ShortOrder`, `o`.`OrderID` AS `Order`
                    FROM `Orders` AS `o`
                    WHERE `o`.`CustomerID` = 'ALFKI'
                    ORDER BY `o`.`OrderID`
                    """);
        }

        public override async Task Select_conditional_with_null_comparison_in_test(bool isAsync)
        {
            await base.Select_conditional_with_null_comparison_in_test(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(`o`.`CustomerID` IS NULL, TRUE, IIF(`o`.`OrderID` < 100, TRUE, FALSE))
                    FROM `Orders` AS `o`
                    WHERE `o`.`CustomerID` = 'ALFKI'
                    """);
        }

        public override async Task Select_over_10_nested_ternary_condition(bool isAsync)
        {
            await base.Select_over_10_nested_ternary_condition(isAsync);

            AssertSql(
"""
SELECT IIF(`c`.`CustomerID` = '1', '01', IIF(`c`.`CustomerID` = '2', '02', IIF(`c`.`CustomerID` = '3', '03', IIF(`c`.`CustomerID` = '4', '04', IIF(`c`.`CustomerID` = '5', '05', IIF(`c`.`CustomerID` = '6', '06', IIF(`c`.`CustomerID` = '7', '07', IIF(`c`.`CustomerID` = '8', '08', IIF(`c`.`CustomerID` = '9', '09', IIF(`c`.`CustomerID` = '10', '10', IIF(`c`.`CustomerID` = '11', '11', NULL)))))))))))
FROM `Customers` AS `c`
""");
        }

        public override async Task Select_conditional_drops_false(bool isAsync)
        {
            await base.Select_conditional_drops_false(isAsync);

            AssertSql(
                """
SELECT IIF((`o`.`OrderID` MOD 2) = 0, `o`.`OrderID`, -`o`.`OrderID`)
FROM `Orders` AS `o`
""");
        }

        public override async Task Select_conditional_terminates_at_true(bool isAsync)
        {
            await base.Select_conditional_terminates_at_true(isAsync);

            AssertSql(
                """
SELECT IIF((`o`.`OrderID` MOD 2) = 0, `o`.`OrderID`, 0)
FROM `Orders` AS `o`
""");
        }

        public override async Task Select_conditional_flatten_nested_results(bool isAsync)
        {
            await base.Select_conditional_flatten_nested_results(isAsync);

            AssertSql(
                """
SELECT IIF((`o`.`OrderID` MOD 2) = 0 AND (`o`.`OrderID` MOD 5) = 0, -`o`.`OrderID`, `o`.`OrderID`)
FROM `Orders` AS `o`
""");
        }

        public override async Task Select_conditional_flatten_nested_tests(bool isAsync)
        {
            await base.Select_conditional_flatten_nested_tests(isAsync);

            AssertSql(
                """
SELECT IIF((`o`.`OrderID` MOD 2) <> 0, `o`.`OrderID`, -`o`.`OrderID`)
FROM `Orders` AS `o`
""");
        }

        public override async Task Projection_in_a_subquery_should_be_liftable(bool isAsync)
        {
            await base.Projection_in_a_subquery_should_be_liftable(isAsync);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@__p_0='1'")}
                    
                    SELECT `e`.`EmployeeID`
                    FROM `Employees` AS `e`
                    ORDER BY `e`.`EmployeeID`
                    SKIP {AssertSqlHelper.Parameter("@__p_0")}
                    """);
        }

        public override async Task Projection_containing_DateTime_subtraction(bool isAsync)
        {
            await base.Projection_containing_DateTime_subtraction(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`OrderDate`
                    FROM `Orders` AS `o`
                    WHERE `o`.`OrderID` < 10300
                    """);
        }

        [ConditionalTheory(Skip = "`SELECT (SELECT TOP 1) FROM` is not supported by Jet.")]
        public override async Task Project_single_element_from_collection_with_OrderBy_Take_and_FirstOrDefault(bool isAsync)
        {
            await base.Project_single_element_from_collection_with_OrderBy_Take_and_FirstOrDefault(isAsync);

            AssertSql(
                $"""
                    SELECT (
                        SELECT TOP 1 `t`.`CustomerID`
                        FROM (
                            SELECT TOP 1 `o`.`CustomerID`, `o`.`OrderID`
                            FROM `Orders` AS `o`
                            WHERE `c`.`CustomerID` = `o`.`CustomerID`
                            ORDER BY `o`.`OrderID`
                        ) AS `t`
                        ORDER BY `t`.`OrderID`)
                    FROM `Customers` AS `c`
                    """);
        }

        [ConditionalTheory(Skip = "`SELECT (SELECT TOP 1) FROM` is not supported by Jet.")]
        public override async Task Project_single_element_from_collection_with_OrderBy_Skip_and_FirstOrDefault(bool isAsync)
        {
            await base.Project_single_element_from_collection_with_OrderBy_Skip_and_FirstOrDefault(isAsync);

            AssertSql(
                $"""
                    SELECT (
                        SELECT `o`.`CustomerID`
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID`
                        ORDER BY `o`.`OrderID`
                        SKIP 1 FETCH NEXT 1 ROWS ONLY)
                    FROM `Customers` AS `c`
                    """);
        }

        public override async Task Project_single_element_from_collection_with_OrderBy_Distinct_and_FirstOrDefault(bool isAsync)
        {
            await base.Project_single_element_from_collection_with_OrderBy_Distinct_and_FirstOrDefault(isAsync);

            AssertSql(
                """
SELECT (
    SELECT TOP 1 `o`.`CustomerID`
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` = `o`.`CustomerID`)
FROM `Customers` AS `c`
""");
        }

        [ConditionalTheory(Skip = "`SELECT (SELECT TOP 1) FROM` is not supported by Jet.")]
        public override async Task
            Project_single_element_from_collection_with_OrderBy_Distinct_and_FirstOrDefault_followed_by_projecting_length(bool isAsync)
        {
            await base.Project_single_element_from_collection_with_OrderBy_Distinct_and_FirstOrDefault_followed_by_projecting_length(
                isAsync);

            AssertSql(
                $"""
                    SELECT (
                        SELECT TOP 1 CAST(LEN(`t`.`CustomerID`) AS int)
                        FROM (
                            SELECT DISTINCT `o`.`CustomerID`
                            FROM `Orders` AS `o`
                            WHERE `c`.`CustomerID` = `o`.`CustomerID`
                        ) AS `t`)
                    FROM `Customers` AS `c`
                    """);
        }

        [ConditionalTheory(Skip = "`SELECT (SELECT TOP 1) FROM` is not supported by Jet.")]
        public override async Task Project_single_element_from_collection_with_OrderBy_Take_and_SingleOrDefault(bool isAsync)
        {
            await base.Project_single_element_from_collection_with_OrderBy_Take_and_SingleOrDefault(isAsync);

            AssertSql(
                $"""
                    SELECT (
                        SELECT TOP 1 `t`.`CustomerID`
                        FROM (
                            SELECT TOP 1 `o`.`CustomerID`, `o`.`OrderID`
                            FROM `Orders` AS `o`
                            WHERE `c`.`CustomerID` = `o`.`CustomerID`
                            ORDER BY `o`.`OrderID`
                        ) AS `t`
                        ORDER BY `t`.`OrderID`)
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` = 'ALFKI'
                    """);
        }

        [ConditionalTheory(Skip = "`SELECT (SELECT TOP 1) FROM` is not supported by Jet.")]
        public override async Task Project_single_element_from_collection_with_OrderBy_Take_and_FirstOrDefault_with_parameter(bool isAsync)
        {
            await base.Project_single_element_from_collection_with_OrderBy_Take_and_FirstOrDefault_with_parameter(isAsync);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@__i_0='1'")}
                    
                    SELECT (
                        SELECT TOP 1 `t`.`CustomerID`
                        FROM (
                            SELECT TOP {AssertSqlHelper.Parameter("@__i_0")} `o`.`CustomerID`, `o`.`OrderID`
                            FROM `Orders` AS `o`
                            WHERE `c`.`CustomerID` = `o`.`CustomerID`
                            ORDER BY `o`.`OrderID`
                        ) AS `t`
                        ORDER BY `t`.`OrderID`)
                    FROM `Customers` AS `c`
                    """);
        }

        [ConditionalTheory(Skip = "`SELECT (SELECT TOP 1) FROM` is not supported by Jet.")]
        public override async Task Project_single_element_from_collection_with_multiple_OrderBys_Take_and_FirstOrDefault(bool isAsync)
        {
            await base.Project_single_element_from_collection_with_multiple_OrderBys_Take_and_FirstOrDefault(isAsync);

            AssertSql(
                $"""
                    SELECT (
                        SELECT TOP 1 `t`.`CustomerID`
                        FROM (
                            SELECT TOP 2 `o`.`CustomerID`, `o`.`OrderID`, `o`.`OrderDate`
                            FROM `Orders` AS `o`
                            WHERE `c`.`CustomerID` = `o`.`CustomerID`
                            ORDER BY `o`.`OrderID`, `o`.`OrderDate` DESC
                        ) AS `t`
                        ORDER BY `t`.`OrderID`, `t`.`OrderDate` DESC)
                    FROM `Customers` AS `c`
                    """);
        }

        public override async Task
            Project_single_element_from_collection_with_multiple_OrderBys_Take_and_FirstOrDefault_followed_by_projection_of_length_property(
                bool isAsync)
        {
            await base
                .Project_single_element_from_collection_with_multiple_OrderBys_Take_and_FirstOrDefault_followed_by_projection_of_length_property(
                    isAsync);

            AssertSql(
                """
SELECT (
    SELECT TOP 1 IIF(LEN(`o`.`CustomerID`) IS NULL, NULL, CLNG(LEN(`o`.`CustomerID`)))
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` = `o`.`CustomerID`
    ORDER BY `o`.`OrderID`, `o`.`OrderDate` DESC)
FROM `Customers` AS `c`
""");
        }

        [ConditionalTheory(Skip = "`SELECT (SELECT TOP 1) FROM` is not supported by Jet.")]
        public override async Task Project_single_element_from_collection_with_multiple_OrderBys_Take_and_FirstOrDefault_2(bool isAsync)
        {
            await base.Project_single_element_from_collection_with_multiple_OrderBys_Take_and_FirstOrDefault_2(isAsync);

            AssertSql(
                $"""
                    SELECT (
                        SELECT TOP 1 `t`.`CustomerID`
                        FROM (
                            SELECT TOP 2 `o`.`CustomerID`, `o`.`OrderID`, `o`.`OrderDate`
                            FROM `Orders` AS `o`
                            WHERE `c`.`CustomerID` = `o`.`CustomerID`
                            ORDER BY `o`.`CustomerID`, `o`.`OrderDate` DESC
                        ) AS `t`
                        ORDER BY `t`.`CustomerID`, `t`.`OrderDate` DESC)
                    FROM `Customers` AS `c`
                    """);
        }

        [ConditionalTheory(Skip = "`SELECT (SELECT TOP 1) FROM` is not supported by Jet.")]
        public override async Task Project_single_element_from_collection_with_OrderBy_over_navigation_Take_and_FirstOrDefault(bool isAsync)
        {
            await base.Project_single_element_from_collection_with_OrderBy_over_navigation_Take_and_FirstOrDefault(isAsync);

            AssertSql(
                $"""
                    SELECT (
                        SELECT TOP 1 `t`.`OrderID`
                        FROM (
                            SELECT TOP 1 `o`.`OrderID`, `o`.`ProductID`, `p`.`ProductID` AS `ProductID0`, `p`.`ProductName`
                            FROM `Order Details` AS `o`
                            INNER JOIN `Products` AS `p` ON `o`.`ProductID` = `p`.`ProductID`
                            WHERE `o0`.`OrderID` = `o`.`OrderID`
                            ORDER BY `p`.`ProductName`
                        ) AS `t`
                        ORDER BY `t`.`ProductName`)
                    FROM `Orders` AS `o0`
                    WHERE `o0`.`OrderID` < 10300
                    """);
        }

        public override async Task Project_single_element_from_collection_with_OrderBy_over_navigation_Take_and_FirstOrDefault_2(
            bool isAsync)
        {
            await base.Project_single_element_from_collection_with_OrderBy_over_navigation_Take_and_FirstOrDefault_2(isAsync);

            AssertSql(
                $"""
                    SELECT `t0`.`OrderID`, `t0`.`ProductID`, `t0`.`Discount`, `t0`.`Quantity`, `t0`.`UnitPrice`
                    FROM `Orders` AS `o`
                    OUTER APPLY (
                        SELECT TOP 1 `t`.`OrderID`, `t`.`ProductID`, `t`.`Discount`, `t`.`Quantity`, `t`.`UnitPrice`, `t`.`ProductID0`, `t`.`ProductName`
                        FROM (
                            SELECT TOP 1 `o0`.`OrderID`, `o0`.`ProductID`, `o0`.`Discount`, `o0`.`Quantity`, `o0`.`UnitPrice`, `p`.`ProductID` AS `ProductID0`, `p`.`ProductName`
                            FROM `Order Details` AS `o0`
                            INNER JOIN `Products` AS `p` ON `o0`.`ProductID` = `p`.`ProductID`
                            WHERE `o`.`OrderID` = `o0`.`OrderID`
                            ORDER BY `p`.`ProductName`
                        ) AS `t`
                        ORDER BY `t`.`ProductName`
                    ) AS `t0`
                    WHERE `o`.`OrderID` < 10250
                    """);
        }

        public override async Task Select_datetime_year_component(bool isAsync)
        {
            await base.Select_datetime_year_component(isAsync);

            AssertSql(
                $"""
                    SELECT DATEPART('yyyy', `o`.`OrderDate`)
                    FROM `Orders` AS `o`
                    """);
        }

        public override async Task Select_datetime_month_component(bool isAsync)
        {
            await base.Select_datetime_month_component(isAsync);

            AssertSql(
                $"""
                    SELECT DATEPART('m', `o`.`OrderDate`)
                    FROM `Orders` AS `o`
                    """);
        }

        public override async Task Select_datetime_day_of_year_component(bool isAsync)
        {
            await base.Select_datetime_day_of_year_component(isAsync);

            AssertSql(
                $"""
                    SELECT DATEPART('y', `o`.`OrderDate`)
                    FROM `Orders` AS `o`
                    """);
        }

        public override async Task Select_datetime_day_component(bool isAsync)
        {
            await base.Select_datetime_day_component(isAsync);

            AssertSql(
                $"""
                    SELECT DATEPART('d', `o`.`OrderDate`)
                    FROM `Orders` AS `o`
                    """);
        }

        public override async Task Select_datetime_hour_component(bool isAsync)
        {
            await base.Select_datetime_hour_component(isAsync);

            AssertSql(
                $"""
                    SELECT DATEPART('h', `o`.`OrderDate`)
                    FROM `Orders` AS `o`
                    """);
        }

        public override async Task Select_datetime_minute_component(bool isAsync)
        {
            await base.Select_datetime_minute_component(isAsync);

            AssertSql(
                $"""
                    SELECT DATEPART('n', `o`.`OrderDate`)
                    FROM `Orders` AS `o`
                    """);
        }

        public override async Task Select_datetime_second_component(bool isAsync)
        {
            await base.Select_datetime_second_component(isAsync);

            AssertSql(
                $"""
                    SELECT DATEPART('s', `o`.`OrderDate`)
                    FROM `Orders` AS `o`
                    """);
        }

        public override async Task Select_datetime_millisecond_component(bool isAsync)
        {
            await base.Select_datetime_millisecond_component(isAsync);

            AssertSql(
"""
SELECT `o`.`OrderDate`
FROM `Orders` AS `o`
""");
        }

        public override async Task Select_byte_constant(bool isAsync)
        {
            await base.Select_byte_constant(isAsync);

            AssertSql(
                """
SELECT IIF(`c`.`CustomerID` = 'ALFKI', CBYTE(1), CBYTE(2))
FROM `Customers` AS `c`
""");
        }

        public override async Task Select_short_constant(bool isAsync)
        {
            await base.Select_short_constant(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(`c`.`CustomerID` = 'ALFKI', 1, 2)
                    FROM `Customers` AS `c`
                    """);
        }

        public override async Task Select_bool_constant(bool isAsync)
        {
            await base.Select_bool_constant(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(`c`.`CustomerID` = 'ALFKI', TRUE, FALSE)
                    FROM `Customers` AS `c`
                    """);
        }

        public override async Task Anonymous_projection_AsNoTracking_Selector(bool isAsync)
        {
            await base.Anonymous_projection_AsNoTracking_Selector(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`OrderDate`
                    FROM `Orders` AS `o`
                    """);
        }

        public override async Task Anonymous_projection_with_repeated_property_being_ordered(bool isAsync)
        {
            await base.Anonymous_projection_with_repeated_property_being_ordered(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID` AS `A`
                    FROM `Customers` AS `c`
                    ORDER BY `c`.`CustomerID`
                    """);
        }

        public override async Task Anonymous_projection_with_repeated_property_being_ordered_2(bool isAsync)
        {
            await base.Anonymous_projection_with_repeated_property_being_ordered_2(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID` AS `A`, `o`.`CustomerID` AS `B`
                    FROM `Orders` AS `o`
                    LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
                    ORDER BY `o`.`CustomerID`
                    """);
        }

        public override async Task Select_GetValueOrDefault_on_DateTime(bool isAsync)
        {
            await base.Select_GetValueOrDefault_on_DateTime(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`OrderDate`
                    FROM `Orders` AS `o`
                    """);
        }

        public override async Task Select_GetValueOrDefault_on_DateTime_with_null_values(bool isAsync)
        {
            await base.Select_GetValueOrDefault_on_DateTime_with_null_values(isAsync);

            AssertSql(
                """
                    SELECT `o`.`OrderDate`
                    FROM `Customers` AS `c`
                    LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
                    """);
        }

        public override async Task Cast_on_top_level_projection_brings_explicit_Cast(bool isAsync)
        {
            await base.Cast_on_top_level_projection_brings_explicit_Cast(isAsync);

            AssertSql(
                $"""
                    SELECT CDBL(`o`.`OrderID`)
                    FROM `Orders` AS `o`
                    """);
        }

        public override async Task Projecting_nullable_struct(bool isAsync)
        {
            await base.Projecting_nullable_struct(isAsync);

            AssertSql(
"""
SELECT `o`.`CustomerID`, IIF(`o`.`CustomerID` = 'ALFKI' AND `o`.`CustomerID` IS NOT NULL, TRUE, FALSE), `o`.`OrderID`, IIF(LEN(`o`.`CustomerID`) IS NULL, NULL, CLNG(LEN(`o`.`CustomerID`)))
FROM `Orders` AS `o`
""");
        }

        public override async Task Multiple_select_many_with_predicate(bool isAsync)
        {
            await base.Multiple_select_many_with_predicate(isAsync);

            AssertSql(
                """
    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM (`Customers` AS `c`
    INNER JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`)
    LEFT JOIN `Order Details` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`
    WHERE (CDBL(`o0`.`Discount`) >= 0.25) AND (`o`.`OrderID` IS NOT NULL AND `o0`.`OrderID` IS NOT NULL)
    """);
        }

        public override async Task SelectMany_without_result_selector_naked_collection_navigation(bool isAsync)
        {
            await base.SelectMany_without_result_selector_naked_collection_navigation(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
                    FROM `Customers` AS `c`
                    INNER JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
                    """);
        }

        public override async Task SelectMany_without_result_selector_collection_navigation_composed(bool isAsync)
        {
            await base.SelectMany_without_result_selector_collection_navigation_composed(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`CustomerID`
                    FROM `Customers` AS `c`
                    INNER JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
                    """);
        }

        public override async Task SelectMany_correlated_with_outer_1(bool isAsync)
        {
            await base.SelectMany_correlated_with_outer_1(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `t`.`City` AS `o`
                    FROM `Customers` AS `c`
                    CROSS APPLY (
                        SELECT `c`.`City`, `o`.`OrderID`, `o`.`CustomerID`
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID`
                    ) AS `t`
                    """);
        }

        public override async Task SelectMany_correlated_with_outer_2(bool isAsync)
        {
            await base.SelectMany_correlated_with_outer_2(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `t`.`OrderID`, `t`.`CustomerID`, `t`.`EmployeeID`, `t`.`OrderDate`
                    FROM `Customers` AS `c`
                    CROSS APPLY (
                        SELECT TOP 2 `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `c`.`City`
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID`
                        ORDER BY `c`.`City`, `o`.`OrderID`
                    ) AS `t`
                    """);
        }

        public override async Task SelectMany_correlated_with_outer_3(bool isAsync)
        {
            await base.SelectMany_correlated_with_outer_3(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `t`.`City` AS `o`
                    FROM `Customers` AS `c`
                    OUTER APPLY (
                        SELECT `c`.`City`, `o`.`OrderID`, `o`.`CustomerID`
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID`
                    ) AS `t`
                    """);
        }

        public override async Task SelectMany_correlated_with_outer_4(bool isAsync)
        {
            await base.SelectMany_correlated_with_outer_4(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `t`.`OrderID`, `t`.`CustomerID`, `t`.`EmployeeID`, `t`.`OrderDate`
                    FROM `Customers` AS `c`
                    OUTER APPLY (
                        SELECT TOP 2 `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `c`.`City`
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID`
                        ORDER BY `c`.`City`, `o`.`OrderID`
                    ) AS `t`
                    """);
        }

        public override async Task SelectMany_correlated_with_outer_5(bool async)
        {
            await base.SelectMany_correlated_with_outer_5(async);

            AssertSql(
    """
SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region], [t].[City] AS [o]
FROM [Customers] AS [c]
OUTER APPLY (
    SELECT [c].[City]
    FROM [Orders] AS [o]
    WHERE [c].[CustomerID] <> [o].[CustomerID] OR [o].[CustomerID] IS NULL
) AS [t]
""");
        }

        public override async Task SelectMany_correlated_with_outer_6(bool async)
        {
            await base.SelectMany_correlated_with_outer_6(async);

            AssertSql(
    """
SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region], [t].[OrderID], [t].[CustomerID], [t].[EmployeeID], [t].[OrderDate]
FROM [Customers] AS [c]
OUTER APPLY (
    SELECT TOP(2) [o].[OrderID], [o].[CustomerID], [o].[EmployeeID], [o].[OrderDate]
    FROM [Orders] AS [o]
    WHERE [c].[CustomerID] <> [o].[CustomerID] OR [o].[CustomerID] IS NULL
    ORDER BY [c].[City], [o].[OrderID]
) AS [t]
""");
        }

        public override async Task SelectMany_correlated_with_outer_7(bool async)
        {
            await base.SelectMany_correlated_with_outer_7(async);

            AssertSql(
    """
SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region], [t].[OrderID], [t].[CustomerID], [t].[EmployeeID], [t].[OrderDate]
FROM [Customers] AS [c]
OUTER APPLY (
    SELECT TOP(2) [o].[OrderID], [o].[CustomerID], [o].[EmployeeID], [o].[OrderDate]
    FROM [Orders] AS [o]
    WHERE CAST(LEN([c].[CustomerID]) AS int) >= CAST(LEN([o].[CustomerID]) AS int)
    ORDER BY [c].[City], [o].[OrderID]
) AS [t]
""");
        }

        public override async Task SelectMany_with_multiple_Take(bool async)
        {
            await base.SelectMany_with_multiple_Take(async);

            AssertSql(
                """
SELECT [o1].[OrderID], [o1].[CustomerID], [o1].[EmployeeID], [o1].[OrderDate]
FROM [Customers] AS [c]
INNER JOIN (
    SELECT [o0].[OrderID], [o0].[CustomerID], [o0].[EmployeeID], [o0].[OrderDate]
    FROM (
        SELECT [o].[OrderID], [o].[CustomerID], [o].[EmployeeID], [o].[OrderDate], ROW_NUMBER() OVER(PARTITION BY [o].[CustomerID] ORDER BY [o].[OrderID]) AS [row]
        FROM [Orders] AS [o]
    ) AS [o0]
    WHERE [o0].[row] <= 3
) AS [o1] ON [c].[CustomerID] = [o1].[CustomerID]
""");
        }

        /*public override async Task SelectMany_with_nested_DefaultIfEmpty(bool async)
        {
            await base.SelectMany_with_nested_DefaultIfEmpty(async);

            AssertSql(
                """
SELECT [s].[OrderID], [s].[ProductID], [s].[Discount], [s].[Quantity], [s].[UnitPrice]
FROM [Customers] AS [c]
INNER JOIN (
    SELECT [o0].[OrderID], [o0].[ProductID], [o0].[Discount], [o0].[Quantity], [o0].[UnitPrice], [o].[CustomerID]
    FROM [Orders] AS [o]
    LEFT JOIN [Order Details] AS [o0] ON [o].[OrderID] = [o0].[OrderID]
    WHERE 0 = 1
) AS [s] ON [c].[CustomerID] = [s].[CustomerID]
""");
        }*/

        public override async Task Select_with_multiple_Take(bool async)
        {
            await base.Select_with_multiple_Take(async);

            AssertSql(
                """
SELECT TOP @p0 `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
FROM (
    SELECT TOP @p `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    ORDER BY `c`.`CustomerID`
) AS `c0`
ORDER BY `c0`.`CustomerID`
""");
        }

        public override async Task FirstOrDefault_over_empty_collection_of_value_type_returns_correct_results(bool isAsync)
        {
            await base.FirstOrDefault_over_empty_collection_of_value_type_returns_correct_results(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, IIF((
                            SELECT TOP 1 `o`.`OrderID`
                            FROM `Orders` AS `o`
                            WHERE `c`.`CustomerID` = `o`.`CustomerID`
                            ORDER BY `o`.`OrderID`) IS NULL, 0, (
                            SELECT TOP 1 `o`.`OrderID`
                            FROM `Orders` AS `o`
                            WHERE `c`.`CustomerID` = `o`.`CustomerID`
                            ORDER BY `o`.`OrderID`)) AS `OrderId`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` = 'FISSA'
                    """);
        }

        public override async Task Project_non_nullable_value_after_FirstOrDefault_on_empty_collection(bool async)
        {
            await base.Project_non_nullable_value_after_FirstOrDefault_on_empty_collection(async);

            AssertSql(
"""
SELECT (
    SELECT TOP 1 IIF(LEN(`o`.`CustomerID`) IS NULL, NULL, CLNG(LEN(`o`.`CustomerID`)))
    FROM `Orders` AS `o`
    WHERE `o`.`CustomerID` = 'John Doe')
FROM `Customers` AS `c`
""");
        }

        public override Task Member_binding_after_ctor_arguments_fails_with_client_eval(bool async)
            => AssertTranslationFailed(() => base.Member_binding_after_ctor_arguments_fails_with_client_eval(async));

        public override async Task Filtered_collection_projection_is_tracked(bool isAsync)
        {
            await base.Filtered_collection_projection_is_tracked(isAsync);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM `Customers` AS `c`
LEFT JOIN (
    SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
    FROM `Orders` AS `o`
    WHERE `o`.`OrderID` > 11000
) AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE `c`.`CustomerID` LIKE 'A%'
ORDER BY `c`.`CustomerID`
""");
        }

        public override async Task Filtered_collection_projection_with_to_list_is_tracked(bool isAsync)
        {
            await base.Filtered_collection_projection_with_to_list_is_tracked(isAsync);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM `Customers` AS `c`
LEFT JOIN (
    SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
    FROM `Orders` AS `o`
    WHERE `o`.`OrderID` > 11000
) AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE `c`.`CustomerID` LIKE 'A%'
ORDER BY `c`.`CustomerID`
""");
        }

        public override async Task SelectMany_with_collection_being_correlated_subquery_which_references_inner_and_outer_entity(
            bool isAsync)
        {
            await base.SelectMany_with_collection_being_correlated_subquery_which_references_inner_and_outer_entity(isAsync);

            AssertSql(
                $"""
                    SELECT `t`.`CustomerID` AS `OrderProperty`, `t`.`CustomerID0` AS `CustomerProperty`
                    FROM `Customers` AS `c`
                    CROSS APPLY (
                        SELECT `o`.`CustomerID`, `c`.`CustomerID` AS `CustomerID0`, `o`.`OrderID`
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID`
                    ) AS `t`
                    """);
        }

        public override async Task
            SelectMany_with_collection_being_correlated_subquery_which_references_non_mapped_properties_from_inner_and_outer_entity(
                bool isAsync)
        {
            await AssertUnableToTranslateEFProperty(
                () => base
                    .SelectMany_with_collection_being_correlated_subquery_which_references_non_mapped_properties_from_inner_and_outer_entity(
                        isAsync));

            AssertSql();
        }

        public override async Task Select_with_complex_expression_that_can_be_funcletized(bool isAsync)
        {
            await base.Select_with_complex_expression_that_can_be_funcletized(isAsync);

            AssertSql(
                """
SELECT IIF(`c`.`Region` IS NOT NULL, 0, NULL)
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = 'ALFKI'
""");
        }

        public override async Task Select_chained_entity_navigation_doesnt_materialize_intermittent_entities(bool isAsync)
        {
            await base.Select_chained_entity_navigation_doesnt_materialize_intermittent_entities(isAsync);

            AssertSql(
                """
SELECT `o`.`OrderID`, `c`.`CustomerID`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM (`Orders` AS `o`
LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`)
LEFT JOIN `Orders` AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
ORDER BY `o`.`OrderID`, `c`.`CustomerID`
""");
        }

        public override async Task Select_entity_compared_to_null(bool isAsync)
        {
            await base.Select_entity_compared_to_null(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(`c`.`CustomerID` IS NULL, TRUE, FALSE)
                    FROM `Orders` AS `o`
                    LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
                    WHERE `o`.`CustomerID` = 'ALFKI'
                    """);
        }

        public override async Task Explicit_cast_in_arithmetic_operation_is_preserved(bool async)
        {
            await base.Explicit_cast_in_arithmetic_operation_is_preserved(async);

            AssertSql(
                """
SELECT CAST([o].[OrderID] AS decimal(18,2)) / CAST([o].[OrderID] + 1000 AS decimal(18,2))
FROM [Orders] AS [o]
WHERE [o].[OrderID] = 10243
""");
        }

        public override async Task SelectMany_whose_selector_references_outer_source(bool isAsync)
        {
            await base.SelectMany_whose_selector_references_outer_source(isAsync);

            AssertSql(
                $"""
                    SELECT `t`.`OrderDate`, `t`.`City` AS `CustomerCity`
                    FROM `Customers` AS `c`
                    CROSS APPLY (
                        SELECT `o`.`OrderDate`, `c`.`City`, `o`.`OrderID`, `o`.`CustomerID`
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID`
                    ) AS `t`
                    """);
        }

        public override async Task Collection_FirstOrDefault_with_entity_equality_check_in_projection(bool isAsync)
        {
            await base.Collection_FirstOrDefault_with_entity_equality_check_in_projection(isAsync);

            AssertSql(
"""
SELECT IIF(NOT EXISTS (
        SELECT 1
        FROM `Orders` AS `o`
        WHERE `c`.`CustomerID` = `o`.`CustomerID`) OR NOT EXISTS (
        SELECT 1
        FROM `Orders` AS `o0`
        WHERE `c`.`CustomerID` = `o0`.`CustomerID`), TRUE, FALSE)
FROM `Customers` AS `c`
""");
        }

        public override async Task Collection_FirstOrDefault_with_nullable_unsigned_int_column(bool isAsync)
        {
            await base.Collection_FirstOrDefault_with_nullable_unsigned_int_column(isAsync);

            AssertSql(
                $"""
                    SELECT (
                        SELECT TOP 1 `o`.`EmployeeID`
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID`
                        ORDER BY `o`.`OrderID`)
                    FROM `Customers` AS `c`
                    """);
        }

        public override async Task ToList_Count_in_projection_works(bool isAsync)
        {
            await base.ToList_Count_in_projection_works(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, (
                        SELECT COUNT(*)
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID`) AS `Count`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` LIKE 'A%'
                    """);
        }

        public override async Task LastOrDefault_member_access_in_projection_translates_to_server(bool isAsync)
        {
            await base.LastOrDefault_member_access_in_projection_translates_to_server(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, (
                        SELECT TOP 1 `o`.`OrderDate`
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID`
                        ORDER BY `o`.`OrderID`) AS `OrderDate`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` LIKE 'A%'
                    """);
        }

        public override async Task Projection_with_parameterized_constructor(bool async)
        {
            await base.Projection_with_parameterized_constructor(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` = 'ALFKI'
                    """);
        }

        public override async Task Projection_with_parameterized_constructor_with_member_assignment(bool async)
        {
            await base.Projection_with_parameterized_constructor_with_member_assignment(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` = 'ALFKI'
                    """);
        }

        public override async Task Collection_projection_AsNoTracking_OrderBy(bool async)
        {
            await base.Collection_projection_AsNoTracking_OrderBy(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `o`.`OrderDate`, `o`.`OrderID`
                    FROM `Customers` AS `c`
                    LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
                    ORDER BY `c`.`CustomerID`
                    """);
        }

        public override async Task Coalesce_over_nullable_uint(bool async)
        {
            await base.Coalesce_over_nullable_uint(async);

            AssertSql(
                """
                    SELECT IIF(`o`.`EmployeeID` IS NULL, 0, `o`.`EmployeeID`)
                    FROM `Orders` AS `o`
                    """);
        }

        public override async Task Project_uint_through_collection_FirstOrDefault(bool async)
        {
            await base.Project_uint_through_collection_FirstOrDefault(async);

            AssertSql(
                """
                    SELECT (
                        SELECT TOP 1 `o`.`EmployeeID`
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID`
                        ORDER BY `o`.`OrderID`)
                    FROM `Customers` AS `c`
                    """);
        }

        public override async Task Project_keyless_entity_FirstOrDefault_without_orderby(bool async)
        {
            await base.Project_keyless_entity_FirstOrDefault_without_orderby(async);

            AssertSql(
                """
                    SELECT `t0`.`Address`, `t0`.`City`, `t0`.`CompanyName`, `t0`.`ContactName`, `t0`.`ContactTitle`
                    FROM `Customers` AS `c`
                    LEFT JOIN (
                        SELECT `t`.`Address`, `t`.`City`, `t`.`CompanyName`, `t`.`ContactName`, `t`.`ContactTitle`
                        FROM (
                            SELECT `m`.`Address`, `m`.`City`, `m`.`CompanyName`, `m`.`ContactName`, `m`.`ContactTitle`, ROW_NUMBER() OVER(PARTITION BY `m`.`CompanyName` ORDER BY (SELECT 1)) AS `row`
                            FROM (
                                SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region` FROM `Customers` AS `c`
                            ) AS `m`
                        ) AS `t`
                        WHERE `t`.`row` <= 1
                    ) AS `t0` ON `c`.`CompanyName` = `t0`.`CompanyName`
                    """);
        }

        public override async Task Reverse_changes_asc_order_to_desc(bool async)
        {
            await base.Reverse_changes_asc_order_to_desc(async);

            AssertSql(
                """
                    SELECT `e`.`EmployeeID`
                    FROM `Employees` AS `e`
                    ORDER BY `e`.`EmployeeID` DESC
                    """);
        }

        public override async Task Reverse_changes_desc_order_to_asc(bool async)
        {
            await base.Reverse_changes_desc_order_to_asc(async);

            AssertSql(
                """
                    SELECT `e`.`EmployeeID`
                    FROM `Employees` AS `e`
                    ORDER BY `e`.`EmployeeID`
                    """);
        }

        public override async Task Reverse_after_multiple_orderbys(bool async)
        {
            await base.Reverse_after_multiple_orderbys(async);

            AssertSql(
"""
SELECT `e`.`EmployeeID`
FROM `Employees` AS `e`
ORDER BY `e`.`EmployeeID`
""");
        }

        public override async Task Reverse_after_orderby_thenby(bool async)
        {
            await base.Reverse_after_orderby_thenby(async);

            AssertSql(
"""
SELECT `e`.`EmployeeID`
FROM `Employees` AS `e`
ORDER BY `e`.`EmployeeID` DESC, `e`.`City`
""");
        }

        public override async Task Reverse_in_subquery_via_pushdown(bool async)
        {
            await base.Reverse_in_subquery_via_pushdown(async);

            AssertSql(
                """
SELECT `e1`.`EmployeeID`, `e1`.`City`
FROM (
    SELECT DISTINCT `e0`.`EmployeeID`, `e0`.`City`, `e0`.`Country`, `e0`.`FirstName`, `e0`.`ReportsTo`, `e0`.`Title`
    FROM (
        SELECT TOP @p `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
        FROM `Employees` AS `e`
        ORDER BY `e`.`EmployeeID` DESC
    ) AS `e0`
) AS `e1`
""");
        }

        public override async Task Reverse_after_orderBy_and_take(bool async)
        {
            await base.Reverse_after_orderBy_and_take(async);

            AssertSql(
                """
SELECT `e0`.`EmployeeID`, `e0`.`City`
FROM (
    SELECT TOP @p `e`.`EmployeeID`, `e`.`City`
    FROM `Employees` AS `e`
    ORDER BY `e`.`EmployeeID`
) AS `e0`
ORDER BY `e0`.`EmployeeID` DESC
""");
        }

        public override async Task Reverse_in_join_outer(bool async)
        {
            await base.Reverse_in_join_outer(async);

            AssertSql(
"""
SELECT `c`.`CustomerID`, `o`.`OrderID`
FROM `Customers` AS `c`
INNER JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
ORDER BY `c`.`City`, `c`.`CustomerID` DESC
""");
        }

        public override async Task Reverse_in_join_outer_with_take(bool async)
        {
            await base.Reverse_in_join_outer_with_take(async);

            AssertSql(
                """
SELECT `c0`.`CustomerID`, `o`.`OrderID`
FROM (
    SELECT TOP @p `c`.`CustomerID`
    FROM `Customers` AS `c`
    ORDER BY `c`.`CustomerID`
) AS `c0`
INNER JOIN `Orders` AS `o` ON `c0`.`CustomerID` = `o`.`CustomerID`
ORDER BY `c0`.`CustomerID`
""");
        }

        public override async Task Reverse_in_join_inner(bool async)
        {
            await base.Reverse_in_join_inner(async);

            AssertSql(
"""
SELECT `c`.`CustomerID`, `o`.`OrderID`
FROM `Customers` AS `c`
LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
ORDER BY `c`.`CustomerID`
""");
        }

        public override async Task Reverse_in_join_inner_with_skip(bool async)
        {
            await base.Reverse_in_join_inner_with_skip(async);

            AssertSql(
    """
@__p_0='2'

SELECT [c].[CustomerID], [t].[OrderID]
FROM [Customers] AS [c]
LEFT JOIN (
    SELECT [o].[OrderID], [o].[CustomerID]
    FROM [Orders] AS [o]
    ORDER BY [o].[OrderID] DESC
    OFFSET @__p_0 ROWS
) AS [t] ON [c].[CustomerID] = [t].[CustomerID]
ORDER BY [c].[CustomerID]
""");
        }

        public override async Task Reverse_in_SelectMany(bool async)
        {
            await base.Reverse_in_SelectMany(async);

            AssertSql(
"""
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Customers` AS `c`
INNER JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
ORDER BY `c`.`CustomerID` DESC
""");
        }

        public override async Task Reverse_in_SelectMany_with_Take(bool async)
        {
            await base.Reverse_in_SelectMany_with_Take(async);

            AssertSql(
    """
@__p_0='20'

SELECT [t0].[OrderID], [t0].[CustomerID], [t0].[EmployeeID], [t0].[OrderDate]
FROM (
    SELECT TOP(@__p_0) [c].[CustomerID]
    FROM [Customers] AS [c]
    ORDER BY [c].[CustomerID] DESC
) AS [t]
CROSS APPLY (
    SELECT TOP(30) [o].[OrderID], [o].[CustomerID], [o].[EmployeeID], [o].[OrderDate]
    FROM [Orders] AS [o]
    WHERE [t].[CustomerID] = [o].[CustomerID]
    ORDER BY [o].[OrderID] DESC
) AS [t0]
ORDER BY [t].[CustomerID] DESC
""");
        }

        public override async Task Reverse_in_projection_subquery(bool async)
        {
            await base.Reverse_in_projection_subquery(async);

            AssertSql(
    """
SELECT [c].[CustomerID], [o].[OrderID], [o].[CustomerID], [o].[EmployeeID], [o].[OrderDate]
FROM [Customers] AS [c]
OUTER APPLY [Orders] AS [o]
ORDER BY [c].[CustomerID], [o].[OrderDate] DESC, [o].[OrderID]
""");
        }

        public override async Task Reverse_in_projection_subquery_single_result(bool async)
        {
            await base.Reverse_in_projection_subquery_single_result(async);

            AssertSql(
    """
SELECT [t].[OrderID], [t].[CustomerID], [t].[EmployeeID], [t].[OrderDate]
FROM [Customers] AS [c]
OUTER APPLY (
    SELECT TOP(1) [o].[OrderID], [o].[CustomerID], [o].[EmployeeID], [o].[OrderDate]
    FROM [Orders] AS [o]
    ORDER BY [o].[OrderDate] DESC, [o].[OrderID]
) AS [t]
ORDER BY [c].[CustomerID]
""");
        }

        public override async Task Reverse_in_projection_scalar_subquery(bool async)
        {
            await base.Reverse_in_projection_scalar_subquery(async);

            AssertSql(
"""
SELECT IIF((
        SELECT TOP 1 `o`.`OrderID`
        FROM `Orders` AS `o`
        ORDER BY `o`.`OrderDate` DESC, `o`.`OrderID`) IS NULL, 0, (
        SELECT TOP 1 `o`.`OrderID`
        FROM `Orders` AS `o`
        ORDER BY `o`.`OrderDate` DESC, `o`.`OrderID`))
FROM `Customers` AS `c`
ORDER BY `c`.`CustomerID`
""");
        }

        public override async Task Projection_AsEnumerable_projection(bool async)
        {
            await base.Projection_AsEnumerable_projection(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `o1`.`OrderID`, `o1`.`CustomerID`, `o1`.`EmployeeID`, `o1`.`OrderDate`
FROM `Customers` AS `c`
LEFT JOIN (
    SELECT `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
    FROM `Orders` AS `o0`
    WHERE `o0`.`OrderID` < 10750
) AS `o1` ON `c`.`CustomerID` = `o1`.`CustomerID`
WHERE (`c`.`CustomerID` LIKE 'A%') AND (
    SELECT COUNT(*)
    FROM `Orders` AS `o`
    WHERE `o`.`CustomerID` = `c`.`CustomerID` AND `o`.`OrderID` < 11000) > 0
ORDER BY `c`.`CustomerID`
""");
        }

        public override async Task Projection_custom_type_in_both_sides_of_ternary(bool async)
        {
            await base.Projection_custom_type_in_both_sides_of_ternary(async);

            AssertSql(
"""
SELECT IIF(`c`.`City` = 'Seattle' AND `c`.`City` IS NOT NULL, TRUE, FALSE)
FROM `Customers` AS `c`
ORDER BY `c`.`CustomerID`
""");
        }

        public override async Task Projecting_multiple_collection_with_same_constant_works(bool async)
        {
            await base.Projecting_multiple_collection_with_same_constant_works(async);

            AssertSql(
"""
SELECT `c`.`CustomerID`, 1, `o`.`OrderID`, `o0`.`OrderID`
FROM (`Customers` AS `c`
LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`)
LEFT JOIN `Orders` AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE `c`.`CustomerID` = 'ALFKI'
ORDER BY `c`.`CustomerID`, `o`.`OrderID`
""");
        }

        public override async Task Custom_projection_reference_navigation_PK_to_FK_optimization(bool async)
        {
            await base.Custom_projection_reference_navigation_PK_to_FK_optimization(async);

            AssertSql(
                """
                    SELECT `o`.`OrderID`, `c`.`CustomerID`, `c`.`City`, `o`.`OrderDate`
                    FROM `Orders` AS `o`
                    LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
                    """);
        }

        public override async Task Projecting_Length_of_a_string_property_after_FirstOrDefault_on_correlated_collection(bool async)
        {
            await base.Projecting_Length_of_a_string_property_after_FirstOrDefault_on_correlated_collection(async);

            AssertSql(
                """
    SELECT (
        SELECT TOP 1 IIF(LEN(`o`.`CustomerID`) IS NULL, NULL, CLNG(LEN(`o`.`CustomerID`)))
        FROM `Orders` AS `o`
        WHERE `c`.`CustomerID` = `o`.`CustomerID`
        ORDER BY `o`.`OrderID`)
    FROM `Customers` AS `c`
    ORDER BY `c`.`CustomerID`
    """);
        }

        public override async Task Projecting_count_of_navigation_which_is_generic_list(bool async)
        {
            await base.Projecting_count_of_navigation_which_is_generic_list(async);

            AssertSql(
                """
                    SELECT (
                        SELECT COUNT(*)
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID`)
                    FROM `Customers` AS `c`
                    ORDER BY `c`.`CustomerID`
                    """);
        }

        public override async Task Projecting_count_of_navigation_which_is_generic_collection(bool async)
        {
            await base.Projecting_count_of_navigation_which_is_generic_collection(async);

            AssertSql(
                """
                    SELECT (
                        SELECT COUNT(*)
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID`)
                    FROM `Customers` AS `c`
                    ORDER BY `c`.`CustomerID`
                    """);
        }

        public override async Task Projecting_count_of_navigation_which_is_generic_collection_using_convert(bool async)
        {
            await base.Projecting_count_of_navigation_which_is_generic_collection_using_convert(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
                    FROM `Customers` AS `c`
                    LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
                    ORDER BY `c`.`CustomerID`
                    """);
        }

        public override async Task Projection_take_projection_doesnt_project_intermittent_column(bool async)
        {
            await base.Projection_take_projection_doesnt_project_intermittent_column(async);

            AssertSql(
                """
SELECT TOP @p (`c`.`CustomerID` & ' ') & IIF(`c`.`City` IS NULL, '', `c`.`City`) AS `Aggregate`
FROM `Customers` AS `c`
ORDER BY `c`.`CustomerID`
""");
        }

        public override async Task Projection_skip_projection_doesnt_project_intermittent_column(bool async)
        {
            await base.Projection_skip_projection_doesnt_project_intermittent_column(async);

            AssertSql(
                """
                    @__p_0='7'
                    SELECT (`c`.`CustomerID` + ' ') + COALESCE(`c`.`City`, '') AS `Aggregate`
                    FROM `Customers` AS `c`
                    ORDER BY `c`.`CustomerID`
                    OFFSET @__p_0 ROWS
                    """);
        }

        public override async Task Projection_Distinct_projection_preserves_columns_used_for_distinct_in_subquery(bool async)
        {
            await base.Projection_Distinct_projection_preserves_columns_used_for_distinct_in_subquery(async);

            AssertSql(
                """
SELECT (IIF(`c0`.`FirstLetter` IS NULL, '', `c0`.`FirstLetter`) & ' ') & `c0`.`Foo` AS `Aggregate`
FROM (
    SELECT DISTINCT `c`.`CustomerID`, MID(`c`.`CustomerID`, 0 + 1, 1) AS `FirstLetter`, 'Foo' AS `Foo`
    FROM `Customers` AS `c`
) AS `c0`
""");
        }

        public override async Task Projection_take_predicate_projection(bool async)
        {
            await base.Projection_take_predicate_projection(async);

            AssertSql(
                """
SELECT (`c0`.`CustomerID` & ' ') & IIF(`c0`.`City` IS NULL, '', `c0`.`City`) AS `Aggregate`
FROM (
    SELECT TOP @p `c`.`CustomerID`, `c`.`City`
    FROM `Customers` AS `c`
    ORDER BY `c`.`CustomerID`
) AS `c0`
WHERE `c0`.`CustomerID` LIKE 'A%'
ORDER BY `c0`.`CustomerID`
""");
        }

        public override async Task Do_not_erase_projection_mapping_when_adding_single_projection(bool async)
        {
            await base.Do_not_erase_projection_mapping_when_adding_single_projection(async);

            AssertSql(
                """
                    SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `t`.`OrderID`, `t`.`ProductID`, `t`.`Discount`, `t`.`Quantity`, `t`.`UnitPrice`, `t`.`ProductID0`, `t`.`Discontinued`, `t`.`ProductName`, `t`.`SupplierID`, `t`.`UnitPrice0`, `t`.`UnitsInStock`, `t0`.`OrderID`, `t0`.`ProductID`, `t0`.`ProductID0`, `t2`.`OrderID`, `t2`.`ProductID`, `t2`.`Discount`, `t2`.`Quantity`, `t2`.`UnitPrice`, `t2`.`ProductID0`, `t2`.`Discontinued`, `t2`.`ProductName`, `t2`.`SupplierID`, `t2`.`UnitPrice0`, `t2`.`UnitsInStock`, `t0`.`Discount`, `t0`.`Quantity`, `t0`.`UnitPrice`, `t0`.`Discontinued`, `t0`.`ProductName`, `t0`.`SupplierID`, `t0`.`UnitPrice0`, `t0`.`UnitsInStock`
                    FROM `Orders` AS `o`
                    LEFT JOIN (
                        SELECT `o0`.`OrderID`, `o0`.`ProductID`, `o0`.`Discount`, `o0`.`Quantity`, `o0`.`UnitPrice`, `p`.`ProductID` AS `ProductID0`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice` AS `UnitPrice0`, `p`.`UnitsInStock`
                        FROM `Order Details` AS `o0`
                        INNER JOIN `Products` AS `p` ON `o0`.`ProductID` = `p`.`ProductID`
                    ) AS `t` ON `o`.`OrderID` = `t`.`OrderID`
                    LEFT JOIN (
                        SELECT `t1`.`OrderID`, `t1`.`ProductID`, `t1`.`Discount`, `t1`.`Quantity`, `t1`.`UnitPrice`, `t1`.`ProductID0`, `t1`.`Discontinued`, `t1`.`ProductName`, `t1`.`SupplierID`, `t1`.`UnitPrice0`, `t1`.`UnitsInStock`
                        FROM (
                            SELECT `o1`.`OrderID`, `o1`.`ProductID`, `o1`.`Discount`, `o1`.`Quantity`, `o1`.`UnitPrice`, `p0`.`ProductID` AS `ProductID0`, `p0`.`Discontinued`, `p0`.`ProductName`, `p0`.`SupplierID`, `p0`.`UnitPrice` AS `UnitPrice0`, `p0`.`UnitsInStock`, ROW_NUMBER() OVER(PARTITION BY `o1`.`OrderID` ORDER BY `o1`.`OrderID`, `o1`.`ProductID`, `p0`.`ProductID`) AS `row`
                            FROM `Order Details` AS `o1`
                            INNER JOIN `Products` AS `p0` ON `o1`.`ProductID` = `p0`.`ProductID`
                            WHERE `o1`.`UnitPrice` > 10.0
                        ) AS `t1`
                        WHERE `t1`.`row` <= 1
                    ) AS `t0` ON `o`.`OrderID` = `t0`.`OrderID`
                    LEFT JOIN (
                        SELECT `o2`.`OrderID`, `o2`.`ProductID`, `o2`.`Discount`, `o2`.`Quantity`, `o2`.`UnitPrice`, `p1`.`ProductID` AS `ProductID0`, `p1`.`Discontinued`, `p1`.`ProductName`, `p1`.`SupplierID`, `p1`.`UnitPrice` AS `UnitPrice0`, `p1`.`UnitsInStock`
                        FROM `Order Details` AS `o2`
                        INNER JOIN `Products` AS `p1` ON `o2`.`ProductID` = `p1`.`ProductID`
                        WHERE `o2`.`UnitPrice` < 10.0
                    ) AS `t2` ON `o`.`OrderID` = `t2`.`OrderID`
                    WHERE `o`.`OrderID` < 10350
                    ORDER BY `o`.`OrderID`, `t`.`OrderID`, `t`.`ProductID`, `t`.`ProductID0`, `t0`.`OrderID`, `t0`.`ProductID`, `t0`.`ProductID0`, `t2`.`OrderID`, `t2`.`ProductID`
                    """);
        }

        public override async Task Ternary_in_client_eval_assigns_correct_types(bool async)
        {
            await base.Ternary_in_client_eval_assigns_correct_types(async);

            AssertSql(
                """
                    SELECT `o`.`CustomerID`, IIF(`o`.`OrderDate` IS NOT NULL, TRUE, FALSE), `o`.`OrderDate`, `o`.`OrderID` - 10000, IIF(`o`.`OrderDate` IS NULL, TRUE, FALSE)
                    FROM `Orders` AS `o`
                    WHERE `o`.`OrderID` < 10300
                    ORDER BY `o`.`OrderID`
                    """);
        }

        public override async Task Projecting_after_navigation_and_distinct(bool async)
        {
            await base.Projecting_after_navigation_and_distinct(async);

            AssertSql(
                """
                    SELECT `t`.`CustomerID`, `t0`.`CustomerID`, `t0`.`OrderID`, `t0`.`OrderDate`
                    FROM (
                        SELECT DISTINCT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                        FROM `Orders` AS `o`
                        LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
                    ) AS `t`
                    OUTER APPLY (
                        SELECT `t`.`CustomerID`, `o0`.`OrderID`, `o0`.`OrderDate`
                        FROM `Orders` AS `o0`
                        WHERE ((`t`.`CustomerID` IS NOT NULL) AND (`t`.`CustomerID` = `o0`.`CustomerID`)) AND `o0`.`OrderID` IN (10248, 10249, 10250)
                    ) AS `t0`
                    ORDER BY `t`.`CustomerID`, `t0`.`OrderID`
                    """);
        }

        public override async Task Correlated_collection_after_distinct_with_complex_projection_containing_original_identifier(bool async)
        {
            await base.Correlated_collection_after_distinct_with_complex_projection_containing_original_identifier(async);

            AssertSql(
                """
                    SELECT `t`.`OrderID`, `t`.`Complex`, `t0`.`Outer`, `t0`.`Inner`, `t0`.`OrderDate`
                    FROM (
                        SELECT DISTINCT `o`.`OrderID`, DATEPART(month, `o`.`OrderDate`) AS `Complex`
                        FROM `Orders` AS `o`
                    ) AS `t`
                    OUTER APPLY (
                        SELECT `t`.`OrderID` AS `Outer`, `o0`.`OrderID` AS `Inner`, `o0`.`OrderDate`
                        FROM `Orders` AS `o0`
                        WHERE (`o0`.`OrderID` = `t`.`OrderID`) AND `o0`.`OrderID` IN (10248, 10249, 10250)
                    ) AS `t0`
                    ORDER BY `t`.`OrderID`
                    """);
        }

        public override async Task Correlated_collection_after_distinct_not_containing_original_identifier(bool async)
        {
            await base.Correlated_collection_after_distinct_not_containing_original_identifier(async);

            AssertSql(
                """
                    SELECT `t`.`OrderDate`, `t`.`CustomerID`, `t0`.`Outer1`, `t0`.`Outer2`, `t0`.`Inner`, `t0`.`OrderDate`
                    FROM (
                        SELECT DISTINCT `o`.`OrderDate`, `o`.`CustomerID`
                        FROM `Orders` AS `o`
                    ) AS `t`
                    OUTER APPLY (
                        SELECT `t`.`OrderDate` AS `Outer1`, `t`.`CustomerID` AS `Outer2`, `o0`.`OrderID` AS `Inner`, `o0`.`OrderDate`
                        FROM `Orders` AS `o0`
                        WHERE ((`o0`.`CustomerID` = `t`.`CustomerID`) OR ((`o0`.`CustomerID` IS NULL) AND (`t`.`CustomerID` IS NULL))) AND `o0`.`OrderID` IN (10248, 10249, 10250)
                    ) AS `t0`
                    ORDER BY `t`.`OrderDate`, `t`.`CustomerID`
                    """);
        }

        public override async Task Correlated_collection_after_distinct_with_complex_projection_not_containing_original_identifier(bool async)
        {
            await base.Correlated_collection_after_distinct_with_complex_projection_not_containing_original_identifier(async);

            AssertSql(
                """
                    SELECT `t`.`OrderDate`, `t`.`CustomerID`, `t`.`Complex`, `t0`.`Outer1`, `t0`.`Outer2`, `t0`.`Outer3`, `t0`.`Inner`, `t0`.`OrderDate`
                    FROM (
                        SELECT DISTINCT `o`.`OrderDate`, `o`.`CustomerID`, DATEPART(month, `o`.`OrderDate`) AS `Complex`
                        FROM `Orders` AS `o`
                    ) AS `t`
                    OUTER APPLY (
                        SELECT `t`.`OrderDate` AS `Outer1`, `t`.`CustomerID` AS `Outer2`, `t`.`Complex` AS `Outer3`, `o0`.`OrderID` AS `Inner`, `o0`.`OrderDate`
                        FROM `Orders` AS `o0`
                        WHERE `o0`.`OrderID` IN (10248, 10249, 10250) AND ((`t`.`CustomerID` = `o0`.`CustomerID`) OR (`t`.`CustomerID` IS NULL AND `o0`.`CustomerID` IS NULL))
                    ) AS `t0`
                    ORDER BY `t`.`OrderDate`, `t`.`CustomerID`, `t`.`Complex`, `t0`.`Inner`
                    """);
        }

        public override async Task Correlated_collection_after_groupby_with_complex_projection_containing_original_identifier(bool async)
        {
            await base.Correlated_collection_after_groupby_with_complex_projection_containing_original_identifier(async);

            AssertSql(
                """
                    SELECT `t`.`OrderID`, `t`.`c`, `t0`.`Outer`, `t0`.`Inner`, `t0`.`OrderDate`
                    FROM (
                        SELECT `o`.`OrderID`, DATEPART(month, `o`.`OrderDate`) AS `c`
                        FROM `Orders` AS `o`
                        GROUP BY `o`.`OrderID`, DATEPART(month, `o`.`OrderDate`)
                    ) AS `t`
                    OUTER APPLY (
                        SELECT `t`.`OrderID` AS `Outer`, `o0`.`OrderID` AS `Inner`, `o0`.`OrderDate`
                        FROM `Orders` AS `o0`
                        WHERE (`o0`.`OrderID` = `t`.`OrderID`) AND `o0`.`OrderID` IN (10248, 10249, 10250)
                    ) AS `t0`
                    ORDER BY `t`.`OrderID`
                    """);
        }

        public override async Task Select_nested_collection_deep(bool async)
        {
            await base.Select_nested_collection_deep(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `t0`.`OrderID`, `t0`.`OrderID0`, `t0`.`OrderID00`
                    FROM `Customers` AS `c`
                    OUTER APPLY (
                        SELECT `o`.`OrderID`, `t`.`OrderID` AS `OrderID0`, `t`.`OrderID0` AS `OrderID00`
                        FROM `Orders` AS `o`
                        OUTER APPLY (
                            SELECT `o`.`OrderID`, `o0`.`OrderID` AS `OrderID0`
                            FROM `Orders` AS `o0`
                            WHERE `o`.`CustomerID` = `c`.`CustomerID`
                        ) AS `t`
                        WHERE (`o`.`CustomerID` = `c`.`CustomerID`) AND (DATEPART(year, `o`.`OrderDate`) = 1997)
                    ) AS `t0`
                    WHERE `c`.`City` = 'London'
                    ORDER BY `c`.`CustomerID`, `t0`.`OrderID`, `t0`.`OrderID00`
                    """);
        }

        public override async Task Select_nested_collection_deep_distinct_no_identifiers(bool async)
        {
            await base.Select_nested_collection_deep_distinct_no_identifiers(async);

            AssertSql(
                """
                    SELECT `t`.`City`, `t1`.`OrderID`, `t1`.`OrderID0`, `t1`.`OrderID00`
                    FROM (
                        SELECT DISTINCT `c`.`City`
                        FROM `Customers` AS `c`
                        WHERE `c`.`City` = 'London'
                    ) AS `t`
                    OUTER APPLY (
                        SELECT `t0`.`OrderID`, `t2`.`OrderID` AS `OrderID0`, `t2`.`OrderID0` AS `OrderID00`
                        FROM (
                            SELECT DISTINCT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
                            FROM `Orders` AS `o`
                            WHERE ((`o`.`CustomerID` = `t`.`City`) OR ((`o`.`CustomerID` IS NULL) AND (`t`.`City` IS NULL))) AND (DATEPART(year, `o`.`OrderDate`) = 1997)
                        ) AS `t0`
                        OUTER APPLY (
                            SELECT `t0`.`OrderID`, `o0`.`OrderID` AS `OrderID0`
                            FROM `Orders` AS `o0`
                            WHERE (`t0`.`CustomerID` = `t`.`City`) OR ((`t0`.`CustomerID` IS NULL) AND (`t`.`City` IS NULL))
                        ) AS `t2`
                    ) AS `t1`
                    ORDER BY `t`.`City`, `t1`.`OrderID`, `t1`.`OrderID00`
                    """);
        }

        public override async Task Collection_include_over_result_of_single_non_scalar(bool async)
        {
            await base.Collection_include_over_result_of_single_non_scalar(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `t`.`OrderID`, `t`.`CustomerID`, `t`.`EmployeeID`, `t`.`OrderDate`, `t`.`OrderID0`, `t`.`ProductID`, `t`.`Discount`, `t`.`Quantity`, `t`.`UnitPrice`, `t0`.`OrderID`, `t0`.`CustomerID`, `t0`.`EmployeeID`, `t0`.`OrderDate`, `o2`.`OrderID`, `o2`.`ProductID`, `o2`.`Discount`, `o2`.`Quantity`, `o2`.`UnitPrice`
                    FROM `Customers` AS `c`
                    LEFT JOIN (
                        SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `o0`.`OrderID` AS `OrderID0`, `o0`.`ProductID`, `o0`.`Discount`, `o0`.`Quantity`, `o0`.`UnitPrice`
                        FROM `Orders` AS `o`
                        LEFT JOIN `Order Details` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`
                    ) AS `t` ON `c`.`CustomerID` = `t`.`CustomerID`
                    LEFT JOIN (
                        SELECT `t1`.`OrderID`, `t1`.`CustomerID`, `t1`.`EmployeeID`, `t1`.`OrderDate`
                        FROM (
                            SELECT `o1`.`OrderID`, `o1`.`CustomerID`, `o1`.`EmployeeID`, `o1`.`OrderDate`, ROW_NUMBER() OVER(PARTITION BY `o1`.`CustomerID` ORDER BY `o1`.`OrderDate`) AS `row`
                            FROM `Orders` AS `o1`
                        ) AS `t1`
                        WHERE `t1`.`row` <= 1
                    ) AS `t0` ON `c`.`CustomerID` = `t0`.`CustomerID`
                    LEFT JOIN `Order Details` AS `o2` ON `t0`.`OrderID` = `o2`.`OrderID`
                    WHERE `c`.`CustomerID` LIKE 'F' & '%'
                    ORDER BY `c`.`CustomerID`, `t`.`OrderID`, `t`.`OrderID0`, `t`.`ProductID`, `t0`.`OrderID`, `o2`.`OrderID`
                    """);
        }

        public override async Task Collection_projection_selecting_outer_element_followed_by_take(bool async)
        {
            await base.Collection_projection_selecting_outer_element_followed_by_take(async);

            AssertSql(
                """
                    @__p_0='10'
                    SELECT `t`.`CustomerID`, `t0`.`CustomerID`, `t0`.`Address`, `t0`.`City`, `t0`.`CompanyName`, `t0`.`ContactName`, `t0`.`ContactTitle`, `t0`.`Country`, `t0`.`Fax`, `t0`.`Phone`, `t0`.`PostalCode`, `t0`.`Region`, `t0`.`OrderID`, `t0`.`OrderID0`, `t0`.`CustomerID0`, `t0`.`EmployeeID`, `t0`.`OrderDate`
                    FROM (
                        SELECT TOP(@__p_0) `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                        FROM `Customers` AS `c`
                        WHERE `c`.`CustomerID` LIKE 'F' & '%'
                        ORDER BY `c`.`CustomerID`
                    ) AS `t`
                    OUTER APPLY (
                        SELECT `t`.`CustomerID`, `t`.`Address`, `t`.`City`, `t`.`CompanyName`, `t`.`ContactName`, `t`.`ContactTitle`, `t`.`Country`, `t`.`Fax`, `t`.`Phone`, `t`.`PostalCode`, `t`.`Region`, `o`.`OrderID`, `t1`.`OrderID` AS `OrderID0`, `t1`.`CustomerID` AS `CustomerID0`, `t1`.`EmployeeID`, `t1`.`OrderDate`
                        FROM `Orders` AS `o`
                        OUTER APPLY (
                            SELECT `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
                            FROM `Orders` AS `o0`
                            WHERE `t`.`CustomerID` = `o0`.`CustomerID`
                        ) AS `t1`
                        WHERE `t`.`CustomerID` = `o`.`CustomerID`
                    ) AS `t0`
                    ORDER BY `t`.`CustomerID`, `t0`.`OrderID`
                    """);
        }

        public override async Task Take_on_top_level_and_on_collection_projection_with_outer_apply(bool async)
        {
            await base.Take_on_top_level_and_on_collection_projection_with_outer_apply(async);

            AssertSql(
                """
                    SELECT `t`.`OrderID`, `t`.`OrderDate`, `t0`.`OrderID`, `t0`.`ProductID`, `t0`.`Discontinued`, `t0`.`ProductName`, `t0`.`SupplierID`, `t0`.`UnitPrice`, `t0`.`UnitsInStock`, `t0`.`UnitPrice0`, `t0`.`ProductID0`
                    FROM (
                        SELECT TOP 1 `o`.`OrderID`, `o`.`OrderDate`
                        FROM `Orders` AS `o`
                        WHERE (`o`.`CustomerID` IS NOT NULL) AND (`o`.`CustomerID` LIKE 'F' & '%')
                    ) AS `t`
                    OUTER APPLY (
                        SELECT `t1`.`OrderID`, `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`, `t1`.`UnitPrice` AS `UnitPrice0`, `t1`.`ProductID` AS `ProductID0`
                        FROM (
                            SELECT `o0`.`OrderID`, `o0`.`ProductID`, `o0`.`UnitPrice`
                            FROM `Order Details` AS `o0`
                            WHERE `t`.`OrderID` = `o0`.`OrderID`
                            ORDER BY `o0`.`OrderID` DESC
                            OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY
                        ) AS `t1`
                        INNER JOIN `Products` AS `p` ON `t1`.`ProductID` = `p`.`ProductID`
                    ) AS `t0`
                    ORDER BY `t`.`OrderID`, `t0`.`OrderID` DESC, `t0`.`ProductID0`
                    """);
        }

        public override async Task Take_on_correlated_collection_in_first(bool async)
        {
            await base.Take_on_correlated_collection_in_first(async);

            AssertSql(
                """
                    SELECT `t`.`CustomerID`, `t0`.`Title`, `t0`.`OrderID`, `t0`.`CustomerID`
                    FROM (
                        SELECT TOP 1 `c`.`CustomerID`
                        FROM `Customers` AS `c`
                        WHERE `c`.`CustomerID` LIKE 'F' & '%'
                        ORDER BY `c`.`CustomerID`
                    ) AS `t`
                    OUTER APPLY (
                        SELECT CASE
                            WHEN (`t1`.`CustomerID` = `c0`.`CustomerID`) OR ((`t1`.`CustomerID` IS NULL) AND (`c0`.`CustomerID` IS NULL)) THEN 'A'
                            ELSE 'B'
                        END AS `Title`, `t1`.`OrderID`, `c0`.`CustomerID`, `t1`.`OrderDate`
                        FROM (
                            SELECT TOP 1 `o`.`OrderID`, `o`.`CustomerID`, `o`.`OrderDate`
                            FROM `Orders` AS `o`
                            WHERE `t`.`CustomerID` = `o`.`CustomerID`
                            ORDER BY `o`.`OrderDate`
                        ) AS `t1`
                        LEFT JOIN `Customers` AS `c0` ON `t1`.`CustomerID` = `c0`.`CustomerID`
                    ) AS `t0`
                    ORDER BY `t`.`CustomerID`, `t0`.`OrderDate`, `t0`.`OrderID`
                    """);
        }

        public override async Task Client_projection_via_ctor_arguments(bool async)
        {
            await base.Client_projection_via_ctor_arguments(async);

            AssertSql(
                """
SELECT `c0`.`CustomerID`, `c0`.`City`, `o`.`OrderID`, `o`.`OrderDate`, `c0`.`c`
FROM (
    SELECT TOP 2 `c`.`CustomerID`, `c`.`City`, (
        SELECT COUNT(*)
        FROM `Orders` AS `o0`
        WHERE `c`.`CustomerID` = `o0`.`CustomerID`) AS `c`
    FROM `Customers` AS `c`
    WHERE `c`.`CustomerID` = 'ALFKI'
) AS `c0`
LEFT JOIN `Orders` AS `o` ON `c0`.`CustomerID` = `o`.`CustomerID`
ORDER BY `c0`.`CustomerID`
""");
        }

        public override async Task Client_projection_with_string_initialization_with_scalar_subquery(bool async)
        {
            await base.Client_projection_with_string_initialization_with_scalar_subquery(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, (
                        SELECT TOP 1 `o`.`OrderDate`
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID` AND `o`.`OrderID` < 11000), `c`.`City`, 'test' & IIF(`c`.`City` IS NULL, '', `c`.`City`)
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` LIKE 'F%'
                    """);
        }

        public override async Task MemberInit_in_projection_without_arguments(bool async)
        {
            await base.MemberInit_in_projection_without_arguments(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `o`.`OrderID`
                    FROM `Customers` AS `c`
                    LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
                    WHERE `c`.`CustomerID` LIKE 'F%'
                    ORDER BY `c`.`CustomerID`
                    """);
        }

        public override async Task VisitLambda_should_not_be_visited_trivially(bool async)
        {
            await base.VisitLambda_should_not_be_visited_trivially(async);

            AssertSql(
"""
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Orders` AS `o`
WHERE `o`.`CustomerID` LIKE 'A%'
""");
        }

        public override async Task Select_anonymous_literal(bool async)
        {
            await base.Select_anonymous_literal(async);

            AssertSql(
"""
SELECT 10 AS `X`
FROM `Customers` AS `c`
""");
        }

        public override async Task Select_anonymous_nested(bool async)
        {
            await base.Select_anonymous_nested(async);

            AssertSql(
"""
SELECT `c`.`City`, `c`.`Country`
FROM `Customers` AS `c`
""");
        }

        public override async Task Projection_when_arithmetic_mixed_subqueries(bool async)
        {
            await base.Projection_when_arithmetic_mixed_subqueries(async);

            AssertSql(
                """
SELECT CLNG(`e0`.`EmployeeID`) + CLNG(`o0`.`OrderID`) AS `Add`, `e0`.`Square`, `e0`.`EmployeeID`, `e0`.`City`, `e0`.`Country`, `e0`.`FirstName`, `e0`.`ReportsTo`, `e0`.`Title`, 42 AS `Literal`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`, `o0`.`OrderID` MOD 2 AS `Mod`
FROM (
    SELECT TOP @p `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
    FROM `Orders` AS `o`
    ORDER BY `o`.`OrderID`
) AS `o0`,
(
    SELECT TOP 2 `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`, `e`.`EmployeeID` BXOR 2 AS `Square`
    FROM `Employees` AS `e`
    ORDER BY `e`.`EmployeeID`
) AS `e0`
ORDER BY `o0`.`OrderID`
""");
        }

        public override async Task Select_datetime_Ticks_component(bool async)
        {
            await base.Select_datetime_Ticks_component(async);

            AssertSql(
"""
SELECT `o`.`OrderDate`
FROM `Orders` AS `o`
""");
        }

        public override async Task Select_datetime_TimeOfDay_component(bool async)
        {
            await base.Select_datetime_TimeOfDay_component(async);

            AssertSql(
                """
SELECT IIF(`o`.`OrderDate` IS NULL, NULL, TIMEVALUE(`o`.`OrderDate`))
FROM `Orders` AS `o`
""");
        }

        public override async Task Select_anonymous_with_object(bool async)
        {
            await base.Select_anonymous_with_object(async);

            AssertSql(
"""
SELECT `c`.`City`, `c`.`CustomerID`, `c`.`Address`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
""");
        }

        public override async Task Client_method_in_projection_requiring_materialization_1(bool async)
        {
            await base.Client_method_in_projection_requiring_materialization_1(async);

            AssertSql(
"""
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` LIKE 'A%'
""");
        }

        public override async Task Select_datetime_DayOfWeek_component(bool async)
        {
            await base.Select_datetime_DayOfWeek_component(async);

            AssertSql(
"""
SELECT CLNG(DATEPART('w', `o`.`OrderDate`) - 1)
FROM `Orders` AS `o`
""");
        }

        public override async Task Select_scalar_primitive(bool async)
        {
            await base.Select_scalar_primitive(async);

            AssertSql(
"""
SELECT `e`.`EmployeeID`
FROM `Employees` AS `e`
""");
        }

        public override async Task Client_method_in_projection_requiring_materialization_2(bool async)
        {
            await base.Client_method_in_projection_requiring_materialization_2(async);

            AssertSql(
"""
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` LIKE 'A%'
""");
        }

        public override async Task Select_anonymous_empty(bool async)
        {
            await base.Select_anonymous_empty(async);

            AssertSql(
"""
SELECT 1
FROM `Customers` AS `c`
""");
        }

        public override async Task Select_customer_table(bool async)
        {
            await base.Select_customer_table(async);

            AssertSql(
"""
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
""");
        }

        public override async Task Select_into(bool async)
        {
            await base.Select_into(async);

            AssertSql(
"""
SELECT `c`.`CustomerID`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = 'ALFKI'
""");
        }

        public override async Task Select_bool_closure(bool async)
        {
            await base.Select_bool_closure(async);

            AssertSql(
"""
SELECT 1
FROM `Customers` AS `c`
""",
//
"""
SELECT 1
FROM `Customers` AS `c`
""");
        }

        public override async Task Select_customer_identity(bool async)
        {
            await base.Select_customer_identity(async);

            AssertSql(
"""
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
""");
        }

        public override async Task Correlated_collection_after_groupby_with_complex_projection_not_containing_original_identifier(bool async)
        {
            await base.Correlated_collection_after_groupby_with_complex_projection_not_containing_original_identifier(async);

            AssertSql(
    """
@__filteredOrderIds_0='[10248,10249,10250]' (Size = 4000)

SELECT [t0].[CustomerID], [t0].[Complex], [t1].[Outer], [t1].[Inner], [t1].[OrderDate]
FROM (
    SELECT [t].[CustomerID], [t].[Complex]
    FROM (
        SELECT [o].[CustomerID], DATEPART(month, [o].[OrderDate]) AS [Complex]
        FROM [Orders] AS [o]
    ) AS [t]
    GROUP BY [t].[CustomerID], [t].[Complex]
) AS [t0]
OUTER APPLY (
    SELECT [t0].[CustomerID] AS [Outer], [o0].[OrderID] AS [Inner], [o0].[OrderDate]
    FROM [Orders] AS [o0]
    WHERE ([o0].[CustomerID] = [t0].[CustomerID] OR ([o0].[CustomerID] IS NULL AND [t0].[CustomerID] IS NULL)) AND [o0].[OrderID] IN (
        SELECT [f].[value]
        FROM OPENJSON(@__filteredOrderIds_0) WITH ([value] int '$') AS [f]
    )
) AS [t1]
ORDER BY [t0].[CustomerID], [t0].[Complex]
""");
        }

        public override async Task Select_bool_closure_with_order_by_property_with_cast_to_nullable(bool async)
        {
            await base.Select_bool_closure_with_order_by_property_with_cast_to_nullable(async);

            AssertSql();
        }

        public override async Task Reverse_without_explicit_ordering(bool async)
        {
            await base.Reverse_without_explicit_ordering(async);

            AssertSql();
        }

        public override async Task List_of_list_of_anonymous_type(bool async)
        {
            await base.List_of_list_of_anonymous_type(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `s`.`OrderID`, `s`.`OrderID0`, `s`.`ProductID`
FROM `Customers` AS `c`
LEFT JOIN (
    SELECT `o`.`OrderID`, `o0`.`OrderID` AS `OrderID0`, `o0`.`ProductID`, `o`.`CustomerID`
    FROM `Orders` AS `o`
    LEFT JOIN `Order Details` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`
) AS `s` ON `c`.`CustomerID` = `s`.`CustomerID`
WHERE `c`.`CustomerID` LIKE 'F%'
ORDER BY `c`.`CustomerID`, `s`.`OrderID`, `s`.`OrderID0`
""");
        }

        public override async Task List_from_result_of_single_result(bool async)
        {
            await base.List_from_result_of_single_result(async);

            AssertSql(
                """
SELECT `c0`.`CustomerID`, `o`.`OrderID`
FROM (
    SELECT TOP 1 `c`.`CustomerID`
    FROM `Customers` AS `c`
    ORDER BY `c`.`CustomerID`
) AS `c0`
LEFT JOIN `Orders` AS `o` ON `c0`.`CustomerID` = `o`.`CustomerID`
ORDER BY `c0`.`CustomerID`
""");
        }

        public override async Task List_from_result_of_single_result_2(bool async)
        {
            await base.List_from_result_of_single_result_2(async);

            AssertSql(
                """
SELECT `c0`.`CustomerID`, `o`.`OrderID`, `o`.`OrderDate`
FROM (
    SELECT TOP 1 `c`.`CustomerID`
    FROM `Customers` AS `c`
    ORDER BY `c`.`CustomerID`
) AS `c0`
LEFT JOIN `Orders` AS `o` ON `c0`.`CustomerID` = `o`.`CustomerID`
ORDER BY `c0`.`CustomerID`
""");
        }

        public override async Task List_from_result_of_single_result_3(bool async)
        {
            await base.List_from_result_of_single_result_3(async);

            AssertSql(
                """
                    SELECT `t`.`CustomerID`, `t0`.`OrderID`, `o0`.`ProductID`, `o0`.`OrderID`, `t0`.`c`
                    FROM (
                        SELECT TOP 1 `c`.`CustomerID`
                        FROM `Customers` AS `c`
                        ORDER BY `c`.`CustomerID`
                    ) AS `t`
                    LEFT JOIN (
                        SELECT `t1`.`c`, `t1`.`OrderID`, `t1`.`CustomerID`
                        FROM (
                            SELECT 1 AS `c`, `o`.`OrderID`, `o`.`CustomerID`, ROW_NUMBER() OVER(PARTITION BY `o`.`CustomerID` ORDER BY `o`.`OrderDate`) AS `row`
                            FROM `Orders` AS `o`
                        ) AS `t1`
                        WHERE `t1`.`row` <= 1
                    ) AS `t0` ON `t`.`CustomerID` = `t0`.`CustomerID`
                    LEFT JOIN `Order Details` AS `o0` ON `t0`.`OrderID` = `o0`.`OrderID`
                    ORDER BY `t`.`CustomerID`, `t0`.`OrderID`, `o0`.`OrderID`
                    """);
        }

        public override async Task Using_enumerable_parameter_in_projection(bool async)
        {
            await base.Using_enumerable_parameter_in_projection(async);

            AssertSql(
"""
SELECT `c`.`CustomerID`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` LIKE 'F%'
""");
        }

        public override async Task Entity_passed_to_DTO_constructor_works(bool async)
        {
            await base.Entity_passed_to_DTO_constructor_works(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
""");
        }

        public override async Task Set_operation_in_pending_collection(bool async)
        {
            await base.Set_operation_in_pending_collection(async);

            AssertSql(
                """
@__p_0='5'

SELECT [c0].[CustomerID], [u].[OrderID]
FROM (
    SELECT TOP(@__p_0) [c].[CustomerID]
    FROM [Customers] AS [c]
    ORDER BY [c].[CustomerID]
) AS [c0]
OUTER APPLY (
    SELECT [o].[OrderID]
    FROM [Orders] AS [o]
    WHERE [o].[CustomerID] = [c0].[CustomerID]
    UNION
    SELECT [o0].[OrderID]
    FROM [Orders] AS [o0]
    WHERE [o0].[CustomerID] = [c0].[CustomerID]
) AS [u]
ORDER BY [c0].[CustomerID]
""");
        }

        private void AssertSql(params string[] expected)
            => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);

        protected override void ClearLog()
            => Fixture.TestSqlLoggerFactory.Clear();
    }
}
