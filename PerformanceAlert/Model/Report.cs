using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerformanceAlert.Model {
    public class Report {
        public Report(Guid definitionId) {
            AlertDefinitionId = definitionId;
        }

        public Guid AlertDefinitionId { get; }

        public Notification PeakStartNotification { get; private set; }

        public Notification PeakEndNotification { get; private set; }

        public DateTime PeakStartNotificationTimestamp { get; private set; }

        public DateTime PeakEndNotificationTimestamp { get; private set; }

        public void SetPeakStartNotification(Notification notification) {
            if (PeakStartNotification == null) {
                PeakStartNotification = notification;
                PeakStartNotificationTimestamp = DateTime.Now;
            }
        }

        public void SetPeakEndNotification(Notification notification) {
            if (PeakEndNotification == null) {
                PeakEndNotification = notification;
                PeakEndNotificationTimestamp = DateTime.Now;
            }
        }

        public bool IsClosed {
            get {
                return PeakEndNotification != null;
            }
        }
    }
}
