"use client";

import { Table } from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { formatCurrency, formatNumber } from "@/lib/utils";
import type { ChainType } from "@/types/chains";

type ChainPrice = {
  chain: ChainType;
  exchange: string;
  price: number;
  liquidity: number;
};

type PriceComparisonTableProps = {
  token: string;
  prices: ChainPrice[];
};

export function PriceComparisonTable({ token, prices }: PriceComparisonTableProps) {
  const sortedPrices = [...prices].sort((a, b) => a.price - b.price);
  const lowestPrice = sortedPrices[0]?.price || 0;
  const highestPrice = sortedPrices[sortedPrices.length - 1]?.price || 0;

  const getLiquidityBadge = (liquidity: number) => {
    if (liquidity >= 5000000) return { variant: "success" as const, label: "High" };
    if (liquidity >= 1000000) return { variant: "warning" as const, label: "Med" };
    return { variant: "danger" as const, label: "Low" };
  };

  return (
    <Card
      title={`${token} Price Comparison Across Chains`}
      description={`Lowest: ${formatCurrency(lowestPrice)} · Highest: ${formatCurrency(highestPrice)}`}
      variant="glass"
    >
      <Table
        columns={[
          {
            key: "chain",
            header: "Chain",
            render: (item) => (
              <div className="flex items-center gap-2">
                <div className="h-8 w-8 rounded-lg bg-[var(--accent-soft)] flex items-center justify-center">
                  <span className="text-xs font-bold text-[var(--accent)]">
                    {item.chain.slice(0, 3).toUpperCase()}
                  </span>
                </div>
                <span className="font-medium">{item.chain}</span>
              </div>
            ),
          },
          {
            key: "exchange",
            header: "Exchange",
            render: (item) => (
              <span className="text-[var(--muted)]">{item.exchange}</span>
            ),
          },
          {
            key: "price",
            header: "Price",
            render: (item) => {
              const isLowest = item.price === lowestPrice;
              const isHighest = item.price === highestPrice;
              return (
                <div className="flex items-center gap-2">
                  <span className={`font-mono font-semibold ${
                    isLowest ? "text-[var(--positive)]" :
                    isHighest ? "text-[var(--negative)]" :
                    ""
                  }`}>
                    {formatCurrency(item.price)}
                  </span>
                  {isLowest && <Badge variant="success" size="sm">Lowest</Badge>}
                  {isHighest && <Badge variant="danger" size="sm">Highest</Badge>}
                </div>
              );
            },
            align: "right",
          },
          {
            key: "liquidity",
            header: "Liquidity",
            render: (item) => {
              const liquidityBadge = getLiquidityBadge(item.liquidity);
              return (
                <div className="flex items-center gap-2 justify-end">
                  <span className="text-sm">${formatNumber(item.liquidity, 1)}</span>
                  <Badge variant={liquidityBadge.variant} size="sm">
                    {liquidityBadge.label}
                  </Badge>
                </div>
              );
            },
            align: "right",
          },
          {
            key: "diff",
            header: "vs Lowest",
            render: (item) => {
              const diff = ((item.price - lowestPrice) / lowestPrice) * 100;
              return (
                <span className={`text-sm ${
                  diff === 0 ? "text-[var(--positive)]" :
                  diff < 2 ? "text-[var(--muted)]" :
                  "text-[var(--warning)]"
                }`}>
                  {diff === 0 ? "—" : `+${diff.toFixed(2)}%`}
                </span>
              );
            },
            align: "right",
          },
        ]}
        data={sortedPrices}
        keyExtractor={(item, index) => `${item.chain}-${item.exchange}-${index}`}
      />
    </Card>
  );
}





