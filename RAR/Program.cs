using RAR.Core.Compression;
using RAR.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace RAR
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Select a file to compress";
            ofd.Filter = "All Files (*.*)|*.*|Text Files (*.txt)|*.txt";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string inputFilePath = ofd.FileName;

                try
                {
                    HuffmanCompressor huffman = new HuffmanCompressor();

                    CompressionResult result = huffman.Compress(inputFilePath);

                    Console.WriteLine("Compression completed successfully!");
                    Console.WriteLine($"Original file: {inputFilePath}");
                    Console.WriteLine($"Compressed file: {result.CompressedFilePath}");
                    Console.WriteLine($"Original size: {result.OriginalSize} bits ({result.OriginalSize / 8} bytes)");
                    Console.WriteLine($"Compressed size: {result.CompressedSize} bits ({(result.CompressedSize + 7) / 8} bytes)");

                    if (result.OriginalSize > 0)
                    {
                        double compressionRatio = (double)result.CompressedSize / result.OriginalSize;
                        double spaceSaved = 1.0 - compressionRatio;
                        Console.WriteLine($"Compression ratio: {compressionRatio:F3}");
                        Console.WriteLine($"Space saved: {spaceSaved:P1}");
                    }

                    Console.WriteLine("\nDo you want to see the Huffman codes? (y/n)");
                    if (Console.ReadLine()?.ToLower() == "y")
                    {
                        DisplayHuffmanCodes(inputFilePath, huffman);
                    }

                    Console.WriteLine("\nDo you want to test decompression? (y/n)");
                    if (Console.ReadLine()?.ToLower() == "y")
                    {
                        TestDecompression(result.CompressedFilePath, inputFilePath, huffman);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during compression: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("No file selected.");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadLine();
        }

        private static void DisplayHuffmanCodes(string inputFilePath, HuffmanCompressor huffman)
        {
            try
            {
                if (IsTextFile(inputFilePath))
                {
                    string inputText = File.ReadAllText(inputFilePath);

                    if (string.IsNullOrEmpty(inputText))
                    {
                        Console.WriteLine("File is empty - no codes to display.");
                        return;
                    }

                    var frequencies = huffman.CountFrequencies(inputText);
                    var root = huffman.BuildTree(frequencies);
                    var codes = huffman.GetCodes(root);

                    Console.WriteLine("\nHuffman Codes:");
                    foreach (var kvp in codes)
                    {
                        string displayChar = kvp.Key == '\n' ? "\\n" :
                                           kvp.Key == '\r' ? "\\r" :
                                           kvp.Key == '\t' ? "\\t" :
                                           kvp.Key.ToString();
                        Console.WriteLine($"'{displayChar}' = {kvp.Value}");
                    }
                }
                else
                {
                    Console.WriteLine("Binary file - codes not displayed for readability.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error displaying codes: {ex.Message}");
            }
        }

        private static void TestDecompression(string compressedFilePath, string originalFilePath, HuffmanCompressor huffman)
        {
            try
            {
                string decompressedFilePath = originalFilePath + ".decompressed";

                huffman.Decompress(compressedFilePath, decompressedFilePath);

                byte[] originalBytes = File.ReadAllBytes(originalFilePath);
                byte[] decompressedBytes = File.ReadAllBytes(decompressedFilePath);

                if (ByteArraysEqual(originalBytes, decompressedBytes))
                {
                    Console.WriteLine("✓ Decompression successful! Files are identical.");
                    Console.WriteLine($"Decompressed file saved as: {decompressedFilePath}");
                }
                else
                {
                    Console.WriteLine("✗ Decompression failed! Files do not match.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during decompression: {ex.Message}");
            }
        }

        private static bool IsTextFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            return extension == ".txt" || extension == ".csv" || extension == ".log" ||
                   extension == ".xml" || extension == ".json" || extension == ".cs";
        }

        private static bool ByteArraysEqual(byte[] a1, byte[] a2)
        {
            if (a1.Length != a2.Length) return false;
            for (int i = 0; i < a1.Length; i++)
                if (a1[i] != a2[i]) return false;
            return true;
        }
    }
}