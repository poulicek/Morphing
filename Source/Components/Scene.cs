using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Serialization;
using NGif;
using Morphing.Core;
using VeNET.Objects;
using System.Windows.Threading;

namespace Morphing.Components
{
    public class Scene : Control
    {
        // Parametry sceny
        public enum SceneMode { Warping, GridControl, Drawing }
        private SceneMode mode;
        private double zoom = 1;

        // Ovladace
        public event EventHandler SelectedFrameChanged;
        private MorphManager morphManager = new MorphManager();
        private Morphing.Core.Frame selectedFrame;
        private DrawingManager drawingManager;

        // Ovladani deformacni mrizky
        private Canvas layerImage;
        private Canvas layerGrid;
        private Canvas layerNodes;
        private Ellipse selectedNode;
        private bool nodesModified;
        private double nodeCenter;

        #region Vlastnosti

        /// <summary>
        /// Vrati nebo nastavi mod sceny
        /// </summary>
        public SceneMode Mode
        {
            get { return mode; }
            set
            {
                if (mode == value)
                    return;

                mode = value;
                if (value == SceneMode.Warping || value == SceneMode.GridControl)
                {
                    // Nastaveni drawing managera
                    if (drawingManager != null)
                    {
                        drawingManager.Mode = DrawMode.Select;
                        mergeWithBackground();
                    }
                    layerGrid.Visibility = Visibility.Visible;
                    redraw();
                }
                else
                {
                    // Vytvoreni drawing managera
                    if (drawingManager == null)
                    {
                        Entity.ActualFill = Brushes.White;
                        Entity.ActualStroke = Brushes.Black;

                        drawingManager = new DrawingManager(layerImage);
                        drawingManager.Document = new Document(String.Empty);
                    }
                    layerGrid.Visibility = Visibility.Hidden;
                }
                selectedNode = null;
            }
        }


        /// <summary>
        /// Mod kresleni
        /// </summary>
        public DrawMode DrawingMode
        {
            set { drawingManager.Mode = value; }
        }

        
        /// <summary>
        /// Vrati nebo nastavi aktualni zoom sceny
        /// </summary>
        public double Zoom
        {
            get { return zoom; }
            set
            {
                if (layerImage != null && zoom != value && zoom > 0)
                {
                    zoom = value;
                    ((ScaleTransform)layerImage.RenderTransform).ScaleX = ((ScaleTransform)layerImage.RenderTransform).ScaleY = value;
                    this.Width = zoom * layerImage.ActualWidth;
                    this.Height = zoom * layerImage.ActualHeight;
                    if (mode == SceneMode.Warping || mode == SceneMode.GridControl)
                        refreshGrid();
                }
            }
        }


        /// <summary>
        /// Objekt pro praci se snimky
        /// </summary>
        public MorphManager MorphManager
        {
            get { return morphManager; }
        }


        /// <summary>
        /// Vrati nebo nastavi aktualni snimek
        /// </summary>
        public Morphing.Core.Frame SelectedFrame
        {
            get { return selectedFrame; }
        }


        /// <summary>
        /// Vrati nebo nastavi index vybraneho snimku
        /// </summary>
        public int SelectedFrameIndex
        {
            get { return selectedFrame.Index; }
            set
            {
                if (value < 0)
                    return;

                // Ulozeni editace obrazku
                mergeWithBackground();

                // Uvolneni prostredku aktualniho snimku
                selectedFrame.FreeResources();

                // Nacteni pozadovaneho snimku
                selectedFrame = morphManager.GetFrame(value);
                if (SelectedFrameChanged != null)
                    SelectedFrameChanged(this, EventArgs.Empty);

                redraw();
            }
        }


        /// <summary>
        /// Vrati nebo nastavi rozliseni kontrolni mrizky
        /// </summary>
        public int ControlGridResolution
        {
            get { return morphManager.ControlGridResolution; }
            set
            {
                if (value != morphManager.ControlGridResolution && value > 0 && layerGrid != null)
                {
                    morphManager.ControlGridResolution = value;
                    if (!morphManager.KeyFrames.Contains(selectedFrame))
                    {
                        selectedFrame.ApplyBitmapChanges();
                        selectedFrame.Grid.SetResolution(value, value);
                        selectedFrame.FreeResources();
                    }
                    if (mode == SceneMode.Warping || mode == SceneMode.GridControl)
                        drawGrid();
                }
            }
        }

        #endregion

        public Scene()
        {
            selectedFrame = morphManager.GetFrame(0);
        }


        #region Inicializace

        /// <summary>
        /// Inicializace po nacteni sablony prvku
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            layerGrid = (Canvas)this.Template.FindName("layerGrid", this);
            layerNodes = (Canvas)this.Template.FindName("layerNodes", this);
            layerImage = (Canvas)this.Template.FindName("layerImage", this);
            layerImage.RenderTransform = new ScaleTransform();
            nodeCenter = ((double)((Setter)((Style)FindResource("Node")).Setters[0]).Value) / 2;

            morphManager.KeyFramesChanged += delegate(object sender, EventArgs e) {
                if (!morphManager.KeyFrames.Contains(selectedFrame))
                    SelectedFrameIndex = selectedFrame.Index;
            };

            selectedFrame.Format.PixelWidth = (int)this.Width;
            selectedFrame.Format.PixelHeight = (int)this.Height;
            redraw();
        }

        #endregion

        #region Vykreslovani

        /// <summary>
        /// Inicializuje scenu do puvodni podoby
        /// </summary>
        public void Reset()
        {
            morphManager.RemoveAllFrames();
            clearDrawing();
            selectedFrame = morphManager.GetFrame(0);            

            if (SelectedFrameChanged != null)
                SelectedFrameChanged(this, EventArgs.Empty);
        }


        /// <summary>
        /// Rendering sceny (obrazek + mrizka)
        /// </summary>
        private void redraw()
        {
            reloadFrame();

            if (mode == SceneMode.Warping || mode == SceneMode.GridControl)
            {
                layerGrid.Visibility = selectedFrame.WarpedBitmap != null ? Visibility.Visible : Visibility.Hidden;
                if (selectedFrame.WarpedBitmap != null)
                    refreshGrid();
            }
        }


        /// <summary>
        /// Aktualizuje snimek
        /// </summary>
        private void reloadFrame()
        {
            this.Width = zoom * (layerImage.Width = selectedFrame.Format.PixelWidth);
            this.Height = zoom * (layerImage.Height = selectedFrame.Format.PixelHeight);
            layerImage.Background = selectedFrame.WarpedBitmap != null ? (Brush)new ImageBrush(selectedFrame.WarpedBitmap) : Brushes.White;
        }


        /// <summary>
        /// Spoji nakreslene objekty s pozadim
        /// </summary>
        private void mergeWithBackground()
        {
            if (drawingManager != null && drawingManager.Document.Entities.Count > 0)
            {
                // Upraveny snimek se oznaci jako klicovy
                if (!morphManager.KeyFrames.Contains(selectedFrame))
                    morphManager.AddKeyFrame(selectedFrame);

                // Slouceni objektu s pozadim
                drawingManager.Selection.UnselectAll();
                ((ScaleTransform)layerImage.RenderTransform).ScaleX = ((ScaleTransform)layerImage.RenderTransform).ScaleY = 1;
                RenderTargetBitmap bitmap = new RenderTargetBitmap((int)layerImage.Width, (int)layerImage.Height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                bitmap.Render(layerImage);
                ((ScaleTransform)layerImage.RenderTransform).ScaleX = ((ScaleTransform)layerImage.RenderTransform).ScaleY = zoom;
                selectedFrame.SourceBitmap = bitmap;
                selectedFrame.Grid.Reset();

                // Smazani kresby
                clearDrawing();
            }
        }


        /// <summary>
        /// Obnovi topologii mrizky
        /// </summary>
        private void refreshGrid()
        {
            if (layerNodes.Children.Count == 0)
                drawGrid();
            else
            {
                int nodeIndex = 0;
                double pixelRow = 0, pixelCol = 0;
                Point position = new Point();
                Style nodeStyle = mode == SceneMode.GridControl ? (Style)FindResource("ControlNode") : (Style)FindResource("Node");

                for (int row = 0; row < selectedFrame.Grid.Rows; ++row)
                {
                    pixelCol = 0;
                    for (int col = 0; col < selectedFrame.Grid.Columns; ++col)
                    {
                        position.X = zoom * (pixelCol + selectedFrame.Grid.Nodes[row, col].X);
                        position.Y = zoom * (pixelRow + selectedFrame.Grid.Nodes[row, col].Y);

                        // Deformace krivek mrizky
                        ((Polyline)layerGrid.Children[col]).Points[row] = position;
                        ((Polyline)layerGrid.Children[row + selectedFrame.Grid.Columns]).Points[col] = position;

                        Ellipse node = (Ellipse)layerNodes.Children[nodeIndex];
                        Canvas.SetLeft(node, position.X - nodeCenter);
                        Canvas.SetTop(node, position.Y - nodeCenter);
                        node.Style = nodeStyle;

                        ++nodeIndex;
                        pixelCol += selectedFrame.Grid.ColStep;
                    }
                    pixelRow += selectedFrame.Grid.RowStep;
                }
            }
        }


        /// <summary>
        /// Vykresli warpovaci mrizku
        /// </summary>
        private void drawGrid()
        {
            // Smazani mrizky
            layerNodes.Children.Clear();
            layerGrid.Children.Clear();
            Style nodeStyle = mode == SceneMode.GridControl ? (Style)FindResource("ControlNode") : (Style)FindResource("Node");

            Point position = new Point();
            double pixelRow = 0, pixelCol = 0;
            for (int row = 0; row < selectedFrame.Grid.Rows; ++row)
            {
                // Vytvoreni horizontalni linky (jedna pro kazdy radek)
                Polyline hLine = new Polyline();
                pixelCol = 0;
                for (int col = 0; col < selectedFrame.Grid.Columns; ++col)
                {
                    if (row == 0)
                        layerGrid.Children.Add(new Polyline());

                    // Identifikace pozice uzlu vuci scene
                    position.X = zoom * (pixelCol + selectedFrame.Grid.Nodes[row, col].X);
                    position.Y = zoom * (pixelRow + selectedFrame.Grid.Nodes[row, col].Y);

                    // Vlozeni vrcholu do vrstvy krivek
                    hLine.Points.Add(position);
                    ((Polyline)layerGrid.Children[col]).Points.Add(position);

                    // Vytvoreni uzlu
                    Ellipse node = new Ellipse();
                    node.Style = nodeStyle;
                    node.MouseEnter += delegate(object sender, MouseEventArgs e) { if (selectedNode == null) node.BeginStoryboard((System.Windows.Media.Animation.Storyboard)FindResource("NodeFadeIn")); };
                    node.MouseLeave += delegate(object sender, MouseEventArgs e) { if (selectedNode == null) node.BeginStoryboard((System.Windows.Media.Animation.Storyboard)FindResource("NodeFadeOut")); };
                    node.PreviewMouseDown += delegate(object sender, MouseButtonEventArgs e) { e.Handled = true; selectedNode = (Ellipse)sender; };
                    node.Visibility = row > 0 && row < selectedFrame.Grid.Rows - 1 && col > 0 && col < selectedFrame.Grid.Columns - 1 ? Visibility.Visible : Visibility.Hidden;
                    Canvas.SetLeft(node, position.X - nodeCenter);
                    Canvas.SetTop(node, position.Y - nodeCenter);
                    layerNodes.Children.Add(node);
                    
                    pixelCol += selectedFrame.Grid.ColStep;
                }
                pixelRow += selectedFrame.Grid.RowStep;
                layerGrid.Children.Add(hLine);
            }
            layerGrid.Children.Add(layerNodes);
        }


        /// <summary>
        /// Normalizuje pozici mysi vuci velikosti obrazku
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private Point getMousePosition(MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(layerNodes);

            if (mousePosition.X < 0)
                mousePosition.X = 0;
            else if (mousePosition.X >= this.Width)
                mousePosition.X = this.Width - 1;

            if (mousePosition.Y < 0)
                mousePosition.Y = 0;
            else if (mousePosition.Y >= this.Height)
                mousePosition.Y = this.Height - 1;

            return mousePosition;
        }


        /// <summary>
        /// Vymaze scenu
        /// </summary>
        private void clearDrawing()
        {
            if (drawingManager != null)
            {
                layerImage.Children.Clear();
                drawingManager.Selection.UnselectAll();
                drawingManager.Document.Entities.Clear();
            }
        }


        #endregion

        #region Uzivatelska funkcnost

        /// <summary>
        /// Zjisti, zda mohou byt objekty slouceny s pozadim
        /// </summary>
        /// <returns></returns>
        public bool CanMergeWidthBackground()
        {
            return drawingManager == null || drawingManager.Document.Entities.Count == 0 || (new Dialogs.CanMerge()).ShowDialog() == true;
        }


        /// <summary>
        /// Nastavi rozliseni scene a prezvorkuje klicove snimky
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void SetResolution(int width, int height)
        {
            mergeWithBackground();
            morphManager.ResampleKeyFrames(width, height);
            selectedFrame.ApplyWarping();
            redraw();
        }


        /// <summary>
        /// Vlozi obrazek do sceny
        /// </summary>
        /// <param name="adaptScene"></param>
        public void InsertImage(string path, bool adaptScene)
        {
            // Nahrani bitmapy do skryte vrstvy
            BitmapImage bitmap = new BitmapImage(new Uri(path));
            Image insertedImage = new Image();
            insertedImage.Stretch = Stretch.Fill;
            insertedImage.Width = adaptScene ? bitmap.PixelWidth : this.Width / zoom;
            insertedImage.Height = adaptScene ? bitmap.PixelHeight : this.Height / zoom;
            insertedImage.Source = bitmap;
            insertedImage.Arrange(new Rect(0, 0, insertedImage.Width, insertedImage.Height));

            // Render bitmapy
            RenderTargetBitmap targetBitmap = new RenderTargetBitmap((int)insertedImage.Width, (int)insertedImage.Height, 96, 96, PixelFormats.Pbgra32);
            targetBitmap.Render(insertedImage);

            if (adaptScene)
                SetResolution((int)insertedImage.Width, (int)insertedImage.Height);

            // Upraveny snimek se oznaci jako klicovy
            if (!morphManager.KeyFrames.Contains(selectedFrame))
                morphManager.AddKeyFrame(selectedFrame);
            selectedFrame.SourceBitmap = targetBitmap;
            selectedFrame.Grid.Reset();          
            
            redraw();
        }


        /// <summary>
        /// Vymaze vybrane vektorove entity
        /// </summary>
        public void DeleteSelectedEntities()
        {
            if (drawingManager != null)
                drawingManager.DeleteSelectedEntities();
        }

        #endregion

        #region Udalosti mysi

        /// <summary>
        /// Tahnuti vrcholem
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed && selectedNode != null)
            {
                nodesModified = true;

                // Premisteni uzlu
                Point mousePosition = getMousePosition(e);
                Canvas.SetLeft(selectedNode, mousePosition.X - nodeCenter);
                Canvas.SetTop(selectedNode, mousePosition.Y - nodeCenter);

                // Identifikace pozice uzlu v mrizce
                int index = layerNodes.Children.IndexOf(selectedNode);
                int col = index % selectedFrame.Grid.Columns;
                int row = index / selectedFrame.Grid.Columns;

                // Deformace krivek mrizky
                ((Polyline)layerGrid.Children[col]).Points[row] = mousePosition;
                ((Polyline)layerGrid.Children[row + selectedFrame.Grid.Columns]).Points[col] = mousePosition;
                
                // Vypocet a nastaveni odchylek
                selectedFrame.Grid.Nodes[row, col].X = mousePosition.X / zoom - (col * selectedFrame.Grid.ColStep);
                selectedFrame.Grid.Nodes[row, col].Y = mousePosition.Y / zoom - (row * selectedFrame.Grid.RowStep);

                if (mode == SceneMode.Warping)
                {
                    // Aplikace provedenych zmen
                    selectedFrame.ApplyWarping(selectedFrame.Grid.Nodes, row - 1, col - 1, row + 1, col + 1);
                    reloadFrame();
                }
            }
        }


        /// <summary>
        /// Potvrzeni pozice vrcholu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            if (selectedNode != null)
            {
                if (nodesModified)
                {
                    // Upraveny snimek se oznaci jako klicovy
                    if (!morphManager.KeyFrames.Contains(selectedFrame))
                        morphManager.AddKeyFrame(selectedFrame);

                    // Provedeni zmen
                    if (mode == SceneMode.Warping)
                    {
                        selectedFrame.ApplyWarping();
                        reloadFrame();
                    }
                    else if (mode == SceneMode.GridControl)
                    {
                        int index = layerNodes.Children.IndexOf(selectedNode);
                        int col = index % selectedFrame.Grid.Columns;
                        int row = index / selectedFrame.Grid.Columns;

                        selectedFrame.Grid.RefNodes[row, col].X = selectedFrame.Grid.Nodes[row, col].X;
                        selectedFrame.Grid.RefNodes[row, col].Y = selectedFrame.Grid.Nodes[row, col].Y;
                    }
                }

                nodesModified = false;
                selectedNode = null;
            }
        }

        #endregion

        #region Ukladani / nahravani

        /// <summary>
        /// Ulozi scenu v nativnim format
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            // serializace a komprese sceny
            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                using (GZipStream gzStream = new GZipStream(fs, CompressionMode.Compress))
                {
                    (new XmlSerializer(typeof(MorphManager))).Serialize(gzStream, morphManager);
                }
            }
        }


        /// <summary>
        /// Ulozi aktualni snimek do obrazku
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path, BitmapEncoder encoder)
        {
            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                encoder.Frames.Add(BitmapFrame.Create(selectedFrame.WarpedBitmap));
                encoder.Save(stream);
            }
        }


        /// <summary>
        /// Ulozi obrazek ve formatu GIF
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="delay"></param>
        /// <param name="repeat"></param>
        public void SaveToGif(string path, short delay, bool repeat)
        {
            AnimatedGifEncoder aniEncoder = new AnimatedGifEncoder();
            aniEncoder.Start(path);
            aniEncoder.SetDelay(delay);
            aniEncoder.SetRepeat(repeat ? 0 : -1);

           using (MemoryStream memoryStream = new MemoryStream())
           {
               int lastIndex = morphManager.KeyFrames[morphManager.KeyFrames.Count - 1].Index;
                for (int i = 0; i <= lastIndex; i++)
                {
                    Morphing.Core.Frame frame = morphManager.GetFrame(i);
                    if (frame.WarpedBitmap == null)
                        continue;

                    // Vytvoreni gif obrazku a vlozeni do kolekce snimku
                    GifBitmapEncoder gifEncoder = new GifBitmapEncoder();
                    gifEncoder.Frames.Add(BitmapFrame.Create(frame.WarpedBitmap));
                    gifEncoder.Save(memoryStream);
                    aniEncoder.AddFrame(System.Drawing.Image.FromStream(memoryStream));
                    memoryStream.Seek(0, SeekOrigin.Begin);
                }
                aniEncoder.Finish();
            }
           selectedFrame.ApplyWarping();
        }



        /// <summary>
        /// Nahraje scenu z nativniho formatu
        /// </summary>
        /// <param name="path"></param>
        public void Load(string path)
        {
            // dekomprese a deserializace sceny
            MorphManager loadedMorphManager;
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using (GZipStream gzStream = new GZipStream(fs, CompressionMode.Decompress))
                {
                    loadedMorphManager = (MorphManager)(new XmlSerializer(typeof(MorphManager))).Deserialize(gzStream);
                }
            }

            // Smazani aktualni sceny
            clearDrawing();
            layerNodes.Children.Clear();

            // Nahraje nactenou scenu
            morphManager.RawKeyFramesArray = loadedMorphManager.RawKeyFramesArray;

            if (selectedFrame.Index != 0)
                SelectedFrameIndex = 0;
        }

        #endregion
    }
}