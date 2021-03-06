﻿using System;
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
            // Contains a mapping from buffer name to buffer text
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

            // Displays the given buffer (and sets the selected contact)
            Action<string> showBuffer = buffer =>
            {
                if (!buffers.ContainsKey(buffer))
                    buffers[buffer] = "";
                var prefix = " -- Buffer \"" + buffer + "\" --\n";
                chatBox.Text = prefix + buffers[buffer];
                for (int i = 0; i < contacts.Items.Count; i++)
                {
                    if (((RemoteUser)contacts.Items[i]).Username == buffer)
                    {
                        contacts.SelectedIndex = i;
                    }
                }
            };

            // Prints to a buffer, and then automatically switches to it.
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
            //right mouse click will pull up the option to copy the chat box to clipboard
            chatBox.MouseUp += (sender, args) =>
            {
                if (args.Button == MouseButtons.Right)
                {
                    ContextMenu options = new ContextMenu();

                    MenuItem copy = new MenuItem("Copy");
                    copy.Click += (copySender, copyArgs) =>
                    {
                        Clipboard.SetData(DataFormats.Rtf, chatBox.SelectedRtf);
                    };
                    options.MenuItems.Add(copy);

                    chatBox.ContextMenu = options;
                }
            };

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
            //right mouse click will pull up the option to cut/copy/paste the input box with the clipboard
            inputBox.MouseUp += (sender, args) =>
            {
                if (args.Button == MouseButtons.Right)
                {
                    ContextMenu options = new ContextMenu();

                    MenuItem cut = new MenuItem("Cut");
                    cut.Click += (cutSender, cutArgs) =>
                    {
                        inputBox.Cut();
                    };
                    options.MenuItems.Add(cut);

                    MenuItem copy = new MenuItem("Copy");
                    copy.Click += (copySender, copyArgs) =>
                    {
                        Clipboard.SetData(DataFormats.Rtf, inputBox.SelectedRtf);
                    };
                    options.MenuItems.Add(copy);
                          
                    MenuItem paste = new MenuItem("Paste");
                    paste.Click += (pasteSender, pasteArgs) =>
                    {
                        if (Clipboard.ContainsText(TextDataFormat.Rtf))
                        {
                            inputBox.SelectedRtf = Clipboard.GetData(DataFormats.Rtf).ToString();
                        }
                    };
                    options.MenuItems.Add(paste);

                    inputBox.ContextMenu = options;
                }
            };

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
                if (sendingTo != null)
                {
                    string input = inputBox.Text;
                    int msgMaxLen = 74;
                    int numMsgs = input.Length / msgMaxLen + 1;
                    for (int currMsg = 1; currMsg <= numMsgs; currMsg++)
                    {
                        int startSubString = (currMsg - 1) * msgMaxLen;
                        int endSubString = (currMsg * msgMaxLen) < (input.Length) ? msgMaxLen : input.Length - ((currMsg - 1) * msgMaxLen);
                        string message = string.Format("{0} ({1}/{2})", input.Substring(startSubString, endSubString), currMsg, numMsgs);

                        printToBuffer(sendingTo.Username, string.Format("<{0}> {1}", name, message));
                        client.SendMessage(sendingTo, message);
                    }
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

            // Connect to the server, with the default server if not specified
            string server = Config.Get("Server", "71.13.216.7");
            int port = Config.Get("Port", 60100, int.TryParse);
            client = new Client(server, port, new EncryptionService(), name, () =>
            {
                var message = false;
                while (true)
                {
                    // get all incoming messages
                    var dequeue = client.TryGetNextMessage();
                    if (dequeue.Key == null)
                        break;
                    // print it to the buffer named their nick
                    printToBuffer(dequeue.Key, string.Format("<{0}> {1}", dequeue.Key, dequeue.Value));
                    message = true;
                }
                if (!message)
                {
                    // if we didn't get a new message, then the reason we're called is probably a nicklist update
                    // so clear the nicklist and re-add them
                    contacts.Items.Clear();
                    foreach (var user in client.GetUsers())
                    {
                        // don't add ourselves as a selectable item
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
