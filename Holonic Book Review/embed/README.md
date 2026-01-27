# Holonic Book Review — Embeddable Widget

Lightweight script or SDK so publishers can add **aggregated book reviews** to any site with “paste and configure” integration.

---

## Goal (from implementation plan Phase 6)

- **One script + one container**, or a small SDK with 1–2 lines of config.
- **Config:** At least `bookId` (book holon id) and `apiBase` (OASIS API base URL).
- **Stack-agnostic:** Works with WordPress, Shopify, Squarespace, custom React/Vue/vanilla. No requirement for a specific framework, CMS, or host.

---

## Planned contents

- **book-reviews.js** (or equivalent) — Standalone script that:
  - Finds container(s) with `data-book-id` and `data-api-base`.
  - Calls `load-holons-for-parent(bookId)`.
  - Renders aggregated reviews (and optional aggregate rating) in the container.
- **README.md** (this file) — Usage, attribute names, and minimal example.

Optional:

- **SDK package** (e.g. for npm) with `init({ bookId, apiBase })` and a `renderReviews(container)` (or React/Vue component) that uses the same logic.

---

## Usage (target design)

**Vanilla HTML:**

```html
<div id="book-reviews"
     data-book-id="<bookHolonId>"
     data-api-base="https://api.oasisweb4.com">
  <!-- Widget renders here -->
</div>
<script src="book-reviews.js"></script>
```

**React (conceptual):**

```tsx
import { BookReviews } from '@oasis/holonic-book-reviews';
<BookReviews bookId="..." apiBase="https://api.oasisweb4.com" />
```

Exact API and file names will be set in Phase 6 when the artifact is implemented.

---

## References

- [INSTALLATION.md](../INSTALLATION.md) — Full installation guide and “paste and configure” checklist.
- [Implementation plan](../IMPLEMENTATION_PLAN.md) — Phase 6 deliverables.
