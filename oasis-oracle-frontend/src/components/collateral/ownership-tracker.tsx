"use client";

import { Card } from "@/components/ui/card";
import { StatCard } from "@/components/ui/stat-card";
import { Badge } from "@/components/ui/badge";
import { formatCurrency, formatNumber } from "@/lib/utils";
import { Wallet, Lock, UnlockKeyhole, AlertTriangle } from "lucide-react";

type CollateralPosition = {
  totalValue: number;
  available: number;
  pledged: number;
  utilizationRate: number;
  marginBuffer: number;
};

export function OwnershipTracker() {
  // Mock data - will be replaced with API call to OwnershipOracle
  const position: CollateralPosition = {
    totalValue: 10_200_000_000, // $10.2B
    available: 6_100_000_000,   // $6.1B
    pledged: 4_100_000_000,     // $4.1B
    utilizationRate: 40,
    marginBuffer: 523_000_000,  // $523M
  };

  const marginHealthStatus = 
    position.marginBuffer > 500_000_000 ? "success" :
    position.marginBuffer > 200_000_000 ? "warning" :
    "danger";

  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-semibold text-[var(--color-foreground)]">
        Real-Time Collateral Position
      </h2>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard
          label="Total Value"
          value={formatCurrency(position.totalValue)}
          description="Across all chains and systems"
          icon={<Wallet className="h-5 w-5" />}
          variant="default"
          trend="neutral"
          trendValue="Real-time"
        />
        
        <StatCard
          label="Available"
          value={formatCurrency(position.available)}
          description={`${Math.round((position.available / position.totalValue) * 100)}% of total`}
          icon={<UnlockKeyhole className="h-5 w-5" />}
          variant="success"
          trend="up"
          trendValue="Ready to use"
        />
        
        <StatCard
          label="Pledged"
          value={formatCurrency(position.pledged)}
          description={`${position.utilizationRate}% utilization`}
          icon={<Lock className="h-5 w-5" />}
          variant="warning"
          trend="neutral"
          trendValue={`${position.utilizationRate}% used`}
        />
        
        <StatCard
          label="Margin Buffer"
          value={formatCurrency(position.marginBuffer)}
          description="Safety cushion"
          icon={<AlertTriangle className="h-5 w-5" />}
          variant={marginHealthStatus}
          trend={marginHealthStatus === "success" ? "up" : "down"}
          trendValue={marginHealthStatus === "success" ? "Healthy" : "Monitor"}
        />
      </div>

      {/* Detailed Breakdown */}
      <Card
        title="Cross-Chain Breakdown"
        description="Collateral distribution across blockchains"
        variant="glass"
      >
        <div className="space-y-3">
          <ChainRow
            chain="Ethereum"
            total={4_200_000_000}
            available={2_500_000_000}
            pledged={1_700_000_000}
            status="success"
          />
          <ChainRow
            chain="Polygon"
            total={2_800_000_000}
            available={1_900_000_000}
            pledged={900_000_000}
            status="success"
          />
          <ChainRow
            chain="Solana"
            total={1_800_000_000}
            available={1_100_000_000}
            pledged={700_000_000}
            status="success"
          />
          <ChainRow
            chain="Arbitrum"
            total={900_000_000}
            available={400_000_000}
            pledged={500_000_000}
            status="warning"
          />
          <ChainRow
            chain="Legacy Systems"
            total={500_000_000}
            available={200_000_000}
            pledged={300_000_000}
            status="warning"
          />
        </div>
      </Card>
    </div>
  );
}

function ChainRow({ 
  chain, 
  total, 
  available, 
  pledged, 
  status 
}: {
  chain: string;
  total: number;
  available: number;
  pledged: number;
  status: "success" | "warning" | "danger";
}) {
  const utilization = (pledged / total) * 100;

  return (
    <div className="flex items-center justify-between p-4 rounded-lg bg-[rgba(5,5,16,0.5)] border border-[var(--color-card-border)]/30 hover:bg-[rgba(34,211,238,0.05)] transition">
      <div className="flex items-center gap-3">
        <div className="h-10 w-10 rounded-lg bg-[var(--accent-soft)] flex items-center justify-center">
          <span className="text-xs font-bold text-[var(--accent)]">
            {chain.slice(0, 3).toUpperCase()}
          </span>
        </div>
        <div>
          <h4 className="font-semibold text-[var(--color-foreground)]">{chain}</h4>
          <p className="text-xs text-[var(--muted)]">{formatCurrency(total)}</p>
        </div>
      </div>

      <div className="flex items-center gap-6">
        <div className="text-right">
          <p className="text-sm font-semibold text-[var(--positive)]">
            {formatCurrency(available)}
          </p>
          <p className="text-xs text-[var(--muted)]">Available</p>
        </div>

        <div className="text-right">
          <p className="text-sm font-semibold text-[var(--warning)]">
            {formatCurrency(pledged)}
          </p>
          <p className="text-xs text-[var(--muted)]">Pledged</p>
        </div>

        <div className="w-32">
          <div className="h-2 w-full rounded-full bg-[rgba(5,5,16,0.8)] overflow-hidden">
            <div 
              className={`h-full rounded-full transition-all duration-500 ${
                utilization < 50 ? "bg-[var(--positive)]" :
                utilization < 70 ? "bg-[var(--warning)]" :
                "bg-[var(--negative)]"
              }`}
              style={{ width: `${utilization}%` }}
            />
          </div>
          <p className="text-xs text-[var(--muted)] mt-1 text-center">
            {utilization.toFixed(0)}% used
          </p>
        </div>

        <Badge variant={status} size="sm" dot>
          {status === "success" ? "Healthy" : "Monitor"}
        </Badge>
      </div>
    </div>
  );
}






