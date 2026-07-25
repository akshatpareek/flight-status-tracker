using FlightStatus.Api.Domain.Enums;

namespace FlightStatus.Api.Domain.Models;

public record NormalisedFlightData(
    string FlightNumber,
    DateOnly Date,
    EnumFlightStatus Status,
    DateTimeOffset ScheduledDeparture,
    DateTimeOffset ScheduledArrival,
    DateTimeOffset? ActualDeparture,
    DateTimeOffset? ActualArrival,
    string? Terminal,
    string? Gate,
    string? DelayReason,
    DateTimeOffset LastUpdatedUtc);
