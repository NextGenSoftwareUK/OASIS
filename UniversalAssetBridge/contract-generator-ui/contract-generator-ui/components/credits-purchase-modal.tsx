'use client';

import { useState, useEffect } from 'react';
import { Loader2, CheckCircle, XCircle, Sparkles, ExternalLink } from 'lucide-react';
import { 
  getCreditPacks, 
  purchaseCredits,
  calculateSavings,
  formatCredits,
  type CreditPack 
} from '@/lib/credits-client';
import { 
  getPhantomWallet, 
  isWalletInstalled,
  type WalletAdapter 
} from '@/lib/solana-wallet';

interface CreditsPurchaseModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: (newBalance: number) => void;
  currentBalance?: number;
}

export function CreditsPurchaseModal({ 
  isOpen, 
  onClose, 
  onSuccess,
  currentBalance = 0
}: CreditsPurchaseModalProps) {
  const [wallet, setWallet] = useState<WalletAdapter | null>(null);
  const [connected, setConnected] = useState(false);
  const [packs, setPacks] = useState<CreditPack[]>([]);
  const [selectedPack, setSelectedPack] = useState<CreditPack | null>(null);
  const [status, setStatus] = useState<'idle' | 'purchasing' | 'success' | 'error'>('idle');
  const [error, setError] = useState<string>('');
  const [purchaseSignature, setPurchaseSignature] = useState<string>('');

  useEffect(() => {
    if (isOpen) {
      getCreditPacks().then(setPacks);
      
      const phantomWallet = getPhantomWallet();
      if (phantomWallet && phantomWallet.connected) {
        setWallet(phantomWallet);
        setConnected(true);
      }
    }
  }, [isOpen]);

  const handleConnectWallet = async () => {
    if (!isWalletInstalled()) {
      window.open('https://phantom.app/', '_blank');
      return;
    }

    try {
      const phantomWallet = getPhantomWallet();
      if (!phantomWallet) {
        throw new Error('Phantom wallet not found');
      }

      await phantomWallet.connect();
      setWallet(phantomWallet);
      setConnected(true);
    } catch (err: any) {
      setError(err.message || 'Failed to connect wallet');
    }
  };

  const handlePurchase = async (pack: CreditPack) => {
    if (!wallet || !connected) {
      setError('Please connect your wallet first');
      return;
    }

    setStatus('purchasing');
    setError('');
    setSelectedPack(pack);

    try {
      const result = await purchaseCredits(wallet, pack);
      
      if (!result.success) {
        throw new Error(result.error || 'Purchase failed');
      }

      setPurchaseSignature(result.transactionSignature);
      setStatus('success');
      
      setTimeout(() => {
        onSuccess(result.newBalance);
        onClose();
      }, 3000);
    } catch (err: any) {
      setError(err.message || 'Purchase failed');
      setStatus('error');
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-md">
      <div className="glass-card rounded-2xl border border-[var(--card-border)]/60 bg-[rgba(7,10,26,0.95)] shadow-2xl shadow-[var(--accent)]/20 max-w-3xl w-full p-8 max-h-[90vh] overflow-y-auto scrollbar-thin">
        {/* Header */}
        <div className="flex items-center justify-between mb-8">
          <div>
            <h2 className="text-3xl font-bold flex items-center gap-3 text-[var(--foreground)]">
              <div className="p-2 rounded-lg bg-gradient-to-br from-purple-500/20 to-pink-500/20">
                <Sparkles className="w-7 h-7 text-[var(--accent)]" />
              </div>
              Purchase Credits
            </h2>
            <p className="text-base text-[var(--muted)] mt-2 ml-[52px]">
              Buy credit packs to use across multiple operations
            </p>
          </div>
          <button
            onClick={onClose}
            className="text-[var(--muted)] hover:text-[var(--foreground)] transition-colors text-2xl"
          >
            ✕
          </button>
        </div>

        {/* Current Balance */}
        {currentBalance > 0 && (
          <div className="mb-8 p-4 rounded-xl border border-[var(--accent)]/30 bg-[var(--accent-soft)] backdrop-blur-sm">
            <div className="flex items-center justify-between">
              <span className="text-sm text-[var(--foreground)] font-semibold">
                Current Balance
              </span>
              <span className="text-xl font-bold text-[var(--accent)]">
                {formatCredits(currentBalance)}
              </span>
            </div>
          </div>
        )}

        {/* Wallet Connection */}
        {!connected && (
          <div className="mb-8">
            <button
              onClick={handleConnectWallet}
              className="w-full flex items-center justify-center gap-2 bg-[var(--accent)] hover:bg-[var(--accent)]/80 text-[#041321] font-semibold py-4 px-6 rounded-xl transition-all shadow-lg shadow-[var(--accent)]/30"
            >
              {isWalletInstalled() ? 'Connect Phantom Wallet' : 'Install Phantom Wallet'}
            </button>
          </div>
        )}

        {/* Credit Packs Grid */}
        {connected && status !== 'success' && (
          <div className="grid md:grid-cols-2 gap-4 mb-6">
            {packs.map((pack) => {
              const savings = calculateSavings(pack);
              const isSelected = selectedPack?.name === pack.name && status === 'purchasing';
              
              return (
                <button
                  key={pack.name}
                  onClick={() => handlePurchase(pack)}
                  disabled={status === 'purchasing'}
                  className={`relative glass-card rounded-xl p-6 transition-all overflow-hidden cursor-pointer text-left w-full ${
                    isSelected
                      ? 'border-2 border-[var(--accent)] bg-[rgba(8,11,26,0.9)] gradient-ring scale-[1.02]'
                      : 'border border-[var(--card-border)]/40 bg-[rgba(8,11,26,0.7)] hover:border-[var(--accent)]/60 hover:bg-[rgba(8,11,26,0.85)] hover:scale-[1.01]'
                  } disabled:opacity-50`}
                >
                  {/* Gradient background */}
                  <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_left,rgba(34,211,238,0.12),transparent_70%)]" />
                  
                  <div className="relative">
                    <div className="text-center mb-6">
                      <h3 className="text-xl font-bold mb-3 text-[var(--foreground)]">{pack.name}</h3>
                      <div className="text-5xl font-bold text-[var(--accent)] mb-2">
                        {pack.credits}
                      </div>
                      <div className="text-sm text-[var(--muted)]">credits</div>
                    </div>

                    <div className="space-y-3 mb-6 text-sm p-4 rounded-lg bg-[rgba(5,8,18,0.6)] border border-[var(--card-border)]/20">
                      <div className="flex justify-between">
                        <span className="text-[var(--muted)]">Price</span>
                        <span className="font-semibold text-[var(--foreground)]">{pack.priceSOL} SOL</span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-[var(--muted)]">Per credit</span>
                        <span className="font-semibold text-[var(--foreground)]">{(pack.priceSOL / pack.credits).toFixed(4)} SOL</span>
                      </div>
                      <div className="flex justify-between pt-2 border-t border-[var(--card-border)]/20">
                        <span className="text-[var(--positive)] font-semibold">Save</span>
                        <span className="font-bold text-[var(--positive)]">{savings.savingsPercent.toFixed(0)}%</span>
                      </div>
                    </div>

                    <div className="mt-auto pt-4">
                      <div className={`py-3 px-4 rounded-xl font-semibold transition-all text-center ${
                        isSelected
                          ? 'bg-[var(--accent)] text-[#041321]'
                          : 'bg-[var(--accent)]/80 text-[#041321]'
                      } flex items-center justify-center gap-2`}
                      >
                        {isSelected ? (
                          <>
                            <Loader2 className="w-5 h-5 animate-spin" />
                            Processing...
                          </>
                        ) : (
                          <>Select {pack.credits} Credits</>
                        )}
                      </div>
                    </div>

                    {/* Usage Examples */}
                    <div className="mt-4 pt-4 border-t border-[var(--card-border)]/30">
                      <div className="text-xs text-[var(--muted)] space-y-1">
                        <div className="font-semibold mb-2 text-[var(--foreground)]">Good for:</div>
                      {pack.name === 'Starter' && (
                        <>
                          <div>• 5 Rust generations</div>
                          <div>• 2 compilations</div>
                        </>
                      )}
                      {pack.name === 'Developer' && (
                        <>
                          <div>• 25 Rust generations</div>
                          <div>• 3 compilations + deploys</div>
                        </>
                      )}
                      {pack.name === 'Professional' && (
                        <>
                          <div>• 50 Rust generations</div>
                          <div>• 6+ full workflows</div>
                        </>
                      )}
                      {pack.name === 'Enterprise' && (
                        <>
                          <div>• 250 Rust generations</div>
                          <div>• 30+ full workflows</div>
                        </>
                      )}
                      </div>
                    </div>
                  </div>
                </button>
              );
            })}
          </div>
        )}

        {/* Success Message */}
        {status === 'success' && selectedPack && (
          <div className="p-6 rounded-xl border border-[var(--positive)]/40 bg-[rgba(34,197,94,0.1)] backdrop-blur-sm">
            <div className="flex items-start gap-4">
              <div className="p-2 rounded-lg bg-[var(--positive)]/20">
                <CheckCircle className="w-6 h-6 text-[var(--positive)]" />
              </div>
              <div className="flex-1">
                <h3 className="font-bold text-[var(--foreground)] text-lg mb-2">
                  ✅ Credits Purchased Successfully!
                </h3>
                <p className="text-[var(--positive)] mb-3 font-semibold">
                  +{selectedPack.credits} credits added to your account
                </p>
                <a
                  href={`https://explorer.solana.com/tx/${purchaseSignature}?cluster=devnet`}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-sm text-[var(--accent)] hover:text-[var(--accent)]/80 hover:underline flex items-center gap-1 transition-colors"
                >
                  View transaction on Solana Explorer <ExternalLink className="w-3 h-3" />
                </a>
              </div>
            </div>
          </div>
        )}

        {/* Error Message */}
        {error && (
          <div className="p-4 rounded-xl border border-[var(--negative)]/40 bg-[rgba(239,68,68,0.1)] backdrop-blur-sm">
            <div className="flex items-start gap-3">
              <div className="p-2 rounded-lg bg-[var(--negative)]/20">
                <XCircle className="w-5 h-5 text-[var(--negative)]" />
              </div>
              <div>
                <p className="font-semibold text-[var(--foreground)]">Purchase Failed</p>
                <p className="text-sm text-[var(--negative)] mt-1">{error}</p>
              </div>
            </div>
          </div>
        )}

        {/* Info Footer */}
        <div className="mt-8 pt-6 border-t border-[var(--card-border)]/30">
          <div className="grid md:grid-cols-2 gap-6 text-sm">
            <div className="p-4 rounded-lg bg-[rgba(5,8,18,0.6)] border border-[var(--card-border)]/20">
              <div className="font-semibold mb-2 text-[var(--accent)] flex items-center gap-2">
                💰 What are credits?
              </div>
              <div className="text-[var(--muted)]">Credits let you use the API without paying for each operation. Buy once, use many times!</div>
            </div>
            <div className="p-4 rounded-lg bg-[rgba(5,8,18,0.6)] border border-[var(--card-border)]/20">
              <div className="font-semibold mb-2 text-[var(--accent)] flex items-center gap-2">
                💎 Revenue sharing
              </div>
              <div className="text-[var(--muted)]">90% of all credit purchases are distributed to NFT holders automatically via x402.</div>
            </div>
          </div>
          
          <div className="mt-4 text-center text-xs text-[var(--muted)]">
            Powered by x402 protocol on Solana • Devnet for testing
          </div>
        </div>
      </div>
    </div>
  );
}

