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
        public CompressionResult Compress(string inputFilePath)
        {
            byte[] inputBytes = File.ReadAllBytes(inputFilePath);

            if (inputBytes.Length == 0)
                return CreateEmptyFileResult(inputFilePath);

            if (inputBytes.Length == 1)
                return CreateSingleByteResult(inputFilePath, inputBytes[0]);

            var frequencies = CountFrequencies(inputBytes);

            Node root = BuildOptimizedTree(frequencies);

            var codes = new Dictionary<byte, BitString>();
            GenerateOptimizedCodes(root, new BitString(), codes);

            var encodedData = EncodeData(inputBytes, codes);

            string compressedPath = inputFilePath + ".huff";
            SaveCompressedFile(compressedPath, codes, encodedData, inputBytes.Length);

            return new CompressionResult
            {
                CompressedFilePath = compressedPath,
                OriginalSize = inputBytes.Length * 8,
                CompressedSize = encodedData.BitCount
            };
        }

        public void Decompress(string compressedFilePath, string outputFilePath)
        {
             var reader = new BinaryReader(File.OpenRead(compressedFilePath));

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

            int codeCount = reader.ReadInt32();
            var codes = new Dictionary<BitString, byte>();

            for (int i = 0; i < codeCount; i++)
            {
                byte symbol = reader.ReadByte();
                byte codeLength = reader.ReadByte();
                var codeBytes = reader.ReadBytes((codeLength + 7) / 8);
                var bitString = new BitString(codeBytes, codeLength);
                codes[bitString] = symbol;
            }

            int encodedBitCount = reader.ReadInt32();
            int encodedByteCount = (encodedBitCount + 7) / 8;
            byte[] encodedBytes = reader.ReadBytes(encodedByteCount);

            var decodedData = DecodeData(encodedBytes, codes, encodedBitCount, originalLength);
            File.WriteAllBytes(outputFilePath, decodedData);
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

            public int BitCount => _bitCount;
            public byte[] Bytes => _bytes;

            public BitString()
            {
                _bytes = new byte[0];
                _bitCount = 0;
                _hashCode = 0;
            }

            public BitString(byte[] bytes, int bitCount)
            {
                _bytes = bytes;
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

            public override int GetHashCode() => _hashCode;

            public bool Equals(BitString other)
            {
                if (other == null || _bitCount != other._bitCount) return false;
                for (int i = 0; i < _bytes.Length; i++)
                    if (_bytes[i] != other._bytes[i]) return false;
                return true;
            }

            public override bool Equals(object obj) => Equals(obj as BitString);
        }


        public Dictionary<char, int> CountFrequencies(string text)
        {
            var freq = new Dictionary<char, int>();
            foreach (char c in text)
            {
                if (!freq.ContainsKey(c))
                    freq[c] = 0;
                freq[c]++;
            }
            return freq;
        }

        public Node BuildTree(Dictionary<char, int> frequencies)
        {
            var nodes = new List<Node>();

            foreach (var pair in frequencies)
            {
                nodes.Add(new Node { Symbol = (byte)pair.Key, Frequency = pair.Value });
            }

            while (nodes.Count > 1)
            {
                nodes.Sort();
                Node left = nodes[0];
                Node right = nodes[1];
                nodes.RemoveAt(0);
                nodes.RemoveAt(0);

                Node parent = new Node
                {
                    Symbol = null,
                    Frequency = left.Frequency + right.Frequency,
                    Left = left,
                    Right = right
                };

                nodes.Add(parent);
            }

            return nodes[0];
        }

        public Dictionary<char, string> GetCodes(Node root)
        {
            var map = new Dictionary<char, string>();
            GenerateCodesForChar(root, "", map);
            return map;
        }

        private void GenerateCodesForChar(Node node, string code, Dictionary<char, string> map)
        {
            if (node == null) return;

            if (node.Symbol.HasValue)
                map[(char)node.Symbol.Value] = code;

            GenerateCodesForChar(node.Left, code + "0", map);
            GenerateCodesForChar(node.Right, code + "1", map);
        }

        public string EncodeText(string text, Dictionary<char, string> codes)
        {
            var sb = new StringBuilder();
            foreach (char c in text)
            {
                sb.Append(codes[c]);
            }
            return sb.ToString();
        }


        private Dictionary<byte, long> CountFrequencies(byte[] data)
        {
            var frequencies = new Dictionary<byte, long>();
            foreach (byte b in data)
            {
                frequencies.TryGetValue(b, out long count);
                frequencies[b] = count + 1;
            }
            return frequencies;
        }

        private Node BuildOptimizedTree(Dictionary<byte, long> frequencies)
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

        private void GenerateOptimizedCodes(Node node, BitString code, Dictionary<byte, BitString> codes)
        {
            if (node.Symbol.HasValue)
            {
                codes[node.Symbol.Value] = code;
                return;
            }

            GenerateOptimizedCodes(node.Left, code.Append(false), codes);
            GenerateOptimizedCodes(node.Right, code.Append(true), codes);
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
                    bool bit = (code.Bytes[byteIndex] & (1 << bitIndex)) != 0;

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

        private byte[] DecodeData(byte[] encodedData, Dictionary<BitString, byte> codes, int bitCount, int originalLength)
        {
            var result = new List<byte>(originalLength);
            var currentCode = new BitString();

            for (int bitIndex = 0; bitIndex < bitCount && result.Count < originalLength; bitIndex++)
            {
                int byteIndex = bitIndex / 8;
                int bitPos = 7 - (bitIndex % 8);
                bool bit = (encodedData[byteIndex] & (1 << bitPos)) != 0;

                currentCode = currentCode.Append(bit);

                if (codes.TryGetValue(currentCode, out byte symbol))
                {
                    result.Add(symbol);
                    currentCode = new BitString();
                }
            }

            return result.ToArray();
        }

        private void SaveCompressedFile(string path, Dictionary<byte, BitString> codes, BitString encodedData, int originalLength)
        {
            var writer = new BinaryWriter(File.Create(path));

            writer.Write(originalLength);

            if (originalLength <= 1) return;

            writer.Write(codes.Count);
            foreach (var pair in codes)
            {
                writer.Write(pair.Key);
                writer.Write((byte)pair.Value.BitCount);
                writer.Write(pair.Value.Bytes);
            }

            writer.Write(encodedData.BitCount);
            writer.Write(encodedData.Bytes);
        }

        private CompressionResult CreateEmptyFileResult(string inputPath)
        {
            string compressedPath = inputPath + ".huff";
             var writer = new BinaryWriter(File.Create(compressedPath));
            writer.Write(0); 

            return new CompressionResult
            {
                CompressedFilePath = compressedPath,
                OriginalSize = 0,
                CompressedSize = 32 
            };
        }

        private CompressionResult CreateSingleByteResult(string inputPath, byte singleByte)
        {
            string compressedPath = inputPath + ".huff";
            var writer = new BinaryWriter(File.Create(compressedPath));
            writer.Write(1); 
            writer.Write(singleByte);

            return new CompressionResult
            {
                CompressedFilePath = compressedPath,
                OriginalSize = 8,
                CompressedSize = 40 
            };
        }
    }
}