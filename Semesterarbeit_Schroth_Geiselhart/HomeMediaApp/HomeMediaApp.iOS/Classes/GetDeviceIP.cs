using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

using Foundation;
using HomeMediaApp.iOS.Classes;
using HomeMediaApp.Interfaces;
using UIKit;

[assembly: Xamarin.Forms.Dependency(typeof(CGetDeviceIP))]
namespace HomeMediaApp.iOS.Classes
{
    class CGetDeviceIP : IGetDeviceIPAddress
    {
        public string GetDeviceIP()
        {
            string ipAddress = "";
            foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (netInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                    netInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    foreach (var addrInfo in netInterface.GetIPProperties().UnicastAddresses)
                    {
                        if (addrInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            ipAddress = addrInfo.Address.ToString();

                        }
                    }
                }
            }
            return ipAddress;
        }
    }
}