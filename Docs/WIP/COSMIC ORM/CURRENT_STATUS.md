# COSMIC ORM / CQL current status

Updated 8 November 2026. Goal active and incomplete. Demo breadth comes first;
full native provider implementation and EF/NHibernate parity remain on the
roadmap. Imported, virtual/pointer and hybrid holon modes remain in that roadmap;
this checkpoint does not claim completion of those designs.

## Immediate work

- Full WEB4 image **42473514300** failed Waku503 on all3bounded attempts.
  Failure retained; no live deployment requested.
- Native producer publication **42474138782** passed for exact source
  **0210139b8b0efebc2a2043ff16dfcf42cfe2a480**; Waku/NATS native-only image
  digests and hashed ACK artifacts verified. Same existing WEB4 registry package.
- Final image source **8668fd2d040da71ba27e27e49fbed6792eb4e094** now consumes
  those exact runtime digests.5local policy/graph checks pass; origin producer
  stages are absent from final dependency closure. Cloud consumer verification
  **42474992596** passed both roles: exact pinned digests, producer identity,
  native versions and owned-container removal verified from hashed artifacts
  13615241072 (1103 bytes) and 13615270909 (1084 bytes), inspected in memory.
  Full image42475406236/job128715501131 SUCCEEDED for exact source8668fd2d.
  Explicit checkout/source summary/tag/push digest verified:
  sha256:dcd1e9f67e9e44cfb721b23c47c26ad0f79cf490472a648cd32f7d8e5cec0496.
  No live mutation.
- Native download policy and actual Waku/NATS stages passed **42473182023**;
  artifacts/checksums/native versions and4controlled policy cases verified.
- All seven exact private dependency checkouts passed **42472240791**. The
  production clone script fetches each manifest SHA before checkout. No pin changed.
- Earlier full image failures are preserved:42468917135 release504,
  42471538567 exact WEB6 object absent from branch-tip clone,42472504855 NATS502,
  42473514300 Waku503after3bounded attempts.
  The latest source includes the independently qualified corrections.
- After image success, verify exact source/tag/digest and packaged runtime/pins/
  assemblies, then genuine combined API/native startup using only fresh owned
  cloud data. Full API image startup is still unproven. Task-image qualification
  never authorizes sweeping unrelated work into Development.
  Cloud-only package/startup workflow is prepared: exact tag/digest/helper/model
  hashes/six assemblies first, then optional real API/native startup with fresh
  Mongo replica/accounting data on an internal network. Syntax/config invariants
  pass, including seven actual health-probe response controls and verified-volume
  cleanup requirements; the cloud workflow runs those controls before inspecting
  an image. Initial manual-only workflow request returned404/no job; source-only
  push registration411100785 corrected that. One new combined package/startup
  request acknowledged204 after publication; original run discovery pending.

[OpenVINO/runtime/build evidence](OPENVINO_CQL_MVP.md).
[Artifact download contract](../../Devs/WEB4_NATIVE_ARTIFACT_DOWNLOADS.md).

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
**6284fc990d9e964f3e295599ee3b923998a2569a**, Core
**e242de41c41dbd4a1a4089694a2d945cdff828b4**,65scoped paths. Existing candidate
branch refs still point to MapLibre; publish only after accepted MapLibre and
fresh shared-ref checks. Candidate publisher is prepared, never invoked.
Exact combined-candidate qualification and final image remain required.

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
Last C: measurement46.02GiB free. This laptop's separate ledger:
[RECOVERY_LAPTOP_CLEANUP_LEDGER.md](RECOVERY_LAPTOP_CLEANUP_LEDGER.md).
Owner actions only: [ACTIONS-FOR-DAVID](../Release%20and%20Status/ACTIONS-FOR-DAVID.md).
Use the build lease for shared .NET outputs and bounded workers; no local build
or new SDK/environment is needed for current cloud qualification.

Historical checkpoints remain in git and the linked provider evidence docs.


GOMap follow-on: canonical bounded OSRM client passed28/28controlled cloud cases
(run42476991119, sourcef393451ad). Artifact13616200194/4667bytes hash/TRX verified
in memory. Native developer run42477332751/source24d53fc82 additionally passed28+1cases
and three actual fixed Berlin routes; raw response hashes and attribution verified
in memory. No CQL/live enrollment, coverage increment or four-provider candidate
change. Hosting/use-contract and CQL/HTTP/replica/browser gates remain open.
See [GOMap evidence](GOMAP_PUBLIC_SOURCE_STATUS.md).
