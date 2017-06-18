using PerformanceAlert.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerformanceAlert.Model {
    public class AlertDefinition {

        public AlertDefinition(Guid definitionId, INotificationProvider provider) {
            Id = definitionId;
            NotificationProvider = provider;
        }

        public AlertDefinition(INotificationProvider provider) : this(Guid.NewGuid(), provider){
        }

        public List<string> NotifyDeviceIds { get; } = new List<string>();

        public Guid Id { get; }

        public INotificationProvider NotificationProvider { get; }

        public int MeasurementTime { get; set; }

        public int AvergareCPU { get; set; }

        public int AvergareRAM { get; set; }
    }
}
