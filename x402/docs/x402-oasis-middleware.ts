/**
 * OASIS API x402 Middleware
 * 
 * Integrates x402 payment protocol with existing OASIS NFT minting endpoints
 * Extends the current server.js implementation
 */

import express from 'express';
import { X402PaymentDistributor } from './X402PaymentDistributor';

// Configuration
const X402_CONFIG = {
  solanaRpcUrl: process.env.SOLANA_RPC_URL || 'https://api.devnet.solana.com',
  oasisApiUrl: process.env.OASIS_API_URL || 'https://api.oasis.one',
  oasisApiKey: process.env.OASIS_API_KEY || '',
  x402WebhookUrl: process.env.X402_WEBHOOK_URL || ''
};

// Initialize distributor
const distributor = new X402PaymentDistributor(X402_CONFIG);

/**
 * Middleware to add x402 support to existing NFT minting
 * Add this to your server.js BEFORE the mint-nft endpoint
 */
export function x402Middleware() {
  const router = express.Router();

  /**
   * Enhanced NFT minting with x402 revenue sharing
   * POST /api/mint-nft-x402
   */
  router.post('/api/mint-nft-x402', async (req, res) => {
    try {
      console.log('🎯 x402-enabled NFT minting request received');
      
      const {
        walletAddress,
        brickId,
        brickName,
        imageUrl,
        paymentNetwork,
        x402Config // NEW: x402 configuration
      } = req.body;

      // Validate x402 config
      if (!x402Config || !x402Config.enabled) {
        return res.status(400).json({
          error: 'x402 configuration required. Set x402Config.enabled = true'
        });
      }

      // Step 1: Mint NFT using existing OASIS API (your current logic)
      const mintResult = await mintNFTWithOASIS({
        walletAddress,
        brickId,
        brickName,
        imageUrl,
        paymentNetwork
      });

      if (!mintResult.success) {
        return res.status(500).json({
          error: 'NFT minting failed',
          details: mintResult.error
        });
      }

      const nftMintAddress = mintResult.mintAccount;
      console.log(`✅ NFT minted: ${nftMintAddress}`);

      // Step 2: Register NFT for x402 revenue distribution
      const x402Registration = await distributor.registerNFTForX402(
        nftMintAddress,
        x402Config.paymentEndpoint || `https://api.yourservice.com/x402/pay/${nftMintAddress}`,
        x402Config.revenueModel || 'equal'
      );

      console.log(`✅ x402 registered: ${x402Registration.x402Url}`);

      // Step 3: Return complete response
      res.json({
        success: true,
        message: 'NFT minted with x402 revenue sharing enabled',
        nft: {
          mintAddress: nftMintAddress,
          transactionSignature: mintResult.transactionSignature,
          metadata: mintResult.metadata
        },
        x402: {
          enabled: true,
          paymentUrl: x402Registration.x402Url,
          revenueModel: x402Config.revenueModel,
          status: 'active'
        }
      });

    } catch (error: any) {
      console.error('❌ x402 NFT minting error:', error);
      res.status(500).json({
        error: 'Failed to mint x402-enabled NFT',
        details: error.message
      });
    }
  });

  /**
   * x402 Payment Webhook
   * POST /api/x402/webhook
   * 
   * Called by x402 protocol when payment is received
   */
  router.post('/api/x402/webhook', async (req, res) => {
    try {
      console.log('💰 x402 payment webhook triggered');
      
      const paymentEvent = req.body;

      // Validate x402 signature (production requirement)
      const signature = req.headers['x-x402-signature'] as string;
      if (!validateX402Signature(signature, paymentEvent)) {
        return res.status(401).json({ error: 'Invalid x402 signature' });
      }

      // Process payment distribution
      const result = await distributor.handleX402Payment(paymentEvent);

      console.log(`✅ Distributed ${result.amountPerHolder} SOL to ${result.recipients} holders`);

      res.json({
        success: true,
        distribution: result
      });

    } catch (error: any) {
      console.error('❌ x402 webhook error:', error);
      res.status(500).json({
        error: 'Payment distribution failed',
        details: error.message
      });
    }
  });

  /**
   * Get x402 statistics for an NFT
   * GET /api/x402/stats/:nftMintAddress
   */
  router.get('/api/x402/stats/:nftMintAddress', async (req, res) => {
    try {
      const { nftMintAddress } = req.params;
      
      const stats = await distributor.getPaymentStats(nftMintAddress);
      
      res.json({
        success: true,
        nftMintAddress,
        stats
      });

    } catch (error: any) {
      console.error('❌ Error fetching x402 stats:', error);
      res.status(500).json({
        error: 'Failed to fetch stats',
        details: error.message
      });
    }
  });

  /**
   * Manual distribution trigger (for testing)
   * POST /api/x402/distribute-test
   */
  router.post('/api/x402/distribute-test', async (req, res) => {
    try {
      const { nftMintAddress, amount } = req.body;

      console.log(`🧪 Test distribution: ${amount} SOL to holders of ${nftMintAddress}`);

      const result = await distributor.handleX402Payment({
        endpoint: 'test',
        amount: amount * 1_000_000_000, // Convert to lamports
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

    } catch (error: any) {
      console.error('❌ Test distribution error:', error);
      res.status(500).json({
        error: 'Test failed',
        details: error.message
      });
    }
  });

  return router;
}

/**
 * Helper: Mint NFT using existing OASIS API
 * This wraps your current minting logic
 */
async function mintNFTWithOASIS(params: {
  walletAddress: string;
  brickId: string;
  brickName: string;
  imageUrl: string;
  paymentNetwork: string;
}): Promise<any> {
  // This would call your existing OASIS API endpoint
  // For now, return mock data
  return {
    success: true,
    mintAccount: 'MOCK_MINT_' + Date.now().toString(36),
    transactionSignature: 'MOCK_TX_' + Math.random().toString(36),
    metadata: {
      name: params.brickName,
      image: params.imageUrl
    }
  };
}

/**
 * Validate x402 protocol signature
 * Important for security in production
 */
function validateX402Signature(signature: string, payload: any): boolean {
  // TODO: Implement x402 signature validation
  // This should verify the webhook is from legitimate x402 protocol
  
  if (!signature) {
    console.warn('⚠️  No x402 signature provided (development mode)');
    return true; // Allow in development
  }
  
  // Production implementation:
  // const expectedSignature = crypto
  //   .createHmac('sha256', X402_CONFIG.webhookSecret)
  //   .update(JSON.stringify(payload))
  //   .digest('hex');
  // return signature === expectedSignature;
  
  return true;
}

/**
 * Example: How to integrate with your existing server.js
 */
export function integrateWithExistingServer(app: express.Application) {
  // Add x402 routes
  app.use(x402Middleware());
  
  console.log('✅ x402 middleware integrated with OASIS API');
  console.log('📍 Endpoints:');
  console.log('   POST /api/mint-nft-x402 - Mint NFT with x402 revenue sharing');
  console.log('   POST /api/x402/webhook - Receive x402 payment notifications');
  console.log('   GET /api/x402/stats/:nftMintAddress - Get payment statistics');
  console.log('   POST /api/x402/distribute-test - Test payment distribution');
}

export default x402Middleware;

