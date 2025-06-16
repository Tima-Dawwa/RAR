using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RAR.UI
{
    public partial class PasswordDialog : Form
    {
        private TextBox passwordTextBox;
        private Button okButton;
        private Button cancelButton;

        public string EnteredPassword { get; private set; }

        public PasswordDialog()
        {
            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = "Enter Password";
            this.Size = new Size(350, 180);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Label instructionLabel = new Label
            {
                Text = "Please enter the password for decryption:",
                Location = new Point(20, 20),
                AutoSize = true
            };

            passwordTextBox = new TextBox
            {
                Size = new Size(290, 25),
                Location = new Point(20, 50),
                PasswordChar = '•',
                UseSystemPasswordChar = true
            };

            okButton = new Button
            {
                Text = "OK",
                Size = new Size(100, 30),
                Location = new Point(120, 90),
                DialogResult = DialogResult.OK
            };
            okButton.Click += (s, e) =>
            {
                EnteredPassword = passwordTextBox.Text;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 30),
                Location = new Point(230, 90),
                DialogResult = DialogResult.Cancel
            };
            cancelButton.Click += (s, e) => this.Close();

            this.Controls.Add(instructionLabel);
            this.Controls.Add(passwordTextBox);
            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }
    }
}
