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
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Serialization;

namespace PerformanceAlert {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private NotificationManager _notificationManager;
        private ProcessMonitor _processMonitor;

        private bool _hasElevatedPrivileges {
            get {
                return WindowsIdentity.GetCurrent().Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid);
            }
        }
        public ObservableCollection<PerformanceState> PerformanceStateList { get; } = new ObservableCollection<PerformanceState>();
        public ObservableCollection<IDevice> Devices { get; } = new ObservableCollection<IDevice>();

        public Observable<bool> IsMonitoring { get; set; } = new Observable<bool>();

        private bool _hideInsteadOfClose = true;

        private System.Windows.Forms.NotifyIcon _systemTrayIcon = new System.Windows.Forms.NotifyIcon();

        private int _measurementTimeInterval { get { return Settings.Default.AlertMeasurementTime; } }

        private readonly string _logFileName = "PerformanceMonitorLog.xml";

        public MainWindow() {
            InitializeComponent();
            InitNotificationManager();
            InitProcessMonitor();
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
            counter.Monitoring += Counter_Monitoring;
        }

        private void InitProcessMonitor() {
            _processMonitor = new ProcessMonitor();

            if (_hasElevatedPrivileges) {
                MonitorProcessCxb.IsEnabled = true;
                MonitorProcessCxb.Visibility = Visibility.Visible;
                MonitorProcessInfoTxtBox.Visibility = Visibility.Collapsed;

                if (Settings.Default.MonitorProcesses) {
                    ChangeProcessMonitorState(true);
                }
            }
        }

        private void InitNotificationManager() {
            var notificationProvider = new PushbulletNotificationProvider(Settings.Default.PushbulletApiKey);

            var definition = new AlertDefinition(notificationProvider);
            definition.AvergareCPU = Settings.Default.AlertAvergareCPU;
            definition.AvergareRAM = Settings.Default.AlertAvergareRAM;
            definition.MeasurementTime = Settings.Default.AlertMeasurementTime;

            _notificationManager = new NotificationManager(new[] { definition }, _processMonitor);
        }

        private void InitSettingsMonitoring() {
            IsMonitoring.Value = AllSettingsValid();

            Settings.Default.PropertyChanged += (settings, e) => {
                var alertDefinition = _notificationManager.AlertDefinitions.First();
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
                    case "MonitorProcesses":
                        alertDefinition.IncludeProcess = (bool)value;
                        ChangeProcessMonitorState((bool)value);
                        break;
                }

                IsMonitoring.Value = AllSettingsValid();
            };
        }

        private void InitSystemTrayIcon() {
            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/PerformanceMonitoringAlerts;component/monitor.ico")).Stream;
            _systemTrayIcon.Icon = new System.Drawing.Icon(iconStream);
            _systemTrayIcon.Visible = true;

            _systemTrayIcon.BalloonTipTitle = "Application Hidden";
            _systemTrayIcon.BalloonTipText = "Use the context menu of the application in the task bar notification area to close it.";
            _systemTrayIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;

            _systemTrayIcon.MouseDoubleClick += (s, e) => ShowWindow();
            _systemTrayIcon.ContextMenu = new System.Windows.Forms.ContextMenu();
            _systemTrayIcon.ContextMenu.MenuItems.Add("Exit", (s, e) => CloseWindow());
            _systemTrayIcon.ContextMenu.MenuItems.Add("Open", (s, e) => ShowWindow());

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
                var device = JsonConvert.DeserializeObject<GenericDevice>(json);
                Devices.Add(device as IDevice);
            }

            SetSelectedDevicesToAlertDefinition(Devices);
            GuiDeviceList.SelectAll();
        }

        private void InitLogMessages() {
            if (File.Exists(_logFileName)) {
                try {
                    var xml = new XmlSerializer(typeof(List<PerformanceState>));
                    List<PerformanceState> list;
                    using (var stream = File.OpenRead(_logFileName)) {
                        list = xml.Deserialize(stream) as List<PerformanceState>;
                    }

                    // last 50 entrys
                    foreach (var entry in list.Skip(Math.Max(0, PerformanceStateList.Count() - 50))) {
                        PerformanceStateList.Add(entry);
                    }
                }
                catch {
                    File.Move(_logFileName, DateTime.Now.ToFileTimeUtc() + "-BrokenLogFile.xml");
                }
            }
        }

        private void Counter_Monitoring(object sender, EventArgs e) {

        }

        private void Counter_Update(object sender, EventArgs e) {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                var state = e as PerformanceMonitorUpdateEvent;
                var entry = Model.PerformanceState.FromPerformanceMonitorUpdateEvent(state);

                UpdateGuiLogPreview(entry);
                UpdateXmlLog();
                _notificationManager.Update(entry);
            }));
        }

        private void ChangeProcessMonitorState(bool? isRunning = null) {
            if (!_hasElevatedPrivileges) {
                _processMonitor.Stop();
            }
            else if (isRunning.HasValue && isRunning == _processMonitor.IsRunning) {
                return;
            }
            else if (isRunning.HasValue && isRunning.Value) {
                _processMonitor.Start();
            }
            else if (isRunning.HasValue && !isRunning.Value) {
                _processMonitor.Stop();
            }
            else if (_processMonitor.IsRunning) {
                _processMonitor.Stop();
            }
            else {
                _processMonitor.Start();
            }
        }

        #region Handle Window State

        private void HideWindow() {
            this.ShowInTaskbar = false;
            this.Hide();
            _systemTrayIcon.ShowBalloonTip(400);
        }

        private void ShowWindow() {
            this.ShowInTaskbar = true;
            this.Show();
        }

        private void CloseWindow() {
            _hideInsteadOfClose = false;
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (_hideInsteadOfClose) {
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
            var definition = _notificationManager.AlertDefinitions.First();
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
            var interval = _measurementTimeInterval;
            if (interval <= 0) return false;
            if (string.IsNullOrWhiteSpace(Settings.Default.PushbulletApiKey)) return false;
            if (Settings.Default.SelectedDevices.Count <= 0) return false;

            var notificationprovider = _notificationManager.AlertDefinitions.First().NotificationProvider;
            if (!notificationprovider.SetApiKey(Settings.Default.PushbulletApiKey)) return false;

            return true;
        }

        private void UpdateXmlLog() {
            if (Settings.Default.WriteLogToDisk) {
                var xml = new XmlSerializer(typeof(List<PerformanceState>));
                using (var stream = File.Open(_logFileName, FileMode.OpenOrCreate)) {
                    xml.Serialize(stream, PerformanceStateList.ToList());
                }
            }
        }

        private void UpdateGuiLogPreview(PerformanceState state) {
            PerformanceStateList.Add(state);

            if (PerformanceStateList.Count > 50 && PerformanceStateList.Count > _measurementTimeInterval) {
                // keep the GUI list small
                PerformanceStateList.RemoveAt(0);
            }
        }

        private void OpenLogFolder(object sender, RoutedEventArgs e) {
            Process.Start("explorer.exe", Environment.CurrentDirectory);
        }

        private void SetSelectedDevicesToAlertDefinition(IEnumerable<IDevice> devices) {
            var alertDefinition = _notificationManager.AlertDefinitions.First();
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