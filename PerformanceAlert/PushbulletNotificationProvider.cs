using PerformanceAlert.Interface;
using PerformanceAlert.Model;
using PushbulletSharp;
using PushbulletSharp.Models.Requests;
using PushbulletSharp.Models.Responses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerformanceAlert {
    public class PushbulletNotificationProvider : INotificationProvider {
        private string ApiKey = string.Empty;

        public PushbulletNotificationProvider(string apiKey) {
            SetApiKey(apiKey);
        }

        public bool SetApiKey(string key) {
            ApiKey = key;
            return IsPusbulletApiKeyValid();
        }

        private bool IsPusbulletApiKeyValid() {
            if (string.IsNullOrWhiteSpace(ApiKey)) return false;
            PushbulletClient client = new PushbulletClient(ApiKey);

            try {
                client.CurrentUsersInformation();
                return true;
            }
            catch {
                return false;
            }
        }

        public IEnumerable<IDevice> GetDeviceList() {
            if (!IsPusbulletApiKeyValid()) return null;

            PushbulletClient client = new PushbulletClient(ApiKey);
            var response =  client.CurrentUsersDevices();
            var devices = response.Devices;

            return devices.Where(_ => _.Pushable).Select(_ => new GenericDevice { Id = _.Iden, Nickname = _.Nickname });
        }

        public void Notify(Notification notification, IEnumerable<string> deviceIds) {
            if (deviceIds == null) return;
            if (!deviceIds.Any()) return;
            if (!IsPusbulletApiKeyValid()) return;

            PushbulletClient client = new PushbulletClient(ApiKey);
            var response = client.CurrentUsersDevices();

            List<Device> devices = new List<Device>();
            foreach (var device in response.Devices) {
                if (deviceIds.Any(_ => _ == device.Iden)) {
                    devices.Add(device);
                }
            }

            foreach (var device in devices) {
                PushNoteRequest reqeust = new PushNoteRequest() {
                    DeviceIden = device.Iden,
                    Title = notification.Title,
                    Body = notification.Body
                };

                try {
                    client.PushNote(reqeust);
                }
                catch (Exception e) {
                    if (Debugger.IsAttached) { throw e; }
                }
            }
        }        
    }
}
