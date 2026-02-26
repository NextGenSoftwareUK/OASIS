import { Tool } from '@modelcontextprotocol/sdk/types.js';
import { OASISClient } from '../clients/oasisClient.js';
import { SolanaRpcClient } from '../clients/solanaRpcClient.js';
import { SolanaWalletMCPTools } from '../../solana-wallet-tools.js';
import { EthereumWalletMCPTools } from '../../ethereum-wallet-tools.js';
import { config } from '../config.js';
import fs from 'fs';
import path from 'path';
import axios from 'axios';

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
    description: 'Mint a new Solana NFT interactively. Can be called with partial information - the AI will prompt for missing required fields. Minimum required: JSONMetaDataURL and Symbol. Defaults: OnChainProvider=SolanaOASIS, OffChainProvider=MongoDBOASIS, NFTOffChainMetaType=OASIS, NFTStandardType=SPL. When called with incomplete information, returns a prompt object indicating what is needed.',
    inputSchema: {
      type: 'object',
      properties: {
        JSONMetaDataURL: {
          type: 'string',
          description: 'URL to NFT metadata (JSON or IPFS). If not provided, you will be prompted for it. For testing, you can use: https://jsonplaceholder.typicode.com/posts/1',
        },
        Symbol: {
          type: 'string',
          description: 'NFT symbol/ticker (e.g., "MYNFT", "ART123"). If not provided, you will be prompted for it.',
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
          description: 'Off-chain provider (optional, defaults to "MongoDBOASIS")',
        },
        NFTOffChainMetaType: {
          type: 'string',
          description: 'NFT off-chain metadata type (optional, defaults to "OASIS")',
        },
        NFTStandardType: {
          type: 'string',
          description: 'NFT standard type (optional, defaults to "SPL")',
        },
        MetaData: {
          type: 'object',
          description: 'Additional metadata object with custom attributes (optional)',
        },
        X402Enabled: {
          type: 'boolean',
          description: 'Enable x402 revenue sharing for automatic payment distribution to NFT holders (optional, default: false)',
        },
        X402PaymentEndpoint: {
          type: 'string',
          description: 'x402 payment endpoint URL for revenue distribution webhooks (required if X402Enabled is true)',
        },
        X402RevenueModel: {
          type: 'string',
          description: 'x402 revenue distribution model: "equal" (equal split), "weighted" (by ownership), or "creator-split" (optional, default: "equal")',
          enum: ['equal', 'weighted', 'creator-split'],
        },
        X402TreasuryWallet: {
          type: 'string',
          description: 'Treasury wallet address that receives payments for x402 distribution (optional)',
        },
        SendToAddressAfterMinting: {
          type: 'string',
          description: 'Wallet address to send NFT to after minting (optional)',
        },
        SendToAvatarAfterMintingId: {
          type: 'string',
          description: 'Avatar ID to send NFT to after minting (optional)',
        },
        Cluster: {
          type: 'string',
          description: 'Solana cluster: "devnet" (default for testing, no real SOL) or "mainnet-beta". Use devnet to avoid mainnet fees.',
          enum: ['devnet', 'mainnet-beta', 'mainnet'],
        },
      },
      required: [], // Made optional to support interactive mode - handler will prompt for missing required fields
    },
  },
  {
    name: 'oasis_get_token_metadata_by_mint',
    description: 'Get Solana token metadata by mint address (e.g. memecoin from Solscan). Returns name, symbol, uri, image, description. Use to convert a memecoin/SPL token to an NFT: call this first, then oasis_mint_nft with the returned Title, Symbol, ImageUrl, Description, JSONMetaDataURL.',
    inputSchema: {
      type: 'object',
      properties: {
        mint: {
          type: 'string',
          description: 'Solana token/mint address (e.g. from Solscan token URL: https://solscan.io/token/<mint>)',
        },
      },
      required: ['mint'],
    },
  },
  {
    name: 'oasis_create_nft',
    description: 'Single consistent way to create an NFT: authenticate avatar → generate image with Glif → mint NFT. If required fields are missing, returns prompts so the agent can ask the user. Required: username, password, imagePrompt, symbol. Optional: title, description, numberToMint, price, workflowId.',
    inputSchema: {
      type: 'object',
      properties: {
        username: {
          type: 'string',
          description: 'OASIS avatar username (required)',
        },
        password: {
          type: 'string',
          description: 'OASIS avatar password (required)',
        },
        imagePrompt: {
          type: 'string',
          description: 'Text prompt for Glif to generate the NFT image (required, e.g. "A futuristic OASIS digital art, holographic, cyberpunk, 4k")',
        },
        symbol: {
          type: 'string',
          description: 'NFT symbol/ticker (required, e.g. "OASISART", "MYNFT")',
        },
        title: {
          type: 'string',
          description: 'NFT title (optional, defaults to symbol)',
        },
        description: {
          type: 'string',
          description: 'NFT description (optional)',
        },
        numberToMint: {
          type: 'number',
          description: 'Number of NFTs to mint (optional, default 1)',
        },
        price: {
          type: 'number',
          description: 'NFT price in SOL (optional, default 0)',
        },
        workflowId: {
          type: 'string',
          description: 'Glif workflow ID (optional, uses default image generation workflow)',
        },
      },
      required: [], // Optional at call time so tool can be invoked with no args and return interactive prompts for missing fields
    },
  },
  {
    name: 'oasis_upload_file',
    description: 'Upload a file (image, JSON, etc.) to Pinata/IPFS storage. Returns the IPFS URL that can be used for NFT metadata or images. Requires authentication.',
    inputSchema: {
      type: 'object',
      properties: {
        filePath: {
          type: 'string',
          description: 'Path to the file to upload (absolute or relative to workspace root)',
        },
        provider: {
          type: 'string',
          description: 'Storage provider (default: "PinataOASIS")',
        },
      },
      required: ['filePath'],
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
    description: 'Get detailed avatar information including portrait by ID, username, or email',
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
    description: 'Get provider wallets for an avatar. Can lookup by avatarId, username, or email.',
    inputSchema: {
      type: 'object',
      properties: {
        avatarId: {
          type: 'string',
          description: 'Avatar ID (UUID)',
        },
        username: {
          type: 'string',
          description: 'Avatar username (alternative to avatarId)',
        },
        email: {
          type: 'string',
          description: 'Avatar email (alternative to avatarId)',
        },
        providerType: {
          type: 'string',
          description: 'Provider type (optional)',
        },
      },
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
    description: 'Search holons (data objects). Use parentId parameter to load holons for a specific parent.',
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
          description: 'Parent ID filter (optional). Use this to load all holons for a specific parent.',
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
    name: 'oasis_place_geo_nft',
    description: 'Place (geocache) an existing NFT at real-world coordinates. The NFT must be owned by the authenticated avatar. Coordinates are in standard degrees (e.g., 51.5074, -0.1278 for London).',
    inputSchema: {
      type: 'object',
      properties: {
        originalOASISNFTId: {
          type: 'string',
          description: 'The OASIS NFT ID to place at the coordinates (must be owned by you)',
        },
        latitude: {
          type: 'number',
          description: 'Latitude in degrees (e.g., 51.5074 for London). Will be automatically converted to micro-degrees.',
        },
        longitude: {
          type: 'number',
          description: 'Longitude in degrees (e.g., -0.1278 for London). Will be automatically converted to micro-degrees.',
        },
        allowOtherPlayersToAlsoCollect: {
          type: 'boolean',
          description: 'Allow other players to collect this GeoNFT (default: true)',
        },
        permSpawn: {
          type: 'boolean',
          description: 'Permanent spawn (default: false)',
        },
        globalSpawnQuantity: {
          type: 'number',
          description: 'Global spawn quantity (default: 1)',
        },
        playerSpawnQuantity: {
          type: 'number',
          description: 'Player spawn quantity (default: 1)',
        },
        respawnDurationInSeconds: {
          type: 'number',
          description: 'Respawn duration in seconds (default: 0)',
        },
        geoNFTMetaDataProvider: {
          type: 'string',
          description: 'GeoNFT metadata provider (default: MongoDBOASIS)',
        },
        originalOASISNFTOffChainProvider: {
          type: 'string',
          description: 'Original NFT off-chain provider (default: MongoDBOASIS)',
        },
      },
      required: ['originalOASISNFTId', 'latitude', 'longitude'],
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
    name: 'oasis_advanced_search',
    description: 'Advanced search with filters. Use entityType parameter to search for specific entity types (avatars, NFTs, files, etc.).',
    inputSchema: {
      type: 'object',
      properties: {
        searchQuery: {
          type: 'string',
          description: 'Search query string',
        },
        entityType: {
          type: 'string',
          description: 'Entity type filter (optional). Use "Avatar" for avatars, "NFT" for NFTs, "File" for files, etc.',
        },
        filters: {
          type: 'object',
          description: 'Additional filters (optional). For avatars: {avatarType: "User"}, For NFTs: {avatarId: "..."}, For files: {fileType: "..."}',
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
  {
    name: 'glif_generate_image',
    description: 'Generate an image using Glif.app API (easiest option - uses pre-built workflows). Supports reference images for better accuracy. Returns the generated image that can be used for NFT minting.',
    inputSchema: {
      type: 'object',
      properties: {
        prompt: {
          type: 'string',
          description: 'Text prompt describing the image to generate (e.g., "A Gundam robot in neo-tokyo style")',
        },
        referenceImagePath: {
          type: 'string',
          description: 'Path to a reference image file (optional). If provided, the AI will use this image as a style/context reference. Can be a local file path or you can paste images in chat and I\'ll save them first.',
        },
        referenceImageUrl: {
          type: 'string',
          description: 'URL to a reference image (optional). Alternative to referenceImagePath if you have an image URL.',
        },
        workflowId: {
          type: 'string',
          description: 'Glif workflow ID (optional, uses default image generation workflow if not provided)',
        },
        savePath: {
          type: 'string',
          description: 'Path to save the generated image (optional, defaults to NFT_Content/generated-{timestamp}.png)',
        },
      },
      required: ['prompt'],
    },
  },
  {
    name: 'nanobanana_generate_image',
    description: 'Generate an image using Nano Banana API (Google Gemini-powered, more accurate than Glif). Supports up to 8 reference images for precise results. Best for accurate character/scene generation. Returns the generated image that can be used for NFT minting.',
    inputSchema: {
      type: 'object',
      properties: {
        prompt: {
          type: 'string',
          description: 'Detailed text prompt describing the image to generate (e.g., "Epic battle scene: Godzilla, a massive dark green reptilian kaiju with jagged dorsal fins, standing on hind legs roaring with atomic blue energy. Mothra, a colossal moth with orange-brown fuzzy body, enormous wings with black, orange, and yellow patterns, hovering above with wings spread")',
        },
        referenceImageUrls: {
          type: 'array',
          items: { type: 'string' },
          description: 'Array of reference image URLs (optional, up to 8 images). These will be used to guide the AI for accurate character/scene generation.',
        },
        aspectRatio: {
          type: 'string',
          description: 'Aspect ratio (optional, default: "16:9"). Options: "1:1", "16:9", "9:16", "4:3", "3:4"',
        },
        size: {
          type: 'string',
          description: 'Image size (optional, default: "1024x1024"). Examples: "1024x1024", "1920x1080"',
        },
        savePath: {
          type: 'string',
          description: 'Path to save the generated image (optional, defaults to NFT_Content/generated-{timestamp}.png)',
        },
      },
      required: ['prompt'],
    },
  },
  {
    name: 'ltx_generate_video',
    description: 'Generate a video using LTX.io API. Supports text-to-video and image-to-video generation. Returns MP4 video that can be saved and used for NFT minting or other purposes.',
    inputSchema: {
      type: 'object',
      properties: {
        prompt: {
          type: 'string',
          description: 'Text prompt describing the video to generate (required for text-to-video, optional for image-to-video to guide motion)',
        },
        imageUrl: {
          type: 'string',
          description: 'URL of an image to animate (for image-to-video). If provided, will generate video from this image.',
        },
        imageBase64: {
          type: 'string',
          description: 'Base64 encoded image to animate (for image-to-video). Alternative to imageUrl.',
        },
        model: {
          type: 'string',
          enum: ['ltx-2-fast', 'ltx-2-pro'],
          description: 'LTX model to use: ltx-2-fast (faster, optimized for speed) or ltx-2-pro (higher quality, maximum detail). Default: ltx-2-fast',
        },
        duration: {
          type: 'number',
          description: 'Video duration in seconds (1-20, default: 5)',
        },
        resolution: {
          type: 'string',
          description: 'Video resolution (e.g., "1920x1080", "3840x2160" for 4K). Default: "1920x1080"',
        },
        aspectRatio: {
          type: 'string',
          description: 'Aspect ratio (e.g., "16:9", "9:16", "1:1"). Default: "16:9"',
        },
        fps: {
          type: 'number',
          description: 'Frames per second (up to 50, default: 24)',
        },
        savePath: {
          type: 'string',
          description: 'Path to save the generated video (optional, defaults to NFT_Content/generated-{timestamp}.mp4)',
        },
      },
      required: [],
    },
  },
];

export async function handleOASISTool(
  name: string,
  args: any
): Promise<any> {
  try {
    switch (name) {
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
        // Interactive mode: Check for missing required fields and return prompts
        const missingFields: string[] = [];
        const recommendedFields: string[] = [];
        const optionalFields: string[] = [];
        const prompts: Record<string, string> = {};
        const recommendedPrompts: Record<string, string> = {};
        const optionalPrompts: Record<string, string> = {};
        
        // Required fields
        if (!args.JSONMetaDataURL) {
          missingFields.push('JSONMetaDataURL');
          prompts.JSONMetaDataURL = 'Please provide a URL to the NFT metadata JSON file (or use a placeholder like https://jsonplaceholder.typicode.com/posts/1 for testing)';
        }
        
        if (!args.Symbol) {
          missingFields.push('Symbol');
          prompts.Symbol = 'Please provide a symbol/ticker for your NFT (e.g., "MYNFT", "ART123")';
        }
        
        // Recommended fields (not strictly required, but highly recommended)
        if (!args.ImageUrl) {
          recommendedFields.push('ImageUrl');
          recommendedPrompts.ImageUrl = 'Please provide an image URL for your NFT. Options: 1) Upload a local image file to Pinata/IPFS (I can help with that), 2) Use a local image server (start server in NFT_Content folder), 3) Use any public image URL, or 4) Use placeholder: https://via.placeholder.com/512';
        }
        
        if (args.NumberToMint === undefined || args.NumberToMint === null) {
          recommendedFields.push('NumberToMint');
          recommendedPrompts.NumberToMint = 'How many NFTs would you like to mint? (Default: 1). Useful for creating multiple copies or a collection.';
        }
        
        if (!args.Title) {
          recommendedFields.push('Title');
          recommendedPrompts.Title = 'What would you like to name your NFT? (e.g., "My Awesome Artwork", "Digital Masterpiece")';
        }
        
        if (!args.Description) {
          optionalFields.push('Description');
          optionalPrompts.Description = 'Would you like to add a description for your NFT? This helps explain what the NFT is about.';
        }
        
        // Optional but useful fields
        if (!args.MetaData || !args.MetaData.attributes) {
          optionalFields.push('Attributes/Traits');
          optionalPrompts['Attributes/Traits'] = 'Would you like to add attributes/traits? (e.g., Color: Blue, Rarity: Common, Artist: Your Name). These help with filtering and rarity systems.';
        }
        
        if (!args.MetaData || !args.MetaData.external_url) {
          optionalFields.push('External URL');
          optionalPrompts['External URL'] = 'Do you have a website or project page URL for this NFT? (Optional - links to your project/collection page)';
        }
        
        if (!args.MetaData || !args.MetaData.category) {
          optionalFields.push('Category');
          optionalPrompts.Category = 'What category is your NFT? (e.g., "image", "video", "audio", "3d"). Helps platforms categorize your NFT.';
        }
        
        if (args.Price === undefined || args.Price === null) {
          optionalFields.push('Price');
          optionalPrompts.Price = 'Would you like to set a price for this NFT? (Default: 0 - free). Enter 0 if not for sale.';
        }
        
        // x402 Revenue Sharing (optional) - with natural language handling
        let x402Enabled = args.X402Enabled;
        
        // Handle natural language responses for x402
        if (args.X402Enabled === undefined || args.X402Enabled === null) {
          // Check if user provided a natural language response
          const x402Response = args.X402Response || args['x402 Revenue Sharing'] || args['Enable x402'];
          if (x402Response) {
            const responseLower = String(x402Response).toLowerCase().trim();
            if (responseLower === 'yes' || responseLower === 'y' || responseLower === 'true' || responseLower === 'enable' || responseLower === 'enable x402') {
              x402Enabled = true;
            } else if (responseLower === 'no' || responseLower === 'n' || responseLower === 'false' || responseLower === 'disable' || responseLower === 'skip') {
              x402Enabled = false;
            }
          }
        }
        
        if (x402Enabled === undefined || x402Enabled === null) {
          optionalFields.push('x402 Revenue Sharing');
          optionalPrompts['x402 Revenue Sharing'] = 'Would you like to enable automatic revenue sharing for this NFT? When enabled, any payments your NFT receives will be automatically distributed to all NFT holders. This is great for creating revenue-generating NFTs where holders earn from usage. Answer "yes" to enable revenue sharing, or "no" to skip this feature.';
        } else {
          // Update args with parsed value
          args.X402Enabled = x402Enabled;
        }
        
        // If x402 is enabled, check for payment endpoint
        if (x402Enabled === true && !args.X402PaymentEndpoint) {
          // Check for natural language responses
          const endpointResponse = args.X402EndpointResponse || args['x402 Payment Endpoint'] || args['Payment Endpoint'];
          if (endpointResponse) {
            const responseLower = String(endpointResponse).toLowerCase().trim();
            if (responseLower === 'create endpoint' || responseLower === 'create' || responseLower === 'generate' || responseLower === 'auto' || responseLower === 'use oasis' || responseLower === 'oasis') {
              // Generate a default endpoint URL based on OASIS API
              const baseUrl = config.oasisApiUrl.replace(/\/$/, '');
              const symbol = args.Symbol || 'nft';
              args.X402PaymentEndpoint = `${baseUrl}/api/x402/revenue/${symbol.toLowerCase()}`;
              console.error(`[MCP] ✅ Auto-generated x402 payment endpoint: ${args.X402PaymentEndpoint}`);
              console.error(`[MCP] Note: This endpoint will be created automatically by the OASIS API when payments are received.`);
            } else if (responseLower === 'skip' || responseLower === 'no' || responseLower === 'disable') {
              // Disable x402 if user wants to skip
              args.X402Enabled = false;
              x402Enabled = false;
            } else if (endpointResponse.match(/^https?:\/\//)) {
              // User provided a URL
              args.X402PaymentEndpoint = endpointResponse;
            }
          }
          
          if (x402Enabled === true && !args.X402PaymentEndpoint) {
            optionalFields.push('x402 Payment Endpoint');
            optionalPrompts['x402 Payment Endpoint'] = 'To enable revenue sharing, we need a payment endpoint URL. This is where payment notifications will be sent when your NFT receives payments. You have three options: 1) Say "create endpoint" or "use oasis" to automatically generate an endpoint using the OASIS API (recommended - easiest option), 2) Provide your own custom endpoint URL (e.g., "https://api.yourservice.com/x402/revenue"), or 3) Say "skip" to disable revenue sharing. What would you like to do?';
          }
        }
        
        // If required or recommended fields are missing, return interactive prompt
        // Optional fields are truly optional - don't block minting if only they're missing
        if (missingFields.length > 0 || recommendedFields.length > 0) {
          const allFields = [...missingFields, ...recommendedFields, ...optionalFields];
          const allPrompts = { ...prompts, ...recommendedPrompts, ...optionalPrompts };
          
          return {
            interactive: true,
            needsMoreInfo: true,
            missingFields: missingFields,
            recommendedFields: recommendedFields,
            optionalFields: optionalFields,
            prompts: allPrompts,
            providedFields: {
              Title: args.Title || null,
              Description: args.Description || null,
              ImageUrl: args.ImageUrl || null,
              NumberToMint: args.NumberToMint !== undefined ? args.NumberToMint : null,
              Price: args.Price !== undefined ? args.Price : null,
              X402Enabled: x402Enabled !== undefined ? x402Enabled : (args.X402Enabled !== undefined ? args.X402Enabled : null),
              X402PaymentEndpoint: args.X402PaymentEndpoint || null,
              OnChainProvider: args.OnChainProvider || 'SolanaOASIS (default)',
              NFTStandardType: args.NFTStandardType || 'SPL (default)',
            },
            message: missingFields.length > 0 
              ? `I need a bit more information to mint your NFT. Required: ${missingFields.join(', ')}${recommendedFields.length > 0 ? `. Also recommended: ${recommendedFields.join(', ')}` : ''}${optionalFields.length > 0 ? `. Optional but useful: ${optionalFields.join(', ')}` : ''}`
              : recommendedFields.length > 0
              ? `I have the required information. Would you like to provide: ${recommendedFields.join(', ')}?${optionalFields.length > 0 ? ` Also optional: ${optionalFields.join(', ')}` : ''}`
              : `I have the required and recommended information. Optional fields available: ${optionalFields.join(', ')}`,
            help: 'You can provide the missing information and I\'ll proceed with minting. For testing, you can use placeholder URLs like https://jsonplaceholder.typicode.com/posts/1 for JSONMetaDataURL and https://via.placeholder.com/512 for images. You can provide all information at once or answer prompts one by one.'
          };
        }
        
        // All required fields present, check for local file paths and auto-upload to Pinata
        let imageUrl = args.ImageUrl;
        let thumbnailUrl = args.ThumbnailUrl;
        
        // Auto-detect and upload local file paths to Pinata
        const fs = await import('fs');
        const path = await import('path');
        
        // Check if ImageUrl is a local file path (not a URL)
        if (imageUrl && !imageUrl.match(/^https?:\/\//) && !imageUrl.startsWith('ipfs://')) {
          // Try to resolve the path (could be relative or absolute)
          let resolvedPath = imageUrl;
          if (!path.isAbsolute(imageUrl)) {
            // Try relative to workspace root (common case: NFT_Content/file.png)
            resolvedPath = path.resolve(process.cwd(), imageUrl);
          }
          
          // Check if file exists
          if (fs.existsSync(resolvedPath)) {
            try {
              console.error(`[MCP] Detected local file path, auto-uploading to Pinata: ${resolvedPath}`);
              // Upload to Pinata automatically using PinataOASIS
              const uploadResult = await oasisClient.uploadFile(resolvedPath, 'PinataOASIS');
              if (uploadResult && uploadResult.result && !uploadResult.isError) {
                imageUrl = uploadResult.result; // IPFS URL from Pinata
                console.error(`[MCP] ✅ Successfully uploaded to Pinata: ${imageUrl}`);
              } else {
                console.error(`[MCP] ⚠️ Failed to upload image to Pinata: ${uploadResult?.message || 'Unknown error'}`);
                throw new Error(`Failed to upload image to Pinata: ${uploadResult?.message || 'Unknown error'}`);
              }
            } catch (uploadError: any) {
              console.error(`[MCP] ❌ Error auto-uploading image: ${uploadError.message}`);
              throw new Error(`Failed to auto-upload image to Pinata: ${uploadError.message}`);
            }
          } else {
            console.error(`[MCP] ⚠️ File not found at path: ${resolvedPath}`);
            throw new Error(`Image file not found: ${resolvedPath}`);
          }
        }
        
        // Check if ThumbnailUrl is a local file path
        if (thumbnailUrl && !thumbnailUrl.match(/^https?:\/\//) && !thumbnailUrl.startsWith('ipfs://')) {
          let resolvedPath = thumbnailUrl;
          if (!path.isAbsolute(thumbnailUrl)) {
            resolvedPath = path.resolve(process.cwd(), thumbnailUrl);
          }
          
          if (fs.existsSync(resolvedPath)) {
            try {
              console.error(`[MCP] Auto-uploading thumbnail to Pinata: ${resolvedPath}`);
              const uploadResult = await oasisClient.uploadFile(resolvedPath, 'PinataOASIS');
              if (uploadResult && uploadResult.result && !uploadResult.isError) {
                thumbnailUrl = uploadResult.result;
                console.error(`[MCP] ✅ Thumbnail uploaded: ${thumbnailUrl}`);
              }
            } catch (uploadError: any) {
              console.error(`[MCP] ⚠️ Error auto-uploading thumbnail: ${uploadError.message}`);
              // Don't fail minting if thumbnail upload fails
            }
          }
        }
        
        // Build MetaData object, including x402 config if enabled
        let metadata = args.MetaData || {};
        
        // Add x402 configuration to metadata if enabled
        // Use the parsed x402Enabled value (handles natural language)
        const finalX402Enabled = x402Enabled !== undefined ? x402Enabled : args.X402Enabled;
        
        if (finalX402Enabled === true && args.X402PaymentEndpoint) {
          metadata.x402Config = {
            enabled: true,
            paymentEndpoint: args.X402PaymentEndpoint,
            revenueModel: args.X402RevenueModel || 'equal',
            treasuryWallet: args.X402TreasuryWallet || '',
            preAuthorizeDistributions: false,
            metadata: {
              contentType: metadata.category || 'other',
              distributionFrequency: 'realtime',
              revenueSharePercentage: 100
            }
          };
          
          console.error(`[MCP] ✅ x402 Revenue Sharing enabled with endpoint: ${args.X402PaymentEndpoint}`);
        } else if (finalX402Enabled === true && !args.X402PaymentEndpoint) {
          console.error(`[MCP] ⚠️ x402 enabled but no payment endpoint provided - x402 will be disabled`);
          // Don't add x402 config if endpoint is missing
        }
        
        // Proceed with minting (using uploaded URLs if files were uploaded)
        return await oasisClient.mintNFT({
          JSONMetaDataURL: args.JSONMetaDataURL,
          Symbol: args.Symbol,
          Title: args.Title,
          Description: args.Description,
          ImageUrl: imageUrl,
          ThumbnailUrl: thumbnailUrl,
          Price: args.Price,
          NumberToMint: args.NumberToMint,
          OnChainProvider: args.OnChainProvider,
          OffChainProvider: args.OffChainProvider,
          NFTOffChainMetaType: args.NFTOffChainMetaType,
          NFTStandardType: args.NFTStandardType,
          StoreNFTMetaDataOnChain: args.StoreNFTMetaDataOnChain,
          MetaData: metadata,
          SendToAddressAfterMinting: args.SendToAddressAfterMinting,
          SendToAvatarAfterMintingId: args.SendToAvatarAfterMintingId,
          SendToAvatarAfterMintingUsername: args.SendToAvatarAfterMintingUsername,
          SendToAvatarAfterMintingEmail: args.SendToAvatarAfterMintingEmail,
          WaitTillNFTMinted: args.WaitTillNFTMinted,
          WaitForNFTToMintInSeconds: args.WaitForNFTToMintInSeconds,
          AttemptToMintEveryXSeconds: args.AttemptToMintEveryXSeconds,
          Cluster: args.Cluster,
        });
      }

      case 'oasis_create_nft': {
        // Interactive: prompt for missing required inputs so the agent can ask the user
        const missingFields: string[] = [];
        const prompts: Record<string, string> = {};
        if (!args.username) {
          missingFields.push('username');
          prompts.username = 'OASIS avatar username (e.g. OASIS_ADMIN)';
        }
        if (!args.password) {
          missingFields.push('password');
          prompts.password = 'OASIS avatar password';
        }
        if (!args.imagePrompt) {
          missingFields.push('imagePrompt');
          prompts.imagePrompt = 'Text prompt for the NFT image (e.g. "A futuristic OASIS digital art, holographic, cyberpunk, 4k")';
        }
        if (!args.symbol) {
          missingFields.push('symbol');
          prompts.symbol = 'NFT symbol/ticker (e.g. OASISART, MYNFT)';
        }
        if (missingFields.length > 0) {
          return {
            interactive: true,
            needsMoreInfo: true,
            missingFields,
            prompts,
            message: `To create an NFT I need: ${missingFields.join(', ')}. Please provide these and I'll run the full workflow (authenticate → Glif image → mint).`,
            optionalFields: {
              title: 'NFT title (optional, defaults to symbol)',
              description: 'NFT description (optional)',
              numberToMint: 'Number to mint (optional, default 1)',
              price: 'Price in SOL (optional, default 0)',
            },
          };
        }
        return await oasisClient.createNFTWithGlif({
          username: args.username,
          password: args.password,
          imagePrompt: args.imagePrompt,
          symbol: args.symbol,
          title: args.title,
          description: args.description,
          numberToMint: args.numberToMint,
          price: args.price,
          workflowId: args.workflowId,
        });
      }

      case 'oasis_upload_file': {
        if (!args.filePath) {
          throw new Error('filePath is required');
        }
        return await oasisClient.uploadFile(args.filePath, args.provider);
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
        if (args.avatarId) {
        return await oasisClient.getProviderWalletsForAvatar(args.avatarId, args.providerType);
        } else if (args.username) {
          return await oasisClient.getProviderWalletsForAvatarByUsername(args.username, args.providerType);
        } else if (args.email) {
          return await oasisClient.getProviderWalletsForAvatarByEmail(args.email, args.providerType);
        } else {
          throw new Error('Must provide avatarId, username, or email');
        }
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

      case 'oasis_get_token_metadata_by_mint': {
        if (!args.mint) {
          throw new Error('mint is required (Solana token/mint address, e.g. from Solscan token URL)');
        }
        return await oasisClient.getTokenMetadataByMint(args.mint);
      }

      case 'oasis_place_geo_nft': {
        if (!args.originalOASISNFTId || args.latitude === undefined || args.longitude === undefined) {
          return {
            error: true,
            message: 'originalOASISNFTId, latitude, and longitude are required',
          };
        }

        return await oasisClient.placeGeoNFT({
          originalOASISNFTId: args.originalOASISNFTId,
          latitude: args.latitude,
          longitude: args.longitude,
          allowOtherPlayersToAlsoCollect: args.allowOtherPlayersToAlsoCollect,
          permSpawn: args.permSpawn,
          globalSpawnQuantity: args.globalSpawnQuantity,
          playerSpawnQuantity: args.playerSpawnQuantity,
          respawnDurationInSeconds: args.respawnDurationInSeconds,
          geoNFTMetaDataProvider: args.geoNFTMetaDataProvider,
          originalOASISNFTOffChainProvider: args.originalOASISNFTOffChainProvider,
        });
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

      case 'oasis_advanced_search': {
        return await oasisClient.advancedSearch({
          searchQuery: args.searchQuery,
          entityType: args.entityType,
          filters: args.filters,
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

      case 'glif_generate_image': {
        if (!args.prompt) {
          return {
            error: true,
            message: 'Prompt is required for image generation',
          };
        }

        // Generate image using Glif.app (easiest option)
        // Supports reference images for better accuracy
        const result = await oasisClient.generateImageWithGlif({
          workflowId: args.workflowId,
          prompt: args.prompt,
          referenceImagePath: args.referenceImagePath,
          referenceImageUrl: args.referenceImageUrl,
        });

        if (result.error) {
          return {
            error: true,
            message: `Image generation failed: ${result.error}`,
          };
        }

        if (!result.imageUrl) {
          return {
            error: true,
            message: 'Image generated but no image URL returned',
          };
        }

        // Save image to file
        const savePath = args.savePath || path.join(
          process.cwd(),
          'NFT_Content',
          `generated-${Date.now()}.png`
        );

        // Ensure directory exists
        const dir = path.dirname(savePath);
        if (!fs.existsSync(dir)) {
          fs.mkdirSync(dir, { recursive: true });
        }

        // Download image from URL and save
        try {
          const imageResponse = await axios.get(result.imageUrl, { 
            responseType: 'arraybuffer',
            timeout: 30000,
          });
          fs.writeFileSync(savePath, imageResponse.data);
          
          // Convert to base64 for preview in MCP response
          const imageBase64 = Buffer.from(imageResponse.data).toString('base64');
          
          // Determine MIME type from file extension or default to PNG
          const ext = path.extname(savePath).toLowerCase();
          const mimeType = ext === '.jpg' || ext === '.jpeg' ? 'image/jpeg' : 
                          ext === '.gif' ? 'image/gif' : 
                          ext === '.webp' ? 'image/webp' : 'image/png';
          
          return {
            success: true,
            imagePath: savePath,
            imageUrl: result.imageUrl,
            imageBase64: imageBase64, // Include base64 for preview
            mimeType: mimeType, // Include MIME type for proper display
            message: `Image generated and saved to ${savePath}. Preview shown above. You can now use this image for NFT minting.`,
            prompt: args.prompt,
          };
        } catch (downloadError: any) {
          return {
            error: true,
            message: `Failed to download image: ${downloadError.message}`,
            imageUrl: result.imageUrl, // Still return URL in case user wants to use it directly
          };
        }
      }

      case 'nanobanana_generate_image': {
        const fs = await import('fs');
        const path = await import('path');
        const axios = await import('axios');

        if (!args.prompt) {
          return {
            error: true,
            message: 'Prompt is required for image generation',
          };
        }

        // Generate image using Nano Banana (more accurate, supports reference images)
        const result = await oasisClient.generateImageWithNanoBanana({
          prompt: args.prompt,
          referenceImageUrls: args.referenceImageUrls,
          aspectRatio: args.aspectRatio,
          size: args.size,
        });

        if (result.error) {
          return {
            error: true,
            message: `Image generation failed: ${result.error}`,
          };
        }

        if (!result.imageUrl) {
          return {
            error: true,
            message: 'Image generated but no image URL returned',
          };
        }

        // Save image to file
        const savePath = args.savePath || path.join(
          process.cwd(),
          'NFT_Content',
          `nanobanana-generated-${Date.now()}.png`
        );

        // Ensure directory exists
        const dir = path.dirname(savePath);
        if (!fs.existsSync(dir)) {
          fs.mkdirSync(dir, { recursive: true });
        }

        // Download image from URL and save
        try {
          const imageResponse = await axios.default.get(result.imageUrl, { 
            responseType: 'arraybuffer',
            timeout: 60000,
          });
          fs.writeFileSync(savePath, imageResponse.data);
          
          return {
            success: true,
            imagePath: savePath,
            imageUrl: result.imageUrl,
            message: `Image generated using Nano Banana and saved to ${savePath}. You can now use this image for NFT minting.`,
            prompt: args.prompt,
          };
        } catch (downloadError: any) {
          return {
            error: true,
            message: `Failed to download image: ${downloadError.message}`,
            imageUrl: result.imageUrl, // Still return URL in case user wants to use it directly
          };
        }
      }

      case 'ltx_generate_video': {
        // Determine if this is text-to-video or image-to-video
        const isImageToVideo = !!(args.imageUrl || args.imageBase64);
        
        if (!isImageToVideo && !args.prompt) {
          return {
            error: true,
            message: 'Prompt is required for text-to-video generation. For image-to-video, provide imageUrl or imageBase64.',
          };
        }

        if (isImageToVideo && !args.imageUrl && !args.imageBase64) {
          return {
            error: true,
            message: 'Either imageUrl or imageBase64 is required for image-to-video generation',
          };
        }

        // Generate video using LTX.io
        let result;
        if (isImageToVideo) {
          result = await oasisClient.generateVideoFromImageWithLTX({
            imageUrl: args.imageUrl,
            imageBase64: args.imageBase64,
            prompt: args.prompt, // Optional prompt to guide motion
            model: args.model || 'ltx-2-fast',
            duration: args.duration || 5,
            resolution: args.resolution || '1920x1080',
            aspectRatio: args.aspectRatio || '16:9',
            fps: args.fps || 24,
          });
        } else {
          result = await oasisClient.generateVideoWithLTX({
            prompt: args.prompt,
            model: args.model || 'ltx-2-fast',
            duration: args.duration || 5,
            resolution: args.resolution || '1920x1080',
            aspectRatio: args.aspectRatio || '16:9',
            fps: args.fps || 24,
          });
        }

        if (result.error) {
          return {
            error: true,
            message: `Video generation failed: ${result.error}`,
          };
        }

        if (!result.videoBase64) {
          return {
            error: true,
            message: 'Video generated but no video data returned',
          };
        }

        // Save video to file
        const savePath = args.savePath || path.join(
          process.cwd(),
          'NFT_Content',
          `generated-video-${Date.now()}.mp4`
        );

        // Ensure directory exists
        const dir = path.dirname(savePath);
        if (!fs.existsSync(dir)) {
          fs.mkdirSync(dir, { recursive: true });
        }

        // Decode base64 and save video
        try {
          const videoBuffer = Buffer.from(result.videoBase64, 'base64');
          fs.writeFileSync(savePath, videoBuffer);
          
          return {
            success: true,
            videoPath: savePath,
            message: `Video generated and saved to ${savePath}. Duration: ${args.duration || 5}s, Resolution: ${args.resolution || '1920x1080'}, Model: ${args.model || 'ltx-2-fast'}`,
            prompt: args.prompt,
            type: isImageToVideo ? 'image-to-video' : 'text-to-video',
            duration: args.duration || 5,
            resolution: args.resolution || '1920x1080',
            model: args.model || 'ltx-2-fast',
          };
        } catch (saveError: any) {
          return {
            error: true,
            message: `Failed to save video: ${saveError.message}`,
            videoBase64: result.videoBase64, // Still return base64 in case user wants to use it directly
          };
        }
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

