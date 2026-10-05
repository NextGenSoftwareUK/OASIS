import type { Wallet, Transaction } from '../types';
import type { PrivacyMetrics } from '../api/privacyApi';

export interface PrivacyScoreFactors {
  shieldedBalanceRatio: number; // 0-1
  shieldedTransactionRatio: number; // 0-1
  viewingKeysActive: number;
  privacyLevel: 'low' | 'medium' | 'high' | 'maximum';
  recentPrivacyActivity: number; // 0-1
}

/**
 * Calculate privacy score based on various factors
 * @param factors Privacy score factors
 * @returns Privacy score (0-100)
 */
export function calculatePrivacyScore(factors: PrivacyScoreFactors): number {
  let score = 0;

  // Shielded balance ratio (40% weight)
  score += factors.shieldedBalanceRatio * 40;

  // Shielded transaction ratio (30% weight)
  score += factors.shieldedTransactionRatio * 30;

  // Privacy level (20% weight)
  const privacyLevelScore = {
    low: 0.25,
    medium: 0.5,
    high: 0.75,
    maximum: 1.0,
  }[factors.privacyLevel];
  score += privacyLevelScore * 20;

  // Recent privacy activity (10% weight)
  score += factors.recentPrivacyActivity * 10;

  // Bonus for viewing keys (up to 5 points)
  if (factors.viewingKeysActive > 0) {
    score += Math.min(factors.viewingKeysActive * 0.5, 5);
  }

  return Math.min(Math.round(score), 100);
}

/**
 * Calculate privacy metrics from wallets and transactions
 */
export function calculatePrivacyMetrics(
  wallets: Wallet[],
  transactions: Transaction[],
  viewingKeysCount: number = 0
): PrivacyMetrics {
  // Calculate balances
  let totalBalance = 0;
  let shieldedBalance = 0;

  wallets.forEach((wallet) => {
    totalBalance += wallet.balance || 0;
    // For now, assume Zcash wallets are shielded
    // This will be enhanced when backend provides shielded balance info
    if (wallet.providerType === 'ZcashOASIS') {
      shieldedBalance += wallet.balance || 0;
    }
  });

  // Calculate transaction ratios
  const totalTxs = transactions.length;
  const shieldedTxs = transactions.filter(
    (tx) => tx.fromProviderType === 'ZcashOASIS' || tx.toProviderType === 'ZcashOASIS'
  ).length;

  const shieldedBalanceRatio = totalBalance > 0 ? shieldedBalance / totalBalance : 0;
  const shieldedTransactionRatio = totalTxs > 0 ? shieldedTxs / totalTxs : 0;

  // Determine privacy level
  let privacyLevel: 'low' | 'medium' | 'high' | 'maximum' = 'low';
  if (shieldedBalanceRatio >= 0.8 && shieldedTransactionRatio >= 0.8) {
    privacyLevel = 'maximum';
  } else if (shieldedBalanceRatio >= 0.6 && shieldedTransactionRatio >= 0.6) {
    privacyLevel = 'high';
  } else if (shieldedBalanceRatio >= 0.3 || shieldedTransactionRatio >= 0.3) {
    privacyLevel = 'medium';
  }

  // Calculate recent privacy activity (last 30 days)
  const thirtyDaysAgo = new Date();
  thirtyDaysAgo.setDate(thirtyDaysAgo.getDate() - 30);
  const recentShieldedTxs = transactions.filter(
    (tx) =>
      (tx.fromProviderType === 'ZcashOASIS' || tx.toProviderType === 'ZcashOASIS') &&
      new Date(tx.transactionDate) >= thirtyDaysAgo
  ).length;

  const recentPrivacyActivity = Math.min(recentShieldedTxs / 10, 1); // Normalize to 0-1

  // Calculate privacy score
  const privacyScore = calculatePrivacyScore({
    shieldedBalanceRatio,
    shieldedTransactionRatio,
    viewingKeysActive: viewingKeysCount,
    privacyLevel,
    recentPrivacyActivity,
  });

  return {
    shieldedBalance,
    transparentBalance: totalBalance - shieldedBalance,
    privacyScore,
    recentShieldedTxs,
    viewingKeysActive: viewingKeysCount,
    privacyLevel,
  };
}

/**
 * Get privacy recommendations based on metrics
 */
export function getPrivacyRecommendations(metrics: PrivacyMetrics): string[] {
  const recommendations: string[] = [];

  if (metrics.shieldedBalance < metrics.transparentBalance) {
    recommendations.push('Consider moving more funds to shielded addresses for enhanced privacy');
  }

  if (metrics.privacyScore < 50) {
    recommendations.push('Your privacy score is low. Enable privacy mode for better protection');
  }

  if (metrics.viewingKeysActive === 0 && metrics.recentShieldedTxs > 0) {
    recommendations.push('Generate viewing keys for auditability while maintaining privacy');
  }

  if (metrics.privacyLevel === 'low') {
    recommendations.push('Switch to shielded transactions for better privacy');
  }

  if (metrics.recentShieldedTxs === 0) {
    recommendations.push('Start using shielded transactions to improve your privacy');
  }

  return recommendations;
}

