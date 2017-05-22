using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;
using HomeMediaApp.Interfaces;
using HomeMediaApp.Windows.Classes;
using Xamarin.Forms;

[assembly: Dependency(typeof(CGetDeviceIP))]
namespace HomeMediaApp.Windows.Classes
{
    class CGetDeviceIP : IGetDeviceIPAddress
    {
        public string GetDeviceIP()
        {
            List<string> Name = new List<string>();
            var icp = NetworkInformation.GetInternetConnectionProfile();
            if (icp != null && icp.NetworkAdapter != null)
            {
                foreach (HostName oName in NetworkInformation.GetHostNames())
                {
                    if (oName.IPInformation != null && oName.IPInformation.NetworkAdapter != null &&
                        oName.IPInformation.NetworkAdapter.NetworkAdapterId == icp.NetworkAdapter.NetworkAdapterId)
                    {
                        Name.Add(oName.CanonicalName);
                    }
                }
            }
            foreach (string IP in Name)
            {
                string[] Parts = IP.Split(new string[] {"."}, StringSplitOptions.RemoveEmptyEntries);
                int temp = 0;
                foreach (string part in Parts)
                {
                    int j = 0;
                    if (!int.TryParse(part, out j)) break;
                    else temp++;
                }
                if (temp == Parts.Length) return IP;
            }
            return "";
        }
    }
}
