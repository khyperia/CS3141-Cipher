using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Cipher
{
    class RemoteUser : IEquatable<RemoteUser>
    {
        public string PublicKey { get; }
        public string Username { get; }

        public RemoteUser(string publicKey, string username)
        {
            PublicKey = publicKey;
            Username = username;
        }

        public bool Equals(RemoteUser other)
        {
            return this == other;
        }

        public override bool Equals(object other)
        {
            var obj = other as RemoteUser;
            return obj != null && this == obj;
        }

        public override int GetHashCode()
        {
            return PublicKey.GetHashCode();
        }

        public static bool operator ==(RemoteUser left, RemoteUser right)
        {
            if ((object)left == null)
            {
                return (object)right == null;
            }
            else
            {
                return (object)right != null && left.PublicKey == right.PublicKey;
            }
        }

        public static bool operator !=(RemoteUser left, RemoteUser right)
        {
            return !(left == right);
        }
    }

    class EncryptionService
    {
        ISet<RemoteUser> _userDatabaseCache;
        private static readonly object _signObject = new SHA1CryptoServiceProvider();

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

        public ISet<RemoteUser> LoadUserDatabase()
        {
            if (_userDatabaseCache != null)
                return _userDatabaseCache;
            var path = UserDatabaseName;
            var result = new HashSet<RemoteUser>();
            if (File.Exists(path))
            {
                using (var stream = File.OpenRead(path))
                {
                    var reader = new BinaryReader(stream);
                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        try
                        {
                            var name = reader.ReadString();
                            var publickey = reader.ReadString();
                            result.Add(new RemoteUser(publickey, name));
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Local user database is corrupted: " + e.Message);
                            break;
                        }
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
                    writer.Write(kvp.Username);
                    writer.Write(kvp.PublicKey);
                }
            }
        }

        public void AddUsers(IEnumerable<RemoteUser> remoteUsers)
        {
            var data = LoadUserDatabase();
            foreach (var remoteUser in remoteUsers)
            {
                data.Add(remoteUser);
            }
            SaveUserDatabase();
        }

        public void AddUser(string username, string privatekey)
        {
            var data = LoadUserDatabase();
            data.Add(new RemoteUser(username, privatekey));
            SaveUserDatabase();
        }

        public string MyKey
        {
            get
            {
                return PrivateKey.ToXmlString(false);
            }
        }

        public byte[] Sign(byte[] value)
        {
            using (var myKey = PrivateKey)
            {
                return myKey.SignData(value, _signObject);
            }
        }

        public IEnumerable<RemoteUser> LookUpUser(string username)
        {
            var data = LoadUserDatabase();
            foreach (var remoteUser in data)
            {
                if (remoteUser.Username == username)
                {
                    yield return remoteUser;
                }
            }
        }

        public static bool VerifySigned(byte[] value, byte[] signature, string publickey)
        {
            using (var theirKey = new RSACryptoServiceProvider())
            {
                theirKey.FromXmlString(publickey);
                return theirKey.VerifyData(value, _signObject, signature);
            }
        }

        public byte[] Encrypt(string value, RemoteUser toUser)
        {
            using (var theirKey = new RSACryptoServiceProvider())
            {
                theirKey.FromXmlString(toUser.PublicKey);
                var bytes = Encoding.UTF8.GetBytes(value);
                return theirKey.Encrypt(bytes, true);
            }
        }

        public string Decrypt(byte[] value)
        {
            using (var myKey = PrivateKey)
            {
                return Encoding.UTF8.GetString(myKey.Decrypt(value, true));
            }
        }
    }
}
