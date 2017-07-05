using PerformanceAlert.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerformanceAlert {
    public class ProcessMonitor {
        public List<ProcessStatistics> ProcessStatistics = new List<ProcessStatistics>();

        public void UpdateProcessStatistics() {
            CleanProcessStatistics();

            foreach (var process in Process.GetProcesses()) {
                ProcessStatistics stat = ProcessStatistics.FirstOrDefault(_ => _.Id == process.Id);

                if (stat == null) {
                    stat = new ProcessStatistics(process);
                    ProcessStatistics.Add(stat);
                }
                stat.Update();
            }
        }

        private void CleanProcessStatistics() {
            ProcessStatistics.RemoveAll(_ => _.ProcessHasExited);
        }

        public SystemUsage GetHighestRamProcess(int averageFromEntrys = 6) {
            if (!ProcessStatistics.Any()) return null;

            var process = ProcessStatistics.OrderBy(_ => _.GetAverageRam(averageFromEntrys)).FirstOrDefault();
            var ram = process.GetAverageRam(averageFromEntrys);
            var cpu = process.GetAverageCpu(averageFromEntrys);

            return new SystemUsage(process.Id, process.Name, cpu, ram);
        }

        public SystemUsage GetHighestCpuProcess(int averageFromEntrys = 6) {
            if (!ProcessStatistics.Any()) return null;

            var process = ProcessStatistics.OrderBy(_ => _.GetAverageCpu(averageFromEntrys)).FirstOrDefault();
            var ram = process.GetAverageRam(averageFromEntrys);
            var cpu = process.GetAverageCpu(averageFromEntrys);

            return new SystemUsage(process.Id, process.Name, cpu, ram);
        }      
    }
}
