using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace HomeMediaApp.Pages
{
    public partial class MainPage : ContentPage
    {
        private ObservableCollection<string> mItems = new ObservableCollection<string>();
        public ObservableCollection<string> Items {
            get { return mItems; }
            set
            {
                if (mItems == value) return;
                mItems = value;
                OnPropertyChanged();
            }
        }

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
            Title = "Willkommen";
            Items.CollectionChanged += ItemsOnCollectionChanged;
            Init();
        }

        private void ItemsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            OnPropertyChanged("Items");
        }


        private async void SettingsButton_OnClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SettingsPage());
        }

        private void Init()
        {
            OuterGrid.ForceLayout();
        }
        
        private void ListViewDevices_OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            string Name = (e.Item as string);
            if(Name != null) ProcessDeviceItemTapped(Name);
        }

        private void ProcessDeviceItemTapped(string DeviceName)
        {
            throw new NotImplementedException();
        }
    }
}
