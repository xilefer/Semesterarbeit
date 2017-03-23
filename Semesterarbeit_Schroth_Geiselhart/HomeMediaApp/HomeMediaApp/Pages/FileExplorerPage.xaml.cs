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
        private ObservableCollection<FileExplorerItemBase> mExplorerItems = new ObservableCollection<FileExplorerItemBase>();
        public ObservableCollection<FileExplorerItemBase> ExplorerItems
        {
            get { return mExplorerItems; }
            set
            {
                if (mExplorerItems == value) return;
                mExplorerItems = value;
                OnPropertyChanged();
            }
        }

        public FileExplorerPage()
        {
            InitializeComponent();
            BindingContext = this;
            ExplorerItems.Add(new FolderItem("Ordner"));
            ExplorerItems.Add(new MusicItem("Titel 1"));
            ExplorerItems.Add(new PictureItem("Bild 1"));
            ExplorerItems.Add(new VideoItem("Video 1"));
            ExplorerItems.Add(new ElseItem("Nicht unterstützt"));
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

                    break;
                case FileExplorerItemType.ELSE:

                    break;
            }
        }
    }
}
