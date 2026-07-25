using FlightStatus.Api.Common;
using FlightStatus.Api.Domain.Enums;
using FlightStatus.Api.Domain.Models;
using FlightStatus.Api.Infrastructure.Models;

namespace FlightStatus.Api.Application.Normalisation;

public static class FlightStatusNormaliser
{
    private static readonly Dictionary<string, EnumFlightStatus> AeroTrackStatusMap = new()
    {
        [AppConstants.AeroTrack.RawStatus.OnTime]    = EnumFlightStatus.OnTime,
        [AppConstants.AeroTrack.RawStatus.Delayed]   = EnumFlightStatus.Delayed,
        [AppConstants.AeroTrack.RawStatus.Cancelled] = EnumFlightStatus.Cancelled,
        [AppConstants.AeroTrack.RawStatus.Diverted]  = EnumFlightStatus.Diverted,
    };

    private static readonly Dictionary<string, EnumFlightStatus> QuickFlightStatusMap = new()
    {
        [AppConstants.QuickFlight.RawStatus.OnTime]    = EnumFlightStatus.OnTime,
        [AppConstants.QuickFlight.RawStatus.Delayed]   = EnumFlightStatus.Delayed,
        [AppConstants.QuickFlight.RawStatus.Cancelled] = EnumFlightStatus.Cancelled,
        [AppConstants.QuickFlight.RawStatus.Diverted]  = EnumFlightStatus.Diverted,
    };

    public static NormalisedFlightData NormaliseAeroTrack(AeroTrackResponse aeroTrackResponse, DateOnly flightDate) =>
        new(
            aeroTrackResponse.FlightNumber,
            flightDate,
            ComputeStatus(
                aeroTrackResponse.RawStatus,
                AeroTrackStatusMap,
                aeroTrackResponse.ScheduledDeparture,
                aeroTrackResponse.ScheduledArrival,
                aeroTrackResponse.ActualDeparture,
                aeroTrackResponse.ActualArrival),
            aeroTrackResponse.ScheduledDeparture,
            aeroTrackResponse.ScheduledArrival,
            aeroTrackResponse.ActualDeparture,
            aeroTrackResponse.ActualArrival,
            aeroTrackResponse.Terminal,
            aeroTrackResponse.Gate,
            aeroTrackResponse.DelayReason,
            aeroTrackResponse.LastUpdatedUtc);

    public static NormalisedFlightData NormaliseQuickFlight(QuickFlightResponse quickFlightResponse, DateOnly flightDate) =>
        new(
            quickFlightResponse.FlightCode,
            flightDate,
            ComputeStatus(
                quickFlightResponse.Status,
                QuickFlightStatusMap,
                quickFlightResponse.ScheduledDep,
                quickFlightResponse.ScheduledArr,
                null,
                null),
            quickFlightResponse.ScheduledDep,
            quickFlightResponse.ScheduledArr,
            null,
            null,
            null,
            null,
            null,
            quickFlightResponse.LastUpdatedUtc);

    private static EnumFlightStatus ComputeStatus(
        string rawStatus,
        IReadOnlyDictionary<string, EnumFlightStatus> statusVocabulary,
        DateTimeOffset scheduledDeparture,
        DateTimeOffset scheduledArrival,
        DateTimeOffset? actualDeparture,
        DateTimeOffset? actualArrival)
    {
        statusVocabulary.TryGetValue(rawStatus, out EnumFlightStatus mappedStatus);

        if (mappedStatus == EnumFlightStatus.Cancelled)
            return EnumFlightStatus.Cancelled;

        if (mappedStatus == EnumFlightStatus.Diverted)
            return EnumFlightStatus.Diverted;

        bool hasActualTimes = actualDeparture.HasValue || actualArrival.HasValue;
        if (hasActualTimes)
        {
            bool departureIsDelayed = ExceedsDelayThreshold(scheduledDeparture, actualDeparture);
            bool arrivalIsDelayed = ExceedsDelayThreshold(scheduledArrival, actualArrival);

            if (departureIsDelayed || arrivalIsDelayed)
                return EnumFlightStatus.Delayed;

            return EnumFlightStatus.OnTime;
        }

        if (statusVocabulary.ContainsKey(rawStatus))
            return mappedStatus;

        return EnumFlightStatus.Unknown;
    }

    private static bool ExceedsDelayThreshold(DateTimeOffset scheduledTime, DateTimeOffset? actualTime) =>
        actualTime.HasValue &&
        Math.Abs((actualTime.Value - scheduledTime).TotalMinutes) > AppConstants.DelayThresholdMinutes;
}
