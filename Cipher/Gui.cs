using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Cipher
{
    class Gui
    {
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
            var window = new Form();
            window.Text = "Hello, world!";
            window.Size = new Size(400, 600);
            window.FormClosed += (o, e) =>
            {
                Application.Exit();
            };

            var chatBox = new TextBox();
            chatBox.ReadOnly = true;
            chatBox.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left;
            chatBox.WordWrap = true;
            chatBox.ScrollBars = ScrollBars.Vertical;
            window.Controls.Add(chatBox);

            var textBox = new TextBox();
            textBox.WordWrap = true;
            chatBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            window.Controls.Add(textBox);

            var button = new Button();
            button.Text = "Send";
            button.Click += (sender, args) =>
            {
                // TODO: Actually implement this
                client.SendMessage(client.LookUpUser("alice").Single(), textBox.Text);
                textBox.Text = "";
            };
            window.Controls.Add(button);
            
            string server = Config.Get("Server", "71.13.216.7");
            int port = Config.Get("Port", 60100, int.TryParse);
            client = new Client(server, port, new EncryptionService(), name, () =>
            {
                while (true)
                {
                    var dequeue = client.TryGetNextMessage();
                    if (dequeue.Key == null)
                        break;
                    chatBox.Text += string.Format("<{0}> {1}", dequeue.Key, dequeue.Value);
                }
            }, window);

            window.Show();
        }

        public void Run()
        {
            Application.Run();
        }
    }

}
