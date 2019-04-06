using System;
using System.Collections.Generic;

using System.Text;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;

namespace VeNET.Objects.Entities
{
    public class EllipseEntity : Entity, Interfaces.IFillable
    {
        public Brush Fill { get { return this.Shape.Fill; } set { this.Shape.Fill = value; } }

        public EllipseEntity()
            : base("Ellipse")
        {
            this.setGeometry(new EllipseGeometry(new Point(0.5, 0.5), 0.5, 0.5));
        }
    }
}
