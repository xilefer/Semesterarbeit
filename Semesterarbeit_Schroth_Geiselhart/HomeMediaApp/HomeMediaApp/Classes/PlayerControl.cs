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
using System.Net.NetworkInformation;
using HomeMediaApp.Interfaces;
using Xamarin.Forms;


namespace HomeMediaApp.Classes
{
    public delegate void PlayingStatusChangedEvent();
    public delegate void MuteStatusChangedEvent();
    public delegate void VolumeValueChangedEvent();
    public class PlayerControl
    {
        // public int CurrentPosition() (-1 wenns net spielt, sonst die aktuelle sekundenzahl)
        // public bool IsPlaying; Erledigt
        private string ListenPortAV = "5000";
        private string ListenPortRenderer = "5001";
        public event PlayingStatusChangedEvent PlayingStatusChanged;
        public event MuteStatusChangedEvent MuteStatusChanged;
        public event VolumeValueChangedEvent VolumeValueChanged;
        public UPnPDevice oDevice { get; set; }
        public List<MediaObject> MediaList { get; set; } = new List<MediaObject>();
        public MediaObject CurrentMedia { get; set; }
        public MediaObject NextMedia { get; set; }
        public MediaObject PreviousMedia { get; set; }
        public string Status { get; set; }
        public int CurrentVolume
        {
            get { return GetVolume(); }
            set
            {
                if (value >= 0 && value <= 100) SetVolume(value);
                else return;
            }
        }
        public bool Mute
        {
            get { return GetMute(); }
            set { SetMute(value);   }
        }
        private bool Mutevalue { get; set; }
        private bool TempMute { get; set; } = false;
        private bool GetMuteResponse { get; set; } = false;
        public int Volume
        {
            get { return GetVolume(); }
            set { SetVolume(value); }
        }
        private int Volumevalue { get; set; }  = 0;
        private int TempVolume { get; set; } = 0;
        private bool GetVolumeResponse { get; set; } = false;
        private int LastPosition { get; set; }
        private bool PlayingReponse { get; set; }
        private bool Playing { get; set; } = false;
        private bool PositionResponse { get; set; } = false;
        private bool CurrentIDResponse { get; set; } = false;
        public bool ConnectionSuccessful { get; set; } = false;
        private TcpSocketListener oSocket { get; set; }
        private TcpSocketListener oSocketRenderer { get; set; }
        public bool ConnectionError { get; set; } = false;
        private XDocument CurrentIDResponseDoc { get; set; }




        public PlayerControl(UPnPDevice RendererDevice, MediaObject Media)
        {
            this.CurrentMedia = Media;
            this.oDevice = RendererDevice;
            this.NextMedia = this.CurrentMedia;
            this.PreviousMedia = this.CurrentMedia;
            MediaList.Add(Media);

            //Überprüfen ob bereits eine Verbindung besteht
            //---------------------------------------------------------------------------------------------------------------------------------------//

            UPnPService oConnectionService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "connectionmanager").FirstOrDefault();
            UPnPAction oCurrentIDs = oConnectionService.ActionList.Where(e => e.ActionName.ToLower() == "getcurrentconnectionids").FirstOrDefault();
            oCurrentIDs.OnResponseReceived += new ResponseReceived(OnResponseCurrentIDs);
            List<Tuple<string, string>> argsid = new List<Tuple<string, string>>();
            CurrentIDResponse = false;
            oCurrentIDs.Execute(oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oConnectionService.ControlURL, "ConnectionManager", argsid);
            while (CurrentIDResponse == false) { };
            XElement ConnectionIDElement = CurrentIDResponseDoc.Root.Elements().Elements().Elements().Where(e => e.Name.LocalName.ToLower() == "connectionids").FirstOrDefault();
            string ConnectionIDs = "";
            if (ConnectionIDElement != null) { ConnectionIDs = ConnectionIDElement.Value; }

            if (ConnectionIDs != "")
            {
                Stop();
            }
            //---------------------------------------------------------------------------------------------------------------------------------------//
            //


            //Abonnieren der Events von AVTransport und RenderingControl
            //---------------------------------------------------------------------------------------------------------------------------------------//
            UPnPService oTransportService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "avtransport").FirstOrDefault();
            UPnPService oRendererService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "renderingcontrol").FirstOrDefault();

            if (oTransportService == null) return;
            string IP = DependencyService.Get<IGetDeviceIPAddress>().GetDeviceIP();
            if (IP != null)
            {

                string EventURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oTransportService.EventSubURL;
                HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(EventURI);
                httpRequest.Method = "SUBSCRIBE";
                httpRequest.Headers["CALLBACK"] = "http://" + IP + ":" + ListenPortAV + "/";
                httpRequest.Headers["NT"] = "upnp:event";
                httpRequest.Headers["TIMEOUT"] = "Second-300";
                ActionState oState = new ActionState()
                {
                    oWebRequest = httpRequest
                };
                httpRequest.BeginGetResponse(RequestCallback, oState);
                if(oRendererService != null)
                {
                    string EventURIRenderer = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oRendererService.EventSubURL;
                    HttpWebRequest httpRequestRenderer = (HttpWebRequest)WebRequest.Create(EventURI);
                    httpRequestRenderer.Method = "SUBSCRIBE";
                    httpRequestRenderer.Headers["CALLBACK"] = "http://" + IP + ":" + ListenPortRenderer + "/";
                    httpRequestRenderer.Headers["NT"] = "upnp:event";
                    httpRequestRenderer.Headers["TIMEOUT"] = "Second-300";
                    ActionState oStateRenderer = new ActionState()
                    {
                        oWebRequest = httpRequestRenderer
                    };
                    httpRequestRenderer.BeginGetResponse(RequestCallbackRenderer, oStateRenderer);
                }
            }
            //---------------------------------------------------------------------------------------------------------------------------------------//
            //


            //Verbindung zum Gerät aufbauen
            //---------------------------------------------------------------------------------------------------------------------------------------//

            UPnPAction oTransportAction = oTransportService.ActionList.Where(e => e.ActionName.ToLower() == "setavtransporturi").ToList()[0];
            oTransportAction.OnResponseReceived += new ResponseReceived(OnResponseSetAVTransportURI);

            //Pfad setzen
            string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oTransportService.ControlURL;
            List<Tuple<string, string>> args = new List<Tuple<string, string>>();
            args.Add(new Tuple<string, string>("InstanceID", "0"));
            args.Add(new Tuple<string, string>("CurrentURI", CurrentMedia.Path));
            args.Add(new Tuple<string, string>("CurrentURIMetaData", CurrentMedia.MetaData));

            ConnectionSuccessful = false;
            oTransportAction.Execute(RequestURI, "AVTransport", args);
            while (!ConnectionSuccessful && !ConnectionError) { };
            
            //---------------------------------------------------------------------------------------------------------------------------------------//
            //
        }
        public bool IsPlaying
        {
            get
            {
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
        private void RequestCallback(IAsyncResult oResult)
        {
            ActionState oState = (ActionState)oResult.AsyncState;

            oState.oWebResponse = (HttpWebResponse)oState.oWebRequest.EndGetResponse(oResult);

            Stream st = oState.oWebResponse.GetResponseStream();
            MemoryStream ms = new MemoryStream();
            st.CopyTo(ms);
            string oResponse = Encoding.UTF8.GetString(ms.ToArray(), 0, (int)ms.Length);
            oSocket = new TcpSocketListener();

            oSocket.StartListeningAsync(int.Parse(ListenPortAV));
            oSocket.ConnectionReceived += TCPRec;
        }
        private void RequestCallbackRenderer(IAsyncResult oResult)
        {
            ActionState oState = (ActionState)oResult.AsyncState;

            oState.oWebResponse = (HttpWebResponse)oState.oWebRequest.EndGetResponse(oResult);

            Stream st = oState.oWebResponse.GetResponseStream();
            MemoryStream ms = new MemoryStream();
            st.CopyTo(ms);
            string oResponse = Encoding.UTF8.GetString(ms.ToArray(), 0, (int)ms.Length);
            oSocketRenderer = new TcpSocketListener();

            oSocketRenderer.StartListeningAsync(int.Parse(ListenPortRenderer));
            oSocketRenderer.ConnectionReceived += TCPRecRend;
        }
        private void TCPRecRend(object sender, TcpSocketListenerConnectEventArgs args)
        {
            ITcpSocketClient oClient = args.SocketClient;
            byte[] bytes = new byte[64 * 1024];
            oClient.ReadStream.Read(bytes, 0, bytes.Length);
            string Message = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            string xmlmessage = Message.Substring(Message.IndexOf("<?")).Trim(System.Convert.ToChar(System.Convert.ToUInt32("00", 16)));
            XDocument Response = XDocument.Parse(xmlmessage);
            XElement oElement = Response.Root.Elements().Elements().FirstOrDefault();
            if (oElement != null)
            {
                XDocument Eventinfo = XDocument.Parse(oElement.Value);
                XElement oMuteState = Eventinfo.Elements().Where(e => e.Name.LocalName.ToLower() == "mute").FirstOrDefault();
                if (oMuteState != null)
                {
                    if (oMuteState.Value.ToLower() != Mutevalue.ToString()) MuteStatusChanged?.Invoke();
                }
                XElement oVolume = Eventinfo.Elements().Where(e => e.Name.LocalName.ToLower() == "volume").FirstOrDefault();
                if (oVolume != null)
                {
                    if (oVolume.Value != Volumevalue.ToString()) VolumeValueChanged?.Invoke();
                }
            }
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
        private int GetVolume()
        {
            UPnPService oRendererService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "renderingcontrol").ToList()[0];
            UPnPAction oTransportAction = oRendererService.ActionList.Where(e => e.ActionName.ToLower() == "getvolume").ToList()[0];
            oTransportAction.OnResponseReceived += new ResponseReceived(OnResponseSetCurrent);

            //Pfad setzen
            string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oRendererService.ControlURL;
            List<Tuple<string, string>> args = new List<Tuple<string, string>>();
            args.Add(new Tuple<string, string>("InstanceID", "0"));
            args.Add(new Tuple<string, string>("Channel", "Master"));
            GetVolumeResponse = false;
            oTransportAction.Execute(RequestURI, "RenderingControl", args);
            while (!GetVolumeResponse) { };
            GetVolumeResponse = false;
            return Volumevalue;
        }
        private void SetVolume(int Volume)
        {
            UPnPService oRendererService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "renderingcontrol").ToList()[0];
            UPnPAction oTransportAction = oRendererService.ActionList.Where(e => e.ActionName.ToLower() == "setvolume").ToList()[0];
            oTransportAction.OnResponseReceived += new ResponseReceived(OnResponseSetVolume);

            //Pfad setzen
            string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oRendererService.ControlURL;
            List<Tuple<string, string>> args = new List<Tuple<string, string>>();
            args.Add(new Tuple<string, string>("InstanceID", "0"));
            args.Add(new Tuple<string, string>("Channel", "Master"));
            args.Add(new Tuple<string, string>("DesiredVolume", Volume.ToString()));
            TempVolume = Volume;
            oTransportAction.Execute(RequestURI, "RenderingControl", args);
        }
        private bool GetMute()
        {
            UPnPService oRendererService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "renderingcontrol").ToList()[0];
            UPnPAction oTransportAction = oRendererService.ActionList.Where(e => e.ActionName.ToLower() == "getmute").ToList()[0];
            oTransportAction.OnResponseReceived += new ResponseReceived(OnResponseSetCurrent);

            //Pfad setzen
            string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oRendererService.ControlURL;
            List<Tuple<string, string>> args = new List<Tuple<string, string>>();
            args.Add(new Tuple<string, string>("InstanceID", "0"));
            args.Add(new Tuple<string, string>("Channel", "Master"));
            GetMuteResponse = false;
            oTransportAction.Execute(RequestURI, "RenderingControl", args);
            while (!GetMuteResponse) { };
            GetMuteResponse = false;
            return Mutevalue;
        }
        private void SetMute(bool Mute)
        {
            UPnPService oRendererService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "renderingcontrol").ToList()[0];
            UPnPAction oTransportAction = oRendererService.ActionList.Where(e => e.ActionName.ToLower() == "setmute").ToList()[0];
            oTransportAction.OnResponseReceived += new ResponseReceived(OnResponseSetMute);

            //Pfad setzen
            string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oRendererService.ControlURL;
            List<Tuple<string, string>> args = new List<Tuple<string, string>>();
            args.Add(new Tuple<string, string>("InstanceID", "0"));
            args.Add(new Tuple<string, string>("Channel", "Master"));
            args.Add(new Tuple<string, string>("DesiredMute", Mute.ToString()));

            TempMute = Mute;
            oTransportAction.Execute(RequestURI, "RenderingControl", args);

        }





        private void OnResponsePlaying(XDocument oResponseDocument, ActionState oState)
        {
            if (oState.oWebResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                XElement TransportState = oResponseDocument.Root.Elements().Elements().Elements().Where(e => e.Name.LocalName.ToLower() == "currenttransportstate").ToList()[0];
                if (TransportState.Value.ToLower() == "playing") IsPlaying = true;
                else IsPlaying = false;
            }
            PlayingReponse = true;

        }
        private void OnResponseReceived(XDocument oResponseDocument, ActionState oState)
        {
            IsPlaying = false;
        }
        private void OnResponseSetAVTransportURI(XDocument oResponseDocument, ActionState oState)
        {
            if (oState.Successful && oState.oWebResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                ConnectionSuccessful = true;
                Playable();
            }
            else
            {
                //In diesem Fall gab es einen Fehler beim Abrufen der Response dann Fehler anzeigen in der Oberfläche.   
                ConnectionError = true;
            }
        }
        private void OnResponseGetCurrentTransportAction(XDocument oResponseDocument, ActionState oState)
        {
            if (oState.Successful && oState.oWebResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                Play();
            }
            else
            {
                Playable();
            }
        }
        private void OnResponsePlay(XDocument oResponseDocument, ActionState oState)
        {
            if (oState.Successful && oState.oWebResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                IsPlaying = true;
                //Gut
            }
        }
        private void OnResponsePosition(XDocument oResponseDocument, ActionState oState)
        {
            if (oState.Successful && oState.oWebResponse.StatusCode == System.Net.HttpStatusCode.OK && IsPlaying)
            {
                int Position = int.Parse(oResponseDocument.Root.Elements().Elements().Elements().Where(e => e.Name.LocalName.ToLower() == "abstime").ToList()[0].Value);
                LastPosition = Position;
            }
            else LastPosition = -1;
            PositionResponse = true;
        }
        private void OnResponseNext(XDocument oResponseDocument, ActionState oState)
        {
            if (oState.Successful && oState.oWebResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                PreviousMedia = CurrentMedia;
                CurrentMedia = NextMedia;
                MediaObject oNext = MediaList.Where(e => e.Index == CurrentMedia.Index + 1).ToList().FirstOrDefault();
                if (oNext != null) NextMedia = oNext;
            }

        }
        private void OnResponseSetNext(XDocument oResponseDocument, ActionState oState)
        {
            if (oState.Successful && oState.oWebResponse.StatusCode == HttpStatusCode.OK) Play();
        }
        private void OnResponseStop(XDocument oResponseDocument, ActionState oState)
        {
            if (oState.Successful && oState.oWebResponse.StatusCode == HttpStatusCode.OK) IsPlaying = false;
        }
        private void OnResponseSetCurrent(XDocument oResponseDocument, ActionState oState)
        {
            if (oState.Successful && oState.oWebResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                Playable();
            }
        }
        private void OnResponseCurrentIDs(XDocument oResponseDocument, ActionState oState)
        {
            if (oState.Successful && oState.oWebResponse.StatusCode == HttpStatusCode.OK) CurrentIDResponseDoc = oResponseDocument;
            else CurrentIDResponseDoc = null;
            CurrentIDResponse = true;
        }
        private void OnResponseGetVolume(XDocument oResponseDocument, ActionState oState)
        {
            if(oState.Successful && oState.oWebResponse.StatusCode == HttpStatusCode.OK)
            {
                XElement Volume = oResponseDocument.Root.Elements().Elements().Elements().Where(e => e.Name.LocalName.ToLower() == "currentvolume").FirstOrDefault();
                if (Volume != null) Volumevalue = int.Parse(Volume.Value);
            }
            GetVolumeResponse = true;
        }
        private void OnResponseSetVolume(XDocument oResponseDocument, ActionState oState)
        {
            if (oState.Successful && oState.oWebResponse.StatusCode == HttpStatusCode.OK)
            {
                Volumevalue = TempVolume;
            }
        }
        private void OnResponseGetMute(XDocument oResponseDocument, ActionState oState)
        {
            if (oState.Successful && oState.oWebResponse.StatusCode == HttpStatusCode.OK)
            {
                XElement Mute = oResponseDocument.Root.Elements().Elements().Elements().Where(e => e.Name.LocalName.ToLower() == "currentmute").FirstOrDefault();
                if (Mute != null) Mutevalue = Boolean.Parse(Mute.Value);
                GetMuteResponse = true;
            }
            else Mutevalue = false;
            GetMuteResponse = true;
        }
        private void OnResponseSetMute(XDocument oResponseDocument, ActionState oState)
        {
            if (oState.Successful && oState.oWebResponse.StatusCode == HttpStatusCode.OK)
            {
                Mutevalue = TempMute;
            }
        }
    }
}
