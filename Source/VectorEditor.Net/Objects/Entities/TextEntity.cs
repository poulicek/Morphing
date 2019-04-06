using System;
using System.Collections.Generic;

using System.Text;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;

namespace VeNET.Objects.Entities
{
    public class TextEntity : Entity, Interfaces.IFillable
    {
        private bool bold = false;
        private bool italic = false;
        private bool underline = false;
        private double fontSize = 20;
        private string fontFamily = "Arial";
        private FormattedText formatedText;


        /// <summary>
        /// Vrátí nebo nastavý výplň
        /// </summary>
        public Brush Fill
        {
            get { return this.Shape.Fill; }
            set { this.Shape.Fill = value; }
        }


        /// <summary>
        /// Změní zobrazovaný text
        /// </summary>
        public string Text
        {
            get { return this.formatedText == null ? String.Empty : this.formatedText.Text; }
            set
            {
                this.formatedText = value.Trim().Length > 0 ? new FormattedText(value, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.fontFamily), this.fontSize, Brushes.Black) : null;
                this.FontFamily = this.fontFamily;
                this.FontSize = this.fontSize;
                this.Bold = this.bold;
                this.Italic = this.italic;
                this.Underline = this.underline;
                this.refreshGeometry();
            }
        }


        /// <summary>
        /// Vrátí nebo nastaví druh písma celého textu
        /// </summary>
        public string FontFamily
        {
            get { return this.fontFamily; }
            set
            {
                this.fontFamily = value;
                if (this.formatedText != null)
                {
                    this.formatedText.SetFontFamily(value);
                    this.refreshGeometry();
                }
            }
        }


        /// <summary>
        /// Vrátí nebo nastaví velikost písma celého textu
        /// </summary>
        public double FontSize
        {
            get { return this.fontSize; }
            set
            {
                this.fontSize = value;
                if (this.formatedText != null)
                {
                    this.formatedText.SetFontSize(value);
                    this.refreshGeometry();

                }
            }
        }


        /// <summary>
        /// Vrátí nebo nastaví, zda je celý text vykreslen tlustě
        /// </summary>
        public bool Bold
        {
            get { return this.bold; }
            set
            {
                this.bold = value;
                if (this.formatedText != null)
                {
                    this.formatedText.SetFontWeight(value ? FontWeights.Bold : FontWeights.Normal);
                    this.refreshGeometry();
                }
            }
        }

        /// <summary>
        /// Vrátí nebo nastaví, zda je celý text vykreslen kurzívou
        /// </summary>
        public bool Italic
        {
            get { return this.italic; }
            set
            {
                this.italic = value;
                if (this.formatedText != null)
                {
                    this.formatedText.SetFontStyle(value ? FontStyles.Italic : FontStyles.Normal);
                    this.refreshGeometry();
                }
            }
        }

        /// <summary>
        /// Vrátí nebo nastaví, zda je celý text podtržen
        /// </summary>
        public bool Underline
        {
            get { return this.underline; }
            set
            {
                this.underline = value;
                if (this.formatedText != null)
                {
                    TextDecorationCollection decorations = null;
                    if (value)
                    {
                        decorations = new TextDecorationCollection();
                        decorations.Add(TextDecorations.Underline);
                    }
                    this.formatedText.SetTextDecorations(decorations);
                    this.refreshGeometry();
                }
            }
        }


        public TextEntity()
            : base("Text")
        {
        }


        /// <summary>
        /// Aktualizuje geometrii textu
        /// </summary>
        private void refreshGeometry()
        {
            if (this.formatedText != null)
            {
                Geometry geometry = this.formatedText.BuildGeometry(new Point(0, 0));
                if (this.Shape.Data != null)
                    geometry.Transform = new MatrixTransform(this.Shape.Data.Transform.Value);
                this.setGeometry(geometry);
            }
        }
    }
}
