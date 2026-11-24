# OASIS Messaging: reusable offline-first component

One messaging component for every OASIS app (HerzApp, Our World, any WEB4 client): a backend in the WEB4 API, an offline-first engine in the existing npm package, drop-in UI in the six `@oasisomniverse/*` libraries, and Bluetooth/LAN/mesh sync in the Edge Runtime. See also [Edge offline messaging](EDGE_OFFLINE_MESSAGING.md).

## Pieces

| Layer | Where | What it gives you |
|---|---|---|
| Backend | `MessagingManager` (API.Core) + `MessagingController` (`api/messaging`) | Idempotent send, delivery/read receipts, cursor sync, conversations, groups, real-time push |
| Hosted command | `HyperDriveCommandExecutor` (`oasis.message-command.v1`) | Offline sends replay exactly once through the HyperDrive outbox |
| Engine | `@oasisomniverse/web4-api/messaging` (>= 2.1.0) | Local store, outbox, cursor sync, real-time, receipts, typing, token refresh, themable chat view |
| UI | `@oasisomniverse/react`, `vue`, `angular`, `svelte`, `nextjs`, `js` (>= 1.3.0) | `OASISMessaging` and `OASISChatWidget` |
| Edge | `NextGenSoftware.OASIS.Edge.NearbySync`, `NextGenSoftware.OASIS.Edge.Messaging` | BLE/Wi-Fi Direct/LAN/mesh transport, signed envelopes, gateway forwarding |

## Backend API (`api/messaging`)

| Endpoint | Purpose |
|---|---|
| `POST send` | Send with a client-generated id. Replaying the same id from the same sender returns the stored message; the same id from another sender is rejected. `toAvatarId` for direct, `conversationId` for a group |
| `GET sync?since=&limit=` | Messages after a cursor, oldest first, with `nextCursor` and `hasMore` |
| `GET conversations` | Direct and group conversations with unread counts |
| `POST mark-messages-delivered`, `POST mark-messages-read` | Receipts (per member for groups) |
| `POST realtime-ticket` | Single-use 30-second ticket for `wss://host/ws/messaging?ticket=...` (no token in the URL) |
| `GET presence?avatarIds=` | Which avatars are connected to this server instance |
| `POST groups`, `GET groups`, `POST groups/{id}/members`, `POST groups/{id}/leave`, `GET groups/{id}/messages` | Group conversations (creator manages members) |

Real-time events on the socket: `message`, `receipt`, `typing` (client sends `{"type":"typing", ...}`).

**Scale-out:** push reaches sockets on the same server instance only. Clients always catch up from the durable `sync` cursor after a reconnect, so a message is never lost; push only makes it immediate. If you run several ONODE instances and want push across all of them, add a pub/sub backplane later (not required for correctness).

**Tokens:** JWTs last 15 minutes. After a long offline period the engine refreshes the session with the stored refresh token (`api/avatar/refresh-token`) and retries; if that fails it keeps messages queued and sets `authRequired` so the UI asks the user to sign in. Messages are never failed because a token expired.

## HerzApp (HerzID)

HerzApp signs the user in with HerzID/Avatar SSO, then hands the resulting `OASISClient` to the component. No messaging-specific auth is needed.

```jsx
import { OASISClient } from '@oasisomniverse/web4-api';
import { OASISChatWidget, OASISMessaging } from '@oasisomniverse/react';

const oasis = new OASISClient({ baseUrl: 'https://api.oasisomniverse.one' });
// After HerzID / OASIS SSO login (token + refreshToken from the auth response):
oasis.setToken(jwtToken, { refreshToken, avatarId });

export function HerzChat({ herzNames }) {
  return <OASISChatWidget client={oasis} theme="oasis" resolveName={(id) => herzNames[id] ?? id.slice(0, 8)} />;
}
// or a full page: <OASISMessaging client={oasis} theme="oasis" />
```

Use `resolveName` to show HerzID names. Groups work for HERZ circles: `controller.createGroup(title, participantIds)`. The chat works offline in the browser (messages saved locally, sent on reconnect). For true off-grid HERZ use (Bluetooth/mesh) the native shell uses the Edge packages below.

## Our World (Unity)

Our World runs on the Edge-enabled OGEngineClient. Messaging composes the same real components the end-to-end test uses (`EdgeRuntimeNearbyEndToEndTests`):

```csharp
var identity = new NearbyIdentity(savedKeyOrNull);                         // persist ExportPrivateKey() in the platform secure store
identity.TrustPeer(friendPublicKey, friendAvatarId);                        // after pairing (QR/NFC, or ONET binding while online)
var node = new NearbyNode(identity, new INearbyLink[] { new BleNearbyLink(blePort), new TcpNearbyLink() });
var transport = new ConnectivityRoutedSyncTransport(hostedHttpTransport, new NearbyPeerSyncTransport(node, gatewayNodeId), () => isOnline);
var runtime = new OASISEdgeRuntime(options, transport);
var chat = new EdgeMessagingService(avatarId, new EdgeMessageStore(path), new EdgeRuntimeMessageCommandQueue(runtime), node, identity);
await chat.SendAsync(friendAvatarId, "Meet at the shrine");
```

`blePort` is the platform shim (`IBleGattPort`) for Android/iOS. Bundling the two new assemblies into the `com.nextgensoftware.oasis.edge` Unity package happens through the Edge release pipeline (see ACTIONS-FOR-DAVID).

## Verification status

Verified here: backend against a real SQLite store (idempotency, receipts, sync paging, groups, events), the real Edge runtime with its SQLite outbox over a simulated radio and real TCP sockets (chunking, multi-hop relay, signature and avatar checks, duplicate suppression, exactly-once delivery), the npm engine (33 tests) and all six UI libraries (type-checked, rendered against a stubbed server). Not verified here: physical BLE/Wi-Fi Direct radios, Unity compilation, live deployment.
