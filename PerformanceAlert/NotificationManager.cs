using PerformanceAlert.Model;
using PerformanceAlert.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerformanceAlert {
    public class NotificationManager {
        public IEnumerable<AlertDefinition> AlertDefinitions;

        private List<PerformanceMonitorUpdateEvent> Events = new List<PerformanceMonitorUpdateEvent>();
        private List<Report> Reports = new List<Report>();

        public NotificationManager(IEnumerable<AlertDefinition> alertDefinitions) {
            AlertDefinitions = alertDefinitions;
        }

        public void Update(PerformanceMonitorUpdateEvent item) {
            Events.Add(item);

            foreach (var definition in AlertDefinitions) {
                if (TriggerPeakStartNotification(definition)) {
                    var report = GenerateNewReport(definition.Id);
                    var notification = CreatePeakStartNotification(definition);
                    report.SetPeakStartNotification(notification);

                    Notify(report.PeakStartNotification, definition);
                }

                if (TriggerPeakEndNotification(definition)) {
                    var report = GetOpenReport(definition.Id);
                    var notification = CreatePeakEndNotification(report.PeakStartNotificationTimestamp);
                    report.SetPeakEndNotification(notification);

                    Notify(report.PeakEndNotification, definition);
                }
            }
        }

        private Report GenerateNewReport(Guid definitionId) {
            var report = new Report(definitionId);
            Reports.Add(report);
            return report;
        }

        private void Notify(Notification notification, AlertDefinition definition) {
            definition.NotificationProvider.Notify(notification, definition.NotifyDeviceIds);
        }

        private Report GetOpenReport(Guid definitionId) {
            return Reports.Where(_ => _.AlertDefinitionId == definitionId).FirstOrDefault(_ => !_.IsClosed);
        }           

        private Notification CreatePeakStartNotification(AlertDefinition definition) {
            var notification = new Notification();
            var interval = definition.MeasurementTime;
            var body = DateTime.Now.ToString() + Environment.NewLine
                + "Average peak in the last " + interval + " min:" + Environment.NewLine
                + "CPU: " + GetAverageCpu(interval) + "%" + Environment.NewLine
                + "RAM: " + GetAverageRam(interval) + "%";

            notification.Title = "Performance Monitor Alert from " + Environment.MachineName;
            notification.Body = body;

            return notification;
        }

        private Notification CreatePeakEndNotification(DateTime peakStartTimestamp) {
            var notification = new Notification();
            var duration = Math.Round(DateTime.Now.Subtract(peakStartTimestamp).TotalMinutes);

            var body = DateTime.Now.ToString() + Environment.NewLine
                + "Everything back to normal after " + duration + " min. Average of the last 3 min:" + Environment.NewLine
                + "CPU: " + GetAverageCpu(3) + "%" + Environment.NewLine
                + "RAM: " + GetAverageRam(3) + "%";

            notification.Title = "Performance Monitor Alert from " + Environment.MachineName;
            notification.Body = body;

            return notification;
        }

        private bool TriggerPeakEndNotification(AlertDefinition definition) {
            var interval = definition.MeasurementTime;
            if (Events.Count < interval || interval <= 0) { return false; }

            var peakReached = PeakReached(definition, interval);
            if (peakReached) {
                return false;
            }

            if (LastPeakEndReported(definition.Id)) {
                return false;
            }
            
            return true;
        }

        private bool TriggerPeakStartNotification(AlertDefinition definition) {
            var interval = definition.MeasurementTime;
            if (Events.Count < interval || interval <= 0) { return false; }

            var peakReached = PeakReached(definition, interval);
            if (!peakReached) {
                return false;
            }

            if (CurrentPeakAlreadyReported(definition.Id)) {
                return false;
            }

            return true;
        }

        private bool PeakReached(AlertDefinition definition, int interval = 1) {
            var averageCPU = GetAverageCpu(interval);
            var averageRAM = GetAverageRam(interval);

            return CpuOverPeak(definition.AvergareCPU, averageCPU) || RamOverPeak(definition.AvergareRAM, averageRAM);
        }

        private int GetAverageCpu(int interval) {
            return Events.Skip(Math.Max(0, Events.Count() - interval)).Sum(_ => _.AverageCPU) / interval;
        }

        private int GetAverageRam(int interval) {
            return Events.Skip(Math.Max(0, Events.Count() - interval)).Sum(_ => _.AverageRAM) / interval;
        }

        private bool CpuOverPeak(int averageCpu, int cpu) {
            return cpu > averageCpu;
        }

        private bool RamOverPeak(int averageRam, int ram) {
            return ram > averageRam;
        }

        private bool LastPeakEndReported(Guid definitionId) {
            var openReports = Reports.Where(_ => _.AlertDefinitionId == definitionId).Any(_ => !_.IsClosed);
            return !openReports;
        }

        private bool CurrentPeakAlreadyReported(Guid definitionId) {
            return !LastPeakEndReported(definitionId);
        }
    }
}
