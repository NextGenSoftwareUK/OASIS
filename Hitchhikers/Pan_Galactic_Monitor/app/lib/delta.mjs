export function computeDelta(prev, curr) {
  if (!prev?.domains || !curr?.domains) {
    return { changes: [], summary: "First sweep — no prior comparison." };
  }
  const changes = [];
  for (const domain of Object.keys(curr.domains)) {
    const prevKpis = prev.domains[domain]?.kpis ?? [];
    const currKpis = curr.domains[domain]?.kpis ?? [];
    const map = Object.fromEntries(prevKpis.map((k) => [k.id, k]));
    for (const k of currKpis) {
      const p = map[k.id];
      if (!p || p.value === k.value) continue;
      if (typeof k.value === "number" && typeof p.value === "number") {
        changes.push({
          domain,
          kpiId: k.id,
          label: k.label,
          from: p.value,
          to: k.value,
          unit: k.unit ?? "",
        });
      }
    }
  }
  const summary =
    changes.length === 0
      ? "Galaxy quiet — no aggregate movement."
      : `${changes.length} signal(s) shifted since last sweep.`;
  return { changes, summary, prevSweepAt: prev.meta?.generatedAt };
}
