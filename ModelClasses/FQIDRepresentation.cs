using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MIP_SDK_Tray_Manager.ModelClasses
{
    public class FQIDRepresentation
    {
        public ServerInfo ServerId { get; set; }
        public string ParentId { get; set; }
        public string ObjectId { get; set; }
        public string ObjectIdString { get; set; }
        public string Kind { get; set; }
    }
}
