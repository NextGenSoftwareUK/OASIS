# OASIS2 - Outstanding Improvements

## ONET & HyperDrive follow-ups (from architecture review session 2026-06-29)

### High Priority

1. **Public key bootstrap chicken-and-egg**
   - `RegisterNodePublicKey` must be called before a signed PING can be verified, but fresh nodes have no mechanism to exchange keys on first contact.
   - Fix: include each peer's base64 public key in the `/onet/nodes` peer-exchange JSON response. Recipients register it immediately on receipt so the PING auth path works without out-of-band setup.
   - Files: `ONETDiscovery.cs` (GetBootstrapNodesAsync / QueryNodeForPeersAsync), `ONETSecurity.cs` (RegisterNodePublicKey)

2. **`OASISHyperDrive.DataDirectory` not wired to `OASISDNA`**
   - `OASISDNA.OASIS.DataDirectory` was added but `OASISHyperDrive.DataDirectory` (static field) never reads from it, so the DNA config is ignored.
   - Fix: at startup (e.g. in `OASISHyperDrive` constructor or a static init method), set `OASISHyperDrive.DataDirectory = oasisdna.OASIS.DataDirectory` before the first quota access.
   - Files: `OASISHyperDrive.cs`, `ONETManager.cs` or wherever HyperDrive is first instantiated

3. **Bootstrap / mDNS / blockchain peers not fed into Kademlia table**
   - `DiscoverNodesAsync` merges results from 4 discovery methods (DHT, mDNS, blockchain, bootstrap) but none call `_kademliaTable?.AddNode(...)`.
   - The table is therefore empty on first iterative lookup after a restart, so Kademlia path falls through to BFS gossip every time until `RegisterNodeAsync` has been called.
   - Fix: in `DiscoverNodesAsync`, after merging `uniqueNodes`, call `_kademliaTable?.AddNode(node.Id, node.Address)` for each discovered node.
   - Files: `ONETDiscovery.cs` (DiscoverNodesAsync ~line 528)

### Medium Priority

4. **PING client doesn't send authenticated pings**
   - `ONETRouting.TestNodeConnectivityAsync` sends plain `ONET_PING`. The responder now understands `ONET_PING <nodeId> <sig>` but the client never sends that format, so the auth path is never exercised in real traffic.
   - Fix: in `TestNodeConnectivityAsync`, sign the string "ONET_PING" with the local node's private key and send `ONET_PING <localNodeId> <base64Sig>`.
   - Files: `ONETRouting.cs`

5. **`InitializeAsync().Wait()` sync-over-async in `ONETProtocol` constructor**
   - Can deadlock under ASP.NET's `SynchronizationContext` on first `GetInstance()` call.
   - Fix: move initialization out of the constructor. `GetInstance()` should call `await InitializeAsync()` asynchronously, or use `ConfigureAwait(false)` throughout the init chain.
   - Files: `ONETProtocol.cs` (constructor + GetInstance)

8. **Properly implement `P2PNetworkType.HoloNET` mode — use Holochain DHT as ONET's real P2P transport**

   **Background / current state**
   The two-mode design (`P2PNetworkType.Internal` / `P2PNetworkType.HoloNET`) is already in the code:
   - `P2PNetworkType.cs` — enum defined
   - `ONETManager.InitializeP2PNetworkProvider()` — switch already present; `HoloNET` branch creates a `HoloNETP2PProvider`
   - `HoloNETP2PProvider` — implements `IP2PNetworkProvider`, holds a `HoloNETClientBase` — but **does not use Holochain's DHT**. When `SendMessageAsync` / `BroadcastMessageAsync` are called it silently bypasses HoloNET and routes through the same `ONETProtocol` TCP stack as the Internal mode. The WebSocket connection to the conductor only fires `NodeConnected` / `NodeDisconnected` events — nothing P2P actually flows through Holochain.
   - `P2PNetworkType.Internal` — fully operational; uses ONET's own TCP listener, Kademlia DHT, mDNS SRV+A, ECDSA PING auth, BFS gossip fallback.

   **Why switch to Holochain DHT for the HoloNET mode**
   Holochain's kitsune2 layer already solves the hardest P2P problems: NAT traversal (bootstrap server + relay fallback), peer churn, DHT gossip convergence, Byzantine-resistant validation, and agent cryptographic identity. Rolling our own (the Internal mode) means eventually hitting the same class of bugs HC spent years fixing.

   **What needs to be built**

   *Step 1 — ONET Holochain DNA (Rust, separate project)*
   A minimal hApp with three zomes:
   - `peer_registry` — `register_peer(node_id, endpoint, public_key)` entry; `get_peers()` link traversal; kitsune2 handles the DHT automatically.
   - `onet_messaging` — `send_message(target_agent, payload)` via `call_remote` or signals.
   - `node_status` — heartbeat entry; `get_active_nodes()`.
   The DNA handles validation, replication and gossip; ONET calls zome functions, it doesn't manage the network itself.

   *Step 2 — `HoloNETP2PProvider` rewrite (C#)*
   Replace the current pass-through to `ONETProtocol` with real `CallZomeFunctionAsync` calls via HoloNET:
   ```
   ConnectToNodeAsync  → zome: peer_registry / register_peer
   GetConnectedNodesAsync → zome: peer_registry / get_peers
   SendMessageAsync    → zome: onet_messaging / send_message
   BroadcastMessageAsync → zome: onet_messaging / broadcast (or signals)
   GetNetworkHealthAsync → zome: node_status / get_active_nodes
   ```
   Subscribe to HoloNET signal events for inbound messages instead of polling.

   *Step 3 — Remove ONET-internal P2P plumbing from HoloNET mode*
   When `P2PNetworkType.HoloNET` is active, `ONETDiscovery`, `ONETKademlia`, the mDNS responder, and the TCP listener in `ONETProtocol` should not start — the conductor provides all of this. `ONETSecurity` key management is also redundant (conductor handles agent keys).

   *Step 4 — OASISDNA config*
   Add `P2PMode: "Internal" | "HoloNET"` to `OASISDNA` so the mode is configurable without recompiling, and `ONETManager` reads it at startup to choose the branch.

   **What to keep regardless of mode**
   - `ONETAPIGateway` — HTTP API surface is mode-agnostic
   - `OASISHyperDrive` quota + failover — above the transport layer
   - `ONETConsensus` / `ONETRouting` high-level logic — delegates to whichever `IP2PNetworkProvider` is active

   **Prerequisite**
   ONET DNA must be built and deployed to a conductor before Step 2 can be tested end-to-end. The Internal mode remains the default and fallback until then.

   Files: `HoloNETP2PProvider.cs`, `ONETManager.cs`, `P2PNetworkType.cs`, `OASISDNA.cs`, new `ONET.dna` Rust project.

7. **Holon-backed storage for ONET peer cache and quota counters**
   - **Peer cache**: store as a holon via `IHolonManager` — loaded only at startup (not on the hot path), and sharing it across ONODE cluster nodes gives a shared view of known peers. Replace the local JSON file added in `OASISPersistence` with a holon load/save.
   - **Quota counters**: keep the current in-memory cache (avoids recursive chicken-and-egg dependency since quota checking routes through HyperDrive), but add a background periodic sync that writes the counters *to* a holon every ~60 seconds. On startup, load the holon value as the initial baseline rather than starting from zero.
   - **Prerequisite**: items #2 (DataDirectory wiring) and #1 (public key bootstrap) should land first so HyperDrive and ONET are stable before adding the holon dependency.
   - Files: `OASISPersistence.cs`, `OASISHyperDrive.cs` (quota sync), `ONETDiscovery.cs` (peer cache), `ONETManager.cs` (startup holon load)

### Lower Priority

6. **No unit tests for new features added in this session**
   - Kademlia XOR distance, k-bucket eviction policy, and `GetClosestNodes` correctness.
   - Persistence save/load round-trip (write counters, simulate restart, verify loaded values).
   - `PredictiveFailoverEngine` override: verify background loop sets override, `RouteToProviderAsync` applies it, and it clears on success.
   - mDNS `TryParseServiceResponse` standalone test (PTR+SRV+A packet encode then decode).
   - Files: new test files under `ONODE.Core.UnitTests\` and `ONODE.Core.UnitTests\HyperDrive\`
