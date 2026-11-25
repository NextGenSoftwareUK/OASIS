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

### Holochain transport — implemented (November 2026)

David decided on 24 November 2026 to build this now.

OASIS hApp (`OASIS-Holochain-hApp`), coordinator module `dnas/oasis/zomes/coordinator/oasis/src/onet.rs`. It's coordinator-only, so the DNA hash and existing data are unchanged:
- `onet_register_node(record)`: publishes or replaces this agent's ONET node record (`node_id`, `public_key`, `advertised_address`, `signature`) as its membership of the HyperNET session `onet`. The zome rejects ids that are not lowercase-hex SHA-256, unsigned records, and records over 900 bytes.
- `onet_nodes()`: every agent with a well-formed record. This is discovery.
- `onet_send({to, frame})`: one remote signal of kind `onet-frame` to one agent. The frame is opaque bytes and at most 512 KiB.
- It reuses HyperNET's `recv_remote_signal`, because a zome has only one.

ONODE `HoloNETP2PProvider`, after the conductor connection and the ONET transport start:
- registers this node's record. The ONET identity signs `onet.node|<nodeId>|<agentPubKey>|<advertisedAddress>`.
- reads `onet_nodes` on start and every minute. It keeps only records whose id is the SHA-256 of the key and whose signature verifies, registers their keys and connects them at `holochain:<agentPubKey>`.
- sends frames to `holochain:` peers through `onet_send`. `ONETProtocol.HolochainFrameSender` is the hook, and a `holochain:` peer is never tried over TCP.
- feeds `onet-frame` signals into `ReceiveApplicationFrameAsync`. That runs the same checks as TCP frames: signature, identity, freshness, single use, then decryption.

Remote signals are fire and forget. A successful hand-off counts as accepted, and replies come back as frames of their own.

## Message encryption (audit #24) — implemented (November 2026)

Decided on 24 November 2026: encrypt, with a coordinated upgrade.

- Application frames are version 2. `ONETFrameCrypto` seals each one for the target's identity key: a fresh P-256 ECDH key per frame, HKDF-SHA256, then AES-256-GCM, with version, source, target and issued time as associated data. The sealed frame is then signed.
- Receivers verify the signature, sender identity, freshness and single use before decrypting. The decrypted message must match the signed header.
- No plaintext fallback: a peer with no registered key can't be messaged (`ONET_TARGET_KEY_UNKNOWN`), and v1 frames are rejected.
- **Coordinated upgrade:** every ONET node (Railway WEB4 and self-hosted ONODEs) must run the same release.
- Key learning: on connect to a TCP peer whose key is unknown, the node sends `ONET_KEY`. The peer answers `ONET_KEY <base64 SPKI>`, and the key is registered only if its SHA-256 equals the node id. Peer exchange, bootstrap records, stored peers and Holochain node records also supply keys.
- Trade-off: identity keys both sign and act as the static ECDH key. Compromising a node's key therefore exposes frames previously sent to it.

## Order

#7 is done; #8 reuses the same peer store for Holochain-discovered nodes.
