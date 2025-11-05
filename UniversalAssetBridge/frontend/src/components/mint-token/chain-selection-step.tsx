"use client";

import { TokenConfig } from "@/app/mint-token/page-content";
import Image from "next/image";
import { Button } from "@/components/ui/button";
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
    <div className="space-y-6">
      <div className="flex flex-wrap gap-2">
        <Button onClick={selectAll} variant="outline" size="sm">
          Select All (10)
        </Button>
        <Button onClick={clearAll} variant="outline" size="sm">
          Clear All
        </Button>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-5">
        {chains.map((chain) => {
          const isSelected = config.selectedChains.includes(chain.name);
          return (
            <button
              key={chain.name}
              type="button"
              onClick={() => toggleChain(chain.name)}
              className={cn(
                "glass-card relative overflow-hidden rounded-2xl border p-5 text-left transition",
                isSelected
                  ? "border-[var(--oasis-accent)]/80 shadow-[0_25px_60px_rgba(34,211,238,0.35)] ring-2 ring-[var(--oasis-accent)]/50"
                  : "hover:border-[var(--oasis-accent)]/50"
              )}
              style={{borderColor: isSelected ? 'var(--oasis-accent)' : 'var(--oasis-card-border)'}}
            >
              <div className="absolute inset-0 bg-[radial-gradient(circle_at_top,rgba(34,211,238,0.18),transparent_60%)]" />
              <div className="relative flex flex-col items-center gap-3">
                <Image 
                  src={chain.icon} 
                  alt={chain.name}
                  width={32}
                  height={32}
                />
                <h3 className="text-sm font-semibold" style={{color: 'var(--oasis-foreground)'}}>{chain.name}</h3>
                <p className="text-xs" style={{color: 'var(--oasis-muted)'}}>
                  ${chain.gasCost} gas fee
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
  );
}

