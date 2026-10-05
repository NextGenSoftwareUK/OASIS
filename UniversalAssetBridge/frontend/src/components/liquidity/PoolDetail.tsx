"use client";

import { useState } from "react";
import { Pool } from "@/app/liquidity/liquidity-content";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import Image from "next/image";
import { ArrowLeft, TrendingUp } from "lucide-react";
import { cn } from "@/lib/utils";

interface PoolDetailProps {
  pool: Pool;
  onBack: () => void;
}

const chains = [
  { name: "Solana", icon: "/SOL.svg", tvlPercent: 10 },
  { name: "Ethereum", icon: "/ETH.svg", tvlPercent: 60 },
  { name: "Polygon", icon: "/MATIC.svg", tvlPercent: 16 },
  { name: "Base", icon: "/BASE.svg", tvlPercent: 8 },
  { name: "Arbitrum", icon: "/ARB.png", tvlPercent: 4 },
  { name: "Optimism", icon: "/OP.svg", tvlPercent: 2 },
];

export default function PoolDetail({ pool, onBack }: PoolDetailProps) {
  const [activeTab, setActiveTab] = useState<"add" | "remove">("add");
  const [amount0, setAmount0] = useState("");
  const [amount1, setAmount1] = useState("");
  const [selectedChain, setSelectedChain] = useState("Ethereum");

  return (
    <main className="flex w-full flex-col gap-6 px-4 py-10 lg:px-10 xl:px-20">
      <section className="space-y-6">
        {/* Back Button */}
        <button onClick={onBack} className="flex items-center gap-2 text-sm transition" style={{color: 'var(--oasis-accent)'}}>
          <ArrowLeft size={16} />
          Back to Pools
        </button>

        {/* Pool Header */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-4">
            <div className="flex items-center">
              <Image src={pool.token0Icon} alt={pool.token0} width={48} height={48} className="w-12 h-12" />
              <Image src={pool.token1Icon} alt={pool.token1} width={48} height={48} className="w-12 h-12 -ml-3" />
            </div>
            <div>
              <h2 className="text-3xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
                {pool.token0} / {pool.token1}
              </h2>
              <p className="text-sm mt-1" style={{color: 'var(--oasis-muted)'}}>
                Unified Pool Across 10 Chains
              </p>
            </div>
          </div>
          <div className="text-right">
            <p className="text-sm" style={{color: 'var(--oasis-muted)'}}>APY</p>
            <p className="text-4xl font-bold" style={{color: 'var(--oasis-accent)'}}>
              {pool.apy}%
            </p>
          </div>
        </div>

        {/* Stats Row */}
        <div className="grid grid-cols-2 gap-6">
          <div className="rounded-2xl border p-6" style={{
            borderColor: 'var(--oasis-card-border)',
            background: 'rgba(6,11,26,0.8)'
          }}>
            <p className="text-sm mb-2" style={{color: 'var(--oasis-muted)'}}>Total Value Locked</p>
            <p className="text-3xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
              ${(pool.tvl / 1000000).toFixed(2)}M
            </p>
            <p className="text-xs mt-2" style={{color: 'var(--oasis-accent)'}}>
              Combined across all chains
            </p>
          </div>

          <div className="rounded-2xl border p-6" style={{
            borderColor: 'var(--oasis-card-border)',
            background: 'rgba(6,11,26,0.8)'
          }}>
            <p className="text-sm mb-2" style={{color: 'var(--oasis-muted)'}}>24h Volume</p>
            <p className="text-3xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
              ${(pool.volume24h / 1000000).toFixed(2)}M
            </p>
            <p className="text-xs mt-2" style={{color: 'var(--oasis-accent)'}}>
              All chains combined
            </p>
          </div>
        </div>

        {/* Chain Distribution */}
        <div className="rounded-2xl border p-6" style={{
          borderColor: 'var(--oasis-card-border)',
          background: 'rgba(6,11,26,0.8)'
        }}>
          <h3 className="text-lg font-bold mb-4" style={{color: 'var(--oasis-foreground)'}}>
            Liquidity Distribution Across Chains
          </h3>
          <div className="grid grid-cols-3 gap-4">
            {chains.map((chain) => (
              <div key={chain.name} className="flex items-center gap-3 p-3 rounded-lg" style={{
                background: 'rgba(15,118,110,0.1)'
              }}>
                <Image src={chain.icon} alt={chain.name} width={24} height={24} />
                <div className="flex-1">
                  <p className="text-sm font-semibold" style={{color: 'var(--oasis-foreground)'}}>
                    {chain.name}
                  </p>
                  <p className="text-xs" style={{color: 'var(--oasis-muted)'}}>
                    {chain.tvlPercent}% • ${((pool.tvl * chain.tvlPercent / 100) / 1000).toFixed(0)}K
                  </p>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Add/Remove Liquidity Interface */}
        <div className="rounded-2xl border p-8" style={{
          borderColor: 'var(--oasis-card-border)',
          background: 'rgba(3,7,18,0.85)'
        }}>
          <div className="flex gap-4 mb-6">
            <button
              onClick={() => setActiveTab("add")}
              className={cn(
                "flex-1 py-3 rounded-xl font-bold transition",
                activeTab === "add"
                  ? "text-[#041321]"
                  : ""
              )}
              style={{
                background: activeTab === "add" ? 'var(--oasis-accent)' : 'rgba(8,11,26,0.6)',
                color: activeTab === "add" ? '#041321' : 'var(--oasis-muted)'
              }}
            >
              Add Liquidity
            </button>
            <button
              onClick={() => setActiveTab("remove")}
              className={cn(
                "flex-1 py-3 rounded-xl font-bold transition"
              )}
              style={{
                background: activeTab === "remove" ? 'var(--oasis-accent)' : 'rgba(8,11,26,0.6)',
                color: activeTab === "remove" ? '#041321' : 'var(--oasis-muted)'
              }}
            >
              Remove Liquidity
            </button>
          </div>

          {activeTab === "add" && (
            <div className="space-y-6">
              {/* Amount Inputs */}
              <div className="space-y-4">
                <div className="space-y-2">
                  <label className="text-sm font-semibold" style={{color: 'var(--oasis-foreground)'}}>
                    {pool.token0} Amount
                  </label>
                  <Input
                    type="number"
                    placeholder="0.00"
                    value={amount0}
                    onChange={(e) => setAmount0(e.target.value)}
                    className="h-14 text-xl font-bold"
                    style={{
                      background: 'rgba(3,7,18,0.9)',
                      borderColor: 'var(--oasis-card-border)',
                      color: 'var(--oasis-foreground)'
                    }}
                  />
                </div>

                <div className="space-y-2">
                  <label className="text-sm font-semibold" style={{color: 'var(--oasis-foreground)'}}>
                    {pool.token1} Amount
                  </label>
                  <Input
                    type="number"
                    placeholder="0.00"
                    value={amount1}
                    onChange={(e) => setAmount1(e.target.value)}
                    className="h-14 text-xl font-bold"
                    style={{
                      background: 'rgba(3,7,18,0.9)',
                      borderColor: 'var(--oasis-card-border)',
                      color: 'var(--oasis-foreground)'
                    }}
                  />
                </div>
              </div>

              {/* Chain Selection */}
              <div className="space-y-3">
                <label className="text-sm font-semibold" style={{color: 'var(--oasis-foreground)'}}>
                  Deploy Liquidity On
                </label>
                <p className="text-xs" style={{color: 'var(--oasis-muted)'}}>
                  Choose where to custody your LP tokens. You'll earn from ALL chains regardless.
                </p>
                <div className="grid grid-cols-3 gap-3">
                  {chains.map((chain) => (
                    <button
                      key={chain.name}
                      onClick={() => setSelectedChain(chain.name)}
                      className={cn(
                        "p-3 rounded-lg border transition",
                        selectedChain === chain.name
                          ? "ring-2"
                          : ""
                      )}
                      style={{
                        borderColor: selectedChain === chain.name ? 'var(--oasis-accent)' : 'var(--oasis-card-border)',
                        background: selectedChain === chain.name ? 'rgba(15,118,110,0.2)' : 'rgba(8,11,26,0.6)',
                        ringColor: selectedChain === chain.name ? 'var(--oasis-accent)' : 'transparent'
                      }}
                    >
                      <div className="flex items-center gap-2">
                        <Image src={chain.icon} alt={chain.name} width={20} height={20} />
                        <span className="text-sm font-semibold" style={{color: 'var(--oasis-foreground)'}}>
                          {chain.name}
                        </span>
                      </div>
                    </button>
                  ))}
                </div>
              </div>

              {/* Earning Breakdown */}
              <div className="rounded-xl border p-5" style={{
                borderColor: 'var(--oasis-accent)',
                background: 'rgba(15,118,110,0.15)'
              }}>
                <div className="flex items-center gap-2 mb-3">
                  <TrendingUp size={20} color="var(--oasis-accent)" />
                  <h4 className="font-bold" style={{color: 'var(--oasis-foreground)'}}>
                    You'll Earn From ALL Chains
                  </h4>
                </div>
                <div className="space-y-1 text-sm">
                  <p style={{color: 'var(--oasis-muted)'}}>Deployed on: <strong style={{color: 'var(--oasis-foreground)'}}>{selectedChain}</strong></p>
                  <p style={{color: 'var(--oasis-muted)'}}>Earning from: <strong style={{color: 'var(--oasis-accent)'}}>10 chains simultaneously</strong></p>
                  <p className="text-xs pt-2" style={{color: 'var(--oasis-accent)'}}>
                    10x fee income vs traditional single-chain pools
                  </p>
                </div>
              </div>

              <Button
                size="lg"
                className="w-full text-lg font-bold"
                style={{
                  background: 'var(--oasis-accent)',
                  color: '#041321'
                }}
              >
                Add Liquidity
              </Button>
            </div>
          )}

          {activeTab === "remove" && (
            <div className="text-center py-12" style={{color: 'var(--oasis-muted)'}}>
              <p>Connect wallet to view and remove your liquidity positions</p>
            </div>
          )}
        </div>
      </section>
    </main>
  );
}

