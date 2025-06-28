using System;
using System.Threading;
using System.Threading.Tasks;

public struct PauseToken
{
    private readonly PauseTokenSource _source;
    public PauseToken(PauseTokenSource source) => _source = source;

    public bool IsPaused => _source != null && _source.IsPaused;

    public async Task WaitIfPausedAsync()
    {
        if (IsPaused)
            await _source.WaitWhilePausedAsync().ConfigureAwait(false);
    }

    public void WaitIfPaused()
    {
        if (IsPaused)
            _source.WaitWhilePaused();
    }
}

public class PauseTokenSource
{
    private volatile TaskCompletionSource<bool> _resumeRequest = null;

    public bool IsPaused => _resumeRequest != null;

    public PauseToken Token => new PauseToken(this);

    public void Pause()
    {
        Interlocked.CompareExchange(ref _resumeRequest, new TaskCompletionSource<bool>(), null);
    }

    public void Resume()
    {
        while (true)
        {
            var tcs = _resumeRequest;
            if (tcs == null)
                return;

            if (Interlocked.CompareExchange(ref _resumeRequest, null, tcs) == tcs)
            {
                tcs.SetResult(true);
                return;
            }
        }
    }

    public async Task WaitWhilePausedAsync()
    {
        var tcs = _resumeRequest;
        if (tcs != null)
            await tcs.Task.ConfigureAwait(false);
    }

    public void WaitWhilePaused()
    {
        var tcs = _resumeRequest;
        if (tcs != null)
            tcs.Task.Wait();
    }
}
