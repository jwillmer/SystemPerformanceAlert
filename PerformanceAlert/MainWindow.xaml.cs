using Newtonsoft.Json;
using PerformanceAlert.Properties;
using PushbulletSharp;
using PushbulletSharp.Models.Requests;
using PushbulletSharp.Models.Responses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PerformanceAlert {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public ObservableCollection<PerformanceMonitorUpdateEvent> Events { get; } = new ObservableCollection<PerformanceMonitorUpdateEvent>();
        public ObservableCollection<Device> Devices { get; } = new ObservableCollection<Device>();

        public bool IsMonitoring { get { return AllSettingsValid(); } }

        private bool HideInsteadOfClose = true;

        private System.Windows.Forms.NotifyIcon SystemTrayIcon = new System.Windows.Forms.NotifyIcon();

        private int MeasurementTimeInterval { get { return Settings.Default.AlertMeasurementTime; } }

        private List<DateTime> NotificationSent { get; } = new List<DateTime>();

        private DateTime LastNotification {
            get { return NotificationSent.Any() ? NotificationSent.Last() : new DateTime(); }
            set { NotificationSent.Add(value); }
        }

        private readonly string LogFileName = "PerformanceMonitorLog.txt";

        public MainWindow() {
            InitializeComponent();
            InitDeviceListFromSettings();
            InitSystemTrayIcon();
            InitLogMessages();

            var interval = 10000; // 10 sec
            var averageFrom = 6; // (10 sec * 6) = 1 min

            var counter = new PerformanceMonitor(averageFrom, interval);
            counter.Update += Counter_Update;
        }

        private void InitSystemTrayIcon() {
            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/PerformanceMonitoringAlerts;component/monitor.ico")).Stream;
            SystemTrayIcon.Icon = new System.Drawing.Icon(iconStream);
            SystemTrayIcon.Visible = true;

            SystemTrayIcon.BalloonTipTitle = "Application Hidden";
            SystemTrayIcon.BalloonTipText = "Use the context menu of the application in the task bar notification area to close it.";
            SystemTrayIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;

            SystemTrayIcon.MouseDoubleClick += (s, e) => ShowWindow();
            SystemTrayIcon.ContextMenu = new System.Windows.Forms.ContextMenu();
            SystemTrayIcon.ContextMenu.MenuItems.Add("Exit", (s, e) => CloseWindow());
            SystemTrayIcon.ContextMenu.MenuItems.Add("Open", (s, e) => ShowWindow());

            if (AllSettingsValid() && Settings.Default.StartMinimized) {
                // start minimized
                HideWindow();
            }
            else {
                ShowWindow();
            }
        }

        private void HideWindow() {
            this.ShowInTaskbar = false;
            this.Hide();
            SystemTrayIcon.ShowBalloonTip(400);
        }

        private void ShowWindow() {
            this.ShowInTaskbar = true;
            this.Show();
        }

        private void CloseWindow() {
            HideInsteadOfClose = false;
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (HideInsteadOfClose) {
                e.Cancel = true;
                HideWindow();
            }
        }

        private void InitDeviceListFromSettings() {
            foreach (var json in Settings.Default.SelectedDevices) {
                var device = JsonConvert.DeserializeObject<Device>(json);
                Devices.Add(device);
            }

            GuiDeviceList.SelectAll();
        }

        private void Counter_Update(object sender, EventArgs e) {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                var state = e as PerformanceMonitorUpdateEvent;
                UpdateLogPreview(state);
                WriteToLog(state);
                if (TriggerNotification()) { Notify(); }
            }));
        }

        private bool AllSettingsValid() {
            var interval = MeasurementTimeInterval;
            if (interval <= 0) return false;
            if (string.IsNullOrWhiteSpace(Settings.Default.PushbulletApiKey)) return false;
            if (Settings.Default.SelectedDevices.Count <= 0) return false;
            if (!IsPusbulletApiKeyValid()) return false;

            return true;
        }

        private bool TriggerNotification() {
            var interval = MeasurementTimeInterval;
            if (Events.Count < interval || interval <= 0) { return false; }

            var peakReached = PeakReached();
            if (CurrentPeakAlreadyReported()) {
                return peakReached ? false : PeakEnded();
            }

            return peakReached;
        }

        private bool CurrentPeakAlreadyReported() {
            return DateTime.Now.Subtract(LastNotification).TotalMinutes <= MeasurementTimeInterval;
        }

        private bool PeakReached() {
            var interval = MeasurementTimeInterval;
            var averageCPU = GetAverageCpu(interval);
            var averageRAM = GetAverageRam(interval);

            return CpuOverPeak(averageCPU) || RamOverPeak(averageRAM);
        }

        private bool PeakEnded() {
            var cpuStillOverPeak = Events.Skip(Math.Max(0, Events.Count() - 3)).Any(_ => CpuOverPeak(_.AverageCPU));
            var ramStillOverPeak = Events.Skip(Math.Max(0, Events.Count() - 3)).Any(_ => RamOverPeak(_.AverageRAM));

            return !(cpuStillOverPeak || ramStillOverPeak);
        }

        private int GetAverageCpu(int interval) {
            return Events.Take(interval).Sum(_ => _.AverageCPU) / interval;
        }

        private int GetAverageRam(int interval) {
            return Events.Take(interval).Sum(_ => _.AverageRAM) / interval;
        }

        private bool CpuOverPeak(int cpu) {
            return cpu > Settings.Default.AlertAvergareCPU;
        }

        private bool RamOverPeak(int ram) {
            return ram > Settings.Default.AlertAvergareRAM;
        }

        private void GetDeviceList(object sender = null, RoutedEventArgs e = null) {
            if (string.IsNullOrWhiteSpace(Settings.Default.PushbulletApiKey)) return;
            if (!IsPusbulletApiKeyValid()) return;

            PushbulletClient client = new PushbulletClient(Settings.Default.PushbulletApiKey);
            var devices = client.CurrentUsersDevices();

            AddNewDevices(devices.Devices);
            RemoveMissingDevices(devices.Devices);
        }

        private void RemoveMissingDevices(List<Device> newDeviceList) {
            var removeDevices = Devices.Where(_ => !_.Pushable || !newDeviceList.Any(d => d.Iden == _.Iden));
            foreach (var device in removeDevices) {
                Devices.Remove(device);
            }
        }

        private void AddNewDevices(List<Device> newDeviceList) {
            foreach (var device in newDeviceList) {
                if (device.Pushable && !Devices.Any(_ => _.Iden == device.Iden)) {
                    Devices.Add(device);
                }
            }
        }

        private bool IsPusbulletApiKeyValid() {
            PushbulletClient client = new PushbulletClient(Settings.Default.PushbulletApiKey);

            try {
                client.CurrentUsersInformation();
                return true;
            }
            catch {
                return false;
            }
        }

        private void Notify() {
            if (string.IsNullOrWhiteSpace(Settings.Default.PushbulletApiKey)) return;
            if (Settings.Default.SelectedDevices.Count <= 0) return;
            if (!IsPusbulletApiKeyValid()) return;

            PushbulletClient client = new PushbulletClient(Settings.Default.PushbulletApiKey);
            var devices = client.CurrentUsersDevices();

            List<Device> selectedDevices = new List<Device>();
            foreach (var json in Settings.Default.SelectedDevices) {
                selectedDevices.Add(JsonConvert.DeserializeObject<Device>(json));
            }

            var removeDevices = new List<Device>();
            foreach (var device in selectedDevices) {
                PushNoteRequest reqeust = new PushNoteRequest() {
                    DeviceIden = device.Iden,
                    Title = "Performance Monitor Alert from " + Environment.MachineName,
                    Body = GetNotification()
                };

                try {
                    client.PushNote(reqeust);
                }
                catch (Exception e) {
                    // maybe remove device from selected device list
                    // need to see if/why this case can happen
                    if (Debugger.IsAttached) {
                        throw e;
                    }
                }
            }

            LastNotification = DateTime.Now;
        }

        private string GetNotification() {
            if (CurrentPeakAlreadyReported() && PeakEnded()) {
                var duration = DateTime.Now.Subtract(LastNotification).TotalMinutes;
                return "Everything back to normal after " + duration + " min. Average of the last 3 min:" + Environment.NewLine
                + "CPU: " + GetAverageCpu(3) + "%" + Environment.NewLine
                + "RAM: " + GetAverageRam(3) + " MB";
            }

            var interval = MeasurementTimeInterval;
            return "Average peak in the last " + interval + " min:" + Environment.NewLine
                + "CPU: " + GetAverageCpu(interval) + "%" + Environment.NewLine
                + "RAM: " + GetAverageRam(interval) + " MB";
        }

        private void WriteToLog(PerformanceMonitorUpdateEvent state) {
            if (Settings.Default.WriteLogToDisk) {
                var cpu = state.AverageCPU.ToString().PadLeft(3, ' ');
                var ram = state.AverageRAM.ToString().PadLeft(5, ' ');

                File.AppendAllLines(LogFileName, new[] {
                    // InitLogMessages() is relying on this format!
                    state.Timestamp.ToString() + " - CPU: " + cpu  + "% - RAM: " + ram + " RAM"
                });
            }
        }

        private void InitLogMessages() {
            var lines = File.ReadLines(LogFileName).Take(50);
            foreach(var line in lines) {
                try {
                    // the log was not intended for this
                    var cpuStartIdentifier = " - CPU: ";
                    var ramStartIdentifier = "% - RAM: ";
                    var dateEndIndex = line.IndexOf(cpuStartIdentifier);
                    var cpuEndIndex = line.IndexOf(ramStartIdentifier);
                    var ramEndIndex = line.LastIndexOf(" RAM");
                    var cpuStartIndex = dateEndIndex + cpuStartIdentifier.Length;
                    var ramStartIndex = cpuEndIndex + ramStartIdentifier.Length;
                    var date = line.Substring(0, dateEndIndex);
                    var cpu = line.Substring(cpuStartIndex, cpuEndIndex - cpuStartIndex);
                    var ram = line.Substring(ramStartIndex, ramEndIndex - ramStartIndex);

                    Events.Add(new PerformanceMonitorUpdateEvent(int.Parse(cpu), int.Parse(ram), new TimeSpan(), DateTime.Parse(date)));
                }
                catch { }
            }
        }

        private void UpdateLogPreview(PerformanceMonitorUpdateEvent state) {
            Events.Add(state);

            if (Events.Count > 50 && Events.Count > MeasurementTimeInterval) {
                // keep the GUI list small
                Events.RemoveAt(0);
            }
        }

        private void SelectedDevicesChanged(object sender, SelectionChangedEventArgs e) {
            var listBox = sender as ListBox;
            Settings.Default.SelectedDevices.Clear();

            if (listBox.SelectedItems.Count > 0) {
                foreach (var device in listBox.SelectedItems) {
                    string json = JsonConvert.SerializeObject(device);
                    Settings.Default.SelectedDevices.Add(json);
                }
            }
        }

        private void OnlyNumbersTextValidation(object sender, TextCompositionEventArgs e) {
            Regex regex = new Regex("[^0-9.-]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void OpenLogFolder(object sender, RoutedEventArgs e) {
            Process.Start("explorer.exe", Environment.CurrentDirectory);
        }
    }
}