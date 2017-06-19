using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerformanceAlert.Model {
    public class ProcessState {
        public string Name { get; set; }
        public float Cpu    { get; set; }

        public float Ram { get; set; }
    }

}
