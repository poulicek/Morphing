using System;
using System.Collections.Generic;

using System.Text;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;

namespace VeNET.Objects.Entities
{
    public class PolygonEntity : Entity, Interfaces.IFillable
    {
        public Brush Fill { get { return this.Shape.Fill; } set { this.Shape.Fill = value; } }

        public PointCollection Points
        {
            get
            {
                PointCollection points = new PointCollection();
                if (this.Shape.Data is PathGeometry)
                    foreach (LineSegment segment in ((PathGeometry)this.Shape.Data).Figures[0].Segments)
                        points.Add(segment.Point);
                return points;
            }
        }


        public PolygonEntity()
            : base("Polygon")
        {
            PathGeometry geometry = new PathGeometry();
            PathFigure figure = new PathFigure();
            figure.IsClosed = true;
            geometry.Figures.Add(figure);
            this.setGeometry(geometry);
        }


        /// <summary>
        /// Přidá vrchol polygonu
        /// </summary>
        /// <param name="point"></param>
        public void AddPoint(Point point)
        {
            if (((PathGeometry)this.Shape.Data).Figures[0].Segments.Count == 0)
                ((PathGeometry)this.Shape.Data).Figures[0].StartPoint = point;
            ((PathGeometry)this.Shape.Data).Figures[0].Segments.Add(new LineSegment(point, true));
            this.originalWidth = this.Width;
            this.originalHeight = this.Height;
        }
    }
}
