# HyperDrive/provider chat: Mongo traffic audit - 2026-10-20

## Findings and limits

No Atlas or hosted WEB4/WEB5 request was made for this audit. Recent retained Mongo tests in this continuation explicitly use the existing local replica at127.0.0.1:27029. No owned hosted test loop is active. This is NOT a complete13-20October traffic census: earlier shared/overwritten artifacts cannot reliably establish this fork's exact hosted request counts, frequency or transferred bytes. Older direct Atlas/Railway smoke checks must be treated as UNKNOWN, not zero and not relabeled local without evidence. A documented verification procedure/controller inventory is not proof that every endpoint was exercised.

## Attributable retained evidence

Times below converted from TRX -05:00 offsets to UTC. Test counts are NOT Mongo operation counts.

| Evidence | Endpoint/source | Recorded scope |
|---|---|---|
| Resumed EVM owner42150; Ethereum archived `TestResults/EthereumCurrent/ethereum-native-current-prior-E073BEE82467.trx`, Web3 archived `TestResults/Web3CoreCurrent/web3-native-current-prior-1BB221720B0C.trx` | Existing local Mongo27029 plus local EVM18545/18546 |215Ethereum mixed cases,59paging checks,5Web3 native cases passed. This continuation resumed an already-running owner; not represented as a new historical hosted run. |
| EVM owner59364; current Ethereum/Web3 TRX | Same retained local Mongo/EVM |20October06:58:55-07:00:01UTC:230Ethereum cases passed.07:00:06-07:01:49UTC:4/5Web3 cases passed, one SQLite fault-timing test failed. Failed routing.db/outbox retained; no replay/drop. |
| `TestResults/Web3CoreCurrent/web3-original-outbox-inspection-current.trx` | Same local Mongo27029, original database |20October07:10:11-07:10:12UTC,1inspection passed. Source performs database listing and two complete reads of44original outbox documents. Sorted-document SHA256 unchanged before/after: F29B2CC8F5D42486EE3AD89E717F96EBE6E9739F7F9730EF6673E5036012AD20. No claim/settlement/update/delete.271ms test duration. |
| Recent Azure acceptance and SOLID/native SDK work | Local Azurite10000; retained local SOLID13000; other recorded provider-specific runtimes |Not Atlas acceptance loops. Azure latest146native/76controlled cases. Do not convert these counts into Mongo query/byte volume. No MongoClient reference found in inspected Azure integration sources; routing tests explicitly register Azure/SQLite/LocalFile. |

Local provenance is explicit, not inferred from a test name: `Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1` sets OASIS_OWNED_RECOVERY_REPLICA_URI and SUBSCRIPTION_TEST_MONGODB_URI to127.0.0.1:27029; its Mongo mode sets OASIS_MONGO_REPLICA_SET_CONNECTION from that local value. EthereumPreparedOutboxTests and Web3NativeStorageTests.Outbox refuse a non-loopback27029 URI. Runtime is the already-installed Mongo8.3.11 at `C:/Program Files/MongoDB/Server/8.3/bin/mongod.exe`, data `C:/Source/OASIS/TestResults/SubscriptionUsageCurrent/mongo-data`; exclusive existing lease, no replacement database/environment. This identifies explicit Mongo endpoints for these runs, not an exhaustive packet capture of every dependency.

Earlier18-19October programme documents describe retained local Cosmos/Sui/EVM/provider tests and owned-Mongo fixtures, but source presence/shared reports alone do not supply a complete weekly run census or per-chat attribution. The separate COSMIC report inventories its own10retained19October reports/155results; those are not added to this chat's totals or counted as hosted traffic.

## Processes and test policy

All tracked owned EVM/Mongo/Azure/SOLID test handles in this continuation are terminal. Checked owned reserved ports27029/18545/18546/10000/10003 had no listener after completion. Local127.0.0.1:27017 listenerPID7304 has unknown ownership and was preserved; application/other-session processes were NOT stopped. No owned Atlas-backed loop was found or killed. Future development/regression stays on the existing local Mongo replica. Any further hosted acceptance requires a small bounded run coordinated with David, not a repeated soak against Railway/Atlas.

## Code/deployment distinction and latency hypothesis

At audit, cached origin/Development is the inspected deployed parentc4646ccbf; local branch is30ahead/1behind. `git diff --name-only origin/Development..HEAD -- Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS` is empty. This continuation's unpublished Azure/SOLID/Ethereum/shared-provider fixes therefore are not evidence of a newly deployed Mongo implementation. Dirty Mongo/Core/COSMIC work must be kept separate from deployed pins. No push, Atlas setting/tier change, concurrency removal or live-data mutation was performed for this audit.

The other investigation's `MONGO_RECENT_CHANGE_AUDIT_2026-10-20.md` and `QUEST_LOAD_SAVE_PERFORMANCE_2026-10-20.md` are the source for the raw-driver measurements, not measurements repeated here. Approximately419565bytes/4.58seconds is about90KB/s effective delivery. Similar full-result timings for old/current predicates plus1ms server explain make filter execution or a new HyperDrive algorithm insufficient explanations for most of that raw-read delay. Payload delivery, client/wire behavior and shared infrastructure/load remain hypotheses; Atlas transfer throttling/quota exhaustion is NOT established.

Deployed save safety changes documented there do add work: persisted-BSON observation, full replacement/equality condition, majority/journal acknowledgement, and repeated full avatar-detail reads in the tracker path. Larger correctly-preserved profile/quest metadata and child hydration can amplify traffic. They deserve measured optimization but do not explain away an independent raw read. Appropriate follow-up is a narrow authorized quest projection and atomic tracker-field update retaining identity/lifecycle/concurrency/history/replication/domain-capture invariants, plus historical data-size/Atlas management metrics. Do not disable safety, globally remove children, assume quota exhaustion, or rerun large Atlas controls to establish this report.
