"use client";

import SwapForm from "@/components/SwapForm";
import HowItWorks from "@/components/shared/HowItWorks";

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

        <HowItWorks sections={[
          {
            title: "What is the Universal Asset Bridge?",
            content: (
              <div className="space-y-3">
                <p>
                  The Universal Asset Bridge enables instant token swaps across 10 blockchains without using traditional bridges. 
                  Unlike conventional bridges that lock tokens on one chain and mint wrapped versions on another, our system uses 
                  OASIS HyperDrive to maintain synchronized state across all chains simultaneously.
                </p>
                <p>
                  When you swap SOL for ETH, the transaction executes atomically on both Solana and Ethereum with automatic 
                  rollback if either side fails. No wrapped tokens, no bridge contracts to hack, no waiting periods.
                </p>
              </div>
            )
          },
          {
            title: "Why is this safer than traditional bridges?",
            content: (
              <div className="space-y-3">
                <p>
                  Traditional bridges have lost over $2 billion to hacks because they create single points of failure: 
                  bridge contracts that hold millions in locked tokens become honeypots for attackers.
                </p>
                <p>
                  The Universal Asset Bridge eliminates this risk entirely. There are no bridge contracts holding your funds. 
                  Instead, HyperDrive uses a 50+ provider consensus system where transactions must be validated by multiple 
                  independent blockchain nodes before execution. If any provider fails or is compromised, the system automatically 
                  fails over to the next available provider.
                </p>
                <p>
                  This architecture makes bridge hacks mathematically impossible - an attacker would need to compromise 
                  26+ independent providers simultaneously.
                </p>
              </div>
            )
          },
          {
            title: "How does HyperDrive work?",
            content: (
              <div className="space-y-3">
                <p>
                  HyperDrive is OASIS's auto-failover data replication system that synchronizes state across all supported 
                  blockchains in under 2 seconds. When you initiate a swap, HyperDrive simultaneously writes the transaction 
                  to both source and destination chains.
                </p>
                <p>
                  If Ethereum goes down, HyperDrive automatically routes through Polygon. If Polygon fails, it uses Solana. 
                  The system continues through all 50+ providers until the transaction succeeds. This ensures 100% uptime - 
                  your swap will complete even if multiple chains are offline.
                </p>
                <p>
                  All providers maintain consensus on transaction state. If balances diverge due to network issues, 
                  the majority vote determines truth and conflicts are resolved automatically within 2 seconds.
                </p>
              </div>
            )
          }
        ]} />
        
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

