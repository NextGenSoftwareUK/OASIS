# STAR × IsoCity: Setup and Adapter

**Goal:** Clone IsoCity and wire it to STAR/ONODE so cities are listed, created, loaded, and saved via the World API.

---

## 1. ONODE endpoints (Phase 1 — implemented)

| Action        | Method | Endpoint                         | Body / notes |
|---------------|--------|----------------------------------|--------------|
| List my cities| GET    | `/api/world/my-worlds`           | Authorize. Returns `{ result: [ { id, name, description, sceneImageUrl? } ] }`. |
| Create world  | POST   | `/api/world`                     | `{ "name": "My City", "description": "..." }`. Returns `{ result: { worldId, name, description } }`. |
| Load world    | GET    | `/api/data/load-holon/{worldId}` | Returns full holon; read `metaData.cityState` for saved city. |
| Save city state | PUT  | `/api/world/{worldId}/state`      | `{ "cityState": { ... } }`. Ownership checked. |

**Auth:** `POST /api/avatar/authenticate` with `{ "username", "password" }` → use `result.result.jwtToken` in `Authorization: Bearer <token>`.

---

## 2. Test ONODE from the repo

```bash
# From repo root; ONODE must be running (e.g. dotnet run in ONODE/.../WebAPI).
chmod +x test-star-world-api.sh
USER=OASIS_ADMIN PASS=Uppermall1! ./test-star-world-api.sh
# Or with existing JWT:
./test-star-world-api.sh "your-jwt"
```

---

## 3. Clone and run IsoCity (standalone)

```bash
git clone https://github.com/amilich/isometric-city.git
cd isometric-city
npm install
npm run dev
# Open http://localhost:3000
```

IsoCity runs without STAR; save/load uses browser local storage by default.

---

## 4. STAR adapter (what to add to IsoCity)

**Config (env or query):**

- `NEXT_PUBLIC_ONODE_URL` or `?onode=http://localhost:5003`
- JWT: launcher sets `?token=...` or cookie; adapter reads it for API calls.

**Adapter responsibilities:**

1. **List cities:** `GET {ONODE_URL}/api/world/my-worlds` with `Authorization: Bearer {token}`. Map `result` to “Load city” list (id, name).
2. **New city:** `POST {ONODE_URL}/api/world` with `{ "name": "New City", "description": "" }`. Store returned `worldId`; start empty game; on first Save call PUT state.
3. **Load city:** User picks world id → `GET {ONODE_URL}/api/data/load-holon/{worldId}`. If `metaData.cityState` exists, deserialize into IsoCity’s state; else start empty.
4. **Save city:** Serialize IsoCity’s current state (same shape as existing save format). `PUT {ONODE_URL}/api/world/{worldId}/state` with `{ "cityState": <serialized> }`.
5. **Avatar (optional):** `GET {ONODE_URL}/api/avatar/get-avatar-detail-by-id/{avatarId}` to show “Mayor: {username}” and optional portrait. Avatar id can come from JWT decode or a dedicated “me” endpoint.

**Where to hook in IsoCity:**

- **Save:** Find where IsoCity writes save data (e.g. localStorage); add a branch: if STAR config present, POST to ONODE instead (or in addition).
- **Load:** Find where IsoCity reads save list and load; if STAR config present, fetch my-worlds and load-holon, then hydrate game state from `metaData.cityState`.
- **New game:** If STAR config present, call POST /api/world, then start empty with that `worldId`.

---

## 5. Launcher (minimal)

1. User logs in: `POST /api/avatar/authenticate` → JWT + avatar id.
2. Open IsoCity in iframe or new tab with: `?onode=https://your-onode.com&token={JWT}` (or set cookie).
3. IsoCity STAR adapter reads query (or cookie), then uses ONODE for list/create/load/save as above.

---

## 6. References

- Integration plan: `Docs/STAR_ISOCITY_INTEGRATION_PLAN.md`
- IsoCity repo: [amilich/isometric-city](https://github.com/amilich/isometric-city)
- Avatar/games: `Docs/OASIS_AVATAR_OPEN_SOURCE_GAMES_INTEGRATION.md`
