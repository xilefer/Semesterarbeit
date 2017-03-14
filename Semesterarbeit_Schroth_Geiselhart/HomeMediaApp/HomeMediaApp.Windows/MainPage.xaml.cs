using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace HomeMediaApp.Windows
{
    public sealed partial class MainPage
    {
        public MainPage()
        {
            this.InitializeComponent();
            //Test für die CSSDP-Klasse
            HomeMediaApp.CSSPD oCsspd = new CSSPD();
            //Das ReceivedXML-Event einhängen
            oCsspd.ReceivedXml += OCsspdOnReceivedXml;
            //Den Suchprozess im Netzwerk starten
            oCsspd.StartSearch();
            LoadApplication(new HomeMediaApp.App());

        }

        private void OCsspdOnReceivedXml(XDocument oReceivedXml)
        {
            return;
        }


    }
}
