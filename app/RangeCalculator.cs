using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using Geolocation;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Converters;
using NetTopologySuite.Operation.Overlay;
using range1090;
using range1090.SBS;
using Coordinate = NetTopologySuite.Geometries.Coordinate;

public class RangeCalculator(double latitude, double longitude, string? geojson)
{
    private readonly string? _geojson = geojson;
    private readonly PolarRange _range = new (latitude, longitude);

    public int MessageCount { get; private set; }

    public int FlightCount => _flights.Count;

    private readonly Dictionary<string, Flight> _flights = new();

    public async ValueTask AddAsync(SbsMessage message, CancellationToken cancellationToken)
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
        if (_range.Add(flight.Position, flight.FlightLevel))
        {
            await ExportGeoJsonAsync(cancellationToken);
        }
    }

    private Task ExportGeoJsonAsync(CancellationToken cancellationToken)
    {
        if (String.IsNullOrEmpty(_geojson)) return Task.CompletedTask;

        var factory = new GeometryFactory(new PrecisionModel(), 3857);

        var collection = new FeatureCollection();
        var levels = _range.CreateFlightLevels();
        foreach (var flightLevel in levels)
        {
            var coordinates = new List<Coordinate>();
            for (int bearing = 0; bearing < 360; bearing++)
            {
                //flightLevel.Positions
            }
        }
        return Task.CompletedTask;

        //var options = new JsonSerializerOptions
        //{
        //    Converters = { new GeoJsonConverterFactory() }
        //};
        //var geoJson = JsonSerializer.Serialize(collection, options);

        //return File.WriteAllTextAsync(_geojson, geoJson, cancellationToken);
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
        await ExportGeoJsonAsync(cancellationToken);
    }
}