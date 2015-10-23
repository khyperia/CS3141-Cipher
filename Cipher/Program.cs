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

        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World from Team Cipher's Encrypted Messaging Service!");
            var encryptor = new EncryptionService();
            while (true)
            {
                Console.WriteLine(" ~~~");
                Console.WriteLine(" Enter action:");
                Console.WriteLine("  send [username message is to] [file to write] [message...]");
                Console.WriteLine("  recv [encrypted message file]");
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
                    case "send":
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
                    case "recv":
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
