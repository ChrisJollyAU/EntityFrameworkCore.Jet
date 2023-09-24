// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;
using Xunit.Abstractions;

namespace EntityFrameworkCore.Jet.FunctionalTests.Query
{
    public class InheritanceRelationshipsQueryJetTest
        : InheritanceRelationshipsQueryRelationalTestBase<InheritanceRelationshipsQueryJetFixture>
    {
        public InheritanceRelationshipsQueryJetTest(
            InheritanceRelationshipsQueryJetFixture fixture, ITestOutputHelper testOutputHelper)
            : base(fixture)
        {
            fixture.TestSqlLoggerFactory.Clear();
            Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
        }

        [ConditionalFact]
        public virtual void Check_all_tests_overridden()
            => TestHelpers.AssertAllMethodsOverridden(GetType());

        public override async Task Include_reference_with_inheritance(bool async)
        {
            await base.Include_reference_with_inheritance(async);

            AssertSql(
"""
SELECT `b`.`Id`, `b`.`Discriminator`, `b`.`Name`, `b`.`BaseId`, `b0`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b`.`OwnedReferenceOnBase_Id`, `b`.`OwnedReferenceOnBase_Name`, `b2`.`DerivedInheritanceRelationshipEntityId`, `b2`.`Id`, `b2`.`Name`, `b`.`OwnedReferenceOnDerived_Id`, `b`.`OwnedReferenceOnDerived_Name`, `b0`.`BaseParentId`, `b0`.`Discriminator`, `b0`.`Name`
FROM ((`BaseEntities` AS `b`
LEFT JOIN `BaseReferencesOnBase` AS `b0` ON `b`.`Id` = `b0`.`BaseParentId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b1` ON `b`.`Id` = `b1`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b2` ON `b`.`Id` = `b2`.`DerivedInheritanceRelationshipEntityId`
ORDER BY `b`.`Id`, `b0`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b2`.`DerivedInheritanceRelationshipEntityId`
""");
        }

        public override async Task Include_reference_with_inheritance_reverse(bool async)
        {
            await base.Include_reference_with_inheritance_reverse(async);

            AssertSql(
                """
SELECT `b`.`Id`, `b`.`BaseParentId`, `b`.`Discriminator`, `b`.`Name`, `b0`.`Id`, `b0`.`Discriminator`, `b0`.`Name`, `b0`.`BaseId`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b0`.`OwnedReferenceOnBase_Id`, `b0`.`OwnedReferenceOnBase_Name`, `b2`.`DerivedInheritanceRelationshipEntityId`, `b2`.`Id`, `b2`.`Name`, `b0`.`OwnedReferenceOnDerived_Id`, `b0`.`OwnedReferenceOnDerived_Name`
FROM ((`BaseReferencesOnBase` AS `b`
LEFT JOIN `BaseEntities` AS `b0` ON `b`.`BaseParentId` = `b0`.`Id`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b1` ON `b0`.`Id` = `b1`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b2` ON `b0`.`Id` = `b2`.`DerivedInheritanceRelationshipEntityId`
ORDER BY `b`.`Id`, `b0`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b2`.`DerivedInheritanceRelationshipEntityId`
""");
        }

        public override async Task Include_self_reference_with_inheritance(bool async)
        {
            await base.Include_self_reference_with_inheritance(async);

            AssertSql(
                """
SELECT `b`.`Id`, `b`.`Discriminator`, `b`.`Name`, `b`.`BaseId`, `t`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b`.`OwnedReferenceOnBase_Id`, `b`.`OwnedReferenceOnBase_Name`, `b2`.`DerivedInheritanceRelationshipEntityId`, `b2`.`Id`, `b2`.`Name`, `b`.`OwnedReferenceOnDerived_Id`, `b`.`OwnedReferenceOnDerived_Name`, `t`.`Discriminator`, `t`.`Name`, `t`.`BaseId`, `b3`.`BaseInheritanceRelationshipEntityId`, `b3`.`Id`, `b3`.`Name`, `t`.`OwnedReferenceOnBase_Id`, `t`.`OwnedReferenceOnBase_Name`, `b4`.`DerivedInheritanceRelationshipEntityId`, `b4`.`Id`, `b4`.`Name`, `t`.`OwnedReferenceOnDerived_Id`, `t`.`OwnedReferenceOnDerived_Name`
FROM ((((`BaseEntities` AS `b`
LEFT JOIN (
    SELECT `b0`.`Id`, `b0`.`Discriminator`, `b0`.`Name`, `b0`.`BaseId`, `b0`.`OwnedReferenceOnBase_Id`, `b0`.`OwnedReferenceOnBase_Name`, `b0`.`OwnedReferenceOnDerived_Id`, `b0`.`OwnedReferenceOnDerived_Name`
    FROM `BaseEntities` AS `b0`
    WHERE `b0`.`Discriminator` = 'DerivedInheritanceRelationshipEntity'
) AS `t` ON `b`.`Id` = `t`.`BaseId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b1` ON `b`.`Id` = `b1`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b2` ON `b`.`Id` = `b2`.`DerivedInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b3` ON `t`.`Id` = `b3`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b4` ON `t`.`Id` = `b4`.`DerivedInheritanceRelationshipEntityId`
ORDER BY `b`.`Id`, `t`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b2`.`DerivedInheritanceRelationshipEntityId`, `b2`.`Id`, `b3`.`BaseInheritanceRelationshipEntityId`, `b3`.`Id`, `b4`.`DerivedInheritanceRelationshipEntityId`
""");
        }

        public override async Task Include_self_reference_with_inheritance_reverse(bool async)
        {
            await base.Include_self_reference_with_inheritance_reverse(async);

            AssertSql(
                """
SELECT `b`.`Id`, `b`.`Discriminator`, `b`.`Name`, `b`.`BaseId`, `b0`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b`.`OwnedReferenceOnBase_Id`, `b`.`OwnedReferenceOnBase_Name`, `b2`.`DerivedInheritanceRelationshipEntityId`, `b2`.`Id`, `b2`.`Name`, `b`.`OwnedReferenceOnDerived_Id`, `b`.`OwnedReferenceOnDerived_Name`, `b0`.`Discriminator`, `b0`.`Name`, `b0`.`BaseId`, `b3`.`BaseInheritanceRelationshipEntityId`, `b3`.`Id`, `b3`.`Name`, `b0`.`OwnedReferenceOnBase_Id`, `b0`.`OwnedReferenceOnBase_Name`, `b4`.`DerivedInheritanceRelationshipEntityId`, `b4`.`Id`, `b4`.`Name`, `b0`.`OwnedReferenceOnDerived_Id`, `b0`.`OwnedReferenceOnDerived_Name`
FROM ((((`BaseEntities` AS `b`
LEFT JOIN `BaseEntities` AS `b0` ON `b`.`BaseId` = `b0`.`Id`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b1` ON `b`.`Id` = `b1`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b2` ON `b`.`Id` = `b2`.`DerivedInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b3` ON `b0`.`Id` = `b3`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b4` ON `b0`.`Id` = `b4`.`DerivedInheritanceRelationshipEntityId`
WHERE `b`.`Discriminator` = 'DerivedInheritanceRelationshipEntity'
ORDER BY `b`.`Id`, `b0`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b2`.`DerivedInheritanceRelationshipEntityId`, `b2`.`Id`, `b3`.`BaseInheritanceRelationshipEntityId`, `b3`.`Id`, `b4`.`DerivedInheritanceRelationshipEntityId`
""");
        }
        
        public override async Task Include_reference_with_inheritance_with_filter_reverse(bool async)
        {
            await base.Include_reference_with_inheritance_with_filter_reverse(async);

            AssertSql(
"""
SELECT `b`.`Id`, `b`.`BaseParentId`, `b`.`Discriminator`, `b`.`Name`, `b0`.`Id`, `b0`.`Discriminator`, `b0`.`Name`, `b0`.`BaseId`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b0`.`OwnedReferenceOnBase_Id`, `b0`.`OwnedReferenceOnBase_Name`, `b2`.`DerivedInheritanceRelationshipEntityId`, `b2`.`Id`, `b2`.`Name`, `b0`.`OwnedReferenceOnDerived_Id`, `b0`.`OwnedReferenceOnDerived_Name`
FROM ((`BaseReferencesOnBase` AS `b`
LEFT JOIN `BaseEntities` AS `b0` ON `b`.`BaseParentId` = `b0`.`Id`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b1` ON `b0`.`Id` = `b1`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b2` ON `b0`.`Id` = `b2`.`DerivedInheritanceRelationshipEntityId`
WHERE `b`.`Name` <> 'Bar' OR `b`.`Name` IS NULL
ORDER BY `b`.`Id`, `b0`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b2`.`DerivedInheritanceRelationshipEntityId`
""");
        }

        public override async Task Include_reference_without_inheritance(bool async)
        {
            await base.Include_reference_without_inheritance(async);

            AssertSql(
                """
SELECT `b`.`Id`, `b`.`Discriminator`, `b`.`Name`, `b`.`BaseId`, `r`.`Id`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b0`.`Name`, `b`.`OwnedReferenceOnBase_Id`, `b`.`OwnedReferenceOnBase_Name`, `b1`.`DerivedInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b`.`OwnedReferenceOnDerived_Id`, `b`.`OwnedReferenceOnDerived_Name`, `r`.`Name`, `r`.`ParentId`
FROM ((`BaseEntities` AS `b`
LEFT JOIN `ReferencesOnBase` AS `r` ON `b`.`Id` = `r`.`ParentId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b0` ON `b`.`Id` = `b0`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b1` ON `b`.`Id` = `b1`.`DerivedInheritanceRelationshipEntityId`
ORDER BY `b`.`Id`, `r`.`Id`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b1`.`DerivedInheritanceRelationshipEntityId`
""");
        }

        public override async Task Include_reference_without_inheritance_reverse(bool async)
        {
            await base.Include_reference_without_inheritance_reverse(async);

            AssertSql(
                """
SELECT `r`.`Id`, `r`.`Name`, `r`.`ParentId`, `b`.`Id`, `b`.`Discriminator`, `b`.`Name`, `b`.`BaseId`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b0`.`Name`, `b`.`OwnedReferenceOnBase_Id`, `b`.`OwnedReferenceOnBase_Name`, `b1`.`DerivedInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b`.`OwnedReferenceOnDerived_Id`, `b`.`OwnedReferenceOnDerived_Name`
FROM ((`ReferencesOnBase` AS `r`
LEFT JOIN `BaseEntities` AS `b` ON `r`.`ParentId` = `b`.`Id`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b0` ON `b`.`Id` = `b0`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b1` ON `b`.`Id` = `b1`.`DerivedInheritanceRelationshipEntityId`
ORDER BY `r`.`Id`, `b`.`Id`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b1`.`DerivedInheritanceRelationshipEntityId`
""");
        }

        public override async Task Include_reference_without_inheritance_with_filter(bool async)
        {
            await base.Include_reference_without_inheritance_with_filter(async);

            AssertSql(
"""
SELECT `b`.`Id`, `b`.`Discriminator`, `b`.`Name`, `b`.`BaseId`, `r`.`Id`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b0`.`Name`, `b`.`OwnedReferenceOnBase_Id`, `b`.`OwnedReferenceOnBase_Name`, `b1`.`DerivedInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b`.`OwnedReferenceOnDerived_Id`, `b`.`OwnedReferenceOnDerived_Name`, `r`.`Name`, `r`.`ParentId`
FROM ((`BaseEntities` AS `b`
LEFT JOIN `ReferencesOnBase` AS `r` ON `b`.`Id` = `r`.`ParentId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b0` ON `b`.`Id` = `b0`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b1` ON `b`.`Id` = `b1`.`DerivedInheritanceRelationshipEntityId`
WHERE `b`.`Name` <> 'Bar' OR `b`.`Name` IS NULL
ORDER BY `b`.`Id`, `r`.`Id`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b1`.`DerivedInheritanceRelationshipEntityId`
""");
        }

        public override async Task Include_reference_without_inheritance_with_filter_reverse(bool async)
        {
            await base.Include_reference_without_inheritance_with_filter_reverse(async);

            AssertSql(
"""
SELECT `r`.`Id`, `r`.`Name`, `r`.`ParentId`, `b`.`Id`, `b`.`Discriminator`, `b`.`Name`, `b`.`BaseId`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b0`.`Name`, `b`.`OwnedReferenceOnBase_Id`, `b`.`OwnedReferenceOnBase_Name`, `b1`.`DerivedInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b`.`OwnedReferenceOnDerived_Id`, `b`.`OwnedReferenceOnDerived_Name`
FROM ((`ReferencesOnBase` AS `r`
LEFT JOIN `BaseEntities` AS `b` ON `r`.`ParentId` = `b`.`Id`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b0` ON `b`.`Id` = `b0`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b1` ON `b`.`Id` = `b1`.`DerivedInheritanceRelationshipEntityId`
WHERE `r`.`Name` <> 'Bar' OR `r`.`Name` IS NULL
ORDER BY `r`.`Id`, `b`.`Id`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b1`.`DerivedInheritanceRelationshipEntityId`
""");
        }
        
        public override async Task Include_collection_with_inheritance_reverse(bool async)
        {
            await base.Include_collection_with_inheritance_reverse(async);

            AssertSql(
                """
SELECT `b`.`Id`, `b`.`BaseParentId`, `b`.`Discriminator`, `b`.`Name`, `b`.`DerivedProperty`, `b0`.`Id`, `b0`.`Discriminator`, `b0`.`Name`, `b0`.`BaseId`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b0`.`OwnedReferenceOnBase_Id`, `b0`.`OwnedReferenceOnBase_Name`, `b2`.`DerivedInheritanceRelationshipEntityId`, `b2`.`Id`, `b2`.`Name`, `b0`.`OwnedReferenceOnDerived_Id`, `b0`.`OwnedReferenceOnDerived_Name`
FROM ((`BaseCollectionsOnBase` AS `b`
LEFT JOIN `BaseEntities` AS `b0` ON `b`.`BaseParentId` = `b0`.`Id`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b1` ON `b0`.`Id` = `b1`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b2` ON `b0`.`Id` = `b2`.`DerivedInheritanceRelationshipEntityId`
ORDER BY `b`.`Id`, `b0`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b2`.`DerivedInheritanceRelationshipEntityId`
""");
        }
        
        public override async Task Include_collection_with_inheritance_with_filter_reverse(bool async)
        {
            await base.Include_collection_with_inheritance_with_filter_reverse(async);

            AssertSql(
"""
SELECT `b`.`Id`, `b`.`BaseParentId`, `b`.`Discriminator`, `b`.`Name`, `b`.`DerivedProperty`, `b0`.`Id`, `b0`.`Discriminator`, `b0`.`Name`, `b0`.`BaseId`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b0`.`OwnedReferenceOnBase_Id`, `b0`.`OwnedReferenceOnBase_Name`, `b2`.`DerivedInheritanceRelationshipEntityId`, `b2`.`Id`, `b2`.`Name`, `b0`.`OwnedReferenceOnDerived_Id`, `b0`.`OwnedReferenceOnDerived_Name`
FROM ((`BaseCollectionsOnBase` AS `b`
LEFT JOIN `BaseEntities` AS `b0` ON `b`.`BaseParentId` = `b0`.`Id`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b1` ON `b0`.`Id` = `b1`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b2` ON `b0`.`Id` = `b2`.`DerivedInheritanceRelationshipEntityId`
WHERE `b`.`Name` <> 'Bar' OR `b`.`Name` IS NULL
ORDER BY `b`.`Id`, `b0`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b2`.`DerivedInheritanceRelationshipEntityId`
""");
        }

        public override async Task Include_collection_without_inheritance(bool async)
        {
            await base.Include_collection_without_inheritance(async);

            AssertSql(
                """
SELECT `b`.`Id`, `b`.`Discriminator`, `b`.`Name`, `b`.`BaseId`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b0`.`Name`, `b`.`OwnedReferenceOnBase_Id`, `b`.`OwnedReferenceOnBase_Name`, `b1`.`DerivedInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b`.`OwnedReferenceOnDerived_Id`, `b`.`OwnedReferenceOnDerived_Name`, `c`.`Id`, `c`.`Name`, `c`.`ParentId`
FROM ((`BaseEntities` AS `b`
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b0` ON `b`.`Id` = `b0`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b1` ON `b`.`Id` = `b1`.`DerivedInheritanceRelationshipEntityId`)
LEFT JOIN `CollectionsOnBase` AS `c` ON `b`.`Id` = `c`.`ParentId`
ORDER BY `b`.`Id`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b1`.`DerivedInheritanceRelationshipEntityId`, `b1`.`Id`
""");
        }

        public override async Task Include_collection_without_inheritance_reverse(bool async)
        {
            await base.Include_collection_without_inheritance_reverse(async);

            AssertSql(
                """
SELECT `c`.`Id`, `c`.`Name`, `c`.`ParentId`, `b`.`Id`, `b`.`Discriminator`, `b`.`Name`, `b`.`BaseId`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b0`.`Name`, `b`.`OwnedReferenceOnBase_Id`, `b`.`OwnedReferenceOnBase_Name`, `b1`.`DerivedInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b`.`OwnedReferenceOnDerived_Id`, `b`.`OwnedReferenceOnDerived_Name`
FROM ((`CollectionsOnBase` AS `c`
LEFT JOIN `BaseEntities` AS `b` ON `c`.`ParentId` = `b`.`Id`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b0` ON `b`.`Id` = `b0`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b1` ON `b`.`Id` = `b1`.`DerivedInheritanceRelationshipEntityId`
ORDER BY `c`.`Id`, `b`.`Id`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b1`.`DerivedInheritanceRelationshipEntityId`
""");
        }

        public override async Task Include_collection_without_inheritance_with_filter(bool async)
        {
            await base.Include_collection_without_inheritance_with_filter(async);

            AssertSql(
"""
SELECT `b`.`Id`, `b`.`Discriminator`, `b`.`Name`, `b`.`BaseId`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b0`.`Name`, `b`.`OwnedReferenceOnBase_Id`, `b`.`OwnedReferenceOnBase_Name`, `b1`.`DerivedInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b`.`OwnedReferenceOnDerived_Id`, `b`.`OwnedReferenceOnDerived_Name`, `c`.`Id`, `c`.`Name`, `c`.`ParentId`
FROM ((`BaseEntities` AS `b`
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b0` ON `b`.`Id` = `b0`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b1` ON `b`.`Id` = `b1`.`DerivedInheritanceRelationshipEntityId`)
LEFT JOIN `CollectionsOnBase` AS `c` ON `b`.`Id` = `c`.`ParentId`
WHERE `b`.`Name` <> 'Bar' OR `b`.`Name` IS NULL
ORDER BY `b`.`Id`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b1`.`DerivedInheritanceRelationshipEntityId`, `b1`.`Id`
""");
        }

        public override async Task Include_collection_without_inheritance_with_filter_reverse(bool async)
        {
            await base.Include_collection_without_inheritance_with_filter_reverse(async);

            AssertSql(
"""
SELECT `c`.`Id`, `c`.`Name`, `c`.`ParentId`, `b`.`Id`, `b`.`Discriminator`, `b`.`Name`, `b`.`BaseId`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b0`.`Name`, `b`.`OwnedReferenceOnBase_Id`, `b`.`OwnedReferenceOnBase_Name`, `b1`.`DerivedInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b`.`OwnedReferenceOnDerived_Id`, `b`.`OwnedReferenceOnDerived_Name`
FROM ((`CollectionsOnBase` AS `c`
LEFT JOIN `BaseEntities` AS `b` ON `c`.`ParentId` = `b`.`Id`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b0` ON `b`.`Id` = `b0`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b1` ON `b`.`Id` = `b1`.`DerivedInheritanceRelationshipEntityId`
WHERE `c`.`Name` <> 'Bar' OR `c`.`Name` IS NULL
ORDER BY `c`.`Id`, `b`.`Id`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b1`.`DerivedInheritanceRelationshipEntityId`
""");
        }

        public override async Task Include_reference_with_inheritance_on_derived1(bool async)
        {
            await base.Include_reference_with_inheritance_on_derived1(async);

            AssertSql(
                """
SELECT `b`.`Id`, `b`.`Discriminator`, `b`.`Name`, `b`.`BaseId`, `b0`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b`.`OwnedReferenceOnBase_Id`, `b`.`OwnedReferenceOnBase_Name`, `b2`.`DerivedInheritanceRelationshipEntityId`, `b2`.`Id`, `b2`.`Name`, `b`.`OwnedReferenceOnDerived_Id`, `b`.`OwnedReferenceOnDerived_Name`, `b0`.`BaseParentId`, `b0`.`Discriminator`, `b0`.`Name`
FROM ((`BaseEntities` AS `b`
LEFT JOIN `BaseReferencesOnBase` AS `b0` ON `b`.`Id` = `b0`.`BaseParentId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b1` ON `b`.`Id` = `b1`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b2` ON `b`.`Id` = `b2`.`DerivedInheritanceRelationshipEntityId`
WHERE `b`.`Discriminator` = 'DerivedInheritanceRelationshipEntity'
ORDER BY `b`.`Id`, `b0`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b2`.`DerivedInheritanceRelationshipEntityId`
""");
        }

        public override async Task Include_reference_with_inheritance_on_derived2(bool async)
        {
            await base.Include_reference_with_inheritance_on_derived2(async);

            AssertSql(
                """
SELECT `b`.`Id`, `b`.`Discriminator`, `b`.`Name`, `b`.`BaseId`, `b0`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b`.`OwnedReferenceOnBase_Id`, `b`.`OwnedReferenceOnBase_Name`, `b2`.`DerivedInheritanceRelationshipEntityId`, `b2`.`Id`, `b2`.`Name`, `b`.`OwnedReferenceOnDerived_Id`, `b`.`OwnedReferenceOnDerived_Name`, `b0`.`BaseParentId`, `b0`.`Discriminator`, `b0`.`Name`, `b0`.`DerivedInheritanceRelationshipEntityId`
FROM ((`BaseEntities` AS `b`
LEFT JOIN `BaseReferencesOnDerived` AS `b0` ON `b`.`Id` = `b0`.`BaseParentId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b1` ON `b`.`Id` = `b1`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b2` ON `b`.`Id` = `b2`.`DerivedInheritanceRelationshipEntityId`
WHERE `b`.`Discriminator` = 'DerivedInheritanceRelationshipEntity'
ORDER BY `b`.`Id`, `b0`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b2`.`DerivedInheritanceRelationshipEntityId`
""");
        }

        public override async Task Include_reference_with_inheritance_on_derived4(bool async)
        {
            await base.Include_reference_with_inheritance_on_derived4(async);

            AssertSql(
                """
SELECT `b`.`Id`, `b`.`Discriminator`, `b`.`Name`, `b`.`BaseId`, `t`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b`.`OwnedReferenceOnBase_Id`, `b`.`OwnedReferenceOnBase_Name`, `b2`.`DerivedInheritanceRelationshipEntityId`, `b2`.`Id`, `b2`.`Name`, `b`.`OwnedReferenceOnDerived_Id`, `b`.`OwnedReferenceOnDerived_Name`, `t`.`BaseParentId`, `t`.`Discriminator`, `t`.`Name`, `t`.`DerivedInheritanceRelationshipEntityId`
FROM ((`BaseEntities` AS `b`
LEFT JOIN (
    SELECT `b0`.`Id`, `b0`.`BaseParentId`, `b0`.`Discriminator`, `b0`.`Name`, `b0`.`DerivedInheritanceRelationshipEntityId`
    FROM `BaseReferencesOnDerived` AS `b0`
    WHERE `b0`.`Discriminator` = 'DerivedReferenceOnDerived'
) AS `t` ON `b`.`Id` = `t`.`DerivedInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b1` ON `b`.`Id` = `b1`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b2` ON `b`.`Id` = `b2`.`DerivedInheritanceRelationshipEntityId`
WHERE `b`.`Discriminator` = 'DerivedInheritanceRelationshipEntity'
ORDER BY `b`.`Id`, `t`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b2`.`DerivedInheritanceRelationshipEntityId`
""");
        }

        public override async Task Include_reference_with_inheritance_on_derived_reverse(bool async)
        {
            await base.Include_reference_with_inheritance_on_derived_reverse(async);

            AssertSql(
                """
SELECT `b`.`Id`, `b`.`BaseParentId`, `b`.`Discriminator`, `b`.`Name`, `b`.`DerivedInheritanceRelationshipEntityId`, `t`.`Id`, `t`.`Discriminator`, `t`.`Name`, `t`.`BaseId`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `t`.`OwnedReferenceOnBase_Id`, `t`.`OwnedReferenceOnBase_Name`, `b2`.`DerivedInheritanceRelationshipEntityId`, `b2`.`Id`, `b2`.`Name`, `t`.`OwnedReferenceOnDerived_Id`, `t`.`OwnedReferenceOnDerived_Name`
FROM ((`BaseReferencesOnDerived` AS `b`
LEFT JOIN (
    SELECT `b0`.`Id`, `b0`.`Discriminator`, `b0`.`Name`, `b0`.`BaseId`, `b0`.`OwnedReferenceOnBase_Id`, `b0`.`OwnedReferenceOnBase_Name`, `b0`.`OwnedReferenceOnDerived_Id`, `b0`.`OwnedReferenceOnDerived_Name`
    FROM `BaseEntities` AS `b0`
    WHERE `b0`.`Discriminator` = 'DerivedInheritanceRelationshipEntity'
) AS `t` ON `b`.`BaseParentId` = `t`.`Id`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b1` ON `t`.`Id` = `b1`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b2` ON `t`.`Id` = `b2`.`DerivedInheritanceRelationshipEntityId`
ORDER BY `b`.`Id`, `t`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b2`.`DerivedInheritanceRelationshipEntityId`
""");
        }

        public override async Task Include_reference_with_inheritance_on_derived_with_filter1(bool async)
        {
            await base.Include_reference_with_inheritance_on_derived_with_filter1(async);

            AssertSql(
"""
SELECT `b`.`Id`, `b`.`Discriminator`, `b`.`Name`, `b`.`BaseId`, `b0`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b`.`OwnedReferenceOnBase_Id`, `b`.`OwnedReferenceOnBase_Name`, `b2`.`DerivedInheritanceRelationshipEntityId`, `b2`.`Id`, `b2`.`Name`, `b`.`OwnedReferenceOnDerived_Id`, `b`.`OwnedReferenceOnDerived_Name`, `b0`.`BaseParentId`, `b0`.`Discriminator`, `b0`.`Name`
FROM ((`BaseEntities` AS `b`
LEFT JOIN `BaseReferencesOnBase` AS `b0` ON `b`.`Id` = `b0`.`BaseParentId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b1` ON `b`.`Id` = `b1`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b2` ON `b`.`Id` = `b2`.`DerivedInheritanceRelationshipEntityId`
WHERE `b`.`Discriminator` = 'DerivedInheritanceRelationshipEntity' AND (`b`.`Name` <> 'Bar' OR `b`.`Name` IS NULL)
ORDER BY `b`.`Id`, `b0`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b2`.`DerivedInheritanceRelationshipEntityId`
""");
        }

        public override async Task Include_reference_with_inheritance_on_derived_with_filter2(bool async)
        {
            await base.Include_reference_with_inheritance_on_derived_with_filter2(async);

            AssertSql(
"""
SELECT `b`.`Id`, `b`.`Discriminator`, `b`.`Name`, `b`.`BaseId`, `b0`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b`.`OwnedReferenceOnBase_Id`, `b`.`OwnedReferenceOnBase_Name`, `b2`.`DerivedInheritanceRelationshipEntityId`, `b2`.`Id`, `b2`.`Name`, `b`.`OwnedReferenceOnDerived_Id`, `b`.`OwnedReferenceOnDerived_Name`, `b0`.`BaseParentId`, `b0`.`Discriminator`, `b0`.`Name`, `b0`.`DerivedInheritanceRelationshipEntityId`
FROM ((`BaseEntities` AS `b`
LEFT JOIN `BaseReferencesOnDerived` AS `b0` ON `b`.`Id` = `b0`.`BaseParentId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b1` ON `b`.`Id` = `b1`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b2` ON `b`.`Id` = `b2`.`DerivedInheritanceRelationshipEntityId`
WHERE `b`.`Discriminator` = 'DerivedInheritanceRelationshipEntity' AND (`b`.`Name` <> 'Bar' OR `b`.`Name` IS NULL)
ORDER BY `b`.`Id`, `b0`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b2`.`DerivedInheritanceRelationshipEntityId`
""");
        }

        public override async Task Include_reference_with_inheritance_on_derived_with_filter4(bool async)
        {
            await base.Include_reference_with_inheritance_on_derived_with_filter4(async);

            AssertSql(
"""
SELECT `b`.`Id`, `b`.`Discriminator`, `b`.`Name`, `b`.`BaseId`, `t`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b`.`OwnedReferenceOnBase_Id`, `b`.`OwnedReferenceOnBase_Name`, `b2`.`DerivedInheritanceRelationshipEntityId`, `b2`.`Id`, `b2`.`Name`, `b`.`OwnedReferenceOnDerived_Id`, `b`.`OwnedReferenceOnDerived_Name`, `t`.`BaseParentId`, `t`.`Discriminator`, `t`.`Name`, `t`.`DerivedInheritanceRelationshipEntityId`
FROM ((`BaseEntities` AS `b`
LEFT JOIN (
    SELECT `b0`.`Id`, `b0`.`BaseParentId`, `b0`.`Discriminator`, `b0`.`Name`, `b0`.`DerivedInheritanceRelationshipEntityId`
    FROM `BaseReferencesOnDerived` AS `b0`
    WHERE `b0`.`Discriminator` = 'DerivedReferenceOnDerived'
) AS `t` ON `b`.`Id` = `t`.`DerivedInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b1` ON `b`.`Id` = `b1`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b2` ON `b`.`Id` = `b2`.`DerivedInheritanceRelationshipEntityId`
WHERE `b`.`Discriminator` = 'DerivedInheritanceRelationshipEntity' AND (`b`.`Name` <> 'Bar' OR `b`.`Name` IS NULL)
ORDER BY `b`.`Id`, `t`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b2`.`DerivedInheritanceRelationshipEntityId`
""");
        }

        public override async Task Include_reference_with_inheritance_on_derived_with_filter_reverse(bool async)
        {
            await base.Include_reference_with_inheritance_on_derived_with_filter_reverse(async);

            AssertSql(
"""
SELECT `b`.`Id`, `b`.`BaseParentId`, `b`.`Discriminator`, `b`.`Name`, `b`.`DerivedInheritanceRelationshipEntityId`, `t`.`Id`, `t`.`Discriminator`, `t`.`Name`, `t`.`BaseId`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `t`.`OwnedReferenceOnBase_Id`, `t`.`OwnedReferenceOnBase_Name`, `b2`.`DerivedInheritanceRelationshipEntityId`, `b2`.`Id`, `b2`.`Name`, `t`.`OwnedReferenceOnDerived_Id`, `t`.`OwnedReferenceOnDerived_Name`
FROM ((`BaseReferencesOnDerived` AS `b`
LEFT JOIN (
    SELECT `b0`.`Id`, `b0`.`Discriminator`, `b0`.`Name`, `b0`.`BaseId`, `b0`.`OwnedReferenceOnBase_Id`, `b0`.`OwnedReferenceOnBase_Name`, `b0`.`OwnedReferenceOnDerived_Id`, `b0`.`OwnedReferenceOnDerived_Name`
    FROM `BaseEntities` AS `b0`
    WHERE `b0`.`Discriminator` = 'DerivedInheritanceRelationshipEntity'
) AS `t` ON `b`.`BaseParentId` = `t`.`Id`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b1` ON `t`.`Id` = `b1`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b2` ON `t`.`Id` = `b2`.`DerivedInheritanceRelationshipEntityId`
WHERE `b`.`Name` <> 'Bar' OR `b`.`Name` IS NULL
ORDER BY `b`.`Id`, `t`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b2`.`DerivedInheritanceRelationshipEntityId`
""");
        }

        public override async Task Include_reference_without_inheritance_on_derived1(bool async)
        {
            await base.Include_reference_without_inheritance_on_derived1(async);

            AssertSql(
                """
SELECT `b`.`Id`, `b`.`Discriminator`, `b`.`Name`, `b`.`BaseId`, `r`.`Id`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b0`.`Name`, `b`.`OwnedReferenceOnBase_Id`, `b`.`OwnedReferenceOnBase_Name`, `b1`.`DerivedInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b`.`OwnedReferenceOnDerived_Id`, `b`.`OwnedReferenceOnDerived_Name`, `r`.`Name`, `r`.`ParentId`
FROM ((`BaseEntities` AS `b`
LEFT JOIN `ReferencesOnBase` AS `r` ON `b`.`Id` = `r`.`ParentId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b0` ON `b`.`Id` = `b0`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b1` ON `b`.`Id` = `b1`.`DerivedInheritanceRelationshipEntityId`
WHERE `b`.`Discriminator` = 'DerivedInheritanceRelationshipEntity'
ORDER BY `b`.`Id`, `r`.`Id`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b1`.`DerivedInheritanceRelationshipEntityId`
""");
        }

        public override async Task Include_reference_without_inheritance_on_derived2(bool async)
        {
            await base.Include_reference_without_inheritance_on_derived2(async);

            AssertSql(
                """
SELECT `b`.`Id`, `b`.`Discriminator`, `b`.`Name`, `b`.`BaseId`, `r`.`Id`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b0`.`Name`, `b`.`OwnedReferenceOnBase_Id`, `b`.`OwnedReferenceOnBase_Name`, `b1`.`DerivedInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b`.`OwnedReferenceOnDerived_Id`, `b`.`OwnedReferenceOnDerived_Name`, `r`.`Name`, `r`.`ParentId`
FROM ((`BaseEntities` AS `b`
LEFT JOIN `ReferencesOnDerived` AS `r` ON `b`.`Id` = `r`.`ParentId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b0` ON `b`.`Id` = `b0`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b1` ON `b`.`Id` = `b1`.`DerivedInheritanceRelationshipEntityId`
WHERE `b`.`Discriminator` = 'DerivedInheritanceRelationshipEntity'
ORDER BY `b`.`Id`, `r`.`Id`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b1`.`DerivedInheritanceRelationshipEntityId`
""");
        }

        public override async Task Include_reference_without_inheritance_on_derived_reverse(bool async)
        {
            await base.Include_reference_without_inheritance_on_derived_reverse(async);

            AssertSql(
                """
SELECT `r`.`Id`, `r`.`Name`, `r`.`ParentId`, `t`.`Id`, `t`.`Discriminator`, `t`.`Name`, `t`.`BaseId`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b0`.`Name`, `t`.`OwnedReferenceOnBase_Id`, `t`.`OwnedReferenceOnBase_Name`, `b1`.`DerivedInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `t`.`OwnedReferenceOnDerived_Id`, `t`.`OwnedReferenceOnDerived_Name`
FROM ((`ReferencesOnDerived` AS `r`
LEFT JOIN (
    SELECT `b`.`Id`, `b`.`Discriminator`, `b`.`Name`, `b`.`BaseId`, `b`.`OwnedReferenceOnBase_Id`, `b`.`OwnedReferenceOnBase_Name`, `b`.`OwnedReferenceOnDerived_Id`, `b`.`OwnedReferenceOnDerived_Name`
    FROM `BaseEntities` AS `b`
    WHERE `b`.`Discriminator` = 'DerivedInheritanceRelationshipEntity'
) AS `t` ON `r`.`ParentId` = `t`.`Id`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b0` ON `t`.`Id` = `b0`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b1` ON `t`.`Id` = `b1`.`DerivedInheritanceRelationshipEntityId`
ORDER BY `r`.`Id`, `t`.`Id`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b1`.`DerivedInheritanceRelationshipEntityId`
""");
        }

        public override async Task Include_collection_with_inheritance_on_derived1(bool async)
        {
            await base.Include_collection_with_inheritance_on_derived1(async);

            AssertSql(
                """
SELECT `b`.`Id`, `b`.`Discriminator`, `b`.`Name`, `b`.`BaseId`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b0`.`Name`, `b`.`OwnedReferenceOnBase_Id`, `b`.`OwnedReferenceOnBase_Name`, `b1`.`DerivedInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b`.`OwnedReferenceOnDerived_Id`, `b`.`OwnedReferenceOnDerived_Name`, `b2`.`Id`, `b2`.`BaseParentId`, `b2`.`Discriminator`, `b2`.`Name`, `b2`.`DerivedProperty`
FROM ((`BaseEntities` AS `b`
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b0` ON `b`.`Id` = `b0`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b1` ON `b`.`Id` = `b1`.`DerivedInheritanceRelationshipEntityId`)
LEFT JOIN `BaseCollectionsOnBase` AS `b2` ON `b`.`Id` = `b2`.`BaseParentId`
WHERE `b`.`Discriminator` = 'DerivedInheritanceRelationshipEntity'
ORDER BY `b`.`Id`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b1`.`DerivedInheritanceRelationshipEntityId`, `b1`.`Id`
""");
        }

        public override async Task Include_collection_with_inheritance_on_derived2(bool async)
        {
            await base.Include_collection_with_inheritance_on_derived2(async);

            AssertSql(
                """
SELECT `b`.`Id`, `b`.`Discriminator`, `b`.`Name`, `b`.`BaseId`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b0`.`Name`, `b`.`OwnedReferenceOnBase_Id`, `b`.`OwnedReferenceOnBase_Name`, `b1`.`DerivedInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b`.`OwnedReferenceOnDerived_Id`, `b`.`OwnedReferenceOnDerived_Name`, `b2`.`Id`, `b2`.`Discriminator`, `b2`.`Name`, `b2`.`ParentId`, `b2`.`DerivedInheritanceRelationshipEntityId`
FROM ((`BaseEntities` AS `b`
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b0` ON `b`.`Id` = `b0`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b1` ON `b`.`Id` = `b1`.`DerivedInheritanceRelationshipEntityId`)
LEFT JOIN `BaseCollectionsOnDerived` AS `b2` ON `b`.`Id` = `b2`.`ParentId`
WHERE `b`.`Discriminator` = 'DerivedInheritanceRelationshipEntity'
ORDER BY `b`.`Id`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b1`.`DerivedInheritanceRelationshipEntityId`, `b1`.`Id`
""");
        }

        public override async Task Include_collection_with_inheritance_on_derived3(bool async)
        {
            await base.Include_collection_with_inheritance_on_derived3(async);

            AssertSql(
                """
SELECT `b`.`Id`, `b`.`Discriminator`, `b`.`Name`, `b`.`BaseId`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b0`.`Name`, `b`.`OwnedReferenceOnBase_Id`, `b`.`OwnedReferenceOnBase_Name`, `b1`.`DerivedInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b`.`OwnedReferenceOnDerived_Id`, `b`.`OwnedReferenceOnDerived_Name`, `t`.`Id`, `t`.`Discriminator`, `t`.`Name`, `t`.`ParentId`, `t`.`DerivedInheritanceRelationshipEntityId`
FROM ((`BaseEntities` AS `b`
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b0` ON `b`.`Id` = `b0`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b1` ON `b`.`Id` = `b1`.`DerivedInheritanceRelationshipEntityId`)
LEFT JOIN (
    SELECT `b2`.`Id`, `b2`.`Discriminator`, `b2`.`Name`, `b2`.`ParentId`, `b2`.`DerivedInheritanceRelationshipEntityId`
    FROM `BaseCollectionsOnDerived` AS `b2`
    WHERE `b2`.`Discriminator` = 'DerivedCollectionOnDerived'
) AS `t` ON `b`.`Id` = `t`.`DerivedInheritanceRelationshipEntityId`
WHERE `b`.`Discriminator` = 'DerivedInheritanceRelationshipEntity'
ORDER BY `b`.`Id`, `b0`.`BaseInheritanceRelationshipEntityId`, `b0`.`Id`, `b1`.`DerivedInheritanceRelationshipEntityId`, `b1`.`Id`
""");
        }

        public override async Task Include_collection_with_inheritance_on_derived_reverse(bool async)
        {
            await base.Include_collection_with_inheritance_on_derived_reverse(async);

            AssertSql(
                """
SELECT `b`.`Id`, `b`.`Discriminator`, `b`.`Name`, `b`.`ParentId`, `b`.`DerivedInheritanceRelationshipEntityId`, `t`.`Id`, `t`.`Discriminator`, `t`.`Name`, `t`.`BaseId`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `t`.`OwnedReferenceOnBase_Id`, `t`.`OwnedReferenceOnBase_Name`, `b2`.`DerivedInheritanceRelationshipEntityId`, `b2`.`Id`, `b2`.`Name`, `t`.`OwnedReferenceOnDerived_Id`, `t`.`OwnedReferenceOnDerived_Name`
FROM ((`BaseCollectionsOnDerived` AS `b`
LEFT JOIN (
    SELECT `b0`.`Id`, `b0`.`Discriminator`, `b0`.`Name`, `b0`.`BaseId`, `b0`.`OwnedReferenceOnBase_Id`, `b0`.`OwnedReferenceOnBase_Name`, `b0`.`OwnedReferenceOnDerived_Id`, `b0`.`OwnedReferenceOnDerived_Name`
    FROM `BaseEntities` AS `b0`
    WHERE `b0`.`Discriminator` = 'DerivedInheritanceRelationshipEntity'
) AS `t` ON `b`.`ParentId` = `t`.`Id`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b1` ON `t`.`Id` = `b1`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b2` ON `t`.`Id` = `b2`.`DerivedInheritanceRelationshipEntityId`
ORDER BY `b`.`Id`, `t`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b2`.`DerivedInheritanceRelationshipEntityId`
""");
        }
        
        public override async Task Nested_include_with_inheritance_reference_reference_reverse(bool async)
        {
            await base.Nested_include_with_inheritance_reference_reference_reverse(async);

            AssertSql(
                """
SELECT `n`.`Id`, `n`.`Discriminator`, `n`.`Name`, `n`.`ParentCollectionId`, `n`.`ParentReferenceId`, `b`.`Id`, `b`.`BaseParentId`, `b`.`Discriminator`, `b`.`Name`, `b0`.`Id`, `b0`.`Discriminator`, `b0`.`Name`, `b0`.`BaseId`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b0`.`OwnedReferenceOnBase_Id`, `b0`.`OwnedReferenceOnBase_Name`, `b2`.`DerivedInheritanceRelationshipEntityId`, `b2`.`Id`, `b2`.`Name`, `b0`.`OwnedReferenceOnDerived_Id`, `b0`.`OwnedReferenceOnDerived_Name`
FROM (((`NestedReferences` AS `n`
LEFT JOIN `BaseReferencesOnBase` AS `b` ON `n`.`ParentReferenceId` = `b`.`Id`)
LEFT JOIN `BaseEntities` AS `b0` ON `b`.`BaseParentId` = `b0`.`Id`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b1` ON `b0`.`Id` = `b1`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b2` ON `b0`.`Id` = `b2`.`DerivedInheritanceRelationshipEntityId`
ORDER BY `n`.`Id`, `b`.`Id`, `b0`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b2`.`DerivedInheritanceRelationshipEntityId`
""");
        }
        
        public override async Task Nested_include_with_inheritance_reference_collection_reverse(bool async)
        {
            await base.Nested_include_with_inheritance_reference_collection_reverse(async);

            AssertSql(
                """
SELECT `n`.`Id`, `n`.`Discriminator`, `n`.`Name`, `n`.`ParentCollectionId`, `n`.`ParentReferenceId`, `b`.`Id`, `b`.`BaseParentId`, `b`.`Discriminator`, `b`.`Name`, `b0`.`Id`, `b0`.`Discriminator`, `b0`.`Name`, `b0`.`BaseId`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b0`.`OwnedReferenceOnBase_Id`, `b0`.`OwnedReferenceOnBase_Name`, `b2`.`DerivedInheritanceRelationshipEntityId`, `b2`.`Id`, `b2`.`Name`, `b0`.`OwnedReferenceOnDerived_Id`, `b0`.`OwnedReferenceOnDerived_Name`
FROM (((`NestedCollections` AS `n`
LEFT JOIN `BaseReferencesOnBase` AS `b` ON `n`.`ParentReferenceId` = `b`.`Id`)
LEFT JOIN `BaseEntities` AS `b0` ON `b`.`BaseParentId` = `b0`.`Id`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b1` ON `b0`.`Id` = `b1`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b2` ON `b0`.`Id` = `b2`.`DerivedInheritanceRelationshipEntityId`
ORDER BY `n`.`Id`, `b`.`Id`, `b0`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b2`.`DerivedInheritanceRelationshipEntityId`
""");
        }
        
        public override async Task Nested_include_with_inheritance_collection_reference_reverse(bool async)
        {
            await base.Nested_include_with_inheritance_collection_reference_reverse(async);

            AssertSql(
                """
SELECT `n`.`Id`, `n`.`Discriminator`, `n`.`Name`, `n`.`ParentCollectionId`, `n`.`ParentReferenceId`, `b`.`Id`, `b`.`BaseParentId`, `b`.`Discriminator`, `b`.`Name`, `b`.`DerivedProperty`, `b0`.`Id`, `b0`.`Discriminator`, `b0`.`Name`, `b0`.`BaseId`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b0`.`OwnedReferenceOnBase_Id`, `b0`.`OwnedReferenceOnBase_Name`, `b2`.`DerivedInheritanceRelationshipEntityId`, `b2`.`Id`, `b2`.`Name`, `b0`.`OwnedReferenceOnDerived_Id`, `b0`.`OwnedReferenceOnDerived_Name`
FROM (((`NestedReferences` AS `n`
LEFT JOIN `BaseCollectionsOnBase` AS `b` ON `n`.`ParentCollectionId` = `b`.`Id`)
LEFT JOIN `BaseEntities` AS `b0` ON `b`.`BaseParentId` = `b0`.`Id`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b1` ON `b0`.`Id` = `b1`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b2` ON `b0`.`Id` = `b2`.`DerivedInheritanceRelationshipEntityId`
ORDER BY `n`.`Id`, `b`.`Id`, `b0`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b2`.`DerivedInheritanceRelationshipEntityId`
""");
        }
        
        public override async Task Nested_include_with_inheritance_collection_collection_reverse(bool async)
        {
            await base.Nested_include_with_inheritance_collection_collection_reverse(async);

            AssertSql(
                """
SELECT `n`.`Id`, `n`.`Discriminator`, `n`.`Name`, `n`.`ParentCollectionId`, `n`.`ParentReferenceId`, `b`.`Id`, `b`.`BaseParentId`, `b`.`Discriminator`, `b`.`Name`, `b`.`DerivedProperty`, `b0`.`Id`, `b0`.`Discriminator`, `b0`.`Name`, `b0`.`BaseId`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b1`.`Name`, `b0`.`OwnedReferenceOnBase_Id`, `b0`.`OwnedReferenceOnBase_Name`, `b2`.`DerivedInheritanceRelationshipEntityId`, `b2`.`Id`, `b2`.`Name`, `b0`.`OwnedReferenceOnDerived_Id`, `b0`.`OwnedReferenceOnDerived_Name`
FROM (((`NestedCollections` AS `n`
LEFT JOIN `BaseCollectionsOnBase` AS `b` ON `n`.`ParentCollectionId` = `b`.`Id`)
LEFT JOIN `BaseEntities` AS `b0` ON `b`.`BaseParentId` = `b0`.`Id`)
LEFT JOIN `BaseEntities_OwnedCollectionOnBase` AS `b1` ON `b0`.`Id` = `b1`.`BaseInheritanceRelationshipEntityId`)
LEFT JOIN `BaseEntities_OwnedCollectionOnDerived` AS `b2` ON `b0`.`Id` = `b2`.`DerivedInheritanceRelationshipEntityId`
ORDER BY `n`.`Id`, `b`.`Id`, `b0`.`Id`, `b1`.`BaseInheritanceRelationshipEntityId`, `b1`.`Id`, `b2`.`DerivedInheritanceRelationshipEntityId`
""");
        }

        public override async Task Nested_include_collection_reference_on_non_entity_base(bool async)
        {
            await base.Nested_include_collection_reference_on_non_entity_base(async);

            AssertSql(
                $@"SELECT `r`.`Id`, `r`.`Name`, `t`.`Id`, `t`.`Name`, `t`.`ReferenceId`, `t`.`ReferencedEntityId`, `t`.`Id0`, `t`.`Name0`
FROM `ReferencedEntities` AS `r`
LEFT JOIN (
    SELECT `p`.`Id`, `p`.`Name`, `p`.`ReferenceId`, `p`.`ReferencedEntityId`, `r0`.`Id` AS `Id0`, `r0`.`Name` AS `Name0`
    FROM `PrincipalEntities` AS `p`
    LEFT JOIN `ReferencedEntities` AS `r0` ON `p`.`ReferenceId` = `r0`.`Id`
) AS `t` ON `r`.`Id` = `t`.`ReferencedEntityId`
ORDER BY `r`.`Id`, `t`.`Id`");
        }

        private void AssertSql(params string[] expected)
            => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);
    }
}
