"use client";

import { Table } from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { formatCurrency } from "@/lib/utils";
import { Star } from "lucide-react";
import type { PriceSourceData, PriceSource } from "@/types/price-feed";

type SourceBreakdownTableProps = {
  sources: PriceSourceData[];
  currentPrice: number;
};

export function SourceBreakdownTable({ sources, currentPrice }: SourceBreakdownTableProps) {
  return (
    <Card
      title="Price Source Breakdown"
      description="Real-time prices from multiple data providers"
      variant="glass"
    >
      <Table
        columns={[
          {
            key: "source",
            header: "Source",
            render: (source) => (
              <div className="flex items-center gap-2">
                <div className="h-8 w-8 rounded-lg bg-[var(--accent-soft)] flex items-center justify-center">
                  <span className="text-xs font-bold text-[var(--accent)]">
                    {source.source.slice(0, 2).toUpperCase()}
                  </span>
                </div>
                <span className="font-medium">{source.source}</span>
              </div>
            ),
          },
          {
            key: "price",
            header: "Price",
            render: (source) => (
              <span className="font-mono font-semibold">
                {formatCurrency(source.price)}
              </span>
            ),
            align: "right",
          },
          {
            key: "weight",
            header: "Weight",
            render: (source) => (
              <div className="flex items-center gap-1">
                <span>{source.weight.toFixed(1)}x</span>
                {source.weight >= 1.5 && (
                  <Star className="h-3 w-3 text-[var(--accent)] fill-[var(--accent)]" />
                )}
              </div>
            ),
            align: "center",
          },
          {
            key: "status",
            header: "Status",
            render: (source) => (
              <Badge 
                variant={
                  source.status === "active" ? "success" :
                  source.status === "slow" ? "warning" :
                  "danger"
                }
                size="sm"
                dot
              >
                {source.status}
              </Badge>
            ),
            align: "center",
          },
          {
            key: "latency",
            header: "Latency",
            render: (source) => (
              <span className={`text-sm ${
                source.latency < 100 ? "text-[var(--positive)]" :
                source.latency < 300 ? "text-[var(--muted)]" :
                "text-[var(--warning)]"
              }`}>
                {source.latency}ms
              </span>
            ),
            align: "right",
          },
          {
            key: "deviation",
            header: "vs Aggregate",
            render: (source) => {
              const deviation = ((source.price - currentPrice) / currentPrice) * 100;
              return (
                <span className={`text-sm ${
                  Math.abs(deviation) < 0.5 ? "text-[var(--positive)]" :
                  Math.abs(deviation) < 2 ? "text-[var(--muted)]" :
                  "text-[var(--warning)]"
                }`}>
                  {deviation >= 0 ? "+" : ""}{deviation.toFixed(2)}%
                </span>
              );
            },
            align: "right",
          },
        ]}
        data={sources}
        keyExtractor={(source, index) => source.source + index}
      />
    </Card>
  );
}






