namespace FlightStatus.Api.Infrastructure.Models;

public record QuickFlightResponse(
    string FlightCode,
    string Status,
    DateTimeOffset ScheduledDep,
    DateTimeOffset ScheduledArr,
    DateTimeOffset LastUpdatedUtc);
