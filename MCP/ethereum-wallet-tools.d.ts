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
export interface EthereumWalletCreationResult {
    walletId: string;
    walletAddress: string;
    publicKey: string;
    providerType: string;
    avatarId: string;
    isDefaultWallet?: boolean;
}
export interface EthereumWalletCreationOptions {
    avatarId: string;
    setAsDefault?: boolean;
    ensureProviderActivated?: boolean;
}
export declare class EthereumWalletMCPTools {
    private oasisApiUrl;
    private axiosInstance;
    constructor(oasisApiUrl: string, getAuthToken: () => Promise<string>);
    /**
     * Ensure Ethereum provider is registered and activated
     */
    private ensureProviderActivated;
    /**
     * Generate Ethereum keypair
     */
    private generateKeypair;
    /**
     * Link public key FIRST (creates wallet) - Following same pattern as Solana
     */
    private linkPublicKey;
    /**
     * Link private key SECOND (completes wallet) - Following same pattern as Solana
     */
    private linkPrivateKey;
    /**
     * Set wallet as default (optional)
     */
    private setDefaultWallet;
    /**
     * Create Ethereum wallet for an avatar (works for both regular avatars and agent avatars)
     *
     * MCP Tool: oasis_create_ethereum_wallet
     */
    createEthereumWallet(options: EthereumWalletCreationOptions): Promise<EthereumWalletCreationResult>;
}
/**
 * MCP Tool Definitions
 *
 * These should be registered with your MCP server
 */
export declare const ethereumWalletMCPToolDefinitions: {
    name: string;
    description: string;
    inputSchema: {
        type: string;
        properties: {
            avatarId: {
                type: string;
                description: string;
            };
            setAsDefault: {
                type: string;
                description: string;
                default: boolean;
            };
            ensureProviderActivated: {
                type: string;
                description: string;
                default: boolean;
            };
        };
        required: string[];
    };
}[];
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
//# sourceMappingURL=ethereum-wallet-tools.d.ts.map