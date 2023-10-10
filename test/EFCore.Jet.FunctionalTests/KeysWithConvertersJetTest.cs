// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using EntityFrameworkCore.Jet.FunctionalTests.TestUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.TestUtilities;

namespace EntityFrameworkCore.Jet.FunctionalTests;

public class KeysWithConvertersJetTest : KeysWithConvertersTestBase<
    KeysWithConvertersJetTest.KeysWithConvertersJetFixture>
{
    public KeysWithConvertersJetTest(KeysWithConvertersJetFixture fixture)
        : base(fixture)
    {
    }

    public class KeysWithConvertersJetFixture : KeysWithConvertersFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => JetTestStoreFactory.Instance;

        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            => builder.UseJet(JetTestStore.CreateConnectionString("KeysWithConvertersJetTest"), TestEnvironment.DataAccessProviderFactory);
    }
}
