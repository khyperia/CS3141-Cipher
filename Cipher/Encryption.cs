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
            if (_userDatabaseCache == null)
            {
                var path = UserDatabaseName;
                var result = new HashSet<RemoteUser>();
                try
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
                catch (FileNotFoundException)
                {
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unable to load local user database: " + e.Message);
                }
                System.Threading.Interlocked.CompareExchange(ref _userDatabaseCache, result, null);
            }
            return new HashSet<RemoteUser>(_userDatabaseCache); // clone set
        }

        private void SaveUserDatabase(ISet<RemoteUser> toSave)
        {
            using (var stream = File.Open(UserDatabaseName, FileMode.Create))
            {
                var writer = new BinaryWriter(stream);
                foreach (var kvp in toSave)
                {
                    writer.Write(kvp.Username);
                    writer.Write(kvp.PublicKey);
                }
            }
            _userDatabaseCache = toSave;
        }

        public void AddUsers(IEnumerable<RemoteUser> remoteUsers)
        {
            var data = LoadUserDatabase();
            foreach (var remoteUser in remoteUsers)
            {
                data.Remove(remoteUser);
                data.Add(remoteUser);
            }
            SaveUserDatabase(data);
        }

        public void AddUser(string username, string privatekey)
        {
            var data = LoadUserDatabase();
            var user = new RemoteUser(username, privatekey);
            data.Remove(user);
            data.Add(user);
            SaveUserDatabase(data);
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

        public static byte[] Encrypt(byte[] value, string publicKey)
        {
            using (var theirKey = new RSACryptoServiceProvider())
            {
                theirKey.FromXmlString(publicKey);
                return theirKey.Encrypt(value, true);
            }
        }

        public static byte[] Encrypt(string value, RemoteUser toUser)
        {
            return Encrypt(Encoding.UTF8.GetBytes(value), toUser.PublicKey);
        }

        public byte[] DecryptBytes(byte[] value)
        {
            using (var myKey = PrivateKey)
            {
                return myKey.Decrypt(value, true);
            }
        }

        public string Decrypt(byte[] value)
        {
            return Encoding.UTF8.GetString(DecryptBytes(value));
        }
    }
}
