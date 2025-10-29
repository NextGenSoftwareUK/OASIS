"use client";

import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { formatCurrency, formatNumber } from "@/lib/utils";
import { X, ExternalLink, TrendingUp } from "lucide-react";
import type { ChainNode3D } from "@/lib/visualization-data";

type ChainDetailsOverlayProps = {
  chain: ChainNode3D | null;
  onClose: () => void;
};

export function ChainDetailsOverlay({ chain, onClose }: ChainDetailsOverlayProps) {
  if (!chain) return null;
  
  return (
    <div className="absolute top-4 right-4 w-96 z-10">
      <Card variant="gradient" className="border-[var(--accent)]/50">
        <div className="space-y-4">
          {/* Header */}
          <div className="flex items-start justify-between">
            <div className="flex items-center gap-3">
              <div 
                className="h-12 w-12 rounded-xl flex items-center justify-center"
                style={{ backgroundColor: `${chain.color}20` }}
              >
                <span 
                  className="text-lg font-bold" 
                  style={{ color: chain.color }}
                >
                  {chain.name.slice(0, 3).toUpperCase()}
                </span>
              </div>
              <div>
                <h3 className="text-2xl font-bold text-[var(--color-foreground)]">
                  {chain.name}
                </h3>
                <Badge variant={chain.health === "healthy" ? "success" : "danger"} size="sm" dot>
                  {chain.health}
                </Badge>
              </div>
            </div>
            <button
              onClick={onClose}
              className="text-[var(--muted)] hover:text-[var(--accent)] transition"
            >
              <X className="h-5 w-5" />
            </button>
          </div>
          
          {/* Stats */}
          <div className="grid grid-cols-2 gap-3">
            <StatBox label="Total Value Locked" value={formatCurrency(chain.tvl)} />
            <StatBox label="Transactions/Sec" value={formatNumber(chain.tps, 0)} />
            <StatBox label="Position" value={`(${chain.position[0]}, ${chain.position[1]}, ${chain.position[2]})`} />
            <StatBox label="Connections" value="8 Active" />
          </div>
          
          {/* Active Connections */}
          <div className="pt-4 border-t border-[var(--color-card-border)]/30">
            <h4 className="text-sm font-semibold text-[var(--color-foreground)] mb-3">
              Active Capital Flows
            </h4>
            <div className="space-y-2">
              <ConnectionItem to="Polygon" amount="$2.3B" direction="outgoing" />
              <ConnectionItem to="Arbitrum" amount="$800M" direction="outgoing" />
              <ConnectionItem to="Bitcoin" amount="$1.2B" direction="incoming" />
            </div>
          </div>
          
          {/* Actions */}
          <div className="flex gap-2 pt-4 border-t border-[var(--color-card-border)]/30">
            <Button variant="primary" className="flex-1 flex items-center justify-center gap-2 text-sm">
              <TrendingUp className="h-4 w-4" />
              View Details
            </Button>
            <Button variant="secondary" className="flex items-center gap-2 text-sm">
              <ExternalLink className="h-4 w-4" />
              Explorer
            </Button>
          </div>
        </div>
      </Card>
    </div>
  );
}

function StatBox({ label, value }: { label: string; value: string }) {
  return (
    <div className="p-3 rounded-lg bg-[rgba(5,5,16,0.5)] border border-[var(--color-card-border)]/30">
      <p className="text-xs uppercase tracking-wide text-[var(--muted)] mb-1">
        {label}
      </p>
      <p className="text-sm font-semibold text-[var(--color-foreground)]">
        {value}
      </p>
    </div>
  );
}

function ConnectionItem({ 
  to, 
  amount, 
  direction 
}: { 
  to: string; 
  amount: string; 
  direction: "incoming" | "outgoing";
}) {
  return (
    <div className="flex items-center justify-between p-2 rounded-lg bg-[rgba(5,5,16,0.3)]">
      <div className="flex items-center gap-2">
        <span className={`text-lg ${direction === "outgoing" ? "text-[var(--accent)]" : "text-[var(--positive)]"}`}>
          {direction === "outgoing" ? "→" : "←"}
        </span>
        <span className="text-sm font-medium text-[var(--color-foreground)]">
          {to}
        </span>
      </div>
      <span className="text-sm font-semibold text-[var(--accent)]">
        {amount}
      </span>
    </div>
  );
}

