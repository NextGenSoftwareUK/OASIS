# TONOASIS Provider

Native TON integration is incomplete. The current C# class inherits the Ethereum
`Web3CoreOASIS` adapter; that is not evidence of TON TVM storage or query support.
Do not use this adapter as a native TON demo or enable it as a verified TON
replication/failover target.

The new `Contracts/TonHolonStore.tact` uses TON's native TVM, with an immutable
owner, keyed cell values and owner-authorized update/delete messages. Its local
TVM tests cover three records, rejected foreign changes, long/Unicode payloads,
missing reads and saved-state reload in a separate process.

Run from the repository worktree:

```powershell
./Scripts/TestHosts/Run-CosmicTonContract.ps1
```

This uses the pinned SDK dependencies in `SdkBridge` and local TVM emulation.
It creates no real account, buys no tokens and contacts no public chain. Restore
the exact package lock once if needed, then reuse it. Generated artifacts and
test evidence go under the worktree's `TestResults/TonCqlCurrent`.

C# native transport, encrypted holon persistence, CQL capability controls,
Mongo federation, HTTP/browser examples and real-network acceptance remain open.
Tact compiler deprecation and production contract review are also tracked.
The current DNA example is disabled; no native endpoint is implied.

See [current proof and remaining gates](../../../Docs/WIP/COSMIC%20ORM/TON_CQL_MVP.md).


