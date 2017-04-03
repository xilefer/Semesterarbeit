using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using HomeMediaApp.Droid.Classes;
using HomeMediaApp.Interfaces;
using Java.IO;
using Xamarin.Forms;
using Application = Android.App.Application;

[assembly: Dependency(typeof(AndroidMusicPlayer))]
namespace HomeMediaApp.Droid.Classes
{
    public class AndroidMusicPlayer : IMediaPlayer
    {
        MediaPlayer AndroidMediaPlayer = new MediaPlayer();
        private bool Prepared = false;

        public AndroidMusicPlayer()
        {
            AndroidMediaPlayer.Prepared += AndroidMediaPlayerOnPrepared;
        }

        private void AndroidMediaPlayerOnPrepared(object sender, EventArgs eventArgs)
        {
            Prepared = true;
        }

        public bool PlayFromUri(Uri FileUri)
        {
            if(AndroidMediaPlayer.IsPlaying) AndroidMediaPlayer.Stop();
            AndroidMediaPlayer.SetAudioStreamType(Stream.Music);
            AndroidMediaPlayer.SetDataSource(Application.Context, Android.Net.Uri.Parse(FileUri.ToString()));
            AndroidMediaPlayer.Prepare();
            return true;
        }

        public bool PlayFromFile(string FilePath)
        {
            if(AndroidMediaPlayer.IsPlaying) AndroidMediaPlayer.Stop();
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
    }
}