﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Xml.Linq;
using System.IO;

namespace HomeMediaApp.Classes
{
    public delegate void ResponseReceived(XDocument oResponseDocument, ActionState oState);

    /// <summary>
    /// Klasse zum speichern von Ergebnissen einer Anfrage
    /// </summary>
    public class ActionState
    {
        public HttpWebRequest oWebRequest;
        public HttpWebResponse oWebResponse;
        public string ActionName;
        public byte[] RequestBody;
        public bool Successful = false;
        public string AdditionalInfo = "";
    }

    /// <summary>
    /// Klasse die einen UPnP-Service bereitstellt
    /// </summary>
    public class UPnPService
    {
        public string ServiceType { get; set; }
        public string ServiceID { get; set; }
        public string SCPDURL { get; set; }
        public string ControlURL { get; set; }
        public string EventSubURL { get; set; }
        public List<UPnPAction> ActionList { get; set; } = new List<UPnPAction>();
        public List<UPnPServiceState> ServiceStateTable { get; set; } = new List<UPnPServiceState>();
    }

    /// <summary>
    /// Klasse die eine UPnP-Action verfügbar macht
    /// </summary>
    public class UPnPAction
    {
        public string ActionName { get; set; }
        public List<UPnPActionArgument> ArgumentList { get; set; } = new List<UPnPActionArgument>();
        public XDocument ActionConfig { get; set; }

        public event ResponseReceived OnResponseReceived;

        public void Execute(string ControlURL, string ServiceName, List<Tuple<string, string>> args, string AdditionalInfo)
        {
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(ControlURL);
            httpRequest.Method = "POST";
            httpRequest.ContentType = "text/xml; charset=\"utf-8\"";
            httpRequest.Accept = "text/xml";
            httpRequest.UseDefaultCredentials = true;
            httpRequest.Headers["SOAPACTION"] = "\"urn:schemas-upnp-org:service:" + ServiceName + ":1#" + this.ActionName + "\"";
            StringBuilder soapRequest = new StringBuilder();
            soapRequest.AppendLine(@"<?xml version=""1.0"" encoding=""utf-8""?>");
            soapRequest.AppendLine(@"<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"" s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">");
            soapRequest.AppendLine(@"<s:Body>");
            soapRequest.AppendLine("<m:" + this.ActionName + " xmlns:m=\"urn:schemas-upnp-org:service:" + ServiceName + ":1\">");
            foreach (Tuple<string, string> item in args)
            {
                soapRequest.AppendLine("<" + item.Item1 + ">" + item.Item2 + "</" + item.Item1 + ">");
            }
            soapRequest.AppendLine("</m:" + this.ActionName + ">");
            soapRequest.AppendLine("</s:Body>");
            soapRequest.AppendLine("</s:Envelope>");
            byte[] bytes = Encoding.UTF8.GetBytes(soapRequest.ToString());
            ActionState oState = new ActionState()
            {
                oWebRequest = httpRequest,
                ActionName = this.ActionName,
                RequestBody = bytes,
                AdditionalInfo = AdditionalInfo
            };
            httpRequest.BeginGetRequestStream(RequestCallback, oState);
        }

        public void Execute(string ControlURL,string ServiceName,List<Tuple<string,string>> args)
        {
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(ControlURL);
            httpRequest.Method = "POST";
            httpRequest.ContentType = "text/xml; charset=\"utf-8\"";
            httpRequest.Accept = "text/xml";
            //httpRequest.Headers["Connection"] = "\"close\"";
            httpRequest.UseDefaultCredentials = true;
            httpRequest.Headers["SOAPAction"] = "\"urn:schemas-upnp-org:service:"+ ServiceName + ":1#" + this.ActionName +"\"";
            StringBuilder soapRequest = new StringBuilder();
            soapRequest.AppendLine(@"<?xml version=""1.0"" encoding=""utf-8""?>");
            soapRequest.AppendLine(@"<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"" s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">");
            soapRequest.AppendLine(@"<s:Body>");
            soapRequest.AppendLine("<m:" + this.ActionName + " xmlns:m=\"urn:schemas-upnp-org:service:" + ServiceName + ":1\">");
            foreach(Tuple<string,string> item in args)
            {
                soapRequest.AppendLine("<" + item.Item1 + ">" + item.Item2 + "</" + item.Item1 + ">");
            }
            soapRequest.AppendLine("</m:" + this.ActionName + ">");
            soapRequest.AppendLine("</s:Body>");
            soapRequest.AppendLine("</s:Envelope>");
            byte[] bytes = Encoding.UTF8.GetBytes(soapRequest.ToString());
            ActionState oState = new ActionState()
            {
                oWebRequest = httpRequest,
                ActionName = this.ActionName,
                RequestBody = bytes
            };
            httpRequest.BeginGetRequestStream(RequestCallback, oState);
        }

        private void RequestCallback(IAsyncResult oResult)
        {
            ActionState oState = (ActionState)oResult.AsyncState;
            byte[] bytes = oState.RequestBody;
            try
            {
                using (Stream oResponseStream = oState.oWebRequest.EndGetRequestStream(oResult))
                {
                    oResponseStream.Write(bytes, 0, bytes.Length);
                }
                oState.oWebRequest.BeginGetResponse(ResponseCallback, oState);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                // Wir hatten einen Netzwerkfehler! Eventuell wartet Aufrufer auf Antwort, deshalb event trotzdem auslösen
                OnResponseReceived(null, oState);
            }
            
        }

        private void ResponseCallback(IAsyncResult oResult)
        {
            ActionState oState = (ActionState)oResult.AsyncState;
            try
            {
                oState.oWebResponse = (HttpWebResponse)oState.oWebRequest.EndGetResponse(oResult);
            }
            catch (Exception gEx)
            {
                Debug.WriteLine(gEx);
                OnResponseReceived?.Invoke(new XDocument(), oState);
                return;
            }
            Stream st = oState.oWebResponse.GetResponseStream();
            MemoryStream ms = new MemoryStream();
            st.CopyTo(ms);
            string oResponse = Encoding.UTF8.GetString(ms.ToArray(), 0, (int)ms.Length);
            oState.Successful = true;
            OnResponseReceived?.Invoke(XDocument.Parse(oResponse), oState);
        }
    }

    /// <summary>
    /// Argumente die für UPnP-Action benötigt werden
    /// </summary>
    public class UPnPActionArgument
    {
        public string Name { get; set; }
        public string Direction { get; set; }
        public string RelatedStateVariable { get; set; }
    }

    /// <summary>
    /// Klasse die Eigenschaften zu einem UPnP-Service bereitstellt
    /// </summary>
    public class UPnPServiceState
    {
        public bool SendEvents { get; set; }
        public string Name { get; set; }
        public string DataType { get; set; }
    }
}
