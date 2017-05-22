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

[assembly: Dependency(typeof(AndroidMediaPlayerControl))]
namespace HomeMediaApp.Droid.Pages
{
    public partial class AndroidMediaPlayerControl : ContentView, IMediaPlayerControl
    {
        MediaPlayer AndroidMediaPlayer = new MediaPlayer();
        ImageView ImageViewPlayPause = new ImageView(Application.Context);
        private Xamarin.Forms.View TempView = null;
        private bool Prepared = false;
        public AndroidMediaPlayerControl()
        {
            InitializeComponent();
            AndroidMediaPlayer.Prepared += AndroidMediaPlayerOnPrepared;
            StackLayoutPlayer.Children.Clear();
            ImageViewPlayPause.SetImageResource(Resource.Drawable.play_icon);
            TempView = ImageViewPlayPause.ToView();
            TempView.VerticalOptions = LayoutOptions.FillAndExpand;
            TempView.HorizontalOptions = LayoutOptions.CenterAndExpand;
            TempView.BackgroundColor = Color.White;
            // TODO: Gesture Recognizer
            StackLayoutPlayer.Children.Add(TempView);
            //PlayPauseImage.Source = ImageSource.FromResource("HomeMediaApp.Droid.drawable");
            ForceLayout();
        }
        

        private void AndroidMediaPlayerOnPrepared(object sender, EventArgs eventArgs)
        {
            Prepared = true;
        }

        public bool PlayFromUri(Uri FileUri)
        {
            if (AndroidMediaPlayer.IsPlaying) AndroidMediaPlayer.Stop();
            AndroidMediaPlayer.SetAudioStreamType(Stream.Music);
            AndroidMediaPlayer.SetDataSource(Application.Context, Android.Net.Uri.Parse(FileUri.ToString()));
            AndroidMediaPlayer.Prepare();
            return true;
        }

        public bool PlayFromFile(string FilePath)
        {
            if (AndroidMediaPlayer.IsPlaying) AndroidMediaPlayer.Stop();
            AndroidMediaPlayer.SetAudioStreamType(Stream.Music);
            var fd = global::Android.App.Application.Context.Assets.OpenFd(FilePath);
            AndroidMediaPlayer.SetDataSource(fd);
            AndroidMediaPlayer.Prepare();
            return true;
        }

        public void Pause()
        {
            if (AndroidMediaPlayer.IsPlaying)
            {
                AndroidMediaPlayer.Pause();
            }
        }

        public void Play()
        {
            if (Prepared)
            {
                AndroidMediaPlayer.Start();
            }
        }

        public Action OnFinishedPlaying { get; set; }

        private void NextSongTapGestureRecognizer_OnTapped(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void PlayPauseTapGestureRecognizer_OnTapped(object sender, EventArgs e)
        {

        }

        private void LastSongTapGestureRecognizer_OnTapped(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void View_OnClick(object sender, EventArgs e)
        {
            if (AndroidMediaPlayer.IsPlaying) Pause();
            Play();
        }
    }
}
