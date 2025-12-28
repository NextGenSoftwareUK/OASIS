import { Injectable, Logger, HttpException, HttpStatus } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import axios, { AxiosInstance, InternalAxiosRequestConfig } from 'axios';
import { OasisTokenManagerService } from './oasis-token-manager.service';

export interface GenerateWalletRequest {
  avatarId: string;
  providerType: 'SolanaOASIS' | 'EthereumOASIS';
  setAsDefault?: boolean;
}

export interface Wallet {
  walletId: string;
  avatarId: string;
  publicKey: string;
  walletAddress: string;
  providerType: string;
  isDefaultWallet: boolean;
  balance?: number;
  createdAt?: string;
}

export interface WalletBalance {
  walletId: string;
  balance: number;
  tokenSymbol?: string;
  providerType: string;
}

export interface SendTokenRequest {
  fromAvatarId: string;
  toAvatarId: string;
  amount: number;
  tokenSymbol: string;
  providerType: string;
  walletId: string;
}

export interface Transaction {
  transactionId: string;
  walletId: string;
  transactionHash: string;
  type: string;
  amount: number;
  status: string;
  timestamp: string;
}

@Injectable()
export class OasisWalletService {
  private readonly logger = new Logger(OasisWalletService.name);
  private readonly baseUrl: string;
  private readonly axiosInstance: AxiosInstance;

  constructor(
    private configService: ConfigService,
    private tokenManager: OasisTokenManagerService,
  ) {
    this.baseUrl = this.configService.get<string>('OASIS_API_URL') || 'http://api.oasisweb4.com';

    this.axiosInstance = axios.create({
      baseURL: this.baseUrl,
      headers: {
        'Content-Type': 'application/json',
      },
      timeout: 30000,
    });

    // Add request interceptor to inject token dynamically
    this.axiosInstance.interceptors.request.use(
      async (config: InternalAxiosRequestConfig) => {
        try {
          const token = await this.tokenManager.getToken();
          if (token && config.headers) {
            config.headers.Authorization = `Bearer ${token}`;
            this.logger.debug(`Token injected into request: ${config.method?.toUpperCase()} ${config.url}`);
            this.logger.debug(`Token length: ${token.length}, prefix: ${token.substring(0, 30)}...`);
          } else {
            this.logger.warn(`No token available for request: ${config.method?.toUpperCase()} ${config.url}`);
          }
        } catch (error: any) {
          this.logger.error(`Failed to get token for request ${config.method?.toUpperCase()} ${config.url}: ${error.message}`);
          this.logger.error(`Error stack: ${error.stack}`);
          // Continue without token - request will fail with 401 if auth is required
        }
        return config;
      },
      (error) => {
        return Promise.reject(error);
      },
    );

    // Add response interceptor for error handling
    this.axiosInstance.interceptors.response.use(
      (response) => response,
      (error) => {
        const errorData = error.response?.data;
        const statusCode = error.response?.status;
        const requestUrl = error.config?.url;
        const requestMethod = error.config?.method?.toUpperCase();
        
        // Extract error message from various possible locations
        const errorMessage = 
          errorData?.message || 
          errorData?.result?.message || 
          errorData?.Message ||
          errorData?.result?.Message ||
          error.message || 
          'OASIS Wallet API error';
        
        const errorDetails = errorData ? JSON.stringify(errorData, null, 2) : error.stack;
        
        this.logger.error(`OASIS API Error on ${requestMethod} ${requestUrl}: ${errorMessage}`);
        this.logger.error(`Status Code: ${statusCode}`);
        this.logger.error(`Error details: ${errorDetails}`);
        
        // For 405 errors, provide more specific message
        if (statusCode === 405) {
          throw new HttpException(
            `Method not allowed (405) - This usually indicates an authentication issue. Please check OASIS API token is valid. Original error: ${errorMessage}`,
            HttpStatus.INTERNAL_SERVER_ERROR,
          );
        }
        
        throw new HttpException(
          errorMessage,
          statusCode || HttpStatus.INTERNAL_SERVER_ERROR,
        );
      },
    );
  }

  /**
   * Generate a new wallet for an avatar using OASIS Keys API
   * Flow: Generate keypair -> Link private key (creates wallet) -> Link public key (completes wallet)
   */
  async generateWallet(avatarId: string, providerType: 'SolanaOASIS' | 'EthereumOASIS', setAsDefault: boolean = true): Promise<Wallet> {
    try {
      this.logger.log(`Generating wallet for avatar ${avatarId}, provider: ${providerType}`);

      // Step 1: Generate keypair using Keys API
      this.logger.log(`Step 1: Generating keypair for ${providerType}`);
      this.logger.debug(`Request URL: ${this.baseUrl}/api/keys/generate_keypair_for_provider/${providerType}`);
      
      const keypairResponse = await this.axiosInstance.post(
        `/api/keys/generate_keypair_for_provider/${providerType}`,
      );
      
      this.logger.debug(`Step 1 response status: ${keypairResponse.status}`);
      this.logger.debug(`Step 1 response data (first 500 chars): ${JSON.stringify(keypairResponse.data, null, 2).substring(0, 500)}`);

      // Handle nested OASIS response structure
      const keypairResponseData = keypairResponse.data?.result || keypairResponse.data;
      const keypairResult = keypairResponseData?.result || keypairResponseData;
      
      // Check for error in response
      if (keypairResult?.isError === true || keypairResponseData?.isError === true) {
        const errorMsg = keypairResult?.message || keypairResponseData?.message || 'Unknown error';
        throw new Error(`Failed to generate keypair: ${errorMsg}`);
      }

      const keypairData = keypairResult || keypairResponseData;
      if (!keypairData?.privateKey || !keypairData?.publicKey) {
        this.logger.error('Invalid keypair response:', JSON.stringify(keypairResponse.data, null, 2));
        throw new Error('Failed to generate keypair: Invalid response from Keys API - missing privateKey or publicKey');
      }

      const { privateKey, publicKey, walletAddress: generatedWalletAddress } = keypairData;
      this.logger.log(`Keypair generated successfully. Public key: ${publicKey.substring(0, 20)}...`);

      // Step 2: Link private key (this creates the wallet and returns wallet ID)
      this.logger.log(`Step 2: Linking private key to avatar ${avatarId}`);
      this.logger.debug(`Link private key request: AvatarID=${avatarId}, ProviderType=${providerType}, PrivateKey length=${privateKey?.length || 0}`);
      
      const linkPrivateKeyResponse = await this.axiosInstance.post(
        '/api/keys/link_provider_private_key_to_avatar_by_id',
        {
          AvatarID: avatarId,
          ProviderType: providerType,
          ProviderKey: privateKey,
          // WalletId is omitted - this will create a new wallet
        },
      );

      this.logger.debug(`Link private key response status: ${linkPrivateKeyResponse.status}`);
      this.logger.debug(`Link private key response data: ${JSON.stringify(linkPrivateKeyResponse.data, null, 2).substring(0, 500)}`);

      const linkPrivateKeyData = linkPrivateKeyResponse.data?.result || linkPrivateKeyResponse.data;
      const walletId = linkPrivateKeyData?.walletId || linkPrivateKeyData?.id || linkPrivateKeyData?.WalletId;

      if (!walletId) {
        this.logger.error('Failed to get wallet ID from private key linking response');
        this.logger.error('Full response data:', JSON.stringify(linkPrivateKeyResponse.data, null, 2));
        this.logger.error('Extracted linkPrivateKeyData:', JSON.stringify(linkPrivateKeyData, null, 2));
        throw new Error('Failed to create wallet: No wallet ID returned from private key linking');
      }

      this.logger.log(`Wallet created successfully. Wallet ID: ${walletId}`);

      // Step 3: Link public key (completes the wallet setup)
      this.logger.log(`Step 3: Linking public key to wallet ${walletId}`);
      const linkPublicKeyResponse = await this.axiosInstance.post(
        '/api/keys/link_provider_public_key_to_avatar_by_id',
        {
          WalletId: walletId,
          AvatarID: avatarId,
          ProviderType: providerType,
          ProviderKey: publicKey,
          WalletAddress: generatedWalletAddress || publicKey, // Use generated address or fallback to public key
        },
      );

      // Handle nested OASIS response structure and check for errors
      const linkPublicKeyResponseData = linkPublicKeyResponse.data?.result || linkPublicKeyResponse.data;
      const linkPublicKeyResult = linkPublicKeyResponseData?.result || linkPublicKeyResponseData;
      
      if (linkPublicKeyResult?.isError === true || linkPublicKeyResponseData?.isError === true) {
        const errorMsg = linkPublicKeyResult?.message || linkPublicKeyResponseData?.message || 'Unknown error';
        this.logger.warn(`Failed to link public key: ${errorMsg}. Wallet was created but public key linking failed.`);
        // Continue anyway - wallet was created in step 2
      } else {
        this.logger.log(`Public key linked successfully. Wallet setup complete.`);
      }

      // Step 4: Optionally set as default wallet
      if (setAsDefault) {
        try {
          await this.setDefaultWallet(avatarId, walletId, providerType);
          this.logger.log(`Wallet ${walletId} set as default for avatar ${avatarId}`);
        } catch (error) {
          this.logger.warn(`Failed to set wallet as default: ${error.message}. Wallet was still created successfully.`);
          // Don't fail the whole operation if setting default fails
        }
      }

      // Step 5: Try to get wallet details to return complete information
      // If this fails, we'll return what we already know (wallet was successfully created in steps 1-4)
      try {
        const wallets = await this.getWallets(avatarId, providerType);
        const wallet = wallets.find((w) => w.walletId === walletId);

        if (wallet) {
          this.logger.log(`Successfully fetched complete wallet details for ${walletId}`);
          return wallet;
        }
      } catch (error: any) {
        this.logger.warn(`Failed to fetch wallet details after creation (wallet was still created successfully): ${error.message}`);
        // Continue to return fallback data - wallet was successfully created in steps 1-4
      }

      // Return what we know - wallet was successfully created even if we can't fetch full details
      this.logger.log(`Returning wallet details from creation response (walletId: ${walletId})`);
      return {
        walletId: walletId,
        avatarId: avatarId,
        publicKey: publicKey,
        walletAddress: generatedWalletAddress || publicKey,
        providerType: providerType,
        isDefaultWallet: setAsDefault,
        balance: 0,
        createdAt: new Date().toISOString(),
      };
    } catch (error: any) {
      this.logger.error(`Failed to generate wallet: ${error.message}`, error.stack);
      if (error.response?.data) {
        this.logger.error(`OASIS API error response: ${JSON.stringify(error.response.data, null, 2)}`);
      }
      throw error;
    }
  }

  /**
   * Get all wallets for an avatar
   * OASIS API endpoint: GET /api/wallet/avatar/{id}/wallets/{showOnlyDefault}/{decryptPrivateKeys}
   */
  async getWallets(avatarId: string, providerType?: string): Promise<Wallet[]> {
    try {
      // OASIS API requires path parameters: showOnlyDefault (false) and decryptPrivateKeys (false)
      const url = `/api/wallet/avatar/${avatarId}/wallets/false/false`;
      // Note: providerType is passed as a query parameter in the OASIS API method signature,
      // but the route doesn't include it. We'll include it as a query param anyway.
      const params = providerType ? `?providerType=${providerType}` : '';
      
      const response = await this.axiosInstance.get(`${url}${params}`);
      const result = response.data?.result || response.data || {};
      
      // OASIS returns a dictionary: { SolanaOASIS: [wallets...], EthereumOASIS: [wallets...] }
      // We need to flatten it into an array
      let wallets: any[] = [];
      
      if (typeof result === 'object' && !Array.isArray(result)) {
        // It's a dictionary, flatten it
        Object.values(result).forEach((walletArray: any) => {
          if (Array.isArray(walletArray)) {
            wallets = wallets.concat(walletArray);
          }
        });
      } else if (Array.isArray(result)) {
        wallets = result;
      }

      return wallets.map((w: any) => this.mapWalletResponse(w, avatarId));
    } catch (error) {
      this.logger.error(`Failed to get wallets: ${error.message}`, error.stack);
      throw error;
    }
  }

  /**
   * Get default wallet for an avatar
   */
  async getDefaultWallet(avatarId: string): Promise<Wallet | null> {
    try {
      const response = await this.axiosInstance.get(
        `/api/wallet/avatar/${avatarId}/default-wallet`,
      );

      const result = response.data?.result || response.data;
      if (!result) {
        return null;
      }

      return this.mapWalletResponse(result, avatarId);
    } catch (error) {
      this.logger.error(`Failed to get default wallet: ${error.message}`, error.stack);
      // Return null if wallet not found (404)
      if (error.response?.status === 404) {
        return null;
      }
      throw error;
    }
  }

  /**
   * Set default wallet for an avatar
   * POST /api/wallet/avatar/{avatarId}/default-wallet/{walletId}?providerType={providerType}
   */
  async setDefaultWallet(avatarId: string, walletId: string, providerType: string): Promise<void> {
    try {
      await this.axiosInstance.post(
        `/api/wallet/avatar/${avatarId}/default-wallet/${walletId}?providerType=${providerType}`,
      );

      this.logger.log(`Set default wallet ${walletId} for avatar ${avatarId}, provider: ${providerType}`);
    } catch (error) {
      this.logger.error(`Failed to set default wallet: ${error.message}`, error.stack);
      throw error;
    }
  }

  /**
   * Get balance for a wallet
   * OASIS API endpoint: GET /api/wallet/{walletId}/balance
   */
  async getBalance(walletId: string, providerType: string | number): Promise<WalletBalance> {
    try {
      // Convert providerType enum number to string if needed
      const providerTypeStr = this.convertProviderTypeToString(providerType);
      
      // Correct endpoint format: /api/wallet/{walletId}/balance (not /api/wallet/balance/{walletId})
      const url = `/api/wallet/${walletId}/balance${providerTypeStr ? `?providerType=${providerTypeStr}` : ''}`;
      
      this.logger.debug(`Getting balance for wallet ${walletId}, providerType: ${providerTypeStr}`);
      
      const response = await this.axiosInstance.get(url);

      const result = response.data?.result || response.data;
      
      return {
        walletId,
        balance: parseFloat(result.balance || result.amount || result.data?.balance || '0'),
        tokenSymbol: result.tokenSymbol || result.symbol || result.data?.currency,
        providerType: result.providerType || providerTypeStr || String(providerType),
      };
    } catch (error) {
      this.logger.error(`Failed to get balance for wallet ${walletId}: ${error.message}`, error.stack);
      throw error;
    }
  }

  /**
   * Convert providerType enum number to string name
   * OASIS ProviderType enum: SolanaOASIS = 3, EthereumOASIS = 4, etc.
   */
  private convertProviderTypeToString(providerType: string | number): string {
    if (typeof providerType === 'string') {
      return providerType;
    }
    
    // Map enum numbers to string names
    const providerTypeMap: Record<number, string> = {
      3: 'SolanaOASIS',
      4: 'EthereumOASIS',
      5: 'PolygonOASIS',
      6: 'ArbitrumOASIS',
      7: 'ZcashOASIS',
    };
    
    return providerTypeMap[providerType] || `ProviderType_${providerType}`;
  }

  /**
   * Refresh balance for a wallet
   */
  async refreshBalance(walletId: string, providerType: string): Promise<WalletBalance> {
    try {
      const response = await this.axiosInstance.post('/api/wallet/refresh_balance', {
        walletId,
        providerType,
      });

      const result = response.data?.result || response.data;
      
      return {
        walletId,
        balance: parseFloat(result.balance || result.amount || '0'),
        tokenSymbol: result.tokenSymbol || result.symbol,
        providerType: result.providerType || providerType,
      };
    } catch (error) {
      this.logger.error(`Failed to refresh balance: ${error.message}`, error.stack);
      throw error;
    }
  }

  /**
   * Send tokens between avatars
   */
  async sendToken(request: SendTokenRequest): Promise<{ transactionHash: string; success: boolean }> {
    try {
      this.logger.log(`Sending tokens: ${request.amount} ${request.tokenSymbol} from ${request.fromAvatarId} to ${request.toAvatarId}`);

      const response = await this.axiosInstance.post('/api/wallet/send_token', request);

      const result = response.data?.result || response.data;
      
      return {
        transactionHash: result.transactionHash || result.txHash || result.hash,
        success: true,
      };
    } catch (error) {
      this.logger.error(`Failed to send token: ${error.message}`, error.stack);
      throw error;
    }
  }

  /**
   * Get transaction history for a wallet
   */
  async getTransactions(walletId: string, limit: number = 50, offset: number = 0): Promise<Transaction[]> {
    try {
      const response = await this.axiosInstance.get(
        `/api/wallet/transactions/${walletId}?limit=${limit}&offset=${offset}`,
      );

      const transactions = response.data?.result || response.data || [];
      
      return Array.isArray(transactions)
        ? transactions.map((tx: any) => this.mapTransactionResponse(tx))
        : [];
    } catch (error) {
      this.logger.error(`Failed to get transactions: ${error.message}`, error.stack);
      throw error;
    }
  }

  /**
   * Get wallet by public key
   */
  async getWalletByPublicKey(publicKey: string): Promise<Wallet | null> {
    try {
      const response = await this.axiosInstance.get(
        `/api/wallet/wallet_by_public_key/${publicKey}`,
      );

      const result = response.data?.result || response.data;
      if (!result) {
        return null;
      }

      return this.mapWalletResponse(result, result.avatarId);
    } catch (error) {
      this.logger.error(`Failed to get wallet by public key: ${error.message}`, error.stack);
      // Return null if wallet not found (404)
      if (error.response?.status === 404) {
        return null;
      }
      throw error;
    }
  }

  /**
   * Map wallet API response to Wallet interface
   */
  private mapWalletResponse(data: any, avatarId: string): Wallet {
    // Convert providerType enum number to string if needed
    const providerType = typeof data.providerType === 'number' 
      ? this.convertProviderTypeToString(data.providerType)
      : data.providerType;
    
    return {
      walletId: data.walletId || data.id,
      avatarId: data.avatarId || avatarId,
      publicKey: data.publicKey,
      walletAddress: data.walletAddress || data.address || data.publicKey,
      providerType: providerType,
      isDefaultWallet: data.isDefaultWallet ?? false,
      balance: data.balance ? parseFloat(data.balance) : undefined,
      createdAt: data.createdAt || data.created_at,
    };
  }

  /**
   * Map transaction API response to Transaction interface
   */
  private mapTransactionResponse(data: any): Transaction {
    return {
      transactionId: data.transactionId || data.id,
      walletId: data.walletId,
      transactionHash: data.transactionHash || data.hash || data.txHash,
      type: data.type || data.transactionType,
      amount: parseFloat(data.amount || '0'),
      status: data.status,
      timestamp: data.timestamp || data.createdAt || data.created_at,
    };
  }
}










