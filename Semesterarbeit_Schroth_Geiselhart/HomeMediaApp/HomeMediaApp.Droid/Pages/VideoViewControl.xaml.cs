using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Widget;
using HomeMediaApp.Interfaces;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: Dependency(typeof(HomeMediaApp.Droid.Pages.VideoViewControl))]
namespace HomeMediaApp.Droid.Pages
{
    public partial class VideoViewControl : ContentPage, IVideoViewer
    {
        //VideoView VideoViewControlElement = new VideoView(Android.App.Application.Context);
        public VideoViewControl()
        {
            InitializeComponent();
            //StackLayoutControl.Children.Clear();
            //StackLayoutControl.Children.Add(VideoViewControlElement.ToView());
            //VideoViewControlElement.SetZOrderOnTop(true);
            //this.Content = VideoViewControlElement.ToView();
            ForceLayout();
        }

        public void ShowVideoFromUri(Uri FileUri)
        {
            //VideoViewControlElement.SetVideoURI(Android.Net.Uri.Parse(FileUri.ToString()));
            VideoViewControlControl.FileSource = FileUri.ToString();
        }

        public void ShwoVideoFromPath(string FilePath)
        {
            //VideoViewControlElement.SetVideoPath(FilePath);
        }

        public void Play()
        {
            //VideoViewControlElement.Start();
            //VideoViewControlElement.SetZOrderOnTop(true);
            //ForceLayout();
        }

        protected override void OnAppearing()
        {
            //VideoViewControlElement.SetZOrderOnTop(true);
            //ForceLayout();
        }
    }
}
