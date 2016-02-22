﻿namespace Fr8.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Globalization;
    using Microsoft.ApplicationInsights.DataContracts;
    using Fr8.ApplicationInsights.DependencyCollector.Implementation.Operation;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights;
    internal sealed class FrameworkHttpProcessing : IDisposable
    {
        internal CacheBasedOperationHolder TelemetryTable;
        private readonly ApplicationInsightsUrlFilter applicationInsightsUrlFilter;
        private readonly TelemetryClient telemetryClient;

        internal FrameworkHttpProcessing(TelemetryConfiguration configuration, CacheBasedOperationHolder telemetryTupleHolder)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            if (telemetryTupleHolder == null)
            {
                throw new ArgumentNullException("telemetryTupleHolder");
            }

            this.applicationInsightsUrlFilter = new ApplicationInsightsUrlFilter(configuration);
            this.TelemetryTable = telemetryTupleHolder;
            this.telemetryClient = new TelemetryClient(configuration);
            
            // Since dependencySource is no longer set, sdk version is prepended with information which can identify whether RDD was collected by profiler/framework
            
            // For directly using TrackDependency(), version will be simply what is set by core
            this.telemetryClient.Context.GetInternalContext().SdkVersion = string.Format(CultureInfo.InvariantCulture, "rdd{0}: {1}", RddSource.Framework, SdkVersionUtils.GetAssemblyVersion());
        }
      
        /// <summary>
        /// On begin callback from Framework event source.
        /// </summary>
        /// <param name="id">This object.</param>
        /// <param name="resourceName">URI of the web request.</param>
        public void OnBeginHttpCallback(long id, string resourceName)
        {
            try
            {
                DependencyCollectorEventSource.Log.BeginCallbackCalled(id, resourceName);

                if (string.IsNullOrEmpty(resourceName))
                {
                    DependencyCollectorEventSource.Log.NotExpectedCallback(id, "OnBeginHttp", "resourceName is empty");
                    return;
                }

                if (this.applicationInsightsUrlFilter.IsApplicationInsightsUrl(resourceName))
                {
                    return;
                }

                var telemetryTuple = this.TelemetryTable.Get(id);

                if (telemetryTuple != null)
                {
                    // this operation is already being tracked. We will not restart operation
                    if (telemetryTuple.Item1 != null)
                    {
                        DependencyCollectorEventSource.Log.TrackingAnExistingTelemetryItemVerbose();
                        return;
                    }
                }

                bool isCustomCreated = false;

                var telemetry = ClientServerDependencyTracker.BeginTracking(this.telemetryClient);

                telemetry.Name = resourceName;

               this.TelemetryTable.Store(id, new Tuple<DependencyTelemetry, bool>(telemetry, isCustomCreated));
            }
            catch (Exception exception)
            {
                DependencyCollectorEventSource.Log.CallbackError(id, "OnBeginHttp", exception);
            }
        }
        
        /// <summary>
        /// On end callback from Framework event source.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="success">The success to indicate if the dependency call completed successfully or not.</param>
        /// <param name="synchronous">The synchronous flag to indicate if the dependency call was synchronous or not.</param>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        public void OnEndHttpCallback(long id, bool? success, bool synchronous, int? statusCode)
        {
            DependencyCollectorEventSource.Log.EndCallbackCalled(id);

            var telemetryTuple = this.TelemetryTable.Get(id);

            if (telemetryTuple == null)
            {
                DependencyCollectorEventSource.Log.EndCallbackWithNoBegin(id);
                return;
            }

            if (!telemetryTuple.Item2)
            {
                this.TelemetryTable.Remove(id);
                DependencyTelemetry telemetry = telemetryTuple.Item1;
                telemetry.DependencyKind = RemoteDependencyKind.Http.ToString();

                if (!statusCode.HasValue)
                {
                    statusCode = -1;
                }

                telemetry.ResultCode = statusCode.Value > 0 ? statusCode.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;

                // We calculate success on the base of http code and do not use the 'success' method argument
                // because framework returns true all the time if you use HttpClient to create a request
                // statusCode == -1 if there is no Response
                telemetry.Success = (statusCode > 0) && (statusCode < 400);

                ClientServerDependencyTracker.EndTracking(this.telemetryClient, telemetry);
            }
        }   

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.TelemetryTable.Dispose();
        }
    }
}
