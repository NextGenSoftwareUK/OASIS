# Holonic Book Review — Installation Guide

This document describes how to add the Holonic Book Review widget to a publisher’s site. The goal is **“paste and configure”** integration that works with **existing tech stacks** (WordPress, Shopify, Squarespace, custom React/Vue/vanilla sites).

---

## Design principles

- **Frontend-agnostic:** No requirement for a specific framework, CMS, or host.
- **Minimal change:** One script + one container (or a small SDK with a few lines of config).
- **Configuration only:** Pass at least **book id** and **API base URL**; render reviews in a container you provide.

---

## Installation paths

### Option A: Vanilla HTML — script + div (implemented)

1. Add one container element with `data-book-id` and `data-api-base`.
2. Load the widget script (`embed/book-reviews.js`). It runs on load and renders aggregated reviews into every element that has both attributes.

**Example:**

```html
<div data-book-id="11111111-1111-4111-a111-111111111111"
     data-api-base="https://api.oasisweb4.com">
  <!-- Widget renders here: "Loading reviews…" then the list -->
</div>
<script src="path/to/book-reviews.js"></script>
```

**Attributes:**

| Attribute | Required | Description |
|-----------|----------|-------------|
| `data-book-id` | Yes | OASIS holon id of the book (parent holon). |
| `data-api-base` | No | OASIS API base URL (default: `https://api.oasisweb4.com`). No trailing slash. |
| `data-show-aggregate` | No | Set to `"false"` to hide the aggregate rating and count line. |

### Option B: React or any framework

Use the same widget by rendering a container with `data-book-id` and `data-api-base`. Load `book-reviews.js`; it will find and fill all such elements. If you add elements after load, call `window.HolonicBookReviews.run()` to re-scan.

```tsx
// Example: render a div that the embed script will fill
<div data-book-id={bookId} data-api-base="https://api.oasisweb4.com" />
// Then load <script src="path/to/book-reviews.js"></script> or ensure it's already on the page.
```

### Option C: WordPress / Shopify / Squarespace

- **WordPress:** Custom block or shortcode that outputs the script + div (Option A); admin fields for `book_id` and `api_base`.
- **Shopify:** App block or section that injects the same script + div; configurable via theme editor.
- **Squarespace:** Code block or embed that adds the script tag and div with `data-book-id` and `data-api-base`.

Same “paste and configure” as Option A: one div + one script.

---

## Configuration parameters

| Parameter | Description | Example |
|-----------|-------------|---------|
| `bookId` | OASIS holon id of the **book** (parent holon). | UUID, e.g. `a1b2c3d4-...` |
| `apiBase` | Base URL of the OASIS API (no trailing slash). | `https://api.oasisweb4.com` or `http://localhost:5003` |

Optional (to be defined in implementation):

- `container` — CSS selector or DOM element for rendering (default: the element with `data-book-id`).
- `maxReviews` — Cap number of reviews shown (e.g. 10).
- `showAggregateRating` — Boolean to show/hide average rating.
- `theme` — `"light"` / `"dark"` / `"auto"` for styling.

---

## “Paste and configure” checklist

For a publisher adding the widget:

1. Obtain the **book holon id** for the edition they want to show reviews for (from OASIS or from the person who created the book holon).
2. Choose **API base URL** (e.g. production OASIS API or a dedicated review-service URL).
3. Add the **script** and **container** (or SDK + component) as in Option A or B.
4. Publish. Reviews for that book will load via `load-holons-for-parent(bookId)` and display in the container.

No login or special permissions are required for **reading** reviews; the brief leaves auth “out of scope” except as needed to call the API. If the publisher’s site allows visitors to **submit** reviews, that flow would use `save-holon` (and may require API keys or auth depending on deployment).

---

## Current status

- **Embed script:** `embed/book-reviews.js` — drop on any page; add a div with `data-book-id` and `data-api-base`; reviews load and render automatically.
- **Paste and configure:** Option A above is the concrete path for vanilla HTML. For React/other frameworks, use the same div + attributes and optionally call `HolonicBookReviews.run()` after dynamic insert.
