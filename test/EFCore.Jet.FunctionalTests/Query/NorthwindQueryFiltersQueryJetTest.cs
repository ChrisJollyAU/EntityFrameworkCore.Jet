﻿// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using EntityFrameworkCore.Jet.FunctionalTests.TestUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable InconsistentNaming
namespace EntityFrameworkCore.Jet.FunctionalTests.Query
{
    public class NorthwindQueryFiltersQueryJetTest : NorthwindQueryFiltersQueryTestBase<NorthwindQueryJetFixture<NorthwindQueryFiltersCustomizer>>
    {
        public NorthwindQueryFiltersQueryJetTest(NorthwindQueryJetFixture<NorthwindQueryFiltersCustomizer> fixture, ITestOutputHelper testOutputHelper)
            : base(fixture)
        {
            fixture.TestSqlLoggerFactory.Clear();
            fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
        }

        [ConditionalFact]
        public virtual void Check_all_tests_overridden()
            => TestHelpers.AssertAllMethodsOverridden(GetType());

        public override async Task Count_query(bool async)
        {
            await base.Count_query(async);

            AssertSql(
                """
@ef_filter__TenantPrefix_startswith='B%' (Size = 40)

SELECT COUNT(*)
FROM `Customers` AS `c`
WHERE `c`.`CompanyName` LIKE @ef_filter__TenantPrefix_startswith
""");
        }

        public override async Task Materialized_query(bool async)
        {
            await base.Materialized_query(async);

            AssertSql(
                """
@ef_filter__TenantPrefix_startswith='B%' (Size = 40)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CompanyName` LIKE @ef_filter__TenantPrefix_startswith
""");
        }

        public override async Task Find(bool async)
        {
            await base.Find(async);

            AssertSql(
                """
@ef_filter__TenantPrefix_startswith='B%' (Size = 40)
@p='ALFKI' (Size = 5)

SELECT TOP 1 `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE (`c`.`CompanyName` LIKE @ef_filter__TenantPrefix_startswith) AND `c`.`CustomerID` = @p
""");
        }

        public override async Task Materialized_query_parameter(bool async)
        {
            await base.Materialized_query_parameter(async);

            AssertSql(
                """
@ef_filter__TenantPrefix_startswith='F%' (Size = 40)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CompanyName` LIKE @ef_filter__TenantPrefix_startswith
""");
        }

        public override async Task Materialized_query_parameter_new_context(bool async)
        {
            await base.Materialized_query_parameter_new_context(async);

            AssertSql(
                """
@ef_filter__TenantPrefix_startswith='B%' (Size = 40)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CompanyName` LIKE @ef_filter__TenantPrefix_startswith
""",
                //
                """
@ef_filter__TenantPrefix_startswith='T%' (Size = 40)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE `c`.`CompanyName` LIKE @ef_filter__TenantPrefix_startswith
""");
        }

        public override async Task Projection_query_parameter(bool async)
        {
            await base.Projection_query_parameter(async);

            AssertSql(
                """
@ef_filter__TenantPrefix_startswith='F%' (Size = 40)

SELECT `c`.`CustomerID`
FROM `Customers` AS `c`
WHERE `c`.`CompanyName` LIKE @ef_filter__TenantPrefix_startswith
""");
        }

        public override async Task Projection_query(bool async)
        {
            await base.Projection_query(async);

            AssertSql(
                """
@ef_filter__TenantPrefix_startswith='B%' (Size = 40)

SELECT `c`.`CustomerID`
FROM `Customers` AS `c`
WHERE `c`.`CompanyName` LIKE @ef_filter__TenantPrefix_startswith
""");
        }

        public override async Task Include_query(bool async)
        {
            await base.Include_query(async);

            AssertSql(
                """
@ef_filter__TenantPrefix_startswith='B%' (Size = 40)
@ef_filter__TenantPrefix_startswith='B%' (Size = 40)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `s`.`OrderID`, `s`.`CustomerID`, `s`.`EmployeeID`, `s`.`OrderDate`, `s`.`CustomerID0`
FROM `Customers` AS `c`
LEFT JOIN (
    SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `c1`.`CustomerID` AS `CustomerID0`
    FROM `Orders` AS `o`
    LEFT JOIN (
        SELECT `c0`.`CustomerID`, `c0`.`CompanyName`
        FROM `Customers` AS `c0`
        WHERE `c0`.`CompanyName` LIKE @ef_filter__TenantPrefix_startswith
    ) AS `c1` ON `o`.`CustomerID` = `c1`.`CustomerID`
    WHERE `c1`.`CustomerID` IS NOT NULL AND `c1`.`CompanyName` IS NOT NULL
) AS `s` ON `c`.`CustomerID` = `s`.`CustomerID`
WHERE `c`.`CompanyName` LIKE @ef_filter__TenantPrefix_startswith
ORDER BY `c`.`CustomerID`, `s`.`OrderID`
""");
        }

        public override async Task Include_query_opt_out(bool async)
        {
            await base.Include_query_opt_out(async);

            AssertSql(
                $"""
                    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`, `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
                    FROM `Customers` AS `c`
                    LEFT JOIN `Orders` AS `o` ON `c`.`CustomerID` = `o`.`CustomerID`
                    ORDER BY `c`.`CustomerID`
                    """);
        }

        public override async Task Included_many_to_one_query(bool async)
        {
            await base.Included_many_to_one_query(async);

            AssertSql(
                """
@ef_filter__TenantPrefix_startswith='B%' (Size = 40)

SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
FROM `Orders` AS `o`
LEFT JOIN (
    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    WHERE `c`.`CompanyName` LIKE @ef_filter__TenantPrefix_startswith
) AS `c0` ON `o`.`CustomerID` = `c0`.`CustomerID`
WHERE `c0`.`CustomerID` IS NOT NULL AND `c0`.`CompanyName` IS NOT NULL
""");
        }

        public override async Task Project_reference_that_itself_has_query_filter_with_another_reference(bool async)
        {
            await base.Project_reference_that_itself_has_query_filter_with_another_reference(async);

            AssertSql(
                """
@ef_filter__TenantPrefix_startswith='B%' (Size = 40)
@ef_filter___quantity='50'

SELECT `s`.`OrderID`, `s`.`CustomerID`, `s`.`EmployeeID`, `s`.`OrderDate`
FROM `Order Details` AS `o`
INNER JOIN (
    SELECT `o0`.`OrderID`, `o0`.`CustomerID`, `o0`.`EmployeeID`, `o0`.`OrderDate`
    FROM `Orders` AS `o0`
    LEFT JOIN (
        SELECT `c`.`CustomerID`, `c`.`CompanyName`
        FROM `Customers` AS `c`
        WHERE `c`.`CompanyName` LIKE @ef_filter__TenantPrefix_startswith
    ) AS `c0` ON `o0`.`CustomerID` = `c0`.`CustomerID`
    WHERE `c0`.`CustomerID` IS NOT NULL AND `c0`.`CompanyName` IS NOT NULL
) AS `s` ON `o`.`OrderID` = `s`.`OrderID`
WHERE `o`.`Quantity` > @ef_filter___quantity
""");
        }

        public override async Task Navs_query(bool async)
        {
            await base.Navs_query(async);

            AssertSql(
                $"""
@ef_filter__TenantPrefix_startswith='B%' (Size = 40)
@ef_filter__TenantPrefix_startswith='B%' (Size = 40)
@ef_filter___quantity='50'
@ef_filter__TenantPrefix_startswith='B%' (Size = 40)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM (`Customers` AS `c`
INNER JOIN (
    SELECT `o`.`OrderID`, `o`.`CustomerID`
    FROM `Orders` AS `o`
    LEFT JOIN (
        SELECT `c0`.`CustomerID`, `c0`.`CompanyName`
        FROM `Customers` AS `c0`
        WHERE `c0`.`CompanyName` LIKE {AssertSqlHelper.Parameter("@ef_filter__TenantPrefix_startswith")}
    ) AS `c1` ON `o`.`CustomerID` = `c1`.`CustomerID`
    WHERE `c1`.`CustomerID` IS NOT NULL AND `c1`.`CompanyName` IS NOT NULL
) AS `s` ON `c`.`CustomerID` = `s`.`CustomerID`)
LEFT JOIN (
    SELECT `o0`.`OrderID`, `o0`.`Discount`
    FROM `Order Details` AS `o0`
    INNER JOIN (
        SELECT `o1`.`OrderID`
        FROM `Orders` AS `o1`
        LEFT JOIN (
            SELECT `c2`.`CustomerID`, `c2`.`CompanyName`
            FROM `Customers` AS `c2`
            WHERE `c2`.`CompanyName` LIKE {AssertSqlHelper.Parameter("@ef_filter__TenantPrefix_startswith")}
        ) AS `c3` ON `o1`.`CustomerID` = `c3`.`CustomerID`
        WHERE `c3`.`CustomerID` IS NOT NULL AND `c3`.`CompanyName` IS NOT NULL
    ) AS `s0` ON `o0`.`OrderID` = `s0`.`OrderID`
    WHERE `o0`.`Quantity` > {AssertSqlHelper.Parameter("@ef_filter___quantity")}
) AS `s1` ON `s`.`OrderID` = `s1`.`OrderID`
WHERE ((`c`.`CompanyName` LIKE {AssertSqlHelper.Parameter("@ef_filter__TenantPrefix_startswith")}) AND `s1`.`Discount` < 10) AND (`s`.`OrderID` IS NOT NULL AND `s1`.`OrderID` IS NOT NULL)
""");
        }

        [ConditionalFact]
        public void FromSql_is_composed()
        {
            using (var context = Fixture.CreateContext())
            {
                var results = context.Customers.FromSqlRaw("select * from Customers").ToList();

                Assert.Equal(7, results.Count);
            }

            AssertSql(
                """
@ef_filter__TenantPrefix_startswith='B%' (Size = 40)

SELECT `m`.`CustomerID`, `m`.`Address`, `m`.`City`, `m`.`CompanyName`, `m`.`ContactName`, `m`.`ContactTitle`, `m`.`Country`, `m`.`Fax`, `m`.`Phone`, `m`.`PostalCode`, `m`.`Region`
FROM (
    select * from Customers
) AS `m`
WHERE `m`.`CompanyName` LIKE @ef_filter__TenantPrefix_startswith
""");
        }

        [ConditionalFact]
        public void FromSql_is_composed_when_filter_has_navigation()
        {
            using (var context = Fixture.CreateContext())
            {
                var results = context.Orders.FromSqlRaw("select * from Orders").ToList();

                Assert.Equal(80, results.Count);
            }

            AssertSql(
                """
@ef_filter__TenantPrefix_startswith='B%' (Size = 40)

SELECT `m`.`OrderID`, `m`.`CustomerID`, `m`.`EmployeeID`, `m`.`OrderDate`
FROM (
    select * from Orders
) AS `m`
LEFT JOIN (
    SELECT `c`.`CustomerID`, `c`.`CompanyName`
    FROM `Customers` AS `c`
    WHERE `c`.`CompanyName` LIKE @ef_filter__TenantPrefix_startswith
) AS `c0` ON `m`.`CustomerID` = `c0`.`CustomerID`
WHERE `c0`.`CustomerID` IS NOT NULL AND `c0`.`CompanyName` IS NOT NULL
""");
        }

        public override void Compiled_query()
        {
            base.Compiled_query();

            AssertSql(
                """
@ef_filter__TenantPrefix_startswith='B%' (Size = 40)
@customerID='BERGS' (Size = 5)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE (`c`.`CompanyName` LIKE @ef_filter__TenantPrefix_startswith) AND `c`.`CustomerID` = @customerID
""",
                //
                """
@ef_filter__TenantPrefix_startswith='B%' (Size = 40)
@customerID='BLAUS' (Size = 5)

SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
FROM `Customers` AS `c`
WHERE (`c`.`CompanyName` LIKE @ef_filter__TenantPrefix_startswith) AND `c`.`CustomerID` = @customerID
""");
        }

        public override async Task Entity_Equality(bool async)
        {
            await base.Entity_Equality(async);

            AssertSql(
                """
@ef_filter__TenantPrefix_startswith='B%' (Size = 40)

SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`
FROM `Orders` AS `o`
LEFT JOIN (
    SELECT `c`.`CustomerID`, `c`.`CompanyName`
    FROM `Customers` AS `c`
    WHERE `c`.`CompanyName` LIKE @ef_filter__TenantPrefix_startswith
) AS `c0` ON `o`.`CustomerID` = `c0`.`CustomerID`
WHERE `c0`.`CustomerID` IS NOT NULL AND `c0`.`CompanyName` IS NOT NULL
""");
        }

        public override async Task Client_eval(bool async)
        {
            await base.Client_eval(async);

            AssertSql();
        }

        public override async Task Included_many_to_one_query2(bool async)
        {
            await base.Included_many_to_one_query2(async);

            AssertSql(
                """
@ef_filter__TenantPrefix_startswith='B%' (Size = 40)

SELECT `o`.`OrderID`, `o`.`CustomerID`, `o`.`EmployeeID`, `o`.`OrderDate`, `c0`.`CustomerID`, `c0`.`Address`, `c0`.`City`, `c0`.`CompanyName`, `c0`.`ContactName`, `c0`.`ContactTitle`, `c0`.`Country`, `c0`.`Fax`, `c0`.`Phone`, `c0`.`PostalCode`, `c0`.`Region`
FROM `Orders` AS `o`
LEFT JOIN (
    SELECT `c`.`CustomerID`, `c`.`Address`, `c`.`City`, `c`.`CompanyName`, `c`.`ContactName`, `c`.`ContactTitle`, `c`.`Country`, `c`.`Fax`, `c`.`Phone`, `c`.`PostalCode`, `c`.`Region`
    FROM `Customers` AS `c`
    WHERE `c`.`CompanyName` LIKE @ef_filter__TenantPrefix_startswith
) AS `c0` ON `o`.`CustomerID` = `c0`.`CustomerID`
WHERE `c0`.`CustomerID` IS NOT NULL AND `c0`.`CompanyName` IS NOT NULL
""");
        }

        public override async Task Included_one_to_many_query_with_client_eval(bool async)
        {
            await base.Included_one_to_many_query_with_client_eval(async);

            AssertSql();
        }

        private void AssertSql(params string[] expected)
            => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);
    }
}
