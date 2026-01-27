# Holonic Book Review — Demo App

Standalone demo that shows **holonic aggregation of book reviews** and uses the indie-publisher problem as the story.

---

## What this demo will do (per implementation plan)

1. **Sample data:** At least 2 sample “books” (parent holons) tied to example independent publishers (Red Hen Press, Small Beer Press, etc.), plus several “reviews” (child holons) per book with different `metaData.source` (Publisher website, Bookshop, Festival, Reader).

2. **Add review:** UI to submit a review for a chosen book (and optional source). Backend/frontend creates a new child holon with `parentHolonId` = that book’s id. After submit, the aggregated list for that book updates.

3. **Aggregated reviews view:** For a selected book, call `load-holons-for-parent(bookHolonId)` and display all reviews (snippet or full text, rating if present). Optional: aggregate rating, count by source. Copy explains this is the shared view for that book, not tied to Amazon or one store.

4. **Storyline:** Short explanation of the problem (indie publishers, Amazon’s review rules) and the solution (reviews as child holons; any source can contribute; one aggregated view). Publisher names from the brief in labels / “Published by” lines.

5. **Technical:** Prefer real OASIS API (`save-holon`, `load-holon`, `load-holons-for-parent`). If the demo runs without a live API, use an in-memory/localStorage simulator that mirrors the same parent/child structure (see [DATA_MODEL.md](../DATA_MODEL.md)).

---

## Planned structure

- **index.html** — Entry page (or SPA shell).
- **oasis-api.js** — OASIS client (reuse/adapt from repo `holonic-demo/oasis-api.js`).
- **simulator.js** — In-memory/localStorage stand-in when API is unavailable.
- **seed-data.js** — Sample books + reviews (and/or script to create them via API).
- **styles / UI** — Problem/solution copy, book selector, “Add review” form, aggregated reviews list.

---

## Quick start

1. **Run the demo:**  
   - Open `index.html` in a browser, or serve the folder:  
     `python3 -m http.server 8000` or `npx http-server`  
   - Then visit `http://localhost:8000` (or the URL shown).

2. **Demo mode (simulator):**  
   - Select “Demo mode (simulator)”. Sample books and reviews from `seed-data.js` are used.  
   - Use “Reset to seed” to restore the initial dataset.

3. **OASIS API mode:**  
   - Select “OASIS API”. The demo calls `http://localhost:5003` by default.  
   - Ensure your ONODE (or OASIS API) is running and that the sample book ids exist and have reviews, or you’ll see empty lists. To pre-seed books and reviews on the API, use the same shapes as in `seed-data.js` and call `POST /api/data/save-holon` for each.

---

## References

- [Implementation plan](../IMPLEMENTATION_PLAN.md) — phases and deliverables.
- [Data model](../DATA_MODEL.md) — book/review holon shapes and API usage.
- [Canonical brief](../../Docs/holons/DECENTRALIZED_BOOK_REVIEW_HOLON_BRIEF.md) — single source of truth.
