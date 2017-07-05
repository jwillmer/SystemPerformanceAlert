using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PerformanceAlert.Model {
    public class ProcessStatistics {
        public bool ProcessHasExited { get; private set; }

        public int Id { get; private set; }

        public string Name { get; private set; }

        public List<SystemUsage> Stats = new List<SystemUsage>();

        public Process Process { get; private set; }

        public ProcessStatistics(Process process) {
            Process = process;
            Id = process.Id;
            Name = process.ProcessName;

            process.Exited += Process_Exited;
        }

        private void Process_Exited(object sender, EventArgs e) {
            ProcessHasExited = true;
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
            var ram = GetProcessRamUsageMb();
            var cpu = GetProcessCpuUsage();
            Stats.Add(new SystemUsage(Id, Name, cpu, ram));
        }

        private float GetProcessRamUsageMb() {
            return (Process.WorkingSet64 / 1024 / 1024);
        }

        private float GetProcessCpuUsage() {

            // ToDo get cpu without perf. monitor to save ressources

            return 0;
        }
    }
}
