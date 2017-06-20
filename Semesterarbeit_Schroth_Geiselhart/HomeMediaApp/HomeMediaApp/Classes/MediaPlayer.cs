using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeMediaApp.Classes
{
    /// <summary>
    /// Klasse zum zentralen Ansteuern eines Netzwerkwiedergabegeräts
    /// </summary>
    public class MediaPlayer
    {
        public static PlayerControl Play(MediaObject oMedia, UPnPDevice oDevice)
        {
            PlayerControl Control = new PlayerControl(oDevice, oMedia);
            return Control;
        }
    }
}
