# ONET Audit — October 2026

Scope: `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/ONET/*` (47 files, ~21.5k lines at audit time), `Managers/ONETManager.cs`,
`ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/ONETController.cs`, and `OASIS Architecture/NextGenSoftware.OASIS.ONET`.

Status column is updated as fixes land on `Development`.

## Critical

| # | Issue | Location | Status |
|---|-------|----------|--------|
| 1 | Bootstrap discovery could not work end to end: client GET `{server}/onet/nodes` (no such route), no signature headers, server endpoint behind `[Authorize(Wizard)]`, response wrapped in `OASISResult` while the client expected a bare array. | `ONETDiscovery.Helpers.cs`, `ONETController.cs` | **Fixed** — anonymous, signed `GET /api/v1/onet/peers` returning `NodeInfo[]`; client sends `X-ONET-NodeId/Timestamp/Signature`; `nodes/register` is anonymous. Peer-to-peer exchange: see #22. |
| 18 | **Platform-wide:** GraphQL (`/graphql`) and gRPC services had no caller authentication; every mutation was open (e.g. `DeleteAvatarByEmail`, `SendSolanaTransaction`, `MintWeb4Nft`, `AddKarmaToAvatar`, `StopNetwork`). | `ONODE.WebAPI/GraphQL/*`, `GrpcServices/*`, `Startup.cs` | **Fixed (ONODE WebAPI)** — global HotChocolate field middleware and gRPC interceptor (`ONODE.WebAPI/Security/`) require a Wizard avatar except the login/registration allowlist. Follow-up: add ownership checks per operation before opening it to ordinary avatars. STAR (WEB5) and WEB6–WEB10 require an authenticated WEB4 bearer via the usage ledger; their resolvers have not been audited for ownership checks. |

## High — security

| # | Issue | Location | Status |
|---|-------|----------|--------|
| 2 | Identity takeover: `nodes/register` did not require `NodeId == SHA256(PublicKey)` and silently overwrote keys; gossip registered keys the same way. | `ONETSecurity`, `ONETController.RegisterNode`, discovery | **Fixed** — keys accepted only when their SHA-256 is the node id; discovery drops mismatched peers; binding an address requires a fresh proof-of-key signature. |
| 3 | Replayable auth: PING and HTTP peer exchange signed constant strings. | `ONETProtocol.Network.cs`, `ONETRouting.Optimize.cs`, `ONETController` | **Fixed** — `purpose\|nodeId\|unixSeconds` signatures, ±60s window, single-use. |
| 4 | Signed application frames had no timestamp/seen-id. | `ONETTcpApplicationMessageChannel` | **Fixed** — signed `IssuedUnixSeconds`; frames accepted once. |
| 5 | `GET/PUT /api/v1/onet/oasisdna` exposed / replaced the full DNA (all secrets). Same over GraphQL and gRPC. | `ONETController`, GraphQL, `ONETGrpcService` | **Fixed** — Wizard-only `GET/PUT /api/v1/onet/config` (ONET section, secrets blanked, identity immutable); GraphQL fields removed; gRPC returns `PermissionDenied`. |
| 6 | Node private key stored in plaintext Holon metadata. | `ONETManager` | **Fixed** — AES-GCM sealed under `OASIS_ONET_STATE_KEY`, validated on load. |
| 7 | TCP listener: no read timeout or connection cap; any text was `ONET_ACK`ed. | `ONETProtocol.Network.cs` | **Fixed** — 10s read timeout, 256 connections, `ONET_UNSUPPORTED` for unknown text. |
| 19 | `GET/PUT api/v1/onode/oasisdna` returned / replaced the full DNA; `PUT` never persisted. | `ONODEController`, `ONODEManager` | **Fixed** — endpoints, manager methods, MCP tools and WebUI client methods removed. |
| 24 | ONET message "encryption" used session keys generated locally and never exchanged, so receivers could not decrypt; nothing called `DecryptMessageAsync`. | `ONETSecurity`, `ONETProtocol.SendMessageAsync` | **Changed** — send path no longer encrypts; messages are authenticated and integrity/replay-protected by the signed channel but travel **unencrypted**. Open: a real key exchange (e.g. ECDH over the node keys) if message confidentiality is required. |

## High — correctness

| # | Issue | Location | Status |
|---|-------|----------|--------|
| 8 | Holon persistence keyed on the random container machine name. | `ONETManager` | **Fixed** — keyed on `ONET.InstanceId` / `RAILWAY_SERVICE_ID`. |
| 9 | Plain `Dictionary` written concurrently with no locking. | multiple | **Fixed** — security, protocol peer/bridge and discovery tables are concurrent; consensus member table accessed under its lock. |
| 10 | Three independent peer lists; manager disconnect only edited its own copy. Manager and P2P provider also built and started a second set of protocol components (two discovery and consensus loops, separate key stores). | `ONETManager`, `ONETProtocol`, `InternalP2PNetworkProvider` | **Fixed** — `ONETProtocol` owns the single peer table and component set; manager and provider reuse them; restored peers reconnect through the protocol. |
| 11 | Controllers cached a faulted manager initialization task forever. | `ONETController`, `ONODEController` | **Fixed**. |
| 17 | `SendMessageAsync`/broadcast sent unsigned text that no receiver processed; `MessageReceived` was never raised. | `ONETProtocol.Messaging.cs` | **Fixed** — messages go over the signed channel as `onet.message`; receivers raise `MessageReceived`; the signed reply is the delivery acknowledgement. |
| 22 | Peer exchange with known peers sent HTTP to their ONET TCP port. | `ONETDiscovery.Helpers.cs` | **Fixed** — `onet.peers.list` over the signed channel; the requester's `AdvertisedAddress` (signed) lets the responder connect back to reply. |
| 25 | Several request/response endpoints per node (ONET + HyperDrive host) each answered unknown operations with `ONET_OPERATION_NOT_FOUND`. | `ONETHyperDriveSyncHostedService` | **Fixed** — one endpoint per node (`ONETProtocol.ApplicationEndpoint`). |
| 26 | Every `ConnectToNodeAsync` ran a full network discovery to fill in capabilities (seconds, recursive), then fell back to made-up capabilities. | `ONETProtocol` | **Fixed** — uses capabilities already learned for the peer. |

## Medium

| # | Issue | Location | Status |
|---|-------|----------|--------|
| 12 | Stats metrics were fabricated (ICMP latency, in-memory "throughput", clock-noise defaults); empty `catch { }`. | `ONETProtocol.Metrics.cs`, P2P providers, `ONETManager` | **Fixed** — TCP `ONET_PING` round trip and real byte counters; unmeasurable values are `null`. |
| 13 | Consensus never pruned proposals/votes; quorum counted inactive nodes; anyone could vote; nothing made peers members or sent heartbeats. | `ONETConsensus.cs` | **Fixed** — votes only from active members on pending proposals; quorum over active members; expiry and pruning; connected peers are members and authenticated traffic is their heartbeat. |
| 14 | `UseTestDataWhenLiveDataNotAvailable` returns success with invented data on real failures. | controllers | **Fixed for ONET/ONODE**. The flag remains in ~20 other controllers (opt-in, off by default). |
| 15 | ~6,650 lines unreachable from production. | | **Fixed** — removed. |
| 16 | API key compared with `!=`; frame signatures verified over re-serialized JSON. | `ONETController`, channel | **Fixed** — constant-time compare; frames carry and verify the exact signed bytes. |
| 20 | GraphQL and gRPC built a new manager per call (`Task.Run(...).Result`). | `GraphQL/*`, `ONETGrpcService`, `ONODEGrpcService` | **Fixed** — they use the controllers' process-wide managers. |
| 21 | Silent `catch { }` around the peer file cache. | `ONETManager` | **Fixed** — failures logged. |
| 23 | Custom `AuthorizeAttribute` ignored `[AllowAnonymous]`. | `ONODE.WebAPI/Helpers/AuthorizeAttribute.cs` | **Fixed**. |

## Also fixed in this audit cycle

- Consensus events (`ConsensusReached`/`ConsensusFailed`) were declared but never raised.
- Leader never rotated; now heartbeat-based (`RecordHeartbeatAsync`, `HeartbeatTimeout`), fed by authenticated peer traffic.
- `FindOptimalRouteAsync` searched from the target to itself and returned empty routes; now returns the direct hop and errors for unknown/inactive targets.
- `GetRoutingStatsAsync` threw on an empty table.
- `ONETProtocol` ignored an injected DNA and always reloaded from disk.

## Deployment notes

- New DNA fields: `ONET.AdvertisedAddress` (public `host:port` of the TCP listener; needed to receive peer-exchange replies and to register an address) and `ONET.InstanceId`.
- New env var: `OASIS_ONET_STATE_KEY` — base64 32-byte key; without it the node identity is not persisted through the provider.
- Wire changes: PING, peer-exchange and application-frame formats changed; nodes on older builds cannot authenticate against updated ones (plain `ONET_PING` liveness still works). Upgrade all nodes together.
- Holon state ids changed (MD5 → SHA-256 of the instance id); previous state Holons are not read.
- ONODE GraphQL and gRPC now require a Wizard JWT except the login/registration operations.

## Remaining

1. #18 follow-up: per-operation ownership checks so GraphQL/gRPC operations can be opened beyond Wizards; audit STAR/WEB6–WEB10 resolvers.
2. #24: key exchange if ONET message confidentiality is required.
