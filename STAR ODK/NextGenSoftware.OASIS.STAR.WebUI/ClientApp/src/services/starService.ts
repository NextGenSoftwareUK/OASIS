import axios from 'axios';
import { OASISResult, STARStatus, Avatar, Karma, OAPPKarmaData, AvatarKarmaData, AvatarListResult } from '../types/star';

const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5000/api'; // ONODE WebAPI with STAR endpoints
const WEB4_API_BASE_URL = process.env.REACT_APP_WEB4_API_URL || 'http://localhost:50563/api'; // WEB4 OASIS API

const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

const web4Api = axios.create({
  baseURL: WEB4_API_BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor
api.interceptors.request.use(
  (config) => {
    // Add auth token if available
    const token = localStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor
api.interceptors.response.use(
  (response) => {
    return response;
  },
  (error) => {
    if (error.response?.status === 401) {
      // Handle unauthorized access
      localStorage.removeItem('authToken');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export const starService = {
  // STAR Core Operations
  async igniteSTAR(): Promise<OASISResult<any>> {
    const response = await api.post('/star/ignite');
    console.log('STAR Ignite API Response:', response.data); // Debug logging
    return response.data;
  },

  async extinguishStar(): Promise<OASISResult<boolean>> {
    const response = await api.post('/star/extinguish');
    return response.data;
  },

  async getSTARStatus(): Promise<STARStatus> {
    const response = await api.get('/star/status');
    console.log('STAR Status API Response:', response.data); // Debug logging
    return {
      isIgnited: response.data.isIgnited || false,
      lastUpdated: new Date(),
    };
  },

  // Beam In
  async beamIn(username: string, password: string): Promise<OASISResult<any>> {
    const response = await api.post('/star/beam-in', { username, password });
    return response.data;
  },

  // Create Avatar
  async createAvatar(request: {
    title: string;
    firstName: string;
    lastName: string;
    email: string;
    username: string;
    password: string;
  }): Promise<OASISResult<any>> {
    const response = await api.post('/star/create-avatar', request);
    return response.data;
  },

  // Light OAPP
  async lightOAPP(request: {
    oappName: string;
    oappDescription: string;
    oappType: any;
    oappTemplateId: string;
    oappTemplateVersion: number;
    genesisType: any;
  }): Promise<OASISResult<any>> {
    const response = await api.post('/star/light', request);
    return response.data;
  },

  // Seed OAPP
  async seedOAPP(request: {
    fullPathToOAPP: string;
    launchTarget: string;
    fullPathToPublishTo?: string;
    registerOnSTARNET?: boolean;
    dotnetPublish?: boolean;
    generateOAPPSource?: boolean;
    uploadOAPPSourceToSTARNET?: boolean;
    makeOAPPSourcePublic?: boolean;
    generateOAPPBinary?: boolean;
    generateOAPPSelfContainedBinary?: boolean;
    generateOAPPSelfContainedFullBinary?: boolean;
    uploadOAPPToCloud?: boolean;
    uploadOAPPSelfContainedToCloud?: boolean;
    uploadOAPPSelfContainedFullToCloud?: boolean;
  }): Promise<OASISResult<any>> {
    const response = await api.post('/star/seed', request);
    return response.data;
  },

  // UnSeed OAPP
  async unSeedOAPP(oappId: string, version: number = 0): Promise<OASISResult<any>> {
    const response = await api.post('/star/unseed', { oappId, version });
    return response.data;
  },

  // Avatar Operations
  async getBeamedInAvatar(): Promise<OASISResult<Avatar>> {
    const response = await api.get('/star/avatar/current');
    return response.data;
  },

  async beamInAvatar(): Promise<OASISResult<Avatar>> {
    const response = await api.post('/star/avatar/beam-in');
    return response.data;
  },


  async getAvatar(id: string): Promise<OASISResult<Avatar>> {
    const response = await api.get(`/star/avatar/${id}`);
    return response.data;
  },

  async getAvatarByUsername(username: string): Promise<OASISResult<Avatar>> {
    const response = await api.get(`/star/avatar/username/${username}`);
    return response.data;
  },

  async loginAvatar(username: string, password: string): Promise<OASISResult<Avatar>> {
    const response = await api.post('/star/avatar/login', {
      username,
      password,
    });
    return response.data;
  },

  async saveAvatar(avatar: Avatar): Promise<OASISResult<Avatar>> {
    const response = await api.put('/star/avatar', avatar);
    return response.data;
  },

  async deleteAvatar(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/star/avatar/${id}`);
    return response.data;
  },

  async getAllAvatars(): Promise<OASISResult<Avatar[]>> {
    const response = await api.get('/star/avatars');
    return response.data;
  },

  async searchAvatars(searchTerm: string): Promise<OASISResult<Avatar[]>> {
    const response = await api.get('/star/avatars/search', {
      params: { searchTerm },
    });
    return response.data;
  },

  // Karma Operations
  async getKarma(avatarId: string): Promise<OASISResult<Karma>> {
    const response = await api.get(`/star/karma/${avatarId}`);
    return response.data;
  },

  async addKarma(avatarId: string, karma: number): Promise<OASISResult<Karma>> {
    const response = await api.post(`/star/karma/${avatarId}/add`, karma);
    return response.data;
  },

  async removeKarma(avatarId: string, karma: number): Promise<OASISResult<Karma>> {
    const response = await api.post(`/star/karma/${avatarId}/remove`, karma);
    return response.data;
  },

  async setKarma(avatarId: string, karma: number): Promise<OASISResult<Karma>> {
    const response = await api.post(`/star/karma/${avatarId}/set`, karma);
    return response.data;
  },

  async getAllKarma(): Promise<OASISResult<Karma[]>> {
    const response = await api.get('/star/karma');
    return response.data;
  },

  async getKarmaBetween(fromDate: Date, toDate: Date): Promise<OASISResult<Karma[]>> {
    const response = await api.get('/star/karma/between', {
      params: { fromDate: fromDate.toISOString(), toDate: toDate.toISOString() },
    });
    return response.data;
  },

  async getKarmaAbove(karmaLevel: number): Promise<OASISResult<Karma[]>> {
    const response = await api.get(`/star/karma/above/${karmaLevel}`);
    return response.data;
  },

  async getKarmaBelow(karmaLevel: number): Promise<OASISResult<Karma[]>> {
    const response = await api.get(`/star/karma/below/${karmaLevel}`);
    return response.data;
  },

  // WEB4 OASIS API Methods for Real Karma Data
  async getOAPPKarmaData(oappId: string): Promise<OAPPKarmaData> {
    try {
      console.log(`Getting karma data for OAPP: ${oappId}`);
      
      // Step 1: Get registered avatars for this OAPP
      const avatars = await this.getRegisteredAvatarsForOAPP(oappId);
      
      if (avatars.length === 0) {
        // No avatars registered to this OAPP
        return {
          oappId,
          oappName: 'Unknown OAPP',
          registeredAvatars: [],
          totalKarma: 0,
          userCount: 0,
          averageKarma: 0,
          karmaLevel: 'None',
          karmaSources: []
        };
      }
      
      // Step 2: Calculate total karma from all avatars
      return this.calculateOAPPKarmaData(oappId, avatars);
    } catch (error) {
      console.error('Error getting OAPP karma data:', error);
      throw error;
    }
  },

  async getRegisteredAvatarsForOAPP(oappId: string): Promise<AvatarKarmaData[]> {
    try {
      // Try LoadAvatarsForParent first
      try {
        const response = await web4Api.get<OASISResult<AvatarListResult>>(`/avatar/load-avatars-for-parent/${oappId}`);
        if (!response.data.isError && response.data.result) {
          return response.data.result.avatars;
        }
      } catch (error) {
        console.warn('LoadAvatarsForParent failed, trying fallback methods:', error);
      }
      
      // Fallback: Try LoadHolonsForParent
      try {
        const response = await web4Api.get<OASISResult<any[]>>(`/data/load-holons-for-parent/${oappId}`);
        if (!response.data.isError && response.data.result) {
          return this.convertHolonsToAvatars(response.data.result);
        }
      } catch (error) {
        console.warn('LoadHolonsForParent failed, trying final fallback:', error);
      }
      
      // Final fallback: Try LoadHolonsByMetaData
      try {
        const searchData = {
          parentId: oappId,
          holonType: 'Avatar'
        };
        
        const response = await web4Api.post<OASISResult<any[]>>('/data/load-holons-by-metadata', searchData);
        if (!response.data.isError && response.data.result) {
          return this.convertHolonsToAvatars(response.data.result);
        }
      } catch (error) {
        console.error('All avatar loading methods failed:', error);
      }
      
      return [];
    } catch (error) {
      console.error('Error getting registered avatars:', error);
      return [];
    }
  },

  convertHolonsToAvatars(holons: any[]): AvatarKarmaData[] {
    return holons.map(holon => ({
      avatarId: holon.id || 'placeholder_id',
      avatarName: holon.name || 'Unknown Avatar',
      email: holon.email || 'unknown@example.com',
      totalKarma: holon.karma || 0,
      karmaTransactions: holon.karmaTransactions || [],
      registrationDate: holon.createdDate || new Date().toISOString(),
      lastLoginDate: holon.lastLoginDate || new Date().toISOString()
    }));
  },

  calculateOAPPKarmaData(oappId: string, avatars: AvatarKarmaData[]): OAPPKarmaData {
    let totalKarma = 0;
    const karmaSources: string[] = [];
    const allTransactions: any[] = [];
    
    // Calculate total karma from all avatars
    avatars.forEach(avatar => {
      totalKarma += avatar.totalKarma;
      
      // Collect karma transactions for this OAPP
      if (avatar.karmaTransactions) {
        avatar.karmaTransactions.forEach(transaction => {
          if (transaction.oappId === oappId) {
            allTransactions.push(transaction);
            
            // Add unique karma sources
            if (transaction.source && !karmaSources.includes(transaction.source)) {
              karmaSources.push(transaction.source);
            }
          }
        });
      }
    });
    
    // Calculate average karma
    const averageKarma = avatars.length > 0 ? totalKarma / avatars.length : 0;
    
    // Determine karma level
    const karmaLevel = this.determineKarmaLevel(totalKarma);
    
    return {
      oappId,
      oappName: `OAPP ${oappId}`, // This would come from OAPP data
      registeredAvatars: avatars,
      totalKarma,
      userCount: avatars.length,
      averageKarma,
      karmaLevel,
      karmaSources
    };
  },

  determineKarmaLevel(totalKarma: number): string {
    if (totalKarma <= 0) return 'None';
    if (totalKarma < 100) return 'Low';
    if (totalKarma < 1000) return 'Medium';
    if (totalKarma < 10000) return 'High';
    if (totalKarma < 100000) return 'Very High';
    return 'Legendary';
  },

  // NFT Operations
  async getAllNFTs(): Promise<OASISResult<any[]>> {
    const response = await api.get('/star/nfts');
    return response.data;
  },

  async createNFT(request: {
    name: string;
    description: string;
    imageUrl: string;
    price: number;
    rarity: string;
    category: string;
  }): Promise<OASISResult<any>> {
    const response = await api.post('/star/nfts', request);
    return response.data;
  },

  async deleteNFT(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/star/nfts/${id}`);
    return response.data;
  },

  // Settings Operations
  async getSettings(): Promise<OASISResult<any>> {
    const response = await api.get('/star/settings');
    return response.data;
  },

  async updateSettings(settings: any): Promise<OASISResult<any>> {
    const response = await api.put('/star/settings', settings);
    return response.data;
  },

  // GeoNFT Operations
  async getAllGeoNFTs(): Promise<OASISResult<any[]>> {
    const response = await api.get('/star/geonfts');
    return response.data;
  },

  async createGeoNFT(request: any): Promise<OASISResult<any>> {
    const response = await api.post('/star/geonfts', request);
    return response.data;
  },

  async deleteGeoNFT(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/star/geonfts/${id}`);
    return response.data;
  },

  // Mission Operations
  async getAllMissions(): Promise<OASISResult<any[]>> {
    const response = await api.get('/star/missions');
    return response.data;
  },

  async createMission(request: any): Promise<OASISResult<any>> {
    const response = await api.post('/star/missions', request);
    return response.data;
  },

  async deleteMission(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/star/missions/${id}`);
    return response.data;
  },

  // Inventory Operations
  async getAllInventoryItems(): Promise<OASISResult<any[]>> {
    const response = await api.get('/star/inventory');
    return response.data;
  },

  async createInventoryItem(request: any): Promise<OASISResult<any>> {
    const response = await api.post('/star/inventory', request);
    return response.data;
  },

  async deleteInventoryItem(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/star/inventory/${id}`);
    return response.data;
  },

  // Store Operations
  async getStoreItems(): Promise<OASISResult<any[]>> {
    const response = await api.get('/star/store');
    return response.data;
  },

  // Celestial Bodies Operations
  async getAllCelestialBodies(): Promise<OASISResult<any[]>> {
    const response = await api.get('/star/celestial-bodies');
    return response.data;
  },

  async createCelestialBody(request: any): Promise<OASISResult<any>> {
    const response = await api.post('/star/celestial-bodies', request);
    return response.data;
  },

  async deleteCelestialBody(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/star/celestial-bodies/${id}`);
    return response.data;
  },

  // Celestial Spaces Operations
  async getAllCelestialSpaces(): Promise<OASISResult<any[]>> {
    const response = await api.get('/star/celestial-spaces');
    return response.data;
  },

  async createCelestialSpace(request: any): Promise<OASISResult<any>> {
    const response = await api.post('/star/celestial-spaces', request);
    return response.data;
  },

  async deleteCelestialSpace(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/star/celestial-spaces/${id}`);
    return response.data;
  },

  // Chapters Operations
  async getAllChapters(): Promise<OASISResult<any[]>> {
    const response = await api.get('/star/chapters');
    return response.data;
  },

  async createChapter(request: any): Promise<OASISResult<any>> {
    const response = await api.post('/star/chapters', request);
    return response.data;
  },

  async deleteChapter(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/star/chapters/${id}`);
    return response.data;
  },

  // Geo Hot Spots Operations
  async getAllGeoHotSpots(): Promise<OASISResult<any[]>> {
    const response = await api.get('/star/geo-hotspots');
    return response.data;
  },

  async createGeoHotSpot(request: any): Promise<OASISResult<any>> {
    const response = await api.post('/star/geo-hotspots', request);
    return response.data;
  },

  async deleteGeoHotSpot(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/star/geo-hotspots/${id}`);
    return response.data;
  },

  // Libraries Operations
  async getAllLibraries(): Promise<OASISResult<any[]>> {
    const response = await api.get('/star/libraries');
    return response.data;
  },

  async createLibrary(request: any): Promise<OASISResult<any>> {
    const response = await api.post('/star/libraries', request);
    return response.data;
  },

  async deleteLibrary(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/star/libraries/${id}`);
    return response.data;
  },

  // Plugins Operations
  async getAllPlugins(): Promise<OASISResult<any[]>> {
    const response = await api.get('/star/plugins');
    return response.data;
  },

  async createPlugin(request: any): Promise<OASISResult<any>> {
    const response = await api.post('/star/plugins', request);
    return response.data;
  },

  async deletePlugin(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/star/plugins/${id}`);
    return response.data;
  },

  // Runtimes Operations
  async getAllRuntimes(): Promise<OASISResult<any[]>> {
    const response = await api.get('/star/runtimes');
    return response.data;
  },

  async createRuntime(request: any): Promise<OASISResult<any>> {
    const response = await api.post('/star/runtimes', request);
    return response.data;
  },

  async deleteRuntime(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/star/runtimes/${id}`);
    return response.data;
  },

  // Templates Operations
  async getAllTemplates(): Promise<OASISResult<any[]>> {
    const response = await api.get('/star/templates');
    return response.data;
  },

  async createTemplate(request: any): Promise<OASISResult<any>> {
    const response = await api.post('/star/templates', request);
    return response.data;
  },

  async deleteTemplate(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/star/templates/${id}`);
    return response.data;
  },

  // Karma Operations
  async getKarmaLeaderboard(): Promise<OASISResult<any>> {
    const response = await api.get('/star/karma/leaderboard');
    return response.data;
  },

  // My Data Operations
  async getMyDataFiles(): Promise<OASISResult<any>> {
    const response = await api.get('/star/my-data/files');
    return response.data;
  },

  async uploadFile(file: File, options?: any): Promise<OASISResult<any>> {
    const formData = new FormData();
    formData.append('file', file);
    if (options) {
      formData.append('options', JSON.stringify(options));
    }
    const response = await api.post('/star/my-data/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  async deleteFile(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/star/my-data/files/${id}`);
    return response.data;
  },

  async updateFilePermissions(id: string, permissions: any): Promise<OASISResult<boolean>> {
    const response = await api.put(`/star/my-data/files/${id}/permissions`, permissions);
    return response.data;
  },
};
