"use client";

import { useState } from "react";
import UnifiedBalanceDisplay from "@/components/token-portal/UnifiedBalanceDisplay";
import MultiChainGrid from "@/components/token-portal/MultiChainGrid";
import TokenActions from "@/components/token-portal/TokenActions";
import HyperDriveStatus from "@/components/token-portal/HyperDriveStatus";

export default function TokenPortalClient() {
  const [selectedChain, setSelectedChain] = useState("Solana");
  
  // Mock data - in production this would come from OASIS API
  const totalBalance = 1420;
  const usdValue = 2130.00;

  return (
    <main className="flex w-full flex-col gap-6 px-4 py-10 lg:px-10 xl:px-20">
      <section className="space-y-8">
        <div>
          <p className="text-sm uppercase tracking-[0.4em] text-[var(--oasis-muted)]">Web4 Token System</p>
          <div className="flex flex-col gap-4">
            <div className="flex flex-wrap items-center gap-4">
              <h2 className="mt-2 text-3xl font-semibold" style={{color: 'var(--oasis-foreground)'}}>
                Universal Token Portal
              </h2>
              <span className="mt-2 h-fit rounded-full border border-[var(--oasis-accent)]/60 bg-[rgba(20,118,96,0.25)] px-3 py-1 text-xs uppercase tracking-[0.4em]" style={{color: 'var(--oasis-accent)'}}>
                HyperDrive Active
              </span>
            </div>
            
            <HyperDriveStatus />
          </div>
          
          <p className="mt-3 max-w-3xl text-sm leading-relaxed" style={{color: 'var(--oasis-muted)'}}>
            Your tokens exist natively on all chains simultaneously. No bridges, no wrapping, no risk. Powered by OASIS HyperDrive technology.
          </p>
        </div>

        {/* Unified Balance */}
        <div className="rounded-2xl border p-6 shadow-[0_15px_30px_rgba(15,118,110,0.18)] backdrop-blur-xl" style={{
          borderColor: 'var(--oasis-card-border)',
          background: 'rgba(8,10,25,0.85)'
        }}>
          <UnifiedBalanceDisplay balance={totalBalance} usdValue={usdValue} />
        </div>

        {/* Multi-Chain Grid */}
        <div className="rounded-2xl border p-6 shadow-[0_15px_30px_rgba(15,118,110,0.18)] backdrop-blur-xl" style={{
          borderColor: 'var(--oasis-card-border)',
          background: 'rgba(8,10,25,0.85)'
        }}>
          <MultiChainGrid balance={totalBalance} />
        </div>

        {/* Token Actions */}
        <div className="rounded-2xl border p-6 shadow-[0_15px_30px_rgba(15,118,110,0.18)] backdrop-blur-xl" style={{
          borderColor: 'var(--oasis-card-border)',
          background: 'rgba(8,10,25,0.85)'
        }}>
          <TokenActions 
            selectedChain={selectedChain}
            setSelectedChain={setSelectedChain}
            balance={totalBalance}
          />
        </div>
      </section>
    </main>
  );
}

