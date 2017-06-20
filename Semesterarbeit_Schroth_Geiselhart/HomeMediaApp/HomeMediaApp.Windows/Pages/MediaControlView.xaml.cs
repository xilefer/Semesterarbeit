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
    public partial class MediaControlView : ContentView, IMediaPlayer
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
            MediaElementControl.Source = new Uri(FilePath);
            return true;
        }

        public void Pause()
        {
            MediaElementControl.Pause();
        }

        public void Play()
        {
            MediaElementControl.Play();
        }

        public void SeekTo(int Position)
        {
            if(MediaElementControl.CanSeek) MediaElementControl.Position = TimeSpan.FromSeconds(Position);
        }

        public PlayingState GetPlayingState()
        {
            return new PlayingState()
            {
                Current = (int)MediaElementControl.Position.TotalSeconds,
                Max = (int)MediaElementControl.NaturalDuration.TimeSpan.TotalSeconds
            };
        }

        public void SetName(string ItemName)
        {   //Hier gibt es keine Namensanzeige
            return;
        }

        public Action OnFinishedPlaying { get; set; }
    }
}
