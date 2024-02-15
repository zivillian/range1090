using CommandLine;
using range1090;
using range1090.SBS;
using System.Runtime.InteropServices;

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

using var client = new SbsClient();
await client.ConnectAsync(options.Server, options.Port, cts.Token);
var calculator = new RangeCalculator(options.Latitude, options.Longitude, options.Verbose);
var exporter = new GeoJsonExporter(options.Latitude, options.Longitude, options.Precision, options.Interval);
await calculator.LoadFileFileAsync(options.CacheFile, cts.Token);
var _nextStats = DateTime.MinValue;
try
{
    await foreach (var message in client.ReadAsync(cts.Token))
    {
        if (calculator.Add(message))
        {
            await exporter.ExportGeoJsonAsync(options.GeoJson, calculator.GetFlightLevels(), cts.Token);
        }

        if (DateTime.Now > _nextStats)
        {
            _nextStats = DateTime.Now.AddSeconds(60);
            calculator.DumpStats();
        }
    }
}
catch(TaskCanceledException){}

await calculator.SaveToFileAsync(options.CacheFile, CancellationToken.None);
return 0;