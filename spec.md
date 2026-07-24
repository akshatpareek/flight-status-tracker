# Flight Status Tracker

## Specification

> **Purpose:** This document defines the system design, data models, business rules, and interface contracts for the Flight Status Tracker before implementation begins.

---

# Table of Contents

1. Overview
2. Architecture
3. Unified Status Model
4. Domain Models
5. Provider Models
6. Interface Contracts
7. API Contract
8. Status Normalisation Rules
9. Merge Rules
10. Stub Coverage
11. Frontend Behaviour
12. Design Principles
13. Assumptions
14. Out of Scope

---

# 1. Overview

The Flight Status Tracker allows a support agent to retrieve the current status of a flight using its flight number and travel date.

The application queries two deterministic stub providers:

* **AeroTrack**
* **QuickFlight**

Each provider returns flight information using its own status vocabulary. The application normalises provider-specific responses into a common domain model, applies deterministic merge rules, and returns a single unified response.

The solution is designed to:

* remain provider-independent
* support additional providers without modifying existing business logic
* separate provider integration, orchestration, and merge responsibilities
* follow dependency injection and interface-based design
* remain deterministic and easily testable

---

# 2. Architecture

The application follows a **layered monolithic architecture**.

```text
Frontend
    │
    ▼
Minimal API Endpoint
    │
    ▼
Flight Status Service
    │
    ├──────────────┐
    ▼              ▼
AeroTrack      QuickFlight
 Provider        Provider
    │              │
    └──────┬───────┘
           ▼
   Status Normalisation
           ▼
      Merge Rules
           ▼
  FlightStatusResult
```

Responsibilities are separated into:

* API
* Business Service
* Provider Integration
* Status Normalisation
* Merge Logic

---

# 3. Unified Status Model

| Status    | Description                                               |
| --------- | --------------------------------------------------------- |
| OnTime    | Departure or arrival occurs within 15 minutes of schedule |
| Delayed   | Departure or arrival exceeds 15 minutes from schedule     |
| Cancelled | Flight will not operate                                   |
| Diverted  | Flight lands at a different airport                       |
| Unknown   | No usable provider data                                   |

---

# 4. Domain Models

## FlightStatusResult

Represents the unified response returned by the API.

| Field               | Description                               |
| ------------------- | ----------------------------------------- |
| Flight Number       | Flight identifier                         |
| Date                | Flight date                               |
| Status              | Unified flight status                     |
| Scheduled Departure | Scheduled departure time                  |
| Scheduled Arrival   | Scheduled arrival time                    |
| Actual Departure    | Actual departure time (when available)    |
| Actual Arrival      | Actual arrival time (when available)      |
| Terminal            | AeroTrack only                            |
| Gate                | AeroTrack only                            |
| Delay Reason        | AeroTrack only                            |
| Last Updated (UTC)  | Timestamp of selected provider            |
| Message             | Additional information for Unknown status |

## Normalised Flight Data

Internal model produced after provider-specific status normalisation and used by the merge process.

---

# 5. Provider Models

## AeroTrack

Returns:

* Flight Number
* Provider Status
* Scheduled Departure
* Scheduled Arrival
* Actual Departure
* Actual Arrival
* Terminal
* Gate
* Delay Reason
* Last Updated (UTC)

## QuickFlight

Returns:

* Flight Number
* Provider Status
* Scheduled Departure
* Scheduled Arrival
* Last Updated (UTC)

---

# 6. Interface Contracts

## IFlightStatusProvider

Responsibilities:

* Retrieve flight information from a single provider.
* Return provider-specific flight data.
* Return no result when a flight is unavailable.
* Be independently replaceable through dependency injection.

Implementations:

* AeroTrackProvider
* QuickFlightProvider

---

## Flight Status Service

Responsibilities:

* Query all registered providers.
* Normalise provider responses.
* Apply merge rules.
* Return a unified FlightStatusResult.

---

# 7. API Contract

## Endpoint

`GET /flights/status`

### Query Parameters

| Parameter    | Required | Format     |
| ------------ | -------- | ---------- |
| flightNumber | Yes      | String     |
| date         | Yes      | yyyy-MM-dd |

### Responses

**200 OK**

Returns a unified flight status result.

**400 Bad Request**

Returned when:

* flight number is missing
* date is missing
* date format is invalid

---

# 8. Status Normalisation Rules

| Condition                 | Result              |
| ------------------------- | ------------------- |
| Cancelled                 | Cancelled           |
| Diverted                  | Diverted            |
| Difference ≤ 15 minutes   | OnTime              |
| Difference > 15 minutes   | Delayed             |
| No actual times available | Use provider status |
| Unknown provider status   | Unknown             |

---

# 9. Merge Rules

The application queries both providers concurrently.

| Scenario                       | Result                                               |
| ------------------------------ | ---------------------------------------------------- |
| Both providers return data     | Select the response with the latest `LastUpdatedUtc` |
| Only one provider returns data | Use that response                                    |
| Neither provider returns data  | Return `Unknown` with a clear message                |

---

# 10. Stub Coverage

The stub providers return deterministic responses covering multiple scenarios, including:

* OnTime
* Delayed
* Cancelled
* Diverted
* AeroTrack only
* QuickFlight only
* Conflicting provider responses
* No data from either provider

---

# 11. Frontend Behaviour

| State     | Behaviour                 |
| --------- | ------------------------- |
| Empty     | No result displayed       |
| Loading   | Display loading indicator |
| OnTime    | Green status              |
| Delayed   | Amber status              |
| Cancelled | Red status                |
| Diverted  | Red status                |
| Unknown   | Grey status               |
| Error     | Display API error message |

Provider-specific fields (terminal, gate, delay reason) are displayed only when available.

---

# 12. Design Principles

The solution follows a simple, maintainable architecture.

* **Single Responsibility Principle** – each component has one responsibility.
* **Open/Closed Principle** – new providers can be added without modifying existing business logic.
* **Dependency Inversion Principle** – services depend on abstractions rather than concrete implementations.
* **Dependency Injection** – provider implementations are registered and resolved through the .NET dependency injection container.

---

# 13. Assumptions

* The application runs completely offline.
* Provider responses are deterministic.
* No authentication or authorization is required.
* No database or persistence is required.
* All timestamps are handled in UTC.

---

# 14. Out of Scope

The following features are intentionally excluded:

* Real airline APIs
* Authentication and authorization
* Database persistence
* Caching
* Background processing
* Flight history
* Live push notifications
