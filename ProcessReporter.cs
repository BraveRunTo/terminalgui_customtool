using System.Net.Sockets;
using System.Text;

namespace FastTool_Lib;

public class ProcessReporter : IDisposable
{
    private TcpListener _listener;
    private CancellationTokenSource _source;
    private TcpClient _client;
    public event Action<string> OnReceive;
    public ProcessReporter(bool server, int port)
    {
        if (server)
        {
            _listener = TcpListener.Create(port);
            _listener.Start();
            _source = new CancellationTokenSource();
            _listener.BeginAcceptTcpClient(OnAcceptTcpClient, null);
        }
        else
        {
            _client = new TcpClient();
            _client.BeginConnect("127.0.0.1", port, OnConnect, null);
            
        }
        
    }
    
    void OnConnect(IAsyncResult ar)
    {
        _client.EndConnect(ar);
        Task.Run(() => ClientReader(_source!.Token));
    }
    
    void OnAcceptTcpClient(IAsyncResult ar)
    {
        _client = _listener!.EndAcceptTcpClient(ar);
        Task.Run(() => ClientReader(_source!.Token));
        _listener.BeginAcceptTcpClient(OnAcceptTcpClient, null);
    }
    
    async Task ClientReader(CancellationToken token)
    {
        while (true)
        {
            await Task.Delay(1000, token).ConfigureAwait(true);
            byte[] buffer = new byte[1024];
            int count = await _client.GetStream().ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(true);
            string request = Encoding.UTF8.GetString(buffer, 0, count);

            OnReceive?.Invoke(request);
            
            if (token.IsCancellationRequested)
            {
                break;
            }
        }
    }
    
    public void ClientWriter(string message)
    {
        if (_client.Connected)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            _client.GetStream().Write(buffer, 0, buffer.Length);
        }
    }
    
    ~ProcessReporter()
    {
        _source?.Cancel();
        _client?.Close();
        _listener?.Stop();
        _client?.Dispose();
        _listener?.Dispose();
        OnReceive = null;
    }

    public void Dispose()
    {
        _source?.Cancel();
        _client?.Close();
        _listener?.Stop();
        _client?.Dispose();
        _listener?.Dispose();
        OnReceive = null;
    }
}