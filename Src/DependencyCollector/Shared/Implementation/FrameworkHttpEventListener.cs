﻿#if !NET40
namespace Fr8.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Diagnostics.Tracing;
    using System.Globalization;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Provides methods for listening to events from FrameworkEventSource for HTTP.
    /// </summary>
    internal class FrameworkHttpEventListener : EventListener
    {
        /// <summary>
        /// The Http processor.
        /// </summary>
        internal readonly FrameworkHttpProcessing HttpProcessingFramework;

        /// <summary>
        /// The Framework EventSource name. 
        /// </summary>
        private const string FrameworkEventSourceName = "System.Diagnostics.Eventing.FrameworkEventSource";

        /// <summary>
        /// BeginGetResponse Event ID.
        /// </summary>
        private const int BeginGetResponseEventId = 140;

        /// <summary>
        /// EndGetResponse Event ID.
        /// </summary>
        private const int EndGetResponseEventId = 141;

        /// <summary>
        /// BeginGetRequestStream Event ID.
        /// </summary>
        private const int BeginGetRequestStreamEventId = 142;

        /// <summary>
        /// EndGetRequestStream Event ID.
        /// </summary>
        private const int EndGetRequestStreamEventId = 143;

        internal FrameworkHttpEventListener(TelemetryConfiguration configuration)
        {
            this.HttpProcessingFramework = new FrameworkHttpProcessing(configuration, DependencyTableStore.Instance.WebRequestCacheHolder);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            if (this.HttpProcessingFramework != null)
            {
                this.HttpProcessingFramework.Dispose();
            }

            base.Dispose();
        }

        /// <summary>
        /// Enables HTTP event source when EventSource is created. Called for all existing 
        /// event sources when the event listener is created and when a new event source is attached to the listener.
        /// </summary>
        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource != null && eventSource.Name == FrameworkEventSourceName)
            {
                this.EnableEvents(eventSource, EventLevel.Informational, (EventKeywords)4);
                DependencyCollectorEventSource.Log.RemoteDependencyModuleVerbose("HttpEventListener initialized for event source:" + FrameworkEventSourceName);
            }

            base.OnEventSourceCreated(eventSource);
        }

        /// <summary>
        /// Called whenever an event has been written by an event source for which the event listener has enabled events.
        /// </summary>
        /// <param name="eventData">The event arguments that describe the event.</param>
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData == null || eventData.Payload == null)
            {
                return;
            }

            try
            {
                switch (eventData.EventId)
                {
                    case BeginGetResponseEventId:
                        this.OnBeginGetResponse(eventData);
                        break;
                    case EndGetResponseEventId:
                        this.OnEndGetResponse(eventData);
                        break;
                    case BeginGetRequestStreamEventId:
                        this.OnBeginGetRequestStream(eventData);
                        break;
                    case EndGetRequestStreamEventId:
                        break;
                }
            }
            catch (Exception exc)
            {
                DependencyCollectorEventSource.Log.CallbackError(0, "OnEventWritten", exc);
            }
        }

        /// <summary>
        /// Called when a postfix of a (HttpWebRequest|FileWebRequest|FtpWebRequest).BeginGetResponse method has been invoked.
        /// </summary>
        /// <param name="eventData">The event arguments that describe the event.</param>
        private void OnBeginGetResponse(EventWrittenEventArgs eventData)
        {
            if (eventData.Payload.Count >= 2)
            {
                // the id identifies the unique identifier for HttpWebRequest
                long id = Convert.ToInt64(eventData.Payload[0], CultureInfo.InvariantCulture);
                string uri = Convert.ToString(eventData.Payload[1], CultureInfo.InvariantCulture);
                if (this.HttpProcessingFramework != null)
                {
                    this.HttpProcessingFramework.OnBeginHttpCallback(id, uri);
                }
            }
        }

        /// <summary>
        /// Called when a postfix of a (HttpWebRequest|FileWebRequest|FtpWebRequest).EndGetResponse method has been invoked.
        /// </summary>
        /// <param name="eventData">The event arguments that describe the event.</param>
        private void OnEndGetResponse(EventWrittenEventArgs eventData)
        {
            if (eventData.Payload.Count >= 1)
            {
                long id = Convert.ToInt64(eventData.Payload[0], CultureInfo.InvariantCulture);

                bool? success = null;
                bool synchronous = false;
                int? statusCode = null;

                // .NET 4.6 onwards will be passing the following additional params.
                if (eventData.Payload.Count >= 4)
                {
                    if (eventData.Payload[1] != null)
                    {
                        success = Convert.ToBoolean(eventData.Payload[1], CultureInfo.InvariantCulture);
                    }

                    if (eventData.Payload[2] != null)
                    {
                        synchronous = Convert.ToBoolean(eventData.Payload[2], CultureInfo.InvariantCulture);
                    }

                    if (eventData.Payload[3] != null)
                    {
                        // status code is passed from FW - but its not yet used in RDD 
                        statusCode = Convert.ToInt32(eventData.Payload[3], CultureInfo.InvariantCulture);
                    }
                }
                else
                {
                    // In previous versions - .NET 4.5.1-4.5.2 - we cannot differentiate whether it's sync or async, 
                    // but we know that we collect only async dependency calls
                    synchronous = false;
                }

                if (this.HttpProcessingFramework != null)
                {                    
                    this.HttpProcessingFramework.OnEndHttpCallback(id, success, synchronous, statusCode);
                }
            }
        }

        /// <summary>
        /// Called when a postfix of a (HttpWebRequest|FileWebRequest|FtpWebRequest).BeginGetRequestStream method has been invoked.
        /// </summary>
        /// <param name="eventData">The event arguments that describe the event.</param>
        private void OnBeginGetRequestStream(EventWrittenEventArgs eventData)
        {
            //// ToDo: In BeginGetRequestStream callback it is possible to get incoming HTTP web request context which will allow to correlate 
            //// incoming webreqeusts with outbound web requests. We need to leverage this correlation when UI scenarious will be ready
            if (eventData.Payload.Count >= 2)
            {
                long id = Convert.ToInt64(eventData.Payload[0], CultureInfo.InvariantCulture);
                string uri = Convert.ToString(eventData.Payload[1], CultureInfo.InvariantCulture);

                if (this.HttpProcessingFramework != null)
                {
                    this.HttpProcessingFramework.OnBeginHttpCallback(id, uri);
                }
            }
        }
    }
}
#endif