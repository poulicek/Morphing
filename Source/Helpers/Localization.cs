using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace Morphing.Helpers
{
    public static class Localization
    {
        /// <summary>
        /// Loads localization file
        /// </summary>
        /// <param name="fileName"></param>
        public static void Load(string fileName)
        {
            foreach (ResourceDictionary dictionary in Application.Current.Resources.MergedDictionaries)
                if (dictionary.Source.ToString().StartsWith("Languages"))
                    dictionary.Source = new Uri(fileName, UriKind.Relative);
        }
    }
}
