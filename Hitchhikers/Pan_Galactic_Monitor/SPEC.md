# Pan Galactic Monitor — Product & technical spec

**Status:** Draft v0.1  
**Parent concept:** Hitchhiker’s Guide global builder intelligence (fork DNA from [Port OS](https://github.com/NextGenSoftwareUK/PortOS)).  
**Related:** [Pan Galactic Timebank](../Pan_Galactic_Timebank/), [Ship-Announcements](https://github.com/Improbable-Collaborations/Ship-Announcements), *Save the Planet with Zaphod* brief.

---

## 1. Purpose

A **live intelligence view** of Hitchhiker-related activity worldwide:

- **Map-first:** pins where **hackathons, cohorts, and time-bank circles** run — **student unions**, **impact hubs**, **churches** (and similar venues).
- **Click a pin:** surface **what’s occurring there**, **time pledged** (Pan Galactic Timebank), **upcoming/ongoing events**, and **doc/build pulse** where available.
- **Sweep + delta:** periodic refresh (like Port OS) so users see **what changed** since last visit.

---

## 2. User stories

| As a… | I want to… | So that… |
|-------|------------|----------|
| Global steward | See all active locations on one globe/map | I can grasp scale and geography |
| Hub lead | Click my venue | I see pledges, cohort status, and linked docs |
| Builder | Filter by “student union” / “impact hub” / “church” | I find peers like me |
| Visitor | Read a brief export | I can share status offline |

---

## 3. Location model (map pins)

Each **site** is a record driving one map marker (and optional cluster at low zoom).

### 3.1 Required fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | string (UUID) | Stable id |
| `name` | string | Display name (e.g. “Leeds Beckett SU — Guide cohort”) |
| `lat` | number | WGS84 |
| `lng` | number | WGS84 |
| `venueType` | enum | `impact_hub` \| `student_union` \| `church` \| `hackerspace` \| `other` |
| `status` | enum | `planned` \| `active` \| `paused` \| `completed` |

### 3.2 Detail panel (on click / tap)

Shown in a **slide-over or modal** (Crucix-style popup acceptable).

| Section | Content | Source (phase) |
|---------|---------|----------------|
| **Headline** | Name + venue type + status | Registry |
| **What’s occurring** | Short description, cohort name, dates (e.g. 42-day workshop window) | Registry + manual |
| **Time pledged** | Hours exchanged (7d / 30d / all-time) for **circle(s)** bound to this site | PGT API |
| **Builders** | Count of active members in linked circle(s); optional aggregate skills tags | PGT |
| **Activity feed** | Last N doc changes in Planning-Sprint **scoped to this hub** (optional path prefix) | Git scan / Ship-Announcements pattern |
| **Links** | hitchhikers.earth, Notion, Discord, etc. | Registry |

### 3.3 Linking sites ↔ PGT

- Each site may declare `pgtCircleIds: string[]` (UUIDs of Pan Galactic Timebank **circles**).
- If no circle yet, show **“Time bank: not connected”** and still show narrative + events from registry only.

### 3.4 Visual encoding

- **Color by `venueType`** (e.g. hub = teal, SU = violet, church = amber).
- **Pulse / size** by **recent activity** (e.g. exchanges in last 7d) when PGT connected.
- **Planned** sites: smaller or dashed ring until `active`.

---

## 4. Aggregate domains (non-map panels)

Same sweep JSON pattern as Port OS; domains feed **left rail** and **metric tiles**.

| Domain key | KPIs (examples) |
|------------|-----------------|
| `timebank_global` | Total hours pledged (30d), active circles, new members (7d) |
| `sites` | Count by venueType, countries represented |
| `builders_docs` | Files touched (7d), top folders |
| `delta` | Sites with new activity vs previous sweep |

---

## 5. Architecture

```
┌─────────────────────────────────────────────────────────────┐
│  Sweep orchestrator (Node, Port OS–style)                    │
│  Sources: registry (YAML/JSON), PGT API, optional git scan     │
└────────────────────────────┬────────────────────────────────┘
                             ▼
┌─────────────────────────────────────────────────────────────┐
│  Canonical JSON: sites[], domains{}, meta, delta             │
└────────────────────────────┬────────────────────────────────┘
                             ▼
┌─────────────────────────────────────────────────────────────┐
│  Express: GET /api/data, GET /api/sites, SSE /events         │
│  Static: Globe.gl + panels (Jarvis or Whole-Earth reskin)     │
└─────────────────────────────────────────────────────────────┘
```

---

## 6. Data files (MVP)

| File | Role |
|------|------|
| `data/sites.json` | **Source of truth for map pins** until a CMS exists |
| `data/sites.schema.json` | JSON Schema for validation |
| `.env` | `PGT_BASE_URL`, `PLANNING_SPRINT_PATH` (optional) |

Ship **`data/sites.example.json`** with **fictional + clearly labeled sample** pins (and real coordinates only where you approve).

---

## 7. Build phases

| Phase | Deliverable |
|-------|-------------|
| **P0** | Repo scaffold, `sites.json`, static globe with pins, click → detail panel (registry fields only), mock KPIs |
| **P1** | Wire PGT aggregates per `pgtCircleIds`; real time pledged on panel |
| **P2** | Git/doc pulse per site (path rules in registry) |
| **P3** | Role filters, HTML brief export, OASIS holon read (future) |

---

## 8. Aesthetic note

Align with *Save the Planet* brief where possible: **retro / lo-fi / Whole Earth Catalog** — globe can stay **night earth** or shift to **warmer paper tones**; copy leans **Guide** (Don’t Panic, Sub-Etha, Vogons optional humor).

---

## 9. Privacy

- Default **aggregate** metrics on public dashboard; no personal names on map without **opt-in**.
- Churches / SUs: respect **safeguarding** — public site shows **venue + programme**, not minor identifiers.

---

*Next step: implement P0 in `pan-galactic-monitor/` app folder (see README).*
