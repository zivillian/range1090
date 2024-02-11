namespace range1090;

public class FlightLevelSegment
{
    public ushort BearingIndex { get; }

    public FlightLevelPosition MinBearing { get; private set; }

    public FlightLevelPosition MaxBearing { get; private set; }

    private FlightLevelSegment(FlightLevelPosition position)
    {
        MinBearing = position;
        MaxBearing = position;
        BearingIndex = position.BearingIndex;
    }

    public bool Update(FlightLevelPosition position)
    {
        if (position.BearingIndex != BearingIndex) return false;
        var result = false;
        if (position.Bearing <= MinBearing.Bearing &&
            position.Distance > MinBearing.Distance)
        {
            MinBearing = position;
            result = true;
        }
        else if(position.Bearing < MinBearing.Bearing &&
                position.Distance >= MinBearing.Distance)
        {
            MinBearing = position;
            result = true;
        }
        if (position.Bearing >= MaxBearing.Bearing && 
            position.Distance > MaxBearing.Distance)
        {
            MaxBearing = position;
            result = true;
        }
        else if (position.Bearing > MaxBearing.Bearing && 
                 position.Distance >= MaxBearing.Distance)
        {
            MaxBearing = position;
            result = true;
        }
        return result;
    }

    public static FlightLevelSegment Create(FlightLevelPosition position)
    {
        return new FlightLevelSegment(position);
    }

    public bool IsValid => MinBearing.IsValid && MaxBearing.IsValid;
}