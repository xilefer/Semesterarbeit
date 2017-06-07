using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using HomeMediaApp.Interfaces;
using HomeMediaApp.Windows.Pages;
using Xamarin.Forms;
using Xamarin.Forms.Platform.WinRT;

[assembly: Dependency(typeof(MediaControlView))]
namespace HomeMediaApp.Windows.Pages
{
    public partial class MediaControlView : ContentView, IMediaPlayerControl
    {
        MediaElement MediaElementControl = new MediaElement();

        public MediaControlView()
        {
            InitializeComponent();
            MediaElementControl.AreTransportControlsEnabled = true;
            MediaElementControl.AutoPlay = false;
            StackLayoutContent.Children.Clear();
            StackLayoutContent.Children.Add(MediaElementControl);
            this.Content = MediaElementControl.ToView();
        }

        public bool PlayFromUri(Uri FileUri)
        {

            MediaElementControl.Source = FileUri;
            return true;
        }

        public bool PlayFromFile(string FilePath)
        {
            throw new NotImplementedException();
        }

        public void Pause()
        {
            //throw new NotImplementedException();
        }

        public void Play()
        {
            MediaElementControl.Play();
        }

        public void SeekTo(int Position)
        {
            throw new NotImplementedException();
        }

        public PlayingState GetPlayingState()
        {
            throw new NotImplementedException();
        }

        public Action OnFinishedPlaying { get; set; }
    }
}
