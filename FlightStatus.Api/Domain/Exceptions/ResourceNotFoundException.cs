namespace FlightStatus.Api.Domain.Exceptions;

/// <summary>
/// Thrown when a required embedded resource (e.g. stub JSON file) cannot be found in the assembly.
/// </summary>
public class ResourceNotFoundException : FlightStatusException
{
    public string ResourceName { get; }

    public ResourceNotFoundException(string resourceName)
        : base($"Required resource '{resourceName}' was not found.")
    {
        ResourceName = resourceName;
    }

    public ResourceNotFoundException(string resourceName, Exception innerException)
        : base($"Required resource '{resourceName}' was not found.", innerException)
    {
        ResourceName = resourceName;
    }
}
