﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;
using Xunit.Abstractions;

namespace EntityFrameworkCore.Jet.FunctionalTests.Query;

public class ComplexNavigationsCollectionsQueryJetTest
    : ComplexNavigationsCollectionsQueryRelationalTestBase<ComplexNavigationsQueryJetFixture>
{
    public ComplexNavigationsCollectionsQueryJetTest(
        ComplexNavigationsQueryJetFixture fixture,
        ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override async Task Multi_level_include_one_to_many_optional_and_one_to_many_optional_produces_valid_sql(bool async)
    {
        await base.Multi_level_include_one_to_many_optional_and_one_to_many_optional_produces_valid_sql(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `s`.`Id`, `s`.`Date`, `s`.`Level1_Optional_Id`, `s`.`Level1_Required_Id`, `s`.`Name`, `s`.`OneToMany_Optional_Inverse2Id`, `s`.`OneToMany_Optional_Self_Inverse2Id`, `s`.`OneToMany_Required_Inverse2Id`, `s`.`OneToMany_Required_Self_Inverse2Id`, `s`.`OneToOne_Optional_PK_Inverse2Id`, `s`.`OneToOne_Optional_Self2Id`, `s`.`Id0`, `s`.`Level2_Optional_Id`, `s`.`Level2_Required_Id`, `s`.`Name0`, `s`.`OneToMany_Optional_Inverse3Id`, `s`.`OneToMany_Optional_Self_Inverse3Id`, `s`.`OneToMany_Required_Inverse3Id`, `s`.`OneToMany_Required_Self_Inverse3Id`, `s`.`OneToOne_Optional_PK_Inverse3Id`, `s`.`OneToOne_Optional_Self3Id`
FROM `LevelOne` AS `l`
LEFT JOIN (
    SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id` AS `Id0`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name` AS `Name0`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`
    FROM `LevelTwo` AS `l0`
    LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Inverse3Id`
) AS `s` ON `l`.`Id` = `s`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`, `s`.`Id`
""");
    }

    public override async Task
        Multi_level_include_correct_PK_is_chosen_as_the_join_predicate_for_queries_that_join_same_table_multiple_times(bool async)
    {
        await base
            .Multi_level_include_correct_PK_is_chosen_as_the_join_predicate_for_queries_that_join_same_table_multiple_times(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `s0`.`Id`, `s0`.`Date`, `s0`.`Level1_Optional_Id`, `s0`.`Level1_Required_Id`, `s0`.`Name`, `s0`.`OneToMany_Optional_Inverse2Id`, `s0`.`OneToMany_Optional_Self_Inverse2Id`, `s0`.`OneToMany_Required_Inverse2Id`, `s0`.`OneToMany_Required_Self_Inverse2Id`, `s0`.`OneToOne_Optional_PK_Inverse2Id`, `s0`.`OneToOne_Optional_Self2Id`, `s0`.`Id0`, `s0`.`Level2_Optional_Id`, `s0`.`Level2_Required_Id`, `s0`.`Name0`, `s0`.`OneToMany_Optional_Inverse3Id`, `s0`.`OneToMany_Optional_Self_Inverse3Id`, `s0`.`OneToMany_Required_Inverse3Id`, `s0`.`OneToMany_Required_Self_Inverse3Id`, `s0`.`OneToOne_Optional_PK_Inverse3Id`, `s0`.`OneToOne_Optional_Self3Id`, `s0`.`Id00`, `s0`.`Date0`, `s0`.`Level1_Optional_Id0`, `s0`.`Level1_Required_Id0`, `s0`.`Name00`, `s0`.`OneToMany_Optional_Inverse2Id0`, `s0`.`OneToMany_Optional_Self_Inverse2Id0`, `s0`.`OneToMany_Required_Inverse2Id0`, `s0`.`OneToMany_Required_Self_Inverse2Id0`, `s0`.`OneToOne_Optional_PK_Inverse2Id0`, `s0`.`OneToOne_Optional_Self2Id0`, `s0`.`Id1`, `s0`.`Level2_Optional_Id0`, `s0`.`Level2_Required_Id0`, `s0`.`Name1`, `s0`.`OneToMany_Optional_Inverse3Id0`, `s0`.`OneToMany_Optional_Self_Inverse3Id0`, `s0`.`OneToMany_Required_Inverse3Id0`, `s0`.`OneToMany_Required_Self_Inverse3Id0`, `s0`.`OneToOne_Optional_PK_Inverse3Id0`, `s0`.`OneToOne_Optional_Self3Id0`
FROM `LevelOne` AS `l`
LEFT JOIN (
    SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `s`.`Id` AS `Id0`, `s`.`Level2_Optional_Id`, `s`.`Level2_Required_Id`, `s`.`Name` AS `Name0`, `s`.`OneToMany_Optional_Inverse3Id`, `s`.`OneToMany_Optional_Self_Inverse3Id`, `s`.`OneToMany_Required_Inverse3Id`, `s`.`OneToMany_Required_Self_Inverse3Id`, `s`.`OneToOne_Optional_PK_Inverse3Id`, `s`.`OneToOne_Optional_Self3Id`, `s`.`Id0` AS `Id00`, `s`.`Date` AS `Date0`, `s`.`Level1_Optional_Id` AS `Level1_Optional_Id0`, `s`.`Level1_Required_Id` AS `Level1_Required_Id0`, `s`.`Name0` AS `Name00`, `s`.`OneToMany_Optional_Inverse2Id` AS `OneToMany_Optional_Inverse2Id0`, `s`.`OneToMany_Optional_Self_Inverse2Id` AS `OneToMany_Optional_Self_Inverse2Id0`, `s`.`OneToMany_Required_Inverse2Id` AS `OneToMany_Required_Inverse2Id0`, `s`.`OneToMany_Required_Self_Inverse2Id` AS `OneToMany_Required_Self_Inverse2Id0`, `s`.`OneToOne_Optional_PK_Inverse2Id` AS `OneToOne_Optional_PK_Inverse2Id0`, `s`.`OneToOne_Optional_Self2Id` AS `OneToOne_Optional_Self2Id0`, `s`.`Id1`, `s`.`Level2_Optional_Id0`, `s`.`Level2_Required_Id0`, `s`.`Name1`, `s`.`OneToMany_Optional_Inverse3Id0`, `s`.`OneToMany_Optional_Self_Inverse3Id0`, `s`.`OneToMany_Required_Inverse3Id0`, `s`.`OneToMany_Required_Self_Inverse3Id0`, `s`.`OneToOne_Optional_PK_Inverse3Id0`, `s`.`OneToOne_Optional_Self3Id0`
    FROM `LevelTwo` AS `l0`
    LEFT JOIN (
        SELECT `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`, `l2`.`Id` AS `Id0`, `l2`.`Date`, `l2`.`Level1_Optional_Id`, `l2`.`Level1_Required_Id`, `l2`.`Name` AS `Name0`, `l2`.`OneToMany_Optional_Inverse2Id`, `l2`.`OneToMany_Optional_Self_Inverse2Id`, `l2`.`OneToMany_Required_Inverse2Id`, `l2`.`OneToMany_Required_Self_Inverse2Id`, `l2`.`OneToOne_Optional_PK_Inverse2Id`, `l2`.`OneToOne_Optional_Self2Id`, `l3`.`Id` AS `Id1`, `l3`.`Level2_Optional_Id` AS `Level2_Optional_Id0`, `l3`.`Level2_Required_Id` AS `Level2_Required_Id0`, `l3`.`Name` AS `Name1`, `l3`.`OneToMany_Optional_Inverse3Id` AS `OneToMany_Optional_Inverse3Id0`, `l3`.`OneToMany_Optional_Self_Inverse3Id` AS `OneToMany_Optional_Self_Inverse3Id0`, `l3`.`OneToMany_Required_Inverse3Id` AS `OneToMany_Required_Inverse3Id0`, `l3`.`OneToMany_Required_Self_Inverse3Id` AS `OneToMany_Required_Self_Inverse3Id0`, `l3`.`OneToOne_Optional_PK_Inverse3Id` AS `OneToOne_Optional_PK_Inverse3Id0`, `l3`.`OneToOne_Optional_Self3Id` AS `OneToOne_Optional_Self3Id0`
        FROM (`LevelThree` AS `l1`
        INNER JOIN `LevelTwo` AS `l2` ON `l1`.`OneToMany_Required_Inverse3Id` = `l2`.`Id`)
        LEFT JOIN `LevelThree` AS `l3` ON `l2`.`Id` = `l3`.`OneToMany_Optional_Inverse3Id`
    ) AS `s` ON `l0`.`Id` = `s`.`OneToMany_Optional_Inverse3Id`
) AS `s0` ON `l`.`Id` = `s0`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`, `s0`.`Id`, `s0`.`Id0`, `s0`.`Id00`
""");
    }

    public override async Task Multiple_complex_includes(bool async)
    {
        await base.Multiple_complex_includes(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`, `s`.`Id`, `s`.`Date`, `s`.`Level1_Optional_Id`, `s`.`Level1_Required_Id`, `s`.`Name`, `s`.`OneToMany_Optional_Inverse2Id`, `s`.`OneToMany_Optional_Self_Inverse2Id`, `s`.`OneToMany_Required_Inverse2Id`, `s`.`OneToMany_Required_Self_Inverse2Id`, `s`.`OneToOne_Optional_PK_Inverse2Id`, `s`.`OneToOne_Optional_Self2Id`, `s`.`Id0`, `s`.`Level2_Optional_Id`, `s`.`Level2_Required_Id`, `s`.`Name0`, `s`.`OneToMany_Optional_Inverse3Id`, `s`.`OneToMany_Optional_Self_Inverse3Id`, `s`.`OneToMany_Required_Inverse3Id`, `s`.`OneToMany_Required_Self_Inverse3Id`, `s`.`OneToOne_Optional_PK_Inverse3Id`, `s`.`OneToOne_Optional_Self3Id`
FROM ((`LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`Level1_Optional_Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Inverse3Id`)
LEFT JOIN (
    SELECT `l2`.`Id`, `l2`.`Date`, `l2`.`Level1_Optional_Id`, `l2`.`Level1_Required_Id`, `l2`.`Name`, `l2`.`OneToMany_Optional_Inverse2Id`, `l2`.`OneToMany_Optional_Self_Inverse2Id`, `l2`.`OneToMany_Required_Inverse2Id`, `l2`.`OneToMany_Required_Self_Inverse2Id`, `l2`.`OneToOne_Optional_PK_Inverse2Id`, `l2`.`OneToOne_Optional_Self2Id`, `l3`.`Id` AS `Id0`, `l3`.`Level2_Optional_Id`, `l3`.`Level2_Required_Id`, `l3`.`Name` AS `Name0`, `l3`.`OneToMany_Optional_Inverse3Id`, `l3`.`OneToMany_Optional_Self_Inverse3Id`, `l3`.`OneToMany_Required_Inverse3Id`, `l3`.`OneToMany_Required_Self_Inverse3Id`, `l3`.`OneToOne_Optional_PK_Inverse3Id`, `l3`.`OneToOne_Optional_Self3Id`
    FROM `LevelTwo` AS `l2`
    LEFT JOIN `LevelThree` AS `l3` ON `l2`.`Id` = `l3`.`Level2_Optional_Id`
) AS `s` ON `l`.`Id` = `s`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`, `l0`.`Id`, `l1`.`Id`, `s`.`Id`
""");
    }

    public override async Task Multiple_complex_includes_self_ref(bool async)
    {
        await base.Multiple_complex_includes_self_ref(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Name`, `l0`.`OneToMany_Optional_Self_Inverse1Id`, `l0`.`OneToMany_Required_Self_Inverse1Id`, `l0`.`OneToOne_Optional_Self1Id`, `l1`.`Id`, `l1`.`Date`, `l1`.`Name`, `l1`.`OneToMany_Optional_Self_Inverse1Id`, `l1`.`OneToMany_Required_Self_Inverse1Id`, `l1`.`OneToOne_Optional_Self1Id`, `s`.`Id`, `s`.`Date`, `s`.`Name`, `s`.`OneToMany_Optional_Self_Inverse1Id`, `s`.`OneToMany_Required_Self_Inverse1Id`, `s`.`OneToOne_Optional_Self1Id`, `s`.`Id0`, `s`.`Date0`, `s`.`Name0`, `s`.`OneToMany_Optional_Self_Inverse1Id0`, `s`.`OneToMany_Required_Self_Inverse1Id0`, `s`.`OneToOne_Optional_Self1Id0`
FROM ((`LevelOne` AS `l`
LEFT JOIN `LevelOne` AS `l0` ON `l`.`OneToOne_Optional_Self1Id` = `l0`.`Id`)
LEFT JOIN `LevelOne` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Self_Inverse1Id`)
LEFT JOIN (
    SELECT `l2`.`Id`, `l2`.`Date`, `l2`.`Name`, `l2`.`OneToMany_Optional_Self_Inverse1Id`, `l2`.`OneToMany_Required_Self_Inverse1Id`, `l2`.`OneToOne_Optional_Self1Id`, `l3`.`Id` AS `Id0`, `l3`.`Date` AS `Date0`, `l3`.`Name` AS `Name0`, `l3`.`OneToMany_Optional_Self_Inverse1Id` AS `OneToMany_Optional_Self_Inverse1Id0`, `l3`.`OneToMany_Required_Self_Inverse1Id` AS `OneToMany_Required_Self_Inverse1Id0`, `l3`.`OneToOne_Optional_Self1Id` AS `OneToOne_Optional_Self1Id0`
    FROM `LevelOne` AS `l2`
    LEFT JOIN `LevelOne` AS `l3` ON `l2`.`OneToOne_Optional_Self1Id` = `l3`.`Id`
) AS `s` ON `l`.`Id` = `s`.`OneToMany_Optional_Self_Inverse1Id`
ORDER BY `l`.`Id`, `l0`.`Id`, `l1`.`Id`, `s`.`Id`
""");
    }

    public override async Task Include_reference_and_collection_order_by(bool async)
    {
        await base.Include_reference_and_collection_order_by(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`
FROM (`LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`Level1_Optional_Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Inverse3Id`
ORDER BY `l`.`Name`, `l`.`Id`, `l0`.`Id`
""");
    }

    public override async Task Include_reference_ThenInclude_collection_order_by(bool async)
    {
        await base.Include_reference_ThenInclude_collection_order_by(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`
FROM (`LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`Level1_Optional_Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Inverse3Id`
ORDER BY `l`.`Name`, `l`.`Id`, `l0`.`Id`
""");
    }

    public override async Task Include_collection_then_reference(bool async)
    {
        await base.Include_collection_then_reference(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `s`.`Id`, `s`.`Date`, `s`.`Level1_Optional_Id`, `s`.`Level1_Required_Id`, `s`.`Name`, `s`.`OneToMany_Optional_Inverse2Id`, `s`.`OneToMany_Optional_Self_Inverse2Id`, `s`.`OneToMany_Required_Inverse2Id`, `s`.`OneToMany_Required_Self_Inverse2Id`, `s`.`OneToOne_Optional_PK_Inverse2Id`, `s`.`OneToOne_Optional_Self2Id`, `s`.`Id0`, `s`.`Level2_Optional_Id`, `s`.`Level2_Required_Id`, `s`.`Name0`, `s`.`OneToMany_Optional_Inverse3Id`, `s`.`OneToMany_Optional_Self_Inverse3Id`, `s`.`OneToMany_Required_Inverse3Id`, `s`.`OneToMany_Required_Self_Inverse3Id`, `s`.`OneToOne_Optional_PK_Inverse3Id`, `s`.`OneToOne_Optional_Self3Id`
FROM `LevelOne` AS `l`
LEFT JOIN (
    SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id` AS `Id0`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name` AS `Name0`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`
    FROM `LevelTwo` AS `l0`
    LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`Level2_Optional_Id`
) AS `s` ON `l`.`Id` = `s`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`, `s`.`Id`
""");
    }

    public override async Task Include_collection_with_conditional_order_by(bool async)
    {
        await base.Include_collection_with_conditional_order_by(async);

        AssertSql(
"""
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`
FROM `LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Optional_Inverse2Id`
ORDER BY IIF(`l`.`Name` LIKE '%03', 1, 2), `l`.`Id`
""");
    }

    public override async Task Multiple_complex_include_select(bool async)
    {
        await base.Multiple_complex_include_select(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`, `s`.`Id`, `s`.`Date`, `s`.`Level1_Optional_Id`, `s`.`Level1_Required_Id`, `s`.`Name`, `s`.`OneToMany_Optional_Inverse2Id`, `s`.`OneToMany_Optional_Self_Inverse2Id`, `s`.`OneToMany_Required_Inverse2Id`, `s`.`OneToMany_Required_Self_Inverse2Id`, `s`.`OneToOne_Optional_PK_Inverse2Id`, `s`.`OneToOne_Optional_Self2Id`, `s`.`Id0`, `s`.`Level2_Optional_Id`, `s`.`Level2_Required_Id`, `s`.`Name0`, `s`.`OneToMany_Optional_Inverse3Id`, `s`.`OneToMany_Optional_Self_Inverse3Id`, `s`.`OneToMany_Required_Inverse3Id`, `s`.`OneToMany_Required_Self_Inverse3Id`, `s`.`OneToOne_Optional_PK_Inverse3Id`, `s`.`OneToOne_Optional_Self3Id`
FROM ((`LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`Level1_Optional_Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Inverse3Id`)
LEFT JOIN (
    SELECT `l2`.`Id`, `l2`.`Date`, `l2`.`Level1_Optional_Id`, `l2`.`Level1_Required_Id`, `l2`.`Name`, `l2`.`OneToMany_Optional_Inverse2Id`, `l2`.`OneToMany_Optional_Self_Inverse2Id`, `l2`.`OneToMany_Required_Inverse2Id`, `l2`.`OneToMany_Required_Self_Inverse2Id`, `l2`.`OneToOne_Optional_PK_Inverse2Id`, `l2`.`OneToOne_Optional_Self2Id`, `l3`.`Id` AS `Id0`, `l3`.`Level2_Optional_Id`, `l3`.`Level2_Required_Id`, `l3`.`Name` AS `Name0`, `l3`.`OneToMany_Optional_Inverse3Id`, `l3`.`OneToMany_Optional_Self_Inverse3Id`, `l3`.`OneToMany_Required_Inverse3Id`, `l3`.`OneToMany_Required_Self_Inverse3Id`, `l3`.`OneToOne_Optional_PK_Inverse3Id`, `l3`.`OneToOne_Optional_Self3Id`
    FROM `LevelTwo` AS `l2`
    LEFT JOIN `LevelThree` AS `l3` ON `l2`.`Id` = `l3`.`Level2_Optional_Id`
) AS `s` ON `l`.`Id` = `s`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`, `l0`.`Id`, `l1`.`Id`, `s`.`Id`
""");
    }

    public override async Task Include_nested_with_optional_navigation(bool async)
    {
        await base.Include_nested_with_optional_navigation(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `s`.`Id`, `s`.`Level2_Optional_Id`, `s`.`Level2_Required_Id`, `s`.`Name`, `s`.`OneToMany_Optional_Inverse3Id`, `s`.`OneToMany_Optional_Self_Inverse3Id`, `s`.`OneToMany_Required_Inverse3Id`, `s`.`OneToMany_Required_Self_Inverse3Id`, `s`.`OneToOne_Optional_PK_Inverse3Id`, `s`.`OneToOne_Optional_Self3Id`, `s`.`Id0`, `s`.`Level3_Optional_Id`, `s`.`Level3_Required_Id`, `s`.`Name0`, `s`.`OneToMany_Optional_Inverse4Id`, `s`.`OneToMany_Optional_Self_Inverse4Id`, `s`.`OneToMany_Required_Inverse4Id`, `s`.`OneToMany_Required_Self_Inverse4Id`, `s`.`OneToOne_Optional_PK_Inverse4Id`, `s`.`OneToOne_Optional_Self4Id`
FROM (`LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`Level1_Optional_Id`)
LEFT JOIN (
    SELECT `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`, `l2`.`Id` AS `Id0`, `l2`.`Level3_Optional_Id`, `l2`.`Level3_Required_Id`, `l2`.`Name` AS `Name0`, `l2`.`OneToMany_Optional_Inverse4Id`, `l2`.`OneToMany_Optional_Self_Inverse4Id`, `l2`.`OneToMany_Required_Inverse4Id`, `l2`.`OneToMany_Required_Self_Inverse4Id`, `l2`.`OneToOne_Optional_PK_Inverse4Id`, `l2`.`OneToOne_Optional_Self4Id`
    FROM `LevelThree` AS `l1`
    LEFT JOIN `LevelFour` AS `l2` ON `l1`.`Id` = `l2`.`Level3_Required_Id`
) AS `s` ON `l0`.`Id` = `s`.`OneToMany_Required_Inverse3Id`
WHERE `l0`.`Name` <> 'L2 09' OR `l0`.`Name` IS NULL
ORDER BY `l`.`Id`, `l0`.`Id`, `s`.`Id`
""");
    }

    public override async Task Complex_multi_include_with_order_by_and_paging(bool async)
    {
        await base.Complex_multi_include_with_order_by_and_paging(async);

        AssertSql(
            """
SELECT `l1`.`Id`, `l1`.`Date`, `l1`.`Name`, `l1`.`OneToMany_Optional_Self_Inverse1Id`, `l1`.`OneToMany_Required_Self_Inverse1Id`, `l1`.`OneToOne_Optional_Self1Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l2`.`Id`, `l2`.`Level2_Optional_Id`, `l2`.`Level2_Required_Id`, `l2`.`Name`, `l2`.`OneToMany_Optional_Inverse3Id`, `l2`.`OneToMany_Optional_Self_Inverse3Id`, `l2`.`OneToMany_Required_Inverse3Id`, `l2`.`OneToMany_Required_Self_Inverse3Id`, `l2`.`OneToOne_Optional_PK_Inverse3Id`, `l2`.`OneToOne_Optional_Self3Id`, `l3`.`Id`, `l3`.`Level2_Optional_Id`, `l3`.`Level2_Required_Id`, `l3`.`Name`, `l3`.`OneToMany_Optional_Inverse3Id`, `l3`.`OneToMany_Optional_Self_Inverse3Id`, `l3`.`OneToMany_Required_Inverse3Id`, `l3`.`OneToMany_Required_Self_Inverse3Id`, `l3`.`OneToOne_Optional_PK_Inverse3Id`, `l3`.`OneToOne_Optional_Self3Id`
FROM (((
    SELECT TOP @p0 `l4`.`Id`, `l4`.`Date`, `l4`.`Name`, `l4`.`OneToMany_Optional_Self_Inverse1Id`, `l4`.`OneToMany_Required_Self_Inverse1Id`, `l4`.`OneToOne_Optional_Self1Id`
    FROM (
        SELECT TOP @p + @p0 `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`
        FROM `LevelOne` AS `l`
        ORDER BY `l`.`Name`
    ) AS `l4`
    ORDER BY `l4`.`Name` DESC
) AS `l1`
LEFT JOIN `LevelTwo` AS `l0` ON `l1`.`Id` = `l0`.`Level1_Required_Id`)
LEFT JOIN `LevelThree` AS `l2` ON `l0`.`Id` = `l2`.`OneToMany_Optional_Inverse3Id`)
LEFT JOIN `LevelThree` AS `l3` ON `l0`.`Id` = `l3`.`OneToMany_Required_Inverse3Id`
ORDER BY `l1`.`Name`, `l1`.`Id`, `l0`.`Id`, `l2`.`Id`
""");
    }

    public override async Task Complex_multi_include_with_order_by_and_paging_joins_on_correct_key(bool async)
    {
        await base.Complex_multi_include_with_order_by_and_paging_joins_on_correct_key(async);

        AssertSql(
            """
SELECT `l1`.`Id`, `l1`.`Date`, `l1`.`Name`, `l1`.`OneToMany_Optional_Self_Inverse1Id`, `l1`.`OneToMany_Required_Self_Inverse1Id`, `l1`.`OneToOne_Optional_Self1Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l2`.`Id`, `l3`.`Id`, `l3`.`Level2_Optional_Id`, `l3`.`Level2_Required_Id`, `l3`.`Name`, `l3`.`OneToMany_Optional_Inverse3Id`, `l3`.`OneToMany_Optional_Self_Inverse3Id`, `l3`.`OneToMany_Required_Inverse3Id`, `l3`.`OneToMany_Required_Self_Inverse3Id`, `l3`.`OneToOne_Optional_PK_Inverse3Id`, `l3`.`OneToOne_Optional_Self3Id`, `l2`.`Date`, `l2`.`Level1_Optional_Id`, `l2`.`Level1_Required_Id`, `l2`.`Name`, `l2`.`OneToMany_Optional_Inverse2Id`, `l2`.`OneToMany_Optional_Self_Inverse2Id`, `l2`.`OneToMany_Required_Inverse2Id`, `l2`.`OneToMany_Required_Self_Inverse2Id`, `l2`.`OneToOne_Optional_PK_Inverse2Id`, `l2`.`OneToOne_Optional_Self2Id`, `l4`.`Id`, `l4`.`Level2_Optional_Id`, `l4`.`Level2_Required_Id`, `l4`.`Name`, `l4`.`OneToMany_Optional_Inverse3Id`, `l4`.`OneToMany_Optional_Self_Inverse3Id`, `l4`.`OneToMany_Required_Inverse3Id`, `l4`.`OneToMany_Required_Self_Inverse3Id`, `l4`.`OneToOne_Optional_PK_Inverse3Id`, `l4`.`OneToOne_Optional_Self3Id`
FROM ((((
    SELECT TOP @p0 `l5`.`Id`, `l5`.`Date`, `l5`.`Name`, `l5`.`OneToMany_Optional_Self_Inverse1Id`, `l5`.`OneToMany_Required_Self_Inverse1Id`, `l5`.`OneToOne_Optional_Self1Id`
    FROM (
        SELECT TOP @p + @p0 `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`
        FROM `LevelOne` AS `l`
        ORDER BY `l`.`Name`
    ) AS `l5`
    ORDER BY `l5`.`Name` DESC
) AS `l1`
LEFT JOIN `LevelTwo` AS `l0` ON `l1`.`Id` = `l0`.`Level1_Optional_Id`)
LEFT JOIN `LevelTwo` AS `l2` ON `l1`.`Id` = `l2`.`Level1_Required_Id`)
LEFT JOIN `LevelThree` AS `l3` ON `l0`.`Id` = `l3`.`OneToMany_Optional_Inverse3Id`)
LEFT JOIN `LevelThree` AS `l4` ON `l2`.`Id` = `l4`.`OneToMany_Required_Inverse3Id`
ORDER BY `l1`.`Name`, `l1`.`Id`, `l0`.`Id`, `l2`.`Id`, `l3`.`Id`
""");
    }

    public override async Task Complex_multi_include_with_order_by_and_paging_joins_on_correct_key2(bool async)
    {
        await base.Complex_multi_include_with_order_by_and_paging_joins_on_correct_key2(async);

        AssertSql(
            """
SELECT `l1`.`Id`, `l1`.`Date`, `l1`.`Name`, `l1`.`OneToMany_Optional_Self_Inverse1Id`, `l1`.`OneToMany_Required_Self_Inverse1Id`, `l1`.`OneToOne_Optional_Self1Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l2`.`Id`, `l2`.`Level2_Optional_Id`, `l2`.`Level2_Required_Id`, `l2`.`Name`, `l2`.`OneToMany_Optional_Inverse3Id`, `l2`.`OneToMany_Optional_Self_Inverse3Id`, `l2`.`OneToMany_Required_Inverse3Id`, `l2`.`OneToMany_Required_Self_Inverse3Id`, `l2`.`OneToOne_Optional_PK_Inverse3Id`, `l2`.`OneToOne_Optional_Self3Id`, `l3`.`Id`, `l3`.`Level3_Optional_Id`, `l3`.`Level3_Required_Id`, `l3`.`Name`, `l3`.`OneToMany_Optional_Inverse4Id`, `l3`.`OneToMany_Optional_Self_Inverse4Id`, `l3`.`OneToMany_Required_Inverse4Id`, `l3`.`OneToMany_Required_Self_Inverse4Id`, `l3`.`OneToOne_Optional_PK_Inverse4Id`, `l3`.`OneToOne_Optional_Self4Id`
FROM (((
    SELECT TOP @p0 `l4`.`Id`, `l4`.`Date`, `l4`.`Name`, `l4`.`OneToMany_Optional_Self_Inverse1Id`, `l4`.`OneToMany_Required_Self_Inverse1Id`, `l4`.`OneToOne_Optional_Self1Id`
    FROM (
        SELECT TOP @p + @p0 `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`
        FROM `LevelOne` AS `l`
        ORDER BY `l`.`Name`
    ) AS `l4`
    ORDER BY `l4`.`Name` DESC
) AS `l1`
LEFT JOIN `LevelTwo` AS `l0` ON `l1`.`Id` = `l0`.`Level1_Optional_Id`)
LEFT JOIN `LevelThree` AS `l2` ON `l0`.`Id` = `l2`.`Level2_Required_Id`)
LEFT JOIN `LevelFour` AS `l3` ON `l2`.`Id` = `l3`.`OneToMany_Optional_Inverse4Id`
ORDER BY `l1`.`Name`, `l1`.`Id`, `l0`.`Id`, `l2`.`Id`
""");
    }

    public override async Task Multiple_include_with_multiple_optional_navigations(bool async)
    {
        await base.Multiple_include_with_multiple_optional_navigations(async);

        AssertSql(
"""
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id`, `l2`.`Id`, `l3`.`Id`, `l4`.`Id`, `l5`.`Id`, `l5`.`Level2_Optional_Id`, `l5`.`Level2_Required_Id`, `l5`.`Name`, `l5`.`OneToMany_Optional_Inverse3Id`, `l5`.`OneToMany_Optional_Self_Inverse3Id`, `l5`.`OneToMany_Required_Inverse3Id`, `l5`.`OneToMany_Required_Self_Inverse3Id`, `l5`.`OneToOne_Optional_PK_Inverse3Id`, `l5`.`OneToOne_Optional_Self3Id`, `l2`.`Level2_Optional_Id`, `l2`.`Level2_Required_Id`, `l2`.`Name`, `l2`.`OneToMany_Optional_Inverse3Id`, `l2`.`OneToMany_Optional_Self_Inverse3Id`, `l2`.`OneToMany_Required_Inverse3Id`, `l2`.`OneToMany_Required_Self_Inverse3Id`, `l2`.`OneToOne_Optional_PK_Inverse3Id`, `l2`.`OneToOne_Optional_Self3Id`, `l3`.`Date`, `l3`.`Level1_Optional_Id`, `l3`.`Level1_Required_Id`, `l3`.`Name`, `l3`.`OneToMany_Optional_Inverse2Id`, `l3`.`OneToMany_Optional_Self_Inverse2Id`, `l3`.`OneToMany_Required_Inverse2Id`, `l3`.`OneToMany_Required_Self_Inverse2Id`, `l3`.`OneToOne_Optional_PK_Inverse2Id`, `l3`.`OneToOne_Optional_Self2Id`, `l4`.`Level2_Optional_Id`, `l4`.`Level2_Required_Id`, `l4`.`Name`, `l4`.`OneToMany_Optional_Inverse3Id`, `l4`.`OneToMany_Optional_Self_Inverse3Id`, `l4`.`OneToMany_Required_Inverse3Id`, `l4`.`OneToMany_Required_Self_Inverse3Id`, `l4`.`OneToOne_Optional_PK_Inverse3Id`, `l4`.`OneToOne_Optional_Self3Id`
FROM (((((`LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`Level1_Required_Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToOne_Optional_PK_Inverse3Id`)
LEFT JOIN `LevelThree` AS `l2` ON `l0`.`Id` = `l2`.`Level2_Optional_Id`)
LEFT JOIN `LevelTwo` AS `l3` ON `l`.`Id` = `l3`.`Level1_Optional_Id`)
LEFT JOIN `LevelThree` AS `l4` ON `l3`.`Id` = `l4`.`Level2_Optional_Id`)
LEFT JOIN `LevelThree` AS `l5` ON `l0`.`Id` = `l5`.`OneToMany_Optional_Inverse3Id`
WHERE `l1`.`Name` <> 'Foo' OR `l1`.`Name` IS NULL
ORDER BY `l`.`Id`, `l0`.`Id`, `l1`.`Id`, `l2`.`Id`, `l3`.`Id`, `l4`.`Id`
""");
    }

    public override async Task SelectMany_with_Include1(bool async)
    {
        await base.SelectMany_with_Include1(async);

        AssertSql(
            """
SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l`.`Id`, `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`
FROM (`LevelOne` AS `l`
INNER JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Optional_Inverse2Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Inverse3Id`
ORDER BY `l`.`Id`, `l0`.`Id`
""");
    }

    public override async Task Orderby_SelectMany_with_Include1(bool async)
    {
        await base.Orderby_SelectMany_with_Include1(async);

        AssertSql(
            """
SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l`.`Id`, `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`
FROM (`LevelOne` AS `l`
INNER JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Optional_Inverse2Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Inverse3Id`
ORDER BY `l`.`Id`, `l0`.`Id`
""");
    }

    public override async Task SelectMany_with_Include2(bool async)
    {
        await base.SelectMany_with_Include2(async);

        AssertSql(
            """
SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`
FROM (`LevelOne` AS `l`
INNER JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Optional_Inverse2Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`Level2_Required_Id`
""");
    }

    public override async Task SelectMany_with_Include_ThenInclude(bool async)
    {
        await base.SelectMany_with_Include_ThenInclude(async);

        AssertSql(
            """
SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`, `l`.`Id`, `l2`.`Id`, `l2`.`Level3_Optional_Id`, `l2`.`Level3_Required_Id`, `l2`.`Name`, `l2`.`OneToMany_Optional_Inverse4Id`, `l2`.`OneToMany_Optional_Self_Inverse4Id`, `l2`.`OneToMany_Required_Inverse4Id`, `l2`.`OneToMany_Required_Self_Inverse4Id`, `l2`.`OneToOne_Optional_PK_Inverse4Id`, `l2`.`OneToOne_Optional_Self4Id`
FROM ((`LevelOne` AS `l`
INNER JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Optional_Inverse2Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`Level2_Required_Id`)
LEFT JOIN `LevelFour` AS `l2` ON `l1`.`Id` = `l2`.`OneToMany_Optional_Inverse4Id`
ORDER BY `l`.`Id`, `l0`.`Id`, `l1`.`Id`
""");
    }

    public override async Task Multiple_SelectMany_with_Include(bool async)
    {
        await base.Multiple_SelectMany_with_Include(async);

        AssertSql(
            """
SELECT `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`, `l2`.`Id`, `l2`.`Level3_Optional_Id`, `l2`.`Level3_Required_Id`, `l2`.`Name`, `l2`.`OneToMany_Optional_Inverse4Id`, `l2`.`OneToMany_Optional_Self_Inverse4Id`, `l2`.`OneToMany_Required_Inverse4Id`, `l2`.`OneToMany_Required_Self_Inverse4Id`, `l2`.`OneToOne_Optional_PK_Inverse4Id`, `l2`.`OneToOne_Optional_Self4Id`, `l`.`Id`, `l0`.`Id`, `l3`.`Id`, `l3`.`Level3_Optional_Id`, `l3`.`Level3_Required_Id`, `l3`.`Name`, `l3`.`OneToMany_Optional_Inverse4Id`, `l3`.`OneToMany_Optional_Self_Inverse4Id`, `l3`.`OneToMany_Required_Inverse4Id`, `l3`.`OneToMany_Required_Self_Inverse4Id`, `l3`.`OneToOne_Optional_PK_Inverse4Id`, `l3`.`OneToOne_Optional_Self4Id`
FROM (((`LevelOne` AS `l`
INNER JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Optional_Inverse2Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Inverse3Id`)
LEFT JOIN `LevelFour` AS `l2` ON `l1`.`Id` = `l2`.`Level3_Required_Id`)
LEFT JOIN `LevelFour` AS `l3` ON `l1`.`Id` = `l3`.`OneToMany_Optional_Inverse4Id`
WHERE `l0`.`Id` IS NOT NULL AND `l1`.`OneToMany_Optional_Inverse3Id` IS NOT NULL
ORDER BY `l`.`Id`, `l0`.`Id`, `l1`.`Id`, `l2`.`Id`
""");
    }

    public override async Task Required_navigation_with_Include(bool async)
    {
        await base.Required_navigation_with_Include(async);

        AssertSql(
            """
SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id`, `l1`.`Date`, `l1`.`Name`, `l1`.`OneToMany_Optional_Self_Inverse1Id`, `l1`.`OneToMany_Required_Self_Inverse1Id`, `l1`.`OneToOne_Optional_Self1Id`
FROM (`LevelThree` AS `l`
INNER JOIN `LevelTwo` AS `l0` ON `l`.`Level2_Required_Id` = `l0`.`Id`)
LEFT JOIN `LevelOne` AS `l1` ON `l0`.`OneToMany_Required_Inverse2Id` = `l1`.`Id`
WHERE `l0`.`OneToMany_Required_Inverse2Id` IS NOT NULL AND `l1`.`Id` IS NOT NULL
""");
    }

    public override async Task Required_navigation_with_Include_ThenInclude(bool async)
    {
        await base.Required_navigation_with_Include_ThenInclude(async);

        AssertSql(
            """
SELECT `l0`.`Id`, `l0`.`Level2_Optional_Id`, `l0`.`Level2_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse3Id`, `l0`.`OneToMany_Optional_Self_Inverse3Id`, `l0`.`OneToMany_Required_Inverse3Id`, `l0`.`OneToMany_Required_Self_Inverse3Id`, `l0`.`OneToOne_Optional_PK_Inverse3Id`, `l0`.`OneToOne_Optional_Self3Id`, `l1`.`Id`, `l1`.`Date`, `l1`.`Level1_Optional_Id`, `l1`.`Level1_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse2Id`, `l1`.`OneToMany_Optional_Self_Inverse2Id`, `l1`.`OneToMany_Required_Inverse2Id`, `l1`.`OneToMany_Required_Self_Inverse2Id`, `l1`.`OneToOne_Optional_PK_Inverse2Id`, `l1`.`OneToOne_Optional_Self2Id`, `l2`.`Id`, `l2`.`Date`, `l2`.`Name`, `l2`.`OneToMany_Optional_Self_Inverse1Id`, `l2`.`OneToMany_Required_Self_Inverse1Id`, `l2`.`OneToOne_Optional_Self1Id`
FROM ((`LevelFour` AS `l`
INNER JOIN `LevelThree` AS `l0` ON `l`.`Level3_Required_Id` = `l0`.`Id`)
LEFT JOIN `LevelTwo` AS `l1` ON `l0`.`OneToMany_Required_Inverse3Id` = `l1`.`Id`)
LEFT JOIN `LevelOne` AS `l2` ON `l1`.`OneToMany_Optional_Inverse2Id` = `l2`.`Id`
WHERE `l0`.`OneToMany_Required_Inverse3Id` IS NOT NULL AND `l1`.`Id` IS NOT NULL
""");
    }

    public override async Task Optional_navigation_with_Include_ThenInclude(bool async)
    {
        await base.Optional_navigation_with_Include_ThenInclude(async);

        AssertSql(
            """
SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l`.`Id`, `s`.`Id`, `s`.`Level2_Optional_Id`, `s`.`Level2_Required_Id`, `s`.`Name`, `s`.`OneToMany_Optional_Inverse3Id`, `s`.`OneToMany_Optional_Self_Inverse3Id`, `s`.`OneToMany_Required_Inverse3Id`, `s`.`OneToMany_Required_Self_Inverse3Id`, `s`.`OneToOne_Optional_PK_Inverse3Id`, `s`.`OneToOne_Optional_Self3Id`, `s`.`Id0`, `s`.`Level3_Optional_Id`, `s`.`Level3_Required_Id`, `s`.`Name0`, `s`.`OneToMany_Optional_Inverse4Id`, `s`.`OneToMany_Optional_Self_Inverse4Id`, `s`.`OneToMany_Required_Inverse4Id`, `s`.`OneToMany_Required_Self_Inverse4Id`, `s`.`OneToOne_Optional_PK_Inverse4Id`, `s`.`OneToOne_Optional_Self4Id`
FROM (`LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`Level1_Optional_Id`)
LEFT JOIN (
    SELECT `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`, `l2`.`Id` AS `Id0`, `l2`.`Level3_Optional_Id`, `l2`.`Level3_Required_Id`, `l2`.`Name` AS `Name0`, `l2`.`OneToMany_Optional_Inverse4Id`, `l2`.`OneToMany_Optional_Self_Inverse4Id`, `l2`.`OneToMany_Required_Inverse4Id`, `l2`.`OneToMany_Required_Self_Inverse4Id`, `l2`.`OneToOne_Optional_PK_Inverse4Id`, `l2`.`OneToOne_Optional_Self4Id`
    FROM `LevelThree` AS `l1`
    LEFT JOIN `LevelFour` AS `l2` ON `l1`.`Id` = `l2`.`Level3_Optional_Id`
) AS `s` ON `l0`.`Id` = `s`.`OneToMany_Optional_Inverse3Id`
ORDER BY `l`.`Id`, `l0`.`Id`, `s`.`Id`
""");
    }

    public override async Task Optional_navigation_with_order_by_and_Include(bool async)
    {
        await base.Optional_navigation_with_order_by_and_Include(async);

        AssertSql(
            """
SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l`.`Id`, `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`
FROM (`LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`Level1_Optional_Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Inverse3Id`
ORDER BY `l0`.`Name`, `l`.`Id`, `l0`.`Id`
""");
    }

    public override async Task Optional_navigation_with_Include_and_order(bool async)
    {
        await base.Optional_navigation_with_Include_and_order(async);

        AssertSql(
            """
SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l`.`Id`, `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`
FROM (`LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`Level1_Optional_Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Inverse3Id`
ORDER BY `l0`.`Name`, `l`.`Id`, `l0`.`Id`
""");
    }

    public override async Task SelectMany_with_order_by_and_Include(bool async)
    {
        await base.SelectMany_with_order_by_and_Include(async);

        AssertSql(
            """
SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l`.`Id`, `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`
FROM (`LevelOne` AS `l`
INNER JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Optional_Inverse2Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Inverse3Id`
ORDER BY `l0`.`Name`, `l`.`Id`, `l0`.`Id`
""");
    }

    public override async Task SelectMany_with_Include_and_order_by(bool async)
    {
        await base.SelectMany_with_Include_and_order_by(async);

        AssertSql(
            """
SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l`.`Id`, `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`
FROM (`LevelOne` AS `l`
INNER JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Optional_Inverse2Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Inverse3Id`
ORDER BY `l0`.`Name`, `l`.`Id`, `l0`.`Id`
""");
    }

    public override async Task SelectMany_with_navigation_and_Distinct(bool async)
    {
        await base.SelectMany_with_navigation_and_Distinct(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `l1`.`Id`, `l2`.`Id`, `l2`.`Date`, `l2`.`Level1_Optional_Id`, `l2`.`Level1_Required_Id`, `l2`.`Name`, `l2`.`OneToMany_Optional_Inverse2Id`, `l2`.`OneToMany_Optional_Self_Inverse2Id`, `l2`.`OneToMany_Required_Inverse2Id`, `l2`.`OneToMany_Required_Self_Inverse2Id`, `l2`.`OneToOne_Optional_PK_Inverse2Id`, `l2`.`OneToOne_Optional_Self2Id`
FROM (`LevelOne` AS `l`
INNER JOIN (
    SELECT DISTINCT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`
    FROM `LevelTwo` AS `l0`
) AS `l1` ON `l`.`Id` = `l1`.`OneToMany_Optional_Inverse2Id`)
LEFT JOIN `LevelTwo` AS `l2` ON `l`.`Id` = `l2`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`, `l1`.`Id`
""");
    }

    public override async Task SelectMany_with_navigation_and_Distinct_projecting_columns_including_join_key(bool async)
    {
        await base.SelectMany_with_navigation_and_Distinct_projecting_columns_including_join_key(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `l1`.`Id`, `l2`.`Id`, `l2`.`Date`, `l2`.`Level1_Optional_Id`, `l2`.`Level1_Required_Id`, `l2`.`Name`, `l2`.`OneToMany_Optional_Inverse2Id`, `l2`.`OneToMany_Optional_Self_Inverse2Id`, `l2`.`OneToMany_Required_Inverse2Id`, `l2`.`OneToMany_Required_Self_Inverse2Id`, `l2`.`OneToOne_Optional_PK_Inverse2Id`, `l2`.`OneToOne_Optional_Self2Id`
FROM (`LevelOne` AS `l`
INNER JOIN (
    SELECT DISTINCT `l0`.`Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id` AS `FK`
    FROM `LevelTwo` AS `l0`
) AS `l1` ON `l`.`Id` = `l1`.`FK`)
LEFT JOIN `LevelTwo` AS `l2` ON `l`.`Id` = `l2`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`, `l1`.`Id`
""");
    }

    public override async Task
        Complex_SelectMany_with_nested_navigations_and_explicit_DefaultIfEmpty_with_other_query_operators_composed_on_top(bool async)
    {
        await base.Complex_SelectMany_with_nested_navigations_and_explicit_DefaultIfEmpty_with_other_query_operators_composed_on_top(
            async);

        AssertSql(
            """
SELECT `l2`.`Id`, `l2`.`Level3_Optional_Id`, `l2`.`Level3_Required_Id`, `l2`.`Name`, `l2`.`OneToMany_Optional_Inverse4Id`, `l2`.`OneToMany_Optional_Self_Inverse4Id`, `l2`.`OneToMany_Required_Inverse4Id`, `l2`.`OneToMany_Required_Self_Inverse4Id`, `l2`.`OneToOne_Optional_PK_Inverse4Id`, `l2`.`OneToOne_Optional_Self4Id`, `l`.`Id`, `l0`.`Id`, `l1`.`Id`, `s`.`Id`, `s`.`Id0`, `s`.`Id1`, `s`.`Id2`, `s0`.`Id`, `s0`.`Id0`, `s0`.`Id1`, `s0`.`Id2`, `l11`.`Id`, `l12`.`Id`, `l13`.`Id`, `l14`.`Id`, `l16`.`Id`, `l16`.`Date`, `l16`.`Level1_Optional_Id`, `l16`.`Level1_Required_Id`, `l16`.`Name`, `l16`.`OneToMany_Optional_Inverse2Id`, `l16`.`OneToMany_Optional_Self_Inverse2Id`, `l16`.`OneToMany_Required_Inverse2Id`, `l16`.`OneToMany_Required_Self_Inverse2Id`, `l16`.`OneToOne_Optional_PK_Inverse2Id`, `l16`.`OneToOne_Optional_Self2Id`, `l14`.`Name`
FROM (((((((((`LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`Level1_Required_Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`Level2_Optional_Id`)
LEFT JOIN `LevelFour` AS `l2` ON `l1`.`Id` = `l2`.`OneToMany_Required_Inverse4Id`)
LEFT JOIN (
    SELECT `l3`.`Id`, `l4`.`Id` AS `Id0`, `l5`.`Id` AS `Id1`, `l6`.`Id` AS `Id2`
    FROM ((`LevelFour` AS `l3`
    INNER JOIN `LevelThree` AS `l4` ON `l3`.`Level3_Required_Id` = `l4`.`Id`)
    LEFT JOIN `LevelTwo` AS `l5` ON `l4`.`Level2_Optional_Id` = `l5`.`Id`)
    LEFT JOIN `LevelTwo` AS `l6` ON `l5`.`Id` = `l6`.`OneToMany_Required_Self_Inverse2Id`
) AS `s` ON `l2`.`Id` = `s`.`Id2`)
LEFT JOIN (
    SELECT `l7`.`Id`, `l8`.`Id` AS `Id0`, `l9`.`Id` AS `Id1`, `l10`.`Id` AS `Id2`, `l10`.`Level2_Optional_Id` AS `Level2_Optional_Id0`
    FROM ((`LevelFour` AS `l7`
    INNER JOIN `LevelThree` AS `l8` ON `l7`.`Level3_Required_Id` = `l8`.`Id`)
    LEFT JOIN `LevelTwo` AS `l9` ON `l8`.`Level2_Required_Id` = `l9`.`Id`)
    LEFT JOIN `LevelThree` AS `l10` ON `l9`.`Id` = `l10`.`OneToMany_Required_Inverse3Id`
    WHERE `l8`.`Level2_Required_Id` IS NOT NULL AND `l9`.`Id` IS NOT NULL
) AS `s0` ON `s`.`Id2` = `s0`.`Id2`)
LEFT JOIN `LevelThree` AS `l11` ON `l2`.`OneToMany_Optional_Inverse4Id` = `l11`.`Id`)
LEFT JOIN `LevelThree` AS `l12` ON `s`.`Id2` = `l12`.`Level2_Optional_Id`)
LEFT JOIN `LevelTwo` AS `l13` ON `s0`.`Level2_Optional_Id0` = `l13`.`Id`)
LEFT JOIN `LevelThree` AS `l14` ON `l13`.`Id` = `l14`.`Level2_Required_Id`)
LEFT JOIN (
    SELECT `l15`.`Id`, `l15`.`Date`, `l15`.`Level1_Optional_Id`, `l15`.`Level1_Required_Id`, `l15`.`Name`, `l15`.`OneToMany_Optional_Inverse2Id`, `l15`.`OneToMany_Optional_Self_Inverse2Id`, `l15`.`OneToMany_Required_Inverse2Id`, `l15`.`OneToMany_Required_Self_Inverse2Id`, `l15`.`OneToOne_Optional_PK_Inverse2Id`, `l15`.`OneToOne_Optional_Self2Id`
    FROM `LevelTwo` AS `l15`
    WHERE `l15`.`Id` <> 42
) AS `l16` ON `s`.`Id2` = `l16`.`OneToMany_Optional_Self_Inverse2Id`
WHERE (`l11`.`Name` <> 'Foo' OR `l11`.`Name` IS NULL) AND (`l2`.`Id` IS NOT NULL AND `s`.`Id2` IS NOT NULL)
ORDER BY `l12`.`Id`, `l`.`Id`, `l0`.`Id`, `l1`.`Id`, `l2`.`Id`, `s`.`Id`, `s`.`Id0`, `s`.`Id1`, `s`.`Id2`, `s0`.`Id`, `s0`.`Id0`, `s0`.`Id1`, `s0`.`Id2`, `l11`.`Id`, `l13`.`Id`, `l14`.`Id`
""");
    }

    public override async Task Project_collection_navigation(bool async)
    {
        await base.Project_collection_navigation(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`
FROM `LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`
""");
    }

    public override async Task Project_collection_navigation_nested(bool async)
    {
        await base.Project_collection_navigation_nested(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l0`.`Id`, `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`
FROM (`LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`Level1_Optional_Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Inverse3Id`
ORDER BY `l`.`Id`, `l0`.`Id`
""");
    }

    public override async Task Project_collection_navigation_using_ef_property(bool async)
    {
        await base.Project_collection_navigation_using_ef_property(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l0`.`Id`, `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`
FROM (`LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`Level1_Optional_Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Inverse3Id`
ORDER BY `l`.`Id`, `l0`.`Id`
""");
    }

    public override async Task Project_collection_navigation_nested_anonymous(bool async)
    {
        await base.Project_collection_navigation_nested_anonymous(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l0`.`Id`, `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`
FROM (`LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`Level1_Optional_Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Inverse3Id`
ORDER BY `l`.`Id`, `l0`.`Id`
""");
    }

    public override async Task Project_collection_navigation_composed(bool async)
    {
        await base.Project_collection_navigation_composed(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l1`.`Id`, `l1`.`Date`, `l1`.`Level1_Optional_Id`, `l1`.`Level1_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse2Id`, `l1`.`OneToMany_Optional_Self_Inverse2Id`, `l1`.`OneToMany_Required_Inverse2Id`, `l1`.`OneToMany_Required_Self_Inverse2Id`, `l1`.`OneToOne_Optional_PK_Inverse2Id`, `l1`.`OneToOne_Optional_Self2Id`
FROM `LevelOne` AS `l`
LEFT JOIN (
    SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`
    FROM `LevelTwo` AS `l0`
    WHERE `l0`.`Name` <> 'Foo' OR `l0`.`Name` IS NULL
) AS `l1` ON `l`.`Id` = `l1`.`OneToMany_Optional_Inverse2Id`
WHERE `l`.`Id` < 3
ORDER BY `l`.`Id`
""");
    }

    public override async Task Project_collection_and_root_entity(bool async)
    {
        await base.Project_collection_and_root_entity(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`
FROM `LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`
""");
    }

    public override async Task Project_collection_and_include(bool async)
    {
        await base.Project_collection_and_include(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id`, `l1`.`Date`, `l1`.`Level1_Optional_Id`, `l1`.`Level1_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse2Id`, `l1`.`OneToMany_Optional_Self_Inverse2Id`, `l1`.`OneToMany_Required_Inverse2Id`, `l1`.`OneToMany_Required_Self_Inverse2Id`, `l1`.`OneToOne_Optional_PK_Inverse2Id`, `l1`.`OneToOne_Optional_Self2Id`
FROM (`LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Optional_Inverse2Id`)
LEFT JOIN `LevelTwo` AS `l1` ON `l`.`Id` = `l1`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`, `l0`.`Id`
""");
    }

    public override async Task Project_navigation_and_collection(bool async)
    {
        await base.Project_navigation_and_collection(async);

        AssertSql(
            """
SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l`.`Id`, `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`
FROM (`LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`Level1_Optional_Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Inverse3Id`
ORDER BY `l`.`Id`, `l0`.`Id`
""");
    }

    public override async Task Include_inside_subquery(bool async)
    {
        await base.Include_inside_subquery(async);

        AssertSql(
"""
SELECT [l].[Id], [t].[Id], [t].[Date], [t].[Level1_Optional_Id], [t].[Level1_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse2Id], [t].[OneToMany_Optional_Self_Inverse2Id], [t].[OneToMany_Required_Inverse2Id], [t].[OneToMany_Required_Self_Inverse2Id], [t].[OneToOne_Optional_PK_Inverse2Id], [t].[OneToOne_Optional_Self2Id], [t].[Id0], [t].[Level2_Optional_Id], [t].[Level2_Required_Id], [t].[Name0], [t].[OneToMany_Optional_Inverse3Id], [t].[OneToMany_Optional_Self_Inverse3Id], [t].[OneToMany_Required_Inverse3Id], [t].[OneToMany_Required_Self_Inverse3Id], [t].[OneToOne_Optional_PK_Inverse3Id], [t].[OneToOne_Optional_Self3Id]
FROM [LevelOne] AS [l]
OUTER APPLY (
    SELECT [l0].[Id], [l0].[Date], [l0].[Level1_Optional_Id], [l0].[Level1_Required_Id], [l0].[Name], [l0].[OneToMany_Optional_Inverse2Id], [l0].[OneToMany_Optional_Self_Inverse2Id], [l0].[OneToMany_Required_Inverse2Id], [l0].[OneToMany_Required_Self_Inverse2Id], [l0].[OneToOne_Optional_PK_Inverse2Id], [l0].[OneToOne_Optional_Self2Id], [l1].[Id] AS [Id0], [l1].[Level2_Optional_Id], [l1].[Level2_Required_Id], [l1].[Name] AS [Name0], [l1].[OneToMany_Optional_Inverse3Id], [l1].[OneToMany_Optional_Self_Inverse3Id], [l1].[OneToMany_Required_Inverse3Id], [l1].[OneToMany_Required_Self_Inverse3Id], [l1].[OneToOne_Optional_PK_Inverse3Id], [l1].[OneToOne_Optional_Self3Id]
    FROM [LevelTwo] AS [l0]
    LEFT JOIN [LevelThree] AS [l1] ON [l0].[Id] = [l1].[OneToMany_Optional_Inverse3Id]
    WHERE [l0].[Id] > 0
) AS [t]
WHERE [l].[Id] < 3
ORDER BY [l].[Id], [t].[Id]
""");
    }

    public override async Task Include_collection_with_multiple_orderbys_member(bool async)
    {
        await base.Include_collection_with_multiple_orderbys_member(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Level1_Optional_Id`, `l`.`Level1_Required_Id`, `l`.`Name`, `l`.`OneToMany_Optional_Inverse2Id`, `l`.`OneToMany_Optional_Self_Inverse2Id`, `l`.`OneToMany_Required_Inverse2Id`, `l`.`OneToMany_Required_Self_Inverse2Id`, `l`.`OneToOne_Optional_PK_Inverse2Id`, `l`.`OneToOne_Optional_Self2Id`, `l0`.`Id`, `l0`.`Level2_Optional_Id`, `l0`.`Level2_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse3Id`, `l0`.`OneToMany_Optional_Self_Inverse3Id`, `l0`.`OneToMany_Required_Inverse3Id`, `l0`.`OneToMany_Required_Self_Inverse3Id`, `l0`.`OneToOne_Optional_PK_Inverse3Id`, `l0`.`OneToOne_Optional_Self3Id`
FROM `LevelTwo` AS `l`
LEFT JOIN `LevelThree` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Optional_Inverse3Id`
ORDER BY `l`.`Name`, `l`.`Level1_Required_Id`, `l`.`Id`
""");
    }

    public override async Task Include_collection_with_multiple_orderbys_property(bool async)
    {
        await base.Include_collection_with_multiple_orderbys_property(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Level1_Optional_Id`, `l`.`Level1_Required_Id`, `l`.`Name`, `l`.`OneToMany_Optional_Inverse2Id`, `l`.`OneToMany_Optional_Self_Inverse2Id`, `l`.`OneToMany_Required_Inverse2Id`, `l`.`OneToMany_Required_Self_Inverse2Id`, `l`.`OneToOne_Optional_PK_Inverse2Id`, `l`.`OneToOne_Optional_Self2Id`, `l0`.`Id`, `l0`.`Level2_Optional_Id`, `l0`.`Level2_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse3Id`, `l0`.`OneToMany_Optional_Self_Inverse3Id`, `l0`.`OneToMany_Required_Inverse3Id`, `l0`.`OneToMany_Required_Self_Inverse3Id`, `l0`.`OneToOne_Optional_PK_Inverse3Id`, `l0`.`OneToOne_Optional_Self3Id`
FROM `LevelTwo` AS `l`
LEFT JOIN `LevelThree` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Optional_Inverse3Id`
ORDER BY `l`.`Level1_Required_Id`, `l`.`Name`, `l`.`Id`
""");
    }

    public override async Task Include_collection_with_multiple_orderbys_methodcall(bool async)
    {
        await base.Include_collection_with_multiple_orderbys_methodcall(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Level1_Optional_Id`, `l`.`Level1_Required_Id`, `l`.`Name`, `l`.`OneToMany_Optional_Inverse2Id`, `l`.`OneToMany_Optional_Self_Inverse2Id`, `l`.`OneToMany_Required_Inverse2Id`, `l`.`OneToMany_Required_Self_Inverse2Id`, `l`.`OneToOne_Optional_PK_Inverse2Id`, `l`.`OneToOne_Optional_Self2Id`, `l0`.`Id`, `l0`.`Level2_Optional_Id`, `l0`.`Level2_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse3Id`, `l0`.`OneToMany_Optional_Self_Inverse3Id`, `l0`.`OneToMany_Required_Inverse3Id`, `l0`.`OneToMany_Required_Self_Inverse3Id`, `l0`.`OneToOne_Optional_PK_Inverse3Id`, `l0`.`OneToOne_Optional_Self3Id`
FROM `LevelTwo` AS `l`
LEFT JOIN `LevelThree` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Optional_Inverse3Id`
ORDER BY ABS(`l`.`Level1_Required_Id`), `l`.`Name`, `l`.`Id`
""");
    }

    public override async Task Include_collection_with_multiple_orderbys_complex(bool async)
    {
        await base.Include_collection_with_multiple_orderbys_complex(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Level1_Optional_Id`, `l`.`Level1_Required_Id`, `l`.`Name`, `l`.`OneToMany_Optional_Inverse2Id`, `l`.`OneToMany_Optional_Self_Inverse2Id`, `l`.`OneToMany_Required_Inverse2Id`, `l`.`OneToMany_Required_Self_Inverse2Id`, `l`.`OneToOne_Optional_PK_Inverse2Id`, `l`.`OneToOne_Optional_Self2Id`, `l0`.`Id`, `l0`.`Level2_Optional_Id`, `l0`.`Level2_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse3Id`, `l0`.`OneToMany_Optional_Self_Inverse3Id`, `l0`.`OneToMany_Required_Inverse3Id`, `l0`.`OneToMany_Required_Self_Inverse3Id`, `l0`.`OneToOne_Optional_PK_Inverse3Id`, `l0`.`OneToOne_Optional_Self3Id`
FROM `LevelTwo` AS `l`
LEFT JOIN `LevelThree` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Optional_Inverse3Id`
ORDER BY ABS(`l`.`Level1_Required_Id`) + 7, `l`.`Name`, `l`.`Id`
""");
    }

    public override async Task Include_collection_with_multiple_orderbys_complex_repeated(bool async)
    {
        await base.Include_collection_with_multiple_orderbys_complex_repeated(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Level1_Optional_Id`, `l`.`Level1_Required_Id`, `l`.`Name`, `l`.`OneToMany_Optional_Inverse2Id`, `l`.`OneToMany_Optional_Self_Inverse2Id`, `l`.`OneToMany_Required_Inverse2Id`, `l`.`OneToMany_Required_Self_Inverse2Id`, `l`.`OneToOne_Optional_PK_Inverse2Id`, `l`.`OneToOne_Optional_Self2Id`, `l0`.`Id`, `l0`.`Level2_Optional_Id`, `l0`.`Level2_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse3Id`, `l0`.`OneToMany_Optional_Self_Inverse3Id`, `l0`.`OneToMany_Required_Inverse3Id`, `l0`.`OneToMany_Required_Self_Inverse3Id`, `l0`.`OneToOne_Optional_PK_Inverse3Id`, `l0`.`OneToOne_Optional_Self3Id`
FROM `LevelTwo` AS `l`
LEFT JOIN `LevelThree` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Optional_Inverse3Id`
ORDER BY -`l`.`Level1_Required_Id`, `l`.`Name`, `l`.`Id`
""");
    }

    public override async Task Include_collection_with_groupby_in_subquery(bool async)
    {
        await base.Include_collection_with_groupby_in_subquery(async);

        AssertSql(
"""
SELECT [t0].[Id], [t0].[Date], [t0].[Name], [t0].[OneToMany_Optional_Self_Inverse1Id], [t0].[OneToMany_Required_Self_Inverse1Id], [t0].[OneToOne_Optional_Self1Id], [t].[Name], [l1].[Id], [l1].[Date], [l1].[Level1_Optional_Id], [l1].[Level1_Required_Id], [l1].[Name], [l1].[OneToMany_Optional_Inverse2Id], [l1].[OneToMany_Optional_Self_Inverse2Id], [l1].[OneToMany_Required_Inverse2Id], [l1].[OneToMany_Required_Self_Inverse2Id], [l1].[OneToOne_Optional_PK_Inverse2Id], [l1].[OneToOne_Optional_Self2Id]
FROM (
    SELECT [l].[Name]
    FROM [LevelOne] AS [l]
    GROUP BY [l].[Name]
) AS [t]
LEFT JOIN (
    SELECT [t1].[Id], [t1].[Date], [t1].[Name], [t1].[OneToMany_Optional_Self_Inverse1Id], [t1].[OneToMany_Required_Self_Inverse1Id], [t1].[OneToOne_Optional_Self1Id]
    FROM (
        SELECT [l0].[Id], [l0].[Date], [l0].[Name], [l0].[OneToMany_Optional_Self_Inverse1Id], [l0].[OneToMany_Required_Self_Inverse1Id], [l0].[OneToOne_Optional_Self1Id], ROW_NUMBER() OVER(PARTITION BY [l0].[Name] ORDER BY [l0].[Id]) AS [row]
        FROM [LevelOne] AS [l0]
    ) AS [t1]
    WHERE [t1].[row] <= 1
) AS [t0] ON [t].[Name] = [t0].[Name]
LEFT JOIN [LevelTwo] AS [l1] ON [t0].[Id] = [l1].[OneToMany_Optional_Inverse2Id]
ORDER BY [t].[Name], [t0].[Id]
""");
    }

    public override async Task Include_collection_with_groupby_in_subquery_and_filter_before_groupby(bool async)
    {
        await base.Include_collection_with_groupby_in_subquery_and_filter_before_groupby(async);

        AssertSql(
"""
SELECT [t0].[Id], [t0].[Date], [t0].[Name], [t0].[OneToMany_Optional_Self_Inverse1Id], [t0].[OneToMany_Required_Self_Inverse1Id], [t0].[OneToOne_Optional_Self1Id], [t].[Name], [l1].[Id], [l1].[Date], [l1].[Level1_Optional_Id], [l1].[Level1_Required_Id], [l1].[Name], [l1].[OneToMany_Optional_Inverse2Id], [l1].[OneToMany_Optional_Self_Inverse2Id], [l1].[OneToMany_Required_Inverse2Id], [l1].[OneToMany_Required_Self_Inverse2Id], [l1].[OneToOne_Optional_PK_Inverse2Id], [l1].[OneToOne_Optional_Self2Id]
FROM (
    SELECT [l].[Name]
    FROM [LevelOne] AS [l]
    WHERE [l].[Id] > 3
    GROUP BY [l].[Name]
) AS [t]
LEFT JOIN (
    SELECT [t1].[Id], [t1].[Date], [t1].[Name], [t1].[OneToMany_Optional_Self_Inverse1Id], [t1].[OneToMany_Required_Self_Inverse1Id], [t1].[OneToOne_Optional_Self1Id]
    FROM (
        SELECT [l0].[Id], [l0].[Date], [l0].[Name], [l0].[OneToMany_Optional_Self_Inverse1Id], [l0].[OneToMany_Required_Self_Inverse1Id], [l0].[OneToOne_Optional_Self1Id], ROW_NUMBER() OVER(PARTITION BY [l0].[Name] ORDER BY [l0].[Id]) AS [row]
        FROM [LevelOne] AS [l0]
        WHERE [l0].[Id] > 3
    ) AS [t1]
    WHERE [t1].[row] <= 1
) AS [t0] ON [t].[Name] = [t0].[Name]
LEFT JOIN [LevelTwo] AS [l1] ON [t0].[Id] = [l1].[OneToMany_Optional_Inverse2Id]
ORDER BY [t].[Name], [t0].[Id]
""");
    }

    public override async Task Include_collection_with_groupby_in_subquery_and_filter_after_groupby(bool async)
    {
        await base.Include_collection_with_groupby_in_subquery_and_filter_after_groupby(async);

        AssertSql(
"""
SELECT [t0].[Id], [t0].[Date], [t0].[Name], [t0].[OneToMany_Optional_Self_Inverse1Id], [t0].[OneToMany_Required_Self_Inverse1Id], [t0].[OneToOne_Optional_Self1Id], [t].[Name], [l1].[Id], [l1].[Date], [l1].[Level1_Optional_Id], [l1].[Level1_Required_Id], [l1].[Name], [l1].[OneToMany_Optional_Inverse2Id], [l1].[OneToMany_Optional_Self_Inverse2Id], [l1].[OneToMany_Required_Inverse2Id], [l1].[OneToMany_Required_Self_Inverse2Id], [l1].[OneToOne_Optional_PK_Inverse2Id], [l1].[OneToOne_Optional_Self2Id]
FROM (
    SELECT [l].[Name]
    FROM [LevelOne] AS [l]
    GROUP BY [l].[Name]
    HAVING [l].[Name] <> N'Foo' OR ([l].[Name] IS NULL)
) AS [t]
LEFT JOIN (
    SELECT [t1].[Id], [t1].[Date], [t1].[Name], [t1].[OneToMany_Optional_Self_Inverse1Id], [t1].[OneToMany_Required_Self_Inverse1Id], [t1].[OneToOne_Optional_Self1Id]
    FROM (
        SELECT [l0].[Id], [l0].[Date], [l0].[Name], [l0].[OneToMany_Optional_Self_Inverse1Id], [l0].[OneToMany_Required_Self_Inverse1Id], [l0].[OneToOne_Optional_Self1Id], ROW_NUMBER() OVER(PARTITION BY [l0].[Name] ORDER BY [l0].[Id]) AS [row]
        FROM [LevelOne] AS [l0]
    ) AS [t1]
    WHERE [t1].[row] <= 1
) AS [t0] ON [t].[Name] = [t0].[Name]
LEFT JOIN [LevelTwo] AS [l1] ON [t0].[Id] = [l1].[OneToMany_Optional_Inverse2Id]
ORDER BY [t].[Name], [t0].[Id]
""");
    }

    public override async Task Include_reference_collection_order_by_reference_navigation(bool async)
    {
        await base.Include_reference_collection_order_by_reference_navigation(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`
FROM (`LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`Level1_Optional_Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Inverse3Id`
ORDER BY `l0`.`Id`, `l`.`Id`
""");
    }

    public override async Task Include_after_SelectMany(bool async)
    {
        await base.Include_after_SelectMany(async);

        AssertSql(
            """
SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l`.`Id`, `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`
FROM (`LevelOne` AS `l`
INNER JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Required_Inverse2Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Inverse3Id`
ORDER BY `l`.`Id`, `l0`.`Id`
""");
    }

    public override async Task Include_after_multiple_SelectMany_and_reference_navigation(bool async)
    {
        await base.Include_after_multiple_SelectMany_and_reference_navigation(async);

        AssertSql(
            """
SELECT `l2`.`Id`, `l2`.`Level3_Optional_Id`, `l2`.`Level3_Required_Id`, `l2`.`Name`, `l2`.`OneToMany_Optional_Inverse4Id`, `l2`.`OneToMany_Optional_Self_Inverse4Id`, `l2`.`OneToMany_Required_Inverse4Id`, `l2`.`OneToMany_Required_Self_Inverse4Id`, `l2`.`OneToOne_Optional_PK_Inverse4Id`, `l2`.`OneToOne_Optional_Self4Id`, `l`.`Id`, `l0`.`Id`, `l1`.`Id`, `l3`.`Id`, `l3`.`Level3_Optional_Id`, `l3`.`Level3_Required_Id`, `l3`.`Name`, `l3`.`OneToMany_Optional_Inverse4Id`, `l3`.`OneToMany_Optional_Self_Inverse4Id`, `l3`.`OneToMany_Required_Inverse4Id`, `l3`.`OneToMany_Required_Self_Inverse4Id`, `l3`.`OneToOne_Optional_PK_Inverse4Id`, `l3`.`OneToOne_Optional_Self4Id`
FROM (((`LevelOne` AS `l`
INNER JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Required_Inverse2Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Inverse3Id`)
LEFT JOIN `LevelFour` AS `l2` ON `l1`.`Id` = `l2`.`Level3_Required_Id`)
LEFT JOIN `LevelFour` AS `l3` ON `l2`.`Id` = `l3`.`OneToMany_Required_Self_Inverse4Id`
WHERE `l0`.`Id` IS NOT NULL AND `l1`.`OneToMany_Optional_Inverse3Id` IS NOT NULL
ORDER BY `l`.`Id`, `l0`.`Id`, `l1`.`Id`, `l2`.`Id`
""");
    }

    public override async Task Include_after_SelectMany_and_multiple_reference_navigations(bool async)
    {
        await base.Include_after_SelectMany_and_multiple_reference_navigations(async);

        AssertSql(
            """
SELECT `l2`.`Id`, `l2`.`Level3_Optional_Id`, `l2`.`Level3_Required_Id`, `l2`.`Name`, `l2`.`OneToMany_Optional_Inverse4Id`, `l2`.`OneToMany_Optional_Self_Inverse4Id`, `l2`.`OneToMany_Required_Inverse4Id`, `l2`.`OneToMany_Required_Self_Inverse4Id`, `l2`.`OneToOne_Optional_PK_Inverse4Id`, `l2`.`OneToOne_Optional_Self4Id`, `l`.`Id`, `l0`.`Id`, `l1`.`Id`, `l3`.`Id`, `l3`.`Level3_Optional_Id`, `l3`.`Level3_Required_Id`, `l3`.`Name`, `l3`.`OneToMany_Optional_Inverse4Id`, `l3`.`OneToMany_Optional_Self_Inverse4Id`, `l3`.`OneToMany_Required_Inverse4Id`, `l3`.`OneToMany_Required_Self_Inverse4Id`, `l3`.`OneToOne_Optional_PK_Inverse4Id`, `l3`.`OneToOne_Optional_Self4Id`
FROM (((`LevelOne` AS `l`
INNER JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Required_Inverse2Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`Level2_Optional_Id`)
LEFT JOIN `LevelFour` AS `l2` ON `l1`.`Id` = `l2`.`Level3_Required_Id`)
LEFT JOIN `LevelFour` AS `l3` ON `l2`.`Id` = `l3`.`OneToMany_Optional_Self_Inverse4Id`
ORDER BY `l`.`Id`, `l0`.`Id`, `l1`.`Id`, `l2`.`Id`
""");
    }

    public override async Task Null_check_in_anonymous_type_projection_should_not_be_removed(bool async)
    {
        await base.Null_check_in_anonymous_type_projection_should_not_be_removed(async);

        AssertSql(
            """
SELECT `l`.`Id`, `s`.`c`, `s`.`Name`, `s`.`Id`, `s`.`Id0`
FROM `LevelOne` AS `l`
LEFT JOIN (
    SELECT IIF(`l1`.`Id` IS NULL, TRUE, FALSE) AS `c`, `l1`.`Name`, `l0`.`Id`, `l1`.`Id` AS `Id0`, `l0`.`OneToMany_Optional_Inverse2Id`
    FROM `LevelTwo` AS `l0`
    LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`Level2_Required_Id`
) AS `s` ON `l`.`Id` = `s`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`, `s`.`Id`
""");
    }

    public override async Task Null_check_in_Dto_projection_should_not_be_removed(bool async)
    {
        await base.Null_check_in_Dto_projection_should_not_be_removed(async);

        AssertSql(
            """
SELECT `l`.`Id`, `s`.`c`, `s`.`Name`, `s`.`Id`, `s`.`Id0`
FROM `LevelOne` AS `l`
LEFT JOIN (
    SELECT IIF(`l1`.`Id` IS NULL, TRUE, FALSE) AS `c`, `l1`.`Name`, `l0`.`Id`, `l1`.`Id` AS `Id0`, `l0`.`OneToMany_Optional_Inverse2Id`
    FROM `LevelTwo` AS `l0`
    LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`Level2_Required_Id`
) AS `s` ON `l`.`Id` = `s`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`, `s`.`Id`
""");
    }

    public override async Task SelectMany_navigation_property_followed_by_select_collection_navigation(bool async)
    {
        await base.SelectMany_navigation_property_followed_by_select_collection_navigation(async);

        AssertSql(
            """
SELECT `l0`.`Id`, `l`.`Id`, `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`
FROM (`LevelOne` AS `l`
INNER JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Optional_Inverse2Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Inverse3Id`
ORDER BY `l`.`Id`, `l0`.`Id`
""");
    }

    public override async Task Multiple_SelectMany_navigation_property_followed_by_select_collection_navigation(bool async)
    {
        await base.Multiple_SelectMany_navigation_property_followed_by_select_collection_navigation(async);

        AssertSql(
            """
SELECT `l1`.`Id`, `l`.`Id`, `l0`.`Id`, `l2`.`Id`, `l2`.`Level3_Optional_Id`, `l2`.`Level3_Required_Id`, `l2`.`Name`, `l2`.`OneToMany_Optional_Inverse4Id`, `l2`.`OneToMany_Optional_Self_Inverse4Id`, `l2`.`OneToMany_Required_Inverse4Id`, `l2`.`OneToMany_Required_Self_Inverse4Id`, `l2`.`OneToOne_Optional_PK_Inverse4Id`, `l2`.`OneToOne_Optional_Self4Id`
FROM ((`LevelOne` AS `l`
INNER JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Optional_Inverse2Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Inverse3Id`)
LEFT JOIN `LevelFour` AS `l2` ON `l1`.`Id` = `l2`.`OneToMany_Optional_Inverse4Id`
WHERE `l0`.`Id` IS NOT NULL AND `l1`.`OneToMany_Optional_Inverse3Id` IS NOT NULL
ORDER BY `l`.`Id`, `l0`.`Id`, `l1`.`Id`
""");
    }

    public override async Task SelectMany_navigation_property_with_include_and_followed_by_select_collection_navigation(bool async)
    {
        await base.SelectMany_navigation_property_with_include_and_followed_by_select_collection_navigation(async);

        AssertSql(
            """
SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l`.`Id`, `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`, `l2`.`Id`, `l2`.`Level2_Optional_Id`, `l2`.`Level2_Required_Id`, `l2`.`Name`, `l2`.`OneToMany_Optional_Inverse3Id`, `l2`.`OneToMany_Optional_Self_Inverse3Id`, `l2`.`OneToMany_Required_Inverse3Id`, `l2`.`OneToMany_Required_Self_Inverse3Id`, `l2`.`OneToOne_Optional_PK_Inverse3Id`, `l2`.`OneToOne_Optional_Self3Id`
FROM ((`LevelOne` AS `l`
INNER JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Optional_Inverse2Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Required_Inverse3Id`)
LEFT JOIN `LevelThree` AS `l2` ON `l0`.`Id` = `l2`.`OneToMany_Optional_Inverse3Id`
ORDER BY `l`.`Id`, `l0`.`Id`, `l1`.`Id`
""");
    }

    public override async Task Include_collection(bool async)
    {
        await base.Include_collection(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`
FROM `LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`
""");
    }

    public override async Task Include_collection_followed_by_include_reference(bool async)
    {
        await base.Include_collection_followed_by_include_reference(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `s`.`Id`, `s`.`Date`, `s`.`Level1_Optional_Id`, `s`.`Level1_Required_Id`, `s`.`Name`, `s`.`OneToMany_Optional_Inverse2Id`, `s`.`OneToMany_Optional_Self_Inverse2Id`, `s`.`OneToMany_Required_Inverse2Id`, `s`.`OneToMany_Required_Self_Inverse2Id`, `s`.`OneToOne_Optional_PK_Inverse2Id`, `s`.`OneToOne_Optional_Self2Id`, `s`.`Id0`, `s`.`Level2_Optional_Id`, `s`.`Level2_Required_Id`, `s`.`Name0`, `s`.`OneToMany_Optional_Inverse3Id`, `s`.`OneToMany_Optional_Self_Inverse3Id`, `s`.`OneToMany_Required_Inverse3Id`, `s`.`OneToMany_Required_Self_Inverse3Id`, `s`.`OneToOne_Optional_PK_Inverse3Id`, `s`.`OneToOne_Optional_Self3Id`
FROM `LevelOne` AS `l`
LEFT JOIN (
    SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id` AS `Id0`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name` AS `Name0`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`
    FROM `LevelTwo` AS `l0`
    LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToOne_Optional_PK_Inverse3Id`
) AS `s` ON `l`.`Id` = `s`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`, `s`.`Id`
""");
    }

    public override async Task Include_reference_followed_by_include_collection(bool async)
    {
        await base.Include_reference_followed_by_include_collection(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`
FROM (`LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`Level1_Optional_Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Inverse3Id`
ORDER BY `l`.`Id`, `l0`.`Id`
""");
    }

    public override async Task Include_collection_followed_by_projecting_the_included_collection(bool async)
    {
        await base.Include_collection_followed_by_projecting_the_included_collection(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`
FROM `LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`
""");
    }

    public override async Task Include_and_ThenInclude_collections_followed_by_projecting_the_first_collection(bool async)
    {
        await base.Include_and_ThenInclude_collections_followed_by_projecting_the_first_collection(async);

        AssertSql(
            """
SELECT `l`.`Id`, `s`.`Id`, `s`.`Date`, `s`.`Level1_Optional_Id`, `s`.`Level1_Required_Id`, `s`.`Name`, `s`.`OneToMany_Optional_Inverse2Id`, `s`.`OneToMany_Optional_Self_Inverse2Id`, `s`.`OneToMany_Required_Inverse2Id`, `s`.`OneToMany_Required_Self_Inverse2Id`, `s`.`OneToOne_Optional_PK_Inverse2Id`, `s`.`OneToOne_Optional_Self2Id`, `s`.`Id0`, `s`.`Level2_Optional_Id`, `s`.`Level2_Required_Id`, `s`.`Name0`, `s`.`OneToMany_Optional_Inverse3Id`, `s`.`OneToMany_Optional_Self_Inverse3Id`, `s`.`OneToMany_Required_Inverse3Id`, `s`.`OneToMany_Required_Self_Inverse3Id`, `s`.`OneToOne_Optional_PK_Inverse3Id`, `s`.`OneToOne_Optional_Self3Id`
FROM `LevelOne` AS `l`
LEFT JOIN (
    SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id` AS `Id0`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name` AS `Name0`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`
    FROM `LevelTwo` AS `l0`
    LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToOne_Optional_PK_Inverse3Id`
) AS `s` ON `l`.`Id` = `s`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`, `s`.`Id`
""");
    }

    public override async Task Include_collection_and_another_navigation_chain_followed_by_projecting_the_first_collection(bool async)
    {
        await base.Include_collection_and_another_navigation_chain_followed_by_projecting_the_first_collection(async);

        AssertSql(
            """
SELECT `l`.`Id`, `s`.`Id`, `s`.`Date`, `s`.`Level1_Optional_Id`, `s`.`Level1_Required_Id`, `s`.`Name`, `s`.`OneToMany_Optional_Inverse2Id`, `s`.`OneToMany_Optional_Self_Inverse2Id`, `s`.`OneToMany_Required_Inverse2Id`, `s`.`OneToMany_Required_Self_Inverse2Id`, `s`.`OneToOne_Optional_PK_Inverse2Id`, `s`.`OneToOne_Optional_Self2Id`, `s`.`Id0`, `s`.`Level2_Optional_Id`, `s`.`Level2_Required_Id`, `s`.`Name0`, `s`.`OneToMany_Optional_Inverse3Id`, `s`.`OneToMany_Optional_Self_Inverse3Id`, `s`.`OneToMany_Required_Inverse3Id`, `s`.`OneToMany_Required_Self_Inverse3Id`, `s`.`OneToOne_Optional_PK_Inverse3Id`, `s`.`OneToOne_Optional_Self3Id`, `s`.`Id1`, `s`.`Level3_Optional_Id`, `s`.`Level3_Required_Id`, `s`.`Name1`, `s`.`OneToMany_Optional_Inverse4Id`, `s`.`OneToMany_Optional_Self_Inverse4Id`, `s`.`OneToMany_Required_Inverse4Id`, `s`.`OneToMany_Required_Self_Inverse4Id`, `s`.`OneToOne_Optional_PK_Inverse4Id`, `s`.`OneToOne_Optional_Self4Id`
FROM `LevelOne` AS `l`
LEFT JOIN (
    SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id` AS `Id0`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name` AS `Name0`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`, `l2`.`Id` AS `Id1`, `l2`.`Level3_Optional_Id`, `l2`.`Level3_Required_Id`, `l2`.`Name` AS `Name1`, `l2`.`OneToMany_Optional_Inverse4Id`, `l2`.`OneToMany_Optional_Self_Inverse4Id`, `l2`.`OneToMany_Required_Inverse4Id`, `l2`.`OneToMany_Required_Self_Inverse4Id`, `l2`.`OneToOne_Optional_PK_Inverse4Id`, `l2`.`OneToOne_Optional_Self4Id`
    FROM (`LevelTwo` AS `l0`
    LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToOne_Optional_PK_Inverse3Id`)
    LEFT JOIN `LevelFour` AS `l2` ON `l1`.`Id` = `l2`.`Level3_Optional_Id`
) AS `s` ON `l`.`Id` = `s`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`, `s`.`Id`, `s`.`Id0`
""");
    }

    public override async Task Include_collection_ThenInclude_two_references(bool async)
    {
        await base.Include_collection_ThenInclude_two_references(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `s`.`Id`, `s`.`Date`, `s`.`Level1_Optional_Id`, `s`.`Level1_Required_Id`, `s`.`Name`, `s`.`OneToMany_Optional_Inverse2Id`, `s`.`OneToMany_Optional_Self_Inverse2Id`, `s`.`OneToMany_Required_Inverse2Id`, `s`.`OneToMany_Required_Self_Inverse2Id`, `s`.`OneToOne_Optional_PK_Inverse2Id`, `s`.`OneToOne_Optional_Self2Id`, `s`.`Id0`, `s`.`Level2_Optional_Id`, `s`.`Level2_Required_Id`, `s`.`Name0`, `s`.`OneToMany_Optional_Inverse3Id`, `s`.`OneToMany_Optional_Self_Inverse3Id`, `s`.`OneToMany_Required_Inverse3Id`, `s`.`OneToMany_Required_Self_Inverse3Id`, `s`.`OneToOne_Optional_PK_Inverse3Id`, `s`.`OneToOne_Optional_Self3Id`, `s`.`Id1`, `s`.`Level3_Optional_Id`, `s`.`Level3_Required_Id`, `s`.`Name1`, `s`.`OneToMany_Optional_Inverse4Id`, `s`.`OneToMany_Optional_Self_Inverse4Id`, `s`.`OneToMany_Required_Inverse4Id`, `s`.`OneToMany_Required_Self_Inverse4Id`, `s`.`OneToOne_Optional_PK_Inverse4Id`, `s`.`OneToOne_Optional_Self4Id`
FROM `LevelOne` AS `l`
LEFT JOIN (
    SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id` AS `Id0`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name` AS `Name0`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`, `l2`.`Id` AS `Id1`, `l2`.`Level3_Optional_Id`, `l2`.`Level3_Required_Id`, `l2`.`Name` AS `Name1`, `l2`.`OneToMany_Optional_Inverse4Id`, `l2`.`OneToMany_Optional_Self_Inverse4Id`, `l2`.`OneToMany_Required_Inverse4Id`, `l2`.`OneToMany_Required_Self_Inverse4Id`, `l2`.`OneToOne_Optional_PK_Inverse4Id`, `l2`.`OneToOne_Optional_Self4Id`
    FROM (`LevelTwo` AS `l0`
    LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToOne_Optional_PK_Inverse3Id`)
    LEFT JOIN `LevelFour` AS `l2` ON `l1`.`Id` = `l2`.`Level3_Optional_Id`
) AS `s` ON `l`.`Id` = `s`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`, `s`.`Id`, `s`.`Id0`
""");
    }

    public override async Task Include_collection_followed_by_complex_includes_and_projecting_the_included_collection(bool async)
    {
        await base.Include_collection_followed_by_complex_includes_and_projecting_the_included_collection(async);

        AssertSql(
            """
SELECT `l`.`Id`, `s`.`Id`, `s`.`Date`, `s`.`Level1_Optional_Id`, `s`.`Level1_Required_Id`, `s`.`Name`, `s`.`OneToMany_Optional_Inverse2Id`, `s`.`OneToMany_Optional_Self_Inverse2Id`, `s`.`OneToMany_Required_Inverse2Id`, `s`.`OneToMany_Required_Self_Inverse2Id`, `s`.`OneToOne_Optional_PK_Inverse2Id`, `s`.`OneToOne_Optional_Self2Id`, `s`.`Id0`, `s`.`Level2_Optional_Id`, `s`.`Level2_Required_Id`, `s`.`Name0`, `s`.`OneToMany_Optional_Inverse3Id`, `s`.`OneToMany_Optional_Self_Inverse3Id`, `s`.`OneToMany_Required_Inverse3Id`, `s`.`OneToMany_Required_Self_Inverse3Id`, `s`.`OneToOne_Optional_PK_Inverse3Id`, `s`.`OneToOne_Optional_Self3Id`, `s`.`Id1`, `s`.`Level3_Optional_Id`, `s`.`Level3_Required_Id`, `s`.`Name1`, `s`.`OneToMany_Optional_Inverse4Id`, `s`.`OneToMany_Optional_Self_Inverse4Id`, `s`.`OneToMany_Required_Inverse4Id`, `s`.`OneToMany_Required_Self_Inverse4Id`, `s`.`OneToOne_Optional_PK_Inverse4Id`, `s`.`OneToOne_Optional_Self4Id`, `s`.`Id2`, `s`.`Level2_Optional_Id0`, `s`.`Level2_Required_Id0`, `s`.`Name2`, `s`.`OneToMany_Optional_Inverse3Id0`, `s`.`OneToMany_Optional_Self_Inverse3Id0`, `s`.`OneToMany_Required_Inverse3Id0`, `s`.`OneToMany_Required_Self_Inverse3Id0`, `s`.`OneToOne_Optional_PK_Inverse3Id0`, `s`.`OneToOne_Optional_Self3Id0`, `s`.`Id3`, `s`.`Level3_Optional_Id0`, `s`.`Level3_Required_Id0`, `s`.`Name3`, `s`.`OneToMany_Optional_Inverse4Id0`, `s`.`OneToMany_Optional_Self_Inverse4Id0`, `s`.`OneToMany_Required_Inverse4Id0`, `s`.`OneToMany_Required_Self_Inverse4Id0`, `s`.`OneToOne_Optional_PK_Inverse4Id0`, `s`.`OneToOne_Optional_Self4Id0`
FROM `LevelOne` AS `l`
LEFT JOIN (
    SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id` AS `Id0`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name` AS `Name0`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`, `l2`.`Id` AS `Id1`, `l2`.`Level3_Optional_Id`, `l2`.`Level3_Required_Id`, `l2`.`Name` AS `Name1`, `l2`.`OneToMany_Optional_Inverse4Id`, `l2`.`OneToMany_Optional_Self_Inverse4Id`, `l2`.`OneToMany_Required_Inverse4Id`, `l2`.`OneToMany_Required_Self_Inverse4Id`, `l2`.`OneToOne_Optional_PK_Inverse4Id`, `l2`.`OneToOne_Optional_Self4Id`, `l3`.`Id` AS `Id2`, `l3`.`Level2_Optional_Id` AS `Level2_Optional_Id0`, `l3`.`Level2_Required_Id` AS `Level2_Required_Id0`, `l3`.`Name` AS `Name2`, `l3`.`OneToMany_Optional_Inverse3Id` AS `OneToMany_Optional_Inverse3Id0`, `l3`.`OneToMany_Optional_Self_Inverse3Id` AS `OneToMany_Optional_Self_Inverse3Id0`, `l3`.`OneToMany_Required_Inverse3Id` AS `OneToMany_Required_Inverse3Id0`, `l3`.`OneToMany_Required_Self_Inverse3Id` AS `OneToMany_Required_Self_Inverse3Id0`, `l3`.`OneToOne_Optional_PK_Inverse3Id` AS `OneToOne_Optional_PK_Inverse3Id0`, `l3`.`OneToOne_Optional_Self3Id` AS `OneToOne_Optional_Self3Id0`, `l4`.`Id` AS `Id3`, `l4`.`Level3_Optional_Id` AS `Level3_Optional_Id0`, `l4`.`Level3_Required_Id` AS `Level3_Required_Id0`, `l4`.`Name` AS `Name3`, `l4`.`OneToMany_Optional_Inverse4Id` AS `OneToMany_Optional_Inverse4Id0`, `l4`.`OneToMany_Optional_Self_Inverse4Id` AS `OneToMany_Optional_Self_Inverse4Id0`, `l4`.`OneToMany_Required_Inverse4Id` AS `OneToMany_Required_Inverse4Id0`, `l4`.`OneToMany_Required_Self_Inverse4Id` AS `OneToMany_Required_Self_Inverse4Id0`, `l4`.`OneToOne_Optional_PK_Inverse4Id` AS `OneToOne_Optional_PK_Inverse4Id0`, `l4`.`OneToOne_Optional_Self4Id` AS `OneToOne_Optional_Self4Id0`
    FROM (((`LevelTwo` AS `l0`
    LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToOne_Optional_PK_Inverse3Id`)
    LEFT JOIN `LevelFour` AS `l2` ON `l1`.`Id` = `l2`.`Level3_Optional_Id`)
    LEFT JOIN `LevelThree` AS `l3` ON `l0`.`Id` = `l3`.`Level2_Optional_Id`)
    LEFT JOIN `LevelFour` AS `l4` ON `l3`.`Id` = `l4`.`OneToMany_Optional_Inverse4Id`
) AS `s` ON `l`.`Id` = `s`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`, `s`.`Id`, `s`.`Id0`, `s`.`Id1`, `s`.`Id2`
""");
    }

    public override async Task Include_collection_multiple(bool async)
    {
        await base.Include_collection_multiple(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `s`.`Id`, `s`.`Date`, `s`.`Level1_Optional_Id`, `s`.`Level1_Required_Id`, `s`.`Name`, `s`.`OneToMany_Optional_Inverse2Id`, `s`.`OneToMany_Optional_Self_Inverse2Id`, `s`.`OneToMany_Required_Inverse2Id`, `s`.`OneToMany_Required_Self_Inverse2Id`, `s`.`OneToOne_Optional_PK_Inverse2Id`, `s`.`OneToOne_Optional_Self2Id`, `s`.`Id0`, `s`.`Level2_Optional_Id`, `s`.`Level2_Required_Id`, `s`.`Name0`, `s`.`OneToMany_Optional_Inverse3Id`, `s`.`OneToMany_Optional_Self_Inverse3Id`, `s`.`OneToMany_Required_Inverse3Id`, `s`.`OneToMany_Required_Self_Inverse3Id`, `s`.`OneToOne_Optional_PK_Inverse3Id`, `s`.`OneToOne_Optional_Self3Id`, `s`.`Id1`, `s`.`Level3_Optional_Id`, `s`.`Level3_Required_Id`, `s`.`Name1`, `s`.`OneToMany_Optional_Inverse4Id`, `s`.`OneToMany_Optional_Self_Inverse4Id`, `s`.`OneToMany_Required_Inverse4Id`, `s`.`OneToMany_Required_Self_Inverse4Id`, `s`.`OneToOne_Optional_PK_Inverse4Id`, `s`.`OneToOne_Optional_Self4Id`, `s`.`Id2`, `s`.`Level2_Optional_Id0`, `s`.`Level2_Required_Id0`, `s`.`Name2`, `s`.`OneToMany_Optional_Inverse3Id0`, `s`.`OneToMany_Optional_Self_Inverse3Id0`, `s`.`OneToMany_Required_Inverse3Id0`, `s`.`OneToMany_Required_Self_Inverse3Id0`, `s`.`OneToOne_Optional_PK_Inverse3Id0`, `s`.`OneToOne_Optional_Self3Id0`, `s`.`Id3`, `s`.`Level3_Optional_Id0`, `s`.`Level3_Required_Id0`, `s`.`Name3`, `s`.`OneToMany_Optional_Inverse4Id0`, `s`.`OneToMany_Optional_Self_Inverse4Id0`, `s`.`OneToMany_Required_Inverse4Id0`, `s`.`OneToMany_Required_Self_Inverse4Id0`, `s`.`OneToOne_Optional_PK_Inverse4Id0`, `s`.`OneToOne_Optional_Self4Id0`
FROM `LevelOne` AS `l`
LEFT JOIN (
    SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id` AS `Id0`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name` AS `Name0`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`, `l2`.`Id` AS `Id1`, `l2`.`Level3_Optional_Id`, `l2`.`Level3_Required_Id`, `l2`.`Name` AS `Name1`, `l2`.`OneToMany_Optional_Inverse4Id`, `l2`.`OneToMany_Optional_Self_Inverse4Id`, `l2`.`OneToMany_Required_Inverse4Id`, `l2`.`OneToMany_Required_Self_Inverse4Id`, `l2`.`OneToOne_Optional_PK_Inverse4Id`, `l2`.`OneToOne_Optional_Self4Id`, `l3`.`Id` AS `Id2`, `l3`.`Level2_Optional_Id` AS `Level2_Optional_Id0`, `l3`.`Level2_Required_Id` AS `Level2_Required_Id0`, `l3`.`Name` AS `Name2`, `l3`.`OneToMany_Optional_Inverse3Id` AS `OneToMany_Optional_Inverse3Id0`, `l3`.`OneToMany_Optional_Self_Inverse3Id` AS `OneToMany_Optional_Self_Inverse3Id0`, `l3`.`OneToMany_Required_Inverse3Id` AS `OneToMany_Required_Inverse3Id0`, `l3`.`OneToMany_Required_Self_Inverse3Id` AS `OneToMany_Required_Self_Inverse3Id0`, `l3`.`OneToOne_Optional_PK_Inverse3Id` AS `OneToOne_Optional_PK_Inverse3Id0`, `l3`.`OneToOne_Optional_Self3Id` AS `OneToOne_Optional_Self3Id0`, `l4`.`Id` AS `Id3`, `l4`.`Level3_Optional_Id` AS `Level3_Optional_Id0`, `l4`.`Level3_Required_Id` AS `Level3_Required_Id0`, `l4`.`Name` AS `Name3`, `l4`.`OneToMany_Optional_Inverse4Id` AS `OneToMany_Optional_Inverse4Id0`, `l4`.`OneToMany_Optional_Self_Inverse4Id` AS `OneToMany_Optional_Self_Inverse4Id0`, `l4`.`OneToMany_Required_Inverse4Id` AS `OneToMany_Required_Inverse4Id0`, `l4`.`OneToMany_Required_Self_Inverse4Id` AS `OneToMany_Required_Self_Inverse4Id0`, `l4`.`OneToOne_Optional_PK_Inverse4Id` AS `OneToOne_Optional_PK_Inverse4Id0`, `l4`.`OneToOne_Optional_Self4Id` AS `OneToOne_Optional_Self4Id0`
    FROM (((`LevelTwo` AS `l0`
    LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToOne_Optional_PK_Inverse3Id`)
    LEFT JOIN `LevelFour` AS `l2` ON `l1`.`Id` = `l2`.`Level3_Optional_Id`)
    LEFT JOIN `LevelThree` AS `l3` ON `l0`.`Id` = `l3`.`Level2_Optional_Id`)
    LEFT JOIN `LevelFour` AS `l4` ON `l3`.`Id` = `l4`.`OneToMany_Optional_Inverse4Id`
) AS `s` ON `l`.`Id` = `s`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`, `s`.`Id`, `s`.`Id0`, `s`.`Id1`, `s`.`Id2`
""");
    }

    public override async Task Include_ThenInclude_ThenInclude_followed_by_two_nested_selects(bool async)
    {
        await base.Include_ThenInclude_ThenInclude_followed_by_two_nested_selects(async);

        AssertSql(
            """
SELECT `l`.`Id`, `s`.`Id`, `s`.`Level2_Optional_Id`, `s`.`Level2_Required_Id`, `s`.`Name`, `s`.`OneToMany_Optional_Inverse3Id`, `s`.`OneToMany_Optional_Self_Inverse3Id`, `s`.`OneToMany_Required_Inverse3Id`, `s`.`OneToMany_Required_Self_Inverse3Id`, `s`.`OneToOne_Optional_PK_Inverse3Id`, `s`.`OneToOne_Optional_Self3Id`, `s`.`Id0`, `s`.`Level3_Optional_Id`, `s`.`Level3_Required_Id`, `s`.`Name0`, `s`.`OneToMany_Optional_Inverse4Id`, `s`.`OneToMany_Optional_Self_Inverse4Id`, `s`.`OneToMany_Required_Inverse4Id`, `s`.`OneToMany_Required_Self_Inverse4Id`, `s`.`OneToOne_Optional_PK_Inverse4Id`, `s`.`OneToOne_Optional_Self4Id`, `s`.`Id1`
FROM `LevelOne` AS `l`
LEFT JOIN (
    SELECT `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`, `l2`.`Id` AS `Id0`, `l2`.`Level3_Optional_Id`, `l2`.`Level3_Required_Id`, `l2`.`Name` AS `Name0`, `l2`.`OneToMany_Optional_Inverse4Id`, `l2`.`OneToMany_Optional_Self_Inverse4Id`, `l2`.`OneToMany_Required_Inverse4Id`, `l2`.`OneToMany_Required_Self_Inverse4Id`, `l2`.`OneToOne_Optional_PK_Inverse4Id`, `l2`.`OneToOne_Optional_Self4Id`, `l0`.`Id` AS `Id1`, `l0`.`OneToMany_Optional_Inverse2Id`
    FROM (`LevelTwo` AS `l0`
    LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToOne_Optional_PK_Inverse3Id`)
    LEFT JOIN `LevelFour` AS `l2` ON `l1`.`Id` = `l2`.`Level3_Optional_Id`
) AS `s` ON `l`.`Id` = `s`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`, `s`.`Id1`, `s`.`Id`
""");
    }

    public override async Task Include_collection_ThenInclude_reference_followed_by_projection_into_anonmous_type(bool async)
    {
        await base.Include_collection_ThenInclude_reference_followed_by_projection_into_anonmous_type(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `s`.`Id`, `s`.`Date`, `s`.`Level1_Optional_Id`, `s`.`Level1_Required_Id`, `s`.`Name`, `s`.`OneToMany_Optional_Inverse2Id`, `s`.`OneToMany_Optional_Self_Inverse2Id`, `s`.`OneToMany_Required_Inverse2Id`, `s`.`OneToMany_Required_Self_Inverse2Id`, `s`.`OneToOne_Optional_PK_Inverse2Id`, `s`.`OneToOne_Optional_Self2Id`, `s`.`Id0`, `s`.`Level2_Optional_Id`, `s`.`Level2_Required_Id`, `s`.`Name0`, `s`.`OneToMany_Optional_Inverse3Id`, `s`.`OneToMany_Optional_Self_Inverse3Id`, `s`.`OneToMany_Required_Inverse3Id`, `s`.`OneToMany_Required_Self_Inverse3Id`, `s`.`OneToOne_Optional_PK_Inverse3Id`, `s`.`OneToOne_Optional_Self3Id`, `s0`.`Id`, `s0`.`Date`, `s0`.`Level1_Optional_Id`, `s0`.`Level1_Required_Id`, `s0`.`Name`, `s0`.`OneToMany_Optional_Inverse2Id`, `s0`.`OneToMany_Optional_Self_Inverse2Id`, `s0`.`OneToMany_Required_Inverse2Id`, `s0`.`OneToMany_Required_Self_Inverse2Id`, `s0`.`OneToOne_Optional_PK_Inverse2Id`, `s0`.`OneToOne_Optional_Self2Id`, `s0`.`Id0`, `s0`.`Level2_Optional_Id`, `s0`.`Level2_Required_Id`, `s0`.`Name0`, `s0`.`OneToMany_Optional_Inverse3Id`, `s0`.`OneToMany_Optional_Self_Inverse3Id`, `s0`.`OneToMany_Required_Inverse3Id`, `s0`.`OneToMany_Required_Self_Inverse3Id`, `s0`.`OneToOne_Optional_PK_Inverse3Id`, `s0`.`OneToOne_Optional_Self3Id`
FROM (`LevelOne` AS `l`
LEFT JOIN (
    SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id` AS `Id0`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name` AS `Name0`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`
    FROM `LevelTwo` AS `l0`
    LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToOne_Optional_PK_Inverse3Id`
) AS `s` ON `l`.`Id` = `s`.`OneToMany_Optional_Inverse2Id`)
LEFT JOIN (
    SELECT `l2`.`Id`, `l2`.`Date`, `l2`.`Level1_Optional_Id`, `l2`.`Level1_Required_Id`, `l2`.`Name`, `l2`.`OneToMany_Optional_Inverse2Id`, `l2`.`OneToMany_Optional_Self_Inverse2Id`, `l2`.`OneToMany_Required_Inverse2Id`, `l2`.`OneToMany_Required_Self_Inverse2Id`, `l2`.`OneToOne_Optional_PK_Inverse2Id`, `l2`.`OneToOne_Optional_Self2Id`, `l3`.`Id` AS `Id0`, `l3`.`Level2_Optional_Id`, `l3`.`Level2_Required_Id`, `l3`.`Name` AS `Name0`, `l3`.`OneToMany_Optional_Inverse3Id`, `l3`.`OneToMany_Optional_Self_Inverse3Id`, `l3`.`OneToMany_Required_Inverse3Id`, `l3`.`OneToMany_Required_Self_Inverse3Id`, `l3`.`OneToOne_Optional_PK_Inverse3Id`, `l3`.`OneToOne_Optional_Self3Id`
    FROM `LevelTwo` AS `l2`
    LEFT JOIN `LevelThree` AS `l3` ON `l2`.`Id` = `l3`.`OneToOne_Optional_PK_Inverse3Id`
) AS `s0` ON `l`.`Id` = `s0`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`, `s`.`Id`, `s`.`Id0`, `s0`.`Id`
""");
    }

    public override async Task Include_collection_multiple_with_filter(bool async)
    {
        await base.Include_collection_multiple_with_filter(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `s`.`Id`, `s`.`Date`, `s`.`Level1_Optional_Id`, `s`.`Level1_Required_Id`, `s`.`Name`, `s`.`OneToMany_Optional_Inverse2Id`, `s`.`OneToMany_Optional_Self_Inverse2Id`, `s`.`OneToMany_Required_Inverse2Id`, `s`.`OneToMany_Required_Self_Inverse2Id`, `s`.`OneToOne_Optional_PK_Inverse2Id`, `s`.`OneToOne_Optional_Self2Id`, `s`.`Id0`, `s`.`Level2_Optional_Id`, `s`.`Level2_Required_Id`, `s`.`Name0`, `s`.`OneToMany_Optional_Inverse3Id`, `s`.`OneToMany_Optional_Self_Inverse3Id`, `s`.`OneToMany_Required_Inverse3Id`, `s`.`OneToMany_Required_Self_Inverse3Id`, `s`.`OneToOne_Optional_PK_Inverse3Id`, `s`.`OneToOne_Optional_Self3Id`, `s`.`Id1`, `s`.`Level3_Optional_Id`, `s`.`Level3_Required_Id`, `s`.`Name1`, `s`.`OneToMany_Optional_Inverse4Id`, `s`.`OneToMany_Optional_Self_Inverse4Id`, `s`.`OneToMany_Required_Inverse4Id`, `s`.`OneToMany_Required_Self_Inverse4Id`, `s`.`OneToOne_Optional_PK_Inverse4Id`, `s`.`OneToOne_Optional_Self4Id`
FROM `LevelOne` AS `l`
LEFT JOIN (
    SELECT `l2`.`Id`, `l2`.`Date`, `l2`.`Level1_Optional_Id`, `l2`.`Level1_Required_Id`, `l2`.`Name`, `l2`.`OneToMany_Optional_Inverse2Id`, `l2`.`OneToMany_Optional_Self_Inverse2Id`, `l2`.`OneToMany_Required_Inverse2Id`, `l2`.`OneToMany_Required_Self_Inverse2Id`, `l2`.`OneToOne_Optional_PK_Inverse2Id`, `l2`.`OneToOne_Optional_Self2Id`, `l3`.`Id` AS `Id0`, `l3`.`Level2_Optional_Id`, `l3`.`Level2_Required_Id`, `l3`.`Name` AS `Name0`, `l3`.`OneToMany_Optional_Inverse3Id`, `l3`.`OneToMany_Optional_Self_Inverse3Id`, `l3`.`OneToMany_Required_Inverse3Id`, `l3`.`OneToMany_Required_Self_Inverse3Id`, `l3`.`OneToOne_Optional_PK_Inverse3Id`, `l3`.`OneToOne_Optional_Self3Id`, `l4`.`Id` AS `Id1`, `l4`.`Level3_Optional_Id`, `l4`.`Level3_Required_Id`, `l4`.`Name` AS `Name1`, `l4`.`OneToMany_Optional_Inverse4Id`, `l4`.`OneToMany_Optional_Self_Inverse4Id`, `l4`.`OneToMany_Required_Inverse4Id`, `l4`.`OneToMany_Required_Self_Inverse4Id`, `l4`.`OneToOne_Optional_PK_Inverse4Id`, `l4`.`OneToOne_Optional_Self4Id`
    FROM (`LevelTwo` AS `l2`
    LEFT JOIN `LevelThree` AS `l3` ON `l2`.`Id` = `l3`.`OneToOne_Optional_PK_Inverse3Id`)
    LEFT JOIN `LevelFour` AS `l4` ON `l3`.`Id` = `l4`.`Level3_Optional_Id`
) AS `s` ON `l`.`Id` = `s`.`OneToMany_Optional_Inverse2Id`
WHERE (
    SELECT COUNT(*)
    FROM `LevelTwo` AS `l0`
    LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToOne_Optional_PK_Inverse3Id`
    WHERE `l`.`Id` = `l0`.`OneToMany_Optional_Inverse2Id` AND (`l1`.`Name` <> 'Foo' OR `l1`.`Name` IS NULL)) > 0
ORDER BY `l`.`Id`, `s`.`Id`, `s`.`Id0`
""");
    }

    public override async Task Lift_projection_mapping_when_pushing_down_subquery(bool async)
    {
        await base.Lift_projection_mapping_when_pushing_down_subquery(async);

        AssertSql(
"""
@__p_0='25'

SELECT [t].[Id], [t0].[Id], [l1].[Id], [t0].[c]
FROM (
    SELECT TOP(@__p_0) [l].[Id]
    FROM [LevelOne] AS [l]
) AS [t]
LEFT JOIN (
    SELECT [t1].[Id], [t1].[c], [t1].[OneToMany_Required_Inverse2Id]
    FROM (
        SELECT [l0].[Id], 1 AS [c], [l0].[OneToMany_Required_Inverse2Id], ROW_NUMBER() OVER(PARTITION BY [l0].[OneToMany_Required_Inverse2Id] ORDER BY [l0].[Id]) AS [row]
        FROM [LevelTwo] AS [l0]
    ) AS [t1]
    WHERE [t1].[row] <= 1
) AS [t0] ON [t].[Id] = [t0].[OneToMany_Required_Inverse2Id]
LEFT JOIN [LevelTwo] AS [l1] ON [t].[Id] = [l1].[OneToMany_Required_Inverse2Id]
ORDER BY [t].[Id], [t0].[Id]
""");
    }

    public override async Task Including_reference_navigation_and_projecting_collection_navigation(bool async)
    {
        await base.Including_reference_navigation_and_projecting_collection_navigation(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`, `l2`.`Id`, `l2`.`Date`, `l2`.`Level1_Optional_Id`, `l2`.`Level1_Required_Id`, `l2`.`Name`, `l2`.`OneToMany_Optional_Inverse2Id`, `l2`.`OneToMany_Optional_Self_Inverse2Id`, `l2`.`OneToMany_Required_Inverse2Id`, `l2`.`OneToMany_Required_Self_Inverse2Id`, `l2`.`OneToOne_Optional_PK_Inverse2Id`, `l2`.`OneToOne_Optional_Self2Id`
FROM ((`LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`Level1_Required_Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`Level2_Optional_Id`)
LEFT JOIN `LevelTwo` AS `l2` ON `l`.`Id` = `l2`.`OneToMany_Required_Inverse2Id`
ORDER BY `l`.`Id`, `l0`.`Id`, `l1`.`Id`
""");
    }

    public override async Task LeftJoin_with_Any_on_outer_source_and_projecting_collection_from_inner(bool async)
    {
        await base.LeftJoin_with_Any_on_outer_source_and_projecting_collection_from_inner(async);

        AssertSql(
            """
    SELECT IIF(`l0`.`Id` IS NULL, 0, `l0`.`Id`), `l`.`Id`, `l0`.`Id`, `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`
    FROM (`LevelOne` AS `l`
    LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`Level1_Required_Id`)
    LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Required_Inverse3Id`
    WHERE `l`.`Name` IN ('L1 01', 'L1 02')
    ORDER BY `l`.`Id`, `l0`.`Id`
    """);
    }

    public override async Task Select_subquery_single_nested_subquery(bool async)
    {
        await base.Select_subquery_single_nested_subquery(async);

        AssertSql(
"""
SELECT [l].[Id], [t0].[Id], [l1].[Id], [t0].[c]
FROM [LevelOne] AS [l]
LEFT JOIN (
    SELECT [t].[c], [t].[Id], [t].[OneToMany_Optional_Inverse2Id]
    FROM (
        SELECT 1 AS [c], [l0].[Id], [l0].[OneToMany_Optional_Inverse2Id], ROW_NUMBER() OVER(PARTITION BY [l0].[OneToMany_Optional_Inverse2Id] ORDER BY [l0].[Id]) AS [row]
        FROM [LevelTwo] AS [l0]
    ) AS [t]
    WHERE [t].[row] <= 1
) AS [t0] ON [l].[Id] = [t0].[OneToMany_Optional_Inverse2Id]
LEFT JOIN [LevelThree] AS [l1] ON [t0].[Id] = [l1].[OneToMany_Optional_Inverse3Id]
ORDER BY [l].[Id], [t0].[Id], [l1].[Id]
""");
    }

    public override async Task Select_subquery_single_nested_subquery2(bool async)
    {
        await base.Select_subquery_single_nested_subquery2(async);

        AssertSql(
"""
SELECT [l].[Id], [t1].[Id], [t1].[Id0], [t1].[Id1], [t1].[c]
FROM [LevelOne] AS [l]
LEFT JOIN (
    SELECT [l0].[Id], [t0].[Id] AS [Id0], [l1].[Id] AS [Id1], [t0].[c], [l0].[OneToMany_Optional_Inverse2Id]
    FROM [LevelTwo] AS [l0]
    LEFT JOIN (
        SELECT [t].[c], [t].[Id], [t].[OneToMany_Optional_Inverse3Id]
        FROM (
            SELECT 1 AS [c], [l2].[Id], [l2].[OneToMany_Optional_Inverse3Id], ROW_NUMBER() OVER(PARTITION BY [l2].[OneToMany_Optional_Inverse3Id] ORDER BY [l2].[Id]) AS [row]
            FROM [LevelThree] AS [l2]
        ) AS [t]
        WHERE [t].[row] <= 1
    ) AS [t0] ON [l0].[Id] = [t0].[OneToMany_Optional_Inverse3Id]
    LEFT JOIN [LevelFour] AS [l1] ON [t0].[Id] = [l1].[OneToMany_Optional_Inverse4Id]
) AS [t1] ON [l].[Id] = [t1].[OneToMany_Optional_Inverse2Id]
ORDER BY [l].[Id], [t1].[Id], [t1].[Id0], [t1].[Id1]
""");
    }

    public override async Task Filtered_include_basic_Where(bool async)
    {
        await base.Filtered_include_basic_Where(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `l1`.`Id`, `l1`.`Date`, `l1`.`Level1_Optional_Id`, `l1`.`Level1_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse2Id`, `l1`.`OneToMany_Optional_Self_Inverse2Id`, `l1`.`OneToMany_Required_Inverse2Id`, `l1`.`OneToMany_Required_Self_Inverse2Id`, `l1`.`OneToOne_Optional_PK_Inverse2Id`, `l1`.`OneToOne_Optional_Self2Id`
FROM `LevelOne` AS `l`
LEFT JOIN (
    SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`
    FROM `LevelTwo` AS `l0`
    WHERE `l0`.`Id` > 5
) AS `l1` ON `l`.`Id` = `l1`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`
""");
    }

    public override async Task Filtered_include_OrderBy(bool async)
    {
        await base.Filtered_include_OrderBy(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`
FROM `LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`, `l0`.`Name`
""");
    }

    public override async Task Filtered_ThenInclude_OrderBy(bool async)
    {
        await base.Filtered_ThenInclude_OrderBy(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `s`.`Id`, `s`.`Date`, `s`.`Level1_Optional_Id`, `s`.`Level1_Required_Id`, `s`.`Name`, `s`.`OneToMany_Optional_Inverse2Id`, `s`.`OneToMany_Optional_Self_Inverse2Id`, `s`.`OneToMany_Required_Inverse2Id`, `s`.`OneToMany_Required_Self_Inverse2Id`, `s`.`OneToOne_Optional_PK_Inverse2Id`, `s`.`OneToOne_Optional_Self2Id`, `s`.`Id0`, `s`.`Level2_Optional_Id`, `s`.`Level2_Required_Id`, `s`.`Name0`, `s`.`OneToMany_Optional_Inverse3Id`, `s`.`OneToMany_Optional_Self_Inverse3Id`, `s`.`OneToMany_Required_Inverse3Id`, `s`.`OneToMany_Required_Self_Inverse3Id`, `s`.`OneToOne_Optional_PK_Inverse3Id`, `s`.`OneToOne_Optional_Self3Id`
FROM `LevelOne` AS `l`
LEFT JOIN (
    SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id` AS `Id0`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name` AS `Name0`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`
    FROM `LevelTwo` AS `l0`
    LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Inverse3Id`
) AS `s` ON `l`.`Id` = `s`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`, `s`.`Id`, `s`.`Name0`
""");
    }

    public override async Task Filtered_include_ThenInclude_OrderBy(bool async)
    {
        await base.Filtered_include_ThenInclude_OrderBy(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `s`.`Id`, `s`.`Date`, `s`.`Level1_Optional_Id`, `s`.`Level1_Required_Id`, `s`.`Name`, `s`.`OneToMany_Optional_Inverse2Id`, `s`.`OneToMany_Optional_Self_Inverse2Id`, `s`.`OneToMany_Required_Inverse2Id`, `s`.`OneToMany_Required_Self_Inverse2Id`, `s`.`OneToOne_Optional_PK_Inverse2Id`, `s`.`OneToOne_Optional_Self2Id`, `s`.`Id0`, `s`.`Level2_Optional_Id`, `s`.`Level2_Required_Id`, `s`.`Name0`, `s`.`OneToMany_Optional_Inverse3Id`, `s`.`OneToMany_Optional_Self_Inverse3Id`, `s`.`OneToMany_Required_Inverse3Id`, `s`.`OneToMany_Required_Self_Inverse3Id`, `s`.`OneToOne_Optional_PK_Inverse3Id`, `s`.`OneToOne_Optional_Self3Id`
FROM `LevelOne` AS `l`
LEFT JOIN (
    SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id` AS `Id0`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name` AS `Name0`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`
    FROM `LevelTwo` AS `l0`
    LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Inverse3Id`
) AS `s` ON `l`.`Id` = `s`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`, `s`.`Name`, `s`.`Id`, `s`.`Name0` DESC
""");
    }

    public override async Task Filtered_include_basic_OrderBy_Take(bool async)
    {
        await base.Filtered_include_basic_OrderBy_Take(async);

        AssertSql(
"""
SELECT [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id], [t0].[Id], [t0].[Date], [t0].[Level1_Optional_Id], [t0].[Level1_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse2Id], [t0].[OneToMany_Optional_Self_Inverse2Id], [t0].[OneToMany_Required_Inverse2Id], [t0].[OneToMany_Required_Self_Inverse2Id], [t0].[OneToOne_Optional_PK_Inverse2Id], [t0].[OneToOne_Optional_Self2Id]
FROM [LevelOne] AS [l]
LEFT JOIN (
    SELECT [t].[Id], [t].[Date], [t].[Level1_Optional_Id], [t].[Level1_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse2Id], [t].[OneToMany_Optional_Self_Inverse2Id], [t].[OneToMany_Required_Inverse2Id], [t].[OneToMany_Required_Self_Inverse2Id], [t].[OneToOne_Optional_PK_Inverse2Id], [t].[OneToOne_Optional_Self2Id]
    FROM (
        SELECT [l0].[Id], [l0].[Date], [l0].[Level1_Optional_Id], [l0].[Level1_Required_Id], [l0].[Name], [l0].[OneToMany_Optional_Inverse2Id], [l0].[OneToMany_Optional_Self_Inverse2Id], [l0].[OneToMany_Required_Inverse2Id], [l0].[OneToMany_Required_Self_Inverse2Id], [l0].[OneToOne_Optional_PK_Inverse2Id], [l0].[OneToOne_Optional_Self2Id], ROW_NUMBER() OVER(PARTITION BY [l0].[OneToMany_Optional_Inverse2Id] ORDER BY [l0].[Name]) AS [row]
        FROM [LevelTwo] AS [l0]
    ) AS [t]
    WHERE [t].[row] <= 3
) AS [t0] ON [l].[Id] = [t0].[OneToMany_Optional_Inverse2Id]
ORDER BY [l].[Id], [t0].[OneToMany_Optional_Inverse2Id], [t0].[Name]
""");
    }

    public override async Task Filtered_include_basic_OrderBy_Skip(bool async)
    {
        await base.Filtered_include_basic_OrderBy_Skip(async);

        AssertSql(
"""
SELECT [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id], [t0].[Id], [t0].[Date], [t0].[Level1_Optional_Id], [t0].[Level1_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse2Id], [t0].[OneToMany_Optional_Self_Inverse2Id], [t0].[OneToMany_Required_Inverse2Id], [t0].[OneToMany_Required_Self_Inverse2Id], [t0].[OneToOne_Optional_PK_Inverse2Id], [t0].[OneToOne_Optional_Self2Id]
FROM [LevelOne] AS [l]
LEFT JOIN (
    SELECT [t].[Id], [t].[Date], [t].[Level1_Optional_Id], [t].[Level1_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse2Id], [t].[OneToMany_Optional_Self_Inverse2Id], [t].[OneToMany_Required_Inverse2Id], [t].[OneToMany_Required_Self_Inverse2Id], [t].[OneToOne_Optional_PK_Inverse2Id], [t].[OneToOne_Optional_Self2Id]
    FROM (
        SELECT [l0].[Id], [l0].[Date], [l0].[Level1_Optional_Id], [l0].[Level1_Required_Id], [l0].[Name], [l0].[OneToMany_Optional_Inverse2Id], [l0].[OneToMany_Optional_Self_Inverse2Id], [l0].[OneToMany_Required_Inverse2Id], [l0].[OneToMany_Required_Self_Inverse2Id], [l0].[OneToOne_Optional_PK_Inverse2Id], [l0].[OneToOne_Optional_Self2Id], ROW_NUMBER() OVER(PARTITION BY [l0].[OneToMany_Optional_Inverse2Id] ORDER BY [l0].[Name]) AS [row]
        FROM [LevelTwo] AS [l0]
    ) AS [t]
    WHERE 1 < [t].[row]
) AS [t0] ON [l].[Id] = [t0].[OneToMany_Optional_Inverse2Id]
ORDER BY [l].[Id], [t0].[OneToMany_Optional_Inverse2Id], [t0].[Name]
""");
    }

    public override async Task Filtered_include_basic_OrderBy_Skip_Take(bool async)
    {
        await base.Filtered_include_basic_OrderBy_Skip_Take(async);

        AssertSql(
"""
SELECT [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id], [t0].[Id], [t0].[Date], [t0].[Level1_Optional_Id], [t0].[Level1_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse2Id], [t0].[OneToMany_Optional_Self_Inverse2Id], [t0].[OneToMany_Required_Inverse2Id], [t0].[OneToMany_Required_Self_Inverse2Id], [t0].[OneToOne_Optional_PK_Inverse2Id], [t0].[OneToOne_Optional_Self2Id]
FROM [LevelOne] AS [l]
LEFT JOIN (
    SELECT [t].[Id], [t].[Date], [t].[Level1_Optional_Id], [t].[Level1_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse2Id], [t].[OneToMany_Optional_Self_Inverse2Id], [t].[OneToMany_Required_Inverse2Id], [t].[OneToMany_Required_Self_Inverse2Id], [t].[OneToOne_Optional_PK_Inverse2Id], [t].[OneToOne_Optional_Self2Id]
    FROM (
        SELECT [l0].[Id], [l0].[Date], [l0].[Level1_Optional_Id], [l0].[Level1_Required_Id], [l0].[Name], [l0].[OneToMany_Optional_Inverse2Id], [l0].[OneToMany_Optional_Self_Inverse2Id], [l0].[OneToMany_Required_Inverse2Id], [l0].[OneToMany_Required_Self_Inverse2Id], [l0].[OneToOne_Optional_PK_Inverse2Id], [l0].[OneToOne_Optional_Self2Id], ROW_NUMBER() OVER(PARTITION BY [l0].[OneToMany_Optional_Inverse2Id] ORDER BY [l0].[Name]) AS [row]
        FROM [LevelTwo] AS [l0]
    ) AS [t]
    WHERE 1 < [t].[row] AND [t].[row] <= 4
) AS [t0] ON [l].[Id] = [t0].[OneToMany_Optional_Inverse2Id]
ORDER BY [l].[Id], [t0].[OneToMany_Optional_Inverse2Id], [t0].[Name]
""");
    }

    public override async Task Filtered_include_Skip_without_OrderBy(bool async)
    {
        await base.Filtered_include_Skip_without_OrderBy(async);

        AssertSql(
"""
SELECT [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id], [t0].[Id], [t0].[Date], [t0].[Level1_Optional_Id], [t0].[Level1_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse2Id], [t0].[OneToMany_Optional_Self_Inverse2Id], [t0].[OneToMany_Required_Inverse2Id], [t0].[OneToMany_Required_Self_Inverse2Id], [t0].[OneToOne_Optional_PK_Inverse2Id], [t0].[OneToOne_Optional_Self2Id]
FROM [LevelOne] AS [l]
LEFT JOIN (
    SELECT [t].[Id], [t].[Date], [t].[Level1_Optional_Id], [t].[Level1_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse2Id], [t].[OneToMany_Optional_Self_Inverse2Id], [t].[OneToMany_Required_Inverse2Id], [t].[OneToMany_Required_Self_Inverse2Id], [t].[OneToOne_Optional_PK_Inverse2Id], [t].[OneToOne_Optional_Self2Id]
    FROM (
        SELECT [l0].[Id], [l0].[Date], [l0].[Level1_Optional_Id], [l0].[Level1_Required_Id], [l0].[Name], [l0].[OneToMany_Optional_Inverse2Id], [l0].[OneToMany_Optional_Self_Inverse2Id], [l0].[OneToMany_Required_Inverse2Id], [l0].[OneToMany_Required_Self_Inverse2Id], [l0].[OneToOne_Optional_PK_Inverse2Id], [l0].[OneToOne_Optional_Self2Id], ROW_NUMBER() OVER(PARTITION BY [l0].[OneToMany_Optional_Inverse2Id] ORDER BY [l0].[Id]) AS [row]
        FROM [LevelTwo] AS [l0]
    ) AS [t]
    WHERE 1 < [t].[row]
) AS [t0] ON [l].[Id] = [t0].[OneToMany_Optional_Inverse2Id]
ORDER BY [l].[Id]
""");
    }

    public override async Task Filtered_include_Take_without_OrderBy(bool async)
    {
        await base.Filtered_include_Take_without_OrderBy(async);

        AssertSql(
"""
SELECT [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id], [t0].[Id], [t0].[Date], [t0].[Level1_Optional_Id], [t0].[Level1_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse2Id], [t0].[OneToMany_Optional_Self_Inverse2Id], [t0].[OneToMany_Required_Inverse2Id], [t0].[OneToMany_Required_Self_Inverse2Id], [t0].[OneToOne_Optional_PK_Inverse2Id], [t0].[OneToOne_Optional_Self2Id]
FROM [LevelOne] AS [l]
LEFT JOIN (
    SELECT [t].[Id], [t].[Date], [t].[Level1_Optional_Id], [t].[Level1_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse2Id], [t].[OneToMany_Optional_Self_Inverse2Id], [t].[OneToMany_Required_Inverse2Id], [t].[OneToMany_Required_Self_Inverse2Id], [t].[OneToOne_Optional_PK_Inverse2Id], [t].[OneToOne_Optional_Self2Id]
    FROM (
        SELECT [l0].[Id], [l0].[Date], [l0].[Level1_Optional_Id], [l0].[Level1_Required_Id], [l0].[Name], [l0].[OneToMany_Optional_Inverse2Id], [l0].[OneToMany_Optional_Self_Inverse2Id], [l0].[OneToMany_Required_Inverse2Id], [l0].[OneToMany_Required_Self_Inverse2Id], [l0].[OneToOne_Optional_PK_Inverse2Id], [l0].[OneToOne_Optional_Self2Id], ROW_NUMBER() OVER(PARTITION BY [l0].[OneToMany_Optional_Inverse2Id] ORDER BY [l0].[Id]) AS [row]
        FROM [LevelTwo] AS [l0]
    ) AS [t]
    WHERE [t].[row] <= 1
) AS [t0] ON [l].[Id] = [t0].[OneToMany_Optional_Inverse2Id]
ORDER BY [l].[Id]
""");
    }

    public override async Task Filtered_include_on_ThenInclude(bool async)
    {
        await base.Filtered_include_on_ThenInclude(async);

        AssertSql(
"""
SELECT [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id], [l0].[Id], [l0].[Date], [l0].[Level1_Optional_Id], [l0].[Level1_Required_Id], [l0].[Name], [l0].[OneToMany_Optional_Inverse2Id], [l0].[OneToMany_Optional_Self_Inverse2Id], [l0].[OneToMany_Required_Inverse2Id], [l0].[OneToMany_Required_Self_Inverse2Id], [l0].[OneToOne_Optional_PK_Inverse2Id], [l0].[OneToOne_Optional_Self2Id], [t0].[Id], [t0].[Level2_Optional_Id], [t0].[Level2_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse3Id], [t0].[OneToMany_Optional_Self_Inverse3Id], [t0].[OneToMany_Required_Inverse3Id], [t0].[OneToMany_Required_Self_Inverse3Id], [t0].[OneToOne_Optional_PK_Inverse3Id], [t0].[OneToOne_Optional_Self3Id]
FROM [LevelOne] AS [l]
LEFT JOIN [LevelTwo] AS [l0] ON [l].[Id] = [l0].[Level1_Optional_Id]
LEFT JOIN (
    SELECT [t].[Id], [t].[Level2_Optional_Id], [t].[Level2_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse3Id], [t].[OneToMany_Optional_Self_Inverse3Id], [t].[OneToMany_Required_Inverse3Id], [t].[OneToMany_Required_Self_Inverse3Id], [t].[OneToOne_Optional_PK_Inverse3Id], [t].[OneToOne_Optional_Self3Id]
    FROM (
        SELECT [l1].[Id], [l1].[Level2_Optional_Id], [l1].[Level2_Required_Id], [l1].[Name], [l1].[OneToMany_Optional_Inverse3Id], [l1].[OneToMany_Optional_Self_Inverse3Id], [l1].[OneToMany_Required_Inverse3Id], [l1].[OneToMany_Required_Self_Inverse3Id], [l1].[OneToOne_Optional_PK_Inverse3Id], [l1].[OneToOne_Optional_Self3Id], ROW_NUMBER() OVER(PARTITION BY [l1].[OneToMany_Optional_Inverse3Id] ORDER BY [l1].[Name]) AS [row]
        FROM [LevelThree] AS [l1]
        WHERE [l1].[Name] <> N'Foo' OR ([l1].[Name] IS NULL)
    ) AS [t]
    WHERE 1 < [t].[row] AND [t].[row] <= 4
) AS [t0] ON [l0].[Id] = [t0].[OneToMany_Optional_Inverse3Id]
ORDER BY [l].[Id], [l0].[Id], [t0].[OneToMany_Optional_Inverse3Id], [t0].[Name]
""");
    }

    public override async Task Filtered_include_after_reference_navigation(bool async)
    {
        await base.Filtered_include_after_reference_navigation(async);

        AssertSql(
"""
SELECT [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id], [l0].[Id], [l0].[Date], [l0].[Level1_Optional_Id], [l0].[Level1_Required_Id], [l0].[Name], [l0].[OneToMany_Optional_Inverse2Id], [l0].[OneToMany_Optional_Self_Inverse2Id], [l0].[OneToMany_Required_Inverse2Id], [l0].[OneToMany_Required_Self_Inverse2Id], [l0].[OneToOne_Optional_PK_Inverse2Id], [l0].[OneToOne_Optional_Self2Id], [t0].[Id], [t0].[Level2_Optional_Id], [t0].[Level2_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse3Id], [t0].[OneToMany_Optional_Self_Inverse3Id], [t0].[OneToMany_Required_Inverse3Id], [t0].[OneToMany_Required_Self_Inverse3Id], [t0].[OneToOne_Optional_PK_Inverse3Id], [t0].[OneToOne_Optional_Self3Id]
FROM [LevelOne] AS [l]
LEFT JOIN [LevelTwo] AS [l0] ON [l].[Id] = [l0].[Level1_Optional_Id]
LEFT JOIN (
    SELECT [t].[Id], [t].[Level2_Optional_Id], [t].[Level2_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse3Id], [t].[OneToMany_Optional_Self_Inverse3Id], [t].[OneToMany_Required_Inverse3Id], [t].[OneToMany_Required_Self_Inverse3Id], [t].[OneToOne_Optional_PK_Inverse3Id], [t].[OneToOne_Optional_Self3Id]
    FROM (
        SELECT [l1].[Id], [l1].[Level2_Optional_Id], [l1].[Level2_Required_Id], [l1].[Name], [l1].[OneToMany_Optional_Inverse3Id], [l1].[OneToMany_Optional_Self_Inverse3Id], [l1].[OneToMany_Required_Inverse3Id], [l1].[OneToMany_Required_Self_Inverse3Id], [l1].[OneToOne_Optional_PK_Inverse3Id], [l1].[OneToOne_Optional_Self3Id], ROW_NUMBER() OVER(PARTITION BY [l1].[OneToMany_Optional_Inverse3Id] ORDER BY [l1].[Name]) AS [row]
        FROM [LevelThree] AS [l1]
        WHERE [l1].[Name] <> N'Foo' OR ([l1].[Name] IS NULL)
    ) AS [t]
    WHERE 1 < [t].[row] AND [t].[row] <= 4
) AS [t0] ON [l0].[Id] = [t0].[OneToMany_Optional_Inverse3Id]
ORDER BY [l].[Id], [l0].[Id], [t0].[OneToMany_Optional_Inverse3Id], [t0].[Name]
""");
    }

    public override async Task Filtered_include_after_different_filtered_include_same_level(bool async)
    {
        await base.Filtered_include_after_different_filtered_include_same_level(async);

        AssertSql(
"""
SELECT [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id], [t0].[Id], [t0].[Date], [t0].[Level1_Optional_Id], [t0].[Level1_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse2Id], [t0].[OneToMany_Optional_Self_Inverse2Id], [t0].[OneToMany_Required_Inverse2Id], [t0].[OneToMany_Required_Self_Inverse2Id], [t0].[OneToOne_Optional_PK_Inverse2Id], [t0].[OneToOne_Optional_Self2Id], [t1].[Id], [t1].[Date], [t1].[Level1_Optional_Id], [t1].[Level1_Required_Id], [t1].[Name], [t1].[OneToMany_Optional_Inverse2Id], [t1].[OneToMany_Optional_Self_Inverse2Id], [t1].[OneToMany_Required_Inverse2Id], [t1].[OneToMany_Required_Self_Inverse2Id], [t1].[OneToOne_Optional_PK_Inverse2Id], [t1].[OneToOne_Optional_Self2Id]
FROM [LevelOne] AS [l]
LEFT JOIN (
    SELECT [t].[Id], [t].[Date], [t].[Level1_Optional_Id], [t].[Level1_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse2Id], [t].[OneToMany_Optional_Self_Inverse2Id], [t].[OneToMany_Required_Inverse2Id], [t].[OneToMany_Required_Self_Inverse2Id], [t].[OneToOne_Optional_PK_Inverse2Id], [t].[OneToOne_Optional_Self2Id]
    FROM (
        SELECT [l0].[Id], [l0].[Date], [l0].[Level1_Optional_Id], [l0].[Level1_Required_Id], [l0].[Name], [l0].[OneToMany_Optional_Inverse2Id], [l0].[OneToMany_Optional_Self_Inverse2Id], [l0].[OneToMany_Required_Inverse2Id], [l0].[OneToMany_Required_Self_Inverse2Id], [l0].[OneToOne_Optional_PK_Inverse2Id], [l0].[OneToOne_Optional_Self2Id], ROW_NUMBER() OVER(PARTITION BY [l0].[OneToMany_Optional_Inverse2Id] ORDER BY [l0].[Name]) AS [row]
        FROM [LevelTwo] AS [l0]
        WHERE [l0].[Name] <> N'Foo' OR ([l0].[Name] IS NULL)
    ) AS [t]
    WHERE [t].[row] <= 3
) AS [t0] ON [l].[Id] = [t0].[OneToMany_Optional_Inverse2Id]
LEFT JOIN (
    SELECT [t2].[Id], [t2].[Date], [t2].[Level1_Optional_Id], [t2].[Level1_Required_Id], [t2].[Name], [t2].[OneToMany_Optional_Inverse2Id], [t2].[OneToMany_Optional_Self_Inverse2Id], [t2].[OneToMany_Required_Inverse2Id], [t2].[OneToMany_Required_Self_Inverse2Id], [t2].[OneToOne_Optional_PK_Inverse2Id], [t2].[OneToOne_Optional_Self2Id]
    FROM (
        SELECT [l1].[Id], [l1].[Date], [l1].[Level1_Optional_Id], [l1].[Level1_Required_Id], [l1].[Name], [l1].[OneToMany_Optional_Inverse2Id], [l1].[OneToMany_Optional_Self_Inverse2Id], [l1].[OneToMany_Required_Inverse2Id], [l1].[OneToMany_Required_Self_Inverse2Id], [l1].[OneToOne_Optional_PK_Inverse2Id], [l1].[OneToOne_Optional_Self2Id], ROW_NUMBER() OVER(PARTITION BY [l1].[OneToMany_Required_Inverse2Id] ORDER BY [l1].[Name] DESC) AS [row]
        FROM [LevelTwo] AS [l1]
        WHERE [l1].[Name] <> N'Bar' OR ([l1].[Name] IS NULL)
    ) AS [t2]
    WHERE 1 < [t2].[row]
) AS [t1] ON [l].[Id] = [t1].[OneToMany_Required_Inverse2Id]
ORDER BY [l].[Id], [t0].[OneToMany_Optional_Inverse2Id], [t0].[Name], [t0].[Id], [t1].[OneToMany_Required_Inverse2Id], [t1].[Name] DESC
""");
    }

    public override async Task Filtered_include_after_different_filtered_include_different_level(bool async)
    {
        await base.Filtered_include_after_different_filtered_include_different_level(async);

        AssertSql(
"""
SELECT [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id], [t2].[Id], [t2].[Date], [t2].[Level1_Optional_Id], [t2].[Level1_Required_Id], [t2].[Name], [t2].[OneToMany_Optional_Inverse2Id], [t2].[OneToMany_Optional_Self_Inverse2Id], [t2].[OneToMany_Required_Inverse2Id], [t2].[OneToMany_Required_Self_Inverse2Id], [t2].[OneToOne_Optional_PK_Inverse2Id], [t2].[OneToOne_Optional_Self2Id], [t2].[Id0], [t2].[Level2_Optional_Id], [t2].[Level2_Required_Id], [t2].[Name0], [t2].[OneToMany_Optional_Inverse3Id], [t2].[OneToMany_Optional_Self_Inverse3Id], [t2].[OneToMany_Required_Inverse3Id], [t2].[OneToMany_Required_Self_Inverse3Id], [t2].[OneToOne_Optional_PK_Inverse3Id], [t2].[OneToOne_Optional_Self3Id]
FROM [LevelOne] AS [l]
OUTER APPLY (
    SELECT [t].[Id], [t].[Date], [t].[Level1_Optional_Id], [t].[Level1_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse2Id], [t].[OneToMany_Optional_Self_Inverse2Id], [t].[OneToMany_Required_Inverse2Id], [t].[OneToMany_Required_Self_Inverse2Id], [t].[OneToOne_Optional_PK_Inverse2Id], [t].[OneToOne_Optional_Self2Id], [t0].[Id] AS [Id0], [t0].[Level2_Optional_Id], [t0].[Level2_Required_Id], [t0].[Name] AS [Name0], [t0].[OneToMany_Optional_Inverse3Id], [t0].[OneToMany_Optional_Self_Inverse3Id], [t0].[OneToMany_Required_Inverse3Id], [t0].[OneToMany_Required_Self_Inverse3Id], [t0].[OneToOne_Optional_PK_Inverse3Id], [t0].[OneToOne_Optional_Self3Id]
    FROM (
        SELECT TOP(3) [l0].[Id], [l0].[Date], [l0].[Level1_Optional_Id], [l0].[Level1_Required_Id], [l0].[Name], [l0].[OneToMany_Optional_Inverse2Id], [l0].[OneToMany_Optional_Self_Inverse2Id], [l0].[OneToMany_Required_Inverse2Id], [l0].[OneToMany_Required_Self_Inverse2Id], [l0].[OneToOne_Optional_PK_Inverse2Id], [l0].[OneToOne_Optional_Self2Id]
        FROM [LevelTwo] AS [l0]
        WHERE [l].[Id] = [l0].[OneToMany_Optional_Inverse2Id] AND ([l0].[Name] <> N'Foo' OR ([l0].[Name] IS NULL))
        ORDER BY [l0].[Name]
    ) AS [t]
    LEFT JOIN (
        SELECT [t1].[Id], [t1].[Level2_Optional_Id], [t1].[Level2_Required_Id], [t1].[Name], [t1].[OneToMany_Optional_Inverse3Id], [t1].[OneToMany_Optional_Self_Inverse3Id], [t1].[OneToMany_Required_Inverse3Id], [t1].[OneToMany_Required_Self_Inverse3Id], [t1].[OneToOne_Optional_PK_Inverse3Id], [t1].[OneToOne_Optional_Self3Id]
        FROM (
            SELECT [l1].[Id], [l1].[Level2_Optional_Id], [l1].[Level2_Required_Id], [l1].[Name], [l1].[OneToMany_Optional_Inverse3Id], [l1].[OneToMany_Optional_Self_Inverse3Id], [l1].[OneToMany_Required_Inverse3Id], [l1].[OneToMany_Required_Self_Inverse3Id], [l1].[OneToOne_Optional_PK_Inverse3Id], [l1].[OneToOne_Optional_Self3Id], ROW_NUMBER() OVER(PARTITION BY [l1].[OneToMany_Required_Inverse3Id] ORDER BY [l1].[Name] DESC) AS [row]
            FROM [LevelThree] AS [l1]
            WHERE [l1].[Name] <> N'Bar' OR ([l1].[Name] IS NULL)
        ) AS [t1]
        WHERE 1 < [t1].[row]
    ) AS [t0] ON [t].[Id] = [t0].[OneToMany_Required_Inverse3Id]
) AS [t2]
ORDER BY [l].[Id], [t2].[Name], [t2].[Id], [t2].[OneToMany_Required_Inverse3Id], [t2].[Name0] DESC
""");
    }

    public override async Task Filtered_include_same_filter_set_on_same_navigation_twice(bool async)
    {
        await base.Filtered_include_same_filter_set_on_same_navigation_twice(async);

        AssertSql(
"""
SELECT [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id], [t0].[Id], [t0].[Date], [t0].[Level1_Optional_Id], [t0].[Level1_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse2Id], [t0].[OneToMany_Optional_Self_Inverse2Id], [t0].[OneToMany_Required_Inverse2Id], [t0].[OneToMany_Required_Self_Inverse2Id], [t0].[OneToOne_Optional_PK_Inverse2Id], [t0].[OneToOne_Optional_Self2Id]
FROM [LevelOne] AS [l]
LEFT JOIN (
    SELECT [t].[Id], [t].[Date], [t].[Level1_Optional_Id], [t].[Level1_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse2Id], [t].[OneToMany_Optional_Self_Inverse2Id], [t].[OneToMany_Required_Inverse2Id], [t].[OneToMany_Required_Self_Inverse2Id], [t].[OneToOne_Optional_PK_Inverse2Id], [t].[OneToOne_Optional_Self2Id]
    FROM (
        SELECT [l0].[Id], [l0].[Date], [l0].[Level1_Optional_Id], [l0].[Level1_Required_Id], [l0].[Name], [l0].[OneToMany_Optional_Inverse2Id], [l0].[OneToMany_Optional_Self_Inverse2Id], [l0].[OneToMany_Required_Inverse2Id], [l0].[OneToMany_Required_Self_Inverse2Id], [l0].[OneToOne_Optional_PK_Inverse2Id], [l0].[OneToOne_Optional_Self2Id], ROW_NUMBER() OVER(PARTITION BY [l0].[OneToMany_Optional_Inverse2Id] ORDER BY [l0].[Id] DESC) AS [row]
        FROM [LevelTwo] AS [l0]
        WHERE [l0].[Name] <> N'Foo' OR ([l0].[Name] IS NULL)
    ) AS [t]
    WHERE [t].[row] <= 2
) AS [t0] ON [l].[Id] = [t0].[OneToMany_Optional_Inverse2Id]
ORDER BY [l].[Id], [t0].[OneToMany_Optional_Inverse2Id], [t0].[Id] DESC
""");
    }

    public override async Task Filtered_include_same_filter_set_on_same_navigation_twice_followed_by_ThenIncludes(bool async)
    {
        await base.Filtered_include_same_filter_set_on_same_navigation_twice_followed_by_ThenIncludes(async);

        AssertSql(
"""
SELECT [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id], [t0].[Id], [t0].[Date], [t0].[Level1_Optional_Id], [t0].[Level1_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse2Id], [t0].[OneToMany_Optional_Self_Inverse2Id], [t0].[OneToMany_Required_Inverse2Id], [t0].[OneToMany_Required_Self_Inverse2Id], [t0].[OneToOne_Optional_PK_Inverse2Id], [t0].[OneToOne_Optional_Self2Id], [t0].[Id0], [t0].[Id1], [t0].[Level2_Optional_Id], [t0].[Level2_Required_Id], [t0].[Name0], [t0].[OneToMany_Optional_Inverse3Id], [t0].[OneToMany_Optional_Self_Inverse3Id], [t0].[OneToMany_Required_Inverse3Id], [t0].[OneToMany_Required_Self_Inverse3Id], [t0].[OneToOne_Optional_PK_Inverse3Id], [t0].[OneToOne_Optional_Self3Id], [t0].[Level2_Optional_Id0], [t0].[Level2_Required_Id0], [t0].[Name1], [t0].[OneToMany_Optional_Inverse3Id0], [t0].[OneToMany_Optional_Self_Inverse3Id0], [t0].[OneToMany_Required_Inverse3Id0], [t0].[OneToMany_Required_Self_Inverse3Id0], [t0].[OneToOne_Optional_PK_Inverse3Id0], [t0].[OneToOne_Optional_Self3Id0]
FROM [LevelOne] AS [l]
OUTER APPLY (
    SELECT [t].[Id], [t].[Date], [t].[Level1_Optional_Id], [t].[Level1_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse2Id], [t].[OneToMany_Optional_Self_Inverse2Id], [t].[OneToMany_Required_Inverse2Id], [t].[OneToMany_Required_Self_Inverse2Id], [t].[OneToOne_Optional_PK_Inverse2Id], [t].[OneToOne_Optional_Self2Id], [l0].[Id] AS [Id0], [l1].[Id] AS [Id1], [l1].[Level2_Optional_Id], [l1].[Level2_Required_Id], [l1].[Name] AS [Name0], [l1].[OneToMany_Optional_Inverse3Id], [l1].[OneToMany_Optional_Self_Inverse3Id], [l1].[OneToMany_Required_Inverse3Id], [l1].[OneToMany_Required_Self_Inverse3Id], [l1].[OneToOne_Optional_PK_Inverse3Id], [l1].[OneToOne_Optional_Self3Id], [l0].[Level2_Optional_Id] AS [Level2_Optional_Id0], [l0].[Level2_Required_Id] AS [Level2_Required_Id0], [l0].[Name] AS [Name1], [l0].[OneToMany_Optional_Inverse3Id] AS [OneToMany_Optional_Inverse3Id0], [l0].[OneToMany_Optional_Self_Inverse3Id] AS [OneToMany_Optional_Self_Inverse3Id0], [l0].[OneToMany_Required_Inverse3Id] AS [OneToMany_Required_Inverse3Id0], [l0].[OneToMany_Required_Self_Inverse3Id] AS [OneToMany_Required_Self_Inverse3Id0], [l0].[OneToOne_Optional_PK_Inverse3Id] AS [OneToOne_Optional_PK_Inverse3Id0], [l0].[OneToOne_Optional_Self3Id] AS [OneToOne_Optional_Self3Id0]
    FROM (
        SELECT TOP(2) [l2].[Id], [l2].[Date], [l2].[Level1_Optional_Id], [l2].[Level1_Required_Id], [l2].[Name], [l2].[OneToMany_Optional_Inverse2Id], [l2].[OneToMany_Optional_Self_Inverse2Id], [l2].[OneToMany_Required_Inverse2Id], [l2].[OneToMany_Required_Self_Inverse2Id], [l2].[OneToOne_Optional_PK_Inverse2Id], [l2].[OneToOne_Optional_Self2Id]
        FROM [LevelTwo] AS [l2]
        WHERE [l].[Id] = [l2].[OneToMany_Optional_Inverse2Id] AND ([l2].[Name] <> N'Foo' OR ([l2].[Name] IS NULL))
        ORDER BY [l2].[Id]
    ) AS [t]
    LEFT JOIN [LevelThree] AS [l0] ON [t].[Id] = [l0].[Level2_Required_Id]
    LEFT JOIN [LevelThree] AS [l1] ON [t].[Id] = [l1].[OneToMany_Optional_Inverse3Id]
) AS [t0]
ORDER BY [l].[Id], [t0].[Id], [t0].[Id0]
""");
    }

    public override async Task
        Filtered_include_multiple_multi_level_includes_with_first_level_using_filter_include_on_one_of_the_chains_only(bool async)
    {
        await base
            .Filtered_include_multiple_multi_level_includes_with_first_level_using_filter_include_on_one_of_the_chains_only(async);

        AssertSql(
"""
SELECT [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id], [t0].[Id], [t0].[Date], [t0].[Level1_Optional_Id], [t0].[Level1_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse2Id], [t0].[OneToMany_Optional_Self_Inverse2Id], [t0].[OneToMany_Required_Inverse2Id], [t0].[OneToMany_Required_Self_Inverse2Id], [t0].[OneToOne_Optional_PK_Inverse2Id], [t0].[OneToOne_Optional_Self2Id], [t0].[Id0], [t0].[Id1], [t0].[Level2_Optional_Id], [t0].[Level2_Required_Id], [t0].[Name0], [t0].[OneToMany_Optional_Inverse3Id], [t0].[OneToMany_Optional_Self_Inverse3Id], [t0].[OneToMany_Required_Inverse3Id], [t0].[OneToMany_Required_Self_Inverse3Id], [t0].[OneToOne_Optional_PK_Inverse3Id], [t0].[OneToOne_Optional_Self3Id], [t0].[Level2_Optional_Id0], [t0].[Level2_Required_Id0], [t0].[Name1], [t0].[OneToMany_Optional_Inverse3Id0], [t0].[OneToMany_Optional_Self_Inverse3Id0], [t0].[OneToMany_Required_Inverse3Id0], [t0].[OneToMany_Required_Self_Inverse3Id0], [t0].[OneToOne_Optional_PK_Inverse3Id0], [t0].[OneToOne_Optional_Self3Id0]
FROM [LevelOne] AS [l]
OUTER APPLY (
    SELECT [t].[Id], [t].[Date], [t].[Level1_Optional_Id], [t].[Level1_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse2Id], [t].[OneToMany_Optional_Self_Inverse2Id], [t].[OneToMany_Required_Inverse2Id], [t].[OneToMany_Required_Self_Inverse2Id], [t].[OneToOne_Optional_PK_Inverse2Id], [t].[OneToOne_Optional_Self2Id], [l0].[Id] AS [Id0], [l1].[Id] AS [Id1], [l1].[Level2_Optional_Id], [l1].[Level2_Required_Id], [l1].[Name] AS [Name0], [l1].[OneToMany_Optional_Inverse3Id], [l1].[OneToMany_Optional_Self_Inverse3Id], [l1].[OneToMany_Required_Inverse3Id], [l1].[OneToMany_Required_Self_Inverse3Id], [l1].[OneToOne_Optional_PK_Inverse3Id], [l1].[OneToOne_Optional_Self3Id], [l0].[Level2_Optional_Id] AS [Level2_Optional_Id0], [l0].[Level2_Required_Id] AS [Level2_Required_Id0], [l0].[Name] AS [Name1], [l0].[OneToMany_Optional_Inverse3Id] AS [OneToMany_Optional_Inverse3Id0], [l0].[OneToMany_Optional_Self_Inverse3Id] AS [OneToMany_Optional_Self_Inverse3Id0], [l0].[OneToMany_Required_Inverse3Id] AS [OneToMany_Required_Inverse3Id0], [l0].[OneToMany_Required_Self_Inverse3Id] AS [OneToMany_Required_Self_Inverse3Id0], [l0].[OneToOne_Optional_PK_Inverse3Id] AS [OneToOne_Optional_PK_Inverse3Id0], [l0].[OneToOne_Optional_Self3Id] AS [OneToOne_Optional_Self3Id0]
    FROM (
        SELECT TOP(2) [l2].[Id], [l2].[Date], [l2].[Level1_Optional_Id], [l2].[Level1_Required_Id], [l2].[Name], [l2].[OneToMany_Optional_Inverse2Id], [l2].[OneToMany_Optional_Self_Inverse2Id], [l2].[OneToMany_Required_Inverse2Id], [l2].[OneToMany_Required_Self_Inverse2Id], [l2].[OneToOne_Optional_PK_Inverse2Id], [l2].[OneToOne_Optional_Self2Id]
        FROM [LevelTwo] AS [l2]
        WHERE [l].[Id] = [l2].[OneToMany_Optional_Inverse2Id] AND ([l2].[Name] <> N'Foo' OR ([l2].[Name] IS NULL))
        ORDER BY [l2].[Id]
    ) AS [t]
    LEFT JOIN [LevelThree] AS [l0] ON [t].[Id] = [l0].[Level2_Required_Id]
    LEFT JOIN [LevelThree] AS [l1] ON [t].[Id] = [l1].[OneToMany_Optional_Inverse3Id]
) AS [t0]
ORDER BY [l].[Id], [t0].[Id], [t0].[Id0]
""");
    }

    public override async Task Filtered_include_and_non_filtered_include_on_same_navigation1(bool async)
    {
        await base.Filtered_include_and_non_filtered_include_on_same_navigation1(async);

        AssertSql(
"""
SELECT [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id], [t0].[Id], [t0].[Date], [t0].[Level1_Optional_Id], [t0].[Level1_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse2Id], [t0].[OneToMany_Optional_Self_Inverse2Id], [t0].[OneToMany_Required_Inverse2Id], [t0].[OneToMany_Required_Self_Inverse2Id], [t0].[OneToOne_Optional_PK_Inverse2Id], [t0].[OneToOne_Optional_Self2Id]
FROM [LevelOne] AS [l]
LEFT JOIN (
    SELECT [t].[Id], [t].[Date], [t].[Level1_Optional_Id], [t].[Level1_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse2Id], [t].[OneToMany_Optional_Self_Inverse2Id], [t].[OneToMany_Required_Inverse2Id], [t].[OneToMany_Required_Self_Inverse2Id], [t].[OneToOne_Optional_PK_Inverse2Id], [t].[OneToOne_Optional_Self2Id]
    FROM (
        SELECT [l0].[Id], [l0].[Date], [l0].[Level1_Optional_Id], [l0].[Level1_Required_Id], [l0].[Name], [l0].[OneToMany_Optional_Inverse2Id], [l0].[OneToMany_Optional_Self_Inverse2Id], [l0].[OneToMany_Required_Inverse2Id], [l0].[OneToMany_Required_Self_Inverse2Id], [l0].[OneToOne_Optional_PK_Inverse2Id], [l0].[OneToOne_Optional_Self2Id], ROW_NUMBER() OVER(PARTITION BY [l0].[OneToMany_Optional_Inverse2Id] ORDER BY [l0].[Id]) AS [row]
        FROM [LevelTwo] AS [l0]
        WHERE [l0].[Name] <> N'Foo' OR ([l0].[Name] IS NULL)
    ) AS [t]
    WHERE [t].[row] <= 3
) AS [t0] ON [l].[Id] = [t0].[OneToMany_Optional_Inverse2Id]
ORDER BY [l].[Id], [t0].[OneToMany_Optional_Inverse2Id], [t0].[Id]
""");
    }

    public override async Task Filtered_include_and_non_filtered_include_on_same_navigation2(bool async)
    {
        await base.Filtered_include_and_non_filtered_include_on_same_navigation2(async);

        AssertSql(
"""
SELECT [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id], [t0].[Id], [t0].[Date], [t0].[Level1_Optional_Id], [t0].[Level1_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse2Id], [t0].[OneToMany_Optional_Self_Inverse2Id], [t0].[OneToMany_Required_Inverse2Id], [t0].[OneToMany_Required_Self_Inverse2Id], [t0].[OneToOne_Optional_PK_Inverse2Id], [t0].[OneToOne_Optional_Self2Id]
FROM [LevelOne] AS [l]
LEFT JOIN (
    SELECT [t].[Id], [t].[Date], [t].[Level1_Optional_Id], [t].[Level1_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse2Id], [t].[OneToMany_Optional_Self_Inverse2Id], [t].[OneToMany_Required_Inverse2Id], [t].[OneToMany_Required_Self_Inverse2Id], [t].[OneToOne_Optional_PK_Inverse2Id], [t].[OneToOne_Optional_Self2Id]
    FROM (
        SELECT [l0].[Id], [l0].[Date], [l0].[Level1_Optional_Id], [l0].[Level1_Required_Id], [l0].[Name], [l0].[OneToMany_Optional_Inverse2Id], [l0].[OneToMany_Optional_Self_Inverse2Id], [l0].[OneToMany_Required_Inverse2Id], [l0].[OneToMany_Required_Self_Inverse2Id], [l0].[OneToOne_Optional_PK_Inverse2Id], [l0].[OneToOne_Optional_Self2Id], ROW_NUMBER() OVER(PARTITION BY [l0].[OneToMany_Optional_Inverse2Id] ORDER BY [l0].[Id]) AS [row]
        FROM [LevelTwo] AS [l0]
        WHERE [l0].[Name] <> N'Foo' OR ([l0].[Name] IS NULL)
    ) AS [t]
    WHERE [t].[row] <= 3
) AS [t0] ON [l].[Id] = [t0].[OneToMany_Optional_Inverse2Id]
ORDER BY [l].[Id], [t0].[OneToMany_Optional_Inverse2Id], [t0].[Id]
""");
    }

    public override async Task Filtered_include_and_non_filtered_include_followed_by_then_include_on_same_navigation(bool async)
    {
        await base.Filtered_include_and_non_filtered_include_followed_by_then_include_on_same_navigation(async);

        AssertSql(
"""
SELECT [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id], [t1].[Id], [t1].[Date], [t1].[Level1_Optional_Id], [t1].[Level1_Required_Id], [t1].[Name], [t1].[OneToMany_Optional_Inverse2Id], [t1].[OneToMany_Optional_Self_Inverse2Id], [t1].[OneToMany_Required_Inverse2Id], [t1].[OneToMany_Required_Self_Inverse2Id], [t1].[OneToOne_Optional_PK_Inverse2Id], [t1].[OneToOne_Optional_Self2Id], [t1].[Id0], [t1].[Level2_Optional_Id], [t1].[Level2_Required_Id], [t1].[Name0], [t1].[OneToMany_Optional_Inverse3Id], [t1].[OneToMany_Optional_Self_Inverse3Id], [t1].[OneToMany_Required_Inverse3Id], [t1].[OneToMany_Required_Self_Inverse3Id], [t1].[OneToOne_Optional_PK_Inverse3Id], [t1].[OneToOne_Optional_Self3Id], [t1].[Id1], [t1].[Level3_Optional_Id], [t1].[Level3_Required_Id], [t1].[Name1], [t1].[OneToMany_Optional_Inverse4Id], [t1].[OneToMany_Optional_Self_Inverse4Id], [t1].[OneToMany_Required_Inverse4Id], [t1].[OneToMany_Required_Self_Inverse4Id], [t1].[OneToOne_Optional_PK_Inverse4Id], [t1].[OneToOne_Optional_Self4Id]
FROM [LevelOne] AS [l]
OUTER APPLY (
    SELECT [t].[Id], [t].[Date], [t].[Level1_Optional_Id], [t].[Level1_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse2Id], [t].[OneToMany_Optional_Self_Inverse2Id], [t].[OneToMany_Required_Inverse2Id], [t].[OneToMany_Required_Self_Inverse2Id], [t].[OneToOne_Optional_PK_Inverse2Id], [t].[OneToOne_Optional_Self2Id], [l0].[Id] AS [Id0], [l0].[Level2_Optional_Id], [l0].[Level2_Required_Id], [l0].[Name] AS [Name0], [l0].[OneToMany_Optional_Inverse3Id], [l0].[OneToMany_Optional_Self_Inverse3Id], [l0].[OneToMany_Required_Inverse3Id], [l0].[OneToMany_Required_Self_Inverse3Id], [l0].[OneToOne_Optional_PK_Inverse3Id], [l0].[OneToOne_Optional_Self3Id], [t0].[Id] AS [Id1], [t0].[Level3_Optional_Id], [t0].[Level3_Required_Id], [t0].[Name] AS [Name1], [t0].[OneToMany_Optional_Inverse4Id], [t0].[OneToMany_Optional_Self_Inverse4Id], [t0].[OneToMany_Required_Inverse4Id], [t0].[OneToMany_Required_Self_Inverse4Id], [t0].[OneToOne_Optional_PK_Inverse4Id], [t0].[OneToOne_Optional_Self4Id]
    FROM (
        SELECT TOP(1) [l1].[Id], [l1].[Date], [l1].[Level1_Optional_Id], [l1].[Level1_Required_Id], [l1].[Name], [l1].[OneToMany_Optional_Inverse2Id], [l1].[OneToMany_Optional_Self_Inverse2Id], [l1].[OneToMany_Required_Inverse2Id], [l1].[OneToMany_Required_Self_Inverse2Id], [l1].[OneToOne_Optional_PK_Inverse2Id], [l1].[OneToOne_Optional_Self2Id]
        FROM [LevelTwo] AS [l1]
        WHERE [l].[Id] = [l1].[OneToMany_Optional_Inverse2Id] AND ([l1].[Name] <> N'Foo' OR ([l1].[Name] IS NULL))
        ORDER BY [l1].[Id]
    ) AS [t]
    LEFT JOIN [LevelThree] AS [l0] ON [t].[Id] = [l0].[OneToOne_Optional_PK_Inverse3Id]
    LEFT JOIN (
        SELECT [l2].[Id], [l2].[Level3_Optional_Id], [l2].[Level3_Required_Id], [l2].[Name], [l2].[OneToMany_Optional_Inverse4Id], [l2].[OneToMany_Optional_Self_Inverse4Id], [l2].[OneToMany_Required_Inverse4Id], [l2].[OneToMany_Required_Self_Inverse4Id], [l2].[OneToOne_Optional_PK_Inverse4Id], [l2].[OneToOne_Optional_Self4Id]
        FROM [LevelFour] AS [l2]
        WHERE [l2].[Id] > 1
    ) AS [t0] ON [l0].[Id] = [t0].[OneToMany_Optional_Inverse4Id]
) AS [t1]
ORDER BY [l].[Id], [t1].[Id], [t1].[Id0]
""");
    }

    public override async Task Filtered_include_complex_three_level_with_middle_having_filter1(bool async)
    {
        await base.Filtered_include_complex_three_level_with_middle_having_filter1(async);

        AssertSql(
"""
SELECT [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id], [t1].[Id], [t1].[Date], [t1].[Level1_Optional_Id], [t1].[Level1_Required_Id], [t1].[Name], [t1].[OneToMany_Optional_Inverse2Id], [t1].[OneToMany_Optional_Self_Inverse2Id], [t1].[OneToMany_Required_Inverse2Id], [t1].[OneToMany_Required_Self_Inverse2Id], [t1].[OneToOne_Optional_PK_Inverse2Id], [t1].[OneToOne_Optional_Self2Id], [t1].[Id0], [t1].[Level2_Optional_Id], [t1].[Level2_Required_Id], [t1].[Name0], [t1].[OneToMany_Optional_Inverse3Id], [t1].[OneToMany_Optional_Self_Inverse3Id], [t1].[OneToMany_Required_Inverse3Id], [t1].[OneToMany_Required_Self_Inverse3Id], [t1].[OneToOne_Optional_PK_Inverse3Id], [t1].[OneToOne_Optional_Self3Id], [t1].[Id00], [t1].[Level3_Optional_Id], [t1].[Level3_Required_Id], [t1].[Name00], [t1].[OneToMany_Optional_Inverse4Id], [t1].[OneToMany_Optional_Self_Inverse4Id], [t1].[OneToMany_Required_Inverse4Id], [t1].[OneToMany_Required_Self_Inverse4Id], [t1].[OneToOne_Optional_PK_Inverse4Id], [t1].[OneToOne_Optional_Self4Id], [t1].[Id1], [t1].[Level3_Optional_Id0], [t1].[Level3_Required_Id0], [t1].[Name1], [t1].[OneToMany_Optional_Inverse4Id0], [t1].[OneToMany_Optional_Self_Inverse4Id0], [t1].[OneToMany_Required_Inverse4Id0], [t1].[OneToMany_Required_Self_Inverse4Id0], [t1].[OneToOne_Optional_PK_Inverse4Id0], [t1].[OneToOne_Optional_Self4Id0]
FROM [LevelOne] AS [l]
LEFT JOIN (
    SELECT [l0].[Id], [l0].[Date], [l0].[Level1_Optional_Id], [l0].[Level1_Required_Id], [l0].[Name], [l0].[OneToMany_Optional_Inverse2Id], [l0].[OneToMany_Optional_Self_Inverse2Id], [l0].[OneToMany_Required_Inverse2Id], [l0].[OneToMany_Required_Self_Inverse2Id], [l0].[OneToOne_Optional_PK_Inverse2Id], [l0].[OneToOne_Optional_Self2Id], [t0].[Id] AS [Id0], [t0].[Level2_Optional_Id], [t0].[Level2_Required_Id], [t0].[Name] AS [Name0], [t0].[OneToMany_Optional_Inverse3Id], [t0].[OneToMany_Optional_Self_Inverse3Id], [t0].[OneToMany_Required_Inverse3Id], [t0].[OneToMany_Required_Self_Inverse3Id], [t0].[OneToOne_Optional_PK_Inverse3Id], [t0].[OneToOne_Optional_Self3Id], [t0].[Id0] AS [Id00], [t0].[Level3_Optional_Id], [t0].[Level3_Required_Id], [t0].[Name0] AS [Name00], [t0].[OneToMany_Optional_Inverse4Id], [t0].[OneToMany_Optional_Self_Inverse4Id], [t0].[OneToMany_Required_Inverse4Id], [t0].[OneToMany_Required_Self_Inverse4Id], [t0].[OneToOne_Optional_PK_Inverse4Id], [t0].[OneToOne_Optional_Self4Id], [t0].[Id1], [t0].[Level3_Optional_Id0], [t0].[Level3_Required_Id0], [t0].[Name1], [t0].[OneToMany_Optional_Inverse4Id0], [t0].[OneToMany_Optional_Self_Inverse4Id0], [t0].[OneToMany_Required_Inverse4Id0], [t0].[OneToMany_Required_Self_Inverse4Id0], [t0].[OneToOne_Optional_PK_Inverse4Id0], [t0].[OneToOne_Optional_Self4Id0]
    FROM [LevelTwo] AS [l0]
    OUTER APPLY (
        SELECT [t].[Id], [t].[Level2_Optional_Id], [t].[Level2_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse3Id], [t].[OneToMany_Optional_Self_Inverse3Id], [t].[OneToMany_Required_Inverse3Id], [t].[OneToMany_Required_Self_Inverse3Id], [t].[OneToOne_Optional_PK_Inverse3Id], [t].[OneToOne_Optional_Self3Id], [l1].[Id] AS [Id0], [l1].[Level3_Optional_Id], [l1].[Level3_Required_Id], [l1].[Name] AS [Name0], [l1].[OneToMany_Optional_Inverse4Id], [l1].[OneToMany_Optional_Self_Inverse4Id], [l1].[OneToMany_Required_Inverse4Id], [l1].[OneToMany_Required_Self_Inverse4Id], [l1].[OneToOne_Optional_PK_Inverse4Id], [l1].[OneToOne_Optional_Self4Id], [l2].[Id] AS [Id1], [l2].[Level3_Optional_Id] AS [Level3_Optional_Id0], [l2].[Level3_Required_Id] AS [Level3_Required_Id0], [l2].[Name] AS [Name1], [l2].[OneToMany_Optional_Inverse4Id] AS [OneToMany_Optional_Inverse4Id0], [l2].[OneToMany_Optional_Self_Inverse4Id] AS [OneToMany_Optional_Self_Inverse4Id0], [l2].[OneToMany_Required_Inverse4Id] AS [OneToMany_Required_Inverse4Id0], [l2].[OneToMany_Required_Self_Inverse4Id] AS [OneToMany_Required_Self_Inverse4Id0], [l2].[OneToOne_Optional_PK_Inverse4Id] AS [OneToOne_Optional_PK_Inverse4Id0], [l2].[OneToOne_Optional_Self4Id] AS [OneToOne_Optional_Self4Id0]
        FROM (
            SELECT TOP(1) [l3].[Id], [l3].[Level2_Optional_Id], [l3].[Level2_Required_Id], [l3].[Name], [l3].[OneToMany_Optional_Inverse3Id], [l3].[OneToMany_Optional_Self_Inverse3Id], [l3].[OneToMany_Required_Inverse3Id], [l3].[OneToMany_Required_Self_Inverse3Id], [l3].[OneToOne_Optional_PK_Inverse3Id], [l3].[OneToOne_Optional_Self3Id]
            FROM [LevelThree] AS [l3]
            WHERE [l0].[Id] = [l3].[OneToMany_Optional_Inverse3Id] AND ([l3].[Name] <> N'Foo' OR ([l3].[Name] IS NULL))
            ORDER BY [l3].[Id]
        ) AS [t]
        LEFT JOIN [LevelFour] AS [l1] ON [t].[Id] = [l1].[OneToMany_Optional_Inverse4Id]
        LEFT JOIN [LevelFour] AS [l2] ON [t].[Id] = [l2].[OneToMany_Required_Inverse4Id]
    ) AS [t0]
) AS [t1] ON [l].[Id] = [t1].[OneToMany_Optional_Inverse2Id]
ORDER BY [l].[Id], [t1].[Id], [t1].[Id0], [t1].[Id00]
""");
    }

    public override async Task Filtered_include_complex_three_level_with_middle_having_filter2(bool async)
    {
        await base.Filtered_include_complex_three_level_with_middle_having_filter2(async);

        AssertSql(
"""
SELECT [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id], [t1].[Id], [t1].[Date], [t1].[Level1_Optional_Id], [t1].[Level1_Required_Id], [t1].[Name], [t1].[OneToMany_Optional_Inverse2Id], [t1].[OneToMany_Optional_Self_Inverse2Id], [t1].[OneToMany_Required_Inverse2Id], [t1].[OneToMany_Required_Self_Inverse2Id], [t1].[OneToOne_Optional_PK_Inverse2Id], [t1].[OneToOne_Optional_Self2Id], [t1].[Id0], [t1].[Level2_Optional_Id], [t1].[Level2_Required_Id], [t1].[Name0], [t1].[OneToMany_Optional_Inverse3Id], [t1].[OneToMany_Optional_Self_Inverse3Id], [t1].[OneToMany_Required_Inverse3Id], [t1].[OneToMany_Required_Self_Inverse3Id], [t1].[OneToOne_Optional_PK_Inverse3Id], [t1].[OneToOne_Optional_Self3Id], [t1].[Id00], [t1].[Level3_Optional_Id], [t1].[Level3_Required_Id], [t1].[Name00], [t1].[OneToMany_Optional_Inverse4Id], [t1].[OneToMany_Optional_Self_Inverse4Id], [t1].[OneToMany_Required_Inverse4Id], [t1].[OneToMany_Required_Self_Inverse4Id], [t1].[OneToOne_Optional_PK_Inverse4Id], [t1].[OneToOne_Optional_Self4Id], [t1].[Id1], [t1].[Level3_Optional_Id0], [t1].[Level3_Required_Id0], [t1].[Name1], [t1].[OneToMany_Optional_Inverse4Id0], [t1].[OneToMany_Optional_Self_Inverse4Id0], [t1].[OneToMany_Required_Inverse4Id0], [t1].[OneToMany_Required_Self_Inverse4Id0], [t1].[OneToOne_Optional_PK_Inverse4Id0], [t1].[OneToOne_Optional_Self4Id0]
FROM [LevelOne] AS [l]
LEFT JOIN (
    SELECT [l0].[Id], [l0].[Date], [l0].[Level1_Optional_Id], [l0].[Level1_Required_Id], [l0].[Name], [l0].[OneToMany_Optional_Inverse2Id], [l0].[OneToMany_Optional_Self_Inverse2Id], [l0].[OneToMany_Required_Inverse2Id], [l0].[OneToMany_Required_Self_Inverse2Id], [l0].[OneToOne_Optional_PK_Inverse2Id], [l0].[OneToOne_Optional_Self2Id], [t0].[Id] AS [Id0], [t0].[Level2_Optional_Id], [t0].[Level2_Required_Id], [t0].[Name] AS [Name0], [t0].[OneToMany_Optional_Inverse3Id], [t0].[OneToMany_Optional_Self_Inverse3Id], [t0].[OneToMany_Required_Inverse3Id], [t0].[OneToMany_Required_Self_Inverse3Id], [t0].[OneToOne_Optional_PK_Inverse3Id], [t0].[OneToOne_Optional_Self3Id], [t0].[Id0] AS [Id00], [t0].[Level3_Optional_Id], [t0].[Level3_Required_Id], [t0].[Name0] AS [Name00], [t0].[OneToMany_Optional_Inverse4Id], [t0].[OneToMany_Optional_Self_Inverse4Id], [t0].[OneToMany_Required_Inverse4Id], [t0].[OneToMany_Required_Self_Inverse4Id], [t0].[OneToOne_Optional_PK_Inverse4Id], [t0].[OneToOne_Optional_Self4Id], [t0].[Id1], [t0].[Level3_Optional_Id0], [t0].[Level3_Required_Id0], [t0].[Name1], [t0].[OneToMany_Optional_Inverse4Id0], [t0].[OneToMany_Optional_Self_Inverse4Id0], [t0].[OneToMany_Required_Inverse4Id0], [t0].[OneToMany_Required_Self_Inverse4Id0], [t0].[OneToOne_Optional_PK_Inverse4Id0], [t0].[OneToOne_Optional_Self4Id0]
    FROM [LevelTwo] AS [l0]
    OUTER APPLY (
        SELECT [t].[Id], [t].[Level2_Optional_Id], [t].[Level2_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse3Id], [t].[OneToMany_Optional_Self_Inverse3Id], [t].[OneToMany_Required_Inverse3Id], [t].[OneToMany_Required_Self_Inverse3Id], [t].[OneToOne_Optional_PK_Inverse3Id], [t].[OneToOne_Optional_Self3Id], [l1].[Id] AS [Id0], [l1].[Level3_Optional_Id], [l1].[Level3_Required_Id], [l1].[Name] AS [Name0], [l1].[OneToMany_Optional_Inverse4Id], [l1].[OneToMany_Optional_Self_Inverse4Id], [l1].[OneToMany_Required_Inverse4Id], [l1].[OneToMany_Required_Self_Inverse4Id], [l1].[OneToOne_Optional_PK_Inverse4Id], [l1].[OneToOne_Optional_Self4Id], [l2].[Id] AS [Id1], [l2].[Level3_Optional_Id] AS [Level3_Optional_Id0], [l2].[Level3_Required_Id] AS [Level3_Required_Id0], [l2].[Name] AS [Name1], [l2].[OneToMany_Optional_Inverse4Id] AS [OneToMany_Optional_Inverse4Id0], [l2].[OneToMany_Optional_Self_Inverse4Id] AS [OneToMany_Optional_Self_Inverse4Id0], [l2].[OneToMany_Required_Inverse4Id] AS [OneToMany_Required_Inverse4Id0], [l2].[OneToMany_Required_Self_Inverse4Id] AS [OneToMany_Required_Self_Inverse4Id0], [l2].[OneToOne_Optional_PK_Inverse4Id] AS [OneToOne_Optional_PK_Inverse4Id0], [l2].[OneToOne_Optional_Self4Id] AS [OneToOne_Optional_Self4Id0]
        FROM (
            SELECT TOP(1) [l3].[Id], [l3].[Level2_Optional_Id], [l3].[Level2_Required_Id], [l3].[Name], [l3].[OneToMany_Optional_Inverse3Id], [l3].[OneToMany_Optional_Self_Inverse3Id], [l3].[OneToMany_Required_Inverse3Id], [l3].[OneToMany_Required_Self_Inverse3Id], [l3].[OneToOne_Optional_PK_Inverse3Id], [l3].[OneToOne_Optional_Self3Id]
            FROM [LevelThree] AS [l3]
            WHERE [l0].[Id] = [l3].[OneToMany_Optional_Inverse3Id] AND ([l3].[Name] <> N'Foo' OR ([l3].[Name] IS NULL))
            ORDER BY [l3].[Id]
        ) AS [t]
        LEFT JOIN [LevelFour] AS [l1] ON [t].[Id] = [l1].[OneToMany_Optional_Inverse4Id]
        LEFT JOIN [LevelFour] AS [l2] ON [t].[Id] = [l2].[OneToMany_Required_Inverse4Id]
    ) AS [t0]
) AS [t1] ON [l].[Id] = [t1].[OneToMany_Optional_Inverse2Id]
ORDER BY [l].[Id], [t1].[Id], [t1].[Id0], [t1].[Id00]
""");
    }

    public override async Task Filtered_include_variable_used_inside_filter(bool async)
    {
        await base.Filtered_include_variable_used_inside_filter(async);

        AssertSql(
"""
@__prm_0='Foo' (Size = 4000)

SELECT [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id], [t0].[Id], [t0].[Date], [t0].[Level1_Optional_Id], [t0].[Level1_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse2Id], [t0].[OneToMany_Optional_Self_Inverse2Id], [t0].[OneToMany_Required_Inverse2Id], [t0].[OneToMany_Required_Self_Inverse2Id], [t0].[OneToOne_Optional_PK_Inverse2Id], [t0].[OneToOne_Optional_Self2Id]
FROM [LevelOne] AS [l]
LEFT JOIN (
    SELECT [t].[Id], [t].[Date], [t].[Level1_Optional_Id], [t].[Level1_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse2Id], [t].[OneToMany_Optional_Self_Inverse2Id], [t].[OneToMany_Required_Inverse2Id], [t].[OneToMany_Required_Self_Inverse2Id], [t].[OneToOne_Optional_PK_Inverse2Id], [t].[OneToOne_Optional_Self2Id]
    FROM (
        SELECT [l0].[Id], [l0].[Date], [l0].[Level1_Optional_Id], [l0].[Level1_Required_Id], [l0].[Name], [l0].[OneToMany_Optional_Inverse2Id], [l0].[OneToMany_Optional_Self_Inverse2Id], [l0].[OneToMany_Required_Inverse2Id], [l0].[OneToMany_Required_Self_Inverse2Id], [l0].[OneToOne_Optional_PK_Inverse2Id], [l0].[OneToOne_Optional_Self2Id], ROW_NUMBER() OVER(PARTITION BY [l0].[OneToMany_Optional_Inverse2Id] ORDER BY [l0].[Id]) AS [row]
        FROM [LevelTwo] AS [l0]
        WHERE [l0].[Name] <> @__prm_0 OR ([l0].[Name] IS NULL)
    ) AS [t]
    WHERE [t].[row] <= 3
) AS [t0] ON [l].[Id] = [t0].[OneToMany_Optional_Inverse2Id]
ORDER BY [l].[Id], [t0].[OneToMany_Optional_Inverse2Id], [t0].[Id]
""");
    }

    public override async Task Filtered_include_context_accessed_inside_filter(bool async)
    {
        await base.Filtered_include_context_accessed_inside_filter(async);

        AssertSql(
"""
SELECT COUNT(*)
FROM [LevelOne] AS [l]
""",
//
"""
@__p_0='True'

SELECT [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id], [t0].[Id], [t0].[Date], [t0].[Level1_Optional_Id], [t0].[Level1_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse2Id], [t0].[OneToMany_Optional_Self_Inverse2Id], [t0].[OneToMany_Required_Inverse2Id], [t0].[OneToMany_Required_Self_Inverse2Id], [t0].[OneToOne_Optional_PK_Inverse2Id], [t0].[OneToOne_Optional_Self2Id]
FROM [LevelOne] AS [l]
LEFT JOIN (
    SELECT [t].[Id], [t].[Date], [t].[Level1_Optional_Id], [t].[Level1_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse2Id], [t].[OneToMany_Optional_Self_Inverse2Id], [t].[OneToMany_Required_Inverse2Id], [t].[OneToMany_Required_Self_Inverse2Id], [t].[OneToOne_Optional_PK_Inverse2Id], [t].[OneToOne_Optional_Self2Id]
    FROM (
        SELECT [l0].[Id], [l0].[Date], [l0].[Level1_Optional_Id], [l0].[Level1_Required_Id], [l0].[Name], [l0].[OneToMany_Optional_Inverse2Id], [l0].[OneToMany_Optional_Self_Inverse2Id], [l0].[OneToMany_Required_Inverse2Id], [l0].[OneToMany_Required_Self_Inverse2Id], [l0].[OneToOne_Optional_PK_Inverse2Id], [l0].[OneToOne_Optional_Self2Id], ROW_NUMBER() OVER(PARTITION BY [l0].[OneToMany_Optional_Inverse2Id] ORDER BY [l0].[Id]) AS [row]
        FROM [LevelTwo] AS [l0]
        WHERE @__p_0 = CAST(1 AS bit)
    ) AS [t]
    WHERE [t].[row] <= 3
) AS [t0] ON [l].[Id] = [t0].[OneToMany_Optional_Inverse2Id]
ORDER BY [l].[Id], [t0].[OneToMany_Optional_Inverse2Id], [t0].[Id]
""");
    }

    public override async Task Filtered_include_context_accessed_inside_filter_correlated(bool async)
    {
        await base.Filtered_include_context_accessed_inside_filter_correlated(async);

        AssertSql(
"""
SELECT [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id], [t0].[Id], [t0].[Date], [t0].[Level1_Optional_Id], [t0].[Level1_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse2Id], [t0].[OneToMany_Optional_Self_Inverse2Id], [t0].[OneToMany_Required_Inverse2Id], [t0].[OneToMany_Required_Self_Inverse2Id], [t0].[OneToOne_Optional_PK_Inverse2Id], [t0].[OneToOne_Optional_Self2Id]
FROM [LevelOne] AS [l]
LEFT JOIN (
    SELECT [t].[Id], [t].[Date], [t].[Level1_Optional_Id], [t].[Level1_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse2Id], [t].[OneToMany_Optional_Self_Inverse2Id], [t].[OneToMany_Required_Inverse2Id], [t].[OneToMany_Required_Self_Inverse2Id], [t].[OneToOne_Optional_PK_Inverse2Id], [t].[OneToOne_Optional_Self2Id]
    FROM (
        SELECT [l0].[Id], [l0].[Date], [l0].[Level1_Optional_Id], [l0].[Level1_Required_Id], [l0].[Name], [l0].[OneToMany_Optional_Inverse2Id], [l0].[OneToMany_Optional_Self_Inverse2Id], [l0].[OneToMany_Required_Inverse2Id], [l0].[OneToMany_Required_Self_Inverse2Id], [l0].[OneToOne_Optional_PK_Inverse2Id], [l0].[OneToOne_Optional_Self2Id], ROW_NUMBER() OVER(PARTITION BY [l0].[OneToMany_Optional_Inverse2Id] ORDER BY [l0].[Id]) AS [row]
        FROM [LevelTwo] AS [l0]
        WHERE (
            SELECT COUNT(*)
            FROM [LevelOne] AS [l1]
            WHERE [l1].[Id] <> [l0].[Id]) > 1
    ) AS [t]
    WHERE [t].[row] <= 3
) AS [t0] ON [l].[Id] = [t0].[OneToMany_Optional_Inverse2Id]
ORDER BY [l].[Id], [t0].[OneToMany_Optional_Inverse2Id], [t0].[Id]
""");
    }

    public override async Task Filtered_include_outer_parameter_used_inside_filter(bool async)
    {
        await base.Filtered_include_outer_parameter_used_inside_filter(async);

        AssertSql(
"""
SELECT [l].[Id], [t].[Id], [t].[Date], [t].[Level1_Optional_Id], [t].[Level1_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse2Id], [t].[OneToMany_Optional_Self_Inverse2Id], [t].[OneToMany_Required_Inverse2Id], [t].[OneToMany_Required_Self_Inverse2Id], [t].[OneToOne_Optional_PK_Inverse2Id], [t].[OneToOne_Optional_Self2Id], [t].[Id0], [t].[Level2_Optional_Id], [t].[Level2_Required_Id], [t].[Name0], [t].[OneToMany_Optional_Inverse3Id], [t].[OneToMany_Optional_Self_Inverse3Id], [t].[OneToMany_Required_Inverse3Id], [t].[OneToMany_Required_Self_Inverse3Id], [t].[OneToOne_Optional_PK_Inverse3Id], [t].[OneToOne_Optional_Self3Id], [t0].[Id], [t0].[Date], [t0].[Level1_Optional_Id], [t0].[Level1_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse2Id], [t0].[OneToMany_Optional_Self_Inverse2Id], [t0].[OneToMany_Required_Inverse2Id], [t0].[OneToMany_Required_Self_Inverse2Id], [t0].[OneToOne_Optional_PK_Inverse2Id], [t0].[OneToOne_Optional_Self2Id], [t0].[Id0], [t0].[Level2_Optional_Id], [t0].[Level2_Required_Id], [t0].[Name0], [t0].[OneToMany_Optional_Inverse3Id], [t0].[OneToMany_Optional_Self_Inverse3Id], [t0].[OneToMany_Required_Inverse3Id], [t0].[OneToMany_Required_Self_Inverse3Id], [t0].[OneToOne_Optional_PK_Inverse3Id], [t0].[OneToOne_Optional_Self3Id]
FROM [LevelOne] AS [l]
OUTER APPLY (
    SELECT [l0].[Id], [l0].[Date], [l0].[Level1_Optional_Id], [l0].[Level1_Required_Id], [l0].[Name], [l0].[OneToMany_Optional_Inverse2Id], [l0].[OneToMany_Optional_Self_Inverse2Id], [l0].[OneToMany_Required_Inverse2Id], [l0].[OneToMany_Required_Self_Inverse2Id], [l0].[OneToOne_Optional_PK_Inverse2Id], [l0].[OneToOne_Optional_Self2Id], [l1].[Id] AS [Id0], [l1].[Level2_Optional_Id], [l1].[Level2_Required_Id], [l1].[Name] AS [Name0], [l1].[OneToMany_Optional_Inverse3Id], [l1].[OneToMany_Optional_Self_Inverse3Id], [l1].[OneToMany_Required_Inverse3Id], [l1].[OneToMany_Required_Self_Inverse3Id], [l1].[OneToOne_Optional_PK_Inverse3Id], [l1].[OneToOne_Optional_Self3Id]
    FROM [LevelTwo] AS [l0]
    LEFT JOIN [LevelThree] AS [l1] ON [l0].[Id] = [l1].[OneToMany_Optional_Inverse3Id]
) AS [t]
OUTER APPLY (
    SELECT [l2].[Id], [l2].[Date], [l2].[Level1_Optional_Id], [l2].[Level1_Required_Id], [l2].[Name], [l2].[OneToMany_Optional_Inverse2Id], [l2].[OneToMany_Optional_Self_Inverse2Id], [l2].[OneToMany_Required_Inverse2Id], [l2].[OneToMany_Required_Self_Inverse2Id], [l2].[OneToOne_Optional_PK_Inverse2Id], [l2].[OneToOne_Optional_Self2Id], [t1].[Id] AS [Id0], [t1].[Level2_Optional_Id], [t1].[Level2_Required_Id], [t1].[Name] AS [Name0], [t1].[OneToMany_Optional_Inverse3Id], [t1].[OneToMany_Optional_Self_Inverse3Id], [t1].[OneToMany_Required_Inverse3Id], [t1].[OneToMany_Required_Self_Inverse3Id], [t1].[OneToOne_Optional_PK_Inverse3Id], [t1].[OneToOne_Optional_Self3Id]
    FROM [LevelTwo] AS [l2]
    LEFT JOIN (
        SELECT [l3].[Id], [l3].[Level2_Optional_Id], [l3].[Level2_Required_Id], [l3].[Name], [l3].[OneToMany_Optional_Inverse3Id], [l3].[OneToMany_Optional_Self_Inverse3Id], [l3].[OneToMany_Required_Inverse3Id], [l3].[OneToMany_Required_Self_Inverse3Id], [l3].[OneToOne_Optional_PK_Inverse3Id], [l3].[OneToOne_Optional_Self3Id]
        FROM [LevelThree] AS [l3]
        WHERE [l3].[Id] <> [l].[Id]
    ) AS [t1] ON [l2].[Id] = [t1].[OneToMany_Optional_Inverse3Id]
) AS [t0]
ORDER BY [l].[Id], [t].[Id], [t].[Id0], [t0].[Id]
""");
    }

    public override async Task Complex_query_with_let_collection_projection_FirstOrDefault(bool async)
    {
        await base.Complex_query_with_let_collection_projection_FirstOrDefault(async);

        AssertSql(
"""
SELECT [l].[Id], [t0].[Id], [t1].[Name], [t1].[Id], [t0].[c]
FROM [LevelOne] AS [l]
LEFT JOIN (
    SELECT [t].[c], [t].[Id], [t].[OneToMany_Optional_Inverse2Id]
    FROM (
        SELECT 1 AS [c], [l0].[Id], [l0].[OneToMany_Optional_Inverse2Id], ROW_NUMBER() OVER(PARTITION BY [l0].[OneToMany_Optional_Inverse2Id] ORDER BY [l0].[Id]) AS [row]
        FROM [LevelTwo] AS [l0]
        WHERE [l0].[Name] <> N'Foo' OR ([l0].[Name] IS NULL)
    ) AS [t]
    WHERE [t].[row] <= 1
) AS [t0] ON [l].[Id] = [t0].[OneToMany_Optional_Inverse2Id]
OUTER APPLY (
    SELECT [l1].[Name], [l1].[Id]
    FROM [LevelOne] AS [l1]
    WHERE EXISTS (
        SELECT 1
        FROM [LevelTwo] AS [l2]
        WHERE [l1].[Id] = [l2].[OneToMany_Optional_Inverse2Id] AND [l2].[Id] = [t0].[Id])
) AS [t1]
ORDER BY [l].[Id], [t0].[Id]
""");
    }

    public override async Task SelectMany_DefaultIfEmpty_multiple_times_with_joins_projecting_a_collection(bool async)
    {
        await base.SelectMany_DefaultIfEmpty_multiple_times_with_joins_projecting_a_collection(async);

        AssertSql(
            """
SELECT `l2`.`Id`, `l2`.`Level3_Optional_Id`, `l2`.`Level3_Required_Id`, `l2`.`Name`, `l2`.`OneToMany_Optional_Inverse4Id`, `l2`.`OneToMany_Optional_Self_Inverse4Id`, `l2`.`OneToMany_Required_Inverse4Id`, `l2`.`OneToMany_Required_Self_Inverse4Id`, `l2`.`OneToOne_Optional_PK_Inverse4Id`, `l2`.`OneToOne_Optional_Self4Id`, `l`.`Id`, `l0`.`Id`, `l1`.`Id`, `s`.`Id`, `s`.`Id0`, `s`.`Id1`, `s`.`Id2`, `s0`.`Id`, `s0`.`Id0`, `s0`.`Id1`, `s0`.`Id2`, `l11`.`Id`, `l12`.`Id`, `l13`.`Id`, `l14`.`Id`, `l16`.`Id`, `l16`.`Date`, `l16`.`Level1_Optional_Id`, `l16`.`Level1_Required_Id`, `l16`.`Name`, `l16`.`OneToMany_Optional_Inverse2Id`, `l16`.`OneToMany_Optional_Self_Inverse2Id`, `l16`.`OneToMany_Required_Inverse2Id`, `l16`.`OneToMany_Required_Self_Inverse2Id`, `l16`.`OneToOne_Optional_PK_Inverse2Id`, `l16`.`OneToOne_Optional_Self2Id`, `l14`.`Name`
FROM (((((((((`LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`Level1_Required_Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`Level2_Optional_Id`)
LEFT JOIN `LevelFour` AS `l2` ON `l1`.`Id` = `l2`.`OneToMany_Required_Inverse4Id`)
LEFT JOIN (
    SELECT `l3`.`Id`, `l4`.`Id` AS `Id0`, `l5`.`Id` AS `Id1`, `l6`.`Id` AS `Id2`
    FROM ((`LevelFour` AS `l3`
    INNER JOIN `LevelThree` AS `l4` ON `l3`.`Level3_Required_Id` = `l4`.`Id`)
    LEFT JOIN `LevelTwo` AS `l5` ON `l4`.`Level2_Optional_Id` = `l5`.`Id`)
    LEFT JOIN `LevelTwo` AS `l6` ON `l5`.`Id` = `l6`.`OneToMany_Required_Self_Inverse2Id`
) AS `s` ON `l2`.`Id` = `s`.`Id2`)
LEFT JOIN (
    SELECT `l7`.`Id`, `l8`.`Id` AS `Id0`, `l9`.`Id` AS `Id1`, `l10`.`Id` AS `Id2`, `l10`.`Level2_Optional_Id` AS `Level2_Optional_Id0`
    FROM ((`LevelFour` AS `l7`
    INNER JOIN `LevelThree` AS `l8` ON `l7`.`Level3_Required_Id` = `l8`.`Id`)
    LEFT JOIN `LevelTwo` AS `l9` ON `l8`.`Level2_Required_Id` = `l9`.`Id`)
    LEFT JOIN `LevelThree` AS `l10` ON `l9`.`Id` = `l10`.`OneToMany_Required_Inverse3Id`
    WHERE `l8`.`Level2_Required_Id` IS NOT NULL AND `l9`.`Id` IS NOT NULL
) AS `s0` ON `s`.`Id2` = `s0`.`Id2`)
LEFT JOIN `LevelThree` AS `l11` ON `l2`.`OneToMany_Optional_Inverse4Id` = `l11`.`Id`)
LEFT JOIN `LevelThree` AS `l12` ON `s`.`Id2` = `l12`.`Level2_Optional_Id`)
LEFT JOIN `LevelTwo` AS `l13` ON `s0`.`Level2_Optional_Id0` = `l13`.`Id`)
LEFT JOIN `LevelThree` AS `l14` ON `l13`.`Id` = `l14`.`Level2_Required_Id`)
LEFT JOIN (
    SELECT `l15`.`Id`, `l15`.`Date`, `l15`.`Level1_Optional_Id`, `l15`.`Level1_Required_Id`, `l15`.`Name`, `l15`.`OneToMany_Optional_Inverse2Id`, `l15`.`OneToMany_Optional_Self_Inverse2Id`, `l15`.`OneToMany_Required_Inverse2Id`, `l15`.`OneToMany_Required_Self_Inverse2Id`, `l15`.`OneToOne_Optional_PK_Inverse2Id`, `l15`.`OneToOne_Optional_Self2Id`
    FROM `LevelTwo` AS `l15`
    WHERE `l15`.`Id` <> 42
) AS `l16` ON `s`.`Id2` = `l16`.`OneToMany_Optional_Self_Inverse2Id`
WHERE (`l11`.`Name` <> 'Foo' OR `l11`.`Name` IS NULL) AND (`l2`.`Id` IS NOT NULL AND `s`.`Id2` IS NOT NULL)
ORDER BY `l12`.`Id`, `l`.`Id`, `l0`.`Id`, `l1`.`Id`, `l2`.`Id`, `s`.`Id`, `s`.`Id0`, `s`.`Id1`, `s`.`Id2`, `s0`.`Id`, `s0`.`Id0`, `s0`.`Id1`, `s0`.`Id2`, `l11`.`Id`, `l13`.`Id`, `l14`.`Id`
""");
    }

    public override async Task Take_Select_collection_Take(bool async)
    {
        await base.Take_Select_collection_Take(async);

        AssertSql(
"""
@__p_0='1'

SELECT [t].[Id], [t].[Name], [t0].[Id], [t0].[Name], [t0].[Level1Id], [t0].[Level2Id], [t0].[Id0], [t0].[Date], [t0].[Name0], [t0].[OneToMany_Optional_Self_Inverse1Id], [t0].[OneToMany_Required_Self_Inverse1Id], [t0].[OneToOne_Optional_Self1Id]
FROM (
    SELECT TOP(@__p_0) [l].[Id], [l].[Name]
    FROM [LevelOne] AS [l]
    ORDER BY [l].[Id]
) AS [t]
OUTER APPLY (
    SELECT [t1].[Id], [t1].[Name], [t1].[OneToMany_Required_Inverse2Id] AS [Level1Id], [t1].[Level1_Required_Id] AS [Level2Id], [l0].[Id] AS [Id0], [l0].[Date], [l0].[Name] AS [Name0], [l0].[OneToMany_Optional_Self_Inverse1Id], [l0].[OneToMany_Required_Self_Inverse1Id], [l0].[OneToOne_Optional_Self1Id]
    FROM (
        SELECT TOP(3) [l1].[Id], [l1].[Level1_Required_Id], [l1].[Name], [l1].[OneToMany_Required_Inverse2Id]
        FROM [LevelTwo] AS [l1]
        WHERE [t].[Id] = [l1].[OneToMany_Required_Inverse2Id]
        ORDER BY [l1].[Id]
    ) AS [t1]
    INNER JOIN [LevelOne] AS [l0] ON [t1].[Level1_Required_Id] = [l0].[Id]
) AS [t0]
ORDER BY [t].[Id], [t0].[Id]
""");
    }

    public override async Task Skip_Take_Select_collection_Skip_Take(bool async)
    {
        await base.Skip_Take_Select_collection_Skip_Take(async);

        AssertSql(
"""
@__p_0='1'

SELECT [t].[Id], [t].[Name], [t0].[Id], [t0].[Name], [t0].[Level1Id], [t0].[Level2Id], [t0].[Id0], [t0].[Date], [t0].[Name0], [t0].[OneToMany_Optional_Self_Inverse1Id], [t0].[OneToMany_Required_Self_Inverse1Id], [t0].[OneToOne_Optional_Self1Id]
FROM (
    SELECT [l].[Id], [l].[Name]
    FROM [LevelOne] AS [l]
    ORDER BY [l].[Id]
    OFFSET @__p_0 ROWS FETCH NEXT @__p_0 ROWS ONLY
) AS [t]
OUTER APPLY (
    SELECT [t1].[Id], [t1].[Name], [t1].[OneToMany_Required_Inverse2Id] AS [Level1Id], [t1].[Level1_Required_Id] AS [Level2Id], [l0].[Id] AS [Id0], [l0].[Date], [l0].[Name] AS [Name0], [l0].[OneToMany_Optional_Self_Inverse1Id], [l0].[OneToMany_Required_Self_Inverse1Id], [l0].[OneToOne_Optional_Self1Id]
    FROM (
        SELECT [l1].[Id], [l1].[Level1_Required_Id], [l1].[Name], [l1].[OneToMany_Required_Inverse2Id]
        FROM [LevelTwo] AS [l1]
        WHERE [t].[Id] = [l1].[OneToMany_Required_Inverse2Id]
        ORDER BY [l1].[Id]
        OFFSET 1 ROWS FETCH NEXT 3 ROWS ONLY
    ) AS [t1]
    INNER JOIN [LevelOne] AS [l0] ON [t1].[Level1_Required_Id] = [l0].[Id]
) AS [t0]
ORDER BY [t].[Id], [t0].[Id]
""");
    }

    public override async Task Projecting_collection_with_FirstOrDefault(bool async)
    {
        await base.Projecting_collection_with_FirstOrDefault(async);

        AssertSql(
            """
SELECT `l1`.`Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`
FROM (
    SELECT TOP 1 `l`.`Id`
    FROM `LevelOne` AS `l`
    WHERE `l`.`Id` = 1
) AS `l1`
LEFT JOIN `LevelTwo` AS `l0` ON `l1`.`Id` = `l0`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l1`.`Id`
""");
    }

    public override async Task Filtered_include_Take_with_another_Take_on_top_level(bool async)
    {
        await base.Filtered_include_Take_with_another_Take_on_top_level(async);

        AssertSql(
"""
@__p_0='5'

SELECT [t].[Id], [t].[Date], [t].[Name], [t].[OneToMany_Optional_Self_Inverse1Id], [t].[OneToMany_Required_Self_Inverse1Id], [t].[OneToOne_Optional_Self1Id], [t0].[Id], [t0].[Date], [t0].[Level1_Optional_Id], [t0].[Level1_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse2Id], [t0].[OneToMany_Optional_Self_Inverse2Id], [t0].[OneToMany_Required_Inverse2Id], [t0].[OneToMany_Required_Self_Inverse2Id], [t0].[OneToOne_Optional_PK_Inverse2Id], [t0].[OneToOne_Optional_Self2Id], [t0].[Id0], [t0].[Level2_Optional_Id], [t0].[Level2_Required_Id], [t0].[Name0], [t0].[OneToMany_Optional_Inverse3Id], [t0].[OneToMany_Optional_Self_Inverse3Id], [t0].[OneToMany_Required_Inverse3Id], [t0].[OneToMany_Required_Self_Inverse3Id], [t0].[OneToOne_Optional_PK_Inverse3Id], [t0].[OneToOne_Optional_Self3Id]
FROM (
    SELECT TOP(@__p_0) [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id]
    FROM [LevelOne] AS [l]
    ORDER BY [l].[Id]
) AS [t]
OUTER APPLY (
    SELECT [t1].[Id], [t1].[Date], [t1].[Level1_Optional_Id], [t1].[Level1_Required_Id], [t1].[Name], [t1].[OneToMany_Optional_Inverse2Id], [t1].[OneToMany_Optional_Self_Inverse2Id], [t1].[OneToMany_Required_Inverse2Id], [t1].[OneToMany_Required_Self_Inverse2Id], [t1].[OneToOne_Optional_PK_Inverse2Id], [t1].[OneToOne_Optional_Self2Id], [l0].[Id] AS [Id0], [l0].[Level2_Optional_Id], [l0].[Level2_Required_Id], [l0].[Name] AS [Name0], [l0].[OneToMany_Optional_Inverse3Id], [l0].[OneToMany_Optional_Self_Inverse3Id], [l0].[OneToMany_Required_Inverse3Id], [l0].[OneToMany_Required_Self_Inverse3Id], [l0].[OneToOne_Optional_PK_Inverse3Id], [l0].[OneToOne_Optional_Self3Id]
    FROM (
        SELECT TOP(4) [l1].[Id], [l1].[Date], [l1].[Level1_Optional_Id], [l1].[Level1_Required_Id], [l1].[Name], [l1].[OneToMany_Optional_Inverse2Id], [l1].[OneToMany_Optional_Self_Inverse2Id], [l1].[OneToMany_Required_Inverse2Id], [l1].[OneToMany_Required_Self_Inverse2Id], [l1].[OneToOne_Optional_PK_Inverse2Id], [l1].[OneToOne_Optional_Self2Id]
        FROM [LevelTwo] AS [l1]
        WHERE [t].[Id] = [l1].[OneToMany_Optional_Inverse2Id]
        ORDER BY [l1].[Name] DESC
    ) AS [t1]
    LEFT JOIN [LevelThree] AS [l0] ON [t1].[Id] = [l0].[Level2_Optional_Id]
) AS [t0]
ORDER BY [t].[Id], [t0].[Name] DESC, [t0].[Id]
""");
    }

    public override async Task Filtered_include_Skip_Take_with_another_Skip_Take_on_top_level(bool async)
    {
        await base.Filtered_include_Skip_Take_with_another_Skip_Take_on_top_level(async);

        AssertSql(
"""
@__p_0='10'
@__p_1='5'

SELECT [t].[Id], [t].[Date], [t].[Name], [t].[OneToMany_Optional_Self_Inverse1Id], [t].[OneToMany_Required_Self_Inverse1Id], [t].[OneToOne_Optional_Self1Id], [t0].[Id], [t0].[Date], [t0].[Level1_Optional_Id], [t0].[Level1_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse2Id], [t0].[OneToMany_Optional_Self_Inverse2Id], [t0].[OneToMany_Required_Inverse2Id], [t0].[OneToMany_Required_Self_Inverse2Id], [t0].[OneToOne_Optional_PK_Inverse2Id], [t0].[OneToOne_Optional_Self2Id], [t0].[Id0], [t0].[Level2_Optional_Id], [t0].[Level2_Required_Id], [t0].[Name0], [t0].[OneToMany_Optional_Inverse3Id], [t0].[OneToMany_Optional_Self_Inverse3Id], [t0].[OneToMany_Required_Inverse3Id], [t0].[OneToMany_Required_Self_Inverse3Id], [t0].[OneToOne_Optional_PK_Inverse3Id], [t0].[OneToOne_Optional_Self3Id]
FROM (
    SELECT [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id]
    FROM [LevelOne] AS [l]
    ORDER BY [l].[Id] DESC
    OFFSET @__p_0 ROWS FETCH NEXT @__p_1 ROWS ONLY
) AS [t]
OUTER APPLY (
    SELECT [t1].[Id], [t1].[Date], [t1].[Level1_Optional_Id], [t1].[Level1_Required_Id], [t1].[Name], [t1].[OneToMany_Optional_Inverse2Id], [t1].[OneToMany_Optional_Self_Inverse2Id], [t1].[OneToMany_Required_Inverse2Id], [t1].[OneToMany_Required_Self_Inverse2Id], [t1].[OneToOne_Optional_PK_Inverse2Id], [t1].[OneToOne_Optional_Self2Id], [l0].[Id] AS [Id0], [l0].[Level2_Optional_Id], [l0].[Level2_Required_Id], [l0].[Name] AS [Name0], [l0].[OneToMany_Optional_Inverse3Id], [l0].[OneToMany_Optional_Self_Inverse3Id], [l0].[OneToMany_Required_Inverse3Id], [l0].[OneToMany_Required_Self_Inverse3Id], [l0].[OneToOne_Optional_PK_Inverse3Id], [l0].[OneToOne_Optional_Self3Id]
    FROM (
        SELECT [l1].[Id], [l1].[Date], [l1].[Level1_Optional_Id], [l1].[Level1_Required_Id], [l1].[Name], [l1].[OneToMany_Optional_Inverse2Id], [l1].[OneToMany_Optional_Self_Inverse2Id], [l1].[OneToMany_Required_Inverse2Id], [l1].[OneToMany_Required_Self_Inverse2Id], [l1].[OneToOne_Optional_PK_Inverse2Id], [l1].[OneToOne_Optional_Self2Id]
        FROM [LevelTwo] AS [l1]
        WHERE [t].[Id] = [l1].[OneToMany_Optional_Inverse2Id]
        ORDER BY [l1].[Name] DESC
        OFFSET 2 ROWS FETCH NEXT 4 ROWS ONLY
    ) AS [t1]
    LEFT JOIN [LevelThree] AS [l0] ON [t1].[Id] = [l0].[Level2_Optional_Id]
) AS [t0]
ORDER BY [t].[Id] DESC, [t0].[Name] DESC, [t0].[Id]
""");
    }

    public override async Task Filtered_include_with_Take_without_order_by_followed_by_ThenInclude_and_FirstOrDefault_on_top_level(
        bool async)
    {
        await base.Filtered_include_with_Take_without_order_by_followed_by_ThenInclude_and_FirstOrDefault_on_top_level(async);

        AssertSql(
"""
SELECT [t].[Id], [t].[Date], [t].[Name], [t].[OneToMany_Optional_Self_Inverse1Id], [t].[OneToMany_Required_Self_Inverse1Id], [t].[OneToOne_Optional_Self1Id], [t0].[Id], [t0].[Date], [t0].[Level1_Optional_Id], [t0].[Level1_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse2Id], [t0].[OneToMany_Optional_Self_Inverse2Id], [t0].[OneToMany_Required_Inverse2Id], [t0].[OneToMany_Required_Self_Inverse2Id], [t0].[OneToOne_Optional_PK_Inverse2Id], [t0].[OneToOne_Optional_Self2Id], [t0].[Id0], [t0].[Level2_Optional_Id], [t0].[Level2_Required_Id], [t0].[Name0], [t0].[OneToMany_Optional_Inverse3Id], [t0].[OneToMany_Optional_Self_Inverse3Id], [t0].[OneToMany_Required_Inverse3Id], [t0].[OneToMany_Required_Self_Inverse3Id], [t0].[OneToOne_Optional_PK_Inverse3Id], [t0].[OneToOne_Optional_Self3Id]
FROM (
    SELECT TOP(1) [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id]
    FROM [LevelOne] AS [l]
    ORDER BY [l].[Id]
) AS [t]
OUTER APPLY (
    SELECT [t1].[Id], [t1].[Date], [t1].[Level1_Optional_Id], [t1].[Level1_Required_Id], [t1].[Name], [t1].[OneToMany_Optional_Inverse2Id], [t1].[OneToMany_Optional_Self_Inverse2Id], [t1].[OneToMany_Required_Inverse2Id], [t1].[OneToMany_Required_Self_Inverse2Id], [t1].[OneToOne_Optional_PK_Inverse2Id], [t1].[OneToOne_Optional_Self2Id], [l0].[Id] AS [Id0], [l0].[Level2_Optional_Id], [l0].[Level2_Required_Id], [l0].[Name] AS [Name0], [l0].[OneToMany_Optional_Inverse3Id], [l0].[OneToMany_Optional_Self_Inverse3Id], [l0].[OneToMany_Required_Inverse3Id], [l0].[OneToMany_Required_Self_Inverse3Id], [l0].[OneToOne_Optional_PK_Inverse3Id], [l0].[OneToOne_Optional_Self3Id]
    FROM (
        SELECT TOP(40) [l1].[Id], [l1].[Date], [l1].[Level1_Optional_Id], [l1].[Level1_Required_Id], [l1].[Name], [l1].[OneToMany_Optional_Inverse2Id], [l1].[OneToMany_Optional_Self_Inverse2Id], [l1].[OneToMany_Required_Inverse2Id], [l1].[OneToMany_Required_Self_Inverse2Id], [l1].[OneToOne_Optional_PK_Inverse2Id], [l1].[OneToOne_Optional_Self2Id]
        FROM [LevelTwo] AS [l1]
        WHERE [t].[Id] = [l1].[OneToMany_Optional_Inverse2Id]
    ) AS [t1]
    LEFT JOIN [LevelThree] AS [l0] ON [t1].[Id] = [l0].[Level2_Optional_Id]
) AS [t0]
ORDER BY [t].[Id], [t0].[Id]
""");
    }

    public override async Task Filtered_include_with_Take_without_order_by_followed_by_ThenInclude_and_unordered_Take_on_top_level(
        bool async)
    {
        await base.Filtered_include_with_Take_without_order_by_followed_by_ThenInclude_and_unordered_Take_on_top_level(async);

        AssertSql(
"""
@__p_0='30'

SELECT [t].[Id], [t].[Date], [t].[Name], [t].[OneToMany_Optional_Self_Inverse1Id], [t].[OneToMany_Required_Self_Inverse1Id], [t].[OneToOne_Optional_Self1Id], [t0].[Id], [t0].[Date], [t0].[Level1_Optional_Id], [t0].[Level1_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse2Id], [t0].[OneToMany_Optional_Self_Inverse2Id], [t0].[OneToMany_Required_Inverse2Id], [t0].[OneToMany_Required_Self_Inverse2Id], [t0].[OneToOne_Optional_PK_Inverse2Id], [t0].[OneToOne_Optional_Self2Id], [t0].[Id0], [t0].[Level2_Optional_Id], [t0].[Level2_Required_Id], [t0].[Name0], [t0].[OneToMany_Optional_Inverse3Id], [t0].[OneToMany_Optional_Self_Inverse3Id], [t0].[OneToMany_Required_Inverse3Id], [t0].[OneToMany_Required_Self_Inverse3Id], [t0].[OneToOne_Optional_PK_Inverse3Id], [t0].[OneToOne_Optional_Self3Id]
FROM (
    SELECT TOP(@__p_0) [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id]
    FROM [LevelOne] AS [l]
    ORDER BY [l].[Id]
) AS [t]
OUTER APPLY (
    SELECT [t1].[Id], [t1].[Date], [t1].[Level1_Optional_Id], [t1].[Level1_Required_Id], [t1].[Name], [t1].[OneToMany_Optional_Inverse2Id], [t1].[OneToMany_Optional_Self_Inverse2Id], [t1].[OneToMany_Required_Inverse2Id], [t1].[OneToMany_Required_Self_Inverse2Id], [t1].[OneToOne_Optional_PK_Inverse2Id], [t1].[OneToOne_Optional_Self2Id], [l0].[Id] AS [Id0], [l0].[Level2_Optional_Id], [l0].[Level2_Required_Id], [l0].[Name] AS [Name0], [l0].[OneToMany_Optional_Inverse3Id], [l0].[OneToMany_Optional_Self_Inverse3Id], [l0].[OneToMany_Required_Inverse3Id], [l0].[OneToMany_Required_Self_Inverse3Id], [l0].[OneToOne_Optional_PK_Inverse3Id], [l0].[OneToOne_Optional_Self3Id]
    FROM (
        SELECT TOP(40) [l1].[Id], [l1].[Date], [l1].[Level1_Optional_Id], [l1].[Level1_Required_Id], [l1].[Name], [l1].[OneToMany_Optional_Inverse2Id], [l1].[OneToMany_Optional_Self_Inverse2Id], [l1].[OneToMany_Required_Inverse2Id], [l1].[OneToMany_Required_Self_Inverse2Id], [l1].[OneToOne_Optional_PK_Inverse2Id], [l1].[OneToOne_Optional_Self2Id]
        FROM [LevelTwo] AS [l1]
        WHERE [t].[Id] = [l1].[OneToMany_Optional_Inverse2Id]
    ) AS [t1]
    LEFT JOIN [LevelThree] AS [l0] ON [t1].[Id] = [l0].[Level2_Optional_Id]
) AS [t0]
ORDER BY [t].[Id], [t0].[Id]
""");
    }

    public override async Task Skip_on_grouping_element(bool async)
    {
        await base.Skip_on_grouping_element(async);

        AssertSql(
"""
SELECT [t].[Date], [t0].[Id], [t0].[Date], [t0].[Name], [t0].[OneToMany_Optional_Self_Inverse1Id], [t0].[OneToMany_Required_Self_Inverse1Id], [t0].[OneToOne_Optional_Self1Id]
FROM (
    SELECT [l].[Date]
    FROM [LevelOne] AS [l]
    GROUP BY [l].[Date]
) AS [t]
LEFT JOIN (
    SELECT [t1].[Id], [t1].[Date], [t1].[Name], [t1].[OneToMany_Optional_Self_Inverse1Id], [t1].[OneToMany_Required_Self_Inverse1Id], [t1].[OneToOne_Optional_Self1Id]
    FROM (
        SELECT [l0].[Id], [l0].[Date], [l0].[Name], [l0].[OneToMany_Optional_Self_Inverse1Id], [l0].[OneToMany_Required_Self_Inverse1Id], [l0].[OneToOne_Optional_Self1Id], ROW_NUMBER() OVER(PARTITION BY [l0].[Date] ORDER BY [l0].[Name]) AS [row]
        FROM [LevelOne] AS [l0]
    ) AS [t1]
    WHERE 1 < [t1].[row]
) AS [t0] ON [t].[Date] = [t0].[Date]
ORDER BY [t].[Date], [t0].[Date], [t0].[Name]
""");
    }

    public override async Task Take_on_grouping_element(bool async)
    {
        await base.Take_on_grouping_element(async);

        AssertSql(
"""
SELECT [t].[Date], [t0].[Id], [t0].[Date], [t0].[Name], [t0].[OneToMany_Optional_Self_Inverse1Id], [t0].[OneToMany_Required_Self_Inverse1Id], [t0].[OneToOne_Optional_Self1Id]
FROM (
    SELECT [l].[Date]
    FROM [LevelOne] AS [l]
    GROUP BY [l].[Date]
) AS [t]
LEFT JOIN (
    SELECT [t1].[Id], [t1].[Date], [t1].[Name], [t1].[OneToMany_Optional_Self_Inverse1Id], [t1].[OneToMany_Required_Self_Inverse1Id], [t1].[OneToOne_Optional_Self1Id]
    FROM (
        SELECT [l0].[Id], [l0].[Date], [l0].[Name], [l0].[OneToMany_Optional_Self_Inverse1Id], [l0].[OneToMany_Required_Self_Inverse1Id], [l0].[OneToOne_Optional_Self1Id], ROW_NUMBER() OVER(PARTITION BY [l0].[Date] ORDER BY [l0].[Name] DESC) AS [row]
        FROM [LevelOne] AS [l0]
    ) AS [t1]
    WHERE [t1].[row] <= 10
) AS [t0] ON [t].[Date] = [t0].[Date]
ORDER BY [t].[Date], [t0].[Date], [t0].[Name] DESC
""");
    }

    public override async Task Skip_Take_on_grouping_element(bool async)
    {
        await base.Skip_Take_on_grouping_element(async);

        AssertSql(
"""
SELECT [t].[Date], [t0].[Id], [t0].[Date], [t0].[Name], [t0].[OneToMany_Optional_Self_Inverse1Id], [t0].[OneToMany_Required_Self_Inverse1Id], [t0].[OneToOne_Optional_Self1Id]
FROM (
    SELECT [l].[Date]
    FROM [LevelOne] AS [l]
    GROUP BY [l].[Date]
) AS [t]
LEFT JOIN (
    SELECT [t1].[Id], [t1].[Date], [t1].[Name], [t1].[OneToMany_Optional_Self_Inverse1Id], [t1].[OneToMany_Required_Self_Inverse1Id], [t1].[OneToOne_Optional_Self1Id]
    FROM (
        SELECT [l0].[Id], [l0].[Date], [l0].[Name], [l0].[OneToMany_Optional_Self_Inverse1Id], [l0].[OneToMany_Required_Self_Inverse1Id], [l0].[OneToOne_Optional_Self1Id], ROW_NUMBER() OVER(PARTITION BY [l0].[Date] ORDER BY [l0].[Name]) AS [row]
        FROM [LevelOne] AS [l0]
    ) AS [t1]
    WHERE 1 < [t1].[row] AND [t1].[row] <= 6
) AS [t0] ON [t].[Date] = [t0].[Date]
ORDER BY [t].[Date], [t0].[Date], [t0].[Name]
""");
    }

    public override async Task Skip_Take_Distinct_on_grouping_element(bool async)
    {
        await base.Skip_Take_Distinct_on_grouping_element(async);

        AssertSql(
"""
SELECT [t].[Date], [t0].[Id], [t0].[Date], [t0].[Name], [t0].[OneToMany_Optional_Self_Inverse1Id], [t0].[OneToMany_Required_Self_Inverse1Id], [t0].[OneToOne_Optional_Self1Id]
FROM (
    SELECT [l].[Date]
    FROM [LevelOne] AS [l]
    GROUP BY [l].[Date]
) AS [t]
OUTER APPLY (
    SELECT DISTINCT [t1].[Id], [t1].[Date], [t1].[Name], [t1].[OneToMany_Optional_Self_Inverse1Id], [t1].[OneToMany_Required_Self_Inverse1Id], [t1].[OneToOne_Optional_Self1Id]
    FROM (
        SELECT [l0].[Id], [l0].[Date], [l0].[Name], [l0].[OneToMany_Optional_Self_Inverse1Id], [l0].[OneToMany_Required_Self_Inverse1Id], [l0].[OneToOne_Optional_Self1Id]
        FROM [LevelOne] AS [l0]
        WHERE [t].[Date] = [l0].[Date]
        ORDER BY [l0].[Name]
        OFFSET 1 ROWS FETCH NEXT 5 ROWS ONLY
    ) AS [t1]
) AS [t0]
ORDER BY [t].[Date]
""");
    }

    public override async Task Skip_Take_ToList_on_grouping_element(bool async)
    {
        await base.Skip_Take_ToList_on_grouping_element(async);

        AssertSql(
"""
SELECT [t].[Date], [t0].[Id], [t0].[Date], [t0].[Name], [t0].[OneToMany_Optional_Self_Inverse1Id], [t0].[OneToMany_Required_Self_Inverse1Id], [t0].[OneToOne_Optional_Self1Id]
FROM (
    SELECT [l].[Date]
    FROM [LevelOne] AS [l]
    GROUP BY [l].[Date]
) AS [t]
LEFT JOIN (
    SELECT [t1].[Id], [t1].[Date], [t1].[Name], [t1].[OneToMany_Optional_Self_Inverse1Id], [t1].[OneToMany_Required_Self_Inverse1Id], [t1].[OneToOne_Optional_Self1Id]
    FROM (
        SELECT [l0].[Id], [l0].[Date], [l0].[Name], [l0].[OneToMany_Optional_Self_Inverse1Id], [l0].[OneToMany_Required_Self_Inverse1Id], [l0].[OneToOne_Optional_Self1Id], ROW_NUMBER() OVER(PARTITION BY [l0].[Date] ORDER BY [l0].[Name]) AS [row]
        FROM [LevelOne] AS [l0]
    ) AS [t1]
    WHERE 1 < [t1].[row] AND [t1].[row] <= 6
) AS [t0] ON [t].[Date] = [t0].[Date]
ORDER BY [t].[Date], [t0].[Date], [t0].[Name]
""");
    }

    public override async Task Skip_Take_on_grouping_element_into_non_entity(bool async)
    {
        await base.Skip_Take_on_grouping_element_into_non_entity(async);

        AssertSql(
"""
SELECT [t].[Date], [t0].[Name], [t0].[Id]
FROM (
    SELECT [l].[Date]
    FROM [LevelOne] AS [l]
    GROUP BY [l].[Date]
) AS [t]
LEFT JOIN (
    SELECT [t1].[Name], [t1].[Id], [t1].[Date]
    FROM (
        SELECT [l0].[Name], [l0].[Id], [l0].[Date], ROW_NUMBER() OVER(PARTITION BY [l0].[Date] ORDER BY [l0].[Name]) AS [row]
        FROM [LevelOne] AS [l0]
    ) AS [t1]
    WHERE 1 < [t1].[row] AND [t1].[row] <= 6
) AS [t0] ON [t].[Date] = [t0].[Date]
ORDER BY [t].[Date], [t0].[Date], [t0].[Name]
""");
    }

    public override async Task Skip_Take_on_grouping_element_with_collection_include(bool async)
    {
        await base.Skip_Take_on_grouping_element_with_collection_include(async);

        AssertSql(
"""
SELECT [t].[Date], [t0].[Id], [t0].[Date], [t0].[Name], [t0].[OneToMany_Optional_Self_Inverse1Id], [t0].[OneToMany_Required_Self_Inverse1Id], [t0].[OneToOne_Optional_Self1Id], [t0].[Id0], [t0].[Date0], [t0].[Level1_Optional_Id], [t0].[Level1_Required_Id], [t0].[Name0], [t0].[OneToMany_Optional_Inverse2Id], [t0].[OneToMany_Optional_Self_Inverse2Id], [t0].[OneToMany_Required_Inverse2Id], [t0].[OneToMany_Required_Self_Inverse2Id], [t0].[OneToOne_Optional_PK_Inverse2Id], [t0].[OneToOne_Optional_Self2Id]
FROM (
    SELECT [l].[Date]
    FROM [LevelOne] AS [l]
    GROUP BY [l].[Date]
) AS [t]
OUTER APPLY (
    SELECT [t1].[Id], [t1].[Date], [t1].[Name], [t1].[OneToMany_Optional_Self_Inverse1Id], [t1].[OneToMany_Required_Self_Inverse1Id], [t1].[OneToOne_Optional_Self1Id], [l0].[Id] AS [Id0], [l0].[Date] AS [Date0], [l0].[Level1_Optional_Id], [l0].[Level1_Required_Id], [l0].[Name] AS [Name0], [l0].[OneToMany_Optional_Inverse2Id], [l0].[OneToMany_Optional_Self_Inverse2Id], [l0].[OneToMany_Required_Inverse2Id], [l0].[OneToMany_Required_Self_Inverse2Id], [l0].[OneToOne_Optional_PK_Inverse2Id], [l0].[OneToOne_Optional_Self2Id]
    FROM (
        SELECT [l1].[Id], [l1].[Date], [l1].[Name], [l1].[OneToMany_Optional_Self_Inverse1Id], [l1].[OneToMany_Required_Self_Inverse1Id], [l1].[OneToOne_Optional_Self1Id]
        FROM [LevelOne] AS [l1]
        WHERE [t].[Date] = [l1].[Date]
        ORDER BY [l1].[Name]
        OFFSET 1 ROWS FETCH NEXT 5 ROWS ONLY
    ) AS [t1]
    LEFT JOIN [LevelTwo] AS [l0] ON [t1].[Id] = [l0].[OneToMany_Optional_Inverse2Id]
) AS [t0]
ORDER BY [t].[Date], [t0].[Name], [t0].[Id]
""");
    }

    public override async Task Skip_Take_on_grouping_element_with_reference_include(bool async)
    {
        await base.Skip_Take_on_grouping_element_with_reference_include(async);

        AssertSql(
"""
SELECT [t].[Date], [t0].[Id], [t0].[Date], [t0].[Name], [t0].[OneToMany_Optional_Self_Inverse1Id], [t0].[OneToMany_Required_Self_Inverse1Id], [t0].[OneToOne_Optional_Self1Id], [t0].[Id0], [t0].[Date0], [t0].[Level1_Optional_Id], [t0].[Level1_Required_Id], [t0].[Name0], [t0].[OneToMany_Optional_Inverse2Id], [t0].[OneToMany_Optional_Self_Inverse2Id], [t0].[OneToMany_Required_Inverse2Id], [t0].[OneToMany_Required_Self_Inverse2Id], [t0].[OneToOne_Optional_PK_Inverse2Id], [t0].[OneToOne_Optional_Self2Id]
FROM (
    SELECT [l].[Date]
    FROM [LevelOne] AS [l]
    GROUP BY [l].[Date]
) AS [t]
OUTER APPLY (
    SELECT [t1].[Id], [t1].[Date], [t1].[Name], [t1].[OneToMany_Optional_Self_Inverse1Id], [t1].[OneToMany_Required_Self_Inverse1Id], [t1].[OneToOne_Optional_Self1Id], [l0].[Id] AS [Id0], [l0].[Date] AS [Date0], [l0].[Level1_Optional_Id], [l0].[Level1_Required_Id], [l0].[Name] AS [Name0], [l0].[OneToMany_Optional_Inverse2Id], [l0].[OneToMany_Optional_Self_Inverse2Id], [l0].[OneToMany_Required_Inverse2Id], [l0].[OneToMany_Required_Self_Inverse2Id], [l0].[OneToOne_Optional_PK_Inverse2Id], [l0].[OneToOne_Optional_Self2Id]
    FROM (
        SELECT [l1].[Id], [l1].[Date], [l1].[Name], [l1].[OneToMany_Optional_Self_Inverse1Id], [l1].[OneToMany_Required_Self_Inverse1Id], [l1].[OneToOne_Optional_Self1Id]
        FROM [LevelOne] AS [l1]
        WHERE [t].[Date] = [l1].[Date]
        ORDER BY [l1].[Name]
        OFFSET 1 ROWS FETCH NEXT 5 ROWS ONLY
    ) AS [t1]
    LEFT JOIN [LevelTwo] AS [l0] ON [t1].[Id] = [l0].[Level1_Optional_Id]
) AS [t0]
ORDER BY [t].[Date], [t0].[Name], [t0].[Id]
""");
    }

    public override async Task Skip_Take_on_grouping_element_inside_collection_projection(bool async)
    {
        await base.Skip_Take_on_grouping_element_inside_collection_projection(async);

        AssertSql(
"""
SELECT [l].[Id], [t2].[Date], [t2].[Id], [t2].[Date0], [t2].[Name], [t2].[OneToMany_Optional_Self_Inverse1Id], [t2].[OneToMany_Required_Self_Inverse1Id], [t2].[OneToOne_Optional_Self1Id]
FROM [LevelOne] AS [l]
OUTER APPLY (
    SELECT [t].[Date], [t0].[Id], [t0].[Date] AS [Date0], [t0].[Name], [t0].[OneToMany_Optional_Self_Inverse1Id], [t0].[OneToMany_Required_Self_Inverse1Id], [t0].[OneToOne_Optional_Self1Id]
    FROM (
        SELECT [l0].[Date]
        FROM [LevelOne] AS [l0]
        WHERE [l0].[Name] = [l].[Name] OR (([l0].[Name] IS NULL) AND ([l].[Name] IS NULL))
        GROUP BY [l0].[Date]
    ) AS [t]
    LEFT JOIN (
        SELECT [t1].[Id], [t1].[Date], [t1].[Name], [t1].[OneToMany_Optional_Self_Inverse1Id], [t1].[OneToMany_Required_Self_Inverse1Id], [t1].[OneToOne_Optional_Self1Id]
        FROM (
            SELECT [l1].[Id], [l1].[Date], [l1].[Name], [l1].[OneToMany_Optional_Self_Inverse1Id], [l1].[OneToMany_Required_Self_Inverse1Id], [l1].[OneToOne_Optional_Self1Id], ROW_NUMBER() OVER(PARTITION BY [l1].[Date] ORDER BY [l1].[Name]) AS [row]
            FROM [LevelOne] AS [l1]
            WHERE [l1].[Name] = [l].[Name] OR (([l1].[Name] IS NULL) AND ([l].[Name] IS NULL))
        ) AS [t1]
        WHERE 1 < [t1].[row] AND [t1].[row] <= 6
    ) AS [t0] ON [t].[Date] = [t0].[Date]
) AS [t2]
ORDER BY [l].[Id], [t2].[Date], [t2].[Date0], [t2].[Name]
""");
    }

    public override async Task Collection_projection_over_GroupBy_over_parameter(bool async)
    {
        await base.Collection_projection_over_GroupBy_over_parameter(async);

        AssertSql(
            """
SELECT `l1`.`Date`, `l2`.`Id`
FROM (
    SELECT `l`.`Date`
    FROM `LevelOne` AS `l`
    WHERE `l`.`Name` IN ('L1 01', 'L1 02')
    GROUP BY `l`.`Date`
) AS `l1`
LEFT JOIN (
    SELECT `l0`.`Id`, `l0`.`Date`
    FROM `LevelOne` AS `l0`
    WHERE `l0`.`Name` IN ('L1 01', 'L1 02')
) AS `l2` ON `l1`.`Date` = `l2`.`Date`
ORDER BY `l1`.`Date`
""");
    }

    public override async Task Include_partially_added_before_Where_and_then_build_upon(bool async)
    {
        await base.Include_partially_added_before_Where_and_then_build_upon(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `l1`.`Id`, `l1`.`Date`, `l1`.`Level1_Optional_Id`, `l1`.`Level1_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse2Id`, `l1`.`OneToMany_Optional_Self_Inverse2Id`, `l1`.`OneToMany_Required_Inverse2Id`, `l1`.`OneToMany_Required_Self_Inverse2Id`, `l1`.`OneToOne_Optional_PK_Inverse2Id`, `l1`.`OneToOne_Optional_Self2Id`, `l0`.`Id`, `s`.`Id`, `s`.`Level2_Optional_Id`, `s`.`Level2_Required_Id`, `s`.`Name`, `s`.`OneToMany_Optional_Inverse3Id`, `s`.`OneToMany_Optional_Self_Inverse3Id`, `s`.`OneToMany_Required_Inverse3Id`, `s`.`OneToMany_Required_Self_Inverse3Id`, `s`.`OneToOne_Optional_PK_Inverse3Id`, `s`.`OneToOne_Optional_Self3Id`, `s`.`Id0`, `s`.`Level3_Optional_Id`, `s`.`Level3_Required_Id`, `s`.`Name0`, `s`.`OneToMany_Optional_Inverse4Id`, `s`.`OneToMany_Optional_Self_Inverse4Id`, `s`.`OneToMany_Required_Inverse4Id`, `s`.`OneToMany_Required_Self_Inverse4Id`, `s`.`OneToOne_Optional_PK_Inverse4Id`, `s`.`OneToOne_Optional_Self4Id`
FROM ((`LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`OneToOne_Optional_PK_Inverse2Id`)
LEFT JOIN `LevelTwo` AS `l1` ON `l`.`Id` = `l1`.`Level1_Optional_Id`)
LEFT JOIN (
    SELECT `l2`.`Id`, `l2`.`Level2_Optional_Id`, `l2`.`Level2_Required_Id`, `l2`.`Name`, `l2`.`OneToMany_Optional_Inverse3Id`, `l2`.`OneToMany_Optional_Self_Inverse3Id`, `l2`.`OneToMany_Required_Inverse3Id`, `l2`.`OneToMany_Required_Self_Inverse3Id`, `l2`.`OneToOne_Optional_PK_Inverse3Id`, `l2`.`OneToOne_Optional_Self3Id`, `l3`.`Id` AS `Id0`, `l3`.`Level3_Optional_Id`, `l3`.`Level3_Required_Id`, `l3`.`Name` AS `Name0`, `l3`.`OneToMany_Optional_Inverse4Id`, `l3`.`OneToMany_Optional_Self_Inverse4Id`, `l3`.`OneToMany_Required_Inverse4Id`, `l3`.`OneToMany_Required_Self_Inverse4Id`, `l3`.`OneToOne_Optional_PK_Inverse4Id`, `l3`.`OneToOne_Optional_Self4Id`
    FROM `LevelThree` AS `l2`
    LEFT JOIN `LevelFour` AS `l3` ON `l2`.`Id` = `l3`.`Level3_Optional_Id`
) AS `s` ON `l1`.`Id` = `s`.`OneToMany_Optional_Inverse3Id`
WHERE `l0`.`Id` < 3 OR `l1`.`Id` > 8
ORDER BY `l`.`Id`, `l0`.`Id`, `l1`.`Id`, `s`.`Id`
""");
    }

    public override async Task Include_partially_added_before_Where_and_then_build_upon_with_filtered_include(bool async)
    {
        await base.Include_partially_added_before_Where_and_then_build_upon_with_filtered_include(async);

        AssertSql(
"""
SELECT [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id], [l1].[Id], [l1].[Date], [l1].[Level1_Optional_Id], [l1].[Level1_Required_Id], [l1].[Name], [l1].[OneToMany_Optional_Inverse2Id], [l1].[OneToMany_Optional_Self_Inverse2Id], [l1].[OneToMany_Required_Inverse2Id], [l1].[OneToMany_Required_Self_Inverse2Id], [l1].[OneToOne_Optional_PK_Inverse2Id], [l1].[OneToOne_Optional_Self2Id], [l0].[Id], [t0].[Id], [t0].[Level2_Optional_Id], [t0].[Level2_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse3Id], [t0].[OneToMany_Optional_Self_Inverse3Id], [t0].[OneToMany_Required_Inverse3Id], [t0].[OneToMany_Required_Self_Inverse3Id], [t0].[OneToOne_Optional_PK_Inverse3Id], [t0].[OneToOne_Optional_Self3Id], [t1].[Id], [t1].[Level2_Optional_Id], [t1].[Level2_Required_Id], [t1].[Name], [t1].[OneToMany_Optional_Inverse3Id], [t1].[OneToMany_Optional_Self_Inverse3Id], [t1].[OneToMany_Required_Inverse3Id], [t1].[OneToMany_Required_Self_Inverse3Id], [t1].[OneToOne_Optional_PK_Inverse3Id], [t1].[OneToOne_Optional_Self3Id], [t1].[Id0], [t1].[Level3_Optional_Id], [t1].[Level3_Required_Id], [t1].[Name0], [t1].[OneToMany_Optional_Inverse4Id], [t1].[OneToMany_Optional_Self_Inverse4Id], [t1].[OneToMany_Required_Inverse4Id], [t1].[OneToMany_Required_Self_Inverse4Id], [t1].[OneToOne_Optional_PK_Inverse4Id], [t1].[OneToOne_Optional_Self4Id]
FROM [LevelOne] AS [l]
LEFT JOIN [LevelTwo] AS [l0] ON [l].[Id] = [l0].[OneToOne_Optional_PK_Inverse2Id]
LEFT JOIN [LevelTwo] AS [l1] ON [l].[Id] = [l1].[Level1_Optional_Id]
LEFT JOIN (
    SELECT [t].[Id], [t].[Level2_Optional_Id], [t].[Level2_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse3Id], [t].[OneToMany_Optional_Self_Inverse3Id], [t].[OneToMany_Required_Inverse3Id], [t].[OneToMany_Required_Self_Inverse3Id], [t].[OneToOne_Optional_PK_Inverse3Id], [t].[OneToOne_Optional_Self3Id]
    FROM (
        SELECT [l2].[Id], [l2].[Level2_Optional_Id], [l2].[Level2_Required_Id], [l2].[Name], [l2].[OneToMany_Optional_Inverse3Id], [l2].[OneToMany_Optional_Self_Inverse3Id], [l2].[OneToMany_Required_Inverse3Id], [l2].[OneToMany_Required_Self_Inverse3Id], [l2].[OneToOne_Optional_PK_Inverse3Id], [l2].[OneToOne_Optional_Self3Id], ROW_NUMBER() OVER(PARTITION BY [l2].[OneToMany_Optional_Inverse3Id] ORDER BY [l2].[Id]) AS [row]
        FROM [LevelThree] AS [l2]
    ) AS [t]
    WHERE [t].[row] <= 3
) AS [t0] ON [l1].[Id] = [t0].[OneToMany_Optional_Inverse3Id]
LEFT JOIN (
    SELECT [l3].[Id], [l3].[Level2_Optional_Id], [l3].[Level2_Required_Id], [l3].[Name], [l3].[OneToMany_Optional_Inverse3Id], [l3].[OneToMany_Optional_Self_Inverse3Id], [l3].[OneToMany_Required_Inverse3Id], [l3].[OneToMany_Required_Self_Inverse3Id], [l3].[OneToOne_Optional_PK_Inverse3Id], [l3].[OneToOne_Optional_Self3Id], [l4].[Id] AS [Id0], [l4].[Level3_Optional_Id], [l4].[Level3_Required_Id], [l4].[Name] AS [Name0], [l4].[OneToMany_Optional_Inverse4Id], [l4].[OneToMany_Optional_Self_Inverse4Id], [l4].[OneToMany_Required_Inverse4Id], [l4].[OneToMany_Required_Self_Inverse4Id], [l4].[OneToOne_Optional_PK_Inverse4Id], [l4].[OneToOne_Optional_Self4Id]
    FROM [LevelThree] AS [l3]
    LEFT JOIN [LevelFour] AS [l4] ON [l3].[Id] = [l4].[Level3_Optional_Id]
) AS [t1] ON [l1].[Id] = [t1].[OneToMany_Required_Inverse3Id]
WHERE [l0].[Id] < 3 OR [l1].[Id] > 8
ORDER BY [l].[Id], [l0].[Id], [l1].[Id], [t0].[OneToMany_Optional_Inverse3Id], [t0].[Id], [t1].[Id]
""");
    }

    public override async Task Take_on_correlated_collection_in_projection(bool async)
    {
        await base.Take_on_correlated_collection_in_projection(async);

        AssertSql(
"""
SELECT [l].[Id], [t0].[Id], [t0].[Date], [t0].[Level1_Optional_Id], [t0].[Level1_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse2Id], [t0].[OneToMany_Optional_Self_Inverse2Id], [t0].[OneToMany_Required_Inverse2Id], [t0].[OneToMany_Required_Self_Inverse2Id], [t0].[OneToOne_Optional_PK_Inverse2Id], [t0].[OneToOne_Optional_Self2Id]
FROM [LevelOne] AS [l]
LEFT JOIN (
    SELECT [t].[Id], [t].[Date], [t].[Level1_Optional_Id], [t].[Level1_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse2Id], [t].[OneToMany_Optional_Self_Inverse2Id], [t].[OneToMany_Required_Inverse2Id], [t].[OneToMany_Required_Self_Inverse2Id], [t].[OneToOne_Optional_PK_Inverse2Id], [t].[OneToOne_Optional_Self2Id]
    FROM (
        SELECT [l0].[Id], [l0].[Date], [l0].[Level1_Optional_Id], [l0].[Level1_Required_Id], [l0].[Name], [l0].[OneToMany_Optional_Inverse2Id], [l0].[OneToMany_Optional_Self_Inverse2Id], [l0].[OneToMany_Required_Inverse2Id], [l0].[OneToMany_Required_Self_Inverse2Id], [l0].[OneToOne_Optional_PK_Inverse2Id], [l0].[OneToOne_Optional_Self2Id], ROW_NUMBER() OVER(PARTITION BY [l0].[OneToMany_Optional_Inverse2Id] ORDER BY [l0].[Id]) AS [row]
        FROM [LevelTwo] AS [l0]
    ) AS [t]
    WHERE [t].[row] <= 50
) AS [t0] ON [l].[Id] = [t0].[OneToMany_Optional_Inverse2Id]
ORDER BY [l].[Id]
""");
    }

    public override async Task FirstOrDefault_with_predicate_on_correlated_collection_in_projection(bool async)
    {
        await base.FirstOrDefault_with_predicate_on_correlated_collection_in_projection(async);

        AssertSql(
"""
SELECT [l].[Id], [t0].[Id], [t0].[Date], [t0].[Level1_Optional_Id], [t0].[Level1_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse2Id], [t0].[OneToMany_Optional_Self_Inverse2Id], [t0].[OneToMany_Required_Inverse2Id], [t0].[OneToMany_Required_Self_Inverse2Id], [t0].[OneToOne_Optional_PK_Inverse2Id], [t0].[OneToOne_Optional_Self2Id]
FROM [LevelOne] AS [l]
LEFT JOIN (
    SELECT [t].[Id], [t].[Date], [t].[Level1_Optional_Id], [t].[Level1_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse2Id], [t].[OneToMany_Optional_Self_Inverse2Id], [t].[OneToMany_Required_Inverse2Id], [t].[OneToMany_Required_Self_Inverse2Id], [t].[OneToOne_Optional_PK_Inverse2Id], [t].[OneToOne_Optional_Self2Id]
    FROM (
        SELECT [l0].[Id], [l0].[Date], [l0].[Level1_Optional_Id], [l0].[Level1_Required_Id], [l0].[Name], [l0].[OneToMany_Optional_Inverse2Id], [l0].[OneToMany_Optional_Self_Inverse2Id], [l0].[OneToMany_Required_Inverse2Id], [l0].[OneToMany_Required_Self_Inverse2Id], [l0].[OneToOne_Optional_PK_Inverse2Id], [l0].[OneToOne_Optional_Self2Id], ROW_NUMBER() OVER(PARTITION BY [l0].[OneToMany_Optional_Inverse2Id], [l0].[Id] ORDER BY [l0].[Id]) AS [row]
        FROM [LevelTwo] AS [l0]
    ) AS [t]
    WHERE [t].[row] <= 1
) AS [t0] ON [l].[Id] = [t0].[OneToMany_Optional_Inverse2Id] AND [l].[Id] = [t0].[Id]
""");
    }

    public override async Task SelectMany_with_predicate_and_DefaultIfEmpty_projecting_root_collection_element_and_another_collection(
        bool async)
    {
        await base.SelectMany_with_predicate_and_DefaultIfEmpty_projecting_root_collection_element_and_another_collection(async);

        AssertSql(
"""
SELECT [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id], [t].[Id], [t].[Date], [t].[Level1_Optional_Id], [t].[Level1_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse2Id], [t].[OneToMany_Optional_Self_Inverse2Id], [t].[OneToMany_Required_Inverse2Id], [t].[OneToMany_Required_Self_Inverse2Id], [t].[OneToOne_Optional_PK_Inverse2Id], [t].[OneToOne_Optional_Self2Id], [l1].[Id], [l1].[Date], [l1].[Level1_Optional_Id], [l1].[Level1_Required_Id], [l1].[Name], [l1].[OneToMany_Optional_Inverse2Id], [l1].[OneToMany_Optional_Self_Inverse2Id], [l1].[OneToMany_Required_Inverse2Id], [l1].[OneToMany_Required_Self_Inverse2Id], [l1].[OneToOne_Optional_PK_Inverse2Id], [l1].[OneToOne_Optional_Self2Id]
FROM [LevelOne] AS [l]
OUTER APPLY (
    SELECT [l0].[Id], [l0].[Date], [l0].[Level1_Optional_Id], [l0].[Level1_Required_Id], [l0].[Name], [l0].[OneToMany_Optional_Inverse2Id], [l0].[OneToMany_Optional_Self_Inverse2Id], [l0].[OneToMany_Required_Inverse2Id], [l0].[OneToMany_Required_Self_Inverse2Id], [l0].[OneToOne_Optional_PK_Inverse2Id], [l0].[OneToOne_Optional_Self2Id]
    FROM [LevelTwo] AS [l0]
    WHERE [l0].[Level1_Required_Id] = ([l].[Id] * 2) OR CAST(LEN([l0].[Name]) AS int) = [l0].[Id]
) AS [t]
LEFT JOIN [LevelTwo] AS [l1] ON [l].[Id] = [l1].[OneToMany_Optional_Inverse2Id]
ORDER BY [l].[Id], [t].[Id]
""");
    }

    public override async Task Complex_query_issue_21665(bool async)
    {
        await base.Complex_query_issue_21665(async);

        AssertSql(
"""
SELECT [t].[Id], [t].[Date], [t].[Name], [t].[OneToMany_Optional_Self_Inverse1Id], [t].[OneToMany_Required_Self_Inverse1Id], [t].[OneToOne_Optional_Self1Id], [t].[Name0], [t].[c], [t].[c0], [t].[c1], [t].[Id0], [t0].[Id], [t0].[Date], [t0].[Name], [t0].[OneToMany_Optional_Self_Inverse1Id], [t0].[OneToMany_Required_Self_Inverse1Id], [t0].[OneToOne_Optional_Self1Id], [t0].[ChildCount], [t0].[Level2Name], [t0].[Level2Count], [t0].[IsLevel2There], [t0].[Id0]
FROM (
    SELECT TOP(1) [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id], [l0].[Name] AS [Name0], (
        SELECT COUNT(*)
        FROM [LevelOne] AS [l1]
        WHERE [l].[Id] = [l1].[OneToMany_Optional_Self_Inverse1Id]) AS [c], (
        SELECT COUNT(*)
        FROM [LevelTwo] AS [l2]
        WHERE [l].[Id] = [l2].[OneToMany_Optional_Inverse2Id]) AS [c0], CASE
        WHEN EXISTS (
            SELECT 1
            FROM [LevelTwo] AS [l3]
            WHERE [l].[Id] = [l3].[OneToMany_Optional_Inverse2Id] AND [l3].[Id] = 2) THEN CAST(1 AS bit)
        ELSE CAST(0 AS bit)
    END AS [c1], [l0].[Id] AS [Id0]
    FROM [LevelOne] AS [l]
    LEFT JOIN [LevelTwo] AS [l0] ON [l].[Id] = [l0].[Level1_Optional_Id]
    WHERE [l].[Id] = 2
    ORDER BY [l].[Name]
) AS [t]
OUTER APPLY (
    SELECT [t1].[Id], [t1].[Date], [t1].[Name], [t1].[OneToMany_Optional_Self_Inverse1Id], [t1].[OneToMany_Required_Self_Inverse1Id], [t1].[OneToOne_Optional_Self1Id], (
        SELECT COUNT(*)
        FROM [LevelOne] AS [l5]
        WHERE [t1].[Id] = [l5].[OneToMany_Optional_Self_Inverse1Id]) AS [ChildCount], [l4].[Name] AS [Level2Name], (
        SELECT COUNT(*)
        FROM [LevelTwo] AS [l6]
        WHERE [t1].[Id] = [l6].[OneToMany_Optional_Inverse2Id]) AS [Level2Count], CASE
        WHEN EXISTS (
            SELECT 1
            FROM [LevelTwo] AS [l7]
            WHERE [t1].[Id] = [l7].[OneToMany_Optional_Inverse2Id] AND [l7].[Id] = 2) THEN CAST(1 AS bit)
        ELSE CAST(0 AS bit)
    END AS [IsLevel2There], [l4].[Id] AS [Id0]
    FROM (
        SELECT [l8].[Id], [l8].[Date], [l8].[Name], [l8].[OneToMany_Optional_Self_Inverse1Id], [l8].[OneToMany_Required_Self_Inverse1Id], [l8].[OneToOne_Optional_Self1Id]
        FROM [LevelOne] AS [l8]
        WHERE [t].[Id] = [l8].[OneToMany_Optional_Self_Inverse1Id]
        ORDER BY [l8].[Name]
        OFFSET 1 ROWS FETCH NEXT 5 ROWS ONLY
    ) AS [t1]
    LEFT JOIN [LevelTwo] AS [l4] ON [t1].[Id] = [l4].[Level1_Optional_Id]
) AS [t0]
ORDER BY [t].[Name], [t].[Id], [t].[Id0], [t0].[Name], [t0].[Id]
""");
    }

    public override async Task SelectMany_over_conditional_null_source(bool async)
    {
        await base.SelectMany_over_conditional_null_source(async);

        AssertSql();
    }

    public override async Task SelectMany_over_conditional_empty_source(bool async)
    {
        await base.SelectMany_over_conditional_empty_source(async);

        AssertSql();
    }

    public override async Task Queryable_in_subquery_works_when_final_projection_is_List(bool async)
    {
        await base.Queryable_in_subquery_works_when_final_projection_is_List(async);

        AssertSql();
    }

    public override async Task Include_after_Select(bool async)
    {
        await base.Include_after_Select(async);

        AssertSql();
    }

    public override async Task Include_after_SelectMany_and_reference_navigation(bool async)
    {
        await base.Include_after_SelectMany_and_reference_navigation(async);

        AssertSql();
    }

    public override async Task Filtered_include_different_filter_set_on_same_navigation_twice(bool async)
    {
        await base.Filtered_include_different_filter_set_on_same_navigation_twice(async);

        AssertSql();
    }

    public override async Task Filtered_include_different_filter_set_on_same_navigation_twice_multi_level(bool async)
    {
        await base.Filtered_include_different_filter_set_on_same_navigation_twice_multi_level(async);

        AssertSql();
    }

    public override async Task Filtered_include_include_parameter_used_inside_filter_throws(bool async)
    {
        await base.Filtered_include_include_parameter_used_inside_filter_throws(async);

        AssertSql();
    }

    public override async Task Filtered_include_with_Distinct_throws(bool async)
    {
        await base.Filtered_include_with_Distinct_throws(async);

        AssertSql();
    }

    public override async Task Filtered_include_calling_methods_directly_on_parameter_throws(bool async)
    {
        await base.Filtered_include_calling_methods_directly_on_parameter_throws(async);

        AssertSql();
    }

    public override async Task Filtered_include_is_considered_loaded(bool async)
    {
        await base.Filtered_include_is_considered_loaded(async);

        AssertSql(
"""
SELECT [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id], [t0].[Id], [t0].[Date], [t0].[Level1_Optional_Id], [t0].[Level1_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse2Id], [t0].[OneToMany_Optional_Self_Inverse2Id], [t0].[OneToMany_Required_Inverse2Id], [t0].[OneToMany_Required_Self_Inverse2Id], [t0].[OneToOne_Optional_PK_Inverse2Id], [t0].[OneToOne_Optional_Self2Id]
FROM [LevelOne] AS [l]
LEFT JOIN (
    SELECT [t].[Id], [t].[Date], [t].[Level1_Optional_Id], [t].[Level1_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse2Id], [t].[OneToMany_Optional_Self_Inverse2Id], [t].[OneToMany_Required_Inverse2Id], [t].[OneToMany_Required_Self_Inverse2Id], [t].[OneToOne_Optional_PK_Inverse2Id], [t].[OneToOne_Optional_Self2Id]
    FROM (
        SELECT [l0].[Id], [l0].[Date], [l0].[Level1_Optional_Id], [l0].[Level1_Required_Id], [l0].[Name], [l0].[OneToMany_Optional_Inverse2Id], [l0].[OneToMany_Optional_Self_Inverse2Id], [l0].[OneToMany_Required_Inverse2Id], [l0].[OneToMany_Required_Self_Inverse2Id], [l0].[OneToOne_Optional_PK_Inverse2Id], [l0].[OneToOne_Optional_Self2Id], ROW_NUMBER() OVER(PARTITION BY [l0].[OneToMany_Optional_Inverse2Id] ORDER BY [l0].[Id]) AS [row]
        FROM [LevelTwo] AS [l0]
    ) AS [t]
    WHERE [t].[row] <= 1
) AS [t0] ON [l].[Id] = [t0].[OneToMany_Optional_Inverse2Id]
ORDER BY [l].[Id], [t0].[OneToMany_Optional_Inverse2Id], [t0].[Id]
""");
    }

    public override async Task Select_nav_prop_collection_one_to_many_required(bool async)
    {
        await base.Select_nav_prop_collection_one_to_many_required(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l0`.`Id`
FROM `LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Required_Inverse2Id`
ORDER BY `l`.`Id`
""");
    }

    public override async Task Project_collection_navigation_nested_with_take(bool async)
    {
        await base.Project_collection_navigation_nested_with_take(async);

        AssertSql(
"""
SELECT [l].[Id], [l0].[Id], [t0].[Id], [t0].[Level2_Optional_Id], [t0].[Level2_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse3Id], [t0].[OneToMany_Optional_Self_Inverse3Id], [t0].[OneToMany_Required_Inverse3Id], [t0].[OneToMany_Required_Self_Inverse3Id], [t0].[OneToOne_Optional_PK_Inverse3Id], [t0].[OneToOne_Optional_Self3Id]
FROM [LevelOne] AS [l]
LEFT JOIN [LevelTwo] AS [l0] ON [l].[Id] = [l0].[Level1_Optional_Id]
LEFT JOIN (
    SELECT [t].[Id], [t].[Level2_Optional_Id], [t].[Level2_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse3Id], [t].[OneToMany_Optional_Self_Inverse3Id], [t].[OneToMany_Required_Inverse3Id], [t].[OneToMany_Required_Self_Inverse3Id], [t].[OneToOne_Optional_PK_Inverse3Id], [t].[OneToOne_Optional_Self3Id]
    FROM (
        SELECT [l1].[Id], [l1].[Level2_Optional_Id], [l1].[Level2_Required_Id], [l1].[Name], [l1].[OneToMany_Optional_Inverse3Id], [l1].[OneToMany_Optional_Self_Inverse3Id], [l1].[OneToMany_Required_Inverse3Id], [l1].[OneToMany_Required_Self_Inverse3Id], [l1].[OneToOne_Optional_PK_Inverse3Id], [l1].[OneToOne_Optional_Self3Id], ROW_NUMBER() OVER(PARTITION BY [l1].[OneToMany_Optional_Inverse3Id] ORDER BY [l1].[Id]) AS [row]
        FROM [LevelThree] AS [l1]
    ) AS [t]
    WHERE [t].[row] <= 50
) AS [t0] ON [l0].[Id] = [t0].[OneToMany_Optional_Inverse3Id]
ORDER BY [l].[Id], [l0].[Id]
""");
    }

    public override async Task Multiple_optional_navigation_with_Include(bool async)
    {
        await base.Multiple_optional_navigation_with_Include(async);

        AssertSql(
            """
SELECT `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`, `l`.`Id`, `l0`.`Id`, `l2`.`Id`, `l2`.`Level3_Optional_Id`, `l2`.`Level3_Required_Id`, `l2`.`Name`, `l2`.`OneToMany_Optional_Inverse4Id`, `l2`.`OneToMany_Optional_Self_Inverse4Id`, `l2`.`OneToMany_Required_Inverse4Id`, `l2`.`OneToMany_Required_Self_Inverse4Id`, `l2`.`OneToOne_Optional_PK_Inverse4Id`, `l2`.`OneToOne_Optional_Self4Id`
FROM ((`LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`Level1_Optional_Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToOne_Optional_PK_Inverse3Id`)
LEFT JOIN `LevelFour` AS `l2` ON `l1`.`Id` = `l2`.`OneToMany_Optional_Inverse4Id`
ORDER BY `l`.`Id`, `l0`.`Id`, `l1`.`Id`
""");
    }

    public override async Task Complex_query_with_let_collection_projection_FirstOrDefault_with_ToList_on_inner_and_outer(bool async)
    {
        await base.Complex_query_with_let_collection_projection_FirstOrDefault_with_ToList_on_inner_and_outer(async);

        AssertSql(
"""
SELECT [l].[Id], [t].[Id], [t0].[Name], [t0].[Id], [t].[c]
FROM [LevelOne] AS [l]
OUTER APPLY (
    SELECT TOP(1) 1 AS [c], [l0].[Id]
    FROM [LevelTwo] AS [l0]
    WHERE [l0].[Name] <> N'Foo' OR ([l0].[Name] IS NULL)
) AS [t]
OUTER APPLY (
    SELECT [l1].[Name], [l1].[Id]
    FROM [LevelOne] AS [l1]
    WHERE EXISTS (
        SELECT 1
        FROM [LevelTwo] AS [l2]
        WHERE [l1].[Id] = [l2].[OneToMany_Optional_Inverse2Id] AND [l2].[Id] = [t].[Id])
) AS [t0]
ORDER BY [l].[Id], [t].[Id]
""");
    }

    public override async Task Include_collection_with_multiple_orderbys_complex_repeated_checked(bool async)
    {
        await base.Include_collection_with_multiple_orderbys_complex_repeated_checked(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Level1_Optional_Id`, `l`.`Level1_Required_Id`, `l`.`Name`, `l`.`OneToMany_Optional_Inverse2Id`, `l`.`OneToMany_Optional_Self_Inverse2Id`, `l`.`OneToMany_Required_Inverse2Id`, `l`.`OneToMany_Required_Self_Inverse2Id`, `l`.`OneToOne_Optional_PK_Inverse2Id`, `l`.`OneToOne_Optional_Self2Id`, `l0`.`Id`, `l0`.`Level2_Optional_Id`, `l0`.`Level2_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse3Id`, `l0`.`OneToMany_Optional_Self_Inverse3Id`, `l0`.`OneToMany_Required_Inverse3Id`, `l0`.`OneToMany_Required_Self_Inverse3Id`, `l0`.`OneToOne_Optional_PK_Inverse3Id`, `l0`.`OneToOne_Optional_Self3Id`
FROM `LevelTwo` AS `l`
LEFT JOIN `LevelThree` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Optional_Inverse3Id`
ORDER BY -`l`.`Level1_Required_Id`, `l`.`Name`, `l`.`Id`
""");
    }

    public override async Task Multiple_optional_navigation_with_string_based_Include(bool async)
    {
        await base.Multiple_optional_navigation_with_string_based_Include(async);

        AssertSql(
            """
SELECT `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`, `l`.`Id`, `l0`.`Id`, `l2`.`Id`, `l2`.`Level3_Optional_Id`, `l2`.`Level3_Required_Id`, `l2`.`Name`, `l2`.`OneToMany_Optional_Inverse4Id`, `l2`.`OneToMany_Optional_Self_Inverse4Id`, `l2`.`OneToMany_Required_Inverse4Id`, `l2`.`OneToMany_Required_Self_Inverse4Id`, `l2`.`OneToOne_Optional_PK_Inverse4Id`, `l2`.`OneToOne_Optional_Self4Id`
FROM ((`LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`Level1_Optional_Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToOne_Optional_PK_Inverse3Id`)
LEFT JOIN `LevelFour` AS `l2` ON `l1`.`Id` = `l2`.`OneToMany_Optional_Inverse4Id`
ORDER BY `l`.`Id`, `l0`.`Id`, `l1`.`Id`
""");
    }

    public override async Task Projecting_collection_after_optional_reference_correlated_with_parent(bool async)
    {
        await base.Projecting_collection_after_optional_reference_correlated_with_parent(async);

        AssertSql(
"""
SELECT [l].[Id], [l0].[Id], [t].[ChildId], [t].[ParentName]
FROM [LevelOne] AS [l]
LEFT JOIN [LevelTwo] AS [l0] ON [l].[Id] = [l0].[Level1_Optional_Id]
OUTER APPLY (
    SELECT [l1].[Id] AS [ChildId], [l0].[Name] AS [ParentName]
    FROM [LevelThree] AS [l1]
    WHERE ([l0].[Id] IS NOT NULL) AND [l0].[Id] = [l1].[OneToMany_Optional_Inverse3Id]
) AS [t]
ORDER BY [l].[Id], [l0].[Id]
""");
    }

    public override async Task Projecting_collection_with_group_by_after_optional_reference_correlated_with_parent(bool async)
    {
        await base.Projecting_collection_with_group_by_after_optional_reference_correlated_with_parent(async);

        AssertSql(
"""
SELECT [l].[Id], [l1].[Id], [l1].[Level2_Optional_Id], [l1].[Level2_Required_Id], [l1].[Name], [l1].[OneToMany_Optional_Inverse3Id], [l1].[OneToMany_Optional_Self_Inverse3Id], [l1].[OneToMany_Required_Inverse3Id], [l1].[OneToMany_Required_Self_Inverse3Id], [l1].[OneToOne_Optional_PK_Inverse3Id], [l1].[OneToOne_Optional_Self3Id], [l0].[Id], [t].[Key], [t].[Count]
FROM [LevelOne] AS [l]
LEFT JOIN [LevelTwo] AS [l0] ON [l].[Id] = [l0].[Level1_Optional_Id]
LEFT JOIN [LevelThree] AS [l1] ON [l0].[Id] = [l1].[Level2_Optional_Id]
OUTER APPLY (
    SELECT [l2].[Name] AS [Key], COUNT(*) AS [Count]
    FROM [LevelThree] AS [l2]
    WHERE ([l0].[Id] IS NOT NULL) AND [l0].[Id] = [l2].[OneToMany_Optional_Inverse3Id]
    GROUP BY [l2].[Name]
) AS [t]
ORDER BY [l].[Id], [l0].[Id], [l1].[Id]
""");
    }

    public override async Task Multiple_complex_includes_EF_Property(bool async)
    {
        await base.Multiple_complex_includes_EF_Property(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`, `s`.`Id`, `s`.`Date`, `s`.`Level1_Optional_Id`, `s`.`Level1_Required_Id`, `s`.`Name`, `s`.`OneToMany_Optional_Inverse2Id`, `s`.`OneToMany_Optional_Self_Inverse2Id`, `s`.`OneToMany_Required_Inverse2Id`, `s`.`OneToMany_Required_Self_Inverse2Id`, `s`.`OneToOne_Optional_PK_Inverse2Id`, `s`.`OneToOne_Optional_Self2Id`, `s`.`Id0`, `s`.`Level2_Optional_Id`, `s`.`Level2_Required_Id`, `s`.`Name0`, `s`.`OneToMany_Optional_Inverse3Id`, `s`.`OneToMany_Optional_Self_Inverse3Id`, `s`.`OneToMany_Required_Inverse3Id`, `s`.`OneToMany_Required_Self_Inverse3Id`, `s`.`OneToOne_Optional_PK_Inverse3Id`, `s`.`OneToOne_Optional_Self3Id`
FROM ((`LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`Level1_Optional_Id`)
LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Inverse3Id`)
LEFT JOIN (
    SELECT `l2`.`Id`, `l2`.`Date`, `l2`.`Level1_Optional_Id`, `l2`.`Level1_Required_Id`, `l2`.`Name`, `l2`.`OneToMany_Optional_Inverse2Id`, `l2`.`OneToMany_Optional_Self_Inverse2Id`, `l2`.`OneToMany_Required_Inverse2Id`, `l2`.`OneToMany_Required_Self_Inverse2Id`, `l2`.`OneToOne_Optional_PK_Inverse2Id`, `l2`.`OneToOne_Optional_Self2Id`, `l3`.`Id` AS `Id0`, `l3`.`Level2_Optional_Id`, `l3`.`Level2_Required_Id`, `l3`.`Name` AS `Name0`, `l3`.`OneToMany_Optional_Inverse3Id`, `l3`.`OneToMany_Optional_Self_Inverse3Id`, `l3`.`OneToMany_Required_Inverse3Id`, `l3`.`OneToMany_Required_Self_Inverse3Id`, `l3`.`OneToOne_Optional_PK_Inverse3Id`, `l3`.`OneToOne_Optional_Self3Id`
    FROM `LevelTwo` AS `l2`
    LEFT JOIN `LevelThree` AS `l3` ON `l2`.`Id` = `l3`.`Level2_Optional_Id`
) AS `s` ON `l`.`Id` = `s`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`, `l0`.`Id`, `l1`.`Id`, `s`.`Id`
""");
    }

    public override async Task Multiple_complex_includes_self_ref_EF_Property(bool async)
    {
        await base.Multiple_complex_includes_self_ref_EF_Property(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Name`, `l0`.`OneToMany_Optional_Self_Inverse1Id`, `l0`.`OneToMany_Required_Self_Inverse1Id`, `l0`.`OneToOne_Optional_Self1Id`, `l1`.`Id`, `l1`.`Date`, `l1`.`Name`, `l1`.`OneToMany_Optional_Self_Inverse1Id`, `l1`.`OneToMany_Required_Self_Inverse1Id`, `l1`.`OneToOne_Optional_Self1Id`, `s`.`Id`, `s`.`Date`, `s`.`Name`, `s`.`OneToMany_Optional_Self_Inverse1Id`, `s`.`OneToMany_Required_Self_Inverse1Id`, `s`.`OneToOne_Optional_Self1Id`, `s`.`Id0`, `s`.`Date0`, `s`.`Name0`, `s`.`OneToMany_Optional_Self_Inverse1Id0`, `s`.`OneToMany_Required_Self_Inverse1Id0`, `s`.`OneToOne_Optional_Self1Id0`
FROM ((`LevelOne` AS `l`
LEFT JOIN `LevelOne` AS `l0` ON `l`.`OneToOne_Optional_Self1Id` = `l0`.`Id`)
LEFT JOIN `LevelOne` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Self_Inverse1Id`)
LEFT JOIN (
    SELECT `l2`.`Id`, `l2`.`Date`, `l2`.`Name`, `l2`.`OneToMany_Optional_Self_Inverse1Id`, `l2`.`OneToMany_Required_Self_Inverse1Id`, `l2`.`OneToOne_Optional_Self1Id`, `l3`.`Id` AS `Id0`, `l3`.`Date` AS `Date0`, `l3`.`Name` AS `Name0`, `l3`.`OneToMany_Optional_Self_Inverse1Id` AS `OneToMany_Optional_Self_Inverse1Id0`, `l3`.`OneToMany_Required_Self_Inverse1Id` AS `OneToMany_Required_Self_Inverse1Id0`, `l3`.`OneToOne_Optional_Self1Id` AS `OneToOne_Optional_Self1Id0`
    FROM `LevelOne` AS `l2`
    LEFT JOIN `LevelOne` AS `l3` ON `l2`.`OneToOne_Optional_Self1Id` = `l3`.`Id`
) AS `s` ON `l`.`Id` = `s`.`OneToMany_Optional_Self_Inverse1Id`
ORDER BY `l`.`Id`, `l0`.`Id`, `l1`.`Id`, `s`.`Id`
""");
    }

    public override async Task Include_collection_multiple_with_filter_EF_Property(bool async)
    {
        await base.Include_collection_multiple_with_filter_EF_Property(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `s`.`Id`, `s`.`Date`, `s`.`Level1_Optional_Id`, `s`.`Level1_Required_Id`, `s`.`Name`, `s`.`OneToMany_Optional_Inverse2Id`, `s`.`OneToMany_Optional_Self_Inverse2Id`, `s`.`OneToMany_Required_Inverse2Id`, `s`.`OneToMany_Required_Self_Inverse2Id`, `s`.`OneToOne_Optional_PK_Inverse2Id`, `s`.`OneToOne_Optional_Self2Id`, `s`.`Id0`, `s`.`Level2_Optional_Id`, `s`.`Level2_Required_Id`, `s`.`Name0`, `s`.`OneToMany_Optional_Inverse3Id`, `s`.`OneToMany_Optional_Self_Inverse3Id`, `s`.`OneToMany_Required_Inverse3Id`, `s`.`OneToMany_Required_Self_Inverse3Id`, `s`.`OneToOne_Optional_PK_Inverse3Id`, `s`.`OneToOne_Optional_Self3Id`, `s`.`Id1`, `s`.`Level3_Optional_Id`, `s`.`Level3_Required_Id`, `s`.`Name1`, `s`.`OneToMany_Optional_Inverse4Id`, `s`.`OneToMany_Optional_Self_Inverse4Id`, `s`.`OneToMany_Required_Inverse4Id`, `s`.`OneToMany_Required_Self_Inverse4Id`, `s`.`OneToOne_Optional_PK_Inverse4Id`, `s`.`OneToOne_Optional_Self4Id`
FROM `LevelOne` AS `l`
LEFT JOIN (
    SELECT `l2`.`Id`, `l2`.`Date`, `l2`.`Level1_Optional_Id`, `l2`.`Level1_Required_Id`, `l2`.`Name`, `l2`.`OneToMany_Optional_Inverse2Id`, `l2`.`OneToMany_Optional_Self_Inverse2Id`, `l2`.`OneToMany_Required_Inverse2Id`, `l2`.`OneToMany_Required_Self_Inverse2Id`, `l2`.`OneToOne_Optional_PK_Inverse2Id`, `l2`.`OneToOne_Optional_Self2Id`, `l3`.`Id` AS `Id0`, `l3`.`Level2_Optional_Id`, `l3`.`Level2_Required_Id`, `l3`.`Name` AS `Name0`, `l3`.`OneToMany_Optional_Inverse3Id`, `l3`.`OneToMany_Optional_Self_Inverse3Id`, `l3`.`OneToMany_Required_Inverse3Id`, `l3`.`OneToMany_Required_Self_Inverse3Id`, `l3`.`OneToOne_Optional_PK_Inverse3Id`, `l3`.`OneToOne_Optional_Self3Id`, `l4`.`Id` AS `Id1`, `l4`.`Level3_Optional_Id`, `l4`.`Level3_Required_Id`, `l4`.`Name` AS `Name1`, `l4`.`OneToMany_Optional_Inverse4Id`, `l4`.`OneToMany_Optional_Self_Inverse4Id`, `l4`.`OneToMany_Required_Inverse4Id`, `l4`.`OneToMany_Required_Self_Inverse4Id`, `l4`.`OneToOne_Optional_PK_Inverse4Id`, `l4`.`OneToOne_Optional_Self4Id`
    FROM (`LevelTwo` AS `l2`
    LEFT JOIN `LevelThree` AS `l3` ON `l2`.`Id` = `l3`.`OneToOne_Optional_PK_Inverse3Id`)
    LEFT JOIN `LevelFour` AS `l4` ON `l3`.`Id` = `l4`.`Level3_Optional_Id`
) AS `s` ON `l`.`Id` = `s`.`OneToMany_Optional_Inverse2Id`
WHERE (
    SELECT COUNT(*)
    FROM `LevelTwo` AS `l0`
    LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToOne_Optional_PK_Inverse3Id`
    WHERE `l`.`Id` = `l0`.`OneToMany_Optional_Inverse2Id` AND (`l1`.`Name` <> 'Foo' OR `l1`.`Name` IS NULL)) > 0
ORDER BY `l`.`Id`, `s`.`Id`, `s`.`Id0`
""");
    }

    public override async Task Filtered_include_basic_Where_EF_Property(bool async)
    {
        await base.Filtered_include_basic_Where_EF_Property(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `l1`.`Id`, `l1`.`Date`, `l1`.`Level1_Optional_Id`, `l1`.`Level1_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse2Id`, `l1`.`OneToMany_Optional_Self_Inverse2Id`, `l1`.`OneToMany_Required_Inverse2Id`, `l1`.`OneToMany_Required_Self_Inverse2Id`, `l1`.`OneToOne_Optional_PK_Inverse2Id`, `l1`.`OneToOne_Optional_Self2Id`
FROM `LevelOne` AS `l`
LEFT JOIN (
    SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`
    FROM `LevelTwo` AS `l0`
    WHERE `l0`.`Id` > 5
) AS `l1` ON `l`.`Id` = `l1`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`
""");
    }

    public override async Task Filtered_include_OrderBy_EF_Property(bool async)
    {
        await base.Filtered_include_OrderBy_EF_Property(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l`.`Date`, `l`.`Name`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`
FROM `LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Id`, `l0`.`Name`
""");
    }

    public override async Task Filtered_include_basic_OrderBy_Skip_Take_EF_Property(bool async)
    {
        await base.Filtered_include_basic_OrderBy_Skip_Take_EF_Property(async);

        AssertSql(
"""
SELECT [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id], [t0].[Id], [t0].[Date], [t0].[Level1_Optional_Id], [t0].[Level1_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse2Id], [t0].[OneToMany_Optional_Self_Inverse2Id], [t0].[OneToMany_Required_Inverse2Id], [t0].[OneToMany_Required_Self_Inverse2Id], [t0].[OneToOne_Optional_PK_Inverse2Id], [t0].[OneToOne_Optional_Self2Id]
FROM [LevelOne] AS [l]
LEFT JOIN (
    SELECT [t].[Id], [t].[Date], [t].[Level1_Optional_Id], [t].[Level1_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse2Id], [t].[OneToMany_Optional_Self_Inverse2Id], [t].[OneToMany_Required_Inverse2Id], [t].[OneToMany_Required_Self_Inverse2Id], [t].[OneToOne_Optional_PK_Inverse2Id], [t].[OneToOne_Optional_Self2Id]
    FROM (
        SELECT [l0].[Id], [l0].[Date], [l0].[Level1_Optional_Id], [l0].[Level1_Required_Id], [l0].[Name], [l0].[OneToMany_Optional_Inverse2Id], [l0].[OneToMany_Optional_Self_Inverse2Id], [l0].[OneToMany_Required_Inverse2Id], [l0].[OneToMany_Required_Self_Inverse2Id], [l0].[OneToOne_Optional_PK_Inverse2Id], [l0].[OneToOne_Optional_Self2Id], ROW_NUMBER() OVER(PARTITION BY [l0].[OneToMany_Optional_Inverse2Id] ORDER BY [l0].[Name]) AS [row]
        FROM [LevelTwo] AS [l0]
    ) AS [t]
    WHERE 1 < [t].[row] AND [t].[row] <= 4
) AS [t0] ON [l].[Id] = [t0].[OneToMany_Optional_Inverse2Id]
ORDER BY [l].[Id], [t0].[OneToMany_Optional_Inverse2Id], [t0].[Name]
""");
    }

    public override async Task Filtered_include_on_ThenInclude_EF_Property(bool async)
    {
        await base.Filtered_include_on_ThenInclude_EF_Property(async);

        AssertSql(
"""
SELECT [l].[Id], [l].[Date], [l].[Name], [l].[OneToMany_Optional_Self_Inverse1Id], [l].[OneToMany_Required_Self_Inverse1Id], [l].[OneToOne_Optional_Self1Id], [l0].[Id], [l0].[Date], [l0].[Level1_Optional_Id], [l0].[Level1_Required_Id], [l0].[Name], [l0].[OneToMany_Optional_Inverse2Id], [l0].[OneToMany_Optional_Self_Inverse2Id], [l0].[OneToMany_Required_Inverse2Id], [l0].[OneToMany_Required_Self_Inverse2Id], [l0].[OneToOne_Optional_PK_Inverse2Id], [l0].[OneToOne_Optional_Self2Id], [t0].[Id], [t0].[Level2_Optional_Id], [t0].[Level2_Required_Id], [t0].[Name], [t0].[OneToMany_Optional_Inverse3Id], [t0].[OneToMany_Optional_Self_Inverse3Id], [t0].[OneToMany_Required_Inverse3Id], [t0].[OneToMany_Required_Self_Inverse3Id], [t0].[OneToOne_Optional_PK_Inverse3Id], [t0].[OneToOne_Optional_Self3Id]
FROM [LevelOne] AS [l]
LEFT JOIN [LevelTwo] AS [l0] ON [l].[Id] = [l0].[Level1_Optional_Id]
LEFT JOIN (
    SELECT [t].[Id], [t].[Level2_Optional_Id], [t].[Level2_Required_Id], [t].[Name], [t].[OneToMany_Optional_Inverse3Id], [t].[OneToMany_Optional_Self_Inverse3Id], [t].[OneToMany_Required_Inverse3Id], [t].[OneToMany_Required_Self_Inverse3Id], [t].[OneToOne_Optional_PK_Inverse3Id], [t].[OneToOne_Optional_Self3Id]
    FROM (
        SELECT [l1].[Id], [l1].[Level2_Optional_Id], [l1].[Level2_Required_Id], [l1].[Name], [l1].[OneToMany_Optional_Inverse3Id], [l1].[OneToMany_Optional_Self_Inverse3Id], [l1].[OneToMany_Required_Inverse3Id], [l1].[OneToMany_Required_Self_Inverse3Id], [l1].[OneToOne_Optional_PK_Inverse3Id], [l1].[OneToOne_Optional_Self3Id], ROW_NUMBER() OVER(PARTITION BY [l1].[OneToMany_Optional_Inverse3Id] ORDER BY [l1].[Name]) AS [row]
        FROM [LevelThree] AS [l1]
        WHERE [l1].[Name] <> N'Foo' OR ([l1].[Name] IS NULL)
    ) AS [t]
    WHERE 1 < [t].[row] AND [t].[row] <= 4
) AS [t0] ON [l0].[Id] = [t0].[OneToMany_Optional_Inverse3Id]
ORDER BY [l].[Id], [l0].[Id], [t0].[OneToMany_Optional_Inverse3Id], [t0].[Name]
""");
    }

    public override async Task Final_GroupBy_property_entity_Include_collection(bool async)
    {
        await base.Final_GroupBy_property_entity_Include_collection(async);

        AssertSql(
            """
SELECT `l`.`Name`, `l`.`Id`, `l`.`Date`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`
FROM `LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Name`, `l`.`Id`
""");
    }

    public override async Task Final_GroupBy_property_entity_Include_collection_nested(bool async)
    {
        await base.Final_GroupBy_property_entity_Include_collection_nested(async);

        AssertSql(
            """
SELECT `l`.`Name`, `l`.`Id`, `l`.`Date`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `s`.`Id`, `s`.`Date`, `s`.`Level1_Optional_Id`, `s`.`Level1_Required_Id`, `s`.`Name`, `s`.`OneToMany_Optional_Inverse2Id`, `s`.`OneToMany_Optional_Self_Inverse2Id`, `s`.`OneToMany_Required_Inverse2Id`, `s`.`OneToMany_Required_Self_Inverse2Id`, `s`.`OneToOne_Optional_PK_Inverse2Id`, `s`.`OneToOne_Optional_Self2Id`, `s`.`Id0`, `s`.`Level2_Optional_Id`, `s`.`Level2_Required_Id`, `s`.`Name0`, `s`.`OneToMany_Optional_Inverse3Id`, `s`.`OneToMany_Optional_Self_Inverse3Id`, `s`.`OneToMany_Required_Inverse3Id`, `s`.`OneToMany_Required_Self_Inverse3Id`, `s`.`OneToOne_Optional_PK_Inverse3Id`, `s`.`OneToOne_Optional_Self3Id`
FROM `LevelOne` AS `l`
LEFT JOIN (
    SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id` AS `Id0`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name` AS `Name0`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`
    FROM `LevelTwo` AS `l0`
    LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`OneToMany_Optional_Inverse3Id`
) AS `s` ON `l`.`Id` = `s`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Name`, `l`.`Id`, `s`.`Id`
""");
    }

    public override async Task Final_GroupBy_property_entity_Include_collection_reference(bool async)
    {
        await base.Final_GroupBy_property_entity_Include_collection_reference(async);

        AssertSql(
            """
SELECT `l`.`Name`, `l`.`Id`, `l`.`Date`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `s`.`Id`, `s`.`Date`, `s`.`Level1_Optional_Id`, `s`.`Level1_Required_Id`, `s`.`Name`, `s`.`OneToMany_Optional_Inverse2Id`, `s`.`OneToMany_Optional_Self_Inverse2Id`, `s`.`OneToMany_Required_Inverse2Id`, `s`.`OneToMany_Required_Self_Inverse2Id`, `s`.`OneToOne_Optional_PK_Inverse2Id`, `s`.`OneToOne_Optional_Self2Id`, `s`.`Id0`, `s`.`Level2_Optional_Id`, `s`.`Level2_Required_Id`, `s`.`Name0`, `s`.`OneToMany_Optional_Inverse3Id`, `s`.`OneToMany_Optional_Self_Inverse3Id`, `s`.`OneToMany_Required_Inverse3Id`, `s`.`OneToMany_Required_Self_Inverse3Id`, `s`.`OneToOne_Optional_PK_Inverse3Id`, `s`.`OneToOne_Optional_Self3Id`
FROM `LevelOne` AS `l`
LEFT JOIN (
    SELECT `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id` AS `Id0`, `l1`.`Level2_Optional_Id`, `l1`.`Level2_Required_Id`, `l1`.`Name` AS `Name0`, `l1`.`OneToMany_Optional_Inverse3Id`, `l1`.`OneToMany_Optional_Self_Inverse3Id`, `l1`.`OneToMany_Required_Inverse3Id`, `l1`.`OneToMany_Required_Self_Inverse3Id`, `l1`.`OneToOne_Optional_PK_Inverse3Id`, `l1`.`OneToOne_Optional_Self3Id`
    FROM `LevelTwo` AS `l0`
    LEFT JOIN `LevelThree` AS `l1` ON `l0`.`Id` = `l1`.`Level2_Optional_Id`
) AS `s` ON `l`.`Id` = `s`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Name`, `l`.`Id`, `s`.`Id`
""");
    }

    public override async Task Final_GroupBy_property_entity_Include_collection_multiple(bool async)
    {
        await base.Final_GroupBy_property_entity_Include_collection_multiple(async);

        AssertSql(
            """
SELECT `l`.`Name`, `l`.`Id`, `l`.`Date`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id`, `l1`.`Date`, `l1`.`Level1_Optional_Id`, `l1`.`Level1_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse2Id`, `l1`.`OneToMany_Optional_Self_Inverse2Id`, `l1`.`OneToMany_Required_Inverse2Id`, `l1`.`OneToMany_Required_Self_Inverse2Id`, `l1`.`OneToOne_Optional_PK_Inverse2Id`, `l1`.`OneToOne_Optional_Self2Id`
FROM (`LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Optional_Inverse2Id`)
LEFT JOIN `LevelTwo` AS `l1` ON `l`.`Id` = `l1`.`OneToMany_Required_Inverse2Id`
ORDER BY `l`.`Name`, `l`.`Id`, `l0`.`Id`
""");
    }

    public override async Task Final_GroupBy_property_entity_Include_collection_reference_same_level(bool async)
    {
        await base.Final_GroupBy_property_entity_Include_collection_reference_same_level(async);

        AssertSql(
            """
SELECT `l`.`Name`, `l`.`Id`, `l`.`Date`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `l0`.`Id`, `l1`.`Id`, `l1`.`Date`, `l1`.`Level1_Optional_Id`, `l1`.`Level1_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse2Id`, `l1`.`OneToMany_Optional_Self_Inverse2Id`, `l1`.`OneToMany_Required_Inverse2Id`, `l1`.`OneToMany_Required_Self_Inverse2Id`, `l1`.`OneToOne_Optional_PK_Inverse2Id`, `l1`.`OneToOne_Optional_Self2Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`
FROM (`LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`Level1_Optional_Id`)
LEFT JOIN `LevelTwo` AS `l1` ON `l`.`Id` = `l1`.`OneToMany_Optional_Inverse2Id`
ORDER BY `l`.`Name`, `l`.`Id`, `l0`.`Id`
""");
    }

    public override async Task Final_GroupBy_property_entity_Include_reference(bool async)
    {
        await base.Final_GroupBy_property_entity_Include_reference(async);

        AssertSql(
            """
SELECT `l`.`Name`, `l`.`Id`, `l`.`Date`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`
FROM `LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`Level1_Optional_Id`
ORDER BY `l`.`Name`
""");
    }

    public override async Task Final_GroupBy_property_entity_Include_reference_multiple(bool async)
    {
        await base.Final_GroupBy_property_entity_Include_reference_multiple(async);

        AssertSql(
            """
SELECT `l`.`Name`, `l`.`Id`, `l`.`Date`, `l`.`OneToMany_Optional_Self_Inverse1Id`, `l`.`OneToMany_Required_Self_Inverse1Id`, `l`.`OneToOne_Optional_Self1Id`, `l0`.`Id`, `l0`.`Date`, `l0`.`Level1_Optional_Id`, `l0`.`Level1_Required_Id`, `l0`.`Name`, `l0`.`OneToMany_Optional_Inverse2Id`, `l0`.`OneToMany_Optional_Self_Inverse2Id`, `l0`.`OneToMany_Required_Inverse2Id`, `l0`.`OneToMany_Required_Self_Inverse2Id`, `l0`.`OneToOne_Optional_PK_Inverse2Id`, `l0`.`OneToOne_Optional_Self2Id`, `l1`.`Id`, `l1`.`Date`, `l1`.`Level1_Optional_Id`, `l1`.`Level1_Required_Id`, `l1`.`Name`, `l1`.`OneToMany_Optional_Inverse2Id`, `l1`.`OneToMany_Optional_Self_Inverse2Id`, `l1`.`OneToMany_Required_Inverse2Id`, `l1`.`OneToMany_Required_Self_Inverse2Id`, `l1`.`OneToOne_Optional_PK_Inverse2Id`, `l1`.`OneToOne_Optional_Self2Id`
FROM (`LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`Level1_Optional_Id`)
LEFT JOIN `LevelTwo` AS `l1` ON `l`.`Id` = `l1`.`Level1_Required_Id`
ORDER BY `l`.`Name`
""");
    }

    public override async Task Project_collection_and_nested_conditional(bool async)
    {
        await base.Project_collection_and_nested_conditional(async);

        AssertSql(
            """
SELECT `l`.`Id`, `l0`.`Name`, `l0`.`Id`, IIF(`l`.`Id` = 1, '01', IIF(`l`.`Id` = 2, '02', IIF(`l`.`Id` = 3, '03', NULL)))
FROM `LevelOne` AS `l`
LEFT JOIN `LevelTwo` AS `l0` ON `l`.`Id` = `l0`.`OneToMany_Optional_Inverse2Id`
WHERE IIF(`l`.`Id` = 1, '01', IIF(`l`.`Id` = 2, '02', IIF(`l`.`Id` = 3, '03', NULL))) = '02'
ORDER BY `l`.`Id`, `l0`.`Id`
""");
    }

    private void AssertSql(params string[] expected)
        => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);
}
