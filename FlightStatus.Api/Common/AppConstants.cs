namespace FlightStatus.Api.Common;

/// <summary>
/// Application-wide constants. All magic strings and numeric literals used across
/// the codebase should be defined here to ensure a single source of truth.
/// </summary>
public static class AppConstants
{
    public const string ContentTypeJson = "application/json";

    /// <summary>
    /// HTTP status code returned when the client cancels the request before the server
    /// completes processing (non-standard but widely used "Client Closed Request").
    /// </summary>
    public const int HttpStatusClientClosedRequest = 499;

    /// <summary>
    /// Maximum number of minutes by which a flight's actual time may differ from its
    /// scheduled time before the flight is considered delayed.
    /// </summary>
    public const double DelayThresholdMinutes = 15.0;

    /// <summary>Constants relating to the AeroTrack data provider.</summary>
    public static class AeroTrack
    {
        public const string ProviderName = "AeroTrackProvider";
        public const string StubFileName  = "aerotrack-stubs.json";

        /// <summary>Raw status strings returned by the AeroTrack API.</summary>
        public static class RawStatus
        {
            public const string OnTime    = "ON_TIME";
            public const string Delayed   = "DELAYED";
            public const string Cancelled = "CANCELLED";
            public const string Diverted  = "DIVERTED";
        }
    }

    /// <summary>Constants relating to the QuickFlight data provider.</summary>
    public static class QuickFlight
    {
        public const string ProviderName = "QuickFlightProvider";
        public const string StubFileName  = "quickflight-stubs.json";

        /// <summary>Raw status strings returned by the QuickFlight API.</summary>
        public static class RawStatus
        {
            public const string OnTime    = "on-time";
            public const string Delayed   = "delayed";
            public const string Cancelled = "cancelled";
            public const string Diverted  = "diverted";
        }
    }

    /// <summary>User-facing error messages returned in <c>ErrorResponse</c>.</summary>
    public static class ErrorMessages
    {
        public const string FlightNumberRequired =
            "flightNumber is required.";

        public const string DateRequiredOrInvalid =
            "date is required and must be a valid date (yyyy-MM-dd).";

        public const string NoProviderDataFound =
            "No data returned by any provider for this flight and date.";

        public const string ClientCancelledRequest =
            "The request was cancelled by the client.";

        public const string ServerResourceMissing =
            "A required server resource is missing. Please contact support.";

        public const string UnexpectedServerError =
            "An unexpected error occurred. Please try again later.";

        public static string ProviderFailedToRespond(string providerName) =>
            $"A flight data provider ({providerName}) failed to respond correctly.";

        public static string ProviderInitialisationFailed(string providerName, string fileName) =>
            $"Failed to deserialise stub data from '{fileName}' for provider '{providerName}'.";

        public static string ProviderUnexpectedInitError(string providerName) =>
            $"Unexpected error while initialising provider '{providerName}'.";

        public static string ProviderFetchFailed(string providerName, string flightNumber) =>
            $"Provider '{providerName}' failed to retrieve status for flight '{flightNumber}'.";

        public static string MergeServiceFailed(string flightNumber) =>
            $"An error occurred while merging flight data for flight '{flightNumber}'.";

        public static string ResourceNotFound(string resourceName) =>
            $"Required resource '{resourceName}' was not found.";
    }

    /// <summary>Structured logging message templates.</summary>
    public static class LogMessages
    {
        public const string ProviderStubsLoaded =
            "{ProviderName} loaded {FlightCount} flight stubs from '{StubFileName}'.";

        public const string ProviderNoDataFound =
            "{ProviderName}: No flight data found for '{FlightNumber}' on {Date}.";

        public const string ProviderFetchError =
            "{ProviderName} encountered an error while fetching status for flight '{FlightNumber}'.";

        public const string ProviderSkipped =
            "Provider '{ProviderName}' failed for flight '{FlightNumber}' on {Date}. Skipping this provider.";

        public const string ProviderUnexpectedSkipped =
            "Unexpected error from a provider for flight '{FlightNumber}' on {Date}. Skipping this provider.";

        public const string MergeNoResults =
            "No providers returned data for flight '{FlightNumber}' on {Date}.";

        public const string MergeWinnerSelected =
            "Merge selected provider result for flight '{FlightNumber}' with LastUpdatedUtc {LastUpdatedUtc}.";

        public const string MergeUnexpectedError =
            "Unexpected error while merging provider results for flight '{FlightNumber}' on {Date}.";

        public const string ExceptionHandlerUnhandled =
            "Unhandled exception [{ExceptionType}] on {HttpMethod} {RequestPath}. "
            + "TraceId: {TraceId}. StatusCode: {StatusCode}";

        public const string ExceptionHandlerHandled =
            "Handled exception [{ExceptionType}] on {HttpMethod} {RequestPath}. "
            + "TraceId: {TraceId}. StatusCode: {StatusCode}";
    }
}
