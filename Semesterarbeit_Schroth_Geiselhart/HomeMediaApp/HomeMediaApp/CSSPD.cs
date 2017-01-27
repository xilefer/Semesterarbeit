using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Sockets.Plugin;
using Sockets.Plugin.Abstractions;
using Xamarin.Forms.Xaml;

namespace HomeMediaLibrary
{
    public delegate void ReceivedXml(XDocument oReceivedXml);
    public class CSSPD
    {

        //UdpSocketClient oSendSocket;

        private UdpSocketMulticastClient oSendSocket;
        public event ReceivedXml ReceivedXml;

        /// <summary>
        /// Startet eine Suche nach UPnP-Geräten im Netzwerk über einen SSDP-Broadcast
        /// </summary>
        public void StartSearch()
        {
            oSendSocket = new UdpSocketMulticastClient();
            oSendSocket.MessageReceived += OSendSocketOnMessageReceived;
            
            //oSendSocket = new UdpSocketClient();
            //oSendSocket.MessageReceived += OSendSocketOnMessageReceived;
            string sSearchString = "M-SEARCH * HTTP/1.1\r\nHOST:239.255.255.250:1900\r\nMAN:\"ssdp:discover\"\r\nST:ssdp:all\r\nMX:3\r\n\r\n";
            byte[] sSearchBytes = Encoding.UTF8.GetBytes(sSearchString);
            //oSendSocket.ConnectAsync("239.255.255.250", 1900).Wait();
            oSendSocket.JoinMulticastGroupAsync("239.255.255.250", 1900).Wait();
            oSendSocket.SendMulticastAsync(sSearchBytes, sSearchBytes.Length).Wait();
            //Task oTask = oSendSocket.SendToAsync(sSearchBytes, sSearchString.Length, "239.255.255.250", 1900);
            //oTask.Wait();
        }

        /// <summary>
        /// Verarbeitet die erhaltene Antwort und lädt die XML-Konfiguration des entsprechenden Gerätes
        /// </summary>
        /// <param name="sender">Objekt welches das Event ausgelöst hat</param>
        /// <param name="udpSocketMessageReceivedEventArgs">Eigenschaften der erhaltenen Antwort</param>
        private void OSendSocketOnMessageReceived(object sender, UdpSocketMessageReceivedEventArgs udpSocketMessageReceivedEventArgs)
        {
            if ((UdpSocketMulticastClient)sender == oSendSocket)
            {
                string sAnswer = Encoding.UTF8.GetString(udpSocketMessageReceivedEventArgs.ByteData, 0, udpSocketMessageReceivedEventArgs.ByteData.Length);
                List<string> sAnswerDecomposed = sAnswer.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                string sLocation = "";
                try
                {
                    sLocation = sAnswerDecomposed.First(e => e.ToUpper().StartsWith("LOCATION:"));
                }
                catch (Exception oException)
                {
                    return;
                }
                if (sLocation.Length > 0)
                {
                    #region Tut noch nicht
                    
                    HttpClient oHttpClient = new HttpClient();
                    try
                    {
                        Task<string> oTask = oHttpClient.GetStringAsync(sLocation.Substring(9));
                        oTask.Wait();
                        ReceivedXml?.Invoke(XDocument.Load(new StringReader(oTask.Result)));
                    }
                    catch (Exception e)
                    {

                    }
                    
                    #endregion
                }
            }
        }
    }
}

