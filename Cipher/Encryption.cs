using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Cipher
{
    // Struct that contains a public key and username, plus all the .net machenery for custom equality
    // Also has ToString return Username
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

        public override string ToString()
        {
            return Username;
        }
    }

    class EncryptionService
    {
        ConcurrentDictionary<string, string> _userDatabase; // publickey to username
        private static readonly object _signObject = new SHA1CryptoServiceProvider();

        public EncryptionService()
        {
            _userDatabase = new ConcurrentDictionary<string, string>();
        }

        // Returns our private key
        private static RSACryptoServiceProvider PrivateKey
        {
            get
            {
                var cp = new CspParameters();
                cp.KeyContainerName = "CipherEncryptedMessaging";
                return new RSACryptoServiceProvider(cp);
            }
        }

        // Lists all locally cached users
        public IEnumerable<RemoteUser> LoadUsers()
        {
            foreach (var user in _userDatabase)
            {
                yield return new RemoteUser(user.Key, user.Value);
            }
        }

        // Adds a remote user
        public void AddUser(RemoteUser toAdd)
        {
            _userDatabase[toAdd.PublicKey] = toAdd.Username;
        }

        // Returns our public key as a string
        public string MyKey
        {
            get
            {
                return PrivateKey.ToXmlString(false);
            }
        }

        // Signed a binary blob with the private key
        public byte[] Sign(byte[] value)
        {
            using (var myKey = PrivateKey)
            {
                return myKey.SignData(value, _signObject);
            }
        }

        // Looks up a user by their name (zero to many results)
        public IEnumerable<RemoteUser> LookUpUser(string username)
        {
            foreach (var user in _userDatabase)
            {
                if (user.Value == username)
                {
                    yield return new RemoteUser(user.Key, user.Value);
                }
            }
        }

        // Verifies a blob/sig pair coming from a key
        public static bool VerifySigned(byte[] value, byte[] signature, string publickey)
        {
            using (var theirKey = new RSACryptoServiceProvider())
            {
                theirKey.FromXmlString(publickey);
                return theirKey.VerifyData(value, _signObject, signature);
            }
        }

        // Encrypts a binary blob with a public key
        public static byte[] Encrypt(byte[] value, string publicKey)
        {
            using (var theirKey = new RSACryptoServiceProvider())
            {
                theirKey.FromXmlString(publicKey);
                return theirKey.Encrypt(value, true);
            }
        }

        // Encrypts a string to a blob
        public static byte[] Encrypt(string value, RemoteUser toUser)
        {
            return Encrypt(Encoding.UTF8.GetBytes(value), toUser.PublicKey);
        }

        // Decrypts a blob using our private key
        public byte[] DecryptBytes(byte[] value)
        {
            using (var myKey = PrivateKey)
            {
                return myKey.Decrypt(value, true);
            }
        }

        // Decrypts a blob to a string using our private key
        public string Decrypt(byte[] value)
        {
            return Encoding.UTF8.GetString(DecryptBytes(value));
        }
    }
}
