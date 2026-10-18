# NextGenSoftware.OASIS.API.Providers.PolkadotOASIS

Polkadot adapter under implementation. RPC connectivity is verified; storage, signing, token/NFT and bridge capabilities are not production-certified.

## Current implementation and verification limits

### Explicit original submission and receipt evidence

`ReadOriginalNativeOutcomeAsync(preparation, fromBlockNumber, maximumBlocks, token)` now independently verifies the retained original, then scans an explicit caller-selected finalized range (1–128blocks/call, default16). It returns either the exact original's receipt with canonical AccountId32 fee-payer binding, or an unknown result with `nextBlockNumber`, snapshot height and inspected count. Resume that cursor read-only; absence through a snapshot is not rejection/expiry/no-fees/replay permission. Caller range/original are captured before awaits. This is a resumable range reader, not a full-history index or automatic settlement worker; callers must retain the original and cursor. Current-runtime verification still cannot independently verify historical signing metadata after a runtime upgrade. RPC archive/pruning errors remain errors, not missing-transaction results.

2026-10-18 SDK81/81checks include14range checks (bounded cursor, future cursor, unknown state, found success/paid failure, signer/payer mismatch, malformed limits and caller mutation). Native public reads prove unknown outcome for an unsubmitted signed original and a found receipt via the range reader for public block33490434; no controlled signed original was broadcast. C# two native preparation/independent-verification/original-outcome rows pass31s/zero skips, report hash991CD697176BB879D8B73FE39AA0521BFE07DEE987A97C9AD6B6127EF99B06C4. These do not prove a funded submission-to-finality flow. See release gates for exact evidence/remaining work.

`SubmitNativeTokenTransferPreparationAsync(preparation, execution, clientRequestId, expectedSender, token)` now independently verifies and persists the exact signed original through the existing execution owner, then makes one `author_submitExtrinsic` call with its SCALE hex bytes using pinned `@polkadot/api` HttpProvider (cache disabled). No signing/regeneration, implicit retries or service-key fallback. The write capability is consumed before I/O, even for rejection/lost response; metadata/preparation keep their hard read-only provider. Reuse the stable clientRequestId: the existing outbox rejects an already-started attempt, not an acknowledgement permitting another broadcast.

The staged submission result is **pool acceptance, NOT transfer completion**: `submitted:true`, `poolAccepted:true`, `finalized:false`, `reconciliationRequired:true`. After acknowledged persistence, process/cancellation/error/mismatched response retains the exact original hash through `INativeMutationReconciliationRequired` and its original inner exception. A capped sponsored operation is still rejected because an estimate is not an all-fees maximum. Ordinary native `SendToken` now uses this path through an explicit owner scope, as described below; hosted HTTP admission/billing composition is not verified.

### Ordinary native SendToken ownership and finality

`ExecutePreparedNativeOperationAsync(() => provider.SendTokenAsync(request), execution, clientRequestId, expectedSender, token)` supplies an existing admitted owner. The ordinary sync/async `SendToken` paths now prepare/sign with authoritative precision, persist/submit the exact original once, then read finalized ranges from the SDK signing block until a verified successful dispatch, error or two-minute post-acceptance deadline. Only finalized successful dispatch returns transfer success. Paid dispatch failure, read failure and interrupted/unknown outcomes retain the original reconciliation marker; billing settlement remains separate. Polling repeats only read operations, never submission/signing. Standard sync entry runs the async path on the thread pool while flowing execution context, avoiding a UI synchronization-context wait.

One claim per scope; nested/reused/closed scopes fail. Scope lifetime owns linked cancellation and closes escaped work; preparation also accepts cancellation. Missing scope fails before signing/network/submission. Existing manager/HTTP composition must supply trusted admission/prices/expected canonical sender; this provider does not admit itself or invent funding. Native-only keep-alive transfer/atomic memo are supported by this implementation; token-asset/NFT/storage interfaces remain separate open work. Controlled process/pipe tests prove ownership/finalized-success/paid-failure/read-failure and deterministic cancellation after entering finality; NOT a funded native send or Mongo durability certification.

`ReadFinalizedNativeReceiptAsync(blockHash, transactionHash, token)` independently reads canonical finalized inclusion, unique extrinsic index, System dispatch success/failure and native TransactionPayment.TransactionFeePaid. Actual fee includes tip already. Failed execution can still pay fees. Missing/ambiguous/native-asset fee events are errors, not zero fees. Requires explicit block+transaction identities; no incomplete bounded history lookup is advertised as a full index.

Evidence2026-10-18: SDK67checks; real SDK identity/preparation plus5controlled ownership/submission-order/lost-response/hash/caller-mutation cases, **zero public broadcasts**; official HttpProvider against a controlled loopback JSON-RPC server proves exact bytes/one request for acceptance, rejection and response loss. Four actual owned-child/physical-pipe C# checks prove pool-only response and original-hash error markers, not ledger execution or real Mongo durability. Filtered C#47pass. Separate live read-only Polkadot receipt evidence is in the release-gates document. Funded submission/finalized end-to-end/recovery/settlement/API integration and complete provider interfaces remain OPEN; no production certification or public-chain funds claim.

Activation uses HTTP JSON-RPC `system_chain` and `chain_getBlockHash(0)`, validates response version/correlation/result and configured network before publishing activation. Constructor `chainId` accepts the exact reported chain name (case-insensitive) or a `0x` genesis hash. Use a genesis hash for stronger network identity; a human-readable name is not globally unique. Deactivation preserves the reusable HTTP client; `Dispose` releases it. Each activation RPC has a ten-second deadline. Connection activation is not proof that every advertised capability works.

Explicit opt-in read-only tests use `https://rpc.polkadot.io`, checking name/genesis configuration and sync/async balance reads (including a public non-zero-balance account) without keys, signing, transfers or transaction fees. Controlled tests cover malformed/failed/miscorrelated replies, wrong networks, reuse and exact unsigned native units. These are not full-provider tests.

`GetBalance`/`GetBalanceAsync` now use pinned `@polkadot/api`17.0.2 to decode runtime metadata and finalized `System.Account` storage. They verify the SDK genesis against the activated network, read authoritative token precision, preserve u128 base units through decoding and convert only at the existing `double` interface boundary (display precision is approximate). HTTP/RPC/SDK failures are errors, not invented zero balances. Native free balance is not a promise that all free funds are transferable; freezes/reserves/fees require separate checks before signing. Node must be installed once, and locked SDK dependencies provisioned once with `npm ci --ignore-scripts --no-audit --no-fund` in `SdkBridge`. Reuse that installation. Configure `OASIS_POLKADOT_NODE` and `OASIS_POLKADOT_SDK_BRIDGE` when not using the published bridge directory. Reads have a45second process deadline; the owned process tree is stopped on cancellation. Docker bridge packaging now includes `PolkadotSdkBridge`; an actual hosted image/deployment has not yet been verified.

OPEN: existing storage methods assume REST routes that are not standard node RPC; non-native-transfer transaction methods still construct unsigned JSON-shaped extrinsics; bridge payloads and NFT/storage runtime assumptions require replacement and native proof. Private on-chain record encryption/queryability, funded transfer proof, historical verification/recovery/fee settlement and all relevant interfaces remain unfinished. Do not use this adapter for production storage/minting or claim full XCM/relay/parachain support from a successful activation.

Official references: [Polkadot transaction SDKs](https://docs.polkadot.com/chain-interactions/send-transactions/with-sdks/), [SDK lifecycle guidance](https://docs.polkadot.com/reference/tools/polkadot-js-api/), [runtime queries](https://polkadot.js.org/docs/api/). Polkadot.js is in maintenance mode, not active feature development; PAPI/Dedot are recommended for new applications. This narrowly scoped dynamic-metadata reader uses its existing supported account-storage interface and does not imply new chain-feature support. One pinned SDK installation was added and retained; no local chain was installed. Native signing/storage replacement remains OPEN.

## Native transfer preparation (not submission)

`PrepareNativeTokenTransferAsync(ISendWeb3TokenRequest)` now builds and signs a real metadata-derived SCALE `Balances.transferKeepAlive`. It requires an activated network and an explicit matching sr25519 signer: either a32byte0xhex private seed or valid BIP39 mnemonic, never both. No configured service-wallet fallback is used. Sender, optional public key, recipient, invariant decimal amount and UTF8 memo are captured before asynchronous work. Amount conversion is exact in runtime native units and bounded to u128. Asset-token requests are explicitly outside this native preparation method. A supplied memo is included atomically through `Utility.batchAll` plus `System.remarkWithEvent`; unsupported runtime calls fail rather than silently dropping it.

The SDK verifies the signature, independently decodes the signed extrinsic and checks its sender, call bytes, genesis and transaction hash. It returns signed bytes, sign-document bytes, signature and a runtime `TransactionPaymentApi.queryInfo` estimate against those exact bytes. **The estimate is not a maximum fee, reservation, final fee receipt or successful transfer.** The result explicitly says `submitted:false`. The network provider permits only named read-only RPC methods, rejecting author/transaction broadcast methods even if accidentally called. Signing seeds/mnemonics go through stdin, not command-line arguments; diagnostic output redacts those secrets and endpoint URLs. Signed bytes remain replay-capable transaction material and must be handled by the trusted execution owner, not published as routine logs. JavaScript/managed strings cannot be reliably zeroed; temporary seed buffers are cleared and the key pair locked before exit.

Preparation is an implementation stage, not complete provider certification. The ordinary native-transfer unsigned JSON path has now been replaced by the explicit owner/finality flow above. Live funded test-chain proof, historical recovery, sponsored-fee settlement and hosted API composition remain open. Sponsored spending must not treat `partialFee` as a signed all-fees cap. No public funds were spent during preparation tests. Standard storage/NFT/bridge gaps remain as listed above.

The bridge directly pins `@polkadot/util-crypto`14.0.3 (already present in the retained SDK installation); lock promotion was performed offline without downloads. Publish includes the companion native-transfer/read-only-provider modules. Use the same installation; do not create another per-run SDK environment.

### Independent signed-byte verification

`VerifyNativeTokenTransferPreparationAsync(JsonElement)` independently checks the original signed bytes using the official SDK, without any private key. New preparation protocol `oasis.polkadot.native-transfer.preparation.v2` includes `chainId` and a canonical UTF8 JSON signing-document envelope (`oasis.polkadot.signing-document.v1`) in `signDocBase64`. Unlike the earlier ephemeral v1 raw-payload output, it retains the SDK signing context needed by an independent verifier. Old formats are explicitly rejected rather than guessed or silently reinterpreted; no stored-data migration/replay was performed.

The verifier enforces bounded canonical base64/JSON, current runtime version/signed extensions, configured genesis, canonical signed v4 SCALE encoding, original hash/sender, sr25519 signature (including the over256byte hashed-payload case) and byte-identical reconstruction of signed extensions. It decodes and restricts the call to native keep-alive transfer, optionally the exact atomic memo pair; recipient/units/memo come from decoded signed bytes. Returned fee coverage is identity-only: an altered estimate cannot authorize a fee or become a receipt. Historical metadata verification, mortality/nonce validity at future submission, funds sufficiency, finalized dispatch outcomes and actual fees remain separate OPEN gates. Windows child-process streams now explicitly use strict UTF8; native Unicode memo roundtrip exposed the prior OEM-code-page decoding defect.

### Persisting an original preparation (no broadcast)

`PersistNativeTokenTransferPreparationAsync(preparation, execution, clientRequestId, expectedSender, token)` uses the existing `UsageExecutionHandle.NativePreparationPersistence` owner and shared `PreparedSubmissionServer`/`preparationClient` transport. It does not introduce a parallel production journal. The parent binds the known exact original hash, configured genesis and expected canonical sender, independently verifies signed bytes, then acknowledges only after the owner's persistence callback completes. SDK response must match that acknowledged original. Missing owner/channel, replacement identity, persistence failure or cancellation cannot be reported as successful persistence. Child transport variables are scoped per process, not inherited from another operation; a linked deadline/caller cancellation governs the child. A failed acknowledgement is not evidence that the owner persisted nothing: retain the operation/original identity and inspect its durable state before proceeding.

An admitted `MaximumNativeFeeCostUsd` (including zero) is explicitly rejected with `POLKADOT_VERIFIED_FEE_MAXIMUM_REQUIRED`: the current identity verifier cannot prove a signed all-fees ceiling. An uncapped handle is not implicit spending permission. Trusted host admission/prices and actual fee measurement remain separate requirements. This method NEVER broadcasts, creates a final receipt or settles billing. Existing `SendToken` is not certified by this work.

Native transport tests use actual SDK verification plus a create-only, flushed-to-disk fixture record and independent readback; rejection/cancellation/replacement tests require no acknowledgement and preserve original exceptions. A deliberate failure after flush verifies the original still exists despite no acknowledgement, then explicitly reconciles/removes that exact unsubmitted test record without replay. Unexpected failures preserve their file. This proves the SDK/IPC/persistence boundary, NOT the production Mongo outbox, hosted middleware or sponsored-spending flow end-to-end. Run with one fixed `OASIS_POLKADOT_NATIVE_PREPARATION_JOURNAL` path and never overwrite a pending file. The shared transport has one source and is linked into published assets; no second SDK/runtime is installed.

## Installation

```sh
dotnet add package NextGenSoftware.OASIS.API.Providers.PolkadotOASIS
```

[NuGet versions](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS) · [Source project](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.csproj)

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

For APIs returning `OASISResult<T>`, check `IsError`, `ErrorCode` and `Message`; do not treat a failed result as success. Preserve useful diagnostics without logging access tokens, passwords or private keys. Keep secrets in the host's appropriate protected configuration or platform storage.

## Platform requirements

Target-framework compatibility is not proof that every native dependency or external service works on every operating system. Check the selected version's platform assets and native/service requirements before deployment. Unity/mobile integrations require their supported platform layer and packaging workflow; a managed NuGet archive alone does not establish Android/iOS, IL2CPP or two-device convergence acceptance.

## Troubleshooting

- Restore/assembly mismatch: pin a coherent set of package versions and inspect the dependency graph.
- Configuration or authentication error: inspect the owning component's result diagnostics and service requirements without exposing secrets.
- Native loading failure: verify OS/architecture and required native artifacts rather than suppressing the error.
- Missing functionality in an older version: compare the immutable released source and release notes, not only the moving Development branch.

## Releases and further documentation

[Verified release matrix](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/Docs/Devs/OASIS_RELEASE_STATUS.md) · [Documentation audit](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/Docs/Devs/RELEASE_DOCUMENTATION_AUDIT.md)

Published NuGet versions are immutable. Updated documentation ships with a new unpublished package version after checked promotion and publication. This metadata-backed guide does not replace product-specific API examples or claim unverified platform tests.

