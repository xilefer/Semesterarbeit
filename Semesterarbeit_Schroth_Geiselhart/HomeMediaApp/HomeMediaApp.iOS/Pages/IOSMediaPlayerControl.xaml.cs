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
            throw new NotImplementedException();
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Play()
        {
            MusicPlayerStatus = MusicPlayerControl.Start();
        }

        public Action OnFinishedPlaying { get; set; }
    }
}
