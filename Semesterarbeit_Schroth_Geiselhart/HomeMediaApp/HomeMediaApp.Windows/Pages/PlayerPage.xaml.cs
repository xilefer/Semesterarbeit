using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeMediaApp.Interfaces;
using HomeMediaApp.Windows.Pages;
using Xamarin.Forms;
using System.Diagnostics;
using Windows.UI.Xaml.Controls;


namespace HomeMediaApp.Windows.Pages
{
    public partial class PlayerPage : ContentPage, IMediaPlayer
    {
        MediaElement MediaPlayer = new MediaElement();
        public PlayerPage()
        {
            InitializeComponent();
        }

        public bool PlayFromUri(Uri FileUri)
        {
            bool ReturnValue = false;
            try
            {
                MediaPlayer.Source = FileUri;
                ReturnValue = true;
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
            return ReturnValue;
        }

        public bool PlayFromFile(string FilePath)
        {
            throw new NotImplementedException();
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Play()
        {
            try
            {
                MediaPlayer.Play();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public Action OnFinishedPlaying { get; set; }
    }
}
