using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HomeMediaApp.Classes
{
    /// <summary>
    /// Klasse zum Speichern der Eigenschaften eines UPnP-Gerätes
    /// </summary>
    public class UPnPDevice
    {
        public string               Type { get; set; }
        public List<UPnPService> DeviceMethods { get; set; } = new List<UPnPService>();
        public XDocument            Config { get; set; }
        public string               DeviceName { get; set; }
        public Uri                  DeviceAddress { get; set; }

        public Dictionary<string, List<string>> Protocoltypes = new Dictionary<string, List<string>>();
    }
}
