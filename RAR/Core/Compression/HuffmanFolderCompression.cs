using RAR.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace RAR.Core.Compression
{
    public class HuffmanFolderCompression
    {
        private HuffmanCompressor _fileCompressor;

        public HuffmanFolderCompression()
        {
            _fileCompressor = new HuffmanCompressor();
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
                    CompressedFolderPath = folderPath + ".huff_archive",
                    FileResults = new List<CompressionResult>(),
                    IsEncrypted = !string.IsNullOrEmpty(password)
                };

                // Create compressed folder
                Directory.CreateDirectory(result.CompressedFolderPath);

                token.ThrowIfCancellationRequested();

                // Get all files in folder and subfolders
                string[] files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);

                foreach (string file in files)
                {
                    token.ThrowIfCancellationRequested();
                    pauseToken?.WaitIfPaused();
                    try
                    {
                        // Get relative path to maintain folder structure
                        string relativePath = GetRelativePath(folderPath, file);
                        string compressedFilePath = Path.Combine(result.CompressedFolderPath, relativePath + ".huff");

                        // Ensure directory exists
                        string compressedDir = Path.GetDirectoryName(compressedFilePath);
                        if (!Directory.Exists(compressedDir))
                            Directory.CreateDirectory(compressedDir);

                        // Compress individual file with password if provided
                        CompressionResult fileResult;
                        if (!string.IsNullOrEmpty(password))
                        {
                            fileResult = _fileCompressor.Compress(file,token, pauseToken , password);
                        }
                        else
                        {
                            fileResult = _fileCompressor.Compress(file, token , pauseToken);
                        }

                        token.ThrowIfCancellationRequested();

                        // Move compressed file to archive folder
                        if (File.Exists(fileResult.CompressedFilePath))
                        {
                            File.Move(fileResult.CompressedFilePath, compressedFilePath);
                            fileResult.CompressedFilePath = compressedFilePath;
                        }

                        result.FileResults.Add(fileResult);
                        result.TotalOriginalSize += fileResult.OriginalSize;
                        result.TotalCompressedSize += fileResult.CompressedSize;
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Warning: Failed to compress file " + file + ": " + ex.Message);
                    }
                }

                // Update file count
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

                // Create output folder
                if (!Directory.Exists(outputFolderPath))
                    Directory.CreateDirectory(outputFolderPath);

                // Check if archive info exists to determine if it was encrypted
                string archiveInfoPath = Path.Combine(compressedFolderPath, "archive_info.txt");
                bool wasEncrypted = false;

                token.ThrowIfCancellationRequested();

                if (File.Exists(archiveInfoPath))
                {
                    string infoContent = File.ReadAllText(archiveInfoPath);
                    wasEncrypted = infoContent.Contains("Encrypted: Yes");
                }

                token.ThrowIfCancellationRequested();

                // If archive was encrypted but no password provided, throw exception
                if (wasEncrypted && string.IsNullOrEmpty(password))
                {
                    throw new UnauthorizedAccessException("This archive is encrypted. Please provide a password.");
                }

                // Get all .huff files
                string[] compressedFiles = Directory.GetFiles(compressedFolderPath, "*.huff", SearchOption.AllDirectories);

                token.ThrowIfCancellationRequested();

                foreach (string compressedFile in compressedFiles)
                {
                    token.ThrowIfCancellationRequested();

                    try
                    {
                        token.ThrowIfCancellationRequested();
                        // Calculate output path
                        string relativePath = GetRelativePath(compressedFolderPath, compressedFile);
                        string outputFile = Path.Combine(outputFolderPath, relativePath.Replace(".huff", ""));

                        // Ensure directory exists
                        string outputDir = Path.GetDirectoryName(outputFile);
                        if (!Directory.Exists(outputDir))
                            Directory.CreateDirectory(outputDir);

                        // Decompress file with password if it was encrypted
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
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Warning: Failed to decompress file " + compressedFile + ": " + ex.Message);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                
            }
            catch (Exception ex)
            {
                throw new Exception("Folder decompression failed: " + ex.Message, ex);
            }
        }

        private string GetRelativePath(string basePath, string fullPath)
        {
            Uri baseUri = new Uri(basePath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
            Uri fullUri = new Uri(fullPath);
            return baseUri.MakeRelativeUri(fullUri).ToString().Replace('/', Path.DirectorySeparatorChar);
        }

        private void CreateArchiveInfo(FolderCompressionResult result, string password)
        {
            string infoPath = Path.Combine(result.CompressedFolderPath, "archive_info.txt");
            using (var writer = new StreamWriter(infoPath))
            {
                writer.WriteLine("Huffman Archive Information");
                writer.WriteLine("===========================");
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
                    writer.WriteLine("File: " + Path.GetFileName(fileResult.CompressedFilePath).Replace(".huff", ""));
                    writer.WriteLine("  Original: " + FormatBytes(fileResult.OriginalSize));
                    writer.WriteLine("  Compressed: " + FormatBytes(fileResult.CompressedSize));
                    writer.WriteLine("  Ratio: " + fileResult.CompressionRatioPercent);
                    writer.WriteLine("  Encrypted: " + (fileResult.IsEncrypted ? "Yes" : "No"));
                    writer.WriteLine();
                }
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