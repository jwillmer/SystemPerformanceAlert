using Microsoft.Win32;
using Newtonsoft.Json;
using PerformanceAlert.Interface;
using PerformanceAlert.Model;
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
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PerformanceAlert {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private NotificationManager NotificationManager;


        public ObservableCollection<PerformanceMonitorUpdateEvent> Events { get; } = new ObservableCollection<PerformanceMonitorUpdateEvent>();
        public ObservableCollection<IDevice> Devices { get; } = new ObservableCollection<IDevice>();

        public Observable<bool> IsMonitoring { get; set; } = new Observable<bool>();

        private bool HideInsteadOfClose = true;

        private System.Windows.Forms.NotifyIcon SystemTrayIcon = new System.Windows.Forms.NotifyIcon();

        private int MeasurementTimeInterval { get { return Settings.Default.AlertMeasurementTime; } }

        private readonly string LogFileName = "PerformanceMonitorLog.txt";

        public MainWindow() {
            InitializeComponent();
            InitNotificationManager();
            InitSettingsMonitoring();
            InitDeviceListFromSettings();

            InitSystemTrayIcon();
            InitLogMessages();

            var interval = 10000; // 10 sec
#if DEBUG
            interval = 1000; // 1 sec
#endif
            var averageFrom = 6; // (10 sec * 6) = 1 min

            var counter = new PerformanceMonitor(averageFrom, interval);
            counter.Update += Counter_Update;
        }

        private void InitNotificationManager() {
            var notificationProvider = new PushbulletNotificationProvider(Settings.Default.PushbulletApiKey);

            var definition = new AlertDefinition(notificationProvider);
            definition.AvergareCPU = Settings.Default.AlertAvergareCPU;
            definition.AvergareRAM = Settings.Default.AlertAvergareRAM;
            definition.MeasurementTime = Settings.Default.AlertMeasurementTime;

            NotificationManager = new NotificationManager(new[] { definition });
        }

        private void InitSettingsMonitoring() {
            IsMonitoring.Value = AllSettingsValid();

            Settings.Default.PropertyChanged += (settings, e) => {
                var alertDefinition = NotificationManager.AlertDefinitions.First();
                var value = settings.GetType().GetProperty(e.PropertyName).GetValue(settings, null);

                switch (e.PropertyName) {
                    case "PushbulletApiKey":
                        alertDefinition.NotificationProvider.SetApiKey(value as string);
                        break;
                    case "AlertMeasurementTime":
                        alertDefinition.MeasurementTime = (int)value;
                        break;
                    case "AlertAvergareCPU":
                        alertDefinition.AvergareCPU = (int)value;
                        break;
                    case "AlertAvergareRAM":
                        alertDefinition.AvergareRAM = (int)value;
                        break;
                }

                IsMonitoring.Value = AllSettingsValid();
            };
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

        private void InitDeviceListFromSettings() {
            Devices.Clear();

            foreach (var json in Settings.Default.SelectedDevices) {
                var device = JsonConvert.DeserializeObject<Device>(json);
                Devices.Add(device as IDevice);
            }

            SetSelectedDevicesToAlertDefinition(Devices);
            GuiDeviceList.SelectAll();
        }

        private void InitLogMessages() {
            if (File.Exists(LogFileName)) {
                var lines = File.ReadLines(LogFileName).Take(50);
                foreach (var line in lines) {
                    try {
                        // the log was not intended for this
                        var cpuStartIdentifier = " - CPU: ";
                        var ramStartIdentifier = "% - RAM: ";
                        var dateEndIndex = line.IndexOf(cpuStartIdentifier);
                        var cpuEndIndex = line.IndexOf(ramStartIdentifier);
                        var ramEndIndex = line.LastIndexOf("%");
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
        }

        private void Counter_Update(object sender, EventArgs e) {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                var state = e as PerformanceMonitorUpdateEvent;
                UpdateLogPreview(state);
                WriteToLog(state);
                NotificationManager.Update(state);
            }));
        }

        #region Handle Window State

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
            else {
                var isAutostartActive = IsAutoStartActivated();
                var runOnStartup = Settings.Default.RunOnSystemStart;

                if (runOnStartup && !isAutostartActive) {
                    SetAutostart();
                }
                else if (!runOnStartup && isAutostartActive) {
                    DisableAutostart();
                }
            }
        }

        #endregion

        #region Handle Application Autostart

        private void SetAutostart() {
            var regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            Assembly assembly = Assembly.GetExecutingAssembly();
            regKey.SetValue(assembly.GetName().Name, assembly.Location);
        }

        private void DisableAutostart() {
            var regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            Assembly assembly = Assembly.GetExecutingAssembly();
            regKey.DeleteValue(assembly.GetName().Name);
        }

        private bool IsAutoStartActivated() {
            var regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            Assembly assembly = Assembly.GetExecutingAssembly();

            return regKey.GetValue(assembly.GetName().Name) != null;
        }

        #endregion

        #region Device list interactions

        private void GetDeviceList(object sender = null, RoutedEventArgs e = null) {
            var definition = NotificationManager.AlertDefinitions.First();
            var devices = definition.NotificationProvider.GetDeviceList();

            if (devices == null) { return; }


            AddNewDevices(devices);
            RemoveMissingDevices(devices);
        }

        private void RemoveMissingDevices(IEnumerable<IDevice> newDeviceList) {
            var removeDevices = Devices.Where(_ => !newDeviceList.Any(d => d.Id == _.Id));

            foreach (var device in removeDevices) {
                Devices.Remove(device);
            }
        }

        private void AddNewDevices(IEnumerable<IDevice> newDeviceList) {
            foreach (var device in newDeviceList) {
                if (!Devices.Any(_ => _.Id == device.Id)) {
                    Devices.Add(device);
                }
            }
        }

        private void SelectedDevicesChanged(object sender, SelectionChangedEventArgs e) {
            var listBox = sender as ListBox;
            Settings.Default.SelectedDevices.Clear();

            if (listBox.SelectedItems.Count > 0) {
                var list = listBox.SelectedItems.Cast<IDevice>().ToList();
                SetSelectedDevicesToAlertDefinition(list);

                foreach (var device in list) {
                    string json = JsonConvert.SerializeObject(device);
                    Settings.Default.SelectedDevices.Add(json);
                }
            }

            IsMonitoring.Value = AllSettingsValid();
        }

        #endregion

        private bool AllSettingsValid() {
            var interval = MeasurementTimeInterval;
            if (interval <= 0) return false;
            if (string.IsNullOrWhiteSpace(Settings.Default.PushbulletApiKey)) return false;
            if (Settings.Default.SelectedDevices.Count <= 0) return false;

            var notificationprovider = NotificationManager.AlertDefinitions.First().NotificationProvider;
            if (!notificationprovider.SetApiKey(Settings.Default.PushbulletApiKey)) return false;

            return true;
        }

        private void WriteToLog(PerformanceMonitorUpdateEvent state) {
            if (Settings.Default.WriteLogToDisk) {
                var cpu = state.AverageCPU.ToString().PadLeft(3, ' ');
                var ram = state.AverageRAM.ToString().PadLeft(3, ' ');

                File.AppendAllLines(LogFileName, new[] {
                    // InitLogMessages() is relying on this format!
                    state.Timestamp.ToString() + " - CPU: " + cpu  + "% - RAM: " + ram + "%"
                });
            }
        }

        private void UpdateLogPreview(PerformanceMonitorUpdateEvent state) {
            Events.Add(state);

            if (Events.Count > 50 && Events.Count > MeasurementTimeInterval) {
                // keep the GUI list small
                Events.RemoveAt(0);
            }
        }

        private void OpenLogFolder(object sender, RoutedEventArgs e) {
            Process.Start("explorer.exe", Environment.CurrentDirectory);
        }

        private void SetSelectedDevicesToAlertDefinition(IEnumerable<IDevice> devices) {
            var alertDefinition = NotificationManager.AlertDefinitions.First();
            var deviceIds = alertDefinition.NotifyDeviceIds;
            deviceIds.Clear();

            foreach (var device in devices) {
                deviceIds.Add((device as IDevice).Id);
            }
        }

        private void OnlyNumbersTextValidation(object sender, TextCompositionEventArgs e) {
            Regex regex = new Regex("[^0-9.-]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}