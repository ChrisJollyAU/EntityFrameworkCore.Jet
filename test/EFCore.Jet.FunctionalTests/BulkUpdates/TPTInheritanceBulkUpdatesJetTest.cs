﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.BulkUpdates;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace EntityFrameworkCore.Jet.FunctionalTests.BulkUpdates;

public class TPTInheritanceBulkUpdatesJetTest(
    TPTInheritanceBulkUpdatesJetFixture fixture,
    ITestOutputHelper testOutputHelper)
    : TPTInheritanceBulkUpdatesTestBase<TPTInheritanceBulkUpdatesJetFixture>(fixture, testOutputHelper)
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

        AssertSql();
    }

    public override async Task Delete_where_using_hierarchy(bool async)
    {
        await base.Delete_where_using_hierarchy(async);

        AssertSql();
    }

    public override async Task Delete_where_using_hierarchy_derived(bool async)
    {
        await base.Delete_where_using_hierarchy_derived(async);

        AssertSql();
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

        AssertExecuteUpdateSql(
            """
@p='Animal' (Size = 255)

UPDATE `Animals` AS `a`
SET `a`.`Name` = @p
WHERE `a`.`Name` = 'Great spotted kiwi'
""");
    }

    public override async Task Update_base_type_with_OfType(bool async)
    {
        await base.Update_base_type_with_OfType(async);

        AssertExecuteUpdateSql(
            """
@p='NewBird' (Size = 255)

UPDATE `Animals` AS `a`
LEFT JOIN `Kiwi` AS `k` ON `a`.`Id` = `k`.`Id`
SET `a`.`Name` = @p
WHERE `k`.`Id` IS NOT NULL
""");
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

UPDATE (`Animals` AS `a`
INNER JOIN `Birds` AS `b` ON `a`.`Id` = `b`.`Id`)
INNER JOIN `Kiwi` AS `k` ON `a`.`Id` = `k`.`Id`
SET `a`.`Name` = @p
""");
    }

    public override async Task Update_derived_property_on_derived_type(bool async)
    {
        await base.Update_derived_property_on_derived_type(async);

        AssertExecuteUpdateSql(
            """
@p='0' (Size = 1)

UPDATE (`Animals` AS `a`
INNER JOIN `Birds` AS `b` ON `a`.`Id` = `b`.`Id`)
INNER JOIN `Kiwi` AS `k` ON `a`.`Id` = `k`.`Id`
SET `k`.`FoundOn` = @p
""");
    }

    public override async Task Update_base_and_derived_types(bool async)
    {
        await base.Update_base_and_derived_types(async);

        AssertExecuteUpdateSql();
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
    FROM `Animals` AS `a`
    WHERE `c`.`Id` = `a`.`CountryId` AND `a`.`CountryId` > 0) > 0
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
    FROM `Animals` AS `a`
    LEFT JOIN `Kiwi` AS `k` ON `a`.`Id` = `k`.`Id`
    WHERE `c`.`Id` = `a`.`CountryId` AND `k`.`Id` IS NOT NULL AND `a`.`CountryId` > 0) > 0
""");
    }

    public override async Task Update_where_keyless_entity_mapped_to_sql_query(bool async)
    {
        await base.Update_where_keyless_entity_mapped_to_sql_query(async);

        AssertExecuteUpdateSql();
    }

    public override async Task Update_with_interface_in_property_expression(bool async)
    {
        await base.Update_with_interface_in_property_expression(async);

        AssertExecuteUpdateSql(
            """
@p='0'

UPDATE `Drinks` AS `d`
INNER JOIN `Coke` AS `c` ON `d`.`Id` = `c`.`Id`
SET `c`.`SugarGrams` = @p
""");
    }

    public override async Task Update_with_interface_in_EF_Property_in_property_expression(bool async)
    {
        await base.Update_with_interface_in_EF_Property_in_property_expression(async);

        AssertExecuteUpdateSql(
            """
@p='0'

UPDATE `Drinks` AS `d`
INNER JOIN `Coke` AS `c` ON `d`.`Id` = `c`.`Id`
SET `c`.`SugarGrams` = @p
""");
    }

    protected override void ClearLog()
        => Fixture.TestSqlLoggerFactory.Clear();

    private void AssertSql(params string[] expected)
        => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);

    private void AssertExecuteUpdateSql(params string[] expected)
        => Fixture.TestSqlLoggerFactory.AssertBaseline(expected, forUpdate: true);
}
