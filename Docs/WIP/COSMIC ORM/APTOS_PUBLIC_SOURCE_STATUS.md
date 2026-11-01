# Aptos public account CQL status

Updated 1 November 2026. Native CQL/shared-manager/CLI complete; API/hosted/browser remain open. Counts:33implemented query types and29scoped hosted HTTP providers.

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
3. Independent query admission and native/Mongo presets, shipping planner, local HTTP/Mongo privacy and focused client preparation.
4. Guarded hosted rollout, normal visitor3/2 and5/4, repeat reuse, independent visitor privacy and replica checks. Retain original write ACKs.
5. Sandbox/local-demo links and separate browser proof. No count promotion until the appropriate gate is proven.

## Native CQL milestone

AptosPublicAccountMetadataSource uses the shared query capability/planner/manager and read-only key-value base. Body/header network and ledger agreement, mandatory unique headers, exact decimal UInt64s, retained version ranges and complete selection validation precede output. No pruned/unversioned retry. Public account identity is the requested canonical URI, not an independent state proof.

- TestResults/AptosPublicSourceCandidateCurrent/aptos-source.trx:26passed/0skips, SHA256f584c81fafd559020fba155e5538d14d9c73cba4e48134c51a107aac4e8e6f9d.
- Actual CLI exit0,3/2; TestResults/AptosPublicSourceCandidateCurrent/demo-run.log SHA2569422b16016a6f340df667a782294e74820511a3b39f6f1effeef8df02c76ca11.
- [Developer guide](../../Devs/APTOS_PUBLIC_ACCOUNT_CQL_GUIDE.md). No API admission or hosted-provider count granted yet.
