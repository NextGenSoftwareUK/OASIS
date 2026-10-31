# COSMIC ORM / CQL — current status

Updated 31 October 2026. Programme incomplete. Priority: working hosted demo breadth; deep LINQ parity remains paused.

## Exact next steps

1. Respect the pending extra Development-publication timing decision in [David's single list](../Release%20and%20Status/ACTIONS-FOR-DAVID.md). Do not infer approval or ask again. Normal verified integration remains once/twice daily.
2. The frozen correctness batch is parent **79fe887e3 / Core6bbd015**. It excludes Google/Ethereum/Polygon/Arbitrum. Do not apply their staged configuration to that older application/Core revision.
3. After the matching routing-facts release, prepare the distinct routing fixture with `Scripts/Sandbox/Prepare-HostedHyperdriveRouting.py`, then run `Verify-HostedHyperdriveLoadBalancing.py --cohort routing-v1`. Setup has not run live; no routing-v1 avatar/record exists. Original Solana owner session expired; preserve it without replacement/replay.
4. Separately integrate the tested public-read profiles with their matching application/Core and joint configuration, then run hosted source/preparation/isolation/replica gates. Browser acceptance follows when IAB is available. Do not claim the local HTTP tests are hosted proof.
5. Independent next breadth work: publish the matching tested six-source application/configuration batch at the permitted cadence; Cosmos presets are staged ([native/console3/2, HTTP+Mongo5/4/isolation pass](COSMOS_PUBLIC_SOURCE_READINESS.md)), Polkadot presets/client links are also staged ([C#/native3/2 and HTTP+Mongo5/4/isolation pass](POLKADOT_PUBLIC_SOURCE_READINESS.md)), followed by NEAR C#/HTTP+Mongo pairing ([20controls and native3/2 pass;34s measured](NEAR_PUBLIC_SOURCE_READINESS.md)). No new node installation or download without inspecting existing resources. Keep red checklist scopes ahead of parity depth.

## Working copies and release state

- Work/build/test only `C:/Source/OASIS-cosmic`, parent **work/cosmic-cql-google-public**; Core same branch, **79d78598624b9a0fd6a0cfd65a81a984e3181f12** pushed/remote-verified. Parent gitlink/manifest match. Unknown root `logs` preserved.
- Parent/ Core origin/Development fetched: **0e688ac9c / 759b49a**, unchanged and already included in task history. No new Development push in the public-read work.
- Latest observed WEB4 Railway deployment **68fb8cc4-c603-4d16-a97a-0e5a30e364c2**, parent0e688ac9c, SUCCESS. Last full eight-source HTTP acceptance remains parent498e3fd7f/deployment4dbe69d0; no fresh source gate inferred from the later deployment.
- Frozen parent **work/cosmic-cql-hosted-release79fe887e3** / Core **work/cosmic-cql-sandbox-integration6bbd015** unchanged. Broader earlier local-provider source remains preserved on parent237403a75.
- Frontend `C:/Source/Web4Site`, task **work/cosmic-cql**. Public main **9b65a88** serves exact committed HTML/JS. Google/Ethereum local demo links are public; Polygon/Arbitrum links are task-staged for the next website batch. **53 client controls pass**; Cosmos/Polkadot links task-staged through5066b03, not public.
- IAB `getTab` reports Transport closed. Windows control was stopped with Escape; do not restart it or interfere with games. Browser tooling is agent-owned work, not a new David chore.

## What is actually verified live

| Scope | Verified providers |
|---|---|
| Hosted HTTP queries | MongoDB, SQLite, Neo4j, ActivityPub, IPFS, SOLID, anonymous S3 object metadata, public Solana account metadata — **8** |
| Browser queries | First five above — **5**; SOLID/S3/Solana remain open |
| Private snapshot/federation gates | Each hosted public source: native3/2, private Mongo2, paired5/4, repeat0writes/2existing, foreign-private0 |
| Replicas | New hosted private Mongo snapshots match SQLite/Neo4j ID, version, owner, visibility and metadata |

Original three-store baseline is35/35. These are scoped proofs, not complete ORM parity or universal uptime. S3/Solana are public metadata profiles; IPFS uses bounded configured CIDs on isolated Kubo. ActivityPub is read-only public statuses; SOLID uses configured resources. No general source scan is implied.

Hosted Azure is **deferred**: free-only, no subscription. Hosted Oracle is **deferred**: no account/connection. Do not re-ask signup/payment. Oracle's local native HTTP3/2+Mongo5/4 and existing Azure emulator proofs remain separate.

## New profiles: task-only, not hosted

| Profile | Verified local/native scope | Guide |
|---|---|---|
| Google Cloud Storage public object metadata | Native3/2, HTTP3/2+Mongo5/4/isolation, console3/2 | [Google](../../Devs/GOOGLE_PUBLIC_METADATA_CQL_GUIDE.md) |
| Ethereum public balances | Same scopes, chain/genesis and finalized canonical hash binding | [Ethereum](../../Devs/ETHEREUM_PUBLIC_METADATA_CQL_GUIDE.md) |
| Polygon public balances | Same scopes, fixed Polygon chain/genesis | [Polygon](../../Devs/POLYGON_PUBLIC_METADATA_CQL_GUIDE.md) |
| Arbitrum public balances | Same scopes via official `arb1.arbitrum.io/rpc`; PublicNode lacked finalized state | [Arbitrum](../../Devs/ARBITRUM_PUBLIC_METADATA_CQL_GUIDE.md) |
| Cosmos Hub public validators | Native/console3/2, HTTP3/2+Mongo5/4/isolation via explicit KJNodes | [Cosmos](../../Devs/COSMOS_PUBLIC_METADATA_CQL_GUIDE.md) |
| Polkadot public headers | Native/console3/2, HTTP3/2+Mongo5/4/isolation, SCALE/content hashes verified | [Polkadot](../../Devs/POLKADOT_PUBLIC_METADATA_CQL_GUIDE.md) |

All six have real native HTTP tests, SDK-owned private Mongo snapshots, exact IDs/provenance/visibility,9query receipts each and successful owned-fixture cleanup. Bearer/usage authority is controlled; production auth/billing, hosted replication and browser execution are not inferred.

The shared EVM reader has fixed provider/chain/genesis/metadata/path descriptors. Ethereum/Polygon remain root-path-only; Arbitrum uses `/rpc`. No endpoint fallback/latest downgrade. Public balances are address-state projections, not persisted COSMIC holons or independent settlement proofs.

Polkadot affected admission checks: **2 API / 10 Core** pass (prior Cosmos2/9 and four-profile28/80 remain recorded); **16** is the supported unsigned-read tariff catalogue, not the hosted-provider count. Each source still needs explicit permission, tariff, activation and capability admission. Query-only pricing grants no signed writes.

## HyperDrive correctness and remaining gates

- Source baseline completion exists on all3 stores. All6 directed reviewed repair routes, all6 manager continuation directions and all6 paused-baseline interleavings have native proof. Original SQLite fixture failures were explicitly reconciled; no policy/lineage bypass or new save.
- Three controlled post-commit application-ACK-loss cases and three actual loopback HTTP post-commit disconnect cases pass: one mutation, original disk-intent receipt recovery, no resubmit, two ordinary replicas per authority. All owned fixtures cleaned; services stopped.
- Frozen preflight: full API/Core-test builds,1009HyperDrive/admission contracts and3static manifests pass. Details/hashes: [recovery](REPLICA_BASELINE_AND_RECOVERY.md), [integration](HOSTED_SANDBOX_INTEGRATION_PLAN.md).
- Production hosted balancing, wider operator/late-enrollment/transport acceptance, primary-authority migration and larger stores remain red. Native snapshot-to-live delivery is not primary-authority cutover.
- Mixed unsigned public reads and canonical signed writes for the same provider are rejected by one admission factory. Preserve that fence; representation/operation-aware admission is deferred in [backlog](AFTER_DEMO_BACKLOG.md).

## Durable state, resources and commands

- Persistent source/identity/intents/ACKs only under `LocalState/COSMIC`; raw logs/TRX only `TestResults`. All recent local Mongo fixture DBs/SQLite files/exact Neo records were owner-checked and removed. Reusable Mongo/Neo stores, immutable source data and unknown outcomes remain preserved.
- Source catalogues: `HostedGcsPublicSourceCurrent`470B, `HostedEthereumPublicSourceCurrent`278B, `HostedPolygonPublicSourceCurrent`282B, `HostedArbitrumPublicSourceCurrent`273B. Stable mapping owners must not be regenerated.
- Latest joint candidate: `HostedSourceRolloutCurrent/google-ethereum-public-polygon-arbitrum-cosmos-polkadot-{before,candidate}-variables.dpapi` (67,094/83,270B),8values/24presets; not applied. Earlier individual/2/3/4/5source candidates are historical, not sequential updates to apply. Never print protected contents.
- `Publish-HostedGoogleEthereumConfiguration.py with-polkadot` already prepared that candidate; do not rerun/rewrite it blindly. Its explicit apply requires the matching new application/Core, never frozen6bb. No live variable change in this work.
- Reuse Mongo8.3.11 `LocalState/COSMIC/mongo-data`/27029/oasisRecoveryRs and stopped pinned `oasis-neo4j`; no new runtime/node/account downloads in public-profile work. [Resource/cleanup ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md) owns exact paths, sizes and lifecycle.
- Builds: `Invoke-WithProviderBuildLease.ps1`, `/m:2 /nr:false /p:UseSharedCompilation=false`; cache-only restore when required. Native runner `Test-CqlReplicaBaselineRepair.ps1 -<Provider>MongoHttp` owns its lease; never nest one.
- Validate committed pins: `python Scripts/validate_railway_dependency_manifest.py --require-gitlinks`. Frontend: `node --test cql-playground.test.cjs cql-demo-preparation.test.cjs sandbox-api.test.cjs`.

Acceptance: [strict checklist](CQL_PARITY_CHECKLIST.md), [CQL roadmap](CQL_COMPLETION_ROADMAP.md), [ORM roadmap](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md). V2 implementation only; legacy gaps document-only. No overall completion percentage is inferred from focused demos.
