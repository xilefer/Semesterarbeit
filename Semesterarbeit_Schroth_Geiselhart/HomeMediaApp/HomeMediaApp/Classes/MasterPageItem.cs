﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeMediaApp.Classes
{
    /// <summary>
    /// Klasse für Navigationselemente der MasterDetail-Page
    /// </summary>
    public class MasterPageItem
    {
        public string Title { get; set; } = "";
        public Xamarin.Forms.ImageSource IconSource { get; set; } = "";
        public Type TargetType { get; set; } = null;
    }
}
