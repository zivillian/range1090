using CommandLine;

namespace range1090;

public class Options
{
    [Option(Default = "localhost", HelpText = "BEAST host")]
    public string Server { get; set; }

    [Option(Default = (ushort)30003, HelpText = "BEAST port")]
    public ushort Port { get; set; }
}