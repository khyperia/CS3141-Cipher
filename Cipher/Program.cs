using System;

namespace Cipher
{
    class MainClass
    {
        public static void Window(string name)
        {
            new Gui(name).Run();
        }

        // Implements the change handler for the console client
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

        // Prints a help message for the console client
        private static void Help()
        {
            Console.WriteLine(" Enter action:");
            Console.WriteLine("  help");
            Console.WriteLine("  list");
            Console.WriteLine("  send [username] [message...]");
            Console.WriteLine("  quit");
        }

        // Runs a console client under the specified username
        private static void ConsoleClient(string username)
        {
            Help();
            Client network = null;
            string server = Config.Get("Server", "71.13.216.7");
            int port = Config.Get("Port", 60100, int.TryParse);
            network = new Client(server, port, new EncryptionService(), username, () => OnChange(network), null);
            while (true)
            {
                Console.Write("> ");
                var readLine = Console.ReadLine();
                if (readLine == null)
                {
                    // happens when Ctrl-D is pressed in linux
                    Console.WriteLine("quit");
                    return;
                }
                var message = readLine.Split(' ');
                if (message.Length == 0)
                {
                    // Empty message
                    continue;
                }
                // Determine command
                switch (message[0])
                {
                    case "help":
                        // Print help
                        Help();
                        break;
                    case "list":
                        // Refresh user list
                        network.UpdateKeyPairs();
                        break;
                    case "send":
                        // Send a message to a user
                        if (message.Length < 3)
                        {
                            Console.WriteLine("Bad syntax");
                        }
                        else
                        {
                            var theMessage = string.Join(" ", message, 2, message.Length - 2);
                            var lookupResult = network.LookUpUser(message[1]);
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
                    case "quit":
                        // Quit console client
                        return;
                    default:
                        Console.WriteLine("Unknown command: " + message[0]);
                        break;
                }
            }
        }

        // Checks for a valid username, returns true if good
        private static bool UsernameValid(string username)
        {
            if (username == "")
            {
                return false;
            }
            foreach (var c in username)
            {
                if (!char.IsLetterOrDigit(c))
                {
                    return false;
                }
            }
            return true;
        }

        // Main entry point
        public static void Main(string[] args)
        {
            if (args.Length == 2 && args[0] == "server")
            {
                // Run the server on the specified port
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
            var username = Config.Get("Username", "");
            while (!UsernameValid(username))
            {
                // Ask if we haven't specified a username
                Console.WriteLine("What is your name?");
                Console.Write("> ");
                username = Console.ReadLine();
                Config.Set("Username", username);
            }
            if (args.Length == 1 && args[0] == "console")
            {
                // If we provided arguments, then we're probably in a console
                ConsoleClient(username);
                return;
            }
            // Otherwise we probably double-clicked on the exe and expect a window.
            Window(username);
        }
    }
}
