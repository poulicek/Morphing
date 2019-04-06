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

namespace Morphing.Dialogs
{
    /// <summary>
    /// Interaction logic for Resolution.xaml
    /// </summary>
    public partial class Resolution : Window
    {
        public string FrameWidth
        {
            get { return txtWidth.Text; }
        }

        public string FrameHeight
        {
            get { return txtHeight.Text; }
        }

        public Resolution()
        {
            InitializeComponent();
        }

        public Resolution(string width, string height)
            : this()
        {
            txtWidth.Text = width;
            txtHeight.Text = height;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
