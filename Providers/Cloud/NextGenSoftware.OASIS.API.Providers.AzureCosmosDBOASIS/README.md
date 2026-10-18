# NextGen Software WEB4 OASIS API AzureCosmosDB OASIS Provider

WEB4 OASIS API AzureCosmosDB OASIS Provider is one of the many WEB2/WEB3 providers that plug in and integtate into the OASIS.

Initial Release targeting OASIS API v2.3.1 (Current LIVE REST version): \
https://api.oasisweb4.one

Full documentation and source code can be found here: \
https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK

Make sure you check out the Overview and Benefits Of Building On The WEB 4 OASIS API sections above! :) Thank you!

## Current verification and limitations

Uses Microsoft.Azure.Cosmos SDK, canonical OASIS serialization and shared holon graph planning/projection. Native evidence uses the retained local Cosmos DB emulator, not a hosted Azure account; scoped tests do not certify full-provider or hosted-release completion.

Historical versions are currently rejected: this provider stores current records only. Profile/detail mutations span separate containers and are not a single atomic transaction; partial/uncertain mutations require inspection before replay. History, concurrency/ETag, recovery, full interface-option and hosted/release verification remain open. Current evidence and gates: `Docs/Devs/HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md`.

From the existing repository checkout: `pwsh -NoProfile -File Scripts/TestHosts/Run-ReusableCosmosDBTests.ps1`. Reuses the exact retained container/image and cached dependencies, leases the environment exclusively and stops its owned service afterward. No installation/download/reset/runtime replacement. Fixtures reuse empty native/SQLite schemas; nonempty or unexpected fixtures stop testing for inspection rather than being deleted or overwritten.

Add `-InspectRetainedFixture` for read-only fixture counts and the SQLite file hash. This diagnostic does not certify provider operations or emit keys/record payloads. Ordinary and diagnostic results use separate fixed reports under `TestResults/CosmosDBCurrent`.
