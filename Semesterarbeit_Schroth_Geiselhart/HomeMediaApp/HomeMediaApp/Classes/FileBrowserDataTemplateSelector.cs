using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace HomeMediaApp.Classes
{
    public class CustomImageCell : ViewCell
    {
        Image Icon = new Image() {Aspect = Aspect.AspectFit, HeightRequest = 90, WidthRequest = 90, VerticalOptions = LayoutOptions.CenterAndExpand, Margin = new Thickness(10,0,0,0)};
        Label LabelText = new Label() {FontSize = 15, TextColor=Color.Black, VerticalOptions = LayoutOptions.CenterAndExpand, HorizontalOptions = LayoutOptions.StartAndExpand, Margin = new Thickness(20,0,0,0) };
        public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(CustomImageCell), "", BindingMode.OneWay, null,
            (bindable, value, newValue) =>
            {
                (bindable as CustomImageCell).Text = (string) newValue;
            });
        public static readonly BindableProperty IconSourceProperty = BindableProperty.Create(nameof(IconSource), typeof(ImageSource), typeof(CustomImageCell), null, BindingMode.OneWay, null,
            (bindable, value, newValue) =>
            {
                (bindable as CustomImageCell).IconSource = (ImageSource) newValue;
            });
        public static readonly BindableProperty IconHeightWidth = BindableProperty.Create(nameof(ImageHeightWidth), typeof(int), typeof(CustomImageCell), 0, BindingMode.TwoWay, null,
            (bindable, value, newValue) =>
            {
                (bindable as CustomImageCell).ImageHeightWidth = (int)newValue;
            });
        public static readonly BindableProperty TextColorProperty = BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(CustomImageCell), Color.Black, BindingMode.OneWay, null,
            (bindable, value, newValue) =>
            {
                (bindable as CustomImageCell).TextColor = (Color) newValue;
            });


        public int ImageHeightWidth
        {
            get { return (int)Icon.HeightRequest; }
            set
            {
                Icon.HeightRequest = value;
                Icon.WidthRequest = value;
                OnPropertyChanged();
            }
        }

        public string Text
        {
            get { return LabelText.Text; }
            set { LabelText.Text = value; }
        }

        public ImageSource IconSource
        {
            get { return Icon.Source; }
            set { Icon.Source = value; }
        }

        public Color TextColor
        {
            get { return LabelText.TextColor; }
            set { LabelText.TextColor = value; }
        }

        public CustomImageCell()
        {
            StackLayout Stack = new StackLayout() {Orientation = StackOrientation.Horizontal, Children = { Icon, LabelText}, HeightRequest = 100 };
            View = Stack;
        }
    }

    public class ViewCellBase : CustomImageCell
    {
        public ViewCellBase()
        {
            MenuItem DetailsMenuItem = new MenuItem() { Text = "DETAILS" };
            DetailsMenuItem.Clicked += (sender, e) =>
            {
                MenuItem mi = (MenuItem)sender;
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
            MenuItem AddToPlayListMenuItem = new MenuItem() { Text = "ZUR WIEDERGABELISTE HINZUFÜGEN" };
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
            MenuItem PlayMenuItem = new MenuItem() { Text = "WIEDERGEBEN" };
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
            MenuItem AddToPlaylistItem = new MenuItem() { Text = "ZUR WIEDERGABELISTE HINZUFÜGEN" };
            AddToPlaylistItem.Clicked += (sender, e) =>
            {
                MenuItem mi = sender as MenuItem;
                MessagingCenter.Send(this, GlobalVariables.PlaylistAddToPlayLIstActionName, mi.BindingContext as PlaylistItem);
            };
            ContextActions.Add(AddToPlaylistItem);
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
        private DataTemplate TestTemplate = new DataTemplate(typeof(CustomImageCell));

        public FileBrowserDataTemplateSelector()
        {
            TestTemplate.SetValue(CustomImageCell.TextColorProperty, Color.Orange);
            // WidthHeight
            FolderTemplate.Bindings.Add(CustomImageCell.IconHeightWidth, new Binding("60"));
            MuiscTemplate.Bindings.Add(CustomImageCell.IconHeightWidth, new Binding("60"));
            PictureTemplate.Bindings.Add(CustomImageCell.IconHeightWidth, new Binding("60"));
            VideoTemplate.Bindings.Add(CustomImageCell.IconHeightWidth, new Binding("60"));
            ElseTemplate.Bindings.Add(CustomImageCell.IconHeightWidth, new Binding("60"));
            PlaylistTemplate.Bindings.Add(CustomImageCell.IconHeightWidth, new Binding("60"));
            TestTemplate.SetValue(CustomImageCell.IconHeightWidth, new Binding("60"));
            //Text
            FolderTemplate.Bindings.Add(CustomImageCell.TextProperty, new Binding("DisplayName"));
            MuiscTemplate.Bindings.Add(CustomImageCell.TextProperty, new Binding("DisplayName"));
            PictureTemplate.Bindings.Add(CustomImageCell.TextProperty, new Binding("DisplayName"));
            VideoTemplate.Bindings.Add(CustomImageCell.TextProperty, new Binding("DisplayName"));
            ElseTemplate.Bindings.Add(CustomImageCell.TextProperty, new Binding("DisplayName"));
            PlaylistTemplate.Bindings.Add(CustomImageCell.TextProperty, new Binding("DisplayName"));
            TestTemplate.Bindings.Add(CustomImageCell.TextProperty, new Binding("DisplayName"));
            //Image
            FolderTemplate.Bindings.Add(CustomImageCell.IconSourceProperty, new Binding("IconSource"));
            MuiscTemplate.Bindings.Add(CustomImageCell.IconSourceProperty, new Binding("IconSource"));
            PictureTemplate.Bindings.Add(CustomImageCell.IconSourceProperty, new Binding("IconSource"));
            VideoTemplate.Bindings.Add(CustomImageCell.IconSourceProperty, new Binding("IconSource"));
            ElseTemplate.Bindings.Add(CustomImageCell.IconSourceProperty, new Binding("IconSource"));
            PlaylistTemplate.Bindings.Add(CustomImageCell.IconSourceProperty, new Binding("IconSource"));
            TestTemplate.Bindings.Add(CustomImageCell.IconSourceProperty, new Binding("IconSource"));
            //Textcolor
            FolderTemplate.SetValue(CustomImageCell.TextColorProperty, Color.Black);
            MuiscTemplate.SetValue(CustomImageCell.TextColorProperty, Color.Black);
            PictureTemplate.SetValue(CustomImageCell.TextColorProperty, Color.Black);
            VideoTemplate.SetValue(CustomImageCell.TextColorProperty, Color.Black);
            ElseTemplate.SetValue(CustomImageCell.TextColorProperty, Color.Black);
            PlaylistTemplate.SetValue(CustomImageCell.TextColorProperty, Color.Black);
            TestTemplate.SetValue(CustomImageCell.TextColorProperty, Color.Black);
        }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            FileExplorerItemBase Item = item as FileExplorerItemBase;
            if (Item != null)
            {
                #region Entscheidung anhand des Item-Typs
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
                #endregion
            }
            return new DataTemplate(typeof(TextCell));
        }
    }
}
