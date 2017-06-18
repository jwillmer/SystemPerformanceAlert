using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PerformanceAlert;
using PerformanceAlert.Model;
using System.Collections.Generic;
using System.Linq;
using PerformanceAlert.Interface;
using Moq;

namespace PerformanceAlertTests {
    [TestClass]
    public class NotificationManagerTests {
        Mock<INotificationProvider> NotificationProviderMock => new Mock<INotificationProvider>();

        [TestInitialize]
        public void TestInitialize() {

        }

        [TestMethod]
        public void InitNotificationManagerTest() {
            var definitions = new List<AlertDefinition> {
                new AlertDefinition(NotificationProviderMock.Object),
                new AlertDefinition(NotificationProviderMock.Object)
            };
            var manager = new NotificationManager(definitions);

            Assert.IsTrue(manager.AlertDefinitions.Count() == 2);
        }

        [TestMethod]
        public void GetAverageCpuTest() {
            var definitions = new List<AlertDefinition>();
            NotificationManager manager = new NotificationManager(definitions);

            int v1 = 35,
                v2 = 60,
                v3 = 25;
            var eventT1 = new PerformanceMonitorUpdateEvent(v1, 0, new TimeSpan());
            var eventT2 = new PerformanceMonitorUpdateEvent(v2, 1, new TimeSpan());
            var eventT3 = new PerformanceMonitorUpdateEvent(v3, 2, new TimeSpan());

            manager.Update(eventT1);
            manager.Update(eventT2);
            manager.Update(eventT3);

            manager.AlertDefinitions.Add(new AlertDefinition(NotificationProviderMock.Object) {
                AvergareCPU = 30,
                AvergareRAM = 30,
                MeasurementTime = 10
            });

            PrivateObject obj = new PrivateObject(manager);
            var value1 = (int)obj.Invoke("GetAverageCpu", new object[] { 1 });
            var value2 = (int)obj.Invoke("GetAverageCpu", new object[] { 2 });
            var value3 = (int)obj.Invoke("GetAverageCpu", new object[] { 3 });


            Assert.IsTrue(value1 == v3);
            Assert.IsTrue(value2 == (v3 + v2) / 2);
            Assert.IsTrue(value3 == (v1 + v2 + v3) / 3);
        }

        [TestMethod]
        public void NotifyTest() {
            var notificationProviderMock = new Mock<INotificationProvider>();
            var definition = new AlertDefinition(notificationProviderMock.Object);
            definition.NotifyDeviceIds.AddRange(new[] { "1", "2", "3" });
            var manager = new NotificationManager(new[] { definition });

            notificationProviderMock.Setup(_ => _.Notify(It.IsAny<Notification>(), definition.NotifyDeviceIds)).Verifiable();
            
            PrivateObject obj = new PrivateObject(manager);
            obj.Invoke("Notify", new object[] { new Notification(), definition });


            notificationProviderMock.Verify();
        }

        [TestMethod]
        public void UpdateEventListTest() {
            var definitions = new List<AlertDefinition>();
            NotificationManager manager = new NotificationManager(definitions);

            var eventT1 = new PerformanceMonitorUpdateEvent(0, 0, new TimeSpan());
            var eventT2 = new PerformanceMonitorUpdateEvent(1, 1, new TimeSpan());
            var eventT3 = new PerformanceMonitorUpdateEvent(2, 2, new TimeSpan());


            var prop = manager.GetType().GetField("Events", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var eventList = (List<PerformanceMonitorUpdateEvent>)prop.GetValue(manager);

            Assert.IsTrue(eventList.Count() == 0);

            manager.Update(eventT1);
            manager.Update(eventT2);
            manager.Update(eventT3);

            Assert.IsTrue(eventList.Count() == 3);
        }

        [TestMethod]
        public void GetAverageRamTest() {
            var definitions = new List<AlertDefinition>();
            NotificationManager manager = new NotificationManager(definitions);

            int v1 = 35,
                v2 = 60,
                v3 = 25;
            var eventT1 = new PerformanceMonitorUpdateEvent(0, v1, new TimeSpan());
            var eventT2 = new PerformanceMonitorUpdateEvent(1, v2, new TimeSpan());
            var eventT3 = new PerformanceMonitorUpdateEvent(2, v3, new TimeSpan());

            manager.Update(eventT1);
            manager.Update(eventT2);
            manager.Update(eventT3);

            manager.AlertDefinitions.Add(new AlertDefinition(NotificationProviderMock.Object) {
                AvergareCPU = 30,
                AvergareRAM = 30,
                MeasurementTime = 10
            });

            PrivateObject obj = new PrivateObject(manager);
            var value1 = (int)obj.Invoke("GetAverageRam", new object[] { 1 });
            var value2 = (int)obj.Invoke("GetAverageRam", new object[] { 2 });
            var value3 = (int)obj.Invoke("GetAverageRam", new object[] { 3 });


            Assert.IsTrue(value1 == v3);
            Assert.IsTrue(value2 == (v3 + v2) / 2);
            Assert.IsTrue(value3 == (v1 + v2 + v3) / 3);
        }

        [TestMethod]
        public void RamPeakReachedTest() {
            var definitions = new List<AlertDefinition>();
            NotificationManager manager = new NotificationManager(definitions);

            int current = 60,
                average1 = 59,
                average2 = current,
                average3 = 61;
            var eventT1 = new PerformanceMonitorUpdateEvent(0, current, new TimeSpan());

            manager.Update(eventT1);

            var definition1 = new AlertDefinition(NotificationProviderMock.Object) {
                AvergareCPU = 30,
                AvergareRAM = average1,
                MeasurementTime = 10
            };

            var definition2 = new AlertDefinition(NotificationProviderMock.Object) {
                AvergareCPU = 30,
                AvergareRAM = average2,
                MeasurementTime = 10
            };

            var definition3 = new AlertDefinition(NotificationProviderMock.Object) {
                AvergareCPU = 30,
                AvergareRAM = average3,
                MeasurementTime = 10
            };

            PrivateObject obj = new PrivateObject(manager);
            var value1 = (bool)obj.Invoke("PeakReached", new object[] { definition1, 1 });
            var value2 = (bool)obj.Invoke("PeakReached", new object[] { definition2, 1 });
            var value3 = (bool)obj.Invoke("PeakReached", new object[] { definition3, 1 });


            Assert.IsTrue(value1);
            Assert.IsFalse(value2);
            Assert.IsFalse(value3);
        }

        [TestMethod]
        public void CpuPeakReachedTest() {
            var definitions = new List<AlertDefinition>();
            NotificationManager manager = new NotificationManager(definitions);

            int current = 60,
                average1 = 59,
                average2 = current,
                average3 = 61;
            var eventT1 = new PerformanceMonitorUpdateEvent(current, 0, new TimeSpan());

            manager.Update(eventT1);

            var definition1 = new AlertDefinition(NotificationProviderMock.Object) {
                AvergareCPU = average1,
                AvergareRAM = 30,
                MeasurementTime = 10
            };

            var definition2 = new AlertDefinition(NotificationProviderMock.Object) {
                AvergareCPU = average2,
                AvergareRAM = 30,
                MeasurementTime = 10
            };

            var definition3 = new AlertDefinition(NotificationProviderMock.Object) {
                AvergareCPU = average3,
                AvergareRAM = 30,
                MeasurementTime = 10
            };

            PrivateObject obj = new PrivateObject(manager);
            var value1 = (bool)obj.Invoke("PeakReached", new object[] { definition1, 1 });
            var value2 = (bool)obj.Invoke("PeakReached", new object[] { definition2, 1 });
            var value3 = (bool)obj.Invoke("PeakReached", new object[] { definition3, 1 });


            Assert.IsTrue(value1);
            Assert.IsFalse(value2);
            Assert.IsFalse(value3);
        }

        [TestMethod]
        public void PeakReportedTest_Closed() {
            var definitions = new List<AlertDefinition>();
            NotificationManager manager = new NotificationManager(definitions);

            var definitionId = Guid.NewGuid();
            var reports = new List<Report>() {
                new Report(definitionId)
            };
            reports.First().SetPeakEndNotification(new Notification());

            var prop = manager.GetType().GetField("Reports", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            prop.SetValue(manager, reports);


            PrivateObject obj = new PrivateObject(manager);
            var isEndReported = (bool)obj.Invoke("LastPeakEndReported", new object[] { definitionId });
            var isStartReported = (bool)obj.Invoke("CurrentPeakAlreadyReported", new object[] { definitionId });
            var openReport = (Report)obj.Invoke("GetOpenReport", new object[] { definitionId });


            Assert.IsFalse(isStartReported);
            Assert.IsTrue(isEndReported);
            Assert.IsFalse(openReport != null);

        }

        [TestMethod]
        public void PeakReportedTest_Open() {
            var definitions = new List<AlertDefinition>();
            NotificationManager manager = new NotificationManager(definitions);

            var definitionId = Guid.NewGuid();
            var reports = new List<Report>() {
                new Report(definitionId)
            };

            var prop = manager.GetType().GetField("Reports", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            prop.SetValue(manager, reports);

            PrivateObject obj = new PrivateObject(manager);
            var isEndReported = (bool)obj.Invoke("LastPeakEndReported", new object[] { definitionId });
            var isStartReported = (bool)obj.Invoke("CurrentPeakAlreadyReported", new object[] { definitionId });
            var openReport = (Report)obj.Invoke("GetOpenReport", new object[] { definitionId });


            Assert.IsTrue(isStartReported);
            Assert.IsFalse(isEndReported);
            Assert.IsTrue(openReport != null);
        }

        [TestMethod]
        public void GenerateNewReportTest() {
            var definitions = new List<AlertDefinition>();
            NotificationManager manager = new NotificationManager(definitions);

            var prop = manager.GetType().GetField("Reports", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var reportList = (List<Report>)prop.GetValue(manager);

            Assert.IsTrue(reportList.Count() == 0);

            var definitionId = Guid.NewGuid();
            PrivateObject obj = new PrivateObject(manager);
            var report = (Report)obj.Invoke("GenerateNewReport", new object[] { definitionId });

            Assert.IsTrue(reportList.Count() == 1);
        }
    }
}
