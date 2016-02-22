﻿namespace Microsoft.ApplicationInsights.DependencyCollector
{
    using System;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Fr8.ApplicationInsights.DependencyCollector;
    using Fr8.ApplicationInsights.DependencyCollector.Implementation;    /// <summary>
                                                                         /// DependencyTrackingTelemetryModule .Net 4.5 specific tests. 
                                                                         /// </summary>
    public partial class DependencyTrackingTelemetryModuleTest
    {
        private static readonly string IKey = "F8474271-D231-45B6-8DD4-D344C309AE69";

        [TestMethod]
        [Timeout(5000)]
        public void TestHttpPostRequestsAreCollected()
        {
            ITelemetry sentTelemetry = null;
            var channel = new StubTelemetryChannel { OnSend = telemetry => sentTelemetry = telemetry };

            var config = new TelemetryConfiguration
            {
                InstrumentationKey = IKey,
                TelemetryChannel = channel
            };

            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.Initialize(config);
                const string Url = "http://www.bing.com";
                new HttpWebRequestUtils().ExecuteAsyncHttpRequest(Url, HttpMethod.Post);

                while (sentTelemetry == null)
                {
                    Thread.Sleep(100);
                }

                Assert.IsNotNull(sentTelemetry, "Get requests are not monitored with RDD Event Source.");
                var item = (DependencyTelemetry)sentTelemetry;
                Assert.AreEqual(Url, item.Name, "Reported Url must be " + Url);
                Assert.IsTrue(item.Duration > TimeSpan.FromMilliseconds(0), "Duration has to be positive");
                Assert.AreEqual("Http", item.DependencyKind, "HttpAny has to be dependency kind as it includes http and azure calls");
                Assert.IsTrue(
                    DateTime.UtcNow.Subtract(item.Timestamp.UtcDateTime).TotalMilliseconds < TimeSpan.FromMinutes(1).TotalMilliseconds, "timestamp < now");
                Assert.IsTrue(
                    item.Timestamp.Subtract(DateTime.UtcNow).TotalMilliseconds > -TimeSpan.FromMinutes(1).TotalMilliseconds, "now - 1 min < timestamp");
            }
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestHttpRequestsWithQueryStringAreCollected()
        {
            ITelemetry sentTelemetry = null;
            var channel = new StubTelemetryChannel { OnSend = telemetry => sentTelemetry = telemetry };
            var config = new TelemetryConfiguration
            {
                InstrumentationKey = IKey,
                TelemetryChannel = channel
            };

            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.Initialize(config);
                const string Url = "http://www.bing.com/search?q=1";
                new HttpWebRequestUtils().ExecuteAsyncHttpRequest(Url, HttpMethod.Get);

                while (sentTelemetry == null)
                {
                    Thread.Sleep(100);
                }

                Assert.IsNotNull(sentTelemetry, "Get requests are not monitored with RDD Event Source.");
                var item = (DependencyTelemetry)sentTelemetry;
                Assert.AreEqual(Url, item.Name, "Reported Url must be " + Url);
                Assert.IsTrue(item.Duration > TimeSpan.FromMilliseconds(0), "Duration has to be positive");
                Assert.AreEqual("Http", item.DependencyKind, "HttpAny has to be dependency kind as it includes http and azure calls");
                Assert.IsTrue(
                    DateTime.UtcNow.Subtract(item.Timestamp.UtcDateTime).TotalMilliseconds < TimeSpan.FromMinutes(1).TotalMilliseconds, "timestamp < now");
                Assert.IsTrue(
                    item.Timestamp.Subtract(DateTime.UtcNow).TotalMilliseconds > -TimeSpan.FromMinutes(1).TotalMilliseconds, "now - 1 min < timestamp");
            }
        }

        /// <summary>
        /// Test that http get requests are monitored correctly in remote dependency monitoring.
        /// </summary>
        [TestMethod]
        [Timeout(5000)]
        public void TestHttpGetRequestsAreCollected()
        {
            ITelemetry sentTelemetry = null;
            var channel = new StubTelemetryChannel { OnSend = telemetry => sentTelemetry = telemetry };
            var config = new TelemetryConfiguration
            {
                InstrumentationKey = IKey,
                TelemetryChannel = channel
            };

            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.Initialize(config);
                const string Url = "http://www.bing.com/maps";
                new HttpWebRequestUtils().ExecuteAsyncHttpRequest(Url, HttpMethod.Get);

                while (sentTelemetry == null)
                {
                    Thread.Sleep(100);
                }

                Assert.IsNotNull(sentTelemetry, "Get requests are not monitored with RDD Event Source.");
                var item = (DependencyTelemetry)sentTelemetry;
                Assert.AreEqual(Url, item.Name, "Reported Url must be " + Url);
                Assert.IsTrue(item.Duration > TimeSpan.FromMilliseconds(0), "Duration has to be positive");
                Assert.AreEqual("Http", item.DependencyKind, "HttpAny has to be dependency kind as it includes http and azure calls");
                Assert.IsTrue(
                    DateTime.UtcNow.Subtract(item.Timestamp.UtcDateTime).TotalMilliseconds < TimeSpan.FromMinutes(1).TotalMilliseconds, "timestamp < now");
                Assert.IsTrue(
                    item.Timestamp.Subtract(DateTime.UtcNow).TotalMilliseconds > -TimeSpan.FromMinutes(1).TotalMilliseconds, "now - 1 min < timestamp");
            }
        }

        [TestMethod]
        public void EventSourceInstrumentationDisabledWhenProfilerInstrumentationEnabled()
        {
            using (var module = new TestableDependencyTrackingTelemetryModule())
            {
                module.OnIsProfilerAvailable = () => true;

                module.Initialize(TelemetryConfiguration.CreateDefault());

                var f1 = typeof(DependencyTrackingTelemetryModule).GetField("httpEventListener", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNull(f1.GetValue(module));

                var f2 = typeof(DependencyTrackingTelemetryModule).GetField("sqlEventListener", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNull(f2.GetValue(module));
            }
        }

        [TestMethod]
        public void EventSourceInstrumentationEnabledWhenProfilerInstrumentationDisabled()
        {
            using (var module = new TestableDependencyTrackingTelemetryModule())
            {
                module.OnIsProfilerAvailable = () => false;

                module.Initialize(TelemetryConfiguration.CreateDefault());

                var f1 = typeof(DependencyTrackingTelemetryModule).GetField("httpEventListener", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(f1.GetValue(module));

                var f2 = typeof(DependencyTrackingTelemetryModule).GetField("sqlEventListener", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(f2.GetValue(module));
            }
        }

        [TestMethod]
        public void EventSourceInstrumentationEnabledWhenProfilerFailsToAttach()
        {
            using (var module = new TestableDependencyTrackingTelemetryModule())
            {
                module.OnIsProfilerAvailable = () => true;
                module.OnInitializeForRuntimeProfiler = () => { throw new Exception(); };
                DependencyTableStore.Instance.IsProfilerActivated = false;

                module.Initialize(TelemetryConfiguration.CreateDefault());

                var f1 = typeof(DependencyTrackingTelemetryModule).GetField("httpEventListener", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(f1.GetValue(module));

                var f2 = typeof(DependencyTrackingTelemetryModule).GetField("sqlEventListener", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(f2.GetValue(module));

                Assert.IsFalse(DependencyTableStore.Instance.IsProfilerActivated);
            }
        }

        internal class TestableDependencyTrackingTelemetryModule : DependencyTrackingTelemetryModule
        {
            public TestableDependencyTrackingTelemetryModule()
            {
                this.OnInitializeForRuntimeProfiler = () => { };
                this.OnIsProfilerAvailable = () => true;
            }

            public Action OnInitializeForRuntimeProfiler { get; set; }

            public Func<bool> OnIsProfilerAvailable { get; set; }

            internal override void InitializeForRuntimeProfiler()
            {
                this.OnInitializeForRuntimeProfiler();
            }

            internal override bool IsProfilerAvailable()
            {
                return this.OnIsProfilerAvailable();
            }
        }
    }
}
