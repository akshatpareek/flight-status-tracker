using FlightStatus.Api.Common;
using FlightStatus.Api.Contracts;
using FlightStatus.Api.Domain.Exceptions;
using FlightStatus.Api.Domain.Interfaces;
using FlightStatus.Api.Domain.Models;

namespace FlightStatus.Api.Endpoints;

internal sealed class FlightEndpointsLogger { }

public static class FlightEndpoints
{
    public static IEndpointRouteBuilder MapFlightEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet("/health", () => Results.Ok(new
        {
            status  = "Healthy",
            service = "FlightStatus.Api",
            utc     = DateTimeOffset.UtcNow
        }));

        endpointRouteBuilder.MapGet("/flights/status", async (
            string? flightNumber,
            string? date,
            HttpContext httpContext,
            IEnumerable<IFlightStatusProvider> flightProviders,
            IFlightMergeService mergeService,
            ILogger<FlightEndpointsLogger> logger,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(flightNumber))
                return Results.Json(
                    BuildValidationError(httpContext, AppConstants.ErrorMessages.FlightNumberRequired),
                    statusCode: StatusCodes.Status400BadRequest);

            if (!DateOnly.TryParse(date, out DateOnly parsedFlightDate))
                return Results.Json(
                    BuildValidationError(httpContext, AppConstants.ErrorMessages.DateRequiredOrInvalid),
                    statusCode: StatusCodes.Status400BadRequest);

            IEnumerable<Task<NormalisedFlightData?>> providerTasks = flightProviders
                .Select(provider => FetchFromProviderAsync(
                    provider, flightNumber, parsedFlightDate, logger, cancellationToken));

            NormalisedFlightData?[] providerResults = await Task.WhenAll(providerTasks);

            FlightStatusResponse mergedResponse = mergeService.Merge(providerResults, flightNumber, parsedFlightDate);
            return Results.Ok(mergedResponse);
        });

        return endpointRouteBuilder;
    }

    private static async Task<NormalisedFlightData?> FetchFromProviderAsync(
        IFlightStatusProvider provider,
        string flightNumber,
        DateOnly flightDate,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            return await provider.GetStatusAsync(flightNumber, flightDate, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ProviderException providerException)
        {
            logger.LogWarning(
                providerException,
                AppConstants.LogMessages.ProviderSkipped,
                providerException.ProviderName, flightNumber, flightDate);

            return null;
        }
        catch (Exception unexpectedException)
        {
            logger.LogWarning(
                unexpectedException,
                AppConstants.LogMessages.ProviderUnexpectedSkipped,
                flightNumber, flightDate);

            return null;
        }
    }

    private static ErrorResponse BuildValidationError(HttpContext? httpContext, string message) =>
        new(
            TraceId:    httpContext?.TraceIdentifier ?? "n/a",
            StatusCode: StatusCodes.Status400BadRequest,
            Message:    message);
}
