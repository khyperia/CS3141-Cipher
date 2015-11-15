using System.Xml.Linq;

namespace Cipher
{
    static class Config
    {
        private const string configFilename = "config.xml";

        private static readonly XElement root;

        static Config()
        {
            try
            {
                root = XElement.Load(configFilename);
            }
            catch
            {
                root = new XElement("Cipher");
            }
        }

        private static void Save()
        {
            root.Save(configFilename);
        }

        public static void Set(string key, string value)
        {
            root.SetElementValue(key, value);
            Save();
        }

        public static string Get(string key, string defaultValue)
        {
            var value = root.Element(key);
            if (value != null)
            {
                return value.Value;
            }
            Set(key, defaultValue);
            return defaultValue;
        }
        
        public delegate bool TryParse<T>(string value, out T result);
        
        public static T Get<T>(string key, T defaultValue, TryParse<T> parser)
        {
            T result;
            if (parser(Get(key, defaultValue.ToString()), out result))
            {
                return result;
            }
            return defaultValue;
        }
    }
}