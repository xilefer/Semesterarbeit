using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

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
        public ImageSource IconSource { get; set; } = new FileImageSource();
        public string DisplayName { get; set; } = "";
        public FileExplorerItemType ItemType { get; set; } = FileExplorerItemType.ELSE;
    }

    public class FolderItem : FileExplorerItemBase
    {
        public FolderItem(string FolderName)
        {
            base.ItemType = FileExplorerItemType.FOLDER;
            base.DisplayName = FolderName;
            base.IconSource = ImageSource.FromResource("HomeMediaApp.Icons.folder_icon.png");
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
    }

    public class PictureItem : FileExplorerItemBase
    {
        public PictureItem(string PictureName)
        {
            ItemType = FileExplorerItemType.PICTURE;
            DisplayName = PictureName;
            IconSource = ImageSource.FromResource("HomeMediaApp.Icons.picture_icon.png");
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
