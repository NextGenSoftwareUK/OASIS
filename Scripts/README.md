# Scripts

Scripts for running WEB4 (ONODE) and WEB5 (STAR) APIs and their tests. All scripts that start both APIs do so **in serial** (WEB4 first, then WEB5).

Run from the repository root, e.g. `.\Scripts\run_web4_web5_unit_tests.ps1`, or from this folder. `.bat` files launch the matching PowerShell script.

---

## Start / stop APIs

| Script | Description |
|--------|-------------|
| **start_web4_and_web5_apis.ps1** | Start WEB4 then WEB5 locally in serial. Saves process state to `.local_api_processes.json`. Use `-NoWait` to leave them running (e.g. for harnesses). |
| **start_web4_and_web5_apis.bat** | Launcher for the above. |
| **stop_web4_and_web5_apis.ps1** | Stop APIs started by the start script (uses the state file). Use `-UsePortFallback` to also stop processes on typical API ports. |
| **start_web4_api.bat** | Start only the WEB4 (ONODE) API. |
| **start_web5_api.bat** | Start only the WEB5 (STAR) API. |

---

## Unit tests

| Script | Description |
|--------|-------------|
| **run_web4_web5_unit_tests.ps1** / **.bat** | Run WEB4 and WEB5 API unit tests in serial (no APIs started). |
| **run_web4_web5_unit_tests_with_apis.ps1** / **.bat** | Start both APIs in serial, run both unit test projects, then stop the APIs. |

---

## Integration tests

| Script | Description |
|--------|-------------|
| **run_web4_web5_integration_tests.ps1** / **.bat** | Run WEB4 and WEB5 API integration tests in serial (no APIs started). |
| **run_web4_web5_integration_tests_with_apis.ps1** / **.bat** | Start both APIs in serial, run both integration test projects, then stop the APIs. |

---

## Test harnesses

| Script | Description |
|--------|-------------|
| **run_web4_web5_harnesses.ps1** | Run both API test harnesses (WEB5 then WEB4) against **already running** APIs. Optional `-Web4BaseUrl` / `-Web5BaseUrl`. |
| **run_web4_web5_harnesses_with_apis.ps1** | Start both APIs in serial, run both harnesses (WEB5 then WEB4), then stop the APIs. |
| **run_web5_harness.ps1** | Run only the WEB5 (STAR) test harness. Optional `-Web5BaseUrl`. API should already be running. |

---

## All tests (unit + integration + harnesses)

| Script | Description |
|--------|-------------|
| **run_web4_web5_all_tests.ps1** / **.bat** | Run in order: (1) unit tests, (2) integration tests, (3) harnesses (APIs started only for the harness phase). Prints a final summary and exits with failure if any phase fails. |

---

## STAR API Client (OASIS Omniverse)

| Script | Description |
|--------|-------------|
| **run_starapi_client_test_suite.ps1** | Run the STAR API **Client** suite: unit tests, integration tests, and console harness (Omniverse STARAPIClient). Use `-HarnessMode fake` (default), `real-local` (starts WEB4 then WEB5 in serial), or `real-live`. |

---

## Other

The **Smart Contracts** subfolder contains deployment and contract scripts (e.g. deploy-all-contracts, deploy-master, deploy-all-evm-testnet).
