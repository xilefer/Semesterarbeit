using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Media;
using Android.Views;
using Android.Widget;
using HomeMediaApp.Droid.Pages;
using HomeMediaApp.Interfaces;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Application = Android.App.Application;
using View = Xamarin.Forms.View;
using System.Diagnostics;

[assembly: Dependency(typeof(AndroidMediaPlayerControl))]
namespace HomeMediaApp.Droid.Pages
{
    public partial class AndroidMediaPlayerControl : ContentView, IMediaPlayer
    {
        MediaPlayer AndroidMediaPlayer = new MediaPlayer();
        private bool Prepared = false;
        private bool RunTimer = false;
        private int mSliderValue = 0;
        private string mSongName = "Kein Titel ausgewählt!";

        public string MediaPath = "";
        public string SongName
        {
            get { return mSongName; }
            set
            {
                if (mSongName == value) return;
                mSongName = value;
                OnPropertyChanged();
            }
        }

        public int SliderValue
        {
            get
            {
                return AndroidMediaPlayer.CurrentPosition / 1000;
            }
            set
            {
                if (mSliderValue == value) return;
                mSliderValue = value;
                OnPropertyChanged();
            }
        }

        public int SliderMax
        {
            get
            {
                if (Prepared) return AndroidMediaPlayer.Duration / 1000;
                else return 1;
            }
            set { }
        }

        public int SliderMin
        {
            get { return 0; }
            set { }
        }

        public AndroidMediaPlayerControl()
        {
            InitializeComponent();
            AndroidMediaPlayer.Prepared += AndroidMediaPlayerOnPrepared;
            BindingContext = this;
            ForceLayout();
        }

        private void AndroidMediaPlayerOnPrepared(object sender, EventArgs eventArgs)
        {
            Prepared = true;
        }

        public bool PlayFromUri(Uri FileUri)
        {
            MediaPath = FileUri.ToString();
            if (AndroidMediaPlayer.IsPlaying)
            {
                AndroidMediaPlayer.Stop();
                AndroidMediaPlayer.Reset();
            }
            AndroidMediaPlayer.SetAudioStreamType(Stream.Music);
            AndroidMediaPlayer.SetDataSource(Application.Context, Android.Net.Uri.Parse(FileUri.ToString()));
            AndroidMediaPlayer.Prepare();
            return true;
        }

        public bool PlayFromFile(string FilePath)
        {
            MediaPath = FilePath;
            if (AndroidMediaPlayer.IsPlaying) AndroidMediaPlayer.Stop();
            AndroidMediaPlayer.SetAudioStreamType(Stream.Music);
            AndroidMediaPlayer.SetDataSource(FilePath);
            AndroidMediaPlayer.Prepare();
            return true;
        }

        public void Pause()
        {
            if (AndroidMediaPlayer.IsPlaying)
            {
                AndroidMediaPlayer.Pause();
                ButtonPlayPause.Image = (FileImageSource)ImageSource.FromFile("play_icon_70.png");
                StopPositionTimer();
            }
        }

        public void Play()
        {
            if (Prepared)
            {
                AndroidMediaPlayer.Start();
                ButtonPlayPause.Image = (FileImageSource)ImageSource.FromFile("pause_icon_70.png");
                StartPositionTimer();
            }
        }

        private void StartPositionTimer()
        {
            if (!RunTimer)
            {
                RunTimer = true;
                Device.StartTimer(TimeSpan.FromSeconds(1), () =>
                {
                    OnPropertyChanged("SliderValue");
                    OnPropertyChanged("SliderMax");
                    return RunTimer;
                });
            }
        }

        private void StopPositionTimer()
        {
            RunTimer = false;
        }

        public PlayingState GetPlayingState()
        {
            return new PlayingState()
            {
                Current = AndroidMediaPlayer.CurrentPosition,
                Max = AndroidMediaPlayer.Duration
            };
        }

        public void SetName(string ItemName)
        {
            SongName = ItemName;
        }

        public void SeekTo(int Position)
        {
            try
            {
                AndroidMediaPlayer.SeekTo(Position * 1000);
            }
            catch (Java.Lang.IllegalStateException gEx)
            {
                Exception BaseException = gEx.GetBaseException();
                Debug.WriteLine("Fehler in SeekTo in AndroidMediaPlayerControl.xaml.cs" + BaseException.ToString());
            }
        }

        private void PlayPauseButton_OnClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(MediaPath)) return;
            if (AndroidMediaPlayer.IsPlaying) Pause();
            else
            {
                if (Prepared) Play();
            }
        }

        private void SongSlider_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if ((e.NewValue - e.OldValue) > 2 || (e.OldValue - e.NewValue) > 2)
            {
                AndroidMediaPlayer.SeekTo((int)e.NewValue * 1000);
            }
        }
    }
}
