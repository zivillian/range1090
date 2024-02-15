namespace range1090;

public class FlightLevelArea(ushort flightLevel)
{
    private ushort _validSegments = 0;

    public readonly ushort FlightLevel = flightLevel;

    private readonly FlightLevelSegment?[] _positions = new FlightLevelSegment?[360];

    public IEnumerable<FlightLevelSegment> Positions => _positions.Where(x => x != null)!;

    public bool IsValid => _validSegments > 0;

    public double CoveragePercentage => (_validSegments / 360d)*100;

    public FlightLevelSegment? GetSegment(ushort bearing)
    {
        return _positions[bearing];
    }

    public bool Update(double latitude, double longitude, ushort bearing, double distance)
    {
        var segment = _positions[bearing];
        if (segment is null)
        {
            _positions[bearing] = FlightLevelSegment.Create(latitude, longitude, distance);
            _validSegments++;
            return true;
        }
        return segment.Update(latitude, longitude, distance);
    }
}