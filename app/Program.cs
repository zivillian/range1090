using CommandLine;
using range1090;
using range1090.SBS;

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

using var client = new SbsClient();
await client.ConnectAsync(options.Server, options.Port, cts.Token);
var calculator = new RangeCalculator(options.Latitude, options.Longitude);
var exporter = new GeoJsonExporter(options.Latitude, options.Longitude);
await calculator.LoadFileFileAsync(options.CacheFile, cts.Token);
try
{
    await foreach (var message in client.ReadAsync(cts.Token))
    {
        if (calculator.Add(message))
        {
            await exporter.ExportGeoJsonAsync(options.GeoJson, calculator.GetFlightLevels(), cts.Token);
        }
    }
}
catch(TaskCanceledException){}

await calculator.SaveToFileAsync(options.CacheFile, CancellationToken.None);
return 0;