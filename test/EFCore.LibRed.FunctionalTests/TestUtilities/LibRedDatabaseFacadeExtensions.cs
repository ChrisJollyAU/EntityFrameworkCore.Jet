// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EntityFrameworkCore.LibRed.FunctionalTests.TestUtilities
{
    public static class LibRedDatabaseFacadeExtensions
    {
        public static void EnsureClean(this DatabaseFacade databaseFacade, bool createTables = true)
            => databaseFacade.CreateExecutionStrategy()
                .Execute(databaseFacade, database => new LibRedDatabaseCleaner().Clean(database, createTables));
    }
}
