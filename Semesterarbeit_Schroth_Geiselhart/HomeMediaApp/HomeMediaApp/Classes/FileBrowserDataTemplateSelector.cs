using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace HomeMediaApp.Classes
{
    public class ViewCellBase : ImageCell
    {
        public ViewCellBase()
        {
            MenuItem DetailsMenuItem = new MenuItem() {Text = "DETAILS"};
            DetailsMenuItem.Clicked += (sender, e) =>
            {
                MenuItem mi = (MenuItem) sender;
                MessagingCenter.Send(this, GlobalVariables.BaseShowDetailsActionName, mi.BindingContext as FileExplorerItemBase);
            };
            this.ContextActions.Add(DetailsMenuItem);
        }
    }

    public class FolderViewCell : ViewCellBase
    {
        public FolderViewCell()
        {
            MenuItem OpenMenuItem = new MenuItem() { Text = "ÖFFNEN" };
            OpenMenuItem.Clicked += (sender, e) =>
            {
                MenuItem mi = (MenuItem)sender;
                MessagingCenter.Send(this, GlobalVariables.FolderOpenActionName, mi.BindingContext as FolderItem);
            };
            this.ContextActions.Add(OpenMenuItem);
        }
    }

    public class MusicViewCell : ViewCellBase
    {
        public MusicViewCell()
        {
            MenuItem PlayMenuItem = new MenuItem() { Text = "WIEDERGEBEN" };
            PlayMenuItem.Clicked += (sender, e) =>
            {
                MenuItem mi = sender as MenuItem;
                MessagingCenter.Send(this, GlobalVariables.MusicPlayActionName, mi.BindingContext as MusicItem);
            };
            this.ContextActions.Add(PlayMenuItem);
            MenuItem AddToPlayListMenuItem = new MenuItem() {Text = "ZUR WIEDERGABELIST HINZUFÜGEN"};
            AddToPlayListMenuItem.Clicked += (sender, e) =>
            {
                MenuItem mi = sender as MenuItem;
                MessagingCenter.Send(this, GlobalVariables.MusicAddToPlayLIstActionName, mi.BindingContext as MusicItem);
            };
            ContextActions.Add(AddToPlayListMenuItem);
        }
    }

    public class ImageViewCell : ViewCellBase
    {
        public ImageViewCell()
        {
            MenuItem OpenMenuItem = new MenuItem() { Text = "ÖFFNEN" };
            OpenMenuItem.Clicked += (sender, e) =>
            {
                MenuItem mi = sender as MenuItem;
                MessagingCenter.Send(this, GlobalVariables.ImageOpenActionName, mi.BindingContext as PictureItem);
            };
            this.ContextActions.Add(OpenMenuItem);
        }
    }

    public class VideoViewCell : ViewCellBase
    {
        public VideoViewCell()
        {
            MenuItem PlayMenuItem = new MenuItem() {Text = "WIEDERGEBEN"};
            PlayMenuItem.Clicked += (sender, e) =>
            {
                MenuItem mi = sender as MenuItem;
                MessagingCenter.Send(this, GlobalVariables.VideoPlayActionName, mi.BindingContext as VideoItem);
            };
            this.ContextActions.Add(PlayMenuItem);
        }
    }

    public class PlayListViewCell : ViewCellBase
    {
        public PlayListViewCell()
        {
            MenuItem PlayMenuItem = new MenuItem() { Text = "WIEDERGEBEN" };
            PlayMenuItem.Clicked += (sender, e) =>
            {
                MenuItem mi = sender as MenuItem;
                MessagingCenter.Send(this, GlobalVariables.PlaylistPlayActionName, mi.BindingContext as PlaylistItem);
            };
            this.ContextActions.Add(PlayMenuItem);
        }
    }

    /// <summary>
    /// Eigener TemplateSelector für dynamische Kontext-Aktionen in einer ListView
    /// </summary>
    public class FileBrowserDataTemplateSelector : Xamarin.Forms.DataTemplateSelector
    {
        private DataTemplate FolderTemplate = new DataTemplate(typeof(FolderViewCell));
        private DataTemplate MuiscTemplate = new DataTemplate(typeof(MusicViewCell));
        private DataTemplate PictureTemplate = new DataTemplate(typeof(ImageViewCell));
        private DataTemplate VideoTemplate = new DataTemplate(typeof(VideoViewCell));
        private DataTemplate ElseTemplate = new DataTemplate(typeof(ViewCellBase));
        private DataTemplate PlaylistTemplate = new DataTemplate(typeof(PlayListViewCell));

        public FileBrowserDataTemplateSelector()
        {
            FolderTemplate.Bindings.Add(TextCell.TextProperty, new Binding("DisplayName"));
            MuiscTemplate.Bindings.Add(TextCell.TextProperty, new Binding("DisplayName"));
            PictureTemplate.Bindings.Add(TextCell.TextProperty, new Binding("DisplayName"));
            VideoTemplate.Bindings.Add(TextCell.TextProperty, new Binding("DisplayName"));
            ElseTemplate.Bindings.Add(TextCell.TextProperty, new Binding("DisplayName"));
            PlaylistTemplate.Bindings.Add(TextCell.TextProperty, new Binding("DisplayName"));
            FolderTemplate.Bindings.Add(ImageCell.ImageSourceProperty, new Binding("IconSource"));
            MuiscTemplate.Bindings.Add(ImageCell.ImageSourceProperty, new Binding("IconSource"));
            PictureTemplate.Bindings.Add(ImageCell.ImageSourceProperty, new Binding("IconSource"));
            VideoTemplate.Bindings.Add(ImageCell.ImageSourceProperty, new Binding("IconSource"));
            ElseTemplate.Bindings.Add(ImageCell.ImageSourceProperty, new Binding("IconSource"));
            PlaylistTemplate.Bindings.Add(ImageCell.ImageSourceProperty, new Binding("IconSource"));
            FolderTemplate.SetValue(TextCell.TextColorProperty, Color.Black);
            MuiscTemplate.SetValue(TextCell.TextColorProperty, Color.Black);
            PictureTemplate.SetValue(TextCell.TextColorProperty, Color.Black);
            VideoTemplate.SetValue(TextCell.TextColorProperty, Color.Black);
            ElseTemplate.SetValue(TextCell.TextColorProperty, Color.Black);
            PlaylistTemplate.SetValue(TextCell.TextColorProperty, Color.Black);
        }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            FileExplorerItemBase Item = item as FileExplorerItemBase;
            if (Item != null)
            {
                DataTemplate ReturnTemplate = null;
                switch (Item.ItemType)
                {
                    case FileExplorerItemType.FOLDER:
                        ReturnTemplate = FolderTemplate;
                        break;
                    case FileExplorerItemType.MUSIC:
                        ReturnTemplate = MuiscTemplate;
                        break;
                    case FileExplorerItemType.PICTURE:
                        ReturnTemplate = PictureTemplate;
                        break;
                    case FileExplorerItemType.VIDEO:
                        ReturnTemplate = VideoTemplate;
                        break;
                    case FileExplorerItemType.PLAYLIST:
                        ReturnTemplate = PlaylistTemplate;
                        break;
                    case FileExplorerItemType.ELSE:
                        ReturnTemplate = ElseTemplate;
                        break;
                }
                if (ReturnTemplate != null)
                {
                    return ReturnTemplate;
                }
            }
            // KP ob das so gut ist?
            return new DataTemplate(typeof(TextCell));
        }
    }
}
