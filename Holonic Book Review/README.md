# Holonic Book Review

**Decentralized, holonic book review aggregation for independent publishers.**

This project implements the demo described in the canonical brief:

- **Canonical brief:** [`Docs/holons/DECENTRALIZED_BOOK_REVIEW_HOLON_BRIEF.md`](../Docs/holons/DECENTRALIZED_BOOK_REVIEW_HOLON_BRIEF.md)

---

## Problem

Independent book publishers (e.g. Red Hen Press, Small Beer Press, Rescue Press) struggle to collect and display reviews. Amazon ties “verified” reviews to Amazon purchases; publishers who sell via their own sites, bookshops, and events can’t easily show trusted reviews on Amazon. Discoverability and credibility suffer.

## Solution

A **holonic review aggregation** system:

- **One book** = one parent holon.
- **Each review** = one child holon with `parentHolonId` = that book’s holon id.
- **Aggregated reviews** = `load-holons-for-parent(bookHolonId)`.

Reviews can come from any source (publisher site, bookshop, festival, reader) and appear in one shared view. The same aggregated data can be shown on any publisher or partner site.

The solution is **embeddable** in any frontend and works with publishers’ existing tech stacks (WordPress, Shopify, custom sites). Integration is “paste and configure” where possible (e.g. book id + API base URL; render reviews in a container).

---

## What’s in This Folder

| Item | Purpose |
|------|---------|
| **README.md** (this file) | Overview, problem, solution, and pointers. |
| **IMPLEMENTATION_PLAN.md** | Phased build plan and scope aligned to the brief. |
| **DATA_MODEL.md** | Book/review holon shapes and OASIS API usage. |
| **INSTALLATION.md** | “Paste and configure” installation for 1–2 example stacks. |
| **demo/** | Working demo app (sample books, add review, aggregated view). |
| **embed/** | Embeddable widget / script for publisher sites. |

---

## Quick Start (once implemented)

1. **Run the demo:**  
   See [demo/README.md](demo/README.md) for local run and OASIS vs simulated mode.

2. **Embed on a publisher site:**  
   See [INSTALLATION.md](INSTALLATION.md) for script + div or lightweight SDK examples.

3. **Use real OASIS APIs:**  
   Prefer `save-holon`, `load-holon`, `load-holons-for-parent`. If the demo runs without a live API, use an in-memory/localStorage stand-in that mirrors the same parent/child structure (see DATA_MODEL.md).

---

## References

- **Holonic concepts and API:**  
  [`Docs/holons/HOLONIC_ARCHITECTURE_OVERVIEW.md`](../Docs/holons/HOLONIC_ARCHITECTURE_OVERVIEW.md)
- **Shared parent / multi-contributor:**  
  [`Docs/holons/SHARED_HOLON_PLAYGROUND_GUIDE.md`](../Docs/holons/SHARED_HOLON_PLAYGROUND_GUIDE.md)
- **Existing holon demo patterns:**  
  [`holonic-demo/`](../holonic-demo/) — parent/child, save-holon, load-holons-for-parent.

---

## Success Criteria (from the brief)

1. **Problem is clear:** First-time reader understands indie publishers can’t rely on Amazon alone for reviews.
2. **Holonic aggregation is visible:** One “book” has many “reviews” as children; UI shows one aggregated list from `load-holons-for-parent`.
3. **Decentralization is suggested:** Reviews tagged with different sources (publisher site, bookshop, event, etc.) all appear in one place.
4. **Examples feel real:** At least two books attributed to real independent publishers from the brief (§3).
5. **OASIS-consistent:** Parent/child and API usage match the holonic model in `Docs/holons/`.
6. **Easy installation, stack-agnostic:** Designed for script + div or small SDK; at least one “paste and configure” path documented.

---

*Use [DECENTRALIZED_BOOK_REVIEW_HOLON_BRIEF.md](../Docs/holons/DECENTRALIZED_BOOK_REVIEW_HOLON_BRIEF.md) as the single source of truth when implementing.*
