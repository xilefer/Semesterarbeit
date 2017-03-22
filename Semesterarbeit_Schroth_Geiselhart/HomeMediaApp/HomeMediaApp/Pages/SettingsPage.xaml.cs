using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace HomeMediaApp.Pages
{
    public partial class SettingsPage : ContentPage
    {
        private string sMainText = "";

        public string MainText
        {
            get { return sMainText; }
            set
            {
                if (sMainText == value) return;
                sMainText = value;
                OnPropertyChanged();
            }
        }

        public SettingsPage()
        {
            InitializeComponent();
            MainText = "Einstellungen";
        }
    }
}
