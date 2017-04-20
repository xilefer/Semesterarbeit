﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using HomeMediaApp.Interfaces;
using Xamarin.Forms;
using Xamarin.Forms.Platform.WinRT;
using Xamarin.Forms.Xaml;

[assembly: Dependency(typeof(HomeMediaApp.Windows.Pages.VideoViewer))]
namespace HomeMediaApp.Windows.Pages
{
    public partial class VideoViewer : ContentPage, IVideoViewer
    {
        MediaElement MediaElementControl = new MediaElement();

        public VideoViewer()
        {
            InitializeComponent();
            StackLayoutContent.Children.Clear();
            MediaElementControl.AutoPlay = false;
            StackLayoutContent.Children.Add(MediaElementControl);
            this.Content = MediaElementControl.ToView();
        }

        public void ShowVideoFromUri(Uri FileUri)
        {
            MediaElementControl.Source = FileUri;

        }

        public void ShwoVideoFromPath(string FilePath)
        {
            throw new NotImplementedException();
        }

        public void Play()
        {
            MediaElementControl.Play();
        }
    }
}
