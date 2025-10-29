"use client";

import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { formatCurrency, formatPercentage } from "@/lib/utils";
import { TrendingUp, AlertTriangle, CheckCircle2, Zap } from "lucide-react";
import type { ArbitrageOpportunity } from "@/types/oracle";

type OpportunityCardProps = {
  opportunity: ArbitrageOpportunity;
};

export function OpportunityCard({ opportunity }: OpportunityCardProps) {
  const riskConfig = {
    low: {
      variant: "success" as const,
      icon: CheckCircle2,
      color: "text-[var(--positive)]",
    },
    medium: {
      variant: "warning" as const,
      icon: AlertTriangle,
      color: "text-[var(--warning)]",
    },
    high: {
      variant: "danger" as const,
      icon: AlertTriangle,
      color: "text-[var(--negative)]",
    },
  };

  const config = riskConfig[opportunity.riskScore];
  const RiskIcon = config.icon;

  return (
    <Card variant="gradient" className="relative overflow-hidden hover:scale-[1.02] transition-transform">
      {/* Glow effect */}
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_right,rgba(34,211,238,0.15),transparent_70%)]" />
      
      <div className="relative space-y-4">
        {/* Header */}
        <div className="flex items-start justify-between">
          <div>
            <h3 className="text-2xl font-bold text-[var(--color-foreground)] flex items-center gap-2">
              {opportunity.token}
              <span className="text-sm text-[var(--muted)] font-normal">
                {opportunity.buyChain} → {opportunity.sellChain}
              </span>
            </h3>
          </div>
          <div className="flex items-center gap-2">
            <Badge variant={config.variant} size="sm">
              <RiskIcon className="h-3 w-3" />
              {opportunity.riskScore.toUpperCase()} RISK
            </Badge>
          </div>
        </div>

        {/* Profit Display */}
        <div className="p-4 rounded-xl bg-[rgba(34,211,238,0.12)] border border-[var(--accent)]/30">
          <p className="text-sm text-[var(--muted)] mb-1">Estimated Profit</p>
          <div className="flex items-baseline gap-3">
            <p className="text-4xl font-bold text-[var(--accent)]">
              {formatPercentage(opportunity.profitPercentage, 1)}
            </p>
            <p className="text-xl text-[var(--color-foreground)]">
              ≈ {formatCurrency(opportunity.estimatedProfit)}
            </p>
          </div>
        </div>

        {/* Details Grid */}
        <div className="grid grid-cols-2 gap-4">
          <DetailBox label="Buy Price" value={formatCurrency(opportunity.buyPrice)} chain={opportunity.buyChain} />
          <DetailBox label="Sell Price" value={formatCurrency(opportunity.sellPrice)} chain={opportunity.sellChain} />
          <DetailBox label="Recommended" value={`${opportunity.recommendedAmount} ${opportunity.token}`} />
          <DetailBox label="Time Window" value={`~${opportunity.timeWindow}s`} />
        </div>

        {/* Liquidity Check */}
        <div className="flex items-center gap-2 p-3 rounded-lg bg-[rgba(5,5,16,0.5)] border border-[var(--color-card-border)]/30">
          {opportunity.liquidityCheck ? (
            <>
              <CheckCircle2 className="h-4 w-4 text-[var(--positive)]" />
              <span className="text-sm text-[var(--positive)]">Sufficient liquidity on both chains</span>
            </>
          ) : (
            <>
              <AlertTriangle className="h-4 w-4 text-[var(--warning)]" />
              <span className="text-sm text-[var(--warning)]">Limited liquidity - proceed with caution</span>
            </>
          )}
        </div>

        {/* Action Button */}
        <Button variant="primary" className="w-full flex items-center justify-center gap-2">
          <Zap className="h-4 w-4" />
          Execute Arbitrage
        </Button>
      </div>
    </Card>
  );
}

function DetailBox({ label, value, chain }: { label: string; value: string; chain?: string }) {
  return (
    <div>
      <p className="text-xs uppercase tracking-wide text-[var(--muted)] mb-1">
        {label}
      </p>
      <div>
        <p className="text-sm font-semibold text-[var(--color-foreground)]">
          {value}
        </p>
        {chain && (
          <p className="text-xs text-[var(--accent)] mt-0.5">on {chain}</p>
        )}
      </div>
    </div>
  );
}


