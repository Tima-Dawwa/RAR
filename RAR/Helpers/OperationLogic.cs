using RAR.Core.Compression;
using RAR.Core.Interfaces;
using RAR.Helper; // This namespace now contains your CompressionResult and FolderCompressionResult
using RAR.Services;
using System;
using System.Collections.Generic;
using System.Drawing; // Still needed for UI elements within OperationLogic's scope for SetProcessingState
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms; // Still needed for MessageBox, UI control types

namespace RAR.UI
{
    /// <summary>
    /// Encapsulates the core compression/decompression operations and manages interaction with background services.
    /// </summary>
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
        private int _threadingCompletedCount = 0;
        private int _threadingTotalCount = 0;
        private long _totalOriginalSize = 0;
        private long _totalCompressedSize = 0;

        // Events to communicate status and completion back to MainForm
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

            _threadingStopwatch = Stopwatch.StartNew();
            OperationStarted?.Invoke(this, new OperationStartedEventArgs("Starting compression...", _threadingTotalCount));
            SetProcessingState(true);

            try
            {
                if (useMultithreading)
                {
                    foreach (string itemPath in _selectedFilesListBox.Items)
                    {
                        if (Directory.Exists(itemPath))
                        {
                            _threadingService.FolderCompression(_currentFolderCompressor as HuffmanFolderCompression, _currentFolderCompressor as ShannonFanoFolderCompression, itemPath, pauseTokenSource.Token, password);
                        }
                        else
                        {
                            _threadingService.FileCompression(_currentFileCompressor as HuffmanCompressor, _currentFileCompressor as ShannonFanoCompressor, itemPath, pauseTokenSource.Token, password);
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
                // Handled by OperationCompleted event from _operationLogic (via CancelBtn_Click)
            }
            catch (Exception ex)
            {
                OperationFailed?.Invoke(this, ex);
            }
            finally
            {
                _threadingStopwatch?.Stop();
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

            _threadingStopwatch = Stopwatch.StartNew();
            OperationStarted?.Invoke(this, new OperationStartedEventArgs("Starting decompression...", _threadingTotalCount));
            SetProcessingState(true);

            try
            {
                if (useMultithreading)
                {
                    foreach (string itemPath in _selectedFilesListBox.Items)
                    {
                        string outputPath = GetDecompressionOutputPath(itemPath);
                        if (Directory.Exists(itemPath))
                        {
                            _threadingService.FolderDecompression(_currentFolderCompressor as HuffmanFolderCompression, _currentFolderCompressor as ShannonFanoFolderCompression, itemPath, outputPath, pauseTokenSource.Token, password);
                        }
                        else
                        {
                            _threadingService.FileDecompression(_currentFileCompressor as HuffmanCompressor, _currentFileCompressor as ShannonFanoCompressor, itemPath, outputPath, pauseTokenSource.Token, password);
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
                // Handled by OperationCompleted event from _operationLogic (via CancelBtn_Click)
            }
            catch (Exception ex)
            {
                OperationFailed?.Invoke(this, ex);
            }
            finally
            {
                _threadingStopwatch?.Stop();
            }
        }

        public void CancelBtn_Click()
        {
            _cancellationTokenSource?.Cancel();
            _threadingService?.Cancel();
            OperationCompleted?.Invoke(this, new OperationCompletedEventArgs("❌ Operation cancelled by user", _threadingStopwatch.Elapsed, null));
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
            _threadingTotalCount = _archiveContentComboBox.Items.Count; // Total items in archive for progress

            _threadingStopwatch = Stopwatch.StartNew();
            OperationStarted?.Invoke(this, new OperationStartedEventArgs($"Starting extraction of {selectedItem}...", _threadingTotalCount));
            SetProcessingState(true);

            try
            {
                if (selectedItem == "All")
                {
                    List<string> filesToExtract = GetArchiveContents(folderPathForExtraction).Where(f => f != "All").ToList();
                    if (useMultithreading)
                    {
                        foreach (string itemPath in filesToExtract)
                        {
                            string fullPath = Path.Combine(folderPathForExtraction, itemPath);
                            string outputPath = GetDecompressionOutputPath(fullPath);
                            if (fullPath.EndsWith(".huff") || fullPath.EndsWith(".shf")) // Assuming these are files within the archive
                            {
                                _threadingService.FileDecompression(_currentFileCompressor as HuffmanCompressor, _currentFileCompressor as ShannonFanoCompressor, fullPath, outputPath, pauseTokenSource.Token, password);
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

                            // Password handling logic duplicated for non-multithreading case for clarity
                            string localPassword = password;
                            if (string.IsNullOrEmpty(localPassword) && (EncryptionHelper.IsFileEncrypted(fullPath))) // Simplified check
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
                            ProgressUpdated?.Invoke(this, new ProgressUpdatedEventArgs(processedItems, $"Extracting: {itemPath}", _threadingStopwatch.Elapsed, null));
                        }
                        OperationCompleted?.Invoke(this, new OperationCompletedEventArgs($"✅ Extraction completed! Processed {processedItems} item(s)", _threadingStopwatch.Elapsed, null));
                    }
                }
                else
                {
                    string filePathToExtract = Path.Combine(folderPathForExtraction, selectedItem);
                    if (useMultithreading)
                    {
                        string outputPath = GetDecompressionOutputPath(filePathToExtract);
                        if (filePathToExtract.EndsWith(".huff") || filePathToExtract.EndsWith(".shf"))
                        {
                            _threadingService.FileDecompression(_currentFileCompressor as HuffmanCompressor, _currentFileCompressor as ShannonFanoCompressor, filePathToExtract, outputPath, pauseTokenSource.Token, password);
                        }
                    }
                    else
                    {
                        _statusLabel.Text = $"Extracting: {selectedItem}...";
                        // Password handling logic duplicated for non-multithreading case for clarity
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

                        string outputPath = GetDecompressionOutputPath(filePathToExtract);
                        await Task.Run(() => _currentFileCompressor.Decompress(filePathToExtract, outputPath, _cancellationTokenSource.Token, localPassword));
                        OperationCompleted?.Invoke(this, new OperationCompletedEventArgs($"✅ Extraction completed: {selectedItem}", _threadingStopwatch.Elapsed, null));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                OperationCompleted?.Invoke(this, new OperationCompletedEventArgs("❌ Operation cancelled by user", _threadingStopwatch.Elapsed, null));
            }
            catch (Exception ex)
            {
                OperationFailed?.Invoke(this, ex);
            }
            finally
            {
                _threadingStopwatch?.Stop();
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
                                OperationCompleted?.Invoke(this, new OperationCompletedEventArgs("❌ Operation cancelled by user", _threadingStopwatch.Elapsed, null));
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
                    OperationCompleted?.Invoke(this, new OperationCompletedEventArgs("No items processed.", _threadingStopwatch.Elapsed, null));
                    return;
                }
                items = outputPaths.Keys.ToList(); // Update items to only include those for which output was selected
            }

            foreach (string itemPath in items)
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    OperationCompleted?.Invoke(this, new OperationCompletedEventArgs("❌ Operation cancelled by user", _threadingStopwatch.Elapsed, null));
                    break;
                }

                pauseTokenSource.Token.WaitIfPaused();

                try
                {
                    bool isFolder = Directory.Exists(itemPath);
                    string itemName = Path.GetFileName(itemPath);

                    if (isCompression)
                    {
                        string localPassword = password; // Use the provided password
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
                    else // Decompression
                    {
                        string outputPath = outputPaths[itemPath];
                        string localPassword = password; // Use the provided password, or prompt if necessary

                        bool isEncrypted = false;
                        if (isFolder)
                        {
                            string archiveInfoPath = Path.Combine(itemPath, "archive_info.txt");
                            if (File.Exists(archiveInfoPath))
                            {
                                try { isEncrypted = File.ReadAllText(archiveInfoPath).Contains("Encrypted: Yes"); } catch { /* ignored */ }
                            }
                            if (!isEncrypted) // Double check by inspecting compressed files if info file doesn't explicitly state
                            {
                                try
                                {
                                    string[] compressedFilesInFolder = Directory.GetFiles(itemPath, "*.*", SearchOption.AllDirectories)
                                                                        .Where(f => f.EndsWith(".huff") || f.EndsWith(".shf"))
                                                                        .ToArray();
                                    isEncrypted = compressedFilesInFolder.Any(file => EncryptionHelper.IsFileEncrypted(file));
                                }
                                catch { /* ignored */ }
                            }
                        }
                        else
                        {
                            try { isEncrypted = EncryptionHelper.IsFileEncrypted(itemPath); } catch { /* ignored */ }
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
                                    continue; // Skip this item
                                }
                                localPassword = passwordDialog.EnteredPassword;
                                if (string.IsNullOrWhiteSpace(localPassword))
                                {
                                    MessageBox.Show($"Password cannot be empty for {itemName}. Skipping this file.",
                                                    "Invalid Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    continue;
                                }
                                try // Validate password before proceeding
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
                    ProgressUpdated?.Invoke(this, new ProgressUpdatedEventArgs(processedItems, isCompression ? $"Compressing: {itemName}" : $"Decompressing: {itemName}", _threadingStopwatch.Elapsed, compressionRatio));
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
                    // Continue to next item in non-multithreaded mode if an error occurs
                }
            }

            if (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                OperationCompleted?.Invoke(this, new OperationCompletedEventArgs(
                    isCompression ? $"✅ Compression completed! Processed {processedItems} item(s)" : $"✅ Decompression completed! Processed {processedItems} item(s)",
                    _threadingStopwatch.Elapsed, _totalOriginalSize > 0 ? (double?)((_totalOriginalSize - _totalCompressedSize) * 100.0 / _totalOriginalSize) : null));
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

        // Event Handlers for ThreadingService callbacks (invoked on UI thread by MainForm)
        private void OnFileCompressed(RAR.Helper.CompressionResult result) // Use your provided CompressionResult
        {
            Interlocked.Increment(ref _threadingCompletedCount);
            Interlocked.Add(ref _totalOriginalSize, result.OriginalSize);
            Interlocked.Add(ref _totalCompressedSize, result.CompressedSize);
            // Use the CompressionRatioPercent property from your provided CompressionResult
            ProgressUpdated?.Invoke(this, new ProgressUpdatedEventArgs(_threadingCompletedCount, $"Compressing: {Path.GetFileName(result.CompressedFilePath)}", _threadingStopwatch.Elapsed, result.CompressionRatio));

            if (_threadingCompletedCount == _threadingTotalCount)
            {
                OperationCompleted?.Invoke(this, new OperationCompletedEventArgs("✅ Compression completed!", _threadingStopwatch.Elapsed, result.CompressionRatio));
            }
        }

        private void OnFolderCompressed(RAR.Helper.FolderCompressionResult result) // Use your provided FolderCompressionResult
        {
            Interlocked.Increment(ref _threadingCompletedCount);
            Interlocked.Add(ref _totalOriginalSize, result.TotalOriginalSize);
            Interlocked.Add(ref _totalCompressedSize, result.TotalCompressedSize);
            // Use the OverallCompressionRatio property from your provided FolderCompressionResult
            ProgressUpdated?.Invoke(this, new ProgressUpdatedEventArgs(_threadingCompletedCount, $"Compressing folder: {Path.GetFileName(result.OriginalFolderPath)}", _threadingStopwatch.Elapsed, result.OverallCompressionRatio));

            if (_threadingCompletedCount == _threadingTotalCount)
            {
                OperationCompleted?.Invoke(this, new OperationCompletedEventArgs("✅ Compression completed!", _threadingStopwatch.Elapsed, result.OverallCompressionRatio));
            }
        }

        private void OnFileDecompressed(string outputPath)
        {
            Interlocked.Increment(ref _threadingCompletedCount);
            ProgressUpdated?.Invoke(this, new ProgressUpdatedEventArgs(_threadingCompletedCount, $"Decompressing: {Path.GetFileName(outputPath)}", _threadingStopwatch.Elapsed, null));

            if (_threadingCompletedCount == _threadingTotalCount)
            {
                OperationCompleted?.Invoke(this, new OperationCompletedEventArgs("✅ Decompression completed!", _threadingStopwatch.Elapsed, null));
            }
        }

        private void OnFolderDecompressed(string outputPath)
        {
            Interlocked.Increment(ref _threadingCompletedCount);
            ProgressUpdated?.Invoke(this, new ProgressUpdatedEventArgs(_threadingCompletedCount, $"Decompressing folder: {Path.GetFileName(outputPath)}", _threadingStopwatch.Elapsed, null));

            if (_threadingCompletedCount == _threadingTotalCount)
            {
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

        /// <summary>
        /// Controls the enabled state of action buttons and related UI elements during processing.
        /// </summary>
        /// <param name="processing">True if an operation is in progress, false otherwise.</param>
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