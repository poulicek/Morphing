using System;
using System.Collections.Generic;

using System.Text;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;

namespace VeNET.Objects.Entities
{
    public class RectangleEntity : Entity, Interfaces.IFillable
    {
        public Brush Fill { get { return this.Shape.Fill; } set { this.Shape.Fill = value; } }
        public double RadiusX { get { return ((RectangleGeometry)this.Shape.Data).RadiusX; } set { ((RectangleGeometry)this.Shape.Data).RadiusX = value; } }
        public double RadiusY { get { return ((RectangleGeometry)this.Shape.Data).RadiusY; } set { ((RectangleGeometry)this.Shape.Data).RadiusY = value; } }

        public RectangleEntity()
            : base("Rectangle")
        {
            this.setGeometry(new RectangleGeometry(new Rect(new Point(0, 0), new Point(1, 1)), 0, 0));
        }
    }
}
