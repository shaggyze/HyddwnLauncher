﻿using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HyddwnLauncher.Util
{
    public class LauncherContext
    {
        private readonly string[] _images =
        {
            "bangor.png",
            "damage.png",
            "duncan.png",
            "leehuga.png",
            "lucifer.png",
            "uniqueflyingpuppet.png",
            "vans.png"
        };

        public LauncherContext()
        {
            Settings = new LauncherSettings();
        }


        public ImageSource HostImage
        {
            get
            {
                var rnd = new Random();
                return GetImage(_images[rnd.Next(_images.Length)]);
            }
        }

        public LauncherSettings Settings { get; }

        private ImageSource GetImage(string channel)
        {
            var sri = Application.GetResourceStream(new Uri("pack://application:,,,/Images/" + channel));
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.StreamSource = sri.Stream;
            bmp.EndInit();

            return bmp;
        }

        public void Initialize()
        {
            Settings.Load();
        }
    }
}