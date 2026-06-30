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
