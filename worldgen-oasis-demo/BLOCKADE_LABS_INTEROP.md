# Blockade Labs + STAR: Skybox-Only Interoperable Worlds

No WorldGen or PLY. Flow: **prompt → Blockade Labs skybox → register as STAR world** with `sceneAssetType: "skybox"`. Clients load the world by ID and display the equirectangular (and optional depth) directly.

---

## What Blockade Labs provides

- **Endpoint**: `POST https://backend.blockadelabs.com/api/v1/skybox` with `prompt`.
- **Auth**: Header `x-api-key` (get key at https://skybox.blockadelabs.com/api).
- **Output** when complete: `file_url` (8192×4096 equirectangular), `thumb_url`, `depth_map_url`.

---

## STAR world (skybox type)

A STAR world holon stores:

- `sceneAssetUrl` – Blockade’s equirectangular image URL (or your own URL if you re-host).
- `sceneAssetType` – `"skybox"`.
- `sceneImageUrl` – thumbnail for previews (e.g. Blockade’s `thumb_url`).
- `metaData.depthMapUrl` – optional, for clients that support depth.

Clients that support `sceneAssetType === "skybox"` load and render the 360° image (and optionally depth) from these URLs.

---

## Testing Blockade + STAR

### Prerequisites

- ONODE running (e.g. `http://localhost:5003`).
- OASIS avatar credentials (username/password).
- Blockade Labs API key: https://skybox.blockadelabs.com/api.

### Steps

1. **Set environment**
   ```bash
   cd worldgen-oasis-demo
   cp .env.example .env
   # Edit .env: set BLOCKADE_LABS_API_KEY and (for STAR) OASIS_USER / OASIS_PASS
   # Or export:
   export BLOCKADE_LABS_API_KEY=your_key
   export OASIS_USER=your_avatar_username
   export OASIS_PASS=your_avatar_password
   ```

2. **Install deps** (no GPU or WorldGen)
   ```bash
   pip install requests
   ```

3. **Create a skybox world and register it in STAR**
   ```bash
   python demo_blockade_star.py --prompt "A calm lagoon at sunset"
   ```
   Or with explicit options:
   ```bash
   python demo_blockade_star.py \
     --api-url http://localhost:5003 \
     --username "$OASIS_USER" --password "$OASIS_PASS" \
     --prompt "A calm lagoon at sunset" \
     --name "Lagoon sunset"
   ```

4. **Note the printed World ID** from the script output.

5. **Load the world via STAR API**
   ```bash
   # Get a JWT first (e.g. from your app or a small auth script), then:
   curl -s -H "Authorization: Bearer YOUR_JWT" \
     "http://localhost:5003/api/data/load-holon/WORLD_ID"
   ```
   The response includes `metaData.sceneAssetUrl` (skybox image), `metaData.sceneImageUrl` (thumb), and optionally `metaData.depthMapUrl`. Your client uses these to display the skybox.

6. **View the world in the browser**  
   Use the built-in skybox viewer so you can look around and (later) add overlays:

   ```text
   Open: worldgen-oasis-demo/skybox-world-viewer.html
   ```

   **By world ID (loads from OASIS):**

   ```text
   file:///path/to/worldgen-oasis-demo/skybox-world-viewer.html?worldId=YOUR_WORLD_ID&apiUrl=http://localhost:5003&username=OASIS_ADMIN&password=YOUR_PASS
   ```

   If your ONODE allows unauthenticated read for `load-holon`, you can omit `username` and `password`. Replace `YOUR_WORLD_ID` with the ID printed by the script (e.g. `3620352f-f131-4f18-a864-44bb67a0c081`). If the skybox image is on another domain (e.g. Blockade), open the viewer via a local HTTP server to avoid CORS issues. **Use Python’s server** so the query string is not stripped (some servers redirect `file.html?params` to `file` and drop the params):

   ```bash
   cd worldgen-oasis-demo && python3 -m http.server 3333
   # Then open: http://localhost:3333/skybox-world-viewer.html?worldId=3620352f-f131-4f18-a864-44bb67a0c081&apiUrl=http://localhost:5003&username=OASIS_ADMIN&password=YOUR_PASS
   ```

   If your server does redirect and drop the query string, put params in the **hash** instead (the hash is kept after redirect):  
   `http://localhost:3333/skybox-world-viewer#username=OASIS_ADMIN&password=YOUR_PASS`  
   The viewer defaults to the Muay Thai gym world and `apiUrl=http://localhost:5003` when no params are present.

   **By direct skybox URL:**

   ```text
   .../skybox-world-viewer.html?skyboxUrl=https://images.blockadelabs.com/...
   ```

   Drag to look around. To add things (stats, credentials, links) you’ll load holons (e.g. gym info, avatar stats) from OASIS and render them as UI overlays or 3D labels in this viewer or in a STAR client that supports skybox worlds.

7. **Verify in a 360° viewer (optional)**  
   Open `sceneAssetUrl` in a browser or 360° viewer to confirm the image loads.

---

## Security

Do not expose `BLOCKADE_LABS_API_KEY` on the frontend. Run `demo_blockade_star.py` (or an equivalent API) on a backend; the frontend calls your backend to create skybox worlds and receives the STAR world ID.

---

## References

- [Blockade Labs API](https://api-documentation.blockadelabs.com/api/)
- `blockade_client.py` – Blockade API helpers.
- `demo_blockade_star.py` – Skybox → STAR registration script.
