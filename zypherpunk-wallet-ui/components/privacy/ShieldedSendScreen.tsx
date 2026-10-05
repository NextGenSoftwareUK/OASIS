'use client';

import React, { useState } from 'react';
import { ArrowLeft, Shield, Lock, Eye, AlertCircle, CheckCircle2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card } from '@/components/ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { PrivacyIndicator, PrivacyBadge } from './PrivacyIndicator';
import { privacyAPI } from '@/lib/api/privacyApi';
import { validateShieldedAddress, getRecommendedPrivacyLevel, shouldUsePartialNotes } from '@/lib/privacy/shieldedTx';
import { useWalletStore } from '@/lib/store';
import { formatBalance } from '@/lib/utils';
import type { Wallet } from '@/lib/types';

interface ShieldedSendScreenProps {
  wallet: Wallet;
  onBack: () => void;
  onSuccess?: () => void;
}

export const ShieldedSendScreen: React.FC<ShieldedSendScreenProps> = ({
  wallet,
  onBack,
  onSuccess,
}) => {
  const { sendTransaction, isLoading, error } = useWalletStore();
  const [recipient, setRecipient] = useState('');
  const [amount, setAmount] = useState('');
  const [memo, setMemo] = useState('');
  const [privacyLevel, setPrivacyLevel] = useState<'low' | 'medium' | 'high' | 'maximum'>('high');
  const [usePartialNotes, setUsePartialNotes] = useState(false);
  const [generateViewingKey, setGenerateViewingKey] = useState(true);
  const [localError, setLocalError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [transactionHash, setTransactionHash] = useState<string | null>(null);

  const numAmount = parseFloat(amount) || 0;
  const isValidAmount = numAmount > 0 && numAmount <= (wallet.balance || 0);
  const isValidAddress = recipient.length > 0 && validateShieldedAddress(recipient);
  const canSend = isValidAmount && isValidAddress && !isLoading;

  // Auto-adjust privacy level based on amount
  React.useEffect(() => {
    if (numAmount > 0) {
      const recommended = getRecommendedPrivacyLevel(numAmount);
      setPrivacyLevel(recommended);
      setUsePartialNotes(shouldUsePartialNotes(numAmount, recommended));
    }
  }, [numAmount]);

  const handleMax = () => {
    setAmount(wallet.balance.toString());
  };

  const handleSend = async () => {
    setLocalError(null);

    if (!validateShieldedAddress(recipient)) {
      setLocalError('Invalid shielded address. Must be a Zcash z-address (starts with z or zt)');
      return;
    }

    if (!isValidAmount) {
      setLocalError('Invalid amount');
      return;
    }

    try {
      // Use privacy API for shielded transactions
      const result = await privacyAPI.createShieldedTransaction({
        fromWalletAddress: wallet.walletAddress,
        toWalletAddress: recipient,
        fromProviderType: wallet.providerType,
        toProviderType: wallet.providerType,
        amount: numAmount,
        memoText: memo || undefined,
        privacyLevel,
        usePartialNotes,
        generateViewingKey,
      });

      if (result.isError) {
        setLocalError(result.message || 'Failed to send shielded transaction');
        return;
      }

      // Success
      setSuccess(true);
      setTransactionHash(result.result?.transactionHash || null);

      // Reset form after delay
      setTimeout(() => {
        setRecipient('');
        setAmount('');
        setMemo('');
        setSuccess(false);
        onSuccess?.();
      }, 3000);
    } catch (err) {
      setLocalError(err instanceof Error ? err.message : 'Failed to send transaction');
    }
  };

  const displayError = localError || error;

  return (
    <div className="min-h-screen bg-zypherpunk-bg text-zypherpunk-text p-4">
      {/* Header */}
      <div className="flex items-center gap-4 mb-6">
        <Button
          variant="ghost"
          size="icon"
          onClick={onBack}
          className="text-zypherpunk-text hover:bg-zypherpunk-surface"
        >
          <ArrowLeft className="w-5 h-5" />
        </Button>
        <div className="flex items-center gap-2">
          <Shield className="w-6 h-6 text-zypherpunk-shielded" />
          <h1 className="text-2xl font-bold">Shielded Transaction</h1>
        </div>
      </div>

      {success ? (
        <Card className="bg-zypherpunk-surface border-zypherpunk-border p-6 text-center">
          <CheckCircle2 className="w-16 h-16 text-zypherpunk-shielded mx-auto mb-4" />
          <h2 className="text-2xl font-bold text-zypherpunk-text mb-2">Transaction Sent!</h2>
          <p className="text-zypherpunk-text-muted mb-4">
            Your shielded transaction has been submitted
          </p>
          {transactionHash && (
            <p className="text-sm text-zypherpunk-accent font-mono break-all">
              {transactionHash}
            </p>
          )}
        </Card>
      ) : (
        <div className="space-y-6">
          {/* Privacy Level Selector */}
          <Card className="bg-zypherpunk-surface border-zypherpunk-border p-4">
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-2">
                <Lock className="w-5 h-5 text-zypherpunk-accent" />
                <span className="font-semibold">Privacy Level</span>
              </div>
              <PrivacyBadge level={privacyLevel} />
            </div>
            <Select
              value={privacyLevel}
              onValueChange={(value) => setPrivacyLevel(value as typeof privacyLevel)}
            >
              <SelectTrigger className="bg-zypherpunk-bg border-zypherpunk-border text-zypherpunk-text">
                <SelectValue />
              </SelectTrigger>
              <SelectContent className="bg-zypherpunk-surface border-zypherpunk-border">
                <SelectItem value="low">Low - Standard privacy</SelectItem>
                <SelectItem value="medium">Medium - Enhanced privacy</SelectItem>
                <SelectItem value="high">High - Maximum privacy</SelectItem>
                <SelectItem value="maximum">Maximum - Ultimate privacy</SelectItem>
              </SelectContent>
            </Select>
            <p className="text-xs text-zypherpunk-text-muted mt-2">
              {privacyLevel === 'maximum' && 'Uses 5 partial notes for maximum obfuscation'}
              {privacyLevel === 'high' && 'Uses 3 partial notes for high privacy'}
              {privacyLevel === 'medium' && 'Uses 2 partial notes for enhanced privacy'}
              {privacyLevel === 'low' && 'Standard shielded transaction'}
            </p>
          </Card>

          {/* Recipient Address */}
          <Card className="bg-zypherpunk-surface border-zypherpunk-border p-4">
            <label className="text-sm font-medium text-zypherpunk-text-muted mb-2 block">
              Shielded Recipient Address (z-address)
            </label>
            <Input
              value={recipient}
              onChange={(e) => setRecipient(e.target.value)}
              placeholder="zt1..."
              className="bg-zypherpunk-bg border-zypherpunk-border text-zypherpunk-text font-mono"
            />
            {recipient && !isValidAddress && (
              <div className="flex items-center gap-2 mt-2 text-zypherpunk-warning text-sm">
                <AlertCircle className="w-4 h-4" />
                <span>Invalid shielded address format</span>
              </div>
            )}
            {isValidAddress && (
              <div className="flex items-center gap-2 mt-2 text-zypherpunk-shielded text-sm">
                <Shield className="w-4 h-4" />
                <span>Valid shielded address</span>
              </div>
            )}
          </Card>

          {/* Amount */}
          <Card className="bg-zypherpunk-surface border-zypherpunk-border p-4">
            <div className="flex items-center justify-between mb-2">
              <label className="text-sm font-medium text-zypherpunk-text-muted">
                Amount (ZEC)
              </label>
              <button
                onClick={handleMax}
                className="text-xs text-zypherpunk-accent hover:underline"
              >
                Max
              </button>
            </div>
            <Input
              type="number"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              placeholder="0.0"
              className="bg-zypherpunk-bg border-zypherpunk-border text-zypherpunk-text text-2xl"
            />
            <div className="flex items-center justify-between mt-2">
              <span className="text-xs text-zypherpunk-text-muted">
                Balance: {formatBalance(wallet.balance || 0)} ZEC
              </span>
              {numAmount > 0 && (
                <span className="text-xs text-zypherpunk-text-muted">
                  ≈ ${(numAmount * 1800).toFixed(2)} USD
                </span>
              )}
            </div>
          </Card>

          {/* Memo (Optional) */}
          <Card className="bg-zypherpunk-surface border-zypherpunk-border p-4">
            <label className="text-sm font-medium text-zypherpunk-text-muted mb-2 block">
              Memo (Optional, Encrypted)
            </label>
            <Input
              value={memo}
              onChange={(e) => setMemo(e.target.value.substring(0, 500))}
              placeholder="Private message (encrypted)"
              className="bg-zypherpunk-bg border-zypherpunk-border text-zypherpunk-text"
              maxLength={500}
            />
            <div className="flex items-center gap-2 mt-2">
              <Lock className="w-3 h-3 text-zypherpunk-accent" />
              <span className="text-xs text-zypherpunk-text-muted">
                Memo will be encrypted in the transaction
              </span>
            </div>
          </Card>

          {/* Privacy Options */}
          <Card className="bg-zypherpunk-surface border-zypherpunk-border p-4">
            <h3 className="font-semibold mb-3">Privacy Options</h3>
            <div className="space-y-3">
              <label className="flex items-center gap-3 cursor-pointer">
                <input
                  type="checkbox"
                  checked={usePartialNotes}
                  onChange={(e) => setUsePartialNotes(e.target.checked)}
                  className="w-4 h-4 rounded border-zypherpunk-border bg-zypherpunk-bg"
                />
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <Shield className="w-4 h-4 text-zypherpunk-shielded" />
                    <span className="text-sm font-medium">Use Partial Notes</span>
                  </div>
                  <p className="text-xs text-zypherpunk-text-muted">
                    Split transaction into multiple notes for enhanced privacy
                  </p>
                </div>
              </label>

              <label className="flex items-center gap-3 cursor-pointer">
                <input
                  type="checkbox"
                  checked={generateViewingKey}
                  onChange={(e) => setGenerateViewingKey(e.target.checked)}
                  className="w-4 h-4 rounded border-zypherpunk-border bg-zypherpunk-bg"
                />
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <Eye className="w-4 h-4 text-zypherpunk-accent" />
                    <span className="text-sm font-medium">Generate Viewing Key</span>
                  </div>
                  <p className="text-xs text-zypherpunk-text-muted">
                    Create viewing key for auditability (compliance)
                  </p>
                </div>
              </label>
            </div>
          </Card>

          {/* Error Display */}
          {displayError && (
            <Card className="bg-red-500/10 border-red-500/50 p-4">
              <div className="flex items-center gap-2 text-red-400">
                <AlertCircle className="w-5 h-5" />
                <span>{displayError}</span>
              </div>
            </Card>
          )}

          {/* Send Button */}
          <Button
            onClick={handleSend}
            disabled={!canSend}
            className="w-full bg-zypherpunk-primary/20 hover:bg-zypherpunk-primary/30 border border-zypherpunk-primary/40 text-zypherpunk-primary font-semibold py-6 text-lg disabled:opacity-50"
          >
            {isLoading ? (
              <div className="flex items-center gap-2">
                <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-black"></div>
                <span>Sending...</span>
              </div>
            ) : (
              <div className="flex items-center gap-2">
                <Shield className="w-5 h-5" />
                <span>Send Shielded Transaction</span>
              </div>
            )}
          </Button>
        </div>
      )}
    </div>
  );
};

