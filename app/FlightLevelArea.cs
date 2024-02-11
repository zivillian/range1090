namespace range1090;

public class FlightLevelArea(ushort flightLevel)
{
    public readonly ushort FlightLevel = flightLevel;

    private readonly List<FlightLevelSegment> _positions = new();

    public IEnumerable<FlightLevelSegment> Positions => _positions;

    public bool IsValid => _positions.Count > 0;

    public double CoveragePercentage => (_positions.Count / 360d)*100;

    public bool Update(FlightLevelPosition position)
    {
        foreach (var existing in _positions)
        {
            if (existing.BearingIndex == position.BearingIndex)
            {
                return existing.Update(position);
            }
        }
        _positions.Add(FlightLevelSegment.Create(position));
        return true;
    }
}