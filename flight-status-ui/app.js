// ── Configuration ─────────────────────────────────────────────────────────────
const API_BASE = "https://localhost:7262";

// Date range mirrors the min/max attributes on the date input
const MIN_FLIGHT_DATE = new Date("2020-01-01");
const MAX_FLIGHT_DATE = new Date("2030-12-31");

// IATA flight number pattern: 2–3 letter airline code + 1–4 digits (optional suffix letter)
const FLIGHT_NUMBER_PATTERN = /^[A-Za-z]{2,3}\d{1,4}[A-Za-z]?$/;

// ── DOM references ─────────────────────────────────────────────────────────────
const flightForm       = document.getElementById("flight-form");
const flightNumberInput = document.getElementById("flightNumber");
const dateInput        = document.getElementById("date");
const searchBtn        = document.getElementById("search-btn");
const btnText          = searchBtn.querySelector(".btn-text");
const btnSpinner       = searchBtn.querySelector(".btn-spinner");
const validationError  = document.getElementById("validation-error");
const errorBanner      = document.getElementById("error-banner");
const resultSection    = document.getElementById("result");

// ── Status icon map ────────────────────────────────────────────────────────────
const STATUS_ICONS = {
    OnTime:    "✓",
    Delayed:   "⚠",
    Cancelled: "✕",
    Diverted:  "↗",
    Unknown:   "?",
};

// ── Form submission ────────────────────────────────────────────────────────────
flightForm.addEventListener("submit", async (submitEvent) => {
    submitEvent.preventDefault();
    clearMessages();

    const flightNumber = flightNumberInput.value.trim().toUpperCase();
    const dateValue    = dateInput.value; // yyyy-MM-dd from date input

    const validationMessage = validateInputs(flightNumber, dateValue);
    if (validationMessage) {
        showValidationError(validationMessage);
        return;
    }

    setLoadingState(true);

    try {
        const requestUrl = buildRequestUrl(flightNumber, dateValue);
        const response   = await fetch(requestUrl);

        if (!response.ok) {
            const apiErrorMessage = await extractErrorMessage(response);
            showErrorBanner(apiErrorMessage);
            return;
        }

        const flightData = await response.json();
        renderResultCard(flightData);
    } catch (networkError) {
        showErrorBanner(
            "Could not reach the Flight Status API. " +
            "Make sure the backend is running on http://localhost:5037."
        );
    } finally {
        setLoadingState(false);
    }
});

// ── Input validation ───────────────────────────────────────────────────────────
function validateInputs(flightNumber, dateValue) {
    if (!flightNumber) {
        return "Please enter a flight number.";
    }

    if (!FLIGHT_NUMBER_PATTERN.test(flightNumber)) {
        return "Flight number must follow the IATA format — e.g. BA493, LH100, EK202.";
    }

    if (!dateValue) {
        return "Please select a flight date.";
    }

    const selectedDate = new Date(dateValue);
    if (selectedDate < MIN_FLIGHT_DATE || selectedDate > MAX_FLIGHT_DATE) {
        return "Please select a date between 2020 and 2030.";
    }

    return null; // Validation passed
}

// ── API helpers ────────────────────────────────────────────────────────────────
function buildRequestUrl(flightNumber, dateValue) {
    return `${API_BASE}/flights/status` +
           `?flightNumber=${encodeURIComponent(flightNumber)}` +
           `&date=${encodeURIComponent(dateValue)}`;
}

async function extractErrorMessage(response) {
    try {
        // Try to read the structured error body from the API
        const errorBody = await response.json();
        return errorBody.message || `Request failed (${response.status}).`;
    } catch {
        return `Request failed with status ${response.status} ${response.statusText}.`;
    }
}

// ── Loading state ──────────────────────────────────────────────────────────────
function setLoadingState(isLoading) {
    searchBtn.disabled = isLoading;
    btnText.textContent = isLoading ? "Searching…" : "Search";
    btnSpinner.classList.toggle("hidden", !isLoading);
    if (isLoading) resultSection.innerHTML = ""; // Only clear on start, not on finish
}

// ── Message helpers ────────────────────────────────────────────────────────────
function clearMessages() {
    validationError.textContent = "";
    validationError.classList.add("hidden");
    errorBanner.textContent = "";
    errorBanner.classList.add("hidden");
    resultSection.innerHTML = "";
}

function showValidationError(message) {
    validationError.textContent = message;
    validationError.classList.remove("hidden");
}

function showErrorBanner(message) {
    errorBanner.textContent = message;
    errorBanner.classList.remove("hidden");
}

// ── Result rendering ───────────────────────────────────────────────────────────
function renderResultCard(flightData) {
    const statusIcon  = STATUS_ICONS[flightData.status] ?? "?";
    const hasMessage  = flightData.message && flightData.message.trim().length > 0;

    const cardElement = document.createElement("div");
    cardElement.className = "result-card";

    cardElement.innerHTML = `
        <div class="result-header">
            <span class="result-flight-number">${escapeHtml(flightData.flightNumber)}</span>
            <span class="status-badge status-${escapeHtml(flightData.status)}">
                ${statusIcon} ${escapeHtml(flightData.status)}
            </span>
        </div>

        <div class="result-section-title">Schedule</div>
        <div class="result-rows">
            <div class="result-row">
                <span class="result-label">Scheduled Departure</span>
                <span class="result-value">${formatDateTime(flightData.scheduledDeparture)}</span>
            </div>
            <div class="result-row">
                <span class="result-label">Scheduled Arrival</span>
                <span class="result-value">${formatDateTime(flightData.scheduledArrival)}</span>
            </div>
            ${renderOptionalRow("Actual Departure",  formatDateTime(flightData.actualDeparture))}
            ${renderOptionalRow("Actual Arrival",    formatDateTime(flightData.actualArrival))}
        </div>

        ${buildAeroTrackSection(flightData)}

        <hr class="result-divider" />
        <div class="result-rows">
            <div class="result-row">
                <span class="result-label">Last Updated (UTC)</span>
                <span class="result-value">${formatDateTime(flightData.lastUpdatedUtc)}</span>
            </div>
        </div>

        ${hasMessage ? `<div class="result-message">${escapeHtml(flightData.message)}</div>` : ""}
    `;

    resultSection.appendChild(cardElement);
}

/**
 * Renders the AeroTrack-only section (terminal, gate, delay reason) only when
 * at least one of those fields is present — as required by the spec.
 */
function buildAeroTrackSection(flightData) {
    const terminalRow   = renderOptionalRow("Terminal",     escapeHtml(flightData.terminal ?? ""));
    const gateRow       = renderOptionalRow("Gate",         escapeHtml(flightData.gate ?? ""));
    const delayRow      = renderOptionalRow("Delay Reason", escapeHtml(flightData.delayReason ?? ""));

    const hasAeroTrackData = flightData.terminal || flightData.gate || flightData.delayReason;
    if (!hasAeroTrackData) return "";

    return `
        <hr class="result-divider" />
        <div class="result-section-title">Gate &amp; Terminal</div>
        <div class="result-rows">
            ${terminalRow}
            ${gateRow}
            ${delayRow}
        </div>
    `;
}

function renderOptionalRow(label, value) {
    if (!value || value.trim() === "") return "";
    return `
        <div class="result-row">
            <span class="result-label">${label}</span>
            <span class="result-value">${value}</span>
        </div>`;
}

// ── Formatting helpers ─────────────────────────────────────────────────────────
function formatDateTime(isoString) {
    if (!isoString) return "";
    const parsedDate = new Date(isoString);
    if (isNaN(parsedDate.getTime())) return isoString;
    // Detect DateTimeOffset.MinValue (0001-01-01) and render as N/A
    if (parsedDate.getFullYear() <= 1) return "N/A";
    return parsedDate.toLocaleString(undefined, {
        dateStyle: "medium",
        timeStyle: "short",
    });
}

function escapeHtml(rawString) {
    if (!rawString) return "";
    return String(rawString)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;");
}
