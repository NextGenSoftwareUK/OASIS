'use client';

import React, { useState, useEffect } from 'react';
import Image from 'next/image';
import { ArrowLeft, Copy, Check, Share2, QrCode, DollarSign, MoreVertical } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { QRCodeSVG } from 'qrcode.react';
import { LineChart, Line, XAxis, YAxis, ResponsiveContainer, Tooltip } from 'recharts';
import { Wallet, ProviderType } from '@/lib/types';
import { formatAddress, formatBalance } from '@/lib/utils';
import { getProviderMetadata } from '@/lib/providerMeta';
import { normalizeProviderType } from '@/lib/providerTypeMapper';
import {
  getCoinGeckoId,
  hasCoinGeckoPriceData,
  fetchCoinGeckoPrice,
  fetchCoinGeckoMarketChart,
  timeFrameToDays,
  formatPriceHistory,
} from '@/lib/coingecko';
import { fetchSolanaBalance } from '@/lib/solanaBalance';
import { fetchZcashBalance } from '@/lib/zcashBalance';

interface WalletDetailScreenProps {
  wallet: Wallet;
  onBack: () => void;
  onReceive?: () => void;
  onBuy?: () => void;
  onShare?: () => void;
}

type TimeFrame = '1H' | '1D' | '1W' | '1M' | 'YTD' | 'ALL';

interface PriceData {
  price: number;
  change24h: number;
  changePercent24h: number;
  marketCap?: number;
  history: Array<{ time: string; price: number }>;
}

export const WalletDetailScreen: React.FC<WalletDetailScreenProps> = ({
  wallet,
  onBack,
  onReceive,
  onBuy,
  onShare,
}) => {
  const [copied, setCopied] = useState(false);
  const [timeFrame, setTimeFrame] = useState<TimeFrame>('1D');
  const [priceData, setPriceData] = useState<PriceData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showQR, setShowQR] = useState(false);

  const normalizedType = normalizeProviderType(wallet.providerType);
  const meta = getProviderMetadata(normalizedType);
  const [actualBalance, setActualBalance] = useState<number | null>(null);
  const [balanceLoading, setBalanceLoading] = useState(false);
  const balance = actualBalance !== null ? actualBalance : (wallet.balance || 0);
  const usdValue = balance * (priceData?.price || 0);
  const return24h = balance * (priceData?.change24h || 0);

  // Fetch actual blockchain balance for Solana and Zcash
  useEffect(() => {
    const fetchBlockchainBalance = async () => {
      if (!wallet.walletAddress) {
        return;
      }

      if (normalizedType === ProviderType.SolanaOASIS) {
        setBalanceLoading(true);
        try {
          const solBalance = await fetchSolanaBalance(wallet.walletAddress, 'devnet');
          setActualBalance(solBalance);
          console.log(`💰 Fetched Solana balance: ${solBalance} SOL`);
        } catch (error) {
          console.error('Failed to fetch Solana balance:', error);
          // Keep the wallet.balance as fallback
        } finally {
          setBalanceLoading(false);
        }
      } else if (normalizedType === ProviderType.ZcashOASIS) {
        setBalanceLoading(true);
        try {
          const zecBalance = await fetchZcashBalance(wallet.walletAddress, 'testnet');
          setActualBalance(zecBalance);
          console.log(`💰 Fetched Zcash balance: ${zecBalance} ZEC`);
        } catch (error) {
          console.error('Failed to fetch Zcash balance:', error);
          // Keep the wallet.balance as fallback
        } finally {
          setBalanceLoading(false);
        }
      }
    };

    fetchBlockchainBalance();
  }, [normalizedType, wallet.walletAddress]);

  // Fetch real price data from CoinGecko
  useEffect(() => {
    const fetchPriceData = async () => {
      setLoading(true);
      setError(null);
      try {
        const coinId = getCoinGeckoId(normalizedType);
        
        // Check if this provider has price data available
        if (!coinId) {
          const errorMsg = `Price data is not available for ${meta.name}. This token does not have a listing on CoinGecko.`;
          console.warn(errorMsg);
          setError(errorMsg);
          setPriceData(null);
          setLoading(false);
          return;
        }
        
        console.log(`📊 Fetching CoinGecko data for ${normalizedType} (${coinId}) with timeframe ${timeFrame}...`);

        // Fetch current price and 24h change
        const priceDataResult = await fetchCoinGeckoPrice(coinId);
        if (!priceDataResult) {
          const errorMsg = `No price data found for ${coinId}. The token may not be available on CoinGecko.`;
          console.warn(errorMsg);
          setError(errorMsg);
          setPriceData(null);
          setLoading(false);
          return;
        }

        // Fetch market chart data
        const days = timeFrameToDays(timeFrame);
        console.log(`📊 Fetching market chart for ${coinId} with ${days} days...`);
        const marketChartData = await fetchCoinGeckoMarketChart(coinId, days);
        
        if (!marketChartData || !marketChartData.prices || marketChartData.prices.length === 0) {
          const errorMsg = `No market chart data found for ${coinId}. Historical data may not be available.`;
          console.warn(errorMsg);
          setError(errorMsg);
          // Still show price data even if chart data is missing
          setPriceData({
            price: priceDataResult.price,
            change24h: (priceDataResult.change24h / 100) * priceDataResult.price,
            changePercent24h: priceDataResult.change24h,
            marketCap: priceDataResult.marketCap,
            history: [],
          });
          setLoading(false);
          return;
        }

        // Format history data for chart
        const history = formatPriceHistory(marketChartData, timeFrame);
        
        if (history.length === 0) {
          console.warn('No valid history data after formatting');
          setError('Unable to format price history data');
        } else {
          console.log(`📊 Chart data points: ${history.length}`, history.slice(0, 3), '...', history.slice(-3));
          if (history.length > 0) {
            console.log(`📊 Price range: $${Math.min(...history.map(h => h.price)).toFixed(2)} - $${Math.max(...history.map(h => h.price)).toFixed(2)}`);
          }
        }

        // Calculate 24h change in USD (price change percentage * current price)
        const change24hUsd = (priceDataResult.change24h / 100) * priceDataResult.price;

        setPriceData({
          price: priceDataResult.price,
          change24h: change24hUsd,
          changePercent24h: priceDataResult.change24h,
          marketCap: priceDataResult.marketCap,
          history,
        });

        console.log(`✅ Fetched price data: $${priceDataResult.price.toFixed(2)}, 24h: ${priceDataResult.change24h.toFixed(2)}%`);
        setError(null);
      } catch (error) {
        const errorMsg = error instanceof Error ? error.message : 'Failed to fetch price data';
        console.error('Failed to fetch price data:', error);
        setError(errorMsg);
      } finally {
        setLoading(false);
      }
    };

    fetchPriceData();
  }, [normalizedType, timeFrame]);

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(wallet.walletAddress);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch (error) {
      console.error('Failed to copy:', error);
    }
  };

  const handleShare = async () => {
    if (navigator.share) {
      try {
        await navigator.share({
          title: `My ${meta.name} Wallet`,
          text: `Wallet Address: ${wallet.walletAddress}`,
        });
      } catch (error) {
        console.error('Failed to share:', error);
      }
    } else if (onShare) {
      onShare();
    }
  };

  // Ensure chart data is properly formatted and validated
  const chartData = priceData?.history
    ? priceData.history
        .map((point) => ({
          time: point.time,
          price: typeof point.price === 'number' ? Number(point.price.toFixed(2)) : Number(Number(point.price).toFixed(2)),
        }))
        .filter((point) => !isNaN(point.price) && point.price > 0 && point.price < 1e10)
    : [];

  return (
    <div className="min-h-screen bg-black text-white flex flex-col">
      {/* Header */}
      <header className="px-4 pt-12 pb-4 border-b border-gray-800">
        <div className="flex items-center justify-between mb-4">
          <button
            onClick={onBack}
            className="p-2 hover:bg-gray-800 rounded-lg transition-colors"
          >
            <ArrowLeft className="w-6 h-6" />
          </button>
          <h1 className="text-xl font-semibold">{meta.name}</h1>
          <button
            onClick={() => setShowQR(!showQR)}
            className="p-2 hover:bg-gray-800 rounded-lg transition-colors"
          >
            <QrCode className="w-6 h-6" />
          </button>
        </div>

        {/* Price Display */}
        {loading ? (
          <div className="animate-pulse">
            <div className="h-10 bg-gray-800 rounded w-48 mb-2"></div>
            <div className="h-6 bg-gray-800 rounded w-32"></div>
          </div>
        ) : priceData ? (
          <>
            <div className="text-4xl font-bold mb-2">
              ${priceData.price.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
            </div>
            <div className={`flex items-center space-x-2 ${priceData.change24h >= 0 ? 'text-green-500' : 'text-red-500'}`}>
              <span>
                {priceData.change24h >= 0 ? '+' : ''}${priceData.change24h.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
              </span>
              <span className="px-2 py-1 rounded bg-gray-800 text-sm">
                {priceData.changePercent24h >= 0 ? '+' : ''}{priceData.changePercent24h.toFixed(2)}%
              </span>
            </div>
          </>
        ) : error ? (
          <div className="text-gray-400 text-sm">
            Price data not available
          </div>
        ) : null}
      </header>

      {/* QR Code Modal */}
      {showQR && (
        <div className="fixed inset-0 bg-black/80 z-50 flex items-center justify-center p-4">
          <Card className="bg-zypherpunk-surface border-zypherpunk-border p-6 max-w-sm w-full">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-xl font-semibold">Receive {meta.symbol}</h2>
              <button
                onClick={() => setShowQR(false)}
                className="p-2 hover:bg-gray-800 rounded-lg transition-colors"
              >
                <ArrowLeft className="w-6 h-6" />
              </button>
            </div>
            <div className="flex flex-col items-center space-y-4">
              <div className="bg-white p-4 rounded-lg">
                <QRCodeSVG value={wallet.walletAddress} size={200} />
              </div>
              <div className="w-full">
                <div className="flex items-center space-x-2 bg-gray-900 p-3 rounded-lg mb-2">
                  <span className="text-sm text-gray-400 flex-1 break-all">{wallet.walletAddress}</span>
                  <button
                    onClick={handleCopy}
                    className="p-2 hover:bg-gray-800 rounded transition-colors"
                  >
                    {copied ? <Check className="w-5 h-5 text-green-500" /> : <Copy className="w-5 h-5" />}
                  </button>
                </div>
                <Button
                  onClick={handleShare}
                  className="w-full bg-zypherpunk-primary hover:bg-zypherpunk-primary/80"
                >
                  <Share2 className="w-4 h-4 mr-2" />
                  Share Address
                </Button>
              </div>
            </div>
          </Card>
        </div>
      )}

      {/* Price Chart */}
      {(priceData || error || loading) && (
        <div className="px-4 py-6">
          {error && (
            <div className="mb-4 p-3 bg-yellow-900/20 border border-yellow-700/50 rounded-lg">
              <p className="text-yellow-400 text-sm">{error}</p>
            </div>
          )}
          {loading && (
            <div className="h-64 mb-4 flex items-center justify-center">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-zypherpunk-primary"></div>
            </div>
          )}
          {!loading && priceData && chartData.length > 0 && (
            <div className="h-64 mb-4" key={`chart-${timeFrame}-${normalizedType}`}>
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={chartData} margin={{ top: 5, right: 5, left: 5, bottom: 5 }}>
                <XAxis
                  dataKey="time"
                  stroke="#666"
                  fontSize={11}
                  tickLine={false}
                  axisLine={false}
                  interval="preserveStartEnd"
                />
                <YAxis
                  stroke="#666"
                  fontSize={11}
                  tickLine={false}
                  axisLine={false}
                  domain={['auto', 'auto']}
                  tickFormatter={(value) => `$${value.toFixed(2)}`}
                />
                <Tooltip
                  contentStyle={{
                    backgroundColor: '#1a1a1a',
                    border: '1px solid #333',
                    borderRadius: '8px',
                    padding: '8px 12px',
                  }}
                  labelStyle={{ color: '#fff', marginBottom: '4px' }}
                  formatter={(value: number) => [`$${value.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`, 'Price']}
                />
                <Line
                  type="monotone"
                  dataKey="price"
                  stroke={priceData.change24h >= 0 ? '#10b981' : '#ef4444'}
                  strokeWidth={2}
                  dot={false}
                  activeDot={{ r: 4, fill: priceData.change24h >= 0 ? '#10b981' : '#ef4444' }}
                />
                </LineChart>
              </ResponsiveContainer>
            </div>
          )}
          {!loading && priceData && chartData.length === 0 && !error && (
            <div className="h-64 mb-4 flex items-center justify-center bg-gray-900 rounded-lg">
              <p className="text-gray-400 text-sm">No chart data available</p>
            </div>
          )}
          {!loading && !priceData && error && (
            <div className="h-64 mb-4 flex items-center justify-center bg-gray-900 rounded-lg">
              <p className="text-gray-400 text-sm">Price data not available for this token</p>
            </div>
          )}

          {/* Time Frame Selectors - Only show if price data is available */}
          {priceData && (
            <div className="flex space-x-2 overflow-x-auto pb-2">
              {(['1H', '1D', '1W', '1M', 'YTD', 'ALL'] as TimeFrame[]).map((tf) => (
                <button
                  key={tf}
                  onClick={() => {
                    console.log(`🔄 Switching time frame to ${tf}`);
                    setTimeFrame(tf);
                  }}
                  disabled={loading}
                  className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
                    timeFrame === tf
                      ? 'bg-zypherpunk-primary text-white'
                      : 'bg-gray-900 text-gray-400 hover:bg-gray-800'
                  } ${loading ? 'opacity-50 cursor-not-allowed' : ''}`}
                >
                  {tf}
                </button>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Your Position */}
      <div className="px-4 py-6 space-y-4">
        <h2 className="text-lg font-semibold">Your Position</h2>
        <div className="grid grid-cols-2 gap-3">
          <Card className="bg-zypherpunk-surface border-zypherpunk-border p-4">
            <div className="text-sm text-gray-400 mb-1">Balance</div>
            {balanceLoading ? (
              <div className="text-xl font-semibold animate-pulse">Loading...</div>
            ) : (
              <div className="text-xl font-semibold">{formatBalance(balance)} {meta.symbol}</div>
            )}
            <div className="text-xs text-gray-500 mt-1">{meta.symbol}</div>
          </Card>
          <Card className="bg-zypherpunk-surface border-zypherpunk-border p-4">
            <div className="text-sm text-gray-400 mb-1">Value</div>
            <div className="text-xl font-semibold">
              ${usdValue.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
            </div>
            <div className="text-xs text-gray-500 mt-1">USD</div>
          </Card>
        </div>
        <Card className="bg-zypherpunk-surface border-zypherpunk-border p-4">
          <div className="text-sm text-gray-400 mb-1">24h Return</div>
          <div className={`text-xl font-semibold ${return24h >= 0 ? 'text-green-500' : 'text-red-500'}`}>
            {return24h >= 0 ? '+' : ''}${return24h.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
          </div>
        </Card>
      </div>

      {/* Info Section */}
      <div className="px-4 py-6 space-y-4">
        <h2 className="text-lg font-semibold">Info</h2>
        <Card className="bg-zypherpunk-surface border-zypherpunk-border p-4 space-y-3">
          <div className="flex justify-between">
            <span className="text-gray-400">Name</span>
            <span className="font-medium">{meta.name}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-gray-400">Symbol</span>
            <span className="font-medium">{meta.symbol}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-gray-400">Network</span>
            <span className="font-medium">{meta.name}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-gray-400">Wallet Address</span>
            <span className="font-mono text-sm">{formatAddress(wallet.walletAddress, 8)}</span>
          </div>
          {priceData?.marketCap && (
            <div className="flex justify-between">
              <span className="text-gray-400">Market Cap</span>
              <span className="font-medium">
                {priceData.marketCap >= 1e12
                  ? `$${(priceData.marketCap / 1e12).toFixed(2)}T`
                  : priceData.marketCap >= 1e9
                  ? `$${(priceData.marketCap / 1e9).toFixed(2)}B`
                  : priceData.marketCap >= 1e6
                  ? `$${(priceData.marketCap / 1e6).toFixed(2)}M`
                  : `$${priceData.marketCap.toLocaleString('en-US', { maximumFractionDigits: 0 })}`}
              </span>
            </div>
          )}
        </Card>
      </div>

      {/* Action Buttons */}
      <div className="px-4 pb-8 pt-4 border-t border-gray-800">
        <div className="grid grid-cols-4 gap-3">
          <button
            onClick={() => {
              setShowQR(true);
              onReceive?.();
            }}
            className="flex flex-col items-center justify-center p-4 bg-zypherpunk-surface border border-zypherpunk-border rounded-xl hover:border-zypherpunk-primary/50 transition-colors"
          >
            <QrCode className="w-6 h-6 mb-2 text-zypherpunk-primary" />
            <span className="text-xs">Receive</span>
          </button>
          <button
            onClick={onBuy}
            className="flex flex-col items-center justify-center p-4 bg-zypherpunk-surface border border-zypherpunk-border rounded-xl hover:border-zypherpunk-primary/50 transition-colors"
          >
            <DollarSign className="w-6 h-6 mb-2 text-zypherpunk-accent" />
            <span className="text-xs">Cash Buy</span>
          </button>
          <button
            onClick={handleShare}
            className="flex flex-col items-center justify-center p-4 bg-zypherpunk-surface border border-zypherpunk-border rounded-xl hover:border-zypherpunk-primary/50 transition-colors"
          >
            <Share2 className="w-6 h-6 mb-2 text-zypherpunk-secondary" />
            <span className="text-xs">Share</span>
          </button>
          <button className="flex flex-col items-center justify-center p-4 bg-zypherpunk-surface border border-zypherpunk-border rounded-xl hover:border-zypherpunk-primary/50 transition-colors">
            <MoreVertical className="w-6 h-6 mb-2 text-gray-400" />
            <span className="text-xs">More</span>
          </button>
        </div>
      </div>
    </div>
  );
};

