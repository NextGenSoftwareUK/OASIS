# Demo Quest Seed – Data Flow and Persistence

This doc shows how the **Demo Quest Seed** works and how data is persisted so ODOOM/OQuake can see quests, objectives, and prerequisites.

---

## 1. Seed program (what runs)

**Location:** `OASIS Omniverse/STARAPIClient/TestProjects/DemoQuestSeed/Program.cs`

**What it does:**

1. **Authenticates** with the STAR API (WEB5) using env vars: `STARAPI_WEB5_BASE_URL`, `STARAPI_WEB4_BASE_URL`, `STARAPI_USERNAME`, `STARAPI_PASSWORD` (defaults from `StarApiTestDefaults`: e.g. `dellams` / `test!`).
2. **Resolves avatar ID** (via `GetActiveQuestsAsync`), then prints:  
   `Avatar ID for quests: <guid>` — **you must beam in with this same avatar** in ODOOM/OQuake.
3. **Creates 3 demo quests** with objectives via `CreateCrossGameQuestAsync` (name, description, objectives array). Each is sent as **one** `POST /api/quests/create` with an **Objectives** array in the body.
4. **Starts the first quest** so it appears "In Progress" (`StartQuestAsync` → API start endpoint).
5. **Creates Step 1 / Step 2 / Step 3** quests with objectives, then **sets prerequisites**: Step 2 depends on Step 1, Step 3 on Step 2 (`SetQuestPrerequisitesAsync` → GET quest, add `PrerequisiteQuestIds` to metaData, PUT quest).

**Relevant code (objectives and prerequisites):**

```csharp
// CreateCrossGameQuestAsync sends POST /api/quests/create with:
//   Name, Description, HolonSubType, MetaData (CrossGameQuest, Games), Objectives[]

// Each objective: Description, GameSource, ItemRequired (optional), Order

// Prerequisites: client GETs quest, sets metaData.PrerequisiteQuestIds = [step1Id], PUTs quest
```

---

## 2. How persistence works

### Client → API

- **Create quest + objectives:**  
  `StarApiClient.CreateCrossGameQuestAsync` → `POST {baseUrl}/api/quests/create` with JSON body (Name, Description, Objectives array, etc.).
- **Set prerequisites:**  
  `SetQuestPrerequisitesAsync` → `GET /api/quests/{id}` → add `PrerequisiteQuestIds` to `metaData` → `PUT /api/quests/{id}`.

### API → storage

**Create (parent quest):**

- `QuestsController.CreateQuestWithOptions` → `_starAPI.Quests.CreateAsync(AvatarId, request.Name, request.Description, ...)`.
- `STARNETManagerBase.CreateAsync` builds the parent Quest holon (with STARNETDNA, CreatedByAvatarId, etc.) and saves it via **`Data.SaveHolonAsync<T1>(holon, avatarId, ...)`** (i.e. **HolonManager**). For cross-game quests `fullPathToSourceFolder` is empty, so no disk DNA is written; the holon is saved only to the **current storage provider** (e.g. MongoDB).

**Add objectives (child quests):**

- For each `request.Objectives`, the controller adds an objective to the parent quest's `Quest.Objectives` (or the API may create child quest holons for legacy storage). Objectives are stored with `GameSource`, requirement data, and `MetaData["CreatedByAvatarId"]` / `MetaData["Active"]` where applicable.
- In **QuestManagerBase** / **AddQuestAsync** (when adding child quests): load parent, add child, **UpdateAsync(parent)**, then **quest.SaveAsync** so the child is persisted with MetaData.

So each **objective** is persisted either as an item in the parent's **Quest.Objectives** collection or as a child holon with `ParentHolonId = parent Id` and `CreatedByAvatarId` / `Active` in MetaData.

**Prerequisites:**

- `PUT /api/quests/{id}` → `UpdateIQuest` → `_starAPI.Quests.UpdateAsync(AvatarId, quest)`.
- **STARNETManagerBase.UpdateAsync** → **SaveHolonAsync<T1>** → **holon.SaveAsync<T>()** → **HolonManager.SaveHolonAsync**. The quest body from the client includes `metaData.PrerequisiteQuestIds`; that is stored as part of the holon in the same provider.

**Load (all-for-avatar):**

- `GET /api/quests/all-for-avatar` (or by status) → `_starAPI.Quests.LoadAllForAvatarAsync(avatarId)`.
- **STARNETManagerBase.LoadAllForAvatarAsync** → **Data.LoadHolonsByMetaDataAsync<T1>** (HolonManager) with `CreatedByAvatarId` and `Active = "1"`.
- HolonManager calls the **storage provider** (e.g. MongoDB `GetHolonsByMetaDataAsync`), then **MapMetaData** on results. So you get all quests (top-level and child objectives) that belong to that avatar and are active.

**Summary:** All create/update/save paths go through **HolonManager** (generic `SaveHolonAsync<T>` / `LoadHolonsByMetaDataAsync<T>`). Persistence is entirely in the **configured storage provider** (e.g. MongoDB). No separate “seed DB” – the same provider used by the API at runtime stores the seeded quests.

---

## 3. When the seed “works”

For ODOOM/OQuake to show the demo quests (and objectives/prerequisites), all of the following must be true:

1. **Seed ran successfully**  
   Run `DemoQuestSeed` (e.g. `dotnet run --project "OASIS Omniverse/STARAPIClient/TestProjects/DemoQuestSeed/DemoQuestSeed.csproj"`). Check for “Created quest …”, “Started”, “Prerequisite set”, and no errors.

2. **Same avatar when beaming in**  
   The games load quests for the **currently beamed-in avatar**. The seed prints `Avatar ID for quests: <id>`. That must be the same avatar you use when logging in from ODOOM/OQuake (same WEB5 API and same credentials as the seed).

3. **Persistent storage**  
   The STAR API must use a **persistent** storage provider (e.g. **MongoDB**). If the API uses an in-memory or non-persistent provider, restarting the API loses all quests. Check STAR ODK / ONODE config for the default storage provider.

4. **API and URLs**  
   STAR API (WEB5) and OASIS API (WEB4) must be running. The games and the seed must point at the same WEB5 base URL (and auth must be WEB4 as used by the seed).

If “0 quests” appear in the UI, the API logs: *“Compare with seed output (Avatar ID for quests: <id>). If different, beam in with the same avatar. Ensure API uses a persistent storage provider (e.g. MongoDB).”*

---

## 4. Quick reference – code locations

| What | Where |
|------|--------|
| Seed program | `OASIS Omniverse/STARAPIClient/TestProjects/DemoQuestSeed/Program.cs` |
| Client create (with objectives) | `StarApiClient.CreateCrossGameQuestAsync` → POST `/api/quests/create` |
| Client set prerequisites | `StarApiClient.SetQuestPrerequisitesAsync` → GET quest, set `metaData.PrerequisiteQuestIds`, PUT quest |
| API create quest | `QuestsController.CreateQuestWithOptions` → `CreateAsync` then loop `AddQuestAsync` for each objective |
| API add objective | `QuestsController` adds to parent's objectives or calls `AddQuestAsync(AvatarId, parentId, subQuest)` for child quest |
| Persist parent/child | `QuestManagerBase.AddQuestAsync` → `UpdateAsync(parent)` then `quest.SaveAsync<T1>()` → HolonManager |
| Load for avatar | `LoadAllForAvatarAsync` → `Data.LoadHolonsByMetaDataAsync<T1>(CreatedByAvatarId, Active=1)` → provider + MapMetaData |

So yes: the demo seed data is persisted through the same HolonManager/storage path as normal quest operations; it works as long as the seed runs successfully, the same avatar is used in the games, and the API uses a persistent provider (e.g. MongoDB).
