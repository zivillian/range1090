using System;
using range1090;
using range1090.SBS;

public class RangeCalculator(double latitude, double longitude)
{
    private readonly PolarRange _range = new (latitude, longitude);

    public int MessageCount { get; private set; }

    public int FlightCount => _flights.Count;

    private readonly Dictionary<string, Flight> _flights = new();

    public bool Add(SbsMessage message)
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

        if (!flight.Add(message)) return false;
        if (!flight.IsValid) return false;
        return _range.Add(flight.Position, flight.FlightLevel);
    }

    public Task SaveToFileAsync(string? filename, CancellationToken cancellationToken)
    {
        if (String.IsNullOrEmpty(filename)) return Task.CompletedTask;
        
        var data = _range.Serialize();
        return File.WriteAllBytesAsync(filename, data, cancellationToken);
    }

    public async Task LoadFileFileAsync(string? filename, CancellationToken cancellationToken)
    {
        if (String.IsNullOrEmpty(filename)) return;
        if (!File.Exists(filename)) return;

        var data = await File.ReadAllBytesAsync(filename, cancellationToken);
        _range.Deserialize(data);
    }

    public IEnumerable<FlightLevelArea> GetFlightLevels()
    {
        return _range.CreateFlightLevels();
    }
}