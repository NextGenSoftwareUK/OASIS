# Interactive IPFS and SOLID CQL point demo

27 October2026. Loopback teaching host over an explicitly owned synthetic fixture.
This uses the real native backend, shared HolonManager/LINQ/planner/adapter.
It is separate from the production WEB4 controller's authentication/billing path.

## Launch

From `C:/Source/OASIS-cosmic`, start one provider at a time. Cached environments
already exist. No reinstall/download is needed. First launch has completed native
acceptance evidence, so explicitly archive that completed fixture before serving:

```powershell
& Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner cosmic-ipfs-demo -Operation {
    & Scripts/TestHosts/Run-ReusableIpfsTests.ps1 `
        -CqlInteractive -ArchiveCompletedDemo -ServePointDemo
}
```

For the private SOLID Pod:

```powershell
& Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner cosmic-solid-demo -Operation {
    & Scripts/TestHosts/Run-ReusableSolidReadiness.ps1 `
        -CqlInteractive -ArchiveCompletedDemo -ServePointDemo -RequireCleanShutdown
}
```

Open `http://127.0.0.1:5088/` after it is listening. Click **Run native CQL query**.
The page clearly labels the selected provider, native key and point-only scope,
then shows the canonical record identity/name/source. Actor/provider/fixture key
are fixed by the server; another key or caller-supplied actor rejects.

Stop gracefully from another PowerShell terminal:

```powershell
Invoke-RestMethod -NoProxy -Method Post http://127.0.0.1:5088/demo/shutdown
```

The runner then conditionally cleans its exact fixture and stops its backend.
If the run is interrupted or fails, preserve its pending journal and do not replay.
The archive option requires a completed closure, no pending intent and stopped
listeners, then hash-moves exact prior evidence into Archive. It deletes no keys,
Pod data or immutable IPFS blocks. It is restricted to this worktree's default
persistent environment. Existing incomplete state intentionally prevents relaunch.
Raw reports/logs live under worktree TestResults; ownership/closure stays in
LocalState/COSMIC/{IPFSCurrent,SolidCurrent}.

## What is verified

IPFS and SOLID automatic native interactive tests each pass1/1. Each checks the
HTML page, valid native row, wrong-key400 and forged-actor400. SOLID also exercises
the graceful shutdown route. IPFS explicit leave-running mode was separately
started on5088, queried through HTTP, stopped via POST/demo/shutdown, and completed
verified fixture cleanup. Both services stopped normally. Main demo project builds;
PowerShell runner/archive parsing passes. Browser rendering/JavaScript click was
not automated; page and API were verified through HTTP.

`Examples/CosmicCqlDemo/ProtocolPointApiHost.cs` is the shared spaced/commented host.
The C# usage walkthroughs remain IpfsPointWalkthrough.cs and SolidPointWalkthrough.cs.
The existing SQLite coffee console/Swagger entry point is unchanged.

## Remaining scope

This proves a point-read teaching UI, not the complete eight-provider investor demo.
No arbitrary metadata filtering, ordering, scalar/projection or query index is
claimed. IPFS uses its authenticated current catalogue; SOLID uses one encrypted
LDP resource. Production WEB4 HTTP tests separately prove rows/query receipts with
controlled identity/outbox. Real JWT, durable billing, hosted-cloud acceptance,
chain/index integration and full parity remain open. ActivityPub interactive-host
wiring and a combined demo entry point remain open.

| Evidence relative to worktree | SHA256 |
|---|---|
| TestResults/IPFSCurrent/ipfs-cql-interactive-native.trx | D7F8E5A03A772E965521AB78C4C3CCEF8C74E1A39CDA822AA04B311F29FE9C33 |
| TestResults/SolidCurrent/solid-cql-interactive-native.trx | 3434E7F36CEF120B46BB4484B78574DE57FE1797B739A7A086663D88198C98DC |
| TestResults/SolidCurrent/solid-cql-interactive-lifecycle.json | 1D8A23CC9F19F41F8F6C5A700EB2EE1706DF14CA166D3AD6D1786F87830275E3 |
