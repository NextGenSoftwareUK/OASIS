"use client";

import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { formatNumber } from "@/lib/utils";
import { TrendingUp, TrendingDown, Minus } from "lucide-react";
import type { ChainObserverData } from "@/types/chains";

export function ChainObserverGrid() {
  // Mock data - will be replaced with real API calls
  const mockChainData: ChainObserverData[] = [
    {
      chainName: "Solana",
      isHealthy: true,
      currentBlock: 285234123,
      gasPrice: "0.000005 SOL",
      tps: 3456,
      latency: 45,
      lastUpdate: new Date(),
    },
    {
      chainName: "Ethereum",
      isHealthy: true,
      currentBlock: 18934567,
      gasPrice: "23 Gwei",
      tps: 15,
      latency: 150,
      lastUpdate: new Date(),
    },
    {
      chainName: "Polygon",
      isHealthy: true,
      currentBlock: 52123789,
      gasPrice: "35 Gwei",
      tps: 145,
      latency: 100,
      lastUpdate: new Date(),
    },
    {
      chainName: "Arbitrum",
      isHealthy: true,
      currentBlock: 156789234,
      gasPrice: "0.1 Gwei",
      tps: 450,
      latency: 85,
      lastUpdate: new Date(),
    },
    {
      chainName: "Base",
      isHealthy: true,
      currentBlock: 89456123,
      gasPrice: "0.5 Gwei",
      tps: 234,
      latency: 120,
      lastUpdate: new Date(),
    },
    {
      chainName: "Radix",
      isHealthy: true,
      currentBlock: 12345678,
      gasPrice: "0.01 XRD",
      tps: 567,
      latency: 60,
      lastUpdate: new Date(),
    },
  ];

  const getTrendIcon = (change: number) => {
    if (change > 0) return <TrendingUp className="h-3 w-3 text-[var(--positive)]" />;
    if (change < 0) return <TrendingDown className="h-3 w-3 text-[var(--negative)]" />;
    return <Minus className="h-3 w-3 text-[var(--muted)]" />;
  };

  return (
    <Card
      title="Chain Observers"
      description="Real-time blockchain monitoring across 20+ networks"
      variant="glass"
      headerAction={
        <div className="flex items-center gap-2">
          <Badge variant="success" size="sm" dot>
            {mockChainData.filter(c => c.isHealthy).length} Online
          </Badge>
        </div>
      }
    >
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
        {mockChainData.map((chain) => (
          <div
            key={chain.chainName}
            className="group relative overflow-hidden rounded-xl border border-[var(--color-card-border)]/60 bg-[rgba(6,11,26,0.7)] p-4 transition hover:border-[var(--accent)]/70 hover:bg-[rgba(34,211,238,0.05)]"
          >
            {/* Glow effect on hover */}
            <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_left,rgba(34,211,238,0.15),transparent_70%)] opacity-0 transition group-hover:opacity-100" />
            
            <div className="relative">
              {/* Header */}
              <div className="flex items-center justify-between mb-3">
                <div className="flex items-center gap-2">
                  <div className="h-10 w-10 rounded-lg bg-[var(--accent-soft)] flex items-center justify-center">
                    <span className="text-sm font-bold text-[var(--accent)]">
                      {chain.chainName.slice(0, 2).toUpperCase()}
                    </span>
                  </div>
                  <div>
                    <h4 className="font-semibold text-[var(--color-foreground)]">
                      {chain.chainName}
                    </h4>
                    <Badge 
                      variant={chain.isHealthy ? "success" : "danger"} 
                      size="sm"
                      dot
                    >
                      {chain.isHealthy ? "Healthy" : "Offline"}
                    </Badge>
                  </div>
                </div>
              </div>

              {/* Stats */}
              <div className="space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-[var(--muted)]">Block:</span>
                  <span className="font-mono">
                    {formatNumber(chain.currentBlock, 0)}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-[var(--muted)]">Gas:</span>
                  <span className="font-mono text-[var(--accent)]">
                    {chain.gasPrice}
                  </span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-[var(--muted)]">TPS:</span>
                  <div className="flex items-center gap-1">
                    <span className="font-mono">{chain.tps}</span>
                    {getTrendIcon(Math.random() > 0.5 ? 1 : -1)}
                  </div>
                </div>
                <div className="flex justify-between">
                  <span className="text-[var(--muted)]">Latency:</span>
                  <span className="font-mono text-xs">
                    {chain.latency}ms
                  </span>
                </div>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* View All Button */}
      <div className="mt-6 text-center">
        <button className="text-sm text-[var(--accent)] hover:text-[#38e0ff] transition flex items-center justify-center gap-2 mx-auto">
          View All 20 Chains
          <TrendingUp className="h-4 w-4" />
        </button>
      </div>
    </Card>
  );
}

