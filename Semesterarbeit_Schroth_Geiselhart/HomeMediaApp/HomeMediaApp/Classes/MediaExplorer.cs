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
        PLAYLIST,
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

        public bool IsPlaying = false;

        public Color TextColor
        {
            get
            {
                if (IsPlaying) return Color.Red;
                return Color.Black;
            }
        }
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
        private UPnPVideoItem mRelatedVideo { get; set; }

        public UPnPVideoItem RelatedVideo
        {
            get { return mRelatedVideo;}
            set { mRelatedVideo = value; }
        }
        public VideoItem(string VideoName)
        {
            ItemType = FileExplorerItemType.VIDEO;
            DisplayName = VideoName;
            IconSource = ImageSource.FromResource("HomeMediaApp.Icons.video_icon.png");
        }
    }

    public class PlaylistItem : FileExplorerItemBase
    {
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

        public ObservableCollection<MusicItem> MusicItems = new ObservableCollection<MusicItem>();

        public PlaylistItem(string PlaylistName)
        {
            ItemType = FileExplorerItemType.PLAYLIST;
            DisplayName = PlaylistName;
            IconSource = ImageSource.FromResource("HomeMediaApp.Icons.playlist_icon.png");
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

    public class PlayList
    {
        public PlayList(UPnPContainer PlayListContainer, UPnPDevice SourceDevice)
        {
            
        }
        public List<MusicItem> MusicTitles = new List<MusicItem>();
        private string Name = "";
    }
}
