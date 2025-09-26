import axios from 'axios';
import { OASISResult, STARStatus, Avatar, Karma, OAPPKarmaData, AvatarKarmaData, AvatarListResult } from '../types/star';

const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:50564/api';
const WEB4_API_BASE_URL = process.env.REACT_APP_WEB4_API_URL || 'http://localhost:50563/api';

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
};
