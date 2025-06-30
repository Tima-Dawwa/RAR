using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using RAR.Helpers;

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

        // Helper Class Instances
        private MainFormUILayout _uiLayout;
        private FileSelectionLogic _fileSelectionLogic;
        private OperationLogic _operationLogic;
        private PasswordToggleLogic _passwordToggleLogic;
        private DragWindowLogic _dragWindowLogic; // Static class, but for consistent naming with other logic handlers

        // Existing state variables
        private CancellationTokenSource cancellationTokenSource; // Managed by OperationLogic now, but declared here for MainForm's reference
        private PauseTokenSource pauseTokenSource = new PauseTokenSource();
        private bool isProcessing = false;
        private string folderPathForExtraction; // Renamed from folderPath to be more descriptive

        public MainForm()
        {
            InitializeComponent();
            InitializeStyle();

            _uiLayout = new MainFormUILayout(this); 

            SetupUI();                               
            InitializeHelperClasses();             
            SetupEventHandlers();
            SetInitialControlStates();
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
            this.ResumeLayout(false);
        }

        private void InitializeHelperClasses()
        {
            _uiLayout = new MainFormUILayout(this);
            _fileSelectionLogic = new FileSelectionLogic(selectedFilesListBox, fileCountLabel, extractBtn, extractLabel, archiveContentComboBox);
            _operationLogic = new OperationLogic(statusLabel, progressBar, compressionRatioLabel, timeLabel, multithreadingCheckBox,
                                                 compressBtn, decompressBtn, pauseBtn, cancelBtn,
                                                 selectedFilesListBox, encryptionCheckBox, passwordTextBox, archiveContentComboBox);
            _passwordToggleLogic = new PasswordToggleLogic(passwordLabel, passwordTextBox, passwordToggleBtn);
            _dragWindowLogic = new DragWindowLogic(); // Static class instance for consistent event wiring pattern
        }

        private void SetupUI()
        {
            // Build Title Bar
            _uiLayout.BuildTitleBar(out var titleBar, out var closeBtn, out var titleBarMenuStrip);
            titleBar.Controls.Add(closeBtn);
            titleBar.Controls.Add(titleBarMenuStrip);
            this.Controls.Add(titleBar);

            // Create and add Header Panel
            headerPanel = _uiLayout.CreateHeaderPanel(out titleLabel, out subtitleLabel);
            headerPanel.Controls.AddRange(new Control[] { titleLabel, subtitleLabel });
            this.Controls.Add(headerPanel);

            // Create Footer Panel first (so it docks to bottom before main panel fills)
            footerPanel = _uiLayout.CreateFooterPanel();
            Label footerLabel = _uiLayout.CreateFooterLabel();
            footerPanel.Controls.Add(footerLabel);
            this.Controls.Add(footerPanel);

            // Create Main Panel - this will now fill the remaining space between header and footer
            mainPanel = _uiLayout.CreateMainPanel();
            this.Controls.Add(mainPanel);

            // Create and add File Selection Panel
            fileSelectionPanel = _uiLayout.CreateFileSelectionPanel(
                out selectFilesBtn, out selectFolderBtn, out selectedFilesListBox,
                out selectedFilesLabel, out fileCountLabel);
            fileSelectionPanel.Controls.AddRange(new Control[]
            {
                _uiLayout.CreateSectionTitle("📁 File Selection", new Point(20, 15)),
                selectFilesBtn,
                selectFolderBtn,
                _uiLayout.CreateClearAllButton(selectedFilesListBox, fileCountLabel, extractBtn, extractLabel, archiveContentComboBox),
                selectedFilesLabel,
                fileCountLabel,
                selectedFilesListBox
            });
            mainPanel.Controls.Add(fileSelectionPanel);

            // Create and add Options Panel
            optionsPanel = _uiLayout.CreateOptionsPanel(
                out algorithmLabel, out algorithmComboBox, out encryptionCheckBox,
                out multithreadingCheckBox, out passwordLabel, out passwordTextBox,
                out passwordToggleBtn, out archiveContentComboBox, out extractLabel);
            optionsPanel.Controls.AddRange(new Control[]
            {
                _uiLayout.CreateSectionTitle("⚙️ Compression Options", new Point(20, 15)),
                algorithmLabel,
                algorithmComboBox,
                encryptionCheckBox,
                multithreadingCheckBox,
                passwordLabel,
                passwordTextBox,
                passwordToggleBtn,
                archiveContentComboBox,
                extractLabel
            });
            mainPanel.Controls.Add(optionsPanel);

            // Create and add Action Panel
            actionPanel = _uiLayout.CreateActionPanel(
                out compressBtn, out decompressBtn, out pauseBtn, out cancelBtn, out extractBtn);
            actionPanel.Controls.AddRange(new Control[]
            {
                compressBtn, decompressBtn, pauseBtn, cancelBtn, extractBtn
            });
            mainPanel.Controls.Add(actionPanel);

            // Create and add Progress Panel
            progressPanel = _uiLayout.CreateProgressPanel(
                out progressBar, out statusLabel, out compressionRatioLabel, out timeLabel);
            progressPanel.Controls.AddRange(new Control[]
            {
                _uiLayout.CreateSectionTitle("📊 Progress", new Point(20, 15)),
                progressBar, statusLabel, compressionRatioLabel, timeLabel
            });
            mainPanel.Controls.Add(progressPanel);
        }

        private void SetupEventHandlers()
        {
            // Drag window handlers (using the helper)
            headerPanel.MouseDown += _dragWindowLogic.MainForm_MouseDown;
            headerPanel.MouseMove += _dragWindowLogic.MainForm_MouseMove;
            headerPanel.MouseUp += _dragWindowLogic.MainForm_MouseUp;
            this.MouseDown += _dragWindowLogic.MainForm_MouseDown;
            this.MouseMove += _dragWindowLogic.MainForm_MouseMove;
            this.MouseUp += _dragWindowLogic.MainForm_MouseUp;

            // File Selection Logic
            selectFilesBtn.Click += (s, e) => _fileSelectionLogic.SelectFilesBtn_Click();
            selectFolderBtn.Click += (s, e) => _fileSelectionLogic.SelectFolderBtn_Click();
            selectedFilesListBox.DrawItem += _fileSelectionLogic.SelectedFilesListBox_DrawItem;
            selectedFilesListBox.MouseClick += _fileSelectionLogic.SelectedFilesListBox_MouseClick;
            _fileSelectionLogic.FileCountUpdated += (s, e) => UpdateFileCountLabel();
            _fileSelectionLogic.ArchiveFolderSelected += (s, path) =>
            {
                folderPathForExtraction = path;
                _fileSelectionLogic.UpdateArchiveContentComboBox(path);
                extractBtn.Visible = true;
                extractBtn.Enabled = true;
                extractLabel.Visible = true;
                archiveContentComboBox.Visible = true;
            };

            // Password Toggle Logic
            encryptionCheckBox.CheckedChanged += (s, e) => _passwordToggleLogic.EncryptionCheckBox_CheckedChanged(encryptionCheckBox.Checked);
            passwordToggleBtn.Click += (s, e) => _passwordToggleLogic.PasswordToggleBtn_Click();
            passwordToggleBtn.MouseEnter += (s, e) => passwordToggleBtn.BackColor = Color.FromArgb(70, 70, 70);
            passwordToggleBtn.MouseLeave += (s, e) => passwordToggleBtn.BackColor = Color.FromArgb(50, 50, 50);

            // Operation Logic
            compressBtn.Click += async (s, e) => await _operationLogic.CompressBtn_Click(algorithmComboBox.SelectedItem.ToString(), passwordTextBox.Text, multithreadingCheckBox.Checked, pauseTokenSource, cancellationTokenSource);
            decompressBtn.Click += async (s, e) => await _operationLogic.DecompressBtn_Click(algorithmComboBox.SelectedItem.ToString(), passwordTextBox.Text, multithreadingCheckBox.Checked, pauseTokenSource, cancellationTokenSource);
            cancelBtn.Click += (s, e) => _operationLogic.CancelBtn_Click();
            pauseBtn.Click += (s, e) => _operationLogic.PauseBtn_Click(pauseBtn, statusLabel, pauseTokenSource);
            extractBtn.Click += async (s, e) => await _operationLogic.ExtractBtn_Click(archiveContentComboBox.SelectedItem?.ToString(), folderPathForExtraction, algorithmComboBox.SelectedItem.ToString(), passwordTextBox.Text, multithreadingCheckBox.Checked, pauseTokenSource, cancellationTokenSource);

            // Subscribe to OperationLogic events to update UI
            _operationLogic.OperationStarted += (s, args) => Invoke((Action)(() =>
            {
                isProcessing = true;
                SetProcessingState(true);
                progressBar.Maximum = args.TotalItems;
                progressBar.Value = 0;
                statusLabel.Text = args.StatusMessage;
                timeLabel.Text = "";
                compressionRatioLabel.Text = "Compression Ratio: Calculating ..";
            }));

            _operationLogic.ProgressUpdated += (s, args) => Invoke((Action)(() =>
            {
                progressBar.Value = args.ProcessedItems;
                statusLabel.Text = args.StatusMessage;
                if (args.CompressionRatio.HasValue)
                {
                    compressionRatioLabel.Text = $"Compression Ratio: {args.CompressionRatio.Value:F2}%";
                }
                timeLabel.Text = $"⏱️ Time spent : {args.ElapsedTime.TotalSeconds:F2} sec ({args.ElapsedTime.TotalMilliseconds:F2} millisecond)";
            }));

            _operationLogic.OperationCompleted += (s, args) => Invoke((Action)(() =>
            {
                isProcessing = false;
                SetProcessingState(false);
                statusLabel.Text = args.StatusMessage;
                timeLabel.Text = $"⏱️ Time spent : {args.ElapsedTime.TotalSeconds:F2} sec ({args.ElapsedTime.TotalMilliseconds:F2} millisecond)";
                if (args.CompressionRatio.HasValue)
                {
                    compressionRatioLabel.Text = $"Compression Ratio: {args.CompressionRatio.Value:F2}%";
                }
                else
                {
                    compressionRatioLabel.Text = "Compression Ratio: N/A";
                }
                // Clear the listbox after successful compression/decompression
                selectedFilesListBox.Items.Clear();
                UpdateFileCountLabel();
                archiveContentComboBox.Items.Clear();
                extractBtn.Visible = false;
                extractBtn.Enabled = false;
                extractLabel.Visible = false;
                archiveContentComboBox.Visible = false;
            }));

            _operationLogic.OperationFailed += (s, ex) => Invoke((Action)(() =>
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Operation failed.";
                isProcessing = false;
                SetProcessingState(false);
            }));
        }

        private void SetInitialControlStates()
        {
            SetProcessingState(false);
            passwordLabel.Visible = encryptionCheckBox.Checked;
            passwordTextBox.Visible = encryptionCheckBox.Checked;
            passwordToggleBtn.Visible = encryptionCheckBox.Checked;
        }

        private void UpdateFileCountLabel()
        {
            _fileSelectionLogic.UpdateFileCount();
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
            multithreadingCheckBox.Enabled = !processing;

            if (processing)
            {
                progressBar.Value = 0;
                compressionRatioLabel.Text = "Compression Ratio: Calculating .. ";
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length >= 3)
            {
                string operation = args[1];
                string filePath = args[2];

                if (File.Exists(filePath) || Directory.Exists(filePath))
                {
                    _fileSelectionLogic.AddFiles(new List<string> { filePath });
                    SetDefaultOperation(operation);
                }
            }
        }

        public void AddFilesFromCommandLine(List<string> filePaths)
        {
            _fileSelectionLogic.AddFiles(filePaths);
            statusLabel.Text = $"Added {filePaths.Count} item(s) from context menu.";
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

    public class OperationStartedEventArgs : EventArgs
    {
        public string StatusMessage { get; }
        public int TotalItems { get; }

        public OperationStartedEventArgs(string statusMessage, int totalItems)
        {
            StatusMessage = statusMessage;
            TotalItems = totalItems;
        }
    }

    public class ProgressUpdatedEventArgs : EventArgs
    {
        public int ProcessedItems { get; }
        public string StatusMessage { get; }
        public TimeSpan ElapsedTime { get; }
        public double? CompressionRatio { get; }

        public ProgressUpdatedEventArgs(int processedItems, string statusMessage, TimeSpan elapsedTime, double? compressionRatio)
        {
            ProcessedItems = processedItems;
            StatusMessage = statusMessage;
            ElapsedTime = elapsedTime;
            CompressionRatio = compressionRatio;
        }
    }

    public class OperationCompletedEventArgs : EventArgs
    {
        public string StatusMessage { get; }
        public TimeSpan ElapsedTime { get; }
        public double? CompressionRatio { get; }

        public OperationCompletedEventArgs(string statusMessage, TimeSpan elapsedTime, double? compressionRatio)
        {
            StatusMessage = statusMessage;
            ElapsedTime = elapsedTime;
            CompressionRatio = compressionRatio;
        }
    }

}