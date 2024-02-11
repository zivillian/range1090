using System.Text.Json;
using ColorHelper;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Converters;
using range1090;
using range1090.SBS;

public class RangeCalculator(double latitude, double longitude, string? geojson)
{
    private readonly string? _geojson = geojson;
    private readonly PolarRange _range = new (latitude, longitude);
    private readonly Coordinate _groundZero = new (Math.Round(longitude,4), Math.Round(latitude,4));
    private DateTime _lastExport = DateTime.MinValue;

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

    private async ValueTask ExportGeoJsonAsync(CancellationToken cancellationToken)
    {
        if (String.IsNullOrEmpty(_geojson)) return;
        if (DateTime.Now.AddSeconds(-10) < _lastExport) return;
        var factory = new GeometryFactory(new PrecisionModel(1000), 3857);
        //https://xkcd.com/2170/
        var epsilon = 0.0001;
        var collection = new FeatureCollection();
        var levels = _range.CreateFlightLevels();
        foreach (var flightLevel in levels.OrderByDescending(x=>x.FlightLevel))
        {
            var coordinates = new List<Coordinate>();
            for (int bearing = 0; bearing < 360; bearing++)
            {
                var segment = flightLevel.Positions.FirstOrDefault(x => x.BearingIndex == bearing);
                if (segment is null)
                {
                    if (coordinates.Count == 0 || !coordinates[^1].Equals(_groundZero))
                    {
                        coordinates.Add(_groundZero);
                    }
                }
                else
                {
                    if (coordinates.Count == 0 ||
                        Math.Abs(coordinates[^1].X - segment.MinBearing.Longitude) > epsilon ||
                        Math.Abs(coordinates[^1].Y - segment.MinBearing.Latitude) > epsilon)
                    {
                        coordinates.Add(new Coordinate(
                            Math.Round(segment.MinBearing.Longitude, 4),
                            Math.Round(segment.MinBearing.Latitude, 4)));
                    }

                    if (Math.Abs(coordinates[^1].X - segment.MaxBearing.Longitude) > epsilon ||
                        Math.Abs(coordinates[^1].Y - segment.MaxBearing.Latitude) > epsilon)
                    {
                        coordinates.Add(new Coordinate(
                            Math.Round(segment.MaxBearing.Longitude,4), 
                            Math.Round(segment.MaxBearing.Latitude,4)));
                    }
                }
            }
            if (!coordinates[0].Equals(coordinates[^1]))
            {
                coordinates.Add(coordinates[0]);
            }
            if (coordinates.Count < 4) continue;
            var polygon = factory.CreatePolygon(coordinates.ToArray());
            var attributes = new AttributesTable
            {
                { "level", flightLevel.FlightLevel },
                { "stroke-width", 0 },
                { "fill-opacity", 1 },
                { "fill", $"#{CalculateColor(flightLevel.FlightLevel*100).Value}" },
            };
            var feature = new Feature(polygon, attributes);
            collection.Add(feature);
        }

        var options = new JsonSerializerOptions
        {
            Converters = { new GeoJsonConverterFactory(factory) }
        };

        await using var file = File.Open(_geojson, FileMode.Truncate);

        await JsonSerializer.SerializeAsync(file, collection, options, cancellationToken);
        _lastExport = DateTime.Now;
    }

    private HEX CalculateColor(int altitude)
    {
        var hpoints = new[]
        {
            new { alt = 0, val = 20d }, // orange
            new { alt = 2000, val = 32.5 }, // yellow
            new { alt = 4000, val = 43d }, // yellow
            new { alt = 6000, val = 54d }, // yellow
            new { alt = 8000, val = 72d }, // yellow
            new { alt = 9000, val = 85d }, // green yellow
            new { alt = 11000, val = 140d }, // light green
            new { alt = 40000, val = 300d }, // magenta
            new { alt = 51000, val = 360d }, // red
        };
        var lpoints = new[]
        {
            new { h = 0, val = 53d },
            new { h = 20, val = 50d },
            new { h = 32, val = 54d },
            new { h = 40, val = 52d },
            new { h = 46, val = 51d },
            new { h = 50, val = 46d },
            new { h = 60, val = 43d },
            new { h = 80, val = 41d },
            new { h = 100, val = 41d },
            new { h = 120, val = 41d },
            new { h = 140, val = 41d },
            new { h = 160, val = 40d },
            new { h = 180, val = 40d },
            new { h = 190, val = 44d },
            new { h = 198, val = 50d },
            new { h = 200, val = 58d },
            new { h = 220, val = 58d },
            new { h = 240, val = 58d },
            new { h = 255, val = 55d },
            new { h = 266, val = 55d },
            new { h = 270, val = 58d },
            new { h = 280, val = 58d },
            new { h = 290, val = 47d },
            new { h = 300, val = 43d },
            new { h = 310, val = 48d },
            new { h = 320, val = 48d },
            new { h = 340, val = 52d },
            new { h = 360, val = 53d },
        };

        var h = hpoints[0].val;
        for (int i = hpoints.Length-1; i >=0; i--)
        {
            if (altitude > hpoints[i].alt)
            {
                if (i == hpoints.Length - 1)
                {
                    h = hpoints[i].val;
                }
                else
                {
                    h = hpoints[i].val + (hpoints[i + 1].val - hpoints[i].val) * (altitude - hpoints[i].alt) /
                        (hpoints[i + 1].alt - hpoints[i].alt);
                }

                break;
            }
        }

        var l = lpoints[0].val;
        for (int i = lpoints.Length - 1; i >= 0; --i)
        {
            if (h > lpoints[i].h)
            {
                if (i == lpoints.Length - 1)
                {
                    l = lpoints[i].val;
                }
                else
                {
                    l = lpoints[i].val + (lpoints[i + 1].val - lpoints[i].val) * (h - lpoints[i].h) /
                        (lpoints[i + 1].h - lpoints[i].h);
                }
                break;
            }
        }

        byte s = 88;

        if (h < 0)
        {
            h = (h % 360) + 360;
        }
        else if (h >= 360)
        {
            h = h % 360;
        }

        if (l < 0) l = 0;
        else if (l > 95) l = 95;
        return ColorHelper.ColorConverter.HslToHex(new HSL((int)h, s, (byte)l));
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