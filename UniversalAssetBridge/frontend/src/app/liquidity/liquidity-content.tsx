"use client";

import { useState } from "react";
import PoolGrid from "@/components/liquidity/PoolGrid";
import PoolDetail from "@/components/liquidity/PoolDetail";
import YourPositions from "@/components/liquidity/YourPositions";
import HowItWorks from "@/components/shared/HowItWorks";

export interface Pool {
  id: string;
  token0: string;
  token1: string;
  token0Icon: string;
  token1Icon: string;
  tvl: number;
  volume24h: number;
  apy: number;
  yourPosition?: number;
}

const mockPools: Pool[] = [
  { id: "1", token0: "DPT", token1: "USDC", token0Icon: "/SOL.svg", token1Icon: "/ETH.svg", tvl: 2500000, volume24h: 850000, apy: 42 },
  { id: "2", token0: "DPT", token1: "ETH", token0Icon: "/SOL.svg", token1Icon: "/ETH.svg", tvl: 5200000, volume24h: 1200000, apy: 38 },
  { id: "3", token0: "SOL", token1: "USDC", token0Icon: "/SOL.svg", token1Icon: "/ETH.svg", tvl: 8100000, volume24h: 2100000, apy: 28 },
  { id: "4", token0: "MATIC", token1: "USDC", token0Icon: "/MATIC.svg", token1Icon: "/ETH.svg", tvl: 1200000, volume24h: 320000, apy: 35 },
  { id: "5", token0: "BNB", token1: "USDC", token0Icon: "/BNB.svg", token1Icon: "/ETH.svg", tvl: 3400000, volume24h: 890000, apy: 32 },
  { id: "6", token0: "AVAX", token1: "USDC", token0Icon: "/AVAX.svg", token1Icon: "/ETH.svg", tvl: 1800000, volume24h: 450000, apy: 29 },
];

export default function LiquidityContent() {
  const [selectedPool, setSelectedPool] = useState<Pool | null>(null);
  const [showPositions, setShowPositions] = useState(false);

  const totalTVL = mockPools.reduce((sum, pool) => sum + pool.tvl, 0);
  const total24hVolume = mockPools.reduce((sum, pool) => sum + pool.volume24h, 0);
  const yourTotalLP = 15420; // Mock user LP value

  if (selectedPool) {
    return <PoolDetail pool={selectedPool} onBack={() => setSelectedPool(null)} />;
  }

  if (showPositions) {
    return <YourPositions pools={mockPools} onBack={() => setShowPositions(false)} totalValue={yourTotalLP} />;
  }

  return (
    <main className="flex w-full flex-col gap-6 px-4 py-10 lg:px-10 xl:px-20">
      <section className="space-y-8">
        <div>
          <p className="text-sm uppercase tracking-[0.4em]" style={{color: 'var(--oasis-muted)'}}>HyperDrive Liquidity</p>
          <div className="flex flex-col gap-4">
            <h2 className="mt-2 text-4xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
              Unified Liquidity Pools
            </h2>
            <p className="text-base max-w-3xl" style={{color: 'var(--oasis-muted)'}}>
              Provide liquidity once, earn from all chains. Revolutionary cross-chain AMM powered by OASIS HyperDrive.
            </p>
          </div>
        </div>

        {/* Hero Stats */}
        <div className="text-center py-12 rounded-3xl border" style={{
          borderColor: 'var(--oasis-card-border)',
          background: 'rgba(3,7,18,0.85)'
        }}>
          <p className="text-sm uppercase tracking-wider mb-4" style={{color: 'var(--oasis-muted)'}}>
            Total Value Locked
          </p>
          <h1 className="text-7xl font-bold mb-2" style={{color: 'var(--oasis-accent)'}}>
            ${(totalTVL / 1000000).toFixed(1)}M
          </h1>
          <p className="text-base" style={{color: 'var(--oasis-muted)'}}>
            Across 10 chains simultaneously
          </p>
        </div>

        {/* Stat Cards */}
        <div className="grid grid-cols-3 gap-6">
          <div className="rounded-2xl border p-6 text-center" style={{
            borderColor: 'var(--oasis-card-border)',
            background: 'rgba(6,11,26,0.8)'
          }}>
            <p className="text-xs uppercase tracking-wider mb-2" style={{color: 'var(--oasis-muted)'}}>
              Total Pools
            </p>
            <p className="text-4xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
              {mockPools.length}
            </p>
            <p className="text-xs mt-2" style={{color: 'var(--oasis-accent)'}}>
              Active Web4 Pairs
            </p>
          </div>

          <div className="rounded-2xl border p-6 text-center" style={{
            borderColor: 'var(--oasis-card-border)',
            background: 'rgba(6,11,26,0.8)'
          }}>
            <p className="text-xs uppercase tracking-wider mb-2" style={{color: 'var(--oasis-muted)'}}>
              24h Volume
            </p>
            <p className="text-4xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
              ${(total24hVolume / 1000000).toFixed(1)}M
            </p>
            <p className="text-xs mt-2" style={{color: 'var(--oasis-accent)'}}>
              Combined All Chains
            </p>
          </div>

          <button
            onClick={() => setShowPositions(true)}
            className="rounded-2xl border p-6 text-center transition hover:scale-105"
            style={{
              borderColor: 'var(--oasis-accent)',
              background: 'rgba(15,118,110,0.2)'
            }}
          >
            <p className="text-xs uppercase tracking-wider mb-2" style={{color: 'var(--oasis-muted)'}}>
              Your LP Value
            </p>
            <p className="text-4xl font-bold" style={{color: 'var(--oasis-accent)'}}>
              ${yourTotalLP.toLocaleString()}
            </p>
            <p className="text-xs mt-2" style={{color: 'var(--oasis-muted)'}}>
              Click to manage →
            </p>
          </button>
        </div>

        <HowItWorks sections={[
          {
            title: "What are Unified Liquidity Pools?",
            content: (
              <div className="space-y-3">
                <p>
                  Unified liquidity pools solve the multi-chain liquidity fragmentation problem. Traditional AMMs require 
                  separate pools on each chain - if you want to provide liquidity on 10 chains, you need 10x the capital 
                  with each pool only earning fees from its own chain.
                </p>
                <p>
                  HyperDrive creates a single logical pool that exists simultaneously across all chains. Deploy $1 million 
                  liquidity once on Ethereum, and it serves trades on Solana, Polygon, Base, and all other chains. You earn 
                  fees from all 10 chains with the same capital deployment.
                </p>
              </div>
            )
          },
          {
            title: "How do you earn from all chains?",
            content: (
              <div className="space-y-3">
                <p>
                  When someone swaps tokens on any chain, they use liquidity from the unified pool. HyperDrive tracks every 
                  trade across all chains and calculates your proportional share of fees. If you own 1% of the pool, you 
                  earn 1% of fees from Ethereum trades, 1% from Solana trades, 1% from Polygon trades, and so on.
                </p>
                <p>
                  At the end of each period, HyperDrive aggregates all fees from all chains. On Solana, these are distributed 
                  directly to your wallet via x402. On other chains, the pool's exchange rate increases, automatically 
                  compounding your earnings.
                </p>
                <p>
                  This creates a 10x income multiplier: the same capital earning from 10 chains instead of one, with no 
                  additional risk or management overhead.
                </p>
              </div>
            )
          },
          {
            title: "What about impermanent loss?",
            content: (
              <div className="space-y-3">
                <p>
                  Unified pools actually reduce impermanent loss compared to fragmented pools. Because liquidity is combined 
                  from all chains, the total pool depth is much greater. Deeper pools experience less price impact per trade, 
                  which means less volatility and lower impermanent loss.
                </p>
                <p>
                  Additionally, arbitrage opportunities are minimized since prices stay synchronized across all chains through 
                  HyperDrive. When prices diverge, arbitrage bots quickly equalize them by trading on the cheaper chain and 
                  selling on the more expensive one - these arbitrage trades pay fees to you as well.
                </p>
              </div>
            )
          }
        ]} />

        {/* Pool Grid */}
        <PoolGrid pools={mockPools} onSelectPool={setSelectedPool} />
      </section>
    </main>
  );
}

