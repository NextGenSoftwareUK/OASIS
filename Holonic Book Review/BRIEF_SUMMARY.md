# One-Paragraph Summary (Quick Handoff)

**From the canonical brief:** [`Docs/holons/DECENTRALIZED_BOOK_REVIEW_HOLON_BRIEF.md`](../Docs/holons/DECENTRALIZED_BOOK_REVIEW_HOLON_BRIEF.md) §8.

---

Build a demo that simulates a **decentralized, holonic book review system**. The **problem:** independent publishers (e.g. Red Hen Press, Small Beer Press) can’t get meaningful reviews on Amazon because reviews are tied to Amazon purchases. The **solution:** treat each book as a **parent holon** and each review as a **child holon**; aggregate via `load-holons-for-parent`. The solution must allow **easy installation in any frontend** and work with the **publisher’s existing tech stack** (WordPress, Shopify, custom sites, etc.)—e.g. a small script + container or lightweight SDK, with “paste and configure” style integration. The demo should include **2+ sample books** tied to real indie publishers, a **way to add a review** (creating a child holon), and a **view that shows aggregated reviews** for a book. Use **real OASIS APIs** where possible; if not, simulate the same parent/child structure and document it. The goal is to show that reviews from any source (publisher site, bookshop, event, reader) can live in one shared holon, be displayed in one place, and be dropped into any publisher site without rewriting their stack.

---

*Use the full brief as the single source of truth when implementing.*
