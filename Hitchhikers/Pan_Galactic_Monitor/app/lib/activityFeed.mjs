import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));

const NAMES = [
  "Maya", "Tom", "Priya", "Sam", "Jordan", "Alex", "Rio", "Nia", "Chen", "Eli",
  "Zara", "Finn", "Aisha", "Luca", "Dev", "Sofia", "Kwame", "Yuki", "Omar", "Ines",
];
const VERBS = [
  "paired on a Guide section",
  "logged mentoring hours",
  "ran a stand-up",
  "merged planning notes",
  "hosted a vibe-code intro",
  "peer-reviewed a manifesto",
  "synced with another hub",
  "pledged facilitation time",
  "drafted Trillian-style research",
  "stress-tested with Marvin energy",
];

function ago(ms) {
  const m = Math.floor((Date.now() - ms) / 60000);
  if (m < 1) return "just now";
  if (m < 60) return `${m}m ago`;
  const h = Math.floor(m / 60);
  if (h < 24) return `${h}h ago`;
  return `${Math.floor(h / 24)}d ago`;
}

function pick(arr, seed) {
  return arr[Math.abs(seed) % arr.length];
}

export function buildActivityLayers(sites, planning) {
  const feedStream = [];
  const togetherMoments = [];
  let seed = Date.now() % 10000;

  const files = planning.files || [];
  for (let i = 0; i < Math.min(25, files.length); i++) {
    const f = files[i];
    feedStream.push({
      type: "doc",
      text: `Planning-Sprint · ${f.path.split("/").pop()} updated`,
      sub: f.path.slice(0, 56) + (f.path.length > 56 ? "…" : ""),
      time: ago(f.mtime),
      ts: f.mtime,
      siteId: null,
      tone: "doc",
    });
  }

  for (let i = 0; i < 45; i++) {
    seed += i * 17;
    const site = sites[seed % sites.length];
    const a = pick(NAMES, seed);
    const b = pick(NAMES, seed + 3);
    const v = pick(VERBS, seed + 7);
    feedStream.push({
      type: "together",
      text: `${a} & ${b} — ${v}`,
      sub: site.name,
      time: `${(seed % 47) + 2}m ago`,
      siteId: site.id,
      tone: "people",
    });
    if (i < 18) {
      togetherMoments.push({
        who: `${a} · ${b}`,
        what: v,
        where: site.city || site.name,
        siteId: site.id,
      });
    }
  }

  for (let i = 0; i < 20; i++) {
    seed += 11;
    feedStream.push({
      type: "timebank",
      text: `Pan Galactic Timebank · ${pick(NAMES, seed)} recorded ${1 + (seed % 4)}h exchange`,
      sub: pick(sites, seed).name,
      time: `${(seed % 120) + 5}m ago`,
      siteId: sites[seed % sites.length].id,
      tone: "time",
    });
  }

  feedStream.sort(() => Math.random() - 0.5);

  const microMetrics = [];
  const labels = [
    ["Guide drafts live", 8, 42],
    ["Agent touches (24h)", 20, 180],
    ["Cross-hub syncs", 3, 18],
    ["Student union sessions", 2, 12],
    ["Impact hub check-ins", 4, 22],
    ["Church hall builds", 1, 8],
    ["Mentor hours pooled", 40, 520],
    ["Docs merged (7d)", planning.count7d || 15, 200],
    ["Stand-ups completed", 12, 48],
    ["Pairs programming", 6, 28],
    ["Vogon forms filed", 0, 9],
    ["Heart-of-Gold pings", 14, 55],
    ["Burning Man prep tasks", 3, 40],
    ["OpenSERV SDK pulls", 8, 35],
    ["Holon stubs created", 2, 15],
    ["Workshop RSVPs", 24, 200],
    ["Discord threads", 30, 120],
    ["Notion pages edited", 15, 90],
    ["Figma frames", 5, 40],
    ["Git pushes", 22, 95],
    ["CI runs green", 18, 60],
    ["Loom updates", 4, 20],
    ["Calendar holds", 7, 30],
    ["Bursary apps", 1, 42],
    ["Residency enquiries", 2, 15],
    ["Anarchive writes (mock)", 1, 8],
    ["Zaphod promo clips", 2, 12],
    ["Marvin QA passes", 6, 25],
    ["Trillian research blocks", 9, 40],
    ["Arthur onboarding steps", 11, 50],
    ["Deep Thought braid tests", 1, 5],
  ];
  for (const [label, lo, hi] of labels) {
    const v = lo + Math.floor(Math.random() * (hi - lo + 1));
    microMetrics.push({
      id: `m_${microMetrics.length}`,
      label,
      value: v,
      unit: "",
      bar: Math.min(100, Math.round((v / hi) * 100)),
    });
  }

  const popupBySite = {};
  for (const site of sites) {
    const city = (site.city || "").toLowerCase();
    const nameTok = (site.name || "").toLowerCase().split(/\s+/).find((w) => w.length > 4);
    let matched = files.filter(
      (f) =>
        (city && f.path.toLowerCase().includes(city)) ||
        (nameTok && f.path.toLowerCase().includes(nameTok))
    );
    if (matched.length < 5) {
      const h = [...site.id].reduce((a, c) => a + c.charCodeAt(0), 0);
      const start = files.length ? h % Math.max(1, files.length - 14) : 0;
      matched = [...matched, ...files.slice(start, start + 14)];
    }
    matched = [...new Map(matched.map((f) => [f.path, f])).values()].slice(0, 14);

    const venueFeed = [];
    for (let j = 0; j < 10; j++) {
      const s = site.name.charCodeAt(j % site.name.length) + j;
      venueFeed.push({
        text: `${pick(NAMES, s)} ${pick(VERBS, s + 2)}`,
        time: `${(s % 55) + 1}m ago`,
      });
    }

    const people = [];
    for (let j = 0; j < 8; j++) {
      const n = pick(NAMES, seed + j + site.id.charCodeAt(0));
      people.push({
        initials: n.slice(0, 2).toUpperCase(),
        name: n,
        role: pick(["Builder", "Facilitator", "Mentor", "Student rep", "Hub host", "Time banker"], j + seed),
      });
    }

    popupBySite[site.id] = {
      venueFeed,
      matchedDocs: matched.map((f) => ({
        path: f.path,
        ago: ago(f.mtime),
      })),
      people,
      circleSnippet: site.pgtCircleIds?.length
        ? `${site.pgtCircleIds.length} PGT circle(s) linked — live ledger.`
        : "No PGT circle linked yet — showing simulated circle activity.",
    };
  }

  return { feedStream, togetherMoments, microMetrics, popupBySite };
}

export function planningRootFromEnv() {
  const env = process.env.PLANNING_SPRINT_PATH;
  if (env) return env;
  return join(__dirname, "..", "..", "..", "Planning-Sprint");
}
