/**
 * MCP Tools for Solana Wallet Creation
 * 
 * This module provides an MCP endpoint for creating Solana wallets for avatars.
 * Works for both regular avatars and agent avatars (the process is identical).
 * It follows the correct order as specified in SOLANA_WALLET_CREATION_GUIDE.md:
 * 1. Register provider (if needed)
 * 2. Activate provider (if needed)
 * 3. Generate keypair
 * 4. Link public key FIRST (creates wallet)
 * 5. Link private key SECOND (completes wallet)
 */

import axios, { AxiosInstance } from 'axios';

export interface SolanaWalletCreationResult {
  walletId: string;
  walletAddress: string;
  publicKey: string;
  providerType: string;
  avatarId: string;
  isDefaultWallet?: boolean;
}

export interface SolanaWalletCreationOptions {
  avatarId: string;
  setAsDefault?: boolean;
  ensureProviderActivated?: boolean;
}

export class SolanaWalletMCPTools {
  private oasisApiUrl: string;
  private axiosInstance: AxiosInstance;

  constructor(oasisApiUrl: string, getAuthToken: () => Promise<string>) {
    this.oasisApiUrl = oasisApiUrl;
    this.axiosInstance = axios.create({
      baseURL: oasisApiUrl,
      headers: {
        'Content-Type': 'application/json',
      },
      timeout: 30000,
    });

    // Add request interceptor to inject auth token
    this.axiosInstance.interceptors.request.use(
      async (config) => {
        try {
          const token = await getAuthToken();
          if (token && config.headers) {
            config.headers.Authorization = `Bearer ${token}`;
          }
        } catch (error) {
          console.warn('Failed to get auth token:', error);
        }
        return config;
      },
      (error) => Promise.reject(error)
    );
  }

  /**
   * Ensure Solana provider is registered and activated
   */
  private async ensureProviderActivated(): Promise<void> {
    try {
      // Try to register provider (may already be registered)
      try {
        await this.axiosInstance.post('/api/provider/register-provider-type/SolanaOASIS');
      } catch (error: any) {
        // Ignore errors if already registered
        if (error.response?.status !== 400 && error.response?.status !== 409) {
          console.warn('Provider registration warning:', error.message);
        }
      }

      // Try to activate provider (may already be activated)
      try {
        await this.axiosInstance.post('/api/provider/activate-provider/SolanaOASIS');
      } catch (error: any) {
        // Ignore errors if already activated
        if (error.response?.status !== 400 && error.response?.status !== 409) {
          console.warn('Provider activation warning:', error.message);
        }
      }
    } catch (error: any) {
      throw new Error(`Failed to ensure Solana provider is activated: ${error.message}`);
    }
  }

  /**
   * Generate Solana keypair
   */
  private async generateKeypair(): Promise<{
    privateKey: string;
    publicKey: string;
    walletAddress: string;
  }> {
    try {
      const response = await this.axiosInstance.post(
        '/api/keys/generate_keypair_with_wallet_address_for_provider/SolanaOASIS'
      );

      const result = response.data?.result || response.data;
      
      if (result?.isError === true) {
        throw new Error(result.message || 'Failed to generate keypair');
      }

      const keypairData = result?.result || result;
      
      if (!keypairData?.privateKey || !keypairData?.publicKey) {
        throw new Error('Invalid keypair response: missing privateKey or publicKey');
      }

      return {
        privateKey: keypairData.privateKey,
        publicKey: keypairData.publicKey,
        walletAddress: keypairData.walletAddressLegacy || keypairData.publicKey,
      };
    } catch (error: any) {
      throw new Error(`Failed to generate Solana keypair: ${error.message}`);
    }
  }

  /**
   * Link public key FIRST (creates wallet) - CRITICAL ORDER for Solana
   */
  private async linkPublicKey(
    avatarId: string,
    publicKey: string,
    walletAddress: string
  ): Promise<string> {
    try {
      const response = await this.axiosInstance.post(
        '/api/keys/link_provider_public_key_to_avatar_by_id',
        {
          AvatarID: avatarId,
          ProviderType: 'SolanaOASIS',
          ProviderKey: publicKey,
          WalletAddress: walletAddress,
          // Omit WalletId to create a new wallet
        }
      );

      const result = response.data?.result || response.data;
      const linkResult = result?.result || result;

      if (linkResult?.isError === true || result?.isError === true) {
        throw new Error(linkResult?.message || result?.message || 'Failed to link public key');
      }

      const walletId = linkResult?.walletId || linkResult?.id;
      
      if (!walletId) {
        throw new Error('Failed to get wallet ID from public key linking response');
      }

      return walletId;
    } catch (error: any) {
      throw new Error(`Failed to link public key: ${error.message}`);
    }
  }

  /**
   * Link private key SECOND (completes wallet) - CRITICAL ORDER for Solana
   */
  private async linkPrivateKey(
    walletId: string,
    avatarId: string,
    privateKey: string
  ): Promise<void> {
    try {
      const response = await this.axiosInstance.post(
        '/api/keys/link_provider_private_key_to_avatar_by_id',
        {
          WalletId: walletId, // REQUIRED - from public key linking step
          AvatarID: avatarId,
          ProviderType: 'SolanaOASIS',
          ProviderKey: privateKey,
        }
      );

      const result = response.data?.result || response.data;
      const linkResult = result?.result || result;

      if (linkResult?.isError === true || result?.isError === true) {
        const errorMsg = linkResult?.message || result?.message || 'Failed to link private key';
        throw new Error(errorMsg);
      }
    } catch (error: any) {
      throw new Error(`Failed to link private key: ${error.message}`);
    }
  }

  /**
   * Set wallet as default (optional)
   */
  private async setDefaultWallet(
    avatarId: string,
    walletId: string
  ): Promise<void> {
    try {
      await this.axiosInstance.post(
        `/api/wallet/avatar/${avatarId}/default-wallet/${walletId}?providerType=SolanaOASIS`
      );
    } catch (error: any) {
      // Don't fail the whole operation if setting default fails
      console.warn(`Failed to set wallet as default: ${error.message}`);
    }
  }

  /**
   * Create Solana wallet for an avatar (works for both regular avatars and agent avatars)
   * 
   * MCP Tool: oasis_create_solana_wallet
   */
  async createSolanaWallet(
    options: SolanaWalletCreationOptions
  ): Promise<SolanaWalletCreationResult> {
    const { avatarId, setAsDefault = true, ensureProviderActivated = true } = options;

    try {
      // Step 1: Ensure provider is activated (if requested)
      if (ensureProviderActivated) {
        await this.ensureProviderActivated();
      }

      // Step 2: Generate keypair
      const { privateKey, publicKey, walletAddress } = await this.generateKeypair();

      // Step 3: Link public key FIRST (creates wallet)
      const walletId = await this.linkPublicKey(avatarId, publicKey, walletAddress);

      // Step 4: Link private key SECOND (completes wallet)
      await this.linkPrivateKey(walletId, avatarId, privateKey);

      // Step 5: Set as default wallet (if requested)
      if (setAsDefault) {
        await this.setDefaultWallet(avatarId, walletId);
      }

      return {
        walletId,
        walletAddress,
        publicKey,
        providerType: 'SolanaOASIS',
        avatarId,
        isDefaultWallet: setAsDefault,
      };
    } catch (error: any) {
      throw new Error(`Failed to create Solana wallet: ${error.message}`);
    }
  }
}

/**
 * MCP Tool Definitions
 * 
 * These should be registered with your MCP server
 */
export const solanaWalletMCPToolDefinitions = [
  {
    name: 'oasis_create_solana_wallet',
    description: 'Create a Solana wallet for an avatar (works for both regular avatars and agent avatars). Follows the correct order: generates keypair, links public key first (creates wallet), then links private key (completes wallet). Automatically ensures Solana provider is registered and activated.',
    inputSchema: {
      type: 'object',
      properties: {
        avatarId: {
          type: 'string',
          description: 'The avatar ID (UUID) to create the wallet for. Works for both regular avatars and agent avatars.',
        },
        setAsDefault: {
          type: 'boolean',
          description: 'Whether to set this wallet as the default wallet for the avatar (default: true)',
          default: true,
        },
        ensureProviderActivated: {
          type: 'boolean',
          description: 'Whether to ensure Solana provider is registered and activated before creating wallet (default: true)',
          default: true,
        },
      },
      required: ['avatarId'],
    },
  },
];

/**
 * Example usage in MCP server handler:
 * 
 * ```typescript
 * import { SolanaWalletMCPTools, solanaWalletMCPToolDefinitions } from './solana-wallet-tools';
 * 
 * // Initialize
 * const solanaTools = new SolanaWalletMCPTools(
 *   process.env.OASIS_API_URL || 'https://api.oasisweb4.com',
 *   async () => {
 *     // Your token retrieval logic
 *     return await getAuthToken();
 *   }
 * );
 * 
 * // Register tools
 * for (const toolDef of solanaWalletMCPToolDefinitions) {
 *   mcpServer.addTool(toolDef);
 * }
 * 
 * // Handle tool calls
 * mcpServer.onToolCall(async (toolName, args) => {
 *   if (toolName === 'oasis_create_solana_wallet') {
 *     return await solanaTools.createSolanaWallet(args);
 *   }
 * });
 * ```
 */
