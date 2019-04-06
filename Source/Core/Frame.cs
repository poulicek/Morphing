using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace Morphing.Core
{
    public class Frame
    {
        private int stride;
        private byte[] sourceBitmapData;
        private byte[] resultBitmapData;
        private WriteableBitmap warpedBitmap;
        private Grid grid;


        /// <summary>
        /// Vrati nebo nastavi format snimku
        /// </summary>
        public FrameFormat Format { get; set; }

        
        /// <summary>
        /// Index snimku
        /// </summary>
        public int Index { get; set; }


        /// <summary>
        /// Vrati deformovanou bitmapu
        /// </summary>
        public BitmapSource WarpedBitmap
        {
            get { return warpedBitmap; }
        }


        [XmlIgnore]
        /// <summary>
        /// Vrati nebo nastavi zdrojovou bitmapu obrazku
        /// </summary>
        public BitmapSource SourceBitmap
        {
            get
            {
                if (sourceBitmapData == null || warpedBitmap == null)
                    return warpedBitmap;

                return BitmapSource.Create(warpedBitmap.PixelWidth, warpedBitmap.PixelHeight, warpedBitmap.DpiX, warpedBitmap.DpiY, warpedBitmap.Format, warpedBitmap.Palette, sourceBitmapData, stride);
            }
            set
            {
                FreeResources();
                sourceBitmapData = null;
                Format.Init(value);
                warpedBitmap = new WriteableBitmap(value);
                stride = Format.PixelWidth * warpedBitmap.Format.BitsPerPixel / 8;
                grid.SetProportions(Format.PixelWidth, Format.PixelHeight);
            }
        }


        /// <summary>
        /// Vrati nebo nastavi data zdrojove bitmapy
        /// </summary>
        public byte[] RawSourceBitmapData
        {
            get
            {
                if (warpedBitmap != null && sourceBitmapData == null)
                {
                    sourceBitmapData = new byte[stride * warpedBitmap.PixelHeight];
                    warpedBitmap.CopyPixels(sourceBitmapData, stride, 0);
                }
                return sourceBitmapData;
            }
            set
            {
                sourceBitmapData = value;
                stride = value.Length / Format.PixelHeight;
                warpedBitmap = new WriteableBitmap(BitmapSource.Create(Format.PixelWidth, Format.PixelHeight, Format.DpiX, Format.DpiY, Format.PixelFormat, Format.BitmapPallete, sourceBitmapData, stride));
                grid.SetProportions(Format.PixelWidth, Format.PixelHeight);
            }
        }


        /// <summary>
        /// Vrati objekt deformacni mrizky (v pripade prirazeni priradi jen vrcholy stavajici mrizce)
        /// </summary>
        public Grid Grid
        {
            get { return grid; }
            set
            {
                grid.Nodes = value.Nodes;
                grid.RefNodes = value.RefNodes;
            }
        }


        public Frame()
        {
            Format = new FrameFormat();
            grid = new Grid(FrameFormat.ControlGridResolution, FrameFormat.ControlGridResolution);
        }

        public Frame(int index)
            : this()
        {
            Index = index;
        }

        #region Warping

        /// <summary>
        /// Aplikuje deformace mrizky na celou bitmapu
        /// </summary>
        public void ApplyWarping()
        {
            FreeResources();
            ApplyWarping(grid.Nodes, 0, 0, grid.Rows - 1, grid.Columns - 1);
        }


        /// <summary>
        /// Aplikuje deformace mrizky na celou bitmapu
        /// </summary>
        public void ApplyWarping(Point[,] nodes)
        {
            FreeResources();
            ApplyWarping(nodes, 0, 0, grid.Rows - 1, grid.Columns - 1);
            FreeResources();
        }


        /// <summary>
        /// Aplikuje deformace mrizky na bitmapu
        /// </summary>
        public void ApplyWarping(Point[,] nodes, int row1, int col1, int row2, int col2)
        {
            if (warpedBitmap == null)
                return;

            // Inicializace datovych zdroju
            if (sourceBitmapData == null)
            {
                sourceBitmapData = new byte[stride * warpedBitmap.PixelHeight];
                warpedBitmap.CopyPixels(sourceBitmapData, stride, 0);
            }

            if (resultBitmapData == null)
                resultBitmapData = (byte[])sourceBitmapData.Clone();

            // Ziskani mapy offsetu
            Point[,] offsetMap = grid.GetOffsetMap(nodes, row1, col1, row2, col2);

            // Inicializace transformacniho okna
            int x1 = (int)Math.Round(col1 * grid.ColStep);
            int y1 = (int)Math.Round(row1 * grid.RowStep);
            int x2 = (int)Math.Round(col2 * grid.ColStep);
            int y2 = (int)Math.Round(row2 * grid.RowStep);
            
            for (int row = y1; row < y2; ++row)
            {
                for (int col = x1; col < x2; ++col)
                {
                    double xOffset = offsetMap[row, col].X;
                    double yOffset = offsetMap[row, col].Y;

                    if ((xOffset != 0 || yOffset != 0) && row + yOffset >= 0 && row + yOffset < y2 && col + xOffset >= 0 && col + xOffset < x2)
                    {
                        int resIndex = row * stride + col * 4;
                        int srcIndex = (int)Math.Round((row + yOffset) * stride + (col + xOffset) * 4);

                        resultBitmapData[resIndex] = sourceBitmapData[srcIndex];
                        resultBitmapData[resIndex + 1] = sourceBitmapData[srcIndex + 1];
                        resultBitmapData[resIndex + 2] = sourceBitmapData[srcIndex + 2];
                        resultBitmapData[resIndex + 3] = sourceBitmapData[srcIndex + 3];
                    }
                }
            }
            warpedBitmap.WritePixels(new System.Windows.Int32Rect(0, 0, warpedBitmap.PixelWidth, warpedBitmap.PixelHeight), resultBitmapData, stride, 0);
        }

        #endregion


        /// <summary>
        /// Nastaví aktuální bitmapu jako zdrojovou
        /// </summary>
        public void ApplyBitmapChanges()
        {
            if (warpedBitmap != null)
            {
                FreeResources();
                sourceBitmapData = null;
                grid.Reset();
            }
        }


        /// <summary>
        /// Uvolni prostredky
        /// </summary>
        public void FreeResources()
        {
            resultBitmapData = null;
            if (grid != null)
                grid.FreeResources();
        }
    }
}
