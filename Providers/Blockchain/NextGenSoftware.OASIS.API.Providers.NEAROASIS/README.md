# NextGenSoftware.OASIS.API.Providers.NEAROASIS

NEAR adapter using a pinned official near-api-js SDK through a Node bridge. Native account/key/balance/transfer operations and configured-contract storage/token/NFT/bridge paths have scoped retained-local-chain tests. Those custom-contract fixtures do not certify general NEP-standard interoperability, every provider option, hosted APIs, fee settlement or full HyperDrive V2 policy behavior.

## Process encoding and native verification limits

SDK stdin/stdout/stderr explicitly use strict UTF8, independent of the host console code page. Controlled echo-process tests reproduce the former non-ASCII decoding defect and verify accented text, emoji and Japanese text after the fix; they are not SDK/native-chain tests. The retained native fixture separately checks Unicode NFT metadata through the deployed contract and official SDK. This fixes client transport decoding, not stored-record schema or on-chain state; no record/key migration or re-encoding was performed.

Reuse the configured SDK/runtime and persisted private storage key. The native evidence runner reconnects to the existing retained ledger and checks deployed contract identity; it must not initialize/reset/redeploy a chain per run. Native gas is not a fixed retail/USD price or fee-free guarantee. Durable unknown-outcome recovery, complete fee accounting, hosted authorization, standard-contract interoperability and full interface/V2 certification remain open; see repository release gates for current report scope and hashes.

## Installation

```sh
dotnet add package NextGenSoftware.OASIS.API.Providers.NEAROASIS
```

[NuGet versions](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Providers.NEAROASIS) · [Source project](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.NEAROASIS/NextGenSoftware.OASIS.API.Providers.NEAROASIS.csproj)

Choose a published version compatible with the other OASIS packages in your application. This guide describes current source; older packages may not contain the same functionality.

## Frameworks and dependencies

Declared target frameworks: `net10.0`.

Direct external package references:

- `NextGenSoftware.OASIS.API.Core`

Project references and conditional dependency versions are recorded in the source project. NuGet restore resolves packaged dependencies; do not mix arbitrary DLLs from different release graphs.

## Integration and configuration

Use this package for the responsibility described above, alongside its owning OASIS runtime/provider integration. Inspect the source project's public APIs and owning component documentation for construction and configuration; this package installation command does not start an API server, authenticate an avatar or provision an external service.

Provider adapters require their respective service configuration and credentials. Runtime libraries require the host application's lifecycle and persistent paths where applicable. Supply only the settings actually required by the owning component; do not copy production DNA into a client or public release.

## Errors and security

Avatar, avatar-detail and holon contract storage now requires `OASIS_NEAR_STORAGE_ENCRYPTION_KEY`: a separately generated, persistently retained Base64-encoded 32-byte key. Configure it through protected host secrets; do not derive it from the account signing key, commit it, print it, or generate a replacement on every startup. Losing it prevents reading existing encrypted records. Back it up securely alongside the deployment's network and contract identity.

The existing shared authenticated-encryption codec protects storage values before SDK dispatch and binds them to network, contract and record key. Views decrypt locally, including collection queries. Plaintext records, incorrect keys and swapped ciphertext fail closed; there is no plaintext fallback or automatic migration. Generic storage get/list/put/delete requires key configuration before starting the SDK process. Token/NFT contract calls and their intentionally public metadata are separate from this record-storage boundary.

The encryption boundary has focused codec/preflight tests and a 2026-10-18 retained local-chain SDK roundtrip:12 native cases passed after the raw yocto balance overflow fix. Raw finalized contract inspection also confirmed encrypted record envelopes. This is scoped evidence, not complete interface/options/version or hosted acceptance. The evidence runner connects only to an explicitly retained environment; it does not initialize/reset a sandbox, deploy contracts, download packages or restore .NET dependencies. Configure `OASIS_NEAR_REUSABLE_RPC_URL` (loopback HTTP), `OASIS_NEAR_REUSABLE_CHAIN_ID`, `OASIS_NEAR_REUSABLE_CONTRACT_ID` and `OASIS_NEAR_REUSABLE_SIGNER_PATH` (inside `/var/tmp/oasis-near-evidence/`). Deployed finalized code must match the locally compiled contract byte-for-byte. Provision missing native tools once explicitly and retain them; never use the sandbox SDK's destructive default initialization against an existing ledger.

Owned test credentials pass from the Linux SDK host to Windows through stdin, not a plaintext settings file or command line. The Windows test host creates `LocalAppData/OASIS/provider-evidence/near/storage-key.dpapi` only if absent, refuses to overwrite/replace it and restores pre-existing process environment after execution. Actual configured execution now verifies that DPAPI success path. The retained sandbox2.6.2 runtime and SDK2.0.0 compiler tools were installed once; contract compilation is verified using `Scripts/run_near_provider_evidence.sh --compile-only`. That explicit mode never starts a node. `bootstrap-retained.mjs --inspect` is read-only; `--deploy-once` explicitly bootstraps the reviewed empty local account with a durable pre-broadcast original hash, refuses prior intents, and verifies FINAL before checking code. `--verify-original` only reconciles that original, never broadcasts. Preserve the existing ledger, storage key and bootstrap journals. Detailed evidence and remaining gates: `Docs/Devs/HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md` in the parent repository.

## Record history and costs

Network configuration is explicit: `networkId` and `chainId` must both be nonempty and identify the same RPC chain. The installed official SDK's `getNetworkId()` returns node-status `chain_id`; activation checks the matching configuration and then probes that identity. The constructor retains `chainId` rather than ignoring it. Mainnet remains the default for both fields. For a custom/test chain, set BOTH DNA Network and ChainId (BootLoader already forwards them); changing only Network while leaving the mainnet ChainId default now rejects activation. No hosted DNA variables are changed automatically.

Transaction-status lookup distinguishes execution finality from execution success. The SDK bridge returns the native outcome; its transaction ID must match the requested hash. Only FINAL+SuccessValue maps to Completed; FINAL+Failure maps to Failed, while nonfinal execution remains Pending. Unknown finality or malformed final outcomes return explicit lookup errors, not fabricated successful completion. Always check IsError before reading a status result. These statuses describe the native transaction, not cross-chain settlement or fee recovery.

The retained evidence host requires `failed-status-original.json`, created by the bounded read-only `find-failed-status.mjs` helper from an already-existing finalized failed transaction. It never signs, submits, resets or replaces an original. Existing evidence is reconciled read-only; a pruned/unavailable original is a verification failure, not permission to replay it. The host forwards only its public hash to the native test and restores the prior process environment afterward.

Nearby network-interface queries use shared `StoredRecordLocationQuery`: request coordinates are integer microdegrees, metadata coordinates are invariant-culture degrees, radius is metres. Invalid request ranges/radii/types reject before storage access. Malformed stored coordinate pairs produce an explicit error with no nearby result collection; records with neither coordinate are omitted. Matching occurs after encrypted record reads, without a public location index or provider-specific distance implementation.

Search now validates and evaluates the existing `ISearchParams` contract with shared `StoredRecordSearch`, rather than silently reducing requests to text-group unions. Owner/parent scope, metadata matching, group operators, text/number/date selections, type targets and field masks use that common evaluator. Invalid current-avatar scope or negative history/depth rejects before activation. Matching runs locally after authenticated record decryption; this introduces no public email/username/metadata index. Whole-prefix listing and historical/descendant reads still scale with retained records and may incur RPC-service usage; this is not an indexed server-side query or a full hosted authorization/performance certification.

Avatar, avatar-detail and holon record writes use detached canonical snapshots too. Rejected activation or record-storage preflight leaves caller IDs, provider links and version stamps unchanged; successful acknowledged writes publish those fields back to the caller. This does not imply rollback of an already submitted transaction or guarantee recovery of unknown outcomes. The focused failure checks distinguish rejected activation and controlled post-activation missing-encryption-key rejection from live native write acceptance.

Graph saves preflight the entire selected forest using shared `HolonGraphWritePlan` before activation or signed writes. Null/repeated nodes, cycles, duplicate IDs, conflicting selected-child parents and negative depths fail explicitly. Depth selection follows the OASIS save contract: root counts as depth1; maxChildDepth0 is unlimited. The batch argument is `curentChildDepth` (the base interface's spelling), not an archival version. The one-argument save overload follows the interface defaults: save children recursively. Snapshots receive planned identities; caller IDs/parents/provider keys/version stamps are published only after acknowledged current-record writes. Failed parents block their selected descendants, while continueOnError permits unrelated branches. A result reports the acknowledged count; a later failure does NOT roll back those writes. Unknown post-submission outcomes still require durable native recovery and are not certified by preflight tests. Named callers that used the previously incorrect `version:` batch parameter must use `curentChildDepth:`.

`IsVersionControlEnabled` defaults to false, matching shared key-value storage. Saves stamp current record versions either way; enabling the property archives the replaced version before each update. Archive records use separate `history:` keys through the SAME encrypted SDK boundary. An archived update adds one signed contract write (gas/storage) to the current-record write; hard deletion also submits deletes for retained history. These are sequential operations, not an atomic multi-record transaction. A failure can leave an acknowledged archive without the current update, and callers must not assume rollback or safe replay. Production fee admission/recovery, concurrent-writer control, DNA configuration binding and full interface acceptance remain separate open gates; this does not authorize sponsorship.

Version0 selects current data; positive versions select that exact version or return no record, never substitute latest. Shared `StoredRecordVersions` owns identity validation/version stamps/overflow checks for NEAR and generic key-value storage. Single/list avatar, avatar-detail and holon reads, selectors, search/export and parent queries now forward requested versions. Historical root loads keep descendants at their independent current versions, not a point-in-time graph snapshot. Hard avatar/holon deletion removes that record's history; deleting an avatar is not newly certified as cascading to its separate detail record. Versions overwritten before archiving was enabled cannot be reconstructed. The expanded retained local-chain suite passed13cases on2026-10-18, including historical selectors, root/child version independence and hard-delete paths; it does not prove every option, concurrent failure or hosted API behavior.

For APIs returning `OASISResult<T>`, check `IsError`, `ErrorCode` and `Message`; do not treat a failed result as success. Preserve useful diagnostics without logging access tokens, passwords or private keys. Keep secrets in the host's appropriate protected configuration or platform storage.

## Platform requirements

Target-framework compatibility is not proof that every native dependency or external service works on every operating system. Check the selected version's platform assets and native/service requirements before deployment. Unity/mobile integrations require their supported platform layer and packaging workflow; a managed NuGet archive alone does not establish Android/iOS, IL2CPP or two-device convergence acceptance.

## Troubleshooting

### Independent custom-token senders (2026-10-18)

Custom-contract token sends now forward the supplied `FromWalletAddress` and `OwnerPrivateKey` to the official SDK, like native NEAR sends. Previously the custom branch ignored the owner key and used the service signer, causing contract rejection for independent holders. An explicit independent sender without its key now fails before activation rather than substituting the service key. Default service-owner sends remain supported. Used request fields are frozen before SDK awaits, and exact native/custom units validated before activation.

Current retained native suite17/17 passes153seconds, including an actual funded independent holder returning2custom units after receiving10; balance reads prove issuer92/holder8 and later issuer burn91.75/holder8. Funding and all fees use LOCAL sandbox testNEAR only, not public-chain money. Focused unit39/39 passes1second; legacy default-mainnet template class is excluded, so this is not a complete old unit-project claim. Same SDK7.3.1/runtime2.6.2/ledger/contract retained, node gracefully stopped after verification. Exact hashes and remaining gates are in `Docs/Devs/HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md`.

These custom-token methods target the existing OASIS fixture contract, not generic NEP141 interoperability. General signer authorization, burn/NFT signer coverage, escrow semantics, fee accounting and durable original-transaction recovery remain separate open gates. No API production fee or full-provider certification is implied.

- Restore/assembly mismatch: pin a coherent set of package versions and inspect the dependency graph.
- Configuration or authentication error: inspect the owning component's result diagnostics and service requirements without exposing secrets.
- Native loading failure: verify OS/architecture and required native artifacts rather than suppressing the error.
- Missing functionality in an older version: compare the immutable released source and release notes, not only the moving Development branch.

## Releases and further documentation

[Verified release matrix](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/Docs/Devs/OASIS_RELEASE_STATUS.md) · [Documentation audit](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/Docs/Devs/RELEASE_DOCUMENTATION_AUDIT.md)

Published NuGet versions are immutable. Updated documentation ships with a new unpublished package version after checked promotion and publication. This metadata-backed guide does not replace product-specific API examples or claim unverified platform tests.

