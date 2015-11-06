using Gtk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cipher
{
    class Gui
    {
        static Gui()
        {
            Application.Init();
        }

        private static void ParseConfigFile(out string server, out int port)
        {
            System.Xml.Linq.XElement root;
            try
            {
                root = System.Xml.Linq.XElement.Load("config.xml");
            }
            catch
            {
                root = null;
            }
            var serverElement = root?.Element("Server");
            var portElement = root?.Element("Port");
            if (serverElement != null)
                server = serverElement.Value;
            else
            {
                server = "71.13.216.7";
                Console.WriteLine("Didn't load server address");
            }
            if (portElement == null || !int.TryParse(portElement.Value, out port))
            {
                port = 60100;
                Console.WriteLine("Didn't load server port");
            }
        }

        public Gui(string name)
        {
            Client client = null;
            var window = new Window("Hello World");
            window.Resize(400, 600);

            var grid = new Table(2, 2, false);

            var chatBox = new TextView();
            chatBox.Editable = false;
            chatBox.WrapMode = WrapMode.WordChar;

            var sw = new ScrolledWindow();
            sw.Child = chatBox;
            sw.SetPolicy(PolicyType.Never, PolicyType.Automatic);

            grid.Attach(sw, 0, 2, 0, 1, AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Expand | AttachOptions.Fill, 0, 0);

            var textBox = new TextView();
            textBox.WrapMode = WrapMode.WordChar;
            grid.Attach(textBox, 0, 1, 1, 2, AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Fill, 0, 0);

            var button = new Button();
            button.Label = "Send";
            button.Pressed += (sender, args) =>
            {
                // TODO: Actually implement this
                client.SendMessage(client.LookUpUser("alice").Single(), textBox.Buffer.Text);
                textBox.Buffer.Text = "";
            };
            grid.Attach(button, 1, 2, 1, 2, AttachOptions.Shrink | AttachOptions.Fill, AttachOptions.Shrink | AttachOptions.Fill, 0, 1);

            window.Add(grid);

            window.Destroyed += (o, e) =>
            {
                Application.Quit();
            };

            string server;
            int port;
            ParseConfigFile(out server, out port);
            client = new Client(server, port, new EncryptionService(), name, (sender, eventArg) =>
            {
                while (true)
                {
                    var dequeue = client.TryGetNextMessage();
                    if (dequeue.Key == null)
                        break;
                    chatBox.Buffer.Text += string.Format("<{0}> {1}", dequeue.Key, dequeue.Value);
                }
            }, true);

            window.ShowAll();
        }

        public void Run()
        {
            Application.Run();
        }
    }

}
