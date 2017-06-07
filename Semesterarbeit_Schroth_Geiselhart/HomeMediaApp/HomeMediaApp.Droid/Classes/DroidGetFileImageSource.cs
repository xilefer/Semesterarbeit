using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using HomeMediaApp.Droid.Classes;
using HomeMediaApp.Interfaces;
using Xamarin.Forms;

[assembly: Dependency(typeof(DroidGetFileImageSource))]
namespace HomeMediaApp.Droid.Classes
{
    public class DroidGetFileImageSource : IGetFileImageSource
    {
        public FileImageSource GetPlaySource()
        {
            FileImageSource Temp = (FileImageSource) FileImageSource.FromFile("play_icon.png");
            return Temp;
        }

        public FileImageSource GetPauseSource()
        {
            return (FileImageSource)FileImageSource.FromFile("Resources/drawable/pause_icon.png");
        }
    }
}