﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeMediaApp.Classes;
using System.Xml.Linq;

namespace HomeMediaApp.Classes
{
    public class PlayerControl
    {
        public UPnPDevice oDevice { get; set; }
        public List<MediaObject> MediaList { get; set; } = new List<MediaObject>();
        public MediaObject CurrentMedia { get; set; }
        public MediaObject NextMedia { get; set; }
        public MediaObject PreviousMedia { get; set; }
        public string Status { get; set; }

        public void OnResponseReceived(XDocument oResponseDocument, ActionState oState)
        {

        }
        public void OnResponseSetAVTransportURI(XDocument oResponseDocument, ActionState oState)
        {
            if(oState.oWebResponse.StatusCode == System.Net.HttpStatusCode.OK)
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
            if(oState.oWebResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                Play(CurrentMedia.Index);
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
                //Gut
            }
        }
        public PlayerControl(UPnPDevice RendererDevice, MediaObject Media)
        {
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
            args.Add(new Tuple<string, string>("InstanceID", MediaList.IndexOf(CurrentMedia).ToString()));
            args.Add(new Tuple<string, string>("CurrentURI", CurrentMedia.Path));
            args.Add(new Tuple<string, string>("CurrentURIMetaData", CurrentMedia.MetaData));

            oTransportAction.Execute(RequestURI, "AVTransport", args);
            //Play(0);
            //Event abonnieren
            //Play ausführen
            //Status aktualisieren
        }

        public void  Playable()
        {
            UPnPService oTransportService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "avtransport").ToList()[0];
            UPnPAction oTransportAction = oTransportService.ActionList.Where(e => e.ActionName.ToLower() == "getcurrenttransportactions").ToList()[0];
            oTransportAction.OnResponseReceived += new ResponseReceived(OnResponseGetCurrentTransportAction);

            string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oTransportService.ControlURL;

            List<Tuple<string, string>> args = new List<Tuple<string, string>>();
            args.Add(new Tuple<string, string>("InstanceID", MediaList.IndexOf(CurrentMedia).ToString()));

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
            args.Add(new Tuple<string, string>("InstanceID", MediaList.IndexOf(CurrentMedia).ToString()));

            oPlayAction.Execute(RequestURI, "AVTransport", args);

            return true;
        }
        public bool Play(int Index)
        {
            //Play mit Index ausführen
            UPnPService oTransportService = oDevice.DeviceMethods.Where(e => e.ServiceID.ToLower() == "avtransport").ToList()[0];
            UPnPAction oPlayAction = oTransportService.ActionList.Where(e => e.ActionName.ToLower() == "play").ToList()[0];
            oPlayAction.OnResponseReceived += new ResponseReceived(OnResponsePlay);

            string RequestURI = oDevice.DeviceAddress.Scheme + @"://" + oDevice.DeviceAddress.Authority + oTransportService.ControlURL;

            List<Tuple<string, string>> args = new List<Tuple<string, string>>();
            args.Add(new Tuple<string, string>("InstanceID", MediaList.IndexOf(CurrentMedia).ToString()));
            args.Add(new Tuple<string, string>("Speed", "1"));

            oPlayAction.Execute(RequestURI, "AVTransport", args);

            MediaObject oCurrent = MediaList.Where(e => e.Index == Index).ToList().FirstOrDefault();
            if (oCurrent != null) CurrentMedia = oCurrent;
            else return false;
            MediaObject oNext = MediaList.Where(e => e.Index == Index + 1).ToList().FirstOrDefault();
            if (oNext != null) NextMedia = oNext;
            else NextMedia = CurrentMedia;
            MediaObject oPrev = MediaList.Where(e => e.Index == Index -1).ToList().FirstOrDefault();
            if (oPrev != null) PreviousMedia = oPrev;
            else PreviousMedia = CurrentMedia;
            return true;
        }
        public bool Next()
        {
            PreviousMedia = CurrentMedia;
            CurrentMedia = NextMedia;
            MediaObject oNext = MediaList.Where(e => e.Index == CurrentMedia.Index + 1).ToList().FirstOrDefault();
            if (oNext != null) NextMedia = oNext;
            //hier Next Action ausführen
            return true;
        }
        public bool Previous()
        {
            NextMedia = CurrentMedia;
            CurrentMedia = PreviousMedia;
            MediaObject oPrev = MediaList.Where(e => e.Index == CurrentMedia.Index - 1).ToList().FirstOrDefault();
            if (oPrev != null) PreviousMedia = oPrev;
            else PreviousMedia = CurrentMedia;
            //Previous Action
            return true;
        }
        public bool Stop()
        {
            //Stop asuführen
            //Verbindung beenden
            return true;
        }
        public void AddMedia(MediaObject oMedia)
        {
            if (oMedia.Index != GetLastIndex() + 1) return;
            if (oMedia.Index == CurrentMedia.Index+1)
            {
                this.NextMedia = oMedia;
            }
            MediaList.Add(oMedia);
        }

        public int GetLastIndex()
        {
            int Index = 0;
            foreach(MediaObject item in MediaList)
            {
                if (item.Index > Index) Index = item.Index;
            }
            return Index;
        }
    }
}
