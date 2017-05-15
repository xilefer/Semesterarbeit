using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace HomeMediaApp.Classes
{
    public class PlayListViewViewCell : ImageCell
    {
        public PlayListViewViewCell()
        {
            MenuItem DeleteMenuItem = new MenuItem() { Text = "ENTFERNEN" };
            DeleteMenuItem.Clicked += (sender, e) =>
            {
                MenuItem mi = sender as MenuItem;
                MessagingCenter.Send(this, GlobalVariables.RemoveTrackFromPlayListActionName, mi.BindingContext as MusicItem);
            };
            ContextActions.Add(DeleteMenuItem);
        }
    }

    public class PlayListViewDataTemplateSelector : DataTemplateSelector
    {
        private DataTemplate Template = new DataTemplate(typeof(PlayListViewViewCell));

        public PlayListViewDataTemplateSelector()
        {
            Template.Bindings.Add(TextCell.TextProperty, new Binding("DisplayName"));
            Template.Bindings.Add(ImageCell.ImageSourceProperty, new Binding("IconSource"));
            Template.Bindings.Add(TextCell.TextColorProperty, new Binding("TextColor"));
        }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            return Template;  
        }
    }
}
