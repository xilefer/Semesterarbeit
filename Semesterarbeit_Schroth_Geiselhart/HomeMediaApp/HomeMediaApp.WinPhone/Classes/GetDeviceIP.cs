using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeMediaApp.Interfaces;
using HomeMediaApp.WinPhone.Classes;
using Xamarin.Forms;
using System.Net;

[assembly: Dependency(typeof(CGetDeviceIP))]
namespace HomeMediaApp.WinPhone.Classes
{
    class CGetDeviceIP : IGetDeviceIPAddress
    {
        public string GetDeviceIP()
        {
            try
            {
                List<string> IpAddress = new List<string>();
                var Hosts = Windows.Networking.Connectivity.NetworkInformation.GetHostNames().ToList();
                foreach (var Host in Hosts)
                {
                    string IP = Host.DisplayName;
                    IpAddress.Add(IP);
                }
                foreach (string IP in IpAddress)
                {
                    string[] Parts = IP.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                    int temp = 0;
                    foreach (string part in Parts)
                    {
                        int j = 0;
                        if (!int.TryParse(part, out j)) break;
                        else temp++;
                    }
                    if (temp == Parts.Length) return IP;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
            return "";
        }
    }
}
