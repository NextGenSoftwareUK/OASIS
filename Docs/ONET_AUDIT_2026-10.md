# ONET Audit — October 2026

Scope: `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/ONET/*` (47 files, ~21.5k lines), `Managers/ONETManager.cs`,
`ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/ONETController.cs`, and `OASIS Architecture/NextGenSoftware.OASIS.ONET`.

Status column is updated as fixes land on `Development`.

## Critical

| # | Issue | Location | Status |
|---|-------|----------|--------|
| 1 | Bootstrap discovery could not work end to end: client GET `{server}/onet/nodes` (no such route), no signature headers, server endpoint behind `[Authorize(Wizard)]`, response wrapped in `OASISResult` while the client expected a bare array. | `ONETDiscovery.Helpers.cs`, `ONETController.cs` | **Fixed (bootstrap path)** — new anonymous, signed `GET /api/v1/onet/peers` returning `NodeInfo[]`; client sends `X-ONET-NodeId/Timestamp/Signature`; `nodes/register` is anonymous. Peer-to-peer exchange against a peer's TCP address is still HTTP-to-TCP — see #22. |
| 18 | **Platform-wide:** GraphQL (`/graphql`) and gRPC services have no caller authentication. Every mutation is open, e.g. `DeleteAvatarByEmail`, `SendSolanaTransaction`, `MintWeb4Nft`, `AddKarmaToAvatar`, `StopNetwork`. | `ONODE.WebAPI/GraphQL/Query.cs`, `Mutation.cs`, `GrpcServices/*`, `Startup.cs` | **Fixed (ONODE WebAPI)** — global HotChocolate field middleware and gRPC interceptor (`ONODE.WebAPI/Security/`) require a Wizard avatar except the login/registration allowlist; open individual operations once their resolver checks ownership. STAR (WEB5) and WEB6–WEB10 GraphQL/gRPC already require an authenticated WEB4 bearer via the usage ledger, but their resolvers have not been audited for ownership checks. |

## High — security

| # | Issue | Location | Status |
|---|-------|----------|--------|
| 2 | Identity takeover: `nodes/register` did not require `NodeId == SHA256(PublicKey)` and silently overwrote keys; gossip registered keys the same way. | `ONETSecurity`, `ONETController.RegisterNode`, discovery | **Fixed** — `RegisterNodePublicKey` only accepts a key whose SHA-256 is the node id (invalid keys rejected); discovery drops mismatched peers; binding an address requires a fresh signature over `POST /api/v1/onet/nodes/register {address}`. |
| 3 | Replayable auth: PING and HTTP peer exchange signed constant strings. | `ONETProtocol.Network.cs`, `ONETRouting.Optimize.cs`, `ONETController` | **Fixed** — `purpose\|nodeId\|unixSeconds` signatures, ±60s window, single-use (`ONETSecurity.Freshness.cs`). PING: `ONET_PING <nodeId> <unixSeconds> <sig>`. |
| 4 | Signed application frames had no timestamp/seen-id; captured requests could be replayed. | `ONETTcpApplicationMessageChannel` | **Fixed** — signed `IssuedUnixSeconds`; frames accepted once within the window. |
| 5 | `GET /api/v1/onet/oasisdna` returned the full DNA (all provider secrets, node private key); `PUT` replaced the whole DNA. Same over GraphQL and gRPC. | `ONETController`, GraphQL, `ONETGrpcService` | **Fixed** — replaced by Wizard-only `GET/PUT /api/v1/onet/config` (ONET section only, secrets blanked, identity immutable). GraphQL fields removed; gRPC methods return `PermissionDenied`. `ONETManager.UpdateOASISDNAAsync` deleted. |
| 6 | Node private key stored in plaintext Holon metadata. | `ONETManager.SaveStateToHolonAsync` | **Fixed** — identity stored only as AES-GCM sealed blob under `OASIS_ONET_STATE_KEY` (instance id as AAD); validated on load; never overrides a DNA keypair. |
| 7 | TCP listener had no read timeout or connection cap; any text was `ONET_ACK`ed. | `ONETProtocol.Network.cs` | **Fixed** — 10s read timeout, 256 concurrent connections, unknown text gets `ONET_UNSUPPORTED`. Plain `ONET_PING` stays an unauthenticated liveness probe. |
| 19 | `ONODEController` `GET/PUT api/v1/onode/oasisdna` (Wizard) returned / replaced the full DNA including secrets; `PUT` never persisted. | `ONODEController`, `ONODEManager` | **Fixed** — endpoints, manager methods, MCP tools `web4_onode_get/update_oasisdna` and WebUI client methods removed. |

## High — correctness

| # | Issue | Location | Status |
|---|-------|----------|--------|
| 8 | Holon persistence keyed on `Environment.MachineName` (random per Railway deploy); identity regenerated every restart. | `ONETManager` | **Fixed** — keyed on `ONET.InstanceId`, falling back to `RAILWAY_SERVICE_ID`; skipped (with warning) when neither is set. Set `OASIS_ONET_STATE_KEY` (base64, 32 bytes) to persist the identity. |
| 9 | Plain `Dictionary` written concurrently with no locking. | multiple | **Partial** — `ONETSecurity` key/session maps now `ConcurrentDictionary`. Open: `ONETProtocol._connectedNodes`, discovery tables. |
| 10 | Three independent peer lists can disagree. | `ONETManager`, `ONETProtocol`, `InternalP2PNetworkProvider` | Open |
| 11 | Controllers cached a faulted manager initialization task forever. | `ONETController`, `ONODEController` | **Fixed** — faulted/cancelled tasks are re-initialised on the next request. |
| 17 | Legacy `BroadcastMessageAsync` sends unsigned `type\|source\|target\|content` text that no receiver processes, and reported success regardless of the reply. | `ONETProtocol.Messaging.cs` | **Partial** — sender now fails unless `ONET_ACK`; listener answers `ONET_UNSUPPORTED`, so broadcast now reports failure honestly. Open: port broadcast to the signed application channel. |
| 22 | Peer exchange with already-known peers issues HTTP to the peer's ONET TCP address, which serves no HTTP. | `ONETDiscovery.Helpers.cs` (`QueryNodeForPeersAsync`) | Open |

## Medium

| # | Issue | Location | Status |
|---|-------|----------|--------|
| 12 | Stats metrics are misleading (ICMP latency, in-memory "throughput"); two empty `catch { }`. | `ONETManager.GetNetworkStatsAsync`, `ONETProtocol.Metrics.cs` | Open |
| 13 | Consensus `_pendingProposals` / `_votes` never pruned; quorum counts inactive nodes. | `ONETConsensus.cs` | Open |
| 14 | `UseTestDataWhenLiveDataNotAvailable` returns success with `Result=null` on real failures. | `ONETController`, `ONODEController` | **Fixed for ONET** (endpoint replaced). Open in `ONODEController`. |
| 15 | ~6,400 lines (~30%) unreachable from production code. | see list in previous revision | Open |
| 16 | API key compared with `!=`; frame signature verified over re-serialized JSON. | `ONETController`, channel | **Partial** — API key now constant-time. |
| 20 | GraphQL resolvers and gRPC methods construct a new `ONETManager` per call (`Task.Run(...).Result` sync-over-async), so they never see the running network. | `GraphQL/*`, `ONETGrpcService` | Open |
| 21 | Silent `catch { }` when loading the peer file cache. | `ONETManager` | Open |
| 23 | Custom `AuthorizeAttribute` ignored `[AllowAnonymous]`. | `ONODE.WebAPI/Helpers/AuthorizeAttribute.cs` | **Fixed** — honours `IAllowAnonymous`. |

## Fixed earlier in this audit cycle

- Consensus events (`ConsensusReached`/`ConsensusFailed`) were declared but never raised.
- Leader never rotated; now heartbeat-based (`RecordHeartbeatAsync`, `HeartbeatTimeout`). **Callers must send heartbeats** — nothing in the peer keep-alive path calls `RecordHeartbeatAsync` yet.
- `FindOptimalRouteAsync` searched from the target to itself and returned empty routes; now returns the direct hop and errors for unknown/inactive targets.
- `GetRoutingStatsAsync` threw on an empty table.

## Deployment notes for the fixes above

- New DNA fields: `ONET.AdvertisedAddress` (public `host:port` of the TCP listener, sent on registration) and `ONET.InstanceId`.
- New env var: `OASIS_ONET_STATE_KEY` — base64 32-byte key; without it the node identity is not persisted through the provider.
- Wire changes: PING and peer-exchange signatures changed format; nodes on older builds will fail authentication against updated ones (plain `ONET_PING` liveness still works).
- Holon state ids changed (MD5 → SHA-256 of the instance id); previous state Holons are not read.

## Remediation order (remaining)

1. Ownership checks so GraphQL/gRPC operations can be opened beyond Wizards (#18 follow-up).
2. #9–#10 concurrency and single peer list; #22 peer-to-peer exchange.
3. #12–#13, #17, #20–#21.
4. #15 dead code removal.
