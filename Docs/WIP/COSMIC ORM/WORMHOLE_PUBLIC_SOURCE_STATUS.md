# Wormhole public operation source qualification

Updated2November2026. Native/CQL/local and hosted functional HTTP acceptance pass. Website card publication is next.
Current coverage54integrated/50hosted functional HTTP; browser5verified/45open.

## Verified native observations

One anonymous bounded `GET https://api.wormholescan.io/api/v1/operations?pageSize=3`
returned200with three distinct canonical emitter-chain/address/sequence tuples.
The captured catalogue was reused for one exact operation GET per tuple; all
three returned200and matched the original VAA body SHA256 fingerprints.
No account, wallet, native write, dispatch, source owner or admission was created.
Exclusive catalogue and point-read intents are consumed; do not rerun them.

VAA version1/header/signature framing/body tuple checks pass. Guardian-set
index matches the API envelope, indices are strictly increasing, duplicate
indexer observations are rejected, and sequence remains an exact uint64 string.
Body timestamp/nonce/consistency and payload size are parsed from native bytes.
Eight offline controls include malformed/truncated frames and integer limits.
This is structural binding and an indexer observation, not guardian signature,
trusted guardian-set, destination redemption or independent finality verification.
SHA256 is a retained content fingerprint, not the protocol signing digest.

## Exact next step

Implement a separate read-only `WormholePublicOperationMetadataSource` under
existing WormholeOASIS and the shared HolonManager/CQL path. Use
`WormholeOperationId` equality/membership over at most8configured tuples;
complete selection before paging, with explicit unsupported-query errors.
Configure each tuple with its original VAA body SHA256, so a reorg/body change
fails rather than silently replacing the acknowledged message. Preserve uint64
sequence/uint32 fields as strings. Bound responses, JSON, durations and catalogue;
no native-write capabilities. Create and retain one projection owner only when
implementation needs it. Existing cross-chain tracker remains unchanged.

Then add meaningful source tests, commented C#/CLI3/2, local HTTP/Mongo5/4,
privacy/reuse/replica checks, explicit query-only hosting/presets, and public card
with pinned setup/C#/HTTP links. Hosted permissions are not native capability
proof. Generic browser copies currently retain keys/name/description/provenance,
not the complete native payload. Browser interaction remains a separate gate.

Legacy tracker fixtures include a placeholder VAA `AQ==`; they only exercise
tracker mapping and cannot prove binary VAA correctness. Documented only.

## Evidence outside docs/git

`C:/Source/OASIS-cosmic/TestResults/WormholePublicSourceCurrent`:

| Artifact | Bytes | SHA256 |
|---|---|---|
| `point-0.json` | 2601 | `ba08a875c715e03f61302fcd1daf693a959eb8db40571949a6d755403ef8a4ff` |
| `point-1.json` | 3063 | `a4e156aef6143f6213e7a04193df9045d3f5506ba77a97e36aaed55ffa193a27` |
| `point-2.json` | 2601 | `29c8a0482a41729fa1958052607febe1bdbd31abfec8b0fe778956e02e53e376` |
| `point-qualification.json` | 2722 | `a2df8d9a28810f9fdbfcdd1d22030d8aeedb878ca66a1120ed9589fde1fe5a0e` |
| `point-reads.intent.json` | 281 | `0a47b054b7584e5367074c790d9ed4f7dfe5b1d812cd96427fec4c59d196409a` |
| `probe-tests.log` | 111 | `9f19866c8af7bd0425354480972b9749c259e92bd47393c4d7e76dcf44f46db3` |
| `qualification.intent.json` | 139 | `b9cf1158e5dbb77b3021fd90c65cbb252ae22f5639bbc7ad0fad99bcb3356c19` |
| `qualification.json` | 1027 | `20ceb4c0b3fab1d7e69fbb6380a0aab7e1d6eb0fd301b21eb6ba47eb2f356772` |
| `response.json` | 8284 | `f3a740e0bb10e8f500c8769e2b96132b6fa6417b2c6f14c3b458b6d5f7d941cd` |

Primary contracts: [VAA format and reorg semantics](https://wormhole.com/docs/protocol/infrastructure/vaas/),
[public API guide](https://wormhole.com/docs/products/messaging/guides/wormholescan-api/).
The native byte interpretation follows these contracts; none of their setup or
installation examples were executed. Existing Python/runtime/fixtures reused.

## Native CQL source and actual C# walkthrough complete

`WormholePublicOperationMetadataSource` compiles and35source controls pass,
zero skips. Covers equality/membership/paging, complete-selection failure before
empty pages, body pins/framing, exact integers, cancellation, unknown/inactive
source, budgets, native-write refusal and invalid catalogues. Actual executable
C# manager/deferred LINQ returns3full/2filtered. All original VAA body pins match;
no Mongo/account/dispatch/native writes created. One projection owner/config
retained under `LocalState/COSMIC/HostedWormholePublicSourceCurrent`; exclusive
console intent consumed. No rerun/new owner to reproduce green.

[Developer guide, spaced C# example and trust limits](../../Devs/WORMHOLE_PUBLIC_OPERATION_CQL_GUIDE.md).
HTTP/admission/localMongo/hosting/card remain red. Current local query types54
(3canonical/47bounded public-source/4local owned implementations), hosted49.
Next implement explicit HTTP compiler/read-profile activation and query-only
admission/presets, then local native3/2+Mongo5/4/privacy/replicas before hosting.
Original legacy tracker is unchanged; source writes remain unsupported.

| Artifact | Bytes | SHA256 |
|---|---|---|
| `TestResults/WormholePublicSourceCurrent/Offline/wormhole-source.trx` | 54109 | `113b2d31de26bfa292bfaacc528638faaefba2cf9ea69c3d3487121d2a86008e` |
| `TestResults/WormholePublicSourceCurrent/console-qualification.log` | 1807 | `333e517d53fd629ad151f473497971e44af3fb91a053b09f7262068306c5b03b` |
| `LocalState\COSMIC\HostedWormholePublicSourceCurrent\source.json` | 550 | `e358155de6cae05f61e7fb8f67d13b198948705961628cf2b149347c4e0db434` |
| `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.WormholeOASIS/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.WormholeOASIS.dll` | 44032 | `0e2a6945e8eff8cb3bc86d56435a3c51c5c3e933e9a1726baffa82c02b01154b` |
| `Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll` | 901632 | `560cfa366ed8725d3c368fee0ed2d720dbe62eb0a8475f90812fa369451cecf4` |

## HTTP/admission/local Mongo milestone complete

Core1/API3controlled tests, production WEB4 build, local native HTTP1,
publisher9pure tests, actual prepared-presets1test and105client tests pass.
Local HTTP native3/2, Mongo5/4, foreign-private0/public3; nine query receipts.
Two SDK-owned version1 Mongo copies retain the full projected metadata and pins.
This is not production signup/billing or live replication certification.
Original fixture retained at `LocalState/COSMIC/WormholeMongoHttpCurrent`;
exact marker-verified DB cosmic_cql_wormhole_http_demo removed, owned Mongo
process stopped, port27029absent. Existing data directory/runtime preserved.
Source-bound local qualification ACK is in HostedWormholePublicSourceCurrent.

First controlled API consumer build failed on stale pre-Wormhole transitive
assets after --no-restore. Ordinary cached restore fixed the dependency graph;
original compile log retained. No DLL copying or duplicate reference workaround.
Production WEB4 build itself passed. Native fixture ran once after this fix.

Protected three-key hosted candidate `HostedSourceRolloutCurrent/wormhole-public-*`
is prepared, not applied. It preserves DNA/native-write settings and adds one
free independent query-only tariff/permission plus four planned native/Mongo
presets. Native identities and body fingerprints remain unchanged. Core task
c551a89ff294e68527f6ff75128d97cff54e6c63 is pushed/remote-verified; parent
manifest/gitlink advance together. Frontend taska4e61cf adds105thclient test;
public page/card is not yet changed for Wormhole.

Next publish the verified code batch; observe its exact SUCCESS before applying
reviewed source/settings once and one existing-artifact activation. Then run one
original hosted cohort, verify native/filter/federation/reuse/privacy/replicas,
and publish the card. Hosted count remains49; no Wormhole hosted claim yet.

| Evidence | Bytes | SHA256 |
|---|---|---|
| `Http/core-admission.trx` | 3327 | `2b17e826c16578c1c96146f9f076ce47585421f8595c27db916369e2a86fbaad` |
| `Http/api-admission.trx` | 6604 | `dcde9b4877fc457d9afdee8ec93160e580f9446c6cfa8ebe6b97f74b31b0d1f0` |
| `mongo-http-native.trx` | 3371 | `bf070055470213c3edefa1a18b456d4907fdae2e6e44c38557393e8e6d744957` |
| `Http/prepared-presets.trx` | 3338 | `28ba4041d59547e3f5daddb3ffdcd14855d5696a7181104d610b9d90cd722c98` |
| `prepared-review.json` | 4854 | `f93131bd4d0a3904814e4c0e0a4dd039cdbd5de9f4a83332715c2b22407597d6` |

## Verified code publication checkpoint

ParentDevelopmentd1f42addf0413ddcd381238ccdf17bbfa561a3e2/CoreDevelopmentc551a89ff294e68527f6ff75128d97cff54e6c63 published and remote refs verified after fetched ancestry checks. Manifest/seven Dockerfiles/gitlinks validator passes. Exact Railway code job4da3a095-cdad-4052-850b-baf63020344f observed BUILDING at that parent. Prepared source settings remain unapplied; no hosted cohort or coverage grant. Publication journals HostedWormholeReleaseCurrent. Poll that exact job; after SUCCESS use matching-release gate before applying the existing candidate once. No restart/reprepare.

Website task21cc5be prepares the Wormhole card with source/trust limits and pinned C#/HTTP guide links. Draft remains unpublished until hosted acceptance; public main stayseda15be. Exact code job4da3a095-cdad-4052-850b-baf63020344f remains BUILDING; bounded build-log sample reached restore/publish without compiler errors in that sample. Raw logs not retained; sanitized progress summary in TestResults/WormholePublicSourceCurrent/build-progress.json. Keep original job/candidate/owner unchanged.

## Settings activation checkpoint

Original code4da3a095 SUCCESS atd1f42addf. Three reviewed settings applied once with matching-release gate, per-key hashes/full candidate verified, no per-key deployments. Task branch restored. Single existing-artifact activation b29f93f6-cf25-4589-ab12-a0255cf340f9 observed QUEUED (initially INITIALIZING) at same commit. Preserve consumed apply/activation intents and original source owner/config/body pins. No hosted cohort/coverage increment. Next poll this exact job; no restart/replay. This supersedes earlier unapplied-settings statements.

## Hosted acceptance complete

Exact code4da3a095/settingsb29f93f6 SUCCESS at
parentd1f42addf0413ddcd381238ccdf17bbfa561a3e2/Corec551a89ff294e68527f6ff75128d97cff54e6c63.
First original cohort passes native3/2, Mongo5/4, reuse0new/2existing,
foreign-private0/public3 and exact SQLite/Neo4j replicas2/2. Two original normal
private version1 saves/zero pending intents; no replay or additional saves.
Independent artifact audit verifies source owner/keys/body pins, projection IDs,
private provenance/name/description, versions/visibility, save-operation headers
and exact replica metadata. Immutable acceptance ACK retained in
`LocalState/COSMIC/HostedQueryAcceptance-WormholeOASIS`.
Source/cohort/config/ACKs are frozen. No guardian signature authentication,
full native-payload browser copying, independent finality, native writes or full
ORM certification. Website task21cc5be draft awaits publication after this proof.

| Evidence | Bytes | SHA256 |
|---|---|---|
| `TestResults\HostedCqlIntegrationCurrent\WormholeOASIS-native-http.json` | 17585 | `0ef2a2a686d4f4ab5f571239c282f30909074cd2044d6e350b4f7dd67bd1485a` |
| `TestResults\HostedCqlIntegrationCurrent\WormholeOASIS-client-http-flow.json` | 45858 | `dec8b498beffa02b096978304e94138d9d88254d964afd6d74e1853bdc0adf9d` |
| `TestResults\HostedCqlIntegrationCurrent\WormholeOASIS-isolation-http.json` | 7274 | `e1be6c692b1ecafd2f8e90225c076d62153316948d6aa208e5e1fefe87a92b77` |

## Website publication verified

Website 129c995e706471953eac8a3324eb71acebfd8d9f publishes the Wormhole card with guide413e79ed4 and C#d1f42addf pinned links.105client tests pass. Public HTML180501bytes/SHA25691726f231ab2d8f4c77bb655e1fb6df4263d2cbb02125097ef41593c12b9347c and both scripts match exact git blobs. Raw compact observation TestResults/WormholePublicSourceCurrent/public-assets-observation-1.json; immutable site ACK HostedWormholeReleaseCurrent. Browser interaction remains open. Source/cohort/ACKs frozen; no further hardening absent reproduced bugs. Next red scope: Chainflip public qualification.

11 November UTC: after Railway reported SQL Server revision 400b42ef36 successful (deployment 6d207df7), the live WEB4 Wormhole full public preset returned HTTP 503 CQL_PROVIDER_UNAVAILABLE. The browser still held its previous catalogue. The deployment log tail did not identify a Wormhole cause. Subset and private checks were not run. Keep historical HTTP and replica evidence. Confirm fresh readiness after warmup before another public query.

11 November UTC: after refreshing the live Development WEB4 catalogue to 106 available hosted sources, Wormhole pinned-message public presets returned 3 full and 2 filtered rows. The earlier post-cutover HTTP 503 remains recorded. Original-owner paired browser verification remains open.
