using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeMediaApp.Classes;

using Xamarin.Forms;

namespace HomeMediaApp.Pages
{
    public partial class NavigationDetailPage : ContentPage
    {
        private List<ImageCell> NavigationItems = new  List<ImageCell>();
        

        public NavigationDetailPage()
        {
            InitializeComponent();
            
            //ImageSource Temp = ImageSource.FromFile("Pages/settings_icon.png");
            NavigationItems.Add(new ImageCell()
            {
                Text = "Startseite",
                Height = 50,
                ImageSource = ImageSource.FromResource("HomeMediaApp.Pages.settings_icon.png"),
            });
            
            ListViewNavigationItems.ItemsSource = NavigationItems;
            //masterPageItems.Add(new MasterPageItem
            //{
            //    Title = "Einstellungen",
            //    IconSource = "todo.png",
            //    TargetType = typeof(SettingsPage)
            //});
            //ListViewNavigationItems.ItemsSource = NavigationItems;
        }
    }
}
