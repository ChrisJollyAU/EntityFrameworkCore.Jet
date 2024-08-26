// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using EntityFrameworkCore.Jet.FunctionalTests.TestUtilities;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore;

#nullable disable

public abstract class ProxyGraphUpdatesJetTest
{
    public abstract class ProxyGraphUpdatesJetTestBase<TFixture> : ProxyGraphUpdatesTestBase<TFixture>
        where TFixture : ProxyGraphUpdatesJetTestBase<TFixture>.ProxyGraphUpdatesJetFixtureBase, new()
    {
        protected ProxyGraphUpdatesJetTestBase(TFixture fixture)
            : base(fixture)
        {
        }

        protected override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
            => facade.UseTransaction(transaction.GetDbTransaction());

        public abstract class ProxyGraphUpdatesJetFixtureBase : ProxyGraphUpdatesFixtureBase
        {
            public TestSqlLoggerFactory TestSqlLoggerFactory
                => (TestSqlLoggerFactory)ListLoggerFactory;

            protected override ITestStoreFactory TestStoreFactory
                => JetTestStoreFactory.Instance;
        }
    }

    public class LazyLoading(LazyLoading.ProxyGraphUpdatesWithLazyLoadingJetFixture fixture)
        : ProxyGraphUpdatesJetTestBase<LazyLoading.ProxyGraphUpdatesWithLazyLoadingJetFixture>(fixture)
    {
        protected override bool DoesLazyLoading
            => true;

        protected override bool DoesChangeTracking
            => false;

        public class ProxyGraphUpdatesWithLazyLoadingJetFixture : ProxyGraphUpdatesJetFixtureBase
        {
            protected override string StoreName
                => "ProxyGraphLazyLoadingUpdatesTest";

            public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
                => base.AddOptions(builder.UseLazyLoadingProxies());

            protected override IServiceCollection AddServices(IServiceCollection serviceCollection)
                => base.AddServices(serviceCollection.AddEntityFrameworkProxies());

            protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
            {
                modelBuilder.UseJetIdentityColumns();

                base.OnModelCreating(modelBuilder, context);

                //custom long to int
                modelBuilder.Entity<SharedFkRoot>(
                    b =>
                    {
                        b.Property(e => e.Id).HasColumnType("counter");
                    });
                modelBuilder.Entity<SharedFkParent>(
                    b =>
                    {
                        b.Property(e => e.Id).HasColumnType("counter");
                    });
                modelBuilder.Entity<SharedFkDependant>(
                    b =>
                    {
                        b.Property(e => e.Id).HasColumnType("counter");
                    });
            }
        }
    }

    public class ChangeTracking(ChangeTracking.ProxyGraphUpdatesWithChangeTrackingJetFixture fixture)
        : ProxyGraphUpdatesJetTestBase<ChangeTracking.ProxyGraphUpdatesWithChangeTrackingJetFixture>(fixture)
    {
        // Needs lazy loading
        public override Task Save_two_entity_cycle_with_lazy_loading()
            => Task.CompletedTask;

        protected override bool DoesLazyLoading
            => false;

        protected override bool DoesChangeTracking
            => true;

        public class ProxyGraphUpdatesWithChangeTrackingJetFixture : ProxyGraphUpdatesJetFixtureBase
        {
            protected override string StoreName
                => "ProxyGraphChangeTrackingUpdatesTest";

            public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
                => base.AddOptions(builder.UseChangeTrackingProxies());

            protected override IServiceCollection AddServices(IServiceCollection serviceCollection)
                => base.AddServices(serviceCollection.AddEntityFrameworkProxies());

            protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
            {
                modelBuilder.UseJetIdentityColumns();

                base.OnModelCreating(modelBuilder, context);

                //custom long to int
                modelBuilder.Entity<SharedFkRoot>(
                    b =>
                    {
                        b.Property(e => e.Id).HasColumnType("counter");
                    });
                modelBuilder.Entity<SharedFkParent>(
                    b =>
                    {
                        b.Property(e => e.Id).HasColumnType("counter");
                    });
                modelBuilder.Entity<SharedFkDependant>(
                    b =>
                    {
                        b.Property(e => e.Id).HasColumnType("counter");
                    });
            }
        }
    }

    public class ChangeTrackingAndLazyLoading(
        ChangeTrackingAndLazyLoading.ProxyGraphUpdatesWithChangeTrackingAndLazyLoadingJetFixture fixture)
        : ProxyGraphUpdatesJetTestBase<
            ChangeTrackingAndLazyLoading.ProxyGraphUpdatesWithChangeTrackingAndLazyLoadingJetFixture>(fixture)
    {
        protected override bool DoesLazyLoading
            => true;

        protected override bool DoesChangeTracking
            => true;

        public class ProxyGraphUpdatesWithChangeTrackingAndLazyLoadingJetFixture : ProxyGraphUpdatesJetFixtureBase
        {
            protected override string StoreName
                => "ProxyGraphChangeTrackingAndLazyLoadingUpdatesTest";

            public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
                => base.AddOptions(builder.UseLazyLoadingProxies().UseChangeTrackingProxies());

            protected override IServiceCollection AddServices(IServiceCollection serviceCollection)
                => base.AddServices(serviceCollection.AddEntityFrameworkProxies());

            protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
            {
                modelBuilder.UseJetIdentityColumns();

                base.OnModelCreating(modelBuilder, context);

                //custom long to int
                modelBuilder.Entity<SharedFkRoot>(
                    b =>
                    {
                        b.Property(e => e.Id).HasColumnType("counter");
                    });
                modelBuilder.Entity<SharedFkParent>(
                    b =>
                    {
                        b.Property(e => e.Id).HasColumnType("counter");
                    });
                modelBuilder.Entity<SharedFkDependant>(
                    b =>
                    {
                        b.Property(e => e.Id).HasColumnType("counter");
                    });
            }
        }
    }
}
