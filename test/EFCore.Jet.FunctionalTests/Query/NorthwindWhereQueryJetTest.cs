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
    public class NorthwindWhereQueryJetTest : NorthwindWhereQueryRelationalTestBase<
        NorthwindQueryJetFixture<NoopModelCustomizer>>
    {
        public NorthwindWhereQueryJetTest(
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

        public override async Task Where_simple(bool isAsync)
        {
            await base.Where_simple(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`City` = 'London'
                    """);
        }

        public override async Task Where_as_queryable_expression(bool isAsync)
        {
            await base.Where_as_queryable_expression(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE EXISTS (
                        SELECT 1
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID` AND `o`.`CustomerID` = 'ALFKI')
                    """);
        }

        public override async Task<string> Where_simple_closure(bool isAsync)
        {
            var queryString = await base.Where_simple_closure(isAsync);

            AssertSql(
                """
@city='London' (Size = 15)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = @city
""");

            return queryString;
        }

        public override async Task Where_indexer_closure(bool isAsync)
        {
            await base.Where_indexer_closure(isAsync);

            AssertSql(
                """
@p='London' (Size = 15)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = @p
""");
        }

        public override async Task Where_dictionary_key_access_closure(bool isAsync)
        {
            await base.Where_dictionary_key_access_closure(isAsync);

            AssertSql(
                """
@get_Item='London' (Size = 15)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = @get_Item
""");
        }

        public override async Task Where_tuple_item_closure(bool isAsync)
        {
            await base.Where_tuple_item_closure(isAsync);

            AssertSql(
                """
@predicateTuple_Item2='London' (Size = 15)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = @predicateTuple_Item2
""");
        }

        public override async Task Where_named_tuple_item_closure(bool isAsync)
        {
            await base.Where_named_tuple_item_closure(isAsync);

            AssertSql(
                """
@predicateTuple_Item2='London' (Size = 15)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = @predicateTuple_Item2
""");
        }

        public override async Task Where_simple_closure_constant(bool isAsync)
        {
            await base.Where_simple_closure_constant(isAsync);

            AssertSql(
                """
@predicate='True'

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE @predicate = TRUE
""");
        }

        public override async Task Where_simple_closure_via_query_cache(bool isAsync)
        {
            await base.Where_simple_closure_via_query_cache(isAsync);

            AssertSql(
                """
@city='London' (Size = 15)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = @city
""",
                //
                """
@city='Seattle' (Size = 15)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = @city
""");
        }

        public override async Task Where_method_call_nullable_type_closure_via_query_cache(bool isAsync)
        {
            await base.Where_method_call_nullable_type_closure_via_query_cache(isAsync);

            AssertSql(
                """
@p='2' (Nullable = true) (DbType = Decimal)

SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
FROM `Employees` AS `e`
WHERE IIF(`e`.`ReportsTo` IS NULL, NULL, CLNG(`e`.`ReportsTo`)) = @p
""",
                //
                """
@p='5' (Nullable = true) (DbType = Decimal)

SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
FROM `Employees` AS `e`
WHERE IIF(`e`.`ReportsTo` IS NULL, NULL, CLNG(`e`.`ReportsTo`)) = @p
""");
        }

        public override async Task Where_method_call_nullable_type_reverse_closure_via_query_cache(bool isAsync)
        {
            await base.Where_method_call_nullable_type_reverse_closure_via_query_cache(isAsync);

            AssertSql(
                """
@p='1' (Nullable = true) (DbType = Decimal)

SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
FROM `Employees` AS `e`
WHERE CLNG(`e`.`EmployeeID`) > @p
""",
                //
                """
@p='5' (Nullable = true) (DbType = Decimal)

SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
FROM `Employees` AS `e`
WHERE CLNG(`e`.`EmployeeID`) > @p
""");
        }

        public override async Task Where_method_call_closure_via_query_cache(bool isAsync)
        {
            await base.Where_method_call_closure_via_query_cache(isAsync);

            AssertSql(
                """
@GetCity='London' (Size = 15)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = @GetCity
""",
                //
                """
@GetCity='Seattle' (Size = 15)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = @GetCity
""");
        }

        public override async Task Where_field_access_closure_via_query_cache(bool isAsync)
        {
            await base.Where_field_access_closure_via_query_cache(isAsync);

            AssertSql(
                """
@city_InstanceFieldValue='London' (Size = 15)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = @city_InstanceFieldValue
""",
                //
                """
@city_InstanceFieldValue='Seattle' (Size = 15)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = @city_InstanceFieldValue
""");
        }

        public override async Task Where_property_access_closure_via_query_cache(bool isAsync)
        {
            await base.Where_property_access_closure_via_query_cache(isAsync);

            AssertSql(
                """
@city_InstancePropertyValue='London' (Size = 15)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = @city_InstancePropertyValue
""",
                //
                """
@city_InstancePropertyValue='Seattle' (Size = 15)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = @city_InstancePropertyValue
""");
        }

        public override async Task Where_static_field_access_closure_via_query_cache(bool isAsync)
        {
            await base.Where_static_field_access_closure_via_query_cache(isAsync);

            AssertSql(
                """
@StaticFieldValue='London' (Size = 15)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = @StaticFieldValue
""",
                //
                """
@StaticFieldValue='Seattle' (Size = 15)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = @StaticFieldValue
""");
        }

        public override async Task Where_static_property_access_closure_via_query_cache(bool isAsync)
        {
            await base.Where_static_property_access_closure_via_query_cache(isAsync);

            AssertSql(
                """
@StaticPropertyValue='London' (Size = 15)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = @StaticPropertyValue
""",
                //
                """
@StaticPropertyValue='Seattle' (Size = 15)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = @StaticPropertyValue
""");
        }

        public override async Task Where_nested_field_access_closure_via_query_cache(bool isAsync)
        {
            await base.Where_nested_field_access_closure_via_query_cache(isAsync);

            AssertSql(
                """
@city_Nested_InstanceFieldValue='London' (Size = 15)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = @city_Nested_InstanceFieldValue
""",
                //
                """
@city_Nested_InstanceFieldValue='Seattle' (Size = 15)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = @city_Nested_InstanceFieldValue
""");
        }

        public override async Task Where_nested_property_access_closure_via_query_cache(bool isAsync)
        {
            await base.Where_nested_property_access_closure_via_query_cache(isAsync);

            AssertSql(
                """
@city_Nested_InstancePropertyValue='London' (Size = 15)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = @city_Nested_InstancePropertyValue
""",
                //
                """
@city_Nested_InstancePropertyValue='Seattle' (Size = 15)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = @city_Nested_InstancePropertyValue
""");
        }

        public override async Task Where_new_instance_field_access_query_cache(bool isAsync)
        {
            await base.Where_new_instance_field_access_query_cache(isAsync);

            AssertSql(
                """
@InstanceFieldValue='London' (Size = 15)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = @InstanceFieldValue
""",
                //
                """
@InstanceFieldValue='Seattle' (Size = 15)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = @InstanceFieldValue
""");
        }

        public override async Task Where_new_instance_field_access_closure_via_query_cache(bool isAsync)
        {
            await base.Where_new_instance_field_access_closure_via_query_cache(isAsync);

            AssertSql(
                """
@InstanceFieldValue='London' (Size = 15)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = @InstanceFieldValue
""",
                //
                """
@InstanceFieldValue='Seattle' (Size = 15)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = @InstanceFieldValue
""");
        }

        public override async Task Where_simple_closure_via_query_cache_nullable_type(bool isAsync)
        {
            await base.Where_simple_closure_via_query_cache_nullable_type(isAsync);

            AssertSql(
                """
@p='2' (Nullable = true) (DbType = Decimal)

SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
FROM `Employees` AS `e`
WHERE IIF(`e`.`ReportsTo` IS NULL, NULL, CLNG(`e`.`ReportsTo`)) = @p
""",
                //
                """
@p='5' (Nullable = true) (DbType = Decimal)

SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
FROM `Employees` AS `e`
WHERE IIF(`e`.`ReportsTo` IS NULL, NULL, CLNG(`e`.`ReportsTo`)) = @p
""",
                //
                """
SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
FROM `Employees` AS `e`
WHERE `e`.`ReportsTo` IS NULL
""");
        }

        public override async Task Where_simple_closure_via_query_cache_nullable_type_reverse(bool isAsync)
        {
            await base.Where_simple_closure_via_query_cache_nullable_type_reverse(isAsync);

            AssertSql(
                """
SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
FROM `Employees` AS `e`
WHERE `e`.`ReportsTo` IS NULL
""",
                //
                """
@p='5' (Nullable = true) (DbType = Decimal)

SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
FROM `Employees` AS `e`
WHERE IIF(`e`.`ReportsTo` IS NULL, NULL, CLNG(`e`.`ReportsTo`)) = @p
""",
                //
                """
@p='2' (Nullable = true) (DbType = Decimal)

SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
FROM `Employees` AS `e`
WHERE IIF(`e`.`ReportsTo` IS NULL, NULL, CLNG(`e`.`ReportsTo`)) = @p
""");
        }

        public override async Task Where_subquery_closure_via_query_cache(bool isAsync)
        {
            await base.Where_subquery_closure_via_query_cache(isAsync);

            AssertSql(
                """
@customerID='ALFKI' (Size = 5)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE EXISTS (
    SELECT 1
    FROM `Orders` AS `o`
    WHERE `o`.`CustomerID` = @customerID AND `o`.`CustomerID` = `c`.`CustomerID`)
""",
                //
                """
@customerID='ANATR' (Size = 5)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE EXISTS (
    SELECT 1
    FROM `Orders` AS `o`
    WHERE `o`.`CustomerID` = @customerID AND `o`.`CustomerID` = `c`.`CustomerID`)
""");
        }

        public override async Task Where_simple_shadow(bool isAsync)
        {
            await base.Where_simple_shadow(isAsync);

            AssertSql(
                $"""
                    SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
                    FROM `Employees` AS `e`
                    WHERE `e`.`Title` = 'Sales Representative'
                    """);
        }

        public override async Task Where_simple_shadow_projection(bool isAsync)
        {
            await base.Where_simple_shadow_projection(isAsync);

            AssertSql(
                $"""
                    SELECT `e`.`Title`
                    FROM `Employees` AS `e`
                    WHERE `e`.`Title` = 'Sales Representative'
                    """);
        }

        public override async Task Where_shadow_subquery_FirstOrDefault(bool isAsync)
        {
            await base.Where_shadow_subquery_FirstOrDefault(isAsync);

            AssertSql(
                """
SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
FROM `Employees` AS `e`
WHERE `e`.`Title` = (
    SELECT TOP 1 `e0`.`Title`
    FROM `Employees` AS `e0`
    ORDER BY `e0`.`Title`) OR (`e`.`Title` IS NULL AND (
    SELECT TOP 1 `e0`.`Title`
    FROM `Employees` AS `e0`
    ORDER BY `e0`.`Title`) IS NULL)
""");
        }

        public override async Task Where_subquery_correlated(bool isAsync)
        {
            await base.Where_subquery_correlated(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE EXISTS (
                        SELECT 1
                        FROM `Customers` AS `c0`
                        WHERE `c`.`CustomerID` = `c0`.`CustomerID`)
                    """);
        }

        public override async Task Where_equals_method_int(bool isAsync)
        {
            await base.Where_equals_method_int(isAsync);

            AssertSql(
                $"""
                    SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
                    FROM `Employees` AS `e`
                    WHERE `e`.`EmployeeID` = 1
                    """);
        }

        public override async Task Where_equals_using_object_overload_on_mismatched_types(bool isAsync)
        {
            await base.Where_equals_using_object_overload_on_mismatched_types(isAsync);

            AssertSql(
                $"""
                    SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
                    FROM `Employees` AS `e`
                    WHERE 0 = 1
                    """);

            // See issue#17498
            //Assert.Contains(
            //    RelationalStrings.LogPossibleUnintendedUseOfEquals.GenerateMessage(
            //        "e.EmployeeID.Equals(Convert(__longPrm_0, Object))"), Fixture.TestSqlLoggerFactory.Log.Select(l => l.Message));
        }

        public override async Task Where_equals_using_int_overload_on_mismatched_types(bool isAsync)
        {
            await base.Where_equals_using_int_overload_on_mismatched_types(isAsync);

            AssertSql(
                """
@p='1'

SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
FROM `Employees` AS `e`
WHERE `e`.`EmployeeID` = @p
""");
        }

        public override async Task Where_equals_on_mismatched_types_nullable_int_long(bool isAsync)
        {
            await base.Where_equals_on_mismatched_types_nullable_int_long(isAsync);

            AssertSql(
                $"""
                    SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
                    FROM `Employees` AS `e`
                    WHERE 0 = 1
                    """,
                //
                $"""
                    SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
                    FROM `Employees` AS `e`
                    WHERE 0 = 1
                    """);

            // See issue#17498
            //Assert.Contains(
            //    RelationalStrings.LogPossibleUnintendedUseOfEquals.GenerateMessage(
            //        "__longPrm_0.Equals(Convert(e.ReportsTo, Object))"), Fixture.TestSqlLoggerFactory.Log.Select(l => l.Message));

            //Assert.Contains(
            //    RelationalStrings.LogPossibleUnintendedUseOfEquals.GenerateMessage(
            //        "e.ReportsTo.Equals(Convert(__longPrm_0, Object))"), Fixture.TestSqlLoggerFactory.Log.Select(l => l.Message));
        }

        public override async Task Where_equals_on_mismatched_types_nullable_long_nullable_int(bool isAsync)
        {
            await base.Where_equals_on_mismatched_types_nullable_long_nullable_int(isAsync);

            AssertSql(
                $"""
                    SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
                    FROM `Employees` AS `e`
                    WHERE 0 = 1
                    """,
                //
                $"""
                    SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
                    FROM `Employees` AS `e`
                    WHERE 0 = 1
                    """);

            // See issue#17498
            //Assert.Contains(
            //    RelationalStrings.LogPossibleUnintendedUseOfEquals.GenerateMessage(
            //        "__nullableLongPrm_0.Equals(Convert(e.ReportsTo, Object))"),
            //    Fixture.TestSqlLoggerFactory.Log.Select(l => l.Message));

            //Assert.Contains(
            //    RelationalStrings.LogPossibleUnintendedUseOfEquals.GenerateMessage(
            //        "e.ReportsTo.Equals(Convert(__nullableLongPrm_0, Object))"),
            //    Fixture.TestSqlLoggerFactory.Log.Select(l => l.Message));
        }

        public override async Task Where_equals_on_mismatched_types_int_nullable_int(bool isAsync)
        {
            await base.Where_equals_on_mismatched_types_int_nullable_int(isAsync);

            AssertSql(
                """
@intPrm='2'

SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
FROM `Employees` AS `e`
WHERE `e`.`ReportsTo` = @intPrm
""",
                //
                """
@intPrm='2'

SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
FROM `Employees` AS `e`
WHERE @intPrm = `e`.`ReportsTo`
""");
        }

        public override async Task Where_equals_on_matched_nullable_int_types(bool isAsync)
        {
            await base.Where_equals_on_matched_nullable_int_types(isAsync);

            AssertSql(
                """
@nullableIntPrm='2' (Nullable = true)

SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
FROM `Employees` AS `e`
WHERE @nullableIntPrm = `e`.`ReportsTo`
""",
                //
                """
@nullableIntPrm='2' (Nullable = true)

SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
FROM `Employees` AS `e`
WHERE `e`.`ReportsTo` = @nullableIntPrm
""");
        }

        public override async Task Where_equals_on_null_nullable_int_types(bool isAsync)
        {
            await base.Where_equals_on_null_nullable_int_types(isAsync);

            AssertSql(
                $"""
                    SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
                    FROM `Employees` AS `e`
                    WHERE `e`.`ReportsTo` IS NULL
                    """,
                //
                $"""
                    SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
                    FROM `Employees` AS `e`
                    WHERE `e`.`ReportsTo` IS NULL
                    """);
        }

        public override async Task Where_comparison_nullable_type_not_null(bool isAsync)
        {
            await base.Where_comparison_nullable_type_not_null(isAsync);

            AssertSql(
                $"""
                    SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
                    FROM `Employees` AS `e`
                    WHERE `e`.`ReportsTo` = 2
                    """);
        }

        public override async Task Where_comparison_nullable_type_null(bool isAsync)
        {
            await base.Where_comparison_nullable_type_null(isAsync);

            AssertSql(
                $"""
                    SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
                    FROM `Employees` AS `e`
                    WHERE `e`.`ReportsTo` IS NULL
                    """);
        }

        public override async Task Where_simple_reversed(bool isAsync)
        {
            await base.Where_simple_reversed(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE 'London' = `c`.`City`
                    """);
        }

        public override async Task Where_is_null(bool isAsync)
        {
            await base.Where_is_null(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`Region` IS NULL
                    """);
        }

        public override async Task Where_null_is_null(bool isAsync)
        {
            await base.Where_null_is_null(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    """);
        }

        public override async Task Where_constant_is_null(bool isAsync)
        {
            await base.Where_constant_is_null(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE 0 = 1
                    """);
        }

        public override async Task Where_is_not_null(bool isAsync)
        {
            await base.Where_is_not_null(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`City` IS NOT NULL
                    """);
        }

        public override async Task Where_null_is_not_null(bool isAsync)
        {
            await base.Where_null_is_not_null(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE 0 = 1
                    """);
        }

        public override async Task Where_constant_is_not_null(bool isAsync)
        {
            await base.Where_constant_is_not_null(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    """);
        }

        public override async Task Where_identity_comparison(bool isAsync)
        {
            await base.Where_identity_comparison(isAsync);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = `c`.`City` OR `c`.`City` IS NULL
""");
        }

        public override async Task Where_in_optimization_multiple(bool isAsync)
        {
            await base.Where_in_optimization_multiple(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
                    FROM `Customers` AS `c`,
                    `Employees` AS `e`
                    WHERE `c`.`City` IN ('London', 'Berlin') OR `c`.`CustomerID` = 'ALFKI' OR `c`.`CustomerID` = 'ABCDE'
                    """);
        }

        public override async Task Where_not_in_optimization1(bool isAsync)
        {
            await base.Where_not_in_optimization1(isAsync);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
FROM `Customers` AS `c`,
`Employees` AS `e`
WHERE (`c`.`City` <> 'London' OR `c`.`City` IS NULL) AND (`e`.`City` <> 'London' OR `e`.`City` IS NULL)
""");
        }

        public override async Task Where_not_in_optimization2(bool isAsync)
        {
            await base.Where_not_in_optimization2(isAsync);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
FROM `Customers` AS `c`,
`Employees` AS `e`
WHERE `c`.`City` NOT IN ('London', 'Berlin') OR `c`.`City` IS NULL
""");
        }

        public override async Task Where_not_in_optimization3(bool isAsync)
        {
            await base.Where_not_in_optimization3(isAsync);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
FROM `Customers` AS `c`,
`Employees` AS `e`
WHERE `c`.`City` NOT IN ('London', 'Berlin', 'Seattle') OR `c`.`City` IS NULL
""");
        }

        public override async Task Where_not_in_optimization4(bool isAsync)
        {
            await base.Where_not_in_optimization4(isAsync);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
FROM `Customers` AS `c`,
`Employees` AS `e`
WHERE `c`.`City` NOT IN ('London', 'Berlin', 'Seattle', 'Lisboa') OR `c`.`City` IS NULL
""");
        }

        public override async Task Where_select_many_and(bool isAsync)
        {
            await base.Where_select_many_and(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
                    FROM `Customers` AS `c`,
                    `Employees` AS `e`
                    WHERE `c`.`City` = 'London' AND `c`.`Country` = 'UK' AND `e`.`City` = 'London' AND `e`.`Country` = 'UK'
                    """);
        }

        public override async Task Where_primitive(bool isAsync)
        {
            await base.Where_primitive(isAsync);

            AssertSql(
                """
SELECT `e0`.`EmployeeID`
FROM (
    SELECT TOP @p `e`.`EmployeeID`
    FROM `Employees` AS `e`
) AS `e0`
WHERE `e0`.`EmployeeID` = 5
""");
        }

        public override async Task Where_bool_member(bool isAsync)
        {
            await base.Where_bool_member(isAsync);

            AssertSql(
                $"""
                    SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
                    FROM `Products` AS `p`
                    WHERE `p`.`Discontinued` = TRUE
                    """);
        }

        public override async Task Where_bool_member_false(bool isAsync)
        {
            await base.Where_bool_member_false(isAsync);

            AssertSql(
                """
SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
FROM `Products` AS `p`
WHERE `p`.`Discontinued` = FALSE
""");
        }

        public override async Task Where_bool_member_negated_twice(bool isAsync)
        {
            await base.Where_bool_member_negated_twice(isAsync);

            AssertSql(
                $"""
                    SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
                    FROM `Products` AS `p`
                    WHERE `p`.`Discontinued` = TRUE
                    """);
        }

        public override async Task Where_bool_member_shadow(bool isAsync)
        {
            await base.Where_bool_member_shadow(isAsync);

            AssertSql(
                $"""
                    SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
                    FROM `Products` AS `p`
                    WHERE `p`.`Discontinued` = TRUE
                    """);
        }

        public override async Task Where_bool_member_false_shadow(bool isAsync)
        {
            await base.Where_bool_member_false_shadow(isAsync);

            AssertSql(
                """
SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
FROM `Products` AS `p`
WHERE `p`.`Discontinued` = FALSE
""");
        }

        public override async Task Where_bool_member_equals_constant(bool isAsync)
        {
            await base.Where_bool_member_equals_constant(isAsync);

            AssertSql(
                $"""
                    SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
                    FROM `Products` AS `p`
                    WHERE `p`.`Discontinued` = TRUE
                    """);
        }

        public override async Task Where_bool_member_in_complex_predicate(bool isAsync)
        {
            await base.Where_bool_member_in_complex_predicate(isAsync);

            AssertSql(
                $"""
                    SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
                    FROM `Products` AS `p`
                    WHERE (`p`.`ProductID` > 100 AND `p`.`Discontinued` = TRUE) OR `p`.`Discontinued` = TRUE
                    """);
        }

        public override async Task Where_bool_member_compared_to_binary_expression(bool isAsync)
        {
            await base.Where_bool_member_compared_to_binary_expression(isAsync);

            AssertSql(
                $"""
                    SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
                    FROM `Products` AS `p`
                    WHERE `p`.`Discontinued` = IIF(`p`.`ProductID` > 50, TRUE, FALSE)
                    """);
        }

        public override async Task Where_not_bool_member_compared_to_not_bool_member(bool isAsync)
        {
            await base.Where_not_bool_member_compared_to_not_bool_member(isAsync);

            AssertSql(
                $"""
                    SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
                    FROM `Products` AS `p`
                    """);
        }

        public override async Task Where_negated_boolean_expression_compared_to_another_negated_boolean_expression(bool isAsync)
        {
            await base.Where_negated_boolean_expression_compared_to_another_negated_boolean_expression(isAsync);

            AssertSql(
                """
SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
FROM `Products` AS `p`
WHERE IIF(`p`.`ProductID` <= 50, TRUE, FALSE) = IIF(`p`.`ProductID` <= 20, TRUE, FALSE)
""");
        }

        public override async Task Where_not_bool_member_compared_to_binary_expression(bool isAsync)
        {
            await base.Where_not_bool_member_compared_to_binary_expression(isAsync);

            AssertSql(
                """
SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
FROM `Products` AS `p`
WHERE `p`.`Discontinued` <> IIF(`p`.`ProductID` > 50, TRUE, FALSE)
""");
        }

        public override async Task Where_bool_parameter(bool isAsync)
        {
            await base.Where_bool_parameter(isAsync);

            AssertSql(
                """
@prm='True'

SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
FROM `Products` AS `p`
WHERE @prm = TRUE
""");
        }

        public override async Task Where_bool_parameter_compared_to_binary_expression(bool isAsync)
        {
            await base.Where_bool_parameter_compared_to_binary_expression(isAsync);

            AssertSql(
                """
@prm='True'

SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
FROM `Products` AS `p`
WHERE IIF(`p`.`ProductID` > 50, TRUE, FALSE) <> @prm
""");
        }

        public override async Task Where_bool_member_and_parameter_compared_to_binary_expression_nested(bool isAsync)
        {
            await base.Where_bool_member_and_parameter_compared_to_binary_expression_nested(isAsync);

            AssertSql(
                """
@prm='True'

SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
FROM `Products` AS `p`
WHERE `p`.`Discontinued` = (IIF(`p`.`ProductID` > 50, TRUE, FALSE) BXOR @prm)
""");
        }

        public override async Task Where_de_morgan_or_optimized(bool isAsync)
        {
            await base.Where_de_morgan_or_optimized(isAsync);

            AssertSql(
                """
SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
FROM `Products` AS `p`
WHERE `p`.`Discontinued` = FALSE AND `p`.`ProductID` >= 20
""");
        }

        public override async Task Where_de_morgan_and_optimized(bool isAsync)
        {
            await base.Where_de_morgan_and_optimized(isAsync);

            AssertSql(
                """
SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
FROM `Products` AS `p`
WHERE `p`.`Discontinued` = FALSE OR `p`.`ProductID` >= 20
""");
        }

        public override async Task Where_complex_negated_expression_optimized(bool isAsync)
        {
            await base.Where_complex_negated_expression_optimized(isAsync);

            AssertSql(
                """
SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
FROM `Products` AS `p`
WHERE `p`.`Discontinued` = FALSE AND `p`.`ProductID` < 60 AND `p`.`ProductID` > 30
""");
        }

        public override async Task Where_short_member_comparison(bool isAsync)
        {
            await base.Where_short_member_comparison(isAsync);

            AssertSql(
                $"""
                    SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
                    FROM `Products` AS `p`
                    WHERE `p`.`UnitsInStock` > 10
                    """);
        }

        public override async Task Where_comparison_to_nullable_bool(bool isAsync)
        {
            await base.Where_comparison_to_nullable_bool(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` LIKE '%KI'
                    """);
        }

        public override async Task Where_true(bool isAsync)
        {
            await base.Where_true(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    """);
        }

        public override async Task Where_false(bool isAsync)
        {
            await base.Where_false(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE 0 = 1
                    """);
        }

        public override async Task Where_default(bool isAsync)
        {
            await base.Where_default(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`Fax` IS NULL
                    """);
        }

        public override async Task Where_expression_invoke_1(bool isAsync)
        {
            await base.Where_expression_invoke_1(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` = 'ALFKI'
                    """);
        }

        public override async Task Where_expression_invoke_2(bool isAsync)
        {
            await base.Where_expression_invoke_2(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
                    FROM `Orders` AS `o`
                    LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
                    WHERE `c`.`CustomerID` = 'ALFKI'
                    """);
        }

        public override async Task Where_expression_invoke_3(bool async)
        {
            await base.Where_expression_invoke_3(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = 'ALFKI'
""");
        }

        public override async Task Where_ternary_boolean_condition_true(bool isAsync)
        {
            await base.Where_ternary_boolean_condition_true(isAsync);

            AssertSql(
                $"""
                    SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
                    FROM `Products` AS `p`
                    WHERE `p`.`UnitsInStock` >= 20
                    """);
        }

        public override async Task Where_ternary_boolean_condition_false(bool isAsync)
        {
            await base.Where_ternary_boolean_condition_false(isAsync);

            AssertSql(
                $"""
                    SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
                    FROM `Products` AS `p`
                    WHERE `p`.`UnitsInStock` < 20
                    """);
        }

        public override async Task Where_ternary_boolean_condition_with_another_condition(bool isAsync)
        {
            await base.Where_ternary_boolean_condition_with_another_condition(isAsync);

            AssertSql(
                """
@productId='15'

SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
FROM `Products` AS `p`
WHERE `p`.`ProductID` < @productId AND `p`.`UnitsInStock` >= 20
""");
        }

        public override async Task Where_ternary_boolean_condition_with_false_as_result_true(bool isAsync)
        {
            await base.Where_ternary_boolean_condition_with_false_as_result_true(isAsync);

            AssertSql(
                $"""
                    SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
                    FROM `Products` AS `p`
                    WHERE `p`.`UnitsInStock` >= 20
                    """);
        }

        public override async Task Where_ternary_boolean_condition_with_false_as_result_false(bool isAsync)
        {
            await base.Where_ternary_boolean_condition_with_false_as_result_false(isAsync);

            AssertSql(
                """
                    SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
                    FROM `Products` AS `p`
                    WHERE 0 = 1
                    """);
        }

        public override async Task Where_ternary_boolean_condition_negated(bool async)
        {
            await base.Where_ternary_boolean_condition_negated(async);

            AssertSql(
                """
SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
FROM `Products` AS `p`
WHERE IIF(`p`.`UnitsInStock` >= 20, TRUE, FALSE) = TRUE
""");
        }

        public override async Task Where_compare_constructed_equal(bool async)
        {
            //  Anonymous type to constant comparison. Issue #14672.
            await AssertTranslationFailed(() => base.Where_compare_constructed_equal(async));

            AssertSql();
        }

        public override async Task Where_compare_constructed_multi_value_equal(bool async)
        {
            //  Anonymous type to constant comparison. Issue #14672.
            await AssertTranslationFailed(() => base.Where_compare_constructed_multi_value_equal(async));

            AssertSql();
        }

        public override async Task Where_compare_constructed_multi_value_not_equal(bool async)
        {
            //  Anonymous type to constant comparison. Issue #14672.
            await AssertTranslationFailed(() => base.Where_compare_constructed_multi_value_not_equal(async));

            AssertSql();
        }

        public override async Task Where_compare_tuple_constructed_equal(bool async)
        {
            //  Anonymous type to constant comparison. Issue #14672.
            await AssertTranslationFailed(() => base.Where_compare_tuple_constructed_equal(async));

            AssertSql();
        }

        public override async Task Where_compare_tuple_constructed_multi_value_equal(bool async)
        {
            //  Anonymous type to constant comparison. Issue #14672.
            await AssertTranslationFailed(() => base.Where_compare_tuple_constructed_multi_value_equal(async));

            AssertSql();
        }

        public override async Task Where_compare_tuple_constructed_multi_value_not_equal(bool async)
        {
            //  Anonymous type to constant comparison. Issue #14672.
            await AssertTranslationFailed(() => base.Where_compare_tuple_constructed_multi_value_not_equal(async));

            AssertSql();
        }

        public override async Task Where_compare_tuple_create_constructed_equal(bool async)
        {
            //  Anonymous type to constant comparison. Issue #14672.
            await AssertTranslationFailed(() => base.Where_compare_tuple_create_constructed_equal(async));

            AssertSql();
        }

        public override async Task Where_compare_tuple_create_constructed_multi_value_equal(bool async)
        {
            //  Anonymous type to constant comparison. Issue #14672.
            await AssertTranslationFailed(() => base.Where_compare_tuple_create_constructed_multi_value_equal(async));

            AssertSql();
        }

        public override async Task Where_compare_tuple_create_constructed_multi_value_not_equal(bool async)
        {
            //  Anonymous type to constant comparison. Issue #14672.
            await AssertTranslationFailed(() => base.Where_compare_tuple_create_constructed_multi_value_not_equal(async));

            AssertSql();
        }

        public override async Task Where_compare_null(bool isAsync)
        {
            await base.Where_compare_null(isAsync);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`Region` IS NULL AND `c`.`Country` = 'UK'
""");
        }

        public override async Task Where_compare_null_with_cast_to_object(bool async)
        {
            await base.Where_compare_null_with_cast_to_object(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`Region` IS NULL
""");
        }

        public override async Task Where_compare_with_both_cast_to_object(bool async)
        {
            await base.Where_compare_with_both_cast_to_object(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`City` = 'London'
""");
        }

        public override async Task Where_Is_on_same_type(bool isAsync)
        {
            await base.Where_Is_on_same_type(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    """);
        }

        public override async Task Where_chain(bool isAsync)
        {
            await base.Where_chain(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
                    FROM `Orders` AS `o`
                    WHERE `o`.`CustomerID` = 'QUICK' AND `o`.`OrderDate` > #1998-01-01#
                    """);
        }

        public override async Task Where_navigation_contains(bool isAsync)
        {
            await base.Where_navigation_contains(isAsync);

            AssertSql(
                """
SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM (
    SELECT TOP 2 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    WHERE `c`.`CustomerID` = 'ALFKI'
) AS `c0`
LEFT JOIN `Orders` AS `o` ON `c0`.`CustomerID` = `o`.`CustomerID`
ORDER BY `c0`.`CustomerID`
""",
                //
                """
SELECT `o`.`OrderID`, `o`.`ProductID`, `o`.`Discount`, `o`.`Quantity`, `o`.`UnitPrice`
FROM `Order Details` AS `o`
INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`
WHERE `o0`.`OrderID` IN (10643, 10952, 10692, 10835, 11011, 10702)
""");
        }

        public override async Task Where_array_index(bool isAsync)
        {
            await base.Where_array_index(isAsync);

            AssertSql(
                """
@p='ALFKI' (Size = 5)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = @p
""");
        }

        public override async Task Where_multiple_contains_in_subquery_with_or(bool isAsync)
        {
            await base.Where_multiple_contains_in_subquery_with_or(isAsync);

            AssertSql(
                """
SELECT `o`.`OrderID`, `o`.`ProductID`, `o`.`Discount`, `o`.`Quantity`, `o`.`UnitPrice`
FROM `Order Details` AS `o`
WHERE `o`.`ProductID` IN (
    SELECT TOP 1 `p`.`ProductID`
    FROM `Products` AS `p`
    ORDER BY `p`.`ProductID`
) OR `o`.`OrderID` IN (
    SELECT TOP 1 `o0`.`OrderID`
    FROM `Orders` AS `o0`
    ORDER BY `o0`.`OrderID`
)
""");
        }

        public override async Task Where_multiple_contains_in_subquery_with_and(bool isAsync)
        {
            await base.Where_multiple_contains_in_subquery_with_and(isAsync);

            AssertSql(
                """
SELECT `o`.`OrderID`, `o`.`ProductID`, `o`.`Discount`, `o`.`Quantity`, `o`.`UnitPrice`
FROM `Order Details` AS `o`
WHERE `o`.`ProductID` IN (
    SELECT TOP 20 `p`.`ProductID`
    FROM `Products` AS `p`
    ORDER BY `p`.`ProductID`
) AND `o`.`OrderID` IN (
    SELECT TOP 10 `o0`.`OrderID`
    FROM `Orders` AS `o0`
    ORDER BY `o0`.`OrderID`
)
""");
        }

        public override async Task Where_contains_on_navigation(bool isAsync)
        {
            await base.Where_contains_on_navigation(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
                    FROM `Orders` AS `o`
                    WHERE EXISTS (
                        SELECT 1
                        FROM `Customers` AS `c`
                        WHERE `o`.`OrderID` IN (
                            SELECT `o0`.`OrderID`
                            FROM `Orders` AS `o0`
                            WHERE `c`.`CustomerID` = `o0`.`CustomerID`
                        )
                    )
                    """);
        }

        public override async Task Where_subquery_FirstOrDefault_is_null(bool isAsync)
        {
            await base.Where_subquery_FirstOrDefault_is_null(isAsync);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE NOT EXISTS (
    SELECT 1
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` = `o`.`CustomerID`)
""");
        }

        public override async Task Where_subquery_FirstOrDefault_compared_to_entity(bool isAsync)
        {
            await base.Where_subquery_FirstOrDefault_compared_to_entity(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE (
                        SELECT TOP 1 `o`.`OrderID`
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID`
                        ORDER BY `o`.`OrderID`) = 10276
                    """);
        }

        public override async Task TypeBinary_short_circuit(bool isAsync)
        {
            await base.TypeBinary_short_circuit(isAsync);

            AssertSql(
                """
@p='False'

SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Orders` AS `o`
WHERE @p = TRUE
""");
        }

        public override async Task Where_is_conditional(bool isAsync)
        {
            await base.Where_is_conditional(isAsync);

            AssertSql(
                $"""
                    SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
                    FROM `Products` AS `p`
                    WHERE 0 = 1
                    """);
        }

        public override async Task Enclosing_class_settable_member_generates_parameter(bool isAsync)
        {
            await base.Enclosing_class_settable_member_generates_parameter(isAsync);

            AssertSql(
                """
@SettableProperty='10274'

SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Orders` AS `o`
WHERE `o`.`OrderID` = @SettableProperty
""",
                //
                """
@SettableProperty='10275'

SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Orders` AS `o`
WHERE `o`.`OrderID` = @SettableProperty
""");
        }

        public override async Task Enclosing_class_readonly_member_generates_parameter(bool isAsync)
        {
            await base.Enclosing_class_readonly_member_generates_parameter(isAsync);

            AssertSql(
                """
@ReadOnlyProperty='10275'

SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Orders` AS `o`
WHERE `o`.`OrderID` = @ReadOnlyProperty
""");
        }

        public override async Task Enclosing_class_const_member_does_not_generate_parameter(bool isAsync)
        {
            await base.Enclosing_class_const_member_does_not_generate_parameter(isAsync);

            AssertSql(
                $"""
                    SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
                    FROM `Orders` AS `o`
                    WHERE `o`.`OrderID` = 10274
                    """);
        }

        public override async Task Generic_Ilist_contains_translates_to_server(bool isAsync)
        {
            await base.Generic_Ilist_contains_translates_to_server(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`City` = 'Seattle'
                    """);
        }

        public override async Task Filter_non_nullable_value_after_FirstOrDefault_on_empty_collection(bool isAsync)
        {
            await base.Filter_non_nullable_value_after_FirstOrDefault_on_empty_collection(isAsync);

            AssertSql(
                """
    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    WHERE (
        SELECT TOP 1 IIF(LEN(`o`.`CustomerID`) IS NULL, NULL, CLNG(LEN(`o`.`CustomerID`)))
        FROM `Orders` AS `o`
        WHERE `o`.`CustomerID` = 'John Doe') = 0
    """);
        }

        public override async Task Using_same_parameter_twice_in_query_generates_one_sql_parameter(bool async)
        {
            await base.Using_same_parameter_twice_in_query_generates_one_sql_parameter(async);

            AssertSql(
                """
@i='10'
@i='10'

SELECT `c`.`CustomerID`
FROM `Customers` AS `c`
WHERE (((@i & '') & `c`.`CustomerID`) & (@i & '')) = '10ALFKI10'
""");
        }

        public override async Task Two_parameters_with_same_name_get_uniquified(bool async)
        {
            await base.Two_parameters_with_same_name_get_uniquified(async);

            AssertSql(
                """
@p='11'
@p0='12'

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE ((`c`.`CustomerID` & (@p & '')) & (`c`.`CustomerID` & (@p0 & ''))) = 'ALFKI11ALFKI12'
""");
        }

        public override async Task Where_Queryable_ToList_Count(bool async)
        {
            await base.Where_Queryable_ToList_Count(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
                    FROM `Customers` AS `c`
                    LEFT JOIN `Orders` AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
                    WHERE (
                        SELECT COUNT(*)
                        FROM `Orders` AS `o`
                        WHERE `o`.`CustomerID` = `c`.`CustomerID`) = 0
                    ORDER BY `c`.`CustomerID`
                    """);
        }

        public override async Task Where_Queryable_ToList_Contains(bool async)
        {
            await base.Where_Queryable_ToList_Contains(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `o0`.`CustomerID`, `o0`.`OrderID`
FROM `Customers` AS `c`
LEFT JOIN `Orders` AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE 'ALFKI' IN (
    SELECT `o`.`CustomerID`
    FROM `Orders` AS `o`
    WHERE `o`.`CustomerID` = `c`.`CustomerID`
)
ORDER BY `c`.`CustomerID`
""");
        }

        public override async Task Where_Queryable_ToArray_Count(bool async)
        {
            await base.Where_Queryable_ToArray_Count(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
                    FROM `Customers` AS `c`
                    LEFT JOIN `Orders` AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
                    WHERE (
                        SELECT COUNT(*)
                        FROM `Orders` AS `o`
                        WHERE `o`.`CustomerID` = `c`.`CustomerID`) = 0
                    ORDER BY `c`.`CustomerID`
                    """);
        }

        public override async Task Where_Queryable_ToArray_Contains(bool async)
        {
            await base.Where_Queryable_ToArray_Contains(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `o0`.`CustomerID`, `o0`.`OrderID`
FROM `Customers` AS `c`
LEFT JOIN `Orders` AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE 'ALFKI' IN (
    SELECT `o`.`CustomerID`
    FROM `Orders` AS `o`
    WHERE `o`.`CustomerID` = `c`.`CustomerID`
)
ORDER BY `c`.`CustomerID`
""");
        }

        public override async Task Where_Queryable_AsEnumerable_Count(bool async)
        {
            await base.Where_Queryable_AsEnumerable_Count(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
                    FROM `Customers` AS `c`
                    LEFT JOIN `Orders` AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
                    WHERE (
                        SELECT COUNT(*)
                        FROM `Orders` AS `o`
                        WHERE `o`.`CustomerID` = `c`.`CustomerID`) = 0
                    ORDER BY `c`.`CustomerID`
                    """);
        }

        public override async Task Where_Queryable_AsEnumerable_Contains(bool async)
        {
            await base.Where_Queryable_AsEnumerable_Contains(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `o0`.`CustomerID`, `o0`.`OrderID`
FROM `Customers` AS `c`
LEFT JOIN `Orders` AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE 'ALFKI' IN (
    SELECT `o`.`CustomerID`
    FROM `Orders` AS `o`
    WHERE `o`.`CustomerID` = `c`.`CustomerID`
)
ORDER BY `c`.`CustomerID`
""");
        }

        public override async Task Where_Queryable_AsEnumerable_Contains_negated(bool async)
        {
            await base.Where_Queryable_AsEnumerable_Contains_negated(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `o0`.`CustomerID`, `o0`.`OrderID`
FROM `Customers` AS `c`
LEFT JOIN `Orders` AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE IIF(IIF('ALFKI' IN (
            SELECT `o`.`CustomerID`
            FROM `Orders` AS `o`
            WHERE `o`.`CustomerID` = `c`.`CustomerID`
        ), TRUE, FALSE) IS NULL, FALSE, IIF('ALFKI' IN (
            SELECT `o`.`CustomerID`
            FROM `Orders` AS `o`
            WHERE `o`.`CustomerID` = `c`.`CustomerID`
        ), TRUE, FALSE)) = FALSE
ORDER BY `c`.`CustomerID`
""");
        }

        public override async Task Where_Queryable_ToList_Count_member(bool async)
        {
            await base.Where_Queryable_ToList_Count_member(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
                    FROM `Customers` AS `c`
                    LEFT JOIN `Orders` AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
                    WHERE (
                        SELECT COUNT(*)
                        FROM `Orders` AS `o`
                        WHERE `o`.`CustomerID` = `c`.`CustomerID`) = 0
                    ORDER BY `c`.`CustomerID`
                    """);
        }

        public override async Task Where_Queryable_ToArray_Length_member(bool async)
        {
            await base.Where_Queryable_ToArray_Length_member(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
                    FROM `Customers` AS `c`
                    LEFT JOIN `Orders` AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
                    WHERE (
                        SELECT COUNT(*)
                        FROM `Orders` AS `o`
                        WHERE `o`.`CustomerID` = `c`.`CustomerID`) = 0
                    ORDER BY `c`.`CustomerID`
                    """);
        }

        public override async Task Where_collection_navigation_ToList_Count(bool async)
        {
            await base.Where_collection_navigation_ToList_Count(async);

            AssertSql(
                """
                    SELECT `o`.`OrderID`, `o1`.`OrderID`, `o1`.`ProductID`, `o1`.`Discount`, `o1`.`Quantity`, `o1`.`UnitPrice`
                    FROM `Orders` AS `o`
                    LEFT JOIN `Order Details` AS `o1` ON `o`.`OrderID` = `o1`.`OrderID`
                    WHERE `o`.`OrderID` < 10300 AND (
                        SELECT COUNT(*)
                        FROM `Order Details` AS `o0`
                        WHERE `o`.`OrderID` = `o0`.`OrderID`) = 4
                    ORDER BY `o`.`OrderID`, `o1`.`OrderID`
                    """);
        }

        public override async Task Where_collection_navigation_ToList_Contains(bool async)
        {
            await base.Where_collection_navigation_ToList_Contains(async);

            AssertSql(
                """
@entity_equality_order_OrderID='10248' (Nullable = true)

SELECT `c`.`CustomerID`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM `Customers` AS `c`
LEFT JOIN `Orders` AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE EXISTS (
    SELECT 1
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` = `o`.`CustomerID` AND `o`.`OrderID` = @entity_equality_order_OrderID)
ORDER BY `c`.`CustomerID`, `o0`.`OrderID`
""");
        }

        public override async Task Where_collection_navigation_ToArray_Count(bool async)
        {
            await base.Where_collection_navigation_ToArray_Count(async);

            AssertSql(
                """
                    SELECT `o`.`OrderID`, `o1`.`OrderID`, `o1`.`ProductID`, `o1`.`Discount`, `o1`.`Quantity`, `o1`.`UnitPrice`
                    FROM `Orders` AS `o`
                    LEFT JOIN `Order Details` AS `o1` ON `o`.`OrderID` = `o1`.`OrderID`
                    WHERE `o`.`OrderID` < 10300 AND (
                        SELECT COUNT(*)
                        FROM `Order Details` AS `o0`
                        WHERE `o`.`OrderID` = `o0`.`OrderID`) = 4
                    ORDER BY `o`.`OrderID`, `o1`.`OrderID`
                    """);
        }

        public override async Task Where_collection_navigation_ToArray_Contains(bool async)
        {
            await base.Where_collection_navigation_ToArray_Contains(async);

            AssertSql(
                """
@entity_equality_order_OrderID='10248' (Nullable = true)

SELECT `c`.`CustomerID`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM `Customers` AS `c`
LEFT JOIN `Orders` AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE EXISTS (
    SELECT 1
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` = `o`.`CustomerID` AND `o`.`OrderID` = @entity_equality_order_OrderID)
ORDER BY `c`.`CustomerID`, `o0`.`OrderID`
""");
        }

        public override async Task Where_collection_navigation_AsEnumerable_Count(bool async)
        {
            await base.Where_collection_navigation_AsEnumerable_Count(async);

            AssertSql(
                """
                    SELECT `o`.`OrderID`, `o1`.`OrderID`, `o1`.`ProductID`, `o1`.`Discount`, `o1`.`Quantity`, `o1`.`UnitPrice`
                    FROM `Orders` AS `o`
                    LEFT JOIN `Order Details` AS `o1` ON `o`.`OrderID` = `o1`.`OrderID`
                    WHERE `o`.`OrderID` < 10300 AND (
                        SELECT COUNT(*)
                        FROM `Order Details` AS `o0`
                        WHERE `o`.`OrderID` = `o0`.`OrderID`) = 5
                    ORDER BY `o`.`OrderID`, `o1`.`OrderID`
                    """);
        }

        public override async Task Where_collection_navigation_AsEnumerable_Contains(bool async)
        {
            await base.Where_collection_navigation_AsEnumerable_Contains(async);

            AssertSql(
                """
@entity_equality_order_OrderID='10248' (Nullable = true)

SELECT `c`.`CustomerID`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM `Customers` AS `c`
LEFT JOIN `Orders` AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE EXISTS (
    SELECT 1
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` = `o`.`CustomerID` AND `o`.`OrderID` = @entity_equality_order_OrderID)
ORDER BY `c`.`CustomerID`, `o0`.`OrderID`
""");
        }

        public override async Task Where_collection_navigation_ToList_Count_member(bool async)
        {
            await base.Where_collection_navigation_ToList_Count_member(async);

            AssertSql(
                """
                    SELECT `o`.`OrderID`, `o1`.`OrderID`, `o1`.`ProductID`, `o1`.`Discount`, `o1`.`Quantity`, `o1`.`UnitPrice`
                    FROM `Orders` AS `o`
                    LEFT JOIN `Order Details` AS `o1` ON `o`.`OrderID` = `o1`.`OrderID`
                    WHERE `o`.`OrderID` < 10300 AND (
                        SELECT COUNT(*)
                        FROM `Order Details` AS `o0`
                        WHERE `o`.`OrderID` = `o0`.`OrderID`) = 3
                    ORDER BY `o`.`OrderID`, `o1`.`OrderID`
                    """);
        }

        public override async Task Where_collection_navigation_ToArray_Length_member(bool async)
        {
            await base.Where_collection_navigation_ToArray_Length_member(async);

            AssertSql(
                """
                    SELECT `o`.`OrderID`, `o1`.`OrderID`, `o1`.`ProductID`, `o1`.`Discount`, `o1`.`Quantity`, `o1`.`UnitPrice`
                    FROM `Orders` AS `o`
                    LEFT JOIN `Order Details` AS `o1` ON `o`.`OrderID` = `o1`.`OrderID`
                    WHERE `o`.`OrderID` < 10300 AND (
                        SELECT COUNT(*)
                        FROM `Order Details` AS `o0`
                        WHERE `o`.`OrderID` = `o0`.`OrderID`) = 3
                    ORDER BY `o`.`OrderID`, `o1`.`OrderID`
                    """);
        }

        public override async Task Where_list_object_contains_over_value_type(bool async)
        {
            await base.Where_list_object_contains_over_value_type(async);

            AssertSql(
                """
                    SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
                    FROM `Orders` AS `o`
                    WHERE `o`.`OrderID` IN (10248, 10249)
                    """);
        }

        public override async Task Where_array_of_object_contains_over_value_type(bool async)
        {
            await base.Where_array_of_object_contains_over_value_type(async);

            AssertSql(
                """
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Orders` AS `o`
WHERE `o`.`OrderID` IN (10248, 10249)
""");
        }

        public override async Task Multiple_OrElse_on_same_column_converted_to_in_with_overlap(bool async)
        {
            await base.Multiple_OrElse_on_same_column_converted_to_in_with_overlap(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` IN ('ALFKI', 'ANATR', 'ANTON')
                    """);
        }

        public override async Task Multiple_OrElse_on_same_column_with_null_constant_comparison_converted_to_in(bool async)
        {
            await base.Multiple_OrElse_on_same_column_with_null_constant_comparison_converted_to_in(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`Region` IN ('WA', 'OR', 'BC') OR `c`.`Region` IS NULL
""");
        }

        public override async Task Constant_array_Contains_OrElse_comparison_with_constant_gets_combined_to_one_in(bool async)
        {
            await base.Constant_array_Contains_OrElse_comparison_with_constant_gets_combined_to_one_in(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` IN ('ALFKI', 'ANATR', 'ANTON')
                    """);
        }

        public override async Task Constant_array_Contains_OrElse_comparison_with_constant_gets_combined_to_one_in_with_overlap(bool async)
        {
            await base.Constant_array_Contains_OrElse_comparison_with_constant_gets_combined_to_one_in_with_overlap(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` IN ('ANTON', 'ALFKI', 'ANATR')
""");
        }

        public override async Task Constant_array_Contains_OrElse_another_Contains_gets_combined_to_one_in_with_overlap(bool async)
        {
            await base.Constant_array_Contains_OrElse_another_Contains_gets_combined_to_one_in_with_overlap(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` IN ('ALFKI', 'ANATR', 'ANTON')
                    """);
        }

        public override async Task Constant_array_Contains_AndAlso_another_Contains_gets_combined_to_one_in_with_overlap(bool async)
        {
            await base.Constant_array_Contains_AndAlso_another_Contains_gets_combined_to_one_in_with_overlap(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` NOT IN ('ALFKI', 'ANATR', 'ANTON')
                    """);
        }

        public override async Task Multiple_AndAlso_on_same_column_converted_to_in_using_parameters(bool async)
        {
            await base.Multiple_AndAlso_on_same_column_converted_to_in_using_parameters(async);

            AssertSql(
                """
@prm1='ALFKI' (Size = 5)
@prm2='ANATR' (Size = 5)
@prm3='ANTON' (Size = 5)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` <> @prm1 AND `c`.`CustomerID` <> @prm2 AND `c`.`CustomerID` <> @prm3
""");
        }

        public override async Task Array_of_parameters_Contains_OrElse_comparison_with_constant_gets_combined_to_one_in(bool async)
        {
            await base.Array_of_parameters_Contains_OrElse_comparison_with_constant_gets_combined_to_one_in(async);

            // issue #21462
            AssertSql(
                """
@prm1='ALFKI' (Size = 5)
@prm2='ANATR' (Size = 5)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` IN (@prm1, @prm2, 'ANTON')
""");
        }

        public override async Task Multiple_OrElse_on_same_column_with_null_parameter_comparison_converted_to_in(bool async)
        {
            await base.Multiple_OrElse_on_same_column_with_null_parameter_comparison_converted_to_in(async);

            // issue #21462
            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`Region` IN ('WA', 'OR') OR `c`.`Region` IS NULL OR `c`.`Region` = 'BC'
""");
        }

        public override async Task Parameter_array_Contains_OrElse_comparison_with_constant(bool async)
        {
            await base.Parameter_array_Contains_OrElse_comparison_with_constant(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` IN ('ALFKI', 'ANATR') OR `c`.`CustomerID` = 'ANTON'
                    """);
        }

        public override async Task Parameter_array_Contains_OrElse_comparison_with_parameter_with_overlap(bool async)
        {
            await base.Parameter_array_Contains_OrElse_comparison_with_parameter_with_overlap(async);

            AssertSql(
                """
@prm1='ANTON' (Size = 5)
@prm2='ALFKI' (Size = 5)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = @prm1 OR `c`.`CustomerID` IN ('ALFKI', 'ANATR') OR `c`.`CustomerID` = @prm2
""");
        }

        public override async Task Two_sets_of_comparison_combine_correctly(bool async)
        {
            await base.Two_sets_of_comparison_combine_correctly(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` = 'ANATR'
                    """);
        }

        public override async Task Two_sets_of_comparison_combine_correctly2(bool async)
        {
            await base.Two_sets_of_comparison_combine_correctly2(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`Region` IS NOT NULL AND `c`.`Region` <> 'WA'
""");
        }

        public override async Task Filter_with_EF_Property_using_closure_for_property_name(bool async)
        {
            await base.Filter_with_EF_Property_using_closure_for_property_name(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` = 'ALFKI'
                    """);
        }

        public override async Task Filter_with_EF_Property_using_function_for_property_name(bool async)
        {
            await base.Filter_with_EF_Property_using_function_for_property_name(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE `c`.`CustomerID` = 'ALFKI'
                    """);
        }

        public override async Task FirstOrDefault_over_scalar_projection_compared_to_null(bool async)
        {
            await base.FirstOrDefault_over_scalar_projection_compared_to_null(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE (
                        SELECT TOP 1 `o`.`OrderID`
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID`) IS NULL
                    """);
        }

        public override async Task FirstOrDefault_over_scalar_projection_compared_to_not_null(bool async)
        {
            await base.FirstOrDefault_over_scalar_projection_compared_to_not_null(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE (
                        SELECT TOP 1 `o`.`OrderID`
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID`) IS NOT NULL
                    """);
        }

        public override async Task FirstOrDefault_over_custom_projection_compared_to_null(bool async)
        {
            await base.FirstOrDefault_over_custom_projection_compared_to_null(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE NOT EXISTS (
    SELECT 1
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` = `o`.`CustomerID`)
""");
        }

        public override async Task FirstOrDefault_over_custom_projection_compared_to_not_null(bool async)
        {
            await base.FirstOrDefault_over_custom_projection_compared_to_not_null(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE EXISTS (
                        SELECT 1
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID`)
                    """);
        }

        public override async Task SingleOrDefault_over_custom_projection_compared_to_null(bool async)
        {
            await base.SingleOrDefault_over_custom_projection_compared_to_null(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE NOT EXISTS (
    SELECT 1
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` = `o`.`CustomerID`)
""");
        }

        public override async Task SingleOrDefault_over_custom_projection_compared_to_not_null(bool async)
        {
            await base.SingleOrDefault_over_custom_projection_compared_to_not_null(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE EXISTS (
                        SELECT 1
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID`)
                    """);
        }

        public override async Task LastOrDefault_over_custom_projection_compared_to_null(bool async)
        {
            await base.LastOrDefault_over_custom_projection_compared_to_null(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE NOT EXISTS (
    SELECT 1
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` = `o`.`CustomerID`)
""");
        }

        public override async Task LastOrDefault_over_custom_projection_compared_to_not_null(bool async)
        {
            await base.LastOrDefault_over_custom_projection_compared_to_not_null(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE EXISTS (
                        SELECT 1
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID`)
                    """);
        }

        public override async Task First_over_custom_projection_compared_to_null(bool async)
        {
            await base.First_over_custom_projection_compared_to_null(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE NOT EXISTS (
    SELECT 1
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` = `o`.`CustomerID`)
""");
        }

        public override async Task First_over_custom_projection_compared_to_not_null(bool async)
        {
            await base.First_over_custom_projection_compared_to_not_null(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE EXISTS (
                        SELECT 1
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID`)
                    """);
        }

        public override async Task Single_over_custom_projection_compared_to_null(bool async)
        {
            await base.Single_over_custom_projection_compared_to_null(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE NOT EXISTS (
    SELECT 1
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` = `o`.`CustomerID`)
""");
        }

        public override async Task ElementAt_over_custom_projection_compared_to_not_null(bool async)
        {
            await base.ElementAt_over_custom_projection_compared_to_not_null(async);

            AssertSql(
                """
SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region]
FROM [Customers] AS [c]
WHERE EXISTS (
    SELECT 1
    FROM [Orders] AS [o]
    WHERE [c].[CustomerID] = [o].[CustomerID]
    ORDER BY (SELECT 1)
    OFFSET 3 ROWS)
""");
        }

        public override async Task ElementAtOrDefault_over_custom_projection_compared_to_null(bool async)
        {
            await base.ElementAtOrDefault_over_custom_projection_compared_to_null(async);

            AssertSql(
                """
SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region]
FROM [Customers] AS [c]
WHERE NOT EXISTS (
    SELECT 1
    FROM [Orders] AS [o]
    WHERE [c].[CustomerID] = [o].[CustomerID]
    ORDER BY (SELECT 1)
    OFFSET 7 ROWS)
""");
        }

        public override async Task Single_over_custom_projection_compared_to_not_null(bool async)
        {
            await base.Single_over_custom_projection_compared_to_not_null(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE EXISTS (
                        SELECT 1
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID`)
                    """);
        }

        public override async Task Last_over_custom_projection_compared_to_null(bool async)
        {
            await base.Last_over_custom_projection_compared_to_null(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE NOT EXISTS (
    SELECT 1
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` = `o`.`CustomerID`)
""");
        }

        public override async Task Last_over_custom_projection_compared_to_not_null(bool async)
        {
            await base.Last_over_custom_projection_compared_to_not_null(async);

            AssertSql(
                """
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
                    FROM `Customers` AS `c`
                    WHERE EXISTS (
                        SELECT 1
                        FROM `Orders` AS `o`
                        WHERE `c`.`CustomerID` = `o`.`CustomerID`)
                    """);
        }

        public override async Task Where_Contains_and_comparison(bool async)
        {
            await base.Where_Contains_and_comparison(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` IN ('ALFKI', 'FISSA', 'WHITC') AND `c`.`City` = 'Seattle'
""");
        }

        public override async Task Where_Contains_or_comparison(bool async)
        {
            await base.Where_Contains_or_comparison(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` IN ('ALFKI', 'FISSA') OR `c`.`City` = 'Seattle'
""");
        }

        public override async Task GetType_on_non_hierarchy1(bool async)
        {
            await base.GetType_on_non_hierarchy1(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
""");
        }

        public override async Task GetType_on_non_hierarchy2(bool async)
        {
            await base.GetType_on_non_hierarchy2(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE 0 = 1
""");
        }

        public override async Task GetType_on_non_hierarchy3(bool async)
        {
            await base.GetType_on_non_hierarchy3(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE 0 = 1
""");
        }

        public override async Task GetType_on_non_hierarchy4(bool async)
        {
            await base.GetType_on_non_hierarchy4(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
""");
        }

        public override async Task Case_block_simplification_works_correctly(bool async)
        {
            await base.Case_block_simplification_works_correctly(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE IIF(`c`.`Region` IS NULL, 'OR', `c`.`Region`) = 'OR'
""");
        }

        public override async Task Where_poco_closure(bool async)
        {
            await base.Where_poco_closure(async);

            AssertSql(
                """
@entity_equality_customer_CustomerID='ALFKI' (Size = 5)

SELECT `c`.`CustomerID`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = @entity_equality_customer_CustomerID
""",
                //
                """
@entity_equality_customer_CustomerID='ANATR' (Size = 5)

SELECT `c`.`CustomerID`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = @entity_equality_customer_CustomerID
""");
        }

        public override async Task Filter_with_property_compared_to_null_wrapped_in_explicit_convert_to_object(bool async)
        {
            await base.Filter_with_property_compared_to_null_wrapped_in_explicit_convert_to_object(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`Region` IS NULL
""");
        }

        public override async Task Where_simple_shadow_subquery(bool async)
        {
            await base.Where_simple_shadow_subquery(async);

            AssertSql(
                """
SELECT `e0`.`EmployeeID`, `e0`.`City`, `e0`.`Country`, `e0`.`FirstName`, `e0`.`ReportsTo`, `e0`.`Title`
FROM (
    SELECT TOP @p `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
    FROM `Employees` AS `e`
    ORDER BY `e`.`EmployeeID`
) AS `e0`
WHERE `e0`.`Title` = 'Sales Representative'
ORDER BY `e0`.`EmployeeID`
""");
        }

        public override async Task Where_primitive_tracked2(bool async)
        {
            await base.Where_primitive_tracked2(async);

            AssertSql(
                """
SELECT `e0`.`EmployeeID`, `e0`.`City`, `e0`.`Country`, `e0`.`FirstName`, `e0`.`ReportsTo`, `e0`.`Title`
FROM (
    SELECT TOP @p `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
    FROM `Employees` AS `e`
) AS `e0`
WHERE `e0`.`EmployeeID` = 5
""");
        }

        public override async Task Where_projection(bool async)
        {
            await base.Where_projection(async);

            AssertSql(
                """
SELECT `c`.`CompanyName`
FROM `Customers` AS `c`
WHERE `c`.`City` = 'London'
""");
        }

        public override async Task Where_bool_closure(bool async)
        {
            await base.Where_bool_closure(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE 0 = 1
""",
                //
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = 'ALFKI'
""",
                //
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = 'ALFKI'
""",
                //
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
""");
        }

        public override async Task Where_primitive_tracked(bool async)
        {
            await base.Where_primitive_tracked(async);

            AssertSql(
                """
SELECT `e0`.`EmployeeID`, `e0`.`City`, `e0`.`Country`, `e0`.`FirstName`, `e0`.`ReportsTo`, `e0`.`Title`
FROM (
    SELECT TOP @p `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
    FROM `Employees` AS `e`
) AS `e0`
WHERE `e0`.`EmployeeID` = 5
""");
        }

        public override async Task Where_simple_shadow_projection_mixed(bool async)
        {
            await base.Where_simple_shadow_projection_mixed(async);

            AssertSql(
                """
SELECT `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
FROM `Employees` AS `e`
WHERE `e`.`Title` = 'Sales Representative'
""");
        }

        public override async Task Decimal_cast_to_double_works(bool async)
        {
            await base.Decimal_cast_to_double_works(async);

            AssertSql(
                """
SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
FROM `Products` AS `p`
WHERE IIF(`p`.`UnitPrice` IS NULL, NULL, CDBL(`p`.`UnitPrice`)) > 100.0
""");
        }

        public override async Task Where_bool_client_side_negated(bool async)
        {
            await base.Where_bool_client_side_negated(async);

            AssertSql();
        }

        public override async Task Where_nested_field_access_closure_via_query_cache_error_null(bool async)
        {
            await base.Where_nested_field_access_closure_via_query_cache_error_null(async);

            AssertSql();
        }

        public override async Task Where_nested_field_access_closure_via_query_cache_error_method_null(bool async)
        {
            await base.Where_nested_field_access_closure_via_query_cache_error_method_null(async);

            AssertSql();
        }

        public override async Task Where_client(bool async)
        {
            await base.Where_client(async);

            AssertSql();
        }

        public override async Task Where_subquery_correlated_client_eval(bool async)
        {
            await base.Where_subquery_correlated_client_eval(async);

            AssertSql();
        }

        public override async Task Where_client_and_server_top_level(bool async)
        {
            await base.Where_client_and_server_top_level(async);

            AssertSql();
        }

        public override async Task Where_client_or_server_top_level(bool async)
        {
            await base.Where_client_or_server_top_level(async);

            AssertSql();
        }

        public override async Task Where_client_and_server_non_top_level(bool async)
        {
            await base.Where_client_and_server_non_top_level(async);

            AssertSql();
        }

        public override async Task Where_client_deep_inside_predicate_and_server_top_level(bool async)
        {
            await base.Where_client_deep_inside_predicate_and_server_top_level(async);

            AssertSql();
        }

        public override async Task EF_Constant(bool async)
        {
            await base.EF_Constant(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = 'ALFKI'
""");
        }

        public override async Task EF_Constant_with_subtree(bool async)
        {
            await base.EF_Constant_with_subtree(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = 'ALFKI'
""");
        }

        public override async Task EF_Constant_does_not_parameterized_as_part_of_bigger_subtree(bool async)
        {
            await base.EF_Constant_does_not_parameterized_as_part_of_bigger_subtree(async);

            AssertSql(
                """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = ('ALF' & 'KI')
""");
        }

        public override async Task EF_Constant_with_non_evaluatable_argument_throws(bool async)
        {
            await base.EF_Constant_with_non_evaluatable_argument_throws(async);

            AssertSql();
        }

        public override async Task EF_Parameter(bool async)
        {
            await base.EF_Parameter(async);

            AssertSql(
                """
@p='ALFKI' (Size = 5)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = @p
""");
        }

        public override async Task EF_Parameter_with_subtree(bool async)
        {
            await base.EF_Parameter_with_subtree(async);

            AssertSql(
                """
@p='ALFKI' (Size = 5)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = @p
""");
        }

        public override async Task EF_Parameter_does_not_parameterized_as_part_of_bigger_subtree(bool async)
        {
            await base.EF_Parameter_does_not_parameterized_as_part_of_bigger_subtree(async);

            AssertSql(
                """
@id='ALF' (Size = 5)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = (@id & 'KI')
""");
        }

        public override async Task EF_Parameter_with_non_evaluatable_argument_throws(bool async)
        {
            await base.EF_Parameter_with_non_evaluatable_argument_throws(async);

            AssertSql();
        }

        public override async Task Implicit_cast_in_predicate(bool async)
        {
            await base.Implicit_cast_in_predicate(async);

            AssertSql(
                """
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Orders` AS `o`
WHERE `o`.`CustomerID` = '1337'
""",
                //
                """
@prm_Value='1337' (Size = 5)

SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Orders` AS `o`
WHERE `o`.`CustomerID` = @prm_Value
""",
                //
                """
@ToString='1337' (Size = 5)

SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Orders` AS `o`
WHERE `o`.`CustomerID` = @ToString
""",
                //
                """
@p='1337' (Size = 5)

SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Orders` AS `o`
WHERE `o`.`CustomerID` = @p
""",
                //
                """
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Orders` AS `o`
WHERE `o`.`CustomerID` = '1337'
""");
        }

        public override async Task Interface_casting_though_generic_method(bool async)
        {
            await base.Interface_casting_though_generic_method(async);

            AssertSql(
                """
@id='10252'

SELECT `o`.`OrderID` AS `Id`
FROM `Orders` AS `o`
WHERE `o`.`OrderID` = @id
""",
                //
                """
SELECT `o`.`OrderID` AS `Id`
FROM `Orders` AS `o`
WHERE `o`.`OrderID` = 10252
""",
                //
                """
SELECT `o`.`OrderID` AS `Id`
FROM `Orders` AS `o`
WHERE `o`.`OrderID` = 10252
""",
                //
                """
SELECT `o`.`OrderID` AS `Id`
FROM `Orders` AS `o`
WHERE `o`.`OrderID` = 10252
""");
        }

        public override async Task Simplifiable_coalesce_over_nullable(bool async)
        {
            await base.Simplifiable_coalesce_over_nullable(async);

            AssertSql(
                """
@p='10248'

SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Orders` AS `o`
WHERE `o`.`OrderID` = @p
""");
        }

        #region Evaluation order of predicates

        public override async Task Take_and_Where_evaluation_order(bool async)
        {
            await base.Take_and_Where_evaluation_order(async);

            AssertSql(
                """
SELECT `e0`.`EmployeeID`, `e0`.`City`, `e0`.`Country`, `e0`.`FirstName`, `e0`.`ReportsTo`, `e0`.`Title`
FROM (
    SELECT TOP @p `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`
    FROM `Employees` AS `e`
    ORDER BY `e`.`EmployeeID`
) AS `e0`
WHERE (`e0`.`EmployeeID` MOD 2) = 0
ORDER BY `e0`.`EmployeeID`
""");
        }

        public override async Task Skip_and_Where_evaluation_order(bool async)
        {
            await base.Skip_and_Where_evaluation_order(async);

            AssertSql(
                """
@__p_0='3'

SELECT [e0].[EmployeeID], [e0].[City], [e0].[Country], [e0].[FirstName], [e0].[ReportsTo], [e0].[Title]
FROM (
    SELECT [e].[EmployeeID], [e].[City], [e].[Country], [e].[FirstName], [e].[ReportsTo], [e].[Title]
    FROM [Employees] AS [e]
    ORDER BY [e].[EmployeeID]
    OFFSET @__p_0 ROWS
) AS [e0]
WHERE [e0].[EmployeeID] % 2 = 0
ORDER BY [e0].[EmployeeID]
""");
        }

        public override async Task Take_and_Distinct_evaluation_order(bool async)
        {
            await base.Take_and_Distinct_evaluation_order(async);

            AssertSql(
                """
SELECT DISTINCT `c0`.`ContactTitle`
FROM (
    SELECT TOP @p `c`.`ContactTitle`
    FROM `Customers` AS `c`
    ORDER BY `c`.`ContactTitle`
) AS `c0`
""");
        }

        #endregion Evaluation order of predicates

        private void AssertSql(params string[] expected)
            => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);

        protected override void ClearLog()
            => Fixture.TestSqlLoggerFactory.Clear();
    }
}
