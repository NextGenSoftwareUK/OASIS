/**
 * Manual Distribution Panel
 * 
 * Allows NFT creators to manually trigger revenue distributions
 * Perfect for hackathon demo and MVP
 */

"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";

type ManualDistributionPanelProps = {
  nftMintAddress: string;
  baseUrl: string;
  token?: string;
  onDistributionComplete?: (result: any) => void;
};

export function ManualDistributionPanel({ 
  nftMintAddress, 
  baseUrl, 
  token,
  onDistributionComplete 
}: ManualDistributionPanelProps) {
  const [revenueAmount, setRevenueAmount] = useState<string>('');
  const [distributing, setDistributing] = useState(false);
  const [lastResult, setLastResult] = useState<any>(null);
  const [error, setError] = useState<string | null>(null);

  const handleDistribute = async () => {
    try {
      setDistributing(true);
      setError(null);
      
      const amount = parseFloat(revenueAmount);
      if (isNaN(amount) || amount <= 0) {
        setError('Please enter a valid amount');
        return;
      }
      
      // Call x402 webhook (test endpoint for demo)
      const response = await fetch(`${baseUrl}/api/x402/distribute-test`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(token && { 'Authorization': `Bearer ${token}` })
        },
        body: JSON.stringify({
          nftMintAddress: nftMintAddress,
          amount: amount
        })
      });
      
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }
      
      const data = await response.json();
      
      if (data.success) {
        setLastResult(data.result);
        setRevenueAmount('');
        onDistributionComplete?.(data.result);
      } else {
        throw new Error(data.error || 'Distribution failed');
      }
      
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to distribute';
      setError(message);
      console.error('Distribution error:', err);
    } finally {
      setDistributing(false);
    }
  };

  return (
    <div className="space-y-4">
      <div className="rounded-2xl border border-[var(--color-card-border)]/60 bg-[rgba(8,12,28,0.85)] p-6">
        <div className="flex items-center gap-3 mb-2">
          <svg 
            className="w-7 h-7 text-[var(--accent)]" 
            viewBox="0 0 24 24" 
            fill="none" 
            stroke="currentColor" 
            strokeWidth="2"
          >
            <path d="M12 2v20M17 5H9.5a3.5 3.5 0 000 7h5a3.5 3.5 0 010 7H6" />
          </svg>
          <h3 className="text-xl font-semibold text-[var(--color-foreground)]">
            Distribute Revenue to NFT Holders
          </h3>
        </div>
        
        <p className="text-sm text-[var(--muted)] mb-6">
          Manually trigger a revenue distribution to all current NFT holders. 
          Enter the amount you earned and it will be split according to your configured revenue model.
        </p>
        
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-semibold text-[var(--color-foreground)] mb-2">
              Revenue Amount (SOL)
            </label>
            <input
              type="number"
              step="0.01"
              min="0"
              value={revenueAmount}
              onChange={(e) => setRevenueAmount(e.target.value)}
              placeholder="1.0"
              disabled={distributing}
              className="w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-4 py-3 text-sm text-[var(--color-foreground)] placeholder:text-[var(--muted)]/50 focus:border-[var(--accent)] focus:outline-none disabled:opacity-50"
            />
            <p className="mt-2 text-xs text-[var(--muted)]">
              Enter the revenue amount you want to distribute (in SOL or SOL-equivalent)
            </p>
          </div>
          
          <Button
            variant="primary"
            onClick={handleDistribute}
            disabled={!revenueAmount || distributing}
            className="w-full"
          >
            {distributing ? (
              <>
                <span className="inline-block animate-spin mr-2">⏳</span>
                Distributing...
              </>
            ) : (
              'Distribute to All Holders'
            )}
          </Button>
          
          {error && (
            <div className="rounded-lg border border-[var(--negative)]/60 bg-[rgba(239,68,68,0.08)] p-3">
              <p className="text-xs text-[var(--negative)]">
                ❌ Error: {error}
              </p>
            </div>
          )}
        </div>
      </div>
      
      {lastResult && (
        <div className="rounded-2xl border border-[var(--accent)]/60 bg-[rgba(34,211,238,0.08)] p-6">
          <h4 className="text-lg font-semibold text-[var(--color-foreground)] mb-3">
            ✅ Distribution Complete!
          </h4>
          
          <div className="grid gap-3 sm:grid-cols-2 mb-4">
            <div className="rounded-lg border border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.7)] p-3">
              <p className="text-[10px] uppercase tracking-[0.35em] text-[var(--muted)]">Recipients</p>
              <p className="text-2xl font-bold text-[var(--accent)] mt-1">
                {lastResult.recipients}
              </p>
              <p className="text-xs text-[var(--muted)]">NFT holders</p>
            </div>
            
            <div className="rounded-lg border border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.7)] p-3">
              <p className="text-[10px] uppercase tracking-[0.35em] text-[var(--muted)]">Per Holder</p>
              <p className="text-2xl font-bold text-[var(--accent)] mt-1">
                {lastResult.amountPerHolder?.toFixed(6)}
              </p>
              <p className="text-xs text-[var(--muted)]">SOL each</p>
            </div>
          </div>
          
          {lastResult.distributionTx && (
            <div className="rounded-lg border border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.7)] p-3">
              <p className="text-[10px] uppercase tracking-[0.35em] text-[var(--muted)] mb-2">
                Transaction Signature
              </p>
              <p className="text-xs text-[var(--accent)] font-mono break-all">
                {lastResult.distributionTx}
              </p>
              <a 
                href={`https://solscan.io/tx/${lastResult.distributionTx}${baseUrl.includes('devnet') ? '?cluster=devnet' : ''}`}
                target="_blank"
                rel="noopener noreferrer"
                className="text-xs text-[var(--accent)] hover:underline mt-2 inline-block"
              >
                View on Solscan →
              </a>
            </div>
          )}
        </div>
      )}
      
      <div className="rounded-xl border border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.5)] p-4">
        <p className="text-xs text-[var(--muted)] leading-relaxed">
          💡 <strong className="text-[var(--color-foreground)]">How to use:</strong> When you receive 
          revenue from streaming, rentals, API usage, or other sources, enter the amount here 
          and click distribute. All current NFT holders will receive their share automatically 
          within 30 seconds. You'll be notified when complete.
        </p>
        
        <div className="mt-3 pt-3 border-t border-[var(--color-card-border)]/30">
          <p className="text-xs text-[var(--muted)]">
            🔮 <strong className="text-[var(--color-foreground)]">Coming soon:</strong> Automatic 
            distributions via Spotify API, rental management integrations, and platform partnerships. 
            The webhook infrastructure is ready - we just need to connect your revenue sources!
          </p>
        </div>
      </div>
    </div>
  );
}

