# Build plan — Pan Galactic Monitor

## P0 — Scaffold ✅

- **`app/`** — Express, `GET /api/data`, `GET /api/site/:id`, `POST /api/sweep`, SSE `/events`.
- **Globe** — pins from `../data/sites.json`; color by `venueType`; larger pins for `active`.
- **Click** — detail panel: summary, cohort, dates, **mock** time pledged + builders, doc scope, links.
- **Left rail** — aggregates + legend.

## P1 — Pan Galactic Timebank

- `PGT_BASE_URL` + server route on PGT (or read-only SQL) returning per-circle: `hoursExchanged7d`, `hoursExchanged30d`, `memberCount`.
- Merge into sweep payload under each site or aggregate domain.

## P2 — Doc pulse

- Optional subprocess or HTTP to Ship-Announcements-style scanner filtered by `planningSprintPathPrefix`.

## P3 — Polish

- Role filters, brief export, Whole-Earth reskin option.
