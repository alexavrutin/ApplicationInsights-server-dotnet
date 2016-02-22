﻿namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using Fr8.ApplicationInsights.DependencyCollector.Implementation;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DependencyCollectorEventSourceTest
    {
        [TestMethod]
        public void MethodsAreImplementedConsistentlyWithTheirAttributes()
        {
            EventSourceTest.MethodsAreImplementedConsistentlyWithTheirAttributes(DependencyCollectorEventSource.Log);
        }
    }
}
