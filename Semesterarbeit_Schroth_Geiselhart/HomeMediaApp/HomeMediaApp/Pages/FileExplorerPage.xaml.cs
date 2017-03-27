using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
                    UPnPContainer Container = UPnPContainer.GenerateContainer(Node);
                    FolderItem ContainerFolder = new FolderItem(Container.Title);
                    ContainerFolder.Parent = MasterItem;
                    ContainerFolder.RelatedContainer = Container;
                    Device.BeginInvokeOnMainThread(() => MasterItem.Childrens.Add(ContainerFolder));
                }
                else if(Name == "item")
                {
                    string UPnPClass = Node.Elements().Where(e => e.Name.LocalName.ToLower() == "class").ToList()[0].Value;
                    string[] ClassDecomposed = UPnPClass.Split('.');
                    if (ClassDecomposed[ClassDecomposed.Length - 1].ToLower() == "musictrack")
                    {
                        UPnPMusicTrack MusicTrack = UPnPMusicTrack.CreateTrack(Node);
                        MusicItem MusicItem = new MusicItem(MusicTrack.Title);
                        MusicItem.Parent = MasterItem;
                        MusicItem.RelatedTrack = MusicTrack;
                        Device.BeginInvokeOnMainThread(() => MasterItem.AddChild(MusicItem));
                    }
                    else if (ClassDecomposed[ClassDecomposed.Length - 1].ToLower() == "")
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
            UPnPAction BrowseAction =  CurrentDevice.DeviceMethods.Where(e => e.ServiceType.ToLower() == "contentdirectory").ToList()[0].ActionList.Where(x => x.ActionName.ToLower() == "browse").ToList()[0];
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
            switch ((e.Item as FileExplorerItemBase).ItemType)
            {
                case FileExplorerItemType.MUSIC:
                    MusicItem MusicItem = e.Item as MusicItem;
                    List<string> MediaRenderer = new List<string>();
                    foreach (UPnPDevice upnPMediaServer in GlobalVariables.UPnPMediaRenderer)
                    {
                        MediaRenderer.Add(upnPMediaServer.DeviceName);
                    }
                    //Popup anzeigen
                    Task<string> SelectedMediaRenderer = DisplayActionSheet("Wiedergabe auf?", "Abbrechen", null, MediaRenderer.ToArray());
                    SelectedMediaRenderer.Wait();
                    string Result = SelectedMediaRenderer.Result;
                    break;
                case FileExplorerItemType.PICTURE:

                    break;
                case FileExplorerItemType.VIDEO:

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

        private void BackButton_OnClicked(object sender, EventArgs e)
        {
            if (MasterItem.Parent != null)
            {
                MasterItem = MasterItem.Parent;
            }
        }
    }
}
