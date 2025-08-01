// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit.Abstractions;
using EntityFrameworkCore.Jet.FunctionalTests.TestUtilities;
using Microsoft.EntityFrameworkCore.TestModels.GearsOfWarModel;
using System.Linq;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace EntityFrameworkCore.Jet.FunctionalTests.Query
{
    public class GearsOfWarQueryJetTest : GearsOfWarQueryRelationalTestBase<GearsOfWarQueryJetFixture>
    {
        // ReSharper disable once UnusedParameter.Local
#pragma warning disable IDE0060 // Remove unused parameter
        public GearsOfWarQueryJetTest(GearsOfWarQueryJetFixture fixture, ITestOutputHelper testOutputHelper)
#pragma warning restore IDE0060 // Remove unused parameter
            : base(fixture)
        {
            Fixture.TestSqlLoggerFactory.Clear();
            Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
        }

        [Xunit.ConditionalFact]
        public virtual void Check_all_tests_overridden()
            => TestHelpers.AssertAllMethodsOverridden(GetType());

        public override async Task Entity_equality_empty(bool isAsync)
        {
            await base.Entity_equality_empty(isAsync);

            AssertSql(
                $"""
                    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
                    FROM `Gears` AS `g`
                    WHERE 0 = 1
                    """);
        }

        public override async Task Include_multiple_one_to_one_and_one_to_many(bool isAsync)
        {
            await base.Include_multiple_one_to_one_and_one_to_many(isAsync);

            AssertSql(
"""
SELECT `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`, `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM (`Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`)
LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`
ORDER BY `t`.`Id`, `g`.`Nickname`, `g`.`SquadId`
""");
        }

        public override async Task Include_multiple_one_to_one_optional_and_one_to_one_required(bool isAsync)
        {
            await base.Include_multiple_one_to_one_optional_and_one_to_one_required(isAsync);

            AssertSql(
"""
SELECT `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`, `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `s`.`Id`, `s`.`Banner`, `s`.`Banner5`, `s`.`InternalNumber`, `s`.`Name`
FROM (`Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`)
LEFT JOIN `Squads` AS `s` ON `g`.`SquadId` = `s`.`Id`
""");
        }

        public override async Task Include_multiple_circular(bool isAsync)
        {
            await base.Include_multiple_circular(isAsync);

            AssertSql(
"""
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `c`.`Name`, `c`.`Location`, `c`.`Nation`, `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
FROM (`Gears` AS `g`
INNER JOIN `Cities` AS `c` ON `g`.`CityOfBirthName` = `c`.`Name`)
LEFT JOIN `Gears` AS `g0` ON `c`.`Name` = `g0`.`AssignedCityName`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `c`.`Name`, `g0`.`Nickname`
""");
        }

        public override async Task Include_multiple_circular_with_filter(bool isAsync)
        {
            await base.Include_multiple_circular_with_filter(isAsync);

            AssertSql(
"""
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `c`.`Name`, `c`.`Location`, `c`.`Nation`, `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
FROM (`Gears` AS `g`
INNER JOIN `Cities` AS `c` ON `g`.`CityOfBirthName` = `c`.`Name`)
LEFT JOIN `Gears` AS `g0` ON `c`.`Name` = `g0`.`AssignedCityName`
WHERE `g`.`Nickname` = 'Marcus'
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `c`.`Name`, `g0`.`Nickname`
""");
        }

        public override async Task Include_using_alternate_key(bool isAsync)
        {
            await base.Include_using_alternate_key(isAsync);

            AssertSql(
"""
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM `Gears` AS `g`
LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`
WHERE `g`.`Nickname` = 'Marcus'
ORDER BY `g`.`Nickname`, `g`.`SquadId`
""");
        }

        public override async Task Include_navigation_on_derived_type(bool isAsync)
        {
            await base.Include_navigation_on_derived_type(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
FROM `Gears` AS `g`
LEFT JOIN `Gears` AS `g0` ON `g`.`Nickname` = `g0`.`LeaderNickname` AND `g`.`SquadId` = `g0`.`LeaderSquadId`
WHERE `g`.`Discriminator` = 'Officer'
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `g0`.`Nickname`
""");
        }

        public override async Task String_based_Include_navigation_on_derived_type(bool isAsync)
        {
            await base.String_based_Include_navigation_on_derived_type(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
FROM `Gears` AS `g`
LEFT JOIN `Gears` AS `g0` ON `g`.`Nickname` = `g0`.`LeaderNickname` AND `g`.`SquadId` = `g0`.`LeaderSquadId`
WHERE `g`.`Discriminator` = 'Officer'
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `g0`.`Nickname`
""");
        }

        public override async Task Select_Where_Navigation_Included(bool isAsync)
        {
            await base.Select_Where_Navigation_Included(isAsync);

            AssertSql(
"""
SELECT `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`, `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE `g`.`Nickname` = 'Marcus'
""");
        }

        public override async Task Include_with_join_reference1(bool isAsync)
        {
            await base.Include_with_join_reference1(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `c`.`Name`, `c`.`Location`, `c`.`Nation`
FROM (`Gears` AS `g`
INNER JOIN `Tags` AS `t` ON `g`.`SquadId` = `t`.`GearSquadId` AND `g`.`Nickname` = `t`.`GearNickName`)
INNER JOIN `Cities` AS `c` ON `g`.`CityOfBirthName` = `c`.`Name`
""");
        }

        public override async Task Include_with_join_reference2(bool isAsync)
        {
            await base.Include_with_join_reference2(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `c`.`Name`, `c`.`Location`, `c`.`Nation`
FROM (`Tags` AS `t`
INNER JOIN `Gears` AS `g` ON `t`.`GearSquadId` = `g`.`SquadId` AND `t`.`GearNickName` = `g`.`Nickname`)
LEFT JOIN `Cities` AS `c` ON `g`.`CityOfBirthName` = `c`.`Name`
WHERE `g`.`CityOfBirthName` IS NOT NULL AND `c`.`Name` IS NOT NULL
""");
        }

        public override async Task Include_with_join_collection1(bool isAsync)
        {
            await base.Include_with_join_collection1(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `t`.`Id`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM (`Gears` AS `g`
INNER JOIN `Tags` AS `t` ON `g`.`SquadId` = `t`.`GearSquadId` AND `g`.`Nickname` = `t`.`GearNickName`)
LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `t`.`Id`
""");
        }

        public override async Task Include_with_join_collection2(bool isAsync)
        {
            await base.Include_with_join_collection2(isAsync);

            AssertSql(
"""
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `t`.`Id`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM (`Tags` AS `t`
INNER JOIN `Gears` AS `g` ON `t`.`GearSquadId` = `g`.`SquadId` AND `t`.`GearNickName` = `g`.`Nickname`)
LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`
ORDER BY `t`.`Id`, `g`.`Nickname`, `g`.`SquadId`
""");
        }

        public override async Task Include_where_list_contains_navigation(bool isAsync)
        {
            await base.Include_where_list_contains_navigation(isAsync);

            AssertSql(
                """
SELECT `t`.`Id`
FROM `Tags` AS `t`
""",
                //
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`
FROM `Gears` AS `g`
LEFT JOIN `Tags` AS `t` ON `g`.`Nickname` = `t`.`GearNickName` AND `g`.`SquadId` = `t`.`GearSquadId`
WHERE `t`.`Id` IS NOT NULL AND `t`.`Id` IN ('{b39a6fba-9026-4d69-828e-fd7068673e57}', '{34c8d86e-a4ac-4be5-827f-584dda348a07}', '{70534e05-782c-4052-8720-c2c54481ce5f}', '{a8ad98f9-e023-4e2a-9a70-c2728455bd34}', '{df36f493-463f-4123-83f9-6b135deeb7ba}', '{a7be028a-0cf2-448f-ab55-ce8bc5d8cf69}')
""");
        }

        public override async Task Include_where_list_contains_navigation2(bool isAsync)
        {
            await base.Include_where_list_contains_navigation2(isAsync);

            AssertSql(
                """
SELECT `t`.`Id`
FROM `Tags` AS `t`
""",
                //
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`
FROM (`Gears` AS `g`
INNER JOIN `Cities` AS `c` ON `g`.`CityOfBirthName` = `c`.`Name`)
LEFT JOIN `Tags` AS `t` ON `g`.`Nickname` = `t`.`GearNickName` AND `g`.`SquadId` = `t`.`GearSquadId`
WHERE `c`.`Location` IS NOT NULL AND `t`.`Id` IN ('{b39a6fba-9026-4d69-828e-fd7068673e57}', '{34c8d86e-a4ac-4be5-827f-584dda348a07}', '{70534e05-782c-4052-8720-c2c54481ce5f}', '{a8ad98f9-e023-4e2a-9a70-c2728455bd34}', '{df36f493-463f-4123-83f9-6b135deeb7ba}', '{a7be028a-0cf2-448f-ab55-ce8bc5d8cf69}')
""");
        }

        public override async Task Navigation_accessed_twice_outside_and_inside_subquery(bool isAsync)
        {
            await base.Navigation_accessed_twice_outside_and_inside_subquery(isAsync);

            AssertSql(
                """
SELECT `t`.`Id`
FROM `Tags` AS `t`
""",
                //
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
LEFT JOIN `Tags` AS `t` ON `g`.`Nickname` = `t`.`GearNickName` AND `g`.`SquadId` = `t`.`GearSquadId`
WHERE `t`.`Id` IS NOT NULL AND `t`.`Id` IN ('{b39a6fba-9026-4d69-828e-fd7068673e57}', '{34c8d86e-a4ac-4be5-827f-584dda348a07}', '{70534e05-782c-4052-8720-c2c54481ce5f}', '{a8ad98f9-e023-4e2a-9a70-c2728455bd34}', '{df36f493-463f-4123-83f9-6b135deeb7ba}', '{a7be028a-0cf2-448f-ab55-ce8bc5d8cf69}')
""");
        }

        public override async Task Include_with_join_multi_level(bool isAsync)
        {
            await base.Include_with_join_multi_level(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `c`.`Name`, `c`.`Location`, `c`.`Nation`, `t`.`Id`, `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
FROM ((`Gears` AS `g`
INNER JOIN `Tags` AS `t` ON `g`.`SquadId` = `t`.`GearSquadId` AND `g`.`Nickname` = `t`.`GearNickName`)
INNER JOIN `Cities` AS `c` ON `g`.`CityOfBirthName` = `c`.`Name`)
LEFT JOIN `Gears` AS `g0` ON `c`.`Name` = `g0`.`AssignedCityName`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `t`.`Id`, `c`.`Name`, `g0`.`Nickname`
""");
        }

        public override async Task Include_with_join_and_inheritance1(bool isAsync)
        {
            await base.Include_with_join_and_inheritance1(isAsync);

            AssertSql(
                """
SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`, `c`.`Name`, `c`.`Location`, `c`.`Nation`
FROM (`Tags` AS `t`
INNER JOIN (
    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
    FROM `Gears` AS `g`
    WHERE `g`.`Discriminator` = 'Officer'
) AS `g0` ON `t`.`GearSquadId` = `g0`.`SquadId` AND `t`.`GearNickName` = `g0`.`Nickname`)
LEFT JOIN `Cities` AS `c` ON `g0`.`CityOfBirthName` = `c`.`Name`
WHERE `g0`.`CityOfBirthName` IS NOT NULL AND `c`.`Name` IS NOT NULL
""");
        }

        public override async Task Include_with_join_and_inheritance_with_orderby_before_and_after_include(bool isAsync)
        {
            await base.Include_with_join_and_inheritance_with_orderby_before_and_after_include(isAsync);

            AssertSql(
                """
SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`, `t`.`Id`, `g1`.`Nickname`, `g1`.`SquadId`, `g1`.`AssignedCityName`, `g1`.`CityOfBirthName`, `g1`.`Discriminator`, `g1`.`FullName`, `g1`.`HasSoulPatch`, `g1`.`LeaderNickname`, `g1`.`LeaderSquadId`, `g1`.`Rank`
FROM (`Tags` AS `t`
INNER JOIN (
    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
    FROM `Gears` AS `g`
    WHERE `g`.`Discriminator` = 'Officer'
) AS `g0` ON `t`.`GearSquadId` = `g0`.`SquadId` AND `t`.`GearNickName` = `g0`.`Nickname`)
LEFT JOIN `Gears` AS `g1` ON `g0`.`Nickname` = `g1`.`LeaderNickname` AND `g0`.`SquadId` = `g1`.`LeaderSquadId`
ORDER BY NOT (`g0`.`HasSoulPatch`), `g0`.`Nickname` DESC, `t`.`Id`, `g0`.`SquadId`, `g1`.`Nickname`
""");
        }

        public override async Task Include_with_join_and_inheritance2(bool isAsync)
        {
            await base.Include_with_join_and_inheritance2(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `t`.`Id`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM (`Gears` AS `g`
INNER JOIN `Tags` AS `t` ON `g`.`SquadId` = `t`.`GearSquadId` AND `g`.`Nickname` = `t`.`GearNickName`)
LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`
WHERE `g`.`Discriminator` = 'Officer'
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `t`.`Id`
""");
        }

        public override async Task Include_with_join_and_inheritance3(bool isAsync)
        {
            await base.Include_with_join_and_inheritance3(isAsync);

            AssertSql(
                """
SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`, `t`.`Id`, `g1`.`Nickname`, `g1`.`SquadId`, `g1`.`AssignedCityName`, `g1`.`CityOfBirthName`, `g1`.`Discriminator`, `g1`.`FullName`, `g1`.`HasSoulPatch`, `g1`.`LeaderNickname`, `g1`.`LeaderSquadId`, `g1`.`Rank`
FROM (`Tags` AS `t`
INNER JOIN (
    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
    FROM `Gears` AS `g`
    WHERE `g`.`Discriminator` = 'Officer'
) AS `g0` ON `t`.`GearSquadId` = `g0`.`SquadId` AND `t`.`GearNickName` = `g0`.`Nickname`)
LEFT JOIN `Gears` AS `g1` ON `g0`.`Nickname` = `g1`.`LeaderNickname` AND `g0`.`SquadId` = `g1`.`LeaderSquadId`
ORDER BY `t`.`Id`, `g0`.`Nickname`, `g0`.`SquadId`, `g1`.`Nickname`
""");
        }

        public override async Task Include_with_nested_navigation_in_order_by(bool isAsync)
        {
            await base.Include_with_nested_navigation_in_order_by(isAsync);

            AssertSql(
"""
SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`, `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM (`Weapons` AS `w`
LEFT JOIN `Gears` AS `g` ON `w`.`OwnerFullName` = `g`.`FullName`)
LEFT JOIN `Cities` AS `c` ON `g`.`CityOfBirthName` = `c`.`Name`
WHERE `g`.`Nickname` <> 'Paduk' OR `g`.`Nickname` IS NULL
ORDER BY `c`.`Name`, `w`.`Id`
""");
        }

        public override async Task Where_count_subquery_without_collision(bool isAsync)
        {
            await base.Where_count_subquery_without_collision(isAsync);

            AssertSql(
"""
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE (
    SELECT COUNT(*)
    FROM `Weapons` AS `w`
    WHERE `g`.`FullName` = `w`.`OwnerFullName`) = 2
""");
        }

        public override async Task Where_any_subquery_without_collision(bool isAsync)
        {
            await base.Where_any_subquery_without_collision(isAsync);

            AssertSql(
"""
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE EXISTS (
    SELECT 1
    FROM `Weapons` AS `w`
    WHERE `g`.`FullName` = `w`.`OwnerFullName`)
""");
        }

        public override async Task Select_inverted_boolean(bool isAsync)
        {
            await base.Select_inverted_boolean(isAsync);

            AssertSql(
                """
SELECT `w`.`Id`, `w`.`IsAutomatic` BXOR TRUE AS `Manual`
FROM `Weapons` AS `w`
WHERE `w`.`IsAutomatic` = TRUE
""");
        }

        public override async Task Select_inverted_nullable_boolean(bool async)
        {
            await base.Select_inverted_nullable_boolean(async);

            AssertSql(
                """
SELECT `f`.`Id`, `f`.`Eradicated` BXOR TRUE AS `Alive`
FROM `Factions` AS `f`
""");
        }

        public override async Task Select_comparison_with_null(bool isAsync)
        {
            await base.Select_comparison_with_null(isAsync);

            AssertSql(
                """
@ammunitionType='1' (Nullable = true)
@ammunitionType='1' (Nullable = true)

SELECT `w`.`Id`, IIF(`w`.`AmmunitionType` = @ammunitionType AND `w`.`AmmunitionType` IS NOT NULL, TRUE, FALSE) AS `Cartridge`
FROM `Weapons` AS `w`
WHERE `w`.`AmmunitionType` = @ammunitionType
""",
                //
                """
SELECT `w`.`Id`, IIF(`w`.`AmmunitionType` IS NULL, TRUE, FALSE) AS `Cartridge`
FROM `Weapons` AS `w`
WHERE `w`.`AmmunitionType` IS NULL
""");
        }

        public override async Task Select_null_parameter(bool isAsync)
        {
            await base.Select_null_parameter(isAsync);

            AssertSql(
                """
@ammunitionType='1' (Nullable = true)

SELECT `w`.`Id`, @ammunitionType AS `AmmoType`
FROM `Weapons` AS `w`
""",
                //
                """
SELECT `w`.`Id`, CVar(NULL) AS `AmmoType`
FROM `Weapons` AS `w`
""",
                //
                """
@ammunitionType='2' (Nullable = true)

SELECT `w`.`Id`, @ammunitionType AS `AmmoType`
FROM `Weapons` AS `w`
""",
                //
                """
SELECT `w`.`Id`, CVar(NULL) AS `AmmoType`
FROM `Weapons` AS `w`
""");
        }

        public override async Task Select_ternary_operation_with_boolean(bool isAsync)
        {
            await base.Select_ternary_operation_with_boolean(isAsync);

            AssertSql(
"""
SELECT `w`.`Id`, IIF(`w`.`IsAutomatic` = TRUE, 1, 0) AS `Num`
FROM `Weapons` AS `w`
""");
        }

        public override async Task Select_ternary_operation_with_inverted_boolean(bool isAsync)
        {
            await base.Select_ternary_operation_with_inverted_boolean(isAsync);
            AssertSql(
                """
SELECT `w`.`Id`, IIF(`w`.`IsAutomatic` = FALSE, 1, 0) AS `Num`
FROM `Weapons` AS `w`
""");
        }

        public override async Task Select_ternary_operation_with_has_value_not_null(bool isAsync)
        {
            await base.Select_ternary_operation_with_has_value_not_null(isAsync);
            AssertSql(
"""
SELECT `w`.`Id`, IIF(`w`.`AmmunitionType` IS NOT NULL AND `w`.`AmmunitionType` = 1, 'Yes', 'No') AS `IsCartridge`
FROM `Weapons` AS `w`
WHERE `w`.`AmmunitionType` IS NOT NULL AND `w`.`AmmunitionType` = 1
""");
        }

        public override async Task Select_ternary_operation_multiple_conditions(bool isAsync)
        {
            await base.Select_ternary_operation_multiple_conditions(isAsync);

            AssertSql(
"""
SELECT `w`.`Id`, IIF(`w`.`AmmunitionType` = 2 AND `w`.`SynergyWithId` = 1, 'Yes', 'No') AS `IsCartridge`
FROM `Weapons` AS `w`
""");
        }

        public override async Task Select_ternary_operation_multiple_conditions_2(bool isAsync)
        {
            await base.Select_ternary_operation_multiple_conditions_2(isAsync);
            AssertSql(
                """
SELECT `w`.`Id`, IIF(`w`.`IsAutomatic` = FALSE AND `w`.`SynergyWithId` = 1, 'Yes', 'No') AS `IsCartridge`
FROM `Weapons` AS `w`
""");
        }

        public override async Task Select_multiple_conditions(bool isAsync)
        {
            await base.Select_multiple_conditions(isAsync);

            AssertSql(
                """
SELECT `w`.`Id`, IIF(`w`.`IsAutomatic` = FALSE AND `w`.`SynergyWithId` = 1 AND `w`.`SynergyWithId` IS NOT NULL, TRUE, FALSE) AS `IsCartridge`
FROM `Weapons` AS `w`
""");
        }

        public override async Task Select_nested_ternary_operations(bool isAsync)
        {
            await base.Select_nested_ternary_operations(isAsync);

            AssertSql(
                """
SELECT `w`.`Id`, IIF(`w`.`IsAutomatic` = FALSE, IIF(`w`.`AmmunitionType` = 1, 'ManualCartridge', 'Manual'), 'Auto') AS `IsManualCartridge`
FROM `Weapons` AS `w`
""");
        }

        public override async Task Null_propagation_optimization1(bool isAsync)
        {
            await base.Null_propagation_optimization1(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE `g`.`LeaderNickname` = 'Marcus'
""");
        }

        public override async Task Null_propagation_optimization2(bool isAsync)
        {
            await base.Null_propagation_optimization2(isAsync);

            // issue #16050
            //            AssertSql(
            //                $@"SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
            //FROM `Gears` AS `g`
            //WHERE `g`.`Discriminator` IN ('Officer', 'Gear') AND `g`.`LeaderNickname` LIKE '%us'");
        }

        public override async Task Null_propagation_optimization3(bool isAsync)
        {
            await base.Null_propagation_optimization3(isAsync);

            // issue #16050
            //            AssertSql(
            //                $@"SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
            //FROM `Gears` AS `g`
            //WHERE `g`.`Discriminator` IN ('Officer', 'Gear') AND `g`.`LeaderNickname` LIKE '%us'");
        }

        public override async Task Null_propagation_optimization4(bool isAsync)
        {
            await base.Null_propagation_optimization4(isAsync);

            // issue #16050
            //            AssertSql(
            //                $@"SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
            //FROM `Gears` AS `g`
            //WHERE `g`.`Discriminator` IN ('Officer', 'Gear') AND (CAST(LEN(`g`.`LeaderNickname`) AS int) = 5)");
        }

        public override async Task Null_propagation_optimization5(bool isAsync)
        {
            await base.Null_propagation_optimization5(isAsync);

            // issue #16050
            //            AssertSql(
            //                $@"SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
            //FROM `Gears` AS `g`
            //WHERE `g`.`Discriminator` IN ('Officer', 'Gear') AND (CAST(LEN(`g`.`LeaderNickname`) AS int) = 5)");
        }

        public override async Task Null_propagation_optimization6(bool isAsync)
        {
            await base.Null_propagation_optimization6(isAsync);

            // issue #16050
            //            AssertSql(
            //                $@"SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
            //FROM `Gears` AS `g`
            //WHERE `g`.`Discriminator` IN ('Officer', 'Gear') AND (CAST(LEN(`g`.`LeaderNickname`) AS int) = 5)");
        }

        public override async Task Select_null_propagation_optimization7(bool isAsync)
        {
            await base.Select_null_propagation_optimization7(isAsync);

            // issue #16050
            //            AssertSql(
            //                $@"SELECT `g`.`LeaderNickname` + `g`.`LeaderNickname`
            //FROM `Gears` AS `g`
            //WHERE `g`.`Discriminator` IN ('Officer', 'Gear')");
        }

        public override async Task Select_null_propagation_optimization8(bool isAsync)
        {
            await base.Select_null_propagation_optimization8(isAsync);

            AssertSql(
"""
SELECT IIF(`g`.`LeaderNickname` IS NULL, '', `g`.`LeaderNickname`) & IIF(`g`.`LeaderNickname` IS NULL, '', `g`.`LeaderNickname`)
FROM `Gears` AS `g`
""");
        }

        public override async Task Select_null_propagation_optimization9(bool isAsync)
        {
            await base.Select_null_propagation_optimization9(isAsync);
            AssertSql(
                """
    SELECT IIF(LEN(`g`.`FullName`) IS NULL, NULL, CLNG(LEN(`g`.`FullName`)))
    FROM `Gears` AS `g`
    """);
        }

        public override async Task Select_null_propagation_negative1(bool isAsync)
        {
            await base.Select_null_propagation_negative1(isAsync);
            AssertSql(
                """
SELECT IIF(`g`.`LeaderNickname` IS NOT NULL, CBOOL(IIF(LEN(`g`.`Nickname`) IS NULL, NULL, CLNG(LEN(`g`.`Nickname`))) BXOR 5) BXOR TRUE, NULL)
FROM `Gears` AS `g`
""");
        }

        public override async Task Select_null_propagation_negative2(bool isAsync)
        {
            await base.Select_null_propagation_negative2(isAsync);

            AssertSql(
"""
SELECT IIF(`g`.`LeaderNickname` IS NOT NULL, `g0`.`LeaderNickname`, NULL)
FROM `Gears` AS `g`,
`Gears` AS `g0`
""");
        }

        public override async Task Select_null_propagation_negative3(bool isAsync)
        {
            await base.Select_null_propagation_negative3(isAsync);

            AssertSql(
                $"""
                    SELECT `t`.`Nickname`, CASE
                        WHEN `t`.`Nickname` IS NOT NULL THEN IIF(`t`.`LeaderNickname` IS NOT NULL, 1, 0)
                        ELSE NULL
                    END AS `Condition`
                    FROM `Gears` AS `g`
                    LEFT JOIN (
                        SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
                        FROM `Gears` AS `g0`
                        WHERE `g0`.`Discriminator` IN ('Gear', 'Officer')
                    ) AS `t` ON `g`.`HasSoulPatch` = True
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer')
                    ORDER BY `t`.`Nickname`
                    """);
        }

        public override async Task Select_null_propagation_negative4(bool isAsync)
        {
            await base.Select_null_propagation_negative4(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(`t`.`Nickname` IS NOT NULL, 1, 0), `t`.`Nickname`
                    FROM `Gears` AS `g`
                    LEFT JOIN (
                        SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
                        FROM `Gears` AS `g0`
                        WHERE `g0`.`Discriminator` IN ('Gear', 'Officer')
                    ) AS `t` ON `g`.`HasSoulPatch` = True
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer')
                    ORDER BY `t`.`Nickname`
                    """);
        }

        public override async Task Select_null_propagation_negative5(bool isAsync)
        {
            await base.Select_null_propagation_negative5(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(`t`.`Nickname` IS NOT NULL, 1, 0), `t`.`Nickname`
                    FROM `Gears` AS `g`
                    LEFT JOIN (
                        SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
                        FROM `Gears` AS `g0`
                        WHERE `g0`.`Discriminator` IN ('Gear', 'Officer')
                    ) AS `t` ON `g`.`HasSoulPatch` = True
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer')
                    ORDER BY `t`.`Nickname`
                    """);
        }

        public override async Task Select_null_propagation_negative6(bool isAsync)
        {
            await base.Select_null_propagation_negative6(isAsync);

            AssertSql(
                """
SELECT IIF(`g`.`LeaderNickname` IS NOT NULL, FALSE, NULL)
FROM `Gears` AS `g`
""");
        }

        public override async Task Select_null_propagation_negative7(bool isAsync)
        {
            await base.Select_null_propagation_negative7(isAsync);

            AssertSql(
"""
SELECT IIF(`g`.`LeaderNickname` IS NOT NULL, TRUE, NULL)
FROM `Gears` AS `g`
""");
        }

        public override async Task Select_null_propagation_negative8(bool isAsync)
        {
            await base.Select_null_propagation_negative8(isAsync);

            AssertSql(
"""
SELECT IIF(`s`.`Id` IS NOT NULL, `c`.`Name`, NULL)
FROM ((`Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`)
LEFT JOIN `Squads` AS `s` ON `g`.`SquadId` = `s`.`Id`)
LEFT JOIN `Cities` AS `c` ON `g`.`AssignedCityName` = `c`.`Name`
""");
        }

        public override async Task Select_null_propagation_negative9(bool async)
        {
            await base.Select_null_propagation_negative9(async);

            AssertSql(
                """
SELECT IIF(`g`.`LeaderNickname` IS NOT NULL, CBOOL(IIF(LEN(`g`.`Nickname`) IS NULL, NULL, CLNG(LEN(`g`.`Nickname`))) BXOR 5) BXOR TRUE, NULL)
FROM `Gears` AS `g`
""");
        }

        public override async Task Select_null_propagation_works_for_navigations_with_composite_keys(bool isAsync)
        {
            await base.Select_null_propagation_works_for_navigations_with_composite_keys(isAsync);

            AssertSql(
"""
SELECT `g`.`Nickname`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
""");
        }

        public override async Task Select_null_propagation_works_for_multiple_navigations_with_composite_keys(bool isAsync)
        {
            await base.Select_null_propagation_works_for_multiple_navigations_with_composite_keys(isAsync);

            AssertSql(
"""
SELECT IIF(`c`.`Name` IS NOT NULL, `c`.`Name`, NULL)
FROM (((`Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`)
LEFT JOIN `Tags` AS `t0` ON (`g`.`Nickname` = `t0`.`GearNickName` OR (`g`.`Nickname` IS NULL AND `t0`.`GearNickName` IS NULL)) AND (`g`.`SquadId` = `t0`.`GearSquadId` OR (`g`.`SquadId` IS NULL AND `t0`.`GearSquadId` IS NULL)))
LEFT JOIN `Gears` AS `g0` ON `t0`.`GearNickName` = `g0`.`Nickname` AND `t0`.`GearSquadId` = `g0`.`SquadId`)
LEFT JOIN `Cities` AS `c` ON `g0`.`AssignedCityName` = `c`.`Name`
""");
        }

        public override async Task Select_conditional_with_anonymous_type_and_null_constant(bool isAsync)
        {
            await base.Select_conditional_with_anonymous_type_and_null_constant(isAsync);

            AssertSql(
"""
SELECT IIF(`g`.`LeaderNickname` IS NOT NULL, TRUE, FALSE), `g`.`HasSoulPatch`
FROM `Gears` AS `g`
ORDER BY `g`.`Nickname`
""");
        }

        public override async Task Select_conditional_with_anonymous_types(bool isAsync)
        {
            await base.Select_conditional_with_anonymous_types(isAsync);

            AssertSql(
"""
SELECT IIF(`g`.`LeaderNickname` IS NOT NULL, TRUE, FALSE), `g`.`Nickname`, `g`.`FullName`
FROM `Gears` AS `g`
ORDER BY `g`.`Nickname`
""");
        }

        public override async Task Where_conditional_equality_1(bool async)
        {
            await base.Where_conditional_equality_1(async);

            AssertSql(
"""
SELECT `g`.`Nickname`
FROM `Gears` AS `g`
WHERE `g`.`LeaderNickname` IS NULL
ORDER BY `g`.`Nickname`
""");
        }

        public override async Task Where_conditional_equality_2(bool async)
        {
            await base.Where_conditional_equality_2(async);

            AssertSql(
"""
SELECT `g`.`Nickname`
FROM `Gears` AS `g`
WHERE `g`.`LeaderNickname` IS NULL
ORDER BY `g`.`Nickname`
""");
        }

        public override async Task Where_conditional_equality_3(bool async)
        {
            await base.Where_conditional_equality_3(async);

            AssertSql(
"""
SELECT `g`.`Nickname`
FROM `Gears` AS `g`
ORDER BY `g`.`Nickname`
""");
        }

        public override async Task Select_coalesce_with_anonymous_types(bool isAsync)
        {
            await base.Select_coalesce_with_anonymous_types(isAsync);

            AssertSql(
"""
SELECT `g`.`LeaderNickname`, `g`.`FullName`
FROM `Gears` AS `g`
ORDER BY `g`.`Nickname`
""");
        }

        public override async Task Where_compare_anonymous_types(bool isAsync)
        {
            await base.Where_compare_anonymous_types(isAsync);

            AssertSql();
        }

        public override async Task Where_member_access_on_anonymous_type(bool isAsync)
        {
            await base.Where_member_access_on_anonymous_type(isAsync);

            AssertSql(
"""
SELECT `g`.`Nickname`
FROM `Gears` AS `g`
WHERE `g`.`LeaderNickname` = 'Marcus'
""");
        }

        public override async Task Where_compare_anonymous_types_with_uncorrelated_members(bool isAsync)
        {
            await base.Where_compare_anonymous_types_with_uncorrelated_members(isAsync);

            AssertSql(
"""
SELECT `g`.`Nickname`
FROM `Gears` AS `g`
WHERE 0 = 1
""");
        }

        public override async Task Select_Where_Navigation_Scalar_Equals_Navigation_Scalar(bool isAsync)
        {
            await base.Select_Where_Navigation_Scalar_Equals_Navigation_Scalar(isAsync);

            AssertSql(
                """
SELECT `s`.`Id`, `s`.`GearNickName`, `s`.`GearSquadId`, `s`.`IssueDate`, `s`.`Note`, `s`.`Id0`, `s`.`GearNickName0`, `s`.`GearSquadId0`, `s`.`IssueDate0`, `s`.`Note0`
FROM ((
    SELECT `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`, `t0`.`Id` AS `Id0`, `t0`.`GearNickName` AS `GearNickName0`, `t0`.`GearSquadId` AS `GearSquadId0`, `t0`.`IssueDate` AS `IssueDate0`, `t0`.`Note` AS `Note0`
    FROM `Tags` AS `t`,
    `Tags` AS `t0`
) AS `s`
LEFT JOIN `Gears` AS `g` ON `s`.`GearNickName` = `g`.`Nickname` AND `s`.`GearSquadId` = `g`.`SquadId`)
LEFT JOIN `Gears` AS `g0` ON `s`.`GearNickName0` = `g0`.`Nickname` AND `s`.`GearSquadId0` = `g0`.`SquadId`
WHERE `g`.`Nickname` = `g0`.`Nickname` OR (`g`.`Nickname` IS NULL AND `g0`.`Nickname` IS NULL)
""");
        }

        public override async Task Conditional_Navigation_With_Trivial_Member_Access(bool async)
        {
            await base.Conditional_Navigation_With_Trivial_Member_Access(async);

            AssertSql(
                """
SELECT `g`.`Nickname`
FROM (`Gears` AS `g`
LEFT JOIN `Cities` AS `c` ON `g`.`AssignedCityName` = `c`.`Name`)
INNER JOIN `Cities` AS `c0` ON `g`.`CityOfBirthName` = `c0`.`Name`
WHERE IIF(`c`.`Name` IS NOT NULL, `c`.`Name`, `c0`.`Name`) <> 'Ephyra'
""");
        }

        public override async Task Conditional_Navigation_With_Member_Access_On_Same_Type(bool async)
        {
            await base.Conditional_Navigation_With_Member_Access_On_Same_Type(async);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`FullName`
FROM (`Gears` AS `g`
LEFT JOIN `Cities` AS `c` ON `g`.`AssignedCityName` = `c`.`Name`)
INNER JOIN `Cities` AS `c0` ON `g`.`CityOfBirthName` = `c0`.`Name`
WHERE IIF(`c`.`Name` IS NOT NULL, `c`.`Nation`, `c0`.`Nation`) = 'Tyrus'
""");
        }

        public override async Task Conditional_Navigation_With_Member_Access_On_Related_Types(bool async)
        {
            await base.Conditional_Navigation_With_Member_Access_On_Related_Types(async);

            AssertSql(
                """
SELECT `f`.`Name`
FROM (`Factions` AS `f`
LEFT JOIN `LocustLeaders` AS `l` ON `f`.`DeputyCommanderName` = `l`.`Name`)
LEFT JOIN (
    SELECT `l0`.`Name`, `l0`.`ThreatLevel`
    FROM `LocustLeaders` AS `l0`
    WHERE `l0`.`Discriminator` = 'LocustCommander'
) AS `l1` ON `f`.`CommanderName` = `l1`.`Name`
WHERE IIF(`l`.`Name` IS NOT NULL, `l`.`ThreatLevel`, `l1`.`ThreatLevel`) = 4
""");
        }

        public override async Task Select_Singleton_Navigation_With_Member_Access(bool isAsync)
        {
            await base.Select_Singleton_Navigation_With_Member_Access(isAsync);

            AssertSql(
"""
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE `g`.`Nickname` = 'Marcus' AND (`g`.`CityOfBirthName` <> 'Ephyra' OR `g`.`CityOfBirthName` IS NULL)
""");
        }

        public override async Task Select_Where_Navigation(bool isAsync)
        {
            await base.Select_Where_Navigation(isAsync);

            AssertSql(
"""
SELECT `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE `g`.`Nickname` = 'Marcus'
""");
        }

        public override async Task Select_Where_Navigation_Equals_Navigation(bool isAsync)
        {
            await base.Select_Where_Navigation_Equals_Navigation(isAsync);

            AssertSql(
                """
SELECT `s`.`Id`, `s`.`GearNickName`, `s`.`GearSquadId`, `s`.`IssueDate`, `s`.`Note`, `s`.`Id0`, `s`.`GearNickName0`, `s`.`GearSquadId0`, `s`.`IssueDate0`, `s`.`Note0`
FROM ((
    SELECT `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`, `t0`.`Id` AS `Id0`, `t0`.`GearNickName` AS `GearNickName0`, `t0`.`GearSquadId` AS `GearSquadId0`, `t0`.`IssueDate` AS `IssueDate0`, `t0`.`Note` AS `Note0`
    FROM `Tags` AS `t`,
    `Tags` AS `t0`
) AS `s`
LEFT JOIN `Gears` AS `g` ON `s`.`GearNickName` = `g`.`Nickname` AND `s`.`GearSquadId` = `g`.`SquadId`)
LEFT JOIN `Gears` AS `g0` ON `s`.`GearNickName0` = `g0`.`Nickname` AND `s`.`GearSquadId0` = `g0`.`SquadId`
WHERE (`g`.`Nickname` = `g0`.`Nickname` OR (`g`.`Nickname` IS NULL AND `g0`.`Nickname` IS NULL)) AND (`g`.`SquadId` = `g0`.`SquadId` OR (`g`.`SquadId` IS NULL AND `g0`.`SquadId` IS NULL))
""");
        }

        public override async Task Select_Where_Navigation_Null(bool isAsync)
        {
            await base.Select_Where_Navigation_Null(isAsync);

            AssertSql(
"""
SELECT `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE `g`.`Nickname` IS NULL OR `g`.`SquadId` IS NULL
""");
        }

        public override async Task Select_Where_Navigation_Null_Reverse(bool isAsync)
        {
            await base.Select_Where_Navigation_Null_Reverse(isAsync);

            AssertSql(
"""
SELECT `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE `g`.`Nickname` IS NULL OR `g`.`SquadId` IS NULL
""");
        }

        public override async Task Select_Where_Navigation_Scalar_Equals_Navigation_Scalar_Projected(bool isAsync)
        {
            await base.Select_Where_Navigation_Scalar_Equals_Navigation_Scalar_Projected(isAsync);

            AssertSql(
                """
SELECT `s`.`Id` AS `Id1`, `s`.`Id0` AS `Id2`
FROM ((
    SELECT `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t0`.`Id` AS `Id0`, `t0`.`GearNickName` AS `GearNickName0`, `t0`.`GearSquadId` AS `GearSquadId0`
    FROM `Tags` AS `t`,
    `Tags` AS `t0`
) AS `s`
LEFT JOIN `Gears` AS `g` ON `s`.`GearNickName` = `g`.`Nickname` AND `s`.`GearSquadId` = `g`.`SquadId`)
LEFT JOIN `Gears` AS `g0` ON `s`.`GearNickName0` = `g0`.`Nickname` AND `s`.`GearSquadId0` = `g0`.`SquadId`
WHERE `g`.`Nickname` = `g0`.`Nickname` OR (`g`.`Nickname` IS NULL AND `g0`.`Nickname` IS NULL)
""");
        }

        public override async Task Optional_Navigation_Null_Coalesce_To_Clr_Type(bool isAsync)
        {
            await base.Optional_Navigation_Null_Coalesce_To_Clr_Type(isAsync);

            AssertSql(
"""
SELECT TOP 1 IIF(`w0`.`IsAutomatic` IS NULL, FALSE, `w0`.`IsAutomatic`) AS `IsAutomatic`
FROM `Weapons` AS `w`
LEFT JOIN `Weapons` AS `w0` ON `w`.`SynergyWithId` = `w0`.`Id`
ORDER BY `w`.`Id`
""");
        }

        public override async Task Where_subquery_boolean(bool isAsync)
        {
            await base.Where_subquery_boolean(isAsync);

            AssertSql(
"""
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE IIF((
        SELECT TOP 1 `w`.`IsAutomatic`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName`
        ORDER BY `w`.`Id`) IS NULL, FALSE, (
        SELECT TOP 1 `w`.`IsAutomatic`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName`
        ORDER BY `w`.`Id`)) = TRUE
""");
        }

        public override async Task Where_subquery_boolean_with_pushdown(bool isAsync)
        {
            await base.Where_subquery_boolean_with_pushdown(isAsync);

            AssertSql(
"""
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE (
    SELECT TOP 1 `w`.`IsAutomatic`
    FROM `Weapons` AS `w`
    WHERE `g`.`FullName` = `w`.`OwnerFullName`
    ORDER BY `w`.`Id`) = TRUE
""");
        }

        public override async Task Where_subquery_distinct_firstordefault_boolean(bool isAsync)
        {
            await base.Where_subquery_distinct_firstordefault_boolean(isAsync);

            AssertSql(
                $"""
                    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
                    FROM `Gears` AS `g`
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer') AND ((`g`.`HasSoulPatch` = True) AND ((
                        SELECT TOP 1 `t`.`IsAutomatic`
                        FROM (
                            SELECT DISTINCT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
                            FROM `Weapons` AS `w`
                            WHERE `g`.`FullName` = `w`.`OwnerFullName`
                        ) AS `t`
                        ORDER BY `t`.`Id`) = True))
                    """);
        }

        public override async Task Where_subquery_distinct_firstordefault_boolean_with_pushdown(bool isAsync)
        {
            await base.Where_subquery_distinct_firstordefault_boolean_with_pushdown(isAsync);

            AssertSql(
                $"""
                    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
                    FROM `Gears` AS `g`
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer') AND ((`g`.`HasSoulPatch` = True) AND ((
                        SELECT TOP 1 `t`.`IsAutomatic`
                        FROM (
                            SELECT DISTINCT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
                            FROM `Weapons` AS `w`
                            WHERE `g`.`FullName` = `w`.`OwnerFullName`
                        ) AS `t`
                        ORDER BY `t`.`Id`) = True))
                    """);
        }

        public override async Task Where_subquery_distinct_first_boolean(bool isAsync)
        {
            await base.Where_subquery_distinct_first_boolean(isAsync);

            AssertSql(
                $"""
                    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
                    FROM `Gears` AS `g`
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer') AND ((`g`.`HasSoulPatch` = True) AND ((
                        SELECT TOP 1 `t`.`IsAutomatic`
                        FROM (
                            SELECT DISTINCT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
                            FROM `Weapons` AS `w`
                            WHERE `g`.`FullName` = `w`.`OwnerFullName`
                        ) AS `t`
                        ORDER BY `t`.`Id`) = True))
                    ORDER BY `g`.`Nickname`
                    """);
        }

        public override async Task Where_subquery_distinct_singleordefault_boolean1(bool isAsync)
        {
            await base.Where_subquery_distinct_singleordefault_boolean1(isAsync);

            AssertSql(
"""
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE `g`.`HasSoulPatch` = TRUE AND IIF((
        SELECT DISTINCT TOP 1 `w`.`IsAutomatic`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName` AND (`w`.`Name` LIKE '%Lancer%')) IS NULL, FALSE, (
        SELECT DISTINCT TOP 1 `w`.`IsAutomatic`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName` AND (`w`.`Name` LIKE '%Lancer%'))) = TRUE
ORDER BY `g`.`Nickname`
""");
        }

        public override async Task Where_subquery_distinct_singleordefault_boolean2(bool isAsync)
        {
            await base.Where_subquery_distinct_singleordefault_boolean2(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE `g`.`HasSoulPatch` = TRUE AND IIF((
        SELECT TOP 1 `w`.`IsAutomatic`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName` AND (`w`.`Name` LIKE '%Lancer%')) IS NULL, FALSE, (
        SELECT TOP 1 `w`.`IsAutomatic`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName` AND (`w`.`Name` LIKE '%Lancer%'))) = TRUE
ORDER BY `g`.`Nickname`
""");
        }

        public override async Task Where_subquery_distinct_singleordefault_boolean_with_pushdown(bool isAsync)
        {
            await base.Where_subquery_distinct_singleordefault_boolean_with_pushdown(isAsync);

            AssertSql(
                $"""
                    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
                    FROM `Gears` AS `g`
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer') AND ((`g`.`HasSoulPatch` = True) AND ((
                        SELECT TOP 1 `t`.`IsAutomatic`
                        FROM (
                            SELECT DISTINCT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
                            FROM `Weapons` AS `w`
                            WHERE (`g`.`FullName` = `w`.`OwnerFullName`) AND (CHARINDEX('Lancer', `w`.`Name`) > 0)
                        ) AS `t`) = True))
                    ORDER BY `g`.`Nickname`
                    """);
        }

        public override async Task Where_subquery_distinct_lastordefault_boolean(bool isAsync)
        {
            await base.Where_subquery_distinct_lastordefault_boolean(isAsync);

            AssertSql(
                $"""
                    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
                    FROM `Gears` AS `g`
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer') AND ((
                        SELECT TOP 1 `t`.`IsAutomatic`
                        FROM (
                            SELECT DISTINCT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
                            FROM `Weapons` AS `w`
                            WHERE `g`.`FullName` = `w`.`OwnerFullName`
                        ) AS `t`
                        ORDER BY `t`.`Id` DESC) <> True)
                    ORDER BY `g`.`Nickname`
                    """);
        }

        public override async Task Where_subquery_distinct_last_boolean(bool isAsync)
        {
            await base.Where_subquery_distinct_last_boolean(isAsync);

            AssertSql(
                $"""
                    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
                    FROM `Gears` AS `g`
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer') AND ((`g`.`HasSoulPatch` <> True) AND ((
                        SELECT TOP 1 `t`.`IsAutomatic`
                        FROM (
                            SELECT DISTINCT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
                            FROM `Weapons` AS `w`
                            WHERE `g`.`FullName` = `w`.`OwnerFullName`
                        ) AS `t`
                        ORDER BY `t`.`Id` DESC) = True))
                    ORDER BY `g`.`Nickname`
                    """);
        }

        public override async Task Where_subquery_distinct_orderby_firstordefault_boolean(bool isAsync)
        {
            await base.Where_subquery_distinct_orderby_firstordefault_boolean(isAsync);

            AssertSql(
                $"""
                    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
                    FROM `Gears` AS `g`
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer') AND ((`g`.`HasSoulPatch` = True) AND ((
                        SELECT TOP 1 `t`.`IsAutomatic`
                        FROM (
                            SELECT DISTINCT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
                            FROM `Weapons` AS `w`
                            WHERE `g`.`FullName` = `w`.`OwnerFullName`
                        ) AS `t`
                        ORDER BY `t`.`Id`) = True))
                    """);
        }

        public override async Task Where_subquery_distinct_orderby_firstordefault_boolean_with_pushdown(bool isAsync)
        {
            await base.Where_subquery_distinct_orderby_firstordefault_boolean_with_pushdown(isAsync);

            AssertSql(
                $"""
                    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
                    FROM `Gears` AS `g`
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer') AND ((`g`.`HasSoulPatch` = True) AND ((
                        SELECT TOP 1 `t`.`IsAutomatic`
                        FROM (
                            SELECT DISTINCT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
                            FROM `Weapons` AS `w`
                            WHERE `g`.`FullName` = `w`.`OwnerFullName`
                        ) AS `t`
                        ORDER BY `t`.`Id`) = True))
                    """);
        }

        public override async Task Where_subquery_union_firstordefault_boolean(bool isAsync)
        {
            await base.Where_subquery_union_firstordefault_boolean(isAsync);

            AssertSql(
                $"""
                    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
                    FROM `Gears` AS `g`
                    WHERE `g`.`Discriminator` IN ('Officer', 'Gear') AND (`g`.`HasSoulPatch` = 1)
                    """,
                //
                $"""
                    {AssertSqlHelper.Declaration("@_outer_FullName6='Damon Baird' (Size = 450)")}
                    
                    SELECT `w6`.`Id`, `w6`.`AmmunitionType`, `w6`.`IsAutomatic`, `w6`.`Name`, `w6`.`OwnerFullName`, `w6`.`SynergyWithId`
                    FROM `Weapons` AS `w6`
                    WHERE {AssertSqlHelper.Parameter("@_outer_FullName6")} = `w6`.`OwnerFullName`
                    """,
                //
                $"""
                    {AssertSqlHelper.Declaration("@_outer_FullName5='Damon Baird' (Size = 450)")}
                    
                    SELECT `w5`.`Id`, `w5`.`AmmunitionType`, `w5`.`IsAutomatic`, `w5`.`Name`, `w5`.`OwnerFullName`, `w5`.`SynergyWithId`
                    FROM `Weapons` AS `w5`
                    WHERE {AssertSqlHelper.Parameter("@_outer_FullName5")} = `w5`.`OwnerFullName`
                    """,
                //
                $"""
                    {AssertSqlHelper.Declaration("@_outer_FullName6='Marcus Fenix' (Size = 450)")}
                    
                    SELECT `w6`.`Id`, `w6`.`AmmunitionType`, `w6`.`IsAutomatic`, `w6`.`Name`, `w6`.`OwnerFullName`, `w6`.`SynergyWithId`
                    FROM `Weapons` AS `w6`
                    WHERE {AssertSqlHelper.Parameter("@_outer_FullName6")} = `w6`.`OwnerFullName`
                    """,
                //
                $"""
                    {AssertSqlHelper.Declaration("@_outer_FullName5='Marcus Fenix' (Size = 450)")}
                    
                    SELECT `w5`.`Id`, `w5`.`AmmunitionType`, `w5`.`IsAutomatic`, `w5`.`Name`, `w5`.`OwnerFullName`, `w5`.`SynergyWithId`
                    FROM `Weapons` AS `w5`
                    WHERE {AssertSqlHelper.Parameter("@_outer_FullName5")} = `w5`.`OwnerFullName`
                    """);
        }

        public override async Task Where_subquery_join_firstordefault_boolean(bool async)
        {
            await base.Where_subquery_join_firstordefault_boolean(async);

            AssertSql(
    """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE `g`.`HasSoulPatch` = CAST(1 AS bit) AND (
    SELECT TOP(1) `w`.`IsAutomatic`
    FROM `Weapons` AS `w`
    INNER JOIN (
        SELECT `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
        FROM `Weapons` AS `w0`
        WHERE `g`.`FullName` = `w0`.`OwnerFullName`
    ) AS `t` ON `w`.`Id` = `t`.`Id`
    WHERE `g`.`FullName` = `w`.`OwnerFullName`
    ORDER BY `w`.`Id`) = CAST(1 AS bit)
""");
        }

        public override async Task Where_subquery_left_join_firstordefault_boolean(bool async)
        {
            await base.Where_subquery_left_join_firstordefault_boolean(async);

            AssertSql(
    """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE `g`.`HasSoulPatch` = CAST(1 AS bit) AND (
    SELECT TOP(1) `w`.`IsAutomatic`
    FROM `Weapons` AS `w`
    LEFT JOIN (
        SELECT `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
        FROM `Weapons` AS `w0`
        WHERE `g`.`FullName` = `w0`.`OwnerFullName`
    ) AS `t` ON `w`.`Id` = `t`.`Id`
    WHERE `g`.`FullName` = `w`.`OwnerFullName`
    ORDER BY `w`.`Id`) = CAST(1 AS bit)
""");
        }

        public override async Task Where_subquery_concat_firstordefault_boolean(bool async)
        {
            await base.Where_subquery_concat_firstordefault_boolean(async);

            AssertSql(
    """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE `g`.`HasSoulPatch` = CAST(1 AS bit) AND (
    SELECT TOP(1) `t`.`IsAutomatic`
    FROM (
        SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName`
        UNION ALL
        SELECT `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
        FROM `Weapons` AS `w0`
        WHERE `g`.`FullName` = `w0`.`OwnerFullName`
    ) AS `t`
    ORDER BY `t`.`Id`) = CAST(1 AS bit)
""");
        }

        public override async Task Concat_with_count(bool isAsync)
        {
            await base.Concat_with_count(isAsync);

            AssertSql(
                """
SELECT COUNT(*)
FROM (
    SELECT 1
    FROM `Gears` AS `g`
    UNION ALL
    SELECT 1
    FROM `Gears` AS `g0`
) AS `u`
""");
        }

        public override async Task Concat_scalars_with_count(bool isAsync)
        {
            await base.Concat_scalars_with_count(isAsync);

            AssertSql(
                """
SELECT COUNT(*)
FROM (
    SELECT 1
    FROM `Gears` AS `g`
    UNION ALL
    SELECT 1
    FROM `Gears` AS `g0`
) AS `u`
""");
        }

        public override async Task Concat_anonymous_with_count(bool isAsync)
        {
            await base.Concat_anonymous_with_count(isAsync);

            AssertSql(
                """
SELECT COUNT(*)
FROM (
    SELECT 1
    FROM `Gears` AS `g`
    UNION ALL
    SELECT 1
    FROM `Gears` AS `g0`
) AS `u`
""");
        }

        public override async Task Concat_with_scalar_projection(bool async)
        {
            await base.Concat_with_scalar_projection(async);

            AssertSql(
                """
SELECT `g`.`Nickname`
FROM `Gears` AS `g`
UNION ALL
SELECT `g0`.`Nickname`
FROM `Gears` AS `g0`
""");
        }

        public override async Task Select_navigation_with_concat_and_count(bool async)
        {
            await base.Select_navigation_with_concat_and_count(async);

            AssertSql(
    """
SELECT (
    SELECT COUNT(*)
    FROM (
        SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName`
        UNION ALL
        SELECT `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
        FROM `Weapons` AS `w0`
        WHERE `g`.`FullName` = `w0`.`OwnerFullName`
    ) AS `t`)
FROM `Gears` AS `g`
WHERE `g`.`HasSoulPatch` = CAST(0 AS bit)
""");
        }

        public override async Task Concat_with_collection_navigations(bool async)
        {
            await base.Concat_with_collection_navigations(async);

            AssertSql(
    """
SELECT (
    SELECT COUNT(*)
    FROM (
        SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName`
        UNION
        SELECT `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
        FROM `Weapons` AS `w0`
        WHERE `g`.`FullName` = `w0`.`OwnerFullName`
    ) AS `t`)
FROM `Gears` AS `g`
WHERE `g`.`HasSoulPatch` = CAST(1 AS bit)
""");
        }

        public override async Task Union_with_collection_navigations(bool async)
        {
            await base.Union_with_collection_navigations(async);

            AssertSql(
    """
SELECT (
    SELECT COUNT(*)
    FROM (
        SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
        FROM `Gears` AS `g0`
        WHERE `g`.`Nickname` = `g0`.`LeaderNickname` AND `g`.`SquadId` = `g0`.`LeaderSquadId`
        UNION
        SELECT `g1`.`Nickname`, `g1`.`SquadId`, `g1`.`AssignedCityName`, `g1`.`CityOfBirthName`, `g1`.`Discriminator`, `g1`.`FullName`, `g1`.`HasSoulPatch`, `g1`.`LeaderNickname`, `g1`.`LeaderSquadId`, `g1`.`Rank`
        FROM `Gears` AS `g1`
        WHERE `g`.`Nickname` = `g1`.`LeaderNickname` AND `g`.`SquadId` = `g1`.`LeaderSquadId`
    ) AS `t`)
FROM `Gears` AS `g`
WHERE `g`.`Discriminator` = 'Officer'
""");
        }

        public override async Task Select_subquery_distinct_firstordefault(bool isAsync)
        {
            await base.Select_subquery_distinct_firstordefault(isAsync);

            AssertSql(
                $"""
                    SELECT (
                        SELECT TOP 1 `t`.`Name`
                        FROM (
                            SELECT DISTINCT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
                            FROM `Weapons` AS `w`
                            WHERE `g`.`FullName` = `w`.`OwnerFullName`
                        ) AS `t`
                        ORDER BY `t`.`Id`)
                    FROM `Gears` AS `g`
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer') AND (`g`.`HasSoulPatch` = True)
                    """);
        }

        public override async Task Singleton_Navigation_With_Member_Access(bool isAsync)
        {
            await base.Singleton_Navigation_With_Member_Access(isAsync);

            AssertSql(
"""
SELECT `g`.`CityOfBirthName` AS `B`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE `g`.`Nickname` = 'Marcus' AND (`g`.`CityOfBirthName` <> 'Ephyra' OR `g`.`CityOfBirthName` IS NULL)
""");
        }

        public override async Task GroupJoin_Composite_Key(bool isAsync)
        {
            await base.GroupJoin_Composite_Key(isAsync);

            AssertSql(
"""
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Tags` AS `t`
INNER JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
""");
        }

        public override async Task Join_navigation_translated_to_subquery_composite_key(bool isAsync)
        {
            await base.Join_navigation_translated_to_subquery_composite_key(isAsync);

            AssertSql(
                """
SELECT `g`.`FullName`, `s`.`Note`
FROM `Gears` AS `g`
INNER JOIN (
    SELECT `t`.`Note`, `g0`.`FullName`
    FROM `Tags` AS `t`
    LEFT JOIN `Gears` AS `g0` ON `t`.`GearNickName` = `g0`.`Nickname` AND `t`.`GearSquadId` = `g0`.`SquadId`
) AS `s` ON `g`.`FullName` = `s`.`FullName`
""");
        }

        public override async Task Join_with_order_by_on_inner_sequence_navigation_translated_to_subquery_composite_key(bool isAsync)
        {
            await base.Join_with_order_by_on_inner_sequence_navigation_translated_to_subquery_composite_key(isAsync);

            AssertSql(
                """
SELECT `g`.`FullName`, `s`.`Note`
FROM `Gears` AS `g`
INNER JOIN (
    SELECT `t`.`Note`, `g0`.`FullName`
    FROM `Tags` AS `t`
    LEFT JOIN `Gears` AS `g0` ON `t`.`GearNickName` = `g0`.`Nickname` AND `t`.`GearSquadId` = `g0`.`SquadId`
) AS `s` ON `g`.`FullName` = `s`.`FullName`
""");
        }

        public override async Task Join_with_order_by_without_skip_or_take(bool isAsync)
        {
            await base.Join_with_order_by_without_skip_or_take(isAsync);

            AssertSql(
                """
    SELECT `w`.`Name`, `g`.`FullName`
    FROM `Gears` AS `g`
    INNER JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`
    """);
        }

        public override async Task Join_with_order_by_without_skip_or_take_nested(bool isAsync)
        {
            await base.Join_with_order_by_without_skip_or_take_nested(isAsync);

            AssertSql(
                """
SELECT `w`.`Name`, `g`.`FullName`
FROM (`Squads` AS `s`
INNER JOIN `Gears` AS `g` ON `s`.`Id` = `g`.`SquadId`)
LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`
WHERE `g`.`FullName` IS NOT NULL AND `w`.`OwnerFullName` IS NOT NULL
""");
        }

        public override async Task Collection_with_inheritance_and_join_include_joined(bool isAsync)
        {
            await base.Collection_with_inheritance_and_join_include_joined(isAsync);

            AssertSql(
                """
SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`, `t0`.`Id`, `t0`.`GearNickName`, `t0`.`GearSquadId`, `t0`.`IssueDate`, `t0`.`Note`
FROM (`Tags` AS `t`
INNER JOIN (
    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
    FROM `Gears` AS `g`
    WHERE `g`.`Discriminator` = 'Officer'
) AS `g0` ON `t`.`GearSquadId` = `g0`.`SquadId` AND `t`.`GearNickName` = `g0`.`Nickname`)
LEFT JOIN `Tags` AS `t0` ON `g0`.`Nickname` = `t0`.`GearNickName` AND `g0`.`SquadId` = `t0`.`GearSquadId`
""");
        }

        public override async Task Collection_with_inheritance_and_join_include_source(bool isAsync)
        {
            await base.Collection_with_inheritance_and_join_include_source(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `t0`.`Id`, `t0`.`GearNickName`, `t0`.`GearSquadId`, `t0`.`IssueDate`, `t0`.`Note`
FROM (`Gears` AS `g`
INNER JOIN `Tags` AS `t` ON `g`.`SquadId` = `t`.`GearSquadId` AND `g`.`Nickname` = `t`.`GearNickName`)
LEFT JOIN `Tags` AS `t0` ON `g`.`Nickname` = `t0`.`GearNickName` AND `g`.`SquadId` = `t0`.`GearSquadId`
WHERE `g`.`Discriminator` = 'Officer'
""");
        }

        public override async Task Non_unicode_string_literal_is_used_for_non_unicode_column(bool isAsync)
        {
            await base.Non_unicode_string_literal_is_used_for_non_unicode_column(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`Name`, `c`.`Location`, `c`.`Nation`
                    FROM `Cities` AS `c`
                    WHERE `c`.`Location` = 'Unknown'
                    """);
        }

        public override async Task Non_unicode_string_literal_is_used_for_non_unicode_column_right(bool isAsync)
        {
            await base.Non_unicode_string_literal_is_used_for_non_unicode_column_right(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`Name`, `c`.`Location`, `c`.`Nation`
                    FROM `Cities` AS `c`
                    WHERE 'Unknown' = `c`.`Location`
                    """);
        }

        public override async Task Non_unicode_parameter_is_used_for_non_unicode_column(bool isAsync)
        {
            await base.Non_unicode_parameter_is_used_for_non_unicode_column(isAsync);

            AssertSql(
                """
@value='Unknown' (Size = 100)

SELECT `c`.`Name`, `c`.`Location`, `c`.`Nation`
FROM `Cities` AS `c`
WHERE `c`.`Location` = @value
""");
        }

        public override async Task Non_unicode_string_literals_in_contains_is_used_for_non_unicode_column(bool isAsync)
        {
            await base.Non_unicode_string_literals_in_contains_is_used_for_non_unicode_column(isAsync);

            AssertSql(
                $"""
                    SELECT `c`.`Name`, `c`.`Location`, `c`.`Nation`
                    FROM `Cities` AS `c`
                    WHERE `c`.`Location` IN ('Unknown', 'Jacinto''s location', 'Ephyra''s location')
                    """);
        }

        public override async Task Non_unicode_string_literals_is_used_for_non_unicode_column_with_subquery(bool isAsync)
        {
            await base.Non_unicode_string_literals_is_used_for_non_unicode_column_with_subquery(isAsync);

            AssertSql(
"""
SELECT `c`.`Name`, `c`.`Location`, `c`.`Nation`
FROM `Cities` AS `c`
WHERE `c`.`Location` = 'Unknown' AND (
    SELECT COUNT(*)
    FROM `Gears` AS `g`
    WHERE `c`.`Name` = `g`.`CityOfBirthName` AND `g`.`Nickname` = 'Paduk') = 1
""");
        }

        public override async Task Non_unicode_string_literals_is_used_for_non_unicode_column_in_subquery(bool isAsync)
        {
            await base.Non_unicode_string_literals_is_used_for_non_unicode_column_in_subquery(isAsync);

            AssertSql(
"""
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
INNER JOIN `Cities` AS `c` ON `g`.`CityOfBirthName` = `c`.`Name`
WHERE `g`.`Nickname` = 'Marcus' AND `c`.`Location` = 'Jacinto''s location'
""");
        }

        public override async Task Non_unicode_string_literals_is_used_for_non_unicode_column_with_contains(bool isAsync)
        {
            await base.Non_unicode_string_literals_is_used_for_non_unicode_column_with_contains(isAsync);

            AssertSql(
"""
SELECT `c`.`Name`, `c`.`Location`, `c`.`Nation`
FROM `Cities` AS `c`
WHERE `c`.`Location` LIKE '%Jacinto%'
""");
        }

        public override async Task Unicode_string_literals_is_used_for_non_unicode_column_with_concat(bool isAsync)
        {
            await base.Unicode_string_literals_is_used_for_non_unicode_column_with_concat(isAsync);

            AssertSql(
"""
SELECT `c`.`Name`, `c`.`Location`, `c`.`Nation`
FROM `Cities` AS `c`
WHERE IIF(`c`.`Location` IS NULL, '', `c`.`Location`) & 'Added' LIKE '%Add%'
""");
        }

        public override void Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_coalesce_result1()
        {
            base.Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_coalesce_result1();

            AssertSql(
                """
SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`, `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM (`Gears` AS `g`
LEFT JOIN `Gears` AS `g0` ON `g`.`LeaderNickname` = `g0`.`Nickname`)
LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `g0`.`Nickname`, `g0`.`SquadId`
""");
        }

        public override void Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_coalesce_result2()
        {
            base.Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_coalesce_result2();

            AssertSql(
                """
SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`, `g`.`Nickname`, `g`.`SquadId`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM (`Gears` AS `g`
LEFT JOIN `Gears` AS `g0` ON `g`.`LeaderNickname` = `g0`.`Nickname`)
LEFT JOIN `Weapons` AS `w` ON `g0`.`FullName` = `w`.`OwnerFullName`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `g0`.`Nickname`, `g0`.`SquadId`
""");
        }

        public override async Task Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_coalesce_result3(bool isAsync)
        {
            await base.Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_coalesce_result3(isAsync);

            AssertSql(
                """
SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`, `g`.`Nickname`, `g`.`SquadId`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
FROM ((`Gears` AS `g`
LEFT JOIN `Gears` AS `g0` ON `g`.`LeaderNickname` = `g0`.`Nickname`)
LEFT JOIN `Weapons` AS `w` ON `g0`.`FullName` = `w`.`OwnerFullName`)
LEFT JOIN `Weapons` AS `w0` ON `g`.`FullName` = `w0`.`OwnerFullName`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `g0`.`Nickname`, `g0`.`SquadId`, `w`.`Id`
""");
        }

        public override async Task Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_coalesce_result4(bool isAsync)
        {
            await base.Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_coalesce_result4(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `g0`.`Nickname`, `g0`.`SquadId`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`, `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`, `w1`.`Id`, `w1`.`AmmunitionType`, `w1`.`IsAutomatic`, `w1`.`Name`, `w1`.`OwnerFullName`, `w1`.`SynergyWithId`, `w2`.`Id`, `w2`.`AmmunitionType`, `w2`.`IsAutomatic`, `w2`.`Name`, `w2`.`OwnerFullName`, `w2`.`SynergyWithId`
FROM ((((`Gears` AS `g`
LEFT JOIN `Gears` AS `g0` ON `g`.`LeaderNickname` = `g0`.`Nickname`)
LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`)
LEFT JOIN `Weapons` AS `w0` ON `g0`.`FullName` = `w0`.`OwnerFullName`)
LEFT JOIN `Weapons` AS `w1` ON `g0`.`FullName` = `w1`.`OwnerFullName`)
LEFT JOIN `Weapons` AS `w2` ON `g`.`FullName` = `w2`.`OwnerFullName`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `g0`.`Nickname`, `g0`.`SquadId`, `w`.`Id`, `w0`.`Id`, `w1`.`Id`
""");
        }

        public override async Task Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_inheritance_and_coalesce_result(bool isAsync)
        {
            await base.Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_inheritance_and_coalesce_result(isAsync);

            AssertSql(
                """
SELECT `g1`.`Nickname`, `g1`.`SquadId`, `g1`.`AssignedCityName`, `g1`.`CityOfBirthName`, `g1`.`Discriminator`, `g1`.`FullName`, `g1`.`HasSoulPatch`, `g1`.`LeaderNickname`, `g1`.`LeaderSquadId`, `g1`.`Rank`, `g`.`Nickname`, `g`.`SquadId`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
FROM ((`Gears` AS `g`
LEFT JOIN (
    SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
    FROM `Gears` AS `g0`
    WHERE `g0`.`Discriminator` = 'Officer'
) AS `g1` ON `g`.`LeaderNickname` = `g1`.`Nickname`)
LEFT JOIN `Weapons` AS `w` ON `g1`.`FullName` = `w`.`OwnerFullName`)
LEFT JOIN `Weapons` AS `w0` ON `g`.`FullName` = `w0`.`OwnerFullName`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `g1`.`Nickname`, `g1`.`SquadId`, `w`.`Id`
""");
        }

        public override async Task Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_conditional_result(bool isAsync)
        {
            await base.Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_conditional_result(isAsync);

            AssertSql(
"""
SELECT IIF(`g0`.`Nickname` IS NOT NULL AND `g0`.`SquadId` IS NOT NULL, TRUE, FALSE), `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`, `g`.`Nickname`, `g`.`SquadId`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
FROM ((`Gears` AS `g`
LEFT JOIN `Gears` AS `g0` ON `g`.`LeaderNickname` = `g0`.`Nickname`)
LEFT JOIN `Weapons` AS `w` ON `g0`.`FullName` = `w`.`OwnerFullName`)
LEFT JOIN `Weapons` AS `w0` ON `g`.`FullName` = `w0`.`OwnerFullName`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `g0`.`Nickname`, `g0`.`SquadId`, `w`.`Id`
""");
        }

        public override async Task Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_complex_projection_result(bool isAsync)
        {
            await base.Include_on_GroupJoin_SelectMany_DefaultIfEmpty_with_complex_projection_result(isAsync);

            AssertSql(
"""
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `g0`.`Nickname`, `g0`.`SquadId`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`, `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`, `w1`.`Id`, `w1`.`AmmunitionType`, `w1`.`IsAutomatic`, `w1`.`Name`, `w1`.`OwnerFullName`, `w1`.`SynergyWithId`, `w2`.`Id`, `w2`.`AmmunitionType`, `w2`.`IsAutomatic`, `w2`.`Name`, `w2`.`OwnerFullName`, `w2`.`SynergyWithId`, IIF(`g0`.`Nickname` IS NOT NULL AND `g0`.`SquadId` IS NOT NULL, TRUE, FALSE), `w3`.`Id`, `w3`.`AmmunitionType`, `w3`.`IsAutomatic`, `w3`.`Name`, `w3`.`OwnerFullName`, `w3`.`SynergyWithId`, `w4`.`Id`, `w4`.`AmmunitionType`, `w4`.`IsAutomatic`, `w4`.`Name`, `w4`.`OwnerFullName`, `w4`.`SynergyWithId`
FROM ((((((`Gears` AS `g`
LEFT JOIN `Gears` AS `g0` ON `g`.`LeaderNickname` = `g0`.`Nickname`)
LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`)
LEFT JOIN `Weapons` AS `w0` ON `g0`.`FullName` = `w0`.`OwnerFullName`)
LEFT JOIN `Weapons` AS `w1` ON `g0`.`FullName` = `w1`.`OwnerFullName`)
LEFT JOIN `Weapons` AS `w2` ON `g`.`FullName` = `w2`.`OwnerFullName`)
LEFT JOIN `Weapons` AS `w3` ON `g0`.`FullName` = `w3`.`OwnerFullName`)
LEFT JOIN `Weapons` AS `w4` ON `g`.`FullName` = `w4`.`OwnerFullName`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `g0`.`Nickname`, `g0`.`SquadId`, `w`.`Id`, `w0`.`Id`, `w1`.`Id`, `w2`.`Id`, `w3`.`Id`
""");
        }

        public override async Task Coalesce_operator_in_predicate(bool isAsync)
        {
            await base.Coalesce_operator_in_predicate(isAsync);

            AssertSql(
                """
SELECT `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE IIF(`g`.`HasSoulPatch` IS NULL, FALSE, `g`.`HasSoulPatch`) = TRUE
""");
        }

        public override async Task Coalesce_operator_in_predicate_with_other_conditions(bool isAsync)
        {
            await base.Coalesce_operator_in_predicate_with_other_conditions(isAsync);

            AssertSql(
                """
SELECT `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE (`t`.`Note` <> 'K.I.A.' OR `t`.`Note` IS NULL) AND IIF(`g`.`HasSoulPatch` IS NULL, FALSE, `g`.`HasSoulPatch`) = TRUE
""");
        }

        public override async Task Coalesce_operator_in_projection_with_other_conditions(bool isAsync)
        {
            await base.Coalesce_operator_in_projection_with_other_conditions(isAsync);

            AssertSql(
                """
SELECT IIF((`t`.`Note` <> 'K.I.A.' OR `t`.`Note` IS NULL) AND IIF(`g`.`HasSoulPatch` IS NULL, FALSE, `g`.`HasSoulPatch`) = TRUE, TRUE, FALSE)
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
""");
        }

        public override async Task Optional_navigation_type_compensation_works_with_predicate(bool isAsync)
        {
            await base.Optional_navigation_type_compensation_works_with_predicate(isAsync);
            AssertSql(
"""
SELECT `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE (`t`.`Note` <> 'K.I.A.' OR `t`.`Note` IS NULL) AND `g`.`HasSoulPatch` = TRUE
""");
        }

        public override async Task Optional_navigation_type_compensation_works_with_predicate2(bool isAsync)
        {
            await base.Optional_navigation_type_compensation_works_with_predicate2(isAsync);

            AssertSql(
                """
    SELECT `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`
    FROM `Tags` AS `t`
    LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
    WHERE `g`.`HasSoulPatch` = TRUE
    """);
        }

        public override async Task Optional_navigation_type_compensation_works_with_predicate_negated(bool isAsync)
        {
            await base.Optional_navigation_type_compensation_works_with_predicate_negated(isAsync);

            AssertSql(
                """
SELECT `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE `g`.`HasSoulPatch` = FALSE
""");
        }

        public override async Task Optional_navigation_type_compensation_works_with_predicate_negated_complex1(bool isAsync)
        {
            await base.Optional_navigation_type_compensation_works_with_predicate_negated_complex1(isAsync);

            AssertSql(
                """
SELECT `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE IIF(`g`.`HasSoulPatch` = TRUE, TRUE, `g`.`HasSoulPatch`) = FALSE
""");
        }

        public override async Task Optional_navigation_type_compensation_works_with_predicate_negated_complex2(bool isAsync)
        {
            await base.Optional_navigation_type_compensation_works_with_predicate_negated_complex2(isAsync);

            AssertSql(
                """
SELECT `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE IIF(`g`.`HasSoulPatch` = FALSE, FALSE, `g`.`HasSoulPatch`) = FALSE
""");
        }

        public override async Task Optional_navigation_type_compensation_works_with_conditional_expression(bool isAsync)
        {
            await base.Optional_navigation_type_compensation_works_with_conditional_expression(isAsync);
            AssertSql(
                """
SELECT `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE IIF(`g`.`HasSoulPatch` = TRUE, TRUE, FALSE) = TRUE
""");
        }

        public override async Task Optional_navigation_type_compensation_works_with_binary_expression(bool isAsync)
        {
            await base.Optional_navigation_type_compensation_works_with_binary_expression(isAsync);

            AssertSql(
                """
SELECT `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE `g`.`HasSoulPatch` = TRUE OR (`t`.`Note` LIKE '%Cole%')
""");
        }

        public override async Task Optional_navigation_type_compensation_works_with_binary_and_expression(bool isAsync)
        {
            await base.Optional_navigation_type_compensation_works_with_binary_and_expression(isAsync);

            AssertSql(
                """
SELECT IIF(`g`.`HasSoulPatch` = TRUE AND (`t`.`Note` LIKE '%Cole%') AND `t`.`Note` IS NOT NULL, TRUE, FALSE)
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
""");
        }

        public override async Task Optional_navigation_type_compensation_works_with_projection(bool isAsync)
        {
            await base.Optional_navigation_type_compensation_works_with_projection(isAsync);

            AssertSql(
"""
SELECT `g`.`SquadId`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE `t`.`Note` <> 'K.I.A.' OR `t`.`Note` IS NULL
""");
        }

        public override async Task Optional_navigation_type_compensation_works_with_projection_into_anonymous_type(bool isAsync)
        {
            await base.Optional_navigation_type_compensation_works_with_projection_into_anonymous_type(isAsync);

            AssertSql(
"""
SELECT `g`.`SquadId`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE `t`.`Note` <> 'K.I.A.' OR `t`.`Note` IS NULL
""");
        }

        public override async Task Optional_navigation_type_compensation_works_with_DTOs(bool isAsync)
        {
            await base.Optional_navigation_type_compensation_works_with_DTOs(isAsync);

            AssertSql(
"""
SELECT `g`.`SquadId` AS `Id`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE `t`.`Note` <> 'K.I.A.' OR `t`.`Note` IS NULL
""");
        }

        public override async Task Optional_navigation_type_compensation_works_with_list_initializers(bool isAsync)
        {
            await base.Optional_navigation_type_compensation_works_with_list_initializers(isAsync);

            AssertSql(
"""
SELECT `g`.`SquadId`, `g`.`SquadId` + 1
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE `t`.`Note` <> 'K.I.A.' OR `t`.`Note` IS NULL
ORDER BY `t`.`Note`
""");
        }

        public override async Task Optional_navigation_type_compensation_works_with_array_initializers(bool isAsync)
        {
            await base.Optional_navigation_type_compensation_works_with_array_initializers(isAsync);
            AssertSql(
"""
SELECT `g`.`SquadId`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE `t`.`Note` <> 'K.I.A.' OR `t`.`Note` IS NULL
""");
        }

        public override async Task Optional_navigation_type_compensation_works_with_orderby(bool isAsync)
        {
            await base.Optional_navigation_type_compensation_works_with_orderby(isAsync);

            AssertSql(
"""
SELECT `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE `t`.`Note` <> 'K.I.A.' OR `t`.`Note` IS NULL
ORDER BY `g`.`SquadId`
""");
        }

        public override async Task Optional_navigation_type_compensation_works_with_all(bool isAsync)
        {
            await base.Optional_navigation_type_compensation_works_with_all(isAsync);
            AssertSql(
                """
SELECT IIF(NOT EXISTS (
        SELECT 1
        FROM `Tags` AS `t`
        LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
        WHERE (`t`.`Note` <> 'K.I.A.' OR `t`.`Note` IS NULL) AND `g`.`HasSoulPatch` = FALSE), TRUE, FALSE)
FROM (SELECT COUNT(*) FROM `#Dual`)
""");
        }

        public override async Task Optional_navigation_type_compensation_works_with_negated_predicate(bool isAsync)
        {
            await base.Optional_navigation_type_compensation_works_with_negated_predicate(isAsync);

            AssertSql(
                """
SELECT `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE (`t`.`Note` <> 'K.I.A.' OR `t`.`Note` IS NULL) AND `g`.`HasSoulPatch` = FALSE
""");
        }

        public override async Task Optional_navigation_type_compensation_works_with_contains(bool isAsync)
        {
            await base.Optional_navigation_type_compensation_works_with_contains(isAsync);

            AssertSql(
"""
SELECT `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE (`t`.`Note` <> 'K.I.A.' OR `t`.`Note` IS NULL) AND `g`.`SquadId` IN (
    SELECT `g0`.`SquadId`
    FROM `Gears` AS `g0`
)
""");
        }

        public override async Task Optional_navigation_type_compensation_works_with_skip(bool isAsync)
        {
            await base.Optional_navigation_type_compensation_works_with_skip(isAsync);

            AssertSql();
        }

        public override async Task Optional_navigation_type_compensation_works_with_take(bool isAsync)
        {
            await base.Optional_navigation_type_compensation_works_with_take(isAsync);

            AssertSql();
        }

        public override async Task Select_correlated_filtered_collection(bool isAsync)
        {
            await base.Select_correlated_filtered_collection(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `c`.`Name`, `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
FROM (`Gears` AS `g`
INNER JOIN `Cities` AS `c` ON `g`.`CityOfBirthName` = `c`.`Name`)
LEFT JOIN (
    SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
    FROM `Weapons` AS `w`
    WHERE `w`.`Name` <> 'Lancer' OR `w`.`Name` IS NULL
) AS `w0` ON `g`.`FullName` = `w0`.`OwnerFullName`
WHERE `c`.`Name` IN ('Ephyra', 'Hanover')
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `c`.`Name`
""");
        }

        public override async Task Select_correlated_filtered_collection_with_composite_key(bool isAsync)
        {
            await base.Select_correlated_filtered_collection_with_composite_key(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g1`.`Nickname`, `g1`.`SquadId`, `g1`.`AssignedCityName`, `g1`.`CityOfBirthName`, `g1`.`Discriminator`, `g1`.`FullName`, `g1`.`HasSoulPatch`, `g1`.`LeaderNickname`, `g1`.`LeaderSquadId`, `g1`.`Rank`
FROM `Gears` AS `g`
LEFT JOIN (
    SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
    FROM `Gears` AS `g0`
    WHERE `g0`.`Nickname` <> 'Dom'
) AS `g1` ON `g`.`Nickname` = `g1`.`LeaderNickname` AND `g`.`SquadId` = `g1`.`LeaderSquadId`
WHERE `g`.`Discriminator` = 'Officer'
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `g1`.`Nickname`
""");
        }

        public override async Task Select_correlated_filtered_collection_works_with_caching(bool isAsync)
        {
            await base.Select_correlated_filtered_collection_works_with_caching(isAsync);

            AssertSql(
"""
SELECT `t`.`Id`, `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname`
ORDER BY `t`.`Note`, `t`.`Id`, `g`.`Nickname`
""");
        }

        public override async Task Join_predicate_value_equals_condition(bool isAsync)
        {
            await base.Join_predicate_value_equals_condition(isAsync);

            AssertSql(
                $"""
                    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
                    FROM `Gears` AS `g`
                    INNER JOIN `Weapons` AS `w` ON `w`.`SynergyWithId` IS NOT NULL
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer')
                    """);
        }

        public override async Task Join_predicate_value(bool isAsync)
        {
            await base.Join_predicate_value(isAsync);

            AssertSql(
                $"""
                    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
                    FROM `Gears` AS `g`
                    INNER JOIN `Weapons` AS `w` ON `g`.`HasSoulPatch` = True
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer')
                    """);
        }

        public override async Task Join_predicate_condition_equals_condition(bool isAsync)
        {
            await base.Join_predicate_condition_equals_condition(isAsync);

            AssertSql(
                $"""
                    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
                    FROM `Gears` AS `g`
                    INNER JOIN `Weapons` AS `w` ON `w`.`SynergyWithId` IS NOT NULL
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer')
                    """);
        }

        public override async Task Left_join_predicate_value_equals_condition(bool isAsync)
        {
            await base.Left_join_predicate_value_equals_condition(isAsync);

            AssertSql(
                $"""
                    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
                    FROM `Gears` AS `g`
                    LEFT JOIN `Weapons` AS `w` ON `w`.`SynergyWithId` IS NOT NULL
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer')
                    """);
        }

        public override async Task Left_join_predicate_value(bool isAsync)
        {
            await base.Left_join_predicate_value(isAsync);

            AssertSql(
                $"""
                    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
                    FROM `Gears` AS `g`
                    LEFT JOIN `Weapons` AS `w` ON `g`.`HasSoulPatch` = True
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer')
                    """);
        }

        public override async Task Left_join_predicate_condition_equals_condition(bool isAsync)
        {
            await base.Left_join_predicate_condition_equals_condition(isAsync);

            AssertSql(
                $"""
                    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
                    FROM `Gears` AS `g`
                    LEFT JOIN `Weapons` AS `w` ON `w`.`SynergyWithId` IS NOT NULL
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer')
                    """);
        }

        public override async Task Orderby_added_for_client_side_GroupJoin_composite_dependent_to_principal_LOJ_when_incomplete_key_is_used(
            bool isAsync)
        {
            await base.Orderby_added_for_client_side_GroupJoin_composite_dependent_to_principal_LOJ_when_incomplete_key_is_used(isAsync);

            AssertSql();
        }

        public override async Task Complex_predicate_with_AndAlso_and_nullable_bool_property(bool isAsync)
        {
            await base.Complex_predicate_with_AndAlso_and_nullable_bool_property(isAsync);

            AssertSql(
                """
SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM `Weapons` AS `w`
LEFT JOIN `Gears` AS `g` ON `w`.`OwnerFullName` = `g`.`FullName`
WHERE `w`.`Id` <> 50 AND `g`.`HasSoulPatch` = FALSE
""");
        }

        public override async Task Distinct_with_optional_navigation_is_translated_to_sql(bool isAsync)
        {
            await base.Distinct_with_optional_navigation_is_translated_to_sql(isAsync);

            AssertSql(
"""
SELECT DISTINCT `g`.`HasSoulPatch`
FROM `Gears` AS `g`
LEFT JOIN `Tags` AS `t` ON `g`.`Nickname` = `t`.`GearNickName` AND `g`.`SquadId` = `t`.`GearSquadId`
WHERE `t`.`Note` <> 'Foo' OR `t`.`Note` IS NULL
""");
        }

        public override async Task Sum_with_optional_navigation_is_translated_to_sql(bool isAsync)
        {
            await base.Sum_with_optional_navigation_is_translated_to_sql(isAsync);

            AssertSql(
"""
SELECT IIF(SUM(`g`.`SquadId`) IS NULL, 0, SUM(`g`.`SquadId`))
FROM `Gears` AS `g`
LEFT JOIN `Tags` AS `t` ON `g`.`Nickname` = `t`.`GearNickName` AND `g`.`SquadId` = `t`.`GearSquadId`
WHERE `t`.`Note` <> 'Foo' OR `t`.`Note` IS NULL
""");
        }

        public override async Task Count_with_optional_navigation_is_translated_to_sql(bool isAsync)
        {
            await base.Count_with_optional_navigation_is_translated_to_sql(isAsync);

            AssertSql(
"""
SELECT COUNT(*)
FROM `Gears` AS `g`
LEFT JOIN `Tags` AS `t` ON `g`.`Nickname` = `t`.`GearNickName` AND `g`.`SquadId` = `t`.`GearSquadId`
WHERE `t`.`Note` <> 'Foo' OR `t`.`Note` IS NULL
""");
        }

        public override async Task FirstOrDefault_with_manually_created_groupjoin_is_translated_to_sql(bool isAsync)
        {
            await base.FirstOrDefault_with_manually_created_groupjoin_is_translated_to_sql(isAsync);

            AssertSql(
                """
    SELECT TOP 1 `s`.`Id`, `s`.`Banner`, `s`.`Banner5`, `s`.`InternalNumber`, `s`.`Name`
    FROM `Squads` AS `s`
    LEFT JOIN `Gears` AS `g` ON `s`.`Id` = `g`.`SquadId`
    WHERE `s`.`Name` = 'Kilo'
    """);
        }

        public override async Task Any_with_optional_navigation_as_subquery_predicate_is_translated_to_sql(bool isAsync)
        {
            await base.Any_with_optional_navigation_as_subquery_predicate_is_translated_to_sql(isAsync);

            AssertSql(
"""
SELECT `s`.`Name`
FROM `Squads` AS `s`
WHERE NOT EXISTS (
    SELECT 1
    FROM `Gears` AS `g`
    LEFT JOIN `Tags` AS `t` ON `g`.`Nickname` = `t`.`GearNickName` AND `g`.`SquadId` = `t`.`GearSquadId`
    WHERE `s`.`Id` = `g`.`SquadId` AND `t`.`Note` = 'Dom''s Tag')
""");
        }

        public override async Task All_with_optional_navigation_is_translated_to_sql(bool isAsync)
        {
            await base.All_with_optional_navigation_is_translated_to_sql(isAsync);

            AssertSql(
                """
SELECT IIF(NOT EXISTS (
        SELECT 1
        FROM `Gears` AS `g`
        LEFT JOIN `Tags` AS `t` ON `g`.`Nickname` = `t`.`GearNickName` AND `g`.`SquadId` = `t`.`GearSquadId`
        WHERE `t`.`Note` = 'Foo'), TRUE, FALSE)
FROM (SELECT COUNT(*) FROM `#Dual`)
""");
        }

        public override async Task Contains_with_local_nullable_guid_list_closure(bool isAsync)
        {
            await base.Contains_with_local_nullable_guid_list_closure(isAsync);

            AssertSql(
                """
SELECT `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`
FROM `Tags` AS `t`
WHERE `t`.`Id` IN ('{df36f493-463f-4123-83f9-6b135deeb7ba}', '{23cbcf9b-ce14-45cf-aafa-2c2667ebfdd3}', '{ab1b82d7-88db-42bd-a132-7eef9aa68af4}')
""");
        }

        public override async Task Unnecessary_include_doesnt_get_added_complex_when_projecting_EF_Property(bool isAsync)
        {
            await base.Unnecessary_include_doesnt_get_added_complex_when_projecting_EF_Property(isAsync);

            AssertSql(
                """
SELECT `g`.`FullName`
FROM `Gears` AS `g`
WHERE `g`.`HasSoulPatch` = TRUE
ORDER BY `g`.`Rank`
""");
        }

        public override async Task Multiple_order_bys_are_properly_lifted_from_subquery_created_by_include(bool isAsync)
        {
            await base.Multiple_order_bys_are_properly_lifted_from_subquery_created_by_include(isAsync);

            AssertSql(
                """
SELECT `g`.`FullName`
FROM `Gears` AS `g`
WHERE `g`.`HasSoulPatch` = FALSE
ORDER BY `g`.`FullName`
""");
        }

        public override async Task Order_by_is_properly_lifted_from_subquery_with_same_order_by_in_the_outer_query(bool isAsync)
        {
            await base.Order_by_is_properly_lifted_from_subquery_with_same_order_by_in_the_outer_query(isAsync);

            AssertSql(
                """
SELECT `g`.`FullName`
FROM `Gears` AS `g`
WHERE `g`.`HasSoulPatch` = FALSE
ORDER BY `g`.`FullName`
""");
        }

        public override async Task Where_is_properly_lifted_from_subquery_created_by_include(bool isAsync)
        {
            await base.Where_is_properly_lifted_from_subquery_created_by_include(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`
FROM `Gears` AS `g`
LEFT JOIN `Tags` AS `t` ON `g`.`Nickname` = `t`.`GearNickName` AND `g`.`SquadId` = `t`.`GearSquadId`
WHERE `g`.`FullName` <> 'Augustus Cole' AND `g`.`HasSoulPatch` = FALSE
ORDER BY `g`.`FullName`
""");
        }

        public override async Task Subquery_is_lifted_from_main_from_clause_of_SelectMany(bool isAsync)
        {
            await base.Subquery_is_lifted_from_main_from_clause_of_SelectMany(isAsync);

            AssertSql(
                """
SELECT `g`.`FullName` AS `Name1`, `g0`.`FullName` AS `Name2`
FROM `Gears` AS `g`,
`Gears` AS `g0`
WHERE `g`.`HasSoulPatch` = TRUE AND `g0`.`HasSoulPatch` = FALSE
ORDER BY `g`.`FullName`
""");
        }

        public override async Task Subquery_containing_SelectMany_projecting_main_from_clause_gets_lifted(bool isAsync)
        {
            await base.Subquery_containing_SelectMany_projecting_main_from_clause_gets_lifted(isAsync);
            AssertSql(
"""
SELECT `g`.`FullName`
FROM `Gears` AS `g`,
`Tags` AS `t`
WHERE `g`.`HasSoulPatch` = TRUE
ORDER BY `g`.`FullName`
""");
        }

        public override async Task Subquery_containing_join_projecting_main_from_clause_gets_lifted(bool isAsync)
        {
            await base.Subquery_containing_join_projecting_main_from_clause_gets_lifted(isAsync);
            AssertSql(
"""
SELECT `g`.`Nickname`
FROM `Gears` AS `g`
INNER JOIN `Tags` AS `t` ON `g`.`Nickname` = `t`.`GearNickName`
ORDER BY `g`.`Nickname`
""");
        }

        public override async Task Subquery_containing_left_join_projecting_main_from_clause_gets_lifted(bool isAsync)
        {
            await base.Subquery_containing_left_join_projecting_main_from_clause_gets_lifted(isAsync);
            AssertSql(
"""
SELECT `g`.`Nickname`
FROM `Gears` AS `g`
LEFT JOIN `Tags` AS `t` ON `g`.`Nickname` = `t`.`GearNickName`
ORDER BY `g`.`Nickname`
""");
        }

        public override async Task Subquery_containing_join_gets_lifted_clashing_names(bool isAsync)
        {
            await base.Subquery_containing_join_gets_lifted_clashing_names(isAsync);
            AssertSql(
"""
SELECT `g`.`Nickname`
FROM (`Gears` AS `g`
INNER JOIN `Tags` AS `t` ON `g`.`Nickname` = `t`.`GearNickName`)
INNER JOIN `Tags` AS `t0` ON `g`.`Nickname` = `t0`.`GearNickName`
WHERE `t`.`GearNickName` <> 'Cole Train' OR `t`.`GearNickName` IS NULL
ORDER BY `g`.`Nickname`, `t0`.`Id`
""");
        }

        public override async Task Subquery_created_by_include_gets_lifted_nested(bool isAsync)
        {
            await base.Subquery_created_by_include_gets_lifted_nested(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `c`.`Name`, `c`.`Location`, `c`.`Nation`
FROM `Gears` AS `g`
INNER JOIN `Cities` AS `c` ON `g`.`CityOfBirthName` = `c`.`Name`
WHERE EXISTS (
    SELECT 1
    FROM `Weapons` AS `w`
    WHERE `g`.`FullName` = `w`.`OwnerFullName`) AND `g`.`HasSoulPatch` = FALSE
ORDER BY `g`.`Nickname`
""");
        }

        public override async Task Subquery_is_lifted_from_additional_from_clause(bool isAsync)
        {
            await base.Subquery_is_lifted_from_additional_from_clause(isAsync);

            AssertSql(
                """
SELECT `g`.`FullName` AS `Name1`, `g0`.`FullName` AS `Name2`
FROM `Gears` AS `g`,
`Gears` AS `g0`
WHERE `g`.`HasSoulPatch` = TRUE AND `g0`.`HasSoulPatch` = FALSE
ORDER BY `g`.`FullName`
""");
        }

        public override async Task Subquery_with_result_operator_is_not_lifted(bool isAsync)
        {
            await base.Subquery_with_result_operator_is_not_lifted(isAsync);

            AssertSql(
                """
SELECT `g0`.`FullName`
FROM (
    SELECT TOP @p `g`.`FullName`, `g`.`Rank`
    FROM `Gears` AS `g`
    WHERE `g`.`HasSoulPatch` = FALSE
    ORDER BY `g`.`FullName`
) AS `g0`
ORDER BY `g0`.`Rank`
""");
        }

        public override async Task Skip_with_orderby_followed_by_orderBy_is_pushed_down(bool isAsync)
        {
            await base.Skip_with_orderby_followed_by_orderBy_is_pushed_down(isAsync);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@__p_0='1'")}
                    
                    SELECT `t`.`FullName`
                    FROM (
                        SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
                        FROM `Gears` AS `g`
                        WHERE `g`.`Discriminator` IN ('Gear', 'Officer') AND (`g`.`HasSoulPatch` <> True)
                        ORDER BY `g`.`FullName`
                        SKIP {AssertSqlHelper.Parameter("@__p_0")}
                    ) AS `t`
                    ORDER BY `t`.`Rank`
                    """);
        }

        public override async Task Take_without_orderby_followed_by_orderBy_is_pushed_down1(bool isAsync)
        {
            await base.Take_without_orderby_followed_by_orderBy_is_pushed_down1(isAsync);

            AssertSql(
                """
SELECT `t`.`FullName`
FROM (
    SELECT TOP 999 `g`.`FullName`, `g`.`Rank`
    FROM `Gears` AS `g`
    WHERE `g`.`HasSoulPatch` <> TRUE
) AS `t`
ORDER BY `t`.`FullName`, `t`.`Rank`
""");
        }

        public override async Task Take_without_orderby_followed_by_orderBy_is_pushed_down2(bool isAsync)
        {
            await base.Take_without_orderby_followed_by_orderBy_is_pushed_down2(isAsync);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@__p_0='999'")}
                    
                    SELECT `t`.`FullName`
                    FROM (
                        SELECT TOP {AssertSqlHelper.Parameter("@__p_0")} `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
                        FROM `Gears` AS `g`
                        WHERE `g`.`Discriminator` IN ('Gear', 'Officer') AND (`g`.`HasSoulPatch` <> True)
                    ) AS `t`
                    ORDER BY `t`.`Rank`
                    """);
        }

        public override async Task Take_without_orderby_followed_by_orderBy_is_pushed_down3(bool isAsync)
        {
            await base.Take_without_orderby_followed_by_orderBy_is_pushed_down3(isAsync);

            AssertSql(
                """
SELECT `g0`.`FullName`
FROM (
    SELECT TOP @p `g`.`FullName`, `g`.`Rank`
    FROM `Gears` AS `g`
    WHERE `g`.`HasSoulPatch` = FALSE
) AS `g0`
ORDER BY `g0`.`FullName`, `g0`.`Rank`
""");
        }

        public override async Task Select_length_of_string_property(bool isAsync)
        {
            await base.Select_length_of_string_property(isAsync);

            AssertSql(
                """
    SELECT `w`.`Name`, IIF(LEN(`w`.`Name`) IS NULL, NULL, CLNG(LEN(`w`.`Name`))) AS `Length`
    FROM `Weapons` AS `w`
    """);
        }

        public override async Task Client_method_on_collection_navigation_in_outer_join_key(bool isAsync)
        {
            await base.Client_method_on_collection_navigation_in_outer_join_key(isAsync);

            AssertSql();
        }

        public override async Task Member_access_on_derived_entity_using_cast(bool isAsync)
        {
            await base.Member_access_on_derived_entity_using_cast(isAsync);

            AssertSql(
                """
    SELECT `f`.`Name`, `f`.`Eradicated`
    FROM `Factions` AS `f`
    ORDER BY `f`.`Name`
    """);
        }

        public override async Task Member_access_on_derived_materialized_entity_using_cast(bool isAsync)
        {
            await base.Member_access_on_derived_materialized_entity_using_cast(isAsync);

            AssertSql(
                """
SELECT `f`.`Id`, `f`.`CapitalName`, `f`.`Discriminator`, `f`.`Name`, `f`.`ServerAddress`, `f`.`CommanderName`, `f`.`DeputyCommanderName`, `f`.`Eradicated`
FROM `Factions` AS `f`
ORDER BY `f`.`Name`
""");
        }

        public override async Task Member_access_on_derived_entity_using_cast_and_let(bool isAsync)
        {
            await base.Member_access_on_derived_entity_using_cast_and_let(isAsync);

            AssertSql(
                """
    SELECT `f`.`Name`, `f`.`Eradicated`
    FROM `Factions` AS `f`
    ORDER BY `f`.`Name`
    """);
        }

        public override async Task Property_access_on_derived_entity_using_cast(bool isAsync)
        {
            await base.Property_access_on_derived_entity_using_cast(isAsync);

            AssertSql(
"""
SELECT `f`.`Name`, `f`.`Eradicated`
FROM `Factions` AS `f`
ORDER BY `f`.`Name`
""");
        }

        public override async Task Navigation_access_on_derived_entity_using_cast(bool isAsync)
        {
            await base.Navigation_access_on_derived_entity_using_cast(isAsync);

            AssertSql(
                """
SELECT `f`.`Name`, `l0`.`ThreatLevel` AS `Threat`
FROM `Factions` AS `f`
LEFT JOIN (
    SELECT `l`.`Name`, `l`.`ThreatLevel`
    FROM `LocustLeaders` AS `l`
    WHERE `l`.`Discriminator` = 'LocustCommander'
) AS `l0` ON `f`.`CommanderName` = `l0`.`Name`
ORDER BY `f`.`Name`
""");
        }

        public override async Task Navigation_access_on_derived_materialized_entity_using_cast(bool isAsync)
        {
            await base.Navigation_access_on_derived_materialized_entity_using_cast(isAsync);

            AssertSql(
                """
SELECT `f`.`Id`, `f`.`CapitalName`, `f`.`Discriminator`, `f`.`Name`, `f`.`ServerAddress`, `f`.`CommanderName`, `f`.`DeputyCommanderName`, `f`.`Eradicated`, `l0`.`ThreatLevel` AS `Threat`
FROM `Factions` AS `f`
LEFT JOIN (
    SELECT `l`.`Name`, `l`.`ThreatLevel`
    FROM `LocustLeaders` AS `l`
    WHERE `l`.`Discriminator` = 'LocustCommander'
) AS `l0` ON `f`.`CommanderName` = `l0`.`Name`
ORDER BY `f`.`Name`
""");
        }

        public override async Task Navigation_access_via_EFProperty_on_derived_entity_using_cast(bool isAsync)
        {
            await base.Navigation_access_via_EFProperty_on_derived_entity_using_cast(isAsync);

            AssertSql(
                """
SELECT `f`.`Name`, `l0`.`ThreatLevel` AS `Threat`
FROM `Factions` AS `f`
LEFT JOIN (
    SELECT `l`.`Name`, `l`.`ThreatLevel`
    FROM `LocustLeaders` AS `l`
    WHERE `l`.`Discriminator` = 'LocustCommander'
) AS `l0` ON `f`.`CommanderName` = `l0`.`Name`
ORDER BY `f`.`Name`
""");
        }

        public override async Task Navigation_access_fk_on_derived_entity_using_cast(bool isAsync)
        {
            await base.Navigation_access_fk_on_derived_entity_using_cast(isAsync);

            AssertSql(
                """
SELECT `f`.`Name`, `l0`.`Name` AS `CommanderName`
FROM `Factions` AS `f`
LEFT JOIN (
    SELECT `l`.`Name`
    FROM `LocustLeaders` AS `l`
    WHERE `l`.`Discriminator` = 'LocustCommander'
) AS `l0` ON `f`.`CommanderName` = `l0`.`Name`
ORDER BY `f`.`Name`
""");
        }

        public override async Task Collection_navigation_access_on_derived_entity_using_cast(bool isAsync)
        {
            await base.Collection_navigation_access_on_derived_entity_using_cast(isAsync);

            AssertSql(
                """
    SELECT `f`.`Name`, (
        SELECT COUNT(*)
        FROM `LocustLeaders` AS `l`
        WHERE `f`.`Id` = `l`.`LocustHordeId`) AS `LeadersCount`
    FROM `Factions` AS `f`
    ORDER BY `f`.`Name`
    """);
        }

        public override async Task Collection_navigation_access_on_derived_entity_using_cast_in_SelectMany(bool isAsync)
        {
            await base.Collection_navigation_access_on_derived_entity_using_cast_in_SelectMany(isAsync);

            AssertSql(
                """
    SELECT `f`.`Name`, `l`.`Name` AS `LeaderName`
    FROM `Factions` AS `f`
    INNER JOIN `LocustLeaders` AS `l` ON `f`.`Id` = `l`.`LocustHordeId`
    ORDER BY `l`.`Name`
    """);
        }

        public override async Task Include_on_derived_entity_using_OfType(bool isAsync)
        {
            await base.Include_on_derived_entity_using_OfType(isAsync);

            AssertSql(
                """
SELECT `f`.`Id`, `f`.`CapitalName`, `f`.`Discriminator`, `f`.`Name`, `f`.`ServerAddress`, `f`.`CommanderName`, `f`.`DeputyCommanderName`, `f`.`Eradicated`, `l0`.`Name`, `l0`.`Discriminator`, `l0`.`LocustHordeId`, `l0`.`ThreatLevel`, `l0`.`ThreatLevelByte`, `l0`.`ThreatLevelNullableByte`, `l0`.`DefeatedByNickname`, `l0`.`DefeatedBySquadId`, `l0`.`HighCommandId`, `l1`.`Name`, `l1`.`Discriminator`, `l1`.`LocustHordeId`, `l1`.`ThreatLevel`, `l1`.`ThreatLevelByte`, `l1`.`ThreatLevelNullableByte`, `l1`.`DefeatedByNickname`, `l1`.`DefeatedBySquadId`, `l1`.`HighCommandId`
FROM (`Factions` AS `f`
LEFT JOIN (
    SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`
    FROM `LocustLeaders` AS `l`
    WHERE `l`.`Discriminator` = 'LocustCommander'
) AS `l0` ON `f`.`CommanderName` = `l0`.`Name`)
LEFT JOIN `LocustLeaders` AS `l1` ON `f`.`Id` = `l1`.`LocustHordeId`
ORDER BY `f`.`Name`, `f`.`Id`, `l0`.`Name`
""");
        }

        //        public override async Task Include_on_derived_entity_using_subquery_with_cast(bool isAsync)
        //        {
        //            await base.Include_on_derived_entity_using_subquery_with_cast(isAsync);

        //            AssertSql(
        //                $@"SELECT `f`.`Id`, `f`.`CapitalName`, `f`.`Discriminator`, `f`.`Name`, `f`.`CommanderName`, `f`.`Eradicated`, `t`.`Name`, `t`.`Discriminator`, `t`.`LocustHordeId`, `t`.`ThreatLevel`, `t`.`DefeatedByNickname`, `t`.`DefeatedBySquadId`, `t`.`HighCommandId`
        //FROM `Factions` AS `f`
        //LEFT JOIN (
        //    SELECT `f.Commander`.*
        //    FROM `LocustLeaders` AS `f.Commander`
        //    WHERE `f.Commander`.`Discriminator` = 'LocustCommander'
        //) AS `t` ON (`f`.`Discriminator` = 'LocustHorde') AND (`f`.`CommanderName` = `t`.`Name`)
        //WHERE (`f`.`Discriminator` = 'LocustHorde') AND (`f`.`Discriminator` = 'LocustHorde')
        //ORDER BY `f`.`Name`, `f`.`Id`",
        //                //
        //                $@"SELECT `f.Leaders`.`Name`, `f.Leaders`.`Discriminator`, `f.Leaders`.`LocustHordeId`, `f.Leaders`.`ThreatLevel`, `f.Leaders`.`DefeatedByNickname`, `f.Leaders`.`DefeatedBySquadId`, `f.Leaders`.`HighCommandId`
        //FROM `LocustLeaders` AS `f.Leaders`
        //INNER JOIN (
        //    SELECT DISTINCT `f0`.`Id`, `f0`.`Name`
        //    FROM `Factions` AS `f0`
        //    LEFT JOIN (
        //        SELECT `f.Commander0`.*
        //        FROM `LocustLeaders` AS `f.Commander0`
        //        WHERE `f.Commander0`.`Discriminator` = 'LocustCommander'
        //    ) AS `t0` ON (`f0`.`Discriminator` = 'LocustHorde') AND (`f0`.`CommanderName` = `t0`.`Name`)
        //    WHERE (`f0`.`Discriminator` = 'LocustHorde') AND (`f0`.`Discriminator` = 'LocustHorde')
        //) AS `t1` ON `f.Leaders`.`LocustHordeId` = `t1`.`Id`
        //WHERE `f.Leaders`.`Discriminator` IN ('LocustCommander', 'LocustLeader')
        //ORDER BY `t1`.`Name`, `t1`.`Id`");
        //        }

        //        public override async Task Include_on_derived_entity_using_subquery_with_cast_AsNoTracking(bool isAsync)
        //        {
        //            await base.Include_on_derived_entity_using_subquery_with_cast_AsNoTracking(isAsync);

        //            AssertSql(
        //                $@"SELECT `f`.`Id`, `f`.`CapitalName`, `f`.`Discriminator`, `f`.`Name`, `f`.`CommanderName`, `f`.`Eradicated`, `t`.`Name`, `t`.`Discriminator`, `t`.`LocustHordeId`, `t`.`ThreatLevel`, `t`.`DefeatedByNickname`, `t`.`DefeatedBySquadId`, `t`.`HighCommandId`
        //FROM `Factions` AS `f`
        //LEFT JOIN (
        //    SELECT `f.Commander`.*
        //    FROM `LocustLeaders` AS `f.Commander`
        //    WHERE `f.Commander`.`Discriminator` = 'LocustCommander'
        //) AS `t` ON (`f`.`Discriminator` = 'LocustHorde') AND (`f`.`CommanderName` = `t`.`Name`)
        //WHERE (`f`.`Discriminator` = 'LocustHorde') AND (`f`.`Discriminator` = 'LocustHorde')
        //ORDER BY `f`.`Name`, `f`.`Id`",
        //                //
        //                $@"SELECT `f.Leaders`.`Name`, `f.Leaders`.`Discriminator`, `f.Leaders`.`LocustHordeId`, `f.Leaders`.`ThreatLevel`, `f.Leaders`.`DefeatedByNickname`, `f.Leaders`.`DefeatedBySquadId`, `f.Leaders`.`HighCommandId`
        //FROM `LocustLeaders` AS `f.Leaders`
        //INNER JOIN (
        //    SELECT DISTINCT `f0`.`Id`, `f0`.`Name`
        //    FROM `Factions` AS `f0`
        //    LEFT JOIN (
        //        SELECT `f.Commander0`.*
        //        FROM `LocustLeaders` AS `f.Commander0`
        //        WHERE `f.Commander0`.`Discriminator` = 'LocustCommander'
        //    ) AS `t0` ON (`f0`.`Discriminator` = 'LocustHorde') AND (`f0`.`CommanderName` = `t0`.`Name`)
        //    WHERE (`f0`.`Discriminator` = 'LocustHorde') AND (`f0`.`Discriminator` = 'LocustHorde')
        //) AS `t1` ON `f.Leaders`.`LocustHordeId` = `t1`.`Id`
        //WHERE `f.Leaders`.`Discriminator` IN ('LocustCommander', 'LocustLeader')
        //ORDER BY `t1`.`Name`, `t1`.`Id`");
        //        }

        //        public override void Include_on_derived_entity_using_subquery_with_cast_cross_product_base_entity()
        //        {
        //            base.Include_on_derived_entity_using_subquery_with_cast_cross_product_base_entity();

        //            AssertSql(
        //                $@"SELECT `f2`.`Id`, `f2`.`CapitalName`, `f2`.`Discriminator`, `f2`.`Name`, `f2`.`CommanderName`, `f2`.`Eradicated`, `t`.`Name`, `t`.`Discriminator`, `t`.`LocustHordeId`, `t`.`ThreatLevel`, `t`.`DefeatedByNickname`, `t`.`DefeatedBySquadId`, `t`.`HighCommandId`, `ff`.`Id`, `ff`.`CapitalName`, `ff`.`Discriminator`, `ff`.`Name`, `ff`.`CommanderName`, `ff`.`Eradicated`, `ff.Capital`.`Name`, `ff.Capital`.`Location`, `ff.Capital`.`Nation`
        //FROM `Factions` AS `f2`
        //LEFT JOIN (
        //    SELECT `f2.Commander`.*
        //    FROM `LocustLeaders` AS `f2.Commander`
        //    WHERE `f2.Commander`.`Discriminator` = 'LocustCommander'
        //) AS `t` ON (`f2`.`Discriminator` = 'LocustHorde') AND (`f2`.`CommanderName` = `t`.`Name`)
        //CROSS JOIN `Factions` AS `ff`
        //LEFT JOIN `Cities` AS `ff.Capital` ON `ff`.`CapitalName` = `ff.Capital`.`Name`
        //WHERE (`f2`.`Discriminator` = 'LocustHorde') AND (`f2`.`Discriminator` = 'LocustHorde')
        //ORDER BY `f2`.`Name`, `ff`.`Name`, `f2`.`Id`",
        //                //
        //                $@"SELECT `f2.Leaders`.`Name`, `f2.Leaders`.`Discriminator`, `f2.Leaders`.`LocustHordeId`, `f2.Leaders`.`ThreatLevel`, `f2.Leaders`.`DefeatedByNickname`, `f2.Leaders`.`DefeatedBySquadId`, `f2.Leaders`.`HighCommandId`
        //FROM `LocustLeaders` AS `f2.Leaders`
        //INNER JOIN (
        //    SELECT DISTINCT `f20`.`Id`, `f20`.`Name`, `ff0`.`Name` AS `Name0`
        //    FROM `Factions` AS `f20`
        //    LEFT JOIN (
        //        SELECT `f2.Commander0`.*
        //        FROM `LocustLeaders` AS `f2.Commander0`
        //        WHERE `f2.Commander0`.`Discriminator` = 'LocustCommander'
        //    ) AS `t0` ON (`f20`.`Discriminator` = 'LocustHorde') AND (`f20`.`CommanderName` = `t0`.`Name`)
        //    CROSS JOIN `Factions` AS `ff0`
        //    LEFT JOIN `Cities` AS `ff.Capital0` ON `ff0`.`CapitalName` = `ff.Capital0`.`Name`
        //    WHERE (`f20`.`Discriminator` = 'LocustHorde') AND (`f20`.`Discriminator` = 'LocustHorde')
        //) AS `t1` ON `f2.Leaders`.`LocustHordeId` = `t1`.`Id`
        //WHERE `f2.Leaders`.`Discriminator` IN ('LocustCommander', 'LocustLeader')
        //ORDER BY `t1`.`Name`, `t1`.`Name0`, `t1`.`Id`");
        //        }

        public override async Task Distinct_on_subquery_doesnt_get_lifted(bool isAsync)
        {
            await base.Distinct_on_subquery_doesnt_get_lifted(isAsync);

            AssertSql(
                """
SELECT `g0`.`HasSoulPatch`
FROM (
    SELECT DISTINCT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
    FROM `Gears` AS `g`
) AS `g0`
""");
        }

        public override async Task Cast_result_operator_on_subquery_is_properly_lifted_to_a_convert(bool isAsync)
        {
            await base.Cast_result_operator_on_subquery_is_properly_lifted_to_a_convert(isAsync);

            AssertSql(
                $"""
                    SELECT `f`.`Eradicated`
                    FROM `Factions` AS `f`
                    """);
        }

        public override async Task Comparing_two_collection_navigations_composite_key(bool isAsync)
        {
            await base.Comparing_two_collection_navigations_composite_key(isAsync);

            AssertSql(
                """
    SELECT `g`.`Nickname` AS `Nickname1`, `g0`.`Nickname` AS `Nickname2`
    FROM `Gears` AS `g`,
    `Gears` AS `g0`
    WHERE `g`.`Nickname` = `g0`.`Nickname` AND `g`.`SquadId` = `g0`.`SquadId`
    ORDER BY `g`.`Nickname`
    """);
        }

        public override async Task Comparing_two_collection_navigations_inheritance(bool isAsync)
        {
            await base.Comparing_two_collection_navigations_inheritance(isAsync);

            AssertSql(
                """
SELECT `s`.`Name`, `s`.`Nickname`
FROM ((
    SELECT `f`.`Name`, `f`.`CommanderName`, `g0`.`Nickname`, `g0`.`SquadId`
    FROM `Factions` AS `f`,
    (
        SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`HasSoulPatch`
        FROM `Gears` AS `g`
        WHERE `g`.`Discriminator` = 'Officer'
    ) AS `g0`
    WHERE `g0`.`HasSoulPatch` = TRUE
) AS `s`
LEFT JOIN (
    SELECT `l`.`Name`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`
    FROM `LocustLeaders` AS `l`
    WHERE `l`.`Discriminator` = 'LocustCommander'
) AS `l0` ON `s`.`CommanderName` = `l0`.`Name`)
LEFT JOIN `Gears` AS `g1` ON `l0`.`DefeatedByNickname` = `g1`.`Nickname` AND `l0`.`DefeatedBySquadId` = `g1`.`SquadId`
WHERE `g1`.`Nickname` = `s`.`Nickname` AND `g1`.`SquadId` = `s`.`SquadId`
""");
        }

        public override async Task Comparing_entities_using_Equals_inheritance(bool isAsync)
        {
            await base.Comparing_entities_using_Equals_inheritance(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname` AS `Nickname1`, `g1`.`Nickname` AS `Nickname2`
FROM `Gears` AS `g`,
(
    SELECT `g0`.`Nickname`, `g0`.`SquadId`
    FROM `Gears` AS `g0`
    WHERE `g0`.`Discriminator` = 'Officer'
) AS `g1`
WHERE `g`.`Nickname` = `g1`.`Nickname` AND `g`.`SquadId` = `g1`.`SquadId`
ORDER BY `g`.`Nickname`, `g1`.`Nickname`
""");
        }

        public override async Task Contains_on_nullable_array_produces_correct_sql(bool isAsync)
        {
            await base.Contains_on_nullable_array_produces_correct_sql(isAsync);

            AssertSql(
"""
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
LEFT JOIN `Cities` AS `c` ON `g`.`AssignedCityName` = `c`.`Name`
WHERE `g`.`SquadId` < 2 AND (`c`.`Name` IS NULL OR `c`.`Name` = 'Ephyra')
""");
        }

        public override async Task Optional_navigation_with_collection_composite_key(bool isAsync)
        {
            await base.Optional_navigation_with_collection_composite_key(isAsync);
            AssertSql(
"""
SELECT `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE `g`.`Discriminator` = 'Officer' AND (
    SELECT COUNT(*)
    FROM `Gears` AS `g0`
    WHERE `g`.`Nickname` IS NOT NULL AND `g`.`SquadId` IS NOT NULL AND `g`.`Nickname` = `g0`.`LeaderNickname` AND `g`.`SquadId` = `g0`.`LeaderSquadId` AND `g0`.`Nickname` = 'Dom') > 0
""");
        }

        public override async Task Select_null_conditional_with_inheritance(bool isAsync)
        {
            await base.Select_null_conditional_with_inheritance(isAsync);

            AssertSql(
"""
SELECT IIF(`f`.`CommanderName` IS NOT NULL, `f`.`CommanderName`, NULL)
FROM `Factions` AS `f`
""");
        }

        public override async Task Select_null_conditional_with_inheritance_negative(bool isAsync)
        {
            await base.Select_null_conditional_with_inheritance_negative(isAsync);

            AssertSql(
"""
SELECT IIF(`f`.`CommanderName` IS NOT NULL, `f`.`Eradicated`, NULL)
FROM `Factions` AS `f`
""");
        }

        public override async Task Project_collection_navigation_with_inheritance1(bool isAsync)
        {
            await base.Project_collection_navigation_with_inheritance1(isAsync);

            AssertSql(
                """
SELECT `f`.`Id`, `l0`.`Name`, `f0`.`Id`, `l1`.`Name`, `l1`.`Discriminator`, `l1`.`LocustHordeId`, `l1`.`ThreatLevel`, `l1`.`ThreatLevelByte`, `l1`.`ThreatLevelNullableByte`, `l1`.`DefeatedByNickname`, `l1`.`DefeatedBySquadId`, `l1`.`HighCommandId`
FROM ((`Factions` AS `f`
LEFT JOIN (
    SELECT `l`.`Name`
    FROM `LocustLeaders` AS `l`
    WHERE `l`.`Discriminator` = 'LocustCommander'
) AS `l0` ON `f`.`CommanderName` = `l0`.`Name`)
LEFT JOIN `Factions` AS `f0` ON `l0`.`Name` = `f0`.`CommanderName`)
LEFT JOIN `LocustLeaders` AS `l1` ON `f0`.`Id` = `l1`.`LocustHordeId`
ORDER BY `f`.`Id`, `l0`.`Name`, `f0`.`Id`
""");
        }

        public override async Task Project_collection_navigation_with_inheritance2(bool isAsync)
        {
            await base.Project_collection_navigation_with_inheritance2(isAsync);

            AssertSql(
                """
SELECT `f`.`Id`, `l0`.`Name`, `g`.`Nickname`, `g`.`SquadId`, `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
FROM ((`Factions` AS `f`
LEFT JOIN (
    SELECT `l`.`Name`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`
    FROM `LocustLeaders` AS `l`
    WHERE `l`.`Discriminator` = 'LocustCommander'
) AS `l0` ON `f`.`CommanderName` = `l0`.`Name`)
LEFT JOIN `Gears` AS `g` ON `l0`.`DefeatedByNickname` = `g`.`Nickname` AND `l0`.`DefeatedBySquadId` = `g`.`SquadId`)
LEFT JOIN `Gears` AS `g0` ON (`g`.`Nickname` = `g0`.`LeaderNickname` OR (`g`.`Nickname` IS NULL AND `g0`.`LeaderNickname` IS NULL)) AND `g`.`SquadId` = `g0`.`LeaderSquadId`
ORDER BY `f`.`Id`, `l0`.`Name`, `g`.`Nickname`, `g`.`SquadId`, `g0`.`Nickname`
""");
        }

        public override async Task Project_collection_navigation_with_inheritance3(bool isAsync)
        {
            await base.Project_collection_navigation_with_inheritance3(isAsync);

            AssertSql(
                """
SELECT `f`.`Id`, `l0`.`Name`, `g`.`Nickname`, `g`.`SquadId`, `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
FROM ((`Factions` AS `f`
LEFT JOIN (
    SELECT `l`.`Name`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`
    FROM `LocustLeaders` AS `l`
    WHERE `l`.`Discriminator` = 'LocustCommander'
) AS `l0` ON `f`.`CommanderName` = `l0`.`Name`)
LEFT JOIN `Gears` AS `g` ON `l0`.`DefeatedByNickname` = `g`.`Nickname` AND `l0`.`DefeatedBySquadId` = `g`.`SquadId`)
LEFT JOIN `Gears` AS `g0` ON (`g`.`Nickname` = `g0`.`LeaderNickname` OR (`g`.`Nickname` IS NULL AND `g0`.`LeaderNickname` IS NULL)) AND `g`.`SquadId` = `g0`.`LeaderSquadId`
ORDER BY `f`.`Id`, `l0`.`Name`, `g`.`Nickname`, `g`.`SquadId`, `g0`.`Nickname`
""");
        }

        public override async Task Include_reference_on_derived_type_using_string(bool isAsync)
        {
            await base.Include_reference_on_derived_type_using_string(isAsync);

            AssertSql(
                """
    SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`, `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
    FROM `LocustLeaders` AS `l`
    LEFT JOIN `Gears` AS `g` ON `l`.`DefeatedByNickname` = `g`.`Nickname` AND `l`.`DefeatedBySquadId` = `g`.`SquadId`
    """);
        }

        public override async Task Include_reference_on_derived_type_using_string_nested1(bool isAsync)
        {
            await base.Include_reference_on_derived_type_using_string_nested1(isAsync);

            AssertSql(
                """
    SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`, `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `s`.`Id`, `s`.`Banner`, `s`.`Banner5`, `s`.`InternalNumber`, `s`.`Name`
    FROM (`LocustLeaders` AS `l`
    LEFT JOIN `Gears` AS `g` ON `l`.`DefeatedByNickname` = `g`.`Nickname` AND `l`.`DefeatedBySquadId` = `g`.`SquadId`)
    LEFT JOIN `Squads` AS `s` ON `g`.`SquadId` = `s`.`Id`
    """);
        }

        public override async Task Include_reference_on_derived_type_using_string_nested2(bool isAsync)
        {
            await base.Include_reference_on_derived_type_using_string_nested2(isAsync);

            AssertSql(
                """
SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`, `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `s`.`Nickname`, `s`.`SquadId`, `s`.`AssignedCityName`, `s`.`CityOfBirthName`, `s`.`Discriminator`, `s`.`FullName`, `s`.`HasSoulPatch`, `s`.`LeaderNickname`, `s`.`LeaderSquadId`, `s`.`Rank`, `s`.`Name`, `s`.`Location`, `s`.`Nation`
FROM (`LocustLeaders` AS `l`
LEFT JOIN `Gears` AS `g` ON `l`.`DefeatedByNickname` = `g`.`Nickname` AND `l`.`DefeatedBySquadId` = `g`.`SquadId`)
LEFT JOIN (
    SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`, `c`.`Name`, `c`.`Location`, `c`.`Nation`
    FROM `Gears` AS `g0`
    INNER JOIN `Cities` AS `c` ON `g0`.`CityOfBirthName` = `c`.`Name`
) AS `s` ON (`g`.`Nickname` = `s`.`LeaderNickname` OR (`g`.`Nickname` IS NULL AND `s`.`LeaderNickname` IS NULL)) AND `g`.`SquadId` = `s`.`LeaderSquadId`
ORDER BY `l`.`Name`, `g`.`Nickname`, `g`.`SquadId`, `s`.`Nickname`, `s`.`SquadId`
""");
        }

        public override async Task Include_reference_on_derived_type_using_lambda(bool isAsync)
        {
            await base.Include_reference_on_derived_type_using_lambda(isAsync);

            AssertSql(
                """
    SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`, `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
    FROM `LocustLeaders` AS `l`
    LEFT JOIN `Gears` AS `g` ON `l`.`DefeatedByNickname` = `g`.`Nickname` AND `l`.`DefeatedBySquadId` = `g`.`SquadId`
    """);
        }

        public override async Task Include_reference_on_derived_type_using_lambda_with_soft_cast(bool isAsync)
        {
            await base.Include_reference_on_derived_type_using_lambda_with_soft_cast(isAsync);

            AssertSql(
                """
    SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`, `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
    FROM `LocustLeaders` AS `l`
    LEFT JOIN `Gears` AS `g` ON `l`.`DefeatedByNickname` = `g`.`Nickname` AND `l`.`DefeatedBySquadId` = `g`.`SquadId`
    """);
        }

        public override async Task Include_reference_on_derived_type_using_lambda_with_tracking(bool isAsync)
        {
            await base.Include_reference_on_derived_type_using_lambda_with_tracking(isAsync);

            AssertSql(
                """
    SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`, `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
    FROM `LocustLeaders` AS `l`
    LEFT JOIN `Gears` AS `g` ON `l`.`DefeatedByNickname` = `g`.`Nickname` AND `l`.`DefeatedBySquadId` = `g`.`SquadId`
    """);
        }

        public override async Task Include_collection_on_derived_type_using_string(bool isAsync)
        {
            await base.Include_collection_on_derived_type_using_string(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
FROM `Gears` AS `g`
LEFT JOIN `Gears` AS `g0` ON `g`.`Nickname` = `g0`.`LeaderNickname` AND `g`.`SquadId` = `g0`.`LeaderSquadId`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `g0`.`Nickname`
""");
        }

        public override async Task Include_collection_on_derived_type_using_lambda(bool isAsync)
        {
            await base.Include_collection_on_derived_type_using_lambda(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
FROM `Gears` AS `g`
LEFT JOIN `Gears` AS `g0` ON `g`.`Nickname` = `g0`.`LeaderNickname` AND `g`.`SquadId` = `g0`.`LeaderSquadId`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `g0`.`Nickname`
""");
        }

        public override async Task Include_collection_on_derived_type_using_lambda_with_soft_cast(bool isAsync)
        {
            await base.Include_collection_on_derived_type_using_lambda_with_soft_cast(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
FROM `Gears` AS `g`
LEFT JOIN `Gears` AS `g0` ON `g`.`Nickname` = `g0`.`LeaderNickname` AND `g`.`SquadId` = `g0`.`LeaderSquadId`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `g0`.`Nickname`
""");
        }

        public override async Task Include_base_navigation_on_derived_entity(bool isAsync)
        {
            await base.Include_base_navigation_on_derived_entity(isAsync);

            AssertSql(
"""
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM (`Gears` AS `g`
LEFT JOIN `Tags` AS `t` ON `g`.`Nickname` = `t`.`GearNickName` AND `g`.`SquadId` = `t`.`GearSquadId`)
LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `t`.`Id`
""");
        }

        public override async Task ThenInclude_collection_on_derived_after_base_reference(bool isAsync)
        {
            await base.ThenInclude_collection_on_derived_after_base_reference(isAsync);

            AssertSql(
                """
    SELECT `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`, `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
    FROM (`Tags` AS `t`
    LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`)
    LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`
    ORDER BY `t`.`Id`, `g`.`Nickname`, `g`.`SquadId`
    """);
        }

        public override async Task ThenInclude_collection_on_derived_after_derived_reference(bool isAsync)
        {
            await base.ThenInclude_collection_on_derived_after_derived_reference(isAsync);

            AssertSql(
                """
SELECT `f`.`Id`, `f`.`CapitalName`, `f`.`Discriminator`, `f`.`Name`, `f`.`ServerAddress`, `f`.`CommanderName`, `f`.`DeputyCommanderName`, `f`.`Eradicated`, `l0`.`Name`, `l0`.`Discriminator`, `l0`.`LocustHordeId`, `l0`.`ThreatLevel`, `l0`.`ThreatLevelByte`, `l0`.`ThreatLevelNullableByte`, `l0`.`DefeatedByNickname`, `l0`.`DefeatedBySquadId`, `l0`.`HighCommandId`, `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
FROM ((`Factions` AS `f`
LEFT JOIN (
    SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`
    FROM `LocustLeaders` AS `l`
    WHERE `l`.`Discriminator` = 'LocustCommander'
) AS `l0` ON `f`.`CommanderName` = `l0`.`Name`)
LEFT JOIN `Gears` AS `g` ON `l0`.`DefeatedByNickname` = `g`.`Nickname` AND `l0`.`DefeatedBySquadId` = `g`.`SquadId`)
LEFT JOIN `Gears` AS `g0` ON (`g`.`Nickname` = `g0`.`LeaderNickname` OR (`g`.`Nickname` IS NULL AND `g0`.`LeaderNickname` IS NULL)) AND `g`.`SquadId` = `g0`.`LeaderSquadId`
ORDER BY `f`.`Id`, `l0`.`Name`, `g`.`Nickname`, `g`.`SquadId`, `g0`.`Nickname`
""");
        }

        public override async Task ThenInclude_collection_on_derived_after_derived_collection(bool isAsync)
        {
            await base.ThenInclude_collection_on_derived_after_derived_collection(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `s`.`Nickname`, `s`.`SquadId`, `s`.`AssignedCityName`, `s`.`CityOfBirthName`, `s`.`Discriminator`, `s`.`FullName`, `s`.`HasSoulPatch`, `s`.`LeaderNickname`, `s`.`LeaderSquadId`, `s`.`Rank`, `s`.`Nickname0`, `s`.`SquadId0`, `s`.`AssignedCityName0`, `s`.`CityOfBirthName0`, `s`.`Discriminator0`, `s`.`FullName0`, `s`.`HasSoulPatch0`, `s`.`LeaderNickname0`, `s`.`LeaderSquadId0`, `s`.`Rank0`
FROM `Gears` AS `g`
LEFT JOIN (
    SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`, `g1`.`Nickname` AS `Nickname0`, `g1`.`SquadId` AS `SquadId0`, `g1`.`AssignedCityName` AS `AssignedCityName0`, `g1`.`CityOfBirthName` AS `CityOfBirthName0`, `g1`.`Discriminator` AS `Discriminator0`, `g1`.`FullName` AS `FullName0`, `g1`.`HasSoulPatch` AS `HasSoulPatch0`, `g1`.`LeaderNickname` AS `LeaderNickname0`, `g1`.`LeaderSquadId` AS `LeaderSquadId0`, `g1`.`Rank` AS `Rank0`
    FROM `Gears` AS `g0`
    LEFT JOIN `Gears` AS `g1` ON `g0`.`Nickname` = `g1`.`LeaderNickname` AND `g0`.`SquadId` = `g1`.`LeaderSquadId`
) AS `s` ON `g`.`Nickname` = `s`.`LeaderNickname` AND `g`.`SquadId` = `s`.`LeaderSquadId`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `s`.`Nickname`, `s`.`SquadId`, `s`.`Nickname0`
""");
        }

        public override async Task ThenInclude_reference_on_derived_after_derived_collection(bool isAsync)
        {
            await base.ThenInclude_reference_on_derived_after_derived_collection(isAsync);

            AssertSql(
                """
SELECT `f`.`Id`, `f`.`CapitalName`, `f`.`Discriminator`, `f`.`Name`, `f`.`ServerAddress`, `f`.`CommanderName`, `f`.`DeputyCommanderName`, `f`.`Eradicated`, `s`.`Name`, `s`.`Discriminator`, `s`.`LocustHordeId`, `s`.`ThreatLevel`, `s`.`ThreatLevelByte`, `s`.`ThreatLevelNullableByte`, `s`.`DefeatedByNickname`, `s`.`DefeatedBySquadId`, `s`.`HighCommandId`, `s`.`Nickname`, `s`.`SquadId`, `s`.`AssignedCityName`, `s`.`CityOfBirthName`, `s`.`Discriminator0`, `s`.`FullName`, `s`.`HasSoulPatch`, `s`.`LeaderNickname`, `s`.`LeaderSquadId`, `s`.`Rank`
FROM `Factions` AS `f`
LEFT JOIN (
    SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`, `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator` AS `Discriminator0`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
    FROM `LocustLeaders` AS `l`
    LEFT JOIN `Gears` AS `g` ON `l`.`DefeatedByNickname` = `g`.`Nickname` AND `l`.`DefeatedBySquadId` = `g`.`SquadId`
) AS `s` ON `f`.`Id` = `s`.`LocustHordeId`
ORDER BY `f`.`Id`, `s`.`Name`, `s`.`Nickname`
""");
        }

        public override async Task Multiple_derived_included_on_one_method(bool isAsync)
        {
            await base.Multiple_derived_included_on_one_method(isAsync);

            AssertSql(
                """
SELECT `f`.`Id`, `f`.`CapitalName`, `f`.`Discriminator`, `f`.`Name`, `f`.`ServerAddress`, `f`.`CommanderName`, `f`.`DeputyCommanderName`, `f`.`Eradicated`, `l0`.`Name`, `l0`.`Discriminator`, `l0`.`LocustHordeId`, `l0`.`ThreatLevel`, `l0`.`ThreatLevelByte`, `l0`.`ThreatLevelNullableByte`, `l0`.`DefeatedByNickname`, `l0`.`DefeatedBySquadId`, `l0`.`HighCommandId`, `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
FROM ((`Factions` AS `f`
LEFT JOIN (
    SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`
    FROM `LocustLeaders` AS `l`
    WHERE `l`.`Discriminator` = 'LocustCommander'
) AS `l0` ON `f`.`CommanderName` = `l0`.`Name`)
LEFT JOIN `Gears` AS `g` ON `l0`.`DefeatedByNickname` = `g`.`Nickname` AND `l0`.`DefeatedBySquadId` = `g`.`SquadId`)
LEFT JOIN `Gears` AS `g0` ON (`g`.`Nickname` = `g0`.`LeaderNickname` OR (`g`.`Nickname` IS NULL AND `g0`.`LeaderNickname` IS NULL)) AND `g`.`SquadId` = `g0`.`LeaderSquadId`
ORDER BY `f`.`Id`, `l0`.`Name`, `g`.`Nickname`, `g`.`SquadId`, `g0`.`Nickname`
""");
        }

        public override async Task Include_on_derived_multi_level(bool isAsync)
        {
            await base.Include_on_derived_multi_level(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `s1`.`Nickname`, `s1`.`SquadId`, `s1`.`AssignedCityName`, `s1`.`CityOfBirthName`, `s1`.`Discriminator`, `s1`.`FullName`, `s1`.`HasSoulPatch`, `s1`.`LeaderNickname`, `s1`.`LeaderSquadId`, `s1`.`Rank`, `s1`.`Id`, `s1`.`Banner`, `s1`.`Banner5`, `s1`.`InternalNumber`, `s1`.`Name`, `s1`.`SquadId0`, `s1`.`MissionId`
FROM `Gears` AS `g`
LEFT JOIN (
    SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`, `s`.`Id`, `s`.`Banner`, `s`.`Banner5`, `s`.`InternalNumber`, `s`.`Name`, `s0`.`SquadId` AS `SquadId0`, `s0`.`MissionId`
    FROM (`Gears` AS `g0`
    INNER JOIN `Squads` AS `s` ON `g0`.`SquadId` = `s`.`Id`)
    LEFT JOIN `SquadMissions` AS `s0` ON `s`.`Id` = `s0`.`SquadId`
) AS `s1` ON `g`.`Nickname` = `s1`.`LeaderNickname` AND `g`.`SquadId` = `s1`.`LeaderSquadId`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `s1`.`Nickname`, `s1`.`SquadId`, `s1`.`Id`, `s1`.`SquadId0`
""");
        }

        public override async Task Projecting_nullable_bool_in_conditional_works(bool isAsync)
        {
            await base.Projecting_nullable_bool_in_conditional_works(isAsync);

            AssertSql(
"""
SELECT IIF(`g`.`Nickname` IS NOT NULL AND `g`.`SquadId` IS NOT NULL, `g`.`HasSoulPatch`, FALSE) AS `Prop`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
""");
        }

        public override async Task ToString_string_property_projection(bool async)
        {
            await base.ToString_string_property_projection(async);

            AssertSql(
                """
SELECT `w`.`Name`
FROM `Weapons` AS `w`
""");
        }

        public override async Task ToString_boolean_property_non_nullable(bool async)
        {
            await base.ToString_boolean_property_non_nullable(async);

            AssertSql(
                """
SELECT IIF(`w`.`IsAutomatic` = FALSE, 'False', 'True')
FROM `Weapons` AS `w`
""");
        }

        public override async Task ToString_boolean_property_nullable(bool async)
        {
            await base.ToString_boolean_property_nullable(async);

            AssertSql(
                """
SELECT IIF(`f`.`Eradicated` = FALSE, 'False', IIF(`f`.`Eradicated` = TRUE, 'True', ''))
FROM `Factions` AS `f`
""");
        }

        public override async Task ToString_boolean_computed_nullable(bool async)
        {
            await base.ToString_boolean_computed_nullable(async);
            
            AssertSql(
                """
SELECT CASE CASE
    WHEN NOT ([f].[Eradicated] = CAST(1 AS bit) OR ([f].[CommanderName] = N'Unknown' AND [f].[CommanderName] IS NOT NULL)) THEN CAST(0 AS bit)
    WHEN [f].[Eradicated] = CAST(1 AS bit) OR ([f].[CommanderName] = N'Unknown' AND [f].[CommanderName] IS NOT NULL) THEN CAST(1 AS bit)
END
    WHEN CAST(0 AS bit) THEN N'False'
    WHEN CAST(1 AS bit) THEN N'True'
    ELSE N''
END
FROM [Factions] AS [f]
""");
        }

        public override async Task ToString_enum_property_projection(bool async)
        {
            await base.ToString_enum_property_projection(async);

            AssertSql(
                """
SELECT IIF(`g`.`Rank` = 0, 'None', IIF(`g`.`Rank` = 1, 'Private', IIF(`g`.`Rank` = 2, 'Corporal', IIF(`g`.`Rank` = 4, 'Sergeant', IIF(`g`.`Rank` = 8, 'Lieutenant', IIF(`g`.`Rank` = 16, 'Captain', IIF(`g`.`Rank` = 32, 'Major', IIF(`g`.`Rank` = 64, 'Colonel', IIF(`g`.`Rank` = 128, 'General', (`g`.`Rank` & ''))))))))))
FROM `Gears` AS `g`
""");
        }

        public override async Task ToString_nullable_enum_property_projection(bool async)
        {
            await base.ToString_nullable_enum_property_projection(async);

            AssertSql(
                """
SELECT IIF(`w`.`AmmunitionType` = 1, 'Cartridge', IIF(`w`.`AmmunitionType` = 2, 'Shell', IIF((`w`.`AmmunitionType` & '') IS NULL, '', (`w`.`AmmunitionType` & ''))))
FROM `Weapons` AS `w`
""");
        }

        public override async Task ToString_enum_contains(bool async)
        {
            await base.ToString_enum_contains(async);

            AssertSql(
                """
SELECT `m`.`CodeName`
FROM `Missions` AS `m`
WHERE (`m`.`Difficulty` & '') LIKE '%Med%'
""");
        }

        public override async Task ToString_nullable_enum_contains(bool async)
        {
            await base.ToString_nullable_enum_contains(async);

            AssertSql(
                """
SELECT `w`.`Name`
FROM `Weapons` AS `w`
WHERE IIF(`w`.`AmmunitionType` = 1, 'Cartridge', IIF(`w`.`AmmunitionType` = 2, 'Shell', IIF((`w`.`AmmunitionType` & '') IS NULL, '', (`w`.`AmmunitionType` & '')))) LIKE '%Cart%'
""");
        }

        public override async Task Correlated_collections_naked_navigation_with_ToList(bool isAsync)
        {
            await base.Correlated_collections_naked_navigation_with_ToList(isAsync);

            AssertSql(
"""
SELECT `g`.`Nickname`, `g`.`SquadId`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM `Gears` AS `g`
LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`
WHERE `g`.`Nickname` <> 'Marcus'
ORDER BY `g`.`Nickname`, `g`.`SquadId`
""");
        }

        public override async Task Correlated_collections_naked_navigation_with_ToList_followed_by_projecting_count(bool isAsync)
        {
            await base.Correlated_collections_naked_navigation_with_ToList_followed_by_projecting_count(isAsync);

            AssertSql(
                """
    SELECT (
        SELECT COUNT(*)
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName`)
    FROM `Gears` AS `g`
    WHERE `g`.`Nickname` <> 'Marcus'
    ORDER BY `g`.`Nickname`
    """);
        }

        public override async Task Correlated_collections_naked_navigation_with_ToArray(bool isAsync)
        {
            await base.Correlated_collections_naked_navigation_with_ToArray(isAsync);

            AssertSql(
"""
SELECT `g`.`Nickname`, `g`.`SquadId`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM `Gears` AS `g`
LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`
WHERE `g`.`Nickname` <> 'Marcus'
ORDER BY `g`.`Nickname`, `g`.`SquadId`
""");
        }

        public override async Task Correlated_collections_basic_projection(bool isAsync)
        {
            await base.Correlated_collections_basic_projection(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
FROM `Gears` AS `g`
LEFT JOIN (
    SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
    FROM `Weapons` AS `w`
    WHERE `w`.`IsAutomatic` = TRUE OR `w`.`Name` <> 'foo' OR `w`.`Name` IS NULL
) AS `w0` ON `g`.`FullName` = `w0`.`OwnerFullName`
WHERE `g`.`Nickname` <> 'Marcus'
ORDER BY `g`.`Nickname`, `g`.`SquadId`
""");
        }

        public override async Task Correlated_collections_basic_projection_explicit_to_list(bool isAsync)
        {
            await base.Correlated_collections_basic_projection_explicit_to_list(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
FROM `Gears` AS `g`
LEFT JOIN (
    SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
    FROM `Weapons` AS `w`
    WHERE `w`.`IsAutomatic` = TRUE OR `w`.`Name` <> 'foo' OR `w`.`Name` IS NULL
) AS `w0` ON `g`.`FullName` = `w0`.`OwnerFullName`
WHERE `g`.`Nickname` <> 'Marcus'
ORDER BY `g`.`Nickname`, `g`.`SquadId`
""");
        }

        public override async Task Correlated_collections_basic_projection_explicit_to_array(bool isAsync)
        {
            await base.Correlated_collections_basic_projection_explicit_to_array(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
FROM `Gears` AS `g`
LEFT JOIN (
    SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
    FROM `Weapons` AS `w`
    WHERE `w`.`IsAutomatic` = TRUE OR `w`.`Name` <> 'foo' OR `w`.`Name` IS NULL
) AS `w0` ON `g`.`FullName` = `w0`.`OwnerFullName`
WHERE `g`.`Nickname` <> 'Marcus'
ORDER BY `g`.`Nickname`, `g`.`SquadId`
""");
        }

        public override async Task Correlated_collections_basic_projection_ordered(bool isAsync)
        {
            await base.Correlated_collections_basic_projection_ordered(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
FROM `Gears` AS `g`
LEFT JOIN (
    SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
    FROM `Weapons` AS `w`
    WHERE `w`.`IsAutomatic` = TRUE OR `w`.`Name` <> 'foo' OR `w`.`Name` IS NULL
) AS `w0` ON `g`.`FullName` = `w0`.`OwnerFullName`
WHERE `g`.`Nickname` <> 'Marcus'
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `w0`.`Name` DESC
""");
        }

        public override async Task Correlated_collections_basic_projection_composite_key(bool isAsync)
        {
            await base.Correlated_collections_basic_projection_composite_key(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g1`.`Nickname`, `g1`.`FullName`, `g1`.`SquadId`
FROM `Gears` AS `g`
LEFT JOIN (
    SELECT `g0`.`Nickname`, `g0`.`FullName`, `g0`.`SquadId`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`
    FROM `Gears` AS `g0`
    WHERE `g0`.`HasSoulPatch` = FALSE
) AS `g1` ON `g`.`Nickname` = `g1`.`LeaderNickname` AND `g`.`SquadId` = `g1`.`LeaderSquadId`
WHERE `g`.`Discriminator` = 'Officer' AND `g`.`Nickname` <> 'Foo'
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `g1`.`Nickname`
""");
        }

        public override async Task Correlated_collections_basic_projecting_single_property(bool isAsync)
        {
            await base.Correlated_collections_basic_projecting_single_property(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `w0`.`Name`, `w0`.`Id`
FROM `Gears` AS `g`
LEFT JOIN (
    SELECT `w`.`Name`, `w`.`Id`, `w`.`OwnerFullName`
    FROM `Weapons` AS `w`
    WHERE `w`.`IsAutomatic` = TRUE OR `w`.`Name` <> 'foo' OR `w`.`Name` IS NULL
) AS `w0` ON `g`.`FullName` = `w0`.`OwnerFullName`
WHERE `g`.`Nickname` <> 'Marcus'
ORDER BY `g`.`Nickname`, `g`.`SquadId`
""");
        }

        public override async Task Correlated_collections_basic_projecting_constant(bool isAsync)
        {
            await base.Correlated_collections_basic_projecting_constant(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `w0`.`c`, `w0`.`Id`
FROM `Gears` AS `g`
LEFT JOIN (
    SELECT 'BFG' AS `c`, `w`.`Id`, `w`.`OwnerFullName`
    FROM `Weapons` AS `w`
    WHERE `w`.`IsAutomatic` = TRUE OR `w`.`Name` <> 'foo' OR `w`.`Name` IS NULL
) AS `w0` ON `g`.`FullName` = `w0`.`OwnerFullName`
WHERE `g`.`Nickname` <> 'Marcus'
ORDER BY `g`.`Nickname`, `g`.`SquadId`
""");
        }

        public override async Task Correlated_collections_basic_projecting_constant_bool(bool isAsync)
        {
            await base.Correlated_collections_basic_projecting_constant_bool(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `w0`.`c`, `w0`.`Id`
FROM `Gears` AS `g`
LEFT JOIN (
    SELECT TRUE AS `c`, `w`.`Id`, `w`.`OwnerFullName`
    FROM `Weapons` AS `w`
    WHERE `w`.`IsAutomatic` = TRUE OR `w`.`Name` <> 'foo' OR `w`.`Name` IS NULL
) AS `w0` ON `g`.`FullName` = `w0`.`OwnerFullName`
WHERE `g`.`Nickname` <> 'Marcus'
ORDER BY `g`.`Nickname`, `g`.`SquadId`
""");
        }

        public override async Task Correlated_collections_projection_of_collection_thru_navigation(bool isAsync)
        {
            await base.Correlated_collections_projection_of_collection_thru_navigation(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `s`.`Id`, `s1`.`SquadId`, `s1`.`MissionId`
FROM (`Gears` AS `g`
INNER JOIN `Squads` AS `s` ON `g`.`SquadId` = `s`.`Id`)
LEFT JOIN (
    SELECT `s0`.`SquadId`, `s0`.`MissionId`
    FROM `SquadMissions` AS `s0`
    WHERE `s0`.`MissionId` <> 17
) AS `s1` ON `s`.`Id` = `s1`.`SquadId`
WHERE `g`.`Nickname` <> 'Marcus'
ORDER BY `g`.`FullName`, `g`.`Nickname`, `g`.`SquadId`, `s`.`Id`, `s1`.`SquadId`
""");
        }

        public override async Task Correlated_collections_project_anonymous_collection_result(bool isAsync)
        {
            await base.Correlated_collections_project_anonymous_collection_result(isAsync);

            AssertSql(
                """
    SELECT `s`.`Name`, `s`.`Id`, `g`.`FullName`, `g`.`Rank`, `g`.`Nickname`, `g`.`SquadId`
    FROM `Squads` AS `s`
    LEFT JOIN `Gears` AS `g` ON `s`.`Id` = `g`.`SquadId`
    WHERE `s`.`Id` < 20
    ORDER BY `s`.`Id`, `g`.`Nickname`
    """);
        }

        public override async Task Correlated_collections_nested(bool isAsync)
        {
            await base.Correlated_collections_nested(isAsync);

            AssertSql(
                """
SELECT `s`.`Id`, `s3`.`SquadId`, `s3`.`MissionId`, `s3`.`Id`, `s3`.`SquadId0`, `s3`.`MissionId0`
FROM `Squads` AS `s`
LEFT JOIN (
    SELECT `s0`.`SquadId`, `s0`.`MissionId`, `m`.`Id`, `s2`.`SquadId` AS `SquadId0`, `s2`.`MissionId` AS `MissionId0`
    FROM (`SquadMissions` AS `s0`
    INNER JOIN `Missions` AS `m` ON `s0`.`MissionId` = `m`.`Id`)
    LEFT JOIN (
        SELECT `s1`.`SquadId`, `s1`.`MissionId`
        FROM `SquadMissions` AS `s1`
        WHERE `s1`.`SquadId` < 7
    ) AS `s2` ON `m`.`Id` = `s2`.`MissionId`
    WHERE `s0`.`MissionId` < 42
) AS `s3` ON `s`.`Id` = `s3`.`SquadId`
ORDER BY `s`.`Id`, `s3`.`SquadId`, `s3`.`MissionId`, `s3`.`Id`, `s3`.`SquadId0`
""");
        }

        public override async Task Correlated_collections_nested_mixed_streaming_with_buffer1(bool isAsync)
        {
            await base.Correlated_collections_nested_mixed_streaming_with_buffer1(isAsync);

            AssertSql(
                """
SELECT `s`.`Id`, `s3`.`SquadId`, `s3`.`MissionId`, `s3`.`Id`, `s3`.`SquadId0`, `s3`.`MissionId0`
FROM `Squads` AS `s`
LEFT JOIN (
    SELECT `s0`.`SquadId`, `s0`.`MissionId`, `m`.`Id`, `s2`.`SquadId` AS `SquadId0`, `s2`.`MissionId` AS `MissionId0`
    FROM (`SquadMissions` AS `s0`
    INNER JOIN `Missions` AS `m` ON `s0`.`MissionId` = `m`.`Id`)
    LEFT JOIN (
        SELECT `s1`.`SquadId`, `s1`.`MissionId`
        FROM `SquadMissions` AS `s1`
        WHERE `s1`.`SquadId` < 2
    ) AS `s2` ON `m`.`Id` = `s2`.`MissionId`
    WHERE `s0`.`MissionId` < 3
) AS `s3` ON `s`.`Id` = `s3`.`SquadId`
ORDER BY `s`.`Id`, `s3`.`SquadId`, `s3`.`MissionId`, `s3`.`Id`, `s3`.`SquadId0`
""");
        }

        public override async Task Correlated_collections_nested_mixed_streaming_with_buffer2(bool isAsync)
        {
            await base.Correlated_collections_nested_mixed_streaming_with_buffer2(isAsync);

            AssertSql(
                """
SELECT `s`.`Id`, `s3`.`SquadId`, `s3`.`MissionId`, `s3`.`Id`, `s3`.`SquadId0`, `s3`.`MissionId0`
FROM `Squads` AS `s`
LEFT JOIN (
    SELECT `s0`.`SquadId`, `s0`.`MissionId`, `m`.`Id`, `s2`.`SquadId` AS `SquadId0`, `s2`.`MissionId` AS `MissionId0`
    FROM (`SquadMissions` AS `s0`
    INNER JOIN `Missions` AS `m` ON `s0`.`MissionId` = `m`.`Id`)
    LEFT JOIN (
        SELECT `s1`.`SquadId`, `s1`.`MissionId`
        FROM `SquadMissions` AS `s1`
        WHERE `s1`.`SquadId` < 7
    ) AS `s2` ON `m`.`Id` = `s2`.`MissionId`
    WHERE `s0`.`MissionId` < 42
) AS `s3` ON `s`.`Id` = `s3`.`SquadId`
ORDER BY `s`.`Id`, `s3`.`SquadId`, `s3`.`MissionId`, `s3`.`Id`, `s3`.`SquadId0`
""");
        }

        public override async Task Correlated_collections_nested_with_custom_ordering(bool isAsync)
        {
            await base.Correlated_collections_nested_with_custom_ordering(isAsync);

            AssertSql(
                """
SELECT `g`.`FullName`, `g`.`Nickname`, `g`.`SquadId`, `s`.`FullName`, `s`.`Nickname`, `s`.`SquadId`, `s`.`Id`, `s`.`AmmunitionType`, `s`.`IsAutomatic`, `s`.`Name`, `s`.`OwnerFullName`, `s`.`SynergyWithId`
FROM `Gears` AS `g`
LEFT JOIN (
    SELECT `g0`.`FullName`, `g0`.`Nickname`, `g0`.`SquadId`, `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`, `g0`.`Rank`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`
    FROM `Gears` AS `g0`
    LEFT JOIN (
        SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
        FROM `Weapons` AS `w`
        WHERE `w`.`Name` <> 'Bar' OR `w`.`Name` IS NULL
    ) AS `w0` ON `g0`.`FullName` = `w0`.`OwnerFullName`
    WHERE `g0`.`FullName` <> 'Foo'
) AS `s` ON `g`.`Nickname` = `s`.`LeaderNickname` AND `g`.`SquadId` = `s`.`LeaderSquadId`
WHERE `g`.`Discriminator` = 'Officer'
ORDER BY NOT (`g`.`HasSoulPatch`) DESC, `g`.`Nickname`, `g`.`SquadId`, `s`.`Rank`, `s`.`Nickname`, `s`.`SquadId`, NOT (`s`.`IsAutomatic`)
""");
        }

        public override async Task Correlated_collections_same_collection_projected_multiple_times(bool isAsync)
        {
            await base.Correlated_collections_same_collection_projected_multiple_times(isAsync);

            AssertSql(
                """
SELECT `g`.`FullName`, `g`.`Nickname`, `g`.`SquadId`, `w1`.`Id`, `w1`.`AmmunitionType`, `w1`.`IsAutomatic`, `w1`.`Name`, `w1`.`OwnerFullName`, `w1`.`SynergyWithId`, `w2`.`Id`, `w2`.`AmmunitionType`, `w2`.`IsAutomatic`, `w2`.`Name`, `w2`.`OwnerFullName`, `w2`.`SynergyWithId`
FROM (`Gears` AS `g`
LEFT JOIN (
    SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
    FROM `Weapons` AS `w`
    WHERE `w`.`IsAutomatic` = TRUE
) AS `w1` ON `g`.`FullName` = `w1`.`OwnerFullName`)
LEFT JOIN (
    SELECT `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
    FROM `Weapons` AS `w0`
    WHERE `w0`.`IsAutomatic` = TRUE
) AS `w2` ON `g`.`FullName` = `w2`.`OwnerFullName`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `w1`.`Id`
""");
        }

        public override async Task Correlated_collections_similar_collection_projected_multiple_times(bool isAsync)
        {
            await base.Correlated_collections_similar_collection_projected_multiple_times(isAsync);

            AssertSql(
                """
SELECT `g`.`FullName`, `g`.`Nickname`, `g`.`SquadId`, `w1`.`Id`, `w1`.`AmmunitionType`, `w1`.`IsAutomatic`, `w1`.`Name`, `w1`.`OwnerFullName`, `w1`.`SynergyWithId`, `w2`.`Id`, `w2`.`AmmunitionType`, `w2`.`IsAutomatic`, `w2`.`Name`, `w2`.`OwnerFullName`, `w2`.`SynergyWithId`
FROM (`Gears` AS `g`
LEFT JOIN (
    SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
    FROM `Weapons` AS `w`
    WHERE `w`.`IsAutomatic` = TRUE
) AS `w1` ON `g`.`FullName` = `w1`.`OwnerFullName`)
LEFT JOIN (
    SELECT `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
    FROM `Weapons` AS `w0`
    WHERE `w0`.`IsAutomatic` = FALSE
) AS `w2` ON `g`.`FullName` = `w2`.`OwnerFullName`
ORDER BY `g`.`Rank`, `g`.`Nickname`, `g`.`SquadId`, `w1`.`OwnerFullName`, `w1`.`Id`, NOT (`w2`.`IsAutomatic`)
""");
        }

        public override async Task Correlated_collections_different_collections_projected(bool isAsync)
        {
            await base.Correlated_collections_different_collections_projected(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `w0`.`Name`, `w0`.`IsAutomatic`, `w0`.`Id`, `g0`.`Nickname`, `g0`.`Rank`, `g0`.`SquadId`
FROM (`Gears` AS `g`
LEFT JOIN (
    SELECT `w`.`Name`, `w`.`IsAutomatic`, `w`.`Id`, `w`.`OwnerFullName`
    FROM `Weapons` AS `w`
    WHERE `w`.`IsAutomatic` = TRUE
) AS `w0` ON `g`.`FullName` = `w0`.`OwnerFullName`)
LEFT JOIN `Gears` AS `g0` ON `g`.`Nickname` = `g0`.`LeaderNickname` AND `g`.`SquadId` = `g0`.`LeaderSquadId`
WHERE `g`.`Discriminator` = 'Officer'
ORDER BY `g`.`FullName`, `g`.`Nickname`, `g`.`SquadId`, `w0`.`Id`, `g0`.`FullName`, `g0`.`Nickname`
""");
        }

        public override async Task Multiple_orderby_with_navigation_expansion_on_one_of_the_order_bys(bool isAsync)
        {
            await base.Multiple_orderby_with_navigation_expansion_on_one_of_the_order_bys(isAsync);

            AssertSql(
                """
SELECT `g`.`FullName`
FROM `Gears` AS `g`
LEFT JOIN `Tags` AS `t` ON `g`.`Nickname` = `t`.`GearNickName` AND `g`.`SquadId` = `t`.`GearSquadId`
WHERE `g`.`Discriminator` = 'Officer' AND EXISTS (
    SELECT 1
    FROM `Gears` AS `g0`
    WHERE `g`.`Nickname` = `g0`.`LeaderNickname` AND `g`.`SquadId` = `g0`.`LeaderSquadId`)
ORDER BY NOT (`g`.`HasSoulPatch`) DESC, `t`.`Note`
""");
        }

        public override async Task Multiple_orderby_with_navigation_expansion_on_one_of_the_order_bys_inside_subquery(bool isAsync)
        {
            await base.Multiple_orderby_with_navigation_expansion_on_one_of_the_order_bys_inside_subquery(isAsync);

            AssertSql(
                """
SELECT `g`.`FullName`, `g`.`Nickname`, `g`.`SquadId`, `t`.`Id`, `g1`.`Nickname`, `g1`.`SquadId`, `s`.`Id`, `s`.`AmmunitionType`, `s`.`IsAutomatic`, `s`.`Name`, `s`.`OwnerFullName`, `s`.`SynergyWithId`, `s`.`Nickname`, `s`.`SquadId`
FROM ((`Gears` AS `g`
LEFT JOIN `Tags` AS `t` ON `g`.`Nickname` = `t`.`GearNickName` AND `g`.`SquadId` = `t`.`GearSquadId`)
LEFT JOIN `Gears` AS `g1` ON `t`.`GearNickName` = `g1`.`Nickname` AND `t`.`GearSquadId` = `g1`.`SquadId`)
LEFT JOIN (
    SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`, `g2`.`Nickname`, `g2`.`SquadId`
    FROM `Weapons` AS `w`
    LEFT JOIN `Gears` AS `g2` ON `w`.`OwnerFullName` = `g2`.`FullName`
) AS `s` ON `g1`.`FullName` = `s`.`OwnerFullName`
WHERE `g`.`Discriminator` = 'Officer' AND EXISTS (
    SELECT 1
    FROM `Gears` AS `g0`
    WHERE `g`.`Nickname` = `g0`.`LeaderNickname` AND `g`.`SquadId` = `g0`.`LeaderSquadId`)
ORDER BY NOT (`g`.`HasSoulPatch`) DESC, `t`.`Note`, `g`.`Nickname`, `g`.`SquadId`, `t`.`Id`, `g1`.`Nickname`, `g1`.`SquadId`, NOT (`s`.`IsAutomatic`), `s`.`Nickname` DESC, `s`.`Id`
""");
        }

        public override async Task Multiple_orderby_with_navigation_expansion_on_one_of_the_order_bys_inside_subquery_duplicated_orderings(
            bool isAsync)
        {
            await base.Multiple_orderby_with_navigation_expansion_on_one_of_the_order_bys_inside_subquery_duplicated_orderings(isAsync);

            AssertSql(
                """
SELECT `g`.`FullName`, `g`.`Nickname`, `g`.`SquadId`, `t`.`Id`, `g1`.`Nickname`, `g1`.`SquadId`, `s`.`Id`, `s`.`AmmunitionType`, `s`.`IsAutomatic`, `s`.`Name`, `s`.`OwnerFullName`, `s`.`SynergyWithId`, `s`.`Nickname`, `s`.`SquadId`
FROM ((`Gears` AS `g`
LEFT JOIN `Tags` AS `t` ON `g`.`Nickname` = `t`.`GearNickName` AND `g`.`SquadId` = `t`.`GearSquadId`)
LEFT JOIN `Gears` AS `g1` ON `t`.`GearNickName` = `g1`.`Nickname` AND `t`.`GearSquadId` = `g1`.`SquadId`)
LEFT JOIN (
    SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`, `g2`.`Nickname`, `g2`.`SquadId`
    FROM `Weapons` AS `w`
    LEFT JOIN `Gears` AS `g2` ON `w`.`OwnerFullName` = `g2`.`FullName`
) AS `s` ON `g1`.`FullName` = `s`.`OwnerFullName`
WHERE `g`.`Discriminator` = 'Officer' AND EXISTS (
    SELECT 1
    FROM `Gears` AS `g0`
    WHERE `g`.`Nickname` = `g0`.`LeaderNickname` AND `g`.`SquadId` = `g0`.`LeaderSquadId`)
ORDER BY NOT (`g`.`HasSoulPatch`) DESC, `t`.`Note`, `g`.`Nickname`, `g`.`SquadId`, `t`.`Id`, `g1`.`Nickname`, `g1`.`SquadId`, NOT (`s`.`IsAutomatic`), `s`.`Nickname` DESC, `s`.`Id`
""");
        }

        public override async Task Multiple_orderby_with_navigation_expansion_on_one_of_the_order_bys_inside_subquery_complex_orderings(
            bool isAsync)
        {
            await base.Multiple_orderby_with_navigation_expansion_on_one_of_the_order_bys_inside_subquery_complex_orderings(isAsync);

            AssertSql(
                """
SELECT `g`.`FullName`, `g`.`Nickname`, `g`.`SquadId`, `t`.`Id`, `g1`.`Nickname`, `g1`.`SquadId`, `s`.`Id`, `s`.`AmmunitionType`, `s`.`IsAutomatic`, `s`.`Name`, `s`.`OwnerFullName`, `s`.`SynergyWithId`, `s`.`Nickname`, `s`.`SquadId`
FROM ((`Gears` AS `g`
LEFT JOIN `Tags` AS `t` ON `g`.`Nickname` = `t`.`GearNickName` AND `g`.`SquadId` = `t`.`GearSquadId`)
LEFT JOIN `Gears` AS `g1` ON `t`.`GearNickName` = `g1`.`Nickname` AND `t`.`GearSquadId` = `g1`.`SquadId`)
LEFT JOIN (
    SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`, `g2`.`Nickname`, `g2`.`SquadId`, (
        SELECT COUNT(*)
        FROM `Weapons` AS `w0`
        WHERE `g2`.`FullName` IS NOT NULL AND `g2`.`FullName` = `w0`.`OwnerFullName`) AS `c`
    FROM `Weapons` AS `w`
    LEFT JOIN `Gears` AS `g2` ON `w`.`OwnerFullName` = `g2`.`FullName`
) AS `s` ON `g1`.`FullName` = `s`.`OwnerFullName`
WHERE `g`.`Discriminator` = 'Officer' AND EXISTS (
    SELECT 1
    FROM `Gears` AS `g0`
    WHERE `g`.`Nickname` = `g0`.`LeaderNickname` AND `g`.`SquadId` = `g0`.`LeaderSquadId`)
ORDER BY NOT (`g`.`HasSoulPatch`) DESC, `t`.`Note`, `g`.`Nickname`, `g`.`SquadId`, `t`.`Id`, `g1`.`Nickname`, `g1`.`SquadId`, `s`.`Id` DESC, `s`.`c`, `s`.`Nickname`
""");
        }

        public override async Task Correlated_collections_multiple_nested_complex_collections(bool isAsync)
        {
            await base.Correlated_collections_multiple_nested_complex_collections(isAsync);

            AssertSql(
                """
SELECT `g`.`FullName`, `g`.`Nickname`, `g`.`SquadId`, `t`.`Id`, `g1`.`Nickname`, `g1`.`SquadId`, `s1`.`FullName`, `s1`.`Nickname`, `s1`.`SquadId`, `s1`.`Id`, `s1`.`Nickname0`, `s1`.`SquadId0`, `s1`.`Id0`, `s1`.`Name`, `s1`.`IsAutomatic`, `s1`.`Id1`, `s1`.`Nickname00`, `s1`.`HasSoulPatch`, `s1`.`SquadId00`, `s2`.`Id`, `s2`.`AmmunitionType`, `s2`.`IsAutomatic`, `s2`.`Name`, `s2`.`OwnerFullName`, `s2`.`SynergyWithId`, `s2`.`Nickname`, `s2`.`SquadId`
FROM (((`Gears` AS `g`
LEFT JOIN `Tags` AS `t` ON `g`.`Nickname` = `t`.`GearNickName` AND `g`.`SquadId` = `t`.`GearSquadId`)
LEFT JOIN `Gears` AS `g1` ON `t`.`GearNickName` = `g1`.`Nickname` AND `t`.`GearSquadId` = `g1`.`SquadId`)
LEFT JOIN (
    SELECT `g2`.`FullName`, `g2`.`Nickname`, `g2`.`SquadId`, `s0`.`Id`, `s0`.`Nickname` AS `Nickname0`, `s0`.`SquadId` AS `SquadId0`, `s0`.`Id0`, `s0`.`Name`, `s0`.`IsAutomatic`, `s0`.`Id1`, `s0`.`Nickname0` AS `Nickname00`, `s0`.`HasSoulPatch`, `s0`.`SquadId0` AS `SquadId00`, `g2`.`Rank`, `s0`.`IsAutomatic0`, `g2`.`LeaderNickname`, `g2`.`LeaderSquadId`
    FROM `Gears` AS `g2`
    LEFT JOIN (
        SELECT `w`.`Id`, `g3`.`Nickname`, `g3`.`SquadId`, `s`.`Id` AS `Id0`, `w0`.`Name`, `w0`.`IsAutomatic`, `w0`.`Id` AS `Id1`, `g4`.`Nickname` AS `Nickname0`, `g4`.`HasSoulPatch`, `g4`.`SquadId` AS `SquadId0`, `w`.`IsAutomatic` AS `IsAutomatic0`, `w`.`OwnerFullName`
        FROM (((`Weapons` AS `w`
        LEFT JOIN `Gears` AS `g3` ON `w`.`OwnerFullName` = `g3`.`FullName`)
        LEFT JOIN `Squads` AS `s` ON `g3`.`SquadId` = `s`.`Id`)
        LEFT JOIN `Weapons` AS `w0` ON `g3`.`FullName` = `w0`.`OwnerFullName`)
        LEFT JOIN `Gears` AS `g4` ON `s`.`Id` = `g4`.`SquadId`
        WHERE `w`.`Name` <> 'Bar' OR `w`.`Name` IS NULL
    ) AS `s0` ON `g2`.`FullName` = `s0`.`OwnerFullName`
    WHERE `g2`.`FullName` <> 'Foo'
) AS `s1` ON `g`.`Nickname` = `s1`.`LeaderNickname` AND `g`.`SquadId` = `s1`.`LeaderSquadId`)
LEFT JOIN (
    SELECT `w1`.`Id`, `w1`.`AmmunitionType`, `w1`.`IsAutomatic`, `w1`.`Name`, `w1`.`OwnerFullName`, `w1`.`SynergyWithId`, `g5`.`Nickname`, `g5`.`SquadId`
    FROM `Weapons` AS `w1`
    LEFT JOIN `Gears` AS `g5` ON `w1`.`OwnerFullName` = `g5`.`FullName`
) AS `s2` ON `g1`.`FullName` = `s2`.`OwnerFullName`
WHERE `g`.`Discriminator` = 'Officer' AND EXISTS (
    SELECT 1
    FROM `Gears` AS `g0`
    WHERE `g`.`Nickname` = `g0`.`LeaderNickname` AND `g`.`SquadId` = `g0`.`LeaderSquadId`)
ORDER BY NOT (`g`.`HasSoulPatch`) DESC, `t`.`Note`, `g`.`Nickname`, `g`.`SquadId`, `t`.`Id`, `g1`.`Nickname`, `g1`.`SquadId`, `s1`.`Rank`, `s1`.`Nickname`, `s1`.`SquadId`, NOT (`s1`.`IsAutomatic0`), `s1`.`Id`, `s1`.`Nickname0`, `s1`.`SquadId0`, `s1`.`Id0`, `s1`.`Id1`, `s1`.`Nickname00`, `s1`.`SquadId00`, NOT (`s2`.`IsAutomatic`), `s2`.`Nickname` DESC, `s2`.`Id`
""");
        }

        public override async Task Correlated_collections_inner_subquery_selector_references_outer_qsre(bool isAsync)
        {
            await base.Correlated_collections_inner_subquery_selector_references_outer_qsre(isAsync);

            AssertSql(
                $"""
                    SELECT `g`.`FullName`, `g`.`Nickname`, `g`.`SquadId`, `t`.`FullName`, `t`.`FullName0`, `t`.`Nickname`, `t`.`SquadId`
                    FROM `Gears` AS `g`
                    OUTER APPLY (
                        SELECT `g0`.`FullName`, `g`.`FullName` AS `FullName0`, `g0`.`Nickname`, `g0`.`SquadId`
                        FROM `Gears` AS `g0`
                        WHERE `g0`.`Discriminator` IN ('Gear', 'Officer') AND ((`g`.`Nickname` = `g0`.`LeaderNickname`) AND (`g`.`SquadId` = `g0`.`LeaderSquadId`))
                    ) AS `t`
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer') AND (`g`.`Discriminator` = 'Officer')
                    ORDER BY `g`.`Nickname`, `g`.`SquadId`, `t`.`Nickname`, `t`.`SquadId`
                    """);
        }

        public override async Task Correlated_collections_inner_subquery_predicate_references_outer_qsre(bool isAsync)
        {
            await base.Correlated_collections_inner_subquery_predicate_references_outer_qsre(isAsync);

            AssertSql(
                $"""
                    SELECT `g`.`FullName`, `g`.`Nickname`, `g`.`SquadId`, `t`.`FullName`, `t`.`Nickname`, `t`.`SquadId`
                    FROM `Gears` AS `g`
                    OUTER APPLY (
                        SELECT `g0`.`FullName`, `g0`.`Nickname`, `g0`.`SquadId`
                        FROM `Gears` AS `g0`
                        WHERE (`g0`.`Discriminator` IN ('Gear', 'Officer') AND (`g`.`FullName` <> 'Foo')) AND ((`g`.`Nickname` = `g0`.`LeaderNickname`) AND (`g`.`SquadId` = `g0`.`LeaderSquadId`))
                    ) AS `t`
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer') AND (`g`.`Discriminator` = 'Officer')
                    ORDER BY `g`.`Nickname`, `g`.`SquadId`, `t`.`Nickname`, `t`.`SquadId`
                    """);
        }

        public override async Task Correlated_collections_nested_inner_subquery_references_outer_qsre_one_level_up(bool isAsync)
        {
            await base.Correlated_collections_nested_inner_subquery_references_outer_qsre_one_level_up(isAsync);

            AssertSql(
                $"""
                    SELECT `g`.`FullName`, `g`.`Nickname`, `g`.`SquadId`, `t0`.`FullName`, `t0`.`Nickname`, `t0`.`SquadId`, `t0`.`Name`, `t0`.`Nickname0`, `t0`.`Id`
                    FROM `Gears` AS `g`
                    LEFT JOIN (
                        SELECT `g0`.`FullName`, `g0`.`Nickname`, `g0`.`SquadId`, `t`.`Name`, `t`.`Nickname` AS `Nickname0`, `t`.`Id`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`
                        FROM `Gears` AS `g0`
                        OUTER APPLY (
                            SELECT `w`.`Name`, `g0`.`Nickname`, `w`.`Id`
                            FROM `Weapons` AS `w`
                            WHERE ((`w`.`Name` <> 'Bar') OR `w`.`Name` IS NULL) AND (`g0`.`FullName` = `w`.`OwnerFullName`)
                        ) AS `t`
                        WHERE `g0`.`Discriminator` IN ('Gear', 'Officer') AND (`g0`.`FullName` <> 'Foo')
                    ) AS `t0` ON (`g`.`Nickname` = `t0`.`LeaderNickname`) AND (`g`.`SquadId` = `t0`.`LeaderSquadId`)
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer') AND (`g`.`Discriminator` = 'Officer')
                    ORDER BY `g`.`Nickname`, `g`.`SquadId`, `t0`.`Nickname`, `t0`.`SquadId`, `t0`.`Id`
                    """);
        }

        public override async Task Correlated_collections_nested_inner_subquery_references_outer_qsre_two_levels_up(bool isAsync)
        {
            await base.Correlated_collections_nested_inner_subquery_references_outer_qsre_two_levels_up(isAsync);

            AssertSql(
                $"""
                    SELECT `g`.`FullName`, `g`.`Nickname`, `g`.`SquadId`, `t0`.`FullName`, `t0`.`Nickname`, `t0`.`SquadId`, `t0`.`Name`, `t0`.`Nickname0`, `t0`.`Id`
                    FROM `Gears` AS `g`
                    OUTER APPLY (
                        SELECT `g0`.`FullName`, `g0`.`Nickname`, `g0`.`SquadId`, `t`.`Name`, `t`.`Nickname` AS `Nickname0`, `t`.`Id`
                        FROM `Gears` AS `g0`
                        LEFT JOIN (
                            SELECT `w`.`Name`, `g`.`Nickname`, `w`.`Id`, `w`.`OwnerFullName`
                            FROM `Weapons` AS `w`
                            WHERE (`w`.`Name` <> 'Bar') OR `w`.`Name` IS NULL
                        ) AS `t` ON `g0`.`FullName` = `t`.`OwnerFullName`
                        WHERE (`g0`.`Discriminator` IN ('Gear', 'Officer') AND (`g0`.`FullName` <> 'Foo')) AND ((`g`.`Nickname` = `g0`.`LeaderNickname`) AND (`g`.`SquadId` = `g0`.`LeaderSquadId`))
                    ) AS `t0`
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer') AND (`g`.`Discriminator` = 'Officer')
                    ORDER BY `g`.`Nickname`, `g`.`SquadId`, `t0`.`Nickname`, `t0`.`SquadId`, `t0`.`Id`
                    """);
        }

        public override async Task Correlated_collections_on_select_many(bool isAsync)
        {
            await base.Correlated_collections_on_select_many(isAsync);

            AssertSql(
                $"""
                    SELECT `g`.`Nickname`, `s`.`Name`, `g`.`SquadId`, `s`.`Id`, `t`.`Id`, `t`.`AmmunitionType`, `t`.`IsAutomatic`, `t`.`Name`, `t`.`OwnerFullName`, `t`.`SynergyWithId`, `t0`.`Nickname`, `t0`.`SquadId`, `t0`.`AssignedCityName`, `t0`.`CityOfBirthName`, `t0`.`Discriminator`, `t0`.`FullName`, `t0`.`HasSoulPatch`, `t0`.`LeaderNickname`, `t0`.`LeaderSquadId`, `t0`.`Rank`
                    FROM `Gears` AS `g`,
                    `Squads` AS `s`
                    LEFT JOIN (
                        SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
                        FROM `Weapons` AS `w`
                        WHERE (`w`.`IsAutomatic` = True) OR ((`w`.`Name` <> 'foo') OR `w`.`Name` IS NULL)
                    ) AS `t` ON `g`.`FullName` = `t`.`OwnerFullName`
                    LEFT JOIN (
                        SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
                        FROM `Gears` AS `g0`
                        WHERE `g0`.`Discriminator` IN ('Gear', 'Officer') AND (`g0`.`HasSoulPatch` <> True)
                    ) AS `t0` ON `s`.`Id` = `t0`.`SquadId`
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer') AND (`g`.`HasSoulPatch` = True)
                    ORDER BY `g`.`Nickname`, `s`.`Id` DESC, `g`.`SquadId`, `t`.`Id`, `t0`.`Nickname`, `t0`.`SquadId`
                    """);
        }

        public override async Task Correlated_collections_with_Skip(bool isAsync)
        {
            await base.Correlated_collections_with_Skip(isAsync);

            AssertSql(
                $"""
                    SELECT `s`.`Id`
                    FROM `Squads` AS `s`
                    ORDER BY `s`.`Name`
                    """,
                //
                $"""
                    {AssertSqlHelper.Declaration("@_outer_Id='1'")}
                    
                    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
                    FROM `Gears` AS `g`
                    WHERE `g`.`Discriminator` IN ('Officer', 'Gear') AND ({AssertSqlHelper.Parameter("@_outer_Id")} = `g`.`SquadId`)
                    ORDER BY `g`.`Nickname`
                    SKIP 1
                    """,
                //
                $"""
                    {AssertSqlHelper.Declaration("@_outer_Id='2'")}
                    
                    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
                    FROM `Gears` AS `g`
                    WHERE `g`.`Discriminator` IN ('Officer', 'Gear') AND ({AssertSqlHelper.Parameter("@_outer_Id")} = `g`.`SquadId`)
                    ORDER BY `g`.`Nickname`
                    SKIP 1
                    """);
        }

        public override async Task Correlated_collections_with_Take(bool isAsync)
        {
            await base.Correlated_collections_with_Take(isAsync);

            AssertSql(
                $"""
                    SELECT `s`.`Id`
                    FROM `Squads` AS `s`
                    ORDER BY `s`.`Name`
                    """,
                //
                $"""
                    {AssertSqlHelper.Declaration("@_outer_Id='1'")}
                    
                    SELECT TOP 2 `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
                    FROM `Gears` AS `g`
                    WHERE `g`.`Discriminator` IN ('Officer', 'Gear') AND ({AssertSqlHelper.Parameter("@_outer_Id")} = `g`.`SquadId`)
                    ORDER BY `g`.`Nickname`
                    """,
                //
                $"""
                    {AssertSqlHelper.Declaration("@_outer_Id='2'")}
                    
                    SELECT TOP 2 `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
                    FROM `Gears` AS `g`
                    WHERE `g`.`Discriminator` IN ('Officer', 'Gear') AND ({AssertSqlHelper.Parameter("@_outer_Id")} = `g`.`SquadId`)
                    ORDER BY `g`.`Nickname`
                    """);
        }

        public override async Task Correlated_collections_with_Distinct(bool isAsync)
        {
            await base.Correlated_collections_with_Distinct(isAsync);

            AssertSql(
                $"""
                    SELECT `s`.`Id`, `t`.`Nickname`, `t`.`SquadId`, `t`.`AssignedCityName`, `t`.`CityOfBirthName`, `t`.`Discriminator`, `t`.`FullName`, `t`.`HasSoulPatch`, `t`.`LeaderNickname`, `t`.`LeaderSquadId`, `t`.`Rank`
                    FROM `Squads` AS `s`
                    LEFT JOIN (
                        SELECT DISTINCT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
                        FROM `Gears` AS `g`
                        WHERE `g`.`Discriminator` IN ('Gear', 'Officer')
                    ) AS `t` ON `s`.`Id` = `t`.`SquadId`
                    ORDER BY `s`.`Name`, `s`.`Id`, `t`.`Nickname`, `t`.`SquadId`
                    """);
        }

        public override async Task Correlated_collections_with_FirstOrDefault(bool isAsync)
        {
            await base.Correlated_collections_with_FirstOrDefault(isAsync);

            AssertSql(
                """
    SELECT (
        SELECT TOP 1 `g`.`FullName`
        FROM `Gears` AS `g`
        WHERE `s`.`Id` = `g`.`SquadId`
        ORDER BY `g`.`Nickname`)
    FROM `Squads` AS `s`
    ORDER BY `s`.`Name`
    """);
        }

        public override async Task Correlated_collections_on_left_join_with_predicate(bool isAsync)
        {
            await base.Correlated_collections_on_left_join_with_predicate(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `t`.`Id`, `g`.`SquadId`, `w`.`Name`, `w`.`Id`
FROM (`Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname`)
LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`
WHERE `g`.`HasSoulPatch` = FALSE
ORDER BY `t`.`Id`, `g`.`Nickname`, `g`.`SquadId`
""");
        }

        public override async Task Correlated_collections_on_RightJoin_with_predicate(bool async)
        {
            await base.Correlated_collections_on_RightJoin_with_predicate(async);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `t`.`Id`, `w`.`Name`, `w`.`Id`
FROM (`Gears` AS `g`
RIGHT JOIN `Tags` AS `t` ON `g`.`Nickname` = `t`.`GearNickName`)
LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`
WHERE `g`.`HasSoulPatch` = FALSE
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `t`.`Id`
""");
        }

        public override async Task Correlated_collections_on_left_join_with_null_value(bool isAsync)
        {
            await base.Correlated_collections_on_left_join_with_null_value(isAsync);

            AssertSql(
                """
    SELECT `t`.`Id`, `g`.`Nickname`, `g`.`SquadId`, `w`.`Name`, `w`.`Id`
    FROM (`Tags` AS `t`
    LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname`)
    LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`
    ORDER BY `t`.`Note`, `t`.`Id`, `g`.`Nickname`, `g`.`SquadId`
    """);
        }

        public override async Task Correlated_collections_left_join_with_self_reference(bool isAsync)
        {
            await base.Correlated_collections_left_join_with_self_reference(isAsync);

            AssertSql(
                """
SELECT `t`.`Note`, `t`.`Id`, `g0`.`Nickname`, `g0`.`SquadId`, `g1`.`FullName`, `g1`.`Nickname`, `g1`.`SquadId`
FROM (`Tags` AS `t`
LEFT JOIN (
    SELECT `g`.`Nickname`, `g`.`SquadId`
    FROM `Gears` AS `g`
    WHERE `g`.`Discriminator` = 'Officer'
) AS `g0` ON `t`.`GearNickName` = `g0`.`Nickname`)
LEFT JOIN `Gears` AS `g1` ON (`g0`.`Nickname` = `g1`.`LeaderNickname` OR (`g0`.`Nickname` IS NULL AND `g1`.`LeaderNickname` IS NULL)) AND `g0`.`SquadId` = `g1`.`LeaderSquadId`
ORDER BY `t`.`Id`, `g0`.`Nickname`, `g0`.`SquadId`, `g1`.`Nickname`
""");
        }

        public override async Task Correlated_collections_deeply_nested_left_join(bool isAsync)
        {
            await base.Correlated_collections_deeply_nested_left_join(isAsync);

            AssertSql(
                """
SELECT `t`.`Id`, `g`.`Nickname`, `g`.`SquadId`, `s`.`Id`, `s0`.`Nickname`, `s0`.`SquadId`, `s0`.`Id`, `s0`.`AmmunitionType`, `s0`.`IsAutomatic`, `s0`.`Name`, `s0`.`OwnerFullName`, `s0`.`SynergyWithId`
FROM ((`Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname`)
LEFT JOIN `Squads` AS `s` ON `g`.`SquadId` = `s`.`Id`)
LEFT JOIN (
    SELECT `g0`.`Nickname`, `g0`.`SquadId`, `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
    FROM `Gears` AS `g0`
    LEFT JOIN (
        SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
        FROM `Weapons` AS `w`
        WHERE `w`.`IsAutomatic` = TRUE
    ) AS `w0` ON `g0`.`FullName` = `w0`.`OwnerFullName`
    WHERE `g0`.`HasSoulPatch` = TRUE
) AS `s0` ON `s`.`Id` = `s0`.`SquadId`
ORDER BY `t`.`Note`, `g`.`Nickname` DESC, `t`.`Id`, `g`.`SquadId`, `s`.`Id`, `s0`.`Nickname`, `s0`.`SquadId`
""");
        }

        public override async Task Correlated_collections_from_left_join_with_additional_elements_projected_of_that_join(bool isAsync)
        {
            await base.Correlated_collections_from_left_join_with_additional_elements_projected_of_that_join(isAsync);

            AssertSql(
                """
SELECT `w`.`Id`, `g`.`Nickname`, `g`.`SquadId`, `s`.`Id`, `s0`.`Nickname`, `s0`.`SquadId`, `s0`.`Id`, `s0`.`AmmunitionType`, `s0`.`IsAutomatic`, `s0`.`Name`, `s0`.`OwnerFullName`, `s0`.`SynergyWithId`, `s0`.`Rank`
FROM ((`Weapons` AS `w`
LEFT JOIN `Gears` AS `g` ON `w`.`OwnerFullName` = `g`.`FullName`)
LEFT JOIN `Squads` AS `s` ON `g`.`SquadId` = `s`.`Id`)
LEFT JOIN (
    SELECT `g0`.`Nickname`, `g0`.`SquadId`, `w1`.`Id`, `w1`.`AmmunitionType`, `w1`.`IsAutomatic`, `w1`.`Name`, `w1`.`OwnerFullName`, `w1`.`SynergyWithId`, `g0`.`Rank`, `g0`.`FullName`
    FROM `Gears` AS `g0`
    LEFT JOIN (
        SELECT `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
        FROM `Weapons` AS `w0`
        WHERE `w0`.`IsAutomatic` = FALSE
    ) AS `w1` ON `g0`.`FullName` = `w1`.`OwnerFullName`
) AS `s0` ON `s`.`Id` = `s0`.`SquadId`
ORDER BY `w`.`Name`, `w`.`Id`, `g`.`Nickname`, `g`.`SquadId`, `s`.`Id`, `s0`.`FullName` DESC, `s0`.`Nickname`, `s0`.`SquadId`, `s0`.`Id`
""");
        }

        public override async Task Correlated_collections_complex_scenario1(bool isAsync)
        {
            await base.Correlated_collections_complex_scenario1(isAsync);

            AssertSql(
                """
SELECT `g`.`FullName`, `g`.`Nickname`, `g`.`SquadId`, `s0`.`Id`, `s0`.`Nickname`, `s0`.`SquadId`, `s0`.`Id0`, `s0`.`Nickname0`, `s0`.`HasSoulPatch`, `s0`.`SquadId0`
FROM `Gears` AS `g`
LEFT JOIN (
    SELECT `w`.`Id`, `g0`.`Nickname`, `g0`.`SquadId`, `s`.`Id` AS `Id0`, `g1`.`Nickname` AS `Nickname0`, `g1`.`HasSoulPatch`, `g1`.`SquadId` AS `SquadId0`, `w`.`OwnerFullName`
    FROM ((`Weapons` AS `w`
    LEFT JOIN `Gears` AS `g0` ON `w`.`OwnerFullName` = `g0`.`FullName`)
    LEFT JOIN `Squads` AS `s` ON `g0`.`SquadId` = `s`.`Id`)
    LEFT JOIN `Gears` AS `g1` ON `s`.`Id` = `g1`.`SquadId`
) AS `s0` ON `g`.`FullName` = `s0`.`OwnerFullName`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `s0`.`Id`, `s0`.`Nickname`, `s0`.`SquadId`, `s0`.`Id0`, `s0`.`Nickname0`
""");
        }

        public override async Task Correlated_collections_complex_scenario2(bool isAsync)
        {
            await base.Correlated_collections_complex_scenario2(isAsync);

            AssertSql(
                """
SELECT `g`.`FullName`, `g`.`Nickname`, `g`.`SquadId`, `s1`.`FullName`, `s1`.`Nickname`, `s1`.`SquadId`, `s1`.`Id`, `s1`.`Nickname0`, `s1`.`SquadId0`, `s1`.`Id0`, `s1`.`Nickname00`, `s1`.`HasSoulPatch`, `s1`.`SquadId00`
FROM `Gears` AS `g`
LEFT JOIN (
    SELECT `g0`.`FullName`, `g0`.`Nickname`, `g0`.`SquadId`, `s0`.`Id`, `s0`.`Nickname` AS `Nickname0`, `s0`.`SquadId` AS `SquadId0`, `s0`.`Id0`, `s0`.`Nickname0` AS `Nickname00`, `s0`.`HasSoulPatch`, `s0`.`SquadId0` AS `SquadId00`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`
    FROM `Gears` AS `g0`
    LEFT JOIN (
        SELECT `w`.`Id`, `g1`.`Nickname`, `g1`.`SquadId`, `s`.`Id` AS `Id0`, `g2`.`Nickname` AS `Nickname0`, `g2`.`HasSoulPatch`, `g2`.`SquadId` AS `SquadId0`, `w`.`OwnerFullName`
        FROM ((`Weapons` AS `w`
        LEFT JOIN `Gears` AS `g1` ON `w`.`OwnerFullName` = `g1`.`FullName`)
        LEFT JOIN `Squads` AS `s` ON `g1`.`SquadId` = `s`.`Id`)
        LEFT JOIN `Gears` AS `g2` ON `s`.`Id` = `g2`.`SquadId`
    ) AS `s0` ON `g0`.`FullName` = `s0`.`OwnerFullName`
) AS `s1` ON `g`.`Nickname` = `s1`.`LeaderNickname` AND `g`.`SquadId` = `s1`.`LeaderSquadId`
WHERE `g`.`Discriminator` = 'Officer'
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `s1`.`Nickname`, `s1`.`SquadId`, `s1`.`Id`, `s1`.`Nickname0`, `s1`.`SquadId0`, `s1`.`Id0`, `s1`.`Nickname00`
""");
        }

        public override async Task Correlated_collections_with_funky_orderby_complex_scenario1(bool isAsync)
        {
            await base.Correlated_collections_with_funky_orderby_complex_scenario1(isAsync);

            AssertSql(
                """
SELECT `g`.`FullName`, `g`.`Nickname`, `g`.`SquadId`, `s0`.`Id`, `s0`.`Nickname`, `s0`.`SquadId`, `s0`.`Id0`, `s0`.`Nickname0`, `s0`.`HasSoulPatch`, `s0`.`SquadId0`
FROM `Gears` AS `g`
LEFT JOIN (
    SELECT `w`.`Id`, `g0`.`Nickname`, `g0`.`SquadId`, `s`.`Id` AS `Id0`, `g1`.`Nickname` AS `Nickname0`, `g1`.`HasSoulPatch`, `g1`.`SquadId` AS `SquadId0`, `w`.`OwnerFullName`
    FROM ((`Weapons` AS `w`
    LEFT JOIN `Gears` AS `g0` ON `w`.`OwnerFullName` = `g0`.`FullName`)
    LEFT JOIN `Squads` AS `s` ON `g0`.`SquadId` = `s`.`Id`)
    LEFT JOIN `Gears` AS `g1` ON `s`.`Id` = `g1`.`SquadId`
) AS `s0` ON `g`.`FullName` = `s0`.`OwnerFullName`
ORDER BY `g`.`FullName`, `g`.`Nickname` DESC, `g`.`SquadId`, `s0`.`Id`, `s0`.`Nickname`, `s0`.`SquadId`, `s0`.`Id0`, `s0`.`Nickname0`
""");
        }

        public override async Task Correlated_collections_with_funky_orderby_complex_scenario2(bool isAsync)
        {
            await base.Correlated_collections_with_funky_orderby_complex_scenario2(isAsync);

            AssertSql(
                """
SELECT `g`.`FullName`, `g`.`Nickname`, `g`.`SquadId`, `s1`.`FullName`, `s1`.`Nickname`, `s1`.`SquadId`, `s1`.`Id`, `s1`.`Nickname0`, `s1`.`SquadId0`, `s1`.`Id0`, `s1`.`Nickname00`, `s1`.`HasSoulPatch`, `s1`.`SquadId00`
FROM `Gears` AS `g`
LEFT JOIN (
    SELECT `g0`.`FullName`, `g0`.`Nickname`, `g0`.`SquadId`, `s0`.`Id`, `s0`.`Nickname` AS `Nickname0`, `s0`.`SquadId` AS `SquadId0`, `s0`.`Id0`, `s0`.`Nickname0` AS `Nickname00`, `s0`.`HasSoulPatch`, `s0`.`SquadId0` AS `SquadId00`, `g0`.`HasSoulPatch` AS `HasSoulPatch0`, `s0`.`IsAutomatic`, `s0`.`Name`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`
    FROM `Gears` AS `g0`
    LEFT JOIN (
        SELECT `w`.`Id`, `g1`.`Nickname`, `g1`.`SquadId`, `s`.`Id` AS `Id0`, `g2`.`Nickname` AS `Nickname0`, `g2`.`HasSoulPatch`, `g2`.`SquadId` AS `SquadId0`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`
        FROM ((`Weapons` AS `w`
        LEFT JOIN `Gears` AS `g1` ON `w`.`OwnerFullName` = `g1`.`FullName`)
        LEFT JOIN `Squads` AS `s` ON `g1`.`SquadId` = `s`.`Id`)
        LEFT JOIN `Gears` AS `g2` ON `s`.`Id` = `g2`.`SquadId`
    ) AS `s0` ON `g0`.`FullName` = `s0`.`OwnerFullName`
) AS `s1` ON `g`.`Nickname` = `s1`.`LeaderNickname` AND `g`.`SquadId` = `s1`.`LeaderSquadId`
WHERE `g`.`Discriminator` = 'Officer'
ORDER BY NOT (`g`.`HasSoulPatch`), `g`.`LeaderNickname`, `g`.`FullName`, `g`.`Nickname`, `g`.`SquadId`, `s1`.`FullName`, NOT (`s1`.`HasSoulPatch0`) DESC, `s1`.`Nickname`, `s1`.`SquadId`, NOT (`s1`.`IsAutomatic`), `s1`.`Name` DESC, `s1`.`Id`, `s1`.`Nickname0`, `s1`.`SquadId0`, `s1`.`Id0`, `s1`.`Nickname00`
""");
        }

        public override async Task Correlated_collection_with_top_level_FirstOrDefault(bool isAsync)
        {
            await base.Correlated_collection_with_top_level_FirstOrDefault(isAsync);

            AssertSql(
                """
SELECT `g0`.`Nickname`, `g0`.`SquadId`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM (
    SELECT TOP 1 `g`.`Nickname`, `g`.`SquadId`, `g`.`FullName`
    FROM `Gears` AS `g`
    ORDER BY `g`.`Nickname`
) AS `g0`
LEFT JOIN `Weapons` AS `w` ON `g0`.`FullName` = `w`.`OwnerFullName`
ORDER BY `g0`.`Nickname`, `g0`.`SquadId`
""");
        }

        public override async Task Correlated_collection_with_top_level_Count(bool isAsync)
        {
            await base.Correlated_collection_with_top_level_Count(isAsync);

            AssertSql(
                """
    SELECT COUNT(*)
    FROM `Gears` AS `g`
    """);
        }

        public override async Task Correlated_collection_with_top_level_Last_with_orderby_on_outer(bool isAsync)
        {
            await base.Correlated_collection_with_top_level_Last_with_orderby_on_outer(isAsync);

            AssertSql(
                """
SELECT `g0`.`Nickname`, `g0`.`SquadId`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM (
    SELECT TOP 1 `g`.`Nickname`, `g`.`SquadId`, `g`.`FullName`
    FROM `Gears` AS `g`
    ORDER BY `g`.`FullName`
) AS `g0`
LEFT JOIN `Weapons` AS `w` ON `g0`.`FullName` = `w`.`OwnerFullName`
ORDER BY `g0`.`FullName`, `g0`.`Nickname`, `g0`.`SquadId`
""");
        }

        public override async Task Correlated_collection_with_top_level_Last_with_order_by_on_inner(bool isAsync)
        {
            await base.Correlated_collection_with_top_level_Last_with_order_by_on_inner(isAsync);

            AssertSql(
                """
SELECT `g0`.`Nickname`, `g0`.`SquadId`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM (
    SELECT TOP 1 `g`.`Nickname`, `g`.`SquadId`, `g`.`FullName`
    FROM `Gears` AS `g`
    ORDER BY `g`.`FullName` DESC
) AS `g0`
LEFT JOIN `Weapons` AS `w` ON `g0`.`FullName` = `w`.`OwnerFullName`
ORDER BY `g0`.`FullName` DESC, `g0`.`Nickname`, `g0`.`SquadId`, `w`.`Name`
""");
        }

        public override async Task Null_semantics_on_nullable_bool_from_inner_join_subquery_is_fully_applied(bool isAsync)
        {
            await base.Null_semantics_on_nullable_bool_from_inner_join_subquery_is_fully_applied(isAsync);

            AssertSql(
                """
SELECT `f0`.`Id`, `f0`.`CapitalName`, `f0`.`Discriminator`, `f0`.`Name`, `f0`.`ServerAddress`, `f0`.`CommanderName`, `f0`.`DeputyCommanderName`, `f0`.`Eradicated`
FROM `LocustLeaders` AS `l`
INNER JOIN (
    SELECT `f`.`Id`, `f`.`CapitalName`, `f`.`Discriminator`, `f`.`Name`, `f`.`ServerAddress`, `f`.`CommanderName`, `f`.`DeputyCommanderName`, `f`.`Eradicated`
    FROM `Factions` AS `f`
    WHERE `f`.`Name` = 'Swarm'
) AS `f0` ON `l`.`Name` = `f0`.`CommanderName`
WHERE `f0`.`Eradicated` <> TRUE OR `f0`.`Eradicated` IS NULL
""");
        }

        public override async Task Null_semantics_on_nullable_bool_from_left_join_subquery_is_fully_applied(bool isAsync)
        {
            await base.Null_semantics_on_nullable_bool_from_left_join_subquery_is_fully_applied(isAsync);

            AssertSql(
                """
SELECT `f0`.`Id`, `f0`.`CapitalName`, `f0`.`Discriminator`, `f0`.`Name`, `f0`.`ServerAddress`, `f0`.`CommanderName`, `f0`.`DeputyCommanderName`, `f0`.`Eradicated`
FROM `LocustLeaders` AS `l`
LEFT JOIN (
    SELECT `f`.`Id`, `f`.`CapitalName`, `f`.`Discriminator`, `f`.`Name`, `f`.`ServerAddress`, `f`.`CommanderName`, `f`.`DeputyCommanderName`, `f`.`Eradicated`
    FROM `Factions` AS `f`
    WHERE `f`.`Name` = 'Swarm'
) AS `f0` ON `l`.`Name` = `f0`.`CommanderName`
WHERE `f0`.`Eradicated` <> TRUE OR `f0`.`Eradicated` IS NULL
""");
        }

        public override async Task Include_on_derived_type_with_order_by_and_paging(bool isAsync)
        {
            await base.Include_on_derived_type_with_order_by_and_paging(isAsync);

            AssertSql(
                """
SELECT `s`.`Name`, `s`.`Discriminator`, `s`.`LocustHordeId`, `s`.`ThreatLevel`, `s`.`ThreatLevelByte`, `s`.`ThreatLevelNullableByte`, `s`.`DefeatedByNickname`, `s`.`DefeatedBySquadId`, `s`.`HighCommandId`, `s`.`Nickname`, `s`.`SquadId`, `s`.`AssignedCityName`, `s`.`CityOfBirthName`, `s`.`Discriminator0`, `s`.`FullName`, `s`.`HasSoulPatch`, `s`.`LeaderNickname`, `s`.`LeaderSquadId`, `s`.`Rank`, `s`.`Id`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM (
    SELECT TOP @p `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`, `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator` AS `Discriminator0`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `t`.`Id`, `t`.`Note`
    FROM (`LocustLeaders` AS `l`
    LEFT JOIN `Gears` AS `g` ON `l`.`DefeatedByNickname` = `g`.`Nickname` AND `l`.`DefeatedBySquadId` = `g`.`SquadId`)
    LEFT JOIN `Tags` AS `t` ON (`g`.`Nickname` = `t`.`GearNickName` OR (`g`.`Nickname` IS NULL AND `t`.`GearNickName` IS NULL)) AND (`g`.`SquadId` = `t`.`GearSquadId` OR (`g`.`SquadId` IS NULL AND `t`.`GearSquadId` IS NULL))
    ORDER BY `t`.`Note`
) AS `s`
LEFT JOIN `Weapons` AS `w` ON `s`.`FullName` = `w`.`OwnerFullName`
ORDER BY `s`.`Note`, `s`.`Name`, `s`.`Nickname`, `s`.`SquadId`, `s`.`Id`
""");
        }

        public override async Task Select_required_navigation_on_derived_type(bool isAsync)
        {
            await base.Select_required_navigation_on_derived_type(isAsync);
            AssertSql(
"""
SELECT `l0`.`Name`
FROM `LocustLeaders` AS `l`
LEFT JOIN `LocustHighCommands` AS `l0` ON `l`.`HighCommandId` = `l0`.`Id`
""");
        }

        public override async Task Select_required_navigation_on_the_same_type_with_cast(bool isAsync)
        {
            await base.Select_required_navigation_on_the_same_type_with_cast(isAsync);

            AssertSql(
"""
SELECT `c`.`Name`
FROM `Gears` AS `g`
INNER JOIN `Cities` AS `c` ON `g`.`CityOfBirthName` = `c`.`Name`
""");
        }

        public override async Task Where_required_navigation_on_derived_type(bool isAsync)
        {
            await base.Where_required_navigation_on_derived_type(isAsync);

            AssertSql(
                """
    SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`
    FROM `LocustLeaders` AS `l`
    LEFT JOIN `LocustHighCommands` AS `l0` ON `l`.`HighCommandId` = `l0`.`Id`
    WHERE `l0`.`IsOperational` = TRUE
    """);
        }

        public override async Task Outer_parameter_in_join_key(bool isAsync)
        {
            await base.Outer_parameter_in_join_key(isAsync);

            AssertSql(
                $"""
                    SELECT `g`.`Nickname`, `g`.`SquadId`, `t1`.`Note`, `t1`.`Id`, `t1`.`Nickname`, `t1`.`SquadId`
                    FROM `Gears` AS `g`
                    OUTER APPLY (
                        SELECT `t`.`Note`, `t`.`Id`, `t0`.`Nickname`, `t0`.`SquadId`
                        FROM `Tags` AS `t`
                        INNER JOIN (
                            SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
                            FROM `Gears` AS `g0`
                            WHERE `g0`.`Discriminator` IN ('Gear', 'Officer')
                        ) AS `t0` ON `g`.`FullName` = `t0`.`FullName`
                    ) AS `t1`
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer') AND (`g`.`Discriminator` = 'Officer')
                    ORDER BY `g`.`Nickname`, `g`.`SquadId`, `t1`.`Id`, `t1`.`Nickname`, `t1`.`SquadId`
                    """);
        }

        public override async Task Outer_parameter_in_join_key_inner_and_outer(bool isAsync)
        {
            await base.Outer_parameter_in_join_key_inner_and_outer(isAsync);

            AssertSql(
                $"""
                    SELECT `g`.`Nickname`, `g`.`SquadId`, `t1`.`Note`, `t1`.`Id`, `t1`.`Nickname`, `t1`.`SquadId`
                    FROM `Gears` AS `g`
                    OUTER APPLY (
                        SELECT `t`.`Note`, `t`.`Id`, `t0`.`Nickname`, `t0`.`SquadId`
                        FROM `Tags` AS `t`
                        INNER JOIN (
                            SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
                            FROM `Gears` AS `g0`
                            WHERE `g0`.`Discriminator` IN ('Gear', 'Officer')
                        ) AS `t0` ON `g`.`FullName` = `g`.`Nickname`
                    ) AS `t1`
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer') AND (`g`.`Discriminator` = 'Officer')
                    ORDER BY `g`.`Nickname`, `g`.`SquadId`, `t1`.`Id`, `t1`.`Nickname`, `t1`.`SquadId`
                    """);
        }

        public override async Task Outer_parameter_in_group_join_with_DefaultIfEmpty(bool isAsync)
        {
            await base.Outer_parameter_in_group_join_with_DefaultIfEmpty(isAsync);

            AssertSql(
                $"""
                    SELECT `g`.`Nickname`, `g`.`SquadId`, `t1`.`Note`, `t1`.`Id`
                    FROM `Gears` AS `g`
                    OUTER APPLY (
                        SELECT `t`.`Note`, `t`.`Id`
                        FROM `Tags` AS `t`
                        LEFT JOIN (
                            SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
                            FROM `Gears` AS `g0`
                            WHERE `g0`.`Discriminator` IN ('Gear', 'Officer')
                        ) AS `t0` ON `g`.`FullName` = `t0`.`FullName`
                    ) AS `t1`
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer') AND (`g`.`Discriminator` = 'Officer')
                    ORDER BY `g`.`Nickname`, `g`.`SquadId`, `t1`.`Id`
                    """);
        }

        public override async Task Negated_bool_ternary_inside_anonymous_type_in_projection(bool isAsync)
        {
            await base.Negated_bool_ternary_inside_anonymous_type_in_projection(isAsync);

            AssertSql(
                """
SELECT IIF(`g`.`HasSoulPatch` = TRUE, TRUE, IIF(`g`.`HasSoulPatch` IS NULL, TRUE, `g`.`HasSoulPatch`)) BXOR TRUE AS `c`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
""");
        }

        public override async Task Order_by_entity_qsre(bool isAsync)
        {
            await base.Order_by_entity_qsre(isAsync);

            AssertSql(
"""
SELECT `g`.`FullName`
FROM `Gears` AS `g`
LEFT JOIN `Cities` AS `c` ON `g`.`AssignedCityName` = `c`.`Name`
ORDER BY `c`.`Name`, `g`.`Nickname` DESC
""");
        }

        public override async Task Order_by_entity_qsre_with_inheritance(bool isAsync)
        {
            await base.Order_by_entity_qsre_with_inheritance(isAsync);
            AssertSql(
                """
SELECT `l`.`Name`
FROM `LocustLeaders` AS `l`
INNER JOIN `LocustHighCommands` AS `l0` ON `l`.`HighCommandId` = `l0`.`Id`
WHERE `l`.`Discriminator` = 'LocustCommander'
ORDER BY `l0`.`Id`, `l`.`Name`
""");
        }

        public override async Task Order_by_entity_qsre_composite_key(bool isAsync)
        {
            await base.Order_by_entity_qsre_composite_key(isAsync);
            AssertSql(
                """
SELECT `w`.`Name`
FROM `Weapons` AS `w`
LEFT JOIN `Gears` AS `g` ON `w`.`OwnerFullName` = `g`.`FullName`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `w`.`Id`
""");
        }

        public override async Task Order_by_entity_qsre_with_other_orderbys(bool isAsync)
        {
            await base.Order_by_entity_qsre_with_other_orderbys(isAsync);

            AssertSql(
                """
SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM (`Weapons` AS `w`
LEFT JOIN `Gears` AS `g` ON `w`.`OwnerFullName` = `g`.`FullName`)
LEFT JOIN `Weapons` AS `w0` ON `w`.`SynergyWithId` = `w0`.`Id`
ORDER BY NOT (`w`.`IsAutomatic`), `g`.`Nickname` DESC, `g`.`SquadId` DESC, `w0`.`Id`, `w`.`Name`
""");
        }

        public override async Task Join_on_entity_qsre_keys(bool isAsync)
        {
            await base.Join_on_entity_qsre_keys(isAsync);

            AssertSql(
                $"""
                    SELECT `w`.`Name` AS `Name1`, `w0`.`Name` AS `Name2`
                    FROM `Weapons` AS `w`
                    INNER JOIN `Weapons` AS `w0` ON `w`.`Id` = `w0`.`Id`
                    """);
        }

        public override async Task Join_on_entity_qsre_keys_composite_key(bool isAsync)
        {
            await base.Join_on_entity_qsre_keys_composite_key(isAsync);

            AssertSql(
                """
SELECT `g`.`FullName` AS `GearName1`, `g0`.`FullName` AS `GearName2`
FROM `Gears` AS `g`
INNER JOIN `Gears` AS `g0` ON `g`.`Nickname` = `g0`.`Nickname` AND `g`.`SquadId` = `g0`.`SquadId`
""");
        }

        public override async Task Join_on_entity_qsre_keys_inheritance(bool isAsync)
        {
            await base.Join_on_entity_qsre_keys_inheritance(isAsync);

            AssertSql(
                """
SELECT `g`.`FullName` AS `GearName`, `g1`.`FullName` AS `OfficerName`
FROM `Gears` AS `g`
INNER JOIN (
    SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`FullName`
    FROM `Gears` AS `g0`
    WHERE `g0`.`Discriminator` = 'Officer'
) AS `g1` ON `g`.`Nickname` = `g1`.`Nickname` AND `g`.`SquadId` = `g1`.`SquadId`
""");
        }

        public override async Task Join_on_entity_qsre_keys_outer_key_is_navigation(bool isAsync)
        {
            await base.Join_on_entity_qsre_keys_outer_key_is_navigation(isAsync);

            AssertSql(
                """
SELECT `w`.`Name` AS `Name1`, `w1`.`Name` AS `Name2`
FROM (`Weapons` AS `w`
LEFT JOIN `Weapons` AS `w0` ON `w`.`SynergyWithId` = `w0`.`Id`)
LEFT JOIN `Weapons` AS `w1` ON `w0`.`Id` = `w1`.`Id`
WHERE `w0`.`Id` IS NOT NULL AND `w1`.`Id` IS NOT NULL
""");
        }

        public override async Task Join_on_entity_qsre_keys_inner_key_is_navigation(bool isAsync)
        {
            await base.Join_on_entity_qsre_keys_inner_key_is_navigation(isAsync);

            AssertSql(
                """
SELECT `c`.`Name` AS `CityName`, `s`.`Nickname` AS `GearNickname`
FROM `Cities` AS `c`
INNER JOIN (
    SELECT `g`.`Nickname`, `c0`.`Name`
    FROM `Gears` AS `g`
    LEFT JOIN `Cities` AS `c0` ON `g`.`AssignedCityName` = `c0`.`Name`
) AS `s` ON `c`.`Name` = `s`.`Name`
""");
        }

        public override async Task Join_on_entity_qsre_keys_inner_key_is_navigation_composite_key(bool isAsync)
        {
            await base.Join_on_entity_qsre_keys_inner_key_is_navigation_composite_key(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `s`.`Note`
FROM `Gears` AS `g`
INNER JOIN (
    SELECT `t`.`Note`, `g0`.`Nickname`, `g0`.`SquadId`
    FROM `Tags` AS `t`
    LEFT JOIN `Gears` AS `g0` ON `t`.`GearNickName` = `g0`.`Nickname` AND `t`.`GearSquadId` = `g0`.`SquadId`
    WHERE `t`.`Note` IN ('Cole''s Tag', 'Dom''s Tag')
) AS `s` ON `g`.`Nickname` = `s`.`Nickname` AND `g`.`SquadId` = `s`.`SquadId`
""");
        }

        public override async Task Join_on_entity_qsre_keys_inner_key_is_nested_navigation(bool isAsync)
        {
            await base.Join_on_entity_qsre_keys_inner_key_is_nested_navigation(isAsync);

            AssertSql(
                """
SELECT `s`.`Name` AS `SquadName`, `s1`.`Name` AS `WeaponName`
FROM `Squads` AS `s`
INNER JOIN (
    SELECT `w`.`Name`, `s0`.`Id` AS `Id0`
    FROM (`Weapons` AS `w`
    LEFT JOIN `Gears` AS `g` ON `w`.`OwnerFullName` = `g`.`FullName`)
    LEFT JOIN `Squads` AS `s0` ON `g`.`SquadId` = `s0`.`Id`
    WHERE `w`.`IsAutomatic` = TRUE
) AS `s1` ON `s`.`Id` = `s1`.`Id0`
""");
        }

        public override async Task GroupJoin_on_entity_qsre_keys_inner_key_is_nested_navigation(bool isAsync)
        {
            await base.GroupJoin_on_entity_qsre_keys_inner_key_is_nested_navigation(isAsync);

            AssertSql(
                """
SELECT `s`.`Name` AS `SquadName`, `s1`.`Name` AS `WeaponName`
FROM `Squads` AS `s`
LEFT JOIN (
    SELECT `w`.`Name`, `s0`.`Id` AS `Id0`
    FROM (`Weapons` AS `w`
    LEFT JOIN `Gears` AS `g` ON `w`.`OwnerFullName` = `g`.`FullName`)
    LEFT JOIN `Squads` AS `s0` ON `g`.`SquadId` = `s0`.`Id`
) AS `s1` ON `s`.`Id` = `s1`.`Id0`
""");
        }

        public override async Task Streaming_correlated_collection_issue_11403(bool isAsync)
        {
            await base.Streaming_correlated_collection_issue_11403(isAsync);
            AssertSql(
                """
SELECT `g0`.`Nickname`, `g0`.`SquadId`, `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
FROM (
    SELECT TOP 1 `g`.`Nickname`, `g`.`SquadId`, `g`.`FullName`
    FROM `Gears` AS `g`
    ORDER BY `g`.`Nickname`
) AS `g0`
LEFT JOIN (
    SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
    FROM `Weapons` AS `w`
    WHERE `w`.`IsAutomatic` = FALSE
) AS `w0` ON `g0`.`FullName` = `w0`.`OwnerFullName`
ORDER BY `g0`.`Nickname`, `g0`.`SquadId`, `w0`.`Id`
""");
        }

        public override async Task Project_one_value_type_from_empty_collection(bool isAsync)
        {
            await base.Project_one_value_type_from_empty_collection(isAsync);
            AssertSql(
                """
SELECT `s`.`Name`, IIF((
        SELECT TOP 1 `g`.`SquadId`
        FROM `Gears` AS `g`
        WHERE `s`.`Id` = `g`.`SquadId` AND `g`.`HasSoulPatch` = TRUE) IS NULL, 0, (
        SELECT TOP 1 `g`.`SquadId`
        FROM `Gears` AS `g`
        WHERE `s`.`Id` = `g`.`SquadId` AND `g`.`HasSoulPatch` = TRUE)) AS `SquadId`
FROM `Squads` AS `s`
WHERE `s`.`Name` = 'Kilo'
""");
        }

        public override async Task Project_one_value_type_converted_to_nullable_from_empty_collection(bool isAsync)
        {
            await base.Project_one_value_type_converted_to_nullable_from_empty_collection(isAsync);
            AssertSql(
                """
SELECT `s`.`Name`, (
    SELECT TOP 1 `g`.`SquadId`
    FROM `Gears` AS `g`
    WHERE `s`.`Id` = `g`.`SquadId` AND `g`.`HasSoulPatch` = TRUE) AS `SquadId`
FROM `Squads` AS `s`
WHERE `s`.`Name` = 'Kilo'
""");
        }

        public override async Task Project_one_value_type_with_client_projection_from_empty_collection(bool isAsync)
        {
            await base.Project_one_value_type_with_client_projection_from_empty_collection(isAsync);

            AssertSql(
                $"""
                    SELECT `s`.`Name`, `t0`.`SquadId`, `t0`.`LeaderSquadId`, `t0`.`c`
                    FROM `Squads` AS `s`
                    LEFT JOIN (
                        SELECT `t`.`SquadId`, `t`.`LeaderSquadId`, `t`.`c`, `t`.`Nickname`
                        FROM (
                            SELECT `g`.`SquadId`, `g`.`LeaderSquadId`, 1 AS `c`, `g`.`Nickname`, ROW_NUMBER() OVER(PARTITION BY `g`.`SquadId` ORDER BY `g`.`Nickname`, `g`.`SquadId`) AS `row`
                            FROM `Gears` AS `g`
                            WHERE `g`.`Discriminator` IN ('Gear', 'Officer') AND (`g`.`HasSoulPatch` = True)
                        ) AS `t`
                        WHERE `t`.`row` <= 1
                    ) AS `t0` ON `s`.`Id` = `t0`.`SquadId`
                    WHERE `s`.`Name` = 'Kilo'
                    """);
        }

        public override async Task Filter_on_subquery_projecting_one_value_type_from_empty_collection(bool isAsync)
        {
            await base.Filter_on_subquery_projecting_one_value_type_from_empty_collection(isAsync);

            AssertSql(
                """
    SELECT `s`.`Name`
    FROM `Squads` AS `s`
    WHERE `s`.`Name` = 'Kilo' AND IIF((
            SELECT TOP 1 `g`.`SquadId`
            FROM `Gears` AS `g`
            WHERE `s`.`Id` = `g`.`SquadId` AND `g`.`HasSoulPatch` = TRUE) IS NULL, 0, (
            SELECT TOP 1 `g`.`SquadId`
            FROM `Gears` AS `g`
            WHERE `s`.`Id` = `g`.`SquadId` AND `g`.`HasSoulPatch` = TRUE)) <> 0
    """);
        }

        public override async Task Select_subquery_projecting_single_constant_int(bool isAsync)
        {
            await base.Select_subquery_projecting_single_constant_int(isAsync);

            AssertSql(
                """
SELECT `s`.`Name`, IIF((
        SELECT TOP 1 42
        FROM `Gears` AS `g`
        WHERE `s`.`Id` = `g`.`SquadId` AND `g`.`HasSoulPatch` = TRUE) IS NULL, 0, (
        SELECT TOP 1 42
        FROM `Gears` AS `g`
        WHERE `s`.`Id` = `g`.`SquadId` AND `g`.`HasSoulPatch` = TRUE)) AS `Gear`
FROM `Squads` AS `s`
""");
        }

        public override async Task Select_subquery_projecting_single_constant_string(bool isAsync)
        {
            await base.Select_subquery_projecting_single_constant_string(isAsync);
            AssertSql(
                """
SELECT `s`.`Name`, (
    SELECT TOP 1 'Foo'
    FROM `Gears` AS `g`
    WHERE `s`.`Id` = `g`.`SquadId` AND `g`.`HasSoulPatch` = TRUE) AS `Gear`
FROM `Squads` AS `s`
""");
        }

        public override async Task Select_subquery_projecting_single_constant_bool(bool isAsync)
        {
            await base.Select_subquery_projecting_single_constant_bool(isAsync);

            AssertSql(
                """
    SELECT `s`.`Name`, IIF((
            SELECT TOP 1 TRUE
            FROM `Gears` AS `g`
            WHERE `s`.`Id` = `g`.`SquadId` AND `g`.`HasSoulPatch` = TRUE) IS NULL, FALSE, (
            SELECT TOP 1 TRUE
            FROM `Gears` AS `g`
            WHERE `s`.`Id` = `g`.`SquadId` AND `g`.`HasSoulPatch` = TRUE)) AS `Gear`
    FROM `Squads` AS `s`
    """);
        }

        public override async Task Select_subquery_projecting_single_constant_inside_anonymous(bool isAsync)
        {
            await base.Select_subquery_projecting_single_constant_inside_anonymous(isAsync);

            AssertSql(
                $"""
                    SELECT `s`.`Name`, `t0`.`c`
                    FROM `Squads` AS `s`
                    LEFT JOIN (
                        SELECT `t`.`c`, `t`.`Nickname`, `t`.`SquadId`
                        FROM (
                            SELECT 1 AS `c`, `g`.`Nickname`, `g`.`SquadId`, ROW_NUMBER() OVER(PARTITION BY `g`.`SquadId` ORDER BY `g`.`Nickname`, `g`.`SquadId`) AS `row`
                            FROM `Gears` AS `g`
                            WHERE `g`.`Discriminator` IN ('Gear', 'Officer') AND (`g`.`HasSoulPatch` = True)
                        ) AS `t`
                        WHERE `t`.`row` <= 1
                    ) AS `t0` ON `s`.`Id` = `t0`.`SquadId`
                    """);
        }

        public override async Task Select_subquery_projecting_multiple_constants_inside_anonymous(bool isAsync)
        {
            await base.Select_subquery_projecting_multiple_constants_inside_anonymous(isAsync);

            AssertSql(
                $"""
                    SELECT `s`.`Name`, `t0`.`c`, `t0`.`c0`, `t0`.`c1`
                    FROM `Squads` AS `s`
                    LEFT JOIN (
                        SELECT `t`.`c`, `t`.`c0`, `t`.`c1`, `t`.`Nickname`, `t`.`SquadId`
                        FROM (
                            SELECT True AS `c`, False AS `c0`, 1 AS `c1`, `g`.`Nickname`, `g`.`SquadId`, ROW_NUMBER() OVER(PARTITION BY `g`.`SquadId` ORDER BY `g`.`Nickname`, `g`.`SquadId`) AS `row`
                            FROM `Gears` AS `g`
                            WHERE `g`.`Discriminator` IN ('Gear', 'Officer') AND (`g`.`HasSoulPatch` = True)
                        ) AS `t`
                        WHERE `t`.`row` <= 1
                    ) AS `t0` ON `s`.`Id` = `t0`.`SquadId`
                    """);
        }

        public override async Task Include_with_order_by_constant(bool isAsync)
        {
            await base.Include_with_order_by_constant(isAsync);
            AssertSql(
                """
SELECT `s`.`Id`, `s`.`Banner`, `s`.`Banner5`, `s`.`InternalNumber`, `s`.`Name`, `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Squads` AS `s`
LEFT JOIN `Gears` AS `g` ON `s`.`Id` = `g`.`SquadId`
ORDER BY `s`.`Id`, `g`.`Nickname`
""");
        }

        public override async Task Correlated_collection_order_by_constant(bool isAsync)
        {
            await base.Correlated_collection_order_by_constant(isAsync);

            AssertSql(
                """
    SELECT `g`.`Nickname`, `g`.`SquadId`, `w`.`Name`, `w`.`Id`
    FROM `Gears` AS `g`
    LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`
    ORDER BY `g`.`Nickname`, `g`.`SquadId`
    """);
        }

        public override async Task Select_subquery_projecting_single_constant_null_of_non_mapped_type(bool isAsync)
        {
            await base.Select_subquery_projecting_single_constant_null_of_non_mapped_type(isAsync);

            AssertSql(
                $"""
                    SELECT `s`.`Name`, `t0`.`c`
                    FROM `Squads` AS `s`
                    LEFT JOIN (
                        SELECT `t`.`c`, `t`.`Nickname`, `t`.`SquadId`
                        FROM (
                            SELECT 1 AS `c`, `g`.`Nickname`, `g`.`SquadId`, ROW_NUMBER() OVER(PARTITION BY `g`.`SquadId` ORDER BY `g`.`Nickname`, `g`.`SquadId`) AS `row`
                            FROM `Gears` AS `g`
                            WHERE `g`.`Discriminator` IN ('Gear', 'Officer') AND (`g`.`HasSoulPatch` = True)
                        ) AS `t`
                        WHERE `t`.`row` <= 1
                    ) AS `t0` ON `s`.`Id` = `t0`.`SquadId`
                    """);
        }

        public override async Task Select_subquery_projecting_single_constant_of_non_mapped_type(bool isAsync)
        {
            await base.Select_subquery_projecting_single_constant_of_non_mapped_type(isAsync);

            AssertSql(
                $"""
                    SELECT `s`.`Name`, `t0`.`c`
                    FROM `Squads` AS `s`
                    LEFT JOIN (
                        SELECT `t`.`c`, `t`.`Nickname`, `t`.`SquadId`
                        FROM (
                            SELECT 1 AS `c`, `g`.`Nickname`, `g`.`SquadId`, ROW_NUMBER() OVER(PARTITION BY `g`.`SquadId` ORDER BY `g`.`Nickname`, `g`.`SquadId`) AS `row`
                            FROM `Gears` AS `g`
                            WHERE `g`.`Discriminator` IN ('Gear', 'Officer') AND (`g`.`HasSoulPatch` = True)
                        ) AS `t`
                        WHERE `t`.`row` <= 1
                    ) AS `t0` ON `s`.`Id` = `t0`.`SquadId`
                    """);
        }

        public override async Task Include_collection_OrderBy_aggregate(bool isAsync)
        {
            await base.Include_collection_OrderBy_aggregate(isAsync);

            AssertSql(
                """
SELECT `s`.`Nickname`, `s`.`SquadId`, `s`.`AssignedCityName`, `s`.`CityOfBirthName`, `s`.`Discriminator`, `s`.`FullName`, `s`.`HasSoulPatch`, `s`.`LeaderNickname`, `s`.`LeaderSquadId`, `s`.`Rank`, `s`.`Nickname0`, `s`.`SquadId0`, `s`.`AssignedCityName0`, `s`.`CityOfBirthName0`, `s`.`Discriminator0`, `s`.`FullName0`, `s`.`HasSoulPatch0`, `s`.`LeaderNickname0`, `s`.`LeaderSquadId0`, `s`.`Rank0`, `s`.`c`
FROM (
    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `g0`.`Nickname` AS `Nickname0`, `g0`.`SquadId` AS `SquadId0`, `g0`.`AssignedCityName` AS `AssignedCityName0`, `g0`.`CityOfBirthName` AS `CityOfBirthName0`, `g0`.`Discriminator` AS `Discriminator0`, `g0`.`FullName` AS `FullName0`, `g0`.`HasSoulPatch` AS `HasSoulPatch0`, `g0`.`LeaderNickname` AS `LeaderNickname0`, `g0`.`LeaderSquadId` AS `LeaderSquadId0`, `g0`.`Rank` AS `Rank0`, (
        SELECT COUNT(*)
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName`) AS `c`
    FROM `Gears` AS `g`
    LEFT JOIN `Gears` AS `g0` ON `g`.`Nickname` = `g0`.`LeaderNickname` AND `g`.`SquadId` = `g0`.`LeaderSquadId`
    WHERE `g`.`Discriminator` = 'Officer'
) AS `s`
ORDER BY `s`.`c`, `s`.`Nickname`, `s`.`SquadId`, `s`.`Nickname0`
""");
        }

        public override async Task Include_collection_with_complex_OrderBy2(bool isAsync)
        {
            await base.Include_collection_with_complex_OrderBy2(isAsync);

            AssertSql(
                """
SELECT `s`.`Nickname`, `s`.`SquadId`, `s`.`AssignedCityName`, `s`.`CityOfBirthName`, `s`.`Discriminator`, `s`.`FullName`, `s`.`HasSoulPatch`, `s`.`LeaderNickname`, `s`.`LeaderSquadId`, `s`.`Rank`, `s`.`Nickname0`, `s`.`SquadId0`, `s`.`AssignedCityName0`, `s`.`CityOfBirthName0`, `s`.`Discriminator0`, `s`.`FullName0`, `s`.`HasSoulPatch0`, `s`.`LeaderNickname0`, `s`.`LeaderSquadId0`, `s`.`Rank0`, `s`.`c`
FROM (
    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `g0`.`Nickname` AS `Nickname0`, `g0`.`SquadId` AS `SquadId0`, `g0`.`AssignedCityName` AS `AssignedCityName0`, `g0`.`CityOfBirthName` AS `CityOfBirthName0`, `g0`.`Discriminator` AS `Discriminator0`, `g0`.`FullName` AS `FullName0`, `g0`.`HasSoulPatch` AS `HasSoulPatch0`, `g0`.`LeaderNickname` AS `LeaderNickname0`, `g0`.`LeaderSquadId` AS `LeaderSquadId0`, `g0`.`Rank` AS `Rank0`, (
        SELECT TOP 1 `w`.`IsAutomatic`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName`
        ORDER BY `w`.`Id`) AS `c`
    FROM `Gears` AS `g`
    LEFT JOIN `Gears` AS `g0` ON `g`.`Nickname` = `g0`.`LeaderNickname` AND `g`.`SquadId` = `g0`.`LeaderSquadId`
    WHERE `g`.`Discriminator` = 'Officer'
) AS `s`
ORDER BY NOT (`s`.`c`), `s`.`Nickname`, `s`.`SquadId`, `s`.`Nickname0`
""");
        }

        public override async Task Include_collection_with_complex_OrderBy3(bool isAsync)
        {
            await base.Include_collection_with_complex_OrderBy3(isAsync);

            AssertSql(
                """
SELECT `s`.`Nickname`, `s`.`SquadId`, `s`.`AssignedCityName`, `s`.`CityOfBirthName`, `s`.`Discriminator`, `s`.`FullName`, `s`.`HasSoulPatch`, `s`.`LeaderNickname`, `s`.`LeaderSquadId`, `s`.`Rank`, `s`.`Nickname0`, `s`.`SquadId0`, `s`.`AssignedCityName0`, `s`.`CityOfBirthName0`, `s`.`Discriminator0`, `s`.`FullName0`, `s`.`HasSoulPatch0`, `s`.`LeaderNickname0`, `s`.`LeaderSquadId0`, `s`.`Rank0`, `s`.`c`
FROM (
    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `g0`.`Nickname` AS `Nickname0`, `g0`.`SquadId` AS `SquadId0`, `g0`.`AssignedCityName` AS `AssignedCityName0`, `g0`.`CityOfBirthName` AS `CityOfBirthName0`, `g0`.`Discriminator` AS `Discriminator0`, `g0`.`FullName` AS `FullName0`, `g0`.`HasSoulPatch` AS `HasSoulPatch0`, `g0`.`LeaderNickname` AS `LeaderNickname0`, `g0`.`LeaderSquadId` AS `LeaderSquadId0`, `g0`.`Rank` AS `Rank0`, IIF((
            SELECT TOP 1 `w`.`IsAutomatic`
            FROM `Weapons` AS `w`
            WHERE `g`.`FullName` = `w`.`OwnerFullName`
            ORDER BY `w`.`Id`) IS NULL, FALSE, (
            SELECT TOP 1 `w`.`IsAutomatic`
            FROM `Weapons` AS `w`
            WHERE `g`.`FullName` = `w`.`OwnerFullName`
            ORDER BY `w`.`Id`)) AS `c`
    FROM `Gears` AS `g`
    LEFT JOIN `Gears` AS `g0` ON `g`.`Nickname` = `g0`.`LeaderNickname` AND `g`.`SquadId` = `g0`.`LeaderSquadId`
    WHERE `g`.`Discriminator` = 'Officer'
) AS `s`
ORDER BY NOT (`s`.`c`), `s`.`Nickname`, `s`.`SquadId`, `s`.`Nickname0`
""");
        }

        public override async Task Correlated_collection_with_complex_OrderBy(bool isAsync)
        {
            await base.Correlated_collection_with_complex_OrderBy(isAsync);

            AssertSql(
                """
SELECT `s`.`Nickname`, `s`.`SquadId`, `s`.`Nickname0`, `s`.`SquadId0`, `s`.`AssignedCityName`, `s`.`CityOfBirthName`, `s`.`Discriminator`, `s`.`FullName`, `s`.`HasSoulPatch`, `s`.`LeaderNickname`, `s`.`LeaderSquadId`, `s`.`Rank`, `s`.`c`
FROM (
    SELECT `g`.`Nickname`, `g`.`SquadId`, `g1`.`Nickname` AS `Nickname0`, `g1`.`SquadId` AS `SquadId0`, `g1`.`AssignedCityName`, `g1`.`CityOfBirthName`, `g1`.`Discriminator`, `g1`.`FullName`, `g1`.`HasSoulPatch`, `g1`.`LeaderNickname`, `g1`.`LeaderSquadId`, `g1`.`Rank`, (
        SELECT COUNT(*)
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName`) AS `c`
    FROM `Gears` AS `g`
    LEFT JOIN (
        SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
        FROM `Gears` AS `g0`
        WHERE `g0`.`HasSoulPatch` = FALSE
    ) AS `g1` ON `g`.`Nickname` = `g1`.`LeaderNickname` AND `g`.`SquadId` = `g1`.`LeaderSquadId`
    WHERE `g`.`Discriminator` = 'Officer'
) AS `s`
ORDER BY `s`.`c`, `s`.`Nickname`, `s`.`SquadId`, `s`.`Nickname0`
""");
        }

        public override async Task Correlated_collection_with_very_complex_order_by(bool isAsync)
        {
            await base.Correlated_collection_with_very_complex_order_by(isAsync);

            AssertSql(
                """
SELECT `s`.`Nickname`, `s`.`SquadId`, `s`.`Nickname0`, `s`.`SquadId0`, `s`.`AssignedCityName`, `s`.`CityOfBirthName`, `s`.`Discriminator`, `s`.`FullName`, `s`.`HasSoulPatch`, `s`.`LeaderNickname`, `s`.`LeaderSquadId`, `s`.`Rank`, `s`.`c`
FROM (
    SELECT `g`.`Nickname`, `g`.`SquadId`, `g2`.`Nickname` AS `Nickname0`, `g2`.`SquadId` AS `SquadId0`, `g2`.`AssignedCityName`, `g2`.`CityOfBirthName`, `g2`.`Discriminator`, `g2`.`FullName`, `g2`.`HasSoulPatch`, `g2`.`LeaderNickname`, `g2`.`LeaderSquadId`, `g2`.`Rank`, (
        SELECT COUNT(*)
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName` AND `w`.`IsAutomatic` = IIF((
                SELECT TOP 1 `g0`.`HasSoulPatch`
                FROM `Gears` AS `g0`
                WHERE `g0`.`Nickname` = 'Marcus') IS NULL, FALSE, (
                SELECT TOP 1 `g0`.`HasSoulPatch`
                FROM `Gears` AS `g0`
                WHERE `g0`.`Nickname` = 'Marcus'))) AS `c`
    FROM `Gears` AS `g`
    LEFT JOIN (
        SELECT `g1`.`Nickname`, `g1`.`SquadId`, `g1`.`AssignedCityName`, `g1`.`CityOfBirthName`, `g1`.`Discriminator`, `g1`.`FullName`, `g1`.`HasSoulPatch`, `g1`.`LeaderNickname`, `g1`.`LeaderSquadId`, `g1`.`Rank`
        FROM `Gears` AS `g1`
        WHERE `g1`.`HasSoulPatch` = FALSE
    ) AS `g2` ON `g`.`Nickname` = `g2`.`LeaderNickname` AND `g`.`SquadId` = `g2`.`LeaderSquadId`
    WHERE `g`.`Discriminator` = 'Officer'
) AS `s`
ORDER BY `s`.`c`, `s`.`Nickname`, `s`.`SquadId`, `s`.`Nickname0`
""");
        }

        public override async Task Cast_to_derived_type_after_OfType_works(bool isAsync)
        {
            await base.Cast_to_derived_type_after_OfType_works(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE `g`.`Discriminator` = 'Officer'
""");
        }

        public override async Task Select_subquery_boolean(bool isAsync)
        {
            await base.Select_subquery_boolean(isAsync);
            AssertSql(
"""
SELECT IIF((
        SELECT TOP 1 `w`.`IsAutomatic`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName`
        ORDER BY `w`.`Id`) IS NULL, FALSE, (
        SELECT TOP 1 `w`.`IsAutomatic`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName`
        ORDER BY `w`.`Id`))
FROM `Gears` AS `g`
""");
        }

        public override async Task Select_subquery_boolean_with_pushdown(bool isAsync)
        {
            await base.Select_subquery_boolean_with_pushdown(isAsync);
            AssertSql(
                """
SELECT (
    SELECT TOP 1 `w`.`IsAutomatic`
    FROM `Weapons` AS `w`
    WHERE `g`.`FullName` = `w`.`OwnerFullName`
    ORDER BY `w`.`Id`)
FROM `Gears` AS `g`
""");
        }

        public override async Task Select_subquery_int_with_inside_cast_and_coalesce(bool isAsync)
        {
            await base.Select_subquery_int_with_inside_cast_and_coalesce(isAsync);

            AssertSql(
"""
SELECT IIF((
        SELECT TOP 1 `w`.`Id`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName`
        ORDER BY `w`.`Id`) IS NULL, 42, (
        SELECT TOP 1 `w`.`Id`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName`
        ORDER BY `w`.`Id`))
FROM `Gears` AS `g`
""");
        }

        public override async Task Select_subquery_int_with_outside_cast_and_coalesce(bool isAsync)
        {
            await base.Select_subquery_int_with_outside_cast_and_coalesce(isAsync);

            AssertSql(
                """
SELECT IIF((
        SELECT TOP 1 `w`.`Id`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName`
        ORDER BY `w`.`Id`) IS NULL, 0, (
        SELECT TOP 1 `w`.`Id`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName`
        ORDER BY `w`.`Id`))
FROM `Gears` AS `g`
""");
        }

        public override async Task Select_subquery_int_with_pushdown_and_coalesce(bool isAsync)
        {
            await base.Select_subquery_int_with_pushdown_and_coalesce(isAsync);

            AssertSql(
"""
SELECT IIF((
        SELECT TOP 1 `w`.`Id`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName`
        ORDER BY `w`.`Id`) IS NULL, 42, (
        SELECT TOP 1 `w`.`Id`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName`
        ORDER BY `w`.`Id`))
FROM `Gears` AS `g`
""");
        }

        public override async Task Select_subquery_int_with_pushdown_and_coalesce2(bool isAsync)
        {
            await base.Select_subquery_int_with_pushdown_and_coalesce2(isAsync);

            AssertSql(
"""
SELECT IIF((
        SELECT TOP 1 `w`.`Id`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName`
        ORDER BY `w`.`Id`) IS NULL, (
        SELECT TOP 1 `w0`.`Id`
        FROM `Weapons` AS `w0`
        WHERE `g`.`FullName` = `w0`.`OwnerFullName`
        ORDER BY `w0`.`Id`), (
        SELECT TOP 1 `w`.`Id`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName`
        ORDER BY `w`.`Id`))
FROM `Gears` AS `g`
""");
        }

        public override async Task Select_subquery_boolean_empty(bool isAsync)
        {
            await base.Select_subquery_boolean_empty(isAsync);
            AssertSql(
                """
SELECT IIF((
        SELECT TOP 1 `w`.`IsAutomatic`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName` AND `w`.`Name` = 'BFG'
        ORDER BY `w`.`Id`) IS NULL, FALSE, (
        SELECT TOP 1 `w`.`IsAutomatic`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName` AND `w`.`Name` = 'BFG'
        ORDER BY `w`.`Id`))
FROM `Gears` AS `g`
""");
        }

        public override async Task Select_subquery_boolean_empty_with_pushdown(bool isAsync)
        {
            await base.Select_subquery_boolean_empty_with_pushdown(isAsync);

            AssertSql(
"""
SELECT (
    SELECT TOP 1 `w`.`IsAutomatic`
    FROM `Weapons` AS `w`
    WHERE `g`.`FullName` = `w`.`OwnerFullName` AND `w`.`Name` = 'BFG'
    ORDER BY `w`.`Id`)
FROM `Gears` AS `g`
""");
        }

        public override async Task Select_subquery_distinct_singleordefault_boolean1(bool isAsync)
        {
            await base.Select_subquery_distinct_singleordefault_boolean1(isAsync);

            AssertSql(
                $"""
                    SELECT (
                        SELECT TOP 1 `t`.`IsAutomatic`
                        FROM (
                            SELECT DISTINCT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
                            FROM `Weapons` AS `w`
                            WHERE (`g`.`FullName` = `w`.`OwnerFullName`) AND (CHARINDEX('Lancer', `w`.`Name`) > 0)
                        ) AS `t`)
                    FROM `Gears` AS `g`
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer') AND (`g`.`HasSoulPatch` = True)
                    """);
        }

        public override async Task Select_subquery_distinct_singleordefault_boolean2(bool isAsync)
        {
            await base.Select_subquery_distinct_singleordefault_boolean2(isAsync);
            AssertSql(
                """
SELECT IIF((
        SELECT TOP 1 `w`.`IsAutomatic`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName` AND (`w`.`Name` LIKE '%Lancer%')) IS NULL, FALSE, (
        SELECT TOP 1 `w`.`IsAutomatic`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName` AND (`w`.`Name` LIKE '%Lancer%')))
FROM `Gears` AS `g`
WHERE `g`.`HasSoulPatch` = TRUE
""");
        }

        public override async Task Select_subquery_distinct_singleordefault_boolean_with_pushdown(bool isAsync)
        {
            await base.Select_subquery_distinct_singleordefault_boolean_with_pushdown(isAsync);
            AssertSql(
                """
SELECT IIF((
        SELECT DISTINCT TOP 1 `w`.`IsAutomatic`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName` AND (`w`.`Name` LIKE '%Lancer%')) IS NULL, FALSE, (
        SELECT DISTINCT TOP 1 `w`.`IsAutomatic`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName` AND (`w`.`Name` LIKE '%Lancer%')))
FROM `Gears` AS `g`
WHERE `g`.`HasSoulPatch` = TRUE
""");
        }

        public override async Task Select_subquery_distinct_singleordefault_boolean_empty1(bool isAsync)
        {
            await base.Select_subquery_distinct_singleordefault_boolean_empty1(isAsync);

            AssertSql(
                $"""
                    SELECT (
                        SELECT TOP 1 `t`.`IsAutomatic`
                        FROM (
                            SELECT DISTINCT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
                            FROM `Weapons` AS `w`
                            WHERE (`g`.`FullName` = `w`.`OwnerFullName`) AND (`w`.`Name` = 'BFG')
                        ) AS `t`)
                    FROM `Gears` AS `g`
                    WHERE `g`.`Discriminator` IN ('Gear', 'Officer') AND (`g`.`HasSoulPatch` = True)
                    """);
        }

        public override async Task Select_subquery_distinct_singleordefault_boolean_empty2(bool isAsync)
        {
            await base.Select_subquery_distinct_singleordefault_boolean_empty2(isAsync);

            AssertSql(
                """
SELECT IIF((
        SELECT TOP 1 `w`.`IsAutomatic`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName` AND `w`.`Name` = 'BFG') IS NULL, FALSE, (
        SELECT TOP 1 `w`.`IsAutomatic`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName` AND `w`.`Name` = 'BFG'))
FROM `Gears` AS `g`
WHERE `g`.`HasSoulPatch` = TRUE
""");
        }

        public override async Task Select_subquery_distinct_singleordefault_boolean_empty_with_pushdown(bool isAsync)
        {
            await base.Select_subquery_distinct_singleordefault_boolean_empty_with_pushdown(isAsync);

            AssertSql(
"""
SELECT IIF((
        SELECT DISTINCT TOP 1 `w`.`IsAutomatic`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName` AND `w`.`Name` = 'BFG') IS NULL, FALSE, (
        SELECT DISTINCT TOP 1 `w`.`IsAutomatic`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName` AND `w`.`Name` = 'BFG'))
FROM `Gears` AS `g`
WHERE `g`.`HasSoulPatch` = TRUE
""");
        }

        public override async Task Cast_subquery_to_base_type_using_typed_ToList(bool isAsync)
        {
            await base.Cast_subquery_to_base_type_using_typed_ToList(isAsync);

            AssertSql(
                """
    SELECT `c`.`Name`, `g`.`CityOfBirthName`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Nickname`, `g`.`Rank`, `g`.`SquadId`
    FROM `Cities` AS `c`
    LEFT JOIN `Gears` AS `g` ON `c`.`Name` = `g`.`AssignedCityName`
    WHERE `c`.`Name` = 'Ephyra'
    ORDER BY `c`.`Name`, `g`.`Nickname`
    """);
        }

        public override async Task Cast_ordered_subquery_to_base_type_using_typed_ToArray(bool isAsync)
        {
            await base.Cast_ordered_subquery_to_base_type_using_typed_ToArray(isAsync);

            AssertSql(
                """
    SELECT `c`.`Name`, `g`.`CityOfBirthName`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Nickname`, `g`.`Rank`, `g`.`SquadId`
    FROM `Cities` AS `c`
    LEFT JOIN `Gears` AS `g` ON `c`.`Name` = `g`.`AssignedCityName`
    WHERE `c`.`Name` = 'Ephyra'
    ORDER BY `c`.`Name`, `g`.`Nickname` DESC
    """);
        }

        public override async Task Correlated_collection_with_complex_order_by_funcletized_to_constant_bool(bool isAsync)
        {
            await base.Correlated_collection_with_complex_order_by_funcletized_to_constant_bool(isAsync);

            AssertSql(
                """
    SELECT `g`.`Nickname`, `g`.`SquadId`, `w`.`Name`, `w`.`Id`
    FROM `Gears` AS `g`
    LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`
    ORDER BY `g`.`Nickname`, `g`.`SquadId`
    """);
        }

        public override async Task Double_order_by_on_nullable_bool_coming_from_optional_navigation(bool isAsync)
        {
            await base.Double_order_by_on_nullable_bool_coming_from_optional_navigation(isAsync);

            AssertSql(
                """
SELECT `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
FROM `Weapons` AS `w`
LEFT JOIN `Weapons` AS `w0` ON `w`.`SynergyWithId` = `w0`.`Id`
ORDER BY NOT (`w0`.`IsAutomatic`), `w0`.`Id`
""");
        }

        public override async Task Double_order_by_on_Like(bool isAsync)
        {
            await base.Double_order_by_on_Like(isAsync);

            AssertSql(
                """
SELECT `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
FROM `Weapons` AS `w`
LEFT JOIN `Weapons` AS `w0` ON `w`.`SynergyWithId` = `w0`.`Id`
ORDER BY NOT (IIF((`w0`.`Name` LIKE '%Lancer') AND `w0`.`Name` IS NOT NULL, TRUE, FALSE))
""");
        }

        public override async Task Double_order_by_on_is_null(bool isAsync)
        {
            await base.Double_order_by_on_is_null(isAsync);

            AssertSql(
                """
SELECT `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
FROM `Weapons` AS `w`
LEFT JOIN `Weapons` AS `w0` ON `w`.`SynergyWithId` = `w0`.`Id`
ORDER BY NOT (IIF(`w0`.`Name` IS NULL, TRUE, FALSE))
""");
        }

        public override async Task Double_order_by_on_string_compare(bool isAsync)
        {
            await base.Double_order_by_on_string_compare(isAsync);

            AssertSql(
"""
SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM `Weapons` AS `w`
ORDER BY NOT (IIF(`w`.`Name` = 'Marcus'' Lancer' AND `w`.`Name` IS NOT NULL, TRUE, FALSE)), `w`.`Id`
""");
        }

        public override async Task Double_order_by_binary_expression(bool isAsync)
        {
            await base.Double_order_by_binary_expression(isAsync);

            AssertSql(
                """
SELECT `w0`.`Binary`
FROM (
    SELECT `w`.`Id` + 2 AS `Binary`
    FROM `Weapons` AS `w`
) AS `w0`
ORDER BY `w0`.`Binary`
""");
        }

        public override async Task String_compare_with_null_conditional_argument(bool isAsync)
        {
            await base.String_compare_with_null_conditional_argument(isAsync);

            AssertSql(
"""
SELECT `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
FROM `Weapons` AS `w`
LEFT JOIN `Weapons` AS `w0` ON `w`.`SynergyWithId` = `w0`.`Id`
ORDER BY NOT (IIF(`w0`.`Name` = 'Marcus'' Lancer' AND `w0`.`Name` IS NOT NULL, TRUE, FALSE))
""");
        }

        public override async Task String_compare_with_null_conditional_argument2(bool isAsync)
        {
            await base.String_compare_with_null_conditional_argument2(isAsync);

            AssertSql(
"""
SELECT `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
FROM `Weapons` AS `w`
LEFT JOIN `Weapons` AS `w0` ON `w`.`SynergyWithId` = `w0`.`Id`
ORDER BY NOT (IIF('Marcus'' Lancer' = `w0`.`Name` AND `w0`.`Name` IS NOT NULL, TRUE, FALSE))
""");
        }

        public override async Task String_concat_with_null_conditional_argument(bool isAsync)
        {
            await base.String_concat_with_null_conditional_argument(isAsync);

            AssertSql(
"""
SELECT `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
FROM `Weapons` AS `w`
LEFT JOIN `Weapons` AS `w0` ON `w`.`SynergyWithId` = `w0`.`Id`
ORDER BY IIF(`w0`.`Name` IS NULL, '', `w0`.`Name`) & (5 & '')
""");
        }

        public override async Task String_concat_with_null_conditional_argument2(bool isAsync)
        {
            await base.String_concat_with_null_conditional_argument2(isAsync);

            AssertSql(
"""
SELECT `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
FROM `Weapons` AS `w`
LEFT JOIN `Weapons` AS `w0` ON `w`.`SynergyWithId` = `w0`.`Id`
ORDER BY IIF(`w0`.`Name` IS NULL, '', `w0`.`Name`) & 'Marcus'' Lancer'
""");
        }

        public override async Task String_concat_on_various_types(bool isAsync)
        {
            await base.String_concat_on_various_types(isAsync);

            AssertSql(
                """
SELECT ('HasSoulPatch ' & (`g`.`HasSoulPatch` & '')) & ' HasSoulPatch' AS `HasSoulPatch`, ('Rank ' & (`g`.`Rank` & '')) & ' Rank' AS `Rank`, ('SquadId ' & (`g`.`SquadId` & '')) & ' SquadId' AS `SquadId`, ('Rating ' & IIF((`m`.`Rating` & '') IS NULL, '', (`m`.`Rating` & ''))) & ' Rating' AS `Rating`, ('Timeline ' & (`m`.`Timeline` & '')) & ' Timeline' AS `Timeline`
FROM `Gears` AS `g`,
`Missions` AS `m`
ORDER BY `g`.`Nickname`, `m`.`Id`
""");
        }

        public override async Task GroupBy_Property_Include_Select_Average(bool isAsync)
        {
            await base.GroupBy_Property_Include_Select_Average(isAsync);

            AssertSql(
"""
SELECT AVG(CDBL(`g`.`SquadId`))
FROM `Gears` AS `g`
GROUP BY `g`.`Rank`
""");
        }

        public override async Task GroupBy_Property_Include_Select_Sum(bool isAsync)
        {
            await base.GroupBy_Property_Include_Select_Sum(isAsync);

            AssertSql(
"""
SELECT IIF(SUM(`g`.`SquadId`) IS NULL, 0, SUM(`g`.`SquadId`))
FROM `Gears` AS `g`
GROUP BY `g`.`Rank`
""");
        }

        public override async Task GroupBy_Property_Include_Select_Count(bool isAsync)
        {
            await base.GroupBy_Property_Include_Select_Count(isAsync);

            AssertSql(
"""
SELECT COUNT(*)
FROM `Gears` AS `g`
GROUP BY `g`.`Rank`
""");
        }

        public override async Task GroupBy_Property_Include_Select_LongCount(bool isAsync)
        {
            await base.GroupBy_Property_Include_Select_LongCount(isAsync);

            AssertSql(
                """
    SELECT COUNT(*)
    FROM `Gears` AS `g`
    GROUP BY `g`.`Rank`
    """);
        }

        public override async Task GroupBy_Property_Include_Select_Min(bool isAsync)
        {
            await base.GroupBy_Property_Include_Select_Min(isAsync);

            AssertSql(
                """
    SELECT MIN(`g`.`SquadId`)
    FROM `Gears` AS `g`
    GROUP BY `g`.`Rank`
    """);
        }

        public override async Task GroupBy_Property_Include_Aggregate_with_anonymous_selector(bool isAsync)
        {
            await base.GroupBy_Property_Include_Aggregate_with_anonymous_selector(isAsync);

            AssertSql(
"""
SELECT `g`.`Nickname` AS `Key`, COUNT(*) AS `c`
FROM `Gears` AS `g`
GROUP BY `g`.`Nickname`
ORDER BY `g`.`Nickname`
""");
        }

        public override async Task Group_by_with_include_with_entity_in_result_selector(bool isAsync)
        {
            await base.Group_by_with_include_with_entity_in_result_selector(isAsync);

            AssertSql(
                $"""
                    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `g.CityOfBirth`.`Name`, `g.CityOfBirth`.`Location`, `g.CityOfBirth`.`Nation`
                    FROM `Gears` AS `g`
                    INNER JOIN `Cities` AS `g.CityOfBirth` ON `g`.`CityOfBirthName` = `g.CityOfBirth`.`Name`
                    WHERE `g`.`Discriminator` IN ('Officer', 'Gear')
                    ORDER BY `g`.`Rank`
                    """);
        }

        public override async Task GroupBy_Property_Include_Select_Max(bool isAsync)
        {
            await base.GroupBy_Property_Include_Select_Max(isAsync);

            AssertSql(
                """
    SELECT MAX(`g`.`SquadId`)
    FROM `Gears` AS `g`
    GROUP BY `g`.`Rank`
    """);
        }

        public override async Task Include_with_group_by_and_FirstOrDefault_gets_properly_applied(bool isAsync)
        {
            await base.Include_with_group_by_and_FirstOrDefault_gets_properly_applied(isAsync);

            AssertSql(
                $"""
                    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `g.CityOfBirth`.`Name`, `g.CityOfBirth`.`Location`, `g.CityOfBirth`.`Nation`
                    FROM `Gears` AS `g`
                    INNER JOIN `Cities` AS `g.CityOfBirth` ON `g`.`CityOfBirthName` = `g.CityOfBirth`.`Name`
                    WHERE `g`.`Discriminator` IN ('Officer', 'Gear')
                    ORDER BY `g`.`Rank`
                    """);
        }

        public override async Task Include_collection_with_Cast_to_base(bool isAsync)
        {
            await base.Include_collection_with_Cast_to_base(isAsync);

            AssertSql(
                """
    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
    FROM `Gears` AS `g`
    LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`
    WHERE `g`.`Discriminator` = 'Officer'
    ORDER BY `g`.`Nickname`, `g`.`SquadId`
    """);
        }

        public override async Task Include_with_client_method_and_member_access_still_applies_includes(bool isAsync)
        {
            await base.Include_with_client_method_and_member_access_still_applies_includes(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`
FROM `Gears` AS `g`
LEFT JOIN `Tags` AS `t` ON `g`.`Nickname` = `t`.`GearNickName` AND `g`.`SquadId` = `t`.`GearSquadId`
""");
        }

        public override async Task Include_with_projection_of_unmapped_property_still_gets_applied(bool isAsync)
        {
            await base.Include_with_projection_of_unmapped_property_still_gets_applied(isAsync);

            AssertSql(
                """
    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
    FROM `Gears` AS `g`
    LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`
    ORDER BY `g`.`Nickname`, `g`.`SquadId`
    """);
        }

        public override async Task Multiple_includes_with_client_method_around_entity_and_also_projecting_included_collection()
        {
            await base.Multiple_includes_with_client_method_around_entity_and_also_projecting_included_collection();

            AssertSql(
                """
SELECT `s`.`Name`, `s`.`Id`, `s`.`Banner`, `s`.`Banner5`, `s`.`InternalNumber`, `s0`.`Nickname`, `s0`.`SquadId`, `s0`.`AssignedCityName`, `s0`.`CityOfBirthName`, `s0`.`Discriminator`, `s0`.`FullName`, `s0`.`HasSoulPatch`, `s0`.`LeaderNickname`, `s0`.`LeaderSquadId`, `s0`.`Rank`, `s0`.`Id`, `s0`.`AmmunitionType`, `s0`.`IsAutomatic`, `s0`.`Name`, `s0`.`OwnerFullName`, `s0`.`SynergyWithId`
FROM `Squads` AS `s`
LEFT JOIN (
    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
    FROM `Gears` AS `g`
    LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`
) AS `s0` ON `s`.`Id` = `s0`.`SquadId`
WHERE `s`.`Name` = 'Delta'
ORDER BY `s`.`Id`, `s0`.`Nickname`, `s0`.`SquadId`
""");
        }

        public override async Task OrderBy_same_expression_containing_IsNull_correctly_deduplicates_the_ordering(bool isAsync)
        {
            await base.OrderBy_same_expression_containing_IsNull_correctly_deduplicates_the_ordering(isAsync);

            AssertSql(
                """
SELECT IIF(`g`.`LeaderNickname` IS NOT NULL, CBOOL(IIF(LEN(`g`.`Nickname`) IS NULL, NULL, CLNG(LEN(`g`.`Nickname`))) BXOR 5) BXOR TRUE, NULL)
FROM `Gears` AS `g`
ORDER BY NOT (IIF(`g`.`LeaderNickname` IS NOT NULL, TRUE, FALSE))
""");

        }

        public override async Task GetValueOrDefault_in_projection(bool isAsync)
        {
            await base.GetValueOrDefault_in_projection(isAsync);

            AssertSql(
                $"""
                    SELECT IIF(`w`.`SynergyWithId` IS NULL, 0, `w`.`SynergyWithId`)
                    FROM `Weapons` AS `w`
                    """);
        }

        public override async Task GetValueOrDefault_in_filter(bool isAsync)
        {
            await base.GetValueOrDefault_in_filter(isAsync);

            AssertSql(
                $"""
                    SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
                    FROM `Weapons` AS `w`
                    WHERE IIF(`w`.`SynergyWithId` IS NULL, 0, `w`.`SynergyWithId`) = 0
                    """);
        }

        public override async Task GetValueOrDefault_in_filter_non_nullable_column(bool isAsync)
        {
            await base.GetValueOrDefault_in_filter_non_nullable_column(isAsync);

            AssertSql(
                """
SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM `Weapons` AS `w`
WHERE `w`.`Id` = 0
""");
        }

        public override async Task GetValueOrDefault_in_order_by(bool isAsync)
        {
            await base.GetValueOrDefault_in_order_by(isAsync);

            AssertSql(
                $"""
                    SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
                    FROM `Weapons` AS `w`
                    ORDER BY IIF(`w`.`SynergyWithId` IS NULL, 0, `w`.`SynergyWithId`), `w`.`Id`
                    """);
        }

        public override async Task GetValueOrDefault_with_argument(bool isAsync)
        {
            await base.GetValueOrDefault_with_argument(isAsync);

            AssertSql(
                """
    SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
    FROM `Weapons` AS `w`
    WHERE IIF(`w`.`SynergyWithId` IS NULL, `w`.`Id`, `w`.`SynergyWithId`) = 1
    """);
        }

        public override async Task GetValueOrDefault_with_argument_complex(bool isAsync)
        {
            await base.GetValueOrDefault_with_argument_complex(isAsync);

            AssertSql(
                """
    SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
    FROM `Weapons` AS `w`
    WHERE IIF(`w`.`SynergyWithId` IS NULL, IIF(LEN(`w`.`Name`) IS NULL, NULL, CLNG(LEN(`w`.`Name`))) + 42, `w`.`SynergyWithId`) > 10
    """);
        }

        public override async Task Filter_with_complex_predicate_containing_subquery(bool isAsync)
        {
            await base.Filter_with_complex_predicate_containing_subquery(isAsync);

            AssertSql(
                """
    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
    FROM `Gears` AS `g`
    WHERE `g`.`FullName` <> 'Dom' AND EXISTS (
        SELECT 1
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName` AND `w`.`IsAutomatic` = TRUE)
    """);
        }

        public override async Task Query_with_complex_let_containing_ordering_and_filter_projecting_firstOrDefault_element_of_let(
            bool isAsync)
        {
            await base.Query_with_complex_let_containing_ordering_and_filter_projecting_firstOrDefault_element_of_let(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, (
    SELECT TOP 1 `w`.`Name`
    FROM `Weapons` AS `w`
    WHERE `g`.`FullName` = `w`.`OwnerFullName` AND `w`.`IsAutomatic` = TRUE
    ORDER BY `w`.`AmmunitionType` DESC) AS `WeaponName`
FROM `Gears` AS `g`
WHERE `g`.`Nickname` <> 'Dom'
""");
        }

        public override async Task
            Null_semantics_is_correctly_applied_for_function_comparisons_that_take_arguments_from_optional_navigation(bool isAsync)
        {
            await base.Null_semantics_is_correctly_applied_for_function_comparisons_that_take_arguments_from_optional_navigation(isAsync);

            AssertSql(
"""
SELECT `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE IIF(`g`.`SquadId` IS NULL, NULL, MID(`t`.`Note`, 0 + 1, `g`.`SquadId`)) = `t`.`GearNickName` OR ((`t`.`Note` IS NULL OR `g`.`SquadId` IS NULL) AND `t`.`GearNickName` IS NULL)
""");
        }

        public override async Task
            Null_semantics_is_correctly_applied_for_function_comparisons_that_take_arguments_from_optional_navigation_complex(bool isAsync)
        {
            await base.Null_semantics_is_correctly_applied_for_function_comparisons_that_take_arguments_from_optional_navigation_complex(
                isAsync);

            AssertSql(
"""
SELECT `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`
FROM (`Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`)
LEFT JOIN `Squads` AS `s` ON `g`.`SquadId` = `s`.`Id`
WHERE IIF(LEN(`s`.`Name`) IS NULL, NULL, MID(`t`.`Note`, 0 + 1, IIF(LEN(`s`.`Name`) IS NULL, NULL, CLNG(LEN(`s`.`Name`))))) = `t`.`GearNickName` OR ((`t`.`Note` IS NULL OR `s`.`Name` IS NULL) AND `t`.`GearNickName` IS NULL)
""");
        }

        public override async Task OfTypeNav1(bool isAsync)
        {
            await base.OfTypeNav1(isAsync);

            AssertSql(
"""
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM (`Gears` AS `g`
LEFT JOIN `Tags` AS `t` ON `g`.`Nickname` = `t`.`GearNickName` AND `g`.`SquadId` = `t`.`GearSquadId`)
LEFT JOIN `Tags` AS `t0` ON `g`.`Nickname` = `t0`.`GearNickName` AND `g`.`SquadId` = `t0`.`GearSquadId`
WHERE (`t`.`Note` <> 'Foo' OR `t`.`Note` IS NULL) AND `g`.`Discriminator` = 'Officer' AND (`t0`.`Note` <> 'Bar' OR `t0`.`Note` IS NULL)
""");
        }

        public override async Task OfTypeNav2(bool isAsync)
        {
            await base.OfTypeNav2(isAsync);

            AssertSql(
"""
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM (`Gears` AS `g`
LEFT JOIN `Tags` AS `t` ON `g`.`Nickname` = `t`.`GearNickName` AND `g`.`SquadId` = `t`.`GearSquadId`)
LEFT JOIN `Cities` AS `c` ON `g`.`AssignedCityName` = `c`.`Name`
WHERE (`t`.`Note` <> 'Foo' OR `t`.`Note` IS NULL) AND `g`.`Discriminator` = 'Officer' AND (`c`.`Location` <> 'Bar' OR `c`.`Location` IS NULL)
""");
        }

        public override async Task OfTypeNav3(bool isAsync)
        {
            await base.OfTypeNav3(isAsync);

            AssertSql(
"""
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM ((`Gears` AS `g`
LEFT JOIN `Tags` AS `t` ON `g`.`Nickname` = `t`.`GearNickName` AND `g`.`SquadId` = `t`.`GearSquadId`)
INNER JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`)
LEFT JOIN `Tags` AS `t0` ON `g`.`Nickname` = `t0`.`GearNickName` AND `g`.`SquadId` = `t0`.`GearSquadId`
WHERE (`t`.`Note` <> 'Foo' OR `t`.`Note` IS NULL) AND `g`.`Discriminator` = 'Officer' AND (`t0`.`Note` <> 'Bar' OR `t0`.`Note` IS NULL)
""");
        }

        public override async Task Nav_rewrite_Distinct_with_convert()
        {
            await base.Nav_rewrite_Distinct_with_convert();

            AssertSql();
        }

        public override async Task Nav_rewrite_Distinct_with_convert_anonymous()
        {
            await base.Nav_rewrite_Distinct_with_convert_anonymous();

            AssertSql();
        }

        public override async Task Nav_rewrite_with_convert1(bool isAsync)
        {
            await base.Nav_rewrite_with_convert1(isAsync);

            AssertSql(
                """
SELECT `l0`.`Name`, `l0`.`Discriminator`, `l0`.`LocustHordeId`, `l0`.`ThreatLevel`, `l0`.`ThreatLevelByte`, `l0`.`ThreatLevelNullableByte`, `l0`.`DefeatedByNickname`, `l0`.`DefeatedBySquadId`, `l0`.`HighCommandId`
FROM (`Factions` AS `f`
LEFT JOIN `Cities` AS `c` ON `f`.`CapitalName` = `c`.`Name`)
LEFT JOIN (
    SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`
    FROM `LocustLeaders` AS `l`
    WHERE `l`.`Discriminator` = 'LocustCommander'
) AS `l0` ON `f`.`CommanderName` = `l0`.`Name`
WHERE `c`.`Name` <> 'Foo' OR `c`.`Name` IS NULL
""");
        }

        public override async Task Nav_rewrite_with_convert2(bool isAsync)
        {
            await base.Nav_rewrite_with_convert2(isAsync);

            AssertSql(
                """
SELECT `f`.`Id`, `f`.`CapitalName`, `f`.`Discriminator`, `f`.`Name`, `f`.`ServerAddress`, `f`.`CommanderName`, `f`.`DeputyCommanderName`, `f`.`Eradicated`
FROM (`Factions` AS `f`
LEFT JOIN `Cities` AS `c` ON `f`.`CapitalName` = `c`.`Name`)
LEFT JOIN (
    SELECT `l`.`Name`
    FROM `LocustLeaders` AS `l`
    WHERE `l`.`Discriminator` = 'LocustCommander'
) AS `l0` ON `f`.`CommanderName` = `l0`.`Name`
WHERE (`c`.`Name` <> 'Foo' OR `c`.`Name` IS NULL) AND (`l0`.`Name` <> 'Bar' OR `l0`.`Name` IS NULL)
""");
        }

        public override async Task Nav_rewrite_with_convert3(bool isAsync)
        {
            await base.Nav_rewrite_with_convert3(isAsync);

            AssertSql(
                """
SELECT `f`.`Id`, `f`.`CapitalName`, `f`.`Discriminator`, `f`.`Name`, `f`.`ServerAddress`, `f`.`CommanderName`, `f`.`DeputyCommanderName`, `f`.`Eradicated`
FROM (`Factions` AS `f`
LEFT JOIN `Cities` AS `c` ON `f`.`CapitalName` = `c`.`Name`)
LEFT JOIN (
    SELECT `l`.`Name`
    FROM `LocustLeaders` AS `l`
    WHERE `l`.`Discriminator` = 'LocustCommander'
) AS `l0` ON `f`.`CommanderName` = `l0`.`Name`
WHERE (`c`.`Name` <> 'Foo' OR `c`.`Name` IS NULL) AND (`l0`.`Name` <> 'Bar' OR `l0`.`Name` IS NULL)
""");
        }

        public override async Task Where_contains_on_navigation_with_composite_keys(bool isAsync)
        {
            await base.Where_contains_on_navigation_with_composite_keys(isAsync);
            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE EXISTS (
    SELECT 1
    FROM `Cities` AS `c`
    WHERE EXISTS (
        SELECT 1
        FROM `Gears` AS `g0`
        WHERE `c`.`Name` = `g0`.`CityOfBirthName` AND `g0`.`Nickname` = `g`.`Nickname` AND `g0`.`SquadId` = `g`.`SquadId`))
""");
        }

        public override async Task Include_with_complex_order_by(bool isAsync)
        {
            await base.Include_with_complex_order_by(isAsync);

            AssertSql(
                """
SELECT `s`.`Nickname`, `s`.`SquadId`, `s`.`AssignedCityName`, `s`.`CityOfBirthName`, `s`.`Discriminator`, `s`.`FullName`, `s`.`HasSoulPatch`, `s`.`LeaderNickname`, `s`.`LeaderSquadId`, `s`.`Rank`, `s`.`Id`, `s`.`AmmunitionType`, `s`.`IsAutomatic`, `s`.`Name`, `s`.`OwnerFullName`, `s`.`SynergyWithId`, `s`.`c`
FROM (
    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`, (
        SELECT TOP 1 `w`.`Name`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName` AND (`w`.`Name` LIKE '%Gnasher%')) AS `c`
    FROM `Gears` AS `g`
    LEFT JOIN `Weapons` AS `w0` ON `g`.`FullName` = `w0`.`OwnerFullName`
) AS `s`
ORDER BY `s`.`c`, `s`.`Nickname`, `s`.`SquadId`
""");
        }

        public override async Task Anonymous_projection_take_followed_by_projecting_single_element_from_collection_navigation(bool isAsync)
        {
            await base.Anonymous_projection_take_followed_by_projecting_single_element_from_collection_navigation(isAsync);

            AssertSql(
                $@"");
        }

        public override async Task Bool_projection_from_subquery_treated_appropriately_in_where(bool isAsync)
        {
            await base.Bool_projection_from_subquery_treated_appropriately_in_where(isAsync);

            AssertSql(
                """
    SELECT `c`.`Name`, `c`.`Location`, `c`.`Nation`
    FROM `Cities` AS `c`
    WHERE (
        SELECT TOP 1 `g`.`HasSoulPatch`
        FROM `Gears` AS `g`
        ORDER BY `g`.`Nickname`, `g`.`SquadId`) = TRUE
    """);
        }

        public override async Task DateTimeOffset_Contains_Less_than_Greater_than(bool isAsync)
        {
            var dto = JetTestHelpers.GetExpectedValue(new DateTimeOffset(599898024001234567, new TimeSpan(1, 30, 0)));
            var start = dto.AddDays(-1);
            var end = dto.AddDays(1);
            var dates = new[] { dto };

            await AssertQuery(
                isAsync,
                ss => ss.Set<Mission>().Where(
                    m => start <= m.Timeline.Date && m.Timeline < end && dates.Contains(m.Timeline)));

            AssertSql(
                """
@start='1902-01-01T08:30:00.0000000Z' (DbType = DateTime)
@end='1902-01-03T08:30:00.0000000Z' (DbType = DateTime)

SELECT `m`.`Id`, `m`.`CodeName`, `m`.`Date`, `m`.`Difficulty`, `m`.`Duration`, `m`.`Rating`, `m`.`Time`, `m`.`Timeline`
FROM `Missions` AS `m`
WHERE @start <= DATEVALUE(`m`.`Timeline`) AND `m`.`Timeline` < @end AND `m`.`Timeline` = CDATE('1902-01-02 08:30:00')
""");
        }

        public override Task DateTimeOffsetNow_minus_timespan(bool async)
            => AssertTranslationFailed(() => base.DateTimeOffsetNow_minus_timespan(async));

        public override async Task Navigation_inside_interpolated_string_expanded(bool isAsync)
        {
            await base.Navigation_inside_interpolated_string_expanded(isAsync);

            AssertSql(
"""
SELECT IIF(`w`.`SynergyWithId` IS NOT NULL, TRUE, FALSE), `w0`.`OwnerFullName`
FROM `Weapons` AS `w`
LEFT JOIN `Weapons` AS `w0` ON `w`.`SynergyWithId` = `w0`.`Id`
""");
        }

        public override async Task Left_join_projection_using_coalesce_tracking(bool isAsync)
        {
            await base.Left_join_projection_using_coalesce_tracking(isAsync);

            AssertSql(
                """
    SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`, `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
    FROM `Gears` AS `g`
    LEFT JOIN `Gears` AS `g0` ON `g`.`LeaderNickname` = `g0`.`Nickname`
    """);
        }

        public override async Task Left_join_projection_using_conditional_tracking(bool isAsync)
        {
            await base.Left_join_projection_using_conditional_tracking(isAsync);

            AssertSql(
"""
SELECT IIF(`g0`.`Nickname` IS NULL OR `g0`.`SquadId` IS NULL, TRUE, FALSE), `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
FROM `Gears` AS `g`
LEFT JOIN `Gears` AS `g0` ON `g`.`LeaderNickname` = `g0`.`Nickname`
""");
        }

        public override async Task Project_collection_navigation_nested_with_take_composite_key(bool isAsync)
        {
            await base.Project_collection_navigation_nested_with_take_composite_key(isAsync);

            AssertSql(
                $"""
                    SELECT `t`.`Id`, `t1`.`Nickname`, `t1`.`SquadId`, `t1`.`AssignedCityName`, `t1`.`CityOfBirthName`, `t1`.`Discriminator`, `t1`.`FullName`, `t1`.`HasSoulPatch`, `t1`.`LeaderNickname`, `t1`.`LeaderSquadId`, `t1`.`Rank`
                    FROM `Tags` AS `t`
                    LEFT JOIN (
                        SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
                        FROM `Gears` AS `g`
                        WHERE `g`.`Discriminator` IN ('Gear', 'Officer')
                    ) AS `t0` ON (`t`.`GearNickName` = `t0`.`Nickname`) AND (`t`.`GearSquadId` = `t0`.`SquadId`)
                    OUTER APPLY (
                        SELECT TOP 50 `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
                        FROM `Gears` AS `g0`
                        WHERE `g0`.`Discriminator` IN ('Gear', 'Officer') AND (`t0`.`Nickname` IS NOT NULL AND ((`t0`.`Nickname` = `g0`.`LeaderNickname`) AND (`t0`.`SquadId` = `g0`.`LeaderSquadId`)))
                    ) AS `t1`
                    WHERE `t0`.`Discriminator` = 'Officer'
                    ORDER BY `t`.`Id`, `t1`.`Nickname`, `t1`.`SquadId`
                    """);
        }

        public override async Task Project_collection_navigation_nested_composite_key(bool isAsync)
        {
            await base.Project_collection_navigation_nested_composite_key(isAsync);

            AssertSql(
"""
SELECT `t`.`Id`, `g`.`Nickname`, `g`.`SquadId`, `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
FROM (`Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`)
LEFT JOIN `Gears` AS `g0` ON (`g`.`Nickname` = `g0`.`LeaderNickname` OR (`g`.`Nickname` IS NULL AND `g0`.`LeaderNickname` IS NULL)) AND `g`.`SquadId` = `g0`.`LeaderSquadId`
WHERE `g`.`Discriminator` = 'Officer'
ORDER BY `t`.`Id`, `g`.`Nickname`, `g`.`SquadId`, `g0`.`Nickname`
""");
        }

        public override async Task Null_checks_in_correlated_predicate_are_correctly_translated(bool isAsync)
        {
            await base.Null_checks_in_correlated_predicate_are_correctly_translated(isAsync);

            AssertSql(
                $"""
                    SELECT `t`.`Id`, `t0`.`Nickname`, `t0`.`SquadId`, `t0`.`AssignedCityName`, `t0`.`CityOfBirthName`, `t0`.`Discriminator`, `t0`.`FullName`, `t0`.`HasSoulPatch`, `t0`.`LeaderNickname`, `t0`.`LeaderSquadId`, `t0`.`Rank`
                    FROM `Tags` AS `t`
                    LEFT JOIN (
                        SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
                        FROM `Gears` AS `g`
                        WHERE `g`.`Discriminator` IN ('Gear', 'Officer')
                    ) AS `t0` ON (((`t`.`GearNickName` = `t0`.`Nickname`) AND (`t`.`GearSquadId` = `t0`.`SquadId`)) AND `t`.`Note` IS NOT NULL) AND `t`.`Note` IS NOT NULL
                    ORDER BY `t`.`Id`, `t0`.`Nickname`, `t0`.`SquadId`
                    """);
        }

        public override async Task SelectMany_Where_DefaultIfEmpty_with_navigation_in_the_collection_selector(bool isAsync)
        {
            await base.SelectMany_Where_DefaultIfEmpty_with_navigation_in_the_collection_selector(isAsync);

            AssertSql(
                """
@isAutomatic='True'

SELECT `g`.`Nickname`, `g`.`FullName`, IIF(`w0`.`Id` IS NOT NULL, TRUE, FALSE) AS `Collection`
FROM `Gears` AS `g`
LEFT JOIN (
    SELECT `w`.`Id`, `w`.`OwnerFullName`
    FROM `Weapons` AS `w`
    WHERE `w`.`IsAutomatic` = @isAutomatic
) AS `w0` ON `g`.`FullName` = `w0`.`OwnerFullName`
""");
        }

        public override async Task SelectMany_Where_DefaultIfEmpty_with_navigation_in_the_collection_selector_not_equal(bool async)
        {
            await base.SelectMany_Where_DefaultIfEmpty_with_navigation_in_the_collection_selector_not_equal(async);

            AssertSql(
                """
@isAutomatic='True'

SELECT `g`.`Nickname`, `g`.`FullName`, IIF(`w0`.`Id` IS NOT NULL, TRUE, FALSE) AS `Collection`
FROM `Gears` AS `g`
LEFT JOIN (
    SELECT `w`.`Id`, `w`.`OwnerFullName`
    FROM `Weapons` AS `w`
    WHERE `w`.`IsAutomatic` <> @isAutomatic
) AS `w0` ON `g`.`FullName` = `w0`.`OwnerFullName`
""");
        }

        public override async Task SelectMany_Where_DefaultIfEmpty_with_navigation_in_the_collection_selector_order_comparison(bool async)
        {
            await base.SelectMany_Where_DefaultIfEmpty_with_navigation_in_the_collection_selector_order_comparison(async);

            AssertSql(
                """
@prm='1'

SELECT `g`.`Nickname`, `g`.`FullName`, IIF(`w0`.`Id` IS NOT NULL, TRUE, FALSE) AS `Collection`
FROM `Gears` AS `g`
LEFT JOIN (
    SELECT `w`.`Id`, `w`.`OwnerFullName`
    FROM `Weapons` AS `w`
    WHERE `w`.`Id` > @prm
) AS `w0` ON `g`.`FullName` = `w0`.`OwnerFullName`
""");
        }

        public override async Task Join_with_inner_being_a_subquery_projecting_single_property(bool isAsync)
        {
            await base.Join_with_inner_being_a_subquery_projecting_single_property(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
INNER JOIN `Gears` AS `g0` ON `g`.`Nickname` = `g0`.`Nickname`
""");
        }

        public override async Task Join_with_inner_being_a_subquery_projecting_anonymous_type_with_single_property(bool isAsync)
        {
            await base.Join_with_inner_being_a_subquery_projecting_anonymous_type_with_single_property(isAsync);

            AssertSql(
                """
    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
    FROM `Gears` AS `g`
    INNER JOIN `Gears` AS `g0` ON `g`.`Nickname` = `g0`.`Nickname`
    """);
        }

        public override async Task Navigation_based_on_complex_expression1(bool isAsync)
        {
            await base.Navigation_based_on_complex_expression1(isAsync);

            AssertSql(
                """
SELECT `f`.`Id`, `f`.`CapitalName`, `f`.`Discriminator`, `f`.`Name`, `f`.`ServerAddress`, `f`.`CommanderName`, `f`.`DeputyCommanderName`, `f`.`Eradicated`
FROM `Factions` AS `f`
LEFT JOIN (
    SELECT `l`.`Name`
    FROM `LocustLeaders` AS `l`
    WHERE `l`.`Discriminator` = 'LocustCommander'
) AS `l0` ON `f`.`CommanderName` = `l0`.`Name`
WHERE `l0`.`Name` IS NOT NULL
""");
        }

        public override async Task Navigation_based_on_complex_expression2(bool isAsync)
        {
            await base.Navigation_based_on_complex_expression2(isAsync);

            AssertSql(
                """
SELECT `f`.`Id`, `f`.`CapitalName`, `f`.`Discriminator`, `f`.`Name`, `f`.`ServerAddress`, `f`.`CommanderName`, `f`.`DeputyCommanderName`, `f`.`Eradicated`
FROM `Factions` AS `f`
LEFT JOIN (
    SELECT `l`.`Name`
    FROM `LocustLeaders` AS `l`
    WHERE `l`.`Discriminator` = 'LocustCommander'
) AS `l0` ON `f`.`CommanderName` = `l0`.`Name`
WHERE `l0`.`Name` IS NOT NULL
""");
        }

        public override async Task Navigation_based_on_complex_expression3(bool isAsync)
        {
            await base.Navigation_based_on_complex_expression3(isAsync);

            AssertSql(
                """
SELECT `l0`.`Name`, `l0`.`Discriminator`, `l0`.`LocustHordeId`, `l0`.`ThreatLevel`, `l0`.`ThreatLevelByte`, `l0`.`ThreatLevelNullableByte`, `l0`.`DefeatedByNickname`, `l0`.`DefeatedBySquadId`, `l0`.`HighCommandId`
FROM `Factions` AS `f`
LEFT JOIN (
    SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`
    FROM `LocustLeaders` AS `l`
    WHERE `l`.`Discriminator` = 'LocustCommander'
) AS `l0` ON `f`.`CommanderName` = `l0`.`Name`
""");
        }

        public override async Task Navigation_based_on_complex_expression4(bool isAsync)
        {
            await base.Navigation_based_on_complex_expression4(isAsync);

            AssertSql(
                """
SELECT TRUE, `l2`.`Name`, `l2`.`Discriminator`, `l2`.`LocustHordeId`, `l2`.`ThreatLevel`, `l2`.`ThreatLevelByte`, `l2`.`ThreatLevelNullableByte`, `l2`.`DefeatedByNickname`, `l2`.`DefeatedBySquadId`, `l2`.`HighCommandId`, `s`.`Name0`, `s`.`Discriminator0`, `s`.`LocustHordeId`, `s`.`ThreatLevel`, `s`.`ThreatLevelByte`, `s`.`ThreatLevelNullableByte`, `s`.`DefeatedByNickname`, `s`.`DefeatedBySquadId`, `s`.`HighCommandId`
FROM (
    SELECT `f`.`CommanderName`, `l0`.`Name` AS `Name0`, `l0`.`Discriminator` AS `Discriminator0`, `l0`.`LocustHordeId`, `l0`.`ThreatLevel`, `l0`.`ThreatLevelByte`, `l0`.`ThreatLevelNullableByte`, `l0`.`DefeatedByNickname`, `l0`.`DefeatedBySquadId`, `l0`.`HighCommandId`
    FROM `Factions` AS `f`,
    (
        SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`
        FROM `LocustLeaders` AS `l`
        WHERE `l`.`Discriminator` = 'LocustCommander'
    ) AS `l0`
) AS `s`
LEFT JOIN (
    SELECT `l1`.`Name`, `l1`.`Discriminator`, `l1`.`LocustHordeId`, `l1`.`ThreatLevel`, `l1`.`ThreatLevelByte`, `l1`.`ThreatLevelNullableByte`, `l1`.`DefeatedByNickname`, `l1`.`DefeatedBySquadId`, `l1`.`HighCommandId`
    FROM `LocustLeaders` AS `l1`
    WHERE `l1`.`Discriminator` = 'LocustCommander'
) AS `l2` ON `s`.`CommanderName` = `l2`.`Name`
""");
        }

        public override async Task Navigation_based_on_complex_expression5(bool isAsync)
        {
            await base.Navigation_based_on_complex_expression5(isAsync);

            AssertSql(
                $@"");
        }

        public override async Task Navigation_based_on_complex_expression6(bool isAsync)
        {
            await base.Navigation_based_on_complex_expression6(isAsync);

            AssertSql(
                $@"");
        }

        public override async Task Select_as_operator(bool isAsync)
        {
            await base.Select_as_operator(isAsync);

            AssertSql(
"""
SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`
FROM `LocustLeaders` AS `l`
""");
        }

        public override async Task Select_datetimeoffset_comparison_in_projection(bool isAsync)
        {
            await base.Select_datetimeoffset_comparison_in_projection(isAsync);

            AssertSql(
"""
SELECT IIF(`m`.`Timeline` > NOW(), TRUE, FALSE)
FROM `Missions` AS `m`
""");
        }

        public override async Task OfType_in_subquery_works(bool isAsync)
        {
            await base.OfType_in_subquery_works(isAsync);

            AssertSql(
                """
SELECT `s`.`Name`, `s`.`Location`, `s`.`Nation`
FROM `Gears` AS `g`
INNER JOIN (
    SELECT `c`.`Name`, `c`.`Location`, `c`.`Nation`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`
    FROM `Gears` AS `g0`
    LEFT JOIN `Cities` AS `c` ON `g0`.`AssignedCityName` = `c`.`Name`
    WHERE `g0`.`Discriminator` = 'Officer'
) AS `s` ON `g`.`Nickname` = `s`.`LeaderNickname` AND `g`.`SquadId` = `s`.`LeaderSquadId`
WHERE `g`.`Discriminator` = 'Officer'
""");
        }

        public override async Task Nullable_bool_comparison_is_translated_to_server(bool isAsync)
        {
            await base.Nullable_bool_comparison_is_translated_to_server(isAsync);

            AssertSql(
"""
SELECT IIF(`f`.`Eradicated` = TRUE AND `f`.`Eradicated` IS NOT NULL, TRUE, FALSE) AS `IsEradicated`
FROM `Factions` AS `f`
""");
        }
        public override async Task Accessing_reference_navigation_collection_composition_generates_single_query(bool isAsync)
        {
            await base.Accessing_reference_navigation_collection_composition_generates_single_query(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `s`.`Id`, `s`.`IsAutomatic`, `s`.`Name`, `s`.`Id0`
FROM `Gears` AS `g`
LEFT JOIN (
    SELECT `w`.`Id`, `w`.`IsAutomatic`, `w0`.`Name`, `w0`.`Id` AS `Id0`, `w`.`OwnerFullName`
    FROM `Weapons` AS `w`
    LEFT JOIN `Weapons` AS `w0` ON `w`.`SynergyWithId` = `w0`.`Id`
) AS `s` ON `g`.`FullName` = `s`.`OwnerFullName`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `s`.`Id`
""");
        }

        public override async Task Reference_include_chain_loads_correctly_when_middle_is_null(bool isAsync)
        {
            await base.Reference_include_chain_loads_correctly_when_middle_is_null(isAsync);

            AssertSql(
"""
SELECT `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`, `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `s`.`Id`, `s`.`Banner`, `s`.`Banner5`, `s`.`InternalNumber`, `s`.`Name`
FROM (`Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`)
LEFT JOIN `Squads` AS `s` ON `g`.`SquadId` = `s`.`Id`
ORDER BY `t`.`Note`
""");
        }

        public override async Task Accessing_property_of_optional_navigation_in_child_projection_works(bool isAsync)
        {
            await base.Accessing_property_of_optional_navigation_in_child_projection_works(isAsync);

            AssertSql(
                """
SELECT IIF(`g`.`Nickname` IS NOT NULL AND `g`.`SquadId` IS NOT NULL, TRUE, FALSE), `t`.`Id`, `g`.`Nickname`, `g`.`SquadId`, `s`.`Nickname`, `s`.`Id`, `s`.`SquadId`
FROM (`Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`)
LEFT JOIN (
    SELECT `g0`.`Nickname`, `w`.`Id`, `g0`.`SquadId`, `w`.`OwnerFullName`
    FROM `Weapons` AS `w`
    LEFT JOIN `Gears` AS `g0` ON `w`.`OwnerFullName` = `g0`.`FullName`
) AS `s` ON `g`.`FullName` = `s`.`OwnerFullName`
ORDER BY `t`.`Note`, `t`.`Id`, `g`.`Nickname`, `g`.`SquadId`, `s`.`Id`, `s`.`Nickname`
""");
        }

        public override async Task Collection_navigation_ofType_filter_works(bool isAsync)
        {
            await base.Collection_navigation_ofType_filter_works(isAsync);

            AssertSql(
                """
    SELECT `c`.`Name`, `c`.`Location`, `c`.`Nation`
    FROM `Cities` AS `c`
    WHERE EXISTS (
        SELECT 1
        FROM `Gears` AS `g`
        WHERE `c`.`Name` = `g`.`CityOfBirthName` AND `g`.`Discriminator` = 'Officer' AND `g`.`Nickname` = 'Marcus')
    """);
        }

        public override async Task Query_reusing_parameter_doesnt_declare_duplicate_parameter(bool isAsync)
        {
            await base.Query_reusing_parameter_doesnt_declare_duplicate_parameter(isAsync);

            AssertSql(
                """
@prm_Inner_Nickname='Marcus' (Size = 255)

SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
FROM (
    SELECT DISTINCT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
    FROM `Gears` AS `g`
    WHERE `g`.`Nickname` <> @prm_Inner_Nickname
) AS `g0`
ORDER BY `g0`.`FullName`
""");
        }

        public override async Task Query_reusing_parameter_with_inner_query_doesnt_declare_duplicate_parameter(bool async)
        {
            await base.Query_reusing_parameter_with_inner_query_doesnt_declare_duplicate_parameter(async);

            AssertSql(
                """
@squadId='1'
@squadId='1'

SELECT `u`.`Nickname`, `u`.`SquadId`, `u`.`AssignedCityName`, `u`.`CityOfBirthName`, `u`.`Discriminator`, `u`.`FullName`, `u`.`HasSoulPatch`, `u`.`LeaderNickname`, `u`.`LeaderSquadId`, `u`.`Rank`
FROM (
    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
    FROM `Gears` AS `g`
    INNER JOIN `Squads` AS `s` ON `g`.`SquadId` = `s`.`Id`
    WHERE `s`.`Id` IN (
        SELECT `s0`.`Id`
        FROM `Squads` AS `s0`
        WHERE `s0`.`Id` = @squadId
    )
    UNION ALL
    SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
    FROM `Gears` AS `g0`
    INNER JOIN `Squads` AS `s1` ON `g0`.`SquadId` = `s1`.`Id`
    WHERE `s1`.`Id` IN (
        SELECT `s2`.`Id`
        FROM `Squads` AS `s2`
        WHERE `s2`.`Id` = @squadId
    )
) AS `u`
ORDER BY `u`.`FullName`
""");
        }

        public override async Task Query_reusing_parameter_with_inner_query_expression_doesnt_declare_duplicate_parameter(bool async)
        {
            await base.Query_reusing_parameter_with_inner_query_expression_doesnt_declare_duplicate_parameter(async);

            AssertSql(
                """
@gearId='1'
@gearId='1'

SELECT `s`.`Id`, `s`.`Banner`, `s`.`Banner5`, `s`.`InternalNumber`, `s`.`Name`
FROM `Squads` AS `s`
WHERE EXISTS (
    SELECT 1
    FROM `Gears` AS `g`
    WHERE `s`.`Id` = `g`.`SquadId` AND `g`.`SquadId` = @gearId AND `g`.`SquadId` = @gearId)
""");
        }

        public override async Task Query_reusing_parameter_doesnt_declare_duplicate_parameter_complex(bool isAsync)
        {
            await base.Query_reusing_parameter_doesnt_declare_duplicate_parameter_complex(isAsync);

            AssertSql(
                """
@entity_equality_prm_Inner_Squad_Id='1' (Nullable = true)
@entity_equality_prm_Inner_Squad_Id='1' (Nullable = true)

SELECT `s1`.`Nickname`, `s1`.`SquadId`, `s1`.`AssignedCityName`, `s1`.`CityOfBirthName`, `s1`.`Discriminator`, `s1`.`FullName`, `s1`.`HasSoulPatch`, `s1`.`LeaderNickname`, `s1`.`LeaderSquadId`, `s1`.`Rank`
FROM (
    SELECT DISTINCT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
    FROM `Gears` AS `g`
    INNER JOIN `Squads` AS `s` ON `g`.`SquadId` = `s`.`Id`
    WHERE `s`.`Id` = @entity_equality_prm_Inner_Squad_Id
) AS `s1`
INNER JOIN `Squads` AS `s0` ON `s1`.`SquadId` = `s0`.`Id`
WHERE `s0`.`Id` = @entity_equality_prm_Inner_Squad_Id
ORDER BY `s1`.`FullName`
""");
        }

        public override async Task Complex_GroupBy_after_set_operator(bool isAsync)
        {
            await base.Complex_GroupBy_after_set_operator(isAsync);

            AssertSql(
                """
SELECT `u`.`Name`, `u`.`Count`, IIF(SUM(`u`.`Count`) IS NULL, 0, SUM(`u`.`Count`)) AS `Sum`
FROM (
    SELECT `c`.`Name`, (
        SELECT COUNT(*)
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName`) AS `Count`
    FROM `Gears` AS `g`
    LEFT JOIN `Cities` AS `c` ON `g`.`AssignedCityName` = `c`.`Name`
    UNION ALL
    SELECT `c0`.`Name`, (
        SELECT COUNT(*)
        FROM `Weapons` AS `w0`
        WHERE `g0`.`FullName` = `w0`.`OwnerFullName`) AS `Count`
    FROM `Gears` AS `g0`
    INNER JOIN `Cities` AS `c0` ON `g0`.`CityOfBirthName` = `c0`.`Name`
) AS `u`
GROUP BY `u`.`Name`, `u`.`Count`
""");
        }

        public override async Task Complex_GroupBy_after_set_operator_using_result_selector(bool isAsync)
        {
            await base.Complex_GroupBy_after_set_operator_using_result_selector(isAsync);

            AssertSql(
                """
SELECT `u`.`Name`, `u`.`Count`, IIF(SUM(`u`.`Count`) IS NULL, 0, SUM(`u`.`Count`)) AS `Sum`
FROM (
    SELECT `c`.`Name`, (
        SELECT COUNT(*)
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName`) AS `Count`
    FROM `Gears` AS `g`
    LEFT JOIN `Cities` AS `c` ON `g`.`AssignedCityName` = `c`.`Name`
    UNION ALL
    SELECT `c0`.`Name`, (
        SELECT COUNT(*)
        FROM `Weapons` AS `w0`
        WHERE `g0`.`FullName` = `w0`.`OwnerFullName`) AS `Count`
    FROM `Gears` AS `g0`
    INNER JOIN `Cities` AS `c0` ON `g0`.`CityOfBirthName` = `c0`.`Name`
) AS `u`
GROUP BY `u`.`Name`, `u`.`Count`
""");
        }

        public override async Task Left_join_with_GroupBy_with_composite_group_key(bool isAsync)
        {
            await base.Left_join_with_GroupBy_with_composite_group_key(isAsync);

            AssertSql(
                """
    SELECT `g`.`CityOfBirthName`, `g`.`HasSoulPatch`
    FROM (`Gears` AS `g`
    INNER JOIN `Squads` AS `s` ON `g`.`SquadId` = `s`.`Id`)
    LEFT JOIN `Tags` AS `t` ON `g`.`Nickname` = `t`.`GearNickName`
    GROUP BY `g`.`CityOfBirthName`, `g`.`HasSoulPatch`
    """);
        }

        public override async Task GroupBy_with_boolean_grouping_key(bool isAsync)
        {
            await base.GroupBy_with_boolean_grouping_key(isAsync);

            AssertSql(
                """
SELECT `g0`.`CityOfBirthName`, `g0`.`HasSoulPatch`, `g0`.`IsMarcus`, COUNT(*) AS `Count`
FROM (
    SELECT `g`.`CityOfBirthName`, `g`.`HasSoulPatch`, IIF(`g`.`Nickname` = 'Marcus', TRUE, FALSE) AS `IsMarcus`
    FROM `Gears` AS `g`
) AS `g0`
GROUP BY `g0`.`CityOfBirthName`, `g0`.`HasSoulPatch`, `g0`.`IsMarcus`
""");
        }

        public override async Task GroupBy_with_boolean_groupin_key_thru_navigation_access(bool isAsync)
        {
            await base.GroupBy_with_boolean_groupin_key_thru_navigation_access(isAsync);

            AssertSql(
                """
    SELECT `g`.`HasSoulPatch`, LCASE(`s`.`Name`) AS `Name`
    FROM (`Tags` AS `t`
    LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`)
    LEFT JOIN `Squads` AS `s` ON `g`.`SquadId` = `s`.`Id`
    GROUP BY `g`.`HasSoulPatch`, `s`.`Name`
    """);
        }

        public override async Task Group_by_over_projection_with_multiple_properties_accessed_thru_navigation(bool isAsync)
        {
            await base.Group_by_over_projection_with_multiple_properties_accessed_thru_navigation(isAsync);

            AssertSql(
"""
SELECT `c`.`Name`
FROM `Gears` AS `g`
INNER JOIN `Cities` AS `c` ON `g`.`CityOfBirthName` = `c`.`Name`
GROUP BY `c`.`Name`
""");
        }

        public override async Task Group_by_on_StartsWith_with_null_parameter_as_argument(bool isAsync)
        {
            await base.Group_by_on_StartsWith_with_null_parameter_as_argument(isAsync);

            AssertSql(
                """
SELECT `g0`.`Key`
FROM (
    SELECT FALSE AS `Key`
    FROM `Gears` AS `g`
) AS `g0`
GROUP BY `g0`.`Key`
""");
        }

        public override async Task Group_by_with_having_StartsWith_with_null_parameter_as_argument(bool isAsync)
        {
            await base.Group_by_with_having_StartsWith_with_null_parameter_as_argument(isAsync);
            AssertSql(
            """
SELECT `g`.`FullName`
FROM `Gears` AS `g`
GROUP BY `g`.`FullName`
HAVING 0 = 1
""");
        }

        public override async Task Select_StartsWith_with_null_parameter_as_argument(bool isAsync)
        {
            await base.Select_StartsWith_with_null_parameter_as_argument(isAsync);

            AssertSql(
"""
SELECT FALSE
FROM `Gears` AS `g`
""");
        }

        public override async Task Select_null_parameter_is_not_null(bool isAsync)
        {
            await base.Select_null_parameter_is_not_null(isAsync);

            AssertSql(
                """
@p='False'

SELECT CBOOL(@p)
FROM `Gears` AS `g`
""");
        }

        public override async Task Where_null_parameter_is_not_null(bool isAsync)
        {
            await base.Where_null_parameter_is_not_null(isAsync);
            AssertSql(
                """
@p='False'

SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE @p = TRUE
""");
        }

        public override async Task OrderBy_StartsWith_with_null_parameter_as_argument(bool isAsync)
        {
            await base.OrderBy_StartsWith_with_null_parameter_as_argument(isAsync);

            AssertSql(
"""
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
ORDER BY `g`.`Nickname`
""");
        }

        public override async Task OrderBy_Contains_empty_list(bool async)
        {
            await base.OrderBy_Contains_empty_list(async);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
""");
        }

        public override async Task Where_with_enum_flags_parameter(bool isAsync)
        {
            await base.Where_with_enum_flags_parameter(isAsync);

            AssertSql(
                """
@rank='1' (Nullable = true)
@rank='1' (Nullable = true)

SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE (`g`.`Rank` BAND @rank) = @rank
""",
                //
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
""",
                //
                """
@rank='2' (Nullable = true)
@rank='2' (Nullable = true)

SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE (`g`.`Rank` BOR @rank) <> @rank
""",
                //
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE 0 = 1
""");
        }

        public override async Task FirstOrDefault_navigation_access_entity_equality_in_where_predicate_apply_peneding_selector(bool isAsync)
        {
            await base.FirstOrDefault_navigation_access_entity_equality_in_where_predicate_apply_peneding_selector(isAsync);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
LEFT JOIN `Cities` AS `c` ON `g`.`AssignedCityName` = `c`.`Name`
WHERE `c`.`Name` = (
    SELECT TOP 1 `c0`.`Name`
    FROM `Gears` AS `g0`
    INNER JOIN `Cities` AS `c0` ON `g0`.`CityOfBirthName` = `c0`.`Name`
    ORDER BY `g0`.`Nickname`) OR (`c`.`Name` IS NULL AND (
    SELECT TOP 1 `c0`.`Name`
    FROM `Gears` AS `g0`
    INNER JOIN `Cities` AS `c0` ON `g0`.`CityOfBirthName` = `c0`.`Name`
    ORDER BY `g0`.`Nickname`) IS NULL)
""");
        }

        public override async Task Bitwise_operation_with_non_null_parameter_optimizes_null_checks(bool async)
        {
            await base.Bitwise_operation_with_non_null_parameter_optimizes_null_checks(async);

            AssertSql(
                """
@ranks='134'

SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE (`g`.`Rank` BAND @ranks) <> 0
""",
                //
                """
@ranks='134'
@ranks='134'

SELECT CBOOL((`g`.`Rank` BOR @ranks) BXOR @ranks) BXOR TRUE
FROM `Gears` AS `g`
""",
                //
                """
@ranks='134'
@ranks='134'
@ranks='134'

SELECT CBOOL((`g`.`Rank` BOR (`g`.`Rank` BOR (@ranks BOR (`g`.`Rank` BOR @ranks)))) BXOR @ranks) BXOR TRUE
FROM `Gears` AS `g`
""");
        }

        public override async Task Bitwise_operation_with_null_arguments(bool async)
        {
            await base.Bitwise_operation_with_null_arguments(async);

            AssertSql(
                """
SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM `Weapons` AS `w`
WHERE `w`.`AmmunitionType` IS NULL
""",
                //
                """
SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM `Weapons` AS `w`
WHERE `w`.`AmmunitionType` IS NULL
""",
                //
                """
SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM `Weapons` AS `w`
WHERE `w`.`AmmunitionType` IS NULL
""",
                //
                """
SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM `Weapons` AS `w`
""",
                //
                """
@prm='2' (Nullable = true)

SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM `Weapons` AS `w`
WHERE (`w`.`AmmunitionType` BAND @prm) <> 0 OR `w`.`AmmunitionType` IS NULL
""",
                //
                """
@prm='1' (Nullable = true)
@prm='1' (Nullable = true)

SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM `Weapons` AS `w`
WHERE (`w`.`AmmunitionType` BAND @prm) = @prm
""");
        }

        public override async Task Logical_operation_with_non_null_parameter_optimizes_null_checks(bool async)
        {
            await base.Logical_operation_with_non_null_parameter_optimizes_null_checks(async);

            AssertSql(
                """
@prm='True'

SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE `g`.`HasSoulPatch` <> @prm
""",
                //
                """
@prm='False'

SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE `g`.`HasSoulPatch` <> @prm
""");
        }

        public override async Task Cast_OfType_works_correctly(bool async)
        {
            await base.Cast_OfType_works_correctly(async);

            AssertSql(
    """
SELECT `g`.`FullName`
FROM `Gears` AS `g`
WHERE `g`.`Discriminator` = 'Officer'
""");
        }

        public override async Task Join_inner_source_custom_projection_followed_by_filter(bool async)
        {
            await base.Join_inner_source_custom_projection_followed_by_filter(async);

            AssertSql(
                """
SELECT IIF(`f`.`Name` = 'Locust', TRUE, NULL) AS `IsEradicated`, `f`.`CommanderName`, `f`.`Name`
FROM `LocustLeaders` AS `l`
INNER JOIN `Factions` AS `f` ON `l`.`Name` = `f`.`CommanderName`
WHERE IIF(`f`.`Name` = 'Locust', TRUE, NULL) <> TRUE OR IIF(`f`.`Name` = 'Locust', TRUE, NULL) IS NULL
""");
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task Byte_array_filter_by_length_literal2(bool async)
        {
            await AssertQuery(
                async,
                ss => ss.Set<Squad>().Where(w => EF.Functions.ByteArrayLength(w.Banner) == 2),
                ss => ss.Set<Squad>().Where(w => w.Banner != null && w.Banner.Length == 2));

            AssertSql(
                """
SELECT `s`.`Id`, `s`.`Banner`, `s`.`Banner5`, `s`.`InternalNumber`, `s`.`Name`
FROM `Squads` AS `s`
WHERE IIF(ASCB(RIGHTB(`s`.`Banner`, 1)) = 0, LENB(`s`.`Banner`) - 1, LENB(`s`.`Banner`)) = 2
""");
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task Byte_array_filter_by_length_parameter2(bool async)
        {
            var someByteArr = new[] { (byte)42, (byte)24 };
            await AssertQuery(
                async,
                ss => ss.Set<Squad>().Where(w => EF.Functions.ByteArrayLength(w.Banner) == someByteArr.Length),
                ss => ss.Set<Squad>().Where(w => w.Banner != null && w.Banner.Length == someByteArr.Length));

            AssertSql(
                """
@p='2'

SELECT `s`.`Id`, `s`.`Banner`, `s`.`Banner5`, `s`.`InternalNumber`, `s`.`Name`
FROM `Squads` AS `s`
WHERE IIF(ASCB(RIGHTB(`s`.`Banner`, 1)) = 0, LENB(`s`.`Banner`) - 1, LENB(`s`.`Banner`)) = @p
""");
        }

        [ConditionalFact]
        public virtual void Byte_array_filter_by_length_parameter_compiled2()
        {
            var query = EF.CompileQuery(
                (GearsOfWarContext context, byte[] byteArrayParam)
                    => context.Squads.Where(w => EF.Functions.ByteArrayLength(w.Banner) == EF.Functions.ByteArrayLength(byteArrayParam)).Count());

            using var context = CreateContext();
            var byteQueryParam = new[] { (byte)42, (byte)128 };

            Assert.Equal(2, query(context, byteQueryParam));

            AssertSql(
                """
@byteArrayParam='0x2A80' (Size = 510)
@byteArrayParam='0x2A80' (Size = 510)
@byteArrayParam='0x2A80' (Size = 510)
@byteArrayParam='0x2A80' (Size = 510)
@byteArrayParam='0x2A80' (Size = 510)
@byteArrayParam='0x2A80' (Size = 510)

SELECT COUNT(*)
FROM `Squads` AS `s`
WHERE IIF(ASCB(RIGHTB(`s`.`Banner`, 1)) = 0, LENB(`s`.`Banner`) - 1, LENB(`s`.`Banner`)) = IIF(IIF(ASCB(RIGHTB(@byteArrayParam, 1)) = 0, LENB(@byteArrayParam) - 1, LENB(@byteArrayParam)) IS NULL, NULL, CLNG(IIF(ASCB(RIGHTB(@byteArrayParam, 1)) = 0, LENB(@byteArrayParam) - 1, LENB(@byteArrayParam))))
""");
        }

        public override async Task Byte_array_filter_by_length_literal_does_not_cast_on_varbinary_n(bool async)
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => base.Byte_array_filter_by_length_literal_does_not_cast_on_varbinary_n(async));
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task Byte_array_filter_by_length_literal_does_not_cast_on_varbinary_n2(bool async)
        {
            await AssertQuery(
                async,
                ss => ss.Set<Squad>().Where(w => EF.Functions.ByteArrayLength(w.Banner5) == 5),
                ss => ss.Set<Squad>().Where(w => w.Banner5 != null && w.Banner5.Length == 5));

            AssertSql(
                """
SELECT `s`.`Id`, `s`.`Banner`, `s`.`Banner5`, `s`.`InternalNumber`, `s`.`Name`
FROM `Squads` AS `s`
WHERE IIF(ASCB(RIGHTB(`s`.`Banner5`, 1)) = 0, LENB(`s`.`Banner5`) - 1, LENB(`s`.`Banner5`)) = 5
""");
        }

        public override async Task Conditional_expression_with_test_being_simplified_to_constant_simple(bool isAsync)
        {
            await base.Conditional_expression_with_test_being_simplified_to_constant_simple(isAsync);

            AssertSql(
                """
@prm='True'

SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE IIF(`g`.`HasSoulPatch` = @prm, TRUE, FALSE) = TRUE
""");
        }

        public override async Task Conditional_expression_with_test_being_simplified_to_constant_complex(bool isAsync)
        {
            await base.Conditional_expression_with_test_being_simplified_to_constant_complex(isAsync);

            AssertSql(
                """
@prm='True'
@prm2='Marcus' Lancer' (Size = 255)

SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE IIF(`g`.`HasSoulPatch` = @prm AND (
        SELECT TOP 1 `w`.`Name`
        FROM `Weapons` AS `w`
        WHERE `w`.`Id` = `g`.`SquadId`) = @prm2, TRUE, FALSE) = TRUE
""");
        }

        public override async Task OrderBy_bool_coming_from_optional_navigation(bool async)
        {
            await base.OrderBy_bool_coming_from_optional_navigation(async);

            AssertSql(
                """
SELECT `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
FROM `Weapons` AS `w`
LEFT JOIN `Weapons` AS `w0` ON `w`.`SynergyWithId` = `w0`.`Id`
ORDER BY NOT (`w0`.`IsAutomatic`)
""");
        }

        public override async Task DateTimeOffset_Date_returns_datetime(bool async)
        {
            var dateTimeOffset = new DateTimeOffset(102, 3, 1, 8, 0, 0, new TimeSpan(-5, 0, 0));

            await AssertQuery(
                async,
                ss => ss.Set<Mission>().Where(m => m.Timeline.Date >= dateTimeOffset.Date));

            AssertSql(
                """
@dateTimeOffset_Date='0102-03-01T00:00:00.0000000' (DbType = DateTime)

SELECT `m`.`Id`, `m`.`CodeName`, `m`.`Date`, `m`.`Difficulty`, `m`.`Duration`, `m`.`Rating`, `m`.`Time`, `m`.`Timeline`
FROM `Missions` AS `m`
WHERE DATEVALUE(`m`.`Timeline`) >= CDATE(@dateTimeOffset_Date)
""");
        }

        public override async Task Conditional_with_conditions_evaluating_to_false_gets_optimized(bool async)
        {
            await base.Conditional_with_conditions_evaluating_to_false_gets_optimized(async);

            AssertSql(
    """
SELECT `g`.`FullName`
FROM `Gears` AS `g`
""");
        }

        public override async Task Conditional_with_conditions_evaluating_to_true_gets_optimized(bool async)
        {
            await base.Conditional_with_conditions_evaluating_to_true_gets_optimized(async);

            AssertSql(
    """
SELECT `g`.`CityOfBirthName`
FROM `Gears` AS `g`
""");
        }

        public override async Task Projecting_required_string_column_compared_to_null_parameter(bool async)
        {
            await base.Projecting_required_string_column_compared_to_null_parameter(async);

            AssertSql(
    """
SELECT FALSE
FROM `Gears` AS `g`
""");
        }

        public override async Task Group_by_nullable_property_HasValue_and_project_the_grouping_key(bool async)
        {
            await base.Group_by_nullable_property_HasValue_and_project_the_grouping_key(async);

            AssertSql(
                """
SELECT `w0`.`Key`
FROM (
    SELECT IIF(`w`.`SynergyWithId` IS NOT NULL, TRUE, FALSE) AS `Key`
    FROM `Weapons` AS `w`
) AS `w0`
GROUP BY `w0`.`Key`
""");
        }

        public override async Task Group_by_nullable_property_and_project_the_grouping_key_HasValue(bool async)
        {
            await base.Group_by_nullable_property_and_project_the_grouping_key_HasValue(async);

            AssertSql(
                """
    SELECT IIF(`w`.`SynergyWithId` IS NOT NULL, TRUE, FALSE)
    FROM `Weapons` AS `w`
    GROUP BY `w`.`SynergyWithId`
    """);
        }

        public override async Task Checked_context_with_cast_does_not_fail(bool isAsync)
        {
            await base.Checked_context_with_cast_does_not_fail(isAsync);

            AssertSql(
                """
SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`
FROM `LocustLeaders` AS `l`
WHERE CBYTE(`l`.`ThreatLevel`) >= CBYTE(5)
""");
        }

        public override async Task Checked_context_with_addition_does_not_fail(bool isAsync)
        {
            await base.Checked_context_with_addition_does_not_fail(isAsync);

            AssertSql(
                """
SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`
FROM `LocustLeaders` AS `l`
WHERE CLNG(`l`.`ThreatLevel`) <= (5 + CLNG(`l`.`ThreatLevel`))
""");
        }

        public override async Task Contains_on_collection_of_byte_subquery(bool async)
        {
            await base.Contains_on_collection_of_byte_subquery(async);

            AssertSql(
"""
SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`
FROM `LocustLeaders` AS `l`
WHERE `l`.`ThreatLevelByte` IN (
    SELECT `l0`.`ThreatLevelByte`
    FROM `LocustLeaders` AS `l0`
)
""");
        }

        public override async Task Contains_on_collection_of_nullable_byte_subquery(bool async)
        {
            await base.Contains_on_collection_of_nullable_byte_subquery(async);

            AssertSql(
"""
SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`
FROM `LocustLeaders` AS `l`
WHERE EXISTS (
    SELECT 1
    FROM `LocustLeaders` AS `l0`
    WHERE `l0`.`ThreatLevelNullableByte` = `l`.`ThreatLevelNullableByte` OR (`l0`.`ThreatLevelNullableByte` IS NULL AND `l`.`ThreatLevelNullableByte` IS NULL))
""");
        }

        public override async Task Contains_on_collection_of_nullable_byte_subquery_null_constant(bool async)
        {
            await base.Contains_on_collection_of_nullable_byte_subquery_null_constant(async);

            AssertSql(
    """
SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`
FROM `LocustLeaders` AS `l`
WHERE EXISTS (
    SELECT 1
    FROM `LocustLeaders` AS `l0`
    WHERE `l0`.`ThreatLevelNullableByte` IS NULL)
""");
        }

        public override async Task Contains_on_collection_of_nullable_byte_subquery_null_parameter(bool async)
        {
            await base.Contains_on_collection_of_nullable_byte_subquery_null_parameter(async);

            AssertSql(
    """
SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`
FROM `LocustLeaders` AS `l`
WHERE EXISTS (
    SELECT 1
    FROM `LocustLeaders` AS `l0`
    WHERE `l0`.`ThreatLevelNullableByte` IS NULL)
""");
        }

        public override async Task Subquery_projecting_non_nullable_scalar_contains_non_nullable_value_doesnt_need_null_expansion(
        bool async)
        {
            await base.Subquery_projecting_non_nullable_scalar_contains_non_nullable_value_doesnt_need_null_expansion(async);

            AssertSql(
    """
SELECT `t`.`Nickname`, `t`.`SquadId`, `t`.`AssignedCityName`, `t`.`CityOfBirthName`, `t`.`Discriminator`, `t`.`FullName`, `t`.`HasSoulPatch`, `t`.`LeaderNickname`, `t`.`LeaderSquadId`, `t`.`Rank`
FROM `LocustLeaders` AS `l`
CROSS APPLY (
    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
    FROM `Gears` AS `g`
    WHERE EXISTS (
        SELECT 1
        FROM `LocustLeaders` AS `l0`
        WHERE `l0`.`ThreatLevelByte` = `l`.`ThreatLevelByte`)
) AS `t`
""");
        }

        public override async Task Subquery_projecting_non_nullable_scalar_contains_non_nullable_value_doesnt_need_null_expansion_negated(
            bool async)
        {
            await base.Subquery_projecting_non_nullable_scalar_contains_non_nullable_value_doesnt_need_null_expansion_negated(async);

            AssertSql(
    """
SELECT `t`.`Nickname`, `t`.`SquadId`, `t`.`AssignedCityName`, `t`.`CityOfBirthName`, `t`.`Discriminator`, `t`.`FullName`, `t`.`HasSoulPatch`, `t`.`LeaderNickname`, `t`.`LeaderSquadId`, `t`.`Rank`
FROM `LocustLeaders` AS `l`
CROSS APPLY (
    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
    FROM `Gears` AS `g`
    WHERE NOT (EXISTS (
        SELECT 1
        FROM `LocustLeaders` AS `l0`
        WHERE `l0`.`ThreatLevelByte` = `l`.`ThreatLevelByte`))
) AS `t`
""");
        }

        public override async Task Subquery_projecting_nullable_scalar_contains_nullable_value_needs_null_expansion(bool async)
        {
            await base.Subquery_projecting_nullable_scalar_contains_nullable_value_needs_null_expansion(async);

            AssertSql(
    """
SELECT `t`.`Nickname`, `t`.`SquadId`, `t`.`AssignedCityName`, `t`.`CityOfBirthName`, `t`.`Discriminator`, `t`.`FullName`, `t`.`HasSoulPatch`, `t`.`LeaderNickname`, `t`.`LeaderSquadId`, `t`.`Rank`
FROM `LocustLeaders` AS `l`
CROSS APPLY (
    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
    FROM `Gears` AS `g`
    WHERE EXISTS (
        SELECT 1
        FROM `LocustLeaders` AS `l0`
        WHERE `l0`.`ThreatLevelNullableByte` = `l`.`ThreatLevelNullableByte` OR ((`l0`.`ThreatLevelNullableByte` IS NULL) AND (`l`.`ThreatLevelNullableByte` IS NULL)))
) AS `t`
""");
        }

        public override async Task Subquery_projecting_nullable_scalar_contains_nullable_value_needs_null_expansion_negated(bool async)
        {
            await base.Subquery_projecting_nullable_scalar_contains_nullable_value_needs_null_expansion_negated(async);

            AssertSql(
    """
SELECT `t`.`Nickname`, `t`.`SquadId`, `t`.`AssignedCityName`, `t`.`CityOfBirthName`, `t`.`Discriminator`, `t`.`FullName`, `t`.`HasSoulPatch`, `t`.`LeaderNickname`, `t`.`LeaderSquadId`, `t`.`Rank`
FROM `LocustLeaders` AS `l`
CROSS APPLY (
    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
    FROM `Gears` AS `g`
    WHERE NOT (EXISTS (
        SELECT 1
        FROM `LocustLeaders` AS `l0`
        WHERE `l0`.`ThreatLevelNullableByte` = `l`.`ThreatLevelNullableByte` OR ((`l0`.`ThreatLevelNullableByte` IS NULL) AND (`l`.`ThreatLevelNullableByte` IS NULL))))
) AS `t`
""");
        }

        public override async Task Enum_closure_typed_as_underlying_type_generates_correct_parameter_type(bool async)
        {
            await base.Enum_closure_typed_as_underlying_type_generates_correct_parameter_type(async);

            AssertSql(
                """
@prm='1' (Nullable = true)

SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM `Weapons` AS `w`
WHERE @prm = `w`.`AmmunitionType`
""");
        }

        public override async Task Enum_flags_closure_typed_as_underlying_type_generates_correct_parameter_type(bool async)
        {
            await base.Enum_flags_closure_typed_as_underlying_type_generates_correct_parameter_type(async);

            AssertSql(
                """
@prm='133'

SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE (@prm BAND `g`.`Rank`) = `g`.`Rank`
""");
        }

        public override async Task Enum_flags_closure_typed_as_different_type_generates_correct_parameter_type(bool async)
        {
            await base.Enum_flags_closure_typed_as_different_type_generates_correct_parameter_type(async);

            AssertSql(
                """
@prm='5'

SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE (@prm BAND CLNG(`g`.`Rank`)) = CLNG(`g`.`Rank`)
""");
        }

        public override async Task Constant_enum_with_same_underlying_value_as_previously_parameterized_int(bool async)
        {
            await base.Constant_enum_with_same_underlying_value_as_previously_parameterized_int(async);

            AssertSql(
                """
SELECT TOP @p `g`.`Rank` BAND 1
FROM `Gears` AS `g`
ORDER BY `g`.`Nickname`
""");
        }

        public override async Task Enum_array_contains(bool async)
        {
            await base.Enum_array_contains(async);

            AssertSql(
                """
SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM `Weapons` AS `w`
LEFT JOIN `Weapons` AS `w0` ON `w`.`SynergyWithId` = `w0`.`Id`
WHERE `w0`.`Id` IS NOT NULL AND (`w0`.`AmmunitionType` IS NULL OR `w0`.`AmmunitionType` = 1)
""");
        }

        public override async Task Coalesce_with_non_root_evaluatable_Convert(bool async)
        {
            await base.Coalesce_with_non_root_evaluatable_Convert(async);

            AssertSql(
                """
@rank='1' (Nullable = true)

SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE @rank = `g`.`Rank`
""");
        }

        /*`ConditionalTheory`
        `MemberData(nameof(IsAsyncData))`
        public async Task DataLength_function_for_string_parameter(bool async)
        {
            await AssertQueryScalar(
                async,
                ss => ss.Set<Mission>().Select(m => EF.Functions.DataLength(m.CodeName)),
                ss => ss.Set<Mission>().Select(m => (int?)(m.CodeName.Length * 2)));

            AssertSql(
    """
SELECT CAST(DATALENGTH(`m`.`CodeName`) AS int)
FROM `Missions` AS `m`
""");
        }*/

        public override async Task CompareTo_used_with_non_unicode_string_column_and_constant(bool async)
        {
            await base.CompareTo_used_with_non_unicode_string_column_and_constant(async);

            AssertSql(
    """
SELECT `c`.`Name`, `c`.`Location`, `c`.`Nation`
FROM `Cities` AS `c`
WHERE `c`.`Location` = 'Unknown'
""");
        }

        public override async Task Coalesce_used_with_non_unicode_string_column_and_constant(bool async)
        {
            await base.Coalesce_used_with_non_unicode_string_column_and_constant(async);

            AssertSql(
                """
    SELECT IIF(`c`.`Location` IS NULL, 'Unknown', `c`.`Location`)
    FROM `Cities` AS `c`
    """);
        }

        public override async Task Groupby_anonymous_type_with_navigations_followed_up_by_anonymous_projection_and_orderby(bool async)
        {
            await base.Groupby_anonymous_type_with_navigations_followed_up_by_anonymous_projection_and_orderby(async);

            AssertSql(
"""
SELECT `c`.`Name`, `c`.`Location`, COUNT(*) AS `Count`
FROM (`Weapons` AS `w`
LEFT JOIN `Gears` AS `g` ON `w`.`OwnerFullName` = `g`.`FullName`)
LEFT JOIN `Cities` AS `c` ON `g`.`CityOfBirthName` = `c`.`Name`
GROUP BY `c`.`Name`, `c`.`Location`
ORDER BY `c`.`Location`
""");
        }

        public override async Task SelectMany_predicate_with_non_equality_comparison_converted_to_inner_join(bool async)
        {
            await base.SelectMany_predicate_with_non_equality_comparison_converted_to_inner_join(async);

            AssertSql(
"""
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM `Gears` AS `g`
INNER JOIN `Weapons` AS `w` ON `g`.`FullName` <> `w`.`OwnerFullName` OR `w`.`OwnerFullName` IS NULL
ORDER BY `g`.`Nickname`, `w`.`Id`
""");
        }

        public override async Task SelectMany_predicate_with_non_equality_comparison_DefaultIfEmpty_converted_to_left_join(bool async)
        {
            await base.SelectMany_predicate_with_non_equality_comparison_DefaultIfEmpty_converted_to_left_join(async);

            AssertSql(
"""
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM `Gears` AS `g`
LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` <> `w`.`OwnerFullName` OR `w`.`OwnerFullName` IS NULL
ORDER BY `g`.`Nickname`, `w`.`Id`
""");
        }

        public override async Task SelectMany_predicate_after_navigation_with_non_equality_comparison_DefaultIfEmpty_converted_to_left_join(
            bool async)
        {
            await base.SelectMany_predicate_after_navigation_with_non_equality_comparison_DefaultIfEmpty_converted_to_left_join(async);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `s`.`Id`, `s`.`AmmunitionType`, `s`.`IsAutomatic`, `s`.`Name`, `s`.`OwnerFullName`, `s`.`SynergyWithId`
FROM `Gears` AS `g`
LEFT JOIN (
    SELECT `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
    FROM `Weapons` AS `w`
    LEFT JOIN `Weapons` AS `w0` ON `w`.`SynergyWithId` = `w0`.`Id`
) AS `s` ON `g`.`FullName` <> `s`.`OwnerFullName` OR `s`.`OwnerFullName` IS NULL
ORDER BY `g`.`Nickname`, `s`.`Id`
""");
        }

        public override async Task SelectMany_without_result_selector_and_non_equality_comparison_converted_to_join(bool async)
        {
            await base.SelectMany_without_result_selector_and_non_equality_comparison_converted_to_join(async);

            AssertSql(
"""
SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM `Gears` AS `g`
LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` <> `w`.`OwnerFullName` OR `w`.`OwnerFullName` IS NULL
""");
        }

        public override async Task Filtered_collection_projection_with_order_comparison_predicate_converted_to_join(bool async)
        {
            await base.Filtered_collection_projection_with_order_comparison_predicate_converted_to_join(async);

            AssertSql(
    """
SELECT `g`.`Nickname`, `g`.`SquadId`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM `Gears` AS `g`
LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName` AND `g`.`SquadId` < `w`.`Id`
ORDER BY `g`.`Nickname`, `g`.`SquadId`
""");
        }

        public override async Task Filtered_collection_projection_with_order_comparison_predicate_converted_to_join2(bool async)
        {
            await base.Filtered_collection_projection_with_order_comparison_predicate_converted_to_join2(async);

            AssertSql(
    """
SELECT `g`.`Nickname`, `g`.`SquadId`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM `Gears` AS `g`
LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName` AND `g`.`SquadId` <= `w`.`Id`
ORDER BY `g`.`Nickname`, `g`.`SquadId`
""");
        }

        public override async Task Filtered_collection_projection_with_order_comparison_predicate_converted_to_join3(bool async)
        {
            await base.Filtered_collection_projection_with_order_comparison_predicate_converted_to_join3(async);

            AssertSql(
    """
SELECT `g`.`Nickname`, `g`.`SquadId`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM `Gears` AS `g`
LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName` AND `g`.`SquadId` >= `w`.`Id`
ORDER BY `g`.`Nickname`, `g`.`SquadId`
""");
        }

        public override async Task SelectMany_predicate_with_non_equality_comparison_with_Take_doesnt_convert_to_join(bool async)
        {
            await base.SelectMany_predicate_with_non_equality_comparison_with_Take_doesnt_convert_to_join(async);

            AssertSql(
    """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `t`.`Id`, `t`.`AmmunitionType`, `t`.`IsAutomatic`, `t`.`Name`, `t`.`OwnerFullName`, `t`.`SynergyWithId`
FROM `Gears` AS `g`
CROSS APPLY (
    SELECT TOP(3) `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
    FROM `Weapons` AS `w`
    WHERE `w`.`OwnerFullName` <> `g`.`FullName` OR (`w`.`OwnerFullName` IS NULL)
    ORDER BY `w`.`Id`
) AS `t`
ORDER BY `g`.`Nickname`, `t`.`Id`
""");
        }

        public override async Task FirstOrDefault_over_int_compared_to_zero(bool async)
        {
            await base.FirstOrDefault_over_int_compared_to_zero(async);

            AssertSql(
                """
SELECT `s`.`Name`
FROM `Squads` AS `s`
WHERE `s`.`Name` = 'Delta' AND IIF((
        SELECT TOP 1 `g`.`SquadId`
        FROM `Gears` AS `g`
        WHERE `s`.`Id` = `g`.`SquadId` AND `g`.`HasSoulPatch` = TRUE
        ORDER BY `g`.`FullName`) IS NULL, 0, (
        SELECT TOP 1 `g`.`SquadId`
        FROM `Gears` AS `g`
        WHERE `s`.`Id` = `g`.`SquadId` AND `g`.`HasSoulPatch` = TRUE
        ORDER BY `g`.`FullName`)) <> 0
""");
        }

        public override async Task Correlated_collection_with_inner_collection_references_element_two_levels_up(bool async)
        {
            await base.Correlated_collection_with_inner_collection_references_element_two_levels_up(async);

            AssertSql(
    """
SELECT `g`.`FullName`, `g`.`Nickname`, `g`.`SquadId`, `t`.`ReportName`, `t`.`OfficerName`, `t`.`Nickname`, `t`.`SquadId`
FROM `Gears` AS `g`
OUTER APPLY (
    SELECT `g0`.`FullName` AS `ReportName`, `g`.`FullName` AS `OfficerName`, `g0`.`Nickname`, `g0`.`SquadId`
    FROM `Gears` AS `g0`
    WHERE `g`.`Nickname` = `g0`.`LeaderNickname` AND `g`.`SquadId` = `g0`.`LeaderSquadId`
) AS `t`
WHERE `g`.`Discriminator` = 'Officer'
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `t`.`Nickname`
""");
        }

        public override async Task Accessing_derived_property_using_hard_and_soft_cast(bool async)
        {
            await base.Accessing_derived_property_using_hard_and_soft_cast(async);

            AssertSql(
"""
SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`
FROM `LocustLeaders` AS `l`
WHERE `l`.`Discriminator` = 'LocustCommander' AND (`l`.`HighCommandId` <> 0 OR `l`.`HighCommandId` IS NULL)
""",
//
"""
SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`
FROM `LocustLeaders` AS `l`
WHERE `l`.`Discriminator` = 'LocustCommander' AND (`l`.`HighCommandId` <> 0 OR `l`.`HighCommandId` IS NULL)
""");
        }

        public override async Task Cast_to_derived_followed_by_include_and_FirstOrDefault(bool async)
        {
            await base.Cast_to_derived_followed_by_include_and_FirstOrDefault(async);

            AssertSql(
    """
SELECT TOP 1 `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`, `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `LocustLeaders` AS `l`
LEFT JOIN `Gears` AS `g` ON `l`.`DefeatedByNickname` = `g`.`Nickname` AND `l`.`DefeatedBySquadId` = `g`.`SquadId`
WHERE `l`.`Name` LIKE '%Queen%'
""");
        }

        public override async Task Correlated_collection_take(bool async)
        {
            await base.Correlated_collection_take(async);

            AssertSql(
    """
SELECT `g`.`Nickname`, `g`.`SquadId`, `c`.`Name`, `t0`.`Id`, `t0`.`AmmunitionType`, `t0`.`IsAutomatic`, `t0`.`Name`, `t0`.`OwnerFullName`, `t0`.`SynergyWithId`, `c`.`Location`, `c`.`Nation`
FROM `Gears` AS `g`
INNER JOIN `Cities` AS `c` ON `g`.`CityOfBirthName` = `c`.`Name`
LEFT JOIN (
    SELECT `t`.`Id`, `t`.`AmmunitionType`, `t`.`IsAutomatic`, `t`.`Name`, `t`.`OwnerFullName`, `t`.`SynergyWithId`
    FROM (
        SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`, ROW_NUMBER() OVER(PARTITION BY `w`.`OwnerFullName` ORDER BY `w`.`Id`) AS `row`
        FROM `Weapons` AS `w`
    ) AS `t`
    WHERE `t`.`row` <= 10
) AS `t0` ON `g`.`FullName` = `t0`.`OwnerFullName`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `c`.`Name`
""");
        }

        public override async Task FirstOrDefault_on_empty_collection_of_DateTime_in_subquery(bool async)
        {
            //await base.FirstOrDefault_on_empty_collection_of_DateTime_in_subquery(async);
            await AssertQuery(
                async,
                ss => from g in ss.Set<Gear>()
                    let invalidTagIssueDate = (from t in ss.Set<CogTag>()
                        where t.GearNickName == g.FullName
                        orderby t.Id
                        select t.IssueDate).FirstOrDefault()
                    where g.Tag.IssueDate > invalidTagIssueDate
                    select new { g.Nickname, invalidTagIssueDate },
                ss => from g in ss.Set<Gear>()
                    let invalidTagIssueDate = (from t in ss.Set<CogTag>()
                        where t.GearNickName == g.FullName
                        orderby t.Id
                        select t.IssueDate).FirstOrDefault(new DateTime(100,1,1))
                    where g.Tag.IssueDate > invalidTagIssueDate
                    select new { g.Nickname, invalidTagIssueDate });

            AssertSql(
                """
SELECT `g`.`Nickname`, IIF((
        SELECT TOP 1 `t1`.`IssueDate`
        FROM `Tags` AS `t1`
        WHERE `t1`.`GearNickName` = `g`.`FullName`
        ORDER BY `t1`.`Id`) IS NULL, #0100-01-01#, (
        SELECT TOP 1 `t1`.`IssueDate`
        FROM `Tags` AS `t1`
        WHERE `t1`.`GearNickName` = `g`.`FullName`
        ORDER BY `t1`.`Id`)) AS `invalidTagIssueDate`
FROM `Gears` AS `g`
LEFT JOIN `Tags` AS `t` ON `g`.`Nickname` = `t`.`GearNickName` AND `g`.`SquadId` = `t`.`GearSquadId`
WHERE `t`.`IssueDate` > IIF((
        SELECT TOP 1 `t0`.`IssueDate`
        FROM `Tags` AS `t0`
        WHERE `t0`.`GearNickName` = `g`.`FullName`
        ORDER BY `t0`.`Id`) IS NULL, #0100-01-01#, (
        SELECT TOP 1 `t0`.`IssueDate`
        FROM `Tags` AS `t0`
        WHERE `t0`.`GearNickName` = `g`.`FullName`
        ORDER BY `t0`.`Id`))
""");
        }

        public override async Task Project_shadow_properties(bool async)
        {
            await base.Project_shadow_properties(async);

            AssertSql(
    """
SELECT `g`.`Nickname`, `g`.`AssignedCityName`
FROM `Gears` AS `g`
""");
        }

        public override async Task Project_discriminator_columns(bool async)
        {
            await base.Project_discriminator_columns(async);

            AssertSql(
                """
    SELECT `g`.`Nickname`, `g`.`Discriminator`
    FROM `Gears` AS `g`
    """,
                //
                """
    SELECT `g`.`Nickname`, `g`.`Discriminator`
    FROM `Gears` AS `g`
    WHERE `g`.`Discriminator` = 'Officer'
    """,
                //
                """
    SELECT `f`.`Id`, `f`.`Discriminator`
    FROM `Factions` AS `f`
    """,
                //
                """
    SELECT `f`.`Id`, `f`.`Discriminator`
    FROM `Factions` AS `f`
    """,
                //
                """
    SELECT `l`.`Name`, `l`.`Discriminator`
    FROM `LocustLeaders` AS `l`
    """,
                //
                """
    SELECT `l`.`Name`, `l`.`Discriminator`
    FROM `LocustLeaders` AS `l`
    WHERE `l`.`Discriminator` = 'LocustCommander'
    """);
        }

        public override async Task Composite_key_entity_equal(bool async)
        {
            await base.Composite_key_entity_equal(async);

            AssertSql(
                """
    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
    FROM `Gears` AS `g`,
    `Gears` AS `g0`
    WHERE `g`.`Nickname` = `g0`.`Nickname` AND `g`.`SquadId` = `g0`.`SquadId`
    """);
        }

        public override async Task Composite_key_entity_not_equal(bool async)
        {
            await base.Composite_key_entity_not_equal(async);

            AssertSql(
                """
    SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
    FROM `Gears` AS `g`,
    `Gears` AS `g0`
    WHERE `g`.`Nickname` <> `g0`.`Nickname` OR `g`.`SquadId` <> `g0`.`SquadId`
    """);
        }

        public override async Task Composite_key_entity_equal_null(bool async)
        {
            await base.Composite_key_entity_equal_null(async);

            AssertSql(
"""
SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`
FROM `LocustLeaders` AS `l`
LEFT JOIN `Gears` AS `g` ON `l`.`DefeatedByNickname` = `g`.`Nickname` AND `l`.`DefeatedBySquadId` = `g`.`SquadId`
WHERE `l`.`Discriminator` = 'LocustCommander' AND (`g`.`Nickname` IS NULL OR `g`.`SquadId` IS NULL)
""");
        }

        public override async Task Composite_key_entity_not_equal_null(bool async)
        {
            await base.Composite_key_entity_not_equal_null(async);

            AssertSql(
"""
SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`
FROM `LocustLeaders` AS `l`
LEFT JOIN `Gears` AS `g` ON `l`.`DefeatedByNickname` = `g`.`Nickname` AND `l`.`DefeatedBySquadId` = `g`.`SquadId`
WHERE `l`.`Discriminator` = 'LocustCommander' AND `g`.`Nickname` IS NOT NULL AND `g`.`SquadId` IS NOT NULL
""");
        }

        public override async Task Projecting_property_converted_to_nullable_with_comparison(bool async)
        {
            await base.Projecting_property_converted_to_nullable_with_comparison(async);

            AssertSql(
                """
SELECT `t`.`Note`, IIF(`t`.`GearNickName` IS NOT NULL, TRUE, FALSE), `g`.`Nickname`, `g`.`SquadId`, `g`.`HasSoulPatch`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE IIF(`t`.`GearNickName` IS NOT NULL, `g`.`SquadId`, NULL) = 1
""");
        }

        public override async Task Projecting_property_converted_to_nullable_with_addition(bool async)
        {
            await base.Projecting_property_converted_to_nullable_with_addition(async);

            AssertSql(
                """
SELECT `t`.`Note`, IIF(`t`.`GearNickName` IS NOT NULL, TRUE, FALSE), `g`.`Nickname`, `g`.`SquadId`, `g`.`HasSoulPatch`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE (IIF(`t`.`GearNickName` IS NOT NULL, `g`.`SquadId`, NULL) + 1) = 2
""");
        }

        public override async Task Projecting_property_converted_to_nullable_with_addition_and_final_projection(bool async)
        {
            await base.Projecting_property_converted_to_nullable_with_addition_and_final_projection(async);

            AssertSql(
"""
SELECT `t`.`Note`, IIF(`t`.`GearNickName` IS NOT NULL, `g`.`SquadId`, NULL) + 1 AS `Value`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE IIF(`t`.`GearNickName` IS NOT NULL, `g`.`Nickname`, NULL) IS NOT NULL
""");
        }

        public override async Task Projecting_property_converted_to_nullable_with_conditional(bool async)
        {
            await base.Projecting_property_converted_to_nullable_with_conditional(async);

            AssertSql(
                """
SELECT IIF(`t`.`Note` <> 'K.I.A.' OR `t`.`Note` IS NULL, IIF(`t`.`GearNickName` IS NOT NULL, `g`.`SquadId`, NULL), -1)
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
""");
        }

        public override async Task Projecting_property_converted_to_nullable_with_function_call(bool async)
        {
            await base.Projecting_property_converted_to_nullable_with_function_call(async);

            AssertSql(
"""
SELECT MID(IIF(`t`.`GearNickName` IS NOT NULL, `g`.`Nickname`, NULL), 0 + 1, 3)
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
""");
        }

        public override async Task Projecting_property_converted_to_nullable_with_function_call2(bool async)
        {
            await base.Projecting_property_converted_to_nullable_with_function_call2(async);

            AssertSql(
"""
SELECT `t`.`Note`, MID(`t`.`Note`, 0 + 1, IIF(`t`.`GearNickName` IS NOT NULL, `g`.`SquadId`, NULL)) AS `Function`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE IIF(`t`.`GearNickName` IS NOT NULL, `g`.`Nickname`, NULL) IS NOT NULL
""");
        }

        public override async Task Projecting_property_converted_to_nullable_into_element_init(bool async)
        {
            await base.Projecting_property_converted_to_nullable_into_element_init(async);

            AssertSql(
                """
SELECT IIF(`t`.`GearNickName` IS NOT NULL, IIF(LEN(`g`.`Nickname`) IS NULL, NULL, CLNG(LEN(`g`.`Nickname`))), NULL), IIF(`t`.`GearNickName` IS NOT NULL, `g`.`SquadId`, NULL), IIF(`t`.`GearNickName` IS NOT NULL, `g`.`SquadId`, NULL) + 1
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE IIF(`t`.`GearNickName` IS NOT NULL, `g`.`Nickname`, NULL) IS NOT NULL
ORDER BY `t`.`Note`
""");
        }

        public override async Task Projecting_property_converted_to_nullable_into_member_assignment(bool async)
        {
            await base.Projecting_property_converted_to_nullable_into_member_assignment(async);

            AssertSql(
                """
SELECT IIF(`t`.`GearNickName` IS NOT NULL, `g`.`SquadId`, NULL) AS `Id`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE IIF(`t`.`GearNickName` IS NOT NULL, `g`.`Nickname`, NULL) IS NOT NULL
ORDER BY `t`.`Note`
""");
        }

        public override async Task Projecting_property_converted_to_nullable_into_new_array(bool async)
        {
            await base.Projecting_property_converted_to_nullable_into_new_array(async);

            AssertSql(
                """
SELECT IIF(`t`.`GearNickName` IS NOT NULL, IIF(LEN(`g`.`Nickname`) IS NULL, NULL, CLNG(LEN(`g`.`Nickname`))), NULL), IIF(`t`.`GearNickName` IS NOT NULL, `g`.`SquadId`, NULL), IIF(`t`.`GearNickName` IS NOT NULL, `g`.`SquadId`, NULL) + 1
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE IIF(`t`.`GearNickName` IS NOT NULL, `g`.`Nickname`, NULL) IS NOT NULL
ORDER BY `t`.`Note`
""");
        }

        public override async Task Projecting_property_converted_to_nullable_into_unary(bool async)
        {
            await base.Projecting_property_converted_to_nullable_into_unary(async);

            AssertSql(
                """
SELECT `t`.`Note`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE IIF(`t`.`GearNickName` IS NOT NULL, `g`.`Nickname`, NULL) IS NOT NULL AND IIF(`t`.`GearNickName` IS NOT NULL, `g`.`HasSoulPatch`, NULL) = FALSE
ORDER BY `t`.`Note`
""");
        }

        public override async Task Projecting_property_converted_to_nullable_into_member_access(bool async)
        {
            await base.Projecting_property_converted_to_nullable_into_member_access(async);

            AssertSql(
"""
SELECT `g`.`Nickname`
FROM `Gears` AS `g`
LEFT JOIN `Tags` AS `t` ON `g`.`Nickname` = `t`.`GearNickName` AND `g`.`SquadId` = `t`.`GearSquadId`
WHERE DATEPART('m', `t`.`IssueDate`) <> 5
ORDER BY `g`.`Nickname`
""");
        }

        public override async Task Projecting_property_converted_to_nullable_and_use_it_in_order_by(bool async)
        {
            await base.Projecting_property_converted_to_nullable_and_use_it_in_order_by(async);

            AssertSql(
"""
SELECT `t`.`Note`, IIF(`t`.`GearNickName` IS NOT NULL, TRUE, FALSE), `g`.`Nickname`, `g`.`SquadId`, `g`.`HasSoulPatch`
FROM `Tags` AS `t`
LEFT JOIN `Gears` AS `g` ON `t`.`GearNickName` = `g`.`Nickname` AND `t`.`GearSquadId` = `g`.`SquadId`
WHERE IIF(`t`.`GearNickName` IS NOT NULL, `g`.`Nickname`, NULL) IS NOT NULL
ORDER BY IIF(`t`.`GearNickName` IS NOT NULL, `g`.`SquadId`, NULL), `t`.`Note`
""");
        }

        public override async Task Correlated_collection_with_distinct_projecting_identifier_column(bool async)
        {
            await base.Correlated_collection_with_distinct_projecting_identifier_column(async);

            AssertSql(
    """
SELECT `g`.`Nickname`, `g`.`SquadId`, `t`.`Id`, `t`.`Name`
FROM `Gears` AS `g`
OUTER APPLY (
    SELECT DISTINCT `w`.`Id`, `w`.`Name`
    FROM `Weapons` AS `w`
    WHERE `g`.`FullName` = `w`.`OwnerFullName`
) AS `t`
ORDER BY `g`.`Nickname`, `g`.`SquadId`
""");
        }

        public override async Task Correlated_collection_with_distinct_projecting_identifier_column_and_correlation_key(bool async)
        {
            await base.Correlated_collection_with_distinct_projecting_identifier_column_and_correlation_key(async);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `w0`.`Id`, `w0`.`Name`, `w0`.`OwnerFullName`
FROM `Gears` AS `g`
LEFT JOIN (
    SELECT DISTINCT `w`.`Id`, `w`.`Name`, `w`.`OwnerFullName`
    FROM `Weapons` AS `w`
) AS `w0` ON `g`.`FullName` = `w0`.`OwnerFullName`
ORDER BY `g`.`Nickname`, `g`.`SquadId`
""");
        }

        public override async Task Correlated_collection_with_distinct_projecting_identifier_column_composite_key(bool async)
        {
            await base.Correlated_collection_with_distinct_projecting_identifier_column_composite_key(async);

            AssertSql(
                """
SELECT `s`.`Id`, `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`HasSoulPatch`
FROM `Squads` AS `s`
LEFT JOIN (
    SELECT DISTINCT `g`.`Nickname`, `g`.`SquadId`, `g`.`HasSoulPatch`
    FROM `Gears` AS `g`
) AS `g0` ON `s`.`Id` = `g0`.`SquadId`
ORDER BY `s`.`Id`, `g0`.`Nickname`
""");
        }

        public override async Task Correlated_collection_with_distinct_not_projecting_identifier_column(bool async)
        {
            await base.Correlated_collection_with_distinct_not_projecting_identifier_column(async);

            AssertSql(
    """
SELECT `g`.`Nickname`, `g`.`SquadId`, `t`.`Name`, `t`.`IsAutomatic`
FROM `Gears` AS `g`
OUTER APPLY (
    SELECT DISTINCT `w`.`Name`, `w`.`IsAutomatic`
    FROM `Weapons` AS `w`
    WHERE `g`.`FullName` = `w`.`OwnerFullName`
) AS `t`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `t`.`Name`
""");
        }

        public override async Task
            Correlated_collection_with_groupby_not_projecting_identifier_column_but_only_grouping_key_in_final_projection(bool async)
        {
            await base.Correlated_collection_with_groupby_not_projecting_identifier_column_but_only_grouping_key_in_final_projection(async);

            AssertSql(
    """
SELECT `g`.`Nickname`, `g`.`SquadId`, `t`.`Key`
FROM `Gears` AS `g`
OUTER APPLY (
    SELECT `w`.`IsAutomatic` AS `Key`
    FROM `Weapons` AS `w`
    WHERE `g`.`FullName` = `w`.`OwnerFullName`
    GROUP BY `w`.`IsAutomatic`
) AS `t`
ORDER BY `g`.`Nickname`, `g`.`SquadId`
""");
        }

        public override async Task
            Correlated_collection_with_groupby_not_projecting_identifier_column_with_group_aggregate_in_final_projection(bool async)
        {
            await base.Correlated_collection_with_groupby_not_projecting_identifier_column_with_group_aggregate_in_final_projection(async);

            AssertSql(
    """
SELECT `g`.`Nickname`, `g`.`SquadId`, `t`.`Key`, `t`.`Count`
FROM `Gears` AS `g`
OUTER APPLY (
    SELECT `w`.`IsAutomatic` AS `Key`, COUNT(*) AS `Count`
    FROM `Weapons` AS `w`
    WHERE `g`.`FullName` = `w`.`OwnerFullName`
    GROUP BY `w`.`IsAutomatic`
) AS `t`
ORDER BY `g`.`Nickname`, `g`.`SquadId`
""");
        }

        public override async Task
            Correlated_collection_with_groupby_not_projecting_identifier_column_with_group_aggregate_in_final_projection_multiple_grouping_keys(
                bool async)
        {
            await base
                .Correlated_collection_with_groupby_not_projecting_identifier_column_with_group_aggregate_in_final_projection_multiple_grouping_keys(
                    async);

            AssertSql(
    """
SELECT `g`.`Nickname`, `g`.`SquadId`, `t`.`IsAutomatic`, `t`.`Name`, `t`.`Count`
FROM `Gears` AS `g`
OUTER APPLY (
    SELECT `w`.`IsAutomatic`, `w`.`Name`, COUNT(*) AS `Count`
    FROM `Weapons` AS `w`
    WHERE `g`.`FullName` = `w`.`OwnerFullName`
    GROUP BY `w`.`IsAutomatic`, `w`.`Name`
) AS `t`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `t`.`IsAutomatic`
""");
        }

        public override async Task
            Correlated_collection_with_groupby_with_complex_grouping_key_not_projecting_identifier_column_with_group_aggregate_in_final_projection(
                bool async)
        {
            await base
                .Correlated_collection_with_groupby_with_complex_grouping_key_not_projecting_identifier_column_with_group_aggregate_in_final_projection(
                    async);

            AssertSql(
    """
SELECT `g`.`Nickname`, `g`.`SquadId`, `t0`.`Key`, `t0`.`Count`
FROM `Gears` AS `g`
OUTER APPLY (
    SELECT `t`.`Key`, COUNT(*) AS `Count`
    FROM (
        SELECT CAST(LEN(`w`.`Name`) AS int) AS `Key`
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName`
    ) AS `t`
    GROUP BY `t`.`Key`
) AS `t0`
ORDER BY `g`.`Nickname`, `g`.`SquadId`
""");
        }

        public override async Task Correlated_collection_via_SelectMany_with_Distinct_missing_indentifying_columns_in_projection(bool async)
        {
            await base.Correlated_collection_via_SelectMany_with_Distinct_missing_indentifying_columns_in_projection(async);

            AssertSql(
    """
SELECT `g`.`Nickname`, `g`.`SquadId`, `t`.`HasSoulPatch`
FROM `Gears` AS `g`
OUTER APPLY (
    SELECT DISTINCT `g1`.`HasSoulPatch`
    FROM `Weapons` AS `w`
    LEFT JOIN `Gears` AS `g0` ON `w`.`OwnerFullName` = `g0`.`FullName`
    LEFT JOIN `Cities` AS `c` ON `g0`.`AssignedCityName` = `c`.`Name`
    INNER JOIN `Gears` AS `g1` ON `c`.`Name` = `g1`.`CityOfBirthName`
    WHERE `g`.`FullName` = `w`.`OwnerFullName`
) AS `t`
ORDER BY `g`.`Nickname`, `g`.`SquadId`
""");
        }

        public override async Task Correlated_collection_after_distinct_3_levels(bool async)
        {
            await base.Correlated_collection_after_distinct_3_levels(async);

            AssertSql(
    """
SELECT `t`.`Id`, `t`.`Name`, `t1`.`Nickname`, `t1`.`FullName`, `t1`.`HasSoulPatch`, `t1`.`Id`, `t1`.`Name`, `t1`.`Nickname0`, `t1`.`FullName0`, `t1`.`HasSoulPatch0`, `t1`.`Id0`
FROM (
    SELECT DISTINCT `s`.`Id`, `s`.`Name`
    FROM `Squads` AS `s`
) AS `t`
OUTER APPLY (
    SELECT `t0`.`Nickname`, `t0`.`FullName`, `t0`.`HasSoulPatch`, `t2`.`Id`, `t2`.`Name`, `t2`.`Nickname` AS `Nickname0`, `t2`.`FullName` AS `FullName0`, `t2`.`HasSoulPatch` AS `HasSoulPatch0`, `t2`.`Id0`
    FROM (
        SELECT DISTINCT `g`.`Nickname`, `g`.`FullName`, `g`.`HasSoulPatch`
        FROM `Gears` AS `g`
        WHERE `g`.`SquadId` = `t`.`Id`
    ) AS `t0`
    OUTER APPLY (
        SELECT `t`.`Id`, `t`.`Name`, `t0`.`Nickname`, `t0`.`FullName`, `t0`.`HasSoulPatch`, `w`.`Id` AS `Id0`
        FROM `Weapons` AS `w`
        WHERE `w`.`OwnerFullName` = `t0`.`FullName`
    ) AS `t2`
) AS `t1`
ORDER BY `t`.`Id`, `t1`.`Nickname`, `t1`.`FullName`, `t1`.`HasSoulPatch`
""");
        }

        public override async Task Correlated_collection_after_distinct_3_levels_without_original_identifiers(bool async)
        {
            await base.Correlated_collection_after_distinct_3_levels_without_original_identifiers(async);

            AssertSql();
        }

        public override async Task Include_on_entity_that_is_not_present_in_final_projection_but_uses_TypeIs_instead(bool async)
        {
            await base.Include_on_entity_that_is_not_present_in_final_projection_but_uses_TypeIs_instead(async);

            AssertSql(
"""
SELECT `g`.`Nickname`, IIF(`g`.`Discriminator` = 'Officer', TRUE, FALSE) AS `IsOfficer`
FROM `Gears` AS `g`
""");
        }

        public override async Task Comparison_with_value_converted_subclass(bool async)
        {
            await base.Comparison_with_value_converted_subclass(async);

            AssertSql(
                """
SELECT `f`.`Id`, `f`.`CapitalName`, `f`.`Discriminator`, `f`.`Name`, `f`.`ServerAddress`, `f`.`CommanderName`, `f`.`DeputyCommanderName`, `f`.`Eradicated`
FROM `Factions` AS `f`
WHERE `f`.`ServerAddress` = '127.0.0.1'
""");
        }

        public override async Task Project_equality_with_value_converted_property(bool async)
        {
            await base.Project_equality_with_value_converted_property(async);

            AssertSql(
                """
SELECT CASE
    WHEN [m].[Difficulty] = N'Unknown' THEN CAST(1 AS bit)
    ELSE CAST(0 AS bit)
END
FROM [Missions] AS [m]
""");
        }

        public override async Task Contains_on_readonly_enumerable(bool async)
        {
            await base.Contains_on_readonly_enumerable(async);

            AssertSql(
    """
SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM `Weapons` AS `w`
WHERE `w`.`AmmunitionType` = 1
""");
        }

        public override async Task Project_navigation_defined_on_base_from_entity_with_inheritance_using_soft_cast(bool async)
        {
            await base.Project_navigation_defined_on_base_from_entity_with_inheritance_using_soft_cast(async);

            AssertSql(
    """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `t`.`Id`, `t`.`GearNickName`, `t`.`GearSquadId`, `t`.`IssueDate`, `t`.`Note`, CASE
    WHEN `t`.`Id` IS NULL THEN CAST(1 AS bit)
    ELSE CAST(0 AS bit)
END AS `IsNull`, `c`.`Name`, `c`.`Location`, `c`.`Nation`, CASE
    WHEN `c`.`Name` IS NULL THEN CAST(1 AS bit)
    ELSE CAST(0 AS bit)
END AS `IsNull`, `s`.`Id`, `s`.`Banner`, `s`.`Banner5`, `s`.`InternalNumber`, `s`.`Name`, CASE
    WHEN `s`.`Id` IS NULL THEN CAST(1 AS bit)
    ELSE CAST(0 AS bit)
END AS `IsNull`
FROM `Gears` AS `g`
LEFT JOIN `Tags` AS `t` ON `g`.`Nickname` = `t`.`GearNickName` AND `g`.`SquadId` = `t`.`GearSquadId`
LEFT JOIN `Cities` AS `c` ON `g`.`CityOfBirthName` = `c`.`Name`
LEFT JOIN `Squads` AS `s` ON `g`.`SquadId` = `s`.`Id`
""");
        }

        public override async Task Project_navigation_defined_on_derived_from_entity_with_inheritance_using_soft_cast(bool async)
        {
            await base.Project_navigation_defined_on_derived_from_entity_with_inheritance_using_soft_cast(async);

            AssertSql(
    """
SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`, `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, CASE
    WHEN (`g`.`Nickname` IS NULL) OR (`g`.`SquadId` IS NULL) THEN CAST(1 AS bit)
    ELSE CAST(0 AS bit)
END AS `IsNull`, `f`.`Id`, `f`.`CapitalName`, `f`.`Discriminator`, `f`.`Name`, `f`.`ServerAddress`, `f`.`CommanderName`, `f`.`Eradicated`, CASE
    WHEN `f`.`Id` IS NULL THEN CAST(1 AS bit)
    ELSE CAST(0 AS bit)
END AS `IsNull`, `l0`.`Id`, `l0`.`IsOperational`, `l0`.`Name`, CASE
    WHEN `l0`.`Id` IS NULL THEN CAST(1 AS bit)
    ELSE CAST(0 AS bit)
END AS `IsNull`
FROM `LocustLeaders` AS `l`
LEFT JOIN `Gears` AS `g` ON `l`.`DefeatedByNickname` = `g`.`Nickname` AND `l`.`DefeatedBySquadId` = `g`.`SquadId`
LEFT JOIN `Factions` AS `f` ON `l`.`Name` = `f`.`CommanderName`
LEFT JOIN `LocustHighCommands` AS `l0` ON `l`.`HighCommandId` = `l0`.`Id`
""");
        }

        public override async Task Join_entity_with_itself_grouped_by_key_followed_by_include_skip_take(bool async)
        {
            await base.Join_entity_with_itself_grouped_by_key_followed_by_include_skip_take(async);

            AssertSql(
                """
SELECT `s0`.`Nickname`, `s0`.`SquadId`, `s0`.`AssignedCityName`, `s0`.`CityOfBirthName`, `s0`.`Discriminator`, `s0`.`FullName`, `s0`.`HasSoulPatch`, `s0`.`LeaderNickname`, `s0`.`LeaderSquadId`, `s0`.`Rank`, `s0`.`HasSoulPatch0`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM (
    SELECT TOP @p0 `s`.`Nickname`, `s`.`SquadId`, `s`.`AssignedCityName`, `s`.`CityOfBirthName`, `s`.`Discriminator`, `s`.`FullName`, `s`.`HasSoulPatch`, `s`.`LeaderNickname`, `s`.`LeaderSquadId`, `s`.`Rank`, `s`.`HasSoulPatch0`
    FROM (
        SELECT TOP @p + @p0 `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `g1`.`HasSoulPatch` AS `HasSoulPatch0`
        FROM `Gears` AS `g`
        LEFT JOIN (
            SELECT MIN(IIF(LEN(`g0`.`Nickname`) IS NULL, NULL, CLNG(LEN(`g0`.`Nickname`)))) AS `c`, `g0`.`HasSoulPatch`
            FROM `Gears` AS `g0`
            WHERE `g0`.`Nickname` <> 'Dom'
            GROUP BY `g0`.`HasSoulPatch`
        ) AS `g1` ON IIF(LEN(`g`.`Nickname`) IS NULL, NULL, CLNG(LEN(`g`.`Nickname`))) = `g1`.`c`
        WHERE `g1`.`c` IS NOT NULL
        ORDER BY `g`.`Nickname`
    ) AS `s`
    ORDER BY `s`.`Nickname` DESC
) AS `s0`
LEFT JOIN `Weapons` AS `w` ON `s0`.`FullName` = `w`.`OwnerFullName`
ORDER BY `s0`.`Nickname`, `s0`.`SquadId`, NOT (`s0`.`HasSoulPatch0`)
""");
        }

        public override async Task Where_bool_column_and_Contains(bool async)
        {
            await base.Where_bool_column_and_Contains(async);
            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE `g`.`HasSoulPatch` = TRUE AND `g`.`HasSoulPatch` IN (FALSE, TRUE)
""");
        }

        public override async Task Where_bool_column_or_Contains(bool async)
        {
            await base.Where_bool_column_or_Contains(async);
            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE `g`.`HasSoulPatch` = TRUE AND `g`.`HasSoulPatch` IN (FALSE, TRUE)
""");
        }

        public override async Task Parameter_used_multiple_times_take_appropriate_inferred_type_mapping(bool async)
        {
            await base.Parameter_used_multiple_times_take_appropriate_inferred_type_mapping(async);

            AssertSql(
                """
@place='Ephyra's location' (Size = 255), @place0='Ephyra's location' (Size = 100)
@place='Ephyra's location' (Size = 255)

SELECT `c`.`Name`, `c`.`Location`, `c`.`Nation`
FROM `Cities` AS `c`
WHERE `c`.`Nation` = @place OR `c`.`Location` = @place0 OR `c`.`Location` = @place
""");
        }

        public override async Task Enum_matching_take_value_gets_different_type_mapping(bool async)
        {
            await base.Enum_matching_take_value_gets_different_type_mapping(async);

            AssertSql(
                """
@value='1'

SELECT TOP @p `g`.`Rank` BAND @value
FROM `Gears` AS `g`
ORDER BY `g`.`Nickname`
""");
        }

        public override async Task Include_after_Select_throws(bool async)
        {
            await base.Include_after_Select_throws(async);

            AssertSql(
                """
SELECT `f`.`Id`, `f`.`CapitalName`, `f`.`Discriminator`, `f`.`Name`, `f`.`ServerAddress`, `f`.`CommanderName`, `f`.`DeputyCommanderName`, `f`.`Eradicated`, `c`.`Name`, `c`.`Location`, `c`.`Nation`
FROM `Factions` AS `f`
LEFT JOIN `Cities` AS `c` ON `f`.`CapitalName` = `c`.`Name`
""");
        }

        public override async Task GroupBy_Select_sum(bool async)
        {
            await base.GroupBy_Select_sum(async);

            AssertSql(
                """
SELECT IIF(SUM(`m`.`Rating`) IS NULL, 0.0, SUM(`m`.`Rating`))
FROM `Missions` AS `m`
GROUP BY `m`.`CodeName`
""");
        }

        public override async Task String_concat_nullable_expressions_are_coalesced(bool async)
        {
            await base.String_concat_nullable_expressions_are_coalesced(async);

            AssertSql(
                """
SELECT ((`g`.`FullName` & '') & IIF(`g`.`LeaderNickname` IS NULL, '', `g`.`LeaderNickname`)) & ''
FROM `Gears` AS `g`
""");
        }

        public override async Task Cast_to_derived_type_causes_client_eval(bool async)
        {
            await base.Cast_to_derived_type_causes_client_eval(async);

            AssertSql(
    """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
""");
        }

        public override async Task Trying_to_access_unmapped_property_in_projection(bool async)
        {
            await base.Trying_to_access_unmapped_property_in_projection(async);

            AssertSql(
    """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
""");
        }

        public override async Task Basic_query_gears(bool async)
        {
            await base.Basic_query_gears(async);

            AssertSql(
    """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
""");
        }

        public override async Task Include_on_derived_entity_with_cast(bool async)
        {
            await base.Include_on_derived_entity_with_cast(async);

            AssertSql(
                """
SELECT `f`.`Id`, `f`.`CapitalName`, `f`.`Discriminator`, `f`.`Name`, `f`.`ServerAddress`, `f`.`CommanderName`, `f`.`DeputyCommanderName`, `f`.`Eradicated`, `c`.`Name`, `c`.`Location`, `c`.`Nation`
FROM `Factions` AS `f`
LEFT JOIN `Cities` AS `c` ON `f`.`CapitalName` = `c`.`Name`
ORDER BY `f`.`Id`
""");
        }

        public override async Task Where_equals_method_on_nullable_with_object_overload(bool async)
        {
            await base.Where_equals_method_on_nullable_with_object_overload(async);

            AssertSql(
                """
SELECT `m`.`Id`, `m`.`CodeName`, `m`.`Date`, `m`.`Difficulty`, `m`.`Duration`, `m`.`Rating`, `m`.`Time`, `m`.`Timeline`
FROM `Missions` AS `m`
WHERE `m`.`Rating` IS NULL
""");
        }

        public override async Task Cast_to_derived_followed_by_multiple_includes(bool async)
        {
            await base.Cast_to_derived_followed_by_multiple_includes(async);

            AssertSql(
                """
    SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`, `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
    FROM (`LocustLeaders` AS `l`
    LEFT JOIN `Gears` AS `g` ON `l`.`DefeatedByNickname` = `g`.`Nickname` AND `l`.`DefeatedBySquadId` = `g`.`SquadId`)
    LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`
    WHERE `l`.`Name` LIKE '%Queen%'
    ORDER BY `l`.`Name`, `g`.`Nickname`, `g`.`SquadId`
    """);
        }

        public override async Task Sum_with_no_data_nullable_double(bool async)
        {
            await base.Sum_with_no_data_nullable_double(async);
            AssertSql(
                """
SELECT IIF(SUM(`m`.`Rating`) IS NULL, 0.0, SUM(`m`.`Rating`))
FROM `Missions` AS `m`
WHERE `m`.`CodeName` = 'Operation Foobar'
""");
        }

        public override async Task Include_after_SelectMany_throws(bool async)
        {
            await base.Include_after_SelectMany_throws(async);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `s`.`Id`, `s`.`Banner`, `s`.`Banner5`, `s`.`InternalNumber`, `s`.`Name`
FROM ((`Factions` AS `f`
LEFT JOIN `Cities` AS `c` ON `f`.`CapitalName` = `c`.`Name`)
LEFT JOIN `Gears` AS `g` ON `c`.`Name` = `g`.`CityOfBirthName`)
LEFT JOIN `Squads` AS `s` ON `g`.`SquadId` = `s`.`Id`
WHERE `c`.`Name` IS NOT NULL AND `g`.`CityOfBirthName` IS NOT NULL AND `g`.`SquadId` IS NOT NULL AND `s`.`Id` IS NOT NULL
""");
        }

        public override async Task Project_derivied_entity_with_convert_to_parent(bool async)
        {
            await base.Project_derivied_entity_with_convert_to_parent(async);

            AssertSql(
                """
SELECT `f`.`Id`, `f`.`CapitalName`, `f`.`Discriminator`, `f`.`Name`, `f`.`ServerAddress`, `f`.`CommanderName`, `f`.`DeputyCommanderName`, `f`.`Eradicated`
FROM `Factions` AS `f`
""");
        }

        public override async Task Project_entity_and_collection_element(bool async)
        {
            await base.Project_entity_and_collection_element(async);

            AssertSql(
    """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `s`.`Id`, `s`.`Banner`, `s`.`Banner5`, `s`.`InternalNumber`, `s`.`Name`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`, `t0`.`Id`, `t0`.`AmmunitionType`, `t0`.`IsAutomatic`, `t0`.`Name`, `t0`.`OwnerFullName`, `t0`.`SynergyWithId`
FROM `Gears` AS `g`
INNER JOIN `Squads` AS `s` ON `g`.`SquadId` = `s`.`Id`
LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`
LEFT JOIN (
    SELECT `t`.`Id`, `t`.`AmmunitionType`, `t`.`IsAutomatic`, `t`.`Name`, `t`.`OwnerFullName`, `t`.`SynergyWithId`
    FROM (
        SELECT `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`, ROW_NUMBER() OVER(PARTITION BY `w0`.`OwnerFullName` ORDER BY `w0`.`Id`) AS `row`
        FROM `Weapons` AS `w0`
    ) AS `t`
    WHERE `t`.`row` <= 1
) AS `t0` ON `g`.`FullName` = `t0`.`OwnerFullName`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `s`.`Id`
""");
        }

        public override async Task Correlated_collection_with_distinct_not_projecting_identifier_column_also_projecting_complex_expressions(
            bool async)
        {
            await base.Correlated_collection_with_distinct_not_projecting_identifier_column_also_projecting_complex_expressions(async);

            AssertSql();
        }

        public override async Task Client_eval_followed_by_aggregate_operation(bool async)
        {
            await base.Client_eval_followed_by_aggregate_operation(async);

            AssertSql();
        }

        public override async Task Client_member_and_unsupported_string_Equals_in_the_same_query(bool async)
        {
            await base.Client_member_and_unsupported_string_Equals_in_the_same_query(async);

            AssertSql();
        }

        public override async Task Client_side_equality_with_parameter_works_with_optional_navigations(bool async)
        {
            await base.Client_side_equality_with_parameter_works_with_optional_navigations(async);

            AssertSql();
        }

        public override async Task Correlated_collection_order_by_constant_null_of_non_mapped_type(bool async)
        {
            await base.Correlated_collection_order_by_constant_null_of_non_mapped_type(async);

            AssertSql();
        }

        public override async Task GetValueOrDefault_on_DateTimeOffset(bool async)
        {
            await base.GetValueOrDefault_on_DateTimeOffset(async);

            AssertSql();
        }

        public override async Task Where_coalesce_with_anonymous_types(bool async)
        {
            await base.Where_coalesce_with_anonymous_types(async);

            AssertSql();
        }

        public override async Task Projecting_correlated_collection_followed_by_Distinct(bool async)
        {
            await base.Projecting_correlated_collection_followed_by_Distinct(async);

            AssertSql();
        }

        public override async Task Projecting_some_properties_as_well_as_correlated_collection_followed_by_Distinct(bool async)
        {
            await base.Projecting_some_properties_as_well_as_correlated_collection_followed_by_Distinct(async);

            AssertSql();
        }

        public override async Task Projecting_entity_as_well_as_correlated_collection_followed_by_Distinct(bool async)
        {
            await base.Projecting_entity_as_well_as_correlated_collection_followed_by_Distinct(async);

            AssertSql();
        }

        public override async Task Projecting_entity_as_well_as_complex_correlated_collection_followed_by_Distinct(bool async)
        {
            await base.Projecting_entity_as_well_as_complex_correlated_collection_followed_by_Distinct(async);

            AssertSql();
        }

        public override async Task Projecting_entity_as_well_as_correlated_collection_of_scalars_followed_by_Distinct(bool async)
        {
            await base.Projecting_entity_as_well_as_correlated_collection_of_scalars_followed_by_Distinct(async);

            AssertSql();
        }

        public override async Task Correlated_collection_with_distinct_3_levels(bool async)
        {
            await base.Correlated_collection_with_distinct_3_levels(async);

            AssertSql();
        }

        public override async Task Checked_context_throws_on_client_evaluation(bool async)
        {
            await base.Checked_context_throws_on_client_evaluation(async);

            AssertSql();
        }

        public override async Task Trying_to_access_unmapped_property_throws_informative_error(bool async)
        {
            await base.Trying_to_access_unmapped_property_throws_informative_error(async);

            AssertSql();
        }

        public override async Task Trying_to_access_unmapped_property_inside_aggregate(bool async)
        {
            await base.Trying_to_access_unmapped_property_inside_aggregate(async);

            AssertSql();
        }

        public override async Task Trying_to_access_unmapped_property_inside_subquery(bool async)
        {
            await base.Trying_to_access_unmapped_property_inside_subquery(async);

            AssertSql();
        }

        public override async Task Trying_to_access_unmapped_property_inside_join_key_selector(bool async)
        {
            await base.Trying_to_access_unmapped_property_inside_join_key_selector(async);

            AssertSql();
        }

        public override async Task Client_projection_with_nested_unmapped_property_bubbles_up_translation_failure_info(bool async)
        {
            await base.Client_projection_with_nested_unmapped_property_bubbles_up_translation_failure_info(async);

            AssertSql();
        }

        public override async Task Include_after_select_with_cast_throws(bool async)
        {
            await base.Include_after_select_with_cast_throws(async);

            AssertSql();
        }

        public override async Task Include_after_select_with_entity_projection_throws(bool async)
        {
            await base.Include_after_select_with_entity_projection_throws(async);

            AssertSql();
        }

        public override async Task Include_after_select_anonymous_projection_throws(bool async)
        {
            await base.Include_after_select_anonymous_projection_throws(async);

            AssertSql();
        }

        public override async Task Group_by_with_aggregate_max_on_entity_type(bool async)
        {
            await base.Group_by_with_aggregate_max_on_entity_type(async);

            AssertSql();
        }

        public override async Task Include_collection_and_invalid_navigation_using_string_throws(bool async)
        {
            await base.Include_collection_and_invalid_navigation_using_string_throws(async);

            AssertSql();
        }

        public override async Task Include_with_concat(bool async)
        {
            await base.Include_with_concat(async);

            AssertSql();
        }

        public override async Task Join_with_complex_key_selector(bool async)
        {
            await base.Join_with_complex_key_selector(async);

            AssertSql(
"""
SELECT `s`.`Id`, `t0`.`Id` AS `TagId`
FROM `Squads` AS `s`,
(
    SELECT `t`.`Id`
    FROM `Tags` AS `t`
    WHERE `t`.`Note` = 'Marcus'' Tag'
) AS `t0`
""");
        }

        public override async Task Streaming_correlated_collection_issue_11403_returning_ordered_enumerable_throws(bool async)
        {
            await base.Streaming_correlated_collection_issue_11403_returning_ordered_enumerable_throws(async);

            AssertSql();
        }

        public override async Task Select_correlated_filtered_collection_returning_queryable_throws(bool async)
        {
            await base.Select_correlated_filtered_collection_returning_queryable_throws(async);

            AssertSql();
        }

        public override async Task Client_method_on_collection_navigation_in_predicate(bool async)
        {
            await base.Client_method_on_collection_navigation_in_predicate(async);

            AssertSql();
        }

        public override async Task Client_method_on_collection_navigation_in_predicate_accessed_by_ef_property(bool async)
        {
            await base.Client_method_on_collection_navigation_in_predicate_accessed_by_ef_property(async);

            AssertSql();
        }

        public override async Task Client_method_on_collection_navigation_in_order_by(bool async)
        {
            await base.Client_method_on_collection_navigation_in_order_by(async);

            AssertSql();
        }

        public override async Task Client_method_on_collection_navigation_in_additional_from_clause(bool async)
        {
            await base.Client_method_on_collection_navigation_in_additional_from_clause(async);

            AssertSql();
        }

        public override async Task Include_multiple_one_to_one_and_one_to_many_self_reference(bool async)
        {
            await base.Include_multiple_one_to_one_and_one_to_many_self_reference(async);

            AssertSql();
        }

        public override async Task Include_multiple_one_to_one_and_one_to_one_and_one_to_many(bool async)
        {
            await base.Include_multiple_one_to_one_and_one_to_one_and_one_to_many(async);

            AssertSql();
        }

        public override async Task Include_multiple_include_then_include(bool async)
        {
            await base.Include_multiple_include_then_include(async);

            AssertSql();
        }

        public override async Task Select_Where_Navigation_Client(bool async)
        {
            await base.Select_Where_Navigation_Client(async);

            AssertSql();
        }

        public override async Task Where_subquery_equality_to_null_with_composite_key(bool async)
        {
            await base.Where_subquery_equality_to_null_with_composite_key(async);

            AssertSql(
"""
SELECT `s`.`Id`, `s`.`Banner`, `s`.`Banner5`, `s`.`InternalNumber`, `s`.`Name`
FROM `Squads` AS `s`
WHERE NOT EXISTS (
    SELECT 1
    FROM `Gears` AS `g`
    WHERE `s`.`Id` = `g`.`SquadId`)
""");
        }

        public override async Task Where_subquery_equality_to_null_with_composite_key_should_match_nulls(bool async)
        {
            await base.Where_subquery_equality_to_null_with_composite_key_should_match_nulls(async);

            AssertSql(
                """
SELECT `s`.`Id`, `s`.`Banner`, `s`.`Banner5`, `s`.`InternalNumber`, `s`.`Name`
FROM `Squads` AS `s`
WHERE NOT EXISTS (
    SELECT 1
    FROM `Gears` AS `g`
    WHERE `s`.`Id` = `g`.`SquadId` AND `g`.`FullName` = 'Anthony Carmine')
""");
        }

        public override async Task Where_subquery_equality_to_null_without_composite_key(bool async)
        {
            await base.Where_subquery_equality_to_null_without_composite_key(async);

            AssertSql(
"""
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE NOT EXISTS (
    SELECT 1
    FROM `Weapons` AS `w`
    WHERE `g`.`FullName` = `w`.`OwnerFullName`)
""");
        }

        public override async Task Where_subquery_equality_to_null_without_composite_key_should_match_null(bool async)
        {
            await base.Where_subquery_equality_to_null_without_composite_key_should_match_null(async);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE NOT EXISTS (
    SELECT 1
    FROM `Weapons` AS `w`
    WHERE `g`.`FullName` = `w`.`OwnerFullName` AND `w`.`Name` = 'Hammer of Dawn')
""");
        }

        public override async Task Include_reference_on_derived_type_using_EF_Property(bool async)
        {
            await base.Include_reference_on_derived_type_using_EF_Property(async);

            AssertSql(
    """
SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`, `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `LocustLeaders` AS `l`
LEFT JOIN `Gears` AS `g` ON `l`.`DefeatedByNickname` = `g`.`Nickname` AND `l`.`DefeatedBySquadId` = `g`.`SquadId`
""");
        }

        public override async Task Include_collection_on_derived_type_using_EF_Property(bool async)
        {
            await base.Include_collection_on_derived_type_using_EF_Property(async);

            AssertSql(
    """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
FROM `Gears` AS `g`
LEFT JOIN `Gears` AS `g0` ON `g`.`Nickname` = `g0`.`LeaderNickname` AND `g`.`SquadId` = `g0`.`LeaderSquadId`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `g0`.`Nickname`
""");
        }

        public override async Task EF_Property_based_Include_navigation_on_derived_type(bool async)
        {
            await base.EF_Property_based_Include_navigation_on_derived_type(async);

            AssertSql(
    """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
FROM `Gears` AS `g`
LEFT JOIN `Gears` AS `g0` ON `g`.`Nickname` = `g0`.`LeaderNickname` AND `g`.`SquadId` = `g0`.`LeaderSquadId`
WHERE `g`.`Discriminator` = 'Officer'
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `g0`.`Nickname`
""");
        }

        public override async Task ElementAt_basic_with_OrderBy(bool async)
        {
            await base.ElementAt_basic_with_OrderBy(async);

            AssertSql(
                """
SELECT `g1`.`Nickname`, `g1`.`SquadId`, `g1`.`AssignedCityName`, `g1`.`CityOfBirthName`, `g1`.`Discriminator`, `g1`.`FullName`, `g1`.`HasSoulPatch`, `g1`.`LeaderNickname`, `g1`.`LeaderSquadId`, `g1`.`Rank`
FROM (
    SELECT TOP 1 `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
    FROM (
        SELECT TOP @p + 1 `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
        FROM `Gears` AS `g`
        ORDER BY `g`.`FullName`
    ) AS `g0`
    ORDER BY `g0`.`FullName` DESC
) AS `g1`
ORDER BY `g1`.`FullName`
""");
        }

        public override async Task ElementAtOrDefault_basic_with_OrderBy(bool async)
        {
            await base.ElementAtOrDefault_basic_with_OrderBy(async);

            AssertSql(
                """
SELECT `g1`.`Nickname`, `g1`.`SquadId`, `g1`.`AssignedCityName`, `g1`.`CityOfBirthName`, `g1`.`Discriminator`, `g1`.`FullName`, `g1`.`HasSoulPatch`, `g1`.`LeaderNickname`, `g1`.`LeaderSquadId`, `g1`.`Rank`
FROM (
    SELECT TOP 1 `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
    FROM (
        SELECT TOP @p + 1 `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
        FROM `Gears` AS `g`
        ORDER BY `g`.`FullName`
    ) AS `g0`
    ORDER BY `g0`.`FullName` DESC
) AS `g1`
ORDER BY `g1`.`FullName`
""");
        }

        public override async Task ElementAtOrDefault_basic_with_OrderBy_parameter(bool async)
        {
            await base.ElementAtOrDefault_basic_with_OrderBy_parameter(async);

            AssertSql(
                """
SELECT `g1`.`Nickname`, `g1`.`SquadId`, `g1`.`AssignedCityName`, `g1`.`CityOfBirthName`, `g1`.`Discriminator`, `g1`.`FullName`, `g1`.`HasSoulPatch`, `g1`.`LeaderNickname`, `g1`.`LeaderSquadId`, `g1`.`Rank`
FROM (
    SELECT TOP 1 `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`
    FROM (
        SELECT TOP @p + 1 `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
        FROM `Gears` AS `g`
        ORDER BY `g`.`FullName`
    ) AS `g0`
    ORDER BY `g0`.`FullName` DESC
) AS `g1`
ORDER BY `g1`.`FullName`
""");
        }

        public override async Task Where_subquery_with_ElementAtOrDefault_equality_to_null_with_composite_key(bool async)
        {
            await base.Where_subquery_with_ElementAtOrDefault_equality_to_null_with_composite_key(async);

            AssertSql(
                """
SELECT [s].[Id], [s].[Banner], [s].[Banner5], [s].[InternalNumber], [s].[Name]
FROM [Squads] AS [s]
WHERE NOT EXISTS (
    SELECT 1
    FROM [Gears] AS [g]
    WHERE [s].[Id] = [g].[SquadId]
    ORDER BY [g].[Nickname]
    OFFSET 2 ROWS)
""");
        }

        public override async Task Where_subquery_with_ElementAt_using_column_as_index(bool async)
        {
            await base.Where_subquery_with_ElementAt_using_column_as_index(async);

            AssertSql(
                """
SELECT [s].[Id], [s].[Banner], [s].[Banner5], [s].[InternalNumber], [s].[Name]
FROM [Squads] AS [s]
WHERE (
    SELECT [g].[Nickname]
    FROM [Gears] AS [g]
    WHERE [s].[Id] = [g].[SquadId]
    ORDER BY [g].[Nickname]
    OFFSET [s].[Id] ROWS FETCH NEXT 1 ROWS ONLY) = N'Cole Train'
""");
        }

        public override async Task Using_indexer_on_byte_array_and_string_in_projection(bool async)
        {
            await base.Using_indexer_on_byte_array_and_string_in_projection(async);

            AssertSql(
                """
SELECT `s`.`Id`, ASCB(MIDB(`s`.`Banner`, 0 + 1, 1)), `s`.`Name`
FROM `Squads` AS `s`
""");
        }

        public override async Task Set_operator_with_navigation_in_projection_groupby_aggregate(bool async)
        {
            await base.Set_operator_with_navigation_in_projection_groupby_aggregate(async);

            AssertSql(
                """
SELECT `s`.`Name`, (
    SELECT IIF(SUM(IIF(LEN(`c`.`Location`) IS NULL, NULL, CLNG(LEN(`c`.`Location`)))) IS NULL, 0, SUM(IIF(LEN(`c`.`Location`) IS NULL, NULL, CLNG(LEN(`c`.`Location`)))))
    FROM (`Gears` AS `g2`
    INNER JOIN `Squads` AS `s0` ON `g2`.`SquadId` = `s0`.`Id`)
    INNER JOIN `Cities` AS `c` ON `g2`.`CityOfBirthName` = `c`.`Name`
    WHERE 'Marcus' IN (
        SELECT `u0`.`Nickname`
        FROM (
            SELECT `g3`.`Nickname`
            FROM `Gears` AS `g3`
            UNION ALL
            SELECT `g4`.`Nickname`
            FROM `Gears` AS `g4`
        ) AS `u0`
    ) AND (`s`.`Name` = `s0`.`Name` OR (`s`.`Name` IS NULL AND `s0`.`Name` IS NULL))) AS `SumOfLengths`
FROM `Gears` AS `g`
INNER JOIN `Squads` AS `s` ON `g`.`SquadId` = `s`.`Id`
WHERE 'Marcus' IN (
    SELECT `u`.`Nickname`
    FROM (
        SELECT `g0`.`Nickname`
        FROM `Gears` AS `g0`
        UNION ALL
        SELECT `g1`.`Nickname`
        FROM `Gears` AS `g1`
    ) AS `u`
)
GROUP BY `s`.`Name`
""");
        }

        public override async Task Nav_expansion_inside_Contains_argument(bool async)
        {
            await base.Nav_expansion_inside_Contains_argument(async);

            AssertSql(
"""
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE IIF(EXISTS (
        SELECT 1
        FROM `Weapons` AS `w`
        WHERE `g`.`FullName` = `w`.`OwnerFullName`), 1, 0) IN (1, -1)
""");
        }

        public override async Task Nav_expansion_with_member_pushdown_inside_Contains_argument(bool async)
        {
            await base.Nav_expansion_with_member_pushdown_inside_Contains_argument(async);

            AssertSql(
"""
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE (
    SELECT TOP 1 `w`.`Name`
    FROM `Weapons` AS `w`
    WHERE `g`.`FullName` = `w`.`OwnerFullName`
    ORDER BY `w`.`Id`) IN ('Marcus'' Lancer', 'Dom''s Gnasher')
""");
        }

        public override async Task Subquery_inside_Take_argument(bool async)
        {
            await base.Subquery_inside_Take_argument(async);

            AssertSql(
                """
@__numbers_0='[0,1,2]' (Size = 4000)

SELECT [g].[Nickname], [g].[SquadId], [t0].[Id], [t0].[AmmunitionType], [t0].[IsAutomatic], [t0].[Name], [t0].[OwnerFullName], [t0].[SynergyWithId]
FROM [Gears] AS [g]
LEFT JOIN (
    SELECT [t].[Id], [t].[AmmunitionType], [t].[IsAutomatic], [t].[Name], [t].[OwnerFullName], [t].[SynergyWithId]
    FROM (
        SELECT [w].[Id], [w].[AmmunitionType], [w].[IsAutomatic], [w].[Name], [w].[OwnerFullName], [w].[SynergyWithId], ROW_NUMBER() OVER(PARTITION BY [w].[OwnerFullName] ORDER BY [w].[Id]) AS [row]
        FROM [Weapons] AS [w]
    ) AS [t]
    WHERE [t].[row] <= COALESCE((
        SELECT [n].[value]
        FROM OPENJSON(@__numbers_0) WITH ([value] int '$') AS [n]
        ORDER BY [n].[value]
        OFFSET 1 ROWS FETCH NEXT 1 ROWS ONLY), 0)
) AS [t0] ON [g].[FullName] = [t0].[OwnerFullName]
ORDER BY [g].[Nickname], [g].[SquadId], [t0].[OwnerFullName], [t0].[Id]
""");
        }

        public override async Task Nav_expansion_inside_Skip_correlated_to_source(bool async)
        {
            await base.Nav_expansion_inside_Skip_correlated_to_source(async);

            AssertSql();
        }

        public override async Task Nav_expansion_inside_Take_correlated_to_source(bool async)
        {
            await base.Nav_expansion_inside_Take_correlated_to_source(async);

            AssertSql();
        }

        public override async Task Nav_expansion_with_member_pushdown_inside_Take_correlated_to_source(bool async)
        {
            await base.Nav_expansion_with_member_pushdown_inside_Take_correlated_to_source(async);

            AssertSql();
        }

        public override async Task Nav_expansion_inside_ElementAt_correlated_to_source(bool async)
        {
            await base.Nav_expansion_inside_ElementAt_correlated_to_source(async);

            AssertSql();
        }

        public override async Task Include_one_to_many_on_composite_key_then_orderby_key_properties(bool async)
        {
            await base.Include_one_to_many_on_composite_key_then_orderby_key_properties(async);

            AssertSql(
                """
SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM `Gears` AS `g`
LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`
ORDER BY `g`.`SquadId`, `g`.`Nickname`
""");
        }

        public override async Task Find_underlying_property_after_GroupJoin_DefaultIfEmpty(bool async)
        {
            await base.Find_underlying_property_after_GroupJoin_DefaultIfEmpty(async);

            AssertSql(
                """
SELECT `g`.`FullName`, IIF(`l0`.`ThreatLevel` IS NULL, NULL, CLNG(`l0`.`ThreatLevel`)) AS `ThreatLevel`
FROM `Gears` AS `g`
LEFT JOIN (
    SELECT `l`.`ThreatLevel`, `l`.`DefeatedByNickname`
    FROM `LocustLeaders` AS `l`
    WHERE `l`.`Discriminator` = 'LocustCommander'
) AS `l0` ON `g`.`Nickname` = `l0`.`DefeatedByNickname`
""");
        }

        public override async Task Join_include_coalesce_simple(bool async)
        {
            await base.Join_include_coalesce_simple(async);

            AssertSql(
                """
SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`, `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`, IIF(`g`.`Nickname` = 'Marcus', TRUE, FALSE)
FROM (`Gears` AS `g`
LEFT JOIN `Gears` AS `g0` ON `g`.`LeaderNickname` = `g0`.`Nickname`)
LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `g0`.`Nickname`, `g0`.`SquadId`
""",
                //
                """
SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`, `g`.`Nickname`, `g`.`SquadId`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM (`Gears` AS `g`
LEFT JOIN `Gears` AS `g0` ON `g`.`LeaderNickname` = `g0`.`Nickname`)
LEFT JOIN `Weapons` AS `w` ON `g0`.`FullName` = `w`.`OwnerFullName`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `g0`.`Nickname`, `g0`.`SquadId`
""",
                //
                """
SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`, `g`.`Nickname`, `g`.`SquadId`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`
FROM ((`Gears` AS `g`
LEFT JOIN `Gears` AS `g0` ON `g`.`LeaderNickname` = `g0`.`Nickname`)
LEFT JOIN `Weapons` AS `w` ON `g0`.`FullName` = `w`.`OwnerFullName`)
LEFT JOIN `Weapons` AS `w0` ON `g`.`FullName` = `w0`.`OwnerFullName`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `g0`.`Nickname`, `g0`.`SquadId`, `w`.`Id`
""");
        }

        public override async Task Join_include_coalesce_nested(bool async)
        {
            await base.Join_include_coalesce_nested(async);

            AssertSql(
                """
SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`, `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`, IIF(`g`.`Nickname` = 'Marcus', TRUE, FALSE)
FROM (`Gears` AS `g`
LEFT JOIN `Gears` AS `g0` ON `g`.`LeaderNickname` = `g0`.`Nickname`)
LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `g0`.`Nickname`, `g0`.`SquadId`
""",
                //
                """
SELECT `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`, `g`.`Nickname`, `g`.`SquadId`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`, `w0`.`Id`, `w0`.`AmmunitionType`, `w0`.`IsAutomatic`, `w0`.`Name`, `w0`.`OwnerFullName`, `w0`.`SynergyWithId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `w1`.`Id`, `w1`.`AmmunitionType`, `w1`.`IsAutomatic`, `w1`.`Name`, `w1`.`OwnerFullName`, `w1`.`SynergyWithId`
FROM (((`Gears` AS `g`
LEFT JOIN `Gears` AS `g0` ON `g`.`LeaderNickname` = `g0`.`Nickname`)
LEFT JOIN `Weapons` AS `w` ON `g0`.`FullName` = `w`.`OwnerFullName`)
LEFT JOIN `Weapons` AS `w0` ON `g0`.`FullName` = `w0`.`OwnerFullName`)
LEFT JOIN `Weapons` AS `w1` ON `g0`.`FullName` = `w1`.`OwnerFullName`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `g0`.`Nickname`, `g0`.`SquadId`, `w`.`Id`, `w0`.`Id`
""");
        }

        public override async Task Join_include_conditional(bool async)
        {
            await base.Join_include_conditional(async);

            AssertSql(
                """
SELECT IIF(`g0`.`Nickname` IS NOT NULL AND `g0`.`SquadId` IS NOT NULL, TRUE, FALSE), `g0`.`Nickname`, `g0`.`SquadId`, `g0`.`AssignedCityName`, `g0`.`CityOfBirthName`, `g0`.`Discriminator`, `g0`.`FullName`, `g0`.`HasSoulPatch`, `g0`.`LeaderNickname`, `g0`.`LeaderSquadId`, `g0`.`Rank`, `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`, `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`, IIF(`g`.`Nickname` = 'Marcus', TRUE, FALSE)
FROM (`Gears` AS `g`
LEFT JOIN `Gears` AS `g0` ON `g`.`LeaderNickname` = `g0`.`Nickname`)
LEFT JOIN `Weapons` AS `w` ON `g`.`FullName` = `w`.`OwnerFullName`
ORDER BY `g`.`Nickname`, `g`.`SquadId`, `g0`.`Nickname`, `g0`.`SquadId`
""");
        }

        public override async Task Derived_reference_is_skipped_when_base_type(bool async)
        {
            await base.Derived_reference_is_skipped_when_base_type(async);

            AssertSql(
                """
SELECT `l`.`Name`, `l`.`Discriminator`, `l`.`LocustHordeId`, `l`.`ThreatLevel`, `l`.`ThreatLevelByte`, `l`.`ThreatLevelNullableByte`, `l`.`DefeatedByNickname`, `l`.`DefeatedBySquadId`, `l`.`HighCommandId`, `l0`.`Id`, `l0`.`IsOperational`, `l0`.`Name`
FROM `LocustLeaders` AS `l`
LEFT JOIN `LocustHighCommands` AS `l0` ON `l`.`HighCommandId` = `l0`.`Id`
""");
        }

        public override async Task Nested_contains_with_enum(bool async)
        {
            await base.Nested_contains_with_enum(async);

            AssertSql(
                """
@key='5f221fb9-66f4-442a-92c9-d97ed5989cc7'
@key='5f221fb9-66f4-442a-92c9-d97ed5989cc7'

SELECT `g`.`Nickname`, `g`.`SquadId`, `g`.`AssignedCityName`, `g`.`CityOfBirthName`, `g`.`Discriminator`, `g`.`FullName`, `g`.`HasSoulPatch`, `g`.`LeaderNickname`, `g`.`LeaderSquadId`, `g`.`Rank`
FROM `Gears` AS `g`
WHERE IIF(`g`.`Rank` = 1, @key, @key) IN ('{0a47bcb7-a1cb-4345-8944-c58f82d6aac7}', '{5f221fb9-66f4-442a-92c9-d97ed5989cc7}')
""",
                //
                """
@key='5f221fb9-66f4-442a-92c9-d97ed5989cc7'
@key='5f221fb9-66f4-442a-92c9-d97ed5989cc7'

SELECT `w`.`Id`, `w`.`AmmunitionType`, `w`.`IsAutomatic`, `w`.`Name`, `w`.`OwnerFullName`, `w`.`SynergyWithId`
FROM `Weapons` AS `w`
WHERE IIF(`w`.`AmmunitionType` = 1, @key, @key) IN ('{0a47bcb7-a1cb-4345-8944-c58f82d6aac7}', '{5f221fb9-66f4-442a-92c9-d97ed5989cc7}')
""");
        }

        public override async Task Non_string_concat_uses_appropriate_type_mapping(bool async)
        {
            await base.Non_string_concat_uses_appropriate_type_mapping(async);

            AssertSql(
                """
SELECT `m`.`Duration`
FROM `Missions` AS `m`
""");
        }

        private void AssertSql(params string[] expected)
            => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);
    }
}
