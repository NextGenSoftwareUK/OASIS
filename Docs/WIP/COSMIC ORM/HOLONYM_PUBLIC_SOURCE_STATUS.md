# Holonym / Human ID public CQL source review

7 November 2026. Native CQL and loopback WEB4/Mongo qualified; hosted/browser gates remain open.
No new runtime, account, proof, wallet or native business write.
Self Protocol image42447469381 remains the active rollout priority.

The existing HolonymOASIS uses real Nethereum calls to the Optimism Hub V3
sbtOwners mapping. It distinguishes absent/expired/revoked state and preserves
transport failures as errors. Keep that implementation and existing tests.
Current constants match the [maintainer's legacy deployment documentation](https://docs.passport.human.tech/building-with-passport/individual-verifications/major-concepts/sbts).
That documentation marks EVM SBTs as legacy and directs new EVM integrations to
attestations; Stellar's SBT integration is a separate current representation.
A new current-verification demo therefore needs the attestation contract,
not a renamed legacy proof. A deliberate historical SBT profile could remain
separately named, without automatic fallback between those representations.

The [published attestation contract](https://docs.passport.human.tech/building-with-passport/individual-verifications/major-concepts/attestation-protocols)
names production index https://mainnet-rpc.sign.global/api/index/attestations,
attester0xB1f50c6C34C72346b1229e5C80587D0D659556Fd and Optimism production schema
onchain_evm_10_0x1. Its raw payload schema is string circuitId,uint256[] publicValues.
Qualification must bind network/schema/attester and interpret these unsigned
values exactly, including action, expiry and issuer. Public attestations alone
are not a fresh independent identity proof. The page also labels a Base schema
onchain_evm_10_0x8 despite listing Base's chain8453: resolve that discrepancy
against the actual protocol before any Base admission; do not guess an encoding.

Native qualification remains agent-owned: pin the maintained Sign read ABI/SDK,
prove its bounded page and exact-ID contract, retain original raw/status before
parsing, select three explicit authentic records and verify all source bindings.
No unbounded scan or invented missing records to satisfy a three-row demo.
Require malformed/cross-schema/partial-error rejection, exact-ID CQL3/2,
WEB4/Mongo5/4/privacy, immutable deployment and hosted reuse/replica/browser gates.
The unrelated untracked SignProtocol provider directory belongs to other work;
do not stage or modify it for this review.

For the legacy implementation, primary Solidity source is pinned at
[holonym-foundation/id-hub-contracts cb757697740d9b56d5aef4f7169ea41ee47c1c83](https://github.com/holonym-foundation/id-hub-contracts/blob/cb757697740d9b56d5aef4f7169ea41ee47c1c83/contracts/Hub.sol).
It exposes the mapping and a separate public-values-bearing SBT representation.
The mapping getter does not return the dynamic array; preserve that distinction.
Only tree paths/hash retained locally:81528bytes/SHA256
e3aabad70436a60ec3a2a062816c996b3dbf08078ea25178e17bfd80328971d1.
No source archive/clone, package or native environment installed. The docs edit
link could not be fetched; no access restriction was retried or bypassed.

[Inventory](PROVIDER_CQL_AND_SANDBOX_STATUS.md) · [Current rollout](CURRENT_STATUS.md).

## Current indexed records and exact-ID contract

One bounded request using the [Sign REST contract](https://docs.sign.global/developers/api/rest-attestations/)
(size3/page1, fixed Optimism schema/attester/mode) returned HTTP200/application-json,
5985bytes, SHA256f1fe2a4cd689bbb4fcbd25a3e2d41f4bdf77dc4263090289ca46069e594c5d12.
Exactly3distinct native IDs: onchain_evm_10_0x3248d, onchain_evm_10_0x3248c,
onchain_evm_10_0x3248a. Envelope, chain10/modeonchain, published attester,
schemaid/data types and canonical bounded string/uint256[] ABI layout checked.
The advertised total147138 is index metadata, not a scanned or imported count.

The observed payloads contain5unsigned values, while the Human ID field table
lists6indices. The third circuit's literal string omits the documentation's
leading zero nibble. Schema type is string; preserve that native literal.
The first local observer's fixed66byte assumption failed at the third row;
reconciliation used the original retained bytes and actual ABI string contract,
without another request. No assigned field meanings, normalization workaround,
proof validity, active identity or native consensus claim.

One documented exact-ID GET for onchain_evm_10_0x3248d returned200/2519bytes,
SHA2562abd129de8e9fd3bf2447f67ac409da071b5c2880687e4391d813323906dd236.
All19core attestation fields match the original listed record, including raw
payload and recipients; direct fullSchemaId/rawschemaId/nested schema/data
bindings agree. Initial whole-envelope equality stopped on observed exact-route
extensions and index-maintenance timestamps, not changed credential contents.
Reconciled from the same response. Preserve sync/extra fields as maintenance
metadata; they must not silently change a prepared native record's content hash.

Next source can use this single exact-ID read contract with the fixed3IDs;
listing was discovery only, never an error fallback. Preserve raw circuit,
all exact uint256 values, schema/attester/record identity and reported state.
Broader semantic identity verification needs authoritative emitter/circuit
layout resolution. Do not substitute a legacy SBT call on index/read failure.
Require real SDK ABI round-trip validation and negative controls before CQL.

All raw/status captured before parsing under existing recovery state:
holonym-current-attestation-readiness.* and holonym-exact-attestation-readiness.*.
Redirects/proxies/retries disabled; reads only, no SDK installation. These are
source readiness observations, not CQL3/2, independent on-chain confirmation,
manager/HTTP/federation, hosted replication/reuse or browser acceptance.

## Exact-ID CQL implementation awaiting cloud qualification

HolonymAttestationQuerySource adds a current Sign-index read representation;
legacy Nethereum SBT methods remain unchanged. It preflights the complete ID
catalogue, reads only the fixed native exact-ID route, binds record/schema/
network/attester and payload types, then uses Nethereum7.0 ABI decode plus exact
re-encoding. Allocation-safe dynamic string/array framing checked first; UTF-8
literal and all unsigned values retained without guessed identity meanings.
Observed null revokeTimestamp is preserved, not changed to zero; a reported
revocation requires its timestamp. Moving index maintenance fields excluded.
No listing fallback, writes, scans, callbacks or private document operations.

Cloud workflow qualify-holonym-attestation.yml requires25controlled cases
(23new exact-ID/protocol/ABI/privacy-boundary checks plus2legacy controls),
then1real current-index CQL3/2 case with no skips. Full uint256 values, short
biometric string, allocation bombs, malformed offsets/padding/trailing bytes,
source mismatches, partial errors and stable snapshot metadata covered.
Nethereum ABI7.0.0 API contract read from the already cached NuGet README and
pinned package repository revisiona7aadf5cc89478dd91ef406a8780647acd484925;
no package installed. This implementation is not yet compiled or accepted.
No WEB4/Core read tariff, hosted admission, deployment or count increase.

First cloud42448890292/job128633651813 compiles but3positive activation checks fail;22/25controlled pass, native stage skipped. Root invariant: NativeRecord permits portable scalar metadata, not raw arrays. Normalize complete value/recipient arrays to explicit canonical JSON string fields HolonymPublicValuesJson/HolonymRecipientsJson at the provider boundary; preserve exact uint256 strings and raw ABI payload, no core-contract relaxation or discarded elements. Positive assertions now include the retained exception for diagnosis. Corrected qualification follows; no count/deployment claim.

Corrected cloud42449086090 on25d8f3a03b39b5597835f9af412922c5cff63d79 succeeds:25controls+1actual native CQL3/2, zero skips. Artifact13605126354/5471bytes SHA256faa87fde3963a77c424d098c482105427a54844c0ca3746f5c5d445e83d01f23 and both TRX counts/hashes verified entirely in memory. The only source correction normalized unsupported array types at the provider boundary; all3previously failing positive tests now pass. Count122local/93hostedHTTP/28browser. WEB4 explicit owner/profile/read-compiler allowance,8boundary controls and real shared Mongo/HTTP fixture prepared; Coreabaedaf read-only tariff pushed on original task branch, gitlink/manifest match. Self immutable candidate916c62210 remains separate and already queued for deployment; Holonym changes excluded.

WEB4 source91b08d8d6048656e0195fe9a2d8f401c7cbcc929 cloud42449470293/job128635384346 succeeds:8boundary+1real HTTP/Mongo case, no skips. Artifact13605033325/4617bytes SHA2564faf70866cf5da2939c37ebd4e23478f2b54fa5b382cf0739712af9c5a9c9ebd, both TRX/hash/summary and databaseAbsent:true verified in memory. Actual native3/2,paired5/4,foreign private0/public3,2SDK saves,9receipts. Not hosted or replica/browser acceptance. Protected fixed-catalogue planner adds Holonym native IDs/source binding;39candidate controls+5publisher guards pass with self-cleaning fixtures. No Holonym owner/snapshot/settings rollout created; wait for accepted Self batch before advancing the existing candidate refs.

## Scoped release candidate after accepted Self batch

Candidate638993968dfd9d43467d97f42d7931892df8c549/Corebe4636a6436ad304a08de446ac6a456ea76960dc uses accepted916c62210/Core25a296e as base,14scoped paths only. Shared/other provider changes excluded; source blob matches accepted task25d8f3a03. Manifest/gitlinks verified, existing candidate refs advanced normally after fresh checks. First private generator stopped at Python syntax parsing (newline escaping); corrected literal generation with repr before any Git/environment operation. Temporary indices removed in finally, no worktree/runtime/build.

Both new qualification workflows explicitly include existing candidate branch, so push triggers exact runs once. Attestation42451496266/job128641478981 passes25+1native/no skips; artifact13606136927/5460bytes SHA2562391d24b236819d0d1dc7c42ad9376f0533894f692fbd79bc580be8f7d72ab86 verified in memory. WEB442451496300/job128641478943 and admission42451496310/job128641479004 remain observed active. No manual duplicate dispatch. Guarded dispatch-holonym-image.py prepared only, requires all exact success receipts and commit-specific teaching tag.

One offline mapping owner5eda0cab-e29b-4b9a-969b-ad4001c0eace/source plan prepared without native writes; before255/candidate257protected settings differ on exactly four intended keys. Baseline336438bytes SHA256d0d2f13545c725e672167c18461fa7cc9384f5185dc5a4674808cc00554d3e87; candidate338790bytes SHA256b05dcd0a2ab9e01c2d961d1eca297d5bfaa62183d3835da611c516429ae97700. No setting applied or deployment requested; fresh live baseline/pins still required. Conditional card80 backed up only on website task46eaf41, mainfc2714b5 unchanged.

Exact candidate638993968 all gates succeed,60cases/no skips:25controls+1native;8boundary+1realWEB4/Mongo;25existing-admission. WEB4 artifact13605139355/4602bytes SHA256f76a18960d261d96a00aa66261eaa7bd6927c5b5e8e2ec33a5a586cee9c04c35 and admission13605863152/4381bytes SHA256b0fc86c5747837d714048d4ca0f54a8857dd1d714d3370afe8bb2a989e8410a9 verified wholly in memory. Original HTTP3/2,5/4,private0/public3/9receipts and databaseAbsent:true verified. No hosted/replica/browser claim.

One explicit commit-tagged teaching image42451953369/job128642888031 is active, Build and push; tag638993968dfd9d43467d97f42d7931892df8c549-teaching, no mutable sandbox tag or second request. Prereview verifies all7advertised pins, complete255settings unchanged and exact accepted Self baseline1fe29f38/source916/actuale2f01ef06f, including active Self/SubQuery/Dapr source settings. Guarded publisher prepared only, requires exact successful image receipt and fresh branches/pins/settings/deployment checks. No Holonym settings or Development push.

Exact image run freshly observed in_progress on the same source; job remains
in Build and push. Logs are not yet available (GitHub BlobNotFound); this is
not a terminal build failure and does not authorize redispatch. Read-only
package inspector and four-setting activation operator are prepared only.
Both Python files parse and the remote inspector passes cached Node24 syntax
checking. Code phase requires all255 baseline settings, Holonym disabled and
accepted Self/SubQuery/Dapr enabled; settings phase requires all257 candidate
settings and original Holonym owner. Installed Holonym assembly, exact-ID
reader/schema/source binding and WEB4 registration are checked alongside the
existing helpers, runtime proofs and accepted source enrollment. No operator
was invoked, settings applied, deployment requested or cohort created.

Image42451953369/job128642888031 succeeds;166013byte logs inspected in memory.
Single explicit638993968-teaching tag and exact provenance revision verified,
GHCR digestsha256:e1739b2960d20d4414c9a457c704fc161da8827472e5ee9760230911da17915d.
No image downloaded. Fresh7pin/255setting/accepted baseline checks pass;
one normal Development push638993968 acknowledged. Exact code deployment
ff89fa57-7dee-4563-b38b-14321118c433 isQUEUED. Settings unchanged, Holonym
not enrolled; inspect this exact running package before four-setting rollout.

Exact codeff89fa57 freshly observedBUILDING; retain original deployment handle, no duplicate request. All255baseline settings remain unchanged pending successful package inspection.

Codeff89fa57 SUCCESS; exact running instancea43e45c3-1744-48e9-9ca8-c2ba50b3b56f,
actual Railway digestsha256:f131765279aaf2e10f85f74848d82a9f3c805991d99ebc476f4bc55c578def86.
All255baseline settings/208critical,6helpers,12SDK/provider assemblies,
native/runtime/source proof inspected. Holonym present/disabled; accepted
Self/SubQuery/Dapr enabled. No image downloaded or source write.

First interactive publisher invocation failed during local module import,
before any settings/journal operation; corrected script import context then
applied exactly4values once with4observedACKs and no deployments. Full257
candidate settings and original owner5eda verified. One guarded activation
request acknowledged; response DPAPI retained and exact returned ID extracted,
7ba0af2c-62bb-4c75-a792-53fd953807d6 observedBUILDING on638993968. No hosted
cohort/source creation yet; next inspect this exact running settings package.

## Accepted live HTTP cohort — 8 November 2026

Activation7ba0af2c SUCCESS onf7305680-27ba-4d6e-9d19-ea68ac310bc6,
actual imagesha256:fc74e84d7d935f5160c0ae2860032b0e791770613a8af8ef6592cc6e54a43e7b.
All257settings/210critical/6helpers/12assemblies and accepted Self/SubQuery/
Dapr enrollment/runtime/source proofs inspected; Holonym enabled with original
source owner5eda0cab-e29b-4b9a-969b-ad4001c0eace, native source writes0.

Initial live verifier entry stopped before cohort creation because its explicit
settings-deployment environment value was missing; confirmed no cohort existed.
Supplied the inspected7ba0handle and reused the original acknowledged PouchDB
readiness visitor, with no replacement readiness identity. One first owner
c41676eb-ee6a-4e99-bd55-22f0860b0642 and independent visitor
6c66c794-8b02-4f24-9053-eb6ef32013ab then completed the normal client flow.
Native3/2, Mongo federation5/4, first2saves/repeat0new+2existing, exact private
replica ID sets2/2 in SQLite and Neo4j, foreign private0/public3 and foreign
replica-private0 pass. Original2version-one save ACKs and empty pending-intent
registry retained. No continuation or original save/source replay.

Retained native/client/isolation reports19731/49769/10158bytes have SHA256:
a3e9fed7cbe287ace9465a886570fb42cf46050508bc8aa5ba056b08104d40e3;
851cd9f48604800234b0589b80b41c62d771234d59177fe06a68a3bcae9f6612;
7cf3dce9fd48471814fba77381d40aad12b536f9ff4a518bbd023969efa47a0e.
Original-cohort offline audit checks exact logical/native IDs, source/schema/
chain/attester, full unsigned value arrays, ownership/version, save operation
correlation, federation/private replica equality and foreign isolation. First
composition failed at the guard's module-vs-dictionary calling contract before
its observation/write; corrected argument type and sealed from the same files,
without query/session/save replay. Durable acceptance receipt retained under
HostedQueryAcceptance-HolonymOASIS/cql-http-acceptance.acknowledged.json.

Coverage123scoped local/95liveHTTP/28browser. Browser acceptance remains open;
this does not independently verify a person, attestation consensus, source
writes, reliability or full ORM/identity/legacy SBT parity. Preserve this cohort.

Private record IDs: 23e0acb8-cd87-8695-90be-0fb1022372ef, fbfa15d5-f09c-83eb-9413-83f51aa3dabd. Original save operation IDs: 1cf17e9d-6914-235e-4a69-5ed3d97d9d5b, a4e2533c-5a42-16af-be7a-5d4d913ffc35.

Website99791ff81061922b22f870405ccd1f4eb50ed79c publishes accepted card80
via normal atomic work/cosmic-cql/main push after fresh refs/ancestry checks.
Evidence link pins parent143d5808b, runtime C# link pins638993968. Public
HTML271531bytes/SHA256c4e4012d5306b4f0baa4dbd8aa3a5ad1ad0f34a6a2b20d2a4175276e8aa110e5
matches exact Git bytes, HTTP200, card present. Existing119client controls
cover unchanged client logic; this changes HTML wording/links only. No new
browser claim, archive download, native write, cohort or source replay.

11 November UTC: the live Development CQL Playground rendered 3/2 HolonymOASIS public teaching records for its full and subset presets. Original-cohort private paired browser verification remains open.
