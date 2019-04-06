using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;
using System.IO;

namespace Morphing.Helpers
{
    #region ConfigurationList

    /// <summary>
    /// Pomocný seznam konfigurací
    /// </summary>
    public class ConfigurationList
    {
        public ConfigurationList()
        {
            this.Keys = new List<string>();
            this.Values = new List<object>();
        }


        public object this[string key]
        {
            get { return Keys.Contains(key) ? Values[Keys.IndexOf(key)] : null; }
            set
            {
                if (Keys.Contains(key))
                {
                    if (value != null && (!(value is string) || !String.IsNullOrEmpty((string)value)))
                        Values[Keys.IndexOf(key)] = value;
                    else
                    {
                        Values.RemoveAt(Keys.IndexOf(key));
                        Keys.Remove(key);
                    }
                }
                else if (value != null)
                {
                    Values.Add(value);
                    Keys.Add(key);
                }
            }
        }

        public List<string> Keys { get; set; }
        public List<object> Values { get; set; }
    }

    #endregion

    #region Configuration

    public static class Configuration
    {
        private static string configFile;
        private static string storagePath = System.Windows.Forms.Application.StartupPath; //Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Poulicek\\Reservation";


        /// <summary>
        /// Vrati adresar pro ulozeni uzivatelskych souboru
        /// </summary>
        public static string StoragePath
        {
            get { return storagePath; }
            set { storagePath = !String.IsNullOrEmpty(value) ? value : System.Windows.Forms.Application.StartupPath; }//Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Poulicek\\Reservation"; }
        }


        /// <summary>
        /// Vrati adresar pro ulozeni docasnych souboru
        /// </summary>
        public static string TmpPath
        {
            get { return storagePath + "\\tmp"; }
        }


        public static ConfigurationList Values { get; set; }


        static Configuration()
        {
            configFile = storagePath + "\\user.config";
            Load();
            
            if (Values["StoragePath"] != null)
                storagePath = (string)Values["StoragePath"];
        }


        /// <summary>
        /// Ulozi konfiguraci do xml souboru
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static void Save()
        {

            if (StoragePath.Length > 0 && !Directory.Exists(StoragePath))
                Directory.CreateDirectory(StoragePath);

            using (FileStream file = new FileStream(configFile, FileMode.Create, FileAccess.Write))
            {
                (new XmlSerializer(typeof(ConfigurationList))).Serialize(file, Values);
                file.Close();
            }
        }


        /// <summary>
        /// Načte konfiguraci z xml souboru
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static void Load()
        {
            try
            {
                using (FileStream file = new FileStream(configFile, FileMode.Open, FileAccess.Read))
                {
                    Values = (ConfigurationList)(new XmlSerializer(typeof(ConfigurationList))).Deserialize(file);
                    file.Close();
                }
            }
            catch
            {
                Values = new ConfigurationList();
            }
        }
    }

    #endregion
}
