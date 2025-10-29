"use client";

import { useState } from "react";
import { OracleLayout } from "@/components/layout/oracle-layout";
import { OpportunityCard } from "@/components/arbitrage/opportunity-card";
import { PriceComparisonTable } from "@/components/arbitrage/price-comparison-table";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Search, X, RefreshCw } from "lucide-react";
import { mockPriceFeeds } from "@/lib/mock-data";
import type { ArbitrageOpportunity } from "@/types/oracle";
import type { ChainType } from "@/types/chains";

export default function ArbitragePage() {
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedToken, setSelectedToken] = useState("SOL");
  const [isScanning, setIsScanning] = useState(false);

  // Generate mock arbitrage opportunities
  const mockOpportunities: ArbitrageOpportunity[] = [
    {
      token: "SOL",
      buyChain: "Solana",
      sellChain: "Ethereum",
      buyPrice: 142.31,
      sellPrice: 146.29,
      profitPercentage: 2.8,
      estimatedProfit: 39.80,
      recommendedAmount: 10,
      riskScore: "low",
      liquidityCheck: true,
      timeWindow: 45,
    },
    {
      token: "ETH",
      buyChain: "Polygon",
      sellChain: "Ethereum",
      buyPrice: 3421.00,
      sellPrice: 3462.12,
      profitPercentage: 1.2,
      estimatedProfit: 41.12,
      recommendedAmount: 1,
      riskScore: "medium",
      liquidityCheck: true,
      timeWindow: 60,
    },
    {
      token: "MATIC",
      buyChain: "Polygon",
      sellChain: "BNBChain",
      buyPrice: 0.8234,
      sellPrice: 0.8489,
      profitPercentage: 3.1,
      estimatedProfit: 25.50,
      recommendedAmount: 100,
      riskScore: "low",
      liquidityCheck: true,
      timeWindow: 30,
    },
  ];

  // Generate mock price comparison data
  type ChainPrice = {
    chain: ChainType;
    exchange: string;
    price: number;
    liquidity: number;
  };

  const mockPriceComparison: ChainPrice[] = [
    { chain: "Solana", exchange: "Orca", price: 142.31, liquidity: 2300000 },
    { chain: "Solana", exchange: "Jupiter", price: 142.35, liquidity: 8100000 },
    { chain: "Ethereum", exchange: "Uniswap V3", price: 146.29, liquidity: 45000000 },
    { chain: "Polygon", exchange: "QuickSwap", price: 142.40, liquidity: 1200000 },
    { chain: "Arbitrum", exchange: "Camelot", price: 142.38, liquidity: 5600000 },
    { chain: "Base", exchange: "Aerodrome", price: 142.33, liquidity: 3400000 },
  ];

  const handleScan = () => {
    setIsScanning(true);
    setTimeout(() => setIsScanning(false), 2000);
  };

  const filteredTokens = mockPriceFeeds.filter(feed =>
    feed.token.toLowerCase().includes(searchQuery.toLowerCase())
  );

  return (
    <OracleLayout>
      <div className="space-y-8">
        {/* Hero Section */}
        <div className="flex items-start justify-between">
          <div className="space-y-3">
            <h1 className="text-4xl font-bold tracking-tight text-[var(--color-foreground)]">
              Arbitrage Finder
            </h1>
            <p className="text-lg text-[var(--muted)] max-w-3xl">
              Discover profitable arbitrage opportunities across 20+ blockchains with real-time 
              price comparison and risk analysis.
            </p>
          </div>
          <Button 
            variant="primary" 
            onClick={handleScan}
            disabled={isScanning}
            className="flex items-center gap-2"
          >
            <RefreshCw className={`h-4 w-4 ${isScanning ? "animate-spin" : ""}`} />
            {isScanning ? "Scanning..." : "Scan Now"}
          </Button>
        </div>

        {/* Search Bar */}
        <div className="relative max-w-2xl">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-[var(--muted)]" />
          <input
            type="text"
            placeholder="Search tokens (e.g., SOL, ETH, BTC, MATIC...)"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full pl-10 pr-10 py-3 rounded-lg bg-[rgba(5,5,16,0.8)] border border-[var(--color-card-border)]/50 text-[var(--color-foreground)] placeholder:text-[var(--muted)] focus:border-[var(--accent)] focus:outline-none transition"
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

        {/* Quick Token Selection */}
        {!searchQuery && (
          <div className="flex gap-2 flex-wrap">
            {mockPriceFeeds.slice(0, 10).map((feed) => (
              <Button
                key={feed.token}
                variant={selectedToken === feed.token ? "primary" : "secondary"}
                onClick={() => setSelectedToken(feed.token)}
                className="text-sm"
              >
                {feed.token}
              </Button>
            ))}
          </div>
        )}

        {/* Opportunities Section */}
        <div className="space-y-6">
          <div className="flex items-center justify-between">
            <h2 className="text-2xl font-semibold text-[var(--color-foreground)]">
              Active Opportunities
            </h2>
            <Badge variant="info" dot>
              {mockOpportunities.length} Found
            </Badge>
          </div>

          {mockOpportunities.length > 0 ? (
            <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-6">
              {mockOpportunities.map((opp, index) => (
                <OpportunityCard key={index} opportunity={opp} />
              ))}
            </div>
          ) : (
            <div className="text-center py-12 text-[var(--muted)]">
              No arbitrage opportunities found at the moment. Try scanning again.
            </div>
          )}
        </div>

        {/* Price Comparison */}
        <PriceComparisonTable token={selectedToken} prices={mockPriceComparison} />

        {/* Info Cards */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mt-12">
          <InfoCard
            title="Real-Time Scanning"
            description="Continuously monitors prices across all chains to identify profitable arbitrage opportunities as they emerge."
          />
          <InfoCard
            title="Risk Assessment"
            description="AI-powered risk scoring considers liquidity, gas costs, time windows, and market volatility."
          />
          <InfoCard
            title="Auto-Execution"
            description="Optional automated execution of arbitrage trades with configurable parameters and safety limits."
          />
        </div>
      </div>
    </OracleLayout>
  );
}

function InfoCard({ title, description }: { title: string; description: string }) {
  return (
    <div className="rounded-2xl border border-[var(--color-card-border)]/50 bg-[rgba(8,11,26,0.85)] p-6 backdrop-blur-xl">
      <h3 className="text-lg font-semibold text-[var(--color-foreground)] mb-2">
        {title}
      </h3>
      <p className="text-sm text-[var(--muted)] leading-relaxed">
        {description}
      </p>
    </div>
  );
}


