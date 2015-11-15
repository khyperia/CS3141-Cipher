using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Cipher
{
    class Gui
    {
        public Gui(string name)
        {
            Client client = null;
            RemoteUser sendingTo = null;
            Dictionary<string, string> buffers = new Dictionary<string, string>();

            Form window = new Form();
            Panel chatInputPanel = new Panel();
            Panel chatPanel = new Panel();
            RichTextBox chatBox = new RichTextBox();
            Panel inputSendPanel = new Panel();
            Button sendButton = new Button();
            RichTextBox inputBox = new RichTextBox();
            Panel contactPanel = new Panel();
            ListBox contacts = new ListBox();
            Button refreshContacts = new Button();

            Action<string> showBuffer = (buffer) =>
            {
                if (!buffers.ContainsKey(buffer))
                    buffers[buffer] = "";
                var prefix = buffer == "" ? "Buffer server\n" : "Buffer \"" + buffer + "\"\n";
                chatBox.Text = prefix + buffers[buffer];
                for (int i = 0; i < contacts.Items.Count; i++)
                {
                    if (((RemoteUser)contacts.Items[i]).Username == buffer)
                    {
                        contacts.SelectedIndex = i;
                    }
                }
            };

            Action<string, string> printToBuffer = (buffer, message) =>
            {
                if (!buffers.ContainsKey(buffer))
                    buffers[buffer] = "";
                buffers[buffer] += message + "\n";
                showBuffer(buffer);
            };

            window.SuspendLayout();
            chatInputPanel.SuspendLayout();
            chatPanel.SuspendLayout();
            inputSendPanel.SuspendLayout();
            contactPanel.SuspendLayout();

            chatInputPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            chatInputPanel.Controls.Add(inputSendPanel);
            chatInputPanel.Controls.Add(chatPanel);
            chatInputPanel.Location = new Point(0, 0);
            chatInputPanel.Name = "ChatInputPanel";
            chatInputPanel.Size = new Size(400, 600);
            chatInputPanel.TabIndex = 0;

            chatPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            chatPanel.Controls.Add(chatBox);
            chatPanel.Location = new Point(0, 0);
            chatPanel.Name = "ChatPanel";
            chatPanel.Size = new Size(400, 500);
            chatPanel.TabIndex = 1;

            chatBox.BackColor = SystemColors.Window;
            chatBox.Dock = DockStyle.Fill;
            chatBox.Location = new Point(0, 0);
            chatBox.Name = "ChatBox";
            chatBox.ReadOnly = true;
            chatBox.Size = new Size(400, 500);
            chatBox.TabIndex = 2;
            chatBox.Text = "";

            inputSendPanel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            inputSendPanel.Controls.Add(sendButton);
            inputSendPanel.Controls.Add(inputBox);
            inputSendPanel.Location = new Point(0, 500);
            inputSendPanel.Name = "InputSendPanel";
            inputSendPanel.Size = new Size(400, 100);
            inputSendPanel.TabIndex = 1;

            inputBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            inputBox.Location = new Point(0, 0);
            inputBox.Name = "InputBox";
            inputBox.Size = new Size(300, 100);
            inputBox.TabIndex = 2;
            inputBox.Text = "";

            sendButton.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            sendButton.Location = new Point(300, 0);
            sendButton.Name = "SendButton";
            sendButton.Size = new Size(100, 100);
            sendButton.TabIndex = 2;
            sendButton.Text = "Send";
            sendButton.UseVisualStyleBackColor = true;
            sendButton.Click += (sender, args) =>
            {
                if (sendingTo == null)
                {
                    printToBuffer("", "No user selected");
                }
                else
                {
                    printToBuffer(sendingTo.Username, string.Format("<{0}> {1}", name, inputBox.Text));
                    client.SendMessage(sendingTo, inputBox.Text);
                    inputBox.Text = "";
                }
            };

            contactPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            contactPanel.Controls.Add(contacts);
            contactPanel.Controls.Add(refreshContacts);
            contactPanel.Location = new Point(400, 0);
            contactPanel.Name = "ContactPanel";
            contactPanel.Size = new Size(200, 600);
            contactPanel.TabIndex = 0;

            contacts.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            contacts.Location = new Point(0, 0);
            contacts.Size = new Size(200, 500);
            contacts.TabIndex = 1;
            contacts.Name = "Contacts";
            contacts.SelectionMode = SelectionMode.One;
            contacts.SelectedValueChanged += (sender, args) =>
            {
                if (contacts.SelectedIndex != -1)
                {
                    var selected = (RemoteUser)contacts.Items[contacts.SelectedIndex];
                    sendingTo = selected;
                    showBuffer(selected.Username);
                }
            };

            refreshContacts.Anchor = AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right;
            refreshContacts.Location = new Point(0, 500);
            refreshContacts.Size = new Size(200, 100);
            refreshContacts.TabIndex = 1;
            refreshContacts.Name = "RefreshContacts";
            refreshContacts.Text = "Refresh contacts";
            refreshContacts.Click += (sender, args) =>
            {
                client.UpdateKeyPairs();
            };

            window.AutoScaleDimensions = new SizeF(12F, 25F);
            window.AutoScaleMode = AutoScaleMode.Font;
            window.ClientSize = new Size(600, 600);
            window.Controls.Add(chatInputPanel);
            window.Controls.Add(contactPanel);
            window.Name = "Window";
            window.Text = "CipherEMS";
            window.FormClosed += (o, e) =>
            {
                Application.Exit();
            };

            contactPanel.ResumeLayout(false);
            inputSendPanel.ResumeLayout(false);
            chatPanel.ResumeLayout(false);
            chatInputPanel.ResumeLayout(false);
            window.ResumeLayout(false);

            window.Show();

            string server = Config.Get("Server", "71.13.216.7");
            int port = Config.Get("Port", 60100, int.TryParse);
            client = new Client(server, port, new EncryptionService(), name, () =>
            {
                var message = false;
                while (true)
                {
                    var dequeue = client.TryGetNextMessage();
                    if (dequeue.Key == null)
                        break;
                    printToBuffer(dequeue.Key, string.Format("<{0}> {1}", dequeue.Key, dequeue.Value));
                    message = true;
                }
                if (!message)
                {
                    contacts.Items.Clear();
                    foreach (var user in client.GetUsers())
                    {
                        if (!string.Equals(user.Username, name, StringComparison.OrdinalIgnoreCase))
                        {
                            contacts.Items.Add(user);
                        }
                    }
                }
            }, window);
        }

        public void Run()
        {
            Application.Run();
        }
    }

}
