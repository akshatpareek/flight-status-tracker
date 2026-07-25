namespace FlightStatus.Api.Infrastructure.Models;

public record AeroTrackResponse(
    string FlightNumber,
    string RawStatus,
    DateTimeOffset ScheduledDeparture,
    DateTimeOffset ScheduledArrival,
    DateTimeOffset? ActualDeparture,
    DateTimeOffset? ActualArrival,
    string? Terminal,
    string? Gate,
    string? DelayReason,
    DateTimeOffset LastUpdatedUtc);
