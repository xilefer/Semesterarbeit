﻿using System;
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

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FileExplorerPage : ContentPage
    {

        private FolderItem mMasterItem = new FolderItem("Master");
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

        public string CurrentDirectory { get { return MasterItem.DisplayName; } }

        private ObservableCollection<FileExplorerItemBase> mExplorerItems = new ObservableCollection<FileExplorerItemBase>();
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

        public FileExplorerPage()
        {
            InitializeComponent();
            BackButtonImage.Source = ImageSource.FromResource("HomeMediaApp.Icons.folder_up_icon.png");
            BindingContext = this;
            GlobalVariables.GlobalMediaPlayerDevice = DependencyService.Get<IMediaPlayerControl>() as ContentView;
            GlobalVariables.GlobalVideoViewerDevice = DependencyService.Get<IVideoViewer>() as ContentView; ;
            PlayerStackLayout.Children.Clear();
            PlayerStackLayout.Children.Add(GlobalVariables.GlobalMediaPlayerDevice);
            PlayerStackLayout.ForceLayout();
            // Subscription für die View-Events die für die Kontextaktionen der ListView-Elemente benötigt werden
            // Detailfunktion
            MessagingCenter.Subscribe<ViewCellBase, FileExplorerItemBase>(this, GlobalVariables.BaseShowDetailsActionName, (sender, arg) =>
            {
                throw new NotImplementedException("Detail-Funktion gibts noch nicht");
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
                        MediaRenderer.Add(upnPMediaServer.DeviceName);
                    }
                    MediaRenderer.Add("Dieses Gerät");
                    MusicItemTapped(arg, MediaRenderer.ToArray());
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
        }

        public void OnResponeReceived(XDocument oResponseDocument, ActionState oState)
        {
            XDocument ResultXML = XDocument.Parse(oResponseDocument.Root.Descendants().Where(e => e.Name.LocalName.ToLower() == "result").ToList()[0].Value);
            List<XElement> Nodes = ResultXML.Root.Elements().ToList();
            Device.BeginInvokeOnMainThread(() => MasterItem.Childrens.Clear());
            // In Nodes sind jetzt alle Kinder "Container, Music usw." drin
            foreach (XElement Node in Nodes)
            {
                string Name = Node.Name.LocalName.ToLower();
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
                    string UPnPClass = Node.Elements().Where(e => e.Name.LocalName.ToLower() == "class").ToList()[0].Value;
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
            }
            Device.BeginInvokeOnMainThread(() =>
            {
                CurrentDevice.DeviceMethods.Where(e => e.ServiceType.ToLower() == "contentdirectory").ToList()[0].ActionList.Where(x => x.ActionName.ToLower() == "browse").ToList()[0].OnResponseReceived -= OnResponeReceived;
            });
        }

        public void BrowseChildrens(FolderItem FolderItem)
        {
            UPnPAction BrowseAction = CurrentDevice.DeviceMethods.Where(e => e.ServiceType.ToLower() == "contentdirectory").ToList()[0].ActionList.Where(x => x.ActionName.ToLower() == "browse").ToList()[0];
            BrowseAction.OnResponseReceived += new ResponseReceived(OnResponeReceived);
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
        }

        private void FileListView_OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item as FileExplorerItemBase == null) return;
            List<string> MediaRenderer = new List<string>();
            foreach (UPnPDevice upnPMediaServer in GlobalVariables.UPnPMediaRenderer)
            {   // TODO: Hier muss noch auf die Möglichkeiten des Renderers geprüft werden bsp. Kann er auch videos wiedergeben?
                if (upnPMediaServer.Type == "DUMMY") continue;
                MediaRenderer.Add(upnPMediaServer.DeviceName);
            }
            MediaRenderer.Add("Dieses Gerät");
            switch ((e.Item as FileExplorerItemBase).ItemType)
            {
                case FileExplorerItemType.MUSIC:
                    MusicItemTapped(e.Item as MusicItem, MediaRenderer.ToArray());
                    break;
                case FileExplorerItemType.PICTURE:
                    PictureItemTapped(e.Item as PictureItem, MediaRenderer.ToArray());
                    break;
                case FileExplorerItemType.VIDEO:
                    VideoItemTapped(e.Item as VideoItem, MediaRenderer.ToArray());
                    break;
                case FileExplorerItemType.FOLDER:
                    FolderItem Item = e.Item as FolderItem;
                    MasterItem = Item;
                    BrowseChildrens(MasterItem);
                    break;
                case FileExplorerItemType.PLAYLIST:
                    PlaylistItemTapped(e.Item as PlaylistItem, MediaRenderer.ToArray());
                    break;
                case FileExplorerItemType.ELSE:
                    break;
            }
        }

        private void PlaylistItemTapped(PlaylistItem TappedItem, string[] Options)
        {
            string SelectedRenderer = "";
            Device.BeginInvokeOnMainThread(async () =>
            {
                SelectedRenderer = await DisplayActionSheet("Wiedergabegerät auswählen", "Wiedergabe Abbrechen", null, Options);
            });
            if (SelectedRenderer != null && SelectedRenderer == "Wiedergabe Abbrechen")
            {

            }
            else if (SelectedRenderer == "Dieses Gerät")
            {
                // TappedItem ist eine Playlist d.h. Die Kinder von TappedItem browsen und Wiedergeben
                /*
                if ((GlobalVariables.GlobalMediaPlayerDevice as IMediaPlayerControl).PlayFromUri(new Uri(MusicItem.RelatedTrack.Res)))
                {
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        PlayerStackLayout.Children.Clear();
                        PlayerStackLayout.Children.Add(GlobalVariables.GlobalMediaPlayerDevice);
                        PlayerStackLayout.ForceLayout();
                        bool Answer = await DisplayAlert("Wiedergabe", "Möchten sie die Wiedergabe sofort starten?", "Ja", "Nein");
                        if (Answer)
                        {
                            //try
                            //{
                            //    (Parent.Parent as MasterDetailPageHomeMediaApp).Detail = new NavigationPage(GlobalVariables.GlobalMediaPlayerDevice as Page);
                            //}
                            //catch (Exception e)
                            //{
                            //    Debug.WriteLine(e);
                            //    throw;
                            //}
                            GlobalVariables.GlobalMediaPlayerControl.Play();
                        }
                    });
                }*/
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
                    /*
                    MediaObject Song = new MediaObject()
                    {
                        Index = 0,
                        // TODO: Metadaten definieren
                        MetaData = MusicItem.RelatedTrack.Res,
                        Path = MusicItem.RelatedTrack.Res
                    };
                    GlobalVariables.GlobalPlayerControl = MediaPlayer.Play(Song, SelectedRendererList[0]);
                    */
                }
            }
            //throw new NotImplementedException("Playlist item Tapped");
        }

        private void VideoItemTapped(VideoItem TappedItem, string[] Options)
        {
            string SelectedRenderer = "";
            Device.BeginInvokeOnMainThread(async () =>
            {
                SelectedRenderer = await DisplayActionSheet("Wiedergabegerät auswählen", "Wiedergabe Abbrechen", null, Options);
                VideoDeviceSelected(SelectedRenderer, TappedItem);
            });
        }

        private void PictureItemTapped(PictureItem TappedItem, string[] Options)
        {
            string SelectedRenderer = "";
            Device.BeginInvokeOnMainThread(async () =>
            {
                SelectedRenderer = await DisplayActionSheet("Wiedergabegerät auswählen", "Wiedergabe Abbrechen", null, Options);
                PictureDeviceSelected(SelectedRenderer, TappedItem);
            });
        }

        private void MusicItemTapped(MusicItem TappedItem, string[] Options)
        {
            //Popup anzeigen
            string SelectedRenderer = "";
            Device.BeginInvokeOnMainThread(async () =>
            {
                SelectedRenderer = await DisplayActionSheet("Wiedergabegerät auswählen", "Wiedergabe Abbrechen", null, Options);
                PlayDeviceSelected(SelectedRenderer, TappedItem);
            });
        }

        private void PlayDeviceSelected(string SelectedRenderer, MusicItem MusicItem)
        {
            if (SelectedRenderer != null && SelectedRenderer == "Wiedergabe Abbrechen")
            {   // Keine Wiedergabe starten

            }
            else if (SelectedRenderer != null)
            {   // Wiedergabe starten
                if (SelectedRenderer == "Dieses Gerät")
                {   // Auf diesem Gerät wiedergeben
                    if ((GlobalVariables.GlobalMediaPlayerDevice as IMediaPlayerControl).PlayFromUri(new Uri(MusicItem.RelatedTrack.Res)))
                    {
                        Device.BeginInvokeOnMainThread(async () =>
                        {
                            PlayerStackLayout.Children.Clear();
                            PlayerStackLayout.Children.Add(GlobalVariables.GlobalMediaPlayerDevice);
                            PlayerStackLayout.ForceLayout();
                            bool Answer = await DisplayAlert("Wiedergabe", "Möchten sie die Wiedergabe sofort starten?", "Ja", "Nein");
                            if (Answer)
                            {
                                //try
                                //{
                                //    (Parent.Parent as MasterDetailPageHomeMediaApp).Detail = new NavigationPage(GlobalVariables.GlobalMediaPlayerDevice as Page);
                                //}
                                //catch (Exception e)
                                //{
                                //    Debug.WriteLine(e);
                                //    throw;
                                //}
                                GlobalVariables.GlobalMediaPlayerControl.Play();
                            }
                        });
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
                        MediaObject Song = new MediaObject()
                        {
                            Index = 0,
                            // TODO: Metadaten definieren
                            MetaData = MusicItem.RelatedTrack.Res,
                            Path = MusicItem.RelatedTrack.Res
                        };
                        GlobalVariables.GlobalPlayerControl = MediaPlayer.Play(Song, SelectedRendererList[0]);
                        OpenRemotePlayerView();
                    }
                }
            }
            else
            {   // SelectedRenderer ist null!

            }
        }

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
            else
            {
                throw new NotImplementedException();
            }
        }

        public void OpenRemotePlayerView()
        {
            Navigation.PushAsync(new NavigationPage(new RemoteMediaPlayerPage()));
        }

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
                        // TODO: Metadaten definieren
                        MetaData = VideoItem.RelatedVideo.Res,
                        Path = VideoItem.RelatedVideo.Res
                    };
                    GlobalVariables.GlobalPlayerControl = MediaPlayer.Play(Video, SelectedRendererList[0]);
                    OpenRemotePlayerView();
                }
            }
        }

        private void BackButton_OnClicked(object sender, EventArgs e)
        {
            if (MasterItem.Parent != null)
            {
                MasterItem = MasterItem.Parent;
            }
        }

        protected override bool OnBackButtonPressed()
        {
            BackButton_OnClicked(this, null);
            return true;
        }
    }
}
