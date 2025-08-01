﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using EntityFrameworkCore.Jet.FunctionalTests.TestUtilities;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;
using Xunit.Abstractions;

namespace EntityFrameworkCore.Jet.FunctionalTests.Query;

public class TPCManyToManyQueryJetTest : TPCManyToManyQueryRelationalTestBase<TPCManyToManyQueryJetFixture>
{
    public TPCManyToManyQueryJetTest(TPCManyToManyQueryJetFixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override async Task Skip_navigation_all(bool async)
    {
        await base.Skip_navigation_all(async);

        AssertSql(
            """
SELECT `e`.`Id`, `e`.`Name`
FROM `EntityOnes` AS `e`
WHERE NOT EXISTS (
    SELECT `j`.`OneId`
    FROM `JoinOneToTwo` AS `j`
    INNER JOIN `EntityTwos` AS `e0` ON `j`.`TwoId` = `e0`.`Id`
    WHERE `e`.`Id` = `j`.`OneId` AND (`e0`.`Name` NOT LIKE '%B%' OR `e0`.`Name` IS NULL))
""");
    }

    public override async Task Skip_navigation_any_without_predicate(bool async)
    {
        await base.Skip_navigation_any_without_predicate(async);

        AssertSql(
            """
SELECT `e`.`Id`, `e`.`Name`
FROM `EntityOnes` AS `e`
WHERE EXISTS (
    SELECT 1
    FROM `JoinOneToThreePayloadFull` AS `j`
    INNER JOIN `EntityThrees` AS `e0` ON `j`.`ThreeId` = `e0`.`Id`
    WHERE `e`.`Id` = `j`.`OneId` AND (`e0`.`Name` LIKE '%B%'))
""");
    }

    public override async Task Skip_navigation_any_with_predicate(bool async)
    {
        await base.Skip_navigation_any_with_predicate(async);

        AssertSql(
            """
SELECT `e`.`Id`, `e`.`Name`
FROM `EntityOnes` AS `e`
WHERE EXISTS (
    SELECT 1
    FROM `EntityOneEntityTwo` AS `e0`
    INNER JOIN `EntityTwos` AS `e1` ON `e0`.`TwoSkipSharedId` = `e1`.`Id`
    WHERE `e`.`Id` = `e0`.`OneSkipSharedId` AND (`e1`.`Name` LIKE '%B%'))
""");
    }

    public override async Task Skip_navigation_contains(bool async)
    {
        await base.Skip_navigation_contains(async);

        AssertSql(
"""
SELECT `e`.`Id`, `e`.`Name`
FROM `EntityOnes` AS `e`
WHERE EXISTS (
    SELECT 1
    FROM `JoinOneToThreePayloadFullShared` AS `j`
    INNER JOIN `EntityThrees` AS `e0` ON `j`.`ThreeId` = `e0`.`Id`
    WHERE `e`.`Id` = `j`.`OneId` AND `e0`.`Id` = 1)
""");
    }

    public override async Task Skip_navigation_count_without_predicate(bool async)
    {
        await base.Skip_navigation_count_without_predicate(async);

        AssertSql(
            """
SELECT `e`.`Id`, `e`.`Name`
FROM `EntityOnes` AS `e`
WHERE EXISTS (
    SELECT 1
    FROM `JoinOneSelfPayload` AS `j`
    INNER JOIN `EntityOnes` AS `e0` ON `j`.`LeftId` = `e0`.`Id`
    WHERE `e`.`Id` = `j`.`RightId`)
""");
    }

    public override async Task Skip_navigation_count_with_predicate(bool async)
    {
        await base.Skip_navigation_count_with_predicate(async);

        AssertSql(
            """
SELECT `e0`.`Id`, `e0`.`Name`, `e0`.`c`
FROM (
    SELECT `e`.`Id`, `e`.`Name`, (
        SELECT COUNT(*)
        FROM `JoinOneToBranch` AS `j`
        INNER JOIN (
            SELECT `b`.`Id`, `b`.`Name`
            FROM `Branches` AS `b`
            UNION ALL
            SELECT `l`.`Id`, `l`.`Name`
            FROM `Leaves` AS `l`
        ) AS `u` ON `j`.`EntityBranchId` = `u`.`Id`
        WHERE `e`.`Id` = `j`.`EntityOneId` AND (`u`.`Name` LIKE 'L%')) AS `c`
    FROM `EntityOnes` AS `e`
) AS `e0`
ORDER BY `e0`.`c`, `e0`.`Id`
""");
    }

    public override async Task Skip_navigation_long_count_without_predicate(bool async)
    {
        await base.Skip_navigation_long_count_without_predicate(async);

        AssertSql(
"""
SELECT [e].[Id], [e].[CollectionInverseId], [e].[ExtraId], [e].[Name], [e].[ReferenceInverseId]
FROM [EntityTwos] AS [e]
WHERE (
    SELECT COUNT_BIG(*)
    FROM [JoinTwoToThree] AS [j]
    INNER JOIN [EntityThrees] AS [e0] ON [j].[ThreeId] = [e0].[Id]
    WHERE [e].[Id] = [j].[TwoId]) > CAST(0 AS bigint)
""");
    }

    public override async Task Skip_navigation_long_count_with_predicate(bool async)
    {
        await base.Skip_navigation_long_count_with_predicate(async);

        AssertSql(
            """
SELECT `e2`.`Id`, `e2`.`CollectionInverseId`, `e2`.`ExtraId`, `e2`.`Name`, `e2`.`ReferenceInverseId`, `e2`.`c`
FROM (
    SELECT `e`.`Id`, `e`.`CollectionInverseId`, `e`.`ExtraId`, `e`.`Name`, `e`.`ReferenceInverseId`, (
        SELECT COUNT(*)
        FROM `EntityTwoEntityTwo` AS `e0`
        INNER JOIN `EntityTwos` AS `e1` ON `e0`.`SelfSkipSharedLeftId` = `e1`.`Id`
        WHERE `e`.`Id` = `e0`.`SelfSkipSharedRightId` AND (`e1`.`Name` LIKE 'L%')) AS `c`
    FROM `EntityTwos` AS `e`
) AS `e2`
ORDER BY `e2`.`c` DESC, `e2`.`Id`
""");
    }

    public override async Task Skip_navigation_select_many_average(bool async)
    {
        await base.Skip_navigation_select_many_average(async);

        AssertSql(
            """
SELECT AVG(CDBL(`s`.`Key1`))
FROM `EntityTwos` AS `e`
INNER JOIN (
    SELECT `e1`.`Key1`, `e0`.`TwoSkipSharedId`
    FROM `EntityCompositeKeyEntityTwo` AS `e0`
    INNER JOIN `EntityCompositeKeys` AS `e1` ON `e0`.`CompositeKeySkipSharedKey1` = `e1`.`Key1` AND `e0`.`CompositeKeySkipSharedKey2` = `e1`.`Key2` AND `e0`.`CompositeKeySkipSharedKey3` = `e1`.`Key3`
) AS `s` ON `e`.`Id` = `s`.`TwoSkipSharedId`
""");
    }

    public override async Task Skip_navigation_select_many_max(bool async)
    {
        await base.Skip_navigation_select_many_max(async);

        AssertSql(
            """
SELECT MAX(`s`.`Key1`)
FROM `EntityThrees` AS `e`
INNER JOIN (
    SELECT `e0`.`Key1`, `j`.`ThreeId`
    FROM `JoinThreeToCompositeKeyFull` AS `j`
    INNER JOIN `EntityCompositeKeys` AS `e0` ON `j`.`CompositeId1` = `e0`.`Key1` AND `j`.`CompositeId2` = `e0`.`Key2` AND `j`.`CompositeId3` = `e0`.`Key3`
) AS `s` ON `e`.`Id` = `s`.`ThreeId`
""");
    }

    public override async Task Skip_navigation_select_many_min(bool async)
    {
        await base.Skip_navigation_select_many_min(async);

        AssertSql(
            """
SELECT MIN(`s`.`Id`)
FROM `EntityThrees` AS `e`
INNER JOIN (
    SELECT `u`.`Id`, `e0`.`ThreeSkipSharedId`
    FROM `EntityRootEntityThree` AS `e0`
    INNER JOIN (
        SELECT `r`.`Id`
        FROM `Roots` AS `r`
        UNION ALL
        SELECT `b`.`Id`
        FROM `Branches` AS `b`
        UNION ALL
        SELECT `l`.`Id`
        FROM `Leaves` AS `l`
        UNION ALL
        SELECT `l0`.`Id`
        FROM `Leaf2s` AS `l0`
    ) AS `u` ON `e0`.`RootSkipSharedId` = `u`.`Id`
) AS `s` ON `e`.`Id` = `s`.`ThreeSkipSharedId`
""");
    }

    public override async Task Skip_navigation_select_many_sum(bool async)
    {
        await base.Skip_navigation_select_many_sum(async);

        AssertSql(
            """
SELECT IIF(SUM(`s`.`Key1`) IS NULL, 0, SUM(`s`.`Key1`))
FROM (
    SELECT `r`.`Id`
    FROM `Roots` AS `r`
    UNION ALL
    SELECT `b`.`Id`
    FROM `Branches` AS `b`
    UNION ALL
    SELECT `l`.`Id`
    FROM `Leaves` AS `l`
    UNION ALL
    SELECT `l0`.`Id`
    FROM `Leaf2s` AS `l0`
) AS `u`
INNER JOIN (
    SELECT `e0`.`Key1`, `e`.`RootSkipSharedId`
    FROM `EntityCompositeKeyEntityRoot` AS `e`
    INNER JOIN `EntityCompositeKeys` AS `e0` ON `e`.`CompositeKeySkipSharedKey1` = `e0`.`Key1` AND `e`.`CompositeKeySkipSharedKey2` = `e0`.`Key2` AND `e`.`CompositeKeySkipSharedKey3` = `e0`.`Key3`
) AS `s` ON `u`.`Id` = `s`.`RootSkipSharedId`
""");
    }

    public override async Task Skip_navigation_select_subquery_average(bool async)
    {
        await base.Skip_navigation_select_subquery_average(async);

        AssertSql(
"""
SELECT (
    SELECT AVG(CDBL(`e`.`Key1`))
    FROM `JoinCompositeKeyToLeaf` AS `j`
    INNER JOIN `EntityCompositeKeys` AS `e` ON `j`.`CompositeId1` = `e`.`Key1` AND `j`.`CompositeId2` = `e`.`Key2` AND `j`.`CompositeId3` = `e`.`Key3`
    WHERE `l`.`Id` = `j`.`LeafId`)
FROM `Leaves` AS `l`
""");
    }

    public override async Task Skip_navigation_select_subquery_max(bool async)
    {
        await base.Skip_navigation_select_subquery_max(async);

        AssertSql(
"""
SELECT (
    SELECT MAX(`e0`.`Id`)
    FROM `JoinOneToTwo` AS `j`
    INNER JOIN `EntityOnes` AS `e0` ON `j`.`OneId` = `e0`.`Id`
    WHERE `e`.`Id` = `j`.`TwoId`)
FROM `EntityTwos` AS `e`
""");
    }

    public override async Task Skip_navigation_select_subquery_min(bool async)
    {
        await base.Skip_navigation_select_subquery_min(async);

        AssertSql(
"""
SELECT (
    SELECT MIN(`e0`.`Id`)
    FROM `JoinOneToThreePayloadFull` AS `j`
    INNER JOIN `EntityOnes` AS `e0` ON `j`.`OneId` = `e0`.`Id`
    WHERE `e`.`Id` = `j`.`ThreeId`)
FROM `EntityThrees` AS `e`
""");
    }

    public override async Task Skip_navigation_select_subquery_sum(bool async)
    {
        await base.Skip_navigation_select_subquery_sum(async);

        AssertSql(
"""
SELECT (
    SELECT IIF(SUM(`e1`.`Id`) IS NULL, 0, SUM(`e1`.`Id`))
    FROM `EntityOneEntityTwo` AS `e0`
    INNER JOIN `EntityOnes` AS `e1` ON `e0`.`OneSkipSharedId` = `e1`.`Id`
    WHERE `e`.`Id` = `e0`.`TwoSkipSharedId`)
FROM `EntityTwos` AS `e`
""");
    }

    public override async Task Skip_navigation_order_by_first_or_default(bool async)
    {
        await base.Skip_navigation_order_by_first_or_default(async);

        AssertSql(
"""
SELECT [t0].[Id], [t0].[Name]
FROM [EntityThrees] AS [e]
LEFT JOIN (
    SELECT [t].[Id], [t].[Name], [t].[ThreeId]
    FROM (
        SELECT [e0].[Id], [e0].[Name], [j].[ThreeId], ROW_NUMBER() OVER(PARTITION BY [j].[ThreeId] ORDER BY [e0].[Id]) AS [row]
        FROM [JoinOneToThreePayloadFullShared] AS [j]
        INNER JOIN [EntityOnes] AS [e0] ON [j].[OneId] = [e0].[Id]
    ) AS [t]
    WHERE [t].[row] <= 1
) AS [t0] ON [e].[Id] = [t0].[ThreeId]
""");
    }

    public override async Task Skip_navigation_order_by_single_or_default(bool async)
    {
        await base.Skip_navigation_order_by_single_or_default(async);

        AssertSql(
"""
SELECT [t0].[Id], [t0].[Name]
FROM [EntityOnes] AS [e]
OUTER APPLY (
    SELECT TOP(1) [t].[Id], [t].[Name]
    FROM (
        SELECT TOP(1) [e0].[Id], [e0].[Name]
        FROM [JoinOneSelfPayload] AS [j]
        INNER JOIN [EntityOnes] AS [e0] ON [j].[RightId] = [e0].[Id]
        WHERE [e].[Id] = [j].[LeftId]
        ORDER BY [e0].[Id]
    ) AS [t]
    ORDER BY [t].[Id]
) AS [t0]
""");
    }

    public override async Task Skip_navigation_order_by_last_or_default(bool async)
    {
        await base.Skip_navigation_order_by_last_or_default(async);

        AssertSql(
"""
SELECT [t0].[Id], [t0].[Name]
FROM (
    SELECT [b].[Id]
    FROM [Branches] AS [b]
    UNION ALL
    SELECT [l].[Id]
    FROM [Leaves] AS [l]
) AS [t]
LEFT JOIN (
    SELECT [t1].[Id], [t1].[Name], [t1].[EntityBranchId]
    FROM (
        SELECT [e].[Id], [e].[Name], [j].[EntityBranchId], ROW_NUMBER() OVER(PARTITION BY [j].[EntityBranchId] ORDER BY [e].[Id] DESC) AS [row]
        FROM [JoinOneToBranch] AS [j]
        INNER JOIN [EntityOnes] AS [e] ON [j].[EntityOneId] = [e].[Id]
    ) AS [t1]
    WHERE [t1].[row] <= 1
) AS [t0] ON [t].[Id] = [t0].[EntityBranchId]
""");
    }

    public override async Task Skip_navigation_order_by_reverse_first_or_default(bool async)
    {
        await base.Skip_navigation_order_by_reverse_first_or_default(async);

        AssertSql(
"""
SELECT [t0].[Id], [t0].[CollectionInverseId], [t0].[ExtraId], [t0].[Name], [t0].[ReferenceInverseId]
FROM [EntityThrees] AS [e]
LEFT JOIN (
    SELECT [t].[Id], [t].[CollectionInverseId], [t].[ExtraId], [t].[Name], [t].[ReferenceInverseId], [t].[ThreeId]
    FROM (
        SELECT [e0].[Id], [e0].[CollectionInverseId], [e0].[ExtraId], [e0].[Name], [e0].[ReferenceInverseId], [j].[ThreeId], ROW_NUMBER() OVER(PARTITION BY [j].[ThreeId] ORDER BY [e0].[Id] DESC) AS [row]
        FROM [JoinTwoToThree] AS [j]
        INNER JOIN [EntityTwos] AS [e0] ON [j].[TwoId] = [e0].[Id]
    ) AS [t]
    WHERE [t].[row] <= 1
) AS [t0] ON [e].[Id] = [t0].[ThreeId]
""");
    }

    public override async Task Skip_navigation_cast(bool async)
    {
        await base.Skip_navigation_cast(async);

        AssertSql(
            """
SELECT `e`.`Key1`, `e`.`Key2`, `e`.`Key3`, `s`.`Id`, `s`.`Name`, `s`.`Number`, `s`.`IsGreen`, `s`.`LeafId`, `s`.`CompositeId1`, `s`.`CompositeId2`, `s`.`CompositeId3`
FROM `EntityCompositeKeys` AS `e`
LEFT JOIN (
    SELECT `l`.`Id`, `l`.`Name`, `l`.`Number`, `l`.`IsGreen`, `j`.`LeafId`, `j`.`CompositeId1`, `j`.`CompositeId2`, `j`.`CompositeId3`
    FROM `JoinCompositeKeyToLeaf` AS `j`
    INNER JOIN `Leaves` AS `l` ON `j`.`LeafId` = `l`.`Id`
) AS `s` ON `e`.`Key1` = `s`.`CompositeId1` AND `e`.`Key2` = `s`.`CompositeId2` AND `e`.`Key3` = `s`.`CompositeId3`
ORDER BY `e`.`Key1`, `e`.`Key2`, `e`.`Key3`, `s`.`LeafId`, `s`.`CompositeId1`, `s`.`CompositeId2`, `s`.`CompositeId3`
""");
    }

    public override async Task Skip_navigation_of_type(bool async)
    {
        await base.Skip_navigation_of_type(async);

        AssertSql(
            """
SELECT `e`.`Key1`, `e`.`Key2`, `e`.`Key3`, `s`.`Id`, `s`.`Name`, `s`.`Number`, `s`.`IsGreen`, `s`.`Discriminator`, `s`.`RootSkipSharedId`, `s`.`CompositeKeySkipSharedKey1`, `s`.`CompositeKeySkipSharedKey2`, `s`.`CompositeKeySkipSharedKey3`
FROM `EntityCompositeKeys` AS `e`
LEFT JOIN (
    SELECT `u`.`Id`, `u`.`Name`, `u`.`Number`, `u`.`IsGreen`, `u`.`Discriminator`, `e0`.`RootSkipSharedId`, `e0`.`CompositeKeySkipSharedKey1`, `e0`.`CompositeKeySkipSharedKey2`, `e0`.`CompositeKeySkipSharedKey3`
    FROM `EntityCompositeKeyEntityRoot` AS `e0`
    INNER JOIN (
        SELECT `r`.`Id`, `r`.`Name`, CVar(NULL) AS `Number`, CVar(NULL) AS `IsGreen`, 'EntityRoot' AS `Discriminator`
        FROM `Roots` AS `r`
        UNION ALL
        SELECT `b`.`Id`, `b`.`Name`, `b`.`Number`, CVar(NULL) AS `IsGreen`, 'EntityBranch' AS `Discriminator`
        FROM `Branches` AS `b`
        UNION ALL
        SELECT `l`.`Id`, `l`.`Name`, `l`.`Number`, `l`.`IsGreen`, 'EntityLeaf' AS `Discriminator`
        FROM `Leaves` AS `l`
        UNION ALL
        SELECT `l0`.`Id`, `l0`.`Name`, CVar(NULL) AS `Number`, CVar(NULL) AS `IsGreen`, 'EntityLeaf2' AS `Discriminator`
        FROM `Leaf2s` AS `l0`
    ) AS `u` ON `e0`.`RootSkipSharedId` = `u`.`Id`
    WHERE `u`.`Discriminator` = 'EntityLeaf'
) AS `s` ON `e`.`Key1` = `s`.`CompositeKeySkipSharedKey1` AND `e`.`Key2` = `s`.`CompositeKeySkipSharedKey2` AND `e`.`Key3` = `s`.`CompositeKeySkipSharedKey3`
ORDER BY `e`.`Key1`, `e`.`Key2`, `e`.`Key3`, `s`.`RootSkipSharedId`, `s`.`CompositeKeySkipSharedKey1`, `s`.`CompositeKeySkipSharedKey2`, `s`.`CompositeKeySkipSharedKey3`
""");
    }

    public override async Task Join_with_skip_navigation(bool async)
    {
        await base.Join_with_skip_navigation(async);

        AssertSql(
"""
SELECT [e].[Id], [e].[CollectionInverseId], [e].[ExtraId], [e].[Name], [e].[ReferenceInverseId], [e0].[Id], [e0].[CollectionInverseId], [e0].[ExtraId], [e0].[Name], [e0].[ReferenceInverseId]
FROM [EntityTwos] AS [e]
INNER JOIN [EntityTwos] AS [e0] ON [e].[Id] = (
    SELECT TOP(1) [e2].[Id]
    FROM [EntityTwoEntityTwo] AS [e1]
    INNER JOIN [EntityTwos] AS [e2] ON [e1].[SelfSkipSharedRightId] = [e2].[Id]
    WHERE [e0].[Id] = [e1].[SelfSkipSharedLeftId]
    ORDER BY [e2].[Id])
""");
    }

    public override async Task Left_join_with_skip_navigation(bool async)
    {
        await base.Left_join_with_skip_navigation(async);

        AssertSql(
"""
SELECT [e].[Key1], [e].[Key2], [e].[Key3], [e].[Name], [e0].[Key1], [e0].[Key2], [e0].[Key3], [e0].[Name]
FROM [EntityCompositeKeys] AS [e]
LEFT JOIN [EntityCompositeKeys] AS [e0] ON (
    SELECT TOP(1) [e2].[Id]
    FROM [EntityCompositeKeyEntityTwo] AS [e1]
    INNER JOIN [EntityTwos] AS [e2] ON [e1].[TwoSkipSharedId] = [e2].[Id]
    WHERE [e].[Key1] = [e1].[CompositeKeySkipSharedKey1] AND [e].[Key2] = [e1].[CompositeKeySkipSharedKey2] AND [e].[Key3] = [e1].[CompositeKeySkipSharedKey3]
    ORDER BY [e2].[Id]) = (
    SELECT TOP(1) [e3].[Id]
    FROM [JoinThreeToCompositeKeyFull] AS [j]
    INNER JOIN [EntityThrees] AS [e3] ON [j].[ThreeId] = [e3].[Id]
    WHERE [e0].[Key1] = [j].[CompositeId1] AND [e0].[Key2] = [j].[CompositeId2] AND [e0].[Key3] = [j].[CompositeId3]
    ORDER BY [e3].[Id])
ORDER BY [e].[Key1], [e0].[Key1], [e].[Key2], [e0].[Key2]
""");
    }

    public override async Task Select_many_over_skip_navigation(bool async)
    {
        await base.Select_many_over_skip_navigation(async);

        AssertSql(
            """
SELECT `s`.`Id`, `s`.`CollectionInverseId`, `s`.`Name`, `s`.`ReferenceInverseId`
FROM (
    SELECT `r`.`Id`
    FROM `Roots` AS `r`
    UNION ALL
    SELECT `b`.`Id`
    FROM `Branches` AS `b`
    UNION ALL
    SELECT `l`.`Id`
    FROM `Leaves` AS `l`
    UNION ALL
    SELECT `l0`.`Id`
    FROM `Leaf2s` AS `l0`
) AS `u`
INNER JOIN (
    SELECT `e0`.`Id`, `e0`.`CollectionInverseId`, `e0`.`Name`, `e0`.`ReferenceInverseId`, `e`.`RootSkipSharedId`
    FROM `EntityRootEntityThree` AS `e`
    INNER JOIN `EntityThrees` AS `e0` ON `e`.`ThreeSkipSharedId` = `e0`.`Id`
) AS `s` ON `u`.`Id` = `s`.`RootSkipSharedId`
""");
    }

    public override async Task Select_many_over_skip_navigation_where(bool async)
    {
        await base.Select_many_over_skip_navigation_where(async);

        AssertSql(
            """
SELECT `s`.`Id`, `s`.`CollectionInverseId`, `s`.`ExtraId`, `s`.`Name`, `s`.`ReferenceInverseId`
FROM `EntityOnes` AS `e`
LEFT JOIN (
    SELECT `e0`.`Id`, `e0`.`CollectionInverseId`, `e0`.`ExtraId`, `e0`.`Name`, `e0`.`ReferenceInverseId`, `j`.`OneId`
    FROM `JoinOneToTwo` AS `j`
    INNER JOIN `EntityTwos` AS `e0` ON `j`.`TwoId` = `e0`.`Id`
) AS `s` ON `e`.`Id` = `s`.`OneId`
""");
    }

    public override async Task Select_many_over_skip_navigation_order_by_skip(bool async)
    {
        await base.Select_many_over_skip_navigation_order_by_skip(async);

        AssertSql(
"""
SELECT [t0].[Id], [t0].[CollectionInverseId], [t0].[Name], [t0].[ReferenceInverseId]
FROM [EntityOnes] AS [e]
INNER JOIN (
    SELECT [t].[Id], [t].[CollectionInverseId], [t].[Name], [t].[ReferenceInverseId], [t].[OneId]
    FROM (
        SELECT [e0].[Id], [e0].[CollectionInverseId], [e0].[Name], [e0].[ReferenceInverseId], [j].[OneId], ROW_NUMBER() OVER(PARTITION BY [j].[OneId] ORDER BY [e0].[Id]) AS [row]
        FROM [JoinOneToThreePayloadFull] AS [j]
        INNER JOIN [EntityThrees] AS [e0] ON [j].[ThreeId] = [e0].[Id]
    ) AS [t]
    WHERE 2 < [t].[row]
) AS [t0] ON [e].[Id] = [t0].[OneId]
""");
    }

    public override async Task Select_many_over_skip_navigation_order_by_take(bool async)
    {
        await base.Select_many_over_skip_navigation_order_by_take(async);

        AssertSql(
"""
SELECT [t0].[Id], [t0].[CollectionInverseId], [t0].[ExtraId], [t0].[Name], [t0].[ReferenceInverseId]
FROM [EntityOnes] AS [e]
INNER JOIN (
    SELECT [t].[Id], [t].[CollectionInverseId], [t].[ExtraId], [t].[Name], [t].[ReferenceInverseId], [t].[OneSkipSharedId]
    FROM (
        SELECT [e1].[Id], [e1].[CollectionInverseId], [e1].[ExtraId], [e1].[Name], [e1].[ReferenceInverseId], [e0].[OneSkipSharedId], ROW_NUMBER() OVER(PARTITION BY [e0].[OneSkipSharedId] ORDER BY [e1].[Id]) AS [row]
        FROM [EntityOneEntityTwo] AS [e0]
        INNER JOIN [EntityTwos] AS [e1] ON [e0].[TwoSkipSharedId] = [e1].[Id]
    ) AS [t]
    WHERE [t].[row] <= 2
) AS [t0] ON [e].[Id] = [t0].[OneSkipSharedId]
""");
    }

    public override async Task Select_many_over_skip_navigation_order_by_skip_take(bool async)
    {
        await base.Select_many_over_skip_navigation_order_by_skip_take(async);

        AssertSql(
"""
SELECT [t0].[Id], [t0].[CollectionInverseId], [t0].[Name], [t0].[ReferenceInverseId]
FROM [EntityOnes] AS [e]
INNER JOIN (
    SELECT [t].[Id], [t].[CollectionInverseId], [t].[Name], [t].[ReferenceInverseId], [t].[OneId]
    FROM (
        SELECT [e0].[Id], [e0].[CollectionInverseId], [e0].[Name], [e0].[ReferenceInverseId], [j].[OneId], ROW_NUMBER() OVER(PARTITION BY [j].[OneId] ORDER BY [e0].[Id]) AS [row]
        FROM [JoinOneToThreePayloadFullShared] AS [j]
        INNER JOIN [EntityThrees] AS [e0] ON [j].[ThreeId] = [e0].[Id]
    ) AS [t]
    WHERE 2 < [t].[row] AND [t].[row] <= 5
) AS [t0] ON [e].[Id] = [t0].[OneId]
""");
    }

    public override async Task Select_many_over_skip_navigation_of_type(bool async)
    {
        await base.Select_many_over_skip_navigation_of_type(async);

        AssertSql(
            """
SELECT `s`.`Id`, `s`.`Name`, `s`.`Number`, `s`.`IsGreen`, `s`.`Discriminator`
FROM `EntityThrees` AS `e`
INNER JOIN (
    SELECT `u`.`Id`, `u`.`Name`, `u`.`Number`, `u`.`IsGreen`, `u`.`Discriminator`, `e0`.`ThreeSkipSharedId`
    FROM `EntityRootEntityThree` AS `e0`
    INNER JOIN (
        SELECT `r`.`Id`, `r`.`Name`, CVar(NULL) AS `Number`, CVar(NULL) AS `IsGreen`, 'EntityRoot' AS `Discriminator`
        FROM `Roots` AS `r`
        UNION ALL
        SELECT `b`.`Id`, `b`.`Name`, `b`.`Number`, CVar(NULL) AS `IsGreen`, 'EntityBranch' AS `Discriminator`
        FROM `Branches` AS `b`
        UNION ALL
        SELECT `l`.`Id`, `l`.`Name`, `l`.`Number`, `l`.`IsGreen`, 'EntityLeaf' AS `Discriminator`
        FROM `Leaves` AS `l`
        UNION ALL
        SELECT `l0`.`Id`, `l0`.`Name`, CVar(NULL) AS `Number`, CVar(NULL) AS `IsGreen`, 'EntityLeaf2' AS `Discriminator`
        FROM `Leaf2s` AS `l0`
    ) AS `u` ON `e0`.`RootSkipSharedId` = `u`.`Id`
    WHERE `u`.`Discriminator` IN ('EntityBranch', 'EntityLeaf')
) AS `s` ON `e`.`Id` = `s`.`ThreeSkipSharedId`
""");
    }

    public override async Task Select_many_over_skip_navigation_cast(bool async)
    {
        await base.Select_many_over_skip_navigation_cast(async);

        AssertSql(
            """
SELECT `s`.`Id`, `s`.`Name`, `s`.`Number`, `s`.`IsGreen`, `s`.`Discriminator`
FROM `EntityOnes` AS `e`
INNER JOIN (
    SELECT `u`.`Id`, `u`.`Name`, `u`.`Number`, `u`.`IsGreen`, `u`.`Discriminator`, `j`.`EntityOneId`
    FROM `JoinOneToBranch` AS `j`
    INNER JOIN (
        SELECT `b`.`Id`, `b`.`Name`, `b`.`Number`, CVar(NULL) AS `IsGreen`, 'EntityBranch' AS `Discriminator`
        FROM `Branches` AS `b`
        UNION ALL
        SELECT `l`.`Id`, `l`.`Name`, `l`.`Number`, `l`.`IsGreen`, 'EntityLeaf' AS `Discriminator`
        FROM `Leaves` AS `l`
    ) AS `u` ON `j`.`EntityBranchId` = `u`.`Id`
) AS `s` ON `e`.`Id` = `s`.`EntityOneId`
""");
    }

    public override async Task Select_skip_navigation(bool async)
    {
        await base.Select_skip_navigation(async);

        AssertSql(
            """
SELECT `e`.`Id`, `s`.`Id`, `s`.`Name`, `s`.`LeftId`, `s`.`RightId`
FROM `EntityOnes` AS `e`
LEFT JOIN (
    SELECT `e0`.`Id`, `e0`.`Name`, `j`.`LeftId`, `j`.`RightId`
    FROM `JoinOneSelfPayload` AS `j`
    INNER JOIN `EntityOnes` AS `e0` ON `j`.`LeftId` = `e0`.`Id`
) AS `s` ON `e`.`Id` = `s`.`RightId`
ORDER BY `e`.`Id`, `s`.`LeftId`, `s`.`RightId`
""");
    }

    public override async Task Select_skip_navigation_multiple(bool async)
    {
        await base.Select_skip_navigation_multiple(async);

        AssertSql(
            """
SELECT `e`.`Id`, `s`.`Id`, `s`.`CollectionInverseId`, `s`.`Name`, `s`.`ReferenceInverseId`, `s`.`ThreeId`, `s`.`TwoId`, `s0`.`Id`, `s0`.`CollectionInverseId`, `s0`.`ExtraId`, `s0`.`Name`, `s0`.`ReferenceInverseId`, `s0`.`SelfSkipSharedLeftId`, `s0`.`SelfSkipSharedRightId`, `s1`.`Key1`, `s1`.`Key2`, `s1`.`Key3`, `s1`.`Name`, `s1`.`TwoSkipSharedId`, `s1`.`CompositeKeySkipSharedKey1`, `s1`.`CompositeKeySkipSharedKey2`, `s1`.`CompositeKeySkipSharedKey3`
FROM ((`EntityTwos` AS `e`
LEFT JOIN (
    SELECT `e0`.`Id`, `e0`.`CollectionInverseId`, `e0`.`Name`, `e0`.`ReferenceInverseId`, `j`.`ThreeId`, `j`.`TwoId`
    FROM `JoinTwoToThree` AS `j`
    INNER JOIN `EntityThrees` AS `e0` ON `j`.`ThreeId` = `e0`.`Id`
) AS `s` ON `e`.`Id` = `s`.`TwoId`)
LEFT JOIN (
    SELECT `e2`.`Id`, `e2`.`CollectionInverseId`, `e2`.`ExtraId`, `e2`.`Name`, `e2`.`ReferenceInverseId`, `e1`.`SelfSkipSharedLeftId`, `e1`.`SelfSkipSharedRightId`
    FROM `EntityTwoEntityTwo` AS `e1`
    INNER JOIN `EntityTwos` AS `e2` ON `e1`.`SelfSkipSharedLeftId` = `e2`.`Id`
) AS `s0` ON `e`.`Id` = `s0`.`SelfSkipSharedRightId`)
LEFT JOIN (
    SELECT `e4`.`Key1`, `e4`.`Key2`, `e4`.`Key3`, `e4`.`Name`, `e3`.`TwoSkipSharedId`, `e3`.`CompositeKeySkipSharedKey1`, `e3`.`CompositeKeySkipSharedKey2`, `e3`.`CompositeKeySkipSharedKey3`
    FROM `EntityCompositeKeyEntityTwo` AS `e3`
    INNER JOIN `EntityCompositeKeys` AS `e4` ON `e3`.`CompositeKeySkipSharedKey1` = `e4`.`Key1` AND `e3`.`CompositeKeySkipSharedKey2` = `e4`.`Key2` AND `e3`.`CompositeKeySkipSharedKey3` = `e4`.`Key3`
) AS `s1` ON `e`.`Id` = `s1`.`TwoSkipSharedId`
ORDER BY `e`.`Id`, `s`.`ThreeId`, `s`.`TwoId`, `s`.`Id`, `s0`.`SelfSkipSharedLeftId`, `s0`.`SelfSkipSharedRightId`, `s0`.`Id`, `s1`.`TwoSkipSharedId`, `s1`.`CompositeKeySkipSharedKey1`, `s1`.`CompositeKeySkipSharedKey2`, `s1`.`CompositeKeySkipSharedKey3`, `s1`.`Key1`, `s1`.`Key2`
""");
    }

    public override async Task Select_skip_navigation_first_or_default(bool async)
    {
        await base.Select_skip_navigation_first_or_default(async);

        AssertSql(
"""
SELECT [t0].[Key1], [t0].[Key2], [t0].[Key3], [t0].[Name]
FROM [EntityThrees] AS [e]
LEFT JOIN (
    SELECT [t].[Key1], [t].[Key2], [t].[Key3], [t].[Name], [t].[ThreeId]
    FROM (
        SELECT [e0].[Key1], [e0].[Key2], [e0].[Key3], [e0].[Name], [j].[ThreeId], ROW_NUMBER() OVER(PARTITION BY [j].[ThreeId] ORDER BY [e0].[Key1], [e0].[Key2]) AS [row]
        FROM [JoinThreeToCompositeKeyFull] AS [j]
        INNER JOIN [EntityCompositeKeys] AS [e0] ON [j].[CompositeId1] = [e0].[Key1] AND [j].[CompositeId2] = [e0].[Key2] AND [j].[CompositeId3] = [e0].[Key3]
    ) AS [t]
    WHERE [t].[row] <= 1
) AS [t0] ON [e].[Id] = [t0].[ThreeId]
ORDER BY [e].[Id]
""");
    }

    public override async Task Include_skip_navigation(bool async)
    {
        await base.Include_skip_navigation(async);

        AssertSql(
            """
SELECT `e`.`Key1`, `e`.`Key2`, `e`.`Key3`, `e`.`Name`, `s`.`RootSkipSharedId`, `s`.`CompositeKeySkipSharedKey1`, `s`.`CompositeKeySkipSharedKey2`, `s`.`CompositeKeySkipSharedKey3`, `s`.`Id`, `s`.`Name`, `s`.`Number`, `s`.`Slumber`, `s`.`IsGreen`, `s`.`IsBrown`, `s`.`Discriminator`
FROM `EntityCompositeKeys` AS `e`
LEFT JOIN (
    SELECT `e0`.`RootSkipSharedId`, `e0`.`CompositeKeySkipSharedKey1`, `e0`.`CompositeKeySkipSharedKey2`, `e0`.`CompositeKeySkipSharedKey3`, `u`.`Id`, `u`.`Name`, `u`.`Number`, `u`.`Slumber`, `u`.`IsGreen`, `u`.`IsBrown`, `u`.`Discriminator`
    FROM `EntityCompositeKeyEntityRoot` AS `e0`
    INNER JOIN (
        SELECT `r`.`Id`, `r`.`Name`, CVar(NULL) AS `Number`, CVar(NULL) AS `Slumber`, CVar(NULL) AS `IsGreen`, CVar(NULL) AS `IsBrown`, 'EntityRoot' AS `Discriminator`
        FROM `Roots` AS `r`
        UNION ALL
        SELECT `b`.`Id`, `b`.`Name`, `b`.`Number`, CVar(NULL) AS `Slumber`, CVar(NULL) AS `IsGreen`, CVar(NULL) AS `IsBrown`, 'EntityBranch' AS `Discriminator`
        FROM `Branches` AS `b`
        UNION ALL
        SELECT `l`.`Id`, `l`.`Name`, `l`.`Number`, CVar(NULL) AS `Slumber`, `l`.`IsGreen`, CVar(NULL) AS `IsBrown`, 'EntityLeaf' AS `Discriminator`
        FROM `Leaves` AS `l`
        UNION ALL
        SELECT `l0`.`Id`, `l0`.`Name`, CVar(NULL) AS `Number`, `l0`.`Slumber`, CVar(NULL) AS `IsGreen`, `l0`.`IsBrown`, 'EntityLeaf2' AS `Discriminator`
        FROM `Leaf2s` AS `l0`
    ) AS `u` ON `e0`.`RootSkipSharedId` = `u`.`Id`
) AS `s` ON `e`.`Key1` = `s`.`CompositeKeySkipSharedKey1` AND `e`.`Key2` = `s`.`CompositeKeySkipSharedKey2` AND `e`.`Key3` = `s`.`CompositeKeySkipSharedKey3`
ORDER BY `e`.`Key1`, `e`.`Key2`, `e`.`Key3`, `s`.`RootSkipSharedId`, `s`.`CompositeKeySkipSharedKey1`, `s`.`CompositeKeySkipSharedKey2`, `s`.`CompositeKeySkipSharedKey3`
""");
    }

    public override async Task Include_skip_navigation_then_reference(bool async)
    {
        await base.Include_skip_navigation_then_reference(async);

        AssertSql(
            """
SELECT `e`.`Id`, `e`.`CollectionInverseId`, `e`.`ExtraId`, `e`.`Name`, `e`.`ReferenceInverseId`, `s`.`OneId`, `s`.`TwoId`, `s`.`JoinOneToTwoExtraId`, `s`.`Id`, `s`.`Name`, `s`.`Id0`, `s`.`CollectionInverseId`, `s`.`ExtraId`, `s`.`Name0`, `s`.`ReferenceInverseId`
FROM `EntityTwos` AS `e`
LEFT JOIN (
    SELECT `j`.`OneId`, `j`.`TwoId`, `j`.`JoinOneToTwoExtraId`, `e0`.`Id`, `e0`.`Name`, `e1`.`Id` AS `Id0`, `e1`.`CollectionInverseId`, `e1`.`ExtraId`, `e1`.`Name` AS `Name0`, `e1`.`ReferenceInverseId`
    FROM (`JoinOneToTwo` AS `j`
    INNER JOIN `EntityOnes` AS `e0` ON `j`.`OneId` = `e0`.`Id`)
    LEFT JOIN `EntityTwos` AS `e1` ON `e0`.`Id` = `e1`.`ReferenceInverseId`
) AS `s` ON `e`.`Id` = `s`.`TwoId`
ORDER BY `e`.`Id`, `s`.`OneId`, `s`.`TwoId`, `s`.`Id`
""");
    }

    public override async Task Include_skip_navigation_then_include_skip_navigation(bool async)
    {
        await base.Include_skip_navigation_then_include_skip_navigation(async);

        AssertSql(
            """
SELECT `e`.`Key1`, `e`.`Key2`, `e`.`Key3`, `e`.`Name`, `s0`.`LeafId`, `s0`.`CompositeId1`, `s0`.`CompositeId2`, `s0`.`CompositeId3`, `s0`.`Id`, `s0`.`Name`, `s0`.`Number`, `s0`.`IsGreen`, `s0`.`EntityBranchId`, `s0`.`EntityOneId`, `s0`.`Id0`, `s0`.`Name0`
FROM `EntityCompositeKeys` AS `e`
LEFT JOIN (
    SELECT `j`.`LeafId`, `j`.`CompositeId1`, `j`.`CompositeId2`, `j`.`CompositeId3`, `l`.`Id`, `l`.`Name`, `l`.`Number`, `l`.`IsGreen`, `s`.`EntityBranchId`, `s`.`EntityOneId`, `s`.`Id` AS `Id0`, `s`.`Name` AS `Name0`
    FROM (`JoinCompositeKeyToLeaf` AS `j`
    INNER JOIN `Leaves` AS `l` ON `j`.`LeafId` = `l`.`Id`)
    LEFT JOIN (
        SELECT `j0`.`EntityBranchId`, `j0`.`EntityOneId`, `e0`.`Id`, `e0`.`Name`
        FROM `JoinOneToBranch` AS `j0`
        INNER JOIN `EntityOnes` AS `e0` ON `j0`.`EntityOneId` = `e0`.`Id`
    ) AS `s` ON `l`.`Id` = `s`.`EntityBranchId`
) AS `s0` ON `e`.`Key1` = `s0`.`CompositeId1` AND `e`.`Key2` = `s0`.`CompositeId2` AND `e`.`Key3` = `s0`.`CompositeId3`
ORDER BY `e`.`Key1`, `e`.`Key2`, `e`.`Key3`, `s0`.`LeafId`, `s0`.`CompositeId1`, `s0`.`CompositeId2`, `s0`.`CompositeId3`, `s0`.`Id`, `s0`.`EntityBranchId`, `s0`.`EntityOneId`
""");
    }

    public override async Task Include_skip_navigation_then_include_reference_and_skip_navigation(bool async)
    {
        await base.Include_skip_navigation_then_include_reference_and_skip_navigation(async);

        AssertSql(
            """
SELECT `e`.`Id`, `e`.`CollectionInverseId`, `e`.`Name`, `e`.`ReferenceInverseId`, `s0`.`OneId`, `s0`.`ThreeId`, `s0`.`Payload`, `s0`.`Id`, `s0`.`Name`, `s0`.`Id0`, `s0`.`CollectionInverseId`, `s0`.`ExtraId`, `s0`.`Name0`, `s0`.`ReferenceInverseId`, `s0`.`LeftId`, `s0`.`RightId`, `s0`.`Payload0`, `s0`.`Id1`, `s0`.`Name1`
FROM `EntityThrees` AS `e`
LEFT JOIN (
    SELECT `j`.`OneId`, `j`.`ThreeId`, `j`.`Payload`, `e0`.`Id`, `e0`.`Name`, `e1`.`Id` AS `Id0`, `e1`.`CollectionInverseId`, `e1`.`ExtraId`, `e1`.`Name` AS `Name0`, `e1`.`ReferenceInverseId`, `s`.`LeftId`, `s`.`RightId`, `s`.`Payload` AS `Payload0`, `s`.`Id` AS `Id1`, `s`.`Name` AS `Name1`
    FROM ((`JoinOneToThreePayloadFull` AS `j`
    INNER JOIN `EntityOnes` AS `e0` ON `j`.`OneId` = `e0`.`Id`)
    LEFT JOIN `EntityTwos` AS `e1` ON `e0`.`Id` = `e1`.`ReferenceInverseId`)
    LEFT JOIN (
        SELECT `j0`.`LeftId`, `j0`.`RightId`, `j0`.`Payload`, `e2`.`Id`, `e2`.`Name`
        FROM `JoinOneSelfPayload` AS `j0`
        INNER JOIN `EntityOnes` AS `e2` ON `j0`.`RightId` = `e2`.`Id`
    ) AS `s` ON `e0`.`Id` = `s`.`LeftId`
) AS `s0` ON `e`.`Id` = `s0`.`ThreeId`
ORDER BY `e`.`Id`, `s0`.`OneId`, `s0`.`ThreeId`, `s0`.`Id`, `s0`.`Id0`, `s0`.`LeftId`, `s0`.`RightId`
""");
    }

    public override async Task Include_skip_navigation_and_reference(bool async)
    {
        await base.Include_skip_navigation_and_reference(async);

        AssertSql(
            """
SELECT `e`.`Id`, `e`.`CollectionInverseId`, `e`.`ExtraId`, `e`.`Name`, `e`.`ReferenceInverseId`, `e0`.`Id`, `s`.`OneSkipSharedId`, `s`.`TwoSkipSharedId`, `s`.`Id`, `s`.`Name`, `e0`.`CollectionInverseId`, `e0`.`Name`, `e0`.`ReferenceInverseId`
FROM (`EntityTwos` AS `e`
LEFT JOIN `EntityThrees` AS `e0` ON `e`.`Id` = `e0`.`ReferenceInverseId`)
LEFT JOIN (
    SELECT `e1`.`OneSkipSharedId`, `e1`.`TwoSkipSharedId`, `e2`.`Id`, `e2`.`Name`
    FROM `EntityOneEntityTwo` AS `e1`
    INNER JOIN `EntityOnes` AS `e2` ON `e1`.`OneSkipSharedId` = `e2`.`Id`
) AS `s` ON `e`.`Id` = `s`.`TwoSkipSharedId`
ORDER BY `e`.`Id`, `e0`.`Id`, `s`.`OneSkipSharedId`, `s`.`TwoSkipSharedId`
""");
    }

    public override async Task Filtered_include_skip_navigation_where(bool async)
    {
        await base.Filtered_include_skip_navigation_where(async);

        AssertSql(
            """
SELECT `e`.`Id`, `e`.`CollectionInverseId`, `e`.`Name`, `e`.`ReferenceInverseId`, `s`.`OneId`, `s`.`ThreeId`, `s`.`Payload`, `s`.`Id`, `s`.`Name`
FROM `EntityThrees` AS `e`
LEFT JOIN (
    SELECT `j`.`OneId`, `j`.`ThreeId`, `j`.`Payload`, `e0`.`Id`, `e0`.`Name`
    FROM `JoinOneToThreePayloadFullShared` AS `j`
    INNER JOIN `EntityOnes` AS `e0` ON `j`.`OneId` = `e0`.`Id`
    WHERE `e0`.`Id` < 10
) AS `s` ON `e`.`Id` = `s`.`ThreeId`
ORDER BY `e`.`Id`, `s`.`OneId`, `s`.`ThreeId`
""");
    }

    public override async Task Filtered_include_skip_navigation_order_by(bool async)
    {
        await base.Filtered_include_skip_navigation_order_by(async);

        AssertSql(
            """
SELECT `e`.`Id`, `e`.`CollectionInverseId`, `e`.`Name`, `e`.`ReferenceInverseId`, `s`.`ThreeId`, `s`.`TwoId`, `s`.`Id`, `s`.`CollectionInverseId`, `s`.`ExtraId`, `s`.`Name`, `s`.`ReferenceInverseId`
FROM `EntityThrees` AS `e`
LEFT JOIN (
    SELECT `j`.`ThreeId`, `j`.`TwoId`, `e0`.`Id`, `e0`.`CollectionInverseId`, `e0`.`ExtraId`, `e0`.`Name`, `e0`.`ReferenceInverseId`
    FROM `JoinTwoToThree` AS `j`
    INNER JOIN `EntityTwos` AS `e0` ON `j`.`TwoId` = `e0`.`Id`
) AS `s` ON `e`.`Id` = `s`.`ThreeId`
ORDER BY `e`.`Id`, `s`.`Id`, `s`.`ThreeId`
""");
    }

    public override async Task Filtered_include_skip_navigation_order_by_skip(bool async)
    {
        await base.Filtered_include_skip_navigation_order_by_skip(async);

        AssertSql(
"""
SELECT [e].[Id], [e].[CollectionInverseId], [e].[ExtraId], [e].[Name], [e].[ReferenceInverseId], [t0].[SelfSkipSharedLeftId], [t0].[SelfSkipSharedRightId], [t0].[Id], [t0].[CollectionInverseId], [t0].[ExtraId], [t0].[Name], [t0].[ReferenceInverseId]
FROM [EntityTwos] AS [e]
LEFT JOIN (
    SELECT [t].[SelfSkipSharedLeftId], [t].[SelfSkipSharedRightId], [t].[Id], [t].[CollectionInverseId], [t].[ExtraId], [t].[Name], [t].[ReferenceInverseId]
    FROM (
        SELECT [e0].[SelfSkipSharedLeftId], [e0].[SelfSkipSharedRightId], [e1].[Id], [e1].[CollectionInverseId], [e1].[ExtraId], [e1].[Name], [e1].[ReferenceInverseId], ROW_NUMBER() OVER(PARTITION BY [e0].[SelfSkipSharedLeftId] ORDER BY [e1].[Id]) AS [row]
        FROM [EntityTwoEntityTwo] AS [e0]
        INNER JOIN [EntityTwos] AS [e1] ON [e0].[SelfSkipSharedRightId] = [e1].[Id]
    ) AS [t]
    WHERE 2 < [t].[row]
) AS [t0] ON [e].[Id] = [t0].[SelfSkipSharedLeftId]
ORDER BY [e].[Id], [t0].[SelfSkipSharedLeftId], [t0].[Id]
""");
    }

    public override async Task Filtered_include_skip_navigation_order_by_take(bool async)
    {
        await base.Filtered_include_skip_navigation_order_by_take(async);

        AssertSql(
"""
SELECT [e].[Key1], [e].[Key2], [e].[Key3], [e].[Name], [t0].[TwoSkipSharedId], [t0].[CompositeKeySkipSharedKey1], [t0].[CompositeKeySkipSharedKey2], [t0].[CompositeKeySkipSharedKey3], [t0].[Id], [t0].[CollectionInverseId], [t0].[ExtraId], [t0].[Name], [t0].[ReferenceInverseId]
FROM [EntityCompositeKeys] AS [e]
LEFT JOIN (
    SELECT [t].[TwoSkipSharedId], [t].[CompositeKeySkipSharedKey1], [t].[CompositeKeySkipSharedKey2], [t].[CompositeKeySkipSharedKey3], [t].[Id], [t].[CollectionInverseId], [t].[ExtraId], [t].[Name], [t].[ReferenceInverseId]
    FROM (
        SELECT [e0].[TwoSkipSharedId], [e0].[CompositeKeySkipSharedKey1], [e0].[CompositeKeySkipSharedKey2], [e0].[CompositeKeySkipSharedKey3], [e1].[Id], [e1].[CollectionInverseId], [e1].[ExtraId], [e1].[Name], [e1].[ReferenceInverseId], ROW_NUMBER() OVER(PARTITION BY [e0].[CompositeKeySkipSharedKey1], [e0].[CompositeKeySkipSharedKey2], [e0].[CompositeKeySkipSharedKey3] ORDER BY [e1].[Id]) AS [row]
        FROM [EntityCompositeKeyEntityTwo] AS [e0]
        INNER JOIN [EntityTwos] AS [e1] ON [e0].[TwoSkipSharedId] = [e1].[Id]
    ) AS [t]
    WHERE [t].[row] <= 2
) AS [t0] ON [e].[Key1] = [t0].[CompositeKeySkipSharedKey1] AND [e].[Key2] = [t0].[CompositeKeySkipSharedKey2] AND [e].[Key3] = [t0].[CompositeKeySkipSharedKey3]
ORDER BY [e].[Key1], [e].[Key2], [e].[Key3], [t0].[CompositeKeySkipSharedKey1], [t0].[CompositeKeySkipSharedKey2], [t0].[CompositeKeySkipSharedKey3], [t0].[Id]
""");
    }

    public override async Task Filtered_include_skip_navigation_order_by_skip_take(bool async)
    {
        await base.Filtered_include_skip_navigation_order_by_skip_take(async);

        AssertSql(
"""
SELECT [e].[Key1], [e].[Key2], [e].[Key3], [e].[Name], [t0].[Id], [t0].[CompositeId1], [t0].[CompositeId2], [t0].[CompositeId3], [t0].[ThreeId], [t0].[Id0], [t0].[CollectionInverseId], [t0].[Name], [t0].[ReferenceInverseId]
FROM [EntityCompositeKeys] AS [e]
LEFT JOIN (
    SELECT [t].[Id], [t].[CompositeId1], [t].[CompositeId2], [t].[CompositeId3], [t].[ThreeId], [t].[Id0], [t].[CollectionInverseId], [t].[Name], [t].[ReferenceInverseId]
    FROM (
        SELECT [j].[Id], [j].[CompositeId1], [j].[CompositeId2], [j].[CompositeId3], [j].[ThreeId], [e0].[Id] AS [Id0], [e0].[CollectionInverseId], [e0].[Name], [e0].[ReferenceInverseId], ROW_NUMBER() OVER(PARTITION BY [j].[CompositeId1], [j].[CompositeId2], [j].[CompositeId3] ORDER BY [e0].[Id]) AS [row]
        FROM [JoinThreeToCompositeKeyFull] AS [j]
        INNER JOIN [EntityThrees] AS [e0] ON [j].[ThreeId] = [e0].[Id]
    ) AS [t]
    WHERE 1 < [t].[row] AND [t].[row] <= 3
) AS [t0] ON [e].[Key1] = [t0].[CompositeId1] AND [e].[Key2] = [t0].[CompositeId2] AND [e].[Key3] = [t0].[CompositeId3]
ORDER BY [e].[Key1], [e].[Key2], [e].[Key3], [t0].[CompositeId1], [t0].[CompositeId2], [t0].[CompositeId3], [t0].[Id0]
""");
    }

    public override async Task Filtered_then_include_skip_navigation_where(bool async)
    {
        await base.Filtered_then_include_skip_navigation_where(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`Name`, `u`.`Number`, `u`.`Slumber`, `u`.`IsGreen`, `u`.`IsBrown`, `u`.`Discriminator`, `s0`.`RootSkipSharedId`, `s0`.`ThreeSkipSharedId`, `s0`.`Id`, `s0`.`CollectionInverseId`, `s0`.`Name`, `s0`.`ReferenceInverseId`, `s0`.`OneId`, `s0`.`ThreeId`, `s0`.`Payload`, `s0`.`Id0`, `s0`.`Name0`
FROM (
    SELECT `r`.`Id`, `r`.`Name`, CVar(NULL) AS `Number`, CVar(NULL) AS `Slumber`, CVar(NULL) AS `IsGreen`, CVar(NULL) AS `IsBrown`, 'EntityRoot' AS `Discriminator`
    FROM `Roots` AS `r`
    UNION ALL
    SELECT `b`.`Id`, `b`.`Name`, `b`.`Number`, CVar(NULL) AS `Slumber`, CVar(NULL) AS `IsGreen`, CVar(NULL) AS `IsBrown`, 'EntityBranch' AS `Discriminator`
    FROM `Branches` AS `b`
    UNION ALL
    SELECT `l`.`Id`, `l`.`Name`, `l`.`Number`, CVar(NULL) AS `Slumber`, `l`.`IsGreen`, CVar(NULL) AS `IsBrown`, 'EntityLeaf' AS `Discriminator`
    FROM `Leaves` AS `l`
    UNION ALL
    SELECT `l0`.`Id`, `l0`.`Name`, CVar(NULL) AS `Number`, `l0`.`Slumber`, CVar(NULL) AS `IsGreen`, `l0`.`IsBrown`, 'EntityLeaf2' AS `Discriminator`
    FROM `Leaf2s` AS `l0`
) AS `u`
LEFT JOIN (
    SELECT `e`.`RootSkipSharedId`, `e`.`ThreeSkipSharedId`, `e0`.`Id`, `e0`.`CollectionInverseId`, `e0`.`Name`, `e0`.`ReferenceInverseId`, `s`.`OneId`, `s`.`ThreeId`, `s`.`Payload`, `s`.`Id` AS `Id0`, `s`.`Name` AS `Name0`
    FROM (`EntityRootEntityThree` AS `e`
    INNER JOIN `EntityThrees` AS `e0` ON `e`.`ThreeSkipSharedId` = `e0`.`Id`)
    LEFT JOIN (
        SELECT `j`.`OneId`, `j`.`ThreeId`, `j`.`Payload`, `e1`.`Id`, `e1`.`Name`
        FROM `JoinOneToThreePayloadFullShared` AS `j`
        INNER JOIN `EntityOnes` AS `e1` ON `j`.`OneId` = `e1`.`Id`
        WHERE `e1`.`Id` < 10
    ) AS `s` ON `e0`.`Id` = `s`.`ThreeId`
) AS `s0` ON `u`.`Id` = `s0`.`RootSkipSharedId`
ORDER BY `u`.`Id`, `s0`.`RootSkipSharedId`, `s0`.`ThreeSkipSharedId`, `s0`.`Id`, `s0`.`OneId`, `s0`.`ThreeId`
""");
    }

    public override async Task Filtered_then_include_skip_navigation_order_by_skip_take(bool async)
    {
        await base.Filtered_then_include_skip_navigation_order_by_skip_take(async);

        AssertSql(
"""
SELECT [t].[Id], [t].[Name], [t].[Number], [t].[Slumber], [t].[IsGreen], [t].[IsBrown], [t].[Discriminator], [t1].[RootSkipSharedId], [t1].[CompositeKeySkipSharedKey1], [t1].[CompositeKeySkipSharedKey2], [t1].[CompositeKeySkipSharedKey3], [t1].[Key1], [t1].[Key2], [t1].[Key3], [t1].[Name], [t1].[Id], [t1].[CompositeId1], [t1].[CompositeId2], [t1].[CompositeId3], [t1].[ThreeId], [t1].[Id0], [t1].[CollectionInverseId], [t1].[Name0], [t1].[ReferenceInverseId]
FROM (
    SELECT [r].[Id], [r].[Name], NULL AS [Number], NULL AS [Slumber], NULL AS [IsGreen], NULL AS [IsBrown], N'EntityRoot' AS [Discriminator]
    FROM [Roots] AS [r]
    UNION ALL
    SELECT [b].[Id], [b].[Name], [b].[Number], NULL AS [Slumber], NULL AS [IsGreen], NULL AS [IsBrown], N'EntityBranch' AS [Discriminator]
    FROM [Branches] AS [b]
    UNION ALL
    SELECT [l].[Id], [l].[Name], [l].[Number], NULL AS [Slumber], [l].[IsGreen], NULL AS [IsBrown], N'EntityLeaf' AS [Discriminator]
    FROM [Leaves] AS [l]
    UNION ALL
    SELECT [l0].[Id], [l0].[Name], NULL AS [Number], [l0].[Slumber], NULL AS [IsGreen], [l0].[IsBrown], N'EntityLeaf2' AS [Discriminator]
    FROM [Leaf2s] AS [l0]
) AS [t]
LEFT JOIN (
    SELECT [e].[RootSkipSharedId], [e].[CompositeKeySkipSharedKey1], [e].[CompositeKeySkipSharedKey2], [e].[CompositeKeySkipSharedKey3], [e0].[Key1], [e0].[Key2], [e0].[Key3], [e0].[Name], [t0].[Id], [t0].[CompositeId1], [t0].[CompositeId2], [t0].[CompositeId3], [t0].[ThreeId], [t0].[Id0], [t0].[CollectionInverseId], [t0].[Name] AS [Name0], [t0].[ReferenceInverseId]
    FROM [EntityCompositeKeyEntityRoot] AS [e]
    INNER JOIN [EntityCompositeKeys] AS [e0] ON [e].[CompositeKeySkipSharedKey1] = [e0].[Key1] AND [e].[CompositeKeySkipSharedKey2] = [e0].[Key2] AND [e].[CompositeKeySkipSharedKey3] = [e0].[Key3]
    LEFT JOIN (
        SELECT [t2].[Id], [t2].[CompositeId1], [t2].[CompositeId2], [t2].[CompositeId3], [t2].[ThreeId], [t2].[Id0], [t2].[CollectionInverseId], [t2].[Name], [t2].[ReferenceInverseId]
        FROM (
            SELECT [j].[Id], [j].[CompositeId1], [j].[CompositeId2], [j].[CompositeId3], [j].[ThreeId], [e1].[Id] AS [Id0], [e1].[CollectionInverseId], [e1].[Name], [e1].[ReferenceInverseId], ROW_NUMBER() OVER(PARTITION BY [j].[CompositeId1], [j].[CompositeId2], [j].[CompositeId3] ORDER BY [e1].[Id]) AS [row]
            FROM [JoinThreeToCompositeKeyFull] AS [j]
            INNER JOIN [EntityThrees] AS [e1] ON [j].[ThreeId] = [e1].[Id]
        ) AS [t2]
        WHERE 1 < [t2].[row] AND [t2].[row] <= 3
    ) AS [t0] ON [e0].[Key1] = [t0].[CompositeId1] AND [e0].[Key2] = [t0].[CompositeId2] AND [e0].[Key3] = [t0].[CompositeId3]
) AS [t1] ON [t].[Id] = [t1].[RootSkipSharedId]
ORDER BY [t].[Id], [t1].[RootSkipSharedId], [t1].[CompositeKeySkipSharedKey1], [t1].[CompositeKeySkipSharedKey2], [t1].[CompositeKeySkipSharedKey3], [t1].[Key1], [t1].[Key2], [t1].[Key3], [t1].[CompositeId1], [t1].[CompositeId2], [t1].[CompositeId3], [t1].[Id0]
""");
    }

    public override async Task Filtered_include_skip_navigation_where_then_include_skip_navigation(bool async)
    {
        await base.Filtered_include_skip_navigation_where_then_include_skip_navigation(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Name`, `l`.`Number`, `l`.`IsGreen`, `s0`.`LeafId`, `s0`.`CompositeId1`, `s0`.`CompositeId2`, `s0`.`CompositeId3`, `s0`.`Key1`, `s0`.`Key2`, `s0`.`Key3`, `s0`.`Name`, `s0`.`TwoSkipSharedId`, `s0`.`CompositeKeySkipSharedKey1`, `s0`.`CompositeKeySkipSharedKey2`, `s0`.`CompositeKeySkipSharedKey3`, `s0`.`Id`, `s0`.`CollectionInverseId`, `s0`.`ExtraId`, `s0`.`Name0`, `s0`.`ReferenceInverseId`
FROM `Leaves` AS `l`
LEFT JOIN (
    SELECT `j`.`LeafId`, `j`.`CompositeId1`, `j`.`CompositeId2`, `j`.`CompositeId3`, `e`.`Key1`, `e`.`Key2`, `e`.`Key3`, `e`.`Name`, `s`.`TwoSkipSharedId`, `s`.`CompositeKeySkipSharedKey1`, `s`.`CompositeKeySkipSharedKey2`, `s`.`CompositeKeySkipSharedKey3`, `s`.`Id`, `s`.`CollectionInverseId`, `s`.`ExtraId`, `s`.`Name` AS `Name0`, `s`.`ReferenceInverseId`
    FROM (`JoinCompositeKeyToLeaf` AS `j`
    INNER JOIN `EntityCompositeKeys` AS `e` ON `j`.`CompositeId1` = `e`.`Key1` AND `j`.`CompositeId2` = `e`.`Key2` AND `j`.`CompositeId3` = `e`.`Key3`)
    LEFT JOIN (
        SELECT `e0`.`TwoSkipSharedId`, `e0`.`CompositeKeySkipSharedKey1`, `e0`.`CompositeKeySkipSharedKey2`, `e0`.`CompositeKeySkipSharedKey3`, `e1`.`Id`, `e1`.`CollectionInverseId`, `e1`.`ExtraId`, `e1`.`Name`, `e1`.`ReferenceInverseId`
        FROM `EntityCompositeKeyEntityTwo` AS `e0`
        INNER JOIN `EntityTwos` AS `e1` ON `e0`.`TwoSkipSharedId` = `e1`.`Id`
    ) AS `s` ON `e`.`Key1` = `s`.`CompositeKeySkipSharedKey1` AND `e`.`Key2` = `s`.`CompositeKeySkipSharedKey2` AND `e`.`Key3` = `s`.`CompositeKeySkipSharedKey3`
    WHERE `e`.`Key1` < 5
) AS `s0` ON `l`.`Id` = `s0`.`LeafId`
ORDER BY `l`.`Id`, `s0`.`LeafId`, `s0`.`CompositeId1`, `s0`.`CompositeId2`, `s0`.`CompositeId3`, `s0`.`Key1`, `s0`.`Key2`, `s0`.`Key3`, `s0`.`TwoSkipSharedId`, `s0`.`CompositeKeySkipSharedKey1`, `s0`.`CompositeKeySkipSharedKey2`, `s0`.`CompositeKeySkipSharedKey3`
""");
    }

    public override async Task Filtered_include_skip_navigation_order_by_skip_take_then_include_skip_navigation_where(bool async)
    {
        await base.Filtered_include_skip_navigation_order_by_skip_take_then_include_skip_navigation_where(async);

        AssertSql(
"""
SELECT [e].[Id], [e].[Name], [t1].[OneId], [t1].[TwoId], [t1].[JoinOneToTwoExtraId], [t1].[Id], [t1].[CollectionInverseId], [t1].[ExtraId], [t1].[Name], [t1].[ReferenceInverseId], [t1].[ThreeId], [t1].[TwoId0], [t1].[Id0], [t1].[CollectionInverseId0], [t1].[Name0], [t1].[ReferenceInverseId0]
FROM [EntityOnes] AS [e]
OUTER APPLY (
    SELECT [t].[OneId], [t].[TwoId], [t].[JoinOneToTwoExtraId], [t].[Id], [t].[CollectionInverseId], [t].[ExtraId], [t].[Name], [t].[ReferenceInverseId], [t0].[ThreeId], [t0].[TwoId] AS [TwoId0], [t0].[Id] AS [Id0], [t0].[CollectionInverseId] AS [CollectionInverseId0], [t0].[Name] AS [Name0], [t0].[ReferenceInverseId] AS [ReferenceInverseId0]
    FROM (
        SELECT [j].[OneId], [j].[TwoId], [j].[JoinOneToTwoExtraId], [e0].[Id], [e0].[CollectionInverseId], [e0].[ExtraId], [e0].[Name], [e0].[ReferenceInverseId]
        FROM [JoinOneToTwo] AS [j]
        INNER JOIN [EntityTwos] AS [e0] ON [j].[TwoId] = [e0].[Id]
        WHERE [e].[Id] = [j].[OneId]
        ORDER BY [e0].[Id]
        OFFSET 1 ROWS FETCH NEXT 2 ROWS ONLY
    ) AS [t]
    LEFT JOIN (
        SELECT [j0].[ThreeId], [j0].[TwoId], [e1].[Id], [e1].[CollectionInverseId], [e1].[Name], [e1].[ReferenceInverseId]
        FROM [JoinTwoToThree] AS [j0]
        INNER JOIN [EntityThrees] AS [e1] ON [j0].[ThreeId] = [e1].[Id]
        WHERE [e1].[Id] < 10
    ) AS [t0] ON [t].[Id] = [t0].[TwoId]
) AS [t1]
ORDER BY [e].[Id], [t1].[Id], [t1].[OneId], [t1].[TwoId], [t1].[ThreeId], [t1].[TwoId0]
""");
    }

    public override async Task Filtered_include_skip_navigation_where_then_include_skip_navigation_order_by_skip_take(bool async)
    {
        await base.Filtered_include_skip_navigation_where_then_include_skip_navigation_order_by_skip_take(async);

        AssertSql(
"""
SELECT [e].[Id], [e].[Name], [t1].[OneId], [t1].[TwoId], [t1].[JoinOneToTwoExtraId], [t1].[Id], [t1].[CollectionInverseId], [t1].[ExtraId], [t1].[Name], [t1].[ReferenceInverseId], [t1].[ThreeId], [t1].[TwoId0], [t1].[Id0], [t1].[CollectionInverseId0], [t1].[Name0], [t1].[ReferenceInverseId0]
FROM [EntityOnes] AS [e]
LEFT JOIN (
    SELECT [j].[OneId], [j].[TwoId], [j].[JoinOneToTwoExtraId], [e0].[Id], [e0].[CollectionInverseId], [e0].[ExtraId], [e0].[Name], [e0].[ReferenceInverseId], [t0].[ThreeId], [t0].[TwoId] AS [TwoId0], [t0].[Id] AS [Id0], [t0].[CollectionInverseId] AS [CollectionInverseId0], [t0].[Name] AS [Name0], [t0].[ReferenceInverseId] AS [ReferenceInverseId0]
    FROM [JoinOneToTwo] AS [j]
    INNER JOIN [EntityTwos] AS [e0] ON [j].[TwoId] = [e0].[Id]
    LEFT JOIN (
        SELECT [t].[ThreeId], [t].[TwoId], [t].[Id], [t].[CollectionInverseId], [t].[Name], [t].[ReferenceInverseId]
        FROM (
            SELECT [j0].[ThreeId], [j0].[TwoId], [e1].[Id], [e1].[CollectionInverseId], [e1].[Name], [e1].[ReferenceInverseId], ROW_NUMBER() OVER(PARTITION BY [j0].[TwoId] ORDER BY [e1].[Id]) AS [row]
            FROM [JoinTwoToThree] AS [j0]
            INNER JOIN [EntityThrees] AS [e1] ON [j0].[ThreeId] = [e1].[Id]
        ) AS [t]
        WHERE 1 < [t].[row] AND [t].[row] <= 3
    ) AS [t0] ON [e0].[Id] = [t0].[TwoId]
    WHERE [e0].[Id] < 10
) AS [t1] ON [e].[Id] = [t1].[OneId]
ORDER BY [e].[Id], [t1].[OneId], [t1].[TwoId], [t1].[Id], [t1].[TwoId0], [t1].[Id0]
""");
    }

    public override async Task Filter_include_on_skip_navigation_combined(bool async)
    {
        await base.Filter_include_on_skip_navigation_combined(async);

        AssertSql(
            """
SELECT `e`.`Id`, `e`.`CollectionInverseId`, `e`.`ExtraId`, `e`.`Name`, `e`.`ReferenceInverseId`, `s`.`OneId`, `s`.`TwoId`, `s`.`JoinOneToTwoExtraId`, `s`.`Id`, `s`.`Name`, `s`.`Id0`, `s`.`CollectionInverseId`, `s`.`ExtraId`, `s`.`Name0`, `s`.`ReferenceInverseId`, `s`.`Id1`, `s`.`CollectionInverseId0`, `s`.`ExtraId0`, `s`.`Name1`, `s`.`ReferenceInverseId0`
FROM `EntityTwos` AS `e`
LEFT JOIN (
    SELECT `j`.`OneId`, `j`.`TwoId`, `j`.`JoinOneToTwoExtraId`, `e0`.`Id`, `e0`.`Name`, `e1`.`Id` AS `Id0`, `e1`.`CollectionInverseId`, `e1`.`ExtraId`, `e1`.`Name` AS `Name0`, `e1`.`ReferenceInverseId`, `e2`.`Id` AS `Id1`, `e2`.`CollectionInverseId` AS `CollectionInverseId0`, `e2`.`ExtraId` AS `ExtraId0`, `e2`.`Name` AS `Name1`, `e2`.`ReferenceInverseId` AS `ReferenceInverseId0`
    FROM ((`JoinOneToTwo` AS `j`
    INNER JOIN `EntityOnes` AS `e0` ON `j`.`OneId` = `e0`.`Id`)
    LEFT JOIN `EntityTwos` AS `e1` ON `e0`.`Id` = `e1`.`ReferenceInverseId`)
    LEFT JOIN `EntityTwos` AS `e2` ON `e0`.`Id` = `e2`.`CollectionInverseId`
    WHERE `e0`.`Id` < 10
) AS `s` ON `e`.`Id` = `s`.`TwoId`
ORDER BY `e`.`Id`, `s`.`OneId`, `s`.`TwoId`, `s`.`Id`, `s`.`Id0`
""");
    }

    public override async Task Filter_include_on_skip_navigation_combined_with_filtered_then_includes(bool async)
    {
        await base.Filter_include_on_skip_navigation_combined_with_filtered_then_includes(async);

        AssertSql(
"""
SELECT [e].[Id], [e].[CollectionInverseId], [e].[Name], [e].[ReferenceInverseId], [t3].[OneId], [t3].[ThreeId], [t3].[Payload], [t3].[Id], [t3].[Name], [t3].[OneId0], [t3].[TwoId], [t3].[JoinOneToTwoExtraId], [t3].[Id0], [t3].[CollectionInverseId], [t3].[ExtraId], [t3].[Name0], [t3].[ReferenceInverseId], [t3].[EntityBranchId], [t3].[EntityOneId], [t3].[Id1], [t3].[Name1], [t3].[Number], [t3].[IsGreen], [t3].[Discriminator]
FROM [EntityThrees] AS [e]
LEFT JOIN (
    SELECT [j].[OneId], [j].[ThreeId], [j].[Payload], [e0].[Id], [e0].[Name], [t0].[OneId] AS [OneId0], [t0].[TwoId], [t0].[JoinOneToTwoExtraId], [t0].[Id] AS [Id0], [t0].[CollectionInverseId], [t0].[ExtraId], [t0].[Name] AS [Name0], [t0].[ReferenceInverseId], [t1].[EntityBranchId], [t1].[EntityOneId], [t1].[Id] AS [Id1], [t1].[Name] AS [Name1], [t1].[Number], [t1].[IsGreen], [t1].[Discriminator]
    FROM [JoinOneToThreePayloadFull] AS [j]
    INNER JOIN [EntityOnes] AS [e0] ON [j].[OneId] = [e0].[Id]
    LEFT JOIN (
        SELECT [t].[OneId], [t].[TwoId], [t].[JoinOneToTwoExtraId], [t].[Id], [t].[CollectionInverseId], [t].[ExtraId], [t].[Name], [t].[ReferenceInverseId]
        FROM (
            SELECT [j0].[OneId], [j0].[TwoId], [j0].[JoinOneToTwoExtraId], [e1].[Id], [e1].[CollectionInverseId], [e1].[ExtraId], [e1].[Name], [e1].[ReferenceInverseId], ROW_NUMBER() OVER(PARTITION BY [j0].[OneId] ORDER BY [e1].[Id]) AS [row]
            FROM [JoinOneToTwo] AS [j0]
            INNER JOIN [EntityTwos] AS [e1] ON [j0].[TwoId] = [e1].[Id]
        ) AS [t]
        WHERE 1 < [t].[row] AND [t].[row] <= 3
    ) AS [t0] ON [e0].[Id] = [t0].[OneId]
    LEFT JOIN (
        SELECT [j1].[EntityBranchId], [j1].[EntityOneId], [t2].[Id], [t2].[Name], [t2].[Number], [t2].[IsGreen], [t2].[Discriminator]
        FROM [JoinOneToBranch] AS [j1]
        INNER JOIN (
            SELECT [b].[Id], [b].[Name], [b].[Number], NULL AS [IsGreen], N'EntityBranch' AS [Discriminator]
            FROM [Branches] AS [b]
            UNION ALL
            SELECT [l].[Id], [l].[Name], [l].[Number], [l].[IsGreen], N'EntityLeaf' AS [Discriminator]
            FROM [Leaves] AS [l]
        ) AS [t2] ON [j1].[EntityBranchId] = [t2].[Id]
        WHERE [t2].[Id] < 20
    ) AS [t1] ON [e0].[Id] = [t1].[EntityOneId]
    WHERE [e0].[Id] < 10
) AS [t3] ON [e].[Id] = [t3].[ThreeId]
ORDER BY [e].[Id], [t3].[OneId], [t3].[ThreeId], [t3].[Id], [t3].[OneId0], [t3].[Id0], [t3].[TwoId], [t3].[EntityBranchId], [t3].[EntityOneId]
""");
    }

    public override async Task Filtered_include_on_skip_navigation_then_filtered_include_on_navigation(bool async)
    {
        await base.Filtered_include_on_skip_navigation_then_filtered_include_on_navigation(async);

        AssertSql(
            """
SELECT `e`.`Id`, `e`.`CollectionInverseId`, `e`.`Name`, `e`.`ReferenceInverseId`, `s`.`OneId`, `s`.`ThreeId`, `s`.`Payload`, `s`.`Id`, `s`.`Name`, `s`.`Id0`, `s`.`CollectionInverseId`, `s`.`ExtraId`, `s`.`Name0`, `s`.`ReferenceInverseId`
FROM `EntityThrees` AS `e`
LEFT JOIN (
    SELECT `j`.`OneId`, `j`.`ThreeId`, `j`.`Payload`, `e0`.`Id`, `e0`.`Name`, `e2`.`Id` AS `Id0`, `e2`.`CollectionInverseId`, `e2`.`ExtraId`, `e2`.`Name` AS `Name0`, `e2`.`ReferenceInverseId`
    FROM (`JoinOneToThreePayloadFull` AS `j`
    INNER JOIN `EntityOnes` AS `e0` ON `j`.`OneId` = `e0`.`Id`)
    LEFT JOIN (
        SELECT `e1`.`Id`, `e1`.`CollectionInverseId`, `e1`.`ExtraId`, `e1`.`Name`, `e1`.`ReferenceInverseId`
        FROM `EntityTwos` AS `e1`
        WHERE `e1`.`Id` < 5
    ) AS `e2` ON `e0`.`Id` = `e2`.`CollectionInverseId`
    WHERE `e0`.`Id` > 15
) AS `s` ON `e`.`Id` = `s`.`ThreeId`
ORDER BY `e`.`Id`, `s`.`OneId`, `s`.`ThreeId`, `s`.`Id`
""");
    }

    public override async Task Filtered_include_on_navigation_then_filtered_include_on_skip_navigation(bool async)
    {
        await base.Filtered_include_on_navigation_then_filtered_include_on_skip_navigation(async);

        AssertSql(
            """
SELECT `e`.`Id`, `e`.`Name`, `s0`.`Id`, `s0`.`CollectionInverseId`, `s0`.`ExtraId`, `s0`.`Name`, `s0`.`ReferenceInverseId`, `s0`.`ThreeId`, `s0`.`TwoId`, `s0`.`Id0`, `s0`.`CollectionInverseId0`, `s0`.`Name0`, `s0`.`ReferenceInverseId0`
FROM `EntityOnes` AS `e`
LEFT JOIN (
    SELECT `e0`.`Id`, `e0`.`CollectionInverseId`, `e0`.`ExtraId`, `e0`.`Name`, `e0`.`ReferenceInverseId`, `s`.`ThreeId`, `s`.`TwoId`, `s`.`Id` AS `Id0`, `s`.`CollectionInverseId` AS `CollectionInverseId0`, `s`.`Name` AS `Name0`, `s`.`ReferenceInverseId` AS `ReferenceInverseId0`
    FROM `EntityTwos` AS `e0`
    LEFT JOIN (
        SELECT `j`.`ThreeId`, `j`.`TwoId`, `e1`.`Id`, `e1`.`CollectionInverseId`, `e1`.`Name`, `e1`.`ReferenceInverseId`
        FROM `JoinTwoToThree` AS `j`
        INNER JOIN `EntityThrees` AS `e1` ON `j`.`ThreeId` = `e1`.`Id`
        WHERE `e1`.`Id` < 5
    ) AS `s` ON `e0`.`Id` = `s`.`TwoId`
    WHERE `e0`.`Id` > 15
) AS `s0` ON `e`.`Id` = `s0`.`CollectionInverseId`
ORDER BY `e`.`Id`, `s0`.`Id`, `s0`.`ThreeId`, `s0`.`TwoId`
""");
    }

    public override async Task Includes_accessed_via_different_path_are_merged(bool async)
    {
        await base.Includes_accessed_via_different_path_are_merged(async);

        AssertSql();
    }

    public override async Task Filtered_includes_accessed_via_different_path_are_merged(bool async)
    {
        await base.Filtered_includes_accessed_via_different_path_are_merged(async);

        AssertSql();
    }

    public override async Task Include_skip_navigation_split(bool async)
    {
        await base.Include_skip_navigation_split(async);

        AssertSql(
            """
SELECT `e`.`Key1`, `e`.`Key2`, `e`.`Key3`, `e`.`Name`
FROM `EntityCompositeKeys` AS `e`
ORDER BY `e`.`Key1`, `e`.`Key2`, `e`.`Key3`
""",
            //
            """
SELECT `s`.`RootSkipSharedId`, `s`.`CompositeKeySkipSharedKey1`, `s`.`CompositeKeySkipSharedKey2`, `s`.`CompositeKeySkipSharedKey3`, `s`.`Id`, `s`.`Name`, `s`.`Number`, `s`.`Slumber`, `s`.`IsGreen`, `s`.`IsBrown`, `s`.`Discriminator`, `e`.`Key1`, `e`.`Key2`, `e`.`Key3`
FROM `EntityCompositeKeys` AS `e`
INNER JOIN (
    SELECT `e0`.`RootSkipSharedId`, `e0`.`CompositeKeySkipSharedKey1`, `e0`.`CompositeKeySkipSharedKey2`, `e0`.`CompositeKeySkipSharedKey3`, `u`.`Id`, `u`.`Name`, `u`.`Number`, `u`.`Slumber`, `u`.`IsGreen`, `u`.`IsBrown`, `u`.`Discriminator`
    FROM `EntityCompositeKeyEntityRoot` AS `e0`
    INNER JOIN (
        SELECT `r`.`Id`, `r`.`Name`, CVar(NULL) AS `Number`, CVar(NULL) AS `Slumber`, CVar(NULL) AS `IsGreen`, CVar(NULL) AS `IsBrown`, 'EntityRoot' AS `Discriminator`
        FROM `Roots` AS `r`
        UNION ALL
        SELECT `b`.`Id`, `b`.`Name`, `b`.`Number`, CVar(NULL) AS `Slumber`, CVar(NULL) AS `IsGreen`, CVar(NULL) AS `IsBrown`, 'EntityBranch' AS `Discriminator`
        FROM `Branches` AS `b`
        UNION ALL
        SELECT `l`.`Id`, `l`.`Name`, `l`.`Number`, CVar(NULL) AS `Slumber`, `l`.`IsGreen`, CVar(NULL) AS `IsBrown`, 'EntityLeaf' AS `Discriminator`
        FROM `Leaves` AS `l`
        UNION ALL
        SELECT `l0`.`Id`, `l0`.`Name`, CVar(NULL) AS `Number`, `l0`.`Slumber`, CVar(NULL) AS `IsGreen`, `l0`.`IsBrown`, 'EntityLeaf2' AS `Discriminator`
        FROM `Leaf2s` AS `l0`
    ) AS `u` ON `e0`.`RootSkipSharedId` = `u`.`Id`
) AS `s` ON `e`.`Key1` = `s`.`CompositeKeySkipSharedKey1` AND `e`.`Key2` = `s`.`CompositeKeySkipSharedKey2` AND `e`.`Key3` = `s`.`CompositeKeySkipSharedKey3`
ORDER BY `e`.`Key1`, `e`.`Key2`, `e`.`Key3`
""");
    }

    public override async Task Include_skip_navigation_then_reference_split(bool async)
    {
        await base.Include_skip_navigation_then_reference_split(async);

        AssertSql(
            """
SELECT `e`.`Id`, `e`.`CollectionInverseId`, `e`.`ExtraId`, `e`.`Name`, `e`.`ReferenceInverseId`
FROM `EntityTwos` AS `e`
ORDER BY `e`.`Id`
""",
            //
            """
SELECT `s`.`OneId`, `s`.`TwoId`, `s`.`JoinOneToTwoExtraId`, `s`.`Id`, `s`.`Name`, `s`.`Id0`, `s`.`CollectionInverseId`, `s`.`ExtraId`, `s`.`Name0`, `s`.`ReferenceInverseId`, `e`.`Id`
FROM `EntityTwos` AS `e`
INNER JOIN (
    SELECT `j`.`OneId`, `j`.`TwoId`, `j`.`JoinOneToTwoExtraId`, `e0`.`Id`, `e0`.`Name`, `e1`.`Id` AS `Id0`, `e1`.`CollectionInverseId`, `e1`.`ExtraId`, `e1`.`Name` AS `Name0`, `e1`.`ReferenceInverseId`
    FROM (`JoinOneToTwo` AS `j`
    INNER JOIN `EntityOnes` AS `e0` ON `j`.`OneId` = `e0`.`Id`)
    LEFT JOIN `EntityTwos` AS `e1` ON `e0`.`Id` = `e1`.`ReferenceInverseId`
) AS `s` ON `e`.`Id` = `s`.`TwoId`
ORDER BY `e`.`Id`
""");
    }

    public override async Task Include_skip_navigation_then_include_skip_navigation_split(bool async)
    {
        await base.Include_skip_navigation_then_include_skip_navigation_split(async);

        AssertSql(
            """
SELECT `e`.`Key1`, `e`.`Key2`, `e`.`Key3`, `e`.`Name`
FROM `EntityCompositeKeys` AS `e`
ORDER BY `e`.`Key1`, `e`.`Key2`, `e`.`Key3`
""",
            //
            """
SELECT `s`.`LeafId`, `s`.`CompositeId1`, `s`.`CompositeId2`, `s`.`CompositeId3`, `s`.`Id`, `s`.`Name`, `s`.`Number`, `s`.`IsGreen`, `e`.`Key1`, `e`.`Key2`, `e`.`Key3`
FROM `EntityCompositeKeys` AS `e`
INNER JOIN (
    SELECT `j`.`LeafId`, `j`.`CompositeId1`, `j`.`CompositeId2`, `j`.`CompositeId3`, `l`.`Id`, `l`.`Name`, `l`.`Number`, `l`.`IsGreen`
    FROM `JoinCompositeKeyToLeaf` AS `j`
    INNER JOIN `Leaves` AS `l` ON `j`.`LeafId` = `l`.`Id`
) AS `s` ON `e`.`Key1` = `s`.`CompositeId1` AND `e`.`Key2` = `s`.`CompositeId2` AND `e`.`Key3` = `s`.`CompositeId3`
ORDER BY `e`.`Key1`, `e`.`Key2`, `e`.`Key3`, `s`.`LeafId`, `s`.`CompositeId1`, `s`.`CompositeId2`, `s`.`CompositeId3`, `s`.`Id`
""",
            //
            """
SELECT `s0`.`EntityBranchId`, `s0`.`EntityOneId`, `s0`.`Id`, `s0`.`Name`, `e`.`Key1`, `e`.`Key2`, `e`.`Key3`, `s`.`LeafId`, `s`.`CompositeId1`, `s`.`CompositeId2`, `s`.`CompositeId3`, `s`.`Id`
FROM (`EntityCompositeKeys` AS `e`
INNER JOIN (
    SELECT `j`.`LeafId`, `j`.`CompositeId1`, `j`.`CompositeId2`, `j`.`CompositeId3`, `l`.`Id`
    FROM `JoinCompositeKeyToLeaf` AS `j`
    INNER JOIN `Leaves` AS `l` ON `j`.`LeafId` = `l`.`Id`
) AS `s` ON `e`.`Key1` = `s`.`CompositeId1` AND `e`.`Key2` = `s`.`CompositeId2` AND `e`.`Key3` = `s`.`CompositeId3`)
LEFT JOIN (
    SELECT `j0`.`EntityBranchId`, `j0`.`EntityOneId`, `e0`.`Id`, `e0`.`Name`
    FROM `JoinOneToBranch` AS `j0`
    INNER JOIN `EntityOnes` AS `e0` ON `j0`.`EntityOneId` = `e0`.`Id`
) AS `s0` ON `s`.`Id` = `s0`.`EntityBranchId`
WHERE `s`.`Id` IS NOT NULL AND `s0`.`EntityBranchId` IS NOT NULL
ORDER BY `e`.`Key1`, `e`.`Key2`, `e`.`Key3`, `s`.`LeafId`, `s`.`CompositeId1`, `s`.`CompositeId2`, `s`.`CompositeId3`, `s`.`Id`
""");
    }

    public override async Task Include_skip_navigation_then_include_reference_and_skip_navigation_split(bool async)
    {
        await base.Include_skip_navigation_then_include_reference_and_skip_navigation_split(async);

        AssertSql(
            """
SELECT `e`.`Id`, `e`.`CollectionInverseId`, `e`.`Name`, `e`.`ReferenceInverseId`
FROM `EntityThrees` AS `e`
ORDER BY `e`.`Id`
""",
            //
            """
SELECT `s`.`OneId`, `s`.`ThreeId`, `s`.`Payload`, `s`.`Id`, `s`.`Name`, `s`.`Id0`, `s`.`CollectionInverseId`, `s`.`ExtraId`, `s`.`Name0`, `s`.`ReferenceInverseId`, `e`.`Id`
FROM `EntityThrees` AS `e`
INNER JOIN (
    SELECT `j`.`OneId`, `j`.`ThreeId`, `j`.`Payload`, `e0`.`Id`, `e0`.`Name`, `e1`.`Id` AS `Id0`, `e1`.`CollectionInverseId`, `e1`.`ExtraId`, `e1`.`Name` AS `Name0`, `e1`.`ReferenceInverseId`
    FROM (`JoinOneToThreePayloadFull` AS `j`
    INNER JOIN `EntityOnes` AS `e0` ON `j`.`OneId` = `e0`.`Id`)
    LEFT JOIN `EntityTwos` AS `e1` ON `e0`.`Id` = `e1`.`ReferenceInverseId`
) AS `s` ON `e`.`Id` = `s`.`ThreeId`
ORDER BY `e`.`Id`, `s`.`OneId`, `s`.`ThreeId`, `s`.`Id`, `s`.`Id0`
""",
            //
            """
SELECT `s0`.`LeftId`, `s0`.`RightId`, `s0`.`Payload`, `s0`.`Id`, `s0`.`Name`, `e`.`Id`, `s`.`OneId`, `s`.`ThreeId`, `s`.`Id`, `s`.`Id0`
FROM (`EntityThrees` AS `e`
INNER JOIN (
    SELECT `j`.`OneId`, `j`.`ThreeId`, `e0`.`Id`, `e1`.`Id` AS `Id0`
    FROM (`JoinOneToThreePayloadFull` AS `j`
    INNER JOIN `EntityOnes` AS `e0` ON `j`.`OneId` = `e0`.`Id`)
    LEFT JOIN `EntityTwos` AS `e1` ON `e0`.`Id` = `e1`.`ReferenceInverseId`
) AS `s` ON `e`.`Id` = `s`.`ThreeId`)
LEFT JOIN (
    SELECT `j0`.`LeftId`, `j0`.`RightId`, `j0`.`Payload`, `e2`.`Id`, `e2`.`Name`
    FROM `JoinOneSelfPayload` AS `j0`
    INNER JOIN `EntityOnes` AS `e2` ON `j0`.`RightId` = `e2`.`Id`
) AS `s0` ON `s`.`Id` = `s0`.`LeftId`
WHERE `s`.`Id` IS NOT NULL AND `s0`.`LeftId` IS NOT NULL
ORDER BY `e`.`Id`, `s`.`OneId`, `s`.`ThreeId`, `s`.`Id`, `s`.`Id0`
""");
    }

    public override async Task Include_skip_navigation_and_reference_split(bool async)
    {
        await base.Include_skip_navigation_and_reference_split(async);

        AssertSql(
            """
SELECT `e`.`Id`, `e`.`CollectionInverseId`, `e`.`ExtraId`, `e`.`Name`, `e`.`ReferenceInverseId`, `e0`.`Id`, `e0`.`CollectionInverseId`, `e0`.`Name`, `e0`.`ReferenceInverseId`
FROM `EntityTwos` AS `e`
LEFT JOIN `EntityThrees` AS `e0` ON `e`.`Id` = `e0`.`ReferenceInverseId`
ORDER BY `e`.`Id`, `e0`.`Id`
""",
            //
            """
SELECT `s`.`OneSkipSharedId`, `s`.`TwoSkipSharedId`, `s`.`Id`, `s`.`Name`, `e`.`Id`, `e0`.`Id`
FROM (`EntityTwos` AS `e`
LEFT JOIN `EntityThrees` AS `e0` ON `e`.`Id` = `e0`.`ReferenceInverseId`)
INNER JOIN (
    SELECT `e1`.`OneSkipSharedId`, `e1`.`TwoSkipSharedId`, `e2`.`Id`, `e2`.`Name`
    FROM `EntityOneEntityTwo` AS `e1`
    INNER JOIN `EntityOnes` AS `e2` ON `e1`.`OneSkipSharedId` = `e2`.`Id`
) AS `s` ON `e`.`Id` = `s`.`TwoSkipSharedId`
ORDER BY `e`.`Id`, `e0`.`Id`
""");
    }

    public override async Task Filtered_include_skip_navigation_where_split(bool async)
    {
        await base.Filtered_include_skip_navigation_where_split(async);

        AssertSql(
            """
SELECT `e`.`Id`, `e`.`CollectionInverseId`, `e`.`Name`, `e`.`ReferenceInverseId`
FROM `EntityThrees` AS `e`
ORDER BY `e`.`Id`
""",
            //
            """
SELECT `s`.`OneId`, `s`.`ThreeId`, `s`.`Payload`, `s`.`Id`, `s`.`Name`, `e`.`Id`
FROM `EntityThrees` AS `e`
INNER JOIN (
    SELECT `j`.`OneId`, `j`.`ThreeId`, `j`.`Payload`, `e0`.`Id`, `e0`.`Name`
    FROM `JoinOneToThreePayloadFullShared` AS `j`
    INNER JOIN `EntityOnes` AS `e0` ON `j`.`OneId` = `e0`.`Id`
    WHERE `e0`.`Id` < 10
) AS `s` ON `e`.`Id` = `s`.`ThreeId`
ORDER BY `e`.`Id`
""");
    }

    public override async Task Filtered_include_skip_navigation_order_by_split(bool async)
    {
        await base.Filtered_include_skip_navigation_order_by_split(async);

        AssertSql(
            """
SELECT `e`.`Id`, `e`.`CollectionInverseId`, `e`.`Name`, `e`.`ReferenceInverseId`
FROM `EntityThrees` AS `e`
ORDER BY `e`.`Id`
""",
            //
            """
SELECT `s`.`ThreeId`, `s`.`TwoId`, `s`.`Id`, `s`.`CollectionInverseId`, `s`.`ExtraId`, `s`.`Name`, `s`.`ReferenceInverseId`, `e`.`Id`
FROM `EntityThrees` AS `e`
INNER JOIN (
    SELECT `j`.`ThreeId`, `j`.`TwoId`, `e0`.`Id`, `e0`.`CollectionInverseId`, `e0`.`ExtraId`, `e0`.`Name`, `e0`.`ReferenceInverseId`
    FROM `JoinTwoToThree` AS `j`
    INNER JOIN `EntityTwos` AS `e0` ON `j`.`TwoId` = `e0`.`Id`
) AS `s` ON `e`.`Id` = `s`.`ThreeId`
ORDER BY `e`.`Id`, `s`.`Id`
""");
    }

    public override async Task Filtered_include_skip_navigation_order_by_skip_split(bool async)
    {
        await base.Filtered_include_skip_navigation_order_by_skip_split(async);

        AssertSql(
"""
SELECT [e].[Id], [e].[CollectionInverseId], [e].[ExtraId], [e].[Name], [e].[ReferenceInverseId]
FROM [EntityTwos] AS [e]
ORDER BY [e].[Id]
""",
//
"""
SELECT [t0].[SelfSkipSharedLeftId], [t0].[SelfSkipSharedRightId], [t0].[Id], [t0].[CollectionInverseId], [t0].[ExtraId], [t0].[Name], [t0].[ReferenceInverseId], [e].[Id]
FROM [EntityTwos] AS [e]
INNER JOIN (
    SELECT [t].[SelfSkipSharedLeftId], [t].[SelfSkipSharedRightId], [t].[Id], [t].[CollectionInverseId], [t].[ExtraId], [t].[Name], [t].[ReferenceInverseId]
    FROM (
        SELECT [e0].[SelfSkipSharedLeftId], [e0].[SelfSkipSharedRightId], [e1].[Id], [e1].[CollectionInverseId], [e1].[ExtraId], [e1].[Name], [e1].[ReferenceInverseId], ROW_NUMBER() OVER(PARTITION BY [e0].[SelfSkipSharedLeftId] ORDER BY [e1].[Id]) AS [row]
        FROM [EntityTwoEntityTwo] AS [e0]
        INNER JOIN [EntityTwos] AS [e1] ON [e0].[SelfSkipSharedRightId] = [e1].[Id]
    ) AS [t]
    WHERE 2 < [t].[row]
) AS [t0] ON [e].[Id] = [t0].[SelfSkipSharedLeftId]
ORDER BY [e].[Id], [t0].[SelfSkipSharedLeftId], [t0].[Id]
""");
    }

    public override async Task Filtered_include_skip_navigation_order_by_take_split(bool async)
    {
        await base.Filtered_include_skip_navigation_order_by_take_split(async);

        AssertSql(
"""
SELECT [e].[Key1], [e].[Key2], [e].[Key3], [e].[Name]
FROM [EntityCompositeKeys] AS [e]
ORDER BY [e].[Key1], [e].[Key2], [e].[Key3]
""",
//
"""
SELECT [t0].[TwoSkipSharedId], [t0].[CompositeKeySkipSharedKey1], [t0].[CompositeKeySkipSharedKey2], [t0].[CompositeKeySkipSharedKey3], [t0].[Id], [t0].[CollectionInverseId], [t0].[ExtraId], [t0].[Name], [t0].[ReferenceInverseId], [e].[Key1], [e].[Key2], [e].[Key3]
FROM [EntityCompositeKeys] AS [e]
INNER JOIN (
    SELECT [t].[TwoSkipSharedId], [t].[CompositeKeySkipSharedKey1], [t].[CompositeKeySkipSharedKey2], [t].[CompositeKeySkipSharedKey3], [t].[Id], [t].[CollectionInverseId], [t].[ExtraId], [t].[Name], [t].[ReferenceInverseId]
    FROM (
        SELECT [e0].[TwoSkipSharedId], [e0].[CompositeKeySkipSharedKey1], [e0].[CompositeKeySkipSharedKey2], [e0].[CompositeKeySkipSharedKey3], [e1].[Id], [e1].[CollectionInverseId], [e1].[ExtraId], [e1].[Name], [e1].[ReferenceInverseId], ROW_NUMBER() OVER(PARTITION BY [e0].[CompositeKeySkipSharedKey1], [e0].[CompositeKeySkipSharedKey2], [e0].[CompositeKeySkipSharedKey3] ORDER BY [e1].[Id]) AS [row]
        FROM [EntityCompositeKeyEntityTwo] AS [e0]
        INNER JOIN [EntityTwos] AS [e1] ON [e0].[TwoSkipSharedId] = [e1].[Id]
    ) AS [t]
    WHERE [t].[row] <= 2
) AS [t0] ON [e].[Key1] = [t0].[CompositeKeySkipSharedKey1] AND [e].[Key2] = [t0].[CompositeKeySkipSharedKey2] AND [e].[Key3] = [t0].[CompositeKeySkipSharedKey3]
ORDER BY [e].[Key1], [e].[Key2], [e].[Key3], [t0].[CompositeKeySkipSharedKey1], [t0].[CompositeKeySkipSharedKey2], [t0].[CompositeKeySkipSharedKey3], [t0].[Id]
""");
    }

    public override async Task Filtered_include_skip_navigation_order_by_skip_take_split(bool async)
    {
        await base.Filtered_include_skip_navigation_order_by_skip_take_split(async);

        AssertSql(
"""
SELECT [e].[Key1], [e].[Key2], [e].[Key3], [e].[Name]
FROM [EntityCompositeKeys] AS [e]
ORDER BY [e].[Key1], [e].[Key2], [e].[Key3]
""",
//
"""
SELECT [t0].[Id], [t0].[CompositeId1], [t0].[CompositeId2], [t0].[CompositeId3], [t0].[ThreeId], [t0].[Id0], [t0].[CollectionInverseId], [t0].[Name], [t0].[ReferenceInverseId], [e].[Key1], [e].[Key2], [e].[Key3]
FROM [EntityCompositeKeys] AS [e]
INNER JOIN (
    SELECT [t].[Id], [t].[CompositeId1], [t].[CompositeId2], [t].[CompositeId3], [t].[ThreeId], [t].[Id0], [t].[CollectionInverseId], [t].[Name], [t].[ReferenceInverseId]
    FROM (
        SELECT [j].[Id], [j].[CompositeId1], [j].[CompositeId2], [j].[CompositeId3], [j].[ThreeId], [e0].[Id] AS [Id0], [e0].[CollectionInverseId], [e0].[Name], [e0].[ReferenceInverseId], ROW_NUMBER() OVER(PARTITION BY [j].[CompositeId1], [j].[CompositeId2], [j].[CompositeId3] ORDER BY [e0].[Id]) AS [row]
        FROM [JoinThreeToCompositeKeyFull] AS [j]
        INNER JOIN [EntityThrees] AS [e0] ON [j].[ThreeId] = [e0].[Id]
    ) AS [t]
    WHERE 1 < [t].[row] AND [t].[row] <= 3
) AS [t0] ON [e].[Key1] = [t0].[CompositeId1] AND [e].[Key2] = [t0].[CompositeId2] AND [e].[Key3] = [t0].[CompositeId3]
ORDER BY [e].[Key1], [e].[Key2], [e].[Key3], [t0].[CompositeId1], [t0].[CompositeId2], [t0].[CompositeId3], [t0].[Id0]
""");
    }

    public override async Task Filtered_then_include_skip_navigation_where_split(bool async)
    {
        await base.Filtered_then_include_skip_navigation_where_split(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`Name`, `u`.`Number`, `u`.`Slumber`, `u`.`IsGreen`, `u`.`IsBrown`, `u`.`Discriminator`
FROM (
    SELECT `r`.`Id`, `r`.`Name`, CVar(NULL) AS `Number`, CVar(NULL) AS `Slumber`, CVar(NULL) AS `IsGreen`, CVar(NULL) AS `IsBrown`, 'EntityRoot' AS `Discriminator`
    FROM `Roots` AS `r`
    UNION ALL
    SELECT `b`.`Id`, `b`.`Name`, `b`.`Number`, CVar(NULL) AS `Slumber`, CVar(NULL) AS `IsGreen`, CVar(NULL) AS `IsBrown`, 'EntityBranch' AS `Discriminator`
    FROM `Branches` AS `b`
    UNION ALL
    SELECT `l`.`Id`, `l`.`Name`, `l`.`Number`, CVar(NULL) AS `Slumber`, `l`.`IsGreen`, CVar(NULL) AS `IsBrown`, 'EntityLeaf' AS `Discriminator`
    FROM `Leaves` AS `l`
    UNION ALL
    SELECT `l0`.`Id`, `l0`.`Name`, CVar(NULL) AS `Number`, `l0`.`Slumber`, CVar(NULL) AS `IsGreen`, `l0`.`IsBrown`, 'EntityLeaf2' AS `Discriminator`
    FROM `Leaf2s` AS `l0`
) AS `u`
ORDER BY `u`.`Id`
""",
            //
            """
SELECT `s`.`RootSkipSharedId`, `s`.`ThreeSkipSharedId`, `s`.`Id`, `s`.`CollectionInverseId`, `s`.`Name`, `s`.`ReferenceInverseId`, `u`.`Id`
FROM (
    SELECT `r`.`Id`
    FROM `Roots` AS `r`
    UNION ALL
    SELECT `b`.`Id`
    FROM `Branches` AS `b`
    UNION ALL
    SELECT `l`.`Id`
    FROM `Leaves` AS `l`
    UNION ALL
    SELECT `l0`.`Id`
    FROM `Leaf2s` AS `l0`
) AS `u`
INNER JOIN (
    SELECT `e`.`RootSkipSharedId`, `e`.`ThreeSkipSharedId`, `e0`.`Id`, `e0`.`CollectionInverseId`, `e0`.`Name`, `e0`.`ReferenceInverseId`
    FROM `EntityRootEntityThree` AS `e`
    INNER JOIN `EntityThrees` AS `e0` ON `e`.`ThreeSkipSharedId` = `e0`.`Id`
) AS `s` ON `u`.`Id` = `s`.`RootSkipSharedId`
ORDER BY `u`.`Id`, `s`.`RootSkipSharedId`, `s`.`ThreeSkipSharedId`, `s`.`Id`
""",
            //
            """
SELECT `s0`.`OneId`, `s0`.`ThreeId`, `s0`.`Payload`, `s0`.`Id`, `s0`.`Name`, `u`.`Id`, `s`.`RootSkipSharedId`, `s`.`ThreeSkipSharedId`, `s`.`Id`
FROM ((
    SELECT `r`.`Id`
    FROM `Roots` AS `r`
    UNION ALL
    SELECT `b`.`Id`
    FROM `Branches` AS `b`
    UNION ALL
    SELECT `l`.`Id`
    FROM `Leaves` AS `l`
    UNION ALL
    SELECT `l0`.`Id`
    FROM `Leaf2s` AS `l0`
) AS `u`
INNER JOIN (
    SELECT `e`.`RootSkipSharedId`, `e`.`ThreeSkipSharedId`, `e0`.`Id`
    FROM `EntityRootEntityThree` AS `e`
    INNER JOIN `EntityThrees` AS `e0` ON `e`.`ThreeSkipSharedId` = `e0`.`Id`
) AS `s` ON `u`.`Id` = `s`.`RootSkipSharedId`)
LEFT JOIN (
    SELECT `j`.`OneId`, `j`.`ThreeId`, `j`.`Payload`, `e1`.`Id`, `e1`.`Name`
    FROM `JoinOneToThreePayloadFullShared` AS `j`
    INNER JOIN `EntityOnes` AS `e1` ON `j`.`OneId` = `e1`.`Id`
    WHERE `e1`.`Id` < 10
) AS `s0` ON `s`.`Id` = `s0`.`ThreeId`
WHERE `s`.`Id` IS NOT NULL AND `s0`.`ThreeId` IS NOT NULL
ORDER BY `u`.`Id`, `s`.`RootSkipSharedId`, `s`.`ThreeSkipSharedId`, `s`.`Id`
""");
    }

    public override async Task Filtered_then_include_skip_navigation_order_by_skip_take_split(bool async)
    {
        await base.Filtered_then_include_skip_navigation_order_by_skip_take_split(async);

        AssertSql(
"""
SELECT [t].[Id], [t].[Name], [t].[Number], [t].[Slumber], [t].[IsGreen], [t].[IsBrown], [t].[Discriminator]
FROM (
    SELECT [r].[Id], [r].[Name], NULL AS [Number], NULL AS [Slumber], NULL AS [IsGreen], NULL AS [IsBrown], N'EntityRoot' AS [Discriminator]
    FROM [Roots] AS [r]
    UNION ALL
    SELECT [b].[Id], [b].[Name], [b].[Number], NULL AS [Slumber], NULL AS [IsGreen], NULL AS [IsBrown], N'EntityBranch' AS [Discriminator]
    FROM [Branches] AS [b]
    UNION ALL
    SELECT [l].[Id], [l].[Name], [l].[Number], NULL AS [Slumber], [l].[IsGreen], NULL AS [IsBrown], N'EntityLeaf' AS [Discriminator]
    FROM [Leaves] AS [l]
    UNION ALL
    SELECT [l0].[Id], [l0].[Name], NULL AS [Number], [l0].[Slumber], NULL AS [IsGreen], [l0].[IsBrown], N'EntityLeaf2' AS [Discriminator]
    FROM [Leaf2s] AS [l0]
) AS [t]
ORDER BY [t].[Id]
""",
//
"""
SELECT [t0].[RootSkipSharedId], [t0].[CompositeKeySkipSharedKey1], [t0].[CompositeKeySkipSharedKey2], [t0].[CompositeKeySkipSharedKey3], [t0].[Key1], [t0].[Key2], [t0].[Key3], [t0].[Name], [t].[Id]
FROM (
    SELECT [r].[Id]
    FROM [Roots] AS [r]
    UNION ALL
    SELECT [b].[Id]
    FROM [Branches] AS [b]
    UNION ALL
    SELECT [l].[Id]
    FROM [Leaves] AS [l]
    UNION ALL
    SELECT [l0].[Id]
    FROM [Leaf2s] AS [l0]
) AS [t]
INNER JOIN (
    SELECT [e].[RootSkipSharedId], [e].[CompositeKeySkipSharedKey1], [e].[CompositeKeySkipSharedKey2], [e].[CompositeKeySkipSharedKey3], [e0].[Key1], [e0].[Key2], [e0].[Key3], [e0].[Name]
    FROM [EntityCompositeKeyEntityRoot] AS [e]
    INNER JOIN [EntityCompositeKeys] AS [e0] ON [e].[CompositeKeySkipSharedKey1] = [e0].[Key1] AND [e].[CompositeKeySkipSharedKey2] = [e0].[Key2] AND [e].[CompositeKeySkipSharedKey3] = [e0].[Key3]
) AS [t0] ON [t].[Id] = [t0].[RootSkipSharedId]
ORDER BY [t].[Id], [t0].[RootSkipSharedId], [t0].[CompositeKeySkipSharedKey1], [t0].[CompositeKeySkipSharedKey2], [t0].[CompositeKeySkipSharedKey3], [t0].[Key1], [t0].[Key2], [t0].[Key3]
""",
//
"""
SELECT [t1].[Id], [t1].[CompositeId1], [t1].[CompositeId2], [t1].[CompositeId3], [t1].[ThreeId], [t1].[Id0], [t1].[CollectionInverseId], [t1].[Name], [t1].[ReferenceInverseId], [t].[Id], [t0].[RootSkipSharedId], [t0].[CompositeKeySkipSharedKey1], [t0].[CompositeKeySkipSharedKey2], [t0].[CompositeKeySkipSharedKey3], [t0].[Key1], [t0].[Key2], [t0].[Key3]
FROM (
    SELECT [r].[Id]
    FROM [Roots] AS [r]
    UNION ALL
    SELECT [b].[Id]
    FROM [Branches] AS [b]
    UNION ALL
    SELECT [l].[Id]
    FROM [Leaves] AS [l]
    UNION ALL
    SELECT [l0].[Id]
    FROM [Leaf2s] AS [l0]
) AS [t]
INNER JOIN (
    SELECT [e].[RootSkipSharedId], [e].[CompositeKeySkipSharedKey1], [e].[CompositeKeySkipSharedKey2], [e].[CompositeKeySkipSharedKey3], [e0].[Key1], [e0].[Key2], [e0].[Key3]
    FROM [EntityCompositeKeyEntityRoot] AS [e]
    INNER JOIN [EntityCompositeKeys] AS [e0] ON [e].[CompositeKeySkipSharedKey1] = [e0].[Key1] AND [e].[CompositeKeySkipSharedKey2] = [e0].[Key2] AND [e].[CompositeKeySkipSharedKey3] = [e0].[Key3]
) AS [t0] ON [t].[Id] = [t0].[RootSkipSharedId]
INNER JOIN (
    SELECT [t2].[Id], [t2].[CompositeId1], [t2].[CompositeId2], [t2].[CompositeId3], [t2].[ThreeId], [t2].[Id0], [t2].[CollectionInverseId], [t2].[Name], [t2].[ReferenceInverseId]
    FROM (
        SELECT [j].[Id], [j].[CompositeId1], [j].[CompositeId2], [j].[CompositeId3], [j].[ThreeId], [e1].[Id] AS [Id0], [e1].[CollectionInverseId], [e1].[Name], [e1].[ReferenceInverseId], ROW_NUMBER() OVER(PARTITION BY [j].[CompositeId1], [j].[CompositeId2], [j].[CompositeId3] ORDER BY [e1].[Id]) AS [row]
        FROM [JoinThreeToCompositeKeyFull] AS [j]
        INNER JOIN [EntityThrees] AS [e1] ON [j].[ThreeId] = [e1].[Id]
    ) AS [t2]
    WHERE 1 < [t2].[row] AND [t2].[row] <= 3
) AS [t1] ON [t0].[Key1] = [t1].[CompositeId1] AND [t0].[Key2] = [t1].[CompositeId2] AND [t0].[Key3] = [t1].[CompositeId3]
ORDER BY [t].[Id], [t0].[RootSkipSharedId], [t0].[CompositeKeySkipSharedKey1], [t0].[CompositeKeySkipSharedKey2], [t0].[CompositeKeySkipSharedKey3], [t0].[Key1], [t0].[Key2], [t0].[Key3], [t1].[CompositeId1], [t1].[CompositeId2], [t1].[CompositeId3], [t1].[Id0]
""");
    }

    public override async Task Filtered_include_skip_navigation_where_then_include_skip_navigation_split(bool async)
    {
        await base.Filtered_include_skip_navigation_where_then_include_skip_navigation_split(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Name`, `l`.`Number`, `l`.`IsGreen`
FROM `Leaves` AS `l`
ORDER BY `l`.`Id`
""",
            //
            """
SELECT `s`.`LeafId`, `s`.`CompositeId1`, `s`.`CompositeId2`, `s`.`CompositeId3`, `s`.`Key1`, `s`.`Key2`, `s`.`Key3`, `s`.`Name`, `l`.`Id`
FROM `Leaves` AS `l`
INNER JOIN (
    SELECT `j`.`LeafId`, `j`.`CompositeId1`, `j`.`CompositeId2`, `j`.`CompositeId3`, `e`.`Key1`, `e`.`Key2`, `e`.`Key3`, `e`.`Name`
    FROM `JoinCompositeKeyToLeaf` AS `j`
    INNER JOIN `EntityCompositeKeys` AS `e` ON `j`.`CompositeId1` = `e`.`Key1` AND `j`.`CompositeId2` = `e`.`Key2` AND `j`.`CompositeId3` = `e`.`Key3`
    WHERE `e`.`Key1` < 5
) AS `s` ON `l`.`Id` = `s`.`LeafId`
ORDER BY `l`.`Id`, `s`.`LeafId`, `s`.`CompositeId1`, `s`.`CompositeId2`, `s`.`CompositeId3`, `s`.`Key1`, `s`.`Key2`, `s`.`Key3`
""",
            //
            """
SELECT `s0`.`TwoSkipSharedId`, `s0`.`CompositeKeySkipSharedKey1`, `s0`.`CompositeKeySkipSharedKey2`, `s0`.`CompositeKeySkipSharedKey3`, `s0`.`Id`, `s0`.`CollectionInverseId`, `s0`.`ExtraId`, `s0`.`Name`, `s0`.`ReferenceInverseId`, `l`.`Id`, `s`.`LeafId`, `s`.`CompositeId1`, `s`.`CompositeId2`, `s`.`CompositeId3`, `s`.`Key1`, `s`.`Key2`, `s`.`Key3`
FROM (`Leaves` AS `l`
INNER JOIN (
    SELECT `j`.`LeafId`, `j`.`CompositeId1`, `j`.`CompositeId2`, `j`.`CompositeId3`, `e`.`Key1`, `e`.`Key2`, `e`.`Key3`
    FROM `JoinCompositeKeyToLeaf` AS `j`
    INNER JOIN `EntityCompositeKeys` AS `e` ON `j`.`CompositeId1` = `e`.`Key1` AND `j`.`CompositeId2` = `e`.`Key2` AND `j`.`CompositeId3` = `e`.`Key3`
    WHERE `e`.`Key1` < 5
) AS `s` ON `l`.`Id` = `s`.`LeafId`)
LEFT JOIN (
    SELECT `e0`.`TwoSkipSharedId`, `e0`.`CompositeKeySkipSharedKey1`, `e0`.`CompositeKeySkipSharedKey2`, `e0`.`CompositeKeySkipSharedKey3`, `e1`.`Id`, `e1`.`CollectionInverseId`, `e1`.`ExtraId`, `e1`.`Name`, `e1`.`ReferenceInverseId`
    FROM `EntityCompositeKeyEntityTwo` AS `e0`
    INNER JOIN `EntityTwos` AS `e1` ON `e0`.`TwoSkipSharedId` = `e1`.`Id`
) AS `s0` ON `s`.`Key1` = `s0`.`CompositeKeySkipSharedKey1` AND `s`.`Key2` = `s0`.`CompositeKeySkipSharedKey2` AND `s`.`Key3` = `s0`.`CompositeKeySkipSharedKey3`
WHERE `s`.`Key1` IS NOT NULL AND `s0`.`CompositeKeySkipSharedKey1` IS NOT NULL AND `s`.`Key2` IS NOT NULL AND `s0`.`CompositeKeySkipSharedKey2` IS NOT NULL AND `s`.`Key3` IS NOT NULL AND `s0`.`CompositeKeySkipSharedKey3` IS NOT NULL
ORDER BY `l`.`Id`, `s`.`LeafId`, `s`.`CompositeId1`, `s`.`CompositeId2`, `s`.`CompositeId3`, `s`.`Key1`, `s`.`Key2`, `s`.`Key3`
""");
    }

    public override async Task Filtered_include_skip_navigation_order_by_skip_take_then_include_skip_navigation_where_split(bool async)
    {
        await base.Filtered_include_skip_navigation_order_by_skip_take_then_include_skip_navigation_where_split(async);

        AssertSql(
"""
SELECT [e].[Id], [e].[Name]
FROM [EntityOnes] AS [e]
ORDER BY [e].[Id]
""",
//
"""
SELECT [t0].[OneId], [t0].[TwoId], [t0].[JoinOneToTwoExtraId], [t0].[Id], [t0].[CollectionInverseId], [t0].[ExtraId], [t0].[Name], [t0].[ReferenceInverseId], [e].[Id]
FROM [EntityOnes] AS [e]
INNER JOIN (
    SELECT [t].[OneId], [t].[TwoId], [t].[JoinOneToTwoExtraId], [t].[Id], [t].[CollectionInverseId], [t].[ExtraId], [t].[Name], [t].[ReferenceInverseId]
    FROM (
        SELECT [j].[OneId], [j].[TwoId], [j].[JoinOneToTwoExtraId], [e0].[Id], [e0].[CollectionInverseId], [e0].[ExtraId], [e0].[Name], [e0].[ReferenceInverseId], ROW_NUMBER() OVER(PARTITION BY [j].[OneId] ORDER BY [e0].[Id]) AS [row]
        FROM [JoinOneToTwo] AS [j]
        INNER JOIN [EntityTwos] AS [e0] ON [j].[TwoId] = [e0].[Id]
    ) AS [t]
    WHERE 1 < [t].[row] AND [t].[row] <= 3
) AS [t0] ON [e].[Id] = [t0].[OneId]
ORDER BY [e].[Id], [t0].[OneId], [t0].[Id], [t0].[TwoId]
""",
//
"""
SELECT [t1].[ThreeId], [t1].[TwoId], [t1].[Id], [t1].[CollectionInverseId], [t1].[Name], [t1].[ReferenceInverseId], [e].[Id], [t0].[OneId], [t0].[TwoId], [t0].[Id]
FROM [EntityOnes] AS [e]
INNER JOIN (
    SELECT [t].[OneId], [t].[TwoId], [t].[Id]
    FROM (
        SELECT [j].[OneId], [j].[TwoId], [e0].[Id], ROW_NUMBER() OVER(PARTITION BY [j].[OneId] ORDER BY [e0].[Id]) AS [row]
        FROM [JoinOneToTwo] AS [j]
        INNER JOIN [EntityTwos] AS [e0] ON [j].[TwoId] = [e0].[Id]
    ) AS [t]
    WHERE 1 < [t].[row] AND [t].[row] <= 3
) AS [t0] ON [e].[Id] = [t0].[OneId]
INNER JOIN (
    SELECT [j0].[ThreeId], [j0].[TwoId], [e1].[Id], [e1].[CollectionInverseId], [e1].[Name], [e1].[ReferenceInverseId]
    FROM [JoinTwoToThree] AS [j0]
    INNER JOIN [EntityThrees] AS [e1] ON [j0].[ThreeId] = [e1].[Id]
    WHERE [e1].[Id] < 10
) AS [t1] ON [t0].[Id] = [t1].[TwoId]
ORDER BY [e].[Id], [t0].[OneId], [t0].[Id], [t0].[TwoId]
""");
    }

    public override async Task Filtered_include_skip_navigation_where_then_include_skip_navigation_order_by_skip_take_split(bool async)
    {
        await base.Filtered_include_skip_navigation_where_then_include_skip_navigation_order_by_skip_take_split(async);

        AssertSql(
"""
SELECT [e].[Id], [e].[Name]
FROM [EntityOnes] AS [e]
ORDER BY [e].[Id]
""",
//
"""
SELECT [t].[OneId], [t].[TwoId], [t].[JoinOneToTwoExtraId], [t].[Id], [t].[CollectionInverseId], [t].[ExtraId], [t].[Name], [t].[ReferenceInverseId], [e].[Id]
FROM [EntityOnes] AS [e]
INNER JOIN (
    SELECT [j].[OneId], [j].[TwoId], [j].[JoinOneToTwoExtraId], [e0].[Id], [e0].[CollectionInverseId], [e0].[ExtraId], [e0].[Name], [e0].[ReferenceInverseId]
    FROM [JoinOneToTwo] AS [j]
    INNER JOIN [EntityTwos] AS [e0] ON [j].[TwoId] = [e0].[Id]
    WHERE [e0].[Id] < 10
) AS [t] ON [e].[Id] = [t].[OneId]
ORDER BY [e].[Id], [t].[OneId], [t].[TwoId], [t].[Id]
""",
//
"""
SELECT [t0].[ThreeId], [t0].[TwoId], [t0].[Id], [t0].[CollectionInverseId], [t0].[Name], [t0].[ReferenceInverseId], [e].[Id], [t].[OneId], [t].[TwoId], [t].[Id]
FROM [EntityOnes] AS [e]
INNER JOIN (
    SELECT [j].[OneId], [j].[TwoId], [e0].[Id]
    FROM [JoinOneToTwo] AS [j]
    INNER JOIN [EntityTwos] AS [e0] ON [j].[TwoId] = [e0].[Id]
    WHERE [e0].[Id] < 10
) AS [t] ON [e].[Id] = [t].[OneId]
INNER JOIN (
    SELECT [t1].[ThreeId], [t1].[TwoId], [t1].[Id], [t1].[CollectionInverseId], [t1].[Name], [t1].[ReferenceInverseId]
    FROM (
        SELECT [j0].[ThreeId], [j0].[TwoId], [e1].[Id], [e1].[CollectionInverseId], [e1].[Name], [e1].[ReferenceInverseId], ROW_NUMBER() OVER(PARTITION BY [j0].[TwoId] ORDER BY [e1].[Id]) AS [row]
        FROM [JoinTwoToThree] AS [j0]
        INNER JOIN [EntityThrees] AS [e1] ON [j0].[ThreeId] = [e1].[Id]
    ) AS [t1]
    WHERE 1 < [t1].[row] AND [t1].[row] <= 3
) AS [t0] ON [t].[Id] = [t0].[TwoId]
ORDER BY [e].[Id], [t].[OneId], [t].[TwoId], [t].[Id], [t0].[TwoId], [t0].[Id]
""");
    }

    public override async Task Filter_include_on_skip_navigation_combined_split(bool async)
    {
        await base.Filter_include_on_skip_navigation_combined_split(async);

        AssertSql(
            """
SELECT `e`.`Id`, `e`.`CollectionInverseId`, `e`.`ExtraId`, `e`.`Name`, `e`.`ReferenceInverseId`
FROM `EntityTwos` AS `e`
ORDER BY `e`.`Id`
""",
            //
            """
SELECT `s`.`OneId`, `s`.`TwoId`, `s`.`JoinOneToTwoExtraId`, `s`.`Id`, `s`.`Name`, `s`.`Id0`, `s`.`CollectionInverseId`, `s`.`ExtraId`, `s`.`Name0`, `s`.`ReferenceInverseId`, `e`.`Id`
FROM `EntityTwos` AS `e`
INNER JOIN (
    SELECT `j`.`OneId`, `j`.`TwoId`, `j`.`JoinOneToTwoExtraId`, `e0`.`Id`, `e0`.`Name`, `e1`.`Id` AS `Id0`, `e1`.`CollectionInverseId`, `e1`.`ExtraId`, `e1`.`Name` AS `Name0`, `e1`.`ReferenceInverseId`
    FROM (`JoinOneToTwo` AS `j`
    INNER JOIN `EntityOnes` AS `e0` ON `j`.`OneId` = `e0`.`Id`)
    LEFT JOIN `EntityTwos` AS `e1` ON `e0`.`Id` = `e1`.`ReferenceInverseId`
    WHERE `e0`.`Id` < 10
) AS `s` ON `e`.`Id` = `s`.`TwoId`
ORDER BY `e`.`Id`, `s`.`OneId`, `s`.`TwoId`, `s`.`Id`, `s`.`Id0`
""",
            //
            """
SELECT `e2`.`Id`, `e2`.`CollectionInverseId`, `e2`.`ExtraId`, `e2`.`Name`, `e2`.`ReferenceInverseId`, `e`.`Id`, `s`.`OneId`, `s`.`TwoId`, `s`.`Id`, `s`.`Id0`
FROM (`EntityTwos` AS `e`
INNER JOIN (
    SELECT `j`.`OneId`, `j`.`TwoId`, `e0`.`Id`, `e1`.`Id` AS `Id0`
    FROM (`JoinOneToTwo` AS `j`
    INNER JOIN `EntityOnes` AS `e0` ON `j`.`OneId` = `e0`.`Id`)
    LEFT JOIN `EntityTwos` AS `e1` ON `e0`.`Id` = `e1`.`ReferenceInverseId`
    WHERE `e0`.`Id` < 10
) AS `s` ON `e`.`Id` = `s`.`TwoId`)
LEFT JOIN `EntityTwos` AS `e2` ON `s`.`Id` = `e2`.`CollectionInverseId`
WHERE `s`.`Id` IS NOT NULL AND `e2`.`CollectionInverseId` IS NOT NULL
ORDER BY `e`.`Id`, `s`.`OneId`, `s`.`TwoId`, `s`.`Id`, `s`.`Id0`
""");
    }

    public override async Task Filter_include_on_skip_navigation_combined_with_filtered_then_includes_split(bool async)
    {
        await base.Filter_include_on_skip_navigation_combined_with_filtered_then_includes_split(async);

        AssertSql(
"""
SELECT [e].[Id], [e].[CollectionInverseId], [e].[Name], [e].[ReferenceInverseId]
FROM [EntityThrees] AS [e]
ORDER BY [e].[Id]
""",
//
"""
SELECT [t].[OneId], [t].[ThreeId], [t].[Payload], [t].[Id], [t].[Name], [e].[Id]
FROM [EntityThrees] AS [e]
INNER JOIN (
    SELECT [j].[OneId], [j].[ThreeId], [j].[Payload], [e0].[Id], [e0].[Name]
    FROM [JoinOneToThreePayloadFull] AS [j]
    INNER JOIN [EntityOnes] AS [e0] ON [j].[OneId] = [e0].[Id]
    WHERE [e0].[Id] < 10
) AS [t] ON [e].[Id] = [t].[ThreeId]
ORDER BY [e].[Id], [t].[OneId], [t].[ThreeId], [t].[Id]
""",
//
"""
SELECT [t0].[OneId], [t0].[TwoId], [t0].[JoinOneToTwoExtraId], [t0].[Id], [t0].[CollectionInverseId], [t0].[ExtraId], [t0].[Name], [t0].[ReferenceInverseId], [e].[Id], [t].[OneId], [t].[ThreeId], [t].[Id]
FROM [EntityThrees] AS [e]
INNER JOIN (
    SELECT [j].[OneId], [j].[ThreeId], [e0].[Id]
    FROM [JoinOneToThreePayloadFull] AS [j]
    INNER JOIN [EntityOnes] AS [e0] ON [j].[OneId] = [e0].[Id]
    WHERE [e0].[Id] < 10
) AS [t] ON [e].[Id] = [t].[ThreeId]
INNER JOIN (
    SELECT [t1].[OneId], [t1].[TwoId], [t1].[JoinOneToTwoExtraId], [t1].[Id], [t1].[CollectionInverseId], [t1].[ExtraId], [t1].[Name], [t1].[ReferenceInverseId]
    FROM (
        SELECT [j0].[OneId], [j0].[TwoId], [j0].[JoinOneToTwoExtraId], [e1].[Id], [e1].[CollectionInverseId], [e1].[ExtraId], [e1].[Name], [e1].[ReferenceInverseId], ROW_NUMBER() OVER(PARTITION BY [j0].[OneId] ORDER BY [e1].[Id]) AS [row]
        FROM [JoinOneToTwo] AS [j0]
        INNER JOIN [EntityTwos] AS [e1] ON [j0].[TwoId] = [e1].[Id]
    ) AS [t1]
    WHERE 1 < [t1].[row] AND [t1].[row] <= 3
) AS [t0] ON [t].[Id] = [t0].[OneId]
ORDER BY [e].[Id], [t].[OneId], [t].[ThreeId], [t].[Id], [t0].[OneId], [t0].[Id]
""",
//
"""
SELECT [t0].[EntityBranchId], [t0].[EntityOneId], [t0].[Id], [t0].[Name], [t0].[Number], [t0].[IsGreen], [t0].[Discriminator], [e].[Id], [t].[OneId], [t].[ThreeId], [t].[Id]
FROM [EntityThrees] AS [e]
INNER JOIN (
    SELECT [j].[OneId], [j].[ThreeId], [e0].[Id]
    FROM [JoinOneToThreePayloadFull] AS [j]
    INNER JOIN [EntityOnes] AS [e0] ON [j].[OneId] = [e0].[Id]
    WHERE [e0].[Id] < 10
) AS [t] ON [e].[Id] = [t].[ThreeId]
INNER JOIN (
    SELECT [j0].[EntityBranchId], [j0].[EntityOneId], [t1].[Id], [t1].[Name], [t1].[Number], [t1].[IsGreen], [t1].[Discriminator]
    FROM [JoinOneToBranch] AS [j0]
    INNER JOIN (
        SELECT [b].[Id], [b].[Name], [b].[Number], NULL AS [IsGreen], N'EntityBranch' AS [Discriminator]
        FROM [Branches] AS [b]
        UNION ALL
        SELECT [l].[Id], [l].[Name], [l].[Number], [l].[IsGreen], N'EntityLeaf' AS [Discriminator]
        FROM [Leaves] AS [l]
    ) AS [t1] ON [j0].[EntityBranchId] = [t1].[Id]
    WHERE [t1].[Id] < 20
) AS [t0] ON [t].[Id] = [t0].[EntityOneId]
ORDER BY [e].[Id], [t].[OneId], [t].[ThreeId], [t].[Id]
""");
    }

    public override async Task Filtered_include_on_skip_navigation_then_filtered_include_on_navigation_split(bool async)
    {
        await base.Filtered_include_on_skip_navigation_then_filtered_include_on_navigation_split(async);

        AssertSql(
            """
SELECT `e`.`Id`, `e`.`CollectionInverseId`, `e`.`Name`, `e`.`ReferenceInverseId`
FROM `EntityThrees` AS `e`
ORDER BY `e`.`Id`
""",
            //
            """
SELECT `s`.`OneId`, `s`.`ThreeId`, `s`.`Payload`, `s`.`Id`, `s`.`Name`, `e`.`Id`
FROM `EntityThrees` AS `e`
INNER JOIN (
    SELECT `j`.`OneId`, `j`.`ThreeId`, `j`.`Payload`, `e0`.`Id`, `e0`.`Name`
    FROM `JoinOneToThreePayloadFull` AS `j`
    INNER JOIN `EntityOnes` AS `e0` ON `j`.`OneId` = `e0`.`Id`
    WHERE `e0`.`Id` > 15
) AS `s` ON `e`.`Id` = `s`.`ThreeId`
ORDER BY `e`.`Id`, `s`.`OneId`, `s`.`ThreeId`, `s`.`Id`
""",
            //
            """
SELECT `e2`.`Id`, `e2`.`CollectionInverseId`, `e2`.`ExtraId`, `e2`.`Name`, `e2`.`ReferenceInverseId`, `e`.`Id`, `s`.`OneId`, `s`.`ThreeId`, `s`.`Id`
FROM (`EntityThrees` AS `e`
INNER JOIN (
    SELECT `j`.`OneId`, `j`.`ThreeId`, `e0`.`Id`
    FROM `JoinOneToThreePayloadFull` AS `j`
    INNER JOIN `EntityOnes` AS `e0` ON `j`.`OneId` = `e0`.`Id`
    WHERE `e0`.`Id` > 15
) AS `s` ON `e`.`Id` = `s`.`ThreeId`)
LEFT JOIN (
    SELECT `e1`.`Id`, `e1`.`CollectionInverseId`, `e1`.`ExtraId`, `e1`.`Name`, `e1`.`ReferenceInverseId`
    FROM `EntityTwos` AS `e1`
    WHERE `e1`.`Id` < 5
) AS `e2` ON `s`.`Id` = `e2`.`CollectionInverseId`
WHERE `s`.`Id` IS NOT NULL AND `e2`.`CollectionInverseId` IS NOT NULL
ORDER BY `e`.`Id`, `s`.`OneId`, `s`.`ThreeId`, `s`.`Id`
""");
    }

    public override async Task Filtered_include_on_navigation_then_filtered_include_on_skip_navigation_split(bool async)
    {
        await base.Filtered_include_on_navigation_then_filtered_include_on_skip_navigation_split(async);

        AssertSql(
            """
SELECT `e`.`Id`, `e`.`Name`
FROM `EntityOnes` AS `e`
ORDER BY `e`.`Id`
""",
            //
            """
SELECT `e2`.`Id`, `e2`.`CollectionInverseId`, `e2`.`ExtraId`, `e2`.`Name`, `e2`.`ReferenceInverseId`, `e`.`Id`
FROM `EntityOnes` AS `e`
INNER JOIN (
    SELECT `e0`.`Id`, `e0`.`CollectionInverseId`, `e0`.`ExtraId`, `e0`.`Name`, `e0`.`ReferenceInverseId`
    FROM `EntityTwos` AS `e0`
    WHERE `e0`.`Id` > 15
) AS `e2` ON `e`.`Id` = `e2`.`CollectionInverseId`
ORDER BY `e`.`Id`, `e2`.`Id`
""",
            //
            """
SELECT `s`.`ThreeId`, `s`.`TwoId`, `s`.`Id`, `s`.`CollectionInverseId`, `s`.`Name`, `s`.`ReferenceInverseId`, `e`.`Id`, `e2`.`Id`
FROM (`EntityOnes` AS `e`
INNER JOIN (
    SELECT `e0`.`Id`, `e0`.`CollectionInverseId`
    FROM `EntityTwos` AS `e0`
    WHERE `e0`.`Id` > 15
) AS `e2` ON `e`.`Id` = `e2`.`CollectionInverseId`)
LEFT JOIN (
    SELECT `j`.`ThreeId`, `j`.`TwoId`, `e1`.`Id`, `e1`.`CollectionInverseId`, `e1`.`Name`, `e1`.`ReferenceInverseId`
    FROM `JoinTwoToThree` AS `j`
    INNER JOIN `EntityThrees` AS `e1` ON `j`.`ThreeId` = `e1`.`Id`
    WHERE `e1`.`Id` < 5
) AS `s` ON `e2`.`Id` = `s`.`TwoId`
WHERE `e2`.`Id` IS NOT NULL AND `s`.`TwoId` IS NOT NULL
ORDER BY `e`.`Id`, `e2`.`Id`
""");
    }

    public override async Task Include_skip_navigation_then_include_inverse_works_for_tracking_query(bool async)
    {
        await base.Include_skip_navigation_then_include_inverse_works_for_tracking_query(async);

        AssertSql(
            """
SELECT `e`.`Id`, `e`.`CollectionInverseId`, `e`.`Name`, `e`.`ReferenceInverseId`, `s0`.`OneId`, `s0`.`ThreeId`, `s0`.`Payload`, `s0`.`Id`, `s0`.`Name`, `s0`.`OneId0`, `s0`.`ThreeId0`, `s0`.`Payload0`, `s0`.`Id0`, `s0`.`CollectionInverseId`, `s0`.`Name0`, `s0`.`ReferenceInverseId`
FROM `EntityThrees` AS `e`
LEFT JOIN (
    SELECT `j`.`OneId`, `j`.`ThreeId`, `j`.`Payload`, `e0`.`Id`, `e0`.`Name`, `s`.`OneId` AS `OneId0`, `s`.`ThreeId` AS `ThreeId0`, `s`.`Payload` AS `Payload0`, `s`.`Id` AS `Id0`, `s`.`CollectionInverseId`, `s`.`Name` AS `Name0`, `s`.`ReferenceInverseId`
    FROM (`JoinOneToThreePayloadFullShared` AS `j`
    INNER JOIN `EntityOnes` AS `e0` ON `j`.`OneId` = `e0`.`Id`)
    LEFT JOIN (
        SELECT `j0`.`OneId`, `j0`.`ThreeId`, `j0`.`Payload`, `e1`.`Id`, `e1`.`CollectionInverseId`, `e1`.`Name`, `e1`.`ReferenceInverseId`
        FROM `JoinOneToThreePayloadFullShared` AS `j0`
        INNER JOIN `EntityThrees` AS `e1` ON `j0`.`ThreeId` = `e1`.`Id`
    ) AS `s` ON `e0`.`Id` = `s`.`OneId`
) AS `s0` ON `e`.`Id` = `s0`.`ThreeId`
ORDER BY `e`.`Id`, `s0`.`OneId`, `s0`.`ThreeId`, `s0`.`Id`, `s0`.`OneId0`, `s0`.`ThreeId0`
""");
    }

    public override async Task Throws_when_different_filtered_include(bool async)
    {
        await base.Throws_when_different_filtered_include(async);

        AssertSql();
    }

    public override async Task Throws_when_different_filtered_then_include(bool async)
    {
        await base.Throws_when_different_filtered_then_include(async);

        AssertSql();
    }

    public override async Task Throws_when_different_filtered_then_include_via_different_paths(bool async)
    {
        await base.Throws_when_different_filtered_then_include_via_different_paths(async);

        AssertSql();
    }

    public override async Task Select_many_over_skip_navigation_where_non_equality(bool async)
    {
        await base.Select_many_over_skip_navigation_where_non_equality(async);

        AssertSql(
            """
SELECT `s`.`Id`, `s`.`CollectionInverseId`, `s`.`ExtraId`, `s`.`Name`, `s`.`ReferenceInverseId`
FROM `EntityOnes` AS `e`
LEFT JOIN (
    SELECT `e0`.`Id`, `e0`.`CollectionInverseId`, `e0`.`ExtraId`, `e0`.`Name`, `e0`.`ReferenceInverseId`, `j`.`OneId`
    FROM `JoinOneToTwo` AS `j`
    INNER JOIN `EntityTwos` AS `e0` ON `j`.`TwoId` = `e0`.`Id`
) AS `s` ON `e`.`Id` = `s`.`OneId` AND `e`.`Id` <> `s`.`Id`
""");
    }

    public override async Task Contains_on_skip_collection_navigation(bool async)
    {
        await base.Contains_on_skip_collection_navigation(async);

        AssertSql(
            """
@entity_equality_two_Id='1' (Nullable = true)

SELECT `e`.`Id`, `e`.`Name`
FROM `EntityOnes` AS `e`
WHERE EXISTS (
    SELECT 1
    FROM `JoinOneToTwo` AS `j`
    INNER JOIN `EntityTwos` AS `e0` ON `j`.`TwoId` = `e0`.`Id`
    WHERE `e`.`Id` = `j`.`OneId` AND `e0`.`Id` = @entity_equality_two_Id)
""");
    }

    public override async Task GetType_in_hierarchy_in_base_type(bool async)
    {
        await base.GetType_in_hierarchy_in_base_type(async);

        AssertSql(
            """
SELECT `r`.`Id`, `r`.`Name`, CVar(NULL) AS `Number`, CVar(NULL) AS `Slumber`, CVar(NULL) AS `IsGreen`, CVar(NULL) AS `IsBrown`, 'EntityRoot' AS `Discriminator`
FROM `Roots` AS `r`
""");
    }

    public override async Task GetType_in_hierarchy_in_intermediate_type(bool async)
    {
        await base.GetType_in_hierarchy_in_intermediate_type(async);

        AssertSql(
            """
SELECT `b`.`Id`, `b`.`Name`, `b`.`Number`, CVar(NULL) AS `Slumber`, CVar(NULL) AS `IsGreen`, CVar(NULL) AS `IsBrown`, 'EntityBranch' AS `Discriminator`
FROM `Branches` AS `b`
""");
    }

    public override async Task GetType_in_hierarchy_in_leaf_type(bool async)
    {
        await base.GetType_in_hierarchy_in_leaf_type(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Name`, `l`.`Number`, CVar(NULL) AS `Slumber`, `l`.`IsGreen`, CVar(NULL) AS `IsBrown`, 'EntityLeaf' AS `Discriminator`
FROM `Leaves` AS `l`
""");
    }

    public override async Task GetType_in_hierarchy_in_querying_base_type(bool async)
    {
        await base.GetType_in_hierarchy_in_querying_base_type(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`Name`, `u`.`Number`, `u`.`IsGreen`, `u`.`Discriminator`
FROM (
    SELECT `b`.`Id`, `b`.`Name`, `b`.`Number`, CVar(NULL) AS `IsGreen`, 'EntityBranch' AS `Discriminator`
    FROM `Branches` AS `b`
    UNION ALL
    SELECT `l`.`Id`, `l`.`Name`, `l`.`Number`, `l`.`IsGreen`, 'EntityLeaf' AS `Discriminator`
    FROM `Leaves` AS `l`
) AS `u`
WHERE 0 = 1
""");
    }

    public override async Task Filtered_include_skip_navigation_order_by_take_EF_Property(bool async)
    {
        await base.Filtered_include_skip_navigation_order_by_take_EF_Property(async);

        AssertSql(
"""
SELECT [e].[Key1], [e].[Key2], [e].[Key3], [e].[Name], [t0].[TwoSkipSharedId], [t0].[CompositeKeySkipSharedKey1], [t0].[CompositeKeySkipSharedKey2], [t0].[CompositeKeySkipSharedKey3], [t0].[Id], [t0].[CollectionInverseId], [t0].[ExtraId], [t0].[Name], [t0].[ReferenceInverseId]
FROM [EntityCompositeKeys] AS [e]
LEFT JOIN (
    SELECT [t].[TwoSkipSharedId], [t].[CompositeKeySkipSharedKey1], [t].[CompositeKeySkipSharedKey2], [t].[CompositeKeySkipSharedKey3], [t].[Id], [t].[CollectionInverseId], [t].[ExtraId], [t].[Name], [t].[ReferenceInverseId]
    FROM (
        SELECT [e0].[TwoSkipSharedId], [e0].[CompositeKeySkipSharedKey1], [e0].[CompositeKeySkipSharedKey2], [e0].[CompositeKeySkipSharedKey3], [e1].[Id], [e1].[CollectionInverseId], [e1].[ExtraId], [e1].[Name], [e1].[ReferenceInverseId], ROW_NUMBER() OVER(PARTITION BY [e0].[CompositeKeySkipSharedKey1], [e0].[CompositeKeySkipSharedKey2], [e0].[CompositeKeySkipSharedKey3] ORDER BY [e1].[Id]) AS [row]
        FROM [EntityCompositeKeyEntityTwo] AS [e0]
        INNER JOIN [EntityTwos] AS [e1] ON [e0].[TwoSkipSharedId] = [e1].[Id]
    ) AS [t]
    WHERE [t].[row] <= 2
) AS [t0] ON [e].[Key1] = [t0].[CompositeKeySkipSharedKey1] AND [e].[Key2] = [t0].[CompositeKeySkipSharedKey2] AND [e].[Key3] = [t0].[CompositeKeySkipSharedKey3]
ORDER BY [e].[Key1], [e].[Key2], [e].[Key3], [t0].[CompositeKeySkipSharedKey1], [t0].[CompositeKeySkipSharedKey2], [t0].[CompositeKeySkipSharedKey3], [t0].[Id]
""");
    }

    public override async Task Filtered_include_skip_navigation_order_by_skip_take_then_include_skip_navigation_where_EF_Property(
        bool async)
    {
        await base.Filtered_include_skip_navigation_order_by_skip_take_then_include_skip_navigation_where_EF_Property(async);

        AssertSql(
"""
SELECT [e].[Id], [e].[Name], [t1].[OneId], [t1].[TwoId], [t1].[JoinOneToTwoExtraId], [t1].[Id], [t1].[CollectionInverseId], [t1].[ExtraId], [t1].[Name], [t1].[ReferenceInverseId], [t1].[ThreeId], [t1].[TwoId0], [t1].[Id0], [t1].[CollectionInverseId0], [t1].[Name0], [t1].[ReferenceInverseId0]
FROM [EntityOnes] AS [e]
OUTER APPLY (
    SELECT [t].[OneId], [t].[TwoId], [t].[JoinOneToTwoExtraId], [t].[Id], [t].[CollectionInverseId], [t].[ExtraId], [t].[Name], [t].[ReferenceInverseId], [t0].[ThreeId], [t0].[TwoId] AS [TwoId0], [t0].[Id] AS [Id0], [t0].[CollectionInverseId] AS [CollectionInverseId0], [t0].[Name] AS [Name0], [t0].[ReferenceInverseId] AS [ReferenceInverseId0]
    FROM (
        SELECT [j].[OneId], [j].[TwoId], [j].[JoinOneToTwoExtraId], [e0].[Id], [e0].[CollectionInverseId], [e0].[ExtraId], [e0].[Name], [e0].[ReferenceInverseId]
        FROM [JoinOneToTwo] AS [j]
        INNER JOIN [EntityTwos] AS [e0] ON [j].[TwoId] = [e0].[Id]
        WHERE [e].[Id] = [j].[OneId]
        ORDER BY [e0].[Id]
        OFFSET 1 ROWS FETCH NEXT 2 ROWS ONLY
    ) AS [t]
    LEFT JOIN (
        SELECT [j0].[ThreeId], [j0].[TwoId], [e1].[Id], [e1].[CollectionInverseId], [e1].[Name], [e1].[ReferenceInverseId]
        FROM [JoinTwoToThree] AS [j0]
        INNER JOIN [EntityThrees] AS [e1] ON [j0].[ThreeId] = [e1].[Id]
        WHERE [e1].[Id] < 10
    ) AS [t0] ON [t].[Id] = [t0].[TwoId]
) AS [t1]
ORDER BY [e].[Id], [t1].[Id], [t1].[OneId], [t1].[TwoId], [t1].[ThreeId], [t1].[TwoId0]
""");
    }

    public override async Task Skip_navigation_all_unidirectional(bool async)
    {
        await base.Skip_navigation_all_unidirectional(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`Name`
FROM `UnidirectionalEntityOnes` AS `u`
WHERE NOT EXISTS (
    SELECT `u0`.`OneId`
    FROM `UnidirectionalJoinOneToTwo` AS `u0`
    INNER JOIN `UnidirectionalEntityTwos` AS `u1` ON `u0`.`TwoId` = `u1`.`Id`
    WHERE `u`.`Id` = `u0`.`OneId` AND (`u1`.`Name` NOT LIKE '%B%' OR `u1`.`Name` IS NULL))
""");
    }

    public override async Task Skip_navigation_any_with_predicate_unidirectional(bool async)
    {
        await base.Skip_navigation_any_with_predicate_unidirectional(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`Name`
FROM `UnidirectionalEntityOnes` AS `u`
WHERE EXISTS (
    SELECT 1
    FROM `UnidirectionalEntityOneUnidirectionalEntityTwo` AS `u0`
    INNER JOIN `UnidirectionalEntityTwos` AS `u1` ON `u0`.`TwoSkipSharedId` = `u1`.`Id`
    WHERE `u`.`Id` = `u0`.`UnidirectionalEntityOneId` AND (`u1`.`Name` LIKE '%B%'))
""");
    }

    public override async Task Skip_navigation_contains_unidirectional(bool async)
    {
        await base.Skip_navigation_contains_unidirectional(async);

        AssertSql(
"""
SELECT `u`.`Id`, `u`.`Name`
FROM `UnidirectionalEntityOnes` AS `u`
WHERE EXISTS (
    SELECT 1
    FROM `UnidirectionalJoinOneToThreePayloadFullShared` AS `u0`
    INNER JOIN `UnidirectionalEntityThrees` AS `u1` ON `u0`.`ThreeId` = `u1`.`Id`
    WHERE `u`.`Id` = `u0`.`OneId` AND `u1`.`Id` = 1)
""");
    }

    public override async Task Skip_navigation_count_without_predicate_unidirectional(bool async)
    {
        await base.Skip_navigation_count_without_predicate_unidirectional(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`Name`
FROM `UnidirectionalEntityOnes` AS `u`
WHERE EXISTS (
    SELECT 1
    FROM `UnidirectionalJoinOneSelfPayload` AS `u0`
    INNER JOIN `UnidirectionalEntityOnes` AS `u1` ON `u0`.`LeftId` = `u1`.`Id`
    WHERE `u`.`Id` = `u0`.`RightId`)
""");
    }

    public override async Task Skip_navigation_count_with_predicate_unidirectional(bool async)
    {
        await base.Skip_navigation_count_with_predicate_unidirectional(async);

        AssertSql(
            """
SELECT `u4`.`Id`, `u4`.`Name`, `u4`.`c`
FROM (
    SELECT `u`.`Id`, `u`.`Name`, (
        SELECT COUNT(*)
        FROM `UnidirectionalJoinOneToBranch` AS `u0`
        INNER JOIN (
            SELECT `u1`.`Id`, `u1`.`Name`
            FROM `UnidirectionalBranches` AS `u1`
            UNION ALL
            SELECT `u2`.`Id`, `u2`.`Name`
            FROM `UnidirectionalLeaves` AS `u2`
        ) AS `u3` ON `u0`.`UnidirectionalEntityBranchId` = `u3`.`Id`
        WHERE `u`.`Id` = `u0`.`UnidirectionalEntityOneId` AND (`u3`.`Name` LIKE 'L%')) AS `c`
    FROM `UnidirectionalEntityOnes` AS `u`
) AS `u4`
ORDER BY `u4`.`c`, `u4`.`Id`
""");
    }

    public override async Task Skip_navigation_select_subquery_average_unidirectional(bool async)
    {
        await base.Skip_navigation_select_subquery_average_unidirectional(async);

        AssertSql(
"""
SELECT (
    SELECT AVG(CDBL(`u1`.`Key1`))
    FROM `UnidirectionalJoinCompositeKeyToLeaf` AS `u0`
    INNER JOIN `UnidirectionalEntityCompositeKeys` AS `u1` ON `u0`.`CompositeId1` = `u1`.`Key1` AND `u0`.`CompositeId2` = `u1`.`Key2` AND `u0`.`CompositeId3` = `u1`.`Key3`
    WHERE `u`.`Id` = `u0`.`LeafId`)
FROM `UnidirectionalLeaves` AS `u`
""");
    }

    public override async Task Skip_navigation_order_by_reverse_first_or_default_unidirectional(bool async)
    {
        await base.Skip_navigation_order_by_reverse_first_or_default_unidirectional(async);

        AssertSql(
"""
SELECT [t0].[Id], [t0].[CollectionInverseId], [t0].[ExtraId], [t0].[Name], [t0].[ReferenceInverseId]
FROM [UnidirectionalEntityThrees] AS [u]
LEFT JOIN (
    SELECT [t].[Id], [t].[CollectionInverseId], [t].[ExtraId], [t].[Name], [t].[ReferenceInverseId], [t].[ThreeId]
    FROM (
        SELECT [u1].[Id], [u1].[CollectionInverseId], [u1].[ExtraId], [u1].[Name], [u1].[ReferenceInverseId], [u0].[ThreeId], ROW_NUMBER() OVER(PARTITION BY [u0].[ThreeId] ORDER BY [u1].[Id] DESC) AS [row]
        FROM [UnidirectionalJoinTwoToThree] AS [u0]
        INNER JOIN [UnidirectionalEntityTwos] AS [u1] ON [u0].[TwoId] = [u1].[Id]
    ) AS [t]
    WHERE [t].[row] <= 1
) AS [t0] ON [u].[Id] = [t0].[ThreeId]
""");
    }

    public override async Task Skip_navigation_of_type_unidirectional(bool async)
    {
        await base.Skip_navigation_of_type_unidirectional(async);

        AssertSql(
            """
SELECT `u`.`Key1`, `u`.`Key2`, `u`.`Key3`, `s`.`Id`, `s`.`Name`, `s`.`Number`, `s`.`IsGreen`, `s`.`Discriminator`, `s`.`RootSkipSharedId`, `s`.`UnidirectionalEntityCompositeKeyKey1`, `s`.`UnidirectionalEntityCompositeKeyKey2`, `s`.`UnidirectionalEntityCompositeKeyKey3`
FROM `UnidirectionalEntityCompositeKeys` AS `u`
LEFT JOIN (
    SELECT `u4`.`Id`, `u4`.`Name`, `u4`.`Number`, `u4`.`IsGreen`, `u4`.`Discriminator`, `u0`.`RootSkipSharedId`, `u0`.`UnidirectionalEntityCompositeKeyKey1`, `u0`.`UnidirectionalEntityCompositeKeyKey2`, `u0`.`UnidirectionalEntityCompositeKeyKey3`
    FROM `UnidirectionalEntityCompositeKeyUnidirectionalEntityRoot` AS `u0`
    INNER JOIN (
        SELECT `u1`.`Id`, `u1`.`Name`, CVar(NULL) AS `Number`, CVar(NULL) AS `IsGreen`, 'UnidirectionalEntityRoot' AS `Discriminator`
        FROM `UnidirectionalRoots` AS `u1`
        UNION ALL
        SELECT `u2`.`Id`, `u2`.`Name`, `u2`.`Number`, CVar(NULL) AS `IsGreen`, 'UnidirectionalEntityBranch' AS `Discriminator`
        FROM `UnidirectionalBranches` AS `u2`
        UNION ALL
        SELECT `u3`.`Id`, `u3`.`Name`, `u3`.`Number`, `u3`.`IsGreen`, 'UnidirectionalEntityLeaf' AS `Discriminator`
        FROM `UnidirectionalLeaves` AS `u3`
    ) AS `u4` ON `u0`.`RootSkipSharedId` = `u4`.`Id`
    WHERE `u4`.`Discriminator` = 'UnidirectionalEntityLeaf'
) AS `s` ON `u`.`Key1` = `s`.`UnidirectionalEntityCompositeKeyKey1` AND `u`.`Key2` = `s`.`UnidirectionalEntityCompositeKeyKey2` AND `u`.`Key3` = `s`.`UnidirectionalEntityCompositeKeyKey3`
ORDER BY `u`.`Key1`, `u`.`Key2`, `u`.`Key3`, `s`.`RootSkipSharedId`, `s`.`UnidirectionalEntityCompositeKeyKey1`, `s`.`UnidirectionalEntityCompositeKeyKey2`, `s`.`UnidirectionalEntityCompositeKeyKey3`
""");
    }

    public override async Task Join_with_skip_navigation_unidirectional(bool async)
    {
        await base.Join_with_skip_navigation_unidirectional(async);

        AssertSql(
"""
SELECT [u].[Id], [u].[CollectionInverseId], [u].[ExtraId], [u].[Name], [u].[ReferenceInverseId], [u0].[Id], [u0].[CollectionInverseId], [u0].[ExtraId], [u0].[Name], [u0].[ReferenceInverseId]
FROM [UnidirectionalEntityTwos] AS [u]
INNER JOIN [UnidirectionalEntityTwos] AS [u0] ON [u].[Id] = (
    SELECT TOP(1) [u2].[Id]
    FROM [UnidirectionalEntityTwoUnidirectionalEntityTwo] AS [u1]
    INNER JOIN [UnidirectionalEntityTwos] AS [u2] ON [u1].[SelfSkipSharedRightId] = [u2].[Id]
    WHERE [u0].[Id] = [u1].[UnidirectionalEntityTwoId]
    ORDER BY [u2].[Id])
""");
    }

    public override async Task Left_join_with_skip_navigation_unidirectional(bool async)
    {
        await base.Left_join_with_skip_navigation_unidirectional(async);

        AssertSql(
"""
SELECT [u].[Key1], [u].[Key2], [u].[Key3], [u].[Name], [u0].[Key1], [u0].[Key2], [u0].[Key3], [u0].[Name]
FROM [UnidirectionalEntityCompositeKeys] AS [u]
LEFT JOIN [UnidirectionalEntityCompositeKeys] AS [u0] ON (
    SELECT TOP(1) [u2].[Id]
    FROM [UnidirectionalEntityCompositeKeyUnidirectionalEntityTwo] AS [u1]
    INNER JOIN [UnidirectionalEntityTwos] AS [u2] ON [u1].[TwoSkipSharedId] = [u2].[Id]
    WHERE [u].[Key1] = [u1].[UnidirectionalEntityCompositeKeyKey1] AND [u].[Key2] = [u1].[UnidirectionalEntityCompositeKeyKey2] AND [u].[Key3] = [u1].[UnidirectionalEntityCompositeKeyKey3]
    ORDER BY [u2].[Id]) = (
    SELECT TOP(1) [u4].[Id]
    FROM [UnidirectionalJoinThreeToCompositeKeyFull] AS [u3]
    INNER JOIN [UnidirectionalEntityThrees] AS [u4] ON [u3].[ThreeId] = [u4].[Id]
    WHERE [u0].[Key1] = [u3].[CompositeId1] AND [u0].[Key2] = [u3].[CompositeId2] AND [u0].[Key3] = [u3].[CompositeId3]
    ORDER BY [u4].[Id])
ORDER BY [u].[Key1], [u0].[Key1], [u].[Key2], [u0].[Key2]
""");
    }

    public override async Task Select_many_over_skip_navigation_unidirectional(bool async)
    {
        await base.Select_many_over_skip_navigation_unidirectional(async);

        AssertSql(
            """
SELECT `s`.`Id`, `s`.`CollectionInverseId`, `s`.`Name`, `s`.`ReferenceInverseId`
FROM (
    SELECT `u`.`Id`
    FROM `UnidirectionalRoots` AS `u`
    UNION ALL
    SELECT `u0`.`Id`
    FROM `UnidirectionalBranches` AS `u0`
    UNION ALL
    SELECT `u1`.`Id`
    FROM `UnidirectionalLeaves` AS `u1`
) AS `u2`
INNER JOIN (
    SELECT `u4`.`Id`, `u4`.`CollectionInverseId`, `u4`.`Name`, `u4`.`ReferenceInverseId`, `u3`.`UnidirectionalEntityRootId`
    FROM `UnidirectionalEntityRootUnidirectionalEntityThree` AS `u3`
    INNER JOIN `UnidirectionalEntityThrees` AS `u4` ON `u3`.`ThreeSkipSharedId` = `u4`.`Id`
) AS `s` ON `u2`.`Id` = `s`.`UnidirectionalEntityRootId`
""");
    }

    public override async Task Select_many_over_skip_navigation_where_unidirectional(bool async)
    {
        await base.Select_many_over_skip_navigation_where_unidirectional(async);

        AssertSql(
            """
SELECT `s`.`Id`, `s`.`CollectionInverseId`, `s`.`ExtraId`, `s`.`Name`, `s`.`ReferenceInverseId`
FROM `UnidirectionalEntityOnes` AS `u`
LEFT JOIN (
    SELECT `u1`.`Id`, `u1`.`CollectionInverseId`, `u1`.`ExtraId`, `u1`.`Name`, `u1`.`ReferenceInverseId`, `u0`.`OneId`
    FROM `UnidirectionalJoinOneToTwo` AS `u0`
    INNER JOIN `UnidirectionalEntityTwos` AS `u1` ON `u0`.`TwoId` = `u1`.`Id`
) AS `s` ON `u`.`Id` = `s`.`OneId`
""");
    }

    public override async Task Select_many_over_skip_navigation_order_by_take_unidirectional(bool async)
    {
        await base.Select_many_over_skip_navigation_order_by_take_unidirectional(async);

        AssertSql(
"""
SELECT [t0].[Id], [t0].[CollectionInverseId], [t0].[ExtraId], [t0].[Name], [t0].[ReferenceInverseId]
FROM [UnidirectionalEntityOnes] AS [u]
INNER JOIN (
    SELECT [t].[Id], [t].[CollectionInverseId], [t].[ExtraId], [t].[Name], [t].[ReferenceInverseId], [t].[UnidirectionalEntityOneId]
    FROM (
        SELECT [u1].[Id], [u1].[CollectionInverseId], [u1].[ExtraId], [u1].[Name], [u1].[ReferenceInverseId], [u0].[UnidirectionalEntityOneId], ROW_NUMBER() OVER(PARTITION BY [u0].[UnidirectionalEntityOneId] ORDER BY [u1].[Id]) AS [row]
        FROM [UnidirectionalEntityOneUnidirectionalEntityTwo] AS [u0]
        INNER JOIN [UnidirectionalEntityTwos] AS [u1] ON [u0].[TwoSkipSharedId] = [u1].[Id]
    ) AS [t]
    WHERE [t].[row] <= 2
) AS [t0] ON [u].[Id] = [t0].[UnidirectionalEntityOneId]
""");
    }

    public override async Task Select_many_over_skip_navigation_order_by_skip_take_unidirectional(bool async)
    {
        await base.Select_many_over_skip_navigation_order_by_skip_take_unidirectional(async);

        AssertSql(
"""
SELECT [t0].[Id], [t0].[CollectionInverseId], [t0].[Name], [t0].[ReferenceInverseId]
FROM [UnidirectionalEntityOnes] AS [u]
INNER JOIN (
    SELECT [t].[Id], [t].[CollectionInverseId], [t].[Name], [t].[ReferenceInverseId], [t].[OneId]
    FROM (
        SELECT [u1].[Id], [u1].[CollectionInverseId], [u1].[Name], [u1].[ReferenceInverseId], [u0].[OneId], ROW_NUMBER() OVER(PARTITION BY [u0].[OneId] ORDER BY [u1].[Id]) AS [row]
        FROM [UnidirectionalJoinOneToThreePayloadFullShared] AS [u0]
        INNER JOIN [UnidirectionalEntityThrees] AS [u1] ON [u0].[ThreeId] = [u1].[Id]
    ) AS [t]
    WHERE 2 < [t].[row] AND [t].[row] <= 5
) AS [t0] ON [u].[Id] = [t0].[OneId]
""");
    }

    public override async Task Select_many_over_skip_navigation_cast_unidirectional(bool async)
    {
        await base.Select_many_over_skip_navigation_cast_unidirectional(async);

        AssertSql(
            """
SELECT `s`.`Id`, `s`.`Name`, `s`.`Number`, `s`.`IsGreen`, `s`.`Discriminator`
FROM `UnidirectionalEntityOnes` AS `u`
INNER JOIN (
    SELECT `u3`.`Id`, `u3`.`Name`, `u3`.`Number`, `u3`.`IsGreen`, `u3`.`Discriminator`, `u0`.`UnidirectionalEntityOneId`
    FROM `UnidirectionalJoinOneToBranch` AS `u0`
    INNER JOIN (
        SELECT `u1`.`Id`, `u1`.`Name`, `u1`.`Number`, CVar(NULL) AS `IsGreen`, 'UnidirectionalEntityBranch' AS `Discriminator`
        FROM `UnidirectionalBranches` AS `u1`
        UNION ALL
        SELECT `u2`.`Id`, `u2`.`Name`, `u2`.`Number`, `u2`.`IsGreen`, 'UnidirectionalEntityLeaf' AS `Discriminator`
        FROM `UnidirectionalLeaves` AS `u2`
    ) AS `u3` ON `u0`.`UnidirectionalEntityBranchId` = `u3`.`Id`
) AS `s` ON `u`.`Id` = `s`.`UnidirectionalEntityOneId`
""");
    }

    public override async Task Select_skip_navigation_unidirectional(bool async)
    {
        await base.Select_skip_navigation_unidirectional(async);
        AssertSql(
            """
SELECT `u`.`Id`, `s`.`Id`, `s`.`Name`, `s`.`LeftId`, `s`.`RightId`
FROM `UnidirectionalEntityOnes` AS `u`
LEFT JOIN (
    SELECT `u1`.`Id`, `u1`.`Name`, `u0`.`LeftId`, `u0`.`RightId`
    FROM `UnidirectionalJoinOneSelfPayload` AS `u0`
    INNER JOIN `UnidirectionalEntityOnes` AS `u1` ON `u0`.`LeftId` = `u1`.`Id`
) AS `s` ON `u`.`Id` = `s`.`RightId`
ORDER BY `u`.`Id`, `s`.`LeftId`, `s`.`RightId`
""");
    }

    public override async Task Include_skip_navigation_unidirectional(bool async)
    {
        await base.Include_skip_navigation_unidirectional(async);

        AssertSql(
            """
SELECT `u`.`Key1`, `u`.`Key2`, `u`.`Key3`, `u`.`Name`, `s`.`RootSkipSharedId`, `s`.`UnidirectionalEntityCompositeKeyKey1`, `s`.`UnidirectionalEntityCompositeKeyKey2`, `s`.`UnidirectionalEntityCompositeKeyKey3`, `s`.`Id`, `s`.`Name`, `s`.`Number`, `s`.`IsGreen`, `s`.`Discriminator`
FROM `UnidirectionalEntityCompositeKeys` AS `u`
LEFT JOIN (
    SELECT `u0`.`RootSkipSharedId`, `u0`.`UnidirectionalEntityCompositeKeyKey1`, `u0`.`UnidirectionalEntityCompositeKeyKey2`, `u0`.`UnidirectionalEntityCompositeKeyKey3`, `u4`.`Id`, `u4`.`Name`, `u4`.`Number`, `u4`.`IsGreen`, `u4`.`Discriminator`
    FROM `UnidirectionalEntityCompositeKeyUnidirectionalEntityRoot` AS `u0`
    INNER JOIN (
        SELECT `u1`.`Id`, `u1`.`Name`, CVar(NULL) AS `Number`, CVar(NULL) AS `IsGreen`, 'UnidirectionalEntityRoot' AS `Discriminator`
        FROM `UnidirectionalRoots` AS `u1`
        UNION ALL
        SELECT `u2`.`Id`, `u2`.`Name`, `u2`.`Number`, CVar(NULL) AS `IsGreen`, 'UnidirectionalEntityBranch' AS `Discriminator`
        FROM `UnidirectionalBranches` AS `u2`
        UNION ALL
        SELECT `u3`.`Id`, `u3`.`Name`, `u3`.`Number`, `u3`.`IsGreen`, 'UnidirectionalEntityLeaf' AS `Discriminator`
        FROM `UnidirectionalLeaves` AS `u3`
    ) AS `u4` ON `u0`.`RootSkipSharedId` = `u4`.`Id`
) AS `s` ON `u`.`Key1` = `s`.`UnidirectionalEntityCompositeKeyKey1` AND `u`.`Key2` = `s`.`UnidirectionalEntityCompositeKeyKey2` AND `u`.`Key3` = `s`.`UnidirectionalEntityCompositeKeyKey3`
ORDER BY `u`.`Key1`, `u`.`Key2`, `u`.`Key3`, `s`.`RootSkipSharedId`, `s`.`UnidirectionalEntityCompositeKeyKey1`, `s`.`UnidirectionalEntityCompositeKeyKey2`, `s`.`UnidirectionalEntityCompositeKeyKey3`
""");
    }

    public override async Task Include_skip_navigation_then_reference_unidirectional(bool async)
    {
        await base.Include_skip_navigation_then_reference_unidirectional(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`CollectionInverseId`, `u`.`ExtraId`, `u`.`Name`, `u`.`ReferenceInverseId`, `s`.`OneId`, `s`.`TwoId`, `s`.`UnidirectionalJoinOneToTwoExtraId`, `s`.`Id`, `s`.`Name`, `s`.`Id0`, `s`.`CollectionInverseId`, `s`.`ExtraId`, `s`.`Name0`, `s`.`ReferenceInverseId`
FROM `UnidirectionalEntityTwos` AS `u`
LEFT JOIN (
    SELECT `u0`.`OneId`, `u0`.`TwoId`, `u0`.`UnidirectionalJoinOneToTwoExtraId`, `u1`.`Id`, `u1`.`Name`, `u2`.`Id` AS `Id0`, `u2`.`CollectionInverseId`, `u2`.`ExtraId`, `u2`.`Name` AS `Name0`, `u2`.`ReferenceInverseId`
    FROM (`UnidirectionalJoinOneToTwo` AS `u0`
    INNER JOIN `UnidirectionalEntityOnes` AS `u1` ON `u0`.`OneId` = `u1`.`Id`)
    LEFT JOIN `UnidirectionalEntityTwos` AS `u2` ON `u1`.`Id` = `u2`.`ReferenceInverseId`
) AS `s` ON `u`.`Id` = `s`.`TwoId`
ORDER BY `u`.`Id`, `s`.`OneId`, `s`.`TwoId`, `s`.`Id`
""");
    }

    public override async Task Include_skip_navigation_then_include_skip_navigation_unidirectional(bool async)
    {
        await base.Include_skip_navigation_then_include_skip_navigation_unidirectional(async);

        AssertSql(
            """
SELECT `u`.`Key1`, `u`.`Key2`, `u`.`Key3`, `u`.`Name`, `s0`.`LeafId`, `s0`.`CompositeId1`, `s0`.`CompositeId2`, `s0`.`CompositeId3`, `s0`.`Id`, `s0`.`Name`, `s0`.`Number`, `s0`.`IsGreen`, `s0`.`UnidirectionalEntityBranchId`, `s0`.`UnidirectionalEntityOneId`, `s0`.`Id0`, `s0`.`Name0`
FROM `UnidirectionalEntityCompositeKeys` AS `u`
LEFT JOIN (
    SELECT `u0`.`LeafId`, `u0`.`CompositeId1`, `u0`.`CompositeId2`, `u0`.`CompositeId3`, `u1`.`Id`, `u1`.`Name`, `u1`.`Number`, `u1`.`IsGreen`, `s`.`UnidirectionalEntityBranchId`, `s`.`UnidirectionalEntityOneId`, `s`.`Id` AS `Id0`, `s`.`Name` AS `Name0`
    FROM (`UnidirectionalJoinCompositeKeyToLeaf` AS `u0`
    INNER JOIN `UnidirectionalLeaves` AS `u1` ON `u0`.`LeafId` = `u1`.`Id`)
    LEFT JOIN (
        SELECT `u2`.`UnidirectionalEntityBranchId`, `u2`.`UnidirectionalEntityOneId`, `u3`.`Id`, `u3`.`Name`
        FROM `UnidirectionalJoinOneToBranch` AS `u2`
        INNER JOIN `UnidirectionalEntityOnes` AS `u3` ON `u2`.`UnidirectionalEntityOneId` = `u3`.`Id`
    ) AS `s` ON `u1`.`Id` = `s`.`UnidirectionalEntityBranchId`
) AS `s0` ON `u`.`Key1` = `s0`.`CompositeId1` AND `u`.`Key2` = `s0`.`CompositeId2` AND `u`.`Key3` = `s0`.`CompositeId3`
ORDER BY `u`.`Key1`, `u`.`Key2`, `u`.`Key3`, `s0`.`LeafId`, `s0`.`CompositeId1`, `s0`.`CompositeId2`, `s0`.`CompositeId3`, `s0`.`Id`, `s0`.`UnidirectionalEntityBranchId`, `s0`.`UnidirectionalEntityOneId`
""");
    }

    public override async Task Include_skip_navigation_then_include_reference_and_skip_navigation_unidirectional(bool async)
    {
        await base.Include_skip_navigation_then_include_reference_and_skip_navigation_unidirectional(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`CollectionInverseId`, `u`.`Name`, `u`.`ReferenceInverseId`, `s0`.`OneId`, `s0`.`ThreeId`, `s0`.`Payload`, `s0`.`Id`, `s0`.`Name`, `s0`.`Id0`, `s0`.`CollectionInverseId`, `s0`.`ExtraId`, `s0`.`Name0`, `s0`.`ReferenceInverseId`, `s0`.`LeftId`, `s0`.`RightId`, `s0`.`Payload0`, `s0`.`Id1`, `s0`.`Name1`
FROM `UnidirectionalEntityThrees` AS `u`
LEFT JOIN (
    SELECT `u0`.`OneId`, `u0`.`ThreeId`, `u0`.`Payload`, `u1`.`Id`, `u1`.`Name`, `u2`.`Id` AS `Id0`, `u2`.`CollectionInverseId`, `u2`.`ExtraId`, `u2`.`Name` AS `Name0`, `u2`.`ReferenceInverseId`, `s`.`LeftId`, `s`.`RightId`, `s`.`Payload` AS `Payload0`, `s`.`Id` AS `Id1`, `s`.`Name` AS `Name1`
    FROM ((`UnidirectionalJoinOneToThreePayloadFull` AS `u0`
    INNER JOIN `UnidirectionalEntityOnes` AS `u1` ON `u0`.`OneId` = `u1`.`Id`)
    LEFT JOIN `UnidirectionalEntityTwos` AS `u2` ON `u1`.`Id` = `u2`.`ReferenceInverseId`)
    LEFT JOIN (
        SELECT `u3`.`LeftId`, `u3`.`RightId`, `u3`.`Payload`, `u4`.`Id`, `u4`.`Name`
        FROM `UnidirectionalJoinOneSelfPayload` AS `u3`
        INNER JOIN `UnidirectionalEntityOnes` AS `u4` ON `u3`.`RightId` = `u4`.`Id`
    ) AS `s` ON `u1`.`Id` = `s`.`LeftId`
) AS `s0` ON `u`.`Id` = `s0`.`ThreeId`
ORDER BY `u`.`Id`, `s0`.`OneId`, `s0`.`ThreeId`, `s0`.`Id`, `s0`.`Id0`, `s0`.`LeftId`, `s0`.`RightId`
""");
    }

    public override async Task Include_skip_navigation_and_reference_unidirectional(bool async)
    {
        await base.Include_skip_navigation_and_reference_unidirectional(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`CollectionInverseId`, `u`.`ExtraId`, `u`.`Name`, `u`.`ReferenceInverseId`, `u0`.`Id`, `s`.`TwoSkipSharedId`, `s`.`UnidirectionalEntityOneId`, `s`.`Id`, `s`.`Name`, `u0`.`CollectionInverseId`, `u0`.`Name`, `u0`.`ReferenceInverseId`
FROM (`UnidirectionalEntityTwos` AS `u`
LEFT JOIN `UnidirectionalEntityThrees` AS `u0` ON `u`.`Id` = `u0`.`ReferenceInverseId`)
LEFT JOIN (
    SELECT `u1`.`TwoSkipSharedId`, `u1`.`UnidirectionalEntityOneId`, `u2`.`Id`, `u2`.`Name`
    FROM `UnidirectionalEntityOneUnidirectionalEntityTwo` AS `u1`
    INNER JOIN `UnidirectionalEntityOnes` AS `u2` ON `u1`.`UnidirectionalEntityOneId` = `u2`.`Id`
) AS `s` ON `u`.`Id` = `s`.`TwoSkipSharedId`
ORDER BY `u`.`Id`, `u0`.`Id`, `s`.`TwoSkipSharedId`, `s`.`UnidirectionalEntityOneId`
""");
    }

    public override async Task Include_skip_navigation_then_include_inverse_works_for_tracking_query_unidirectional(bool async)
    {
        await base.Include_skip_navigation_then_include_inverse_works_for_tracking_query_unidirectional(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`CollectionInverseId`, `u`.`Name`, `u`.`ReferenceInverseId`, `s0`.`OneId`, `s0`.`ThreeId`, `s0`.`Payload`, `s0`.`Id`, `s0`.`Name`, `s0`.`OneId0`, `s0`.`ThreeId0`, `s0`.`Payload0`, `s0`.`Id0`, `s0`.`CollectionInverseId`, `s0`.`Name0`, `s0`.`ReferenceInverseId`
FROM `UnidirectionalEntityThrees` AS `u`
LEFT JOIN (
    SELECT `u0`.`OneId`, `u0`.`ThreeId`, `u0`.`Payload`, `u1`.`Id`, `u1`.`Name`, `s`.`OneId` AS `OneId0`, `s`.`ThreeId` AS `ThreeId0`, `s`.`Payload` AS `Payload0`, `s`.`Id` AS `Id0`, `s`.`CollectionInverseId`, `s`.`Name` AS `Name0`, `s`.`ReferenceInverseId`
    FROM (`UnidirectionalJoinOneToThreePayloadFullShared` AS `u0`
    INNER JOIN `UnidirectionalEntityOnes` AS `u1` ON `u0`.`OneId` = `u1`.`Id`)
    LEFT JOIN (
        SELECT `u2`.`OneId`, `u2`.`ThreeId`, `u2`.`Payload`, `u3`.`Id`, `u3`.`CollectionInverseId`, `u3`.`Name`, `u3`.`ReferenceInverseId`
        FROM `UnidirectionalJoinOneToThreePayloadFullShared` AS `u2`
        INNER JOIN `UnidirectionalEntityThrees` AS `u3` ON `u2`.`ThreeId` = `u3`.`Id`
    ) AS `s` ON `u1`.`Id` = `s`.`OneId`
) AS `s0` ON `u`.`Id` = `s0`.`ThreeId`
ORDER BY `u`.`Id`, `s0`.`OneId`, `s0`.`ThreeId`, `s0`.`Id`, `s0`.`OneId0`, `s0`.`ThreeId0`
""");
    }

    public override async Task Filtered_include_skip_navigation_where_unidirectional(bool async)
    {
        await base.Filtered_include_skip_navigation_where_unidirectional(async);

        AssertSql(
            """
SELECT `e`.`Id`, `e`.`CollectionInverseId`, `e`.`Name`, `e`.`ReferenceInverseId`, `s`.`OneId`, `s`.`ThreeId`, `s`.`Payload`, `s`.`Id`, `s`.`Name`
FROM `EntityThrees` AS `e`
LEFT JOIN (
    SELECT `j`.`OneId`, `j`.`ThreeId`, `j`.`Payload`, `e0`.`Id`, `e0`.`Name`
    FROM `JoinOneToThreePayloadFullShared` AS `j`
    INNER JOIN `EntityOnes` AS `e0` ON `j`.`OneId` = `e0`.`Id`
    WHERE `e0`.`Id` < 10
) AS `s` ON `e`.`Id` = `s`.`ThreeId`
ORDER BY `e`.`Id`, `s`.`OneId`, `s`.`ThreeId`
""");
    }

    public override async Task Filtered_include_skip_navigation_order_by_unidirectional(bool async)
    {
        await base.Filtered_include_skip_navigation_order_by_unidirectional(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`CollectionInverseId`, `u`.`Name`, `u`.`ReferenceInverseId`, `s`.`ThreeId`, `s`.`TwoId`, `s`.`Id`, `s`.`CollectionInverseId`, `s`.`ExtraId`, `s`.`Name`, `s`.`ReferenceInverseId`
FROM `UnidirectionalEntityThrees` AS `u`
LEFT JOIN (
    SELECT `u0`.`ThreeId`, `u0`.`TwoId`, `u1`.`Id`, `u1`.`CollectionInverseId`, `u1`.`ExtraId`, `u1`.`Name`, `u1`.`ReferenceInverseId`
    FROM `UnidirectionalJoinTwoToThree` AS `u0`
    INNER JOIN `UnidirectionalEntityTwos` AS `u1` ON `u0`.`TwoId` = `u1`.`Id`
) AS `s` ON `u`.`Id` = `s`.`ThreeId`
ORDER BY `u`.`Id`, `s`.`Id`, `s`.`ThreeId`
""");
    }

    public override async Task Filtered_include_skip_navigation_order_by_skip_unidirectional(bool async)
    {
        await base.Filtered_include_skip_navigation_order_by_skip_unidirectional(async);

        AssertSql(
"""
SELECT [u].[Id], [u].[CollectionInverseId], [u].[ExtraId], [u].[Name], [u].[ReferenceInverseId], [t0].[SelfSkipSharedRightId], [t0].[UnidirectionalEntityTwoId], [t0].[Id], [t0].[CollectionInverseId], [t0].[ExtraId], [t0].[Name], [t0].[ReferenceInverseId]
FROM [UnidirectionalEntityTwos] AS [u]
LEFT JOIN (
    SELECT [t].[SelfSkipSharedRightId], [t].[UnidirectionalEntityTwoId], [t].[Id], [t].[CollectionInverseId], [t].[ExtraId], [t].[Name], [t].[ReferenceInverseId]
    FROM (
        SELECT [u0].[SelfSkipSharedRightId], [u0].[UnidirectionalEntityTwoId], [u1].[Id], [u1].[CollectionInverseId], [u1].[ExtraId], [u1].[Name], [u1].[ReferenceInverseId], ROW_NUMBER() OVER(PARTITION BY [u0].[UnidirectionalEntityTwoId] ORDER BY [u1].[Id]) AS [row]
        FROM [UnidirectionalEntityTwoUnidirectionalEntityTwo] AS [u0]
        INNER JOIN [UnidirectionalEntityTwos] AS [u1] ON [u0].[SelfSkipSharedRightId] = [u1].[Id]
    ) AS [t]
    WHERE 2 < [t].[row]
) AS [t0] ON [u].[Id] = [t0].[UnidirectionalEntityTwoId]
ORDER BY [u].[Id], [t0].[UnidirectionalEntityTwoId], [t0].[Id]
""");
    }

    public override async Task Filtered_include_skip_navigation_order_by_take_unidirectional(bool async)
    {
        await base.Filtered_include_skip_navigation_order_by_take_unidirectional(async);

        AssertSql(
"""
SELECT [u].[Key1], [u].[Key2], [u].[Key3], [u].[Name], [t0].[TwoSkipSharedId], [t0].[UnidirectionalEntityCompositeKeyKey1], [t0].[UnidirectionalEntityCompositeKeyKey2], [t0].[UnidirectionalEntityCompositeKeyKey3], [t0].[Id], [t0].[CollectionInverseId], [t0].[ExtraId], [t0].[Name], [t0].[ReferenceInverseId]
FROM [UnidirectionalEntityCompositeKeys] AS [u]
LEFT JOIN (
    SELECT [t].[TwoSkipSharedId], [t].[UnidirectionalEntityCompositeKeyKey1], [t].[UnidirectionalEntityCompositeKeyKey2], [t].[UnidirectionalEntityCompositeKeyKey3], [t].[Id], [t].[CollectionInverseId], [t].[ExtraId], [t].[Name], [t].[ReferenceInverseId]
    FROM (
        SELECT [u0].[TwoSkipSharedId], [u0].[UnidirectionalEntityCompositeKeyKey1], [u0].[UnidirectionalEntityCompositeKeyKey2], [u0].[UnidirectionalEntityCompositeKeyKey3], [u1].[Id], [u1].[CollectionInverseId], [u1].[ExtraId], [u1].[Name], [u1].[ReferenceInverseId], ROW_NUMBER() OVER(PARTITION BY [u0].[UnidirectionalEntityCompositeKeyKey1], [u0].[UnidirectionalEntityCompositeKeyKey2], [u0].[UnidirectionalEntityCompositeKeyKey3] ORDER BY [u1].[Id]) AS [row]
        FROM [UnidirectionalEntityCompositeKeyUnidirectionalEntityTwo] AS [u0]
        INNER JOIN [UnidirectionalEntityTwos] AS [u1] ON [u0].[TwoSkipSharedId] = [u1].[Id]
    ) AS [t]
    WHERE [t].[row] <= 2
) AS [t0] ON [u].[Key1] = [t0].[UnidirectionalEntityCompositeKeyKey1] AND [u].[Key2] = [t0].[UnidirectionalEntityCompositeKeyKey2] AND [u].[Key3] = [t0].[UnidirectionalEntityCompositeKeyKey3]
ORDER BY [u].[Key1], [u].[Key2], [u].[Key3], [t0].[UnidirectionalEntityCompositeKeyKey1], [t0].[UnidirectionalEntityCompositeKeyKey2], [t0].[UnidirectionalEntityCompositeKeyKey3], [t0].[Id]
""");
    }

    public override async Task Filtered_include_skip_navigation_order_by_skip_take_unidirectional(bool async)
    {
        await base.Filtered_include_skip_navigation_order_by_skip_take_unidirectional(async);

        AssertSql(
"""
SELECT [u].[Key1], [u].[Key2], [u].[Key3], [u].[Name], [t0].[Id], [t0].[CompositeId1], [t0].[CompositeId2], [t0].[CompositeId3], [t0].[ThreeId], [t0].[Id0], [t0].[CollectionInverseId], [t0].[Name], [t0].[ReferenceInverseId]
FROM [UnidirectionalEntityCompositeKeys] AS [u]
LEFT JOIN (
    SELECT [t].[Id], [t].[CompositeId1], [t].[CompositeId2], [t].[CompositeId3], [t].[ThreeId], [t].[Id0], [t].[CollectionInverseId], [t].[Name], [t].[ReferenceInverseId]
    FROM (
        SELECT [u0].[Id], [u0].[CompositeId1], [u0].[CompositeId2], [u0].[CompositeId3], [u0].[ThreeId], [u1].[Id] AS [Id0], [u1].[CollectionInverseId], [u1].[Name], [u1].[ReferenceInverseId], ROW_NUMBER() OVER(PARTITION BY [u0].[CompositeId1], [u0].[CompositeId2], [u0].[CompositeId3] ORDER BY [u1].[Id]) AS [row]
        FROM [UnidirectionalJoinThreeToCompositeKeyFull] AS [u0]
        INNER JOIN [UnidirectionalEntityThrees] AS [u1] ON [u0].[ThreeId] = [u1].[Id]
    ) AS [t]
    WHERE 1 < [t].[row] AND [t].[row] <= 3
) AS [t0] ON [u].[Key1] = [t0].[CompositeId1] AND [u].[Key2] = [t0].[CompositeId2] AND [u].[Key3] = [t0].[CompositeId3]
ORDER BY [u].[Key1], [u].[Key2], [u].[Key3], [t0].[CompositeId1], [t0].[CompositeId2], [t0].[CompositeId3], [t0].[Id0]
""");
    }

    public override async Task Filtered_include_skip_navigation_where_then_include_skip_navigation_unidirectional(bool async)
    {
        await base.Filtered_include_skip_navigation_where_then_include_skip_navigation_unidirectional(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`Name`, `u`.`Number`, `u`.`IsGreen`, `s0`.`LeafId`, `s0`.`CompositeId1`, `s0`.`CompositeId2`, `s0`.`CompositeId3`, `s0`.`Key1`, `s0`.`Key2`, `s0`.`Key3`, `s0`.`Name`, `s0`.`TwoSkipSharedId`, `s0`.`UnidirectionalEntityCompositeKeyKey1`, `s0`.`UnidirectionalEntityCompositeKeyKey2`, `s0`.`UnidirectionalEntityCompositeKeyKey3`, `s0`.`Id`, `s0`.`CollectionInverseId`, `s0`.`ExtraId`, `s0`.`Name0`, `s0`.`ReferenceInverseId`
FROM `UnidirectionalLeaves` AS `u`
LEFT JOIN (
    SELECT `u0`.`LeafId`, `u0`.`CompositeId1`, `u0`.`CompositeId2`, `u0`.`CompositeId3`, `u1`.`Key1`, `u1`.`Key2`, `u1`.`Key3`, `u1`.`Name`, `s`.`TwoSkipSharedId`, `s`.`UnidirectionalEntityCompositeKeyKey1`, `s`.`UnidirectionalEntityCompositeKeyKey2`, `s`.`UnidirectionalEntityCompositeKeyKey3`, `s`.`Id`, `s`.`CollectionInverseId`, `s`.`ExtraId`, `s`.`Name` AS `Name0`, `s`.`ReferenceInverseId`
    FROM (`UnidirectionalJoinCompositeKeyToLeaf` AS `u0`
    INNER JOIN `UnidirectionalEntityCompositeKeys` AS `u1` ON `u0`.`CompositeId1` = `u1`.`Key1` AND `u0`.`CompositeId2` = `u1`.`Key2` AND `u0`.`CompositeId3` = `u1`.`Key3`)
    LEFT JOIN (
        SELECT `u2`.`TwoSkipSharedId`, `u2`.`UnidirectionalEntityCompositeKeyKey1`, `u2`.`UnidirectionalEntityCompositeKeyKey2`, `u2`.`UnidirectionalEntityCompositeKeyKey3`, `u3`.`Id`, `u3`.`CollectionInverseId`, `u3`.`ExtraId`, `u3`.`Name`, `u3`.`ReferenceInverseId`
        FROM `UnidirectionalEntityCompositeKeyUnidirectionalEntityTwo` AS `u2`
        INNER JOIN `UnidirectionalEntityTwos` AS `u3` ON `u2`.`TwoSkipSharedId` = `u3`.`Id`
    ) AS `s` ON `u1`.`Key1` = `s`.`UnidirectionalEntityCompositeKeyKey1` AND `u1`.`Key2` = `s`.`UnidirectionalEntityCompositeKeyKey2` AND `u1`.`Key3` = `s`.`UnidirectionalEntityCompositeKeyKey3`
    WHERE `u1`.`Key1` < 5
) AS `s0` ON `u`.`Id` = `s0`.`LeafId`
ORDER BY `u`.`Id`, `s0`.`LeafId`, `s0`.`CompositeId1`, `s0`.`CompositeId2`, `s0`.`CompositeId3`, `s0`.`Key1`, `s0`.`Key2`, `s0`.`Key3`, `s0`.`TwoSkipSharedId`, `s0`.`UnidirectionalEntityCompositeKeyKey1`, `s0`.`UnidirectionalEntityCompositeKeyKey2`, `s0`.`UnidirectionalEntityCompositeKeyKey3`
""");
    }

    public override async Task Filter_include_on_skip_navigation_combined_unidirectional(bool async)
    {
        await base.Filter_include_on_skip_navigation_combined_unidirectional(async);

        AssertSql(
            """
SELECT `e`.`Id`, `e`.`CollectionInverseId`, `e`.`ExtraId`, `e`.`Name`, `e`.`ReferenceInverseId`, `s`.`OneId`, `s`.`TwoId`, `s`.`JoinOneToTwoExtraId`, `s`.`Id`, `s`.`Name`, `s`.`Id0`, `s`.`CollectionInverseId`, `s`.`ExtraId`, `s`.`Name0`, `s`.`ReferenceInverseId`, `s`.`Id1`, `s`.`CollectionInverseId0`, `s`.`ExtraId0`, `s`.`Name1`, `s`.`ReferenceInverseId0`
FROM `EntityTwos` AS `e`
LEFT JOIN (
    SELECT `j`.`OneId`, `j`.`TwoId`, `j`.`JoinOneToTwoExtraId`, `e0`.`Id`, `e0`.`Name`, `e1`.`Id` AS `Id0`, `e1`.`CollectionInverseId`, `e1`.`ExtraId`, `e1`.`Name` AS `Name0`, `e1`.`ReferenceInverseId`, `e2`.`Id` AS `Id1`, `e2`.`CollectionInverseId` AS `CollectionInverseId0`, `e2`.`ExtraId` AS `ExtraId0`, `e2`.`Name` AS `Name1`, `e2`.`ReferenceInverseId` AS `ReferenceInverseId0`
    FROM ((`JoinOneToTwo` AS `j`
    INNER JOIN `EntityOnes` AS `e0` ON `j`.`OneId` = `e0`.`Id`)
    LEFT JOIN `EntityTwos` AS `e1` ON `e0`.`Id` = `e1`.`ReferenceInverseId`)
    LEFT JOIN `EntityTwos` AS `e2` ON `e0`.`Id` = `e2`.`CollectionInverseId`
    WHERE `e0`.`Id` < 10
) AS `s` ON `e`.`Id` = `s`.`TwoId`
ORDER BY `e`.`Id`, `s`.`OneId`, `s`.`TwoId`, `s`.`Id`, `s`.`Id0`
""");
    }

    public override async Task Throws_when_different_filtered_include_unidirectional(bool async)
    {
        await base.Throws_when_different_filtered_include_unidirectional(async);

        AssertSql();
    }

    public override async Task Includes_accessed_via_different_path_are_merged_unidirectional(bool async)
    {
        await base.Includes_accessed_via_different_path_are_merged_unidirectional(async);

        AssertSql(
"""
SELECT [u].[Id], [u].[CollectionInverseId], [u].[Name], [u].[ReferenceInverseId], [t0].[OneId], [t0].[ThreeId], [t0].[Payload], [t0].[Id], [t0].[Name], [t0].[OneId0], [t0].[ThreeId0], [t0].[Payload0], [t0].[Id0], [t0].[CollectionInverseId], [t0].[Name0], [t0].[ReferenceInverseId]
FROM [UnidirectionalEntityThrees] AS [u]
LEFT JOIN (
    SELECT [u0].[OneId], [u0].[ThreeId], [u0].[Payload], [u1].[Id], [u1].[Name], [t].[OneId] AS [OneId0], [t].[ThreeId] AS [ThreeId0], [t].[Payload] AS [Payload0], [t].[Id] AS [Id0], [t].[CollectionInverseId], [t].[Name] AS [Name0], [t].[ReferenceInverseId]
    FROM [UnidirectionalJoinOneToThreePayloadFullShared] AS [u0]
    INNER JOIN [UnidirectionalEntityOnes] AS [u1] ON [u0].[OneId] = [u1].[Id]
    LEFT JOIN (
        SELECT [u2].[OneId], [u2].[ThreeId], [u2].[Payload], [u3].[Id], [u3].[CollectionInverseId], [u3].[Name], [u3].[ReferenceInverseId]
        FROM [UnidirectionalJoinOneToThreePayloadFullShared] AS [u2]
        INNER JOIN [UnidirectionalEntityThrees] AS [u3] ON [u2].[ThreeId] = [u3].[Id]
    ) AS [t] ON [u1].[Id] = [t].[OneId]
) AS [t0] ON [u].[Id] = [t0].[ThreeId]
ORDER BY [u].[Id], [t0].[OneId], [t0].[ThreeId], [t0].[Id], [t0].[OneId0], [t0].[ThreeId0]
""");
    }

    public override async Task Select_many_over_skip_navigation_where_non_equality_unidirectional(bool async)
    {
        await base.Select_many_over_skip_navigation_where_non_equality_unidirectional(async);

        AssertSql(
            """
SELECT `s`.`Id`, `s`.`CollectionInverseId`, `s`.`ExtraId`, `s`.`Name`, `s`.`ReferenceInverseId`
FROM `UnidirectionalEntityOnes` AS `u`
LEFT JOIN (
    SELECT `u1`.`Id`, `u1`.`CollectionInverseId`, `u1`.`ExtraId`, `u1`.`Name`, `u1`.`ReferenceInverseId`, `u0`.`OneId`
    FROM `UnidirectionalJoinOneToTwo` AS `u0`
    INNER JOIN `UnidirectionalEntityTwos` AS `u1` ON `u0`.`TwoId` = `u1`.`Id`
) AS `s` ON `u`.`Id` = `s`.`OneId` AND `u`.`Id` <> `s`.`Id`
""");
    }

    public override async Task Contains_on_skip_collection_navigation_unidirectional(bool async)
    {
        await base.Contains_on_skip_collection_navigation_unidirectional(async);

        AssertSql(
            """
@entity_equality_two_Id='1' (Nullable = true)

SELECT `u`.`Id`, `u`.`Name`
FROM `UnidirectionalEntityOnes` AS `u`
WHERE EXISTS (
    SELECT 1
    FROM `UnidirectionalJoinOneToTwo` AS `u0`
    INNER JOIN `UnidirectionalEntityTwos` AS `u1` ON `u0`.`TwoId` = `u1`.`Id`
    WHERE `u`.`Id` = `u0`.`OneId` AND `u1`.`Id` = @entity_equality_two_Id)
""");
    }

    public override async Task GetType_in_hierarchy_in_base_type_unidirectional(bool async)
    {
        await base.GetType_in_hierarchy_in_base_type_unidirectional(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`Name`, CVar(NULL) AS `Number`, CVar(NULL) AS `IsGreen`, 'UnidirectionalEntityRoot' AS `Discriminator`
FROM `UnidirectionalRoots` AS `u`
""");
    }

    public override async Task GetType_in_hierarchy_in_intermediate_type_unidirectional(bool async)
    {
        await base.GetType_in_hierarchy_in_intermediate_type_unidirectional(async);
        AssertSql(
            """
SELECT `u`.`Id`, `u`.`Name`, `u`.`Number`, CVar(NULL) AS `IsGreen`, 'UnidirectionalEntityBranch' AS `Discriminator`
FROM `UnidirectionalBranches` AS `u`
""");
    }

    public override async Task GetType_in_hierarchy_in_leaf_type_unidirectional(bool async)
    {
        await base.GetType_in_hierarchy_in_leaf_type_unidirectional(async);

        AssertSql(
"""
SELECT `u`.`Id`, `u`.`Name`, `u`.`Number`, `u`.`IsGreen`, 'UnidirectionalEntityLeaf' AS `Discriminator`
FROM `UnidirectionalLeaves` AS `u`
""");
    }

    public override async Task GetType_in_hierarchy_in_querying_base_type_unidirectional(bool async)
    {
        await base.GetType_in_hierarchy_in_querying_base_type_unidirectional(async);

        AssertSql(
            """
SELECT `u1`.`Id`, `u1`.`Name`, `u1`.`Number`, `u1`.`IsGreen`, `u1`.`Discriminator`
FROM (
    SELECT `u`.`Id`, `u`.`Name`, `u`.`Number`, CVar(NULL) AS `IsGreen`, 'UnidirectionalEntityBranch' AS `Discriminator`
    FROM `UnidirectionalBranches` AS `u`
    UNION ALL
    SELECT `u0`.`Id`, `u0`.`Name`, `u0`.`Number`, `u0`.`IsGreen`, 'UnidirectionalEntityLeaf' AS `Discriminator`
    FROM `UnidirectionalLeaves` AS `u0`
) AS `u1`
WHERE 0 = 1
""");
    }

    private void AssertSql(params string[] expected)
        => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);
}
