using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using HomeMediaApp.Interfaces;

[assembly: Xamarin.Forms.Dependency(typeof(HomeMediaApp.Droid.Classes.CGetDeviceIP))]
namespace HomeMediaApp.Droid.Classes
{
    class CGetDeviceIP : IGetDeviceIPAddress
    {
        public string GetDeviceIP()
        {
            string sTemp = Dns.GetHostName();
            IPAddress[] adresses = Dns.GetHostAddresses(Dns.GetHostName());

            if (adresses != null && adresses[0] != null)
            {
                foreach (IPAddress address in adresses)
                {
                    if (!address.ToString().StartsWith("169.254")) return address.ToString();
                }
                return "";
            }
            else
            {
                return null;
            }
        }
    }
}