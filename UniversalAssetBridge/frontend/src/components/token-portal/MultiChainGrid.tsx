"use client";

import Image from "next/image";

interface MultiChainGridProps {
  balance: number;
}

const chains = [
  { name: "Solana", token: "SOL", icon: "/SOL.svg" },
  { name: "Ethereum", token: "ETH", icon: "/ETH.svg" },
  { name: "Polygon", token: "MATIC", icon: "/MATIC.svg" },
  { name: "Base", token: "BASE", icon: "/BASE.svg" },
  { name: "Arbitrum", token: "ARB", icon: "/ARB.png" },
  { name: "Optimism", token: "OP", icon: "/OP.svg" },
  { name: "BNB Chain", token: "BNB", icon: "/BNB.svg" },
  { name: "Avalanche", token: "AVAX", icon: "/AVAX.svg" },
  { name: "Fantom", token: "FTM", icon: "/FTM.svg" },
  { name: "Radix", token: "XRD", icon: "/XRD.svg" },
];

export default function MultiChainGrid({ balance }: MultiChainGridProps) {
  return (
    <div className="space-y-4">
      <h3 className="text-sm uppercase tracking-[0.3em]" style={{color: 'var(--oasis-muted)'}}>
        Your Balance Across All Chains
      </h3>
      
      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-4">
        {chains.map((chain) => (
          <div 
            key={chain.token}
            className="flex flex-col items-center gap-3 rounded-xl border p-4 transition-all hover:scale-105"
            style={{
              borderColor: 'var(--oasis-card-border)',
              background: 'rgba(15,118,110,0.1)'
            }}
          >
            <Image 
              src={chain.icon} 
              alt={chain.name}
              width={32}
              height={32}
              className="h-8 w-8"
            />
            
            <div className="flex flex-col items-center gap-1">
              <span className="text-xs font-semibold" style={{color: 'var(--oasis-accent)'}}>
                {chain.name}
              </span>
              <span className="text-lg font-bold" style={{color: 'var(--oasis-foreground)'}}>
                {balance.toLocaleString('en-US')}
              </span>
              <span className="text-[10px]" style={{color: 'var(--oasis-muted)'}}>
                DPT
              </span>
            </div>

            <div className="flex items-center gap-1">
              <div className="h-1.5 w-1.5 rounded-full" style={{backgroundColor: 'var(--oasis-positive)'}} />
              <span className="text-[10px]" style={{color: 'var(--oasis-positive)'}}>
                Synced
              </span>
            </div>
          </div>
        ))}
      </div>

      <p className="text-xs text-center pt-2" style={{color: 'var(--oasis-muted)'}}>
        Same balance on all 10 chains • Synchronized in real-time • No bridges required
      </p>
    </div>
  );
}

