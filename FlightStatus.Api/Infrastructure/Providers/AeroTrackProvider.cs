using FlightStatus.Api.Application.Normalisation;
using FlightStatus.Api.Common;
using FlightStatus.Api.Domain.Exceptions;
using FlightStatus.Api.Domain.Interfaces;
using FlightStatus.Api.Domain.Models;
using FlightStatus.Api.Infrastructure.Helpers;
using FlightStatus.Api.Infrastructure.Models;
using System.Text.Json;

namespace FlightStatus.Api.Infrastructure.Providers;

public class AeroTrackProvider : IFlightStatusProvider
{
    private readonly Dictionary<string, NormalisedFlightData> _flightsByNumber;
    private readonly ILogger<AeroTrackProvider> _logger;

    public AeroTrackProvider(ILogger<AeroTrackProvider> logger)
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
                AppConstants.AeroTrack.ProviderName, flightNumber, date);

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
                AppConstants.AeroTrack.ProviderName, flightNumber);

            throw new ProviderException(
                AppConstants.AeroTrack.ProviderName,
                AppConstants.ErrorMessages.ProviderFetchFailed(AppConstants.AeroTrack.ProviderName, flightNumber),
                unexpectedException);
        }
    }

    private Dictionary<string, NormalisedFlightData> LoadFlightStubs()
    {
        try
        {
            string stubJson = EmbeddedResourceReader.Read(AppConstants.AeroTrack.StubFileName);

            JsonSerializerOptions caseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };

            List<AeroTrackResponse> rawResponses =
                JsonSerializer.Deserialize<List<AeroTrackResponse>>(stubJson, caseInsensitiveOptions) ?? [];

            DateOnly stubDate = DateOnly.FromDateTime(DateTime.UtcNow);

            Dictionary<string, NormalisedFlightData> flightLookup = rawResponses
                .ToDictionary(
                    rawResponse => rawResponse.FlightNumber.ToUpperInvariant(),
                    rawResponse => FlightStatusNormaliser.NormaliseAeroTrack(rawResponse, stubDate));

            _logger.LogInformation(
                AppConstants.LogMessages.ProviderStubsLoaded,
                AppConstants.AeroTrack.ProviderName,
                flightLookup.Count,
                AppConstants.AeroTrack.StubFileName);

            return flightLookup;
        }
        catch (ResourceNotFoundException)
        {
            throw;
        }
        catch (JsonException jsonException)
        {
            throw new ProviderException(
                AppConstants.AeroTrack.ProviderName,
                AppConstants.ErrorMessages.ProviderInitialisationFailed(
                    AppConstants.AeroTrack.ProviderName,
                    AppConstants.AeroTrack.StubFileName),
                jsonException);
        }
        catch (Exception unexpectedException)
        {
            throw new ProviderException(
                AppConstants.AeroTrack.ProviderName,
                AppConstants.ErrorMessages.ProviderUnexpectedInitError(AppConstants.AeroTrack.ProviderName),
                unexpectedException);
        }
    }
}
