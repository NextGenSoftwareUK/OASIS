# Dev Portal Integration (STAR Web UI → Oportal)

This folder contains the **OASIS Dev Portal** UI ported from the STAR Web UI (`DevPortalPage.tsx`) to vanilla JavaScript for the [Oportal](https://github.com/NextGenSoftwareUK/Oportal) repo.

## Files to copy into Oportal

1. **dev-portal.js** → place in the Oportal repo root (same level as `developer-tools.js`, `star-dashboard.js`).
2. **dev-portal.css** → place in the Oportal repo root, or merge the rules into `styles.css`.
3. **api-oasisApi-devportal-snippet.js** → copy the two methods into `api/oasisApi.js` inside the `oasisAPI` object (optional; if omitted, the Dev Portal uses built-in demo data).

## 1. Add the Dev Portal tab in portal.html

### Tab button (in the portal tabs list with other `data-tab` buttons)

```html
<button type="button" class="portal-tab" data-tab="dev-portal">Dev Portal</button>
```

### Tab content panel (with other `id="tab-*"` panels)

```html
<div id="tab-dev-portal" class="portal-tab-content" style="display: none;">
  <div id="dev-portal-content"></div>
</div>
```

### Script tag (with other module scripts)

```html
<script src="dev-portal.js"></script>
```

### Styles (either link the CSS file or paste into your main stylesheet)

```html
<link rel="stylesheet" href="dev-portal.css">
```

## 2. Load the Dev Portal when the tab is selected

In the same place where you have:

- `if (tabName === 'developer') { loadDeveloperTools(); }`
- `if (tabName === 'star') { initSTARDashboard(); }`

add:

```javascript
if (tabName === 'dev-portal') {
  if (typeof loadDevPortal === 'function') {
    loadDevPortal();
  } else {
    console.error('loadDevPortal not found. dev-portal.js may not be loaded.');
    const container = document.getElementById('dev-portal-content');
    if (container) {
      container.innerHTML = '<p>Dev Portal script not loaded. Please refresh the page.</p>';
    }
  }
}
```

## 3. Optional: STAR Dev Portal API in oasisApi.js

To use live data from your OASIS/STAR backend, add the methods from **api-oasisApi-devportal-snippet.js** into the `oasisAPI` object in `api/oasisApi.js`. Adjust the request paths if your API uses different URLs (e.g. `/star/dev-portal/stats` without the `/api` prefix).

If you do not add these methods or the backend does not implement these endpoints, the Dev Portal will still work using the built-in demo stats and resources.

## Features (aligned with STAR Web UI Dev Portal)

- **Stats cards**: Resources count, total downloads, active developers, average rating.
- **Filters**: Search, category, type, difficulty, grid/list view.
- **Resource cards**: Title, description, type, difficulty, version, tags, rating, downloads, size, author, estimated time; View Details, GitHub, Docs links.
- **Detail modal**: Full description, prerequisites, platforms, languages/frameworks, code examples, Download Now.

## Source

Ported from:

- **STAR ODK** → `NextGenSoftware.OASIS.STAR.WebUI\ClientApp\src\pages\DevPortalPage.tsx`
- **Oportal** target → [NextGenSoftwareUK/Oportal](https://github.com/NextGenSoftwareUK/Oportal)
