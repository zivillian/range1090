﻿using System.Globalization;

namespace range1090.SBS;

public class SbsMessage
{
    //http://woodair.net/sbs/article/barebones42_socket_data.htm
    public SbsMessage(ReadOnlySpan<char> message)
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
        Span<Range> ranges = stackalloc Range[22];
        var fields = message.Split(ranges, ',');

        SessionId = message[ranges[2]].ToString();
        AircraftId = message[ranges[3]].ToString();
        HexIdent = message[ranges[4]].ToString();
        FlightId = message[ranges[5]].ToString();
        if (fields < 10) throw new NotImplementedException(message.ToString());
        Generated = new DateTimeOffset(
            DateOnly.ParseExact(message[ranges[6]], "yyyy/MM/dd"),
            TimeOnly.ParseExact(message[ranges[7]], "HH':'mm':'ss'.'fff"),
            TimeSpan.Zero);
        Logged = new DateTimeOffset(
            DateOnly.ParseExact(message[ranges[8]], "yyyy/MM/dd"),
            TimeOnly.ParseExact(message[ranges[9]], "HH':'mm':'ss'.'fff"),
            TimeSpan.Zero);
        if (fields < 22) throw new NotImplementedException(message.ToString());
        if (message[ranges[10]].Length > 0)
        {
            HasCallSign = true;
            CallSign = message[ranges[10]].ToString();
        }

        if (message[ranges[11]].Length > 0)
        {
            Altitude = Int32.Parse(message[ranges[11]]);
        }

        if (message[ranges[12]].Length > 0)
        {
            GroundSpeed = Int32.Parse(message[ranges[12]]);
        }

        if (message[ranges[13]].Length > 0)
        {
            Track = Int32.Parse(message[ranges[13]]);
        }

        if (message[ranges[14]].Length > 0)
        {
            Latitude = Double.Parse(message[ranges[14]], CultureInfo.InvariantCulture);
        }

        if (message[ranges[15]].Length > 0)
        {
            Longitude = Double.Parse(message[ranges[15]], CultureInfo.InvariantCulture);
        }

        if (message[ranges[16]].Length > 0)
        {
            VerticalRate = Int32.Parse(message[ranges[16]]);
        }

        if (message[ranges[17]].Length > 0)
        {
            Squawk = message[ranges[17]].ToString();
        }

        if (message[ranges[18]].Length > 0)
        {
            Alert = message[ranges[18]] == "-1";
        }

        if (message[ranges[19]].Length > 0)
        {
            Emergency = message[ranges[19]] == "-1";
        }

        if (message[ranges[20]].Length > 0)
        {
            SPI = message[ranges[20]] == "-1";
        }

        if (message[ranges[21]].Length > 0)
        {
            IsOnGround = message[ranges[21]] == "-1";
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

    public string? CallSign { get; }

    public int? Altitude { get; }

    public int? GroundSpeed { get; }
    
    public int? Track { get; }

    public double? Latitude { get; }

    public double? Longitude { get; }

    public int? VerticalRate { get; }

    public string? Squawk { get; }

    public bool? Alert { get; }

    public bool? Emergency { get; }

    public bool? SPI { get; }

    public bool? IsOnGround { get; }

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
            Latitude?.ToString(CultureInfo.InvariantCulture), Longitude?.ToString(CultureInfo.InvariantCulture),
            VerticalRate, Squawk, Alert, Emergency, SPI, IsOnGround);
    }
}