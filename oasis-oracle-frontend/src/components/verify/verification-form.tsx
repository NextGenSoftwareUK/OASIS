"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { CheckCircle2, Loader2 } from "lucide-react";
import type { ChainType } from "@/types/chains";
import type { VerificationRequest } from "@/types/verification";

const SUPPORTED_CHAINS: ChainType[] = [
  "Solana",
  "Ethereum",
  "Polygon",
  "Arbitrum",
  "Base",
  "Radix",
  "Avalanche",
  "Optimism",
  "BNBChain",
  "Bitcoin",
];

type VerificationFormProps = {
  onSubmit: (request: VerificationRequest) => void;
  isLoading?: boolean;
};

export function VerificationForm({ onSubmit, isLoading = false }: VerificationFormProps) {
  const [selectedChain, setSelectedChain] = useState<ChainType>("Solana");
  const [transactionHash, setTransactionHash] = useState("");
  const [confirmations, setConfirmations] = useState("32");

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit({
      chain: selectedChain,
      transactionHash,
      requiredConfirmations: parseInt(confirmations, 10),
    });
  };

  const getRecommendedConfirmations = (chain: ChainType): number => {
    const recommendations: Record<string, number> = {
      Solana: 32,
      Ethereum: 12,
      Polygon: 100,
      Arbitrum: 64,
      Base: 12,
      Radix: 10,
      Avalanche: 20,
      Optimism: 64,
      BNBChain: 15,
      Bitcoin: 6,
    };
    return recommendations[chain] || 10;
  };

  const handleChainSelect = (chain: ChainType) => {
    setSelectedChain(chain);
    setConfirmations(getRecommendedConfirmations(chain).toString());
  };

  return (
    <Card variant="gradient" className="max-w-4xl mx-auto">
      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Step 1: Select Chain */}
        <div>
          <label className="block text-sm font-medium text-[var(--color-foreground)] mb-3">
            1. SELECT SOURCE CHAIN
          </label>
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-5 gap-3">
            {SUPPORTED_CHAINS.map((chain) => (
              <button
                key={chain}
                type="button"
                onClick={() => handleChainSelect(chain)}
                className={`
                  relative p-4 rounded-xl border-2 transition-all
                  ${
                    selectedChain === chain
                      ? "border-[var(--accent)] bg-[rgba(34,211,238,0.12)]"
                      : "border-[var(--color-card-border)]/50 bg-[rgba(6,11,26,0.5)] hover:border-[var(--accent)]/50"
                  }
                `}
              >
                <div className="flex flex-col items-center gap-2">
                  <div className="h-10 w-10 rounded-lg bg-[var(--accent-soft)] flex items-center justify-center">
                    <span className="text-xs font-bold text-[var(--accent)]">
                      {chain.slice(0, 3).toUpperCase()}
                    </span>
                  </div>
                  <span className="text-xs font-medium">{chain}</span>
                  {selectedChain === chain && (
                    <CheckCircle2 className="absolute top-2 right-2 h-4 w-4 text-[var(--accent)]" />
                  )}
                </div>
              </button>
            ))}
          </div>
        </div>

        {/* Step 2: Enter Transaction Hash */}
        <div>
          <label className="block text-sm font-medium text-[var(--color-foreground)] mb-3">
            2. ENTER TRANSACTION HASH
          </label>
          <input
            type="text"
            value={transactionHash}
            onChange={(e) => setTransactionHash(e.target.value)}
            placeholder="0x5f3d2e1c4b6a7d9e8f1c2b3a4d5e6f7g8h9i0j1k2l3m4n5o6p7q..."
            className="w-full px-4 py-3 rounded-lg bg-[rgba(5,5,16,0.8)] border border-[var(--color-card-border)]/50 text-[var(--color-foreground)] placeholder:text-[var(--muted)] focus:border-[var(--accent)] focus:outline-none transition font-mono text-sm"
            required
          />
          <p className="mt-2 text-xs text-[var(--muted)]">
            Paste the transaction hash you want to verify
          </p>
        </div>

        {/* Step 3: Required Confirmations */}
        <div>
          <label className="block text-sm font-medium text-[var(--color-foreground)] mb-3">
            3. REQUIRED CONFIRMATIONS
          </label>
          <div className="flex items-center gap-4">
            <input
              type="number"
              value={confirmations}
              onChange={(e) => setConfirmations(e.target.value)}
              min="1"
              max="1000"
              className="w-32 px-4 py-3 rounded-lg bg-[rgba(5,5,16,0.8)] border border-[var(--color-card-border)]/50 text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none transition font-mono text-center"
              required
            />
            <Badge variant="info" size="sm">
              Recommended: {getRecommendedConfirmations(selectedChain)} for {selectedChain}
            </Badge>
          </div>
          <p className="mt-2 text-xs text-[var(--muted)]">
            Number of block confirmations required for finality
          </p>
        </div>

        {/* Submit Button */}
        <div className="flex items-center justify-end gap-4 pt-4 border-t border-[var(--color-card-border)]/30">
          <Button
            type="button"
            variant="secondary"
            onClick={() => {
              setTransactionHash("");
              setConfirmations(getRecommendedConfirmations(selectedChain).toString());
            }}
            disabled={isLoading}
          >
            Clear
          </Button>
          <Button
            type="submit"
            variant="primary"
            disabled={isLoading || !transactionHash}
            className="min-w-[200px]"
          >
            {isLoading ? (
              <>
                <Loader2 className="h-4 w-4 animate-spin" />
                Verifying...
              </>
            ) : (
              <>
                <CheckCircle2 className="h-4 w-4" />
                Verify Transaction
              </>
            )}
          </Button>
        </div>
      </form>
    </Card>
  );
}


