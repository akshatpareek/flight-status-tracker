namespace FlightStatus.Api.Contracts;

/// <summary>
/// Structured error payload returned by the global exception handler.
/// </summary>
public record ErrorResponse(
    string TraceId,
    int StatusCode,
    string Message,
    string? Detail = null
);
