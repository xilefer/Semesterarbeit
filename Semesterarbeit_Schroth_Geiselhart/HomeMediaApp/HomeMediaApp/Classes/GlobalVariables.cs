using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using HomeMediaApp.Interfaces;
using HomeMediaApp.Pages;
using Xamarin.Forms;

namespace HomeMediaApp.Classes
{
    public static class GlobalVariables
    {
        #region CONST Strings
        public const string BaseShowDetailsActionName = "MEDIAEXPLORER_BASE_SHOW_DETAILS";
        public const string FolderOpenActionName = "MEDIAEXPLORER_FOLDER_OPEN";
        public const string MusicPlayActionName = "MEDIAEXPLORER_MUSIC_PLAY";
        public const string ImageOpenActionName = "MEDIAEXPLORER_IMAGE_OPEN";
        public const string VideoPlayActionName = "MEDIAEXPLORER_VIDEO_PLAY";
        public const string PlaylistPlayActionName = "MEDIAEXPLORER_PLAYLIST_PLAY";
        public const string MusicAddToPlayLIstActionName = "MEDIAEXPLORER_MUSIC_ADD_TO_PLAYLIST";
        public const string PlaylistAddToPlayLIstActionName = "MEDIAEXPLORER_PLAYLIST_ADD_TO_PLAYLIST";
        public const string RemoveTrackFromPlayListActionName = "PLAYLIST_VIEW_REMOVE_TRACK";
        #endregion

        public static ObservableCollection<UPnPDevice> UPnPMediaServers = new ObservableCollection<UPnPDevice>()
        {
            new UPnPDevice()
            {
                Config = null,
                DeviceAddress = null,
                DeviceMethods = null,
                DeviceName = "Keine Medienserver gefunden!",
                Type = "DUMMY"
            }
        };

        public static ObservableCollection<UPnPDevice> UPnPMediaRenderer = new ObservableCollection<UPnPDevice>()
        {
            new UPnPDevice()
            {
                Config = null,
                DeviceAddress = null,
                DeviceMethods = null,
                DeviceName = "Keine Ausgabegeräte gefunden!",
                Type = "DUMMY"
            }
        };

        public static PlayerControl GlobalPlayerControl = null;

        public static ContentView GlobalMediaPlayerDevice = null;

        public static ContentView GlobalVideoViewerDevice = null;

        public static RemoteMediaPlayerPage GlobalRemoteMediaPlayerPage = null;

        public static IMediaPlayerControl GlobalMediaPlayerControl
        {
            get { return GlobalMediaPlayerDevice as IMediaPlayerControl; }
        }

        public static IVideoViewer GlobalVideoViewer
        {
            get { return GlobalVideoViewerDevice as IVideoViewer; }
        }
    }
}
