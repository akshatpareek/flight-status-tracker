# Flight Status Tracker — SkyRoute Support Tool

This repository contains the complete implementation of the Flight Status Lookup tool for the SkyRoute platform. The application aggregates, normalizes, and merges real-time flight status data from two mock data providers (**AeroTrack** and **QuickFlight**) to assist support agents.

---

## System Overview & Architecture

The application is built completely offline, utilizing embedded JSON mock data stubs to simulate downstream provider APIs.

* **Backend**: .NET 9 Minimal API using the **Strategy Pattern** for providers and a **Pure Static Normalization Engine**.
* **Frontend**: Responsive, modern dark-themed HTML5/ES6 application with robust validation and error handling.
* **Testing**: 19 xUnit tests verifying normalization rules, merge logic, and API endpoints.

---

## 🚀 Getting Started (Quick Run)

### Step 1: Trust your local .NET HTTPS Dev Certificate (Required)
Since the frontend communicates with the backend over **HTTPS** (`https://localhost:7262`), your browser will block requests unless your machine trusts the local .NET development certificate.

Run this command in your terminal:
```powershell
dotnet dev-certs https --trust
```

---

### Step 2: Run the Backend API
Start the backend using the default HTTPS launch profile:
1. Navigate to the repository root directory.
2. Run the project:
   ```powershell
   dotnet run --project FlightStatus.Api --launch-profile https
   ```
3. Once running, open `https://localhost:7262/` in your browser. You will see a custom dark-themed API landing page confirming the service is live.

---

### Step 3: Run the Frontend UI
1. Navigate to the `flight-status-ui/` directory.
2. Open `index.html` directly in any modern browser (double-click the file).
3. Check the status of a flight!

---

## 🧪 Running Tests

To run the xUnit test suite (19 tests covering business rules, edge cases, and parallel endpoint execution):

From the repository root, run:
```powershell
dotnet test FlightStatus.Tests/FlightStatus.Tests.csproj -v normal
```

---

## 📂 Repository Structure

```text
flight-status/
├── README.md                 # Setup, run steps, assumptions (this file)
├── spec.md                   # Data models and interface contracts
├── prompts.md                # Log of AI prompts and human design decisions
├── reflection.md             # Review of lessons learned and future improvements
├── FlightStatus.Api/         # Backend Minimal API Project
│   ├── Domain/               # Enums, interfaces, and core domain models
│   ├── Application/          # Business logic (Normaliser engine)
│   ├── Infrastructure/       # Providers, helper readers, and JSON stubs
│   ├── Api/                  # Endpoint routers and contracts
│   └── Program.cs            # DI registration and middleware configuration
├── FlightStatus.Tests/       # xUnit Unit & Integration Tests
└── flight-status-ui/         # Frontend Web Application (HTML, CSS, JS)
```

---

## 🔎 Scenario Test Matrix (Try These)

Use these flight codes and dates in the frontend form to test different scenarios:

| Flight Number | Date | Expected Status | Provider Source(s) | Scenario Tested |
|---|---|---|---|---|
| **BA493** | `2025-08-15` | **Delayed** | AeroTrack & QuickFlight | Merging (AeroTrack is selected as it has the newer `lastUpdatedUtc`). |
| **LH100** | `2025-08-15` | **OnTime** | AeroTrack & QuickFlight | Match (both providers return OnTime). |
| **EK202** | `2025-08-15` | **Cancelled** | AeroTrack & QuickFlight | Match (both providers return Cancelled). |
| **TK801** | `2025-08-15` | **Diverted** | AeroTrack only | Graceful single-provider resolution. |
| **FR550** | `2025-08-15` | **Delayed** | QuickFlight only | Graceful single-provider resolution. |
| **XX999** | `2025-08-15` | **Unknown** | None | No data found fallback message. |

### Validation Edge Cases to Test:
* **Invalid Date Range**: Try entering `11/11/1111` or any year outside **2020 to 2030**. The UI will display a red validation warning.
* **Invalid Flight Format**: Try searching for `A` or `1234567`. The UI blocks requests that do not match the standard IATA flight format.

---

## 🛠 Design Assumptions & Decisions
* **Deterministic Stub Resolver**: Mock provider files are loaded dynamically from embedded resources in the `FlightStatus.Api` assembly.
* **Global Exception Shielding**: Any unhandled backend failures return a consistent `ErrorResponse` payload containing a unique `TraceId` for debugging.
* **Parallel Querying**: Providers are queried concurrently using `Task.WhenAll` to maximize responsiveness. If one fails, the other is still allowed to load.
* **No Database or Persistence**: Fully self-contained, offline-compatible solution.