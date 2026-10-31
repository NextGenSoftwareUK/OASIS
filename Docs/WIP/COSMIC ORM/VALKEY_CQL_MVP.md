# Valkey + Mongo CQL demo

Updated30 October2026. Current independent native/restart/C#/HTTP/browser scope
verified; full provider and ORM parity remain incomplete.

## Run

From C:/Source/OASIS-cosmic, PowerShell7 with the retained Ubuntu setup:

```powershell
Scripts/TestHosts/Run-CosmicValKeyLinuxDemo.ps1 -Federated -Serve
```

Open http://127.0.0.1:5120/, run both buttons, stop locally and wait for cleanup.
All returns5 copies (Valkey3+Mongo2); subset4 (2+2). Omit -Serve for bounded checks.
Start one paired demo at a time. Original encrypted data and protected keys are
reused; pending/changed/missing original source refuses replay or replacement.

## Independent scope

Valkey binds its own CQL authority while reusing the existing encrypted Redis-wire
transport executor. Mutable provider identity alone cannot grant capabilities to
another facade. Actual native INFO proves Valkey9.0.4 independently of Redis.

At most100 server-authorized IDs support equality/membership, Boolean algebra,
missing/deleted exclusion and paging after exclusion. Native canonical-key reads;
no keyspace scan or managed content predicate. Result and4MiB post-decode budgets
apply, without a pre-allocation bound or distributed snapshot claim.

## Current evidence

Raw outputs under C:/Source/OASIS-cosmic/TestResults/ValKeyCqlCurrent/:

| Report | Result | SHA256 |
|---|---|---|
| valkey-cql-current-controls.trx |8passed/0skips |ACC8246FB0FD59C0ED2E5F07042A9D1F3CCE552BCF5DD8796742CB23B679E13E |
| valkey-cql-native.trx |1passed/0skips |17679599A9007BCBCE39AAE501EC31233C09E0D06B2CA70633E07E4321F0B949 |
| valkey-cql-restart.trx |1passed/0skips |660922227FEB3D8E2458DCFF3700A8A7BE8C7B22849C79824C7BE2B8BBBF95D4 |
| mongo-init.trx |1passed/0skips; retained replica identity |C561429DBE825445A2149FCA780D5B03D26499B995568338F8337CF2F3593A61 |
| valkey-mongo-federation.trx |1passed/0skips; SDK/HTTP5/4, rejection and cleanup |0BEFF04F0CED20EEFF7378408D6B5B25EB9C349D8CE1F71BA4207027695AEEEA |

Independent browser5/4 verified: valkey-mongo-subset-current.png. Three live/one
deleted records and encrypted DUMP snapshot unchanged through native queries,
clean restart and paired demo. Seed/configuration/key/ACK fingerprints unchanged.
Seed SHA2562A4F8D4807DC41B21318E911EC24A40C227818EC260BC9E1DFBCBF1F0D246730.
Existing Redis green controls were not rerun: no Redis/shared executor change.

Exact temporary Mongo fixture verified/dropped, pending/active/ready absent,
runner exit0 and both daemon shutdowns clean;16389/27039/5120 absent. Original
source retained; successful reports and previous verification receipt hash-archived.

Public section26 verified: Web4Site main54d1a58748623b98626431c26f08767fa9c31b9a,
Production7452952934success, expanded command/fixed5120link and independent scope
visible. Screenshot valkey-catalogue-public.png SHA2565FC777A2EA46AA9BAC86BDDDC8B47A2886B8D822D5ED81235E54A027ACB65903.
Backend remains task-branch only; no Railway deployment. Hosted selector unchanged.

## Resources and remaining gates

Original Valkey9.0.4/runtime/admitted source /var/tmp/oasis-valkey-cql retained:
10095B including471B archived prior verification. Loopback16389/DB0/AOFalways,
protected key/password/config0600 preserved with data. Existing LinuxMongo8.3.13,
.NET10.0.12/portable test18.11.0/NuGet reused; no download/install/new environment.

General metadata/content/children/order queries, conditional writes/history,
cluster failover/replication, public hosting, central factory enrollment and full
HyperDrive/ORM parity remain separate red gates. Protocol compatibility does not
certify another provider or any of these broader requirements.

[Preserved history](Archive/VALKEY_CQL_MVP_PRESERVED_2026-10-30.md) ·
[Current status](CURRENT_STATUS.md) · [After-demo backlog](AFTER_DEMO_BACKLOG.md) ·
[Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
