# Flight Status Tracker — Specification

## Purpose

This document defines the system design, data models, business rules, interface contracts, and implementation constraints for the Flight Status Tracker before development begins.

---

# 1. Unified Status Enum

```csharp
public enum FlightStatus
{
    OnTime,    // Departure or arrival within 15 minutes of schedule
    Delayed,   // Departure or arrival pushed beyond 15 minutes
    Cancelled, // Flight will not operate
    Diverted,  // Flight landed at a different airport
    Unknown    // No usable status returned by either provider
}
```

### Serialization Rules

`FlightStatus` must always be serialized as a string in API responses.

Examples:

```json
"Delayed"
```

```json
"Cancelled"
```

Numeric enum serialization is not permitted.

---

# 2. Data Models

## 2.1 FlightStatusResult

Returned to the frontend by the API.

### Contract

- Must always be returned.
- Must never be `null`.
- Business logic failures must not throw exceptions.
- Unknown results should be represented using `FlightStatus.Unknown`.

| Field | Type | Nullable | Description |
|---------|---------|---------|---------|
| flightNumber | string | No | IATA flight code (e.g. BA493) |
| date | DateOnly | No | Scheduled departure date |
| status | FlightStatus | No | Unified status value |
| scheduledDeparture | DateTimeOffset | No | Scheduled departure in UTC |
| scheduledArrival | DateTimeOffset | No | Scheduled arrival in UTC |
| actualDeparture | DateTimeOffset? | Yes | Actual departure in UTC |
| actualArrival | DateTimeOffset? | Yes | Actual arrival in UTC |
| terminal | string? | Yes | Departure terminal (AeroTrack only) |
| gate | string? | Yes | Departure gate (AeroTrack only) |
| delayReason | string? | Yes | Delay reason (AeroTrack only) |
| lastUpdatedUtc | DateTimeOffset | No | Timestamp of winning provider data |
| message | string? | Yes | Populated only when status is Unknown |

### No Provider Data Scenario

When no provider returns data:

```text
status               = Unknown
scheduledDeparture   = DateTimeOffset.MinValue
scheduledArrival     = DateTimeOffset.MinValue
lastUpdatedUtc       = DateTimeOffset.UtcNow
message              = "No data returned by any provider for this flight and date."
```

---

## 2.2 NormalisedFlightData

Internal intermediate model produced by providers after normalization.

### Rules

- Not returned via HTTP.
- Used exclusively by merge logic.

| Field | Type | Nullable |
|---------|---------|---------|
| flightNumber | string | No |
| date | DateOnly | No |
| status | FlightStatus | No |
| scheduledDeparture | DateTimeOffset | No |
| scheduledArrival | DateTimeOffset | No |
| actualDeparture | DateTimeOffset? | Yes |
| actualArrival | DateTimeOffset? | Yes |
| terminal | string? | Yes |
| gate | string? | Yes |
| delayReason | string? | Yes |
| lastUpdatedUtc | DateTimeOffset | No |

---

## 2.3 AeroTrackResponse

Raw provider model used by AeroTrack.

### Visibility

- Internal only
- Not exposed externally

| Field | Type | Nullable |
|---------|---------|---------|
| flightNumber | string | No |
| rawStatus | string | No |
| scheduledDeparture | DateTimeOffset | No |
| scheduledArrival | DateTimeOffset | No |
| actualDeparture | DateTimeOffset? | Yes |
| actualArrival | DateTimeOffset? | Yes |
| terminal | string? | Yes |
| gate | string? | Yes |
| delayReason | string? | Yes |
| lastUpdatedUtc | DateTimeOffset | No |

### Status Mapping

| AeroTrack Value | FlightStatus |
|-----------------|-------------|
| ON_TIME | OnTime |
| DELAYED | Delayed |
| CANCELLED | Cancelled |
| DIVERTED | Diverted |
| Any Other Value | Unknown |

---

## 2.4 QuickFlightResponse

Raw provider model used by QuickFlight.

### Visibility

- Internal only
- Not exposed externally

| Field | Type | Nullable |
|---------|---------|---------|
| flightCode | string | No |
| status | string | No |
| scheduledDep | DateTimeOffset | No |
| scheduledArr | DateTimeOffset | No |
| lastUpdatedUtc | DateTimeOffset | No |

### Status Mapping

| QuickFlight Value | FlightStatus |
|------------------|-------------|
| on-time | OnTime |
| delayed | Delayed |
| cancelled | Cancelled |
| diverted | Diverted |
| Any Other Value | Unknown |

---

# 3. Interface Contracts

## 3.1 IFlightStatusProvider

```csharp
namespace FlightStatus.Api.Domain.Interfaces;

public interface IFlightStatusProvider
{
    /// <summary>
    /// Queries the provider for a flight status.
    /// Returns null when no data exists for the flight/date.
    /// Missing flight records must not throw exceptions.
    /// </summary>
    Task<NormalisedFlightData?> GetStatusAsync(
        string flightNumber,
        DateOnly date,
        CancellationToken cancellationToken = default);
}
```

### Implementations

- `AeroTrackProvider`
- `QuickFlightProvider`

### DI Registration

Both providers are registered as:

```csharp
IFlightStatusProvider
```

and injected as:

```csharp
IEnumerable<IFlightStatusProvider>
```

---

## 3.2 IFlightMergeService

```csharp
namespace FlightStatus.Api.Domain.Interfaces;

public interface IFlightMergeService
{
    /// <summary>
    /// Merges provider responses into a single result.
    /// Never returns null.
    /// </summary>
    FlightStatusResult Merge(
        IEnumerable<NormalisedFlightData?> providerResults);
}
```

---

# 4. Normalisation Rules

Applied by:

```csharp
FlightStatusNormaliser
```

### Characteristics

- Static class
- Pure function
- No dependency injection
- No side effects
- No logging

### Resolution Priority

| Priority | Condition | Result |
|----------|-----------|--------|
| 1 | Raw status maps to Cancelled | Cancelled |
| 2 | Raw status maps to Diverted | Diverted |
| 3 | Actual departure or arrival differs by more than 15 minutes | Delayed |
| 4 | Actual departure or arrival differs by 15 minutes or less | OnTime |
| 5 | No actual times and raw status maps successfully | Mapped status |
| 6 | No actual times and raw status cannot be mapped | Unknown |

### Time Comparison Rule

```csharp
Math.Abs(timeDifference.TotalMinutes)
```

must be used.

### Override Rule

If:

```text
rawStatus = ON_TIME
```

but actual times differ by more than 15 minutes,

then:

```text
status = Delayed
```

---

# 5. Merge Rules

Applied by:

```csharp
FlightMergeService
```

### Resolution Priority

| Priority | Condition | Action |
|----------|-----------|--------|
| 1 | No providers returned data | Return Unknown result |
| 2 | Exactly one provider returned data | Use that provider |
| 3 | Both providers returned data | Use result with latest lastUpdatedUtc |

### Winning Record Rule

Latest timestamp wins:

```text
Max(lastUpdatedUtc)
```

---

# 6. API Contract

## 6.1 Endpoint

```http
GET /flights/status?flightNumber={code}&date={yyyy-MM-dd}
```

### Execution Flow

1. Validate request.
2. Query all providers in parallel.
3. Merge responses.
4. Return unified result.

Provider execution must use:

```csharp
Task.WhenAll(...)
```

---

## 6.2 Query Parameters

| Parameter | Required | Format | Example |
|------------|----------|---------|---------|
| flightNumber | Yes | IATA code | BA493 |
| date | Yes | yyyy-MM-dd | 2025-08-15 |

---

## 6.3 Validation Rules

Validation occurs before any provider call.

| Condition | HTTP Status | Response |
|------------|------------|-----------|
| flightNumber missing or whitespace | 400 | `{ "error": "flightNumber and date are required." }` |
| date missing or whitespace | 400 | `{ "error": "flightNumber and date are required." }` |
| date format invalid | 400 | `{ "error": "date must be in yyyy-MM-dd format." }` |
| unexpected exception | 500 | `{ "error": "An unexpected error occurred." }` |

---

## 6.4 Response Examples

### 200 OK — Delayed Flight

```json
{
  "flightNumber": "BA493",
  "date": "2025-08-15",
  "status": "Delayed",
  "scheduledDeparture": "2025-08-15T06:00:00Z",
  "scheduledArrival": "2025-08-15T08:30:00Z",
  "actualDeparture": "2025-08-15T06:45:00Z",
  "actualArrival": null,
  "terminal": "T5",
  "gate": "B22",
  "delayReason": "Late inbound aircraft",
  "lastUpdatedUtc": "2025-08-15T05:50:00Z",
  "message": null
}
```

### 200 OK — Unknown

```json
{
  "flightNumber": "XX999",
  "date": "2025-08-15",
  "status": "Unknown",
  "scheduledDeparture": "0001-01-01T00:00:00Z",
  "scheduledArrival": "0001-01-01T00:00:00Z",
  "actualDeparture": null,
  "actualArrival": null,
  "terminal": null,
  "gate": null,
  "delayReason": null,
  "lastUpdatedUtc": "2025-08-15T08:58:47Z",
  "message": "No data returned by any provider for this flight and date."
}
```

---

# 7. Stub Coverage Matrix

Stub files reside in:

```text
Infrastructure/Stubs/
```

### Lookup Rule

Stub lookup is based on:

```text
flightNumber
```

only.

The `date` parameter is accepted but ignored for stub resolution.

| Flight | AeroTrack | QuickFlight | Expected Result |
|----------|-----------|-------------|-----------------|
| BA493 | Delayed (latest timestamp) | OnTime | Delayed |
| LH100 | OnTime | OnTime | OnTime |
| EK202 | Cancelled | Cancelled | Cancelled |
| TK801 | Diverted | No Record | Diverted |
| FR550 | No Record | Delayed | Delayed |
| XX999 | No Record | No Record | Unknown |

### Requirement

`XX999` must not exist in either stub file.

Its Unknown state must be produced by both providers returning:

```csharp
null
```

---

# 8. Project Structure

```text
flight-status/
├── spec.md
├── README.md
├── prompts.md
├── reflection.md
├── FlightStatus.Api/
│   ├── Domain/
│   │   ├── Enums/
│   │   │   └── FlightStatus.cs
│   │   ├── Models/
│   │   │   └── NormalisedFlightData.cs
│   │   └── Interfaces/
│   │       ├── IFlightStatusProvider.cs
│   │       └── IFlightMergeService.cs
│   ├── Application/
│   │   ├── Services/
│   │   │   └── FlightMergeService.cs
│   │   └── Normalisation/
│   │       └── FlightStatusNormaliser.cs
│   ├── Infrastructure/
│   │   ├── Providers/
│   │   │   ├── AeroTrackProvider.cs
│   │   │   └── QuickFlightProvider.cs
│   │   ├── Stubs/
│   │   │   ├── aerotrack-stubs.json
│   │   │   └── quickflight-stubs.json
│   │   └── Models/
│   │       ├── AeroTrackResponse.cs
│   │       └── QuickFlightResponse.cs
│   ├── Api/
│   │   ├── Endpoints/
│   │   │   └── FlightEndpoints.cs
│   │   └── Contracts/
│   │       └── FlightStatusResponse.cs
│   └── Program.cs
├── FlightStatus.Tests/
│   ├── NormaliserTests.cs
│   ├── MergeServiceTests.cs
│   └── FlightEndpointTests.cs
└── flight-status-ui/
    ├── index.html
    ├── app.js
    └── styles.css
```

---

# 9. Design Patterns

| Pattern | Applied In | Rationale |
|----------|------------|-----------|
| Strategy | IFlightStatusProvider implementations | Add providers without changing merge logic |
| Result Object | FlightStatusResult | Consistent API output |
| Pure Static Normaliser | FlightStatusNormaliser | Simple, deterministic testing |
| Dependency Inversion | API layer → interfaces | No provider coupling |

---

# 10. SOLID Alignment

| Principle | Implementation |
|------------|---------------|
| SRP | Normaliser only normalises; MergeService only merges |
| OCP | New providers added without modifying existing providers |
| LSP | Providers substitute through interface |
| ISP | Single focused interface method |
| DIP | High-level modules depend on abstractions |

---

# 11. Logging Strategy

Logging uses:

```csharp
ILogger<T>
```

No additional logging frameworks are required.

| Level | Location | Event |
|---------|---------|---------|
| Information | Provider | Flight found and resolved |
| Warning | Provider | No provider data found |
| Warning | FlightMergeService | All providers returned null |
| Information | FlightMergeService | Winning provider selected |
| Error | Endpoint | Unexpected exception |

### Logging Exclusion

```csharp
FlightStatusNormaliser
```

must never log.

---

# 12. Frontend State Definitions

| State | Trigger | UI Color |
|---------|---------|---------|
| OnTime | status === "OnTime" | #16a34a |
| Delayed | status === "Delayed" | #d97706 |
| Cancelled | status === "Cancelled" | #dc2626 |
| Diverted | status === "Diverted" | #dc2626 |
| Unknown | status === "Unknown" | #6b7280 |
| Error | Network or non-2xx response | Red banner |
| Empty | Before first search | No card displayed |

### Conditional Rendering

Only display the following fields when populated:

- terminal
- gate
- delayReason

### API Configuration

The API base URL must be defined once at the top of:

```javascript
app.js
```

using a single constant.

---

# 13. Out of Scope

The following features are explicitly excluded:

- Real flight provider integrations
- Provider credentials or secrets
- Authentication
- Authorization
- Databases
- Persistent storage
- Swagger/OpenAPI UI
- External JavaScript libraries
- CSS frameworks