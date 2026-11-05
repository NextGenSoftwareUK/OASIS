# SQL Server + Mongo CQL content demo

## Source recovery — 2 November 2026

Named source/launcher restored from retained pin237403a75. Current controlled
tests pass (17), producer/consumer compilation and launcher syntax pass.
Original native data, credentials and accepted fixtures were not opened/replayed.
Native/browser acceptance below is historical, not a new run; runtime availability
was not reverified. Hosted SqlServer is not certified by this recovery.


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

## Recovery laptop verification and Development gate

The retained synthetic LocalDB source now passes native identity/restart,
native content and Mongo-paired SDK/HTTP tests on the recovery laptop. Read-only
activation checks existing table access and column types without provisioning.
Cancellation and source fingerprint preservation passed in the native suite.

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider SQLServer -Federated -RecoveryMongo -VerifyOnly
```

For Development, use an explicitly configured SQL Server query source with an
already provisioned synthetic dataset, a reachable endpoint and a SELECT-only
credential. LocalDB on this laptop is local evidence. WEB4 needs an opt-in
CqlReadSourceProfile for SQLServerDBOASIS and public catalogue grants limited
to the admitted IDs. Update both the API CQL compiler's hosted-source admission
and Core PreparedStorageExecution's explicit query-tariff allowlist. Pin the
reviewed Core commit with the matching Railway manifest. Verify no enrollment
without configuration and no storage write tariff for the query-only profile.
Then verify discovery, native and Mongo-paired presets, isolation and replicas
through the live Development service. LocalFile remains the first rollout.

Core admission candidate 2ef15c3d7ad3c68c97d3b0dcd32ee24c2428b33a is on local
Core branch work/cosmic-sqlserver-admission, based on parent pin 47c3a841.
It adds SQL Server to the explicit query-tariff allowlist. The shared Core
checkout was untouched. Build and verify query-only admission before publishing
the Core commit or advancing the parent manifest and gitlink.

WEB4 opt-in registration, published Core admission and the parent pin are
ready on the task branch. The four canonical verifier presets passed the
focused admission test with no skips. Receipt:
TestResults/SqlServerCqlCurrent/web4-preset-names.log. Reused existing test
outputs and started no services. The hosted source is still pending.

### Development configuration review

The rollout uses four settings. SQL Server provisioning is separate from WEB4
configuration. Seed and qualify the three teaching records on the reachable
database before enrolling their IDs.

| Setting | Required value |
|---|---|
| CQL_SQLSERVER_QUERY_CONNECTION_STRING | Secret connection string for the qualified SELECT-only login |
| PREPARED_STORAGE_ADMISSION_JSON | Preserve existing entries and add SQLServerDBOASIS as query-only with an explicit zero-cost read tariff |
| CQL_PUBLIC_READ_CATALOGUES_JSON | Preserve existing catalogues and add exactly the three qualified GUIDs for SQLServerDBOASIS |
| CQL_HOSTED_QUERY_EXAMPLES__SQLServerDBOASIS | public-records, public-subset, mongo-public-records, mongo-public-subset |

Use the tested identity predicates and preserve physical copies in paired
queries. Verify the exact reviewed Development release and settings, then run
the cohort-free check before any snapshot preparation:

```powershell
python Scripts/Sandbox/Verify-HostedQuerySource.py SQLServerDBOASIS --readiness
```
