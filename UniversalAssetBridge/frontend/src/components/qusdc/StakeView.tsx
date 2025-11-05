"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { TrendingUp, ArrowDownUp, Info, ArrowRight } from "lucide-react";

const mockData = {
  qUSDCBalance: 5000,
  sqUSDCBalance: 8500,
  exchangeRate: 1.0309,
  currentAPY: 12.5
};

export default function StakeView() {
  const [activeAction, setActiveAction] = useState<"stake" | "unstake">("stake");
  const [amount, setAmount] = useState("");
  const [processing, setProcessing] = useState(false);

  const handleStake = async () => {
    setProcessing(true);
    await new Promise(resolve => setTimeout(resolve, 2000));
    setProcessing(false);
    alert(`Staked ${amount} qUSDC!`);
  };

  const handleUnstake = async () => {
    setProcessing(true);
    await new Promise(resolve => setTimeout(resolve, 2000));
    setProcessing(false);
    alert(`Unstaked ${amount} sqUSDC!`);
  };

  const sqUSDCToReceive = parseFloat(amount || "0") / mockData.exchangeRate;
  const qUSDCToReceive = parseFloat(amount || "0") * mockData.exchangeRate;

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      {/* APY Highlight */}
      <div className="text-center rounded-2xl border p-8" style={{
        borderColor: 'var(--oasis-accent)',
        background: 'rgba(15,118,110,0.2)'
      }}>
        <div className="flex items-center justify-center gap-3 mb-3">
          <TrendingUp size={32} color="var(--oasis-accent)" />
          <h3 className="text-5xl font-bold" style={{color: 'var(--oasis-accent)'}}>
            {mockData.currentAPY}% APY
          </h3>
        </div>
        <p className="text-base" style={{color: 'var(--oasis-muted)'}}>
          Current staking yield from diversified strategies
        </p>
      </div>

      {/* Stake/Unstake Interface */}
      <div className="glass-card relative overflow-hidden rounded-2xl border p-8" style={{
        borderColor: 'var(--oasis-card-border)',
        background: 'rgba(3,7,18,0.85)'
      }}>
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_top,rgba(34,211,238,0.15),transparent_70%)]" />
        <div className="relative space-y-6">
          {/* Tabs */}
          <div className="flex gap-4">
            <button
              onClick={() => setActiveAction("stake")}
              className="flex-1 py-3 rounded-xl font-bold transition"
              style={{
                background: activeAction === "stake" ? 'var(--oasis-accent)' : 'rgba(8,11,26,0.6)',
                color: activeAction === "stake" ? '#041321' : 'var(--oasis-muted)'
              }}
            >
              Stake qUSDC
            </button>
            <button
              onClick={() => setActiveAction("unstake")}
              className="flex-1 py-3 rounded-xl font-bold transition"
              style={{
                background: activeAction === "unstake" ? 'var(--oasis-accent)' : 'rgba(8,11,26,0.6)',
                color: activeAction === "unstake" ? '#041321' : 'var(--oasis-muted)'
              }}
            >
              Unstake sqUSDC
            </button>
          </div>

          {activeAction === "stake" && (
            <div className="space-y-6">
              <div>
                <h4 className="text-2xl font-bold mb-2" style={{color: 'var(--oasis-foreground)'}}>
                  Stake qUSDC → Earn Yield
                </h4>
                <p className="text-base" style={{color: 'var(--oasis-muted)'}}>
                  Stake qUSDC to receive sqUSDC and start earning {mockData.currentAPY}% APY
                </p>
              </div>

              {/* Amount Input */}
              <div className="space-y-3">
                <div className="flex items-center justify-between">
                  <label className="text-sm font-semibold" style={{color: 'var(--oasis-foreground)'}}>
                    Amount to Stake
                  </label>
                  <button className="text-sm px-3 py-1 rounded-lg" style={{
                    background: 'var(--oasis-accent-soft)',
                    color: 'var(--oasis-accent)'
                  }}>
                    Max: {mockData.qUSDCBalance.toLocaleString()}
                  </button>
                </div>
                <Input
                  type="number"
                  placeholder="0.00"
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                  className="h-16 text-2xl font-bold"
                  style={{
                    background: 'rgba(3,7,18,0.9)',
                    borderColor: 'var(--oasis-card-border)',
                    color: 'var(--oasis-foreground)'
                  }}
                />
              </div>

              {/* Preview */}
              <div className="rounded-xl border p-6" style={{
                borderColor: 'var(--oasis-accent)',
                background: 'rgba(15,118,110,0.15)'
              }}>
                <div className="flex items-center justify-center gap-4 mb-4">
                  <div className="text-center">
                    <p className="text-sm mb-1" style={{color: 'var(--oasis-muted)'}}>You stake</p>
                    <p className="text-2xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
                      {parseFloat(amount || "0").toFixed(2)} qUSDC
                    </p>
                  </div>
                  <ArrowRight size={24} color="var(--oasis-accent)" />
                  <div className="text-center">
                    <p className="text-sm mb-1" style={{color: 'var(--oasis-muted)'}}>You receive</p>
                    <p className="text-2xl font-bold" style={{color: 'var(--oasis-accent)'}}>
                      {sqUSDCToReceive.toFixed(2)} sqUSDC
                    </p>
                  </div>
                </div>
                <div className="pt-4 border-t space-y-2 text-sm" style={{borderColor: 'var(--oasis-card-border)'}}>
                  <div className="flex justify-between">
                    <span style={{color: 'var(--oasis-muted)'}}>Exchange Rate:</span>
                    <span className="font-semibold" style={{color: 'var(--oasis-foreground)'}}>
                      1 sqUSDC = {mockData.exchangeRate.toFixed(4)} qUSDC
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span style={{color: 'var(--oasis-muted)'}}>Current APY:</span>
                    <span className="font-bold" style={{color: 'var(--oasis-accent)'}}>
                      {mockData.currentAPY}%
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span style={{color: 'var(--oasis-muted)'}}>Estimated Daily Yield:</span>
                    <span className="font-semibold" style={{color: 'var(--oasis-positive)'}}>
                      ${((parseFloat(amount || "0") * mockData.currentAPY / 100) / 365).toFixed(2)}
                    </span>
                  </div>
                </div>
              </div>

              {/* Stake Button */}
              <Button
                onClick={handleStake}
                disabled={!amount || parseFloat(amount) <= 0 || processing}
                className="w-full text-lg font-bold h-14"
                style={{
                  background: 'var(--oasis-accent)',
                  color: '#041321'
                }}
              >
                {processing ? 'Staking...' : 'Stake qUSDC'}
              </Button>
            </div>
          )}

          {activeAction === "unstake" && (
            <div className="space-y-6">
              <div>
                <h4 className="text-2xl font-bold mb-2" style={{color: 'var(--oasis-foreground)'}}>
                  Unstake sqUSDC → Get qUSDC
                </h4>
                <p className="text-base" style={{color: 'var(--oasis-muted)'}}>
                  Convert sqUSDC back to qUSDC at current exchange rate
                </p>
              </div>

              {/* Amount Input */}
              <div className="space-y-3">
                <div className="flex items-center justify-between">
                  <label className="text-sm font-semibold" style={{color: 'var(--oasis-foreground)'}}>
                    sqUSDC to Unstake
                  </label>
                  <button className="text-sm px-3 py-1 rounded-lg" style={{
                    background: 'var(--oasis-accent-soft)',
                    color: 'var(--oasis-accent)'
                  }}>
                    Max: {mockData.sqUSDCBalance.toLocaleString()}
                  </button>
                </div>
                <Input
                  type="number"
                  placeholder="0.00"
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                  className="h-16 text-2xl font-bold"
                  style={{
                    background: 'rgba(3,7,18,0.9)',
                    borderColor: 'var(--oasis-card-border)',
                    color: 'var(--oasis-foreground)'
                  }}
                />
              </div>

              {/* Preview */}
              <div className="rounded-xl border p-6" style={{
                borderColor: 'var(--oasis-accent)',
                background: 'rgba(15,118,110,0.15)'
              }}>
                <div className="flex items-center justify-center gap-4 mb-4">
                  <div className="text-center">
                    <p className="text-sm mb-1" style={{color: 'var(--oasis-muted)'}}>You unstake</p>
                    <p className="text-2xl font-bold" style={{color: 'var(--oasis-accent)'}}>
                      {parseFloat(amount || "0").toFixed(2)} sqUSDC
                    </p>
                  </div>
                  <ArrowRight size={24} color="var(--oasis-accent)" />
                  <div className="text-center">
                    <p className="text-sm mb-1" style={{color: 'var(--oasis-muted)'}}>You receive</p>
                    <p className="text-2xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
                      {qUSDCToReceive.toFixed(2)} qUSDC
                    </p>
                  </div>
                </div>
                <div className="pt-4 border-t space-y-2 text-sm" style={{borderColor: 'var(--oasis-card-border)'}}>
                  <div className="flex justify-between">
                    <span style={{color: 'var(--oasis-muted)'}}>Exchange Rate:</span>
                    <span className="font-semibold" style={{color: 'var(--oasis-foreground)'}}>
                      1 sqUSDC = {mockData.exchangeRate.toFixed(4)} qUSDC
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span style={{color: 'var(--oasis-muted)'}}>Value Gained:</span>
                    <span className="font-bold" style={{color: 'var(--oasis-positive)'}}>
                      +{((mockData.exchangeRate - 1) * 100).toFixed(2)}%
                    </span>
                  </div>
                </div>
              </div>

              {/* Unstake Button */}
              <Button
                onClick={handleUnstake}
                disabled={!amount || parseFloat(amount) <= 0 || processing}
                className="w-full text-lg font-bold h-14"
                style={{
                  background: 'var(--oasis-accent)',
                  color: '#041321'
                }}
              >
                {processing ? 'Unstaking...' : 'Unstake to qUSDC'}
              </Button>
            </div>
          )}
        </div>
      </div>

      {/* Yield Projection */}
      <div className="rounded-2xl border p-8" style={{
        borderColor: 'var(--oasis-card-border)',
        background: 'rgba(6,11,26,0.6)'
      }}>
        <h4 className="text-xl font-bold mb-4" style={{color: 'var(--oasis-foreground)'}}>
          Yield Projection
        </h4>
        <div className="grid grid-cols-3 gap-6">
          <div className="text-center">
            <p className="text-sm mb-2" style={{color: 'var(--oasis-muted)'}}>Daily</p>
            <p className="text-2xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
              ${((parseFloat(amount || "0") * mockData.currentAPY / 100) / 365).toFixed(2)}
            </p>
          </div>
          <div className="text-center">
            <p className="text-sm mb-2" style={{color: 'var(--oasis-muted)'}}>Monthly</p>
            <p className="text-2xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
              ${((parseFloat(amount || "0") * mockData.currentAPY / 100) / 12).toFixed(2)}
            </p>
          </div>
          <div className="text-center">
            <p className="text-sm mb-2" style={{color: 'var(--oasis-muted)'}}>Yearly</p>
            <p className="text-2xl font-bold" style={{color: 'var(--oasis-accent)'}}>
              ${(parseFloat(amount || "0") * mockData.currentAPY / 100).toFixed(2)}
            </p>
          </div>
        </div>
      </div>

      {/* Info */}
      <div className="rounded-xl border p-6" style={{
        borderColor: 'var(--oasis-card-border)',
        background: 'rgba(6,11,26,0.6)'
      }}>
        <div className="flex items-start gap-3">
          <Info size={20} color="var(--oasis-accent)" className="flex-shrink-0 mt-1" />
          <div className="space-y-2 text-sm" style={{color: 'var(--oasis-muted)'}}>
            <p>
              <strong style={{color: 'var(--oasis-foreground)'}}>How staking works:</strong> When you stake qUSDC, 
              you receive sqUSDC at the current exchange rate. The exchange rate increases daily as yield accrues 
              from RWA, delta-neutral, and altcoin strategies.
            </p>
            <p>
              <strong style={{color: 'var(--oasis-foreground)'}}>On Solana:</strong> You receive daily yield payments 
              directly to your wallet via x402 (automatic, no claim needed).
            </p>
            <p>
              <strong style={{color: 'var(--oasis-foreground)'}}>On other chains:</strong> Your sqUSDC value increases 
              automatically via exchange rate updates. Yield compounds if left staked.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}

