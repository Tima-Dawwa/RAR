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
            string text = File.ReadAllText(inputFilePath);

            Dictionary<char, int> frequencies = CountFrequencies(text);
            Node root = BuildTree(frequencies);
            Dictionary<char, string> codes = GetCodes(root);
            string encodedText = EncodeText(text, codes);

            string compressedPath = inputFilePath + ".huff";
            using (StreamWriter writer = new StreamWriter(compressedPath))
            {
                foreach (var pair in codes)
                {
                    writer.WriteLine($"{(int)pair.Key}:{pair.Value}");
                }
                writer.WriteLine("###");
                writer.WriteLine(encodedText);
            }

            return new CompressionResult
            {
                CompressedFilePath = compressedPath,
                OriginalSize = text.Length * 8,
                CompressedSize = encodedText.Length
            };
        }


        public void Decompress(string compressedFilePath, string outputFilePath)
        {
            var lines = File.ReadAllLines(compressedFilePath);
            var codes = new Dictionary<string, char>();
            int i = 0;

            // Read the code map
            while (i < lines.Length && lines[i] != "###")
            {
                var parts = lines[i].Split(':');
                char symbol = (char)int.Parse(parts[0]);
                codes[parts[1]] = symbol;
                i++;
            }

            // Read encoded data
            i++; // Skip the "###" line
            StringBuilder encodedText = new StringBuilder();
            while (i < lines.Length)
            {
                encodedText.Append(lines[i]);
                i++;
            }

            // Decode
            string bits = encodedText.ToString();
            StringBuilder decoded = new StringBuilder();
            string current = "";

            foreach (char bit in bits)
            {
                current += bit;
                if (codes.ContainsKey(current))
                {
                    decoded.Append(codes[current]);
                    current = "";
                }
            }

            File.WriteAllText(outputFilePath, decoded.ToString());
        }


        // ====== Public Helper Classes and Methods ======

        public class Node : IComparable<Node>
        {
            public char? Symbol;
            public int Frequency;
            public Node Left, Right;

            public int CompareTo(Node other)
            {
                int result = Frequency.CompareTo(other.Frequency);
                if (result == 0)
                    result = (Symbol ?? '\0').CompareTo(other.Symbol ?? '\0'); // For tie-break
                return result;
            }
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
                nodes.Add(new Node { Symbol = pair.Key, Frequency = pair.Value });
            }

            while (nodes.Count > 1)
            {
                // Sort by frequency ascending
                nodes.Sort();

                // Take two smallest nodes
                Node left = nodes[0];
                Node right = nodes[1];

                // Remove them
                nodes.RemoveAt(0);
                nodes.RemoveAt(0);

                // Create new parent node
                Node parent = new Node
                {
                    Symbol = null,
                    Frequency = left.Frequency + right.Frequency,
                    Left = left,
                    Right = right
                };

                // Add back to list
                nodes.Add(parent);
            }

            return nodes[0];
        }


        void GenerateCodes(Node node, string code, Dictionary<char, string> map)
        {
            if (node == null) return;

            if (node.Symbol.HasValue)
                map[node.Symbol.Value] = code;

            GenerateCodes(node.Left, code + "0", map);
            GenerateCodes(node.Right, code + "1", map);
        }

        public Dictionary<char, string> GetCodes(Node root)
        {
            var map = new Dictionary<char, string>();
            GenerateCodes(root, "", map);
            return map;
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

    }
}
