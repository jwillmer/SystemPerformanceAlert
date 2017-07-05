using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerformanceAlert.Model {
    public class SystemUsage {
        public SystemUsage(int id, string name, float cpu, float ram) {
            Id = id;
            Name = name;
            Cpu = cpu;
            Ram = ram;
            Timestamp = DateTime.Now;
        }
        public int Id { get; private set; }
        public string Name { get; private set; }
        public DateTime Timestamp { get; private set; }

        /// <summary>
        /// Gets the cpu usage in percent.
        /// </summary>
        /// <value>
        /// The cpu.
        /// </value>
        public float Cpu { get; private set; }

        /// <summary>
        /// Gets the ram usage in MB.
        /// </summary>
        /// <value>
        /// The ram.
        /// </value>
        public float Ram { get; private set; }
    }

}
