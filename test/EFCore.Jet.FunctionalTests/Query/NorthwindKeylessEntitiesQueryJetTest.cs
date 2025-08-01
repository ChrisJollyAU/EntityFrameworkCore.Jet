// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Data.Odbc;
using System.Data.OleDb;
using System.Threading.Tasks;
using EntityFrameworkCore.Jet.FunctionalTests.TestUtilities;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;
using Xunit.Abstractions;

namespace EntityFrameworkCore.Jet.FunctionalTests.Query
{
    public class NorthwindKeylessEntitiesQueryJetTest : NorthwindKeylessEntitiesQueryRelationalTestBase<
        NorthwindQueryJetFixture<NoopModelCustomizer>>
    {
        public NorthwindKeylessEntitiesQueryJetTest(
            NorthwindQueryJetFixture<NoopModelCustomizer> fixture,
            ITestOutputHelper testOutputHelper)
            : base(fixture)
        {
            ClearLog();
            Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
        }

        [ConditionalFact]
        public virtual void Check_all_tests_overridden()
            => TestHelpers.AssertAllMethodsOverridden(GetType());

        [ConditionalTheory]
        public override async Task KeylessEntity_simple(bool isAsync)
        {
            await base.KeylessEntity_simple(isAsync);

            AssertSql(
"""
SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region] FROM [Customers] AS [c]
""");
        }

        [ConditionalTheory]
        public override async Task KeylessEntity_where_simple(bool isAsync)
        {
            await base.KeylessEntity_where_simple(isAsync);

            AssertSql(
"""
SELECT `m`.`Address`, `m`.`City`, `m`.`CompanyName`, `m`.`ContactName`, `m`.`ContactTitle`
FROM (
    SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region] FROM [Customers] AS [c]
) AS `m`
WHERE `m`.`City` = 'London'
""");
        }

        public override async Task KeylessEntity_by_database_view(bool isAsync)
        {
            await base.KeylessEntity_by_database_view(isAsync);

            AssertSql(
                """
SELECT `a`.`CategoryName`, `a`.`ProductID`, `a`.`ProductName`
FROM `Alphabetical list of products` AS `a`
""");
        }

        public override async Task KeylessEntity_with_nav_defining_query(bool isAsync)
        {
            // FromSql mapping. Issue #21627.
            var testStore = (JetTestStore)Fixture.TestStore;
            if (testStore.IsOleDb())
            {
                await Assert.ThrowsAsync<OleDbException>(() => base.KeylessEntity_with_nav_defining_query(isAsync));
            }
            else
            {
                await Assert.ThrowsAsync<OdbcException>(() => base.KeylessEntity_with_nav_defining_query(isAsync));
            }
        }

        public override async Task KeylessEntity_with_mixed_tracking(bool isAsync)
        {
            await base.KeylessEntity_with_mixed_tracking(isAsync);

            AssertSql(
                """
    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `m`.`CustomerID`
    FROM `Customers` AS `c`
    INNER JOIN (
        select * from `Orders`
    ) AS `m` ON `c`.`CustomerID` = `m`.`CustomerID`
    """);
        }

        public override async Task KeylessEntity_with_defining_query(bool isAsync)
        {
            await base.KeylessEntity_with_defining_query(isAsync);

            AssertSql(
                """
SELECT `m`.`CustomerID`
FROM (
    select * from `Orders`
) AS `m`
WHERE `m`.`CustomerID` = 'ALFKI'
""");
        }

        public override async Task KeylessEntity_select_where_navigation(bool isAsync)
        {
            await base.KeylessEntity_select_where_navigation(isAsync);

            AssertSql(
                """
    SELECT `m`.`CustomerID`
    FROM (
        select * from `Orders`
    ) AS `m`
    LEFT JOIN `Customers` AS `c` ON `m`.`CustomerID` = `c`.`CustomerID`
    WHERE `c`.`City` = 'Seattle'
    """);
        }

        public override async Task KeylessEntity_select_where_navigation_multi_level(bool isAsync)
        {
            await base.KeylessEntity_select_where_navigation_multi_level(isAsync);

            AssertSql(
"""
SELECT `m`.`CustomerID`
FROM (
    select * from `Orders`
) AS `m`
LEFT JOIN `Customers` AS `c` ON `m`.`CustomerID` = `c`.`CustomerID`
WHERE EXISTS (
    SELECT 1
    FROM `Orders` AS `o`
    WHERE `c`.`CustomerID` IS NOT NULL AND `c`.`CustomerID` = `o`.`CustomerID`)
""");
        }

        public override async Task Auto_initialized_view_set(bool isAsync)
        {
            await base.Auto_initialized_view_set(isAsync);

            AssertSql(
"""
SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region] FROM [Customers] AS [c]
""");
        }

        public override async Task KeylessEntity_groupby(bool async)
        {
            await base.KeylessEntity_groupby(async);

            AssertSql(
"""
SELECT `m`.`City` AS `Key`, COUNT(*) AS `Count`, IIF(SUM(IIF(LEN(`m`.`Address`) IS NULL, NULL, CLNG(LEN(`m`.`Address`)))) IS NULL, 0, SUM(IIF(LEN(`m`.`Address`) IS NULL, NULL, CLNG(LEN(`m`.`Address`))))) AS `Sum`
FROM (
    SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region] FROM [Customers] AS [c]
) AS `m`
GROUP BY `m`.`City`
""");
        }

        public override async Task Entity_mapped_to_view_on_right_side_of_join(bool async)
        {
            await base.Entity_mapped_to_view_on_right_side_of_join(async);

            AssertSql(
"""
SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `a`.`CategoryName`, `a`.`ProductID`, `a`.`ProductName`
FROM `Orders` AS `o`
LEFT JOIN `Alphabetical list of products` AS `a` ON `o`.`CustomerID` = `a`.`CategoryName`
""");
        }

        public override async Task Collection_correlated_with_keyless_entity_in_predicate_works(bool async)
        {
            await base.Collection_correlated_with_keyless_entity_in_predicate_works(async);

            AssertSql(
                """
SELECT TOP @p `m`.`City`, `m`.`ContactName`
FROM (
    SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region] FROM [Customers] AS [c]
) AS `m`
WHERE EXISTS (
    SELECT 1
    FROM `Customers` AS `c`
    WHERE `c`.`City` = `m`.`City` OR (`c`.`City` IS NULL AND `m`.`City` IS NULL))
ORDER BY `m`.`ContactName`
""");
        }

        public override async Task Projecting_collection_correlated_with_keyless_entity_throws(bool async)
        {
            await base.Projecting_collection_correlated_with_keyless_entity_throws(async);

            AssertSql();
        }

        public override async Task Collection_of_entities_projecting_correlated_collection_of_keyless_entities(bool async)
        {
            await base.Collection_of_entities_projecting_correlated_collection_of_keyless_entities(async);

            AssertSql();
        }

        public override async Task KeylessEntity_with_included_navs_multi_level(bool async)
        {
            await base.KeylessEntity_with_included_navs_multi_level(async);

            AssertSql();
        }

        public override async Task KeylessEntity_with_defining_query_and_correlated_collection(bool async)
        {
            await base.KeylessEntity_with_defining_query_and_correlated_collection(async);

            AssertSql();
        }

        public override async Task KeylessEntity_with_included_nav(bool async)
        {
            await base.KeylessEntity_with_included_nav(async);

            AssertSql(
"""
SELECT `m`.`CustomerID`, `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM (
    select * from `Orders`
) AS `m`
LEFT JOIN `Customers` AS `c` ON `m`.`CustomerID` = `c`.`CustomerID`
WHERE `m`.`CustomerID` = 'ALFKI'
""");
        }

        public override async Task Count_over_keyless_entity(bool async)
        {
            await base.Count_over_keyless_entity(async);

            AssertSql(
"""
SELECT COUNT(*)
FROM (
    SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region] FROM [Customers] AS [c]
) AS `m`
""");
        }

        public override async Task Count_over_keyless_entity_with_pushdown(bool async)
        {
            await base.Count_over_keyless_entity_with_pushdown(async);

            AssertSql(
                """
SELECT COUNT(*)
FROM (
    SELECT TOP @p 1
    FROM (
        SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region] FROM [Customers] AS [c]
    ) AS `m`
    ORDER BY `m`.`ContactTitle`
) AS `m0`
""");
        }

        public override async Task Count_over_keyless_entity_with_pushdown_empty_projection(bool async)
        {
            await base.Count_over_keyless_entity_with_pushdown_empty_projection(async);

            AssertSql(
                """
SELECT COUNT(*)
FROM (
    SELECT TOP @p 1
    FROM (
        SELECT [c].[CustomerID], [c].[Address], [c].[City], [c].[CompanyName], [c].[ContactName], [c].[ContactTitle], [c].[Country], [c].[Fax], [c].[Phone], [c].[PostalCode], [c].[Region] FROM [Customers] AS [c]
    ) AS `m`
) AS `m0`
""");
        }

        private void AssertSql(params string[] expected)
            => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);

        protected override void ClearLog()
            => Fixture.TestSqlLoggerFactory.Clear();
    }
}
