﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HomeMediaApp.Classes;
using HomeMediaApp.Interfaces;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace HomeMediaApp.Pages
{
    /// <summary>
    /// Hintergrundprogrammcode des Remote-Media-Players
    /// </summary>
    public partial class RemoteMediaPlayerPage : ContentPage
    {
        private int nSliderValue = 0;
        private bool PositionTimerRun = false;
        private bool EventSet = false;
        private object LockObject = new object();
        private UPnPMusicTrack nCurrentMusicTrack = null;
        private double ManualVal = 0;

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

        public FileImageSource PlayPauseFileImageSource
        {
            get
            {
                if (GlobalVariables.GlobalPlayerControl != null)
                {
                    if (GlobalVariables.GlobalPlayerControl.IsPlaying)
                    {
                        return DependencyService.Get<IGetFileImageSource>().GetPauseSource();
                    }
                    return DependencyService.Get<IGetFileImageSource>().GetPlaySource();
                }
                return DependencyService.Get<IGetFileImageSource>().GetPlaySource();
            }
            set { }
        }

        public ImageSource PlayPauseSource
        {
            get
            {
                if (GlobalVariables.GlobalPlayerControl != null)
                {
                    if (GlobalVariables.GlobalPlayerControl.IsPlaying) return ImageSource.FromResource("HomeMediaApp.Icons.pause_icon.png");
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

        /// <summary>
        /// Konstruktor
        /// </summary>
        public RemoteMediaPlayerPage()
        {
            InitializeComponent();
            BindingContext = this;
            Device.BeginInvokeOnMainThread(() => CurrentMusicLabel.FontSize = 22);
            if (GlobalVariables.GlobalPlayerControl != null)
            {
                EventSet = true;
                GlobalVariables.GlobalPlayerControl.PlayingStatusChanged += OnPlayingstatuschanged;
            }
            OnPropertyChanged("PlayPauseFileImageSource");
            // Subscription für die ContextActions!
            MessagingCenter.Subscribe<PlayListViewViewCell, MusicItem>(this, GlobalVariables.RemoveTrackFromPlayListActionName, (sender, arg) =>
            {
                if (PlayList != null && PlayList.MusicItems != null) PlayList.MusicItems.Remove(arg);
                OnPropertyChanged("MusicItems");
            });
        }
        
        /// <summary>
        /// Event-Callback wird aufgerufen wenn sich der Wiedergabestatus geändert hat
        /// </summary>
        public void OnPlayingstatuschanged()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                bool Temp = GlobalVariables.GlobalPlayerControl.IsPlaying;
                Debug.WriteLine(Temp);
                if (Temp) StartPositionTimer();
                else StopPositionTimer();
                OnPropertyChanged("PlayPauseFileImageSource");
            });
        }

        /// <summary>
        /// Verarbeitet die Eingabe auf der "Vorheriger-Titel"-Schaltfläche
        /// </summary>
        /// <param name="sender">Das aufrufende Objekt</param>
        /// <param name="e">Eventparameter</param>
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

        /// <summary>
        /// Änder den aktuelle wiedergegebenen Musiktitel
        /// </summary>
        /// <param name="NewTrack">Der neue Musiktitel</param>
        /// <param name="Next">Setzt den Titel als nächsten Titel</param>
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
                    int CurrentIndex = GlobalVariables.GlobalPlayerControl.MediaList.IndexOf(GlobalVariables.GlobalPlayerControl.NextMedia);
                    GlobalVariables.GlobalPlayerControl.MediaList.Insert(CurrentIndex - 1, new MediaObject
                    {
                        Index = CurrentIndex - 1,
                        MetaData = NewTrack.RelatedTrack.Res,
                        Path = NewTrack.RelatedTrack.Res
                    });
                    GlobalVariables.GlobalPlayerControl.PreviousMedia = GlobalVariables.GlobalPlayerControl.MediaList[CurrentIndex - 1];
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

        /// <summary>
        /// Behandelt die Play-Pause-Schaltfläche
        /// </summary>
        /// <param name="sender">Das aufrufende Objekt</param>
        /// <param name="e">Eventparameter</param>
        private void ImagePlayGestureRecognizer_OnTapped(object sender, EventArgs e)
        {
            if (GlobalVariables.GlobalPlayerControl == null) return;
            if (GlobalVariables.GlobalPlayerControl.IsPlaying) GlobalVariables.GlobalPlayerControl.Pause();
            else
            {
                GlobalVariables.GlobalPlayerControl.Play();
                StartPositionTimer();
            }
        }

        /// <summary>
        /// Fügt der aktuellen Wiedergabeliste einen neuen Musiktitel hinzu
        /// </summary>
        /// <param name="MusicTrack">Der hinzuzufügende Musiktitel</param>
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

        /// <summary>
        /// Behandelt die Nächster-Titel-Schaltfläche
        /// </summary>
        /// <param name="sender">Das aufrufende Objekt</param>
        /// <param name="e">Eventparameter</param>
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

        /// <summary>
        /// Schaltet zwischen Wiedergabeliste und Albumcover um
        /// </summary>
        /// <param name="sender">Das aufrufende Objekt</param>
        /// <param name="e">Eventparameter</param>
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

        /// <summary>
        /// Wird vor dem Anzeigen der Seite ausgelöst
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            OnPropertyChanged("MusicItems");
            OnPropertyChanged("CurrentDeviceName");
            if (GlobalVariables.GlobalPlayerControl != null && !EventSet)
            {
                EventSet = true;
                Task.Delay(10);
                GlobalVariables.GlobalPlayerControl.PlayingStatusChanged += OnPlayingstatuschanged;
            }
            // Event einmal triggern, damit es evtl automatisch läuft
            if (GlobalVariables.GlobalPlayerControl != null && GlobalVariables.GlobalPlayerControl.IsPlaying) StartPositionTimer();
        }

        /// <summary>
        /// Startet die zyklische Positionsabfrage
        /// </summary>
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
                        if (GlobalVariables.GlobalPlayerControl != null)
                        {
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                int PosTemp = GlobalVariables.GlobalPlayerControl.GetCurrentPosition();
                                if (PosTemp > 0) SliderValue = PosTemp;
                            });
                        }
                        //Debug.WriteLine("Returning Position" + DateTime.Now);
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

        /// <summary>
        /// Stoppt die zyklische Positionsabfrage
        /// </summary>
        private void StopPositionTimer()
        {
            PositionTimerRun = false;
        }

        /// <summary>
        /// Clicked-Event des Ausgabegerät-Buttons
        /// </summary>
        /// <param name="sender">Das aufrufende Objekt</param>
        /// <param name="e">Eventparameter</param>
        private void DeviceChangeButton_OnClicked(object sender, EventArgs e)
        {
            List<string> MediaRenderers = new List<string>();
            foreach (var upnPDevice in GlobalVariables.UPnPMediaRenderer)
            {
                if (upnPDevice.Type == "DUMMY") continue;
                MediaRenderers.Add(upnPDevice.DeviceName);
            }
            Device.BeginInvokeOnMainThread(async () =>
            {
                string Temp = await DisplayActionSheet("Ausgabegerät Wechseln", "Abbrechen", null, MediaRenderers.ToArray());
                if (Temp != null && Temp != "Abbrechen") ChangeOutputDevice(Temp);
            });
        }

        /// <summary>
        /// Wechselt das aktuelle Ausgabegerät
        /// </summary>
        /// <param name="NewDevice">Das Gerät auf welches umgezogen werden soll</param>
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
            {   
                if (NewDevice == GlobalVariables.GlobalPlayerControl.oDevice.DeviceName) return;
                GlobalVariables.GlobalPlayerControl.Pause();
                PlayerControl oNewControl = new PlayerControl(GlobalVariables.UPnPMediaRenderer.Where(e => e.DeviceName == NewDevice).ToList()[0], new MediaObject() { Index = 0, Path = CurrentMusicTrack.Res }, GlobalVariables.GlobalPlayerControl.GetCurrentPosition());
                GlobalVariables.GlobalPlayerControl.Stop();
                GlobalVariables.GlobalPlayerControl.DeInit();
                GlobalVariables.GlobalPlayerControl.PlayingStatusChanged -= OnPlayingstatuschanged;
                GlobalVariables.GlobalPlayerControl = null;
                GlobalVariables.GlobalPlayerControl = oNewControl;
                GlobalVariables.GlobalPlayerControl.PlayingStatusChanged += OnPlayingstatuschanged;
                while (!GlobalVariables.GlobalPlayerControl.IsPlaying)
                {
                    Task.Delay(20);
                }
                OnPlayingstatuschanged();
                OnPropertyChanged("CurrentDeviceName");
                OnPropertyChanged("RemotePlayerControl");
            }
        }

        /// <summary>
        /// Clicked-Event der "Leiser"-Schaltfläche
        /// </summary>
        /// <param name="sender">Das aufrufende Objekt</param>
        /// <param name="e">Eventparameter</param>
        private void ButtonVolumeDown_OnClicked(object sender, EventArgs e)
        {
            if (GlobalVariables.GlobalPlayerControl != null)
            {
                int CurrentVolume = GlobalVariables.GlobalPlayerControl.CurrentVolume;
                if ((CurrentVolume - 5) > -1)
                {
                    GlobalVariables.GlobalPlayerControl.CurrentVolume = CurrentVolume - 5;
                }
                else
                {
                    GlobalVariables.GlobalPlayerControl.CurrentVolume = 0;
                }
            }
        }

        /// <summary>
        /// Clicked-Event der "Lauter"-Schaltfläche
        /// </summary>
        /// <param name="sender">Das aufrufende Objekt</param>
        /// <param name="e">Eventparameter</param>
        private void ButtonVolumeUp_OnClicked(object sender, EventArgs e)
        {
            if (GlobalVariables.GlobalPlayerControl != null)
            {
                int CurrentVolume = GlobalVariables.GlobalPlayerControl.CurrentVolume;
                if (CurrentVolume + 5 < 101)
                {
                    GlobalVariables.GlobalPlayerControl.CurrentVolume = CurrentVolume + 5;
                }
                else
                {  
                    GlobalVariables.GlobalPlayerControl.CurrentVolume = 100;
                }
            }
        }

        /// <summary>
        /// Wird aufgerufen wenn sich der Wert des Positionsindikators ändert
        /// </summary>
        /// <param name="sender">Das aufrufende Objekt</param>
        /// <param name="e">Eventparameter</param>
        private void PositionSlider_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            if ((e.NewValue - e.OldValue) > 3 || (e.NewValue - e.OldValue) < -5)
            {   //Manuelle Usereingabe
                // Timer starten der nach 20ms nachschaut ob sich der Wert nicht mehr geändert hat
                ManualVal = e.NewValue;
                Device.StartTimer(TimeSpan.FromMilliseconds(20), () =>
                {
                    double TempVal = e.NewValue;
                    if (ManualVal == TempVal)
                    {   // Wir nehmen an dass dies die letzte Wertänderung sein soll
                        if (GlobalVariables.GlobalPlayerControl != null)
                        {
                            Debug.WriteLine("Updating Position To: " + (int)TempVal);
                            GlobalVariables.GlobalPlayerControl.SetPosition((int)TempVal);
                        }
                    }
                    return false;
                });
            }
        }
    }
}
