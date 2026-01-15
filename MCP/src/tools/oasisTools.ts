import { Tool } from '@modelcontextprotocol/sdk/types.js';
import { OASISClient } from '../clients/oasisClient.js';
import { SolanaRpcClient } from '../clients/solanaRpcClient.js';
import { SolanaWalletMCPTools } from '../../solana-wallet-tools.js';
import { EthereumWalletMCPTools } from '../../ethereum-wallet-tools.js';
import { config } from '../config.js';

const oasisClient = new OASISClient();
const solanaRpcClient = new SolanaRpcClient();

// Initialize Solana wallet tools
// Note: The SolanaWalletMCPTools will use its own axios instance
// Authentication should be set via oasis_authenticate_avatar first
const solanaWalletTools = new SolanaWalletMCPTools(
  config.oasisApiUrl,
  async () => {
    // Try to get token from environment or config
    // Users should authenticate first using oasis_authenticate_avatar
    return config.oasisApiKey || process.env.OASIS_JWT_TOKEN || '';
  }
);

// Initialize Ethereum wallet tools
// Note: The EthereumWalletMCPTools will use its own axios instance
// Authentication should be set via oasis_authenticate_avatar first
const ethereumWalletTools = new EthereumWalletMCPTools(
  config.oasisApiUrl,
  async () => {
    // Try to get token from environment or config
    // Users should authenticate first using oasis_authenticate_avatar
    return config.oasisApiKey || process.env.OASIS_JWT_TOKEN || '';
  }
);

export const oasisTools: Tool[] = [
  {
    name: 'oasis_get_avatar',
    description: 'Get avatar information by ID, username, or email',
    inputSchema: {
      type: 'object',
      properties: {
        avatarId: {
          type: 'string',
          description: 'Avatar ID (UUID)',
        },
        username: {
          type: 'string',
          description: 'Avatar username',
        },
        email: {
          type: 'string',
          description: 'Avatar email address',
        },
      },
    },
  },
  {
    name: 'oasis_get_karma',
    description: 'Get karma (reputation) score for an avatar',
    inputSchema: {
      type: 'object',
      properties: {
        avatarId: {
          type: 'string',
          description: 'Avatar ID (UUID)',
        },
      },
      required: ['avatarId'],
    },
  },
  {
    name: 'oasis_get_nfts',
    description: 'Get all NFTs owned by an avatar',
    inputSchema: {
      type: 'object',
      properties: {
        avatarId: {
          type: 'string',
          description: 'Avatar ID (UUID)',
        },
      },
      required: ['avatarId'],
    },
  },
  {
    name: 'oasis_get_nft',
    description: 'Get NFT details by NFT ID',
    inputSchema: {
      type: 'object',
      properties: {
        nftId: {
          type: 'string',
          description: 'NFT ID',
        },
      },
      required: ['nftId'],
    },
  },
  {
    name: 'oasis_get_wallet',
    description: 'Get wallet information for an avatar',
    inputSchema: {
      type: 'object',
      properties: {
        avatarId: {
          type: 'string',
          description: 'Avatar ID (UUID)',
        },
      },
      required: ['avatarId'],
    },
  },
  {
    name: 'oasis_get_holon',
    description: 'Get holon (data object) by ID',
    inputSchema: {
      type: 'object',
      properties: {
        holonId: {
          type: 'string',
          description: 'Holon ID',
        },
      },
      required: ['holonId'],
    },
  },
  {
    name: 'oasis_health_check',
    description: 'Check OASIS API health status',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },
  {
    name: 'oasis_get_all_avatars',
    description: 'Get all avatars in OASIS (requires Wizard/Admin authentication)',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },
  {
    name: 'oasis_register_avatar',
    description: 'Register/Create a new avatar in OASIS. For A2A agents, set avatarType to "Agent". After registration, authenticate to get JWT token for subsequent requests.',
    inputSchema: {
      type: 'object',
      properties: {
        username: {
          type: 'string',
          description: 'Username for the avatar',
        },
        email: {
          type: 'string',
          description: 'Email address',
        },
        password: {
          type: 'string',
          description: 'Password for the avatar',
        },
        firstName: {
          type: 'string',
          description: 'First name (optional)',
        },
        lastName: {
          type: 'string',
          description: 'Last name (optional)',
        },
        title: {
          type: 'string',
          description: 'Title (optional)',
        },
        avatarType: {
          type: 'string',
          description: 'Avatar type: "User" (default), "Wizard", "Agent" (for A2A agents), or "System"',
        },
        acceptTerms: {
          type: 'boolean',
          description: 'Accept terms and conditions (required, must be true)',
        },
        confirmPassword: {
          type: 'string',
          description: 'Confirm password (must match password)',
        },
      },
      required: ['username', 'email', 'password'],
    },
  },
  {
    name: 'oasis_authenticate_avatar',
    description: 'Authenticate (login) an avatar and get JWT token. The token is automatically set for subsequent authenticated requests. Required before registering agent capabilities or SERV services.',
    inputSchema: {
      type: 'object',
      properties: {
        username: {
          type: 'string',
          description: 'Username',
        },
        password: {
          type: 'string',
          description: 'Password',
        },
      },
      required: ['username', 'password'],
    },
  },
  {
    name: 'oasis_mint_nft',
    description: 'Mint a new NFT. Requires authentication. Minimum required: JSONMetaDataURL and Symbol. Defaults: OnChainProvider=SolanaOASIS, OffChainProvider=None, NFTOffChainMetaType=ExternalJsonURL, NFTStandardType=SPL',
    inputSchema: {
      type: 'object',
      properties: {
        JSONMetaDataURL: {
          type: 'string',
          description: 'URL to NFT metadata (JSON or IPFS) - REQUIRED',
        },
        Symbol: {
          type: 'string',
          description: 'NFT symbol/ticker - REQUIRED',
        },
        Title: {
          type: 'string',
          description: 'NFT title (optional, defaults to "Untitled NFT")',
        },
        Description: {
          type: 'string',
          description: 'NFT description (optional)',
        },
        ImageUrl: {
          type: 'string',
          description: 'NFT image URL (optional, defaults to JSONMetaDataURL)',
        },
        ThumbnailUrl: {
          type: 'string',
          description: 'NFT thumbnail URL (optional)',
        },
        Price: {
          type: 'number',
          description: 'NFT price (optional, defaults to 0)',
        },
        NumberToMint: {
          type: 'number',
          description: 'Number of NFTs to mint (optional, defaults to 1)',
        },
        OnChainProvider: {
          type: 'string',
          description: 'On-chain provider (optional, defaults to "SolanaOASIS")',
        },
        OffChainProvider: {
          type: 'string',
          description: 'Off-chain provider (optional, defaults to "None")',
        },
        NFTOffChainMetaType: {
          type: 'string',
          description: 'NFT off-chain metadata type (optional, defaults to "ExternalJsonURL")',
        },
        NFTStandardType: {
          type: 'string',
          description: 'NFT standard type (optional, defaults to "SPL")',
        },
        SendToAddressAfterMinting: {
          type: 'string',
          description: 'Wallet address to send NFT to after minting (optional)',
        },
        SendToAvatarAfterMintingId: {
          type: 'string',
          description: 'Avatar ID to send NFT to after minting (optional)',
        },
      },
      required: ['JSONMetaDataURL', 'Symbol'],
    },
  },
  {
    name: 'oasis_save_holon',
    description: 'Create or update a holon (data object) in OASIS',
    inputSchema: {
      type: 'object',
      properties: {
        holon: {
          type: 'object',
          description: 'Holon object to save (can include id, name, description, data, etc.)',
        },
      },
      required: ['holon'],
    },
  },
  {
    name: 'oasis_update_avatar',
    description: 'Update avatar information',
    inputSchema: {
      type: 'object',
      properties: {
        avatarId: {
          type: 'string',
          description: 'Avatar ID to update',
        },
        updates: {
          type: 'object',
          description: 'Fields to update (firstName, lastName, description, etc.)',
        },
      },
      required: ['avatarId', 'updates'],
    },
  },
  {
    name: 'oasis_create_wallet',
    description: 'Create a wallet for an avatar',
    inputSchema: {
      type: 'object',
      properties: {
        avatarId: {
          type: 'string',
          description: 'Avatar ID',
        },
        walletType: {
          type: 'string',
          description: 'Wallet type (Ethereum, Solana, etc.)',
        },
      },
      required: ['avatarId'],
    },
  },
  {
    name: 'oasis_send_transaction',
    description: 'Send a transaction (tokens) between avatars or addresses via OASIS API. Uses the send_token endpoint which requires wallet addresses and provider types.',
    inputSchema: {
      type: 'object',
      properties: {
        fromAvatarId: {
          type: 'string',
          description: 'Source avatar ID (optional if fromWalletAddress provided)',
        },
        fromWalletAddress: {
          type: 'string',
          description: 'Source wallet address (optional if fromAvatarId provided)',
        },
        toAvatarId: {
          type: 'string',
          description: 'Destination avatar ID (optional if toAddress/toWalletAddress provided)',
        },
        toAddress: {
          type: 'string',
          description: 'Destination wallet address (optional if toAvatarId provided)',
        },
        toWalletAddress: {
          type: 'string',
          description: 'Destination wallet address (alternative to toAddress)',
        },
        amount: {
          type: 'number',
          description: 'Amount to send',
        },
        fromProvider: {
          type: 'number',
          description: 'Source provider type enum (e.g., 3 for SolanaOASIS, default: 3)',
        },
        toProvider: {
          type: 'number',
          description: 'Destination provider type enum (e.g., 3 for SolanaOASIS, default: 3)',
        },
        memoText: {
          type: 'string',
          description: 'Optional memo text for the transaction',
        },
      },
      required: ['amount'],
    },
  },
  {
    name: 'oasis_get_avatar_detail',
    description: 'Get detailed avatar information by ID, username, or email',
    inputSchema: {
      type: 'object',
      properties: {
        avatarId: {
          type: 'string',
          description: 'Avatar ID (UUID)',
        },
        username: {
          type: 'string',
          description: 'Avatar username',
        },
        email: {
          type: 'string',
          description: 'Avatar email address',
        },
      },
    },
  },
  {
    name: 'oasis_get_all_avatar_details',
    description: 'Get all avatar details (requires Wizard/Admin authentication)',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },
  {
    name: 'oasis_get_nft_by_hash',
    description: 'Get NFT details by hash',
    inputSchema: {
      type: 'object',
      properties: {
        hash: {
          type: 'string',
          description: 'NFT hash',
        },
      },
      required: ['hash'],
    },
  },
  {
    name: 'oasis_get_geo_nfts',
    description: 'Get all GeoNFTs for an avatar',
    inputSchema: {
      type: 'object',
      properties: {
        avatarId: {
          type: 'string',
          description: 'Avatar ID (UUID)',
        },
      },
      required: ['avatarId'],
    },
  },
  {
    name: 'oasis_send_nft',
    description: 'Send NFT between wallets',
    inputSchema: {
      type: 'object',
      properties: {
        FromWalletAddress: {
          type: 'string',
          description: 'Source wallet address',
        },
        ToWalletAddress: {
          type: 'string',
          description: 'Destination wallet address',
        },
        FromProvider: {
          type: 'string',
          description: 'Source provider (e.g., SolanaOASIS)',
        },
        ToProvider: {
          type: 'string',
          description: 'Destination provider (e.g., SolanaOASIS)',
        },
        Amount: {
          type: 'number',
          description: 'Amount to send (optional)',
        },
        MemoText: {
          type: 'string',
          description: 'Memo text (optional)',
        },
      },
      required: ['FromWalletAddress', 'ToWalletAddress', 'FromProvider', 'ToProvider'],
    },
  },
  {
    name: 'oasis_get_provider_wallets',
    description: 'Get provider wallets for an avatar',
    inputSchema: {
      type: 'object',
      properties: {
        avatarId: {
          type: 'string',
          description: 'Avatar ID (UUID)',
        },
        providerType: {
          type: 'string',
          description: 'Provider type (optional)',
        },
      },
      required: ['avatarId'],
    },
  },
  {
    name: 'oasis_get_transaction',
    description: 'Get transaction details by hash',
    inputSchema: {
      type: 'object',
      properties: {
        transactionHash: {
          type: 'string',
          description: 'Transaction hash/signature',
        },
        blockchain: {
          type: 'string',
          description: 'Blockchain type (solana or ethereum, optional - auto-detected if not provided)',
        },
        providerType: {
          type: 'string',
          description: 'Provider type (optional)',
        },
      },
      required: ['transactionHash'],
    },
  },
  {
    name: 'oasis_get_portfolio_value',
    description: 'Get total portfolio value for an avatar',
    inputSchema: {
      type: 'object',
      properties: {
        avatarId: {
          type: 'string',
          description: 'Avatar ID (UUID)',
        },
      },
      required: ['avatarId'],
    },
  },
  {
    name: 'oasis_search_holons',
    description: 'Search holons (data objects)',
    inputSchema: {
      type: 'object',
      properties: {
        searchQuery: {
          type: 'string',
          description: 'Search query string',
        },
        holonType: {
          type: 'string',
          description: 'Holon type filter (optional)',
        },
        parentId: {
          type: 'string',
          description: 'Parent ID filter (optional)',
        },
        limit: {
          type: 'number',
          description: 'Maximum results (optional, default: 50)',
        },
        offset: {
          type: 'number',
          description: 'Offset for pagination (optional, default: 0)',
        },
      },
    },
  },
  {
    name: 'oasis_load_holons_for_parent',
    description: 'Load all holons for a parent holon',
    inputSchema: {
      type: 'object',
      properties: {
        parentId: {
          type: 'string',
          description: 'Parent holon ID',
        },
      },
      required: ['parentId'],
    },
  },
  {
    name: 'oasis_delete_holon',
    description: 'Delete a holon (data object)',
    inputSchema: {
      type: 'object',
      properties: {
        holonId: {
          type: 'string',
          description: 'Holon ID to delete',
        },
      },
      required: ['holonId'],
    },
  },
  {
    name: 'oasis_get_karma_stats',
    description: 'Get karma statistics for an avatar',
    inputSchema: {
      type: 'object',
      properties: {
        avatarId: {
          type: 'string',
          description: 'Avatar ID (UUID)',
        },
      },
      required: ['avatarId'],
    },
  },
  {
    name: 'oasis_get_karma_history',
    description: 'Get karma history for an avatar',
    inputSchema: {
      type: 'object',
      properties: {
        avatarId: {
          type: 'string',
          description: 'Avatar ID (UUID)',
        },
        limit: {
          type: 'number',
          description: 'Maximum results (optional, default: 50)',
        },
        offset: {
          type: 'number',
          description: 'Offset for pagination (optional, default: 0)',
        },
      },
      required: ['avatarId'],
    },
  },
  {
    name: 'oasis_add_karma',
    description: 'Add positive karma to an avatar',
    inputSchema: {
      type: 'object',
      properties: {
        avatarId: {
          type: 'string',
          description: 'Avatar ID (UUID)',
        },
        KarmaType: {
          type: 'string',
          description: 'Positive karma type (e.g., HelpOthers, ShareKnowledge)',
        },
        karmaSourceType: {
          type: 'string',
          description: 'Source type (App, dApp, hApp, Website, Game)',
        },
        KaramSourceTitle: {
          type: 'string',
          description: 'Source title (optional)',
        },
        KarmaSourceDesc: {
          type: 'string',
          description: 'Source description (optional)',
        },
      },
      required: ['avatarId', 'KarmaType', 'karmaSourceType'],
    },
  },
  {
    name: 'oasis_remove_karma',
    description: 'Remove karma (negative karma) from an avatar',
    inputSchema: {
      type: 'object',
      properties: {
        avatarId: {
          type: 'string',
          description: 'Avatar ID (UUID)',
        },
        KarmaType: {
          type: 'string',
          description: 'Negative karma type (e.g., HarmOthers, SpreadMisinformation)',
        },
        karmaSourceType: {
          type: 'string',
          description: 'Source type (App, dApp, hApp, Website, Game)',
        },
        KaramSourceTitle: {
          type: 'string',
          description: 'Source title (optional)',
        },
        KarmaSourceDesc: {
          type: 'string',
          description: 'Source description (optional)',
        },
      },
      required: ['avatarId', 'KarmaType', 'karmaSourceType'],
    },
  },
  {
    name: 'oasis_search_avatars',
    description: 'Search avatars',
    inputSchema: {
      type: 'object',
      properties: {
        searchQuery: {
          type: 'string',
          description: 'Search query string',
        },
        avatarType: {
          type: 'string',
          description: 'Avatar type filter (optional)',
        },
        limit: {
          type: 'number',
          description: 'Maximum results (optional, default: 50)',
        },
        offset: {
          type: 'number',
          description: 'Offset for pagination (optional, default: 0)',
        },
      },
    },
  },
  {
    name: 'oasis_search_nfts',
    description: 'Search NFTs',
    inputSchema: {
      type: 'object',
      properties: {
        searchQuery: {
          type: 'string',
          description: 'Search query string',
        },
        avatarId: {
          type: 'string',
          description: 'Avatar ID filter (optional)',
        },
        limit: {
          type: 'number',
          description: 'Maximum results (optional, default: 50)',
        },
        offset: {
          type: 'number',
          description: 'Offset for pagination (optional, default: 0)',
        },
      },
    },
  },
  {
    name: 'oasis_get_all_avatar_names',
    description: 'Get all avatar names (optionally including usernames and IDs)',
    inputSchema: {
      type: 'object',
      properties: {
        includeUsernames: {
          type: 'boolean',
          description: 'Include usernames (default: true)',
        },
        includeIds: {
          type: 'boolean',
          description: 'Include IDs (default: true)',
        },
      },
    },
  },
  {
    name: 'oasis_get_avatar_portrait',
    description: 'Get avatar portrait by ID, username, or email',
    inputSchema: {
      type: 'object',
      properties: {
        avatarId: {
          type: 'string',
          description: 'Avatar ID (UUID)',
        },
        username: {
          type: 'string',
          description: 'Avatar username',
        },
        email: {
          type: 'string',
          description: 'Avatar email address',
        },
      },
    },
  },
  {
    name: 'oasis_get_nfts_for_mint_address',
    description: 'Get all NFTs for a mint wallet address',
    inputSchema: {
      type: 'object',
      properties: {
        mintWalletAddress: {
          type: 'string',
          description: 'Mint wallet address',
        },
      },
      required: ['mintWalletAddress'],
    },
  },
  {
    name: 'oasis_get_geo_nfts_for_mint_address',
    description: 'Get all GeoNFTs for a mint wallet address',
    inputSchema: {
      type: 'object',
      properties: {
        mintWalletAddress: {
          type: 'string',
          description: 'Mint wallet address',
        },
      },
      required: ['mintWalletAddress'],
    },
  },
  {
    name: 'oasis_get_all_nfts',
    description: 'Get all NFTs (requires Wizard/Admin authentication)',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },
  {
    name: 'oasis_get_all_geo_nfts',
    description: 'Get all GeoNFTs (requires Wizard/Admin authentication)',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },
  {
    name: 'oasis_get_provider_wallets_by_username',
    description: 'Get provider wallets for avatar by username',
    inputSchema: {
      type: 'object',
      properties: {
        username: {
          type: 'string',
          description: 'Avatar username',
        },
        providerType: {
          type: 'string',
          description: 'Provider type (optional)',
        },
      },
      required: ['username'],
    },
  },
  {
    name: 'oasis_get_provider_wallets_by_email',
    description: 'Get provider wallets for avatar by email',
    inputSchema: {
      type: 'object',
      properties: {
        email: {
          type: 'string',
          description: 'Avatar email',
        },
        providerType: {
          type: 'string',
          description: 'Provider type (optional)',
        },
      },
      required: ['email'],
    },
  },
  {
    name: 'oasis_get_default_wallet',
    description: 'Get default wallet for an avatar',
    inputSchema: {
      type: 'object',
      properties: {
        avatarId: {
          type: 'string',
          description: 'Avatar ID (UUID)',
        },
        providerType: {
          type: 'string',
          description: 'Provider type',
        },
      },
      required: ['avatarId', 'providerType'],
    },
  },
  {
    name: 'oasis_set_default_wallet',
    description: 'Set default wallet for an avatar',
    inputSchema: {
      type: 'object',
      properties: {
        avatarId: {
          type: 'string',
          description: 'Avatar ID (UUID)',
        },
        walletId: {
          type: 'string',
          description: 'Wallet ID to set as default',
        },
        providerType: {
          type: 'string',
          description: 'Provider type',
        },
      },
      required: ['avatarId', 'walletId', 'providerType'],
    },
  },
  {
    name: 'oasis_get_wallets_by_chain',
    description: 'Get wallets by chain for an avatar',
    inputSchema: {
      type: 'object',
      properties: {
        avatarId: {
          type: 'string',
          description: 'Avatar ID (UUID)',
        },
        chain: {
          type: 'string',
          description: 'Chain name (e.g., ethereum, solana)',
        },
      },
      required: ['avatarId', 'chain'],
    },
  },
  {
    name: 'oasis_get_wallet_analytics',
    description: 'Get wallet analytics for an avatar',
    inputSchema: {
      type: 'object',
      properties: {
        avatarId: {
          type: 'string',
          description: 'Avatar ID (UUID)',
        },
        walletId: {
          type: 'string',
          description: 'Wallet ID',
        },
      },
      required: ['avatarId', 'walletId'],
    },
  },
  {
    name: 'oasis_get_wallet_tokens',
    description: 'Get tokens in a wallet',
    inputSchema: {
      type: 'object',
      properties: {
        avatarId: {
          type: 'string',
          description: 'Avatar ID (UUID)',
        },
        walletId: {
          type: 'string',
          description: 'Wallet ID',
        },
      },
      required: ['avatarId', 'walletId'],
    },
  },
  {
    name: 'oasis_get_supported_chains',
    description: 'Get list of supported blockchain chains',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },
  {
    name: 'oasis_import_wallet_private_key',
    description: 'Import wallet using private key',
    inputSchema: {
      type: 'object',
      properties: {
        avatarId: {
          type: 'string',
          description: 'Avatar ID (UUID)',
        },
        privateKey: {
          type: 'string',
          description: 'Private key',
        },
        providerType: {
          type: 'string',
          description: 'Provider type to import to',
        },
      },
      required: ['avatarId', 'privateKey', 'providerType'],
    },
  },
  {
    name: 'oasis_import_wallet_public_key',
    description: 'Import wallet using public key',
    inputSchema: {
      type: 'object',
      properties: {
        avatarId: {
          type: 'string',
          description: 'Avatar ID (UUID)',
        },
        publicKey: {
          type: 'string',
          description: 'Public key',
        },
        providerType: {
          type: 'string',
          description: 'Provider type to import to',
        },
      },
      required: ['avatarId', 'publicKey', 'providerType'],
    },
  },
  {
    name: 'oasis_create_wallet_full',
    description: 'Create wallet with full options',
    inputSchema: {
      type: 'object',
      properties: {
        avatarId: {
          type: 'string',
          description: 'Avatar ID (UUID)',
        },
        Name: {
          type: 'string',
          description: 'Wallet name (optional)',
        },
        Description: {
          type: 'string',
          description: 'Wallet description (optional)',
        },
        WalletProviderType: {
          type: 'string',
          description: 'Wallet provider type (e.g., Ethereum, Solana)',
        },
        GenerateKeyPair: {
          type: 'boolean',
          description: 'Generate key pair (optional, default: true)',
        },
        IsDefaultWallet: {
          type: 'boolean',
          description: 'Set as default wallet (optional, default: false)',
        },
        providerType: {
          type: 'string',
          description: 'Provider type to load/save from (optional)',
        },
      },
      required: ['avatarId', 'WalletProviderType'],
    },
  },
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
  {
    name: 'oasis_load_all_holons',
    description: 'Load all holons (requires authentication)',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },
  {
    name: 'oasis_update_holon',
    description: 'Update a holon (data object)',
    inputSchema: {
      type: 'object',
      properties: {
        holonId: {
          type: 'string',
          description: 'Holon ID to update',
        },
        holon: {
          type: 'object',
          description: 'Updated holon object',
        },
      },
      required: ['holonId', 'holon'],
    },
  },
  {
    name: 'oasis_get_karma_akashic_records',
    description: 'Get karma akashic records for an avatar',
    inputSchema: {
      type: 'object',
      properties: {
        avatarId: {
          type: 'string',
          description: 'Avatar ID (UUID)',
        },
      },
      required: ['avatarId'],
    },
  },
  {
    name: 'oasis_get_positive_karma_weighting',
    description: 'Get positive karma weighting for a karma type. Valid karma types include: HelpOtherPerson, HelpOtherPeople, BeAHero, BeASuperHero, BeATeamPlayer, HelpingAnimals, HelpingTheEnvironment, ContributingTowardsAGoodCauseContributor, ContributingTowardsAGoodCauseFunder, SelfHelpImprovement, OurWorld, Other, etc. Use exact enum value names from KarmaTypePositive.',
    inputSchema: {
      type: 'object',
      properties: {
        karmaType: {
          type: 'string',
          description: 'Positive karma type (must be valid KarmaTypePositive enum value, e.g., HelpOtherPerson, BeAHero, HelpingAnimals, etc.)',
          enum: ['HelpOtherPerson', 'HelpOtherPeople', 'BeAHero', 'BeASuperHero', 'BeATeamPlayer', 'HelpingAnimals', 'HelpingTheEnvironment', 'ContributingTowardsAGoodCauseContributor', 'ContributingTowardsAGoodCauseFunder', 'SelfHelpImprovement', 'OurWorld', 'Other', 'PickupLitter', 'DefendOtherPerson', 'DefendOtherPeople', 'BeSelfless', 'LevelUp', 'BeingPresent', 'BeingDetermined', 'BeingMindful', 'BeingHappy', 'BeingPeaceful', 'BeingWise', 'BeingPositive', 'BeingFast', 'BeingSuperFast', 'BeingStrong', 'BeingSuperStrong', 'BeingGrateful', 'SpeakingYourTruth'],
        },
      },
      required: ['karmaType'],
    },
  },
  {
    name: 'oasis_get_negative_karma_weighting',
    description: 'Get negative karma weighting for a karma type. Valid karma types include: DropLitter, AttackVerballyOtherPersonOrPeople, AttackPhysciallyOtherPersonOrPeople, BeingSelfish, NotTeamPlayer, HarmingAnimals, HarmingChildren, HarmingNature, Other, etc. Use exact enum value names from KarmaTypeNegative.',
    inputSchema: {
      type: 'object',
      properties: {
        karmaType: {
          type: 'string',
          description: 'Negative karma type (must be valid KarmaTypeNegative enum value, e.g., DropLitter, BeingSelfish, HarmingAnimals, etc.)',
          enum: ['DropLitter', 'AttackVerballyOtherPersonOrPeople', 'AttackPhysciallyOtherPersonOrPeople', 'BeingSelfish', 'NotTeamPlayer', 'HarmingAnimals', 'HarmingChildren', 'HarmingNature', 'Other', 'DisrespectPersonOrPeople', 'NutritionEatMeat', 'NutritionEatDiary', 'NutritionEatDrinkUnhealthy'],
        },
      },
      required: ['karmaType'],
    },
  },
  {
    name: 'oasis_vote_positive_karma_weighting',
    description: 'Vote for positive karma weighting. Valid karma types include: HelpOtherPerson, HelpOtherPeople, BeAHero, BeASuperHero, BeATeamPlayer, HelpingAnimals, HelpingTheEnvironment, ContributingTowardsAGoodCauseContributor, ContributingTowardsAGoodCauseFunder, SelfHelpImprovement, OurWorld, Other, etc. Use exact enum value names from KarmaTypePositive.',
    inputSchema: {
      type: 'object',
      properties: {
        karmaType: {
          type: 'string',
          description: 'Positive karma type (must be valid KarmaTypePositive enum value)',
          enum: ['HelpOtherPerson', 'HelpOtherPeople', 'BeAHero', 'BeASuperHero', 'BeATeamPlayer', 'HelpingAnimals', 'HelpingTheEnvironment', 'ContributingTowardsAGoodCauseContributor', 'ContributingTowardsAGoodCauseFunder', 'SelfHelpImprovement', 'OurWorld', 'Other'],
        },
        weighting: {
          type: 'number',
          description: 'Weighting value to vote for',
        },
      },
      required: ['karmaType', 'weighting'],
    },
  },
  {
    name: 'oasis_vote_negative_karma_weighting',
    description: 'Vote for negative karma weighting. Valid karma types include: DropLitter, AttackVerballyOtherPersonOrPeople, AttackPhysciallyOtherPersonOrPeople, BeingSelfish, NotTeamPlayer, HarmingAnimals, HarmingChildren, HarmingNature, Other, etc. Use exact enum value names from KarmaTypeNegative.',
    inputSchema: {
      type: 'object',
      properties: {
        karmaType: {
          type: 'string',
          description: 'Negative karma type (must be valid KarmaTypeNegative enum value)',
          enum: ['DropLitter', 'AttackVerballyOtherPersonOrPeople', 'AttackPhysciallyOtherPersonOrPeople', 'BeingSelfish', 'NotTeamPlayer', 'HarmingAnimals', 'HarmingChildren', 'HarmingNature', 'Other'],
        },
        weighting: {
          type: 'number',
          description: 'Weighting value to vote for',
        },
      },
      required: ['karmaType', 'weighting'],
    },
  },
  {
    name: 'oasis_basic_search',
    description: 'Basic search across OASIS',
    inputSchema: {
      type: 'object',
      properties: {
        searchQuery: {
          type: 'string',
          description: 'Search query string',
        },
        limit: {
          type: 'number',
          description: 'Maximum results (optional, default: 50)',
        },
        offset: {
          type: 'number',
          description: 'Offset for pagination (optional, default: 0)',
        },
      },
      required: ['searchQuery'],
    },
  },
  {
    name: 'oasis_advanced_search',
    description: 'Advanced search with filters',
    inputSchema: {
      type: 'object',
      properties: {
        searchQuery: {
          type: 'string',
          description: 'Search query string',
        },
        entityType: {
          type: 'string',
          description: 'Entity type filter (optional)',
        },
        filters: {
          type: 'object',
          description: 'Additional filters (optional)',
        },
        limit: {
          type: 'number',
          description: 'Maximum results (optional, default: 50)',
        },
        offset: {
          type: 'number',
          description: 'Offset for pagination (optional, default: 0)',
        },
      },
    },
  },
  {
    name: 'oasis_search_files',
    description: 'Search files',
    inputSchema: {
      type: 'object',
      properties: {
        searchQuery: {
          type: 'string',
          description: 'Search query string',
        },
        avatarId: {
          type: 'string',
          description: 'Avatar ID filter (optional)',
        },
        fileType: {
          type: 'string',
          description: 'File type filter (optional)',
        },
        limit: {
          type: 'number',
          description: 'Maximum results (optional, default: 50)',
        },
        offset: {
          type: 'number',
          description: 'Offset for pagination (optional, default: 0)',
        },
      },
    },
  },
  // ============================================
  // A2A Protocol & SERV Infrastructure Tools
  // ============================================
  {
    name: 'oasis_get_agent_card',
    description: 'Get agent card (A2A Protocol) by agent ID',
    inputSchema: {
      type: 'object',
      properties: {
        agentId: {
          type: 'string',
          description: 'Agent ID (UUID)',
        },
      },
      required: ['agentId'],
    },
  },
  {
    name: 'oasis_get_all_agents',
    description: 'Get all A2A agents',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },
  {
    name: 'oasis_get_agents_by_service',
    description: 'Get A2A agents by service name',
    inputSchema: {
      type: 'object',
      properties: {
        serviceName: {
          type: 'string',
          description: 'Service name (e.g., "data-analysis", "payment-processing")',
        },
      },
      required: ['serviceName'],
    },
  },
  {
    name: 'oasis_get_my_agents',
    description: 'Get all Agent avatars owned by the authenticated User or Wizard avatar. Requires authentication.',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },
  {
    name: 'oasis_register_agent_capabilities',
    description: 'Register agent capabilities via A2A Protocol. Requires authentication (use oasis_authenticate_avatar first). Must be called before registering as SERV service. Workflow: 1) Register Agent avatar, 2) Authenticate, 3) Register capabilities, 4) Register as SERV service, 5) Discover agents.',
    inputSchema: {
      type: 'object',
      properties: {
        services: {
          type: 'array',
          items: { type: 'string' },
          description: 'List of services the agent provides (e.g., ["data-analysis", "report-generation"])',
        },
        skills: {
          type: 'array',
          items: { type: 'string' },
          description: 'List of skills (e.g., ["Python", "Machine Learning"])',
        },
        pricing: {
          type: 'object',
          description: 'Pricing for each service (e.g., {"data-analysis": 0.1})',
        },
        status: {
          type: 'string',
          description: 'Agent status (Available, Busy, Offline) or number (0=Available)',
        },
        max_concurrent_tasks: {
          type: 'number',
          description: 'Maximum concurrent tasks (optional)',
        },
        description: {
          type: 'string',
          description: 'Agent description (optional)',
        },
      },
      required: ['services'],
    },
  },
  {
    name: 'oasis_register_agent_as_serv_service',
    description: 'Register A2A agent as SERV service in ONET Unified Architecture. Requires: 1) Authentication (oasis_authenticate_avatar), 2) Registered capabilities (oasis_register_agent_capabilities). After registration, the agent will be discoverable via oasis_discover_agents_via_serv.',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },
  {
    name: 'oasis_discover_agents_via_serv',
    description: 'Discover agents via SERV infrastructure (ONET Unified Architecture). Returns all agents registered as SERV services. Optionally filter by service name. No authentication required.',
    inputSchema: {
      type: 'object',
      properties: {
        serviceName: {
          type: 'string',
          description: 'Service name to filter by (optional, e.g., "data-analysis")',
        },
      },
    },
  },
  {
    name: 'oasis_send_a2a_jsonrpc_request',
    description: 'Send A2A JSON-RPC 2.0 request (requires authentication). Methods: ping, payment_request, service_request, capability_query, task_delegation, etc.',
    inputSchema: {
      type: 'object',
      properties: {
        method: {
          type: 'string',
          description: 'JSON-RPC method (e.g., "ping", "payment_request", "service_request")',
        },
        params: {
          type: 'object',
          description: 'Method parameters (varies by method)',
        },
        id: {
          type: 'string',
          description: 'Request ID (optional, auto-generated if not provided)',
        },
      },
      required: ['method'],
    },
  },
  {
    name: 'oasis_get_pending_a2a_messages',
    description: 'Get pending A2A messages for authenticated agent (requires authentication)',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },
  {
    name: 'oasis_mark_a2a_message_processed',
    description: 'Mark A2A message as processed (requires authentication)',
    inputSchema: {
      type: 'object',
      properties: {
        messageId: {
          type: 'string',
          description: 'Message ID (UUID)',
        },
      },
      required: ['messageId'],
    },
  },
  {
    name: 'oasis_register_openserv_agent',
    description: 'Register OpenSERV AI agent as A2A agent and SERV service',
    inputSchema: {
      type: 'object',
      properties: {
        openServAgentId: {
          type: 'string',
          description: 'OpenSERV agent ID',
        },
        openServEndpoint: {
          type: 'string',
          description: 'OpenSERV API endpoint URL',
        },
        capabilities: {
          type: 'array',
          items: { type: 'string' },
          description: 'List of capabilities (e.g., ["data-analysis", "nlp"])',
        },
        apiKey: {
          type: 'string',
          description: 'OpenSERV API key (optional)',
        },
        username: {
          type: 'string',
          description: 'Username for A2A agent (optional)',
        },
        email: {
          type: 'string',
          description: 'Email for A2A agent (optional)',
        },
        password: {
          type: 'string',
          description: 'Password for A2A agent (optional)',
        },
      },
      required: ['openServAgentId', 'openServEndpoint', 'capabilities'],
    },
  },
  {
    name: 'oasis_execute_ai_workflow',
    description: 'Execute AI workflow via A2A Protocol (requires authentication)',
    inputSchema: {
      type: 'object',
      properties: {
        toAgentId: {
          type: 'string',
          description: 'Target agent ID (UUID)',
        },
        workflowRequest: {
          type: 'string',
          description: 'Workflow request description',
        },
        workflowParameters: {
          type: 'object',
          description: 'Workflow parameters (optional)',
        },
      },
      required: ['toAgentId', 'workflowRequest'],
    },
  },
  {
    name: 'solana_send_sol',
    description: 'Send SOL tokens from one wallet to another using Solana RPC (direct blockchain transaction)',
    inputSchema: {
      type: 'object',
      properties: {
        fromPrivateKey: {
          type: 'string',
          description: 'Base58 encoded private key of the sender wallet',
        },
        toAddress: {
          type: 'string',
          description: 'Recipient Solana public key address',
        },
        amount: {
          type: 'number',
          description: 'Amount to send in SOL (not lamports)',
        },
        network: {
          type: 'string',
          enum: ['devnet', 'mainnet-beta', 'testnet'],
          description: 'Solana network to use (default: devnet)',
        },
      },
      required: ['fromPrivateKey', 'toAddress', 'amount'],
    },
  },
  {
    name: 'solana_get_balance',
    description: 'Get SOL balance for a Solana wallet address',
    inputSchema: {
      type: 'object',
      properties: {
        address: {
          type: 'string',
          description: 'Solana public key address',
        },
        network: {
          type: 'string',
          enum: ['devnet', 'mainnet-beta', 'testnet'],
          description: 'Solana network to use (default: devnet)',
        },
      },
      required: ['address'],
    },
  },
  {
    name: 'solana_get_transaction',
    description: 'Get transaction details by signature',
    inputSchema: {
      type: 'object',
      properties: {
        signature: {
          type: 'string',
          description: 'Transaction signature',
        },
        network: {
          type: 'string',
          enum: ['devnet', 'mainnet-beta', 'testnet'],
          description: 'Solana network to use (default: devnet)',
        },
      },
      required: ['signature'],
    },
  },
];

export async function handleOASISTool(
  name: string,
  args: any
): Promise<any> {
  try {
    switch (name) {
      case 'oasis_get_avatar': {
        if (args.avatarId) {
          return await oasisClient.getAvatar(args.avatarId);
        } else if (args.username) {
          return await oasisClient.getAvatarByUsername(args.username);
        } else if (args.email) {
          return await oasisClient.getAvatarByEmail(args.email);
        } else {
          throw new Error('Must provide avatarId, username, or email');
        }
      }

      case 'oasis_get_karma': {
        if (!args.avatarId) {
          throw new Error('avatarId is required');
        }
        return await oasisClient.getKarma(args.avatarId);
      }

      case 'oasis_get_nfts': {
        if (!args.avatarId) {
          throw new Error('avatarId is required');
        }
        return await oasisClient.getNFTs(args.avatarId);
      }

      case 'oasis_get_nft': {
        if (!args.nftId) {
          throw new Error('nftId is required');
        }
        return await oasisClient.getNFT(args.nftId);
      }

      case 'oasis_get_wallet': {
        if (!args.avatarId) {
          throw new Error('avatarId is required');
        }
        return await oasisClient.getWallet(args.avatarId);
      }

      case 'oasis_get_holon': {
        if (!args.holonId) {
          throw new Error('holonId is required');
        }
        return await oasisClient.getHolon(args.holonId);
      }

      case 'oasis_health_check': {
        return await oasisClient.healthCheck();
      }

      case 'oasis_get_all_avatars': {
        return await oasisClient.getAllAvatars();
      }

      case 'oasis_register_avatar': {
        if (!args.username || !args.email || !args.password) {
          throw new Error('username, email, and password are required');
        }
        return await oasisClient.registerAvatar({
          username: args.username,
          email: args.email,
          password: args.password,
          firstName: args.firstName,
          lastName: args.lastName,
          title: args.title,
          avatarType: args.avatarType || 'User',
          acceptTerms: args.acceptTerms !== undefined ? args.acceptTerms : true,
          confirmPassword: args.confirmPassword || args.password,
        });
      }

      case 'oasis_authenticate_avatar': {
        if (!args.username || !args.password) {
          throw new Error('username and password are required');
        }
        const result = await oasisClient.authenticateAvatar(args.username, args.password);
        // Auto-set auth token if we got one
        // Response structure: { result: { result: { jwtToken: "..." } } }
        // So the path is: result.result.result.jwtToken
        const token = result?.result?.result?.jwtToken;
        if (token && typeof token === 'string') {
          oasisClient.setAuthToken(token);
          return {
            ...result,
            _authTokenSet: true,
            _message: 'Authentication successful. Token has been set for subsequent requests.',
          };
        } else {
          // Try alternative paths
          const altToken = result?.result?.jwtToken || result?.jwtToken;
          if (altToken && typeof altToken === 'string') {
            oasisClient.setAuthToken(altToken);
            return {
              ...result,
              _authTokenSet: true,
              _message: 'Authentication successful. Token has been set for subsequent requests.',
            };
          }
          return {
            ...result,
            _authTokenSet: false,
            _error: 'JWT token not found in authentication response',
            _debug: JSON.stringify({
              hasResult: !!result,
              hasNestedResult: !!result?.result,
              hasDoubleNestedResult: !!result?.result?.result,
              keys: result ? Object.keys(result) : [],
            }),
          };
        }
      }

      case 'oasis_mint_nft': {
        if (!args.JSONMetaDataURL || !args.Symbol) {
          throw new Error('JSONMetaDataURL and Symbol are required');
        }
        return await oasisClient.mintNFT({
          JSONMetaDataURL: args.JSONMetaDataURL,
          Symbol: args.Symbol,
          Title: args.Title,
          Description: args.Description,
          ImageUrl: args.ImageUrl,
          ThumbnailUrl: args.ThumbnailUrl,
          Price: args.Price,
          NumberToMint: args.NumberToMint,
          OnChainProvider: args.OnChainProvider,
          OffChainProvider: args.OffChainProvider,
          NFTOffChainMetaType: args.NFTOffChainMetaType,
          NFTStandardType: args.NFTStandardType,
          StoreNFTMetaDataOnChain: args.StoreNFTMetaDataOnChain,
          SendToAddressAfterMinting: args.SendToAddressAfterMinting,
          SendToAvatarAfterMintingId: args.SendToAvatarAfterMintingId,
          SendToAvatarAfterMintingUsername: args.SendToAvatarAfterMintingUsername,
          SendToAvatarAfterMintingEmail: args.SendToAvatarAfterMintingEmail,
          WaitTillNFTMinted: args.WaitTillNFTMinted,
          WaitForNFTToMintInSeconds: args.WaitForNFTToMintInSeconds,
          AttemptToMintEveryXSeconds: args.AttemptToMintEveryXSeconds,
        });
      }

      case 'oasis_save_holon': {
        if (!args.holon) {
          throw new Error('holon object is required');
        }
        return await oasisClient.saveHolon(args.holon);
      }

      case 'oasis_update_avatar': {
        if (!args.avatarId || !args.updates) {
          throw new Error('avatarId and updates are required');
        }
        return await oasisClient.updateAvatar(args.avatarId, args.updates);
      }

      case 'oasis_create_wallet': {
        if (!args.avatarId) {
          throw new Error('avatarId is required');
        }
        return await oasisClient.createWallet(args.avatarId, args.walletType);
      }

      case 'oasis_send_transaction': {
        if (args.amount === undefined) {
          throw new Error('amount is required');
        }
        // Require at least one sender identifier
        if (!args.fromAvatarId && !args.fromWalletAddress) {
          throw new Error('Either fromAvatarId or fromWalletAddress is required');
        }
        // Require at least one recipient identifier
        if (!args.toAvatarId && !args.toAddress && !args.toWalletAddress) {
          throw new Error('Either toAvatarId, toAddress, or toWalletAddress is required');
        }
        return await oasisClient.sendTransaction({
          fromAvatarId: args.fromAvatarId,
          fromWalletAddress: args.fromWalletAddress,
          toAvatarId: args.toAvatarId,
          toAddress: args.toAddress,
          toWalletAddress: args.toWalletAddress,
          amount: args.amount,
          fromProvider: args.fromProvider,
          toProvider: args.toProvider,
          memoText: args.memoText,
        });
      }

      case 'oasis_get_avatar_detail': {
        if (args.avatarId) {
          return await oasisClient.getAvatarDetailById(args.avatarId);
        } else if (args.username) {
          return await oasisClient.getAvatarDetailByUsername(args.username);
        } else if (args.email) {
          return await oasisClient.getAvatarDetailByEmail(args.email);
        } else {
          throw new Error('Must provide avatarId, username, or email');
        }
      }

      case 'oasis_get_all_avatar_details': {
        return await oasisClient.getAllAvatarDetails();
      }

      case 'oasis_get_nft_by_hash': {
        if (!args.hash) {
          throw new Error('hash is required');
        }
        return await oasisClient.getNFTByHash(args.hash);
      }

      case 'oasis_get_geo_nfts': {
        if (!args.avatarId) {
          throw new Error('avatarId is required');
        }
        return await oasisClient.getGeoNFTsForAvatar(args.avatarId);
      }

      case 'oasis_send_nft': {
        if (!args.FromWalletAddress || !args.ToWalletAddress || !args.FromProvider || !args.ToProvider) {
          throw new Error('FromWalletAddress, ToWalletAddress, FromProvider, and ToProvider are required');
        }
        return await oasisClient.sendNFT({
          FromWalletAddress: args.FromWalletAddress,
          ToWalletAddress: args.ToWalletAddress,
          FromProvider: args.FromProvider,
          ToProvider: args.ToProvider,
          Amount: args.Amount,
          MemoText: args.MemoText,
          WaitTillNFTSent: args.WaitTillNFTSent,
          WaitForNFTToSendInSeconds: args.WaitForNFTToSendInSeconds,
          AttemptToSendEveryXSeconds: args.AttemptToSendEveryXSeconds,
        });
      }

      case 'oasis_get_provider_wallets': {
        if (!args.avatarId) {
          throw new Error('avatarId is required');
        }
        return await oasisClient.getProviderWalletsForAvatar(args.avatarId, args.providerType);
      }

      case 'oasis_get_transaction': {
        if (!args.transactionHash) {
          throw new Error('transactionHash is required');
        }
        return await oasisClient.getTransactionByHash(args.transactionHash, args.blockchain, args.providerType);
      }

      case 'oasis_get_portfolio_value': {
        if (!args.avatarId) {
          throw new Error('avatarId is required');
        }
        return await oasisClient.getPortfolioValue(args.avatarId);
      }

      case 'oasis_search_holons': {
        return await oasisClient.searchHolons({
          searchQuery: args.searchQuery,
          holonType: args.holonType,
          parentId: args.parentId,
          limit: args.limit,
          offset: args.offset,
        });
      }

      case 'oasis_load_holons_for_parent': {
        if (!args.parentId) {
          throw new Error('parentId is required');
        }
        return await oasisClient.loadHolonsForParent(args.parentId);
      }

      case 'oasis_delete_holon': {
        if (!args.holonId) {
          throw new Error('holonId is required');
        }
        return await oasisClient.deleteHolon(args.holonId);
      }

      case 'oasis_get_karma_stats': {
        if (!args.avatarId) {
          throw new Error('avatarId is required');
        }
        return await oasisClient.getKarmaStats(args.avatarId);
      }

      case 'oasis_get_karma_history': {
        if (!args.avatarId) {
          throw new Error('avatarId is required');
        }
        return await oasisClient.getKarmaHistory(args.avatarId, args.limit, args.offset);
      }

      case 'oasis_add_karma': {
        if (!args.avatarId || !args.KarmaType || !args.karmaSourceType) {
          throw new Error('avatarId, KarmaType, and karmaSourceType are required');
        }
        return await oasisClient.addKarmaToAvatar(args.avatarId, {
          KarmaType: args.KarmaType,
          karmaSourceType: args.karmaSourceType,
          KaramSourceTitle: args.KaramSourceTitle,
          KarmaSourceDesc: args.KarmaSourceDesc,
        });
      }

      case 'oasis_remove_karma': {
        if (!args.avatarId || !args.KarmaType || !args.karmaSourceType) {
          throw new Error('avatarId, KarmaType, and karmaSourceType are required');
        }
        return await oasisClient.removeKarmaFromAvatar(args.avatarId, {
          KarmaType: args.KarmaType,
          karmaSourceType: args.karmaSourceType,
          KaramSourceTitle: args.KaramSourceTitle,
          KarmaSourceDesc: args.KarmaSourceDesc,
        });
      }

      case 'oasis_search_avatars': {
        return await oasisClient.searchAvatars({
          searchQuery: args.searchQuery,
          avatarType: args.avatarType,
          limit: args.limit,
          offset: args.offset,
        });
      }

      case 'oasis_search_nfts': {
        return await oasisClient.searchNFTs({
          searchQuery: args.searchQuery,
          avatarId: args.avatarId,
          limit: args.limit,
          offset: args.offset,
        });
      }

      case 'oasis_get_all_avatar_names': {
        return await oasisClient.getAllAvatarNames(args.includeUsernames ?? true, args.includeIds ?? true);
      }

      case 'oasis_get_avatar_portrait': {
        if (args.avatarId) {
          return await oasisClient.getAvatarPortrait(args.avatarId);
        } else if (args.username) {
          return await oasisClient.getAvatarPortraitByUsername(args.username);
        } else if (args.email) {
          return await oasisClient.getAvatarPortraitByEmail(args.email);
        } else {
          throw new Error('Must provide avatarId, username, or email');
        }
      }

      case 'oasis_get_nfts_for_mint_address': {
        if (!args.mintWalletAddress) {
          throw new Error('mintWalletAddress is required');
        }
        return await oasisClient.getNFTsForMintAddress(args.mintWalletAddress);
      }

      case 'oasis_get_geo_nfts_for_mint_address': {
        if (!args.mintWalletAddress) {
          throw new Error('mintWalletAddress is required');
        }
        return await oasisClient.getGeoNFTsForMintAddress(args.mintWalletAddress);
      }

      case 'oasis_get_all_nfts': {
        return await oasisClient.getAllNFTs();
      }

      case 'oasis_get_all_geo_nfts': {
        return await oasisClient.getAllGeoNFTs();
      }

      case 'oasis_get_provider_wallets_by_username': {
        if (!args.username) {
          throw new Error('username is required');
        }
        return await oasisClient.getProviderWalletsForAvatarByUsername(args.username, args.providerType);
      }

      case 'oasis_get_provider_wallets_by_email': {
        if (!args.email) {
          throw new Error('email is required');
        }
        return await oasisClient.getProviderWalletsForAvatarByEmail(args.email, args.providerType);
      }

      case 'oasis_get_default_wallet': {
        if (!args.avatarId || !args.providerType) {
          throw new Error('avatarId and providerType are required');
        }
        return await oasisClient.getDefaultWallet(args.avatarId, args.providerType);
      }

      case 'oasis_set_default_wallet': {
        if (!args.avatarId || !args.walletId || !args.providerType) {
          throw new Error('avatarId, walletId, and providerType are required');
        }
        return await oasisClient.setDefaultWallet(args.avatarId, args.walletId, args.providerType);
      }

      case 'oasis_get_wallets_by_chain': {
        if (!args.avatarId || !args.chain) {
          throw new Error('avatarId and chain are required');
        }
        return await oasisClient.getWalletsByChain(args.avatarId, args.chain);
      }

      case 'oasis_get_wallet_analytics': {
        if (!args.avatarId || !args.walletId) {
          throw new Error('avatarId and walletId are required');
        }
        return await oasisClient.getWalletAnalytics(args.avatarId, args.walletId);
      }

      case 'oasis_get_wallet_tokens': {
        if (!args.avatarId || !args.walletId) {
          throw new Error('avatarId and walletId are required');
        }
        return await oasisClient.getWalletTokens(args.avatarId, args.walletId);
      }

      case 'oasis_get_supported_chains': {
        return await oasisClient.getSupportedChains();
      }

      case 'oasis_import_wallet_private_key': {
        if (!args.avatarId || !args.privateKey || !args.providerType) {
          throw new Error('avatarId, privateKey, and providerType are required');
        }
        return await oasisClient.importWalletPrivateKey(args.avatarId, args.privateKey, args.providerType);
      }

      case 'oasis_import_wallet_public_key': {
        if (!args.avatarId || !args.publicKey || !args.providerType) {
          throw new Error('avatarId, publicKey, and providerType are required');
        }
        return await oasisClient.importWalletPublicKey(args.avatarId, args.publicKey, args.providerType);
      }

      case 'oasis_create_wallet_full': {
        if (!args.avatarId || !args.WalletProviderType) {
          throw new Error('avatarId and WalletProviderType are required');
        }
        return await oasisClient.createWalletFull(args.avatarId, {
          Name: args.Name,
          Description: args.Description,
          WalletProviderType: args.WalletProviderType,
          GenerateKeyPair: args.GenerateKeyPair ?? true,
          IsDefaultWallet: args.IsDefaultWallet ?? false,
        }, args.providerType);
      }

      case 'oasis_create_solana_wallet': {
        if (!args.avatarId) {
          throw new Error('avatarId is required');
        }
        return await solanaWalletTools.createSolanaWallet({
          avatarId: args.avatarId,
          setAsDefault: args.setAsDefault ?? true,
          ensureProviderActivated: args.ensureProviderActivated ?? true,
        });
      }

      case 'oasis_create_ethereum_wallet': {
        if (!args.avatarId) {
          throw new Error('avatarId is required');
        }
        return await ethereumWalletTools.createEthereumWallet({
          avatarId: args.avatarId,
          setAsDefault: args.setAsDefault ?? true,
          ensureProviderActivated: args.ensureProviderActivated ?? true,
        });
      }

      case 'oasis_load_all_holons': {
        return await oasisClient.loadAllHolons();
      }

      case 'oasis_update_holon': {
        if (!args.holonId || !args.holon) {
          throw new Error('holonId and holon are required');
        }
        return await oasisClient.updateHolon(args.holonId, args.holon);
      }

      case 'oasis_get_karma_akashic_records': {
        if (!args.avatarId) {
          throw new Error('avatarId is required');
        }
        return await oasisClient.getKarmaAkashicRecords(args.avatarId);
      }

      case 'oasis_get_positive_karma_weighting': {
        if (!args.karmaType) {
          throw new Error('karmaType is required');
        }
        return await oasisClient.getPositiveKarmaWeighting(args.karmaType);
      }

      case 'oasis_get_negative_karma_weighting': {
        if (!args.karmaType) {
          throw new Error('karmaType is required');
        }
        return await oasisClient.getNegativeKarmaWeighting(args.karmaType);
      }

      case 'oasis_vote_positive_karma_weighting': {
        if (!args.karmaType || args.weighting === undefined) {
          throw new Error('karmaType and weighting are required');
        }
        return await oasisClient.voteForPositiveKarmaWeighting(args.karmaType, args.weighting);
      }

      case 'oasis_vote_negative_karma_weighting': {
        if (!args.karmaType || args.weighting === undefined) {
          throw new Error('karmaType and weighting are required');
        }
        return await oasisClient.voteForNegativeKarmaWeighting(args.karmaType, args.weighting);
      }

      case 'oasis_basic_search': {
        if (!args.searchQuery) {
          throw new Error('searchQuery is required');
        }
        return await oasisClient.basicSearch(args.searchQuery, args.limit, args.offset);
      }

      case 'oasis_advanced_search': {
        return await oasisClient.advancedSearch({
          searchQuery: args.searchQuery,
          entityType: args.entityType,
          filters: args.filters,
          limit: args.limit,
          offset: args.offset,
        });
      }

      case 'oasis_search_files': {
        return await oasisClient.searchFiles({
          searchQuery: args.searchQuery,
          avatarId: args.avatarId,
          fileType: args.fileType,
          limit: args.limit,
          offset: args.offset,
        });
      }

      // ============================================
      // A2A Protocol & SERV Infrastructure Handlers
      // ============================================
      case 'oasis_get_agent_card': {
        if (!args.agentId) {
          throw new Error('agentId is required');
        }
        return await oasisClient.getAgentCard(args.agentId);
      }

      case 'oasis_get_all_agents': {
        return await oasisClient.getAllAgents();
      }

      case 'oasis_get_agents_by_service': {
        if (!args.serviceName) {
          throw new Error('serviceName is required');
        }
        return await oasisClient.getAgentsByService(args.serviceName);
      }

      case 'oasis_get_my_agents': {
        return await oasisClient.getMyAgents();
      }

      case 'oasis_register_agent_capabilities': {
        if (!args.services || !Array.isArray(args.services) || args.services.length === 0) {
          throw new Error('services array is required and must not be empty');
        }
        return await oasisClient.registerAgentCapabilities({
          services: args.services,
          skills: args.skills,
          pricing: args.pricing,
          status: args.status,
          max_concurrent_tasks: args.max_concurrent_tasks,
          description: args.description,
        });
      }

      case 'oasis_register_agent_as_serv_service': {
        return await oasisClient.registerAgentAsSERVService();
      }

      case 'oasis_discover_agents_via_serv': {
        return await oasisClient.discoverAgentsViaSERV(args.serviceName);
      }

      case 'oasis_send_a2a_jsonrpc_request': {
        if (!args.method) {
          throw new Error('method is required');
        }
        const requestId = args.id || `req-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
        return await oasisClient.sendA2AJsonRpcRequest({
          jsonrpc: '2.0',
          method: args.method,
          params: args.params || {},
          id: requestId,
        });
      }

      case 'oasis_get_pending_a2a_messages': {
        return await oasisClient.getPendingA2AMessages();
      }

      case 'oasis_mark_a2a_message_processed': {
        if (!args.messageId) {
          throw new Error('messageId is required');
        }
        return await oasisClient.markA2AMessageProcessed(args.messageId);
      }

      case 'oasis_register_openserv_agent': {
        if (!args.openServAgentId || !args.openServEndpoint || !args.capabilities) {
          throw new Error('openServAgentId, openServEndpoint, and capabilities are required');
        }
        if (!Array.isArray(args.capabilities) || args.capabilities.length === 0) {
          throw new Error('capabilities must be a non-empty array');
        }
        return await oasisClient.registerOpenServAgent({
          openServAgentId: args.openServAgentId,
          openServEndpoint: args.openServEndpoint,
          capabilities: args.capabilities,
          apiKey: args.apiKey,
          username: args.username,
          email: args.email,
          password: args.password,
        });
      }

      case 'oasis_execute_ai_workflow': {
        if (!args.toAgentId || !args.workflowRequest) {
          throw new Error('toAgentId and workflowRequest are required');
        }
        return await oasisClient.executeAIWorkflow({
          toAgentId: args.toAgentId,
          workflowRequest: args.workflowRequest,
          workflowParameters: args.workflowParameters,
        });
      }

      case 'solana_send_sol': {
        if (!args.fromPrivateKey || !args.toAddress || args.amount === undefined) {
          throw new Error('fromPrivateKey, toAddress, and amount are required');
        }
        return await solanaRpcClient.sendSol({
          fromPrivateKey: args.fromPrivateKey,
          toAddress: args.toAddress,
          amount: args.amount,
          network: args.network || 'devnet',
        });
      }

      case 'solana_get_balance': {
        if (!args.address) {
          throw new Error('address is required');
        }
        return await solanaRpcClient.getBalance(
          args.address,
          args.network || 'devnet'
        );
      }

      case 'solana_get_transaction': {
        if (!args.signature) {
          throw new Error('signature is required');
        }
        return await solanaRpcClient.getTransaction(
          args.signature,
          args.network || 'devnet'
        );
      }

      default:
        throw new Error(`Unknown tool: ${name}`);
    }
  } catch (error: any) {
    return {
      error: true,
      message: error.message || 'Unknown error',
      details: error.response?.data || error.stack,
    };
  }
}

