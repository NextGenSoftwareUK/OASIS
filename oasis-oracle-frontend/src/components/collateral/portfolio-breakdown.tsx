"use client";

import { Card } from "@/components/ui/card";
import { Table } from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { formatCurrency } from "@/lib/utils";

type Asset = {
  name: string;
  type: string;
  value: number;
  chain: string;
  isEncumbered: boolean;
  counterparty?: string;
  maturityTime?: string;
};

export function PortfolioBreakdown() {
  // Mock data - will be replaced with API call to OwnershipOracle.GetPortfolioAsync()
  const assets: Asset[] = [
    {
      name: "10-Year US Treasury",
      type: "US Treasuries",
      value: 5_000_000_000,
      chain: "Ethereum",
      isEncumbered: false,
    },
    {
      name: "Apple Inc. Corporate Bond",
      type: "Corporate Bonds",
      value: 3_000_000_000,
      chain: "Polygon",
      isEncumbered: true,
      counterparty: "JP Morgan",
      maturityTime: "2:00 PM",
    },
    {
      name: "MBS Portfolio #3",
      type: "MBS",
      value: 1_200_000_000,
      chain: "Solana",
      isEncumbered: true,
      counterparty: "Goldman",
      maturityTime: "5:00 PM",
    },
    {
      name: "Corporate Bonds Portfolio",
      type: "Corporate Bonds",
      value: 900_000_000,
      chain: "Arbitrum",
      isEncumbered: false,
    },
  ];

  return (
    <Card
      title="Asset Portfolio"
      description="Complete breakdown of all owned assets"
      variant="glass"
    >
      <Table
        columns={[
          {
            key: "asset",
            header: "Asset",
            render: (asset) => (
              <div>
                <p className="font-medium text-[var(--color-foreground)]">
                  {asset.name}
                </p>
                <p className="text-xs text-[var(--muted)]">{asset.type}</p>
              </div>
            ),
          },
          {
            key: "value",
            header: "Value",
            render: (asset) => (
              <span className="font-mono font-semibold">
                {formatCurrency(asset.value)}
              </span>
            ),
            align: "right",
          },
          {
            key: "chain",
            header: "Chain",
            render: (asset) => (
              <Badge variant="default" size="sm">
                {asset.chain}
              </Badge>
            ),
            align: "center",
          },
          {
            key: "status",
            header: "Status",
            render: (asset) => (
              <div className="space-y-1">
                <Badge 
                  variant={asset.isEncumbered ? "warning" : "success"}
                  size="sm"
                  dot
                >
                  {asset.isEncumbered ? "Pledged" : "Available"}
                </Badge>
                {asset.isEncumbered && asset.counterparty && (
                  <p className="text-xs text-[var(--muted)]">
                    to {asset.counterparty}
                  </p>
                )}
              </div>
            ),
            align: "center",
          },
          {
            key: "maturity",
            header: "Maturity",
            render: (asset) => (
              <span className="text-sm text-[var(--muted)]">
                {asset.maturityTime || "—"}
              </span>
            ),
            align: "right",
          },
        ]}
        data={assets}
        keyExtractor={(asset, index) => asset.name + index}
      />

      {/* Summary */}
      <div className="mt-6 grid grid-cols-3 gap-4">
        <SummaryBox
          label="Total Assets"
          value={formatCurrency(assets.reduce((sum, a) => sum + a.value, 0))}
        />
        <SummaryBox
          label="Available"
          value={formatCurrency(assets.filter(a => !a.isEncumbered).reduce((sum, a) => sum + a.value, 0))}
          status="success"
        />
        <SummaryBox
          label="Pledged"
          value={formatCurrency(assets.filter(a => a.isEncumbered).reduce((sum, a) => sum + a.value, 0))}
          status="warning"
        />
      </div>
    </Card>
  );
}

function SummaryBox({
  label,
  value,
  status = "default",
}: {
  label: string;
  value: string;
  status?: "default" | "success" | "warning";
}) {
  const colors = {
    default: "text-[var(--color-foreground)]",
    success: "text-[var(--positive)]",
    warning: "text-[var(--warning)]",
  };

  return (
    <div className="p-4 rounded-lg bg-[rgba(5,5,16,0.5)] border border-[var(--color-card-border)]/30">
      <p className="text-xs uppercase tracking-wide text-[var(--muted)] mb-2">
        {label}
      </p>
      <p className={`text-xl font-semibold ${colors[status]}`}>
        {value}
      </p>
    </div>
  );
}






