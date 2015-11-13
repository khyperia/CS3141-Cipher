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

            string server = Config.Get("Server", "71.13.216.7");
            int port = Config.Get("Port", 60100, int.TryParse);
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
