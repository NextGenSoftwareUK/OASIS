# COSMIC ORM / CQL â€” current status

Updated 31 October 2026. Programme incomplete. Priority: working hosted demo breadth; deep LINQ parity remains paused.

## Exact next steps

1. ApprovedDevelopment57174def3/Core6bbd015 deployedSUCCESS. Routing-v1onewrite/3exactreplicas and3explicitreads pass; Default wasboundtoMongo beforebalancing. Scopedrootfix450614de6 passes13guard+61HTTPtests0skips, awaitingfollowup publication decision in [David list PartF](../Release%20and%20Status/ACTIONS-FOR-DAVID.md). NoextraDevelopmentpush.
2. The frozen correctness batch is parent **79fe887e3 / Core6bbd015**. It excludes all newly staged public-read profiles. Do not apply their staged configuration to that older application/Core revision.
3. Originalrouting-v1fixture/session/saveACK preserved. Aftermatchingfixdeployment run Verify-HostedHyperdriveLoadBalancing.py --cohort routing-v1, reads only. Do notresave/recreatesession or repeatsuccessfulsetup. Browseracceptance remainsopen.
4. Separately integrate the tested public-read profiles with their matching application/Core and joint configuration, then run hosted source/preparation/isolation/replica gates. Browser acceptance follows when IAB is available. Do not claim the local HTTP tests are hosted proof.
5. Base/Avalanche/BNB native HTTP+Mongo gates pass:3/2native,5/4paired,isolation and9receipts each; original SDK cohorts cleaned exactly. Strict preset builders10controls and website links54client tests pass (frontend taskb6c6854;3new preparation contracts pass). Ten-source protected candidate now prepared offline; all40actual presets pass shipping-planner preflight. Read-only hosted reconciliation confirms snapshot still exact (12planned settings,no drift). Single12setting apply-reviewed entry point prepared (2scope tests),not executed. Next permitted matching rollout and live acceptance. [Three-source report](EVM_NEXT_THREE_SOURCE_STATUS.md).

Independent next breadth: Optimism native/published C#/CLI3/2 and HTTP/Mongo3/2+5/4/isolation pass;1Core/2API/3configuration controls pass. Client preparation1test and website link passe8df3c4; optional eleven-source protected candidate now staged; actual4Optimism presets pass planner (1test/0skips),3application-scope controls pass. Next permitted matching release and hosted acceptance. Original ten-source batch preserved.

Linea/Scroll native+publishedC#/actualCLI3/2 and HTTP/Mongo3/2+5/4/isolation pass;2Core/4API/6config/2nativeHTTP/2client checks pass. Website links/client proofs pushed8c54051;optional13source batch prepared with corrected retained-source endpoint validation,8newpresets pass shippingplanner (1test/0skips),4scopecontrols pass. Matching hostedrollout pending. Rootstock finalized-header qualificationHTTP400 remains red. Existing release candidates unchanged.

zkSync/Abstract native+publishedC#/CLI and HTTP3/2+Mongo5/4/isolation pass;2Core/4API/6config/2nativeHTTP/2client checks pass. Website links/client proofs pushed5aad1d1. Optional15source/60preset candidate staged separately;8newpresets pass planner,5scope controls pass. Currenthost snapshot exact (17planned settings,no drift). Matching permittedrollout next. Existing13source releasecandidate unchanged.

Monad native/C#/CLI and HTTP3/2+Mongo5/4/isolation pass;1Core/2API/3config/1nativeHTTP/1client checks pass. Website link/clientproof pushed354958e; optional16source/64preset candidate prepared;4newMonadpresets passplanner (1test/0skips),6scopechecks pass. Hosted rollout remains separate; the approved routing deployment succeeded, and its Default-routing correction awaits a separate publication decision. Sei native/C#/CLI and HTTP3/2+Mongo5/4/isolation pass;1Core/2API/3config/1nativeHTTP/1client checks pass. Website link/clientproof pushed36933fb; Optional seventeen-source candidate now prepared: 68 presets; all four added Sei presets pass the shipping planner (1 test, zero skips), and all 7 application-scope controls pass. Matching hosted rollout and acceptance remain next. Existingcanonical profiles retainstrictwireformat. Existing15sourcecandidate unchanged.

## Working copies and release state

- Work/build/test only `C:/Source/OASIS-cosmic`, parent **work/cosmic-cql-google-public**; Core same branch, **56c869ff8de0a520da8c09ba8ebc6f85bbd40ea4** pushed/remote-verified. Parent gitlink/manifest match. Unknown root `logs` preserved.
- Merged parent origin/Development57174def3; Core6bbd015 already ancestor of current taskCore6d26291. Documentation conflicts resolved preserving both histories/newer provider status. No provider expansion deployed.
- Approved Railway job e107f7e5-fb8b-4cce-854b-46ebad7c8600,parent57174def3,SUCCESS. Priorlive68fb8cc4-c603-4d16-a97a-0e5a30e364c2,parent0e688ac9c,SUCCESS. Last full eight-source HTTP acceptance remains parent498e3fd7f/deployment4dbe69d0; no fresh source gate inferred from the later deployment.
- Frozen parent **work/cosmic-cql-hosted-release79fe887e3** / Core **work/cosmic-cql-sandbox-integration6bbd015** unchanged. Broader earlier local-provider source remains preserved on parent237403a75.
- Frontend `C:/Source/Web4Site`, task **work/cosmic-cql**. Public main **9b65a88** serves exact committed HTML/JS. Google/Ethereum local demo links are public; Polygon/Arbitrum links are task-staged for the next website batch. **54 client controls pass**; Cosmos/Polkadot/NEAR links task-staged throughe8bf2d3, not public.
- IAB `getTab` rechecked after ten-source reconciliation31October: Transport closed; existing live deployment also rechecked unchanged. Windows control was stopped with Escape; do not restart it or interfere with games. Browser tooling is agent-owned work, not a new David chore.

## What is actually verified live

| Scope | Verified providers |
|---|---|
| Hosted HTTP queries | MongoDB, SQLite, Neo4j, ActivityPub, IPFS, SOLID, anonymous S3 object metadata, public Solana account metadata â€” **8** |
| Browser queries | First five above â€” **5**; SOLID/S3/Solana remain open |
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
| NEAR public accounts | Native/console3/2, original-cohort HTTP3/2+Mongo5/4/isolation via FastNear; zero resaves | [NEAR](../../Devs/NEAR_PUBLIC_METADATA_CQL_GUIDE.md) |

All seven have real native HTTP tests, SDK-owned private Mongo snapshots, exact IDs/provenance/visibility,9query receipts each and successful owned-fixture cleanup. Bearer/usage authority is controlled; production auth/billing, hosted replication and browser execution are not inferred.

The shared EVM reader has fixed provider/chain/genesis/metadata/path descriptors. Ethereum/Polygon remain root-path-only; Arbitrum uses `/rpc`. No endpoint fallback/latest downgrade. Public balances are address-state projections, not persisted COSMIC holons or independent settlement proofs.

NEAR affected admission checks: **2 API / 11 Core** pass (prior scope results remain in source reports); **27** is the supported unsigned-read tariff catalogue, not the hosted-provider count. Each source still needs explicit permission, tariff, activation and capability admission. Query-only pricing grants no signed writes.

Prepared seven-source batch: all28actual presets pass the shipping planner with real source/Mongo capabilities (1test/0skips); public-only exporter3controls pass. This is offline planning, not hosted/browser acceptance. [Release preflight](HOSTED_SANDBOX_INTEGRATION_PLAN.md#seven-source-prepared-configuration-review--31-october-2026).

## HyperDrive correctness and remaining gates

- Source baseline completion exists on all3 stores. All6 directed reviewed repair routes, all6 manager continuation directions and all6 paused-baseline interleavings have native proof. Original SQLite fixture failures were explicitly reconciled; no policy/lineage bypass or new save.
- Three controlled post-commit application-ACK-loss cases and three actual loopback HTTP post-commit disconnect cases pass: one mutation, original disk-intent receipt recovery, no resubmit, two ordinary replicas per authority. All owned fixtures cleaned; services stopped.
- Frozen preflight: full API/Core-test builds,1009HyperDrive/admission contracts and3static manifests pass. Details/hashes: [recovery](REPLICA_BASELINE_AND_RECOVERY.md), [integration](HOSTED_SANDBOX_INTEGRATION_PLAN.md).
- Production hosted balancing, wider operator/late-enrollment/transport acceptance, primary-authority migration and larger stores remain red. Native snapshot-to-live delivery is not primary-authority cutover.
- Mixed unsigned public reads and canonical signed writes for the same provider are rejected by one admission factory. Preserve that fence; representation/operation-aware admission is deferred in [backlog](AFTER_DEMO_BACKLOG.md).

## Durable state, resources and commands

- Persistent source/identity/intents/ACKs only under `LocalState/COSMIC`; raw logs/TRX only `TestResults`. All recent local Mongo fixture DBs/SQLite files/exact Neo records were owner-checked and removed. Reusable Mongo/Neo stores, immutable source data and unknown outcomes remain preserved.
- Source catalogues: NEAR `HostedNearPublicSourceCurrent`224B (explicit FastNear, stable owner), `HostedGcsPublicSourceCurrent`470B, `HostedEthereumPublicSourceCurrent`278B, `HostedPolygonPublicSourceCurrent`282B, `HostedArbitrumPublicSourceCurrent`273B. Stable mapping owners must not be regenerated.
- Latest joint candidate: `HostedSourceRolloutCurrent/google-ethereum-public-polygon-arbitrum-cosmos-polkadot-near-{before,candidate}-variables.dpapi` (67,094/85,206B),9values/28presets; not applied. Earlier individual/2/3/4/5/6source candidates are historical, not sequential updates to apply. Never print protected contents.
- `Publish-HostedGoogleEthereumConfiguration.py with-near` already prepared that candidate; do not rerun/rewrite it blindly. Its explicit apply requires the matching new application/Core, never frozen6bb. No live variable change in this work.
- Reuse Mongo8.3.11 `LocalState/COSMIC/mongo-data`/27029/oasisRecoveryRs and stopped pinned `oasis-neo4j`; no new runtime/node/account downloads in public-profile work. [Resource/cleanup ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md) owns exact paths, sizes and lifecycle.
- Builds: `Invoke-WithProviderBuildLease.ps1`, `/m:2 /nr:false /p:UseSharedCompilation=false`; cache-only restore when required. Native runner `Test-CqlReplicaBaselineRepair.ps1 -<Provider>MongoHttp` owns its lease; never nest one.
- Validate committed pins: `python Scripts/validate_railway_dependency_manifest.py --require-gitlinks`. Frontend: `node --test cql-playground.test.cjs cql-demo-preparation.test.cjs sandbox-api.test.cjs`.

Acceptance: [strict checklist](CQL_PARITY_CHECKLIST.md), [CQL roadmap](CQL_COMPLETION_ROADMAP.md), [ORM roadmap](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md). V2 implementation only; legacy gaps document-only. No overall completion percentage is inferred from focused demos.
