﻿using System;
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
            ImageControl.Aspect = Aspect.AspectFit;
            ImageControl.HorizontalOptions = LayoutOptions.CenterAndExpand;
            ImageControl.VerticalOptions = LayoutOptions.CenterAndExpand;
            ImageControl.Source = ImageSource.FromUri(FileURI);
        }

        public void ShowPhoto(string FilePath)
        {
            ImageControl.Source = ImageSource.FromFile(FilePath);
        }

        private void Button_OnClicked(object sender, EventArgs e)
        {
            Navigation.PopAsync(true);
        }
    }
}
