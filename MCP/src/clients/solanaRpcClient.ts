import {
  Connection,
  Keypair,
  PublicKey,
  Transaction,
  SystemProgram,
  sendAndConfirmTransaction,
  Cluster,
} from '@solana/web3.js';
import bs58 from 'bs58';

export interface SendSolRequest {
  fromPrivateKey: string; // Base58 encoded private key
  toAddress: string; // Recipient public key
  amount: number; // Amount in SOL (not lamports)
  network?: 'devnet' | 'mainnet-beta' | 'testnet'; // Default: devnet
}

export interface SendSolResponse {
  success: boolean;
  transactionSignature?: string;
  fromAddress?: string;
  toAddress: string;
  amount: number;
  network: string;
  error?: string;
}

export class SolanaRpcClient {
  /**
   * Send SOL from one wallet to another using Solana RPC
   * @param request SendSolRequest with fromPrivateKey, toAddress, amount, and optional network
   * @returns SendSolResponse with transaction signature
   */
  async sendSol(request: SendSolRequest): Promise<SendSolResponse> {
    try {
      const network = request.network || 'devnet';
      const rpcUrl = this.getRpcUrl(network);
      const connection = new Connection(rpcUrl, 'confirmed');

      // Decode private key from base58
      // bs58 uses default export with decode method
      const secretKey = (bs58 as any).default.decode(request.fromPrivateKey);
      const fromKeypair = Keypair.fromSecretKey(secretKey);
      const fromPubkey = fromKeypair.publicKey;
      const toPubkey = new PublicKey(request.toAddress);

      // Validate addresses
      if (!PublicKey.isOnCurve(fromPubkey)) {
        throw new Error('Invalid from private key - public key not on curve');
      }
      if (!PublicKey.isOnCurve(toPubkey)) {
        throw new Error('Invalid to address - not a valid Solana public key');
      }

      // Validate amount
      if (request.amount <= 0) {
        throw new Error('Amount must be greater than 0');
      }

      // Convert SOL to lamports (1 SOL = 1,000,000,000 lamports)
      const lamports = request.amount * 1000000000;

      // Create transaction
      const transaction = new Transaction().add(
        SystemProgram.transfer({
          fromPubkey: fromPubkey,
          toPubkey: toPubkey,
          lamports: lamports,
        })
      );

      // Send and confirm transaction
      const signature = await sendAndConfirmTransaction(
        connection,
        transaction,
        [fromKeypair],
        {
          commitment: 'confirmed',
        }
      );

      return {
        success: true,
        transactionSignature: signature,
        fromAddress: fromPubkey.toString(),
        toAddress: request.toAddress,
        amount: request.amount,
        network: network,
      };
    } catch (error: any) {
      return {
        success: false,
        toAddress: request.toAddress,
        amount: request.amount,
        network: request.network || 'devnet',
        error: error.message || 'Unknown error occurred',
      };
    }
  }

  /**
   * Get RPC URL for a given network
   */
  private getRpcUrl(network: string): string {
    switch (network) {
      case 'mainnet-beta':
        return 'https://api.mainnet-beta.solana.com';
      case 'testnet':
        return 'https://api.testnet.solana.com';
      case 'devnet':
      default:
        return 'https://api.devnet.solana.com';
    }
  }

  /**
   * Get balance for a Solana address
   */
  async getBalance(
    address: string,
    network: 'devnet' | 'mainnet-beta' | 'testnet' = 'devnet'
  ): Promise<{ balance: number; lamports: number; address: string }> {
    try {
      const rpcUrl = this.getRpcUrl(network);
      const connection = new Connection(rpcUrl, 'confirmed');
      const pubkey = new PublicKey(address);
      const lamports = await connection.getBalance(pubkey);

      return {
        balance: lamports / 1000000000, // Convert to SOL
        lamports: lamports,
        address: address,
      };
    } catch (error: any) {
      throw new Error(`Failed to get balance: ${error.message}`);
    }
  }

  /**
   * Get transaction details
   */
  async getTransaction(
    signature: string,
    network: 'devnet' | 'mainnet-beta' | 'testnet' = 'devnet'
  ): Promise<any> {
    try {
      const rpcUrl = this.getRpcUrl(network);
      const connection = new Connection(rpcUrl, 'confirmed');
      const transaction = await connection.getTransaction(signature, {
        maxSupportedTransactionVersion: 0,
      });

      return transaction;
    } catch (error: any) {
      throw new Error(`Failed to get transaction: ${error.message}`);
    }
  }
}

