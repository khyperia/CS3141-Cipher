using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MySql.Data.MySqlClient;
using System.Windows.Forms;

namespace Cipher
{
    static class NetworkHelper
    {
        public static void WriteLenBytes(this BinaryWriter writer, byte[] value)
        {
            writer.Write(value.Length);
            writer.Write(value);
        }

        public static byte[] ReadLenBytes(this BinaryReader reader)
        {
            var length = reader.ReadInt32();
            return reader.ReadBytes(length);
        }
    }

    class Client
    {
        private readonly EncryptionService encryption;
        private readonly TcpClient client;
        private readonly BinaryReader reader;
        private readonly BinaryWriter writer;
        private readonly Queue<KeyValuePair<string, string>> incomingMessages;
        private readonly Action onChange;
        private readonly Control invokeOnChangeObj;

        public Client(string server, int port, EncryptionService encryption, string myName, Action onChange, Control invokeOnChangeObj)
        {
            this.encryption = encryption;
            this.onChange = onChange;
            this.invokeOnChangeObj = invokeOnChangeObj;
            incomingMessages = new Queue<KeyValuePair<string, string>>();
            client = new TcpClient(server, port);
            var stream = client.GetStream();
            reader = new BinaryReader(stream);
            writer = new BinaryWriter(stream);
            Authenticate(myName);
            UpdateKeyPairs();
            var thread = new Thread(ProcessRecv);
            thread.IsBackground = true;
            thread.Start();
        }

        private void OnChange()
        {
            if (invokeOnChangeObj != null)
            {
                invokeOnChangeObj.BeginInvoke(onChange);
            }
            else
            {
                onChange();
            }
        }

        private void Authenticate(string myName)
        {
            var memstream = new MemoryStream();
            var stream = new BinaryWriter(memstream);
            stream.Write(myName);
            var bytes = memstream.ToArray();
            var sign = encryption.Sign(bytes);
            writer.Write(encryption.MyKey);
            writer.WriteLenBytes(bytes);
            writer.WriteLenBytes(sign);
        }

        private void AddIncomingMessage(string name, string message)
        {
            lock (incomingMessages)
            {
                incomingMessages.Enqueue(new KeyValuePair<string, string>(name, message));
            }
            OnChange();
        }

        private void ProcessListres(BinaryReader reader)
        {
            var dict = new List<RemoteUser>();
            var count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var name = reader.ReadString();
                var pubkey = reader.ReadString();
                dict.Add(new RemoteUser(pubkey, name));
            }
            encryption.AddUsers(dict);
            OnChange();
        }

        private void ProcessMsgRecv(BinaryReader reader)
        {
            var name = reader.ReadString();
            var message = reader.ReadLenBytes();
            var decrypt = encryption.Decrypt(message);
            AddIncomingMessage(name, decrypt);
        }

        private void ProcessUserNotFound(BinaryReader reader)
        {
            var sentTo = reader.ReadString();
            foreach (var user in encryption.LoadUserDatabase())
            {
                if (user.PublicKey == sentTo)
                {
                    AddIncomingMessage("[server]", "Sending to " + user.Username + " failed: user not online");
                }
            }
        }

        private void ProcessCmd(BinaryReader reader, BinaryWriter writer)
        {
            var cmd = reader.ReadString();
            if (string.Equals(cmd, "listres", StringComparison.OrdinalIgnoreCase))
            {
                ProcessListres(reader);
            }
            else if (string.Equals(cmd, "msgrecv", StringComparison.OrdinalIgnoreCase))
            {
                ProcessMsgRecv(reader);
            }
            else if (string.Equals(cmd, "usernotfound", StringComparison.OrdinalIgnoreCase))
            {
                ProcessUserNotFound(reader);
            }
            else
            {
                // Invalid command
            }
        }

        private void ProcessRecv()
        {
            try
            {
                while (true)
                {
                    ProcessCmd(reader, writer);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Disconnected from server:");
                Console.WriteLine(e);
            }
        }

        public void UpdateKeyPairs()
        {
            writer.Write("list");
        }

        public ISet<RemoteUser> GetUsers()
        {
            return encryption.LoadUserDatabase();
        }

        public IEnumerable<RemoteUser> LookUpUser(string username)
        {
            return encryption.LookUpUser(username);
        }

        public void SendMessage(RemoteUser toUser, string message)
        {
            var encrypt = EncryptionService.Encrypt(message, toUser);
            writer.Write("msg");
            writer.Write(toUser.PublicKey);
            writer.WriteLenBytes(encrypt);
        }

        public KeyValuePair<string, string> TryGetNextMessage()
        {
            lock (incomingMessages)
            {
                if (incomingMessages.Count == 0)
                {
                    return new KeyValuePair<string, string>(null, null);
                }
                return incomingMessages.Dequeue();
            }
        }
    }

    class Server
    {
        private readonly TcpListener serverSocket;

        private readonly List<ClientCon> clients;

        private string dbString;

        struct ClientCon
        {
            public TcpClient Client { get; }
            public BinaryWriter Writer { get; }
            public string Name { get; }
            public string Pubkey { get; }

            public ClientCon(TcpClient client, BinaryWriter writer, string name, string pubkey)
            {
                this.Client = client;
                Writer = writer;
                Name = name;
                Pubkey = pubkey;
            }
        }

        public Server(int port)
        {
            dbString = Config.Get("DbString", "server=localhost;uid=EMS;pwd=Team_cipher5;database=Cipher;");
            clients = new List<ClientCon>();
            serverSocket = new TcpListener(IPAddress.Any, port);
        }

        public void Run()
        {
            serverSocket.Start();
            while (true)
            {
                try
                {
                    var client = serverSocket.AcceptTcpClient();
                    var thread = new Thread(RunClientListen);
                    thread.IsBackground = true;
                    thread.Start(client);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine("Server shutting down");
                    break;
                }
            }
            serverSocket.Stop();
        }

        private void ProcessMsg(BinaryReader reader, ClientCon me)
        {
            var sendToKey = reader.ReadString();
            var message = reader.ReadLenBytes();
            lock (clients)
            {
                var found = false;
                foreach (var other in clients)
                {
                    if (other.Pubkey == sendToKey)
                    {
                        found = true;
                        other.Writer.Write("msgrecv");
                        other.Writer.Write(me.Name);
                        other.Writer.WriteLenBytes(message);
                    }
                }
                if (!found)
                {
                    me.Writer.Write("usernotfound");
                    me.Writer.Write(sendToKey);
                }
            }
        }

        private void ProcessList(ClientCon me)
        {
            lock (clients)
            {
                me.Writer.Write("listres");
                me.Writer.Write(clients.Count);
                foreach (var other in clients)
                {
                    me.Writer.Write(other.Name);
                    me.Writer.Write(other.Pubkey);
                }
            }
        }

        private void ProcessCmd(BinaryReader reader, ClientCon me)
        {
            var cmd = reader.ReadString();
            if (string.Equals(cmd, "msg", StringComparison.OrdinalIgnoreCase))
            {
                ProcessMsg(reader, me);
            }
            else if (string.Equals(cmd, "list", StringComparison.OrdinalIgnoreCase))
            {
                ProcessList(me);
            }
            else
            {
                Console.WriteLine("Unknown command by client: " + cmd);
            }
        }

        private ClientCon Authenticate(TcpClient tcpClient, BinaryWriter writer, BinaryReader reader)
        {
            var pubkey = reader.ReadString();
            var authToken = reader.ReadLenBytes();
            var signature = reader.ReadLenBytes();
            try
            {
                if (!EncryptionService.VerifySigned(authToken, signature, pubkey))
                {
                    return new ClientCon();
                }
                var authReader = new BinaryReader(new MemoryStream(authToken));
                var nick = authReader.ReadString();
                lock (clients)
                {
                    foreach (var other in clients)
                    {
                        if (other.Pubkey == pubkey && other.Name != nick)
                        {
                            Console.WriteLine(nick + " (" + tcpClient.Client.RemoteEndPoint +
                                "): Cannot log in with the same publickey as " + other.Name);
                            return new ClientCon();
                        }
                    }
                }
                switch (CheckDatabase(nick, pubkey))
                {
                    case 0:
                        return new ClientCon(tcpClient, writer, nick, pubkey);
                    case 1:
                        return new ClientCon(tcpClient, writer, nick, pubkey);
                    case 2:
                        // user connecting with public key that does not match the username
                        return new ClientCon();
                    default:
                        // something else happened
                        return new ClientCon();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new ClientCon();
            }
        }

        private void RunClientListen(object state)
        {
            var client = (TcpClient)state;
            var netstream = client.GetStream();
            var reader = new BinaryReader(netstream);
            var writer = new BinaryWriter(netstream);
            Console.WriteLine(client.Client.RemoteEndPoint + ": connected");
            try
            {
                var clientCon = Authenticate(client, writer, reader);
                if (clientCon.Name == null)
                {
                    throw new Exception("Authentication failure");
                }
                try
                {
                    lock (clients)
                    {
                        clients.Add(clientCon);
                    }
                    Console.WriteLine(client.Client.RemoteEndPoint + ": authed as " + clientCon.Name);
                    while (true)
                    {
                        ProcessCmd(reader, clientCon);
                    }
                }
                finally
                {
                    lock (clients)
                    {
                        clients.Remove(clientCon);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(client.Client.RemoteEndPoint + ": disconnected (" + e.Message + ")");
                try
                {
                    if (client.Connected)
                        client.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(client.Client.RemoteEndPoint + ": error when closing (" + ex.Message + ")");
                }
            }
        }

        private int CheckDatabase(string nick, string pubkey)
        {
            int result = -1;
            int numOfUsers = 0;
            var dbConnection = new MySqlConnection(dbString);
            try
            {
                dbConnection.Open();
                Console.WriteLine("Successfully connected to Database.");

                var cmd = dbConnection.CreateCommand();

                cmd.CommandText = "SELECT COUNT(*) FROM users WHERE username=@user";
                cmd.Parameters.AddWithValue("@user", nick);
                numOfUsers = Convert.ToInt32(cmd.ExecuteScalar());

                if (numOfUsers == 0)
                {
                    Console.WriteLine("New user detected.");
                    // insert user into db.
                    cmd = dbConnection.CreateCommand();
                    cmd.CommandText = "INSERT INTO users(username, public_key) VALUES (@user, @key)";
                    cmd.Parameters.AddWithValue("@user", nick.ToLower());
                    cmd.Parameters.AddWithValue("@key", pubkey);
                    cmd.ExecuteNonQuery();
                    result = 0;
                }
                else if (numOfUsers == 1)
                {
                    // user is already in db. check the key
                    Console.WriteLine("Returning user possibly detected.");
                    cmd = dbConnection.CreateCommand();
                    cmd.CommandText = "SELECT public_key FROM users WHERE username=@user";
                    cmd.Parameters.AddWithValue("@user", nick.ToLower());
                    string serverPublicKey = Convert.ToString(cmd.ExecuteScalar());
                    if (serverPublicKey == pubkey)
                    {
                        Console.WriteLine("Returning user confirmed.");
                        result = 1;
                    }
                    else
                    {
                        Console.WriteLine("Duplicate username detected.");
                        result = 2;
                    }
                }
                else
                {
                    // we should never get here.
                    result = -1;
                }
            }
            catch (MySqlException e)
            {
                Console.WriteLine(e);
                Console.WriteLine("MySql server connection failed.");
                result = -1;
            }
            finally
            {
                dbConnection.Close();
            }
            return result;
        }
    }
}
