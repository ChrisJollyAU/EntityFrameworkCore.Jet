using System;
using System.Collections.Generic;
using System.Data.Common;
using EntityFrameworkCore.Jet.Design.Internal;
using EntityFrameworkCore.Jet.FunctionalTests.TestUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.TestUtilities;
using System.Reflection;

namespace EntityFrameworkCore.Jet.FunctionalTests;

public class RuntimeMigrationJetTest(RuntimeMigrationJetTest.RuntimeMigrationJetFixture fixture)
    : RuntimeMigrationTestBase<RuntimeMigrationJetTest.RuntimeMigrationJetFixture>(fixture)
{
    protected override Assembly ProviderAssembly
        => typeof(JetDesignTimeServices).Assembly;

    protected override List<string> GetTableNames(DbConnection connection)
    {
        var tables = new List<string>();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT * FROM `INFORMATION_SCHEMA.TABLES` WHERE TABLE_NAME <> '__EFMigrationsHistory' AND TABLE_TYPE = 'BASE TABLE'";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            tables.Add(reader.GetString(reader.GetOrdinal("TABLE_NAME")));
        }

        return tables;
    }

    // EF's migrations generator imports only the context type's own namespace, never the namespaces of the
    // generic arguments in its name. The base context is RuntimeMigrationTestBase<RuntimeMigrationJetFixture>.
    // RuntimeMigrationDbContext, so the scaffolded code could not resolve this fixture's name. A context declared
    // here puts this namespace into the scaffolded usings. The migrations assembly stays the base context's, which
    // is the one the base test scaffolds into.
    public class RuntimeMigrationJetContext(DbContextOptions options) : RuntimeMigrationDbContext(options);

    public class RuntimeMigrationJetFixture : RuntimeMigrationFixtureBase
    {
        protected override Type ContextType
            => typeof(RuntimeMigrationJetContext);

        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            => base.AddOptions(builder)
                .UseJet(b => b.MigrationsAssembly(typeof(RuntimeMigrationDbContext).Assembly.GetName().Name));

        protected override ITestStoreFactory TestStoreFactory
            => JetTestStoreFactory.Instance;
    }
}
