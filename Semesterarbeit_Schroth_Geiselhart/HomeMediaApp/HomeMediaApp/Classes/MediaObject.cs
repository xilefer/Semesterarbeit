﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeMediaApp.Classes
{
    /// <summary>
    /// Klasse für die Wiedergabe eines beliebigen UPnP-Objekts über das Netzwerk
    /// </summary>
    public class MediaObject
    {
        public string Path { get; set; } = "";
        public int Index { get; set; }
        public string MetaData { get; set; } = "";

    }
}
