// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using EntityFrameworkCore.Jet.Data;
using EntityFrameworkCore.Jet.FunctionalTests.TestUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EntityFrameworkCore.Jet.FunctionalTests
{
    public abstract class ConnectionInterceptionJetTestBase(
        ConnectionInterceptionJetTestBase.InterceptionJetFixtureBase fixture)
        : ConnectionInterceptionTestBase(fixture)
    {
        public abstract class InterceptionJetFixtureBase : InterceptionFixtureBase
        {
            protected override string StoreName => "ConnectionInterception";
            protected override ITestStoreFactory TestStoreFactory => JetTestStoreFactory.Instance;

            protected override IServiceCollection InjectInterceptors(
                IServiceCollection serviceCollection,
                IEnumerable<IInterceptor> injectedInterceptors)
                => base.InjectInterceptors(serviceCollection.AddEntityFrameworkJet(), injectedInterceptors);
        }
        protected override DbContextOptionsBuilder ConfigureProvider(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseJet();

        protected override BadUniverseContext CreateBadUniverse(DbContextOptionsBuilder optionsBuilder)
            => new(optionsBuilder.UseJet(new FakeDbConnection()).Options);

        public class FakeDbConnection : DbConnection
        {
            [AllowNull]
            public override string ConnectionString { get; set; }
            public override string Database => "Database";
            public override string DataSource => "DataSource";
            public override string ServerVersion => throw new NotImplementedException();
            public override ConnectionState State => ConnectionState.Closed;
            public override void ChangeDatabase(string databaseName) => throw new NotImplementedException();
            public override void Close() => throw new NotImplementedException();
            public override void Open() => throw new NotImplementedException();
            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotImplementedException();
            protected override DbCommand CreateDbCommand() => throw new NotImplementedException();
        }

        public class ConnectionInterceptionJetTest(ConnectionInterceptionJetTest.InterceptionJetFixture fixture)
            : ConnectionInterceptionJetTestBase(fixture),
                IClassFixture<ConnectionInterceptionJetTest.InterceptionJetFixture>
        {
            public class InterceptionJetFixture : InterceptionJetFixtureBase
            {
                protected override bool ShouldSubscribeToDiagnosticListener => false;
            }
        }

        public class ConnectionInterceptionWithDiagnosticsJetTest(
            ConnectionInterceptionWithDiagnosticsJetTest.InterceptionJetFixture fixture)
            : ConnectionInterceptionJetTestBase(fixture),
                IClassFixture<ConnectionInterceptionWithDiagnosticsJetTest.InterceptionJetFixture>
        {
            public class InterceptionJetFixture : InterceptionJetFixtureBase
            {
                protected override bool ShouldSubscribeToDiagnosticListener => true;
            }
        }
    }
}
