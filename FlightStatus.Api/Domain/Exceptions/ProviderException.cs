namespace FlightStatus.Api.Domain.Exceptions;

/// <summary>
/// Thrown when a flight data provider encounters an error fetching or deserialising data.
/// </summary>
public class ProviderException : FlightStatusException
{
    public string ProviderName { get; }

    public ProviderException(string providerName, string message)
        : base(message)
    {
        ProviderName = providerName;
    }

    public ProviderException(string providerName, string message, Exception innerException)
        : base(message, innerException)
    {
        ProviderName = providerName;
    }
}
