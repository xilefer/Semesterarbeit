﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeMediaApp.Classes
{
    public class MediaPlayer
    {
        public static PlayerControl Play(MediaObject oMedia, UPnPDevice oDevice)
        {
            PlayerControl Control = new PlayerControl(oDevice, oMedia);
            Control.MediaList.Add(oMedia);
            return Control;
        }
    }
}
