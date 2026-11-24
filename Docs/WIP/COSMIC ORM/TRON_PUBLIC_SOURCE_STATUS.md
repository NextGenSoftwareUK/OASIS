# TRON public header qualification

Updated 1 November 2026. Qualification only; no CQL/API/hosted grant or count increase.

While the exact Sui deployment is queued, three anonymous header-only reads from `https://api.trongrid.io/walletsolidity/getblock` at heights0/1/2 returned HTTP200. Bodies were522bytes each, no transaction bodies. The observed1→0 and2→1 parent links agree. No wallet/API key/account/runtime install or transaction was used.

Official contract: [SolidityNode GetBlock](https://developers.tron.network/reference/getblock-2) supports explicit height/ID and header-only selection. [SolidityNode GetBlockByNum](https://developers.tron.network/reference/getblockbynum) describes successful block results as solidified in that node view. This observation is not independent consensus or signature verification.

Mainnet genesis observed: `00000000000000001ebf88508a03865c71d452e25f4d51194196a1d22b6653dc`. Verify network identity explicitly in any future adapter. Genesis raw_data omits number/timestamp and has a special witness payload; do not treat it as a normal account address or silently invent general defaults. A future demo can select ordinary early headers1/2/3 while checking genesis separately.

Raw public observation: `TestResults/TronPublicSourceCandidateCurrent/readiness.json`, SHA256 `894646b9935fa21f92ff8bdb552956aa8112a29ff56c922e44e7fe5a2f4ed3a8`, 1985bytes. Source identity/catalogue not created yet.

Next after Sui hosted gate: qualify third ordinary header and strict native schema/precision/identity rules; implement explicit bounded read-only header profile, tests and commented C# sample; then admission/planner/localHTTP before hosting. Existing TRON signed wallet provider remains separate. No generic scans/metadata queries/signed writes are certified by this probe.

## Strict qualification complete

Nine pure tests pass. Four bounded anonymous reads (genesis plus ordinary headers1/2/3) pass exact block ID/header height agreement, canonical encodings, nonnegative native Int64 precision, and adjacent parent linkage. Header-only request `detail:false` is enforced; duplicate JSON properties, HTTP200 Error bodies, transaction bodies, malformed fields and numeric coercion fail. Genesis zero omission is explicitly scoped to genesis; ordinary missing fields are not defaulted.

- Probe: [Probe-TronPublicReadiness.py](../../../Scripts/Sandbox/Probe-TronPublicReadiness.py).
- Controls: [test_tron_public_readiness.py](../../../Scripts/Sandbox/test_tron_public_readiness.py).
- Raw `TestResults/TronPublicSourceCandidateCurrent/qualified-readiness.json`: 1725bytes, SHA256 `1552532a1bdf389c9ae32cf9cf25cb39453dfc1ce615f753c32959254b76f380`.

No provider admission, CQL adapter, signing operation, sourceOwner or catalogue was created. Next independent work is the bounded native header adapter and shared-manager demo; hosted enrollment must wait for its own controls. Sui exact deployment is still tracked separately, without duplicate submission.

## Native candidate — live gate red

The new `TronPublicHeaderMetadataSource` implements common query/read-activation interfaces and the existing shared key-value base. It admits only equality or bounded IN on canonical positive Int64 `TronBlockHeight` keys. Explicit Mainnet genesis and header/block-ID identities are verified. Native Int64 values remain decimal strings. Complete selection validation precedes windows/publication; no source writes or retries. Maximum three concurrent ordinary-header reads per native query are drained on cancellation.

**31 controlled tests pass; the single real shared-manager test fails with HTTP429 during activation.** The earlier four-request qualification passed, but does not establish sustained anonymous capacity. No native CQL certification/count increment, CLI acceptance, API enrollment or hosted grant. Native catalogue/sourceOwner retained once under `LocalState/COSMIC/HostedTronPublicSourceCurrent`; no chain account was created.

- Raw `TestResults/TronPublicSourceCandidateCurrent/tron-source.trx`:31passed/1failed/0skips, SHA256 `123fbcdab402eb1e0c3832bd714d334a4f9b0a0d76f4a2b1d5c0307701d65dc7`.
- `native-build.log` retains HTTP429 and exact stage; no write fixture or database exists.
- Controlled tests include complete-page failures, identity/genesis, numeric precision, HTTP errors, unknown catalogue keys, concurrent overlap/order and inflight/queued cancellation.

[Official rate-limit policy](https://developers.tron.network/reference/rate-limits) says anonymous requests may be restricted by IP and endpoint; current limits vary. Do not invent a fixed QPS promise or sleep/retry until a test turns green. Investigate a properly bounded native batch selection contract or explicit suitable access before another real acceptance attempt. A block-range API is a distinct native query design, not an automatic fallback; retain header-only/privacy/byte-budget/capability invariants if adopting it. [Official query primitives](https://developers.tron.network/reference/query-the-network).

This is agent-owned work; no new David account/action request yet. Sui live deployment remains the immediate release priority.

Native batch redesign: adjacent selected heights use exact native ranges; sparse/single keys retain native points, with no intervening records or after-failure route change. Bounded range responses may include block bodies; only header fields are mapped/exposed.34controlled tests pass, including malformed range size/order; real shared-manager acceptance still fails with429. Red gate retained, no third blind attempt. Raw tron-source-batch.trx SHA256 `af140b5c52f879beaef7a214faca25d335a83b9fd38764530e9e29c9b20086f6`. Need suitable actual endpoint capacity/access; fewer requests alone did not resolve anonymous quota.

PublicNode qualification (separate explicit candidate, not fallback) failed with HTTPError during the genesis/range probe. Only error type was captured, so status/cause remains unverified; do not invent an archive-access diagnosis. PublicNode was not enrolled, source.json remains original TronGrid, and owner/catalogue unchanged. Raw publicnode-readiness.json SHA2565690c19f79e19a4c9ae8eb996b7605b58a82be090e4904bc81963bd527e2864e. Operator endpoint listing: https://tron.publicnode.com/. No account request or new endpoint claim.

## Bounded diagnostic while TON rollout waits

Rechecked official operator policy: anonymous limits may vary by IP, endpoint and time window; no fixed delay is proven to solve this access failure. PublicNode still publishes its native endpoint. One explicit diagnostic of https://tron-rpc.publicnode.com/walletsolidity/getblock for genesis returned HTTP403 before any header validated. Stopped immediately, no other heights/retries/fallback, no catalogue/owner change or provider grant. This establishes only the new diagnostic status, not the status/cause of the older probe or an archive-access diagnosis.

Raw TestResults/TronPublicSourceCandidateCurrent/publicnode-qualification.json:238bytes, SHA256beff38cae21c62c8b8985e568dd2583757bd4f7a97a7c30a443afd41a9693b50. Durable publicnode-qualification.intent.json remains beside existing unchanged source.json. No native CQL success/count increment. Continue TON hosted rollout; revisit TRON only with a separately qualified access contract, not another blind acceptance run. [TRON rate limits](https://developers.tron.network/reference/rate-limits), [operator endpoint](https://tron.publicnode.com/).

## Authenticated recovery candidate

The original adapter and controls were recovered from `f8508cca8` into the
machine-local qualification directory. The draft accepts a separate
dedicated TronGrid reader key, retains the original mainnet endpoint and
catalogue shape, and keeps credentials outside the catalogue. Its native
test requires `COSMIC_TRON_PUBLIC_SOURCE` and the private process setting
`COSMIC_TRON_PUBLIC_READER_KEY`. The draft uses a dedicated TLS client with
redirects, proxying and cookies disabled. A 15-second deadline covers the
response body and caller cancellation is classified separately.
All 38 controlled cases passed with zero skips at Core
`2abc7100acbdf2e4e6d8249c24f7cf5746daba34`.
The TRX SHA-256 is
`ead1d9ab42731b361c8886dc020f065f96beaafb7ee5184987a1f32320c43505`.
[TronGrid API key](https://developers.tron.network/reference/select-network).

The earlier local descriptor is absent on this laptop. A separate,
machine-local catalogue selects the previously qualified public heights
1, 2 and 3 with a new synthetic projection owner. The catalogue SHA-256 is
`7f1c8fe3c35ef3c45ee31be6eaef72f34f8d10185a28e2177ace267822e89c02`.
It is separate from the original cohort.

After the reader key is available, run
`LocalState/COSMIC/HostedTronAuthenticatedQualification/verify-native.ps1`
from this checkout. The runner checks the catalogue and controlled assembly
hashes under the shared lease and requires one passed native test with zero
skips. It keeps a unique intent, log and TRX for each execution. The missing
key preflight passed without creating a run directory or making a request.
Authenticated native qualification and live WEB4 enrollment remain open. The offline admission candidate and exact image evidence are recorded below.

On replacement WEB4 deployment 7f8f646e, the Development configuration
inventory still showed no CQL_TRON_ settings. The dedicated reader variable
was absent from the process, Windows user, and Windows machine environment.
No credential values were displayed and no anonymous request was replayed.
The existing private-configuration action remains open in ACTIONS-FOR-DAVID.

## Prepared WEB4 admission and remaining live gate

WEB4 now has an opt-in registration for the authenticated header reader.
It requires sandbox mode, the reviewed catalogue in
`CQL_TRON_PUBLIC_HEADER_QUERY_SOURCE_JSON`, and the separate protected
`COSMIC_TRON_PUBLIC_READER_KEY`. The hosted preparer binds the same
catalogue to four full/subset native and paired presets. Existing
enrollment controls passed 46 of 46 with outbound HTTP forbidden.

The exact scoped image for source beee2d25 and Core 7b426c9 passed the
real WEB4 startup and cleanup check. This evidence closes the image gate.
Authenticated native results, hosted enrollment, live paired reads and
browser verification are open. Resume with the existing private reader
access action and retained catalogue. Keep the original anonymous denial
evidence and the recovered authenticated cohort separate.

[Exact image and enrollment evidence](CURRENT_STATUS.md)

[Existing reader access action](../Release%20and%20Status/ACTIONS-FOR-DAVID.md#tron-priority-demo-dedicated-trongrid-reader-access--8-november-2026)

### Recovery build for authenticated qualification

The replacement test build passed with zero errors and four warnings.
The 38 offline header controls passed with zero skips against these
binaries. The local native verifier checks their SHA-256 values before
opening a qualification run.

| Artifact | SHA-256 |
|---|---|
| TRON test assembly | b452f8a10aebc1e90d1861c310f9ebbd9718d331cb387adcec28631928a86041 |
| TRON provider assembly | d2c6e547054d9c24a33762b8e87b1ea9b2dd76792813e0e01c9d9e08b784357b |
| API Core assembly | aafc70259341ae9d2867c0ebb821b9fb793c65f1b39a4aa363ca953bd4c743eb |

The original catalogue digest matched. Authenticated execution remains
open. Preserve the prior anonymous evidence and original source cohort.
Once the protected key is available in the current process, run LocalState/COSMIC/HostedTronAuthenticatedQualification/verify-native.ps1 from the repository root. The recovery runner refuses a second native run while any prior native run directory is retained. Reconcile the original intent and result before any further request.

Recovery cleanup removed the three pinned Debug assemblies from the verifier path. The retained runner and source catalogue are present, and there is no native-run directory to reconcile. Once the dedicated reader key is available, restore the exact pinned artifacts or rebuild under the provider build lease and record the replacement hashes before authenticated qualification.

11 November UTC: HyperDrive verified a public TRON wallet balance through its native gRPC client at `6e4449a68f`. Independently probed the published Solidity gRPC GetBlock header-only method for the CQL reader. The genesis reply was 231 bytes with HTTP 200, gRPC OK, no transaction entries and the expected mainnet block ID. The height-1 reply had no block ID and failed validation, so heights 2 and 3 were not requested. This endpoint did not qualify the retained CQL catalogue. Preserve the original authenticated reader path and cohort. [PublicNode endpoints](https://tron.publicnode.com/) and [official TRON protocol](https://raw.githubusercontent.com/tronprotocol/protocol/master/api/api.proto).

11 November UTC: Railway marked standalone NEAR wallet revision `ae885fc092` removed after build logs reached image export. The building Cosmos revision `f4d314f7a3` includes NEAR. Development head `6ce625714f` adds the HyperDrive TRON wallet gRPC change on top of both and is queued in Railway. Live WEB4 still serves `fcf2a522e6`. The wallet change is separate from the TRON CQL header reader and its restricted access requirement.

11 November UTC: HyperDrive verified all 52 TRON native storage cases on disposable TRE nodes through the shipping TronNet gRPC adapter. Both modes used independent contract deployments, and native reads confirmed both contracts empty after cleanup. Development `6103e4e632` contains the verified CI changes. Hosted storage, retained ledger recovery and authenticated TronGrid CQL remain open. [TRON native CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43035175165)
