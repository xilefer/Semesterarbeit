using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Net;
using System.IO;

namespace HomeMediaApp.Classes
{
    public delegate void DeviceFinished(UPnPDevice oDevice, UPnPService oService);

    public class RequestState
    {
        public HttpWebRequest oWebRequest;
        public HttpWebResponse oWebResponse;
        public UPnPService oServiceCallback;
        public UPnPDevice oDevice;
    }
    public class XMLParser
    {
        //private UPnPDevice oDevice;

        public event DeviceFinished DeviceFinished;
        public UPnPDevice Parse(UPnPDevice oInputDevice)
        {
            UPnPDevice oDevice = new UPnPDevice();
            List<UPnPService> oServiceList = new List<UPnPService>();
            List<XElement> DeviceElements = oInputDevice.Config.Root.Elements().Where(e=> e.Name.LocalName.ToLower() == "device").ToList()[0].Elements().ToList();
            oDevice.Config = oInputDevice.Config;
            oDevice.DeviceAddress = oInputDevice.DeviceAddress;
            oDevice.DeviceName = oInputDevice.DeviceName;
            oDevice.Type = DeviceElements.Where(e => e.Name.LocalName.ToLower() == "devicetype").ToList()[0].Value.ToString().Split(':')[3].ToLower();
            if(oDevice.Type.ToLower() == "mediarenderer" || oDevice.Type.ToLower() == "mediaserver")
            {
                List<XElement> oDeviceServiceList = DeviceElements.Where(e => e.Name.LocalName.ToLower() == "servicelist").ToList()[0].Elements().ToList();
                foreach (XElement oDeviceService in oDeviceServiceList)
                {
                    UPnPService oService = new UPnPService();
                    oService.ControlURL = oDeviceService.Elements().Where(e => e.Name.LocalName.ToLower() == "controlurl").ToList()[0].Value.ToString();
                    oService.ServiceType = oDeviceService.Elements().Where(e => e.Name.LocalName.ToLower() == "servicetype").ToList()[0].Value.ToString().Split(':')[3];
                    oService.ServiceID = oDeviceService.Elements().Where(e => e.Name.LocalName.ToLower() == "serviceid").ToList()[0].Value.ToString().Split(':')[3];
                    oService.SCPDURL = oDeviceService.Elements().Where(e => e.Name.LocalName.ToLower() == "scpdurl").ToList()[0].Value.ToString();
                    oService.EventSubURL = oDeviceService.Elements().Where(e => e.Name.LocalName.ToLower() == "eventsuburl").ToList()[0].Value.ToString();
                    oServiceList.Add(oService);
                }
                foreach (UPnPService oService in oServiceList)
                {
                    if (!oService.SCPDURL.StartsWith(@"/"))
                    {
                        oService.SCPDURL = @"/" + oService.SCPDURL;
                    }
                    string RequestString = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oService.SCPDURL;
                    HttpWebRequest oHttpWebRequest = WebRequest.CreateHttp(RequestString);
                    oHttpWebRequest.Method = "GET";
                    RequestState oState = new RequestState()
                    {
                        oWebRequest = oHttpWebRequest,
                        oServiceCallback = oService,
                        oDevice = oDevice
                    };
                    oHttpWebRequest.BeginGetResponse(RequestCallback, oState);
                }
            }
            return oDevice;
        }
        private void RequestCallback(IAsyncResult oResult)
        {
            RequestState oState = (RequestState)oResult.AsyncState;
            HttpWebRequest oWebRequest = oState.oWebRequest;

            oState.oWebResponse = (HttpWebResponse)oWebRequest.EndGetResponse(oResult);
            Stream oResponseStream = oState.oWebResponse.GetResponseStream();
            MemoryStream ms = new MemoryStream();
            oResponseStream.CopyTo(ms);
            string sResponse = Encoding.UTF8.GetString(ms.ToArray(), 0, (int)ms.Length);
            XDocument oActionDescription = XDocument.Parse(sResponse);
            UPnPAction oAction = new UPnPAction();
            oAction.ActionConfig = oActionDescription;
            oState.oServiceCallback.ActionList.Add(oAction);
            DeviceFinished(oState.oDevice, oState.oServiceCallback);
        }

    }
    
}
