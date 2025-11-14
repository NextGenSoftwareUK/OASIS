"use client";

import { useState, useMemo } from "react";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { formatNumber } from "@/lib/utils";
import { TrendingUp, TrendingDown, Minus, Search, X, ChevronDown, ChevronUp } from "lucide-react";
import { mockChainData } from "@/lib/mock-data";
import type { ChainObserverData } from "@/types/chains";

export function ChainObserverGrid() {
  const [searchQuery, setSearchQuery] = useState("");
  const [showAll, setShowAll] = useState(false);

  // Filter chains based on search
  const filteredChains = useMemo(() => {
    return mockChainData.filter(chain =>
      chain.chainName.toLowerCase().includes(searchQuery.toLowerCase())
    );
  }, [searchQuery]);

  // Display limited or all chains
  const displayedChains = showAll ? filteredChains : filteredChains.slice(0, 6);

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
            {mockChainData.filter(c => c.isHealthy).length} / {mockChainData.length} Online
          </Badge>
        </div>
      }
    >
      {/* Search Bar */}
      <div className="mb-4 relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-[var(--muted)]" />
        <input
          type="text"
          placeholder="Search chains (e.g., Solana, Ethereum, Bitcoin...)"
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          className="w-full pl-10 pr-10 py-2 rounded-lg bg-[rgba(5,5,16,0.8)] border border-[var(--color-card-border)]/50 text-[var(--color-foreground)] placeholder:text-[var(--muted)] focus:border-[var(--accent)] focus:outline-none transition"
        />
        {searchQuery && (
          <button
            onClick={() => setSearchQuery("")}
            className="absolute right-3 top-1/2 -translate-y-1/2 text-[var(--muted)] hover:text-[var(--accent)] transition"
          >
            <X className="h-4 w-4" />
          </button>
        )}
      </div>

      {/* Results Count */}
      {searchQuery && (
        <div className="mb-4 text-sm text-[var(--muted)]">
          Found {filteredChains.length} chain{filteredChains.length !== 1 ? "s" : ""}
        </div>
      )}

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
        {displayedChains.map((chain) => (
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

      {/* Show More/Less Button */}
      {!searchQuery && filteredChains.length > 6 && (
        <div className="mt-6 text-center">
          <Button
            variant="secondary"
            onClick={() => setShowAll(!showAll)}
            className="flex items-center gap-2 mx-auto"
          >
            {showAll ? (
              <>
                <ChevronUp className="h-4 w-4" />
                Show Less
              </>
            ) : (
              <>
                <ChevronDown className="h-4 w-4" />
                Show All {filteredChains.length} Chains
              </>
            )}
          </Button>
        </div>
      )}
    </Card>
  );
}

