using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;
using Morphing.Components;
using Morphing.Core;

namespace Morphing.UserControls
{
    /// <summary>
    /// Interaction logic for TimeLine.xaml
    /// </summary>
    public partial class TimeLine : UserControl
    {
        private int FRAME_WIDTH = 10;
        private int FRAME_HEIGHT = 30;

        private int lastKeyFrameIndex;
        private int mouseDownIndex;
        private int framesCount;
        private Scene scene;

        /// <summary>
        /// Objekt pro ovladani sceny
        /// </summary>
        public Scene Scene
        {
            set
            {
                if (scene == null)
                {
                    scene = value;
                    scene.SelectedFrameChanged += delegate(object sender, EventArgs e)
                    {
                        Canvas.SetLeft(selectionFrame, FRAME_WIDTH * scene.SelectedFrame.Index);
                        if (scene.SelectedFrame.Index >= framesCount)
                            FramesCount++;
                    };
                    scene.MorphManager.KeyFramesChanged += delegate(object sender, EventArgs e)
                    {
                        lastKeyFrameIndex = scene.MorphManager.KeyFrames[scene.MorphManager.KeyFrames.Count - 1].Index;
                        drawFrames();
                    };
                }
            }
        }


        /// <summary>
        /// Vrati nebo nastavi pocet zobrazovanych snimku
        /// </summary>
        public int FramesCount
        {
            get { return framesCount; }
            set
            {
                if (value > framesCount)
                {
                    framesCount = value;
                    canvas.Width = framesCount * FRAME_WIDTH;
                    timeLineGraph.Source = new RenderTargetBitmap((int)canvas.Width, (int)this.Height, 96, 96, PixelFormats.Pbgra32);
                    drawFrames();
                }
            }
        }


        public TimeLine()
        {
            InitializeComponent();
            FRAME_HEIGHT = (int)canvas.Height - 20;
            selectionFrame.Width = (hoverFrame.Width = FRAME_WIDTH) - 2;
            selectionFrame.Height = (hoverFrame.Height = FRAME_HEIGHT) - 2;

            this.Loaded += delegate(object sender, RoutedEventArgs e)
            {
                FramesCount = 200;
                canvas.MouseDown += new MouseButtonEventHandler(mouseDown);
                canvas.MouseUp += new MouseButtonEventHandler(mouseUp);
                canvas.MouseMove += new MouseEventHandler(mouseMove);
                canvas.MouseLeave += new MouseEventHandler(mouseLeave);
            };
        }

        
        /// <summary>
        /// Vykresli osu snimku
        /// </summary>
        private void drawFrames()
        {
            DrawingVisual dv = new DrawingVisual();
            DrawingContext dc = dv.RenderOpen();

            for (int i = 0; i < framesCount; i++)
            {
                int x = FRAME_WIDTH * i;
                dc.DrawRectangle(i > lastKeyFrameIndex ? Brushes.Gray : Brushes.WhiteSmoke, new Pen(Brushes.DarkGray, 0.5), new Rect(x, 0, FRAME_WIDTH, FRAME_HEIGHT));

                // Vykresleni popisku
                if (i % 10 == 0)
                {
                    dc.DrawLine(new Pen(Brushes.White, 1), new Point(x + FRAME_WIDTH / 2, FRAME_HEIGHT), new Point(x + FRAME_WIDTH / 2, FRAME_HEIGHT + 5));
                    dc.DrawText(new FormattedText(i.ToString(), new CultureInfo("cs-cz"), FlowDirection.LeftToRight, new Typeface(this.FontFamily, FontStyles.Normal, FontWeights.Normal, this.FontStretch), this.FontSize, Brushes.LightGray), new Point(x, FRAME_HEIGHT + 4));
                }
                else if (i % 5 == 0)
                    dc.DrawLine(new Pen(Brushes.LightGray, 1), new Point(x + FRAME_WIDTH / 2, FRAME_HEIGHT), new Point(x + FRAME_WIDTH / 2, FRAME_HEIGHT + 3));
            }

            // Oznaceni klicovych snimku
            if (lastKeyFrameIndex == 0)
                dc.DrawRectangle(Brushes.LightPink, new Pen(Brushes.Red, 0.5), new Rect(0, 0, FRAME_WIDTH, FRAME_HEIGHT));
            else
                foreach (Morphing.Core.Frame keyFrame in scene.MorphManager.KeyFrames)
                    dc.DrawRectangle(Brushes.LightPink, new Pen(Brushes.Red, 0.5), new Rect(FRAME_WIDTH * keyFrame.Index, 0, FRAME_WIDTH, FRAME_HEIGHT));
            dc.Close();

            ((RenderTargetBitmap)timeLineGraph.Source).Clear();
            ((RenderTargetBitmap)timeLineGraph.Source).Render(dv);
        }

        #region Zpracovani udalosti
        
        /// <summary>
        /// Opusteni mysi - skrytu vyberoveho ramecku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void mouseLeave(object sender, MouseEventArgs e)
        {
            hoverFrame.Visibility = Visibility.Hidden;
        }


        /// <summary>
        /// Pohyb mysi nad casovou osou - zobrazeni vyberoveho ramecku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mouseMove(object sender, MouseEventArgs e)
        {
            hoverFrame.Visibility = Visibility.Visible;
            Canvas.SetLeft(hoverFrame, FRAME_WIDTH * ((int)e.GetPosition(canvas).X / FRAME_WIDTH));
        }

        
        /// <summary>
        /// Reakce na stisk tlacitka nad casovou osou
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mouseDown(object sender, MouseButtonEventArgs e)
        {
            mouseDownIndex = (int)e.GetPosition(canvas).X / FRAME_WIDTH;
            if (mouseDownIndex > 0 && scene.MorphManager.KeyFrameExists(mouseDownIndex))
                this.Cursor = Cursors.Hand;
        }


        /// <summary>
        /// Reakce na uvolneni tlacitka nad casovou osou
        /// - bud se zvoli snimek, nebo je presouvan klicovy snimek
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mouseUp(object sender, MouseButtonEventArgs e)
        {
            int frameIndex = (int)e.GetPosition(canvas).X / FRAME_WIDTH;

            // Zobrazeni pozadovaneho snimku
            if (mouseDownIndex == frameIndex)
            {
                if (scene.CanMergeWidthBackground())
                    scene.SelectedFrameIndex = frameIndex;
            }

            // Presunuti klicoveho snimku
            else if (mouseDownIndex > 0 && frameIndex > 0 && scene.MorphManager.KeyFrameExists(mouseDownIndex))
            {
                int selectIndex =  mouseDownIndex == scene.SelectedFrameIndex ?  frameIndex : scene.SelectedFrameIndex;
                scene.MorphManager.SetKeyFrameIndex(scene.MorphManager.GetFrame(mouseDownIndex), frameIndex);
                scene.SelectedFrameIndex = selectIndex;
            }
            this.Cursor = Cursors.Arrow;
        }

        #endregion
    }
}
