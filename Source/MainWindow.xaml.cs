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
using System.IO;

namespace Morphing
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string loadedFile;

        public MainWindow()
        {
            InitializeComponent();
            
            timeLine.Scene = this.scene;
            scene.SelectedFrameChanged += new EventHandler(actualizeMenuOptions);
            scene.MorphManager.KeyFramesChanged += new EventHandler(actualizeMenuOptions);
        }


        #region Udalosti UI

        /// <summary>
        /// Actualization of menu options
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void actualizeMenuOptions(object sender, EventArgs e)
        {
            bool isKeyFrame = this.scene.MorphManager.KeyFrames.Contains(this.scene.SelectedFrame);
            this.menuCreateKeyFrame.IsEnabled = !isKeyFrame && this.scene.SelectedFrame.SourceBitmap != null;
            this.menuDeleteKeyFrame.IsEnabled = isKeyFrame && this.scene.SelectedFrameIndex != 0;
        }


        /// <summary>
        /// Change of current zoom
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sliderZoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double zoom = 1 + this.sliderZoom.Value - (int)(this.sliderZoom.Maximum / 2 + 1);
            if (zoom <= 1)
                zoom = Math.Pow(2, zoom - 1);

            this.scene.Zoom = zoom;
        }


        /// <summary>
        /// Change of grid resolution
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sliderGridResolution_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            scene.ControlGridResolution = (int)sliderGridResolution.Value;
        }


        /// <summary>
        /// Performation of drag & drop
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mainWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                this.Cursor = Cursors.Wait;
                try
                {
                    string path = ((string[])e.Data.GetData(DataFormats.FileDrop, true))[0];
                    string ext = System.IO.Path.GetExtension(path).ToLower();
                    switch (ext)
                    {
                        case ".zpo":
                            scene.Load(path);
                            sliderGridResolution.Value = scene.ControlGridResolution;
                            this.Title = System.IO.Path.GetFileName(loadedFile = path) + " - " + this.Title;
                            break;
                        case ".bmp":
                        case ".jpg":
                        case ".gif":
                        case ".png":
                        case ".tif":
                            (new Dialogs.InsertImage(this, path, scene)).ShowDialog();
                            break;
                    }
                }
                catch
                {
                    MessageBox.Show("Zvolený soubor se nepodařilo otevřít.", "Otevřít...", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                this.Cursor = Cursors.Arrow;
            }
            e.Handled = true;
        }


        /// <summary>
        /// Processing of key pressing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                    if (this.mainMenu.IsMouseCaptured || sliderGridResolution.IsFocused || sliderZoom.IsFocused)
                        break;

                    e.Handled = true;
                    if (scene.CanMergeWidthBackground())
                        --scene.SelectedFrameIndex;
                    break;

                case Key.Right:
                    if (this.mainMenu.IsMouseCaptured || sliderGridResolution.IsFocused || sliderZoom.IsFocused)
                        break;

                    e.Handled = true;
                    if (scene.CanMergeWidthBackground())
                        ++scene.SelectedFrameIndex;
                    break;
                
                case Key.Delete:
                    e.Handled = true;
                    if (scene.Mode == Morphing.Components.Scene.SceneMode.Drawing)
                        scene.DeleteSelectedEntities();
                    else
                        scene.MorphManager.RemoveKeyFrame(this.scene.SelectedFrame);
                    break;

                case Key.S:
                    if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                    {
                        e.Handled = true;
                        menuSave_Click(sender, new RoutedEventArgs());
                    }
                    break;

                case Key.Next:
                    if (this.mainMenu.IsMouseCaptured || sliderGridResolution.IsFocused || sliderZoom.IsFocused)
                        break;

                    e.Handled = true;
                    btnNextKey_Click(this, null);
                    break;

                case Key.Prior:
                    if (this.mainMenu.IsMouseCaptured || sliderGridResolution.IsFocused || sliderZoom.IsFocused)
                        break;

                    e.Handled = true;
                    btnPrevKey_Click(this, null);
                    break;
                
                case Key.End:
                    if (this.mainMenu.IsMouseCaptured || sliderGridResolution.IsFocused || sliderZoom.IsFocused)
                        break;

                    e.Handled = true;
                    btnEnd_Click(this, null);
                    break;

                case Key.Home:
                    if (this.mainMenu.IsMouseCaptured || sliderGridResolution.IsFocused || sliderZoom.IsFocused)
                        break;

                    e.Handled = true;
                    btnStart_Click(this, null);
                    break;

                case Key.N:
                    if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                    {
                        e.Handled = true;
                        menuNew_Click(this, e);
                    }
                    break;

                case Key.F4:
                    if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
                    {
                        e.Handled = true;
                        menuClose_Click(this, e);
                    }
                    break;

                case Key.System:
                    if (!Keyboard.IsKeyDown(Key.F4))
                        e.Handled = true;
                    break;
            }
        }


        /// <summary>
        /// Change of zoom based on mouse scrolling
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mainWindow_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                e.Handled = true;
                sliderZoom.Value += (double)e.Delta / 600;
                this.Focus();
            }
            else if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                e.Handled = true;
                sliderGridResolution.Value += (double)e.Delta / 120;
                this.Focus();
            }
            else if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            {
                e.Handled = true;
                if (e.Delta != 0 && scene.CanMergeWidthBackground())
                    scene.SelectedFrameIndex += e.Delta > 0 ? -1 : (scene.SelectedFrameIndex < scene.MorphManager.KeyFrames[scene.MorphManager.KeyFrames.Count - 1].Index ? 1 : 0);
                this.Focus();
            }
        }


        #endregion

        #region Udalosti panelu nastroju

        private void btnWarping_Click(object sender, RoutedEventArgs e)
        {
            if (scene.CanMergeWidthBackground())
            {
                scene.Mode = Morphing.Components.Scene.SceneMode.Warping;
                btnWarping.IsChecked = true;
            }
            else
                e.Handled = true;
        }

        private void btnGridControl_Click(object sender, RoutedEventArgs e)
        {
            if (scene.CanMergeWidthBackground())
            {
                scene.Mode = Morphing.Components.Scene.SceneMode.GridControl;
                btnGridControl.IsChecked = true;
            }
            else
                e.Handled = true;
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            scene.Mode = Morphing.Components.Scene.SceneMode.Drawing;
            scene.DrawingMode = VeNET.Objects.DrawMode.Select;
        }

        private void btnLine_Click(object sender, RoutedEventArgs e)
        {
            scene.Mode = Morphing.Components.Scene.SceneMode.Drawing;
            scene.DrawingMode = VeNET.Objects.DrawMode.Line;
        }

        private void btnText_Click(object sender, RoutedEventArgs e)
        {
            scene.Mode = Morphing.Components.Scene.SceneMode.Drawing;
            scene.DrawingMode = VeNET.Objects.DrawMode.Text;
        }

        private void btnRectangle_Click(object sender, RoutedEventArgs e)
        {
            scene.Mode = Morphing.Components.Scene.SceneMode.Drawing;
            scene.DrawingMode = VeNET.Objects.DrawMode.Rectangle;
        }

        private void btnEllipse_Click(object sender, RoutedEventArgs e)
        {
            scene.Mode = Morphing.Components.Scene.SceneMode.Drawing;
            scene.DrawingMode = VeNET.Objects.DrawMode.Ellipse;
        }

        private void btnPolygon_Click(object sender, RoutedEventArgs e)
        {
            scene.Mode = Morphing.Components.Scene.SceneMode.Drawing;
            scene.DrawingMode = VeNET.Objects.DrawMode.Polygon;
        }

        private void btnBitmap_Click(object sender, RoutedEventArgs e)
        {
            scene.Mode = Morphing.Components.Scene.SceneMode.Drawing;
            scene.DrawingMode = VeNET.Objects.DrawMode.Bitmap;
        }

        #endregion

        #region Udalosti navigacniho panelu

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            scene.SelectedFrameIndex = 0;
        }

        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (scene.SelectedFrameIndex > 0)
                --scene.SelectedFrameIndex;
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (scene.SelectedFrameIndex < scene.MorphManager.KeyFrames[scene.MorphManager.KeyFrames.Count - 1].Index)
                ++scene.SelectedFrameIndex;
        }

        private void btnEnd_Click(object sender, RoutedEventArgs e)
        {
            scene.SelectedFrameIndex = scene.MorphManager.KeyFrames[scene.MorphManager.KeyFrames.Count - 1].Index;
        }

        private void btnPrevKey_Click(object sender, RoutedEventArgs e)
        {
            for (int i = scene.MorphManager.KeyFrames.Count - 1; i >= 0; --i)
            {
                if (scene.MorphManager.KeyFrames[i].Index < scene.SelectedFrameIndex)
                {
                    scene.SelectedFrameIndex = scene.MorphManager.KeyFrames[i].Index;
                    break;
                }
            }
        }

        private void btnNextKey_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < scene.MorphManager.KeyFrames.Count; ++i)
            {
                if (scene.MorphManager.KeyFrames[i].Index > scene.SelectedFrameIndex)
                {
                    scene.SelectedFrameIndex = scene.MorphManager.KeyFrames[i].Index;
                    break;
                }
            }
        }

        #endregion

        #region Udalosti menu

        /// <summary>
        /// Zavrit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        /// <summary>
        /// Novy
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuNew_Click(object sender, RoutedEventArgs e)
        {
            btnWarping.IsChecked = true;
            scene.Mode = Morphing.Components.Scene.SceneMode.Warping;
            scene.Reset();
            loadedFile = null;
            this.Title = this.Title;
        }


        /// <summary>
        /// Otevrit...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuOpen_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog();
            dlg.Filter = this.Title + " (*.zpo)|*.zpo";
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.Cursor = Cursors.Wait;
                try
                {
                    scene.Load(dlg.FileName);
                    sliderGridResolution.Value = scene.ControlGridResolution;
                    this.Title = System.IO.Path.GetFileName(loadedFile = dlg.FileName) + " - " + this.Title;
                }
                catch
                {
                    MessageBox.Show("Zvolený soubor se nepodařilo otevřít.", "Otevřít...", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                this.Cursor = Cursors.Arrow;
            }
        }



        /// <summary>
        /// Ulozit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuSave_Click(object sender, RoutedEventArgs e)
        {
            if (loadedFile == null)
                menuSaveAs_Click(sender, e);
            else
            {
                if (!scene.CanMergeWidthBackground())
                    return;

                this.Cursor = Cursors.Wait;
                try
                {
                    scene.Mode = Morphing.Components.Scene.SceneMode.Warping;
                    btnWarping.IsChecked = true;
                    scene.Save(loadedFile);
                }
                catch
                {
                    MessageBox.Show("Zvolený soubor se nepodařilo uložit.", "Uložit", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                this.Cursor = Cursors.Arrow;
            }
        }


        /// <summary>
        /// Ulozit jako...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuSaveAs_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog dlg = new System.Windows.Forms.SaveFileDialog();
            dlg.FileName = System.IO.Path.GetFileNameWithoutExtension(loadedFile);
            dlg.Filter = this.Title + " (*.zpo)|*.zpo|GIF (*.gif)|*.gif|JPG (*.jpg)|*.jpg|PNG (*.png)|*.png|BMP (*.bmp)|*.bmp";
            dlg.AddExtension = true;
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (!scene.CanMergeWidthBackground())
                    return;

                this.Cursor = Cursors.Wait;
                try
                {
                    scene.Mode = Morphing.Components.Scene.SceneMode.Warping;
                    btnWarping.IsChecked = true;
                    switch (dlg.FilterIndex)
                    {
                        case 1:
                            scene.Save(dlg.FileName);
                            loadedFile = dlg.FileName;
                            this.Title = System.IO.Path.GetFileName(loadedFile = dlg.FileName) + " - " + this.Title;
                            break;

                        case 2: (new Dialogs.GifSettings(this, dlg.FileName, scene)).ShowDialog(); break;
                        case 3: scene.Save(dlg.FileName, new JpegBitmapEncoder()); break;
                        case 4: scene.Save(dlg.FileName, new PngBitmapEncoder()); break;
                        case 5: scene.Save(dlg.FileName, new BmpBitmapEncoder()); break;
                    }
                }
                catch
                {
                    MessageBox.Show("Zvolený soubor se nepodařilo uložit.", "Uložit jako...", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                this.Cursor = Cursors.Arrow;
            }
        }


        /// <summary>
        /// Vytvorit klicovy snimek
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuCreateKeyFrame_Click(object sender, RoutedEventArgs e)
        {
            if (scene.SelectedFrame.WarpedBitmap != null)
                scene.MorphManager.AddKeyFrame(scene.SelectedFrame);
        }


        /// <summary>
        /// Odstranit klicovy snimek
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuDeleteKeyFrame_Click(object sender, RoutedEventArgs e)
        {
            scene.MorphManager.RemoveKeyFrame(this.scene.SelectedFrame);
        }


        /// <summary>
        /// Vlozit obrazek
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuInsertImage_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog();
            dlg.Filter = "Obrázky|*.jpg;*.gif;*.png;*.tif;*.bmp|Všechny soubory|*.*";
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                (new Dialogs.InsertImage(this, dlg.FileName, scene)).ShowDialog();
            }
        }


        /// <summary>
        /// Zmenit rozmery
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuResolution_Click(object sender, RoutedEventArgs e)
        {
            Dialogs.Resolution dlg = new Morphing.Dialogs.Resolution(((int)(scene.Width / scene.Zoom)).ToString(), ((int)(scene.Height / scene.Zoom)).ToString());
            dlg.Owner = this;
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    if (scene.CanMergeWidthBackground())
                        scene.SetResolution(int.Parse(dlg.FrameWidth), int.Parse(dlg.FrameHeight));
                }
                catch { }
            }
        }


        /// <summary>
        /// O programu...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuAbout_Click(object sender, RoutedEventArgs e)
        {
            (new Dialogs.About(this)).ShowDialog();
        }


        /// <summary>
        /// Language
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuLanguage_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists("Languages") || !this.menuLanguage.Items.Contains(this.menuLanguageDefault))
                return;

            string[] langFiles = Directory.GetFiles("Languages");
            this.menuLanguage.Items.Clear();
            foreach (string langFile in langFiles)
            {
                MenuItem langOption = new MenuItem();
                langOption.Header = System.IO.Path.GetFileNameWithoutExtension(langFile);
                langOption.Click += delegate(object _sender, RoutedEventArgs _e) { Helpers.Localization.Load("Languages/" + langOption.Header + ".xaml"); };
                this.menuLanguage.Items.Add(langOption);
            }
        }
        #endregion
    }
}
