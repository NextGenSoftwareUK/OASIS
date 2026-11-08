# SubQuery public-source qualification

7 November 2026 on the recovery laptop (8 November UTC). No new CQL or hosted acceptance claim.

The existing provider exposes a GraphQL HTTP client. Its activation/default
schema and legacy storage methods have not been certified by this probe.

The official [Polkadot manifest example](https://www.subquery.network/doc/indexer/build/manifest/chain-specific/polkadot.html)
publishes `https://api.subquery.network/sq/subquery/polkadot-dictionary` as an
optional dictionary. The official [query-service guide](https://academy.subquery.network/doc/indexer/run_publish/query/query.html)
distinguishes indexed-project GraphQL access from the chain RPC used by an indexer.
An RPC response therefore does not qualify this GraphQL source.

One bounded anonymous POST with the read-only query
`{ _metadata { chain lastProcessedHeight lastProcessedTimestamp } }` returned
HTTP404,95bytes, SHA256
`7421f2d09e7164dc7f16d5204bdf1fb161487e11baa2c75d09467bfb6ca3f26c`.
Redirects were disabled, timeout20seconds, response budget16KiB. No automatic
retry, alternate endpoint, API key, node install or database was used.
The result establishes failure of this particular documented example endpoint;
it does not establish that every SubQuery deployment is unavailable.

Original intent/public outcome are retained under
`LocalState/COSMIC/RecoveryLaptopResume20261107/subquery-dictionary-readiness.*.json`.
They prevent accidental repetition of this same readiness attempt.

## Separate Arbitrum dictionary source review

The official [Snapshot manifest](https://www.subquery.network/doc/indexer/quickstart/quickstart_multichain/snapshot.html)
publishes `https://dict-tyk.subquery.network/query/arbitrum`. This was reviewed as
a separate potential source; no configured provider endpoint was switched.

The documentation-style metadata query including `chain` returned HTTP422,
177bytes, SHA25688f6a5c3b843173e22b61827704b4d1ffa113fcb156735094531418c6033f744.
Its body was not retained, so the precise validation error is unproven.
An explicit JSON-RPC capabilities request from the pinned SDK V2 implementation
returned HTTP422/110bytes with GRAPHQL_VALIDATION_FAILED/no operation provided.
Thus SDK V2 support must not be assumed for this URL.

Upstream SDK commit `b35d488cfe4d9ff85cf2f6d1752ff7038e5a4eb8`,
[V1 dictionary implementation](https://github.com/subquery/subql/blob/b35d488cfe4d9ff85cf2f6d1752ff7038e5a4eb8/packages/node-core/src/indexer/dictionary/v1/dictionaryV1.ts),
defines the V1 metadata fields. Its exact field selection
`{ _metadata { lastProcessedHeight genesisHash startHeight } }` returned HTTP200,
155bytes, SHA25684c9e58befda8a9d03b4a15073700f7bfb7259625b1255c9fe65d78590515417:
lastProcessedHeight522211686, startHeight1, genesisHash
`0x7ee576b35482195fc49205cec9af72ce14f003b9ae69f6ba0faef4514be8b442`.
This is preliminary metadata evidence, not CQL or chain-identity acceptance.
No client automatically switches protocols or retries these attempts.

An independent genesis-block read using the official Arbitrum One RPC stopped
at JSON parsing. The one-off observation failed to retain the raw response and
HTTP status; both are recorded as missing, with no identity match claimed and
no automatic repeat. Preserve the original attempt rather than inferring a
network error/status. Each distinct request has its own intent/outcome under
`RecoveryLaptopResume20261107/subquery-arbitrum-*`.

The observer was then corrected to persist raw/status before parsing. One
separately journalled independent identity batch (chain ID plus genesis block)
returned HTTP403/706bytes, SHA256
`b3dbc17ed26cf4d67d6b47cb93e736f370718842b9a8e402e5e31b16c9bd2e76`.
The response explicitly identifies Cloudflare1010/browser_signature_banned,
access_denied and retryable=false. Stop requests to this endpoint; no header
spoofing, proxy, alternate route or automatic retry. This observed denial does
not retroactively establish the missing first response's status or contents.
SubQuery chain identity remains unverified, despite its own V1 metadata200.

## Native dictionary schema and bounded block evidence

Bounded schema reads succeeded: root fields are `_metadata`, `evmLogs` and
`evmTransactions`; EvmLog exposes id/address/blockHeight/topics; StringFilter
declares equality and key-set filters. Root313bytes SHA256
ba8760934386c11fa2e4057bba75ec87a44a1283d6950a3f7db7382953b7256a;
connection/arguments1540bytes SHA2561424d43ff67f646923f272d0599950cc932c271720cc98eb54f32d3ef430e2e0;
entity/filter/metadata1819bytes SHA256e3f009982c0c918050d7d68cd04c993b7194823ecd56667485ce1faceebeaab4.

Do not infer a generic entity API from introspection: a first:3 read with no
explicit order returned PRIMARY_KEY_ASC is not a valid EvmLogsOrderBy. The
declared enum includes ID_ASC, but a separate explicit ID_ASC read returned
not supported orderby option. Both failed queries and raw responses are retained;
a local summary initially mishandled the null connection, but request/status/body
were already retained and no request was repeated to repair that summary.

The pinned SDK's [V1 query builder](https://github.com/subquery/subql/blob/b35d488cfe4d9ff85cf2f6d1752ff7038e5a4eb8/packages/node-core/src/indexer/dictionary/v1/utils.ts)
specifies the actual dictionary contract: range-constrained filters,
BLOCK_HEIGHT_ASC, distinct BLOCK_HEIGHT and nodes containing blockHeight.
One exact-shape query limited to3nodes in [400000000,400010000), matching
Transfer event topic (shared by ERC20 and ERC721), returned HTTP200/245bytes, SHA256
67be5266179614e631f2fc166b77b922794babc98b6cac66faa86dbcd8b6d289.
Heights400000000,400000002,400000003 are distinct ascending decimal strings;
the same reported genesis identifier is preserved. Raw values/metadata remain
under subquery-arbitrum-sdk-v1-blocks.*. No full log/transaction or on-chain
finality claim follows from these dictionary matches.

Task-branch implementation now adds `SubqueryDictionaryBlockQuerySource` using
the shared bounded identity planner and explicit one-height dictionary requests.
It validates reported genesis and index coverage, rejects malformed/partial
GraphQL replies, bounds response bytes and concurrency, and exposes stable match
attributes without persisting the moving index cursor. Native writes/scans are
unsupported. This is dictionary activity evidence, not full log/header support.

Exact task source `c223bfc009107d58f7ae5b8ae4980dba4fa0be8b` passed cloud
[run42438203693](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42438203693),
job128601533699: sixteen controlled cases and one explicitly enabled real
dictionary CQL case, all executed/passed, zero skips. The native case activates
the source and checks three/two selected historical match identities through the
real CQL planner/executor. It does not query the denied independent RPC endpoint.

Artifact13600278331 is4398bytes, SHA256
`37efaaeca441fcec14065ead5397a8a75dc3a386650d4830a55dafafb37c890e`.
Both TRX reports were verified in memory: controlled25895bytes/SHA256
`90c258533638020411a04e436cc60e3663f6bd997fbb7e5c221368a40dfa1635`,
native2825bytes/SHA256
`eea83e5e60b18185f7c27d801a6b11df34e24312413f350da0b052dd7b55d997`.
Compact receipt is `LocalState/COSMIC/RecoveryLaptopResume20261107/subquery-dictionary-cql-qualified.json`.
All compilation/native testing was cloud-only; no local archive/build/runtime
retained. No hosted deployment, coverage count or public admission advances.

Next complete independent chain identity before admission, and qualify actual
manager/WEB4/federation/privacy/snapshot/replica/browser gates. Existing legacy
storage methods are outside this preliminary proof. No runtime installed, provider
configuration changed, accepted cohort replayed or owner action requested.

[Provider inventory](PROVIDER_CQL_AND_SANDBOX_STATUS.md) · [Current status](CURRENT_STATUS.md).
