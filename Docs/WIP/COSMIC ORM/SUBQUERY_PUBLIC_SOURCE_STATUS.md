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

## WEB4 task integration and historical identity evidence

Task sourceca1a06e30 adds an explicitly configured query-only WEB4 reader,
compiler support, four native/Mongo preset plans and a real loopback HTTP/Mongo
fixture. Cloud run42438643607/job128602921160 compiled successfully but failed
two of eight controls: Core did not yet support the explicit query tariff, and
the preset fixture omitted its authoritative public-ID grant. Six passed, zero
skips; the native HTTP stage did not run. Core task6dbdc0366da8a6b279b724b5b3f7b2e7d9ee3904
adds the central read-tariff entry; parentc34107088 updates its gitlink and manifest
together. Exact manifest/gitlink validation passes. The preset fixture now binds
the actual authoritative catalogue and also asserts rejection without that grant.
Corrected source04ecc5d5085e15dc39a9047fbd793bd72f97a50d is pushed; cloud
run42439418296/job128605236758 isSUCCESS: all8boundary controls and the real
loopback WEB4/Mongo case pass, zero skips. Native3/2 and paired5/4 check exact
IDs/provider labels, foreign-private0/public3 and9usage receipts. Two SDK-owned
Mongo saves are fixture data; the success receipt proves the owned database
absent, and the cloud container teardown passed. Production auth/billing,
replication, snapshot reuse and browser acceptance are not inferred.
The exact source's dictionary regression42438643558/job128602920938 isSUCCESS.
No Development/settings admission has run.

Task WEB4 artifact13601640895/4625bytes, SHA256
b04c553ee2fcab1123d317075a138970a23af578d23e2c4f81f851f904aadfdf,
was verified entirely in memory: boundary TRX8/8 and web4 TRX1/1, both zero skips;
summary249bytes SHA256963a4d58477e53394da149cf98a29beab0e516a54a5440e9838461346bef2996;
cleanup70bytes SHA256baa8ee4636f9637b09dce94f3501b5fb79bc5bb63caaa2bf1ff10e0cd9c64426.
Compact receipt: `LocalState/COSMIC/RecoveryLaptopResume20261107/subquery-task-web4-qualified.json`.

Scoped candidate45734a552506b7249d5332685e075b83a9bf7329/Core06df0885c398e0b16749565852d12a9d499288d2
is now published on the existing `work/cosmic-teaching-release-candidate` branches.
It extends accepted Development74a7b60c0/Coreec7d440 with the named SubQuery closure;
the working task branches were not switched. Exact manifest/gitlink checks pass.
Exact candidate WEB4 cloud42440093218, dictionary42440093239 and accepted public
reader admission regression42440093241 are in progress. No image or Development
deployment request has been made. First publication preflight stopped before any
push because its exact changed-file set included an unchanged legacy file; fixed
the comparison to derive actual changed blobs, then normal fast-forward pushes
and fresh remote-SHA observations succeeded. No force push or worktree created.

Stable future host plan: `LocalState/COSMIC/HostedSubqueryDictionarySourceCurrent`,
ownerf0d6a43f-cdf4-4112-8397-b406571401a6, mapped IDs
4e399094-1338-810d-b2be-dc801dbf8889,
70b801fd-aceb-896f-99c0-7e4ecfdb31db,
bf29084c-a4b5-8fc7-9044-be3b58719d10. No native data is created.
Protected offline prefix `subquery-dictionary-public` preserves the acknowledged
251setting Dapr baseline and prepares253settings/four changed keys (source owner,
read admission, public grants and four presets). Live baseline recheck, exact
candidate/image gates and explicit journalled publication remain required.
No live settings have changed. Source JSON initially existed when an operator
import failed; completed only its review/plan from the same original owner, with
no ID regeneration, source/network operation or replay.

Superseded corrected-tariff run42439322377 was explicitly cancelled after the
remaining fixture failure was understood, before native HTTP execution; terminal
CANCELLED observed. Unrelated Waku run42438643581, triggered only by the shared
fixture edit, is terminalCANCELLED. Neither cancellation touched live cohorts.

The shared rollout verifier now derives SubQuery's three mapped IDs from the
fixed dictionary namespace/heights and explicit owner. All39protected candidate
controls pass, including foreign-catalogue rejection, owner-binding mismatch,
existing-enrollment rejection and preservation of other provider settings.
These are isolated temporary test fixtures, not actual live settings candidates.

An existing historical identity binding was located without another RPC request:
accepted release4e0d58f0868a7a4d0e6e1951a7331e04888e19c6 pins the same genesis
in `ArbitrumPublicAccountMetadataSource`. Its shared EVM reader requires chain
0xa4b1 and genesis equality before each native selection. The original accepted
Arbitrum cohort is recorded in [the1November report](HOSTED_PUBLIC_SOURCE_ACCEPTANCE_2026-11-01.md),
including native report SHA25638ceaac374ad51b2c77e8db865c5c3dccf8a6612df3bb2bca43eafbc357553a9.
This corroborates the immutable expected network pin from earlier accepted work;
it is not a fresh successful independent identity request or validation of the
specific dictionary matches. Original raw reports are not present on this laptop.
The new independent-RPC observation remains403/unavailable; no endpoint retry,
alternate route, proxy or header spoofing was attempted.

The immutable expected network pin is now independently corroborated by the
[StreamingFast Firehose network registry](https://github.com/streamingfast/firehose-core/blob/8a16569d5fea3c768251ecc5ea57f82553852492/well-known/chains.go).
Tagv1.9.10 resolves to8a16569d5fea3c768251ecc5ea57f82553852492; its Arbitrum One
entry names block0 and the exact same genesis hash. One bounded source-file GET
returned200/12254bytes, SHA25611aa69f1a4bc60a5940e9b6ff7e58a852fb78d26c2bd75ea28c2441759d477e0;
read/validated in memory, compact intent/outcome only. This independently binds
the expected network name/genesis pin used by the dictionary implementation.
Fresh RPC/consensus verification and validation of specific event matches remain
unavailable/unclaimed. This is static primary-maintainer registry evidence,
not an alternative route to the denied RPC service. The scoped public profile
continues to describe dictionary-reported activity, never verified full logs.

[Provider inventory](PROVIDER_CQL_AND_SANDBOX_STATUS.md) · [Current status](CURRENT_STATUS.md).
