# CQL demo launcher

[Presenter sequence and expected results](CQL_DEMO_RUNTHROUGH.md).

Run from C:/Source/OASIS-cosmic. One interactive demo at a time; each runner owns
its build lease and verified fixture teardown. Do not nest a lease around the hub.
All examples below launch native-backed teaching presets with synthetic fixtures.
Hosted Azure remains unavailable until free-account setup; local Cosmos is labelled.

## Mongo-paired presets

```powershell
# Discover the command and URL without starting services or writing records.
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Oracle -Federated -ShowPlan

# Start the Oracle teaching page; archive only a completed unskipped pass.
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Oracle -Federated -ArchiveCompletedDemo

# Bounded SDK/HTTP verification instead of waiting for browser shutdown.
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Oracle -Federated -VerifyOnly -ArchiveCompletedDemo
```

| Provider argument | URL | Matching native participation | ArchiveCompletedDemo |
|---|---|---|---|
| AWS | http://127.0.0.1:5096/ | Mongo2/DynamoDB Local3, then2/2 | Omit; reusable AWS seed, verified temporary Mongo cleanup |
| Oracle | http://127.0.0.1:5090/ | Mongo2/Oracle3, then2/2 | Explicit opt-in; rejects failed/pending outcomes |
| CosmosDB | http://127.0.0.1:5092/ | Mongo2/local Cosmos3, then2/2 | Omit; Cosmos runner owns hash-preserved report reuse |
| SolanaMetadata | http://127.0.0.1:5091/ | Mongo1/Solana1 via explicit SQLite index;0empty retains assurance | Pass to preserve reviewed completed evidence |
| IPFS | http://127.0.0.1:5088/ | Mongo2/IPFS3, then2/2; native CID membership | Pass for completed evidence reuse |
| SOLID | http://127.0.0.1:5088/ | Mongo2/SOLID3, then2/2; native resource membership | Pass for completed evidence reuse |
| ActivityPub | http://127.0.0.1:5088/ | Mongo2/public Mastodon3, then2/2; native status membership | Pass for completed evidence reuse |

Use `-Federated` for every row. Actor, source scopes and readable catalogues are
server-owned. The Solana host uses independent bounded ingestion; no chain write.
Oracle and AWS are exposed through paired presets only. Plain Solana (schema2), SQLite
and the Databases group do not accept this new flag; the group retains its existing
federation walkthrough. Unsupported combinations reject before starting services.
Protocol `-Federated` already selects the multi-record mode; `-MultiRecord` is optional.

Follow the page's shutdown instructions. Oracle/Cosmos/Solana have cleanup buttons;
protocol hosts expose `POST /demo/shutdown`. Do not kill a runner midway through a
write or discard a pending journal. Unknown outcomes require reconciliation.

## Existing individual profiles

Without `-Federated`, existing SQLite, Databases, CosmosDB, Solana,
SolanaMetadata, IPFS, SOLID and ActivityPub launchers retain their current behavior.
Protocol `-MultiRecord` selects native several-record membership. `-VerifyOnly`
keeps acceptance bounded; `-ShowPlan` is read-only and starts no services.

## Verification scope

`Scripts/TestHosts/Test-CosmicCqlDemoHubRouting.ps1` checks8individual profiles,
16lifecycle plans,7paired profiles/14plans,5unsupported-pair rejections and3existing
archive rejections. Native/demo listeners remain unchanged during discovery.
Raw report:TestResults/ProtocolCqlCurrent/database-hub-routing.json.
Discovery proves routing only; native provider proof remains in each linked report
from [current status](CURRENT_STATUS.md), including explicit pre/post-merge scope.


Post-merge Oracle hub execution also passes1/1native SDK/HTTP case, hash
28CA5EE9AA3B227E2320A1A3A492369D8661B94D82FD105A45D6CF47C4360239,
with exact cleanup and service teardown. Discovery JSON SHA256
541AAFB947BF22DFD766621ECEF2C36784FA1F6725AD546C563D511F143CC37E.
Other paired hub routes are discovery-tested against their previously verified
individual runners; this milestone does not claim fresh native reruns of all providers.

## AWS local MVP

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider AWS -Federated
```

Open http://127.0.0.1:5096/ after startup. Native AWS SDK + HTTP and actual browser
buttons verify5/4 rows with provider copies. Bounded verification uses -VerifyOnly.
The existing DynamoDB runtime is reused read-only; persistent CQL data are inside
this worktree. No hosted AWS or arbitrary metadata-query claim. See [AWS MVP](AWS_CQL_MVP.md).
