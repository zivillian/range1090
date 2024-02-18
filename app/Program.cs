using CommandLine;
using range1090;
using range1090.SBS;
using System.Runtime.InteropServices;
using System.Threading.Channels;

var parsed = Parser.Default.ParseArguments<Options>(args);
if (parsed.Tag == ParserResultType.NotParsed)
{
    return -1;
}
var options = parsed.Value;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};
using var handler = PosixSignalRegistration.Create(PosixSignal.SIGTERM, x =>
{
    x.Cancel = true;
    cts.Cancel();
});

var calculator = new RangeCalculator(options.Latitude, options.Longitude, options.Verbose);
var exporter = new GeoJsonExporter(options.Latitude, options.Longitude, options.Precision, options.Interval);
await calculator.LoadFromFileAsync(options.CacheFile, cts.Token);

var clients = new List<SbsClient>();
foreach (var port in options.Ports)
{
    clients.Add(new SbsClient(options.Server, port));
}

await Task.WhenAll(clients.Select(x => x.ConnectAsync(cts.Token)));

var channel = Channel.CreateBounded<SbsMessage>(10);
var tasks = clients.Select(x => WriteToChannelAsync(channel.Writer, x, cts.Token)).ToList();
var readTask = ReadFromChannelAsync(channel.Reader, calculator, exporter, options, cts.Token);
tasks.Add(readTask);
await Task.WhenAny(tasks);
channel.Writer.Complete();
await readTask;

await calculator.SaveToFileAsync(options.CacheFile, CancellationToken.None);
foreach (var client in clients)
{
    client.Dispose();
}
return 0;

async Task WriteToChannelAsync(ChannelWriter<SbsMessage> writer, SbsClient client, CancellationToken cancellationToken)
{
    await Task.Yield();
    try
    {
        await foreach (var message in client.ReadAsync(cancellationToken).WithCancellation(cancellationToken))
        {
            await writer.WriteAsync(message, cancellationToken);
        }
    }
    catch (TaskCanceledException) { }
}

async Task ReadFromChannelAsync(ChannelReader<SbsMessage> reader, RangeCalculator calculator, GeoJsonExporter exporter, Options options, CancellationToken cancellationToken)
{
    var nextStats = DateTime.MinValue;
    try
    {
        await foreach (var message in reader.ReadAllAsync(cancellationToken).WithCancellation(cancellationToken))
        {
            if (calculator.Add(message))
            {
                await exporter.ExportGeoJsonAsync(options.GeoJson, calculator.GetFlightLevels(), cancellationToken);
            }

            if (!options.Verbose && DateTime.Now > nextStats)
            {
                nextStats = DateTime.Now.AddSeconds(60);
                calculator.DumpStats();
            }
        }
    }
    catch (TaskCanceledException) { }
    catch (OperationCanceledException) { }
}