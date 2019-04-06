using System;
using System.Collections.Generic;

using System.Text;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VeNET.Objects.Entities
{
    public class BitmapEntity : Entity
    {
        BitmapImage bitmap = new BitmapImage();

        public BitmapImage Bitmap
        {
            get { return this.bitmap; }
            set
            {
                this.bitmap = value;
                this.Width = this.bitmap.Width;
                this.Height = this.bitmap.Height;
                this.Shape.Fill = new ImageBrush(this.bitmap);
            }
        }


        public BitmapEntity()
            : base("Bitmap")
        {
            this.setGeometry(new RectangleGeometry(new Rect(new Point(0, 0), new Point(1, 1)), 0, 0));
            this.StrokeThickness = 0;
            this.Shape.Fill = new ImageBrush();
        }


        /// <summary>
        /// Načte obrázek ze zadané cesty
        /// </summary>
        public void LoadFromFile(string path)
        {
            this.Bitmap = new BitmapImage(new Uri(path));
        }


        /// <summary>
        /// Aplikuje transformaci
        /// </summary>
        /// <param name="transform"></param>
        public override void ApplyTransfrom(Transform transform)
        {
            base.ApplyTransfrom(transform);
        }

        public override void Rotate(double angle)
        {
            base.Rotate(angle);
            this.Shape.Fill.Transform = new RotateTransform(this.rotationAngle, this.rotationCenter.X, this.rotationCenter.Y);
        }
    }
}
