using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MIP_SDK_Tray_Manager.ModelClasses
{
    public class SystemLogEntry
    {
        public DateTime LocalTime { get; set; }       // Local time of the event
        public string SourceType { get; set; }      // Type of the source
        public string Group { get; set; }           // Group/category of the log
        public string MessageText { get; set; }     // Message associated with the log
        public string LogLevel { get; set; }        // Log level (e.g., Info, Error)
        public string SourceName { get; set; }      // Source name (device or server)
        public int Number { get; set; }          // Unique number identifier for the log
        public string EventType { get; set; }       // Type of event
        public string Category { get; set; }        // Category of the log (e.g., Hardware and devices)
    }
}
