﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ReSharper disable InconsistentNaming

using Microsoft.EntityFrameworkCore.Query;

namespace EntityFrameworkCore.Jet.FunctionalTests.Query;

public class TPCInheritanceQueryJetTest : TPCInheritanceQueryJetTestBase<TPCInheritanceQueryJetFixture>
{
    public TPCInheritanceQueryJetTest(TPCInheritanceQueryJetFixture fixture)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
    }
}
