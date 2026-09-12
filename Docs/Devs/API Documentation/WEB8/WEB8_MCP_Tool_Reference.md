# WEB8 MCP Tool Reference

The WEB8 Galactic Mesh Layer exposes **8 typed MCP tools** covering distributed mesh node registration, routing, message relay, and protocol translation. All tools are in-process — no extra HTTP layer.

All tools return a JSON-serialised `OASISResult<T>` envelope (or a raw JSON string for translation tools). On success `isError` is `false` and data is in `result`. On failure `isError` is `true` and `message` describes the problem.

Install: `npm install -g @oasisomniverse/mcp-server` or `dotnet tool install -g NextGenSoftware.OASIS.MCP.Server`

---

## Galactic Mesh tools

#### `web8_register_node`
Registers a node in the galactic mesh — any externally reachable system that can accept a relayed message at an HTTP endpoint.

**Parameters:**
| Name | Type | Required | Description |
|---|---|---|---|
| `node` | object | yes | JSON `GalacticNode` object (fields: `nodeId`, `name`, `endpointUrl`, `region`) |
| `avatarId` | string (GUID) | no | Avatar registering the node |

**Returns:** Registered `GalacticNode` with assigned `nodeId`.

---

#### `web8_get_nodes`
Lists every node currently registered in the mesh.

**Parameters:**
| Name | Type | Required | Description |
|---|---|---|---|
| `avatarId` | string (GUID) | no | Requesting avatar |

**Returns:** Array of `GalacticNode` objects.

---

#### `web8_add_link`
Declares a bidirectional weighted link (mesh edge) between two nodes, carrying its measured latency for shortest-path routing.

**Parameters:**
| Name | Type | Required | Description |
|---|---|---|---|
| `nodeAId` | string (GUID) | yes | First node |
| `nodeBId` | string (GUID) | yes | Second node |
| `latencyMs` | double | no | Link latency in ms (default 50) |
| `avatarId` | string (GUID) | no | Avatar adding the link |

**Returns:** Created mesh link object.

---

#### `web8_heartbeat`
Records a heartbeat for a node, keeping it inside the liveness window so routing continues to consider it healthy.

**Parameters:**
| Name | Type | Required | Description |
|---|---|---|---|
| `nodeId` | string (GUID) | yes | Node sending the heartbeat |
| `avatarId` | string (GUID) | no | Avatar associated with the node |

**Returns:** Updated node liveness timestamp.

---

#### `web8_compute_route`
Computes the shortest (lowest cumulative latency) path between two nodes via Dijkstra's algorithm. Automatically excludes any node outside its liveness window (self-healing routing).

**Parameters:**
| Name | Type | Required | Description |
|---|---|---|---|
| `sourceNodeId` | string (GUID) | yes | Starting node |
| `destinationNodeId` | string (GUID) | yes | Destination node |
| `avatarId` | string (GUID) | no | Requesting avatar |

**Returns:** Ordered array of `GalacticNode` objects forming the optimal route, plus total latency.

---

#### `web8_send_message`
Routes and relays a message hop-by-hop to its destination via real HTTP forwarding. Self-heals around failed/stale nodes by excluding them and recomputing the route.

**Parameters:**
| Name | Type | Required | Description |
|---|---|---|---|
| `message` | object | yes | JSON `MeshMessage` object (fields: `sourceNodeId`, `destinationNodeId`, `payload`, `messageId`) |
| `avatarId` | string (GUID) | no | Sending avatar |

**Returns:** Delivery confirmation with route taken and any nodes that were bypassed.

---

## Protocol Bridge tools

#### `web8_translate_inbound`
Translates an external system's raw payload into the unified `MeshMessage` envelope. Supports Json, FormUrlEncoded, and PlainText formats.

**Parameters:**
| Name | Type | Required | Description |
|---|---|---|---|
| `rawPayload` | string | yes | Raw inbound payload string |
| `format` | string | yes | `Json`, `FormUrlEncoded`, or `PlainText` |
| `sourceNodeId` | string (GUID) | yes | Node the payload arrived from |
| `destinationNodeId` | string (GUID) | yes | Intended destination node |

**Returns:** JSON `MeshMessage` object ready for mesh routing.

---

#### `web8_translate_outbound`
Translates a `MeshMessage`'s payload back into a target external wire format.

**Parameters:**
| Name | Type | Required | Description |
|---|---|---|---|
| `message` | object | yes | JSON `MeshMessage` to translate |
| `targetFormat` | string | yes | `Json`, `FormUrlEncoded`, or `PlainText` |

**Returns:** String in the requested target format.

---

## See also

- [WEB8 REST API Reference](WEB8_REST_API_Reference.md) *(in development — MCP tools are the primary access method now)*
- [Full MCP Tool Reference (all layers)](../WEB6/WEB6_MCP_Tool_Reference.md)
- [MCP Server README](../../../../WEB6/NextGenSoftware.OASIS.MCP.Server/README.md)
