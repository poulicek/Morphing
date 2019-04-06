using System;
using System.Collections.Generic;

using System.Text;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Media;
using VeNET.UserControls;

namespace VeNET.Objects
{
    public enum TransformLock
    {
        None,
        Horizontal,
        Vertical
    }

    public enum SelectionState
    {
        Draw,
        Resize,
        Rotate
    }


    public class Selection
    {
        public event EventHandler SelectionChanged;

        private SelectionBorder border = new SelectionBorder();
        private Point correctedMousePosition;
        private Cursor cursor;
        private Point defaultProportions;
        private Canvas drawCanvas;
        private List<Entity> entities = new List<Entity>();
        private bool flippedHorizontaly = false;
        private bool flippedVerticaly = false;
        private bool hitted;
        private Point position;
        private Point proportions;
        private Point rotationCenter;
        private double rotationAngle;

        // pevný bod transformace
        private Point solidPoint;

        // korekční úhel
        private double solidAngle;

        // delegát pro transformace
        public TransformFunction Transform;
        public delegate void TransformFunction(Point mousePosition);

        #region Vlastnosti

        /// <summary>
        /// Vrátí počet vybraných entit
        /// </summary>
        public int Count { get { return this.entities.Count; } }

        /// <summary>
        /// Vrátí nebo nastaví zamknutí transformace
        /// </summary>
        public TransformLock TransformLock { get; set; }

        /// <summary>
        /// Vrátí nebo nastaví střed rotace (hodnoty od 0.0 do 1.0)
        /// </summary>
        public Point RotationCenter
        {
            get { return this.rotationCenter; }
            set
            {
                this.rotationCenter = value;
                foreach (Entity entity in this.entities)
                    entity.RotationCenter = value;
            }
        }

        /// <summary>
        /// Nastaví bod aktivace výběru
        /// </summary>
        public Point HitPoint { get; set; }

        public Point CorrectedMousePosition
        {
            get { return this.correctedMousePosition; }
        }

        /// <summary>
        /// Vrátí nebo nastaví, zda je výběr aktivován myší
        /// </summary>
        public bool Hitted
        {
            get { return this.hitted || this.border.ActiveCorner != null; }
            set { this.hitted = value; }
        }

        /// <summary>
        /// Vrátí nebo nastaví aktuální stav výběru (druh transformace)
        /// </summary>
        public SelectionState State
        {
            get { return this.border.State; }
            set
            {
                if (value != this.border.State)
                {
                    switch (value)
                    {
                        case SelectionState.Resize: this.cursor = Cursors.SizeAll; break;
                        case SelectionState.Rotate: this.cursor = Cursors.Arrow; break;
                        default: this.cursor = this.border.Cursor; break;
                    }
                    this.border.State = value;
                    foreach (Entity entity in this.entities)
                        entity.SetCursor(this.cursor);
                }
            }
        }

        /// <summary>
        /// Vrátí vybranou entitu (v případě, že je jediná)
        /// </summary>
        public Entity Entity { get { return this.entities.Count == 1 ? this.entities[0] : null; } }

        /// <summary>
        /// Vrátí seznam vybraných entit
        /// </summary>
        public List<Entity> Entities { get { return this.entities; } }

        /// <summary>
        /// Vrátí nebo nastaví pozici výběru (ovlivní všechny vybrané entity)
        /// </summary>
        public Point Position
        {
            get { return position; }
            set
            {
                if (this.entities.Count == 1)
                    this.entities[0].Move(value);
                else
                {
                    double offsetX = value.X - this.position.X;
                    double offsetY = value.Y - this.position.Y;

                    foreach (Entity entity in this.entities)
                        entity.Move(new Point(entity.X + offsetX, entity.Y + offsetY));
                }
                this.border.Position = this.position = value;
                //this.raiseSelectionChanged();
            }
        }


        /// <summary>
        /// Vrátí nebo nastaví horizontální pozici výběru
        /// </summary>
        public double X
        {
            get { return this.position.X; }
            set { this.Position = new Point(value, this.position.Y); }
        }

        /// <summary>
        /// Vrátí nebo nastaví vertikální pozici výběru
        /// </summary>
        public double Y
        {
            get { return this.position.Y; }
            set { this.Position = new Point(this.position.X, value); }
        }

        /// <summary>
        /// Vrátí nebo nastaví rozměry objektů ve výběru (ovlivní všechny vybrané entity)
        /// </summary>
        public Point Proportions
        {
            get { return this.proportions; }
            set
            {
                double ratioX = (value.X == 0 ? value.X = 0.0001 : value.X) / this.proportions.X;
                double ratioY = (value.Y == 0 ? value.Y = 0.0001 : value.Y) / this.proportions.Y;

                if (this.entities.Count == 1)
                    this.entities[0].Scale(ratioX, ratioY);
                else
                {
                    foreach (Entity entity in this.entities)
                    {
                        entity.Move(new Point(this.position.X + ratioX * (entity.X - this.position.X), this.position.Y + ratioY * (entity.Y - this.position.Y)));
                        entity.Scale(ratioX, ratioY);
                    }
                }
                this.border.Proportions = this.proportions = new Point(value.X, value.Y);
                //this.raiseSelectionChanged();
            }
        }

        /// <summary>
        /// Vrátí nebo nastaví šířku výběru
        /// </summary>
        public double Width
        {
            get { return this.proportions.X; }
            set { this.Proportions = new Point(value, this.proportions.Y); }
        }

        /// <summary>
        /// Vrátí nebo nastaví výšku výběru
        /// </summary>
        public double Height
        {
            get { return this.proportions.Y; }
            set { this.Proportions = new Point(this.proportions.X, value); }
        }

        /// <summary>
        /// Vrátí nebo nastavá úhel rotace
        /// </summary>
        public double RotationAngle
        {
            get { return this.rotationAngle; }
            set
            {
                this.rotationAngle = value;
                foreach (Entity entity in this.entities)
                    entity.Rotate(value);
            }
        }

        /// <summary>
        /// Vrátí nebo nastaví zobrazování transformačního rámu
        /// </summary>
        public bool DisplayBorder
        {
            get { return this.drawCanvas != null && this.drawCanvas.Children.Contains(this.border); }
            set
            {
                if (!value)
                    this.border.Visibility = Visibility.Hidden;
                else
                {
                    this.computeSelectionParametres();
                    this.border.Visibility = Visibility.Visible;
                    if (!this.drawCanvas.Children.Contains(this.border))
                        this.drawCanvas.Children.Add(this.border);
                }
            }
        }


        /// <summary>
        /// Vrátí nebo nastaví výpň vybraných objektů
        /// </summary>
        public Brush Fill
        {
            get { return this.entities.Count > 1 || !(this.Entity is Interfaces.IFillable) ? null : ((Interfaces.IFillable)this.Entity).Fill; }
            set
            {
                foreach (Entity entity in this.entities)
                {
                    if (entity is Interfaces.IFillable)
                        ((Interfaces.IFillable)this.Entity).Fill = value;
                }
            }
        }



        /// <summary>
        /// Vrátí nebo nastaví obrysovou barvu vybraných objektů
        /// </summary>
        public Brush Stroke
        {
            get { return this.entities.Count > 1 ? null : this.Entity.Stroke; }
            set
            {
                foreach (Entity entity in this.entities)
                    entity.Stroke = value;
            }
        }


        /// <summary>
        /// Vrátí nebo nastaví tloušťku obrysu entity
        /// </summary>
        public double StrokeThicnkess
        {
            get { return this.entities.Count > 1 ? 0 : this.Entity.StrokeThickness; }
            set
            {
                foreach (Entity entity in this.entities)
                    entity.StrokeThickness = value;
            }
        }


        #endregion


        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="drawCanvas"></param>
        public Selection(Canvas drawCanvas)
        {
            this.drawCanvas = drawCanvas;
        }
        

        /// <summary>
        /// Konstruktor
        /// </summary>
        public Selection()
        {
        }


        #region Základní akce

        /// <summary>
        /// Inicializuje parametry výběru na původní hodnoty
        /// </summary>
        private void init()
        {
            this.position = this.proportions = new Point(double.NaN, double.NaN);
        }


        /// <summary>
        /// Vyvolá událost změnu výběru
        /// </summary>
        private void raiseSelectionChanged()
        {
            if (this.SelectionChanged != null)
                this.SelectionChanged(this, new EventArgs());
        }


        /// <summary>
        /// Nastaví vlastnost vybraným objektům pokud je to možné
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        public void SetProperty(string propertyName, object value)
        {
            foreach (Entity entity in this.entities)
            {
                PropertyInfo pi;
                if ((pi = entity.GetType().GetProperty(propertyName)) != null)
                    pi.SetValue(entity, value, null);
            }
            this.computeSelectionParametres();
        }


        /// <summary>
        /// Zjistí, zda je daná entita vybraná
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool Contains(Entity entity)
        {
            return this.entities.Contains(entity);
        }


        /// <summary>
        /// Vybere jednu entitu a zobrazí transformační rám
        /// </summary>
        /// <param name="shape">Vybíraná entita</param>
        public void Select(Entity entity)
        {
            this.drawCanvas.Children.Remove(this.border);
            this.State = SelectionState.Resize;
            this.entities.Clear();
            this.Add(entity);
        }


        /// <summary>
        /// Vybere jednu entitu
        /// </summary>
        /// <param name="entity">Vybíraná entita</param>
        /// <param name="displayBorder">Nasatvení zobrazení transformačního rámu</param>
        public void SelectWithoutBorder(Entity entity)
        {
            this.drawCanvas.Children.Remove(this.border);
            this.entities.Clear();

            this.entities.Add(entity);
            this.RotationCenter = new Point(0.5, 0.5);
            this.position = this.solidPoint = new Point(entity.X, entity.Y);
            this.proportions = new Point(entity.Width, entity.Height);
        }


        /// <summary>
        /// Přidá entitu do výběru
        /// </summary>
        /// <param name="entity"></param>
        public void Add(Entity entity)
        {
            this.entities.Add(entity);
            foreach (Entity gEntity in entity.GroupedEntities)
                this.entities.Add(gEntity);

            if (this.entities.Count > 1)
                this.computeSelectionParametres();
            else
            {
                this.border.Position = this.position = this.solidPoint = new Point(entity.X, entity.Y);
                this.border.Proportions = this.proportions = new Point(entity.Width, entity.Height);
                this.setRotationCenter();
                if (this.drawCanvas != null)
                    this.drawCanvas.Children.Add(this.border);
            }
            this.raiseSelectionChanged();
        }


        /// <summary>
        /// Akktualizuje výběr
        /// </summary>
        public void Refresh()
        {
            this.computeSelectionParametres();
        }


        /// <summary>
        /// Odebere entitu z výběru
        /// </summary>
        /// <param name="entity"></param>
        public void Unselect(Entity entity)
        {
            if (this.entities.Contains(entity))
            {
                if (this.entities.Count == 1)
                    this.UnselectAll();
                else
                {
                    this.entities.Remove(entity);
                    foreach (Entity gEntity in entity.GroupedEntities)
                        this.entities.Remove(gEntity);
                    this.computeSelectionParametres();
                }
                this.raiseSelectionChanged();
            }
        }


        /// <summary>
        /// Vymaže výběr entit a nastaví parametry do původního stavu
        /// </summary>
        public void UnselectAll()
        {
            this.drawCanvas.Children.Remove(this.border);
            this.entities.Clear();
            this.init();
            this.State = SelectionState.Resize;
            this.raiseSelectionChanged();
        }


        /// <summary>
        /// Sloučí vybrané entity
        /// </summary>
        public void Group()
        {
            foreach (Entity entity in this.entities)
            {
                entity.GroupedEntities.AddRange(this.entities);
                entity.GroupedEntities.Remove(entity);
            }
        }


        /// <summary>
        /// Rozdělí vybrané entity
        /// </summary>
        public void UnGroup()
        {
            foreach (Entity entity in this.entities)
                entity.GroupedEntities.Clear();
        }


        /// <summary>
        /// Přepočítá parametry výběru
        /// </summary>
        private void computeSelectionParametres()
        {
            Point startPosition = new Point(double.NaN, double.NaN);
            Point endPosition = new Point(double.NaN, double.NaN);

            foreach (Entity entity in this.entities)
            {
                Point tmpStartPos = new Point(entity.X, entity.Y);
                Point tmpEndPos = new Point(tmpStartPos.X + entity.Width, tmpStartPos.Y + entity.Height);

                if (double.IsNaN(startPosition.X) || tmpStartPos.X < startPosition.X)
                    startPosition.X = tmpStartPos.X;
                if (double.IsNaN(startPosition.Y) || tmpStartPos.Y < startPosition.Y)
                    startPosition.Y = tmpStartPos.Y;

                if (double.IsNaN(endPosition.X) || tmpEndPos.X > endPosition.X)
                    endPosition.X = tmpEndPos.X;
                if (double.IsNaN(endPosition.Y) || tmpEndPos.Y > endPosition.Y)
                    endPosition.Y = tmpEndPos.Y;
            }

            this.border.Position = this.position = startPosition;
            this.border.Proportions = this.proportions = new Point(endPosition.X - startPosition.X, endPosition.Y - startPosition.Y);
            this.setRotationCenter();
            this.raiseSelectionChanged();
        }

        #endregion

        #region Transformace

        /// <summary>
        /// Zvolí a inicializuje transformaci podle aktuálního stavu výběru
        /// </summary>
        /// <param name="mousePosition"></param>
        public void InitTransformation(Point mousePosition)
        {
            this.flippedHorizontaly = this.flippedVerticaly = false;
            if (this.State == SelectionState.Draw)
            {
                // změna proporcí nově kresleného objektu
                this.defaultProportions = new Point(1, 1);
                this.TransformLock = TransformLock.None;
                this.solidPoint = this.position;
                this.Transform = this.resizeToPoint;
                return;
            }
            else if (this.State == SelectionState.Resize)
            {                
                // změna pozice výběru
                if (this.border.Visibility == Visibility.Hidden || this.border.ActiveCorner == null)
                {
                    this.defaultProportions = new Point(1, 1);
                    this.border.Visibility = Visibility.Hidden;
                    this.solidPoint = mousePosition;
                    this.Transform = this.moveToPoint;
                    return;
                }
                
                // změna proporcí výběru
                this.defaultProportions = this.proportions;
                this.Transform = this.resizeToPoint;
                switch (this.border.ActiveCorner.Name)
                {
                    case "corner1":
                        this.drawCanvas.Cursor = Cursors.SizeNESW;
                        this.TransformLock = TransformLock.None;
                        this.solidPoint = new Point(this.position.X + this.proportions.X, this.position.Y + this.proportions.Y);
                        this.flippedHorizontaly = this.flippedVerticaly = true;
                        break;

                    case "corner2":
                        this.drawCanvas.Cursor = Cursors.SizeNWSE;
                        this.TransformLock = TransformLock.None;
                        this.solidPoint = new Point(this.position.X + this.proportions.X, this.position.Y);
                        this.flippedHorizontaly = true;
                        this.flippedVerticaly = false;
                        break;

                    case "corner3":
                        this.drawCanvas.Cursor = Cursors.SizeNWSE;
                        this.TransformLock = TransformLock.None;
                        this.solidPoint = new Point(this.position.X, this.position.Y + this.proportions.Y);
                        this.flippedHorizontaly = false;
                        this.flippedVerticaly = true;
                        break;

                    case "corner4":
                        this.drawCanvas.Cursor = Cursors.SizeNESW;
                        this.TransformLock = TransformLock.None;
                        this.solidPoint = this.position;
                        this.flippedHorizontaly = this.flippedVerticaly = false;
                        break;

                    case "corner5":
                        this.drawCanvas.Cursor = Cursors.SizeNS;
                        this.TransformLock = TransformLock.Horizontal;
                        this.solidPoint = new Point(this.position.X, this.position.Y + this.proportions.Y);
                        this.flippedVerticaly = true;
                        break;

                    case "corner6":
                        this.drawCanvas.Cursor = Cursors.SizeNS;
                        this.TransformLock = TransformLock.Horizontal;
                        this.solidPoint = this.position;
                        this.flippedVerticaly = false;
                        break;

                    case "corner7":
                        this.drawCanvas.Cursor = Cursors.SizeWE;
                        this.TransformLock = TransformLock.Vertical;
                        this.solidPoint = new Point(this.position.X + this.proportions.X, this.position.Y);
                        this.flippedHorizontaly = true;
                        break;

                    case "corner8":
                        this.drawCanvas.Cursor = Cursors.SizeWE;
                        this.TransformLock = TransformLock.Vertical;
                        this.solidPoint = this.position;
                        this.flippedHorizontaly = false;
                        break;

                    default :
                        this.Transform = delegate(Point point) { };
                        break;
                }
            }
            else
            {
                if (this.border.ActiveCorner == null)
                    this.Transform = delegate(Point point) { };
                else
                {
                    this.drawCanvas.Cursor = Cursors.Cross;
                    this.border.Visibility = Visibility.Hidden;                    
                    this.Transform = rotateToPoint;
                    this.solidAngle = 180 * Math.Atan(this.Width / this.Height) / Math.PI;

                    switch (this.border.ActiveCorner.Name)
                    {
                        case "corner1" :
                            this.solidAngle = 180 - this.solidAngle;
                            break;
                        case "corner2" :
                            break;
                        case "corner3" :
                            this.solidAngle -= 180;
                            break;
                        case "corner4" :
                            this.solidAngle = -this.solidAngle;
                            break;
                    }
                    this.solidAngle -= this.rotationAngle;
                }
            }
        }


        /// <summary>
        /// Nastaví střed rotace pro celý výběr
        /// </summary>
        private void setRotationCenter()
        {
            this.RotationCenter = new Point(this.position.X + this.proportions.X / 2, this.position.Y + this.proportions.Y / 2);
        }


        /// <summary>
        /// Upraví pozici myši dle zmáčknutých kláves
        /// </summary>
        /// <param name="mousePosition">Aktuální pozice myši</param>
        /// <returns></returns>
        private Point correctMousePosition(Point mousePosition)
        {
            if (this.TransformLock == TransformLock.None && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                double offsetX = (this.defaultProportions.Y / this.defaultProportions.X) * Math.Abs(mousePosition.X - this.solidPoint.X);
                double offsetY = (this.defaultProportions.X / this.defaultProportions.Y) * Math.Abs(mousePosition.Y - this.solidPoint.Y);

                mousePosition = this.State == SelectionState.Draw && this.Entity is Entities.LineEntity ?
                    new Point(offsetX < offsetY ? this.solidPoint.X : mousePosition.X, offsetY < offsetX ? this.solidPoint.Y : mousePosition.Y) :
                    new Point(offsetX > offsetY ? this.solidPoint.X + (mousePosition.X < this.solidPoint.X ? -offsetY : offsetY) : mousePosition.X, offsetY > offsetX ? this.solidPoint.Y + (mousePosition.Y < this.solidPoint.Y ? -offsetX : offsetX) : mousePosition.Y);
            }

            if ((Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            {
                double offsetX = mousePosition.X > this.solidPoint.X ? mousePosition.X - this.solidPoint.X - this.Width : - (this.solidPoint.X - mousePosition.X - this.Width);
                double offsetY = mousePosition.Y > this.solidPoint.Y ? mousePosition.Y - this.solidPoint.Y - this.Height : - (this.solidPoint.Y - mousePosition.Y - this.Height);
                this.solidPoint = new Point(this.TransformLock == TransformLock.Horizontal ? this.solidPoint.X : this.solidPoint.X - offsetX, this.TransformLock == TransformLock.Vertical ? this.solidPoint.Y : this.solidPoint.Y - offsetY);
            }

            return this.correctedMousePosition = mousePosition;
        }


        /// <summary>
        /// Změní rozměry výběru
        /// </summary>
        /// <param name="point">Bod pro změnu transformace</param>
        private void resizeToPoint(Point point)
        {
            point = this.correctMousePosition(point);
            double resizeX = this.TransformLock == TransformLock.Horizontal ? this.proportions.X : point.X - this.solidPoint.X;
            double resizeY = this.TransformLock == TransformLock.Vertical ? this.proportions.Y : point.Y - this.solidPoint.Y;

            if ((resizeX < 0 && !this.flippedHorizontaly) || (resizeX > 0 && this.flippedHorizontaly))
            {
                this.FlipHorizontaly();
                this.flippedHorizontaly = !this.flippedHorizontaly;
            }

            if ((resizeY < 0 && !this.flippedVerticaly) || (resizeY > 0 && this.flippedVerticaly))
            {
                this.FlipVerticaly();
                this.flippedVerticaly = !this.flippedVerticaly;
            }

            this.Proportions = new Point(Math.Abs(resizeX), Math.Abs(resizeY));
            this.Position = new Point(this.TransformLock == TransformLock.Horizontal || resizeX > 0 ? this.solidPoint.X : point.X, this.TransformLock == TransformLock.Vertical || resizeY > 0 ? this.solidPoint.Y : point.Y);
        }


        /// <summary>
        /// Přesune výběr
        /// </summary>
        /// <param name="point">Bod pro změnu transformace</param>
        private void moveToPoint(Point point)
        {
            this.Position = new Point(this.position.X + point.X - this.solidPoint.X, this.position.Y + point.Y - this.solidPoint.Y);
            this.solidPoint = point;
        }


        /// <summary>
        /// Provede rotaci výběru
        /// </summary>
        /// <param name="point">Bod pro změnu transformace</param>
        private void rotateToPoint(Point point)
        {
            double angle = 180 * Math.Atan((this.RotationCenter.X - point.X) / (point.Y - this.RotationCenter.Y)) / Math.PI;
            angle -= point.Y < this.rotationCenter.Y ? 180 : 0;
            this.RotationAngle =  angle - this.solidAngle;
        }


        /// <summary>
        /// Převrátí selekci podle jejího středu
        /// </summary>
        public void FlipHorizontaly()
        {
            foreach (Entity entity in this.entities)
            {
                entity.FlipHorizontaly();
                entity.X = 2 * this.position.X + this.proportions.X - (entity.X + entity.Width);
            }
        }


        /// <summary>
        /// Převrátí selekci podle jejího středu
        /// </summary>
        public void FlipVerticaly()
        {
            foreach (Entity entity in this.entities)
            {
                entity.FlipVerticaly();
                entity.Y = 2 * this.position.Y + this.proportions.Y - (entity.Y + entity.Height);
            }
        }

        #endregion
    }
}
