# Decentralized Book Review Platform (Holonic) — Agent Brief

## Purpose of This Document

This brief is for an agent (or team) building a **demo** that simulates how book reviews can be aggregated holonically to help independent publishers. Use it as the single source of truth for problem, solution, examples, and scope.

---

## 1. Problem Statement

**Independent book publishers struggle to collect and display reviews.**

- **Amazon’s rules:** Reviews on Amazon are only “verified” if the book was bought on Amazon. Non‑verified reviews are allowed from other sources but still require the reviewer to have spent ≥$50 on Amazon in the past 12 months.
- **Effect:** Publishers who sell mainly through their own sites, bookshops, festivals, or other channels cannot easily gather “verified” or prominent reviews on Amazon. Their books look unreviewed or less trusted.
- **Result:** Discoverability and credibility suffer; indie publishers need a way to **aggregate reviews from any source** (their site, bookshops, events, reader emails, etc.) and expose that as shared, trustworthy data.

---

## 2. Solution Concept: Decentralized, Holonic Review Aggregation

**Idea:** A system where **reviews live in a shared holon** instead of being locked to one platform (e.g. Amazon).

- **One “book” or “edition”** = one parent holon (e.g. “*Title* by Author, Publisher, ISBN”).
- **Each review** = one child holon of that parent, with `parentHolonId` = the book’s holon id.
- **Aggregated reviews** = “all child holons of this parent” → load via `load-holons-for-parent(bookHolonId)`.
- **Decentralized:** Any app (publisher site, bookstore, reader app, future “review hub”) can:
  - **Write** reviews as child holons under the same book parent (if they have the parent id and permission/model).
  - **Read** the same aggregated list via the same API, without going through Amazon or any single platform.

Benefits we want the demo to illustrate:

- Reviews can come from **any source** (publisher site, events, other retailers, direct from readers).
- They are **aggregated in one place** (the parent holon), so one “review score” or “review list” can be built from many sources.
- The **same aggregated data** can be shown on publisher sites, partner sites, or future discovery tools.
- **Independent publishers** are no longer reliant on a single platform’s review rules to show that their books are reviewed.

**Installation and publisher tech stack:**

- The solution must support **easy installation in any frontend** (e.g. a small script, embed, or lightweight SDK that can be dropped into an existing site).
- It must **work with the publisher’s existing tech stack** — no requirement to adopt a specific framework, CMS, or hosting. Publishers may use WordPress, Shopify, Squarespace, custom React/Vue/vanilla sites, or other platforms; the review widget or integration should be addable without rewriting their stack.
- Design the demo (and any embeddable component) so that integration is **“paste and configure”** where possible (e.g. pass a book id and API base URL; render reviews in a container). Document what “easy installation” looks like for 1–2 example stacks (e.g. “add one script tag + one div” or “npm install + 3 lines of config”).

---

## 3. Independent Book Publishers to Use as Examples

Use these real independent presses as **concrete examples** in copy, sample data, and UI (e.g. “Published by Red Hen Press”, “Small Beer Press title”):

| Publisher           | Location / note | Use in demo as |
|---------------------|-----------------|-----------------|
| **Small Beer Press** | Easthampton, MA | Speculative/literary fiction; “small press” example |
| **Red Hen Press**   | Nonprofit literary | “Biggest little indie”; poetry/literary |
| **Rescue Press**    | Poetry, prose, hybrid | Independent literary; 2024 releases |
| **Dzanc Books**     | Literary fiction since 2006 | Award‑winning small press |
| **Sarabande Books** | Louisville, KY; poetry/short fiction/essays | 30‑year indie; prize editions |

Suggestions for the demo:

- Create **2–3 sample “books”** attributed to these publishers (e.g. “*Example Title* by [Author], Red Hen Press”).
- Use **real publisher names** and plausible genres; avoid real ISBNs/titles unless you have permission.
- Optionally add short “Publisher story” lines (e.g. “Can’t get verified Amazon reviews when most sales are at conferences and indie bookshops”) to make the problem tangible.

---

## 4. Holonic Model for the Demo

**Conventions:**

1. **Book / edition holon (parent)**  
   - One holon per book (or per edition, if you want to split hardback/ebook).  
   - Fields we care about: `name`, `description`, `metaData` (e.g. `author`, `publisher`, `isbn`, `coverUrl`).  
   - This holon’s `id` is the **parent id** for all its reviews.

2. **Review holon (child)**  
   - One holon per review.  
   - `parentHolonId` = the book holon’s id.  
   - Fields we care about: `name` (e.g. “Review by [Reviewer]”), `description` (full review text), `metaData` (e.g. `rating`, `reviewerName`, `source`, `date`).  
   - “Source” can be: “Publisher site”, “Bookshop”, “Event”, “Reader”, “Partner site”, etc., to show decentralization.

3. **Aggregation**  
   - “All reviews for this book” = `load-holons-for-parent(bookHolonId)`.  
   - From that list you can compute average rating, counts by source, and show the list.

**No change to generic OASIS holon APIs:** use existing `save-holon` and `load-holons-for-parent`. The “book review platform” is a convention (parent = book, children = reviews) on top of existing holons.

---

## 5. What to Build (Demo Scope)

**Objective:** A **working demo** that simulates holonic aggregation of book reviews and uses the indie‑publisher problem as the story.

**In scope:**

1. **Data model**
   - At least **2 sample “books”** (parent holons) tied to the example independent publishers above.
   - **Several sample “reviews”** (child holons) per book, with different `metaData.source` (e.g. “Publisher website”, “Independent bookshop”, “Festival”, “Reader”).

2. **“Add review” flow**
   - UI where a user can submit a review for a chosen book (and optionally “source”).
   - Backend (or frontend calling OASIS) creates a **new child holon** with `parentHolonId` = that book’s id.
   - After submit, the aggregated list for that book updates (refresh or simple reload).

3. **“Aggregated reviews” view**
   - For a selected book, call `load-holons-for-parent(bookHolonId)` and display:
     - All reviews (snippet or full text, rating if present).
     - Optional: aggregate rating, count by source.
   - Show that the same list is “the” shared view for that book, not tied to Amazon or one store.

4. **Storyline and copy**
   - Short explanation of the **problem** (indie publishers, Amazon’s review rules).
   - Short explanation of the **solution** (reviews as child holons under one book parent; any source can contribute; one aggregated view).
   - Use **publisher names** from §3 in labels, dropdowns, or “Published by” lines.

5. **Technical**
   - Prefer **real OASIS API** (save-holon, load-holon, load-holons-for-parent) where possible.
   - If the demo must run without a live API, **simulate** the same structure in memory/localStorage and document that it’s a stand‑in for the real holon API (so swapping to real API later is straightforward).

6. **Frontend-agnostic, easy installation**
   - The solution should be **embeddable in any frontend** and work with **publishers’ existing tech stacks** (WordPress, Shopify, custom sites, etc.).
   - Include a clear “installation” path: e.g. a small script + container, or a minimal SDK with 1–2 example integrations (e.g. “vanilla HTML” and “React”). The demo or docs should show what “easy installation” looks like so a publisher could add it without changing their overall stack.

**Out of scope for this demo:**

- User accounts, login, or permissions (beyond what’s needed to call the API).
- Moderation, spam, or trust scoring.
- Payments, purchases, or “verified purchase” logic.
- Production UX polish; this is a **simulation/demo** to show holonic aggregation and the use case.

---

## 6. Success Criteria

The demo is successful if:

1. **Problem is clear:** A first‑time reader understands that indie publishers can’t rely on Amazon alone for reviews.
2. **Holonic aggregation is visible:** The same “book” has many “reviews” as children of one parent; the UI shows one aggregated list built from `load-holons-for-parent`.
3. **Decentralization is suggested:** Reviews can be tagged with different “sources” (publisher site, bookshop, event, etc.) and all appear in one place.
4. **Examples feel real:** At least two books are clearly attributed to real independent publishers from §3.
5. **Implementation is consistent with OASIS:** Parent/child and API usage match the holonic model in `Docs/holons/` (e.g. HOLONIC_ARCHITECTURE_OVERVIEW.md, SHARED_HOLON_PLAYGROUND_GUIDE.md).
6. **Easy installation, stack-agnostic:** The solution is designed so it can be installed in any frontend (script + div, or small SDK) and works with publishers’ existing tech stacks; the brief or demo docs describe at least one concrete “paste and configure” installation path.

---

## 7. References for the Agent

- **Holonic concepts and API usage:**  
  `Docs/holons/HOLONIC_ARCHITECTURE_OVERVIEW.md`  
  `Docs/holons/AGENT_INTEROPERABILITY_HOLONIC_ARCHITECTURE.md`
- **Shared parent / multi-contributor pattern:**  
  `Docs/holons/SHARED_HOLON_PLAYGROUND_GUIDE.md`
- **Existing holon demo (patterns to reuse):**  
  `holonic-demo/` (especially parent/child, save-holon, load-holons-for-parent, and how a “shared parent” is used).

---

## 8. One-Paragraph Summary for Quick Handoff

**“Build a demo that simulates a decentralized, holonic book review system. The problem: independent publishers (e.g. Red Hen Press, Small Beer Press) can’t get meaningful reviews on Amazon because reviews are tied to Amazon purchases. The solution: treat each book as a parent holon and each review as a child holon; aggregate via load-holons-for-parent. The solution must allow easy installation in any frontend and work with the publisher’s existing tech stack (WordPress, Shopify, custom sites, etc.)—e.g. a small script + container or lightweight SDK, with ‘paste and configure’ style integration. The demo should include 2+ sample books tied to real indie publishers, a way to add a review (creating a child holon), and a view that shows the aggregated reviews for a book. Use real OASIS APIs where possible; if not, simulate the same parent/child structure and document it. The goal is to show that reviews from any source (publisher site, bookshop, event, reader) can live in one shared holon, be displayed in one place, and be dropped into any publisher site without rewriting their stack.”**

---

*End of brief. Use this document as the canonical spec when implementing the decentralized book review holon demo.*
