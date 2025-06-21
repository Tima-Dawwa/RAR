using RAR.Core.Compression;
using RAR.Core.Interfaces;
using RAR.Helper;
using RAR.Services;
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
        // UI Controls
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
        private Label compressionRatioLabel;

        // Application state
        private bool isDragging = false;
        private Point lastCursor;
        private Point lastForm;
        private CancellationTokenSource cancellationTokenSource;
        private bool isProcessing = false;
        private ICompressor currentCompressor;
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

            // Header Panel
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
                Location = new Point(40, 30),
                AutoSize = true
            };

            subtitleLabel = new Label
            {
                Text = "Compress and decompress files and folders with Huffman | Shannon-Fano algorithms",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(40, 75),
                AutoSize = true
            };

            headerPanel.Controls.AddRange(new Control[] { titleLabel, subtitleLabel });

            // Main Panel
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

            // Footer Panel
            footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
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

            this.Controls.AddRange(new Control[] { mainPanel, headerPanel, footerPanel });
        }

        private void CreateTitleBar()
        {
            Panel titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.FromArgb(35, 35, 35)
            };
            titleBar.MouseDown += MainForm_MouseDown;
            titleBar.MouseMove += MainForm_MouseMove;
            titleBar.MouseUp += MainForm_MouseUp;

            // Close button
            Button closeBtn = new Button
            {
                Text = "✕",
                Size = new Size(45, 30),
                Dock = DockStyle.Right,
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

            // Maximize/Restore button
            Button maxBtn = new Button
            {
                Text = "🗖",
                Size = new Size(45, 30),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8F),
                TextAlign = ContentAlignment.MiddleCenter
            };
            maxBtn.FlatAppearance.BorderSize = 0;
            maxBtn.FlatAppearance.MouseDownBackColor = Color.FromArgb(70, 70, 70);
            maxBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 50, 50);
            maxBtn.Click += (s, e) =>
            {
                if (this.WindowState == FormWindowState.Maximized)
                {
                    this.WindowState = FormWindowState.Normal;
                    maxBtn.Text = "🗖";
                }
                else
                {
                    this.WindowState = FormWindowState.Maximized;
                    maxBtn.Text = "🗗";
                }
            };

            // Minimize button
            Button minBtn = new Button
            {
                Text = "−",
                Size = new Size(45, 30),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            minBtn.FlatAppearance.BorderSize = 0;
            minBtn.FlatAppearance.MouseDownBackColor = Color.FromArgb(70, 70, 70);
            minBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 50, 50);
            minBtn.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            // App title label (optional - shows app name in title bar)
            Label titleLabel = new Label
            {
                Text = "File Compression Tool",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(200, 30),
                Location = new Point(10, 0),
                TextAlign = ContentAlignment.MiddleLeft
            };
            titleLabel.MouseDown += MainForm_MouseDown;
            titleLabel.MouseMove += MainForm_MouseMove;
            titleLabel.MouseUp += MainForm_MouseUp;

            titleBar.Controls.Add(titleLabel);
            titleBar.Controls.Add(minBtn);
            titleBar.Controls.Add(maxBtn);
            titleBar.Controls.Add(closeBtn);

            this.Controls.Add(titleBar);
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
                Text = "👁️",
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
                multithreadingCheckBox, passwordLabel, passwordTextBox, passwordToggleBtn
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

            actionPanel.Controls.AddRange(new Control[]
            {
                compressBtn, decompressBtn, pauseBtn, cancelBtn
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
                Location = new Point(20, 75),
                AutoSize = true
            };

            compressionRatioLabel = new Label
            {
                Text = "Compression Ratio: 0%",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 160, 67),
                Location = new Point(650, 75),
                AutoSize = true
            };

            progressPanel.Controls.AddRange(new Control[]
            {
                sectionTitle, progressBar, statusLabel, compressionRatioLabel
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
                    string folderPath = folderBrowserDialog.SelectedPath;
                    if (!selectedFilesListBox.Items.Contains(folderPath))
                    {
                        selectedFilesListBox.Items.Add(folderPath);
                        UpdateFileCount();
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

            // Show different icons for files vs folders
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
                passwordToggleBtn.Text = "👁️";
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
                passwordToggleBtn.Text = "👁️";
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

            string algorithm = algorithmComboBox.SelectedItem.ToString();
            currentCompressor = algorithm == "Huffman"
                ? (ICompressor)new HuffmanCompressor()
                : new ShannonFanoCompressor();

            bool useMultithreading = multithreadingCheckBox.Checked;
            if (useMultithreading)
            {
                statusLabel.Text = "⚡ Running with multithreading...";
                threadingStopwatch = Stopwatch.StartNew();
                SetProcessingState(true);
                progressBar.Maximum = selectedFilesListBox.Items.Count;
                progressBar.Value = 0;
                threadingCompletedCount = 0;
                threadingTotalCount = selectedFilesListBox.Items.Count;
                foreach (string itemPath in selectedFilesListBox.Items)
                {
                    if (Directory.Exists(itemPath))
                    {
                        threadingService.FolderCompression(folderCompressor, itemPath);
                    }
                    else
                    {
                        threadingService.FileCompression((HuffmanCompressor)currentCompressor, itemPath);
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
            if (useMultithreading)
            {
                statusLabel.Text = "⚡ Running with multithreading...";
                threadingStopwatch = Stopwatch.StartNew();
                SetProcessingState(true);
                progressBar.Maximum = selectedFilesListBox.Items.Count;
                progressBar.Value = 0;
                threadingCompletedCount = 0;
                threadingTotalCount = selectedFilesListBox.Items.Count;
                foreach (string itemPath in selectedFilesListBox.Items)
                {
                    string outputPath = GetDecompressionOutputPath(itemPath);

                    if (Directory.Exists(itemPath))
                    {
                        threadingService.FolderDecompression(folderCompressor, itemPath, outputPath);
                    }
                    else
                    {
                        threadingService.FileDecompression((HuffmanCompressor)currentCompressor, itemPath, outputPath);
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
                statusLabel.Text = "Paused...";
            }
            else
            {
                pauseBtn.Text = "⏸️ Pause";
                statusLabel.Text = "Resuming...";
            }
        }

        private async Task ProcessOperation(bool isCompression)
        {
            if (isProcessing) return;

            isProcessing = true;
            var stopwatch = Stopwatch.StartNew();
            cancellationTokenSource = new CancellationTokenSource();
            SetProcessingState(true);

            try
            {
                var items = selectedFilesListBox.Items.Cast<string>().ToList();
                progressBar.Maximum = items.Count;
                progressBar.Value = 0;

                long totalOriginalSize = 0;
                long totalCompressedSize = 0;
                int processedItems = 0;

                // For decompression, collect output paths first
                Dictionary<string, string> outputPaths = new Dictionary<string, string>();

                if (!isCompression)
                {
                    // First, get all output paths
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
                                    // Skip this item
                                    continue;
                                }
                            }
                            outputPaths[itemPath] = outputDialog.OutputPath;
                        }
                    }

                    // If no items to process after output selection, return
                    if (outputPaths.Count == 0)
                    {
                        statusLabel.Text = "No items selected for processing.";
                        return;
                    }

                    // Update items list to only process items that have output paths
                    items = outputPaths.Keys.ToList();
                }

                // Process items
                foreach (string itemPath in items)
                {
                    if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        statusLabel.Text = "❌ Operation cancelled by user";
                        break;
                    }

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
                                var folderResult = await Task.Run(() => folderCompressor.CompressFolder(itemPath, cancellationTokenSource.Token, localPassword));
                                totalOriginalSize += folderResult.TotalOriginalSize;
                                totalCompressedSize += folderResult.TotalCompressedSize;
                            }
                            else
                            {
                                statusLabel.Text = $"Compressing: {itemName}...";
                                var result = await Task.Run(() => currentCompressor.Compress(itemPath, cancellationTokenSource.Token, localPassword));
                                if (result == null)
                                {
                                    //statusLabel.Text = $"❌ Operation cancelled by user";
                                    break;
                                }
                                else 
                                {
                                    totalOriginalSize += result.OriginalSize;
                                    totalCompressedSize += result.CompressedSize;
                                }
                                
                            }
                        }
                        else // Decompression
                        {
                            string outputPath = outputPaths[itemPath];
                            string password = null;

                            // Check if this specific file/folder is encrypted
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
                                    // Also check if any .huff files in the folder are encrypted
                                    try
                                    {
                                        string[] huffFiles = Directory.GetFiles(itemPath, "*.huff", SearchOption.AllDirectories);
                                        isEncrypted = huffFiles.Any(huffFile => EncryptionHelper.IsFileEncrypted(huffFile));
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

                            // Ask for password if this file is encrypted
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

                                    // Validate password for this specific file
                                    bool passwordValid = false;
                                    try
                                    {
                                        if (isFolder)
                                        {
                                            string[] huffFiles = Directory.GetFiles(itemPath, "*.huff", SearchOption.TopDirectoryOnly);
                                            if (huffFiles.Length > 0)
                                            {
                                                byte[] fileData = File.ReadAllBytes(huffFiles[0]);
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
                                await Task.Run(() => folderCompressor.DecompressFolder(itemPath, outputPath, cancellationTokenSource.Token, password));
                            }
                            else
                            {
                                statusLabel.Text = $"Decompressing: {itemName}...";
                                await Task.Run(() => currentCompressor.Decompress(itemPath, outputPath, cancellationTokenSource.Token, password));
                            }
                        }

                        processedItems++;
                        progressBar.Value = processedItems;

                        if (isCompression && totalOriginalSize > 0)
                        {
                            double ratio = (double)(totalOriginalSize - totalCompressedSize) / totalOriginalSize * 100;
                            compressionRatioLabel.Text = $"Compression Ratio: {ratio:F1}%";
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
                statusLabel.Text += $" ⏱️ Time: {stopwatch.Elapsed.TotalMilliseconds:F2} milliseconds";
            }
        }

        private string GetDecompressionOutputPath(string inputPath)
        {
            // For folders
            if (Directory.Exists(inputPath))
            {
                if (inputPath.EndsWith(".huff_archive"))
                {
                    return inputPath.Substring(0, inputPath.Length - ".huff_archive".Length);
                }
                return inputPath + "_decompressed";
            }

            // For files
            string extension = Path.GetExtension(inputPath).ToLower();

            if (extension == ".huff" || extension == ".sf")
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

            if (!processing)
            {
                progressBar.Value = 0;
                if (!encryptionCheckBox.Checked)
                {
                    compressionRatioLabel.Text = "Compression Ratio: 0%";
                }
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
                    statusLabel.Text += $" ⏱️ Time: {threadingStopwatch.Elapsed.TotalMilliseconds:F2} milliseconds";
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
                    statusLabel.Text += $" ⏱️ Time: {threadingStopwatch.Elapsed.TotalMilliseconds:F2} milliseconds";
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
                    statusLabel.Text += $" ⏱️ Time: {threadingStopwatch.Elapsed.TotalMilliseconds:F2} milliseconds";
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
                    statusLabel.Text += $" ⏱️ Time: {threadingStopwatch.Elapsed.TotalMilliseconds:F2} milliseconds";
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
            this.BackColor = ControlPaint.Light(this.BackColor, 0.1f);
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
        }
    }
}