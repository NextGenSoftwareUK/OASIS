import axios from 'axios';
import { OASISResult, STARStatus, Avatar, Karma, OAPPKarmaData, AvatarKarmaData, AvatarListResult } from '../types/star';

const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:50564/api'; // STAR Web API
const WEB4_API_BASE_URL = process.env.REACT_APP_WEB4_API_URL || 'http://localhost:5000/api'; // ONODE WEB4 OASIS API

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

  // Avatar Operations - Using WEB4 OASIS API for core avatar functionality
  async getCurrentAvatar(): Promise<OASISResult<Avatar>> {
    try {
      // Try STAR Web API first (if it has avatar endpoints)
      const response = await api.get('/avatar/current');
      return response.data;
    } catch (error) {
      // Fallback to WEB4 OASIS API
      try {
        const response = await web4Api.get('/avatar/get-current-avatar');
        return response.data;
      } catch (web4Error) {
        console.error('Error fetching current avatar:', web4Error);
        // Return demo data as final fallback
        return {
          result: {
            id: '1',
            username: 'demo_user',
            email: 'demo@example.com',
            firstName: 'Demo',
            lastName: 'User',
            karma: 1000,
            level: 5,
            xp: 2500,
            isActive: true,
            createdDate: new Date(),
            lastLoginDate: new Date()
          },
          isError: false,
          message: 'Demo avatar loaded (APIs unavailable)'
        };
      }
    }
  },

  async authenticateAvatar(username: string, password: string): Promise<OASISResult<any>> {
    try {
      // Try STAR Web API first
      const response = await api.post('/avatar/authenticate', {
        username,
        password,
      });
      return response.data;
    } catch (error) {
      // Fallback to WEB4 OASIS API
      try {
        const response = await web4Api.post('/avatar/authenticate', {
          username,
          password,
        });
        return response.data;
      } catch (web4Error) {
        console.error('Error authenticating avatar:', web4Error);
        throw web4Error;
      }
    }
  },

  async getAllAvatars(): Promise<OASISResult<Avatar[]>> {
    try {
      // Use WEB4 OASIS API for avatar operations
      const response = await web4Api.get('/avatar/load-all-avatars');
      return response.data;
    } catch (error) {
      console.error('Error fetching avatars:', error);
      // Return demo data as fallback
      return {
        result: [
          {
            id: '1',
            username: 'demo_user',
            email: 'demo@example.com',
            firstName: 'Demo',
            lastName: 'User',
            karma: 1000,
            level: 5,
            xp: 2500,
            isActive: true,
            createdDate: new Date(),
            lastLoginDate: new Date()
          }
        ],
        isError: false,
        message: 'Demo avatars loaded (API unavailable)'
      };
    }
  },

  // Karma Operations - ONLY using WEB4 OASIS API
  async getKarma(avatarId: string): Promise<OASISResult<Karma>> {
    try {
      const response = await web4Api.get(`/karma/get-karma-for-avatar/${avatarId}`);
      return response.data;
    } catch (error) {
      console.error('Error fetching karma:', error);
      throw error;
    }
  },

  async getKarmaLeaderboard(limit: number = 50): Promise<OASISResult<any>> {
    try {
      // Get all avatars from WEB4 OASIS API
      const avatarsResponse = await web4Api.get('/avatar/load-all-avatars');
      
      if (avatarsResponse.data.isError || !avatarsResponse.data.result) {
        throw new Error('Failed to load avatars');
      }

      // Get karma for each avatar and create leaderboard
      const leaderboard = [];
      
      for (const avatar of avatarsResponse.data.result) {
        try {
          const karmaResponse = await web4Api.get(`/karma/get-karma-for-avatar/${avatar.id}`);
          const totalKarma = karmaResponse.data.isError ? 0 : karmaResponse.data.result;
          
          leaderboard.push({
            avatarId: avatar.id,
            avatarName: avatar.username || 'Unknown',
            email: avatar.email || 'unknown@example.com',
            totalKarma: totalKarma,
            karmaLevel: this.determineKarmaLevel(totalKarma),
            lastActivity: avatar.lastLoginDate || new Date().toISOString(),
            rank: 0 // Will be set after sorting
          });
        } catch (error) {
          console.warn(`Failed to get karma for avatar ${avatar.id}:`, error);
          // Add avatar with 0 karma
          leaderboard.push({
            avatarId: avatar.id,
            avatarName: avatar.username || 'Unknown',
            email: avatar.email || 'unknown@example.com',
            totalKarma: 0,
            karmaLevel: 'Common',
            lastActivity: avatar.lastLoginDate || new Date().toISOString(),
            rank: 0
          });
        }
      }

      // Sort by total karma (descending) and assign ranks
      leaderboard.sort((a, b) => b.totalKarma - a.totalKarma);
      leaderboard.forEach((item, index) => {
        item.rank = index + 1;
      });

      return {
        result: leaderboard.slice(0, limit),
        isError: false,
        message: 'Karma leaderboard loaded successfully'
      };
    } catch (error) {
      console.error('Error loading karma leaderboard:', error);
      throw error;
    }
  },

  async addKarma(avatarId: string, karma: number): Promise<OASISResult<Karma>> {
    try {
      const response = await web4Api.post('/karma/add-karma', {
        avatarId: avatarId,
        karma: karma
      });
      return response.data;
    } catch (error) {
      console.error('Error adding karma:', error);
      throw error;
    }
  },

  async removeKarma(avatarId: string, karma: number): Promise<OASISResult<Karma>> {
    try {
      const response = await web4Api.post('/karma/remove-karma', {
        avatarId: avatarId,
        karma: karma
      });
      return response.data;
    } catch (error) {
      console.error('Error removing karma:', error);
      throw error;
    }
  },

  async setKarma(avatarId: string, karma: number): Promise<OASISResult<Karma>> {
    try {
      const response = await web4Api.post('/karma/set-karma', {
        avatarId: avatarId,
        karma: karma
      });
      return response.data;
    } catch (error) {
      console.error('Error setting karma:', error);
      throw error;
    }
  },

  async getAllKarma(): Promise<OASISResult<Karma[]>> {
    try {
      const response = await web4Api.get('/karma/get-all-karma');
      return response.data;
    } catch (error) {
      console.error('Error fetching all karma:', error);
      throw error;
    }
  },

  async getKarmaBetween(fromDate: Date, toDate: Date): Promise<OASISResult<Karma[]>> {
    try {
      const response = await web4Api.get('/karma/get-karma-between', {
        params: { fromDate: fromDate.toISOString(), toDate: toDate.toISOString() },
      });
      return response.data;
    } catch (error) {
      console.error('Error fetching karma between dates:', error);
      throw error;
    }
  },

  async getKarmaAbove(karmaLevel: number): Promise<OASISResult<Karma[]>> {
    try {
      const response = await web4Api.get(`/karma/get-karma-above/${karmaLevel}`);
      return response.data;
    } catch (error) {
      console.error('Error fetching karma above level:', error);
      throw error;
    }
  },

  async getKarmaBelow(karmaLevel: number): Promise<OASISResult<Karma[]>> {
    try {
      const response = await web4Api.get(`/karma/get-karma-below/${karmaLevel}`);
      return response.data;
    } catch (error) {
      console.error('Error fetching karma below level:', error);
      throw error;
    }
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

  // NFT Operations - Using STAR Web API
  async getAllNFTs(): Promise<OASISResult<any[]>> {
    try {
      const response = await api.get('/nfts');
      console.log('NFTs API Response:', response.data);
      return response.data;
    } catch (error) {
      console.error('Error fetching NFTs from STAR Web API:', error);
      // Return demo data as fallback
      return {
        result: [
          {
            id: '1',
            name: 'Cosmic Dragon',
            description: 'A legendary dragon from the depths of space',
            imageUrl: 'https://images.unsplash.com/photo-1518709268805-4e9042af2176?w=400&h=400&fit=crop&auto=format&q=80',
            price: 2.5,
            rarity: 'Legendary',
            category: 'Creatures',
            owner: 'SpaceExplorer',
            mintDate: new Date().toISOString(),
            attributes: [
              { trait_type: 'Power', value: 95 },
              { trait_type: 'Speed', value: 88 },
              { trait_type: 'Magic', value: 92 }
            ]
          },
          {
            id: '2',
            name: 'Neon Cityscape',
            description: 'A futuristic city bathed in neon lights',
            imageUrl: 'https://images.unsplash.com/photo-1518709268805-4e9042af2176?w=400&h=400&fit=crop&auto=format&q=80',
            price: 1.8,
            rarity: 'Epic',
            category: 'Landscapes',
            owner: 'CyberPunk',
            mintDate: new Date().toISOString(),
            attributes: [
              { trait_type: 'Brightness', value: 100 },
              { trait_type: 'Futuristic', value: 95 },
              { trait_type: 'Energy', value: 90 }
            ]
          }
        ],
        isError: false,
        message: 'Demo NFTs loaded (STAR Web API unavailable)'
      };
    }
  },

  async createNFT(request: {
    name: string;
    description: string;
    imageUrl: string;
    price: number;
    rarity: string;
    category: string;
  }): Promise<OASISResult<any>> {
    const response = await api.post('/nfts', request);
    return response.data;
  },

  async deleteNFT(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/nfts/${id}`);
    return response.data;
  },

  // GeoNFT Operations - Using actual GeoNFTs controller endpoints
  async getAllGeoNFTs(): Promise<OASISResult<any[]>> {
    const response = await api.get('/geonfts');
    return response.data;
  },

  async createGeoNFT(request: any): Promise<OASISResult<any>> {
    const response = await api.post('/geonfts', request);
    return response.data;
  },

  async deleteGeoNFT(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/geonfts/${id}`);
    return response.data;
  },

  // Mission Operations - Using STAR Web API
  async getAllMissions(): Promise<OASISResult<any[]>> {
    try {
      const response = await api.get('/missions');
      console.log('Missions API Response:', response.data);
      return response.data;
    } catch (error) {
      console.error('Error fetching missions from STAR Web API:', error);
      // Return demo data as fallback
      return {
        result: [
          {
            id: '1',
            title: 'Explore the Cosmic Frontier',
            description: 'Journey to the edge of the known universe and discover new worlds',
            status: 'Active',
            difficulty: 'Hard',
            reward: 1000,
            xpReward: 500,
            category: 'Exploration',
            startDate: new Date().toISOString(),
            endDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString(),
            objectives: [
              'Reach the outer rim',
              'Scan 5 new planets',
              'Collect rare minerals'
            ],
            progress: 65
          },
          {
            id: '2',
            title: 'Defend the Space Station',
            description: 'Protect the orbital station from alien invaders',
            status: 'Active',
            difficulty: 'Medium',
            reward: 750,
            xpReward: 300,
            category: 'Combat',
            startDate: new Date().toISOString(),
            endDate: new Date(Date.now() + 3 * 24 * 60 * 60 * 1000).toISOString(),
            objectives: [
              'Eliminate 20 hostiles',
              'Repair station systems',
              'Evacuate civilians'
            ],
            progress: 40
          }
        ],
        isError: false,
        message: 'Demo missions loaded (STAR Web API unavailable)'
      };
    }
  },

  async createMission(request: any): Promise<OASISResult<any>> {
    const response = await api.post('/missions', request);
    return response.data;
  },

  async deleteMission(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/missions/${id}`);
    return response.data;
  },

  // Inventory Operations - Using actual InventoryItems controller endpoints
  async getAllInventoryItems(): Promise<OASISResult<any[]>> {
    try {
      const response = await api.get('/inventoryitems');
      console.log('Inventory Items API Response:', response.data);
      return response.data;
    } catch (error) {
      console.error('Error fetching inventory items from API:', error);
      // Return demo data as fallback
      return {
        result: [
          {
            id: '1',
            name: 'Quantum Blaster',
            description: 'A powerful energy weapon from the future',
            type: 'Weapon',
            rarity: 'Legendary',
            power: 95,
            durability: 100,
            value: 2500,
            imageUrl: 'https://images.unsplash.com/photo-1518709268805-4e9042af2176?w=400&h=400&fit=crop&auto=format&q=80',
            acquiredDate: new Date().toISOString(),
            equipped: true
          },
          {
            id: '2',
            name: 'Shield Generator',
            description: 'Advanced energy shield technology',
            type: 'Defense',
            rarity: 'Epic',
            power: 85,
            durability: 90,
            value: 1800,
            imageUrl: 'https://images.unsplash.com/photo-1518709268805-4e9042af2176?w=400&h=400&fit=crop&auto=format&q=80',
            acquiredDate: new Date().toISOString(),
            equipped: false
          },
          {
            id: '3',
            name: 'Space Suit',
            description: 'Protective gear for space exploration',
            type: 'Armor',
            rarity: 'Rare',
            power: 70,
            durability: 95,
            value: 1200,
            imageUrl: 'https://images.unsplash.com/photo-1518709268805-4e9042af2176?w=400&h=400&fit=crop&auto=format&q=80',
            acquiredDate: new Date().toISOString(),
            equipped: true
          }
        ],
        isError: false,
        message: 'Demo inventory items loaded (API unavailable)'
      };
    }
  },

  async createInventoryItem(request: any): Promise<OASISResult<any>> {
    const response = await api.post('/inventoryitems', request);
    return response.data;
  },

  async deleteInventoryItem(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/inventoryitems/${id}`);
    return response.data;
  },

  // Celestial Bodies Operations - Using actual CelestialBodies controller endpoints
  async getAllCelestialBodies(): Promise<OASISResult<any[]>> {
    const response = await api.get('/celestialbodies');
    return response.data;
  },

  async createCelestialBody(request: any): Promise<OASISResult<any>> {
    const response = await api.post('/celestialbodies', request);
    return response.data;
  },

  async deleteCelestialBody(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/celestialbodies/${id}`);
    return response.data;
  },

  // Celestial Spaces Operations - Using actual CelestialSpaces controller endpoints
  async getAllCelestialSpaces(): Promise<OASISResult<any[]>> {
    const response = await api.get('/celestialspaces');
    return response.data;
  },

  async createCelestialSpace(request: any): Promise<OASISResult<any>> {
    const response = await api.post('/celestialspaces', request);
    return response.data;
  },

  async deleteCelestialSpace(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/celestialspaces/${id}`);
    return response.data;
  },

  // Chapters Operations - Using actual Chapters controller endpoints
  async getAllChapters(): Promise<OASISResult<any[]>> {
    const response = await api.get('/chapters');
    return response.data;
  },

  async createChapter(request: any): Promise<OASISResult<any>> {
    const response = await api.post('/chapters', request);
    return response.data;
  },

  async deleteChapter(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/chapters/${id}`);
    return response.data;
  },

  // Geo Hot Spots Operations - Using actual GeoHotSpots controller endpoints
  async getAllGeoHotSpots(): Promise<OASISResult<any[]>> {
    const response = await api.get('/geohotspots');
    return response.data;
  },

  async createGeoHotSpot(request: any): Promise<OASISResult<any>> {
    const response = await api.post('/geohotspots', request);
    return response.data;
  },

  async deleteGeoHotSpot(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/geohotspots/${id}`);
    return response.data;
  },

  // OAPPs Operations - Using actual OAPPs controller endpoints
  async getAllOAPPs(): Promise<OASISResult<any[]>> {
    const response = await api.get('/oapps');
    return response.data;
  },

  async createOAPP(request: any): Promise<OASISResult<any>> {
    const response = await api.post('/oapps', request);
    return response.data;
  },

  async deleteOAPP(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/oapps/${id}`);
    return response.data;
  },

  // Quests Operations - Using actual Quests controller endpoints
  async getAllQuests(): Promise<OASISResult<any[]>> {
    const response = await api.get('/quests');
    return response.data;
  },

  async createQuest(request: any): Promise<OASISResult<any>> {
    const response = await api.post('/quests', request);
    return response.data;
  },

  async deleteQuest(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/quests/${id}`);
    return response.data;
  },

  // Holons Operations - Using actual Holons controller endpoints
  async getAllHolons(): Promise<OASISResult<any[]>> {
    const response = await api.get('/holons');
    return response.data;
  },

  async createHolon(request: any): Promise<OASISResult<any>> {
    const response = await api.post('/holons', request);
    return response.data;
  },

  async deleteHolon(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/holons/${id}`);
    return response.data;
  },

  // Zomes Operations - Using actual Zomes controller endpoints
  async getAllZomes(): Promise<OASISResult<any[]>> {
    const response = await api.get('/zomes');
    return response.data;
  },

  async createZome(request: any): Promise<OASISResult<any>> {
    const response = await api.post('/zomes', request);
    return response.data;
  },

  async deleteZome(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/zomes/${id}`);
    return response.data;
  },

  // Parks Operations - Using actual Parks controller endpoints
  async getAllParks(): Promise<OASISResult<any[]>> {
    const response = await api.get('/parks');
    return response.data;
  },

  async createPark(request: any): Promise<OASISResult<any>> {
    const response = await api.post('/parks', request);
    return response.data;
  },

  async deletePark(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/parks/${id}`);
    return response.data;
  },

  // Note: The following endpoints don't exist in the current Web API, so we'll remove them
  // or implement them as needed. For now, we'll keep them commented out:
  
  // Settings Operations - NOT IMPLEMENTED YET
  // async getSettings(): Promise<OASISResult<any>> {
  //   const response = await api.get('/star/settings');
  //   return response.data;
  // },

  // async updateSettings(settings: any): Promise<OASISResult<any>> {
  //   const response = await api.put('/star/settings', settings);
  //   return response.data;
  // },

  // Store Operations - NOT IMPLEMENTED YET
  // async getStoreItems(): Promise<OASISResult<any[]>> {
  //   const response = await api.get('/star/store');
  //   return response.data;
  // },

  // Libraries Operations - Using WEB5 STAR Web API
  async getAllLibraries(): Promise<OASISResult<any[]>> {
    try {
      const response = await api.get('/libraries');
      console.log('Libraries API Response:', response.data);
      return response.data;
    } catch (error) {
      console.error('Error fetching Libraries from API:', error);
      // Return demo data as fallback
      return {
        result: [
          {
            id: '1',
            name: 'STAR Core Library',
            description: 'Core functionality for STAR applications',
            version: '2.1.0',
            category: 'Core',
            downloads: 15420,
            rating: 4.9,
            lastUpdated: '2024-01-15T10:30:00Z'
          },
          {
            id: '2',
            name: 'Quantum Physics Engine',
            description: 'Advanced quantum mechanics simulation library',
            version: '1.8.3',
            category: 'Science',
            downloads: 8930,
            rating: 4.7,
            lastUpdated: '2024-01-12T14:20:00Z'
          }
        ],
        isError: false,
        message: 'Demo libraries loaded (API unavailable)'
      };
    }
  },

  // Plugins Operations - Using WEB5 STAR Web API
  async getAllPlugins(): Promise<OASISResult<any[]>> {
    try {
      const response = await api.get('/plugins');
      console.log('Plugins API Response:', response.data);
      return response.data;
    } catch (error) {
      console.error('Error fetching Plugins from API:', error);
      // Return demo data as fallback
      return {
        result: [
          {
            id: '1',
            name: 'Neural Network Plugin',
            description: 'Advanced AI neural network processing',
            version: '3.2.1',
            category: 'AI',
            downloads: 12500,
            rating: 4.8,
            lastUpdated: '2024-01-14T09:15:00Z'
          },
          {
            id: '2',
            name: 'Blockchain Integration',
            description: 'Seamless blockchain connectivity',
            version: '2.5.0',
            category: 'Blockchain',
            downloads: 9800,
            rating: 4.6,
            lastUpdated: '2024-01-10T16:45:00Z'
          }
        ],
        isError: false,
        message: 'Demo plugins loaded (API unavailable)'
      };
    }
  },

  // Runtimes Operations - Using WEB5 STAR Web API
  async getAllRuntimes(): Promise<OASISResult<any[]>> {
    try {
      const response = await api.get('/runtimes');
      console.log('Runtimes API Response:', response.data);
      return response.data;
    } catch (error) {
      console.error('Error fetching Runtimes from API:', error);
      // Return demo data as fallback
      return {
        result: [
          {
            id: '1',
            name: 'Quantum Runtime',
            description: 'High-performance quantum computing runtime',
            version: '4.1.0',
            category: 'Quantum',
            status: 'Running',
            uptime: '7d 12h 30m',
            lastUpdated: '2024-01-15T08:00:00Z'
          },
          {
            id: '2',
            name: 'Neural Processing Runtime',
            description: 'Optimized for AI/ML workloads',
            version: '3.7.2',
            category: 'AI',
            status: 'Stopped',
            uptime: '0d 0h 0m',
            lastUpdated: '2024-01-13T20:30:00Z'
          }
        ],
        isError: false,
        message: 'Demo runtimes loaded (API unavailable)'
      };
    }
  },

  // Templates Operations - Using WEB5 STAR Web API
  async getAllTemplates(): Promise<OASISResult<any[]>> {
    try {
      const response = await api.get('/templates');
      console.log('Templates API Response:', response.data);
      return response.data;
    } catch (error) {
      console.error('Error fetching Templates from API:', error);
      // Return demo data as fallback
      return {
        result: [
          {
            id: '1',
            name: 'STAR Application Template',
            description: 'Complete template for STAR applications',
            version: '2.0.0',
            category: 'Application',
            downloads: 5600,
            rating: 4.9,
            lastUpdated: '2024-01-11T11:20:00Z'
          },
          {
            id: '2',
            name: 'Quantum Algorithm Template',
            description: 'Template for quantum computing algorithms',
            version: '1.5.3',
            category: 'Quantum',
            downloads: 3200,
            rating: 4.7,
            lastUpdated: '2024-01-09T15:10:00Z'
          }
        ],
        isError: false,
        message: 'Demo templates loaded (API unavailable)'
      };
    }
  },

  // My Data Operations - Using WEB4 OASIS API
  async getMyDataFiles(): Promise<OASISResult<any>> {
    try {
      const response = await web4Api.get('/data/get-my-data-files');
      console.log('My Data Files API Response:', response.data);
      return response.data;
    } catch (error) {
      console.error('Error fetching My Data Files from API:', error);
      // Return demo data as fallback
      return {
        result: [
          {
            id: '1',
            name: 'Project Alpha Documentation',
            type: 'document',
            size: '2.4 MB',
            lastModified: '2024-01-15T14:30:00Z',
            permissions: 'private',
            storageNodes: ['Node-1', 'Node-3'],
            replication: 3
          },
          {
            id: '2',
            name: 'Quantum Algorithm Research',
            type: 'research',
            size: '15.7 MB',
            lastModified: '2024-01-14T09:15:00Z',
            permissions: 'shared',
            storageNodes: ['Node-2', 'Node-4', 'Node-5'],
            replication: 5
          }
        ],
        isError: false,
        message: 'Demo my data files loaded (API unavailable)'
      };
    }
  },

  // Settings Operations - Using WEB4 OASIS API
  async getSettings(): Promise<OASISResult<any>> {
    try {
      const response = await web4Api.get('/avatar/get-settings');
      console.log('Settings API Response:', response.data);
      return response.data;
    } catch (error) {
      console.error('Error fetching Settings from API:', error);
      // Return demo data as fallback
      return {
        result: {
          theme: 'dark',
          language: 'en',
          notifications: true,
          autoSave: true,
          privacy: 'private',
          dataReplication: 3,
          preferredStorageNodes: ['Node-1', 'Node-2', 'Node-3']
        },
        isError: false,
        message: 'Demo settings loaded (API unavailable)'
      };
    }
  },

  async updateSettings(settings: any): Promise<OASISResult<any>> {
    try {
      const response = await web4Api.put('/avatar/update-settings', settings);
      console.log('Update Settings API Response:', response.data);
      return response.data;
    } catch (error) {
      console.error('Error updating Settings from API:', error);
      // Return success for demo purposes
      return {
        result: settings,
        isError: false,
        message: 'Settings updated successfully (Demo Mode)'
      };
    }
  },

  // Store Operations - Using WEB5 STAR Web API
  async getStoreItems(): Promise<OASISResult<any[]>> {
    try {
      const response = await api.get('/store');
      console.log('Store Items API Response:', response.data);
      return response.data;
    } catch (error) {
      console.error('Error fetching Store Items from API:', error);
      // Return demo data as fallback
      return {
        result: [
          {
            id: '1',
            name: 'Quantum Processing Unit',
            description: 'Advanced quantum computing processor',
            price: 2500,
            currency: 'HERZ',
            category: 'Hardware',
            rating: 4.9,
            inStock: true,
            image: 'https://images.unsplash.com/photo-1518709268805-4e9042af2176?w=300&h=200&fit=crop'
          },
          {
            id: '2',
            name: 'Neural Network Library',
            description: 'Premium AI neural network components',
            price: 150,
            currency: 'HERZ',
            category: 'Software',
            rating: 4.7,
            inStock: true,
            image: 'https://images.unsplash.com/photo-1555949963-aa79dcee981c?w=300&h=200&fit=crop'
          }
        ],
        isError: false,
        message: 'Demo store items loaded (API unavailable)'
      };
    }
  },

  // async uploadFile(file: File, options?: any): Promise<OASISResult<any>> {
  //   const formData = new FormData();
  //   formData.append('file', file);
  //   if (options) {
  //     formData.append('options', JSON.stringify(options));
  //   }
  //   const response = await api.post('/star/my-data/upload', formData, {
  //     headers: {
  //       'Content-Type': 'multipart/form-data',
  //     },
  //   });
  //   return response.data;
  // },

  // async deleteFile(id: string): Promise<OASISResult<boolean>> {
  //   const response = await api.delete(`/star/my-data/files/${id}`);
  //   return response.data;
  // },

  // async updateFilePermissions(id: string, permissions: any): Promise<OASISResult<boolean>> {
  //   const response = await api.put(`/star/my-data/files/${id}/permissions`, permissions);
  //   return response.data;
  // },
};
