using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace HomeMediaApp.Classes
{
    public enum FileExplorerItemType
    {
        FOLDER,
        PICTURE,
        VIDEO,
        MUSIC,
        ELSE
    };

    public class FileExplorerItemBase
    {
        public FolderItem Parent { get; set; } = null;
        public ImageSource IconSource { get; set; } = new FileImageSource();
        public virtual string DisplayName { get; set; } = "";
        public FileExplorerItemType ItemType { get; set; } = FileExplorerItemType.ELSE;
    }

    public class FolderItem : FileExplorerItemBase
    {
        /// <summary>
        /// Parst die Response XML in ein Folder Object
        /// </summary>
        /// <param name="Response"></param>
        /// <returns></returns>
        public static FolderItem CreateFolderItemFromXML(XDocument Response)
        {
            List<XElement> ContainerElements = Response.Descendants().Where(e => e.Name.LocalName == "container").ToList();
            return new FolderItem("");
        }

        private UPnPContainer mRelatedContainer;
        public UPnPContainer RelatedContainer
        {
            get { return mRelatedContainer; }
            set
            {
                mRelatedContainer = value;
                DisplayName = mRelatedContainer.Title;
            }
        }

        public ObservableCollection<FileExplorerItemBase> Childrens = new ObservableCollection<FileExplorerItemBase>();
        public FolderItem(string FolderName)
        {
            ItemType = FileExplorerItemType.FOLDER;
            DisplayName = FolderName;
            IconSource = ImageSource.FromResource("HomeMediaApp.Icons.folder_icon.png");
        }

        public void AddChild(FileExplorerItemBase Child)
        {
            Childrens.Add(Child);
        }
    }

    public class MusicItem : FileExplorerItemBase
    {
        public MusicItem(string MusicName)
        {
            ItemType = FileExplorerItemType.MUSIC;
            DisplayName = MusicName;
            IconSource = ImageSource.FromResource("HomeMediaApp.Icons.music_icon.png");
        }

        private UPnPMusicTrack mRelatedTrack { get; set; }
        public UPnPMusicTrack RelatedTrack
        {
            get { return mRelatedTrack; }
            set
            {
                if (mRelatedTrack == value) return;
                mRelatedTrack = value;
                DisplayName = mRelatedTrack.Title + Environment.NewLine + "Album: " + mRelatedTrack.Album +
                           Environment.NewLine + "Interpret: " + mRelatedTrack.Artist;
                UpdateImageUri();
            }
        }

        private void UpdateImageUri()
        {
            if (mRelatedTrack.AlbumArtURI != null && mRelatedTrack.AlbumArtURI.Length > 0)
            {
                IconSource = ImageSource.FromUri(new Uri(mRelatedTrack.AlbumArtURI));
            }
        }

        public override string DisplayName { get; set; }
    }

    public class PictureItem : FileExplorerItemBase
    {
        private UPnPPhoto mRelatedPhoto { get; set; }
        public UPnPPhoto RelatedPhoto
        {
            get { return mRelatedPhoto; }
            set
            {
                if (mRelatedPhoto== value) return;
                mRelatedPhoto = value;
                DisplayName = mRelatedPhoto.Title + Environment.NewLine + "Album: " + mRelatedPhoto.Album;
                UpdateImageURI();
            }
        }
        public PictureItem(string PictureName)
        {
            ItemType = FileExplorerItemType.PICTURE;
            DisplayName = PictureName;
            IconSource = ImageSource.FromResource("HomeMediaApp.Icons.picture_icon.png");
        }

        private void UpdateImageURI()
        {
            if (mRelatedPhoto.AlbumArtURI != null && mRelatedPhoto.AlbumArtURI.Length > 0)
            {
                IconSource = ImageSource.FromUri(new Uri(mRelatedPhoto.AlbumArtURI));
            }
        }
    }

    public class VideoItem : FileExplorerItemBase
    {
        public VideoItem(string VideoName)
        {
            ItemType = FileExplorerItemType.VIDEO;
            DisplayName = VideoName;
            IconSource = ImageSource.FromResource("HomeMediaApp.Icons.video_icon.png");
        }
    }

    public class ElseItem : FileExplorerItemBase
    {
        public ElseItem(string ElseName)
        {
            ItemType = FileExplorerItemType.ELSE;
            DisplayName = ElseName;
            IconSource = ImageSource.FromResource("HomeMediaApp.Icons.else_icon.png");
        }
    }
}
