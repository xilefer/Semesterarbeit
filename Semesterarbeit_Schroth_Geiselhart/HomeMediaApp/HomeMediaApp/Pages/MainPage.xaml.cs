using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xamarin.Forms;

namespace HomeMediaApp.Pages
{
    public partial class MainPage : ContentPage
    {
        private List<XDocument> XMLConfigurations = new List<XDocument>();
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

        private CSSPD oDeviceSearcher = new CSSPD();
        public MainPage()
        {
            InitializeComponent();
            oDeviceSearcher.ReceivedXml += new ReceivedXml(OnReceivedXML);
            oDeviceSearcher.StartSearch();
            BindingContext = this;
            Title = "Willkommen";
            Items.CollectionChanged += ItemsOnCollectionChanged;
            Init();
        }

        private void ItemsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            OnPropertyChanged("Items");
        }

        private void UpdateXMLConfigs(XDocument oReceivedXML)
        {
            string sUDN = oReceivedXML.Descendants().Where(e => e.Name.LocalName == "UDN").ToList().Count > 0
                ? oReceivedXML.Descendants().Where(e => e.Name.LocalName == "UDN").ToList()[0].Value
                : null;
            if (sUDN == null) return;   // Konfiguration enthält keine UDN
            // Ist UDN bereits vorhanden?
            XDocument Result = XMLConfigurations.FirstOrDefault(e =>
            {
                if (e.Descendants().Where(b => b.Name.LocalName == "UDN" && b.Value == sUDN).ToList().Count > 0) return true;
                return false;
            });
            if (Result == null || XMLConfigurations.Count == 0)
            {   // Es wurde keine Element mit dem UDN gefunden!
                XMLConfigurations.Add(oReceivedXML);
                ObservableCollection<string> TempItems = Items;
                TempItems.Add(oReceivedXML.Descendants().Where(e => e.Name.LocalName.ToLower() == "friendlyname").ToList()[0].Value);
                Items = TempItems;
            }
        }

        private void OnReceivedXML(XDocument oXmlConfig)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                UpdateXMLConfigs(oXmlConfig);
            });
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
            XDocument oConfiguration = null;
            foreach(XDocument config in XMLConfigurations)
            {
                List<XElement> Elements = config.Root.Descendants().Where(e => e.Name.LocalName.ToLower() == "friendlyname").ToList();
                if (Elements[0].Value == DeviceName)
                {
                    oConfiguration = config;
                }
            }
            if (oConfiguration == null) return;
            else
            {
                DisplayAlert("Daag", oConfiguration.ToString(), "Abbrecha");
            }
        }
    }
}
