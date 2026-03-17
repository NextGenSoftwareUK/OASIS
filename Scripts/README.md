# Scripts

Scripts for running WEB4 (ONODE) and WEB5 (STAR) APIs and their tests. All scripts that start both APIs do so **in serial** (WEB4 first, then WEB5).

**Windows:** Run from the repository root or this folder, e.g. `.\Scripts\run_web4_web5_unit_tests.ps1`. `.bat` files launch the matching PowerShell script.

**Linux / macOS:** Use the `.sh` launchers in this folder. Most `.sh` scripts call the same `.ps1` via PowerShell Core (pwsh); **start_web4_api.sh** and **start_web5_api.sh** are native bash (no pwsh required). Install PowerShell for the rest, then run for example:

```bash
# Install PowerShell Core (one-time, for scripts that invoke .ps1)
# Ubuntu/Debian:
sudo apt update && sudo apt install -y powershell
# macOS:
brew install powershell/tap/powershell

# Run a script (from repo root or Scripts folder)
./Scripts/start_web4_and_web5_apis.sh
./Scripts/run_web4_web5_harnesses.sh
# Start only one API (native bash, no pwsh needed):
./Scripts/start_web4_api.sh
./Scripts/start_web5_api.sh
```

Each Windows script has a matching `.sh`: same name with `.sh` instead of `.bat` (e.g. `run_web4_web5_harnesses.sh` → `run_web4_web5_harnesses.ps1`). Pass any arguments the same way: `./Scripts/run_web4_web5_harnesses.sh -Web5BaseUrl http://localhost:5556`. The tables below list `.ps1` / `.bat`; on Linux/macOS use the same name with `.sh` instead of `.bat`.

---

## MongoDB (required for WEB4/WEB5 APIs)

The OASIS WEB4 and WEB5 APIs use MongoDB at **localhost:27017**. If you see `Connection refused 127.0.0.1:27017` or `MongoConnectionException`, start MongoDB first.

| Script | Description |
|--------|-------------|
| **start_mongodb.sh** | Start MongoDB. On Linux with systemd, use `ENABLE_MONGO_ON_BOOT=1 ./Scripts/start_mongodb.sh` or `./Scripts/start_mongodb.sh --enable-boot` to also enable start on boot. |

**Install and enable MongoDB on Ubuntu/Debian (one-time):**

```bash
# Option A: Ubuntu's packaged MongoDB (simplest)
sudo apt update && sudo apt install -y mongodb
sudo systemctl start mongodb
sudo systemctl enable mongodb   # start on boot

# Option B: Official MongoDB Community Edition (newer version)
# See: https://www.mongodb.com/docs/manual/tutorial/install-mongodb-on-ubuntu/
# Then:
sudo systemctl start mongod
sudo systemctl enable mongod   # start on boot
```

**Start MongoDB (if already installed):**

```bash
./Scripts/start_mongodb.sh
# Or with systemd:
sudo systemctl start mongod    # or: mongodb
```

---

## Start / stop APIs

| Script | Description |
|--------|-------------|
| **start_web4_and_web5_apis.ps1** | Start WEB4 then WEB5 locally in serial. Saves process state to `.local_api_processes.json`. Use `-NoWait` to leave them running (e.g. for harnesses). |
| **start_web4_and_web5_apis.bat** | Launcher for the above. |
| **start_web4_and_web5_apis.sh** | Linux/macOS: launcher for the above (calls .ps1 via pwsh). |
| **stop_web4_and_web5_apis.ps1** / **.bat** | Stop APIs started by the start script (uses the state file). Use `-UsePortFallback` to also stop processes on typical API ports. |
| **stop_web4_and_web5_apis.sh** | Linux/macOS: launcher for the above (calls .ps1 via pwsh). |
| **start_web4_api.bat** / **.sh** | Start only the WEB4 (ONODE) API (port 5555). .sh is native bash. |
| **start_web5_api.bat** / **.sh** | Start only the WEB5 (STAR) API (port 5556). .sh is native bash. |

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
| **run_web4_web5_harnesses.ps1** / **.bat** | Run both API test harnesses (WEB5 then WEB4) against **already running** APIs (default: 5555, 5556). Optional `-Web4BaseUrl` / `-Web5BaseUrl`. Full output: `...\Test Results\test_results_web4.txt` and `test_results_web5.txt`. |
| **run_web4_web5_harnesses_with_apis.ps1** / **.bat** | Start both APIs in serial, run both harnesses (WEB5 then WEB4), then stop the APIs. |
| **run_web5_harness.ps1** / **.bat** | Run only the WEB5 (STAR) test harness. Optional `-Web5BaseUrl`. API should already be running. |

---

## All tests (unit + integration + harnesses)

| Script | Description |
|--------|-------------|
| **run_web4_web5_all_tests.ps1** / **.bat** | Run in order: (1) unit tests, (2) integration tests, (3) harnesses (APIs started only for the harness phase). Prints a final summary and exits with failure if any phase fails. |

---

## STAR API Client (OASIS Omniverse)

| Script | Description |
|--------|-------------|
| **run_starapi_client_test_suite.ps1** / **.bat** | Run the STAR API **Client** suite: unit tests, integration tests, and console harness (Omniverse STARAPIClient). Use `-HarnessMode fake` (default), `real-local` (starts WEB4 then WEB5 in serial), or `real-live`. |

---

## Linux/macOS parity with Windows

Every Windows (`.bat`) script has a matching `.sh` with the same name (e.g. `run_web4_web5_harnesses.bat` → `run_web4_web5_harnesses.sh`). Most `.sh` invoke the same `.ps1` via `pwsh`; **start_web4_api.sh** and **start_web5_api.sh** are native bash (no PowerShell required).

| Windows (.bat) | Linux/macOS (.sh) | Notes |
|----------------|-------------------|--------|
| start_web4_and_web5_apis.bat | start_web4_and_web5_apis.sh | Calls .ps1 |
| stop_web4_and_web5_apis.bat | stop_web4_and_web5_apis.sh | Calls .ps1 |
| start_web4_api.bat | start_web4_api.sh | Native bash |
| start_web5_api.bat | start_web5_api.sh | Native bash |
| run_web4_web5_unit_tests.bat | run_web4_web5_unit_tests.sh | Calls .ps1 |
| run_web4_web5_unit_tests_with_apis.bat | run_web4_web5_unit_tests_with_apis.sh | Calls .ps1 |
| run_web4_web5_integration_tests.bat | run_web4_web5_integration_tests.sh | Calls .ps1 |
| run_web4_web5_integration_tests_with_apis.bat | run_web4_web5_integration_tests_with_apis.sh | Calls .ps1 |
| run_web4_web5_harnesses.bat | run_web4_web5_harnesses.sh | Calls .ps1 |
| run_web4_web5_harnesses_with_apis.bat | run_web4_web5_harnesses_with_apis.sh | Calls .ps1 |
| run_web5_harness.bat | run_web5_harness.sh | Calls .ps1 |
| run_web4_web5_all_tests.bat | run_web4_web5_all_tests.sh | Calls .ps1 |
| run_starapi_client_test_suite.bat | run_starapi_client_test_suite.sh | Calls .ps1 |

**Smart Contracts** (subfolder): each `.bat` has a `.sh` with the same name (native bash implementations).

- **deploy-all-contracts.ps1** / **.bat** / **.sh**
- **deploy-all-evm-testnet.ps1** / **.bat** / **.sh**
- **deploy-all-evm-mainnet.ps1** / **.bat** / **.sh**
- **deploy-master.ps1** / **.bat** / **.sh**
