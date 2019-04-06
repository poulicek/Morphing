using System;
using System.Collections.Generic;

using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;

namespace VeNET.Objects.Entities
{
    public class DimensionEntity : Entity
    {
        // Události
        public event System.Windows.Input.MouseButtonEventHandler MouseDoubleClick;

        // Části geomtrie
        private PathFigure leftArrow = new PathFigure();
        private PathFigure rightArrow = new PathFigure();
        private PathFigure horizontal = new PathFigure();
        private PathFigure leftVertical = new PathFigure();
        private PathFigure rightVertical = new PathFigure();
        private Label label = new Label();

        private bool flippedVerticaly = false;
        private double verticalLabelPosition = 0.08;
        private double horizontalLabelPosition = 0.5;

        #region Vlastnosti

        /// <summary>
        /// Vrátí objekt textového popisku
        /// </summary>
        public Label Label
        {
            get { return this.label; }
        }

        /// <summary>
        /// Vrátí nebo nastaví obsah textového popisku
        /// </summary>
        public string Text
        {
            get { return label.Content.ToString(); }
            set
            {
                this.label.Content = value;
                this.X = this.X;
                this.Y = this.Y;
            }
        }

        /// <summary>
        /// Vrátí nebo nastaví horizontální pozici
        /// </summary>
        public new double X
        {
            get
            {
                return base.X;
            }
            set
            {
                base.X = value;
                double pos = base.X + this.horizontalLabelPosition * this.Width;
                if (this.horizontalLabelPosition == 0.5)
                    pos -= this.Text.Length * 3.5;
                else if (this.horizontalLabelPosition < 0.5 )
                    pos -= 20;
                Canvas.SetLeft(this.label, pos);
            }
        }

        /// <summary>
        /// Vrátí nebo nastaví vertikalní pozici
        /// </summary>
        public new double Y
        {
            get
            {
                return base.Y;
            }
            set
            {
                base.Y = value;
                double pos = base.Y + this.verticalLabelPosition * this.Height;
                if (this.verticalLabelPosition == 0.5)
                    pos -= this.Text.Length * 3.5;
                else
                    pos -= 20;
                Canvas.SetTop(this.label, pos);
            }
        }

        /// <summary>
        /// Vrátí nebo nastaví šířku
        /// </summary>
        public new double Width
        {
            get { return this.Shape.Data.Bounds.Width; }
            set
            {
                this.ApplyTransfrom(new ScaleTransform(value / (this.Width == 0 ? 1 : this.Width), 1));

                double ratio = (value - 8) / value;
                this.leftArrow.StartPoint = new Point(1 - ratio, this.leftArrow.StartPoint.Y);
                this.rightArrow.StartPoint = new Point(ratio, this.rightArrow.StartPoint.Y);
                ((LineSegment)this.leftArrow.Segments[1]).Point = new Point(1-ratio, ((LineSegment)this.leftArrow.Segments[1]).Point.Y);
                ((LineSegment)this.rightArrow.Segments[1]).Point = new Point(ratio, ((LineSegment)this.rightArrow.Segments[1]).Point.Y);
            }
        }

        /// <summary>
        /// Vrátí nebo nastaví výšku
        /// </summary>
        public new double Height
        {
            get { return this.Shape.Data.Bounds.Height; }
            set
            {
                this.ApplyTransfrom(new ScaleTransform(1, value / (this.Height == 0 ? 1 : this.Height)));

                double ratio = 1 - ((value - 8) / value);
                this.leftArrow.StartPoint = new Point(this.leftArrow.StartPoint.X, -ratio);
                this.rightArrow.StartPoint = new Point(this.rightArrow.StartPoint.X, -ratio);
                ((LineSegment)this.leftArrow.Segments[1]).Point = new Point(((LineSegment)this.leftArrow.Segments[1]).Point.X, ratio);
                ((LineSegment)this.rightArrow.Segments[1]).Point = new Point(((LineSegment)this.rightArrow.Segments[1]).Point.X, ratio);
            }
        }

        #endregion

        public DimensionEntity()
            : base("Dimension")
        {
            this.label.Content = "0";
            this.label.FontFamily = new FontFamily("Courier New");

            leftArrow.IsFilled = false;
            leftArrow.StartPoint = new Point(0.08, -0.08);
            leftArrow.Segments.Add(new LineSegment(new Point(0, 0), true));
            leftArrow.Segments.Add(new LineSegment(new Point(0.08, 0.08), true));

            rightArrow.IsFilled = false;
            rightArrow.StartPoint = new Point(0.92, -0.08);
            rightArrow.Segments.Add(new LineSegment(new Point(1, 0), true));
            rightArrow.Segments.Add(new LineSegment(new Point(0.92, 0.08), true));

            horizontal.StartPoint = new Point(0, 0);
            horizontal.Segments.Add(new LineSegment(new Point(1, 0), true));

            leftVertical.StartPoint = new Point(0, 1);
            leftVertical.Segments.Add(new LineSegment(new Point(0, 0), true));

            rightVertical.StartPoint = new Point(1, 0);
            rightVertical.Segments.Add(new LineSegment(new Point(1, 1), true));

            PathGeometry lines = new PathGeometry();
            lines.Figures.Add(leftArrow);
            lines.Figures.Add(rightArrow);
            lines.Figures.Add(horizontal);
            lines.Figures.Add(leftVertical);
            lines.Figures.Add(rightVertical);
            this.setGeometry(lines);


            // Přiřazení delegátů
            this.label.MouseLeftButtonDown += this.raiseMouseButtonDown;
            this.label.MouseRightButtonDown += this.raiseMouseButtonDown;
            this.label.MouseLeftButtonUp += this.raiseMouseButtonUp;
            this.label.MouseRightButtonUp += this.raiseMouseButtonUp;
            this.label.MouseDoubleClick += delegate(object sender, MouseButtonEventArgs e)
            {
                if (this.MouseDoubleClick != null)
                    this.MouseDoubleClick(sender, e);
            };
        }


        /// <summary>
        /// Nastaví kurzor
        /// </summary>
        /// <param name="cursor">Kurzor</param>
        public override void SetCursor(Cursor cursor)
        {
            this.label.Cursor = this.Shape.Cursor = cursor;
        }


        /// <summary>
        /// Presune objekt na zadanou pozici
        /// </summary>
        /// <param name="point">Nová pozice</param>
        public override void Move(Point point)
        {
            this.X = point.X;
            this.Y = point.Y;
        }


        /// <summary>
        /// Změna rozměrů
        /// </summary>
        /// <param name="scaleX">Horizontální změna</param>
        /// <param name="scaleY">Vertikální změna</param>
        public override void Scale(double scaleX, double scaleY)
        {
            base.Scale(scaleX, scaleY);
            this.X = this.X;
            this.Y = this.Y;
            return;
        }


        /// <summary>
        /// Horizontální převrácení kolem středu entity
        /// </summary>
        public override void FlipHorizontaly()
        {
            base.FlipHorizontaly();
            this.horizontalLabelPosition = 1 - this.horizontalLabelPosition;
            this.X = this.X;
        }


        /// <summary>
        /// Vertikální převrácení kolem středu entity
        /// </summary>
        public override void FlipVerticaly()
        {
            base.FlipVerticaly();
            this.flippedVerticaly = !this.flippedVerticaly;
            this.verticalLabelPosition = 1 - this.verticalLabelPosition;
            this.Y = this.Y;
        }


        /// <summary>
        /// Provede rotaci entity kolem zadaného středu rotace
        /// </summary>
        /// <param name="angle">Úhel otočení</param>
        public override void Rotate(double angle)
        {
            if ((angle = 90 * Math.Truncate(angle / 90)) == this.rotationAngle)
                return;

            base.Rotate(angle);
            switch ((int)((360 + angle) % 360))
            {
                case 0:
                    this.verticalLabelPosition = 0.08;
                    this.horizontalLabelPosition = 0.5;
                    this.label.LayoutTransform = null;
                    break;

                case 90:
                    this.verticalLabelPosition = 0.5;
                    this.horizontalLabelPosition = 0.92;
                    this.label.LayoutTransform = new RotateTransform(-90);
                    break;
                
                case 180:
                    this.verticalLabelPosition = 0.92;
                    this.horizontalLabelPosition = 0.5;
                    this.label.LayoutTransform = null;
                    break;

                case 270:
                    this.verticalLabelPosition = 0.5;
                    this.horizontalLabelPosition = 0.08;
                    this.label.LayoutTransform = new RotateTransform(-90);
                    break;
            }

            if (this.flippedVerticaly)
            {
                this.horizontalLabelPosition = 1 - this.horizontalLabelPosition;
                this.verticalLabelPosition = 1 - this.verticalLabelPosition;
            }

            this.X = this.X;
            this.Y = this.Y;
        }
    }
}