/**
 * OASIS Web4 x402 Service
 * 
 * Main service class for x402 payment distribution
 * Can be used as standalone service or embedded in applications
 */

const express = require('express');
const cors = require('cors');
const X402PaymentDistributor = require('./distributor/X402PaymentDistributor');

class X402Service {
  constructor(options = {}) {
    this.options = {
      solanaRpcUrl: options.solanaRpcUrl || process.env.SOLANA_RPC_URL || 'https://api.devnet.solana.com',
      useMockData: options.useMockData !== undefined ? options.useMockData : process.env.X402_USE_MOCK_DATA !== 'false',
      webhookSecret: options.webhookSecret || process.env.X402_WEBHOOK_SECRET,
      platformFeePercent: options.platformFeePercent || 2.5,
      storage: options.storage,
      ...options
    };

    if (!this.options.storage) {
      throw new Error('Storage adapter is required. Provide a storage option.');
    }

    // Initialize distributor
    this.distributor = new X402PaymentDistributor({
      solanaRpcUrl: this.options.solanaRpcUrl,
      useMockData: this.options.useMockData,
      storage: this.options.storage
    });

    // Create Express router
    this.router = this.createRouter();
    
    console.log('✅ X402Service initialized');
    console.log('   Solana RPC:', this.options.solanaRpcUrl);
    console.log('   Mock Data:', this.options.useMockData);
  }

  /**
   * Create Express router with all x402 endpoints
   */
  createRouter() {
    const router = express.Router();
    router.use(express.json());

    // Register NFT for x402
    router.post('/register', async (req, res) => {
      try {
        const { nftMintAddress, paymentEndpoint, revenueModel, treasuryWallet } = req.body;

        const result = await this.distributor.registerNFTForX402(
          nftMintAddress,
          paymentEndpoint,
          revenueModel,
          treasuryWallet
        );

        res.json({
          success: true,
          message: 'NFT registered for x402',
          ...result
        });
      } catch (error) {
        console.error('❌ Registration error:', error);
        res.status(500).json({
          success: false,
          error: error.message
        });
      }
    });

    // Payment webhook
    router.post('/webhook', async (req, res) => {
      try {
        console.log('💰 x402 payment webhook received');
        
        const signature = req.headers['x-x402-signature'];
        if (!this.distributor.validateSignature(signature, req.body, this.options.webhookSecret)) {
          return res.status(401).json({ error: 'Invalid signature' });
        }

        const result = await this.distributor.handleX402Payment(req.body);

        res.json({
          success: true,
          message: 'Payment distributed',
          distribution: result
        });
      } catch (error) {
        console.error('❌ Webhook error:', error);
        res.status(500).json({
          success: false,
          error: error.message
        });
      }
    });

    // Get stats
    router.get('/stats/:nftMintAddress', async (req, res) => {
      try {
        const stats = await this.distributor.getPaymentStats(req.params.nftMintAddress);
        res.json({
          success: true,
          stats
        });
      } catch (error) {
        console.error('❌ Stats error:', error);
        res.status(500).json({
          success: false,
          error: error.message
        });
      }
    });

    // Manual distribution (for testing/demos)
    router.post('/distribute', async (req, res) => {
      try {
        const { nftMintAddress, amount } = req.body;
        const { LAMPORTS_PER_SOL } = require('@solana/web3.js');

        const result = await this.distributor.handleX402Payment({
          endpoint: 'manual',
          amount: amount * LAMPORTS_PER_SOL,
          currency: 'SOL',
          payer: 'manual',
          metadata: {
            nftMintAddress,
            serviceType: 'manual_trigger',
            timestamp: Date.now()
          }
        });

        res.json({
          success: true,
          message: 'Distribution complete',
          result
        });
      } catch (error) {
        console.error('❌ Distribution error:', error);
        res.status(500).json({
          success: false,
          error: error.message
        });
      }
    });

    // Get distribution history
    router.get('/history/:nftMintAddress', async (req, res) => {
      try {
        const limit = parseInt(req.query.limit) || 10;
        const distributions = await this.options.storage.getDistributions(
          req.params.nftMintAddress,
          limit
        );

        res.json({
          success: true,
          distributions
        });
      } catch (error) {
        console.error('❌ History error:', error);
        res.status(500).json({
          success: false,
          error: error.message
        });
      }
    });

    // Health check
    router.get('/health', (req, res) => {
      res.json({
        status: 'ok',
        service: 'x402',
        version: require('../package.json').version,
        solanaRpc: this.options.solanaRpcUrl,
        mockData: this.options.useMockData
      });
    });

    return router;
  }

  /**
   * Start as standalone server
   */
  async start({ port = 4000, host = '0.0.0.0' } = {}) {
    const app = express();
    
    app.use(cors());
    app.use('/api/x402', this.router);
    
    // Health check at root
    app.get('/health', (req, res) => {
      res.json({
        status: 'ok',
        service: 'OASIS x402 Service',
        version: require('../package.json').version
      });
    });

    return new Promise((resolve) => {
      const server = app.listen(port, host, () => {
        console.log(`🚀 x402 Service running on http://${host}:${port}`);
        console.log(`📊 Health: http://${host}:${port}/health`);
        console.log(`🔌 API: http://${host}:${port}/api/x402`);
        resolve(server);
      });
    });
  }

  /**
   * Register NFT programmatically
   */
  async register(config) {
    return await this.distributor.registerNFTForX402(
      config.nftMintAddress,
      config.paymentEndpoint,
      config.revenueModel,
      config.treasuryWallet
    );
  }

  /**
   * Distribute payment programmatically
   */
  async distributePayment(payment) {
    return await this.distributor.handleX402Payment(payment);
  }

  /**
   * Get statistics programmatically
   */
  async getStats(nftMintAddress) {
    return await this.distributor.getPaymentStats(nftMintAddress);
  }
}

module.exports = X402Service;

