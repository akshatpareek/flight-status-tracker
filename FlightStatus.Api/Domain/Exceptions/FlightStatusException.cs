namespace FlightStatus.Api.Domain.Exceptions;

/// <summary>
/// Base exception for all domain-level errors in the Flight Status application.
/// All custom exceptions thrown from the domain or infrastructure layers inherit from this type,
/// allowing the global exception handler to apply consistent error handling policy.
/// </summary>
public class FlightStatusException : Exception
{
    public FlightStatusException(string message)
        : base(message)
    {
    }

    public FlightStatusException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
