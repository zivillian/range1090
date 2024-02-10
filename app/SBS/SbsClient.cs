using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace range1090.SBS;

public class SbsClient : IDisposable
{
    private TcpClient? _client;

    public async Task ConnectAsync(string server, ushort port, CancellationToken cancellationToken)
    {
        _client = new TcpClient();
        await _client.ConnectAsync(server, port, cancellationToken);
    }

    public async IAsyncEnumerable<SbsMessage> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (_client is null) throw new InvalidOperationException("not connected");
        await using var stream = _client.GetStream();
        using var reader = new StreamReader(stream);
        while (_client.Connected && stream.CanRead && !reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null) continue;
            yield return new SbsMessage(line);
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}