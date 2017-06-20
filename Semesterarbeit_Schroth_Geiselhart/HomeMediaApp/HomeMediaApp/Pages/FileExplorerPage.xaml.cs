using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;
using HomeMediaApp.Classes;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using HomeMediaApp.Interfaces;

namespace HomeMediaApp.Pages
{
    /// <summary>
    /// Hintergrundprogrammcode der FileExplorer-Oberfläche (in Dokumentation auch MediaExplorer)
    /// </summary>
    public partial class FileExplorerPage : ContentPage
    {
        private FolderItem mMasterItem = new FolderItem("Master");
        private bool DetailResponseReceived = false;
        private string CurrentDetailText = "";
        private ObservableCollection<FileExplorerItemBase> mExplorerItems = new ObservableCollection<FileExplorerItemBase>();
        private PlayList CreatedPlayList = null;
        private bool CreatePlayListResponseReceived = false;
        private bool BrowseChildrenReceived { get; set; } = false;
        private bool BrowseActionPlayListReceived { get; set; } = false;

        public FolderItem MasterItem
        {
            get { return mMasterItem; }
            set
            {
                if (mMasterItem == value) return;
                mMasterItem = value;
                OnPropertyChanged();
                OnPropertyChanged("ExplorerItems");
                OnPropertyChanged("CurrentDirectory");
            }
        }

        public string CurrentDirectory
        {
            get
            {
                string Path = MasterItem.DisplayName;
                FolderItem Parent = MasterItem.Parent;
                while (Parent != null)
                {
                    Path = Parent.DisplayName + "/" + Path;
                    Parent = Parent.Parent;
                }
                return Path;
            }
        }

        public ObservableCollection<FileExplorerItemBase> ExplorerItems
        {
            get { return mMasterItem.Childrens; }
            set
            {
                if (mMasterItem.Childrens == value) return;
                mMasterItem.Childrens = value;
                OnPropertyChanged();
            }
        }

        public UPnPDevice CurrentDevice { get; set; }

        /// <summary>
        /// Konstruktor
        /// </summary>
        public FileExplorerPage()
        {
            InitializeComponent();
            BindingContext = this;
            GlobalVariables.GlobalMediaPlayerDevice = DependencyService.Get<IMediaPlayer>() as ContentView;
            GlobalVariables.GlobalVideoViewerDevice = DependencyService.Get<IVideoViewer>() as ContentView; ;
            PlayerStackLayout.Children.Clear();
            PlayerStackLayout.Children.Add(GlobalVariables.GlobalMediaPlayerDevice);
            PlayerStackLayout.ForceLayout();
            // Subscription für die View-Events die für die Kontextaktionen der ListView-Elemente benötigt werden
            // Detailfunktion
            MessagingCenter.Subscribe<ViewCellBase, FileExplorerItemBase>(this, GlobalVariables.BaseShowDetailsActionName, (sender, arg) =>
            {
                ShowDetails(arg);
            });
            // Ordner öffnen
            MessagingCenter.Subscribe<FolderViewCell, FolderItem>(this, GlobalVariables.FolderOpenActionName, (sender, arg) =>
            {
                this.MasterItem = arg;
                BrowseChildrens(MasterItem);
            });
            // Musik wiedergeben
            MessagingCenter.Subscribe<MusicViewCell, MusicItem>(this, GlobalVariables.MusicPlayActionName, (sender, arg) =>
                {
                    List<string> MediaRenderer = new List<string>();
                    foreach (UPnPDevice upnPMediaServer in GlobalVariables.UPnPMediaRenderer)
                    {
                        if (upnPMediaServer.Type == "DUMMY") continue;
                        if (!upnPMediaServer.Protocoltypes.ContainsKey("audio")) continue;
                        MediaRenderer.Add(upnPMediaServer.DeviceName);
                    }
                    MediaRenderer.Add("Dieses Gerät");
                    MusicItemTapped(arg, MediaRenderer.ToArray());
                });
            // Musik in aktuelle Wiedergabeliste einfügen
            MessagingCenter.Subscribe<MusicViewCell, MusicItem>(this, GlobalVariables.MusicAddToPlayLIstActionName, (sender, arg) =>
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        GlobalVariables.GlobalRemoteMediaPlayerPage.AddMusicTrackToPlayList(arg, false);
                    });
                });
            // Bild anzeigen
            MessagingCenter.Subscribe<ImageViewCell, PictureItem>(this, GlobalVariables.ImageOpenActionName, (sender, arg) =>
                {
                    List<string> MediaRenderer = new List<string>();
                    foreach (UPnPDevice upnPMediaServer in GlobalVariables.UPnPMediaRenderer)
                    {
                        if (upnPMediaServer.Type == "DUMMY") continue;
                        MediaRenderer.Add(upnPMediaServer.DeviceName);
                    }
                    MediaRenderer.Add("Dieses Gerät");
                    PictureItemTapped(arg, MediaRenderer.ToArray());
                });
            // Video wiedergeben
            MessagingCenter.Subscribe<VideoViewCell, VideoItem>(this, GlobalVariables.VideoPlayActionName, (sender, arg) =>
                {
                    List<string> MediaRenderer = new List<string>();
                    foreach (UPnPDevice upnPMediaServer in GlobalVariables.UPnPMediaRenderer)
                    {
                        if (upnPMediaServer.Type == "DUMMY") continue;
                        MediaRenderer.Add(upnPMediaServer.DeviceName);
                    }
                    MediaRenderer.Add("Dieses Gerät");
                    VideoItemTapped(arg, MediaRenderer.ToArray());
                });
            // Playlist wiedergeben
            MessagingCenter.Subscribe<PlayListViewCell, PlaylistItem>(this, GlobalVariables.PlaylistPlayActionName, (sender, arg) =>
                {
                    List<string> MediaRenderer = new List<string>();
                    foreach (UPnPDevice upnPMediaServer in GlobalVariables.UPnPMediaRenderer)
                    {
                        if (upnPMediaServer.Type == "DUMMY") continue;
                        MediaRenderer.Add(upnPMediaServer.DeviceName);
                    }
                    MediaRenderer.Add("Dieses Gerät");
                    PlaylistItemTapped(arg, MediaRenderer.ToArray());
                });
            // Playlist zur Wiedergabeliste anfügen
            MessagingCenter.Subscribe<PlayListViewCell, PlaylistItem>(this, GlobalVariables.PlaylistAddToPlayLIstActionName, (sender, arg) =>
            {
                PlayList playlist = CreatePlayList(arg);
                if (playlist.MusicTitles.Count == 0) return;
                foreach (MusicItem oItem in playlist.MusicTitles)
                {
                    GlobalVariables.GlobalRemoteMediaPlayerPage.AddMusicTrackToPlayList(oItem, false);
                }
            });
        }

        /// <summary>
        /// Ruft die Details eines Objekts ab und zeigt diese an
        /// </summary>
        /// <param name="Item">Objekt zu dem Details abgerufen werden sollen</param>
        private void ShowDetails(FileExplorerItemBase Item)
        {
            UPnPService ContentDirectoryService = CurrentDevice.DeviceMethods.FirstOrDefault(e => e.ServiceID.ToLower() == "contentdirectory");
            if (ContentDirectoryService == null) return;
            UPnPAction BrowseAction = ContentDirectoryService.ActionList.FirstOrDefault(e => e.ActionName.ToLower() == "browse");
            if (BrowseAction == null) return;
            #region BrowseVariablen
            UPnPStateVariables.A_ARG_TYPE_BrowseFlag = UPnPBrowseFlag.BrowseDirectChildren;
            UPnPStateVariables.A_ARG_TYPE_Count = "100";
            UPnPStateVariables.A_ARG_TYPE_Index = "0";
            switch (Item.ItemType)
            {
                case FileExplorerItemType.MUSIC:
                    UPnPStateVariables.A_ARG_TYPE_ObjectID = (Item as MusicItem).RelatedTrack.id;
                    break;
                case FileExplorerItemType.VIDEO:
                    UPnPStateVariables.A_ARG_TYPE_ObjectID = (Item as VideoItem).RelatedVideo.id;
                    break;
                case FileExplorerItemType.PICTURE:
                    UPnPStateVariables.A_ARG_TYPE_ObjectID = (Item as PictureItem).RelatedPhoto.id;
                    break;
                case FileExplorerItemType.FOLDER:
                    UPnPStateVariables.A_ARG_TYPE_ObjectID = (Item as FolderItem).RelatedContainer.id;
                    break;
                case FileExplorerItemType.PLAYLIST:
                    UPnPStateVariables.A_ARG_TYPE_ObjectID = (Item as PlaylistItem).RelatedContainer.id;
                    break;
                case FileExplorerItemType.ELSE:
                    return;
            }
            UPnPStateVariables.A_ARG_TYPE_SortCriteria = "+upnp:artist";
            Type TypeInfo = typeof(UPnPStateVariables);
            List<Tuple<string, string>> ArgList = new List<Tuple<string, string>>();
            List<UPnPActionArgument> InArgs = new List<UPnPActionArgument>();
            foreach (UPnPActionArgument oArg in BrowseAction.ArgumentList)
            {
                if (oArg.Direction == "in")
                {
                    InArgs.Add(oArg);
                }
            }
            foreach (UPnPActionArgument Arg in InArgs)
            {
                PropertyInfo ResultProperty = TypeInfo.GetRuntimeProperty(Arg.RelatedStateVariable);
                if (ResultProperty != null)
                {
                    ArgList.Add(new Tuple<string, string>(Arg.Name, ResultProperty.GetValue(null).ToString()));
                }
                else
                {
                    throw new Exception("Die Funktion konnte nicht ausgeführt werden!");
                }
            }
            string sRequestURI = CurrentDevice.DeviceAddress.Scheme + "://" + CurrentDevice.DeviceAddress.Authority;
            if (sRequestURI.Length == 0)
            {
                throw new Exception("Die Funktion konnte nicht ausgeführt werden!");
            }
            if (sRequestURI.EndsWith("/")) sRequestURI = sRequestURI.Substring(0, sRequestURI.Length - 1); // Schrägstrich entfernen
            if (!CurrentDevice.DeviceMethods.Where(x => x.ServiceID == "ContentDirectory").ToList()[0].ControlURL.StartsWith("/")) sRequestURI += "/";
            sRequestURI += CurrentDevice.DeviceMethods.Where(x => x.ServiceID == "ContentDirectory").ToList()[0].ControlURL;
            #endregion

            ResponseReceived temp = OnResponseReceivedDetail;
            BrowseAction.OnResponseReceived += temp;
            DetailResponseReceived = false;
            CurrentDetailText = "";
            BrowseAction.Execute(sRequestURI, ContentDirectoryService.ServiceID, ArgList);
            while (!DetailResponseReceived) Task.Delay(10);
            BrowseAction.OnResponseReceived -= temp;
            if (!string.IsNullOrEmpty(CurrentDetailText))
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    DisplayAlert("Details", CurrentDetailText, "OK");
                });
            }
        }

        /// <summary>
        /// Callback-Funktion zur Detailanfragen
        /// </summary>
        /// <param name="ResultXML">XML Antwort</param>
        /// <param name="AnswerState">Status-Objekt der Antwort</param>
        private void OnResponseReceivedDetail(XDocument ResultXML, ActionState AnswerState)
        {
            try
            {
                CurrentDetailText = "";
                XElement ResultElement = ResultXML.Descendants().FirstOrDefault(e => e.Name.LocalName.ToLower() == "result");
                if (ResultElement == null) return;
                List<XElement> ResultElements = XDocument.Parse(ResultElement.Value).Root.Elements().ToList();
                if (ResultElements.Count == 0)
                {
                    CurrentDetailText += "Kein Inhalt";
                }
                else
                {
                    CurrentDetailText += "Inhalt:" + Environment.NewLine;
                    foreach (XElement element in ResultElements)
                    {
                        if (element.Name.LocalName.ToLower() == "container")
                        {
                            CurrentDetailText += "Ordner: " + (element.Elements().FirstOrDefault(e => e.Name.LocalName.ToLower() == "title") == null ? "-" : element.Elements().FirstOrDefault(e => e.Name.LocalName.ToLower() == "title").Value);
                            CurrentDetailText += Environment.NewLine;
                        }
                        else if (element.Name.LocalName.ToLower() == "item")
                        {
                            CurrentDetailText += "Element: " + (element.Elements().FirstOrDefault(e => e.Name.LocalName.ToLower() == "title") == null ? "-" : element.Elements().FirstOrDefault(e => e.Name.LocalName.ToLower() == "title").Value);
                            CurrentDetailText += Environment.NewLine;
                        }
                    }
                }
            }
            finally
            {
                DetailResponseReceived = true;
            }
        }
        
        /// <summary>
        /// Funktion zum Erstellen einer Playlist
        /// </summary>
        /// <param name="playlist">Abgerufene UPnP-PlayList</param>
        /// <returns>PlayList-Objekt mit allen enthaltenen Titeln</returns>
        private PlayList CreatePlayList(PlaylistItem playlist)
        {
            UPnPAction BrowseAction = null;
            try
            {
                BrowseAction =
                    CurrentDevice.DeviceMethods.Where(e => e.ServiceType.ToLower() == "contentdirectory").ToList()[0]
                        .ActionList.Where(x => x.ActionName.ToLower() == "browse").ToList()[0];

            }
            catch (Exception e)
            {
                DisplayAlert("Fehler", e.ToString(), "Vorgang abbrechen");
                return new PlayList(new UPnPContainer(), CurrentDevice);
            }
            CreatePlayListResponseReceived = false;
            CreatedPlayList = null;
            ResponseReceived temp = OnResponseReceivedCreatePlayList;
            BrowseAction.OnResponseReceived += temp;
            List<UPnPActionArgument> InArgs = new List<UPnPActionArgument>();
            foreach (UPnPActionArgument oArg in BrowseAction.ArgumentList)
            {
                if (oArg.Direction == "in")
                {
                    InArgs.Add(oArg);
                }
            }
            UPnPStateVariables.A_ARG_TYPE_BrowseFlag = UPnPBrowseFlag.BrowseDirectChildren;
            UPnPStateVariables.A_ARG_TYPE_Count = "100";
            UPnPStateVariables.A_ARG_TYPE_Index = "0";
            UPnPStateVariables.A_ARG_TYPE_ObjectID = playlist.RelatedContainer.id;
            UPnPStateVariables.A_ARG_TYPE_SortCriteria = ""; //Keine Sortierung
            Type TypeInfo = typeof(UPnPStateVariables);
            List<Tuple<string, string>> ArgList = new List<Tuple<string, string>>();
            foreach (UPnPActionArgument Arg in InArgs)
            {
                PropertyInfo ResultProperty = TypeInfo.GetRuntimeProperty(Arg.RelatedStateVariable);
                if (ResultProperty != null)
                {
                    ArgList.Add(new Tuple<string, string>(Arg.Name, ResultProperty.GetValue(null).ToString()));
                }
                else
                {
                    throw new Exception("Die Funktion konnte nicht ausgeführt werden!");
                }
            }
            string sRequestURI = CurrentDevice.DeviceAddress.Scheme + "://" + CurrentDevice.DeviceAddress.Authority;
            if (sRequestURI.Length == 0)
            {
                throw new Exception("Die Funktion konnte nicht ausgeführt werden!");
            }
            if (sRequestURI.EndsWith("/"))
                sRequestURI = sRequestURI.Substring(0, sRequestURI.Length - 1); // Schrägstrich entfernen
            if (
                !CurrentDevice.DeviceMethods.Where(x => x.ServiceID == "ContentDirectory").ToList()[0].ControlURL
                    .StartsWith("/")) sRequestURI += "/";
            sRequestURI +=
                CurrentDevice.DeviceMethods.Where(x => x.ServiceID == "ContentDirectory").ToList()[0].ControlURL;
            BrowseAction.Execute(sRequestURI, "ContentDirectory", ArgList);

            while (!CreatePlayListResponseReceived)
            {
                Task.Delay(5);
            }
            BrowseAction.OnResponseReceived -= temp;
            return CreatedPlayList;
        }

        /// <summary>
        /// Callback-Funktion zum Erstellen einer PlayList
        /// </summary>
        /// <param name="oResponseDocument">XML Antwort</param>
        /// <param name="oState">Status-Objekt der Antwort</param>
        private void OnResponseReceivedCreatePlayList(XDocument oResponseDocument, ActionState oState)
        {
            try
            {
                if (oResponseDocument != null)
                {
                    List<XElement> oTemp = oResponseDocument.Descendants().Where(e => e.Name.LocalName.ToLower() == "result").ToList();
                    if (oTemp.Count == 1)
                    {

                        UPnPContainer PlayListContainer = new UPnPContainer();
                        CreatedPlayList = new PlayList(PlayListContainer, CurrentDevice);
                        XDocument ResultDocument = XDocument.Parse(oTemp[0].Value);
                        List<XElement> Items = ResultDocument.Descendants().Where(e => e.Name.LocalName.ToLower() == "item").ToList();
                        foreach (XElement item in Items)
                        {
                            UPnPMusicTrack oTrack = new UPnPMusicTrack();
                            oTrack = oTrack.Create(item, oTrack);
                            MusicItem oItem = new MusicItem(oTrack.Title);
                            oItem.RelatedTrack = oTrack;
                            PlayListContainer.MusicTracks.Add(oTrack);
                            CreatedPlayList.MusicTitles.Add(oItem);
                        }

                    }
                    else
                    {

                    }
                }
                //CreatedPlayList = new PlayList();
            }
            finally
            {
                CreatePlayListResponseReceived = true;
            }
        }

        /// <summary>
        /// Wird aufgerufen wenn die Oberfläche verschwindet
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            MessagingCenter.Unsubscribe<ViewCellBase, FileExplorerItemBase>(this, GlobalVariables.BaseShowDetailsActionName);
            // Ordner öffnen
            MessagingCenter.Unsubscribe<FolderViewCell, FolderItem>(this, GlobalVariables.FolderOpenActionName);
            // Musik wiedergeben
            MessagingCenter.Unsubscribe<MusicViewCell, MusicItem>(this, GlobalVariables.MusicPlayActionName);
            // Musik in aktuelle Wiedergabeliste einfügen
            MessagingCenter.Unsubscribe<MusicViewCell, MusicItem>(this, GlobalVariables.MusicAddToPlayLIstActionName);
            // Bild anzeigen
            MessagingCenter.Unsubscribe<ImageViewCell, PictureItem>(this, GlobalVariables.ImageOpenActionName);
            // Video wiedergeben
            MessagingCenter.Unsubscribe<VideoViewCell, VideoItem>(this, GlobalVariables.VideoPlayActionName);
            // Playlist wiedergeben
            MessagingCenter.Unsubscribe<PlayListViewCell, PlaylistItem>(this, GlobalVariables.PlaylistPlayActionName);
            // Playlist zur Wiedergabeliste anfügen
            MessagingCenter.Unsubscribe<PlayListViewCell, PlaylistItem>(this, GlobalVariables.PlaylistAddToPlayLIstActionName);
        }

        /// <summary>
        /// Callback-Funktion
        /// </summary>
        /// <param name="oResponseDocument">XML Antwort</param>
        /// <param name="oState">Status-Objekt der Antwort</param>
        private void OnResponeReceived(XDocument oResponseDocument, ActionState oState)
        {
            BrowseChildrenReceived = true;
            if (oResponseDocument != null)
            {
                XElement ResultElement = oResponseDocument.Root.Descendants().Where(e => e.Name.LocalName.ToLower() == "result").FirstOrDefault();
                if (ResultElement == null) return;
                XDocument ResultXML = XDocument.Parse(ResultElement.Value);

                List<XElement> Nodes = ResultXML.Root.Elements().ToList();
                Device.BeginInvokeOnMainThread(() => MasterItem.Childrens.Clear());
                // In Nodes sind jetzt alle Kinder "Container, Music usw." drin
                foreach (XElement Node in Nodes)
                {
                    string Name = Node.Name.LocalName.ToLower();
                    string Class = Node.Descendants().Where(e => e.Name.LocalName.ToLower() == "class").ToList()[0].Value;
                    if (Class.Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries).Last().ToLower() ==
                        "playlistcontainer")
                    {
                        Name = Class.Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries).Last().ToLower();
                    }
                    if (Name == "container")
                    {   // Zwischen Playlist und Folder unterscheiden
                        string Title = Node.Descendants().Where(e => e.Name.LocalName.ToLower() == "title").ToList()[0].Value;
                        if (Title.EndsWith(".m3u"))
                        {
                            UPnPContainer Container = new UPnPContainer();
                            Container = Container.Create(Node, Container);
                            PlaylistItem Playlist = new PlaylistItem(Container.Title);
                            Playlist.Parent = MasterItem;
                            Playlist.RelatedContainer = Container;
                            Device.BeginInvokeOnMainThread(() => MasterItem.Childrens.Add(Playlist));
                            // Playlist
                        }
                        else
                        {
                            UPnPContainer Container = new UPnPContainer();
                            Container = Container.Create(Node, Container);
                            FolderItem ContainerFolder = new FolderItem(Container.Title);
                            ContainerFolder.Parent = MasterItem;
                            ContainerFolder.RelatedContainer = Container;
                            Device.BeginInvokeOnMainThread(() => MasterItem.Childrens.Add(ContainerFolder));
                        }
                    }
                    else if (Name == "item")
                    {
                        string UPnPClass =
                            Node.Elements().Where(e => e.Name.LocalName.ToLower() == "class").ToList()[0].Value;
                        string[] ClassDecomposed = UPnPClass.Split('.');
                        if (ClassDecomposed[ClassDecomposed.Length - 1].ToLower() == "musictrack")
                        {
                            //UPnPMusicTrack MusicTrack = UPnPMusicTrack.CreateTrack(Node);
                            UPnPMusicTrack MusicTrack = new UPnPMusicTrack();
                            MusicTrack = MusicTrack.Create(Node, MusicTrack);
                            MusicItem MusicItem = new MusicItem(MusicTrack.Title);
                            MusicItem.Parent = MasterItem;
                            MusicItem.RelatedTrack = MusicTrack;
                            Device.BeginInvokeOnMainThread(() => MasterItem.AddChild(MusicItem));
                        }
                        else if (ClassDecomposed[ClassDecomposed.Length - 1].ToLower() == "photo")
                        {
                            UPnPPhoto Photo = new UPnPPhoto();
                            Photo = Photo.Create(Node, Photo);
                            PictureItem PictureItem = new PictureItem(Photo.Title);
                            PictureItem.Parent = MasterItem;
                            PictureItem.RelatedPhoto = Photo;
                            Device.BeginInvokeOnMainThread(() => MasterItem.AddChild(PictureItem));
                        }
                        else if (ClassDecomposed[ClassDecomposed.Length - 1].ToLower() == "videoitem")
                        {
                            UPnPVideoItem Video = new UPnPVideoItem();
                            Video = Video.Create(Node, Video);
                            VideoItem VideoItem1 = new VideoItem(Video.Title);
                            VideoItem1.Parent = MasterItem;
                            VideoItem1.RelatedVideo = Video;
                            Device.BeginInvokeOnMainThread(() => MasterItem.AddChild(VideoItem1));
                        }
                        else
                        {

                        }
                    }
                    else if (Name == "playlistcontainer")
                    {
                        UPnPContainer Container = new UPnPContainer();
                        Container = Container.Create(Node, Container);
                        PlaylistItem Playlist = new PlaylistItem(Container.Title);
                        Playlist.Parent = MasterItem;
                        Playlist.RelatedContainer = Container;
                        Device.BeginInvokeOnMainThread(() => MasterItem.Childrens.Add(Playlist));
                        // Playlist
                    }
                    else
                    {
                        Debug.WriteLine(Name);
                    }
                }

            }
            Device.BeginInvokeOnMainThread(() =>
            {
                CurrentDevice.DeviceMethods.Where(e => e.ServiceType.ToLower() == "contentdirectory").ToList()[0].ActionList.Where(x => x.ActionName.ToLower() == "browse").ToList()[0].OnResponseReceived -= OnResponeReceived;
            });
        }
        
        /// <summary>
        /// Ruft die Kinder eines ausgewählten Elements ab
        /// </summary>
        /// <param name="FolderItem">Eltern-Element</param>
        private void BrowseChildrens(FolderItem FolderItem)
        {
            ResponseReceived temp = new ResponseReceived(OnResponeReceived);
            UPnPAction BrowseAction = CurrentDevice.DeviceMethods.Where(e => e.ServiceType.ToLower() == "contentdirectory").ToList()[0].ActionList.Where(x => x.ActionName.ToLower() == "browse").ToList()[0];
            BrowseAction.OnResponseReceived += temp;
            BrowseChildrenReceived = false;
            List<UPnPActionArgument> InArgs = new List<UPnPActionArgument>();
            foreach (UPnPActionArgument oArg in BrowseAction.ArgumentList)
            {
                if (oArg.Direction == "in")
                {
                    InArgs.Add(oArg);
                }
            }
            UPnPStateVariables.A_ARG_TYPE_BrowseFlag = UPnPBrowseFlag.BrowseDirectChildren;
            UPnPStateVariables.A_ARG_TYPE_Count = "100";
            UPnPStateVariables.A_ARG_TYPE_Index = "0";
            UPnPStateVariables.A_ARG_TYPE_ObjectID = FolderItem.RelatedContainer.id;
            UPnPStateVariables.A_ARG_TYPE_SortCriteria = "+upnp:artist";
            Type TypeInfo = typeof(UPnPStateVariables);
            List<Tuple<string, string>> ArgList = new List<Tuple<string, string>>();
            foreach (UPnPActionArgument Arg in InArgs)
            {
                PropertyInfo ResultProperty = TypeInfo.GetRuntimeProperty(Arg.RelatedStateVariable);
                if (ResultProperty != null)
                {
                    ArgList.Add(new Tuple<string, string>(Arg.Name, ResultProperty.GetValue(null).ToString()));
                }
                else
                {
                    throw new Exception("Die Funktion konnte nicht ausgeführt werden!");
                }
            }
            string sRequestURI = CurrentDevice.DeviceAddress.Scheme + "://" + CurrentDevice.DeviceAddress.Authority;
            if (sRequestURI.Length == 0)
            {
                throw new Exception("Die Funktion konnte nicht ausgeführt werden!");
            }
            if (sRequestURI.EndsWith("/")) sRequestURI = sRequestURI.Substring(0, sRequestURI.Length - 1); // Schrägstrich entfernen
            if (!CurrentDevice.DeviceMethods.Where(x => x.ServiceID == "ContentDirectory").ToList()[0].ControlURL.StartsWith("/")) sRequestURI += "/";
            sRequestURI += CurrentDevice.DeviceMethods.Where(x => x.ServiceID == "ContentDirectory").ToList()[0].ControlURL;
            BrowseAction.Execute(sRequestURI, "ContentDirectory", ArgList);
            while (!BrowseChildrenReceived)
            {
                Task.Delay(5);
            }
            BrowseAction.OnResponseReceived -= temp;
        }

        /// <summary>
        /// Wird aufgerufen wenn ein Element aus der Liste ausgewählt wird
        /// </summary>
        /// <param name="sender">Das aufrufende Objekt</param>
        /// <param name="e">Eventparameter</param>
        private void FileListView_OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item as FileExplorerItemBase == null) return;
            List<string> MediaRenderer = new List<string>();

            MediaRenderer.Add("Dieses Gerät");
            switch ((e.Item as FileExplorerItemBase).ItemType)
            {
                case FileExplorerItemType.MUSIC:
                    foreach (UPnPDevice oTempDevice in GlobalVariables.UPnPMediaRenderer)
                    {
                        if (oTempDevice.Type == "DUMMY") continue;
                        if (oTempDevice.Protocoltypes.Keys.Contains("audio")) MediaRenderer.Add(oTempDevice.DeviceName);
                    }
                    MusicItemTapped(e.Item as MusicItem, MediaRenderer.ToArray());
                    break;
                case FileExplorerItemType.PICTURE:
                    foreach (UPnPDevice oTempDevice in GlobalVariables.UPnPMediaRenderer)
                    {
                        if (oTempDevice.Type == "DUMMY") continue;
                        if (oTempDevice.Protocoltypes.Keys.Contains("image")) MediaRenderer.Add(oTempDevice.DeviceName);
                    }
                    PictureItemTapped(e.Item as PictureItem, MediaRenderer.ToArray());
                    break;
                case FileExplorerItemType.VIDEO:
                    foreach (UPnPDevice oTempDevice in GlobalVariables.UPnPMediaRenderer)
                    {
                        if (oTempDevice.Type == "DUMMY") continue;
                        if (oTempDevice.Protocoltypes.Keys.Contains("video")) MediaRenderer.Add(oTempDevice.DeviceName);
                    }
                    VideoItemTapped(e.Item as VideoItem, MediaRenderer.ToArray());
                    break;
                case FileExplorerItemType.FOLDER:
                    FolderItem Item = e.Item as FolderItem;
                    MasterItem = Item;
                    BrowseChildrens(MasterItem);
                    break;
                case FileExplorerItemType.PLAYLIST:
                    foreach (UPnPDevice oTempDevice in GlobalVariables.UPnPMediaRenderer)
                    {
                        if (oTempDevice.Type == "DUMMY") continue;
                        if (oTempDevice.Protocoltypes.Keys.Contains("audio")) MediaRenderer.Add(oTempDevice.DeviceName);
                    }
                    PlaylistItemTapped(e.Item as PlaylistItem, MediaRenderer.ToArray());
                    break;
                case FileExplorerItemType.ELSE:
                    break;
            }
        }

        /// <summary>
        /// Behandelt die Auswahl eines PlayList-Items
        /// </summary>
        /// <param name="TappedItem">Ausgewählte PlayList</param>
        /// <param name="Options">Abspieloptionen</param>
        private void PlaylistItemTapped(PlaylistItem TappedItem, string[] Options)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                string SelectedRenderer = await DisplayActionSheet("Wiedergabegerät auswählen", "Wiedergabe Abbrechen", null,
                    Options);
                PlayListDeviceSelected(TappedItem, SelectedRenderer);
            });
        }

        /// <summary>
        /// Behandelt die Wiedergabe eines PlayList-Elements
        /// </summary>
        /// <param name="TappedItem">Das ausgewählte PlayList-Element</param>
        /// <param name="SelectedRenderer">Das ausgewählte Ausgabegerät</param>
        private void PlayListDeviceSelected(PlaylistItem TappedItem, string SelectedRenderer)
        {
            if (SelectedRenderer != null && SelectedRenderer == "Wiedergabe Abbrechen")
            {

            }
            else if (string.IsNullOrEmpty(SelectedRenderer)) { }
            else if (SelectedRenderer == "Dieses Gerät")
            {
                DisplayAlert("Hinweis", "Die Wiedergabe von PlayLists ist auf dem Gerät nicht möglich." + Environment.NewLine + "PlayLists können nur auf Netzwerkgeräten wiedergegeben werden.", "OK");
            }
            else
            {
                UPnPAction BrowseAction = null;
                try
                {
                    BrowseAction =
                        CurrentDevice.DeviceMethods.Where(e => e.ServiceType.ToLower() == "contentdirectory").ToList()[0
                        ].ActionList.Where(x => x.ActionName.ToLower() == "browse").ToList()[0];
                }
                catch (Exception gEx)
                {
                    Debug.WriteLine(gEx.ToString());
                }
                if (BrowseAction == null)
                {
                    DisplayAlert("Fehler", "Die Playlist kann nicht geöffnet werden!", "OK");
                    return;
                }
                ResponseReceived temp = new ResponseReceived(OnResponseReceivedPlaylist);
                BrowseAction.OnResponseReceived += temp;
                BrowseActionPlayListReceived = false;
                List<UPnPActionArgument> InArgs = new List<UPnPActionArgument>();
                foreach (UPnPActionArgument oArg in BrowseAction.ArgumentList)
                {
                    if (oArg.Direction == "in")
                    {
                        InArgs.Add(oArg);
                    }
                }
                UPnPStateVariables.A_ARG_TYPE_BrowseFlag = UPnPBrowseFlag.BrowseDirectChildren;
                UPnPStateVariables.A_ARG_TYPE_Count = "100";
                UPnPStateVariables.A_ARG_TYPE_Index = "0";
                UPnPStateVariables.A_ARG_TYPE_ObjectID = TappedItem.RelatedContainer.id;
                UPnPStateVariables.A_ARG_TYPE_SortCriteria = ""; //Keine Sortierung
                Type TypeInfo = typeof(UPnPStateVariables);
                List<Tuple<string, string>> ArgList = new List<Tuple<string, string>>();
                foreach (UPnPActionArgument Arg in InArgs)
                {
                    PropertyInfo ResultProperty = TypeInfo.GetRuntimeProperty(Arg.RelatedStateVariable);
                    if (ResultProperty != null)
                    {
                        ArgList.Add(new Tuple<string, string>(Arg.Name, ResultProperty.GetValue(null).ToString()));
                    }
                    else
                    {
                        throw new Exception("Die Funktion konnte nicht ausgeführt werden!");
                    }
                }
                string sRequestURI = CurrentDevice.DeviceAddress.Scheme + "://" + CurrentDevice.DeviceAddress.Authority;
                if (sRequestURI.Length == 0)
                {
                    throw new Exception("Die Funktion konnte nicht ausgeführt werden!");
                }
                if (sRequestURI.EndsWith("/"))
                    sRequestURI = sRequestURI.Substring(0, sRequestURI.Length - 1); // Schrägstrich entfernen
                if (
                    !CurrentDevice.DeviceMethods.Where(x => x.ServiceID == "ContentDirectory").ToList()[0].ControlURL
                        .StartsWith("/")) sRequestURI += "/";
                sRequestURI +=
                    CurrentDevice.DeviceMethods.Where(x => x.ServiceID == "ContentDirectory").ToList()[0].ControlURL;
                BrowseAction.Execute(sRequestURI, "ContentDirectory", ArgList, SelectedRenderer);
                while (!BrowseActionPlayListReceived)
                {
                    Task.Delay(5);
                }
                BrowseAction.OnResponseReceived -= temp;
            }

        }

        /// <summary>
        /// Callback-Funktion zum erstellen von PlayLists
        /// </summary>
        /// <param name="oResponseDocument">XML Antwort</param>
        /// <param name="oState">Status-Objekt der Antwort</param>
        private void OnResponseReceivedPlaylist(XDocument oResponseDocument, ActionState oState)
        {
            BrowseActionPlayListReceived = true;
            if (oResponseDocument != null)
            {
                PlaylistItem TappedPlayList = FileListView.SelectedItem as PlaylistItem;
                if (TappedPlayList == null) return;
                XDocument ResultXML =
                    XDocument.Parse(
                        oResponseDocument.Root.Descendants().Where(e => e.Name.LocalName.ToLower() == "result").ToList()
                            [0].Value);
                List<XElement> Nodes = ResultXML.Root.Elements().ToList();
                foreach (XElement Node in Nodes)
                {
                    string Name = Node.Name.LocalName.ToLower();
                    if (Name == "item")
                    {
                        string UPnPClass =
                            Node.Elements().Where(e => e.Name.LocalName.ToLower() == "class").ToList()[0].Value;
                        string[] ClassDecomposed = UPnPClass.Split('.');
                        if (ClassDecomposed[ClassDecomposed.Length - 1].ToLower() == "musictrack")
                        {
                            UPnPMusicTrack MusicTrack = new UPnPMusicTrack();
                            MusicTrack = MusicTrack.Create(Node, MusicTrack);
                            MusicItem MusicItem = new MusicItem(MusicTrack.Title);
                            MusicItem.Parent = MasterItem;
                            MusicItem.RelatedTrack = MusicTrack;
                            if (TappedPlayList.MusicItems.Where(e => e.DisplayName == MusicItem.DisplayName).Count() ==
                                0) TappedPlayList.MusicItems.Add(MusicItem);
                        }
                    }
                }
                if (TappedPlayList.MusicItems.Count > 0)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        GlobalVariables.GlobalPlayerControl = new PlayerControl(GlobalVariables.UPnPMediaRenderer.Where(e => e.DeviceName == oState.AdditionalInfo).ToList()[0], new MediaObject() { Index = 0, Path = TappedPlayList.MusicItems[0].RelatedTrack.Res });
                        for (int i = 1; i < TappedPlayList.MusicItems.Count; i++)
                        {
                            GlobalVariables.GlobalPlayerControl.AddMedia(new MediaObject() { Index = i, Path = TappedPlayList.MusicItems[i].RelatedTrack.Res });
                        }
                        OpenRemotePlayerView(TappedPlayList);
                    });
                }
            }
            else
            {

            }
        }

        /// <summary>
        /// Behandelt die Auswahl eines Video-Elements
        /// </summary>
        /// <param name="TappedItem">Ausgewähltes Video Element</param>
        /// <param name="Options">Abspieloptionen</param>
        private void VideoItemTapped(VideoItem TappedItem, string[] Options)
        {
            string SelectedRenderer = "";
            Device.BeginInvokeOnMainThread(async () =>
            {
                SelectedRenderer = await DisplayActionSheet("Wiedergabegerät auswählen", "Wiedergabe Abbrechen", null, Options);
                VideoDeviceSelected(SelectedRenderer, TappedItem);
            });
        }

        /// <summary>
        /// Behandelt die Auswahl eines Bild-Elements
        /// </summary>
        /// <param name="TappedItem">Ausgewähltes Bild-Element</param>
        /// <param name="Options">Abspieloptionen</param>
        private void PictureItemTapped(PictureItem TappedItem, string[] Options)
        {
            string SelectedRenderer = "";
            Device.BeginInvokeOnMainThread(async () =>
            {
                SelectedRenderer = await DisplayActionSheet("Wiedergabegerät auswählen", "Wiedergabe Abbrechen", null, Options);
                PictureDeviceSelected(SelectedRenderer, TappedItem);
            });
        }

        /// <summary>
        /// Behandelt die Auswahl eines Musik-Element
        /// </summary>
        /// <param name="TappedItem">Ausgewähltes Musik-Element</param>
        /// <param name="Options">Abspieloptionen</param>
        private void MusicItemTapped(MusicItem TappedItem, string[] Options)
        {
            //Popup anzeigen
            string SelectedRenderer = "";
            Device.BeginInvokeOnMainThread(async () =>
            {
                Task<string> TaskRenderer = DisplayActionSheet("Wiedergabegerät auswählen", "Wiedergabe Abbrechen", null, Options);
                SelectedRenderer = await TaskRenderer;
                PlayDeviceSelected(SelectedRenderer, TappedItem);
            });
        }

        /// <summary>
        /// Behandelt die Auswahl eines Wiedergabegeräts
        /// </summary>
        /// <param name="SelectedRenderer">Ausgewähltes Wiedergabegerät</param>
        /// <param name="MusicItem">Wiederzugebender Musiktitel</param>
        private void PlayDeviceSelected(string SelectedRenderer, MusicItem MusicItem)
        {
            if (SelectedRenderer != null && SelectedRenderer == "Wiedergabe Abbrechen")
            {   // Keine Wiedergabe starten

            }
            else if (SelectedRenderer != null)
            {   // Wiedergabe starten
                #region Play Dieses Gerät
                if (SelectedRenderer == "Dieses Gerät")
                {   // Auf diesem Gerät wiedergeben
                    if ((GlobalVariables.GlobalMediaPlayerDevice as IMediaPlayer).PlayFromUri(new Uri(MusicItem.RelatedTrack.Res)))
                    {
                        (GlobalVariables.GlobalMediaPlayerDevice as IMediaPlayer).SetName(MusicItem.RelatedTrack.Title);
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            PlayerStackLayout.Children.Clear();
                            PlayerStackLayout.Children.Add(GlobalVariables.GlobalMediaPlayerDevice);
                            PlayerStackLayout.ForceLayout();
                            GlobalVariables.GlobalMediaPlayerControl.Play();
                        });
                    }
                }
                #endregion
                else
                {   // Remote-Gerät
                    List<UPnPDevice> SelectedRendererList = GlobalVariables.UPnPMediaRenderer.Where(Renderer => Renderer.DeviceName == SelectedRenderer).ToList();
                    if (SelectedRendererList.Count == 0)
                    {   // Keinen Renderer gefunden
                        DisplayAlert("Warnung", "Die Wiedergabe konnte nicht gestartet werden." + Environment.NewLine + "Das Ausgabegerät konnte nicht gefunden werden!", "OK");
                        return;
                    }
                    else
                    {
                        MediaObject Song = new MediaObject()
                        {
                            Index = 0,
                            MetaData = "",
                            Path = MusicItem.RelatedTrack.Res
                        };
                        GlobalVariables.GlobalPlayerControl = MediaPlayer.Play(Song, SelectedRendererList[0]);
                        if (GlobalVariables.GlobalPlayerControl.ConnectionError) { DisplayAlert("Fehler", "Wiedergabe konnte aufgrund eines Gerätefehlers nicht gestartet werden.", "OK"); return; }
                        if (GlobalVariables.GlobalRemoteMediaPlayerPage.PlayList == null)
                        {
                            GlobalVariables.GlobalRemoteMediaPlayerPage.AddMusicTrackToPlayList(MusicItem, true);
                        }
                        else
                        {
                            GlobalVariables.GlobalRemoteMediaPlayerPage.PlayList.MusicItems.Clear();
                            GlobalVariables.GlobalRemoteMediaPlayerPage.PlayList.MusicItems.Add(MusicItem);
                        }
                        GlobalVariables.GlobalRemoteMediaPlayerPage.PlayList.MusicItems[0].IsPlaying = true;
                        GlobalVariables.GlobalRemoteMediaPlayerPage.CurrentMusicTrack = GlobalVariables.GlobalRemoteMediaPlayerPage.PlayList.MusicItems[0].RelatedTrack;
                        OpenRemotePlayerView();
                    }
                }
            }
            else
            {   // SelectedRenderer ist null!

            }
        }

        /// <summary>
        /// Behandelt die Wiedergabe eines Bild-Elements
        /// </summary>
        /// <param name="SelectedRenderer">Ausgewähltes Wiedergabegerät</param>
        /// <param name="PictureItem">Wiederzugebendes Bild</param>
        private async void PictureDeviceSelected(string SelectedRenderer, PictureItem PictureItem)
        {
            if (SelectedRenderer == null || SelectedRenderer == "Wiedergabe Abbrechen") return;
            if (SelectedRenderer == "Dieses Gerät")
            {
                try
                {
                    IPhotoViewer PhotoViewer = DependencyService.Get<IPhotoViewer>();
                    PhotoViewer.ShowPhotoFromUri(new Uri(PictureItem.RelatedPhoto.Res));
                    ContentPage PhotoViewerPage = PhotoViewer as ContentPage;
                    await Navigation.PushAsync(PhotoViewerPage);

                }
                catch (Exception gEx)
                {
                    throw gEx;
                }
            }
        }

        /// <summary>
        /// Öffnet die RemotePlayer-Steuerungsansicht
        /// </summary>
        /// <param name="PlayList">Playlist zur Wiedergabe</param>
        private void OpenRemotePlayerView(PlaylistItem PlayList)
        {
            PlayList.MusicItems[0].IsPlaying = true;
            GlobalVariables.GlobalRemoteMediaPlayerPage.PlayList = PlayList;
            GlobalVariables.GlobalRemoteMediaPlayerPage.CurrentMusicTrack = PlayList.MusicItems[0].RelatedTrack;
            while (!GlobalVariables.GlobalPlayerControl.IsPlaying)
            {
                Task.Delay(10);
            }
            GlobalVariables.GlobalRemoteMediaPlayerPage.OnPlayingstatuschanged();
            (Parent.Parent as MasterDetailPageHomeMediaApp).Detail = GlobalVariables.GlobalRemoteMediaPlayerPage;
        }

        /// <summary>
        /// Öffnet die RemotPlayer-Steuerungsansicht
        /// </summary>
        private void OpenRemotePlayerView()
        {
            while (!GlobalVariables.GlobalPlayerControl.IsPlaying)
            {
                Task.Delay(10);
            }
            GlobalVariables.GlobalRemoteMediaPlayerPage.OnPlayingstatuschanged();
            (Parent.Parent as MasterDetailPageHomeMediaApp).Detail = GlobalVariables.GlobalRemoteMediaPlayerPage;
        }

        /// <summary>
        /// Behandelt die Wiedergabe eines Videos
        /// </summary>
        /// <param name="SelectedRenderer">Ausgewähtles Ausgabegerät</param>
        /// <param name="VideoItem">Wiederzugebendes Video</param>
        private void VideoDeviceSelected(string SelectedRenderer, VideoItem VideoItem)
        {
            if (SelectedRenderer == null || SelectedRenderer == "Wiedergabe Abbrechen") return;
            if (SelectedRenderer == "Dieses Gerät")
            {
                if (Device.OS == TargetPlatform.Android)
                {   // Auf Android haben wir eine extra Video-Player Seite
                    ContentPage oPage = DependencyService.Get<IVideoViewer>() as ContentPage;
                    (oPage as IVideoViewer).ShowVideoFromUri(new Uri(VideoItem.RelatedVideo.Res));
                    Navigation.PushAsync(oPage);
                }
                else
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        PlayerStackLayout.Children.Clear();
                        PlayerStackLayout.Children.Add(GlobalVariables.GlobalVideoViewerDevice);
                        PlayerStackLayout.ForceLayout();
                    });
                    GlobalVariables.GlobalVideoViewer.ShowVideoFromUri(new Uri(VideoItem.RelatedVideo.Res));
                    GlobalVariables.GlobalVideoViewer.Play();
                }
            }
            else
            {
                List<UPnPDevice> SelectedRendererList = GlobalVariables.UPnPMediaRenderer.Where(Renderer => Renderer.DeviceName == SelectedRenderer).ToList();
                if (SelectedRendererList.Count == 0)
                {   // Keinen Renderer gefunden
                    DisplayAlert("Warnung", "Die Wiedergabe konnte nicht gestartet werden." + Environment.NewLine + "Das Ausgabegerät konnte nicht gefunden werden!", "OK");
                    return;
                }
                else
                {
                    MediaObject Video = new MediaObject()
                    {
                        Index = 0,
                        MetaData = VideoItem.RelatedVideo.Res,
                        Path = VideoItem.RelatedVideo.Res
                    };
                    GlobalVariables.GlobalPlayerControl = MediaPlayer.Play(Video, SelectedRendererList[0]);
                    OpenRemotePlayerView();
                }
            }
        }

        /// <summary>
        /// Wird ausgelöst wenn der "Ordner-Hoch"-Button ausgelöst wird
        /// </summary>
        /// <param name="sender">Aufrufendes Objekt</param>
        /// <param name="e">Eventparameter</param>
        private void BackButton_OnClicked(object sender, EventArgs e)
        {
            if (MasterItem.Parent != null)
            {
                MasterItem = MasterItem.Parent;
            }
        }

        /// <summary>
        /// Wird ausgelöst wenn die "Zurück"-Hardwaretaste des jeweiligen Systems ausgelöst wird
        /// </summary>
        /// <returns>Behandelt</returns>
        protected override bool OnBackButtonPressed()
        {
            BackButton_OnClicked(this, null);
            return true;
        }
    }
}
