# NextGen Software WEB4 OASIS API AzureCosmosDB OASIS Provider

WEB4 OASIS API AzureCosmosDB OASIS Provider is one of the many WEB2/WEB3 providers that plug in and integtate into the OASIS.

Username/email exports resolve the current avatar identity; `version` selects exported holon roots, not independent avatar history. Use the current username/email when exporting older holons. Linked children keep independent versions. Explicit versioned avatar lookups retain historical identity fields. Negative export versions fail before identity repository reads. Owned exports report returned-root `LoadedCount`, including zero for an empty result; this count is not Cosmos request-unit usage or cost.

Successful point/identity reads count one record. Collection, parent, metadata, type-filtered and nearby reads count returned roots, not scanned records or nested children; empty collections stay loaded with count zero. Search counts returned avatar/holon roots (`NumberOfResults`). Failed graph projection clears any earlier point-read count, and deletion does not publish its internal lookup as a successful read. These response annotations do not change query, persistence, history, native request charges or retry policy.

Initial Release targeting OASIS API v2.3.1 (Current LIVE REST version): \
https://api.oasisweb4.one

Full documentation and source code can be found here: \
https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK

Make sure you check out the Overview and Benefits Of Building On The WEB 4 OASIS API sections above! :) Thank you!

## Current verification and limitations

Cosmos stores JSON numbers within binary64 precision. This adapter explicitly uses shared Binary64JsonNumberCodec for numbers that cannot survive that representation. Its reserved root `_oasis_exact_numbers` envelope (`schema: binary64-v1`) binds exact numeric literals to structural paths; those native values alone become strings. Point/query/ack decoding restores canonical numeric types before identity/history/metadata matching and strict acknowledgement comparison. Ordinary strings remain strings, representable values/routing keys remain native, and malformed/unknown/tampered envelopes fail validation. This is storage encoding, not encryption or a second write.

Do not write the reserved envelope as application data or assume direct native SQL numeric ordering/range/index semantics over escaped values. Public provider metadata matching sees restored values; arbitrary native numeric query capabilities are not certified. Envelope overhead can increase payload/index size and RUs; hosted cost acceptance remains open. Already-rounded legacy data cannot be reconstructed. Current local proof and remaining gates: `Docs/WIP/HyperDrive-Providers/COSMOSDB_RECORD_HISTORY.md`.

Trusted caller objects are projected into canonical Avatar/AvatarDetail/Holon DTOs before repository create, upsert, replacement and conditional replacement. Application subclasses need not be allowed by the stored JSON binder; persisted records still use restricted decoding. Repository subclasses implement the typed ProjectInput hook with the shared OasisJson.Project helper. Native subclass tests cover direct and recursive saves, not arbitrary proxy/explicit-interface implementations or extra application-specific fields.

Uses Microsoft.Azure.Cosmos SDK, canonical OASIS serialization and shared holon graph planning/projection. Native evidence uses the retained local Cosmos DB emulator, not a hosted Azure account; scoped tests do not certify full-provider or hosted-release completion.

Retained record history is supported when IsVersionControlEnabled is enabled; existing archives remain readable when creation is disabled. Version zero selects current and positive versions select retained history. Historical credentials are not authentication authority. Profile/detail mutations span separate containers and are not a single atomic transaction; partial/uncertain mutations require inspection before replay. History, concurrency/ETag, recovery, full interface-option and hosted/release verification remain open. Current evidence and gates: `Docs/WIP/HyperDrive-Providers/HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md`.

From the existing repository checkout: `pwsh -NoProfile -File Scripts/TestHosts/Run-ReusableCosmosDBTests.ps1`. Reuses the exact retained container/image and cached dependencies, leases the environment exclusively and stops its owned service afterward. No installation/download/reset/runtime replacement. Fixtures reuse empty native/SQLite schemas; nonempty or unexpected fixtures stop testing for inspection rather than being deleted or overwritten.

Add `-InspectRetainedFixture` for read-only fixture counts and the SQLite file hash. This diagnostic does not certify provider operations or emit keys/record payloads. Ordinary and diagnostic results use separate fixed reports under `TestResults/CosmosDBCurrent`.

Soft deletion now compares each observed record and uses native ETag `IfMatch` when replacing it. A changed record is rejected rather than overwritten. This is per-record optimistic concurrency, not an atomic transaction across profile/detail containers; earlier acknowledged changes remain explicit partial-operation evidence. Ordinary unconditional saves and hard deletes do not gain an all-purpose concurrency guarantee from this path.

The optional `-ReconcileRetainedRoutingFixture` mode is only for the reviewed routing fixture shape. It requires `OASIS_COSMOS_ROUTING_FIXTURE_SHA256` to match an independently inspected file, validates every nonempty table count, checks original native IDs are absent while their containers remain readable, then removes only that closed disposable SQLite file. It is not a general cleanup/reset tool and does not replay native mutations. Keep the inspection/reconciliation reports; an arbitrary hash alone is not ownership authorization.
