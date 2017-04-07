using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeMediaApp.Interfaces;
using HomeMediaApp.Windows.Pages;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: Dependency(typeof(PhotoViewer))]
namespace HomeMediaApp.Windows.Pages
{
    public partial class PhotoViewer : ContentPage, IPhotoViewer
    {
        public PhotoViewer()
        {
            InitializeComponent();
        }

        public void ShowPhotoFromUri(Uri FileURI)
        {
            ImageControl.Aspect = Aspect.Fill;
            ImageControl.Source = ImageSource.FromUri(FileURI);
            //throw new NotImplementedException();
        }

        public void ShowPhoto(string FilePath)
        {
            throw new NotImplementedException();
        }

        private void Button_OnClicked(object sender, EventArgs e)
        {
            
        }
    }
}
