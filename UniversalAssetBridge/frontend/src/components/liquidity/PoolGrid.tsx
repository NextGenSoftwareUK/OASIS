"use client";

import Image from "next/image";
import { Pool } from "@/app/liquidity/liquidity-content";
import { cn } from "@/lib/utils";

interface PoolGridProps {
  pools: Pool[];
  onSelectPool: (pool: Pool) => void;
}

export default function PoolGrid({ pools, onSelectPool }: PoolGridProps) {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h3 className="text-2xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
          Available Liquidity Pools
        </h3>
        <span className="text-sm px-4 py-2 rounded-full" style={{
          background: 'var(--oasis-accent-soft)',
          color: 'var(--oasis-accent)'
        }}>
          Unified Pools • 10 Chains
        </span>
      </div>

      <div className="grid gap-5 grid-cols-3">
        {pools.map((pool) => (
          <button
            key={pool.id}
            onClick={() => onSelectPool(pool)}
            className="glass-card relative overflow-hidden rounded-2xl border p-6 text-left transition hover:scale-105"
            style={{
              borderColor: 'var(--oasis-card-border)',
              background: 'rgba(6,11,26,0.6)'
            }}
          >
            <div className="absolute inset-0 bg-[radial-gradient(circle_at_top,rgba(34,211,238,0.18),transparent_60%)]" />
            <div className="relative space-y-4">
              {/* Token Pair Icons */}
              <div className="flex items-center justify-center gap-2">
                <Image src={pool.token0Icon} alt={pool.token0} width={40} height={40} className="w-10 h-10" />
                <span className="text-2xl font-bold" style={{color: 'var(--oasis-muted)'}}>/</span>
                <Image src={pool.token1Icon} alt={pool.token1} width={40} height={40} className="w-10 h-10" />
              </div>

              {/* Pool Name */}
              <h4 className="text-xl font-bold text-center" style={{color: 'var(--oasis-foreground)'}}>
                {pool.token0} / {pool.token1}
              </h4>

              {/* Stats */}
              <div className="space-y-2 pt-3 border-t" style={{borderColor: 'var(--oasis-card-border)'}}>
                <div className="flex justify-between text-sm">
                  <span style={{color: 'var(--oasis-muted)'}}>TVL:</span>
                  <span className="font-bold" style={{color: 'var(--oasis-foreground)'}}>
                    ${(pool.tvl / 1000000).toFixed(2)}M
                  </span>
                </div>
                <div className="flex justify-between text-sm">
                  <span style={{color: 'var(--oasis-muted)'}}>24h Vol:</span>
                  <span className="font-bold" style={{color: 'var(--oasis-foreground)'}}>
                    ${(pool.volume24h / 1000000).toFixed(2)}M
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm" style={{color: 'var(--oasis-muted)'}}>APY:</span>
                  <span className="text-2xl font-bold" style={{color: 'var(--oasis-accent)'}}>
                    {pool.apy}%
                  </span>
                </div>
              </div>

              {/* Unified Pool Badge */}
              <div className="text-center pt-3">
                <span className="text-xs px-3 py-1 rounded-full" style={{
                  background: 'var(--oasis-accent-soft)',
                  color: 'var(--oasis-accent)'
                }}>
                  Unified Pool • 10 Chains
                </span>
              </div>
            </div>
          </button>
        ))}
      </div>
    </div>
  );
}

