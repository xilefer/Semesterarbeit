using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HomeMediaApp.Classes
{
    public class XMLParser
    {
        public UPnPDevice Parse(UPnPDevice oInputDevice)
        {
            UPnPDevice oDevice = new UPnPDevice();
            List<XElement> DeviceElements = oInputDevice.Config.Root.Elements().Where(e=> e.Name.LocalName.ToLower() == "device").ToList()[0].Elements().ToList();
            oDevice.Config = oInputDevice.Config;
            oDevice.DeviceAddress = oInputDevice.DeviceAddress;
            oDevice.DeviceName = oInputDevice.DeviceName;
            oDevice.Type = DeviceElements.Where(e => e.Name.LocalName.ToLower() == "devicetype").ToList()[0].Value.ToString().Split(':')[3].ToLower();
            List<XElement> oDeviceServiceList = DeviceElements.Where(e => e.Name.LocalName.ToLower() == "servicelist").ToList()[0].Elements().ToList();
            foreach(XElement oDeviceService in oDeviceServiceList)
            {
                UPnPService oService = new UPnPService();
                oService.ControlURL = oDeviceService.Elements().Where(e => e.Name.LocalName.ToLower() == "controlurl").ToList()[0].Value.ToString();
                oService.ServiceType = oDeviceService.Elements().Where(e => e.Name.LocalName.ToLower() == "servicetype").ToList()[0].Value.ToString().Split(':')[3];
                oService.ServiceID = oDeviceService.Elements().Where(e => e.Name.LocalName.ToLower() == "serviceid").ToList()[0].Value.ToString().Split(':')[3];
                oService.SCPDURL = oDeviceService.Elements().Where(e => e.Name.LocalName.ToLower() == "scpdurl").ToList()[0].Value.ToString();
                oService.EventSubURL = oDeviceService.Elements().Where(e => e.Name.LocalName.ToLower() == "eventsuburl").ToList()[0].Value.ToString();
                oDevice.DeviceMethods.Add(oService);
            }
            return oDevice;
        }


    }
    
}
