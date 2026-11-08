# COSMIC ORM / CQL current status

Updated 8 November 2026. Goal active and incomplete. Demo breadth comes first;
full native provider implementation and EF/NHibernate parity remain on the
roadmap. Imported, virtual/pointer and hybrid holon modes remain in that roadmap;
this checkpoint does not claim completion of those designs.

## User priority order updated 8 November 2026

Solana → Ethereum → Polygon → Arbitrum → NEAR → EOS/EOSIO → SOLID → IPFS →
ActivityPub → Cosmos → Polkadot → TON → TRON → Hedera/Hashgraph, then remaining
providers in popularity order. This matches the latest user-edited goal. OLID
means SOLID; ActibityPub means ActivityPub; HetraGraph means Hedera/Hashgraph
(HashgraphOASIS).

Solana, Ethereum, Polygon, Arbitrum, NEAR, Cosmos and Polkadot retain accepted
scoped live HTTP and browser evidence. IPFS and ActivityPub also retain live HTTP
and browser proofs. SOLID, TON, Hedera and EOS retain accepted live HTTP evidence;
their browser gates remain open. TRON has no accepted current live CQL admission: preserve rate-limit/denial
outcomes and the authenticated-reader draft/credential preflight; do not retry
failed unauthenticated endpoints or recreate its original cohort. Use the single
owner-action list for any missing account/access requirement, preserving existing
entries and deferrals. Do not rerun accepted native sources or resave copies just
to change queue order.

Focus engineering/demo usability on this chain order before adding further
providers. Existing MapLibre and four-provider plans/owners/settings remain
prepared and unchanged behind the chain priorities; no unverified rollout or
third8November Development push. GOMap WEB4 run42480693015 has completed success;
close out its already-running artifact/cleanup evidence without starting another
GOMap feature ahead of this queue. Remaining-provider popularity ranking must use
current adoption/use evidence when that queue is resumed, with dependencies and
owner deferrals explicit.

## Immediate work

Full WEB4 image source **8668fd2d040da71ba27e27e49fbed6792eb4e094** built and
published successfully in42475406236. Exact teaching tag/digest:
**sha256:dcd1e9f67e9e44cfb721b23c47c26ad0f79cf490472a648cd32f7d8e5cec0496**.
Combined cloud qualifier **42478524577** (checker sourceb34b55e103) PASSED:
exact package/source/helper/model hashes, six assemblies, genuine WEB4 API health,
OpenVINO metadata, graceful exit0, no OOM and complete owned container/network/
anonymous database-volume removal. Artifact13616282018/4834bytes
SHA256dcd008fe15813240031a2ca4e316f35b2e5b25d6ffae19861e778da2368a703f
verified entirely in memory. No local image/runtime/model/database download.

Waku/NATS immutable producer publication42474138782 and exact consumer42474992596
passed; seven pinned dependency checkouts42472240791 passed. Earlier image and
checker failures are retained in [OpenVINO evidence](OPENVINO_CQL_MVP.md).
API startup proof is task-image qualification, not live CQL acceptance or approval
to deploy unrelated task-branch changes. Exact scoped-candidate tests/image and
live cohort acceptance still remain. The chain priority queue above now takes
precedence over additional provider rollouts; existing prepared batches are retained.

[Full229-project status report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) separates
SDK/native evidence, CQL/COSMIC depth, live/browser and HyperDrive scope. Latest
reconciled baseline has93query-only+3canonical=96accepted/enrolled query types;
129scoped integration checkpoint and28browser flows remain the recorded totals.
No fresh all-provider availability sweep is claimed.

## Live Development baseline and integration cadence

Live accepted baseline remains Civic source
**88c378c1e08ee270365b760c9d3abc29b629a0f4**, activation
**51ca9b77-38e5-4b20-9b26-33b932c8594c**,259settings/212critical/6helpers/13assemblies.
Original cohort18e405bd is sealed: native3/2,federation5/4,reuse0new2existing,
private0/public3,replicas2/2,2saveACKs/pending0. Earlier accepted Holonym, Self,
SubQuery, Dapr and all other sources/cohorts remain preserved; do not recreate them.
LocalFile is accepted; do not redo that resolved gate.

Two Development integrations already occurred8November. No third8November push;
next permitted integration window is9November America/Cancun. Never promote master.

MapLibre candidate **23532a6fc9146d5535152c182e61ec21d8889d6e** / Core
**681a1d0b2d4aa8f3b2751a1fad8857a1dd5f86e0** has62exact checks and image
qualification passed. Its original source owner77afeb39-df6d-4df4-8686-5ff43bd01cbb,
protected baseline/candidate and guarded rollout operators already exist.
Do not regenerate them. Next: permitted scoped integration, exact deployed package,
once-only settings/activation and first owned HTTP/reuse/privacy/replica seal.
[MapLibre details](MAPLIBRE_PUBLIC_SOURCE_STATUS.md).

## Four-provider follow-on batch

Sandbox contracts, Polygon ID schemas, SEEDS settings and OpenVINO metadata have
separate native CQL/WEB4 proofs. OpenVINO additionally passed actual production
runtime packaging, real loopback service startup/inference and graceful shutdown.
These scopes are not yet live-enrolled.

Current **unpublished** scoped parent candidate:
**d555cb2a7959a27a3af22dc3d45b06e25568bac7**, Core
**e242de41c41dbd4a1a4089694a2d945cdff828b4**,69scoped paths. Existing candidate
branch refs still point to MapLibre; publish only after accepted MapLibre and
fresh shared-ref checks. Candidate publisher is prepared, never invoked.
Exact combined-candidate qualification and candidate image remain required.
Task-image startup is now passed; the guarded publisher additionally requires
that exact API/native/graceful-stop/cleanup proof. It has not been invoked.

Original offline plan: `LocalState/COSMIC/HostedFourProviderSourcesCurrent`.
It reuses the original MapLibre261setting candidate and prepares270settings/
11changed keys/one intended activation. Source owners/public IDs and the353014byte
protected candidate are already created. Preserve them; amendments update only
candidate references. No settings were applied and no native source writes occurred.

[Sandbox](THE_SANDBOX_PUBLIC_SOURCE_STATUS.md),
[Polygon ID](POLYGON_ID_PUBLIC_SOURCE_STATUS.md),
[SEEDS](SEEDS_PUBLIC_SOURCE_STATUS.md), [OpenVINO](OPENVINO_CQL_MVP.md).

## Coverage and website

**128 scoped local /96live HTTP /28functional browser**. These are bounded demo
scopes, never full SDK/storage/ORM parity claims. Cloud HTTP tests do not increment
live counts. [Provider matrix](PROVIDER_CQL_AND_SANDBOX_STATUS.md).

Website public main remains accepted Civic card81 at
**fa0f848b2bbb7418fdbe2fca4f0e0a45367ec3bd** (byte-verified).
Website task **a0883cfccdf55aa4ef3e1a183d4913d69b454ce7** contains pending82MapLibre,
83Sandbox,84PolygonID,85SEEDS,86OpenVINO;99selected client checks pass.
For MapLibre-only publication, omit83-86; restore them only on the task branch.
Browser URL-confidence rejection is recorded; do not retry or bypass it. Recent
Windows discovery found no browser windows; no new navigation/browser acceptance.

## Other open gates

Gelato supported Gasless client passes34controlled cases on563e87ed1;
native credentials/CQL remain open. Access is deferred in the single owner list;
no immediate key/funding/account request. [Proof](GELATO_PUBLIC_SOURCE_STATUS.md).
Preserve Azure Cosmos/Oracle/Postgres hosting and Redis/Valkey TLS owner decisions,
Lens403no-retry restrictions, Nostr reliability and Cassandra/Astra hosting gates.
Cassandra's original accepted native/restart/CQL/HTTP/browser evidence is not a
reason to rerun it. Remaining inventory and deeper parity are still incomplete.

## Workspace and resource rules

Use `C:/Source/OASIS2`, parent `work/cosmic-cql-ton-public`, Core
`work/cosmic-cql-decentraland`, website `C:/Source/Web4Site` / `work/cosmic-cql`.
Preserve unrelated dirty TRON, general cleanup/owner-list edits, Core ProviderType,
STAR/WEB6, solution, native-evidence deletions and untracked provider directories.
Stage only owned paths/hunks; never reset, force-push, switch shared branches or
create another worktree/environment for this workflow.

All heavy builds/runtime/model/image downloads stay in cloud. Reuse cached tools;
retain compact evidence, verify owned cleanup, never repeat denied cleanup.
Last C: measurement45.76GiB free. This laptop's separate ledger:
[RECOVERY_LAPTOP_CLEANUP_LEDGER.md](RECOVERY_LAPTOP_CLEANUP_LEDGER.md).
Owner actions only: [ACTIONS-FOR-DAVID](../Release%20and%20Status/ACTIONS-FOR-DAVID.md).
Use the build lease for shared .NET outputs and bounded workers; no local build
or new SDK/environment is needed for current cloud qualification.

Historical checkpoints remain in git and the linked provider evidence docs.


GOMap follow-on: native SDK routing passed28controlled+1native case (3fixed
public routes); bounded CQL adapter now passed12controlled+1native case on
source525434ce2/run42480043896. Exact full3/subset2 route keys/IDs/native JSON
hashes/attribution verified from8168byte artifact13616947227 in memory. No source
writes, Core/WEB4 admission, Mongo federation/replicas/browser/live enrollment or
four-provider candidate change. Next: explicit query-only Core/WEB4 integration
and scoped cloud HTTP proof. Coverage checkpoint remains unchanged.
See [GOMap evidence](GOMAP_PUBLIC_SOURCE_STATUS.md).

GOMap query-only Core8b085f8/WEB4 owner registration,8boundary cases and1native loopback HTTP/Mongo case prepared; cloud qualification pending. Core gitlink/manifest updated together, existing source SDK/native CQL proof retained. No new live source or candidate change.

GOMap completed closeout: WEB4a1417375e/Core8b085f8 run42480693015 PASSED8+1,
native3/2/Mongo5/4/private0/public3/2SDK saves9receipts; DB absent. Hashed5132byte
main+237byte cleanup artifacts verified in memory; owned container/anonymous
volumes absent. Scoped integration checkpoint129, live96/browser28 unchanged.
No further GOMap feature/rollout before the priority chain queue. Existing Edge
window has no sandbox tab; no navigation, save replay or URL-policy bypass made.
TRON reader key missing in expected process/user/machine locations; one access
entry added to ACTIONS-FOR-DAVID and privately configuring it requested once.
Continue engineering while access is pending; do not retry anonymous endpoints.

TRON priority engineering: recovered authenticated header adapter/tests copied
byte-for-byte from existing HostedTronAuthenticatedQualification into the normal
provider/unit projects; no retained fixture/source rewritten. The exact separate
catalogue SHA2567f1c8fe3c35ef3c45ee31be6eaef72f34f8d10185a28e2177ace267822e89c02
is preserved as an explicit test fixture, not claimed as the absent old cohort.
Shared manager/LINQ walkthrough linked normally; test-only MSBuild injection and
branch switching are not used. New cloud-only workflow verifies38controlled cases
against current pinned Core; actual compilation pending. Native stage defaults off,
requires an explicit dispatch and dedicated configured reader secret, and consumes
frozen private outputs. No native request, grant, live enrollment or count increase.
Missing access remains recorded once in ACTIONS-FOR-DAVID; no repeat ask.
