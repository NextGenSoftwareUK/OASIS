# Getting People Playing with Holonic Interoperability

## Goal

Several people contributing to a **shared holon** in a database, so they experience shared knowledge and holonic interoperability firsthand.

---

## How It Works (Conceptually)

```
Shared Parent Holon (e.g. "London Facts Collective")
  ├─ Child holon (Alice added: "Big Ben was built in 1859")
  ├─ Child holon (Bob added: "Tower of London dates to 1066")
  ├─ Child holon (Charlie added: "Westminster Abbey is Gothic")
  └─ ... (everyone’s contributions as children of the same parent)
```

- **One parent holon** = one shared “room” or “collective.”
- **Each contribution** = one child holon with `parentHolonId` = that room’s ID.
- **Shared knowledge** = load all children for that parent; everyone sees the same growing set.

---

## Practical Options (Easiest → Most Flexible)

### Option 1: Shared link + simple “add” form (fastest to run)

**Idea:** One pre-created parent holon, one webpage that only “adds a fact” to it.

**Flow:**

1. You (or an admin) create a parent holon via OASIS API and get its `id`.
2. You deploy a minimal page, e.g. `https://yoursite.com/share?id=<parentHolonId>`.
3. Someone opens the link → sees “Add a fact to our shared knowledge” → types and submits.
4. Backend (or frontend with API key) calls `POST /api/data/save-holon` with:
   - `parentHolonId`: the shared room’s id  
   - `name`: e.g. “Fact: [first 50 chars]”  
   - `description` or `metaData.memory`: the full fact  
   - `holonType`: e.g. `"Holon"`  
   - Optionally `metaData.contributorName` or `createdByAvatarId` if you have auth.
5. Another page or section “View shared knowledge” loads `GET /api/data/load-holons-for-parent/{parentHolonId}` and renders all children.

**What people do:** Click link → type fact → submit → (and/or refresh view) see their + others’ facts. No need to understand holons.

**Requirements:**

- OASIS API reachable (your ONODE or public API).
- One backend/edge function or a small server that:
  - Accepts “add fact” and “optional nickname”,
  - Maps that to a child holon with the fixed `parentHolonId`,
  - Calls OASIS save-holon.
- Optionally: auth (e.g. OASIS login) to attach `createdByAvatarId` and show “who added what.”

---

### Option 2: “Collective” mode in the existing Holonic Demo

**Idea:** Reuse the current demo, add a “Shared room” mode that uses one parent holon for many users.

**Flow:**

1. Demo gets a “Room ID” (parent holon id):
   - **Create room:** call save-holon for a new parent, store its id, show “Share this link: …?room=<id>”.
   - **Join room:** open link with `?room=<id>`; demo uses that as `state.parentHolonId`.
2. Each user logs in (OASIS auth).
3. “Agent X learns a fact” still creates a child holon, but now with the **shared** `parentHolonId`.
4. “View shared knowledge” = load-holons-for-parent(roomId) and show all children from all users.

**Changes in demo:**

- URL param or input: `room=<parentHolonId>`.
- If `room` is set, use that as `state.parentHolonId` for all “learns a fact” and for “view knowledge.”
- Optional: show “Room: …” and “X contributors, Y facts” from load-holons-for-parent.

**What people do:** Open shared link → log in → click “Agent A learns a fact” (or “Add a fact”) → see the same shared parent holon growing; they can repeat from multiple devices/people.

---

### Option 3: Dedicated “Shared Holon Playground” app

**Idea:** A small app focused only on “create/join collective → add contributions → see shared knowledge.”

**Screens:**

1. **Landing**
   - “Create a collective” → creates parent holon, redirects to `/c/<parentHolonId>`.
   - “Join a collective” → input or link with `<parentHolonId>`.
2. **Collective page** `/c/<parentHolonId>`
   - **View:** List/cards of all child holons (facts, ideas, etc.) from `load-holons-for-parent`.
   - **Add:** “Add your contribution” → form (text, maybe title) → save child holon with that `parentHolonId`.
   - Optional: “Live” refresh (poll every N s or SSE if you add it).
3. **Auth:** Optional OASIS login to label contributions with avatar/name.

**Tech:**

- Same OASIS endpoints: `save-holon`, `load-holon`, `load-holons-for-parent`.
- Any simple front (e.g. React/Next/Vue or even static + serverless) that calls your API.

**What people do:** Create or open a collective, add entries, refresh and see everyone’s entries in one shared list.

---

### Option 4: Use OASIS Portal (or existing product) as the “face”

**Idea:** Add a “Collectives” or “Shared knowledge” feature to an existing OASIS app (e.g. Portal).

**Flow:**

- “Collectives” in the nav → “Create / Join” using a parent holon id.
- Inside a collective: same as Option 3 (view children, add child holon).
- Uses existing auth and API; you’re just defining a convention: “this parent holon = this collective.”

**What people do:** Same as Option 3, but inside an app they already use.

---

## What You Need from OASIS

- **Endpoints you already have:**
  - `POST /api/data/save-holon` – create/update holon (include `parentHolonId` for children).
  - `GET /api/data/load-holon/{id}` – load one holon.
  - `GET /api/data/load-holons-for-parent/{parentId}` – list all children of the shared parent.
- **Auth:** `POST /api/avatar/authenticate` so contributions can be tied to avatars (optional but good for “who added what”).
- **One shared parent holon:** Create it once, then give its id to the app/link (Options 1–3).

No schema change: you’re using existing parent/child semantics. The “shared knowledge” is “all child holons of this parent.”

---

## Minimal “Shared Holon” API Contract

Use the same shapes you use in the demo; only the source of `parentHolonId` changes (link, create, or join):

**Create shared room (once):**

```json
POST /api/data/save-holon
{
  "name": "London Facts Collective",
  "description": "Shared knowledge from the group",
  "holonType": "Holon"
}
→ returns { id: "<parentHolonId>" }
```

**Add a contribution (per person per fact):**

```json
POST /api/data/save-holon
{
  "parentHolonId": "<parentHolonId>",
  "name": "Fact: Big Ben was built in 1859",
  "description": "Big Ben was built in 1859",
  "holonType": "Holon",
  "metaData": {
    "memory": "Big Ben was built in 1859",
    "contributorName": "Alice",
    "timestamp": "2026-01-26T..."
  }
}
```

**Read shared knowledge:**

```
GET /api/data/load-holons-for-parent/<parentHolonId>/All?loadChildren=false
→ list of child holons (each has name, description, metaData)
```

Display that list as “what’s in the shared holon.”

---

## Practical Rollout (Concrete Steps)

### Phase 1: “One shared holon, one form” (no UI framework)

1. Create one parent holon via API or admin, copy its id.
2. Build a single page:
   - Form: “Your name”, “Your fact”, [Submit].
   - On submit: call save-holon with that parent id and the fact as a child.
   - Below: “Current knowledge” – call load-holons-for-parent, render titles/descriptions.
3. Share the page link (and optionally the “view only” link) with a few people.
4. Run a short “jam”: everyone adds a few facts, then refreshes and sees the shared list.

### Phase 2: “Shared mode” in the existing demo

1. Add `?room=<parentHolonId>` (and “Create room” → use new parent id in URL).
2. When `room` is set, all “learns a fact” writes to that parent and “view” reads from it.
3. Share the “room” link; multiple people open it and add facts.
4. They see the same parent holon and total fact count; you can add “Contributors: N” by counting distinct `createdByAvatarId` or `metaData.contributorName` if you store it.

### Phase 3: Playground or Portal feature

1. Implement Option 3 or 4: full “create/join collective → add → view” flow.
2. Use the same parent/child and load-holons-for-parent logic; only UX and branding change.
3. Optional: “Live” updates via polling or WebSocket/SSE later.

---

## How People “See” Shared Knowledge

- **Shared knowledge** = “all child holons of this parent.”
- **Implementation:** `load-holons-for-parent(parentHolonId)` → list of children → show name/description/metaData in a feed or list.
- **Discovery:** People get the link (e.g. `yoursite.com/c/<parentHolonId>` or `yoursite.com/demo?room=<parentHolonId>`) and open it. No extra “discovery” service is required for a first version.

---

## Summary

| Approach | Effort | Good for |
|----------|--------|----------|
| **Option 1:** Single form + one parent id | Low | Quick workshops, internal tests |
| **Option 2:** Demo + “room” param | Medium | Demos, small groups, reuse of current UI |
| **Option 3:** Standalone Playground | Medium–High | Dedicated “try holonic interoperability” product |
| **Option 4:** Portal “Collectives” | Depends on Portal | Long-term, productized experience |

Fastest path to “several people generating stuff into a shared holon”: **create one parent holon, then add a small “add + view” flow** (Option 1 or 2) and share the link. Reuse the same `save-holon` / `load-holons-for-parent` patterns you already use in the holonic demo.
