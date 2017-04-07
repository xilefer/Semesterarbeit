using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Media;
using HomeMediaApp.Droid.Classes;
using HomeMediaApp.Droid.Pages;
using HomeMediaApp.Interfaces;
using Xamarin.Forms;

[assembly: Dependency(typeof(ApplicationClose))]
namespace HomeMediaApp.Droid.Classes
{
    public class ApplicationClose : ICloseApplication
    {
        public void Close()
        {
            var Activity = (Android.App.Activity) Forms.Context;
            Activity.FinishAffinity();
        }
    }
}