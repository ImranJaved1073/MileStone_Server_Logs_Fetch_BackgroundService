using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MIP_SDK_Tray_Manager.ModelClasses
{
    public class RuleLogEntry
    {
        public int Number { get; set; }       // Local time of the event
        public DateTime LocalTime { get; set; }      // Type of the source
        public string MessageText { get; set; }           // Group/category of the log
        public string Category { get; set; }     // Message associated with the log
        public string SourceType { get; set; }        // Log level (e.g., Info, Error)
        public string SourceName { get; set; }      // Source name (device or server)
        public string EventType { get; set; }          // Unique number identifier for the log
        public string RuleName { get; set; }       // Type of event
        public string ServiceName { get; set; }       // Type of event
        public string Group { get; set; }        // Category of the log (e.g., Hardware and devices)
    }
}
