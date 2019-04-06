using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Media;

namespace VeNET.Objects
{


    public class TransformationBorder
    {
        private TransformationBorderState state = TransformationBorderState.Resize;
        private Grid border = new VeNet.UserControls.SelectionBorder();


       

        public TransformationBorder()
        {
            /*
            this.stroke = new Rectangle();
            this.stroke.Stroke = Brushes.Black;
            this.stroke.StrokeDashCap = PenLineCap.Square;
            this.stroke.StrokeDashArray.Add(5);
            this.border.Children.Add(stroke);

            Rectangle corner;

            corner = this.getResizeCorner();
            Canvas.SetLeft(corner, -5);
            Canvas.SetTop(corner, -5);
            corner.Cursor = System.Windows.Input.Cursors.SizeNWSE;
            this.resizeCorners.Add(corner);
            this.border.Children.Add(corner);

            corner = this.getResizeCorner();
            Canvas.SetTop(corner, -5);
            corner.HorizontalAlignment = HorizontalAlignment.Center;
            corner.Cursor = System.Windows.Input.Cursors.SizeNS;
            this.resizeCorners.Add(corner);
            this.border.Children.Add(corner);

            corner = this.getResizeCorner();
            Canvas.SetRight(corner, -5);
            Canvas.SetTop(corner, -5);
            corner.Cursor = System.Windows.Input.Cursors.SizeNESW;
            this.resizeCorners.Add(corner);
            this.border.Children.Add(corner);

            corner = this.getResizeCorner();
            Canvas.SetRight(corner, -5);
            Canvas.SetBottom(corner, -5);
            corner.Cursor = System.Windows.Input.Cursors.SizeNWSE;
            this.resizeCorners.Add(corner);
            this.border.Children.Add(corner);

            corner = this.getResizeCorner();
            Canvas.SetLeft(corner, -5);
            Canvas.SetBottom(corner, -5);
            corner.Cursor = System.Windows.Input.Cursors.SizeNESW;
            this.resizeCorners.Add(corner);
            this.border.Children.Add(corner);
             */
        }

        /*
        private void activateState()
        {
            this.border.Children.RemoveRange(this.border.Children.Count - 4, 4);
            List<Shape> corners = this.state == TransformationBorderState.Resize ? this.resizeCorners : this.rotateCorners;
            foreach (Shape corner in corners)
                this.border.Children.Add(corner);
        }*/

        private Rectangle getResizeCorner()
        {
            Rectangle corner = new Rectangle();
            corner.Width = corner.Height = 10;
            corner.Stroke = Brushes.Black;
            corner.Fill = Brushes.White;
            return corner;
        }
    }
}
