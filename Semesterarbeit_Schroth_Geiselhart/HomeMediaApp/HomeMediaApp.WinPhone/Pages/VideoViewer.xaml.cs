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

	    public void ShwoVideoFromPath(string FilePath)
	    {
	        throw new NotImplementedException();
	    }

	    public void Play()
	    {
	        MediaElementControl.Play();
	    }
	}
}
