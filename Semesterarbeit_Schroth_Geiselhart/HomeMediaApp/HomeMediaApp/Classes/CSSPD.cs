using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Sockets.Plugin;
using Sockets.Plugin.Abstractions;
using Xamarin.Forms.Xaml;
using System.Xml;
using HomeMediaApp.Classes;

namespace HomeMediaApp
{
    public delegate void ReceivedXml(XDocument oReceivedXml, Uri oDeviceAddress);

    public class CSSDPState
    {
        public HttpWebRequest oWebRequest;
        public HttpWebResponse oWebResponse;
    }

    public class CSSPD
    {
        private List<string> ReceivedUDN = new List<string>();
        private UdpSocketReceiver oSendSocket;
        public event ReceivedXml ReceivedXml;
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Startet eine Suche nach UPnP-Geräten im Netzwerk über einen SSDP-Broadcast
        /// </summary>
        public void StartSearch()
        {
            try
            {
                Task.Factory.StartNew(() =>
                {
                    StartSearchBackground();
                }, cancellationTokenSource.Token);
            }
            catch (Exception gEx)
            {
                Debug.WriteLine(gEx.ToString());
            }
        }

        public void StopSearch()
        {
            cancellationTokenSource.Cancel();
        }

        private void StartSearchBackground()
        {
            oSendSocket = new UdpSocketReceiver();
            oSendSocket.MessageReceived += OSendSocketOnMessageReceived;
            string sSearchString = "M-SEARCH * HTTP/1.1\r\nHOST:239.255.255.250:1900\r\nMAN:\"ssdp:discover\"\r\nST:ssdp:all\r\nMX:3\r\n\r\n";
            byte[] sSearchBytes = Encoding.UTF8.GetBytes(sSearchString);
            oSendSocket.StartListeningAsync(0).Wait();
            oSendSocket.SendToAsync(sSearchBytes, sSearchBytes.Length, "239.255.255.250", 1900);
        }

        /// <summary>
        /// Verarbeitet die erhaltene Antwort und lädt die XML-Konfiguration des entsprechenden Gerätes
        /// </summary>
        /// <param name="sender">Objekt welches das Event ausgelöst hat</param>
        /// <param name="udpSocketMessageReceivedEventArgs">Eigenschaften der erhaltenen Antwort</param>
        private void OSendSocketOnMessageReceived(object sender, UdpSocketMessageReceivedEventArgs udpSocketMessageReceivedEventArgs)
        {
            if ((UdpSocketReceiver)sender == oSendSocket)
            {
                string sAnswer = Encoding.UTF8.GetString(udpSocketMessageReceivedEventArgs.ByteData, 0, udpSocketMessageReceivedEventArgs.ByteData.Length);
                List<string> sAnswerDecomposed = sAnswer.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                string sLocation = "";
                try
                {
                    if (sAnswerDecomposed.Where(e => e.ToUpper().StartsWith("LOCATION:")).ToList().Count > 0)
                    {
                        sLocation = sAnswerDecomposed.First(e => e.ToUpper().StartsWith("LOCATION:"));
                        if (!sLocation.Contains("http://"))
                        {
                            sLocation = "http://" + sLocation.Substring(9);
                            sLocation = "LOCATION: " + sLocation;
                        }
                        HttpWebRequest oHttpWebRequest = WebRequest.CreateHttp(sLocation.Substring(9));
                        oHttpWebRequest.Method = "GET";
                        CSSDPState oState = new CSSDPState()
                        {
                            oWebRequest = oHttpWebRequest
                        };
                        oHttpWebRequest.BeginGetResponse(RequestCallback, oState);
                    }
                }
                catch (Exception gEx)
                {
                    throw gEx;
                }
            }
        }

        /// <summary>
        /// Callback der GET-Request, liefert die XML-Konfiguration
        /// </summary>
        /// <param name="oResult">Das IAsyncResult Objekt</param>
        private void RequestCallback(IAsyncResult oResult)
        {
            CSSDPState oState = (CSSDPState)oResult.AsyncState;
            HttpWebRequest oWebRequest = oState.oWebRequest;

            try
            {
                oState.oWebResponse = (HttpWebResponse)oWebRequest.EndGetResponse(oResult);
            }
            catch (Exception gEx)
            {
                Debug.WriteLine(gEx);
                return;
            }
            Stream oResponseStream = oState.oWebResponse.GetResponseStream();
            MemoryStream ms = new MemoryStream();
            oResponseStream.CopyTo(ms);
            string sResponse = Encoding.UTF8.GetString(ms.ToArray(), 0, (int)ms.Length);
            XDocument ReceivedXML = XDocument.Parse(sResponse);
            List<XElement> Devices = ReceivedXML.Descendants().Where(e => e.Name.LocalName.ToLower() == "device").ToList();
            List<XElement> DevicesList = ReceivedXML.Descendants().Where(e => e.Name.LocalName.ToLower() == "devicelist").ToList();
            if (DevicesList.Count > 0)
            {
                Devices = Devices.Descendants().Where(e => e.Name.LocalName.ToLower() == "device").ToList();
            }
            //List<XElement> Devices =
              //  ReceivedXML.Descendants().Where(e => e.Name.LocalName.ToLower() == "device").ToList();
            foreach (XElement DeviceXML in Devices)
            {
                List<XElement> UDNs = DeviceXML.Descendants().Where(Desc => Desc.Name.LocalName.ToLower() == "udn").ToList();
                if (UDNs.Count > 0)
                {
                    bool OneNewUDN = false;
                    if (Monitor.TryEnter(ReceivedUDN))
                    {
                        try
                        {
                            foreach (var xElement in UDNs)
                            {
                                if (!ReceivedUDN.Contains(xElement.Value))
                                {
                                    ReceivedUDN.Add(xElement.Value);
                                    OneNewUDN = true;
                                }
                            }
                        }
                        finally
                        {
                            Monitor.Exit(ReceivedUDN);
                        }
                        if (OneNewUDN) ReceivedXml(XDocument.Parse("<root xmlns=\"urn: schemas - upnp - org:device - 1 - 0\">" + DeviceXML.ToString() + "</root>"), oState.oWebRequest.RequestUri);
                    }
                }
            }
            
        }
    }
}

