using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MIP_SDK_Tray_Manager.ModelClasses
{
    public class EventStatus
    {
        public string Timestamp { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string State { get; set; }
        public FQIDRepresentation FQID { get; set; }
    }
}
