using FlightStatus.Api.Domain.Enums;
using FlightStatus.Api.Domain.Models;
using FlightStatus.Api.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlightStatus.Tests;

public class MergeServiceTests
{
    private readonly FlightMergeService _service = new(new NullLogger<FlightMergeService>());

    [Fact]
    public void Merge_BothNull_ReturnsUnknownWithDefaults()
    {
        var result = _service.Merge(new NormalisedFlightData?[] { null, null }, "XX999", new DateOnly(2024, 6, 10));
        Assert.Equal(EnumFlightStatus.Unknown, result.Status);
        Assert.Equal(DateTimeOffset.MinValue, result.ScheduledDeparture);
        Assert.Equal(DateTimeOffset.MinValue, result.ScheduledArrival);
        Assert.Equal("No data returned by any provider for this flight and date.", result.Message);
        Assert.Equal("XX999", result.FlightNumber);
        Assert.Equal(new DateOnly(2024, 6, 10), result.Date);
    }

    [Fact]
    public void Merge_OneNull_UsesNonNull()
    {
        var data = new NormalisedFlightData(
            "BA493",
            new DateOnly(2024, 6, 10),
            EnumFlightStatus.Delayed,
            DateTimeOffset.Parse("2024-06-10T10:00:00Z"),
            DateTimeOffset.Parse("2024-06-10T12:00:00Z"),
            null, null, null, null, null,
            DateTimeOffset.UtcNow
        );
        var result = _service.Merge(new NormalisedFlightData?[] { data, null }, "BA493", new DateOnly(2024, 6, 10));
        Assert.Equal(EnumFlightStatus.Delayed, result.Status);
        Assert.Null(result.Message);
        Assert.Equal("BA493", result.FlightNumber);
    }

    [Fact]
    public void Merge_BothPresent_PicksLatestLastUpdated()
    {
        var older = new NormalisedFlightData(
            "BA493",
            new DateOnly(2024, 6, 10),
            EnumFlightStatus.OnTime,
            DateTimeOffset.Parse("2024-06-10T10:00:00Z"),
            DateTimeOffset.Parse("2024-06-10T12:00:00Z"),
            null, null, null, null, null,
            DateTimeOffset.UtcNow.AddMinutes(-10)
        );
        var newer = new NormalisedFlightData(
            "BA493",
            new DateOnly(2024, 6, 10),
            EnumFlightStatus.Delayed,
            DateTimeOffset.Parse("2024-06-10T10:00:00Z"),
            DateTimeOffset.Parse("2024-06-10T12:00:00Z"),
            null, null, null, null, null,
            DateTimeOffset.UtcNow
        );
        var result = _service.Merge(new NormalisedFlightData?[] { older, newer }, "BA493", new DateOnly(2024, 6, 10));
        Assert.Equal(EnumFlightStatus.Delayed, result.Status);
        Assert.Null(result.Message);
    }
}
