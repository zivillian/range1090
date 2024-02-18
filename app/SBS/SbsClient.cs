using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace range1090.SBS;

public class SbsClient : IDisposable
{
    private readonly string _server;
    private readonly ushort _port;
    private TcpClient _client;

    public SbsClient(string server, ushort port)
    {
        _server = server;
        _port = port;
        _client = new TcpClient();
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        await _client.ConnectAsync(_server, _port, cancellationToken);
    }

    public async IAsyncEnumerable<SbsMessage> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!_client.Connected) throw new InvalidOperationException("not connected");
        await using var stream = _client.GetStream();
        using var reader = new StreamReader(stream);
        var buffer = new char[256];
        var bufferValidEnd = 0;
        while (_client.Connected && stream.CanRead && !reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(bufferValidEnd), cancellationToken);
            bufferValidEnd += read;
            var readBuffer = buffer.AsMemory(0, bufferValidEnd);
            var eol = readBuffer.Span.IndexOf('\n');
            if (eol == -1)
            {
                if (readBuffer.Length == buffer.Length)
                {
                    throw new InvalidOperationException("buffer full");
                }
                else
                {
                    continue;
                }
            }
            do
            {
                var line = readBuffer[..eol];
                if (line.Length > 1)
                {
                    if (line.Span[^1] == '\r')
                    {
                        line = line[..^1];
                    }

                    yield return new SbsMessage(line.Span);
                }

                readBuffer = readBuffer[eol..][1..];
                eol = readBuffer.Span.IndexOf('\n');
            } while (eol != -1);

            if (!readBuffer.IsEmpty)
            {
                readBuffer.CopyTo(buffer);
                bufferValidEnd = readBuffer.Length;
            }
            else
            {
                bufferValidEnd = 0;
            }
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}