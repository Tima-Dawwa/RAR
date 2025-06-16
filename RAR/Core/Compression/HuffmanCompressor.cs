using RAR.Core.Interfaces;
using RAR.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RAR.Core.Compression
{
    public class HuffmanCompressor : ICompressor
    {
        public CompressionResult Compress(string inputFilePath, string password = null)
        {
            try
            {
                if (!File.Exists(inputFilePath))
                    throw new FileNotFoundException("Input file not found: " + inputFilePath);

                byte[] inputBytes = File.ReadAllBytes(inputFilePath);
                bool isEncrypted = !string.IsNullOrWhiteSpace(password);

                if (inputBytes.Length == 0)
                    return CreateEmptyFileResult(inputFilePath, isEncrypted);

                if (inputBytes.Length == 1)
                    return CreateSingleByteResult(inputFilePath, inputBytes[0], password, isEncrypted);

                var frequencies = CountFrequencies(inputBytes);
                Node root = BuildHuffmanTree(frequencies);
                var codes = new Dictionary<byte, BitString>();
                GenerateCodes(root, new BitString(), codes);

                var encodedData = EncodeData(inputBytes, codes);
                string compressedPath = inputFilePath + ".huff";

                // Create the compressed data
                byte[] compressedBytes = CreateCompressedData(codes, encodedData, inputBytes.Length);

                // Encrypt if password is provided
                if (isEncrypted)
                {
                    compressedBytes = EncryptionHelper.Encrypt(compressedBytes, password);
                }

                // Save to file
                File.WriteAllBytes(compressedPath, compressedBytes);

                return new CompressionResult
                {
                    CompressedFilePath = compressedPath,
                    OriginalSize = inputBytes.Length * 8,
                    CompressedSize = compressedBytes.Length * 8,
                    IsEncrypted = isEncrypted
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Compression failed: " + ex.Message, ex);
            }
        }

        public void Decompress(string compressedFilePath, string outputFilePath, string password = null)
        {
            try
            {
                if (!File.Exists(compressedFilePath))
                    throw new FileNotFoundException("Compressed file not found: " + compressedFilePath);

                byte[] fileBytes = File.ReadAllBytes(compressedFilePath);

                // Try to decrypt if password is provided
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
                    int originalLength = reader.ReadInt32();

                    if (originalLength == 0)
                    {
                        File.WriteAllBytes(outputFilePath, new byte[0]);
                        return;
                    }

                    if (originalLength == 1)
                    {
                        byte singleByte = reader.ReadByte();
                        File.WriteAllBytes(outputFilePath, new byte[] { singleByte });
                        return;
                    }

                    var codes = ReadCodesFromFile(reader);
                    int encodedBitCount = reader.ReadInt32();
                    int encodedByteCount = (encodedBitCount + 7) / 8;
                    byte[] encodedBytes = reader.ReadBytes(encodedByteCount);

                    var decodedData = DecodeData(encodedBytes, codes, encodedBitCount, originalLength);
                    File.WriteAllBytes(outputFilePath, decodedData);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Decompression failed: " + ex.Message, ex);
            }
        }

        private byte[] CreateCompressedData(Dictionary<byte, BitString> codes, BitString encodedData, int originalLength)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(originalLength);

                if (originalLength <= 1) return ms.ToArray();

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

        // ... (Keep all existing Node, BitString, and other helper classes/methods) ...

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

        private CompressionResult CreateEmptyFileResult(string inputPath, bool isEncrypted)
        {
            string compressedPath = inputPath + ".huff";
            byte[] data = new byte[] { 0, 0, 0, 0 }; // 4 bytes for originalLength = 0

            if (isEncrypted)
            {
                // This won't actually be called since we check for empty password first
                data = new byte[] { 0, 0, 0, 0 };
            }

            File.WriteAllBytes(compressedPath, data);

            return new CompressionResult
            {
                CompressedFilePath = compressedPath,
                OriginalSize = 0,
                CompressedSize = data.Length * 8,
                IsEncrypted = isEncrypted
            };
        }

        private CompressionResult CreateSingleByteResult(string inputPath, byte singleByte, string password, bool isEncrypted)
        {
            string compressedPath = inputPath + ".huff";
            byte[] data;

            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(1); // originalLength = 1
                writer.Write(singleByte);
                data = ms.ToArray();
            }

            if (isEncrypted && !string.IsNullOrWhiteSpace(password))
            {
                data = EncryptionHelper.Encrypt(data, password);
            }

            File.WriteAllBytes(compressedPath, data);

            return new CompressionResult
            {
                CompressedFilePath = compressedPath,
                OriginalSize = 8,
                CompressedSize = data.Length * 8,
                IsEncrypted = isEncrypted
            };
        }
    }
}