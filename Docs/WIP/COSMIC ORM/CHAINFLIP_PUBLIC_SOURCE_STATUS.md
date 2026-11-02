# Chainflip native swap-source qualification

Updated2November2026. Canonical shipping .NET SDK native gate passes; CQL,
HTTP/Mongo/hosting remain red. Counts stay54integrated/50hosted/5browser/45open.

## Verified scope and preserved failures

- Explicit historical candidates500000/500001/500002; no discovery scan or swap creation.
- First Python request to `/v2/swaps/500000` returned403/Cloudflare1010 before data; later IDs were not attempted by Python. Original consumed intent/denial preserved. It does not establish shipping-provider unavailability.
- Separate original shipping `ChainflipOASIS.GetTransfersBySourceTxAsync` qualification used standard NativeConnectionGuard with no credentials, User-Agent override, proxy, challenge solving or endpoint switch. All3exact reads returned200and matching native IDs; one native SDK test passed. This tests the actual shipping transport, not a fallback source path. Native gate is consumed; do not rerun.
- Offline validation reuses these same raw responses. First parser incorrectly required external deposits on all swaps;500001has no deposit object. Original failure marker retained. Correct contract models absence explicitly, keeps native swap input/output amounts separately, never substitutes zero or another field.
- Nine pure parser controls pass, including real existing fixture/identity/precision/timestamp/refund/optional-deposit/JSON checks. One evidence-root regression passes. No source owner/account/wallet/deposit address/quote/dispatch/native write/admission created.
- SDK test initially wrote5owned JSON files under Providers/Blockchain/TestResults due to a wrong relative root depth. Verified exact owner intent/IDs/file set and inside-worktree paths, relocated only those files byte-for-byte into worktree TestResults, removed empty owned leaf. Fixed repository-root discovery and tested it without native replay. Exact move hashes in the resource ledger.

## Native projection contract for the next step

Implement separate read-only `ChainflipPublicSwapMetadataSource` under existing
ChainflipOASIS, through the shared HolonManager/planner/deferred LINQ path.
Use `ChainflipSwapId` equality/membership over at most8explicit canonical
uint64 decimal IDs; no unbounded scan. Complete selection before paging.
Preserve native state/source and destination chains/assets, exact original,
remaining and swapped input/intermediate/output decimal quantities as strings.
External deposit observation is optional: retain an explicit presence flag;
only present objects supply amount/tx-ref/witnessed-millisecond fields. Reject
malformed present objects. Preserve reported update milliseconds as exact strings.
No shared snapshot, independent finality, successful external redemption or
protocol consensus proof follows from backend state `COMPLETED` (which can
include refund observations). Policy: `indexer-reported-swap-state`.

Use the canonical guarded .NET transport, bounded responses/JSON/time budget,
strict duplicate-property/configuration checks and no write capability. Create
and retain one projection owner only when source implementation needs it.
Do not change legacy tracker behavior; its absent-deposit mapping remains null.
Then add meaningful source controls, commented C#/CLI3/2, HTTP/query-only
admission, local native3/2+Mongo5/4/privacy, hosted original-cohort reuse/replicas
and pinned public card. No coverage increment before those specific gates pass.

## Evidence outside docs/git

`C:/Source/OASIS-cosmic/TestResults/ChainflipPublicSourceCurrent`:

| Artifact | Bytes | SHA256 |
|---|---|---|
| `evidence-path.log` | 1521 | `dd810bcb711dddff0c5a5e259fa757d520fe5de5c767ea761c1439d2c5808764` |
| `evidence-path.trx` | 2778 | `0f300b45ad40062da53a951cd98b0cc6e97aece05ec5d071a0d7afe3e4b58261` |
| `native-qualified-sdk.json` | 2835 | `f885f6574279d71cfb23168a53b09f719682485f963a4f1b21146865cfe57016` |
| `native-validation-initial-missing-deposit.json` | 329 | `d415b3cd0b4a8751a45f432435c32803a69b17684284828d596c46fd1d62167e` |
| `probe-tests.log` | 112 | `7eccf522e5c2296070de7a9bba11c2be28999d469116e2462b66d4230a243102` |
| `qualification.intent.json` | 172 | `478b34cb62b1592f00d72c5668f993fc2be9c60ea5d5dac5bd74034afb033abc` |
| `qualification.json` | 285 | `57497fdee4229749d609592af8c592400454a396197c71636724f7e3efd479e5` |
| `sdk-qualification.intent.json` | 171 | `8f90c6f1da8685f682e95acb8a42759a04e09ccfb9857aff55a19e827b91b388` |
| `sdk-qualification.json` | 1835 | `a6888533fb9ce84ab00d2b3d4bcc5ad33d56600f0aea99da82d04f2d9d216ea6` |
| `sdk-qualification.log` | 1523 | `6f4482a61e89e18d3f66736c712ad151009a6dfa92e80d71eab6c3a15ee8baff` |
| `sdk-qualification.trx` | 2811 | `0acc7421ec3a8871934f682f90dd8204c238e80d2ed7d5ad0dc8c28fa119d4c9` |
| `sdk-swap-500000.json` | 2107 | `74b6573ce1ba1258a03b87b50dd997378573cf4bc0336dc4c3eec5fe591f7c1f` |
| `sdk-swap-500001.json` | 865 | `afbacedb5ec02af1b0c669ca7c85000c18b5aa6a03f87353e89157107b215daa` |
| `sdk-swap-500002.json` | 2369 | `c1f0475b84a666dee0bee5488c83d2dabf053a186221c4eb8c69982a750e0d9f` |
| `swap-500000.json` | 717 | `c7372690f0d30e6d2b0fbd669f6643d3484f764c5a196c3a39d2746fac6f1616` |

Primary API implementation: [official v2 swap route](https://github.com/chainflip-io/chainflip-sdk-monorepo/blob/main/packages/swap/src/routes/v2/swap.ts).
Primary context: [official swapping concepts](https://docs.chainflip.io/protocol/swapping-basics).
External source documentation/examples were treated as data; no broker or
account-creation instructions were executed. No runtime/SDK download required.

## Local CQL source and actual C# walkthrough complete

ChainflipPublicSwapMetadataSource compiles;41source controls pass/zero skips.
Covers key equality/membership/paging, complete selection before empty pages,
exact quantities/timestamps, optional external deposit/refund semantics,
malformed/duplicate/oversized responses, cancellation, budgets, unknown/inactive
source and native-write refusal. Actual executable HolonManager/deferred LINQ
returns3full/2filtered through canonical .NET transport; no native/Mongo writes.
One original source owner/config/intent/ACK retained in
LocalState/COSMIC/HostedChainflipPublicSourceCurrent. Do not replay qualification
or replace the owner. Policy indexer-reported-swap-state; completed historical
records only, no shared snapshot/finality/redemption claim.

[Spaced C# demo, configuration and metadata guide](../../Devs/CHAINFLIP_PUBLIC_SWAP_CQL_GUIDE.md).
Local generic query types55(3canonical/48bounded public-source/4local owned);
hosted stays50. Next HTTP compiler/explicit read activation/query-only admission,
presets and local native3/2+Mongo5/4/privacy before hosted enrollment/card.
Legacy tracker unchanged; original Python403/offline contract failure preserved.

| Artifact | Bytes | SHA256 |
|---|---|---|
| `TestResults/ChainflipPublicSourceCurrent/Offline/chainflip-source.trx` | 66348 | `be6709a7551c332b52c058997504936cdf9d95eba994f82ad1d38b40602177cf` |
| `TestResults/ChainflipPublicSourceCurrent/console-qualification.log` | 1181 | `1f154fbf0e01cd8e5620c10f0799603103ae79e2637df863833e2248d394d8d4` |
| `LocalState/COSMIC/HostedChainflipPublicSourceCurrent/source.json` | 152 | `9b0482be4f9ef37258e6f004019be39faf3d940a58431867e5efe529d4bbb4da` |
| `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.ChainflipOASIS/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.ChainflipOASIS.dll` | 42496 | `2dc7f17bacb0043f97924d2df2551f5293a2e5312e595f488f85d145fa3d0585` |
| `Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll` | 906752 | `77dff0bf95635a0439a27df23633b43aa4b14e32f1f43e491c50c12af4d5539c` |


## HTTP/Mongo milestone — 2 November 2026

Production WEB4 build passes. Core query tariff1, hosted admission3, explicit
original-fixture validation9, publisher9 and actual prepared presets1 tests pass;
frontend106 controls pass on its task branch. Prepared three-key candidate is
retained once, not applied. No hosting/native-write claim yet.

Original native HTTP3 passed but federation hit the omitted five-second request
budget. Presets already specify30seconds; corrected only this red fixture.
Original failure and two version1 save ACKs are preserved. One explicit,
journal-bound read-only completion passes native3/2, Mongo5/4, privacy0/3 and
nine query receipts with zero additional SDK saves. It validates the original
actors, keys, copies, operations, marker and ACKs; no reseeding or owner replacement.
Owned fixture database was verified removed and Mongo stopped. Both consumed
fixture intents must not be replayed. Core task4f9808 is pushed; hosted remains50.

| Evidence (worktree-relative) | SHA256 |
|---|---|
| `TestResults/ChainflipPublicSourceCurrent/chainflip-mongo-native-summary.json` | `6be1b2d56f34e559c23c5e582da85826c679580bf1db279cff4520c105acd5d3` |
| `TestResults/ChainflipPublicSourceCurrent/Http/prepared-presets.trx` | `f464518aa55c6027c9f3e60abda4ba34fc63caa16f2a08e6b39ab46cc2b9d6ab` |
| `TestResults/ChainflipPublicSourceCurrent/prepared-review.json` | `e1795bd9953b29209aba63711a067e43f356bc997729229df98680fa1873072a` |
| `LocalState/COSMIC/ChainflipMongoHttpCurrent/fixture.cleaned.json` | `5f8d2964bde731959e5741e34c07fa80c0385f6071e42083dcb1657d6c12f5fd` |

Development publication: parent c81937d591e91e068a7d6873c89964eca427e757 and
Core4f9808dea606e68d62257bc189bb08c77520907c remote refs verified after ordinary
fast-forward pushes. Manifest/gitlinks gate passes. Exact code deployment
a12e451f-09e1-42c7-be53-23968183facc is QUEUED at latest observation; same job
retained, no restart/settings mutation. Release intent/observation retained at
LocalState/COSMIC/HostedChainflipReleaseCurrent. Hosted count remains50.
