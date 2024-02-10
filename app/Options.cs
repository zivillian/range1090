using CommandLine;

namespace range1090;

public class Options
{
    [Option(Default = "localhost", HelpText = "SBS host")]
    public string Server { get; set; }

    [Option(Default = (ushort)30003, HelpText = "SBS port")]
    public ushort Port { get; set; }

    [Option("lat", Required = true, HelpText = "Latitude of the receiver location")]
    public double Latitude { get; set; }

    [Option("lon", Required = true, HelpText = "Longitude of the receiver location")]
    public double Longitude { get; set; }
}