"use client";

import { TokenConfig } from "@/app/mint-token/page-content";
import Image from "next/image";

type ReviewStepProps = {
  config: TokenConfig;
};

const chainIcons: Record<string, string> = {
  "Solana": "/SOL.svg", "Ethereum": "/ETH.svg", "Polygon": "/MATIC.svg", "Base": "/BASE.svg",
  "Arbitrum": "/ARB.png", "Optimism": "/OP.svg", "BNB Chain": "/BNB.svg", "Avalanche": "/AVAX.svg",
  "Fantom": "/FTM.svg", "Radix": "/XRD.svg",
};

export function ReviewStep({ config }: ReviewStepProps) {
  const gasCosts: Record<string, number> = {
    "Solana": 5, "Ethereum": 150, "Polygon": 2, "Base": 10, "Arbitrum": 8, "Optimism": 12,
    "BNB Chain": 3, "Avalanche": 7, "Fantom": 4, "Radix": 1,
  };

  const totalGas = config.selectedChains.reduce((sum, chain) => sum + (gasCosts[chain] || 0), 0);
  const grandTotal = totalGas + 100;

  return (
    <div className="space-y-6">
      <div className="rounded-lg border p-5" style={{borderColor: 'var(--oasis-card-border)', background: 'rgba(15,118,110,0.05)'}}>
        <h4 className="font-semibold mb-3" style={{color: 'var(--oasis-foreground)'}}>Token Details</h4>
        <div className="space-y-2 text-sm">
          <div className="flex justify-between">
            <span style={{color: 'var(--oasis-muted)'}}>Name:</span>
            <span style={{color: 'var(--oasis-foreground)'}} className="font-semibold">{config.name || "Not set"}</span>
          </div>
          <div className="flex justify-between">
            <span style={{color: 'var(--oasis-muted)'}}>Symbol:</span>
            <span style={{color: 'var(--oasis-accent)'}} className="font-bold">{config.symbol || "N/A"}</span>
          </div>
          <div className="flex justify-between">
            <span style={{color: 'var(--oasis-muted)'}}>Total Supply:</span>
            <span style={{color: 'var(--oasis-foreground)'}}>{parseInt(config.totalSupply || "0").toLocaleString()}</span>
          </div>
        </div>
      </div>

      <div className="rounded-lg border p-5" style={{borderColor: 'var(--oasis-card-border)', background: 'rgba(15,118,110,0.05)'}}>
        <h4 className="font-semibold mb-3" style={{color: 'var(--oasis-foreground)'}}>Deployment Chains ({config.selectedChains.length})</h4>
        <div className="flex flex-wrap gap-2">
          {config.selectedChains.map((chain) => (
            <div key={chain} className="flex items-center gap-2 px-3 py-1.5 rounded-lg" style={{
              background: 'rgba(15,118,110,0.15)',
              borderColor: 'var(--oasis-accent)'
            }}>
              <Image src={chainIcons[chain]} alt={chain} width={16} height={16} />
              <span className="text-xs" style={{color: 'var(--oasis-foreground)'}}>{chain}</span>
            </div>
          ))}
        </div>
      </div>

      <div className="rounded-lg border p-5" style={{borderColor: 'var(--oasis-accent)', background: 'rgba(15,118,110,0.15)'}}>
        <h4 className="font-semibold mb-4" style={{color: 'var(--oasis-foreground)'}}>Total Deployment Cost</h4>
        <div className="space-y-2 text-sm mb-4">
          <div className="flex justify-between">
            <span style={{color: 'var(--oasis-muted)'}}>Gas Fees ({config.selectedChains.length} chains):</span>
            <span style={{color: 'var(--oasis-foreground)'}}>${totalGas}</span>
          </div>
          <div className="flex justify-between">
            <span style={{color: 'var(--oasis-muted)'}}>HyperDrive Activation:</span>
            <span style={{color: 'var(--oasis-foreground)'}}>$100</span>
          </div>
          <div className="h-px" style={{background: 'var(--oasis-card-border)'}} />
          <div className="flex justify-between text-lg font-bold">
            <span style={{color: 'var(--oasis-foreground)'}}>Grand Total:</span>
            <span style={{color: 'var(--oasis-accent)'}}>${grandTotal}</span>
          </div>
        </div>
      </div>
    </div>
  );
}

