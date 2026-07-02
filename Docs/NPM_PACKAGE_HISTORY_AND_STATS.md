# OASIS JS API Package History & Download Stats

A running historic log of the OASIS JS npm package rename(s), the `@oasisomniverse` scope migration, and download stats snapshots — kept for historic record of the OASIS project's evolution.

## Package Lineage

The WEB 4 JS client has been renamed twice (npm does not support renaming a package in place, so each rename forces a new package to be published):

1. **`oasis-api`** (original name) — first published 2022-03-17
2. **`web4-oasis-api`** (renamed for WEB 4 branding) — first published 2022-08-11
3. **`@oasisomniverse/web4-api`** (current — renamed into the `@oasisomniverse` scope) — published 2026-07-01 at v2.0.0

All WEB 5–WEB 10 JS packages are brand new with no prior published history, so they correctly start at `v1.0.0` (vs. `v2.0.0` for WEB 4, which continues the `web4-oasis-api` version lineage):

| Package | Scope name | Starting version |
|---|---|---|
| WEB 4 | `@oasisomniverse/web4-api` | v2.0.0 (continuation) |
| WEB 5 | `@oasisomniverse/web5-api` | v1.0.0 (new) |
| WEB 6 | `@oasisomniverse/web6-api` | v1.0.0 (new) |
| WEB 7 | `@oasisomniverse/web7-api` | v1.0.0 (new) |
| WEB 8 | `@oasisomniverse/web8-api` | v1.0.0 (new) |
| WEB 9 | `@oasisomniverse/web9-api` | v1.0.0 (new) |
| WEB 10 | `@oasisomniverse/web10-api` | v1.0.0 (new) |

**Why the scope migration happened:** the original npm account's recovery email (`david@nextgensoftware.co.uk`) stopped working after that domain lapsed, locking out publish access to the unscoped names. A support ticket was filed with npm to recover the original account (Plan A — if successful, keep publishing `web4-oasis-api` under its original name to preserve its download history rather than deprecating it). As a Plan B (and because it also makes sense as a permanent naming convention regardless of account recovery), all 7 packages were renamed into the `@oasisomniverse` scope, which:
- Groups the whole WEB 4–WEB 10 family under one consistent, professional namespace.
- Avoids ever being locked out of a name by one personal account/email again (scopes belong to an npm org, which can have multiple owners).
- Uses a flat `web{N}-api` naming convention (not `web5-star-api` / `web6-ai-api` — descriptive detail belongs in `description`/README, not the package name) for visual consistency across the family.

All 7 packages were published to the `@oasisomniverse` scope on 2026-07-01. `oasis-api` and `web4-oasis-api` were then deprecated (not unpublished, to preserve their stats/history) with `npm deprecate`, pointing users at `@oasisomniverse/web4-api`.

**Note on the recovered `nextgensoftware` npm org:** while resolving npm account access, it turned out `oasis-api` and `web4-oasis-api` were already associated with an existing `nextgensoftware` org (via npm's "Add Package" org-linking feature, which grants org members access to an *existing unscoped* package without renaming/scoping it). This is a different mechanism from package scoping — a package only gets an `@scope/` prefix if its literal name is written that way in `package.json`. The `@oasisomniverse` org was created separately and specifically to hold the new scoped `@oasisomniverse/web{4-10}-api` packages.

## Download Stats Snapshot — 2026-06-29

Pulled via the public npm download-counts API (`https://api.npmjs.org/downloads/point/<range>/<package>`). Note: the `point` endpoint silently clamps any requested range to ~18 months, so full lifetime totals for older packages require chaining multiple sub-18-month windows and summing them.

**Commands run:**

```bash
# Find first-publish date for a package
npm view oasis-api time.created
npm view web4-oasis-api time.created

# Lifetime totals chained in <=18 month windows (point endpoint clamps longer ranges)
curl -s "https://api.npmjs.org/downloads/point/2022-03-17:2023-09-17/oasis-api"
curl -s "https://api.npmjs.org/downloads/point/2023-09-18:2025-03-17/oasis-api"
curl -s "https://api.npmjs.org/downloads/point/2025-03-18:2026-06-29/oasis-api"

curl -s "https://api.npmjs.org/downloads/point/2022-08-11:2024-01-31/web4-oasis-api"
curl -s "https://api.npmjs.org/downloads/point/2024-02-01:2025-07-31/web4-oasis-api"
curl -s "https://api.npmjs.org/downloads/point/2025-08-01:2026-06-29/web4-oasis-api"
```

**Results:**

| Package | First published | Window | Downloads |
|---|---|---|---|
| `oasis-api` | 2022-03-17 | 2022-03-17 → 2023-09-17 | 1,087 |
| `oasis-api` | | 2023-09-18 → 2025-03-17 | 288 |
| `oasis-api` | | 2025-03-18 → 2026-06-29 | 595 |
| **`oasis-api` total** | | | **1,970** |
| `web4-oasis-api` | 2022-08-11 | 2022-08-11 → 2024-01-31 | 1,276 |
| `web4-oasis-api` | | 2024-02-01 → 2025-07-31 | 939 |
| `web4-oasis-api` | | 2025-08-01 → 2026-06-29 | 1,470 |
| **`web4-oasis-api` total** | | | **3,685** |
| **Combined WEB 4 lineage total** | | | **5,655** |
| `@oasisomniverse/web4-api` | published 2026-07-01 | — | 0 (just launched) |

For comparison, the existing infra stat shown on the OASIS Omniverse site is **256K+ NuGet downloads** for the C# packages — a much larger, longer-established number from a different ecosystem.

**Other site stats** referenced alongside downloads (NuGet count, number of npm/JS packages, API endpoint count, lines of code) are tracked directly in the site source (`OASISOmniverseSite/index.html` infra-stats section) rather than here, since those are derived from the codebase/package list rather than a point-in-time external API snapshot.

## Decisions Made (2026-06-29)

- **Not adding the JS/npm download count to any site yet.** ~5,655 combined lifetime downloads (~27/week average) is low relative to genuinely popular npm packages, and displaying it next to the existing "256K+ NuGet Downloads" stat would invite an unflattering comparison rather than reinforce the platform's strength. Revisit once the new `@oasisomniverse` scope has accumulated meaningful post-launch traction, and show it then as one combined cross-package total rather than a single package's number.
- **Not folding npm downloads into the NuGet stat either.** Adding ~5.6K to ~256K is a ~2% change nobody would notice, while raising "why are two different ecosystems' counts combined?" questions. Leave the NuGet stat as-is; treat a combined ecosystem total as a future addition once npm's side is no longer negligible.
- Tracking tool of choice for future checks: npm-stat.com or npmtrends.com for a no-script cumulative view; the `api.npmjs.org/downloads/point` API (chained per ~18-month window) for exact historic totals.
