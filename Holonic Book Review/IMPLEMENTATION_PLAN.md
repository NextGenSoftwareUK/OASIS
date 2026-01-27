# Holonic Book Review — Implementation Plan

This plan follows the canonical brief: [`Docs/holons/DECENTRALIZED_BOOK_REVIEW_HOLON_BRIEF.md`](../Docs/holons/DECENTRALIZED_BOOK_REVIEW_HOLON_BRIEF.md).

---

## Phase 1: Data model and sample data

**Goal:** Establish book/review holon shapes and seed 2+ sample books with reviews.

1. **Define holon shapes** (see [DATA_MODEL.md](DATA_MODEL.md)):
   - **Book (parent):** `name`, `description`, `metaData` (e.g. `author`, `publisher`, `isbn`, `coverUrl`). This holon’s `id` is the parent for all its reviews.
   - **Review (child):** `parentHolonId` = book id; `name` (e.g. “Review by [Reviewer]”), `description` (full text), `metaData` (`rating`, `reviewerName`, `source`, `date`). Source: “Publisher site”, “Bookshop”, “Event”, “Reader”, “Partner site”, etc.

2. **Create 2–3 sample “books”** (parent holons) using example publishers from the brief:
   - Small Beer Press (Easthampton, MA) — speculative/literary
   - Red Hen Press — nonprofit literary
   - Rescue Press, Dzanc Books, or Sarabande Books — literary/poetry

   Use plausible titles and authors; avoid real ISBNs unless permitted.

3. **Create several sample “reviews”** (child holons) per book, with varied `metaData.source`:
   - e.g. “Publisher website”, “Independent bookshop”, “Festival”, “Reader”.

4. **Document** how to create these via OASIS `save-holon` (or in-memory/localStorage equivalent). Provide a small seed script or fixture that can run against real API or simulator.

**Deliverables:** DATA_MODEL.md updated; seed data or script; at least 2 book holons and several review holons per book.

---

## Phase 2: “Add review” flow

**Goal:** User can submit a review for a chosen book; it is stored as a new child holon.

1. **UI:** Form or modal: book selector (or current book context), review text, optional rating, optional “source” (Publisher site, Bookshop, Event, Reader, etc.).

2. **Backend / API usage:** On submit, create a child holon with:
   - `parentHolonId` = selected book’s holon id
   - `name`, `description`, `metaData` as in DATA_MODEL

   Use OASIS `save-holon` when API is available; otherwise same structure via in-memory/localStorage.

3. **Post-submit:** Aggregated list for that book updates (refresh or simple reload).

**Deliverables:** “Add review” UI; integration with save-holon (or simulator); list refreshes after submit.

---

## Phase 3: “Aggregated reviews” view

**Goal:** For a selected book, show one aggregated list of all reviews.

1. **Data:** Call `load-holons-for-parent(bookHolonId)` (or simulator equivalent). Interpret returned children as reviews.

2. **UI:** For the selected book, display:
   - All reviews (snippet or full text, rating if present)
   - Optional: aggregate rating, count by source
   - Short copy that this is “the” shared view for that book, not tied to Amazon or one store.

3. **Storyline:** Ensure it’s clear that reviews from many sources are combined here.

**Deliverables:** Aggregated reviews view; optional aggregate stats; copy that explains shared, decentralized aggregation.

---

## Phase 4: Storyline and copy

**Goal:** Make problem and solution obvious to a first-time reader.

1. **Problem:** Short explanation of indie publishers and Amazon’s review rules (from brief §1).
2. **Solution:** Short explanation of reviews as child holons under one book parent; any source can contribute; one aggregated view.
3. **Examples:** Use publisher names from brief §3 (Red Hen, Small Beer, Rescue, Dzanc, Sarabande) in labels, “Published by” lines, or dropdowns.

**Deliverables:** Landing or info section with problem/solution; “Published by [Publisher]” and source labels in the UI.

---

## Phase 5: Technical — OASIS vs simulator

**Goal:** Prefer real OASIS API; support offline/unreachable API with a drop-in simulator.

1. **Real API:** Use `save-holon`, `load-holon`, `load-holons-for-parent` (see holonic-demo `oasis-api.js` and ONODE routes for endpoint shapes).
2. **Simulator:** If demo must run without a live API, use in-memory or localStorage with the **same** parent/child structure. Document clearly that it’s a stand-in so swapping to real API later is straightforward.
3. **Config:** Single place (e.g. env or config) for API base URL; “demo mode” or “simulator” flag when API is unavailable.

**Deliverables:** Client/API module that supports both real OASIS and simulator; one-paragraph “swap to real API” note in DATA_MODEL or README.

---

## Phase 6: Embeddable widget and installation docs

**Goal:** Easy installation in any frontend; works with publishers’ existing stacks.

1. **Embeddable piece:**  
   - Option A: One script + one container div; pass `data-book-id` and `data-api-base` (or similar).  
   - Option B: Small SDK (e.g. `init({ bookId, apiBase }); renderReviews(container)`).

2. **Demo integration:** Use the same widget in the demo app so it doubles as the first “paste and configure” example.

3. **Documentation:** [INSTALLATION.md](INSTALLATION.md) should describe at least one concrete path, e.g.:
   - “Vanilla HTML: add one script tag + one div; set data attributes.”
   - “React: npm install (or copy script); 3 lines of config + one component.”

4. **Stack-agnostic:** No requirement for a specific framework, CMS, or host. Call out compatibility with WordPress, Shopify, Squarespace, custom React/Vue/vanilla.

**Deliverables:** Working embed (script or SDK); INSTALLATION.md with 1–2 example stacks and “paste and configure” steps.

---

## Out of scope (per brief)

- User accounts, login, or permissions beyond what’s needed to call the API
- Moderation, spam, or trust scoring
- Payments, purchases, or “verified purchase” logic
- Production UX polish — this is a simulation/demo

---

## Suggested folder layout (after implementation)

```
Holonic Book Review/
├── README.md                 # This project overview
├── IMPLEMENTATION_PLAN.md    # This file
├── DATA_MODEL.md             # Book/review holon shapes + API
├── INSTALLATION.md           # Embed and “paste and configure”
├── demo/                     # Standalone demo app
│   ├── README.md
│   ├── index.html (or SPA entry)
│   ├── oasis-api.js          # Reuse/adapt from holonic-demo
│   ├── simulator.js          # In-memory/localStorage stand-in
│   └── seed-data.js          # Sample books + reviews
└── embed/                    # Embeddable widget
    ├── book-reviews.js       # Script or SDK bundle
    └── README.md             # Embed usage
```

---

## Order of work

1. **Phase 1** — Data model + sample data (unblocks all other phases).
2. **Phase 5** — API client + simulator (unblocks Phase 2 and 3).
3. **Phase 2** — Add review flow.
4. **Phase 3** — Aggregated reviews view.
5. **Phase 4** — Storyline and copy (can be done in parallel with 2/3).
6. **Phase 6** — Embeddable widget + INSTALLATION.md.

After Phase 1 and 5, Phases 2, 3, and 4 can proceed in parallel or in any order that fits the chosen UI structure.
