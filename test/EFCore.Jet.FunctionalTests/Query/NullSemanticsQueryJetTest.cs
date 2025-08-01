using System.Data.Common;
using System.Threading.Tasks;
using EntityFrameworkCore.Jet.FunctionalTests.TestUtilities;
using EntityFrameworkCore.Jet.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestModels.NullSemanticsModel;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace EntityFrameworkCore.Jet.FunctionalTests.Query
{
    public class NullSemanticsQueryJetTest : NullSemanticsQueryTestBase<NullSemanticsQueryJetFixture>
    {
        // ReSharper disable once UnusedParameter.Local
        public NullSemanticsQueryJetTest(NullSemanticsQueryJetFixture fixture, ITestOutputHelper testOutputHelper)
            : base(fixture)
        {
            Fixture.TestSqlLoggerFactory.Clear();
            Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
        }

        public override async Task Rewrite_compare_int_with_int(bool async)
        {
            await base.Rewrite_compare_int_with_int(async);

            AssertSql(
                """
SELECT `e`.`Id`, CBOOL(`e`.`IntA` BXOR `e`.`IntB`) BXOR TRUE AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`IntA` = `e`.`IntB`
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableIntA` = `e`.`IntB` AND `e`.`NullableIntA` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` = `e`.`IntB`
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`IntA` = `e`.`NullableIntB` AND `e`.`NullableIntB` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`IntA` = `e`.`NullableIntB`
""",
                //
                """
SELECT `e`.`Id`, IIF((`e`.`NullableIntA` = `e`.`NullableIntB` AND `e`.`NullableIntA` IS NOT NULL AND `e`.`NullableIntB` IS NOT NULL) OR (`e`.`NullableIntA` IS NULL AND `e`.`NullableIntB` IS NULL), TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` = `e`.`NullableIntB` OR (`e`.`NullableIntA` IS NULL AND `e`.`NullableIntB` IS NULL)
""",
                //
                """
SELECT `e`.`Id`, CBOOL(`e`.`IntA` BXOR `e`.`IntB`) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`IntA` <> `e`.`IntB`
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableIntA` <> `e`.`IntB` OR `e`.`NullableIntA` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` <> `e`.`IntB` OR `e`.`NullableIntA` IS NULL
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`IntA` <> `e`.`NullableIntB` OR `e`.`NullableIntB` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`IntA` <> `e`.`NullableIntB` OR `e`.`NullableIntB` IS NULL
""",
                //
                """
SELECT `e`.`Id`, IIF((`e`.`NullableIntA` <> `e`.`NullableIntB` OR `e`.`NullableIntA` IS NULL OR `e`.`NullableIntB` IS NULL) AND (`e`.`NullableIntA` IS NOT NULL OR `e`.`NullableIntB` IS NOT NULL), TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableIntA` <> `e`.`NullableIntB` OR `e`.`NullableIntA` IS NULL OR `e`.`NullableIntB` IS NULL) AND (`e`.`NullableIntA` IS NOT NULL OR `e`.`NullableIntB` IS NOT NULL)
""",
                //
                """
SELECT `e`.`Id`, CBOOL(`e`.`IntA` BXOR `e`.`IntB`) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`IntA` <> `e`.`IntB`
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableIntA` <> `e`.`IntB` OR `e`.`NullableIntA` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` <> `e`.`IntB` OR `e`.`NullableIntA` IS NULL
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`IntA` <> `e`.`NullableIntB` OR `e`.`NullableIntB` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`IntA` <> `e`.`NullableIntB` OR `e`.`NullableIntB` IS NULL
""",
                //
                """
SELECT `e`.`Id`, IIF((`e`.`NullableIntA` <> `e`.`NullableIntB` OR `e`.`NullableIntA` IS NULL OR `e`.`NullableIntB` IS NULL) AND (`e`.`NullableIntA` IS NOT NULL OR `e`.`NullableIntB` IS NOT NULL), TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableIntA` <> `e`.`NullableIntB` OR `e`.`NullableIntA` IS NULL OR `e`.`NullableIntB` IS NULL) AND (`e`.`NullableIntA` IS NOT NULL OR `e`.`NullableIntB` IS NOT NULL)
""",
                //
                """
SELECT `e`.`Id`, CBOOL(`e`.`IntA` BXOR `e`.`IntB`) BXOR TRUE AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`IntA` = `e`.`IntB`
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableIntA` = `e`.`IntB` AND `e`.`NullableIntA` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` = `e`.`IntB`
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`IntA` = `e`.`NullableIntB` AND `e`.`NullableIntB` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`IntA` = `e`.`NullableIntB`
""",
                //
                """
SELECT `e`.`Id`, IIF((`e`.`NullableIntA` = `e`.`NullableIntB` AND `e`.`NullableIntA` IS NOT NULL AND `e`.`NullableIntB` IS NOT NULL) OR (`e`.`NullableIntA` IS NULL AND `e`.`NullableIntB` IS NULL), TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` = `e`.`NullableIntB` OR (`e`.`NullableIntA` IS NULL AND `e`.`NullableIntB` IS NULL)
""");
        }

        public override async Task Rewrite_compare_bool_with_bool(bool async)
        {
            await base.Rewrite_compare_bool_with_bool(async);

            AssertSql(
"""
SELECT `e`.`Id`, (`e`.`BoolA` BXOR `e`.`BoolB`) BXOR TRUE AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`BoolA` = `e`.`NullableBoolB` AND `e`.`NullableBoolB` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`NullableBoolB`
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA` AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = TRUE
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA` BXOR `e`.`BoolB` AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`BoolA` <> `e`.`NullableBoolB` AND `e`.`NullableBoolB` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`NullableBoolB`
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA` BXOR TRUE AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = FALSE
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` = `e`.`BoolB` AND `e`.`NullableBoolA` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`, IIF((`e`.`NullableBoolA` = `e`.`NullableBoolB` AND `e`.`NullableBoolA` IS NOT NULL AND `e`.`NullableBoolB` IS NOT NULL) OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL), TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`NullableBoolB` OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL)
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` = TRUE AND `e`.`NullableBoolA` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = TRUE
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` <> `e`.`BoolB` AND `e`.`NullableBoolA` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`, IIF((`e`.`NullableBoolA` <> `e`.`NullableBoolB` AND `e`.`NullableBoolA` IS NOT NULL AND `e`.`NullableBoolB` IS NOT NULL) OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL), TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> `e`.`NullableBoolB` OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL)
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` <> TRUE AND `e`.`NullableBoolA` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = FALSE
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolB` AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolB` = TRUE
""",
                //
                """
SELECT `e`.`Id`, IIF(TRUE = `e`.`NullableBoolB` AND `e`.`NullableBoolB` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolB` = TRUE
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolB` BXOR TRUE AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolB` = FALSE
""",
                //
                """
SELECT `e`.`Id`, IIF(FALSE = `e`.`NullableBoolB` AND `e`.`NullableBoolB` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE FALSE = `e`.`NullableBoolB`
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA` BXOR `e`.`BoolB` AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`BoolA` <> `e`.`NullableBoolB` AND `e`.`NullableBoolB` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`NullableBoolB`
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA` BXOR TRUE AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = FALSE
""",
                //
                """
SELECT `e`.`Id`, (`e`.`BoolA` BXOR `e`.`BoolB`) BXOR TRUE AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`BoolA` = `e`.`NullableBoolB` AND `e`.`NullableBoolB` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`NullableBoolB`
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA` AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = TRUE
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` <> `e`.`BoolB` AND `e`.`NullableBoolA` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`, IIF((`e`.`NullableBoolA` <> `e`.`NullableBoolB` AND `e`.`NullableBoolA` IS NOT NULL AND `e`.`NullableBoolB` IS NOT NULL) OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL), TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> `e`.`NullableBoolB` OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL)
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` = FALSE AND `e`.`NullableBoolA` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = FALSE
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` = `e`.`BoolB` AND `e`.`NullableBoolA` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`, IIF((`e`.`NullableBoolA` = `e`.`NullableBoolB` AND `e`.`NullableBoolA` IS NOT NULL AND `e`.`NullableBoolB` IS NOT NULL) OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL), TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`NullableBoolB` OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL)
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` <> FALSE AND `e`.`NullableBoolA` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> FALSE
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolB` BXOR TRUE AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolB` = FALSE
""",
                //
                """
SELECT `e`.`Id`, IIF(TRUE <> `e`.`NullableBoolB` AND `e`.`NullableBoolB` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolB` = FALSE
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolB` AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolB` = TRUE
""",
                //
                """
SELECT `e`.`Id`, IIF(FALSE <> `e`.`NullableBoolB` AND `e`.`NullableBoolB` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE FALSE <> `e`.`NullableBoolB`
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA` BXOR `e`.`BoolB` AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`BoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA` BXOR TRUE AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = FALSE
""",
                //
                """
SELECT `e`.`Id`, (`e`.`BoolA` BXOR `e`.`BoolB`) BXOR TRUE AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`BoolA` = `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA` AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = TRUE
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` <> `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL
""",
                //
                """
SELECT `e`.`Id`, IIF((`e`.`NullableBoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL), TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableBoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL)
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` <> TRUE OR `e`.`NullableBoolA` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> TRUE OR `e`.`NullableBoolA` IS NULL
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` = `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL
""",
                //
                """
SELECT `e`.`Id`, IIF((`e`.`NullableBoolA` = `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL), TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableBoolA` = `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL)
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` = TRUE OR `e`.`NullableBoolA` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = TRUE OR `e`.`NullableBoolA` IS NULL
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolB` BXOR TRUE AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolB` = FALSE
""",
                //
                """
SELECT `e`.`Id`, IIF(TRUE <> `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE TRUE <> `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolB` AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolB` = TRUE
""",
                //
                """
SELECT `e`.`Id`, IIF(FALSE <> `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE FALSE <> `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL
""",
                //
                """
SELECT `e`.`Id`, (`e`.`BoolA` BXOR `e`.`BoolB`) BXOR TRUE AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`BoolA` = `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA` AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = TRUE
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA` BXOR `e`.`BoolB` AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`BoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA` BXOR TRUE AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = FALSE
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` = `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL
""",
                //
                """
SELECT `e`.`Id`, IIF((`e`.`NullableBoolA` = `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL), TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableBoolA` = `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL)
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` <> FALSE OR `e`.`NullableBoolA` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> FALSE OR `e`.`NullableBoolA` IS NULL
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` <> `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL
""",
                //
                """
SELECT `e`.`Id`, IIF((`e`.`NullableBoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL), TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableBoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL)
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` = FALSE OR `e`.`NullableBoolA` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = FALSE OR `e`.`NullableBoolA` IS NULL
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolB` AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolB` = TRUE
""",
                //
                """
SELECT `e`.`Id`, IIF(TRUE = `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE TRUE = `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolB` BXOR TRUE AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolB` = FALSE
""",
                //
                """
SELECT `e`.`Id`, IIF(FALSE = `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE FALSE = `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA` BXOR `e`.`BoolB` AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`BoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA` BXOR TRUE AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = FALSE
""",
                //
                """
SELECT `e`.`Id`, (`e`.`BoolA` BXOR `e`.`BoolB`) BXOR TRUE AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`BoolA` = `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA` AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = TRUE
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` <> `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL
""",
                //
                """
SELECT `e`.`Id`, IIF((`e`.`NullableBoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL), TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableBoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL)
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` <> TRUE OR `e`.`NullableBoolA` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> TRUE OR `e`.`NullableBoolA` IS NULL
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` = `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL
""",
                //
                """
SELECT `e`.`Id`, IIF((`e`.`NullableBoolA` = `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL), TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableBoolA` = `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL)
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` = TRUE OR `e`.`NullableBoolA` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = TRUE OR `e`.`NullableBoolA` IS NULL
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolB` BXOR TRUE AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolB` = FALSE
""",
                //
                """
SELECT `e`.`Id`, IIF(TRUE <> `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE TRUE <> `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolB` AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolB` = TRUE
""",
                //
                """
SELECT `e`.`Id`, IIF(FALSE <> `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE FALSE <> `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL
""",
                //
                """
SELECT `e`.`Id`, (`e`.`BoolA` BXOR `e`.`BoolB`) BXOR TRUE AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`BoolA` = `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA` AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = TRUE
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA` BXOR `e`.`BoolB` AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`BoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA` BXOR TRUE AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = FALSE
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` = `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL
""",
                //
                """
SELECT `e`.`Id`, IIF((`e`.`NullableBoolA` = `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL), TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableBoolA` = `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL)
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` <> FALSE OR `e`.`NullableBoolA` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> FALSE OR `e`.`NullableBoolA` IS NULL
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` <> `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL
""",
                //
                """
SELECT `e`.`Id`, IIF((`e`.`NullableBoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL), TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableBoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL)
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` = FALSE OR `e`.`NullableBoolA` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = FALSE OR `e`.`NullableBoolA` IS NULL
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolB` AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolB` = TRUE
""",
                //
                """
SELECT `e`.`Id`, IIF(TRUE = `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE TRUE = `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolB` BXOR TRUE AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolB` = FALSE
""",
                //
                """
SELECT `e`.`Id`, IIF(FALSE = `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE FALSE = `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL
""",
                //
                """
SELECT `e`.`Id`, (`e`.`BoolA` BXOR `e`.`BoolB`) BXOR TRUE AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`BoolA` = `e`.`NullableBoolB` AND `e`.`NullableBoolB` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`NullableBoolB`
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA` AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = TRUE
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA` BXOR `e`.`BoolB` AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`BoolA` <> `e`.`NullableBoolB` AND `e`.`NullableBoolB` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`NullableBoolB`
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA` BXOR TRUE AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = FALSE
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` = `e`.`BoolB` AND `e`.`NullableBoolA` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`, IIF((`e`.`NullableBoolA` = `e`.`NullableBoolB` AND `e`.`NullableBoolA` IS NOT NULL AND `e`.`NullableBoolB` IS NOT NULL) OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL), TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`NullableBoolB` OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL)
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` = TRUE AND `e`.`NullableBoolA` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = TRUE
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` <> `e`.`BoolB` AND `e`.`NullableBoolA` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`, IIF((`e`.`NullableBoolA` <> `e`.`NullableBoolB` AND `e`.`NullableBoolA` IS NOT NULL AND `e`.`NullableBoolB` IS NOT NULL) OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL), TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> `e`.`NullableBoolB` OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL)
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` <> TRUE AND `e`.`NullableBoolA` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = FALSE
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolB` AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolB` = TRUE
""",
                //
                """
SELECT `e`.`Id`, IIF(TRUE = `e`.`NullableBoolB` AND `e`.`NullableBoolB` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolB` = TRUE
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolB` BXOR TRUE AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolB` = FALSE
""",
                //
                """
SELECT `e`.`Id`, IIF(FALSE = `e`.`NullableBoolB` AND `e`.`NullableBoolB` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE FALSE = `e`.`NullableBoolB`
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA` BXOR `e`.`BoolB` AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`BoolA` <> `e`.`NullableBoolB` AND `e`.`NullableBoolB` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`NullableBoolB`
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA` BXOR TRUE AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = FALSE
""",
                //
                """
SELECT `e`.`Id`, (`e`.`BoolA` BXOR `e`.`BoolB`) BXOR TRUE AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`BoolA` = `e`.`NullableBoolB` AND `e`.`NullableBoolB` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`NullableBoolB`
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA` AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = TRUE
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` <> `e`.`BoolB` AND `e`.`NullableBoolA` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`, IIF((`e`.`NullableBoolA` <> `e`.`NullableBoolB` AND `e`.`NullableBoolA` IS NOT NULL AND `e`.`NullableBoolB` IS NOT NULL) OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL), TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> `e`.`NullableBoolB` OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL)
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` = FALSE AND `e`.`NullableBoolA` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = FALSE
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` = `e`.`BoolB` AND `e`.`NullableBoolA` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`, IIF((`e`.`NullableBoolA` = `e`.`NullableBoolB` AND `e`.`NullableBoolA` IS NOT NULL AND `e`.`NullableBoolB` IS NOT NULL) OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL), TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`NullableBoolB` OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL)
""",
                //
                """
SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` <> FALSE AND `e`.`NullableBoolA` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> FALSE
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolB` BXOR TRUE AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolB` = FALSE
""",
                //
                """
SELECT `e`.`Id`, IIF(TRUE <> `e`.`NullableBoolB` AND `e`.`NullableBoolB` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolB` = FALSE
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolB` AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolB` = TRUE
""",
                //
                """
SELECT `e`.`Id`, IIF(FALSE <> `e`.`NullableBoolB` AND `e`.`NullableBoolB` IS NOT NULL, TRUE, FALSE) AS `X`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE FALSE <> `e`.`NullableBoolB`
""");
        }

        public override async Task Compare_bool_with_bool_equal(bool async)
        {
            await base.Compare_bool_with_bool_equal(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`BoolB`
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`NullableBoolB`
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`BoolB`
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`NullableBoolB` OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL)
""");
        }

        public override async Task Compare_negated_bool_with_bool_equal(bool async)
        {
            await base.Compare_negated_bool_with_bool_equal(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`NullableBoolB`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> `e`.`NullableBoolB` OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL)
""");
        }

        public override async Task Compare_bool_with_negated_bool_equal(bool async)
        {
            await base.Compare_bool_with_negated_bool_equal(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`NullableBoolB`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> `e`.`NullableBoolB` OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL)
""");
        }

        public override async Task Compare_negated_bool_with_negated_bool_equal(bool async)
        {
            await base.Compare_negated_bool_with_negated_bool_equal(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`NullableBoolB`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`NullableBoolB` OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL)
""");
        }

        public override async Task Compare_bool_with_bool_equal_negated(bool async)
        {
            await base.Compare_bool_with_bool_equal_negated(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`BoolB`
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableBoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL)
""");
        }

        public override async Task Compare_negated_bool_with_bool_equal_negated(bool async)
        {
            await base.Compare_negated_bool_with_bool_equal_negated(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`BoolB`
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableBoolA` = `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL)
""");
        }

        public override async Task Compare_bool_with_negated_bool_equal_negated(bool async)
        {
            await base.Compare_bool_with_negated_bool_equal_negated(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`BoolB`
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableBoolA` = `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL)
""");
        }

        public override async Task Compare_negated_bool_with_negated_bool_equal_negated(bool async)
        {
            await base.Compare_negated_bool_with_negated_bool_equal_negated(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`BoolB`
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableBoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL)
""");
        }

        public override async Task Compare_bool_with_bool_not_equal(bool async)
        {
            await base.Compare_bool_with_bool_not_equal(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`BoolB`
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableBoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL)
""");
        }

        public override async Task Compare_negated_bool_with_bool_not_equal(bool async)
        {
            await base.Compare_negated_bool_with_bool_not_equal(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`BoolB`
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableBoolA` = `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL)
""");
        }

        public override async Task Compare_bool_with_negated_bool_not_equal(bool async)
        {
            await base.Compare_bool_with_negated_bool_not_equal(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`BoolB`
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableBoolA` = `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL)
""");
        }

        public override async Task Compare_negated_bool_with_negated_bool_not_equal(bool async)
        {
            await base.Compare_negated_bool_with_negated_bool_not_equal(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`BoolB`
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableBoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL)
""");
        }

        public override async Task Compare_bool_with_bool_not_equal_negated(bool async)
        {
            await base.Compare_bool_with_bool_not_equal_negated(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`NullableBoolB`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`NullableBoolB` OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL)
""");
        }

        public override async Task Compare_negated_bool_with_bool_not_equal_negated(bool async)
        {
            await base.Compare_negated_bool_with_bool_not_equal_negated(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`NullableBoolB`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> `e`.`NullableBoolB` OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL)
""");
        }

        public override async Task Compare_bool_with_negated_bool_not_equal_negated(bool async)
        {
            await base.Compare_bool_with_negated_bool_not_equal_negated(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`NullableBoolB`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> `e`.`NullableBoolB` OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL)
""");
        }

        public override async Task Compare_negated_bool_with_negated_bool_not_equal_negated(bool async)
        {
            await base.Compare_negated_bool_with_negated_bool_not_equal_negated(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`NullableBoolB`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`BoolB`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`NullableBoolB` OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL)
""");
        }

        public override async Task Compare_equals_method(bool async)
        {
            await base.Compare_equals_method(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`BoolB`
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`NullableBoolB`
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`BoolB`
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`NullableBoolB` OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL)
""");
        }

        public override async Task Compare_equals_method_static(bool async)
        {
            await base.Compare_equals_method_static(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`BoolB`
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = `e`.`NullableBoolB`
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`BoolB`
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`NullableBoolB` OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL)
""");
        }

        public override async Task Compare_equals_method_negated(bool async)
        {
            await base.Compare_equals_method_negated(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`BoolB`
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableBoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL)
""");
        }

        public override async Task Compare_equals_method_negated_static(bool async)
        {
            await base.Compare_equals_method_negated_static(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`BoolB`
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` <> `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableBoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL)
""");
        }

        public override async Task Compare_complex_equal_equal_equal(bool async)
        {
            await base.Compare_complex_equal_equal_equal(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE ((`e`.`BoolA` BXOR `e`.`BoolB`) BXOR TRUE) = (CBOOL(`e`.`IntA` BXOR `e`.`IntB`) BXOR TRUE)
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE IIF(`e`.`NullableBoolA` = `e`.`BoolB` AND `e`.`NullableBoolA` IS NOT NULL, TRUE, FALSE) = IIF(`e`.`IntA` = `e`.`NullableIntB` AND `e`.`NullableIntB` IS NOT NULL, TRUE, FALSE)
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE IIF((`e`.`NullableBoolA` = `e`.`NullableBoolB` AND `e`.`NullableBoolA` IS NOT NULL AND `e`.`NullableBoolB` IS NOT NULL) OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL), TRUE, FALSE) = IIF((`e`.`NullableIntA` = `e`.`NullableIntB` AND `e`.`NullableIntA` IS NOT NULL AND `e`.`NullableIntB` IS NOT NULL) OR (`e`.`NullableIntA` IS NULL AND `e`.`NullableIntB` IS NULL), TRUE, FALSE)
""");
        }

        public override async Task Compare_complex_equal_not_equal_equal(bool async)
        {
            await base.Compare_complex_equal_not_equal_equal(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE ((`e`.`BoolA` BXOR `e`.`BoolB`) BXOR TRUE) <> (CBOOL(`e`.`IntA` BXOR `e`.`IntB`) BXOR TRUE)
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE IIF(`e`.`NullableBoolA` = `e`.`BoolB` AND `e`.`NullableBoolA` IS NOT NULL, TRUE, FALSE) <> IIF(`e`.`IntA` = `e`.`NullableIntB` AND `e`.`NullableIntB` IS NOT NULL, TRUE, FALSE)
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE IIF((`e`.`NullableBoolA` = `e`.`NullableBoolB` AND `e`.`NullableBoolA` IS NOT NULL AND `e`.`NullableBoolB` IS NOT NULL) OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL), TRUE, FALSE) <> IIF((`e`.`NullableIntA` = `e`.`NullableIntB` AND `e`.`NullableIntA` IS NOT NULL AND `e`.`NullableIntB` IS NOT NULL) OR (`e`.`NullableIntA` IS NULL AND `e`.`NullableIntB` IS NULL), TRUE, FALSE)
""");
        }

        public override async Task Compare_complex_not_equal_equal_equal(bool async)
        {
            await base.Compare_complex_not_equal_equal_equal(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`BoolA` BXOR `e`.`BoolB`) = (CBOOL(`e`.`IntA` BXOR `e`.`IntB`) BXOR TRUE)
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE IIF(`e`.`NullableBoolA` <> `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL, TRUE, FALSE) = IIF(`e`.`IntA` = `e`.`NullableIntB` AND `e`.`NullableIntB` IS NOT NULL, TRUE, FALSE)
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE IIF((`e`.`NullableBoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL), TRUE, FALSE) = IIF((`e`.`NullableIntA` = `e`.`NullableIntB` AND `e`.`NullableIntA` IS NOT NULL AND `e`.`NullableIntB` IS NOT NULL) OR (`e`.`NullableIntA` IS NULL AND `e`.`NullableIntB` IS NULL), TRUE, FALSE)
""");
        }

        public override async Task Compare_complex_not_equal_not_equal_equal(bool async)
        {
            await base.Compare_complex_not_equal_not_equal_equal(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`BoolA` BXOR `e`.`BoolB`) <> (CBOOL(`e`.`IntA` BXOR `e`.`IntB`) BXOR TRUE)
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE IIF(`e`.`NullableBoolA` <> `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL, TRUE, FALSE) <> IIF(`e`.`IntA` = `e`.`NullableIntB` AND `e`.`NullableIntB` IS NOT NULL, TRUE, FALSE)
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE IIF((`e`.`NullableBoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL), TRUE, FALSE) <> IIF((`e`.`NullableIntA` = `e`.`NullableIntB` AND `e`.`NullableIntA` IS NOT NULL AND `e`.`NullableIntB` IS NOT NULL) OR (`e`.`NullableIntA` IS NULL AND `e`.`NullableIntB` IS NULL), TRUE, FALSE)
""");
        }

        public override async Task Compare_complex_not_equal_equal_not_equal(bool async)
        {
            await base.Compare_complex_not_equal_equal_not_equal(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`BoolA` BXOR `e`.`BoolB`) = CBOOL(`e`.`IntA` BXOR `e`.`IntB`)
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE IIF(`e`.`NullableBoolA` <> `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL, TRUE, FALSE) = IIF(`e`.`IntA` <> `e`.`NullableIntB` OR `e`.`NullableIntB` IS NULL, TRUE, FALSE)
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE IIF((`e`.`NullableBoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL), TRUE, FALSE) = IIF((`e`.`NullableIntA` <> `e`.`NullableIntB` OR `e`.`NullableIntA` IS NULL OR `e`.`NullableIntB` IS NULL) AND (`e`.`NullableIntA` IS NOT NULL OR `e`.`NullableIntB` IS NOT NULL), TRUE, FALSE)
""");
        }

        public override async Task Compare_complex_not_equal_not_equal_not_equal(bool async)
        {
            await base.Compare_complex_not_equal_not_equal_not_equal(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`BoolA` BXOR `e`.`BoolB`) <> CBOOL(`e`.`IntA` BXOR `e`.`IntB`)
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE IIF(`e`.`NullableBoolA` <> `e`.`BoolB` OR `e`.`NullableBoolA` IS NULL, TRUE, FALSE) <> IIF(`e`.`IntA` <> `e`.`NullableIntB` OR `e`.`NullableIntB` IS NULL, TRUE, FALSE)
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE IIF((`e`.`NullableBoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL), TRUE, FALSE) <> IIF((`e`.`NullableIntA` <> `e`.`NullableIntB` OR `e`.`NullableIntA` IS NULL OR `e`.`NullableIntB` IS NULL) AND (`e`.`NullableIntA` IS NOT NULL OR `e`.`NullableIntB` IS NOT NULL), TRUE, FALSE)
""");
        }

        public override async Task Compare_nullable_with_null_parameter_equal(bool async)
        {
            await base.Compare_nullable_with_null_parameter_equal(async);

            AssertSql(
                $"""
                    SELECT `e`.`Id`
                    FROM `Entities1` AS `e`
                    WHERE `e`.`NullableStringA` IS NULL
                    """);
        }

        public override async Task Compare_nullable_with_non_null_parameter_not_equal(bool async)
        {
            await base.Compare_nullable_with_non_null_parameter_not_equal(async);

            AssertSql(
                """
@prm='Foo' (Size = 255)

SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringA` = @prm
""");
        }

        public override async Task Join_uses_database_semantics(bool async)
        {
            await base.Join_uses_database_semantics(async);

            AssertSql(
                $"""
                    SELECT `e`.`Id` AS `Id1`, `e0`.`Id` AS `Id2`, `e`.`NullableIntA`, `e0`.`NullableIntB`
                    FROM `Entities1` AS `e`
                    INNER JOIN `Entities2` AS `e0` ON `e`.`NullableIntA` = `e0`.`NullableIntB`
                    """);
        }

        public override async Task Join_uses_csharp_semantics_for_anon_objects(bool async)
        {
            await base.Join_uses_csharp_semantics_for_anon_objects(async);

            AssertSql(
                """
SELECT `e`.`Id` AS `Id1`, `e0`.`Id` AS `Id2`, `e`.`NullableIntA`, `e0`.`NullableIntB`
FROM `Entities1` AS `e`
INNER JOIN `Entities2` AS `e0` ON (`e`.`NullableIntA` = `e0`.`NullableIntB`) OR (`e`.`NullableIntA` IS NULL AND `e0`.`NullableIntB` IS NULL)
""");
        }

        public override async Task Contains_with_local_array_closure_with_null(bool async)
        {
            await base.Contains_with_local_array_closure_with_null(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringA` IS NULL OR `e`.`NullableStringA` = 'Foo'
""");
        }

        public override async Task Contains_with_local_array_closure_false_with_null(bool async)
        {
            await base.Contains_with_local_array_closure_false_with_null(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringA` IS NOT NULL AND `e`.`NullableStringA` <> 'Foo'
""");
        }

        public override async Task Contains_with_local_nullable_array_closure_negated(bool async)
        {
            await base.Contains_with_local_nullable_array_closure_negated(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringA` <> 'Foo' OR `e`.`NullableStringA` IS NULL
""");
        }

        public override async Task Contains_with_local_array_closure_with_multiple_nulls(bool async)
        {
            await base.Contains_with_local_array_closure_with_multiple_nulls(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringA` IS NULL OR `e`.`NullableStringA` = 'Foo'
""");
        }

        public override async Task Where_multiple_ors_with_null(bool async)
        {
            await base.Where_multiple_ors_with_null(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringA` IN ('Foo', 'Blah') OR `e`.`NullableStringA` IS NULL
""");
        }

        public override async Task Where_multiple_ands_with_null(bool async)
        {
            await base.Where_multiple_ands_with_null(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringA` NOT IN ('Foo', 'Blah') AND `e`.`NullableStringA` IS NOT NULL
""");
        }

        public override async Task Where_multiple_ors_with_nullable_parameter(bool async)
        {
            await base.Where_multiple_ors_with_nullable_parameter(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringA` = 'Foo' OR `e`.`NullableStringA` IS NULL
""");
        }

        public override async Task Where_multiple_ands_with_nullable_parameter_and_constant(bool async)
        {
            await base.Where_multiple_ands_with_nullable_parameter_and_constant(async);

            AssertSql(
                """
@prm3='Blah' (Size = 255)

SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableStringA` <> 'Foo' OR `e`.`NullableStringA` IS NULL) AND `e`.`NullableStringA` IS NOT NULL AND `e`.`NullableStringA` <> @prm3
""");
        }

        public override async Task Where_multiple_ands_with_nullable_parameter_and_constant_not_optimized(bool async)
        {
            await base.Where_multiple_ands_with_nullable_parameter_and_constant_not_optimized(async);

            AssertSql(
                """
@prm3='Blah' (Size = 255)

SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringB` IS NOT NULL AND (`e`.`NullableStringA` <> 'Foo' OR `e`.`NullableStringA` IS NULL) AND `e`.`NullableStringA` IS NOT NULL AND `e`.`NullableStringA` <> @prm3
""");
        }

        public override async Task Where_coalesce(bool async)
        {
            await base.Where_coalesce(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE IIF(`e`.`NullableBoolA` IS NULL, TRUE, `e`.`NullableBoolA`) = TRUE
""");
        }

        public override async Task Where_coalesce_shortcircuit(bool async)
        {
            await base.Where_coalesce_shortcircuit(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = TRUE OR `e`.`BoolB` = TRUE
""");
        }

        public override async Task Where_coalesce_shortcircuit_many(bool async)
        {
            await base.Where_coalesce_shortcircuit_many(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE IIF(`e`.`NullableBoolA` IS NULL, IIF(`e`.`BoolA` = TRUE OR `e`.`BoolB` = TRUE, TRUE, FALSE), `e`.`NullableBoolA`) = TRUE
""");
        }

        public override async Task Where_equal_nullable_with_null_value_parameter(bool async)
        {
            await base.Where_equal_nullable_with_null_value_parameter(async);

            AssertSql(
                $"""
                    SELECT `e`.`Id`
                    FROM `Entities1` AS `e`
                    WHERE `e`.`NullableStringA` IS NULL
                    """);
        }

        public override async Task Where_not_equal_nullable_with_null_value_parameter(bool async)
        {
            await base.Where_not_equal_nullable_with_null_value_parameter(async);

            AssertSql(
                $"""
                    SELECT `e`.`Id`
                    FROM `Entities1` AS `e`
                    WHERE `e`.`NullableStringA` IS NOT NULL
                    """);
        }

        public override async Task Where_equal_with_coalesce(bool async)
        {
            await base.Where_equal_with_coalesce(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE IIF(`e`.`NullableStringA` IS NULL, `e`.`NullableStringB`, `e`.`NullableStringA`) = `e`.`NullableStringC` OR (`e`.`NullableStringA` IS NULL AND `e`.`NullableStringB` IS NULL AND `e`.`NullableStringC` IS NULL)
""");
        }

        public override async Task Where_not_equal_with_coalesce(bool async)
        {
            await base.Where_not_equal_with_coalesce(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (IIF(`e`.`NullableStringA` IS NULL, `e`.`NullableStringB`, `e`.`NullableStringA`) <> `e`.`NullableStringC` OR (`e`.`NullableStringA` IS NULL AND `e`.`NullableStringB` IS NULL) OR `e`.`NullableStringC` IS NULL) AND (`e`.`NullableStringA` IS NOT NULL OR `e`.`NullableStringB` IS NOT NULL OR `e`.`NullableStringC` IS NOT NULL)
""");
        }

        public override async Task Where_equal_with_coalesce_both_sides(bool async)
        {
            await base.Where_equal_with_coalesce_both_sides(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE IIF(`e`.`NullableStringA` IS NULL, `e`.`NullableStringB`, `e`.`NullableStringA`) = IIF(`e`.`NullableStringC` IS NULL, `e`.`StringA`, `e`.`NullableStringC`)
""");
        }

        public override async Task Where_not_equal_with_coalesce_both_sides(bool async)
        {
            await base.Where_not_equal_with_coalesce_both_sides(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (IIF(`e`.`NullableIntA` IS NULL, `e`.`NullableIntB`, `e`.`NullableIntA`) <> IIF(`e`.`NullableIntC` IS NULL, `e`.`NullableIntB`, `e`.`NullableIntC`) OR (`e`.`NullableIntA` IS NULL AND `e`.`NullableIntB` IS NULL) OR (`e`.`NullableIntC` IS NULL AND `e`.`NullableIntB` IS NULL)) AND (`e`.`NullableIntA` IS NOT NULL OR `e`.`NullableIntB` IS NOT NULL OR `e`.`NullableIntC` IS NOT NULL OR `e`.`NullableIntB` IS NOT NULL)
""");
        }

        public override async Task Where_equal_with_conditional(bool async)
        {
            await base.Where_equal_with_conditional(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE IIF(`e`.`NullableStringA` = `e`.`NullableStringB` OR (`e`.`NullableStringA` IS NULL AND `e`.`NullableStringB` IS NULL), `e`.`NullableStringA`, `e`.`NullableStringB`) = `e`.`NullableStringC` OR (IIF(`e`.`NullableStringA` = `e`.`NullableStringB` OR (`e`.`NullableStringA` IS NULL AND `e`.`NullableStringB` IS NULL), `e`.`NullableStringA`, `e`.`NullableStringB`) IS NULL AND `e`.`NullableStringC` IS NULL)
""");
        }

        public override async Task Where_not_equal_with_conditional(bool async)
        {
            await base.Where_not_equal_with_conditional(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableStringC` <> IIF(`e`.`NullableStringA` = `e`.`NullableStringB` OR (`e`.`NullableStringA` IS NULL AND `e`.`NullableStringB` IS NULL), `e`.`NullableStringA`, `e`.`NullableStringB`) OR `e`.`NullableStringC` IS NULL OR IIF(`e`.`NullableStringA` = `e`.`NullableStringB` OR (`e`.`NullableStringA` IS NULL AND `e`.`NullableStringB` IS NULL), `e`.`NullableStringA`, `e`.`NullableStringB`) IS NULL) AND (`e`.`NullableStringC` IS NOT NULL OR IIF(`e`.`NullableStringA` = `e`.`NullableStringB` OR (`e`.`NullableStringA` IS NULL AND `e`.`NullableStringB` IS NULL), `e`.`NullableStringA`, `e`.`NullableStringB`) IS NOT NULL)
""");
        }

        public override async Task Where_equal_with_conditional_non_nullable(bool async)
        {
            await base.Where_equal_with_conditional_non_nullable(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringC` <> IIF(`e`.`NullableStringA` = `e`.`NullableStringB` OR (`e`.`NullableStringA` IS NULL AND `e`.`NullableStringB` IS NULL), `e`.`StringA`, `e`.`StringB`) OR `e`.`NullableStringC` IS NULL
""");
        }

        public override async Task Where_equal_with_and_and_contains(bool async)
        {
            await base.Where_equal_with_and_and_contains(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringA` IS NOT NULL AND `e`.`NullableStringB` IS NOT NULL AND (INSTR(1, `e`.`NullableStringA`, `e`.`NullableStringB`, 1) > 0 OR (`e`.`NullableStringB` LIKE '')) AND `e`.`BoolA` = TRUE
""");
        }

        public override async Task Null_comparison_in_selector_with_relational_nulls(bool async)
        {
            await base.Null_comparison_in_selector_with_relational_nulls(async);

            AssertSql(
"""
SELECT IIF(`e`.`NullableStringA` <> 'Foo' OR `e`.`NullableStringA` IS NULL, TRUE, FALSE)
FROM `Entities1` AS `e`
""");
        }

        public override async Task Null_comparison_in_order_by_with_relational_nulls(bool async)
        {
            await base.Null_comparison_in_order_by_with_relational_nulls(async);
            AssertSql(
            """
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
ORDER BY NOT (IIF(`e`.`NullableStringA` <> 'Foo' OR `e`.`NullableStringA` IS NULL, TRUE, FALSE)), NOT (IIF(`e`.`NullableIntB` <> 10 OR `e`.`NullableIntB` IS NULL, TRUE, FALSE))
""");
        }

        public override async Task Null_comparison_in_join_key_with_relational_nulls(bool async)
        {
            await base.Null_comparison_in_join_key_with_relational_nulls(async);

            AssertSql(
                $"""
                    SELECT `e1`.`Id`, `e1`.`BoolA`, `e1`.`BoolB`, `e1`.`BoolC`, `e1`.`IntA`, `e1`.`IntB`, `e1`.`IntC`, `e1`.`NullableBoolA`, `e1`.`NullableBoolB`, `e1`.`NullableBoolC`, `e1`.`NullableIntA`, `e1`.`NullableIntB`, `e1`.`NullableIntC`, `e1`.`NullableStringA`, `e1`.`NullableStringB`, `e1`.`NullableStringC`, `e1`.`StringA`, `e1`.`StringB`, `e1`.`StringC`, `e2`.`Id`, `e2`.`BoolA`, `e2`.`BoolB`, `e2`.`BoolC`, `e2`.`IntA`, `e2`.`IntB`, `e2`.`IntC`, `e2`.`NullableBoolA`, `e2`.`NullableBoolB`, `e2`.`NullableBoolC`, `e2`.`NullableIntA`, `e2`.`NullableIntB`, `e2`.`NullableIntC`, `e2`.`NullableStringA`, `e2`.`NullableStringB`, `e2`.`NullableStringC`, `e2`.`StringA`, `e2`.`StringB`, `e2`.`StringC`
                    FROM `Entities1` AS `e1`
                    INNER JOIN `Entities2` AS `e2` ON CASE
                        WHEN `e1`.`NullableStringA` <> 'Foo'
                        THEN True ELSE False
                    END = CASE
                        WHEN `e2`.`NullableBoolB` <> True
                        THEN True ELSE False
                    END
                    """);
        }

        public override async Task Where_conditional_search_condition_in_result(bool async)
        {
            await base.Where_conditional_search_condition_in_result(async);

            AssertSql(
                $"""
                    SELECT `e`.`Id`
                    FROM `Entities1` AS `e`
                    WHERE `e`.`StringA` IN ('Foo', 'Bar')
                    """,
                //
                $"""
                    SELECT `e`.`Id`
                    FROM `Entities1` AS `e`
                    WHERE `e`.`StringA` LIKE 'B%'
                    """);
        }

        public override async Task Where_nested_conditional_search_condition_in_result(bool async)
        {
            await base.Where_nested_conditional_search_condition_in_result(async);

            AssertSql(
                $"""
                    SELECT `e`.`Id`
                    FROM `Entities1` AS `e`
                    """);
        }

        public override void Where_equal_using_relational_null_semantics()
        {
            base.Where_equal_using_relational_null_semantics();

            AssertSql(
                $"""
                    SELECT `e`.`Id`
                    FROM `Entities1` AS `e`
                    WHERE `e`.`NullableBoolA` = `e`.`NullableBoolB`
                    """);
        }

        public override void Where_contains_on_parameter_array_with_relational_null_semantics()
        {
            base.Where_contains_on_parameter_array_with_relational_null_semantics();

            AssertSql(
                """
SELECT `e`.`NullableStringA`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringA` IN ('Foo', 'Bar')
""");
        }

        public override void Where_contains_on_parameter_empty_array_with_relational_null_semantics()
        {
            base.Where_contains_on_parameter_empty_array_with_relational_null_semantics();

            AssertSql(
                """
SELECT `e`.`NullableStringA`
FROM `Entities1` AS `e`
WHERE 0 = 1
""");
        }

        public override void Where_contains_on_parameter_array_with_just_null_with_relational_null_semantics()
        {
            base.Where_contains_on_parameter_array_with_just_null_with_relational_null_semantics();

            AssertSql(
                """
SELECT `e`.`NullableStringA`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringA` = NULL
""");
        }

        public override async Task Where_nullable_bool(bool async)
        {
            await base.Where_nullable_bool(async);

            AssertSql(
                $"""
                    SELECT `e`.`Id`
                    FROM `Entities1` AS `e`
                    WHERE `e`.`NullableBoolA` = TRUE
                    """);
        }

        public override async Task Where_nullable_bool_equal_with_constant(bool async)
        {
            await base.Where_nullable_bool_equal_with_constant(async);

            AssertSql(
                $"""
                    SELECT `e`.`Id`
                    FROM `Entities1` AS `e`
                    WHERE `e`.`NullableBoolA` = TRUE
                    """);
        }

        public override async Task Where_nullable_bool_with_null_check(bool async)
        {
            await base.Where_nullable_bool_with_null_check(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` IS NOT NULL AND `e`.`NullableBoolA` = TRUE
""");
        }

        public override void Where_equal_using_relational_null_semantics_with_parameter()
        {
            base.Where_equal_using_relational_null_semantics_with_parameter();

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` IS NULL
""");
        }

        public override void Where_equal_using_relational_null_semantics_complex_with_parameter()
        {
            base.Where_equal_using_relational_null_semantics_complex_with_parameter();

            AssertSql(
                $"""
                    SELECT `e`.`Id`
                    FROM `Entities1` AS `e`
                    WHERE `e`.`NullableBoolA` = `e`.`NullableBoolB`
                    """);
        }

        public override void Where_not_equal_using_relational_null_semantics()
        {
            base.Where_not_equal_using_relational_null_semantics();

            AssertSql(
                $"""
                    SELECT `e`.`Id`
                    FROM `Entities1` AS `e`
                    WHERE `e`.`NullableBoolA` <> `e`.`NullableBoolB`
                    """);
        }

        public override void Where_not_equal_using_relational_null_semantics_with_parameter()
        {
            base.Where_not_equal_using_relational_null_semantics_with_parameter();

            AssertSql(
                $"""
                    SELECT `e`.`Id`
                    FROM `Entities1` AS `e`
                    WHERE `e`.`NullableBoolA` IS NOT NULL
                    """);
        }

        public override void Where_not_equal_using_relational_null_semantics_complex_with_parameter()
        {
            base.Where_not_equal_using_relational_null_semantics_complex_with_parameter();

            AssertSql(
                $"""
                    SELECT `e`.`Id`
                    FROM `Entities1` AS `e`
                    WHERE `e`.`NullableBoolA` <> `e`.`NullableBoolB`
                    """);
        }

        public override async Task Where_comparison_null_constant_and_null_parameter(bool async)
        {
            await base.Where_comparison_null_constant_and_null_parameter(async);

            AssertSql(
                """
@p='True'

SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE @p = TRUE
""",
                //
                """
@p='False'

SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE @p = TRUE
""");
        }

        public override async Task Where_comparison_null_constant_and_nonnull_parameter(bool async)
        {
            await base.Where_comparison_null_constant_and_nonnull_parameter(async);

            AssertSql(
                """
@p='False'

SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE @p = TRUE
""",
                //
                """
@p='True'

SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE @p = TRUE
""");
        }

        public override async Task Where_comparison_nonnull_constant_and_null_parameter(bool async)
        {
            await base.Where_comparison_nonnull_constant_and_null_parameter(async);

            AssertSql(
                """
@p='False'

SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE @p = TRUE
""",
                //
                """
@p='True'

SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE @p = TRUE
""");
        }

        public override async Task Where_comparison_null_semantics_optimization_works_with_complex_predicates(bool async)
        {
            await base.Where_comparison_null_semantics_optimization_works_with_complex_predicates(async);

            AssertSql(
                $"""
                    SELECT `e`.`Id`
                    FROM `Entities1` AS `e`
                    WHERE `e`.`NullableStringA` IS NULL
                    """);
        }

        public override void Switching_null_semantics_produces_different_cache_entry()
        {
            base.Switching_null_semantics_produces_different_cache_entry();

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`NullableBoolB` OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL)
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = `e`.`NullableBoolB`
""");
        }

        public override void Switching_parameter_value_to_null_produces_different_cache_entry()
        {
            base.Switching_parameter_value_to_null_produces_different_cache_entry();

            AssertSql(
                """
@p='True'

SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE @p = TRUE
""",
                //
                """
@p='False'

SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE @p = TRUE
""");
        }

        public override void From_sql_composed_with_relational_null_comparison()
        {
            base.From_sql_composed_with_relational_null_comparison();

            AssertSql(
                """
SELECT `m`.`Id`, `m`.`BoolA`, `m`.`BoolB`, `m`.`BoolC`, `m`.`IntA`, `m`.`IntB`, `m`.`IntC`, `m`.`NullableBoolA`, `m`.`NullableBoolB`, `m`.`NullableBoolC`, `m`.`NullableIntA`, `m`.`NullableIntB`, `m`.`NullableIntC`, `m`.`NullableStringA`, `m`.`NullableStringB`, `m`.`NullableStringC`, `m`.`StringA`, `m`.`StringB`, `m`.`StringC`
FROM (
    SELECT * FROM `Entities1`
) AS `m`
WHERE `m`.`StringA` = `m`.`StringB`
""");
        }

        public override async Task Projecting_nullable_bool_with_coalesce(bool async)
        {
            await base.Projecting_nullable_bool_with_coalesce(async);

            AssertSql(
                $"""
                    SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` IS NULL, FALSE, `e`.`NullableBoolA`) AS `Coalesce`
                    FROM `Entities1` AS `e`
                    """);
        }

        public override async Task Projecting_nullable_bool_with_coalesce_nested(bool async)
        {
            await base.Projecting_nullable_bool_with_coalesce_nested(async);

            AssertSql(
                $"""
                    SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` IS NULL, IIF(`e`.`NullableBoolB` IS NULL, FALSE, `e`.`NullableBoolB`), `e`.`NullableBoolA`) AS `Coalesce`
                    FROM `Entities1` AS `e`
                    """,
                //
                $"""
                    SELECT `e`.`Id`, IIF(`e`.`NullableBoolA` IS NULL, IIF(`e`.`NullableBoolB` IS NULL, FALSE, `e`.`NullableBoolB`), `e`.`NullableBoolA`) AS `Coalesce`
                    FROM `Entities1` AS `e`
                    """);
        }

        public override async Task Null_semantics_applied_when_comparing_function_with_nullable_argument_to_a_nullable_column(bool async)
        {
            await base.Null_semantics_applied_when_comparing_function_with_nullable_argument_to_a_nullable_column(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (INSTR(1, `e`.`NullableStringA`, 'oo', 1) - 1) = `e`.`NullableIntA` OR (`e`.`NullableStringA` IS NULL AND `e`.`NullableIntA` IS NULL)
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (INSTR(1, `e`.`NullableStringA`, 'ar', 1) - 1) = `e`.`NullableIntA` OR (`e`.`NullableStringA` IS NULL AND `e`.`NullableIntA` IS NULL)
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE ((INSTR(1, `e`.`NullableStringA`, 'oo', 1) - 1) <> `e`.`NullableIntB` OR `e`.`NullableStringA` IS NULL OR `e`.`NullableIntB` IS NULL) AND (`e`.`NullableStringA` IS NOT NULL OR `e`.`NullableIntB` IS NOT NULL)
""");
        }

        public override async Task Where_IndexOf_empty(bool async)
        {
            await base.Where_IndexOf_empty(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE IIF(`e`.`NullableStringA` IS NOT NULL, 0, NULL) = `e`.`NullableIntA` OR (IIF(`e`.`NullableStringA` IS NOT NULL, 0, NULL) IS NULL AND `e`.`NullableIntA` IS NULL)
""");
        }

        public override async Task Select_IndexOf(bool async)
        {
            await base.Select_IndexOf(async);

            AssertSql(
"""
SELECT INSTR(1, `e`.`NullableStringA`, 'oo', 1) - 1
FROM `Entities1` AS `e`
ORDER BY `e`.`Id`
""");
        }

        public override async Task Null_semantics_applied_when_comparing_two_functions_with_nullable_arguments(bool async)
        {
            await base.Null_semantics_applied_when_comparing_two_functions_with_nullable_arguments(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (INSTR(1, `e`.`NullableStringA`, 'oo', 1) - 1) = (INSTR(1, `e`.`NullableStringB`, 'ar', 1) - 1) OR (`e`.`NullableStringA` IS NULL AND `e`.`NullableStringB` IS NULL)
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE ((INSTR(1, `e`.`NullableStringA`, 'oo', 1) - 1) <> (INSTR(1, `e`.`NullableStringB`, 'ar', 1) - 1) OR `e`.`NullableStringA` IS NULL OR `e`.`NullableStringB` IS NULL) AND (`e`.`NullableStringA` IS NOT NULL OR `e`.`NullableStringB` IS NOT NULL)
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE ((INSTR(1, `e`.`NullableStringA`, 'oo', 1) - 1) <> (INSTR(1, `e`.`NullableStringA`, 'ar', 1) - 1) OR `e`.`NullableStringA` IS NULL) AND `e`.`NullableStringA` IS NOT NULL
""");
        }

        public override async Task Null_semantics_applied_when_comparing_two_functions_with_multiple_nullable_arguments(bool async)
        {
            await base.Null_semantics_applied_when_comparing_two_functions_with_multiple_nullable_arguments(async);

            AssertSql(
                $"""
                    SELECT `e`.`Id`
                    FROM `Entities1` AS `e`
                    WHERE (REPLACE(`e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`) = `e`.`NullableStringA`) OR (REPLACE(`e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`) IS NULL AND `e`.`NullableStringA` IS NULL)
                    """,
                //
                $"""
                    SELECT `e`.`Id`
                    FROM `Entities1` AS `e`
                    WHERE ((REPLACE(`e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`) <> `e`.`NullableStringA`) OR (REPLACE(`e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`) IS NULL OR `e`.`NullableStringA` IS NULL)) AND (REPLACE(`e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`) IS NOT NULL OR `e`.`NullableStringA` IS NOT NULL)
                    """);
        }

        public override async Task Null_semantics_coalesce(bool async)
        {
            await base.Null_semantics_coalesce(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = IIF(`e`.`NullableBoolB` IS NULL, `e`.`BoolC`, `e`.`NullableBoolB`)
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` = IIF(`e`.`NullableBoolB` IS NULL, `e`.`NullableBoolC`, `e`.`NullableBoolB`) OR (`e`.`NullableBoolA` IS NULL AND `e`.`NullableBoolB` IS NULL AND `e`.`NullableBoolC` IS NULL)
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE IIF(`e`.`NullableBoolB` IS NULL, `e`.`BoolC`, `e`.`NullableBoolB`) <> `e`.`NullableBoolA` OR `e`.`NullableBoolA` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (IIF(`e`.`NullableBoolB` IS NULL, `e`.`NullableBoolC`, `e`.`NullableBoolB`) <> `e`.`NullableBoolA` OR (`e`.`NullableBoolB` IS NULL AND `e`.`NullableBoolC` IS NULL) OR `e`.`NullableBoolA` IS NULL) AND (`e`.`NullableBoolB` IS NOT NULL OR `e`.`NullableBoolC` IS NOT NULL OR `e`.`NullableBoolA` IS NOT NULL)
""");
        }

        public override async Task Null_semantics_conditional(bool async)
        {
            await base.Null_semantics_conditional(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`BoolA` = IIF(`e`.`BoolB` = TRUE, `e`.`NullableBoolB`, `e`.`NullableBoolC`)
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE IIF((`e`.`NullableBoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL), `e`.`BoolB`, `e`.`BoolC`) = `e`.`BoolA`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE IIF(IIF(`e`.`BoolA` = TRUE, IIF((`e`.`NullableBoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolB` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolB` IS NOT NULL), TRUE, FALSE), `e`.`BoolC`) <> `e`.`BoolB`, `e`.`BoolA`, IIF(`e`.`NullableBoolB` = `e`.`NullableBoolC` OR (`e`.`NullableBoolB` IS NULL AND `e`.`NullableBoolC` IS NULL), TRUE, FALSE)) = TRUE
""",
                //
                """
SELECT IIF(IIF(`e`.`BoolA` = TRUE, `e`.`NullableIntA`, `e`.`IntB`) > `e`.`IntC`, TRUE, FALSE)
FROM `Entities1` AS `e`
""");
        }

        public override async Task Null_semantics_function(bool async)
        {
            await base.Null_semantics_function(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (MID(`e`.`NullableStringA`, 0 + 1, `e`.`IntA`) <> `e`.`NullableStringB` OR `e`.`NullableStringA` IS NULL OR `e`.`NullableStringB` IS NULL) AND (`e`.`NullableStringA` IS NOT NULL OR `e`.`NullableStringB` IS NOT NULL)
""");
        }

        public override async Task Null_semantics_join_with_composite_key(bool async)
        {
            await base.Null_semantics_join_with_composite_key(async);

            AssertSql(
"""
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`, `e0`.`Id`, `e0`.`BoolA`, `e0`.`BoolB`, `e0`.`BoolC`, `e0`.`IntA`, `e0`.`IntB`, `e0`.`IntC`, `e0`.`NullableBoolA`, `e0`.`NullableBoolB`, `e0`.`NullableBoolC`, `e0`.`NullableIntA`, `e0`.`NullableIntB`, `e0`.`NullableIntC`, `e0`.`NullableStringA`, `e0`.`NullableStringB`, `e0`.`NullableStringC`, `e0`.`StringA`, `e0`.`StringB`, `e0`.`StringC`
FROM `Entities1` AS `e`
INNER JOIN `Entities2` AS `e0` ON (`e`.`NullableStringA` = `e0`.`NullableStringB` OR (`e`.`NullableStringA` IS NULL AND `e0`.`NullableStringB` IS NULL)) AND IIF((`e`.`NullableStringB` <> `e`.`NullableStringC` OR `e`.`NullableStringB` IS NULL OR `e`.`NullableStringC` IS NULL) AND (`e`.`NullableStringB` IS NOT NULL OR `e`.`NullableStringC` IS NOT NULL), TRUE, FALSE) = IIF(`e0`.`NullableBoolA` IS NULL, `e0`.`BoolC`, `e0`.`NullableBoolA`)
""");
        }

        public override async Task Null_semantics_contains(bool async)
        {
            await base.Null_semantics_contains(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` IN (1, 2)
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` NOT IN (1, 2) OR `e`.`NullableIntA` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` IN (1, 2) OR `e`.`NullableIntA` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` NOT IN (1, 2) AND `e`.`NullableIntA` IS NOT NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` IN (1, 2)
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` NOT IN (1, 2) OR `e`.`NullableIntA` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` IN (1, 2) OR `e`.`NullableIntA` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` NOT IN (1, 2) AND `e`.`NullableIntA` IS NOT NULL
""");
        }

        public override async Task Null_semantics_contains_array_with_no_values(bool async)
        {
            await base.Null_semantics_contains_array_with_no_values(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE 0 = 1
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` IS NULL
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` IS NOT NULL
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE 0 = 1
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` IS NULL
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` IS NOT NULL
""");
        }

        public override async Task Null_semantics_contains_non_nullable_item_with_non_nullable_subquery(bool async)
        {
            await base.Null_semantics_contains_non_nullable_item_with_non_nullable_subquery(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`StringA` IN (
    SELECT `e0`.`StringA`
    FROM `Entities2` AS `e0`
)
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`StringA` NOT IN (
    SELECT `e0`.`StringA`
    FROM `Entities2` AS `e0`
)
""");
        }

        public override async Task Null_semantics_contains_nullable_item_with_non_nullable_subquery(bool async)
        {
            await base.Null_semantics_contains_nullable_item_with_non_nullable_subquery(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringA` IN (
    SELECT `e0`.`StringA`
    FROM `Entities2` AS `e0`
)
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringA` NOT IN (
    SELECT `e0`.`StringA`
    FROM `Entities2` AS `e0`
) OR `e`.`NullableStringA` IS NULL
""");
        }

        public override async Task Null_semantics_contains_non_nullable_item_with_nullable_subquery(bool async)
        {
            await base.Null_semantics_contains_non_nullable_item_with_nullable_subquery(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`StringA` IN (
    SELECT `e0`.`NullableStringA`
    FROM `Entities2` AS `e0`
)
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE IIF(IIF(`e`.`StringA` IN (
            SELECT `e0`.`NullableStringA`
            FROM `Entities2` AS `e0`
        ), TRUE, FALSE) IS NULL, FALSE, IIF(`e`.`StringA` IN (
            SELECT `e0`.`NullableStringA`
            FROM `Entities2` AS `e0`
        ), TRUE, FALSE)) = FALSE
""");
        }

        public override async Task Null_semantics_contains_nullable_item_with_nullable_subquery(bool async)
        {
            await base.Null_semantics_contains_nullable_item_with_nullable_subquery(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE EXISTS (
    SELECT 1
    FROM `Entities2` AS `e0`
    WHERE `e0`.`NullableStringA` = `e`.`NullableStringB` OR (`e0`.`NullableStringA` IS NULL AND `e`.`NullableStringB` IS NULL))
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE NOT EXISTS (
    SELECT 1
    FROM `Entities2` AS `e0`
    WHERE `e0`.`NullableStringA` = `e`.`NullableStringB` OR (`e0`.`NullableStringA` IS NULL AND `e`.`NullableStringB` IS NULL))
""");
        }


        #region Contains with inline collection

        public override async Task Null_semantics_contains_with_non_nullable_item_and_inline_non_nullable_values(bool async)
        {
            await base.Null_semantics_contains_with_non_nullable_item_and_inline_non_nullable_values(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`IntA` IN (1, 2)
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`IntA` NOT IN (1, 2)
""");
        }

        #endregion Contains with inline collection
        
        public override async Task Null_semantics_contains_non_nullable_item_with_values(bool async)
        {
            await base.Null_semantics_contains_non_nullable_item_with_values(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`IntA` IN (1, 2)
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`IntA` NOT IN (1, 2)
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`IntA` IN (1, 2)
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`IntA` NOT IN (1, 2)
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE 0 = 1
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE 0 = 1
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
""");
        }

        public override async Task Null_semantics_with_null_check_simple(bool async)
        {
            await base.Null_semantics_with_null_check_simple(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` IS NOT NULL AND `e`.`NullableIntA` = `e`.`NullableIntB`
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` IS NOT NULL AND (`e`.`NullableIntA` <> `e`.`NullableIntB` OR `e`.`NullableIntB` IS NULL)
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` IS NOT NULL AND `e`.`NullableIntA` = `e`.`IntC`
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` IS NOT NULL AND `e`.`NullableIntB` IS NOT NULL AND `e`.`NullableIntA` = `e`.`NullableIntB`
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` IS NOT NULL AND `e`.`NullableIntB` IS NOT NULL AND `e`.`NullableIntA` <> `e`.`NullableIntB`
""");
        }

        public override async Task Null_semantics_with_null_check_complex(bool async)
        {
            await base.Null_semantics_with_null_check_complex(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` IS NOT NULL AND (`e`.`NullableIntC` <> `e`.`NullableIntA` OR `e`.`NullableIntC` IS NULL OR (`e`.`NullableIntB` IS NOT NULL AND `e`.`NullableIntA` <> `e`.`NullableIntB`))
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` IS NOT NULL AND (`e`.`NullableIntC` <> `e`.`NullableIntA` OR `e`.`NullableIntC` IS NULL OR `e`.`NullableIntA` <> `e`.`NullableIntB` OR `e`.`NullableIntB` IS NULL)
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableIntA` IS NOT NULL OR `e`.`NullableIntB` IS NOT NULL) AND (`e`.`NullableIntA` = `e`.`NullableIntC` OR (`e`.`NullableIntA` IS NULL AND `e`.`NullableIntC` IS NULL))
""");
        }

        public override async Task Null_semantics_with_null_check_complex2(bool async)
        {
            await base.Null_semantics_with_null_check_complex2(async);

            AssertSql(
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableBoolA` IS NOT NULL AND `e`.`NullableBoolB` IS NOT NULL AND (`e`.`NullableBoolB` <> `e`.`NullableBoolA` OR `e`.`NullableBoolC` IS NOT NULL) AND (`e`.`NullableBoolC` <> `e`.`NullableBoolB` OR `e`.`NullableBoolC` IS NULL)) OR `e`.`NullableBoolC` <> `e`.`BoolB` OR `e`.`NullableBoolC` IS NULL
""",
                //
                """
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableBoolA` IS NOT NULL AND `e`.`NullableBoolB` IS NOT NULL AND (`e`.`NullableBoolB` <> `e`.`NullableBoolA` OR `e`.`NullableBoolC` IS NOT NULL) AND (`e`.`NullableBoolC` <> `e`.`NullableBoolB` OR `e`.`NullableBoolC` IS NULL)) OR `e`.`NullableBoolB` <> `e`.`BoolB` OR `e`.`NullableBoolB` IS NULL
""");
        }

        public override async Task IsNull_on_complex_expression(bool async)
        {
            await base.IsNull_on_complex_expression(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` IS NOT NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` IS NULL OR `e`.`NullableIntB` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` IS NULL AND `e`.`NullableIntB` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` IS NOT NULL OR `e`.`NullableIntB` IS NOT NULL
""");
        }

        public override async Task Coalesce_not_equal(bool async)
        {
            await base.Coalesce_not_equal(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE IIF(`e`.`NullableIntA` IS NULL, 0, `e`.`NullableIntA`) <> 0
""");
        }

        public override async Task Negated_order_comparison_on_non_nullable_arguments_gets_optimized(bool async)
        {
            await base.Negated_order_comparison_on_non_nullable_arguments_gets_optimized(async);

            AssertSql(
                """
@i='1'

SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`IntA` <= @i
""",
                //
                """
@i='1'

SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`IntA` < @i
""",
                //
                """
@i='1'

SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`IntA` >= @i
""",
                //
                """
@i='1'

SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`IntA` > @i
""");
        }

        public override async Task Negated_order_comparison_on_nullable_arguments_doesnt_get_optimized(bool async)
        {
            await base.Negated_order_comparison_on_nullable_arguments_doesnt_get_optimized(async);

            AssertSql(
                """
@i='1' (Nullable = true)

SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE IIF(`e`.`NullableIntA` > @i, FALSE, TRUE) = TRUE
""",
                //
                """
@i='1' (Nullable = true)

SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE IIF(`e`.`NullableIntA` >= @i, FALSE, TRUE) = TRUE
""",
                //
                """
@i='1' (Nullable = true)

SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE IIF(`e`.`NullableIntA` < @i, FALSE, TRUE) = TRUE
""",
                //
                """
@i='1' (Nullable = true)

SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE IIF(`e`.`NullableIntA` <= @i, FALSE, TRUE) = TRUE
""");
        }

        public override async Task Nullable_column_info_propagates_inside_binary_AndAlso(bool async)
        {
            await base.Nullable_column_info_propagates_inside_binary_AndAlso(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringA` IS NOT NULL AND `e`.`NullableStringB` IS NOT NULL AND `e`.`NullableStringA` <> `e`.`NullableStringB`
""");
        }

        public override async Task Nullable_column_info_doesnt_propagate_inside_binary_OrElse(bool async)
        {
            await base.Nullable_column_info_doesnt_propagate_inside_binary_OrElse(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableStringA` IS NOT NULL OR `e`.`NullableStringB` IS NOT NULL) AND (`e`.`NullableStringA` <> `e`.`NullableStringB` OR `e`.`NullableStringA` IS NULL OR `e`.`NullableStringB` IS NULL) AND (`e`.`NullableStringA` IS NOT NULL OR `e`.`NullableStringB` IS NOT NULL)
""");
        }

        public override async Task Nullable_column_info_propagates_inside_binary_OrElse_when_info_is_duplicated(bool async)
        {
            await base.Nullable_column_info_propagates_inside_binary_OrElse_when_info_is_duplicated(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE ((`e`.`NullableStringA` IS NOT NULL AND `e`.`NullableStringB` IS NOT NULL) OR `e`.`NullableStringA` IS NOT NULL) AND (`e`.`NullableStringA` <> `e`.`NullableStringB` OR `e`.`NullableStringB` IS NULL)
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE ((`e`.`NullableStringA` IS NOT NULL AND `e`.`NullableStringB` IS NOT NULL) OR (`e`.`NullableStringB` IS NOT NULL AND `e`.`NullableStringA` IS NOT NULL)) AND `e`.`NullableStringA` <> `e`.`NullableStringB`
""");
        }

        public override async Task Nullable_column_info_propagates_inside_conditional(bool async)
        {
            await base.Nullable_column_info_propagates_inside_conditional(async);

            AssertSql(
"""
SELECT IIF(`e`.`NullableStringA` IS NOT NULL, IIF(`e`.`NullableStringA` <> `e`.`StringA`, TRUE, FALSE), `e`.`BoolA`)
FROM `Entities1` AS `e`
""");
        }

        public override async Task Nullable_column_info_doesnt_propagate_between_projections(bool async)
        {
            await base.Nullable_column_info_doesnt_propagate_between_projections(async);

            AssertSql(
"""
SELECT IIF(`e`.`NullableStringA` IS NOT NULL, TRUE, FALSE) AS `Foo`, IIF(`e`.`NullableStringA` <> `e`.`StringA` OR `e`.`NullableStringA` IS NULL, TRUE, FALSE) AS `Bar`
FROM `Entities1` AS `e`
""");
        }

        public override async Task Nullable_column_info_doesnt_propagate_between_different_parts_of_select(bool async)
        {
            await base.Nullable_column_info_doesnt_propagate_between_different_parts_of_select(async);

            AssertSql(
                """
SELECT [e].[Id]
FROM [Entities1] AS [e]
INNER JOIN [Entities1] AS [e0] ON [e].[NullableBoolA] IS NULL
WHERE ([e].[NullableBoolA] <> [e0].[NullableBoolB] OR [e].[NullableBoolA] IS NULL OR [e0].[NullableBoolB] IS NULL) AND ([e].[NullableBoolA] IS NOT NULL OR [e0].[NullableBoolB] IS NOT NULL)
""");
        }

        public override async Task Nullable_column_info_propagation_complex(bool async)
        {
            await base.Nullable_column_info_propagation_complex(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringA` IS NOT NULL AND `e`.`NullableBoolB` IS NOT NULL AND `e`.`NullableStringC` IS NOT NULL AND (`e`.`NullableBoolB` <> `e`.`NullableBoolC` OR `e`.`NullableBoolC` IS NULL)
""");
        }

        public override async Task Empty_subquery_with_contains_returns_false(bool async)
        {
            await base.Empty_subquery_with_contains_returns_false(async);

            AssertSql(
"""
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE 0 = 1
""");
        }

        public override async Task Empty_subquery_with_contains_negated_returns_true(bool async)
        {
            await base.Empty_subquery_with_contains_negated_returns_true(async);

            AssertSql(
"""
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
""");
        }

        public override async Task Nullable_string_FirstOrDefault_compared_to_nullable_string_LastOrDefault(bool async)
        {
            await base.Nullable_string_FirstOrDefault_compared_to_nullable_string_LastOrDefault(async);

            AssertSql(
                """
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE MID(`e`.`NullableStringA`, 1, 1) = MID(`e`.`NullableStringB`, IIF(IIF(LEN(`e`.`NullableStringB`) = 0, 1, LEN(`e`.`NullableStringB`)) IS NULL, 0, IIF(LEN(`e`.`NullableStringB`) = 0, 1, LEN(`e`.`NullableStringB`))), 1) OR (`e`.`NullableStringA` IS NULL AND `e`.`NullableStringB` IS NULL)
""");
        }

        public override async Task Null_semantics_applied_to_CompareTo_equality(bool async)
        {
            await base.Null_semantics_applied_to_CompareTo_equality(async);

            AssertSql(
"""
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringA` = `e`.`NullableStringB` OR (`e`.`NullableStringA` IS NULL AND `e`.`NullableStringB` IS NULL)
""",
//
"""
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringA` = `e`.`NullableStringB` OR (`e`.`NullableStringA` IS NULL AND `e`.`NullableStringB` IS NULL)
""",
//
"""
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableStringA` <> `e`.`NullableStringB` OR `e`.`NullableStringA` IS NULL OR `e`.`NullableStringB` IS NULL) AND (`e`.`NullableStringA` IS NOT NULL OR `e`.`NullableStringB` IS NOT NULL)
""",
//
"""
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableStringA` <> `e`.`NullableStringB` OR `e`.`NullableStringA` IS NULL OR `e`.`NullableStringB` IS NULL) AND (`e`.`NullableStringA` IS NOT NULL OR `e`.`NullableStringB` IS NOT NULL)
""");
        }

        public override async Task Nested_CompareTo_optimized(bool async)
        {
            await base.Nested_CompareTo_optimized(async);

            AssertSql(
"""
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringA` = `e`.`NullableStringB` OR (`e`.`NullableStringA` IS NULL AND `e`.`NullableStringB` IS NULL)
""",
//
"""
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringA` = `e`.`NullableStringB` OR (`e`.`NullableStringA` IS NULL AND `e`.`NullableStringB` IS NULL)
""",
//
"""
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableStringA` <> `e`.`NullableStringB` OR `e`.`NullableStringA` IS NULL OR `e`.`NullableStringB` IS NULL) AND (`e`.`NullableStringA` IS NOT NULL OR `e`.`NullableStringB` IS NOT NULL)
""",
//
"""
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableStringA` <> `e`.`NullableStringB` OR `e`.`NullableStringA` IS NULL OR `e`.`NullableStringB` IS NULL) AND (`e`.`NullableStringA` IS NOT NULL OR `e`.`NullableStringB` IS NOT NULL)
""");
        }

        public override async Task False_compared_to_negated_is_null(bool async)
        {
            await base.False_compared_to_negated_is_null(async);

            AssertSql(
"""
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringA` IS NULL
""");
        }

        public override async Task CaseWhen_equal_to_second_filter(bool async)
        {
            await base.CaseWhen_equal_to_second_filter(async);

            AssertSql(
                """
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE IIF(`e`.`StringA` = 'Foo', 3, IIF(`e`.`StringB` = 'Foo', 2, IIF(`e`.`StringC` = 'Foo', 3, NULL))) = 2
""");
        }

        public override async Task CaseWhen_equal_to_first_or_third_filter(bool async)
        {
            await base.CaseWhen_equal_to_first_or_third_filter(async);

            AssertSql(
                """
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE IIF(`e`.`StringA` = 'Foo', 3, IIF(`e`.`StringB` = 'Foo', 2, IIF(`e`.`StringC` = 'Foo', 3, NULL))) = 3
""");
        }

        public override async Task CaseWhen_equal_to_second_select(bool async)
        {
            await base.CaseWhen_equal_to_second_select(async);

            AssertSql(
                """
SELECT IIF(IIF(`e`.`StringA` = 'Foo', 3, IIF(`e`.`StringB` = 'Foo', 2, IIF(`e`.`StringC` = 'Foo', 3, NULL))) = 2 AND IIF(`e`.`StringA` = 'Foo', 3, IIF(`e`.`StringB` = 'Foo', 2, IIF(`e`.`StringC` = 'Foo', 3, NULL))) IS NOT NULL, TRUE, FALSE)
FROM `Entities1` AS `e`
ORDER BY `e`.`Id`
""");
        }

        public override async Task CaseWhen_equal_to_first_or_third_select(bool async)
        {
            await base.CaseWhen_equal_to_first_or_third_select(async);

            AssertSql(
                """
SELECT IIF(IIF(`e`.`StringA` = 'Foo', 3, IIF(`e`.`StringB` = 'Foo', 2, IIF(`e`.`StringC` = 'Foo', 3, NULL))) = 3 AND IIF(`e`.`StringA` = 'Foo', 3, IIF(`e`.`StringB` = 'Foo', 2, IIF(`e`.`StringC` = 'Foo', 3, NULL))) IS NOT NULL, TRUE, FALSE)
FROM `Entities1` AS `e`
ORDER BY `e`.`Id`
""");
        }

        public override async Task CaseOpWhen_projection(bool async)
        {
            await base.CaseOpWhen_projection(async);

            AssertSql(
                """
SELECT IIF(IIF(`e`.`StringA` = 'Foo', TRUE, FALSE) = TRUE, 3, IIF(IIF(`e`.`StringA` = 'Foo', TRUE, FALSE) = FALSE, 2, NULL))
FROM `Entities1` AS `e`
ORDER BY `e`.`Id`
""");
        }

        public override async Task CaseOpWhen_predicate(bool async)
        {
            await base.CaseOpWhen_predicate(async);

            AssertSql(
                """
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE IIF(IIF(`e`.`StringA` = 'Foo', TRUE, FALSE) = TRUE, 3, IIF(IIF(`e`.`StringA` = 'Foo', TRUE, FALSE) = FALSE, 2, NULL)) = 2
""");
        }

        public override async Task Multiple_non_equality_comparisons_with_null_in_the_middle(bool async)
        {
            await base.Multiple_non_equality_comparisons_with_null_in_the_middle(async);

            AssertSql(
"""
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` NOT IN (1, 2) AND `e`.`NullableIntA` IS NOT NULL
""");
        }

        public override async Task Multiple_non_equality_comparisons_including_null_comparison_work_for_relational_null_semantics(
            bool async)
        {
            await base.Multiple_non_equality_comparisons_including_null_comparison_work_for_relational_null_semantics(async);

            AssertSql(
"""
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` <> 1 AND `e`.`NullableIntA` IS NOT NULL
""");
        }

        public override async Task Multiple_non_equality_comparisons_without_null_comparison_work_for_relational_null_semantics(bool async)
        {
            await base.Multiple_non_equality_comparisons_without_null_comparison_work_for_relational_null_semantics(async);

            AssertSql(
"""
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` NOT IN (1, 2)
""");
        }

        public override async Task Multiple_equality_comparisons_including_null_comparison_work_for_relational_null_semantics(bool async)
        {
            await base.Multiple_equality_comparisons_including_null_comparison_work_for_relational_null_semantics(async);

            AssertSql(
"""
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` = 1 OR `e`.`NullableIntA` IS NULL
""");
        }
        
        public override async Task Bool_equal_nullable_bool_HasValue(bool async)
        {
            await base.Bool_equal_nullable_bool_HasValue(async);

            AssertSql(
                """
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` IS NOT NULL
""",
                //
                """
@prm='False'

SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE @prm = IIF(`e`.`NullableBoolA` IS NOT NULL, TRUE, FALSE)
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE `e`.`BoolB` = IIF(`e`.`NullableBoolA` IS NOT NULL, TRUE, FALSE)
""");
        }

        public override async Task Bool_equal_nullable_bool_compared_to_null(bool async)
        {
            await base.Bool_equal_nullable_bool_compared_to_null(async);

            AssertSql(
                """
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` IS NULL
""",
                //
                """
@prm='False'

SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE @prm = IIF(`e`.`NullableBoolA` IS NOT NULL, TRUE, FALSE)
""");
        }

        public override async Task Bool_not_equal_nullable_bool_HasValue(bool async)
        {
            await base.Bool_not_equal_nullable_bool_HasValue(async);

            AssertSql(
                """
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` IS NULL
""",
                //
                """
@prm='False'

SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE @prm <> IIF(`e`.`NullableBoolA` IS NOT NULL, TRUE, FALSE)
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE `e`.`BoolB` <> IIF(`e`.`NullableBoolA` IS NOT NULL, TRUE, FALSE)
""");
        }

        public override async Task Bool_not_equal_nullable_int_HasValue(bool async)
        {
            await base.Bool_not_equal_nullable_int_HasValue(async);

            AssertSql(
                """
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` IS NULL
""",
                //
                """
@prm='False'

SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE @prm <> IIF(`e`.`NullableIntA` IS NOT NULL, TRUE, FALSE)
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE `e`.`BoolB` <> IIF(`e`.`NullableIntA` IS NOT NULL, TRUE, FALSE)
""");
        }

        public override async Task Bool_not_equal_nullable_bool_compared_to_null(bool async)
        {
            await base.Bool_not_equal_nullable_bool_compared_to_null(async);

            AssertSql(
                """
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` IS NOT NULL
""",
                //
                """
@prm='False'

SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE @prm <> IIF(`e`.`NullableBoolA` IS NOT NULL, TRUE, FALSE)
""");
        }

        public override async Task Bool_logical_operation_with_nullable_bool_HasValue(bool async)
        {
            await base.Bool_logical_operation_with_nullable_bool_HasValue(async);

            AssertSql(
                """
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE 0 = 1
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE `e`.`BoolB` = TRUE OR `e`.`NullableBoolA` IS NOT NULL
""");
        }

        public override async Task Comparison_compared_to_null_check_on_bool(bool async)
        {
            await base.Comparison_compared_to_null_check_on_bool(async);

            AssertSql(
                """
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE (CBOOL(`e`.`IntA` BXOR `e`.`IntB`) BXOR TRUE) <> IIF(`e`.`NullableBoolA` IS NOT NULL, TRUE, FALSE)
""",
                //
                """
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE CBOOL(`e`.`IntA` BXOR `e`.`IntB`) = IIF(`e`.`NullableBoolA` IS NOT NULL, TRUE, FALSE)
""");
        }

        public override async Task Is_null_on_column_followed_by_OrElse_optimizes_nullability_simple(bool async)
        {
            await base.Is_null_on_column_followed_by_OrElse_optimizes_nullability_simple(async);

            AssertSql(
"""
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringA` IS NOT NULL AND `e`.`NullableStringA` = 'Foo'
""");
        }

        public override async Task Is_null_on_column_followed_by_OrElse_optimizes_nullability_negative(bool async)
        {
            await base.Is_null_on_column_followed_by_OrElse_optimizes_nullability_negative(async);

            AssertSql(
                """
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringA` IS NOT NULL OR `e`.`NullableStringA` = 'Foo'
""");
        }

        public override async Task Is_null_on_column_followed_by_OrElse_optimizes_nullability_nested(bool async)
        {
            await base.Is_null_on_column_followed_by_OrElse_optimizes_nullability_nested(async);

            AssertSql(
"""
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringA` IS NULL OR `e`.`NullableStringB` IS NULL OR `e`.`NullableStringA` <> `e`.`NullableStringB`
""");
        }

        public override async Task Is_null_on_column_followed_by_OrElse_optimizes_nullability_intersection(bool async)
        {
            await base.Is_null_on_column_followed_by_OrElse_optimizes_nullability_intersection(async);

            AssertSql(
"""
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE (`e`.`NullableStringA` IS NULL AND (`e`.`StringA` = 'Foo' OR `e`.`NullableStringA` IS NULL OR `e`.`NullableStringB` IS NULL)) OR `e`.`NullableStringA` <> `e`.`NullableStringB` OR `e`.`NullableStringB` IS NULL
""");
        }

        public override async Task Is_null_on_column_followed_by_OrElse_optimizes_nullability_conditional(bool async)
        {
            await base.Is_null_on_column_followed_by_OrElse_optimizes_nullability_conditional(async);

            // issue #25977
            AssertSql(
"""
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE IIF(`e`.`NullableStringA` IS NULL, IIF((`e`.`NullableStringA` <> `e`.`NullableStringB` OR `e`.`NullableStringA` IS NULL OR `e`.`NullableStringB` IS NULL) AND (`e`.`NullableStringA` IS NOT NULL OR `e`.`NullableStringB` IS NOT NULL), TRUE, FALSE), IIF((`e`.`NullableStringA` <> `e`.`NullableStringC` OR `e`.`NullableStringA` IS NULL OR `e`.`NullableStringC` IS NULL) AND (`e`.`NullableStringA` IS NOT NULL OR `e`.`NullableStringC` IS NOT NULL), TRUE, FALSE)) = TRUE
""");
        }

        public override async Task Is_null_on_column_followed_by_OrElse_optimizes_nullability_conditional_multiple(bool async)
        {
            await base.Is_null_on_column_followed_by_OrElse_optimizes_nullability_conditional_multiple(async);

            AssertSql(
"""
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE IIF(`e`.`NullableStringA` IS NULL OR `e`.`NullableStringB` IS NULL, IIF(`e`.`NullableStringA` = `e`.`NullableStringB` OR (`e`.`NullableStringA` IS NULL AND `e`.`NullableStringB` IS NULL), TRUE, FALSE), IIF((`e`.`NullableStringA` <> `e`.`NullableStringB` OR `e`.`NullableStringA` IS NULL OR `e`.`NullableStringB` IS NULL) AND (`e`.`NullableStringA` IS NOT NULL OR `e`.`NullableStringB` IS NOT NULL), TRUE, FALSE)) = TRUE
""");
        }

        public override async Task Is_null_on_column_followed_by_OrElse_optimizes_nullability_conditional_negative(bool async)
        {
            await base.Is_null_on_column_followed_by_OrElse_optimizes_nullability_conditional_negative(async);

            AssertSql(
"""
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE IIF((`e`.`NullableStringA` IS NULL OR `e`.`NullableStringB` IS NULL) AND `e`.`NullableBoolC` IS NULL, IIF(`e`.`NullableStringA` = `e`.`NullableStringB` OR (`e`.`NullableStringA` IS NULL AND `e`.`NullableStringB` IS NULL), TRUE, FALSE), IIF((`e`.`NullableStringA` <> `e`.`NullableStringB` OR `e`.`NullableStringA` IS NULL OR `e`.`NullableStringB` IS NULL) AND (`e`.`NullableStringA` IS NOT NULL OR `e`.`NullableStringB` IS NOT NULL), TRUE, FALSE)) = TRUE
""");
        }

        public override async Task Is_null_on_column_followed_by_OrElse_optimizes_nullability_conditional_with_setup(bool async)
        {
            await base.Is_null_on_column_followed_by_OrElse_optimizes_nullability_conditional_with_setup(async);

            // issue #25977
            AssertSql(
"""
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE `e`.`NullableBoolA` IS NULL OR IIF(`e`.`NullableBoolB` IS NULL, IIF(`e`.`NullableBoolB` <> `e`.`NullableBoolA` OR `e`.`NullableBoolB` IS NULL, TRUE, FALSE), IIF(`e`.`NullableBoolA` <> `e`.`NullableBoolB` OR `e`.`NullableBoolB` IS NULL, TRUE, FALSE)) = TRUE
""");
        }

        public override async Task Is_null_on_column_followed_by_OrElse_optimizes_nullability_conditional_nested(bool async)
        {
            await base.Is_null_on_column_followed_by_OrElse_optimizes_nullability_conditional_nested(async);

            // issue #25977
            AssertSql(
                """
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE IIF(`e`.`NullableBoolA` IS NULL, (`e`.`BoolA` BXOR `e`.`BoolB`) BXOR TRUE, IIF(`e`.`NullableBoolC` IS NULL, IIF((`e`.`NullableBoolA` <> `e`.`NullableBoolC` OR `e`.`NullableBoolA` IS NULL OR `e`.`NullableBoolC` IS NULL) AND (`e`.`NullableBoolA` IS NOT NULL OR `e`.`NullableBoolC` IS NOT NULL), TRUE, FALSE), IIF((`e`.`NullableBoolC` <> `e`.`NullableBoolA` OR `e`.`NullableBoolC` IS NULL OR `e`.`NullableBoolA` IS NULL) AND (`e`.`NullableBoolC` IS NOT NULL OR `e`.`NullableBoolA` IS NOT NULL), TRUE, FALSE))) = TRUE
""");
        }

        public override async Task Sum_function_is_always_considered_non_nullable(bool async)
        {
            await base.Sum_function_is_always_considered_non_nullable(async);

            AssertSql(
"""
SELECT `e`.`NullableIntA` AS `Key`, IIF(IIF(SUM(`e`.`IntA`) IS NULL, 0, SUM(`e`.`IntA`)) <> `e`.`NullableIntA` OR `e`.`NullableIntA` IS NULL, TRUE, FALSE) AS `Sum`
FROM `Entities1` AS `e`
GROUP BY `e`.`NullableIntA`
""");
        }

        public override async Task Nullability_is_computed_correctly_for_chained_coalesce(bool async)
        {
            await base.Nullability_is_computed_correctly_for_chained_coalesce(async);

            AssertSql(
"""
SELECT `e`.`Id`, `e`.`BoolA`, `e`.`BoolB`, `e`.`BoolC`, `e`.`IntA`, `e`.`IntB`, `e`.`IntC`, `e`.`NullableBoolA`, `e`.`NullableBoolB`, `e`.`NullableBoolC`, `e`.`NullableIntA`, `e`.`NullableIntB`, `e`.`NullableIntC`, `e`.`NullableStringA`, `e`.`NullableStringB`, `e`.`NullableStringC`, `e`.`StringA`, `e`.`StringB`, `e`.`StringC`
FROM `Entities1` AS `e`
WHERE IIF(`e`.`NullableIntA` IS NULL, IIF(`e`.`NullableIntB` IS NULL, `e`.`IntC`, `e`.`NullableIntB`), `e`.`NullableIntA`) <> `e`.`NullableIntC` OR `e`.`NullableIntC` IS NULL
""");
        }

        public override async Task Nullability_check_is_computed_correctly_for_chained_coalesce(bool async)
        {
            await base.Nullability_check_is_computed_correctly_for_chained_coalesce(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` IS NULL AND `e`.`NullableIntB` IS NULL AND `e`.`NullableIntC` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableIntA` IS NOT NULL OR `e`.`NullableIntB` IS NOT NULL OR `e`.`NullableIntC` IS NOT NULL
""");
        }

        public override async Task Coalesce_on_self_gets_simplified(bool async)
        {
            await base.Coalesce_on_self_gets_simplified(async);

            AssertSql(
"""
SELECT `e`.`NullableStringA`
FROM `Entities1` AS `e`
""");
        }

        public override async Task Coalesce_deeply_nested(bool async)
        {
            await base.Coalesce_deeply_nested(async);

            AssertSql(
"""
SELECT IIF(`e`.`NullableIntA` IS NULL, IIF(`e`.`NullableIntB` IS NULL, IIF(`e0`.`NullableIntC` IS NULL, IIF(`e0`.`NullableIntB` IS NULL, IIF(`e`.`NullableIntC` IS NULL, `e0`.`NullableIntA`, `e`.`NullableIntC`), `e0`.`NullableIntB`), `e0`.`NullableIntC`), `e`.`NullableIntB`), `e`.`NullableIntA`)
FROM `Entities1` AS `e`
INNER JOIN `Entities2` AS `e0` ON `e`.`Id` = `e0`.`Id`
""");
        }

        public override async Task Like(bool async)
        {
            await base.Like(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`StringA` LIKE `e`.`StringB`
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`StringA` LIKE `e`.`NullableStringB`
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringA` LIKE `e`.`StringB`
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringA` LIKE `e`.`NullableStringB`
""");
        }

        public override async Task Like_negated(bool async)
        {
            await base.Like_negated(async);

            AssertSql(
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`StringA` NOT LIKE `e`.`StringB`
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`StringA` NOT LIKE `e`.`NullableStringB` OR `e`.`NullableStringB` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringA` NOT LIKE `e`.`StringB` OR `e`.`NullableStringA` IS NULL
""",
//
"""
SELECT `e`.`Id`
FROM `Entities1` AS `e`
WHERE `e`.`NullableStringA` NOT LIKE `e`.`NullableStringB` OR `e`.`NullableStringA` IS NULL OR `e`.`NullableStringB` IS NULL
""");
        }

        public override async Task Like_with_escape_char(bool async)
        {
            await Assert.ThrowsAnyAsync<DbException>(
                async () => await base.Like_with_escape_char(async));
        }

        private void AssertSql(params string[] expected)
            => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);

        protected override void ClearLog()
            => Fixture.TestSqlLoggerFactory.Clear();

        protected override NullSemanticsContext CreateContext(bool useRelationalNulls = false)
        {
            var options = new DbContextOptionsBuilder(Fixture.CreateOptions());
            if (useRelationalNulls)
            {
                new JetDbContextOptionsBuilder(options).UseRelationalNulls();
            }

            var context = new NullSemanticsContext(options.Options);

            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            return context;
        }
    }
}
