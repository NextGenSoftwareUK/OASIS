'use client';

import React, { useState } from 'react';
import Image from 'next/image';
import { ArrowLeft, Copy, Check, Share2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { ProviderType } from '@/lib/types';
import { formatAddress } from '@/lib/utils';

interface ReceiveScreenProps {
  providerType: ProviderType;
  walletAddress: string;
  onBack: () => void;
}

export const ReceiveScreen: React.FC<ReceiveScreenProps> = ({
  providerType,
  walletAddress,
  onBack,
}) => {
  const [copied, setCopied] = useState(false);

  const providerName = providerType.replace('OASIS', '');
  const tokenSymbol = providerName === 'Solana' ? 'SOL' : providerName === 'Ethereum' ? 'ETH' : providerName;

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(walletAddress);
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
          title: `My ${providerName} Address`,
          text: walletAddress,
        });
      } catch (error) {
        console.error('Failed to share:', error);
      }
    }
  };

  // Generate QR code data URL (simplified - in production use a QR library)
  const qrCodeData = `https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=${encodeURIComponent(walletAddress)}`;

  return (
    <div className="min-h-screen bg-black text-white flex flex-col">
      {/* Header */}
      <header className="px-4 pt-12 pb-4 border-b border-gray-800">
        <div className="flex items-center space-x-4">
          <button onClick={onBack} className="p-2">
            <ArrowLeft className="w-6 h-6" />
          </button>
          <h1 className="text-xl font-semibold">Receive {tokenSymbol}</h1>
        </div>
      </header>

      {/* Content */}
      <div className="flex-1 px-4 py-8">
        <div className="max-w-sm mx-auto">
          {/* QR Code */}
          <Card className="bg-white p-6 mb-6">
            <div className="flex justify-center">
              <Image
                src={qrCodeData}
                alt="QR Code"
                width={256}
                height={256}
                className="w-64 h-64"
              />
            </div>
          </Card>

          {/* Wallet Address */}
          <div className="mb-6">
            <label className="block text-sm text-gray-400 mb-2">
              Your {providerName} Address
            </label>
            <div className="flex items-center space-x-2 p-4 bg-gray-900 rounded-lg border border-gray-800">
              <span className="flex-1 font-mono text-sm break-all">
                {walletAddress}
              </span>
              <button
                onClick={handleCopy}
                className="p-2 hover:bg-gray-800 rounded transition-colors"
              >
                {copied ? (
                  <Check className="w-5 h-5 text-green-400" />
                ) : (
                  <Copy className="w-5 h-5 text-gray-400" />
                )}
              </button>
            </div>
          </div>

          {/* Info */}
          <div className="mb-6 p-4 bg-blue-500/10 border border-blue-500/20 rounded-lg">
            <div className="text-sm text-blue-400">
              <p className="mb-2">• Only send {tokenSymbol} to this address</p>
              <p className="mb-2">• Sending other tokens may result in permanent loss</p>
              <p>• Double-check the address before sending</p>
            </div>
          </div>

          {/* Actions */}
          <div className="space-y-3">
            <Button
              onClick={handleCopy}
              className="w-full bg-purple-600 text-white hover:bg-purple-700"
            >
              {copied ? (
                <>
                  <Check className="w-5 h-5 mr-2" />
                  Copied!
                </>
              ) : (
                <>
                  <Copy className="w-5 h-5 mr-2" />
                  Copy Address
                </>
              )}
            </Button>
            {navigator.share && (
              <Button
                variant="outline"
                onClick={handleShare}
                className="w-full bg-gray-900 border-gray-800 text-white hover:bg-gray-800"
              >
                <Share2 className="w-5 h-5 mr-2" />
                Share Address
              </Button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

