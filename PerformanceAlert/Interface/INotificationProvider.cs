using PerformanceAlert.Model;
using System.Collections.Generic;

namespace PerformanceAlert.Interface {
    public interface INotificationProvider {
        bool SetApiKey(string key);

        IEnumerable<IDevice> GetDeviceList();

        void Notify(Notification notification, IEnumerable<string> deviceIds);
    }
}