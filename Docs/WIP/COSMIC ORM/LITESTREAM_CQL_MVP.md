# Linux Litestream + Mongo CQL demo

Updated30 October2026. Current supported-Linux integration verified. Windows
sidecar durability failure, broader queries and full parity remain incomplete.

## Run

From C:/Source/OASIS-cosmic, PowerShell7 with the retained Ubuntu setup:

```powershell
Scripts/TestHosts/Run-CosmicLitestreamLinuxDemo.ps1 -Federated -Serve
```

Open http://127.0.0.1:5118/, run both buttons, stop locally and wait for cleanup.
All returns5 physical rows (Litestream3+Mongo2); subset returns4 (2+2).
Omit -Serve for bounded verification. Start one paired demo at a time.

## Exact scope

The actual CQL adapter reads the existing SQLite source using parameterized native
ID selection/tombstone filtering/UUID paging over at most100 authorized IDs.
No managed predicate/source scan or simulated replication. Unsupported metadata,
content, children and arbitrary ordering reject. JSON is plaintext;4MiB is a
post-decode budget, not a native allocation bound. Native interruption, distributed
snapshots and complete HyperDrive/parity acceptance remain separate gates.

The earlier actual Linux sidecar/live-LTX/restore proof is retained. This integration
reuses its completed receipt, verifies source and restored-file queries, and does
not replay the live probe or overwrite the restored file. A fresh sidecar/PITR/crash
test is not claimed. Source/replica/restored file and receipts22fingerprints unchanged.

## Current evidence

Raw outputs under C:/Source/OASIS-cosmic/TestResults/LitestreamCqlCurrent/:

| Report | Result | SHA256 |
|---|---|---|
| litestream-cql-current-controls.trx |14passed/0skips |551DDEB56D1CC0ABF313587855EC1624AA19AF588147A1613287BBC14AE74B0D |
| litestream-linux-native.trx |1passed/0skips |6C4B310F6668BD8799454C1C626782691C60B758EF8BD633C1BA0AFFF7385723 |
| litestream-linux-restore.trx |1passed/0skips |E467135ABD5A0EF178D26596422EDC0FD2D5402E174987D54164917A40C2E01F |
| linux-mongo-init.trx |1passed/0skips; retained replica identity |8BA92C496FD4DD1E4245966B60D7D43D49E5226F3183306F8675C6DDA9646C99 |
| litestream-mongo-federation.trx |1passed/0skips; SDK/HTTP5/4, rejection and cleanup |7E5CD213DE32079B417D7F0C5891BF20AF3649E60C773FA572133D3F7F454EDB |

Independent browser5/4 verified: litestream-mongo-subset-current.png. Exact owned
Mongo fixture verified/dropped, pending/ready absent, runner exit0. Owned Linux
Mongo SIGTERM/wait exit0;27039/5118 absent. No sidecar started during receipt reuse.
Producer/consumer builds use cached dependencies; successful reports hash-archived,
unresolved failures preserved. No download/install/new environment.

Public section24 verified: Web4Site main3ac9f836d8074afb472ee436a5a94515aecc24af,
Production7452803356success, expanded Linux command/fixed5118link visible.
Screenshot litestream-catalogue-public.png SHA2562FB2704E5BF6C2A6E49AEF316C0A19A1949FCBA19C0AC3ECDB137AFF9A7FB151.
Backend remains task-branch only; no Railway deployment. Hosted selector unchanged.

## Retention and remaining gates

Original Linux /var/tmp/oasis-litestream-cql172398B retained. Reuse .NET10.0.12,
Litestream0.5.17, portable test runner18.11.0 and Mongo8.3.13; Linux Mongo store
/var/tmp/oasis-cosmic-mongo264226275B retained. Loopback-only, no firewall widening.

Windows LocalState/COSMIC/LitestreamCql/replication.pending.json remains unresolved;
source/replica/probe/restore unchanged. Native daemon/restore directory-sync failures
remain red. Original failure logs are hash-verified and preserved outside cleanup
targets under failure-evidence/ (3311B total), not just disposable TestResults.
Never replay/reseed the uncertain Windows probe to regenerate evidence.

Original successful Linux live-probe TRX/replication/restore logs are also preserved
under LocalState/COSMIC/LitestreamCql/linux-evidence/ (4292B), outside cleanup targets.
The original probe hash matches the historical report; no probe was repeated.

Remote replicas/PITR/crash recovery, Windows durability, production registration,
conditional writes/children/history/encryption and full HyperDrive/ORM parity remain
separate red work. Linux success does not certify those requirements.

[Preserved history](Archive/LITESTREAM_CQL_MVP_PRESERVED_2026-10-30.md) ·
[Current status](CURRENT_STATUS.md) · [After-demo backlog](AFTER_DEMO_BACKLOG.md) ·
[Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
