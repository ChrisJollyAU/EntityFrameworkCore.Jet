﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using EntityFrameworkCore.Jet.FunctionalTests.TestUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;
using System.Threading.Tasks;

namespace EntityFrameworkCore.Jet.FunctionalTests.Query;

#nullable disable

public class AdHocAdvancedMappingsQueryJetTest(NonSharedFixture fixture) : AdHocAdvancedMappingsQueryRelationalTestBase(fixture)
{
    protected override ITestStoreFactory TestStoreFactory
        => JetTestStoreFactory.Instance;

    public override async Task Setting_IsUnicode_generates_unicode_literal_in_SQL()
    {
        await base.Setting_IsUnicode_generates_unicode_literal_in_SQL();

        AssertSql(
            """
SELECT `t`.`Id`, `t`.`Nombre`
FROM `TipoServicio` AS `t`
WHERE `t`.`Nombre` LIKE '%lla%'
""");
    }

    public override async Task Projecting_correlated_collection_along_with_non_mapped_property()
    {
        await base.Projecting_correlated_collection_along_with_non_mapped_property();

        AssertSql(
            """
SELECT `b`.`Id`, `p0`.`Id`, `p0`.`BlogId`, `p0`.`Name`
FROM `Blogs` AS `b`
LEFT JOIN (
    SELECT `p`.`Id`, `p`.`BlogId`, `p`.`Name`
    FROM `Posts` AS `p`
    WHERE `p`.`Name` LIKE '%2%'
) AS `p0` ON `b`.`Id` = `p0`.`BlogId`
ORDER BY `b`.`Id`
""",
            //
            """
SELECT `b`.`Id`, (
    SELECT TOP 1 `p`.`Name`
    FROM `Posts` AS `p`
    WHERE `b`.`Id` = `p`.`BlogId`
    ORDER BY `p`.`Id`)
FROM `Blogs` AS `b`
""");
    }

    public override async Task Projection_failing_with_EnumToStringConverter()
    {
        await base.Projection_failing_with_EnumToStringConverter();

        AssertSql(
            """
SELECT `p`.`Id`, `p`.`Name`, IIF(`c`.`Id` IS NULL, 'Other', `c`.`Name`) AS `CategoryName`, IIF(`c`.`Id` IS NULL, 'Active', `c`.`Status`) AS `CategoryStatus`
FROM `Products` AS `p`
LEFT JOIN `Categories` AS `c` ON `p`.`CategoryId` = `c`.`Id`
""");
    }

    public override async Task Expression_tree_constructed_via_interface_works()
    {
        await base.Expression_tree_constructed_via_interface_works();

        AssertSql(
            """
SELECT `r`.`Id`, `r`.`IsRemoved`, `r`.`Removed`, `r`.`RemovedByUser`, `r`.`OwnedEntity_Exists`, `r`.`OwnedEntity_OwnedValue`
FROM `RemovableEntities` AS `r`
WHERE `r`.`IsRemoved` = FALSE
""",
            //
            """
SELECT `p`.`Id`, `p`.`RemovableEntityId`
FROM `Parents` AS `p`
LEFT JOIN `RemovableEntities` AS `r` ON `p`.`RemovableEntityId` = `r`.`Id`
WHERE `r`.`IsRemoved` = TRUE
""",
            //
            """
SELECT `r`.`Id`, `r`.`IsRemoved`, `r`.`Removed`, `r`.`RemovedByUser`, `r`.`OwnedEntity_Exists`, `r`.`OwnedEntity_OwnedValue`
FROM `RemovableEntities` AS `r`
WHERE `r`.`OwnedEntity_OwnedValue` = 'Abc'
""",
            //
            """
@id='1'

SELECT `p`.`Id`, `p`.`RemovableEntityId`
FROM `Parents` AS `p`
WHERE `p`.`Id` = @id
""");
    }

    public override async Task Double_convert_interface_created_expression_tree()
    {
        await base.Double_convert_interface_created_expression_tree();

        AssertSql(
            """
@action='1'

SELECT COUNT(*)
FROM `Offers` AS `o`
WHERE EXISTS (
    SELECT 1
    FROM `OfferActions` AS `o0`
    WHERE `o`.`Id` = `o0`.`OfferId` AND `o0`.`Action` = @action)
""");
    }

    public override async Task Casts_are_removed_from_expression_tree_when_redundant()
    {
        await base.Casts_are_removed_from_expression_tree_when_redundant();

        AssertSql(
            """
@id='1'

SELECT TOP 1 `m`.`Id`, `m`.`Name`, `m`.`NavigationEntityId`
FROM `MockEntities` AS `m`
WHERE `m`.`Id` = @id
""",
            //
            """
SELECT COUNT(*)
FROM `MockEntities` AS `m`
""");
    }

    public override async Task Can_query_hierarchy_with_non_nullable_property_on_derived()
    {
        await base.Can_query_hierarchy_with_non_nullable_property_on_derived();

        AssertSql(
            """
SELECT `b`.`Id`, `b`.`Name`, `b`.`Type`, `b`.`IsOnline`
FROM `Businesses` AS `b`
""");
    }

    public override async Task Query_generates_correct_datetime2_parameter_definition(int? fractionalSeconds, string postfix)
    {
        await base.Query_generates_correct_datetime2_parameter_definition(fractionalSeconds, postfix);

        AssertSql(
            """
@parameter='2021-11-12T13:14:15.0000000' (DbType = DateTime)

SELECT TOP 1 `e`.`DateTime`
FROM `Entities` AS `e`
WHERE `e`.`DateTime` = CDATE(@parameter)
""");
    }

    public override async Task Query_generates_correct_datetimeoffset_parameter_definition(int? fractionalSeconds, string postfix)
    {
        await base.Query_generates_correct_datetimeoffset_parameter_definition(fractionalSeconds, postfix);

        AssertSql(
            """
@parameter='2021-11-12T03:14:15.0000000Z' (DbType = DateTime)

SELECT TOP 1 `e`.`DateTimeOffset`
FROM `Entities` AS `e`
WHERE `e`.`DateTimeOffset` = @parameter
""");
    }

    public override async Task Query_generates_correct_timespan_parameter_definition(int? fractionalSeconds, string postfix)
    {
        await base.Query_generates_correct_timespan_parameter_definition(fractionalSeconds, postfix);

        AssertSql(
            """
@parameter='12:34:56.7890123'

SELECT TOP 1 `e`.`TimeSpan`
FROM `Entities` AS `e`
WHERE `e`.`TimeSpan` = @parameter
""");
    }

    public override async Task Hierarchy_query_with_abstract_type_sibling(bool async)
    {
        await base.Hierarchy_query_with_abstract_type_sibling(async);

        AssertSql(
            """
SELECT `a`.`Id`, `a`.`Discriminator`, `a`.`Species`, `a`.`Name`, `a`.`EdcuationLevel`, `a`.`FavoriteToy`
FROM `Animals` AS `a`
WHERE `a`.`Discriminator` IN ('Cat', 'Dog') AND (`a`.`Species` LIKE 'F%')
""");
    }

    public override async Task Hierarchy_query_with_abstract_type_sibling_TPT(bool async)
    {
        await base.Hierarchy_query_with_abstract_type_sibling_TPT(async);

        AssertSql(
            """
SELECT `a`.`Id`, `a`.`Species`, `p`.`Name`, `c`.`EdcuationLevel`, `d`.`FavoriteToy`, IIF(`d`.`Id` IS NOT NULL, 'Dog', IIF(`c`.`Id` IS NOT NULL, 'Cat', NULL)) AS `Discriminator`
FROM ((`Animals` AS `a`
LEFT JOIN `Pets` AS `p` ON `a`.`Id` = `p`.`Id`)
LEFT JOIN `Cats` AS `c` ON `a`.`Id` = `c`.`Id`)
LEFT JOIN `Dogs` AS `d` ON `a`.`Id` = `d`.`Id`
WHERE (`d`.`Id` IS NOT NULL OR `c`.`Id` IS NOT NULL) AND (`a`.`Species` LIKE 'F%')
""");
    }

    public override async Task Hierarchy_query_with_abstract_type_sibling_TPC(bool async)
    {
        await base.Hierarchy_query_with_abstract_type_sibling_TPC(async);

        AssertSql(
            """
SELECT `u`.`Id`, `u`.`Species`, `u`.`Name`, `u`.`EdcuationLevel`, `u`.`FavoriteToy`, `u`.`Discriminator`
FROM (
    SELECT `c`.`Id`, `c`.`Species`, `c`.`Name`, `c`.`EdcuationLevel`, NULL AS `FavoriteToy`, 'Cat' AS `Discriminator`
    FROM `Cats` AS `c`
    UNION ALL
    SELECT `d`.`Id`, `d`.`Species`, `d`.`Name`, NULL AS `EdcuationLevel`, `d`.`FavoriteToy`, 'Dog' AS `Discriminator`
    FROM `Dogs` AS `d`
) AS `u`
WHERE `u`.`Species` LIKE 'F%'
""");
    }

    public override async Task Two_similar_complex_properties_projected_with_split_query1()
    {
        await base.Two_similar_complex_properties_projected_with_split_query1();

        AssertSql(
            """
SELECT `o`.`Id`
FROM `Offers` AS `o`
ORDER BY `o`.`Id`
""",
            //
            """
SELECT `s`.`Id`, `s`.`NestedId`, `s`.`OfferId`, `s`.`payment_brutto`, `s`.`payment_netto`, `s`.`Id0`, `s`.`payment_brutto0`, `s`.`payment_netto0`, `o`.`Id`
FROM `Offers` AS `o`
INNER JOIN (
    SELECT `v`.`Id`, `v`.`NestedId`, `v`.`OfferId`, `v`.`payment_brutto`, `v`.`payment_netto`, `n`.`Id` AS `Id0`, `n`.`payment_brutto` AS `payment_brutto0`, `n`.`payment_netto` AS `payment_netto0`
    FROM `Variation` AS `v`
    LEFT JOIN `NestedEntity` AS `n` ON `v`.`NestedId` = `n`.`Id`
) AS `s` ON `o`.`Id` = `s`.`OfferId`
ORDER BY `o`.`Id`
""");
    }

    public override async Task Two_similar_complex_properties_projected_with_split_query2()
    {
        await base.Two_similar_complex_properties_projected_with_split_query2();

        AssertSql(
            """
SELECT TOP 2 `o`.`Id`
FROM `Offers` AS `o`
WHERE `o`.`Id` = 1
ORDER BY `o`.`Id`
""",
            //
            """
SELECT `s`.`Id`, `s`.`NestedId`, `s`.`OfferId`, `s`.`payment_brutto`, `s`.`payment_netto`, `s`.`Id0`, `s`.`payment_brutto0`, `s`.`payment_netto0`, `o0`.`Id`
FROM (
    SELECT TOP 1 `o`.`Id`
    FROM `Offers` AS `o`
    WHERE `o`.`Id` = 1
    ORDER BY `o`.`Id`
) AS `o0`
INNER JOIN (
    SELECT `v`.`Id`, `v`.`NestedId`, `v`.`OfferId`, `v`.`payment_brutto`, `v`.`payment_netto`, `n`.`Id` AS `Id0`, `n`.`payment_brutto` AS `payment_brutto0`, `n`.`payment_netto` AS `payment_netto0`
    FROM `Variation` AS `v`
    LEFT JOIN `NestedEntity` AS `n` ON `v`.`NestedId` = `n`.`Id`
) AS `s` ON `o0`.`Id` = `s`.`OfferId`
ORDER BY `o0`.`Id`
""");
    }

    public override async Task Projecting_one_of_two_similar_complex_types_picks_the_correct_one()
    {
        await base.Projecting_one_of_two_similar_complex_types_picks_the_correct_one();

        AssertSql(
            """
SELECT `a`.`Id`, `s`.`Info_Created0` AS `Created`
FROM (
    SELECT TOP @p `c`.`Id`, `b`.`AId`, `b`.`Info_Created` AS `Info_Created0`
    FROM `Cs` AS `c`
    INNER JOIN `Bs` AS `b` ON `c`.`BId` = `b`.`Id`
    WHERE `b`.`AId` = 1
    ORDER BY `c`.`Id`
) AS `s`
LEFT JOIN `As` AS `a` ON `s`.`AId` = `a`.`Id`
ORDER BY `s`.`Id`
""");
    }
}
