export interface STARStatus {
  isIgnited: boolean;
  version?: string;
  status?: string;
  lastUpdated: Date;
}

export interface Avatar {
  id: string;
  username: string;
  email: string;
  firstName?: string;
  lastName?: string;
  title?: string;
  avatarType?: string;
  karma?: number;
  level?: number;
  xp?: number;
  isActive?: boolean;
  isBeamedIn?: boolean;
  lastBeamedIn?: string;
  createdDate?: Date;
  lastLoginDate?: Date;
}

export interface Karma {
  id: string;
  avatarId: string;
  karma: number;
  karmaSourceType?: string;
  karmaSourceTitle?: string;
  karmaSourceDesc?: string;
  webLink?: string;
  icon?: string;
  date?: Date;
}

export interface OASISResult<T> {
  result?: T;
  isError: boolean;
  isWarning?: boolean;
  message?: string;
  exception?: any;
  innerMessages?: string[];
  count?: number;
  httpStatusCode?: number;
}

export interface STARNETItem {
  id: string;
  name: string;
  description: string;
  version?: string;
  author?: string;
  category?: string;
  tags?: string[];
  isInstalled?: boolean;
  isPublished?: boolean;
  isActive?: boolean;
  downloadCount?: number;
  rating?: number;
  createdDate?: Date;
  updatedDate?: Date;
  publishedDate?: Date;
  installedDate?: Date;
  // Additional properties for detail pages
  type?: string;
  status?: string;
  lastUpdated?: Date;
  downloads?: number;
  bookmarks?: number;
  license?: string;
  size?: number;
  fileCount?: number;
  hasDocumentation?: boolean;
  exampleCount?: number;
  technologies?: string[];
}

export interface OAPP extends STARNETItem {
  oappType?: string;
  templateType?: string;
  ourWorldLat?: number;
  ourWorldLong?: number;
  oneWorldLat?: number;
  oneWorldLong?: number;
  ourWorld3dObjectPath?: string;
  ourWorld2dSpritePath?: string;
  oneWorld3dObjectPath?: string;
  oneWorld2dSpritePath?: string;
  parentMissionId?: string;
  parentQuestId?: string;
  order?: number;
}

export interface Quest extends STARNETItem {
  questType?: string;
  parentMissionId?: string;
  parentQuestId?: string;
  order?: number;
  difficulty?: string;
  estimatedDuration?: number;
  rewards?: string[];
  requirements?: string[];
  // Additional properties for detail pages
  title?: string;
  dueDate?: Date;
  status?: string;
  progress?: number;
  completedTasks?: number;
  totalTasks?: number;
  karmaReward?: number;
  xpReward?: number;
  itemRewards?: string[];
  creator?: string;
  steps?: string[];
  completedSteps?: number;
}

export interface NFT extends STARNETItem {
  nftType?: string;
  tokenId?: string;
  contractAddress?: string;
  metadata?: any;
  imageUrl?: string;
  animationUrl?: string;
  externalUrl?: string;
  attributes?: NFTAttribute[];
  // Additional properties for detail pages
  creator?: string;
  rarity?: string;
  price?: number;
  lastSalePrice?: number;
  views?: number;
  likes?: number;
  collection?: string;
}

export interface NFTAttribute {
  trait_type: string;
  value: string | number;
  display_type?: string;
  max_value?: number;
}

export interface GeoNFT extends NFT {
  latitude?: number;
  longitude?: number;
  altitude?: number;
  radius?: number;
  isActive?: boolean;
  collectedBy?: string;
  collectedDate?: Date;
  // Additional properties for detail pages
  country?: string;
  region?: string;
  address?: string;
  status?: string;
}

export interface Mission extends STARNETItem {
  missionType?: string;
  difficulty?: string;
  estimatedDuration?: number;
  rewards?: string[];
  requirements?: string[];
  quests?: Quest[];
  // Additional properties for detail pages
  title?: string;
  priority?: string;
  dueDate?: Date;
  status?: string;
  progress?: number;
  completedTasks?: number;
  totalTasks?: number;
  karmaReward?: number;
  xpReward?: number;
  itemRewards?: string[];
  assignee?: string;
  steps?: string[];
  completedSteps?: number;
}

export interface Chapter extends STARNETItem {
  chapterType?: string;
  order?: number;
  content?: string;
  estimatedReadTime?: number;
  parentMissionId?: string;
  parentQuestId?: string;
  // Additional properties for detail pages
  title?: string;
  chapterNumber?: number;
  difficulty?: string;
  status?: string;
  readingProgress?: number;
  wordsRead?: number;
  totalWords?: number;
  publishedDate?: Date;
  estimatedReadingTime?: number;
  level?: string;
  sections?: string[];
}

export interface CelestialBody extends STARNETItem {
  celestialBodyType?: string;
  size?: number;
  mass?: number;
  temperature?: number;
  composition?: string;
  orbitRadius?: number;
  orbitSpeed?: number;
  parentId?: string;
  children?: CelestialBody[];
  // Additional properties for detail pages
  galaxy?: string;
  discoveredDate?: Date;
  radius?: number;
  orbitalPeriod?: number;
  distanceFromStar?: number;
  discoverer?: string;
  discoveryMethod?: string;
  luminosity?: number;
  age?: number;
  atmosphere?: string;
}

export interface CelestialSpace extends STARNETItem {
  celestialSpaceType?: string;
  dimensions?: {
    width: number;
    height: number;
    depth: number;
  };
  gravity?: number;
  atmosphere?: string;
  temperature?: number;
  celestialBodies?: CelestialBody[];
  // Additional properties for detail pages
  galaxy?: string;
  discoveredDate?: Date;
  diameter?: number;
  volume?: number;
  matterDensity?: number;
  darkMatterPercentage?: number;
  energyLevel?: number;
  discoverer?: string;
  discoveryMethod?: string;
  starCount?: number;
  age?: number;
  expansionRate?: number;
  gravitationalField?: string;
}

export interface Runtime extends STARNETItem {
  runtimeType?: string;
  version?: string;
  platform?: string;
  architecture?: string;
  dependencies?: string[];
  requirements?: string[];
  framework?: string;
  category?: string;
  lastUpdated?: Date;
  uptime?: string;
  language?: string;
  status?: 'Running' | 'Stopped' | 'Paused' | 'Error';
  cpuUsage?: number;
  memoryUsage?: number;
  diskUsage?: number;
  instances?: number;
  maxInstances?: number;
  port?: number;
  environment?: string;
  // Additional properties for detail pages
  owner?: string;
  securityLevel?: string;
  maxMemory?: number;
  maxCpu?: number;
  logs?: string[];
  isActive?: boolean;
  isPublic?: boolean;
  region?: string;
  cost?: number;
  imageUrl?: string;
  type?: string;
  lastStarted?: string;
  lastStopped?: string;
}

export interface Library extends STARNETItem {
  libraryType?: string;
  version?: string;
  language?: string;
  framework?: string;
  dependencies?: string[];
  documentation?: string;
  repository?: string;
}

export interface Template extends STARNETItem {
  templateType?: string;
  category?: string;
  language?: string;
  framework?: string;
  complexity?: string;
  estimatedSetupTime?: number;
  features?: string[];
  requirements?: string[];
}

export interface InventoryItem extends STARNETItem {
  itemType?: string;
  quantity?: number;
  rarity?: string;
  value?: number;
  weight?: number;
  stackable?: boolean;
  tradeable?: boolean;
  consumable?: boolean;
  effects?: string[];
  // Additional properties for detail pages
  type?: string;
  status?: string;
  imageUrl?: string;
  durability?: number;
  maxDurability?: number;
  acquiredDate?: Date;
  lastUsed?: Date;
  location?: string;
}

export interface Plugin extends STARNETItem {
  pluginType?: string;
  version?: string;
  compatibleVersions?: string[];
  dependencies?: string[];
  configuration?: any;
  permissions?: string[];
}

export interface GeoHotSpot extends STARNETItem {
  hotSpotType?: string;
  latitude?: number;
  longitude?: number;
  altitude?: number;
  radius?: number;
  isActive?: boolean;
  triggerType?: string;
  rewards?: string[];
  cooldownPeriod?: number;
  maxUses?: number;
  currentUses?: number;
}

export interface ConnectionStatus {
  isConnected: boolean;
  status: 'connecting' | 'connected' | 'disconnected' | 'error';
  lastConnected?: Date;
  error?: string;
}

export interface ProgressUpdate {
  operation: string;
  progress: number;
  message: string;
  timestamp: Date;
}

export interface SearchFilters {
  category?: string;
  type?: string;
  status?: string;
  author?: string;
  tags?: string[];
  dateRange?: {
    from: Date;
    to: Date;
  };
  rating?: {
    min: number;
    max: number;
  };
}

export interface PaginationParams {
  page: number;
  pageSize: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}

export interface SearchParams extends PaginationParams {
  searchTerm?: string;
  filters?: SearchFilters;
}

// WEB4 OASIS API Data Types for Karma Integration
export interface OAPPKarmaData {
  oappId: string;
  oappName: string;
  registeredAvatars: AvatarKarmaData[];
  totalKarma: number;
  userCount: number;
  averageKarma: number;
  karmaLevel: string;
  karmaSources: string[];
}

export interface AvatarKarmaData {
  avatarId: string;
  avatarName: string;
  email: string;
  totalKarma: number;
  karmaTransactions: KarmaTransaction[];
  registrationDate: string;
  lastLoginDate: string;
}

export interface KarmaTransaction {
  transactionId: string;
  karmaAmount: number;
  karmaType: 'Earned' | 'Lost' | 'Transferred';
  source: string;
  description: string;
  timestamp: string;
  oappId: string;
}

export interface AvatarListResult {
  avatars: AvatarKarmaData[];
  totalCount: number;
  hasMore: boolean;
}