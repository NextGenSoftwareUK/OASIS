import axios, { AxiosInstance } from 'axios';
import https from 'https';
import fs from 'fs';
import FormData from 'form-data';
import { config } from '../config.js';

// Import axios for Banana.dev (separate instance)
const bananaAxios = axios.create();

export class OASISClient {
  private client: AxiosInstance;
  private authToken: string | null = null;

  constructor() {
    // Configure HTTPS agent for self-signed certificates
    const httpsAgent = new https.Agent({
      rejectUnauthorized: false
    });

    this.client = axios.create({
      baseURL: config.oasisApiUrl,
      headers: {
        'Content-Type': 'application/json',
        ...(config.oasisApiKey && {
          Authorization: `Bearer ${config.oasisApiKey}`,
        }),
      },
      timeout: 60000, // Increased timeout
      httpsAgent: httpsAgent,
      maxRedirects: 0, // Don't follow redirects - use HTTPS directly
      validateStatus: (status) => status < 500, // Accept all status codes < 500
    });

    // Use request interceptor to ensure token is always included
    this.client.interceptors.request.use((config) => {
      if (this.authToken) {
        config.headers = config.headers || {};
        config.headers['Authorization'] = `Bearer ${this.authToken}`;
      }
      return config;
    });
  }

  /**
   * Set authentication token for subsequent requests
   */
  setAuthToken(token: string) {
    this.authToken = token;
    this.client.defaults.headers.common['Authorization'] = `Bearer ${token}`;
  }

  /**
   * Get avatar by ID
   */
  async getAvatar(avatarId: string) {
    const response = await this.client.get(`/api/avatar/get-by-id/${avatarId}`);
    return response.data;
  }

  /**
   * Get avatar by username
   */
  async getAvatarByUsername(username: string) {
    const response = await this.client.get(`/api/avatar/username/${username}`);
    return response.data;
  }

  /**
   * Get avatar by email
   */
  async getAvatarByEmail(email: string) {
    const response = await this.client.get(`/api/avatar/email/${email}`);
    return response.data;
  }

  /**
   * Get karma for avatar
   */
  async getKarma(avatarId: string) {
    const response = await this.client.get(`/api/karma/${avatarId}`);
    return response.data;
  }

  /**
   * Get NFTs for avatar
   */
  async getNFTs(avatarId: string) {
    const response = await this.client.get(`/api/nft/load-all-nfts-for_avatar/${avatarId}`);
    return response.data;
  }

  /**
   * Get NFT by ID
   */
  async getNFT(nftId: string) {
    const response = await this.client.get(`/api/nft/load-nft/${nftId}`);
    return response.data;
  }

  /**
   * Mint NFT
   * Requires authentication. Minimum required: JSONMetaDataURL, Symbol
   * Defaults: OnChainProvider=SolanaOASIS, OffChainProvider=MongoDBOASIS, NFTOffChainMetaType=OASIS, NFTStandardType=SPL
   */
  async mintNFT(request: {
    JSONMetaDataURL: string;
    Symbol: string;
    Title?: string;
    Description?: string;
    ImageUrl?: string;
    ThumbnailUrl?: string;
    Price?: number;
    NumberToMint?: number;
    OnChainProvider?: string;
    OffChainProvider?: string;
    NFTOffChainMetaType?: string;
    NFTStandardType?: string;
    StoreNFTMetaDataOnChain?: boolean;
    MetaData?: Record<string, any>;
    SendToAddressAfterMinting?: string;
    SendToAvatarAfterMintingId?: string;
    SendToAvatarAfterMintingUsername?: string;
    SendToAvatarAfterMintingEmail?: string;
    WaitTillNFTMinted?: boolean;
    WaitForNFTToMintInSeconds?: number;
    AttemptToMintEveryXSeconds?: number;
  }) {
    // Set defaults for required provider fields if not provided
    const mintRequest = {
      JSONMetaDataURL: request.JSONMetaDataURL,
      Symbol: request.Symbol,
      Title: request.Title || 'Untitled NFT',
      Description: request.Description || '',
      ImageUrl: request.ImageUrl || request.JSONMetaDataURL,
      ThumbnailUrl: request.ThumbnailUrl,
      Price: request.Price || 0,
      NumberToMint: request.NumberToMint || 1,
      OnChainProvider: request.OnChainProvider || 'SolanaOASIS',
      OffChainProvider: request.OffChainProvider || 'MongoDBOASIS',
      NFTOffChainMetaType: request.NFTOffChainMetaType || 'OASIS',
      NFTStandardType: request.NFTStandardType || 'SPL',
      StoreNFTMetaDataOnChain: request.StoreNFTMetaDataOnChain || false,
      MetaData: request.MetaData,
      SendToAddressAfterMinting: request.SendToAddressAfterMinting,
      SendToAvatarAfterMintingId: request.SendToAvatarAfterMintingId,
      SendToAvatarAfterMintingUsername: request.SendToAvatarAfterMintingUsername,
      SendToAvatarAfterMintingEmail: request.SendToAvatarAfterMintingEmail,
      WaitTillNFTMinted: request.WaitTillNFTMinted !== undefined ? request.WaitTillNFTMinted : true,
      WaitForNFTToMintInSeconds: request.WaitForNFTToMintInSeconds || 60,
      AttemptToMintEveryXSeconds: request.AttemptToMintEveryXSeconds || 1,
    };
    const response = await this.client.post('/api/nft/mint-nft', mintRequest);
    return response.data;
  }

  /**
   * Upload a file to Pinata/IPFS
   * @param filePath - Path to the file to upload
   * @param provider - Storage provider (default: "PinataOASIS")
   * @returns IPFS URL of the uploaded file
   */
  async uploadFile(filePath: string, provider: string = 'PinataOASIS'): Promise<any> {
    if (!fs.existsSync(filePath)) {
      throw new Error(`File not found: ${filePath}`);
    }

    const formData = new FormData();
    const fileStream = fs.createReadStream(filePath);
    const fileName = filePath.split('/').pop() || filePath.split('\\').pop() || 'file';
    
    formData.append('file', fileStream, fileName);
    formData.append('provider', provider);

    // Use a separate axios instance for multipart/form-data
    const httpsAgent = new https.Agent({
      rejectUnauthorized: false
    });

    const uploadClient = axios.create({
      baseURL: config.oasisApiUrl,
      headers: {
        ...formData.getHeaders(),
        ...(this.authToken && {
          Authorization: `Bearer ${this.authToken}`,
        }),
      },
      timeout: 120000, // 2 minutes for large files
      httpsAgent: httpsAgent,
      maxRedirects: 0,
      validateStatus: (status) => status < 500,
    });

    const response = await uploadClient.post('/api/files/upload', formData);
    return response.data;
  }

  /**
   * Get wallet for avatar
   */
  async getWallet(avatarId: string) {
    const response = await this.client.get(`/api/wallet/${avatarId}`);
    return response.data;
  }

  /**
   * Get holon by ID
   */
  async getHolon(holonId: string) {
    const response = await this.client.get(`/api/data/load-holon/${holonId}`);
    return response.data;
  }

  /**
   * Register/Create new avatar
   */
  async registerAvatar(request: {
    username: string;
    email: string;
    password: string;
    firstName?: string;
    lastName?: string;
    title?: string;
    avatarType?: string;
    acceptTerms?: boolean;
    confirmPassword?: string;
  }) {
    const response = await this.client.post('/api/avatar/register', request);
    return response.data;
  }

  /**
   * Authenticate avatar (login)
   */
  async authenticateAvatar(username: string, password: string) {
    const response = await this.client.post('/api/avatar/authenticate', {
      username,
      password,
    });
    return response.data;
  }

  /**
   * Save holon
   */
  async saveHolon(holon: any) {
    const response = await this.client.post('/api/data/save-holon', holon);
    return response.data;
  }

  /**
   * Update avatar
   */
  async updateAvatar(avatarId: string, updates: any) {
    const response = await this.client.post(`/api/avatar/update-by-id/${avatarId}`, updates);
    return response.data;
  }

  /**
   * Create wallet for avatar
   */
  async createWallet(avatarId: string, walletType?: string) {
    const response = await this.client.post(`/api/wallet/${avatarId}`, {
      walletType: walletType || 'Ethereum',
    });
    return response.data;
  }

  /**
   * Send transaction via OASIS API
   * Uses the send_token endpoint which requires wallet addresses and provider types
   */
  async sendTransaction(request: {
    fromAvatarId?: string;
    fromWalletAddress?: string;
    toAvatarId?: string;
    toAddress?: string;
    toWalletAddress?: string;
    amount: number;
    fromProvider?: number; // ProviderType enum (e.g., 3 for SolanaOASIS)
    toProvider?: number; // ProviderType enum
    memoText?: string;
  }) {
    // Build request according to IWalletTransactionRequest interface
    const walletRequest: any = {
      Amount: request.amount,
    };

    // Set sender (prefer wallet address, fallback to avatar ID)
    if (request.fromWalletAddress) {
      walletRequest.FromWalletAddress = request.fromWalletAddress;
    } else if (request.fromAvatarId) {
      walletRequest.FromAvatarId = request.fromAvatarId;
    }

    // Set recipient (prefer wallet address, fallback to avatar ID)
    if (request.toWalletAddress || request.toAddress) {
      walletRequest.ToWalletAddress = request.toWalletAddress || request.toAddress;
    } else if (request.toAvatarId) {
      walletRequest.ToAvatarId = request.toAvatarId;
    }

    // Set provider types (default to SolanaOASIS = 3 if not specified)
    walletRequest.FromProvider = request.fromProvider ?? 3; // SolanaOASIS
    walletRequest.ToProvider = request.toProvider ?? 3; // SolanaOASIS

    if (request.memoText) {
      walletRequest.MemoText = request.memoText;
    }

    // Use the correct endpoint: send_token (not send-transaction)
    const response = await this.client.post('/api/wallet/send_token', walletRequest);
    return response.data;
  }

  /**
   * Get all avatars
   * Note: Requires Wizard (Admin) authentication
   */
  async getAllAvatars() {
    const response = await this.client.get('/api/avatar/get-all-avatars');
    return response.data;
  }

  /**
   * Get avatar detail by ID
   */
  async getAvatarDetailById(avatarId: string) {
    const response = await this.client.get(`/api/avatar/get-avatar-detail-by-id/${avatarId}`);
    return response.data;
  }

  /**
   * Get avatar detail by username
   */
  async getAvatarDetailByUsername(username: string) {
    const response = await this.client.get(`/api/avatar/get-avatar-detail-by-username/${username}`);
    return response.data;
  }

  /**
   * Get avatar detail by email
   */
  async getAvatarDetailByEmail(email: string) {
    const response = await this.client.get(`/api/avatar/get-avatar-detail-by-email/${email}`);
    return response.data;
  }

  /**
   * Get all avatar details (Wizard only)
   */
  async getAllAvatarDetails() {
    const response = await this.client.get('/api/avatar/get-all-avatar-details');
    return response.data;
  }

  /**
   * Get NFT by hash
   */
  async getNFTByHash(hash: string) {
    const response = await this.client.get(`/api/nft/load-nft-by-hash/${hash}`);
    return response.data;
  }

  /**
   * Get GeoNFTs for avatar
   */
  async getGeoNFTsForAvatar(avatarId: string) {
    const response = await this.client.get(`/api/nft/load-all-geo-nfts-for-avatar/${avatarId}`);
    return response.data;
  }

  /**
   * Place GeoNFT at real-world coordinates
   * Converts lat/long from degrees to micro-degrees automatically
   */
  async placeGeoNFT(request: {
    originalOASISNFTId: string;
    latitude: number; // In degrees (e.g., 51.5074)
    longitude: number; // In degrees (e.g., -0.1278)
    allowOtherPlayersToAlsoCollect?: boolean;
    permSpawn?: boolean;
    globalSpawnQuantity?: number;
    playerSpawnQuantity?: number;
    respawnDurationInSeconds?: number;
    geoNFTMetaDataProvider?: string;
    originalOASISNFTOffChainProvider?: string;
  }) {
    // Convert degrees to micro-degrees (multiply by 1,000,000)
    const latMicroDegrees = Math.round(request.latitude * 1000000);
    const longMicroDegrees = Math.round(request.longitude * 1000000);

    const requestBody = {
      originalOASISNFTId: request.originalOASISNFTId,
      lat: latMicroDegrees,
      long: longMicroDegrees,
      allowOtherPlayersToAlsoCollect: request.allowOtherPlayersToAlsoCollect ?? true,
      permSpawn: request.permSpawn ?? false,
      globalSpawnQuantity: request.globalSpawnQuantity ?? 1,
      playerSpawnQuantity: request.playerSpawnQuantity ?? 1,
      respawnDurationInSeconds: request.respawnDurationInSeconds ?? 0,
      geoNFTMetaDataProvider: request.geoNFTMetaDataProvider || 'MongoDBOASIS',
      originalOASISNFTOffChainProvider: request.originalOASISNFTOffChainProvider || 'MongoDBOASIS',
    };

    const response = await this.client.post('/api/nft/place-geo-nft', requestBody);
    return response.data;
  }

  /**
   * Send NFT
   */
  async sendNFT(request: {
    FromWalletAddress: string;
    ToWalletAddress: string;
    FromProvider: string;
    ToProvider: string;
    Amount?: number;
    MemoText?: string;
    WaitTillNFTSent?: boolean;
    WaitForNFTToSendInSeconds?: number;
    AttemptToSendEveryXSeconds?: number;
  }) {
    const response = await this.client.post('/api/nft/send-nft', request);
    return response.data;
  }

  /**
   * Get provider wallets for avatar
   */
  async getProviderWalletsForAvatar(avatarId: string, providerType?: string) {
    const url = providerType
      ? `/api/wallet/avatar/${avatarId}/wallets?providerType=${providerType}`
      : `/api/wallet/avatar/${avatarId}/wallets`;
    const response = await this.client.get(url);
    return response.data;
  }

  /**
   * Get wallet transaction by hash
   */
  async getTransactionByHash(transactionHash: string, blockchain?: string, providerType?: string) {
    const params = new URLSearchParams();
    if (blockchain) params.append('blockchain', blockchain);
    if (providerType) params.append('providerType', providerType);
    const query = params.toString();
    const url = `/api/wallet/transaction/${transactionHash}${query ? `?${query}` : ''}`;
    const response = await this.client.get(url);
    return response.data;
  }

  /**
   * Get wallet portfolio value
   */
  async getPortfolioValue(avatarId: string) {
    const response = await this.client.get(`/api/wallet/avatar/${avatarId}/portfolio/value`);
    return response.data;
  }

  /**
   * Search holons - uses POST to /api/data/search-holons (via DataController)
   * Note: This actually uses the general search endpoint with holon-specific params
   */
  async searchHolons(searchParams: {
    searchQuery?: string;
    holonType?: string;
    parentId?: string;
    limit?: number;
    offset?: number;
  }) {
    const oasisSearchParams = {
      AvatarId: searchParams.parentId || '00000000-0000-0000-0000-000000000000',
      SearchOnlyForCurrentAvatar: false,
      SearchGroups: searchParams.searchQuery ? [{
        SearchQuery: searchParams.searchQuery,
        HolonType: searchParams.holonType || 'All',
        SearchHolons: true
      }] : []
    };
    const response = await this.client.post('/api/data/search-holons', oasisSearchParams);
    return response.data;
  }

  /**
   * Load holons for parent
   */
  async loadHolonsForParent(parentId: string) {
    const response = await this.client.get(`/api/data/load-holons-for-parent/${parentId}`);
    return response.data;
  }

  /**
   * Delete holon
   */
  async deleteHolon(holonId: string) {
    const response = await this.client.delete(`/api/data/delete-holon/${holonId}`);
    return response.data;
  }

  /**
   * Get karma stats
   */
  async getKarmaStats(avatarId: string) {
    const response = await this.client.get(`/api/karma/get-karma-stats/${avatarId}`);
    return response.data;
  }

  /**
   * Get karma history
   */
  async getKarmaHistory(avatarId: string, limit: number = 50, offset: number = 0) {
    const response = await this.client.get(`/api/karma/get-karma-history/${avatarId}?limit=${limit}&offset=${offset}`);
    return response.data;
  }

  /**
   * Add karma to avatar
   */
  async addKarmaToAvatar(avatarId: string, request: {
    KarmaType: string;
    karmaSourceType: string;
    KaramSourceTitle?: string;
    KarmaSourceDesc?: string;
  }) {
    const response = await this.client.post(`/api/karma/add-karma-to-avatar/${avatarId}`, request);
    return response.data;
  }

  /**
   * Remove karma from avatar
   */
  async removeKarmaFromAvatar(avatarId: string, request: {
    KarmaType: string;
    karmaSourceType: string;
    KaramSourceTitle?: string;
    KarmaSourceDesc?: string;
  }) {
    const response = await this.client.post(`/api/karma/remove-karma-from-avatar/${avatarId}`, request);
    return response.data;
  }

  /**
   * Search avatars - now uses POST to /api/search/search-avatars (fixed endpoint)
   */
  async searchAvatars(searchParams: {
    searchQuery?: string;
    avatarType?: string;
    limit?: number;
    offset?: number;
  }) {
    const oasisSearchParams = {
      AvatarId: '00000000-0000-0000-0000-000000000000',
      SearchOnlyForCurrentAvatar: false,
      SearchGroups: searchParams.searchQuery ? [{
        SearchQuery: searchParams.searchQuery,
        HolonType: 'Avatar',
        SearchAvatars: true
      }] : []
    };
    const response = await this.client.post('/api/search/search-avatars', oasisSearchParams);
    return response.data;
  }

  /**
   * Search NFTs - now uses POST to /api/search/search-nfts (fixed endpoint)
   */
  async searchNFTs(searchParams: {
    searchQuery?: string;
    avatarId?: string;
    limit?: number;
    offset?: number;
  }) {
    const oasisSearchParams = {
      AvatarId: searchParams.avatarId || '00000000-0000-0000-0000-000000000000',
      SearchOnlyForCurrentAvatar: !!searchParams.avatarId,
      SearchGroups: searchParams.searchQuery ? [{
        SearchQuery: searchParams.searchQuery,
        HolonType: 'NFT',
        SearchHolons: true
      }] : []
    };
    const response = await this.client.post('/api/search/search-nfts', oasisSearchParams);
    return response.data;
  }

  /**
   * Get all avatar names
   */
  async getAllAvatarNames(includeUsernames: boolean = true, includeIds: boolean = true) {
    const response = await this.client.get(`/api/avatar/get-all-avatar-names/${includeUsernames}/${includeIds}`);
    return response.data;
  }

  /**
   * Get avatar portrait by ID
   */
  async getAvatarPortrait(avatarId: string) {
    const response = await this.client.get(`/api/avatar/get-avatar-portrait/${avatarId}`);
    return response.data;
  }

  /**
   * Get avatar portrait by username
   */
  async getAvatarPortraitByUsername(username: string) {
    const response = await this.client.get(`/api/avatar/get-avatar-portrait-by-username/${username}`);
    return response.data;
  }

  /**
   * Get avatar portrait by email
   */
  async getAvatarPortraitByEmail(email: string) {
    const response = await this.client.get(`/api/avatar/get-avatar-portrait-by-email/${email}`);
    return response.data;
  }

  /**
   * Get NFTs for mint wallet address
   */
  async getNFTsForMintAddress(mintWalletAddress: string) {
    const response = await this.client.get(`/api/nft/load-all-nfts-for-mint-wallet-address/${mintWalletAddress}`);
    return response.data;
  }

  /**
   * Get GeoNFTs for mint wallet address
   */
  async getGeoNFTsForMintAddress(mintWalletAddress: string) {
    const response = await this.client.get(`/api/nft/load-all-geo-nfts-for-mint-wallet-address/${mintWalletAddress}`);
    return response.data;
  }

  /**
   * Get all NFTs (Wizard only)
   */
  async getAllNFTs() {
    const response = await this.client.get('/api/nft/load-all-nfts');
    return response.data;
  }

  /**
   * Get all GeoNFTs (Wizard only)
   */
  async getAllGeoNFTs() {
    const response = await this.client.get('/api/nft/load-all-geo-nfts');
    return response.data;
  }

  /**
   * Get token metadata by Solana mint address (e.g. memecoin/SPL token from Solscan).
   * Returns name, symbol, uri, image, description. Use to convert memecoin → NFT.
   * Endpoint is AllowAnonymous so no auth required.
   */
  async getTokenMetadataByMint(mint: string) {
    const response = await this.client.get('/api/nft/metadata-by-mint', { params: { mint } });
    return response.data;
  }

  /**
   * Get provider wallets for avatar by username
   */
  async getProviderWalletsForAvatarByUsername(username: string, providerType?: string) {
    const url = providerType
      ? `/api/wallet/avatar/username/${username}/wallets?providerType=${providerType}`
      : `/api/wallet/avatar/username/${username}/wallets`;
    const response = await this.client.get(url);
    return response.data;
  }

  /**
   * Get provider wallets for avatar by email
   */
  async getProviderWalletsForAvatarByEmail(email: string, providerType?: string) {
    const url = providerType
      ? `/api/wallet/avatar/email/${email}/wallets?providerType=${providerType}`
      : `/api/wallet/avatar/email/${email}/wallets`;
    const response = await this.client.get(url);
    return response.data;
  }

  /**
   * Get default wallet for avatar
   */
  async getDefaultWallet(avatarId: string, providerType: string) {
    const response = await this.client.get(`/api/wallet/avatar/${avatarId}/default-wallet?providerType=${providerType}`);
    return response.data;
  }

  /**
   * Set default wallet for avatar
   */
  async setDefaultWallet(avatarId: string, walletId: string, providerType: string) {
    const response = await this.client.post(`/api/wallet/avatar/${avatarId}/default-wallet/${walletId}?providerType=${providerType}`);
    return response.data;
  }

  /**
   * Get wallets by chain
   */
  async getWalletsByChain(avatarId: string, chain: string) {
    const response = await this.client.get(`/api/wallet/avatar/${avatarId}/wallets/chain/${chain}`);
    return response.data;
  }

  /**
   * Get wallet analytics
   */
  async getWalletAnalytics(avatarId: string, walletId: string) {
    const response = await this.client.get(`/api/wallet/avatar/${avatarId}/wallet/${walletId}/analytics`);
    return response.data;
  }

  /**
   * Get wallet tokens
   */
  async getWalletTokens(avatarId: string, walletId: string) {
    const response = await this.client.get(`/api/wallet/avatar/${avatarId}/wallet/${walletId}/tokens`);
    return response.data;
  }

  /**
   * Get supported chains
   */
  async getSupportedChains() {
    const response = await this.client.get('/api/wallet/supported-chains');
    return response.data;
  }

  /**
   * Import wallet using private key
   */
  async importWalletPrivateKey(avatarId: string, privateKey: string, providerType: string) {
    const response = await this.client.post(`/api/wallet/avatar/${avatarId}/import/private-key?key=${encodeURIComponent(privateKey)}&providerToImportTo=${providerType}`);
    return response.data;
  }

  /**
   * Import wallet using public key
   */
  async importWalletPublicKey(avatarId: string, publicKey: string, providerType: string) {
    const response = await this.client.post(`/api/wallet/avatar/${avatarId}/import/public-key?key=${encodeURIComponent(publicKey)}&providerToImportTo=${providerType}`);
    return response.data;
  }

  /**
   * Create wallet (full version with options)
   */
  async createWalletFull(avatarId: string, request: {
    Name?: string;
    Description?: string;
    WalletProviderType: string | number;
    GenerateKeyPair?: boolean;
    IsDefaultWallet?: boolean;
  }, providerType?: string) {
    const url = providerType
      ? `/api/wallet/avatar/${avatarId}/create-wallet?providerTypeToLoadSave=${providerType}`
      : `/api/wallet/avatar/${avatarId}/create-wallet`;
    
    // Convert string provider types to numeric enum values if needed
    const walletRequest = { ...request };
    if (typeof walletRequest.WalletProviderType === 'string') {
      const providerTypeMap: Record<string, number> = {
        'SolanaOASIS': 3,
        'EthereumOASIS': 12,
        'ArbitrumOASIS': 9,
        'PolygonOASIS': 14,
        'Default': 2,
      };
      if (providerTypeMap[walletRequest.WalletProviderType]) {
        walletRequest.WalletProviderType = providerTypeMap[walletRequest.WalletProviderType];
      }
    }
    
    const response = await this.client.post(url, walletRequest);
    return response.data;
  }

  /**
   * Load all holons
   */
  async loadAllHolons() {
    const response = await this.client.get('/api/data/load-all-holons');
    return response.data;
  }

  /**
   * Update holon
   */
  async updateHolon(holonId: string, holon: any) {
    const response = await this.client.put(`/api/data/update-holon/${holonId}`, holon);
    return response.data;
  }

  /**
   * Get karma akashic records for avatar
   */
  async getKarmaAkashicRecords(avatarId: string) {
    const response = await this.client.get(`/api/karma/get-karma-akashic-records-for-avatar/${avatarId}`);
    return response.data;
  }

  /**
   * Get positive karma weighting
   */
  async getPositiveKarmaWeighting(karmaType: string) {
    const response = await this.client.get(`/api/karma/get-positive-karma-weighting/${karmaType}`);
    return response.data;
  }

  /**
   * Get negative karma weighting
   */
  async getNegativeKarmaWeighting(karmaType: string) {
    const response = await this.client.get(`/api/karma/get-negative-karma-weighting/${karmaType}`);
    return response.data;
  }

  /**
   * Vote for positive karma weighting
   */
  async voteForPositiveKarmaWeighting(karmaType: string, weighting: number) {
    const response = await this.client.post(`/api/karma/vote-for-positive-karma-weighting/${karmaType}/${weighting}`);
    return response.data;
  }

  /**
   * Vote for negative karma weighting
   */
  async voteForNegativeKarmaWeighting(karmaType: string, weighting: number) {
    const response = await this.client.post(`/api/karma/vote-for-negative-karma-weighting/${karmaType}/${weighting}`);
    return response.data;
  }

  /**
   * Basic search - now uses GET with query parameters (fixed endpoint)
   */
  async basicSearch(searchQuery: string, limit?: number, offset?: number) {
    const params = new URLSearchParams();
    params.append('searchQuery', searchQuery);
    if (limit) params.append('limit', limit.toString());
    if (offset) params.append('offset', offset.toString());
    const response = await this.client.get(`/api/search/basic?${params.toString()}`);
    return response.data;
  }

  /**
   * Advanced search - now uses POST with SearchParams body (fixed endpoint)
   */
  async advancedSearch(searchParams: {
    searchQuery?: string;
    entityType?: string;
    filters?: any;
    limit?: number;
    offset?: number;
  }) {
    const oasisSearchParams = {
      AvatarId: '00000000-0000-0000-0000-000000000000',
      SearchOnlyForCurrentAvatar: false,
      SearchGroups: [] as any[]
    };
    const response = await this.client.post('/api/search/advanced', oasisSearchParams);
    return response.data;
  }

  /**
   * Search files - now uses POST to /api/search/search-files (fixed endpoint)
   */
  async searchFiles(searchParams: {
    searchQuery?: string;
    avatarId?: string;
    fileType?: string;
    limit?: number;
    offset?: number;
  }) {
    const oasisSearchParams = {
      AvatarId: searchParams.avatarId || '00000000-0000-0000-0000-000000000000',
      SearchOnlyForCurrentAvatar: !!searchParams.avatarId,
      SearchGroups: searchParams.searchQuery ? [{
        SearchQuery: searchParams.searchQuery,
        HolonType: 'File',
        SearchHolons: true
      }] : []
    };
    const response = await this.client.post('/api/search/search-files', oasisSearchParams);
    return response.data;
  }

  /**
   * Health check
   */
  async healthCheck() {
    try {
      const response = await this.client.get('/api/health');
      return { status: 'healthy', data: response.data };
    } catch (error: any) {
      // Provide more detailed error information
      const errorDetails = {
        message: error.message,
        status: error.response?.status,
        statusText: error.response?.statusText,
        data: error.response?.data,
        headers: error.response?.headers,
      };
      return { status: 'unhealthy', error: errorDetails };
    }
  }

  // ============================================
  // A2A Protocol & SERV Infrastructure Methods
  // ============================================

  /**
   * Get agent card by agent ID
   */
  async getAgentCard(agentId: string) {
    const response = await this.client.get(`/api/a2a/agent-card/${agentId}`);
    return response.data;
  }

  /**
   * Get all agents
   */
  async getAllAgents() {
    const response = await this.client.get('/api/a2a/agents');
    return response.data;
  }

  /**
   * Get agents by service name
   */
  async getAgentsByService(serviceName: string) {
    const response = await this.client.get(`/api/a2a/agents/by-service/${serviceName}`);
    return response.data;
  }

  /**
   * Get agents owned by authenticated user
   */
  async getMyAgents() {
    const response = await this.client.get('/api/a2a/agents/my-agents');
    return response.data;
  }

  /**
   * Register agent capabilities
   */
  async registerAgentCapabilities(request: {
    services: string[];
    skills?: string[];
    pricing?: Record<string, number>;
    status?: string | number;
    max_concurrent_tasks?: number;
    description?: string;
  }) {
    const response = await this.client.post('/api/a2a/agent/capabilities', request);
    return response.data;
  }

  /**
   * Register agent as SERV service
   */
  async registerAgentAsSERVService() {
    const response = await this.client.post('/api/a2a/agent/register-service');
    return response.data;
  }

  /**
   * Discover agents via SERV infrastructure
   */
  async discoverAgentsViaSERV(serviceName?: string) {
    const url = serviceName
      ? `/api/a2a/agents/discover-serv?service=${encodeURIComponent(serviceName)}`
      : '/api/a2a/agents/discover-serv';
    const response = await this.client.get(url);
    return response.data;
  }

  /**
   * Send A2A JSON-RPC 2.0 request
   */
  async sendA2AJsonRpcRequest(request: {
    jsonrpc: string;
    method: string;
    params?: any;
    id: string | number;
  }) {
    const response = await this.client.post('/api/a2a/jsonrpc', request);
    return response.data;
  }

  /**
   * Get pending A2A messages for authenticated agent
   */
  async getPendingA2AMessages() {
    const response = await this.client.get('/api/a2a/messages');
    return response.data;
  }

  /**
   * Mark A2A message as processed
   */
  async markA2AMessageProcessed(messageId: string) {
    const response = await this.client.post(`/api/a2a/messages/${messageId}/process`);
    return response.data;
  }

  /**
   * Register OpenSERV agent as A2A agent
   */
  async registerOpenServAgent(request: {
    openServAgentId: string;
    openServEndpoint: string;
    capabilities: string[];
    apiKey?: string;
    username?: string;
    email?: string;
    password?: string;
  }) {
    const response = await this.client.post('/api/a2a/openserv/register', request);
    return response.data;
  }

  /**
   * Execute AI workflow via A2A Protocol
   */
  async executeAIWorkflow(request: {
    toAgentId: string;
    workflowRequest: string;
    workflowParameters?: Record<string, any>;
  }) {
    const response = await this.client.post('/api/a2a/workflow/execute', request);
    return response.data;
  }

  /**
   * Generate image using Glif.app API (workflow-based, easiest option)
   * Supports both text prompts and reference images
   */
  async generateImageWithGlif(request: {
    workflowId?: string;
    prompt: string;
    referenceImageUrl?: string;
    referenceImagePath?: string;
  }): Promise<{ imageUrl?: string; error?: string }> {
    try {
      if (!config.glifApiToken) {
        return { error: 'Glif.app API token not configured. Get your free token at https://glif.app/settings/api-tokens' };
      }

      // Use a default image generation workflow if none provided
      // You can find public workflows at glif.app or create your own
      // Default: "Flux 2 Pro" - better for accurate/realistic image generation
      // Previous default (clgh1vxtu0011mo081dplq3xs) was a "Heavy Metal Covers" workflow
      const workflowId = request.workflowId || 'cmigcvfwm0000k004u9shifki'; // Flux 2 Pro - accurate image generation
      
      // Build inputs array - can include both text and image inputs
      const inputs: any[] = [];
      
      // If reference image is provided, upload it first and include in inputs
      let imageInputUrl: string | undefined;
      if (request.referenceImagePath) {
        // Upload reference image to Pinata/IPFS first
        try {
          const uploadResult = await this.uploadFile(request.referenceImagePath);
          if (uploadResult && !uploadResult.isError) {
            imageInputUrl = uploadResult.result;
          }
        } catch (uploadError: any) {
          console.error('[MCP] Failed to upload reference image:', uploadError.message);
          // Continue without reference image
        }
      } else if (request.referenceImageUrl) {
        imageInputUrl = request.referenceImageUrl;
      }

      // Build inputs - Glif workflows can accept named inputs
      // For workflows that support images, we'll use named inputs
      const requestBody: any = {
        id: workflowId,
      };

      if (imageInputUrl) {
        // Use named inputs for workflows that support image inputs
        // Flux 2 Pro uses "input1" for text and "image1" for reference images
        requestBody.inputs = {
          input1: request.prompt,
          image1: imageInputUrl,
        };
      } else {
        // Text-only input - Flux 2 Pro uses "input1" as the main text input
        requestBody.inputs = {
          input1: request.prompt,
        };
      }
      
      const response = await bananaAxios.post(
        config.glifApiUrl,
        requestBody,
        {
          headers: {
            'Authorization': `Bearer ${config.glifApiToken}`,
            'Content-Type': 'application/json',
          },
          timeout: 120000, // 2 minutes
        }
      );

      if (response.data.error) {
        return { error: response.data.error };
      }

      if (response.data.output) {
        return { imageUrl: response.data.output };
      }

      return { error: 'No image URL in response' };
    } catch (error: any) {
      console.error('[MCP] Glif.app API error:', error.message);
      return { error: error.message || 'Failed to generate image' };
    }
  }

  /**
   * Single workflow: authenticate → generate image with Glif → mint NFT.
   * Use this for one consistent way to trigger NFT creation.
   */
  async createNFTWithGlif(request: {
    username: string;
    password: string;
    imagePrompt: string;
    symbol: string;
    title?: string;
    description?: string;
    numberToMint?: number;
    price?: number;
    workflowId?: string;
  }): Promise<{
    success?: boolean;
    error?: string;
    auth?: { ok: boolean };
    imageUrl?: string;
    mintResult?: any;
  }> {
    try {
      // 1) Authenticate
      const authResponse = await this.authenticateAvatar(request.username, request.password);
      const jwt = (authResponse as any)?.result?.result?.jwtToken
        ?? (authResponse as any)?.result?.jwtToken
        ?? (authResponse as any)?.jwtToken;
      if (!jwt || typeof jwt !== 'string') {
        return {
          success: false,
          error: 'Authentication failed: no JWT in response',
          auth: { ok: false },
        };
      }
      this.setAuthToken(jwt);

      // 2) Generate image with Glif
      const glifResult = await this.generateImageWithGlif({
        prompt: request.imagePrompt,
        workflowId: request.workflowId,
      });
      if (glifResult.error || !glifResult.imageUrl) {
        return {
          success: false,
          error: glifResult.error || 'No image URL from Glif',
          auth: { ok: true },
        };
      }

      // 3) Mint NFT
      const mintResult = await this.mintNFT({
        Symbol: request.symbol,
        JSONMetaDataURL: 'https://jsonplaceholder.typicode.com/posts/1',
        Title: request.title || request.symbol,
        Description: request.description || '',
        ImageUrl: glifResult.imageUrl,
        NumberToMint: request.numberToMint ?? 1,
        Price: request.price ?? 0,
      });

      return {
        success: true,
        auth: { ok: true },
        imageUrl: glifResult.imageUrl,
        mintResult,
      };
    } catch (err: any) {
      return {
        success: false,
        error: err?.message || String(err),
      };
    }
  }

  /**
   * Generate image using Nano Banana API (Google Gemini-powered, supports reference images)
   */
  async generateImageWithNanoBanana(request: {
    prompt: string;
    referenceImageUrls?: string[];
    aspectRatio?: string;
    size?: string;
  }): Promise<{ imageUrl?: string; error?: string }> {
    try {
      if (!config.nanoBananaApiKey) {
        return { error: 'Nano Banana API key not configured. Please set NANO_BANANA_API_KEY environment variable.' };
      }

      // Use the Generate Image (Pro) endpoint for better quality
      // Based on docs.nanobananaapi.ai documentation
      const endpoint = `${config.nanoBananaApiUrl}/generate-pro`;
      
      const payload: any = {
        prompt: request.prompt,
        resolution: '2K', // Required: "1K", "2K", etc. Default to 2K for high quality
        aspectRatio: request.aspectRatio || '16:9', // Required: "16:9", "9:16", "1:1", etc.
      };

      // Add reference images if provided
      if (request.referenceImageUrls && request.referenceImageUrls.length > 0) {
        payload.imageUrls = request.referenceImageUrls.slice(0, 8); // Max 8 images
      }

      const response = await bananaAxios.post(
        endpoint,
        payload,
        {
          headers: {
            'Authorization': `Bearer ${config.nanoBananaApiKey}`,
            'Content-Type': 'application/json',
          },
          timeout: 180000, // 3 minutes for image generation
        }
      );

      if (response.data.code !== 200) {
        return { error: response.data.msg || 'API request failed' };
      }

      // Nano Banana Pro returns a taskId for async processing
      if (response.data.data && response.data.data.taskId) {
        const taskId = response.data.data.taskId;
        
        // Poll for task completion (max 3 minutes)
        const maxAttempts = 30;
        const pollInterval = 6000; // 6 seconds
        
        for (let attempt = 0; attempt < maxAttempts; attempt++) {
          await new Promise(resolve => setTimeout(resolve, pollInterval));
          
          try {
            const taskResponse = await bananaAxios.get(
              `https://api.nanobananaapi.ai/api/v1/common/get-task-details`,
              {
                params: { taskId },
                headers: {
                  'Authorization': `Bearer ${config.nanoBananaApiKey}`,
                },
                timeout: 10000,
              }
            );
            
            if (taskResponse.data.code === 200 && taskResponse.data.data) {
              const taskData = taskResponse.data.data;
              
              // Check if task is complete
              if (taskData.status === 'completed' || taskData.status === 'success') {
                // Extract image URL from various possible fields
                if (taskData.imageUrl) {
                  return { imageUrl: taskData.imageUrl };
                } else if (taskData.image_url) {
                  return { imageUrl: taskData.image_url };
                } else if (taskData.url) {
                  return { imageUrl: taskData.url };
                } else if (taskData.result && taskData.result.imageUrl) {
                  return { imageUrl: taskData.result.imageUrl };
                } else if (taskData.data && taskData.data.imageUrl) {
                  return { imageUrl: taskData.data.imageUrl };
                }
              } else if (taskData.status === 'failed' || taskData.status === 'error') {
                return { error: taskData.message || taskData.error || 'Image generation failed' };
              }
              // If status is 'processing' or 'pending', continue polling
            }
          } catch (pollError: any) {
            // Continue polling on errors (task might not be ready yet)
            console.error(`[MCP] Poll attempt ${attempt + 1} failed:`, pollError.message);
          }
        }
        
        return { error: 'Image generation timed out. Task ID: ' + taskId };
      }

      // Fallback: check for direct image URL in response
      if (response.data.imageUrl) {
        return { imageUrl: response.data.imageUrl };
      } else if (response.data.image_url) {
        return { imageUrl: response.data.image_url };
      } else if (response.data.url) {
        return { imageUrl: response.data.url };
      } else if (response.data.data && response.data.data.imageUrl) {
        return { imageUrl: response.data.data.imageUrl };
      }

      return { error: 'No taskId or image URL in response' };
    } catch (error: any) {
      console.error('[MCP] Nano Banana API error:', error.message);
      if (error.response?.data) {
        console.error('[MCP] Nano Banana API error details:', JSON.stringify(error.response.data, null, 2));
      }
      return { error: error.response?.data?.error || error.message || 'Failed to generate image' };
    }
  }

  /**
   * Generate image using Banana.dev API (Stable Diffusion or other models)
   */
  async generateImageWithBanana(request: {
    modelKey?: string;
    prompt: string;
    negativePrompt?: string;
    width?: number;
    height?: number;
    numOutputs?: number;
    guidanceScale?: number;
    numInferenceSteps?: number;
    seed?: number;
  }): Promise<{ imageUrl?: string; imageBase64?: string; error?: string }> {
    try {
      if (!config.bananaApiKey) {
        return { error: 'Banana.dev API key not configured. Please set BANANA_API_KEY environment variable.' };
      }

      // Use bananaAxios directly with config
      bananaAxios.defaults.baseURL = config.bananaApiUrl;
      bananaAxios.defaults.timeout = 120000;

      // Banana.dev API call
      // If modelKey is not provided, we'll need to use a default or discover available models
      const modelKey = request.modelKey || 'stable-diffusion-v1-5'; // Default model
      
      const response = await bananaAxios.post('/v1/run', {
        modelKey: modelKey,
        startOnly: false,
        callID: `mcp-${Date.now()}`,
        modelInputs: {
          prompt: request.prompt,
          negative_prompt: request.negativePrompt || '',
          width: request.width || 512,
          height: request.height || 512,
          num_outputs: request.numOutputs || 1,
          guidance_scale: request.guidanceScale || 7.5,
          num_inference_steps: request.numInferenceSteps || 50,
          seed: request.seed || -1,
        },
      }, {
        headers: {
          'X-Banana-API-Key': config.bananaApiKey,
        },
      });

      if (response.data && response.data.modelOutputs && response.data.modelOutputs.length > 0) {
        const output = response.data.modelOutputs[0];
        
        // Banana.dev returns base64 encoded images
        if (output.image_base64) {
          return {
            imageBase64: output.image_base64,
          };
        } else if (output.image) {
          return {
            imageUrl: output.image,
          };
        }
      }

      return { error: 'No image in response' };
    } catch (error: any) {
      console.error('[MCP] Banana.dev API error:', error.message);
      return { error: error.message || 'Failed to generate image' };
    }
  }

  /**
   * Generate video using LTX.io API (text-to-video)
   */
  async generateVideoWithLTX(request: {
    prompt: string;
    model?: 'ltx-2-fast' | 'ltx-2-pro';
    duration?: number; // seconds, up to 20
    resolution?: string; // e.g., '1920x1080', '3840x2160'
    aspectRatio?: string; // e.g., '16:9', '9:16', '1:1'
    fps?: number; // frames per second, up to 50
  }): Promise<{ videoUrl?: string; videoBase64?: string; error?: string }> {
    try {
      if (!config.ltxApiToken) {
        return { error: 'LTX.io API token not configured. Get your API key at https://ltx.io/model/api' };
      }

      const response = await axios.post(
        `${config.ltxApiUrl}/text-to-video`,
        {
          prompt: request.prompt,
          model: request.model || 'ltx-2-fast',
          duration: request.duration || 5,
          resolution: request.resolution || '1920x1080',
          aspect_ratio: request.aspectRatio || '16:9',
          fps: request.fps || 24,
        },
        {
          headers: {
            'Authorization': `Bearer ${config.ltxApiToken}`,
            'Content-Type': 'application/json',
          },
          timeout: 300000, // 5 minutes for video generation
          responseType: 'arraybuffer', // LTX API returns MP4 directly
        }
      );

      // LTX API returns MP4 file directly in response body
      if (response.data && response.data.length > 0) {
        const videoBase64 = Buffer.from(response.data).toString('base64');
        return {
          videoBase64: videoBase64,
        };
      }

      return { error: 'No video data in response' };
    } catch (error: any) {
      console.error('[MCP] LTX.io API error:', error.message);
      if (error.response) {
        console.error('[MCP] LTX.io API error response:', error.response.status, error.response.data);
      }
      return { error: error.message || 'Failed to generate video' };
    }
  }

  /**
   * Generate video from image using LTX.io API (image-to-video)
   */
  async generateVideoFromImageWithLTX(request: {
    imageUrl?: string;
    imageBase64?: string;
    prompt?: string; // Optional prompt to guide motion
    model?: 'ltx-2-fast' | 'ltx-2-pro';
    duration?: number;
    resolution?: string;
    aspectRatio?: string;
    fps?: number;
  }): Promise<{ videoUrl?: string; videoBase64?: string; error?: string }> {
    try {
      if (!config.ltxApiToken) {
        return { error: 'LTX.io API token not configured. Get your API key at https://ltx.io/model/api' };
      }

      if (!request.imageUrl && !request.imageBase64) {
        return { error: 'Either imageUrl or imageBase64 is required for image-to-video generation' };
      }

      const requestBody: any = {
        model: request.model || 'ltx-2-fast',
        duration: request.duration || 5,
        resolution: request.resolution || '1920x1080',
        aspect_ratio: request.aspectRatio || '16:9',
        fps: request.fps || 24,
      };

      if (request.imageUrl) {
        requestBody.image_uri = request.imageUrl;
      } else if (request.imageBase64) {
        requestBody.image_base64 = request.imageBase64;
      }

      if (request.prompt) {
        requestBody.prompt = request.prompt;
      }

      const response = await axios.post(
        `${config.ltxApiUrl}/image-to-video`,
        requestBody,
        {
          headers: {
            'Authorization': `Bearer ${config.ltxApiToken}`,
            'Content-Type': 'application/json',
          },
          timeout: 300000, // 5 minutes for video generation
          responseType: 'arraybuffer', // LTX API returns MP4 directly
        }
      );

      // LTX API returns MP4 file directly in response body
      if (response.data && response.data.length > 0) {
        const videoBase64 = Buffer.from(response.data).toString('base64');
        return {
          videoBase64: videoBase64,
        };
      }

      return { error: 'No video data in response' };
    } catch (error: any) {
      console.error('[MCP] LTX.io API error:', error.message);
      if (error.response) {
        console.error('[MCP] LTX.io API error response:', error.response.status, error.response.data);
      }
      return { error: error.message || 'Failed to generate video from image' };
    }
  }
}

