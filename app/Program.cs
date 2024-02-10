using CommandLine;
using range1090;

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

using var client = new BeastClient();
await client.ConnectAsync(options.Server, options.Port, cts.Token);
await foreach (var line in client.ReadAsync(cts.Token))
{
    Console.WriteLine(line);
}
return 0;