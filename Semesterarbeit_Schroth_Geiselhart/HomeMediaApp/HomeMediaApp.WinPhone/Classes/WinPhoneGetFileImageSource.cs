using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeMediaApp.Interfaces;
using HomeMediaApp.WinPhone.Classes;
using Xamarin.Forms;

[assembly: Dependency(typeof(WinPhoneGetFileImageSource))]
namespace HomeMediaApp.WinPhone.Classes
{
    public class WinPhoneGetFileImageSource : IGetFileImageSource
    {
        public FileImageSource GetPlaySource()
        {
            return (FileImageSource)FileImageSource.FromFile("Assets/play_icon.png");
        }

        public FileImageSource GetPauseSource()
        {
            return (FileImageSource)FileImageSource.FromFile("Assets/pause_icon.png");
        }
    }
}
