﻿using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace VDS.RDF.XunitExtensions
{
    public class SkippableFactDiscoverer : IXunitTestCaseDiscoverer
    {
        readonly IMessageSink diagnosticMessageSink;

        public SkippableFactDiscoverer(IMessageSink diagnosticMessageSink)
        {
            this.diagnosticMessageSink = diagnosticMessageSink;
        }

        public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            yield return new SkippableFactTestCase(diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod);
        }
    }
}
