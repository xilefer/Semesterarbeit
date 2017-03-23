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
}
