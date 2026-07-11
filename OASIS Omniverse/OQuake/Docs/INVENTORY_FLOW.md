# How inventory is hooked into Quake (minimal hooks)

## Design: heavy lifting in C# client

All sync logic, local pickup-delta array, multithreading, and cache merge live in the **C# StarApiClient**. OQuake keeps **minimal hooks** so other games can integrate quickly.

- **On pickup:** OQuake calls `star_api_queue_add_item(name, description, gameSource, itemType, NULL, quantity, 1)`. That enqueues to the C# client; no local array in the engine.
- **To show inventory:** OQuake calls `star_api_get_inventory(&list)`. The C# client returns **API cache + local pending** merged (one row per type, qty = API qty + unsynced pickups). No append step in the engine.
- **Sync:** The C# client’s background worker flushes queued add_item jobs to the API. OQuake does **not** start inventory sync or pass a local list; it only calls `star_sync_pump()` for auth/send/use-item completion.

So:

- **OQuake:** `OQ_AddInventoryEvent` / `OQ_AddInventoryUnlockIfMissing` → `star_api_queue_add_item` only. `OQ_RefreshOverlayFromClient` → `star_api_get_inventory` only.
- **StarApiClient:** Holds the pickup delta (e.g. `_localPending`), merges it into `GetInventoryAsync`, runs a worker that flushes to the API, manages cache and single-flight fetch.

---

## Flow (step by step)

### 1. Quake detects a pickup

Game code updates `cl.stats[STAT_SHELLS]`, `cl.stats[STAT_ARMOR]`, etc. `OQuake_STAR_PollItems()` (from Host_Frame) calls `star_sync_pump()` then `OQuake_STAR_OnStatsChangedEx(...)` (and items-changed) with previous vs current values.

### 2. OQuake queues the pickup to the client (minimal hook)

```c
// oquake_star_integration.c
if (new_shells > old_shells) {
    q_snprintf(desc, sizeof(desc), "Shells pickup +%d", new_shells - old_shells);
    added += OQ_AddInventoryEvent("Shells", desc, "Ammo");
}
// OQ_AddInventoryEvent → star_api_queue_add_item(item_prefix, description, "Quake", item_type, NULL, delta, 1);
```

No local array; no sync started from Quake. The C# client enqueues the job and merges into its local pending map.

### 3. C# client: delta + merge + background flush

- `EnqueueAddItemJobOnly` adds/merges into `_localPending` and wakes the add-item worker.
- `GetInventoryAsync` returns cache (or fetches once) then `MergeLocalPendingIntoInventory` so one row per type = API qty + pending.
- Background worker batches and sends `AddItemCoreAsync` to the API, then invalidates cache.

### 4. Display in OQuake

When the overlay opens or refreshes:

```c
OQ_RefreshOverlayFromClient();  // star_api_get_inventory(&list) → fill g_inventory_entries from list
```

The list is already merged (API + pending) from the C# client. One row per type, one qty. No append step, no local pending array in OQuake.

---

## Summary

| Responsibility              | Where it lives        |
|----------------------------|------------------------|
| Pickup delta array         | C# StarApiClient       |
| Merge API + pending        | C# GetInventoryAsync   |
| Background flush to API   | C# ProcessAddItemJobsAsync |
| Multithreading / batching | C# StarApiClient       |
| Cache (inventory)          | C# StarApiClient       |
| **OQuake**                 | `star_api_queue_add_item` on pickup; `star_api_get_inventory` to load; `star_sync_pump` for auth/send/use |

This keeps Doom/Quake (and future games) as thin as possible: add item on pickup, load inventory when needed.
