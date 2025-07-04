﻿using RAR.Core.Interfaces;
using RAR.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace RAR.Core.Compression
{
    public class HuffmanCompressor : ICompressor
    {
        public CompressionResult Compress(string inputFilePath, CancellationToken token, PauseToken? pauseToken = null, string password = null)
        {
            return CompressMultiple(new string[] { inputFilePath }, inputFilePath + ".huff",token, pauseToken, password);
        }

        public CompressionResult CompressMultiple(string[] inputFilePaths, string outputPath, CancellationToken token, PauseToken? pauseToken = null, string password = null)
        {
            try
            {
                if (inputFilePaths == null || inputFilePaths.Length == 0)
                    throw new ArgumentException("No input files provided");

                foreach (string filePath in inputFilePaths)
                {
                    token.ThrowIfCancellationRequested();
                    pauseToken?.WaitIfPaused();
                    if (!File.Exists(filePath))
                        throw new FileNotFoundException($"Input file not found: {filePath}");
                }

                string commonBasePath = FindCommonBasePath(inputFilePaths);

                var fileMetadata = new List<FileMetadata>();
                var combinedData = new List<byte>();
                long totalOriginalSize = 0;

                foreach (string filePath in inputFilePaths)
                {
                    token.ThrowIfCancellationRequested();
                    pauseToken?.WaitIfPaused();
                    byte[] fileBytes = File.ReadAllBytes(filePath);

                    string relativePath = GetRelativePath(commonBasePath, filePath);

                    var metadata = new FileMetadata
                    {
                        RelativePath = relativePath, 
                        OriginalPath = filePath,
                        FileSize = fileBytes.Length,
                        StartOffset = combinedData.Count
                    };

                    fileMetadata.Add(metadata);
                    combinedData.AddRange(fileBytes);
                    totalOriginalSize += fileBytes.Length;
                }

                token.ThrowIfCancellationRequested();

                byte[] allData = combinedData.ToArray();
                bool isEncrypted = !string.IsNullOrWhiteSpace(password);
                if (allData.Length == 0)
                    return CreateEmptyArchiveResult(outputPath, fileMetadata, isEncrypted);

                if (allData.Length == 1)
                    return CreateSingleByteArchiveResult(outputPath, allData[0], fileMetadata, password, isEncrypted);
                
                token.ThrowIfCancellationRequested();
                var frequencies = CountFrequencies(allData);
                pauseToken?.WaitIfPaused();
                Node root = BuildHuffmanTree(frequencies);
                pauseToken?.WaitIfPaused();
                var codes = new Dictionary<byte, BitString>();
                GenerateCodes(root, new BitString(), codes);
                pauseToken?.WaitIfPaused();
                token.ThrowIfCancellationRequested();

                var encodedData = EncodeData(allData, codes);
                pauseToken?.WaitIfPaused();

                token.ThrowIfCancellationRequested();

                byte[] compressedBytes = CreateCompressedArchive(fileMetadata, codes, encodedData, allData.Length);
                pauseToken?.WaitIfPaused();

                if (isEncrypted)
                {
                    compressedBytes = EncryptionHelper.Encrypt(compressedBytes, password);
                }
                pauseToken?.WaitIfPaused();

                File.WriteAllBytes(outputPath, compressedBytes);

                token.ThrowIfCancellationRequested();

                return new CompressionResult
                {
                    CompressedFilePath = outputPath,
                    OriginalSize = totalOriginalSize * 8,
                    CompressedSize = compressedBytes.Length * 8,
                    IsEncrypted = isEncrypted
                };
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Multi-file compression failed: " + ex.Message, ex);
            }
        }

        public void Decompress(string compressedFilePath, string outputFilePath, CancellationToken token, string password = null, PauseToken? pauseToken = null)
        {
            string tempDir = Path.GetTempPath() + Guid.NewGuid().ToString();
            try
            {
                DecompressMultiple(compressedFilePath, tempDir, token, password, pauseToken);

                string[] files = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    File.Move(files[0], outputFilePath);
                }
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        public void DecompressMultiple(string compressedFilePath, string outputDirectory, CancellationToken token, string password = null, PauseToken ?pauseToken = null)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (!File.Exists(compressedFilePath))
                    throw new FileNotFoundException("Compressed file not found: " + compressedFilePath);

                token.ThrowIfCancellationRequested();

                if (!Directory.Exists(outputDirectory))
                    Directory.CreateDirectory(outputDirectory);

                token.ThrowIfCancellationRequested();

                byte[] fileBytes = File.ReadAllBytes(compressedFilePath);

                token.ThrowIfCancellationRequested();

                if (!string.IsNullOrWhiteSpace(password))
                {
                    try
                    {
                        fileBytes = EncryptionHelper.Decrypt(fileBytes, password);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Incorrect password or corrupted encrypted file: " + ex.Message, ex);
                    }
                }

                using (var reader = new BinaryReader(new MemoryStream(fileBytes)))
                {
                    token.ThrowIfCancellationRequested();

                    var fileMetadata = ReadFileMetadata(reader);

                    token.ThrowIfCancellationRequested();

                    int originalLength = reader.ReadInt32();

                    if (originalLength == 0)
                    {
                        token.ThrowIfCancellationRequested();

                        CreateEmptyFiles(fileMetadata, outputDirectory);
                        return;
                    }

                    if (originalLength == 1)
                    {
                        token.ThrowIfCancellationRequested();

                        byte singleByte = reader.ReadByte();
                        CreateSingleByteFiles(fileMetadata, outputDirectory, singleByte);
                        return;
                    }

                    token.ThrowIfCancellationRequested();

                    var codes = ReadCodesFromFile(reader);
                    int encodedBitCount = reader.ReadInt32();
                    int encodedByteCount = (encodedBitCount + 7) / 8;
                    byte[] encodedBytes = reader.ReadBytes(encodedByteCount);

                    token.ThrowIfCancellationRequested();

                    var decodedData = DecodeData(encodedBytes, codes, encodedBitCount, originalLength);

                    token.ThrowIfCancellationRequested();

                    ExtractFiles(decodedData, fileMetadata, outputDirectory);
                }
            }
            catch (OperationCanceledException)
            {
                
            }
            catch (Exception ex)
            {
                throw new Exception("Multi-file decompression failed: " + ex.Message, ex);
            }
        }

        private byte[] CreateCompressedArchive(List<FileMetadata> fileMetadata, Dictionary<byte, BitString> codes, BitString encodedData, int originalLength)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                WriteFileMetadata(writer, fileMetadata);

                writer.Write(originalLength);

                if (originalLength <= 1)
                    return ms.ToArray();

                writer.Write(codes.Count);
                foreach (var pair in codes)
                {
                    writer.Write(pair.Key);
                    writer.Write((byte)pair.Value.BitCount);

                    if (pair.Value.BitCount > 0)
                        writer.Write(pair.Value.Bytes);
                }

                writer.Write(encodedData.BitCount);
                writer.Write(encodedData.Bytes);

                return ms.ToArray();
            }
        }

        private void WriteFileMetadata(BinaryWriter writer, List<FileMetadata> fileMetadata)
        {
            writer.Write(fileMetadata.Count);

            foreach (var metadata in fileMetadata)
            {
                byte[] relativePathBytes = Encoding.UTF8.GetBytes(metadata.RelativePath);
                writer.Write(relativePathBytes.Length);
                writer.Write(relativePathBytes);

                byte[] originalPathBytes = Encoding.UTF8.GetBytes(metadata.OriginalPath ?? "");
                writer.Write(originalPathBytes.Length);
                writer.Write(originalPathBytes);

                writer.Write(metadata.FileSize);
                writer.Write(metadata.StartOffset);
            }
        }

        private List<FileMetadata> ReadFileMetadata(BinaryReader reader)
        {
            var fileMetadata = new List<FileMetadata>();
            int fileCount = reader.ReadInt32();

            for (int i = 0; i < fileCount; i++)
            {
                int relativePathLength = reader.ReadInt32();
                byte[] relativePathBytes = reader.ReadBytes(relativePathLength);
                string relativePath = Encoding.UTF8.GetString(relativePathBytes);

                int originalPathLength = reader.ReadInt32();
                byte[] originalPathBytes = reader.ReadBytes(originalPathLength);
                string originalPath = Encoding.UTF8.GetString(originalPathBytes);

                int fileSize = reader.ReadInt32();
                long startOffset = reader.ReadInt64();

                fileMetadata.Add(new FileMetadata
                {
                    RelativePath = relativePath,
                    OriginalPath = originalPath,
                    FileSize = fileSize,
                    StartOffset = startOffset
                });
            }

            return fileMetadata;
        }

        private void ExtractFiles(byte[] decodedData, List<FileMetadata> fileMetadata, string outputDirectory)
        {
            foreach (var metadata in fileMetadata)
            {
                byte[] fileData = new byte[metadata.FileSize];
                Array.Copy(decodedData, metadata.StartOffset, fileData, 0, metadata.FileSize);

                string outputPath = Path.Combine(outputDirectory, metadata.RelativePath);
                string outputDir = Path.GetDirectoryName(outputPath);

                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);

                File.WriteAllBytes(outputPath, fileData);
            }
        }

        private void CreateEmptyFiles(List<FileMetadata> fileMetadata, string outputDirectory)
        {
            foreach (var metadata in fileMetadata)
            {
                string outputPath = Path.Combine(outputDirectory, metadata.RelativePath);
                string outputDir = Path.GetDirectoryName(outputPath);

                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);

                File.WriteAllBytes(outputPath, new byte[0]);
            }
        }

        private void CreateSingleByteFiles(List<FileMetadata> fileMetadata, string outputDirectory, byte singleByte)
        {
            foreach (var metadata in fileMetadata)
            {
                if (metadata.FileSize > 0)
                {
                    string outputPath = Path.Combine(outputDirectory, metadata.RelativePath);
                    string outputDir = Path.GetDirectoryName(outputPath);

                    if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                        Directory.CreateDirectory(outputDir);

                    File.WriteAllBytes(outputPath, new byte[] { singleByte });
                }
            }
        }

        private CompressionResult CreateEmptyArchiveResult(string outputPath, List<FileMetadata> fileMetadata, bool isEncrypted)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                WriteFileMetadata(writer, fileMetadata);
                writer.Write(0); 

                byte[] data = ms.ToArray();
                File.WriteAllBytes(outputPath, data);

                return new CompressionResult
                {
                    CompressedFilePath = outputPath,
                    OriginalSize = 0,
                    CompressedSize = data.Length * 8,
                    IsEncrypted = isEncrypted
                };
            }
        }

        private CompressionResult CreateSingleByteArchiveResult(string outputPath, byte singleByte, List<FileMetadata> fileMetadata, string password, bool isEncrypted)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                WriteFileMetadata(writer, fileMetadata);
                writer.Write(1); 
                writer.Write(singleByte);

                byte[] data = ms.ToArray();

                if (isEncrypted && !string.IsNullOrWhiteSpace(password))
                {
                    data = EncryptionHelper.Encrypt(data, password);
                }

                File.WriteAllBytes(outputPath, data);

                return new CompressionResult
                {
                    CompressedFilePath = outputPath,
                    OriginalSize = 8,
                    CompressedSize = data.Length * 8,
                    IsEncrypted = isEncrypted
                };
            }
        }

        private class FileMetadata
        {
            public string RelativePath { get; set; } 
            public string OriginalPath { get; set; } 
            public int FileSize { get; set; }
            public long StartOffset { get; set; }
        }

        private string FindCommonBasePath(string[] filePaths)
        {
            if (filePaths.Length == 0) return "";
            if (filePaths.Length == 1) return Path.GetDirectoryName(filePaths[0]) ?? "";

            string[] parts = filePaths[0].Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            for (int i = 1; i < filePaths.Length; i++)
            {
                string[] currentParts = filePaths[i].Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                int commonLength = 0;

                for (int j = 0; j < Math.Min(parts.Length - 1, currentParts.Length - 1); j++) 
                {
                    if (string.Equals(parts[j], currentParts[j], StringComparison.OrdinalIgnoreCase))
                        commonLength++;
                    else
                        break;
                }

                Array.Resize(ref parts, commonLength);
            }

            return string.Join(Path.DirectorySeparatorChar.ToString(), parts);
        }

        private string GetRelativePath(string basePath, string fullPath)
        {
            if (string.IsNullOrEmpty(basePath))
                return Path.GetFileName(fullPath);

            try
            {
                Uri baseUri = new Uri(basePath + Path.DirectorySeparatorChar);
                Uri fullUri = new Uri(fullPath);
                return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString().Replace('/', Path.DirectorySeparatorChar));
            }
            catch
            {
                return fullPath.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
        }

        public class Node : IComparable<Node>
        {
            public byte? Symbol;
            public long Frequency;
            public Node Left, Right;
            public int Id;

            private static int _nextId = 0;

            public Node()
            {
                Id = _nextId++;
            }

            public int CompareTo(Node other)
            {
                if (other == null) return 1;
                int result = Frequency.CompareTo(other.Frequency);
                if (result == 0)
                    result = Id.CompareTo(other.Id);
                return result;
            }
        }

        public class BitString : IEquatable<BitString>
        {
            private readonly byte[] _bytes;
            private readonly int _bitCount;
            private readonly int _hashCode;

            public int BitCount { get { return _bitCount; } }
            public byte[] Bytes { get { return _bytes; } }

            public BitString()
            {
                _bytes = new byte[0];
                _bitCount = 0;
                _hashCode = 0;
            }

            public BitString(byte[] bytes, int bitCount)
            {
                _bytes = bytes ?? new byte[0];
                _bitCount = bitCount;
                _hashCode = ComputeHashCode();
            }

            public BitString Append(bool bit)
            {
                int newBitCount = _bitCount + 1;
                int newByteCount = (newBitCount + 7) / 8;
                byte[] newBytes = new byte[newByteCount];

                if (_bytes.Length > 0)
                    Array.Copy(_bytes, newBytes, _bytes.Length);

                if (bit)
                {
                    int byteIndex = _bitCount / 8;
                    int bitIndex = 7 - (_bitCount % 8);
                    if (byteIndex < newBytes.Length)
                        newBytes[byteIndex] |= (byte)(1 << bitIndex);
                }

                return new BitString(newBytes, newBitCount);
            }

            private int ComputeHashCode()
            {
                int hash = _bitCount;
                for (int i = 0; i < _bytes.Length; i++)
                    hash = hash * 31 + _bytes[i];
                return hash;
            }

            public override int GetHashCode()
            {
                return _hashCode;
            }

            public bool Equals(BitString other)
            {
                if (other == null || _bitCount != other._bitCount) return false;

                int relevantByteCount = (_bitCount + 7) / 8;
                for (int i = 0; i < relevantByteCount; i++)
                {
                    if (i < relevantByteCount - 1)
                    {
                        if (_bytes[i] != other._bytes[i]) return false;
                    }
                    else
                    {
                        int remainingBits = _bitCount % 8;
                        if (remainingBits == 0)
                        {
                            if (_bytes[i] != other._bytes[i]) return false;
                        }
                        else
                        {
                            byte mask = (byte)(0xFF << (8 - remainingBits));
                            if ((_bytes[i] & mask) != (other._bytes[i] & mask)) return false;
                        }
                    }
                }
                return true;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as BitString);
            }

            public override string ToString()
            {
                var sb = new StringBuilder();
                for (int i = 0; i < _bitCount; i++)
                {
                    int byteIndex = i / 8;
                    int bitIndex = 7 - (i % 8);
                    bool bit = (byteIndex < _bytes.Length) && (_bytes[byteIndex] & (1 << bitIndex)) != 0;
                    sb.Append(bit ? '1' : '0');
                }
                return sb.ToString();
            }
        }

        private Dictionary<byte, long> CountFrequencies(byte[] data)
        {
            var frequencies = new Dictionary<byte, long>();
            foreach (byte b in data)
            {
                if (frequencies.ContainsKey(b))
                    frequencies[b]++;
                else
                    frequencies[b] = 1;
            }
            return frequencies;
        }

        private Node BuildHuffmanTree(Dictionary<byte, long> frequencies)
        {
            var priorityQueue = new SortedSet<Node>();

            foreach (var pair in frequencies)
            {
                priorityQueue.Add(new Node
                {
                    Symbol = pair.Key,
                    Frequency = pair.Value
                });
            }

            if (priorityQueue.Count == 1)
            {
                var singleNode = priorityQueue.Min;
                var root = new Node
                {
                    Frequency = singleNode.Frequency,
                    Left = singleNode,
                    Right = null
                };
                return root;
            }

            while (priorityQueue.Count > 1)
            {
                Node left = priorityQueue.Min;
                priorityQueue.Remove(left);

                Node right = priorityQueue.Min;
                priorityQueue.Remove(right);

                var parent = new Node
                {
                    Frequency = left.Frequency + right.Frequency,
                    Left = left,
                    Right = right
                };

                priorityQueue.Add(parent);
            }

            return priorityQueue.Min;
        }

        private void GenerateCodes(Node node, BitString code, Dictionary<byte, BitString> codes)
        {
            if (node == null) return;

            if (node.Symbol.HasValue)
            {
                if (code.BitCount == 0)
                    code = code.Append(false);
                codes[node.Symbol.Value] = code;
                return;
            }

            if (node.Left != null)
                GenerateCodes(node.Left, code.Append(false), codes);
            if (node.Right != null)
                GenerateCodes(node.Right, code.Append(true), codes);
        }

        private BitString EncodeData(byte[] data, Dictionary<byte, BitString> codes)
        {
            var result = new List<byte>();
            int totalBits = 0;
            byte currentByte = 0;
            int currentBitPos = 0;

            foreach (byte b in data)
            {
                var code = codes[b];
                totalBits += code.BitCount;

                for (int i = 0; i < code.BitCount; i++)
                {
                    int byteIndex = i / 8;
                    int bitIndex = 7 - (i % 8);
                    bool bit = (byteIndex < code.Bytes.Length) && (code.Bytes[byteIndex] & (1 << bitIndex)) != 0;

                    if (bit)
                        currentByte |= (byte)(1 << (7 - currentBitPos));

                    currentBitPos++;
                    if (currentBitPos == 8)
                    {
                        result.Add(currentByte);
                        currentByte = 0;
                        currentBitPos = 0;
                    }
                }
            }

            if (currentBitPos > 0)
                result.Add(currentByte);

            return new BitString(result.ToArray(), totalBits);
        }

        private Dictionary<BitString, byte> ReadCodesFromFile(BinaryReader reader)
        {
            var codes = new Dictionary<BitString, byte>();
            int codeCount = reader.ReadInt32();

            for (int i = 0; i < codeCount; i++)
            {
                byte symbol = reader.ReadByte();
                byte codeLength = reader.ReadByte();

                if (codeLength == 0)
                {
                    codes[new BitString()] = symbol;
                    continue;
                }

                int codeByteCount = (codeLength + 7) / 8;
                var codeBytes = reader.ReadBytes(codeByteCount);
                var bitString = new BitString(codeBytes, codeLength);
                codes[bitString] = symbol;
            }

            return codes;
        }

        private byte[] DecodeData(byte[] encodedData, Dictionary<BitString, byte> codes, int bitCount, int originalLength)
        {
            var result = new List<byte>(originalLength);
            var currentCode = new BitString();

            for (int bitIndex = 0; bitIndex < bitCount && result.Count < originalLength; bitIndex++)
            {
                int byteIndex = bitIndex / 8;
                int bitPos = 7 - (bitIndex % 8);
                bool bit = (byteIndex < encodedData.Length) && (encodedData[byteIndex] & (1 << bitPos)) != 0;

                currentCode = currentCode.Append(bit);

                if (codes.ContainsKey(currentCode))
                {
                    result.Add(codes[currentCode]);
                    currentCode = new BitString();
                }
            }

            return result.ToArray();
        }
    }
}