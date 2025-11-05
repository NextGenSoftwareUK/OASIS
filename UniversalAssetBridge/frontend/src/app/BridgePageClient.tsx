"use client";

import SwapForm from "@/components/SwapForm";

export default function BridgePageClient() {
  return (
    <main className="flex w-full flex-col gap-6 px-4 py-10 lg:px-10 xl:px-20">
      <section className="space-y-8">
        <div>
          <p className="text-sm uppercase tracking-[0.4em] text-[var(--oasis-muted)]">Cross-Chain Bridge</p>
          <div className="flex flex-col gap-4">
            <div className="flex flex-wrap items-center gap-4">
              <h2 className="mt-2 text-3xl font-semibold" style={{color: 'var(--oasis-foreground)'}}>
                Universal Asset Bridge
              </h2>
              <span className="mt-2 h-fit rounded-full border border-[var(--oasis-accent)]/60 bg-[rgba(20,118,96,0.25)] px-3 py-1 text-xs uppercase tracking-[0.4em]" style={{color: 'var(--oasis-accent)'}}>
                Live on Testnet
              </span>
            </div>
            <div className="flex flex-wrap items-center gap-4 rounded-2xl border px-4 py-3 text-[11px]" style={{
              borderColor: 'var(--oasis-card-border)',
              background: 'rgba(8,12,26,0.7)',
              color: 'var(--oasis-muted)'
            }}>
              <span className="text-[9px] uppercase tracking-[0.4em]">Bridge Status</span>
              <div className="flex items-center gap-3">
                <span className="text-xs font-semibold" style={{color: 'var(--oasis-accent)'}}>10 Chains Active</span>
                <span className="text-[var(--oasis-positive)]">✓ Live</span>
              </div>
              <div className="flex items-center gap-3">
                <span className="text-xs" style={{color: 'var(--oasis-muted)'}}>SOL • ETH • MATIC • BASE • ARB • OP • BNB • AVAX • FTM • XRD</span>
              </div>
              <div className="flex items-center gap-3">
                <span className="text-xs font-semibold" style={{color: 'var(--oasis-accent)'}}>Network</span>
                <span>Testnet</span>
              </div>
            </div>
          </div>
          <p className="mt-3 max-w-3xl text-sm leading-relaxed" style={{color: 'var(--oasis-muted)'}}>
            Swap tokens seamlessly across any blockchain. Powered by atomic operations with automatic rollback for maximum security.
          </p>
        </div>
        
        <div className="rounded-2xl border p-6 shadow-[0_15px_30px_rgba(15,118,110,0.18)] backdrop-blur-xl" style={{
          borderColor: 'var(--oasis-card-border)',
          background: 'rgba(8,10,25,0.85)'
        }}>
          <SwapForm />
        </div>
      </section>
    </main>
  );
}

