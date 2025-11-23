'use client';

import React, { useEffect, useMemo, useState } from 'react';
import Image from 'next/image';
import { ArrowLeftRight, Loader2, Shield, Lock, Eye } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { toastManager } from '@/lib/toast';
import { useWalletStore } from '@/lib/store';
import { ProviderType } from '@/lib/types';
import { formatBalance, formatAddress } from '@/lib/utils';
import { bridgeAPI, type BridgeTransferRequest } from '@/lib/api/bridgeApi';

// Re-export for use in PrivacyBridgeScreen
export type { BridgeTransferRequest };
import { PrivacyBridgeTokenSelectModal } from './PrivacyBridgeTokenSelectModal';

// Privacy-focused bridge chains (Zcash, Aztec, Miden)
export const privacyBridgeChains = [
  { 
    name: 'Zcash', 
    symbol: 'ZEC', 
    logoUrl: 'https://cryptologos.cc/logos/zcash-zec-logo.svg?v=025', 
    network: 'Zcash',
    description: 'Privacy-focused cryptocurrency',
    providerType: ProviderType.ZcashOASIS,
    category: 'Layer1' as const
  },
  { 
    name: 'Aztec', 
    symbol: 'AZTEC', 
    logoUrl: '/aztec-logo.png', 
    network: 'Aztec',
    description: 'Privacy-first L2 with private smart contracts',
    providerType: ProviderType.AztecOASIS,
    category: 'Layer2' as const
  },
  { 
    name: 'Miden', 
    symbol: 'MIDEN', 
    logoUrl: '/miden-logo.png',
    network: 'Miden',
    description: 'Zero-knowledge VM for privacy',
    providerType: ProviderType.MidenOASIS,
    category: 'Layer2' as const
  },
];

interface PrivacyBridgeFormState {
  from: typeof privacyBridgeChains[0];
  to: typeof privacyBridgeChains[0];
  fromAmount: string;
  toAmount: string;
  destination: string;
  memo: string;
  usePartialNotes: boolean;
  generateViewingKey: boolean;
}

export function PrivacyBridgeForm() {
  const { user, wallets } = useWalletStore();
  const [state, setState] = useState<PrivacyBridgeFormState>({
    from: privacyBridgeChains[0], // Zcash
    to: privacyBridgeChains[1], // Aztec
    fromAmount: '',
    toAmount: '',
    destination: '',
    memo: '',
    usePartialNotes: false,
    generateViewingKey: true,
  });
  const [submitting, setSubmitting] = useState(false);
  const [tokenModalTarget, setTokenModalTarget] = useState<'from' | 'to' | null>(null);

  const userId = user?.id;

  // Get wallet for selected chain
  const availableWallets = wallets[state.from.providerType] || [];
  const fromWallet = availableWallets[0]; // Use first wallet, or allow selection if multiple
  const fromBalance = fromWallet?.balance || 0;
  const numAmount = parseFloat(state.fromAmount) || 0;
  const isValidAmount = numAmount > 0 && numAmount <= fromBalance;

  // Calculate exchange rate (1:1 for same asset, or fetch real rate)
  const exchangeRate = useMemo(() => {
    // For privacy bridges, typically 1:1 (same asset, different chain)
    // Could fetch real rate if needed
    return 1.0;
  }, [state.from.symbol, state.to.symbol]);

  // Update toAmount when fromAmount changes
  useEffect(() => {
    if (state.fromAmount && exchangeRate) {
      const numeric = parseFloat(state.fromAmount || '0');
      setState((prev) => ({
        ...prev,
        toAmount: formatNumber(numeric * exchangeRate),
      }));
    }
  }, [state.fromAmount, exchangeRate]);

  const handleAmountChange = (value: string, field: 'fromAmount' | 'toAmount') => {
    if (field === 'fromAmount') {
      const numeric = parseFloat(value || '0');
      setState((prev) => ({
        ...prev,
        fromAmount: value,
        toAmount: formatNumber(numeric * exchangeRate),
      }));
    } else {
      const numeric = parseFloat(value || '0');
      setState((prev) => ({
        ...prev,
        toAmount: value,
        fromAmount: exchangeRate ? formatNumber(numeric / exchangeRate) : prev.fromAmount,
      }));
    }
  };

  const handleSwap = () => {
    setState((prev) => ({
      ...prev,
      from: prev.to,
      to: prev.from,
      fromAmount: prev.toAmount,
      toAmount: prev.fromAmount,
      destination: '', // Clear destination when swapping
    }));
  };

  const onSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!userId) {
      toastManager.error('Connect your avatar before creating bridge orders.');
      return;
    }

    if (!state.destination) {
      toastManager.error('Destination address is required');
      return;
    }
    if (!isValidAmount) {
      toastManager.error('Enter a valid amount (must be less than or equal to your balance)');
      return;
    }

    if (!fromWallet) {
      toastManager.error(`${state.from.name} wallet not found. Please create or import a wallet first.`);
      return;
    }

    try {
      setSubmitting(true);
      
      const request: BridgeTransferRequest = {
        fromProviderType: state.from.providerType.toString(),
        toProviderType: state.to.providerType.toString(),
        fromAddress: fromWallet.walletAddress,
        toAddress: state.destination,
        amount: numAmount,
        memo: state.memo || undefined,
        partialNotes: state.usePartialNotes,
        generateViewingKey: state.generateViewingKey,
      };

      const result = await bridgeAPI.transfer(request);

      if (result.isError) {
        toastManager.error(result.message || 'Bridge transfer failed');
        return;
      }

      toastManager.success(`Bridge transfer initiated! Transaction ID: ${result.result?.transactionId}`);
      
      // Reset form
      setState((prev) => ({
        ...prev,
        fromAmount: '',
        toAmount: '',
        destination: '',
        memo: '',
      }));
    } catch (error: any) {
      toastManager.error(error?.message || 'Bridge transfer failed');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="rounded-3xl border border-white/5 bg-white/5 p-5 space-y-5">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-xs uppercase tracking-widest text-gray-400">Privacy Bridge</p>
            <h2 className="text-xl font-semibold text-white">Private Cross-Chain Transfer</h2>
          </div>
          <Button
            variant="outline"
            size="sm"
            onClick={handleSwap}
            className="bg-black/40 border-white/10 text-white"
          >
            <ArrowLeftRight className="w-4 h-4 mr-2" />
            Flip
          </Button>
        </div>

        <form className="space-y-4" onSubmit={onSubmit}>
          <PrivacyBridgeInputCard
            label="From"
            chain={state.from}
            amount={state.fromAmount}
            balance={fromBalance}
            walletAddress={fromWallet?.walletAddress}
            walletCount={availableWallets.length}
            onAmountChange={(value) => handleAmountChange(value, 'fromAmount')}
            onSelect={() => setTokenModalTarget('from')}
          />

          <PrivacyBridgeInputCard
            label="To"
            chain={state.to}
            amount={state.toAmount}
            balance={0} // Destination balance not shown
            onAmountChange={(value) => handleAmountChange(value, 'toAmount')}
            onSelect={() => setTokenModalTarget('to')}
          />

          <div>
            <label className="text-sm text-gray-400 block mb-2">Destination address</label>
            <Input
              placeholder={`Enter ${state.to.name} address`}
              value={state.destination}
              onChange={(e) => setState((prev) => ({ ...prev, destination: e.target.value }))}
              className="bg-black/40 border-white/10 text-white placeholder:text-gray-500"
            />
          </div>

          <div>
            <label className="text-sm text-gray-400 block mb-2">Memo (Optional)</label>
            <Input
              placeholder="Private memo for this transaction"
              value={state.memo}
              onChange={(e) => setState((prev) => ({ ...prev, memo: e.target.value }))}
              className="bg-black/40 border-white/10 text-white placeholder:text-gray-500"
            />
          </div>

          {/* Privacy Options */}
          <div className="rounded-2xl bg-black/40 border border-white/5 p-4 space-y-3">
            <p className="text-sm font-semibold text-white flex items-center space-x-2">
              <Shield className="w-4 h-4 text-zypherpunk-primary" />
              <span>Privacy Options</span>
            </p>
            
            <div className="flex items-center space-x-2">
              <input
                type="checkbox"
                id="partialNotes"
                checked={state.usePartialNotes}
                onChange={(e) => setState((prev) => ({ ...prev, usePartialNotes: e.target.checked }))}
                className="rounded border-white/10 bg-black/40"
              />
              <label htmlFor="partialNotes" className="text-sm text-gray-300 flex items-center space-x-2">
                <Lock className="w-4 h-4 text-zypherpunk-primary" />
                <span>Use Partial Notes (Enhanced Privacy)</span>
              </label>
            </div>

            <div className="flex items-center space-x-2">
              <input
                type="checkbox"
                id="viewingKey"
                checked={state.generateViewingKey}
                onChange={(e) => setState((prev) => ({ ...prev, generateViewingKey: e.target.checked }))}
                className="rounded border-white/10 bg-black/40"
              />
              <label htmlFor="viewingKey" className="text-sm text-gray-300 flex items-center space-x-2">
                <Eye className="w-4 h-4 text-zypherpunk-accent" />
                <span>Generate Viewing Key (For Auditability)</span>
              </label>
            </div>
          </div>

          {/* Bridge Info */}
          <div className="rounded-2xl bg-black/40 border border-white/5 p-4 text-sm text-gray-300 space-y-2">
            <p className="flex justify-between">
              <span>Exchange Rate</span>
              <span>1 {state.from.symbol} = {exchangeRate.toFixed(6)} {state.to.symbol}</span>
            </p>
            <p className="flex justify-between">
              <span>Privacy Level</span>
              <span className="text-zypherpunk-primary font-semibold">Maximum</span>
            </p>
            <p className="flex justify-between">
              <span>Transaction Type</span>
              <span className="text-zypherpunk-accent">Shielded / Private</span>
            </p>
          </div>

          <Button
            type="submit"
            className="w-full bg-white text-black hover:bg-white/90 text-base font-semibold"
            disabled={submitting || !isValidAmount || !state.destination}
          >
            {submitting ? (
              <>
                <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                Bridging...
              </>
            ) : (
              <>
                <Shield className="w-4 h-4 mr-2" />
                Bridge Privately
              </>
            )}
          </Button>
        </form>
      </div>

      <PrivacyBridgeTokenSelectModal
        isOpen={tokenModalTarget !== null}
        onClose={() => setTokenModalTarget(null)}
        onSelect={(chain) => {
          if (!tokenModalTarget) return;
          setState((prev) => ({
            ...prev,
            [tokenModalTarget]: chain,
          }));
        }}
        target={tokenModalTarget || 'from'}
        chains={privacyBridgeChains}
      />
    </div>
  );
}

interface PrivacyBridgeInputCardProps {
  label: string;
  chain: typeof privacyBridgeChains[0];
  amount: string;
  balance: number;
  walletAddress?: string;
  walletCount?: number;
  onAmountChange: (value: string) => void;
  onSelect: () => void;
}

function PrivacyBridgeInputCard({ 
  label, 
  chain, 
  amount, 
  balance, 
  walletAddress,
  walletCount = 0,
  onAmountChange, 
  onSelect 
}: PrivacyBridgeInputCardProps) {

  return (
    <div className="rounded-2xl border border-white/10 bg-black/40 p-4 space-y-3">
      <div className="flex items-center justify-between text-xs uppercase tracking-widest text-gray-400">
        <span>{label}</span>
        <button type="button" onClick={onSelect} className="text-white text-sm font-semibold hover:underline">
          {chain.symbol}
        </button>
      </div>
      <div className="flex items-center space-x-3">
        <div className="flex-1">
          <Input
            value={amount}
            onChange={(e) => onAmountChange(e.target.value)}
            type="number"
            min="0"
            step="0.0001"
            placeholder="0.0"
            className="bg-transparent border-none text-3xl font-semibold text-white focus-visible:ring-0 px-0"
          />
          {label === 'From' && (
            <div className="mt-1 space-y-1">
              {balance > 0 && (
                <p className="text-xs text-gray-500">
                  Balance: {formatBalance(balance)} {chain.symbol}
                </p>
              )}
              {walletAddress && (
                <p className="text-xs text-gray-400">
                  From: <span className="text-gray-300 font-mono">{formatAddress(walletAddress, 6)}</span>
                  {walletCount > 1 && (
                    <span className="text-gray-500 ml-1">({walletCount} wallets available)</span>
                  )}
                </p>
              )}
              {!walletAddress && (
                <p className="text-xs text-yellow-500">
                  ⚠️ No {chain.name} wallet found. Please create or import a wallet first.
                </p>
              )}
            </div>
          )}
        </div>
        <button
          type="button"
          onClick={onSelect}
          className="w-12 h-12 rounded-full bg-white/10 flex items-center justify-center hover:bg-white/20 transition-colors cursor-pointer"
          title={`Click to select ${chain.name}`}
        >
          <Image
            src={chain.logoUrl}
            alt={chain.symbol}
            width={32}
            height={32}
            className="object-contain"
            loading="lazy"
          />
        </button>
      </div>
      <p className="text-xs text-gray-400">{chain.description}</p>
    </div>
  );
}

function formatNumber(value: number) {
  if (!isFinite(value)) return '0';
  return value.toFixed(6).replace(/\.?0+$/, '');
}

