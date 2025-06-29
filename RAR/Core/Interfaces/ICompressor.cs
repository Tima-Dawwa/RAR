using RAR.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RAR.Core.Interfaces
{
    public interface ICompressor
    {
        // Single file compression
        CompressionResult Compress(string inputFilePath, CancellationToken token, PauseToken? pauseToken = null, string password = null);

        // Multi-file compression - ADD THIS METHOD
        CompressionResult CompressMultiple(string[] inputFilePaths, string outputPath, CancellationToken token, PauseToken? pauseToken = null, string password = null);

        // Single file decompression
        void Decompress(string compressedFilePath, string outputFilePath, CancellationToken token, string password = null, PauseToken? pauseToken = null);

        // Multi-file decompression - ADD THIS METHOD
        void DecompressMultiple(string compressedFilePath, string outputDirectory, CancellationToken token, string password = null, PauseToken? pauseToken = null);
    }

    public interface IFolderCompression
    {
        FolderCompressionResult CompressFolder(string folderPath, CancellationToken token, PauseToken? pauseToken = null, string password = null);
        void DecompressFolder(string compressedFolderPath, string outputFolderPath, CancellationToken token, string password = null, PauseToken? pauseToken = null);
    }
}