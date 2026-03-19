import { randomUUID } from "node:crypto";
import { loadSitesJson } from "./loadSites.mjs";
import { enrichSite } from "./state.mjs";
import { scanPlanningSprint } from "./planningPulse.mjs";
import { buildActivityLayers, planningRootFromEnv } from "./activityFeed.mjs";

const VENUE_LABEL = {
  impact_hub: "Impact hubs",
  student_union: "Student unions",
  church: "Churches",
  hackerspace: "Hackerspaces",
  other: "Other venues",
};

const PIN_TYPE_LABEL = {
  site:     "Active sites",
  pledge:   "Time pledges",
  exchange: "Exchanges",
  guide:    "Guides",
  equity:   "Equity agreements",
};

function countBy(sites, key) {
  const m = {};
  for (const s of sites) {
    m[s[key]] = (m[s[key]] || 0) + 1;
  }
  return m;
}

function sum(arr, fn) {
  return arr.reduce((a, s) => a + (fn(s) || 0), 0);
}

export async function runSweep() {
  const { sites: rawSites, sourcePath } = await loadSitesJson();
  const sites = await Promise.all(rawSites.map(enrichSite));
  const planning = await scanPlanningSprint(planningRootFromEnv());
  const activity = buildActivityLayers(sites, planning);

  const byVenue   = countBy(sites, "venueType");
  const byStatus  = countBy(sites, "status");
  const byPinType = countBy(sites, "pinType");
  const countries = new Set(sites.map((s) => s.country).filter(Boolean)).size;

  // ── Aggregate time stats by pin type ──────────────────────────────────────
  const pledgePins    = sites.filter((s) => s.pinType === "pledge");
  const exchangePins  = sites.filter((s) => s.pinType === "exchange");
  const guidePins     = sites.filter((s) => s.pinType === "guide");
  const equityPins    = sites.filter((s) => s.pinType === "equity");
  const sitePins      = sites.filter((s) => s.pinType === "site" || !s.pinType);
  const hackathonPins = sites.filter((s) => s.pinType === "hackathon");
  const builderPins   = sites.filter((s) => s.pinType === "builder");

  const totalHoursPledged     = sum(pledgePins,   (s) => s.pledgedHours ?? 0);
  const totalHoursExchanged   = sum(exchangePins,  (s) => s.exchangeHours ?? 0);
  const totalGuideCrowdfund   = sum(guidePins,     (s) => s.totalHoursCrowdfunded ?? 0);
  const totalEquityHours      = sum(equityPins,    (s) => s.totalHoursRecorded ?? 0);
  const totalGuideSpent       = sum(guidePins,     (s) => s.hoursSpent ?? 0);

  // Hackathon / OpenSERV Builders Fund aggregates
  const totalHackathonBuilders  = sum(hackathonPins, (s) => s.buildersCount ?? 0);
  const totalHackathonGuidesLogged = hackathonPins.reduce(
    (acc, s) => acc + (s.guidesProduced ?? []).reduce((a, g) => a + (g.hoursLogged ?? 0), 0), 0
  );
  const totalHackathonFund = sum(
    hackathonPins.filter((s) => s.buildersGrantStatus === "disbursed" || s.buildersGrantStatus === "confirmed"),
    (s) => s.buildersGrantAmount ?? 0
  );
  const totalBuilderCommits = sum(builderPins, (s) => s.github?.recentCommits ?? 0);
  const totalBuilderHours   = sum(builderPins, (s) => s.hoursLogged ?? 0);
  const liveTokens          = builderPins.filter((s) => s.token?.status === "live").length;

  // Site-level 7d/30d telemetry (includes live PGT if available)
  const totalSiteHours7d    = sum(sitePins,      (s) => s.telemetry?.hoursPledged7d ?? 0);
  const totalSiteHours30d   = sum(sitePins,      (s) => s.telemetry?.hoursPledged30d ?? 0);
  const totalSiteMembers    = sum(sitePins,      (s) => s.telemetry?.activeMembers ?? 0);

  const totalBuilders = sum(guidePins, (s) => s.builderCount ?? 0)
    + sum(equityPins, (s) => s.builderCount ?? 0)
    + totalSiteMembers
    + totalHackathonBuilders
    + builderPins.length;

  const domains = {
    sites: {
      label: "Map pins",
      kpis: [
        { id: "pin_count", label: "Total pins",         value: sites.length,             unit: "pins" },
        { id: "countries", label: "Countries",          value: countries,                unit: "regions" },
        { id: "pledges",    label: "Pledge pins",         value: byPinType.pledge     || 0, unit: "pledges" },
        { id: "exchanges",  label: "Exchange pins",       value: byPinType.exchange   || 0, unit: "exchanges" },
        { id: "guides",     label: "Guide pins",          value: byPinType.guide      || 0, unit: "guides" },
        { id: "equity",     label: "Equity agreements",   value: byPinType.equity     || 0, unit: "agreements" },
        { id: "hackathons", label: "SERV hackathons",     value: byPinType.hackathon  || 0, unit: "events" },
        { id: "builders",   label: "SERV builders",       value: byPinType.builder    || 0, unit: "builders" },
      ],
    },
    timebank_global: {
      label: "Time bank network",
      kpis: [
        { id: "pledged_total",   label: "Hours pledged (total)",    value: totalHoursPledged,                 unit: "h" },
        { id: "exchanged_total", label: "Hours exchanged",           value: totalHoursExchanged,               unit: "h" },
        { id: "guide_crowdfund", label: "Guide hours crowdfunded",   value: totalGuideCrowdfund,               unit: "h" },
        { id: "guide_spent",     label: "Guide hours deployed",      value: totalGuideSpent,                   unit: "h" },
        { id: "equity_hours",    label: "Equity-recorded hours",     value: totalEquityHours,                  unit: "h" },
        { id: "h7",              label: "Site hours (7d, live)",     value: Math.round(totalSiteHours7d * 10) / 10, unit: "h" },
        { id: "builders",        label: "Active builders",           value: totalBuilders,                     unit: "people" },
      ],
    },
    openserv_fund: {
      label: "OpenSERV Builders Fund",
      kpis: [
        { id: "hackathon_count",   label: "Active hackathons",      value: hackathonPins.length,               unit: "events" },
        { id: "hacakthon_builders",label: "Hackathon builders",      value: totalHackathonBuilders,             unit: "builders" },
        { id: "hackathon_hours",   label: "Hackathon hours logged",  value: totalHackathonGuidesLogged,         unit: "h" },
        { id: "fund_disbursed",    label: "Fund disbursed (approx)", value: totalHackathonFund,                 unit: "GBP" },
        { id: "builder_pins",      label: "Tracked builders",        value: builderPins.length,                 unit: "builders" },
        { id: "builder_commits",   label: "Recent commits",          value: totalBuilderCommits,                unit: "commits" },
        { id: "builder_hours",     label: "Builder hours logged",    value: totalBuilderHours,                  unit: "h" },
        { id: "live_tokens",       label: "Live tokens on launchpad",value: liveTokens,                         unit: "tokens" },
      ],
    },
    builders_docs: {
      label: "Planning-Sprint pulse",
      kpis: [
        {
          id: "docs_7d",
          label: "Markdown touched (7d)",
          value: planning.scanned ? planning.count7d : 0,
          unit: "files",
        },
        {
          id: "planning_scan",
          label: "Corpus scan",
          value: planning.scanned ? planning.totalTouched || 0 : 0,
          unit: "recent .md",
        },
        {
          id: "feed_items",
          label: "Activity stream items",
          value: activity.feedStream?.length ?? 0,
          unit: "events",
        },
      ],
    },
  };

  const venueKpis = Object.entries(VENUE_LABEL).map(([id, label]) => ({
    id: "v_" + id,
    label,
    value: byVenue[id] || 0,
    unit: "sites",
  }));
  domains.venues_by_type = {
    label: "Venue types",
    kpis: venueKpis,
  };

  const alerts = [];
  if (totalHoursPledged > 500) {
    alerts.push({
      tier: "PRIORITY",
      message: `${totalHoursPledged}h pledged across ${byPinType.pledge || 0} contributors.`,
      domain: "timebank_global",
    });
  }
  if (totalHoursExchanged > 50) {
    alerts.push({
      tier: "ROUTINE",
      message: `${totalHoursExchanged}h successfully exchanged across the network.`,
      domain: "timebank_global",
    });
  }
  if ((byPinType.guide || 0) > 3) {
    alerts.push({
      tier: "ROUTINE",
      message: `${byPinType.guide} Hitchhiker Guides active — ${totalGuideCrowdfund}h crowdfunded.`,
      domain: "timebank_global",
    });
  }

  return {
    meta: {
      sweepId: randomUUID(),
      generatedAt: new Date().toISOString(),
      registryPath: sourcePath,
      planningRoot: planning.rootPath,
      planningScanned: planning.scanned,
      sourcesOk: 3,
      sourcesFailed: 0,
    },
    sites,
    domains,
    alerts,
    venueLegend: VENUE_LABEL,
    pinTypeLegend: PIN_TYPE_LABEL,
    tally: {
      totalHoursPledged,
      totalHoursExchanged,
      totalGuideCrowdfund,
      totalGuideSpent,
      totalEquityHours,
      totalBuilders,
      pledgeCount:           byPinType.pledge     || 0,
      exchangeCount:         byPinType.exchange   || 0,
      guideCount:            byPinType.guide      || 0,
      equityCount:           byPinType.equity     || 0,
      hackathonCount:        byPinType.hackathon  || 0,
      builderCount:          byPinType.builder    || 0,
      totalHackathonBuilders,
      totalHackathonGuidesLogged,
      totalHackathonFund,
      totalBuilderCommits,
      totalBuilderHours,
      liveTokens,
    },
    planning: {
      rootPath: planning.rootPath,
      scanned: planning.scanned,
      count7d: planning.count7d,
      samplePaths: (planning.files || []).slice(0, 8).map((f) => f.path),
    },
    activity: {
      feedStream: activity.feedStream,
      togetherMoments: activity.togetherMoments,
      microMetrics: activity.microMetrics,
    },
    popupBySite: activity.popupBySite,
  };
}
