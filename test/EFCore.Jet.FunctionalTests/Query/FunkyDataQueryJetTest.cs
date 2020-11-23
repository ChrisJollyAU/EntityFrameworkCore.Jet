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
            //Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
        }

        public override async Task String_contains_on_argument_with_wildcard_constant(bool isAsync)
        {
            await base.String_contains_on_argument_with_wildcard_constant(isAsync);

            AssertSql(
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE CHARINDEX('%B', `f`.`FirstName`) > 0",
                //
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE CHARINDEX('a_', `f`.`FirstName`) > 0",
                //
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE CHARINDEX(NULL, `f`.`FirstName`) > 0",
                //
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`",
                //
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE CHARINDEX('_Ba_', `f`.`FirstName`) > 0",
                //
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE CHARINDEX('%B%a%r', `f`.`FirstName`) <= 0",
                //
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE False = True",
                //
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE CHARINDEX(NULL, `f`.`FirstName`) <= 0");
        }

        public override async Task String_contains_on_argument_with_wildcard_parameter(bool isAsync)
        {
            await base.String_contains_on_argument_with_wildcard_parameter(isAsync);

            AssertSql(
                $@"{AssertSqlHelper.Declaration("@__prm1_0='%B' (Size = 4000)")}

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE ({AssertSqlHelper.Parameter("@__prm1_0")} = '') OR (CHARINDEX({AssertSqlHelper.Parameter("@__prm1_0")}, `f`.`FirstName`) > 0)",
                //
                $@"{AssertSqlHelper.Declaration("@__prm2_0='a_' (Size = 4000)")}

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE ({AssertSqlHelper.Parameter("@__prm2_0")} = '') OR (CHARINDEX({AssertSqlHelper.Parameter("@__prm2_0")}, `f`.`FirstName`) > 0)",
                //
                $@"@__prm3_0=NULL (Size = 4000)

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE CHARINDEX({AssertSqlHelper.Parameter("@__prm3_0")}, `f`.`FirstName`) > 0",
                //
                $@"{AssertSqlHelper.Declaration("@__prm4_0='' (Size = 4000)")}

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE ({AssertSqlHelper.Parameter("@__prm4_0")} = '') OR (CHARINDEX({AssertSqlHelper.Parameter("@__prm4_0")}, `f`.`FirstName`) > 0)",
                //
                $@"{AssertSqlHelper.Declaration("@__prm5_0='_Ba_' (Size = 4000)")}

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE ({AssertSqlHelper.Parameter("@__prm5_0")} = '') OR (CHARINDEX({AssertSqlHelper.Parameter("@__prm5_0")}, `f`.`FirstName`) > 0)",
                //
                $@"{AssertSqlHelper.Declaration("@__prm6_0='%B%a%r' (Size = 4000)")}

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE ({AssertSqlHelper.Parameter("@__prm6_0")} <> '') AND (CHARINDEX({AssertSqlHelper.Parameter("@__prm6_0")}, `f`.`FirstName`) <= 0)",
                //
                $@"{AssertSqlHelper.Declaration("@__prm7_0='' (Size = 4000)")}

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE ({AssertSqlHelper.Parameter("@__prm7_0")} <> '') AND (CHARINDEX({AssertSqlHelper.Parameter("@__prm7_0")}, `f`.`FirstName`) <= 0)",
                //
                $@"@__prm8_0=NULL (Size = 4000)

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE CHARINDEX({AssertSqlHelper.Parameter("@__prm8_0")}, `f`.`FirstName`) <= 0");
        }

        public override async Task String_contains_on_argument_with_wildcard_column(bool isAsync)
        {
            await base.String_contains_on_argument_with_wildcard_column(isAsync);

            AssertSql(
                $@"SELECT `f`.`FirstName` AS `fn`, `f0`.`LastName` AS `ln`
FROM `FunkyCustomers` AS `f`,
`FunkyCustomers` AS `f0`
WHERE (`f0`.`LastName` = '') OR (CHARINDEX(`f0`.`LastName`, `f`.`FirstName`) > 0)");
        }

        public override async Task String_contains_on_argument_with_wildcard_column_negated(bool isAsync)
        {
            await base.String_contains_on_argument_with_wildcard_column_negated(isAsync);

            AssertSql(
                $@"SELECT `f`.`FirstName` AS `fn`, `f0`.`LastName` AS `ln`
FROM `FunkyCustomers` AS `f`,
`FunkyCustomers` AS `f0`
WHERE ((`f0`.`LastName` <> '') OR `f0`.`LastName` IS NULL) AND (CHARINDEX(`f0`.`LastName`, `f`.`FirstName`) <= 0)");
        }

        public override async Task String_starts_with_on_argument_with_wildcard_constant(bool isAsync)
        {
            await base.String_starts_with_on_argument_with_wildcard_constant(isAsync);

            AssertSql(
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` IS NOT NULL AND (`f`.`FirstName` LIKE '\%B%' ESCAPE '\')",
                //
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` IS NOT NULL AND (`f`.`FirstName` LIKE 'a\_%' ESCAPE '\')",
                //
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE False = True",
                //
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`",
                //
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` IS NOT NULL AND (`f`.`FirstName` LIKE '\_Ba\_%' ESCAPE '\')",
                //
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` IS NOT NULL AND NOT (`f`.`FirstName` LIKE '\%B\%a\%r%' ESCAPE '\')",
                //
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE False = True",
                //
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE False = True");
        }

        public override async Task String_starts_with_on_argument_with_wildcard_parameter(bool isAsync)
        {
            await base.String_starts_with_on_argument_with_wildcard_parameter(isAsync);

            AssertSql(
                $@"{AssertSqlHelper.Declaration("@__prm1_0='%B' (Size = 4000)")}

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE ({AssertSqlHelper.Parameter("@__prm1_0")} = '') OR (`f`.`FirstName` IS NOT NULL AND (LEFT(`f`.`FirstName`, LEN({AssertSqlHelper.Parameter("@__prm1_0")})) = {AssertSqlHelper.Parameter("@__prm1_0")}))",
                //
                $@"{AssertSqlHelper.Declaration("@__prm2_0='a_' (Size = 4000)")}

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE ({AssertSqlHelper.Parameter("@__prm2_0")} = '') OR (`f`.`FirstName` IS NOT NULL AND (LEFT(`f`.`FirstName`, LEN({AssertSqlHelper.Parameter("@__prm2_0")})) = {AssertSqlHelper.Parameter("@__prm2_0")}))",
                //
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE False = True",
                //
                $@"{AssertSqlHelper.Declaration("@__prm4_0='' (Size = 4000)")}

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE ({AssertSqlHelper.Parameter("@__prm4_0")} = '') OR (`f`.`FirstName` IS NOT NULL AND (LEFT(`f`.`FirstName`, LEN({AssertSqlHelper.Parameter("@__prm4_0")})) = {AssertSqlHelper.Parameter("@__prm4_0")}))",
                //
                $@"{AssertSqlHelper.Declaration("@__prm5_0='_Ba_' (Size = 4000)")}

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE ({AssertSqlHelper.Parameter("@__prm5_0")} = '') OR (`f`.`FirstName` IS NOT NULL AND (LEFT(`f`.`FirstName`, LEN({AssertSqlHelper.Parameter("@__prm5_0")})) = {AssertSqlHelper.Parameter("@__prm5_0")}))",
                //
                $@"{AssertSqlHelper.Declaration("@__prm6_0='%B%a%r' (Size = 4000)")}

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE ({AssertSqlHelper.Parameter("@__prm6_0")} <> '') AND (`f`.`FirstName` IS NOT NULL AND ((LEFT(`f`.`FirstName`, LEN({AssertSqlHelper.Parameter("@__prm6_0")})) <> {AssertSqlHelper.Parameter("@__prm6_0")}) OR LEFT(`f`.`FirstName`, LEN({AssertSqlHelper.Parameter("@__prm6_0")})) IS NULL))",
                //
                $@"{AssertSqlHelper.Declaration("@__prm7_0='' (Size = 4000)")}

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE ({AssertSqlHelper.Parameter("@__prm7_0")} <> '') AND (`f`.`FirstName` IS NOT NULL AND ((LEFT(`f`.`FirstName`, LEN({AssertSqlHelper.Parameter("@__prm7_0")})) <> {AssertSqlHelper.Parameter("@__prm7_0")}) OR LEFT(`f`.`FirstName`, LEN({AssertSqlHelper.Parameter("@__prm7_0")})) IS NULL))",
                //
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE False = True");
        }

        public override async Task String_starts_with_on_argument_with_bracket(bool isAsync)
        {
            await base.String_starts_with_on_argument_with_bracket(isAsync);

            AssertSql(
                $@"SELECT `f`.`Id`, `f`.`FirstName`, `f`.`LastName`, `f`.`NullableBool`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` IS NOT NULL AND (`f`.`FirstName` LIKE '\[%' ESCAPE '\')",
                //
                $@"SELECT `f`.`Id`, `f`.`FirstName`, `f`.`LastName`, `f`.`NullableBool`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` IS NOT NULL AND (`f`.`FirstName` LIKE 'B\[%' ESCAPE '\')",
                //
                $@"SELECT `f`.`Id`, `f`.`FirstName`, `f`.`LastName`, `f`.`NullableBool`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` IS NOT NULL AND (`f`.`FirstName` LIKE 'B\[\[a^%' ESCAPE '\')",
                //
                $@"{AssertSqlHelper.Declaration("@__prm1_0='[' (Size = 4000)")}

SELECT `f`.`Id`, `f`.`FirstName`, `f`.`LastName`, `f`.`NullableBool`
FROM `FunkyCustomers` AS `f`
WHERE ({AssertSqlHelper.Parameter("@__prm1_0")} = '') OR (`f`.`FirstName` IS NOT NULL AND (LEFT(`f`.`FirstName`, LEN({AssertSqlHelper.Parameter("@__prm1_0")})) = {AssertSqlHelper.Parameter("@__prm1_0")}))",
                //
                $@"{AssertSqlHelper.Declaration("@__prm2_0='B[' (Size = 4000)")}

SELECT `f`.`Id`, `f`.`FirstName`, `f`.`LastName`, `f`.`NullableBool`
FROM `FunkyCustomers` AS `f`
WHERE ({AssertSqlHelper.Parameter("@__prm2_0")} = '') OR (`f`.`FirstName` IS NOT NULL AND (LEFT(`f`.`FirstName`, LEN({AssertSqlHelper.Parameter("@__prm2_0")})) = {AssertSqlHelper.Parameter("@__prm2_0")}))",
                //
                $@"{AssertSqlHelper.Declaration("@__prm3_0='B[[a^' (Size = 4000)")}

SELECT `f`.`Id`, `f`.`FirstName`, `f`.`LastName`, `f`.`NullableBool`
FROM `FunkyCustomers` AS `f`
WHERE ({AssertSqlHelper.Parameter("@__prm3_0")} = '') OR (`f`.`FirstName` IS NOT NULL AND (LEFT(`f`.`FirstName`, LEN({AssertSqlHelper.Parameter("@__prm3_0")})) = {AssertSqlHelper.Parameter("@__prm3_0")}))",
                //
                $@"SELECT `f`.`Id`, `f`.`FirstName`, `f`.`LastName`, `f`.`NullableBool`
FROM `FunkyCustomers` AS `f`
WHERE (`f`.`LastName` = '') OR (`f`.`FirstName` IS NOT NULL AND (`f`.`LastName` IS NOT NULL AND (LEFT(`f`.`FirstName`, LEN(`f`.`LastName`)) = `f`.`LastName`)))");
        }

        public override async Task String_starts_with_on_argument_with_wildcard_column(bool isAsync)
        {
            await base.String_starts_with_on_argument_with_wildcard_column(isAsync);

            AssertSql(
                $@"SELECT `f`.`FirstName` AS `fn`, `f0`.`LastName` AS `ln`
FROM `FunkyCustomers` AS `f`,
`FunkyCustomers` AS `f0`
WHERE (`f0`.`LastName` = '') OR (`f`.`FirstName` IS NOT NULL AND (`f0`.`LastName` IS NOT NULL AND (LEFT(`f`.`FirstName`, LEN(`f0`.`LastName`)) = `f0`.`LastName`)))");
        }

        public override async Task String_starts_with_on_argument_with_wildcard_column_negated(bool isAsync)
        {
            await base.String_starts_with_on_argument_with_wildcard_column_negated(isAsync);

            AssertSql(
                $@"SELECT `f`.`FirstName` AS `fn`, `f0`.`LastName` AS `ln`
FROM `FunkyCustomers` AS `f`,
`FunkyCustomers` AS `f0`
WHERE ((`f0`.`LastName` <> '') OR `f0`.`LastName` IS NULL) AND (`f`.`FirstName` IS NOT NULL AND (`f0`.`LastName` IS NOT NULL AND ((LEFT(`f`.`FirstName`, LEN(`f0`.`LastName`)) <> `f0`.`LastName`) OR LEFT(`f`.`FirstName`, LEN(`f0`.`LastName`)) IS NULL)))");
        }

        public override async Task String_ends_with_on_argument_with_wildcard_constant(bool isAsync)
        {
            await base.String_ends_with_on_argument_with_wildcard_constant(isAsync);

            AssertSql(
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` IS NOT NULL AND (`f`.`FirstName` LIKE '%\%B' ESCAPE '\')",
                //
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` IS NOT NULL AND (`f`.`FirstName` LIKE '%a\_' ESCAPE '\')",
                //
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE False = True",
                //
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`",
                //
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` IS NOT NULL AND (`f`.`FirstName` LIKE '%\_Ba\_' ESCAPE '\')",
                //
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE `f`.`FirstName` IS NOT NULL AND NOT (`f`.`FirstName` LIKE '%\%B\%a\%r' ESCAPE '\')",
                //
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE False = True",
                //
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE False = True");
        }

        public override async Task String_ends_with_on_argument_with_wildcard_parameter(bool isAsync)
        {
            await base.String_ends_with_on_argument_with_wildcard_parameter(isAsync);

            AssertSql(
                $@"{AssertSqlHelper.Declaration("@__prm1_0='%B' (Size = 4000)")}

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE ({AssertSqlHelper.Parameter("@__prm1_0")} = '') OR (`f`.`FirstName` IS NOT NULL AND (RIGHT(`f`.`FirstName`, LEN({AssertSqlHelper.Parameter("@__prm1_0")})) = {AssertSqlHelper.Parameter("@__prm1_0")}))",
                //
                $@"{AssertSqlHelper.Declaration("@__prm2_0='a_' (Size = 4000)")}

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE ({AssertSqlHelper.Parameter("@__prm2_0")} = '') OR (`f`.`FirstName` IS NOT NULL AND (RIGHT(`f`.`FirstName`, LEN({AssertSqlHelper.Parameter("@__prm2_0")})) = {AssertSqlHelper.Parameter("@__prm2_0")}))",
                //
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE False = True",
                //
                $@"{AssertSqlHelper.Declaration("@__prm4_0='' (Size = 4000)")}

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE ({AssertSqlHelper.Parameter("@__prm4_0")} = '') OR (`f`.`FirstName` IS NOT NULL AND (RIGHT(`f`.`FirstName`, LEN({AssertSqlHelper.Parameter("@__prm4_0")})) = {AssertSqlHelper.Parameter("@__prm4_0")}))",
                //
                $@"{AssertSqlHelper.Declaration("@__prm5_0='_Ba_' (Size = 4000)")}

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE ({AssertSqlHelper.Parameter("@__prm5_0")} = '') OR (`f`.`FirstName` IS NOT NULL AND (RIGHT(`f`.`FirstName`, LEN({AssertSqlHelper.Parameter("@__prm5_0")})) = {AssertSqlHelper.Parameter("@__prm5_0")}))",
                //
                $@"{AssertSqlHelper.Declaration("@__prm6_0='%B%a%r' (Size = 4000)")}

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE ({AssertSqlHelper.Parameter("@__prm6_0")} <> '') AND (`f`.`FirstName` IS NOT NULL AND ((RIGHT(`f`.`FirstName`, LEN({AssertSqlHelper.Parameter("@__prm6_0")})) <> {AssertSqlHelper.Parameter("@__prm6_0")}) OR RIGHT(`f`.`FirstName`, LEN({AssertSqlHelper.Parameter("@__prm6_0")})) IS NULL))",
                //
                $@"{AssertSqlHelper.Declaration("@__prm7_0='' (Size = 4000)")}

SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE ({AssertSqlHelper.Parameter("@__prm7_0")} <> '') AND (`f`.`FirstName` IS NOT NULL AND ((RIGHT(`f`.`FirstName`, LEN({AssertSqlHelper.Parameter("@__prm7_0")})) <> {AssertSqlHelper.Parameter("@__prm7_0")}) OR RIGHT(`f`.`FirstName`, LEN({AssertSqlHelper.Parameter("@__prm7_0")})) IS NULL))",
                //
                $@"SELECT `f`.`FirstName`
FROM `FunkyCustomers` AS `f`
WHERE False = True");
        }

        public override async Task String_ends_with_on_argument_with_wildcard_column(bool isAsync)
        {
            await base.String_ends_with_on_argument_with_wildcard_column(isAsync);

            AssertSql(
                $@"SELECT `f`.`FirstName` AS `fn`, `f0`.`LastName` AS `ln`
FROM `FunkyCustomers` AS `f`,
`FunkyCustomers` AS `f0`
WHERE (`f0`.`LastName` = '') OR (`f`.`FirstName` IS NOT NULL AND (`f0`.`LastName` IS NOT NULL AND (RIGHT(`f`.`FirstName`, LEN(`f0`.`LastName`)) = `f0`.`LastName`)))");
        }

        public override async Task String_ends_with_on_argument_with_wildcard_column_negated(bool isAsync)
        {
            await base.String_ends_with_on_argument_with_wildcard_column_negated(isAsync);

            AssertSql(
                $@"SELECT `f`.`FirstName` AS `fn`, `f0`.`LastName` AS `ln`
FROM `FunkyCustomers` AS `f`,
`FunkyCustomers` AS `f0`
WHERE ((`f0`.`LastName` <> '') OR `f0`.`LastName` IS NULL) AND (`f`.`FirstName` IS NOT NULL AND (`f0`.`LastName` IS NOT NULL AND ((RIGHT(`f`.`FirstName`, LEN(`f0`.`LastName`)) <> `f0`.`LastName`) OR RIGHT(`f`.`FirstName`, LEN(`f0`.`LastName`)) IS NULL)))");
        }

        public override async Task String_ends_with_inside_conditional(bool isAsync)
        {
            await base.String_ends_with_inside_conditional(isAsync);

            AssertSql(
                $@"SELECT `f`.`FirstName` AS `fn`, `f0`.`LastName` AS `ln`
FROM `FunkyCustomers` AS `f`,
`FunkyCustomers` AS `f0`
WHERE IIF((`f0`.`LastName` = '') OR (`f`.`FirstName` IS NOT NULL AND (`f0`.`LastName` IS NOT NULL AND (RIGHT(`f`.`FirstName`, LEN(`f0`.`LastName`)) = `f0`.`LastName`))), 1, 0) = True");
        }

        public override async Task String_ends_with_inside_conditional_negated(bool isAsync)
        {
            await base.String_ends_with_inside_conditional_negated(isAsync);

            AssertSql(
                $@"SELECT `f`.`FirstName` AS `fn`, `f0`.`LastName` AS `ln`
FROM `FunkyCustomers` AS `f`,
`FunkyCustomers` AS `f0`
WHERE IIF(((`f0`.`LastName` <> '') OR `f0`.`LastName` IS NULL) AND (`f`.`FirstName` IS NOT NULL AND (`f0`.`LastName` IS NOT NULL AND ((RIGHT(`f`.`FirstName`, LEN(`f0`.`LastName`)) <> `f0`.`LastName`) OR RIGHT(`f`.`FirstName`, LEN(`f0`.`LastName`)) IS NULL))), 1, 0) = True");
        }

        public override async Task String_ends_with_equals_nullable_column(bool isAsync)
        {
            await base.String_ends_with_equals_nullable_column(isAsync);

            AssertSql(
                $@"SELECT `f`.`Id`, `f`.`FirstName`, `f`.`LastName`, `f`.`NullableBool`, `f0`.`Id`, `f0`.`FirstName`, `f0`.`LastName`, `f0`.`NullableBool`
FROM `FunkyCustomers` AS `f`,
`FunkyCustomers` AS `f0`
WHERE IIF(((`f0`.`LastName` = '') AND `f0`.`LastName` IS NOT NULL) OR (`f`.`FirstName` IS NOT NULL AND (`f0`.`LastName` IS NOT NULL AND ((RIGHT(`f`.`FirstName`, LEN(`f0`.`LastName`)) = `f0`.`LastName`) AND RIGHT(`f`.`FirstName`, LEN(`f0`.`LastName`)) IS NOT NULL))), 1, 0) = `f`.`NullableBool`");
        }

        public override async Task String_ends_with_not_equals_nullable_column(bool isAsync)
        {
            await base.String_ends_with_not_equals_nullable_column(isAsync);

            AssertSql(
                $@"SELECT `f`.`Id`, `f`.`FirstName`, `f`.`LastName`, `f`.`NullableBool`, `f0`.`Id`, `f0`.`FirstName`, `f0`.`LastName`, `f0`.`NullableBool`
FROM `FunkyCustomers` AS `f`,
`FunkyCustomers` AS `f0`
WHERE (IIF(((`f0`.`LastName` = '') AND `f0`.`LastName` IS NOT NULL) OR (`f`.`FirstName` IS NOT NULL AND (`f0`.`LastName` IS NOT NULL AND ((RIGHT(`f`.`FirstName`, LEN(`f0`.`LastName`)) = `f0`.`LastName`) AND RIGHT(`f`.`FirstName`, LEN(`f0`.`LastName`)) IS NOT NULL))), 1, 0) <> `f`.`NullableBool`) OR `f`.`NullableBool` IS NULL");
        }

        protected override void ClearLog()
            => Fixture.TestSqlLoggerFactory.Clear();

        private void AssertSql(params string[] expected)
            => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);

        public class FunkyDataQueryJetFixture : FunkyDataQueryFixtureBase
        {
            public TestSqlLoggerFactory TestSqlLoggerFactory => (TestSqlLoggerFactory)ListLoggerFactory;

            protected override ITestStoreFactory TestStoreFactory => JetTestStoreFactory.Instance;
        }
    }
}
