﻿namespace Fr8.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Globalization;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.DataContracts;
    using Fr8.ApplicationInsights.DependencyCollector.Implementation.Operation;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights;

    /// <summary>
    /// Concrete class with all processing logic to generate RDD data from the calls backs
    /// received from Profiler instrumentation for SQL.    
    /// </summary>
    internal sealed class ProfilerSqlProcessing
    {
        internal ObjectInstanceBasedOperationHolder TelemetryTable;
        private TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfilerSqlProcessing"/> class.
        /// </summary>
        internal ProfilerSqlProcessing(TelemetryConfiguration configuration, string agentVersion, ObjectInstanceBasedOperationHolder telemetryTupleHolder)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            if (telemetryTupleHolder == null)
            {
                throw new ArgumentNullException("telemetryHolder");
            }

            this.TelemetryTable = telemetryTupleHolder;
            this.telemetryClient = new TelemetryClient(configuration);
           
            // Since dependencySource is no longer set, sdk version is prepended with information which can identify whether RDD was collected by profiler/framework
           
            // For directly using TrackDependency(), version will be simply what is set by core
            this.telemetryClient.Context.GetInternalContext().SdkVersion = string.Format(CultureInfo.InvariantCulture, "rdd{0}: {1}", RddSource.Profiler, SdkVersionUtils.GetAssemblyVersion());
            if (!string.IsNullOrEmpty(agentVersion))
            {
                this.telemetryClient.Context.GetInternalContext().AgentVersion = agentVersion;
            }
        }

        #region Sql callbacks

        /// <summary>
        /// On begin callback for ExecuteReader.
        /// </summary>
        /// <param name="thisObj">This object.</param>
        /// <param name="behavior">The callback parameter.</param>
        /// <param name="method">The state parameter.</param>
        /// <returns>The context for end callback.</returns>
        public object OnBeginForExecuteReader(object thisObj, object behavior, object method)
        {
            return this.OnBegin(thisObj, false);
        }

        /// <summary>
        /// On begin callback for sync methods except ExecuteReader.
        /// </summary>
        /// <param name="thisObj">This object.</param>        
        /// <returns>The context for end callback.</returns>
        public object OnBeginForSync(object thisObj)
        {
            return this.OnBegin(thisObj, false);
        }

        /// <summary>
        /// On end callback for ExecuteReader.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="returnValue">The return value.</param>
        /// <param name="thisObj">This object.</param>
        /// <param name="behavior">The callback parameter.</param>
        /// <param name="method">The state parameter.</param>
        /// <returns>The resulting return value.</returns>
        public object OnEndForExecuteReader(object context, object returnValue, object thisObj, object behavior, object method)
        {
            this.OnEnd(context, null, thisObj, false);
            return returnValue;
        }

        /// <summary>
        /// On end for sync methods except ExecuteReader callback.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="returnValue">The return value.</param>
        /// <param name="thisObj">This object.</param>        
        /// <returns>The resulting return value.</returns>
        public object OnEndForSync(object context, object returnValue, object thisObj)
        {
            this.OnEnd(context, null, thisObj, false);
            return returnValue;
        }

        /// <summary>
        /// On exception callback for ExecuteReader.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="thisObj">This object.</param>
        /// <param name="behavior">The callback parameter.</param>
        /// <param name="method">The state parameter.</param>
        public void OnExceptionForExecuteReader(object context, object exception, object thisObj, object behavior, object method)
        {
            this.OnEnd(context, exception, thisObj, false);
        }

        /// <summary>
        /// On end callback for sync methods except ExecuteReader.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="thisObj">This object.</param>        
        public void OnExceptionForSync(object context, object exception, object thisObj)
        {
            this.OnEnd(context, exception, thisObj, false);
        }

        /// <summary>
        /// On begin for BeginExecuteNonQueryInternal callback.
        /// </summary>
        /// <param name="thisObj">This object.</param>
        /// <param name="callback">The callback parameter.</param>
        /// <param name="stateObject">The stateObject parameter.</param>
        /// <param name="timeout">The timeout parameter.</param>
        /// <param name="asyncWrite">The asyncWrite parameter.</param>
        /// <returns>The context for end callback.</returns>
        public object OnBeginForBeginExecuteNonQueryInternal(object thisObj, object callback, object stateObject, object timeout, object asyncWrite)
        {
            return this.OnBegin(thisObj, true);
        }

        /// <summary>
        /// On begin for BeginExecuteReaderInternal callback.
        /// </summary>
        /// <param name="thisObj">This object.</param>
        /// <param name="behavior">The behavior parameter.</param>
        /// <param name="callback">The callback parameter.</param>
        /// <param name="stateObject">The stateObject parameter.</param>
        /// <param name="timeout">The timeout parameter.</param>
        /// <param name="asyncWrite">The asyncWrite parameter.</param>
        /// <returns>The context for end callback.</returns>
        public object OnBeginForBeginExecuteReaderInternal(object thisObj, object behavior, object callback, object stateObject, object timeout, object asyncWrite)
        {
            return this.OnBegin(thisObj, true);
        }

        /// <summary>
        /// On begin for BeginExecuteXmlReaderInternal callback.
        /// </summary>
        /// <param name="thisObj">This object.</param>
        /// <param name="callback">The callback parameter.</param>
        /// <param name="stateObject">The stateObject parameter.</param>
        /// <param name="timeout">The timeout parameter.</param>
        /// <param name="asyncWrite">The asyncWrite parameter.</param>
        /// <returns>The context for end callback.</returns>
        public object OnBeginForBeginExecuteXmlReaderInternal(object thisObj, object callback, object stateObject, object timeout, object asyncWrite)
        {
            return this.OnBegin(thisObj, true);
        }

        /// <summary>
        /// On end for all SQL async callbacks.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="returnValue">The return value.</param>
        /// <param name="thisObj">This object.</param>
        /// <param name="asyncResult">The asyncResult parameter.</param>
        /// <returns>The context for end callback.</returns>
        public object OnEndForSqlAsync(object context, object returnValue, object thisObj, object asyncResult)
        {
            // See implementation of EndExecuteXXX - before calling EndExecuteXXX it will check exception 
            // set by BeginXXX and throw it if it had failed
            Exception exc = null;
            if (asyncResult is Task)
            {
                exc = ((Task)asyncResult).Exception;
            }

            this.OnEnd(context, null, thisObj, true);
            return returnValue;
        }

        /// <summary>
        /// On exception for all SQL async callback.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="thisObj">This object.</param>
        /// <param name="asyncResult">The asyncResult parameter.</param>
        public void OnExceptionForSqlAsync(object context, object exception, object thisObj, object asyncResult)
        {
            this.OnEnd(context, exception, thisObj, true);
        }

        #endregion //Sql callbacks

        /// <summary>
        /// Gets SQL command resource name.
        /// </summary>
        /// <param name="thisObj">The SQL command.</param>
        /// <remarks>Before we have clarity with SQL team around EventSource instrumentation, providing name as a concatenation of parameters.</remarks>
        /// <returns>The resource name if possible otherwise empty string.</returns>
        internal string GetResourceName(object thisObj)
        {
            SqlCommand command = thisObj as SqlCommand;
            string resource = string.Empty;
            if (command != null)
            {
                if (command.Connection != null)
                {
                    string commandName = command.CommandType == CommandType.StoredProcedure
                        ? command.CommandText
                        : string.Empty;

                    resource = string.IsNullOrEmpty(commandName)
                        ? string.Join(" | ", command.Connection.DataSource, command.Connection.Database)
                        : string.Join(" | ", command.Connection.DataSource, command.Connection.Database, commandName);
                }
            }

            return resource;
        }

        /// <summary>
        /// Return CommandTest for SQL resource.
        /// </summary>
        /// <param name="thisObj">The SQL command.</param>
        /// <returns>Returns the command text or empty.</returns>
        internal string GetCommandName(object thisObj)
        {
            SqlCommand command = thisObj as SqlCommand;

            if (null != command && null != command.Connection)
            {
                return command.CommandText ?? string.Empty;
            }

            return string.Empty;
        }
     
        /// <summary>
        ///  Common helper for all Begin Callbacks.
        /// </summary>
        /// <param name="thisObj">This object.</param>
        /// <param name="isAsyncCall">Is Async Invocation.</param>
        /// <returns>The context for end callback.</returns>
        private object OnBegin(object thisObj, bool isAsyncCall)
        {
            try
            {
                if (thisObj == null)
                {
                    DependencyCollectorEventSource.Log.NotExpectedCallback(0, "OnBeginSql", "thisObj == null");
                    return null;
                }

                string resourceName = this.GetResourceName(thisObj);
                DependencyCollectorEventSource.Log.BeginCallbackCalled(thisObj.GetHashCode(), resourceName);

                if (string.IsNullOrEmpty(resourceName))
                {
                    DependencyCollectorEventSource.Log.NotExpectedCallback(thisObj.GetHashCode(), "OnBeginSql", "resourceName is empty");
                    return null;
                }

                var telemetryTuple = this.TelemetryTable.Get(thisObj);
                if (telemetryTuple != null)
                {
                    // We are already tracking this item
                    if (telemetryTuple.Item1 != null)
                    {
                        DependencyCollectorEventSource.Log.TrackingAnExistingTelemetryItemVerbose();
                        return null;
                    }
                }

                string commandText = this.GetCommandName(thisObj);

                // Try to begin if sampling this operation
                bool isCustomCreated = false;
                var telemetry = ClientServerDependencyTracker.BeginTracking(this.telemetryClient);

                telemetry.Name = resourceName;
                telemetry.DependencyKind = RemoteDependencyKind.SQL.ToString();
                telemetry.CommandName = commandText;

                // We use weaktables to store the thisObj for correlating begin with end call.
                this.TelemetryTable.Store(thisObj, new Tuple<DependencyTelemetry, bool>(telemetry, isCustomCreated));
                return null;
            }
            catch (Exception exception)
            {
                DependencyCollectorEventSource.Log.CallbackError(thisObj == null ? 0 : thisObj.GetHashCode(), "OnBeginSql", exception);
            }

            return null;
        }

        /// <summary>
        ///  Common helper for all End Callbacks.
        /// </summary>
        /// <param name="context">The context.</param>        
        /// <param name="exception">The exception object if any.</param>
        /// <param name="thisObj">This object.</param>
        /// <param name="isAsync">Whether the End is for an async invocation.</param>        
        private void OnEnd(object context, object exception, object thisObj, bool isAsync)
        {
            try
            {
                if (thisObj == null)
                {
                    DependencyCollectorEventSource.Log.NotExpectedCallback(0, "OnEndSql", "thisObj == null");
                    return;
                }

                DependencyCollectorEventSource.Log.EndCallbackCalled(thisObj.GetHashCode());

                DependencyTelemetry telemetry = null;
                Tuple<DependencyTelemetry, bool> telemetryTuple = null;
                bool isCustomGenerated = false;

                telemetryTuple = this.TelemetryTable.Get(thisObj);
                if (telemetryTuple != null)
                {
                    telemetry = telemetryTuple.Item1;
                    isCustomGenerated = telemetryTuple.Item2;
                }

                if (telemetry == null)
                {
                    DependencyCollectorEventSource.Log.EndCallbackWithNoBegin(thisObj.GetHashCode());
                    return;
                }

                if (!isCustomGenerated)
                {
                    this.TelemetryTable.Remove(thisObj);
                    telemetry.Success = exception == null;
                    var sqlEx = exception as SqlException;
                    if (sqlEx != null)
                    {
                        telemetry.ResultCode = sqlEx.Number.ToString(CultureInfo.InvariantCulture);
                    }

                    ClientServerDependencyTracker.EndTracking(this.telemetryClient, telemetry);
                }               
            }
            catch (Exception ex)
            {
                DependencyCollectorEventSource.Log.CallbackError(thisObj == null ? 0 : thisObj.GetHashCode(), "OnEndSql", ex);
            }
        }
    }
}