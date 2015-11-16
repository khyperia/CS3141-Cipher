using System.Xml.Linq;

namespace Cipher
{
    // Runtime configuration class
    static class Config
    {
        private const string configFilename = "config.xml";

        private static readonly XElement root;

        static Config()
        {
            // Load the config, otherwise make an empty one if failed
            try
            {
                root = XElement.Load(configFilename);
            }
            catch
            {
                root = new XElement("Cipher");
            }
        }

        // Save the config file
        private static void Save()
        {
            root.Save(configFilename);
        }

        // Forcibly set a value and save the config file afterwards
        public static void Set(string key, string value)
        {
            root.SetElementValue(key, value);
            Save();
        }

        // Get a key from the config file, and set a default if none exists (and then save)
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

        // Same as Get(), but parses with the parser and returns the result of parsing.
        // parser(x.ToString()) should return x.
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