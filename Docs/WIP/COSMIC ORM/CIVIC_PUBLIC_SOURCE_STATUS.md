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
