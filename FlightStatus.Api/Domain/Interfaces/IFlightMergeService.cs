using FlightStatus.Api.Contracts;
using FlightStatus.Api.Domain.Models;

namespace FlightStatus.Api.Domain.Interfaces;

public interface IFlightMergeService
{
    FlightStatusResponse Merge(
        IEnumerable<NormalisedFlightData?> providerResults,
        string flightNumber,
        DateOnly date);
}