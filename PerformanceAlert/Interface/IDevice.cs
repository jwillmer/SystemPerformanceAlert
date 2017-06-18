using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerformanceAlert.Interface {
    public interface IDevice {
        string Nickname { get; set; }
        string Id { get; set; }
    }
}
