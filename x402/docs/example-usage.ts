/**
 * Example: How to use OASIS x402 integration
 * 
 * This shows how to mint NFTs with automatic revenue distribution
 */

import { X402PaymentDistributor } from './X402PaymentDistributor';
import axios from 'axios';

// ============================================================================
// EXAMPLE 1: Mint NFT with x402 Revenue Sharing
// ============================================================================

async function exampleMintRevenueNFT() {
  console.log('🎨 Example: Minting NFT with x402 revenue sharing\n');

  // Your existing OASIS API call, enhanced with x402
  const response = await axios.post('https://api.oasis.one/api/mint-nft-x402', {
    // Standard OASIS NFT fields
    walletAddress: 'YourSolanaWalletAddress',
    brickId: 'Brick 1',
    brickName: 'Revenue Share Music NFT',
    imageUrl: 'https://your-ipfs-gateway.com/image.png',
    paymentNetwork: 'solana',
    
    // NEW: x402 configuration
    x402Config: {
      enabled: true,
      paymentEndpoint: 'https://api.yourservice.com/x402/pay',
      revenueModel: 'equal', // Options: 'equal', 'weighted', 'creator-split'
      metadata: {
        type: 'music-streaming',
        artist: 'Artist Name',
        royaltyRate: 0.05 // 5% of streaming revenue
      }
    }
  });

  console.log('✅ NFT Minted:', response.data.nft.mintAddress);
  console.log('💰 x402 Payment URL:', response.data.x402.paymentUrl);
  console.log('📊 Revenue Model:', response.data.x402.revenueModel);
}

// ============================================================================
// EXAMPLE 2: Simulate x402 Payment (Testing)
// ============================================================================

async function exampleTestPayment() {
  console.log('🧪 Example: Testing x402 payment distribution\n');

  const nftMintAddress = 'YourNFTMintAddress';
  
  // Simulate a payment of 1 SOL
  const response = await axios.post('https://api.oasis.one/api/x402/distribute-test', {
    nftMintAddress: nftMintAddress,
    amount: 1.0 // 1 SOL
  });

  console.log(`✅ Distributed to ${response.data.result.recipients} holders`);
  console.log(`💵 Each holder received: ${response.data.result.amountPerHolder} SOL`);
  console.log(`🔗 Transaction: ${response.data.result.distributionTx}`);
}

// ============================================================================
// EXAMPLE 3: Music Streaming Revenue NFT
// ============================================================================

async function exampleMusicStreamingNFT() {
  console.log('🎵 Example: Music streaming revenue NFT\n');
  
  /**
   * Use case: Artist mints 1000 NFTs representing ownership shares
   * Fans buy NFTs, streaming revenue auto-distributed monthly via x402
   */

  const mintResponse = await axios.post('https://api.oasis.one/api/mint-nft-x402', {
    walletAddress: 'ArtistWallet',
    brickId: 'Album Track 1',
    brickName: 'Song Title - Revenue Share NFT',
    imageUrl: 'https://ipfs.io/album-cover.png',
    paymentNetwork: 'solana',
    
    x402Config: {
      enabled: true,
      paymentEndpoint: 'https://streaming-platform.com/x402/revenue-share',
      revenueModel: 'equal',
      metadata: {
        contentType: 'music-track',
        isrc: 'US-ABC-12-34567',
        artistShare: 0.7, // 70% to NFT holders, 30% to platform
        distributionFrequency: 'monthly'
      }
    }
  });

  console.log('🎼 Music NFT Created!');
  console.log(`   Fans who hold this NFT will automatically receive`);
  console.log(`   streaming revenue via x402 protocol every month.`);
  console.log(`   \n   Payment URL: ${mintResponse.data.x402.paymentUrl}`);
}

// ============================================================================
// EXAMPLE 4: API Access NFT with Usage-Based Payments
// ============================================================================

async function exampleAPIAccessNFT() {
  console.log('🔌 Example: API access NFT with usage revenue\n');
  
  /**
   * Use case: Developer offers API access via NFT
   * Users who hold NFT get API credits
   * Revenue from API usage distributed to NFT holders via x402
   */

  const mintResponse = await axios.post('https://api.oasis.one/api/mint-nft-x402', {
    walletAddress: 'DeveloperWallet',
    brickId: 'API Access Pass',
    brickName: 'Premium API Access Token',
    imageUrl: 'https://ipfs.io/api-badge.png',
    paymentNetwork: 'solana',
    
    x402Config: {
      enabled: true,
      paymentEndpoint: 'https://your-api.com/x402/usage-payment',
      revenueModel: 'equal',
      metadata: {
        accessTier: 'premium',
        rateLimitPerDay: 100000,
        revenueShareEnabled: true,
        paymentPerRequest: 0.00001 // $0.00001 per API call
      }
    }
  });

  console.log('🔑 API Access NFT Created!');
  console.log(`   Every API call generates micro-payment via x402`);
  console.log(`   Revenue automatically split among all NFT holders`);
}

// ============================================================================
// EXAMPLE 5: Real Estate Rental Income NFT
// ============================================================================

async function exampleRealEstateNFT() {
  console.log('🏠 Example: Real estate rental income NFT\n');
  
  /**
   * Use case: Property tokenized as NFTs
   * Rental income distributed monthly to NFT holders via x402
   * Integrates with OASIS RWA tokenization
   */

  const mintResponse = await axios.post('https://api.oasis.one/api/mint-nft-x402', {
    walletAddress: 'PropertyTrustWallet',
    brickId: '123 Sunset Blvd',
    brickName: 'Beverly Hills Property Share #1',
    imageUrl: 'https://ipfs.io/property-photo.jpg',
    paymentNetwork: 'solana',
    
    x402Config: {
      enabled: true,
      paymentEndpoint: 'https://property-trust.com/x402/rental-income',
      revenueModel: 'equal',
      metadata: {
        propertyAddress: '123 Sunset Blvd, Beverly Hills, CA',
        totalTokens: 3500,
        monthlyRent: 7875, // $7,875/month
        distributionDay: 1, // 1st of each month
        trustEntity: 'Beverly Hills Property Trust 2025-A'
      }
    }
  });

  console.log('🏡 Real Estate NFT Created!');
  console.log(`   Monthly rental income: $7,875`);
  console.log(`   Distributed via x402 to 3,500 NFT holders`);
  console.log(`   Each holder receives ~$2.25/month automatically`);
}

// ============================================================================
// EXAMPLE 6: Get Payment Statistics
// ============================================================================

async function exampleGetStats() {
  console.log('📊 Example: Fetching x402 payment statistics\n');

  const nftMintAddress = 'YourNFTMintAddress';
  
  const response = await axios.get(
    `https://api.oasis.one/api/x402/stats/${nftMintAddress}`
  );

  const stats = response.data.stats;
  
  console.log(`NFT: ${nftMintAddress}`);
  console.log(`Total Distributed: ${stats.totalDistributed} SOL`);
  console.log(`Distribution Events: ${stats.distributionCount}`);
  console.log(`Current Holders: ${stats.holderCount}`);
  console.log(`Avg per Distribution: ${stats.averagePerDistribution} SOL`);
}

// ============================================================================
// EXAMPLE 7: Content Creator Subscription NFT
// ============================================================================

async function exampleCreatorSubscriptionNFT() {
  console.log('🎬 Example: Content creator subscription NFT\n');
  
  /**
   * Use case: YouTuber/Podcaster offers subscriber NFTs
   * Ad revenue and sponsorships distributed via x402
   * Holders get exclusive content + revenue share
   */

  const mintResponse = await axios.post('https://api.oasis.one/api/mint-nft-x402', {
    walletAddress: 'CreatorWallet',
    brickId: 'Creator Patron Pass',
    brickName: 'Premium Patron NFT - Channel Name',
    imageUrl: 'https://ipfs.io/creator-badge.png',
    paymentNetwork: 'solana',
    
    x402Config: {
      enabled: true,
      paymentEndpoint: 'https://creator-platform.com/x402/ad-revenue',
      revenueModel: 'weighted', // Based on holding duration
      metadata: {
        channelName: 'Your Channel',
        subscriberCount: 1000000,
        revenueSharePercentage: 20, // 20% of ad revenue to NFT holders
        exclusiveBenefits: [
          'Ad-free viewing',
          'Early access to content',
          'Monthly revenue share',
          'Exclusive Discord access'
        ]
      }
    }
  });

  console.log('🎥 Creator NFT Created!');
  console.log(`   20% of ad revenue distributed to NFT holders`);
  console.log(`   Payments automated via x402 protocol`);
}

// ============================================================================
// Run Examples
// ============================================================================

async function runExamples() {
  try {
    // Uncomment to run specific examples:
    
    // await exampleMintRevenueNFT();
    // await exampleTestPayment();
    // await exampleMusicStreamingNFT();
    // await exampleAPIAccessNFT();
    // await exampleRealEstateNFT();
    // await exampleGetStats();
    // await exampleCreatorSubscriptionNFT();
    
    console.log('\n✨ All examples completed!');
    
  } catch (error) {
    console.error('Error running examples:', error);
  }
}

// Export for use in other modules
export {
  exampleMintRevenueNFT,
  exampleTestPayment,
  exampleMusicStreamingNFT,
  exampleAPIAccessNFT,
  exampleRealEstateNFT,
  exampleGetStats,
  exampleCreatorSubscriptionNFT
};

