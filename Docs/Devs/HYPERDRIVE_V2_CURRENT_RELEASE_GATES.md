# HyperDrive V2 current release gates

Updated 2026-10-13, inspected parent `beeb608a0`. This is a current navigation checklist, not a replacement for the original all-provider scope or the detailed evidence log. **Not release-ready; do not merge yet.**

## Authoritative state

- Reuse `C:/Source/OASIS-hyperdrive-v2-gaps` and its existing branch. Do not create/rename `codex/` branches.
- Core pin: `e196295c0e40934cd7ddf3bca5f03b7d5a6457ae`; ONODE Core pin: `f9c985d0faff66b3b4525ebd9b4becba1f564f93`. Full Core 477 tests passed previously. Current WEB4 build and 147 tests passed after attributed-property/null-metadata fixes; deployed verification and other consumer refresh remain required.
- TRON `TestResults/HyperDriveTron/pending-native-outcome.json` exists and must be preserved. Original transaction outcome is unresolved; NotFound is not non-submission proof.
- Cosmos/Sui replication ownership journals are absent after verified cleanup. Do not delete new pending journals blindly.
- Free space observed 91.79 GiB. Requested disk recovery is not achieved. No broad cleanup of unrelated services/data is authorized by this checklist.

## Evidence versus completion

| Area | Current evidence | Remaining gate |
| --- | --- | --- |
| Mongo/SQLite priority foundation | Historical Mongo43/narrow V2 stores; SQLite current-row metadata/type and provider-key isolation/unique index fixes; real integration6 passed | SQLite schema upgrade/backfill/history/parent/children/tracking/full mappings+avatars; current Mongo implementation/API/routing audit and complete real matrix |
| Cosmos | Actual retained-chain encrypted query test covers all golden exemption keys plus AppRegion; typed mint owner/Mongo proof; full119 and affected subsets passed; historical V2 live subsets | Typed/combined query parity across manager+provider, live owned mint/escrow/crash recovery, host/valuation/resolver, complete interfaces/API/failure matrix and published Linux |
| Sui | Actual retained-chain equality queries cover golden metadata keys+AppRegion; typed mint owner/Mongo proof; current affected subsets and earlier full97; historical V2 live subsets | Typed/compound/manager query parity, deployed host/receipt/valuation, crash recovery/live NFT ownership; complete custody/options/routing/API |
| TRON | 294 unit tests passed; read-only actual retained-node inspection preserves revision/fence; selected real read distribution/closed-transport failover passed | Original uncertain mutation reconciliation, current mutation/replication, all response classifications/options/API |
| Hedera | Typed mint/delivery owner uses separate stage correlations; batch response covers serials; current non-funded280 passed; offline real SDK+Mongo preparation6 passed | Actual public mint-owner/funded consensus storage/NFT/escrow, receipt/valuation/resolver/host integration and real V2; no funded proof substituted |
| Core routing | Latest full suite479 passed including golden metadata exemption compatibility; generic sync V2 routing and preparation fixes retained | Broader mixed-record/child preparation, successful generic/bulk replication/live distribution and original complete route matrix |
| WEB4 | Current build: 0 errors, 253 warnings; 147 unit tests passed; Core source-build/API/test-output DLL hashes match | Authenticated hosted endpoints with real providers, Linux published artifact execution and other consumers |
| Encryption | DNA helper combinations/negative checks, private whole-record tests, real Cosmos/Sui storage, DNA key references | Unified policy/key lifecycle, positive live custom-reference registration, Mongo end-to-end coverage, scale/rotation |
| NFT off-chain mode | Cosmos/Sui URI projection implemented; shared full111, current Sui affected13/Cosmos affected11, paired real-Mongo controlled on/off-chain4 passed | Actual live mint/readback/escrow with durable owner, external metadata availability/authentication, deployed request/response behavior |
| SDK packaging | Retained Windows output imports/direct pins verified; 9 verifier tests; Docker installs/verifies dependencies | Actual published Linux/runtime execution; source-path or junction proof is not that proof |
| Usage recovery | Durable contracts/worker exist; 77 tests passed against owned real Mongo replica set, initializer 1 passed; worker refuses missing/conflicting evidence | Production resolver/registration and NFT execution host ownership, valuation, original signed binding, all-attempt receipts, crash/concurrency/exact settlement |

Counts are revision-scoped and overlap. Do not add them together or claim all endpoints/provider interfaces are covered by them.

## Work order

1. Return to the user-approved provider order: MongoDB/SQLite, Ethereum/Solana, IPFS, clouds, L2s, Neo4j, remaining chains, protocol integrations. Current SQLite fixes are incomplete; historical passing subsets do not certify priority providers. Preserve native uncertainty fences and the original four-chain completion scope.
2. Close unified encryption configuration/policy and live registration gaps without exposing legacy exempt private fields on-chain or breaking database indexes.
3. Complete live interfaces/options and failure/distribution matrices; preserve uncertain transaction ownership. Resume funded Hedera only when adequately funded, without real-money fees.
4. Prove authenticated API and published Linux packaging in approved existing infrastructure. Do not create another Docker/runtime environment by default.
5. Complete production native recovery in the actual execution host with original signed evidence and approved valuation; no guessed resolver, zero-charge inference or rebroadcast. Do not let lower-priority chain work indefinitely displace the agreed order.
6. Audit every original requirement against current authoritative evidence. Merge/push to dev and promote only after all required gates pass; current green subsets are insufficient.

## Documents

- Detailed chronological findings, report paths and adoption recommendations: [provider I/O coverage](HYPERDRIVE_V2_PROVIDER_IO_COVERAGE.md).
- Existing three-layer/password/metadata system versus whole-record storage, exact exemptions and integration pros/cons: [encryption comparison](OASIS_ENCRYPTION_ARCHITECTURE_COMPARISON.md).

No sponsorship/mint fee policy is enabled by this document. Previously proposed NFT pricing remains unapproved; do not remove spending restrictions based on a green provider test.
