using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using HomeMediaApp.Interfaces;
using Xamarin.Forms;
using Xamarin.Forms.Platform.WinRT;

[assembly: Dependency(typeof(HomeMediaApp.WinPhone.Pages.VideoViewer))]
namespace HomeMediaApp.WinPhone.Pages
{
	public partial class VideoViewer : ContentView, IVideoViewer
	{
        MediaElement MediaElementControl = new MediaElement();
		public VideoViewer ()
		{
			InitializeComponent ();
            StackLayoutControl.Children.Clear();
		    MediaElementControl.AutoPlay = false;
		    MediaElementControl.AreTransportControlsEnabled = true;
            StackLayoutControl.Children.Add(MediaElementControl);
		    this.Content = MediaElementControl.ToView();
		}

	    public void ShowVideoFromUri(Uri FileUri)
	    {
	        MediaElementControl.Source = FileUri;
	    }

	    public void ShowVideoFromPath(string FilePath)
	    {
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
	        if(MediaElementControl.CanSeek) MediaElementControl.Position = TimeSpan.FromSeconds(Position);
	    }
	}
}
