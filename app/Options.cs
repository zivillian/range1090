using CommandLine;

namespace range1090;

public class Options
{
    [Option(Default = "localhost", HelpText = "SBS host")]
    public string Server { get; set; } = "localhost";

    [Option(Default = (ushort)30003, HelpText = "SBS port")]
    public ushort Port { get; set; }

    [Option("lat", Required = true, HelpText = "Latitude of the receiver location")]
    public double Latitude { get; set; }

    [Option("lon", Required = true, HelpText = "Longitude of the receiver location")]
    public double Longitude { get; set; }

    [Option("cache", HelpText = "path to cachefile")]
    public string? CacheFile { get; set; }

    [Option("geojson", HelpText = "path to geojson file")]
    public string? GeoJson { get; set; }
}