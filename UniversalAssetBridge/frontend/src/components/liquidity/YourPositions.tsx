"use client";

import { Pool } from "@/app/liquidity/liquidity-content";
import Image from "next/image";
import { ArrowLeft } from "lucide-react";
import { Button } from "@/components/ui/button";

interface YourPositionsProps {
  pools: Pool[];
  onBack: () => void;
  totalValue: number;
}

const mockPositions = [
  { poolId: "1", value: 5420, fees24h: 8.50, feesTotal: 850, depositedOn: "Ethereum" },
  { poolId: "2", value: 10000, fees24h: 12.00, feesTotal: 1490, depositedOn: "Solana" },
];

export default function YourPositions({ pools, onBack, totalValue }: YourPositionsProps) {
  return (
    <main className="flex w-full flex-col gap-6 px-4 py-10 lg:px-10 xl:px-20">
      <section className="space-y-8">
        {/* Back Button */}
        <button onClick={onBack} className="flex items-center gap-2 text-sm transition" style={{color: 'var(--oasis-accent)'}}>
          <ArrowLeft size={16} />
          Back to Pools
        </button>

        {/* Header */}
        <div>
          <h2 className="text-4xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
            Your Liquidity Positions
          </h2>
          <p className="text-base mt-3" style={{color: 'var(--oasis-muted)'}}>
            Manage your LP positions earning from all chains simultaneously.
          </p>
        </div>

        {/* Total Stats */}
        <div className="grid grid-cols-3 gap-6">
          <div className="rounded-2xl border p-6 text-center" style={{
            borderColor: 'var(--oasis-accent)',
            background: 'rgba(15,118,110,0.2)'
          }}>
            <p className="text-sm mb-2" style={{color: 'var(--oasis-muted)'}}>Total LP Value</p>
            <p className="text-4xl font-bold" style={{color: 'var(--oasis-accent)'}}>
              ${totalValue.toLocaleString()}
            </p>
          </div>

          <div className="rounded-2xl border p-6 text-center" style={{
            borderColor: 'var(--oasis-card-border)',
            background: 'rgba(6,11,26,0.8)'
          }}>
            <p className="text-sm mb-2" style={{color: 'var(--oasis-muted)'}}>24h Fees Earned</p>
            <p className="text-4xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
              $20.50
            </p>
          </div>

          <div className="rounded-2xl border p-6 text-center" style={{
            borderColor: 'var(--oasis-card-border)',
            background: 'rgba(6,11,26,0.8)'
          }}>
            <p className="text-sm mb-2" style={{color: 'var(--oasis-muted)'}}>All-Time Fees</p>
            <p className="text-4xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
              $2,340
            </p>
          </div>
        </div>

        {/* Positions Grid */}
        <div>
          <h3 className="text-2xl font-bold mb-6" style={{color: 'var(--oasis-foreground)'}}>
            Your Positions
          </h3>
          
          <div className="grid gap-5 grid-cols-2">
            {mockPositions.map((position) => {
              const pool = pools.find(p => p.id === position.poolId);
              if (!pool) return null;

              return (
                <div
                  key={position.poolId}
                  className="glass-card relative overflow-hidden rounded-2xl border p-6"
                  style={{
                    borderColor: 'var(--oasis-card-border)',
                    background: 'rgba(6,11,26,0.6)'
                  }}
                >
                  <div className="absolute inset-0 bg-[radial-gradient(circle_at_top,rgba(34,211,238,0.18),transparent_60%)]" />
                  <div className="relative space-y-4">
                    {/* Pool Icons */}
                    <div className="flex items-center justify-center gap-2">
                      <Image src={pool.token0Icon} alt={pool.token0} width={32} height={32} />
                      <span className="text-xl font-bold" style={{color: 'var(--oasis-muted)'}}>/</span>
                      <Image src={pool.token1Icon} alt={pool.token1} width={32} height={32} />
                    </div>

                    {/* Pool Name */}
                    <h4 className="text-xl font-bold text-center" style={{color: 'var(--oasis-foreground)'}}>
                      {pool.token0} / {pool.token1}
                    </h4>

                    {/* Position Stats */}
                    <div className="space-y-3 pt-3 border-t" style={{borderColor: 'var(--oasis-card-border)'}}>
                      <div className="flex justify-between">
                        <span className="text-sm" style={{color: 'var(--oasis-muted)'}}>LP Value:</span>
                        <span className="text-lg font-bold" style={{color: 'var(--oasis-foreground)'}}>
                          ${position.value.toLocaleString()}
                        </span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-sm" style={{color: 'var(--oasis-muted)'}}>Deployed on:</span>
                        <span className="text-sm font-semibold" style={{color: 'var(--oasis-accent)'}}>
                          {position.depositedOn}
                        </span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-sm" style={{color: 'var(--oasis-muted)'}}>Earning from:</span>
                        <span className="text-sm font-semibold" style={{color: 'var(--oasis-accent)'}}>
                          10 chains
                        </span>
                      </div>
                    </div>

                    {/* Fees */}
                    <div className="rounded-lg p-3" style={{
                      background: 'rgba(15,118,110,0.15)',
                      borderColor: 'var(--oasis-accent)'
                    }}>
                      <div className="flex justify-between text-sm mb-1">
                        <span style={{color: 'var(--oasis-muted)'}}>24h Fees:</span>
                        <span className="font-bold" style={{color: 'var(--oasis-positive)'}}>
                          +${position.fees24h.toFixed(2)}
                        </span>
                      </div>
                      <div className="flex justify-between text-sm">
                        <span style={{color: 'var(--oasis-muted)'}}>All-Time:</span>
                        <span className="font-bold" style={{color: 'var(--oasis-foreground)'}}>
                          ${position.feesTotal.toLocaleString()}
                        </span>
                      </div>
                    </div>

                    {/* Actions */}
                    <div className="flex gap-2 pt-2">
                      <Button variant="outline" className="flex-1">
                        Manage
                      </Button>
                      <Button variant="secondary" className="flex-1">
                        Remove
                      </Button>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </section>
    </main>
  );
}

