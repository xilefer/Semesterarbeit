using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeMediaApp.Interfaces;
using Xamarin.Forms;

namespace HomeMediaApp.Classes
{
    public static class GlobalVariables
    {
        public static ObservableCollection<UPnPDevice> UPnPMediaServers = new ObservableCollection<UPnPDevice>();

        public static ObservableCollection<UPnPDevice> UPnPMediaRenderer = new ObservableCollection<UPnPDevice>();

        public static PlayerControl GlobalPlayerControl = null;

        public static ContentView GlobalMediaPlayerDevice = null;

        public static ContentView GlobalVideoViewerDevice = null;

        public static IMediaPlayerControl GlobalMediaPlayerControl
        {
            get { return  GlobalMediaPlayerDevice as IMediaPlayerControl; }
        }

        public static IVideoViewer GlobalVideoViewer
        {
            get { return GlobalVideoViewerDevice as IVideoViewer; }
        }
    }
}
