namespace range1090;

public class FlightLevelArea(ushort flightLevel)
{
    public readonly ushort FlightLevel = flightLevel;

    private readonly List<FlightLevelPosition> _positions = new();

    public IEnumerable<FlightLevelPosition> Positions => _positions;

    public bool IsValid => _positions.Count > 0;

    public bool Update(FlightLevelPosition position)
    {
        foreach (var existing in _positions)
        {
            if (existing.BearingIndex == position.BearingIndex)
            {
                if (existing.Distance < position.Distance)
                {
                    _positions.Remove(existing);
                    break;
                }
                else
                {
                    return false;
                }
            }
        }
        _positions.Add(position);
        return true;
    }
}