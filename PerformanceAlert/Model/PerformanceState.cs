using PerformanceAlert.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PerformanceAlert.Model {
    public class PerformanceState : IPerformanceState {

       public static PerformanceState FromPerformanceMonitorUpdateEvent(PerformanceMonitorUpdateEvent e) {
            return new PerformanceState {
                AverageCPU = e.AverageCPU,
                AverageRAM = e.AverageRAM,
                Timestamp = e.Timestamp,
                MeasurementDuration = e.MeasurementDuration
            };
        }

        /// <summary>
        /// Gets the average cpu in percent.
        /// </summary>
        /// <value>
        /// The average cpu.
        /// </value>
        public int AverageCPU { get; set; }

        /// <summary>
        /// Gets the average ram in percent.
        /// </summary>
        /// <value>
        /// The average ram.
        /// </value>
        public int AverageRAM { get; set; }

        /// <summary>
        /// Gets the record creation.
        /// </summary>
        /// <value>
        /// The record creation.
        /// </value>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets the duration of the measurement.
        /// </summary>
        /// <value>
        /// The duration of the measurement.
        /// </value>
        public TimeSpan MeasurementDuration { get; set; }
    }
}
