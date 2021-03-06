﻿using PerformanceAlert.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace PerformanceAlert {
    public class PerformanceMonitor {
        int _averageRotations;
        double _interval;

        List<float> _availableCPU = new List<float>();
        List<float> _availableRAM = new List<float>();
        PerformanceCounter _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        PerformanceCounter _ramCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");

        /// <summary>
        /// Gets the average cpu in percent.
        /// </summary>
        /// <value>
        /// The average cpu.
        /// </value>
        public int AverageCPU { get; private set; }

        /// <summary>
        /// Gets the average ram in percent.
        /// </summary>
        /// <value>
        /// The average ram.
        /// </value>
        public int AverageRAM { get; private set; }

        /// <summary>
        /// Gets the duration of the measurement.
        /// </summary>
        /// <value>
        /// The duration of the measurement.
        /// </value>
        public TimeSpan MeasurementDuration { get; private set; }

        /// <summary>
        /// Occurs when [update].
        /// </summary>
        public event EventHandler Update;

        /// <summary>
        /// Occurs when [update].
        /// </summary>
        public event EventHandler Monitoring;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceMonitor"/> class.
        /// </summary>
        /// <param name="averageFrom">The number of logs that are used to calculate the average.</param>
        /// <param name="interval">The log interval in milliseconds.</param>
        public PerformanceMonitor(int averageFrom = 6, double interval = 10000) {
            _averageRotations = averageFrom;
            _interval = interval;
            MeasurementDuration = TimeSpan.FromMilliseconds(_averageRotations * _interval);

            var timer = new System.Timers.Timer(_interval);
            timer.Elapsed += new ElapsedEventHandler(TimerElapsed);
            timer.AutoReset = true;
            timer.Start();
        }

        void TimerElapsed(object source, ElapsedEventArgs e) {
            Monitoring?.Invoke(this, null);
            _availableCPU.Add(_cpuCounter.NextValue());
            _availableRAM.Add(_ramCounter.NextValue());            

            if (_availableCPU.Count() == _averageRotations) {
                AverageCPU = (int)(_availableCPU.Sum() / _averageRotations);
                AverageRAM = (int)(_availableRAM.Sum() / _averageRotations);

                _availableCPU.Clear();
                _availableRAM.Clear();

                UpdateEventHandler();
            }
        }

        void UpdateEventHandler() {
            Update?.Invoke(this, new PerformanceMonitorUpdateEvent(AverageCPU, AverageRAM, MeasurementDuration));
        }
    }
}
