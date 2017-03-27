using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeMediaApp.Classes
{
    public static class GlobalVariables
    {
        public static List<UPnPDevice> UPnPMediaServers = new List<UPnPDevice>();

        public static List<UPnPDevice> UPnPMediaRenderer = new List<UPnPDevice>();
    }
}
