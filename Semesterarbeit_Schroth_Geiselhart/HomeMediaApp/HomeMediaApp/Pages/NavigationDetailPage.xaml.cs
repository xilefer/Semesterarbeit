using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeMediaApp.Classes;
using Xamarin.Forms;

namespace HomeMediaApp.Pages
{
    /// <summary>
    /// Die Klasse für Detail-Seiten
    /// </summary>
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

        /// <summary>
        /// Konstruktor
        /// </summary>
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
                IconSource = ImageSource.FromResource("HomeMediaApp.Icons.music_icon.png"),
                Title="Aktuelle Wiedergabe",
                TargetType = typeof(RemoteMediaPlayerPage)
            });
            PageItems = TempItems;
            listView.ItemsSource = PageItems;
        }

        /// <summary>
        /// Individualkonstruktor
        /// </summary>
        /// <param name="Parent">Element welches die neue Seite enthält</param>
        public NavigationDetailPage(MasterDetailPageHomeMediaApp Parent) : this()
        {
            this.mParent = Parent;
        }

        /// <summary>
        /// Item-Tapped Behandlung des Navigationssmenüs
        /// </summary>
        /// <param name="sender">Das aufrufende Objekt</param>
        /// <param name="e">Eventparameter</param>
        private void ListView_OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            MasterPageItem temp = e.Item as MasterPageItem;
            if (temp == null) return;
            if (temp.TargetType == typeof(RemoteMediaPlayerPage))
            {
                GlobalVariables.GlobalRemoteMediaPlayerPage.Parent = null;
                (Parent as MasterDetailPageHomeMediaApp).Detail = GlobalVariables.GlobalRemoteMediaPlayerPage;
                (Parent as MasterDetailPageHomeMediaApp).IsPresented = false;
            }
            else
            {
                (Parent as MasterDetailPageHomeMediaApp).Detail = new NavigationPage((Page)Activator.CreateInstance(temp.TargetType));
                (Parent as MasterDetailPageHomeMediaApp).IsPresented = false;
            }
        }
    }
}
