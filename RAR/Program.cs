using RAR.Core.Compression;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace RAR
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Select a text file to compress";
            ofd.Filter = "Text Files (*.txt)|*.txt";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string inputFilePath = ofd.FileName;
                string inputText = File.ReadAllText(inputFilePath);

                HuffmanCompressor huffman = new HuffmanCompressor();

                Dictionary<char, int> frequencies = huffman.CountFrequencies(inputText);
                HuffmanCompressor.Node root = huffman.BuildTree(frequencies);
                Dictionary<char, string> codes = huffman.GetCodes(root);
                string encoded = huffman.EncodeText(inputText, codes);

                string compressedFilePath = inputFilePath + ".huff";
                File.WriteAllText(compressedFilePath, encoded);

                Console.WriteLine("\nHuffman Codes:");
                foreach (var kvp in codes)
                {
                    Console.WriteLine($"'{kvp.Key}' = {kvp.Value}");
                }

                Console.WriteLine($"\nEncoded Output: {encoded}");
                Console.WriteLine($"\nOriginal Length: {inputText.Length * 8} bits");
                Console.WriteLine($"Compressed Length: {encoded.Length} bits");
                Console.WriteLine($"Compression Ratio: {(double)encoded.Length / (inputText.Length * 8):F2}");
                Console.WriteLine($"\nCompressed file saved at: {compressedFilePath}");
            }
            else
            {
                Console.WriteLine("No file selected.");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadLine();

        }
    }
}
