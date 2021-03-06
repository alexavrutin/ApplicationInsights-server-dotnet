﻿namespace Microsoft.ApplicationInsights.Web.Implementation
{
#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif
#if NET45
    using System.Diagnostics.Tracing;
#endif

    /// <summary>
    /// Class provides methods to post event about Web event like begin or end of the request.
    /// </summary>
    [EventSource(Name = "Microsoft-ApplicationInsights-WebEventsPublisher")]
    public sealed class WebEventsPublisher : EventSource
    {
        /// <summary>
        /// WebEventsPublisher static instance.
        /// </summary>
        private static readonly WebEventsPublisher Instance = new WebEventsPublisher();

        private WebEventsPublisher()
        {
        }

        /// <summary>
        /// Gets the instance of WebEventsPublisher type.
        /// </summary>
        public static WebEventsPublisher Log
        {
            [NonEvent]
            get
            {
                return Instance;
            }
        }

        /// <summary>
        /// Method generates event about begin of the request.
        /// </summary>
        [Event(1, Level = EventLevel.LogAlways)]
        public void OnBegin()
        {
            this.WriteEvent(1);
        }

        /// <summary>
        /// Method generates event about end of the request.
        /// </summary>
        [Event(2, Level = EventLevel.LogAlways)]
        public void OnEnd()
        {
            this.WriteEvent(2);
        }

        /// <summary>
        /// Method generates event in case if request failed.
        /// </summary>
        [Event(3, Level = EventLevel.LogAlways)]
        public void OnError()
        {
            this.WriteEvent(3);
        }
    }
}
