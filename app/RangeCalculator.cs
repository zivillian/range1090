using System.Net;
using System.Threading;
using Geolocation;
using range1090;
using range1090.SBS;

public class RangeCalculator(double latitude, double longitude)
{
    private readonly Coordinate _groundZero = new(latitude, longitude);

    private readonly PolarRange _range = new ();

    public int MessageCount { get; private set; }

    public int FlightCount => _flights.Count;

    private readonly Dictionary<string, Flight> _flights = new();

    public void Add(SbsMessage message)
    {
        MessageCount++;
        if (!_flights.TryGetValue(message.HexIdent, out var flight))
        {
            //cleanup flights
            var deadline = message.Generated.AddMinutes(-5);
            foreach (var value in _flights.Values.Where(x=>x.LastMessage < deadline).ToList())
            {
                _flights.Remove(value.HexIdent);
            }
            flight = new Flight(message);
            _flights.Add(message.HexIdent, flight);
        }

        if (!flight.Add(message)) return;
        if (!flight.IsValid) return;
        var distance = GetDistanceInNm(flight.Position);
        var bearing = GetBearingInDegree(flight.Position);
        _range.Add(bearing, flight.FlightLevel, distance);
    }

    public Task SaveToFileAsync(string filename, CancellationToken cancellationToken)
    {
        if (String.IsNullOrEmpty(filename)) return Task.CompletedTask;
        
        var data = _range.Serialize();
        return File.WriteAllBytesAsync(filename, data, cancellationToken);
    }

    public async Task LoadFileFileAsync(string filename, CancellationToken cancellationToken)
    {
        if (String.IsNullOrEmpty(filename)) return;
        if (!File.Exists(filename)) return;

        var data = await File.ReadAllBytesAsync(filename, cancellationToken);
        _range.Deserialize(data);
    }

    public void DumpRange()
    {
        for (ushort i = 0; i < 360; i++)
        {
            Console.WriteLine($"{i}° {_range.MaxDistance(i)}");
        }
    }

    private ushort GetDistanceInNm(Coordinate position)
    {
        return (ushort)GeoCalculator.GetDistance(_groundZero, position, decimalPlaces: 0,
            distanceUnit: DistanceUnit.NauticalMiles);
    }

    private ushort GetBearingInDegree(Coordinate position)
    {
        return (ushort)GeoCalculator.GetBearing(_groundZero, position);
    }
}