using RAR.Core.Interfaces;
using RAR.Helper;
using System;

namespace RAR.Core.Compression
{
    public class ShannonFanoCompressor : ICompressor
    {
        public CompressionResult Compress(string inputFilePath, string password = null)
        {
            // Implementation goes here
            throw new NotImplementedException();
        }

        public void Decompress(string compressedFilePath, string outputFilePath, string password = null)
        {
            // Implementation goes here
            throw new NotImplementedException();
        }
    }
}