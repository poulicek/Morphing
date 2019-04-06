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

namespace VeNET.Dialogs
{
    /// <summary>
    /// Interaction logic for TextInput.xaml
    /// </summary>
    public partial class TextInput : Window
    {
        public string Text
        {
            get { return this.textBox.Text; }
            set { this.textBox.Text = value; }
        }

        public TextInput()
        {
            InitializeComponent();
            this.Title = "Vložte požadovaný text";
            this.textBox.Focus();
            this.btnOk.Click += delegate(object sender, RoutedEventArgs e) { this.DialogResult = true; };
        }
    }
}
