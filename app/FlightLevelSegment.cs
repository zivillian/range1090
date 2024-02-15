namespace range1090;

public class FlightLevelSegment
{
    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    public double Distance { get; private set; }

    private FlightLevelSegment(double latitude, double longitude, double distance)
    {
        if (Double.IsNaN(latitude)) throw new ArgumentOutOfRangeException(nameof(latitude));
        if (Double.IsNaN(longitude)) throw new ArgumentOutOfRangeException(nameof(longitude));
        if (Double.IsNaN(distance)) throw new ArgumentOutOfRangeException(nameof(distance));
        Latitude = latitude;
        Longitude = longitude;
        Distance = distance;
    }

    public bool Update(double latitude, double longitude, double distance)
    {
        if (distance > Distance)
        {
            Distance = distance;
            Latitude = latitude;
            Longitude = longitude;
            return true;
        }

        return false;
    }
    public static FlightLevelSegment Create(double latitude, double longitude, double distance)
    {
        return new FlightLevelSegment(latitude, longitude, distance);
    }
    
    public bool IsValid
    {
        get
        {
            if (Double.IsNaN(Latitude)) return false;
            if (Double.IsNaN(Longitude)) return false;
            if (Double.IsNaN(Distance)) return false;
            return true;
        }
    }
}