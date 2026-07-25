using FlightStatus.Api.Common;
using FlightStatus.Api.Contracts;
using FlightStatus.Api.Domain.Enums;
using FlightStatus.Api.Domain.Exceptions;
using FlightStatus.Api.Domain.Interfaces;
using FlightStatus.Api.Domain.Models;

namespace FlightStatus.Api.Infrastructure.Services;

public class FlightMergeService : IFlightMergeService
{
    private readonly ILogger<FlightMergeService> _logger;

    public FlightMergeService(ILogger<FlightMergeService> logger)
    {
        _logger = logger;
    }

    public FlightStatusResponse Merge(
        IEnumerable<NormalisedFlightData?> providerResults,
        string flightNumber,
        DateOnly flightDate)
    {
        try
        {
            List<NormalisedFlightData> validResults =
                providerResults?.OfType<NormalisedFlightData>().ToList()
                ?? [];

            if (validResults.Count == 0)
                return BuildNoDataResponse(flightNumber, flightDate);

            NormalisedFlightData winningResult = validResults.MaxBy(result => result.LastUpdatedUtc)!;

            _logger.LogInformation(
                AppConstants.LogMessages.MergeWinnerSelected,
                winningResult.FlightNumber,
                winningResult.LastUpdatedUtc);

            return BuildSuccessResponse(winningResult);
        }
        catch (FlightStatusException)
        {
            throw;
        }
        catch (Exception unexpectedException)
        {
            _logger.LogError(
                unexpectedException,
                AppConstants.LogMessages.MergeUnexpectedError,
                flightNumber, flightDate);

            throw new FlightStatusException(
                AppConstants.ErrorMessages.MergeServiceFailed(flightNumber),
                unexpectedException);
        }
    }

    private FlightStatusResponse BuildNoDataResponse(string flightNumber, DateOnly flightDate)
    {
        _logger.LogWarning(
            AppConstants.LogMessages.MergeNoResults,
            flightNumber, flightDate);

        return new FlightStatusResponse(
            FlightNumber:       flightNumber,
            Date:               flightDate,
            Status:             EnumFlightStatus.Unknown,
            ScheduledDeparture: DateTimeOffset.MinValue,
            ScheduledArrival:   DateTimeOffset.MinValue,
            ActualDeparture:    null,
            ActualArrival:      null,
            Terminal:           null,
            Gate:               null,
            DelayReason:        null,
            LastUpdatedUtc:     DateTimeOffset.UtcNow,
            Message:            AppConstants.ErrorMessages.NoProviderDataFound);
    }

    private static FlightStatusResponse BuildSuccessResponse(NormalisedFlightData winningResult) =>
        new(
            FlightNumber:       winningResult.FlightNumber,
            Date:               winningResult.Date,
            Status:             winningResult.Status,
            ScheduledDeparture: winningResult.ScheduledDeparture,
            ScheduledArrival:   winningResult.ScheduledArrival,
            ActualDeparture:    winningResult.ActualDeparture,
            ActualArrival:      winningResult.ActualArrival,
            Terminal:           winningResult.Terminal,
            Gate:               winningResult.Gate,
            DelayReason:        winningResult.DelayReason,
            LastUpdatedUtc:     winningResult.LastUpdatedUtc,
            Message:            null);
}
