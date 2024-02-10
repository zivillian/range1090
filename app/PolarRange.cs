using System.Buffers.Binary;

public class PolarRange
{
    private const int MAX_FLIGHT_LEVEL = 500;
    private const int DEGREES_360 = 360;
    private readonly ushort[,] _ranges = new ushort[DEGREES_360, MAX_FLIGHT_LEVEL];

    public void Add(ushort bearing, ushort flightLevel, ushort distance)
    {
        if (flightLevel > MAX_FLIGHT_LEVEL) return;
        if (_ranges[bearing, flightLevel] < distance)
        {
            _ranges[bearing, flightLevel] = distance;
        }
    }

    public ushort MaxDistance(ushort bearing)
    {
        ushort distance = 0;
        for (int i = 0; i < MAX_FLIGHT_LEVEL; i++)
        {
            var value = _ranges[bearing, i];
            if (value > distance)
            {
                distance = value;
            }
        }

        return distance;
    }

    public byte[] Serialize()
    {
        var buffer = new byte[2 * DEGREES_360 * MAX_FLIGHT_LEVEL];
        var span = buffer.AsSpan();
        foreach (var value in _ranges)
        {
            BinaryPrimitives.WriteUInt16BigEndian(span, value);
            span = span.Slice(2);
        }
        return buffer;
    }

    public void Deserialize(byte[] data)
    {
        var span = data.AsSpan();
        for (int i = 0; i < DEGREES_360; i++)
        {
            for (int j = 0; j < MAX_FLIGHT_LEVEL; j++)
            {
                _ranges[i,j] = BinaryPrimitives.ReadUInt16BigEndian(span);
                if (_ranges[i, j] != 0)
                {

                }
                span = span.Slice(2);
            }
        }
    }
}