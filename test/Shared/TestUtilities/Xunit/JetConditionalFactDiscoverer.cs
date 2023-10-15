﻿using Microsoft.EntityFrameworkCore.TestUtilities.Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace EntityFrameworkCore.Jet.FunctionalTests.TestUtilities.Xunit;

public class JetConditionalFactDiscoverer : ConditionalFactDiscoverer
{
    public JetConditionalFactDiscoverer(IMessageSink messageSink)
        : base(messageSink)
    {
    }

    protected override IXunitTestCase CreateTestCase(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        ITestMethod testMethod,
        IAttributeInfo factAttribute)
        => new JetConditionalFactTestCase(
            DiagnosticMessageSink,
            discoveryOptions.MethodDisplayOrDefault(),
            discoveryOptions.MethodDisplayOptionsOrDefault(),
            testMethod);
}