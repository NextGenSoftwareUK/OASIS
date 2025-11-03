"use client";

import { Badge } from "@/components/ui/badge";
import { formatCurrency } from "@/lib/utils";
import { Activity, Zap, CheckCircle } from "lucide-react";

type StatsOverlayProps = {
  totalTVL: number;
  activeChains: number;
  totalChains: number;
  activeFlows: number;
  consensus: number;
};

export function StatsOverlay({ 
  totalTVL, 
  activeChains, 
  totalChains, 
  activeFlows, 
  consensus 
}: StatsOverlayProps) {
  return (
    <div className="absolute bottom-4 left-4 flex gap-3 z-10">
      <StatBadge
        icon={<Activity className="h-4 w-4" />}
        label="Total TVL"
        value={formatCurrency(totalTVL)}
      />
      <StatBadge
        icon={<CheckCircle className="h-4 w-4" />}
        label="Active Chains"
        value={`${activeChains}/${totalChains}`}
        variant="success"
      />
      <StatBadge
        icon={<Zap className="h-4 w-4" />}
        label="Capital Flows"
        value={`${activeFlows} Active`}
      />
      <StatBadge
        icon={<Activity className="h-4 w-4" />}
        label="Consensus"
        value={`${consensus}%`}
        variant="success"
      />
    </div>
  );
}

function StatBadge({
  icon,
  label,
  value,
  variant = "default",
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
  variant?: "default" | "success";
}) {
  return (
    <div className="flex items-center gap-2 px-4 py-2 rounded-xl border border-[var(--color-card-border)]/50 bg-[rgba(5,5,16,0.95)] backdrop-blur-xl">
      <div className={variant === "success" ? "text-[var(--positive)]" : "text-[var(--accent)]"}>
        {icon}
      </div>
      <div>
        <p className="text-xs text-[var(--muted)]">{label}</p>
        <p className="text-sm font-semibold text-[var(--color-foreground)]">{value}</p>
      </div>
    </div>
  );
}





