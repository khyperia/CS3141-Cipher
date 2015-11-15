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
        public Gui(string name)
        {
            Client client = null;

            Form window = new Form();
            Panel chatInputPanel = new Panel();
            Panel chatPanel = new Panel();
            RichTextBox chatBox = new RichTextBox();
            Panel inputSendPanel = new Panel();
            Button sendButton = new Button();
            RichTextBox inputBox = new RichTextBox();
            Panel contactPanel = new Panel();

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
                // TODO: Actually implement this
                client.SendMessage(client.LookUpUser("alice").Single(), inputBox.Text);
                inputBox.Text = "";
            };

            contactPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            contactPanel.Location = new Point(400, 0);
            contactPanel.Name = "ContactPanel";
            contactPanel.Size = new Size(200, 600);
            contactPanel.TabIndex = 0;

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
