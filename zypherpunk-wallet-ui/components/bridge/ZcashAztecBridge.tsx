'use client';

import React, { useState, useEffect } from 'react';
import { ArrowLeft, ArrowRight, Shield, Loader2, CheckCircle2, Clock, XCircle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card } from '@/components/ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { bridgeAPI, type BridgeRequest, type BridgeStatus } from '@/lib/api/bridgeApi';
import { useWalletStore } from '@/lib/store';
import { ProviderType } from '@/lib/types';
import { formatBalance } from '@/lib/utils';
import { toastManager } from '@/lib/toast';

interface ZcashAztecBridgeProps {
  onBack: () => void;
}

export const ZcashAztecBridge: React.FC<ZcashAztecBridgeProps> = ({ onBack }) => {
  const { wallets, user } = useWalletStore();
  const [direction, setDirection] = useState<'zcash-to-aztec' | 'aztec-to-zcash'>('zcash-to-aztec');
  const [amount, setAmount] = useState('');
  const [destination, setDestination] = useState('');
  const [usePartialNotes, setUsePartialNotes] = useState(false);
  const [generateViewingKey, setGenerateViewingKey] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [bridgeHistory, setBridgeHistory] = useState<BridgeStatus[]>([]);
  const [isLoadingHistory, setIsLoadingHistory] = useState(false);

  const zcashWallet = wallets[ProviderType.ZcashOASIS]?.[0];
  const aztecWallet = wallets[ProviderType.AztecOASIS]?.[0];

  const sourceWallet = direction === 'zcash-to-aztec' ? zcashWallet : aztecWallet;
  const sourceBalance = sourceWallet?.balance || 0;
  const numAmount = parseFloat(amount) || 0;
  const isValidAmount = numAmount > 0 && numAmount <= sourceBalance;
  const canBridge = isValidAmount && destination.length > 0 && !isSubmitting;

  useEffect(() => {
    loadBridgeHistory();
  }, []);

  const loadBridgeHistory = async () => {
    setIsLoadingHistory(true);
    try {
      const result = await bridgeAPI.getBridgeHistory();
      if (!result.isError && result.result) {
        setBridgeHistory(result.result.bridges || []);
      }
    } catch (error) {
      console.error('Failed to load bridge history:', error);
    } finally {
      setIsLoadingHistory(false);
    }
  };

  const handleBridge = async () => {
    if (!canBridge || !sourceWallet) return;

    setIsSubmitting(true);
    try {
      const request: BridgeRequest = {
        fromChain: direction === 'zcash-to-aztec' ? 'Zcash' : 'Aztec',
        toChain: direction === 'zcash-to-aztec' ? 'Aztec' : 'Zcash',
        amount: numAmount,
        fromAddress: sourceWallet.walletAddress,
        toAddress: destination,
        usePartialNotes,
        generateViewingKey,
      };

      const result = direction === 'zcash-to-aztec'
        ? await bridgeAPI.bridgeZcashToAztec(request)
        : await bridgeAPI.bridgeAztecToZcash(request);

      if (result.isError) {
        toastManager.error(result.message || 'Bridge failed');
        return;
      }

      toastManager.success(`Bridge initiated! Bridge ID: ${result.result?.bridgeId}`);
      setAmount('');
      setDestination('');
      await loadBridgeHistory();
    } catch (error: any) {
      toastManager.error(error.message || 'Bridge failed');
    } finally {
      setIsSubmitting(false);
    }
  };

  const getStatusIcon = (status: BridgeStatus['status']) => {
    switch (status) {
      case 'completed':
        return <CheckCircle2 className="w-4 h-4 text-zypherpunk-primary" />;
      case 'failed':
        return <XCircle className="w-4 h-4 text-zypherpunk-secondary" />;
      case 'pending':
      case 'locked':
      case 'minting':
        return <Clock className="w-4 h-4 text-zypherpunk-accent animate-spin" />;
      default:
        return <Clock className="w-4 h-4 text-zypherpunk-text-muted" />;
    }
  };

  return (
    <div className="min-h-screen bg-zypherpunk-bg text-zypherpunk-text p-4">
      <div className="max-w-2xl mx-auto space-y-6">
        {/* Header */}
        <div className="flex items-center space-x-4">
          <Button
            variant="ghost"
            onClick={onBack}
            className="text-zypherpunk-text-muted hover:text-zypherpunk-text"
          >
            <ArrowLeft className="w-5 h-5" />
          </Button>
          <div>
            <h1 className="text-2xl font-bold">Zcash ↔ Aztec Private Bridge</h1>
            <p className="text-sm text-zypherpunk-text-muted">Privacy-preserving cross-chain transfers</p>
          </div>
        </div>

        {/* Bridge Form */}
        <Card className="bg-zypherpunk-surface border-zypherpunk-border p-6">
          <div className="space-y-4">
            {/* Direction Selector */}
            <div>
              <label className="text-sm text-zypherpunk-text-muted mb-2 block">Direction</label>
              <Select
                value={direction}
                onValueChange={(value: 'zcash-to-aztec' | 'aztec-to-zcash') => setDirection(value)}
              >
                <SelectTrigger className="bg-zypherpunk-bg border-zypherpunk-border">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="zcash-to-aztec">
                    <div className="flex items-center space-x-2">
                      <span className="text-zypherpunk-primary">Zcash</span>
                      <ArrowRight className="w-4 h-4" />
                      <span className="text-zypherpunk-accent">Aztec</span>
                    </div>
                  </SelectItem>
                  <SelectItem value="aztec-to-zcash">
                    <div className="flex items-center space-x-2">
                      <span className="text-zypherpunk-accent">Aztec</span>
                      <ArrowRight className="w-4 h-4" />
                      <span className="text-zypherpunk-primary">Zcash</span>
                    </div>
                  </SelectItem>
                </SelectContent>
              </Select>
            </div>

            {/* Amount */}
            <div>
              <label className="text-sm text-zypherpunk-text-muted mb-2 block">
                Amount ({direction === 'zcash-to-aztec' ? 'ZEC' : 'AZTEC'})
              </label>
              <Input
                type="number"
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                placeholder="0.0"
                className="bg-zypherpunk-bg border-zypherpunk-border"
              />
              {sourceWallet && (
                <p className="text-xs text-zypherpunk-text-muted mt-1">
                  Balance: {formatBalance(sourceBalance)} {direction === 'zcash-to-aztec' ? 'ZEC' : 'AZTEC'}
                </p>
              )}
            </div>

            {/* Destination */}
            <div>
              <label className="text-sm text-zypherpunk-text-muted mb-2 block">
                Destination Address ({direction === 'zcash-to-aztec' ? 'Aztec' : 'Zcash'})
              </label>
              <Input
                value={destination}
                onChange={(e) => setDestination(e.target.value)}
                placeholder={direction === 'zcash-to-aztec' ? 'Aztec address' : 'Zcash shielded address'}
                className="bg-zypherpunk-bg border-zypherpunk-border"
              />
            </div>

            {/* Privacy Options */}
            <div className="space-y-2">
              <label className="text-sm text-zypherpunk-text-muted">Privacy Options</label>
              <div className="flex items-center space-x-2">
                <input
                  type="checkbox"
                  id="partialNotes"
                  checked={usePartialNotes}
                  onChange={(e) => setUsePartialNotes(e.target.checked)}
                  className="rounded border-zypherpunk-border"
                />
                <label htmlFor="partialNotes" className="text-sm">
                  Use Partial Notes (Enhanced Privacy)
                </label>
              </div>
              <div className="flex items-center space-x-2">
                <input
                  type="checkbox"
                  id="viewingKey"
                  checked={generateViewingKey}
                  onChange={(e) => setGenerateViewingKey(e.target.checked)}
                  className="rounded border-zypherpunk-border"
                />
                <label htmlFor="viewingKey" className="text-sm">
                  Generate Viewing Key (For Auditability)
                </label>
              </div>
            </div>

            {/* Bridge Button */}
            <Button
              onClick={handleBridge}
              disabled={!canBridge}
              className="w-full bg-zypherpunk-primary/20 hover:bg-zypherpunk-primary/30 border border-zypherpunk-primary/40 text-zypherpunk-primary font-semibold"
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                  Initiating Bridge...
                </>
              ) : (
                <>
                  <Shield className="w-4 h-4 mr-2" />
                  Initiate Bridge
                </>
              )}
            </Button>
          </div>
        </Card>

        {/* Bridge History */}
        <Card className="bg-zypherpunk-surface border-zypherpunk-border p-6">
          <h2 className="text-lg font-semibold mb-4">Bridge History</h2>
          {isLoadingHistory ? (
            <div className="text-center py-8">
              <Loader2 className="w-6 h-6 animate-spin mx-auto text-zypherpunk-primary" />
            </div>
          ) : bridgeHistory.length === 0 ? (
            <p className="text-center text-zypherpunk-text-muted py-8">No bridge transactions yet</p>
          ) : (
            <div className="space-y-3">
              {bridgeHistory.map((bridge) => (
                <div
                  key={bridge.bridgeId}
                  className="flex items-center justify-between p-3 bg-zypherpunk-bg rounded-lg border border-zypherpunk-border"
                >
                  <div className="flex items-center space-x-3">
                    {getStatusIcon(bridge.status)}
                    <div>
                      <p className="text-sm font-semibold">
                        {bridge.fromChain} → {bridge.toChain}
                      </p>
                      <p className="text-xs text-zypherpunk-text-muted">
                        {formatBalance(bridge.amount)} {bridge.fromChain === 'Zcash' ? 'ZEC' : 'AZTEC'}
                      </p>
                    </div>
                  </div>
                  <div className="text-right">
                    <p className="text-xs text-zypherpunk-text-muted capitalize">{bridge.status}</p>
                    <p className="text-xs text-zypherpunk-text-muted">
                      {new Date(bridge.createdAt).toLocaleDateString()}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>
      </div>
    </div>
  );
};

