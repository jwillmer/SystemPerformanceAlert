using PerformanceAlert.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerformanceAlert {
    public class ProcessMonitor {
        PerformanceCounter _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        PerformanceCounter _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");

        public void GetProcess() {
            var list = new List<ProcessState>();

            foreach (var process in Process.GetProcesses()) {
                var cpuPercent = GetProcessCpuUsagePercent(process.ProcessName);
                var ramPercent = GetProcessRamUsagePercent(process.ProcessName);
            }     
        }

        private float GetProcessCpuUsagePercent(string processName) {
            var processCounter = new PerformanceCounter("Process", "% Processor Time", processName);
            var processCpuUsage = (_cpuCounter.NextValue() / 100) * processCounter.NextValue();
            return processCpuUsage / Environment.ProcessorCount;

        }

        private float GetProcessRamUsagePercent(string processName) {
            using (var processCounter = new PerformanceCounter("Process", "Working Set - Private", processName)) {
                var processWorkingSetMb = Convert.ToInt32(processCounter.NextValue()) / (int)(1024);
                return processWorkingSetMb * (_memoryCounter.NextValue() / 100);
            }
        }
    }
}
