using FlightStatus.Api.Application.Normalisation;
using FlightStatus.Api.Common;
using FlightStatus.Api.Domain.Exceptions;
using FlightStatus.Api.Domain.Interfaces;
using FlightStatus.Api.Domain.Models;
using FlightStatus.Api.Infrastructure.Helpers;
using FlightStatus.Api.Infrastructure.Models;
using System.Text.Json;

namespace FlightStatus.Api.Infrastructure.Providers;

public class QuickFlightProvider : IFlightStatusProvider
{
    private readonly Dictionary<string, NormalisedFlightData> _flightsByNumber;
    private readonly ILogger<QuickFlightProvider> _logger;

    public QuickFlightProvider(ILogger<QuickFlightProvider> logger)
    {
        _logger = logger;
        _flightsByNumber = LoadFlightStubs();
    }

    public Task<NormalisedFlightData?> GetStatusAsync(
        string flightNumber,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            string normalizedFlightNumber = flightNumber.ToUpperInvariant();

            bool flightFound = _flightsByNumber.TryGetValue(normalizedFlightNumber, out NormalisedFlightData? flightData);
            if (flightFound)
                return Task.FromResult<NormalisedFlightData?>(flightData);

            _logger.LogDebug(
                AppConstants.LogMessages.ProviderNoDataFound,
                AppConstants.QuickFlight.ProviderName, flightNumber, date);

            return Task.FromResult<NormalisedFlightData?>(null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception unexpectedException)
        {
            _logger.LogError(
                unexpectedException,
                AppConstants.LogMessages.ProviderFetchError,
                AppConstants.QuickFlight.ProviderName, flightNumber);

            throw new ProviderException(
                AppConstants.QuickFlight.ProviderName,
                AppConstants.ErrorMessages.ProviderFetchFailed(AppConstants.QuickFlight.ProviderName, flightNumber),
                unexpectedException);
        }
    }

    private Dictionary<string, NormalisedFlightData> LoadFlightStubs()
    {
        try
        {
            string stubJson = EmbeddedResourceReader.Read(AppConstants.QuickFlight.StubFileName);

            JsonSerializerOptions caseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };

            List<QuickFlightResponse> rawResponses =
                JsonSerializer.Deserialize<List<QuickFlightResponse>>(stubJson, caseInsensitiveOptions) ?? [];

            DateOnly stubDate = DateOnly.FromDateTime(DateTime.UtcNow);

            Dictionary<string, NormalisedFlightData> flightLookup = rawResponses
                .ToDictionary(
                    rawResponse => rawResponse.FlightCode.ToUpperInvariant(),
                    rawResponse => FlightStatusNormaliser.NormaliseQuickFlight(rawResponse, stubDate));

            _logger.LogInformation(
                AppConstants.LogMessages.ProviderStubsLoaded,
                AppConstants.QuickFlight.ProviderName,
                flightLookup.Count,
                AppConstants.QuickFlight.StubFileName);

            return flightLookup;
        }
        catch (ResourceNotFoundException)
        {
            throw;
        }
        catch (JsonException jsonException)
        {
            throw new ProviderException(
                AppConstants.QuickFlight.ProviderName,
                AppConstants.ErrorMessages.ProviderInitialisationFailed(
                    AppConstants.QuickFlight.ProviderName,
                    AppConstants.QuickFlight.StubFileName),
                jsonException);
        }
        catch (Exception unexpectedException)
        {
            throw new ProviderException(
                AppConstants.QuickFlight.ProviderName,
                AppConstants.ErrorMessages.ProviderUnexpectedInitError(AppConstants.QuickFlight.ProviderName),
                unexpectedException);
        }
    }
}
