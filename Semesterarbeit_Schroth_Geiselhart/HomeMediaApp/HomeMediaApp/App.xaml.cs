using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Collections.ObjectModel;
using System.Threading;
using Xamarin.Forms;

namespace HomeMediaApp
{
    public partial class App : Application
    {
        private CSSPD oDeviceSearcher = new CSSPD();

        public App()
        {
            InitializeComponent();
            oDeviceSearcher.ReceivedXml += new ReceivedXml(ReceivedXML);
            MainPage = new HomeMediaApp.MainPage();
            (MainPage as MainPage).Items.Clear();
            oDeviceSearcher.StartSearch();
        }

        private void ReceivedXML(XDocument oReceivedXml)
        {
            HomeMediaApp.MainPage MainPageTemp = MainPage as MainPage;
            ObservableCollection<string> TempItems = MainPageTemp.Items;
            MainPageTemp.Items.Clear();
            TempItems.Add(oReceivedXml.ToString());
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
