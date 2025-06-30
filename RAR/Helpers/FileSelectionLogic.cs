using RAR.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace RAR.Helpers
{
    public class FileSelectionLogic
    {
        private readonly ListBox _selectedFilesListBox;
        private readonly Label _fileCountLabel;
        private readonly RoundedButton _extractBtn;
        private readonly Label _extractLabel;
        private readonly ComboBox _archiveContentComboBox;

        public event EventHandler FileCountUpdated;
        public event EventHandler<string> ArchiveFolderSelected; // Event to notify MainForm when an archive folder is selected

        public FileSelectionLogic(ListBox selectedFilesListBox, Label fileCountLabel, RoundedButton extractBtn, Label extractLabel, ComboBox archiveContentComboBox)
        {
            _selectedFilesListBox = selectedFilesListBox;
            _fileCountLabel = fileCountLabel;
            _extractBtn = extractBtn;
            _extractLabel = extractLabel;
            _archiveContentComboBox = archiveContentComboBox;
        }

        public void SelectFilesBtn_Click()
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
                        if (!_selectedFilesListBox.Items.Contains(file))
                        {
                            _selectedFilesListBox.Items.Add(file);
                        }
                    }
                    OnFileCountUpdated();
                }
            }
        }

        public void SelectFolderBtn_Click()
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "Select Folder to Compress";
                folderBrowserDialog.ShowNewFolderButton = false;

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    string folderPath = folderBrowserDialog.SelectedPath;
                    if (!_selectedFilesListBox.Items.Contains(folderPath))
                    {
                        _selectedFilesListBox.Items.Add(folderPath);
                        OnFileCountUpdated();
                        if (folderPath.EndsWith(".huff_archive") || folderPath.EndsWith(".shf_archive"))
                        {
                            ArchiveFolderSelected?.Invoke(this, folderPath);
                        }
                    }
                }
            }
        }

        public void SelectedFilesListBox_DrawItem(object sender, DrawItemEventArgs e)
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

        public void SelectedFilesListBox_MouseClick(object sender, MouseEventArgs e)
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
                    OnFileCountUpdated();
                    // If the removed item was an archive folder, hide extraction controls
                    if (!(_selectedFilesListBox.Items.Cast<string>().Any(item => item.EndsWith(".huff_archive") || item.EndsWith(".shf_archive"))))
                    {
                        _extractBtn.Visible = false;
                        _extractBtn.Enabled = false;
                        _extractLabel.Visible = false;
                        _archiveContentComboBox.Visible = false;
                        _archiveContentComboBox.Items.Clear();
                    }
                }
            }
        }

        public void UpdateFileCount()
        {
            int count = _selectedFilesListBox.Items.Count;
            int folderCount = 0;
            int fileCount = 0;

            foreach (string item in _selectedFilesListBox.Items)
            {
                if (Directory.Exists(item))
                    folderCount++;
                else
                    fileCount++;
            }

            if (count == 0)
            {
                _fileCountLabel.Text = "Count: 0 items";
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
                _fileCountLabel.Text = countText;
            }
        }

        public void AddFiles(List<string> filePaths)
        {
            if (filePaths == null || filePaths.Count == 0) return;

            foreach (string path in filePaths)
            {
                if (!_selectedFilesListBox.Items.Contains(path))
                {
                    _selectedFilesListBox.Items.Add(path);
                }
            }
            OnFileCountUpdated();
        }

        public void UpdateArchiveContentComboBox(string archivePath)
        {
            List<string> archiveContents = GetArchiveContents(archivePath);

            _archiveContentComboBox.Items.Clear();
            _archiveContentComboBox.Items.AddRange(archiveContents.ToArray());
            if (archiveContents.Any())
            {
                _archiveContentComboBox.SelectedIndex = 0;
            }
        }

        private List<string> GetArchiveContents(string archivePath)
        {
            List<string> fileNames = new List<string>();

            try
            {
                string[] huffFiles = Directory.GetFiles(archivePath, "*.huff", SearchOption.AllDirectories);
                string[] shfFiles = Directory.GetFiles(archivePath, "*.shf", SearchOption.AllDirectories);

                string[] allCompressedFiles = huffFiles.Concat(shfFiles).ToArray();

                fileNames.Add("All"); // Option to extract all files

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

        protected virtual void OnFileCountUpdated()
        {
            FileCountUpdated?.Invoke(this, EventArgs.Empty);
        }
    }


}
