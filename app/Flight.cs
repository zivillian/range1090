using System.Diagnostics.CodeAnalysis;
using range1090.SBS;

namespace range1090;

public class Flight(SbsMessage message)
{
    public string HexIdent { get; } = message.HexIdent;
    private readonly DateTimeOffset _firstMessageTimestamp = message.Generated;
    private SbsMessage? _lastAltitude;
    private SbsMessage? _lastPosition;

    public DateTimeOffset LastMessage
    {
        get
        {
            if (_lastAltitude is null)
            {
                if (_lastPosition is null)
                {
                    return _firstMessageTimestamp;
                }

                return _lastPosition.Generated;
            }

            if (_lastPosition is null)
            {
                return _lastAltitude.Generated;
            }

            if (_lastAltitude.Generated > _lastPosition.Generated)
            {
                return _lastAltitude.Generated;
            }

            return _lastPosition.Generated;
        }
    }

    [MemberNotNullWhen(true, nameof(_lastAltitude), nameof(_lastPosition))]
    public bool IsValid
    {
        get
        {
            if (_lastAltitude is null) return false;
            if (_lastPosition is null) return false;
            var delta = (_lastAltitude.Generated - _lastPosition.Generated);
            if (Math.Abs(delta.TotalSeconds) > 10) return false;
            return true;
        }
    }

    public ushort FlightLevel
    {
        get
        {
            if (!IsValid) return 0;
            return (ushort)(_lastAltitude.Altitude / 100);
        }
    }

    public decimal Longitude
    {
        get
        {
            if (!IsValid) return -1;
            return _lastPosition.Longitude;
        }
    }

    public decimal Latitude
    {
        get
        {
            if (!IsValid) return -1;
            return _lastPosition.Latitude;
        }
    }

    public bool Add(SbsMessage message)
    {
        var result = false;
        if (message.HasAltitude)
        {
            _lastAltitude = message;
            result = true;
        }

        if (message.HasLongitude && message.HasLatitude)
        {
            _lastPosition = message;
            result = true;
        }
        return result;
    }
}