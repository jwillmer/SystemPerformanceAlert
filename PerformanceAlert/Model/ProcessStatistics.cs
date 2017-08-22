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

        private Process _process;

        private double _totalProcessorTime;

        public ProcessStatistics(int processId, string name) {
            Id = processId;
            Name = name;

            var procList = Process.GetProcessesByName(name);
            if (procList.Length == 0 || !procList.Select(_ => _.Id == processId).Any()) {
                ProcessHasEnded = true;
            }
            else {
                try {
                    _process = procList.First(_ => _.Id == processId);
                    _totalProcessorTime = _process.TotalProcessorTime.TotalMilliseconds;
                }
                catch {
                    ProcessHasEnded = true;
                }
            }
        }

        public float GetAverageCpu(int averageFrom = 6) {
            if (!Stats.Any()) return 0;
            var cpu = Stats.OrderByDescending(_ => _.Timestamp).Take(averageFrom).Average(_ => _.Cpu);
            return (float)Math.Round(cpu, 1);
        }

        public float GetAverageRam(int averageFrom = 6) {
            if (!Stats.Any()) return 0;
            var ram = Stats.OrderByDescending(_ => _.Timestamp).Take(averageFrom).Average(_ => _.Ram);
            return (float)Math.Round(ram, 1);
        }

        public void Update() {
            try {
                if (_process != null && _process.Id != 0) {   
                    var ram = GetProcessRamUsageMb(_process);
                    var cpu = GetProcessCpuUsage(_process);
                    Stats.Add(new SystemUsage(Id, Name, cpu, ram));
                }else {
                    ProcessHasEnded = true;
                }
            }
            catch {
                ProcessHasEnded = true;
            }
        }

        private float GetProcessRamUsageMb(Process process) {
            return (process.PrivateMemorySize64 / 1024 / 1024);
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
            float CPULoad = (float)((process.TotalProcessorTime.TotalMilliseconds - _totalProcessorTime) / lifeInterval.TotalMilliseconds) * 100;
            _totalProcessorTime = process.TotalProcessorTime.TotalMilliseconds;
            var percent = CPULoad / Environment.ProcessorCount;
            return (float)Math.Round(percent, 1);
        }
    }
}
