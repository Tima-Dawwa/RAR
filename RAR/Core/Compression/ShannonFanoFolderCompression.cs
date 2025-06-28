using RAR.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace RAR.Core.Compression
{
    public class ShannonFanoFolderCompression
    {
        private ShannonFanoCompressor _fileCompressor;

        public ShannonFanoFolderCompression()
        {
            _fileCompressor = new ShannonFanoCompressor();
        }

        public FolderCompressionResult CompressFolder(string folderPath, CancellationToken token, PauseToken pauseToken = null, string password = null)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (!Directory.Exists(folderPath))
                    throw new DirectoryNotFoundException("Folder not found: " + folderPath);

                var result = new FolderCompressionResult
                {
                    OriginalFolderPath = folderPath,
                    CompressedFolderPath = folderPath + ".shf_archive",
                    FileResults = new List<CompressionResult>(),
                    IsEncrypted = !string.IsNullOrEmpty(password)
                };

                // Create compressed folder
                if (Directory.Exists(result.CompressedFolderPath))
                {
                    Directory.Delete(result.CompressedFolderPath, true);
                }
                Directory.CreateDirectory(result.CompressedFolderPath);
                
                token.ThrowIfCancellationRequested();

                // Get all files in folder and subfolders
                string[] files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);

                if (files.Length == 0)
                {
                    // Handle empty folder
                    CreateArchiveInfo(result, password);
                    return result;
                }

                foreach (string file in files)
                {
                    token.ThrowIfCancellationRequested();
                    pauseToken?.WaitIfPaused();
                    
                    try
                    {
                        // Get relative path to maintain folder structure
                        string relativePath = GetRelativePath(folderPath, file);
                        string compressedFilePath = Path.Combine(result.CompressedFolderPath, relativePath + ".shf");

                        // Create directory structure if needed
                        string compressedDir = Path.GetDirectoryName(compressedFilePath);
                        if (!Directory.Exists(compressedDir))
                            Directory.CreateDirectory(compressedDir);

                        // Compress the file
                        CompressionResult fileResult = !string.IsNullOrEmpty(password)
                            ? _fileCompressor.Compress(file, token, pauseToken, password)
                            : _fileCompressor.Compress(file, token, pauseToken);

                        token.ThrowIfCancellationRequested();

                        if (fileResult != null && File.Exists(fileResult.CompressedFilePath))
                        {
                            // Move compressed file to the correct location
                            File.Move(fileResult.CompressedFilePath, compressedFilePath);
                            fileResult.CompressedFilePath = compressedFilePath;
                            
                            result.FileResults.Add(fileResult);
                            result.TotalOriginalSize += fileResult.OriginalSize;
                            result.TotalCompressedSize += fileResult.CompressedSize;
                        }
                        else
                        {
                            Console.WriteLine($"Warning: Failed to compress file {file} - result is null or file not found");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Clean up on cancellation
                        if (Directory.Exists(result.CompressedFolderPath))
                        {
                            try
                            {
                                Directory.Delete(result.CompressedFolderPath, true);
                            }
                            catch { }
                        }
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Failed to compress file {file}: {ex.Message}");
                        // Continue with other files
                    }
                }

                result.FileCount = result.FileResults.Count;
                token.ThrowIfCancellationRequested();
                
                // Create archive info file
                CreateArchiveInfo(result, password);
                
                return result;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Folder compression failed: " + ex.Message, ex);
            }
        }

        public void DecompressFolder(string compressedFolderPath, string outputFolderPath, CancellationToken token, string password = null)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (!Directory.Exists(compressedFolderPath))
                    throw new DirectoryNotFoundException("Compressed folder not found: " + compressedFolderPath);

                token.ThrowIfCancellationRequested();

                // Create output directory if it doesn't exist
                if (Directory.Exists(outputFolderPath))
                {
                    Directory.Delete(outputFolderPath, true);
                }
                Directory.CreateDirectory(outputFolderPath);

                // Check if archive is encrypted
                string archiveInfoPath = Path.Combine(compressedFolderPath, "archive_info.txt");
                bool wasEncrypted = false;

                token.ThrowIfCancellationRequested();

                if (File.Exists(archiveInfoPath))
                {
                    try
                    {
                        string infoContent = File.ReadAllText(archiveInfoPath);
                        wasEncrypted = infoContent.Contains("Encrypted: Yes");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Could not read archive info: {ex.Message}");
                        wasEncrypted = false;
                    }
                }

                token.ThrowIfCancellationRequested();

                if (wasEncrypted && string.IsNullOrEmpty(password))
                    throw new UnauthorizedAccessException("This archive is encrypted. Please provide a password.");

                // Get all compressed files
                string[] compressedFiles = Directory.GetFiles(compressedFolderPath, "*.shf", SearchOption.AllDirectories);

                if (compressedFiles.Length == 0)
                {
                    Console.WriteLine("No compressed files found in archive");
                    return;
                }

                token.ThrowIfCancellationRequested();

                foreach (string compressedFile in compressedFiles)
                {
                    token.ThrowIfCancellationRequested();
                    
                    try
                    {
                        // Get relative path and create output path
                        string relativePath = GetRelativePath(compressedFolderPath, compressedFile);
                        string outputFile = Path.Combine(outputFolderPath, relativePath.Replace(".shf", ""));

                        // Create output directory if needed
                        string outputDir = Path.GetDirectoryName(outputFile);
                        if (!Directory.Exists(outputDir))
                            Directory.CreateDirectory(outputDir);

                        // Decompress the file
                        if (wasEncrypted && !string.IsNullOrEmpty(password))
                        {
                            _fileCompressor.Decompress(compressedFile, outputFile, token, password);
                        }
                        else
                        {
                            _fileCompressor.Decompress(compressedFile, outputFile, token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Clean up on cancellation
                        if (Directory.Exists(outputFolderPath))
                        {
                            try
                            {
                                Directory.Delete(outputFolderPath, true);
                            }
                            catch { }
                        }
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Failed to decompress file {compressedFile}: {ex.Message}");
                        // Continue with other files
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled
            }
            catch (Exception ex)
            {
                throw new Exception("Folder decompression failed: " + ex.Message, ex);
            }
        }

        private string GetRelativePath(string basePath, string fullPath)
        {
            try
            {
                Uri baseUri = new Uri(basePath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
                Uri fullUri = new Uri(fullPath);
                return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString().Replace('/', Path.DirectorySeparatorChar));
            }
            catch
            {
                // Fallback if URI creation fails
                return fullPath.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
        }

        private void CreateArchiveInfo(FolderCompressionResult result, string password)
        {
            try
            {
                string infoPath = Path.Combine(result.CompressedFolderPath, "archive_info.txt");
                using (var writer = new StreamWriter(infoPath, false, Encoding.UTF8))
                {
                    writer.WriteLine("Shannon-Fano Archive Information");
                    writer.WriteLine("==============================");
                    writer.WriteLine("Original Folder: " + result.OriginalFolderPath);
                    writer.WriteLine("Compressed: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    writer.WriteLine("Files Compressed: " + result.FileResults.Count);
                    writer.WriteLine("Encrypted: " + (string.IsNullOrEmpty(password) ? "No" : "Yes"));
                    writer.WriteLine("Total Original Size: " + FormatBytes(result.TotalOriginalSize));
                    writer.WriteLine("Total Compressed Size: " + FormatBytes(result.TotalCompressedSize));
                    writer.WriteLine("Overall Compression Ratio: " + result.OverallCompressionRatioPercent);
                    writer.WriteLine();
                    writer.WriteLine("File Details:");
                    writer.WriteLine("=============");

                    foreach (var fileResult in result.FileResults)
                    {
                        writer.WriteLine("File: " + Path.GetFileName(fileResult.CompressedFilePath).Replace(".shf", ""));
                        writer.WriteLine("  Original: " + FormatBytes(fileResult.OriginalSize));
                        writer.WriteLine("  Compressed: " + FormatBytes(fileResult.CompressedSize));
                        writer.WriteLine("  Ratio: " + fileResult.CompressionRatioPercent);
                        writer.WriteLine("  Encrypted: " + (fileResult.IsEncrypted ? "Yes" : "No"));
                        writer.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to create archive info: {ex.Message}");
            }
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return string.Format("{0:0.##} {1}", len, sizes[order]);
        }
    }
}
