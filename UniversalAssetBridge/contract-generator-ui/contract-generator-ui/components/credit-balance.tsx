'use client';

import { useState, useEffect } from 'react';
import { Sparkles, Plus } from 'lucide-react';
import { getCreditBalance, formatCredits } from '@/lib/credits-client';

interface CreditBalanceProps {
  walletAddress: string | null;
  onPurchaseClick: () => void;
  refreshTrigger?: number;
}

export function CreditBalance({ walletAddress, onPurchaseClick, refreshTrigger }: CreditBalanceProps) {
  const [credits, setCredits] = useState<number>(0);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (walletAddress) {
      setLoading(true);
      getCreditBalance(walletAddress)
        .then(setCredits)
        .finally(() => setLoading(false));
    } else {
      setCredits(0);
    }
  }, [walletAddress, refreshTrigger]);

  if (!walletAddress) {
    return null;
  }

  return (
    <div className="inline-flex items-center gap-3 bg-gradient-to-r from-purple-600/10 to-pink-600/10 border border-purple-500/30 rounded-lg px-4 py-2">
      <div className="flex items-center gap-2">
        <Sparkles className="w-4 h-4 text-purple-600 dark:text-purple-400" />
        <div>
          <div className="text-xs text-gray-600 dark:text-gray-400">Credits</div>
          <div className="font-bold text-purple-600 dark:text-purple-400">
            {loading ? '...' : formatCredits(credits)}
          </div>
        </div>
      </div>
      
      <button
        onClick={onPurchaseClick}
        className="bg-purple-600 hover:bg-purple-700 text-white rounded-md px-3 py-1 text-sm font-semibold flex items-center gap-1 transition-colors"
      >
        <Plus className="w-4 h-4" />
        Buy
      </button>
    </div>
  );
}

