# ONODE host profiles and provider hosting

Date: 26-27 November 2026. Direction set by David.

Running the current Railway setup: [Railway deployment guide](RAILWAY_DEPLOYMENT.md).

**Status: PROPOSED.** Host profiles and per-provider connection modes are not built yet. Do not add these keys to a live DNA; no code reads them. On-demand sidecars are the one part that exists (see below).

## Why

Many ONODEs (WEB4 to WEB10) will run on different hosts: Railway, Holo, ThreeFold, Akash, Flux, Fly.io or the Oracle free tier, Raspberry Pis and home servers. Hosts differ in memory, processor type and what they allow a container to run. Each node must only start what its host supports, and providers should be hosted wherever it is cheapest (free tiers first) instead of all on Railway.

## Proposed DNA (not built)

`ONODE.HostProfile` describes the machine the node runs on:

| Key | Meaning |
|---|---|
| `Name` | Label, for example `Railway`, `Holo`, `RaspberryPi` |
| `CanRunSidecars` | Whether the host can run local helper processes |
| `AllowedSidecars` | Which sidecars may run on this host (list) |
| `MaxMemoryMB` | Memory the node must stay within |
| `Architecture` | `x64` or `arm64` |

Each provider section gets a connection mode:

| `ConnectionMode` | Meaning | Address key |
|---|---|---|
| `External` | A hosted service | `ConnectionString` |
| `Local` | A service on the same machine or private network, for example a Railway private-network address | `LocalAddress` |
| `Sidecar` | A helper process the node starts itself | none |
| `Disabled` | Not used on this node | none |

Illustration only:

```json
"ONODE": {
  "HostProfile": { "Name": "RaspberryPi", "CanRunSidecars": false, "AllowedSidecars": [], "MaxMemoryMB": 1024, "Architecture": "arm64" }
}
```

```json
"MongoDBOASIS": { "ConnectionMode": "External", "ConnectionString": "<from a secret>" },
"SQLLiteDBOASIS": { "ConnectionMode": "Local", "LocalAddress": "<local database file>" },
"Neo4jOASIS": { "ConnectionMode": "Disabled" }
```

### The rule

A node activates a provider only if its mode works on this host. Unsupported providers are skipped with a clear log line. HyperDrive routes, replicates and fails over only to the providers the node really has. This is a stated configuration rule, not a fallback path: a provider that is configured and supported but fails must still report a real error.

## Where to host providers cheaply

Free-tier limits change; check them before relying on one.

| Need | Option |
|---|---|
| Graph (Neo4j) | Neo4j AuraDB Free |
| Documents | MongoDB Atlas Flex (in use) |
| SQL and files | SQLite and LocalFile, local to the node |
| Redis-like | Upstash |
| IPFS | Public gateways plus a free pinning tier |
| Blockchains | Public or testnet RPC endpoints |
| Holochain | Holo |

Peer-to-peer networks that fit a decentralised ONODE network: IPFS, Holochain, Nostr, ThreeFold, Ceramic and OrbitDB.

The shared Atlas cluster is not a test backend (see `AGENTS.md`).

## On-demand sidecars (built, on Development)

WEB4's local helper processes, which serve the CQL sandbox sources, can start only when a query needs them and stop after an idle period. Development measured WEB4 memory falling from 1,481 MB to 974 MB.

DNA: `OASIS.Sandbox.Sidecars`: `OnDemand` (default `false`), `IdleMinutes` (default 10, minimum 2), `AlwaysOn` (comma-separated source providers, for example `SOLIDOASIS, GUNOASIS`).

- A running query holds its sidecar, so it is not stopped mid-request.
- Start-up launches no sidecars (except `AlwaysOn`); a source is activated by its first query.
- Log lines start with `SIDECAR`. Status, Wizard only: `GET /api/admin/cql/sidecars`.
- Not covered: Holochain (WEB4 starts it itself) and a CI test with a real sidecar.

## Plan

1. On-demand sidecars: built on Development; rollout to be completed and verified.
2. `HostProfile` and `ConnectionMode` in the DNA, with typed C# defaults and every `OASIS_DNA` copy (and Railway `OASIS_DNA_JSON`) synced.
3. Move stateful services off Railway to free tiers, so Railway runs only the thin APIs.
4. A node-profile setup guide with sample DNA for Railway, Holo, ThreeFold, Akash and a Raspberry Pi. Not every provider or sidecar has been checked on arm64.

## Current state (26-27 November 2026)

- Railway runs WEB4 to WEB10 in production, development and staging.
- Neo4j's Railway deployments are stopped (volume kept). `Neo4jOASIS` stays in the HyperDrive lists, so fail-over routes around it.
- All WEB services have `DOTNET_gcServer=0` and `DOTNET_GCConserveMemory=5`, app sleeping on, Watch Paths `["**","!Docs/**","!**/*.md","!.github/**"]`, and memory limits: WEB4 3 GB, WEB5 and WEB6 1 GB, WEB7 to WEB10 0.5 GB.
- Production memory fell from about 4.6 GB to about 2.5 GB.
