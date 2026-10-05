/**
 * Treasury Activity Feed
 * 
 * Shows real-time income and distributions for an NFT treasury wallet
 * Provides complete transparency - investors can see money flowing in and out
 */

"use client";

import { useState, useEffect } from "react";

type TreasuryActivityFeedProps = {
  treasuryWallet: string;
  nftName: string;
};

type Transaction = {
  signature: string;
  timestamp: number;
  type: 'income' | 'distribution';
  amount: number;
  from?: string;
  to?: string;
  status: 'confirmed' | 'pending';
  description: string;
};

export function TreasuryActivityFeed({ treasuryWallet, nftName }: TreasuryActivityFeedProps) {
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [balance, setBalance] = useState<number>(0);
  const [loading, setLoading] = useState(true);
  const [totalIncome, setTotalIncome] = useState(0);
  const [totalDistributed, setTotalDistributed] = useState(0);

  useEffect(() => {
    loadTransactions();
    const interval = setInterval(loadTransactions, 10000); // Refresh every 10s
    return () => clearInterval(interval);
  }, [treasuryWallet]);

  const loadTransactions = async () => {
    try {
      // Skip if no wallet provided
      if (!treasuryWallet || treasuryWallet.length < 32) {
        console.log('Invalid treasury wallet, skipping load');
        setLoading(false);
        return;
      }

      // Dynamic import to avoid SSR issues
      const { Connection, PublicKey, LAMPORTS_PER_SOL } = await import('@solana/web3.js');
      
      const connection = new Connection('https://api.devnet.solana.com');
      const pubkey = new PublicKey(treasuryWallet);
      
      // Get current balance
      const balanceLamports = await connection.getBalance(pubkey);
      setBalance(balanceLamports / LAMPORTS_PER_SOL);
      
      // Get transaction signatures
      const signatures = await connection.getSignaturesForAddress(pubkey, { limit: 20 });
      
      // Parse transactions
      const txs: Transaction[] = [];
      let income = 0;
      let distributed = 0;
      
      for (const sig of signatures) {
        // Get transaction details
        const tx = await connection.getParsedTransaction(sig.signature, {
          maxSupportedTransactionVersion: 0
        });
        
        if (!tx || !tx.meta) continue;
        
        // Determine if this is income or distribution
        const postBalance = tx.meta.postBalances[0];
        const preBalance = tx.meta.preBalances[0];
        const change = (postBalance - preBalance) / LAMPORTS_PER_SOL;
        
        if (change > 0) {
          // Money came IN - this is income
          income += change;
          txs.push({
            signature: sig.signature,
            timestamp: (sig.blockTime || 0) * 1000,
            type: 'income',
            amount: change,
            status: 'confirmed',
            description: 'Revenue received'
          });
        } else if (change < 0) {
          // Money went OUT - this is distribution
          const amountOut = Math.abs(change);
          distributed += amountOut;
          txs.push({
            signature: sig.signature,
            timestamp: (sig.blockTime || 0) * 1000,
            type: 'distribution',
            amount: amountOut,
            status: 'confirmed',
            description: 'Distributed to holders'
          });
        }
      }
      
      setTransactions(txs);
      setTotalIncome(income);
      setTotalDistributed(distributed);
      setLoading(false);
      
    } catch (error) {
      console.error('Error loading transactions:', error);
      setLoading(false);
    }
  };

  const formatDate = (timestamp: number) => {
    return new Date(timestamp).toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-lg font-semibold text-[var(--color-foreground)]">
            Treasury Activity
          </h3>
          <p className="text-sm text-[var(--muted)]">
            Real-time income and distributions for {nftName}
          </p>
        </div>
        <button
          onClick={loadTransactions}
          className="text-xs text-[var(--accent)] hover:underline"
          disabled={loading}
        >
          {loading ? 'Refreshing...' : 'Refresh'}
        </button>
      </div>

      {/* Stats Grid */}
      <div className="grid gap-4 sm:grid-cols-3">
        <div className="rounded-xl border border-[var(--color-card-border)]/60 bg-[rgba(8,12,28,0.85)] p-4">
          <div className="flex items-center gap-2 mb-2">
            <svg className="w-5 h-5 text-[var(--accent)]" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <rect x="2" y="7" width="20" height="14" rx="2" ry="2" />
              <path d="M16 21V5a2 2 0 00-2-2h-4a2 2 0 00-2 2v16" />
            </svg>
            <span className="text-xs text-[var(--muted)]">Current Balance</span>
          </div>
          <div className="text-2xl font-bold text-[var(--accent)]">
            {balance.toFixed(4)} SOL
          </div>
        </div>

        <div className="rounded-xl border border-[var(--color-card-border)]/60 bg-[rgba(8,12,28,0.85)] p-4">
          <div className="flex items-center gap-2 mb-2">
            <svg className="w-5 h-5 text-green-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <line x1="12" y1="5" x2="12" y2="19" />
              <polyline points="19 12 12 19 5 12" />
            </svg>
            <span className="text-xs text-[var(--muted)]">Total Income</span>
          </div>
          <div className="text-2xl font-bold text-green-400">
            +{totalIncome.toFixed(4)} SOL
          </div>
        </div>

        <div className="rounded-xl border border-[var(--color-card-border)]/60 bg-[rgba(8,12,28,0.85)] p-4">
          <div className="flex items-center gap-2 mb-2">
            <svg className="w-5 h-5 text-orange-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <line x1="12" y1="5" x2="12" y2="19" />
              <polyline points="5 12 12 5 19 12" />
            </svg>
            <span className="text-xs text-[var(--muted)]">Total Distributed</span>
          </div>
          <div className="text-2xl font-bold text-orange-400">
            -{totalDistributed.toFixed(4)} SOL
          </div>
        </div>
      </div>

      {/* Treasury Wallet Address */}
      <div className="rounded-xl border border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.5)] p-4">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-xs text-[var(--muted)] mb-1">Treasury Wallet</p>
            <p className="text-sm text-[var(--accent)] font-mono">
              {treasuryWallet.slice(0, 12)}...{treasuryWallet.slice(-12)}
            </p>
          </div>
          <a
            href={`https://solscan.io/account/${treasuryWallet}?cluster=devnet`}
            target="_blank"
            rel="noopener noreferrer"
            className="text-xs text-[var(--accent)] hover:underline"
          >
            View on Solscan →
          </a>
        </div>
      </div>

      {/* Transaction Timeline */}
      <div className="rounded-2xl border border-[var(--color-card-border)]/60 bg-[rgba(8,12,28,0.85)] p-6">
        <h4 className="text-base font-semibold text-[var(--color-foreground)] mb-4">
          Activity Timeline
        </h4>

        {loading ? (
          <div className="text-center py-8 text-[var(--muted)]">
            <div className="animate-spin inline-block w-6 h-6 border-2 border-[var(--accent)] border-t-transparent rounded-full mb-2" />
            <p className="text-sm">Loading transactions...</p>
          </div>
        ) : transactions.length === 0 ? (
          <div className="text-center py-8">
            <svg className="w-12 h-12 mx-auto mb-3 text-[var(--muted)]" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <circle cx="12" cy="12" r="10" />
              <line x1="12" y1="8" x2="12" y2="12" />
              <line x1="12" y1="16" x2="12.01" y2="16" />
            </svg>
            <p className="text-sm text-[var(--muted)]">
              No transactions yet. Income will appear here when revenue arrives.
            </p>
          </div>
        ) : (
          <div className="space-y-3 max-h-[400px] overflow-y-auto">
            {transactions.map((tx, index) => (
              <div
                key={tx.signature}
                className={`
                  rounded-lg border p-4 transition-all
                  ${tx.type === 'income'
                    ? 'border-green-400/40 bg-green-400/5'
                    : 'border-orange-400/40 bg-orange-400/5'
                  }
                `}
              >
                <div className="flex items-start justify-between mb-2">
                  <div className="flex items-center gap-2">
                    {tx.type === 'income' ? (
                      <svg className="w-5 h-5 text-green-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                        <line x1="12" y1="5" x2="12" y2="19" />
                        <polyline points="19 12 12 19 5 12" />
                      </svg>
                    ) : (
                      <svg className="w-5 h-5 text-orange-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                        <line x1="12" y1="5" x2="12" y2="19" />
                        <polyline points="5 12 12 5 19 12" />
                      </svg>
                    )}
                    <div>
                      <p className="text-sm font-semibold text-[var(--color-foreground)]">
                        {tx.description}
                      </p>
                      <p className="text-xs text-[var(--muted)]">
                        {formatDate(tx.timestamp)}
                      </p>
                    </div>
                  </div>
                  <div className={`text-right ${tx.type === 'income' ? 'text-green-400' : 'text-orange-400'}`}>
                    <p className="text-lg font-bold">
                      {tx.type === 'income' ? '+' : '-'}{tx.amount.toFixed(4)}
                    </p>
                    <p className="text-xs">SOL</p>
                  </div>
                </div>
                
                <div className="mt-2 pt-2 border-t border-[var(--color-card-border)]/30">
                  <p className="text-xs text-[var(--muted)] font-mono break-all">
                    {tx.signature}
                  </p>
                  <a
                    href={`https://solscan.io/tx/${tx.signature}?cluster=devnet`}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-xs text-[var(--accent)] hover:underline mt-1 inline-block"
                  >
                    View on Solscan →
                  </a>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Info */}
      <div className="rounded-xl border border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.5)] p-4">
        <div className="flex items-start gap-2">
          <svg 
            className="w-4 h-4 text-[var(--accent)] mt-0.5 flex-shrink-0" 
            viewBox="0 0 24 24" 
            fill="none" 
            stroke="currentColor" 
            strokeWidth="2"
          >
            <circle cx="12" cy="12" r="10" />
            <line x1="12" y1="16" x2="12" y2="12" />
            <line x1="12" y1="8" x2="12.01" y2="8" />
          </svg>
          <p className="text-xs text-[var(--muted)] leading-relaxed">
            <strong className="text-[var(--color-foreground)]">Complete Transparency:</strong> This feed shows
            all transactions to and from the treasury wallet. <span className="text-green-400">Green arrows</span> are 
            revenue coming in (Spotify, rent, etc.). <span className="text-orange-400">Orange arrows</span> are 
            distributions going out to NFT holders. Every transaction is verifiable on Solscan.
          </p>
        </div>
      </div>
    </div>
  );
}

