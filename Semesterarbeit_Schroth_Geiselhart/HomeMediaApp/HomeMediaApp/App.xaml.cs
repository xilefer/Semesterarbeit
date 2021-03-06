﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using HomeMediaApp.Classes;
using Xamarin.Forms;
using HomeMediaApp.Pages;

namespace HomeMediaApp
{
    public partial class App : Application
    {
        private List<XDocument> XMLConfigurations = new List<XDocument>();

        public App()
        {
            InitializeComponent();
            // Startseite als NavigationPage starten
            try
            {
                GlobalVariables.GlobalRemoteMediaPlayerPage = new RemoteMediaPlayerPage();
                MainPage = new MasterDetailPageHomeMediaApp();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
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
