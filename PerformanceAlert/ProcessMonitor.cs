using PerformanceAlert.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Security.Principal;
using System.Text;
using System.Timers;

namespace PerformanceAlert {
    public class ProcessMonitor {
        public List<ProcessStatistics> ProcessStatistics = new List<ProcessStatistics>();
        public bool IsRunning { get; private set; }

        ManagementEventWatcher _processStartWatch;
        Timer _updateStatistics;
        object _objLock = new object();

        /// <summary>
        /// Starts the process monitoring.
        /// </summary>
        /// <param name="updateInterval">The update interval in ms.</param>
        public void Start(int updateInterval = 10000) {
            IsRunning = true;

            foreach (var process in Process.GetProcesses()) {
                AddProcess(process.Id, process.ProcessName);
            }

            SubscribeToProcessStart();
            InitUpdateStatistics(updateInterval);
        }

        private void InitUpdateStatistics(int interval) {
            _updateStatistics = new Timer(interval);
            _updateStatistics.Elapsed += new ElapsedEventHandler(TimerElapsed);
            _updateStatistics.AutoReset = true;
            _updateStatistics.Start();
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e) {
            lock (_objLock) {
                ProcessStatistics.ForEach(_ => _.Update());
                ProcessStatistics.RemoveAll(_ => _.ProcessHasEnded);

#if DEBUG
                var ram = GetHighestRamProcess();
                var cpu = GetHighestCpuProcess();
                Debug.WriteLine(ram.Name + ";" + ram.Cpu + "%;" + ram.Ram  +"MB");
                Debug.WriteLine(cpu.Name + ";" + cpu.Cpu + "%;" + cpu.Ram + "MB");
#endif
            }
        }

        public void Stop() {
            IsRunning = false;
            _processStartWatch.Stop();
            _updateStatistics.Stop();
            ProcessStatistics.Clear();
        }

        private void SubscribeToProcessStart() {    
            var hasElevatedPrivileges = WindowsIdentity.GetCurrent().Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid);

            if (hasElevatedPrivileges) {
                _processStartWatch = new ManagementEventWatcher("SELECT ProcessID, ProcessName FROM Win32_ProcessStartTrace");
                _processStartWatch.EventArrived += new EventArrivedEventHandler(StartWatch_EventArrived);
                _processStartWatch.Start();
            }
        }
        private void StartWatch_EventArrived(object sender, EventArrivedEventArgs e) {
            var processId = (UInt32)e.NewEvent.Properties["ProcessID"].Value;
            var processName = (string)e.NewEvent.Properties["ProcessName"].Value;
            lock (_objLock) {
                AddProcess((int)processId, processName);
            }
        }

        private void AddProcess(int processId, string processName) {
            if(processId == 0 || processName == "Idle" || processName == "Memory Compression") {
                return;
            }

            var stat = new ProcessStatistics(processId, processName);
            ProcessStatistics.Add(stat);
        }

        public SystemUsage GetHighestRamProcess(int averageFromEntrys = 6) {
            if (!ProcessStatistics.Any()) return null;

            var process = ProcessStatistics.OrderByDescending(_ => _.GetAverageRam(averageFromEntrys)).FirstOrDefault();
            var ram = process.GetAverageRam(averageFromEntrys);
            var cpu = process.GetAverageCpu(averageFromEntrys);

            return new SystemUsage(process.Id, process.Name, cpu, ram);
        }

        public SystemUsage GetHighestCpuProcess(int averageFromEntrys = 6) {
            if (!ProcessStatistics.Any()) return null;

            var process = ProcessStatistics.OrderByDescending(_ => _.GetAverageCpu(averageFromEntrys)).FirstOrDefault();
            var ram = process.GetAverageRam(averageFromEntrys);
            var cpu = process.GetAverageCpu(averageFromEntrys);

            return new SystemUsage(process.Id, process.Name, cpu, ram);
        }
    }
}
