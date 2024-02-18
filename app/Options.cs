using CommandLine;

namespace range1090;

public class Options
{
    [Option(Default = "localhost", HelpText = "SBS host")]
    public string Server { get; set; } = "localhost";

    [Option(Separator = ',', HelpText = "SBS ports (separated by ',')")]
    public IEnumerable<ushort> Ports { get; set; } = new ushort[] { 30003 };

    [Option("lat", Required = true, HelpText = "Latitude of the receiver location")]
    public double Latitude { get; set; }

    [Option("lon", Required = true, HelpText = "Longitude of the receiver location")]
    public double Longitude { get; set; }

    [Option("cache", HelpText = "path to cachefile")]
    public string? CacheFile { get; set; }

    [Option("geojson", HelpText = "path to geojson file")]
    public string? GeoJson { get; set; }

    [Option(Default = 60, HelpText = "interval for geojson export")]
    public int Interval { get; set; } = 60;

    [Option(Default = (byte)4, HelpText = "number of decimals for geojson coordinates")]
    public byte Precision { get; set; } = 4;

    [Option('v', HelpText = "output statistics whenever a position is added")]
    public bool Verbose { get; set; }
}