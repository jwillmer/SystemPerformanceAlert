using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerformanceAlert {
    /// <summary>
    /// Used for the PerformanceMonitor event handler argument
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class PerformanceMonitorUpdateEvent : EventArgs {
        public PerformanceMonitorUpdateEvent(int averageCPU, int averageRAM, TimeSpan measurementDuration)
            : this(averageCPU, averageRAM, measurementDuration, DateTime.Now) {
        }

        public PerformanceMonitorUpdateEvent(int averageCPU, int averageRAM, TimeSpan measurementDuration, DateTime timestamp) {
            AverageCPU = averageCPU;
            AverageRAM = averageRAM;
            MeasurementDuration = measurementDuration;
            Timestamp = timestamp;
        }

        /// <summary>
        /// Gets the average cpu in percent.
        /// </summary>
        /// <value>
        /// The average cpu.
        /// </value>
        public int AverageCPU { get; }

        /// <summary>
        /// Gets the average ram in percent.
        /// </summary>
        /// <value>
        /// The average ram.
        /// </value>
        public int AverageRAM { get; }

        /// <summary>
        /// Gets the record creation.
        /// </summary>
        /// <value>
        /// The record creation.
        /// </value>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets the duration of the measurement.
        /// </summary>
        /// <value>
        /// The duration of the measurement.
        /// </value>
        public TimeSpan MeasurementDuration { get; }
    }
}
