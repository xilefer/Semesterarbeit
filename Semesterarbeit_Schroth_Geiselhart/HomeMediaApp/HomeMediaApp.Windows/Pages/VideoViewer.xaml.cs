using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media;
using Windows.UI.Xaml.Controls;
using HomeMediaApp.Interfaces;
using Xamarin.Forms;
using Xamarin.Forms.Platform.WinRT;
using Xamarin.Forms.Xaml;

[assembly: Dependency(typeof(HomeMediaApp.Windows.Pages.VideoViewer))]
namespace HomeMediaApp.Windows.Pages
{
    public partial class VideoViewer : ContentView, IVideoViewer
    {
        MediaElement MediaElementControl = new MediaElement();

        public VideoViewer()
        {
            InitializeComponent();
            StackLayoutContent.Children.Clear();
            MediaElementControl.AreTransportControlsEnabled = true;
            MediaElementControl.AutoPlay = false;
            StackLayoutContent.Children.Add(MediaElementControl);
            this.Content = MediaElementControl.ToView();
        }

        public void ShowVideoFromUri(Uri FileUri)
        {
            MediaElementControl.Source = FileUri;

        }

        public void ShowVideoFromPath(string FilePath)
        {
            throw new NotImplementedException();
        }

        public void Play()
        {
            MediaElementControl.Play();
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public PlayingState GetPlayingState()
        {
            throw new NotImplementedException();
        }

        public void SeekTo(int Position)
        {
            throw new NotImplementedException();
        }
    }
}
