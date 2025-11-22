'use client';

import React, { useState } from 'react';
import { ArrowLeft, Search, Bookmark, ChevronRight } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { ProviderType } from '@/lib/types';
import { formatAddress, cn } from '@/lib/utils';

interface BuyScreenProps {
  providerType: ProviderType;
  walletAddress: string;
  onBack: () => void;
  onBuy: (amount: number) => void;
}

export const BuyScreen: React.FC<BuyScreenProps> = ({
  providerType,
  walletAddress,
  onBack,
  onBuy,
}) => {
  const [usdAmount, setUsdAmount] = useState('');
  const [selectedAmount, setSelectedAmount] = useState<number | null>(null);

  const providerName = providerType.replace('OASIS', '');
  const tokenSymbol = providerName === 'Solana' ? 'SOL' : providerName === 'Ethereum' ? 'ETH' : providerName;
  
  // Mock conversion rate
  const solPrice = 100; // $100 per SOL
  const estimatedSol = usdAmount ? (parseFloat(usdAmount) / solPrice).toFixed(6) : '0';

  const handleQuickAmount = (amount: number) => {
    setUsdAmount(amount.toString());
    setSelectedAmount(amount);
  };

  const handleBuy = () => {
    if (usdAmount && parseFloat(usdAmount) > 0) {
      onBuy(parseFloat(usdAmount));
    }
  };

  const isValid = usdAmount && parseFloat(usdAmount) >= 25;

  return (
    <div className="min-h-screen bg-black text-white flex flex-col">
      {/* Header */}
      <header className="px-4 pt-12 pb-4 border-b border-gray-800">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center space-x-3">
            <div className="w-10 h-10 rounded-full bg-gradient-to-br from-green-400 to-orange-500 flex items-center justify-center">
              <span className="text-lg">👤</span>
            </div>
            <div>
              <div className="flex items-center space-x-2">
                <span className="font-semibold text-sm">@jocktalk</span>
              </div>
              <div className="text-xs text-gray-400">yNFT Investor</div>
            </div>
          </div>
          <div className="flex items-center space-x-3">
            <Search className="w-5 h-5 text-gray-400" />
            <Bookmark className="w-5 h-5 text-gray-400" />
          </div>
        </div>
        <div className="flex items-center space-x-4">
          <button onClick={onBack} className="p-2">
            <ArrowLeft className="w-6 h-6" />
          </button>
          <h1 className="text-xl font-semibold">Buy {tokenSymbol}</h1>
        </div>
      </header>

      {/* Content */}
      <div className="flex-1 px-4 py-8">
        {/* Provider Logo */}
        <div className="flex justify-center mb-8">
          <div className="w-24 h-24 rounded-full bg-gradient-to-br from-green-400 via-purple-500 to-pink-500 flex items-center justify-center">
            {providerName === 'Solana' ? (
              <div className="flex flex-col items-center">
                <div className="w-8 h-2 bg-white rounded mb-1"></div>
                <div className="w-8 h-2 bg-white rounded mb-1"></div>
                <div className="w-8 h-2 bg-white rounded"></div>
              </div>
            ) : (
              <span className="text-3xl font-bold text-white">Ξ</span>
            )}
          </div>
        </div>

        {/* Wallet Address */}
        <div className="mb-6 p-4 bg-gray-900 rounded-lg border border-gray-800">
          <div className="text-sm text-gray-400 mb-1">yNFT Investor</div>
          <div className="flex items-center justify-between">
            <span className="text-sm font-mono">{formatAddress(walletAddress, 8)}</span>
          </div>
        </div>

        {/* Amount Input */}
        <div className="mb-6">
          <div className="relative">
            <Input
              type="number"
              value={usdAmount}
              onChange={(e) => {
                setUsdAmount(e.target.value);
                setSelectedAmount(null);
              }}
              placeholder="0"
              className="bg-gray-900 border-gray-800 text-white text-3xl placeholder:text-gray-600 pr-16"
            />
            <span className="absolute right-4 top-1/2 -translate-y-1/2 text-gray-400">USD</span>
          </div>
          <div className="mt-2 flex items-center space-x-2 text-sm text-gray-400">
            <span>~{estimatedSol} {tokenSymbol}</span>
            <span className="text-xs">↓↑</span>
          </div>
        </div>

        {/* Quick Amount Buttons */}
        <div className="mb-6">
          <div className="grid grid-cols-3 gap-3">
            {[25, 50, 100].map((amount) => (
              <button
                key={amount}
                onClick={() => handleQuickAmount(amount)}
                className={cn(
                  "py-3 px-4 rounded-lg border transition-colors",
                  selectedAmount === amount
                    ? "bg-purple-600 border-purple-500 text-white"
                    : "bg-gray-900 border-gray-800 text-white hover:bg-gray-800"
                )}
              >
                ${amount}
              </button>
            ))}
          </div>
        </div>

        {/* Payment Method */}
        <div className="mb-6 p-4 bg-gray-900 rounded-lg border border-gray-800">
          <div className="flex items-center justify-between">
            <div>
              <div className="text-sm font-semibold mb-1">Debit/Credit Card</div>
              <div className="text-xs text-gray-400">Secure payment processing</div>
            </div>
            <div className="flex items-center space-x-2">
              <span className="text-sm font-semibold">MoonPay</span>
              <ChevronRight className="w-5 h-5 text-gray-400" />
            </div>
          </div>
        </div>

        {/* Info */}
        <div className="mb-6 p-4 bg-blue-500/10 border border-blue-500/20 rounded-lg">
          <div className="text-sm text-blue-400">
            <p className="mb-2">• Minimum purchase: $25</p>
            <p className="mb-2">• Payment processed by MoonPay</p>
            <p>• Tokens will be sent to your wallet address</p>
          </div>
        </div>
      </div>

      {/* Bottom Actions */}
      <div className="px-4 pb-8 border-t border-gray-800 pt-4">
        <div className="flex space-x-4">
          <Button
            variant="outline"
            onClick={onBack}
            className="flex-1 bg-gray-900 border-gray-800 text-white hover:bg-gray-800"
          >
            Cancel
          </Button>
          <Button
            onClick={handleBuy}
            disabled={!isValid}
            className={cn(
              "flex-1 bg-purple-600 text-white hover:bg-purple-700",
              !isValid && "opacity-50 cursor-not-allowed"
            )}
          >
            Buy
          </Button>
        </div>
      </div>
    </div>
  );
};

