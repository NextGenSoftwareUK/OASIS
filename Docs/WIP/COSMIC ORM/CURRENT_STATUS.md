# COSMIC ORM / CQL — current status

Updated 1 November 2026. Programme incomplete. Goal resumed after the routing publication approval; preserve the full roadmap. Priority: hosted demo breadth and reproducible HyperDrive bugs; deep LINQ parity remains paused.

## Active deployment

David approved the correction on 1 November. Scoped Development051ac6af7841f95d4babb1a3e061feee06b4a6c6 is pushed and remote-verified; Core stays6bbd015. Exact Railway jobd8876691-5ce2-4495-9e3d-dedfb1732de0 is QUEUED. Poll this job; never trigger a replacement because observation times out. New public profiles/settings are excluded. Browser transport and deferred hosting remain separate limits.

## Exact next steps

1. Observe the exact approved correction job above until terminal. Do not infer live behavior from a successful git push. Owner approval is recorded in [David list, Part F](../Release%20and%20Status/ACTIONS-FOR-DAVID.md).
2. The routing-v1 owner session is now verified expired. Preserve its owner, record and save ACK; do not rerun setup or replace that identity. After the matching correction deploys, explicitly plan a distinct bounded acceptance cohort before live balancing/failover checks.
3. Integrate the separately tested public-read expansion with its matching Core, application and joint configuration in a permitted release batch. Then verify hosted native queries, Mongo presets, isolation, replicas and browser behavior.
4. Finish SOLID, S3 and Solana browser acceptance when browser control recovers. IAB documentation retry still returns `Transport closed`; this is agent-owned work.

## Release and working copies

- Work/build/test only `C:/Source/OASIS-cosmic`; parent/Core task branch `work/cosmic-cql-google-public`. Parent b36259d1e pushed; Core56c869ff8de0a520da8c09ba8ebc6f85bbd40ea4 pushed and remote-verified. Gitlink/manifest match. Unknown root `logs` preserved.
- Scoped correction branch `work/cosmic-cql-hosted-release`:450614de6 includes production fix51de129d3 and incoming Development494f988bc owner-document updates. It excludes the public-read expansion.
- Approved Development57174def3/Core6bbd015 deployed successfully: Railway e107f7e5-fb8b-4cce-854b-46ebad7c8600. Remote Development is now494f988bc (another contributor's owner-document update).
- Frontend `C:/Source/Web4Site`, branch `work/cosmic-cql`,36933fb pushed. Public main last verified9b65a88; later source links remain task-only.

## What is actually verified

| Scope | Evidence and limits |
|---|---|
| Hosted HTTP: 8 providers | MongoDB, SQLite, Neo4j, ActivityPub, IPFS, SOLID, public S3 object metadata, public Solana account metadata. Last full eight-source acceptance498e3fd7f; later deployment does not imply a fresh complete run. |
| Browser: 5 providers | MongoDB, SQLite, Neo4j, ActivityPub and IPFS. SOLID/S3/Solana remain open. |
| Hosted source/Mongo presets | Native3/2, private Mongo2, paired5/4, repeated preparation0writes/2existing, foreign-private0. Snapshot replicas match SQLite/Neo4j IDs, versions, owners, visibility and metadata. |
| Original three-store baseline | 35/35 within its declared scope; not full ORM parity. |
| Frozen correctness preflight | 1,009 contracts pass; full API/Core-test builds and three static manifests pass. |
| New routing-v1 cohort | One acknowledged Mongo create; exact three-store baseline and three explicit-provider reads pass. Default balancing fails on the deployed image. |
| Default-routing correction | Unbound manager acquisition preserves Default until HyperDrive chooses. 13 acquisition guards plus61 HTTP/controller tests pass, zero skips. Fix committed; live acceptance pending. |

Hosted Azure is deferred: free-only, no subscription. Hosted Oracle is deferred: no account/connection. Do not re-ask signup/payment. Existing Azure emulator and Oracle local native proofs are separate from hosting.

## Prepared public-read expansion — not hosted

Seventeen new profiles have native/C#/CLI and local HTTP/Mongo/isolation proofs: Google Cloud Storage, Ethereum, Polygon, Arbitrum, Cosmos Hub, Polkadot, NEAR, Base, Avalanche, BNB, Optimism, Linea, Scroll, zkSync, Abstract, Monad and Sei.

These are bounded unsigned public projections, not owned native persistence or independent chain settlement proofs. Each has one ready Mongo federation preset family. HTTP gates verify native3/2, paired5/4, exact IDs/provenance/visibility and query receipts; controlled bearer/usage authority is not production auth/billing proof.

- Optional seventeen-source candidate:68 presets,19 settings, preserved under `LocalState/COSMIC/HostedSourceRolloutCurrent/seventeen-source-public-{before,candidate}-variables.dpapi`. Earlier candidates remain preserved; do not apply them sequentially.
- The complete seventeen-source candidate now passes the shipping planner: all68actual presets using shipping provider/Mongo capabilities (1 test, zero skips). Eleven application/release-guard controls also pass. Publication now enforces an exact successful deployment matching the reviewed checked-out release before any configuration mutation. This is offline planning, not hosted execution.
- Read-only hosted reconciliation now confirms the original snapshot still matches exactly: zero changed keys,19 planned settings, no mutation or deployment.
- Public review, planner TRX and reconciliation: `TestResults/HostedCqlIntegrationCurrent/`. No protected values in reports.
- Core supports27 unsigned-read tariff types; that is not the hosted-provider count. Query pricing never grants signing/storage authority.
- Rootstock remains red: selected endpoint rejects finalized-header requests. No silent downgrade or source profile created. Sei's explicit mixed-case hash codec is scoped to Sei; canonical profiles remain strict.

Developer manual now lists all17prepared profiles, their native metadata keys/CLI modes and an explicit two-key HTTP example. Nine stale console messages now distinguish native-run proof, separately verified local federation and pending hosting.

## Remaining HyperDrive and ORM work

All six native directed repair routes, manager continuations and paused-baseline interleavings pass. Three application ACK-loss cases and three real loopback HTTP post-commit disconnect cases prove one mutation, original receipt recovery without resubmission and two ordinary replicas. Owned fixtures were cleaned after verification.

The unfinished hosted failover gate now requires per-response proof of a different enrolled serving provider, in addition to exact record preservation; eight acceptance controls pass (including all six directed routes and expiry/replay guards). This is verifier coverage, not a new live pass.

Still red: hosted Default balancing; wider operator/late-enrollment/transport acceptance; primary-authority cutover; scale certification; full ORM acceptance reconciliation. Replica delivery is not migration of write authority. Mixed unsigned-read/signed-write representations retain the admission fence.

Deep LINQ terminals, unrestricted non-IHolon POCO mapping and migration tooling remain deferred. Work only on red acceptance scopes; revisit green scopes only for a reproducible bug. Legacy implementation remains document-only.

## Resources and key commands

- Durable source identities/intents/ACKs: `LocalState/COSMIC`; regenerable logs/TRX: `TestResults`, never Docs. Do not replay acknowledged setup or regenerate stable source owners.
- Reuse Mongo8.3.11, `LocalState/COSMIC/mongo-data`,27029/oasisRecoveryRs; stopped pinned `oasis-neo4j`. No new downloads/installations for this milestone. Latest free C:75,428,438,016 bytes.
- Builds use `Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1` and `/m:2 /nr:false /p:UseSharedCompilation=false`. Provider runners own their lease; never nest.
- Pins: `python Scripts/validate_railway_dependency_manifest.py --require-gitlinks`.
- Read-only drift check: `python Scripts/Sandbox/Reconcile-TenSourceHostedConfiguration.py --scope seventeen`.

Details: [integration](HOSTED_SANDBOX_INTEGRATION_PLAN.md), [source expansion](EVM_NEXT_THREE_SOURCE_STATUS.md), [recovery](REPLICA_BASELINE_AND_RECOVERY.md), [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md), [backlog](AFTER_DEMO_BACKLOG.md), [strict parity checklist](CQL_PARITY_CHECKLIST.md).
