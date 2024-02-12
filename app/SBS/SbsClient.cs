using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace range1090.SBS;

public class SbsClient : IDisposable
{
    private TcpClient? _client;
    private const string LineBreak = "\r\n";

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
        var buffer = new char[256];
        var bufferStart = 0;
        while (_client.Connected && stream.CanRead && !reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(bufferStart), cancellationToken);
            var readBuffer = buffer.AsMemory(0, read + bufferStart);
            var eol = readBuffer.Span.IndexOf(LineBreak);
            if (eol == -1)
            {
                throw new InvalidOperationException("buffer full");
            }
            do
            {
                var line = readBuffer.Slice(0, eol);
                yield return new SbsMessage(line.Span);
                readBuffer = readBuffer.Slice(eol).Slice(LineBreak.Length);
                if (readBuffer.IsEmpty)
                {

                }
                eol = readBuffer.Span.IndexOf(LineBreak);
            } while (eol != -1);

            if (!readBuffer.IsEmpty)
            {
                readBuffer.CopyTo(buffer);
                bufferStart = readBuffer.Length;
            }
            else
            {
                bufferStart = 0;
            }
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}