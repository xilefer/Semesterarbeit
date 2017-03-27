using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
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
                Debug.WriteLine(oOutputDevice.Type);
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
            if (UPnPDeviceList.Count == 0) { int j = 0;}
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
            UPnPDevice oDevice = (e.Item as UPnPDevice);
            try
            {
                //Methode für das event übergeben.
                UPnPAction oAction =
                    oDevice.DeviceMethods.Where(x => x.ServiceID == "ContentDirectory").ToList()[0].ActionList.Where(
                        x => x.ActionName == "Browse").ToList()[0];
                oAction.OnResponseReceived += OnResponseReceived;
                List<UPnPActionArgument> InArgs = new List<UPnPActionArgument>();
                foreach (UPnPActionArgument Arg in oAction.ArgumentList)
                {
                    if (Arg.Direction.ToLower() == "in")
                    {
                        InArgs.Add(Arg);
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
                string sRequestURI =
                    oDevice.Config.Root.Descendants().Where(Node => Node.Name.LocalName.ToLower() == "urlbase").ToList()
                        [0].Value;
                if (sRequestURI.Length == 0)
                {
                    throw new Exception("Die Funktion konnte nicht ausgeführt werden!");
                }
                if (sRequestURI.EndsWith("/")) sRequestURI = sRequestURI.Substring(0, sRequestURI.Length - 1); // Schrägstrich entfernen
                if (!oDevice.DeviceMethods.Where(x => x.ServiceID == "ContentDirectory").ToList()[0].ControlURL.StartsWith("/")) sRequestURI += "/";
                sRequestURI += oDevice.DeviceMethods.Where(x => x.ServiceID == "ContentDirectory").ToList()[0].ControlURL;
                oAction.Execute(sRequestURI, "ContentDirectory", ArgList);
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

            }
            catch (Exception gEx)
            {
                DisplayAlert(Title, gEx.Message, "OK");
            }
        }

        //Test zum Anzeigen der Response einer Action

        private void OnResponseReceived(XDocument oResponseDocument, ActionState oState)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (oState.ActionName == "Browse")
                {

                    UPnPContainer Container = UPnPContainer.GenerateRootContainer(oResponseDocument);
                    FolderItem RootFolderItem = new FolderItem("DUMMY");
                    FolderItem StartFolderItem = new FolderItem(Container.Title);
                    StartFolderItem.RelatedContainer = Container;
                    RootFolderItem.Childrens.Add(StartFolderItem);
                    RootFolderItem.RelatedContainer = Container;
                    // Jetzt zum Explorer wechseln
                    FileExplorerPage ExplorerPage = new FileExplorerPage();
                    ExplorerPage.MasterItem = RootFolderItem;
                    ExplorerPage.CurrentDevice = ListViewDevices.SelectedItem as UPnPDevice;
                    (Parent.Parent as MasterDetailPageHomeMediaApp).IsPresented = false;
                    (Parent.Parent as MasterDetailPageHomeMediaApp).Detail = new NavigationPage(ExplorerPage);
                    UPnPDevice oDevice = ListViewDevices.SelectedItem as UPnPDevice;
                    UPnPAction oAction = oDevice.DeviceMethods.Where(x => x.ServiceID == "ContentDirectory").ToList()[0].ActionList.Where(x => x.ActionName == "Browse").ToList()[0];
                    oAction.OnResponseReceived -= OnResponseReceived;
                }
            });
        }
    }
}
