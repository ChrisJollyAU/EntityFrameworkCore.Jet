﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using EntityFrameworkCore.Jet.FunctionalTests.TestUtilities;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;
using Xunit.Abstractions;

namespace EntityFrameworkCore.Jet.FunctionalTests.Query;

public class Ef6GroupByJetTest : Ef6GroupByTestBase<Ef6GroupByJetTest.Ef6GroupByJetFixture>
{
    public Ef6GroupByJetTest(Ef6GroupByJetFixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override async Task GroupBy_is_optimized_when_projecting_group_key(bool async)
    {
        await base.GroupBy_is_optimized_when_projecting_group_key(async);

        AssertSql(
"""
SELECT `a`.`FirstName`
FROM `ArubaOwner` AS `a`
GROUP BY `a`.`FirstName`
""");

        // EF6 SQL:
        // @"SELECT
        // [Distinct1].[FirstName] AS [FirstName]
        // FROM ( SELECT DISTINCT
        // [Extent1].[FirstName] AS [FirstName]
        // FROM [dbo].[ArubaOwners] AS [Extent1]
        // )  AS [Distinct1]";
    }

    public override async Task GroupBy_is_optimized_when_projecting_group_count(bool async)
    {
        await base.GroupBy_is_optimized_when_projecting_group_count(async);

        AssertSql(
"""
SELECT COUNT(*)
FROM `ArubaOwner` AS `a`
GROUP BY `a`.`FirstName`
""");

        // EF6 SQL:
        // @"SELECT
        // [GroupBy1].[A1] AS [C1]
        // FROM ( SELECT
        // 	[Extent1].[FirstName] AS [K1],
        // 	COUNT(1) AS [A1]
        // 	FROM [dbo].[ArubaOwners] AS [Extent1]
        // 	GROUP BY [Extent1].[FirstName]
        // )  AS [GroupBy1]";
    }

    public override async Task GroupBy_is_optimized_when_projecting_expression_containing_group_key(bool async)
    {
        await base.GroupBy_is_optimized_when_projecting_expression_containing_group_key(async);

        AssertSql(
"""
SELECT `a`.`Id` * 2
FROM `ArubaOwner` AS `a`
GROUP BY `a`.`Id`
""");

        // EF6 SQL:
        // @"SELECT
        // [Extent1].[Id] * 2 AS [C1]
        // FROM [dbo].[ArubaOwners] AS [Extent1]";
    }

    public override async Task GroupBy_is_optimized_when_projecting_aggregate_on_the_group(bool async)
    {
        await base.GroupBy_is_optimized_when_projecting_aggregate_on_the_group(async);

        AssertSql(
"""
SELECT MAX(`a`.`Id`)
FROM `ArubaOwner` AS `a`
GROUP BY `a`.`FirstName`
""");

        // EF6 SQL:
        // @"SELECT
        // [GroupBy1].[A1] AS [C1]
        // FROM ( SELECT
        // 	[Extent1].[FirstName] AS [K1],
        // 	MAX([Extent1].[Id]) AS [A1]
        // 	FROM [dbo].[ArubaOwners] AS [Extent1]
        // 	GROUP BY [Extent1].[FirstName]
        // )  AS [GroupBy1]";
    }

    public override async Task GroupBy_is_optimized_when_projecting_anonymous_type_containing_group_key_and_group_aggregate(bool async)
    {
        await base.GroupBy_is_optimized_when_projecting_anonymous_type_containing_group_key_and_group_aggregate(async);

        AssertSql(
"""
SELECT `a`.`FirstName` AS `Key`, MAX(`a`.`Id`) AS `Aggregate`
FROM `ArubaOwner` AS `a`
GROUP BY `a`.`FirstName`
""");

        // EF6 SQL:
        // @"SELECT
        // 1 AS [C1],
        // [GroupBy1].[K1] AS [FirstName],
        // [GroupBy1].[A1] AS [C2]
        // FROM ( SELECT
        // 	[Extent1].[FirstName] AS [K1],
        // 	MAX([Extent1].[Id]) AS [A1]
        // 	FROM [dbo].[ArubaOwners] AS [Extent1]
        // 	GROUP BY [Extent1].[FirstName]
        // )  AS [GroupBy1]";
    }

    public override async Task GroupBy_is_optimized_when_projecting_anonymous_type_containing_group_key_and_multiple_group_aggregates(
        bool async)
    {
        await base.GroupBy_is_optimized_when_projecting_anonymous_type_containing_group_key_and_multiple_group_aggregates(async);

        AssertSql(
"""
SELECT `a`.`FirstName` AS `key1`, MAX(`a`.`Id`) AS `max`, MIN(`a`.`Id` + 2) AS `min`
FROM `ArubaOwner` AS `a`
GROUP BY `a`.`FirstName`
""");

        // EF6 SQL:
        // @"SELECT
        // 1 AS [C1],
        // [GroupBy1].[K1] AS [FirstName],
        // [GroupBy1].[A1] AS [C2],
        // [GroupBy1].[A2] AS [C3]
        // FROM ( SELECT
        // 	[Extent1].[K1] AS [K1],
        // 	MAX([Extent1].[A1_0]) AS [A1],
        // 	MIN([Extent1].[A2_0]) AS [A2]
        // 	FROM ( SELECT
        // 		[Extent1].[FirstName] AS [K1],
        // 		[Extent1].[Id] AS [A1_0],
        // 		[Extent1].[Id] + 2 AS [A2_0]
        // 		FROM [dbo].[ArubaOwners] AS [Extent1]
        // 	)  AS [Extent1]
        // 	GROUP BY [K1]
        // )  AS [GroupBy1]";
    }

    public override async Task GroupBy_is_optimized_when_projecting_conditional_expression_containing_group_key(bool async)
    {
        await base.GroupBy_is_optimized_when_projecting_conditional_expression_containing_group_key(async);

        AssertSql(
            """
SELECT IIF(`a`.`FirstName` IS NULL, 'is null', 'not null') AS `keyIsNull`, FALSE AS `logicExpression`
FROM `ArubaOwner` AS `a`
GROUP BY `a`.`FirstName`
""");

        // EF6 SQL:
        // @"SELECT
        // 1 AS [C1],
        // CASE WHEN ([Distinct1].[FirstName] IS NULL) THEN N'is null' ELSE N'not null' END AS [C2],
        // CASE WHEN (((@p__linq__0 = 1) AND (@p__linq__1 = 1)) OR ((@p__linq__2 = 1) AND (@p__linq__3 = 1))) THEN cast(1 as bit) WHEN ( NOT (((@p__linq__0 = 1) AND (@p__linq__1 = 1)) OR ((@p__linq__2 = 1) AND (@p__linq__3 = 1)))) THEN cast(0 as bit) END AS [C3]
        // FROM ( SELECT DISTINCT
        // 	[Extent1].[FirstName] AS [FirstName]
        // 	FROM [dbo].[ArubaOwners] AS [Extent1]
        // )  AS [Distinct1]";
    }

    public override async Task GroupBy_is_optimized_when_filtering_and_projecting_anonymous_type_with_group_key_and_function_aggregate(
        bool async)
    {
        await base.GroupBy_is_optimized_when_filtering_and_projecting_anonymous_type_with_group_key_and_function_aggregate(async);

        AssertSql(
"""
SELECT `a`.`FirstName`, AVG(CDBL(`a`.`Id`)) AS `AverageId`
FROM `ArubaOwner` AS `a`
WHERE `a`.`Id` > 5
GROUP BY `a`.`FirstName`
""");

        // EF6 SQL:
        // @"SELECT
        // 1 AS [C1],
        // [GroupBy1].[K1] AS [FirstName],
        // [GroupBy1].[A1] AS [C2]
        // FROM ( SELECT
        // 	[Extent1].[FirstName] AS [K1],
        // 	AVG( CAST( [Extent1].[Id] AS float)) AS [A1]
        // 	FROM [dbo].[ArubaOwners] AS [Extent1]
        // 	WHERE [Extent1].[Id] > 5
        // 	GROUP BY [Extent1].[FirstName]
        // )  AS [GroupBy1]";
    }

    public override async Task GroupBy_is_optimized_when_projecting_function_aggregate_with_expression(bool async)
    {
        await base.GroupBy_is_optimized_when_projecting_function_aggregate_with_expression(async);

        AssertSql(
"""
SELECT MAX(`a`.`Id` * 2)
FROM `ArubaOwner` AS `a`
GROUP BY `a`.`FirstName`
""");

        // EF6 SQL:
        // @"SELECT
        // [GroupBy1].[A1] AS [C1]
        // FROM ( SELECT
        // 	[Extent1].[K1] AS [K1],
        // 	MAX([Extent1].[A1_0]) AS [A1]
        // 	FROM ( SELECT
        // 		[Extent1].[FirstName] AS [K1],
        // 		[Extent1].[Id] * 2 AS [A1_0]
        // 		FROM [dbo].[ArubaOwners] AS [Extent1]
        // 	)  AS [Extent1]
        // 	GROUP BY [K1]
        // )  AS [GroupBy1]";
    }

    public override async Task GroupBy_is_optimized_when_projecting_expression_with_multiple_function_aggregates(bool async)
    {
        await base.GroupBy_is_optimized_when_projecting_expression_with_multiple_function_aggregates(async);

        AssertSql(
"""
SELECT MAX(`a`.`Id`) - MIN(`a`.`Id`) AS `maxMinusMin`
FROM `ArubaOwner` AS `a`
GROUP BY `a`.`FirstName`
""");

        // EF6 SQL:
        // @"SELECT
        // 1 AS [C1],
        // [GroupBy1].[A1] - [GroupBy1].[A2] AS [C2]
        // FROM ( SELECT
        // 	[Extent1].[FirstName] AS [K1],
        // 	MAX([Extent1].[Id]) AS [A1],
        // 	MIN([Extent1].[Id]) AS [A2]
        // 	FROM [dbo].[ArubaOwners] AS [Extent1]
        // 	GROUP BY [Extent1].[FirstName]
        // )  AS [GroupBy1]";
    }

    public override async Task GroupBy_is_optimized_when_grouping_by_row_and_projecting_column_of_the_key_row(bool async)
    {
        await base.GroupBy_is_optimized_when_grouping_by_row_and_projecting_column_of_the_key_row(async);

        AssertSql(
"""
SELECT `a`.`FirstName`
FROM `ArubaOwner` AS `a`
WHERE `a`.`Id` < 4
GROUP BY `a`.`FirstName`
""");

        // EF6 SQL:
        // @"SELECT
        // [Distinct1].[FirstName] AS [FirstName]
        // FROM ( SELECT DISTINCT
        // 	[Extent1].[FirstName] AS [FirstName]
        // 	FROM [dbo].[ArubaOwners] AS [Extent1]
        // 	WHERE [Extent1].[Id] < 4
        // )  AS [Distinct1]";
    }

    public override async Task Grouping_by_all_columns_doesnt_produce_a_groupby_statement(bool async)
    {
        await base.Grouping_by_all_columns_doesnt_produce_a_groupby_statement(async);

        AssertSql(
"""
SELECT `a`.`Id`, `a`.`Alias`, `a`.`FirstName`, `a`.`LastName`
FROM `ArubaOwner` AS `a`
GROUP BY `a`.`Id`, `a`.`Alias`, `a`.`FirstName`, `a`.`LastName`
""");
    }

    public override async Task Grouping_by_all_columns_with_aggregate_function_works_1(bool async)
    {
        await base.Grouping_by_all_columns_with_aggregate_function_works_1(async);

        AssertSql(
"""
SELECT COUNT(*)
FROM `ArubaOwner` AS `a`
GROUP BY `a`.`Id`, `a`.`FirstName`, `a`.`LastName`, `a`.`Alias`
""");

        // EF6 SQL:
        // @"SELECT
        //     (SELECT
        //         COUNT(1) AS [A1]
        //         FROM [dbo].[ArubaOwners] AS [Extent2]
        //         WHERE [Extent1].[Id] = [Extent2].[Id]) AS [C1]
        //     FROM [dbo].[ArubaOwners] AS [Extent1]";
    }

    public override async Task Grouping_by_all_columns_with_aggregate_function_works_2(bool async)
    {
        await base.Grouping_by_all_columns_with_aggregate_function_works_2(async);

        AssertSql(
"""
SELECT COUNT(*)
FROM `ArubaOwner` AS `a`
GROUP BY `a`.`Id`, `a`.`Alias`, `a`.`FirstName`, `a`.`LastName`
""");
    }

    public override async Task Grouping_by_all_columns_with_aggregate_function_works_3(bool async)
    {
        await base.Grouping_by_all_columns_with_aggregate_function_works_3(async);

        AssertSql(
"""
SELECT COUNT(*)
FROM `ArubaOwner` AS `a`
GROUP BY `a`.`Id`, `a`.`Alias`, `a`.`FirstName`, `a`.`LastName`
""");
    }

    public override async Task Grouping_by_all_columns_with_aggregate_function_works_4(bool async)
    {
        await base.Grouping_by_all_columns_with_aggregate_function_works_4(async);

        AssertSql(
"""
SELECT COUNT(*) AS `Count`
FROM `ArubaOwner` AS `a`
GROUP BY `a`.`Id`, `a`.`Alias`, `a`.`FirstName`, `a`.`LastName`
""");
    }

    public override async Task Grouping_by_all_columns_with_aggregate_function_works_5(bool async)
    {
        await base.Grouping_by_all_columns_with_aggregate_function_works_5(async);

        AssertSql(
"""
SELECT `a`.`Id`, COUNT(*) AS `Count`
FROM `ArubaOwner` AS `a`
GROUP BY `a`.`Id`, `a`.`Alias`, `a`.`FirstName`, `a`.`LastName`
""");
    }

    public override async Task Grouping_by_all_columns_with_aggregate_function_works_6(bool async)
    {
        await base.Grouping_by_all_columns_with_aggregate_function_works_6(async);

        AssertSql(
"""
SELECT `a`.`Id`, `a`.`Alias`, COUNT(*) AS `Count`
FROM `ArubaOwner` AS `a`
GROUP BY `a`.`Id`, `a`.`Alias`, `a`.`FirstName`, `a`.`LastName`
""");
    }

    public override async Task Grouping_by_all_columns_with_aggregate_function_works_7(bool async)
    {
        await base.Grouping_by_all_columns_with_aggregate_function_works_7(async);

        AssertSql(
"""
SELECT COUNT(*)
FROM `ArubaOwner` AS `a`
GROUP BY `a`.`Id`, `a`.`Alias`, `a`.`FirstName`, `a`.`LastName`
""");
    }

    public override async Task Grouping_by_all_columns_with_aggregate_function_works_8(bool async)
    {
        await base.Grouping_by_all_columns_with_aggregate_function_works_8(async);

        AssertSql(
"""
SELECT `a`.`Id`, COUNT(*) AS `Count`
FROM `ArubaOwner` AS `a`
GROUP BY `a`.`Id`, `a`.`Alias`, `a`.`FirstName`, `a`.`LastName`
""");
    }

    public override async Task Grouping_by_all_columns_with_aggregate_function_works_9(bool async)
    {
        await base.Grouping_by_all_columns_with_aggregate_function_works_9(async);

        AssertSql(
"""
SELECT `a`.`Id`, `a`.`Alias`, COUNT(*) AS `Count`
FROM `ArubaOwner` AS `a`
GROUP BY `a`.`Id`, `a`.`Alias`, `a`.`FirstName`, `a`.`LastName`
""");
    }

    public override async Task Grouping_by_all_columns_with_aggregate_function_works_10(bool async)
    {
        await base.Grouping_by_all_columns_with_aggregate_function_works_10(async);

        AssertSql(
"""
SELECT `a`.`Id`, IIF(SUM(`a`.`Id`) IS NULL, 0, SUM(`a`.`Id`)) AS `Sum`, COUNT(*) AS `Count`
FROM `ArubaOwner` AS `a`
GROUP BY `a`.`Id`, `a`.`Alias`, `a`.`FirstName`, `a`.`LastName`
""");
    }

    public override async Task GroupBy_Simple_1_from_LINQ_101(bool async)
    {
        await base.GroupBy_Simple_1_from_LINQ_101(async);

        AssertSql();
    }

    public override async Task GroupBy_Simple_2_from_LINQ_101(bool async)
    {
        await base.GroupBy_Simple_2_from_LINQ_101(async);

        AssertSql();
    }

    public override async Task GroupBy_Simple_3_from_LINQ_101(bool async)
    {
        await base.GroupBy_Simple_3_from_LINQ_101(async);

        AssertSql();
    }

    public override async Task GroupBy_Nested_from_LINQ_101(bool async)
    {
        await base.GroupBy_Nested_from_LINQ_101(async);

        AssertSql();
    }

    public override async Task Any_Grouped_from_LINQ_101(bool async)
    {
        await base.Any_Grouped_from_LINQ_101(async);

        AssertSql();
    }

    public override async Task All_Grouped_from_LINQ_101(bool async)
    {
        await base.All_Grouped_from_LINQ_101(async);

        AssertSql();
    }

    public override async Task Min_Elements_from_LINQ_101(bool async)
    {
        await base.Min_Elements_from_LINQ_101(async);

        AssertSql();
    }

    public override async Task Max_Elements_from_LINQ_101(bool async)
    {
        await base.Max_Elements_from_LINQ_101(async);

        AssertSql();
    }

    public override async Task Group_Join_from_LINQ_101(bool async)
    {
        await base.Group_Join_from_LINQ_101(async);

        AssertSql(
            """
SELECT `c`.`Id`, `c`.`CompanyName`, `c`.`Region`, `s`.`Id`, `s`.`CustomerId`, `s`.`OrderDate`, `s`.`Total`, `s`.`Id0`
FROM `CustomerForLinq` AS `c`
LEFT JOIN (
    SELECT `o`.`Id`, `o`.`CustomerId`, `o`.`OrderDate`, `o`.`Total`, `c0`.`Id` AS `Id0`
    FROM `OrderForLinq` AS `o`
    LEFT JOIN `CustomerForLinq` AS `c0` ON `o`.`CustomerId` = `c0`.`Id`
) AS `s` ON `c`.`Id` = `s`.`Id0`
ORDER BY `c`.`Id`, `s`.`Id`
""");
    }

    public override async Task Whats_new_2021_sample_3(bool async)
    {
        await base.Whats_new_2021_sample_3(async);

        AssertSql(
"""
SELECT (
    SELECT TOP(1) [p1].[LastName]
    FROM [Person] AS [p1]
    WHERE [p1].[MiddleInitial] = N'Q' AND [p1].[Age] = 20 AND ([p].[LastName] = [p1].[LastName] OR (([p].[LastName] IS NULL) AND ([p1].[LastName] IS NULL))))
FROM [Person] AS [p]
WHERE [p].[MiddleInitial] = N'Q' AND [p].[Age] = 20
GROUP BY [p].[LastName]
ORDER BY CAST(LEN((
    SELECT TOP(1) [p1].[LastName]
    FROM [Person] AS [p1]
    WHERE [p1].[MiddleInitial] = N'Q' AND [p1].[Age] = 20 AND ([p].[LastName] = [p1].[LastName] OR (([p].[LastName] IS NULL) AND ([p1].[LastName] IS NULL))))) AS int)
""");
    }

    public override async Task Whats_new_2021_sample_5(bool async)
    {
        await base.Whats_new_2021_sample_5(async);

        AssertSql(
            """
SELECT `p2`.`c`
FROM (
    SELECT (
        SELECT TOP 1 `p0`.`LastName`
        FROM `Person` AS `p0`
        WHERE `p`.`FirstName` = `p0`.`FirstName` OR (`p`.`FirstName` IS NULL AND `p0`.`FirstName` IS NULL)) AS `c`, `p`.`FirstName`
    FROM `Person` AS `p`
    GROUP BY `p`.`FirstName`
) AS `p2`
ORDER BY `p2`.`c`
""");
    }

    public override async Task Whats_new_2021_sample_6(bool async)
    {
        await base.Whats_new_2021_sample_6(async);

        AssertSql(
            """
SELECT `p2`.`c`
FROM (
    SELECT (
        SELECT TOP 1 `p0`.`MiddleInitial`
        FROM `Person` AS `p0`
        WHERE `p0`.`Age` = 20 AND `p`.`Id` = `p0`.`Id`) AS `c`, `p`.`Id`
    FROM `Person` AS `p`
    WHERE `p`.`Age` = 20
    GROUP BY `p`.`Id`
) AS `p2`
ORDER BY `p2`.`c`
""");
    }

    public override async Task Whats_new_2021_sample_14(bool async)
    {
        await base.Whats_new_2021_sample_14(async);

        AssertSql();
    }

    public override async Task Whats_new_2021_sample_15(bool async)
    {
        await base.Whats_new_2021_sample_15(async);

        AssertSql(
"""
SELECT [t0].[Id], [t0].[Age], [t0].[FirstName], [t0].[LastName], [t0].[MiddleInitial]
FROM (
    SELECT [f].[Id], [f].[Size]
    FROM [Person] AS [p]
    LEFT JOIN [Feet] AS [f] ON [p].[Id] = [f].[Id]
    GROUP BY [f].[Id], [f].[Size]
) AS [t]
LEFT JOIN (
    SELECT [t1].[Id], [t1].[Age], [t1].[FirstName], [t1].[LastName], [t1].[MiddleInitial], [t1].[Id0], [t1].[Size]
    FROM (
        SELECT [p0].[Id], [p0].[Age], [p0].[FirstName], [p0].[LastName], [p0].[MiddleInitial], [f0].[Id] AS [Id0], [f0].[Size], ROW_NUMBER() OVER(PARTITION BY [f0].[Id], [f0].[Size] ORDER BY [p0].[Id] DESC) AS [row]
        FROM [Person] AS [p0]
        LEFT JOIN [Feet] AS [f0] ON [p0].[Id] = [f0].[Id]
    ) AS [t1]
    WHERE [t1].[row] <= 1
) AS [t0] ON ([t].[Id] = [t0].[Id0] OR (([t].[Id] IS NULL) AND ([t0].[Id0] IS NULL))) AND ([t].[Size] = [t0].[Size] OR (([t].[Size] IS NULL) AND ([t0].[Size] IS NULL)))
""");
    }

    public override async Task Whats_new_2021_sample_16(bool async)
    {
        await base.Whats_new_2021_sample_16(async);

        AssertSql();
    }

    public override async Task Min_Grouped_from_LINQ_101(bool async)
    {
        await base.Min_Grouped_from_LINQ_101(async);

        AssertSql(
"""
SELECT `p`.`Category`, MIN(`p`.`UnitPrice`) AS `CheapestPrice`
FROM `ProductForLinq` AS `p`
GROUP BY `p`.`Category`
""");
    }

    public override async Task Average_Grouped_from_LINQ_101(bool async)
    {
        await base.Average_Grouped_from_LINQ_101(async);

        AssertSql(
"""
SELECT `p`.`Category`, AVG(`p`.`UnitPrice`) AS `AveragePrice`
FROM `ProductForLinq` AS `p`
GROUP BY `p`.`Category`
""");
    }

    public override async Task Whats_new_2021_sample_8(bool async)
    {
        await base.Whats_new_2021_sample_8(async);

        AssertSql(
            """
SELECT COUNT(*)
FROM (
    SELECT 1
    FROM `Person` AS `p`
    LEFT JOIN `Feet` AS `f` ON `p`.`Id` = `f`.`Id`
    GROUP BY `f`.`Id`, `f`.`Size`
) AS `s`
""");
    }

    public override async Task Whats_new_2021_sample_12(bool async)
    {
        await base.Whats_new_2021_sample_12(async);

        AssertSql(
            """
SELECT `p1`.`FirstName`, `s0`.`Id`, `s0`.`Age`, `s0`.`FirstName`, `s0`.`LastName`, `s0`.`MiddleInitial`, `s0`.`Id0`, `s0`.`Age0`, `s0`.`PersonId`, `s0`.`Style`
FROM (
    SELECT `p`.`FirstName`
    FROM `Person` AS `p`
    GROUP BY `p`.`FirstName`
) AS `p1`
LEFT JOIN (
    SELECT `p0`.`Id`, `p0`.`Age`, `p0`.`FirstName`, `p0`.`LastName`, `p0`.`MiddleInitial`, `s`.`Id` AS `Id0`, `s`.`Age` AS `Age0`, `s`.`PersonId`, `s`.`Style`
    FROM `Person` AS `p0`
    LEFT JOIN `Shoes` AS `s` ON `p0`.`Id` = `s`.`PersonId`
) AS `s0` ON `p1`.`FirstName` = `s0`.`FirstName`
ORDER BY `p1`.`FirstName`, `s0`.`Id`
""");
    }

    public override async Task Whats_new_2021_sample_10(bool async)
    {
        await base.Whats_new_2021_sample_10(async);

        AssertSql(
            """
SELECT `s1`.`Id`, `s1`.`Age`, `s1`.`Style`, `s2`.`Id`, `s2`.`Style`, `s2`.`Age`, `s2`.`Id0`
FROM (
    SELECT `p`.`Id`, `s`.`Age`, `s`.`Style`
    FROM `Person` AS `p`
    INNER JOIN `Shoes` AS `s` ON `p`.`Age` = `s`.`Age`
    GROUP BY `p`.`Id`, `s`.`Style`, `s`.`Age`
) AS `s1`
LEFT JOIN (
    SELECT `s0`.`Id`, `s0`.`Style`, `s0`.`Age`, `p0`.`Id` AS `Id0`
    FROM `Person` AS `p0`
    INNER JOIN `Shoes` AS `s0` ON `p0`.`Age` = `s0`.`Age`
) AS `s2` ON `s1`.`Id` = `s2`.`Id0` AND (`s1`.`Style` = `s2`.`Style` OR (`s1`.`Style` IS NULL AND `s2`.`Style` IS NULL)) AND `s1`.`Age` = `s2`.`Age`
ORDER BY `s1`.`Id`, `s1`.`Style`, `s1`.`Age`, `s2`.`Id0`
""");
    }

    public override async Task Whats_new_2021_sample_13(bool async)
    {
        await base.Whats_new_2021_sample_13(async);

        AssertSql(
            """
SELECT `p1`.`FirstName`, `p1`.`MiddleInitial`, `p0`.`Id`, `p0`.`Age`, `p0`.`FirstName`, `p0`.`LastName`, `p0`.`MiddleInitial`
FROM (
    SELECT `p`.`FirstName`, `p`.`MiddleInitial`
    FROM `Person` AS `p`
    GROUP BY `p`.`FirstName`, `p`.`MiddleInitial`
) AS `p1`
LEFT JOIN `Person` AS `p0` ON (`p1`.`FirstName` = `p0`.`FirstName` OR (`p1`.`FirstName` IS NULL AND `p0`.`FirstName` IS NULL)) AND (`p1`.`MiddleInitial` = `p0`.`MiddleInitial` OR (`p1`.`MiddleInitial` IS NULL AND `p0`.`MiddleInitial` IS NULL))
ORDER BY `p1`.`FirstName`, `p1`.`MiddleInitial`, `p0`.`Id`
""");
    }

    public override async Task Cross_Join_with_Group_Join_from_LINQ_101(bool async)
    {
        await base.Cross_Join_with_Group_Join_from_LINQ_101(async);

        AssertSql(
            """
SELECT `c`.`Id`, `c`.`CompanyName`, `c`.`Region`, `s`.`Id`
FROM `CustomerForLinq` AS `c`
INNER JOIN (
    SELECT `o`.`Id`, `c0`.`Id` AS `Id0`
    FROM `OrderForLinq` AS `o`
    LEFT JOIN `CustomerForLinq` AS `c0` ON `o`.`CustomerId` = `c0`.`Id`
) AS `s` ON `c`.`Id` = `s`.`Id0`
""");
    }

    public override async Task Whats_new_2021_sample_2(bool async)
    {
        await base.Whats_new_2021_sample_2(async);

        AssertSql(
"""
SELECT [t0].[FirstName], [t0].[FullName], [t0].[c]
FROM (
    SELECT TOP(1) [p].[FirstName]
    FROM [Person] AS [p]
    GROUP BY [p].[FirstName]
    ORDER BY [p].[FirstName]
) AS [t]
LEFT JOIN (
    SELECT [t1].[FirstName], [t1].[FullName], [t1].[c]
    FROM (
        SELECT [p0].[FirstName], (((COALESCE([p0].[FirstName], N'') + N' ') + COALESCE([p0].[MiddleInitial], N'')) + N' ') + COALESCE([p0].[LastName], N'') AS [FullName], 1 AS [c], ROW_NUMBER() OVER(PARTITION BY [p0].[FirstName] ORDER BY [p0].[Id]) AS [row]
        FROM [Person] AS [p0]
    ) AS [t1]
    WHERE [t1].[row] <= 1
) AS [t0] ON [t].[FirstName] = [t0].[FirstName]
ORDER BY [t].[FirstName]
""");
    }

    public override async Task Whats_new_2021_sample_1(bool async)
    {
        await base.Whats_new_2021_sample_1(async);

        AssertSql(
"""
SELECT [t0].[Id], [t0].[Age], [t0].[FirstName], [t0].[LastName], [t0].[MiddleInitial], [t].[FirstName], [s].[Id], [s].[Age], [s].[PersonId], [s].[Style]
FROM (
    SELECT [p].[FirstName]
    FROM [Person] AS [p]
    GROUP BY [p].[FirstName]
) AS [t]
LEFT JOIN (
    SELECT [t1].[Id], [t1].[Age], [t1].[FirstName], [t1].[LastName], [t1].[MiddleInitial]
    FROM (
        SELECT [p0].[Id], [p0].[Age], [p0].[FirstName], [p0].[LastName], [p0].[MiddleInitial], ROW_NUMBER() OVER(PARTITION BY [p0].[FirstName] ORDER BY [p0].[FirstName], [p0].[LastName]) AS [row]
        FROM [Person] AS [p0]
    ) AS [t1]
    WHERE [t1].[row] <= 1
) AS [t0] ON [t].[FirstName] = [t0].[FirstName]
LEFT JOIN [Shoes] AS [s] ON [t0].[Id] = [s].[PersonId]
ORDER BY [t].[FirstName], [t0].[Id]
""");
    }

    public override async Task Whats_new_2021_sample_7(bool async)
    {
        await base.Whats_new_2021_sample_7(async);

        AssertSql(
            """
@size='11'
@size='11'

SELECT `p0`.`LastName`, `f`.`Size`, (
    SELECT MIN(`f1`.`Size`)
    FROM ((`Person` AS `p1`
    LEFT JOIN `Feet` AS `f0` ON `p1`.`Id` = `f0`.`Id`)
    LEFT JOIN `Person` AS `p2` ON `f0`.`Id` = `p2`.`Id`)
    LEFT JOIN `Feet` AS `f1` ON `p1`.`Id` = `f1`.`Id`
    WHERE `f0`.`Size` = @size AND `p1`.`MiddleInitial` IS NOT NULL AND (`f0`.`Id` <> 1 OR `f0`.`Id` IS NULL) AND (`f`.`Size` = `f0`.`Size` OR (`f`.`Size` IS NULL AND `f0`.`Size` IS NULL)) AND (`p0`.`LastName` = `p2`.`LastName` OR (`p0`.`LastName` IS NULL AND `p2`.`LastName` IS NULL))) AS `Min`
FROM (`Person` AS `p`
LEFT JOIN `Feet` AS `f` ON `p`.`Id` = `f`.`Id`)
LEFT JOIN `Person` AS `p0` ON `f`.`Id` = `p0`.`Id`
WHERE `f`.`Size` = @size AND `p`.`MiddleInitial` IS NOT NULL AND (`f`.`Id` <> 1 OR `f`.`Id` IS NULL)
GROUP BY `f`.`Size`, `p0`.`LastName`
""");
    }

    public override async Task Sum_Grouped_from_LINQ_101(bool async)
    {
        await base.Sum_Grouped_from_LINQ_101(async);

        AssertSql(
"""
SELECT `p`.`Category`, IIF(SUM(`p`.`UnitsInStock`) IS NULL, 0, SUM(`p`.`UnitsInStock`)) AS `TotalUnitsInStock`
FROM `ProductForLinq` AS `p`
GROUP BY `p`.`Category`
""");
    }

    public override async Task Count_Grouped_from_LINQ_101(bool async)
    {
        await base.Count_Grouped_from_LINQ_101(async);

        AssertSql(
"""
SELECT `p`.`Category`, COUNT(*) AS `ProductCount`
FROM `ProductForLinq` AS `p`
GROUP BY `p`.`Category`
""");
    }

    public override async Task Whats_new_2021_sample_9(bool async)
    {
        await base.Whats_new_2021_sample_9(async);

        AssertSql(
"""
SELECT `p`.`FirstName` AS `Feet`, (
    SELECT IIF(SUM(`f`.`Size`) IS NULL, 0, SUM(`f`.`Size`))
    FROM `Person` AS `p0`
    LEFT JOIN `Feet` AS `f` ON `p0`.`Id` = `f`.`Id`
    WHERE `p`.`FirstName` = `p0`.`FirstName` OR (`p`.`FirstName` IS NULL AND `p0`.`FirstName` IS NULL)) AS `Total`
FROM `Person` AS `p`
GROUP BY `p`.`FirstName`
""");
    }

    public override async Task LongCount_Grouped_from_LINQ_101(bool async)
    {
        await base.LongCount_Grouped_from_LINQ_101(async);

        AssertSql(
"""
SELECT `p`.`Category`, COUNT(*) AS `ProductLongCount`
FROM `ProductForLinq` AS `p`
GROUP BY `p`.`Category`
""");
    }

    public override async Task Whats_new_2021_sample_4(bool async)
    {
        await base.Whats_new_2021_sample_4(async);

        AssertSql(
"""
SELECT `s`.`Style` AS `Key`, (
    SELECT TOP 1 `s0`.`Style`
    FROM `Person` AS `p0`
    INNER JOIN `Shoes` AS `s0` ON `p0`.`Age` = `s0`.`Age`
    WHERE `s`.`Style` = `s0`.`Style` OR (`s`.`Style` IS NULL AND `s0`.`Style` IS NULL)) AS `Style`, COUNT(*) AS `Count`
FROM `Person` AS `p`
INNER JOIN `Shoes` AS `s` ON `p`.`Age` = `s`.`Age`
GROUP BY `s`.`Style`
""");
    }

    public override async Task Left_Outer_Join_with_Group_Join_from_LINQ_101(bool async)
    {
        await base.Left_Outer_Join_with_Group_Join_from_LINQ_101(async);

        AssertSql(
            """
SELECT `c`.`Id`, `c`.`CompanyName`, `c`.`Region`, `s`.`Id`, `s`.`Id0`, `o0`.`Id`, `o0`.`CustomerId`, `o0`.`OrderDate`, `o0`.`Total`, IIF(`s`.`Id` IS NULL, -1, `s`.`Id`)
FROM (`CustomerForLinq` AS `c`
LEFT JOIN (
    SELECT `o`.`Id`, `c0`.`Id` AS `Id0`
    FROM `OrderForLinq` AS `o`
    LEFT JOIN `CustomerForLinq` AS `c0` ON `o`.`CustomerId` = `c0`.`Id`
) AS `s` ON `c`.`Id` = `s`.`Id0`)
LEFT JOIN `OrderForLinq` AS `o0` ON `c`.`Id` = `o0`.`CustomerId`
ORDER BY `c`.`Id`, `s`.`Id`, `s`.`Id0`
""");
    }

    public override async Task Max_Grouped_from_LINQ_101(bool async)
    {
        await base.Max_Grouped_from_LINQ_101(async);

        AssertSql(
"""
SELECT `p`.`Category`, MAX(`p`.`UnitPrice`) AS `MostExpensivePrice`
FROM `ProductForLinq` AS `p`
GROUP BY `p`.`Category`
""");
    }

    public override async Task Whats_new_2021_sample_11(bool async)
    {
        await base.Whats_new_2021_sample_11(async);

        AssertSql(
"""
SELECT [t].[LastName], [t].[c], [t0].[Id], [t2].[Id], [t2].[Age], [t2].[FirstName], [t2].[LastName], [t2].[MiddleInitial], [t0].[Age], [t0].[FirstName], [t0].[LastName], [t0].[MiddleInitial]
FROM (
    SELECT [p].[LastName], COUNT(*) AS [c]
    FROM [Person] AS [p]
    GROUP BY [p].[LastName]
) AS [t]
LEFT JOIN (
    SELECT [t1].[Id], [t1].[Age], [t1].[FirstName], [t1].[LastName], [t1].[MiddleInitial]
    FROM (
        SELECT [p0].[Id], [p0].[Age], [p0].[FirstName], [p0].[LastName], [p0].[MiddleInitial], ROW_NUMBER() OVER(PARTITION BY [p0].[LastName] ORDER BY [p0].[Id]) AS [row]
        FROM [Person] AS [p0]
    ) AS [t1]
    WHERE [t1].[row] <= 1
) AS [t0] ON [t].[LastName] = [t0].[LastName]
LEFT JOIN (
    SELECT [t3].[Id], [t3].[Age], [t3].[FirstName], [t3].[LastName], [t3].[MiddleInitial]
    FROM (
        SELECT [p1].[Id], [p1].[Age], [p1].[FirstName], [p1].[LastName], [p1].[MiddleInitial], ROW_NUMBER() OVER(PARTITION BY [p1].[LastName] ORDER BY [p1].[Id]) AS [row]
        FROM [Person] AS [p1]
    ) AS [t3]
    WHERE [t3].[row] <= 2
) AS [t2] ON [t].[LastName] = [t2].[LastName]
ORDER BY [t].[LastName] DESC, [t0].[Id], [t2].[LastName], [t2].[Id]
""");
    }

    private void AssertSql(params string[] expected)
        => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);

    public class Ef6GroupByJetFixture : Ef6GroupByFixtureBase, ITestSqlLoggerFactory
    {
        public TestSqlLoggerFactory TestSqlLoggerFactory
            => (TestSqlLoggerFactory)ListLoggerFactory;

        protected override ITestStoreFactory TestStoreFactory
            => JetTestStoreFactory.Instance;
    }
}
