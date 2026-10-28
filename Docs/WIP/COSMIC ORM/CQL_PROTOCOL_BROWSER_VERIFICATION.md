# Protocol federation browser verification

28 October 2026. Real retained native providers; controlled loopback teaching host.
Screenshots are unavailable, so these are functional browser AX/control proofs,
not visual layout approval or production authentication/hosting claims.

| Pairing | Full query | Narrowed selection | Protocol-only subset | Native acceptance |
|---|---|---|---|---|
| Mongo + Mastodon | 5(Mongo2/Mastodon3) | 4(Mongo2/Mastodon2) | Previously verified2 | 1pass |
| Mongo + IPFS | 5(Mongo2/IPFS3) | 4(Mongo2/IPFS2) | 2encrypted native records | 1pass |
| Mongo + SOLID | 5(Mongo2/SOLID3) | 4(Mongo2/SOLID2) | 2private native records | 1pass |

All rows expose provider/copy provenance and MovingProviderReads consistency.
The browser selects only server-owned native keys. Per-provider authoritative readable
ID catalogues remain server-owned. Native/HTTP acceptance rejects foreign keys and
forged actor fields. No provider result substitutes for the other backend.

## Run

```powershell
& Scripts/TestHosts/Run-ReusableProtocolMongoTests.ps1 -Provider IPFS -Serve -ArchiveCompletedDemo

& Scripts/TestHosts/Run-ReusableProtocolMongoTests.ps1 -Provider SOLID -Serve -ArchiveCompletedDemo

& Scripts/TestHosts/Run-ReusableProtocolMongoTests.ps1 -Provider ActivityPub -Serve -ArchiveCompletedDemo
```

One host at a time; open http://127.0.0.1:5088/. Use Run Mongo + protocol query,
change the selected keys and run again. Run selected-record query executes only the
protocol target. Result counts depend on which selected keys have matching Mongo rows.
Stop with POST /demo/shutdown to finish fixture cleanup. Omit archive on a first run;
pending/unknown writes must be reconciled, never replayed/reset.

## Lifecycle proof

IPFS and SOLID each completed1native acceptance case with no skips after browser use.
Mongo fixture database independently verified/removed; exact native protocol fixture
cleaned. No Mongo/protocol pending journals or listeners27029/13000/18547–18549/5088
remain. SOLID testExit0/stopExit0/final exited143false (normal SIGTERM,no OOM).
Retained data/keys/identities and immutable IPFS history remain preserved.

Raw TRX/logs: C:/Source/OASIS-cosmic/TestResults/{IPFSCurrent,SolidCurrent,DemoBreadthCurrent}.
Hashes and resource lifecycle: ENVIRONMENT_AND_CLEANUP_LEDGER.md.
No installation/download/restore/new provider environment in this verification step.

Cloud/chain pairings, hosted free Azure, screenshot/layout QA and full ORM/CQL roadmap
remain open. LINQ depth remains paused. These protocol native-key presets do not
claim arbitrary content predicates, general EF parity or distributed snapshots.
