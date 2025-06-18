using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace RAR.UI
{
    public partial class OutputNameDialog : Form
    {
        private TextBox outputNameTextBox;
        private Button okButton;
        private Button cancelButton;
        private Button browseButton;
        private Label instructionLabel;
        private Label pathLabel;

        public string OutputPath { get; private set; }
        public string OriginalPath { get; private set; }

        public OutputNameDialog(string originalPath)
        {
            OriginalPath = originalPath;
            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = "Choose Output Name";
            this.Size = new Size(500, 220);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(240, 240, 240);

            instructionLabel = new Label
            {
                Text = "Enter the name for the decompressed file/folder:",
                Location = new Point(20, 20),
                Size = new Size(450, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60)
            };

            pathLabel = new Label
            {
                Text = $"Original: {Path.GetFileName(OriginalPath)}",
                Location = new Point(20, 45),
                Size = new Size(450, 20),
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            outputNameTextBox = new TextBox
            {
                Size = new Size(350, 25),
                Location = new Point(20, 75),
                Font = new Font("Segoe UI", 9F),
                Text = GetDefaultOutputName()
            };

            browseButton = new Button
            {
                Text = "Browse...",
                Size = new Size(80, 25),
                Location = new Point(380, 75),
                Font = new Font("Segoe UI", 8F),
                BackColor = Color.FromArgb(225, 225, 225),
                FlatStyle = FlatStyle.Flat
            };
            browseButton.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            browseButton.Click += BrowseButton_Click;

            okButton = new Button
            {
                Text = "OK",
                Size = new Size(100, 35),
                Location = new Point(180, 120),
                DialogResult = DialogResult.OK,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 212),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            okButton.FlatAppearance.BorderSize = 0;
            okButton.Click += OkButton_Click;

            cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 35),
                Location = new Point(290, 120),
                DialogResult = DialogResult.Cancel,
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(220, 220, 220),
                ForeColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat
            };
            cancelButton.FlatAppearance.BorderSize = 0;
            cancelButton.Click += (s, e) => this.Close();

            // Add hover effects
            okButton.MouseEnter += (s, e) => okButton.BackColor = Color.FromArgb(16, 110, 190);
            okButton.MouseLeave += (s, e) => okButton.BackColor = Color.FromArgb(0, 120, 212);

            cancelButton.MouseEnter += (s, e) => cancelButton.BackColor = Color.FromArgb(200, 200, 200);
            cancelButton.MouseLeave += (s, e) => cancelButton.BackColor = Color.FromArgb(220, 220, 220);

            browseButton.MouseEnter += (s, e) => browseButton.BackColor = Color.FromArgb(210, 210, 210);
            browseButton.MouseLeave += (s, e) => browseButton.BackColor = Color.FromArgb(225, 225, 225);

            this.Controls.Add(instructionLabel);
            this.Controls.Add(pathLabel);
            this.Controls.Add(outputNameTextBox);
            this.Controls.Add(browseButton);
            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;

            // Focus on text box and select all text
            outputNameTextBox.Focus();
            outputNameTextBox.SelectAll();
        }

        private string GetDefaultOutputName()
        {
            string fileName = Path.GetFileNameWithoutExtension(OriginalPath);
            string directory = Path.GetDirectoryName(OriginalPath);

            // Remove compression extensions
            if (fileName.EndsWith(".huff") || fileName.EndsWith(".sf"))
            {
                fileName = Path.GetFileNameWithoutExtension(fileName);
            }

            // Handle folder archives
            if (fileName.EndsWith("_archive"))
            {
                fileName = fileName.Substring(0, fileName.Length - "_archive".Length);
            }

            return Path.Combine(directory, fileName);
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            bool isFolder = Directory.Exists(OriginalPath) ||
                           OriginalPath.EndsWith(".huff_archive") ||
                           Path.GetFileName(OriginalPath).Contains("_archive");

            if (isFolder)
            {
                using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "Select output folder location";
                    folderDialog.SelectedPath = Path.GetDirectoryName(outputNameTextBox.Text);

                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        string folderName = Path.GetFileName(outputNameTextBox.Text);
                        if (string.IsNullOrEmpty(folderName))
                        {
                            folderName = Path.GetFileNameWithoutExtension(OriginalPath);
                        }
                        outputNameTextBox.Text = Path.Combine(folderDialog.SelectedPath, folderName);
                    }
                }
            }
            else
            {
                using (SaveFileDialog saveDialog = new SaveFileDialog())
                {
                    saveDialog.Title = "Choose output file location";
                    saveDialog.FileName = Path.GetFileName(outputNameTextBox.Text);
                    saveDialog.InitialDirectory = Path.GetDirectoryName(outputNameTextBox.Text);
                    saveDialog.Filter = "All files (*.*)|*.*";

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        outputNameTextBox.Text = saveDialog.FileName;
                    }
                }
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            string outputPath = outputNameTextBox.Text.Trim();

            if (string.IsNullOrEmpty(outputPath))
            {
                MessageBox.Show("Please enter a valid output name.", "Invalid Name",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                outputNameTextBox.Focus();
                return;
            }

            // Auto-add extension if missing (for files only, not folders)
            try
            {
                outputPath = EnsureProperExtension(outputPath);
            }
            catch (OperationCanceledException)
            {
                // User cancelled extension selection, return to allow them to edit
                outputNameTextBox.Focus();
                return;
            }

            // Validate the path
            try
            {
                string directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    var result = MessageBox.Show(
                        $"The directory '{directory}' does not exist. Do you want to create it?",
                        "Directory Not Found",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        Directory.CreateDirectory(directory);
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        return;
                    }
                    else
                    {
                        outputNameTextBox.Focus();
                        return;
                    }
                }

                // Check if output already exists
                if (File.Exists(outputPath) || Directory.Exists(outputPath))
                {
                    var result = MessageBox.Show(
                        $"'{Path.GetFileName(outputPath)}' already exists. Do you want to replace it?",
                        "File/Folder Exists",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result != DialogResult.Yes)
                    {
                        outputNameTextBox.Focus();
                        return;
                    }
                }

                OutputPath = outputPath;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Invalid path: {ex.Message}", "Invalid Path",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                outputNameTextBox.Focus();
            }
        }

        private string EnsureProperExtension(string outputPath)
        {
            // Check if this is a folder operation
            bool isFolder = Directory.Exists(OriginalPath) ||
                           OriginalPath.EndsWith(".huff_archive") ||
                           Path.GetFileName(OriginalPath).Contains("_archive");

            // For folders, don't add extensions
            if (isFolder)
            {
                return outputPath;
            }

            // For files, determine the expected extension
            string expectedExtension = GetExpectedFileExtension();

            if (string.IsNullOrEmpty(expectedExtension))
            {
                // No expected extension, return as is
                return outputPath;
            }

            // Check if the output path already has an extension
            string currentExtension = Path.GetExtension(outputPath);

            if (string.IsNullOrEmpty(currentExtension))
            {
                // No extension provided, add the expected one
                return outputPath + expectedExtension;
            }
            else if (!currentExtension.Equals(expectedExtension, StringComparison.OrdinalIgnoreCase))
            {
                // Different extension provided, ask user what to do
                var result = MessageBox.Show(
                    $"The file will be decompressed as a {expectedExtension.TrimStart('.')} file, but you specified {currentExtension}.\n\n" +
                    $"Do you want to use the correct extension ({expectedExtension})?\n\n" +
                    "Yes: Use correct extension\n" +
                    "No: Keep your extension\n" +
                    "Cancel: Go back and edit",
                    "Extension Mismatch",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    // Replace with correct extension
                    return Path.ChangeExtension(outputPath, expectedExtension);
                }
                else if (result == DialogResult.Cancel)
                {
                    // This will be handled by the caller
                    throw new OperationCanceledException("User cancelled extension selection");
                }
                // If No, keep the user's extension
            }

            return outputPath;
        }

        private string GetExpectedFileExtension()
        {
            string originalFileName = Path.GetFileName(OriginalPath);
            string originalExtension = Path.GetExtension(OriginalPath).ToLower();

            // Handle different compression formats
            switch (originalExtension)
            {
                case ".huff":
                    // For .huff files, the original extension is usually preserved
                    // e.g., "document.txt.huff" should become "document.txt"
                    string nameWithoutHuff = Path.GetFileNameWithoutExtension(OriginalPath);
                    string innerExtension = Path.GetExtension(nameWithoutHuff);
                    return string.IsNullOrEmpty(innerExtension) ? "" : innerExtension;

                case ".sf":
                    // Similar logic for .sf files
                    string nameWithoutSf = Path.GetFileNameWithoutExtension(OriginalPath);
                    string sfInnerExtension = Path.GetExtension(nameWithoutSf);
                    return string.IsNullOrEmpty(sfInnerExtension) ? "" : sfInnerExtension;

                default:
                    // For other formats or if we can't determine, return empty
                    // This could be enhanced based on your specific compression formats
                    return "";
            }
        }
    }
}