# PostgreSQL + Mongo CQL content demo

Updated30 October2026. Current-branch native/C#/HTTP/browser integration verified;
hosted PostgreSQL and full parity remain incomplete.

## Run

From C:/Source/OASIS-cosmic, PowerShell7:

```powershell
Scripts/TestHosts/Run-CosmicPostgreSqlCqlNative.ps1 -Content -Federated -Serve
```

Open http://127.0.0.1:5114/. Run four content buttons, then stop locally and wait
for cleanup. Omit -Serve for bounded verification. Start one paired demo at a time.

| Preset | Predicate | Physical copies |
|---|---|---|
| Exact name | Name == Espresso |2 (1PostgreSQL+1Mongo) |
| Description Contains | Description.Contains(coffee) |4 (2+2) |
| Metadata equality | category == coffee |4 (2+2) |
| AND / OR | (Espresso OR Tea) AND category == coffee |2 (1+1) |

The page includes spaced C# examples and original ID all5/subset4 presets.
Per-source authorization catalogues stay server-owned; provider copies remain visible.

## Native scope

One parameterized SELECT applies at most100 authorized IDs, business predicates
and UUID Skip/Take before materialization. No managed predicate filtering.
Name/Description equality and Contains; string metadata equality/nested object
paths; ID equality/membership; Boolean constants and AND/OR. C collation gives
case-sensitive equality; strpos treats wildcard characters literally. Metadata
requires object parents and a terminal string. Missing/null/wrong-type/array-parent
values do not match. Numeric-looking object keys work; NUL rejects. Values and
path segments are parameters. Aliases require independent capability admission.

Storage is plaintext canonical JSON.4MiB is a post-decode budget, not a native
allocation bound. No cross-provider snapshot, hosted-service or full parity claim.

## Current evidence

Raw outputs under C:/Source/OASIS-cosmic/TestResults/PostgreSqlCqlCurrent/:

| Report | Result | SHA256 |
|---|---|---|
| postgres-cql-current-controls.trx |16passed/0skips |8D082F951974D6DB5C200A902DC6B8969A8D6E9D0FAFEE4D5EF8A989ED88F00A |
| postgres-content-native.trx |1passed/0skips;17native cases |9B930C32E6D7D4170B38919D9FE9FF61E382F29ED1F9C26037CF2690B1C40525 |
| postgres-content-mongo-federation.trx |1passed/0skips; SDK/HTTP2/4/4/2, identity5/4, rejection and cleanup |36536E68EB0ACEDDCC0E1F498CF4DB6D8A6C4994060D35D85B88F2B7BD5E0FDC |

Independent browser presets2/4/4/2 verified: postgres-content-browser-current.png.
Both original and content-schema JSON hashes/xmin versions remained unchanged;
seed/snapshot/all ACK fingerprints unchanged. Fourth matching record excluded by
catalogue even through OR. Successful reports hash-archived; unresolved failures
stop another run. Producer builds before consumer; no source reset/reseed/write.

Public section20 verified: Web4Site main39333e3488eea265ea2d35a4fce9d90641f1a4a4,
Production7452616850success; expanded command/fixed5114link visible. Screenshot
postgres-catalogue-public.png SHA256DB7BB7042752A620953C9F477403B59CA2617BFAC4064D1D61814A0D86DED8E7.
Backend remains task-branch only; no Railway deployment. Hosted selector unchanged.

## Retention and remaining work

Original PostgreSQL containercee58d44cc44aace8863ca53ffa955deed78a710a9800b750a484bb478206a69
and volumeoasis-postgres-data retained, both schemas preserved. LocalState/COSMIC/
PostgreSqlCql15files1605B holds original/content seeds, snapshots, ACKs and locks.
Exact temporary cosmic_cql_mongo_postgres_content_demo verified/dropped, pending/
ready absent, runner exit0, PostgreSQL exited,15432/27029/5114 stopped.
No install/download/new environment; generated outputs stay in TestResults/bin/obj.

Hosted tenant authorization/admission/registration remain unverified: the local
catalogue card does not supply them. Never replace trusted catalogues with caller IDs.
Broader ordering/numeric/null/NOT/graph/conditional-write/encryption/HyperDrive/parity
acceptance remains separate red work.

[Preserved history](Archive/POSTGRESQL_CQL_MVP_PRESERVED_2026-10-30.md) ·
[Current status](CURRENT_STATUS.md) · [After-demo backlog](AFTER_DEMO_BACKLOG.md) ·
[Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
