﻿namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation.Operation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Fr8.ApplicationInsights.DependencyCollector.Implementation.Operation;

    /// <summary>
    /// Shared WebRequestDependencyTrackingHelpers class tests.
    /// </summary>
    [TestClass]
    public class CacheBasedOperationHolderTests : IDisposable
    {
        private Tuple<DependencyTelemetry, bool> telemetryTuple;
        private CacheBasedOperationHolder cacheBasedOperationHolder;

        [TestInitialize]
        public void TestInitialize()
        {
            this.telemetryTuple = new Tuple<DependencyTelemetry, bool>(new DependencyTelemetry(), true);
            this.cacheBasedOperationHolder = new CacheBasedOperationHolder();
        }

        /// <summary>
        /// Tests the scenario if Store() adds telemetry tuple to the cache.
        /// </summary>
        [TestMethod]
        public void StoreAddsTelemetryTupleToTheCache()
        {
            long id = 12345;
            Assert.IsNull(this.cacheBasedOperationHolder.Get(id));
            this.cacheBasedOperationHolder.Store(id, this.telemetryTuple);
            Assert.AreEqual(this.telemetryTuple, this.cacheBasedOperationHolder.Get(id));
        }

        /// <summary>
        /// Tests the scenario if Store() adds telemetry tuple with same id.
        /// </summary>
        [TestMethod]
        public void StoreOverWritesTelemetryTupleWithSameKeyToTheCache()
        {
            long id = 12345;
            Assert.IsNull(this.cacheBasedOperationHolder.Get(id));
            var tuple = new Tuple<DependencyTelemetry, bool>(new DependencyTelemetry(), true);
            this.cacheBasedOperationHolder.Store(id, tuple);
            this.cacheBasedOperationHolder.Store(id, this.telemetryTuple);
            Assert.AreEqual(this.telemetryTuple, this.cacheBasedOperationHolder.Get(id));
        }

        /// <summary>
        /// Tests the scenario if Store() throws exception null tuple.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void StoreThrowsExceptionForNullTelemetryTuple()
        {
            long id = 12345;
            this.cacheBasedOperationHolder.Store(id, null);
        }

        /// <summary>
        /// Tests the scenario if Remove() removes telemetry tuple from the cache.
        /// </summary>
        [TestMethod]
        public void RemoveDeletesTelemetryTupleFromTheCache()
        {
            long id = 12345;
            this.cacheBasedOperationHolder.Store(id, this.telemetryTuple);
            Assert.AreEqual(this.telemetryTuple, this.cacheBasedOperationHolder.Get(id));
            this.cacheBasedOperationHolder.Remove(id);
            Assert.IsNull(this.cacheBasedOperationHolder.Get(id));
        }

        /// <summary>
        /// Tests the scenario if Remove() does not throw an exception when it tries to delete a non existing id.
        /// </summary>
        [TestMethod]
        public void RemoveDoesNotThrowExceptionForNonExistingItem()
        {
            long id = 12345;
            this.cacheBasedOperationHolder.Store(id, this.telemetryTuple);
            Assert.IsFalse(this.cacheBasedOperationHolder.Remove(889855));
            Assert.IsTrue(this.cacheBasedOperationHolder.Remove(id));
            Assert.IsFalse(this.cacheBasedOperationHolder.Remove(id));
        }

        /// <summary>
        /// Tests the scenario if Get retrieves the tuple that corresponds to the given id.
        /// </summary>
        [TestMethod]
        public void GetReturnsItemIfItExistsInTheTable()
        {
            long id = 12345;
            this.cacheBasedOperationHolder.Store(id, this.telemetryTuple);
            Assert.AreEqual(this.telemetryTuple, this.cacheBasedOperationHolder.Get(id));
        }

        /// <summary>
        /// Tests the scenario if Get returns null for a non existing item in the table.
        /// </summary>
        [TestMethod]
        public void GetReturnsNullIfIdDoesNotExist()
        {
            Assert.IsNull(this.cacheBasedOperationHolder.Get(555555));
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool dispose)
        {
            if (dispose)
            {
                this.cacheBasedOperationHolder.Dispose();
            }
        }
    }
}
