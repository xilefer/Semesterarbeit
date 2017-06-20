using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xamarin.Forms;
using HomeMediaApp.Classes;
using HomeMediaApp.Interfaces;

namespace HomeMediaApp.Pages
{
    /// <summary>
    /// Hintergrundprogrammcode der Startseite
    /// </summary>
    public partial class MainPage : ContentPage
    {
        private bool BrowseResponseReceived = false;
        private bool SetRendererInfoResponseReceived = false;
        private CSSDP oDeviceSearcher = new CSSDP();

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
            get
            {
                return GlobalVariables.UPnPMediaRenderer;
            }
            set
            {
                if (GlobalVariables.UPnPMediaRenderer == value) return;
                GlobalVariables.UPnPMediaRenderer = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Konstruktor
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            oDeviceSearcher.ReceivedXml += OnReceivedXML;
            BindingContext = this;
            UPnPServerList.CollectionChanged += ItemsOnCollectionChanged;
            OuterGrid.ForceLayout();
        }

        /// <summary>
        /// Wird vor dem Anzeigen ausgelöst
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            oDeviceSearcher.StartSearch();
        }

        /// <summary>
        /// Wird vor dem Verschwinden ausgelöst
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            oDeviceSearcher.StopSearch();
        }

        /// <summary>
        /// Wird ausgelöst wenn sich die Liste mit den Medienservern ändert
        /// </summary>
        /// <param name="sender">Das aufrufende Objekt</param>
        /// <param name="notifyCollectionChangedEventArgs">Eventparameter</param>
        private void ItemsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            OnPropertyChanged("UPnPServerList");
        }

        /// <summary>
        /// Wird aufgerufen um eine neue XML-Konfiguration zu verarbeiten
        /// </summary>
        /// <param name="oReceivedXML">Die erhaltenen XML-Konfiguration</param>
        /// <param name="oDeviceAddress">Die URI zum Gerät</param>
        private void UpdateXMLConfigs(XDocument oReceivedXML, Uri oDeviceAddress)
        {
            string sUDN = oReceivedXML.Descendants().Where(e => e.Name.LocalName == "UDN").ToList().Count > 0
                ? oReceivedXML.Descendants().Where(e => e.Name.LocalName == "UDN").ToList()[0].Value
                : null;
            if (sUDN == null) return;   // Konfiguration enthält keine UDN
                                        // Ist UDN bereits vorhanden?
            bool DocumentExists = false;
            foreach (UPnPDevice Device in GlobalVariables.UPnPMediaServers)
            {
                if (Device.Type == "DUMMY") continue;
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
            foreach (UPnPDevice Device in GlobalVariables.UPnPMediaRenderer)
            {
                if (Device.Type == "DUMMY") continue;
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
            if (DocumentExists == false || GlobalVariables.UPnPMediaServers.Count == 0)
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
                    bool ClearCollection = false;
                    foreach (var upnPMediaServer in GlobalVariables.UPnPMediaServers)
                    {
                        if (upnPMediaServer.Type == "DUMMY") ClearCollection = true;
                    }
                    if (ClearCollection) GlobalVariables.UPnPMediaServers.Clear();
                    GlobalVariables.UPnPMediaServers.Add(oOutputDevice);
                    OnPropertyChanged("UPnPServerList");    // Damit die Oberfläche aktualisiert wird
                    ForceLayout();
                }
                else if (oOutputDevice.Type.ToLower() == "mediarenderer")
                {
                    bool ClearCollection = false;
                    foreach (var upnPDevice in GlobalVariables.UPnPMediaRenderer)
                    {
                        if (upnPDevice.Type == "DUMMY") ClearCollection = true;
                    }
                    if (ClearCollection) GlobalVariables.UPnPMediaRenderer.Clear();
                    GlobalVariables.UPnPMediaRenderer.Add(oOutputDevice);
                    OnPropertyChanged("UPnPMediaRendererList");
                    ForceLayout();
                }
            }
        }

        /// <summary>
        /// Wird aufgerufen wenn eine XML-Konfiguration über das Netzwerk erhalten wurde
        /// </summary>
        /// <param name="oXmlConfig">Die erhaltene XML-Konfiguration</param>
        /// <param name="oDeviceAddress">Die URI zum Gerät</param>
        private void OnReceivedXML(XDocument oXmlConfig, Uri oDeviceAddress)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                UpdateXMLConfigs(oXmlConfig, oDeviceAddress);
            });
        }

        /// <summary>
        /// Aktualisiert die Geräte und deren Funktionen
        /// Callbackfunktion
        /// </summary>
        /// <param name="oDevice">Das betroffene Netzwerkgerät</param>
        /// <param name="oService">Der neue Service</param>
        private async void OnDeviceFinished(UPnPDevice oDevice, UPnPService oService)
        {
            List<UPnPDevice> TempList = new List<UPnPDevice>();
            while (!Monitor.TryEnter(UPnPServerList))
            {
                await Task.Delay(50);  // 50 ms schlafen legen
            }
            try
            {
                while (!Monitor.TryEnter(UPnPMediaRendererList))
                {
                    await Task.Delay(50);   // 50 ms schlafen legen
                }
                try
                {
                    do
                    {
                        if (oDevice.Type.ToLower() == "mediaserver") TempList = UPnPServerList.Where(e => e.DeviceName == oDevice.DeviceName).ToList();
                        else if (oDevice.Type.ToLower() == "mediarenderer") TempList = UPnPMediaRendererList.Where(e => e.DeviceName == oDevice.DeviceName).ToList();
                    } while (TempList.Count == 0);
                }
                finally { Monitor.Exit(UPnPMediaRendererList);}
            }
            finally { Monitor.Exit(UPnPServerList); }
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

        /// <summary>
        /// Behandelt die Auswahl eines Medienservers aus der Listenansicht
        /// </summary>
        /// <param name="sender">Das aufrufende Objekt</param>
        /// <param name="e">Eventparameter</param>
        private void ListViewDevices_OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            foreach(UPnPDevice oRenderer in GlobalVariables.UPnPMediaRenderer) {  SetRendererInfo(oRenderer);}
            if ((e.Item as UPnPDevice).Type == "DUMMY") return;
            UPnPDevice oDevice = (e.Item as UPnPDevice);
            UPnPService ContentDirectoryService = oDevice.DeviceMethods.FirstOrDefault(y => y.ServiceType.ToLower() == "contentdirectory");
            if (ContentDirectoryService == null) return;
            UPnPAction BrowseAction = ContentDirectoryService.ActionList.FirstOrDefault(x => x.ActionName.ToLower() == "browse");
            if (BrowseAction == null) return;
            ResponseReceived Temp = OnResponseReceived;
            BrowseAction.OnResponseReceived += Temp;

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
            string sRequestURI = oDevice.DeviceAddress.Scheme + "://" + oDevice.DeviceAddress.Authority;
            if (sRequestURI.Length == 0)
            {
                throw new Exception("Die Funktion konnte nicht ausgeführt werden!");
            }
            if (sRequestURI.EndsWith("/")) sRequestURI = sRequestURI.Substring(0, sRequestURI.Length - 1); // Schrägstrich entfernen
            if (!oDevice.DeviceMethods.Where(x => x.ServiceID == "ContentDirectory").ToList()[0].ControlURL.StartsWith("/")) sRequestURI += "/";
            sRequestURI += oDevice.DeviceMethods.Where(x => x.ServiceID == "ContentDirectory").ToList()[0].ControlURL;
            BrowseAction.Execute(sRequestURI, "ContentDirectory", ArgList);
            while (!BrowseResponseReceived) { Task.Delay(20); }
            BrowseResponseReceived = false;
            BrowseAction.OnResponseReceived -= Temp;
        }

        /// <summary>
        /// Callback-Funktion für die Ausgabegerät-Details
        /// </summary>
        /// <param name="oResponseDocument">XML Antwort</param>
        /// <param name="oState">Status-Objekt der Anfrage</param>
        private void OnResponseReceived(XDocument oResponseDocument, ActionState oState)
        {
            try
            {
                if (oResponseDocument != null)
                {
                    if (oState.ActionName.ToLower() == "browse")
                    {
                        Device.BeginInvokeOnMainThread(() =>
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
                            (Parent.Parent as MasterDetailPageHomeMediaApp).Detail = new NavigationPage(oExplorerPage);
                            (ListViewDevices.SelectedItem as UPnPDevice).DeviceMethods.Where(y => y.ServiceType.ToLower() == "contentdirectory").ToList()[0].ActionList.Where(x => x.ActionName.ToLower() == "browse").ToList()[0].OnResponseReceived -= OnResponseReceived;
                        });
                    }
                }
            }
            finally
            {
                BrowseResponseReceived = true;
            }
        }

        /// <summary>
        /// Behandelt die Auswahl eines Ausgabegeräts aus der Listenansicht
        /// </summary>
        /// <param name="sender">Das aufrufende Objekt</param>
        /// <param name="e">Eventparameter</param>
        private void ListViewRenderer_OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            UPnPDevice TappedDevice = e.Item as UPnPDevice;
            SetRendererInfo(TappedDevice);
            string Message = "";
            foreach (string Key in TappedDevice.Protocoltypes.Keys)
            {
                Message += Key + ": " + Environment.NewLine;
                foreach (string Value in TappedDevice.Protocoltypes[Key])
                {
                    Message += Value + ";  ";
                }
                Message += Environment.NewLine;
            }
            DisplayAlert("Unterstützte Protokolle: " + TappedDevice.DeviceName, Message, "OK");
        }
        
        /// <summary>
        /// Ruft die Eigenschaften eines Wiedergabegeräts ab
        /// </summary>
        /// <param name="Renderer">Das Gerät für das Eigenschaften abgerufen werden sollen</param>
        private void SetRendererInfo(UPnPDevice Renderer)
        {
            if (Renderer.Type == "DUMMY") return;
            if (Renderer.Protocoltypes.Count > 0) return;
            List<UPnPService> ConnManList = Renderer.DeviceMethods.Where(e => e.ServiceType.ToLower() == "connectionmanager").ToList();
            if (ConnManList.Count == 0) return;
            List<UPnPAction> ProtocolInfoActions = ConnManList[0].ActionList.Where(e => e.ActionName.ToLower() == "getprotocolinfo").ToList();
            if (ProtocolInfoActions.Count == 0) return;
            SetRendererInfoResponseReceived = false;
            UPnPAction GetProtocolInfoAction = ProtocolInfoActions[0];
            ResponseReceived temp = SetRendererInfoResponse;
            GetProtocolInfoAction.OnResponseReceived += temp;
            string RequestURL = Renderer.DeviceAddress.Scheme + @"://" + Renderer.DeviceAddress.Authority + ConnManList[0].ControlURL;
            GetProtocolInfoAction.Execute(RequestURL, ConnManList[0].ServiceType, new List<Tuple<string, string>>(), Renderer.DeviceName);

            while (!SetRendererInfoResponseReceived)
            {
                Task.Delay(5);
            }
            GetProtocolInfoAction.OnResponseReceived -= temp;
        }

        /// <summary>
        /// Callback-Funktion von SetRendererInfo
        /// </summary>
        /// <param name="oResponseDocument">XML Antwort</param>
        /// <param name="oState">Eventparameter</param>
        private void SetRendererInfoResponse(XDocument oResponseDocument, ActionState oState)
        {
            try
            {
                List<XElement> Sinks = oResponseDocument.Descendants().Where(e => e.Name.LocalName.ToLower() == "sink").ToList();
                if (Sinks.Count == 0) return;
                string[] Protocols = Sinks[0].Value.Split(new string[] {","}, StringSplitOptions.RemoveEmptyEntries);
                Dictionary<string, List<string>> ProtocolInfoDic = new Dictionary<string, List<string>>();
                foreach (string Protocol in Protocols)
                {
                    string[] Temp = Protocol.Split(new string[] {"*"}, StringSplitOptions.RemoveEmptyEntries);
                    if (Temp.Length > 1)
                    {
                        try
                        {
                            string Info = Temp[1].Substring(1);
                            Info = Info.Split(new string[] { ":" }, StringSplitOptions.None)[0];
                            string[] KeyValue = Info.Split(new string[] { "/" }, StringSplitOptions.None);
                            if (!ProtocolInfoDic.Keys.Contains(KeyValue[0])) ProtocolInfoDic.Add(KeyValue[0], new List<string>());
                            ProtocolInfoDic[KeyValue[0]].Add(KeyValue[1]);
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e.ToString());                            
                        }
                    }
                }
                GlobalVariables.UPnPMediaRenderer.Where(e => e.DeviceName == oState.AdditionalInfo).ToList()[0].Protocoltypes = ProtocolInfoDic;
            }
            finally
            {
                SetRendererInfoResponseReceived = true;
            }
        }
    }
}
