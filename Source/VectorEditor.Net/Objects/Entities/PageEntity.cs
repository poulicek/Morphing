using System;
using System.Collections.Generic;

using System.Text;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;

namespace VeNET.Objects.Entities
{
    public class PageEntity : Entity, Interfaces.IFillable
    {
        public Brush Fill { get { return this.Shape.Fill; } set { this.Shape.Fill = value; } }

        public PageEntity()
            : base("Page")
        {
            this.setGeometry(new RectangleGeometry(new Rect(new Point(0, 0), new Point(1, 1)), 0, 0));
            this.Width = 400;
            this.Height = 200;
            this.Fill = Brushes.White;
            this.Stroke = Brushes.Black;
        }
    }
}
