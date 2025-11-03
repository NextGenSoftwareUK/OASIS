"use client";

import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { formatCurrency } from "@/lib/utils";
import { Clock, TrendingUp, AlertCircle } from "lucide-react";

type MaturityEvent = {
  time: string;
  type: string;
  counterparty: string;
  amount: number;
  chain: string;
  isApproaching: boolean;
};

export function MaturityCalendar() {
  // Mock data - will be replaced with API call to EncumbranceTracker
  const upcomingMaturities: MaturityEvent[] = [
    {
      time: "2:00 PM",
      type: "Repo",
      counterparty: "JP Morgan",
      amount: 1_200_000_000,
      chain: "Ethereum",
      isApproaching: true,
    },
    {
      time: "5:00 PM",
      type: "Swap",
      counterparty: "Goldman Sachs",
      amount: 1_500_000_000,
      chain: "Polygon",
      isApproaching: false,
    },
    {
      time: "Tomorrow 9 AM",
      type: "Loan",
      counterparty: "Citadel",
      amount: 400_000_000,
      chain: "Arbitrum",
      isApproaching: false,
    },
  ];

  const totalFreeing = upcomingMaturities.reduce((sum, m) => sum + m.amount, 0);

  return (
    <Card
      title="Active Encumbrances"
      description="Upcoming maturities and when collateral becomes available"
      variant="glass"
      headerAction={
        <Badge variant="info" size="sm">
          {upcomingMaturities.length} Active
        </Badge>
      }
    >
      <div className="space-y-3">
        {upcomingMaturities.map((maturity, index) => (
          <div
            key={index}
            className={`flex items-center justify-between p-4 rounded-lg border transition ${
              maturity.isApproaching
                ? "bg-[rgba(250,204,21,0.1)] border-[var(--warning)]/30"
                : "bg-[rgba(5,5,16,0.5)] border-[var(--color-card-border)]/30"
            }`}
          >
            <div className="flex items-center gap-4">
              {/* Time Badge */}
              <div className="flex items-center gap-2">
                <Clock className={`h-5 w-5 ${
                  maturity.isApproaching ? "text-[var(--warning)] animate-pulse" : "text-[var(--muted)]"
                }`} />
                <div>
                  <p className="font-semibold text-[var(--color-foreground)]">
                    {maturity.time}
                  </p>
                  {maturity.isApproaching && (
                    <p className="text-xs text-[var(--warning)]">Approaching</p>
                  )}
                </div>
              </div>

              {/* Details */}
              <div className="flex items-center gap-6">
                <div>
                  <p className="text-sm font-medium text-[var(--color-foreground)]">
                    {maturity.type}
                  </p>
                  <p className="text-xs text-[var(--muted)]">
                    Type
                  </p>
                </div>

                <div>
                  <p className="text-sm font-medium text-[var(--color-foreground)]">
                    {maturity.counterparty}
                  </p>
                  <p className="text-xs text-[var(--muted)]">
                    Counterparty
                  </p>
                </div>

                <div>
                  <Badge variant="default" size="sm">
                    {maturity.chain}
                  </Badge>
                </div>
              </div>
            </div>

            {/* Amount */}
            <div className="text-right">
              <p className="text-lg font-semibold text-[var(--accent)]">
                {formatCurrency(maturity.amount)}
              </p>
              <p className="text-xs text-[var(--muted)]">
                Freeing Up
              </p>
            </div>
          </div>
        ))}
      </div>

      {/* Summary */}
      <div className="mt-6 p-4 rounded-lg bg-[rgba(34,211,238,0.1)] border border-[var(--accent)]/30">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <TrendingUp className="h-5 w-5 text-[var(--accent)]" />
            <span className="text-sm font-medium text-[var(--color-foreground)]">
              Total Freeing Up
            </span>
          </div>
          <span className="text-2xl font-bold text-[var(--accent)]">
            {formatCurrency(totalFreeing)}
          </span>
        </div>
        <p className="text-xs text-[var(--muted)] mt-2">
          Across {upcomingMaturities.length} upcoming maturities in next 24 hours
        </p>
      </div>
    </Card>
  );
}





