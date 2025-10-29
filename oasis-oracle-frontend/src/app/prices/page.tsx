"use client";

import { useState } from "react";
import { OracleLayout } from "@/components/layout/oracle-layout";
import { PriceSummaryCard } from "@/components/prices/price-summary-card";
import { SourceBreakdownTable } from "@/components/prices/source-breakdown-table";
import { PriceHistoryChart } from "@/components/prices/price-history-chart";
import { Button } from "@/components/ui/button";
import { mockPriceFeeds } from "@/lib/mock-data";
import type { PriceSourceData, PriceHistory } from "@/types/price-feed";

export default function PricesPage() {
  // Default to SOL
  const [selectedToken, setSelectedToken] = useState("SOL");
  
  const selectedFeed = mockPriceFeeds.find(f => f.token === selectedToken) || mockPriceFeeds[0];
  
  // Generate mock source data
  const mockSources: PriceSourceData[] = [
    {
      source: "CoinGecko",
      price: selectedFeed.price * (1 + (Math.random() - 0.5) * 0.01),
      weight: 1.0,
      status: "active",
      latency: 120,
      lastUpdate: new Date(),
    },
    {
      source: "Binance",
      price: selectedFeed.price * (1 + (Math.random() - 0.5) * 0.01),
      weight: 1.5,
      status: "active",
      latency: 85,
      lastUpdate: new Date(),
    },
    {
      source: "PythNetwork",
      price: selectedFeed.price * (1 + (Math.random() - 0.5) * 0.01),
      weight: 2.0,
      status: "active",
      latency: 45,
      lastUpdate: new Date(),
    },
    {
      source: "Chainlink",
      price: selectedFeed.price * (1 + (Math.random() - 0.5) * 0.01),
      weight: 1.5,
      status: "active",
      latency: 150,
      lastUpdate: new Date(),
    },
    {
      source: "KuCoin",
      price: selectedFeed.price * (1 + (Math.random() - 0.5) * 0.01),
      weight: 1.0,
      status: "active",
      latency: 200,
      lastUpdate: new Date(),
    },
    {
      source: "UniswapV3",
      price: selectedFeed.price * (1 + (Math.random() - 0.5) * 0.01),
      weight: 1.2,
      status: "active",
      latency: 95,
      lastUpdate: new Date(),
    },
    {
      source: "PancakeSwap",
      price: selectedFeed.price * (1 + (Math.random() - 0.5) * 0.01),
      weight: 1.0,
      status: "slow",
      latency: 450,
      lastUpdate: new Date(),
    },
    {
      source: "Jupiter",
      price: selectedFeed.price * (1 + (Math.random() - 0.5) * 0.01),
      weight: 1.2,
      status: "active",
      latency: 70,
      lastUpdate: new Date(),
    },
  ];

  // Generate mock price history (24 data points for 24 hours)
  const mockHistory: PriceHistory[] = Array.from({ length: 24 }, (_, i) => {
    const hoursAgo = 24 - i;
    const variance = Math.sin(i / 4) * 0.1 + (Math.random() - 0.5) * 0.05;
    return {
      timestamp: new Date(Date.now() - hoursAgo * 3600000),
      price: selectedFeed.price * (1 + variance),
    };
  });

  return (
    <OracleLayout>
      <div className="space-y-8">
        {/* Hero Section */}
        <div className="space-y-3">
          <h1 className="text-4xl font-bold tracking-tight text-[var(--color-foreground)]">
            Price Aggregation
          </h1>
          <p className="text-lg text-[var(--muted)] max-w-3xl">
            Real-time price aggregation from multiple oracle sources with confidence scoring,
            deviation analysis, and historical tracking.
          </p>
        </div>

        {/* Token Selector */}
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

        {/* Price Summary */}
        <PriceSummaryCard priceFeed={selectedFeed} />

        {/* Source Breakdown */}
        <SourceBreakdownTable sources={mockSources} currentPrice={selectedFeed.price} />

        {/* Price History Chart */}
        <PriceHistoryChart data={mockHistory} token={selectedFeed.token} />

        {/* Info Cards */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mt-12">
          <InfoCard
            title="Multi-Source Aggregation"
            description="Prices are aggregated from 8+ independent oracle sources using weighted averages to ensure accuracy."
          />
          <InfoCard
            title="Deviation Monitoring"
            description="Real-time monitoring of price deviations across sources to detect anomalies and manipulation attempts."
          />
          <InfoCard
            title="Confidence Scoring"
            description="AI-powered confidence scores based on source reliability, latency, and consensus levels."
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


