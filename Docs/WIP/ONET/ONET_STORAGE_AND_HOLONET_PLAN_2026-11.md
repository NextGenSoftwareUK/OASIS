# ONET: peer storage consolidation and HoloNET P2P mode — plan

November 2026. Follows `ONET_AUDIT_2026-10.md`. Covers the two open strategic items (#7, #8).

## Done this cycle

- Kademlia table is keyed on the node's own identity (DNA `NodeId` or `RegisterLocalNodeIdentity`) and holds every known peer, whichever of identity and cache load comes first. Before, it was created on the first `RegisterNodeAsync`, which only `ONETProviderIntegration` called with provider-bridge ids, so normal nodes had no table.
- `RegisterNodeForDiscoveryAsync` no longer overwrites the protocol's local node id. Registering a provider bridge used to make outbound PINGs and peer exchange sign as `<Provider>_node`, an id with no key.
- Provider bridges are no longer registered as discovery peers; they were persisted and gossiped as peers whose address was a provider name.
- `ONETManager` and `ONETDiscovery` both wrote `onet-peers.json` with different schemas (`List<ONETNode>` vs `Dictionary<string, DiscoveredNode>`). After any network stop, discovery's next `LoadPeerCacheAsync` threw and aborted discovery init. The manager now writes `onet-known-peers.json` via `OASISPersistence` atomic writes; the integration test `ONETManager_StartStop_PeerCacheRoundTrip_PreservesConnectedNodes` checks discovery still loads after a manager stop.
- Unit test project no longer globs the nested `ONODE.Core.Tests` project (root cause of the CS0579 duplicate-attribute errors).

## 7. One peer store — done (November 2026)

Peers used to be persisted in up to three places (discovery's `onet-peers.json`, the manager's file and the ONET state Holon), and restart behaviour depended on which was written last.

Now each node has exactly one durable peer store, chosen by `ONETManager.UsesProviderPeerStore`:

| Configuration | Store |
|---|---|
| storage provider and `ONET.InstanceId` (or `RAILWAY_SERVICE_ID`) | ONET state Holon, `onet_peers` metadata (identity sealed alongside) |
| otherwise | node-local `onet-known-peers.json` via `OASISPersistence` (atomic writes) |

- Peers are saved on network stop and loaded on network start. Restored peers go to `ONETDiscovery.AddKnownPeers`, which fills the discovered-node table and the Kademlia table, and then `ReconnectKnownPeersAsync` reconnects them, rejecting id/key mismatches.
- Discovery no longer reads or writes a file of its own.
- There's no read-one-then-the-other fallback. A failed load or save is logged as an error.
- Old `onet-peers.json` files are ignored and can be deleted.

Follow-ups:
- Save on peer add/remove (debounced) as well as on stop, so a crash does not lose recently learned peers.
- Discovery-only peers (learned but never connected) are not persisted. They are re-learned through bootstrap and peer exchange.

## 8. HoloNET P2P mode

### Current state

`P2PNetworkType.HoloNET` builds `HoloNETP2PProvider` over HoloOASIS's `HoloNETClientAppAgent`, but nothing travels over Holochain:

- `SendMessageAsync`/`BroadcastMessageAsync` only check that the HoloNET websocket is open, then send through `ONETProtocol` TCP like Internal mode, with `SourceNodeId = "local"`.
- `GetConnectedNodesAsync` maps `NetworkConnection.FromNodeId`/`ToNodeId` into `ONETNode.Id`/`Address`.
- `ONETManager.ApplicationEndpoint` throws in HoloNET mode, so request/response (peer exchange, HyperDrive sync) is unavailable.

So HoloNET mode is Internal mode with an extra dependency, and the topology it reports is wrong.

### Interim fix (November 2026)

`HoloNETP2PProvider` now does what this mode claimed to do:
- It requires a live conductor connection. It reuses HoloOASIS's connection, or connects to `HoloNETDNA.HolochainConductorAppAgentURI`, and fails clearly when that isn't configured. The URL is no longer hardcoded.
- It runs the real signed ONET transport by wrapping `InternalP2PNetworkProvider`.
- Peers, messages and events come from that transport.
- `ApplicationEndpoint` works in both modes.

Removed:
- the conductor counted as a fake "peer", and invented "data received" messages;
- `ConnectToNodeAsync`/`DisconnectFromNodeAsync` re-pointing or closing the conductor websocket;
- ONET never being started in this mode.

Also: `ONETProtocol` now raises `NodeConnected`/`NodeDisconnected`. It used to declare them and never raise them, so no provider forwarded any connect events.

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

#7 is done; #8 reuses the same peer store for Holochain-discovered nodes.
