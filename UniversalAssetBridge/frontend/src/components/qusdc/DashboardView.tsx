"use client";

import { TrendingUp, DollarSign, Clock, Shield } from "lucide-react";
import Image from "next/image";

// Mock data
const mockData = {
  qUSDCBalance: 5000,
  sqUSDCBalance: 8500,
  exchangeRate: 1.0309,
  currentAPY: 12.5,
  yieldToday: 3.09,
  yieldWeek: 21.63,
  yieldMonth: 92.70,
  yieldAllTime: 427.50,
  totalTVL: 127500000,
  totalStaked: 89000000,
  reserveFund: 8900000,
  dailyYield: 43493,
  lastDistribution: "12 hours ago",
  nextDistribution: "12 hours"
};

const yieldSources = [
  { name: "RWA Yield", allocation: 40, apy: 4.2, yourDaily: 1.23, color: "#14b8a6" },
  { name: "Delta-Neutral", allocation: 40, apy: 6.8, yourDaily: 2.00, color: "#0f766e" },
  { name: "Altcoin Strategy", allocation: 20, apy: 15.0, yourDaily: 0.86, color: "#22d3ee" }
];

export default function DashboardView() {
  return (
    <div className="space-y-8">
      {/* Balance Cards */}
      <div className="grid grid-cols-2 gap-6">
        {/* qUSDC Balance */}
        <div className="glass-card relative overflow-hidden rounded-2xl border p-8" style={{
          borderColor: 'var(--oasis-card-border)',
          background: 'rgba(6,11,26,0.8)'
        }}>
          <div className="absolute inset-0 bg-[radial-gradient(circle_at_top,rgba(34,211,238,0.15),transparent_70%)]" />
          <div className="relative space-y-4">
            <div className="flex items-center justify-between">
              <h3 className="text-lg font-semibold" style={{color: 'var(--oasis-muted)'}}>
                qUSDC Balance
              </h3>
              <DollarSign size={24} color="var(--oasis-accent)" />
            </div>
            <div>
              <p className="text-5xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
                {mockData.qUSDCBalance.toLocaleString()}
              </p>
              <p className="text-xl mt-2" style={{color: 'var(--oasis-muted)'}}>
                ${mockData.qUSDCBalance.toLocaleString()}
              </p>
            </div>
            <p className="text-sm" style={{color: 'var(--oasis-accent)'}}>
              Available on all 10 chains
            </p>
          </div>
        </div>

        {/* sqUSDC Balance */}
        <div className="glass-card relative overflow-hidden rounded-2xl border p-8" style={{
          borderColor: 'var(--oasis-accent)',
          background: 'rgba(15,118,110,0.2)'
        }}>
          <div className="absolute inset-0 bg-[radial-gradient(circle_at_top,rgba(34,211,238,0.2),transparent_70%)]" />
          <div className="relative space-y-4">
            <div className="flex items-center justify-between">
              <h3 className="text-lg font-semibold" style={{color: 'var(--oasis-muted)'}}>
                sqUSDC Staked
              </h3>
              <TrendingUp size={24} color="var(--oasis-accent)" />
            </div>
            <div>
              <p className="text-5xl font-bold" style={{color: 'var(--oasis-accent)'}}>
                {mockData.sqUSDCBalance.toLocaleString()}
              </p>
              <p className="text-xl mt-2" style={{color: 'var(--oasis-muted)'}}>
                ${(mockData.sqUSDCBalance * mockData.exchangeRate).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
              </p>
            </div>
            <div className="flex items-center justify-between">
              <p className="text-sm" style={{color: 'var(--oasis-muted)'}}>
                Exchange Rate: 1 sqUSDC = {mockData.exchangeRate.toFixed(4)} qUSDC
              </p>
            </div>
            <div className="rounded-lg border p-3" style={{
              borderColor: 'var(--oasis-accent)',
              background: 'rgba(15,118,110,0.15)'
            }}>
              <p className="text-lg font-bold" style={{color: 'var(--oasis-accent)'}}>
                {mockData.currentAPY}% APY
              </p>
              <p className="text-xs mt-1" style={{color: 'var(--oasis-muted)'}}>
                Current staking yield
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Yield Earnings */}
      <div className="rounded-2xl border p-8" style={{
        borderColor: 'var(--oasis-card-border)',
        background: 'rgba(3,7,18,0.85)'
      }}>
        <h3 className="text-2xl font-bold mb-6" style={{color: 'var(--oasis-foreground)'}}>
          Your Yield Earnings
        </h3>
        <div className="grid grid-cols-4 gap-6">
          <div className="text-center">
            <p className="text-sm mb-2" style={{color: 'var(--oasis-muted)'}}>Today</p>
            <p className="text-3xl font-bold" style={{color: 'var(--oasis-positive)'}}>
              ${mockData.yieldToday.toFixed(2)}
            </p>
          </div>
          <div className="text-center">
            <p className="text-sm mb-2" style={{color: 'var(--oasis-muted)'}}>This Week</p>
            <p className="text-3xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
              ${mockData.yieldWeek.toFixed(2)}
            </p>
          </div>
          <div className="text-center">
            <p className="text-sm mb-2" style={{color: 'var(--oasis-muted)'}}>This Month</p>
            <p className="text-3xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
              ${mockData.yieldMonth.toFixed(2)}
            </p>
          </div>
          <div className="text-center">
            <p className="text-sm mb-2" style={{color: 'var(--oasis-muted)'}}>All Time</p>
            <p className="text-3xl font-bold" style={{color: 'var(--oasis-accent)'}}>
              ${mockData.yieldAllTime.toFixed(2)}
            </p>
          </div>
        </div>
      </div>

      {/* Yield Sources Breakdown */}
      <div className="rounded-2xl border p-8" style={{
        borderColor: 'var(--oasis-card-border)',
        background: 'rgba(3,7,18,0.85)'
      }}>
        <h3 className="text-2xl font-bold mb-6" style={{color: 'var(--oasis-foreground)'}}>
          Yield Sources
        </h3>
        <div className="space-y-4">
          {yieldSources.map((source) => (
            <div key={source.name} className="rounded-xl border p-5" style={{
              borderColor: 'var(--oasis-card-border)',
              background: 'rgba(6,11,26,0.6)'
            }}>
              <div className="flex items-center justify-between mb-3">
                <div className="flex items-center gap-3">
                  <div className="w-3 h-3 rounded-full" style={{background: source.color}} />
                  <h4 className="text-lg font-bold" style={{color: 'var(--oasis-foreground)'}}>
                    {source.name}
                  </h4>
                </div>
                <span className="text-2xl font-bold" style={{color: source.color}}>
                  {source.apy}% APY
                </span>
              </div>
              <div className="grid grid-cols-3 gap-4 text-sm">
                <div>
                  <p style={{color: 'var(--oasis-muted)'}}>Allocation</p>
                  <p className="text-lg font-semibold mt-1" style={{color: 'var(--oasis-foreground)'}}>
                    {source.allocation}%
                  </p>
                </div>
                <div>
                  <p style={{color: 'var(--oasis-muted)'}}>Your Daily Yield</p>
                  <p className="text-lg font-semibold mt-1" style={{color: 'var(--oasis-foreground)'}}>
                    ${source.yourDaily.toFixed(2)}
                  </p>
                </div>
                <div>
                  <p style={{color: 'var(--oasis-muted)'}}>Your Monthly</p>
                  <p className="text-lg font-semibold mt-1" style={{color: 'var(--oasis-foreground)'}}>
                    ${(source.yourDaily * 30).toFixed(2)}
                  </p>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Distribution Method (Solana) */}
      <div className="rounded-2xl border p-8" style={{
        borderColor: 'var(--oasis-accent)',
        background: 'rgba(15,118,110,0.15)'
      }}>
        <div className="flex items-center gap-3 mb-6">
          <Clock size={24} color="var(--oasis-accent)" />
          <h3 className="text-2xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
            Distribution Method (Solana)
          </h3>
        </div>
        <div className="space-y-4">
          <div className="flex items-center gap-4">
            <div className="flex-shrink-0">
              <div className="w-12 h-12 rounded-full flex items-center justify-center" style={{
                background: 'var(--oasis-accent)'
              }}>
                <span className="text-2xl">✓</span>
              </div>
            </div>
            <div className="flex-1">
              <h4 className="font-bold text-lg mb-1" style={{color: 'var(--oasis-foreground)'}}>
                Direct Payment via x402
              </h4>
              <p className="text-sm" style={{color: 'var(--oasis-muted)'}}>
                Yield is automatically sent to your Solana wallet daily
              </p>
            </div>
          </div>
          
          <div className="grid grid-cols-3 gap-4 pt-4 border-t" style={{borderColor: 'var(--oasis-card-border)'}}>
            <div>
              <p className="text-sm mb-1" style={{color: 'var(--oasis-muted)'}}>Last Distribution</p>
              <p className="text-base font-semibold" style={{color: 'var(--oasis-foreground)'}}>
                {mockData.lastDistribution}
              </p>
              <p className="text-sm mt-1" style={{color: 'var(--oasis-accent)'}}>
                Amount: 0.0412 SOL ($3.09)
              </p>
            </div>
            <div>
              <p className="text-sm mb-1" style={{color: 'var(--oasis-muted)'}}>Next Distribution</p>
              <p className="text-base font-semibold" style={{color: 'var(--oasis-foreground)'}}>
                {mockData.nextDistribution}
              </p>
              <p className="text-sm mt-1" style={{color: 'var(--oasis-accent)'}}>
                Estimated: 0.0415 SOL
              </p>
            </div>
            <div>
              <p className="text-sm mb-1" style={{color: 'var(--oasis-muted)'}}>Distribution Method</p>
              <p className="text-base font-semibold" style={{color: 'var(--oasis-foreground)'}}>
                x402 Protocol
              </p>
              <p className="text-sm mt-1" style={{color: 'var(--oasis-accent)'}}>
                Automatic & instant
              </p>
            </div>
          </div>

          <div className="rounded-lg border p-4 mt-4" style={{
            borderColor: 'var(--oasis-card-border)',
            background: 'rgba(3,7,18,0.6)'
          }}>
            <p className="text-sm" style={{color: 'var(--oasis-muted)'}}>
              <strong style={{color: 'var(--oasis-foreground)'}}>Note:</strong> On other chains (Ethereum, Polygon, etc.), 
              your sqUSDC value increases automatically via exchange rate updates. No claim needed!
            </p>
          </div>
        </div>
      </div>

      {/* Protocol Stats */}
      <div>
        <h3 className="text-2xl font-bold mb-6" style={{color: 'var(--oasis-foreground)'}}>
          Protocol Statistics
        </h3>
        <div className="grid grid-cols-4 gap-6">
          <div className="rounded-2xl border p-6 text-center" style={{
            borderColor: 'var(--oasis-card-border)',
            background: 'rgba(6,11,26,0.8)'
          }}>
            <p className="text-sm mb-2" style={{color: 'var(--oasis-muted)'}}>Total TVL</p>
            <p className="text-3xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
              ${(mockData.totalTVL / 1000000).toFixed(1)}M
            </p>
          </div>

          <div className="rounded-2xl border p-6 text-center" style={{
            borderColor: 'var(--oasis-card-border)',
            background: 'rgba(6,11,26,0.8)'
          }}>
            <p className="text-sm mb-2" style={{color: 'var(--oasis-muted)'}}>Total Staked</p>
            <p className="text-3xl font-bold" style={{color: 'var(--oasis-accent)'}}>
              ${(mockData.totalStaked / 1000000).toFixed(1)}M
            </p>
            <p className="text-xs mt-2" style={{color: 'var(--oasis-muted)'}}>
              {((mockData.totalStaked / mockData.totalTVL) * 100).toFixed(0)}% of qUSDC
            </p>
          </div>

          <div className="rounded-2xl border p-6 text-center" style={{
            borderColor: 'var(--oasis-card-border)',
            background: 'rgba(6,11,26,0.8)'
          }}>
            <p className="text-sm mb-2" style={{color: 'var(--oasis-muted)'}}>Daily Yield</p>
            <p className="text-3xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
              ${(mockData.dailyYield / 1000).toFixed(1)}K
            </p>
          </div>

          <div className="rounded-2xl border p-6 text-center" style={{
            borderColor: 'var(--oasis-positive)',
            background: 'rgba(20,118,96,0.15)'
          }}>
            <div className="flex items-center justify-center gap-2 mb-2">
              <Shield size={16} color="var(--oasis-positive)" />
              <p className="text-sm" style={{color: 'var(--oasis-muted)'}}>Reserve Fund</p>
            </div>
            <p className="text-3xl font-bold" style={{color: 'var(--oasis-positive)'}}>
              ${(mockData.reserveFund / 1000000).toFixed(1)}M
            </p>
            <p className="text-xs mt-2" style={{color: 'var(--oasis-muted)'}}>
              {((mockData.reserveFund / mockData.totalTVL) * 100).toFixed(0)}% buffer • Healthy ✓
            </p>
          </div>
        </div>
      </div>

      {/* Recent Distributions */}
      <div className="rounded-2xl border p-8" style={{
        borderColor: 'var(--oasis-card-border)',
        background: 'rgba(3,7,18,0.85)'
      }}>
        <div className="flex items-center justify-between mb-6">
          <h3 className="text-2xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
            Recent Distributions
          </h3>
          <button className="text-sm px-4 py-2 rounded-lg border transition" style={{
            borderColor: 'var(--oasis-accent)',
            color: 'var(--oasis-accent)'
          }}>
            View All History →
          </button>
        </div>
        <div className="space-y-3">
          {[
            { time: "12 hours ago", amount: "0.0412 SOL", usd: "$3.09", holders: 4000 },
            { time: "1 day ago", amount: "0.0408 SOL", usd: "$3.06", holders: 3985 },
            { time: "2 days ago", amount: "0.0415 SOL", usd: "$3.11", holders: 3972 }
          ].map((dist, i) => (
            <div key={i} className="flex items-center justify-between p-4 rounded-lg border" style={{
              borderColor: 'var(--oasis-card-border)',
              background: 'rgba(6,11,26,0.6)'
            }}>
              <div className="flex items-center gap-4">
                <div className="w-2 h-2 rounded-full" style={{background: 'var(--oasis-positive)'}} />
                <div>
                  <p className="font-semibold" style={{color: 'var(--oasis-foreground)'}}>
                    {dist.time}
                  </p>
                  <p className="text-sm" style={{color: 'var(--oasis-muted)'}}>
                    Distributed to {dist.holders.toLocaleString()} holders
                  </p>
                </div>
              </div>
              <div className="text-right">
                <p className="font-bold" style={{color: 'var(--oasis-accent)'}}>
                  {dist.amount}
                </p>
                <p className="text-sm" style={{color: 'var(--oasis-muted)'}}>
                  {dist.usd}
                </p>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

