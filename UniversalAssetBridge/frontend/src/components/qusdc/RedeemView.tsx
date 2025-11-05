"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ArrowRight, AlertCircle } from "lucide-react";
import Image from "next/image";

const outputOptions = [
  { symbol: "USDC", name: "USD Coin", icon: "/ETH.svg", rate: 1 },
  { symbol: "USDT", name: "Tether", icon: "/ETH.svg", rate: 1 },
  { symbol: "DAI", name: "Dai Stablecoin", icon: "/ETH.svg", rate: 1 },
  { symbol: "ETH", name: "Ethereum", icon: "/ETH.svg", rate: 0.0005 }
];

const mockData = {
  qUSDCBalance: 5000,
  reserveFundHealth: "Healthy",
  redemptionFee: 0.1 // 0.1%
};

export default function RedeemView() {
  const [selectedOutput, setSelectedOutput] = useState(outputOptions[0]);
  const [amount, setAmount] = useState("");
  const [redeeming, setRedeeming] = useState(false);

  const outputAmount = (parseFloat(amount || "0") * selectedOutput.rate) * (1 - mockData.redemptionFee / 100);
  const fee = parseFloat(amount || "0") * (mockData.redemptionFee / 100);

  const handleRedeem = async () => {
    setRedeeming(true);
    await new Promise(resolve => setTimeout(resolve, 2000));
    setRedeeming(false);
    alert(`Redeemed ${amount} qUSDC for ${outputAmount.toFixed(2)} ${selectedOutput.symbol}!`);
  };

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      {/* Reserve Fund Status */}
      <div className="rounded-xl border p-6" style={{
        borderColor: 'var(--oasis-positive)',
        background: 'rgba(20,118,96,0.15)'
      }}>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-3 h-3 rounded-full" style={{background: 'var(--oasis-positive)'}} />
            <p className="font-semibold" style={{color: 'var(--oasis-foreground)'}}>
              Reserve Fund Status: {mockData.reserveFundHealth} ✓
            </p>
          </div>
          <p className="text-sm" style={{color: 'var(--oasis-muted)'}}>
            Instant redemptions available
          </p>
        </div>
      </div>

      {/* Main Redeem Card */}
      <div className="glass-card relative overflow-hidden rounded-2xl border p-8" style={{
        borderColor: 'var(--oasis-card-border)',
        background: 'rgba(3,7,18,0.85)'
      }}>
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_top,rgba(34,211,238,0.15),transparent_70%)]" />
        <div className="relative space-y-6">
          <div>
            <h3 className="text-3xl font-bold mb-2" style={{color: 'var(--oasis-foreground)'}}>
              Redeem qUSDC
            </h3>
            <p className="text-base" style={{color: 'var(--oasis-muted)'}}>
              Burn qUSDC to withdraw your collateral. 1:1 redemption guaranteed.
            </p>
          </div>

          {/* Amount Input */}
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <label className="text-sm font-semibold" style={{color: 'var(--oasis-foreground)'}}>
                qUSDC to Redeem
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

          {/* Output Selection */}
          <div className="space-y-3">
            <label className="text-sm font-semibold" style={{color: 'var(--oasis-foreground)'}}>
              Receive As
            </label>
            <div className="grid grid-cols-4 gap-3">
              {outputOptions.map((option) => (
                <button
                  key={option.symbol}
                  onClick={() => setSelectedOutput(option)}
                  className="p-4 rounded-lg border transition"
                  style={{
                    borderColor: selectedOutput.symbol === option.symbol 
                      ? 'var(--oasis-accent)' 
                      : 'var(--oasis-card-border)',
                    background: selectedOutput.symbol === option.symbol
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

          {/* Preview */}
          <div className="rounded-xl border p-6" style={{
            borderColor: 'var(--oasis-accent)',
            background: 'rgba(15,118,110,0.15)'
          }}>
            <div className="flex items-center justify-center gap-4 mb-4">
              <div className="text-center">
                <p className="text-sm mb-1" style={{color: 'var(--oasis-muted)'}}>You burn</p>
                <p className="text-2xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
                  {parseFloat(amount || "0").toFixed(2)} qUSDC
                </p>
              </div>
              <ArrowRight size={24} color="var(--oasis-accent)" />
              <div className="text-center">
                <p className="text-sm mb-1" style={{color: 'var(--oasis-muted)'}}>You receive</p>
                <p className="text-2xl font-bold" style={{color: 'var(--oasis-accent)'}}>
                  {outputAmount.toFixed(2)} {selectedOutput.symbol}
                </p>
              </div>
            </div>
            <div className="pt-4 border-t space-y-2 text-sm" style={{borderColor: 'var(--oasis-card-border)'}}>
              <div className="flex justify-between">
                <span style={{color: 'var(--oasis-muted)'}}>Redemption Fee:</span>
                <span className="font-semibold" style={{color: 'var(--oasis-foreground)'}}>
                  {mockData.redemptionFee}% ({fee.toFixed(2)} qUSDC)
                </span>
              </div>
              <div className="flex justify-between">
                <span style={{color: 'var(--oasis-muted)'}}>Processing Time:</span>
                <span className="font-semibold" style={{color: 'var(--oasis-foreground)'}}>
                  Instant
                </span>
              </div>
            </div>
          </div>

          {/* Redeem Button */}
          <Button
            onClick={handleRedeem}
            disabled={!amount || parseFloat(amount) <= 0 || redeeming}
            className="w-full text-lg font-bold h-14"
            style={{
              background: 'var(--oasis-accent)',
              color: '#041321'
            }}
          >
            {redeeming ? 'Redeeming...' : 'Redeem qUSDC'}
          </Button>
        </div>
      </div>

      {/* Warning */}
      <div className="rounded-xl border p-6" style={{
        borderColor: 'var(--oasis-warning)',
        background: 'rgba(250,204,21,0.1)'
      }}>
        <div className="flex items-start gap-3">
          <AlertCircle size={20} color="var(--oasis-warning)" className="flex-shrink-0 mt-1" />
          <div className="space-y-2 text-sm" style={{color: 'var(--oasis-muted)'}}>
            <p>
              <strong style={{color: 'var(--oasis-foreground)'}}>Note:</strong> If you have staked sqUSDC, 
              you must unstake it first before redeeming. This converts sqUSDC → qUSDC at the current 
              exchange rate (including all accrued yield).
            </p>
            <p>
              Redemptions withdraw from the reserve fund and yield strategies proportionally. 
              Large redemptions (&gt;$1M) may take 24-48 hours.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}

