"use client";

import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { formatCurrency, formatPercentage, timeAgo } from "@/lib/utils";
import { TrendingUp, TrendingDown } from "lucide-react";
import type { PriceFeed } from "@/types/price-feed";

type PriceSummaryCardProps = {
  priceFeed: PriceFeed;
};

export function PriceSummaryCard({ priceFeed }: PriceSummaryCardProps) {
  const isPositiveChange = priceFeed.change24h >= 0;

  return (
    <Card variant="gradient" className="relative overflow-hidden">
      {/* Radial gradient */}
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_left,rgba(34,211,238,0.15),transparent_70%)]" />
      
      <div className="relative space-y-6">
        {/* Token Header */}
        <div className="flex items-start justify-between">
          <div>
            <div className="flex items-center gap-3">
              <div className="h-12 w-12 rounded-xl bg-[var(--accent-soft)] flex items-center justify-center">
                <span className="text-lg font-bold text-[var(--accent)]">
                  {priceFeed.token}
                </span>
              </div>
              <div>
                <h3 className="text-2xl font-bold text-[var(--color-foreground)]">
                  {priceFeed.token}
                </h3>
                <p className="text-sm text-[var(--muted)]">
                  {priceFeed.activeSources}/{priceFeed.totalSources} sources active
                </p>
              </div>
            </div>
          </div>
          <Badge 
            variant={priceFeed.confidence > 90 ? "success" : "warning"}
            dot
          >
            {priceFeed.confidence}% Confidence
          </Badge>
        </div>

        {/* Price Display */}
        <div className="space-y-2">
          <p className="text-5xl font-bold tracking-tight text-[var(--color-foreground)]">
            {formatCurrency(priceFeed.price)}
          </p>
          <div className="flex items-center gap-3">
            <div className={`flex items-center gap-1 ${isPositiveChange ? "text-[var(--positive)]" : "text-[var(--negative)]"}`}>
              {isPositiveChange ? (
                <TrendingUp className="h-5 w-5" />
              ) : (
                <TrendingDown className="h-5 w-5" />
              )}
              <span className="text-xl font-semibold">
                {formatPercentage(priceFeed.change24h)}
              </span>
            </div>
            <span className="text-sm text-[var(--muted)]">
              24h change
            </span>
          </div>
        </div>

        {/* Metrics Grid */}
        <div className="grid grid-cols-3 gap-4 pt-6 border-t border-[var(--color-card-border)]/30">
          <MetricBox
            label="Confidence"
            value={`${priceFeed.confidence}%`}
            progress={priceFeed.confidence}
          />
          <MetricBox
            label="Deviation"
            value={`${priceFeed.deviation.toFixed(2)}%`}
            status={priceFeed.deviation < 0.15 ? "success" : "warning"}
          />
          <MetricBox
            label="Last Update"
            value={timeAgo(priceFeed.lastUpdate)}
          />
        </div>
      </div>
    </Card>
  );
}

function MetricBox({ 
  label, 
  value, 
  progress, 
  status 
}: { 
  label: string; 
  value: string; 
  progress?: number;
  status?: "success" | "warning";
}) {
  return (
    <div className="space-y-2">
      <p className="text-xs uppercase tracking-wide text-[var(--muted)]">
        {label}
      </p>
      <p className={`text-lg font-semibold ${
        status === "success" ? "text-[var(--positive)]" :
        status === "warning" ? "text-[var(--warning)]" :
        "text-[var(--color-foreground)]"
      }`}>
        {value}
      </p>
      {progress !== undefined && (
        <div className="h-1 w-full rounded-full bg-[rgba(5,5,16,0.8)] overflow-hidden">
          <div 
            className="h-full bg-[var(--accent)] rounded-full transition-all duration-500"
            style={{ width: `${progress}%` }}
          />
        </div>
      )}
    </div>
  );
}

