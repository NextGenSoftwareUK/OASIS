# Celestia public CQL source — interface review

2 November 2026. Native protocol and local CQL/CLI qualified; hosting remains red/unadmitted.
One local source owner/config retained; no hosted admission/card/browser proof.
Provider totals now56integrated/51hosted after source36controls/actual CLI3/2.

## Current source and contract

Existing `Providers/Network/NextGenSoftware.OASIS.API.Providers.CelestiaOASIS/CelestiaOASIS.cs`
uses a default localhost bridge at port26659, optional bearer authentication,
and activation through GET `/header/local`. It submits/retrieves DA blobs.
This is not an existing anonymous hosted query source. Legacy code is unchanged;
its broader blob-storage correctness is outside this read-profile milestone.

[Official Mainnet Beta documentation](https://docs.celestia.org/operate/networks/mainnet-beta/)
now documents `https://public-endpoint.celestia-mainnet.quiknode.pro` for
HTTP RPC/REST and DA JSON-RPC development/testing. It explicitly provides no
production SLA. The interface review does not prove live availability.
[Official node API design](https://github.com/celestiaorg/celestia-node/blob/main/docs/adr/adr-009-public-api.md)
distinguishes extended-header reads by height from blob retrieval and sampling.

## Proposed next red scope

After the current Chainflip release gate, qualify a bounded anonymous
extended-header query over three explicit mainnet heights, then a filtered pair.
First verify the current documented RPC method, network identifier, exact integer
height, header identity and response bounds using the shipping guarded transport.
Record an exclusive qualification intent before any native requests and retain
raw responses only under the worktree TestResults folder. Do not use paid keys,
submit blobs, run a node or change endpoints automatically after failure.

Only after native qualification: implement a separate read-only CQL source with
explicit height equality/membership, bounded complete selection before paging,
consistent identity/provenance, cancellation and request/response budgets. Include
controlled malformed/network/unknown-height failures, an actual spaced/commented
C# walkthrough, HTTP admission and one Mongo federation preset. Prove the original
cohort, privacy, reuse and exact replicas before a public card.

A returned extended header does not by itself prove independently verified
consensus, blob inclusion, availability sampling or successful blob writes. Any
such guarantees need their own implementation and proving tests. No new runtime,
download, environment, test data or account was introduced by this review.


## Native protocol milestone verified

Canonical guarded .NET transport, no account or node installation: one
header.NetworkHead and three header.GetByHeight requests all returned200.
Frozen mainnet heights: 15521793, 15521794, 15521795. Returned chain ID celestia,
canonical uint64 height, commit-height agreement, bounded reported block/data
hashes and timestamp validated. One native test passes; five controlled cases
cover valid input, wrong network, noncanonical/zero height and commit mismatch.

Methods confirmed against [endpoint API GetByHeight](https://www.quicknode.com/docs/celestia/header-getByHeight)
and [NetworkHead](https://www.quicknode.com/docs/celestia/header-networkHead).
Reported block identity is structurally checked; header hash/validator signatures
and data availability have not been independently verified. The exact original
qualification intent/outcome/selected heights are retained under
LocalState/COSMIC/CelestiaPublicQualificationCurrent. Do not replay the consumed
native test or silently choose newer heights. CQL activation/query tests will
be separate explicitly planned reads over this original catalogue.

Proving test: Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.CelestiaOASIS.UnitTests/CelestiaPublicNativeQualificationTests.cs.
Next implement a separate bounded read-only CQL header source, with explicit
configuration/capabilities, complete selection before paging, error propagation,
controlled tests and a spaced/commented executable C# walkthrough. Legacy blob
code unchanged; no native writes, federation or hosting claim yet.

| Evidence | Bytes | SHA256 |
|---|---|---|
| `LocalState/COSMIC/CelestiaPublicQualificationCurrent/qualification.intent.json` | 282 | `9d02d3f5f211afafcacbb46d0c8ad58d6abd09435c5b967f95dde1d920cb8682` |
| `LocalState/COSMIC/CelestiaPublicQualificationCurrent/qualification.observed.json` | 782 | `63ae4ef4746247a8ecead84ee7804f095bdfe058825985c2624ef69335ec3c8e` |
| `LocalState/COSMIC/CelestiaPublicQualificationCurrent/selected-heights.json` | 67 | `e26e34d9b92d7ef5d38719cd98d839b3ed00b05a21a892ddf51d10ab2673db11` |
| `TestResults/CelestiaPublicSourceCurrent/qualification-contract.trx` | 9251 | `56a53a01b2292044d46d6def31925f77c4a4b98af86aa4dd52b76d6138a43f46` |
| `TestResults/CelestiaPublicSourceCurrent/native-qualification.trx` | 2790 | `084100fa8d3874c517658f600e50e8c97a2387f44d0ed36aff578920b1621a88` |
| `TestResults/CelestiaPublicSourceCurrent/rpc-1.json` | 47264 | `f034d54d9306805053618f9ee67d6f943ccdfc68bb01438aa5ea61fbc7110e08` |
| `TestResults/CelestiaPublicSourceCurrent/rpc-2.json` | 54729 | `dab7b445316d3b8665a7b3e5d456cd9b89e0533219c8ef3cd34116764d2675f4` |
| `TestResults/CelestiaPublicSourceCurrent/rpc-3.json` | 43465 | `35bd984338b594233637405cde60c2d97471a84eb9a25ef4e0043c99ae97283d` |
| `TestResults/CelestiaPublicSourceCurrent/rpc-4.json` | 41765 | `5dcbaa0bf1a51fd86e4ec3523460e9e7cfb5aec81adeae771cb04ab8885ca444` |


## Local CQL/C# milestone verified

CelestiaPublicHeaderMetadataSource implements shared CQL/read-only activation
contracts over an explicit height-to-reported-block-ID catalogue.36controlled
cases pass: equality/membership/paging, uint64max precision, malformed/wrong-network/
wrong-height/pin/RPC/duplicate/oversized responses, cancellation, budgets, unknown
keys/inactive source, invalid config and native-write refusal. Complete selection
precedes paging, with no partial results after late failure.

Actual spaced/commented C# walkthrough returns3full/2filtered through HolonManager
and deferred LINQ. Original source/config/pins/owner/intent/ACK retained under
LocalState/COSMIC/HostedCelestiaPublicSourceCurrent. Never replace the owner/pins
or replay the consumed console qualification. Build initially found a wrong
launcher return statement; corrected to the program integer exit contract, initial
log retained. Corrected build passes/0errors; no legacy code changes.

Next HTTP compiler/explicit read activation/query-only admission/four prepared
presets and local native3/2+Mongo5/4/privacy. No hosted/CQL write/independent
consensus/DA-sampling claim. Source56integrated/49public profiles; hosted stays51.
[Configuration, C# usage and metadata guide](../../Devs/CELESTIA_PUBLIC_HEADER_CQL_GUIDE.md).

| Artifact | Bytes | SHA256 |
|---|---|---|
| `TestResults/CelestiaPublicSourceCurrent/source-controls.trx` | 56682 | `679b2767f2852049c6b92926d844fe6f8bb18b4fd70477480c91ed1099dcfb9f` |
| `TestResults/CelestiaPublicSourceCurrent/console-qualification.log` | 1579 | `a8805aad0989f76a821df12806e96eb57897346799edad6812ad25f5f715b97d` |
| `LocalState/COSMIC/HostedCelestiaPublicSourceCurrent/source.json` | 398 | `d30c915410b522ad352a8540a25d82013ebae050caa1edb4a77ba3b5017d830c` |
| `Providers/Network/NextGenSoftware.OASIS.API.Providers.CelestiaOASIS/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.CelestiaOASIS.dll` | 79872 | `c9b8828b1ba9dd05c6cc95c83fd0780017e2fd19447b09fc79ce30e294aec0cf` |
| `Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll` | 911872 | `081a0b13f9d678edf37b1181bac7a7b8faad5866eb1223b17cdb21ef0cf9648a` |


## Local HTTP/Mongo milestone verified

Shipping WEB4 build0errors. Core query-only tariff1/API admission3/publisher9/
actual prepared presets1/client107 controls pass. Local native HTTP1test passes:
3/2source rows,5/4Mongo pairs, foreign-private0/public3,9query receipts. Two
SDK-owned version1 private copies seeded through HolonManager; controlled bearer/
billing fixtures are not production billing or replication certification.
Exact owned DB removed after marker/count checks, Mongo stopped and27029absent.
Original fixture intent/save ACKs/cleanup retained at LocalState/COSMIC/CelestiaMongoHttpCurrent.
No replay/resave/owner/pin replacement.

Core task7edffc773e76ab565f204ec34e87520e020910e0 pushed/remote verified; parent
gitlink and deployment manifest advanced together. Three-key candidate prepared
once under HostedSourceRolloutCurrent/celestia-public-*; full review and four
presets retained below. No live settings applied/deployment requested yet. Next
commit/integrate exact batch, observe code SUCCESS, then apply/activate once,
original hosted acceptance and only then public card. Hosted count stays51.

| Evidence | Bytes | SHA256 |
|---|---|---|
| `TestResults/CelestiaPublicSourceCurrent/mongo-http-native.trx` | 3371 | `f688214277985d89d3e0d6b34282a399ca097bae40570bec73066a22b7273834` |
| `TestResults/CelestiaPublicSourceCurrent/celestia-mongo-native-summary.json` | 229 | `10d5efd2067ab828b805e773c2c266a94de1302f07a2c70523e4fbb172adfc84` |
| `TestResults/CelestiaPublicSourceCurrent/Http/core-tariff.trx` | 3315 | `b518105159834f98e9a866521a339d44b39c37c12c763016fd7ad8f14446dad6` |
| `TestResults/CelestiaPublicSourceCurrent/Http/api-admission.trx` | 6604 | `5d8e75ffb93535a5d05483d7aa6550d98f67970dc02c700736d5b1345ddc01fb` |
| `TestResults/CelestiaPublicSourceCurrent/Http/prepared-presets.trx` | 3338 | `a365d25b9d072cced303407a6ec05aa80daba68e524d011f1da074fd8493639e` |
| `TestResults/CelestiaPublicSourceCurrent/prepared-review.json` | 4023 | `83638bddfd5f485622ec9c4e2ce8241ad6a9307d5753e5831c21a2c8a96fa2ca` |
| `TestResults/CelestiaPublicSourceCurrent/client-all-tests.log` | 10369 | `fd6e9006e1f82528635a8befaac89cc2fe4a32fb6e8ffdcdd0a99de455520d9e` |

Development publication: parent9c0b5c591f6ecc13032d4c2fa51d9c2fc469750b and
Core7edffc773e76ab565f204ec34e87520e020910e0 remote refs verified after ordinary
fast-forward pushes. Manifest/gitlinks gate passes. Exact code job
9af995a6-38c7-4c63-9059-c2bfafe4a90e last observed QUEUED. Three-key candidate
remains unapplied; follow same job, no restart. Durable release intent/observation
under LocalState/COSMIC/HostedCelestiaReleaseCurrent.

Code9af995a6 SUCCESS at exact9c0b5c591. Applied three reviewed values once
with skip-deploys; full candidate and each key hash observed exactly. Activation
fa9ccb7b-3384-4c77-80f1-309e07fab649 last verified BUILDING at same commit.
Follow this existing job; no reapply/redeploy/reprepare. Original hosted cohort
has not started. Durable success/application/activation ACKs and intents retained
under HostedCelestiaReleaseCurrent/HostedSourceRolloutCurrent.


## Hosted HTTP acceptance complete

Exact code9af995a6/settingsfa9ccb7b SUCCESS at parent9c0b5c591/Core7edffc7.
Three settings applied once with exact full candidate/per-key hashes observed.
Original hosted cohort native3/2+Mongo5/4, reuse0new/2existing, foreign-private0/
public3 and exact SQLite/Neo4j2/2 pass. Two original normal API version1 saves/
matching operation ACKs, pending0. Independent audit checks original header
block/data/previous hashes, timestamp/square width, source owner/keys/provenance,
private-copy identities/visibility/content and exact replica metadata. Immutable
acceptance ACK retained under HostedQueryAcceptance-CelestiaOASIS; no replay.

Hosted functional proofs/query tariffs52; browser5verified/47open. Public card
next. Not independent consensus, DA sampling, full payload copying, native blob
writes, complete ORM support or universal HyperDrive reliability proof.

| Evidence | Bytes | SHA256 |
|---|---|---|
| `TestResults\HostedCqlIntegrationCurrent\CelestiaOASIS-native-http.json` | 14713 | `b98a54a8b161342d465e3d2c0bbe676539a235caf8a2e5ac01f9233ac1bbcf11` |
| `TestResults\HostedCqlIntegrationCurrent\CelestiaOASIS-client-http-flow.json` | 38406 | `fc4ce45717556a9f4d543c8918ca7c92c902248d0ee0a45091d4d6b766d074e5` |
| `TestResults\HostedCqlIntegrationCurrent\CelestiaOASIS-isolation-http.json` | 5985 | `a855616d42e24fa195d5857266cfb73b06181adcc834ef827d011e925ca8361f` |


## Public sandbox publication verified

Website task/main6a863c74a804a832a9f42088f9055c750f6d7d8f verified remotely.
Celestia card has native/filter/Mongo instructions, precise proof limits and
pinned guide/C#links. HTML parses/IDs unique;107client controls pass, no new JS
change. Three anonymous public GETs match exact git blobs. Observation:
TestResults/CelestiaPublicSourceCurrent/public-assets-observation-1.json; immutable
site ACK under HostedCelestiaReleaseCurrent. Browser interaction remains open;
original source/catalogue/cohort/save ACKs frozen. Hosted52/browser5/open47.

| Public asset | Bytes | SHA256 |
|---|---|---|
| `web4-sandbox.html` | 185401 | `300ae8ead82af8ff5dddc775fdcd1fcaae5f14d3a9b74a55ea274d15bf065ba5` |
| `cql-demo-preparation.js` | 10241 | `c1006abf6074616428f93e83e13774ba8a6198f35d8ed097d84323466a8b309a` |
| `cql-playground.js` | 21871 | `7cb6210b9adda1e83630fc966926772f8338c43163c24d1abd88c626ad1ad649` |
