'use client';

import React, { useState } from 'react';
import Image from 'next/image';
import { Copy, Search, Menu, QrCode, Send, ArrowLeftRight, Home, Clock, Shield, Lock, Coins, Network } from 'lucide-react';
import { useWalletStore } from '@/lib/store';
import { ProviderType } from '@/lib/types';
import { formatAddress, formatBalance } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Card } from '@/components/ui/card';
import { cn } from '@/lib/utils';
import { getProviderMetadata, universalBridgeChains } from '@/lib/providerMeta';
import { PrivacyIndicator } from '@/components/privacy/PrivacyIndicator';
import { filterUniversalWallets } from '@/lib/walletUtils';
import { normalizeProviderType } from '@/lib/providerTypeMapper';

interface MobileWalletHomeProps {
  onSend: () => void;
  onReceive: () => void;
  onSwap: () => void;
  onBuy: () => void;
  onTokens: () => void;
  onCollectibles: () => void;
  onHistory: () => void;
  onHome: () => void;
  onSecurity?: () => void;
  onPrivacy?: () => void;
  onShieldedSend?: () => void;
  onStablecoin?: () => void;
  onBridge?: () => void;
  onLogout?: () => void;
  onCreateWallet?: () => void;
}

export const MobileWalletHome: React.FC<MobileWalletHomeProps> = ({
  onSend,
  onReceive,
  onSwap,
  onBuy,
  onTokens,
  onCollectibles,
  onHistory,
  onHome,
  onSecurity,
  onPrivacy,
  onShieldedSend,
  onStablecoin,
  onBridge,
  onLogout,
  onCreateWallet,
}) => {
  const { wallets, user, isLoading, error } = useWalletStore();
  const [activeTab, setActiveTab] = useState<'tokens' | 'collectibles' | 'stablecoin'>('tokens');

  // Filter out Universal/Default wallets
  const filteredWallets = filterUniversalWallets(wallets);
  const allWallets = Object.values(filteredWallets).flat();
  
  // Additional filter: Remove wallets that are Default or show as "OASIS" (defaultMeta)
  const finalWallets = allWallets.filter(wallet => {
    // Normalize providerType first (handles numeric values from API)
    const normalizedType = normalizeProviderType(wallet.providerType);
    
    // Direct check for Default provider type
    if (normalizedType === ProviderType.Default || 
        normalizedType === ProviderType.LocalFileOASIS ||
        normalizedType === ProviderType.MongoDBOASIS) {
      console.log('Filtering out Default/LocalFile/MongoDB wallet:', normalizedType, wallet.walletId);
      return false;
    }
    
    // Check if metadata resolves to default (OASIS)
    const meta = getProviderMetadata(normalizedType);
    if (meta.name === 'OASIS' && meta.providerType === ProviderType.Default) {
      console.log('Filtering out OASIS default wallet:', normalizedType, wallet.walletId);
      return false;
    }
    
    return true;
  });
  // Group wallets by provider type and get one wallet per type (prioritize privacy chains)
  const walletsByProvider = new Map<ProviderType, Wallet>();
  const priorityOrder = [
    ProviderType.ZcashOASIS,
    ProviderType.AztecOASIS,
    ProviderType.MidenOASIS,
    ProviderType.StarknetOASIS,
    ProviderType.EthereumOASIS,
    ProviderType.SolanaOASIS,
    ProviderType.PolygonOASIS,
    ProviderType.ArbitrumOASIS,
  ];
  
  // First, add one wallet from each priority provider type
  for (const providerType of priorityOrder) {
    const wallet = finalWallets.find(w => normalizeProviderType(w.providerType) === providerType);
    if (wallet) {
      walletsByProvider.set(providerType, wallet);
    }
  }
  
  // Then add any remaining provider types
  for (const wallet of finalWallets) {
    const normalizedType = normalizeProviderType(wallet.providerType);
    if (!walletsByProvider.has(normalizedType)) {
      walletsByProvider.set(normalizedType, wallet);
    }
  }
  
  // Convert to array and sort by priority order
  const featuredWallets = Array.from(walletsByProvider.values())
    .sort((a, b) => {
      const aType = normalizeProviderType(a.providerType);
      const bType = normalizeProviderType(b.providerType);
      const aIndex = priorityOrder.indexOf(aType);
      const bIndex = priorityOrder.indexOf(bType);
      // If both are in priority order, sort by index; otherwise, prioritize those in the list
      if (aIndex !== -1 && bIndex !== -1) return aIndex - bIndex;
      if (aIndex !== -1) return -1;
      if (bIndex !== -1) return 1;
      // If neither is in priority, sort by balance
      return (b.balance || 0) - (a.balance || 0);
    });

  const totalBalance = finalWallets.reduce((sum, w) => sum + (w.balance || 0), 0);
  const usdValue = totalBalance * 1800; // Mock conversion rate

  const handleCopyAddress = async (address: string) => {
    try {
      await navigator.clipboard.writeText(address);
    } catch (error) {
      console.error('Failed to copy:', error);
    }
  };

  const displayUsername = user?.username ? `@${user.username}` : '@guest';
  const accountLabel = user?.email || (user?.id ? formatAddress(user.id, 6) : 'No avatar connected');
  const avatarInitial = user?.username?.[0]?.toUpperCase() || user?.firstName?.[0]?.toUpperCase() || '👤';

  return (
    <div className="min-h-screen bg-black text-white flex flex-col">
      {/* Header */}
      <header className="px-4 pt-12 pb-4 border-b border-gray-800">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center space-x-3">
            <div className="w-10 h-10 rounded-full bg-gradient-to-br from-green-400 to-orange-500 flex items-center justify-center text-lg font-semibold">
              {avatarInitial}
            </div>
            <div>
              <div className="flex items-center space-x-2">
                <span className="font-semibold">{displayUsername}</span>
                {user?.username && (
                  <button
                    onClick={() => handleCopyAddress(user.username)}
                    className="text-gray-400 hover:text-white transition-colors"
                    aria-label="Copy username"
                  >
                    <Copy className="w-4 h-4" />
                  </button>
                )}
              </div>
              <div className="flex items-center space-x-2 text-sm text-gray-400">
                <span>{accountLabel}</span>
                {user?.id && (
                  <button
                    onClick={() => handleCopyAddress(user.id)}
                    className="text-gray-500 hover:text-white transition-colors"
                    aria-label="Copy avatar id"
                  >
                    <Copy className="w-3 h-3" />
                  </button>
                )}
              </div>
            </div>
          </div>
          <div className="flex items-center space-x-3">
            <Search className="w-5 h-5 text-gray-400" />
            <Menu className="w-5 h-5 text-gray-400" />
            {onLogout && (
              <button
                onClick={onLogout}
                className="text-xs text-gray-400 hover:text-white border border-gray-800 rounded-full px-3 py-1 transition-colors"
              >
                Sign out
              </button>
            )}
          </div>
        </div>
      </header>

      {/* Testnet Mode Banner */}
      <div className="bg-yellow-500/20 border-y border-yellow-500/30 px-4 py-2">
        <p className="text-sm text-yellow-400 text-center">
          You are currently in Testnet Mode
        </p>
      </div>

      {/* Balance Display */}
      <div className="px-4 py-6 text-center">
        <div className="text-4xl font-bold mb-1">
          {totalBalance > 0 ? formatBalance(totalBalance) : '—'}
        </div>
        <div className="text-gray-400 text-sm">
          {usdValue > 0 ? `≈ $${formatBalance(usdValue)} USD` : '—'}
        </div>
      </div>


      {/* Action Buttons */}
      <div className="px-4 mb-6">
        <div className="grid grid-cols-3 gap-3 mb-3">
          <button
            onClick={onReceive}
            className="flex flex-col items-center justify-center p-4 bg-zypherpunk-surface border border-zypherpunk-border rounded-xl hover:border-zypherpunk-accent transition-colors"
          >
            <div className="w-12 h-12 bg-zypherpunk-bg border border-zypherpunk-border rounded-lg flex items-center justify-center mb-2">
              <QrCode className="w-6 h-6 text-zypherpunk-accent" />
            </div>
            <span className="text-xs text-zypherpunk-text">Receive</span>
          </button>
          <button
            onClick={onSend}
            className="flex flex-col items-center justify-center p-4 bg-zypherpunk-surface border border-zypherpunk-border rounded-xl hover:border-zypherpunk-accent transition-colors"
          >
            <div className="w-12 h-12 bg-zypherpunk-bg border border-zypherpunk-border rounded-lg flex items-center justify-center mb-2">
              <Send className="w-6 h-6 text-zypherpunk-accent" />
            </div>
            <span className="text-xs text-zypherpunk-text">Send</span>
          </button>
          {onShieldedSend && (
            <button
              onClick={onShieldedSend}
              className="flex flex-col items-center justify-center p-4 bg-zypherpunk-surface border border-zypherpunk-primary/40 rounded-xl hover:border-zypherpunk-primary/60 transition-colors"
              title="Shielded Send (Zcash)"
            >
              <div className="w-12 h-12 bg-zypherpunk-primary/10 border border-zypherpunk-primary/30 rounded-lg flex items-center justify-center mb-2">
                <Shield className="w-6 h-6 text-zypherpunk-primary" />
              </div>
              <span className="text-xs text-zypherpunk-primary">Shielded</span>
            </button>
          )}
        </div>
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
            {onBridge && (
              <button
                onClick={onBridge}
                className="flex flex-col items-center justify-center p-4 bg-zypherpunk-surface border border-zypherpunk-accent/40 rounded-xl hover:border-zypherpunk-accent/60 transition-colors"
              >
                <div className="w-12 h-12 bg-zypherpunk-accent/10 border border-zypherpunk-accent/30 rounded-lg flex items-center justify-center mb-2">
                  <Network className="w-6 h-6 text-zypherpunk-accent" />
                </div>
                <span className="text-xs text-zypherpunk-accent font-semibold">Bridge</span>
              </button>
            )}
            {onPrivacy && (
              <button
                onClick={onPrivacy}
                className="flex flex-col items-center justify-center p-4 bg-zypherpunk-surface border border-zypherpunk-secondary/40 rounded-xl hover:border-zypherpunk-secondary/60 transition-colors"
              >
                <div className="w-12 h-12 bg-zypherpunk-secondary/10 border border-zypherpunk-secondary/30 rounded-lg flex items-center justify-center mb-2">
                  <Lock className="w-6 h-6 text-zypherpunk-secondary" />
                </div>
                <span className="text-xs text-zypherpunk-secondary font-semibold">Privacy</span>
              </button>
            )}
          </div>
      </div>

      {/* Universal Asset Bridge Logos */}
      <div className="px-4 mb-6">
        <div className="flex items-center justify-between mb-3">
          <p className="text-xs uppercase tracking-widest text-gray-400">Universal Asset Bridge</p>
          <button
            onClick={onTokens}
            className="text-[11px] text-purple-400 hover:text-purple-300 transition-colors"
          >
            View tokens →
          </button>
        </div>
        <div className="flex gap-3 overflow-x-auto pb-2 scrollbar-thin scrollbar-thumb-gray-800">
          {universalBridgeChains.map(chain => (
            <div
              key={chain.name}
              className="min-w-[88px] rounded-2xl border border-white/5 bg-white/5 backdrop-blur-sm px-3 py-2 flex flex-col items-center"
            >
              <div className="w-10 h-10 rounded-full bg-black/40 mb-2 overflow-hidden relative">
                <Image
                  src={chain.logoUrl}
                  alt={chain.name}
                  fill
                  className="object-cover"
                  loading="lazy"
                />
              </div>
              <p className="text-xs font-semibold">{chain.symbol}</p>
              <p className="text-[10px] text-gray-400">{chain.name}</p>
            </div>
          ))}
        </div>
      </div>

      {/* Tabs */}
      <div className="px-4 mb-4">
        <div className="flex items-center justify-between mb-4">
          <Tabs
            value={activeTab}
            onValueChange={(v) => {
              const value = v as 'tokens' | 'collectibles' | 'stablecoin';
              setActiveTab(value);
              if (value === 'collectibles') {
                onCollectibles();
              } else if (value === 'stablecoin' && onStablecoin) {
                onStablecoin();
              }
            }}
            className="w-full"
          >
            <TabsList className="bg-transparent border-b border-zypherpunk-border w-full justify-start p-0 h-auto">
              <TabsTrigger
                value="tokens"
                className="data-[state=active]:border-b-2 data-[state=active]:border-zypherpunk-primary/60 rounded-none pb-2 text-zypherpunk-text-muted data-[state=active]:text-zypherpunk-primary"
              >
                Tokens
              </TabsTrigger>
              <TabsTrigger
                value="collectibles"
                className="data-[state=active]:border-b-2 data-[state=active]:border-zypherpunk-primary/60 rounded-none pb-2 text-zypherpunk-text-muted data-[state=active]:text-zypherpunk-primary"
              >
                Collectibles
              </TabsTrigger>
              {onStablecoin && (
                <TabsTrigger
                  value="stablecoin"
                  className="data-[state=active]:border-b-2 data-[state=active]:border-zypherpunk-accent/60 rounded-none pb-2 text-zypherpunk-text-muted data-[state=active]:text-zypherpunk-accent"
                >
                  Stablecoin
                </TabsTrigger>
              )}
            </TabsList>
          </Tabs>
          <div className="flex items-center space-x-2">
            <div className="w-1 h-1 bg-zypherpunk-primary rounded-full animate-pulse"></div>
            <div className="w-1 h-1 bg-zypherpunk-accent rounded-full animate-pulse" style={{ animationDelay: '0.2s' }}></div>
            <div className="w-1 h-1 bg-zypherpunk-secondary rounded-full animate-pulse" style={{ animationDelay: '0.4s' }}></div>
          </div>
        </div>

        {/* Tokens Tab Content */}
        {activeTab === 'tokens' && (
          <div className="space-y-3">
            {isLoading ? (
              <div className="text-center py-12">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-zypherpunk-primary mx-auto mb-4"></div>
                <p className="text-zypherpunk-text-muted">Loading wallets...</p>
              </div>
            ) : error ? (
              <div className="text-center py-12 px-4">
                <div className="bg-zypherpunk-secondary/10 border border-zypherpunk-secondary/30 rounded-lg p-4 mb-4">
                  <p className="text-zypherpunk-secondary font-semibold mb-2 flex items-center justify-center space-x-2">
                    <Shield className="w-5 h-5" />
                    <span>⚠️ API Unavailable</span>
                  </p>
                  <p className="text-sm text-zypherpunk-text-muted mb-3">{error}</p>
                  <div className="text-xs text-zypherpunk-text-muted space-y-1 text-left">
                    <p>• Check if OASIS API is running locally</p>
                    <p>• Set NEXT_PUBLIC_OASIS_API_URL in .env.local</p>
                    <p>• The API may be blocked by bot protection</p>
                  </div>
                </div>
                <p className="text-xs text-zypherpunk-text-muted mt-4">You can still use the UI, but wallet data won't load.</p>
              </div>
            ) : (
              <>
                {featuredWallets.length > 0 ? (
                  featuredWallets.map(wallet => {
                    const meta = getProviderMetadata(wallet.providerType);
                    return (
                      <Card key={wallet.walletId} className="bg-zypherpunk-surface border-zypherpunk-border p-4 hover:border-zypherpunk-primary/50 transition-colors">
                        <div className="flex items-center justify-between">
                          <div className="flex items-center space-x-3">
                            <div
                              className={cn(
                                'w-10 h-10 rounded-full bg-gradient-to-br overflow-hidden relative',
                                meta.backgroundGradient
                              )}
                            >
                              <Image
                                src={meta.logoUrl}
                                alt={meta.name}
                                fill
                                className="object-cover"
                                loading="lazy"
                              />
                            </div>
                            <div>
                              <div className="font-semibold">{meta.name}</div>
                              <div className="text-sm text-zypherpunk-text-muted">
                                {formatBalance(wallet.balance || 0)} {meta.symbol}
                              </div>
                            </div>
                          </div>
                          <div className="text-right">
                            <button
                              onClick={() => handleCopyAddress(wallet.walletAddress)}
                              className="text-xs text-zypherpunk-text-muted hover:text-zypherpunk-text transition-colors mb-1"
                            >
                              {formatAddress(wallet.walletAddress, 6)}
                            </button>
                            <div className="text-xs text-zypherpunk-text-muted">Updated {wallet.modifiedDate || 'today'}</div>
                          </div>
                        </div>
                      </Card>
                    );
                  })
                ) : (
                  <div className="text-center py-12 text-zypherpunk-text-muted">
                    <p>No tokens found</p>
                    <p className="text-sm mt-2 mb-4">Import or create a wallet to get started</p>
                    {onCreateWallet && (
                      <Button
                        onClick={onCreateWallet}
                        className="bg-zypherpunk-shielded hover:bg-zypherpunk-shielded/80 text-black"
                      >
                        Create Wallet
                      </Button>
                    )}
                  </div>
                )}
              </>
            )}
          </div>
        )}

        {/* Collectibles Tab Content */}
        {activeTab === 'collectibles' && (
          <div className="text-center py-12">
            <div className="w-24 h-24 mx-auto mb-4 flex items-center justify-center">
              <div className="relative">
                <div className="absolute inset-0 bg-gradient-to-br from-yellow-400 to-orange-500 rounded-full blur-xl opacity-50"></div>
                <div className="relative w-24 h-24 bg-gradient-to-br from-yellow-400 to-orange-500 rounded-full flex items-center justify-center">
                  <span className="text-4xl">🎵</span>
                </div>
              </div>
            </div>
            <p className="text-2xl font-semibold mb-2">No collectibles found</p>
            <p className="text-sm text-gray-400">
              Buy or transfer a collectible to start building your collection.
            </p>
          </div>
        )}

        {/* Stablecoin Tab Content */}
        {activeTab === 'stablecoin' && onStablecoin && (
          <div className="space-y-4">
            <div className="bg-gradient-to-r from-zypherpunk-accent/20 to-zypherpunk-secondary/20 border-2 border-zypherpunk-accent rounded-xl p-6">
              <div className="flex items-center justify-between mb-4">
                <div>
                  <h3 className="text-xl font-bold text-zypherpunk-accent mb-1">zUSD Stablecoin</h3>
                  <p className="text-sm text-zypherpunk-text-muted">Zcash-backed private stablecoin on Aztec</p>
                </div>
                <Coins className="w-8 h-8 text-zypherpunk-accent" />
              </div>
              <div className="grid grid-cols-2 gap-4 mb-4">
                <div>
                  <p className="text-xs text-zypherpunk-text-muted mb-1">Your Balance</p>
                  <p className="text-2xl font-bold text-zypherpunk-text">—</p>
                </div>
                <div>
                  <p className="text-xs text-zypherpunk-text-muted mb-1">APY</p>
                  <p className="text-2xl font-bold text-zypherpunk-accent">—</p>
                </div>
              </div>
              <button
                onClick={onStablecoin}
                className="w-full bg-zypherpunk-accent/20 hover:bg-zypherpunk-accent/30 border border-zypherpunk-accent/40 text-zypherpunk-accent font-semibold py-3 rounded-lg transition-colors"
              >
                Open Stablecoin Dashboard
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Bottom Navigation */}
      <footer className="mt-auto px-4 py-3 border-t border-zypherpunk-border bg-zypherpunk-bg">
        <div className="flex justify-around items-center">
          <button onClick={onHome} className="flex flex-col items-center text-zypherpunk-primary">
            <Home className="w-5 h-5" />
            <span className="text-[10px] mt-1">Home</span>
          </button>
          <button onClick={onSwap} className="flex flex-col items-center text-zypherpunk-text-muted hover:text-zypherpunk-primary transition-colors">
            <ArrowLeftRight className="w-5 h-5" />
            <span className="text-[10px] mt-1">Swap</span>
          </button>
          <button onClick={onHistory} className="flex flex-col items-center text-zypherpunk-text-muted hover:text-zypherpunk-primary transition-colors">
            <Clock className="w-5 h-5" />
            <span className="text-[10px] mt-1">History</span>
          </button>
          {onSecurity ? (
            <button onClick={onSecurity} className="flex flex-col items-center text-zypherpunk-text-muted hover:text-zypherpunk-primary transition-colors">
              <Shield className="w-5 h-5" />
              <span className="text-[10px] mt-1">Security</span>
            </button>
          ) : (
            <button onClick={onPrivacy} className="flex flex-col items-center text-zypherpunk-text-muted hover:text-zypherpunk-primary transition-colors">
              <Lock className="w-5 h-5" />
              <span className="text-[10px] mt-1">Privacy</span>
            </button>
          )}
        </div>
      </footer>
    </div>
  );
};

