using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
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
            }
        }

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

        public FileExplorerPage()
        {
            InitializeComponent();
            FileImageSourceConverter FileConverter = new FileImageSourceConverter();
            BackButtonImage.Source = ImageSource.FromResource("HomeMediaApp.Icons.folder_up_icon.png");
            BindingContext = this;
            FolderItem MasterFolder = new FolderItem("MasterFolder");
            FolderItem TempFolder = new FolderItem("Ordner")
            {
                Parent = MasterFolder
            };
            TempFolder.AddChild(new MusicItem("Titel 2") { Parent = TempFolder });
            TempFolder.AddChild(new VideoItem("Video 2") { Parent = TempFolder });
            MasterFolder.AddChild(TempFolder);
            MasterFolder.AddChild(new MusicItem("Titel 1") {Parent = MasterFolder });
            MasterFolder.AddChild(new PictureItem("Bild 1") {Parent = MasterFolder });
            MasterFolder.AddChild(new VideoItem("Video 1") {Parent = MasterFolder });
            MasterFolder.AddChild(new ElseItem("Nicht unterstützt") {Parent = MasterFolder });
            MasterItem = MasterFolder;
        }

        private void FileListView_OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            switch ((e.Item as FileExplorerItemBase).ItemType)
            {
                case FileExplorerItemType.MUSIC:

                    break;
                case FileExplorerItemType.PICTURE:

                    break;
                case FileExplorerItemType.VIDEO:

                    break;
                case FileExplorerItemType.FOLDER:
                    FolderItem Item = e.Item as FolderItem;
                    MasterItem = Item;
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
