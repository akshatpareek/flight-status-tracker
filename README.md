# Flight Status Tracker

A Flight Status lookup application for the SkyRoute platform.

The application allows a support agent to search for a flight using its flight number and travel date. It retrieves data from two deterministic stub providers, normalises provider-specific responses into a unified flight status model, applies merge rules, and returns a single result for display.

---

# Project Goals

* Build a Flight Status lookup feature using .NET Minimal API and React.
* Query two independent stub providers.
* Normalise provider-specific status values into a unified status model.
* Merge provider responses using deterministic business rules.
* Produce a maintainable, extensible, and testable solution.
* Document AI-assisted development throughout the project.

---

# Technology Stack

| Component | Technology                                   |
| --------- | -------------------------------------------- |
| Backend   | .NET 9 Minimal API (compatible with .NET 8+) |
| Frontend  | Angular                                        |
| Testing   | xUnit                                        |
| Language  | C#                                           |

---

# Repository Structure

```
flight-status-tracker/
│
├── README.md
├── spec.md
├── FlightStatus.Api/
├── FlightStatus.Tests/
├── flight-status-ui/
├── prompts.md
└── reflection.md
```

---

# Current Status

This repository is being developed incrementally.

Current progress:

* Repository initialized
* Solution specification completed
* Implementation pending

---

# Assumptions

* The application runs completely offline.
* Stub providers return deterministic responses.
* No authentication or authorization is required.
* No database or persistence is required.
* AI usage will be documented in `prompts.md`.
* Design decisions are documented in `spec.md` before implementation begins.

---

# Documentation

The repository includes the following documentation:

* **README.md** – Project overview and setup information.
* **spec.md** – System design, architecture, business rules, and interface contracts.
* **prompts.md** – Significant AI prompts and design decisions used during development.
* **reflection.md** – Improvements and future enhancements identified after implementation.

---

# Project Status

Work in Progress