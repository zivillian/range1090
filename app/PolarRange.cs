using System.Buffers.Binary;
using Geolocation;

namespace range1090;

public class PolarRange(double latitude, double longitude)
{
    private const int MAX_FLIGHT_LEVEL = 500;
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
            Console.WriteLine($"FL{flightLevel} {position.BearingIndex}° {position.Distance:F1}nm");
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
        const int recordSize = sizeof(ushort) + 2 * sizeof(double);
        var buffer = new byte[recordSize * validRanges.Count];
        var span = buffer.AsSpan();
        foreach (var area in validRanges)
        {
            BinaryPrimitives.WriteUInt16BigEndian(span, area.FlightLevel);
            span = span.Slice(sizeof(ushort));
            BinaryPrimitives.WriteDoubleBigEndian(span, area.Value.Latitude);
            span = span.Slice(sizeof(double));
            BinaryPrimitives.WriteDoubleBigEndian(span, area.Value.Longitude);
            span = span.Slice(sizeof(double));
        }

        return buffer;
    }

    public void Deserialize(byte[] data)
    {
        var span = data.AsSpan();
        while (!span.IsEmpty)
        {
            var level= BinaryPrimitives.ReadUInt16BigEndian(span);
            span = span.Slice(sizeof(ushort));
            latitude = BinaryPrimitives.ReadDoubleBigEndian(span);
            span = span.Slice(sizeof(double));
            longitude = BinaryPrimitives.ReadDoubleBigEndian(span);
            span = span.Slice(sizeof(double));

            var area = _ranges[level];
            if (area is null)
            {
                area = new FlightLevelArea(level);
                _ranges[level] = area;
            }

            area.Update(FlightLevelPosition.Create(_groundZero, new Coordinate(latitude, longitude)));
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