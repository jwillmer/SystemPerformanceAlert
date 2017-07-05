using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PerformanceAlert.Model;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace PerformanceAlertTests {
    [TestClass]
    public class ProcessStatisticsTest {
        List<SystemUsage> Usage = new List<SystemUsage>();

        [TestInitialize]
        public void TestInitialize() {
            Usage = GenerateUsage(5);
        }
         

        [TestMethod]
        public void GetAverageCpuTest() {
            ProcessStatistics stats = new ProcessStatistics(Process.GetCurrentProcess());
            stats.Stats.AddRange(Usage);
            var averageOf3 = stats.GetAverageCpu(3);
            Assert.IsTrue(averageOf3 == (double) (3 + 4 + 5) / 3);
        }

        [TestMethod]
        public void GetAverageRamTest() {
            ProcessStatistics stats = new ProcessStatistics(Process.GetCurrentProcess());
            stats.Stats.AddRange(Usage);
            var averageOf3 = stats.GetAverageRam(3);
            Assert.IsTrue(averageOf3 == (double) (3 + 4 + 5) / 3);
        }

        [TestMethod]
        public void GetAverageRamTest_MoreThenStored() {
            ProcessStatistics stats = new ProcessStatistics(Process.GetCurrentProcess());
            stats.Stats.AddRange(Usage);
            var averageOf12 = stats.GetAverageRam(12);
            Assert.IsTrue(averageOf12 == (double) (1 + 2 + 3 + 4 + 5) / 5);
        }

        [TestMethod]
        public void GetAverageCpuTest_MoreThenStored() {
            ProcessStatistics stats = new ProcessStatistics(Process.GetCurrentProcess());
            stats.Stats.AddRange(Usage);
            var averageOf12 = stats.GetAverageCpu(12);
            Assert.IsTrue(averageOf12 == (double) (1 + 2 + 3 + 4 + 5) / 5);
        }

        private List<SystemUsage> GenerateUsage(int amount = 10) {
            var usage = new List<SystemUsage>();

            for(var x =0; x < amount; x++) {
                var n = x + 1;
                usage.Add(new SystemUsage(0,"", n, n));
               Thread.Sleep(1000);
            }

            return usage;
        }

    }
}
