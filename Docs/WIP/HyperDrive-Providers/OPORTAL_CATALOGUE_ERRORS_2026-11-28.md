# Real OAPP catalogue/detail errors

OAPP REST list/detail previously fabricated empty/null success when the legacy test-data flag was enabled. Removed those paths and return the existing declared400 for real manager errors; a direct detail read now ensures the same runtime boot as catalogue/create. No route/parameter/result schema addition; SDK/MCP/Postman consume the existing error contract.

Root cause also existed in ONODE.Core shared STARNET Load/LoadAsync: an empty selection returned IsError=false/null and a genuine backend failure message was overwritten with No Holon Found. One common mapper now preserves underlying errors and uses the standard RecordNotFound result for a real empty detail selection, sync and async. No null-success fallback or alternative provider path.

Native scope pending: actual current WEB5 SDK cold missing-ID read (before any OAPP list/create), declared400/IsError/RecordNotFound, using real native Mongo and legacy test-data setting explicitly true in this isolated WEB5 process. Tiny owned HTML document loads the actual SDK asset; API calls are real. Existing populated Map/Launcher46126474607is retained rather than rerun. No shared Atlas, source DNA default/Railway setting changes, real funds, local downloads/runtime/build/database/output or new owner action. Production submodule pins remain unmerged until acceptance.
