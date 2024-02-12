namespace range1090;

public class FlightLevelArea(ushort flightLevel)
{
    public readonly ushort FlightLevel = flightLevel;

    private readonly List<FlightLevelSegment> _positions = new(360);

    public IEnumerable<FlightLevelSegment> Positions => _positions;

    public bool IsValid => _positions.Count > 0;

    public double CoveragePercentage => (_positions.Count / 360d)*100;

    public bool Update(double latitude, double longitude, ushort bearing, double distance)
    {
        foreach (var existing in _positions)
        {
            if (existing.Bearing == bearing)
            {
                return existing.Update(latitude, longitude, bearing, distance);
            }
        }
        _positions.Add(FlightLevelSegment.Create(latitude, longitude, bearing, distance));
        return true;
    }
}