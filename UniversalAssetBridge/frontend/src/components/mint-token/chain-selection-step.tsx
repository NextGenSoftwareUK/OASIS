"use client";

import { TokenConfig } from "@/app/mint-token/page-content";
import Image from "next/image";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";

type ChainSelectionStepProps = {
  config: TokenConfig;
  updateConfig: (updates: Partial<TokenConfig>) => void;
};

const chains = [
  { name: "Solana", icon: "/SOL.svg", gasCost: 5 },
  { name: "Ethereum", icon: "/ETH.svg", gasCost: 150 },
  { name: "Polygon", icon: "/MATIC.svg", gasCost: 2 },
  { name: "Base", icon: "/BASE.svg", gasCost: 10 },
  { name: "Arbitrum", icon: "/ARB.png", gasCost: 8 },
  { name: "Optimism", icon: "/OP.svg", gasCost: 12 },
  { name: "BNB Chain", icon: "/BNB.svg", gasCost: 3 },
  { name: "Avalanche", icon: "/AVAX.svg", gasCost: 7 },
  { name: "Fantom", icon: "/FTM.svg", gasCost: 4 },
  { name: "Radix", icon: "/XRD.svg", gasCost: 1 },
];

export function ChainSelectionStep({ config, updateConfig }: ChainSelectionStepProps) {
  const toggleChain = (chainName: string) => {
    const newChains = config.selectedChains.includes(chainName)
      ? config.selectedChains.filter(c => c !== chainName)
      : [...config.selectedChains, chainName];
    updateConfig({ selectedChains: newChains });
  };

  const selectAll = () => updateConfig({ selectedChains: chains.map(c => c.name) });
  const clearAll = () => updateConfig({ selectedChains: [] });

  return (
    <div className="space-y-8">
      <div>
        <h3 className="text-2xl font-bold" style={{color: 'var(--oasis-foreground)'}}>Configure Your Web4 Token</h3>
        <p className="mt-3 text-base" style={{color: 'var(--oasis-muted)'}}>
          Your token will exist natively on all selected chains simultaneously. No bridges required.
        </p>
      </div>

      {/* Token Configuration Card - On Top */}
      <div className="glass-card relative overflow-hidden rounded-2xl border p-6" style={{
        borderColor: 'var(--oasis-accent)',
        background: 'rgba(15,118,110,0.2)'
      }}>
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_top,rgba(34,211,238,0.25),transparent_60%)]" />
        <div className="relative">
          <div className="text-center pb-4 mb-4 border-b" style={{borderColor: 'var(--oasis-card-border)'}}>
            {config.imageUrl && (
              <div className="mb-3">
                <Image 
                  src={config.imageUrl} 
                  alt={config.name || 'Token'} 
                  width={64} 
                  height={64}
                  className="w-16 h-16 mx-auto rounded-full"
                />
              </div>
            )}
            <h4 className="text-xl font-bold" style={{color: 'var(--oasis-accent)'}}>
              {config.name || 'YOUR TOKEN'}
            </h4>
            <p className="text-xs mt-1" style={{color: 'var(--oasis-muted)'}}>
              {config.symbol ? `${config.symbol} • ` : ''}This configuration applies to all chains below
            </p>
          </div>
          
          <div className="grid grid-cols-4 gap-4">
            <div className="space-y-2">
              <label className="text-xs font-semibold uppercase tracking-wider" style={{color: 'var(--oasis-muted)'}}>
                Token Name *
              </label>
              <Input
                placeholder="Don't Panic Token"
                value={config.name}
                onChange={(e) => updateConfig({ name: e.target.value })}
                className="h-11 text-sm font-medium"
                style={{
                  background: 'rgba(3,7,18,0.9)',
                  borderColor: 'var(--oasis-card-border)',
                  color: 'var(--oasis-foreground)'
                }}
              />
            </div>

            <div className="space-y-2">
              <label className="text-xs font-semibold uppercase tracking-wider" style={{color: 'var(--oasis-muted)'}}>
                Symbol *
              </label>
              <Input
                placeholder="DPT"
                value={config.symbol}
                onChange={(e) => updateConfig({ symbol: e.target.value.toUpperCase() })}
                className="h-11 text-lg font-bold text-center"
                style={{
                  background: 'rgba(3,7,18,0.9)',
                  borderColor: 'var(--oasis-card-border)',
                  color: 'var(--oasis-accent)'
                }}
                maxLength={10}
              />
            </div>

            <div className="space-y-2">
              <label className="text-xs font-semibold uppercase tracking-wider" style={{color: 'var(--oasis-muted)'}}>
                Total Supply *
              </label>
              <Input
                type="number"
                placeholder="1000000000"
                value={config.totalSupply}
                onChange={(e) => updateConfig({ totalSupply: e.target.value })}
                className="h-11 text-sm font-medium"
                style={{
                  background: 'rgba(3,7,18,0.9)',
                  borderColor: 'var(--oasis-card-border)',
                  color: 'var(--oasis-foreground)'
                }}
              />
            </div>

            <div className="space-y-2">
              <label className="text-xs font-semibold uppercase tracking-wider" style={{color: 'var(--oasis-muted)'}}>
                Token Image
              </label>
              <Input
                type="text"
                placeholder="Image URL or upload"
                value={config.imageUrl}
                onChange={(e) => updateConfig({ imageUrl: e.target.value })}
                className="h-11 text-sm font-medium"
                style={{
                  background: 'rgba(3,7,18,0.9)',
                  borderColor: 'var(--oasis-card-border)',
                  color: 'var(--oasis-foreground)'
                }}
              />
            </div>
          </div>
        </div>
      </div>

      {/* Chain Selection - Below */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h4 className="text-lg font-bold" style={{color: 'var(--oasis-foreground)'}}>
            Select Deployment Chains
          </h4>
          <div className="flex gap-3">
            <Button onClick={selectAll} size="sm" className="font-semibold">
              Select All
            </Button>
            <Button onClick={clearAll} variant="outline" size="sm" className="font-semibold">
              Clear
            </Button>
          </div>
        </div>

        <div className="grid gap-5 grid-cols-3">
        {chains.map((chain) => {
          const isSelected = config.selectedChains.includes(chain.name);
          return (
            <button
              key={chain.name}
              type="button"
              onClick={() => toggleChain(chain.name)}
              className={cn(
                "glass-card relative overflow-hidden rounded-2xl border p-6 text-left transition hover:scale-105",
                isSelected
                  ? "border-[var(--oasis-accent)]/80 shadow-[0_25px_60px_rgba(34,211,238,0.35)] ring-2 ring-[var(--oasis-accent)]/50"
                  : "hover:border-[var(--oasis-accent)]/50"
              )}
              style={{
                borderColor: isSelected ? 'var(--oasis-accent)' : 'var(--oasis-card-border)',
                background: isSelected ? 'rgba(3,7,18,0.9)' : 'rgba(6,11,26,0.6)'
              }}
            >
              <div className="absolute inset-0 bg-[radial-gradient(circle_at_top,rgba(34,211,238,0.18),transparent_60%)]" />
              <div className="relative flex flex-col items-center gap-4">
                <Image 
                  src={chain.icon} 
                  alt={chain.name}
                  width={48}
                  height={48}
                  className="w-12 h-12"
                />
                <h3 className="text-base font-bold" style={{color: 'var(--oasis-foreground)'}}>{chain.name}</h3>
                <p className="text-sm font-medium" style={{color: 'var(--oasis-muted)'}}>
                  ${chain.gasCost} gas
                </p>
                {isSelected && (
                  <div className="absolute top-2 right-2 w-5 h-5 rounded-full flex items-center justify-center" style={{background: 'var(--oasis-accent)'}}>
                    <span className="text-xs" style={{color: '#041321'}}>✓</span>
                  </div>
                )}
              </div>
            </button>
          );
        })}
        </div>
      </div>
    </div>
  );
}

