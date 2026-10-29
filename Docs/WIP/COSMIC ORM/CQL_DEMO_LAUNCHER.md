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
| CosmosChain | http://127.0.0.1:5101/ | Mongo2/retained Cosmos blockchain3, then2/2 | Omit; retained native contract/key/seed, temporary Mongo fixture |
| Polkadot | http://127.0.0.1:5102/ | Mongo2/retained Polkadot3, then2/2 | Omit; retained encrypted native seed, temporary Mongo fixture |
| NEAR | http://127.0.0.1:5103/ | Mongo2/retained NEAR3, then2/2 | Omit; original encrypted source/key, temporary Mongo fixture |
| Base | http://127.0.0.1:5104/ | Mongo2/Base3 on local EVM31338, then2/2 | Omit; retained encrypted native seed, temporary Mongo fixture |
| Avalanche | http://127.0.0.1:5105/ | Mongo2/Avalanche3 on local EVM31338, then2/2 | Omit; original contract/IDs and encrypted seed retained |
| Google | http://127.0.0.1:5100/ | Mongo2/retained Firestore emulator3, then2/2 | Omit; immutable seed/checkpoint, temporary Mongo fixture |
| Arbitrum | http://127.0.0.1:5099/ | Mongo2/shipping Arbitrum V2 adapter3, then2/2 on local EVM | Omit; retained chain/seed, temporary Mongo fixture |
| Polygon | http://127.0.0.1:5098/ | Mongo2/concrete Polygon adapter3, then2/2 on retained local EVM | Omit; retained chain/seed, temporary Mongo fixture |
| Ethereum | http://127.0.0.1:5097/ | Mongo2/local Ethereum3, then2/2 | Omit; retained local chain/seed, temporary Mongo fixture |
| AWS | http://127.0.0.1:5096/ | Mongo2/DynamoDB Local3, then2/2 | Omit; reusable AWS seed, verified temporary Mongo cleanup |
| Oracle | http://127.0.0.1:5090/ | Mongo2/Oracle3, then2/2 | Explicit opt-in; rejects failed/pending outcomes |
| CosmosDB | http://127.0.0.1:5092/ | Mongo2/local Cosmos3, then2/2 | Omit; Cosmos runner owns hash-preserved report reuse |
| SolanaMetadata | http://127.0.0.1:5091/ | Mongo1/Solana1 via explicit SQLite index;0empty retains assurance | Pass to preserve reviewed completed evidence |
| IPFS | http://127.0.0.1:5088/ | Mongo2/IPFS3, then2/2; native CID membership | Pass for completed evidence reuse |
| SOLID | http://127.0.0.1:5088/ | Mongo2/SOLID3, then2/2; native resource membership | Pass for completed evidence reuse |
| ActivityPub | http://127.0.0.1:5088/ | Mongo2/public Mastodon3, then2/2; native status membership | Pass for completed evidence reuse |

Use `-Federated` for every row. Actor, source scopes and readable catalogues are
server-owned. The Solana host uses independent bounded ingestion; no chain write.
Oracle, AWS, Ethereum, Polygon, Arbitrum, Google, CosmosChain, Polkadot, NEAR, Base and Avalanche are exposed through paired presets only. Plain Solana (schema2), SQLite
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
16lifecycle plans,12paired profiles/24plans,10unsupported-pair rejections and3existing
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

## Ethereum local MVP

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Ethereum -Federated
```

Open http://127.0.0.1:5097/ after startup. Add `-VerifyOnly` for bounded native/SDK/HTTP verification. The all preset returns Ethereum3 + Mongo2 physical rows; subset returns2 +2. Native Ethereum reads use one observed local block number; federation retains provider copies and makes no shared-snapshot/finality/mainnet claim. Original chain/key/seed are reused; shutdown removes only the verified temporary Mongo fixture. [Scope](ETHEREUM_CQL_MVP.md).

## Polygon local MVP

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Polygon -Federated
```

Open http://127.0.0.1:5098/ after startup. Add `-VerifyOnly` for bounded native/SDK/HTTP checks. Concrete PolygonOASIS uses its existing Web3Core contract/encrypted records on local chain31338; this is not Polygon mainnet. All/subset yield5/4 physical rows, with both provider copies preserved. [Scope and evidence](POLYGON_CQL_MVP.md).

Arbitrum V2 uses `-Provider Arbitrum -Federated` (5099), with the same5/4 teaching flow. BootLoader selects the Web3Core variant inV2; no legacy/hosted/rollup/mainnet claim. [Scope](ARBITRUM_CQL_MVP.md).

Google Firestore uses `-Provider Google -Federated` (5100), or add `-VerifyOnly` for bounded checks. Reuses the exact immutable local checkpoint, produces5/4 physical rows and labels emulator scope explicitly. No hosted Google claim. [Guide](FIRESTORE_CQL_MVP.md).

Cosmos **blockchain** uses `-Provider CosmosChain -Federated` (5101), distinct from `-Provider CosmosDB` (Azure emulator). Add `-VerifyOnly` for bounded native/SDK/HTTP checks. The existing encrypted native seed is reused without new submissions, and all/subset yield5/4 physical rows. No Cosmos Hub/mainnet or shared-snapshot claim. [Guide](COSMOS_BLOCKCHAIN_CQL_MVP.md).

## Azure Blob + Mongo MVP

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider AzureBlob -Federated`.
The retained Azurite source supplies three records and Mongo two copies: full query
returns five physical rows; the two-ID subset returns four. C#, HTTP and browser
share HolonManager federation and preserve provider provenance. Reads observe
separate moments. This is local Azurite, not hosted Azure; arbitrary blob discovery,
content predicates and full provider parity remain outside this MVP.
See [commands, tests and limits](AZURE_BLOB_CQL_MVP.md).

## LocalFile + Mongo MVP

`Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider LocalFile -Federated`
serves localhost5108. A bounded authorized ID query returns five physical rows;
the two-ID subset returns four, preserving both provider copies. Native file reads,
process restart reuse and C#/HTTP/browser are verified. Source files are plaintext;
reads have separate observation times. [Tests, commands and limits](LOCALFILE_CQL_MVP.md).

## Aptos + Mongo direct preset

Start `Scripts/TestHosts/Start-CosmicAptosCqlNode.ps1` in terminal1, then
`Scripts/TestHosts/Run-CosmicAptosCqlNative.ps1 -Federated -Serve` in terminal2.
Browser localhost5109 shows full5/subset4 rows with provider provenance. Native,
restart, SDK, HTTP and browser are verified; main-hub enrollment remains pending.
The retained plaintext Move source/key is reused; no faucet or chain reset. Stop
the demo to clean Mongo, then Ctrl-C terminal1. [Tests and scope](APTOS_CQL_MVP.md).

## Sui + Mongo MVP

`Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Sui -Federated` serves localhost5110
and owns its reusable local node/Mongo lifecycle. Full query returns5 physical rows,
two-ID subset4, with provenance. Native/restart/C#/HTTP/browser are verified on the
same dedicated encrypted source. Original deployment/Storage stays untouched; signed
preparations persist. No hosted/finality/snapshot or arbitrary content-query claim.
[Commands, evidence and limits](SUI_CQL_MVP.md).
