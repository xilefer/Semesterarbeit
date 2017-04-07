using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xamarin.Forms;
using HomeMediaApp.Classes;

namespace HomeMediaApp.Pages
{
    public partial class MainPage : ContentPage
    {
        public ObservableCollection<UPnPDevice> UPnPServerList
        {
            get
            {
                return GlobalVariables.UPnPMediaServers;
            }
            set
            {
                if (GlobalVariables.UPnPMediaServers == value) return;
                GlobalVariables.UPnPMediaServers = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<UPnPDevice> UPnPMediaRendererList
        {
            get { return GlobalVariables.UPnPMediaRenderer; }
            set
            {
                if(GlobalVariables.UPnPMediaRenderer == value) return;
                GlobalVariables.UPnPMediaRenderer = value;
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
            UPnPServerList.CollectionChanged += ItemsOnCollectionChanged;
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
            foreach (UPnPDevice Device in UPnPServerList)
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
            foreach (UPnPDevice Device in UPnPMediaRendererList)
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
            if (DocumentExists == false || UPnPServerList.Count == 0)
            {   // Es wurde keine Element mit dem UDN gefunden!
                UPnPDevice oDevice = new UPnPDevice();
                XMLParser oParser = new XMLParser();
                oParser.DeviceFinished += new DeviceFinished(OnDeviceFinished);
                oDevice.DeviceName = oReceivedXML.Descendants().Where(e => e.Name.LocalName.ToLower() == "friendlyname").ToList()[0].Value;
                oDevice.Config = oReceivedXML;
                oDevice.DeviceAddress = oDeviceAddress;
                UPnPDevice oOutputDevice = oParser.Parse(oDevice);
                // Jetzt wird zwischen mediaserver und Mediarenderer unterschieden (Wägs dr oberfläch)
                if (oOutputDevice.Type.ToLower() == "mediaserver") 
                {
                    UPnPServerList.Add(oOutputDevice);
                    OnPropertyChanged("UPnPServerList");    // Damit die Oberfläche aktualisiert wird
                }
                else if (oOutputDevice.Type.ToLower() == "mediarenderer")
                {
                    UPnPMediaRendererList.Add(oOutputDevice);
                    OnPropertyChanged("UPnPMediaRenererList");
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
            List<UPnPDevice> TempList = new List<UPnPDevice>();
            do
            {
                if(oDevice.Type.ToLower() =="mediaserver") TempList = UPnPServerList.Where(e => e.DeviceName == oDevice.DeviceName).ToList();
                else if (oDevice.Type.ToLower() == "mediarenderer") TempList = UPnPServerList.Where(e => e.DeviceName == oDevice.DeviceName).ToList();
            } while (TempList.Count == 0);
            UPnPDevice oTempDevice = TempList[0];
            // Ebenfalls zwischen renderer und Server unterscheiden
            if (oDevice.Type.ToLower() == "mediaserver")
            {
                int i = UPnPServerList.IndexOf(oTempDevice);
                UPnPServerList[i].DeviceMethods.Add(oService);
            }
            else if (oDevice.Type.ToLower() == "mediarenderer")
            {
                int i = UPnPMediaRendererList.IndexOf(oTempDevice);
                UPnPMediaRendererList[i].DeviceMethods.Add(oService);
            }
            Device.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged("UPnPServerList");
            });

        }

        private void Init()
        {
            OuterGrid.ForceLayout();
        }

        private void ListViewDevices_OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            string Config = (e.Item as UPnPDevice).Config.ToString();
            UPnPDevice oDevice = (e.Item as UPnPDevice);
            UPnPAction BrowseAction = oDevice.DeviceMethods.Where(y => y.ServiceType.ToLower() == "contentdirectory").ToList()[0].ActionList.Where(x => x.ActionName.ToLower() == "browse").ToList()[0];
            BrowseAction.OnResponseReceived += new ResponseReceived(OnResponseReceived);

            List<UPnPActionArgument> InArgs = new List<UPnPActionArgument>();
            foreach (UPnPActionArgument oArg in BrowseAction.ArgumentList)
            {
                if (oArg.Direction == "in")
                {
                    InArgs.Add(oArg);
                }
            }
            UPnPStateVariables.A_ARG_TYPE_BrowseFlag = UPnPBrowseFlag.BrowseMetadata;
            UPnPStateVariables.A_ARG_TYPE_Count = "100";
            UPnPStateVariables.A_ARG_TYPE_Index = "0";
            UPnPStateVariables.A_ARG_TYPE_ObjectID = "0";
            UPnPStateVariables.A_ARG_TYPE_SortCriteria = "+upnp:artist";
            Type TypeInfo = typeof(UPnPStateVariables);
            List<Tuple<string, string>> ArgList = new List<Tuple<string, string>>();
            foreach (UPnPActionArgument Arg in InArgs)
            {
                PropertyInfo ResultProperty = TypeInfo.GetRuntimeProperty(Arg.RelatedStateVariable);
                if (ResultProperty != null)
                {
                    ArgList.Add(new Tuple<string, string>(Arg.Name, ResultProperty.GetValue(null).ToString()));
                }
                else
                {
                    throw new Exception("Die Funktion konnte nicht ausgeführt werden!");
                }
            }
            string sRequestURI = oDevice.Config.Root.Descendants().Where(Node => Node.Name.LocalName.ToLower() == "urlbase").ToList()
                        [0].Value;
            if (sRequestURI.Length == 0)
            {
                throw new Exception("Die Funktion konnte nicht ausgeführt werden!");
            }
            if (sRequestURI.EndsWith("/")) sRequestURI = sRequestURI.Substring(0, sRequestURI.Length - 1); // Schrägstrich entfernen
            if (!oDevice.DeviceMethods.Where(x => x.ServiceID == "ContentDirectory").ToList()[0].ControlURL.StartsWith("/")) sRequestURI += "/";
            sRequestURI += oDevice.DeviceMethods.Where(x => x.ServiceID == "ContentDirectory").ToList()[0].ControlURL;
            BrowseAction.Execute(sRequestURI, "ContentDirectory", ArgList);

            //Methode für das event übergeben.
            //UPnPAction oAction = oDevice.DeviceMethods.Where(x => x.ServiceID == "ContentDirectory").ToList()[0].ActionList.Where(x => x.ActionName == "Browse").ToList()[0];
            //oAction.OnResponseReceived += OnResponseReceived;


            //Request URI muss wie folgt aussehen wobei das nach dem Port = Service.ControlURL ist
            //string RequestURI = @"http://129.144.51.89:49000/MediaServer/ContentDirectory/Control";

            //Beispiel Tupel für die in Arguments der Action bspw aus Action.Argumentlist.Name

            //List<Tuple<string,string>> args = new List<Tuple<string, string>>();
            //args.Add(new Tuple<string, string>("ObjectID", "0"));
            //args.Add(new Tuple<string, string>("BrowseFlag", "BrowseDirectChildren"));
            //args.Add(new Tuple<string, string>("Filter", "*"));
            //args.Add(new Tuple<string, string>("StartingIndex", "0"));
            //args.Add(new Tuple<string, string>("RequestCount", "10"));
            //args.Add(new Tuple<string, string>("SortCriteria", "*"));

            //Execute brauch die ControlURL und die ServiceID des Services und die Argumentlist der Action
            //oAction.Execute(RequestURI,"ContentDirectory", args);


            //if (Config != null) DisplayAlert("Test", Config, "Abbrechen");
        }

        //Test zum Anzeigen der Response einer Action

        private void OnResponseReceived(XDocument oResponseDocument, ActionState oState)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (oState.ActionName.ToLower() == "browse")
                {
                    FileExplorerPage oExplorerPage = new FileExplorerPage();
                    UPnPContainer RootContainer = UPnPContainer.GenerateRootContainer(oResponseDocument);
                    FolderItem MasterItem = new FolderItem(RootContainer.Title);
                    FolderItem RootItem = new FolderItem(RootContainer.Title);
                    MasterItem.RelatedContainer = RootContainer;
                    RootItem.RelatedContainer = RootContainer;
                    RootItem.AddChild(MasterItem);
                    oExplorerPage.CurrentDevice = ListViewDevices.SelectedItem as UPnPDevice;
                    oExplorerPage.MasterItem = RootItem;
                    (Parent.Parent as MasterDetailPageHomeMediaApp).IsPresented = false;
                    (Parent.Parent as MasterDetailPageHomeMediaApp).Detail = oExplorerPage;
                    (ListViewDevices.SelectedItem as UPnPDevice).DeviceMethods.Where(y => y.ServiceType.ToLower() == "contentdirectory").ToList()[0].ActionList.Where(x => x.ActionName.ToLower() == "browse").ToList()[0].OnResponseReceived -= OnResponseReceived;
                }
            });
        }
    }
}
