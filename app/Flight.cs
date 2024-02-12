using System.Diagnostics.CodeAnalysis;
using Geolocation;
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
            return (ushort)Math.Round(_lastAltitude.Altitude!.Value / 100d, 0);
        }
    }

    public Coordinate Position
    {
        get
        {
            if (!IsValid) return default;
            return new Coordinate(_lastPosition.Latitude!.Value, _lastPosition.Longitude!.Value);
        }
    }

    public bool Add(SbsMessage message)
    {
        var result = false;
        if (message.Altitude.HasValue)
        {
            _lastAltitude = message;
            result = true;
        }

        if (message.Longitude.HasValue && message.Latitude.HasValue)
        {
            _lastPosition = message;
            result = true;
        }
        return result;
    }
}