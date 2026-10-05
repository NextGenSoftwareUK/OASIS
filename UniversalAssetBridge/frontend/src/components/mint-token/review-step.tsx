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
    <div className="space-y-8">
      <div>
        <h3 className="text-2xl font-bold" style={{color: 'var(--oasis-foreground)'}}>Review & Deploy</h3>
        <p className="mt-3 text-base" style={{color: 'var(--oasis-muted)'}}>
          Review your token configuration before deployment. This cannot be changed after deployment.
        </p>
      </div>

      <div className="rounded-2xl border p-6" style={{borderColor: 'var(--oasis-card-border)', background: 'rgba(6,11,26,0.8)'}}>
        <h4 className="text-lg font-bold mb-4" style={{color: 'var(--oasis-foreground)'}}>Token Details</h4>
        <div className="space-y-3 text-base">
          <div className="flex justify-between">
            <span style={{color: 'var(--oasis-muted)'}}>Name:</span>
            <span style={{color: 'var(--oasis-foreground)'}} className="font-bold">{config.name || "Not set"}</span>
          </div>
          <div className="flex justify-between">
            <span style={{color: 'var(--oasis-muted)'}}>Symbol:</span>
            <span style={{color: 'var(--oasis-accent)'}} className="font-bold text-xl">{config.symbol || "N/A"}</span>
          </div>
          <div className="flex justify-between">
            <span style={{color: 'var(--oasis-muted)'}}>Total Supply:</span>
            <span style={{color: 'var(--oasis-foreground)'}} className="font-bold">{parseInt(config.totalSupply || "0").toLocaleString()}</span>
          </div>
        </div>
      </div>

      <div className="rounded-2xl border p-6" style={{borderColor: 'var(--oasis-card-border)', background: 'rgba(6,11,26,0.8)'}}>
        <h4 className="text-lg font-bold mb-4" style={{color: 'var(--oasis-foreground)'}}>Deployment Chains ({config.selectedChains.length})</h4>
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

      <div className="rounded-2xl border p-6" style={{borderColor: 'var(--oasis-accent)', background: 'rgba(15,118,110,0.2)'}}>
        <h4 className="text-lg font-bold mb-5" style={{color: 'var(--oasis-foreground)'}}>Total Deployment Cost</h4>
        <div className="space-y-4 text-base mb-4">
          <div className="flex justify-between">
            <span style={{color: 'var(--oasis-muted)'}}>Gas Fees ({config.selectedChains.length} chains):</span>
            <span style={{color: 'var(--oasis-foreground)'}} className="font-bold">${totalGas}</span>
          </div>
          <div className="flex justify-between">
            <span style={{color: 'var(--oasis-muted)'}}>HyperDrive Activation:</span>
            <span style={{color: 'var(--oasis-foreground)'}} className="font-bold">$100</span>
          </div>
          <div className="h-px" style={{background: 'var(--oasis-card-border)'}} />
          <div className="flex justify-between text-2xl font-bold">
            <span style={{color: 'var(--oasis-foreground)'}}>Grand Total:</span>
            <span style={{color: 'var(--oasis-accent)'}}>${grandTotal}</span>
          </div>
        </div>
      </div>
    </div>
  );
}

