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

            // GUI Components
            // Please list all component inits here to keep track
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
            
            //suspend layouts to make componets add easier
            window.SuspendLayout();
            chatInputPanel.SuspendLayout();
            chatPanel.SuspendLayout();
            inputSendPanel.SuspendLayout();
            contactPanel.SuspendLayout();

            //chat and input panel, split from contact panel
            //anchored to all sides to maintain proportional scaling
            chatInputPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            //contains the input and send panel as well as chat panel
            chatInputPanel.Controls.Add(inputSendPanel);
            chatInputPanel.Controls.Add(chatPanel);
            chatInputPanel.Location = new Point(0, 0);
            chatInputPanel.Name = "ChatInputPanel";
            chatInputPanel.Size = new Size(400, 600);
            chatInputPanel.TabIndex = 0;

            //chat panel, split from input panel
            //anchored ot all sides to maintain proportional scaling
            chatPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            //contains the chat box
            chatPanel.Controls.Add(chatBox);
            chatPanel.Location = new Point(0, 0);
            chatPanel.Name = "ChatPanel";
            chatPanel.Size = new Size(400, 500);
            chatPanel.TabIndex = 1;

            //chat box, main chat log
            chatBox.BackColor = SystemColors.Window;
            //docked to fill as it's the component in the chat panel
            chatBox.Dock = DockStyle.Fill;
            chatBox.Location = new Point(0, 0);
            chatBox.Name = "ChatBox";
            //note readonly
            chatBox.ReadOnly = true;
            chatBox.Size = new Size(400, 500);
            chatBox.TabIndex = 2;
            chatBox.Text = "";

            //input send panel, split from chat panel
            //anchored to only bottom, left, right to make height constant during scaling
            inputSendPanel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            //contains the input box and send button
            inputSendPanel.Controls.Add(sendButton);
            inputSendPanel.Controls.Add(inputBox);
            inputSendPanel.Location = new Point(0, 500);
            inputSendPanel.Name = "InputSendPanel";
            inputSendPanel.Size = new Size(400, 100);
            inputSendPanel.TabIndex = 1;

            //input box, split from the send button
            //anchored ot all sides to maintain proportional scaling
            inputBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            inputBox.Location = new Point(0, 0);
            inputBox.Name = "InputBox";
            inputBox.Size = new Size(300, 100);
            inputBox.TabIndex = 2;
            inputBox.Text = "";

            //send button, split from input box
            //anchored to only top, bottom, right to make width constant during scaling
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

            //contact panel, split from chat input panel
            //anchored to only top, bottom, right to make width constant during scaling
            contactPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            //contains the contacts listbox and refresh contacts button
            contactPanel.Controls.Add(contacts);
            contactPanel.Controls.Add(refreshContacts);
            contactPanel.Location = new Point(400, 0);
            contactPanel.Name = "ContactPanel";
            contactPanel.Size = new Size(200, 600);
            contactPanel.TabIndex = 0;

            //contacts listbox, contains a seletable list of users, split from refresh contacts button
            //anchored ot all sides to maintain proportional scaling
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

            //refresh contacts, split from contacts listbox
            //anchored to only left, bottom, right to make height constant during scaling
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

            //main window
            //autoscaled based off of font size
            window.AutoScaleDimensions = new SizeF(12F, 25F);
            window.AutoScaleMode = AutoScaleMode.Font;
            window.ClientSize = new Size(600, 600);
            //contains the chat input panel, and contact panel
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
