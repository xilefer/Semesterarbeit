using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeMediaApp.Classes;
using System.Xml.Linq;
using System.Net;
using System.IO;
using Sockets.Plugin;
using Sockets.Plugin.Abstractions;


namespace HomeMediaApp.Classes
{
    public delegate void PlayingStatusChangedEvent();
    public class PlayerControl
    {

        // public int CurrentPosition() (-1 wenns net spielt, sonst die aktuelle sekundenzahl)
        // public bool IsPlaying; Erledigt
        public event PlayingStatusChangedEvent PlayingStatusChanged;
        public UPnPDevice oDevice { get; set; }
        public List<MediaObject> MediaList { get; set; } = new List<MediaObject>();
        public MediaObject CurrentMedia { get; set; }
        public MediaObject NextMedia { get; set; }
        public MediaObject PreviousMedia { get; set; }
        public string Status { get; set; }
        private int LastPosition { get; set; }
        private bool PlayingReponse { get; set; }
        private bool Playing { get; set; } = false;
        private bool PositionResponse { get; set; } = false;
        public bool IsPlaying
        {
            get
            {
                //return true;
                UPnPService oTransportService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "avtransport").ToList()[0];
                UPnPAction oPlayingAction = oTransportService.ActionList.Where(e => e.ActionName.ToLower() == "gettransportinfo").ToList()[0];
                oPlayingAction.OnResponseReceived += new ResponseReceived(OnResponsePlaying);

                string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oTransportService.ControlURL;

                List<Tuple<string, string>> args = new List<Tuple<string, string>>();
                args.Add(new Tuple<string, string>("InstanceID", "0"));
                PlayingReponse = false;
                oPlayingAction.Execute(RequestURI, "AVTransport", args);
                while(PlayingReponse == false)
                { }
                PlayingReponse = false;
                return Playing;
            }
            set
            {
                if (Playing == value) return;
                Playing = value;
                PlayingStatusChanged?.Invoke();
            }
        }
        public void RequestCallback(IAsyncResult oResult)
        {
            ActionState oState = (ActionState)oResult.AsyncState;

            oState.oWebResponse = (HttpWebResponse)oState.oWebRequest.EndGetResponse(oResult);

            Stream st = oState.oWebResponse.GetResponseStream();
            MemoryStream ms = new MemoryStream();
            st.CopyTo(ms);
            string oResponse = Encoding.UTF8.GetString(ms.ToArray(), 0, (int)ms.Length);
            TcpSocketListener oSocket = new TcpSocketListener();

            oSocket.StartListeningAsync(8080);
            oSocket.ConnectionReceived += TCPRec;
        }
        private void TCPRec(object sender, TcpSocketListenerConnectEventArgs args)
        {
            ITcpSocketClient oClient = args.SocketClient;
            byte[] bytes = new byte[64*1024];
            oClient.ReadStream.Read(bytes, 0, bytes.Length);
            string Message = Encoding.UTF8.GetString(bytes,0,bytes.Length);
            string xmlmessage = Message.Substring(Message.IndexOf("<?")).Trim(System.Convert.ToChar(System.Convert.ToUInt32("00", 16)));
            XDocument Response = XDocument.Parse(xmlmessage);
            XElement oElement = Response.Root.Elements().Elements().FirstOrDefault();
            if(oElement != null)
            {
                XDocument Eventinfo = XDocument.Parse(oElement.Value);
                XElement oTransportState = Eventinfo.Elements().Where(e => e.Name.LocalName.ToLower() == "transportstate").FirstOrDefault();
                if(oTransportState != null)
                {
                    if (oTransportState.Value.ToLower() == "playing") IsPlaying = true;
                    else IsPlaying = false;
                }
                XElement oCurrentURI = Eventinfo.Elements().Where(e => e.Name.LocalName.ToLower() == "avtransporturi").FirstOrDefault();
                if(oCurrentURI != null)
                {
                    if(oCurrentURI.Value != CurrentMedia.Path)
                    {
                        CurrentMedia = MediaList.Where(e => e.Path == oCurrentURI.Value).FirstOrDefault();
                        if (MediaList.IndexOf(CurrentMedia) -1 >= 0) PreviousMedia = MediaList[MediaList.IndexOf(CurrentMedia) -1];
                        else PreviousMedia = CurrentMedia;
                        if (MediaList.IndexOf(CurrentMedia) + 1 != MediaList.Count) NextMedia = MediaList[MediaList.IndexOf(CurrentMedia) + 1];
                        else NextMedia = CurrentMedia;
                        SetNextMedia(NextMedia);
                    }
                }
            }
            
        }
        public PlayerControl(UPnPDevice RendererDevice, MediaObject Media)
        {
            
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create("http://129.144.51.117:50148/MediaRenderer_AVTransport/event");
            httpRequest.Method = "SUBSCRIBE";
            httpRequest.Headers["CALLBACK"] = "http://129.144.51.103:8080/"; 
            httpRequest.Headers["NT"] = "upnp:event";
            httpRequest.Headers["TIMEOUT"] = "Second-300";
            ActionState oState = new ActionState()
            {
                oWebRequest = httpRequest
            };
            httpRequest.BeginGetResponse(RequestCallback, oState);
            
            this.CurrentMedia = Media;
            this.oDevice = RendererDevice;
            this.NextMedia = this.CurrentMedia;
            this.PreviousMedia = this.CurrentMedia;
            MediaList.Add(Media);
            //Verbindung zum Gerät aufbauen
            //Entsprechende Action des Gerätes finden
            UPnPService oTransportService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "avtransport").ToList()[0];
            UPnPAction oTransportAction = oTransportService.ActionList.Where(e => e.ActionName.ToLower() == "setavtransporturi").ToList()[0];
            oTransportAction.OnResponseReceived += new ResponseReceived(OnResponseSetAVTransportURI);

            //Pfad setzen
            string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oTransportService.ControlURL;
            List<Tuple<string, string>> args = new List<Tuple<string, string>>();
            args.Add(new Tuple<string, string>("InstanceID", "0"));
            args.Add(new Tuple<string, string>("CurrentURI", CurrentMedia.Path));
            args.Add(new Tuple<string, string>("CurrentURIMetaData", CurrentMedia.MetaData));

            oTransportAction.Execute(RequestURI, "AVTransport", args);
            //Play(0);
            //Event abonnieren
            //Play ausführen
            //Status aktualisieren
        }
        public int GetCurrentPosition()
        {
            UPnPService oTransportService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "avtransport").ToList()[0];
            UPnPAction oPositionAction = oTransportService.ActionList.Where(e => e.ActionName.ToLower() == "getpositioninfo").ToList()[0];
            oPositionAction.OnResponseReceived += new ResponseReceived(OnResponsePosition);

            string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oTransportService.ControlURL;

            List<Tuple<string, string>> args = new List<Tuple<string, string>>();
            args.Add(new Tuple<string, string>("InstanceID", "0"));

            oPositionAction.Execute(RequestURI, "AVTransport", args);
            PositionResponse = false;
            while (PositionResponse == false)
            { }
            PositionResponse = false;
            return LastPosition;
        }
        public void  Playable()
        {
            UPnPService oTransportService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "avtransport").ToList()[0];
            UPnPAction oTransportAction = oTransportService.ActionList.Where(e => e.ActionName.ToLower() == "getcurrenttransportactions").ToList()[0];
            oTransportAction.OnResponseReceived += new ResponseReceived(OnResponseGetCurrentTransportAction);

            string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oTransportService.ControlURL;

            List<Tuple<string, string>> args = new List<Tuple<string, string>>();
            args.Add(new Tuple<string, string>("InstanceID", "0"));

            oTransportAction.Execute(RequestURI, "AVTransport", args);
        }
        public bool Pause()
        {
            //Pause Action ausführen.
            //Status abrufen und aktualisieren
            UPnPService oTransportService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "avtransport").ToList()[0];
            UPnPAction oPlayAction = oTransportService.ActionList.Where(e => e.ActionName.ToLower() == "pause").ToList()[0];
            oPlayAction.OnResponseReceived += new ResponseReceived(OnResponseReceived);

            string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oTransportService.ControlURL;

            List<Tuple<string, string>> args = new List<Tuple<string, string>>();
            args.Add(new Tuple<string, string>("InstanceID", "0"));

            oPlayAction.Execute(RequestURI, "AVTransport", args);

            return true;
        }
        public bool Play()
        {
            //Play mit Index ausführen
            UPnPService oTransportService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "avtransport").ToList()[0];
            UPnPAction oPlayAction = oTransportService.ActionList.Where(e => e.ActionName.ToLower() == "play").ToList()[0];
            oPlayAction.OnResponseReceived += new ResponseReceived(OnResponsePlay);

            string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oTransportService.ControlURL;

            List<Tuple<string, string>> args = new List<Tuple<string, string>>();
            args.Add(new Tuple<string, string>("InstanceID", "0"));
            args.Add(new Tuple<string, string>("Speed", "1"));

            oPlayAction.Execute(RequestURI, "AVTransport", args);
            /*
            MediaObject oCurrent = MediaList.Where(e => e.Index == Index).ToList().FirstOrDefault();
            if (oCurrent != null) CurrentMedia = oCurrent;
            else return false;
            MediaObject oNext = MediaList.Where(e => e.Index == Index + 1).ToList().FirstOrDefault();
            if (oNext != null) NextMedia = oNext;
            else NextMedia = CurrentMedia;
            MediaObject oPrev = MediaList.Where(e => e.Index == Index -1).ToList().FirstOrDefault();
            if (oPrev != null) PreviousMedia = oPrev;
            else PreviousMedia = CurrentMedia;*/
            return true;
        }
        public bool Next()
        {
            if (IsPlaying) Stop();
            while (IsPlaying) { };
            SetCurrentMedia(NextMedia);
            return true;
        }
        public bool Previous()
        {
            if (IsPlaying) Stop();
            while (IsPlaying) { };
            SetCurrentMedia(PreviousMedia);
            return true;
        }
        public bool Stop()
        {
            UPnPService oTransportService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "avtransport").ToList()[0];
            UPnPAction oPlayAction = oTransportService.ActionList.Where(e => e.ActionName.ToLower() == "stop").ToList()[0];
            oPlayAction.OnResponseReceived += new ResponseReceived(OnResponseStop);

            string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oTransportService.ControlURL;
            List<Tuple<string, string>> args = new List<Tuple<string, string>>();
            args.Add(new Tuple<string, string>("InstanceID", "0"));

            oPlayAction.Execute(RequestURI, "AVTransport", args);
            return true;
        }
        public void AddMedia(MediaObject oMedia)
        {
            MediaList.Add(oMedia);
            if(MediaList.IndexOf(CurrentMedia)+1 == MediaList.IndexOf(oMedia))
            {
                NextMedia = oMedia;
                SetNextMedia(oMedia);
            }
        }
        public void SetNextMedia(MediaObject oMedia)
        {
            UPnPService oTransportService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "avtransport").ToList()[0];
            UPnPAction oTransportAction = oTransportService.ActionList.Where(e => e.ActionName.ToLower() == "setnextavtransporturi").ToList()[0];
            oTransportAction.OnResponseReceived += new ResponseReceived(OnResponseSetNext);

            //Pfad setzen
            string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oTransportService.ControlURL;
            List<Tuple<string, string>> args = new List<Tuple<string, string>>();
            args.Add(new Tuple<string, string>("InstanceID", "0"));
            args.Add(new Tuple<string, string>("NextURI", oMedia.Path));
            args.Add(new Tuple<string, string>("NextURIMetaData", oMedia.MetaData));

            oTransportAction.Execute(RequestURI, "AVTransport", args);
            
        }
        public void SetCurrentMedia(MediaObject oMedia)
        {
            UPnPService oTransportService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "avtransport").ToList()[0];
            UPnPAction oTransportAction = oTransportService.ActionList.Where(e => e.ActionName.ToLower() == "setavtransporturi").ToList()[0];
            oTransportAction.OnResponseReceived += new ResponseReceived(OnResponseSetCurrent);

            //Pfad setzen
            string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oTransportService.ControlURL;
            List<Tuple<string, string>> args = new List<Tuple<string, string>>();
            args.Add(new Tuple<string, string>("InstanceID", "0"));
            args.Add(new Tuple<string, string>("CurrentURI", oMedia.Path));
            args.Add(new Tuple<string, string>("CurrentURIMetaData", oMedia.MetaData));

            oTransportAction.Execute(RequestURI, "AVTransport", args);
        }
        public void OnResponsePlaying(XDocument oResponseDocument, ActionState oState)
        {
            if (oState.oWebResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                XElement TransportState = oResponseDocument.Root.Elements().Elements().Elements().Where(e => e.Name.LocalName.ToLower() == "currenttransportstate").ToList()[0];
                if (TransportState.Value.ToLower() == "playing") IsPlaying = true;
                else IsPlaying = false;
            }
            PlayingReponse = true;

        }
        public void OnResponseReceived(XDocument oResponseDocument, ActionState oState)
        {
            IsPlaying = false;
        }
        public void OnResponseSetAVTransportURI(XDocument oResponseDocument, ActionState oState)
        {
            if (oState.oWebResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                Playable();
            }
            else
            {
                //Fehler
            }
        }
        public void OnResponseGetCurrentTransportAction(XDocument oResponseDocument, ActionState oState)
        {
            if (oState.oWebResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                Play();
            }
            else
            {
                Playable();
            }
        }
        public void OnResponsePlay(XDocument oResponseDocument, ActionState oState)
        {
            if (oState.oWebResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                IsPlaying = true;
                //Gut
            }
        }
        public void OnResponsePosition(XDocument oResponseDocument, ActionState oState)
        {
            if (oState.oWebResponse.StatusCode == System.Net.HttpStatusCode.OK && IsPlaying)
            {
                int Position = int.Parse(oResponseDocument.Root.Elements().Elements().Elements().Where(e => e.Name.LocalName.ToLower() == "abstime").ToList()[0].Value);
                LastPosition = Position;
            }
            else LastPosition = -1;
            PositionResponse = true;
        }
        public void OnResponseNext(XDocument oResponseDocument, ActionState oState)
        {
            if (oState.oWebResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                PreviousMedia = CurrentMedia;
                CurrentMedia = NextMedia;
                MediaObject oNext = MediaList.Where(e => e.Index == CurrentMedia.Index + 1).ToList().FirstOrDefault();
                if (oNext != null) NextMedia = oNext;
            }

        }
        public void OnResponseSetNext(XDocument oResponseDocument, ActionState oState)
        {
            if (oState.oWebResponse.StatusCode == HttpStatusCode.OK) Play();
        }
        public void OnResponseStop(XDocument oResponseDocument, ActionState oState)
        {
            if (oState.oWebResponse.StatusCode == HttpStatusCode.OK) IsPlaying = false;
        }
        public void OnResponseSetCurrent(XDocument oResponseDocument, ActionState oState)
        {
            if (oState.oWebResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                Playable();
            }
        }
    }
}
