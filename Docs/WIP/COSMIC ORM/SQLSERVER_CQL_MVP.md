# SQL Server + Mongo CQL content demo

Updated30 October2026. Current-branch native/C#/HTTP/browser integration verified;
hosted SQL Server/Azure SQL and full parity remain incomplete.

## Run

From C:/Source/OASIS-cosmic, PowerShell7:

```powershell
Scripts/TestHosts/Run-CosmicSqlServerCqlNative.ps1 -Content -Federated -Serve
```

Open http://127.0.0.1:5115/. Run all four content buttons, stop locally and wait
for cleanup. Omit -Serve for bounded verification. Start one paired demo at a time.

| Preset | Shared predicate | Physical copies |
|---|---|---|
| Exact name | Name == Espresso |2 (1SQLServer+1Mongo) |
| Description Contains | Description.Contains(coffee) |4 (2+2) |
| Metadata equality | category == coffee |4 (2+2) |
| AND / OR | (Espresso OR Tea) AND category == coffee |2 (1+1) |

The page includes spaced C# examples and original ID all5/subset4 presets.
Trusted per-source catalogues stay server-owned; provider copies remain visible.

## Native semantics

One parameterized SELECT applies at most100 authorized IDs, content predicates
and binary UUID paging before materialization. No managed predicate scan.
Name/Description equality/Contains, string metadata equality/nested object paths,
ID equality/membership, Boolean constants and AND/OR. OPENJSON preserves strings
beyond4000 characters. BIN2 plus DATALENGTH preserves trailing-space distinctions;
CHARINDEX treats wildcard characters literally. Metadata parents must be objects
and terminal values strings; missing/null/non-string/array parents do not match.
Numeric-looking object keys work; NUL rejects. Values/keys remain parameters.

Activation/deactivation now owns public provider state; native upsert requires one
acknowledged row before publishing IsSaved/SavedCount. Historical write proof is
preserved below; current integration reuses the acknowledged source without writes.
Plaintext canonical JSON;4MiB post-decode budget, no pre-allocation/snapshot claim.

## Current evidence

Raw outputs under C:/Source/OASIS-cosmic/TestResults/SqlServerCqlCurrent/:

| Report | Result | SHA256 |
|---|---|---|
| sqlserver-cql-current-controls.trx |17passed/0skips |59EABFA98924959F50BBAF66FAFD6E4C3A71FD189BD53F7FCF562B9C8F40FB2C |
| sqlserver-content-native.trx |1passed/0skips;23native cases |2650E2F65421D06C1435D546DBE8857B68F9A590C1A8F3A864BB1458E6A16FD7 |
| sqlserver-content-mongo-federation.trx |1passed/0skips; SDK/HTTP2/4/4/2, identity5/4, rejection and cleanup |56B5FF399E6192BAE45D6AD669C18C161B2272C79DF2461B0F26A756BD0F1400 |

Independent browser presets2/4/4/2 verified: sqlserver-content-browser-current.png.
Original3/content4/long-extension2 rows and timestamps/JSON snapshots unchanged;
all seed/snapshot/ACK/admission fingerprints unchanged. Producer builds before
consumer; successful reports hash-archived and unresolved failures preserved.

Public section21 verified: Web4Site main2d3850c30348f6c2c409a3913ff40b15dc723ad5,
Production7452664788success, expanded command/fixed5115link visible. Screenshot
sqlserver-catalogue-public.png SHA2568453CFF804DD0EB43F68BCD6E7FDC9A59622FB6EFB2C98165741E79F56BE7077.
Backend remains task-branch only; no Railway deployment. Hosted selector unchanged.

## Retention and remaining gates

LocalDB17.0.4025.3/user-private CosmicCql instance reused. LocalState/COSMIC/
SqlServerCql24files33557431B includes both attached databases, seeds, snapshots,
ACKs and admission. Preserve them together; never delete attached database files.
Exact temporary cosmic_cql_mongo_sqlserver_content_demo verified/dropped,
pending/ready absent, runner exit0, LocalDBStopped,27029/5115 stopped.
No install/download/new environment; outputs stay in TestResults/bin/obj.

Hosted tenant authorization/admission/registration, arbitrary catalogues, broader
query features, encryption, conditional writes, HyperDrive and full parity remain
separate red gates. A local catalogue card does not certify those requirements.

[Preserved history](Archive/SQLSERVER_CQL_MVP_PRESERVED_2026-10-30.md) ·
[Current status](CURRENT_STATUS.md) · [After-demo backlog](AFTER_DEMO_BACKLOG.md) ·
[Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
