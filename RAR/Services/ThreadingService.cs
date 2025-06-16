using RAR.Core.Compression;
using RAR.Helper;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RAR.Services
{
    public class ThreadingService
    {
        private CancellationTokenSource _cts;

        public event Action<CompressionResult> FileCompressionCompleted;
        public event Action<FolderCompressionResult> FolderCompressionCompleted;
        public event Action<string> FileDecompressionCompleted;
        public event Action<string> FolderDecompressionCompleted;
        public event Action<Exception> OperationFailed;

        public bool IsRunning { get; private set; }

        public void FileCompression(HuffmanCompressor compressor, string inputFilePath)
        {
            PrepareNewTask();

            Task.Run(() =>
            {
                try
                {
                    var result = compressor.Compress(inputFilePath);
                    if (!_cts.Token.IsCancellationRequested)
                        FileCompressionCompleted?.Invoke(result);
                }
                catch (Exception ex)
                {
                    if (!_cts.Token.IsCancellationRequested)
                        OperationFailed?.Invoke(ex);
                }
                finally
                {
                    IsRunning = false;
                }
            }, _cts.Token);
        }

        public void FileDecompression(HuffmanCompressor compressor, string compressedFilePath, string outputPath)
        {
            PrepareNewTask();

            Task.Run(() =>
            {
                try
                {
                    compressor.Decompress(compressedFilePath, outputPath);
                    if (!_cts.Token.IsCancellationRequested)
                        FileDecompressionCompleted?.Invoke(outputPath);
                }
                catch (Exception ex)
                {
                    if (!_cts.Token.IsCancellationRequested)
                        OperationFailed?.Invoke(ex);
                }
                finally
                {
                    IsRunning = false;
                }
            }, _cts.Token);
        }

        public void FolderCompression(HuffmanFolderCompression folderCompressor, string folderPath)
        {
            PrepareNewTask();

            Task.Run(() =>
            {
                try
                {
                    var result = folderCompressor.CompressFolder(folderPath);
                    if (!_cts.Token.IsCancellationRequested)
                        FolderCompressionCompleted?.Invoke(result);
                }
                catch (Exception ex)
                {
                    if (!_cts.Token.IsCancellationRequested)
                        OperationFailed?.Invoke(ex);
                }
                finally
                {
                    IsRunning = false;
                }
            }, _cts.Token);
        }

        public void FolderDecompression(HuffmanFolderCompression folderCompressor, string compressedFolderPath, string outputPath)
        {
            PrepareNewTask();

            Task.Run(() =>
            {
                try
                {
                    folderCompressor.DecompressFolder(compressedFolderPath, outputPath);
                    if (!_cts.Token.IsCancellationRequested)
                        FolderDecompressionCompleted?.Invoke(outputPath);
                }
                catch (Exception ex)
                {
                    if (!_cts.Token.IsCancellationRequested)
                        OperationFailed?.Invoke(ex);
                }
                finally
                {
                    IsRunning = false;
                }
            }, _cts.Token);
        }

        public void Cancel()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
                _cts.Cancel();

            IsRunning = false;
        }

        private void PrepareNewTask()
        {
            Cancel();
            _cts = new CancellationTokenSource();
            IsRunning = true;
        }
    }
}
