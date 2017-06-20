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
            MediaElementControl.Source = new Uri(FilePath);
        }

        public void Play()
        {
            MediaElementControl.Play();
        }

        public void Pause()
        {
            MediaElementControl.Pause();
        }

        public PlayingState GetPlayingState()
        {
            return new PlayingState()
            {
                Current = (int)MediaElementControl.Position.TotalSeconds,
                Max = (int)MediaElementControl.NaturalDuration.TimeSpan.TotalSeconds
            };
        }

        public void SeekTo(int Position)
        {
            if (MediaElementControl.CanSeek) MediaElementControl.Position = TimeSpan.FromSeconds(Position);
        }
    }
}
