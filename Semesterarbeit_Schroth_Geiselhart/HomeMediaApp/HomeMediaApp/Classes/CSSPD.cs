using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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
        private UdpSocketReceiver oSendSocket;
        public event ReceivedXml ReceivedXml;

        /// <summary>
        /// Startet eine Suche nach UPnP-Geräten im Netzwerk über einen SSDP-Broadcast
        /// </summary>
        public void StartSearch()
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
                    sLocation = sAnswerDecomposed.First(e => e.ToUpper().StartsWith("LOCATION:"));
                    HttpWebRequest oHttpWebRequest = WebRequest.CreateHttp(sLocation.Substring(9));
                    oHttpWebRequest.Method = "GET";
                    CSSDPState oState = new CSSDPState()
                    {
                        oWebRequest = oHttpWebRequest
                    };
                    oHttpWebRequest.BeginGetResponse(RequestCallback, oState);

                }
                catch (Exception)
                {
                    // TODO: Logmeldung o.ä.
                    return;
                }
            }
        }

        /// <summary>
        /// Callback der GET-Request, liefert die XML-Konfiguration
        /// </summary>
        /// <param name="oResult">Das IAsyncResult Objekt</param>
        private void RequestCallback(IAsyncResult oResult)
        {
            CSSDPState oState = (CSSDPState) oResult.AsyncState;
            HttpWebRequest oWebRequest = oState.oWebRequest;
            
            oState.oWebResponse = (HttpWebResponse) oWebRequest.EndGetResponse(oResult);
            Stream oResponseStream = oState.oWebResponse.GetResponseStream();
            MemoryStream ms = new MemoryStream();
            oResponseStream.CopyTo(ms);
            string sResponse = Encoding.UTF8.GetString(ms.ToArray(), 0, (int)ms.Length);
            ReceivedXml(XDocument.Parse(sResponse), oState.oWebRequest.RequestUri);
        }
    }
}

