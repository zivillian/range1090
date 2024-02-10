using range1090;
using range1090.SBS;

public class RangeCalculator
{
    private readonly double _latitude;
    private readonly double _longitude;

    public int MessageCount { get; private set; }

    public int FlightCount => _flights.Count;

    private readonly Dictionary<string, Flight> _flights = new Dictionary<string, Flight>();

    public RangeCalculator(double latitude, double longitude)
    {
        _latitude = latitude;
        _longitude = longitude;
    }

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
    }
}