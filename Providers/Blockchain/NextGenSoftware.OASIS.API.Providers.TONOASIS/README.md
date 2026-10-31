# TONOASIS Provider

`TONOASIS` now uses the native TVM backend and mandatory shared private storage.
The former EVM inheritance is removed; its obsolete constructor rejects EVM
configuration. This is an explicit **local TVM emulator** profile, not public TON
network access or certification of HyperDrive replication/failover.

The new `Contracts/TonHolonStore.tact` uses TON's native TVM, with an immutable
owner, keyed cell values and owner-authorized update/delete messages. Its local
TVM tests cover three records, rejected foreign changes, long/Unicode payloads,
missing reads and saved-state reload in a separate process.

Run from the repository worktree:

```powershell
./Scripts/TestHosts/Run-CosmicTonContract.ps1 -Storage
```

This uses the pinned SDK dependencies in `SdkBridge` and local TVM emulation.
It creates no real account, buys no tokens and contacts no public chain. Restore
the exact package lock once if needed, then reuse it. Generated artifacts and
test evidence go under the worktree's `TestResults/TonCqlCurrent`.

The actual adapter passes a three-holon encrypted save/load roundtrip and
independent worker reopen, with one lifecycle owner. CQL supports bounded ID
equality/membership, boolean algebra and paging over an explicitly authorized
catalogue (at most100 IDs). Live controls verify full/subset/missing queries,
budgets/cancellation, read-only rejection and exact fixture cleanup. General
metadata/text/order queries reject before runtime access.

Retained source, Mongo federation and HTTP/browser examples now pass. To launch:

```powershell
./Scripts/TestHosts/Run-CosmicTonCqlNative.ps1 -Federated -Serve
```

Open http://127.0.0.1:5109/ when ready. The stop button removes only the owned
Mongo fixture; original TVM source/key/seed/receipts remain in LocalState.
Signed public-network transport, conditional writes and full ORM/HyperDrive parity
remain open.
Tact compiler deprecation and production contract review are also tracked.
The current DNA example is disabled; no native endpoint is implied.

See [current proof and remaining gates](../../../Docs/WIP/COSMIC%20ORM/TON_CQL_MVP.md).


