﻿using System;
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

[assembly: Dependency(typeof(AndroidMediaPlayerControl))]
namespace HomeMediaApp.Droid.Pages
{
    public partial class AndroidMediaPlayerControl : ContentView, IMediaPlayerControl
    {
        MediaPlayer AndroidMediaPlayer = new MediaPlayer();
        ImageView ImageViewPlayPause = new ImageView(Application.Context);
        TapGestureRecognizer TapRecognizer = new TapGestureRecognizer();
        private Xamarin.Forms.View TempView = null;
        private bool Prepared = false;

        private string mSongName = "";

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

        public AndroidMediaPlayerControl()
        {
            InitializeComponent();
            AndroidMediaPlayer.Prepared += AndroidMediaPlayerOnPrepared;
            StackLayoutPlayer.Children.Clear();
            TempView = ImageViewPlayPause.ToView();
            TempView.VerticalOptions = LayoutOptions.FillAndExpand;
            TempView.HorizontalOptions = LayoutOptions.FillAndExpand;
            TempView.BackgroundColor = Color.White;
            TapRecognizer.TappedCallback = new Action<View, object>((sender, args) => View_OnClick(sender, null));
            TempView.GestureRecognizers.Add(TapRecognizer);
            StackLayoutPlayer.Children.Add(TempView);
            ImageViewPlayPause.SetImageResource(Resource.Drawable.play_icon);
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
