# Edge offline messaging and nearby sync

Offline-first OASIS messaging that works with no internet: over Bluetooth LE, Wi-Fi Direct/LAN, and multi-hop mesh, and re-syncs through HyperDrive when a route to the hosted node returns.

## Packages

| Package | Purpose |
|---|---|
| `NextGenSoftware.OASIS.Edge.NearbySync` | Radio-agnostic nearby transport: links, chunking, signed envelopes, mesh relay, `IHyperDriveSyncTransport` implementation |
| `NextGenSoftware.OASIS.Edge.Messaging` | Local message store, durable outbox commands, online cursor sync, nearby delivery |

## How a message travels

1. `EdgeMessagingService.SendAsync` stores the message locally and queues a `oasis.message-command.v1` command in the Edge outbox. The command's entity id is the client-generated message id, so replays are idempotent.
2. If the recipient is a paired, reachable nearby peer, the message is also delivered directly (BLE, Wi-Fi Direct, LAN or relayed over the mesh). This is an acceleration; the outbox stays the durable route.
3. Whenever any sync route exists, HyperDrive delivers the outbox: directly to the hosted node when online, or through a nearby **gateway** (a device with internet) when not.
4. The hosted `HyperDriveCommandExecutor` runs the command through `MessagingManager.SendMessageAsync`, which is idempotent by message id.
5. Online, `SyncFromServerAsync` pulls `GET api/messaging/sync?since=<cursor>` pages, merges by message id, and queues delivery receipts.

Both routes use the same message id, so the nearby copy and the server copy converge without duplicates.

## Wiring it up

```csharp
var identity = new NearbyIdentity(savedPrivateKeyOrNull);      // persist ExportPrivateKey() in the secure store
identity.TrustPeer(friendPublicKey, friendAvatarId);            // after authenticated pairing
var node = new NearbyNode(identity, new INearbyLink[] { new BleNearbyLink(blePort), new TcpNearbyLink() });

var nearby = new NearbyPeerSyncTransport(node, gatewayNodeId);
var transport = new ConnectivityRoutedSyncTransport(httpTransport, nearby, () => isOnline);
var runtime = new OASISEdgeRuntime(options, transport);

var messaging = new EdgeMessagingService(avatarId, new EdgeMessageStore(path),
    new EdgeRuntimeMessageCommandQueue(runtime), node, identity);
```

A gateway device (online) runs `new NearbyPeerResponder(node, new GrantForwardingExchangeHandler(anonymousHttpClient))`. It forwards a peer's exchange to the hosted node using the **peer's own signed offline-session grant**, so the gateway holds no authority over the peer's avatar.

## Security model

- Every request and response is signed (ECDSA P-256). A node id is the hash of its public key.
- A peer is obeyed only after its public key was bound to an avatar through an authenticated step (ONET peer binding while online, or in-person QR/NFC pairing). Unknown keys are ignored.
- The avatar a request acts for is the one bound to the verified key, never a self-claimed field. A request claiming another avatar is rejected.
- Relays can decrement the hop count but cannot alter content (the hop field is outside the signature). Relays never need to trust the endpoints.
- Mesh flooding is bounded by hop limit and a duplicate cache.
- Parsing hostile bytes never throws; frame, message and partial-reassembly sizes are capped.

## BLE profile

Service `0a51500b-1e5e-4f0d-9a3b-0a5150e00001`, RX (write without response) `...0002`, TX (notify) `...0003`. Every device is peripheral and central. Request the largest MTU (Android `requestMtu(247)`); `BleNearbyLink` frames to MTU minus 3 and the chunker handles the rest. The host implements `IBleGattPort` per OS; everything else is shared.

## Known limits

- Message **content is not end-to-end encrypted** yet. Nearby traffic is authenticated and tamper-evident, not confidential. Encrypting content to the recipient's key is the next security step.
- No store-and-carry for third parties: the mesh delivers while sender and recipient are connected through relays at the same time. Offline-to-offline delivery later is handled by the outbox plus the server.
- A lost radio frame expires the partial message; the idempotent exchange is retried by the caller (the Edge runtime's sync loop).
- The platform BLE shims (`IBleGattPort` for Android/iOS) and Wi-Fi Direct/Multipeer discovery are host code. They have not been run on physical devices; see `ACTIONS-FOR-DAVID.md`.
- The JSON message store suits chat-sized histories. Move to the Edge SQLite store for very large archives.
