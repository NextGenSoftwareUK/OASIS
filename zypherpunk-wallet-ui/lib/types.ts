export interface Wallet {
  walletId: string;
  avatarId: string;
  privateKey?: string;
  publicKey: string;
  walletAddress: string;
  secretRecoveryPhrase?: string;
  transactions: Transaction[];
  providerType: ProviderType;
  balance: number;
  isDefaultWallet: boolean;
  createdDate: string;
  modifiedDate: string;
}

export interface Transaction {
  transactionId: string;
  fromWalletAddress: string;
  toWalletAddress: string;
  fromProviderType: ProviderType;
  toProviderType: ProviderType;
  amount: number;
  memoText?: string;
  transactionHash?: string;
  blockHash?: string;
  blockNumber?: number;
  gasUsed?: number;
  gasPrice?: number;
  gasLimit?: number;
  nonce?: string;
  transactionDate: string;
  isSuccessful: boolean;
  errorMessage?: string;
  contractAddress?: string;
  tokenSymbol?: string;
  tokenDecimals?: number;
  networkName?: string;
  networkChainId?: string;
}

export interface AvatarProfile {
  id?: string;
  avatarId?: string;
  username: string;
  email?: string;
  firstName?: string;
  lastName?: string;
  avatarImageUrl?: string;
  description?: string;
  karma?: number;
  title?: string;
  createdDate?: string;
  lastLoginDate?: string;
  trustLevel?: 'bronze' | 'silver' | 'gold' | 'platinum';
  region?: string;
}

export interface AvatarRegistrationRequest {
  username: string;
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
}

export interface AvatarAuthResponse {
  avatar: AvatarProfile;
  jwtToken: string;
  refreshToken?: string;
  expiresIn?: number;
}

export interface WalletTransactionRequest {
  fromWalletAddress: string;
  toWalletAddress: string;
  fromProviderType: ProviderType;
  toProviderType: ProviderType;
  amount: number;
  memoText?: string;
}

export interface WalletImportRequest {
  key: string;
  providerToImportTo: ProviderType;
}

export interface WalletSecretPhraseImportRequest {
  secretRecoveryPhrase: string;
}

export interface WalletJsonImportRequest {
  jsonFilePath: string;
}

export interface OASISResult<T> {
  result?: T;
  isError: boolean;
  message: string;
  detailedMessage?: string;
  isLoaded?: boolean;
  isSaved?: boolean;
  warningCount?: number;
  innerMessages?: string[];
}

export interface WalletManager {
  // Transaction Operations
  sendTokenAsync(request: WalletTransactionRequest): Promise<OASISResult<Transaction>>;
  sendToken(request: WalletTransactionRequest): OASISResult<Transaction>;
  
  // Wallet Loading Operations
  loadProviderWalletsForAvatarByIdAsync(id: string, providerType?: ProviderType): Promise<OASISResult<Partial<Record<ProviderType, Wallet[]>>>>;
  loadProviderWalletsForAvatarByUsernameAsync(username: string, providerType?: ProviderType): Promise<OASISResult<Partial<Record<ProviderType, Wallet[]>>>>;
  loadProviderWalletsForAvatarByEmailAsync(email: string, providerType?: ProviderType): Promise<OASISResult<Partial<Record<ProviderType, Wallet[]>>>>;
  
  // Wallet Saving Operations
  saveProviderWalletsForAvatarByIdAsync(id: string, wallets: Partial<Record<ProviderType, Wallet[]>>, providerType?: ProviderType): Promise<OASISResult<boolean>>;
  saveProviderWalletsForAvatarByUsernameAsync(username: string, wallets: Partial<Record<ProviderType, Wallet[]>>, providerType?: ProviderType): Promise<OASISResult<boolean>>;
  saveProviderWalletsForAvatarByEmailAsync(email: string, wallets: Partial<Record<ProviderType, Wallet[]>>, providerType?: ProviderType): Promise<OASISResult<boolean>>;
  
  // Default Wallet Operations
  getAvatarDefaultWalletByIdAsync(avatarId: string, providerType: ProviderType): Promise<OASISResult<Wallet>>;
  getAvatarDefaultWalletByUsernameAsync(username: string, providerType: ProviderType): Promise<OASISResult<Wallet>>;
  getAvatarDefaultWalletByEmailAsync(email: string, providerType: ProviderType): Promise<OASISResult<Wallet>>;
  setAvatarDefaultWalletByIdAsync(avatarId: string, walletId: string, providerType: ProviderType): Promise<OASISResult<boolean>>;
  setAvatarDefaultWalletByUsernameAsync(username: string, walletId: string, providerType: ProviderType): Promise<OASISResult<boolean>>;
  setAvatarDefaultWalletByEmailAsync(email: string, walletId: string, providerType: ProviderType): Promise<OASISResult<boolean>>;
  
  // Wallet Import Operations
  importWalletUsingSecretPhase(phase: string): OASISResult<Wallet>;
  importWalletUsingJSONFile(pathToJSONFile: string): OASISResult<Wallet>;
  importWalletUsingPrivateKeyById(avatarId: string, key: string, providerToImportTo: ProviderType): OASISResult<string>;
  importWalletUsingPrivateKeyByUsername(username: string, key: string, providerToImportTo: ProviderType): OASISResult<string>;
  importWalletUsingPrivateKeyByEmail(email: string, key: string, providerToImportTo: ProviderType): OASISResult<string>;
  importWalletUsingPublicKeyById(avatarId: string, key: string, providerToImportTo: ProviderType): OASISResult<string>;
  importWalletUsingPublicKeyByUsername(username: string, key: string, providerToImportTo: ProviderType): OASISResult<string>;
  importWalletUsingPublicKeyByEmail(email: string, key: string, providerToImportTo: ProviderType): OASISResult<string>;
  
  // Utility Operations
  copyProviderWallets(wallets: Partial<Record<ProviderType, Wallet[]>>): Partial<Record<ProviderType, Wallet[]>>;
  getWalletThatPublicKeyBelongsTo(providerKey: string, providerType: ProviderType): OASISResult<Wallet>;
  clearCache(): OASISResult<boolean>;
}

export enum ProviderType {
  None = "None",
  All = "All",
  Default = "Default",
  SolanaOASIS = "SolanaOASIS",
  RadixOASIS = "RadixOASIS",
  ArbitrumOASIS = "ArbitrumOASIS",
  EthereumOASIS = "EthereumOASIS",
  PolygonOASIS = "PolygonOASIS",
  EOSIOOASIS = "EOSIOOASIS",
  TelosOASIS = "TelosOASIS",
  SEEDSOASIS = "SEEDSOASIS",
  LoomOASIS = "LoomOASIS",
  TONOASIS = "TONOASIS",
  StellarOASIS = "StellarOASIS",
  BlockStackOASIS = "BlockStackOASIS",
  HashgraphOASIS = "HashgraphOASIS",
  ElrondOASIS = "ElrondOASIS",
  TRONOASIS = "TRONOASIS",
  CosmosBlockChainOASIS = "CosmosBlockChainOASIS",
  RootstockOASIS = "RootstockOASIS",
  KadenaOASIS = "KadenaOASIS",
  ChainLinkOASIS = "ChainLinkOASIS",
  AztecOASIS = "AztecOASIS",
  IPFSOASIS = "IPFSOASIS",
  PinataOASIS = "PinataOASIS",
  HoloOASIS = "HoloOASIS",
  MongoDBOASIS = "MongoDBOASIS",
  Neo4jOASIS = "Neo4jOASIS",
  SQLLiteDBOASIS = "SQLLiteDBOASIS",
  SQLServerDBOASIS = "SQLServerDBOASIS",
  OracleDBOASIS = "OracleDBOASIS",
  GoogleCloudOASIS = "GoogleCloudOASIS",
  AzureStorageOASIS = "AzureStorageOASIS",
  AzureCosmosDBOASIS = "AzureCosmosDBOASIS",
  AWSOASIS = "AWSOASIS",
  UrbitOASIS = "UrbitOASIS",
  ThreeFoldOASIS = "ThreeFoldOASIS",
  PLANOASIS = "PLANOASIS",
  HoloWebOASIS = "HoloWebOASIS",
  SOLIDOASIS = "SOLIDOASIS",
  ActivityPubOASIS = "ActivityPubOASIS",
  ScuttlebuttOASIS = "ScuttlebuttOASIS",
  LocalFileOASIS = "LocalFileOASIS",
  ZcashOASIS = "ZcashOASIS",
  MidenOASIS = "MidenOASIS"
}

export interface User {
  id: string;
  username: string;
  email?: string;
  avatarUrl?: string;
  firstName?: string;
  lastName?: string;
  wallets?: Partial<Record<ProviderType, Wallet[]>>;
  defaultWallets?: Partial<Record<ProviderType, string>>;
  createdDate?: string;
  lastLoginDate?: string;
}

export interface WalletStore {
  user: User | null;
  wallets: Partial<Record<ProviderType, Wallet[]>>;
  selectedWallet: Wallet | null;
  transactions: Transaction[];
  isLoading: boolean;
  error: string | null;
  
  // Actions
  setUser: (user: User | null) => void;
  setWallets: (wallets: Partial<Record<ProviderType, Wallet[]>>) => void;
  setSelectedWallet: (wallet: Wallet | null) => void;
  setTransactions: (transactions: Transaction[]) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  
  // Wallet operations
  loadWallets: (userId?: string) => Promise<void>;
  sendTransaction: (request: WalletTransactionRequest) => Promise<void>;
  importWallet: (request: WalletImportRequest) => Promise<void>;
  setDefaultWallet: (walletId: string, providerType: ProviderType) => Promise<void>;
} 

export interface AvatarStat {
  id: string;
  label: string;
  value: string;
  sublabel?: string;
  trend?: {
    direction: 'up' | 'down' | 'flat';
    value: string;
  };
}

export interface AvatarActivity {
  id: string;
  source: string;
  title: string;
  description: string;
  timestamp: string;
  valueChange?: string;
  badge?: string;
  providerType?: ProviderType;
}

export interface AvatarReward {
  id: string;
  title: string;
  source: string;
  description: string;
  status: 'claimed' | 'available' | 'locked';
  imageUrl: string;
  chain?: ProviderType;
}

export interface AvatarMission {
  id: string;
  title: string;
  description: string;
  progress: number;
  target: number;
  rewardSummary: string;
  status: 'active' | 'completed' | 'locked';
}

export interface ReformStat {
  id: string;
  label: string;
  value: string;
  detail: string;
  icon: string;
}

export interface ReformBadge {
  id: string;
  title: string;
  description: string;
  status: 'unlocked' | 'in-progress';
  reward: string;
  imageUrl: string;
}

export interface ReformNewsItem {
  id: string;
  title: string;
  summary: string;
  publishedAt: string;
  channel: 'X' | 'YouTube' | 'Telegram' | 'Field';
  link: string;
}

export interface ReformRole {
  id: string;
  title: string;
  description: string;
  requirements: string[];
  permissions: string[];
  achieved: boolean;
}

export interface ReformHubData {
  greeting: string;
  stats: ReformStat[];
  badges: ReformBadge[];
  news: ReformNewsItem[];
  roles: ReformRole[];
  feed: AvatarActivity[];
  notifications: ReformNotification[];
  events: ReformEvent[];
  leaderboard: ReformLeader[];
  contacts: ReformContact[];
  hq: ReformHqItem[];
  pathways: ReformPathway[];
}

export interface ReformNotification {
  id: string;
  title: string;
  description: string;
  time: string;
  priority: 'high' | 'normal';
}

export interface ReformEvent {
  id: string;
  title: string;
  location: string;
  time: string;
  rsvps: number;
  status: 'open' | 'waitlist';
}

export interface ReformLeader {
  id: string;
  name: string;
  region: string;
  avatarColor: string;
  points: number;
  missionsCompleted: number;
  providerFocus: ProviderType;
  streak: number;
}

export interface ReformContact {
  id: string;
  name: string;
  platform: ProviderType;
  username: string;
  priority: 'normal' | 'high';
  lastSeen: string;
  action: 'message' | 'call' | 'meet';
}

export interface ReformHqItem {
  id: string;
  title: string;
  summary: string;
  category: 'policy' | 'content' | 'campaign';
  link: string;
  publishedAt: string;
}

export interface ReformPathwayStep {
  id: string;
  title: string;
  description: string;
  completed: boolean;
}

export interface ReformPathway {
  id: string;
  title: string;
  category: 'signal' | 'field' | 'policy' | 'fundraising' | 'student' | 'data';
  description: string;
  level: number;
  progress: number;
  reward: string;
  steps: ReformPathwayStep[];
  icon: string;
}