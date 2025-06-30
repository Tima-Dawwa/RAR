using RAR.Core.Interfaces;
using RAR.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace RAR.Core.Compression
{
    public class ShannonFanoCompressor : ICompressor
    {
        public CompressionResult Compress(string inputFilePath, CancellationToken token, PauseToken? pauseToken = null, string password = null)
        {
            return CompressMultiple(new[] { inputFilePath }, inputFilePath + ".shf", token, pauseToken, password);
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
                    throw new FileNotFoundException($"File not found: {filePath}");
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
                var sorted = frequencies.OrderByDescending(kvp => kvp.Value).ToList();
                var codes = new Dictionary<byte, string>();
                BuildShannonFanoCodes(sorted, codes, "");

                token.ThrowIfCancellationRequested();

                var encodedBits = EncodeData(allData, codes, out int bitCount);

                token.ThrowIfCancellationRequested();

                byte[] compressedBytes = CreateCompressedArchive(fileMetadata, codes, encodedBits, bitCount, allData.Length);

                if (isEncrypted)
                {
                    compressedBytes = EncryptionHelper.Encrypt(compressedBytes, password);
                }

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

        public void Decompress(string compressedFilePath, string outputFilePath, CancellationToken token, string password = null, PauseToken ?pauseToken = null)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                DecompressMultiple(compressedFilePath, tempDir, token, password);
                string[] files = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
                if (files.Length > 0)
                    File.Move(files[0], outputFilePath);
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
                throw new FileNotFoundException("Compressed file not found.");

                token.ThrowIfCancellationRequested();

                if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

                token.ThrowIfCancellationRequested();

                byte[] fileBytes = File.ReadAllBytes(compressedFilePath);

                token.ThrowIfCancellationRequested();

                if (!string.IsNullOrWhiteSpace(password))
                fileBytes = EncryptionHelper.Decrypt(fileBytes, password);

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

                        byte b = reader.ReadByte();
                        CreateSingleByteFiles(fileMetadata, outputDirectory, b);
                        return;
                    }

                    token.ThrowIfCancellationRequested();

                    int codeCount = reader.ReadInt32();
                    var codes = new Dictionary<string, byte>();

                    token.ThrowIfCancellationRequested();

                    for (int i = 0; i < codeCount; i++)
                    {
                        token.ThrowIfCancellationRequested();

                        byte symbol = reader.ReadByte();
                        int codeLength = reader.ReadByte();
                        byte[] codeBytes = reader.ReadBytes(codeLength);
                        string code = Encoding.ASCII.GetString(codeBytes);
                        codes[code] = symbol;
                    }

                    token.ThrowIfCancellationRequested();

                    int bitCount = reader.ReadInt32();
                    int byteCount = (bitCount + 7) / 8;
                    byte[] encodedBytes = reader.ReadBytes(byteCount);

                    token.ThrowIfCancellationRequested();

                    byte[] decodedData = DecodeData(encodedBytes, codes, bitCount, originalLength);

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

        private Dictionary<byte, long> CountFrequencies(byte[] data)
        {
            var frequencies = new Dictionary<byte, long>();
            foreach (byte b in data)
            {
                if (!frequencies.ContainsKey(b))
                    frequencies[b] = 0;
                frequencies[b]++;
            }
            return frequencies;
        }

        private void BuildShannonFanoCodes(List<KeyValuePair<byte, long>> symbols, Dictionary<byte, string> codes, string prefix)
        {
            if (symbols.Count == 1)
            {
                codes[symbols[0].Key] = string.IsNullOrEmpty(prefix) ? "0" : prefix;
                return;
            }

            long total = symbols.Sum(kvp => kvp.Value);
            long half = total / 2;
            long runningTotal = 0;
            int splitIndex = 0;

            for (int i = 0; i < symbols.Count; i++)
            {
                runningTotal += symbols[i].Value;
                if (runningTotal >= half)
                {
                    splitIndex = i + 1;
                    break;
                }
            }

            var left = symbols.Take(splitIndex).ToList();
            var right = symbols.Skip(splitIndex).ToList();

            BuildShannonFanoCodes(left, codes, prefix + "0");
            BuildShannonFanoCodes(right, codes, prefix + "1");
        }

        private byte[] EncodeData(byte[] data, Dictionary<byte, string> codes, out int bitCount)
        {
            var bits = new List<bool>();
            foreach (var b in data)
            {
                string code = codes[b];
                bits.AddRange(code.Select(c => c == '1'));
            }
            bitCount = bits.Count;
            int byteCount = (bitCount + 7) / 8;
            byte[] result = new byte[byteCount];
            for (int i = 0; i < bitCount; i++)
            {
                if (bits[i])
                    result[i / 8] |= (byte)(1 << (7 - (i % 8)));
            }
            return result;
        }

        private byte[] DecodeData(byte[] encodedData, Dictionary<string, byte> codes, int bitCount, int originalLength)
        {
            var result = new List<byte>(originalLength);
            var buffer = new StringBuilder();

            for (int i = 0; i < bitCount; i++)
            {
                int byteIndex = i / 8;
                int bitIndex = 7 - (i % 8);
                bool bit = (encodedData[byteIndex] & (1 << bitIndex)) != 0;
                buffer.Append(bit ? '1' : '0');

                if (codes.TryGetValue(buffer.ToString(), out byte symbol))
                {
                    result.Add(symbol);
                    buffer.Clear();
                }
            }
            return result.ToArray();
        }

        private byte[] CreateCompressedArchive(List<FileMetadata> fileMetadata, Dictionary<byte, string> codes, byte[] encodedBits, int bitCount, int originalLength)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                WriteFileMetadata(writer, fileMetadata);
                writer.Write(originalLength);

                if (originalLength <= 1)
                    return ms.ToArray();

                writer.Write(codes.Count);
                foreach (var kvp in codes)
                {
                    writer.Write(kvp.Key);
                    var codeBytes = Encoding.ASCII.GetBytes(kvp.Value);
                    writer.Write((byte)codeBytes.Length);
                    writer.Write(codeBytes);
                }

                writer.Write(bitCount);
                writer.Write(encodedBits);

                return ms.ToArray();
            }
        }

        private void WriteFileMetadata(BinaryWriter writer, List<FileMetadata> metadata)
        {
            writer.Write(metadata.Count);
            foreach (var m in metadata)
            {
                var relBytes = Encoding.UTF8.GetBytes(m.RelativePath);
                writer.Write(relBytes.Length);
                writer.Write(relBytes);

                var orgBytes = Encoding.UTF8.GetBytes(m.OriginalPath ?? "");
                writer.Write(orgBytes.Length);
                writer.Write(orgBytes);

                writer.Write(m.FileSize);
                writer.Write(m.StartOffset);
            }
        }

        private List<FileMetadata> ReadFileMetadata(BinaryReader reader)
        {
            var list = new List<FileMetadata>();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                int relLen = reader.ReadInt32();
                var rel = Encoding.UTF8.GetString(reader.ReadBytes(relLen));

                int orgLen = reader.ReadInt32();
                var org = Encoding.UTF8.GetString(reader.ReadBytes(orgLen));

                int size = reader.ReadInt32();
                long offset = reader.ReadInt64();

                list.Add(new FileMetadata { RelativePath = rel, OriginalPath = org, FileSize = size, StartOffset = offset });
            }
            return list;
        }

        private void ExtractFiles(byte[] data, List<FileMetadata> metadata, string outputDir)
        {
            foreach (var meta in metadata)
            {
                byte[] fileData = new byte[meta.FileSize];
                Array.Copy(data, meta.StartOffset, fileData, 0, meta.FileSize);

                string path = Path.Combine(outputDir, meta.RelativePath);
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                File.WriteAllBytes(path, fileData);
            }
        }

        private void CreateEmptyFiles(List<FileMetadata> metadata, string outputDir)
        {
            foreach (var meta in metadata)
            {
                string path = Path.Combine(outputDir, meta.RelativePath);
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(path, new byte[0]);
            }
        }

        private void CreateSingleByteFiles(List<FileMetadata> metadata, string outputDir, byte value)
        {
            foreach (var meta in metadata)
            {
                string path = Path.Combine(outputDir, meta.RelativePath);
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(path, Enumerable.Repeat(value, meta.FileSize).ToArray());
            }
        }

        private CompressionResult CreateEmptyArchiveResult(string outputPath, List<FileMetadata> metadata, bool isEncrypted)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                WriteFileMetadata(writer, metadata);
                writer.Write(0);
                var data = ms.ToArray();
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

        private CompressionResult CreateSingleByteArchiveResult(string outputPath, byte b, List<FileMetadata> metadata, string password, bool isEncrypted)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                WriteFileMetadata(writer, metadata);
                writer.Write(1);
                writer.Write(b);
                var data = ms.ToArray();
                if (isEncrypted)
                    data = EncryptionHelper.Encrypt(data, password);
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

        private string FindCommonBasePath(string[] paths)
        {
            if (paths.Length == 1) return Path.GetDirectoryName(paths[0]) ?? "";
            var parts = paths[0].Split(Path.DirectorySeparatorChar);
            for (int i = 1; i < paths.Length; i++)
            {
                var current = paths[i].Split(Path.DirectorySeparatorChar);
                int j = 0;
                while (j < parts.Length && j < current.Length && parts[j] == current[j]) j++;
                parts = parts.Take(j).ToArray();
            }
            return string.Join(Path.DirectorySeparatorChar.ToString(), parts);
        }

        private string GetRelativePath(string basePath, string fullPath)
        {
            if (string.IsNullOrEmpty(basePath)) return Path.GetFileName(fullPath);
            Uri baseUri = new Uri(basePath + Path.DirectorySeparatorChar);
            Uri fullUri = new Uri(fullPath);
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

      

        private class FileMetadata
        {
            public string RelativePath { get; set; }
            public string OriginalPath { get; set; }
            public int FileSize { get; set; }
            public long StartOffset { get; set; }
        }
    }
}
