# WEB7 MCP Tool Reference

The WEB7 Symbiosis Layer exposes **7 typed MCP tools** covering non-invasive bio-signal session management and collective consciousness spaces. All tools are in-process — no HTTP overhead, no external dependencies.

All tools return a JSON-serialised `OASISResult<T>` envelope. On success `isError` is `false` and the data is in `result`. On failure `isError` is `true` and `message` describes the problem.

Install: `npm install -g @oasisomniverse/mcp-server` or `dotnet tool install -g NextGenSoftware.OASIS.MCP.Server`

---

## Symbiosis Session tools

#### `web7_start_session`
Starts a new WEB7 symbiosis session. The connection is always voluntary — `consentGranted` must be explicitly `true` or the call is rejected.

**Parameters:**
| Name | Type | Required | Description |
|---|---|---|---|
| `avatarId` | string (GUID) | yes | OASIS avatar starting the session |
| `consentGranted` | bool | yes | Must be `true` — enforces the Borg-Free pledge |
| `retention` | string | no | `Ephemeral` (default) or `Persistent` — Ephemeral wipes all signal data on end |

**Returns:** Session object with `sessionId`, `state`, and `consentGranted`.

---

#### `web7_submit_signals`
Submits a batch of raw bio-signal samples for an active consenting session. Accepts EEG, HRV, GSR, EyeTracking, and VocalHarmonics channels. Rejects any channel name implying an invasive/implanted source.

**Parameters:**
| Name | Type | Required | Description |
|---|---|---|---|
| `sessionId` | string (GUID) | yes | Active session id returned by `web7_start_session` |
| `samples` | array | yes | JSON array of `BioSignalSample` objects (fields: `channel`, `value`, `timestamp`) |
| `avatarId` | string (GUID) | no | Avatar submitting signals |

**Returns:** Freshly computed `IntentionState` (focus, arousal, emotional valence, cognitive load) derived via real FFT/HRV/GSR DSP.

---

#### `web7_end_session`
Ends a symbiosis session. With Ephemeral retention (the default) all signal-derived data is wiped immediately — no trace left.

**Parameters:**
| Name | Type | Required | Description |
|---|---|---|---|
| `sessionId` | string (GUID) | yes | Session to end |
| `avatarId` | string (GUID) | no | Avatar ending the session |

**Returns:** Confirmation with final session state.

---

#### `web7_get_session`
Gets a symbiosis session's current state including its last computed intention state.

**Parameters:**
| Name | Type | Required | Description |
|---|---|---|---|
| `sessionId` | string (GUID) | yes | Session to query |
| `avatarId` | string (GUID) | no | Requesting avatar |

**Returns:** Full session object including `intentionState`, `state`, and `retentionMode`.

---

## Collective Consciousness tools

#### `web7_create_space`
Creates a collective consciousness space — a shared intention field where multiple consenting sessions co-create.

**Parameters:**
| Name | Type | Required | Description |
|---|---|---|---|
| `name` | string | yes | Human-readable name for the space |
| `avatarId` | string (GUID) | no | Avatar creating the space |

**Returns:** Space object with `spaceId` and `name`.

---

#### `web7_join_space`
Joins a consenting symbiosis session to a collective consciousness space. Only sessions with valid consent can join.

**Parameters:**
| Name | Type | Required | Description |
|---|---|---|---|
| `spaceId` | string (GUID) | yes | Target space |
| `sessionId` | string (GUID) | yes | Active consenting session to join |
| `avatarId` | string (GUID) | no | Avatar requesting the join |

**Returns:** Updated space membership list.

---

#### `web7_get_aggregate_field`
Recomputes and returns the aggregate (mean) intention field across every participating session in a collective consciousness space. Never returns any individual's raw signal — privacy is enforced at this boundary.

**Parameters:**
| Name | Type | Required | Description |
|---|---|---|---|
| `spaceId` | string (GUID) | yes | Space to aggregate |
| `avatarId` | string (GUID) | no | Requesting avatar |

**Returns:** `AggregateIntentionField` with mean focus, arousal, valence, and cognitive load across all participants.

---

## See also

- [WEB7 REST API Reference](WEB7_REST_API_Reference.md) *(in development — MCP tools are the primary access method now)*
- [Full MCP Tool Reference (all layers)](../WEB6/WEB6_MCP_Tool_Reference.md)
- [MCP Server README](../../../../WEB6/NextGenSoftware.OASIS.MCP.Server/README.md)
