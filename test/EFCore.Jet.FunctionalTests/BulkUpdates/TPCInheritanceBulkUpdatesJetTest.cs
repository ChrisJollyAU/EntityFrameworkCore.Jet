﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.BulkUpdates;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;
using Xunit.Abstractions;

namespace EntityFrameworkCore.Jet.FunctionalTests.BulkUpdates;

public class TPCInheritanceBulkUpdatesJetTest(
    TPCInheritanceBulkUpdatesJetFixture fixture,
    ITestOutputHelper testOutputHelper)
    : TPCInheritanceBulkUpdatesTestBase<TPCInheritanceBulkUpdatesJetFixture>(fixture, testOutputHelper)
{
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override async Task Delete_where_hierarchy(bool async)
    {
        await base.Delete_where_hierarchy(async);

        AssertSql();
    }

    public override async Task Delete_where_hierarchy_derived(bool async)
    {
        await base.Delete_where_hierarchy_derived(async);

        AssertSql(
            """
DELETE FROM `Kiwi` AS `k`
WHERE `k`.`Name` = 'Great spotted kiwi'
""");
    }

    public override async Task Delete_where_using_hierarchy(bool async)
    {
        await base.Delete_where_using_hierarchy(async);

        AssertSql(
            """
DELETE FROM `Countries` AS `c`
WHERE (
    SELECT COUNT(*)
    FROM (
        SELECT `e`.`CountryId`
        FROM `Eagle` AS `e`
        UNION ALL
        SELECT `k`.`CountryId`
        FROM `Kiwi` AS `k`
    ) AS `u`
    WHERE `c`.`Id` = `u`.`CountryId` AND `u`.`CountryId` > 0) > 0
""");
    }

    public override async Task Delete_where_using_hierarchy_derived(bool async)
    {
        await base.Delete_where_using_hierarchy_derived(async);

        AssertSql(
            """
DELETE FROM `Countries` AS `c`
WHERE (
    SELECT COUNT(*)
    FROM (
        SELECT `k`.`CountryId`
        FROM `Kiwi` AS `k`
    ) AS `u`
    WHERE `c`.`Id` = `u`.`CountryId` AND `u`.`CountryId` > 0) > 0
""");
    }

    public override async Task Delete_where_keyless_entity_mapped_to_sql_query(bool async)
    {
        await base.Delete_where_keyless_entity_mapped_to_sql_query(async);

        AssertSql();
    }

    public override async Task Delete_where_hierarchy_subquery(bool async)
    {
        await base.Delete_where_hierarchy_subquery(async);

        AssertSql();
    }

    public override async Task Delete_GroupBy_Where_Select_First(bool async)
    {
        await base.Delete_GroupBy_Where_Select_First(async);

        AssertSql();
    }

    public override async Task Delete_GroupBy_Where_Select_First_2(bool async)
    {
        await base.Delete_GroupBy_Where_Select_First_2(async);

        AssertSql();
    }

    public override async Task Delete_GroupBy_Where_Select_First_3(bool async)
    {
        await base.Delete_GroupBy_Where_Select_First_3(async);

        AssertSql();
    }

    public override async Task Update_base_type(bool async)
    {
        await base.Update_base_type(async);

        AssertExecuteUpdateSql();
    }

    public override async Task Update_base_type_with_OfType(bool async)
    {
        await base.Update_base_type_with_OfType(async);

        AssertExecuteUpdateSql();
    }

    public override async Task Update_where_hierarchy_subquery(bool async)
    {
        await base.Update_where_hierarchy_subquery(async);

        AssertExecuteUpdateSql();
    }

    public override async Task Update_base_property_on_derived_type(bool async)
    {
        await base.Update_base_property_on_derived_type(async);

        AssertExecuteUpdateSql(
            """
@p='SomeOtherKiwi' (Size = 255)

UPDATE `Kiwi` AS `k`
SET `k`.`Name` = @p
""");
    }

    public override async Task Update_derived_property_on_derived_type(bool async)
    {
        await base.Update_derived_property_on_derived_type(async);

        AssertExecuteUpdateSql(
            """
@p='0' (Size = 1)

UPDATE `Kiwi` AS `k`
SET `k`.`FoundOn` = @p
""");
    }

    public override async Task Update_where_using_hierarchy(bool async)
    {
        await base.Update_where_using_hierarchy(async);

        AssertExecuteUpdateSql(
            """
@p='Monovia' (Size = 255)

UPDATE `Countries` AS `c`
SET `c`.`Name` = @p
WHERE (
    SELECT COUNT(*)
    FROM (
        SELECT `e`.`CountryId`
        FROM `Eagle` AS `e`
        UNION ALL
        SELECT `k`.`CountryId`
        FROM `Kiwi` AS `k`
    ) AS `u`
    WHERE `c`.`Id` = `u`.`CountryId` AND `u`.`CountryId` > 0) > 0
""");
    }

    public override async Task Update_base_and_derived_types(bool async)
    {
        await base.Update_base_and_derived_types(async);

        AssertExecuteUpdateSql(
            """
@p='Kiwi' (Size = 255)
@p0='0' (Size = 1)

UPDATE `Kiwi` AS `k`
SET `k`.`Name` = @p,
    `k`.`FoundOn` = @p0
""");
    }

    public override async Task Update_where_using_hierarchy_derived(bool async)
    {
        await base.Update_where_using_hierarchy_derived(async);

        AssertExecuteUpdateSql(
            """
@p='Monovia' (Size = 255)

UPDATE `Countries` AS `c`
SET `c`.`Name` = @p
WHERE (
    SELECT COUNT(*)
    FROM (
        SELECT `k`.`CountryId`
        FROM `Kiwi` AS `k`
    ) AS `u`
    WHERE `c`.`Id` = `u`.`CountryId` AND `u`.`CountryId` > 0) > 0
""");
    }

    public override async Task Update_with_interface_in_property_expression(bool async)
    {
        await base.Update_with_interface_in_property_expression(async);

        AssertExecuteUpdateSql(
            """
@p='0'

UPDATE `Coke` AS `c`
SET `c`.`SugarGrams` = @p
""");
    }

    public override async Task Update_with_interface_in_EF_Property_in_property_expression(bool async)
    {
        await base.Update_with_interface_in_EF_Property_in_property_expression(async);

        AssertExecuteUpdateSql(
            """
@p='0'

UPDATE `Coke` AS `c`
SET `c`.`SugarGrams` = @p
""");
    }

    public override async Task Update_where_keyless_entity_mapped_to_sql_query(bool async)
    {
        await base.Update_where_keyless_entity_mapped_to_sql_query(async);

        AssertExecuteUpdateSql();
    }

    protected override void ClearLog()
        => Fixture.TestSqlLoggerFactory.Clear();

    private void AssertSql(params string[] expected)
        => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);

    private void AssertExecuteUpdateSql(params string[] expected)
        => Fixture.TestSqlLoggerFactory.AssertBaseline(expected, forUpdate: true);
}
