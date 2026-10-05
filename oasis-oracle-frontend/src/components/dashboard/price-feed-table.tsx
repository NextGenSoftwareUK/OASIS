"use client";

import { useState, useMemo } from "react";
import { Table } from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { formatCurrency, formatPercentage, timeAgo } from "@/lib/utils";
import { TrendingUp, TrendingDown, Search, X } from "lucide-react";
import { Card } from "@/components/ui/card";
import { mockPriceFeeds } from "@/lib/mock-data";
import type { PriceFeed } from "@/types/price-feed";

export function PriceFeedTable() {
  const [searchQuery, setSearchQuery] = useState("");
  const [showOnlyVerified, setShowOnlyVerified] = useState(false);
  
  // Filter price feeds based on search and filters
  const filteredFeeds = useMemo(() => {
    return mockPriceFeeds.filter(feed => {
      const matchesSearch = feed.token.toLowerCase().includes(searchQuery.toLowerCase());
      const matchesFilter = !showOnlyVerified || feed.confidence > 90;
      return matchesSearch && matchesFilter;
    });
  }, [searchQuery, showOnlyVerified]);

  return (
    <Card
      title="Live Price Feeds"
      description="Real-time aggregated prices from multiple sources"
      variant="glass"
      headerAction={
        <div className="flex items-center gap-2">
          <Button
            variant={showOnlyVerified ? "primary" : "secondary"}
            onClick={() => setShowOnlyVerified(!showOnlyVerified)}
            className="text-xs"
          >
            {showOnlyVerified ? "All" : "Verified Only"}
          </Button>
        </div>
      }
    >
      {/* Search Bar */}
      <div className="mb-4 relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-[var(--muted)]" />
        <input
          type="text"
          placeholder="Search tokens (e.g., SOL, ETH, BTC...)"
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
      <div className="mb-4 text-sm text-[var(--muted)]">
        Showing {filteredFeeds.length} of {mockPriceFeeds.length} tokens
      </div>
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
        data={filteredFeeds}
        keyExtractor={(feed, index) => feed.token + index}
        emptyMessage={searchQuery ? `No tokens found matching "${searchQuery}"` : "No price data available"}
      />
    </Card>
  );
}

