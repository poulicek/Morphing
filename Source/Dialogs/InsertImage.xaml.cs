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
    public partial class InsertImage : Window
    {
        private Scene scene;
        private string path;

        public InsertImage(Window owner, string path, Scene scene)
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
                scene.InsertImage(path, rdbAdaptScene.IsChecked == null ? false : (bool)rdbAdaptScene.IsChecked);
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
