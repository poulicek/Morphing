using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace Morphing.Core
{
    public class FrameFormat
    {
        public static int ControlGridResolution { get; set; }
        private static int pixelWidth;
        private static int pixelHeight;


        /// <summary>
        /// Vrati nebo nastavi sirku snimku
        /// </summary>
        public int PixelWidth
        {
            get { return pixelWidth; }
            set { pixelWidth = value; }
        }


        /// <summary>
        /// Vrati nebo nastavi vysku snimku
        /// </summary>
        public int PixelHeight
        {
            get { return pixelHeight; }
            set { pixelHeight = value; }
        }

        
        /// <summary>
        /// Vrati nebo nastavi PixelFormat
        /// </summary>
        public PixelFormat PixelFormat { get { return PixelFormats.Pbgra32; } }


        /// <summary>
        /// Vrati nebo nastavi BitmapPallete
        /// </summary>
        public BitmapPalette BitmapPallete { get; set; }


        /// <summary>
        /// Vrati nebo nastavi DpiX
        /// </summary>
        public double DpiX { get; set; }


        /// <summary>
        /// Vrati nebo nastavi DpiY
        /// </summary>
        public double DpiY { get; set; }


        public FrameFormat()
        {
        }


        /// <summary>
        /// Inicializuje objekt vlastnostmi obrazku
        /// </summary>
        /// <param name="bitmap"></param>
        public void Init(BitmapSource bitmap)
        {
            BitmapPallete = bitmap.Palette;
            DpiX = bitmap.DpiX;
            DpiY = bitmap.DpiY;
        }
    }
}
