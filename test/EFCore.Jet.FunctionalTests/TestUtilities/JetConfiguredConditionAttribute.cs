// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.TestUtilities.Xunit;

namespace EntityFrameworkCore.Jet.FunctionalTests.TestUtilities
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
    public sealed class JetConfiguredConditionAttribute : Attribute, ITestCondition
    {
        public ValueTask<bool> IsMetAsync()
            => new ValueTask<bool>(
                TestEnvironment.IsConfigured /*&& (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || !TestEnvironment.IsLocalDb)*/);

        public string SkipReason => /*TestEnvironment.IsLocalDb
            ? "LocalDb is not accessible on this platform. An external SQL Server must be configured."
            : */"No test database has been configured.";
    }
}
