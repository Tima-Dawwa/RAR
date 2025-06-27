using RAR.Core.Compression;
using RAR.Core.Interfaces;
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

        public void FileCompression(HuffmanCompressor huffmanCompressor, ShannonFanoCompressor shannonCompressor, string inputFilePath)
        {
            PrepareNewTask();

            Task.Run(() =>
            {
                try
                {
                    if (huffmanCompressor != null)
                    {
                        var result = huffmanCompressor.Compress(inputFilePath, _cts.Token);
                        if (!_cts.Token.IsCancellationRequested)
                            FileCompressionCompleted?.Invoke(result);
                    }
                    else if (shannonCompressor != null)
                    {
                        var result = shannonCompressor.Compress(inputFilePath, _cts.Token);
                        if (!_cts.Token.IsCancellationRequested)
                            FileCompressionCompleted?.Invoke(result);
                    }
                    else
                    {
                        throw new ArgumentException("Both compressors cannot be null");
                    }
                }
                catch (Exception ex)
                {
                    if (!_cts.Token.IsCancellationRequested)
                        OperationFailed?.Invoke(ex);
                }
                finally
                {
                    Console.WriteLine($"[THREAD] Compressing on thread: {Thread.CurrentThread.ManagedThreadId}");
                    IsRunning = false;
                }
            }, _cts.Token);
        }

        public void FileDecompression(HuffmanCompressor huffmanDecompressor, ShannonFanoCompressor shannonDecompressor, string compressedFilePath, string outputPath)
        {
            PrepareNewTask();

            Task.Run(() =>
            {
                try
                {
                    if (huffmanDecompressor != null)
                    {
                        huffmanDecompressor.Decompress(compressedFilePath, outputPath, _cts.Token);
                        if (!_cts.Token.IsCancellationRequested)
                            FileDecompressionCompleted?.Invoke(outputPath);
                    }
                    else if (shannonDecompressor != null)
                    {
                        shannonDecompressor.Decompress(compressedFilePath, outputPath, _cts.Token);
                        if (!_cts.Token.IsCancellationRequested)
                            FileDecompressionCompleted?.Invoke(outputPath);
                    }
                    else
                    {
                        throw new ArgumentException("Both compressors cannot be null");
                    }
                }
                catch (Exception ex)
                {
                    if (!_cts.Token.IsCancellationRequested)
                        OperationFailed?.Invoke(ex);
                }
                finally
                {
                    Console.WriteLine($"[THREAD] Compressing on thread: {Thread.CurrentThread.ManagedThreadId}");
                    IsRunning = false;
                }
            }, _cts.Token);
        }

        public void FolderCompression(HuffmanFolderCompression huffmanFolderCompressor, ShannonFanoFolderCompression shannonFolderCompressor, string folderPath)
        {
            PrepareNewTask();
            Task.Run(() =>
            {
                try
                {
                    if (huffmanFolderCompressor != null)
                    {
                        var result = ((HuffmanFolderCompression)huffmanFolderCompressor).CompressFolder(folderPath, _cts.Token);
                        if (!_cts.Token.IsCancellationRequested)
                            FolderCompressionCompleted?.Invoke(result);
                    }
                    else if (shannonFolderCompressor != null)
                    {
                        var result = ((ShannonFanoFolderCompression)shannonFolderCompressor).CompressFolder(folderPath, _cts.Token);
                        if (!_cts.Token.IsCancellationRequested)
                            FolderCompressionCompleted?.Invoke(result);
                    }
                    else
                    {
                        throw new ArgumentException("Both compressors cannot be null.");
                    }
                }
                catch (Exception ex)
                {
                    if (!_cts.Token.IsCancellationRequested)
                        OperationFailed?.Invoke(ex);
                }
                finally
                {
                    Console.WriteLine($"[THREAD] Compressing on thread: {Thread.CurrentThread.ManagedThreadId}");
                    IsRunning = false;
                }
            }, _cts.Token);
        }

        public void FolderDecompression(HuffmanFolderCompression huffmanFolderDecompressor, ShannonFanoFolderCompression shannonFolderDecompressor, string compressedFolderPath, string outputPath)
        {
            PrepareNewTask();

            Task.Run(() =>
            {
                try
                {
                    if (huffmanFolderDecompressor != null)
                    {
                        ((HuffmanFolderCompression)huffmanFolderDecompressor).DecompressFolder(compressedFolderPath, outputPath, _cts.Token);
                        if (!_cts.Token.IsCancellationRequested)
                            FolderDecompressionCompleted?.Invoke(outputPath);
                    }
                    else if (shannonFolderDecompressor != null)
                    {
                        ((ShannonFanoFolderCompression)shannonFolderDecompressor).DecompressFolder(compressedFolderPath, outputPath, _cts.Token);
                        if (!_cts.Token.IsCancellationRequested)
                            FolderDecompressionCompleted?.Invoke(outputPath);
                    }
                    else
                    {
                        throw new ArgumentException("Both compressors cannot be null.");
                    }
                }
                catch (Exception ex)
                {
                    if (!_cts.Token.IsCancellationRequested)
                        OperationFailed?.Invoke(ex);
                }
                finally
                {
                    Console.WriteLine($"[THREAD] Compressing on thread: {Thread.CurrentThread.ManagedThreadId}");
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
