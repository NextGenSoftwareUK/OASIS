# Algorand public account CQL status

Updated 1 November 2026. Qualification complete; CQL/API/hosted/browser gates remain open. Counts remain 31 implemented query types and 29 scoped hosted HTTP providers.

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

1. Implement explicit read-only source with canonical SHA512/256 checksum, exact numbers and current-state semantics. Reject historical options, unknown config, writes and unsupported scans.
2. Controlled source tests and real shared-manager C#/CLI native3/2 demo; readable comments/spacing.
3. Independent query admission, shipping planner presets, local HTTP/Mongo/isolation tests and generic client preparation proof.
4. Guarded hosted enrollment, exact deployment success, native3/2 + Mongo5/4 + repeat reuse + foreign-private0 + replicas2/2. Preserve original save intents and ACKs.
5. Publish sandbox/local-demo links and verify browser interaction separately. Do not promote provider counts before the appropriate evidence exists.
