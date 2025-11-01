"use client";

import { useState } from "react";
import { ManualDistributionPanel } from "@/components/x402/manual-distribution-panel";
import { DistributionDashboard } from "@/components/x402/distribution-dashboard";
import { TreasuryActivityFeed } from "@/components/x402/treasury-activity-feed";
import { Button } from "@/components/ui/button";

/**
 * x402 Revenue Distribution Dashboard
 * 
 * Where artists/creators come to:
 * - View their x402-enabled NFTs
 * - Distribute revenue to holders
 * - View distribution history
 * - See statistics
 */

export default function X402DashboardPage() {
  const [selectedNFT, setSelectedNFT] = useState<string | null>(null);
  const [viewMode, setViewMode] = useState<'distribute' | 'stats' | 'treasury'>('distribute');

  // Mock NFTs for demo - in production, fetch from API
  const mockNFTs = [
    {
      mintAddress: "DEMO_MUSIC_NFT_ABC123",
      name: "My Album NFT Collection",
      symbol: "ALBUM1",
      totalSupply: 1000,
      imageUrl: "/spinning tape.mov",
      imageType: "video" as const,
      revenueModel: "equal" as const,
      treasuryWallet: "HT2sbYb6qjYKNjSdSWkwCp6bfYtrW9LMaGsnevLRRVnB",
      totalDistributed: 45.5,
      lastDistribution: "2026-01-15",
      holderCount: 250
    },
    {
      mintAddress: "DEMO_PROPERTY_NFT_XYZ789",
      name: "Downtown Property Shares",
      symbol: "PROP1",
      totalSupply: 500,
      imageUrl: "https://via.placeholder.com/200",
      imageType: "image" as const,
      revenueModel: "weighted" as const,
      treasuryWallet: "HT2sbYb6qjYKNjSdSWkwCp6bfYtrW9LMaGsnevLRRVnB",
      totalDistributed: 120.0,
      lastDistribution: "2026-01-10",
      holderCount: 125
    }
  ];

  const selectedNFTData = mockNFTs.find(nft => nft.mintAddress === selectedNFT);

  return (
    <div className="min-h-screen bg-[var(--color-background)] text-[var(--color-foreground)]">
      <div className="mx-auto max-w-7xl px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-4xl font-bold mb-2">
            x402 Revenue Dashboard
          </h1>
          <p className="text-[var(--muted)]">
            Manage revenue distributions for your x402-enabled NFTs
          </p>
        </div>

        {/* NFT Selection Grid */}
        <div className="mb-8">
          <h2 className="text-xl font-semibold mb-4">Your x402 NFT Collections</h2>
          
          <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
            {mockNFTs.map((nft) => (
              <button
                key={nft.mintAddress}
                onClick={() => setSelectedNFT(nft.mintAddress)}
                className={`
                  relative overflow-hidden rounded-2xl border p-6 text-left transition-all duration-300
                  ${selectedNFT === nft.mintAddress
                    ? 'border-[var(--accent)] ring-2 ring-[var(--accent)]/50 shadow-[0_20px_50px_rgba(34,211,238,0.25)]'
                    : 'border-[var(--color-card-border)]/60 hover:border-[var(--accent)]/50'
                  }
                `}
              >
                <div className="absolute inset-0 bg-[radial-gradient(circle_at_top,rgba(34,211,238,0.15),transparent_60%)] opacity-50" />
                
                <div className="relative">
                  {/* NFT Image/Video */}
                  <div className="mb-4 aspect-square overflow-hidden rounded-xl bg-[var(--color-card-background)]">
                    {nft.imageType === 'video' ? (
                      <video 
                        src={nft.imageUrl} 
                        autoPlay
                        loop
                        muted
                        playsInline
                        className="h-full w-full object-cover"
                      />
                    ) : (
                      <img 
                        src={nft.imageUrl} 
                        alt={nft.name}
                        className="h-full w-full object-cover"
                      />
                    )}
                  </div>

                  {/* NFT Info */}
                  <h3 className="text-lg font-semibold mb-1">{nft.name}</h3>
                  <p className="text-sm text-[var(--muted)] mb-3">
                    {nft.symbol} • {nft.totalSupply} total supply
                  </p>

                  {/* Stats Grid */}
                  <div className="grid grid-cols-2 gap-3 text-xs">
                    <div className="rounded-lg bg-[var(--color-card-background)]/50 p-2">
                      <div className="text-[var(--muted)]">Total Distributed</div>
                      <div className="text-lg font-semibold text-[var(--accent)]">
                        {nft.totalDistributed} SOL
                      </div>
                    </div>
                    <div className="rounded-lg bg-[var(--color-card-background)]/50 p-2">
                      <div className="text-[var(--muted)]">Holders</div>
                      <div className="text-lg font-semibold text-[var(--accent)]">
                        {nft.holderCount}
                      </div>
                    </div>
                  </div>

                  {/* Last Distribution */}
                  <div className="mt-3 pt-3 border-t border-[var(--color-card-border)]/30 text-xs text-[var(--muted)]">
                    Last distribution: {nft.lastDistribution}
                  </div>
                </div>
              </button>
            ))}

            {/* Add More Placeholder */}
            <div className="flex items-center justify-center rounded-2xl border border-dashed border-[var(--color-card-border)]/60 p-6 min-h-[400px]">
              <div className="text-center">
                <svg 
                  className="w-12 h-12 mx-auto mb-3 text-[var(--muted)]" 
                  viewBox="0 0 24 24" 
                  fill="none" 
                  stroke="currentColor" 
                  strokeWidth="2"
                >
                  <circle cx="12" cy="12" r="10" />
                  <line x1="12" y1="8" x2="12" y2="16" />
                  <line x1="8" y1="12" x2="16" y2="12" />
                </svg>
                <p className="text-sm text-[var(--muted)]">
                  Mint more x402 NFTs
                </p>
              </div>
            </div>
          </div>
        </div>

        {/* Selected NFT Actions */}
        {selectedNFT && selectedNFTData && (
          <div className="rounded-2xl border border-[var(--color-card-border)]/60 bg-[rgba(8,12,28,0.85)] p-6">
            {/* View Mode Toggle */}
            <div className="flex items-center justify-between mb-6">
              <h2 className="text-2xl font-bold">
                {selectedNFTData.name}
              </h2>
              
              <div className="flex gap-2">
                <Button
                  variant={viewMode === 'distribute' ? 'primary' : 'secondary'}
                  onClick={() => setViewMode('distribute')}
                  className="text-sm"
                >
                  Distribute Revenue
                </Button>
                <Button
                  variant={viewMode === 'treasury' ? 'primary' : 'secondary'}
                  onClick={() => setViewMode('treasury')}
                  className="text-sm"
                >
                  Treasury Activity
                </Button>
                <Button
                  variant={viewMode === 'stats' ? 'primary' : 'secondary'}
                  onClick={() => setViewMode('stats')}
                  className="text-sm"
                >
                  Statistics
                </Button>
              </div>
            </div>

            {/* Content */}
            {viewMode === 'treasury' ? (
              <div>
                <TreasuryActivityFeed
                  treasuryWallet={selectedNFTData.treasuryWallet}
                  nftName={selectedNFTData.name}
                />
              </div>
            ) : viewMode === 'distribute' ? (
              <div>
                <ManualDistributionPanel
                  nftMintAddress={selectedNFT}
                  baseUrl="http://localhost:4000"
                  onDistributionComplete={(result) => {
                    console.log('Distribution complete:', result);
                    // Refresh stats
                  }}
                />

                {/* Quick Info */}
                <div className="mt-6 grid gap-4 sm:grid-cols-3">
                  <div className="rounded-xl border border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.7)] p-4">
                    <div className="text-sm text-[var(--muted)]">Revenue Model</div>
                    <div className="text-lg font-semibold text-[var(--accent)] capitalize">
                      {selectedNFTData.revenueModel}
                    </div>
                  </div>
                  <div className="rounded-xl border border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.7)] p-4">
                    <div className="text-sm text-[var(--muted)]">Current Holders</div>
                    <div className="text-lg font-semibold text-[var(--accent)]">
                      {selectedNFTData.holderCount}
                    </div>
                  </div>
                  <div className="rounded-xl border border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.7)] p-4">
                    <div className="text-sm text-[var(--muted)]">Treasury</div>
                    <div className="text-xs font-mono text-[var(--accent)]">
                      {selectedNFTData.treasuryWallet.slice(0, 12)}...
                    </div>
                  </div>
                </div>
              </div>
            ) : viewMode === 'stats' ? (
              <div className="text-center py-8 text-[var(--muted)]">
                <p>Statistics view - Coming soon</p>
              </div>
            ) : null}
          </div>
        )}

        {/* No Selection State */}
        {!selectedNFT && (
          <div className="text-center py-16">
            <svg 
              className="w-16 h-16 mx-auto mb-4 text-[var(--muted)]" 
              viewBox="0 0 24 24" 
              fill="none" 
              stroke="currentColor" 
              strokeWidth="2"
            >
              <path d="M12 2v20M17 5H9.5a3.5 3.5 0 000 7h5a3.5 3.5 0 010 7H6" />
            </svg>
            <h3 className="text-xl font-semibold mb-2">
              Select an NFT Collection
            </h3>
            <p className="text-[var(--muted)]">
              Choose an x402-enabled NFT collection above to distribute revenue
            </p>
          </div>
        )}
      </div>
    </div>
  );
}

