'use client';

import React from 'react';
import { ArrowLeft, Clock } from 'lucide-react';
import { useWalletStore } from '@/lib/store';
import { Card } from '@/components/ui/card';
import { formatBalance } from '@/lib/utils';

interface HistoryScreenProps {
  onBack: () => void;
}

export const HistoryScreen: React.FC<HistoryScreenProps> = ({ onBack }) => {
  const { transactions, wallets } = useWalletStore();

  const getWalletLabel = (address: string) => {
    const wallet = Object.values(wallets).flat().find(w => w.walletAddress === address);
    return wallet ? wallet.providerType.replace('OASIS', '') : 'External';
  };

  return (
    <div className="min-h-screen bg-black text-white flex flex-col">
      <header className="px-4 pt-12 pb-4 border-b border-gray-800 flex items-center space-x-4">
        <button onClick={onBack} className="p-2">
          <ArrowLeft className="w-6 h-6" />
        </button>
        <div>
          <p className="text-xs text-gray-400 uppercase tracking-wider">Wallet Activity</p>
          <h1 className="text-xl font-semibold">History</h1>
        </div>
      </header>

      <main className="flex-1 px-4 py-6 space-y-3 overflow-y-auto">
        {transactions.length === 0 ? (
          <div className="text-center py-12 text-gray-400">
            <Clock className="w-10 h-10 mx-auto mb-4" />
            <p className="text-lg font-semibold mb-2">No transactions yet</p>
            <p className="text-sm">Send your first transaction to populate history.</p>
          </div>
        ) : (
          transactions.map(tx => (
            <Card key={tx.transactionId} className="bg-gray-900 border-gray-800 p-4 space-y-2">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-gray-400">From</p>
                  <p className="font-semibold">{getWalletLabel(tx.fromWalletAddress)}</p>
                  <p className="text-xs text-gray-500">{tx.fromWalletAddress}</p>
                </div>
                <div className="text-right">
                  <p className="text-sm text-gray-400">To</p>
                  <p className="font-semibold">{formatBalance(tx.amount)} {tx.tokenSymbol || tx.fromProviderType.replace('OASIS','')}</p>
                  <p className="text-xs text-gray-500">{tx.toWalletAddress}</p>
                </div>
              </div>
              <div className="flex items-center justify-between text-xs text-gray-500">
                <span>{new Date(tx.transactionDate || Date.now()).toLocaleString()}</span>
                <span className={tx.isSuccessful ? 'text-green-400' : 'text-red-400'}>
                  {tx.isSuccessful ? 'Success' : 'Failed'}
                </span>
              </div>
            </Card>
          ))
        )}
      </main>
    </div>
  );
};

