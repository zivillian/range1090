using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
using Geolocation;

namespace range1090;

public class PolarRange(double latitude, double longitude, bool verbose)
{
    private const int MAX_FLIGHT_LEVEL = 500;
    private const string MAGIC = "range1090\n";
    private readonly FlightLevelArea?[] _ranges = new FlightLevelArea[MAX_FLIGHT_LEVEL];
    private readonly Coordinate _groundZero = new(latitude, longitude);
    private bool _verbose = verbose;

    public bool Add(Coordinate location, ushort flightLevel)
    {
        if (flightLevel >= MAX_FLIGHT_LEVEL) flightLevel = MAX_FLIGHT_LEVEL-1;
        var area = _ranges[flightLevel];
        if (area is null)
        {
            area = new FlightLevelArea(flightLevel);
            _ranges[flightLevel] = area;
        }
        if (UpdateArea(area, location))
        {
            return true;
        }
        return false;
    }

    public void DumpStats()
    {
        var totalPercentage = _ranges.Where(x => x is not null)
        .Sum(x => x!.CoveragePercentage) / _ranges.Length;
        Console.WriteLine($"Total coverage:{totalPercentage,7:F3}%");
    }

    public byte[] Serialize()
    {
        var validRanges = _ranges
            .Select((x,i)=>new{FlightLevel = i, Value = x})
            .Where(x => x.Value is not null)
            .SelectMany(x => x.Value!.Positions
                .Select(y => new
                {
                    FlightLevel = (ushort)x.FlightLevel,
                    Value = y
                }))
            .Where(x => x.Value.IsValid)
            .ToList();
        const int recordSize = sizeof(ushort) + 2 * sizeof(double);

        var buffer = new byte[MAGIC.Length + (recordSize * validRanges.Count)];
        var span = buffer.AsSpan();
        Encoding.ASCII.GetBytes(MAGIC).CopyTo(span);
        span = span.Slice(MAGIC.Length);
        foreach (var area in validRanges)
        {
            BinaryPrimitives.WriteUInt16BigEndian(span, area.FlightLevel);
            span = span.Slice(sizeof(ushort));
            BinaryPrimitives.WriteDoubleBigEndian(span, area.Value.Latitude);
            span = span.Slice(sizeof(double));
            BinaryPrimitives.WriteDoubleBigEndian(span, area.Value.Longitude);
            span = span.Slice(sizeof(double));
        }
        Debug.Assert(span.IsEmpty, "Too large buffer allocated");
        return buffer;
    }

    public void Deserialize(byte[] data)
    {
        bool verbose = _verbose;
        _verbose = false;
        var span = data.AsSpan();
        if (span.Length < MAGIC.Length) return;
        var magic = Encoding.ASCII.GetBytes(MAGIC);
        if (!span.StartsWith(magic)) return;
        span = span.Slice(magic.Length);
        while (!span.IsEmpty)
        {
            var level= BinaryPrimitives.ReadUInt16BigEndian(span);
            span = span.Slice(sizeof(ushort));
            var lat = BinaryPrimitives.ReadDoubleBigEndian(span);
            span = span.Slice(sizeof(double));
            var lon = BinaryPrimitives.ReadDoubleBigEndian(span);
            span = span.Slice(sizeof(double));

            var area = _ranges[level];
            if (area is null)
            {
                area = new FlightLevelArea(level);
                _ranges[level] = area;
            }
            UpdateArea(area, new Coordinate(lat, lon));
        }
        _verbose = verbose;
    }

    public IEnumerable<FlightLevelArea> CreateFlightLevels()
    {
        foreach (var area in _ranges)
        {
            if (area is null) continue;
            if (!area.IsValid) continue;
            yield return area;
        }
    }

    private bool UpdateArea(FlightLevelArea area, Coordinate position)
    {
        var distance = GeoCalculator.GetDistance(_groundZero, position, decimalPlaces: 6, DistanceUnit.NauticalMiles);
        var bearing = (ushort)Math.Floor(GeoCalculator.GetBearing(_groundZero, position));
        if (area.Update(position.Latitude, position.Longitude, bearing, distance))
        {
            if (_verbose)
            {
                var totalPercentage = _ranges.Where(x => x is not null)
                    .Sum(x => x!.CoveragePercentage) / _ranges.Length;
                Console.WriteLine($"FL{area.FlightLevel:D3} (Covered:{area.CoveragePercentage,5:F1}%) Bearing:{bearing,3}° Distance:{distance,5:F1}nm Total coverage:{totalPercentage,7:F3}%");
            }
            return true;
        }
        return false;
    }
}