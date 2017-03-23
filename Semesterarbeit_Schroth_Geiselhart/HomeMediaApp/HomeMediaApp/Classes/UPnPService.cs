using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Xml.Linq;

namespace HomeMediaApp.Classes
{
    public class UPnPService
    {
        public string ServiceType { get; set; }
        public string ServiceID { get; set; }
        public string SCPDURL { get; set; }
        public string ControlURL { get; set; }
        public string EventSubURL { get; set; }
        public List<UPnPAction> ActionList { get; set; } = new List<UPnPAction>();
        public List<UPnPServiceState> ServiceStateTable { get; set; } = new List<UPnPServiceState>();
    }

    public class UPnPAction
    {
        public string ActionName { get; set; }
        public List<UPnPActionArgument> ArgumentList { get; set; } = new List<UPnPActionArgument>();
        public XDocument ActionConfig { get; set; }
        public bool Execute()
        {
            return true;
        }
    }

    public class UPnPActionArgument
    {
        public string Name { get; set; }
        public string Direction { get; set; }
        public string RelatedStateVariable { get; set; }
    }

    public class UPnPServiceState
    {
        public bool SendEvents { get; set; }
        public string Name { get; set; }
        public string DataType { get; set; }
    }
}
