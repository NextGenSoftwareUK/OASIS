# STAR API & Photo-to-3D-World: Current vs Needed

**Purpose:** Answer advisor question: *“Could we upload a photo and use that to generate a 3D world?”*  
**Date:** January 31, 2026

---

## 1. What Are the World Templates / Data in STAR?

STAR’s “world” layer is **data and structure**, not pre-built 3D scenes.

### Celestial hierarchy (world data)

- **Spaces:** Omniverse → Multiverse → Universe → Dimension → GalaxyCluster → Galaxy → SolarSystem (and Nebula, WormHole, Portal, etc.).
- **Bodies:** GreatGrandSuperStar, GrandSuperStar, SuperStar, Star, Planet, Moon, Asteroid, Comet, etc.
- **HolonType enum:** `STARCelestialBody`, `STARCelestialSpace`, `Planet`, `Moon`, `SolarSystem`, `Galaxy` — used when saving/loading holons.
- **Default IDs** in STARDNA (e.g. “Our World” planet, Sun star) are in `STAR-Mac-Build/STAR ODK/.../STARDNA.json` (`DefaultPlanetId`, `DefaultStarId`, etc.).

### STAR “templates” (what they actually are)

| Type | Location / API | What it is |
|------|----------------|------------|
| **DNA templates** | `DNATemplates/CSharpDNATemplates/`, `CelestialBodyDNA.json` | C#/JSON **structure** for a celestial body’s Zomes/Holons. Used to **generate code/metadata** for a new OAPP — not 3D art or scenes. |
| **OAPP templates** | `OAPPTemplates/Source`, STAR CLI `star create-from-template oapp "My Game" --template "game-template"` | **Project scaffolding**: e.g. “3D Game Template” = skeleton (GameManager, WorldManager, Three.js/Unity). You still build the 3D world. |
| **Templates API** | `GET/POST/PUT/DELETE /api/templates`, `apply`, `clone`, `export` | CRUD for **reusable template configs** (OAPPs, Zomes). No image input. |
| **STAR CLI intents** | `PromptTemplates.cs` (create_nft, create_geonft, create_quest, create_mission) | **Natural-language intents** for STAR/OASIS. No “create_world” yet. |

### Where “worlds” are stored

- **ONODE:** Holons with `HolonType = STARCelestialBody` or `STARCelestialSpace` (or `Planet`, `Moon`) are persisted via **HolonManager** (`DataController.SaveHolon` / `LoadHolon`).
- **Holon** has `Name`, `Description`, and `MetaData` (Dictionary<string, object>) — so we can store e.g. `MetaData["sceneImageUrl"]` or `MetaData["sceneAssetUrl"]` for a generated scene image or future 3D asset.

So: **world templates/data** = celestial hierarchy + DNA/OAPP scaffolding + template CRUD + holon storage. There is **no** built-in “world template” that is a 3D scene or that accepts a photo to generate a world.

---

## 2. How the STAR API Works (Metaverse Generation Today)

The **WEB5 STAR API** is the gamification and metaverse **layer** on top of WEB4 OASIS. It does **not** render or generate 3D geometry. It provides:

- **Game mechanics:** Missions, quests, competition, eggs  
- **Location/AR:** GeoNFTs, map, visit history  
- **Celestial & worlds:** Celestial bodies (planets, stars, moons) and spaces as **data entities** (create/update/explore/colonize via API)  
- **Development:** OAPPs (OASIS Applications), **templates**, runtimes, libraries  

### What “Templates” Mean in STAR

“Metaverse generation using templates” in our docs refers to:

| Kind | What it is | What it is **not** |
|------|------------|---------------------|
| **Templates API** (`/api/templates`) | CRUD for **reusable templates** for OAPPs, Zomes, components. Apply/clone/export template configs. | Not image-based. No photo input. |
| **STAR ODK DNA templates** | C# / metadata **code templates** (CelestialBody, Zome, Holon) used to **generate OAPP/code structure** from STAR DNA. | Not visual. No 3D mesh or scene from an image. |
| **OAPP templates** (e.g. “3d-world”, “metaverse-basic”) | **Project scaffolding**: e.g. “3D Game Template” gives you a game OAPP skeleton (GameManager, PlayerController, WorldManager zomes, Three.js/Unity setup). | You still design and build the 3D world; the template does not create it from a photo. |
| **Celestial Bodies API** | Create/update/list **virtual world objects** (planets, spaces) as **metadata** (name, type, spaceId, etc.). | No mesh/scene/geometry generation. No image input. |

So today, “metaverse generation” = **creating and configuring worlds and apps from predefined templates and APIs**, not **generating a 3D world from a single photo**.

---

## 3. What We Currently Have (Relevant to “Photo” and “World”)

| Capability | Where | Photo / image role |
|------------|--------|--------------------|
| **Image generation** | MCP (Glif, Banana): text-to-image; Glif can use a **reference image** for style. | Input: text (+ optional reference image). Output: **2D image**. Not 3D. |
| **Image-to-video** | ONODE `POST /api/video/generate` (LTX.io): animate **one image** into a video. | Input: photo + optional motion prompt. Output: **video**. Not 3D world. |
| **File upload** | WEB4 Files API: upload to OASIS storage or IPFS (e.g. for NFT images). | Store/use images. No world generation. |
| **Avatar portrait** | WEB4 Avatar API: upload avatar portrait image. | Identity/UI. No 3D world. |
| **GeoNFT mint/place** | STAR/WEB4: mint/place NFTs with **imageUrl** at locations. | Image as NFT art; placement in a map/world. No generation of 3D geometry from the image. |
| **Celestial bodies / spaces** | STAR API: create worlds/spaces as **data** (names, types, hierarchy). | No mesh or scene generation. No photo input. |

So we have **photo → 2D image** (and style reference) and **photo → video**, plus **world data** and **templates for app/world structure**. We do **not** have **photo → 3D world**.

---

## 4. What “Upload a Photo → Generate a 3D World” Would Require

To support “upload a photo and use that to generate a 3D world” we would need:

1. **Image-to-3D / single-image 3D reconstruction**
   - Research/ML capability: e.g. NeRF, 3D Gaussian splatting, or single-image-to-3D models that take a **single image** and produce a **3D representation** (mesh, point cloud, or splat) that can be rendered as a “world” or “scene”.
   - This is **not** in the current OASIS/STAR codebase; it would be a new integration (own model or third-party API).

2. **Pipeline: photo → 3D asset → “world”**
   - Accept upload (photo).
   - Call image-to-3D service.
   - Post-process and optionally place the result in a “world” (e.g. celestial space / OAPP scene).
   - Return or host the 3D asset / scene (e.g. for STAR/OAPP/AR World or a web viewer).

3. **API and product surface**
   - New endpoint(s), e.g. under STAR or ONODE: e.g. `POST /api/world/generate-from-image` or `POST /api/3d/generate-from-image`.
   - Optional: tie output to Celestial Bodies / Spaces (e.g. “world” id) and/or OAPP templates so the generated scene is a first-class “world” in STAR.

4. **Cost / quality / UX**
   - Choice of model/API (latency, cost, quality).
   - Clear UX: “upload photo → get explorable 3D world” (and possibly “use in AR World” or “use as OAPP scene”).

---

## 5. Summary for the Advisor

| Question | Short answer |
|----------|--------------|
| **How does STAR API support metaverse generation today?** | Via **templates and data APIs**: you create worlds (celestial bodies/spaces), OAPPs, and games using **predefined templates** and STAR/WEB4 endpoints. No automatic generation of 3D geometry from a photo. |
| **Can we today “upload a photo and generate a 3D world”?** | **No.** We have image generation (2D), image-to-video, file upload, and world **metadata**/templates—but no **photo → 3D world** pipeline. |
| **What would we need to add?** | **(1)** An **image-to-3D** capability (model or API); **(2)** a **pipeline** that turns an uploaded photo into a 3D asset/world and optionally attaches it to STAR (e.g. celestial space/OAPP); **(3)** **API** and product exposure (e.g. “generate world from photo”). |

---

## 6. Building the 3D World Generator from Prompts Using STAR (Implementation Plan)

**Yes — we can build “prompt → world” using STAR.** Here’s a concrete approach.

### What we reuse

- **STAR world data:** Create a “world” as a **Holon** with `HolonType = STARCelestialBody` (or `Planet` / `STARCelestialSpace`). Store `Name`, `Description`, and e.g. `MetaData["sceneImageUrl"]` (and later `sceneAssetUrl` for 3D).
- **DataController:** `SaveHolon` / `LoadHolon` already persist holons by type — no new persistence layer needed.
- **Image generation:** ONODE already uses **Glif** (e.g. in `VoiceMemoService`) for image generation. Reuse the same pattern (Glif API token from env or OASIS_DNA, POST to Glif) to generate a **scene image** from a text prompt.
- **Optional:** Upload generated image to IPFS via existing Files API so the world has a durable `sceneImageUrl`.

### Minimal v1: Prompt → 2D scene image + STAR world record

1. **New endpoint:** e.g. `POST /api/world/generate-from-prompt`
   - **Request:** `{ "prompt": "cyberpunk city at sunset", "name": "optional world name" }`
   - **Flow:**
     a. Call Glif (or existing image API) with `prompt` to generate a scene image.
     b. Optionally upload image to IPFS via Files API; get URL.
     c. Create a **Holon** with:
        - `HolonType = STARCelestialBody` (or `Planet`)
        - `Name` = request name or derived from prompt (e.g. first 50 chars)
        - `Description` = prompt
        - `MetaData["sceneImageUrl"]` = Glif output URL or IPFS URL
        - `ParentAvatarId` = current avatar (so “my worlds” are per-avatar)
     d. Save via `HolonManager.SaveHolonAsync(holon, avatarId, ...)`.
   - **Response:** `{ "worldId", "name", "description", "sceneImageUrl" }` (and optionally holon payload for clients).

2. **Optional: “create_world” intent in AI parse**
   - Extend `AIController` system prompt (and `PromptTemplates.cs` in STAR CLI) with intent `create_world` and parameters `name`, `description`, `prompt`. Then a separate “execute” step or endpoint can call the world generator with the parsed prompt.

### v2: Photo upload → same pipeline

- **Request:** `POST /api/world/generate-from-prompt` with `{ "prompt": "...", "imageUrl": "..." }` or multipart image.
- Use **image as reference** for Glif (if workflow supports it) to generate a scene image in that style/location, then same as v1: store `sceneImageUrl` on the world holon.

### v3: True 3D (photo/image → 3D world)

- Add an **image-to-3D** step (external API or model): single image → mesh/splat/GLB.
- Upload 3D asset to storage; set `MetaData["sceneAssetUrl"]` (and optionally keep `sceneImageUrl` as thumbnail).
- Same STAR layer: world = Holon (STARCelestialBody/Planet) with metadata pointing at the 3D asset; clients (e.g. AR World, web viewer) load and render it.

### Implemented (v1)

- **ONODE:** `WorldController.cs` with `POST /api/world/generate-from-prompt`; `WorldService.cs` calls Glif to generate a scene image from the prompt, then creates a Holon with `HolonType.STARCelestialBody`, `Name`, `Description`, and `MetaData["sceneImageUrl"]`, saved via `HolonManager.SaveHolonAsync`. Response returns `worldId`, `name`, `description`, `sceneImageUrl`. Requires `GLIF_API_TOKEN` (or `OASIS.AI.Glif.ApiToken` in OASIS_DNA.json) and auth.
- **Startup:** `WorldService` registered as singleton for DI.

### Next (optional)

- **STAR CLI:** Add intent `create_world` and parameters to `PromptTemplates.cs` so natural language (“Create a world: cyberpunk city”) parses to structured params; execution can call ONODE `POST /api/world/generate-from-prompt`.
- **v2:** Optional image upload/reference in request; optional IPFS upload for durable `sceneImageUrl`.
- **v3:** Image-to-3D integration; store `sceneAssetUrl` for 3D mesh/splat.

---

## 7. Are We Pretty Close? Can We Generate a 3D Game World Using Open Source Game Templates?

### Yes — we’re pretty close

- **Done:** Prompt → scene image (Glif) + STAR world record (HolonType.STARCelestialBody with `sceneImageUrl`). So we already “generate a world” from a prompt and store it in STAR.
- **Gap to “3D game world”:** (1) True 3D geometry (image-to-3D or similar) and/or (2) Turning that world into something **playable** inside an open source game or engine.

### Yes — we can generate a 3D game world using open source game templates

The idea: use **open source game/world templates** as a **viewer** (or minimal game shell) that loads **our generated world** (STAR record + scene image, and later 3D assets) by `worldId`. We don’t have to generate the whole game — we generate the **world content** (prompt → image + STAR); the template provides the **engine and UI**.

**What we have today that fits:**

| Piece | What it is | How it helps |
|-------|------------|--------------|
| **STAR OAPP templates** | “3D Game Template”, “metaverse-basic” = project scaffolding (GameManager, WorldManager, Three.js/Unity skeleton) | Can be the **base** for a “world viewer” OAPP: a minimal 3D scene that fetches a world by ID from OASIS and loads `sceneImageUrl` (e.g. as skybox/background or full-screen panorama). |
| **Prompt → world API** | `POST /api/world/generate-from-prompt` → `worldId`, `sceneImageUrl`, name, description | Supplies the **content** that the template will display. |
| **Avatar × open source games** | One OASIS avatar across DOOM, Half-Life, etc. (identity, portrait, karma) | Same avatar can be “player” in a template-based 3D world viewer (e.g. show OASIS avatar in menu/HUD). |

**Concrete path: “3D game world from prompt + open source template”**

1. **Curate (or build) one “world viewer” template**
   - **Option A (web):** Minimal **Three.js** (or Babylon.js) app: single 3D scene that calls OASIS (e.g. `GET /api/data/load-holon/{worldId}` or a thin `GET /api/world/{worldId}`) to get world holon with `sceneImageUrl` (and later `sceneAssetUrl`). Use the image as a **skybox** or **360° background** so the user is “inside” the generated world. No need for full game logic at first — just explore/look around.
   - **Option B (engine):** Small **Unity** or **Godot** “empty world” template: scene with a world loader that fetches world config from OASIS by ID and applies `sceneImageUrl` (e.g. as skybox/cubemap) or, later, loads a 3D asset from `sceneAssetUrl`. Can add OASIS avatar (from [OASIS_AVATAR_OPEN_SOURCE_GAMES_INTEGRATION](OASIS_AVATAR_OPEN_SOURCE_GAMES_INTEGRATION.md)) for identity/portrait.
2. **Pipeline**
   - User: “Create a cyberpunk city world” → `POST /api/world/generate-from-prompt` → get `worldId` + `sceneImageUrl`.
   - User: “Play / open this world” → launch or navigate to the **template** (e.g. web URL or built binary) with `?worldId=...` (or store “last world” in avatar). Template loads world from OASIS and renders the generated scene (image as environment; later 3D).
3. **Result**
   - “Generate a 3D game world using open source game templates” = **generate world content (STAR + image) + render it in an open source template**. The “game world” is the template’s 3D scene driven by our API; no need to generate full game code.

**Optional next steps**

- Add a **GET /api/world/{worldId}** (or use existing `LoadHolon`) so the template has a single “world config” endpoint (name, description, sceneImageUrl, sceneAssetUrl).
- Ship one **reference “world viewer” template** (e.g. Three.js) in the repo or docs and document “generate world → open in template” in the brief.
- Later: **image-to-3D** → store `sceneAssetUrl` (mesh/splat) and have the template load that for true 3D exploration.

---

## 8. What the Generated Image Is; Playable World vs Current Output; OASIS Avatar

### 8.1 What is the Cloudinary image URL? Did we pull it from online?

**No.** The URL you see (e.g. `https://res.cloudinary.com/dzkwltgyd/image/upload/v1769892345/glif-run-outputs/aqkw2bwkb20jvljjv89w.jpg`) is **not** something we pulled from the internet. It is **generated by our pipeline**:

1. **ONODE** receives `POST /api/world/generate-from-prompt` with a text prompt (e.g. “cyberpunk city at sunset”).
2. **WorldService** calls the **Glif.app API** with that prompt; Glif generates a **new 2D image** and uploads it to **Cloudinary** (Glif’s image host).
3. Glif returns the **Cloudinary URL** of that newly created image.
4. We store that URL in the STAR world holon’s `MetaData["sceneImageUrl"]` and return it in the API response.

So: **we create the image via Glif; Cloudinary hosts it.** The image is unique to that generation request.

### 8.2 Playable game world: what we have vs what you need

**What we have today:**

- A **2D scene image** (from Glif) + a **STAR world record** (Holon with `HolonType.STARCelestialBody`) holding `name`, `description`, and `sceneImageUrl`. No levels, no movement, no mechanics.

**What a “playable” game world needs:**

- **Level/structure data:** Missions (e.g. episode) and Quests (levels) with objectives, optional `mapId` or `seed` for which map or procedural level to load.
- **A game client:** A DOOM-style (or other) engine that loads that structure, displays the world (e.g. using `sceneImageUrl` as skybox/menu art), and lets the player move and complete objectives.
- **Progress and rewards:** Quest completion and optional karma/XP updates back into STAR and the avatar.

So the **current API output is the “theme” and world record**, not the full playable experience. To get a playable world you add:

1. **Missions + Quests** (via STAR Missions/Quests APIs or a “generate campaign” endpoint) linked to the world.
2. A **launcher** that loads world + mission + quests and starts the game client with level list and theme URL.
3. A **game client** (DOOM-style or other template) that consumes that data and runs the levels. See `Docs/STAR_DOOM_STYLE_GAME.md` and `Docs/OASIS_AVATAR_OPEN_SOURCE_GAMES_INTEGRATION.md`.

### 8.3 OASIS avatar integrated

**Already in place:**

- The world is created **for the authenticated avatar**: `HolonManager.SaveHolonAsync(..., AvatarId, ...)` uses the avatar from the JWT. So the world is **owned** by that avatar.
- The avatar’s **identity** (id, username, karma, level, portrait, etc.) is available via existing APIs: `POST /api/avatar/authenticate`, `GET /api/avatar/get-avatar-detail-by-id/{id}`.

**What the game/launcher must do to “integrate” your OASIS avatar:**

1. **Authenticate** (e.g. `POST /api/avatar/authenticate`) and get JWT + avatar id.
2. **Load avatar profile** for the game: `GET /api/avatar/get-avatar-detail-by-id/{id}` (or get-by-username) to get username, portrait, karma, level, etc.
3. **Pass that into the game client:** The launcher starts the DOOM-style (or other) client with: level list + world theme (`sceneImageUrl`) **and** avatar (id, display name, portrait URL, karma/level) so the game can show “you” as the player and optionally sync XP/karma on level complete.

So: **OASIS avatar is integrated by having the launcher resolve “who is playing” via the avatar API and pass that identity (and optional stats) into the playable game.** The world record we create is already tied to that avatar; the playable experience ties in the same avatar via the same APIs. See `Docs/OASIS_AVATAR_OPEN_SOURCE_GAMES_INTEGRATION.md` for the minimal “game avatar profile” and launcher flow.

---

## 9. How to Test

### Prerequisites

- **ONODE** running (e.g. `dotnet run` from `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI`; often `http://localhost:5003` and `https://localhost:5004` if ports are overridden).
- **GLIF_API_TOKEN** set (get a token at [glif.app/settings/api-tokens](https://glif.app/settings/api-tokens)) so world generation can call Glif for the scene image.
- **JWT** for an authenticated avatar (required for `POST /api/world/generate-from-prompt`).

### Option A: Test script (world generate only)

From the repo root:

```bash
# 1. Get a JWT (use your ONODE avatar username/password)
export AUTH_TOKEN="your-jwt-here"

# Or login via env and run in one go:
USER=yourusername PASS=yourpassword ./test-world-generate-api.sh

# 2. Set Glif token (required for image generation)
export GLIF_API_TOKEN="your-glif-token"

# 3. Run the test (default: http://localhost:5003)
./test-world-generate-api.sh

# Custom API URL or prompt (use 5003 if your ONODE is bound there):
./test-world-generate-api.sh "" "http://localhost:5003" "hell fortress at night"
```

The script calls `POST /api/world/generate-from-prompt` and prints `worldId` and `sceneImageUrl`. If you have `jq`, it parses the response.

### Option B: curl (manual)

```bash
# 1. Authenticate (replace username/password)
TOKEN=$(curl -s -X POST "http://localhost:5003/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username":"YOUR_USER","password":"YOUR_PASS"}' | jq -r '.result.jwtToken // .result.JwtToken // empty')

# 2. Generate world (requires GLIF_API_TOKEN in ONODE env)
curl -X POST "http://localhost:5003/api/world/generate-from-prompt" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"prompt":"cyberpunk city at sunset","name":"My Test World"}' | jq '.'
```

### Option C: Load the created world

After generating, you get a `worldId` (GUID). Load the holon (world record) via the Data API:

```bash
curl -X GET "http://localhost:5003/api/data/load-holon/WORLD_ID" \
  -H "Authorization: Bearer $TOKEN" | jq '.'
```

Replace `WORLD_ID` with the returned `worldId`. The holon has `name`, `description`, and `metaData.sceneImageUrl`.

### Testing the DOOM-style flow (missions/quests)

If your deployment has Missions and Quests APIs live:

1. Create a world (Option A or B above); note `worldId`.
2. Create a mission: `POST /api/missions` with name e.g. "Episode 1: Hell Fortress", `metadata.worldId` = `worldId`.
3. Create quests (levels): `POST /api/quests` per level with `parentMissionId`, objectives, and optional `metadata.mapId`.
4. Use the test script or launcher (when built) to fetch mission + quests + world and pass them to a DOOM-style client.

If Missions/Quests are not yet deployed on your host, verify with Swagger (`http://localhost:5003/swagger` or your ONODE base URL) which endpoints exist.

---

## 10. Doc References (for internal use)

- WEB5 STAR API overview: `Docs/Devs/docs-new/web5-star-api/overview.md`
- Templates API: `Docs/Devs/API Documentation/WEB5 STAR API/Templates-API.md`
- Celestial Bodies API: `Docs/Devs/docs-new/web5-star-api/celestial-systems/celestial-bodies-api.md`
- STAR ODK / DNA templates: `STAR-Mac-Build/STAR ODK/NextGenSoftware.OASIS.STAR.STARDNA/`, `OASIS_Alpha_Tester_Documentation.md` (Generate OAPP from template, STAR Templates)
- Video (image-to-video): `ONODE/.../Controllers/VideoController.cs`, `MCP/LTX_VIDEO_GENERATION.md`
- Image generation: `MCP/GLIF_IMAGE_GENERATION.md`, `MCP/BANANA_IMAGE_GENERATION.md`
