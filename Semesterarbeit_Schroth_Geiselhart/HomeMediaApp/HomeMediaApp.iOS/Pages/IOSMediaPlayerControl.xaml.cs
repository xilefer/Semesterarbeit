using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioToolbox;
using Foundation;
using HomeMediaApp.Interfaces;
using UIKit;

using Xamarin.Forms;

namespace HomeMediaApp.iOS.Pages
{
    
    public partial class IOSMediaPlayerControl : ContentView, IMediaPlayerControl
    {
        private MusicPlayerStatus MusicPlayerStatus;
        private MusicPlayer MusicPlayerControl = null; 
        public IOSMediaPlayerControl()
        {
            InitializeComponent();
            MusicPlayerControl = MusicPlayer.Create(out MusicPlayerStatus);
        }

        public bool PlayFromUri(Uri FileUri)
        {
            MusicSequence MusicSequence = new MusicSequence();
            MusicSequence.LoadFile(NSUrl.CreateFileUrl(FileUri.ToString(), null), MusicSequenceFileTypeID.Any);
            MusicPlayerControl.MusicSequence = MusicSequence;
            return true;
        }

        public bool PlayFromFile(string FilePath)
        {
            MusicSequence MusicSequence = new MusicSequence();
            MusicSequence.LoadFile(NSUrl.CreateFileUrl(FilePath, null), MusicSequenceFileTypeID.Any);
            MusicPlayerControl.MusicSequence = MusicSequence;
            return true;
        }

        public void Pause()
        {
            if (MusicPlayerControl.IsPlaying) MusicPlayerControl.Stop();
        }

        public void Play()
        {
            MusicPlayerStatus = MusicPlayerControl.Start();
        }

        public void SeekTo(int Position)
        {
            MusicPlayerControl.Time = Position;
        }

        public PlayingState GetPlayingState()
        {
            return new PlayingState()
            {
                Current = (int)MusicPlayerControl.Time,
                Max = (int)MusicPlayerControl.MusicSequence.GetTrack(0).TrackLength
            };
        }

        public void SetName(string ItemName)
        {   //Hier gibt es keine Titelanzeige
            return;
        }

        public Action OnFinishedPlaying { get; set; }
    }
}
