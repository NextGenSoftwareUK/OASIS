# Volatile Memcached + Mongo CQL demo

Updated30 October2026. Current native/C#/HTTP/browser integration verified;
persistence, atomic shared indexes and full parity remain incomplete.

## Run and lifecycle

From C:/Source/OASIS-cosmic, PowerShell7 with the retained Ubuntu setup:

```powershell
Scripts/TestHosts/Run-CosmicMemcachedLinuxDemo.ps1 -Federated -Serve
```

Open http://127.0.0.1:5119/, run both buttons, then stop locally and wait for
verified cleanup/retirement. All returns5 copies (cache3+Mongo2); subset4 (2+2).
Omit -Serve for bounded verification. Start one paired demo at a time.

Memcached is volatile. Each explicit demo admits one new empty owned process,
persists its generation intent before writes and warms the same canonical test
identities. Known clean retirement intentionally discards cached data. This is
not persistence recovery. Pending/unknown outcomes refuse replay/adoption/flush.
Previous three retirement receipts are hash-archived before replacing latest state.

## Exact implemented scope

At most100 server-authorized IDs become bounded native GETs on canonical holon
keys. Missing/deleted rows are removed before paging; no keyspace/index scan or
managed predicate substitutes for unsupported content/order/child queries.
Actual binary status1/KEY_ENOENT without an exception means absence. Transport/
server errors remain errors. Native false write acknowledgements fail; saved flags
publish only after existing record/index operations succeed. Multi-command save
and shared-index read/modify/write are not certified atomic across clients.

Plaintext JSON;4MiB post-decode budget. SDK GetAsync has no token, so cancellation
checks occur before/after awaited reads without prompt in-flight interruption.
No distributed snapshot, full history/children/HyperDrive or production-routing claim.

## Current evidence

Raw outputs under C:/Source/OASIS-cosmic/TestResults/MemcachedCqlCurrent/:

| Report | Result | SHA256 |
|---|---|---|
| memcached-cql-current-controls.trx |15passed/0skips; includes real unavailable endpoint |49E501B5AB5E2B751E713FB8727D1A5875DF2B08EC2D6D9D8178E11E85474FAD |
| memcached-cql-native.trx |1passed/0skips |99B3278AA8A70D24CCF3F7A1CD246442490638BE581980C6D42033BA977FE1B4 |
| mongo-init.trx |1passed/0skips; retained replica identity |511E465E29493205762E3F13FFE38CE6DE30541F0A3312FF5BC6EBB3278E6B89 |
| memcached-mongo-federation.trx |1passed/0skips; SDK/HTTP5/4, rejection and cleanup |38EE27E454B2EE8517AE3936F260CED7E3B2CE0A76F621F01F68C32C1E2369DE |

Independent browser5/4 verified: memcached-mongo-subset-current.png. Bounded native
reads confirm all four cache records/index values and CAS versions unchanged during
queries and the complete paired demo; exactly five owned items. Four acknowledged
saves, missing/tombstone handling, paging and budget rejection pass. Original seed
SHA25622B76504DC168E90A67A81184E03CB4DCDDB9B9152DE433079BA78598EB53623 unchanged.

Exact temporary Mongo fixture verified/dropped. Runner exit0; cache and Mongo
daemons exited0,11239/27039/5119 absent. Active/pending/ready files gone, latest
receipt clean-retired and bound to current native report. No source persistence claim.

Public section25 verified: Web4Site maind219b8a464b0932ef0ab930360aaccc2a4eacaf8,
Production7452885165success, volatility/expanded command/fixed5119link visible.
Screenshot memcached-catalogue-public.png SHA2562F1AA244C9C26326C024DBE5CB0575BDD1A20252D9A6C4AAE013BB994E60DE19.
Backend remains task-branch only; no Railway deployment. Hosted selector unchanged.

## Resources and remaining work

Existing Memcached1.6.40/Enyim2.5.1/.NET10.0.12/portable test18.11.0/LinuxMongo8.3.13
reused; no install/download/new environment. Runtime loopback11239/UDPoff/32MiB/
one thread. Native state /var/tmp/oasis-memcached-cql3124B includes1434B archived
retirement evidence. Keep canonical seed and lineage; never replay unknown intent.

Shared-index atomicity, conditional writes, native cancellation, broader queries,
history/children/encryption/production routing and full HyperDrive/parity remain red.

[Preserved history](Archive/MEMCACHED_CQL_MVP_PRESERVED_2026-10-30.md) ·
[Current status](CURRENT_STATUS.md) · [After-demo backlog](AFTER_DEMO_BACKLOG.md) ·
[Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
