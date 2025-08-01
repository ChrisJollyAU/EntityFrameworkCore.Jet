﻿// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using EntityFrameworkCore.Jet.FunctionalTests.TestUtilities;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit.Abstractions;

namespace EntityFrameworkCore.Jet.FunctionalTests.Query
{
    public class FunkyDataQueryJetTest : FunkyDataQueryTestBase<FunkyDataQueryJetTest.FunkyDataQueryJetFixture>
    {
        public FunkyDataQueryJetTest(FunkyDataQueryJetFixture fixture, ITestOutputHelper testOutputHelper)
            : base(fixture)
        {
            Fixture.TestSqlLoggerFactory.Clear();
            Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
        }

        public override async Task String_contains_on_argument_with_wildcard_constant(bool isAsync)
        {
            await base.String_contains_on_argument_with_wildcard_constant(isAsync);

            AssertSql(
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` LIKE '%[%]B%'
""",
                //
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` LIKE '%a[_]%'
""",
                //
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE 0 = 1
""",
                //
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` IS NOT NULL
""",
                //
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` LIKE '%[_]Ba[_]%'
""",
                //
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` NOT LIKE '%[%]B[%]a[%]r%' OR `f`.`FirstName` IS NULL
""",
                //
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` IS NULL
""",
                //
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
""");
        }

        public override async Task String_contains_on_argument_with_wildcard_parameter(bool isAsync)
        {
            await base.String_contains_on_argument_with_wildcard_parameter(isAsync);

            AssertSql(
                """
@prm1_contains='%[%]B%' (Size = 255)

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` LIKE @prm1_contains
""",
                //
                """
@prm2_contains='%a[_]%' (Size = 255)

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` LIKE @prm2_contains
""",
                //
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE 0 = 1
""",
                //
                """
@prm4_contains='%' (Size = 255)

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` LIKE @prm4_contains
""",
                //
                """
@prm5_contains='%[_]Ba[_]%' (Size = 255)

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` LIKE @prm5_contains
""",
                //
                """
@prm6_contains='%[%]B[%]a[%]r%' (Size = 255)

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` NOT LIKE @prm6_contains OR `f`.`FirstName` IS NULL
""",
                //
                """
@prm7_contains='%' (Size = 255)

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` NOT LIKE @prm7_contains OR `f`.`FirstName` IS NULL
""",
                //
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
""");
        }

        public override async Task String_contains_on_argument_with_wildcard_column(bool isAsync)
        {
            await base.String_contains_on_argument_with_wildcard_column(isAsync);

            AssertSql(
                """
SELECT `f`.`FirstName` AS `fn`, `f0`.`LastName` AS `ln`
FROM `FunkyCustomers` AS `f`,
`FunkyCustomers` AS `f0`
WHERE `f`.`FirstName` IS NOT NULL AND `f0`.`LastName` IS NOT NULL AND (INSTR(1, `f`.`FirstName`, `f0`.`LastName`, 1) > 0 OR (`f0`.`LastName` LIKE ''))
""");
        }

        public override async Task String_contains_on_argument_with_wildcard_column_negated(bool isAsync)
        {
            await base.String_contains_on_argument_with_wildcard_column_negated(isAsync);

            AssertSql(
                """
SELECT `f`.`FirstName` AS `fn`, `f0`.`LastName` AS `ln`
FROM `FunkyCustomers` AS `f`,
`FunkyCustomers` AS `f0`
WHERE `f`.`FirstName` IS NULL OR `f0`.`LastName` IS NULL OR (INSTR(1, `f`.`FirstName`, `f0`.`LastName`, 1) <= 0 AND `f0`.`LastName` NOT LIKE '')
""");
        }

        public override async Task String_starts_with_on_argument_with_wildcard_constant(bool isAsync)
        {
            await base.String_starts_with_on_argument_with_wildcard_constant(isAsync);

            AssertSql(
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` LIKE '[%]B%'
""",
                //
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` LIKE '[_]B%'
""",
                //
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE 0 = 1
""",
                //
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` IS NOT NULL
""",
                //
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` LIKE '[_]Ba[_]%'
""",
                //
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` NOT LIKE '[%]B[%]a[%]r%' OR `f`.`FirstName` IS NULL
""",
                //
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` IS NULL
""",
                //
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
""");
        }

        public override async Task String_starts_with_on_argument_with_wildcard_parameter(bool isAsync)
        {
            await base.String_starts_with_on_argument_with_wildcard_parameter(isAsync);

            AssertSql(
                """
@prm1_startswith='[%]B%' (Size = 255)

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` LIKE @prm1_startswith
""",
                //
                """
@prm2_startswith='[_]B%' (Size = 255)

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` LIKE @prm2_startswith
""",
                //
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE 0 = 1
""",
                //
                """
@prm4_startswith='%' (Size = 255)

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` LIKE @prm4_startswith
""",
                //
                """
@prm5_startswith='[_]Ba[_]%' (Size = 255)

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` LIKE @prm5_startswith
""",
                //
                """
@prm6_startswith='[%]B[%]a[%]r%' (Size = 255)

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` NOT LIKE @prm6_startswith OR `f`.`FirstName` IS NULL
""",
                //
                """
@prm7_startswith='%' (Size = 255)

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` NOT LIKE @prm7_startswith OR `f`.`FirstName` IS NULL
""",
                //
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
""");
        }

        public override async Task String_starts_with_on_argument_with_bracket(bool isAsync)
        {
            await base.String_starts_with_on_argument_with_bracket(isAsync);

            AssertSql(
                """
SELECT `f`.`Id`, `f`.`FirstName`, `f`.`LastName`, `f`.`NullableBool`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` LIKE '[[]%'
""",
                //
                """
SELECT `f`.`Id`, `f`.`FirstName`, `f`.`LastName`, `f`.`NullableBool`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` LIKE 'B[[]%'
""",
                //
                """
SELECT `f`.`Id`, `f`.`FirstName`, `f`.`LastName`, `f`.`NullableBool`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` LIKE 'B[[][[]a[^]%'
""",
                //
                """
@prm1_startswith='[[]%' (Size = 255)

SELECT `f`.`Id`, `f`.`FirstName`, `f`.`LastName`, `f`.`NullableBool`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` LIKE @prm1_startswith
""",
                //
                """
@prm2_startswith='B[[]%' (Size = 255)

SELECT `f`.`Id`, `f`.`FirstName`, `f`.`LastName`, `f`.`NullableBool`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` LIKE @prm2_startswith
""",
                //
                """
@prm3_startswith='B[[][[]a[^]%' (Size = 255)

SELECT `f`.`Id`, `f`.`FirstName`, `f`.`LastName`, `f`.`NullableBool`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` LIKE @prm3_startswith
""",
                //
                """
SELECT `f`.`Id`, `f`.`FirstName`, `f`.`LastName`, `f`.`NullableBool`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` IS NOT NULL AND `f`.`LastName` IS NOT NULL AND LEFT(`f`.`FirstName`, IIF(LEN(`f`.`LastName`) IS NULL, 0, LEN(`f`.`LastName`))) = `f`.`LastName`
""");
        }

        public override async Task String_starts_with_on_argument_with_wildcard_column(bool isAsync)
        {
            await base.String_starts_with_on_argument_with_wildcard_column(isAsync);

            AssertSql(
                """
SELECT `f`.`FirstName` AS `fn`, `f0`.`LastName` AS `ln`
FROM `FunkyCustomers` AS `f`,
`FunkyCustomers` AS `f0`
WHERE `f`.`FirstName` IS NOT NULL AND `f0`.`LastName` IS NOT NULL AND LEFT(`f`.`FirstName`, IIF(LEN(`f0`.`LastName`) IS NULL, 0, LEN(`f0`.`LastName`))) = `f0`.`LastName`
""");
        }

        public override async Task String_starts_with_on_argument_with_wildcard_column_negated(bool isAsync)
        {
            await base.String_starts_with_on_argument_with_wildcard_column_negated(isAsync);

            AssertSql(
                """
SELECT `f`.`FirstName` AS `fn`, `f0`.`LastName` AS `ln`
FROM `FunkyCustomers` AS `f`,
`FunkyCustomers` AS `f0`
WHERE `f`.`FirstName` IS NULL OR `f0`.`LastName` IS NULL OR LEFT(`f`.`FirstName`, IIF(LEN(`f0`.`LastName`) IS NULL, 0, LEN(`f0`.`LastName`))) <> `f0`.`LastName`
""");
        }

        public override async Task String_ends_with_on_argument_with_wildcard_constant(bool isAsync)
        {
            await base.String_ends_with_on_argument_with_wildcard_constant(isAsync);

            AssertSql(
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` LIKE '%[%]r'
""",
                //
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` LIKE '%r[_]'
""",
                //
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE 0 = 1
""",
                //
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` IS NOT NULL
""",
                //
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` LIKE '%[_]r[_]'
""",
                //
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` NOT LIKE '%a[%]r[%]' OR `f`.`FirstName` IS NULL
""",
                //
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` IS NULL
""",
                //
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
""");
        }

        public override async Task String_ends_with_on_argument_with_wildcard_parameter(bool isAsync)
        {
            await base.String_ends_with_on_argument_with_wildcard_parameter(isAsync);

            AssertSql(
                """
@prm1_endswith='%[%]r' (Size = 255)

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` LIKE @prm1_endswith
""",
                //
                """
@prm2_endswith='%r[_]' (Size = 255)

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` LIKE @prm2_endswith
""",
                //
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE 0 = 1
""",
                //
                """
@prm4_endswith='%' (Size = 255)

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` LIKE @prm4_endswith
""",
                //
                """
@prm5_endswith='%[_]r[_]' (Size = 255)

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` LIKE @prm5_endswith
""",
                //
                """
@prm6_endswith='%a[%]r[%]' (Size = 255)

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` NOT LIKE @prm6_endswith OR `f`.`FirstName` IS NULL
""",
                //
                """
@prm7_endswith='%' (Size = 255)

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` NOT LIKE @prm7_endswith OR `f`.`FirstName` IS NULL
""",
                //
                """
SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
""");
        }

        public override async Task String_ends_with_on_argument_with_wildcard_column(bool isAsync)
        {
            await base.String_ends_with_on_argument_with_wildcard_column(isAsync);

            AssertSql(
                """
SELECT `f`.`FirstName` AS `fn`, `f0`.`LastName` AS `ln`
FROM `FunkyCustomers` AS `f`,
`FunkyCustomers` AS `f0`
WHERE `f`.`FirstName` IS NOT NULL AND `f0`.`LastName` IS NOT NULL AND RIGHT(`f`.`FirstName`, IIF(LEN(`f0`.`LastName`) IS NULL, 0, LEN(`f0`.`LastName`))) = `f0`.`LastName`
""");
        }

        public override async Task String_ends_with_on_argument_with_wildcard_column_negated(bool isAsync)
        {
            await base.String_ends_with_on_argument_with_wildcard_column_negated(isAsync);

            AssertSql(
                """
SELECT `f`.`FirstName` AS `fn`, `f0`.`LastName` AS `ln`
FROM `FunkyCustomers` AS `f`,
`FunkyCustomers` AS `f0`
WHERE `f`.`FirstName` IS NULL OR `f0`.`LastName` IS NULL OR RIGHT(`f`.`FirstName`, IIF(LEN(`f0`.`LastName`) IS NULL, 0, LEN(`f0`.`LastName`))) <> `f0`.`LastName`
""");
        }

        public override async Task String_ends_with_inside_conditional(bool isAsync)
        {
            await base.String_ends_with_inside_conditional(isAsync);

            AssertSql(
                """
SELECT `f`.`FirstName` AS `fn`, `f0`.`LastName` AS `ln`
FROM `FunkyCustomers` AS `f`,
`FunkyCustomers` AS `f0`
WHERE IIF(`f`.`FirstName` IS NOT NULL AND `f0`.`LastName` IS NOT NULL AND RIGHT(`f`.`FirstName`, IIF(LEN(`f0`.`LastName`) IS NULL, 0, LEN(`f0`.`LastName`))) = `f0`.`LastName`, TRUE, FALSE) = TRUE
""");
        }

        public override async Task String_ends_with_inside_conditional_negated(bool isAsync)
        {
            await base.String_ends_with_inside_conditional_negated(isAsync);

            AssertSql(
                """
SELECT `f`.`FirstName` AS `fn`, `f0`.`LastName` AS `ln`
FROM `FunkyCustomers` AS `f`,
`FunkyCustomers` AS `f0`
WHERE IIF(`f`.`FirstName` IS NULL OR `f0`.`LastName` IS NULL OR RIGHT(`f`.`FirstName`, IIF(LEN(`f0`.`LastName`) IS NULL, 0, LEN(`f0`.`LastName`))) <> `f0`.`LastName`, TRUE, FALSE) = TRUE
""");
        }

        public override async Task String_ends_with_equals_nullable_column(bool isAsync)
        {
            await base.String_ends_with_equals_nullable_column(isAsync);

            AssertSql(
                """
SELECT `f`.`Id`, `f`.`FirstName`, `f`.`LastName`, `f`.`NullableBool`, `f0`.`Id`, `f0`.`FirstName`, `f0`.`LastName`, `f0`.`NullableBool`
FROM `FunkyCustomers` AS `f`,
`FunkyCustomers` AS `f0`
WHERE IIF(`f`.`FirstName` IS NOT NULL AND `f0`.`LastName` IS NOT NULL AND RIGHT(`f`.`FirstName`, IIF(LEN(`f0`.`LastName`) IS NULL, 0, LEN(`f0`.`LastName`))) = `f0`.`LastName`, TRUE, FALSE) = `f`.`NullableBool`
""");
        }

        public override async Task String_ends_with_not_equals_nullable_column(bool isAsync)
        {
            await base.String_ends_with_not_equals_nullable_column(isAsync);

            AssertSql(
                """
SELECT `f`.`Id`, `f`.`FirstName`, `f`.`LastName`, `f`.`NullableBool`, `f0`.`Id`, `f0`.`FirstName`, `f0`.`LastName`, `f0`.`NullableBool`
FROM `FunkyCustomers` AS `f`,
`FunkyCustomers` AS `f0`
WHERE IIF(`f`.`FirstName` IS NOT NULL AND `f0`.`LastName` IS NOT NULL AND RIGHT(`f`.`FirstName`, IIF(LEN(`f0`.`LastName`) IS NULL, 0, LEN(`f0`.`LastName`))) = `f0`.`LastName`, TRUE, FALSE) <> `f`.`NullableBool` OR `f`.`NullableBool` IS NULL
""");
        }

        protected override void ClearLog()
            => Fixture.TestSqlLoggerFactory.Clear();

        private void AssertSql(params string[] expected)
            => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);

        public class FunkyDataQueryJetFixture : FunkyDataQueryFixtureBase, ITestSqlLoggerFactory
        {
            public TestSqlLoggerFactory TestSqlLoggerFactory => (TestSqlLoggerFactory)ListLoggerFactory;

            protected override ITestStoreFactory TestStoreFactory => JetTestStoreFactory.Instance;
        }
    }
}
