// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using EntityFrameworkCore.Jet.Data;
using System.Data.OleDb;
using Microsoft.EntityFrameworkCore.TestUtilities.Xunit;

namespace EntityFrameworkCore.Jet.FunctionalTests.TestUtilities
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class JetConditionAttribute(JetCondition conditions) : Attribute, ITestCondition
    {
        public JetCondition Conditions { get; set; } = conditions;

        public ValueTask<bool> IsMetAsync()
        {
            var isMet = true;

            if (Conditions.HasFlag(JetCondition.IsNotCI))
            {
                isMet &= !TestEnvironment.IsCI;
            }

            return ValueTask.FromResult(isMet);
        }

        public string SkipReason =>
            // ReSharper disable once UseStringInterpolation
            string.Format(
                "The test Jet does not meet these conditions: '{0}'",
                string.Join(
                    ", ", Enum.GetValues(typeof(JetCondition))
                        .Cast<Enum>()
                        .Where(f => Conditions.HasFlag(f))
                        .Select(f => Enum.GetName(typeof(JetCondition), f))));
    }
}
