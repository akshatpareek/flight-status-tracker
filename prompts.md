# Flight Status Tracker — AI Prompts & Key Decisions

This document logs the significant prompts used during the development of the Flight Status Tracker, highlighting key architectural decisions and refactoring choices.

---

## Prompt 1: Initial Provider Setup
> **Prompt:**
> *"use my spec.md for reference and Create AeroTrackProvider and QuickFlightProvider, both implementing IFlightStatusProvider. Each loads its JSON stub file once in the constructor from AppContext.BaseDirectory. On GetStatusAsync, look up by flightNumber (case-insensitive). Log Information if found, Warning if not. Add a comment on GetStatusAsync that date is accepted per interface but stubs are keyed by flightNumber only. Also update the .csproj so the JSON stubs copy to output directory."*

### AI Judgement & Decisions
* Set up stubs folder layout and registered file copy rules in `.csproj`.
* Created the providers with initial dictionary lookups.

---

## Prompt 2: Response Contract & Merge Service
> **Prompt:**
> *"Create FlightStatusResponse record under Api/Contracts using fields from spec.md. Then implement FlightMergeService following the merge rules. When both providers return null, set ScheduledDeparture and ScheduledArrival to DateTimeOffset.MinValue, LastUpdatedUtc to UtcNow, and Message as specified in spec.md. Never return null from Merge. Inject ILogger and log at Warning for Unknown, Information when a winner is selected."*

### AI Judgement & Decisions
* Created `FlightStatusResponse.cs` under the contracts folder.
* Implemented the first pass of the `FlightMergeService` using `lastUpdatedUtc` comparisons to pick a winner.

---

## Prompt 3: Wiring Up API & Minimal API Endpoints
> **Prompt:**
> *"Wire up the API in Program.cs and create the endpoint in Api/Endpoints/FlightEndpoints.cs. Register both providers as IFlightStatusProvider so they inject as IEnumerable (Strategy pattern). Endpoint: GET /flights/status validate per spec.md before calling providers. Call providers in parallel with Task.WhenAll, pass to merge service, return 200. Add CORS (any origin, local dev only). JSON must serialise enums as strings, properties as camelCase. No Swagger."*

### AI Judgement & Decisions
* Wired up ASP.NET Core DI mapping for providers.
* Configured Minimal API endpoint with parameter validation checks.
* Enabled CORS policy and configured HTTP JSON serialization options.

---

## Prompt 4: Creating Test Files
> **Prompt:**
> *"Create three xUnit test files in FlightStatus.Tests covering the cases in spec.md 7. NormaliserTests: pure static calls, no mocking needed. One test per rule in 4. MergeServiceTests: use NullLogger, no Moq. Cover all three merge branches from 5 plus field mapping. FlightEndpointTests: use WebApplicationFactory. Test each 400 case and BA493 → Delayed, XX999 → Unknown. Add public partial class Program {} to Program.cs so WebApplicationFactory can see it."*

### AI Judgement & Decisions
* Established the test project framework `FlightStatus.Tests`.
* Created the target files for unit and integration testing.

---

## Prompt 5: Creating the Plain Frontend UI
> **Prompt:**
> *"Create a plain HTML/JS frontend in flight-status-ui/ — no frameworks, no CDN links. Three files: index.html, styles.css, app.js. const API_BASE at the top of app.js so it's easy to change. Result card shows all FlightStatusResponse fields. Terminal, gate, delayReason only when non-null. Status badge colours from spec.md 12. Error banner on any non-2xx or network failure."*

### AI Judgement & Decisions
* Designed a responsive UI utilizing semantic HTML5 structure.
* Wrote vanilla JS to perform input checks, handle API calls, and render results.

---

## Prompt 6: Refactoring FlightMergeService
> **Prompt:**
> *"Refactor FlightMergeService to match spec.md exactly. Requirements: Change Merge signature to: FlightStatusResponse Merge(IEnumerable<NormalisedFlightData?> providerResults, string flightNumber, DateOnly flightDate). Follow the merge rules from spec.md: If both providers return null, return Unknown with defaults. If one provider returns data, use that result. If multiple providers return data, select the one with the latest LastUpdatedUtc. Map every field to FlightStatusResponse. Use ILogger for Warning and Info."*

### AI Judgement & Decisions
* Refactored `FlightMergeService.cs` to ensure all fields are mapped correctly and standard log messages are emitted.

---

## Prompt 7: Normalizer Unit Tests
> **Prompt:**
> *"Use the existing AeroTrackResponse and QuickFlightResponse records and test FlightStatusNormaliser.NormaliseAeroTrack() and NormaliseQuickFlight(). Cover: ON_TIME -> OnTime, DELAYED -> Delayed, CANCELLED -> Cancelled, DIVERTED -> Diverted, Actual times more than 15 minutes late -> Delayed, Actual times within 15 minutes -> OnTime, Unknown status -> Unknown, QuickFlight status mapping. Remove all placeholder code."*

### AI Judgement & Decisions
* Built robust, descriptive unit tests asserting the status mapping rules of `FlightStatusNormaliser`.

---

## Prompt 8: Embedded Resource Stub Loading
> **Prompt:**
> *"Update the stub JSON loading approach for both AeroTrackProvider and QuickFlightProvider. I want the JSON files (aerotrack_stub.json and quickflight_stub.json) to be embedded resources instead of reading them using file paths. Add the required EmbeddedResource configuration in FlightStatus.Api.csproj. Create a small reusable helper class to read embedded JSON files from the assembly. Update both providers to use this helper."*

### AI Judgement & Decisions
* Set JSON stubs as embedded resources inside the `.csproj` file.
* Created `EmbeddedResourceReader` to read stubs out-of-process, solving folder directory dependency issues.

---

## Prompt 9: Explaining Test Methods
> **Prompt:**
> *"can you explain what each test methods doing"*

### AI Judgement & Decisions
* Documented the purpose of each test method to verify system correctness:

### 1. `NormaliserTests.cs` (Unit Tests)
* **`NormaliseAeroTrack_OnTime_ReturnsOnTime`**: Verifies that when AeroTrack returns a raw status of `ON_TIME` and the scheduled times match actual times, the normalizer maps it to `EnumFlightStatus.OnTime`.
* **`NormaliseAeroTrack_DelayedByActualDeparture_ReturnsDelayed`**: Verifies the override rule. If actual departure exceeds the scheduled time by more than 15 minutes, it overrides `ON_TIME` and returns `EnumFlightStatus.Delayed`.
* **`NormaliseAeroTrack_StatusMapping`**: Verifies raw AeroTrack status values (`CANCELLED`, `DIVERTED`, and unknown values) map to their canonical counterparts.
* **`NormaliseQuickFlight_StatusMapping`**: Verifies raw QuickFlight status values map to their canonical counterparts.

### 2. `MergeServiceTests.cs` (Unit Tests)
* **`Merge_BothNull_ReturnsUnknownWithDefaults`**: Verifies that when all providers return `null` (no data found), the merge service constructs a default `Unknown` response with min-date schedule fields.
* **`Merge_OneNull_UsesNonNull`**: Verifies that if one provider returns `null` and the other returns data, the non-null provider's data is chosen.
* **`Merge_BothPresent_PicksLatestLastUpdated`**: Verifies conflict resolution. When both providers return data, it picks the record with the most recent `LastUpdatedUtc`.

### 3. `FlightEndpointTests.cs` (Integration Tests)
* **`GetStatus_Returns400_OnInvalidInput`**: Verifies that invalid inputs (empty flight numbers, bad date formats) fail backend validation and return `400 Bad Request`.
* **`GetStatus_BA493_ReturnsDelayed`**: Asserts that querying the flight `BA493` on a valid date returns the combined response with status `Delayed` (AeroTrack record).
* **`GetStatus_XX999_ReturnsUnknown`**: Asserts that querying a non-existent flight like `XX999` returns a `200 OK` response with status `Unknown` and the fallback error message.

---

## Prompt 10: Global Exception Handling Implementation

> **Prompt:**
> *"read my project and implement global exception handler in proper way. use try catch if requreid in other classes"*

### AI Judgement & Actions
* **Global Handler Registration**: Created `GlobalExceptionHandler.cs` implementing .NET's `IExceptionHandler` interface (introduced in .NET 8). Intercepts all unhandled errors, writes a clean JSON response using the `ErrorResponse` record contract, and returns appropriate HTTP status codes (e.g. 502 for provider failures, 499 for client cancellation).
* **Defensive Resilience**: Added specific try-catch blocks to the `EmbeddedResourceReader` and stub provider loading logic. If a single stub provider fails, it logs a warning but allows the other providers to continue functioning (graceful degradation).

---

## Prompt 11: Clean Code & Standards Refactoring

> **Prompt:**
> *"Use appconstant create a app constant file use that. also in all code give me meaning full names to variable they shoudnlt have just x y n l etc single char var no like in linq expressions also all statements not exceed 150 chars per line make my full code clean code also"*

### AI Judgement & Actions
* **Magic String Centralization**: Created `Common/AppConstants.cs` containing separate nested classes for AeroTrack, QuickFlight, Log Messages, and Error Messages. Replaced all raw strings and hardcoded threshold values (like `15` minutes for delay check) with these constants.
* **Variable Renaming**: Renamed single-character variables in loops, LINQ expressions, and lambdas (e.g. `f` -> `flightData`, `p` -> `provider`, `results` -> `validResults`).
* **Line-Length Formatting**: Checked every C# statement to ensure no line exceeds 150 characters, improving readability.

---

## Prompt 12: Handling Port & Connection Protocol Conflicts

> **Prompt:**
> *"its working from postman but only when i am using https when i ran my frotnend it gave network error. also when backend opens it shows his localhost page can’t be found No webpage was found for the web address: https://localhost:7262/ HTTP ERROR 404 can we show something valuable here"*

### AI Judgement & Actions
* **Mixed Protocol Fix**: Aligned the frontend `app.js` API base address to the correct HTTPS endpoint `https://localhost:7262` to prevent mixed-content/network errors.
* **Default Landing Page**: Created a modern, dark-themed HTML landing page returning from the API root (`/`) detailing available endpoints, usage schemas, and a test link, replacing the unhelpful 404 error page.
* **Health Check**: Added a `GET /health` endpoint returning server health telemetry.

---

## Prompt 13: Spec Alignment & Frontend Improvements

> **Prompt:**
> *[Provided spec.md text]*
> *"check this verify and check wether we not missed something from backend or frontend also add frontned validation or any other things but just dont make too much it should look good enough read it and do if something not missed from frontend"*

### AI Judgement & Actions
* **Stub Deserialization Bug Fix**: Identified that the providers were previously deserializing raw stub JSONs directly into `NormalisedFlightData` (causing `NullReferenceException` due to property mismatches). Rewrote both provider stub loaders to parse the JSON into their respective provider shapes (`AeroTrackResponse` / `QuickFlightResponse`) before passing them through `FlightStatusNormaliser`.
* **Frontend Date & IATA Validation**: Added regex check for flight numbers to ensure IATA format (e.g. `BA493`). Added `min` and `max` date limits (2020–2030) to block unrealistic years like `11/11/1111`.
* **AeroTrack Exclusivity**: Ensured optional fields (`terminal`, `gate`, `delayReason`) are only rendered on the frontend card when they are populated.
* **Loading Spinner**: Added a loading state spinner on the Search button to improve UX during API calls.
* **Security**: Escaped HTML values to prevent XSS.

---

## Prompt 14: Enum Serialization Fix

> **Prompt:**
> *"it showed this: BA493 ? 1 Schedule ... correct? also whats this ? 1 is it status?"*

### AI Judgement & Actions
* **Minimal API Serialization**: Configured `builder.Services.ConfigureHttpJsonOptions` inside `Program.cs` to add the `JsonStringEnumConverter()`. The previous config only targeted `AddControllers()`, leaving minimal API endpoints serializing enums as integers (`1` instead of `"Delayed"`).

---

## Prompt 15: Serving Default Landing Page from wwwroot
> **Prompt:**
> *"For BuildLandingPageHtml can we provide workaround solution?"*

### AI Judgement & Actions
* **Static File Workaround**: Created `wwwroot/index.html` and configured ASP.NET Core static files middleware (`UseDefaultFiles` and `UseStaticFiles`). Removed the inline HTML string and root route mapping from `FlightEndpoints.cs` to keep C# files clean of UI presentation logic.

---

## Prompt 16: Removing Code Comments

> **Prompt:**
> *"remove all commented code have only nexecesaary ones very few"*

### AI Judgement & Actions
* Overwrote 8 core classes to remove verbose XML docs and redundant explanatory comments, leaving the code clean, minimal, and self-documenting.

---

## Prompt 17: Resolving Integration Test Serialization Mismatch
> **Prompt:**
> *[Provided compiler error detailing JsonException on FlightStatusResponse deserialization]*

### AI Judgement & Actions
* Added `JsonSerializerOptions` with `JsonStringEnumConverter` registered inside `FlightEndpointTests.cs` to align the test client's JSON engine with the backend configuration.
