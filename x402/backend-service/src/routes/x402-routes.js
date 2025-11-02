/**
 * x402 Routes for OASIS API
 * 
 * Handles x402 payment webhooks and distribution endpoints
 */

const express = require('express');
const router = express.Router();
const X402PaymentDistributor = require('../x402/X402PaymentDistributor');
const { LAMPORTS_PER_SOL } = require('@solana/web3.js');

// Initialize x402 distributor
const x402Distributor = new X402PaymentDistributor({
  solanaRpcUrl: process.env.SOLANA_RPC_URL || 'https://api.devnet.solana.com',
  oasisApiUrl: process.env.OASIS_API_URL || 'http://devnet.oasisweb4.one',
  oasisApiKey: process.env.OASIS_API_KEY || '',
  useMockData: process.env.X402_USE_MOCK_DATA !== 'false' // Default to mock for development
});

/**
 * POST /api/nft/mint-nft-x402
 * Enhanced NFT minting with x402 revenue sharing
 */
router.post('/mint-nft-x402', async (req, res) => {
  try {
    console.log('🎯 x402-enabled NFT minting request received');
    
    const { x402Config, ...mintData } = req.body;

    // Validate x402 config
    if (!x402Config || !x402Config.enabled) {
      return res.status(400).json({
        error: 'x402 configuration required. Set x402Config.enabled = true'
      });
    }

    console.log('🔧 x402 Config:', {
      enabled: x402Config.enabled,
      revenueModel: x402Config.revenueModel,
      endpoint: x402Config.paymentEndpoint,
      hasTreasuryWallet: !!x402Config.treasuryWallet
    });

    // Step 1: Mint NFT using existing OASIS API logic
    // (This will call your existing mint-nft endpoint logic)
    const mintResult = await mintNFTWithOASIS(mintData, req);

    if (!mintResult.success) {
      return res.status(500).json({
        error: 'NFT minting failed',
        details: mintResult.error
      });
    }

    const nftMintAddress = mintResult.mintAccount || mintResult.data?.result?.mintAccount;
    console.log(`✅ NFT minted: ${nftMintAddress}`);

    // Step 2: Register NFT for x402 revenue distribution
    const x402Registration = await x402Distributor.registerNFTForX402(
      nftMintAddress,
      x402Config.paymentEndpoint,
      x402Config.revenueModel,
      x402Config.treasuryWallet
    );

    console.log(`✅ x402 registered: ${x402Registration.x402Url}`);

    // Step 3: Return complete response
    res.json({
      success: true,
      message: 'NFT minted with x402 revenue sharing enabled',
      nft: {
        mintAddress: nftMintAddress,
        ...mintResult
      },
      x402: {
        enabled: true,
        paymentUrl: x402Registration.x402Url,
        revenueModel: x402Config.revenueModel,
        treasuryWallet: x402Config.treasuryWallet,
        status: 'active'
      }
    });

  } catch (error) {
    console.error('❌ x402 NFT minting error:', error);
    res.status(500).json({
      error: 'Failed to mint x402-enabled NFT',
      details: error.message
    });
  }
});

/**
 * POST /api/x402/webhook
 * Receive x402 payment notifications and trigger distribution
 */
router.post('/webhook', async (req, res) => {
  try {
    console.log('💰 x402 payment webhook triggered');
    console.log('📦 Payload:', JSON.stringify(req.body, null, 2));
    
    const paymentEvent = req.body;

    // Validate x402 signature (important for production!)
    const signature = req.headers['x-x402-signature'];
    const webhookSecret = process.env.X402_WEBHOOK_SECRET;
    
    if (!x402Distributor.validateSignature(signature, paymentEvent, webhookSecret)) {
      console.error('❌ Invalid x402 signature');
      return res.status(401).json({ error: 'Invalid x402 signature' });
    }

    // Process payment distribution
    const result = await x402Distributor.handleX402Payment(paymentEvent);

    console.log(`✅ Distributed ${result.totalDistributed} SOL to ${result.recipients} holders`);

    res.json({
      success: true,
      message: 'Payment distributed successfully',
      distribution: result
    });

  } catch (error) {
    console.error('❌ x402 webhook error:', error);
    res.status(500).json({
      error: 'Payment distribution failed',
      details: error.message
    });
  }
});

/**
 * GET /api/x402/stats/:nftMintAddress
 * Get distribution statistics for an NFT
 */
router.get('/stats/:nftMintAddress', async (req, res) => {
  try {
    const { nftMintAddress } = req.params;
    
    console.log(`📊 Fetching x402 stats for: ${nftMintAddress}`);
    
    const stats = await x402Distributor.getPaymentStats(nftMintAddress);
    
    res.json({
      success: true,
      nftMintAddress,
      stats
    });

  } catch (error) {
    console.error('❌ Error fetching x402 stats:', error);
    res.status(500).json({
      error: 'Failed to fetch stats',
      details: error.message
    });
  }
});

/**
 * POST /api/x402/distribute-test
 * Manual distribution trigger for testing/demo
 */
router.post('/distribute-test', async (req, res) => {
  try {
    const { nftMintAddress, amount } = req.body;

    console.log(`🧪 Test distribution: ${amount} SOL to holders of ${nftMintAddress}`);

    const result = await x402Distributor.handleX402Payment({
      endpoint: 'test',
      amount: amount * LAMPORTS_PER_SOL, // Convert SOL to lamports
      currency: 'SOL',
      payer: 'test-wallet',
      metadata: {
        nftMintAddress,
        serviceType: 'test',
        timestamp: Date.now()
      }
    });

    res.json({
      success: true,
      message: 'Test distribution complete',
      result
    });

  } catch (error) {
    console.error('❌ Test distribution error:', error);
    res.status(500).json({
      error: 'Test failed',
      details: error.message
    });
  }
});

/**
 * GET /api/x402/history/:nftMintAddress
 * Get distribution history for an NFT
 */
router.get('/history/:nftMintAddress', async (req, res) => {
  try {
    const { nftMintAddress } = req.params;
    const limit = parseInt(req.query.limit) || 10;
    
    const storageUtils = require('../storage/storage-utils');
    const distributions = await storageUtils.getX402Distributions(nftMintAddress, limit);
    
    res.json({
      success: true,
      nftMintAddress,
      distributions
    });

  } catch (error) {
    console.error('❌ Error fetching distribution history:', error);
    res.status(500).json({
      error: 'Failed to fetch history',
      details: error.message
    });
  }
});

/**
 * Helper: Mint NFT using existing OASIS API
 * This calls the existing mint-nft logic
 */
async function mintNFTWithOASIS(mintData, req) {
  // This function wraps your existing mint-nft logic
  // For now, we'll simulate success
  // In production, this would call your actual minting code
  
  console.log('🎨 Minting NFT with OASIS API...');
  
  // Return mock success for now
  // You'll replace this with actual minting logic
  return {
    success: true,
    mintAccount: 'MOCK_MINT_' + Date.now().toString(36),
    transactionSignature: 'MOCK_TX_' + Math.random().toString(36),
    data: {
      result: {
        mintAccount: 'MOCK_MINT_' + Date.now().toString(36)
      }
    }
  };
}

module.exports = router;

