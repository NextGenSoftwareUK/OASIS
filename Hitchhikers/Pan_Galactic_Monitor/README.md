# Pan Galactic Monitor

Global **Hitchhiker / builder intelligence** dashboard: **map pins** for hackathons and cohorts (impact hubs, student unions, churches, etc.), **click for local detail** — what’s on, **time pledged** (via [Pan Galactic Timebank](../Pan_Galactic_Timebank/)), and more.

| Doc | Description |
|-----|-------------|
| **[SPEC.md](./SPEC.md)** | Full product/technical spec |
| [data/sites.example.json](./data/sites.example.json) | Example map registry |
| [data/sites.schema.json](./data/sites.schema.json) | JSON Schema for sites |

## Build status

**P0 done:** [app](./app/) — globe pins from `data/sites.json`, click → detail (time pledged mock, cohort, links). Sweeps + SSE + `runs/latest.json`.

```bash
cd Hitchhikers/Pan_Galactic_Monitor/app && npm install && npm start
# → http://localhost:3142
```

**P1:** Wire [Pan Galactic Timebank](../Pan_Galactic_Timebank/) API for real hours.

## Related repos

- [PortOS](https://github.com/NextGenSoftwareUK/PortOS) — sweep/delta/SSE pattern
- [Ship-Announcements](https://github.com/Improbable-Collaborations/Ship-Announcements) — Planning-Sprint doc pulse (phase 2)
