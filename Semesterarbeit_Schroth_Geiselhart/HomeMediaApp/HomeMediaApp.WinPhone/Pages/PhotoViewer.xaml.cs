using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeMediaApp.Interfaces;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: Dependency(typeof(HomeMediaApp.WinPhone.Pages.PhotoViewer))]
namespace HomeMediaApp.WinPhone.Pages
{
    public partial class PhotoViewer : ContentPage, IPhotoViewer
    {
        public PhotoViewer()
        {
            InitializeComponent();
        }

        public void ShowPhotoFromUri(Uri FileURI)
        {
            ImageControl.Aspect = Aspect.AspectFit;
            ImageControl.Source = ImageSource.FromUri(FileURI);
        }

        public void ShowPhoto(string FilePath)
        {
            throw new NotImplementedException();
        }

        protected override bool OnBackButtonPressed()
        {
            Navigation.PopAsync(true);
            return true;
        }
    }
}
