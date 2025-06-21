using RAR.Core.Interfaces;
using RAR.Helper;
using System;
using System.Threading;

namespace RAR.Core.Compression
{
    public class ShannonFanoCompressor : ICompressor
    {
        public CompressionResult Compress(string inputFilePath, CancellationToken token, string password = null)
        {
            // Implementation goes here
            throw new NotImplementedException();
        }

        public void Decompress(string compressedFilePath, string outputFilePath, CancellationToken token, string password = null)
        {
            // Implementation goes here
            throw new NotImplementedException();
        }
    }
}