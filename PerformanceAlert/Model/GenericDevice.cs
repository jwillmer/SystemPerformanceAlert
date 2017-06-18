using PerformanceAlert.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerformanceAlert.Model {
    public class GenericDevice : IDevice {
        public string Nickname { get; set; }
        public string Id { get; set; }
    }
}
