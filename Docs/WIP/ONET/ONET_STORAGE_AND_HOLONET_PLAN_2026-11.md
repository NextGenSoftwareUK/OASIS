# ONET: peer storage consolidation and HoloNET P2P mode — plan

November 2026. Follows `ONET_AUDIT_2026-10.md`. Covers the two open strategic items (#7, #8).

## Done this cycle

- Kademlia table is keyed on the node's own identity (DNA `NodeId` or `RegisterLocalNodeIdentity`) and holds every known peer, whichever of identity and cache load comes first. Before, it was created on the first `RegisterNodeAsync`, which only `ONETProviderIntegration` called with provider-bridge ids, so normal nodes had no table.
- `RegisterNodeForDiscoveryAsync` no longer overwrites the protocol's local node id. Registering a provider bridge used to make outbound PINGs and peer exchange sign as `<Provider>_node`, an id with no key.
- Provider bridges are no longer registered as discovery peers; they were persisted and gossiped as peers whose address was a provider name.
- `ONETManager` and `ONETDiscovery` both wrote `onet-peers.json` with different schemas (`List<ONETNode>` vs `Dictionary<string, DiscoveredNode>`). After any network stop, discovery's next `LoadPeerCacheAsync` threw and aborted discovery init. The manager now writes `onet-known-peers.json` via `OASISPersistence` atomic writes; the integration test `ONETManager_StartStop_PeerCacheRoundTrip_PreservesConnectedNodes` checks discovery still loads after a manager stop.
- Unit test project no longer globs the nested `ONODE.Core.Tests` project (root cause of the CS0579 duplicate-attribute errors).

## 7. One peer store

### Current state

Peers are persisted in three places:

| Store | Owner | Contents | When written |
|---|---|---|---|
| `onet-peers.json` | `ONETDiscovery` | discovered nodes (no public keys) | node registration |
| `onet-known-peers.json` | `ONETManager` | connected peers with public keys | network stop |
| ONET state Holon (`HolonManager`) | `ONETManager` | connected peers + AES-GCM sealed identity | network stop, new identity; only with `ONET.InstanceId`/`RAILWAY_SERVICE_ID` and a provider |

The audit made `ONETProtocol` the owner of the single live peer table, but persistence is still split. Restart behaviour depends on which store happened to be written last.

### Target

- One record type, `ONETPeerRecord` (`Id`, `Address`, `PublicKey`, `Capabilities`, `LastSeen`), owned by `ONETProtocol`.
- One durable store per node, chosen once at start:
  - provider-backed: the state Holon, when `InstanceId` and a storage provider are configured (Railway and hosted nodes);
  - node-local: one file through `OASISPersistence` otherwise.
  Not both, and no read-one-then-the-other fallback.
- Discovery, Kademlia seeding and reconnect all read from the protocol's table after load; nothing else reads files.
- Writes on peer add/remove (debounced) as well as on stop, so a crash does not lose the table.

### Steps

1. Add `ONETPeerRecord` and an `IONETPeerStore` with `LoadAsync`/`SaveAsync` returning `OASISResult`; implement `HolonPeerStore` and `FilePeerStore`.
2. `ONETProtocol` loads the store once in `InitializeAsync`, fills its peer table, registers public keys (rejecting id/key mismatches as now) and seeds Kademlia.
3. Remove `ONETDiscovery.PersistPeerCacheAsync`/`LoadPeerCacheAsync`, `ONETManager.PersistPeersAsync`/`LoadPersistedPeersAsync`, and the `onet_peers` metadata write in `SaveStateToHolonAsync` (identity stays in the Holon).
4. One-time read of the old files/metadata on upgrade, then delete them. Upgrade path only, removed after one release.
5. Tests: restart round-trip for each store; corrupt store surfaces an error; Kademlia seeded from the store.

## 8. HoloNET P2P mode

### Current state

`P2PNetworkType.HoloNET` builds `HoloNETP2PProvider` over HoloOASIS's `HoloNETClientAppAgent`, but nothing travels over Holochain:

- `SendMessageAsync`/`BroadcastMessageAsync` only check that the HoloNET websocket is open, then send through `ONETProtocol` TCP like Internal mode, with `SourceNodeId = "local"`.
- `GetConnectedNodesAsync` maps `NetworkConnection.FromNodeId`/`ToNodeId` into `ONETNode.Id`/`Address`.
- `ONETManager.ApplicationEndpoint` throws in HoloNET mode, so request/response (peer exchange, HyperDrive sync) is unavailable.

So HoloNET mode is Internal mode with an extra dependency, and the topology it reports is wrong.

### Target

Holochain carries ONET traffic; ONET identity and signing stay the same, so both modes interoperate at the message level.

- `onet` zome in the OASIS hApp:
  - `register_node(record)`: commits a node record (ONET node id, public key, advertised address, capabilities) signed with the ONET key, linked from a well-known anchor;
  - `list_nodes()`: reads the anchor links (discovery);
  - `send(to_agent, frame)` using `send_remote_signal`; the receiving zome emits the frame as a signal.
- `HoloNETP2PProvider` sends the existing signed `ONETTcpApplicationMessageChannel` frames through `send`, raises `MessageReceived` from HoloNET signals after the same signature, freshness and single-use checks, and builds nodes from `list_nodes()`.
- Map agent pubkey to ONET node id through the signed node record; reject records whose id is not SHA-256 of the key.
- Enable `ApplicationEndpoint` in both modes by putting the endpoint on top of the provider rather than TCP.

### Steps

1. Write the `onet` zome and add it to the hApp DNA (HoloNET/hApp repo). Zome tests with Tryorama.
2. Provider: replace the TCP path with zome calls and signal handling; fix `GetConnectedNodesAsync`; real `NodeConnected`/`NodeDisconnected` from node records and signal activity.
3. Move `ApplicationEndpoint` onto `IP2PNetworkProvider` transport.
4. Live test with the existing HoloNET TestHarness (it manages the conductor itself): two agents exchange signed frames and peer lists.
5. Docs: DNA `ONET.NetworkType`, conductor requirements, and the interop note.

## Order

#7 first (small, contained, fixes restart determinism for both modes), then #8, which reuses the peer store for Holochain-discovered nodes.
