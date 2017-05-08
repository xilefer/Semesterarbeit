using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Widget;
using HomeMediaApp.Interfaces;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: Dependency(typeof(HomeMediaApp.Droid.Pages.VideoViewControl))]
namespace HomeMediaApp.Droid.Pages
{
    public partial class VideoViewControl : ContentPage, IVideoViewer
    {
        public VideoViewControl()
        {
            InitializeComponent();
            ForceLayout();
        }

        public void ShowVideoFromUri(Uri FileUri)
        {
            VideoContentViewControl.VideoViewControl.SetVideoURI(Android.Net.Uri.Parse(FileUri.ToString()));
            VideoContentViewControl.VideoViewControl.SeekTo(1);
            VideoContentViewControl.VideoViewControl.SetZOrderOnTop(true);
        }

        public void ShwoVideoFromPath(string FilePath)
        {
            VideoContentViewControl.VideoViewControl.SetVideoPath(FilePath);
        }

        public void Play()
        {
            VideoContentViewControl.VideoViewControl.Start();
        }

        protected override void OnAppearing()
        {
            //this.ShowVideoFromUri(new Uri("http://192.168.1.102:5001/get/131/Boy+with+taco+FAIL.mp4"));
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if(VideoContentViewControl.VideoViewControl.IsPlaying) VideoContentViewControl.VideoViewControl.StopPlayback();
        }

        private void PlayPauseButton_OnClicked(object sender, EventArgs e)
        {
            if (VideoContentViewControl.VideoViewControl.IsPlaying) VideoContentViewControl.VideoViewControl.Pause();
            else
            {
                VideoContentViewControl.VideoViewControl.Start();
            }
        }
    }
}
