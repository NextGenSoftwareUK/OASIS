# TRON CQL — controls pass, native demo red

Updated30 October2026. Current branch now includes the generic TRONOASIS CQL adapter and shared bounded-identity compiler. Seven integration controls pass/no skips. No native CQL, restart, Mongo pairing or browser proof yet.

## Implemented scope

Id equality/membership, constants/AND/OR and bounded paging, at most100 authorized keys. Shipping lifecycle-serialized encrypted backend/shared canonical RecordKey owns native reads and authenticated decode. Identity/read access checked, tombstones hidden, errors/cancellation discard partial rows. Unsupported metadata/content/order/graph queries fail before I/O. Separate reads do not imply a common-block snapshot;4MiB canonical payload budget is after SDK decoding.

Evidence: TestResults/TronCqlCurrent/tron-scoped-integration-controls.trx SHA25637D55A77115943633FDF9ECB48ED4F7DBC1643E9285088078371CB081C8E7DFB,7passed. Source/build/test writes only in OASIS-cosmic. No native process started or signing/storage key read by this audit.

## Original-source provenance audit

- Existing stopped oasis-tron-tre container:ef2843babd111bde2709159e6eaacf2350977cccee738d62d1bcbd8833c46a12; imagebf3a2b631ff2a1834530f69fcb90508c40970203aa1240b692223308cc90cae0. Read-only inspection reports exited/no mounts. Container presence is not fixture usability.
- Worktree TestResults/HyperDriveTron/pending-native-outcome.json absent; expected historical hash9A6B6E580DB63E5F20A91D9A345430FA72670CC041B278E3B44E00BD0FDEBAEE is documentary, not a recovered file.
- Checked Windows LocalAppData/OASIS/provider-evidence/tron absent; scoped WSL /var/tmp/oasis-tron-evidence,/var/tmp/oasis-tron,/var/tmp/oasis-runtimes/tron absent. This is not an exhaustive machine-wide recovery claim.
- Archived provider report records original unknown transactiondb05ea6d3067038a56dfc75b64d2c46c32824282e8242335ce007d1e34a415bb. Historical NotFound/empty receipt was explicitly not proof of non-submission. Its outcome remains unresolved; no replay/reset/fence fabrication/removal permitted.
- Run-ReusableTronInspection.ps1 requires exact original fence hash before startup. Keep this guard; do not bypass it or substitute a newly generated fence.

## Next bounded admission work

Inspect existing independent-fixture/SDK/deployment code and exact retained public contract/account identities without printing secrets. Original encrypted-record recovery requires the original key/provenance. If it cannot be recovered, define a separately declared owned synthetic source/contract/key/operation journal with explicit identities and preserve all original outcomes; never describe it as original recovery or select it automatically when old reads fail. Reuse suitable runtime/ledger, do not reset/recreate them. User has authorized missing demo environments, but no hosted/billable resource is authorized.

Before native mutation: prove source separation, journal exact signed preparation before broadcast, preserve unknown outcomes without retry, ensure local-only resource/fee bounds and exact ownership/teardown. Then prove selected3/subset2/missing, unchanged encrypted objects and restart, Mongo SDK/HTTP/browser5/4, rejection/failure behavior and exact temporary cleanup. Keep native inventory red until evidence actually exists.

Full ORM/HyperDrive/history/finality/mainnet parity remains open. Resource ledger retains audit/lifecycle. No install/download/restore/new environment/native mutation this milestone.


## Separate-source fixture preparation (30 October)

TronCqlDeclaredSourceTests.cs now compiles against shipping SDK/provider/CQL APIs. No native execution occurred. It requires explicit local source admission bound to the exact retained container/genesis, originalRecovery=false, separately supplied persistent encryption key and an independently owned state folder. It refuses pending setup and existing preparations without a completed source manifest. Deployment and storage operations reuse CreatePreparedNativeTransfers, whose callback flushes each signed preparation before the existing single broadcast. Acknowledgements and final manifest are durable before setup closure; queries never request signing material and must preserve every preparation hash.

Source inspection corrected an earlier provenance assumption: existing original test code contains reproducible public local-fixture signer/encryption derivation. This is not recovery of private user credentials or of the missing contract/operation manifest. The historical unknown transaction/fence remains unresolved and untouched.

Initial build stopped because integration-consumer obj/project.assets.json was absent. Restored that project from the existing local NuGet cache only; current fixture build succeeds0errors/2warnings. Logs TestResults/TronCqlCurrent/declared-source-build.log(initial) and declared-source-build-current.log(success). No download/install/newchain/key/source/native mutation. This is preparation, not native certification.

Next implement the exact owned-node/source-admission runner: persist a separately declared context before startup, validate stopped container/image/memory/ports and retained ledger, use existing retained-ledger launcher without initialization/reset, keep original-provenance observations separate from old fences, create one protected reusable demo key, compile shipping contract from cached compiler, run the explicitly selected new fixture, preserve all uncertain outcomes and verify graceful shutdown. Then prove restart/missing/unchanged source and Mongo SDK/HTTP/browser acceptance before any catalogue enrollment.
