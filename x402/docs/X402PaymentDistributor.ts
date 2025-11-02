/**
 * OASIS x402 Payment Distribution Service
 * 
 * Integrates x402 protocol with OASIS NFT infrastructure to enable
 * automatic revenue distribution to NFT holders
 * 
 * For x402 Solana Hackathon
 */

import { Connection, PublicKey, Transaction, SystemProgram, LAMPORTS_PER_SOL } from '@solana/web3.js';
import { getAssociatedTokenAddress, TOKEN_PROGRAM_ID } from '@solana/spl-token';
import axios from 'axios';

// x402 Protocol Configuration
interface X402PaymentEvent {
  endpoint: string;
  amount: number; // in lamports
  currency: 'SOL' | 'USDC';
  payer: string; // wallet address
  metadata: {
    nftMintAddress: string;
    serviceType: string;
    timestamp: number;
  };
}

interface NFTHolderInfo {
  walletAddress: string;
  tokenAccount: string;
  sharePercentage: number;
}

interface X402Config {
  solanaRpcUrl: string;
  oasisApiUrl: string;
  oasisApiKey: string;
  x402WebhookUrl?: string;
}

export class X402PaymentDistributor {
  private connection: Connection;
  private config: X402Config;
  
  constructor(config: X402Config) {
    this.config = config;
    this.connection = new Connection(config.solanaRpcUrl, 'confirmed');
  }

  /**
   * Initialize x402 payment endpoint for an NFT
   * This embeds the payment URL in NFT metadata
   */
  async registerNFTForX402(
    nftMintAddress: string,
    paymentEndpoint: string,
    revenueModel: 'equal' | 'weighted' | 'creator-split'
  ): Promise<{ success: boolean; x402Url: string }> {
    
    console.log(`🔧 Registering NFT ${nftMintAddress} with x402 protocol...`);
    
    // Create x402 payment URL
    const x402Url = `${paymentEndpoint}?nft=${nftMintAddress}&protocol=x402`;
    
    // Update NFT metadata to include x402 payment info
    const metadataUpdate = {
      x402: {
        paymentUrl: x402Url,
        revenueModel: revenueModel,
        distributionEnabled: true,
        protocol: 'x402-v1'
      }
    };
    
    // Store in OASIS API
    try {
      await axios.post(`${this.config.oasisApiUrl}/api/nft/update-metadata`, {
        nftMintAddress,
        metadata: metadataUpdate
      }, {
        headers: {
          'Authorization': `Bearer ${this.config.oasisApiKey}`,
          'Content-Type': 'application/json'
        }
      });
      
      console.log(`✅ NFT registered with x402: ${x402Url}`);
      return { success: true, x402Url };
      
    } catch (error) {
      console.error('❌ Failed to register x402 endpoint:', error);
      throw error;
    }
  }

  /**
   * Handle incoming x402 payment and distribute to NFT holders
   * This is called by your x402 webhook
   */
  async handleX402Payment(paymentEvent: X402PaymentEvent): Promise<{
    success: boolean;
    distributionTx?: string;
    recipients: number;
    amountPerHolder: number;
  }> {
    
    console.log(`💰 Processing x402 payment: ${paymentEvent.amount} lamports`);
    console.log(`🎯 Target NFT: ${paymentEvent.metadata.nftMintAddress}`);
    
    try {
      // Step 1: Get all NFT holders
      const holders = await this.getNFTHolders(paymentEvent.metadata.nftMintAddress);
      
      if (holders.length === 0) {
        throw new Error('No NFT holders found');
      }
      
      console.log(`👥 Found ${holders.length} NFT holders`);
      
      // Step 2: Calculate distribution (equal split for demo)
      const totalAmount = paymentEvent.amount;
      const platformFee = totalAmount * 0.025; // 2.5% OASIS fee
      const distributionAmount = totalAmount - platformFee;
      const amountPerHolder = Math.floor(distributionAmount / holders.length);
      
      console.log(`💵 Amount per holder: ${amountPerHolder / LAMPORTS_PER_SOL} SOL`);
      
      // Step 3: Execute distribution transaction
      const txSignature = await this.distributePayments(
        holders,
        amountPerHolder,
        paymentEvent.currency
      );
      
      console.log(`✅ Distribution complete! Tx: ${txSignature}`);
      
      // Step 4: Record in OASIS analytics
      await this.recordDistribution(
        paymentEvent.metadata.nftMintAddress,
        totalAmount,
        holders.length,
        txSignature
      );
      
      return {
        success: true,
        distributionTx: txSignature,
        recipients: holders.length,
        amountPerHolder: amountPerHolder / LAMPORTS_PER_SOL
      };
      
    } catch (error) {
      console.error('❌ Payment distribution failed:', error);
      throw error;
    }
  }

  /**
   * Get all current holders of an NFT collection
   * Uses Solana RPC and OASIS API
   */
  private async getNFTHolders(nftMintAddress: string): Promise<NFTHolderInfo[]> {
    try {
      // Query OASIS API for NFT holders (cached data)
      const response = await axios.get(
        `${this.config.oasisApiUrl}/api/nft/holders/${nftMintAddress}`,
        {
          headers: {
            'Authorization': `Bearer ${this.config.oasisApiKey}`
          }
        }
      );
      
      if (response.data.result && response.data.result.holders) {
        return response.data.result.holders.map((holder: any) => ({
          walletAddress: holder.walletAddress,
          tokenAccount: holder.tokenAccount,
          sharePercentage: 1.0 / response.data.result.holders.length // Equal split
        }));
      }
      
      // Fallback: Query blockchain directly
      return await this.queryHoldersFromChain(nftMintAddress);
      
    } catch (error) {
      console.error('Error fetching holders from OASIS API, querying chain...', error);
      return await this.queryHoldersFromChain(nftMintAddress);
    }
  }

  /**
   * Query Solana blockchain directly for token holders
   */
  private async queryHoldersFromChain(mintAddress: string): Promise<NFTHolderInfo[]> {
    const mintPublicKey = new PublicKey(mintAddress);
    
    // Get all token accounts for this mint
    const tokenAccounts = await this.connection.getParsedProgramAccounts(
      TOKEN_PROGRAM_ID,
      {
        filters: [
          {
            dataSize: 165, // Token account size
          },
          {
            memcmp: {
              offset: 0,
              bytes: mintPublicKey.toBase58(),
            },
          },
        ],
      }
    );
    
    const holders: NFTHolderInfo[] = [];
    
    for (const accountInfo of tokenAccounts) {
      const parsedData = (accountInfo.account.data as any).parsed;
      if (parsedData && parsedData.info.tokenAmount.uiAmount > 0) {
        holders.push({
          walletAddress: parsedData.info.owner,
          tokenAccount: accountInfo.pubkey.toBase58(),
          sharePercentage: 1.0 / tokenAccounts.length
        });
      }
    }
    
    return holders;
  }

  /**
   * Execute payment distribution to multiple holders
   */
  private async distributePayments(
    holders: NFTHolderInfo[],
    amountPerHolder: number,
    currency: 'SOL' | 'USDC'
  ): Promise<string> {
    
    // For SOL distribution (native token)
    if (currency === 'SOL') {
      return await this.distributeSol(holders, amountPerHolder);
    }
    
    // For USDC distribution (SPL token)
    return await this.distributeSplToken(holders, amountPerHolder, 'USDC');
  }

  /**
   * Distribute SOL to holders
   */
  private async distributeSol(
    holders: NFTHolderInfo[],
    amountPerHolder: number
  ): Promise<string> {
    
    console.log(`💸 Distributing ${amountPerHolder / LAMPORTS_PER_SOL} SOL to ${holders.length} holders...`);
    
    // Create transaction with multiple transfer instructions
    const transaction = new Transaction();
    
    // Note: In production, you'd use the OASIS treasury wallet here
    // For demo, this would be funded by the payment receiver
    const fromPubkey = new PublicKey('HT2sbYb6qjYKNjSdSWkwCp6bfYtrW9LMaGsnevLRRVnB'); // OASIS wallet
    
    for (const holder of holders) {
      const toPubkey = new PublicKey(holder.walletAddress);
      
      transaction.add(
        SystemProgram.transfer({
          fromPubkey,
          toPubkey,
          lamports: amountPerHolder,
        })
      );
    }
    
    // In production, sign and send this transaction
    // For POC, we'll simulate and return a mock signature
    console.log(`📝 Transaction created with ${transaction.instructions.length} transfers`);
    
    // Mock signature for demo
    return 'x402_distribution_' + Date.now().toString(36) + Math.random().toString(36);
    
    // Production code would be:
    // const signature = await this.connection.sendTransaction(transaction, [payerKeypair]);
    // await this.connection.confirmTransaction(signature);
    // return signature;
  }

  /**
   * Distribute SPL token (USDC) to holders
   */
  private async distributeSplToken(
    holders: NFTHolderInfo[],
    amount: number,
    tokenSymbol: string
  ): Promise<string> {
    
    console.log(`💸 Distributing ${amount} ${tokenSymbol} to ${holders.length} holders...`);
    
    // For POC, return mock signature
    // Production implementation would use SPL Token transfers
    return 'x402_spl_distribution_' + Date.now().toString(36);
  }

  /**
   * Record distribution in OASIS analytics
   */
  private async recordDistribution(
    nftMintAddress: string,
    totalAmount: number,
    recipientCount: number,
    txSignature: string
  ): Promise<void> {
    
    try {
      await axios.post(`${this.config.oasisApiUrl}/api/analytics/x402-distribution`, {
        nftMintAddress,
        totalAmount,
        recipientCount,
        txSignature,
        timestamp: Date.now(),
        protocol: 'x402'
      }, {
        headers: {
          'Authorization': `Bearer ${this.config.oasisApiKey}`,
          'Content-Type': 'application/json'
        }
      });
      
      console.log('📊 Distribution recorded in OASIS analytics');
      
    } catch (error) {
      console.error('⚠️  Failed to record analytics:', error);
      // Non-critical - don't throw
    }
  }

  /**
   * Get x402 payment statistics for an NFT
   */
  async getPaymentStats(nftMintAddress: string): Promise<{
    totalDistributed: number;
    distributionCount: number;
    holderCount: number;
    averagePerDistribution: number;
  }> {
    
    const response = await axios.get(
      `${this.config.oasisApiUrl}/api/analytics/x402-stats/${nftMintAddress}`,
      {
        headers: {
          'Authorization': `Bearer ${this.config.oasisApiKey}`
        }
      }
    );
    
    return response.data.result;
  }
}

/**
 * Express.js webhook handler example
 */
export function createX402WebhookHandler(distributor: X402PaymentDistributor) {
  return async (req: any, res: any) => {
    try {
      const paymentEvent: X402PaymentEvent = req.body;
      
      // Validate x402 signature (important for production)
      // const isValid = validateX402Signature(req.headers['x-x402-signature'], req.body);
      // if (!isValid) {
      //   return res.status(401).json({ error: 'Invalid signature' });
      // }
      
      const result = await distributor.handleX402Payment(paymentEvent);
      
      res.json({
        success: true,
        message: 'Payment distributed successfully',
        ...result
      });
      
    } catch (error: any) {
      console.error('Webhook error:', error);
      res.status(500).json({
        success: false,
        error: error.message
      });
    }
  };
}

// Export singleton instance
export default X402PaymentDistributor;

