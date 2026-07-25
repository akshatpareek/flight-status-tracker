using FlightStatus.Api.Domain.Models;

namespace FlightStatus.Api.Domain.Interfaces;

public interface IFlightStatusProvider
{
    Task<NormalisedFlightData?> GetStatusAsync(
        string flightNumber,
        DateOnly date,
        CancellationToken cancellationToken = default);
}
