using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Android.Graphics;
using Android.Widget;
using HomeMediaApp.Droid.Pages;
using HomeMediaApp.Interfaces;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Xamarin.Forms.Xaml;
using Application = Android.App.Application;

[assembly: Xamarin.Forms.Dependency(typeof(PhotoViewControl))]
namespace HomeMediaApp.Droid.Pages
{

    public partial class PhotoViewControl : ContentPage, IPhotoViewer
    {
        ImageView ImageViewControl = new ImageView(Application.Context);
        public PhotoViewControl()
        {
            InitializeComponent();
            StackLayoutControl.Children.Clear();
            StackLayoutControl.Children.Add(ImageViewControl.ToView());
            this.Content = ImageViewControl.ToView();
            BindingContext = this;
        }

        public void ShowPhotoFromUri(Uri FileURI)
        {
            Bitmap Photo = GetBitmaptFromURI(FileURI);
            ImageViewControl.SetImageBitmap(Photo);
        }

        public void ShowPhoto(string FilePath)
        {

        }

        private Bitmap GetBitmaptFromURI(Uri FileURI)
        {
            Bitmap ImageBitmap = null;
            using (var WebClient = new WebClient())
            {
                var ImageBytes = WebClient.DownloadData(FileURI);
                if (ImageBytes != null && ImageBytes.Length > 0)
                {
                    ImageBitmap = BitmapFactory.DecodeByteArray(ImageBytes.ToArray(), 0, ImageBytes.Length);
                }
            }
            return ImageBitmap;
        }
    }
}
