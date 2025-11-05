"use client";

import { MigrationConfig } from "@/app/migrate-token/page-content";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Wallet, Search } from "lucide-react";
import Image from "next/image";

type ConnectDetectStepProps = {
  config: MigrationConfig;
  updateConfig: (updates: Partial<MigrationConfig>) => void;
};

const sourceChains = [
  { name: "Ethereum", icon: "/ETH.svg" },
  { name: "Polygon", icon: "/MATIC.svg" },
  { name: "Solana", icon: "/SOL.svg" },
  { name: "Arbitrum", icon: "/ARB.png" },
  { name: "Base", icon: "/BASE.svg" },
  { name: "BNB Chain", icon: "/BNB.svg" },
];

export function ConnectDetectStep({ config, updateConfig }: ConnectDetectStepProps) {
  const handleConnectWallet = () => {
    // Mock wallet connection
    updateConfig({ 
      walletConnected: true,
      walletAddress: "0x1234567890abcdef1234567890abcdef12345678"
    });
  };

  const handleDetectToken = () => {
    // Mock token detection
    updateConfig({
      existingTokenName: "USD Coin",
      existingTokenSymbol: "USDC",
      existingBalance: "1000000",
      tokenImage: "/ETH.svg", // Mock - would be actual token logo
    });
  };

  return (
    <div className="space-y-8">
      <div>
        <h3 className="text-2xl font-bold" style={{color: 'var(--oasis-foreground)'}}>Connect & Detect Token</h3>
        <p className="mt-3 text-base" style={{color: 'var(--oasis-muted)'}}>
          Connect your wallet and identify the existing token you want to upgrade to Web4.
        </p>
      </div>

      {/* Wallet Connection */}
      {!config.walletConnected ? (
        <div className="glass-card relative overflow-hidden rounded-2xl border p-8 text-center" style={{
          borderColor: 'var(--oasis-card-border)',
          background: 'rgba(6,11,26,0.6)'
        }}>
          <div className="absolute inset-0 bg-[radial-gradient(circle_at_top,rgba(34,211,238,0.18),transparent_60%)]" />
          <div className="relative">
            <Wallet size={48} className="mx-auto mb-4" color="var(--oasis-accent)" />
            <h4 className="text-xl font-bold mb-2" style={{color: 'var(--oasis-foreground)'}}>Connect Your Wallet</h4>
            <p className="text-sm mb-6" style={{color: 'var(--oasis-muted)'}}>
              Connect your wallet to detect existing tokens
            </p>
            <Button 
              onClick={handleConnectWallet}
              size="lg" 
              className="font-semibold"
              style={{
                background: 'var(--oasis-accent)',
                color: '#041321'
              }}
            >
              <Wallet size={20} className="mr-2" />
              Connect Wallet
            </Button>
          </div>
        </div>
      ) : (
        <div className="space-y-6">
          {/* Source Chain Selection */}
          <div className="space-y-3">
            <label className="text-base font-semibold" style={{color: 'var(--oasis-foreground)'}}>
              Source Chain
            </label>
            <div className="grid grid-cols-3 gap-4">
              {sourceChains.map((chain) => (
                <button
                  key={chain.name}
                  onClick={() => updateConfig({ existingChain: chain.name })}
                  className={cn(
                    "glass-card relative overflow-hidden rounded-xl border p-4 transition hover:scale-105",
                    config.existingChain === chain.name
                      ? "border-[var(--oasis-accent)]/80 ring-2 ring-[var(--oasis-accent)]/50"
                      : "hover:border-[var(--oasis-accent)]/50"
                  )}
                  style={{
                    borderColor: config.existingChain === chain.name ? 'var(--oasis-accent)' : 'var(--oasis-card-border)',
                    background: config.existingChain === chain.name ? 'rgba(15,118,110,0.2)' : 'rgba(6,11,26,0.6)'
                  }}
                >
                  <div className="flex items-center justify-center gap-3">
                    <Image src={chain.icon} alt={chain.name} width={24} height={24} />
                    <span className="font-semibold" style={{color: 'var(--oasis-foreground)'}}>{chain.name}</span>
                  </div>
                </button>
              ))}
            </div>
          </div>

          {/* Token Contract Input */}
          <div className="space-y-3">
            <label className="text-base font-semibold" style={{color: 'var(--oasis-foreground)'}}>
              Token Contract Address
            </label>
            <div className="flex gap-3">
              <Input
                placeholder="0x... or paste token contract address"
                value={config.existingTokenContract}
                onChange={(e) => updateConfig({ existingTokenContract: e.target.value })}
                className="h-12 text-sm font-mono"
                style={{
                  background: 'rgba(3,7,18,0.8)',
                  borderColor: 'var(--oasis-card-border)',
                  color: 'var(--oasis-foreground)'
                }}
              />
              <Button 
                onClick={handleDetectToken}
                size="lg"
                className="font-semibold"
                style={{
                  background: 'var(--oasis-accent)',
                  color: '#041321'
                }}
              >
                <Search size={20} className="mr-2" />
                Detect
              </Button>
            </div>
            <p className="text-sm" style={{color: 'var(--oasis-muted)'}}>
              Enter the contract address of your existing token
            </p>
          </div>

          {/* Detected Token Display */}
          {config.existingTokenSymbol && (
            <div className="glass-card relative overflow-hidden rounded-2xl border p-6" style={{
              borderColor: 'var(--oasis-positive)',
              background: 'rgba(20,118,96,0.15)'
            }}>
              <div className="absolute inset-0 bg-[radial-gradient(circle_at_top,rgba(34,211,238,0.18),transparent_60%)]" />
              <div className="relative">
                <div className="flex items-center justify-between mb-4">
                  <h4 className="text-lg font-bold" style={{color: 'var(--oasis-foreground)'}}>Token Detected</h4>
                  <span className="text-xs px-3 py-1 rounded-full" style={{
                    background: 'var(--oasis-positive)',
                    color: '#041321'
                  }}>
                    ✓ Verified
                  </span>
                </div>
                <div className="grid grid-cols-2 gap-4 text-sm">
                  <div>
                    <p style={{color: 'var(--oasis-muted)'}}>Name:</p>
                    <p className="font-bold" style={{color: 'var(--oasis-foreground)'}}>{config.existingTokenName}</p>
                  </div>
                  <div>
                    <p style={{color: 'var(--oasis-muted)'}}>Symbol:</p>
                    <p className="font-bold text-lg" style={{color: 'var(--oasis-accent)'}}>{config.existingTokenSymbol}</p>
                  </div>
                  <div>
                    <p style={{color: 'var(--oasis-muted)'}}>Your Balance:</p>
                    <p className="font-bold" style={{color: 'var(--oasis-foreground)'}}>
                      {parseFloat(config.existingBalance).toLocaleString()} {config.existingTokenSymbol}
                    </p>
                  </div>
                  <div>
                    <p style={{color: 'var(--oasis-muted)'}}>Chain:</p>
                    <p className="font-bold" style={{color: 'var(--oasis-foreground)'}}>{config.existingChain}</p>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

