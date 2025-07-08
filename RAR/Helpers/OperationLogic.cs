using RAR.Core.Compression;
using RAR.Core.Interfaces;
using RAR.Helper; 
using RAR.Services;
using System;
using System.Collections.Generic;
using System.Drawing; 
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms; 

namespace RAR.UI
{
    public class OperationLogic
    {
        private readonly Label _statusLabel;
        private readonly ProgressBar _progressBar;
        private readonly Label _compressionRatioLabel;
        private readonly Label _timeLabel;
        private readonly CheckBox _multithreadingCheckBox;
        private readonly RoundedButton _compressBtn;
        private readonly RoundedButton _decompressBtn;
        private readonly RoundedButton _pauseBtn;
        private readonly RoundedButton _cancelBtn;
        private readonly ListBox _selectedFilesListBox;
        private readonly CheckBox _encryptionCheckBox;
        private readonly TextBox _passwordTextBox;
        private readonly ComboBox _archiveContentComboBox;

        private ICompressor _currentFileCompressor;
        private IFolderCompression _currentFolderCompressor;
        private ThreadingService _threadingService;

        private CancellationTokenSource _cancellationTokenSource;
        private Stopwatch _threadingStopwatch;
        private Stopwatch _processingStopwatch;
        private int _threadingCompletedCount = 0;
        private int _threadingTotalCount = 0;
        private long _totalOriginalSize = 0;
        private long _totalCompressedSize = 0;

        public event EventHandler<OperationStartedEventArgs> OperationStarted;
        public event EventHandler<ProgressUpdatedEventArgs> ProgressUpdated;
        public event EventHandler<OperationCompletedEventArgs> OperationCompleted;
        public event EventHandler<Exception> OperationFailed;

        public OperationLogic(Label statusLabel, ProgressBar progressBar, Label compressionRatioLabel, Label timeLabel, CheckBox multithreadingCheckBox,
                              RoundedButton compressBtn, RoundedButton decompressBtn, RoundedButton pauseBtn, RoundedButton cancelBtn,
                              ListBox selectedFilesListBox, CheckBox encryptionCheckBox, TextBox passwordTextBox, ComboBox archiveContentComboBox)
        {
            _statusLabel = statusLabel;
            _progressBar = progressBar;
            _compressionRatioLabel = compressionRatioLabel;
            _timeLabel = timeLabel;
            _multithreadingCheckBox = multithreadingCheckBox;
            _compressBtn = compressBtn;
            _decompressBtn = decompressBtn;
            _pauseBtn = pauseBtn;
            _cancelBtn = cancelBtn;
            _selectedFilesListBox = selectedFilesListBox;
            _encryptionCheckBox = encryptionCheckBox;
            _passwordTextBox = passwordTextBox;
            _archiveContentComboBox = archiveContentComboBox;

            _threadingService = new ThreadingService();
            _threadingService.FileCompressionCompleted += OnFileCompressed;
            _threadingService.FolderCompressionCompleted += OnFolderCompressed;
            _threadingService.FileDecompressionCompleted += OnFileDecompressed;
            _threadingService.FolderDecompressionCompleted += OnFolderDecompressed;
            _threadingService.OperationFailed += OnOperationFailed;
        }

        public async Task CompressBtn_Click(string algorithm, string password, bool useMultithreading, PauseTokenSource pauseTokenSource, CancellationTokenSource mainFormCancellationTokenSource)
        {
            if (_selectedFilesListBox.Items.Count == 0)
            {
                MessageBox.Show("Please select files or folders to compress.", "No Items Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _cancellationTokenSource = mainFormCancellationTokenSource ?? new CancellationTokenSource();
            _currentFileCompressor = algorithm == "Huffman" ? (ICompressor)new HuffmanCompressor() : new ShannonFanoCompressor();
            _currentFolderCompressor = algorithm == "Huffman" ? (IFolderCompression)new HuffmanFolderCompression() : new ShannonFanoFolderCompression();

            _threadingCompletedCount = 0;
            _threadingTotalCount = _selectedFilesListBox.Items.Count;
            _totalOriginalSize = 0;
            _totalCompressedSize = 0;

            _processingStopwatch = Stopwatch.StartNew();
            OperationStarted?.Invoke(this, new OperationStartedEventArgs("Starting compression...", _threadingTotalCount));
            SetProcessingState(true);

            try
            {
                if (useMultithreading)
                {
                    _threadingStopwatch = Stopwatch.StartNew();
                    foreach (string itemPath in _selectedFilesListBox.Items)
                    {
                        string localPassword = password;
                        bool isEncrypted = File.Exists(itemPath)
                        ? EncryptionHelper.IsFileEncrypted(itemPath)
                        : EncryptionHelper.IsFolderEncrypted(itemPath);
                        if (string.IsNullOrEmpty(localPassword) && isEncrypted) 
                        {
                            using (var passwordDialog = new PasswordDialog())
                            {
                                passwordDialog.Text = $"Password Required - {itemPath}";
                                if (passwordDialog.ShowDialog() != DialogResult.OK)
                                {
                                    MessageBox.Show($"Password required for {itemPath}. Skipping this file.", "Password Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    continue;
                                }
                                localPassword = passwordDialog.EnteredPassword;
                                if (string.IsNullOrWhiteSpace(localPassword))
                                {
                                    MessageBox.Show($"Password cannot be empty for {itemPath}. Skipping this file.", "Invalid Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    continue;
                                }
                                try
                                {
                                    EncryptionHelper.ValidatePassword(File.ReadAllBytes(itemPath), localPassword);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Invalid password for {itemPath}: {ex.Message}. Skipping this file.", "Invalid Password", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    continue;
                                }
                            }
                        }

                        _statusLabel.Text = $"Compressing using multithreading: {itemPath}...";

                        if (Directory.Exists(itemPath))
                        {
                            _threadingService.FolderCompression(_currentFolderCompressor as HuffmanFolderCompression, _currentFolderCompressor as ShannonFanoFolderCompression, itemPath, pauseTokenSource.Token, localPassword);
                        }
                        else
                        {
                            _threadingService.FileCompression(_currentFileCompressor as HuffmanCompressor, _currentFileCompressor as ShannonFanoCompressor, itemPath, pauseTokenSource.Token, localPassword);
                        }
                    }
                }
                else
                {
                    await ProcessOperation(true, algorithm, password, pauseTokenSource);
                }
            }
            catch (OperationCanceledException)
            {
                OperationCompleted?.Invoke(this, new OperationCompletedEventArgs("❌ Operation cancelled by user", useMultithreading ? _threadingStopwatch.Elapsed : _processingStopwatch.Elapsed, null));
            }
            catch (Exception ex)
            {
                OperationFailed?.Invoke(this, ex);
            }
            finally
            {
                _processingStopwatch?.Stop();
            }
        }

        public async Task DecompressBtn_Click(string algorithm, string password, bool useMultithreading, PauseTokenSource pauseTokenSource, CancellationTokenSource mainFormCancellationTokenSource)
        {
            if (_selectedFilesListBox.Items.Count == 0)
            {
                MessageBox.Show("Please select compressed files or folders to decompress.", "No Items Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _cancellationTokenSource = mainFormCancellationTokenSource ?? new CancellationTokenSource();
            _currentFileCompressor = algorithm == "Huffman" ? (ICompressor)new HuffmanCompressor() : new ShannonFanoCompressor();
            _currentFolderCompressor = algorithm == "Huffman" ? (IFolderCompression)new HuffmanFolderCompression() : new ShannonFanoFolderCompression();

            _threadingCompletedCount = 0;
            _threadingTotalCount = _selectedFilesListBox.Items.Count;

            _processingStopwatch = Stopwatch.StartNew();
            OperationStarted?.Invoke(this, new OperationStartedEventArgs("Starting decompression...", _threadingTotalCount));
            SetProcessingState(true);

            try
            {
                if (useMultithreading)
                {
                    _threadingStopwatch = Stopwatch.StartNew();
                    foreach (string itemPath in _selectedFilesListBox.Items)
                    {
                        string outputPath = GetDecompressionOutputPath(itemPath);
                        string archiveInfoPath = Path.Combine(itemPath, "archive_info.txt");

                        bool isFolder = Directory.Exists(itemPath);
                        string itemName = Path.GetFileName(itemPath);

                        string localPassword = password;
                        bool isEncrypted = File.Exists(itemPath)
                        ? EncryptionHelper.IsFileEncrypted(itemPath)
                        : EncryptionHelper.IsFolderEncrypted(itemPath);

                        if (isFolder)
                        {
                            if (File.Exists(archiveInfoPath))
                            {
                                try { isEncrypted = File.ReadAllText(archiveInfoPath).Contains("Encrypted: Yes"); } catch {}
                            }
                            if (!isEncrypted) 
                            {
                                try
                                {
                                    string[] compressedFilesInFolder = Directory.GetFiles(itemPath, "*.*", SearchOption.AllDirectories)
                                                                        .Where(f => f.EndsWith(".huff") || f.EndsWith(".shf"))
                                                                        .ToArray();
                                    isEncrypted = compressedFilesInFolder.Any(file => EncryptionHelper.IsFileEncrypted(file));
                                }
                                catch {}
                            }
                        }
                        else
                        {
                            try { isEncrypted = EncryptionHelper.IsFileEncrypted(itemPath); } catch {}
                        }

                        if (isEncrypted && string.IsNullOrEmpty(localPassword))
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
                                localPassword = passwordDialog.EnteredPassword;
                                if (string.IsNullOrWhiteSpace(localPassword))
                                {
                                    MessageBox.Show($"Password cannot be empty for {itemName}. Skipping this file.",
                                                    "Invalid Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    continue;
                                }
                                try 
                                {
                                    bool isValidPassword = false;
                                    if (isFolder)
                                    {
                                        string[] compressedFiles = Directory.GetFiles(itemPath, "*.*", SearchOption.AllDirectories) 
                                                                            .Where(f => f.EndsWith(".huff") || f.EndsWith(".shf"))
                                                                            .ToArray();
                                        if (compressedFiles.Length > 0)
                                        {
                                            isValidPassword = EncryptionHelper.ValidatePassword(File.ReadAllBytes(compressedFiles[0]), localPassword);
                                        }
                                    }
                                    else
                                    {
                                        isValidPassword = EncryptionHelper.ValidatePassword(File.ReadAllBytes(itemPath), localPassword);
                                    }

                                    if (!isValidPassword)
                                    {
                                        MessageBox.Show($"Invalid password for {itemName}. Skipping this file.", "Invalid Password", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        continue;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Error validating password for {itemName}: {ex.Message}. Skipping this file.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    continue;
                                }
                            }
                        }

                        _statusLabel.Text = $"Decompressing using multithreading: {itemName}...";

                        if (isFolder)
                        {
                            _threadingService.FolderDecompression(_currentFolderCompressor as HuffmanFolderCompression, _currentFolderCompressor as ShannonFanoFolderCompression, itemPath, outputPath, pauseTokenSource.Token, localPassword);
                        }
                        else
                        {
                            _threadingService.FileDecompression(_currentFileCompressor as HuffmanCompressor, _currentFileCompressor as ShannonFanoCompressor, itemPath, outputPath, pauseTokenSource.Token, localPassword);
                        }
                    }
                }
                else
                {
                    await ProcessOperation(false, algorithm, password, pauseTokenSource);
                }
            }
            catch (OperationCanceledException)
            {
                OperationCompleted?.Invoke(this, new OperationCompletedEventArgs("❌ Operation cancelled by user", useMultithreading ? _threadingStopwatch.Elapsed : _processingStopwatch.Elapsed, null));
            }
            catch (Exception ex)
            {
                OperationFailed?.Invoke(this, ex);
            }
            finally
            {
                _processingStopwatch?.Stop();
            }
        }

        public void CancelBtn_Click()
        {
            _cancellationTokenSource?.Cancel();
            _threadingService?.Cancel();
            OperationCompleted?.Invoke(this, new OperationCompletedEventArgs("❌ Operation cancelled by user", _multithreadingCheckBox.Checked ? _threadingStopwatch.Elapsed : _processingStopwatch.Elapsed, null));
        }

        public void PauseBtn_Click(RoundedButton pauseBtn, Label statusLabel, PauseTokenSource pauseTokenSource)
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

        public async Task ExtractBtn_Click(string selectedItem, string folderPathForExtraction, string algorithm, string password, bool useMultithreading, PauseTokenSource pauseTokenSource, CancellationTokenSource mainFormCancellationTokenSource)
        {
            if (selectedItem == null || string.IsNullOrEmpty(folderPathForExtraction))
            {
                MessageBox.Show("Please select a file to extract and ensure an archive folder is selected.", "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _cancellationTokenSource = mainFormCancellationTokenSource ?? new CancellationTokenSource();
            _currentFileCompressor = algorithm == "Huffman" ? (ICompressor)new HuffmanCompressor() : new ShannonFanoCompressor();
            _currentFolderCompressor = algorithm == "Huffman" ? (IFolderCompression)new HuffmanFolderCompression() : new ShannonFanoFolderCompression();

            _threadingCompletedCount = 0;
            _threadingTotalCount = _archiveContentComboBox.Items.Count; 

            _processingStopwatch = Stopwatch.StartNew();
            OperationStarted?.Invoke(this, new OperationStartedEventArgs($"Starting extraction of {selectedItem}...", _threadingTotalCount));
            SetProcessingState(true);

            try
            {
                if (selectedItem == "All")
                {
                    List<string> filesToExtract = GetArchiveContents(folderPathForExtraction).Where(f => f != "All").ToList();
                    if (useMultithreading)
                    {
                        _threadingStopwatch = Stopwatch.StartNew();
                        foreach (string itemPath in filesToExtract)
                        {
                            string fullPath = Path.Combine(folderPathForExtraction, itemPath);
                            string outputPath = GetDecompressionOutputPath(fullPath);

                            string localPassword = password;
                            if (string.IsNullOrEmpty(localPassword) && (EncryptionHelper.IsFileEncrypted(fullPath))) 
                            {
                                using (var passwordDialog = new PasswordDialog())
                                {
                                    passwordDialog.Text = $"Password Required - {itemPath}";
                                    if (passwordDialog.ShowDialog() != DialogResult.OK)
                                    {
                                        MessageBox.Show($"Password required for {itemPath}. Skipping this file.", "Password Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        continue;
                                    }
                                    localPassword = passwordDialog.EnteredPassword;
                                    if (string.IsNullOrWhiteSpace(localPassword))
                                    {
                                        MessageBox.Show($"Password cannot be empty for {itemPath}. Skipping this file.", "Invalid Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        continue;
                                    }
                                    try
                                    {
                                        EncryptionHelper.ValidatePassword(File.ReadAllBytes(fullPath), localPassword);
                                    }
                                    catch (Exception ex)
                                    {
                                        MessageBox.Show($"Invalid password for {itemPath}: {ex.Message}. Skipping this file.", "Invalid Password", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        continue;
                                    }
                                }
                            }
                            if (fullPath.EndsWith(".huff") || fullPath.EndsWith(".shf"))
                            {
                                _threadingService.FileDecompression(_currentFileCompressor as HuffmanCompressor, _currentFileCompressor as ShannonFanoCompressor, fullPath, outputPath, pauseTokenSource.Token, localPassword);
                            }
                        }
                    }
                    else
                    {
                        int processedItems = 0;
                        foreach (string itemPath in filesToExtract)
                        {
                            _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                            pauseTokenSource.Token.WaitIfPaused();

                            string fullPath = Path.Combine(folderPathForExtraction, itemPath);
                            string outputPath = GetDecompressionOutputPath(fullPath);
                            _statusLabel.Text = $"Extracting: {itemPath}...";

                            string localPassword = password;
                            if (string.IsNullOrEmpty(localPassword) && (EncryptionHelper.IsFileEncrypted(fullPath)))
                            {
                                using (var passwordDialog = new PasswordDialog())
                                {
                                    passwordDialog.Text = $"Password Required - {itemPath}";
                                    if (passwordDialog.ShowDialog() != DialogResult.OK)
                                    {
                                        MessageBox.Show($"Password required for {itemPath}. Skipping this file.", "Password Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        continue;
                                    }
                                    localPassword = passwordDialog.EnteredPassword;
                                    if (string.IsNullOrWhiteSpace(localPassword))
                                    {
                                        MessageBox.Show($"Password cannot be empty for {itemPath}. Skipping this file.", "Invalid Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        continue;
                                    }
                                    try
                                    {
                                        EncryptionHelper.ValidatePassword(File.ReadAllBytes(fullPath), localPassword);
                                    }
                                    catch (Exception ex)
                                    {
                                        MessageBox.Show($"Invalid password for {itemPath}: {ex.Message}. Skipping this file.", "Invalid Password", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        continue;
                                    }
                                }
                            }

                            if (fullPath.EndsWith(".huff") || fullPath.EndsWith(".shf"))
                            {
                                await Task.Run(() => _currentFileCompressor.Decompress(fullPath, outputPath, _cancellationTokenSource.Token, localPassword));
                            }
                            processedItems++;
                            ProgressUpdated?.Invoke(this, new ProgressUpdatedEventArgs(processedItems, $"Extracting: {itemPath}", _processingStopwatch.Elapsed, null));
                        }
                        OperationCompleted?.Invoke(this, new OperationCompletedEventArgs($"✅ Extraction all completed: {processedItems}", _processingStopwatch.Elapsed, null));
                    }
                }
                else
                {
                    string filePathToExtract = Path.Combine(folderPathForExtraction, selectedItem);
                    if (useMultithreading)
                    {
                        _threadingStopwatch = Stopwatch.StartNew();
                        string outputPath = GetDecompressionOutputPath(filePathToExtract);
                        string localPassword = password;
                        if (string.IsNullOrEmpty(localPassword) && (EncryptionHelper.IsFileEncrypted(filePathToExtract))) // Simplified check
                        {
                            using (var passwordDialog = new PasswordDialog())
                            {
                                passwordDialog.Text = $"Password Required - {selectedItem}";
                                if (passwordDialog.ShowDialog() != DialogResult.OK)
                                {
                                    MessageBox.Show($"Password required for {selectedItem}. Skipping this file.", "Password Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                                localPassword = passwordDialog.EnteredPassword;
                                if (string.IsNullOrWhiteSpace(localPassword))
                                {
                                    MessageBox.Show($"Password cannot be empty for {selectedItem}. Skipping this file.", "Invalid Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                                try
                                {
                                    EncryptionHelper.ValidatePassword(File.ReadAllBytes(filePathToExtract), localPassword);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Invalid password for {selectedItem}: {ex.Message}. Skipping this file.", "Invalid Password", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return;
                                }
                            }
                        }
                        if (filePathToExtract.EndsWith(".huff") || filePathToExtract.EndsWith(".shf"))
                        {
                            _threadingService.FileDecompression(_currentFileCompressor as HuffmanCompressor, _currentFileCompressor as ShannonFanoCompressor, filePathToExtract, outputPath, pauseTokenSource.Token, localPassword);
                        }
                    }
                    else
                    {
                        _statusLabel.Text = $"Extracting: {selectedItem}...";
                        string localPassword = password;
                        if (string.IsNullOrEmpty(localPassword) && (EncryptionHelper.IsFileEncrypted(filePathToExtract)))
                        {
                            using (var passwordDialog = new PasswordDialog())
                            {
                                passwordDialog.Text = $"Password Required - {selectedItem}";
                                if (passwordDialog.ShowDialog() != DialogResult.OK)
                                {
                                    MessageBox.Show($"Password required for {selectedItem}. Skipping this file.", "Password Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                                localPassword = passwordDialog.EnteredPassword;
                                if (string.IsNullOrWhiteSpace(localPassword))
                                {
                                    MessageBox.Show($"Password cannot be empty for {selectedItem}. Skipping this file.", "Invalid Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                                try
                                {
                                    EncryptionHelper.ValidatePassword(File.ReadAllBytes(filePathToExtract), localPassword);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Invalid password for {selectedItem}: {ex.Message}. Skipping this file.", "Invalid Password", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return;
                                }
                            }
                        }

                        string outputPath = GetDecompressionOutputPath(filePathToExtract);
                        await Task.Run(() => _currentFileCompressor.Decompress(filePathToExtract, outputPath, _cancellationTokenSource.Token, localPassword));
                        OperationCompleted?.Invoke(this, new OperationCompletedEventArgs($"✅ Extraction completed: {selectedItem}", _processingStopwatch.Elapsed, null));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                OperationCompleted?.Invoke(this, new OperationCompletedEventArgs("❌ Operation cancelled by user", useMultithreading ? _threadingStopwatch.Elapsed : _processingStopwatch.Elapsed, null));
            }
            catch (Exception ex)
            {
                OperationFailed?.Invoke(this, ex);
            }
            finally
            {
                _processingStopwatch?.Stop();
            }
        }

        private async Task ProcessOperation(bool isCompression, string algorithm, string password, PauseTokenSource pauseTokenSource)
        {
            int processedItems = 0;
            _totalOriginalSize = 0;
            _totalCompressedSize = 0;

            var items = _selectedFilesListBox.Items.Cast<string>().ToList();

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
                                _cancellationTokenSource.Cancel();
                                OperationCompleted?.Invoke(this, new OperationCompletedEventArgs("❌ Operation cancelled by user", _processingStopwatch.Elapsed, null));
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
                    _statusLabel.Text = "No items selected for processing.";
                    OperationCompleted?.Invoke(this, new OperationCompletedEventArgs("No items processed.", _processingStopwatch.Elapsed, null));
                    return;
                }
                items = outputPaths.Keys.ToList();
            }

            foreach (string itemPath in items)
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    OperationCompleted?.Invoke(this, new OperationCompletedEventArgs("❌ Operation cancelled by user", _processingStopwatch.Elapsed, null));
                    break;
                }

                pauseTokenSource.Token.WaitIfPaused();

                try
                {
                    bool isFolder = Directory.Exists(itemPath);
                    string itemName = Path.GetFileName(itemPath);

                    if (isCompression)
                    {
                        string localPassword = password;
                        if (isFolder)
                        {
                            _statusLabel.Text = $"Compressing folder: {itemName}...";
                            var folderResult = await Task.Run(() => _currentFolderCompressor.CompressFolder(itemPath, _cancellationTokenSource.Token, pauseTokenSource.Token, localPassword));
                            if (folderResult != null)
                            {
                                _totalOriginalSize += folderResult.TotalOriginalSize;
                                _totalCompressedSize += folderResult.TotalCompressedSize;
                            }
                        }
                        else
                        {
                            _statusLabel.Text = $"Compressing: {itemName}...";
                            var result = await Task.Run(() => _currentFileCompressor.Compress(itemPath, _cancellationTokenSource.Token, pauseTokenSource.Token, localPassword));
                            if (result != null)
                            {
                                _totalOriginalSize += result.OriginalSize;
                                _totalCompressedSize += result.CompressedSize;
                            }
                        }
                    }
                    else 
                    {
                        string outputPath = outputPaths[itemPath];
                        string localPassword = password;

                        bool isEncrypted = false;
                        if (isFolder)
                        {
                            string archiveInfoPath = Path.Combine(itemPath, "archive_info.txt");
                            if (File.Exists(archiveInfoPath))
                            {
                                try { isEncrypted = File.ReadAllText(archiveInfoPath).Contains("Encrypted: Yes"); } catch {  }
                            }
                            if (!isEncrypted)
                            {
                                try
                                {
                                    string[] compressedFilesInFolder = Directory.GetFiles(itemPath, "*.*", SearchOption.AllDirectories)
                                                                        .Where(f => f.EndsWith(".huff") || f.EndsWith(".shf"))
                                                                        .ToArray();
                                    isEncrypted = compressedFilesInFolder.Any(file => EncryptionHelper.IsFileEncrypted(file));
                                }
                                catch { }
                            }
                        }
                        else
                        {
                            try { isEncrypted = EncryptionHelper.IsFileEncrypted(itemPath); } catch {  }
                        }

                        if (isEncrypted && string.IsNullOrEmpty(localPassword))
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
                                localPassword = passwordDialog.EnteredPassword;
                                if (string.IsNullOrWhiteSpace(localPassword))
                                {
                                    MessageBox.Show($"Password cannot be empty for {itemName}. Skipping this file.",
                                                    "Invalid Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    continue;
                                }
                                try 
                                {
                                    bool isValidPassword = false;
                                    if (isFolder)
                                    {
                                        string[] compressedFiles = Directory.GetFiles(itemPath, "*.*", SearchOption.AllDirectories) // Also fix the SearchOption issue
                                                                            .Where(f => f.EndsWith(".huff") || f.EndsWith(".shf"))
                                                                            .ToArray();
                                        if (compressedFiles.Length > 0)
                                        {
                                            isValidPassword = EncryptionHelper.ValidatePassword(File.ReadAllBytes(compressedFiles[0]), localPassword);
                                        }
                                    }
                                    else
                                    {
                                        isValidPassword = EncryptionHelper.ValidatePassword(File.ReadAllBytes(itemPath), localPassword);
                                    }

                                    if (!isValidPassword)
                                    {
                                        MessageBox.Show($"Invalid password for {itemName}. Skipping this file.", "Invalid Password", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        continue;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Error validating password for {itemName}: {ex.Message}. Skipping this file.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    continue;
                                }
                            }
                        }

                        _statusLabel.Text = $"Decompressing: {itemName}...";
                        if (isFolder)
                        {
                            await Task.Run(() => _currentFolderCompressor.DecompressFolder(itemPath, outputPath, _cancellationTokenSource.Token, localPassword));
                        }
                        else
                        {
                            await Task.Run(() => _currentFileCompressor.Decompress(itemPath, outputPath, _cancellationTokenSource.Token, localPassword));
                        }
                    }

                    processedItems++;
                    double? compressionRatio = null;
                    if (isCompression && _totalOriginalSize > 0)
                    {
                        compressionRatio = (_totalOriginalSize - _totalCompressedSize) * 100.0 / _totalOriginalSize;
                    }
                    ProgressUpdated?.Invoke(this, new ProgressUpdatedEventArgs(processedItems, isCompression ? $"Compressing: {itemName}" : $"Decompressing: {itemName}", _processingStopwatch.Elapsed, compressionRatio));
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

            if (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                OperationCompleted?.Invoke(this, new OperationCompletedEventArgs(
                    isCompression ? $"✅ Compression completed! Processed {processedItems} item(s)" : $"✅ Decompression completed! Processed {processedItems} item(s)",
                    _processingStopwatch.Elapsed, _totalOriginalSize > 0 ? (double?)((_totalOriginalSize - _totalCompressedSize) * 100.0 / _totalOriginalSize) : null));
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

        private void OnFileCompressed(RAR.Helper.CompressionResult result) 
        {
            Interlocked.Increment(ref _threadingCompletedCount);
            Interlocked.Add(ref _totalOriginalSize, result.OriginalSize);
            Interlocked.Add(ref _totalCompressedSize, result.CompressedSize);
            ProgressUpdated?.Invoke(this, new ProgressUpdatedEventArgs(_threadingCompletedCount, $"Compressing using multithreading: {Path.GetFileName(result.CompressedFilePath)}", _threadingStopwatch.Elapsed, result.CompressionRatio));

            if (_threadingCompletedCount == _threadingTotalCount)
            {
                _threadingStopwatch.Stop();
                OperationCompleted?.Invoke(this, new OperationCompletedEventArgs("✅ Compression completed!", _threadingStopwatch.Elapsed, result.CompressionRatio));
            }
        }

        private void OnFolderCompressed(RAR.Helper.FolderCompressionResult result) 
        {
            Interlocked.Increment(ref _threadingCompletedCount);
            Interlocked.Add(ref _totalOriginalSize, result.TotalOriginalSize);
            Interlocked.Add(ref _totalCompressedSize, result.TotalCompressedSize);
            ProgressUpdated?.Invoke(this, new ProgressUpdatedEventArgs(_threadingCompletedCount, $"Compressing folder using multithreading: {Path.GetFileName(result.OriginalFolderPath)}", _threadingStopwatch.Elapsed, result.OverallCompressionRatio));

            if (_threadingCompletedCount == _threadingTotalCount)
            {
                _threadingStopwatch.Stop();
                OperationCompleted?.Invoke(this, new OperationCompletedEventArgs("✅ Compression completed!", _threadingStopwatch.Elapsed, result.OverallCompressionRatio));
            }
        }

        private void OnFileDecompressed(string outputPath)
        {
            Interlocked.Increment(ref _threadingCompletedCount);
            ProgressUpdated?.Invoke(this, new ProgressUpdatedEventArgs(_threadingCompletedCount, $"Decompressing using multithreading: {Path.GetFileName(outputPath)}", _threadingStopwatch.Elapsed, null));

            if (_threadingCompletedCount == _threadingTotalCount)
            {
                _threadingStopwatch.Stop();
                OperationCompleted?.Invoke(this, new OperationCompletedEventArgs("✅ Decompression completed!", _threadingStopwatch.Elapsed, null));
            }
        }

        private void OnFolderDecompressed(string outputPath)
        {
            Interlocked.Increment(ref _threadingCompletedCount);
            ProgressUpdated?.Invoke(this, new ProgressUpdatedEventArgs(_threadingCompletedCount, $"Decompressing folder using multithreading: {Path.GetFileName(outputPath)}", _threadingStopwatch.Elapsed, null));

            if (_threadingCompletedCount == _threadingTotalCount)
            {
                _threadingStopwatch.Stop();
                OperationCompleted?.Invoke(this, new OperationCompletedEventArgs("✅ Decompression completed!", _threadingStopwatch.Elapsed, null));
            }
        }

        private void OnOperationFailed(Exception ex)
        {
            OperationFailed?.Invoke(this, ex);
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

        private void SetProcessingState(bool processing)
        {
            _compressBtn.Enabled = !processing;
            _decompressBtn.Enabled = !processing;
            _pauseBtn.Enabled = processing;
            _cancelBtn.Enabled = processing;

            _multithreadingCheckBox.Enabled = !processing;
            _encryptionCheckBox.Enabled = !processing;
            _passwordTextBox.Enabled = _encryptionCheckBox.Checked && !processing;

            if (processing)
            {
                _progressBar.Value = 0;
                _compressionRatioLabel.Text = "Compression Ratio: Calculating .. ";
            }
        }
    }
}