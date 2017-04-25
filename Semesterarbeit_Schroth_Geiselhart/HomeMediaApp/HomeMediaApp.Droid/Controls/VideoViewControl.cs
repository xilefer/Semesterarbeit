using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using HomeMediaApp.Droid.Pages;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

namespace HomeMediaApp.Droid.Controls
{
    class VideoViewControl : Xamarin.Forms.View
    {
        public Action StopAction;
        public VideoViewControl() { }
        public static readonly  BindableProperty FileSourcreProperty = BindableProperty.Create<VideoViewControl, string>(p => p.FileSource, string.Empty);

        public string FileSource
        {
            get { return (string) GetValue(FileSourcreProperty); } 
            set { SetValue(FileSourcreProperty, value);}
        }

        public void Stop()
        {
            if (StopAction != null) StopAction();
        }

        class VideoViewRenderer : ViewRenderer<HomeMediaApp.Droid.Controls.VideoViewControl, VideoView>,
            ISurfaceHolderCallback
        {
            private VideoView videoview;
            private MediaPlayer player;

            public void Play(string URI)
            {
                player.SetDataSource(Context, Android.Net.Uri.Parse(URI));
                player.Prepare();
                player.Start();
                Control.Layout(0,200,player.VideoHeight, player.VideoWidth); 
            }

            public VideoViewRenderer() { }

            public void SurfaceChanged(ISurfaceHolder holder, Format format, int width, int height)
            {
            }

            protected override void OnElementChanged(ElementChangedEventArgs<VideoViewControl> e)
            {
                base.OnElementChanged(e);
                e.NewElement.StopAction = () =>
                {
                    this.player.Stop();
                    this.Control.StopPlayback();
                };
                videoview = new VideoView(Context);
                base.SetNativeControl(videoview);
                Control.Holder.AddCallback(this);
                player = new MediaPlayer();
                Play(e.NewElement.FileSource);    
            }

            public void SurfaceCreated(ISurfaceHolder holder)
            {
                player.SetDisplay(holder);
            }

            public void SurfaceDestroyed(ISurfaceHolder holder)
            {
            }
            
        }
    }
}