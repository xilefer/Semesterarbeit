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
        private ObservableCollection<UPnPDevice> mUPnPDeviceList = new ObservableCollection<UPnPDevice>();
        public ObservableCollection<UPnPDevice> UPnPDeviceList
        {
            get
            {
                return mUPnPDeviceList;
            }
            set
            {
                if (mUPnPDeviceList == value) return;
                mUPnPDeviceList = value;
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
            UPnPDeviceList.CollectionChanged += ItemsOnCollectionChanged;
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
            foreach (UPnPDevice Device in UPnPDeviceList)
            {
                foreach (XElement oConfigElement in Device.Config.Root.Elements())
                {
                    List<XElement> ElementList = oConfigElement.Elements().ToList();
                    foreach (XElement oElement in ElementList)
                    {
                        if (oElement.Name.LocalName == "UDN" && oElement.Value == sUDN)
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
                oParser.DeviceFinished += new DeviceFinished(OnDeviceFinished);
                oDevice.DeviceName = oReceivedXML.Descendants().Where(e => e.Name.LocalName.ToLower() == "friendlyname").ToList()[0].Value;
                oDevice.Config = oReceivedXML;
                oDevice.DeviceAddress = oDeviceAddress;
                UPnPDevice oOutputDevice = oParser.Parse(oDevice);
                if (oOutputDevice.Type.ToLower() == "mediarenderer" || oOutputDevice.Type.ToLower() == "mediaserver")
                {
                    UPnPDeviceList.Add(oOutputDevice);
                    OnPropertyChanged("UPnPDeviceList");    // Damit die Oberfläche aktualisiert wird
                }
            }
        }

        private void OnReceivedXML(XDocument oXmlConfig, Uri oDeviceAddress)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                UpdateXMLConfigs(oXmlConfig, oDeviceAddress);
            });
        }

        private void OnDeviceFinished(UPnPDevice oDevice, UPnPService oService)
        {
            UPnPDevice oTempDevice = UPnPDeviceList.Where(e => e.DeviceName == oDevice.DeviceName).ToList()[0];
            int i = UPnPDeviceList.IndexOf(oTempDevice);
            UPnPDeviceList[i].DeviceMethods.Add(oService);
            Device.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged("UPnPDeviceList");
            });

        }

        private void Init()
        {
            OuterGrid.ForceLayout();
        }

        private void ListViewDevices_OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            string Config = (e.Item as UPnPDevice).Config.ToString();
            if (Config != null) DisplayAlert("Test", Config, "Abbrechen");
        }
    }
}
