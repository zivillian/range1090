using Geolocation;

namespace range1090;

public class FlightLevelPosition
{
    public double Latitude { get; }

    public double Longitude { get; }

    public double Bearing { get; }

    public ushort BearingIndex { get; }

    public double Distance { get; }

    private FlightLevelPosition(double latitude, double longitude, double bearing, double distance)
    {
        if (Double.IsNaN(latitude)) throw new ArgumentOutOfRangeException(nameof(latitude));
        if (Double.IsNaN(longitude)) throw new ArgumentOutOfRangeException(nameof(longitude));
        if (Double.IsNaN(bearing)) throw new ArgumentOutOfRangeException(nameof(bearing));
        if (Double.IsNaN(distance)) throw new ArgumentOutOfRangeException(nameof(distance));
        Latitude = latitude;
        Bearing = Double.NaN;
        Longitude = longitude;
        Bearing = bearing;
        BearingIndex = (ushort)bearing;
        Distance = distance;
    }

    public bool IsValid
    {
        get
        {
            if (Double.IsNaN(Latitude)) return false;
            if (Double.IsNaN(Longitude)) return false;
            if (Double.IsNaN(Bearing)) return false;
            if (Double.IsNaN(Distance)) return false;
            return true;
        }
    }

    public static FlightLevelPosition Create(Coordinate groundZero, Coordinate position)
    {
        var distance = GeoCalculator.GetDistance(groundZero, position, decimalPlaces: 1, DistanceUnit.NauticalMiles);
        var bearing = GeoCalculator.GetBearing(groundZero, position);
        return new FlightLevelPosition(position.Latitude, position.Longitude, bearing, distance);
    }
}