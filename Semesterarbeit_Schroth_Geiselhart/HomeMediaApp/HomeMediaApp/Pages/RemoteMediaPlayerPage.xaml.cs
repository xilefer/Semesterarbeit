using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HomeMediaApp.Classes;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace HomeMediaApp.Pages
{
    // TODO: Datatemplate selector für PlayListView implementieren
    public partial class RemoteMediaPlayerPage : ContentPage
    {
        private int CurrentMediaIndex = 0;
        private int nSliderValue = 0;
        private bool PositionTimerRun = false;
        private bool EventSet = false;
        private object LockObject = new object();

        public int SliderValue
        {
            get { return nSliderValue; }
            set
            {
                if (nSliderValue == value) return;
                nSliderValue = value;
                OnPropertyChanged();
            }
        }

        public PlaylistItem PlayList { get; set; } = null;

        private UPnPMusicTrack nCurrentMusicTrack = null;
        public UPnPMusicTrack CurrentMusicTrack
        {
            get { return nCurrentMusicTrack; }
            set
            {
                if (nCurrentMusicTrack == value) return;
                nCurrentMusicTrack = value;
                OnPropertyChanged();
                OnPropertyChanged("AlbumArtSource");
                OnPropertyChanged("CurrentMusicTrackName");
                OnPropertyChanged("SliderMaximum");
                SliderValue = 0;
            }
        }

        public string CurrentMusicTrackName
        {
            get
            {
                if (CurrentMusicTrack != null) return CurrentMusicTrack.Title;
                else return "";
            }
        }

        public string CurrentDeviceName
        {
            get
            {
                if (GlobalVariables.GlobalPlayerControl == null) return "Kein Gerät gewählt";
                return "Ausgabegerät: " + GlobalVariables.GlobalPlayerControl.oDevice.DeviceName;
            }
        }

        public ImageSource AlbumArtSource
        {
            get
            {
                if (CurrentMusicTrack != null)
                {
                    if (CurrentMusicTrack.AlbumArtURI.Length > 0) return ImageSource.FromUri(new Uri(CurrentMusicTrack.AlbumArtURI));
                    return ImageSource.FromResource("HomeMediaApp.Icons.music_icon.png");
                }
                else return null;
            }
        }

        public PlayerControl RemotePlayerControl
        {
            get { return GlobalVariables.GlobalPlayerControl; }
            set { GlobalVariables.GlobalPlayerControl = value; }
        }

        public ImageSource PlayPauseSource
        {
            get
            {
                if (GlobalVariables.GlobalPlayerControl != null)
                {
                    if (GlobalVariables.GlobalPlayerControl.Playing) return ImageSource.FromResource("HomeMediaApp.Icons.pause_icon.png");
                    return ImageSource.FromResource("HomeMediaApp.Icons.play_icon.png");
                }
                return ImageSource.FromResource("HomeMediaApp.Icons.play_icon.png");
            }
            set { }
        }

        public double SliderMaximum
        {
            get
            {
                if (CurrentMusicTrack != null && CurrentMusicTrack.DurationSec != 0) return CurrentMusicTrack.DurationSec;
                return 1;
            }
        }

        public ObservableCollection<MusicItem> MusicItems
        {
            get
            {
                if (PlayList != null) return PlayList.MusicItems;
                return new ObservableCollection<MusicItem>()
                {
                    new MusicItem("Keine Titel vorhanden")
                };
            }
        }


        public RemoteMediaPlayerPage()
        {
            InitializeComponent();
            BindingContext = this;
            Device.BeginInvokeOnMainThread(() => CurrentMusicLabel.FontSize = 22);
            if (GlobalVariables.GlobalPlayerControl != null)
            {
                EventSet = true;
                GlobalVariables.GlobalPlayerControl.PlayingStatusChanged += new PlayingStatusChangedEvent(() => Device.BeginInvokeOnMainThread(
                () =>
                {
                    OnPropertyChanged("PlayPauseSource");
                    if (GlobalVariables.GlobalPlayerControl.Playing) StartPositionTimer();
                    else StopPositionTimer();
                }));
            }
            OnPropertyChanged("PlayPauseSource");
            // Subscription für die ContextActions!
            MessagingCenter.Subscribe<PlayListViewViewCell, MusicItem>(this, GlobalVariables.RemoveTrackFromPlayListActionName, (sender, arg) =>
            {
                PlayList.MusicItems.Remove(arg);
                OnPropertyChanged("MusicItems");
            });
        }




        private void ImageLastGestureRecognizer_OnTapped(object sender, EventArgs e)
        {
            if (PlayList == null) return;
            List<MusicItem> CurrentItem = PlayList.MusicItems.Where(Item => Item.RelatedTrack.Title == CurrentMusicTrackName).ToList();
            if (CurrentItem.Count == 1)
            {

                int IndexOfItem = PlayList.MusicItems.IndexOf(CurrentItem[0]);
                PlayList.MusicItems[IndexOfItem].IsPlaying = false;
                IndexOfItem--;
                if (IndexOfItem < 0)
                {
                    IndexOfItem++;
                }
                CurrentMusicTrack = PlayList.MusicItems[IndexOfItem].RelatedTrack;
                PlayList.MusicItems[IndexOfItem].IsPlaying = true;
                PlayListView.ItemsSource = null;
                PlayListView.ItemsSource = MusicItems;
                OnPropertyChanged("MusicItems");
                ChangeMusicTrack(PlayList.MusicItems[IndexOfItem], false);
                ForceLayout();
            }
        }

        private void ChangeMusicTrack(MusicItem NewTrack, bool Next)
        {
            // Ist NewTrack dem PlayerControl bereits bekannt?
            int nIndex = -1;
            foreach (var MediaObject in GlobalVariables.GlobalPlayerControl.MediaList)
            {
                if (MediaObject.Path == NewTrack.RelatedTrack.Res)
                {
                    nIndex = GlobalVariables.GlobalPlayerControl.MediaList.IndexOf(MediaObject);
                    break;
                }
            }
            if (nIndex == -1)
            {
                if (Next)
                {
                    // Nicht bekannt --> Nach aktuellem Medium Einfügen
                    GlobalVariables.GlobalPlayerControl.MediaList.Insert(
                        GlobalVariables.GlobalPlayerControl.MediaList.IndexOf(
                            GlobalVariables.GlobalPlayerControl.CurrentMedia) + 1, new MediaObject()
                            {
                                Index =
                                    GlobalVariables.GlobalPlayerControl.MediaList.IndexOf(
                                        GlobalVariables.GlobalPlayerControl.CurrentMedia) + 1,
                                Path = NewTrack.RelatedTrack.Res
                            });
                    GlobalVariables.GlobalPlayerControl.NextMedia =
                        GlobalVariables.GlobalPlayerControl.MediaList[
                            GlobalVariables.GlobalPlayerControl.MediaList.IndexOf(
                                GlobalVariables.GlobalPlayerControl.CurrentMedia) + 1];
                    GlobalVariables.GlobalPlayerControl.SetNextMedia(
                        GlobalVariables.GlobalPlayerControl.MediaList[
                            GlobalVariables.GlobalPlayerControl.MediaList.IndexOf(
                                GlobalVariables.GlobalPlayerControl.CurrentMedia) + 1]);
                }
                else
                {
                    // nicht bekannt --> Sicherstellen dass es vor dem aktuellen Element ist
                    //TODO: Siehe Kommentar
                }
            }
            else
            {
                if (Next)
                {
                    // bekannt --> Sicherstellen dass es als nächstes Wiedergegeben wird
                    if (GlobalVariables.GlobalPlayerControl.NextMedia != GlobalVariables.GlobalPlayerControl.MediaList[nIndex])
                    {
                        GlobalVariables.GlobalPlayerControl.NextMedia = GlobalVariables.GlobalPlayerControl.MediaList[nIndex];
                    }
                }
                else
                {
                    if (GlobalVariables.GlobalPlayerControl.PreviousMedia != GlobalVariables.GlobalPlayerControl.MediaList[nIndex])
                    {
                        GlobalVariables.GlobalPlayerControl.PreviousMedia = GlobalVariables.GlobalPlayerControl.MediaList[nIndex];
                    }
                }
            }
            if (Next) GlobalVariables.GlobalPlayerControl.Next(); // Wiedergabe des nächsten Titels starten
            else GlobalVariables.GlobalPlayerControl.Previous();
        }

        private void ImagePlayGestureRecognizer_OnTapped(object sender, EventArgs e)
        {
            if (GlobalVariables.GlobalPlayerControl == null) return;
            if (GlobalVariables.GlobalPlayerControl.IsPlaying) GlobalVariables.GlobalPlayerControl.Pause();
            else GlobalVariables.GlobalPlayerControl.Play();
        }

        /// <summary>
        /// Fügt der aktuellen Wiedergabeliste einen neuen Musiktitel hinzu
        /// </summary>
        /// <param name="MusicTrack"></param>
        public void AddMusicTrackToPlayList(MusicItem MusicTrack, bool bFirst)
        {
            if (PlayList == null)
            {
                PlayList = new PlaylistItem("Aktuelle Wiedergabe");
            }
            if (PlayList.MusicItems.Where(e => e.DisplayName == MusicTrack.DisplayName).ToList().Count == 0) PlayList.MusicItems.Add(MusicTrack);
            else return;
            if (!bFirst)
            {
                if (GlobalVariables.GlobalPlayerControl != null)
                {
                    GlobalVariables.GlobalPlayerControl.AddMedia(new MediaObject() { Index = GlobalVariables.GlobalPlayerControl.MediaList.Count, Path = MusicTrack.RelatedTrack.Res });
                }
            }
            OnPropertyChanged("MusicItems");
        }

        private void ImageNextGestureRecognizer_OnTapped(object sender, EventArgs e)
        {
            if (PlayList == null) return;
            List<MusicItem> CurrentItem = PlayList.MusicItems.Where(item => item.RelatedTrack.Title == CurrentMusicTrackName).ToList();
            if (CurrentItem.Count == 1)
            {
                int IndexOfItem = PlayList.MusicItems.IndexOf(CurrentItem[0]);
                PlayList.MusicItems[IndexOfItem].IsPlaying = false;
                IndexOfItem++;
                if (IndexOfItem > (PlayList.MusicItems.Count - 1))
                {
                    IndexOfItem--;
                }
                CurrentMusicTrack = PlayList.MusicItems[IndexOfItem].RelatedTrack;
                PlayList.MusicItems[IndexOfItem].IsPlaying = true;
                PlayListView.ItemsSource = null;
                PlayListView.ItemsSource = MusicItems;
                OnPropertyChanged("MusicItems");
                ChangeMusicTrack(PlayList.MusicItems[IndexOfItem], true);
                ForceLayout();
            }
        }

        private void Button_OnClicked(object sender, EventArgs e)
        {
            if (PlayListView.IsVisible)
            {
                PlayListView.IsVisible = false;
                AlbumImage.IsVisible = true;
                PlaylistButton.Text = "Aktuelle Wiedergabeliste";
            }
            else
            {
                PlayListView.IsVisible = true;
                AlbumImage.IsVisible = false;
                PlaylistButton.Text = "Aktuelle Wiedergabe";
            }
            OnPropertyChanged("MusicItems");
            ForceLayout();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            OnPropertyChanged("MusicItems");
            OnPropertyChanged("CurrentDeviceName");
            if (GlobalVariables.GlobalPlayerControl != null && !EventSet)
            {
                EventSet = true;
                Task.Delay(10);
                GlobalVariables.GlobalPlayerControl.PlayingStatusChanged += new PlayingStatusChangedEvent(() => Device.BeginInvokeOnMainThread(
                () =>
                {
                    OnPropertyChanged("PlayPauseSource");
                    if (GlobalVariables.GlobalPlayerControl.IsPlaying) StartPositionTimer();
                    else StopPositionTimer();
                }));
            }
            // Event einmal triggern, damit es evtl automatisch läuft
            if (GlobalVariables.GlobalPlayerControl != null && GlobalVariables.GlobalPlayerControl.IsPlaying) StartPositionTimer();
        }

        private void StartPositionTimer()
        {
            if (Monitor.TryEnter(LockObject))
            {
                try
                {
                    if (PositionTimerRun) return; // Keine weiteren Timer starten!
                    PositionTimerRun = true;
                    Device.StartTimer(TimeSpan.FromSeconds(1), () =>
                    {
                        Debug.WriteLine("Request Position" + DateTime.Now);
                        if (GlobalVariables.GlobalPlayerControl != null)
                        {
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                int PosTemp = GlobalVariables.GlobalPlayerControl.GetCurrentPosition();
                                if (PosTemp > 0) SliderValue = PosTemp;
                            });
                        }
                        Debug.WriteLine("Returning Position" + DateTime.Now);
                        return PositionTimerRun;
                    });
                }
                finally
                {
                    Monitor.Exit(LockObject);
                }
            }
            else
            {

            }
        }

        private void StopPositionTimer()
        {
            PositionTimerRun = false;
        }


        private void PlayListView_OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            return;
            if (PlayList == null) return;
            foreach (var playListMusicItem in PlayList.MusicItems)
            {
                playListMusicItem.IsPlaying = false;
            }
            int index = PlayList.MusicItems.IndexOf(e.Item as MusicItem);
            PlayList.MusicItems[index].IsPlaying = true;
            PlayListView.ItemsSource = null;
            PlayListView.ItemsSource = MusicItems;
            OnPropertyChanged("MusicItems");
            ForceLayout();
            CurrentMusicTrack = (e.Item as MusicItem).RelatedTrack;
            // TODO: Wiedergabe des ausgewählten Elements starten
            throw new NotImplementedException();
            //ChangeMusicTrack(PlayList.MusicItems[index]);
            // TODO: Wiedergabe starten
            Button_OnClicked(this, null);
        }

        private void PositionSlider_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            // TODO: Erkennen wenn der Slider losgelassen wird   
        }

        private void DeviceChangeButton_OnClicked(object sender, EventArgs e)
        {
            List<string> MediaRenderers = new List<string>();
            foreach (var upnPDevice in GlobalVariables.UPnPMediaRenderer)
            {
                MediaRenderers.Add(upnPDevice.DeviceName);
            }
            Device.BeginInvokeOnMainThread(async () =>
            {
                string Temp = await DisplayActionSheet("Ausgabegerät Wechseln", "Abbrechen", null, MediaRenderers.ToArray());
                if (Temp != null && Temp != "Abbrechen") ChangeOutputDevice(Temp);
            });
        }

        private void ChangeOutputDevice(string NewDevice)
        {
            if (GlobalVariables.GlobalPlayerControl == null)
            {   // Wiedergabe auf Gerät starten
                if (PlayList.MusicItems.Count == 0)
                {
                    DisplayAlert("Fehler", "Die Wiedergabeliste ist leer", "OK");
                    return;
                }
                GlobalVariables.GlobalPlayerControl = new PlayerControl(GlobalVariables.UPnPMediaRenderer.Where(e => e.DeviceName == NewDevice).ToList()[0], new MediaObject() { Index = 0, Path = PlayList.MusicItems[0].RelatedTrack.Res });
                for (int i = 1; i < PlayList.MusicItems.Count; i++)
                {   // Alle Elemente der Playlist einfügen
                    GlobalVariables.GlobalPlayerControl.AddMedia(new MediaObject() { Index = 1, Path = PlayList.MusicItems[i].RelatedTrack.Res });
                }
                GlobalVariables.GlobalPlayerControl.Play();
                PlayList.MusicItems[0].IsPlaying = true;
                CurrentMusicTrack = PlayList.MusicItems[0].RelatedTrack;
                OnAppearing();
                OnPropertyChanged("RemotePlayerControl");
                OnPropertyChanged("CurrentDeviceName");
                OnPropertyChanged("CurrentMusicTrackName");
            }
            else
            {   // Ausgabegerät ändern
                // TODO: Das ausgabegerät wechseln
                throw new NotImplementedException();
            }
        }

        private void ButtonVolumeDown_OnClicked(object sender, EventArgs e)
        {
            if (GlobalVariables.GlobalPlayerControl != null)
            {
                GlobalVariables.GlobalPlayerControl.CurrentVolume = (GlobalVariables.GlobalPlayerControl.CurrentVolume - 1) < 0 ? 0 : GlobalVariables.GlobalPlayerControl.CurrentVolume - 1;
            }
        }

        private void ButtonVolumeUp_OnClicked(object sender, EventArgs e)
        {
            if (GlobalVariables.GlobalPlayerControl != null)
            {
                GlobalVariables.GlobalPlayerControl.CurrentVolume = GlobalVariables.GlobalPlayerControl.CurrentVolume + 1;
            }
        }

        private void TapGestureRecognizer_OnTapped(object sender, EventArgs e)
        {
            // Wird erst ausgelöst wenn der Anwender den Slider loslässt
            int Position = SliderValue;
            if (GlobalVariables.GlobalPlayerControl != null)
            {
            }
        }
    }
}
