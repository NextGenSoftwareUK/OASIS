'use client';

import React, { useEffect, useMemo, useState } from 'react';
import Image from 'next/image';
import { ArrowLeftRight, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { toastManager } from '@/lib/toast';
import { universalBridgeChains, type BridgeChain } from '@/lib/providerMeta';
import { fetchExchangeRate, fetchUsdPrice } from '@/lib/bridge/exchangeRate';
import { useWalletStore } from '@/lib/store';
import { config } from '@/lib/config';
import { TokenSelectModal } from '@/components/swap/TokenSelectModal';

interface SwapFormState {
  from: BridgeChain;
  to: BridgeChain;
  fromAmount: string;
  toAmount: string;
  destination: string;
}

export function SwapForm() {
  const { user } = useWalletStore();
  const [state, setState] = useState<SwapFormState>({
    from: universalBridgeChains[0],
    to: universalBridgeChains[1],
    fromAmount: '1',
    toAmount: '',
    destination: '',
  });
  const [rate, setRate] = useState(1);
  const [rateStatus, setRateStatus] = useState<'idle' | 'loading' | 'error' | 'ready'>('idle');
  const [usdValues, setUsdValues] = useState<{ from: number; to: number }>({ from: 0, to: 0 });
  const [submitting, setSubmitting] = useState(false);
  const [tokenModalTarget, setTokenModalTarget] = useState<'from' | 'to' | null>(null);

  const userId = user?.id;
  // Use local OASIS bridge API instead of external qstreetrwa API
  const oasisApiUrl = process.env.NEXT_PUBLIC_OASIS_API_URL || 'https://localhost:5004';
  const apiUrl = `${oasisApiUrl}/api/v1`;

  // fetch exchange rate on token change
  useEffect(() => {
    let cancelled = false;
    setRateStatus('loading');
    fetchExchangeRate(state.from.symbol, state.to.symbol)
      .then((nextRate) => {
        if (cancelled) return;
        setRate(nextRate);
        setRateStatus('ready');
        setState((prev) => ({
          ...prev,
          toAmount: formatNumber(parseFloat(prev.fromAmount || '0') * nextRate),
        }));
      })
      .catch(() => {
        if (cancelled) return;
        setRateStatus('error');
      });
    return () => {
      cancelled = true;
    };
  }, [state.from.symbol, state.to.symbol]);

  // fetch USD price approximations
  useEffect(() => {
    let cancelled = false;
    const loadPrices = async () => {
      const [fromUsd, toUsd] = await Promise.all([
        fetchUsdPrice(state.from.symbol),
        fetchUsdPrice(state.to.symbol),
      ]);
      if (!cancelled) {
        setUsdValues({ from: fromUsd, to: toUsd });
      }
    };
    loadPrices();
    return () => {
      cancelled = true;
    };
  }, [state.from.symbol, state.to.symbol]);

  const feeEstimate = useMemo(() => {
    const amount = parseFloat(state.fromAmount || '0');
    return amount * 0.0025; // 0.25%
  }, [state.fromAmount]);

  const handleAmountChange = (value: string, field: 'fromAmount' | 'toAmount') => {
    if (field === 'fromAmount') {
      const numeric = parseFloat(value || '0');
      setState((prev) => ({
        ...prev,
        fromAmount: value,
        toAmount: formatNumber(numeric * rate),
      }));
    } else {
      const numeric = parseFloat(value || '0');
      setState((prev) => ({
        ...prev,
        toAmount: value,
        fromAmount: rate ? formatNumber(numeric / rate) : prev.fromAmount,
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
    }));
  };

  const onSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!userId) {
      toastManager.error('Connect your avatar before creating swap orders.');
      return;
    }

    if (!state.destination) {
      toastManager.error('Destination address is required');
      return;
    }
    if (!parseFloat(state.fromAmount)) {
      toastManager.error('Enter an amount to swap');
      return;
    }

    try {
      setSubmitting(true);
      
      // Use proxy for local OASIS API (handles self-signed certificates)
      const useProxy = process.env.NEXT_PUBLIC_USE_API_PROXY === 'true';
      const targetUrl = useProxy 
        ? `/api/proxy/api/v1/orders`
        : `${apiUrl}/orders`;
      
      // Prepare request body matching CreateBridgeOrderRequest format
      const requestBody = {
        userId: userId, // Will be converted to Guid on backend
        fromToken: state.from.symbol,
        toToken: state.to.symbol,
        amount: parseFloat(state.fromAmount),
        fromNetwork: state.from.network,
        toNetwork: state.to.network,
        destinationAddress: state.destination,
      };
      
      const response = await fetch(targetUrl, {
        method: 'POST',
        headers: { 
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(requestBody),
      });

      if (!response.ok) {
        const message = await response.text();
        throw new Error(message || 'Swap order failed');
      }

      const data = await response.json();
      // OASIS API returns { orderId: "...", ... } directly, not nested in data
      const orderId = data?.orderId || data?.data?.orderId || data?.result?.orderId || '';
      toastManager.success(`Swap order ${orderId ? `#${orderId}` : ''} created successfully!`);
      // reset?
    } catch (error: any) {
      toastManager.error(error?.message || 'Swap order failed');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="rounded-3xl border border-white/5 bg-white/5 p-5 space-y-5">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-xs uppercase tracking-widest text-gray-400">Universal Asset Bridge</p>
            <h2 className="text-xl font-semibold text-white">Swap assets</h2>
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
          <SwapInputCard
            label="From"
            chain={state.from}
            amount={state.fromAmount}
            onAmountChange={(value) => handleAmountChange(value, 'fromAmount')}
            usdValue={parseFloat(state.fromAmount || '0') * usdValues.from}
            onSelect={() => setTokenModalTarget('from')}
          />

          <SwapInputCard
            label="To"
            chain={state.to}
            amount={state.toAmount}
            onAmountChange={(value) => handleAmountChange(value, 'toAmount')}
            usdValue={parseFloat(state.toAmount || '0') * usdValues.to}
            onSelect={() => setTokenModalTarget('to')}
          />

          <div>
            <label className="text-sm text-gray-400 block mb-2">Destination address</label>
            <Input
              placeholder="Enter recipient address"
              value={state.destination}
              onChange={(e) => setState((prev) => ({ ...prev, destination: e.target.value }))}
              className="bg-black/40 border-white/10 text-white placeholder:text-gray-500"
            />
          </div>

          <div className="rounded-2xl bg-black/40 border border-white/5 p-4 text-sm text-gray-300 space-y-2">
            <p className="flex justify-between">
              <span>Rate</span>
              <span>
                {rateStatus === 'loading' && 'Fetching rate...'}
                {rateStatus === 'error' && 'Unavailable'}
                {rateStatus === 'ready' && `1 ${state.from.symbol} = ${rate.toFixed(6)} ${state.to.symbol}`}
              </span>
            </p>
            <p className="flex justify-between">
              <span>Estimated fee (0.25%)</span>
              <span>{feeEstimate.toFixed(4)} {state.from.symbol}</span>
            </p>
            <p className="flex justify-between">
              <span>Slippage control</span>
              <span className="text-green-400">Smart routing enabled</span>
            </p>
          </div>

          <Button
            type="submit"
            className="w-full bg-white text-black hover:bg-white/90 text-base font-semibold"
            disabled={submitting || rateStatus === 'loading'}
          >
            {submitting ? (
              <>
                <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                Creating order...
              </>
            ) : (
              'Create swap order'
            )}
          </Button>
        </form>
      </div>

      <TokenSelectModal
        isOpen={tokenModalTarget !== null}
        onClose={() => setTokenModalTarget(null)}
        onSelect={(chain) => {
          if (!tokenModalTarget) return;
          setState((prev) => ({
            ...prev,
            [tokenModalTarget]: chain,
            // Reset amounts when chain changes to avoid confusion
            fromAmount: tokenModalTarget === 'from' ? prev.fromAmount : '',
            toAmount: tokenModalTarget === 'to' ? prev.toAmount : '',
          }));
          setTokenModalTarget(null);
        }}
        target={tokenModalTarget || 'from'}
      />
    </div>
  );
}

interface SwapInputCardProps {
  label: string;
  chain: BridgeChain;
  amount: string;
  usdValue: number;
  onAmountChange: (value: string) => void;
  onSelect: () => void;
}

function SwapInputCard({ label, chain, amount, usdValue, onAmountChange, onSelect }: SwapInputCardProps) {
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
            className="bg-transparent border-none text-3xl font-semibold text-white focus-visible:ring-0 px-0"
          />
          <p className="text-xs text-gray-500 mt-1">≈ ${usdValue.toLocaleString(undefined, { maximumFractionDigits: 2 })} USD</p>
        </div>
        <button
          type="button"
          onClick={onSelect}
          className="w-12 h-12 rounded-full bg-white/10 hover:bg-white/20 transition-colors cursor-pointer group overflow-hidden p-0 relative"
          aria-label={`Select ${chain.name} chain`}
        >
          <Image
            src={chain.logoUrl}
            alt={chain.symbol}
            fill
            className="object-cover group-hover:scale-110 transition-transform"
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

