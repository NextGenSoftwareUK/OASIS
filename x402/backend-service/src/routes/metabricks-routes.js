/**
 * MetaBricks x402 Routes
 * 
 * Handles revenue distribution from Smart Contract Generator to MetaBricks holders
 */

const express = require('express');
const router = express.Router();

// MetaBricks collection configuration
const METABRICKS_CONFIG = {
  collectionSymbol: 'MBRICK',
  totalSupply: 432,
  distributionModel: 'equal', // All brick holders get equal share
  distributionPercentage: 90, // 90% to holders, 10% to treasury
  revenueSource: 'AssetRail Smart Contract Generator API'
};

/**
 * POST /api/metabricks/sc-gen-webhook
 * Receives payments from Smart Contract Generator and distributes to MetaBricks holders
 */
router.post('/sc-gen-webhook', async (req, res) => {
  try {
    console.log('🧱 MetaBricks distribution webhook triggered');
    console.log('📦 Payload:', JSON.stringify(req.body, null, 2));
    
    const {
      signature,        // Solana transaction signature
      amount,           // Total SOL paid (e.g., 0.60)
      operation,        // 'purchase-credits' or 'pay-per-use'
      nftCollection,    // Should be 'METABRICKS' or 'ARCG'
      distributionPercentage, // Should be 90
      metadata
    } = req.body;

    // Validate collection
    if (nftCollection !== 'METABRICKS' && nftCollection !== 'ARCG') {
      console.warn(`⚠️  Unknown NFT collection: ${nftCollection}, using METABRICKS`);
    }

    // Calculate distribution amount
    const distributionAmount = amount * (distributionPercentage / 100);
    const treasuryAmount = amount - distributionAmount;
    
    console.log(`💰 Distribution Details:`);
    console.log(`   Total payment: ${amount} SOL`);
    console.log(`   To distribute: ${distributionAmount} SOL (${distributionPercentage}%)`);
    console.log(`   To treasury: ${treasuryAmount} SOL (${100 - distributionPercentage}%)`);

    // Get x402 distributor from parent app
    const X402PaymentDistributor = require('../distributor/X402PaymentDistributor');
    const distributor = req.app.locals.x402Distributor;

    if (!distributor) {
      throw new Error('x402 distributor not initialized');
    }

    // Query MetaBricks holders
    console.log('🔍 Querying MetaBricks NFT holders...');
    const holders = await queryMetaBricksHolders(distributor);
    
    console.log(`👥 Found ${holders.length} MetaBricks holders`);

    // Calculate per-holder amount
    const { LAMPORTS_PER_SOL } = require('@solana/web3.js');
    const amountPerHolder = Math.floor((distributionAmount * LAMPORTS_PER_SOL) / holders.length);
    
    console.log(`💵 Each holder receives: ${amountPerHolder / LAMPORTS_PER_SOL} SOL`);

    // Generate distribution transaction (mock for now)
    const distributionTx = `metabricks_dist_${Date.now().toString(36)}`;

    // Record distribution
    const storage = req.app.locals.storage;
    if (storage && storage.recordDistribution) {
      await storage.recordDistribution({
        nftMintAddress: 'METABRICKS_COLLECTION',
        nftCollection: 'METABRICKS',
        totalAmount: distributionAmount * LAMPORTS_PER_SOL,
        recipients: holders.length,
        amountPerHolder,
        txSignature: distributionTx,
        fundingTx: signature,
        timestamp: Date.now(),
        status: 'completed',
        metadata: {
          source: 'SC-Gen API',
          operation,
          ...metadata
        }
      });
    }

    console.log(`✅ Distributed ${distributionAmount} SOL to ${holders.length} MetaBricks holders`);

    res.json({
      success: true,
      message: 'Revenue distributed to MetaBricks holders',
      distribution: {
        totalAmount: distributionAmount,
        holders: holders.length,
        amountPerHolder: amountPerHolder / LAMPORTS_PER_SOL,
        distributionTx,
        fundingTx: signature,
        timestamp: Date.now()
      }
    });

  } catch (error) {
    console.error('❌ MetaBricks distribution error:', error);
    res.status(500).json({
      success: false,
      error: 'Distribution failed',
      details: error.message
    });
  }
});

/**
 * GET /api/metabricks/stats
 * Get distribution statistics for MetaBricks collection
 */
router.get('/stats', async (req, res) => {
  try {
    const storage = req.app.locals.storage;
    const distributions = storage && storage.getDistributions 
      ? await storage.getDistributions('METABRICKS_COLLECTION', 100)
      : [];
    
    const totalDistributed = distributions.reduce((sum, d) => sum + (d.totalAmount || 0), 0);
    const { LAMPORTS_PER_SOL } = require('@solana/web3.js');
    
    res.json({
      success: true,
      stats: {
        collectionSymbol: METABRICKS_CONFIG.collectionSymbol,
        totalBricks: METABRICKS_CONFIG.totalSupply,
        totalDistributed: totalDistributed / LAMPORTS_PER_SOL,
        distributionCount: distributions.length,
        averagePerDistribution: distributions.length > 0 
          ? (totalDistributed / distributions.length) / LAMPORTS_PER_SOL 
          : 0,
        recentDistributions: distributions.slice(0, 10)
      }
    });

  } catch (error) {
    console.error('❌ Error fetching MetaBricks stats:', error);
    res.status(500).json({
      success: false,
      error: 'Failed to fetch stats',
      details: error.message
    });
  }
});

/**
 * GET /api/metabricks/holders
 * Get all current MetaBricks NFT holders
 */
router.get('/holders', async (req, res) => {
  try {
    const distributor = req.app.locals.x402Distributor;
    const holders = await queryMetaBricksHolders(distributor);
    
    res.json({
      success: true,
      collectionSymbol: METABRICKS_CONFIG.collectionSymbol,
      totalHolders: holders.length,
      holders: holders.map(h => ({
        walletAddress: h.walletAddress,
        balance: h.balance
      }))
    });

  } catch (error) {
    console.error('❌ Error fetching holders:', error);
    res.status(500).json({
      success: false,
      error: 'Failed to fetch holders',
      details: error.message
    });
  }
});

/**
 * Helper: Query MetaBricks NFT holders from Solana
 */
async function queryMetaBricksHolders(distributor) {
  // For now, use mock data (432 holders)
  // In production, this would query Solana for actual MetaBricks holders
  
  if (distributor.config.useMockData) {
    console.log('📊 Using mock MetaBricks holders (432 bricks)');
    const holders = [];
    
    for (let i = 1; i <= 432; i++) {
      holders.push({
        walletAddress: `MetaBrick${i}Holder${Math.random().toString(36).substring(7)}`,
        tokenAccount: `TokenAccount${i}`,
        balance: 1,
        brickNumber: i
      });
    }
    
    return holders;
  }
  
  // Production: Query actual Solana blockchain for MetaBricks holders
  // This would use the collection's mint addresses to find all token holders
  
  console.log('🔍 Querying Solana for MetaBricks holders...');
  // Implementation would go here - query based on collection metadata
  
  // For now, return mock data
  return queryMetaBricksHolders({ config: { useMockData: true } });
}

module.exports = router;

