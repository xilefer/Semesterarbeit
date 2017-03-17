using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Collections.ObjectModel;
using System.Threading;
using Xamarin.Forms;
using HomeMediaApp.Pages;

namespace HomeMediaApp
{
    public partial class App : Application
    {
        private CSSPD oDeviceSearcher = new CSSPD();
        private List<XDocument> XMLConfigurations = new List<XDocument>();

        public App()
        {
            InitializeComponent();
            oDeviceSearcher.ReceivedXml += new ReceivedXml(ReceivedXML);
            // Startseite als NavigationPage starten
            MainPage = new MasterDetailPageHomeMediaApp();
            oDeviceSearcher.StartSearch();
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
                if(e.Descendants().Where(b => b.Name.LocalName == "UDN" && b.Value == sUDN).ToList().Count > 0) return true;
                return false;
            });
            if (Result == null || XMLConfigurations.Count == 0)
            {   // Es wurde keine Element mit dem UDN gefunden!
                XMLConfigurations.Add(oReceivedXML);
                ObservableCollection<string> TempItems = (MainPage as MainPage).Items;
                TempItems.Add(oReceivedXML.Descendants().Where(e => e.Name.LocalName.ToLower() == "friendlyname").ToList()[0].Value);
                (MainPage as MainPage).Items = TempItems;
            }
        }

        private void ReceivedXML(XDocument oReceivedXml)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                UpdateXMLConfigs(oReceivedXml);
            });
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
