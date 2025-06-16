using RAR.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAR.Core.Interfaces
{
    public interface ICompressor
    {
        CompressionResult Compress(string inputFilePath, string password = null);
        void Decompress(string compressedFilePath, string outputFilePath, string password = null);
    }
}
