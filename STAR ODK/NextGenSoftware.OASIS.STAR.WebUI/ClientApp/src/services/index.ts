/**
 * Main Service Aggregator
 * Centralized exports for all services
 */

// Core Services
export { starCoreService } from './core/starCoreService';
export { avatarService } from './core/avatarService';
export { walletService } from './core/walletService';
export { keysService } from './core/keysService';
export { mapService } from './core/mapService';
export { dataService } from './core/dataService';
export { olandService } from './core/olandService';
export { searchService } from './core/searchService';
export { onodeService } from './core/onodeService';
export { seedsService } from './core/seedsService';
export { hypernetService } from './core/hypernetService';
export { onetService } from './core/onetService';

// STARNET Services
export { oappService } from './starnet/oappService';
export { templateService } from './starnet/templateService';
export { runtimeService } from './starnet/runtimeService';
export { libraryService } from './starnet/libraryService';
export { celestialBodyService } from './starnet/celestialBodyService';
export { celestialSpaceService } from './starnet/celestialSpaceService';
export { zomeService } from './starnet/zomeService';
export { holonService } from './starnet/holonService';
export { pluginService } from './starnet/pluginService';
export { parkService } from './starnet/parkService';

// Data Services
export { nftService } from './data/nftService';
export { geoNftService } from './data/geoNftService';
export { geoHotSpotService } from './data/geoHotSpotService';
export { questService } from './data/questService';
export { missionService } from './data/missionService';
export { chapterService } from './data/chapterService';
export { inventoryService } from './data/inventoryService';

// Metadata Services
export { celestialBodyMetaService } from './metadata/celestialBodyMetaService';
export { zomeMetaService } from './metadata/zomeMetaService';
export { holonMetaService } from './metadata/holonMetaService';

// Configuration
export { API_CONFIG } from './config/apiConfig';
export { isDemoMode, setDemoMode, toggleDemoMode, DEMO_CONFIG } from './config/demoConfig';

// Base Classes
export { BaseService } from './base/baseService';
export { starApiClient, web4ApiClient, hubApiClient } from './base/apiClient';

// Demo Data
export { starDemoData } from './demo/starDemoData';
export { starnetDemoData } from './demo/starnetDemoData';
export { dataDemoData } from './demo/dataDemoData';

// Re-export types
export type { OASISResult, STARStatus, Avatar, Karma } from '../types/star';
