using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Widget;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

namespace HomeMediaApp.Droid.Views
{
    public partial class VideoContentView : ContentView
    {
        public VideoView VideoViewControl = new VideoView(Android.App.Application.Context);
        public VideoContentView()
        {
            InitializeComponent();
            StackLayoutContent.Children.Add(VideoViewControl);
            this.Content = VideoViewControl.ToView();
            ForceLayout();
        }
    }
}
