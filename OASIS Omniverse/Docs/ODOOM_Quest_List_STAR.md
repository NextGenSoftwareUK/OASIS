# ODOOM quest list + STAR (how it is meant to work)

This describes the **current, working** integration between the STAR native client (`star_api_get_top_level_quests_string`), C++ (`ODOOM_RefreshQuestCVars`), and ZScript (`odoom_inventory_popup.zs`). **Changing any of the invariants below without updating the rest of the pipeline is how scrolling and selection break.**

## Files

| Layer | File |
|--------|------|
| Serialized quest payload (top-level only) | `OASIS Omniverse/STARAPIClient/StarApiClient.cs` — `TryGetTopLevelQuestsCache` / `star_api_get_top_level_quests_string` |
| Push payload into engine CVars + tracker scan | `OASIS Omniverse/ODOOM/uzdoom_star_integration.cpp` — `ODOOM_RefreshQuestCVars` |
| UI, input, drawing | `OASIS Omniverse/ODOOM/odoom_inventory_popup.zs` |
| CVar declarations | `OASIS Omniverse/ODOOM/odoom_cvarinfo.txt` |

## Wire format (same family as full quest list)

- One line per record, newline-terminated.
- **Main list rows** (what the left column shows) are lines starting with **`Q\t`**. Fields include id, name, description, status, percent (ZScript expects at least 5–6 tab-separated fields; see popup code).
- Embedded objectives use **`O\t`** lines with **exactly six tab-separated fields after `O`:** `id`, `Title`, `Description`, `ProgressSummary`, `done` (`0` / `1`). Blocks may be separated with **`---`**. (STAR `SerializeQuestsForGame` is the reference.)
- The game uses **`star_api_get_top_level_quests_string`** so the **left list is parent quests only**; sub-quests are loaded for the detail panel via other `star_api_get_quest_*` calls from C++ (`ODOOM_RefreshQuestDetailCVars`).

## C++: `ODOOM_RefreshQuestCVars` (critical behavior)

1. **Native buffer**  
   Calls `star_api_get_top_level_quests_string` into a static buffer capped by **`ODOOM_QUEST_LIST_MAX_BYTES`** (currently 16 384). The API copies at most `buf_size - 1` bytes.

2. **`odoom_quest_list` (string CVar)**  
   Engine string CVars have a **small fixed limit**. The code assigns only a prefix capped by **`ODOOM_QUEST_CVAR_MAX_BYTES`** (currently 4 096).  
   **It must never cut mid-line:** if the cap is hit, the assigned length is pulled back to the **last newline** in that prefix so every line in the CVar is complete.

3. **`odoom_quest_count` (int CVar)**  
   Normally this is the number of **`Q\t`** lines found while scanning the **full** native buffer (`n` bytes).  
   **If the string assigned to `odoom_quest_list` is shorter than the full buffer** (`assignLen < n` because of the CVar cap), **`odoom_quest_count` must be recomputed** by counting **`Q\t` lines only inside the assigned prefix**.  
   That keeps “how many quests are in the list string” consistent with “what we actually pushed to the CVar” for any code or debugging that compares them.

4. **Tracker**  
   Title / first incomplete objective for the tracked quest id are derived by scanning the **full** `questBuf` up to `n` (not just the CVar-truncated prefix), then other tracker lines may use `star_api_get_quest_tracker_objectives_string` / `star_api_get_quest_tracker_active_objective_index`.

## ZScript: main quest popup

- **Source of truth for rows** is **`odoom_quest_list`**: split by newlines, keep only lines whose prefix is **`Q\t`**. The ZScript **does not** read `odoom_quest_count` for drawing or scrolling; it uses the parsed lines + status filters.
- **Filters**: `odoom_quest_filter_not_started`, `odoom_quest_filter_in_progress`, `odoom_quest_filter_completed` (toggled with B / N / M while the popup is open).
- **Selection index** `questSelectedIndex` is an index into the **filtered** list, not the raw `Q\t` list.
- **Scroll CVar** `odoom_quest_scroll_offset` is read in `WorldTick` and updated on PgUp/PgDn/Home/End and when moving the selection. **`RenderOverlay` recomputes `drawOffset` from the selection**, keeps the cursor in view, and **writes `odoom_quest_scroll_offset` back** each frame while the quest popup is open.
- **Popup open**: ZScript sets `odoom_quest_popup_open`; C++ **does not** toggle it (same pattern as inventory). Q toggles the list; backspace closes when the detail popup is not open.

## When C++ refreshes quest CVars

Roughly: when the quest popup opens (immediate push + optional background refresh), every **60 frames** while the popup stays open, when a pending quest refresh flag is set, after **K** handling starts a quest, and in other tracker/profile hooks in `uzdoom_star_integration.cpp`. **Do not move refresh to a random tick phase** without checking both inventory capture order and ZScript expectations.

## Invariants — do not break these

1. **`odoom_quest_list` only contains complete lines** (truncate at newline before the CVar limit).
2. **If the CVar string is a truncated prefix of the native buffer, `odoom_quest_count` counts only `Q\t` lines in that prefix** (see `assignLen < n` branch in `ODOOM_RefreshQuestCVars`).
3. **ZScript builds the visible list only from `Q\t` lines** in `odoom_quest_list`; changing the serialization format requires coordinated changes in C# `SerializeQuestsForGame` and ZScript parsers.
4. **`odoom_quest_selected_id` is set every frame** from the current selection so C++ can handle **K** without relying on a one-frame CVar race.
5. **Scroll offset and selection**: changing `RenderOverlay` or `WorldTick` scroll logic for quests requires keeping **selection visible** and **CVar scroll** in sync (same class of bugs as inventory if one side is updated alone).

## Scaling / known limits

- If the serialized top-level list **exceeds `ODOOM_QUEST_LIST_MAX_BYTES`**, the native copy is **truncated**; quests beyond that will not appear until the buffer/API strategy is changed (e.g. larger buffer and/or windowed API).
- If the list **exceeds `ODOOM_QUEST_CVAR_MAX_BYTES`**, only a prefix of quests fits in `odoom_quest_list`; the **`assignLen < n` count logic** avoids a count/string mismatch for that prefix. **Scrolling cannot show quests that were never placed in the CVar string** — fixing that requires a different transport (windowed slice, larger CVar budget, or multiple CVars), not just ZScript tweaks.

## Related

- Inventory uses a similar pattern: total/filtered count + **windowed** lines under a byte cap (`ODOOM_PushInventoryToCVars`). Quest list is **string-based** from STAR serialization; keep behavior parallel only where it shares the same constraints (CVar size, scroll CVar, popup-open refresh).

When touching this system, **grep** `ODOOM_RefreshQuestCVars`, `odoom_quest_list`, and `odoom_quest_scroll_offset` and read the surrounding block in both C++ and ZScript before merging.
