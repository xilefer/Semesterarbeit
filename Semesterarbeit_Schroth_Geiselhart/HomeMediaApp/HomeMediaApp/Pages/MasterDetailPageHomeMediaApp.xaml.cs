using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeMediaApp.Classes;
using HomeMediaApp.Interfaces;
using Xamarin.Forms;

namespace HomeMediaApp.Pages
{
    public partial class MasterDetailPageHomeMediaApp : MasterDetailPage
    {
        
        public MasterDetailPageHomeMediaApp()
        {
            masterpage = new NavigationDetailPage(this);
            Master = masterpage;
            Detail = new MainPage();
            InitializeComponent();
        }

        //protected override bool OnBackButtonPressed()
        //{
        //    CloseApplication();
        //    return true;
        //    //return base.OnBackButtonPressed();
        //}

        private async void CloseApplication()
        {
            bool bReturn = await DisplayAlert("", "Wollen sie die Applikation schließen?", "Ja", "Nein");
            if (bReturn)
            {
                DependencyService.Get<ICloseApplication>()?.Close();
            }
        }
    }
}
