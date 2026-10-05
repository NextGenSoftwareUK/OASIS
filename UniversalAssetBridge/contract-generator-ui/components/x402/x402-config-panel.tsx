/**
 * x402 Configuration Panel
 * 
 * Allows users to enable and configure x402 revenue distribution for their NFTs
 */

"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import type { X402Config, RevenueModel } from "@/types/x402";
import { REVENUE_MODELS, CONTENT_TYPES, DISTRIBUTION_FREQUENCIES } from "@/types/x402";

type X402ConfigPanelProps = {
  config: X402Config;
  onChange: (config: X402Config) => void;
};

export function X402ConfigPanel({ config, onChange }: X402ConfigPanelProps) {
  const [showAdvanced, setShowAdvanced] = useState(false);

  const updateConfig = (updates: Partial<X402Config>) => {
    onChange({ ...config, ...updates });
  };

  const updateMetadata = (updates: Partial<X402Config['metadata']>) => {
    onChange({
      ...config,
      metadata: { ...config.metadata, ...updates }
    });
  };

  return (
    <div className="space-y-6">
      <style jsx>{`
        @keyframes expandDown {
          from {
            opacity: 0;
            transform: translateY(-20px) scale(0.95);
            max-height: 0;
          }
          to {
            opacity: 1;
            transform: translateY(0) scale(1);
            max-height: 3000px;
          }
        }
        
        @keyframes gradientFlow {
          0% {
            background-position: 0% 50%;
          }
          50% {
            background-position: 100% 50%;
          }
          100% {
            background-position: 0% 50%;
          }
        }
        
        @keyframes fadeIn {
          from {
            opacity: 0;
          }
          to {
            opacity: 1;
          }
        }
        
        @keyframes fadeInUp {
          from {
            opacity: 0;
            transform: translateY(20px);
          }
          to {
            opacity: 1;
            transform: translateY(0);
          }
        }
        
        @keyframes scaleIn {
          from {
            transform: scale(0.95);
          }
          to {
            transform: scale(1);
          }
        }
      `}</style>
      {/* Enable Toggle */}
      <div className="rounded-2xl border border-[var(--color-card-border)]/60 bg-[rgba(8,12,28,0.85)] p-6">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <svg 
              className="w-8 h-8 text-[var(--accent)]" 
              viewBox="0 0 24 24" 
              fill="none" 
              stroke="currentColor" 
              strokeWidth="2"
            >
              <path d="M12 2v20M17 5H9.5a3.5 3.5 0 000 7h5a3.5 3.5 0 010 7H6" />
            </svg>
            <div>
              <h3 className="text-xl font-semibold text-[var(--color-foreground)]">
                Enable x402 Revenue Sharing
              </h3>
              <p className="mt-2 text-sm text-[var(--muted)]">
                Automatically distribute payments to all NFT holders when revenue is generated
              </p>
            </div>
          </div>
          <Button
            variant="toggle"
            data-state={config.enabled ? "active" : undefined}
            onClick={() => updateConfig({ enabled: !config.enabled })}
            className="px-6 py-3 text-base"
          >
            {config.enabled ? "Enabled" : "Disabled"}
          </Button>
        </div>
      </div>

      {config.enabled && (
        <div 
          className="animate-in slide-in-from-top-4 fade-in duration-500"
          style={{
            animation: 'expandDown 0.5s cubic-bezier(0.16, 1, 0.3, 1)',
          }}
        >
          {/* Revenue Model Selection */}
          <div className="space-y-4">
            <div>
              <h3 className="text-lg font-semibold text-[var(--color-foreground)]">
                Distribution Model
              </h3>
              <p className="mt-1 text-sm text-[var(--muted)]">
                Choose how revenue should be distributed to NFT holders
              </p>
            </div>

            <div 
              className="grid gap-4 sm:grid-cols-3"
              style={{
                animation: 'fadeIn 0.6s ease-out 0.1s both'
              }}
            >
              {(Object.entries(REVENUE_MODELS) as [RevenueModel, typeof REVENUE_MODELS[RevenueModel]][]).map(
                ([key, model], index) => {
                  const isSelected = config.revenueModel === key;
                  return (
                  <button
                    key={key}
                    type="button"
                    onClick={() => updateConfig({ revenueModel: key })}
                    className={cn(
                      "relative overflow-hidden rounded-2xl border border-[var(--color-card-border)]/60 p-5 text-left transition-all duration-500",
                      isSelected
                        ? "border-[var(--accent)]/80 ring-2 ring-[var(--accent)]/50"
                        : "hover:border-[var(--accent)]/50 hover:transform hover:scale-105"
                    )}
                    style={{
                      animation: isSelected
                        ? `fadeInUp 0.5s ease-out ${0.1 + index * 0.1}s both, scaleIn 0.4s cubic-bezier(0.16, 1, 0.3, 1)`
                        : `fadeInUp 0.5s ease-out ${0.1 + index * 0.1}s both`,
                      boxShadow: isSelected
                        ? '0 20px 50px rgba(34,211,238,0.25)'
                        : 'none'
                    }}
                  >
                    <div 
                      className="absolute inset-0 opacity-50"
                      style={{
                        background: isSelected
                          ? 'radial-gradient(circle at top, rgba(34,211,238,0.25), transparent 70%)'
                          : 'radial-gradient(circle at top, rgba(34,211,238,0.15), transparent 60%)',
                        transition: 'all 0.5s cubic-bezier(0.16, 1, 0.3, 1)'
                      }}
                    />
                    <div className="relative">
                      <div className="mb-3">
                    {key === 'equal' && (
                      <svg className="w-10 h-10 text-[var(--accent)]" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                        <line x1="5" y1="9" x2="19" y2="9" />
                        <line x1="5" y1="15" x2="19" y2="15" />
                      </svg>
                    )}
                    {key === 'weighted' && (
                      <svg className="w-10 h-10 text-[var(--accent)]" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                        <line x1="12" y1="20" x2="12" y2="10" />
                        <line x1="18" y1="20" x2="18" y2="4" />
                        <line x1="6" y1="20" x2="6" y2="16" />
                      </svg>
                    )}
                    {key === 'creator-split' && (
                      <svg className="w-10 h-10 text-[var(--accent)]" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                        <circle cx="9" cy="12" r="7" />
                        <polyline points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77" />
                      </svg>
                    )}
                  </div>
                      <h4 className="text-base font-semibold text-[var(--color-foreground)]">
                        {model.label}
                      </h4>
                      <p className="mt-2 text-xs text-[var(--muted)]">
                        {model.description}
                      </p>
                    </div>
                  </button>
                  );
                }
              )}
            </div>

            {/* Creator Split Percentage (only show for creator-split model) */}
            {config.revenueModel === 'creator-split' && (
              <div className="rounded-xl border border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.7)] p-4">
                <label className="block text-sm text-[var(--color-foreground)] mb-2">
                  Creator Share Percentage
                </label>
                <div className="flex items-center gap-4">
                  <input
                    type="range"
                    min="0"
                    max="100"
                    step="5"
                    value={config.metadata?.creatorSplitPercentage || 50}
                    onChange={(e) => updateMetadata({ creatorSplitPercentage: Number(e.target.value) })}
                    className="flex-1"
                  />
                  <span className="text-2xl font-bold text-[var(--accent)] w-16 text-right">
                    {config.metadata?.creatorSplitPercentage || 50}%
                  </span>
                </div>
                <p className="mt-2 text-xs text-[var(--muted)]">
                  Creator receives {config.metadata?.creatorSplitPercentage || 50}%, 
                  holders split remaining {100 - (config.metadata?.creatorSplitPercentage || 50)}%
                </p>
              </div>
            )}
          </div>

          {/* Payment Endpoint */}
          <div 
            className="space-y-3"
            style={{
              animation: 'fadeIn 0.6s ease-out 0.3s both'
            }}
          >
            <div>
              <label className="block text-sm font-semibold text-[var(--color-foreground)] mb-2">
                x402 Payment Endpoint URL
              </label>
              <input
                type="url"
                value={config.paymentEndpoint}
                onChange={(e) => updateConfig({ paymentEndpoint: e.target.value })}
                placeholder="https://api.yourservice.com/x402/revenue"
                className="w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-4 py-3 text-sm text-[var(--color-foreground)] placeholder:text-[var(--muted)]/50 focus:border-[var(--accent)] focus:outline-none"
              />
              <p className="mt-2 text-xs text-[var(--muted)]">
                This is the webhook URL where revenue sources will send payments. 
                Each payment triggers automatic distribution to all NFT holders.
              </p>
            </div>

            {/* Auto-generate endpoint option */}
            <Button
              variant="secondary"
              onClick={() => {
                const generatedUrl = `https://api.oasis.one/x402/revenue/${Date.now()}`;
                updateConfig({ paymentEndpoint: generatedUrl });
              }}
              className="text-xs"
            >
              Auto-generate OASIS endpoint
            </Button>
          </div>

          {/* Treasury Wallet (User's Wallet for Distributions) */}
          <div 
            className="space-y-3"
            style={{
              animation: 'fadeIn 0.6s ease-out 0.4s both'
            }}
          >
            <div>
              <div className="flex items-center gap-2 mb-2">
                <svg 
                  className="w-5 h-5 text-[var(--accent)]" 
                  viewBox="0 0 24 24" 
                  fill="none" 
                  stroke="currentColor" 
                  strokeWidth="2"
                >
                  <rect x="2" y="7" width="20" height="14" rx="2" ry="2" />
                  <path d="M16 21V5a2 2 0 00-2-2h-4a2 2 0 00-2 2v16" />
                </svg>
                <label className="text-sm font-semibold text-[var(--color-foreground)]">
                  Treasury Wallet (Optional)
                </label>
              </div>
              <input
                type="text"
                value={config.treasuryWallet || ''}
                onChange={(e) => updateConfig({ treasuryWallet: e.target.value })}
                placeholder="Enter your Solana wallet address"
                className="w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-4 py-3 text-sm text-[var(--color-foreground)] placeholder:text-[var(--muted)]/50 focus:border-[var(--accent)] focus:outline-none font-mono text-xs"
              />
              <p className="mt-2 text-xs text-[var(--muted)]">
                Revenue will be sent to this wallet. Distributions will be made FROM this wallet.
                Leave empty to use OASIS Web4 platform treasury.
              </p>
            </div>

            {/* Use Connected Wallet Button */}
            <div className="flex gap-3">
              <Button
                variant="primary"
                onClick={async () => {
                  try {
                    // Check if Phantom wallet is connected
                    if (typeof window !== 'undefined' && (window as any).solana) {
                      const solana = (window as any).solana;
                      
                      // If not connected, request connection
                      if (!solana.isConnected) {
                        await solana.connect();
                      }
                      
                      // Get public key
                      const walletAddress = solana.publicKey?.toBase58();
                      
                      if (walletAddress) {
                        updateConfig({ treasuryWallet: walletAddress });
                      } else {
                        alert('Unable to get wallet address. Please connect your Phantom wallet.');
                      }
                    } else {
                      alert('Phantom wallet not detected. Please install Phantom to use this feature.');
                    }
                  } catch (error) {
                    console.error('Error connecting to Phantom:', error);
                    alert('Failed to connect wallet. Please try manually entering your address.');
                  }
                }}
                className="flex-1 px-6 py-4 text-base font-semibold"
              >
                <svg 
                  className="inline-block w-5 h-5 mr-2" 
                  viewBox="0 0 24 24" 
                  fill="none" 
                  stroke="currentColor" 
                  strokeWidth="2"
                >
                  <path d="M21 12a9 9 0 01-9 9m9-9a9 9 0 00-9-9m9 9H3m9 9a9 9 0 01-9-9m9 9c1.657 0 3-4.03 3-9s-1.343-9-3-9m0 18c-1.657 0-3-4.03-3-9s1.343-9 3-9m-9 9a9 9 0 019-9" />
                </svg>
                Use Connected Wallet
              </Button>
              
              {config.treasuryWallet && (
                <Button
                  variant="secondary"
                  onClick={() => updateConfig({ treasuryWallet: '' })}
                  className="px-6 py-4"
                >
                  Clear
                </Button>
              )}
            </div>
          </div>

          {/* Pre-Authorization Option */}
          {config.treasuryWallet && (
            <div 
              className="rounded-xl border border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.7)] p-4"
              style={{
                animation: 'fadeIn 0.5s ease-out both'
              }}
            >
              <label className="flex items-start gap-3">
                <input
                  type="checkbox"
                  checked={config.preAuthorizeDistributions || false}
                  onChange={(e) => updateConfig({ preAuthorizeDistributions: e.target.checked })}
                  className="mt-1 rounded border-[var(--color-card-border)] bg-[rgba(9,14,32,0.85)] text-[var(--accent)] focus:ring-[var(--accent)]"
                />
                <div className="flex-1">
                  <p className="text-sm font-semibold text-[var(--color-foreground)]">
                    Pre-authorize automatic distributions
                  </p>
                  <p className="mt-1 text-xs text-[var(--muted)] leading-relaxed">
                    {config.preAuthorizeDistributions 
                      ? "✅ Distributions will happen automatically without requiring your approval each time. You'll sign one authorization transaction during minting."
                      : "Distributions will require your approval via Phantom popup each time. More secure but requires you to be online."}
                  </p>
                </div>
              </label>
            </div>
          )}

          {/* Advanced Options Toggle */}
          <div>
            <button
              type="button"
              onClick={() => setShowAdvanced(!showAdvanced)}
              className="flex items-center gap-2 text-sm text-[var(--accent)] hover:underline"
            >
              {showAdvanced ? '▼' : '▶'} Advanced Options
            </button>
          </div>

          {showAdvanced && (
            <div className="space-y-4 rounded-2xl border border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.5)] p-6">
              {/* Content Type */}
              <div>
                <label className="block text-sm font-semibold text-[var(--color-foreground)] mb-2">
                  Content Type
                </label>
                <select
                  value={config.metadata?.contentType || 'other'}
                  onChange={(e) => updateMetadata({ contentType: e.target.value })}
                  className="w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-4 py-3 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none"
                >
                  {CONTENT_TYPES.map(({ value, label }) => (
                    <option key={value} value={value}>
                      {label}
                    </option>
                  ))}
                </select>
              </div>

              {/* Distribution Frequency */}
              <div>
                <label className="block text-sm font-semibold text-[var(--color-foreground)] mb-2">
                  Distribution Frequency
                </label>
                <select
                  value={config.metadata?.distributionFrequency || 'realtime'}
                  onChange={(e) => updateMetadata({ distributionFrequency: e.target.value as any })}
                  className="w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-4 py-3 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none"
                >
                  {DISTRIBUTION_FREQUENCIES.map(({ value, label }) => (
                    <option key={value} value={value}>
                      {label}
                    </option>
                  ))}
                </select>
                <p className="mt-2 text-xs text-[var(--muted)]">
                  Note: Real-time distribution happens automatically via webhooks. 
                  Other frequencies batch payments for gas efficiency.
                </p>
              </div>

              {/* Revenue Share Percentage */}
              <div>
                <label className="block text-sm font-semibold text-[var(--color-foreground)] mb-2">
                  Revenue Share to Holders (%)
                </label>
                <input
                  type="number"
                  min="0"
                  max="100"
                  step="5"
                  value={config.metadata?.revenueSharePercentage || 100}
                  onChange={(e) => updateMetadata({ revenueSharePercentage: Number(e.target.value) })}
                  className="w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-4 py-3 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none"
                />
                <p className="mt-2 text-xs text-[var(--muted)]">
                  Percentage of revenue that goes to NFT holders (platform fees may apply separately)
                </p>
              </div>
            </div>
          )}

          {/* Preview */}
          <div 
            className="rounded-2xl border border-[var(--accent)]/40 bg-[rgba(34,211,238,0.08)] p-6"
            style={{
              animation: 'fadeIn 0.6s ease-out 0.5s both',
              background: 'linear-gradient(135deg, rgba(34,211,238,0.08) 0%, rgba(153,69,255,0.08) 100%)',
              backgroundSize: '200% 200%',
            }}
          >
            <h4 className="text-sm font-semibold text-[var(--color-foreground)] mb-3">
              ✨ Configuration Preview
            </h4>
            <div className="space-y-2 text-xs text-[var(--muted)]">
              <div className="flex justify-between">
                <span>Revenue Model:</span>
                <span className="text-[var(--accent)] font-medium">
                  {REVENUE_MODELS[config.revenueModel].label}
                </span>
              </div>
              <div className="flex justify-between">
                <span>Distribution:</span>
                <span className="text-[var(--accent)] font-medium">
                  {config.metadata?.distributionFrequency || 'realtime'}
                </span>
              </div>
              <div className="flex justify-between">
                <span>Endpoint:</span>
                <span className="text-[var(--accent)] font-medium truncate max-w-[200px]">
                  {config.paymentEndpoint || 'Not set'}
                </span>
              </div>
              <div className="flex justify-between">
                <span>Treasury:</span>
                <span className="text-[var(--accent)] font-medium truncate max-w-[200px]">
                  {config.treasuryWallet 
                    ? `${config.treasuryWallet.slice(0, 4)}...${config.treasuryWallet.slice(-4)}` 
                    : 'Platform wallet'}
                </span>
              </div>
              <div className="flex justify-between">
                <span>Auto-distribute:</span>
                <span className="text-[var(--accent)] font-medium">
                  {config.treasuryWallet && config.preAuthorizeDistributions ? 'Yes' : 'Requires approval'}
                </span>
              </div>
              {config.revenueModel === 'creator-split' && (
                <div className="flex justify-between">
                  <span>Creator Split:</span>
                  <span className="text-[var(--accent)] font-medium">
                    {config.metadata?.creatorSplitPercentage || 50}%
                  </span>
                </div>
              )}
            </div>
          </div>

          {/* Info Box */}
          <div className="rounded-xl border border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.5)] p-4">
            <div className="flex items-start gap-2">
              <svg 
                className="w-4 h-4 text-[var(--accent)] mt-0.5 flex-shrink-0" 
                viewBox="0 0 24 24" 
                fill="none" 
                stroke="currentColor" 
                strokeWidth="2"
              >
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="16" x2="12" y2="12" />
                <line x1="12" y1="8" x2="12.01" y2="8" />
              </svg>
              <p className="text-xs text-[var(--muted)] leading-relaxed">
                <strong className="text-[var(--color-foreground)]">How it works:</strong> When revenue is generated
                (streaming, API usage, rent, etc.), payments are sent to your x402 endpoint. OASIS Web4 automatically 
                queries all current NFT holders and distributes the payment according to your chosen model. 
                All distributions happen on Solana for ultra-low fees ($0.001 per holder).
                {config.treasuryWallet && (
                  <>
                    <br/><br/>
                    <strong className="text-[var(--accent)]">Using your treasury wallet:</strong> Revenue will be sent to 
                    your wallet ({config.treasuryWallet.slice(0, 8)}...). 
                    {config.preAuthorizeDistributions 
                      ? " You'll sign one authorization transaction, then distributions happen automatically."
                      : " You'll approve each distribution via Phantom popup when revenue arrives."}
                  </>
                )}
              </p>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

