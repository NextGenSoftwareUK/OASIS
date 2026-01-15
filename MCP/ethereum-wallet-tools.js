/**
 * MCP Tools for Ethereum Wallet Creation
 *
 * This module provides an MCP endpoint for creating Ethereum wallets for avatars.
 * Works for both regular avatars and agent avatars (the process is identical).
 * It follows the same pattern as Solana wallet creation:
 * 1. Register provider (if needed)
 * 2. Activate provider (if needed)
 * 3. Generate keypair
 * 4. Link public key FIRST (creates wallet)
 * 5. Link private key SECOND (completes wallet)
 */
import axios from 'axios';
export class EthereumWalletMCPTools {
    oasisApiUrl;
    axiosInstance;
    constructor(oasisApiUrl, getAuthToken) {
        this.oasisApiUrl = oasisApiUrl;
        this.axiosInstance = axios.create({
            baseURL: oasisApiUrl,
            headers: {
                'Content-Type': 'application/json',
            },
            timeout: 30000,
        });
        // Add request interceptor to inject auth token
        this.axiosInstance.interceptors.request.use(async (config) => {
            try {
                const token = await getAuthToken();
                if (token && config.headers) {
                    config.headers.Authorization = `Bearer ${token}`;
                }
            }
            catch (error) {
                console.warn('Failed to get auth token:', error);
            }
            return config;
        }, (error) => Promise.reject(error));
    }
    /**
     * Ensure Ethereum provider is registered and activated
     */
    async ensureProviderActivated() {
        try {
            // Try to register provider (may already be registered)
            try {
                await this.axiosInstance.post('/api/provider/register-provider-type/EthereumOASIS');
            }
            catch (error) {
                // Ignore errors if already registered
                if (error.response?.status !== 400 && error.response?.status !== 409) {
                    console.warn('Provider registration warning:', error.message);
                }
            }
            // Try to activate provider (may already be activated)
            try {
                await this.axiosInstance.post('/api/provider/activate-provider/EthereumOASIS');
            }
            catch (error) {
                // Ignore errors if already activated
                if (error.response?.status !== 400 && error.response?.status !== 409) {
                    console.warn('Provider activation warning:', error.message);
                }
            }
        }
        catch (error) {
            throw new Error(`Failed to ensure Ethereum provider is activated: ${error.message}`);
        }
    }
    /**
     * Generate Ethereum keypair
     */
    async generateKeypair() {
        try {
            const response = await this.axiosInstance.post('/api/keys/generate_keypair_with_wallet_address_for_provider/EthereumOASIS');
            const result = response.data?.result || response.data;
            if (result?.isError === true) {
                throw new Error(result.message || 'Failed to generate keypair');
            }
            const keypairData = result?.result || result;
            if (!keypairData?.privateKey || !keypairData?.publicKey) {
                throw new Error('Invalid keypair response: missing privateKey or publicKey');
            }
            // For Ethereum, the public key IS the wallet address (from GetPublicAddress())
            // But we also check walletAddressLegacy as a fallback
            const walletAddress = keypairData.walletAddressLegacy || keypairData.publicKey;
            return {
                privateKey: keypairData.privateKey,
                publicKey: keypairData.publicKey,
                walletAddress: walletAddress,
            };
        }
        catch (error) {
            throw new Error(`Failed to generate Ethereum keypair: ${error.message}`);
        }
    }
    /**
     * Link public key FIRST (creates wallet) - Following same pattern as Solana
     */
    async linkPublicKey(avatarId, publicKey, walletAddress) {
        try {
            const response = await this.axiosInstance.post('/api/keys/link_provider_public_key_to_avatar_by_id', {
                AvatarID: avatarId,
                ProviderType: 'EthereumOASIS',
                ProviderKey: publicKey,
                WalletAddress: walletAddress,
                // Omit WalletId to create a new wallet
            });
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
        }
        catch (error) {
            throw new Error(`Failed to link public key: ${error.message}`);
        }
    }
    /**
     * Link private key SECOND (completes wallet) - Following same pattern as Solana
     */
    async linkPrivateKey(walletId, avatarId, privateKey) {
        try {
            const response = await this.axiosInstance.post('/api/keys/link_provider_private_key_to_avatar_by_id', {
                WalletId: walletId, // REQUIRED - from public key linking step
                AvatarID: avatarId,
                ProviderType: 'EthereumOASIS',
                ProviderKey: privateKey,
            });
            const result = response.data?.result || response.data;
            const linkResult = result?.result || result;
            if (linkResult?.isError === true || result?.isError === true) {
                const errorMsg = linkResult?.message || result?.message || 'Failed to link private key';
                throw new Error(errorMsg);
            }
        }
        catch (error) {
            throw new Error(`Failed to link private key: ${error.message}`);
        }
    }
    /**
     * Set wallet as default (optional)
     */
    async setDefaultWallet(avatarId, walletId) {
        try {
            await this.axiosInstance.post(`/api/wallet/avatar/${avatarId}/default-wallet/${walletId}?providerType=EthereumOASIS`);
        }
        catch (error) {
            // Don't fail the whole operation if setting default fails
            console.warn(`Failed to set wallet as default: ${error.message}`);
        }
    }
    /**
     * Create Ethereum wallet for an avatar (works for both regular avatars and agent avatars)
     *
     * MCP Tool: oasis_create_ethereum_wallet
     */
    async createEthereumWallet(options) {
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
                providerType: 'EthereumOASIS',
                avatarId,
                isDefaultWallet: setAsDefault,
            };
        }
        catch (error) {
            throw new Error(`Failed to create Ethereum wallet: ${error.message}`);
        }
    }
}
/**
 * MCP Tool Definitions
 *
 * These should be registered with your MCP server
 */
export const ethereumWalletMCPToolDefinitions = [
    {
        name: 'oasis_create_ethereum_wallet',
        description: 'Create an Ethereum wallet for an avatar (works for both regular avatars and agent avatars). Follows the correct order: generates keypair, links public key first (creates wallet), then links private key (completes wallet). Automatically ensures Ethereum provider is registered and activated.',
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
                    description: 'Whether to ensure Ethereum provider is registered and activated before creating wallet (default: true)',
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
 * import { EthereumWalletMCPTools, ethereumWalletMCPToolDefinitions } from './ethereum-wallet-tools';
 *
 * // Initialize
 * const ethereumTools = new EthereumWalletMCPTools(
 *   process.env.OASIS_API_URL || 'https://api.oasisweb4.com',
 *   async () => {
 *     // Your token retrieval logic
 *     return await getAuthToken();
 *   }
 * );
 *
 * // Register tools
 * for (const toolDef of ethereumWalletMCPToolDefinitions) {
 *   mcpServer.addTool(toolDef);
 * }
 *
 * // Handle tool calls
 * mcpServer.onToolCall(async (toolName, args) => {
 *   if (toolName === 'oasis_create_ethereum_wallet') {
 *     return await ethereumTools.createEthereumWallet(args);
 *   }
 * });
 * ```
 */
//# sourceMappingURL=ethereum-wallet-tools.js.map