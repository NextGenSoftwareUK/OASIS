"use client";

import { Card } from "@/components/ui/card";
import { formatCurrency } from "@/lib/utils";
import type { PriceHistory } from "@/types/price-feed";

type PriceHistoryChartProps = {
  data: PriceHistory[];
  token: string;
};

export function PriceHistoryChart({ data, token }: PriceHistoryChartProps) {
  if (data.length === 0) {
    return (
      <Card title="Price History (24H)" variant="glass">
        <div className="h-64 flex items-center justify-center text-[var(--muted)]">
          No historical data available
        </div>
      </Card>
    );
  }

  const prices = data.map(d => d.price);
  const minPrice = Math.min(...prices);
  const maxPrice = Math.max(...prices);
  const priceRange = maxPrice - minPrice;
  
  // Calculate SVG path
  const width = 800;
  const height = 200;
  const padding = 20;
  
  const points = data.map((point, index) => {
    const x = (index / (data.length - 1)) * (width - 2 * padding) + padding;
    const y = height - padding - ((point.price - minPrice) / priceRange) * (height - 2 * padding);
    return `${x},${y}`;
  });
  
  const pathData = `M ${points.join(' L ')}`;

  return (
    <Card 
      title={`${token} Price History (24H)`}
      description={`Min: ${formatCurrency(minPrice)} · Max: ${formatCurrency(maxPrice)}`}
      variant="glass"
    >
      <div className="relative">
        {/* Chart */}
        <svg
          viewBox={`0 0 ${width} ${height}`}
          className="w-full h-auto"
          style={{ minHeight: "200px" }}
        >
          {/* Grid lines */}
          {[0, 0.25, 0.5, 0.75, 1].map((percent) => {
            const y = height - padding - percent * (height - 2 * padding);
            return (
              <line
                key={percent}
                x1={padding}
                y1={y}
                x2={width - padding}
                y2={y}
                stroke="rgba(148, 163, 184, 0.1)"
                strokeWidth="1"
              />
            );
          })}

          {/* Area fill */}
          <defs>
            <linearGradient id="priceGradient" x1="0%" y1="0%" x2="0%" y2="100%">
              <stop offset="0%" stopColor="rgba(34, 211, 238, 0.3)" />
              <stop offset="100%" stopColor="rgba(34, 211, 238, 0.0)" />
            </linearGradient>
          </defs>
          
          <path
            d={`${pathData} L ${width - padding},${height - padding} L ${padding},${height - padding} Z`}
            fill="url(#priceGradient)"
          />

          {/* Line */}
          <path
            d={pathData}
            fill="none"
            stroke="rgb(34, 211, 238)"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
          />

          {/* Data points */}
          {data.map((point, index) => {
            const x = (index / (data.length - 1)) * (width - 2 * padding) + padding;
            const y = height - padding - ((point.price - minPrice) / priceRange) * (height - 2 * padding);
            
            return (
              <circle
                key={index}
                cx={x}
                cy={y}
                r="3"
                fill="rgb(34, 211, 238)"
                className="transition-all hover:r-5"
              />
            );
          })}
        </svg>

        {/* Time labels */}
        <div className="flex justify-between mt-4 text-xs text-[var(--muted)]">
          <span>24h ago</span>
          <span>18h ago</span>
          <span>12h ago</span>
          <span>6h ago</span>
          <span>Now</span>
        </div>
      </div>
    </Card>
  );
}

