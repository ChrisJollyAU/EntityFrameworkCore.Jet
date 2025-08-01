// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using EntityFrameworkCore.Jet.FunctionalTests.TestUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;
using System.Threading.Tasks;
using Xunit;

namespace EntityFrameworkCore.Jet.FunctionalTests.Query;

public class ToSqlQueryJetTest(NonSharedFixture fixture) : ToSqlQueryTestBase(fixture)
{
    protected override ITestStoreFactory TestStoreFactory
        => JetTestStoreFactory.Instance;

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override async Task Entity_type_with_navigation_mapped_to_SqlQuery(bool async)
    {
        await base.Entity_type_with_navigation_mapped_to_SqlQuery(async);

        AssertSql(
"""
SELECT [a].[Id], [a].[Name], [a].[PostStatAuthorId], [m].[Count] AS [PostCount]
FROM [Authors] AS [a]
LEFT JOIN (
    SELECT * FROM PostStats
) AS [m] ON [a].[PostStatAuthorId] = [m].[AuthorId]
""");
    }

    private void AssertSql(params string[] expected)
        => TestSqlLoggerFactory.AssertBaseline(expected);
}
