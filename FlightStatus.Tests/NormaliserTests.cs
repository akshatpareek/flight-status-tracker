using FlightStatus.Api.Application.Normalisation;
using FlightStatus.Api.Domain.Enums;
using FlightStatus.Api.Infrastructure.Models;

namespace FlightStatus.Tests;

public class NormaliserTests
{
    private static readonly DateOnly TestDate = new(2024, 6, 10);

    [Fact]
    public void NormaliseAeroTrack_OnTime_ReturnsOnTime()
    {
        var response = new AeroTrackResponse(
            "BA123",
            "ON_TIME",
            DateTimeOffset.Parse("2024-06-10T10:00:00Z"),
            DateTimeOffset.Parse("2024-06-10T12:00:00Z"),
            null,
            null,
            "T1",
            "A1",
            null,
            DateTimeOffset.UtcNow);

        var result = FlightStatusNormaliser.NormaliseAeroTrack(response, TestDate);

        Assert.Equal(EnumFlightStatus.OnTime, result.Status);
    }

    [Fact]
    public void NormaliseAeroTrack_DelayedByActualDeparture_ReturnsDelayed()
    {
        var response = new AeroTrackResponse(
            "BA123",
            "ON_TIME",
            DateTimeOffset.Parse("2024-06-10T10:00:00Z"),
            DateTimeOffset.Parse("2024-06-10T12:00:00Z"),
            DateTimeOffset.Parse("2024-06-10T10:20:00Z"),
            null,
            "T1",
            "A1",
            "Weather",
            DateTimeOffset.UtcNow);

        var result = FlightStatusNormaliser.NormaliseAeroTrack(response, TestDate);

        Assert.Equal(EnumFlightStatus.Delayed, result.Status);
    }

    [Theory]
    [InlineData("CANCELLED", EnumFlightStatus.Cancelled)]
    [InlineData("DIVERTED", EnumFlightStatus.Diverted)]
    [InlineData("UNKNOWN_STATUS", EnumFlightStatus.Unknown)]
    public void NormaliseAeroTrack_StatusMapping(string rawStatus, EnumFlightStatus expected)
    {
        var response = new AeroTrackResponse(
            "BA123",
            rawStatus,
            DateTimeOffset.Parse("2024-06-10T10:00:00Z"),
            DateTimeOffset.Parse("2024-06-10T12:00:00Z"),
            null,
            null,
            null,
            null,
            null,
            DateTimeOffset.UtcNow);

        var result = FlightStatusNormaliser.NormaliseAeroTrack(response, TestDate);

        Assert.Equal(expected, result.Status);
    }

    [Theory]
    [InlineData("on-time", EnumFlightStatus.OnTime)]
    [InlineData("delayed", EnumFlightStatus.Delayed)]
    [InlineData("cancelled", EnumFlightStatus.Cancelled)]
    [InlineData("diverted", EnumFlightStatus.Diverted)]
    [InlineData("invalid", EnumFlightStatus.Unknown)]
    public void NormaliseQuickFlight_StatusMapping(string status, EnumFlightStatus expected)
    {
        var response = new QuickFlightResponse(
            "QF123",
            status,
            DateTimeOffset.Parse("2024-06-10T10:00:00Z"),
            DateTimeOffset.Parse("2024-06-10T12:00:00Z"),
            DateTimeOffset.UtcNow);

        var result = FlightStatusNormaliser.NormaliseQuickFlight(response, TestDate);

        Assert.Equal(expected, result.Status);
    }
}