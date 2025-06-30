using RAR.Core.Compression;
using RAR.Core.Interfaces;
using RAR.Helper;
using RAR.Services;
using RAR.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace RAR.UI
{
    public partial class MainForm : Form
    {
        private Panel headerPanel;
        private Panel mainPanel;
        private Panel footerPanel;
        private Label titleLabel; 
        private Label subtitleLabel;
        private Panel fileSelectionPanel;
        private RoundedButton selectFilesBtn;
        private RoundedButton selectFolderBtn;
        private ListBox selectedFilesListBox;
        private Label selectedFilesLabel;
        private Label fileCountLabel;
        private Panel optionsPanel;
        private ComboBox algorithmComboBox;
        private CheckBox encryptionCheckBox;
        private CheckBox multithreadingCheckBox;
        private Label algorithmLabel;
        private TextBox passwordTextBox;
        private Label passwordLabel;
        private Button passwordToggleBtn;
        private Panel actionPanel;
        private RoundedButton compressBtn;
        private RoundedButton decompressBtn;
        private RoundedButton pauseBtn;
        private RoundedButton cancelBtn;
        private Panel progressPanel;
        private ProgressBar progressBar;
        private Label statusLabel;
        private Label timeLabel;
        private Label compressionRatioLabel;
        private Label extractLabel;
        private RoundedButton extractBtn;
        private ComboBox archiveContentComboBox;
        private PauseTokenSource pauseTokenSource = new PauseTokenSource();

        private bool isDragging = false;
        string folderPath;
        private Point lastCursor;
        private Point lastForm;
        private CancellationTokenSource cancellationTokenSource;
        private bool isProcessing = false;
        private ICompressor currentCompressor;
        private ICompressor compressor;
        private HuffmanFolderCompression folderCompressor;
        private ThreadingService threadingService;
        private Stopwatch threadingStopwatch;
        private int threadingCompletedCount = 0;
        private int threadingTotalCount = 0;


        public MainForm()
        {
            InitializeComponent();
            InitializeStyle();
            SetupModernUI();
            currentCompressor = new HuffmanCompressor();
            folderCompressor = new HuffmanFolderCompression();
            threadingService = new ThreadingService();
            threadingService.FileCompressionCompleted += OnFileCompressed;
            threadingService.FolderCompressionCompleted += OnFolderCompressed;
            threadingService.FileDecompressionCompleted += OnFileDecompressed;
            threadingService.FolderDecompressionCompleted += OnFolderDecompressed;
            threadingService.OperationFailed += OnOperationFailed;
        }

        private void InitializeStyle()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new SizeF(8F, 16F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(900, 800);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(15, 15, 15);
            this.Font = new Font("Segoe UI", 9F);
            this.Text = "File Compression Tool";
            this.MouseDown += MainForm_MouseDown;
            this.MouseMove += MainForm_MouseMove;
            this.MouseUp += MainForm_MouseUp;
            this.ResumeLayout(false);
        }

        private void SetupModernUI()
        {

            CreateTitleBar();

            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = Color.FromArgb(25, 25, 25)
            };
            headerPanel.MouseDown += MainForm_MouseDown;
            headerPanel.MouseMove += MainForm_MouseMove;
            headerPanel.MouseUp += MainForm_MouseUp;

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

            headerPanel.Controls.AddRange(new Control[] { titleLabel, subtitleLabel });

            mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 15, 15),
                Padding = new Padding(20)
            };

            CreateFileSelectionPanel();
            CreateOptionsPanel();
            CreateActionPanel();
            CreateProgressPanel();

            footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.FromArgb(25, 25, 25)
            };

            Label footerLabel = new Label
            {
                Text = "© 2025 File Compressor | Multimedia Project",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(120, 120, 120),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            footerPanel.Controls.Add(footerLabel);

            this.Controls.Add(footerPanel);
            this.Controls.Add(mainPanel);
            this.Controls.Add(headerPanel);
        }

        private void CreateTitleBar()
        {
            Panel titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 25, 
                BackColor = Color.FromArgb(35, 35, 35)
            };
            titleBar.MouseDown += MainForm_MouseDown;
            titleBar.MouseMove += MainForm_MouseMove;
            titleBar.MouseUp += MainForm_MouseUp;


            Button closeBtn = new Button
            {
                Text = "✕",
                Size = new Size(45, 25),
                Location = new Point(850, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.FlatAppearance.MouseDownBackColor = Color.FromArgb(200, 17, 35);
            closeBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35);
            closeBtn.Click += (s, e) => this.Close();

            MenuStrip titleBarMenuStrip = CreateEmbeddedMenuStrip(); 
            titleBarMenuStrip.Location = new Point(100, 5); 
            titleBarMenuStrip.AutoSize = true; 
            titleBarMenuStrip.GripStyle = ToolStripGripStyle.Hidden; 
            titleBarMenuStrip.Padding = new Padding(0);
            titleBarMenuStrip.Margin = new Padding(0); 

            titleBar.Controls.Add(closeBtn);

            titleBar.Controls.Add(titleBarMenuStrip); 

            this.Controls.Add(titleBar); 
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

            ToolStripMenuItem toolsMenu = new ToolStripMenuItem("Tools")
            {
                ForeColor = Color.White
            };

            ToolStripMenuItem contextMenuManager = new ToolStripMenuItem("Context Menu Manager...")
            {
                ForeColor = Color.White
            };
            contextMenuManager.Click += (s, e) =>
            {
                using (var contextMenuForm = new ContextMenuManagerForm())
                {
                    contextMenuForm.ShowDialog(this);
                }
            };

            ToolStripSeparator separator = new ToolStripSeparator();

            ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("Exit")
            {
                ForeColor = Color.White
            };
            exitMenuItem.Click += (s, e) => this.Close();

            toolsMenu.DropDownItems.Add(contextMenuManager);
            toolsMenu.DropDownItems.Add(separator);
            toolsMenu.DropDownItems.Add(exitMenuItem);

            ToolStripMenuItem helpMenu = new ToolStripMenuItem("Help")
            {
                ForeColor = Color.White
            };

            ToolStripMenuItem aboutMenuItem = new ToolStripMenuItem("About...")
            {
                ForeColor = Color.White
            };
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

        private void CreateFileSelectionPanel()
        {
            fileSelectionPanel = new Panel
            {
                Size = new Size(840, 220),
                Location = new Point(20, 20),
                BackColor = Color.FromArgb(25, 25, 25)
            };
            fileSelectionPanel.Paint += (s, e) => DrawRoundedRectangle(e.Graphics, fileSelectionPanel.ClientRectangle, 15, Color.FromArgb(25, 25, 25));

            Label sectionTitle = new Label
            {
                Text = "📁 File Selection",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };

            selectFilesBtn = new RoundedButton
            {
                Text = "Add Files",
                Size = new Size(120, 40),
                Location = new Point(20, 50),
                BackColor = Color.FromArgb(0, 120, 212),
                ForeColor = Color.White
            };
            selectFilesBtn.Click += SelectFilesBtn_Click;

            selectFolderBtn = new RoundedButton
            {
                Text = "Add Folder",
                Size = new Size(120, 40),
                Location = new Point(160, 50),
                BackColor = Color.FromArgb(16, 110, 190),
                ForeColor = Color.White
            };
            selectFolderBtn.Click += SelectFolderBtn_Click;

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
                UpdateFileCount();
                extractBtn.Visible = false;
                extractBtn.Enabled = false;
                extractLabel.Visible = false;
                archiveContentComboBox.Visible = false;
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
            selectedFilesListBox.DrawItem += SelectedFilesListBox_DrawItem;
            selectedFilesListBox.MouseClick += SelectedFilesListBox_MouseClick;

            fileSelectionPanel.Controls.AddRange(new Control[]
            {
                sectionTitle, selectFilesBtn, selectFolderBtn, clearAllBtn,
                selectedFilesLabel, fileCountLabel, selectedFilesListBox
            });

            mainPanel.Controls.Add(fileSelectionPanel);
        }

        private void CreateOptionsPanel()
        {
            optionsPanel = new Panel
            {
                Size = new Size(840, 160),
                Location = new Point(20, 260),
                BackColor = Color.FromArgb(25, 25, 25)
            };
            optionsPanel.Paint += (s, e) => DrawRoundedRectangle(e.Graphics, optionsPanel.ClientRectangle, 15, Color.FromArgb(25, 25, 25));

            Label sectionTitle = new Label
            {
                Text = "⚙️ Compression Options",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };

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
                Location = new Point(160, 125),
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
            encryptionCheckBox.CheckedChanged += EncryptionCheckBox_CheckedChanged;

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
            passwordToggleBtn.Click += PasswordToggleBtn_Click;
            passwordToggleBtn.MouseEnter += (s, e) => passwordToggleBtn.BackColor = Color.FromArgb(70, 70, 70);
            passwordToggleBtn.MouseLeave += (s, e) => passwordToggleBtn.BackColor = Color.FromArgb(50, 50, 50);

            optionsPanel.Controls.AddRange(new Control[]
            {
                sectionTitle, algorithmLabel, algorithmComboBox, encryptionCheckBox,
                multithreadingCheckBox, passwordLabel, passwordTextBox, passwordToggleBtn,archiveContentComboBox,extractLabel
            });

            mainPanel.Controls.Add(optionsPanel);
        }

        private void CreateActionPanel()
        {
            actionPanel = new Panel
            {
                Size = new Size(840, 80),
                Location = new Point(20, 440),
                BackColor = Color.FromArgb(25, 25, 25)
            };
            actionPanel.Paint += (s, e) => DrawRoundedRectangle(e.Graphics, actionPanel.ClientRectangle, 15, Color.FromArgb(25, 25, 25));

            compressBtn = new RoundedButton
            {
                Text = "🗜️ Compress",
                Size = new Size(140, 45),
                Location = new Point(50, 20),
                BackColor = Color.FromArgb(46, 160, 67),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            };
            compressBtn.Click += CompressBtn_Click;

            decompressBtn = new RoundedButton
            {
                Text = "📦 Decompress",
                Size = new Size(140, 45),
                Location = new Point(220, 20),
                BackColor = Color.FromArgb(255, 140, 0),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            };
            decompressBtn.Click += DecompressBtn_Click;

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
            pauseBtn.Click += PauseBtn_Click;

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
            cancelBtn.Click += CancelBtn_Click;

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
            extractBtn.Click += ExtractBtn_Click;

            actionPanel.Controls.AddRange(new Control[]
            {
                compressBtn, decompressBtn, pauseBtn, cancelBtn, extractBtn
            });

            mainPanel.Controls.Add(actionPanel);
        }

        private void CreateProgressPanel()
        {
            progressPanel = new Panel
            {
                Size = new Size(840, 100),
                Location = new Point(20, 540),
                BackColor = Color.FromArgb(25, 25, 25)
            };
            progressPanel.Paint += (s, e) => DrawRoundedRectangle(e.Graphics, progressPanel.ClientRectangle, 15, Color.FromArgb(25, 25, 25));

            Label sectionTitle = new Label
            {
                Text = "📊 Progress",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };

            progressBar = new ProgressBar
            {
                Size = new Size(780, 25),
                Location = new Point(20, 45),
                Style = ProgressBarStyle.Continuous,
                ForeColor = Color.FromArgb(0, 120, 212)
            };

            statusLabel = new Label
            {
                Text = "Ready to compress files...",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(20, 50),
                AutoSize = true
            };

            timeLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(20, 75),
                AutoSize = true
            };

            compressionRatioLabel = new Label
            {
                Text = "Compression Ratio: ",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 160, 67),
                Location = new Point(650, 55),
                AutoSize = true
            };

            progressPanel.Controls.AddRange(new Control[]
            {
                sectionTitle, statusLabel, compressionRatioLabel, timeLabel
            });

            mainPanel.Controls.Add(progressPanel);
        }

        private void MainForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                lastCursor = Cursor.Position;
                lastForm = this.Location;
            }
        }

        private void MainForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point diff = Point.Subtract(Cursor.Position, new Size(lastCursor));
                this.Location = Point.Add(lastForm, new Size(diff));
            }
        }

        private void MainForm_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        private void SelectFilesBtn_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Multiselect = true;
                openFileDialog.Filter = "All files (*.*)|*.*";
                openFileDialog.Title = "Select Files to Compress";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (string file in openFileDialog.FileNames)
                    {
                        if (!selectedFilesListBox.Items.Contains(file))
                        {
                            selectedFilesListBox.Items.Add(file);
                        }
                    }
                    UpdateFileCount();
                }
            }
        }

        private void SelectFolderBtn_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "Select Folder to Compress";
                folderBrowserDialog.ShowNewFolderButton = false;

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    folderPath = folderBrowserDialog.SelectedPath;
                    if (!selectedFilesListBox.Items.Contains(folderPath))
                    {
                        selectedFilesListBox.Items.Add(folderPath);
                        UpdateFileCount();
                        if (folderPath.EndsWith(".huff_archive") || folderPath.EndsWith(".shf_archive"))
                        {
                            UpdateArchiveContentComboBox(folderPath);
                            extractBtn.Visible = true;
                            extractBtn.Enabled = true;
                            extractLabel.Visible = true;
                            archiveContentComboBox.Visible = true;
                        }
                    }
                }
            }
        }

        private void SelectedFilesListBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            e.DrawBackground();
            ListBox listBox = sender as ListBox;
            string path = listBox.Items[e.Index].ToString();

            string displayText;
            if (Directory.Exists(path))
            {
                displayText = "📁 " + Path.GetFileName(path) + " (Folder)";
            }
            else
            {
                displayText = "📄 " + Path.GetFileName(path);
            }

            using (SolidBrush brush = new SolidBrush(e.ForeColor))
            {
                e.Graphics.DrawString(displayText, e.Font, brush, e.Bounds.Left + 5, e.Bounds.Top + 4);
            }

            Rectangle xRect = new Rectangle(e.Bounds.Right - 25, e.Bounds.Top + 2, 20, 20);
            using (SolidBrush xBrush = new SolidBrush(Color.FromArgb(220, 53, 69)))
            {
                e.Graphics.FillRectangle(xBrush, xRect);
            }

            using (SolidBrush textBrush = new SolidBrush(Color.White))
            {
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                e.Graphics.DrawString("×", new Font("Arial", 10, FontStyle.Bold), textBrush, xRect, sf);
            }

            e.DrawFocusRectangle();
        }

        private void SelectedFilesListBox_MouseClick(object sender, MouseEventArgs e)
        {
            ListBox listBox = sender as ListBox;
            int index = listBox.IndexFromPoint(e.Location);

            if (index >= 0)
            {
                Rectangle itemRect = listBox.GetItemRectangle(index);
                Rectangle xRect = new Rectangle(itemRect.Right - 25, itemRect.Top + 2, 20, 20);

                if (xRect.Contains(e.Location))
                {
                    listBox.Items.RemoveAt(index);
                    UpdateFileCount();
                }
            }
        }

        private void UpdateFileCount()
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

        private void EncryptionCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            bool isChecked = encryptionCheckBox.Checked;
            passwordLabel.Visible = isChecked;
            passwordTextBox.Visible = isChecked;
            passwordToggleBtn.Visible = isChecked;

            if (isChecked)
            {
                passwordTextBox.Focus();
            }
            else
            {
                passwordTextBox.Clear();
                passwordTextBox.UseSystemPasswordChar = true;
                passwordToggleBtn.Text = "👁";
            }
        }

        private void PasswordToggleBtn_Click(object sender, EventArgs e)
        {
            if (passwordTextBox.UseSystemPasswordChar)
            {
                passwordTextBox.UseSystemPasswordChar = false;
                passwordToggleBtn.Text = "🙈";
            }
            else
            {
                passwordTextBox.UseSystemPasswordChar = true;
                passwordToggleBtn.Text = "👁";
            }
            passwordTextBox.Focus();
            passwordTextBox.SelectionStart = passwordTextBox.Text.Length;
        }

        private async void CompressBtn_Click(object sender, EventArgs e)
        {
            if (selectedFilesListBox.Items.Count == 0)
            {
                MessageBox.Show("Please select files or folders to compress.", "No Items Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool useMultithreading = multithreadingCheckBox.Checked;
            string algorithm = algorithmComboBox.SelectedItem.ToString();
            compressor = algorithm == "Huffman"
                ? (ICompressor)new HuffmanCompressor()
                : new ShannonFanoCompressor();
            HuffmanFolderCompression huffmanFolderCompressor = new HuffmanFolderCompression();
            ShannonFanoFolderCompression shannonFolderCompression = new ShannonFanoFolderCompression();
            
            if (useMultithreading)
            {
                statusLabel.Text = "⚡ Running with multithreading...";
                threadingStopwatch = Stopwatch.StartNew();
                SetProcessingState(true);
                progressBar.Maximum = selectedFilesListBox.Items.Count;
                progressBar.Value = 0;
                threadingCompletedCount = 0;
                threadingTotalCount = selectedFilesListBox.Items.Count;
                
                var pauseToken = pauseTokenSource.Token;
                
                foreach (string itemPath in selectedFilesListBox.Items)
                {
                    if (Directory.Exists(itemPath))
                    {
                        if(algorithm == "Huffman")
                        {
                            threadingService.FolderCompression(huffmanFolderCompressor, null, itemPath, pauseToken);
                        }
                        else
                        {
                            threadingService.FolderCompression(null, shannonFolderCompression, itemPath, pauseToken);
                        }
                    }
                    else
                    {
                        if (algorithm == "Huffman")
                        {
                            threadingService.FileCompression((HuffmanCompressor)compressor, null, itemPath, pauseToken);
                        }
                        else
                        {
                            threadingService.FileCompression(null, (ShannonFanoCompressor)compressor, itemPath, pauseToken);
                        }
                        
                    }
                }
            }
            else
            {
                await ProcessOperation(isCompression: true);
            }
        }

        private async void DecompressBtn_Click(object sender, EventArgs e)
        {
            if (selectedFilesListBox.Items.Count == 0)
            {
                MessageBox.Show("Please select compressed files or folders to decompress.", "No Items Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool useMultithreading = multithreadingCheckBox.Checked;
            string algorithm = algorithmComboBox.SelectedItem.ToString();
            compressor = algorithm == "Huffman"
                ? (ICompressor)new HuffmanCompressor()
                : new ShannonFanoCompressor();
            HuffmanFolderCompression huffmanFolderDecompressor = new HuffmanFolderCompression();
            ShannonFanoFolderCompression shannonFolderDecompression = new ShannonFanoFolderCompression();
            
            if (useMultithreading)
            {
                statusLabel.Text = "⚡ Running with multithreading...";
                threadingStopwatch = Stopwatch.StartNew();
                SetProcessingState(true);
                progressBar.Maximum = selectedFilesListBox.Items.Count;
                progressBar.Value = 0;
                threadingCompletedCount = 0;
                threadingTotalCount = selectedFilesListBox.Items.Count;
                
                var pauseToken = pauseTokenSource.Token;
                
                foreach (string itemPath in selectedFilesListBox.Items)
                {
                    string outputPath = GetDecompressionOutputPath(itemPath);

                    if (Directory.Exists(itemPath))
                    {
                        if (algorithm == "Huffman")
                        {
                            threadingService.FolderDecompression(huffmanFolderDecompressor, null, itemPath, outputPath, pauseToken);
                        }
                        else
                        {
                            threadingService.FolderDecompression(null, shannonFolderDecompression, itemPath, outputPath, pauseToken);
                        }
                    }
                    else
                    {
                        if (algorithm == "Huffman")
                        {
                            threadingService.FileDecompression((HuffmanCompressor)compressor, null, itemPath, outputPath, pauseToken);
                        }
                        else
                        {
                            threadingService.FileDecompression(null, (ShannonFanoCompressor)compressor, itemPath, outputPath, pauseToken);
                        }
                    }
                }
            }
            else
            {
                await ProcessOperation(isCompression: false);
            }
        }

        private void CancelBtn_Click(object sender, EventArgs e)
        {
            cancellationTokenSource?.Cancel();
            threadingService?.Cancel();
            statusLabel.Text = "❌ Operation cancelled by user";
        }

        private void PauseBtn_Click(object sender, EventArgs e)
        {
            if (pauseBtn.Text == "⏸️ Pause")
            {
                pauseBtn.Text = "▶️ Resume";
                statusLabel.Text = "⏸️ Paused...";
                pauseTokenSource.Pause(); 
            }
            else
            {
                pauseBtn.Text = "⏸️ Pause";
                statusLabel.Text = "▶️ Resuming...";
                pauseTokenSource.Resume(); 
            }
        }

        private async void ExtractBtn_Click(object sender, EventArgs e)
        {
            if (archiveContentComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a file to extract.", "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedFileName = archiveContentComboBox.SelectedItem.ToString();
            string selectedFilePath = Path.Combine(folderPath, selectedFileName);

            if (selectedFileName == "All")
            {
                bool useMultithreading = multithreadingCheckBox.Checked;
                string algorithm = algorithmComboBox.SelectedItem.ToString();
                HuffmanFolderCompression huffmanFolderDecompressor = new HuffmanFolderCompression();
                ShannonFanoFolderCompression shannonFolderDecompression = new ShannonFanoFolderCompression();
                if (useMultithreading)
                {
                    statusLabel.Text = "⚡ Running with multithreading...";
                    threadingStopwatch = Stopwatch.StartNew();
                    SetProcessingState(true);
                    threadingCompletedCount = 0;
                    threadingTotalCount = archiveContentComboBox.Items.Count;
                    foreach (string itemPath in archiveContentComboBox.Items)
                    {
                        string outputPath = GetDecompressionOutputPath(itemPath);
                        if (algorithm == "Huffman")
                        {
                            threadingService.FolderDecompression(huffmanFolderDecompressor, null, itemPath, outputPath);
                        }
                        else
                        {
                            threadingService.FolderDecompression(null, shannonFolderDecompression, itemPath, outputPath);
                        }
                    }
                }
                else
                {
                    await ProcessOperation(isCompression: false);
                }
            }
            else
            {
                try
                {
                    bool useMultithreading = multithreadingCheckBox.Checked;
                    string algorithm = algorithmComboBox.SelectedItem.ToString();
                    compressor = algorithm == "Huffman"
                        ? (ICompressor)new HuffmanCompressor()
                        : new ShannonFanoCompressor();

                    string outputPath = GetDecompressionOutputPath(selectedFilePath);
                    if (useMultithreading)
                    {
                        statusLabel.Text = "⚡ Running with multithreading...";
                    }
                    else
                    {
                        statusLabel.Text = $"Decompressing: {selectedFileName}...";
                    }
                    threadingStopwatch = Stopwatch.StartNew();
                    SetProcessingState(true);
                    threadingCompletedCount = 0;
                    threadingTotalCount = archiveContentComboBox.Items.Count;
                    if (algorithm == "Huffman")
                    {
                        threadingService.FileDecompression((HuffmanCompressor)compressor, null, selectedFilePath, outputPath);
                    }
                    else
                    {
                        threadingService.FileDecompression(null, (ShannonFanoCompressor)compressor, selectedFilePath, outputPath);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error extracting the file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async Task ProcessOperation(bool isCompression)
        {
            if (isProcessing) return;

            isProcessing = true;
            var stopwatch = Stopwatch.StartNew();
            cancellationTokenSource = new CancellationTokenSource();
            pauseTokenSource = new PauseTokenSource(); 
            var pauseToken = pauseTokenSource.Token;   
            SetProcessingState(true);

            try
            {
                var items = selectedFilesListBox.Items.Cast<string>().ToList();
                progressBar.Maximum = items.Count;
                progressBar.Value = 0;

                long totalOriginalSize = 0;
                long totalCompressedSize = 0;
                int processedItems = 0;

                string algorithm = algorithmComboBox.SelectedItem.ToString();
                ICompressor selectedCompressor = algorithm == "Huffman"
                    ? (ICompressor)new HuffmanCompressor()
                    : new ShannonFanoCompressor();
                
                IFolderCompression selectedFolderCompressor = algorithm == "Huffman"
                    ? (IFolderCompression)new HuffmanFolderCompression()
                    : new ShannonFanoFolderCompression();

                Dictionary<string, string> outputPaths = new Dictionary<string, string>();

                if (!isCompression)
                {
                    foreach (string itemPath in items)
                    {
                        string itemName = Path.GetFileName(itemPath);
                        using (var outputDialog = new OutputNameDialog(itemPath))
                        {
                            if (outputDialog.ShowDialog() != DialogResult.OK)
                            {
                                var result = MessageBox.Show(
                                    $"Output selection cancelled for {itemName}. Do you want to cancel the entire operation?",
                                    "Operation Cancelled",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Question);

                                if (result == DialogResult.Yes)
                                {
                                    statusLabel.Text = "❌ Operation cancelled by user";
                                    return;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            outputPaths[itemPath] = outputDialog.OutputPath;
                        }
                    }

                    if (outputPaths.Count == 0)
                    {
                        statusLabel.Text = "No items selected for processing.";
                        return;
                    }

                    items = outputPaths.Keys.ToList();
                }
                foreach (string itemPath in items)
                {
                    if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        statusLabel.Text = "❌ Operation cancelled by user";
                        break;
                    }

                    pauseToken.WaitIfPaused();
                    
                    try
                    {
                        bool isFolder = Directory.Exists(itemPath);
                        string itemName = Path.GetFileName(itemPath);

                        if (isCompression)
                        {
                            String localPassword = null;
                            if (encryptionCheckBox.Checked)
                            {
                                localPassword = passwordTextBox.Text;
                            }
                            if (isFolder)
                            {
                                statusLabel.Text = $"Compressing folder: {itemName}...";
                                var folderResult = await Task.Run(() => selectedFolderCompressor.CompressFolder(itemPath, cancellationTokenSource.Token, pauseToken, localPassword));
                                if (folderResult == null)
                                {
                                    break;
                                }
                                else
                                {
                                    totalOriginalSize += folderResult.TotalOriginalSize;
                                    totalCompressedSize += folderResult.TotalCompressedSize;
                                }
                            }
                            else
                            {
                                statusLabel.Text = $"Compressing: {itemName}...";
                                var result = await Task.Run(() => selectedCompressor.Compress(itemPath, cancellationTokenSource.Token, pauseToken, localPassword));
                                if (result == null)
                                {
                                    break;
                                }
                                else
                                {
                                    totalOriginalSize += result.OriginalSize;
                                    totalCompressedSize += result.CompressedSize;
                                }
                            }
                        }
                        else 
                        {
                            string outputPath = outputPaths[itemPath];
                            string password = null;

                            bool isEncrypted = false;
                            if (isFolder)
                            {
                                string archiveInfoPath = Path.Combine(itemPath, "archive_info.txt");
                                if (File.Exists(archiveInfoPath))
                                {
                                    try
                                    {
                                        string infoContent = File.ReadAllText(archiveInfoPath);
                                        isEncrypted = infoContent.Contains("Encrypted: Yes");
                                    }
                                    catch
                                    {
                                        isEncrypted = false;
                                    }
                                }

                                if (!isEncrypted)
                                {
                                    try
                                    {
                                        string[] compressedFiles = Directory.GetFiles(itemPath, "*.*", SearchOption.AllDirectories)
                                            .Where(f => f.EndsWith(".huff") || f.EndsWith(".shf"))
                                            .ToArray();
                                        isEncrypted = compressedFiles.Any(file => EncryptionHelper.IsFileEncrypted(file));
                                    }
                                    catch
                                    {
                                        isEncrypted = false;
                                    }
                                }
                            }
                            else
                            {
                                try
                                {
                                    isEncrypted = EncryptionHelper.IsFileEncrypted(itemPath);
                                }
                                catch
                                {
                                    isEncrypted = false;
                                }
                            }
                            if (isEncrypted)
                            {
                                using (var passwordDialog = new PasswordDialog())
                                {
                                    passwordDialog.Text = $"Password Required - {itemName}";
                                    if (passwordDialog.ShowDialog() != DialogResult.OK)
                                    {
                                        MessageBox.Show($"Password required for {itemName}. Skipping this file.",
                                            "Password Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        continue;
                                    }
                                    password = passwordDialog.EnteredPassword;

                                    if (string.IsNullOrWhiteSpace(password))
                                    {
                                        MessageBox.Show($"Password cannot be empty for {itemName}. Skipping this file.",
                                            "Invalid Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        continue;
                                    }
                                    bool passwordValid = false;
                                    try
                                    {
                                        if (isFolder)
                                        {
                                            string[] compressedFiles = Directory.GetFiles(itemPath, "*.*", SearchOption.TopDirectoryOnly)
                                                .Where(f => f.EndsWith(".huff") || f.EndsWith(".shf"))
                                                .ToArray();
                                            if (compressedFiles.Length > 0)
                                            {
                                                byte[] fileData = File.ReadAllBytes(compressedFiles[0]);
                                                EncryptionHelper.ValidatePassword(fileData, password);
                                                passwordValid = true;
                                            }
                                        }
                                        else
                                        {
                                            byte[] fileData = File.ReadAllBytes(itemPath);
                                            EncryptionHelper.ValidatePassword(fileData, password);
                                            passwordValid = true;
                                        }
                                    }
                                    catch
                                    {
                                        passwordValid = false;
                                    }

                                    if (!passwordValid)
                                    {
                                        MessageBox.Show($"Invalid password for {itemName}. Skipping this file.",
                                            "Invalid Password", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        continue;
                                    }
                                }
                            }

                            if (isFolder)
                            {
                                statusLabel.Text = $"Decompressing folder: {itemName}...";
                                await Task.Run(() => selectedFolderCompressor.DecompressFolder(itemPath, outputPath, cancellationTokenSource.Token, password));
                            }
                            else
                            {
                                statusLabel.Text = $"Decompressing: {itemName}...";
                                await Task.Run(() => selectedCompressor.Decompress(itemPath, outputPath, cancellationTokenSource.Token, password));
                            }
                        }

                        processedItems++;
                        progressBar.Value = processedItems;

                        if (isCompression && totalOriginalSize > 0)
                        {
                            var percent = new FolderCompressionResult
                            {
                                TotalOriginalSize = totalOriginalSize,
                                TotalCompressedSize = totalCompressedSize
                            }.OverallCompressionRatioPercent;

                            compressionRatioLabel.Text = $"Compression Ratio: {percent}";
                        }

                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("password") || ex.Message.Contains("decrypt") || ex.Message.Contains("encrypted"))
                        {
                            MessageBox.Show($"Decryption failed for {Path.GetFileName(itemPath)}. " +
                                "The password might be incorrect or the file/folder might be corrupted.\n\n" +
                                $"Error: {ex.Message}",
                                "Decryption Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            MessageBox.Show($"Error processing {Path.GetFileName(itemPath)}: {ex.Message}",
                                "Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }

                if (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    statusLabel.Text = isCompression ?
                        $"✅ Compression completed! Processed {processedItems} item(s)" :
                        $"✅ Decompression completed! Processed {processedItems} item(s)";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"⚠️ An error occurred: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Operation failed.";
            }
            finally
            {
                SetProcessingState(false);
                isProcessing = false;
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
                stopwatch.Stop();
                timeLabel.Text += $"⏱️ Time spent : {stopwatch.Elapsed.TotalSeconds:F2} sec ({stopwatch.Elapsed.TotalMilliseconds:F2} millisecond)";
            }
        }

        private string GetDecompressionOutputPath(string inputPath)
        {
            if (Directory.Exists(inputPath))
            {
                if (inputPath.EndsWith(".huff_archive"))
                {
                    return inputPath.Substring(0, inputPath.Length - ".huff_archive".Length);
                }
                else if (inputPath.EndsWith(".shf_archive"))
                {
                    return inputPath.Substring(0, inputPath.Length - ".shf_archive".Length);
                }
                return inputPath + "_decompressed";
            }
            string extension = Path.GetExtension(inputPath).ToLower();

            if (extension == ".huff" || extension == ".shf")
            {
                return Path.Combine(
                    Path.GetDirectoryName(inputPath),
                    Path.GetFileNameWithoutExtension(inputPath));
            }

            return inputPath + ".decompressed";
        }

        private void SetProcessingState(bool processing)
        {
            compressBtn.Enabled = !processing;
            decompressBtn.Enabled = !processing;
            pauseBtn.Enabled = processing;
            cancelBtn.Enabled = processing;

            selectFilesBtn.Enabled = !processing;
            selectFolderBtn.Enabled = !processing;
            algorithmComboBox.Enabled = !processing;
            encryptionCheckBox.Enabled = !processing;

            passwordTextBox.Enabled = !processing;
            passwordToggleBtn.Enabled = !processing;

            if (processing)
            {
                progressBar.Value = 0;
                compressionRatioLabel.Text = "Compression Ratio: 0%";
            }
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

        private void OnFileCompressed(CompressionResult result)
        {
            Invoke(new Action(() =>
            {
                progressBar.Value++;
                statusLabel.Text = $"✅ Compression completed!";
                compressionRatioLabel.Text = $"Compression Ratio: {result.CompressionRatioPercent}";
                threadingCompletedCount++;
                if (threadingCompletedCount == threadingTotalCount)
                {
                    threadingStopwatch.Stop();
                    timeLabel.Text += $"⏱️ Time spent : {threadingStopwatch.Elapsed.TotalSeconds:F2} sec ({threadingStopwatch.Elapsed.TotalMilliseconds:F2} millisecond)";
                    SetProcessingState(false);
                }
            }));
        }

        private void OnFolderCompressed(FolderCompressionResult result)
        {
            Invoke(new Action(() =>
            {
                progressBar.Value++;
                statusLabel.Text = $"✅ Compression completed!";
                compressionRatioLabel.Text = $"Compression Ratio: {result.OverallCompressionRatioPercent}";
                threadingCompletedCount++;
                if (threadingCompletedCount == threadingTotalCount)
                {
                    threadingStopwatch.Stop();
                    timeLabel.Text += $"⏱️ Time spent : {threadingStopwatch.Elapsed.TotalSeconds:F2} sec ({threadingStopwatch.Elapsed.TotalMilliseconds:F2} millisecond)";
                    SetProcessingState(false);
                }
            }));
        }

        private void OnFileDecompressed(string outputPath)
        {
            Invoke(new Action(() =>
            {
                progressBar.Value++;
                statusLabel.Text = $"✅ Decompression completed!";
                threadingCompletedCount++;
                if (threadingCompletedCount == threadingTotalCount)
                {
                    threadingStopwatch.Stop();
                    timeLabel.Text += $"⏱️ Time spent : {threadingStopwatch.Elapsed.TotalSeconds:F2} sec ({threadingStopwatch.Elapsed.TotalMilliseconds:F2} millisecond)";
                    SetProcessingState(false);
                }
            }));
            Console.WriteLine($"{Path.GetFileName(outputPath)}");
        }

        private void OnFolderDecompressed(string outputPath)
        {
            Invoke(new Action(() =>
            {
                progressBar.Value++;
                statusLabel.Text = $"✅ Decompression completed!";
                threadingCompletedCount++;
                if (threadingCompletedCount == threadingTotalCount)
                {
                    threadingStopwatch.Stop();
                    timeLabel.Text += $"⏱️ Time spent : {threadingStopwatch.Elapsed.TotalSeconds:F2} sec ({threadingStopwatch.Elapsed.TotalMilliseconds:F2} millisecond)";
                    SetProcessingState(false);
                }
            }));
            Console.WriteLine($"{Path.GetFileName(outputPath)}");
        }

        private void OnOperationFailed(Exception ex)
        {
            Invoke(new Action(() =>
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetProcessingState(false);
            }));
        }

        private void UpdateArchiveContentComboBox(string archivePath)
        {
            List<string> archiveContents = GetArchiveContents(archivePath);

            archiveContentComboBox.Items.Clear();
            archiveContentComboBox.Items.AddRange(archiveContents.ToArray());
            archiveContentComboBox.SelectedIndex = 0; 
        }

        private List<string> GetArchiveContents(string archivePath)
        {
            List<string> fileNames = new List<string>();

            try
            {
                string[] huffFiles = Directory.GetFiles(archivePath, "*.huff", SearchOption.AllDirectories);
                string[] shfFiles = Directory.GetFiles(archivePath, "*.shf", SearchOption.AllDirectories);
                
                string[] allCompressedFiles = huffFiles.Concat(shfFiles).ToArray();

                fileNames.Add("All");

                foreach (string file in allCompressedFiles)
                {
                    fileNames.Add(Path.GetFileName(file));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading archive contents: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return fileNames;
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

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length >= 3 && args[1] == "compress")
            {
                string filePath = args[2];
                if (File.Exists(filePath) || Directory.Exists(filePath))
                {
                    selectedFilesListBox.Items.Add(filePath);
                    UpdateFileCount();
                    SetDefaultOperation("compress");
                }
            }
            else if (args.Length >= 3 && args[1] == "decompress")
            {
                string filePath = args[2];
                if (File.Exists(filePath) || Directory.Exists(filePath))
                {
                    selectedFilesListBox.Items.Add(filePath);
                    UpdateFileCount();
                    SetDefaultOperation("decompress");
                }
            }
        }

        public void AddFilesFromCommandLine(System.Collections.Generic.List<string> filePaths)
        {
            if (filePaths == null || filePaths.Count == 0) return;

            try
            {
                foreach (string path in filePaths)
                {
                    if (!selectedFilesListBox.Items.Contains(path))
                    {
                        selectedFilesListBox.Items.Add(path);
                    }
                }
                UpdateFileCount();
                statusLabel.Text = $"Added {filePaths.Count} item(s) from context menu.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding files from command line: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void SetDefaultOperation(string operation)
        {
            try
            {
                switch (operation.ToLower())
                {
                    case "compress":
                        statusLabel.Text = "Ready to compress selected files. Click 'Compress' to begin.";
                        compressBtn.BackColor = Color.FromArgb(60, 179, 113); 
                        decompressBtn.BackColor = Color.FromArgb(255, 140, 0); 
                        break;

                    case "decompress":
                        statusLabel.Text = "Ready to decompress selected files. Click 'Decompress' to begin.";
                        decompressBtn.BackColor = Color.FromArgb(255, 165, 0); 
                        compressBtn.BackColor = Color.FromArgb(46, 160, 67); 
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting default operation: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    public class RoundedButton : Button
    {
        public RoundedButton()
        {
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(0, 0, 20, 20, 180, 90);
            path.AddArc(Width - 20, 0, 20, 20, 270, 90);
            path.AddArc(Width - 20, Height - 20, 20, 20, 0, 90);
            path.AddArc(0, Height - 20, 20, 20, 90, 90);
            path.CloseAllFigures();

            this.Region = new Region(path);

            using (SolidBrush brush = new SolidBrush(this.BackColor))
            {
                pevent.Graphics.FillPath(brush, path);
            }

            TextRenderer.DrawText(pevent.Graphics, this.Text, this.Font, this.ClientRectangle,
                this.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
        }
    }
}