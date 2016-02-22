﻿namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Fr8.ApplicationInsights.DependencyCollector.Implementation;
    [TestClass]
    public class ApplicationInsightsUrlFilterTests
    {
        [TestMethod]
        public void IsApplicationInsightsUrlReturnsTrueForTelemetryServiceEndpoint()
        {
            using (TelemetryConfiguration configuration = this.CreateStubTelemetryConfiguration())
            {               
                string url = "https://dc.services.visualstudio.com/v2/track";
                ApplicationInsightsUrlFilter urlFilter = new ApplicationInsightsUrlFilter(configuration);
                Assert.IsTrue(urlFilter.IsApplicationInsightsUrl(url));
            }
        }

        [TestMethod]
        public void IsApplicationInsightsUrlReturnsTrueForTelemetryChannelEndpointAddress()
        {
            using (TelemetryConfiguration configuration = this.CreateStubTelemetryConfiguration())
            {    
                string url = "https://endpointaddress";
                ApplicationInsightsUrlFilter urlFilter = new ApplicationInsightsUrlFilter(configuration);
                Assert.IsTrue(urlFilter.IsApplicationInsightsUrl(url));
            }
        }

        [TestMethod]
        public void IsApplicationInsightsUrlReturnsFalseForNullOrEmptyUrl()
        {
            using (TelemetryConfiguration configuration = this.CreateStubTelemetryConfiguration())
            {
                string url = null;
                ApplicationInsightsUrlFilter urlFilter = new ApplicationInsightsUrlFilter(configuration);
                Assert.IsFalse(urlFilter.IsApplicationInsightsUrl(url));
                url = string.Empty;
                Assert.IsFalse(urlFilter.IsApplicationInsightsUrl(url));
            }
        }

        private TelemetryConfiguration CreateStubTelemetryConfiguration()
        {
            TelemetryConfiguration configuration = new TelemetryConfiguration();
            configuration.TelemetryChannel = new StubTelemetryChannel { EndpointAddress = "https://endpointaddress" };
            configuration.InstrumentationKey = Guid.NewGuid().ToString();
            return configuration;
        }
    }
}
