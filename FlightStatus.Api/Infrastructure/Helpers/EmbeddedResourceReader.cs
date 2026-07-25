using FlightStatus.Api.Domain.Exceptions;
using System.Reflection;

namespace FlightStatus.Api.Infrastructure.Helpers;

public static class EmbeddedResourceReader
{
    public static string Read(string resourceName)
    {
        try
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();

            string? fullyQualifiedResourceName = executingAssembly
                .GetManifestResourceNames()
                .FirstOrDefault(embeddedName =>
                    embeddedName.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase));

            if (fullyQualifiedResourceName is null)
                throw new ResourceNotFoundException(resourceName);

            using Stream resourceStream = executingAssembly.GetManifestResourceStream(fullyQualifiedResourceName)!;
            using StreamReader streamReader = new(resourceStream);
            return streamReader.ReadToEnd();
        }
        catch (ResourceNotFoundException)
        {
            throw;
        }
        catch (Exception unexpectedException)
        {
            throw new ResourceNotFoundException(resourceName, unexpectedException);
        }
    }
}
