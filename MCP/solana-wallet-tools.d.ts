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
export declare class SolanaWalletMCPTools {
    private oasisApiUrl;
    private axiosInstance;
    constructor(oasisApiUrl: string, getAuthToken: () => Promise<string>);
    /**
     * Ensure Solana provider is registered and activated
     */
    private ensureProviderActivated;
    /**
     * Generate Solana keypair
     */
    private generateKeypair;
    /**
     * Link public key FIRST (creates wallet) - CRITICAL ORDER for Solana
     */
    private linkPublicKey;
    /**
     * Link private key SECOND (completes wallet) - CRITICAL ORDER for Solana
     */
    private linkPrivateKey;
    /**
     * Set wallet as default (optional)
     */
    private setDefaultWallet;
    /**
     * Create Solana wallet for an avatar (works for both regular avatars and agent avatars)
     *
     * MCP Tool: oasis_create_solana_wallet
     */
    createSolanaWallet(options: SolanaWalletCreationOptions): Promise<SolanaWalletCreationResult>;
}
/**
 * MCP Tool Definitions
 *
 * These should be registered with your MCP server
 */
export declare const solanaWalletMCPToolDefinitions: {
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
//# sourceMappingURL=solana-wallet-tools.d.ts.map