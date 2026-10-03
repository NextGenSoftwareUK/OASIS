# ONET Audit — October 2026

Scope: `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/ONET/*` (47 files, ~21.5k lines), `Managers/ONETManager.cs`,
`ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/ONETController.cs`, and `OASIS Architecture/NextGenSoftware.OASIS.ONET`.

Status column is updated as fixes land on `Development`.

## Critical

| # | Issue | Location | Status |
|---|-------|----------|--------|
| 1 | Bootstrap discovery cannot work end to end: client GETs `{server}/onet/nodes` (no such route; server is `/api/v1/onet/network/nodes`), client sends no `X-ONET-*` signature headers, and the whole controller is `[Authorize(AvatarType.Wizard)]` so community nodes get 401 on both peer exchange and `POST nodes/register`. | `ONETDiscovery.Helpers.cs:394`, `ONETController.cs` | Open |

## High — security

| # | Issue | Location | Status |
|---|-------|----------|--------|
| 2 | Identity takeover: `nodes/register` does not require `NodeId == SHA256(PublicKey)` and silently overwrites existing keys. Registration is open when `ONETApiKey` is empty (default). Peer-exchange gossip registers keys the same way. | `ONETSecurity.Part2.cs` `RegisterNodePublicKey`, `ONETController.RegisterNode` | Open |
| 3 | Replayable auth: PING signs the constant `"ONET_PING"`; HTTP peer exchange signs the constant `"GET /onet/network/nodes"`. One captured signature authenticates forever. | `ONETProtocol.Network.cs`, `ONETRouting.Optimize.cs`, `ONETController.GetConnectedNodes` | Open |
| 4 | Signed application frames carry no timestamp/expiry/seen-id, so captured requests (e.g. HyperDrive sync writes) can be replayed. | `ONETTcpApplicationMessageChannel.ReceiveApplicationFrameAsync` | Open |
| 5 | `GET /api/v1/onet/oasisdna` returns the full DNA including every provider secret and `ONET.NodePrivateKey`; `PUT` replaces the whole DNA. | `ONETController`, `ONETManager.GetOASISDNAAsync` | Open |
| 6 | Node private key persisted in plaintext Holon metadata; Holon id is derived from the public key, so a provider-level write to that id swaps the node identity on next start. | `ONETManager.SaveStateToHolonAsync` | Open |
| 7 | TCP listener: no read timeout or connection cap (idle/slow clients exhaust it); unauthenticated `ONET_PING` accepted; any other text is `ONET_ACK`ed though nothing processes it. | `ONETProtocol.Network.cs` `HandlePingConnectionAsync` | Open |

## High — correctness

| # | Issue | Location | Status |
|---|-------|----------|--------|
| 8 | Holon persistence never survives a Railway restart: on a fresh container `NodePublicKey` is blank, so the lookup key falls back to `Environment.MachineName`, which is random per deploy. A new identity is generated every restart. | `ONETManager` `Save/LoadStateFromHolonAsync` | Open |
| 9 | Plain `Dictionary` written concurrently (HTTP requests, TCP handlers, background loops) with no locking: `ONETSecurity._nodeKeys/_activeSessions`, `ONETProtocol._connectedNodes`, discovery tables, `ONETConsensus` (partially locked). | multiple | Open |
| 10 | Three independent peer lists can disagree: `ONETManager._connectedNodes`, `ONETProtocol._connectedNodes`, `InternalP2PNetworkProvider._connectedNodes`. | | Open |
| 11 | Controller caches a faulted `ONETManager` initialization task forever; one failed startup breaks ONET until process restart. | `ONETController.GetOnetManagerStaticAsync` | Open |

## Medium

| # | Issue | Location | Status |
|---|-------|----------|--------|
| 12 | Stats metrics are misleading: latency uses ICMP (usually blocked on Railway → fallback numbers, serial 5s timeouts per peer); "throughput" times an in-memory 1MB copy. Two empty `catch { }` blocks. | `ONETManager.GetNetworkStatsAsync`, `ONETProtocol.Metrics.cs` | Open |
| 13 | Consensus `_pendingProposals` / `_votes` never pruned (unbounded growth); quorum counts inactive nodes, so proposals can stall. | `ONETConsensus.cs` | Open |
| 14 | `UseTestDataWhenLiveDataNotAvailable` returns `IsError=false` with `Result=null` on real failures. | `ONETController.GetOASISDNA` | Open |
| 15 | ~6,400 lines (~30%) unreachable from production: `HoloNETClientV2`, `EnhancedHoloNETClient`, `HoloNETEnhancedWrapper`, `NetworkMetricsService`, `ONETUnifiedArchitecture`, `ONETHyperDriveIntegration`, `ONETWEB4APIIntegration`, `ONETWEB5STARIntegration` (latter two only referenced by tests), `APILoadBalancer.SelectEndpointAsync` pass-through. | | Open |
| 16 | Low: API key compared with `!=` (not constant-time); frame signature verified over re-serialized JSON rather than received bytes. | `ONETController`, `ONETTcpApplicationMessageChannel` | Open |

## Fixed earlier in this audit cycle

- Consensus events (`ConsensusReached`/`ConsensusFailed`) were declared but never raised.
- Leader never rotated; now heartbeat-based (`RecordHeartbeatAsync`, `HeartbeatTimeout`). **Callers must send heartbeats** — nothing in the peer keep-alive path calls `RecordHeartbeatAsync` yet.
- `FindOptimalRouteAsync` searched from the target to itself and returned empty routes; now returns the direct hop and errors for unknown/inactive targets.
- `GetRoutingStatsAsync` threw on an empty table.

## Remediation order

1. Security: items 2–7.
2. Discovery end to end: item 1, with a `WebApplicationFactory` test.
3. Persistence key: item 8 (stable `ONET.InstanceId` / `RAILWAY_SERVICE_ID`).
4. Concurrency and single peer list: items 9–11.
5. Metrics and consensus hygiene: items 12–14.
6. Dead code removal: item 15.
