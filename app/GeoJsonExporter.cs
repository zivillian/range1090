using System.Text.Json;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Converters;
using NetTopologySuite.Precision;
using range1090;

public class GeoJsonExporter
{
    private readonly int _interval;
    private readonly Coordinate _groundZero;
    private DateTime _lastExport = DateTime.MinValue;
    private readonly GeometryFactory _factory;
    private readonly GeometryPrecisionReducer _reducer;
    private readonly JsonSerializerOptions _jsonOptions;

    public GeoJsonExporter(double latitude, double longitude, byte precision, int interval)
    {
        _interval -= interval;
        _groundZero = new(longitude, latitude);
        //https://xkcd.com/2170/
        _factory = new (new PrecisionModel(Math.Pow(10, precision)), 3857);
        _reducer = new(_factory.PrecisionModel);
        _jsonOptions = new JsonSerializerOptions
        {
            Converters = { new GeoJsonConverterFactory(_factory) }
        };
    }

    public async ValueTask ExportGeoJsonAsync(string? filename, IEnumerable<FlightLevelArea> levels, CancellationToken cancellationToken)
    {
        if (String.IsNullOrEmpty(filename)) return;
        if (DateTime.Now.AddSeconds(_interval) < _lastExport) return;
        var collection = new FeatureCollection();

        foreach (var flightLevel in levels.OrderByDescending(x => x.FlightLevel))
        {
            var coordinates = new List<Coordinate>(360 * 2 + 1);
            var bearings = flightLevel.Positions.ToDictionary(x => x.Bearing);
            for (ushort bearing = 0; bearing < 360; bearing++)
            {
                if (!bearings.TryGetValue(bearing, out var segment))
                {
                    coordinates.Add(_groundZero);
                }
                else
                {
                    coordinates.Add(new Coordinate(segment.Longitude, segment.Latitude));
                }
            }
            coordinates.Add(coordinates[0]);
            var polygon = _factory.CreatePolygon(coordinates.ToArray());
            var attributes = new AttributesTable
            {
                { "altitude", flightLevel.FlightLevel * 100 },
            };
            var feature = new Feature(_reducer.Reduce(polygon), attributes);
            if (!feature.Geometry.IsEmpty)
            {
                collection.Add(feature);
            }
        }

        await using var file = File.Open(filename, FileMode.Truncate);
        await JsonSerializer.SerializeAsync(file, collection, _jsonOptions, cancellationToken);
        _lastExport = DateTime.Now;
    }
}