# Hedera / Hashgraph public account CQL status

Updated 1 November 2026. Qualification complete; CQL/API/hosted/browser gates open. Provider type is the existing `HashgraphOASIS`. Counts remain33implemented query types and30scoped hosted HTTP providers.

## Qualified contract

- Anonymous official Mainnet mirror endpoint `https://mainnet.mirrornode.hedera.com/api/v1/`; no SDK bridge, signing key, paid service or account creation.
- Three explicitly selected accounts: `0.0.2`, `0.0.3`, `0.0.98`. Proposed native predicate key `HederaAccountId`, equality/bounded membership over the configured catalogue.
- Canonical numeric shard.realm.number identities; exact nonnegative Int64 tinybar balances exposed as decimal strings. Preserve the native seconds/nine-digit-nanoseconds balance timestamp as a string.
- Each request is `accounts/<id>?limit=1`; do not follow transaction-history pagination or expose the incidental transaction payload as a query result.
- Require response account identity and explicit `deleted: false`; reject malformed balances/timestamps and unexpected transaction counts.
- Consistency is **LatestIndexed**. Returned balance timestamps can be much older than the request and differ across accounts. No freshness SLA, shared snapshot or caller-selected historical view is promised.
- Account responses have no independent network ID. Mainnet binding is the explicitly configured official endpoint, also recognized by the existing provider. Do not invent a network/ledger proof from the nodes response. Future configuration must reject endpoints for another network instead of merely labelling them Mainnet.
- Mirror data is API-reported indexing, not independent state/consensus/signature verification or proof of an avatar's account ownership. No source writes.

Primary references: [official Mainnet Swagger](https://mainnet.mirrornode.hedera.com/api/v1/docs/), [Hedera's public mirror announcement](https://hedera.com/blog/now-available-public-mainnet-mirror-node-managed-by-hedera/), [SDK versus mirror reads](https://hedera.com/blog/querying-data-on-hedera-sdk-vs-mirror-node-rest-api/). Existing provider endpoint policy: `HashgraphOASIS.ResolveMirrorEndpoint`.

## Evidence

`Scripts/Sandbox/Probe-HederaPublicReadiness.py`: three bounded anonymous GETs, three account observations, zero source writes. Redirects and duplicate JSON are rejected. `test_hedera_public_readiness.py`: eight boundary tests pass, no skips or network dependencies.

- Raw `TestResults/HederaPublicSourceCandidateCurrent/readiness.json`,1423bytes, SHA256 `d999e38c182d413a7682c11a761b7ae802b5933e63c34385d6cdb24f94e94efc`.
- Durable `LocalState/COSMIC/HostedHederaPublicSourceCurrent/{source,catalogue}.json`,1688bytes. Stable sourceOwner created once; preserve it and all three identities. Candidate schema is not consumed by shipping code yet.
- Earlier memory-only network/nodes probe has no ledger ID; it is not used as a network certificate.

## Next gates

1. Implement explicit read-only mirror source: strict Mainnet endpoint, exact identity/balance/timestamp validation, bounded responses/cancellation and complete-page failure. Reject unknown config/signing/history options and unsupported scans.
2. Controlled source tests and real shared HolonManager/LINQ3/2 with readable C#/CLI walkthrough.
3. Independent query admission, native/Mongo presets, shipping planner, local HTTP/Mongo privacy and focused client preparation.
4. Guarded hosted rollout, visitor3/2+5/4, repeat reuse, foreign-private0 and exact replica checks; retain original save intents/ACKs.
5. Sandbox/local-demo links and separate browser verification. No provider-count promotion before the appropriate acceptance gate.
