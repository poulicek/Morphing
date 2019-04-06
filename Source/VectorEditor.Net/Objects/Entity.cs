using System;
using System.Collections.Generic;

using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace VeNET.Objects
{
    public abstract class Entity
    {
        #region Vlastnosti

        private string type;

        // Události
        public event System.Windows.Input.MouseButtonEventHandler MouseButtonDown;
        public event System.Windows.Input.MouseButtonEventHandler MouseButtonUp;

        // Základní vlastnoti
        public string Type { get { return this.type; } }
        public double Opacity { get { return this.Shape.Opacity; } set { this.Shape.Opacity = value; } }
        public Path Shape { get; set; }
        public Brush Stroke { get { return this.Shape.Stroke; } set { this.Shape.Stroke = value; } }
        public double StrokeThickness { get { return this.Shape.StrokeThickness; } set { this.Shape.StrokeThickness = value; } }
        public double X { get { return Canvas.GetLeft(this.Shape) + this.Shape.Data.Bounds.X; } set { Canvas.SetLeft(this.Shape, value - (this.Shape.Data != null ? this.Shape.Data.Bounds.X : 0)); } }
        public double Y { get { return Canvas.GetTop(this.Shape) + this.Shape.Data.Bounds.Y; } set { Canvas.SetTop(this.Shape, value - (this.Shape.Data != null ? this.Shape.Data.Bounds.Y : 0)); } }
        public double rotationAngle;
        public int ZIndex { get; set; }
        protected Point rotationCenter;
        private List<Entity> groupedEntities = new List<Entity>();

        protected double originalWidth = 0;
        protected double originalHeight = 0;

        public List<Entity> GroupedEntities
        {
            get { return this.groupedEntities; }
        }

        public double RotationAngle
        {
            get { return this.rotationAngle; }
            set { this.Rotate(value); }
        }

        public Point RotationCenter
        {
            get { return this.rotationCenter; }
            set { this.rotationCenter = new Point(value.X - this.X - (this.Width - this.originalWidth) / 2, value.Y - this.Y - (this.Height - this.originalHeight) / 2); }
        }


        public Point RelativeRotationCenter
        {
            get { return this.rotationCenter; }
            set { this.rotationCenter = value; }
        }


        public static Brush ActualFill { get;  set; }
        public static Brush ActualStroke { get; set; }


        /// <summary>
        /// Vrátí nebo nastaví šířku
        /// </summary>
        public double Width
        {
            get { return this.Shape.Data.Bounds.Width; }
            set { this.Scale(value / (this.Width == 0 ? 1 : this.Width), 1); }
        }

        /// <summary>
        /// Vrátí nebo nastaví výšku
        /// </summary>
        public double Height
        {
            get { return this.Shape.Data.Bounds.Height; }
            set { this.Scale(1, value / (this.Height == 0 ? 1 : this.Height)); }
        }

        public double OriginalX { get { return Canvas.GetLeft(this.Shape); } set { Canvas.SetLeft(this.Shape, value); } }
        public double OriginalY { get { return Canvas.GetTop(this.Shape); } set { Canvas.SetTop(this.Shape, value); } }

        /// <summary>
        /// Vrátí nebo nastaví reáůlnou šířku entity nezávisle na rotaci
        /// </summary>
        public double OriginalWidth
        {
            get { return this.originalWidth; }
            set
            {
                this.Scale(Math.Abs(value / (this.originalWidth == 0 ? 1 : this.originalWidth)), 1);
                if (value < 0)
                    this.FlipHorizontaly();
            }
        }


        /// <summary>
        /// Vrátí nebo nastaví reáůlnou výšku entity nezávisle na rotaci
        /// </summary>
        public double OriginalHeight
        {
            get { return this.originalHeight; }
            set
            {
                this.Scale(1,  Math.Abs(value / (this.originalHeight == 0 ? 1 :this.originalHeight)));
                if (value < 0)
                    this.FlipVerticaly();
            }
        }

        #endregion

        public Entity(string type)
        {
            this.type = type;
            this.rotationAngle = 0;
            this.Shape = new Path();
            this.Shape.Stroke = ActualStroke;
            this.Shape.Fill = ActualFill;
            this.StrokeThickness = 1;

            // Přiřazení delegátů
            this.Shape.MouseLeftButtonDown += this.raiseMouseButtonDown;
            this.Shape.MouseRightButtonDown += this.raiseMouseButtonDown;
            this.Shape.MouseLeftButtonUp += this.raiseMouseButtonUp;
            this.Shape.MouseRightButtonUp += this.raiseMouseButtonUp;
        }

        #region Spouštění událostí

        protected void raiseMouseButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.MouseButtonDown != null)
                this.MouseButtonDown(this, e);
        }

        protected void raiseMouseButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.MouseButtonUp != null)
                this.MouseButtonUp(this, e);
        }

        #endregion

        /// <summary>
        /// Přiřadí objekt typu Geometry
        /// </summary>
        /// <param name="geometry"></param>
        protected void setGeometry(Geometry geometry)
        {
            this.Shape.Data = geometry;
            this.originalWidth = this.Width;
            this.originalHeight = this.Height;
        }


        /// <summary>
        /// Zruší dosavadní navázání delegátů na události
        /// </summary>
        public void ClearEventDelegates()
        {
            this.MouseButtonDown = this.MouseButtonUp = null;
        }


        /// <summary>
        /// Nastaví kurzor
        /// </summary>
        /// <param name="cursor">Kurzor</param>
        public virtual void SetCursor(Cursor cursor)
        {
            this.Shape.Cursor = cursor;
        }

        #region Transformace

        /// <summary>
        /// Aplikuje transformaci
        /// </summary>
        /// <param name="transform"></param>
        public virtual void ApplyTransfrom(Transform transform)
        {
            TransformGroup tGroup = new TransformGroup();
            tGroup.Children.Add(new MatrixTransform(this.Shape.Data.Transform.Value));
            tGroup.Children.Add(transform);
            this.Shape.Data.Transform = new MatrixTransform(tGroup.Value);
        }


        /// <summary>
        /// Presune objekt na zadanou pozici
        /// </summary>
        /// <param name="point">Nová pozice</param>
        public virtual void Move(Point point)
        {
            Canvas.SetLeft(this.Shape, point.X - this.Shape.Data.Bounds.X);
            Canvas.SetTop(this.Shape, point.Y - this.Shape.Data.Bounds.Y);
        }


        /// <summary>
        /// Provede rotaci entity kolem zadaného středu rotace
        /// </summary>
        /// <param name="angle">Úhel otočení</param>
        public virtual void Rotate(double angle)
        {
            this.ApplyTransfrom(new RotateTransform(angle - this.rotationAngle, this.rotationCenter.X, this.rotationCenter.Y));
            this.rotationAngle = angle;
        }


        /// <summary>
        /// Horizontální převrácení kolem středu entity
        /// </summary>
        public virtual void FlipHorizontaly()
        {
            this.originalWidth *= -1;
            this.ApplyTransfrom(new ScaleTransform(-1, 1, this.Width / 2, this.Height / 2));
        }


        /// <summary>
        /// Vertikální převrácení kolem středu entity
        /// </summary>
        public virtual void FlipVerticaly()
        {
            this.originalHeight *= -1;
            this.ApplyTransfrom(new ScaleTransform(1, -1, this.Width / 2, this.Height / 2));
        }


        /// <summary>
        /// Změna rozměrů
        /// </summary>
        /// <param name="scaleX">Horizontální změna</param>
        /// <param name="scaleY">Vertikální změna</param>
        public virtual void Scale(double scaleX, double scaleY)
        {
            this.originalWidth *= scaleX;
            this.originalHeight *= scaleY;
            this.ApplyTransfrom(new ScaleTransform(scaleX, scaleY));
        }

        #endregion
    }
}
