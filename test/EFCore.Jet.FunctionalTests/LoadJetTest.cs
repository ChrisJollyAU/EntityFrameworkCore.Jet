﻿// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using EntityFrameworkCore.Jet.FunctionalTests.TestUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;

namespace EntityFrameworkCore.Jet.FunctionalTests
{
    public class LoadJetTest : LoadTestBase<LoadJetTest.LoadJetFixture>
    {
        public LoadJetTest(LoadJetFixture fixture)
            : base(fixture)
        {
            fixture.TestSqlLoggerFactory.Clear();
        }

        public override async Task Lazy_load_collection(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        {
            await base.Lazy_load_collection(state, queryTrackingBehavior, async);

            AssertSql(
                state == EntityState.Detached && queryTrackingBehavior == QueryTrackingBehavior.TrackAll
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='707' (Nullable = true)")}

SELECT `c`.`Id`, `c`.`ParentId`
FROM `Child` AS `c`
WHERE `c`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override async Task Lazy_load_many_to_one_reference_to_principal(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        {
            await base.Lazy_load_many_to_one_reference_to_principal(state, queryTrackingBehavior, async);

            AssertSql(
                state == EntityState.Detached && queryTrackingBehavior == QueryTrackingBehavior.TrackAll
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='707'")}

SELECT TOP 1 `p`.`Id`, `p`.`AlternateId`
FROM `Parent` AS `p`
WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override async Task Lazy_load_one_to_one_reference_to_principal(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        {
            await base.Lazy_load_one_to_one_reference_to_principal(state, queryTrackingBehavior, async);

            AssertSql(
                state == EntityState.Detached && queryTrackingBehavior == QueryTrackingBehavior.TrackAll
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='707'")}

SELECT TOP 1 `p`.`Id`, `p`.`AlternateId`
FROM `Parent` AS `p`
WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override async Task Lazy_load_one_to_one_reference_to_dependent(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        {
            await base.Lazy_load_one_to_one_reference_to_dependent(state, queryTrackingBehavior, async);

            AssertSql(
                state == EntityState.Detached && queryTrackingBehavior == QueryTrackingBehavior.TrackAll
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='707' (Nullable = true)")}

SELECT TOP 1 `s`.`Id`, `s`.`ParentId`
FROM `Single` AS `s`
WHERE `s`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override void Lazy_load_one_to_one_PK_to_PK_reference_to_principal(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        {
            base.Lazy_load_one_to_one_PK_to_PK_reference_to_principal(state, queryTrackingBehavior);

            AssertSql(
                state == EntityState.Detached && queryTrackingBehavior == QueryTrackingBehavior.TrackAll
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='707'")}

SELECT TOP 1 `p`.`Id`, `p`.`AlternateId`
FROM `Parent` AS `p`
WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override void Lazy_load_one_to_one_PK_to_PK_reference_to_dependent(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        {
            base.Lazy_load_one_to_one_PK_to_PK_reference_to_dependent(state, queryTrackingBehavior);

            AssertSql(
                state == EntityState.Detached && queryTrackingBehavior == QueryTrackingBehavior.TrackAll
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='707'")}

SELECT TOP 1 `s`.`Id`
FROM `SinglePkToPk` AS `s`
WHERE `s`.`Id` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override async Task Lazy_load_many_to_one_reference_to_principal_null_FK(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        {
            await base.Lazy_load_many_to_one_reference_to_principal_null_FK(state, queryTrackingBehavior, async);

            AssertSql();
        }

        public override async Task Lazy_load_one_to_one_reference_to_principal_null_FK(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        {
            await base.Lazy_load_one_to_one_reference_to_principal_null_FK(state, queryTrackingBehavior, async);

            AssertSql();
        }

        public override async Task Lazy_load_collection_not_found(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        {
            await base.Lazy_load_collection_not_found(state, queryTrackingBehavior, async);

            AssertSql(
                state == EntityState.Detached
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='767' (Nullable = true)")}

SELECT `c`.`Id`, `c`.`ParentId`
FROM `Child` AS `c`
WHERE `c`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override async Task Lazy_load_many_to_one_reference_to_principal_not_found(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        {
            await base.Lazy_load_many_to_one_reference_to_principal_not_found(state, queryTrackingBehavior, async);

            AssertSql(
                state == EntityState.Detached
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='787'")}

SELECT TOP 1 `p`.`Id`, `p`.`AlternateId`
FROM `Parent` AS `p`
WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override async Task Lazy_load_one_to_one_reference_to_principal_not_found(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        {
            await base.Lazy_load_one_to_one_reference_to_principal_not_found(state, queryTrackingBehavior, async);

            AssertSql(
                state == EntityState.Detached
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='787'")}

SELECT TOP 1 `p`.`Id`, `p`.`AlternateId`
FROM `Parent` AS `p`
WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override async Task Lazy_load_one_to_one_reference_to_dependent_not_found(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        {
            await base.Lazy_load_one_to_one_reference_to_dependent_not_found(state, queryTrackingBehavior, async);

            AssertSql(
                state == EntityState.Detached
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='767' (Nullable = true)")}

SELECT TOP 1 `s`.`Id`, `s`.`ParentId`
FROM `Single` AS `s`
WHERE `s`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override async Task Lazy_load_collection_already_loaded(EntityState state, CascadeTiming cascadeDeleteTiming, QueryTrackingBehavior queryTrackingBehavior, bool async)
        {
            await base.Lazy_load_collection_already_loaded(state, cascadeDeleteTiming, queryTrackingBehavior, async);

            AssertSql();
        }

        public override async Task Lazy_load_many_to_one_reference_to_principal_already_loaded(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        {
            await base.Lazy_load_many_to_one_reference_to_principal_already_loaded(state, queryTrackingBehavior, async);

            AssertSql();
        }

        public override async Task Lazy_load_one_to_one_reference_to_principal_already_loaded(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        {
            await base.Lazy_load_one_to_one_reference_to_principal_already_loaded(state, queryTrackingBehavior, async);

            AssertSql();
        }

        public override async Task Lazy_load_one_to_one_reference_to_dependent_already_loaded(
            EntityState state, CascadeTiming cascadeDeleteTiming, QueryTrackingBehavior queryTrackingBehavior, bool async)
        {
            await base.Lazy_load_one_to_one_reference_to_dependent_already_loaded(state, cascadeDeleteTiming, queryTrackingBehavior, async);

            AssertSql();
        }

        public override void Lazy_load_one_to_one_PK_to_PK_reference_to_principal_already_loaded(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        {
            base.Lazy_load_one_to_one_PK_to_PK_reference_to_principal_already_loaded(state, queryTrackingBehavior);

            AssertSql();
        }

        public override void Lazy_load_one_to_one_PK_to_PK_reference_to_dependent_already_loaded(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        {
            base.Lazy_load_one_to_one_PK_to_PK_reference_to_dependent_already_loaded(state, queryTrackingBehavior);

            AssertSql();
        }

        public override void Lazy_load_many_to_one_reference_to_principal_alternate_key(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        {
            base.Lazy_load_many_to_one_reference_to_principal_alternate_key(state, queryTrackingBehavior);

            AssertSql(
                state == EntityState.Detached && queryTrackingBehavior == QueryTrackingBehavior.TrackAll
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='Root' (Size = 255)")}

SELECT TOP 1 `p`.`Id`, `p`.`AlternateId`
FROM `Parent` AS `p`
WHERE `p`.`AlternateId` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override void Lazy_load_one_to_one_reference_to_principal_alternate_key(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        {
            base.Lazy_load_one_to_one_reference_to_principal_alternate_key(state, queryTrackingBehavior);

            AssertSql(
                state == EntityState.Detached && queryTrackingBehavior == QueryTrackingBehavior.TrackAll
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='Root' (Size = 255)")}

SELECT TOP 1 `p`.`Id`, `p`.`AlternateId`
FROM `Parent` AS `p`
WHERE `p`.`AlternateId` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override void Lazy_load_one_to_one_reference_to_dependent_alternate_key(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        {
            base.Lazy_load_one_to_one_reference_to_dependent_alternate_key(state, queryTrackingBehavior);

            AssertSql(
                state == EntityState.Detached && queryTrackingBehavior == QueryTrackingBehavior.TrackAll
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='Root' (Size = 255)")}

SELECT TOP 1 `s`.`Id`, `s`.`ParentId`
FROM `SingleAk` AS `s`
WHERE `s`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override void Lazy_load_many_to_one_reference_to_principal_null_FK_alternate_key(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        {
            base.Lazy_load_many_to_one_reference_to_principal_null_FK_alternate_key(state, queryTrackingBehavior);

            AssertSql();
        }

        public override void Lazy_load_one_to_one_reference_to_principal_null_FK_alternate_key(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        {
            base.Lazy_load_one_to_one_reference_to_principal_null_FK_alternate_key(state, queryTrackingBehavior);

            AssertSql();
        }

        public override void Lazy_load_collection_shadow_fk(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        {
            base.Lazy_load_collection_shadow_fk(state, queryTrackingBehavior);

            AssertSql(
                state == EntityState.Detached && queryTrackingBehavior == QueryTrackingBehavior.TrackAll
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='707' (Nullable = true)")}

SELECT `c`.`Id`, `c`.`ParentId`
FROM `ChildShadowFk` AS `c`
WHERE `c`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override void Lazy_load_many_to_one_reference_to_principal_shadow_fk(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        {
            base.Lazy_load_many_to_one_reference_to_principal_shadow_fk(state, queryTrackingBehavior);

            AssertSql(
                state == EntityState.Detached || queryTrackingBehavior != QueryTrackingBehavior.TrackAll
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='707'")}

SELECT TOP 1 `p`.`Id`, `p`.`AlternateId`
FROM `Parent` AS `p`
WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override void Lazy_load_one_to_one_reference_to_principal_shadow_fk(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        {
            base.Lazy_load_one_to_one_reference_to_principal_shadow_fk(state, queryTrackingBehavior);

            AssertSql(
                state == EntityState.Detached || queryTrackingBehavior != QueryTrackingBehavior.TrackAll
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='707'")}

SELECT TOP 1 `p`.`Id`, `p`.`AlternateId`
FROM `Parent` AS `p`
WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override void Lazy_load_one_to_one_reference_to_dependent_shadow_fk(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        {
            base.Lazy_load_one_to_one_reference_to_dependent_shadow_fk(state, queryTrackingBehavior);

            AssertSql(
                state == EntityState.Detached && queryTrackingBehavior == QueryTrackingBehavior.TrackAll
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='707' (Nullable = true)")}

SELECT TOP 1 `s`.`Id`, `s`.`ParentId`
FROM `SingleShadowFk` AS `s`
WHERE `s`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override void Lazy_load_many_to_one_reference_to_principal_null_FK_shadow_fk(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        {
            base.Lazy_load_many_to_one_reference_to_principal_null_FK_shadow_fk(state, queryTrackingBehavior);

            AssertSql();
        }

        public override void Lazy_load_one_to_one_reference_to_principal_null_FK_shadow_fk(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        {
            base.Lazy_load_one_to_one_reference_to_principal_null_FK_shadow_fk(state, queryTrackingBehavior);

            AssertSql();
        }

        public override void Lazy_load_collection_composite_key(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        {
            base.Lazy_load_collection_composite_key(state, queryTrackingBehavior);

            AssertSql(
                state == EntityState.Detached && queryTrackingBehavior == QueryTrackingBehavior.TrackAll
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='Root' (Size = 255)")}
{AssertSqlHelper.Declaration("@p0='707' (Nullable = true)")}

SELECT `c`.`Id`, `c`.`ParentAlternateId`, `c`.`ParentId`
FROM `ChildCompositeKey` AS `c`
WHERE `c`.`ParentAlternateId` = {AssertSqlHelper.Parameter("@p")} AND `c`.`ParentId` = {AssertSqlHelper.Parameter("@p0")}
""");
        }

        public override void Lazy_load_many_to_one_reference_to_principal_composite_key(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        {
            base.Lazy_load_many_to_one_reference_to_principal_composite_key(state, queryTrackingBehavior);

            AssertSql(
                state == EntityState.Detached && queryTrackingBehavior == QueryTrackingBehavior.TrackAll
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='Root' (Size = 255)")}
{AssertSqlHelper.Declaration("@p0='707'")}

SELECT TOP 1 `p`.`Id`, `p`.`AlternateId`
FROM `Parent` AS `p`
WHERE `p`.`AlternateId` = {AssertSqlHelper.Parameter("@p")} AND `p`.`Id` = {AssertSqlHelper.Parameter("@p0")}
""");
        }

        public override void Lazy_load_one_to_one_reference_to_principal_composite_key(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        {
            base.Lazy_load_one_to_one_reference_to_principal_composite_key(state, queryTrackingBehavior);

            AssertSql(
                state == EntityState.Detached && queryTrackingBehavior == QueryTrackingBehavior.TrackAll
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='Root' (Size = 255)")}
{AssertSqlHelper.Declaration("@p0='707'")}

SELECT TOP 1 `p`.`Id`, `p`.`AlternateId`
FROM `Parent` AS `p`
WHERE `p`.`AlternateId` = {AssertSqlHelper.Parameter("@p")} AND `p`.`Id` = {AssertSqlHelper.Parameter("@p0")}
""");
        }

        public override void Lazy_load_one_to_one_reference_to_dependent_composite_key(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        {
            base.Lazy_load_one_to_one_reference_to_dependent_composite_key(state, queryTrackingBehavior);

            AssertSql(
                state == EntityState.Detached && queryTrackingBehavior == QueryTrackingBehavior.TrackAll
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='Root' (Size = 255)")}
{AssertSqlHelper.Declaration("@p0='707' (Nullable = true)")}

SELECT TOP 1 `s`.`Id`, `s`.`ParentAlternateId`, `s`.`ParentId`
FROM `SingleCompositeKey` AS `s`
WHERE `s`.`ParentAlternateId` = {AssertSqlHelper.Parameter("@p")} AND `s`.`ParentId` = {AssertSqlHelper.Parameter("@p0")}
""");
        }

        public override void Lazy_load_many_to_one_reference_to_principal_null_FK_composite_key(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        {
            base.Lazy_load_many_to_one_reference_to_principal_null_FK_composite_key(state, queryTrackingBehavior);

            AssertSql();
        }

        public override void Lazy_load_one_to_one_reference_to_principal_null_FK_composite_key(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        {
            base.Lazy_load_one_to_one_reference_to_principal_null_FK_composite_key(state, queryTrackingBehavior);

            AssertSql();
        }

        public override async Task Load_collection(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        {
            await base.Load_collection(state, queryTrackingBehavior, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='707' (Nullable = true)")}
                    
                    SELECT `c`.`Id`, `c`.`ParentId`
                    FROM `Child` AS `c`
                    WHERE `c`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_many_to_one_reference_to_principal(EntityState state, bool async)
        {
            await base.Load_many_to_one_reference_to_principal(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='707'")}
                    
                    SELECT TOP 1 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_one_to_one_reference_to_principal(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_principal(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='707'")}
                    
                    SELECT TOP 1 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_one_to_one_reference_to_dependent(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_dependent(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='707' (Nullable = true)")}
                    
                    SELECT TOP 1 `s`.`Id`, `s`.`ParentId`
                    FROM `Single` AS `s`
                    WHERE `s`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_one_to_one_PK_to_PK_reference_to_principal(EntityState state, bool async)
        {
            await base.Load_one_to_one_PK_to_PK_reference_to_principal(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='707'")}
                    
                    SELECT TOP 1 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_one_to_one_PK_to_PK_reference_to_dependent(EntityState state, bool async)
        {
            await base.Load_one_to_one_PK_to_PK_reference_to_dependent(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='707'")}
                    
                    SELECT TOP 1 `s`.`Id`
                    FROM `SinglePkToPk` AS `s`
                    WHERE `s`.`Id` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_collection_using_Query(EntityState state, bool async)
        {
            await base.Load_collection_using_Query(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='707' (Nullable = true)")}
                    
                    SELECT `c`.`Id`, `c`.`ParentId`
                    FROM `Child` AS `c`
                    WHERE `c`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_many_to_one_reference_to_principal_using_Query(EntityState state, bool async)
        {
            await base.Load_many_to_one_reference_to_principal_using_Query(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='707'")}
                    
                    SELECT TOP 2 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_one_to_one_reference_to_principal_using_Query(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_principal_using_Query(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='707'")}
                    
                    SELECT TOP 2 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_one_to_one_reference_to_dependent_using_Query(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_dependent_using_Query(state, async);

            AssertSql(
                state == EntityState.Detached
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='707' (Nullable = true)")}

SELECT TOP 2 `s`.`Id`, `s`.`ParentId`
FROM `Single` AS `s`
WHERE `s`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override async Task Load_one_to_one_PK_to_PK_reference_to_principal_using_Query(EntityState state, bool async)
        {
            await base.Load_one_to_one_PK_to_PK_reference_to_principal_using_Query(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='707'")}
                    
                    SELECT TOP 2 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_one_to_one_PK_to_PK_reference_to_dependent_using_Query(EntityState state, bool async)
        {
            await base.Load_one_to_one_PK_to_PK_reference_to_dependent_using_Query(state, async);

            AssertSql(
                state == EntityState.Detached
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='707'")}

SELECT TOP 2 `s`.`Id`
FROM `SinglePkToPk` AS `s`
WHERE `s`.`Id` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override async Task Load_many_to_one_reference_to_principal_null_FK(EntityState state, bool async)
        {
            await base.Load_many_to_one_reference_to_principal_null_FK(state, async);

            AssertSql();
        }

        public override async Task Load_one_to_one_reference_to_principal_null_FK(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_principal_null_FK(state, async);

            AssertSql();
        }

        public override async Task Load_many_to_one_reference_to_principal_using_Query_null_FK(EntityState state, bool async)
        {
            await base.Load_many_to_one_reference_to_principal_using_Query_null_FK(state, async);

            AssertSql(
                $"""
                    SELECT TOP 2 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE 0 = 1
                    """);
        }

        public override async Task Load_one_to_one_reference_to_principal_using_Query_null_FK(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_principal_using_Query_null_FK(state, async);

            AssertSql(
                $"""
                    SELECT TOP 2 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE 0 = 1
                    """);
        }

        public override async Task Load_collection_not_found(EntityState state, bool async)
        {
            await base.Load_collection_not_found(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='767' (Nullable = true)")}
                    
                    SELECT `c`.`Id`, `c`.`ParentId`
                    FROM `Child` AS `c`
                    WHERE `c`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_many_to_one_reference_to_principal_not_found(EntityState state, bool async)
        {
            await base.Load_many_to_one_reference_to_principal_not_found(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='787'")}
                    
                    SELECT TOP 1 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_one_to_one_reference_to_principal_not_found(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_principal_not_found(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='787'")}
                    
                    SELECT TOP 1 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_one_to_one_reference_to_dependent_not_found(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_dependent_not_found(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='767' (Nullable = true)")}
                    
                    SELECT TOP 1 `s`.`Id`, `s`.`ParentId`
                    FROM `Single` AS `s`
                    WHERE `s`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_collection_using_Query_not_found(EntityState state, bool async)
        {
            await base.Load_collection_using_Query_not_found(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='767' (Nullable = true)")}
                    
                    SELECT `c`.`Id`, `c`.`ParentId`
                    FROM `Child` AS `c`
                    WHERE `c`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_many_to_one_reference_to_principal_using_Query_not_found(EntityState state, bool async)
        {
            await base.Load_many_to_one_reference_to_principal_using_Query_not_found(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='787'")}
                    
                    SELECT TOP 2 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_one_to_one_reference_to_principal_using_Query_not_found(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_principal_using_Query_not_found(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='787'")}
                    
                    SELECT TOP 2 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_one_to_one_reference_to_dependent_using_Query_not_found(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_dependent_using_Query_not_found(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='767' (Nullable = true)")}
                    
                    SELECT TOP 2 `s`.`Id`, `s`.`ParentId`
                    FROM `Single` AS `s`
                    WHERE `s`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_collection_already_loaded(EntityState state, bool async, CascadeTiming cascadeDeleteTiming)
        {
            await base.Load_collection_already_loaded(state, async, cascadeDeleteTiming);

            AssertSql($@"");
        }

        public override async Task Load_many_to_one_reference_to_principal_already_loaded(EntityState state, bool async)
        {
            await base.Load_many_to_one_reference_to_principal_already_loaded(state, async);

            AssertSql($@"");
        }

        public override async Task Load_one_to_one_reference_to_principal_already_loaded(
            EntityState state, bool async, CascadeTiming cascadeDeleteTiming)
        {
            await base.Load_one_to_one_reference_to_principal_already_loaded(state, async, cascadeDeleteTiming);

            AssertSql($@"");
        }

        public override async Task Load_one_to_one_reference_to_dependent_already_loaded(
            EntityState state, bool async, CascadeTiming cascadeDeleteTiming)
        {
            await base.Load_one_to_one_reference_to_dependent_already_loaded(state, async, cascadeDeleteTiming);

            AssertSql($@"");
        }

        public override async Task Load_one_to_one_PK_to_PK_reference_to_principal_already_loaded(EntityState state, bool async)
        {
            await base.Load_one_to_one_PK_to_PK_reference_to_principal_already_loaded(state, async);

            AssertSql($@"");
        }

        public override async Task Load_one_to_one_PK_to_PK_reference_to_dependent_already_loaded(EntityState state, bool async)
        {
            await base.Load_one_to_one_PK_to_PK_reference_to_dependent_already_loaded(state, async);

            AssertSql($@"");
        }

        public override async Task Load_collection_using_Query_already_loaded(
            EntityState state, bool async, CascadeTiming cascadeDeleteTiming)
        {
            await base.Load_collection_using_Query_already_loaded(state, async, cascadeDeleteTiming);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='707' (Nullable = true)")}
                    
                    SELECT `c`.`Id`, `c`.`ParentId`
                    FROM `Child` AS `c`
                    WHERE `c`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_many_to_one_reference_to_principal_using_Query_already_loaded(EntityState state, bool async)
        {
            await base.Load_many_to_one_reference_to_principal_using_Query_already_loaded(state, async);

            AssertSql(
                state == EntityState.Deleted
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='707'")}

SELECT TOP 2 `p`.`Id`, `p`.`AlternateId`
FROM `Parent` AS `p`
WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override async Task Load_one_to_one_reference_to_principal_using_Query_already_loaded(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_principal_using_Query_already_loaded(state, async);

            AssertSql(
                state == EntityState.Deleted
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='707'")}

SELECT TOP 2 `p`.`Id`, `p`.`AlternateId`
FROM `Parent` AS `p`
WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override async Task Load_one_to_one_reference_to_dependent_using_Query_already_loaded(
            EntityState state, bool async, CascadeTiming cascadeDeleteTiming)
        {
            await base.Load_one_to_one_reference_to_dependent_using_Query_already_loaded(state, async, cascadeDeleteTiming);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='707' (Nullable = true)")}
                    
                    SELECT TOP 2 `s`.`Id`, `s`.`ParentId`
                    FROM `Single` AS `s`
                    WHERE `s`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_one_to_one_PK_to_PK_reference_to_principal_using_Query_already_loaded(EntityState state, bool async)
        {
            await base.Load_one_to_one_PK_to_PK_reference_to_principal_using_Query_already_loaded(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='707'")}
                    
                    SELECT TOP 2 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_one_to_one_PK_to_PK_reference_to_dependent_using_Query_already_loaded(EntityState state, bool async)
        {
            await base.Load_one_to_one_PK_to_PK_reference_to_dependent_using_Query_already_loaded(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='707'")}
                    
                    SELECT TOP 2 `s`.`Id`
                    FROM `SinglePkToPk` AS `s`
                    WHERE `s`.`Id` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_collection_untyped(EntityState state, bool async)
        {
            await base.Load_collection_untyped(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='707' (Nullable = true)")}
                    
                    SELECT `c`.`Id`, `c`.`ParentId`
                    FROM `Child` AS `c`
                    WHERE `c`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_many_to_one_reference_to_principal_untyped(EntityState state, bool async)
        {
            await base.Load_many_to_one_reference_to_principal_untyped(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='707'")}
                    
                    SELECT TOP 1 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_one_to_one_reference_to_principal_untyped(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_principal_untyped(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='707'")}
                    
                    SELECT TOP 1 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_one_to_one_reference_to_dependent_untyped(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_dependent_untyped(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='707' (Nullable = true)")}
                    
                    SELECT TOP 1 `s`.`Id`, `s`.`ParentId`
                    FROM `Single` AS `s`
                    WHERE `s`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_collection_using_Query_untyped(EntityState state, bool async)
        {
            await base.Load_collection_using_Query_untyped(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='707' (Nullable = true)")}
                    
                    SELECT `c`.`Id`, `c`.`ParentId`
                    FROM `Child` AS `c`
                    WHERE `c`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_many_to_one_reference_to_principal_using_Query_untyped(EntityState state, bool async)
        {
            await base.Load_many_to_one_reference_to_principal_using_Query_untyped(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='707'")}
                    
                    SELECT `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_one_to_one_reference_to_principal_using_Query_untyped(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_principal_using_Query_untyped(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='707'")}
                    
                    SELECT `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_one_to_one_reference_to_dependent_using_Query_untyped(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_dependent_using_Query_untyped(state, async);

            AssertSql(
                state == EntityState.Detached
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='707' (Nullable = true)")}

SELECT `s`.`Id`, `s`.`ParentId`
FROM `Single` AS `s`
WHERE `s`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override async Task Load_collection_not_found_untyped(EntityState state, bool async)
        {
            await base.Load_collection_not_found_untyped(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='767' (Nullable = true)")}
                    
                    SELECT `c`.`Id`, `c`.`ParentId`
                    FROM `Child` AS `c`
                    WHERE `c`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_many_to_one_reference_to_principal_not_found_untyped(EntityState state, bool async)
        {
            await base.Load_many_to_one_reference_to_principal_not_found_untyped(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='787'")}
                    
                    SELECT TOP 1 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_one_to_one_reference_to_principal_not_found_untyped(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_principal_not_found_untyped(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='787'")}
                    
                    SELECT TOP 1 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_one_to_one_reference_to_dependent_not_found_untyped(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_dependent_not_found_untyped(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='767' (Nullable = true)")}
                    
                    SELECT TOP 1 `s`.`Id`, `s`.`ParentId`
                    FROM `Single` AS `s`
                    WHERE `s`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_collection_using_Query_not_found_untyped(EntityState state, bool async)
        {
            await base.Load_collection_using_Query_not_found_untyped(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='767' (Nullable = true)")}
                    
                    SELECT `c`.`Id`, `c`.`ParentId`
                    FROM `Child` AS `c`
                    WHERE `c`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_many_to_one_reference_to_principal_using_Query_not_found_untyped(EntityState state, bool async)
        {
            await base.Load_many_to_one_reference_to_principal_using_Query_not_found_untyped(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='787'")}
                    
                    SELECT `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_one_to_one_reference_to_principal_using_Query_not_found_untyped(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_principal_using_Query_not_found_untyped(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='787'")}
                    
                    SELECT `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_one_to_one_reference_to_dependent_using_Query_not_found_untyped(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_dependent_using_Query_not_found_untyped(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='767' (Nullable = true)")}
                    
                    SELECT `s`.`Id`, `s`.`ParentId`
                    FROM `Single` AS `s`
                    WHERE `s`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_collection_already_loaded_untyped(EntityState state, bool async, CascadeTiming cascadeDeleteTiming)
        {
            await base.Load_collection_already_loaded_untyped(state, async, cascadeDeleteTiming);

            AssertSql($@"");
        }

        public override async Task Load_many_to_one_reference_to_principal_already_loaded_untyped(EntityState state, bool async)
        {
            await base.Load_many_to_one_reference_to_principal_already_loaded_untyped(state, async);

            AssertSql($@"");
        }

        public override async Task Load_one_to_one_reference_to_principal_already_loaded_untyped(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_principal_already_loaded_untyped(state, async);

            AssertSql($@"");
        }

        public override async Task Load_one_to_one_reference_to_dependent_already_loaded_untyped(
            EntityState state, bool async, CascadeTiming cascadeDeleteTiming)
        {
            await base.Load_one_to_one_reference_to_dependent_already_loaded_untyped(state, async, cascadeDeleteTiming);

            AssertSql($@"");
        }

        public override async Task Load_collection_using_Query_already_loaded_untyped(
            EntityState state, bool async, CascadeTiming cascadeDeleteTiming)
        {
            await base.Load_collection_using_Query_already_loaded_untyped(state, async, cascadeDeleteTiming);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='707' (Nullable = true)")}
                    
                    SELECT `c`.`Id`, `c`.`ParentId`
                    FROM `Child` AS `c`
                    WHERE `c`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_many_to_one_reference_to_principal_using_Query_already_loaded_untyped(EntityState state, bool async)
        {
            await base.Load_many_to_one_reference_to_principal_using_Query_already_loaded_untyped(state, async);

            AssertSql(
                state == EntityState.Deleted
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='707'")}

SELECT `p`.`Id`, `p`.`AlternateId`
FROM `Parent` AS `p`
WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override async Task Load_one_to_one_reference_to_principal_using_Query_already_loaded_untyped(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_principal_using_Query_already_loaded_untyped(state, async);

            AssertSql(
                state == EntityState.Deleted
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='707'")}

SELECT `p`.`Id`, `p`.`AlternateId`
FROM `Parent` AS `p`
WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override async Task Load_one_to_one_reference_to_dependent_using_Query_already_loaded_untyped(
            EntityState state, bool async, CascadeTiming cascadeDeleteTiming)
        {
            await base.Load_one_to_one_reference_to_dependent_using_Query_already_loaded_untyped(state, async, cascadeDeleteTiming);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='707' (Nullable = true)")}
                    
                    SELECT `s`.`Id`, `s`.`ParentId`
                    FROM `Single` AS `s`
                    WHERE `s`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_collection_alternate_key(EntityState state, bool async)
        {
            await base.Load_collection_alternate_key(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='Root' (Size = 255)")}
                    
                    SELECT `c`.`Id`, `c`.`ParentId`
                    FROM `ChildAk` AS `c`
                    WHERE `c`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_many_to_one_reference_to_principal_alternate_key(EntityState state, bool async)
        {
            await base.Load_many_to_one_reference_to_principal_alternate_key(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='Root' (Size = 255)")}
                    
                    SELECT TOP 1 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE `p`.`AlternateId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_one_to_one_reference_to_principal_alternate_key(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_principal_alternate_key(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='Root' (Size = 255)")}
                    
                    SELECT TOP 1 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE `p`.`AlternateId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_one_to_one_reference_to_dependent_alternate_key(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_dependent_alternate_key(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='Root' (Size = 255)")}
                    
                    SELECT TOP 1 `s`.`Id`, `s`.`ParentId`
                    FROM `SingleAk` AS `s`
                    WHERE `s`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_collection_using_Query_alternate_key(EntityState state, bool async)
        {
            await base.Load_collection_using_Query_alternate_key(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='Root' (Size = 255)")}
                    
                    SELECT `c`.`Id`, `c`.`ParentId`
                    FROM `ChildAk` AS `c`
                    WHERE `c`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_many_to_one_reference_to_principal_using_Query_alternate_key(EntityState state, bool async)
        {
            await base.Load_many_to_one_reference_to_principal_using_Query_alternate_key(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='Root' (Size = 255)")}
                    
                    SELECT TOP 2 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE `p`.`AlternateId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_one_to_one_reference_to_principal_using_Query_alternate_key(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_principal_using_Query_alternate_key(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='Root' (Size = 255)")}
                    
                    SELECT TOP 2 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE `p`.`AlternateId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_one_to_one_reference_to_dependent_using_Query_alternate_key(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_dependent_using_Query_alternate_key(state, async);

            AssertSql(
                state == EntityState.Detached
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='Root' (Size = 255)")}

SELECT TOP 2 `s`.`Id`, `s`.`ParentId`
FROM `SingleAk` AS `s`
WHERE `s`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override async Task Load_many_to_one_reference_to_principal_null_FK_alternate_key(EntityState state, bool async)
        {
            await base.Load_many_to_one_reference_to_principal_null_FK_alternate_key(state, async);

            AssertSql();
        }

        public override async Task Load_one_to_one_reference_to_principal_null_FK_alternate_key(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_principal_null_FK_alternate_key(state, async);

            AssertSql();
        }

        public override async Task Load_many_to_one_reference_to_principal_using_Query_null_FK_alternate_key(EntityState state, bool async)
        {
            await base.Load_many_to_one_reference_to_principal_using_Query_null_FK_alternate_key(state, async);

            AssertSql(
                $"""
                    SELECT TOP 2 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE 0 = 1
                    """);
        }

        public override async Task Load_one_to_one_reference_to_principal_using_Query_null_FK_alternate_key(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_principal_using_Query_null_FK_alternate_key(state, async);

            AssertSql(
                $"""
                    SELECT TOP 2 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE 0 = 1
                    """);
        }

        public override async Task Load_collection_shadow_fk(EntityState state, bool async)
        {
            await base.Load_collection_shadow_fk(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='707' (Nullable = true)")}
                    
                    SELECT `c`.`Id`, `c`.`ParentId`
                    FROM `ChildShadowFk` AS `c`
                    WHERE `c`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_many_to_one_reference_to_principal_shadow_fk(EntityState state, bool async)
        {
            await base.Load_many_to_one_reference_to_principal_shadow_fk(state, async);

            AssertSql(
                state == EntityState.Detached
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='707'")}

SELECT TOP 1 `p`.`Id`, `p`.`AlternateId`
FROM `Parent` AS `p`
WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override async Task Load_one_to_one_reference_to_principal_shadow_fk(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_principal_shadow_fk(state, async);

            AssertSql(
                state == EntityState.Detached
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='707'")}

SELECT TOP 1 `p`.`Id`, `p`.`AlternateId`
FROM `Parent` AS `p`
WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override async Task Load_one_to_one_reference_to_dependent_shadow_fk(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_dependent_shadow_fk(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='707' (Nullable = true)")}
                    
                    SELECT TOP 1 `s`.`Id`, `s`.`ParentId`
                    FROM `SingleShadowFk` AS `s`
                    WHERE `s`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_collection_using_Query_shadow_fk(EntityState state, bool async)
        {
            await base.Load_collection_using_Query_shadow_fk(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='707' (Nullable = true)")}
                    
                    SELECT `c`.`Id`, `c`.`ParentId`
                    FROM `ChildShadowFk` AS `c`
                    WHERE `c`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
                    """);
        }

        public override async Task Load_many_to_one_reference_to_principal_using_Query_shadow_fk(EntityState state, bool async)
        {
            await base.Load_many_to_one_reference_to_principal_using_Query_shadow_fk(state, async);

            AssertSql(
                state == EntityState.Detached
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='707'")}

SELECT TOP 2 `p`.`Id`, `p`.`AlternateId`
FROM `Parent` AS `p`
WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override async Task Load_one_to_one_reference_to_principal_using_Query_shadow_fk(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_principal_using_Query_shadow_fk(state, async);

            AssertSql(
                state == EntityState.Detached
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='707'")}

SELECT TOP 2 `p`.`Id`, `p`.`AlternateId`
FROM `Parent` AS `p`
WHERE `p`.`Id` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override async Task Load_one_to_one_reference_to_dependent_using_Query_shadow_fk(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_dependent_using_Query_shadow_fk(state, async);

            AssertSql(
                state == EntityState.Detached
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='707' (Nullable = true)")}

SELECT TOP 2 `s`.`Id`, `s`.`ParentId`
FROM `SingleShadowFk` AS `s`
WHERE `s`.`ParentId` = {AssertSqlHelper.Parameter("@p")}
""");
        }

        public override async Task Load_many_to_one_reference_to_principal_null_FK_shadow_fk(EntityState state, bool async)
        {
            await base.Load_many_to_one_reference_to_principal_null_FK_shadow_fk(state, async);

            AssertSql();
        }

        public override async Task Load_one_to_one_reference_to_principal_null_FK_shadow_fk(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_principal_null_FK_shadow_fk(state, async);

            AssertSql();
        }

        public override async Task Load_many_to_one_reference_to_principal_using_Query_null_FK_shadow_fk(EntityState state, bool async)
        {
            await base.Load_many_to_one_reference_to_principal_using_Query_null_FK_shadow_fk(state, async);

            AssertSql(
                state == EntityState.Detached
                    ? ""
                    : """
SELECT TOP 2 `p`.`Id`, `p`.`AlternateId`
FROM `Parent` AS `p`
WHERE 0 = 1
""");
        }

        public override async Task Load_one_to_one_reference_to_principal_using_Query_null_FK_shadow_fk(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_principal_using_Query_null_FK_shadow_fk(state, async);

            AssertSql(
                state == EntityState.Detached
                    ? ""
                    : """
SELECT TOP 2 `p`.`Id`, `p`.`AlternateId`
FROM `Parent` AS `p`
WHERE 0 = 1
""");
        }

        public override async Task Load_collection_composite_key(EntityState state, bool async)
        {
            await base.Load_collection_composite_key(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='Root' (Size = 255)")}
                    {AssertSqlHelper.Declaration("@p0='707' (Nullable = true)")}
                    
                    SELECT `c`.`Id`, `c`.`ParentAlternateId`, `c`.`ParentId`
                    FROM `ChildCompositeKey` AS `c`
                    WHERE `c`.`ParentAlternateId` = {AssertSqlHelper.Parameter("@p")} AND `c`.`ParentId` = {AssertSqlHelper.Parameter("@p0")}
                    """);
        }

        public override async Task Load_many_to_one_reference_to_principal_composite_key(EntityState state, bool async)
        {
            await base.Load_many_to_one_reference_to_principal_composite_key(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='Root' (Size = 255)")}
                    {AssertSqlHelper.Declaration("@p0='707'")}
                    
                    SELECT TOP 1 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE `p`.`AlternateId` = {AssertSqlHelper.Parameter("@p")} AND `p`.`Id` = {AssertSqlHelper.Parameter("@p0")}
                    """);
        }

        public override async Task Load_one_to_one_reference_to_principal_composite_key(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_principal_composite_key(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='Root' (Size = 255)")}
                    {AssertSqlHelper.Declaration("@p0='707'")}
                    
                    SELECT TOP 1 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE `p`.`AlternateId` = {AssertSqlHelper.Parameter("@p")} AND `p`.`Id` = {AssertSqlHelper.Parameter("@p0")}
                    """);
        }

        public override async Task Load_one_to_one_reference_to_dependent_composite_key(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_dependent_composite_key(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='Root' (Size = 255)")}
                    {AssertSqlHelper.Declaration("@p0='707' (Nullable = true)")}
                    
                    SELECT TOP 1 `s`.`Id`, `s`.`ParentAlternateId`, `s`.`ParentId`
                    FROM `SingleCompositeKey` AS `s`
                    WHERE `s`.`ParentAlternateId` = {AssertSqlHelper.Parameter("@p")} AND `s`.`ParentId` = {AssertSqlHelper.Parameter("@p0")}
                    """);
        }

        public override async Task Load_collection_using_Query_composite_key(EntityState state, bool async)
        {
            await base.Load_collection_using_Query_composite_key(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='Root' (Size = 255)")}
                    {AssertSqlHelper.Declaration("@p0='707' (Nullable = true)")}
                    
                    SELECT `c`.`Id`, `c`.`ParentAlternateId`, `c`.`ParentId`
                    FROM `ChildCompositeKey` AS `c`
                    WHERE `c`.`ParentAlternateId` = {AssertSqlHelper.Parameter("@p")} AND `c`.`ParentId` = {AssertSqlHelper.Parameter("@p0")}
                    """);
        }

        public override async Task Load_many_to_one_reference_to_principal_using_Query_composite_key(EntityState state, bool async)
        {
            await base.Load_many_to_one_reference_to_principal_using_Query_composite_key(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='Root' (Size = 255)")}
                    {AssertSqlHelper.Declaration("@p0='707'")}
                    
                    SELECT TOP 2 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE `p`.`AlternateId` = {AssertSqlHelper.Parameter("@p")} AND `p`.`Id` = {AssertSqlHelper.Parameter("@p0")}
                    """);
        }

        public override async Task Load_one_to_one_reference_to_principal_using_Query_composite_key(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_principal_using_Query_composite_key(state, async);

            AssertSql(
                $"""
                    {AssertSqlHelper.Declaration("@p='Root' (Size = 255)")}
                    {AssertSqlHelper.Declaration("@p0='707'")}
                    
                    SELECT TOP 2 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE `p`.`AlternateId` = {AssertSqlHelper.Parameter("@p")} AND `p`.`Id` = {AssertSqlHelper.Parameter("@p0")}
                    """);
        }

        public override async Task Load_one_to_one_reference_to_dependent_using_Query_composite_key(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_dependent_using_Query_composite_key(state, async);

            AssertSql(
                state == EntityState.Detached
                    ? ""
                    : $"""
{AssertSqlHelper.Declaration("@p='Root' (Size = 255)")}
{AssertSqlHelper.Declaration("@p0='707' (Nullable = true)")}

SELECT TOP 2 `s`.`Id`, `s`.`ParentAlternateId`, `s`.`ParentId`
FROM `SingleCompositeKey` AS `s`
WHERE `s`.`ParentAlternateId` = {AssertSqlHelper.Parameter("@p")} AND `s`.`ParentId` = {AssertSqlHelper.Parameter("@p0")}
""");
        }

        public override async Task Load_many_to_one_reference_to_principal_null_FK_composite_key(EntityState state, bool async)
        {
            await base.Load_many_to_one_reference_to_principal_null_FK_composite_key(state, async);

            AssertSql();
        }

        public override async Task Load_one_to_one_reference_to_principal_null_FK_composite_key(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_principal_null_FK_composite_key(state, async);

            AssertSql();
        }

        public override async Task Load_many_to_one_reference_to_principal_using_Query_null_FK_composite_key(EntityState state, bool async)
        {
            await base.Load_many_to_one_reference_to_principal_using_Query_null_FK_composite_key(state, async);

            AssertSql(
                $"""
SELECT TOP 2 `p`.`Id`, `p`.`AlternateId`
FROM `Parent` AS `p`
WHERE 0 = 1
""");
        }

        public override async Task Load_one_to_one_reference_to_principal_using_Query_null_FK_composite_key(EntityState state, bool async)
        {
            await base.Load_one_to_one_reference_to_principal_using_Query_null_FK_composite_key(state, async);

            AssertSql(
                $"""
                    SELECT TOP 2 `p`.`Id`, `p`.`AlternateId`
                    FROM `Parent` AS `p`
                    WHERE 0 = 1
                    """);
        }

        protected override void ClearLog() => Fixture.TestSqlLoggerFactory.Clear();

        protected override void RecordLog() => Sql = Fixture.TestSqlLoggerFactory.Sql;

        private const string FileNewLine = """
            

            """;

        private void AssertSql(string? expected = null)
        {
            var sql = Sql ?? "";
            expected ??= "";
            try
            {
                Assert.Equal(
                    expected, sql, ignoreLineEndingDifferences: true);
            }
            catch
            {
                var methodCallLine = Environment.StackTrace.Split(
                    [Environment.NewLine],
                    StringSplitOptions.RemoveEmptyEntries)[2][6..];

                var indexMethodEnding = methodCallLine.IndexOf(')') + 1;
                var testName = methodCallLine[..indexMethodEnding];
                var parts = methodCallLine[indexMethodEnding..].Split(" ", StringSplitOptions.RemoveEmptyEntries);
                var fileName = parts[1][..^5];
                var lineNumber = int.Parse(parts[2]);

                var currentDirectory = Directory.GetCurrentDirectory();
                var logFile = currentDirectory[..(currentDirectory.LastIndexOf(
                            $"{Path.DirectorySeparatorChar}artifacts{Path.DirectorySeparatorChar}",
                            StringComparison.Ordinal)
                        + 1)]
                              + "QueryBaseline.txt";

                var testInfo = testName + " : " + lineNumber + FileNewLine;
                var newBaseLine = $"""
                                AssertSql(
                                    {"@\"" + sql.Replace("\"", "\"\"") + "\""});
                    

                    """;

                var contents = testInfo + newBaseLine + FileNewLine + "--------------------" + FileNewLine;

                File.AppendAllText(logFile, contents);

                throw;
            }
        }

        private string? Sql { get; set; }

        public class LoadJetFixture : LoadFixtureBase
        {
            public TestSqlLoggerFactory TestSqlLoggerFactory => (TestSqlLoggerFactory)ListLoggerFactory;
            protected override ITestStoreFactory TestStoreFactory => JetTestStoreFactory.Instance;
        }
    }
}
