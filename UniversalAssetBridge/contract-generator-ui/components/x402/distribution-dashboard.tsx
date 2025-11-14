/**
 * x402 Distribution Dashboard
 * 
 * Shows real-time distribution statistics and payment history for x402-enabled NFTs
 */

"use client";

import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { useX402Distribution } from "@/hooks/use-x402-distribution";
import type { X402Stats, X402Distribution } from "@/types/x402";

type DistributionDashboardProps = {
  nftMintAddress: string;
  baseUrl: string;
  token?: string;
};

export function DistributionDashboard({ nftMintAddress, baseUrl, token }: DistributionDashboardProps) {
  const [stats, setStats] = useState<X402Stats | null>(null);
  const [history, setHistory] = useState<X402Distribution[]>([]);
  const [testAmount, setTestAmount] = useState(0.1);
  const { getStats, getHistory, testDistribution, loading, error } = useX402Distribution(baseUrl, token);

  const loadData = async () => {
    const [statsData, historyData] = await Promise.all([
      getStats(nftMintAddress),
      getHistory(nftMintAddress, 10)
    ]);
    
    if (statsData) setStats(statsData);
    if (historyData) setHistory(historyData);
  };

  useEffect(() => {
    if (nftMintAddress) {
      loadData();
    }
  }, [nftMintAddress]);

  const handleTestDistribution = async () => {
    const result = await testDistribution(nftMintAddress, testAmount);
    if (result) {
      // Reload data after successful test
      await loadData();
    }
  };

  if (loading && !stats) {
    return (
      <div className="flex items-center justify-center p-12">
        <div className="text-[var(--muted)]">Loading distribution data...</div>
      </div>
    );
  }

  if (error && !stats) {
    return (
      <div className="rounded-2xl border border-[var(--negative)]/60 bg-[rgba(239,68,68,0.08)] p-6">
        <p className="text-[var(--negative)]">Error loading distribution data: {error}</p>
        <Button variant="secondary" onClick={loadData} className="mt-4">
          Retry
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Stats Grid */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard
          label="Total Distributed"
          value={`${stats?.totalDistributed.toFixed(4) || '0'} SOL`}
          icon="💰"
        />
        <StatCard
          label="Distributions"
          value={stats?.distributionCount.toString() || '0'}
          icon="📊"
        />
        <StatCard
          label="Current Holders"
          value={stats?.holderCount.toString() || '0'}
          icon="👥"
        />
        <StatCard
          label="Avg per Distribution"
          value={`${stats?.averagePerDistribution.toFixed(4) || '0'} SOL`}
          icon="📈"
        />
      </div>

      {/* Test Distribution */}
      <div className="rounded-2xl border border-[var(--color-card-border)]/60 bg-[rgba(8,12,28,0.85)] p-6">
        <h3 className="text-lg font-semibold text-[var(--color-foreground)] mb-4">
          🧪 Test Payment Distribution
        </h3>
        <p className="text-sm text-[var(--muted)] mb-4">
          Simulate an x402 payment to test distribution to all current NFT holders
        </p>
        <div className="flex items-center gap-4">
          <input
            type="number"
            step="0.01"
            min="0.01"
            value={testAmount}
            onChange={(e) => setTestAmount(Number(e.target.value))}
            className="flex-1 rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-4 py-3 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none"
            placeholder="Amount in SOL"
          />
          <Button
            variant="primary"
            onClick={handleTestDistribution}
            disabled={loading || testAmount <= 0}
          >
            {loading ? 'Distributing...' : 'Test Distribution'}
          </Button>
        </div>
        {stats && stats.holderCount > 0 && (
          <p className="mt-3 text-xs text-[var(--muted)]">
            Each of {stats.holderCount} holders will receive approximately{' '}
            <span className="text-[var(--accent)] font-semibold">
              {(testAmount / stats.holderCount).toFixed(6)} SOL
            </span>
          </p>
        )}
      </div>

      {/* Distribution History */}
      <div className="rounded-2xl border border-[var(--color-card-border)]/60 bg-[rgba(8,12,28,0.85)] p-6">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-semibold text-[var(--color-foreground)]">
            📜 Distribution History
          </h3>
          <Button variant="secondary" onClick={loadData} disabled={loading} className="text-xs">
            Refresh
          </Button>
        </div>

        {history.length === 0 ? (
          <div className="text-center py-8 text-[var(--muted)]">
            <p>No distributions yet</p>
            <p className="text-xs mt-2">Run a test distribution to see it here</p>
          </div>
        ) : (
          <div className="space-y-3">
            {history.map((dist, index) => (
              <DistributionRow key={index} distribution={dist} />
            ))}
          </div>
        )}
      </div>

      {/* NFT Info */}
      <div className="rounded-xl border border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.5)] p-4">
        <p className="text-xs text-[var(--muted)]">
          <strong className="text-[var(--color-foreground)]">NFT Mint Address:</strong>{' '}
          <code className="text-[var(--accent)]">{nftMintAddress}</code>
        </p>
      </div>
    </div>
  );
}

function StatCard({ label, value, icon }: { label: string; value: string; icon: string }) {
  return (
    <div className="rounded-2xl border border-[var(--color-card-border)]/60 bg-[rgba(8,12,28,0.85)] p-6 text-center">
      <div className="text-3xl mb-2">{icon}</div>
      <div className="text-2xl font-bold text-[var(--accent)] mb-1">{value}</div>
      <div className="text-xs text-[var(--muted)]">{label}</div>
    </div>
  );
}

function DistributionRow({ distribution }: { distribution: X402Distribution }) {
  const date = new Date(distribution.timestamp);
  const formattedDate = date.toLocaleString('en-US', {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });

  return (
    <div className="flex items-center justify-between rounded-xl border border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.7)] px-4 py-3">
      <div className="flex-1">
        <div className="flex items-center gap-3">
          <span className="text-lg">💸</span>
          <div>
            <p className="text-sm font-semibold text-[var(--color-foreground)]">
              {distribution.amount.toFixed(4)} SOL
            </p>
            <p className="text-xs text-[var(--muted)]">
              Distributed to {distribution.recipients} holders • {formattedDate}
            </p>
          </div>
        </div>
      </div>
      <div className="text-right">
        <p className="text-xs text-[var(--accent)] font-semibold">
          {distribution.amountPerHolder.toFixed(6)} SOL
        </p>
        <p className="text-[10px] text-[var(--muted)]">per holder</p>
      </div>
    </div>
  );
}

