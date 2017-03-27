using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeMediaApp.Classes
{
    public static class GlobalVariables
    {
        public static ObservableCollection<UPnPDevice> UPnPMediaServers = new ObservableCollection<UPnPDevice>();

        public static ObservableCollection<UPnPDevice> UPnPMediaRenderer = new ObservableCollection<UPnPDevice>();
    }
}
