using System;
using System.IO;
using Gtk;

namespace Cipher
{
    class MainClass
    {
        public static void Window()
        {
            Application.Init();

            var HelloWorld = new Window("Hello World");
            HelloWorld.Resize(500, 500);

            var button = new Button();
            button.Label = "I am a button!";
            HelloWorld.Add(button);

            HelloWorld.Destroyed += (o, e) =>
            {
                Application.Quit();
            };

            HelloWorld.ShowAll();

            Application.Run();
        }

        private static void OnRecv(Client client)
        {
            while (true)
            {
                var msg = client.TryGetNextMessage();
                if (msg.Key == null)
                {
                    return;
                }
                Console.WriteLine("<" + msg.Key + "> " + msg.Value);
            }
        }

        public static void Main(string[] args)
        {
            if (args.Length == 2 && args[0] == "server")
            {
                int port;
                if (!int.TryParse(args[1], out port))
                {
                    Console.WriteLine("Server usage: [exe name] server [portnum]");
                    return;
                }
                new Server(port).Run();
                return;
            }
            Console.WriteLine("Hello World from Team Cipher's Encrypted Messaging Service!");
            var encryptor = new EncryptionService();
            var network = (Client)null;
            while (true)
            {
                Console.WriteLine(" ~~~");
                Console.WriteLine(" Enter action:");
                Console.WriteLine("  connect [addr] [port] [username]");
                Console.WriteLine("  list");
                Console.WriteLine("  send [username] [message...]");
                Console.WriteLine("  sendf [username message is to] [file to write] [message...]");
                Console.WriteLine("  recvf [encrypted message file]");
                Console.WriteLine("  exportkey [keyfile to export to]");
                Console.WriteLine("  importkey [username key is from] [keyfile to import from]");
                Console.WriteLine("  window");
                Console.WriteLine("  quit");
                Console.WriteLine(" ~~~");
                Console.Write("> ");
                var readLine = Console.ReadLine();
                if (readLine == null)
                {
                    Console.WriteLine("quit");
                    return;
                }
                var message = readLine.Split(' ');
                if (message.Length == 0)
                {
                    continue;
                }
                switch (message[0])
                {
                    case "connect":
                        if (network == null)
                        {
                            int port;
                            if (message.Length != 4 || !int.TryParse(message[2], out port))
                            {
                                Console.WriteLine("Bad syntax");
                            }
                            else
                            {
                                network = new Client(message[1], port, encryptor, message[3], () => OnRecv(network));
                            }
                        }
                        else
                        {
                            Console.WriteLine("Already connected");
                        }
                        break;
                    case "list":
                        if (network != null)
                        {
                            network.UpdateKeyPairs();
                        }
                        foreach (var other in encryptor.LoadUserDatabase())
                        {
                            Console.WriteLine(other.Key);
                        }
                        break;
                    case "send":
                        if (network == null)
                        {
                            Console.WriteLine("Not connected");
                        }
                        else if (message.Length < 3)
                        {
                            Console.WriteLine("Bad syntax");
                        }
                        else
                        {
                            var theMessage = string.Join(" ", message, 2, message.Length - 2);
                            if (!network.SendMessage(message[1], theMessage))
                            {
                                Console.WriteLine("User not found");
                            }
                        }
                        break;
                    case "sendf":
                        if (message.Length <= 3)
                        {
                            Console.WriteLine("Bad syntax");
                        }
                        else
                        {
                            var theMessage = string.Join(" ", message, 3, message.Length - 3);
                            File.WriteAllBytes(message[2], encryptor.Encrypt(theMessage, message[1]));
                            Console.WriteLine("Encrypted.");
                        }
                        break;
                    case "recvf":
                        if (message.Length != 2)
                        {
                            Console.WriteLine("Bad syntax");
                        }
                        else
                        {
                            Console.WriteLine(" ----");
                            Console.WriteLine(encryptor.Decrypt(File.ReadAllBytes(message[1])));
                            Console.WriteLine(" ----");
                        }
                        break;
                    case "exportkey":
                        if (message.Length != 2)
                        {
                            Console.WriteLine("Bad syntax");
                        }
                        else
                        {
                            File.WriteAllText(message[1], encryptor.MyKey);
                            Console.WriteLine("Exported.");
                        }
                        break;
                    case "importkey":
                        if (message.Length != 3)
                        {
                            Console.WriteLine("Bad syntax");
                        }
                        else
                        {
                            encryptor.AddUser(message[1], File.ReadAllText(message[2]));
                            Console.WriteLine("Imported.");
                        }
                        break;
                    case "window":
                        Window();
                        break;
                    case "quit":
                        return;
                    default:
                        Console.WriteLine("Unknown command: " + message[0]);
                        break;
                }
            }
        }
    }
}
