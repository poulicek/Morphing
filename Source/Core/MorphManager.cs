using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows;
using Morphing.Components;
using System.Windows.Media;
using System.Xml;

namespace Morphing.Core
{
    [Serializable]
    public class MorphManager
    {
        private SortedList<int, Frame> keyFrames = new SortedList<int, Frame>();
        public event EventHandler KeyFramesChanged;

        // Vrstva pro prolnuti obrazku
        private Canvas layerInterleave;
        private Image layerImage;
        private RenderTargetBitmap morphedBitmap;

        #region Vlastnosti

        /// <summary>
        /// Vrati nebo nastavi rozliseni kontrolni mrizky
        /// </summary>
        public int ControlGridResolution
        {
            get { return FrameFormat.ControlGridResolution; }
            set
            {
                if (value > 0)
                {
                    FrameFormat.ControlGridResolution = value;
                    foreach (Frame keyFrame in this.keyFrames.Values)
                    {
                        keyFrame.ApplyBitmapChanges();
                        keyFrame.Grid.SetResolution(FrameFormat.ControlGridResolution, FrameFormat.ControlGridResolution);
                        keyFrame.FreeResources();
                    }
                }
            }
        }


        /// <summary>
        /// Vrati seznam klicovych snimku
        /// </summary>
        public IList<Frame> KeyFrames { get { return (IList<Frame>)keyFrames.Values; } }


        /// <summary>
        /// Vrati nebo nastavi zdrojovy seznam klicovych snimku (nedoporucuje se pouzivat)
        /// </summary>
        public Frame[] RawKeyFramesArray
        {
            get
            {
                Frame[] result = new Frame[keyFrames.Count];
                keyFrames.Values.CopyTo(result, 0);
                return result;
            }
            set
            {
                keyFrames.Clear();
                foreach (Frame frame in value)
                {
                    frame.ApplyWarping();
                    keyFrames.Add(frame.Index, frame);
                }

                if (KeyFramesChanged != null)
                    KeyFramesChanged(this, EventArgs.Empty);

                morphedBitmap = null;
            }
        }

        #endregion Vlastnosti

        public MorphManager()
        {
            FrameFormat.ControlGridResolution = 16;
            keyFrames.Add(0, new Frame(0));

            layerImage = new Image();
            layerImage.Stretch = Stretch.Fill;
            layerImage.HorizontalAlignment = HorizontalAlignment.Stretch;
            layerImage.VerticalAlignment = VerticalAlignment.Stretch;
            layerInterleave = new Canvas();
            layerInterleave.Children.Add(layerImage);
        }


        #region Vytvareni snimku

        /// <summary>
        /// Vrati pozadovany snimek
        /// </summary>
        /// <param name="index">index snimku</param>
        /// <returns>snimek</returns>
        public Frame GetFrame(int index)
        {
            if (keyFrames.ContainsKey(index))
            {
                keyFrames[index].ApplyWarping();
                return keyFrames[index];
            }

            foreach (int key in keyFrames.Keys)
                if (key > index)
                    return getMorphedFrame(index, keyFrames[keyFrames.Keys[keyFrames.IndexOfKey(key) - 1]], keyFrames[key]);

            return new Frame(index);
        }


        /// <summary>
        /// Ziska morphovany snimek
        /// </summary>
        /// <param name="index">Index pozadovaneho snimku</param>
        /// <param name="startKeyFrame">Prvni klicovy snimek</param>
        /// <param name="endKeyFrame">Druhy klicovy snimek</param>
        /// <returns>Morphovany snimek</returns>
        private Frame getMorphedFrame(int index, Frame startKeyFrame, Frame endKeyFrame)
        {
            // inicializace vrstev pro modifikaci
            if (morphedBitmap == null)
                morphedBitmap = new RenderTargetBitmap(endKeyFrame.Format.PixelWidth, endKeyFrame.Format.PixelHeight, 96, 96, endKeyFrame.WarpedBitmap.Format);

            double ratio = (double)(index - startKeyFrame.Index) / (endKeyFrame.Index - startKeyFrame.Index);


            // Vytvoreni noveho snimku se ziskanou bitmapou
            Frame morphedFrame = new Frame(index);
            interpolateGrid(morphedFrame, startKeyFrame, endKeyFrame, 1 - ratio);
            startKeyFrame.ApplyWarping(morphedFrame.Grid.Nodes);
            endKeyFrame.ApplyWarping(morphedFrame.Grid.Nodes);

            // Prolnuti dvou obrazku
            layerImage.Source = endKeyFrame.WarpedBitmap;
            layerImage.Opacity = ratio;
            layerInterleave.Background = startKeyFrame.WarpedBitmap != null ? (Brush)new ImageBrush(startKeyFrame.WarpedBitmap) : Brushes.White;
            layerInterleave.Arrange(new System.Windows.Rect(0, 0, endKeyFrame.Format.PixelWidth, endKeyFrame.Format.PixelHeight));


            morphedBitmap.Render(layerInterleave); // Ulozeni prolnutych snimku do bitmapy
            morphedFrame.SourceBitmap = morphedBitmap;
            //morphedFrame.ApplyWarping();
            return morphedFrame;
        }


        /// <summary>
        /// Interpoluje mrizky vybraneho snimku
        /// </summary>
        /// <param name="actualFrame">Upravovany snimek</param>
        /// <param name="startKeyFrame">Prvni klicovy snimek</param>
        /// <param name="endKeyFrame">Druhy klicovy snimek</param>
        private void interpolateGrid(Frame targetFrame, Frame startKeyFrame, Frame endKeyFrame, double ratio)
        {
            // Interpolace offsetu v horizontalnim smeru
            for (int row = 0; row < startKeyFrame.Grid.Rows; ++row)
                for (int col = 0; col < startKeyFrame.Grid.Columns; ++col)
                    targetFrame.Grid.Nodes[row, col].X = endKeyFrame.Grid.Nodes[row, col].X + (startKeyFrame.Grid.Nodes[row, col].X - endKeyFrame.Grid.Nodes[row, col].X) * ratio;

            // Interpolace offsetu ve vertikalnim smeru
            for (int col = 0; col < startKeyFrame.Grid.Columns; ++col)
                for (int row = 0; row < startKeyFrame.Grid.Rows; ++row)
                    targetFrame.Grid.Nodes[row, col].Y = endKeyFrame.Grid.Nodes[row, col].Y + (startKeyFrame.Grid.Nodes[row, col].Y - endKeyFrame.Grid.Nodes[row, col].Y) * ratio;
            
            targetFrame.Grid.RefNodes = (Point[,])targetFrame.Grid.Nodes.Clone();
        }


        /// <summary>
        /// Prevzorkuje klicove snimky na dany rozmer
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void ResampleKeyFrames(int width, int height)
        {
            // prevzorkovani vsech klicovych snimku
            foreach (Frame frame in keyFrames.Values)
            {
                frame.Format.PixelWidth = width;
                frame.Format.PixelHeight = height;

                if (frame.WarpedBitmap == null)
                    continue;

                // zmena velikosti obrazku
                layerImage.Opacity = 1;
                layerImage.Source = frame.WarpedBitmap;
                layerImage.Arrange(new Rect(0, 0, width, height));

                // ulozeni zmen
                morphedBitmap = new RenderTargetBitmap(width, height, frame.WarpedBitmap.DpiX, frame.WarpedBitmap.DpiY, frame.WarpedBitmap.Format);
                morphedBitmap.Render(layerImage);
                frame.SourceBitmap = morphedBitmap;
            }
        }

        #endregion

        #region Ovladani klicovych snimku

        /// <summary>
        /// Nastavi zvoleny snimek jako klicovy
        /// </summary>
        /// <param name="frame">Snimek</param>
        public void AddKeyFrame(Frame frame)
        {
            if (!keyFrames.ContainsKey(frame.Index))
                SetKeyFrameIndex(frame, frame.Index);

            if (KeyFramesChanged != null)
                KeyFramesChanged(this, EventArgs.Empty);
        }


        /// <summary>
        /// Odstrani zvoleny snimek ze seznamu klicovych
        /// </summary>
        /// <param name="frame">Snimek</param>
        public void RemoveKeyFrame(Frame frame)
        {
            if (frame.Index > 0 && keyFrames.ContainsKey(frame.Index))
                keyFrames.Remove(frame.Index);

            if (KeyFramesChanged != null)
                KeyFramesChanged(this, EventArgs.Empty);
        }



        /// <summary>
        /// Odstrani vsechny klicove snimky
        /// </summary>
        public void RemoveAllFrames()
        {
            keyFrames.Clear();
            keyFrames.Add(0, new Frame(0));

            if (KeyFramesChanged != null)
                KeyFramesChanged(this, EventArgs.Empty);
        }


        /// <summary>
        /// Presune dany klicovy snimek na index
        /// </summary>
        /// <param name="frame">Klicovy snimek</param>
        /// <param name="index">Index, na ktery se ma presunout</param>
        /// <returns>Uspesnost operace</returns>
        public void SetKeyFrameIndex(Frame frame, int index)
        {
            if (frame.Index == 0 || index == 0 || keyFrames.ContainsKey(index))
                return;

            if (keyFrames.ContainsKey(frame.Index))
                keyFrames.Remove(frame.Index);

            frame.Index = index;
            keyFrames.Add(index, frame);

            if (KeyFramesChanged != null)
                KeyFramesChanged(this, EventArgs.Empty);
        }


        /// <summary>
        /// Vrati, zda je snimek na danem indexu klicovy
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool KeyFrameExists(int index)
        {
            return keyFrames.Keys.Contains(index);
        }

        #endregion
    }
}
