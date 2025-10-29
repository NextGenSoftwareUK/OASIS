"use client";

import { Table } from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { formatCurrency, formatPercentage, timeAgo } from "@/lib/utils";
import { TrendingUp, TrendingDown } from "lucide-react";
import { Card } from "@/components/ui/card";
import type { PriceFeed } from "@/types/price-feed";

export function PriceFeedTable() {
  // Mock data - will be replaced with real API calls
  const mockPriceFeeds: PriceFeed[] = [
    {
      token: "SOL",
      price: 142.35,
      change24h: 2.3,
      deviation: 0.12,
      activeSources: 8,
      totalSources: 12,
      lastUpdate: new Date(Date.now() - 5000),
      sources: [],
      confidence: 95,
    },
    {
      token: "ETH",
      price: 3421.12,
      change24h: 1.1,
      deviation: 0.08,
      activeSources: 12,
      totalSources: 12,
      lastUpdate: new Date(Date.now() - 3000),
      sources: [],
      confidence: 99,
    },
    {
      token: "XRD",
      price: 0.0542,
      change24h: -0.5,
      deviation: 0.15,
      activeSources: 6,
      totalSources: 12,
      lastUpdate: new Date(Date.now() - 8000),
      sources: [],
      confidence: 85,
    },
    {
      token: "MATIC",
      price: 0.8234,
      change24h: 0.8,
      deviation: 0.10,
      activeSources: 10,
      totalSources: 12,
      lastUpdate: new Date(Date.now() - 4000),
      sources: [],
      confidence: 92,
    },
    {
      token: "ARB",
      price: 1.2341,
      change24h: 3.2,
      deviation: 0.18,
      activeSources: 9,
      totalSources: 12,
      lastUpdate: new Date(Date.now() - 6000),
      sources: [],
      confidence: 88,
    },
  ];

  return (
    <Card
      title="Live Price Feeds"
      description="Real-time aggregated prices from multiple sources"
      variant="glass"
    >
      <Table
        columns={[
          {
            key: "token",
            header: "Token",
            render: (feed) => (
              <div className="flex items-center gap-2">
                <div className="h-8 w-8 rounded-full bg-[var(--accent-soft)] flex items-center justify-center">
                  <span className="text-xs font-bold text-[var(--accent)]">
                    {feed.token}
                  </span>
                </div>
                <span className="font-semibold">{feed.token}</span>
              </div>
            ),
          },
          {
            key: "price",
            header: "Price",
            render: (feed) => (
              <span className="font-mono font-semibold">
                {formatCurrency(feed.price)}
              </span>
            ),
            align: "right",
          },
          {
            key: "change",
            header: "24h Change",
            render: (feed) => (
              <div className="flex items-center justify-end gap-1">
                {feed.change24h >= 0 ? (
                  <TrendingUp className="h-4 w-4 text-[var(--positive)]" />
                ) : (
                  <TrendingDown className="h-4 w-4 text-[var(--negative)]" />
                )}
                <span
                  className={
                    feed.change24h >= 0
                      ? "text-[var(--positive)]"
                      : "text-[var(--negative)]"
                  }
                >
                  {formatPercentage(feed.change24h)}
                </span>
              </div>
            ),
            align: "right",
          },
          {
            key: "deviation",
            header: "Deviation",
            render: (feed) => (
              <span className={
                feed.deviation < 0.15 
                  ? "text-[var(--positive)]" 
                  : "text-[var(--warning)]"
              }>
                {feed.deviation.toFixed(2)}%
              </span>
            ),
            align: "right",
          },
          {
            key: "sources",
            header: "Sources",
            render: (feed) => (
              <span className="text-[var(--muted)]">
                {feed.activeSources}/{feed.totalSources}
              </span>
            ),
            align: "center",
          },
          {
            key: "age",
            header: "Age",
            render: (feed) => (
              <span className="text-[var(--muted)] text-xs">
                {timeAgo(feed.lastUpdate)}
              </span>
            ),
            align: "right",
          },
          {
            key: "status",
            header: "Status",
            render: (feed) => (
              <Badge 
                variant={feed.confidence > 90 ? "success" : "warning"}
                size="sm"
                dot
              >
                {feed.confidence > 90 ? "Verified" : "Partial"}
              </Badge>
            ),
            align: "center",
          },
        ]}
        data={mockPriceFeeds}
        keyExtractor={(feed, index) => feed.token + index}
      />
    </Card>
  );
}

