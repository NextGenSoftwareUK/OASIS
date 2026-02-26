# How inventory is hooked into Quake (and why it got complex)

## What you want (and what the backend does)

- **One item per type:** e.g. one "Shells" row with a **Quantity**.
- **On pickup:** increment that quantity (e.g. +25).
- Backend already does this: one `InventoryItem` per type, `Quantity` field, add_item with `stack=true` increments it.

So in theory the UI should just show **one row per type** and a **single qty** — no “grouping”, no “max”, no multiple rows for the same type.

---

## How it’s actually wired (step by step)

### 1. Quake detects a pickup (stats change)

Game code (outside OQuake) updates `cl.stats[STAT_SHELLS]`, `cl.stats[STAT_ARMOR]`, etc. when you pick something up.

### 2. OQuake polls stats every frame and records “events”

```c
// oquake_star_integration.c – called from Host_Frame
void OQuake_STAR_PollItems(void) {
    star_sync_pump();
    // ...
    OQuake_STAR_OnStatsChangedEx(
        poll_prev_shells, cl.stats[STAT_SHELLS],
        poll_prev_nails, cl.stats[STAT_NAILS],
        // ...
        poll_prev_armor, cl.stats[STAT_ARMOR], 1);
}
```

When e.g. `new_shells > old_shells` we don’t just “increment a number”. We **add a new row** to a local list:

```c
// oquake_star_integration.c
if (new_shells > old_shells) {
    q_snprintf(desc, sizeof(desc), "Shells pickup +%d", new_shells - old_shells);
    added += OQ_AddInventoryEvent("Shells", desc, "Ammo");  // <-- adds a ROW
}
```

### 3. Each “event” is stored as a separate row with a unique name

```c
// oquake_star_integration.c – OQ_AddInventoryEvent
q_snprintf(item_name, sizeof(item_name), "%s_%06u", item_prefix, g_inventory_event_seq);
// Result: "Shells_000001", "Shells_000002", "Green Armor_000003", ...
dst->name = item_name;   // e.g. "Shells_000001"
dst->description = "Shells pickup +25";
dst->quantity = 1;       // each ROW has quantity 1 (the delta is in description)
```

So after two shell pickups we have **two rows** in `g_local_inventory_entries`:

- `Shells_000001`  description "Shells pickup +25"
- `Shells_000002`  description "Shells pickup +20"

That’s why we don’t have “one item per type” at the source: we have **one row per pickup event**, each with a unique name so the sync layer can send **separate** add_item calls.

### 4. Sync sends each event to the API (backend does the real “one per type + qty”)

```c
// star_sync.c – background thread
// Strip the _000001 suffix and send base name so the API stacks
memcpy(base_name, n, base_len);   // "Shells_000001" -> "Shells"
star_api_queue_add_item(base_name, local[i].description, ..., qty, qty);
// API: one "Shells" item, Quantity += 25, then += 20 → correct total
```

So the **backend** really does “one item per type, increment qty”. The complexity is only in how we **store and display** things on the client.

### 5. Display list: API list + “pending” local events

When we open the overlay we call:

```c
OQ_RefreshOverlayFromClient();  // get_inventory → fill g_inventory_entries from API
// then
OQ_AppendLocalToDisplay();      // append any g_local_inventory_entries not already in g_inventory_entries
```

So `g_inventory_entries` can look like:

- From API: `Shells` (id set, quantity 50)
- From local: `Shells_000003` (no id, description "Shells pickup +25")

So we can have **several rows** that all represent “Shells”:

- one from the API (one item per type, one qty — backend is correct),
- several from local events on the client (Shells_000001, Shells_000002, …).

### 6. Why “grouping” and “max” exist (client-only)

The UI wants **one line per type** (e.g. “Shells x50”). So we:

1. **Group** rows by “display name”: strip `_000001` from `Shells_000001` and treat it as “Shells”.
2. We then had to decide: for that one row, what **number** do we show?
   - The **backend** returns one item per type with one qty (correct). Our **client** was merging that API row with multiple local event rows (Shells_000001, Shells_000002, …). If we summed every row’s value we could double-count (same pickup represented in both API and local, or multiple local rows). So we used **max** as a quick fix.
   - That’s a client-side hack: we’re still thinking in “multiple rows” instead of “one per type, one qty”.

So the “max” is only there because we’re **merging multiple rows** (API + many local events) into one label on the client. The backend has always been one item per type, one qty.

---

## What “simple” would look like (one item per type, one qty)

Conceptually:

- **One row per type** (Shells, Nails, Green Armor, …).
- **One quantity per row:** when we have that type from the API, use **API qty + sum of unsynced local deltas** for that type. When we only have local events, use **sum of local deltas**.

So:

- **Data:** We still need the local “events” (with unique names) so we can send multiple add_item calls. We don’t change that.
- **Display:** Instead of “group by label and take max”, we **build one row per type** and set:
  - `qty = (API qty for that type) + (sum of OQ_ParsePickupDelta(description) for unsynced local rows of that type)`.

That’s basic maths: one item per type, one qty, increment on pickup — no max, no “grouping” in the sense of “merge several rows and pick a number”.

The next step is to change the display path to use this “one row per type, qty = API qty + pending local deltas” and remove the old group-by-label + max logic.

---

## What we do now (simplified display)

**One row per type. One qty = API qty + sum of unsynced local deltas.**

In `OQ_BuildGroupedRows`: `row_api_qty[row]` holds the API quantity for that type (when entry is from API); `row_local_sum[row]` is the sum of unsynced local deltas. We set `out_values[row] = row_api_qty[row] + row_local_sum[row]` — no max, no double-count.
