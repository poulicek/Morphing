using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Input;
using VeNET.Objects.Entities;
using VeNET.Dialogs;

namespace VeNET.Objects
{
    public enum DrawMode
    {
        Bitmap,
        Select,
        Line,
        Rectangle,
        Ellipse,
        Polygon,
        Dimension,
        Text
    }


    public class DrawingManager
    {
        private Document document;
        private DrawMode mode;
        private Canvas canvas;
        private Selection selection;
        private Cursor cursor;
        private Cursor entityCursor;
        private PointCollection polygonPoints = new PointCollection();
        private PageEntity page;

        #region Zpracování událostí

        /// <summary>
        /// Rozhraní pro přiřazování akcí k událostem myši
        /// </summary>
        private MouseEventHandler mouseMove;
        private MouseButtonEventHandler mouseLeftButtonDown;
        private MouseButtonEventHandler mouseLeftButtonUp;

        private MouseButtonEventHandler MouseLeftButtonDown
        {
            set
            {
                if (this.mouseLeftButtonDown != null)
                    this.canvas.MouseLeftButtonDown -= this.mouseLeftButtonDown;
                if ((this.mouseLeftButtonDown = value) != null)
                    this.canvas.MouseLeftButtonDown += value;
            }
        }

        private MouseButtonEventHandler MouseLeftButtonUp
        {
            set
            {
                if (this.mouseLeftButtonUp != null)
                    this.canvas.MouseLeftButtonUp -= this.mouseLeftButtonUp;
                if ((this.mouseLeftButtonUp = value) != null)
                    this.canvas.MouseLeftButtonUp += value;
            }
        }

        private MouseEventHandler MouseMove
        {
            set
            {
                if (this.mouseMove != null)
                    this.canvas.MouseMove -= this.mouseMove;
                if ((this.mouseMove = value) != null)
                    this.canvas.MouseMove += value;
            }
        }

        #endregion

        #region Vlastnosti

        /// <summary>
        /// Objekt Selection
        /// </summary>
        public Selection Selection
        {
            get { return this.selection; }
        }

        /// <summary>
        /// Objekt Dokument
        /// </summary>
        public Document Document
        {
            get { return document; }
            set
            {
                this.selection.UnselectAll();
                this.selection.DisplayBorder = false;
                this.canvas.Children.Clear();

                if ((this.document = value) != null)
                {
                    //this.canvas.Background = value.Background;
                    foreach (Entity entity in value.SortedEntities)
                    {
                        this.drawEntity(entity);
                        entity.SetCursor(this.entityCursor);
                        this.selection.Add(entity);
                    }
                    this.CorrectCanvasSize();
                    this.selection.UnselectAll();
                }
            }
        }


        /// <summary>
        /// Mód kreslení
        /// </summary>
        public DrawMode Mode
        {
            get { return this.mode; }
            set
            {
                this.mode = value;
                switch (value)
                {
                    case DrawMode.Select:
                        this.selection.State = SelectionState.Resize;
                        this.MouseLeftButtonDown = selectionStart;
                        this.cursor = this.canvas.Cursor = Cursors.Arrow;
                        if (document.Entities.Count > 0)
                            this.selection.DisplayBorder = true;
                        break;

                    default:
                        this.selection.State = SelectionState.Draw;
                        this.MouseLeftButtonDown = drawingStart;
                        this.cursor = this.canvas.Cursor = Cursors.Cross;
                        break;
                }

                Cursor cursor = value == DrawMode.Select ? Cursors.SizeAll : this.canvas.Cursor;
                if (this.entityCursor != cursor)
                {
                    this.entityCursor = cursor;
                    if (this.document != null)
                        foreach (Entity entity in this.document.Entities.Values)
                            entity.SetCursor(cursor);
                }
            }
        }

        #endregion


        public DrawingManager(Canvas canvas)
        {
            this.canvas = canvas;
            this.selection = new Selection(this.canvas);
        }



        /// <summary>
        /// Upraví velikost canvas dle umístěných objektů
        /// </summary>
        public void CorrectCanvasSize()
        {
            return;
            double endPos;
            Selection selection = new Selection();

            this.canvas.UpdateLayout();
            foreach (Entity entity in this.document.Entities.Values)
                selection.Add(entity);

            if ((endPos = this.selection.X + selection.Width) > this.canvas.RenderSize.Width)
                this.canvas.Width = endPos;
            if ((endPos = this.selection.Y + selection.Height) > this.canvas.RenderSize.Height)
                this.canvas.Height = endPos;
            
        }


        #region Zpracování výběru objektů

        /// <summary>
        /// Inicializace výběru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void selectionStart(object sender, MouseButtonEventArgs e)
        {
            if (!this.selection.Hitted)
            {
                this.selection.State = SelectionState.Resize;
                this.selection.UnselectAll();
            }
            else
            {
                this.selection.InitTransformation(e.GetPosition(this.canvas));
                this.MouseLeftButtonUp = this.selectionStop;
                this.MouseMove = this.selectionProcess;
            }
        }


        /// <summary>
        /// Transformace výběru dle pohybu myši
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void selectionProcess(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.selection.Transform(e.GetPosition(this.canvas));
            else
                this.selectionStop(sender, null);
        }


        /// <summary>
        /// Ukončení transformace výběru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void selectionStop(object sender, MouseButtonEventArgs e)
        {
            this.CorrectCanvasSize();
            this.MouseMove = null;
            this.MouseLeftButtonUp = null;
            this.selection.DisplayBorder = true;
            this.canvas.Cursor = this.cursor;
        }

        #endregion

        #region Vykreslování objektů

        /// <summary>
        /// Inicializuje kreslení
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void drawingStart(object sender, MouseButtonEventArgs e)
        {
            Point mousePosition = e.GetPosition(this.canvas);
            this.selection.UnselectAll();
            this.selection.State = SelectionState.Draw;
            this.selection.InitTransformation(e.GetPosition(this.canvas));

            switch (this.mode)
            {
                case DrawMode.Bitmap:
                    BitmapEntity bitmap = new BitmapEntity();
                    System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
                    dialog.Filter = "Soubory obrázků|*.jpg;*.jpeg;*.gif;*.png;*.tif;*.tiff;*.bmp|Všechny soubory|*.*";
                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        bitmap.LoadFromFile(dialog.FileName);
                        this.document.AddEntity(bitmap);
                        this.drawNewEntity(bitmap, mousePosition);
                        this.drawingStop(sender, e);
                    }
                    break;

                case DrawMode.Text:
                    TextEntity text = new TextEntity();
                    TextInput dlg = new TextInput();
                    if (dlg.ShowDialog().Value)
                    {
                        text.Text = dlg.Text;
                        if (!String.IsNullOrEmpty(text.Text))
                        {
                            this.document.AddEntity(text);
                            this.drawNewEntity(text, mousePosition);
                            this.drawingStop(sender, e);
                        }
                    }
                    break;

                case DrawMode.Line:
                    this.drawNewEntity(this.document.AddNewEntity<LineEntity>(), mousePosition);
                    this.MouseLeftButtonUp = drawingStop;
                    this.MouseMove = drawingProcess;
                    break;

                case DrawMode.Rectangle:
                    this.drawNewEntity(this.document.AddNewEntity<RectangleEntity>(), mousePosition);
                    this.MouseLeftButtonUp = drawingStop;
                    this.MouseMove = drawingProcess;
                    break;

                case DrawMode.Ellipse:
                    this.drawNewEntity(this.document.AddNewEntity<EllipseEntity>(), mousePosition);
                    this.MouseLeftButtonUp = drawingStop;
                    this.MouseMove = drawingProcess;
                    break;
                case DrawMode.Dimension:
                    this.drawNewEntity(this.document.AddNewEntity<DimensionEntity>(), mousePosition);
                    this.MouseLeftButtonUp = drawingStop;
                    this.MouseMove = drawingProcess;
                    break;

                case DrawMode.Polygon:
                    if (this.polygonPoints.Count == 0)
                        this.MouseMove = delegate(object _sender, System.Windows.Input.MouseEventArgs _e) { this.selection.Transform(e.GetPosition(this.canvas)); };
                    else
                    {
                        mousePosition = this.selection.CorrectedMousePosition;
                        if (this.polygonPoints.Count > 2 && mousePosition == this.polygonPoints[this.polygonPoints.Count - 1])
                        {
                            this.drawPolygonEntity(this.polygonPoints);
                            this.drawingStop(sender, e);
                            break;
                        }
                    }

                    Entity line = new LineEntity();
                    line.Move(mousePosition);
                    this.polygonPoints.Add(mousePosition);
                    this.canvas.Children.Add(line.Shape);
                    this.selection.SelectWithoutBorder(line);
                    break;
            }
        }       


        /// <summary>
        /// Vykreslení objektu dle tažení myši
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void drawingProcess(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.selection.Transform(e.GetPosition(this.canvas));
            else
                this.drawingStop(sender, null);
        }


        /// <summary>
        /// Ukončení kreslení
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void drawingStop(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.CorrectCanvasSize();
            this.MouseMove = null;
            this.MouseLeftButtonUp = null;

            if (this.mode == DrawMode.Polygon)
            {
                this.canvas.Children.RemoveRange(this.canvas.Children.Count - this.polygonPoints.Count - 1, this.polygonPoints.Count);
                this.polygonPoints.Clear();
            }

            Path shape = (Path)this.canvas.Children[this.canvas.Children.Count - 1];
            if (shape.Width == 0 && shape.Height == 0)
                this.deleteShape(shape);
        }

        #endregion

        #region Akce nad entitami

        /// <summary>
        /// Smaže vybrané entity
        /// </summary>
        public void DeleteSelectedEntities()
        {
            foreach (Entity entity in this.selection.Entities)
            {
                this.deleteShape(entity.Shape);
                if (entity is DimensionEntity)
                    this.canvas.Children.Remove(((DimensionEntity)entity).Label);
            }
            this.selection.UnselectAll();
        }


        /// <summary>
        /// Vykreslí danou entitu
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="position"></param>
        private void drawEntity(Entity entity)
        {
            entity.ClearEventDelegates();
            entity.MouseButtonUp += new MouseButtonEventHandler(entityMouseLeftButtonUp);
            entity.MouseButtonDown += new MouseButtonEventHandler(entityMouseLeftButtonDown);
            if (entity is DimensionEntity)
            {
                ((DimensionEntity)entity).MouseDoubleClick += new MouseButtonEventHandler(dimensionMouseDoubleClick);
                this.canvas.Children.Add(((DimensionEntity)entity).Label);
            }
            this.canvas.Children.Add(entity.Shape);
        }


        /// <summary>
        /// Vykreslí novou entitu
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="position"></param>
        private void drawNewEntity(Entity entity, Point position)
        {
            entity.Move(position);
            this.selection.SelectWithoutBorder(entity);
            this.drawEntity(entity);
        }


        /// <summary>
        /// Vytvoří polygon
        /// </summary>
        /// <param name="points">Kolekce bodů polygonu</param>
        /// <returns>Polygon</returns>
        private void drawPolygonEntity(PointCollection points)
        {

            Point startPosition = new Point(double.NaN, double.NaN);
            Point endPosition = new Point(double.NaN, double.NaN);

            foreach (Point vertex in points)
            {
                if (double.IsNaN(startPosition.X) || vertex.X < startPosition.X)
                    startPosition.X = vertex.X;
                if (double.IsNaN(startPosition.Y) || vertex.Y < startPosition.Y)
                    startPosition.Y = vertex.Y;
                if (double.IsNaN(endPosition.X) || vertex.X > endPosition.X)
                    endPosition.X = vertex.X;
                if (double.IsNaN(endPosition.Y) || vertex.Y > endPosition.Y)
                    endPosition.Y = vertex.Y;
            }

            PolygonEntity entity = Document.AddNewEntity<PolygonEntity>();
            entity.Move(startPosition);
            double width = entity.Width = endPosition.X - startPosition.X;
            double height = entity.Height = endPosition.Y - startPosition.Y;

            foreach (Point vertex in points)
                entity.AddPoint(new Point((vertex.X - startPosition.X) / width, (vertex.Y - startPosition.Y) / height));
            this.drawEntity(entity);
            this.selection.SelectWithoutBorder(entity);
        }


        /// <summary>
        /// Odstraní objekt z plátna i dokumentu
        /// </summary>
        /// <param name="shape"></param>
        private void deleteShape(Path shape)
        {
            this.document.Entities.Remove(shape);
            this.canvas.Children.Remove(shape);
        }

        #endregion

        #region Události myši nad entitami
        
        private void dimensionMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TextInput dlg = new TextInput();
            dlg.Text = ((DimensionEntity)this.selection.Entity).Text;
            if (dlg.ShowDialog().Value)
                ((DimensionEntity)this.selection.Entity).Text = dlg.Text;
            this.selection.Hitted = false;
        }


        private void entityMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.mode == DrawMode.Select)
            {
                this.selection.DisplayBorder = false;

                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ||
                    Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    if (this.selection.Entities.Contains((Entity)sender))
                        this.selection.Unselect((Entity)sender);
                    else
                        this.selection.Add((Entity)sender);
                }
                else
                {
                    if (!this.selection.Contains((Entity)sender))
                        this.selection.Select((Entity)sender);
                    else
                        ((Entity)sender).MouseButtonUp += new MouseButtonEventHandler(entitySwitchBorderState);
                }
                this.selection.HitPoint = e.GetPosition(this.canvas);
                this.selection.Hitted = true;
            }
        }


        private void entityMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.selection.Hitted = false;
        }


        private void entitySwitchBorderState(object sender, MouseButtonEventArgs e)
        {
            if (this.mode == DrawMode.Select)
            {
                if (this.selection.HitPoint == e.GetPosition(this.canvas) && e.ChangedButton == MouseButton.Left)
                    this.selection.State = this.selection.State == SelectionState.Rotate ? SelectionState.Resize : SelectionState.Rotate;
                ((Entity)sender).MouseButtonUp -= new MouseButtonEventHandler(entitySwitchBorderState);
            }
        }

        #endregion

        #region Práce se schránkou

        /// <summary>
        /// Zkopíruje vybrané entity do schránky
        /// </summary>
        public void Copy()
        {
            /*
            if (this.selection.Entities.Count > 0)
                Modules.DLLInterface.ClipboardSave(Document.SortByZIndex(this.selection.Entities));
             */
        }


        /// <summary>
        /// Vyjme vybrané entity
        /// </summary>
        public void Cut()
        {
            this.Copy();
            this.DeleteSelectedEntities();
            this.selection.UnselectAll();
        }


        /// <summary>
        /// Vloží entity do dokumentu
        /// </summary>
        /// <param name="entities"></param>
        public void Paste()
        {
            /*
            List<Entity> entities = Modules.DLLInterface.CLipboardOpen();
            this.selection.UnselectAll();
            foreach (Entity entity in entities)
            {
                entity.SetCursor(this.entityCursor);
                this.drawEntity(entity);
                this.document.AddEntity(entity);
                this.selection.Add(entity);
            }
            this.CorrectCanvasSize();
             */
        }

        #endregion
    }
}
