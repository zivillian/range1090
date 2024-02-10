using System.Diagnostics;
using System.Globalization;

namespace range1090.SBS;

public class SbsMessage
{
    //http://woodair.net/sbs/article/barebones42_socket_data.htm
    public SbsMessage(string message)
    {
        Type = message[0] switch
        {
            'S' when message[1] == 'E' => SbsMessageType.SelectionChange,
            'I' => SbsMessageType.NewId,
            'A' => SbsMessageType.NewAircraft,
            'S' when message[1] == 'T' => SbsMessageType.StatusChange,
            'C' => SbsMessageType.Click,
            'M' => SbsMessageType.Transmission,
            _ => throw new ArgumentOutOfRangeException("invalid SBS message type")
        };
        if (Type != SbsMessageType.Transmission)
        {
            throw new NotImplementedException("only SBS message type 'MSG' is implemented");
        }

        TransmissionType = message[4] switch
        {
            '1' => SbsTransmissionMessageType.EsIdentificationAndCategory,
            '2' => SbsTransmissionMessageType.EsSurfacePositionMessage,
            '3' => SbsTransmissionMessageType.EsAirbornePositionMessage,
            '4' => SbsTransmissionMessageType.EsAirborneVelocityMessage,
            '5' => SbsTransmissionMessageType.SurveillanceAltMessage,
            '6' => SbsTransmissionMessageType.SurveillanceIdMessage,
            '7' => SbsTransmissionMessageType.AirToAirMessage,
            '8' => SbsTransmissionMessageType.AllCallReply,
            _ => throw new ArgumentOutOfRangeException("invalid SBS transmission message type")
        };
        var fields = message.Split(',');

        SessionId = fields[2];
        AircraftId = fields[3];
        HexIdent = fields[4];
        FlightId = fields[5];
        Generated = new DateTimeOffset(
            DateOnly.ParseExact(fields[6], "yyyy/MM/dd", null),
            TimeOnly.ParseExact(fields[7], @"HH':'mm':'ss'.'fff", null),
            TimeSpan.Zero);
        Logged = new DateTimeOffset(
            DateOnly.ParseExact(fields[8], "yyyy/MM/dd", null),
            TimeOnly.ParseExact(fields[9], @"HH':'mm':'ss'.'fff", null),
            TimeSpan.Zero);
        if (fields[10].Length > 0)
        {
            HasCallSign = true;
            CallSign = fields[10];
        }

        if (fields[11].Length > 0)
        {
            HasAltitude = true;
            Altitude = int.Parse(fields[11]);
        }

        if (fields[12].Length > 0)
        {
            HasGroundSpeed = true;
            GroundSpeed = int.Parse(fields[12]);
        }

        if (fields[13].Length > 0)
        {
            HasTrack = true;
            Track = int.Parse(fields[13]);
        }

        if (fields[14].Length > 0)
        {
            HasLatitude = true;
            Latitude = Double.Parse(fields[14], CultureInfo.InvariantCulture);
        }

        if (fields[15].Length > 0)
        {
            HasLongitude = true;
            Longitude = Double.Parse(fields[15], CultureInfo.InvariantCulture);
        }

        if (fields[16].Length > 0)
        {
            HasVerticalRate = true;
            VerticalRate = int.Parse(fields[16]);
        }

        if (fields[17].Length > 0)
        {
            HasSquawk = true;
            Squawk = fields[17];
        }

        if (fields[18].Length > 0)
        {
            HasAlert = true;
            Alert = fields[18] == "-1";
        }

        if (fields[19].Length > 0)
        {
            HasEmergency = true;
            Emergency = fields[19] == "-1";
        }

        if (fields[20].Length > 0)
        {
            HasSPI = true;
            SPI = fields[20] == "-1";
        }

        if (fields[21].Length > 0)
        {
            HasIsOnGround = true;
            IsOnGround = fields[21] == "-1";
        }
    }

    public SbsMessageType Type { get; }

    public SbsTransmissionMessageType TransmissionType { get; }

    public string SessionId { get; }

    public string AircraftId { get; }

    public string HexIdent { get; }

    public string FlightId { get; }

    public DateTimeOffset Generated { get; }

    public DateTimeOffset Logged { get; }

    public bool HasCallSign { get; }

    public string CallSign { get; }

    public bool HasAltitude { get; }

    public int Altitude { get; }

    public bool HasGroundSpeed { get; }

    public int GroundSpeed { get; }

    public bool HasTrack { get; }

    public int Track { get; }

    public bool HasLatitude { get; }

    public double Latitude { get; }

    public bool HasLongitude { get; }

    public double Longitude { get; }

    public bool HasVerticalRate { get; }

    public int VerticalRate { get; }

    public bool HasSquawk { get; }

    public string Squawk { get; }

    public bool HasAlert { get; }

    public bool Alert { get; }

    public bool HasEmergency { get; }

    public bool Emergency { get; }

    public bool HasSPI { get; }

    public bool SPI { get; }

    public bool HasIsOnGround { get; }

    public bool IsOnGround { get; }

    public override string ToString()
    {
        var msg = Type switch
        {
            SbsMessageType.SelectionChange => "SEL",
            SbsMessageType.NewId => "ID",
            SbsMessageType.NewAircraft => "AIR",
            SbsMessageType.StatusChange => "STA",
            SbsMessageType.Click => "CLK",
            SbsMessageType.Transmission => "MSG",
            _ => throw new ArgumentOutOfRangeException("Type")
        };

        return String.Join(",", msg, (int)TransmissionType, SessionId, AircraftId, HexIdent, FlightId,
            Generated.ToUniversalTime().ToString("O"), Logged.ToString("O"), CallSign, Altitude, GroundSpeed, Track,
            Latitude.ToString(CultureInfo.InvariantCulture), Longitude.ToString(CultureInfo.InvariantCulture),
            VerticalRate, Squawk, Alert, Emergency, SPI, IsOnGround);
    }
}