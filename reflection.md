# Flight Status Tracker — Reflection & Future Improvements

This document reflects on the architecture of the Flight Status Tracker and details what could be improved, extended, or optimized with more time.

---

## 1. Architectural & Backend Improvements

### 1.1 In-Memory Caching
* **Current State**: Stubs are deserialized and loaded from assembly embedded resources during the constructor initialization of `AeroTrackProvider` and `QuickFlightProvider`.
* **Future Improvement**: Introduce a caching layer (using `IMemoryCache` or a Redis wrapper in production) around provider results. Since flight status changes periodically, a short-lived cache (e.g., 2–5 minutes sliding expiration) would reduce IO lookup and computation overhead.

### 1.2 Resilient API Retries & Circuit Breaker
* **Current State**: Endpoint queries both providers in parallel using `Task.WhenAll`. If a provider throws `ProviderException`, we log a warning and skip that provider's result.
* **Future Improvement**: Add policy-based retries and circuit breakers (using **Polly**) for downstream provider requests. If a provider starts consistently failing or times out, the circuit breaker would open instantly to prevent holding up resource threads.

### 1.3 Object Mapping Abstraction (Mappers)
* **Current State**: Model conversions between raw provider payloads (`AeroTrackResponse` / `QuickFlightResponse`) and the domain model `NormalisedFlightData` are currently mapped manually via inline assignments inside `FlightStatusNormaliser`.
* **Future Improvement**: Utilize an object-to-object mapping framework (like **AutoMapper** or source-generated **Mapperly**) or structure explicit extension methods (e.g., `ToDomain()`). This abstraction eliminates manual property assignments, ensures clean code layout, and simplifies contract versioning when downstream response shapes change.

---

## 2. Testing Improvements

### 2.1 UI End-to-End (E2E) Testing
* **Current State**: We have xUnit tests verifying normalization rules, merge logic, and endpoint responses.
* **Future Improvement**: Add browser-based E2E automation tests using **Playwright** or **Cypress** to test:
  * Input validations (blocking invalid codes or out-of-range dates).
  * Error banners rendering on network failure.
  * Conditional gate/terminal rendering.
  * Responsive layouts across mobile/desktop screen views.

---

## 3. Frontend & UX Improvements

### 3.1 Migration to Component-Based Frameworks
* **Current State**: Vanilla HTML, CSS, and JS (No external libraries, satisfying out-of-scope constraints).
* **Future Improvement**: Port the UI to a component-driven framework like **React** or **Angular** with state management. This would enable cleaner encapsulation of validation logic, loaders, and result cards.

---

## 4. AI Tooling & Prompting Reflection

A critical review of the prompting workflow and how AI-human collaboration could be optimized in future iterations.

### 4.1 What Worked Well
* **Iterative Problem Solving**: Presenting compiler/runtime logs directly (e.g. `NullReferenceException` in `LoadFlightStubs`) allowed the AI to instantly diagnose deserialization mismatches against the raw mock data shapes.
* **Incremental Specifications**: Providing the `spec.md` contract text allowed the AI to quickly spot where the initial implementation deviated (like the name of the `EnumFlightStatus` or the validation payloads) and align them without manually editing dozens of properties.

### 4.2 Key Lessons & Prompting Improvements
* **Clear Permission Parameters**: The build process was initially stalled because of sandbox command permissions. Providing explicit permissions or instruction rules early on would allow the agent to run and verify tests automatically without blocking execution flow.
* **Pre-aligning Vocabulary**: To save refactoring time, establishing custom enum names and contract terminology (like `EnumFlightStatus` vs `FlightStatus`) in the very first prompt prevents double-work when finalizing specifications.
* **Granular UI Requirements**: Describing validation boundaries (such as invalid date ranges or styling conventions) in early prompts helps the model build a complete interface on the first pass, reducing the need for debugging UI rendering states.