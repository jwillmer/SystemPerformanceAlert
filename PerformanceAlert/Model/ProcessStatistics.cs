using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PerformanceAlert.Model {
    public class ProcessStatistics {

        public bool ProcessHasEnded { get; private set; }

        public int Id { get; private set; }

        public string Name { get; private set; }

        public List<SystemUsage> Stats = new List<SystemUsage>();

        private DateTime _lastMeasurement = DateTime.Now;

        public ProcessStatistics(int processId, string name) {
            Id = processId;
            Name = name;
        }

        public float GetAverageCpu(int averageFrom = 6) {
            if (!Stats.Any()) return 0;
            return Stats.OrderByDescending(_ => _.Timestamp).Take(averageFrom).Average(_ => _.Cpu);
        }

        public float GetAverageRam(int averageFrom = 6) {
            if (!Stats.Any()) return 0;
            return Stats.OrderByDescending(_ => _.Timestamp).Take(averageFrom).Average(_ => _.Ram);
        }

        public void Update() {
            try {

                // Reduce CPU load by PerformanceCounter or WMI 
                // Get - WmiObject - Class:Win32_Process | Where {$_.ProcessName - eq "System Idle Process"} | select - Property:ProcessName,WorkingSetSize - First:1


                using (var process = Process.GetProcessById(Id)) {
                    var ram = GetProcessRamUsageMb(process);
                    var cpu = GetProcessCpuUsage(process);
                    Stats.Add(new SystemUsage(Id, Name, cpu, ram));
                }
            }
            catch {
                ProcessHasEnded = true;
            }
        }

        private float GetProcessRamUsageMb(Process process) {
            return (process.WorkingSet64 / 1024 / 1024);
        }

        private float GetProcessCpuUsage(Process process) {
            if (_lastMeasurement != null) {
                DateTime last = _lastMeasurement;
                _lastMeasurement = DateTime.Now;
                return GetAverageCPULoad(process, last, _lastMeasurement);
            }
            else {
                _lastMeasurement = DateTime.Now;
                return 0;
            }
        }
        private float GetAverageCPULoad(Process process, DateTime from, DateTime to) {
            TimeSpan lifeInterval = (to - from);
            float CPULoad = (float)(process.TotalProcessorTime.TotalMilliseconds / lifeInterval.TotalMilliseconds) * 100;
            return CPULoad / Environment.ProcessorCount;
        }
    }
}
