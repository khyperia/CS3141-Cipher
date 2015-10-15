using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Cipher
{
    class EncryptionService
    {
        Dictionary<string, string> _userDatabaseCache;

        public EncryptionService()
        {
        }

        private static RSACryptoServiceProvider PrivateKey
        {
            get
            {
                var cp = new CspParameters();
                cp.KeyContainerName = "CipherEncryptedMessaging";
                return new RSACryptoServiceProvider(cp);
            }
        }

        private string UserDatabaseName
        {
            get
            {
                var specialFolder = Environment.SpecialFolder.LocalApplicationData;
                var path = Environment.GetFolderPath(specialFolder);
                path = Path.Combine(path, "CipherEMS");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                path = Path.Combine(path, "keyDatabase");
                return path;
            }
        }

        private Dictionary<string, string> LoadUserDatabase()
        {
            if (_userDatabaseCache != null)
                return _userDatabaseCache;
            var path = UserDatabaseName;
            var result = new Dictionary<string, string>();
            if (File.Exists(path))
            {
                using (var stream = File.OpenRead(path))
                {
                    var reader = new BinaryReader(stream);
                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        var name = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
                        var key = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
                        result.Add(name, key);
                    }
                }
            }
            _userDatabaseCache = result;
            return result;
        }

        private void SaveUserDatabase()
        {
            if (_userDatabaseCache == null || _userDatabaseCache.Count == 0)
            {
                return;
            }
            using (var stream = File.OpenWrite(UserDatabaseName))
            {
                var writer = new BinaryWriter(stream);
                foreach (var kvp in _userDatabaseCache)
                {
                    var nameBytes = Encoding.UTF8.GetBytes(kvp.Key);
                    writer.Write(nameBytes.Length);
                    writer.Write(nameBytes);
                    var keyBytes = Encoding.UTF8.GetBytes(kvp.Value);
                    writer.Write(keyBytes.Length);
                    writer.Write(keyBytes);
                }
            }
        }

        public void AddUser(string username, string privatekey)
        {
            var data = LoadUserDatabase();
            data.Add(username, privatekey);
            SaveUserDatabase();
        }

        public string MyKey
        {
            get
            {
                return PrivateKey.ToXmlString(false);
            }
        }

        public byte[] Encrypt(string value, string toUser)
        {
            var data = LoadUserDatabase();
            string key;
            if (!data.TryGetValue(toUser, out key))
            {
                return null;
            }
            using (var theirKey = new RSACryptoServiceProvider())
            {
                theirKey.FromXmlString(key);
                var bytes = Encoding.UTF8.GetBytes(value);
                return theirKey.Encrypt(bytes, false);
            }
        }

        public string Decrypt(byte[] value)
        {
            using (var myKey = PrivateKey)
            {
                return Encoding.UTF8.GetString(myKey.Decrypt(value, false));
            }
        }
    }
}
