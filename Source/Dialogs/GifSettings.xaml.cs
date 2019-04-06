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
using System.Windows.Shapes;
using Morphing.Components;

namespace Morphing.Dialogs
{
    /// <summary>
    /// Interaction logic for Resolution.xaml
    /// </summary>
    public partial class GifSettings : Window
    {
        private Scene scene;
        private string path;

        public GifSettings(Window owner, string path, Scene scene)
        {
            InitializeComponent();
            this.Owner = owner;
            this.scene = scene;
            this.path = path;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            if (DialogResult == true)
            {
                Cursor = Cursors.Wait;
                scene.SaveToGif(path, (short)((double)1000 / double.Parse(this.txtFPS.Text)), this.chkRepeat.IsChecked == null ? false : (bool)this.chkRepeat.IsChecked);
                Cursor = Cursors.Arrow;
            }
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
