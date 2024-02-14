using System.Collections;
using System.Numerics;
using System.Text.Json;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Converters;
using range1090;

public class GeoJsonExporter
{
    private readonly int _interval;
    private readonly Coordinate _groundZero;
    private DateTime _lastExport = DateTime.MinValue;
    private readonly GeometryFactory _factory;
    private readonly PrecisionModel _precision;
    private readonly JsonSerializerOptions _jsonOptions;

    public GeoJsonExporter(double latitude, double longitude, byte precision, int interval)
    {
        _interval -= interval;
        _groundZero = new(longitude, latitude);
        //https://xkcd.com/2170/
        _precision = new PrecisionModel(Math.Pow(10, precision));
        _factory = new (_precision, 3857);
        _precision.MakePrecise(_groundZero);
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

        var coordinates = new List<Coordinate>(360 + 1);
        var covered = new BigInteger();
        var one = BigInteger.One;
        foreach (var flightLevel in levels.OrderByDescending(x => x.FlightLevel))
        {
            var maxRange = 0d;
            covered = 0;
            coordinates.Clear();
            var bearings = flightLevel.Positions.ToDictionary(x => x.Bearing);
            var lastWasNull = false;
            var secondToLastWasNull = false;
            for (ushort bearing = 0; bearing < 360; bearing++)
            {
                if (!bearings.TryGetValue(bearing, out var segment))
                {
                    if (lastWasNull) continue;
                    if (secondToLastWasNull)
                    {
                        //remove previous single track point
                        coordinates.RemoveAt(coordinates.Count - 1);
                    }
                    else
                    {
                        coordinates.Add(_groundZero);
                    }
                    secondToLastWasNull = false;
                    lastWasNull = true;
                }
                else
                {
                    covered |= (one << bearing);
                    maxRange = Math.Max(maxRange, segment.Distance);
                    secondToLastWasNull = lastWasNull;
                    lastWasNull = false;
                    coordinates.Add(new Coordinate(segment.Longitude, segment.Latitude));
                }
            }

            if (!coordinates[0].Equals(coordinates[^1]))
            {
                //close the shape
                coordinates.Add(coordinates[0]);
            }
            if (coordinates.Count <= 3) continue;

            //https://github.com/NetTopologySuite/NetTopologySuite.IO.GeoJSON/issues/135
            coordinates.ForEach(_precision.MakePrecise);
            var polygon = _factory.CreatePolygon(new OptimizedCoordinateSequence(coordinates));
            var attributes = new AttributesTable
            {
                { "altitude", flightLevel.FlightLevel * 100 },
                { "range", maxRange },
                { "degrees", $"0x{covered:X}" }
            };
            var feature = new Feature(polygon, attributes);
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