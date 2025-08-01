﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;
using Xunit.Abstractions;

namespace EntityFrameworkCore.Jet.FunctionalTests.Query;

public abstract class TPCInheritanceQueryJetTestBase<TFixture>(TFixture fixture, ITestOutputHelper testOutputHelper)
    : TPCInheritanceQueryTestBase<TFixture>(fixture, testOutputHelper)
    where TFixture : TPCInheritanceQueryJetFixtureBase, new()
{
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType().BaseType!);

    public override async Task Byte_enum_value_constant_used_in_projection(bool async)
    {
        await base.Byte_enum_value_constant_used_in_projection(async);

        AssertSql(
            """
SELECT IIF(`k`.`IsFlightless` = TRUE, CBYTE(0), CBYTE(1))
FROM `Kiwi` AS `k`
""");
    }

    public override async Task Can_filter_all_animals(bool async)
    {
        await base.Can_filter_all_animals(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`CountryId`, `u`.`Name`, `u`.`Species`, `u`.`EagleId`, `u`.`IsFlightless`, `u`.`Group`, `u`.`FoundOn`, `u`.`Discriminator`
FROM (
    SELECT `e`.`Id`, `e`.`CountryId`, `e`.`Name`, `e`.`Species`, `e`.`EagleId`, `e`.`IsFlightless`, `e`.`Group`, CVar(NULL) AS `FoundOn`, 'Eagle' AS `Discriminator`
    FROM `Eagle` AS `e`
    UNION ALL
    SELECT `k`.`Id`, `k`.`CountryId`, `k`.`Name`, `k`.`Species`, `k`.`EagleId`, `k`.`IsFlightless`, CVar(NULL) AS `Group`, `k`.`FoundOn`, 'Kiwi' AS `Discriminator`
    FROM `Kiwi` AS `k`
) AS `u`
WHERE `u`.`Name` = 'Great spotted kiwi'
ORDER BY `u`.`Species`
""");
    }

    public override async Task Can_include_animals(bool async)
    {
        await base.Can_include_animals(async);

        AssertSql(
            """
SELECT `c`.`Id`, `c`.`Name`, `u`.`Id`, `u`.`CountryId`, `u`.`Name`, `u`.`Species`, `u`.`EagleId`, `u`.`IsFlightless`, `u`.`Group`, `u`.`FoundOn`, `u`.`Discriminator`
FROM `Countries` AS `c`
LEFT JOIN (
    SELECT `e`.`Id`, `e`.`CountryId`, `e`.`Name`, `e`.`Species`, `e`.`EagleId`, `e`.`IsFlightless`, `e`.`Group`, CVar(NULL) AS `FoundOn`, 'Eagle' AS `Discriminator`
    FROM `Eagle` AS `e`
    UNION ALL
    SELECT `k`.`Id`, `k`.`CountryId`, `k`.`Name`, `k`.`Species`, `k`.`EagleId`, `k`.`IsFlightless`, CVar(NULL) AS `Group`, `k`.`FoundOn`, 'Kiwi' AS `Discriminator`
    FROM `Kiwi` AS `k`
) AS `u` ON `c`.`Id` = `u`.`CountryId`
ORDER BY `c`.`Name`, `c`.`Id`
""");
    }

    public override async Task Can_include_prey(bool async)
    {
        await base.Can_include_prey(async);

        AssertSql(
            """
SELECT `e1`.`Id`, `e1`.`CountryId`, `e1`.`Name`, `e1`.`Species`, `e1`.`EagleId`, `e1`.`IsFlightless`, `e1`.`Group`, `u`.`Id`, `u`.`CountryId`, `u`.`Name`, `u`.`Species`, `u`.`EagleId`, `u`.`IsFlightless`, `u`.`Group`, `u`.`FoundOn`, `u`.`Discriminator`
FROM (
    SELECT TOP 2 `e`.`Id`, `e`.`CountryId`, `e`.`Name`, `e`.`Species`, `e`.`EagleId`, `e`.`IsFlightless`, `e`.`Group`
    FROM `Eagle` AS `e`
) AS `e1`
LEFT JOIN (
    SELECT `e0`.`Id`, `e0`.`CountryId`, `e0`.`Name`, `e0`.`Species`, `e0`.`EagleId`, `e0`.`IsFlightless`, `e0`.`Group`, CVar(NULL) AS `FoundOn`, 'Eagle' AS `Discriminator`
    FROM `Eagle` AS `e0`
    UNION ALL
    SELECT `k`.`Id`, `k`.`CountryId`, `k`.`Name`, `k`.`Species`, `k`.`EagleId`, `k`.`IsFlightless`, CVar(NULL) AS `Group`, `k`.`FoundOn`, 'Kiwi' AS `Discriminator`
    FROM `Kiwi` AS `k`
) AS `u` ON `e1`.`Id` = `u`.`EagleId`
ORDER BY `e1`.`Id`
""");
    }

    public override async Task Can_insert_update_delete()
        => await base.Can_insert_update_delete();

    public override async Task Can_query_all_animals(bool async)
    {
        await base.Can_query_all_animals(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`CountryId`, `u`.`Name`, `u`.`Species`, `u`.`EagleId`, `u`.`IsFlightless`, `u`.`Group`, `u`.`FoundOn`, `u`.`Discriminator`
FROM (
    SELECT `e`.`Id`, `e`.`CountryId`, `e`.`Name`, `e`.`Species`, `e`.`EagleId`, `e`.`IsFlightless`, `e`.`Group`, CVar(NULL) AS `FoundOn`, 'Eagle' AS `Discriminator`
    FROM `Eagle` AS `e`
    UNION ALL
    SELECT `k`.`Id`, `k`.`CountryId`, `k`.`Name`, `k`.`Species`, `k`.`EagleId`, `k`.`IsFlightless`, CVar(NULL) AS `Group`, `k`.`FoundOn`, 'Kiwi' AS `Discriminator`
    FROM `Kiwi` AS `k`
) AS `u`
ORDER BY `u`.`Species`
""");
    }

    public override async Task Can_query_all_birds(bool async)
    {
        await base.Can_query_all_birds(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`CountryId`, `u`.`Name`, `u`.`Species`, `u`.`EagleId`, `u`.`IsFlightless`, `u`.`Group`, `u`.`FoundOn`, `u`.`Discriminator`
FROM (
    SELECT `e`.`Id`, `e`.`CountryId`, `e`.`Name`, `e`.`Species`, `e`.`EagleId`, `e`.`IsFlightless`, `e`.`Group`, CVar(NULL) AS `FoundOn`, 'Eagle' AS `Discriminator`
    FROM `Eagle` AS `e`
    UNION ALL
    SELECT `k`.`Id`, `k`.`CountryId`, `k`.`Name`, `k`.`Species`, `k`.`EagleId`, `k`.`IsFlightless`, CVar(NULL) AS `Group`, `k`.`FoundOn`, 'Kiwi' AS `Discriminator`
    FROM `Kiwi` AS `k`
) AS `u`
ORDER BY `u`.`Species`
""");
    }

    public override async Task Can_query_all_plants(bool async)
    {
        await base.Can_query_all_plants(async);

        AssertSql(
            """
SELECT `u`.`Species`, `u`.`CountryId`, `u`.`Genus`, `u`.`Name`, `u`.`HasThorns`, `u`.`AdditionalInfo_Nickname`, `u`.`AdditionalInfo_LeafStructure_AreLeavesBig`, `u`.`AdditionalInfo_LeafStructure_NumLeaves`, `u`.`Discriminator`
FROM (
    SELECT `d`.`Species`, `d`.`CountryId`, `d`.`Genus`, `d`.`Name`, `d`.`AdditionalInfo_Nickname`, `d`.`AdditionalInfo_LeafStructure_AreLeavesBig`, `d`.`AdditionalInfo_LeafStructure_NumLeaves`, CVar(NULL) AS `HasThorns`, 'Daisy' AS `Discriminator`
    FROM `Daisies` AS `d`
    UNION ALL
    SELECT `r`.`Species`, `r`.`CountryId`, `r`.`Genus`, `r`.`Name`, NULL AS `AdditionalInfo_Nickname`, CVar(NULL) AS `AdditionalInfo_LeafStructure_AreLeavesBig`, CVar(NULL) AS `AdditionalInfo_LeafStructure_NumLeaves`, `r`.`HasThorns`, 'Rose' AS `Discriminator`
    FROM `Roses` AS `r`
) AS `u`
ORDER BY `u`.`Species`
""");
    }

    public override async Task Filter_on_property_inside_complex_type_on_derived_type(bool async)
    {
        await base.Filter_on_property_inside_complex_type_on_derived_type(async);

        AssertSql(
            """
SELECT `d`.`Species`, `d`.`CountryId`, `d`.`Genus`, `d`.`Name`, `d`.`AdditionalInfo_Nickname`, `d`.`AdditionalInfo_LeafStructure_AreLeavesBig`, `d`.`AdditionalInfo_LeafStructure_NumLeaves`
FROM `Daisies` AS `d`
WHERE `d`.`AdditionalInfo_LeafStructure_AreLeavesBig` = TRUE
""");
    }

    public override async Task Can_query_all_types_when_shared_column(bool async)
    {
        await base.Can_query_all_types_when_shared_column(async);

        AssertSql(
            """
SELECT `d`.`Id`, `d`.`SortIndex`, CVar(NULL) AS `CaffeineGrams`, CVar(NULL) AS `CokeCO2`, CVar(NULL) AS `SugarGrams`, CVar(NULL) AS `LiltCO2`, CVar(NULL) AS `SugarGrams0`, CVar(NULL) AS `CaffeineGrams0`, CVar(NULL) AS `HasMilk`, 'Drink' AS `Discriminator`
FROM `Drinks` AS `d`
UNION ALL
SELECT `c`.`Id`, `c`.`SortIndex`, `c`.`CaffeineGrams`, `c`.`CokeCO2`, `c`.`SugarGrams`, CVar(NULL) AS `LiltCO2`, CVar(NULL) AS `SugarGrams0`, CVar(NULL) AS `CaffeineGrams0`, CVar(NULL) AS `HasMilk`, 'Coke' AS `Discriminator`
FROM `Coke` AS `c`
UNION ALL
SELECT `l`.`Id`, `l`.`SortIndex`, CVar(NULL) AS `CaffeineGrams`, CVar(NULL) AS `CokeCO2`, CVar(NULL) AS `SugarGrams`, `l`.`LiltCO2`, `l`.`SugarGrams` AS `SugarGrams0`, CVar(NULL) AS `CaffeineGrams0`, CVar(NULL) AS `HasMilk`, 'Lilt' AS `Discriminator`
FROM `Lilt` AS `l`
UNION ALL
SELECT `t`.`Id`, `t`.`SortIndex`, CVar(NULL) AS `CaffeineGrams`, CVar(NULL) AS `CokeCO2`, CVar(NULL) AS `SugarGrams`, CVar(NULL) AS `LiltCO2`, CVar(NULL) AS `SugarGrams0`, `t`.`CaffeineGrams` AS `CaffeineGrams0`, `t`.`HasMilk`, 'Tea' AS `Discriminator`
FROM `Tea` AS `t`
""");
    }

    public override async Task Can_query_just_kiwis(bool async)
    {
        await base.Can_query_just_kiwis(async);

        AssertSql(
            """
SELECT TOP 2 `k`.`Id`, `k`.`CountryId`, `k`.`Name`, `k`.`Species`, `k`.`EagleId`, `k`.`IsFlightless`, `k`.`FoundOn`
FROM `Kiwi` AS `k`
""");
    }

    public override async Task Can_query_just_roses(bool async)
    {
        await base.Can_query_just_roses(async);

        AssertSql(
            """
SELECT TOP 2 `r`.`Species`, `r`.`CountryId`, `r`.`Genus`, `r`.`Name`, `r`.`HasThorns`
FROM `Roses` AS `r`
""");
    }

    public override async Task Can_query_when_shared_column(bool async)
    {
        await base.Can_query_when_shared_column(async);

        AssertSql(
            """
SELECT TOP 2 `c`.`Id`, `c`.`SortIndex`, `c`.`CaffeineGrams`, `c`.`CokeCO2`, `c`.`SugarGrams`
FROM `Coke` AS `c`
""",
            //
            """
SELECT TOP 2 `l`.`Id`, `l`.`SortIndex`, `l`.`LiltCO2`, `l`.`SugarGrams`
FROM `Lilt` AS `l`
""",
            //
            """
SELECT TOP 2 `t`.`Id`, `t`.`SortIndex`, `t`.`CaffeineGrams`, `t`.`HasMilk`
FROM `Tea` AS `t`
""");
    }

    public override async Task Can_use_backwards_is_animal(bool async)
    {
        await base.Can_use_backwards_is_animal(async);

        AssertSql(
            """
SELECT `k`.`Id`, `k`.`CountryId`, `k`.`Name`, `k`.`Species`, `k`.`EagleId`, `k`.`IsFlightless`, `k`.`FoundOn`
FROM `Kiwi` AS `k`
""");
    }

    public override async Task Can_use_backwards_of_type_animal(bool async)
    {
        await base.Can_use_backwards_of_type_animal(async);

        AssertSql(
            """
SELECT `k`.`Id`, `k`.`CountryId`, `k`.`Name`, `k`.`Species`, `k`.`EagleId`, `k`.`IsFlightless`, `k`.`FoundOn`
FROM `Kiwi` AS `k`
""");
    }

    public override async Task Can_use_is_kiwi(bool async)
    {
        await base.Can_use_is_kiwi(async);

        AssertSql(
            """
SELECT `k`.`Id`, `k`.`CountryId`, `k`.`Name`, `k`.`Species`, `k`.`EagleId`, `k`.`IsFlightless`, CVar(NULL) AS `Group`, `k`.`FoundOn`, 'Kiwi' AS `Discriminator`
FROM `Kiwi` AS `k`
""");
    }

    public override async Task Can_use_is_kiwi_with_cast(bool async)
    {
        await base.Can_use_is_kiwi_with_cast(async);

        AssertSql(
            """
SELECT IIF(`u`.`Discriminator` = 'Kiwi', `u`.`FoundOn`, CBYTE(0)) AS `Value`
FROM (
    SELECT CVar(NULL) AS `FoundOn`, 'Eagle' AS `Discriminator`
    FROM `Eagle` AS `e`
    UNION ALL
    SELECT `k`.`FoundOn`, 'Kiwi' AS `Discriminator`
    FROM `Kiwi` AS `k`
) AS `u`
""");
    }

    public override async Task Can_use_is_kiwi_in_projection(bool async)
    {
        await base.Can_use_is_kiwi_in_projection(async);

        AssertSql(
            """
SELECT IIF(`u`.`Discriminator` = 'Kiwi', TRUE, FALSE)
FROM (
    SELECT 'Eagle' AS `Discriminator`
    FROM `Eagle` AS `e`
    UNION ALL
    SELECT 'Kiwi' AS `Discriminator`
    FROM `Kiwi` AS `k`
) AS `u`
""");
    }

    public override async Task Can_use_is_kiwi_with_other_predicate(bool async)
    {
        await base.Can_use_is_kiwi_with_other_predicate(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`CountryId`, `u`.`Name`, `u`.`Species`, `u`.`EagleId`, `u`.`IsFlightless`, `u`.`Group`, `u`.`FoundOn`, `u`.`Discriminator`
FROM (
    SELECT `e`.`Id`, `e`.`CountryId`, `e`.`Name`, `e`.`Species`, `e`.`EagleId`, `e`.`IsFlightless`, `e`.`Group`, CVar(NULL) AS `FoundOn`, 'Eagle' AS `Discriminator`
    FROM `Eagle` AS `e`
    UNION ALL
    SELECT `k`.`Id`, `k`.`CountryId`, `k`.`Name`, `k`.`Species`, `k`.`EagleId`, `k`.`IsFlightless`, CVar(NULL) AS `Group`, `k`.`FoundOn`, 'Kiwi' AS `Discriminator`
    FROM `Kiwi` AS `k`
) AS `u`
WHERE `u`.`Discriminator` = 'Kiwi' AND `u`.`CountryId` = 1
""");
    }

    public override async Task Can_use_of_type_animal(bool async)
    {
        await base.Can_use_of_type_animal(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`CountryId`, `u`.`Name`, `u`.`Species`, `u`.`EagleId`, `u`.`IsFlightless`, `u`.`Group`, `u`.`FoundOn`, `u`.`Discriminator`
FROM (
    SELECT `e`.`Id`, `e`.`CountryId`, `e`.`Name`, `e`.`Species`, `e`.`EagleId`, `e`.`IsFlightless`, `e`.`Group`, CVar(NULL) AS `FoundOn`, 'Eagle' AS `Discriminator`
    FROM `Eagle` AS `e`
    UNION ALL
    SELECT `k`.`Id`, `k`.`CountryId`, `k`.`Name`, `k`.`Species`, `k`.`EagleId`, `k`.`IsFlightless`, CVar(NULL) AS `Group`, `k`.`FoundOn`, 'Kiwi' AS `Discriminator`
    FROM `Kiwi` AS `k`
) AS `u`
ORDER BY `u`.`Species`
""");
    }

    public override async Task Can_use_of_type_bird(bool async)
    {
        await base.Can_use_of_type_bird(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`CountryId`, `u`.`Name`, `u`.`Species`, `u`.`EagleId`, `u`.`IsFlightless`, `u`.`Group`, `u`.`FoundOn`, `u`.`Discriminator`
FROM (
    SELECT `e`.`Id`, `e`.`CountryId`, `e`.`Name`, `e`.`Species`, `e`.`EagleId`, `e`.`IsFlightless`, `e`.`Group`, CVar(NULL) AS `FoundOn`, 'Eagle' AS `Discriminator`
    FROM `Eagle` AS `e`
    UNION ALL
    SELECT `k`.`Id`, `k`.`CountryId`, `k`.`Name`, `k`.`Species`, `k`.`EagleId`, `k`.`IsFlightless`, CVar(NULL) AS `Group`, `k`.`FoundOn`, 'Kiwi' AS `Discriminator`
    FROM `Kiwi` AS `k`
) AS `u`
ORDER BY `u`.`Species`
""");
    }

    public override async Task Can_use_of_type_bird_first(bool async)
    {
        await base.Can_use_of_type_bird_first(async);

        AssertSql(
            """
SELECT TOP 1 `u`.`Id`, `u`.`CountryId`, `u`.`Name`, `u`.`Species`, `u`.`EagleId`, `u`.`IsFlightless`, `u`.`Group`, `u`.`FoundOn`, `u`.`Discriminator`
FROM (
    SELECT `e`.`Id`, `e`.`CountryId`, `e`.`Name`, `e`.`Species`, `e`.`EagleId`, `e`.`IsFlightless`, `e`.`Group`, CVar(NULL) AS `FoundOn`, 'Eagle' AS `Discriminator`
    FROM `Eagle` AS `e`
    UNION ALL
    SELECT `k`.`Id`, `k`.`CountryId`, `k`.`Name`, `k`.`Species`, `k`.`EagleId`, `k`.`IsFlightless`, CVar(NULL) AS `Group`, `k`.`FoundOn`, 'Kiwi' AS `Discriminator`
    FROM `Kiwi` AS `k`
) AS `u`
ORDER BY `u`.`Species`
""");
    }

    public override async Task Can_use_of_type_bird_predicate(bool async)
    {
        await base.Can_use_of_type_bird_predicate(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`CountryId`, `u`.`Name`, `u`.`Species`, `u`.`EagleId`, `u`.`IsFlightless`, `u`.`Group`, `u`.`FoundOn`, `u`.`Discriminator`
FROM (
    SELECT `e`.`Id`, `e`.`CountryId`, `e`.`Name`, `e`.`Species`, `e`.`EagleId`, `e`.`IsFlightless`, `e`.`Group`, CVar(NULL) AS `FoundOn`, 'Eagle' AS `Discriminator`
    FROM `Eagle` AS `e`
    UNION ALL
    SELECT `k`.`Id`, `k`.`CountryId`, `k`.`Name`, `k`.`Species`, `k`.`EagleId`, `k`.`IsFlightless`, CVar(NULL) AS `Group`, `k`.`FoundOn`, 'Kiwi' AS `Discriminator`
    FROM `Kiwi` AS `k`
) AS `u`
WHERE `u`.`CountryId` = 1
ORDER BY `u`.`Species`
""");
    }

    public override async Task Can_use_of_type_bird_with_projection(bool async)
    {
        await base.Can_use_of_type_bird_with_projection(async);

        AssertSql(
            """
SELECT `e`.`EagleId`
FROM `Eagle` AS `e`
UNION ALL
SELECT `k`.`EagleId`
FROM `Kiwi` AS `k`
""");
    }

    public override async Task Can_use_of_type_kiwi(bool async)
    {
        await base.Can_use_of_type_kiwi(async);

        AssertSql(
            """
SELECT `k`.`Id`, `k`.`CountryId`, `k`.`Name`, `k`.`Species`, `k`.`EagleId`, `k`.`IsFlightless`, `k`.`FoundOn`, 'Kiwi' AS `Discriminator`
FROM `Kiwi` AS `k`
""");
    }

    public override async Task Can_use_of_type_kiwi_where_north_on_derived_property(bool async)
    {
        await base.Can_use_of_type_kiwi_where_north_on_derived_property(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`CountryId`, `u`.`Name`, `u`.`Species`, `u`.`EagleId`, `u`.`IsFlightless`, `u`.`FoundOn`, `u`.`Discriminator`
FROM (
    SELECT `k`.`Id`, `k`.`CountryId`, `k`.`Name`, `k`.`Species`, `k`.`EagleId`, `k`.`IsFlightless`, `k`.`FoundOn`, 'Kiwi' AS `Discriminator`
    FROM `Kiwi` AS `k`
) AS `u`
WHERE `u`.`FoundOn` = CBYTE(0)
""");
    }

    public override async Task Can_use_of_type_kiwi_where_south_on_derived_property(bool async)
    {
        await base.Can_use_of_type_kiwi_where_south_on_derived_property(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`CountryId`, `u`.`Name`, `u`.`Species`, `u`.`EagleId`, `u`.`IsFlightless`, `u`.`FoundOn`, `u`.`Discriminator`
FROM (
    SELECT `k`.`Id`, `k`.`CountryId`, `k`.`Name`, `k`.`Species`, `k`.`EagleId`, `k`.`IsFlightless`, `k`.`FoundOn`, 'Kiwi' AS `Discriminator`
    FROM `Kiwi` AS `k`
) AS `u`
WHERE `u`.`FoundOn` = CBYTE(1)
""");
    }

    public override async Task Can_use_of_type_rose(bool async)
    {
        await base.Can_use_of_type_rose(async);

        AssertSql(
            """
SELECT `r`.`Species`, `r`.`CountryId`, `r`.`Genus`, `r`.`Name`, `r`.`HasThorns`, 'Rose' AS `Discriminator`
FROM `Roses` AS `r`
""");
    }

    public override async Task Member_access_on_intermediate_type_works()
    {
        await base.Member_access_on_intermediate_type_works();

        AssertSql(
            """
SELECT `k`.`Name`
FROM `Kiwi` AS `k`
ORDER BY `k`.`Name`
""");
    }

    public override async Task OfType_Union_OfType(bool async)
    {
        await base.OfType_Union_OfType(async);

        AssertSql();
    }

    public override async Task OfType_Union_subquery(bool async)
    {
        await base.OfType_Union_subquery(async);

        AssertSql();
    }

    public override async Task Setting_foreign_key_to_a_different_type_throws()
        => await base.Setting_foreign_key_to_a_different_type_throws();

    public override async Task Subquery_OfType(bool async)
    {
        await base.Subquery_OfType(async);

        AssertSql(
            """
SELECT DISTINCT `u0`.`Id`, `u0`.`CountryId`, `u0`.`Name`, `u0`.`Species`, `u0`.`EagleId`, `u0`.`IsFlightless`, `u0`.`FoundOn`, `u0`.`Discriminator`
FROM (
    SELECT TOP @p `u`.`Id`, `u`.`CountryId`, `u`.`Name`, `u`.`Species`, `u`.`EagleId`, `u`.`IsFlightless`, `u`.`FoundOn`, `u`.`Discriminator`
    FROM (
        SELECT `e`.`Id`, `e`.`CountryId`, `e`.`Name`, `e`.`Species`, `e`.`EagleId`, `e`.`IsFlightless`, CVar(NULL) AS `FoundOn`, 'Eagle' AS `Discriminator`
        FROM `Eagle` AS `e`
        UNION ALL
        SELECT `k`.`Id`, `k`.`CountryId`, `k`.`Name`, `k`.`Species`, `k`.`EagleId`, `k`.`IsFlightless`, `k`.`FoundOn`, 'Kiwi' AS `Discriminator`
        FROM `Kiwi` AS `k`
    ) AS `u`
    ORDER BY `u`.`Species`
) AS `u0`
WHERE `u0`.`Discriminator` = 'Kiwi'
""");
    }

    public override async Task Union_entity_equality(bool async)
    {
        await base.Union_entity_equality(async);

        AssertSql();
    }

    public override async Task Union_siblings_with_duplicate_property_in_subquery(bool async)
    {
        await base.Union_siblings_with_duplicate_property_in_subquery(async);

        AssertSql();
    }

    public override async Task Is_operator_on_result_of_FirstOrDefault(bool async)
    {
        await base.Is_operator_on_result_of_FirstOrDefault(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`CountryId`, `u`.`Name`, `u`.`Species`, `u`.`EagleId`, `u`.`IsFlightless`, `u`.`Group`, `u`.`FoundOn`, `u`.`Discriminator`
FROM (
    SELECT `e`.`Id`, `e`.`CountryId`, `e`.`Name`, `e`.`Species`, `e`.`EagleId`, `e`.`IsFlightless`, `e`.`Group`, CVar(NULL) AS `FoundOn`, 'Eagle' AS `Discriminator`
    FROM `Eagle` AS `e`
    UNION ALL
    SELECT `k`.`Id`, `k`.`CountryId`, `k`.`Name`, `k`.`Species`, `k`.`EagleId`, `k`.`IsFlightless`, CVar(NULL) AS `Group`, `k`.`FoundOn`, 'Kiwi' AS `Discriminator`
    FROM `Kiwi` AS `k`
) AS `u`
WHERE EXISTS (
    SELECT 1
    FROM (
        SELECT TOP 1 `u0`.`Discriminator`
        FROM (
            SELECT `e0`.`Name`, 'Eagle' AS `Discriminator`
            FROM `Eagle` AS `e0`
            UNION ALL
            SELECT `k0`.`Name`, 'Kiwi' AS `Discriminator`
            FROM `Kiwi` AS `k0`
        ) AS `u0`
        WHERE `u0`.`Name` = 'Great spotted kiwi'
    ) AS `u1`
    WHERE `u1`.`Discriminator` = 'Kiwi')
ORDER BY `u`.`Species`
""");
    }

    public override async Task Selecting_only_base_properties_on_base_type(bool async)
    {
        await base.Selecting_only_base_properties_on_base_type(async);

        AssertSql(
            """
SELECT `e`.`Name`
FROM `Eagle` AS `e`
UNION ALL
SELECT `k`.`Name`
FROM `Kiwi` AS `k`
""");
    }

    public override async Task Selecting_only_base_properties_on_derived_type(bool async)
    {
        await base.Selecting_only_base_properties_on_derived_type(async);

        AssertSql(
            """
SELECT `e`.`Name`
FROM `Eagle` AS `e`
UNION ALL
SELECT `k`.`Name`
FROM `Kiwi` AS `k`
""");
    }

    public override async Task Can_query_all_animal_views(bool async)
    {
        await base.Can_query_all_animal_views(async);

        AssertSql();
    }

    public override async Task Discriminator_used_when_projection_over_derived_type(bool async)
    {
        await base.Discriminator_used_when_projection_over_derived_type(async);

        AssertSql();
    }

    public override async Task Discriminator_used_when_projection_over_derived_type2(bool async)
    {
        await base.Discriminator_used_when_projection_over_derived_type2(async);

        AssertSql();
    }

    public override async Task Discriminator_used_when_projection_over_of_type(bool async)
    {
        await base.Discriminator_used_when_projection_over_of_type(async);

        AssertSql();
    }

    public override async Task Discriminator_with_cast_in_shadow_property(bool async)
    {
        await base.Discriminator_with_cast_in_shadow_property(async);

        AssertSql();
    }

    public override void Using_from_sql_throws()
    {
        base.Using_from_sql_throws();

        AssertSql();
    }

    public override async Task Using_is_operator_on_multiple_type_with_no_result(bool async)
    {
        await base.Using_is_operator_on_multiple_type_with_no_result(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`CountryId`, `u`.`Name`, `u`.`Species`, `u`.`EagleId`, `u`.`IsFlightless`, `u`.`Group`, `u`.`FoundOn`, `u`.`Discriminator`
FROM (
    SELECT `k`.`Id`, `k`.`CountryId`, `k`.`Name`, `k`.`Species`, `k`.`EagleId`, `k`.`IsFlightless`, CVar(NULL) AS `Group`, `k`.`FoundOn`, 'Kiwi' AS `Discriminator`
    FROM `Kiwi` AS `k`
) AS `u`
WHERE `u`.`Discriminator` = 'Eagle'
""");
    }

    public override async Task Using_is_operator_with_of_type_on_multiple_type_with_no_result(bool async)
    {
        await base.Using_is_operator_with_of_type_on_multiple_type_with_no_result(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`CountryId`, `u`.`Name`, `u`.`Species`, `u`.`EagleId`, `u`.`IsFlightless`, `u`.`Group`, `u`.`Discriminator`
FROM (
    SELECT `k`.`Id`, `k`.`CountryId`, `k`.`Name`, `k`.`Species`, `k`.`EagleId`, `k`.`IsFlightless`, CVar(NULL) AS `Group`, 'Kiwi' AS `Discriminator`
    FROM `Kiwi` AS `k`
) AS `u`
WHERE `u`.`Discriminator` = 'Eagle'
""");
    }

    public override async Task Using_OfType_on_multiple_type_with_no_result(bool async)
    {
        await base.Using_OfType_on_multiple_type_with_no_result(async);

        AssertSql();
    }

    public override async Task GetType_in_hierarchy_in_abstract_base_type(bool async)
    {
        await base.GetType_in_hierarchy_in_abstract_base_type(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`CountryId`, `u`.`Name`, `u`.`Species`, `u`.`EagleId`, `u`.`IsFlightless`, `u`.`Group`, `u`.`FoundOn`, `u`.`Discriminator`
FROM (
    SELECT `e`.`Id`, `e`.`CountryId`, `e`.`Name`, `e`.`Species`, `e`.`EagleId`, `e`.`IsFlightless`, `e`.`Group`, CVar(NULL) AS `FoundOn`, 'Eagle' AS `Discriminator`
    FROM `Eagle` AS `e`
    UNION ALL
    SELECT `k`.`Id`, `k`.`CountryId`, `k`.`Name`, `k`.`Species`, `k`.`EagleId`, `k`.`IsFlightless`, CVar(NULL) AS `Group`, `k`.`FoundOn`, 'Kiwi' AS `Discriminator`
    FROM `Kiwi` AS `k`
) AS `u`
WHERE 0 = 1
""");
    }

    public override async Task GetType_in_hierarchy_in_intermediate_type(bool async)
    {
        await base.GetType_in_hierarchy_in_intermediate_type(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`CountryId`, `u`.`Name`, `u`.`Species`, `u`.`EagleId`, `u`.`IsFlightless`, `u`.`Group`, `u`.`FoundOn`, `u`.`Discriminator`
FROM (
    SELECT `e`.`Id`, `e`.`CountryId`, `e`.`Name`, `e`.`Species`, `e`.`EagleId`, `e`.`IsFlightless`, `e`.`Group`, CVar(NULL) AS `FoundOn`, 'Eagle' AS `Discriminator`
    FROM `Eagle` AS `e`
    UNION ALL
    SELECT `k`.`Id`, `k`.`CountryId`, `k`.`Name`, `k`.`Species`, `k`.`EagleId`, `k`.`IsFlightless`, CVar(NULL) AS `Group`, `k`.`FoundOn`, 'Kiwi' AS `Discriminator`
    FROM `Kiwi` AS `k`
) AS `u`
WHERE 0 = 1
""");
    }

    public override async Task GetType_in_hierarchy_in_leaf_type_with_sibling(bool async)
    {
        await base.GetType_in_hierarchy_in_leaf_type_with_sibling(async);

        AssertSql(
            """
SELECT `e`.`Id`, `e`.`CountryId`, `e`.`Name`, `e`.`Species`, `e`.`EagleId`, `e`.`IsFlightless`, `e`.`Group`, CVar(NULL) AS `FoundOn`, 'Eagle' AS `Discriminator`
FROM `Eagle` AS `e`
""");
    }

    public override async Task GetType_in_hierarchy_in_leaf_type_with_sibling2(bool async)
    {
        await base.GetType_in_hierarchy_in_leaf_type_with_sibling2(async);

        AssertSql(
            """
SELECT `k`.`Id`, `k`.`CountryId`, `k`.`Name`, `k`.`Species`, `k`.`EagleId`, `k`.`IsFlightless`, CVar(NULL) AS `Group`, `k`.`FoundOn`, 'Kiwi' AS `Discriminator`
FROM `Kiwi` AS `k`
""");
    }

    public override async Task GetType_in_hierarchy_in_leaf_type_with_sibling2_reverse(bool async)
    {
        await base.GetType_in_hierarchy_in_leaf_type_with_sibling2_reverse(async);

        AssertSql(
            """
SELECT `k`.`Id`, `k`.`CountryId`, `k`.`Name`, `k`.`Species`, `k`.`EagleId`, `k`.`IsFlightless`, CVar(NULL) AS `Group`, `k`.`FoundOn`, 'Kiwi' AS `Discriminator`
FROM `Kiwi` AS `k`
""");
    }

    public override async Task GetType_in_hierarchy_in_leaf_type_with_sibling2_not_equal(bool async)
    {
        await base.GetType_in_hierarchy_in_leaf_type_with_sibling2_not_equal(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`CountryId`, `u`.`Name`, `u`.`Species`, `u`.`EagleId`, `u`.`IsFlightless`, `u`.`Group`, `u`.`FoundOn`, `u`.`Discriminator`
FROM (
    SELECT `e`.`Id`, `e`.`CountryId`, `e`.`Name`, `e`.`Species`, `e`.`EagleId`, `e`.`IsFlightless`, `e`.`Group`, CVar(NULL) AS `FoundOn`, 'Eagle' AS `Discriminator`
    FROM `Eagle` AS `e`
    UNION ALL
    SELECT `k`.`Id`, `k`.`CountryId`, `k`.`Name`, `k`.`Species`, `k`.`EagleId`, `k`.`IsFlightless`, CVar(NULL) AS `Group`, `k`.`FoundOn`, 'Kiwi' AS `Discriminator`
    FROM `Kiwi` AS `k`
) AS `u`
WHERE `u`.`Discriminator` <> 'Kiwi'
""");
    }

    private void AssertSql(params string[] expected)
        => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);
}
