# COSMIC ORM / CQL current status

Updated 4 November 2026. Goal active and incomplete.
Priority: breadth-first usable demos; deep selected-stage LINQ stays paused.
Work only red scopes or reproduced bugs. Legacy issues documentation-only.

## Verified scope

- **96 local query types/profiles:**3 canonical, 57 public/source, 36 other local;
  includes 31 recovered historical demos. Not 96 fully implemented providers.
- **60 scoped hosted HTTP proofs; 5 browser proofs / 55 browser gates open.**
- MongoDB/SQLite/Neo4j strict parity **50/82 each**; investor subset green.
- Published launcher audit:37 links / 37 present / **0 missing**; 3 audit tests pass.
- Public ThreeFold/ClickHouse/Zcash cohorts are accepted and frozen.

## Current batch / exact next step

Live demo rehearsal, 4 November: Development health200; authenticated discovery
59/60 ready (Nostr unavailable). ActivityPub and IPFS native examples pass3/2.
Published sandbox client IPFS+Mongo passes5/4; two private saves, reuse0 new saves,
SQLite/Neo4j replicas2/2. HTTP/client proof, browser rerun open. Report SHA256
679D2FA2F738B1388BC51B8F77059ED700D73D31BFA81C1B6F6F0D277C56AA19
at TestResults/HostedDemoRehearsal/ipfs-http-flow.json. Separate rehearsal session;
prior cohorts preserved. A second visitor saw3 public rows and0 of the private
Mongo snapshots by exact ID. Isolation summary SHA256
64AEEB6D8B1427DD178A9F95F698185998C78D38D8319653DB96DA6AB1904F90.
LocalFile canonical sandbox-mode fix passes 16 native profile, 13 WEB4 and
5 publisher tests. Retained source parser check awaits the original bundle.
Restore the four files listed by publisher preflight, verify hashes and parser
admission, prepare the five settings, then qualify the exact Development
deployment before applying and rehearsing native, Mongo and replica reads.

Source-recovery fix published Development b718f2879;Core5415961/settings unchanged.
Exact job33c9ef7d-b8b3-4403-9109-32ce47c4d7a8 SUCCESS;healthy/four-provider-ready.
Live anonymous401/demo-user403 pass;59/60ready,Nostr unavailable;operator gate open.
Journal LocalState/COSMIC/HostedSourceRecoveryReleaseCurrent.
Prior d167dff13 healthy/four-provider-ready;59/60ready,Nostr activation HTTP503.
Previous fifth release5507b0aa0 passed health/four-provider discovery/federation.
Published launcher file audit is green;runtime/configuration gates remain.

## V2 owned EVM proof and discovered Mongo bug

Shipping Arbitrum V2 facade + canonical SDK/ABI/decryption + published manager/
LINQ walkthrough pass native3/2 over retained encrypted records. Original seed
unchanged;zero chain writes requested. Owned node exited0.

New native Mongo-paired queries 5/4 passed;original overall run FAILED during
cleanup because Mongo absent-root returned RecordNotFound but IsError=false.
Common read boundary fixed. Separate native reconciliation 1/1 passes sync+async
absence for both IDs;exact original temporary fixture removed, no query replay.
Original failed TRX and reconciliation proof preserved, never relabelled green.
[EVM proof/limits](EVM_V2_CQL_RECOVERY_GAP.md). Hosted owned-EVM gate remains open.

Next source/demos:remaining runtime/configuration gates;preserve current public
adapters, use actual V2 factories and compatible retained contracts. Do not
restore legacy Ethereum demos as V2 proof. No new environment needed.

NEAR(8),Cosmos blockchain(7),Polkadot(7),Sui(7),Aptos(10) recovery checks/builds pass;public profiles kept,
original native environment untouched. Published code;not hosted native acceptance.

TON review found EVM private-query admission without required chain identity.
Regression reproduced;shared V2 fix17controlled cases pass, no native replay.
Explicit TonLocalTvmStorageSource recovered;4controls/builds/syntax pass.
Native local TVM1/1passes encrypted reopen/query3/2/0 and budgets/ownership.
Owned disposable fixture removed;original source untouched. Legacy unchanged.
Oracle local paired/content launcher/consumer restored;build+AST checks pass.
Protocol wrapper/keyset closure restored;3consumer builds+AST checks pass.
TRON declared-source7controls/builds/syntax pass;old uncertain outcome untouched.
Configured V2 Ethereum/Polygon/Arbitrum reader+Mongo/HTTP consumer restored;
7factory/configuration controls pass. Needs explicit compatible existing source.
Source recovery live;27controlled/API/schema cases pass;secure sign-in helper ready.
See CQL_SOURCE_RECOVERY.md. Website commands remain to correct.
Ethereum V2 artifact/ABI/cache+6deploymentcontrols pass;native operation review-blocked.
LocalFile16profile+41API pass;source export/retained parser1+1,rollout controls5 pass.
Next:fix reproduced bootstrap sandbox-setting mismatch (DNA true,extra flag absent);
preparation stopped before protected candidate/config mutation. Hosted proof remains open.
Automatic review rejected the combined website-edit/consumer-build operation;
it did not execute. Website commands remain open. A separate reproduced page
bug is fixed/tested below;no new native/browser proof or denied-operation retry.
Independent sixth-batch shipping API/provider build and GraphQL schema guard
pass1/1,no skips. Original hash/counts:LocalState/COSMIC/LauncherRecoveryCurrent/
sixth-batch-shipping-schema-evidence.json. Consumer now compiles;3page regressions pass.

## Constraints / wider unfinished work

- Hosted Azure Cosmos/Oracle/PostgreSQL deferred by David;no account/spend re-asks.
- Browser/OS control closed;HTTP/static matching is not interaction proof.
- Preserve denied/failed public-source outcomes and original accepted cohorts.
- ORM children/depth/history/deltas,cutover/migration/placement and wider hosted/
  operator/transport/scale HyperDrive acceptance remain open;no uptime guarantee.
- Native browser snapshots do not yet copy arbitrary complete source payloads.

Work only C:/Source/OASIS-cosmic;task branch work/cosmic-cql-ton-public;
Core work/cosmic-cql-google-public;website C:/Source/Web4Site/work/cosmic-cql.
Use one build lease;/m:2 /nr:false /p:UseSharedCompilation=false.
Raw logs TestResults/;original identities/proof LocalState/COSMIC/;named-file commits.
No production/master promotion;preserve unknown root logs and shared/denied cleanup.
Disk120GB target unmet;[resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md) owns resources.

## Entry points / commands

[Full provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) | [Manual](CQL_MANUAL.md) |
[Architecture](CQL_ARCHITECTURE.md) | [Parity](CQL_PARITY_CHECKLIST.md) |
[Launcher gaps](LOCAL_LAUNCHER_INTEGRATION_GAP.md) | [Backlog](AFTER_DEMO_BACKLOG.md) |
[David actions](../Release%20and%20Status/ACTIONS-FOR-DAVID.md)

```powershell
python Scripts/validate_railway_dependency_manifest.py --require-gitlinks
python -B Scripts/Sandbox/Audit-LocalDemoLaunchers.py --html C:/Source/Web4Site/web4-sandbox.html --output TestResults/LauncherRecoveryCurrent/local-launcher-audit.json
```

Audit now exits0:37files present, not native execution. Never rerun
accepted native standalone3/2 or paired queries just to rebuild evidence.
