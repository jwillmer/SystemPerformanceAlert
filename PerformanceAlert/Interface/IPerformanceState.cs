using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerformanceAlert.Interface {
    public interface IPerformanceState {
        /// <summary>
        /// Gets the average cpu in percent.
        /// </summary>
        /// <value>
        /// The average cpu.
        /// </value>
        int AverageCPU { get; set; }

        /// <summary>
        /// Gets the average ram in percent.
        /// </summary>
        /// <value>
        /// The average ram.
        /// </value>
        int AverageRAM { get; set; }

        /// <summary>
        /// Gets the record creation.
        /// </summary>
        /// <value>
        /// The record creation.
        /// </value>
        DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets the duration of the measurement.
        /// </summary>
        /// <value>
        /// The duration of the measurement.
        /// </value>
        TimeSpan MeasurementDuration { get; set; }
    }
}
