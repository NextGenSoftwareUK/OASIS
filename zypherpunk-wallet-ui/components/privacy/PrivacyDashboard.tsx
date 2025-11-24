'use client';

import React, { useEffect, useState } from 'react';
import { Shield, Eye, Lock, TrendingUp, AlertCircle } from 'lucide-react';
import { Card } from '@/components/ui/card';
import { PrivacyIndicator, PrivacyBadge } from './PrivacyIndicator';
import { SecurityFeaturesDisplay } from '@/components/security/SecurityFeaturesDisplay';
import { privacyAPI, type PrivacyMetrics } from '@/lib/api/privacyApi';
import { calculatePrivacyMetrics, getPrivacyRecommendations } from '@/lib/privacy/privacyScore';
import { useWalletStore } from '@/lib/store';
import { useAvatarStore } from '@/lib/avatarStore';

export const PrivacyDashboard: React.FC = () => {
  const { wallets, transactions } = useWalletStore();
  const { avatar } = useAvatarStore();
  const [metrics, setMetrics] = useState<PrivacyMetrics | null>(null);
  const [recommendations, setRecommendations] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const loadPrivacyMetrics = async () => {
      if (!avatar?.id) return;

      setIsLoading(true);
      try {
        // Try to get metrics from API first
        const apiResult = await privacyAPI.getPrivacyMetrics(avatar.id);
        
        if (!apiResult.isError && apiResult.result) {
          setMetrics(apiResult.result);
          setRecommendations(getPrivacyRecommendations(apiResult.result));
        } else {
          // Fallback to calculating from local data
          const allWallets = Object.values(wallets).flat();
          const calculated = calculatePrivacyMetrics(allWallets, transactions, 0);
          setMetrics(calculated);
          setRecommendations(getPrivacyRecommendations(calculated));
        }
      } catch (error) {
        console.error('Failed to load privacy metrics:', error);
        // Fallback to local calculation
        const allWallets = Object.values(wallets).flat();
        const calculated = calculatePrivacyMetrics(allWallets, transactions, 0);
        setMetrics(calculated);
        setRecommendations(getPrivacyRecommendations(calculated));
      } finally {
        setIsLoading(false);
      }
    };

    loadPrivacyMetrics();
  }, [avatar?.id, wallets, transactions]);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center p-8">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-zypherpunk-primary"></div>
      </div>
    );
  }

  if (!metrics) {
    return (
      <div className="text-center p-8 text-zypherpunk-text-muted">
        <AlertCircle className="w-12 h-12 mx-auto mb-4 text-zypherpunk-warning" />
        <p>Unable to load privacy metrics</p>
      </div>
    );
  }

  const totalBalance = metrics.shieldedBalance + metrics.transparentBalance;
  const shieldedPercentage = totalBalance > 0 
    ? (metrics.shieldedBalance / totalBalance) * 100 
    : 0;

  return (
    <div className="space-y-6 p-4">
      {/* Privacy Score Card */}
      <Card className="bg-zypherpunk-surface border-zypherpunk-border p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-2xl font-bold text-zypherpunk-text">Privacy Score</h2>
          <PrivacyBadge level={metrics.privacyLevel} />
        </div>
        
        <div className="flex items-center gap-4">
          <div className="relative w-32 h-32">
            <svg className="w-32 h-32 transform -rotate-90">
              <circle
                cx="64"
                cy="64"
                r="56"
                stroke="currentColor"
                strokeWidth="8"
                fill="none"
                className="text-zypherpunk-border"
              />
              <circle
                cx="64"
                cy="64"
                r="56"
                stroke="currentColor"
                strokeWidth="8"
                fill="none"
                strokeDasharray={`${2 * Math.PI * 56}`}
                strokeDashoffset={`${2 * Math.PI * 56 * (1 - metrics.privacyScore / 100)}`}
                className="text-zypherpunk-primary transition-all duration-500"
                strokeLinecap="round"
              />
            </svg>
            <div className="absolute inset-0 flex items-center justify-center">
              <span className="text-3xl font-bold text-zypherpunk-primary">
                {metrics.privacyScore}
              </span>
            </div>
          </div>
          
          <div className="flex-1">
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <span className="text-zypherpunk-text-muted">Shielded Balance</span>
                <span className="text-zypherpunk-shielded font-semibold">
                  {shieldedPercentage.toFixed(1)}%
                </span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-zypherpunk-text-muted">Shielded Transactions</span>
                <span className="text-zypherpunk-shielded font-semibold">
                  {metrics.recentShieldedTxs}
                </span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-zypherpunk-text-muted">Viewing Keys Active</span>
                <span className="text-zypherpunk-accent font-semibold">
                  {metrics.viewingKeysActive}
                </span>
              </div>
            </div>
          </div>
        </div>
      </Card>

      {/* Balance Breakdown */}
      <Card className="bg-zypherpunk-surface border-zypherpunk-border p-6">
        <h3 className="text-xl font-semibold text-zypherpunk-text mb-4">Balance Breakdown</h3>
        
        <div className="space-y-4">
          <div>
            <div className="flex items-center justify-between mb-2">
              <div className="flex items-center gap-2">
                <Shield className="w-4 h-4 text-zypherpunk-shielded" />
                <span className="text-zypherpunk-text">Shielded</span>
              </div>
              <span className="text-zypherpunk-shielded font-semibold">
                {metrics.shieldedBalance.toFixed(4)} ZEC
              </span>
            </div>
            <div className="w-full bg-zypherpunk-border rounded-full h-2">
              <div
                className="bg-zypherpunk-shielded h-2 rounded-full transition-all duration-500"
                style={{ width: `${shieldedPercentage}%` }}
              />
            </div>
          </div>
          
          <div>
            <div className="flex items-center justify-between mb-2">
              <div className="flex items-center gap-2">
                <Lock className="w-4 h-4 text-zypherpunk-transparent" />
                <span className="text-zypherpunk-text">Transparent</span>
              </div>
              <span className="text-zypherpunk-transparent font-semibold">
                {metrics.transparentBalance.toFixed(4)} ZEC
              </span>
            </div>
            <div className="w-full bg-zypherpunk-border rounded-full h-2">
              <div
                className="bg-zypherpunk-transparent h-2 rounded-full transition-all duration-500"
                style={{ width: `${100 - shieldedPercentage}%` }}
              />
            </div>
          </div>
        </div>
      </Card>

      {/* Privacy Recommendations */}
      {recommendations.length > 0 && (
        <Card className="bg-zypherpunk-surface border-zypherpunk-border p-6">
          <div className="flex items-center gap-2 mb-4">
            <TrendingUp className="w-5 h-5 text-zypherpunk-accent" />
            <h3 className="text-xl font-semibold text-zypherpunk-text">Recommendations</h3>
          </div>
          
          <ul className="space-y-2">
            {recommendations.map((rec, index) => (
              <li key={index} className="flex items-start gap-2 text-zypherpunk-text-muted">
                <span className="text-zypherpunk-accent mt-1">•</span>
                <span>{rec}</span>
              </li>
            ))}
          </ul>
        </Card>
      )}

      {/* Privacy Stats */}
      <div className="grid grid-cols-2 gap-4">
        <Card className="bg-zypherpunk-surface border-zypherpunk-border p-4">
          <div className="flex items-center gap-2 mb-2">
            <Shield className="w-4 h-4 text-zypherpunk-shielded" />
            <span className="text-sm text-zypherpunk-text-muted">Shielded Txs</span>
          </div>
          <p className="text-2xl font-bold text-zypherpunk-shielded">
            {metrics.recentShieldedTxs}
          </p>
        </Card>
        
        <Card className="bg-zypherpunk-surface border-zypherpunk-border p-4">
          <div className="flex items-center gap-2 mb-2">
            <Eye className="w-4 h-4 text-zypherpunk-accent" />
            <span className="text-sm text-zypherpunk-text-muted">Viewing Keys</span>
          </div>
          <p className="text-2xl font-bold text-zypherpunk-accent">
            {metrics.viewingKeysActive}
          </p>
        </Card>
      </div>

      {/* Security Features Link */}
      <Card className="bg-zypherpunk-surface border-zypherpunk-border p-6">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-lg font-semibold text-zypherpunk-text mb-1 flex items-center space-x-2">
              <Shield className="w-5 h-5 text-zypherpunk-primary" />
              <span>Security Features</span>
            </h3>
            <p className="text-sm text-zypherpunk-text-muted">
              View OASIS Wallet API security status
            </p>
          </div>
          <button
            onClick={() => window.location.href = '/security'}
            className="px-4 py-2 bg-zypherpunk-primary/20 hover:bg-zypherpunk-primary/30 border border-zypherpunk-primary/40 text-zypherpunk-primary font-semibold rounded-lg transition-colors"
          >
            View Security
          </button>
        </div>
      </Card>
    </div>
  );
};

