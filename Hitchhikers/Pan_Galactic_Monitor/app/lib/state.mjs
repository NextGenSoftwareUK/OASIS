/**
 * Per-site telemetry enrichment.
 *
 * Handles four pin types:
 *   "site"     — venue-based circle; may fetch live PGT data
 *   "pledge"   — individual/org time pledge; telemetry derived from pledgedHours
 *   "exchange" — completed time exchange; telemetry derived from exchangeHours
 *   "guide"    — Hitchhiker Guide project; telemetry from guide-specific fields
 *   "equity"   — dynamic equity agreement; telemetry from equityHours fields
 */

const PGT_BASE_URL = process.env.PGT_BASE_URL?.replace(/\/$/, "") ?? "";

/** Per-site mock telemetry drift (key = site id) */
const hours7d = {};
const hours30d = {};
const members = {};

function walk(key, store, min, max, spread) {
  const cur = store[key] ?? min + Math.random() * (max - min);
  const d = (Math.random() * 2 - 1) * spread;
  const n = Math.max(min, Math.min(max, cur + d));
  store[key] = n;
  return Math.round(n * 10) / 10;
}

function walkInt(key, min, max, spread) {
  const cur = members[key] ?? Math.floor((min + max) / 2);
  const d = Math.floor((Math.random() * 2 - 1) * (spread * 2 + 1));
  const n = Math.max(min, Math.min(max, cur + d));
  members[key] = n;
  return n;
}

async function fetchPGTCircle(circleId) {
  const url = `${PGT_BASE_URL}/api/monitor/circles/${circleId}`;
  const res = await fetch(url, {
    signal: AbortSignal.timeout(5000),
    headers: { Accept: "application/json" },
  });
  if (!res.ok) throw new Error(`PGT API ${res.status} for circle ${circleId}`);
  return res.json();
}

/**
 * Enrich a pin with telemetry.
 * Returns a Promise — caller must await (see sweep.mjs).
 */
export async function enrichSite(site) {
  const id = site.id;
  const pinType = site.pinType ?? "site";

  // ── Pledge pin: telemetry derived from pledgedHours + slight random walk ──
  if (pinType === "pledge") {
    const base = site.pledgedHours ?? 42;
    const used = walk(id + ":used", hours7d, base * 0.05, base * 0.3, base * 0.03);
    return {
      ...site,
      telemetry: {
        hoursPledged: base,
        hoursUsed7d: Math.round(used * 10) / 10,
        activeMembers: 1,
        pgtConnected: false,
        note: "Pledge pin — static from registry",
      },
    };
  }

  // ── Exchange pin: telemetry from exchangeHours ────────────────────────────
  if (pinType === "exchange") {
    const hrs = site.exchangeHours ?? 0;
    return {
      ...site,
      telemetry: {
        hoursPledged7d: hrs,
        hoursPledged30d: hrs,
        hoursExchanged: hrs,
        activeMembers: 2,
        pgtConnected: false,
        note: "Exchange pin — completed time exchange",
      },
    };
  }

  // ── Guide pin: telemetry from guide-specific fields ───────────────────────
  if (pinType === "guide") {
    return {
      ...site,
      telemetry: {
        hoursPledged7d: site.totalHoursCrowdfunded ?? 0,
        hoursPledged30d: site.totalHoursCrowdfunded ?? 0,
        hoursSpent: site.hoursSpent ?? 0,
        activeMembers: site.builderCount ?? 0,
        pgtConnected: false,
        note: `Guide: ${site.guideName ?? site.name}`,
      },
    };
  }

  // ── Equity pin: telemetry from recorded hours ─────────────────────────────
  if (pinType === "equity") {
    return {
      ...site,
      telemetry: {
        hoursPledged7d: site.totalHoursRecorded ?? 0,
        hoursPledged30d: site.totalHoursRecorded ?? 0,
        activeMembers: site.builderCount ?? 0,
        pgtConnected: false,
        note: `Dynamic equity: ${site.equityModel ?? "Slicing the Pie"}`,
      },
    };
  }

  // ── Hackathon pin: telemetry from builder counts and guide hours ──────────
  if (pinType === "hackathon") {
    const guidesLogged = (site.guidesProduced ?? []).reduce((a, g) => a + (g.hoursLogged ?? 0), 0);
    const builderCount = site.buildersCount ?? 0;
    return {
      ...site,
      telemetry: {
        hoursPledged7d: Math.round(walk(id + ":h7d", hours7d, guidesLogged * 0.1, guidesLogged * 0.25, guidesLogged * 0.03) * 10) / 10,
        hoursPledged30d: guidesLogged,
        activeMembers: builderCount,
        pgtConnected: false,
        note: `OpenSERV hackathon — week ${site.cohortWeek ?? "?"} of ${site.cohortTotal ?? "?"}`,
      },
    };
  }

  // ── Builder pin: telemetry from logged hours and commits ──────────────────
  if (pinType === "builder") {
    const hours = site.hoursLogged ?? 0;
    return {
      ...site,
      telemetry: {
        hoursPledged7d: Math.round(walk(id + ":h7d", hours7d, hours * 0.03, hours * 0.15, hours * 0.02) * 10) / 10,
        hoursPledged30d: hours,
        activeMembers: 1,
        pgtConnected: false,
        note: `Builder: ${site.guideOutput ?? site.name}`,
      },
    };
  }

  // ── Site pin: try live PGT fetch, fall back to mock ───────────────────────
  const circleIds = Array.isArray(site.pgtCircleIds) ? site.pgtCircleIds : [];
  const hasCircles = circleIds.length > 0;
  const canFetch = hasCircles && PGT_BASE_URL !== "";

  if (canFetch) {
    try {
      const results = await Promise.all(circleIds.map(fetchPGTCircle));
      const hoursPledged7d = results.reduce((a, r) => a + (r.hoursPledged7d ?? 0), 0);
      const hoursPledged30d = results.reduce((a, r) => a + (r.hoursPledged30d ?? 0), 0);
      const activeMembers = results.reduce((a, r) => a + (r.activeMembers ?? 0), 0);
      return {
        ...site,
        telemetry: {
          hoursPledged7d: Math.round(hoursPledged7d * 10) / 10,
          hoursPledged30d: Math.round(hoursPledged30d * 10) / 10,
          activeMembers,
          pgtConnected: true,
          note: `Live Pan Galactic Timebank (${circleIds.length} circle${circleIds.length > 1 ? "s" : ""})`,
        },
      };
    } catch (err) {
      console.warn(`[pgt] Failed to fetch circle data for site ${id}: ${err.message}`);
    }
  }

  return {
    ...site,
    telemetry: {
      hoursPledged7d: walk(id + ":7", hours7d, 2, 120, 8),
      hoursPledged30d: walk(id + ":30", hours30d, 20, 400, 25),
      activeMembers: walkInt(id + ":m", 4, 85, 3),
      pgtConnected: false,
      note: hasCircles
        ? "Circle IDs set but PGT_BASE_URL not configured — using mock data"
        : "Mock pledge telemetry — add pgtCircleIds when circle exists",
    },
  };
}

export function enrichSiteMock(site) {
  return enrichSite(site);
}

export function resetMockState() {
  Object.keys(hours7d).forEach((k) => delete hours7d[k]);
  Object.keys(hours30d).forEach((k) => delete hours30d[k]);
  Object.keys(members).forEach((k) => delete members[k]);
}
