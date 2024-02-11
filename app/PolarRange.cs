using System.Buffers.Binary;
using System.Text;
using Geolocation;

namespace range1090;

public class PolarRange(double latitude, double longitude)
{
    private const int MAX_FLIGHT_LEVEL = 500;
    private const string MAGIC = "range1090\n";
    private readonly FlightLevelArea?[] _ranges = new FlightLevelArea[MAX_FLIGHT_LEVEL];
    private readonly Coordinate _groundZero = new(latitude, longitude);

    public bool Add(Coordinate location, ushort flightLevel)
    {
        if (flightLevel > MAX_FLIGHT_LEVEL) return false;
        var position = FlightLevelPosition.Create(_groundZero, location);
        var area = _ranges[flightLevel];
        if (area is null)
        {
            area = new FlightLevelArea(flightLevel);
            _ranges[flightLevel] = area;
        }
        if (area.Update(position))
        {
            var totalPercentage = _ranges.Where(x=>x is not null).Sum(x => x!.CoveragePercentage) / _ranges.Length;
            var areaCoverage = _ranges.Where(x => x is not null)
                .SelectMany(x => x!.Positions)
                .DistinctBy(x => x.BearingIndex)
                .Count() / 360d * 100;
            Console.WriteLine($"FL{flightLevel:D3}({area.CoveragePercentage,5:F1}%) {position.BearingIndex,3}° {position.Distance,5:F1}nm {totalPercentage,7:F3}% {areaCoverage,5:F1}%");
            return true;
        }
        return false;
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
        const int recordSize = sizeof(ushort) + 4 * sizeof(double);

        var buffer = new byte[MAGIC.Length + (recordSize * validRanges.Count)];
        var span = buffer.AsSpan();
        Encoding.ASCII.GetBytes(MAGIC).CopyTo(span);
        span = span.Slice(MAGIC.Length);
        foreach (var area in validRanges)
        {
            BinaryPrimitives.WriteUInt16BigEndian(span, area.FlightLevel);
            span = span.Slice(sizeof(ushort));
            BinaryPrimitives.WriteDoubleBigEndian(span, area.Value.MinBearing.Latitude);
            span = span.Slice(sizeof(double));
            BinaryPrimitives.WriteDoubleBigEndian(span, area.Value.MinBearing.Longitude);
            span = span.Slice(sizeof(double));
            BinaryPrimitives.WriteDoubleBigEndian(span, area.Value.MaxBearing.Latitude);
            span = span.Slice(sizeof(double));
            BinaryPrimitives.WriteDoubleBigEndian(span, area.Value.MaxBearing.Longitude);
            span = span.Slice(sizeof(double));
        }

        return buffer;
    }

    public void Deserialize(byte[] data)
    {
        var span = data.AsSpan();
        if (span.Length < MAGIC.Length) return;
        var magic = Encoding.ASCII.GetBytes(MAGIC);
        if (!span.StartsWith(magic)) return;
        span = span.Slice(magic.Length);
        while (!span.IsEmpty)
        {
            var level= BinaryPrimitives.ReadUInt16BigEndian(span);
            span = span.Slice(sizeof(ushort));
            var minLat = BinaryPrimitives.ReadDoubleBigEndian(span);
            span = span.Slice(sizeof(double));
            var minLon = BinaryPrimitives.ReadDoubleBigEndian(span);
            span = span.Slice(sizeof(double));
            var maxLat = BinaryPrimitives.ReadDoubleBigEndian(span);
            span = span.Slice(sizeof(double));
            var maxLon = BinaryPrimitives.ReadDoubleBigEndian(span);
            span = span.Slice(sizeof(double));

            var area = _ranges[level];
            if (area is null)
            {
                area = new FlightLevelArea(level);
                _ranges[level] = area;
            }

            area.Update(FlightLevelPosition.Create(_groundZero, new Coordinate(minLat, minLon)));
            area.Update(FlightLevelPosition.Create(_groundZero, new Coordinate(maxLat, maxLon)));
        }
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
}