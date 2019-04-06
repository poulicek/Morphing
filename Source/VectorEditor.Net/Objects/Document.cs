using System;
using System.Collections.Generic;

using System.Text;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;

namespace VeNET.Objects
{
    public class Document
    {
        #region Vlastnosti

        public string Title { get; set; }
        public string Path { get; set; }
        public Brush Background { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public Dictionary<Path, Entity> Entities { get; set; }

        /// <summary>
        /// Vrátí seřazený seznam entit podle XIndexu
        /// </summary>
        public List<Entity> SortedEntities
        {
            get { return SortByZIndex(this.Entities.Values); }
        }

        #endregion


        public Document(string title)
        {
            this.Title = title;
            this.Background = Brushes.White;
            this.Width = 700;
            this.Height = 450;
            this.Entities = new Dictionary<Path, Entity>();
        }


        /// <summary>
        /// Seřadí seznam entit podle z-indexu
        /// </summary>
        /// <param name="entitites"></param>
        /// <returns></returns>
        public static List<Entity> SortByZIndex(IEnumerable<Entity> entities)
        {
            List<Entity> returnList = new List<Entity>(entities);

            bool sorted = false;
            Entity pom;
            while (!sorted)
            {
                sorted=true;
                for(int i=0; i<returnList.Count-1; i++)
                {
                    if (returnList[i].ZIndex > returnList[i + 1].ZIndex)
                    {
                        sorted = false;
                        pom = returnList[i + 1];
                        returnList[i + 1] = returnList[i];
                        returnList[i] = pom;                            
                    }
                }
            }


            return returnList;
        }


        /// <summary>
        /// Přidá entitu do dokumentu
        /// </summary>
        /// <param name="entity"></param>
        public void AddEntity(Entity entity)
        {
            this.Entities.Add(entity.Shape, entity);
        }


        /// <summary>
        /// Vytvoří a přidá novou entitu do dokumentu
        /// </summary>
        /// <typeparam name="T">Typ entity</typeparam>
        /// <returns>Vytvořená entita</returns>
        public T AddNewEntity<T>()
            where T : Entity, new()
        {
            T entity = new T();
            entity.ZIndex = this.Entities.Count;
            this.Entities.Add(entity.Shape, entity);
            return entity;
        }
    }
}

