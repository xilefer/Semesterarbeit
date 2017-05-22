using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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
                return "Ausgabegerät: " +  GlobalVariables.GlobalPlayerControl.oDevice.DeviceName;
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
                if (CurrentMusicTrack != null) return CurrentMusicTrack.DurationSec;
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
            if (GlobalVariables.GlobalPlayerControl != null) GlobalVariables.GlobalPlayerControl.PlayingStatusChanged += new PlayingStatusChangedEvent(() => Device.BeginInvokeOnMainThread(
                () => OnPropertyChanged("PlayPauseSource")));
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
            if(PlayList == null) return;
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
                ChangeMusicTrack(PlayList.MusicItems[IndexOfItem]);
                ForceLayout();
            }
        }

        private void ChangeMusicTrack(MusicItem NewTrack)
        {
            CurrentMediaIndex = PlayList.MusicItems.IndexOf(NewTrack);
            GlobalVariables.GlobalPlayerControl.MediaList.Insert(CurrentMediaIndex, new MediaObject() { Index = GlobalVariables.GlobalPlayerControl.MediaList.Count, Path = NewTrack.RelatedTrack.Res, MetaData = ""});
            GlobalVariables.GlobalPlayerControl.Next();
            // TODO: Neuen Song wiedergeben
        }

        private void ImagePlayGestureRecognizer_OnTapped(object sender, EventArgs e)
        {
            if(GlobalVariables.GlobalPlayerControl == null) return;
            if (GlobalVariables.GlobalPlayerControl.IsPlaying) GlobalVariables.GlobalPlayerControl.Pause();
            else GlobalVariables.GlobalPlayerControl.Play();
        }

        /// <summary>
        /// Fügt der aktuellen Wiedergabeliste einen neuen Musiktitel hinzu
        /// </summary>
        /// <param name="MusicTrack"></param>
        public void AddMusicTrackToPlayList(MusicItem MusicTrack)
        {
            if (PlayList == null)
            {
                PlayList = new PlaylistItem("Aktuelle Wiedergabe");
            }
            PlayList.MusicItems.Add(MusicTrack);
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
                ChangeMusicTrack(PlayList.MusicItems[IndexOfItem]);
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
        }

        private void PlayListView_OnItemTapped(object sender, ItemTappedEventArgs e)
        {
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
            ChangeMusicTrack(PlayList.MusicItems[index]);
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
                string Temp =  await DisplayActionSheet("Ausgabegerät Wechseln", "Abbrechen", null, MediaRenderers.ToArray());
                if(Temp != null && Temp != "Abbrechen") ChangeOutputDevice(Temp);
            });
        }

        private void ChangeOutputDevice(string NewDevice)
        {
            // TODO: Das ausgabegerät wechseln
            throw new NotImplementedException();
        }
    }
}
