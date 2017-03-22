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
using HomeMediaApp.Classes;

namespace HomeMediaApp.Pages
{
    public partial class MainPage : ContentPage
    {
        //private List<XDocument> XMLConfigurations = new List<XDocument>();
        private List<UPnPDevice> UPnPDeviceList = new List<UPnPDevice>();
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

        private void UpdateXMLConfigs(XDocument oReceivedXML, Uri oDeviceAddress)
        {
            string sUDN = oReceivedXML.Descendants().Where(e => e.Name.LocalName == "UDN").ToList().Count > 0
                ? oReceivedXML.Descendants().Where(e => e.Name.LocalName == "UDN").ToList()[0].Value
                : null;
            if (sUDN == null) return;   // Konfiguration enthält keine UDN
                                        // Ist UDN bereits vorhanden?

            bool DocumentExists = false;
            foreach(UPnPDevice Device in UPnPDeviceList)
            {
                foreach(XElement oConfigElement in Device.Config.Root.Elements())
                {
                    List<XElement> ElementList = oConfigElement.Elements().ToList();
                    foreach(XElement oElement in ElementList)
                    {
                        if(oElement.Name.LocalName == "UDN" && oElement.Value == sUDN)
                        {
                            DocumentExists = true;
                        }
                    }
                }
            }
       
            if (DocumentExists == false || UPnPDeviceList.Count == 0)
            {   // Es wurde keine Element mit dem UDN gefunden!
                UPnPDevice oDevice = new UPnPDevice();
                XMLParser oParser = new XMLParser();
                oDevice.DeviceName = oReceivedXML.Descendants().Where(e => e.Name.LocalName.ToLower() == "friendlyname").ToList()[0].Value;
                oDevice.Config = oReceivedXML;
                oDevice.DeviceAddress = oDeviceAddress;
                UPnPDevice oOutputDevice = oParser.Parse(oDevice);
                if (oOutputDevice.Type.ToLower() == "mediarenderer" || oOutputDevice.Type.ToLower() == "mediaserver")
                {
                    UPnPDeviceList.Add(oOutputDevice);
                    ObservableCollection<string> TempItems = Items;
                    TempItems.Add(oOutputDevice.DeviceName);
                    Items = TempItems;
                }
            }
        }

        private void OnReceivedXML(XDocument oXmlConfig, Uri oDeviceAddress)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                UpdateXMLConfigs(oXmlConfig,oDeviceAddress);
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
            foreach(UPnPDevice oDevice in UPnPDeviceList)
            {
                List<XElement> Elements = oDevice.Config.Root.Descendants().Where(e => e.Name.LocalName.ToLower() == "friendlyname").ToList();
                if (Elements[0].Value == DeviceName)
                {
                    oConfiguration = oDevice.Config;
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
