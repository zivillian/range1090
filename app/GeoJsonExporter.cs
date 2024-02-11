﻿using System.Text.Json;
using ColorHelper;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Converters;
using NetTopologySuite.Precision;
using range1090;

public class GeoJsonExporter(double latitude, double longitude)
{
    private readonly Coordinate _groundZero = new(longitude, latitude);
    private DateTime _lastExport = DateTime.MinValue;

    public async ValueTask ExportGeoJsonAsync(string? filename, IEnumerable<FlightLevelArea> levels, CancellationToken cancellationToken)
    {
        if (String.IsNullOrEmpty(filename)) return;
        if (DateTime.Now.AddSeconds(-10) < _lastExport) return;
        //https://xkcd.com/2170/
        var factory = new GeometryFactory(new PrecisionModel(100), 3857);
        var reducer = new GeometryPrecisionReducer(factory.PrecisionModel);
        var collection = new FeatureCollection();

        foreach (var flightLevel in levels.OrderByDescending(x => x.FlightLevel))
        {
            var coordinates = new List<Coordinate>();
            for (int bearing = 0; bearing < 360; bearing++)
            {
                var segment = flightLevel.Positions.FirstOrDefault(x => x.BearingIndex == bearing);
                if (segment is null)
                {
                    coordinates.Add(_groundZero);
                }
                else
                {
                    coordinates.Add(new Coordinate(segment.MinBearing.Longitude, segment.MinBearing.Latitude));
                    coordinates.Add(new Coordinate(segment.MaxBearing.Longitude, segment.MaxBearing.Latitude));
                }
            }
            coordinates.Add(coordinates[0]);
            var polygon = factory.CreatePolygon(coordinates.ToArray());
            var attributes = new AttributesTable
            {
                { "level", flightLevel.FlightLevel },
                { "stroke-width", 0 },
                { "fill-opacity", 1 },
                { "fill", $"#{CalculateColor(flightLevel.FlightLevel*100).Value}" },
            };
            var feature = new Feature(reducer.Reduce(polygon), attributes);
            if (!feature.Geometry.IsEmpty)
            {
                collection.Add(feature);
            }
        }

        var options = new JsonSerializerOptions
        {
            Converters = { new GeoJsonConverterFactory(factory) }
        };

        await using var file = File.Open(filename, FileMode.Truncate);

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
        for (int i = hpoints.Length - 1; i >= 0; i--)
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

}