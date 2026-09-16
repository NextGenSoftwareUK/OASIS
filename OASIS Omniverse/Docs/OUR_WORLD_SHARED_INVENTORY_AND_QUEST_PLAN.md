# Our World shared inventory and quest integration plan

## Purpose

Our World will use the same avatar inventory and quest contracts as ODOOM, OQuake, and future OGames. A physical item collected in one game can therefore be displayed, transferred, and used in another game without inventing an Our World-only item model.

The existing Endangered Trees panel is retained in the Unity project as an obsolete reference while the shared inventory UI is built. Its runtime calls are commented out; it must not become a second source of inventory state.

## Inventory identity and category contract

An inventory row has a **functional category** and zero or one token identity:

| `NFTId` | `GeoNFTId` | Meaning |
|---|---|---|
| empty | empty | Normal inventory item |
| set | empty | NFT-backed inventory item |
| empty | set | GeoNFT collected from a geographic placement |
| set | set | Invalid; the API rejects this row |

`NFT` and `GeoNFT` are not functional categories and are not members of `InventoryItemType`. Items use categories such as `Weapon`, `Ammo`, `Armor`, `Key`, `Powerup`, `Nature`, `QuestItem`, `Health`, `Monster`, or `Miscellaneous`.

Example: a golden Quake key found in Our World is stored as category `Key`, with `GeoNFTId` set and `NFTId` empty. A minted shotgun is category `Weapon`, with `NFTId` set. The UI may show identity badges (`NFT` or `GeoNFT`) independently of its category tab.

## Source of truth and data flow

1. Our World loads the signed-in avatar inventory from WEB4 `GET /api/avatar/inventory`.
2. A GeoNFT collection request sends the placement id and functional item category.
3. WEB4 creates or updates the inventory row with `GeoNFTId`; it does not use a GeoNFT category or copy the placement id into `NFTId`.
4. Persistence providers retain both optional identifiers. SQLite adds a `GeoNFTId` column; document providers map the field directly.
5. OGEngineClient reads both identifiers and exposes them to native games. Games group by functional category and render token badges from the identifier fields.
6. After a successful collection, Our World refreshes the authoritative inventory. The collected portal is retired only after the API confirms success.

Stack matching must use a stable item identity, not display name alone. The intended key is the canonical item id/type plus game source and token identity. Distinct NFT or GeoNFT instances must never be collapsed merely because their names match. Stackable normal supplies such as ammunition may share a row and increment `Quantity`.

## Our World inventory UI

Build one responsive inventory popup using the shared inventory response:

- Category tabs: All, Weapons, Ammo, Armor, Keys, Powerups, Nature, Quest Items, Health, Monsters, and Misc.
- A consistent list/grid component in every tab, including Nature. Tree items should use the same component because this gives them search, sorting, filters, stacking, selection details, controller/keyboard navigation, and future cross-game actions for free.
- Search across name, description, game source, category, rarity, and tags.
- Sort by name, category, quantity, rarity, acquired date, and source game.
- Filters for source game, normal/NFT/GeoNFT identity, rarity, usable/quest status, and stackability.
- Item cards show image, name, quantity (`x2`), source game, category, and optional `NFT` or `GeoNFT` badge.
- The detail panel shows description, properties, identity, provenance, and valid actions such as use, equip, inspect, transfer, or locate.

Keeping the old tree-specific layout inside a Nature tab would preserve a second rendering path and would make Nature behave differently from every other category. Its useful visual theme can be reused as Nature styling, but the data binding and interaction component should be shared.

## Seed data

Create an idempotent inventory seed tool beside the existing demo quest seed tooling. It should:

- Resolve the target avatar explicitly and fail with a real error when it cannot.
- Upsert deterministic sample items for Weapons, Ammo, Armor, Keys, Powerups, Nature, Quest Items, Health, Monsters, and Misc.
- Include normal items, NFT-backed items, GeoNFT-backed items, and stackable quantities.
- Never create both `NFTId` and `GeoNFTId` on one item.
- Use stable seed keys so repeated runs update the same samples rather than duplicating rows.
- Emit a machine-readable summary of created, updated, unchanged, and failed items.

Representative cross-game cases should include a Quake golden key discovered as an Our World GeoNFT, ammunition stacks, armor stacks, a minted weapon, and the five Endangered Trees Nature items.

The initial tool is `OASIS Omniverse/OGEngineClient/TestProjects/DemoInventorySeed`. Configure it with the same `STARAPI_WEB4_BASE_URL`, `STARAPI_WEB5_BASE_URL`, `STARAPI_USERNAME`, and `STARAPI_PASSWORD` environment variables as `DemoQuestSeed`, then run `dotnet run` from its directory. Seed names are stable and repeated runs report them as unchanged.

Convenience runners matching the demo quest seed are available under `OASIS Omniverse/OGEngineClient/Scripts`: `RUN_DEMO_INVENTORY_SEED.bat`, `run_demo_inventory_seed.ps1`, and `RUN_DEMO_INVENTORY_SEED.sh`. They default to the deployed WEB4 and WEB5 dev APIs.

## Quest UI and WEB5 integration

Our World should consume the same WEB5 quest definitions and progress used by ODOOM and OQuake. The popup should contain:

- tabs or filters for Active, Available, Completed, and Locked quests;
- quest title, description, source game, status, prerequisites, rewards, and progress;
- nested objectives and subquests with completed/current/locked states;
- a clear active quest and active objective, persisted on the avatar profile;
- search and filters for status, game source, location-based objectives, and rewards;
- an optional tracker view suitable for the world HUD;
- linked GeoHotSpot and external handoff actions where supplied by the quest contract.

The Unity UI must bind to the WEB5 response rather than duplicate quest rules. Objective progress is posted through the existing quest API, followed by an authoritative refresh or the documented OGEngineClient cache merge contract.

## Shared OGEngine client implementation

The portable layer is in `OASIS Omniverse/OGEngineClient/Shared` and targets .NET Standard 2.1. Native OGEngineClient references that project directly. Our World includes the same reviewed source through `Scripts/sync_ogengine_shared.ps1`; the script copies it and verifies the SHA-256 hash so Unity cannot silently ship a stale variant.

The shared layer owns the invariants that must be identical in every game: development WEB4/WEB5 endpoint defaults, optional NFT/GeoNFT identity normalization, and objective/quest completion transition ordering. Unity remains the adapter for `UnityWebRequest`, coroutines, and main-thread UI. Native OGEngineClient remains the adapter for `HttpClient`, NativeAOT, and C exports.

WEB5 progress responses include the exact objective IDs/titles completed by that request and the completed quest ID/title. This applies to every progress source supported by the quest API: kills, XP, weapons, ammo, armour, health, keys, powerups, generic or named pickups, and level time. Our World maps those events to the blue chest/audio `OBJECTIVE COMPLETE` sequence and the final `QUEST COMPLETE` celebration.

The old GeoNFT-placement-to-inventory conversion remains commented out in `EnhancedInventoryIntegration.cs` with an obsolete note. It must stay disabled because collected GeoNFTs are canonical WEB4 inventory items whose category is Nature, Weapons, Ammo, Armour, Keys, or another gameplay category. GeoNFT is an optional identity, not a category.

## Delivery phases

1. **Contract and persistence** — add `GeoNFTId`, enforce mutual exclusion with `NFTId`, remove NFT/GeoNFT categories, update providers and OGEngineClient, and migrate SQLite. Development databases may be reset because no live data depends on the old enum numbering.
2. **Seed tool and contract tests** — create idempotent samples; test normal/NFT/GeoNFT validation, provider round trips, stacking identity, and repeatable seeding.
3. **Shared inventory popup** — implement tabs, shared item cards, details, search, filters, sorting, stacking, and identity badges in Our World.
4. **Tree migration** — map the five tree items into Nature, validate collection refresh, then remove the obsolete UI only in a separate approved cleanup after feature parity.
5. **Quest popup** — replace the early static popup with WEB5-backed quest lists, objectives, subquests, prerequisites, active tracking, and progress.
6. **Cross-game verification** — collect a GeoNFT key in Our World, observe it in ODOOM/OQuake, use it in the intended game, and verify quantity/identity remains correct across refresh and restart.

## Implementation status (September 2026)

Phases 1–5 are implemented in the coordinated OASIS, API Core, OGEngineClient, and Our World branches:

- WEB4 inventory now persists separate optional `NFTId` and `GeoNFTId` values, rejects rows containing both, and uses token identity when deciding whether rows can stack.
- `PUT /api/avatar/inventory/{itemId}` provides an owned-row update contract used by the deterministic inventory seed. The seed matches token-backed rows by token id, matches normal rows by functional identity, updates drift, and emits JSON counts for created, updated, unchanged, and failed rows.
- Native `ogengine_item_t` exposes both identifiers. ODOOM and OQuake render `[NFT]`/`[GEONFT]` independently of category; their category tabs now use `item_type` rather than display prefixes. The ABI field is mirrored in the OQuake2, OQuake2-RTX, and OQuake3 headers.
- Our World uses one WEB4-backed inventory popup for every category, including Nature. It includes category and identity filters, search, sorting, quantities, source labels, token badges, details, and remote item thumbnails.
- The previous Endangered Trees inventory rendering path remains commented and marked obsolete as requested. Collected trees are ordinary Nature items in the shared inventory.
- Our World loads the WEB5 avatar quest feed and displays status tabs, search, objectives, the active objective, prerequisites, subquests, rewards, linked GeoHotSpots, and cross-game handoff URIs.

Phase 6 requires running the services and game executables with an authenticated test avatar. The compile-time contract and UI paths are complete; the runtime acceptance flow below remains the release verification checklist because it depends on a live WEB4/WEB5 database and native game builds.

## Acceptance checks

See [Anorak API quest](OUR_WORLD_ANORAK_API_QUEST.md) for the collection-route correction, four/five placement seed, runtime effects, outstanding deployment checks, and recommendation to consolidate Unity integration in OGEngineClient.

- Repeated GeoNFT seed runs do not create overlapping duplicate placements or inventory rows.
- A successful collection returns HTTP 200 with a populated `GeoNFTId`, the expected category, and an empty `NFTId`.
- A request containing both identifiers fails and persists nothing.
- The collected portal disappears after confirmed collection and remains absent after reload.
- All collected tree items appear under Nature from the WEB4 inventory response.
- Category totals and stack quantities match WEB4 after restart.
- Search, sorting, and filters produce the same result regardless of source game.
- Active quest/objective and progress match WEB5 after restart and cross-game beam-in.
