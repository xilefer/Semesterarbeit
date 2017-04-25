using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace HomeMediaApp.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ContentPageTemplate : ContentPage
    {
        public ContentPageTemplate()
        {
            InitializeComponent();
        }

        protected override bool OnBackButtonPressed()
        {
            Navigation.PopAsync(true);
            return false;
        }

        protected override void OnAppearing()
        {
            ForceLayout();
        }

        public void SetZOrderOnTop(bool b)
        {
            ContentView oContent = this.Content as ContentView;
        }
    }
}
