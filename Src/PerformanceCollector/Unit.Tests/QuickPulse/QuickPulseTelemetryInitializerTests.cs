﻿namespace Unit.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class QuickPulseTelemetryInitializerTests
    {
        [TestMethod]
        public void QuickPulseTelemetryInitializerKeepsAccurateCountOfRequests()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryInitializer = new QuickPulseTelemetryInitializer();
            telemetryInitializer.StartCollection(accumulatorManager);

            // ACT
            telemetryInitializer.Initialize(new RequestTelemetry() { Success = true, Duration = TimeSpan.FromSeconds(1) });
            telemetryInitializer.Initialize(new RequestTelemetry() { Success = true, Duration = TimeSpan.FromSeconds(1) });
            telemetryInitializer.Initialize(new RequestTelemetry() { Success = false, Duration = TimeSpan.FromSeconds(2) });
            telemetryInitializer.Initialize(new RequestTelemetry() { Success = null, Duration = TimeSpan.FromSeconds(3) });

            // ASSERT
            Assert.AreEqual(4, accumulatorManager.CurrentDataAccumulatorReference.AIRequestCount);
            Assert.AreEqual(
                1 + 1 + 2 + 3,
                TimeSpan.FromTicks(accumulatorManager.CurrentDataAccumulatorReference.AIRequestDurationInTicks).TotalSeconds);
            Assert.AreEqual(2, accumulatorManager.CurrentDataAccumulatorReference.AIRequestSuccessCount);
            Assert.AreEqual(1, accumulatorManager.CurrentDataAccumulatorReference.AIRequestFailureCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryInitializerKeepsAccurateCountOfDependencies()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryInitializer = new QuickPulseTelemetryInitializer();
            telemetryInitializer.StartCollection(accumulatorManager);

            // ACT
            telemetryInitializer.Initialize(new DependencyTelemetry() { Success = true, Duration = TimeSpan.FromSeconds(1) });
            telemetryInitializer.Initialize(new DependencyTelemetry() { Success = true, Duration = TimeSpan.FromSeconds(1) });
            telemetryInitializer.Initialize(new DependencyTelemetry() { Success = false, Duration = TimeSpan.FromSeconds(2) });
            telemetryInitializer.Initialize(new DependencyTelemetry() { Success = null, Duration = TimeSpan.FromSeconds(3) });

            // ASSERT
            Assert.AreEqual(4, accumulatorManager.CurrentDataAccumulatorReference.AIDependencyCallCount);
            Assert.AreEqual(
                1 + 1 + 2 + 3,
                TimeSpan.FromTicks(accumulatorManager.CurrentDataAccumulatorReference.AIDependencyCallDurationInTicks).TotalSeconds);
            Assert.AreEqual(2, accumulatorManager.CurrentDataAccumulatorReference.AIDependencyCallSuccessCount);
            Assert.AreEqual(1, accumulatorManager.CurrentDataAccumulatorReference.AIDependencyCallFailureCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryInitializerStopsCollection()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryInitializer = new QuickPulseTelemetryInitializer();

            // ACT
            telemetryInitializer.StartCollection(accumulatorManager);
            telemetryInitializer.Initialize(new RequestTelemetry());
            telemetryInitializer.StopCollection();
            telemetryInitializer.Initialize(new DependencyTelemetry());

            // ASSERT
            Assert.AreEqual(1, accumulatorManager.CurrentDataAccumulatorReference.AIRequestCount);
            Assert.AreEqual(0, accumulatorManager.CurrentDataAccumulatorReference.AIDependencyCallCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryInitializerIgnoresUnrelatedTelemetryItems()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryInitializer = new QuickPulseTelemetryInitializer();
            telemetryInitializer.StartCollection(accumulatorManager);

            // ACT
            telemetryInitializer.Initialize(new EventTelemetry());
            telemetryInitializer.Initialize(new ExceptionTelemetry());
            telemetryInitializer.Initialize(new MetricTelemetry());
            telemetryInitializer.Initialize(new PageViewTelemetry());
            telemetryInitializer.Initialize(new PerformanceCounterTelemetry());
            telemetryInitializer.Initialize(new SessionStateTelemetry());
            telemetryInitializer.Initialize(new TraceTelemetry());

            // ASSERT
            Assert.AreEqual(0, accumulatorManager.CurrentDataAccumulatorReference.AIRequestCount);
            Assert.AreEqual(0, accumulatorManager.CurrentDataAccumulatorReference.AIDependencyCallCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryInitializerHandlesMultipleThreadsCorrectly()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryInitializer = new QuickPulseTelemetryInitializer();
            telemetryInitializer.StartCollection(accumulatorManager);

            // expected data loss if threading is misimplemented is around 10% (established through experiment)
            int taskCount = 10000;
            var tasks = new List<Task>(taskCount);

            for (int i = 0; i < taskCount; i++)
            {
                var requestTelemetry = new RequestTelemetry() { Success = i % 2 == 0, Duration = TimeSpan.FromMilliseconds(i) };

                var task = new Task(() => telemetryInitializer.Initialize(requestTelemetry));
                tasks.Add(task);
            }

            // ACT
            tasks.ForEach(task => task.Start());

            Task.WaitAll(tasks.ToArray());

            // ASSERT
            Assert.AreEqual(taskCount, accumulatorManager.CurrentDataAccumulatorReference.AIRequestCount);
            Assert.AreEqual(taskCount / 2, accumulatorManager.CurrentDataAccumulatorReference.AIRequestSuccessCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryInitializerSwitchesBetweenMultipleAccumulatorManagers()
        {
            // ARRANGE
            var accumulatorManager1 = new QuickPulseDataAccumulatorManager();
            var accumulatorManager2 = new QuickPulseDataAccumulatorManager();
            var telemetryInitializer = new QuickPulseTelemetryInitializer();

            // ACT
            telemetryInitializer.StartCollection(accumulatorManager1);
            telemetryInitializer.Initialize(new RequestTelemetry());
            telemetryInitializer.StopCollection();

            telemetryInitializer.StartCollection(accumulatorManager2);
            telemetryInitializer.Initialize(new DependencyTelemetry());
            telemetryInitializer.StopCollection();

            // ASSERT
            Assert.AreEqual(1, accumulatorManager1.CurrentDataAccumulatorReference.AIRequestCount);
            Assert.AreEqual(0, accumulatorManager1.CurrentDataAccumulatorReference.AIDependencyCallCount);

            Assert.AreEqual(0, accumulatorManager2.CurrentDataAccumulatorReference.AIRequestCount);
            Assert.AreEqual(1, accumulatorManager2.CurrentDataAccumulatorReference.AIDependencyCallCount);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void QuickPulseTelemetryInitializerDoesHasToBeStoppedBeforeReceingStartCommand()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryInitializer = new QuickPulseTelemetryInitializer();

            telemetryInitializer.StartCollection(accumulatorManager);

            // ACT
            telemetryInitializer.StartCollection(accumulatorManager);

            // ASSERT
            // must throw
        }
    }
}