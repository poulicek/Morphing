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
using VeNET.Objects;

namespace VeNET.UserControls
{

    public partial class SelectionBorder : Grid
    {
        private SelectionState state;
        private Rectangle activeCorner = null;

        public Rectangle ActiveCorner { get { return this.activeCorner; } }

        /// <summary>
        /// Aktualní druh rámu
        /// </summary>
        public SelectionState State
        {
            get { return this.state; }
            set
            {
                if (this.state != value)
                {
                    this.state = value;
                    this.activateState(value);
                }
            }
        }


        /// <summary>
        /// Proporce rámu
        /// </summary>
        public Point Proportions
        {
            set
            {
                this.Width = value.X;
                this.Height = value.Y;
            }
        }


        /// <summary>
        /// Pozice rámu
        /// </summary>
        public Point Position
        {
            set
            {
                Canvas.SetLeft(this, value.X);
                Canvas.SetTop(this, value.Y);
            }
        }


        public SelectionBorder()
        {
            InitializeComponent();
            this.border.StrokeDashArray.Add(5);
            this.MouseLeave += delegate(object sender, MouseEventArgs e) { this.activeCorner = null; };
            this.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e) { Rectangle tmp = ((Rectangle)this.InputHitTest(e.GetPosition(this))); if (tmp != this.border) this.activeCorner = tmp; };
        }


        /// <summary>
        /// Aktivuje požadovaný stav transformačního rámu
        /// </summary>
        /// <param name="state"></param>
        private void activateState(SelectionState state)
        {
            if (state == SelectionState.Draw)
                this.Visibility = Visibility.Hidden;
            else if (state == SelectionState.Resize)
            {
                this.Visibility = Visibility.Visible;
                this.corner1.Style = this.corner2.Style = this.corner3.Style = this.corner4.Style = (Style)FindResource("Corner");
                this.corner5.Visibility = this.corner6.Visibility = this.corner7.Visibility = this.corner8.Visibility = Visibility.Visible;
                this.corner1.Cursor = Cursors.SizeNWSE;
                this.corner2.Cursor = Cursors.SizeNESW;
                this.corner3.Cursor = Cursors.SizeNESW;
                this.corner4.Cursor = Cursors.SizeNWSE;
            }
            else
            {
                this.Visibility = Visibility.Visible;
                this.corner1.Style = this.corner2.Style = this.corner3.Style = this.corner4.Style = (Style)FindResource("RoundCorner");
                this.corner1.Cursor = this.corner2.Cursor = this.corner3.Cursor = this.corner4.Cursor = Cursors.Cross;
                this.corner5.Visibility = this.corner6.Visibility = this.corner7.Visibility = this.corner8.Visibility = Visibility.Hidden;
            }
        }
    }
}
