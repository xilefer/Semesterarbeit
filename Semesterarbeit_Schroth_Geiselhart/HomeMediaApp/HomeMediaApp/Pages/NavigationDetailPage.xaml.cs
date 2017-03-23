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
        private List<MasterPageItem> mPageItems = new List<MasterPageItem>();

        public ListView ListView { get { return listView; } }

        private MasterDetailPageHomeMediaApp mParent = null;

        public List<MasterPageItem> PageItems
        {
            get { return mPageItems; }
            set
            {
                if (mPageItems == value) return;
                mPageItems = value;
                OnPropertyChanged();
            }
        }

        public NavigationDetailPage()
        {
            InitializeComponent();
            BindingContext = this;
            List<MasterPageItem> TempItems = new List<MasterPageItem>();
            TempItems.Add(new MasterPageItem()
            {
                IconSource = ImageSource.FromResource("HomeMediaApp.Icons.home_icon.png"),
                Title = "Startseite",
                TargetType = typeof(MainPage)
            });
            TempItems.Add(new MasterPageItem()
            {
                IconSource = ImageSource.FromResource("HomeMediaApp.Icons.settings_icon.png"),
                Title = "Einstellungen",
                TargetType = typeof(SettingsPage)
            });
            TempItems.Add(new MasterPageItem()
            {
                IconSource = ImageSource.FromResource("HomeMediaApp.Icons.folder_icon.png"),
                Title = "Explorer",
                TargetType = typeof(FileExplorerPage)
            });
            PageItems = TempItems;
            listView.ItemsSource = PageItems;

        }

        public NavigationDetailPage(MasterDetailPageHomeMediaApp Parent) : this()
        {
            this.mParent = Parent;
        }

        private void ListView_OnItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            MasterPageItem temp = e.SelectedItem as MasterPageItem;
            if (temp == null) return;
            (Parent as MasterDetailPageHomeMediaApp).Detail = new NavigationPage((Page) Activator.CreateInstance(temp.TargetType));
            (Parent as MasterDetailPageHomeMediaApp).IsPresented = false;
        }
    }
}
