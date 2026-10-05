"use client";

import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { AlertTriangle, TrendingDown, Shield, Zap, Clock } from "lucide-react";

export function MarginCallAlert() {
  // Mock data - will be replaced with API call to MarginCallOracle
  const alert = {
    hasRisk: true,
    riskLevel: "warning" as const,
    marketCondition: "Volatility increasing (VIX +15%)",
    recommendation: "Keep $300M buffer available",
    upcomingMaturities: "$1.2B freeing up at 2 PM",
  };

  if (!alert.hasRisk) {
    return (
      <Card variant="glass" className="border-[var(--positive)]/30">
        <div className="flex items-center gap-4 p-4">
          <Shield className="h-12 w-12 text-[var(--positive)]" />
          <div>
            <h3 className="text-lg font-semibold text-[var(--color-foreground)]">
              All Clear
            </h3>
            <p className="text-sm text-[var(--muted)]">
              No margin call risks detected. Collateral position is healthy.
            </p>
          </div>
        </div>
      </Card>
    );
  }

  return (
    <Card
      variant="gradient"
      className="border-[var(--warning)]/50 border-2"
    >
      <div className="space-y-4">
        {/* Alert Header */}
        <div className="flex items-start justify-between">
          <div className="flex items-center gap-3">
            <div className="h-12 w-12 rounded-xl bg-[rgba(250,204,21,0.2)] flex items-center justify-center">
              <AlertTriangle className="h-6 w-6 text-[var(--warning)] animate-pulse" />
            </div>
            <div>
              <h3 className="text-xl font-bold text-[var(--color-foreground)]">
                Risk Alert
              </h3>
              <Badge variant="warning" size="sm" dot>
                Monitor Closely
              </Badge>
            </div>
          </div>
        </div>

        {/* Alert Details */}
        <div className="space-y-3 p-4 rounded-lg bg-[rgba(5,5,16,0.5)]">
          <AlertItem
            icon={<TrendingDown className="h-4 w-4" />}
            label="Market Condition"
            value={alert.marketCondition}
          />
          <AlertItem
            icon={<Shield className="h-4 w-4" />}
            label="Recommendation"
            value={alert.recommendation}
          />
          <AlertItem
            icon={<Clock className="h-4 w-4" />}
            label="Upcoming"
            value={alert.upcomingMaturities}
          />
        </div>

        {/* Action Buttons */}
        <div className="flex gap-3">
          <Button variant="primary" className="flex-1 flex items-center justify-center gap-2">
            <Zap className="h-4 w-4" />
            View Recommendations
          </Button>
          <Button variant="secondary" className="flex items-center gap-2">
            Dismiss
          </Button>
        </div>
      </div>
    </Card>
  );
}

function AlertItem({ 
  icon, 
  label, 
  value 
}: { 
  icon: React.ReactNode; 
  label: string; 
  value: string; 
}) {
  return (
    <div className="flex items-start gap-3">
      <div className="text-[var(--warning)] mt-0.5">{icon}</div>
      <div>
        <p className="text-xs uppercase tracking-wide text-[var(--muted)]">{label}</p>
        <p className="text-sm font-medium text-[var(--color-foreground)] mt-1">{value}</p>
      </div>
    </div>
  );
}

