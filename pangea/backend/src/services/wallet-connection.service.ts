import { Injectable, Logger, HttpException, HttpStatus } from '@nestjs/common';
import * as nacl from 'tweetnacl';
import * as bs58 from 'bs58';
import { ethers } from 'ethers';

export interface WalletConnectionRequest {
  walletAddress: string;
  signature: string;
  message: string;
  blockchain: 'solana' | 'ethereum';
}

export interface VerifyWalletResult {
  isValid: boolean;
  walletAddress: string;
  blockchain: string;
}

@Injectable()
export class WalletConnectionService {
  private readonly logger = new Logger(WalletConnectionService.name);

  /**
   * Verify wallet ownership for Solana (Phantom)
   * Verifies that the signature was created by the wallet owner
   */
  async verifySolanaOwnership(
    walletAddress: string,
    signature: string,
    message: string,
  ): Promise<boolean> {
    try {
      // Convert message to bytes
      const messageBytes = new TextEncoder().encode(message);
      
      // Convert signature from base58 or hex string
      let signatureBytes: Uint8Array;
      try {
        // Try base58 decode first (Solana standard)
        signatureBytes = bs58.decode(signature);
      } catch {
        // If base58 fails, try hex
        signatureBytes = Uint8Array.from(Buffer.from(signature.replace('0x', ''), 'hex'));
      }

      // Convert public key from base58 or hex
      let publicKeyBytes: Uint8Array;
      try {
        publicKeyBytes = bs58.decode(walletAddress);
      } catch {
        publicKeyBytes = Uint8Array.from(Buffer.from(walletAddress.replace('0x', ''), 'hex'));
      }

      // Verify signature using nacl (Ed25519)
      const isValid = nacl.sign.detached.verify(messageBytes, signatureBytes, publicKeyBytes);
      
      this.logger.log(`Solana signature verification for ${walletAddress}: ${isValid ? 'VALID' : 'INVALID'}`);
      
      return isValid;
    } catch (error) {
      this.logger.error(`Failed to verify Solana ownership: ${error.message}`, error.stack);
      return false;
    }
  }

  /**
   * Verify wallet ownership for Ethereum (MetaMask)
   * Verifies that the signature was created by the wallet owner
   */
  async verifyEthereumOwnership(
    walletAddress: string,
    signature: string,
    message: string,
  ): Promise<boolean> {
    try {
      // Recover the address from the signature
      const recoveredAddress = ethers.utils.verifyMessage(message, signature);
      
      // Compare addresses (case-insensitive)
      const isValid = recoveredAddress.toLowerCase() === walletAddress.toLowerCase();
      
      this.logger.log(`Ethereum signature verification for ${walletAddress}: ${isValid ? 'VALID' : 'INVALID'}`);
      
      return isValid;
    } catch (error) {
      this.logger.error(`Failed to verify Ethereum ownership: ${error.message}`, error.stack);
      return false;
    }
  }

  /**
   * Verify wallet ownership (generic method)
   */
  async verifyOwnership(request: WalletConnectionRequest): Promise<VerifyWalletResult> {
    const { walletAddress, signature, message, blockchain } = request;

    let isValid = false;

    if (blockchain === 'solana') {
      isValid = await this.verifySolanaOwnership(walletAddress, signature, message);
    } else if (blockchain === 'ethereum') {
      isValid = await this.verifyEthereumOwnership(walletAddress, signature, message);
    } else {
      throw new HttpException(
        `Unsupported blockchain: ${blockchain}`,
        HttpStatus.BAD_REQUEST,
      );
    }

    if (!isValid) {
      throw new HttpException(
        'Invalid signature. Wallet ownership verification failed.',
        HttpStatus.UNAUTHORIZED,
      );
    }

    return {
      isValid: true,
      walletAddress,
      blockchain,
    };
  }

  /**
   * Generate a verification message for the user to sign
   */
  generateVerificationMessage(userId: string, walletAddress: string, blockchain: string): string {
    const timestamp = Date.now();
    return `Please sign this message to verify wallet ownership.\n\nUser ID: ${userId}\nWallet: ${walletAddress}\nBlockchain: ${blockchain}\nTimestamp: ${timestamp}`;
  }

  /**
   * Connect Phantom wallet (frontend will handle the connection, backend verifies)
   */
  async connectPhantom(userId: string, walletAddress: string, signature: string, message: string): Promise<VerifyWalletResult> {
    this.logger.log(`Connecting Phantom wallet ${walletAddress} for user ${userId}`);
    
    return this.verifyOwnership({
      walletAddress,
      signature,
      message,
      blockchain: 'solana',
    });
  }

  /**
   * Connect MetaMask wallet (frontend will handle the connection, backend verifies)
   */
  async connectMetaMask(userId: string, walletAddress: string, signature: string, message: string): Promise<VerifyWalletResult> {
    this.logger.log(`Connecting MetaMask wallet ${walletAddress} for user ${userId}`);
    
    return this.verifyOwnership({
      walletAddress,
      signature,
      message,
      blockchain: 'ethereum',
    });
  }
}












