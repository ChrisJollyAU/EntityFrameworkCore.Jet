﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Query.Relationships.Projection;
using Microsoft.EntityFrameworkCore.TestUtilities;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace EntityFrameworkCore.Jet.FunctionalTests.Query.Relationships.Projection;

public class OwnedTableSplittingReferenceProjectionNoTrackingJetTest
    : OwnedTableSplittingReferenceProjectionNoTrackingRelationalTestBase<OwnedTableSplittingRelationshipsJetFixture>
{
    public OwnedTableSplittingReferenceProjectionNoTrackingJetTest(OwnedTableSplittingRelationshipsJetFixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }

    public override async Task Select_root(bool async)
    {
        await base.Select_root(async);
    }

    public override async Task Select_trunk_optional(bool async)
    {
        await base.Select_trunk_optional(async);
    }

    public override async Task Select_trunk_required(bool async)
    {
        await base.Select_trunk_required(async);
    }

    public override async Task Select_branch_required_required(bool async)
    {
        await base.Select_branch_required_required(async);
    }

    public override async Task Select_branch_required_optional(bool async)
    {
        await base.Select_branch_required_optional(async);
    }

    public override async Task Select_branch_optional_required(bool async)
    {
        await base.Select_branch_optional_required(async);
    }

    public override async Task Select_branch_optional_optional(bool async)
    {
        await base.Select_branch_optional_optional(async);
    }

    public override async Task Select_root_duplicated(bool async)
    {
        await base.Select_root_duplicated(async);
    }

    public override async Task Select_trunk_and_branch_duplicated(bool async)
    {
        await base.Select_trunk_and_branch_duplicated(async);
    }

    public override async Task Select_trunk_and_trunk_duplicated(bool async)
    {
        await base.Select_trunk_and_trunk_duplicated(async);
    }

    public override async Task Select_leaf_trunk_root(bool async)
    {
        await base.Select_leaf_trunk_root(async);
    }

    public override async Task Select_subquery_root_set_required_trunk_FirstOrDefault_branch(bool async)
    {
        await base.Select_subquery_root_set_required_trunk_FirstOrDefault_branch(async);
    }

    public override async Task Select_subquery_root_set_optional_trunk_FirstOrDefault_branch(bool async)
    {
        await base.Select_subquery_root_set_optional_trunk_FirstOrDefault_branch(async);
    }

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    private void AssertSql(params string[] expected)
        => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);
}
