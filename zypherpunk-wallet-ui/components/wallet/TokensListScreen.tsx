'use client';

import React, { useState } from 'react';
import { ArrowLeft, Search, Menu, ChevronDown, Home, ArrowLeftRight, Clock } from 'lucide-react';
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';

interface Token {
  rank: number;
  name: string;
  symbol: string;
  avatar: string;
  marketCap: string;
  price: number;
  change24h: number;
  provider: 'Solana' | 'Ethereum';
}

// Mock token data
const mockTokens: Token[] = [
  {
    rank: 1,
    name: 'wifmas',
    symbol: 'WIFMAS',
    avatar: '🐕',
    marketCap: '$705K MC',
    price: 0.0007005,
    change24h: 249.58,
    provider: 'Solana',
  },
  {
    rank: 2,
    name: 'Rizzmas',
    symbol: 'RIZZMAS',
    avatar: '🎅',
    marketCap: '$6.1M MC',
    price: 0.00001239,
    change24h: 54.24,
    provider: 'Solana',
  },
  {
    rank: 3,
    name: 'SPACECAT',
    symbol: 'SPACECAT',
    avatar: '🐱',
    marketCap: '$453K MC',
    price: 0.00044598,
    change24h: 30.66,
    provider: 'Solana',
  },
  {
    rank: 4,
    name: 'CASH',
    symbol: 'CASH',
    avatar: '$',
    marketCap: '$96M MC',
    price: 1.0,
    change24h: -0.01,
    provider: 'Solana',
  },
  {
    rank: 5,
    name: 'USCR',
    symbol: 'USCR',
    avatar: '🦅',
    marketCap: '$7.9M MC',
    price: 0.00787309,
    change24h: -38.96,
    provider: 'Solana',
  },
];

interface TokensListScreenProps {
  onBack: () => void;
  onTokenSelect?: (token: Token) => void;
}

export const TokensListScreen: React.FC<TokensListScreenProps> = ({
  onBack,
  onTokenSelect,
}) => {
  const [selectedRank, setSelectedRank] = useState('Rank');
  const [selectedProvider, setSelectedProvider] = useState('Solana');
  const [selectedTimeframe, setSelectedTimeframe] = useState('24h');

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
                <div className="w-4 h-4 border border-gray-400 rounded"></div>
              </div>
              <div className="text-xs text-gray-400">yNFT Investor</div>
            </div>
          </div>
          <div className="flex items-center space-x-3">
            <Search className="w-5 h-5 text-gray-400" />
            <Menu className="w-5 h-5 text-gray-400" />
          </div>
        </div>
        <div className="flex items-center space-x-4">
          <button onClick={onBack} className="p-2">
            <ArrowLeft className="w-6 h-6" />
          </button>
          <h1 className="text-xl font-semibold">Tokens</h1>
        </div>
      </header>

      {/* Filters */}
      <div className="px-4 py-4 border-b border-gray-800">
        <div className="flex items-center space-x-3 mb-3">
          <button
            onClick={() => setSelectedRank(selectedRank === 'Rank' ? '' : 'Rank')}
            className="flex items-center space-x-1 px-3 py-2 bg-gray-900 rounded-lg border border-gray-800 text-sm"
          >
            <span>{selectedRank}</span>
            <ChevronDown className="w-4 h-4" />
          </button>
          <button
            onClick={() => setSelectedProvider(selectedProvider === 'Solana' ? 'Ethereum' : 'Solana')}
            className="flex items-center space-x-1 px-3 py-2 bg-gray-900 rounded-lg border border-gray-800 text-sm"
          >
            <span>{selectedProvider}</span>
            <ChevronDown className="w-4 h-4" />
          </button>
          <button
            onClick={() => setSelectedTimeframe(selectedTimeframe === '24h' ? '7d' : '24h')}
            className="flex items-center space-x-1 px-3 py-2 bg-gray-900 rounded-lg border border-gray-800 text-sm"
          >
            <span>{selectedTimeframe}</span>
            <ChevronDown className="w-4 h-4" />
          </button>
        </div>
        <div className="flex items-center justify-between text-xs text-gray-400 px-1">
          <span>#</span>
          <span className="flex-1 ml-4">Token</span>
          <span>Price</span>
        </div>
      </div>

      {/* Token List */}
      <div className="flex-1 overflow-y-auto">
        {mockTokens.map((token) => (
          <button
            key={token.rank}
            onClick={() => onTokenSelect?.(token)}
            className="w-full px-4 py-4 border-b border-gray-800 hover:bg-gray-900 transition-colors"
          >
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-3 flex-1">
                <div className="w-10 h-10 rounded-full bg-gray-800 flex items-center justify-center relative">
                  <span className="text-xl">{token.avatar}</span>
                  <div className="absolute -bottom-1 -right-1 w-4 h-4 bg-gradient-to-br from-green-400 via-purple-500 to-pink-500 rounded-full border-2 border-black"></div>
                </div>
                <div className="flex-1">
                  <div className="flex items-center space-x-2">
                    <span className="font-semibold">{token.name}</span>
                    <span className="text-xs text-gray-400">{token.marketCap}</span>
                  </div>
                  <div className="text-xs text-gray-400 mt-0.5">
                    ${token.price.toFixed(8)}
                  </div>
                </div>
              </div>
              <div className="text-right">
                <div
                  className={cn(
                    "text-sm font-semibold",
                    token.change24h >= 0 ? "text-green-400" : "text-red-400"
                  )}
                >
                  {token.change24h >= 0 ? '+' : ''}
                  {token.change24h.toFixed(2)}%
                </div>
                <div className="text-xs text-gray-400 mt-0.5">—</div>
              </div>
            </div>
          </button>
        ))}
      </div>

      {/* Bottom Navigation */}
      <div className="border-t border-gray-800 bg-black">
        <div className="flex items-center justify-around py-3">
          <button className="flex flex-col items-center space-y-1">
            <Home className="w-6 h-6 text-gray-400" />
            <span className="text-xs text-gray-400">Home</span>
          </button>
          <button className="flex flex-col items-center space-y-1">
            <ArrowLeftRight className="w-6 h-6 text-gray-400" />
            <span className="text-xs text-gray-400">Swap</span>
          </button>
          <button className="flex flex-col items-center space-y-1">
            <Clock className="w-6 h-6 text-gray-400" />
            <span className="text-xs text-gray-400">History</span>
          </button>
          <button className="flex flex-col items-center space-y-1">
            <Search className="w-6 h-6 text-purple-500" />
            <span className="text-xs text-purple-500">Search</span>
          </button>
        </div>
      </div>
    </div>
  );
};

