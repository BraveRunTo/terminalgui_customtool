using System.Runtime.InteropServices;
using Terminal.Gui;

namespace FastTool_Lib;

public class ClipboardListener : IDisposable
{
    
    private Task _task;
    private CancellationTokenSource _source;
    public event Action<string> OnReceive; 
    private string _lastText;
    
    public void Start()
    {
        if (_task != null)
        {
            return;
        }
        _source = new CancellationTokenSource();
        _task = Task.Run(() => Listen(_source.Token));
    }
    
    private async Task Listen(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(1000, token).ConfigureAwait(true);
            if (token.IsCancellationRequested)
            {
                return;
            }
            Application.MainLoop.Invoke(() =>
            {
                if (!Clipboard.TryGetClipboardData(out var text) || text == _lastText) return;
                OnReceive?.Invoke(text);
                _lastText = text;
            });
            
        }
    }

    public void SetEmpty()
    {
        Clipboard.TrySetClipboardData("");
    }
    
    public void Stop()
    {
        if (_source == null || _source.IsCancellationRequested)
        {
            return;
        }
        _source.Cancel();
        _source.Dispose();
        _source = null;
        _task = null;
    }

    public void Dispose()
    {
        Stop();
    }
}

