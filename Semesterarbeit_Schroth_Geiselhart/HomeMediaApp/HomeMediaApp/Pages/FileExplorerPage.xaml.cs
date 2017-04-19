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
            ContentView PlayerControlPage = GlobalVariables.GlobalMediaPlayerDevice;
            PlayerStackLayout.Children.Clear();
            PlayerStackLayout.Children.Add(PlayerControlPage);
            PlayerStackLayout.ForceLayout();
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
                {
                    UPnPContainer Container = new UPnPContainer();
                    Container = Container.Create(Node, Container);
                    FolderItem ContainerFolder = new FolderItem(Container.Title);
                    ContainerFolder.Parent = MasterItem;
                    ContainerFolder.RelatedContainer = Container;
                    Device.BeginInvokeOnMainThread(() => MasterItem.Childrens.Add(ContainerFolder));
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
            string sRequestURI = CurrentDevice.Config.Root.Descendants().Where(Node => Node.Name.LocalName.ToLower() == "urlbase").ToList()
                        [0].Value;
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
            {
                MediaRenderer.Add(upnPMediaServer.DeviceName);
            }
            string SelectedRenderer = null;
            MediaRenderer.Add("Dieses Gerät");
            switch ((e.Item as FileExplorerItemBase).ItemType)
            {
                case FileExplorerItemType.MUSIC:
                    MusicItem MusicItem = e.Item as MusicItem;
                    //Popup anzeigen
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        SelectedRenderer = await DisplayActionSheet("Wiedergabegerät auswählen", "Wiedergabe Abbrechen", null, MediaRenderer.ToArray());
                        PlayDeviceSelected(SelectedRenderer, MusicItem);
                    });
                    break;
                case FileExplorerItemType.PICTURE:
                    PictureItem PictureItem = e.Item as PictureItem;
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        SelectedRenderer = await DisplayActionSheet("Wiedergabegerät auswählen", "Wiedergabe Abbrechen", null, MediaRenderer.ToArray());
                        PictureDeviceSelected(SelectedRenderer, PictureItem);
                    });
                    break;
                case FileExplorerItemType.VIDEO:
                    VideoItem VideoItem = e.Item as VideoItem;
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        SelectedRenderer = await DisplayActionSheet("Wiedergabegerät auswählen", "Wiedergabe Abbrechen", null, MediaRenderer.ToArray());
                        VideoDeviceSelected(SelectedRenderer, VideoItem);
                    });
                    break;
                case FileExplorerItemType.FOLDER:
                    FolderItem Item = e.Item as FolderItem;
                    MasterItem = Item;
                    BrowseChildrens(MasterItem);
                    break;
                case FileExplorerItemType.ELSE:

                    break;
            }
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
                    }
                }
            }
            else
            {   // SelectedRenderer ist null!

            }
        }

        private async void PictureDeviceSelected(string SelectedRenderer, PictureItem PictureItem)
        {
            if (SelectedRenderer == null) return;
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

        private void VideoDeviceSelected(string SelectedRenderer, VideoItem VideoItem)
        {
            throw new NotImplementedException();
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
