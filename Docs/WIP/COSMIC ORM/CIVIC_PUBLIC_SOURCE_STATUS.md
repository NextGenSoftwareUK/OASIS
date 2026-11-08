# Civic public source qualification

8 November 2026. Review only; no native query, CQL acceptance or admission.

The existing CivicOASIS adapter uses real Nethereum calls to GatewayToken:
verifyToken, getTokenIdsByOwnerAndNetwork and getToken. Preserve that adapter
while qualifying a bounded public query representation. No personal pass or
wallet catalogue has been selected, scanned or imported.

Primary Gateway Protocol source is pinned to
[440c564b6b5c82a202f15ce064062e0a152d6505](https://github.com/identity-com/gateway-protocol-evm/tree/440c564b6b5c82a202f15ce064062e0a152d6505).
The complete, untruncated tree was inspected in memory: 381762 bytes,
SHA256 afb682f0d2bbb7b6868159d33e57fc9c6ac72227589309806d4fd67c02b5bed5.
No clone, package, runtime, database or local build was created.

The pinned SDK constants identify GatewayToken proxy
0xF65b6396dF6B7e2D8a6270E3AB6c7BB08BAEF22E, matching the existing adapter.
This is a source-address match, not evidence of deployed bytecode or ABI.
The separate IGatewayNetwork interface exposes getNetwork(uint256),
getNetworkId(bytes32) and doesNetworkExist(uint256). Its network structure
uses a bytes32 name and includes authority and configuration fields.
The smart-contract README's newer deployment table is explicitly BNB testnet;
do not assume its GatewayNetwork address or ABI applies to the existing
mainnet GatewayToken proxy.

Next gate: establish one exact deployment/ABI/network identity and a small
reviewed public catalogue before implementing its CQL read contract. A
registry configuration representation must retain its native scope and must
not be described as verification of a person. No source writes, wallet,
credentials, owner decision or hosting change is required for this review.

Compact tree and selected contract/hash receipts are retained under
LocalState/COSMIC/RecoveryLaptopResume20261107/civic-gateway-*.json.
Native, manager, HTTP, replication and browser gates remain open.

## Native network catalogue and CQL preparation

Pinned Polygon deployment files independently identify chain137, the existing
GatewayToken proxy and its original implementation. Proxy deployment
block41896100/transaction728e2c1631fb7580e5f80c6d5481b2aec63bd3abf76af557a6d13d6bcbffe92f;
original implementation65Bb8eb7c66c107e379C1C5781268393303E4b50.
The deployment ABI specifies getNetwork(uint256) returning a string, unlike
the newer separate GatewayNetwork tuple interface. Files3/11372/401785bytes
were inspected/hash recorded in memory; no source archive retained.

One anonymous batch returned HTTP200/9697bytes: reported chain0x89, genesis
0xa9c28ce2141b56c474f1dc504bee9b01eb1bd7d1a507580d5519d4437a97de1b,
finalized block0x5c7349f/hash
0x63e481ab0ed2431ffa2c986b662b35480f69d46d43e847ea54b8cfd7b9dbf7af,
and nonempty680byte proxy code. Genesis matches the already accepted Polygon
source pin; this does not assert independent consensus verification.

One subsequent canonical-hash-bound batch returned HTTP200/993bytes.
Network1=tgnuXXNMDLK8dy7Xm1TdeGyc95MDym4bvAQCwcW21Bf;
network4=ignREusXmGrscGNUesoU9mxfds9AiYTezUKex2PsZV6;
network10=uniqobk8oGh4XBLMqM68K8M2zNu3CdYX7q5go7whQiv.
All three strict ABI strings preserve offset/length/padding and exact bytes.
The proxy implementation slot reportscd868b491d98a3e8543c590186a5eaa923bc40f7,
which differs from the original deployment record: do not claim unchanged
implementation provenance or substitute the original implementation address.
Raw/status/hash persisted before parsing; no retry, redirect, proxy or write.
No personal pass, wallet or user identity queried.

CivicNetworkQuerySource now prepares an explicit immutable1/4/10catalogue,
canonical block/chain/code checks, exact native names and portable provenance.
Shared ReviewedEvmReadClient adds a bounded canonical ABI-string reader with
strict UTF-8, safe length/offset checks and exact zero padding/trailing bounds.
Unknown/mixed IDs preflight before any native dispatch; scans and writes
remain unsupported with explicit errors. Existing pass-verification adapter
is unchanged. New cloud workflow requires27controlled cases (25new+2legacy),
all shared-reader regressions and1actual native CQL3/2case, no skips. Source
and project XML reviewed; compilation/acceptance still pending. No coverage
count increase, WEB4 admission, source owner, settings or hosted cohort.

Tasksourcee8fdfc00fd379e06f4efb99fe316e3c94b37b5c8 pushed on the original task branch; cloud42452950450/job128645866052 observed live. No manual duplicate dispatch; source remains unaccepted until controlled/shared/native gates pass.

## First native failure and source-contract correction

Cloud42452950450 compiles, passes all27controlled and1296shared cases, but
native activation fails at historical eth_getCode; no CQL rows accepted.
A separate bounded protocol diagnostic retained HTTP200/474bytes;
byte count/hash are in civic-polygon-code-contract-diagnostic.observed.json.
The original hash-bound historical code/call replies report unavailable
historical state; numeric archive access explicitly requires a personal token.
No further requests for that archived state or alternate endpoint are allowed.

Root invariant: the anonymous endpoint supports current state; freezing an
old state into a durable demo requires archive access it does not grant.
The single Civic source contract now reads each record at its RPC-reported
current finalized block, verifies its canonical header, and binds code and
getNetwork to that exact hash with requireCanonical. Block number/hash are
retained per row; there is no shared-block, frozen-snapshot, archive, identity
verification or independent-consensus claim. Stable source IDs bind owner,
provider and native network ID, with no block in the identity namespace.
Expected network names remain explicitly reviewed and must match. This is
one code path, not a retry or fallback. Controlled identity/ABI failures remain
mandatory; six calls per observation, whole-catalogue preflight/no partial rows.

WEB4 explicit owner registration, read compiler allowlist,8boundary cases and
real shared Mongo/HTTP fixture are prepared. Core query-only read-tariff entry
46ac61c2abb80c4e3e3c6431f940a758691dcb78 pushed on the original Core task branch;
parent gitlink and manifest advance together. Source correction and WEB4
qualification must pass before any local count or hosted enrollment increase.

Corrected tasksource4a56a62a279045e92248f74c1e507279112ece3f/Core46ac61c manifest/gitlinks pass. Cloud native42453281381 and WEB442453281168 observedqueued, no manual duplicate dispatch or accepted source change.

Corrected cloud42453281381/job128646847144 succeeds:27controlled+1296shared+
1actual native CQL3/2, zero skips. Artifact13606606841/308395bytes SHA256
67add0e29740f0267215b3917abe27ec942883f840e6a709b9e5b4844dc76101 and all
three TRX hashes/counters verified entirely in memory; no ZIP/TRX retained.
The original archive failure is preserved, not retried or re-labelled green.
WEB442453281168/job128646846608 is live at controlled consumer compilation.

Fixed-catalogue planner now binds only CQL_CIVIC_NETWORK_QUERY_SOURCE_OWNER,
namespacecivic/polygon/network and native IDs1/4/10.39candidate controls and
5publisher guards pass with self-cleaning fixtures; simulated test stdout
is not a live setting update. An initial publisher filename pattern matched
zero tests; corrected to test_hosted_fixed_public_catalogue.py, all5executed.
Fixture actor a5808c3e-7031-47e7-92de-48a659568f1b expected logical IDs
fb19f71f-ed17-8a0c-b2dc-b0f5074a6888,863752f8-cc68-8ebe-bf88-4b95f8921590,
3e9729d9-0057-82ea-8335-a83ad0fa8f8d; same assertions added to the existing
C#catalogue test for next cloud qualification. No real source actor/snapshot,
admission, Development image or hosted cohort prepared for Civic yet.

WEB442453281168/job128646846608 succeeds:8boundary+1actual HTTP/Mongo,
zero skips. Artifact13605839734/4574bytes SHA256
52a721880e6880d708c501d44054a50d5ec687d8d1ebc48f2bc00b7ebbd6f6b3,
TRX counters/hashes/summary verified wholly in memory: native3/2, paired5/4,
foreignprivate0/public3,2SDKsaves/9receipts; ownedDBabsent and ephemeral
container removed. Coverage123scoped local/94liveHTTP/28browser. No hosted,
replication, billing or browser acceptance for Civic.

Scoped candidate88c378c1e08ee270365b760c9d3abc29b629a0f4/Core81862b2450c1d4bd0f3e8853523c1b13b8a257d3
is prepared from Holonym638993968/Corebe4636a;15scoped paths. Shared client
change is asserted to be only the bounded ABI-string reader plus its import;
all earlier shared behavior remains byte-identical. CQL registration/compiler/
project edits are constructed from the scoped base, not copied wholesale
from the broader task tree. Source reader blob matches native-qualified4a56;
manifest/gitlinks pass. Temporary indices removed in finally; no checkout,
worktree, runtime or database. Neither candidate ref is changed or pushed;
exact qualification/publication waits for accepted Holonym rollout.

After Holonym accepted/sealed and public page byte verification, existing
candidate refs advanced normally to88c378c1e/Core81862b2 after fresh base/
ancestry/manifest checks. Original task branches/worktrees unchanged; no
Development push/settings/deployment. Exact source-qualified native
42455252759, WEB442455252768 and existing-source admission42455252725 are
live. The admission workflow's actual candidate path is
.github/workflows/qualify-cross-chain-five.yml, displaying the six-reader name
and enforcing25cases/artifact six-public-readers-candidate-admission. All are
original push runs; no duplicate/manual dispatch.

One offline mapping owner73eb9868-650e-4fd5-9398-dc7e794861fb and descriptor/
review prepared; public IDs772e4306-fd30-817c-a8b0-e7bdbbdd564e,
0311a43f-3763-8e76-ba92-ea28a26571b6,38f0dade-a30f-8370-a721-bb6df22e013f.
Before257/candidate259protected settings differ on exactly4intended keys,
using accepted Holonym baseline. Baseline338790bytes SHA256
b05dcd0a2ab9e01c2d961d1eca297d5bfaa62183d3835da611c516429ae97700;
candidate341110bytes SHA256b2ed4ddb4c7a42cd731f77803f8280f028768f529d7b798a506925b2847f661e.
First composition assumed optional publicIds; second comparison used sorted
review key ordering inside compact preset strings. Reconciled original
source/snapshots using exact fixed-preset schema and fresh verifier ordering;
no actor regeneration, snapshot replacement, native request or settings apply.
No public source writes; fresh live baseline comparison still required.

Exact candidate88c378c1e native42455252759/job128652766765 succeeds:27controlled+1296shared+1native, zero skips. Artifact13607421696/308268bytes SHA25645cf16e1bffd8cbdbb29016d0c1675f54e3fe5ac8ae92dd0dae40e8a29d78e18 and all3TRX hashes/counters verified entirely in memory. WEB442455252768/job128652766868 and admission42455252725/job128652766840 freshly observedlive; no image request until both pass.

Exact candidate88c378c1e all1358cases succeed/no skips:27controls+1296shared+
1native,8boundary+1realWEB4/Mongo,25existing-source admission. WEB4 artifact
13607104522/4584bytes SHA25612318c0386090341302553ffa98332ef440fb6281370b4cf970985c8d73d720f;
admission13607164406/4434bytes SHA256cd32743783e0d73613ae5528dc34517403f3ac4b645d571d5955e2a71dbd7a1f.
Reports/counters/privacy/9receipts/ownedDBabsence verified wholly in memory.
One commit-tagged teaching image request acknowledged204; no Railway request,
Development push or settings change. Preserve original dispatch journals.

Exact teaching image42455727615/job128654204988 observedlive in Build and push; tag88c378c1e08ee270365b760c9d3abc29b629a0f4-teaching. Read-only prereview verifies all7advertised pins, complete257settings unchanged, accepted7ba0Holonym baseline/actualfc74e84d, with Holonym/Self/SubQuery/Dapr preserved. No Civic source enrollment, Development push, Railway request or native source write; image success/provenance and fresh integration checks remain required.

Guarded Development publisher and13assembly package inspector/four-setting activation operator prepared only. Python AST and cached Node24 remote syntax pass. Code phase must retain accepted Holonym/Self/SubQuery/Dapr enabled while Civic disabled; settings phase checks original Civic owner73eb and259settings, with genesis/contract/namespace/ABI-string method bindings. No operator invoked; exact successful image proof remains required. Client snapshot logic retains its first observation when live blocks advance; future cohort audit must preserve that contract rather than compare old snapshot names against a newer native block.

Website task9751d1e backs up conditional card81 with candidate88 C#link and immutable scope guide. Public main99791ff8 remains accepted Holonym card80; no Civic availability claim or browser acceptance. Exact Civic image42455727615/job128654204988 still observedBuild and push; no redispatch.

Exact image42455727615/job128654204988 completesSUCCESS.175769byte logs
inspected in memory; single explicit88c378c1e-teaching tag, exact provenance
revision and teachingsidecars:true verified. GHCR digest
sha256:06d39f6b61909c6bf93d74b6ccac9c751395578bd7245cc0c7a4fb8da39b2033;
this is not the future Railway package digest. No image downloaded.

Fresh7pin/257setting/shared refs/accepted7ba0baseline checks pass; one normal
Development push88c378c1e acknowledged, settings unchanged. Exact code
50095dbf-e740-4847-8a27-2f8de45db1f1 isQUEUED. Original protected after-push
observation/compact handle retained; no source enrollment, activation or
hosted cohort. Inspect this exact running package before four-setting rollout.
This is the second8November Development push in configured America/Cancun;
Self was7November23:59:58, Holonym8November00:57:44 per authoritative reflog.

Exact code50095dbf freshly observedBUILDING. Offline original-cohort audit prepared and syntax parsed only; validates per-row network/chain/genesis/contract/block/code provenance and first-observation snapshot names without requiring them to match newer public blocks. No sessions, native requests or saves started for Civic acceptance.

Code50095dbf SUCCESS on95f713d5-5a7d-492b-abbd-8ad0b06dff04; actual Railway
digestsha256:43a3c9cd47af096211d17d2ae0595b2f945dec73f81374668258cf27097be286.
All257settings/210critical/6helpers/13SDK/provider assemblies and native/
runtime/source proofs inspected; Civic reader present/disabled, accepted
Holonym/Self/SubQuery/Dapr enabled. No local image download/source write.

Exactly4reviewed settings applied once without deployment;4exact ACKs and
complete259candidate verified. One guarded activation request acknowledged;
original protected response returned51ca9b77-38e5-4b20-9b26-33b932c8594c,
freshly observedBUILDING on88c378c1e. Preserve original owner73eb/source plan;
no hosted cohort, activation replay or native source write. Next: inspect
this exact running settings package before one first HTTP acceptance flow.

## Accepted live HTTP cohort — 8 November 2026

Activation51ca9b77 SUCCESS on613e1281-4132-4446-a194-8100d332adec,
actual imagesha256:73c657c1da04756f49f7528cd3f66e50b39da56c0544d03809ddbbd92955f03a.
All259settings/212critical/6helpers/13assemblies and native/runtime/source
proofs inspected; Civic enabled, accepted Holonym/Self/SubQuery/Dapr preserved.
Original source owner73eb9868-650e-4fd5-9398-dc7e794861fb retained; source writes0.

One first normal-client cohort owner18e405bd-89fe-4746-9934-998025d1f525 and
foreign visitorc9535896-cd13-41af-a3e8-8cb6e55e4397 complete native3/2,
Mongo federation5/4, initial2saves/repeat0new+2existing, matching private
SQLite/Neo4j ID sets2/2, foreign private0/public3 and replica-private0. Existing
PouchDB readiness visitor reused with its original credential journal; no new
readiness actor. Original2version-one save ACKs/pending0 retained. No resumed
flow, continuation, source write or accepted query/save replay.

Offline audit seals exact logical/native IDs, network names, chain/genesis/
contract, per-row reported canonical block/code provenance, ownership/version,
first-observation snapshots, correlated save operations, federation/replica
projection equality and independent privacy. Native blocks may advance between
reads; private snapshots keep their first observation rather than being updated
or compared against newer public names. Durable receipt is in
HostedQueryAcceptance-CivicOASIS/cql-http-acceptance.acknowledged.json.

CivicOASIS-client-http-flow.json: 36429bytes/SHA256d8a791c9b505ac0b0625241dd4ae7cd417cc31913140793be569fd3dab0bd96b.

CivicOASIS-isolation-http.json: 6296bytes/SHA256edd79c032ca314fcd65c3c9f65643e425ec0a6aad31edd17eb5ec58c227a0172.

CivicOASIS-native-http.json: 13223bytes/SHA256cd02313824aa10cb76db104c0f059ef6bd6af5670a349eaad8013c2ed4699884.

Private record IDs bf051f2f-8411-84bb-9aaa-d5457d28130c, 71984641-5668-89ce-9b5f-6e63eb413b0b; original save operations 6b1fcc8f-386e-e72e-8a48-3b5b92aee453, b175c228-1f71-049c-ba92-2fe593f3a456.

Coverage124scoped local/96liveHTTP/28browser. Website card81 publication next; browser remains open. Registry configuration is not personal verification, pass issuance, archival-state access, independent consensus or full ORM/identity certification. Preserve this accepted cohort.

8 November: accepted sandbox card81 published from website commit fa0f848b2bbb7418fdbe2fca4f0e0a45367ec3bd through normal atomic original-task/main push. Public HTML fetched once:272986bytes,SHA25629a42daa76bb55bb48e6f85569274a582cce33dacd086fae74c9535e5ac94745; exact committed bytes and immutable sealed evidence843620f4978e1a8854db7b1809bb06508def43e3 match. Browser acceptance remains open; no accepted cohort replay.
