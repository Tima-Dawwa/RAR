using System;
using System.Windows.Forms;

namespace RAR.UI
{
    class ContextMenuManagerForm : Form
    {
        private Button installBtn;
        private Button uninstallBtn;
        private Label statusLabel;

        public ContextMenuManagerForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Context Menu Manager";
            this.Size = new System.Drawing.Size(400, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;

            installBtn = new Button()
            {
                Text = "Install Context Menu",
                Size = new System.Drawing.Size(180, 40),
                Location = new System.Drawing.Point(30, 50)
            };
            installBtn.Click += (s, e) => ContextMenuInstaller.InstallContextMenu();

            uninstallBtn = new Button()
            {
                Text = "Uninstall Context Menu",
                Size = new System.Drawing.Size(180, 40),
                Location = new System.Drawing.Point(30, 100)
            };
            uninstallBtn.Click += (s, e) => ContextMenuInstaller.UninstallContextMenu();

            statusLabel = new Label()
            {
                Text = "Manage Windows context menu integration",
                Location = new System.Drawing.Point(30, 20),
                AutoSize = true
            };

            this.Controls.Add(installBtn);
            this.Controls.Add(uninstallBtn);
            this.Controls.Add(statusLabel);
        }
    }
}