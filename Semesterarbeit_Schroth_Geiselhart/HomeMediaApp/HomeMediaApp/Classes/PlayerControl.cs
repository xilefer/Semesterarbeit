using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Threading;
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

        //Events fürs aus und einhängen
        private ResponseReceived ResponseReceivedPlaying = null;
        private bool PlayableResponseReceived { get; set; } = false;


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
            set { SetMute(value); }
        }
        private bool Mutevalue { get; set; }
        private bool TempMute { get; set; } = false;
        private bool GetMuteResponse { get; set; } = false;
        public int Volume
        {
            get { return GetVolume(); }
            set { SetVolume(value); }
        }

        private object LockObject = new object();
        private bool PauseResponse { get; set; } = false;
        private int Volumevalue { get; set; } = 0;
        private int TempVolume { get; set; } = 0;
        private bool GetVolumeResponse { get; set; } = false;
        private int LastPosition { get; set; }
        private bool PlayingReponse { get; set; }
        public bool Playing { get; set; } = false;
        private bool PositionResponse { get; set; } = false;
        private bool CurrentIDResponse { get; set; } = false;
        public bool ConnectionSuccessful { get; set; } = false;
        private TcpSocketListener oSocket { get; set; }
        private TcpSocketListener oSocketRenderer { get; set; }
        public bool ConnectionError { get; set; } = false;
        private XDocument CurrentIDResponseDoc { get; set; }
        private string ConnectionID = null;


        public PlayerControl(UPnPDevice RendererDevice, MediaObject Media)
        {
            this.CurrentMedia = Media;
            this.oDevice = RendererDevice;
            this.NextMedia = this.CurrentMedia;
            this.PreviousMedia = this.CurrentMedia;
            MediaList.Add(Media);
            // Callback Member initialisieren
            PauseResponseReceived = new ResponseReceived(OnResponseReceived);
            ResponseReceivedPlaying = new ResponseReceived(OnResponseGetCurrentTransportAction);


            //Überprüfen ob bereits eine Verbindung besteht
            //---------------------------------------------------------------------------------------------------------------------------------------//

            UPnPService oConnectionService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "connectionmanager").FirstOrDefault();
            UPnPAction oCurrentIDs = oConnectionService.ActionList.Where(e => e.ActionName.ToLower() == "getcurrentconnectionids").FirstOrDefault();
            oCurrentIDs.OnResponseReceived += new ResponseReceived(OnResponseCurrentIDs);
            List<Tuple<string, string>> argsid = new List<Tuple<string, string>>();
            CurrentIDResponse = false;
            oCurrentIDs.Execute(oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oConnectionService.ControlURL, "ConnectionManager", argsid);
            while (CurrentIDResponse == false) { };
            // Der Callback kann auch null zurückgeben wenn ein Fehler aufgetreten ist! Deswegen in ein IF gepackt FG
            if (CurrentIDResponseDoc != null)
            {
                XElement ConnectionIDElement = CurrentIDResponseDoc.Root.Elements().Elements().Elements().Where(e => e.Name.LocalName.ToLower() == "connectionids").FirstOrDefault();
                if (ConnectionIDElement == null) ConnectionIDElement = CurrentIDResponseDoc.Descendants().Where(e => e.Name.LocalName.ToLower() == "currentconnectionids").FirstOrDefault();
                string ConnectionIDs = "";
                if (ConnectionIDElement != null && ConnectionIDElement.Value != "") { ConnectionIDs = ConnectionIDElement.Value; }
                ConnectionID = ConnectionIDs;
                Stop();
                if (ConnectionIDs != "")
                {

                }
            }

            //---------------------------------------------------------------------------------------------------------------------------------------//
            //


            ////Abonnieren der Events von AVTransport und RenderingControl
            ////---------------------------------------------------------------------------------------------------------------------------------------//
            UPnPService oTransportService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "avtransport").FirstOrDefault();
            UPnPService oRendererService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "renderingcontrol").FirstOrDefault();

            if (oTransportService == null) return;
            string IP = DependencyService.Get<IGetDeviceIPAddress>().GetDeviceIP();
            if (IP != null)
            {

                string EventURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oTransportService.EventSubURL;
                HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(EventURI);
                httpRequest.Method = "SUBSCRIBE";
                httpRequest.Headers["CALLBACK"] = "<http://" + IP + ":" + ListenPortAV + "/>";
                httpRequest.Headers["NT"] = "upnp:event";
                httpRequest.Headers["TIMEOUT"] = "Second-300";
                ActionState oState = new ActionState()
                {
                    oWebRequest = httpRequest
                };
                httpRequest.BeginGetResponse(RequestCallback, oState);
                if (oRendererService != null)
                {
                    string EventURIRenderer = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oRendererService.EventSubURL;
                    HttpWebRequest httpRequestRenderer = (HttpWebRequest)WebRequest.Create(EventURI);
                    httpRequestRenderer.Method = "SUBSCRIBE";
                    httpRequestRenderer.Headers["CALLBACK"] = "<http://" + IP + ":" + ListenPortRenderer + "/>";
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
            args.Add(new Tuple<string, string>("CurrentURIMetaData", CurrentMedia.Path));

            ConnectionSuccessful = false;
            oTransportAction.Execute(RequestURI, "AVTransport", args);
            while (!ConnectionSuccessful && !ConnectionError) { };
            Playable();

            //---------------------------------------------------------------------------------------------------------------------------------------//
            //
        }

        public PlayerControl(UPnPDevice RendererDevice, MediaObject Media, int Position)
        {
            this.CurrentMedia = Media;
            this.oDevice = RendererDevice;
            this.NextMedia = this.CurrentMedia;
            this.PreviousMedia = this.CurrentMedia;
            MediaList.Add(Media);
            // Callback Member initialisieren
            PauseResponseReceived = new ResponseReceived(OnResponseReceived);
            ResponseReceivedPlaying = new ResponseReceived(OnResponseGetCurrentTransportAction);


            //Überprüfen ob bereits eine Verbindung besteht
            //---------------------------------------------------------------------------------------------------------------------------------------//

            UPnPService oConnectionService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "connectionmanager").FirstOrDefault();
            UPnPAction oCurrentIDs = oConnectionService.ActionList.Where(e => e.ActionName.ToLower() == "getcurrentconnectionids").FirstOrDefault();
            oCurrentIDs.OnResponseReceived += new ResponseReceived(OnResponseCurrentIDs);
            List<Tuple<string, string>> argsid = new List<Tuple<string, string>>();
            CurrentIDResponse = false;
            oCurrentIDs.Execute(oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oConnectionService.ControlURL, "ConnectionManager", argsid);
            while (CurrentIDResponse == false) { };
            // Der Callback kann auch null zurückgeben wenn ein Fehler aufgetreten ist! Deswegen in ein IF gepackt FG
            if (CurrentIDResponseDoc != null)
            {
                XElement ConnectionIDElement = CurrentIDResponseDoc.Root.Elements().Elements().Elements().Where(e => e.Name.LocalName.ToLower() == "connectionids").FirstOrDefault();
                if (ConnectionIDElement == null) ConnectionIDElement = CurrentIDResponseDoc.Descendants().Where(e => e.Name.LocalName.ToLower() == "currentconnectionids").FirstOrDefault();
                string ConnectionIDs = "";
                if (ConnectionIDElement != null && ConnectionIDElement.Value != "") { ConnectionIDs = ConnectionIDElement.Value; }
                ConnectionID = ConnectionIDs;
                Stop();
                if (ConnectionIDs != "")
                {

                }
            }

            //---------------------------------------------------------------------------------------------------------------------------------------//
            //


            ////Abonnieren der Events von AVTransport und RenderingControl
            ////---------------------------------------------------------------------------------------------------------------------------------------//
            UPnPService oTransportService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "avtransport").FirstOrDefault();
            UPnPService oRendererService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "renderingcontrol").FirstOrDefault();

            if (oTransportService == null) return;
            string IP = DependencyService.Get<IGetDeviceIPAddress>().GetDeviceIP();
            if (IP != null)
            {

                string EventURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oTransportService.EventSubURL;
                HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(EventURI);
                httpRequest.Method = "SUBSCRIBE";
                httpRequest.Headers["CALLBACK"] = "<http://" + IP + ":" + ListenPortAV + "/>";
                httpRequest.Headers["NT"] = "upnp:event";
                httpRequest.Headers["TIMEOUT"] = "Second-300";
                ActionState oState = new ActionState()
                {
                    oWebRequest = httpRequest
                };
                httpRequest.BeginGetResponse(RequestCallback, oState);
                if (oRendererService != null)
                {
                    string EventURIRenderer = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oRendererService.EventSubURL;
                    HttpWebRequest httpRequestRenderer = (HttpWebRequest)WebRequest.Create(EventURI);
                    httpRequestRenderer.Method = "SUBSCRIBE";
                    httpRequestRenderer.Headers["CALLBACK"] = "<http://" + IP + ":" + ListenPortRenderer + "/>";
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
            args.Add(new Tuple<string, string>("CurrentURIMetaData", CurrentMedia.Path));

            ConnectionSuccessful = false;
            oTransportAction.Execute(RequestURI, "AVTransport", args);
            while (!ConnectionSuccessful && !ConnectionError) { };
            SetPosition(Position);
            Playable();

            //---------------------------------------------------------------------------------------------------------------------------------------//
            //
        }

        public void DeInit()
        {
            oSocket.StopListeningAsync();
            oSocket.ConnectionReceived -= TCPRec;
            oSocket.Dispose();
            oSocketRenderer.StopListeningAsync();
            oSocketRenderer.ConnectionReceived -= TCPRecRend;
            oSocketRenderer.Dispose();
        }

        public bool IsPlaying
        {
            get
            {
                UPnPService oTransportService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "avtransport").ToList()[0];
                UPnPAction oPlayingAction = oTransportService.ActionList.Where(e => e.ActionName.ToLower() == "gettransportinfo").ToList()[0];
                ResponseReceived PlayingResponse = new ResponseReceived(OnResponsePlaying);
                oPlayingAction.OnResponseReceived += PlayingResponse;

                string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oTransportService.ControlURL;

                List<Tuple<string, string>> args = new List<Tuple<string, string>>();
                args.Add(new Tuple<string, string>("InstanceID", "0"));
                PlayingReponse = false;
                oPlayingAction.Execute(RequestURI, "AVTransport", args);
                while (PlayingReponse == false)
                { }
                PlayingReponse = false;
                oPlayingAction.OnResponseReceived -= PlayingResponse;
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

        public void SetPosition(int Position)
        {
            if (Position < 0) return;
            List<UPnPService> TransportServices = oDevice.DeviceMethods.Where(e => e.ServiceType.ToLower() == "avtransport").ToList();
            if (TransportServices.Count == 0) return;
            UPnPService TransportService = TransportServices[0];
            List<UPnPAction> SeekActions = TransportService.ActionList.Where(e => e.ActionName.ToLower() == "seek").ToList();
            if (SeekActions.Count == 0) return;
            UPnPAction SeekAction = SeekActions[0];
            ResponseReceived Temp = SeekActionOnOnResponseReceived;
            SeekAction.OnResponseReceived += Temp;
            List<UPnPActionArgument> InArgs = new List<UPnPActionArgument>();
            foreach (UPnPActionArgument Arg in SeekAction.ArgumentList)
            {
                if (Arg.Direction.ToLower() == "in") InArgs.Add(Arg);
            }
            SetPositionResponse = false;
            List<Tuple<string, string>> args = new List<Tuple<string, string>>();
            args.Add(new Tuple<string, string>(InArgs[0].Name, "0"));
            args.Add(new Tuple<string, string>(InArgs[1].Name, "REL_TIME"));
            TimeSpan PositionTimeSpan = TimeSpan.FromSeconds(Position);
            string PositionString = PositionTimeSpan.ToString("hh") + ":" + PositionTimeSpan.ToString("mm") + ":" +
                                    PositionTimeSpan.ToString("ss");
            args.Add(new Tuple<string, string>(InArgs[2].Name, PositionString));
            string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + TransportService.ControlURL;
            SeekAction.Execute(RequestURI, TransportService.ServiceID, args);
            while (!SetPositionResponse)
            {
                Task.Delay(2);
            }
            SeekAction.OnResponseReceived -= Temp;
        }

        private bool SetPositionResponse = false;
        private void SeekActionOnOnResponseReceived(XDocument oResponseDocument, ActionState oState)
        {
            if (oState.Successful && oState.oWebResponse.StatusCode == HttpStatusCode.OK)
            {
                //Gut                
            }
            SetPositionResponse = true;
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
            return;
            string xmlmessage = Message.Substring(Message.IndexOf("<?")).Trim(System.Convert.ToChar(System.Convert.ToUInt32("00", 16)));
            XDocument Response = XDocument.Parse(xmlmessage);
            XElement oElement = Response.Root.Elements().Elements().FirstOrDefault();
            if (oElement != null)
            {
                XDocument Eventinfo = XDocument.Parse(oElement.Value);
                XElement oMuteState = Eventinfo.Elements().Where(e => e.Name.LocalName.ToLower() == "mute").FirstOrDefault();
                if (oMuteState != null)
                {
                    if (oMuteState.Value.ToLower() != Mutevalue.ToString()) MuteStatusChanged?.Invoke(); Mutevalue = Boolean.Parse(oMuteState.Value);
                }
                XElement oVolume = Eventinfo.Elements().Where(e => e.Name.LocalName.ToLower() == "volume").FirstOrDefault();
                if (oVolume != null)
                {
                    if (oVolume.Value != Volumevalue.ToString()) VolumeValueChanged?.Invoke(); Volumevalue = int.Parse(oVolume.Value);
                }
            }
        }

        private void TCPRec(object sender, TcpSocketListenerConnectEventArgs args)
        {
            ITcpSocketClient oClient = args.SocketClient;
            byte[] bytes = new byte[128 * 1024];
            MemoryStream ms = new MemoryStream();
            while (oClient.ReadStream.CanRead)
            {
                int bytesRead = oClient.ReadStream.Read(bytes, 0, bytes.Length);
                if (bytesRead <= 0) break;
                ms.Write(bytes, 0, bytesRead);
            }
            oClient.ReadStream.Flush();
            string Message = Encoding.UTF8.GetString(ms.ToArray(), 0, ms.ToArray().Length);
            string XML = Message.Substring(Message.IndexOf("<")).Trim(System.Convert.ToChar(System.Convert.ToUInt32("00", 16)));

            XDocument Response = XDocument.Parse(XML);
            if (Response != null)
            {
                XElement oTransportState = Response.Descendants().Where(e => e.Name.LocalName.ToLower() == "lastchange").FirstOrDefault();
                if (oTransportState != null)
                {
                    XDocument TransportDoc = XDocument.Parse(oTransportState.Value);
                    XElement Transportstate = TransportDoc.Descendants().Where(e => e.Name.LocalName.ToLower() == "transportstate").FirstOrDefault();
                    if (Transportstate != null)
                    {
                        XAttribute TransportAttribute = Transportstate.Attributes().FirstOrDefault();
                        if (TransportAttribute != null)
                        {
                            if (TransportAttribute.Value.ToLower() == "playing") IsPlaying = true;
                            else IsPlaying = false;
                        }
                    }
                }
                XElement oCurrentURI = Response.Descendants().Where(e => e.Name.LocalName.ToLower() == "avtransporturi").FirstOrDefault();
                if (oCurrentURI != null)
                {
                    if (oCurrentURI.Value != CurrentMedia.Path)
                    {
                        CurrentMedia = MediaList.Where(e => e.Path == oCurrentURI.Value).FirstOrDefault();
                        if (MediaList.IndexOf(CurrentMedia) - 1 >= 0) PreviousMedia = MediaList[MediaList.IndexOf(CurrentMedia) - 1];
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
            ResponseReceived ThisResponse = new ResponseReceived(OnResponsePosition);
            oPositionAction.OnResponseReceived += ThisResponse;

            string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oTransportService.ControlURL;

            List<Tuple<string, string>> args = new List<Tuple<string, string>>();
            args.Add(new Tuple<string, string>("InstanceID", "0"));
            PositionResponse = false;
            oPositionAction.Execute(RequestURI, "AVTransport", args);
            while (PositionResponse == false)
            {
                Task.Delay(5);
            }
            oPositionAction.OnResponseReceived -= ThisResponse;
            PositionResponse = false;
            return LastPosition;
        }


        public void Playable()
        {
            UPnPService oTransportService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "avtransport").ToList()[0];
            UPnPAction oTransportAction = oTransportService.ActionList.Where(e => e.ActionName.ToLower() == "getcurrenttransportactions").ToList()[0];
            oTransportAction.OnResponseReceived += ResponseReceivedPlaying;
            PlayableResponseReceived = false;
            string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oTransportService.ControlURL;

            List<Tuple<string, string>> args = new List<Tuple<string, string>>();
            args.Add(new Tuple<string, string>("InstanceID", "0"));

            oTransportAction.Execute(RequestURI, "AVTransport", args);
            while (!PlayableResponseReceived) { Task.Delay(10); }
            oTransportAction.OnResponseReceived -= ResponseReceivedPlaying;
        }

        private ResponseReceived PauseResponseReceived = null;

        public bool Pause()
        {
            //Pause Action ausführen.
            //Status abrufen und aktualisieren
            UPnPService oTransportService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "avtransport").ToList()[0];
            UPnPAction oPlayAction = oTransportService.ActionList.Where(e => e.ActionName.ToLower() == "pause").ToList()[0];
            oPlayAction.OnResponseReceived += PauseResponseReceived;
            PauseResponse = false;
            string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oTransportService.ControlURL;

            List<Tuple<string, string>> args = new List<Tuple<string, string>>();
            args.Add(new Tuple<string, string>("InstanceID", "0"));

            oPlayAction.Execute(RequestURI, "AVTransport", args);
            while (!PauseResponse)
            {
                Task.Delay(10);
            }
            oPlayAction.OnResponseReceived -= PauseResponseReceived;
            return true;
        }

        private ResponseReceived ResponseReceivedPlay = null;
        private bool PlayResponseReceived { get; set; } = false;
        public bool Play()
        {
            //Play mit Index ausführen
            while (!Monitor.TryEnter(LockObject))
            {
                Task.Delay(10);
            }
            try
            {
                ResponseReceivedPlay = new ResponseReceived(OnResponsePlay);
                UPnPService oTransportService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "avtransport").ToList()[0];
                UPnPAction oPlayAction = oTransportService.ActionList.Where(e => e.ActionName.ToLower() == "play").ToList()[0];
                oPlayAction.OnResponseReceived += ResponseReceivedPlay;
                PlayResponseReceived = false;
                string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oTransportService.ControlURL;

                List<Tuple<string, string>> args = new List<Tuple<string, string>>();
                args.Add(new Tuple<string, string>("InstanceID", "0"));
                args.Add(new Tuple<string, string>("Speed", "1"));

                oPlayAction.Execute(RequestURI, "AVTransport", args);
                while (!PlayResponseReceived)
                {
                    Task.Delay(10);
                }
                oPlayAction.OnResponseReceived -= ResponseReceivedPlay;
                return true;
            }
            finally
            {
                Monitor.Exit(LockObject);
            }
        }
        public bool Next()
        {
            if (IsPlaying)
            {
                Stop();
                Task.Delay(10);
            }
            while (IsPlaying)
            {
                Task.Delay(10);
            };
            SetCurrentMedia(NextMedia);
            return true;
        }
        public bool Previous()
        {
            if (IsPlaying)
            {
                Stop();
                Task.Delay(10);
            }
            while (IsPlaying)
            {
                Task.Delay(10);
            };
            SetCurrentMedia(PreviousMedia);
            return true;
        }
        private bool StopResponseReceived { get; set; } = false;
        public bool Stop()
        {
            ResponseReceived StopResponse = new ResponseReceived(OnResponseStop);
            UPnPService oTransportService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "avtransport").ToList()[0];
            UPnPAction oPlayAction = oTransportService.ActionList.Where(e => e.ActionName.ToLower() == "stop").ToList()[0];
            oPlayAction.OnResponseReceived += StopResponse;
            StopResponseReceived = false;
            string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oTransportService.ControlURL;
            List<Tuple<string, string>> args = new List<Tuple<string, string>>();
            args.Add(new Tuple<string, string>("InstanceID", "0"));

            oPlayAction.Execute(RequestURI, "AVTransport", args);
            while (!StopResponseReceived)
            {
                Task.Delay(10);
            }
            oPlayAction.OnResponseReceived -= StopResponse;
            return true;
        }
        public void AddMedia(MediaObject oMedia)
        {
            MediaList.Add(oMedia);
            if (MediaList.IndexOf(CurrentMedia) + 1 == MediaList.IndexOf(oMedia))
            {
                NextMedia = oMedia;
                SetNextMedia(oMedia);
            }
        }

        private bool SetNextMediaResponseReceived { get; set; } = false;
        public void SetNextMedia(MediaObject oMedia)
        {
            ResponseReceived responseReceivednextmedia = new ResponseReceived(OnResponseSetNext);
            UPnPService oTransportService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "avtransport").ToList()[0];
            UPnPAction oTransportAction = oTransportService.ActionList.Where(e => e.ActionName.ToLower() == "setnextavtransporturi").ToList()[0];
            oTransportAction.OnResponseReceived += responseReceivednextmedia;
            SetNextMediaResponseReceived = false;
            //Pfad setzen
            string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oTransportService.ControlURL;
            List<Tuple<string, string>> args = new List<Tuple<string, string>>();
            args.Add(new Tuple<string, string>("InstanceID", "0"));
            args.Add(new Tuple<string, string>("NextURI", oMedia.Path));
            args.Add(new Tuple<string, string>("NextURIMetaData", oMedia.MetaData));

            oTransportAction.Execute(RequestURI, "AVTransport", args);
            while (!SetNextMediaResponseReceived)
            {
                Task.Delay(10);
            }
            oTransportAction.OnResponseReceived -= responseReceivednextmedia;

        }

        private bool SetCurrentMediaResponseReceived { get; set; } = false;
        public void SetCurrentMedia(MediaObject oMedia)
        {
            ResponseReceived temp = new ResponseReceived(OnResponseSetCurrent);
            UPnPService oTransportService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "avtransport").ToList()[0];
            UPnPAction oTransportAction = oTransportService.ActionList.Where(e => e.ActionName.ToLower() == "setavtransporturi").ToList()[0];
            oTransportAction.OnResponseReceived += temp;
            SetCurrentMediaResponseReceived = false;
            //Pfad setzen
            string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oTransportService.ControlURL;
            List<Tuple<string, string>> args = new List<Tuple<string, string>>();
            args.Add(new Tuple<string, string>("InstanceID", "0"));
            args.Add(new Tuple<string, string>("CurrentURI", oMedia.Path));
            args.Add(new Tuple<string, string>("CurrentURIMetaData", ""));

            oTransportAction.Execute(RequestURI, "AVTransport", args);
            while (!SetCurrentMediaResponseReceived)
            {
                Task.Delay(10);
            }
            oTransportAction.OnResponseReceived -= temp;
        }

        private int GetVolume()
        {
            ResponseReceived temp = new ResponseReceived(OnResponseGetVolume);
            UPnPService oRendererService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "renderingcontrol").ToList()[0];
            UPnPAction oTransportAction = oRendererService.ActionList.Where(e => e.ActionName.ToLower() == "getvolume").ToList()[0];
            oTransportAction.OnResponseReceived += temp;
            GetVolumeResponse = false;
            //Pfad setzen
            string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oRendererService.ControlURL;
            List<Tuple<string, string>> args = new List<Tuple<string, string>>();
            args.Add(new Tuple<string, string>("InstanceID", "0"));
            args.Add(new Tuple<string, string>("Channel", "Master"));
            GetVolumeResponse = false;
            oTransportAction.Execute(RequestURI, "RenderingControl", args);
            while (!GetVolumeResponse)
            {
                Task.Delay(10);
            };
            oTransportAction.OnResponseReceived -= temp;
            return Volumevalue;
        }

        private bool SetVolumeResponseReceived { get; set; } = false;
        private void SetVolume(int Volume)
        {
            ResponseReceived temp = new ResponseReceived(OnResponseSetVolume);
            UPnPService oRendererService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "renderingcontrol").ToList()[0];
            UPnPAction oTransportAction = oRendererService.ActionList.Where(e => e.ActionName.ToLower() == "setvolume").ToList()[0];
            oTransportAction.OnResponseReceived += temp;
            SetVolumeResponseReceived = false;
            //Pfad setzen
            string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oRendererService.ControlURL;
            List<Tuple<string, string>> args = new List<Tuple<string, string>>();
            args.Add(new Tuple<string, string>("InstanceID", "0"));
            args.Add(new Tuple<string, string>("Channel", "Master"));
            args.Add(new Tuple<string, string>("DesiredVolume", Volume.ToString()));
            TempVolume = Volume;
            oTransportAction.Execute(RequestURI, "RenderingControl", args);
            while (!SetVolumeResponseReceived)
            {
                Task.Delay(10);
            }
            oTransportAction.OnResponseReceived -= temp;
        }
        private bool GetMute()
        {
            ResponseReceived temp = new ResponseReceived(OnResponseGetMute);
            UPnPService oRendererService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "renderingcontrol").ToList()[0];
            UPnPAction oTransportAction = oRendererService.ActionList.Where(e => e.ActionName.ToLower() == "getmute").ToList()[0];
            oTransportAction.OnResponseReceived += temp;

            //Pfad setzen
            string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oRendererService.ControlURL;
            List<Tuple<string, string>> args = new List<Tuple<string, string>>();
            args.Add(new Tuple<string, string>("InstanceID", "0"));
            args.Add(new Tuple<string, string>("Channel", "Master"));
            GetMuteResponse = false;
            oTransportAction.Execute(RequestURI, "RenderingControl", args);
            while (!GetMuteResponse)
            {
                Task.Delay(10);
            }
            GetMuteResponse = false;
            oTransportAction.OnResponseReceived -= temp;
            return Mutevalue;
        }

        private bool SetMuteResponseReceived { get; set; } = false;
        private void SetMute(bool Mute)
        {
            ResponseReceived temp = new ResponseReceived(OnResponseSetMute);
            UPnPService oRendererService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "renderingcontrol").ToList()[0];
            UPnPAction oTransportAction = oRendererService.ActionList.Where(e => e.ActionName.ToLower() == "setmute").ToList()[0];
            oTransportAction.OnResponseReceived += temp;
            SetMuteResponseReceived = false;
            //Pfad setzen
            string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oRendererService.ControlURL;
            List<Tuple<string, string>> args = new List<Tuple<string, string>>();
            args.Add(new Tuple<string, string>("InstanceID", "0"));
            args.Add(new Tuple<string, string>("Channel", "Master"));
            args.Add(new Tuple<string, string>("DesiredMute", Mute.ToString()));

            TempMute = Mute;
            oTransportAction.Execute(RequestURI, "RenderingControl", args);
            while (!SetMuteResponseReceived)
            {
                Task.Delay(10);
            }
            oTransportAction.OnResponseReceived -= temp;
        }





        private void OnResponsePlaying(XDocument oResponseDocument, ActionState oState)
        {
            if (oState.oWebResponse == null)
            {   // Fehler bei der Übertragung
                PlayingReponse = true;
                return;
            }
            if (oState.oWebResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                if (oResponseDocument != null)
                {   // FG: ResponseDocument kann Null sein!
                    XElement TransportState =
                        oResponseDocument.Root.Elements()
                            .Elements()
                            .Elements()
                            .Where(e => e.Name.LocalName.ToLower() == "currenttransportstate")
                            .ToList()[0];
                    if (TransportState.Value.ToLower() == "playing") IsPlaying = true;
                    else IsPlaying = false;
                }
                else IsPlaying = false;
            }
            PlayingReponse = true;

        }
        private void OnResponseReceived(XDocument oResponseDocument, ActionState oState)
        {
            PauseResponse = true;
            IsPlaying = false;
        }
        private void OnResponseSetAVTransportURI(XDocument oResponseDocument, ActionState oState)
        {
            if (oState.Successful && oState.oWebResponse.StatusCode == HttpStatusCode.OK)
            {
                ConnectionSuccessful = true;
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
                Play(); //Nicht sofort abspielen
            }
            else
            {
                Playable();
            }
            PlayableResponseReceived = true;
        }
        private void OnResponsePlay(XDocument oResponseDocument, ActionState oState)
        {
            if (oState.Successful && oState.oWebResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                IsPlaying = true;
                //Gut
            }
            PlayResponseReceived = true;
        }
        private void OnResponsePosition(XDocument oResponseDocument, ActionState oState)
        {
            try
            {
                if (oState.Successful && oState.oWebResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    if (oResponseDocument != null)
                    {
                        TimeSpan PosTemp = TimeSpan.Parse(oResponseDocument.Root.Elements().Elements().Elements().Where(e => e.Name.LocalName.ToLower() == "reltime").ToList()[0].Value);
                        LastPosition = (int)PosTemp.TotalSeconds;
                    }
                }
                else LastPosition = -1;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
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
            SetNextMediaResponseReceived = true;
            //if (oState.Successful && oState.oWebResponse.StatusCode == HttpStatusCode.OK) //Play();
        }
        private void OnResponseStop(XDocument oResponseDocument, ActionState oState)
        {
            StopResponseReceived = true;
            if (oState.Successful && oState.oWebResponse.StatusCode == HttpStatusCode.OK) IsPlaying = false;
        }
        private void OnResponseSetCurrent(XDocument oResponseDocument, ActionState oState)
        {

            if (oState.Successful && oState.oWebResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {

                SetCurrentMediaResponseReceived = true;
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
            GetVolumeResponse = true;
            if (oState.Successful && oState.oWebResponse.StatusCode == HttpStatusCode.OK)
            {
                XElement Volume = oResponseDocument.Root.Elements().Elements().Elements().Where(e => e.Name.LocalName.ToLower() == "currentvolume").FirstOrDefault();
                if (Volume != null) Volumevalue = int.Parse(Volume.Value);
            }
        }
        private void OnResponseSetVolume(XDocument oResponseDocument, ActionState oState)
        {
            SetVolumeResponseReceived = true;
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
            SetMuteResponseReceived = true;
            if (oState.Successful && oState.oWebResponse.StatusCode == HttpStatusCode.OK)
            {
                Mutevalue = TempMute;
            }
        }
    }
}
