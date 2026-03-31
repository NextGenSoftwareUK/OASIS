# Quest all-for-avatar: Why it returns (or should return) all quests including sub-quests and objectives

This trace explains how **GET /api/quests/all-for-avatar** is designed to return every quest for the avatar (top-level, sub-quests, and objectives), and where to look if only top-level quests appear.

## 1. API endpoint

- **Controller:** `QuestsController.GetAllQuestsForAvatar()`
- **Calls:** `_starAPI.Quests.LoadAllQuestsForAvatarAsync(avatarId)` (fallback: `LoadAllForAvatarAsync(avatarId)`)
- **Returns:** Flat list of `Quest`; no server-side filter by `ParentQuestId`.

## 2. Manager layer (ONODE)

- **QuestManager.LoadAllQuestsForAvatarAsync** → **LoadAllForAvatarAsync(avatarId)** (from base).
- **STARNETManagerBase.LoadAllForAvatarAsync** (e.g. `STARNETManagerBase.cs` ~798):
  - Calls `Data.LoadHolonsByMetaDataAsync` with:
    - `{ "CreatedByAvatarId", avatarId.ToString() }`
    - `{ "Active", "1" }`
  - **No filter on ParentQuestId or ParentHolonId** — the query is “all holons of this type with this metadata”.

So by design, the manager returns every quest (including children) that has `CreatedByAvatarId` and `Active` in metadata.

## 3. Storage provider (MongoDB)

- **MongoDBOASIS.LoadHolonsByMetaDataAsync(Dictionary, ...)** → **HolonRepository.GetHolonsByMetaDataAsync**.
- **HolonRepository** (e.g. `HolonRepository.cs` ~551):
  - Loads **all** holons of the given `HolonType` (and `DeletedDate == min`) from the collection.
  - Then **in-memory** keeps only those where **every** requested metadata key/value matches (e.g. `CreatedByAvatarId`, `Active`).
  - **No filter by parent:** child quests are included if they match the metadata.
- **Null MetaData:** Holons with `MetaData == null` are skipped (they cannot match `CreatedByAvatarId` / `Active`). So child quests **must** have `MetaData` populated when saved or they will not appear in all-for-avatar.

Conclusion: the MongoDB provider does **not** filter out child quests; it returns all matching holons (including children). If children are missing, the most likely cause is that they were saved with **null or missing MetaData** (so they never match the metadata filter).

## 4. Creating child quests (objectives / sub-quests)

- **QuestsController** (e.g. CreateQuestWithOptions, AddQuestObjective, AddSubQuest):
  - Builds the child quest and sets:
    - `subQuest.STARNETDNA.CreatedByAvatarId = AvatarId`
    - `subQuest.MetaData["CreatedByAvatarId"] = AvatarId.ToString()`
    - `subQuest.MetaData["Active"] = "1"`
  - Then calls `_starAPI.Quests.AddQuestAsync(AvatarId, parentId, subQuest)`.
- **QuestManagerBase.AddQuestAsync** (ONODE):
  - Loads parent, adds child to `parent.Quests`, updates parent, sets `quest.ParentHolonId = parentId`, then calls **quest.SaveAsync&lt;T1&gt;()** to persist the child.

So the child is saved via the same OASIS save path as any other holon. For the child to show up in all-for-avatar:

- The holon must be persisted with **MetaData** containing **CreatedByAvatarId** and **Active** (and any other keys the manager uses). If the save pipeline drops or does not persist `MetaData` for that holon, the child will not match the metadata query and will not be returned.

## 5. Demo seed (DemoQuestSeed)

- **Program.cs** uses `client.CreateCrossGameQuestAsync(name, description, objectivesList)`.
- That calls the API endpoint that creates one parent quest and, for each objective, adds to the parent's objectives or creates a **child Quest** with the same `CreatedByAvatarId` / `Active` in MetaData (as in section 4).

So after a successful run of the demo seed:

- The DB should contain both parent quests and child quest (objective) holons.
- **all-for-avatar** should return both, as a flat list, as long as child holons are stored with **MetaData** populated (e.g. CreatedByAvatarId, Active).

## 6. If only top-level quests appear

Check, in order:

1. **Provider in use**  
   Confirm the STAR ODK is using a provider that implements `LoadHolonsByMetaData` like MongoDB (no parent filter). Other providers may behave differently.

2. **Child quests have MetaData when saved**  
   When **AddQuestAsync** runs and **quest.SaveAsync** is called, does the serialization/save path persist the quest’s **MetaData** (including `CreatedByAvatarId`, `Active`) into the store? If MetaData is null or stripped for child holons, they will not match the metadata query.

3. **MongoDB: HolonType**  
   Load uses `HolonType` (e.g. Quest). Ensure child quests are stored with the same `HolonType` as the parent so they are in the same “all holons” set before the metadata filter.

4. **Client fallback**  
   The STAR API client uses a fallback: if no child quests are found in the cache for a parent, it uses the parent’s embedded **Objectives** array to show objectives in the UI. So the UI can still show objectives even when the API returns only top-level quests.

## 7. Summary

| Layer              | Filters by parent? | Notes                                                                 |
|--------------------|--------------------|-----------------------------------------------------------------------|
| API controller     | No                 | Returns full list from manager.                                      |
| LoadAllForAvatarAsync | No              | Query by CreatedByAvatarId + Active only.                            |
| MongoDB repo       | No                 | Loads all holons of type, then filters by metadata; null MetaData skipped. |
| Child creation     | —                  | API sets MetaData; AddQuestAsync then SaveAsync; MetaData must be persisted. |

**Design:** all-for-avatar returns a flat list of **all** quests for the avatar (top-level, sub-quests, objectives). If only top-level appear, the most likely cause is child quests being saved **without** the required **MetaData** (e.g. CreatedByAvatarId, Active) in the storage layer.
