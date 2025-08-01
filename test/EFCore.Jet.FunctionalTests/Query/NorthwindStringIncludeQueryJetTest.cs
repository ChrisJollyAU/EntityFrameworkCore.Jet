﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;
using Xunit.Abstractions;

namespace EntityFrameworkCore.Jet.FunctionalTests.Query;

public class NorthwindStringIncludeQueryJetTest : NorthwindStringIncludeQueryTestBase<
    NorthwindQueryJetFixture<NoopModelCustomizer>>
{
    // ReSharper disable once UnusedParameter.Local
    public NorthwindStringIncludeQueryJetTest(NorthwindQueryJetFixture<NoopModelCustomizer> fixture, ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override async Task Include_collection_with_last_no_orderby(bool async)
    {
        Assert.Equal(
            RelationalStrings.LastUsedWithoutOrderBy(nameof(Enumerable.Last)),
            (await Assert.ThrowsAsync<InvalidOperationException>(
                () => base.Include_collection_with_last_no_orderby(async))).Message);

        AssertSql();
    }

    public override async Task Include_collection_with_filter_reordered(bool async)
    {
        await base.Include_collection_with_filter_reordered(async);

        AssertSql(
"""
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Customers` AS `c`
LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
WHERE `c`.`CustomerID` = 'ALFKI'
ORDER BY `c`.`CustomerID`
""");
    }

    public override async Task Include_collection_order_by_non_key_with_first_or_default(bool async)
    {
        await base.Include_collection_order_by_non_key_with_first_or_default(async);

        AssertSql(
            """
SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM (
    SELECT TOP 1 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    ORDER BY `c`.`CompanyName` DESC
) AS `c0`
LEFT JOIN `Orders` AS `o` ON `c0`.`CustomerID` = `o`.`CustomerID`
ORDER BY `c0`.`CompanyName` DESC, `c0`.`CustomerID`
""");
    }

    public override async Task Include_with_cycle_does_not_throw_when_AsTracking_NoTrackingWithIdentityResolution(bool async)
    {
        await base.Include_with_cycle_does_not_throw_when_AsTracking_NoTrackingWithIdentityResolution(async);

        AssertSql(
"""
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM (`Orders` AS `o`
LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`)
LEFT JOIN `Orders` AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE `o`.`OrderID` < 10800
ORDER BY `o`.`OrderID`, `c`.`CustomerID`
""");
    }

    public override async Task Include_collection_with_filter(bool async)
    {
        await base.Include_collection_with_filter(async);

        AssertSql(
"""
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Customers` AS `c`
LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
WHERE `c`.`CustomerID` = 'ALFKI'
ORDER BY `c`.`CustomerID`
""");
    }

    public override async Task Include_references_then_include_multi_level(bool async)
    {
        await base.Include_references_then_include_multi_level(async);

        AssertSql(
            """
SELECT `o`.`OrderID`, `o`.`ProductID`, `o`.`Discount`, `o`.`Quantity`, `o`.`UnitPrice`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM (`Order Details` AS `o`
INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`)
LEFT JOIN `Customers` AS `c` ON `o0`.`CustomerID` = `c`.`CustomerID`
WHERE (`o`.`OrderID` MOD 23) = 13
""");
    }

    public override async Task Include_collection_order_by_collection_column(bool async)
    {
        await base.Include_collection_order_by_collection_column(async);

        AssertSql(
            """
SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM (
    SELECT TOP 1 `c1`.`CustomerID`, `c1`.`Address`, `c1`.`City`, `c1`.`CompanyName`, `c1`.`ContactName`, `c1`.`ContactTitle`, `c1`.`Country`, `c1`.`Fax`, `c1`.`Phone`, `c1`.`PostalCode`, `c1`.`Region`, `c1`.`c`
    FROM (
        SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, (
            SELECT TOP 1 `o`.`OrderDate`
            FROM `Orders` AS `o`
            WHERE `c`.`CustomerID` = `o`.`CustomerID`
            ORDER BY `o`.`OrderDate` DESC) AS `c`
        FROM `Customers` AS `c`
        WHERE `c`.`CustomerID` LIKE 'W%'
    ) AS `c1`
    ORDER BY `c1`.`c` DESC
) AS `c0`
LEFT JOIN `Orders` AS `o0` ON `c0`.`CustomerID` = `o0`.`CustomerID`
ORDER BY `c0`.`c` DESC, `c0`.`CustomerID`
""");
    }

    public override async Task Include_collection_alias_generation(bool async)
    {
        await base.Include_collection_alias_generation(async);

        AssertSql(
"""
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `o0`.`OrderID`, `o0`.`ProductID`, `o0`.`Discount`, `o0`.`Quantity`, `o0`.`UnitPrice`
FROM `Orders` AS `o`
LEFT JOIN `Order Details` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`
WHERE `o`.`CustomerID` LIKE 'F%'
ORDER BY `o`.`OrderID`, `o0`.`OrderID`
""");
    }

    public override async Task Include_collection_skip_take_no_order_by(bool async)
    {
        await base.Include_collection_skip_take_no_order_by(async);

        AssertSql(
"""
@__p_0='10'
@__p_1='5'

SELECT [t].[CustomerID], [t].[Address], [t].[City], [t].[CompanyName], [t].[ContactName], [t].[ContactTitle], [t].[Country], [t].[Fax], [t].[Phone], [t].[PostalCode], [t].[Region], [o].[OrderID], [o].[CustomerID], [o].[EmployeeID], [o].[OrderDate]
FROM (
    SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region]
    FROM [Customers] AS [c]
    ORDER BY (SELECT 1)
    OFFSET @__p_0 ROWS FETCH NEXT @__p_1 ROWS ONLY
) AS [t]
LEFT JOIN [Orders] AS [o] ON [t].[CustomerID] = [o].[CustomerID]
ORDER BY [t].[CustomerID]
""");
    }

    public override async Task Include_collection_with_cross_join_clause_with_filter(bool async)
    {
        await base.Include_collection_with_cross_join_clause_with_filter(async);

        AssertSql(
"""
SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region], [t].[OrderID], [o0].[OrderID], [o0].[CustomerID], [o0].[EmployeeID], [o0].[OrderDate]
FROM [Customers] AS [c]
CROSS JOIN (
    SELECT TOP(5) [o].[OrderID]
    FROM [Orders] AS [o]
    ORDER BY [o].[OrderID]
) AS [t]
LEFT JOIN [Orders] AS [o0] ON [c].[CustomerID] = [o0].[CustomerID]
WHERE [c].[CustomerID] LIKE N'F%'
ORDER BY [c].[CustomerID], [t].[OrderID]
""");
    }

    public override async Task Join_Include_reference_GroupBy_Select(bool async)
    {
        await base.Join_Include_reference_GroupBy_Select(async);

        AssertSql(
"""
SELECT [t0].[OrderID], [t0].[CustomerID], [t0].[EmployeeID], [t0].[OrderDate], [t0].[CustomerID0], [t0].[Address], [t0].[City], [t0].[CompanyName], [t0].[ContactName], [t0].[ContactTitle], [t0].[Country], [t0].[Fax], [t0].[Phone], [t0].[PostalCode], [t0].[Region]
FROM (
    SELECT [o0].[OrderID]
    FROM [Order Details] AS [o]
    INNER JOIN [Orders] AS [o0] ON [o].[OrderID] = [o0].[OrderID]
    GROUP BY [o0].[OrderID]
) AS [t]
LEFT JOIN (
    SELECT [t1].[OrderID], [t1].[CustomerID], [t1].[EmployeeID], [t1].[OrderDate], [t1].[CustomerID0], [t1].[Address], [t1].[City], [t1].[CompanyName], [t1].[ContactName], [t1].[ContactTitle], [t1].[Country], [t1].[Fax], [t1].[Phone], [t1].[PostalCode], [t1].[Region]
    FROM (
        SELECT [o2].[OrderID], [o2].[CustomerID], [o2].[EmployeeID], [o2].[OrderDate], [c].[CustomerID] AS [CustomerID0], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region], ROW_NUMBER() OVER(PARTITION BY [o2].[OrderID] ORDER BY [o2].[OrderID]) AS [row]
        FROM [Order Details] AS [o1]
        INNER JOIN [Orders] AS [o2] ON [o1].[OrderID] = [o2].[OrderID]
        LEFT JOIN [Customers] AS [c] ON [o2].[CustomerID] = [c].[CustomerID]
    ) AS [t1]
    WHERE [t1].[row] <= 1
) AS [t0] ON [t].[OrderID] = [t0].[OrderID]
""");
    }

    public override async Task Include_multi_level_reference_and_collection_predicate(bool async)
    {
        await base.Include_multi_level_reference_and_collection_predicate(async);

        AssertSql(
            """
SELECT `s`.`OrderID`, `s`.`CustomerID`, `s`.`EmployeeID`, `s`.`OrderDate`, `s`.`CustomerID0`, `s`.`Address`, `s`.`City`, `s`.`CompanyName`, `s`.`ContactName`, `s`.`ContactTitle`, `s`.`Country`, `s`.`Fax`, `s`.`Phone`, `s`.`PostalCode`, `s`.`Region`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM (
    SELECT TOP 2 `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `c`.`CustomerID` AS `CustomerID0`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Orders` AS `o`
    LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
    WHERE `o`.`OrderID` = 10248
) AS `s`
LEFT JOIN `Orders` AS `o0` ON `s`.`CustomerID0` = `o0`.`CustomerID`
ORDER BY `s`.`OrderID`, `s`.`CustomerID0`
""");
    }

    public override async Task Include_references_then_include_collection(bool async)
    {
        await base.Include_references_then_include_collection(async);

        AssertSql(
"""
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM (`Orders` AS `o`
LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`)
LEFT JOIN `Orders` AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE `o`.`CustomerID` LIKE 'F%'
ORDER BY `o`.`OrderID`, `c`.`CustomerID`
""");
    }

    public override async Task Include_collection_on_additional_from_clause_with_filter(bool async)
    {
        await base.Include_collection_on_additional_from_clause_with_filter(async);

        AssertSql(
"""
SELECT [t].[CustomerID], [t].[Address], [t].[City], [t].[CompanyName], [t].[ContactName], [t].[ContactTitle], [t].[Country], [t].[Fax], [t].[Phone], [t].[PostalCode], [t].[Region], [c].[CustomerID], [o].[OrderID], [o].[CustomerID], [o].[EmployeeID], [o].[OrderDate]
FROM [Customers] AS [c]
CROSS JOIN (
    SELECT [c0].[CustomerID], [c0].[Address], [c0].[City], [c0].[CompanyName], [c0].[ContactName], [c0].[ContactTitle], [c0].[Country], [c0].[Fax], [c0].[Phone], [c0].[PostalCode], [c0].[Region]
    FROM [Customers] AS [c0]
    WHERE [c0].[CustomerID] = N'ALFKI'
) AS [t]
LEFT JOIN [Orders] AS [o] ON [t].[CustomerID] = [o].[CustomerID]
ORDER BY [c].[CustomerID], [t].[CustomerID]
""");
    }

    public override async Task Include_duplicate_reference3(bool async)
    {
        await base.Include_duplicate_reference3(async);

        AssertSql(
            """
SELECT `s`.`OrderID`, `s`.`CustomerID`, `s`.`EmployeeID`, `s`.`OrderDate`, `s`.`OrderID0`, `s`.`CustomerID0`, `s`.`EmployeeID0`, `s`.`OrderDate0`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM (
    SELECT `o1`.`OrderID`, `o1`.`CustomerID`, `o1`.`EmployeeID`, `o1`.`OrderDate`, `o2`.`OrderID` AS `OrderID0`, `o2`.`CustomerID` AS `CustomerID0`, `o2`.`EmployeeID` AS `EmployeeID0`, `o2`.`OrderDate` AS `OrderDate0`
    FROM (
        SELECT TOP @p `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
        FROM `Orders` AS `o`
        ORDER BY `o`.`OrderID`
    ) AS `o1`,
    (
        SELECT `o4`.`OrderID`, `o4`.`CustomerID`, `o4`.`EmployeeID`, `o4`.`OrderDate`
        FROM (
            SELECT TOP 2 `o3`.`OrderID`, `o3`.`CustomerID`, `o3`.`EmployeeID`, `o3`.`OrderDate`
            FROM (
                SELECT TOP 2 + 2 `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
                FROM `Orders` AS `o0`
                ORDER BY `o0`.`OrderID`
            ) AS `o3`
            ORDER BY `o3`.`OrderID` DESC
        ) AS `o4`
        ORDER BY `o4`.`OrderID`
    ) AS `o2`
) AS `s`
LEFT JOIN `Customers` AS `c` ON `s`.`CustomerID0` = `c`.`CustomerID`
ORDER BY `s`.`OrderID`
""");
    }

    public override async Task Include_collection_order_by_non_key_with_take(bool async)
    {
        await base.Include_collection_order_by_non_key_with_take(async);

        AssertSql(
            """
SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM (
    SELECT TOP @p `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    ORDER BY `c`.`ContactTitle`
) AS `c0`
LEFT JOIN `Orders` AS `o` ON `c0`.`CustomerID` = `o`.`CustomerID`
ORDER BY `c0`.`ContactTitle`, `c0`.`CustomerID`
""");
    }

    public override async Task Include_collection_then_include_collection_predicate(bool async)
    {
        await base.Include_collection_then_include_collection_predicate(async);

        AssertSql(
            """
SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`, `s`.`OrderID`, `s`.`CustomerID`, `s`.`EmployeeID`, `s`.`OrderDate`, `s`.`OrderID0`, `s`.`ProductID`, `s`.`Discount`, `s`.`Quantity`, `s`.`UnitPrice`
FROM (
    SELECT TOP 2 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    WHERE `c`.`CustomerID` = 'ALFKI'
) AS `c0`
LEFT JOIN (
    SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `o0`.`OrderID` AS `OrderID0`, `o0`.`ProductID`, `o0`.`Discount`, `o0`.`Quantity`, `o0`.`UnitPrice`
    FROM `Orders` AS `o`
    LEFT JOIN `Order Details` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`
) AS `s` ON `c0`.`CustomerID` = `s`.`CustomerID`
ORDER BY `c0`.`CustomerID`, `s`.`OrderID`, `s`.`OrderID0`
""");
    }

    public override async Task Include_collection_take_no_order_by(bool async)
    {
        await base.Include_collection_take_no_order_by(async);

        AssertSql(
            """
SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM (
    SELECT TOP @p `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
) AS `c0`
LEFT JOIN `Orders` AS `o` ON `c0`.`CustomerID` = `o`.`CustomerID`
ORDER BY `c0`.`CustomerID`
""");
    }

    public override async Task Include_collection_principal_already_tracked(bool async)
    {
        await base.Include_collection_principal_already_tracked(async);

        AssertSql(
            """
SELECT TOP 2 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = 'ALFKI'
""",
            //
            """
SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM (
    SELECT TOP 2 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    WHERE `c`.`CustomerID` = 'ALFKI'
) AS `c0`
LEFT JOIN `Orders` AS `o` ON `c0`.`CustomerID` = `o`.`CustomerID`
ORDER BY `c0`.`CustomerID`
""");
    }

    public override async Task Include_collection_OrderBy_object(bool async)
    {
        await base.Include_collection_OrderBy_object(async);

        AssertSql(
"""
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `o0`.`OrderID`, `o0`.`ProductID`, `o0`.`Discount`, `o0`.`Quantity`, `o0`.`UnitPrice`
FROM `Orders` AS `o`
LEFT JOIN `Order Details` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`
WHERE `o`.`OrderID` < 10250
ORDER BY `o`.`OrderID`, `o0`.`OrderID`
""");
    }

    public override async Task Include_duplicate_collection_result_operator2(bool async)
    {
        await base.Include_duplicate_collection_result_operator2(async);

        AssertSql(
"""
@__p_1='1'
@__p_0='2'

SELECT [t1].[CustomerID], [t1].[Address], [t1].[City], [t1].[CompanyName], [t1].[ContactName], [t1].[ContactTitle], [t1].[Country], [t1].[Fax], [t1].[Phone], [t1].[PostalCode], [t1].[Region], [t1].[CustomerID0], [o].[OrderID], [o].[CustomerID], [o].[EmployeeID], [o].[OrderDate], [t1].[Address0], [t1].[City0], [t1].[CompanyName0], [t1].[ContactName0], [t1].[ContactTitle0], [t1].[Country0], [t1].[Fax0], [t1].[Phone0], [t1].[PostalCode0], [t1].[Region0]
FROM (
    SELECT TOP(@__p_1) [t].[CustomerID], [t].[Address], [t].[City], [t].[CompanyName], [t].[ContactName], [t].[ContactTitle], [t].[Country], [t].[Fax], [t].[Phone], [t].[PostalCode], [t].[Region], [t0].[CustomerID] AS [CustomerID0], [t0].[Address] AS [Address0], [t0].[City] AS [City0], [t0].[CompanyName] AS [CompanyName0], [t0].[ContactName] AS [ContactName0], [t0].[ContactTitle] AS [ContactTitle0], [t0].[Country] AS [Country0], [t0].[Fax] AS [Fax0], [t0].[Phone] AS [Phone0], [t0].[PostalCode] AS [PostalCode0], [t0].[Region] AS [Region0]
    FROM (
        SELECT TOP(@__p_0) [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region]
        FROM [Customers] AS [c]
        ORDER BY [c].[CustomerID]
    ) AS [t]
    CROSS JOIN (
        SELECT [c0].[CustomerID], [c0].[Address], [c0].[City], [c0].[CompanyName], [c0].[ContactName], [c0].[ContactTitle], [c0].[Country], [c0].[Fax], [c0].[Phone], [c0].[PostalCode], [c0].[Region]
        FROM [Customers] AS [c0]
        ORDER BY [c0].[CustomerID]
        OFFSET 2 ROWS FETCH NEXT 2 ROWS ONLY
    ) AS [t0]
    ORDER BY [t].[CustomerID]
) AS [t1]
LEFT JOIN [Orders] AS [o] ON [t1].[CustomerID] = [o].[CustomerID]
ORDER BY [t1].[CustomerID], [t1].[CustomerID0]
""");
    }

    public override async Task Repro9735(bool async)
    {
        await base.Repro9735(async);

        AssertSql(
"""
@__p_0='2'

SELECT [t].[OrderID], [t].[CustomerID], [t].[EmployeeID], [t].[OrderDate], [t].[CustomerID0], [o0].[OrderID], [o0].[ProductID], [o0].[Discount], [o0].[Quantity], [o0].[UnitPrice]
FROM (
    SELECT TOP(@__p_0) [o].[OrderID], [o].[CustomerID], [o].[EmployeeID], [o].[OrderDate], [c].[CustomerID] AS [CustomerID0], CASE
        WHEN [c].[CustomerID] IS NOT NULL THEN CAST(1 AS bit)
        ELSE CAST(0 AS bit)
    END AS [c], CASE
        WHEN [c].[CustomerID] IS NOT NULL THEN [c].[CustomerID]
        ELSE N''
    END AS [c0]
    FROM [Orders] AS [o]
    LEFT JOIN [Customers] AS [c] ON [o].[CustomerID] = [c].[CustomerID]
    ORDER BY CASE
        WHEN [c].[CustomerID] IS NOT NULL THEN CAST(1 AS bit)
        ELSE CAST(0 AS bit)
    END, CASE
        WHEN [c].[CustomerID] IS NOT NULL THEN [c].[CustomerID]
        ELSE N''
    END
) AS [t]
LEFT JOIN [Order Details] AS [o0] ON [t].[OrderID] = [o0].[OrderID]
ORDER BY [t].[c], [t].[c0], [t].[OrderID], [t].[CustomerID0], [o0].[OrderID]
""");
    }

    public override async Task Include_collection_single_or_default_no_result(bool async)
    {
        await base.Include_collection_single_or_default_no_result(async);

        AssertSql(
            """
SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM (
    SELECT TOP 2 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    WHERE `c`.`CustomerID` = 'ALFKI ?'
) AS `c0`
LEFT JOIN `Orders` AS `o` ON `c0`.`CustomerID` = `o`.`CustomerID`
ORDER BY `c0`.`CustomerID`
""");
    }

    public override async Task Include_collection_with_cross_apply_with_filter(bool async)
    {
        await base.Include_collection_with_cross_apply_with_filter(async);

        AssertSql(
"""
SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region], [t].[OrderID], [o0].[OrderID], [o0].[CustomerID], [o0].[EmployeeID], [o0].[OrderDate]
FROM [Customers] AS [c]
CROSS APPLY (
    SELECT TOP(5) [o].[OrderID]
    FROM [Orders] AS [o]
    WHERE [o].[CustomerID] = [c].[CustomerID]
    ORDER BY [c].[CustomerID]
) AS [t]
LEFT JOIN [Orders] AS [o0] ON [c].[CustomerID] = [o0].[CustomerID]
WHERE [c].[CustomerID] LIKE N'F%'
ORDER BY [c].[CustomerID], [t].[OrderID]
""");
    }

    public override async Task Include_collection_with_left_join_clause_with_filter(bool async)
    {
        await base.Include_collection_with_left_join_clause_with_filter(async);

        AssertSql(
"""
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o`.`OrderID`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM (`Customers` AS `c`
LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`)
LEFT JOIN `Orders` AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE `c`.`CustomerID` LIKE 'F%'
ORDER BY `c`.`CustomerID`, `o`.`OrderID`
""");
    }

    public override async Task Include_collection_with_right_join_clause_with_filter(bool async)
    {
        await base.Include_collection_with_right_join_clause_with_filter(async);

        AssertSql(
            """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o`.`OrderID`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM (`Customers` AS `c`
RIGHT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`)
LEFT JOIN `Orders` AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE `c`.`CustomerID` LIKE 'F%'
ORDER BY `c`.`CustomerID`, `o`.`OrderID`
""");
    }

    public override async Task Include_duplicate_collection(bool async)
    {
        await base.Include_duplicate_collection(async);

        AssertSql(
"""
@__p_0='2'

SELECT [t].[CustomerID], [t].[Address], [t].[City], [t].[CompanyName], [t].[ContactName], [t].[ContactTitle], [t].[Country], [t].[Fax], [t].[Phone], [t].[PostalCode], [t].[Region], [t0].[CustomerID], [o].[OrderID], [o].[CustomerID], [o].[EmployeeID], [o].[OrderDate], [t0].[Address], [t0].[City], [t0].[CompanyName], [t0].[ContactName], [t0].[ContactTitle], [t0].[Country], [t0].[Fax], [t0].[Phone], [t0].[PostalCode], [t0].[Region], [o0].[OrderID], [o0].[CustomerID], [o0].[EmployeeID], [o0].[OrderDate]
FROM (
    SELECT TOP(@__p_0) [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region]
    FROM [Customers] AS [c]
    ORDER BY [c].[CustomerID]
) AS [t]
CROSS JOIN (
    SELECT [c0].[CustomerID], [c0].[Address], [c0].[City], [c0].[CompanyName], [c0].[ContactName], [c0].[ContactTitle], [c0].[Country], [c0].[Fax], [c0].[Phone], [c0].[PostalCode], [c0].[Region]
    FROM [Customers] AS [c0]
    ORDER BY [c0].[CustomerID]
    OFFSET 2 ROWS FETCH NEXT 2 ROWS ONLY
) AS [t0]
LEFT JOIN [Orders] AS [o] ON [t].[CustomerID] = [o].[CustomerID]
LEFT JOIN [Orders] AS [o0] ON [t0].[CustomerID] = [o0].[CustomerID]
ORDER BY [t].[CustomerID], [t0].[CustomerID], [o].[OrderID]
""");
    }

    public override async Task Include_collection(bool async)
    {
        await base.Include_collection(async);

        AssertSql(
"""
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Customers` AS `c`
LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
WHERE `c`.`CustomerID` LIKE 'F%'
ORDER BY `c`.`CustomerID`
""");
    }

    public override async Task Include_collection_then_include_collection_then_include_reference(bool async)
    {
        await base.Include_collection_then_include_collection_then_include_reference(async);

        AssertSql(
            """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `s0`.`OrderID`, `s0`.`CustomerID`, `s0`.`EmployeeID`, `s0`.`OrderDate`, `s0`.`OrderID0`, `s0`.`ProductID`, `s0`.`Discount`, `s0`.`Quantity`, `s0`.`UnitPrice`, `s0`.`ProductID0`, `s0`.`Discontinued`, `s0`.`ProductName`, `s0`.`SupplierID`, `s0`.`UnitPrice0`, `s0`.`UnitsInStock`
FROM `Customers` AS `c`
LEFT JOIN (
    SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `s`.`OrderID` AS `OrderID0`, `s`.`ProductID`, `s`.`Discount`, `s`.`Quantity`, `s`.`UnitPrice`, `s`.`ProductID0`, `s`.`Discontinued`, `s`.`ProductName`, `s`.`SupplierID`, `s`.`UnitPrice0`, `s`.`UnitsInStock`
    FROM `Orders` AS `o`
    LEFT JOIN (
        SELECT `o0`.`OrderID`, `o0`.`ProductID`, `o0`.`Discount`, `o0`.`Quantity`, `o0`.`UnitPrice`, `p`.`ProductID` AS `ProductID0`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice` AS `UnitPrice0`, `p`.`UnitsInStock`
        FROM `Order Details` AS `o0`
        INNER JOIN `Products` AS `p` ON `o0`.`ProductID` = `p`.`ProductID`
    ) AS `s` ON `o`.`OrderID` = `s`.`OrderID`
) AS `s0` ON `c`.`CustomerID` = `s0`.`CustomerID`
WHERE `c`.`CustomerID` LIKE 'F%'
ORDER BY `c`.`CustomerID`, `s0`.`OrderID`, `s0`.`OrderID0`, `s0`.`ProductID`
""");
    }

    public override async Task Include_reference_GroupBy_Select(bool async)
    {
        await base.Include_reference_GroupBy_Select(async);

        AssertSql(
"""
SELECT [t0].[OrderID], [t0].[CustomerID], [t0].[EmployeeID], [t0].[OrderDate], [t0].[CustomerID0], [t0].[Address], [t0].[City], [t0].[CompanyName], [t0].[ContactName], [t0].[ContactTitle], [t0].[Country], [t0].[Fax], [t0].[Phone], [t0].[PostalCode], [t0].[Region]
FROM (
    SELECT [o].[OrderID]
    FROM [Orders] AS [o]
    WHERE [o].[OrderID] = 10248
    GROUP BY [o].[OrderID]
) AS [t]
LEFT JOIN (
    SELECT [t1].[OrderID], [t1].[CustomerID], [t1].[EmployeeID], [t1].[OrderDate], [t1].[CustomerID0], [t1].[Address], [t1].[City], [t1].[CompanyName], [t1].[ContactName], [t1].[ContactTitle], [t1].[Country], [t1].[Fax], [t1].[Phone], [t1].[PostalCode], [t1].[Region]
    FROM (
        SELECT [o0].[OrderID], [o0].[CustomerID], [o0].[EmployeeID], [o0].[OrderDate], [c].[CustomerID] AS [CustomerID0], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region], ROW_NUMBER() OVER(PARTITION BY [o0].[OrderID] ORDER BY [o0].[OrderID]) AS [row]
        FROM [Orders] AS [o0]
        LEFT JOIN [Customers] AS [c] ON [o0].[CustomerID] = [c].[CustomerID]
        WHERE [o0].[OrderID] = 10248
    ) AS [t1]
    WHERE [t1].[row] <= 1
) AS [t0] ON [t].[OrderID] = [t0].[OrderID]
""");
    }

    public override async Task Include_multiple_references_multi_level_reverse(bool async)
    {
        await base.Include_multiple_references_multi_level_reverse(async);

        AssertSql(
            """
SELECT `o`.`OrderID`, `o`.`ProductID`, `o`.`Discount`, `o`.`Quantity`, `o`.`UnitPrice`, `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM ((`Order Details` AS `o`
INNER JOIN `Products` AS `p` ON `o`.`ProductID` = `p`.`ProductID`)
INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`)
LEFT JOIN `Customers` AS `c` ON `o0`.`CustomerID` = `c`.`CustomerID`
WHERE (`o`.`OrderID` MOD 23) = 13
""");
    }

    public override async Task Include_collection_with_join_clause_with_filter(bool async)
    {
        await base.Include_collection_with_join_clause_with_filter(async);

        AssertSql(
"""
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o`.`OrderID`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM (`Customers` AS `c`
INNER JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`)
LEFT JOIN `Orders` AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE `c`.`CustomerID` LIKE 'F%'
ORDER BY `c`.`CustomerID`, `o`.`OrderID`
""");
    }

    public override async Task Include_collection_OrderBy_list_does_not_contains(bool async)
    {
        await base.Include_collection_OrderBy_list_does_not_contains(async);

        AssertSql(
"""
@__p_1='1'

SELECT [t].[CustomerID], [t].[Address], [t].[City], [t].[CompanyName], [t].[ContactName], [t].[ContactTitle], [t].[Country], [t].[Fax], [t].[Phone], [t].[PostalCode], [t].[Region], [o].[OrderID], [o].[CustomerID], [o].[EmployeeID], [o].[OrderDate]
FROM (
    SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region], CASE
        WHEN [c].[CustomerID] <> N'ALFKI' THEN CAST(1 AS bit)
        ELSE CAST(0 AS bit)
    END AS [c]
    FROM [Customers] AS [c]
    WHERE [c].[CustomerID] LIKE N'A%'
    ORDER BY CASE
        WHEN [c].[CustomerID] <> N'ALFKI' THEN CAST(1 AS bit)
        ELSE CAST(0 AS bit)
    END
    OFFSET @__p_1 ROWS
) AS [t]
LEFT JOIN [Orders] AS [o] ON [t].[CustomerID] = [o].[CustomerID]
ORDER BY [t].[c], [t].[CustomerID]
""");
    }

    public override async Task Include_reference_dependent_already_tracked(bool async)
    {
        await base.Include_reference_dependent_already_tracked(async);

        AssertSql(
"""
SELECT TOP 2 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CustomerID` = 'ALFKI'
""",
//
"""
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Orders` AS `o`
LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
WHERE `o`.`CustomerID` = 'ALFKI'
""");
    }

    public override async Task Include_reference_with_filter(bool async)
    {
        await base.Include_reference_with_filter(async);

        AssertSql(
"""
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Orders` AS `o`
LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
WHERE `o`.`CustomerID` = 'ALFKI'
""");
    }

    public override async Task Include_duplicate_reference(bool async)
    {
        await base.Include_duplicate_reference(async);

        AssertSql(
            """
SELECT `s`.`OrderID`, `s`.`CustomerID`, `s`.`EmployeeID`, `s`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `s`.`OrderID0`, `s`.`CustomerID0`, `s`.`EmployeeID0`, `s`.`OrderDate0`, `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
FROM ((
    SELECT `o1`.`OrderID`, `o1`.`CustomerID`, `o1`.`EmployeeID`, `o1`.`OrderDate`, `o2`.`OrderID` AS `OrderID0`, `o2`.`CustomerID` AS `CustomerID0`, `o2`.`EmployeeID` AS `EmployeeID0`, `o2`.`OrderDate` AS `OrderDate0`
    FROM (
        SELECT TOP @p `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
        FROM `Orders` AS `o`
        ORDER BY `o`.`CustomerID`, `o`.`OrderID`
    ) AS `o1`,
    (
        SELECT `o4`.`OrderID`, `o4`.`CustomerID`, `o4`.`EmployeeID`, `o4`.`OrderDate`
        FROM (
            SELECT TOP 2 `o3`.`OrderID`, `o3`.`CustomerID`, `o3`.`EmployeeID`, `o3`.`OrderDate`
            FROM (
                SELECT TOP 2 + 2 `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
                FROM `Orders` AS `o0`
                ORDER BY `o0`.`CustomerID`, `o0`.`OrderID`
            ) AS `o3`
            ORDER BY `o3`.`CustomerID` DESC, `o3`.`OrderID` DESC
        ) AS `o4`
        ORDER BY `o4`.`CustomerID`, `o4`.`OrderID`
    ) AS `o2`
) AS `s`
LEFT JOIN `Customers` AS `c` ON `s`.`CustomerID` = `c`.`CustomerID`)
LEFT JOIN `Customers` AS `c0` ON `s`.`CustomerID0` = `c0`.`CustomerID`
ORDER BY `s`.`CustomerID`, `s`.`OrderID`
""");
    }

    public override async Task Include_with_complex_projection(bool async)
    {
        await base.Include_with_complex_projection(async);

        AssertSql(
"""
SELECT `c`.`CustomerID` AS `Id`
FROM `Orders` AS `o`
LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
""");
    }

    public override async Task Include_collection_order_by_non_key_with_skip(bool async)
    {
        await base.Include_collection_order_by_non_key_with_skip(async);

        AssertSql(
"""
@__p_0='2'

SELECT [t].[CustomerID], [t].[Address], [t].[City], [t].[CompanyName], [t].[ContactName], [t].[ContactTitle], [t].[Country], [t].[Fax], [t].[Phone], [t].[PostalCode], [t].[Region], [o].[OrderID], [o].[CustomerID], [o].[EmployeeID], [o].[OrderDate]
FROM (
    SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region]
    FROM [Customers] AS [c]
    WHERE [c].[CustomerID] LIKE N'F%'
    ORDER BY [c].[ContactTitle]
    OFFSET @__p_0 ROWS
) AS [t]
LEFT JOIN [Orders] AS [o] ON [t].[CustomerID] = [o].[CustomerID]
ORDER BY [t].[ContactTitle], [t].[CustomerID]
""");
    }

    public override async Task Include_collection_on_join_clause_with_order_by_and_filter(bool async)
    {
        await base.Include_collection_on_join_clause_with_order_by_and_filter(async);

        AssertSql(
"""
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o`.`OrderID`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM (`Customers` AS `c`
INNER JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`)
LEFT JOIN `Orders` AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE `c`.`CustomerID` = 'ALFKI'
ORDER BY `c`.`City`, `c`.`CustomerID`, `o`.`OrderID`
""");
    }

    public override async Task Multi_level_includes_are_applied_with_take(bool async)
    {
        await base.Multi_level_includes_are_applied_with_take(async);

        AssertSql(
            """
SELECT `c1`.`CustomerID`, `s`.`OrderID`, `s`.`CustomerID`, `s`.`EmployeeID`, `s`.`OrderDate`, `s`.`OrderID0`, `s`.`ProductID`, `s`.`Discount`, `s`.`Quantity`, `s`.`UnitPrice`
FROM (
    SELECT TOP 1 `c0`.`CustomerID`
    FROM (
        SELECT TOP @p `c`.`CustomerID`
        FROM `Customers` AS `c`
        WHERE `c`.`CustomerID` LIKE 'A%'
        ORDER BY `c`.`CustomerID`
    ) AS `c0`
    ORDER BY `c0`.`CustomerID`
) AS `c1`
LEFT JOIN (
    SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `o0`.`OrderID` AS `OrderID0`, `o0`.`ProductID`, `o0`.`Discount`, `o0`.`Quantity`, `o0`.`UnitPrice`
    FROM `Orders` AS `o`
    LEFT JOIN `Order Details` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`
) AS `s` ON `c1`.`CustomerID` = `s`.`CustomerID`
ORDER BY `c1`.`CustomerID`, `s`.`OrderID`, `s`.`OrderID0`
""");
    }

    public override async Task Include_multiple_references_then_include_collection_multi_level_reverse(bool async)
    {
        await base.Include_multiple_references_then_include_collection_multi_level_reverse(async);

        AssertSql(
            """
SELECT `o`.`OrderID`, `o`.`ProductID`, `o`.`Discount`, `o`.`Quantity`, `o`.`UnitPrice`, `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o1`.`OrderID`, `o1`.`CustomerID`, `o1`.`EmployeeID`, `o1`.`OrderDate`
FROM (((`Order Details` AS `o`
INNER JOIN `Products` AS `p` ON `o`.`ProductID` = `p`.`ProductID`)
INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`)
LEFT JOIN `Customers` AS `c` ON `o0`.`CustomerID` = `c`.`CustomerID`)
LEFT JOIN `Orders` AS `o1` ON `c`.`CustomerID` = `o1`.`CustomerID`
WHERE (`o`.`OrderID` MOD 23) = 13
ORDER BY `o`.`OrderID`, `o`.`ProductID`, `p`.`ProductID`, `o0`.`OrderID`, `c`.`CustomerID`
""");
    }

    public override async Task Include_collection_then_reference(bool async)
    {
        await base.Include_collection_then_reference(async);

        AssertSql(
            """
SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`, `s`.`OrderID`, `s`.`ProductID`, `s`.`Discount`, `s`.`Quantity`, `s`.`UnitPrice`, `s`.`OrderID0`, `s`.`CustomerID`, `s`.`EmployeeID`, `s`.`OrderDate`
FROM `Products` AS `p`
LEFT JOIN (
    SELECT `o`.`OrderID`, `o`.`ProductID`, `o`.`Discount`, `o`.`Quantity`, `o`.`UnitPrice`, `o0`.`OrderID` AS `OrderID0`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
    FROM `Order Details` AS `o`
    INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`
) AS `s` ON `p`.`ProductID` = `s`.`ProductID`
WHERE (`p`.`ProductID` MOD 17) = 5
ORDER BY `p`.`ProductID`, `s`.`OrderID`, `s`.`ProductID`
""");
    }

    public override async Task Include_collection_order_by_key(bool async)
    {
        await base.Include_collection_order_by_key(async);

        AssertSql(
"""
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Customers` AS `c`
LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
WHERE `c`.`CustomerID` LIKE 'F%'
ORDER BY `c`.`CustomerID`
""");
    }

    public override async Task Include_collection_with_outer_apply_with_filter(bool async)
    {
        await base.Include_collection_with_outer_apply_with_filter(async);

        AssertSql(
"""
SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region], [t].[OrderID], [o0].[OrderID], [o0].[CustomerID], [o0].[EmployeeID], [o0].[OrderDate]
FROM [Customers] AS [c]
OUTER APPLY (
    SELECT TOP(5) [o].[OrderID]
    FROM [Orders] AS [o]
    WHERE [o].[CustomerID] = [c].[CustomerID]
    ORDER BY [c].[CustomerID]
) AS [t]
LEFT JOIN [Orders] AS [o0] ON [c].[CustomerID] = [o0].[CustomerID]
WHERE [c].[CustomerID] LIKE N'F%'
ORDER BY [c].[CustomerID], [t].[OrderID]
""");
    }

    public override async Task Include_collection_on_additional_from_clause2(bool async)
    {
        await base.Include_collection_on_additional_from_clause2(async);

        AssertSql(
            """
SELECT `c1`.`CustomerID`, `c1`.`Address`, `c1`.`City`, `c1`.`CompanyName`, `c1`.`ContactName`, `c1`.`ContactTitle`, `c1`.`Country`, `c1`.`Fax`, `c1`.`Phone`, `c1`.`PostalCode`, `c1`.`Region`
FROM (
    SELECT TOP @p `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    ORDER BY `c`.`CustomerID`
) AS `c1`,
`Customers` AS `c0`
ORDER BY `c1`.`CustomerID`
""");
    }

    public override async Task Include_collection_dependent_already_tracked(bool async)
    {
        await base.Include_collection_dependent_already_tracked(async);

        AssertSql(
            """
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Orders` AS `o`
WHERE `o`.`CustomerID` = 'ALFKI'
""",
            //
            """
SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM (
    SELECT TOP 2 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    WHERE `c`.`CustomerID` = 'ALFKI'
) AS `c0`
LEFT JOIN `Orders` AS `o` ON `c0`.`CustomerID` = `o`.`CustomerID`
ORDER BY `c0`.`CustomerID`
""");
    }

    public override async Task Include_with_complex_projection_does_not_change_ordering_of_projection(bool async)
    {
        await base.Include_with_complex_projection_does_not_change_ordering_of_projection(async);

        AssertSql(
"""
SELECT `c`.`CustomerID` AS `Id`, (
    SELECT COUNT(*)
    FROM `Orders` AS `o0`
    WHERE `c`.`CustomerID` = `o0`.`CustomerID`) AS `TotalOrders`
FROM `Customers` AS `c`
WHERE `c`.`ContactTitle` = 'Owner' AND (
    SELECT COUNT(*)
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` = `o`.`CustomerID`) > 2
ORDER BY `c`.`CustomerID`
""");
    }

    public override async Task Include_multi_level_collection_and_then_include_reference_predicate(bool async)
    {
        await base.Include_multi_level_collection_and_then_include_reference_predicate(async);

        AssertSql(
            """
SELECT `o1`.`OrderID`, `o1`.`CustomerID`, `o1`.`EmployeeID`, `o1`.`OrderDate`, `s`.`OrderID`, `s`.`ProductID`, `s`.`Discount`, `s`.`Quantity`, `s`.`UnitPrice`, `s`.`ProductID0`, `s`.`Discontinued`, `s`.`ProductName`, `s`.`SupplierID`, `s`.`UnitPrice0`, `s`.`UnitsInStock`
FROM (
    SELECT TOP 2 `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
    FROM `Orders` AS `o`
    WHERE `o`.`OrderID` = 10248
) AS `o1`
LEFT JOIN (
    SELECT `o0`.`OrderID`, `o0`.`ProductID`, `o0`.`Discount`, `o0`.`Quantity`, `o0`.`UnitPrice`, `p`.`ProductID` AS `ProductID0`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice` AS `UnitPrice0`, `p`.`UnitsInStock`
    FROM `Order Details` AS `o0`
    INNER JOIN `Products` AS `p` ON `o0`.`ProductID` = `p`.`ProductID`
) AS `s` ON `o1`.`OrderID` = `s`.`OrderID`
ORDER BY `o1`.`OrderID`, `s`.`OrderID`, `s`.`ProductID`
""");
    }

    public override async Task Multi_level_includes_are_applied_with_skip_take(bool async)
    {
        await base.Multi_level_includes_are_applied_with_skip_take(async);

        AssertSql(
            """
SELECT `c2`.`CustomerID`, `s`.`OrderID`, `s`.`CustomerID`, `s`.`EmployeeID`, `s`.`OrderDate`, `s`.`OrderID0`, `s`.`ProductID`, `s`.`Discount`, `s`.`Quantity`, `s`.`UnitPrice`
FROM (
    SELECT TOP 1 `c0`.`CustomerID`
    FROM (
        SELECT TOP @p `c1`.`CustomerID`
        FROM (
            SELECT TOP @p + @p `c`.`CustomerID`
            FROM `Customers` AS `c`
            WHERE `c`.`CustomerID` LIKE 'A%'
            ORDER BY `c`.`CustomerID`
        ) AS `c1`
        ORDER BY `c1`.`CustomerID` DESC
    ) AS `c0`
    ORDER BY `c0`.`CustomerID`
) AS `c2`
LEFT JOIN (
    SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `o0`.`OrderID` AS `OrderID0`, `o0`.`ProductID`, `o0`.`Discount`, `o0`.`Quantity`, `o0`.`UnitPrice`
    FROM `Orders` AS `o`
    LEFT JOIN `Order Details` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`
) AS `s` ON `c2`.`CustomerID` = `s`.`CustomerID`
ORDER BY `c2`.`CustomerID`, `s`.`OrderID`, `s`.`OrderID0`
""");
    }

    public override async Task Include_collection_OrderBy_empty_list_contains(bool async)
    {
        await base.Include_collection_OrderBy_empty_list_contains(async);

        AssertSql(
"""
@__p_1='1'

SELECT [t].[CustomerID], [t].[Address], [t].[City], [t].[CompanyName], [t].[ContactName], [t].[ContactTitle], [t].[Country], [t].[Fax], [t].[Phone], [t].[PostalCode], [t].[Region], [o].[OrderID], [o].[CustomerID], [o].[EmployeeID], [o].[OrderDate]
FROM (
    SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region], CAST(0 AS bit) AS [c]
    FROM [Customers] AS [c]
    WHERE [c].[CustomerID] LIKE N'A%'
    ORDER BY (SELECT 1)
    OFFSET @__p_1 ROWS
) AS [t]
LEFT JOIN [Orders] AS [o] ON [t].[CustomerID] = [o].[CustomerID]
ORDER BY [t].[c], [t].[CustomerID]
""");
    }

    public override async Task Include_references_and_collection_multi_level(bool async)
    {
        await base.Include_references_and_collection_multi_level(async);

        AssertSql(
            """
SELECT `o`.`OrderID`, `o`.`ProductID`, `o`.`Discount`, `o`.`Quantity`, `o`.`UnitPrice`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o1`.`OrderID`, `o1`.`CustomerID`, `o1`.`EmployeeID`, `o1`.`OrderDate`
FROM ((`Order Details` AS `o`
INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`)
LEFT JOIN `Customers` AS `c` ON `o0`.`CustomerID` = `c`.`CustomerID`)
LEFT JOIN `Orders` AS `o1` ON `c`.`CustomerID` = `o1`.`CustomerID`
WHERE (`o`.`OrderID` MOD 23) = 13 AND `o`.`UnitPrice` < 10.0
ORDER BY `o`.`OrderID`, `o`.`ProductID`, `o0`.`OrderID`, `c`.`CustomerID`
""");
    }

    public override async Task Include_collection_force_alias_uniquefication(bool async)
    {
        await base.Include_collection_force_alias_uniquefication(async);

        AssertSql(
"""
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `o0`.`OrderID`, `o0`.`ProductID`, `o0`.`Discount`, `o0`.`Quantity`, `o0`.`UnitPrice`
FROM `Orders` AS `o`
LEFT JOIN `Order Details` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`
WHERE `o`.`CustomerID` = 'ALFKI'
ORDER BY `o`.`OrderID`, `o0`.`OrderID`
""");
    }

    public override async Task Include_collection_with_outer_apply_with_filter_non_equality(bool async)
    {
        await base.Include_collection_with_outer_apply_with_filter_non_equality(async);

        AssertSql(
"""
SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region], [t].[OrderID], [o0].[OrderID], [o0].[CustomerID], [o0].[EmployeeID], [o0].[OrderDate]
FROM [Customers] AS [c]
OUTER APPLY (
    SELECT TOP(5) [o].[OrderID]
    FROM [Orders] AS [o]
    WHERE [o].[CustomerID] <> [c].[CustomerID] OR ([o].[CustomerID] IS NULL)
    ORDER BY [c].[CustomerID]
) AS [t]
LEFT JOIN [Orders] AS [o0] ON [c].[CustomerID] = [o0].[CustomerID]
WHERE [c].[CustomerID] LIKE N'F%'
ORDER BY [c].[CustomerID], [t].[OrderID]
""");
    }

    public override async Task Include_in_let_followed_by_FirstOrDefault(bool async)
    {
        await base.Include_in_let_followed_by_FirstOrDefault(async);

        AssertSql(
"""
SELECT [c].[CustomerID], [t0].[OrderID], [t0].[CustomerID], [t0].[EmployeeID], [t0].[OrderDate], [o0].[OrderID], [o0].[ProductID], [o0].[Discount], [o0].[Quantity], [o0].[UnitPrice]
FROM [Customers] AS [c]
LEFT JOIN (
    SELECT [t].[OrderID], [t].[CustomerID], [t].[EmployeeID], [t].[OrderDate]
    FROM (
        SELECT [o].[OrderID], [o].[CustomerID], [o].[EmployeeID], [o].[OrderDate], ROW_NUMBER() OVER(PARTITION BY [o].[CustomerID] ORDER BY [o].[OrderDate]) AS [row]
        FROM [Orders] AS [o]
    ) AS [t]
    WHERE [t].[row] <= 1
) AS [t0] ON [c].[CustomerID] = [t0].[CustomerID]
LEFT JOIN [Order Details] AS [o0] ON [t0].[OrderID] = [o0].[OrderID]
WHERE [c].[CustomerID] LIKE N'F%'
ORDER BY [c].[CustomerID], [t0].[OrderID], [o0].[OrderID]
""");
    }

    public override async Task Include_references_multi_level(bool async)
    {
        await base.Include_references_multi_level(async);

        AssertSql(
            """
SELECT `o`.`OrderID`, `o`.`ProductID`, `o`.`Discount`, `o`.`Quantity`, `o`.`UnitPrice`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM (`Order Details` AS `o`
INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`)
LEFT JOIN `Customers` AS `c` ON `o0`.`CustomerID` = `c`.`CustomerID`
WHERE (`o`.`OrderID` MOD 23) = 13
""");
    }

    public override async Task Include_collection_then_include_collection(bool async)
    {
        await base.Include_collection_then_include_collection(async);

        AssertSql(
            """
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `s`.`OrderID`, `s`.`CustomerID`, `s`.`EmployeeID`, `s`.`OrderDate`, `s`.`OrderID0`, `s`.`ProductID`, `s`.`Discount`, `s`.`Quantity`, `s`.`UnitPrice`
FROM `Customers` AS `c`
LEFT JOIN (
    SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `o0`.`OrderID` AS `OrderID0`, `o0`.`ProductID`, `o0`.`Discount`, `o0`.`Quantity`, `o0`.`UnitPrice`
    FROM `Orders` AS `o`
    LEFT JOIN `Order Details` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`
) AS `s` ON `c`.`CustomerID` = `s`.`CustomerID`
WHERE `c`.`CustomerID` LIKE 'F%'
ORDER BY `c`.`CustomerID`, `s`.`OrderID`, `s`.`OrderID0`
""");
    }

    public override async Task Include_collection_with_multiple_conditional_order_by(bool async)
    {
        await base.Include_collection_with_multiple_conditional_order_by(async);

        AssertSql(
"""
@__p_0='5'

SELECT [t].[OrderID], [t].[CustomerID], [t].[EmployeeID], [t].[OrderDate], [t].[CustomerID0], [o0].[OrderID], [o0].[ProductID], [o0].[Discount], [o0].[Quantity], [o0].[UnitPrice]
FROM (
    SELECT TOP(@__p_0) [o].[OrderID], [o].[CustomerID], [o].[EmployeeID], [o].[OrderDate], [c].[CustomerID] AS [CustomerID0], CASE
        WHEN [o].[OrderID] > 0 THEN CAST(1 AS bit)
        ELSE CAST(0 AS bit)
    END AS [c], CASE
        WHEN [c].[CustomerID] IS NOT NULL THEN [c].[City]
        ELSE N''
    END AS [c0]
    FROM [Orders] AS [o]
    LEFT JOIN [Customers] AS [c] ON [o].[CustomerID] = [c].[CustomerID]
    ORDER BY CASE
        WHEN [o].[OrderID] > 0 THEN CAST(1 AS bit)
        ELSE CAST(0 AS bit)
    END, CASE
        WHEN [c].[CustomerID] IS NOT NULL THEN [c].[City]
        ELSE N''
    END
) AS [t]
LEFT JOIN [Order Details] AS [o0] ON [t].[OrderID] = [o0].[OrderID]
ORDER BY [t].[c], [t].[c0], [t].[OrderID], [t].[CustomerID0], [o0].[OrderID]
""");
    }

    public override async Task Include_reference_when_entity_in_projection(bool async)
    {
        await base.Include_reference_when_entity_in_projection(async);

        AssertSql(
"""
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Orders` AS `o`
LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
WHERE `o`.`CustomerID` LIKE 'F%'
""");
    }

    public override async Task Include_reference_single_or_default_when_no_result(bool async)
    {
        await base.Include_reference_single_or_default_when_no_result(async);

        AssertSql(
"""
SELECT TOP 2 `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Orders` AS `o`
LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
WHERE `o`.`OrderID` = -1
""");
    }

    public override async Task Include_reference_alias_generation(bool async)
    {
        await base.Include_reference_alias_generation(async);

        AssertSql(
            """
SELECT `o`.`OrderID`, `o`.`ProductID`, `o`.`Discount`, `o`.`Quantity`, `o`.`UnitPrice`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM `Order Details` AS `o`
INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`
WHERE (`o`.`OrderID` MOD 23) = 13
""");
    }

    public override async Task Include_with_cycle_does_not_throw_when_AsNoTrackingWithIdentityResolution(bool async)
    {
        await base.Include_with_cycle_does_not_throw_when_AsNoTrackingWithIdentityResolution(async);

        AssertSql(
"""
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM (`Orders` AS `o`
LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`)
LEFT JOIN `Orders` AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE `o`.`OrderID` < 10800
ORDER BY `o`.`OrderID`, `c`.`CustomerID`
""");
    }

    public override async Task Include_references_then_include_collection_multi_level(bool async)
    {
        await base.Include_references_then_include_collection_multi_level(async);

        AssertSql(
            """
SELECT `o`.`OrderID`, `o`.`ProductID`, `o`.`Discount`, `o`.`Quantity`, `o`.`UnitPrice`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o1`.`OrderID`, `o1`.`CustomerID`, `o1`.`EmployeeID`, `o1`.`OrderDate`
FROM ((`Order Details` AS `o`
INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`)
LEFT JOIN `Customers` AS `c` ON `o0`.`CustomerID` = `c`.`CustomerID`)
LEFT JOIN `Orders` AS `o1` ON `c`.`CustomerID` = `o1`.`CustomerID`
WHERE (`o`.`ProductID` MOD 23) = 17 AND `o`.`Quantity` < 10
ORDER BY `o`.`OrderID`, `o`.`ProductID`, `o0`.`OrderID`, `c`.`CustomerID`
""");
    }

    public override async Task Include_reference_Join_GroupBy_Select(bool async)
    {
        await base.Include_reference_Join_GroupBy_Select(async);

        AssertSql(
"""
SELECT [t0].[OrderID], [t0].[CustomerID], [t0].[EmployeeID], [t0].[OrderDate], [t0].[CustomerID0], [t0].[Address], [t0].[City], [t0].[CompanyName], [t0].[ContactName], [t0].[ContactTitle], [t0].[Country], [t0].[Fax], [t0].[Phone], [t0].[PostalCode], [t0].[Region]
FROM (
    SELECT [o].[OrderID]
    FROM [Orders] AS [o]
    INNER JOIN [Order Details] AS [o0] ON [o].[OrderID] = [o0].[OrderID]
    WHERE [o].[OrderID] = 10248
    GROUP BY [o].[OrderID]
) AS [t]
LEFT JOIN (
    SELECT [t1].[OrderID], [t1].[CustomerID], [t1].[EmployeeID], [t1].[OrderDate], [t1].[CustomerID0], [t1].[Address], [t1].[City], [t1].[CompanyName], [t1].[ContactName], [t1].[ContactTitle], [t1].[Country], [t1].[Fax], [t1].[Phone], [t1].[PostalCode], [t1].[Region]
    FROM (
        SELECT [o1].[OrderID], [o1].[CustomerID], [o1].[EmployeeID], [o1].[OrderDate], [c].[CustomerID] AS [CustomerID0], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region], ROW_NUMBER() OVER(PARTITION BY [o1].[OrderID] ORDER BY [o1].[OrderID]) AS [row]
        FROM [Orders] AS [o1]
        INNER JOIN [Order Details] AS [o2] ON [o1].[OrderID] = [o2].[OrderID]
        LEFT JOIN [Customers] AS [c] ON [o1].[CustomerID] = [c].[CustomerID]
        WHERE [o1].[OrderID] = 10248
    ) AS [t1]
    WHERE [t1].[row] <= 1
) AS [t0] ON [t].[OrderID] = [t0].[OrderID]
""");
    }

    public override async Task Include_collection_when_projection(bool async)
    {
        await base.Include_collection_when_projection(async);

        AssertSql(
"""
SELECT `c`.`CustomerID`
FROM `Customers` AS `c`
""");
    }

    public override async Task Include_reference_SelectMany_GroupBy_Select(bool async)
    {
        await base.Include_reference_SelectMany_GroupBy_Select(async);

        AssertSql(
"""
SELECT [t0].[OrderID], [t0].[CustomerID], [t0].[EmployeeID], [t0].[OrderDate], [t0].[CustomerID0], [t0].[Address], [t0].[City], [t0].[CompanyName], [t0].[ContactName], [t0].[ContactTitle], [t0].[Country], [t0].[Fax], [t0].[Phone], [t0].[PostalCode], [t0].[Region]
FROM (
    SELECT [o].[OrderID]
    FROM [Orders] AS [o]
    CROSS JOIN [Order Details] AS [o0]
    WHERE [o].[OrderID] = 10248
    GROUP BY [o].[OrderID]
) AS [t]
LEFT JOIN (
    SELECT [t1].[OrderID], [t1].[CustomerID], [t1].[EmployeeID], [t1].[OrderDate], [t1].[CustomerID0], [t1].[Address], [t1].[City], [t1].[CompanyName], [t1].[ContactName], [t1].[ContactTitle], [t1].[Country], [t1].[Fax], [t1].[Phone], [t1].[PostalCode], [t1].[Region]
    FROM (
        SELECT [o1].[OrderID], [o1].[CustomerID], [o1].[EmployeeID], [o1].[OrderDate], [c].[CustomerID] AS [CustomerID0], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region], ROW_NUMBER() OVER(PARTITION BY [o1].[OrderID] ORDER BY [o1].[OrderID]) AS [row]
        FROM [Orders] AS [o1]
        CROSS JOIN [Order Details] AS [o2]
        LEFT JOIN [Customers] AS [c] ON [o1].[CustomerID] = [c].[CustomerID]
        WHERE [o1].[OrderID] = 10248
    ) AS [t1]
    WHERE [t1].[row] <= 1
) AS [t0] ON [t].[OrderID] = [t0].[OrderID]
""");
    }

    public override async Task Include_multiple_references_then_include_collection_multi_level(bool async)
    {
        await base.Include_multiple_references_then_include_collection_multi_level(async);

        AssertSql(
            """
SELECT `o`.`OrderID`, `o`.`ProductID`, `o`.`Discount`, `o`.`Quantity`, `o`.`UnitPrice`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `p`.`ProductID`, `o1`.`OrderID`, `o1`.`CustomerID`, `o1`.`EmployeeID`, `o1`.`OrderDate`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
FROM (((`Order Details` AS `o`
INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`)
LEFT JOIN `Customers` AS `c` ON `o0`.`CustomerID` = `c`.`CustomerID`)
INNER JOIN `Products` AS `p` ON `o`.`ProductID` = `p`.`ProductID`)
LEFT JOIN `Orders` AS `o1` ON `c`.`CustomerID` = `o1`.`CustomerID`
WHERE (`o`.`OrderID` MOD 23) = 13
ORDER BY `o`.`OrderID`, `o`.`ProductID`, `o0`.`OrderID`, `c`.`CustomerID`, `p`.`ProductID`
""");
    }

    public override async Task Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join(bool async)
    {
        await base.Outer_identifier_correctly_determined_when_doing_include_on_right_side_of_left_join(async);

        AssertSql(
"""
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `o0`.`OrderID`, `o0`.`ProductID`, `o0`.`Discount`, `o0`.`Quantity`, `o0`.`UnitPrice`
FROM (`Customers` AS `c`
LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`)
LEFT JOIN `Order Details` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`
WHERE `c`.`City` = 'Seattle'
ORDER BY `c`.`CustomerID`, `o`.`OrderID`, `o0`.`OrderID`
""");
    }

    public override async Task SelectMany_Include_reference_GroupBy_Select(bool async)
    {
        await base.SelectMany_Include_reference_GroupBy_Select(async);

        AssertSql(
"""
SELECT [t0].[OrderID], [t0].[CustomerID], [t0].[EmployeeID], [t0].[OrderDate], [t0].[CustomerID0], [t0].[Address], [t0].[City], [t0].[CompanyName], [t0].[ContactName], [t0].[ContactTitle], [t0].[Country], [t0].[Fax], [t0].[Phone], [t0].[PostalCode], [t0].[Region]
FROM (
    SELECT [o0].[OrderID]
    FROM [Order Details] AS [o]
    CROSS JOIN [Orders] AS [o0]
    WHERE [o].[OrderID] = 10248
    GROUP BY [o0].[OrderID]
) AS [t]
LEFT JOIN (
    SELECT [t1].[OrderID], [t1].[CustomerID], [t1].[EmployeeID], [t1].[OrderDate], [t1].[CustomerID0], [t1].[Address], [t1].[City], [t1].[CompanyName], [t1].[ContactName], [t1].[ContactTitle], [t1].[Country], [t1].[Fax], [t1].[Phone], [t1].[PostalCode], [t1].[Region]
    FROM (
        SELECT [o2].[OrderID], [o2].[CustomerID], [o2].[EmployeeID], [o2].[OrderDate], [c].[CustomerID] AS [CustomerID0], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region], ROW_NUMBER() OVER(PARTITION BY [o2].[OrderID] ORDER BY [o2].[OrderID]) AS [row]
        FROM [Order Details] AS [o1]
        CROSS JOIN [Orders] AS [o2]
        LEFT JOIN [Customers] AS [c] ON [o2].[CustomerID] = [c].[CustomerID]
        WHERE [o1].[OrderID] = 10248
    ) AS [t1]
    WHERE [t1].[row] <= 1
) AS [t0] ON [t].[OrderID] = [t0].[OrderID]
""");
    }

    public override async Task Include_collection_SelectMany_GroupBy_Select(bool async)
    {
        await base.Include_collection_SelectMany_GroupBy_Select(async);

        AssertSql(
"""
SELECT [t0].[OrderID], [t0].[CustomerID], [t0].[EmployeeID], [t0].[OrderDate], [t].[OrderID], [t0].[OrderID0], [t0].[ProductID], [o3].[OrderID], [o3].[ProductID], [o3].[Discount], [o3].[Quantity], [o3].[UnitPrice]
FROM (
    SELECT [o].[OrderID]
    FROM [Orders] AS [o]
    CROSS JOIN [Order Details] AS [o0]
    WHERE [o].[OrderID] = 10248
    GROUP BY [o].[OrderID]
) AS [t]
LEFT JOIN (
    SELECT [t1].[OrderID], [t1].[CustomerID], [t1].[EmployeeID], [t1].[OrderDate], [t1].[OrderID0], [t1].[ProductID]
    FROM (
        SELECT [o1].[OrderID], [o1].[CustomerID], [o1].[EmployeeID], [o1].[OrderDate], [o2].[OrderID] AS [OrderID0], [o2].[ProductID], ROW_NUMBER() OVER(PARTITION BY [o1].[OrderID] ORDER BY [o1].[OrderID]) AS [row]
        FROM [Orders] AS [o1]
        CROSS JOIN [Order Details] AS [o2]
        WHERE [o1].[OrderID] = 10248
    ) AS [t1]
    WHERE [t1].[row] <= 1
) AS [t0] ON [t].[OrderID] = [t0].[OrderID]
LEFT JOIN [Order Details] AS [o3] ON [t0].[OrderID] = [o3].[OrderID]
ORDER BY [t].[OrderID], [t0].[OrderID], [t0].[OrderID0], [t0].[ProductID], [o3].[OrderID]
""");
    }

    public override async Task Include_collection_OrderBy_list_contains(bool async)
    {
        await base.Include_collection_OrderBy_list_contains(async);

        AssertSql(
"""
@__p_1='1'

SELECT [t].[CustomerID], [t].[Address], [t].[City], [t].[CompanyName], [t].[ContactName], [t].[ContactTitle], [t].[Country], [t].[Fax], [t].[Phone], [t].[PostalCode], [t].[Region], [o].[OrderID], [o].[CustomerID], [o].[EmployeeID], [o].[OrderDate]
FROM (
    SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region], CASE
        WHEN [c].[CustomerID] = N'ALFKI' THEN CAST(1 AS bit)
        ELSE CAST(0 AS bit)
    END AS [c]
    FROM [Customers] AS [c]
    WHERE [c].[CustomerID] LIKE N'A%'
    ORDER BY CASE
        WHEN [c].[CustomerID] = N'ALFKI' THEN CAST(1 AS bit)
        ELSE CAST(0 AS bit)
    END
    OFFSET @__p_1 ROWS
) AS [t]
LEFT JOIN [Orders] AS [o] ON [t].[CustomerID] = [o].[CustomerID]
ORDER BY [t].[c], [t].[CustomerID]
""");
    }

    public override async Task Multi_level_includes_are_applied_with_skip(bool async)
    {
        await base.Multi_level_includes_are_applied_with_skip(async);

        AssertSql(
            """
SELECT `c1`.`CustomerID`, `s`.`OrderID`, `s`.`CustomerID`, `s`.`EmployeeID`, `s`.`OrderDate`, `s`.`OrderID0`, `s`.`ProductID`, `s`.`Discount`, `s`.`Quantity`, `s`.`UnitPrice`
FROM (
    SELECT TOP 1 `c0`.`CustomerID`
    FROM (
        SELECT TOP @p + 1 `c`.`CustomerID`
        FROM `Customers` AS `c`
        WHERE `c`.`CustomerID` LIKE 'A%'
        ORDER BY `c`.`CustomerID`
    ) AS `c0`
    ORDER BY `c0`.`CustomerID` DESC
) AS `c1`
LEFT JOIN (
    SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `o0`.`OrderID` AS `OrderID0`, `o0`.`ProductID`, `o0`.`Discount`, `o0`.`Quantity`, `o0`.`UnitPrice`
    FROM `Orders` AS `o`
    LEFT JOIN `Order Details` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`
) AS `s` ON `c1`.`CustomerID` = `s`.`CustomerID`
ORDER BY `c1`.`CustomerID`, `s`.`OrderID`, `s`.`OrderID0`
""");
    }

    public override async Task Include_collection_on_additional_from_clause(bool async)
    {
        await base.Include_collection_on_additional_from_clause(async);

        AssertSql(
"""
@__p_0='5'

SELECT [t0].[CustomerID], [t0].[Address], [t0].[City], [t0].[CompanyName], [t0].[ContactName], [t0].[ContactTitle], [t0].[Country], [t0].[Fax], [t0].[Phone], [t0].[PostalCode], [t0].[Region], [t].[CustomerID], [o].[OrderID], [o].[CustomerID], [o].[EmployeeID], [o].[OrderDate]
FROM (
    SELECT TOP(@__p_0) [c].[CustomerID]
    FROM [Customers] AS [c]
    ORDER BY [c].[CustomerID]
) AS [t]
CROSS JOIN (
    SELECT [c0].[CustomerID], [c0].[Address], [c0].[City], [c0].[CompanyName], [c0].[ContactName], [c0].[ContactTitle], [c0].[Country], [c0].[Fax], [c0].[Phone], [c0].[PostalCode], [c0].[Region]
    FROM [Customers] AS [c0]
    WHERE [c0].[CustomerID] LIKE N'F%'
) AS [t0]
LEFT JOIN [Orders] AS [o] ON [t0].[CustomerID] = [o].[CustomerID]
ORDER BY [t].[CustomerID], [t0].[CustomerID]
""");
    }

    public override async Task Include_reference_distinct_is_server_evaluated(bool async)
    {
        await base.Include_reference_distinct_is_server_evaluated(async);

        AssertSql(
            """
SELECT `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM (
    SELECT DISTINCT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
    FROM `Orders` AS `o`
    WHERE `o`.`OrderID` < 10250
) AS `o0`
LEFT JOIN `Customers` AS `c` ON `o0`.`CustomerID` = `c`.`CustomerID`
""");
    }

    public override async Task Include_collection_distinct_is_server_evaluated(bool async)
    {
        await base.Include_collection_distinct_is_server_evaluated(async);

        AssertSql(
            """
SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM (
    SELECT DISTINCT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    WHERE `c`.`CustomerID` LIKE 'A%'
) AS `c0`
LEFT JOIN `Orders` AS `o` ON `c0`.`CustomerID` = `o`.`CustomerID`
ORDER BY `c0`.`CustomerID`
""");
    }

    public override async Task Include_reference_when_projection(bool async)
    {
        await base.Include_reference_when_projection(async);

        AssertSql(
"""
SELECT `o`.`CustomerID`
FROM `Orders` AS `o`
""");
    }

    public override async Task Include_duplicate_collection_result_operator(bool async)
    {
        await base.Include_duplicate_collection_result_operator(async);

        AssertSql(
"""
@__p_1='1'
@__p_0='2'

SELECT [t1].[CustomerID], [t1].[Address], [t1].[City], [t1].[CompanyName], [t1].[ContactName], [t1].[ContactTitle], [t1].[Country], [t1].[Fax], [t1].[Phone], [t1].[PostalCode], [t1].[Region], [t1].[CustomerID0], [o].[OrderID], [o].[CustomerID], [o].[EmployeeID], [o].[OrderDate], [t1].[Address0], [t1].[City0], [t1].[CompanyName0], [t1].[ContactName0], [t1].[ContactTitle0], [t1].[Country0], [t1].[Fax0], [t1].[Phone0], [t1].[PostalCode0], [t1].[Region0], [o0].[OrderID], [o0].[CustomerID], [o0].[EmployeeID], [o0].[OrderDate]
FROM (
    SELECT TOP(@__p_1) [t].[CustomerID], [t].[Address], [t].[City], [t].[CompanyName], [t].[ContactName], [t].[ContactTitle], [t].[Country], [t].[Fax], [t].[Phone], [t].[PostalCode], [t].[Region], [t0].[CustomerID] AS [CustomerID0], [t0].[Address] AS [Address0], [t0].[City] AS [City0], [t0].[CompanyName] AS [CompanyName0], [t0].[ContactName] AS [ContactName0], [t0].[ContactTitle] AS [ContactTitle0], [t0].[Country] AS [Country0], [t0].[Fax] AS [Fax0], [t0].[Phone] AS [Phone0], [t0].[PostalCode] AS [PostalCode0], [t0].[Region] AS [Region0]
    FROM (
        SELECT TOP(@__p_0) [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region]
        FROM [Customers] AS [c]
        ORDER BY [c].[CustomerID]
    ) AS [t]
    CROSS JOIN (
        SELECT [c0].[CustomerID], [c0].[Address], [c0].[City], [c0].[CompanyName], [c0].[ContactName], [c0].[ContactTitle], [c0].[Country], [c0].[Fax], [c0].[Phone], [c0].[PostalCode], [c0].[Region]
        FROM [Customers] AS [c0]
        ORDER BY [c0].[CustomerID]
        OFFSET 2 ROWS FETCH NEXT 2 ROWS ONLY
    ) AS [t0]
    ORDER BY [t].[CustomerID]
) AS [t1]
LEFT JOIN [Orders] AS [o] ON [t1].[CustomerID] = [o].[CustomerID]
LEFT JOIN [Orders] AS [o0] ON [t1].[CustomerID0] = [o0].[CustomerID]
ORDER BY [t1].[CustomerID], [t1].[CustomerID0], [o].[OrderID]
""");
    }

    public override async Task Include_reference(bool async)
    {
        await base.Include_reference(async);

        AssertSql(
"""
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Orders` AS `o`
LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
WHERE `o`.`CustomerID` LIKE 'F%'
""");
    }

    public override async Task Include_multiple_references_and_collection_multi_level_reverse(bool async)
    {
        await base.Include_multiple_references_and_collection_multi_level_reverse(async);

        AssertSql(
            """
SELECT `o`.`OrderID`, `o`.`ProductID`, `o`.`Discount`, `o`.`Quantity`, `o`.`UnitPrice`, `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o1`.`OrderID`, `o1`.`CustomerID`, `o1`.`EmployeeID`, `o1`.`OrderDate`
FROM (((`Order Details` AS `o`
INNER JOIN `Products` AS `p` ON `o`.`ProductID` = `p`.`ProductID`)
INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`)
LEFT JOIN `Customers` AS `c` ON `o0`.`CustomerID` = `c`.`CustomerID`)
LEFT JOIN `Orders` AS `o1` ON `c`.`CustomerID` = `o1`.`CustomerID`
WHERE (`o`.`OrderID` MOD 23) = 13
ORDER BY `o`.`OrderID`, `o`.`ProductID`, `p`.`ProductID`, `o0`.`OrderID`, `c`.`CustomerID`
""");
    }

    public override async Task Include_closes_reader(bool async)
    {
        await base.Include_closes_reader(async);

        AssertSql(
            """
SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM (
    SELECT TOP 1 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
) AS `c0`
LEFT JOIN `Orders` AS `o` ON `c0`.`CustomerID` = `o`.`CustomerID`
ORDER BY `c0`.`CustomerID`
""",
            //
            """
SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
FROM `Products` AS `p`
""");
    }

    public override async Task Include_with_skip(bool async)
    {
        await base.Include_with_skip(async);

        AssertSql(
"""
@__p_0='80'

SELECT [t].[CustomerID], [t].[Address], [t].[City], [t].[CompanyName], [t].[ContactName], [t].[ContactTitle], [t].[Country], [t].[Fax], [t].[Phone], [t].[PostalCode], [t].[Region], [o].[OrderID], [o].[CustomerID], [o].[EmployeeID], [o].[OrderDate]
FROM (
    SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region]
    FROM [Customers] AS [c]
    ORDER BY [c].[ContactName]
    OFFSET @__p_0 ROWS
) AS [t]
LEFT JOIN [Orders] AS [o] ON [t].[CustomerID] = [o].[CustomerID]
ORDER BY [t].[ContactName], [t].[CustomerID]
""");
    }

    public override async Task Include_collection_Join_GroupBy_Select(bool async)
    {
        await base.Include_collection_Join_GroupBy_Select(async);

        AssertSql(
"""
SELECT [t0].[OrderID], [t0].[CustomerID], [t0].[EmployeeID], [t0].[OrderDate], [t].[OrderID], [t0].[OrderID0], [t0].[ProductID], [o3].[OrderID], [o3].[ProductID], [o3].[Discount], [o3].[Quantity], [o3].[UnitPrice]
FROM (
    SELECT [o].[OrderID]
    FROM [Orders] AS [o]
    INNER JOIN [Order Details] AS [o0] ON [o].[OrderID] = [o0].[OrderID]
    WHERE [o].[OrderID] = 10248
    GROUP BY [o].[OrderID]
) AS [t]
LEFT JOIN (
    SELECT [t1].[OrderID], [t1].[CustomerID], [t1].[EmployeeID], [t1].[OrderDate], [t1].[OrderID0], [t1].[ProductID]
    FROM (
        SELECT [o1].[OrderID], [o1].[CustomerID], [o1].[EmployeeID], [o1].[OrderDate], [o2].[OrderID] AS [OrderID0], [o2].[ProductID], ROW_NUMBER() OVER(PARTITION BY [o1].[OrderID] ORDER BY [o1].[OrderID]) AS [row]
        FROM [Orders] AS [o1]
        INNER JOIN [Order Details] AS [o2] ON [o1].[OrderID] = [o2].[OrderID]
        WHERE [o1].[OrderID] = 10248
    ) AS [t1]
    WHERE [t1].[row] <= 1
) AS [t0] ON [t].[OrderID] = [t0].[OrderID]
LEFT JOIN [Order Details] AS [o3] ON [t0].[OrderID] = [o3].[OrderID]
ORDER BY [t].[OrderID], [t0].[OrderID], [t0].[OrderID0], [t0].[ProductID], [o3].[OrderID]
""");
    }

    public override async Task Include_collection_GroupBy_Select(bool async)
    {
        await base.Include_collection_GroupBy_Select(async);

        AssertSql(
"""
SELECT [t0].[OrderID], [t0].[CustomerID], [t0].[EmployeeID], [t0].[OrderDate], [t].[OrderID], [o1].[OrderID], [o1].[ProductID], [o1].[Discount], [o1].[Quantity], [o1].[UnitPrice]
FROM (
    SELECT [o].[OrderID]
    FROM [Orders] AS [o]
    WHERE [o].[OrderID] = 10248
    GROUP BY [o].[OrderID]
) AS [t]
LEFT JOIN (
    SELECT [t1].[OrderID], [t1].[CustomerID], [t1].[EmployeeID], [t1].[OrderDate]
    FROM (
        SELECT [o0].[OrderID], [o0].[CustomerID], [o0].[EmployeeID], [o0].[OrderDate], ROW_NUMBER() OVER(PARTITION BY [o0].[OrderID] ORDER BY [o0].[OrderID]) AS [row]
        FROM [Orders] AS [o0]
        WHERE [o0].[OrderID] = 10248
    ) AS [t1]
    WHERE [t1].[row] <= 1
) AS [t0] ON [t].[OrderID] = [t0].[OrderID]
LEFT JOIN [Order Details] AS [o1] ON [t0].[OrderID] = [o1].[OrderID]
ORDER BY [t].[OrderID], [t0].[OrderID], [o1].[OrderID]
""");
    }

    public override async Task Include_collection_orderby_take(bool async)
    {
        await base.Include_collection_orderby_take(async);

        AssertSql(
            """
SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM (
    SELECT TOP @p `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    ORDER BY `c`.`CustomerID`
) AS `c0`
LEFT JOIN `Orders` AS `o` ON `c0`.`CustomerID` = `o`.`CustomerID`
ORDER BY `c0`.`CustomerID`
""");
    }

    public override async Task Join_Include_collection_GroupBy_Select(bool async)
    {
        await base.Join_Include_collection_GroupBy_Select(async);

        AssertSql(
"""
SELECT [t0].[OrderID], [t0].[CustomerID], [t0].[EmployeeID], [t0].[OrderDate], [t].[OrderID], [t0].[OrderID0], [t0].[ProductID], [o3].[OrderID], [o3].[ProductID], [o3].[Discount], [o3].[Quantity], [o3].[UnitPrice]
FROM (
    SELECT [o0].[OrderID]
    FROM [Order Details] AS [o]
    INNER JOIN [Orders] AS [o0] ON [o].[OrderID] = [o0].[OrderID]
    WHERE [o].[OrderID] = 10248
    GROUP BY [o0].[OrderID]
) AS [t]
LEFT JOIN (
    SELECT [t1].[OrderID], [t1].[CustomerID], [t1].[EmployeeID], [t1].[OrderDate], [t1].[OrderID0], [t1].[ProductID]
    FROM (
        SELECT [o2].[OrderID], [o2].[CustomerID], [o2].[EmployeeID], [o2].[OrderDate], [o1].[OrderID] AS [OrderID0], [o1].[ProductID], ROW_NUMBER() OVER(PARTITION BY [o2].[OrderID] ORDER BY [o2].[OrderID]) AS [row]
        FROM [Order Details] AS [o1]
        INNER JOIN [Orders] AS [o2] ON [o1].[OrderID] = [o2].[OrderID]
        WHERE [o1].[OrderID] = 10248
    ) AS [t1]
    WHERE [t1].[row] <= 1
) AS [t0] ON [t].[OrderID] = [t0].[OrderID]
LEFT JOIN [Order Details] AS [o3] ON [t0].[OrderID] = [o3].[OrderID]
ORDER BY [t].[OrderID], [t0].[OrderID0], [t0].[ProductID], [t0].[OrderID], [o3].[OrderID]
""");
    }

    public override async Task Include_collection_order_by_non_key(bool async)
    {
        await base.Include_collection_order_by_non_key(async);

        AssertSql(
"""
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Customers` AS `c`
LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
WHERE `c`.`CustomerID` LIKE 'F%'
ORDER BY `c`.`PostalCode`, `c`.`CustomerID`
""");
    }

    public override async Task Include_when_result_operator(bool async)
    {
        await base.Include_when_result_operator(async);

        AssertSql(
"""
SELECT IIF(EXISTS (
        SELECT 1
        FROM `Customers` AS `c`), TRUE, FALSE)
FROM (SELECT COUNT(*) FROM `#Dual`)
""");
    }

    public override async Task Include_duplicate_reference2(bool async)
    {
        await base.Include_duplicate_reference2(async);

        AssertSql(
            """
SELECT `s`.`OrderID`, `s`.`CustomerID`, `s`.`EmployeeID`, `s`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `s`.`OrderID0`, `s`.`CustomerID0`, `s`.`EmployeeID0`, `s`.`OrderDate0`
FROM (
    SELECT `o1`.`OrderID`, `o1`.`CustomerID`, `o1`.`EmployeeID`, `o1`.`OrderDate`, `o2`.`OrderID` AS `OrderID0`, `o2`.`CustomerID` AS `CustomerID0`, `o2`.`EmployeeID` AS `EmployeeID0`, `o2`.`OrderDate` AS `OrderDate0`
    FROM (
        SELECT TOP @p `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
        FROM `Orders` AS `o`
        ORDER BY `o`.`OrderID`
    ) AS `o1`,
    (
        SELECT `o4`.`OrderID`, `o4`.`CustomerID`, `o4`.`EmployeeID`, `o4`.`OrderDate`
        FROM (
            SELECT TOP 2 `o3`.`OrderID`, `o3`.`CustomerID`, `o3`.`EmployeeID`, `o3`.`OrderDate`
            FROM (
                SELECT TOP 2 + 2 `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
                FROM `Orders` AS `o0`
                ORDER BY `o0`.`OrderID`
            ) AS `o3`
            ORDER BY `o3`.`OrderID` DESC
        ) AS `o4`
        ORDER BY `o4`.`OrderID`
    ) AS `o2`
) AS `s`
LEFT JOIN `Customers` AS `c` ON `s`.`CustomerID` = `c`.`CustomerID`
ORDER BY `s`.`OrderID`
""");
    }

    public override async Task Include_collection_and_reference(bool async)
    {
        await base.Include_collection_and_reference(async);

        AssertSql(
"""
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `c`.`CustomerID`, `o0`.`OrderID`, `o0`.`ProductID`, `o0`.`Discount`, `o0`.`Quantity`, `o0`.`UnitPrice`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM (`Orders` AS `o`
LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`)
LEFT JOIN `Order Details` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`
WHERE `o`.`CustomerID` LIKE 'F%'
ORDER BY `o`.`OrderID`, `c`.`CustomerID`, `o0`.`OrderID`
""");
    }

    public override async Task Include_multiple_references_multi_level(bool async)
    {
        await base.Include_multiple_references_multi_level(async);

        AssertSql(
            """
SELECT `o`.`OrderID`, `o`.`ProductID`, `o`.`Discount`, `o`.`Quantity`, `o`.`UnitPrice`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
FROM ((`Order Details` AS `o`
INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`)
LEFT JOIN `Customers` AS `c` ON `o0`.`CustomerID` = `c`.`CustomerID`)
INNER JOIN `Products` AS `p` ON `o`.`ProductID` = `p`.`ProductID`
WHERE (`o`.`OrderID` MOD 23) = 13
""");
    }

    public override async Task Include_references_and_collection_multi_level_predicate(bool async)
    {
        await base.Include_references_and_collection_multi_level_predicate(async);

        AssertSql(
"""
SELECT `o`.`OrderID`, `o`.`ProductID`, `o`.`Discount`, `o`.`Quantity`, `o`.`UnitPrice`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o1`.`OrderID`, `o1`.`CustomerID`, `o1`.`EmployeeID`, `o1`.`OrderDate`
FROM ((`Order Details` AS `o`
INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`)
LEFT JOIN `Customers` AS `c` ON `o0`.`CustomerID` = `c`.`CustomerID`)
LEFT JOIN `Orders` AS `o1` ON `c`.`CustomerID` = `o1`.`CustomerID`
WHERE `o`.`OrderID` = 10248
ORDER BY `o`.`OrderID`, `o`.`ProductID`, `o0`.`OrderID`, `c`.`CustomerID`
""");
    }

    public override async Task SelectMany_Include_collection_GroupBy_Select(bool async)
    {
        await base.SelectMany_Include_collection_GroupBy_Select(async);

        AssertSql(
"""
SELECT [t0].[OrderID], [t0].[CustomerID], [t0].[EmployeeID], [t0].[OrderDate], [t].[OrderID], [t0].[OrderID0], [t0].[ProductID], [o3].[OrderID], [o3].[ProductID], [o3].[Discount], [o3].[Quantity], [o3].[UnitPrice]
FROM (
    SELECT [o0].[OrderID]
    FROM [Order Details] AS [o]
    CROSS JOIN [Orders] AS [o0]
    WHERE [o].[OrderID] = 10248
    GROUP BY [o0].[OrderID]
) AS [t]
LEFT JOIN (
    SELECT [t1].[OrderID], [t1].[CustomerID], [t1].[EmployeeID], [t1].[OrderDate], [t1].[OrderID0], [t1].[ProductID]
    FROM (
        SELECT [o2].[OrderID], [o2].[CustomerID], [o2].[EmployeeID], [o2].[OrderDate], [o1].[OrderID] AS [OrderID0], [o1].[ProductID], ROW_NUMBER() OVER(PARTITION BY [o2].[OrderID] ORDER BY [o2].[OrderID]) AS [row]
        FROM [Order Details] AS [o1]
        CROSS JOIN [Orders] AS [o2]
        WHERE [o1].[OrderID] = 10248
    ) AS [t1]
    WHERE [t1].[row] <= 1
) AS [t0] ON [t].[OrderID] = [t0].[OrderID]
LEFT JOIN [Order Details] AS [o3] ON [t0].[OrderID] = [o3].[OrderID]
ORDER BY [t].[OrderID], [t0].[OrderID0], [t0].[ProductID], [t0].[OrderID], [o3].[OrderID]
""");
    }

    public override async Task Include_collection_with_last(bool async)
    {
        await base.Include_collection_with_last(async);

        AssertSql(
            """
SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM (
    SELECT TOP 1 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    ORDER BY `c`.`CompanyName` DESC
) AS `c0`
LEFT JOIN `Orders` AS `o` ON `c0`.`CustomerID` = `o`.`CustomerID`
ORDER BY `c0`.`CompanyName` DESC, `c0`.`CustomerID`
""");
    }

    public override async Task Include_collection_OrderBy_empty_list_does_not_contains(bool async)
    {
        await base.Include_collection_OrderBy_empty_list_does_not_contains(async);

        AssertSql(
"""
@__p_1='1'

SELECT [t].[CustomerID], [t].[Address], [t].[City], [t].[CompanyName], [t].[ContactName], [t].[ContactTitle], [t].[Country], [t].[Fax], [t].[Phone], [t].[PostalCode], [t].[Region], [o].[OrderID], [o].[CustomerID], [o].[EmployeeID], [o].[OrderDate]
FROM (
    SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region], CAST(1 AS bit) AS [c]
    FROM [Customers] AS [c]
    WHERE [c].[CustomerID] LIKE N'A%'
    ORDER BY (SELECT 1)
    OFFSET @__p_1 ROWS
) AS [t]
LEFT JOIN [Orders] AS [o] ON [t].[CustomerID] = [o].[CustomerID]
ORDER BY [t].[c], [t].[CustomerID]
""");
    }

    public override async Task Include_multiple_references_then_include_multi_level_reverse(bool async)
    {
        await base.Include_multiple_references_then_include_multi_level_reverse(async);

        AssertSql(
            """
SELECT `o`.`OrderID`, `o`.`ProductID`, `o`.`Discount`, `o`.`Quantity`, `o`.`UnitPrice`, `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM ((`Order Details` AS `o`
INNER JOIN `Products` AS `p` ON `o`.`ProductID` = `p`.`ProductID`)
INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`)
LEFT JOIN `Customers` AS `c` ON `o0`.`CustomerID` = `c`.`CustomerID`
WHERE (`o`.`OrderID` MOD 23) = 13
""");
    }

    public override async Task Include_reference_and_collection(bool async)
    {
        await base.Include_reference_and_collection(async);

        AssertSql(
"""
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o0`.`OrderID`, `o0`.`ProductID`, `o0`.`Discount`, `o0`.`Quantity`, `o0`.`UnitPrice`
FROM (`Orders` AS `o`
LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`)
LEFT JOIN `Order Details` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`
WHERE `o`.`CustomerID` LIKE 'F%'
ORDER BY `o`.`OrderID`, `c`.`CustomerID`, `o0`.`OrderID`
""");
    }

    public override async Task Include_is_not_ignored_when_projection_contains_client_method_and_complex_expression(bool async)
    {
        await base.Include_is_not_ignored_when_projection_contains_client_method_and_complex_expression(async);

        AssertSql(
"""
SELECT IIF(`e0`.`EmployeeID` IS NOT NULL, TRUE, FALSE), `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`, `e0`.`EmployeeID`, `e0`.`City`, `e0`.`Country`, `e0`.`FirstName`, `e0`.`ReportsTo`, `e0`.`Title`
FROM `Employees` AS `e`
LEFT JOIN `Employees` AS `e0` ON `e`.`ReportsTo` = `e0`.`EmployeeID`
WHERE `e`.`EmployeeID` IN (1, 2)
ORDER BY `e`.`EmployeeID`
""");
    }

    public override async Task Include_reference_with_filter_reordered(bool async)
    {
        await base.Include_reference_with_filter_reordered(async);

        AssertSql(
"""
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Orders` AS `o`
LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
WHERE `o`.`CustomerID` = 'ALFKI'
""");
    }

    public override async Task Include_collection_order_by_subquery(bool async)
    {
        await base.Include_collection_order_by_subquery(async);

        AssertSql(
"""
SELECT [t].[CustomerID], [t].[Address], [t].[City], [t].[CompanyName], [t].[ContactName], [t].[ContactTitle], [t].[Country], [t].[Fax], [t].[Phone], [t].[PostalCode], [t].[Region], [o0].[OrderID], [o0].[CustomerID], [o0].[EmployeeID], [o0].[OrderDate]
FROM (
    SELECT TOP(1) [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region], (
        SELECT TOP(1) [o].[OrderDate]
        FROM [Orders] AS [o]
        WHERE [c].[CustomerID] = [o].[CustomerID]
        ORDER BY [o].[EmployeeID]) AS [c]
    FROM [Customers] AS [c]
    WHERE [c].[CustomerID] = N'ALFKI'
    ORDER BY (
        SELECT TOP(1) [o].[OrderDate]
        FROM [Orders] AS [o]
        WHERE [c].[CustomerID] = [o].[CustomerID]
        ORDER BY [o].[EmployeeID])
) AS [t]
LEFT JOIN [Orders] AS [o0] ON [t].[CustomerID] = [o0].[CustomerID]
ORDER BY [t].[c], [t].[CustomerID]
""");
    }

    public override async Task Include_reference_and_collection_order_by(bool async)
    {
        await base.Include_reference_and_collection_order_by(async);

        AssertSql(
"""
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM (`Orders` AS `o`
LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`)
LEFT JOIN `Orders` AS `o0` ON `c`.`CustomerID` = `o0`.`CustomerID`
WHERE `o`.`CustomerID` LIKE 'F%'
ORDER BY `o`.`OrderID`, `c`.`CustomerID`
""");
    }

    public override async Task Then_include_collection_order_by_collection_column(bool async)
    {
        await base.Then_include_collection_order_by_collection_column(async);

        AssertSql(
            """
SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`, `s`.`OrderID`, `s`.`CustomerID`, `s`.`EmployeeID`, `s`.`OrderDate`, `s`.`OrderID0`, `s`.`ProductID`, `s`.`Discount`, `s`.`Quantity`, `s`.`UnitPrice`
FROM (
    SELECT TOP 1 `c1`.`CustomerID`, `c1`.`Address`, `c1`.`City`, `c1`.`CompanyName`, `c1`.`ContactName`, `c1`.`ContactTitle`, `c1`.`Country`, `c1`.`Fax`, `c1`.`Phone`, `c1`.`PostalCode`, `c1`.`Region`, `c1`.`c`
    FROM (
        SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, (
            SELECT TOP 1 `o`.`OrderDate`
            FROM `Orders` AS `o`
            WHERE `c`.`CustomerID` = `o`.`CustomerID`
            ORDER BY `o`.`OrderDate` DESC) AS `c`
        FROM `Customers` AS `c`
        WHERE `c`.`CustomerID` LIKE 'W%'
    ) AS `c1`
    ORDER BY `c1`.`c` DESC
) AS `c0`
LEFT JOIN (
    SELECT `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`, `o1`.`OrderID` AS `OrderID0`, `o1`.`ProductID`, `o1`.`Discount`, `o1`.`Quantity`, `o1`.`UnitPrice`
    FROM `Orders` AS `o0`
    LEFT JOIN `Order Details` AS `o1` ON `o0`.`OrderID` = `o1`.`OrderID`
) AS `s` ON `c0`.`CustomerID` = `s`.`CustomerID`
ORDER BY `c0`.`c` DESC, `c0`.`CustomerID`, `s`.`OrderID`, `s`.`OrderID0`
""");
    }

    public override async Task Include_multiple_references_then_include_multi_level(bool async)
    {
        await base.Include_multiple_references_then_include_multi_level(async);

        AssertSql(
            """
SELECT `o`.`OrderID`, `o`.`ProductID`, `o`.`Discount`, `o`.`Quantity`, `o`.`UnitPrice`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
FROM ((`Order Details` AS `o`
INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`)
LEFT JOIN `Customers` AS `c` ON `o0`.`CustomerID` = `c`.`CustomerID`)
INNER JOIN `Products` AS `p` ON `o`.`ProductID` = `p`.`ProductID`
WHERE (`o`.`OrderID` MOD 23) = 13
""");
    }

    public override async Task Include_collection_skip_no_order_by(bool async)
    {
        await base.Include_collection_skip_no_order_by(async);

        AssertSql(
"""
@__p_0='10'

SELECT [t].[CustomerID], [t].[Address], [t].[City], [t].[CompanyName], [t].[ContactName], [t].[ContactTitle], [t].[Country], [t].[Fax], [t].[Phone], [t].[PostalCode], [t].[Region], [o].[OrderID], [o].[CustomerID], [o].[EmployeeID], [o].[OrderDate]
FROM (
    SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region]
    FROM [Customers] AS [c]
    ORDER BY (SELECT 1)
    OFFSET @__p_0 ROWS
) AS [t]
LEFT JOIN [Orders] AS [o] ON [t].[CustomerID] = [o].[CustomerID]
ORDER BY [t].[CustomerID]
""");
    }

    public override async Task Include_multi_level_reference_then_include_collection_predicate(bool async)
    {
        await base.Include_multi_level_reference_then_include_collection_predicate(async);

        AssertSql(
            """
SELECT `s`.`OrderID`, `s`.`CustomerID`, `s`.`EmployeeID`, `s`.`OrderDate`, `s`.`CustomerID0`, `s`.`Address`, `s`.`City`, `s`.`CompanyName`, `s`.`ContactName`, `s`.`ContactTitle`, `s`.`Country`, `s`.`Fax`, `s`.`Phone`, `s`.`PostalCode`, `s`.`Region`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
FROM (
    SELECT TOP 2 `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `c`.`CustomerID` AS `CustomerID0`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Orders` AS `o`
    LEFT JOIN `Customers` AS `c` ON `o`.`CustomerID` = `c`.`CustomerID`
    WHERE `o`.`OrderID` = 10248
) AS `s`
LEFT JOIN `Orders` AS `o0` ON `s`.`CustomerID0` = `o0`.`CustomerID`
ORDER BY `s`.`OrderID`, `s`.`CustomerID0`
""");
    }

    public override async Task Include_multiple_references_and_collection_multi_level(bool async)
    {
        await base.Include_multiple_references_and_collection_multi_level(async);

        AssertSql(
            """
SELECT `o`.`OrderID`, `o`.`ProductID`, `o`.`Discount`, `o`.`Quantity`, `o`.`UnitPrice`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `p`.`ProductID`, `o1`.`OrderID`, `o1`.`CustomerID`, `o1`.`EmployeeID`, `o1`.`OrderDate`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
FROM (((`Order Details` AS `o`
INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`)
LEFT JOIN `Customers` AS `c` ON `o0`.`CustomerID` = `c`.`CustomerID`)
INNER JOIN `Products` AS `p` ON `o`.`ProductID` = `p`.`ProductID`)
LEFT JOIN `Orders` AS `o1` ON `c`.`CustomerID` = `o1`.`CustomerID`
WHERE (`o`.`OrderID` MOD 23) = 13
ORDER BY `o`.`OrderID`, `o`.`ProductID`, `o0`.`OrderID`, `c`.`CustomerID`, `p`.`ProductID`
""");
    }

    public override async Task Include_where_skip_take_projection(bool async)
    {
        await base.Include_where_skip_take_projection(async);

        AssertSql(
            """
SELECT `o0`.`CustomerID`
FROM (
    SELECT TOP @p0 `o2`.`OrderID`, `o2`.`ProductID`
    FROM (
        SELECT TOP @p + @p0 `o`.`OrderID`, `o`.`ProductID`
        FROM `Order Details` AS `o`
        WHERE `o`.`Quantity` = 10
        ORDER BY `o`.`OrderID`, `o`.`ProductID`
    ) AS `o2`
    ORDER BY `o2`.`OrderID` DESC, `o2`.`ProductID` DESC
) AS `o1`
INNER JOIN `Orders` AS `o0` ON `o1`.`OrderID` = `o0`.`OrderID`
ORDER BY `o1`.`OrderID`, `o1`.`ProductID`
""");
    }

    public override async Task Include_with_take(bool async)
    {
        await base.Include_with_take(async);

        AssertSql(
            """
SELECT `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM (
    SELECT TOP @p `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    ORDER BY `c`.`ContactName` DESC
) AS `c0`
LEFT JOIN `Orders` AS `o` ON `c0`.`CustomerID` = `o`.`CustomerID`
ORDER BY `c0`.`ContactName` DESC, `c0`.`CustomerID`
""");
    }

    public override async Task Include_multiple_references(bool async)
    {
        await base.Include_multiple_references(async);

        AssertSql(
            """
SELECT `o`.`OrderID`, `o`.`ProductID`, `o`.`Discount`, `o`.`Quantity`, `o`.`UnitPrice`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`, `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`
FROM (`Order Details` AS `o`
INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`)
INNER JOIN `Products` AS `p` ON `o`.`ProductID` = `p`.`ProductID`
WHERE (`o`.`OrderID` MOD 23) = 13
""");
    }

    public override async Task Include_list(bool async)
    {
        await base.Include_list(async);

        AssertSql(
            """
SELECT `p`.`ProductID`, `p`.`Discontinued`, `p`.`ProductName`, `p`.`SupplierID`, `p`.`UnitPrice`, `p`.`UnitsInStock`, `s`.`OrderID`, `s`.`ProductID`, `s`.`Discount`, `s`.`Quantity`, `s`.`UnitPrice`, `s`.`OrderID0`, `s`.`CustomerID`, `s`.`EmployeeID`, `s`.`OrderDate`
FROM `Products` AS `p`
LEFT JOIN (
    SELECT `o`.`OrderID`, `o`.`ProductID`, `o`.`Discount`, `o`.`Quantity`, `o`.`UnitPrice`, `o0`.`OrderID` AS `OrderID0`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
    FROM `Order Details` AS `o`
    INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`
) AS `s` ON `p`.`ProductID` = `s`.`ProductID`
WHERE (`p`.`ProductID` MOD 17) = 5 AND `p`.`UnitPrice` < 20.0
ORDER BY `p`.`ProductID`, `s`.`OrderID`, `s`.`ProductID`
""");
    }

    public override async Task Include_empty_reference_sets_IsLoaded(bool async)
    {
        await base.Include_empty_reference_sets_IsLoaded(async);

        AssertSql(
"""
SELECT TOP 1 `e`.`EmployeeID`, `e`.`City`, `e`.`Country`, `e`.`FirstName`, `e`.`ReportsTo`, `e`.`Title`, `e0`.`EmployeeID`, `e0`.`City`, `e0`.`Country`, `e0`.`FirstName`, `e0`.`ReportsTo`, `e0`.`Title`
FROM `Employees` AS `e`
LEFT JOIN `Employees` AS `e0` ON `e`.`ReportsTo` = `e0`.`EmployeeID`
WHERE `e0`.`EmployeeID` IS NULL
""");
    }

    public override async Task Include_references_then_include_collection_multi_level_predicate(bool async)
    {
        await base.Include_references_then_include_collection_multi_level_predicate(async);

        AssertSql(
"""
SELECT `o`.`OrderID`, `o`.`ProductID`, `o`.`Discount`, `o`.`Quantity`, `o`.`UnitPrice`, `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o1`.`OrderID`, `o1`.`CustomerID`, `o1`.`EmployeeID`, `o1`.`OrderDate`
FROM ((`Order Details` AS `o`
INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`)
LEFT JOIN `Customers` AS `c` ON `o0`.`CustomerID` = `c`.`CustomerID`)
LEFT JOIN `Orders` AS `o1` ON `c`.`CustomerID` = `o1`.`CustomerID`
WHERE `o`.`OrderID` = 10248
ORDER BY `o`.`OrderID`, `o`.`ProductID`, `o0`.`OrderID`, `c`.`CustomerID`
""");
    }

    public override async Task Include_collection_with_conditional_order_by(bool async)
    {
        await base.Include_collection_with_conditional_order_by(async);

        AssertSql(
"""
SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Customers` AS `c`
LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
WHERE `c`.`CustomerID` LIKE 'F%'
ORDER BY IIF(`c`.`CustomerID` LIKE 'S%', 1, 2), `c`.`CustomerID`
""");
    }

    public override async Task Include_non_existing_navigation(bool async)
    {
        await base.Include_non_existing_navigation(async);

        AssertSql();
    }

    public override async Task Include_property(bool async)
    {
        await base.Include_property(async);

        AssertSql();
    }

    public override async Task Include_property_after_navigation(bool async)
    {
        await base.Include_property_after_navigation(async);

        AssertSql();
    }

    public override async Task Include_property_expression_invalid(bool async)
    {
        await base.Include_property_expression_invalid(async);

        AssertSql();
    }

    public override async Task Then_include_property_expression_invalid(bool async)
    {
        await base.Then_include_property_expression_invalid(async);

        AssertSql();
    }

    public override async Task Filtered_include_with_multiple_ordering(bool async)
    {
        await base.Filtered_include_with_multiple_ordering(async);

        AssertSql();
    }

    public override async Task Include_specified_on_non_entity_not_supported(bool async)
    {
        await base.Include_specified_on_non_entity_not_supported(async);

        AssertSql();
    }

    public override async Task Include_collection_with_client_filter(bool async)
    {
        await base.Include_collection_with_client_filter(async);

        AssertSql();
    }

    private void AssertSql(params string[] expected)
        => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);
}
