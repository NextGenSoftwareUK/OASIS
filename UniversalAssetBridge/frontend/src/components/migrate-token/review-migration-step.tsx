"use client";

import { MigrationConfig } from "@/app/migrate-token/page-content";
import Image from "next/image";
import { ArrowRight } from "lucide-react";

type ReviewMigrationStepProps = {
  config: MigrationConfig;
};

const chainIcons: Record<string, string> = {
  "Solana": "/SOL.svg", "Ethereum": "/ETH.svg", "Polygon": "/MATIC.svg", "Base": "/BASE.svg",
  "Arbitrum": "/ARB.png", "Optimism": "/OP.svg", "BNB Chain": "/BNB.svg", "Avalanche": "/AVAX.svg",
  "Fantom": "/FTM.svg", "Radix": "/XRD.svg",
};

export function ReviewMigrationStep({ config }: ReviewMigrationStepProps) {
  const gasCosts: Record<string, number> = {
    "Solana": 5, "Ethereum": 150, "Polygon": 2, "Base": 10, "Arbitrum": 8, "Optimism": 12,
    "BNB Chain": 3, "Avalanche": 7, "Fantom": 4, "Radix": 1,
  };

  const totalGas = config.selectedChains.reduce((sum, chain) => sum + (gasCosts[chain] || 0), 0);
  const migrationFee = 200; // Higher than mint because includes migration contract
  const grandTotal = totalGas + migrationFee;

  return (
    <div className="space-y-8">
      <div>
        <h3 className="text-2xl font-bold" style={{color: 'var(--oasis-foreground)'}}>Review & Migrate</h3>
        <p className="mt-3 text-base" style={{color: 'var(--oasis-muted)'}}>
          Review your migration before executing. This will lock your original tokens.
        </p>
      </div>

      {/* Migration Flow Visualization */}
      <div className="flex items-center gap-4">
        <div className="flex-1 rounded-2xl border p-5 text-center" style={{
          borderColor: 'var(--oasis-card-border)',
          background: 'rgba(6,11,26,0.8)'
        }}>
          <p className="text-xs uppercase tracking-wider mb-2" style={{color: 'var(--oasis-muted)'}}>Original Token</p>
          <p className="text-2xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
            {parseFloat(config.migrationAmount).toLocaleString()}
          </p>
          <p className="text-sm mt-1" style={{color: 'var(--oasis-accent)'}}>{config.existingTokenSymbol}</p>
          <p className="text-xs mt-2" style={{color: 'var(--oasis-muted)'}}>on {config.existingChain}</p>
        </div>

        <ArrowRight size={32} color="var(--oasis-accent)" />

        <div className="flex-1 rounded-2xl border p-5 text-center" style={{
          borderColor: 'var(--oasis-accent)',
          background: 'rgba(15,118,110,0.2)'
        }}>
          <p className="text-xs uppercase tracking-wider mb-2" style={{color: 'var(--oasis-muted)'}}>Web4 Token</p>
          <p className="text-2xl font-bold" style={{color: 'var(--oasis-accent)'}}>
            {parseFloat(config.migrationAmount).toLocaleString()}
          </p>
          <p className="text-sm mt-1" style={{color: 'var(--oasis-foreground)'}}>Web4-{config.existingTokenSymbol}</p>
          <p className="text-xs mt-2" style={{color: 'var(--oasis-muted)'}}>on {config.selectedChains.length} chains</p>
        </div>
      </div>

      {/* Deployment Chains */}
      <div className="rounded-2xl border p-6" style={{borderColor: 'var(--oasis-card-border)', background: 'rgba(6,11,26,0.8)'}}>
        <h4 className="text-lg font-bold mb-4" style={{color: 'var(--oasis-foreground)'}}>
          Web4 Deployment Chains ({config.selectedChains.length})
        </h4>
        <div className="flex flex-wrap gap-2">
          {config.selectedChains.map((chain) => (
            <div key={chain} className="flex items-center gap-2 px-3 py-1.5 rounded-lg" style={{
              background: 'rgba(15,118,110,0.15)',
              borderColor: 'var(--oasis-accent)'
            }}>
              <Image src={chainIcons[chain]} alt={chain} width={16} height={16} />
              <span className="text-xs font-semibold" style={{color: 'var(--oasis-foreground)'}}>{chain}</span>
            </div>
          ))}
        </div>
      </div>

      {/* Cost Breakdown */}
      <div className="rounded-2xl border p-6" style={{borderColor: 'var(--oasis-accent)', background: 'rgba(15,118,110,0.2)'}}>
        <h4 className="text-lg font-bold mb-5" style={{color: 'var(--oasis-foreground)'}}>Total Migration Cost</h4>
        <div className="space-y-4 text-base mb-4">
          <div className="flex justify-between">
            <span style={{color: 'var(--oasis-muted)'}}>Gas Fees ({config.selectedChains.length} chains):</span>
            <span style={{color: 'var(--oasis-foreground)'}} className="font-bold">${totalGas}</span>
          </div>
          <div className="flex justify-between">
            <span style={{color: 'var(--oasis-muted)'}}>Migration Contract + HyperDrive:</span>
            <span style={{color: 'var(--oasis-foreground)'}} className="font-bold">${migrationFee}</span>
          </div>
          <div className="h-px" style={{background: 'var(--oasis-card-border)'}} />
          <div className="flex justify-between text-2xl font-bold">
            <span style={{color: 'var(--oasis-foreground)'}}>Grand Total:</span>
            <span style={{color: 'var(--oasis-accent)'}}>${grandTotal}</span>
          </div>
        </div>
      </div>

      {/* Important Notice */}
      <div className="rounded-xl border p-5" style={{
        borderColor: 'var(--oasis-warning)',
        background: 'rgba(250,204,21,0.1)'
      }}>
        <p className="font-bold mb-2" style={{color: 'var(--oasis-foreground)'}}>⚠️ Important:</p>
        <ul className="space-y-1 text-sm" style={{color: 'var(--oasis-muted)'}}>
          <li>• Your original {config.existingTokenSymbol} will be locked (not burned)</li>
          <li>• You can always downgrade back to original token</li>
          <li>• 1:1 ratio is guaranteed by smart contract</li>
          <li>• Web4 version exists natively on all {config.selectedChains.length} chains</li>
        </ul>
      </div>
    </div>
  );
}

