'use client';

import React, { useState, useEffect } from 'react';
import { ArrowLeft, Coins, TrendingUp, Shield, AlertTriangle, Loader2, DollarSign, Info } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card } from '@/components/ui/card';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';
import { stablecoinAPI, type MintStablecoinRequest, type RedeemStablecoinRequest, type StablecoinPosition, type SystemStatus } from '@/lib/api/stablecoinApi';
import { useWalletStore } from '@/lib/store';
import { ProviderType } from '@/lib/types';
import { formatBalance } from '@/lib/utils';
import { toastManager } from '@/lib/toast';

interface StablecoinDashboardProps {
  onBack: () => void;
}

export const StablecoinDashboard: React.FC<StablecoinDashboardProps> = ({ onBack }) => {
  const { wallets, user } = useWalletStore();
  const [systemStatus, setSystemStatus] = useState<SystemStatus | null>(null);
  const [positions, setPositions] = useState<StablecoinPosition[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [activeTab, setActiveTab] = useState<'dashboard' | 'mint' | 'redeem'>('dashboard');

  // Mint form state
  const [mintZecAmount, setMintZecAmount] = useState('');
  const [mintStablecoinAmount, setMintStablecoinAmount] = useState('');
  const [isMinting, setIsMinting] = useState(false);

  // Redeem form state
  const [redeemPositionId, setRedeemPositionId] = useState('');
  const [redeemAmount, setRedeemAmount] = useState('');
  const [isRedeeming, setIsRedeeming] = useState(false);

  const zcashWallet = wallets[ProviderType.ZcashOASIS]?.[0];
  const aztecWallet = wallets[ProviderType.AztecOASIS]?.[0];

  // Calculate totals
  const totalZUSD = positions.reduce((sum, pos) => sum + pos.debtAmount, 0);
  const totalCollateral = positions.reduce((sum, pos) => sum + pos.collateralAmount, 0);
  const avgCollateralRatio = positions.length > 0
    ? positions.reduce((sum, pos) => sum + pos.collateralRatio, 0) / positions.length
    : 0;

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    setIsLoading(true);
    try {
      const [statusResult, positionsResult] = await Promise.all([
        stablecoinAPI.getSystemStatus(),
        stablecoinAPI.getPositions(),
      ]);

      if (!statusResult.isError && statusResult.result) {
        setSystemStatus(statusResult.result);
      }

      if (!positionsResult.isError && positionsResult.result) {
        setPositions(positionsResult.result);
        if (positionsResult.result.length > 0) {
          setRedeemPositionId(positionsResult.result[0].positionId);
        }
      }
    } catch (error) {
      console.error('Failed to load stablecoin data:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleMint = async () => {
    if (!zcashWallet || !aztecWallet) {
      toastManager.error('Zcash and Aztec wallets required');
      return;
    }

    const zecAmount = parseFloat(mintZecAmount);
    const stablecoinAmount = parseFloat(mintStablecoinAmount);

    if (!zecAmount || !stablecoinAmount || zecAmount <= 0 || stablecoinAmount <= 0) {
      toastManager.error('Invalid amounts');
      return;
    }

    setIsMinting(true);
    try {
      const request: MintStablecoinRequest = {
        zecAmount,
        stablecoinAmount,
        zcashAddress: zcashWallet.walletAddress,
        aztecAddress: aztecWallet.walletAddress,
        generateViewingKey: true,
      };

      const result = await stablecoinAPI.mintStablecoin(request);

      if (result.isError) {
        toastManager.error(result.message || 'Mint failed');
        return;
      }

      toastManager.success(`Stablecoin minted! Position ID: ${result.result?.positionId}`);
      setMintZecAmount('');
      setMintStablecoinAmount('');
      await loadData();
      setActiveTab('dashboard');
    } catch (error: any) {
      toastManager.error(error.message || 'Mint failed');
    } finally {
      setIsMinting(false);
    }
  };

  const handleRedeem = async () => {
    if (!redeemPositionId || !redeemAmount) {
      toastManager.error('Select position and enter amount');
      return;
    }

    if (!zcashWallet) {
      toastManager.error('Zcash wallet required');
      return;
    }

    const amount = parseFloat(redeemAmount);
    if (!amount || amount <= 0) {
      toastManager.error('Invalid amount');
      return;
    }

    setIsRedeeming(true);
    try {
      const request: RedeemStablecoinRequest = {
        positionId: redeemPositionId,
        stablecoinAmount: amount,
        zcashAddress: zcashWallet.walletAddress,
      };

      const result = await stablecoinAPI.redeemStablecoin(request);

      if (result.isError) {
        toastManager.error(result.message || 'Redeem failed');
        return;
      }

      toastManager.success('Stablecoin redeemed successfully!');
      setRedeemAmount('');
      await loadData();
      setActiveTab('dashboard');
    } catch (error: any) {
      toastManager.error(error.message || 'Redeem failed');
    } finally {
      setIsRedeeming(false);
    }
  };

  const getHealthColor = (health: StablecoinPosition['health']) => {
    switch (health) {
      case 'safe':
        return 'text-green-400';
      case 'warning':
        return 'text-yellow-400';
      case 'danger':
        return 'text-red-400';
      default:
        return 'text-gray-400';
    }
  };

  return (
    <div className="min-h-screen bg-black text-white flex flex-col">
      <header className="px-4 pt-12 pb-4 border-b border-gray-800 flex items-center space-x-4">
        <button onClick={onBack} className="p-2">
          <ArrowLeft className="w-6 h-6" />
        </button>
        <div>
          <p className="text-xs text-gray-400 uppercase tracking-wider">Zcash-Backed Stablecoin</p>
          <h1 className="text-xl font-semibold">zUSD</h1>
        </div>
      </header>

      <main className="flex-1 px-4 py-6 space-y-6 overflow-y-auto">

        <Tabs value={activeTab} onValueChange={(v) => setActiveTab(v as any)} className="space-y-6">
          <TabsList className="bg-white/5 border border-white/10">
            <TabsTrigger value="dashboard" className="data-[state=active]:text-white data-[state=active]:bg-white/10">
              Dashboard
            </TabsTrigger>
            <TabsTrigger value="mint" className="data-[state=active]:text-white data-[state=active]:bg-white/10">
              Mint zUSD
            </TabsTrigger>
            <TabsTrigger value="redeem" className="data-[state=active]:text-white data-[state=active]:bg-white/10">
              Redeem
            </TabsTrigger>
          </TabsList>

          {/* Dashboard Tab */}
          <TabsContent value="dashboard" className="space-y-6">
            {/* Balance Cards */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {/* zUSD Balance Card */}
              <div className="rounded-2xl border border-white/10 bg-black/40 p-6">
                <div className="flex items-center justify-between mb-4">
                  <h3 className="text-sm text-gray-400 uppercase tracking-widest">Your zUSD Balance</h3>
                  <DollarSign className="w-5 h-5 text-white" />
                </div>
                <div>
                  <p className="text-4xl font-bold text-white mb-2">
                    {formatBalance(totalZUSD)}
                  </p>
                  <p className="text-sm text-gray-400">
                    ${formatBalance(totalZUSD)} USD
                  </p>
                  <p className="text-xs text-gray-500 mt-2">
                    Available on Aztec Network
                  </p>
                </div>
              </div>

              {/* Collateral Card */}
              <div className="rounded-2xl border border-white/10 bg-black/40 p-6">
                <div className="flex items-center justify-between mb-4">
                  <h3 className="text-sm text-gray-400 uppercase tracking-widest">Total Collateral</h3>
                  <Shield className="w-5 h-5 text-white" />
                </div>
                <div>
                  <p className="text-4xl font-bold text-white mb-2">
                    {formatBalance(totalCollateral)}
                  </p>
                  <p className="text-sm text-gray-400">
                    ZEC Locked
                  </p>
                  {positions.length > 0 && (
                    <div className="mt-4 rounded-lg border border-white/5 bg-white/5 p-3">
                      <p className="text-lg font-bold text-white">
                        {avgCollateralRatio.toFixed(1)}% Ratio
                      </p>
                      <p className="text-xs text-gray-400 mt-1">
                        Average collateralization
                      </p>
                    </div>
                  )}
                </div>
              </div>
            </div>

            {/* System Status */}
            {systemStatus && (
              <div className="rounded-3xl border border-white/5 bg-white/5 p-5 space-y-5">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-xs uppercase tracking-widest text-gray-400">System Status</p>
                    <h2 className="text-xl font-semibold text-white">zUSD Metrics</h2>
                  </div>
                  <Coins className="w-5 h-5 text-white" />
                </div>
                <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                  <div>
                    <p className="text-xs text-gray-400 mb-1">Total Supply</p>
                    <p className="text-2xl font-bold text-white">{formatBalance(systemStatus.totalSupply)} zUSD</p>
                  </div>
                  <div>
                    <p className="text-xs text-gray-400 mb-1">Total Collateral</p>
                    <p className="text-2xl font-bold text-white">{formatBalance(systemStatus.totalCollateral)} ZEC</p>
                  </div>
                  <div>
                    <p className="text-xs text-gray-400 mb-1">Collateral Ratio</p>
                    <p className="text-2xl font-bold text-white">{systemStatus.collateralRatio.toFixed(1)}%</p>
                  </div>
                  <div>
                    <p className="text-xs text-gray-400 mb-1">Current APY</p>
                    <p className="text-2xl font-bold text-white flex items-center space-x-1">
                      <TrendingUp className="w-5 h-5" />
                      <span>{systemStatus.currentAPY.toFixed(2)}%</span>
                    </p>
                  </div>
                </div>
                <div className="rounded-2xl bg-black/40 border border-white/5 p-4 text-sm text-gray-300 space-y-2">
                  <p className="flex justify-between">
                    <span>ZEC Price (Oracle)</span>
                    <span className="font-semibold">${formatBalance(systemStatus.zecPrice)}</span>
                  </p>
                  <p className="flex justify-between">
                    <span>Liquidation Threshold</span>
                    <span className="font-semibold text-yellow-400">{systemStatus.liquidationThreshold.toFixed(1)}%</span>
                  </p>
                </div>
              </div>
            )}

            {/* Your Positions */}
            <div className="rounded-3xl border border-white/5 bg-white/5 p-5 space-y-5">
              <div>
                <p className="text-xs uppercase tracking-widest text-gray-400">Your Positions</p>
                <h2 className="text-xl font-semibold text-white">Active zUSD Positions</h2>
              </div>
              {isLoading ? (
                <div className="text-center py-12">
                  <Loader2 className="w-8 h-8 animate-spin mx-auto text-white mb-4" />
                  <p className="text-gray-400">Loading positions...</p>
                </div>
              ) : positions.length === 0 ? (
                <div className="text-center py-12">
                  <Coins className="w-16 h-16 mx-auto text-gray-600 mb-4 opacity-50" />
                  <p className="text-gray-400 text-lg mb-2">No positions yet</p>
                  <p className="text-sm text-gray-500 mb-4">Mint some zUSD to get started!</p>
                  <Button
                    onClick={() => setActiveTab('mint')}
                    className="bg-white text-black hover:bg-white/90 text-base font-semibold"
                  >
                    <Coins className="w-4 h-4 mr-2" />
                    Mint zUSD
                  </Button>
                </div>
              ) : (
                <div className="space-y-3">
                  {positions.map((position) => (
                    <div
                      key={position.positionId}
                      className="rounded-2xl border border-white/10 bg-black/40 p-4"
                    >
                      <div className="flex items-center justify-between mb-4">
                        <div className="flex items-center space-x-3">
                          <Shield className={`w-5 h-5 ${getHealthColor(position.health)}`} />
                          <div>
                            <span className="font-semibold text-base capitalize text-white">{position.health}</span>
                            <p className="text-xs text-gray-400">Position ID: {position.positionId.slice(0, 8)}...</p>
                          </div>
                        </div>
                        <div className="text-right">
                          <p className="text-xs text-gray-400">Collateral Ratio</p>
                          <p className="text-lg font-bold text-white">
                            {position.collateralRatio.toFixed(1)}%
                          </p>
                        </div>
                      </div>
                      <div className="grid grid-cols-2 gap-3 mb-4">
                        <div className="p-3 bg-white/5 rounded-lg border border-white/5">
                          <p className="text-xs text-gray-400 mb-1">Collateral (ZEC)</p>
                          <p className="text-lg font-bold text-white">{formatBalance(position.collateralAmount)} ZEC</p>
                          <p className="text-xs text-gray-500 mt-1">
                            ${formatBalance(position.collateralAmount * (systemStatus?.zecPrice || 0))} USD
                          </p>
                        </div>
                        <div className="p-3 bg-white/5 rounded-lg border border-white/5">
                          <p className="text-xs text-gray-400 mb-1">Debt (zUSD)</p>
                          <p className="text-lg font-bold text-white">{formatBalance(position.debtAmount)} zUSD</p>
                        </div>
                      </div>
                      <div className="flex items-center justify-between text-xs text-gray-500 pt-3 border-t border-white/5">
                        <span>Created: {new Date(position.createdAt).toLocaleDateString()}</span>
                        <span>Updated: {new Date(position.lastUpdated).toLocaleDateString()}</span>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </TabsContent>

          {/* Mint Tab */}
          <TabsContent value="mint" className="space-y-6">
            <div className="rounded-3xl border border-white/5 bg-white/5 p-5 space-y-5">
              <div>
                <p className="text-xs uppercase tracking-widest text-gray-400">Mint zUSD</p>
                <h2 className="text-xl font-semibold text-white">Lock ZEC to mint zUSD</h2>
              </div>

              <form className="space-y-4">
                <div className="rounded-2xl border border-white/10 bg-black/40 p-4 space-y-3">
                  <div className="flex items-center justify-between text-xs uppercase tracking-widest text-gray-400">
                    <span>ZEC Amount to Lock</span>
                  </div>
                  <div className="flex items-center space-x-3">
                    <div className="flex-1">
                      <Input
                        type="number"
                        value={mintZecAmount}
                        onChange={(e) => setMintZecAmount(e.target.value)}
                        placeholder="0.0"
                        className="bg-transparent border-none text-3xl font-semibold text-white focus-visible:ring-0 px-0"
                      />
                      {zcashWallet && (
                        <p className="text-xs text-gray-500 mt-1">
                          Balance: {formatBalance(zcashWallet.balance || 0)} ZEC
                        </p>
                      )}
                    </div>
                  </div>
                </div>

                <div className="rounded-2xl border border-white/10 bg-black/40 p-4 space-y-3">
                  <div className="flex items-center justify-between text-xs uppercase tracking-widest text-gray-400">
                    <span>zUSD Amount to Mint</span>
                  </div>
                  <div className="flex items-center space-x-3">
                    <div className="flex-1">
                      <Input
                        type="number"
                        value={mintStablecoinAmount}
                        onChange={(e) => setMintStablecoinAmount(e.target.value)}
                        placeholder="0.0"
                        className="bg-transparent border-none text-3xl font-semibold text-white focus-visible:ring-0 px-0"
                      />
                      {systemStatus && (
                        <p className="text-xs text-gray-500 mt-1">
                          Collateral Ratio: {systemStatus.collateralRatio.toFixed(1)}% • APY: {systemStatus.currentAPY.toFixed(2)}%
                        </p>
                      )}
                    </div>
                  </div>
                </div>

                {/* Preview */}
                {mintZecAmount && mintStablecoinAmount && (
                  <div className="rounded-2xl bg-black/40 border border-white/5 p-4 text-sm text-gray-300 space-y-2">
                    <p className="flex justify-between">
                      <span>You lock</span>
                      <span className="font-semibold">{formatBalance(parseFloat(mintZecAmount))} ZEC</span>
                    </p>
                    <p className="flex justify-between">
                      <span>You receive</span>
                      <span className="font-semibold text-green-400">{formatBalance(parseFloat(mintStablecoinAmount))} zUSD</span>
                    </p>
                    <p className="flex justify-between">
                      <span>Privacy Features</span>
                      <span className="text-green-400">Private on Aztec</span>
                    </p>
                  </div>
                )}

                <Button
                  onClick={handleMint}
                  disabled={isMinting || !mintZecAmount || !mintStablecoinAmount}
                  className="w-full bg-white text-black hover:bg-white/90 text-base font-semibold"
                >
                  {isMinting ? (
                    <>
                      <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                      Minting...
                    </>
                  ) : (
                    'Mint zUSD'
                  )}
                </Button>
              </form>
            </div>

            <div className="rounded-3xl border border-white/5 bg-white/5 p-5">
              <div className="flex items-start gap-3">
                <Info className="w-5 h-5 text-white flex-shrink-0 mt-1" />
                <div className="space-y-2 text-sm text-gray-300">
                  <p>
                    <strong className="text-white">How it works:</strong> Your ZEC is locked as collateral 
                    and stored privately on Aztec Network. zUSD is minted at the current collateral ratio and appears 
                    in your Aztec wallet.
                  </p>
                  <p>
                    Your position generates yield from Aztec's private DeFi protocols. You can redeem zUSD anytime 
                    to unlock your ZEC collateral (minus any fees).
                  </p>
                </div>
              </div>
            </div>
          </TabsContent>

          {/* Redeem Tab */}
          <TabsContent value="redeem" className="space-y-6">
            <div className="rounded-3xl border border-white/5 bg-white/5 p-5 space-y-5">
              <div>
                <p className="text-xs uppercase tracking-widest text-gray-400">Redeem zUSD</p>
                <h2 className="text-xl font-semibold text-white">Burn zUSD to unlock ZEC</h2>
              </div>

              {positions.length > 0 ? (
                <form className="space-y-4">
                  <div>
                    <label className="text-sm text-gray-400 block mb-2">Select Position</label>
                    <select
                      value={redeemPositionId}
                      onChange={(e) => setRedeemPositionId(e.target.value)}
                      className="w-full bg-black/40 border border-white/10 rounded-lg px-4 py-3 text-white"
                    >
                      {positions.map((pos) => (
                        <option key={pos.positionId} value={pos.positionId}>
                          {formatBalance(pos.debtAmount)} zUSD (Collateral: {formatBalance(pos.collateralAmount)} ZEC)
                        </option>
                      ))}
                    </select>
                  </div>
                  <div className="rounded-2xl border border-white/10 bg-black/40 p-4 space-y-3">
                    <div className="flex items-center justify-between text-xs uppercase tracking-widest text-gray-400">
                      <span>zUSD Amount to Redeem</span>
                    </div>
                    <div className="flex items-center space-x-3">
                      <div className="flex-1">
                        <Input
                          type="number"
                          value={redeemAmount}
                          onChange={(e) => setRedeemAmount(e.target.value)}
                          placeholder="0.0"
                          className="bg-transparent border-none text-3xl font-semibold text-white focus-visible:ring-0 px-0"
                        />
                        {redeemPositionId && positions.find(p => p.positionId === redeemPositionId) && (
                          <p className="text-xs text-gray-500 mt-1">
                            Max: {formatBalance(positions.find(p => p.positionId === redeemPositionId)!.debtAmount)} zUSD
                          </p>
                        )}
                      </div>
                    </div>
                  </div>

                  {/* Preview */}
                  {redeemAmount && redeemPositionId && positions.find(p => p.positionId === redeemPositionId) && (
                    <div className="rounded-2xl bg-black/40 border border-white/5 p-4 text-sm text-gray-300 space-y-2">
                      <p className="flex justify-between">
                        <span>You burn</span>
                        <span className="font-semibold">{formatBalance(parseFloat(redeemAmount))} zUSD</span>
                      </p>
                      <p className="flex justify-between">
                        <span>You receive</span>
                        <span className="font-semibold text-green-400">
                          {formatBalance((parseFloat(redeemAmount) / (systemStatus?.collateralRatio || 150) * 100))} ZEC
                        </span>
                      </p>
                      <p className="flex justify-between">
                        <span>Privacy Features</span>
                        <span className="text-green-400">Shielded Zcash Withdrawal</span>
                      </p>
                    </div>
                  )}

                  <Button
                    onClick={handleRedeem}
                    disabled={isRedeeming || !redeemAmount || !redeemPositionId}
                    className="w-full bg-white text-black hover:bg-white/90 text-base font-semibold"
                  >
                    {isRedeeming ? (
                      <>
                        <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                        Redeeming...
                      </>
                    ) : (
                      'Redeem zUSD'
                    )}
                  </Button>
                </form>
              ) : (
                <div className="text-center py-12">
                  <AlertTriangle className="w-16 h-16 mx-auto text-yellow-400 mb-4 opacity-50" />
                  <p className="text-gray-400 text-lg mb-2">No positions to redeem</p>
                  <p className="text-sm text-gray-500 mb-4">Mint some zUSD first</p>
                  <Button
                    onClick={() => setActiveTab('mint')}
                    className="bg-white text-black hover:bg-white/90 text-base font-semibold"
                  >
                    <Coins className="w-4 h-4 mr-2" />
                    Mint zUSD
                  </Button>
                </div>
              )}
            </div>
          </TabsContent>
        </Tabs>
      </main>
    </div>
  );
};
