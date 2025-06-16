using RAR.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RAR.Core.Compression
{
    public class HuffmanFolderCompression
    {
        private HuffmanCompressor _fileCompressor;

        public HuffmanFolderCompression()
        {
            _fileCompressor = new HuffmanCompressor();
        }

        public FolderCompressionResult CompressFolder(string folderPath)
        {
            try
            {
                if (!Directory.Exists(folderPath))
                    throw new DirectoryNotFoundException("Folder not found: " + folderPath);

                var result = new FolderCompressionResult
                {
                    OriginalFolderPath = folderPath,
                    CompressedFolderPath = folderPath + ".huff_archive",
                    FileResults = new List<CompressionResult>()
                };

                // Create compressed folder
                Directory.CreateDirectory(result.CompressedFolderPath);

                // Get all files in folder and subfolders
                string[] files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);

                foreach (string file in files)
                {
                    try
                    {
                        // Get relative path to maintain folder structure
                        string relativePath = GetRelativePath(folderPath, file);
                        string compressedFilePath = Path.Combine(result.CompressedFolderPath, relativePath + ".huff");

                        // Ensure directory exists
                        string compressedDir = Path.GetDirectoryName(compressedFilePath);
                        if (!Directory.Exists(compressedDir))
                            Directory.CreateDirectory(compressedDir);

                        // Compress individual file
                        var fileResult = _fileCompressor.Compress(file);

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
                    catch (Exception ex)
                    {
                        Console.WriteLine("Warning: Failed to compress file " + file + ": " + ex.Message);
                    }
                }

                // Create archive info file
                CreateArchiveInfo(result);

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Folder compression failed: " + ex.Message, ex);
            }
        }

        public void DecompressFolder(string compressedFolderPath, string outputFolderPath)
        {
            try
            {
                if (!Directory.Exists(compressedFolderPath))
                    throw new DirectoryNotFoundException("Compressed folder not found: " + compressedFolderPath);

                // Create output folder
                if (!Directory.Exists(outputFolderPath))
                    Directory.CreateDirectory(outputFolderPath);

                // Get all .huff files
                string[] compressedFiles = Directory.GetFiles(compressedFolderPath, "*.huff", SearchOption.AllDirectories);

                foreach (string compressedFile in compressedFiles)
                {
                    try
                    {
                        // Calculate output path
                        string relativePath = GetRelativePath(compressedFolderPath, compressedFile);
                        string outputFile = Path.Combine(outputFolderPath, relativePath.Replace(".huff", ""));

                        // Ensure directory exists
                        string outputDir = Path.GetDirectoryName(outputFile);
                        if (!Directory.Exists(outputDir))
                            Directory.CreateDirectory(outputDir);

                        // Decompress file
                        _fileCompressor.Decompress(compressedFile, outputFile);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Warning: Failed to decompress file " + compressedFile + ": " + ex.Message);
                    }
                }
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

        private void CreateArchiveInfo(FolderCompressionResult result)
        {
            string infoPath = Path.Combine(result.CompressedFolderPath, "archive_info.txt");
            using (var writer = new StreamWriter(infoPath))
            {
                writer.WriteLine("Huffman Archive Information");
                writer.WriteLine("===========================");
                writer.WriteLine("Original Folder: " + result.OriginalFolderPath);
                writer.WriteLine("Compressed: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                writer.WriteLine("Files Compressed: " + result.FileResults.Count);
                writer.WriteLine("Total Original Size: " + FormatBytes(result.TotalOriginalSize));
                writer.WriteLine("Total Compressed Size: " + FormatBytes(result.TotalCompressedSize));
                writer.WriteLine("Overall Compression Ratio: " + string.Format("{0:P2}", result.OverallCompressionRatio / 100));
                writer.WriteLine();
                writer.WriteLine("File Details:");
                writer.WriteLine("=============");

                foreach (var fileResult in result.FileResults)
                {
                    writer.WriteLine("File: " + Path.GetFileName(fileResult.CompressedFilePath).Replace(".huff", ""));
                    writer.WriteLine("  Original: " + FormatBytes(fileResult.OriginalSize));
                    writer.WriteLine("  Compressed: " + FormatBytes(fileResult.CompressedSize));
                    writer.WriteLine("  Ratio: " + fileResult.CompressionRatioPercent);
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

    public class FolderCompressionResult
    {
        public string OriginalFolderPath { get; set; }
        public string CompressedFolderPath { get; set; }
        public List<CompressionResult> FileResults { get; set; }
        public long TotalOriginalSize { get; set; }
        public long TotalCompressedSize { get; set; }

        public double OverallCompressionRatio
        {
            get
            {
                return TotalOriginalSize > 0 ? (double)(TotalOriginalSize - TotalCompressedSize) / TotalOriginalSize * 100 : 0;
            }
        }

        public string OverallCompressionRatioPercent
        {
            get
            {
                return string.Format("{0:P2}", OverallCompressionRatio / 100);
            }
        }
    }
}