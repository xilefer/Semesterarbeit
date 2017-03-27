using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public PlayerControl()
        {
            this.NextMedia = this.CurrentMedia;
            this.PreviousMedia = this.CurrentMedia;
            //Verbindung zum Gerät aufbauen
            //Pfad setzen
            //Event abonnieren
            //Play ausführen
            //Status aktualisieren
        }
        public bool Pause()
        {
            //Pause Action ausführen.
            //Status abrufen und aktualisieren
            return true;
        }
        public bool Play(int Index)
        {
            //Play mit Index ausführen
            MediaObject oCurrent = MediaList.Where(e => e.Index == Index).ToList().First();
            if (oCurrent != null) CurrentMedia = oCurrent;
            else return false;
            MediaObject oNext = MediaList.Where(e => e.Index == Index+1).ToList().First();
            if (oNext != null) NextMedia = oNext;
            else NextMedia = CurrentMedia;
            MediaObject oPrev = MediaList.Where(e => e.Index == Index -1).ToList().First();
            if (oPrev != null) PreviousMedia = oPrev;
            else PreviousMedia = CurrentMedia;
            return true;
        }
        public bool Next()
        {
            PreviousMedia = CurrentMedia;
            CurrentMedia = NextMedia;
            MediaObject oNext = MediaList.Where(e => e.Index == CurrentMedia.Index + 1).ToList().First();
            if (oNext != null) NextMedia = oNext;
            //hier Next Action ausführen
            return true;
        }
        public bool Previous()
        {
            NextMedia = CurrentMedia;
            CurrentMedia = PreviousMedia;
            MediaObject oPrev = MediaList.Where(e => e.Index == CurrentMedia.Index - 1).ToList().First();
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
