using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeMediaApp.Classes;
using Xamarin.Forms;

namespace HomeMediaApp.Pages
{
    public partial class MasterDetailPageHomeMediaApp : MasterDetailPage
    {
        
        public MasterDetailPageHomeMediaApp()
        {
            masterpage = new NavigationDetailPage(this);
            Master = masterpage;
            Detail = new NavigationPage(new MainPage());
            InitializeComponent();
        }
    }
}
