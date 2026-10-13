# HyperDrive V2 current release gates

Updated 2026-10-12, inspected parent `a376d2578a7ec354677dd8c0f91785909e134742`. This is a current navigation checklist, not a replacement for the original all-provider scope or the detailed evidence log. **Not release-ready; do not merge yet.**

## Authoritative state

- Reuse `C:/Source/OASIS-hyperdrive-v2-gaps` and its existing branch. Do not create/rename `codex/` branches.
- Core pin: `a65e4ab591f294bcd5f9b319d74fb1ccf15095ad`; ONODE Core pin: `f9c985d0faff66b3b4525ebd9b4becba1f564f93`. Full Core 465 tests pass; WEB4/consumer refresh after attributed-property extraction remains required, as does deployed verification.
- TRON `TestResults/HyperDriveTron/pending-native-outcome.json` exists and must be preserved. Original transaction outcome is unresolved; NotFound is not non-submission proof.
- Cosmos/Sui replication ownership journals are absent after verified cleanup. Do not delete new pending journals blindly.
- Free space observed 91.79 GiB. Requested disk recovery is not achieved. No broad cleanup of unrelated services/data is authorized by this checklist.

## Evidence versus completion

| Area | Current evidence | Remaining gate |
| --- | --- | --- |
| Cosmos | 118 current unit tests passed; real encrypted holon roundtrip; selected real V2 replication/load-balancing and two failover cases passed | Full native/interface/options/API/live failure matrix and published Linux dependencies |
| Sui | 85 unit tests passed after bridge correction; real encrypted roundtrip and selected V2 healthy/outage tests passed | Full live custody/mint/escrow/options, all routing strategies/distributions and hosted API |
| TRON | 294 unit tests passed; read-only actual retained-node inspection preserves revision/fence; selected real read distribution/closed-transport failover passed | Original uncertain mutation reconciliation, current mutation/replication, all response classifications/options/API |
| Hedera | 274 selected non-funded tests passed; four bridge-marker cases; prior public mirror evidence | Funded consensus contract/storage/NFT/escrow and real V2 verification; no funded tests were silently substituted |
| Core routing | Latest full suite 465 passed; generic sync V2 routing fixed; preparation errors structured; later-record missing-key batch failure prevents provider writes; attributed properties extracted/encrypted before writes | Broader mixed-record/child preparation, successful generic/bulk replication/live distribution and original complete route matrix |
| WEB4 | 147 unit tests passed including actual registration-reference capture checks | Authenticated hosted endpoints with real providers, Linux published artifact execution and current consumer refresh after subsequent changes |
| Encryption | DNA helper combinations/negative checks, private whole-record tests, real Cosmos/Sui storage, DNA key references | Unified policy/key lifecycle, positive live custom-reference registration, Mongo end-to-end coverage, scale/rotation |
| SDK packaging | Retained Windows output imports/direct pins verified; 9 verifier tests; Docker installs/verifies dependencies | Actual published Linux/runtime execution; source-path or junction proof is not that proof |
| Usage recovery | Durable contracts/worker exist; worker refuses missing/conflicting evidence | Production resolver/registration and NFT execution host ownership, valuation, original signed binding, all-attempt receipts, crash/concurrency/exact settlement |

Counts are revision-scoped and overlap. Do not add them together or claim all endpoints/provider interfaces are covered by them.

## Work order

1. Implement production native recovery in the actual execution host with original signed evidence and approved valuation; do not register a guessed resolver in WEB4 merely because it is the billing authority. No zero-charge inference or rebroadcast.
2. Close unified encryption configuration/policy and live registration gaps without exposing legacy exempt private fields on-chain or breaking database indexes.
3. Complete live interfaces/options and failure/distribution matrices; preserve uncertain transaction ownership. Resume funded Hedera only when adequately funded, without real-money fees.
4. Prove authenticated API and published Linux packaging in approved existing infrastructure. Do not create another Docker/runtime environment by default.
5. Apply the documented higher-priority broader provider order (Mongo/SQLite, Ethereum/Solana, IPFS, clouds, L2s, Neo4j, remaining chains, protocol integrations). This does not remove any provider from the requested completion scope.
6. Audit every original requirement against current authoritative evidence. Merge/push to dev and promote only after all required gates pass; current green subsets are insufficient.

## Documents

- Detailed chronological findings, report paths and adoption recommendations: [provider I/O coverage](HYPERDRIVE_V2_PROVIDER_IO_COVERAGE.md).
- Existing three-layer/password/metadata system versus whole-record storage, exact exemptions and integration pros/cons: [encryption comparison](OASIS_ENCRYPTION_ARCHITECTURE_COMPARISON.md).

No sponsorship/mint fee policy is enabled by this document. Previously proposed NFT pricing remains unapproved; do not remove spending restrictions based on a green provider test.
