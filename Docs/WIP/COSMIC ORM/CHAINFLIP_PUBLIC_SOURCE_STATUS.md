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
