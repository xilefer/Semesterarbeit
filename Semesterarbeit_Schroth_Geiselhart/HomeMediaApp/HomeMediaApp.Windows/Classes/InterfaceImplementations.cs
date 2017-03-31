using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Media;
using Windows.UI.Xaml.Controls;
using HomeMediaApp.Interfaces;
using HomeMediaApp.Windows.Classes;
using Xamarin.Forms;


namespace HomeMediaApp.Windows.Classes
{

    public class MediaPlayerDevice : IMediaPlayer
    {
        public MediaElement Song = new MediaElement();
        

        public Action OnFinishedPlaying
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Play()
        {
            try
            {
                Song.Play();
                
            }
            catch (Exception gEx)
            {
                Debug.WriteLine(gEx.ToString());
            }
        }

        public bool PlayFromFile(string FilePath)
        {
            throw new NotImplementedException();
        }

        public bool PlayFromUri(Uri FileUri)
        {
            bool ReturnValue = false;
            try
            {
                Song.Source = FileUri;
                ReturnValue = true;
            }
            catch (Exception gEx)
            {
                Debug.WriteLine(gEx);
            }
            return ReturnValue;
        }
    }
}
