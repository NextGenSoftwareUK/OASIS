"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ArrowRight, Info } from "lucide-react";
import Image from "next/image";

const collateralOptions = [
  { symbol: "USDC", name: "USD Coin", icon: "/ETH.svg", rate: 1, decimals: 6 },
  { symbol: "USDT", name: "Tether", icon: "/ETH.svg", rate: 1, decimals: 6 },
  { symbol: "DAI", name: "Dai Stablecoin", icon: "/ETH.svg", rate: 1, decimals: 18 },
  { symbol: "ETH", name: "Ethereum", icon: "/ETH.svg", rate: 0.0005, decimals: 18 }
];

export default function MintView() {
  const [selectedCollateral, setSelectedCollateral] = useState(collateralOptions[0]);
  const [amount, setAmount] = useState("");
  const [minting, setMinting] = useState(false);

  const qUSDCAmount = parseFloat(amount || "0") * selectedCollateral.rate;

  const handleMint = async () => {
    setMinting(true);
    // Simulate minting
    await new Promise(resolve => setTimeout(resolve, 2000));
    setMinting(false);
    alert(`Minted ${qUSDCAmount.toFixed(2)} qUSDC!`);
  };

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      {/* Main Mint Card */}
      <div className="glass-card relative overflow-hidden rounded-2xl border p-8" style={{
        borderColor: 'var(--oasis-card-border)',
        background: 'rgba(3,7,18,0.85)'
      }}>
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_top,rgba(34,211,238,0.15),transparent_70%)]" />
        <div className="relative space-y-6">
          <div>
            <h3 className="text-3xl font-bold mb-2" style={{color: 'var(--oasis-foreground)'}}>
              Mint qUSDC
            </h3>
            <p className="text-base" style={{color: 'var(--oasis-muted)'}}>
              Deposit collateral to mint qUSDC. Available on all 10 chains instantly.
            </p>
          </div>

          {/* Collateral Selection */}
          <div className="space-y-3">
            <label className="text-sm font-semibold" style={{color: 'var(--oasis-foreground)'}}>
              Select Collateral
            </label>
            <div className="grid grid-cols-4 gap-3">
              {collateralOptions.map((option) => (
                <button
                  key={option.symbol}
                  onClick={() => setSelectedCollateral(option)}
                  className="p-4 rounded-lg border transition"
                  style={{
                    borderColor: selectedCollateral.symbol === option.symbol 
                      ? 'var(--oasis-accent)' 
                      : 'var(--oasis-card-border)',
                    background: selectedCollateral.symbol === option.symbol
                      ? 'rgba(15,118,110,0.2)'
                      : 'rgba(6,11,26,0.6)'
                  }}
                >
                  <Image src={option.icon} alt={option.symbol} width={32} height={32} className="mx-auto mb-2" />
                  <p className="font-semibold" style={{color: 'var(--oasis-foreground)'}}>
                    {option.symbol}
                  </p>
                </button>
              ))}
            </div>
          </div>

          {/* Amount Input */}
          <div className="space-y-3">
            <label className="text-sm font-semibold" style={{color: 'var(--oasis-foreground)'}}>
              Amount to Deposit
            </label>
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
            <p className="text-sm" style={{color: 'var(--oasis-muted)'}}>
              Balance: 10,000 {selectedCollateral.symbol}
            </p>
          </div>

          {/* Preview */}
          <div className="rounded-xl border p-6" style={{
            borderColor: 'var(--oasis-accent)',
            background: 'rgba(15,118,110,0.15)'
          }}>
            <div className="flex items-center justify-between mb-4">
              <p className="text-sm" style={{color: 'var(--oasis-muted)'}}>You will receive:</p>
              <ArrowRight size={20} color="var(--oasis-accent)" />
            </div>
            <p className="text-4xl font-bold" style={{color: 'var(--oasis-accent)'}}>
              {qUSDCAmount.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} qUSDC
            </p>
            <p className="text-sm mt-3" style={{color: 'var(--oasis-muted)'}}>
              ✓ Available on all 10 chains immediately
            </p>
            <p className="text-sm" style={{color: 'var(--oasis-muted)'}}>
              ✓ Backed by diversified yield strategies
            </p>
            <p className="text-sm" style={{color: 'var(--oasis-muted)'}}>
              ✓ Redeemable 1:1 for USDC anytime
            </p>
          </div>

          {/* Mint Button */}
          <Button
            onClick={handleMint}
            disabled={!amount || parseFloat(amount) <= 0 || minting}
            className="w-full text-lg font-bold h-14"
            style={{
              background: 'var(--oasis-accent)',
              color: '#041321'
            }}
          >
            {minting ? 'Minting...' : 'Mint qUSDC'}
          </Button>
        </div>
      </div>

      {/* Info Card */}
      <div className="rounded-xl border p-6" style={{
        borderColor: 'var(--oasis-card-border)',
        background: 'rgba(6,11,26,0.6)'
      }}>
        <div className="flex items-start gap-3">
          <Info size={20} color="var(--oasis-accent)" className="flex-shrink-0 mt-1" />
          <div className="space-y-2 text-sm" style={{color: 'var(--oasis-muted)'}}>
            <p>
              <strong style={{color: 'var(--oasis-foreground)'}}>How it works:</strong> Your collateral is deposited 
              into the qUSDC vault and allocated across three yield strategies (40% RWA, 40% Delta-Neutral, 20% Altcoin).
            </p>
            <p>
              qUSDC is minted 1:1 and appears in your wallet on ALL chains via HyperDrive. 
              You can stake it to earn yield or use it for trading, lending, and DeFi across the entire Web4 ecosystem.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}

