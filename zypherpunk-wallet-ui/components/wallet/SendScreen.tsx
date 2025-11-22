'use client';

import React, { useState } from 'react';
import { ArrowLeft, ScanLine, Copy, Loader2, CheckCircle2, XCircle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { ProviderType, type Wallet } from '@/lib/types';
import { formatAddress, cn, validateAddress, validateAmount } from '@/lib/utils';
import { useWalletStore } from '@/lib/store';

interface SendScreenProps {
  wallet: Wallet;
  onBack: () => void;
  onSuccess?: () => void;
}

export const SendScreen: React.FC<SendScreenProps> = ({
  wallet,
  onBack,
  onSuccess,
}) => {
  const { sendTransaction, isLoading, error } = useWalletStore();
  const [recipient, setRecipient] = useState('');
  const [amount, setAmount] = useState('');
  const [memo, setMemo] = useState('');
  const [localError, setLocalError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [transactionHash, setTransactionHash] = useState<string | null>(null);

  const providerName = wallet.providerType.replace('OASIS', '');
  const tokenSymbol = providerName === 'Solana' ? 'SOL' : providerName === 'Ethereum' ? 'ETH' : providerName;

  const handleMax = () => {
    setAmount(wallet.balance.toString());
  };

  const handleSend = async () => {
    setLocalError(null);
    
    // Validation
    if (!validateAddress(recipient)) {
      setLocalError('Invalid wallet address');
      return;
    }

    const numAmount = parseFloat(amount);
    if (!validateAmount(amount) || numAmount <= 0) {
      setLocalError('Invalid amount');
      return;
    }

    if (numAmount > wallet.balance) {
      setLocalError('Insufficient balance');
      return;
    }

    try {
      await sendTransaction({
        fromWalletAddress: wallet.walletAddress,
        toWalletAddress: recipient,
        fromProviderType: wallet.providerType,
        toProviderType: wallet.providerType, // Same provider for now
        amount: numAmount,
        memoText: memo || undefined,
      });

      // Check for errors after the call - wait a bit for state to update
      await new Promise(resolve => setTimeout(resolve, 100));
      
      // Re-check error state
      const storeState = useWalletStore.getState();
      if (storeState.error) {
        setLocalError(storeState.error);
        return;
      }

      // Get the transaction from the store
      const latestTransaction = storeState.transactions[0];
      
      // Success
      setSuccess(true);
      setTransactionHash(latestTransaction?.transactionHash || null);
      
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
  const isValid = recipient.length > 0 && parseFloat(amount) > 0 && parseFloat(amount) <= wallet.balance && !isLoading;

  return (
    <div className="min-h-screen bg-black text-white flex flex-col">
      {/* Header */}
      <header className="px-4 pt-12 pb-4 border-b border-gray-800">
        <div className="flex items-center space-x-4">
          <button onClick={onBack} disabled={isLoading} className="p-2">
            <ArrowLeft className="w-6 h-6" />
          </button>
          <h1 className="text-xl font-semibold">Send {tokenSymbol}</h1>
        </div>
      </header>

      {/* Success Message */}
      {success && (
        <div className="mx-4 mt-4 p-4 bg-green-500/20 border border-green-500/30 rounded-lg">
          <div className="flex items-center space-x-2 text-green-400">
            <CheckCircle2 className="w-5 h-5" />
            <span className="font-semibold">Transaction sent successfully!</span>
          </div>
          {transactionHash && (
            <div className="mt-2 text-sm text-gray-400 font-mono break-all">
              Hash: {transactionHash}
            </div>
          )}
        </div>
      )}

      {/* Error Message */}
      {displayError && !success && (
        <div className="mx-4 mt-4 p-4 bg-red-500/20 border border-red-500/30 rounded-lg">
          <div className="flex items-center space-x-2 text-red-400">
            <XCircle className="w-5 h-5" />
            <span>{displayError}</span>
          </div>
        </div>
      )}

      {/* Content */}
      <div className="flex-1 px-4 py-8">
        {/* Provider Logo */}
        <div className="flex justify-center mb-8">
          <div className={cn(
            "w-24 h-24 rounded-full flex items-center justify-center",
            providerName === 'Solana' 
              ? "bg-gradient-to-br from-green-400 via-purple-500 to-pink-500"
              : "bg-gradient-to-br from-blue-500 to-purple-600"
          )}>
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

        {/* Recipient Address */}
        <div className="mb-6">
          <label className="block text-sm text-gray-400 mb-2">
            Recipient&rsquo;s {providerName} {providerName === 'Solana' ? 'Devnet ' : ''}address
          </label>
          <div className="relative">
            <Input
              type="text"
              value={recipient}
              onChange={(e) => {
                setRecipient(e.target.value);
                setLocalError(null);
              }}
              placeholder={`Enter ${providerName} address`}
              className="bg-gray-900 border-gray-800 text-white placeholder:text-gray-600 pr-12"
              disabled={isLoading || success}
            />
            <button 
              className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-white"
              disabled={isLoading || success}
            >
              <ScanLine className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Amount */}
        <div className="mb-6">
          <div className="flex items-center justify-between mb-2">
            <label className="block text-sm text-gray-400">Amount</label>
            <div className="flex items-center space-x-2">
              <span className="text-sm text-gray-400">{tokenSymbol}</span>
              <Button
                variant="outline"
                size="sm"
                onClick={handleMax}
                disabled={isLoading || success}
                className="h-7 px-3 text-xs bg-gray-800 border-gray-700 text-white hover:bg-gray-700"
              >
                Max
              </Button>
            </div>
          </div>
          <Input
            type="number"
            value={amount}
            onChange={(e) => {
              setAmount(e.target.value);
              setLocalError(null);
            }}
            placeholder="0"
            step="0.000001"
            min="0"
            max={wallet.balance}
            className="bg-gray-900 border-gray-800 text-white text-2xl placeholder:text-gray-600"
            disabled={isLoading || success}
          />
          <div className="mt-2 text-sm text-gray-400">
            Available: {wallet.balance.toFixed(6)} {tokenSymbol}
          </div>
        </div>

        {/* Memo (Optional) */}
        <div className="mb-6">
          <label className="block text-sm text-gray-400 mb-2">
            Memo (Optional)
          </label>
          <Input
            type="text"
            value={memo}
            onChange={(e) => setMemo(e.target.value)}
            placeholder="Add a note..."
            className="bg-gray-900 border-gray-800 text-white placeholder:text-gray-600"
            disabled={isLoading || success}
          />
        </div>

        {/* Transaction Summary */}
        {amount && parseFloat(amount) > 0 && !success && (
          <div className="mb-6 p-4 bg-gray-900 rounded-lg border border-gray-800">
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <span className="text-sm text-gray-400">Amount</span>
                <span className="text-sm font-semibold">
                  {parseFloat(amount).toFixed(6)} {tokenSymbol}
                </span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-sm text-gray-400">Network Fee</span>
                <span className="text-sm text-gray-300">~0.0001 {tokenSymbol}</span>
              </div>
              <div className="border-t border-gray-800 pt-2 flex items-center justify-between">
                <span className="text-sm font-semibold">Total</span>
                <span className="text-sm font-semibold">
                  {(parseFloat(amount) + 0.0001).toFixed(6)} {tokenSymbol}
                </span>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Bottom Actions */}
      <div className="px-4 pb-8 border-t border-gray-800 pt-4">
        <div className="flex space-x-4">
          <Button
            variant="outline"
            onClick={onBack}
            disabled={isLoading}
            className="flex-1 bg-gray-900 border-gray-800 text-white hover:bg-gray-800"
          >
            {success ? 'Close' : 'Cancel'}
          </Button>
          <Button
            onClick={handleSend}
            disabled={!isValid || isLoading || success}
            className={cn(
              "flex-1 bg-purple-600 text-white hover:bg-purple-700",
              (!isValid || isLoading || success) && "opacity-50 cursor-not-allowed"
            )}
          >
            {isLoading ? (
              <>
                <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                Sending...
              </>
            ) : success ? (
              <>
                <CheckCircle2 className="w-4 h-4 mr-2" />
                Sent
              </>
            ) : (
              'Send'
            )}
          </Button>
        </div>
      </div>
    </div>
  );
};

