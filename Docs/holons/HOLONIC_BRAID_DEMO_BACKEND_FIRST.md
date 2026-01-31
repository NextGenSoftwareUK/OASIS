# Holonic BRAID Demo: Backend-First Architecture

**Date:** January 2025  
**Status:** Design + implementation

---

## Principle

The Holonic BRAID demo is **backend-driven**. The frontend is a visualization and control layer; all real work (graph lookup, storage, compute/storage accounting) happens in the ONODE WebAPI. Real processes run on the backend for both **Standard** and **Holonic** approaches.

---

## Backend (ONODE WebAPI)

### Endpoints

| Method | Endpoint | Purpose |
|--------|----------|---------|
| `POST` | `/api/braid/run-task` | Run one task on Standard or Holonic; backend does the work and returns result + updated metrics |
| `GET`  | `/api/braid/metrics`  | Return current compute/storage totals for Standard and Holonic (real-time tab) |

### POST /api/braid/run-task

**Request body:**

```json
{
  "approach": "standard" | "holonic",
  "taskType": "gsm8k_arithmetic" | "python_function" | "multi_step_instruction",
  "graphLibraryId": "guid-or-null"
}
```

- `approach`: which side to run (`standard` or `holonic`).
- `taskType`: task type id (realistic names; see below).
- `graphLibraryId`: required when `approach == "holonic"`. Parent holon id for the Graph Library. Omitted or null for Standard.

**Backend behaviour**

- **Standard:** Every run counts as a new graph. Backend does not call OASIS. It increments in-memory totals: graph count, storage bytes (e.g. ~500 bytes per graph), compute units (e.g. 1 generation + 1 solve per run). Returns `reused: false`, updated totals.
- **Holonic:** Backend calls `HolonManager.LoadHolonsForParentAsync(graphLibraryId, ...)`, filters children by `metaData["task_domain"] == taskType`. If any match → **reused**: return `reused: true`, compute = solver only (e.g. 0.1 units), no new storage. If none match → **generated**: create child holon with `ParentHolonId = graphLibraryId`, `MetaData = { mermaid_code, task_domain, usage_count, graph_type }`, call `HolonManager.SaveHolonAsync(...)`, then increment holonic totals (graph count, storage, compute). Return `reused: false`, new graph id.

**Response:**

```json
{
  "reused": false,
  "taskType": "gsm8k_arithmetic",
  "graphId": "guid-if-generated",
  "storageBytes": 512,
  "computeUnits": 1.1,
  "standard": { "graphCount": 5, "storageBytes": 2560, "computeUnits": 5.5 },
  "holonic": { "graphCount": 2, "storageBytes": 1024, "computeUnits": 1.2, "reusedCount": 1, "generatedCount": 1 }
}
```

Cumulative totals (`standard`, `holonic`) are the live metrics after this run. The backend keeps running totals in memory (or a shared store) so `GET /api/braid/metrics` and the next run reflect the same state.

### GET /api/braid/metrics

Returns current cumulative metrics for both approaches so the frontend can drive a **real-time compute/storage** tab.

**Response:**

```json
{
  "standard": { "graphCount": 5, "storageBytes": 2560, "computeUnits": 5.5 },
  "holonic": { "graphCount": 2, "storageBytes": 1024, "computeUnits": 1.2, "reusedCount": 1, "generatedCount": 1 }
}
```

---

## Task types (realistic)

Replace generic "math / code / instruction" with task types that feel real and match BRAID-style use cases:

| taskType                 | Label (for UI)           | Sample input (for UI) |
|--------------------------|--------------------------|------------------------|
| `gsm8k_arithmetic`       | GSM8K arithmetic         | "Janet has 3 boxes of 4 oranges. She buys 2 more. How many?" |
| `python_function`        | Python function          | "Write a function that returns the nth Fibonacci number." |
| `multi_step_instruction` | Multi-step instruction   | "Summarise the text, extract named entities, then format as JSON." |

Backend uses `taskType` as `task_domain` in holon `MetaData`. No LLM calls in Phase 1; the backend only does lookup/save and compute/storage accounting.

---

## Real-time compute/storage tab

Frontend should show:

- **Standard:** graph count, storage (bytes/KiB), compute units — from `GET /api/braid/metrics` or from the last `POST /api/braid/run-task` response.
- **Holonic:** graph count, storage, compute units, reused count, generated count — same source.

Updates in real time: after each "Run task" the frontend calls the backend, then refreshes the metrics panel (and optionally the graph lists) from the response. Optionally, a "Live" panel can poll `GET /api/braid/metrics` every N seconds when the BRAID tab is visible.

---

## Real processes

- **Standard:** Backend treats each `run-task` call as one process: it updates Standard totals and returns. No OASIS calls. Same effect as many separate processes each generating a graph with no sharing.
- **Holonic:** Backend treats each `run-task` call as one process: it uses OASIS (HolonManager) to load the Graph Library children and either reuse a graph or create and save a new one. So real processes = real API calls to load-holons-for-parent and save-holon.

Future Phase 2 can add actual worker processes (Node/Python) that call these same endpoints; the backend and metrics design already support that.

---

## Frontend changes

1. **Use backend when available:** If the demo is in API mode (authenticated), "Run task" calls `POST /api/braid/run-task` with `approach: "standard"` and `approach: "holonic"` (or one combined call that runs both; see below). Display results and totals from the response.
2. **Task types:** Dropdown uses `taskType` and shows `label`; optionally show `sampleInput` for the selected type.
3. **Real-time metrics tab:** New panel or strip showing Standard vs Holonic storage and compute, fed from run-task responses and/or `GET /api/braid/metrics`.
4. **Fallback:** In demo (localStorage) mode, keep current in-browser logic so the demo works without the backend. When backend is present, prefer backend.

Optional: one "Run task" button that sends two requests (Standard + Holonic) with the same `taskType` and displays both sides updating, so it’s visually clear that both approaches are driven by real backend runs.

---

## Files to add/change

- **ONODE WebAPI:**  
  - `Controllers/BraidController.cs` — `POST /api/braid/run-task`, `GET /api/braid/metrics`.  
  - `Models/Braid/RunBraidTaskRequest.cs`, `RunBraidTaskResponse.cs`, `BraidMetricsResponse.cs` (or inline DTOs).  
  - In-memory store for cumulative metrics (e.g. static class or singleton), keyed by "demo" or by AvatarId if we want per-user metrics later.
- **Holonic demo (frontend):**  
  - `oasis-api.js` — add `runBraidTask(approach, taskType, graphLibraryId)` and `getBraidMetrics()`.  
  - BRAID tab — call backend when in API mode; add real-time compute/storage panel; use realistic task type labels and optional sample inputs.

---

*This doc is the canonical backend-first design for the Holonic BRAID demo. Implementation follows it so that compute/storage and reuse are grounded in real backend processes and OASIS.*
