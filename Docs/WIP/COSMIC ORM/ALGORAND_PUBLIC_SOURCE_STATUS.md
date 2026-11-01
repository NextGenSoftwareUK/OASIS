# Algorand public account CQL status

Updated 1 November 2026. Native CQL adapter and shared-manager tests pass; API/local HTTP complete; hosted/browser gates remain open. Counts: 32 implemented query types, 31 deployed query-tariff types and 29 scoped hosted HTTP providers.

## Qualified contract

- Anonymous Algonode Mainnet algod and indexer endpoints; no signing key, paid service or source writes.
- Both algod `/versions` and indexer block zero report `mainnet-v1.0` and genesis `wGHE2Pwdvd7S12BL5FaOP20EGYesN73ktiC1qzkkit8=`. This is API-reported network binding, not independent consensus verification.
- Three unique, checksum-valid uppercase account addresses selected once. Proposed native key: `AlgorandAddress`, equality or bounded membership over the explicit catalogue.
- Current account point reads use `exclude=all`. Amounts/rewards/minimum balance are exact UInt64 values, exposed as decimal strings. Account and response rounds must agree per response.
- The free endpoint explicitly rejects account rewind (`round=`) with HTTP400. The new contract explicitly requests current state, never silently removes a requested round. Rows may reflect different observed rounds; no shared or historical snapshot promise.
- Unknown status, deleted account, mismatched identity/round, invalid checksum, malformed numbers, duplicate JSON and inconsistent reward totals fail visibly.

The official [account lookup reference](https://dev.algorand.co/reference/rest-api/indexer/operations/lookupaccountbyid/) defines response-round and excluded-resource fields. Its generic round parameter does not establish that the selected free endpoint supports rewind.

## Evidence

`Scripts/Sandbox/Probe-AlgorandPublicReadiness.py`: six bounded anonymous GETs, three complete account observations, zero source writes. No redirects or endpoint fallback. `test_algorand_public_readiness.py`: eight tests pass, no skips, no network dependencies.

- Raw report: `TestResults/AlgorandPublicSourceCandidateCurrent/readiness.json`, 2,939 bytes, SHA256 `dbfab84994a67ad9814e8d63abee5eab77587f353b04df97fad9018fdc17c837`.
- All three observations happened at round66537217 in this run. That coincidence does not establish future multi-row snapshot consistency.
- Durable original catalogue/config: `LocalState/COSMIC/HostedAlgorandPublicSourceCurrent/{catalogue,source}.json`, 3,474 bytes. One stable sourceOwner created; never regenerate it or reselect addresses to hide failure. Config is a candidate contract, not currently consumed by shipping code.

## Next acceptance gates

1. Complete: explicit read-only source with canonical SHA512/256 checksum, exact numbers and current-state semantics. Historical options/unknown config/writes/unsupported scans reject.
2. Controlled source tests and real shared-manager3/2 pass. Commented C#/CLI walkthrough added; actual CLI build/run passes3/2.
3. Complete: independent query admission, actual four-preset shipping planning, local HTTP/Mongo/isolation and focused generic client preparation.
4. Guarded hosted enrollment, exact deployment success, native3/2 + Mongo5/4 + repeat reuse + foreign-private0 + replicas2/2. Preserve original save intents and ACKs.
5. Publish sandbox/local-demo links and verify browser interaction separately. Do not promote provider counts before the appropriate evidence exists.

## Native CQL milestone

AlgorandPublicAccountMetadataSource uses Shared.KeyValueStorage and explicit read-only activation, canonical address membership, per-query network checks and exact per-response amounts/rounds.22tests pass (21controlled +1real shared HolonManager3/2), no skips. Raw TestResults/AlgorandPublicSourceCandidateCurrent/algorand-source.trx SHA256172d5e57a9e0f221644093bca61883d7d9dbac6a2854460aa92e76f7ff48f436. Explicit modern BouncyCastle2.6.2 alias resolves inherited duplicate crypto assemblies; no fallback. [Developer guide](../../Devs/ALGORAND_PUBLIC_ACCOUNT_CQL_GUIDE.md).

Actual CLI exit0,3/2; TestResults/AlgorandPublicSourceCandidateCurrent/demo-run.log SHA2567143af57a95707516e11b3f6a1d6f52fd4e1ab0987887d64696d538198f67258. Build0errors; existing dependency warnings remain. No source/Mongo writes.

## API and local HTTP complete

Three API/prepared tests, one Core tariff test and one native loopback HTTP/Mongo test pass, zero skips; three pure publisher controls and one generic frontend preparation control pass. Corec17db5f pushed and remote ref verified. Protected3setting candidate prepared only, wallet DNA byte-identical; no settings applied or hosted Algorand cohort yet.

Native3/2, two private Mongo creates, paired5/4, foreign-private0/public3 and nine query receipts pass. Auth/billing are controlled fixture infrastructure, not production billing or hosted replication proof. Exact DB cosmic_cql_algorand_http_demo marker d2767954-0a23-4261-8031-866081c5715f verified then dropped; Mongo stopped/27029 absent. Durable AlgorandMongoHttpCurrent intents/save ACKs/cleanup retained; never replay.

- algorand-admission-prepared.trx:3passed, SHA256ad7948cd073a30d2997342b2486291c72fb747034b5b4ab6523778340a23c5a3.
- algorand-tariff.trx:1passed, SHA2561cb53255d58f356462c940849e3d69a132c642bf5c48f3ab4d00f400f59ef6da.
- algorand-mongo-http.trx:1passed, SHA2561d68b685f29f5246b10145401bfc793943963584eacb895868ffb06c0c7e5544.

Raw reports only TestResults/AlgorandPublicSourceCandidateCurrent. Separate deployed Node probe reports both network endpoints and one configured account200; reachability only. Next verified Development batch, exact application SUCCESS, guarded3setting application and one journalled rollout, then hosted visitor native/Mongo/isolation/replica checks. Current-state account values may change between observations; copies are captured observations, not synchronized historical chain snapshots.

Development publication: parent7a9ca308e5a4c94a6529b1848e41571d1180d3b7/Corec17db5f5a062822c92f2f7250fe60e484b4e5967, both remote refs verified. Manifest/gitlinks and five SDK import closures pass. Exact application7bb36bf0-4f29-4869-8a97-c2f305876b40 QUEUED. Three hosted settings remain prepared only. Observe same handle; do not resubmit on timeout.

Rollout update1November: exact app7bb36bf0 SUCCESS at7a9ca308e/Corec17db5f. All3Algorand values observed exactly; protected whole candidate equality verified. One journalled settings deploymentf3ee7f37-769e-4c02-b70c-7b54837a6e45 submitted. No visitor/save cohort yet; hosted acceptance remains pending. Later Aptos task work excluded.

## Hosted HTTP complete

Algorand hosted completion1November:7a9ca308e/Corec17db5f, exact settingsf3ee7f37 SUCCESS. Native3/2, paired5/4, repeat0new/2existing, foreign-private0/public3, replicas2/2 pass. Current-account/per-response-round semantics, exact quantities; no source writes/historical/shared snapshot. Counts30hosted HTTP/5browser/25open,32deployed query-tariff types. [Proof/hashes](HOSTED_PUBLIC_SOURCE_ACCEPTANCE_2026-11-01.md#additional-algorand-acceptance).

Original owner/visitor/save/completion ACKs retained in HostedQueryAcceptance-AlgorandOASIS. Original sourceOwner/account catalogue unchanged. Website mainada6e92 publishes only this verified card; Aptos remains staged. Browser proof remains open. Earlier pending statements describe milestone history.

Website mainada6e92 live HTML equals exact committed bytes, SHA256cfff7aa51aafe061966682309ed258ca3e000d313b79c9dbdf678224375c95be. Aptos card absent from main/live. Focused Algorand preparation1pass; browser gate remains open.
