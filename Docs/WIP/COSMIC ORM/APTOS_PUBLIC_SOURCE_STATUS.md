# Aptos public account CQL status

Updated 1 November 2026. Native CQL/shared-manager/CLI complete; API/local HTTP complete; hosted/browser remain open. Counts:33implemented query types and29scoped hosted HTTP providers.

## Qualified contract

- Reuse the existing provider's anonymous Mainnet REST endpoint, `https://api.mainnet.aptoslabs.com/v1/`; no key, paid service or account creation.
- Read ledger info once per query, require body/header Mainnet chain ID1 and exact ledger/oldest-version agreement, then request all selected accounts at that specific version.
- Three fixed system-account addresses (1,3,4), each represented canonically as lowercase `0x` plus64hex characters. Proposed native query key: `AptosAccountAddress`, equality or bounded membership within the explicit catalogue.
- Account response fields are exact `sequence_number` UInt64 decimal string and full32byte `authentication_key`. Authentication key is public chain data; it does not prove signing authority or current avatar ownership.
- Responses must remain on Mainnet and report a retained range containing the requested version. If it is pruned or unavailable, fail the whole query; no unversioned retry or endpoint fallback.
- Per-query version is chosen freshly by the eventual adapter, rather than pinning a configuration version that will be pruned. The retained observation records historical evidence only. This is API-reported version selection, not independent state/consensus proof or a cross-provider snapshot.

Primary references: [Aptos account lookup](https://aptos.guide/rest-api/operations/get_account), [fullnode API and endpoint](https://aptos.guide/build/apis/fullnode-rest-api). Public account responses do not echo the queried address; the adapter must preserve the explicit request identity and validate the documented response, without claiming an independent account-state proof.

## Evidence

`Scripts/Sandbox/Probe-AptosPublicReadiness.py`: four bounded anonymous GETs, three account responses at requested version7854716014, zero writes. Redirects are rejected. `test_aptos_public_readiness.py`: seven boundary tests pass, no skips/network dependencies.

- Raw `TestResults/AptosPublicSourceCandidateCurrent/readiness.json`, 2,079bytes; SHA256 `552fb4a5622db59d60f103388dee567108b0e4867785c53aea31502519bfa4d9`.
- Durable `LocalState/COSMIC/HostedAptosPublicSourceCurrent/{source,catalogue}.json`, 2,516bytes. SourceOwner created once; preserve it and the three account addresses. Candidate schema is not consumed by shipping code yet.

## Next gates

1. Complete: explicit read-only source with per-query version selection, canonical native values/addresses, cancellation/bounds and whole-page failure. Unknown configuration/history overrides/signing fields/unsupported scans reject.
2. Complete:25controlled tests plus1real shared HolonManager3/2 pass/no skips; actual readable C#/CLI3/2 passes.
3. API admission/planner2tests, local native HTTP/Mongo privacy1test, Core tariff1test, pure configuration3tests and focused client1test pass. Protected candidate and actual four-preset export planning pass after verifying exact applied Algorand variables; no Aptos mutation.
4. Guarded hosted rollout, normal visitor3/2 and5/4, repeat reuse, independent visitor privacy and replica checks. Retain original write ACKs.
5. Sandbox/local-demo links and separate browser proof. No count promotion until the appropriate gate is proven.

## Native CQL milestone

AptosPublicAccountMetadataSource uses the shared query capability/planner/manager and read-only key-value base. Body/header network and ledger agreement, mandatory unique headers, exact decimal UInt64s, retained version ranges and complete selection validation precede output. No pruned/unversioned retry. Public account identity is the requested canonical URI, not an independent state proof.

- TestResults/AptosPublicSourceCandidateCurrent/aptos-source.trx:26passed/0skips, SHA256f584c81fafd559020fba155e5538d14d9c73cba4e48134c51a107aac4e8e6f9d.
- Actual CLI exit0,3/2; TestResults/AptosPublicSourceCandidateCurrent/demo-run.log SHA2569422b16016a6f340df667a782294e74820511a3b39f6f1effeef8df02c76ca11.
- [Developer guide](../../Devs/APTOS_PUBLIC_ACCOUNT_CQL_GUIDE.md). No API admission or hosted-provider count granted yet.

## API and local HTTP milestone

Native3/2, two private Mongo creates, paired5/4, foreign-private0/public3 and nine measured query receipts pass. Auth/billing remain controlled fixture infrastructure; no production/hosted replication claim. Exact cosmic_cql_aptos_http_demo owner marker debf8b77-f612-4ceb-9ead-2a34825bb76b verified then dropped. Owned Mongo stopped/27029 absent. Durable AptosMongoHttpCurrent original intents/save ACKs/cleanup retained; never replay.

- TestResults/AptosPublicSourceCandidateCurrent/aptos-admission.trx:2passed/0skips; SHA256f6613342f4645b455d850b4010a9e8daed8e7d3fbc0af3ac719cfebb2f6285e3.
- aptos-tariff.trx:1passed/0skips; SHA2565a498eec7af669c66cfc8f0b877c1b6e2e2f01f5899ee55baec84e28dc0781c9.
- aptos-mongo-http.trx:1passed/0skips; SHA256f1a80fe614839b3bb884d5972176a72d6886aa4f311f4c3482cd5c23413f950f.

Coree845c62 task branch pushed/ref verified, not Development. Keep this later batch out of current Algorand release7a9ca308e. No hosted Aptos snapshots/settings/cohort created; prepare only after Algorand enrollment completes so before-state is accurate.

## Protected candidate ready

Aptos prepared-host milestone1November: protected3setting candidate prepared against the exact applied Algorand settings; actual4preset shipping planner1test passes/no skips. Separate deployed Node4GET reachability/version probe passes, not hosted CQL. Candidate remains unapplied, no visitor cohort. Complete Algorand settings/acceptance before next application batch.

- TestResults/AptosPublicSourceCandidateCurrent/aptos-prepared.trx:1passed/0skips,3329bytes, SHA256f0615584ca1cfd1fb61ac01326423938c3e061a3227b72252efdcb94e644d7c5.
- hosted-egress.json:646bytes, SHA256f8fee42b40bf00ae3a6f13ab70c25bebf4092734d3e75d8ff9235ca20bbacac3.

Guarded3setting apply must wait for the exact future Aptos application SUCCESS. No overlap with current Algorand deploymentf3ee7f37.

Development publication1November: parent1cfb893c80166bf497c3112de359126a8a456fa4/Coree845c62261a74ae800c89a4175daddf0cc9d8f16, both remote refs verified after fresh fetch/ancestry checks. Committed manifest/gitlinks and5SDK closures pass. Exact appbb19d2ab-c34e-492a-9274-5e49c6e4c3b5 QUEUED. Settings candidate unapplied; no hosted visitor/save cohort. Observe same job, never restart for observation timeout.
