using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Xml.Serialization;

namespace Morphing.Core
{
    public class Grid
    {
        #region Proporce

        // Rozmery mrizky
        private int width;
        private int height;

        // Rozliseni mrizky
        private int rows;
        private int columns;
        public int Rows { get { return rows; } set { rows = value; } }
        public int Columns { get { return columns; } set { columns = value; } }

        // Kroky mrizky
        private double rowStep;
        private double colStep;
        public double RowStep { get { return rowStep; } }
        public double ColStep { get { return colStep; } }

        #endregion


        /// <summary>
        /// Vrati nebo nastavi pole vrcholu mrizky
        /// </summary>
        [XmlIgnore]
        public Point[,] Nodes { get; set; }


        /// <summary>
        /// Vrati nebo nastavi referencni pole vrcholu mrizky
        /// </summary>
        [XmlIgnore]
        public Point[,] RefNodes { get; set; }
        

        /// <summary>
        /// Vrati nebo nastavi pole uzlu mrizky
        /// </summary>
        public Point[] RawNodeData
        {
            get
            {
                Point[] result = new Point[Nodes.Length];
                for (int row = 0; row < rows; row++)
                    for (int col = 0; col < columns; col++)
                        result[columns * row + col] = Nodes[row, col];
                return result;
            }
            set
            {
                Nodes = new Point[rows, columns];
                for (int i = 0; i < Nodes.Length; i++)
                    Nodes[(int)(i / columns), (int)(i % columns)] = value[i];
            }
        }


        /// <summary>
        /// Vrati nebo nastavi pole referencnich uzlu mrizky
        /// </summary>
        public Point[] RawRefNodeData
        {
            get
            {
                Point[] result = new Point[RefNodes.Length];
                for (int row = 0; row < rows; row++)
                    for (int col = 0; col < columns; col++)
                        result[columns * row + col] = RefNodes[row, col];
                return result;
            }
            set
            {
                RefNodes = new Point[rows, columns];
                for (int i = 0; i < RefNodes.Length; i++)
                    RefNodes[(int)(i / columns), (int)(i % columns)] = value[i];
            }
        }

        // Pole offsetu
        private Point[,] offsetMap;


        public Grid()
        {
        }


        public Grid(int gridResolutionX, int gridResolutionY)
        {
            SetResolution(gridResolutionX, gridResolutionY);
        }


        /// <summary>
        /// Vrati pole offsetu pro deformaci obrazku
        /// </summary>
        /// <returns>Pole offsetu</returns>
        public Point[,] GetOffsetMap(Point[,] nodes, int row1, int col1, int row2, int col2)
        {
            if (offsetMap == null)
                offsetMap = new Point[height, width];

            double xfrac = 1.0 / rowStep;
            double yfrac = 1.0 / colStep;

            Point p1, p2, p3, p4;
            double s, t, u, v;

            int offsetRow, offsetCol;
            int indexPixelRow, indexPixelCol;

            for (int row = row1; row < row2; ++row)
            {
                for (int col = col1; col < col2; ++col)
                {
                    // Ziskani hodnot jednotlivych offsetu
                    p1 = new Point(nodes[row, col].X - RefNodes[row, col].X, nodes[row, col].Y - RefNodes[row, col].Y);
                    p2 = new Point(nodes[row + 1, col].X - RefNodes[row + 1, col].X, nodes[row + 1, col].Y - RefNodes[row + 1, col].Y);
                    p3 = new Point(nodes[row, col + 1].X - RefNodes[row, col + 1].X, nodes[row, col + 1].Y - RefNodes[row, col + 1].Y);
                    p4 = new Point(nodes[row + 1, col + 1].X - RefNodes[row + 1, col + 1].X, nodes[row + 1, col + 1].Y - RefNodes[row + 1, col + 1].Y);

                    // Vynechani uzlu s nulovym offsetem
                    if (p1.X == 0 && p2.X == 0 && p3.X == 0 && p4.X == 0 &&
                        p1.Y == 0 && p2.Y == 0 && p3.Y == 0 && p4.Y == 0)
                        continue;

                    offsetRow = (int)Math.Round(row * rowStep);
                    offsetCol = (int)Math.Round(col * colStep);

                    for (int pixelRow = 0; pixelRow < rowStep; ++pixelRow)
                    {
                        for (int pixelCol = 0; pixelCol < colStep; ++pixelCol)
                        {
                            indexPixelRow = offsetRow + pixelRow;
                            indexPixelCol = offsetCol + pixelCol;

                            if (indexPixelRow >= height || indexPixelCol >= width)
                                continue;

                            s = (p2.X - p1.X) * pixelRow * xfrac + p1.X;
                            t = (p2.Y - p1.Y) * pixelRow * xfrac + p1.Y;
                            u = (p4.X - p3.X) * pixelRow * xfrac + p3.X;
                            v = (p4.Y - p3.Y) * pixelRow * xfrac + p3.Y;

                            offsetMap[indexPixelRow, indexPixelCol].X = -(int)Math.Round(s + (u - s) * pixelCol * yfrac);
                            offsetMap[indexPixelRow, indexPixelCol].Y = -(int)Math.Round(t + (v - t) * pixelCol * yfrac);
                        }
                    }
                }
            }
            return offsetMap;
        }


        /// <summary>
        /// Nastavi celkove rozmery mrizky v pixelem
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void SetProportions(int width, int height)
        {
            // Prepocitani vychylek
            if (this.width > 0 && this.height > 0)
            {
                double xRatio = (double)width / this.width;
                double yRatio = (double)height / this.height;

                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < columns; col++)
                    {
                        Nodes[row, col].X *= xRatio;
                        Nodes[row, col].Y *= yRatio;
                    }
                }
            }

            this.width = width;
            this.height = height;

            rowStep = (double)height / (rows - 1);
            colStep = (double)width / (columns - 1);

            this.offsetMap = null;
        }


        /// <summary>
        /// Nastavi rozliseni mrizky
        /// </summary>
        /// <param name="rows">Pocet radku</param>
        /// <param name="columns">Pocet sloupcu</param>
        public void SetResolution(int gridResolutionX, int gridResolutionY)
        {
            if (gridResolutionX > 0 && gridResolutionY > 0)
            {
                rowStep = (double)height / gridResolutionY;
                colStep = (double)width / gridResolutionX;

                rows = gridResolutionY + 1;
                columns = gridResolutionX + 1;

                Reset();
            }
        }


        /// <summary>
        /// Uvede mrizku do puvodni podoby
        /// </summary>
        public void Reset()
        {
            this.offsetMap = null;
            this.Nodes = new Point[rows, columns];
            this.RefNodes = new Point[rows, columns];
        }


        /// <summary>
        /// Uvolni prostredky
        /// </summary>
        public void FreeResources()
        {
            offsetMap = null;
        }
    }
}
