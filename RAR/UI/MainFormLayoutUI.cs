using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace RAR.UI
{
    public class MainFormUILayout
    {
        private readonly Form _parentForm;

        public MainFormUILayout(Form parentForm)
        {
            _parentForm = parentForm;
        }

        public void BuildTitleBar(out Panel titleBar, out Button closeBtn, out MenuStrip menuStrip)
        {
            titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 25,
                BackColor = Color.FromArgb(35, 35, 35)
            };

            closeBtn = new Button
            {
                Text = "✕",
                Size = new Size(45, 25),
                Location = new Point(_parentForm.ClientSize.Width - 45, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.FlatAppearance.MouseDownBackColor = Color.FromArgb(200, 17, 35);
            closeBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35);
            closeBtn.Click += (s, e) => _parentForm.Close();

            menuStrip = CreateEmbeddedMenuStrip();
            menuStrip.Location = new Point(0, 0);
            menuStrip.AutoSize = true;
            menuStrip.GripStyle = ToolStripGripStyle.Hidden;
            menuStrip.Padding = new Padding(0);
            menuStrip.Margin = new Padding(0);
        }

        private MenuStrip CreateEmbeddedMenuStrip()
        {
            MenuStrip menuStrip = new MenuStrip
            {
                BackColor = Color.FromArgb(35, 35, 35),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                GripStyle = ToolStripGripStyle.Hidden,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            ToolStripMenuItem toolsMenu = new ToolStripMenuItem("Tools") { ForeColor = Color.White };
            ToolStripMenuItem contextMenuManager = new ToolStripMenuItem("Context Menu Manager...") { ForeColor = Color.White };
            contextMenuManager.Click += (s, e) =>
            {
                // This assumes ContextMenuManagerForm is defined elsewhere in your project.
                using (var contextMenuForm = new ContextMenuManagerForm())
                {
                    contextMenuForm.ShowDialog();
                }
            };
            ToolStripSeparator separator = new ToolStripSeparator();
            ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("Exit") { ForeColor = Color.White };
            exitMenuItem.Click += (s, e) => Application.Exit();

            toolsMenu.DropDownItems.Add(contextMenuManager);
            toolsMenu.DropDownItems.Add(separator);
            toolsMenu.DropDownItems.Add(exitMenuItem);

            ToolStripMenuItem helpMenu = new ToolStripMenuItem("Help") { ForeColor = Color.White };
            ToolStripMenuItem aboutMenuItem = new ToolStripMenuItem("About...") { ForeColor = Color.White };
            aboutMenuItem.Click += (s, e) =>
            {
                MessageBox.Show("File Compression Tool v1.0\n\n" +
                                "A modern compression tool supporting Huffman and Shannon-Fano algorithms.\n" +
                                "Features encryption, multithreading, and Windows context menu integration.\n\n" +
                                "© 2025 Multimedia Project",
                                "About File Compression Tool",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
            };
            helpMenu.DropDownItems.Add(aboutMenuItem);

            menuStrip.Items.Add(toolsMenu);
            menuStrip.Items.Add(helpMenu);
            menuStrip.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());

            return menuStrip;
        }

        public Panel CreateHeaderPanel(out Label titleLabel, out Label subtitleLabel)
        {
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = Color.FromArgb(25, 25, 25)
            };

            titleLabel = new Label
            {
                Text = "File Compression Tool",
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(40, 20),
                AutoSize = true
            };

            subtitleLabel = new Label
            {
                Text = "Compress and decompress files and folders with Huffman | Shannon-Fano algorithms",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(45, 65),
                AutoSize = true
            };
            return headerPanel;
        }

        public Panel CreateMainPanel()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 15, 15),
                Padding = new Padding(20)
            };
        }

        public Panel CreateFooterPanel()
        {
            return new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.FromArgb(25, 25, 25)
            };
        }

        public Label CreateFooterLabel()
        {
            return new Label
            {
                Text = "© 2025 File Compressor | Multimedia Project",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(120, 120, 120),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
        }

        public Panel CreateFileSelectionPanel(
            out RoundedButton selectFilesBtn, out RoundedButton selectFolderBtn, out ListBox selectedFilesListBox,
            out Label selectedFilesLabel, out Label fileCountLabel)
        {
            Panel panel = new Panel
            {
                Size = new Size(840, 220),
                Location = new Point(20, 140),
                BackColor = Color.FromArgb(25, 25, 25)
            };
            panel.Paint += (s, e) => DrawRoundedRectangle(e.Graphics, panel.ClientRectangle, 15, Color.FromArgb(25, 25, 25));

            selectFilesBtn = new RoundedButton
            {
                Text = "Add Files",
                Size = new Size(120, 40),
                Location = new Point(20, 50),
                BackColor = Color.FromArgb(0, 120, 212),
                ForeColor = Color.White
            };

            selectFolderBtn = new RoundedButton
            {
                Text = "Add Folder",
                Size = new Size(120, 40),
                Location = new Point(160, 50),
                BackColor = Color.FromArgb(16, 110, 190),
                ForeColor = Color.White
            };

            selectedFilesLabel = new Label
            {
                Text = "Selected Items:",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 200, 200),
                Location = new Point(20, 105),
                AutoSize = true
            };

            fileCountLabel = new Label
            {
                Text = "Count: 0 items",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 212),
                Location = new Point(700, 105),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            selectedFilesListBox = new ListBox
            {
                Size = new Size(780, 90),
                Location = new Point(20, 125),
                BackColor = Color.FromArgb(35, 35, 35),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9F),
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 25
            };
            return panel;
        }

        public RoundedButton CreateClearAllButton(ListBox selectedFilesListBox, Label fileCountLabel, RoundedButton extractBtn, Label extractLabel, ComboBox archiveContentComboBox)
        {
            RoundedButton clearAllBtn = new RoundedButton
            {
                Text = "Clear All",
                Size = new Size(100, 40),
                Location = new Point(300, 50),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White
            };
            clearAllBtn.Click += (s, e) =>
            {
                selectedFilesListBox.Items.Clear();
                UpdateFileCountLabel(selectedFilesListBox, fileCountLabel); // Call static helper
                if(extractBtn != null) {
                    extractBtn.Visible = false;
                    extractBtn.Enabled = false;
                    extractLabel.Visible = false;
                    archiveContentComboBox.Visible = false;
                }
            };
            return clearAllBtn;
        }

        private static void UpdateFileCountLabel(ListBox selectedFilesListBox, Label fileCountLabel)
        {
            int count = selectedFilesListBox.Items.Count;
            int folderCount = 0;
            int fileCount = 0;

            foreach (string item in selectedFilesListBox.Items)
            {
                if (Directory.Exists(item))
                    folderCount++;
                else
                    fileCount++;
            }

            if (count == 0)
            {
                fileCountLabel.Text = "Count: 0 items";
            }
            else
            {
                string countText = "Count: ";
                if (fileCount > 0 && folderCount > 0)
                {
                    countText += $"{fileCount} file{(fileCount != 1 ? "s" : "")}, {folderCount} folder{(folderCount != 1 ? "s" : "")}";
                }
                else if (fileCount > 0)
                {
                    countText += $"{fileCount} file{(fileCount != 1 ? "s" : "")}";
                }
                else
                {
                    countText += $"{folderCount} folder{(folderCount != 1 ? "s" : "")}";
                }
                fileCountLabel.Text = countText;
            }
        }


        public Panel CreateOptionsPanel(
            out Label algorithmLabel, out ComboBox algorithmComboBox, out CheckBox encryptionCheckBox,
            out CheckBox multithreadingCheckBox, out Label passwordLabel, out TextBox passwordTextBox,
            out Button passwordToggleBtn, out ComboBox archiveContentComboBox, out Label extractLabel)
        {
            Panel panel = new Panel
            {
                Size = new Size(840, 160),
                Location = new Point(20, 360),
                BackColor = Color.FromArgb(25, 25, 25)
            };
            panel.Paint += (s, e) => DrawRoundedRectangle(e.Graphics, panel.ClientRectangle, 15, Color.FromArgb(25, 25, 25));

            algorithmLabel = new Label
            {
                Text = "Algorithm:",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(200, 200, 200),
                Location = new Point(20, 55),
                AutoSize = true
            };

            algorithmComboBox = new ComboBox
            {
                Size = new Size(150, 30),
                Location = new Point(100, 52),
                BackColor = Color.FromArgb(35, 35, 35),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            algorithmComboBox.Items.AddRange(new string[] { "Huffman", "Shannon-Fano" });
            algorithmComboBox.SelectedIndex = 0;

            extractLabel = new Label
            {
                Text = "🗂 Archive Content :",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(200, 200, 200),
                Location = new Point(20, 127),
                AutoSize = true,
                Visible = false,
            };

            archiveContentComboBox = new ComboBox
            {
                Name = "archiveContentComboBox",
                Location = new Point(160, 127),
                Size = new Size(250, 40),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(35, 35, 35),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Visible = false,
                AutoSize = true,
            };

            encryptionCheckBox = new CheckBox
            {
                Text = "🔐 Enable Encryption",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(200, 200, 200),
                Location = new Point(280, 55),
                AutoSize = true,
                FlatStyle = FlatStyle.Flat
            };

            multithreadingCheckBox = new CheckBox
            {
                Text = "⚡ Multithreading",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(200, 200, 200),
                Location = new Point(450, 55),
                AutoSize = true,
                FlatStyle = FlatStyle.Flat
            };

            passwordLabel = new Label
            {
                Text = "Password:",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(200, 200, 200),
                Location = new Point(20, 95),
                AutoSize = true,
                Visible = false
            };

            passwordTextBox = new TextBox
            {
                Size = new Size(200, 25),
                Location = new Point(90, 92),
                BackColor = Color.FromArgb(35, 35, 35),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F),
                UseSystemPasswordChar = true,
                Visible = false
            };

            passwordToggleBtn = new Button
            {
                Text = "👁",
                Size = new Size(30, 25),
                Location = new Point(295, 92),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand,
                Visible = false,
                TabStop = false
            };
            passwordToggleBtn.FlatAppearance.BorderSize = 1;
            passwordToggleBtn.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 70);
            return panel;
        }

        public Panel CreateActionPanel(
            out RoundedButton compressBtn, out RoundedButton decompressBtn,
            out RoundedButton pauseBtn, out RoundedButton cancelBtn, out RoundedButton extractBtn)
        {
            Panel panel = new Panel
            {
                Size = new Size(840, 80),
                Location = new Point(20, 540),
                BackColor = Color.FromArgb(25, 25, 25)
            };
            panel.Paint += (s, e) => DrawRoundedRectangle(e.Graphics, panel.ClientRectangle, 15, Color.FromArgb(25, 25, 25));

            compressBtn = new RoundedButton
            {
                Text = "🗜️ Compress",
                Size = new Size(140, 45),
                Location = new Point(50, 20),
                BackColor = Color.FromArgb(46, 160, 67),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            };

            decompressBtn = new RoundedButton
            {
                Text = "📦 Decompress",
                Size = new Size(140, 45),
                Location = new Point(220, 20),
                BackColor = Color.FromArgb(255, 140, 0),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            };

            pauseBtn = new RoundedButton
            {
                Text = "⏸️ Pause",
                Size = new Size(100, 45),
                Location = new Point(390, 20),
                BackColor = Color.FromArgb(255, 193, 7),
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Enabled = false
            };

            cancelBtn = new RoundedButton
            {
                Text = "❌ Cancel",
                Size = new Size(100, 45),
                Location = new Point(510, 20),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Enabled = false
            };

            extractBtn = new RoundedButton
            {
                Text = "📤 Extract",
                Size = new Size(100, 45),
                Location = new Point(630, 20),
                BackColor = Color.DarkKhaki,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Enabled = false,
                Visible = false,
            };
            return panel;
        }

        public Panel CreateProgressPanel(
            out ProgressBar progressBar, out Label statusLabel,
            out Label compressionRatioLabel, out Label timeLabel)
        {
            Panel panel = new Panel
            {
                Size = new Size(840, 100),
                Location = new Point(20, 640),
                BackColor = Color.FromArgb(25, 25, 25)
            };
            panel.Paint += (s, e) => DrawRoundedRectangle(e.Graphics, panel.ClientRectangle, 15, Color.FromArgb(25, 25, 25));

            progressBar = new ProgressBar
            {
                Size = new Size(780, 25),
                Location = new Point(20, 15), // Adjusted Y position to 15, closer to the top of the panel
                Style = ProgressBarStyle.Continuous,
                ForeColor = Color.FromArgb(0, 120, 212)
            };

            statusLabel = new Label
            {
                Text = "Ready to compress files...",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(20, 45), // Adjusted Y position to be below ProgressBar
                AutoSize = true
            };

            timeLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(20, 70), // Adjusted Y position to be below statusLabel
                AutoSize = true
            };

            compressionRatioLabel = new Label
            {
                Text = "Compression Ratio: ",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 160, 67),
                Location = new Point(650, 70), // Adjusted Y position to align with timeLabel
                AutoSize = true
            };
            return panel;
        }

        public Label CreateSectionTitle(string text, Point location)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = location,
                AutoSize = true
            };
        }

        private void DrawRoundedRectangle(Graphics graphics, Rectangle rect, int radius, Color fillColor)
        {
            using (GraphicsPath path = GetRoundedRectPath(rect, radius))
            {
                using (SolidBrush brush = new SolidBrush(fillColor))
                {
                    graphics.FillPath(brush, path);
                }
            }
        }

        private GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.X + rect.Width - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.X + rect.Width - radius, rect.Y + rect.Height - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Y + rect.Height - radius, radius, radius, 90, 90);
            path.CloseAllFigures();
            return path;
        }

        private class DarkColorTable : ProfessionalColorTable
        {
            public override Color MenuItemSelected => Color.FromArgb(50, 50, 50);
            public override Color MenuItemBorder => Color.FromArgb(70, 70, 70);
            public override Color MenuBorder => Color.FromArgb(70, 70, 70);
            public override Color MenuItemSelectedGradientBegin => Color.FromArgb(50, 50, 50);
            public override Color MenuItemSelectedGradientEnd => Color.FromArgb(50, 50, 50);
            public override Color MenuItemPressedGradientBegin => Color.FromArgb(40, 40, 40);
            public override Color MenuItemPressedGradientEnd => Color.FromArgb(40, 40, 40);
            public override Color ToolStripDropDownBackground => Color.FromArgb(35, 35, 35);
            public override Color ImageMarginGradientBegin => Color.FromArgb(35, 35, 35);
            public override Color ImageMarginGradientEnd => Color.FromArgb(35, 35, 35);
            public override Color ImageMarginGradientMiddle => Color.FromArgb(35, 35, 35);
        }
    }
}