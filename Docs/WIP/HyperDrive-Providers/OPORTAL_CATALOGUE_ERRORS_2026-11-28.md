# Real OAPP catalogue/detail errors

OAPP REST list/detail previously fabricated empty/null success when the legacy test-data flag was enabled. Removed those paths and return the existing declared400 for real manager errors; a direct detail read now ensures the same runtime boot as catalogue/create. No route/parameter/result schema addition; SDK/MCP/Postman consume the existing error contract.

Root cause also existed in ONODE.Core shared STARNET Load/LoadAsync: an empty selection returned IsError=false/null and a genuine backend failure message was overwritten with No Holon Found. One common mapper now preserves underlying errors and uses the standard RecordNotFound result for a real empty detail selection, sync and async. No null-success fallback or alternative provider path.

Accepted native scope (CI46132041520): actual current WEB5 SDK cold missing-ID read (before any OAPP list/create), declared400/IsError/RecordNotFound, using real native Mongo and legacy test-data setting explicitly true in this isolated WEB5 process. Tiny owned HTML document loads the actual SDK asset; API calls are real. Existing populated Map/Launcher46126474607is retained rather than rerun. No shared Atlas, source DNA default/Railway setting changes, real funds, local downloads/runtime/build/database/output or new owner action. Production submodule pins are qualified for the next scoped Development batch.

46131361971compiled the real ONODE.Core/STAR changes, then the SDK probe was blocked by the existing browser transport guard: it constructed its client with the private proxy origin, whereas this harness admits the shipping SDK default host and transparently redirects that host to the owned backend. Use the real SDK default URL under that existing mandatory redirect; do not broaden egress or call a shared backend. The network result had no HTTP status, so it was not evidence of an API error-contract failure. Owned cleanup passed; only this unaccepted probe is retried.


## 2026-11-28 — real OAPP error acceptance

CI46132041520passed1/1new API contract probe through the actual current WEB5 SDK and shipping WEB5 source2e8ae614, ONODE.Coref2c6b219/STARfdc8e3a3. A cold missing-ID read returns declared400, IsError=true, OASIS_RECORD_NOT_FOUND and No Holon Found even with the isolated legacy test-data setting enabled. Original populated Map/Launcher46126474607was not rerun. Artifact15257639687(3232bytes) inspected in RAM: all5Startup cleanup conditions true; publish/browser/cache/TLS outputs absent. No shared Atlas, source DNA/Railway settings change, local runtime/download/build/database/results or new owner action. Native storage baseline123/163unchanged. Whole OPORTAL and global HyperDrive gates remain open.
