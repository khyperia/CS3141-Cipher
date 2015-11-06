using System;
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

        private static void OnChange(Client client)
        {
            Console.WriteLine();
            var any = false;
            while (true)
            {
                var msg = client.TryGetNextMessage();
                if (msg.Key == null)
                {
                    break;
                }
                any = true;
                Console.WriteLine("<" + msg.Key + "> " + msg.Value);
            }
            if (!any)
            {
                foreach (var other in client.GetUsers())
                {
                    Console.WriteLine(other.Username);
                }
            }
            Console.Write("> ");
        }

        private static void Help()
        {
            Console.WriteLine(" Enter action:");
            Console.WriteLine("  help");
            Console.WriteLine("  connect [addr] [port] [username]");
            Console.WriteLine("  list");
            Console.WriteLine("  send [username] [message...]");
            Console.WriteLine("  window");
            Console.WriteLine("  quit");
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
            Help();
            var encryptor = new EncryptionService();
            var network = (Client)null;
            while (true)
            {
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
                    case "help":
                        Help();
                        break;
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
                                network = new Client(message[1], port, encryptor, message[3], (sender, evntArgs) => OnChange(network), false);
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
                        else
                        {
                            Console.WriteLine("Network not connected");
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
                            var lookupResult = encryptor.LookUpUser(message[1]);
                            var found = 0;
                            foreach (var lookup in lookupResult)
                            {
                                network.SendMessage(lookup, theMessage);
                                found++;
                            }
                            if (found == 0)
                            {
                                Console.WriteLine("User not found");
                            }
                            else if (found > 1)
                            {
                                Console.WriteLine("Found multiple users with that name, sent to all");
                            }
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
