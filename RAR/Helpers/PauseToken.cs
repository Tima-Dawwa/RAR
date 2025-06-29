using RAR.Core.Interfaces;
using RAR.Helper;
using System;
using System.Threading;
using System.Threading.Tasks;

public struct PauseToken
{
    private readonly PauseTokenSource _source;

    public PauseToken(PauseTokenSource source) => _source = source;

    public bool IsPaused => _source?.IsPaused ?? false;

    public async Task WaitIfPausedAsync(CancellationToken cancellationToken = default)
    {
        if (_source != null && _source.IsPaused)
            await _source.WaitWhilePausedAsync(cancellationToken).ConfigureAwait(false);
    }

    public void WaitIfPaused(CancellationToken cancellationToken = default)
    {
        _source?.WaitWhilePaused(cancellationToken);
    }
}

public class PauseTokenSource : IDisposable
{
    private readonly object _lock = new object();
    private ManualResetEventSlim _pauseEvent = new ManualResetEventSlim(true); // Initially not paused
    private bool _isPaused = false;
    private bool _disposed = false;

    public bool IsPaused
    {
        get
        {
            lock (_lock)
            {
                return _isPaused && !_disposed;
            }
        }
    }

    public PauseToken Token => new PauseToken(this);

    public void Pause()
    {
        lock (_lock)
        {
            if (_disposed) return;

            if (!_isPaused)
            {
                _isPaused = true;
                _pauseEvent.Reset(); // Block waiting threads
            }
        }
    }

    public void Resume()
    {
        lock (_lock)
        {
            if (_disposed) return;

            if (_isPaused)
            {
                _isPaused = false;
                _pauseEvent.Set(); // Release waiting threads
            }
        }
    }

    public async Task WaitWhilePausedAsync(CancellationToken cancellationToken = default)
    {
        ManualResetEventSlim eventToWait;

        lock (_lock)
        {
            if (_disposed || !_isPaused)
                return;
            eventToWait = _pauseEvent;
        }

        // Wait asynchronously without blocking the thread
        await Task.Run(() => eventToWait.Wait(cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    public void WaitWhilePaused(CancellationToken cancellationToken = default)
    {
        ManualResetEventSlim eventToWait;

        lock (_lock)
        {
            if (_disposed || !_isPaused)
                return;
            eventToWait = _pauseEvent;
        }

        eventToWait.Wait(cancellationToken);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (!_disposed)
            {
                _disposed = true;
                _pauseEvent?.Set(); // Release any waiting threads
                _pauseEvent?.Dispose();
                _pauseEvent = null;
            }
        }
    }
}

// Example usage class to demonstrate proper integration
public class CompressionManager
{
    private PauseTokenSource _pauseTokenSource;
    private CancellationTokenSource _cancellationTokenSource;

    public CompressionManager()
    {
        _pauseTokenSource = new PauseTokenSource();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public void Pause()
    {
        _pauseTokenSource?.Pause();
        Console.WriteLine("Compression paused");
    }

    public void Resume()
    {
        _pauseTokenSource?.Resume();
        Console.WriteLine("Compression resumed");
    }

    public void Cancel()
    {
        _cancellationTokenSource?.Cancel();
        Console.WriteLine("Compression cancelled");
    }

    // Example of how to integrate with your compressor
    public async Task<CompressionResult> CompressWithPauseSupport(
        ICompressor compressor,
        string[] inputFiles,
        string outputPath,
        string password = null)
    {
        try
        {
            // This would be your actual compression call
            return await Task.Run(() =>
                compressor.CompressMultiple(
                    inputFiles,
                    outputPath,
                    _cancellationTokenSource.Token,
                    _pauseTokenSource.Token,
                    password));
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Compression was cancelled");
            return null;
        }
    }

    public void Dispose()
    {
        _pauseTokenSource?.Dispose();
        _cancellationTokenSource?.Dispose();
    }
}