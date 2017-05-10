using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeMediaApp.Classes;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace HomeMediaApp.Pages
{
    public partial class RemoteMediaPlayerPage : ContentPage
    {
        private int CurrentMediaIndex = 0;
        private int nSliderValue = 0;

        public int SliderValue
        {
            get { return nSliderValue; }
            set
            {
                if (nSliderValue == value) return;
                nSliderValue = value;
                OnPropertyChanged();
            }
        }

        public PlayerControl RemotePlayerControl
        {
            get { return GlobalVariables.GlobalPlayerControl; }
            set { GlobalVariables.GlobalPlayerControl = value; }
        }

        public ImageSource PlayPauseSource
        {
            get
            {
                if (GlobalVariables.GlobalPlayerControl != null)
                {
                    if (GlobalVariables.GlobalPlayerControl.IsPlaying) return ImageSource.FromResource("HomeMediaApp.Icons.pause_icon.png");
                    return ImageSource.FromResource("HomeMediaApp.Icons.play_icon.png");
                }
                return ImageSource.FromResource("HomeMediaApp.Icons.play_icon.png");
            }
            set { }
        }

        public RemoteMediaPlayerPage()
        {
            InitializeComponent();
            BindingContext = this;
            if(GlobalVariables.GlobalPlayerControl != null) GlobalVariables.GlobalPlayerControl.PlayingStatusChanged += new PlayingStatusChangedEvent(() => Device.BeginInvokeOnMainThread(
                () => OnPropertyChanged("PlayPauseSource")));
            OnPropertyChanged("PlayPauseSource");
        }

        private void ImageLastGestureRecognizer_OnTapped(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void ImagePlayGestureRecognizer_OnTapped(object sender, EventArgs e)
        {
            if (GlobalVariables.GlobalPlayerControl.IsPlaying) GlobalVariables.GlobalPlayerControl.Pause();
            else GlobalVariables.GlobalPlayerControl.Play(CurrentMediaIndex);
        }

        private void ImageNextGestureRecognizer_OnTapped(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
