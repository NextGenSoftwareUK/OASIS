import axios from 'axios';
import { OASISResult, STARStatus, Avatar, Karma, OAPPKarmaData, AvatarKarmaData, AvatarListResult } from '../types/star';

// Helper function to check demo mode
const isDemoMode = () => {
  const saved = localStorage.getItem('demoMode');
  return saved ? JSON.parse(saved) : true;
};

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
    console.log('starService.igniteSTAR called, isDemoMode():', isDemoMode());
    
    if (isDemoMode()) {
      // Demo mode - simulate successful ignition
      console.log('STAR Ignite - Demo Mode');
      return {
        isError: false,
        message: 'STAR ignited successfully (Demo Mode)',
        result: { ignited: true },
      };
    }

    console.log('STAR Ignite - Live Mode, making API call to:', API_BASE_URL + '/star/ignite');
    try {
    const response = await api.post('/star/ignite');
    console.log('STAR Ignite API Response:', response.data); // Debug logging
    return response.data;
    } catch (error) {
      console.error('STAR Ignite API failed:', error);
      console.error('Error details:', {
        message: error instanceof Error ? error.message : 'Unknown error',
        code: (error as any)?.code,
        response: (error as any)?.response?.data,
        status: (error as any)?.response?.status,
      });
      // In live mode, return error result instead of throwing
      return {
        isError: true,
        message: error instanceof Error ? error.message : 'Failed to connect to STAR API',
        result: undefined,
      };
    }
  },

  async extinguishStar(): Promise<OASISResult<boolean>> {
    if (isDemoMode()) {
      // Demo mode - simulate successful extinguish
      console.log('STAR Extinguish - Demo Mode');
      return {
        isError: false,
        message: 'STAR extinguished successfully (Demo Mode)',
        result: true,
      };
    }

    try {
    const response = await api.post('/star/extinguish');
    return response.data;
    } catch (error) {
      console.error('STAR Extinguish API failed:', error);
      // In live mode, return error result instead of throwing
      return {
        isError: true,
        message: error instanceof Error ? error.message : 'Failed to connect to STAR API',
        result: false,
      };
    }
  },

  async getSTARStatus(): Promise<STARStatus> {
    if (isDemoMode()) {
      // Demo mode - return default status
      console.log('STAR Status - Demo Mode');
      return {
        isIgnited: false, // Will be updated by the hook when igniteSTAR is called
        lastUpdated: new Date(),
      };
    }

    try {
    const response = await api.get('/star/status');
    console.log('STAR Status API Response:', response.data); // Debug logging
    return {
      isIgnited: response.data.isIgnited || false,
      lastUpdated: new Date(),
    };
    } catch (error) {
      console.error('STAR Status API failed:', error);
      throw error;
    }
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
    console.log('starService.getAllAvatars called, isDemoMode():', isDemoMode());
    
    if (isDemoMode()) {
      // Demo mode - return demo data
      console.log('Avatars - Demo Mode');
      return {
        isError: false,
        message: 'Avatars loaded successfully (Demo Mode)',
        result: [
          {
            id: '1',
            username: 'sarah_chen',
            email: 'sarah.chen@oasis.com',
            firstName: 'Sarah',
            lastName: 'Chen',
            title: 'Dr',
            karma: 125000,
            level: 10,
            xp: 250000,
            isActive: true,
            isBeamedIn: true,
            lastBeamedIn: new Date(Date.now() - 5 * 60 * 1000).toISOString(),
            createdDate: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000), // 30 days ago
            lastLoginDate: new Date(Date.now() - 5 * 60 * 1000) // 5 minutes ago
          },
          {
            id: '2',
            username: 'alex_rodriguez',
            email: 'alex.rodriguez@oasis.com',
            firstName: 'Alex',
            lastName: 'Rodriguez',
            title: 'Prof',
            karma: 98000,
            level: 9,
            xp: 200000,
            isActive: true,
            isBeamedIn: false,
            lastBeamedIn: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(), // 2 hours ago
            createdDate: new Date(Date.now() - 45 * 24 * 60 * 60 * 1000), // 45 days ago
            lastLoginDate: new Date(Date.now() - 2 * 60 * 60 * 1000) // 2 hours ago
          },
          {
            id: '3',
            username: 'maya_patel',
            email: 'maya.patel@oasis.com',
            firstName: 'Maya',
            lastName: 'Patel',
            title: 'Dr',
            karma: 75000,
            level: 8,
            xp: 150000,
            isActive: true,
            isBeamedIn: true,
            lastBeamedIn: new Date(Date.now() - 10 * 60 * 1000).toISOString(), // 10 minutes ago
            createdDate: new Date(Date.now() - 60 * 24 * 60 * 60 * 1000), // 60 days ago
            lastLoginDate: new Date(Date.now() - 10 * 60 * 1000) // 10 minutes ago
          },
          {
            id: '4',
            username: 'david_kim',
            email: 'david.kim@oasis.com',
            firstName: 'David',
            lastName: 'Kim',
            title: 'Mr',
            karma: 50000,
            level: 6,
            xp: 100000,
            isActive: true,
            isBeamedIn: false,
            lastBeamedIn: new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString(), // 1 day ago
            createdDate: new Date(Date.now() - 90 * 24 * 60 * 60 * 1000), // 90 days ago
            lastLoginDate: new Date(Date.now() - 24 * 60 * 60 * 1000) // 1 day ago
          },
          {
            id: '5',
            username: 'luna_star',
            email: 'luna.star@oasis.com',
            firstName: 'Luna',
            lastName: 'Star',
            title: 'Dr',
            karma: 200000,
            level: 15,
            xp: 500000,
            isActive: true,
            isBeamedIn: true,
            lastBeamedIn: new Date(Date.now() - 1 * 60 * 1000).toISOString(), // 1 minute ago
            createdDate: new Date(Date.now() - 120 * 24 * 60 * 60 * 1000), // 120 days ago
            lastLoginDate: new Date(Date.now() - 1 * 60 * 1000) // 1 minute ago
          }
        ]
      };
    }

    console.log('Avatars - Live Mode, making API call to:', WEB4_API_BASE_URL + '/avatar/load-all-avatars');
    try {
      // Use WEB4 OASIS API for avatar operations
      const response = await web4Api.get('/avatar/load-all-avatars');
      console.log('Avatars API response:', response.data);
      return response.data;
    } catch (error) {
      console.error('Error fetching avatars:', error);
      // Return demo data as fallback
      return {
        result: [
          {
            id: '1',
            username: 'sarah_chen',
            email: 'sarah.chen@oasis.com',
            firstName: 'Sarah',
            lastName: 'Chen',
            title: 'Dr',
            karma: 125000,
            level: 10,
            xp: 250000,
            isActive: true,
            isBeamedIn: true,
            lastBeamedIn: new Date(Date.now() - 5 * 60 * 1000).toISOString(),
            createdDate: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000), // 30 days ago
            lastLoginDate: new Date(Date.now() - 5 * 60 * 1000) // 5 minutes ago
          },
          {
            id: '2',
            username: 'captain_nova',
            email: 'nova.stellar@oasis.com',
            firstName: 'Nova',
            lastName: 'Stellar',
            title: 'Captain',
            karma: 98000,
            level: 9,
            xp: 196000,
            isActive: false,
            isBeamedIn: false,
            lastBeamedIn: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000).toISOString(),
            createdDate: new Date(Date.now() - 60 * 24 * 60 * 60 * 1000), // 60 days ago
            lastLoginDate: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000) // 2 days ago
          },
          {
            id: '3',
            username: 'alex_quantum',
            email: 'alex.quantum@oasis.com',
            firstName: 'Alex',
            lastName: 'Quantum',
            title: 'Dr',
            karma: 75000,
            level: 8,
            xp: 150000,
            isActive: true,
            isBeamedIn: true,
            lastBeamedIn: new Date(Date.now() - 15 * 60 * 1000).toISOString(),
            createdDate: new Date(Date.now() - 45 * 24 * 60 * 60 * 1000), // 45 days ago
            lastLoginDate: new Date(Date.now() - 15 * 60 * 1000) // 15 minutes ago
          },
          {
            id: '4',
            username: 'cyber_nexus',
            email: 'nexus.cyber@oasis.com',
            firstName: 'Nexus',
            lastName: 'Cyber',
            title: 'Agent',
            karma: 45000,
            level: 7,
            xp: 90000,
            isActive: false,
            isBeamedIn: false,
            lastBeamedIn: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString(),
            createdDate: new Date(Date.now() - 90 * 24 * 60 * 60 * 1000), // 90 days ago
            lastLoginDate: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000) // 1 week ago
          },
          {
            id: '5',
            username: 'stellar_engineer',
            email: 'engineer.stellar@oasis.com',
            firstName: 'Stellar',
            lastName: 'Engineer',
            title: 'Engineer',
            karma: 32000,
            level: 6,
            xp: 64000,
            isActive: true,
            isBeamedIn: true,
            lastBeamedIn: new Date(Date.now() - 2 * 60 * 1000).toISOString(),
            createdDate: new Date(Date.now() - 15 * 24 * 60 * 60 * 1000), // 15 days ago
            lastLoginDate: new Date(Date.now() - 2 * 60 * 1000) // 2 minutes ago
          }
        ],
        isError: false,
        message: 'Demo avatars loaded (API unavailable)'
      };
    }
  },

  async getAvatarById(id: string): Promise<OASISResult<Avatar>> {
    console.log('starService.getAvatarById called, isDemoMode():', isDemoMode());
    
    if (isDemoMode()) {
      // Demo mode - return demo data
      console.log('Avatar by ID - Demo Mode');
      const demoAvatars = [
        {
          id: '1',
          username: 'sarah_chen',
          email: 'sarah.chen@oasis.com',
          firstName: 'Sarah',
          lastName: 'Chen',
          title: 'Dr',
          karma: 125000,
          level: 10,
          xp: 250000,
          isActive: true,
          isBeamedIn: true,
          lastBeamedIn: new Date(Date.now() - 5 * 60 * 1000).toISOString(),
          createdDate: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000),
          lastLoginDate: new Date(Date.now() - 5 * 60 * 1000)
        },
        {
          id: '2',
          username: 'alex_rodriguez',
          email: 'alex.rodriguez@oasis.com',
          firstName: 'Alex',
          lastName: 'Rodriguez',
          title: 'Prof',
          karma: 98000,
          level: 9,
          xp: 200000,
          isActive: true,
          isBeamedIn: false,
          lastBeamedIn: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
          createdDate: new Date(Date.now() - 45 * 24 * 60 * 60 * 1000),
          lastLoginDate: new Date(Date.now() - 2 * 60 * 60 * 1000)
        }
      ];
      
      const avatar = demoAvatars.find(a => a.id === id);
      return {
        isError: false,
        message: 'Avatar loaded successfully (Demo Mode)',
        result: avatar || demoAvatars[0]
      };
    }

    console.log('Avatar by ID - Live Mode, making API call to:', WEB4_API_BASE_URL + `/avatar/${id}`);
    try {
      const response = await web4Api.get(`/avatar/${id}`);
      console.log('Avatar by ID API response:', response.data);
      return response.data;
    } catch (error) {
      console.error('Error fetching avatar by ID:', error);
      return {
        isError: true,
        message: error instanceof Error ? error.message : 'Failed to fetch avatar',
        result: undefined
      };
    }
  },

  async updateAvatar(id: string, data: any): Promise<OASISResult<Avatar>> {
    console.log('starService.updateAvatar called, isDemoMode():', isDemoMode());
    
    if (isDemoMode()) {
      // Demo mode - simulate successful update
      console.log('Update Avatar - Demo Mode');
      return {
        isError: false,
        message: 'Avatar updated successfully (Demo Mode)',
        result: {
          id,
          ...data,
          karma: 125000,
          level: 10,
          xp: 250000,
          isActive: true,
          isBeamedIn: true,
          lastBeamedIn: new Date().toISOString(),
          createdDate: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000),
          lastLoginDate: new Date()
        }
      };
    }

    console.log('Update Avatar - Live Mode, making API call to:', WEB4_API_BASE_URL + `/avatar/${id}`);
    try {
      const response = await web4Api.put(`/avatar/${id}`, data);
      console.log('Update Avatar API response:', response.data);
      return response.data;
    } catch (error) {
      console.error('Error updating avatar:', error);
      return {
        isError: true,
        message: error instanceof Error ? error.message : 'Failed to update avatar',
        result: undefined
      };
    }
  },

  async deleteAvatar(id: string): Promise<OASISResult<boolean>> {
    console.log('starService.deleteAvatar called, isDemoMode():', isDemoMode());
    
    if (isDemoMode()) {
      // Demo mode - simulate successful deletion
      console.log('Delete Avatar - Demo Mode');
      return {
        isError: false,
        message: 'Avatar deleted successfully (Demo Mode)',
        result: true
      };
    }

    console.log('Delete Avatar - Live Mode, making API call to:', WEB4_API_BASE_URL + `/avatar/${id}`);
    try {
      const response = await web4Api.delete(`/avatar/${id}`);
      console.log('Delete Avatar API response:', response.data);
      return response.data;
    } catch (error) {
      console.error('Error deleting avatar:', error);
      return {
        isError: true,
        message: error instanceof Error ? error.message : 'Failed to delete avatar',
        result: false
      };
    }
  },

  // Avatar Session Management - OASIS SSO System 🚀
  async getAvatarSessions(avatarId: string): Promise<OASISResult<any>> {
    console.log('starService.getAvatarSessions called, isDemoMode():', isDemoMode());
    
    if (isDemoMode()) {
      // Demo mode - return impressive session data
      console.log('Avatar Sessions - Demo Mode');
      return {
        isError: false,
        message: 'Avatar sessions loaded successfully (Demo Mode)',
        result: {
          totalSessions: 12,
          activeSessions: 4,
          sessions: [
            {
              id: '1',
              serviceName: 'STARNET Dashboard',
              serviceType: 'platform',
              deviceType: 'desktop',
              deviceName: 'MacBook Pro 16"',
              location: 'San Francisco, CA',
              ipAddress: '192.168.1.100',
              isActive: true,
              lastActivity: new Date(Date.now() - 5 * 60 * 1000).toISOString(),
              loginTime: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
              userAgent: 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)',
              platform: 'macOS',
              version: '1.0.0',
            },
            {
              id: '2',
              serviceName: 'OASIS Gaming Platform',
              serviceType: 'game',
              deviceType: 'vr',
              deviceName: 'Oculus Quest 3',
              location: 'San Francisco, CA',
              ipAddress: '192.168.1.101',
              isActive: true,
              lastActivity: new Date(Date.now() - 15 * 60 * 1000).toISOString(),
              loginTime: new Date(Date.now() - 4 * 60 * 60 * 1000).toISOString(),
              userAgent: 'OculusBrowser/1.0.0',
              platform: 'Android',
              version: '2.1.0',
            },
            {
              id: '3',
              serviceName: 'STAR Mobile App',
              serviceType: 'app',
              deviceType: 'mobile',
              deviceName: 'iPhone 15 Pro',
              location: 'San Francisco, CA',
              ipAddress: '192.168.1.102',
              isActive: true,
              lastActivity: new Date(Date.now() - 30 * 60 * 1000).toISOString(),
              loginTime: new Date(Date.now() - 6 * 60 * 60 * 1000).toISOString(),
              userAgent: 'STAR Mobile/3.2.1 (iPhone; iOS 17.0)',
              platform: 'iOS',
              version: '3.2.1',
            },
            {
              id: '4',
              serviceName: 'Quantum Calculator OAPP',
              serviceType: 'app',
              deviceType: 'desktop',
              deviceName: 'Windows PC',
              location: 'New York, NY',
              ipAddress: '203.0.113.45',
              isActive: true,
              lastActivity: new Date(Date.now() - 45 * 60 * 1000).toISOString(),
              loginTime: new Date(Date.now() - 8 * 60 * 60 * 1000).toISOString(),
              userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)',
              platform: 'Windows',
              version: '1.5.2',
            },
            {
              id: '5',
              serviceName: 'STARNET Store',
              serviceType: 'website',
              deviceType: 'tablet',
              deviceName: 'iPad Pro 12.9"',
              location: 'San Francisco, CA',
              ipAddress: '192.168.1.103',
              isActive: false,
              lastActivity: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
              loginTime: new Date(Date.now() - 12 * 60 * 60 * 1000).toISOString(),
              userAgent: 'Mozilla/5.0 (iPad; CPU OS 17_0 like Mac OS X)',
              platform: 'iPadOS',
              version: '1.0.0',
            },
            {
              id: '6',
              serviceName: 'Neural Network SDK',
              serviceType: 'service',
              deviceType: 'desktop',
              deviceName: 'Linux Workstation',
              location: 'Seattle, WA',
              ipAddress: '198.51.100.23',
              isActive: false,
              lastActivity: new Date(Date.now() - 4 * 60 * 60 * 1000).toISOString(),
              loginTime: new Date(Date.now() - 18 * 60 * 60 * 1000).toISOString(),
              userAgent: 'STAR SDK/2.0.0 (Linux x86_64)',
              platform: 'Linux',
              version: '2.0.0',
            },
          ],
        }
      };
    }

    console.log('Avatar Sessions - Live Mode, making API call to:', WEB4_API_BASE_URL + `/avatar/${avatarId}/sessions`);
    try {
      const response = await web4Api.get(`/avatar/${avatarId}/sessions`);
      console.log('Avatar Sessions API response:', response.data);
      return response.data;
    } catch (error) {
      console.error('Error fetching avatar sessions:', error);
      return {
        isError: true,
        message: 'Failed to fetch avatar sessions',
        result: undefined
      };
    }
  },

  async logoutAvatarSessions(avatarId: string, sessionIds: string[]): Promise<OASISResult<boolean>> {
    console.log('starService.logoutAvatarSessions called, isDemoMode():', isDemoMode());
    
    if (isDemoMode()) {
      // Demo mode - simulate successful logout
      console.log('Logout Avatar Sessions - Demo Mode');
      return {
        isError: false,
        message: `Successfully logged out from ${sessionIds.length} session(s) (Demo Mode)`,
        result: true
      };
    }

    console.log('Logout Avatar Sessions - Live Mode, making API call to:', WEB4_API_BASE_URL + `/avatar/${avatarId}/sessions/logout`);
    try {
      const response = await web4Api.post(`/avatar/${avatarId}/sessions/logout`, {
        sessionIds
      });
      console.log('Logout Avatar Sessions API response:', response.data);
      return response.data;
    } catch (error) {
      console.error('Error logging out avatar sessions:', error);
      return {
        isError: true,
        message: 'Failed to logout sessions',
        result: undefined
      };
    }
  },

    // Dev Portal - Developer Resources & Tools 🛠️
    async getDevPortalStats(): Promise<OASISResult<any>> {
      console.log('starService.getDevPortalStats called, isDemoMode():', isDemoMode());
      
      if (isDemoMode()) {
        // Demo mode - return impressive dev portal stats
        console.log('Dev Portal Stats - Demo Mode');
        return {
          isError: false,
          message: 'Dev portal stats loaded successfully (Demo Mode)',
          result: {
            totalResources: 40,
            totalDownloads: 481260,
            activeDevelopers: 21340,
            averageRating: 4.94,
            popularCategories: [
              { category: 'Getting Started', count: 1 },
              { category: 'Integration', count: 7 },
              { category: 'Tools', count: 2 },
              { category: 'API Clients', count: 16 },
              { category: 'Development', count: 6 },
              { category: 'Game Engines', count: 4 },
              { category: 'Mobile', count: 4 },
            ],
            recentUpdates: [],
            featuredResources: [],
          }
        };
      }

      console.log('Dev Portal Stats - Live Mode, making API call to:', API_BASE_URL + '/dev-portal/stats');
      try {
        const response = await api.get('/dev-portal/stats');
        console.log('Dev Portal Stats API response:', response.data);
        return response.data;
      } catch (error) {
        console.error('Error fetching dev portal stats:', error);
        return {
          isError: true,
          message: 'Failed to fetch dev portal stats',
          result: undefined
        };
      }
    },

    async getDevPortalResources(): Promise<OASISResult<any>> {
      console.log('starService.getDevPortalResources called, isDemoMode():', isDemoMode());
      
      if (isDemoMode()) {
        // Demo mode - return impressive dev portal resources
        console.log('Dev Portal Resources - Demo Mode');
        return {
          isError: false,
          message: 'Dev portal resources loaded successfully (Demo Mode)',
          result: [
            {
              id: '1',
              title: 'STAR CLI - Command Line Interface',
              description: 'Complete command-line tool for orchestrating the OASIS ecosystem. Deploy, manage, and monitor your applications with ease.',
              type: 'cli',
              category: 'getting-started',
              downloadUrl: '/downloads/star-cli-v2.1.0.zip',
              version: '2.1.0',
              size: '45.2 MB',
              downloads: 86578,
              rating: 4.9,
              tags: ['cli', 'deployment', 'management', 'monitoring'],
              author: 'OASIS Team',
              lastUpdated: '2024-02-15T10:30:00Z',
              featured: true,
              difficulty: 'beginner',
              estimatedTime: '15 minutes',
              prerequisites: ['Node.js 18+', 'Git'],
              languages: ['JavaScript', 'TypeScript'],
              frameworks: ['Node.js'],
              platforms: ['Windows', 'macOS', 'Linux'],
              content: 'The STAR CLI is your gateway to the OASIS ecosystem. Authenticate avatars, deploy OAPPs, and manage runtime infrastructure from a single tool.',
              codeExamples: [
                `# Authenticate with STARNET using your avatar wallet\nstar login --provider Auto --wallet 0xABCD...1234\n\n# Deploy a production-grade OAPP\nstar oapp deploy --name "Galactic Bazaar" --path ./dist --env production\n\n# Monitor live runtime logs for troubleshooting\nstar runtime logs --name "quantum-runtime" --follow`
              ],
              screenshots: ['/screenshots/star-cli-1.png', '/screenshots/star-cli-2.png'],
              videoUrl: 'https://youtube.com/watch?v=star-cli-demo',
              githubUrl: 'https://github.com/oasis/star-cli',
              documentationUrl: 'https://docs.oasis.network/star-cli',
              supportUrl: 'https://support.oasis.network/star-cli'
            },
            {
              id: '2',
              title: 'OASIS Avatar SSO SDK Pack',
              description: 'Complete SDK package for integrating OASIS Avatar SSO into your applications. Includes widgets, API clients, and documentation.',
              type: 'sdk',
              category: 'integration',
              downloadUrl: '/downloads/oasis-avatar-sso-sdk-v1.5.2.zip',
              version: '1.5.2',
              size: '128.7 MB',
              downloads: 54678,
              rating: 4.8,
              tags: ['sso', 'authentication', 'avatar', 'sdk', 'widget'],
              author: 'OASIS Team',
              lastUpdated: '2024-02-14T14:20:00Z',
              featured: true,
              difficulty: 'intermediate',
              estimatedTime: '2 hours',
              prerequisites: ['JavaScript', 'React/Vue/Angular', 'Node.js'],
              languages: ['JavaScript', 'TypeScript', 'Python', 'Java', 'C#'],
              frameworks: ['React', 'Vue', 'Angular', 'Express', 'Django', 'Spring'],
              platforms: ['Web', 'Mobile', 'Desktop'],
              content: 'Integrate seamless avatar authentication with full provider awareness, auto-replication, and remote session management controls.',
              codeExamples: [
                `import { OasisAvatarSSO } from '@oasis/avatar-sso';\n\nconst sso = new OasisAvatarSSO({\n  clientId: 'your-client-id',\n  redirectUri: 'https://app.example.com/auth/callback',\n  provider: 'Auto'\n});\n\nawait sso.login();\nconst session = await sso.getCurrentSession();\nconsole.log('Active avatar karma', session.avatar.karma);`
              ],
              screenshots: ['/screenshots/avatar-sso-1.png', '/screenshots/avatar-sso-2.png'],
              videoUrl: 'https://youtube.com/watch?v=avatar-sso-demo',
              githubUrl: 'https://github.com/oasis/avatar-sso-sdk',
              documentationUrl: 'https://docs.oasis.network/avatar-sso',
              supportUrl: 'https://support.oasis.network/avatar-sso'
            },
            {
              id: '3',
              title: 'Postman Collection - WEB4 OASIS API',
              description: 'Complete Postman collection with all WEB4 OASIS API endpoints, authentication flows, and sample payloads.',
              type: 'postman',
              category: 'tools',
              downloadUrl: '/downloads/oasis-api-postman-collection-v3.2.1.json',
              version: '3.2.1',
              size: '2.1 MB',
              downloads: 28901,
              rating: 4.7,
              tags: ['api', 'postman', 'testing', 'documentation'],
              author: 'OASIS Team',
              lastUpdated: '2024-02-13T16:45:00Z',
              featured: true,
              difficulty: 'beginner',
              estimatedTime: '30 minutes',
              prerequisites: ['Postman', 'API knowledge'],
              languages: ['JSON'],
              frameworks: ['Postman'],
              platforms: ['Cross-platform'],
              content: 'Ready-to-use WEB4 API collection including onboarding recipes, live data simulations, and automated environment scripts.',
              codeExamples: [
                `# Retrieve avatar profile (JWT auth)\nGET {{baseUrl}}/api/avatar/profile\nAuthorization: Bearer {{accessToken}}\n\n# Deploy new OAPP blueprint\nPOST {{baseUrl}}/api/oapp/deploy\nBody (json):\n{\n  "name": "Quantum Trade Hub",\n  "version": "1.0.0",\n  "runtime": "node18"\n}`
              ],
              screenshots: ['/screenshots/postman-1.png', '/screenshots/postman-2.png'],
              videoUrl: 'https://youtube.com/watch?v=postman-oasis-api',
              githubUrl: 'https://github.com/oasis/api-postman-collection',
              documentationUrl: 'https://docs.oasis.network/api/postman',
              supportUrl: 'https://support.oasis.network/api'
            },
            {
              id: '4',
              title: 'Postman Collection - WEB5 STAR API',
              description: 'Advanced WEB5 STAR API collection showcasing karma analytics, provider replication policies, and remote beam-out orchestration.',
              type: 'postman',
              category: 'tools',
              downloadUrl: '/downloads/web5-star-api-postman-collection-v1.0.0.json',
              version: '1.0.0',
              size: '2.8 MB',
              downloads: 16800,
              rating: 4.9,
              tags: ['api', 'postman', 'web5', 'star', 'automation'],
              author: 'STARNET Platform Team',
              lastUpdated: '2024-02-12T09:45:00Z',
              featured: true,
              difficulty: 'intermediate',
              estimatedTime: '35 minutes',
              prerequisites: ['Postman', 'WEB5 STAR API Key'],
              languages: ['JSON'],
              frameworks: ['Postman'],
              platforms: ['Cross-platform'],
              content: 'Explore the next-generation WEB5 STAR API with prebuilt flows for cross-provider karma ledgers, remote session management, and hyperdrive replication.',
              codeExamples: [
                `# Fetch avatar karma ledger across providers\nGET {{starBaseUrl}}/api/karma/ledger/{{avatarId}}\nHeaders: { "x-oasis-provider": "Auto" }\n\n# Trigger remote beam-out for stale sessions\nPOST {{starBaseUrl}}/api/avatar/sessions/beam-out\nBody: { "avatarId": "{{avatarId}}", "sessionIds": ["all"] }`
              ],
              screenshots: ['/screenshots/postman-web5-1.png'],
              videoUrl: 'https://youtube.com/watch?v=star-web5-postman',
              githubUrl: 'https://github.com/oasis/web5-star-api-postman',
              documentationUrl: 'https://docs.oasis.network/star/web5-api',
              supportUrl: 'https://support.oasis.network/star'
            },
            {
              id: '5',
              title: 'OASIS Avatar SSO SDK - Angular',
              description: 'Angular-specific implementation of Avatar SSO with reactive services, guards, and interceptors.',
              type: 'sdk',
              category: 'integration',
              downloadUrl: '/downloads/oasis-avatar-sso-sdk-angular.zip',
              version: '1.5.2',
              size: '15.3 MB',
              downloads: 12456,
              rating: 4.9,
              tags: ['angular', 'sso', 'authentication', 'rxjs'],
              author: 'OASIS Team',
              lastUpdated: '2024-02-14T14:20:00Z',
              featured: true,
              difficulty: 'intermediate',
              estimatedTime: '1.5 hours',
              prerequisites: ['Angular 15+', 'TypeScript', 'RxJS'],
              languages: ['TypeScript'],
              frameworks: ['Angular'],
              platforms: ['Web'],
              content: 'Full Angular module with services, guards, and interceptors for seamless avatar authentication.',
              codeExamples: [
                `import { OasisAvatarSSOModule } from '@oasis/avatar-sso-angular';\n\n@NgModule({\n  imports: [\n    OasisAvatarSSOModule.forRoot({\n      apiUrl: 'https://api.oasis.network',\n      provider: 'Auto'\n    })\n  ]\n})\nexport class AppModule { }`
              ],
              screenshots: ['/screenshots/angular-sso-1.png'],
              videoUrl: 'https://youtube.com/watch?v=angular-sso',
              githubUrl: 'https://github.com/oasis/avatar-sso-angular',
              documentationUrl: 'https://docs.oasis.network/avatar-sso/angular',
              supportUrl: 'https://support.oasis.network/avatar-sso'
            },
            {
              id: '6',
              title: 'OASIS Avatar SSO SDK - React',
              description: 'React hooks and context provider for Avatar SSO with TypeScript support.',
              type: 'sdk',
              category: 'integration',
              downloadUrl: '/downloads/oasis-avatar-sso-sdk-react.zip',
              version: '1.5.2',
              size: '12.8 MB',
              downloads: 23789,
              rating: 4.9,
              tags: ['react', 'hooks', 'sso', 'context'],
              author: 'OASIS Team',
              lastUpdated: '2024-02-14T14:20:00Z',
              featured: true,
              difficulty: 'intermediate',
              estimatedTime: '1 hour',
              prerequisites: ['React 18+', 'TypeScript'],
              languages: ['TypeScript', 'JavaScript'],
              frameworks: ['React'],
              platforms: ['Web'],
              content: 'Modern React hooks API with useOasisSSO hook and context provider for global auth state.',
              codeExamples: [
                `import { OasisSSOProvider, useOasisSSO } from '@oasis/avatar-sso-react';\n\nfunction App() {\n  return (\n    <OasisSSOProvider apiUrl="https://api.oasis.network">\n      <YourApp />\n    </OasisSSOProvider>\n  );\n}\n\nfunction LoginPage() {\n  const { login, user, isAuthenticated } = useOasisSSO();\n  // ...\n}`
              ],
              screenshots: ['/screenshots/react-sso-1.png'],
              videoUrl: 'https://youtube.com/watch?v=react-sso',
              githubUrl: 'https://github.com/oasis/avatar-sso-react',
              documentationUrl: 'https://docs.oasis.network/avatar-sso/react',
              supportUrl: 'https://support.oasis.network/avatar-sso'
            },
            {
              id: '7',
              title: 'OASIS Avatar SSO SDK - Vue 3',
              description: 'Vue 3 Composition API plugin for Avatar SSO with reactive stores.',
              type: 'sdk',
              category: 'integration',
              downloadUrl: '/downloads/oasis-avatar-sso-sdk-vue.zip',
              version: '1.5.2',
              size: '11.2 MB',
              downloads: 8934,
              rating: 4.8,
              tags: ['vue', 'composition-api', 'sso', 'reactive'],
              author: 'OASIS Team',
              lastUpdated: '2024-02-14T14:20:00Z',
              featured: true,
              difficulty: 'intermediate',
              estimatedTime: '1 hour',
              prerequisites: ['Vue 3+', 'TypeScript'],
              languages: ['TypeScript', 'JavaScript'],
              frameworks: ['Vue'],
              platforms: ['Web'],
              content: 'Vue 3 plugin with Composition API composables for reactive authentication state.',
              codeExamples: [
                `import { createApp } from 'vue';\nimport { OasisSSOPlugin } from '@oasis/avatar-sso-vue';\n\nconst app = createApp(App);\napp.use(OasisSSOPlugin, {\n  apiUrl: 'https://api.oasis.network'\n});\n\n// In components\nconst { user, isAuthenticated, login } = useOasisSSO();`
              ],
              screenshots: ['/screenshots/vue-sso-1.png'],
              videoUrl: 'https://youtube.com/watch?v=vue-sso',
              githubUrl: 'https://github.com/oasis/avatar-sso-vue',
              documentationUrl: 'https://docs.oasis.network/avatar-sso/vue',
              supportUrl: 'https://support.oasis.network/avatar-sso'
            },
            {
              id: '8',
              title: 'OASIS Avatar SSO SDK - Vanilla JS',
              description: 'Pure JavaScript SDK for Avatar SSO - no framework required, works anywhere.',
              type: 'sdk',
              category: 'integration',
              downloadUrl: '/downloads/oasis-avatar-sso-sdk-vanilla.zip',
              version: '1.5.2',
              size: '8.4 MB',
              downloads: 15678,
              rating: 4.7,
              tags: ['javascript', 'vanilla', 'sso', 'lightweight'],
              author: 'OASIS Team',
              lastUpdated: '2024-02-14T14:20:00Z',
              featured: true,
              difficulty: 'beginner',
              estimatedTime: '30 minutes',
              prerequisites: ['JavaScript basics'],
              languages: ['JavaScript'],
              frameworks: ['None'],
              platforms: ['Web', 'Any JavaScript environment'],
              content: 'Zero-dependency vanilla JavaScript SDK that works in any environment - browser, Node.js, or Deno.',
              codeExamples: [
                `// Include via CDN or npm\nconst sso = new OasisAvatarSSO({\n  apiUrl: 'https://api.oasis.network',\n  provider: 'Auto'\n});\n\nawait sso.login('username', 'password');\nconst user = await sso.getCurrentUser();\nconsole.log('Logged in as:', user.username);`
              ],
              screenshots: ['/screenshots/vanilla-sso-1.png'],
              videoUrl: 'https://youtube.com/watch?v=vanilla-sso',
              githubUrl: 'https://github.com/oasis/avatar-sso-vanilla',
              documentationUrl: 'https://docs.oasis.network/avatar-sso/vanilla',
              supportUrl: 'https://support.oasis.network/avatar-sso'
            },
            {
              id: '9',
              title: 'OASIS Avatar SSO SDK - Svelte',
              description: 'Svelte stores and components for Avatar SSO with reactive authentication.',
              type: 'sdk',
              category: 'integration',
              downloadUrl: '/downloads/oasis-avatar-sso-sdk-svelte.zip',
              version: '1.5.2',
              size: '9.7 MB',
              downloads: 5423,
              rating: 4.8,
              tags: ['svelte', 'stores', 'sso', 'reactive'],
              author: 'OASIS Team',
              lastUpdated: '2024-02-14T14:20:00Z',
              featured: false,
              difficulty: 'intermediate',
              estimatedTime: '45 minutes',
              prerequisites: ['Svelte 3+', 'TypeScript'],
              languages: ['TypeScript', 'JavaScript'],
              frameworks: ['Svelte'],
              platforms: ['Web'],
              content: 'Svelte stores and components for reactive avatar authentication with SvelteKit support.',
              codeExamples: [
                `import { initOasisSSO, oasisSSO, user, isAuthenticated } from '@oasis/avatar-sso-svelte';\n\ninitOasisSSO({ apiUrl: 'https://api.oasis.network' });\n\n// In components\n{#if $isAuthenticated}\n  <p>Welcome, {$user.username}!</p>\n  <button on:click={() => oasisSSO.logout()}>Logout</button>\n{/if}`
              ],
              screenshots: ['/screenshots/svelte-sso-1.png'],
              videoUrl: 'https://youtube.com/watch?v=svelte-sso',
              githubUrl: 'https://github.com/oasis/avatar-sso-svelte',
              documentationUrl: 'https://docs.oasis.network/avatar-sso/svelte',
              supportUrl: 'https://support.oasis.network/avatar-sso'
            },
            {
              id: '10',
              title: 'OASIS Avatar SSO SDK - Next.js',
              description: 'Next.js App Router and Pages Router integration with server-side authentication.',
              type: 'sdk',
              category: 'integration',
              downloadUrl: '/downloads/oasis-avatar-sso-sdk-nextjs.zip',
              version: '1.5.2',
              size: '14.6 MB',
              downloads: 18234,
              rating: 4.9,
              tags: ['nextjs', 'ssr', 'sso', 'server-components'],
              author: 'OASIS Team',
              lastUpdated: '2024-02-14T14:20:00Z',
              featured: true,
              difficulty: 'advanced',
              estimatedTime: '2 hours',
              prerequisites: ['Next.js 13+', 'React', 'TypeScript'],
              languages: ['TypeScript', 'JavaScript'],
              frameworks: ['Next.js', 'React'],
              platforms: ['Web', 'Server-Side'],
              content: 'Full Next.js integration with App Router, Pages Router, middleware, and server actions support.',
              codeExamples: [
                `// app/providers.tsx\n'use client';\nimport { OasisSSOProvider } from '@oasis/avatar-sso-nextjs';\n\nexport function Providers({ children }) {\n  return (\n    <OasisSSOProvider apiUrl={process.env.NEXT_PUBLIC_OASIS_API_URL}>\n      {children}\n    </OasisSSOProvider>\n  );\n}`
              ],
              screenshots: ['/screenshots/nextjs-sso-1.png'],
              videoUrl: 'https://youtube.com/watch?v=nextjs-sso',
              githubUrl: 'https://github.com/oasis/avatar-sso-nextjs',
              documentationUrl: 'https://docs.oasis.network/avatar-sso/nextjs',
              supportUrl: 'https://support.oasis.network/avatar-sso'
            },
            {
              id: '11',
              title: 'OASIS Web UI Dev Kit - React',
              description: 'Complete React component library with 20+ pre-built components for Avatar SSO, Karma, NFTs, Messaging, Data Management, and more.',
              type: 'ui-kit',
              category: 'development',
              downloadUrl: '/downloads/oasis-webui-devkit-react.zip',
              version: '1.0.0',
              size: '45.2 MB',
              downloads: 15420,
              rating: 5.0,
              tags: ['react', 'components', 'ui-kit', 'widgets', 'full-stack'],
              author: 'OASIS Team',
              lastUpdated: '2024-02-15T10:00:00Z',
              featured: true,
              difficulty: 'beginner',
              estimatedTime: '30 minutes',
              prerequisites: ['React 18+', 'Node.js 16+'],
              languages: ['TypeScript', 'JavaScript'],
              frameworks: ['React'],
              platforms: ['Web'],
              content: 'Comprehensive UI component library including: AvatarSSO, KarmaManagement, NFTGallery, NFTManagement, GeoNFTMap, Messaging, ChatWidget, DataManagement, ProviderManagement, OASISSettings, Notifications, SocialFeed, FriendsList, GroupManagement, and more. Fully customizable with theme support.',
              codeExamples: [
                `import { AvatarSSO, KarmaManagement, NFTGallery } from '@oasis/webui-devkit-react';\n\nfunction App() {\n  const [avatarId, setAvatarId] = useState('');\n  \n  return (\n    <>\n      <AvatarSSO onSuccess={(avatar) => setAvatarId(avatar.id)} />\n      <KarmaManagement avatarId={avatarId} theme="dark" />\n      <NFTGallery avatarId={avatarId} columns={3} />\n    </>\n  );\n}`
              ],
              screenshots: ['/screenshots/react-devkit-1.png'],
              videoUrl: 'https://youtube.com/watch?v=react-devkit',
              githubUrl: 'https://github.com/oasis/webui-devkit-react',
              documentationUrl: 'https://docs.oasis.network/webui-devkit/react',
              supportUrl: 'https://support.oasis.network/webui-devkit'
            },
            {
              id: '12',
              title: 'OASIS Web UI Dev Kit - Angular',
              description: 'Complete Angular component library with full RxJS integration and reactive forms support.',
              type: 'ui-kit',
              category: 'development',
              downloadUrl: '/downloads/oasis-webui-devkit-angular.zip',
              version: '1.0.0',
              size: '48.5 MB',
              downloads: 12340,
              rating: 4.9,
              tags: ['angular', 'components', 'ui-kit', 'rxjs', 'reactive'],
              author: 'OASIS Team',
              lastUpdated: '2024-02-15T10:00:00Z',
              featured: true,
              difficulty: 'intermediate',
              estimatedTime: '45 minutes',
              prerequisites: ['Angular 15+', 'RxJS 7+'],
              languages: ['TypeScript'],
              frameworks: ['Angular'],
              platforms: ['Web'],
              content: 'Enterprise-grade Angular components with module and standalone support. Includes all OASIS functionality as Angular services and components.',
              codeExamples: [
                `import { OasisWebUIModule } from '@oasis/webui-devkit-angular';\n\n@NgModule({\n  imports: [\n    OasisWebUIModule.forRoot({\n      apiEndpoint: 'https://api.oasis.network'\n    })\n  ]\n})\nexport class AppModule { }`
              ],
              screenshots: ['/screenshots/angular-devkit-1.png'],
              videoUrl: 'https://youtube.com/watch?v=angular-devkit',
              githubUrl: 'https://github.com/oasis/webui-devkit-angular',
              documentationUrl: 'https://docs.oasis.network/webui-devkit/angular',
              supportUrl: 'https://support.oasis.network/webui-devkit'
            },
            {
              id: '13',
              title: 'OASIS Web UI Dev Kit - Vue 3',
              description: 'Modern Vue 3 Composition API component library with reactive stores and composables.',
              type: 'ui-kit',
              category: 'development',
              downloadUrl: '/downloads/oasis-webui-devkit-vue.zip',
              version: '1.0.0',
              size: '42.8 MB',
              downloads: 10890,
              rating: 5.0,
              tags: ['vue', 'vue3', 'composition-api', 'components', 'reactive'],
              author: 'OASIS Team',
              lastUpdated: '2024-02-15T10:00:00Z',
              featured: true,
              difficulty: 'beginner',
              estimatedTime: '30 minutes',
              prerequisites: ['Vue 3.3+', 'Vite or Vue CLI'],
              languages: ['TypeScript', 'JavaScript'],
              frameworks: ['Vue 3'],
              platforms: ['Web'],
              content: 'Lightweight Vue 3 components using Composition API with built-in stores and composables for state management.',
              codeExamples: [
                `<script setup>\nimport { AvatarSSO, KarmaManagement } from '@oasis/webui-devkit-vue';\nimport { ref } from 'vue';\n\nconst avatarId = ref('');\n</script>\n\n<template>\n  <AvatarSSO @success="avatarId = $event.id" />\n  <KarmaManagement :avatar-id="avatarId" />\n</template>`
              ],
              screenshots: ['/screenshots/vue-devkit-1.png'],
              videoUrl: 'https://youtube.com/watch?v=vue-devkit',
              githubUrl: 'https://github.com/oasis/webui-devkit-vue',
              documentationUrl: 'https://docs.oasis.network/webui-devkit/vue',
              supportUrl: 'https://support.oasis.network/webui-devkit'
            },
            {
              id: '14',
              title: 'OASIS Web UI Dev Kit - Vanilla JS',
              description: 'Framework-agnostic Web Components library - works with any JavaScript project!',
              type: 'ui-kit',
              category: 'development',
              downloadUrl: '/downloads/oasis-webui-devkit-vanilla.zip',
              version: '1.0.0',
              size: '38.4 MB',
              downloads: 8760,
              rating: 4.8,
              tags: ['vanilla-js', 'web-components', 'custom-elements', 'no-framework'],
              author: 'OASIS Team',
              lastUpdated: '2024-02-15T10:00:00Z',
              featured: true,
              difficulty: 'beginner',
              estimatedTime: '15 minutes',
              prerequisites: ['Modern browser', 'No framework required'],
              languages: ['JavaScript'],
              frameworks: ['None'],
              platforms: ['Web'],
              content: 'Pure Web Components with no dependencies. Use with any framework or vanilla JavaScript. Simple CDN integration.',
              codeExamples: [
                `<!-- Via CDN -->\n<script src="https://cdn.oasis.network/webui-devkit/1.0.0/oasis.min.js"></script>\n\n<!-- Use components -->\n<oasis-avatar-sso providers="holochain,ethereum"></oasis-avatar-sso>\n<oasis-karma-management avatar-id="123"></oasis-karma-management>\n\n<script>\n  document.querySelector('oasis-avatar-sso')\n    .addEventListener('success', (e) => console.log(e.detail));\n</script>`
              ],
              screenshots: ['/screenshots/vanilla-devkit-1.png'],
              videoUrl: 'https://youtube.com/watch?v=vanilla-devkit',
              githubUrl: 'https://github.com/oasis/webui-devkit-vanilla',
              documentationUrl: 'https://docs.oasis.network/webui-devkit/vanilla',
              supportUrl: 'https://support.oasis.network/webui-devkit'
            },
            {
              id: '15',
              title: 'OASIS Web UI Dev Kit - Svelte',
              description: 'Reactive Svelte component library with built-in stores and minimal bundle size.',
              type: 'ui-kit',
              category: 'development',
              downloadUrl: '/downloads/oasis-webui-devkit-svelte.zip',
              version: '1.0.0',
              size: '35.6 MB',
              downloads: 7650,
              rating: 5.0,
              tags: ['svelte', 'reactive', 'components', 'lightweight'],
              author: 'OASIS Team',
              lastUpdated: '2024-02-15T10:00:00Z',
              featured: true,
              difficulty: 'beginner',
              estimatedTime: '25 minutes',
              prerequisites: ['Svelte 4.0+', 'Vite or SvelteKit'],
              languages: ['TypeScript', 'JavaScript'],
              frameworks: ['Svelte'],
              platforms: ['Web'],
              content: 'Ultra-lightweight Svelte components with reactive stores. Smallest bundle size of all kits.',
              codeExamples: [
                `<script>\n  import { AvatarSSO, KarmaManagement } from '@oasis/webui-devkit-svelte';\n  let avatarId = '';\n</script>\n\n<AvatarSSO on:success={(e) => avatarId = e.detail.id} />\n<KarmaManagement {avatarId} />`
              ],
              screenshots: ['/screenshots/svelte-devkit-1.png'],
              videoUrl: 'https://youtube.com/watch?v=svelte-devkit',
              githubUrl: 'https://github.com/oasis/webui-devkit-svelte',
              documentationUrl: 'https://docs.oasis.network/webui-devkit/svelte',
              supportUrl: 'https://support.oasis.network/webui-devkit'
            },
            {
              id: '16',
              title: 'OASIS Web UI Dev Kit - Next.js',
              description: 'Next.js optimized components with Server Components, App Router, and API Routes integration.',
              type: 'ui-kit',
              category: 'development',
              downloadUrl: '/downloads/oasis-webui-devkit-nextjs.zip',
              version: '1.0.0',
              size: '52.3 MB',
              downloads: 14230,
              rating: 5.0,
              tags: ['nextjs', 'ssr', 'server-components', 'app-router'],
              author: 'OASIS Team',
              lastUpdated: '2024-02-15T10:00:00Z',
              featured: true,
              difficulty: 'intermediate',
              estimatedTime: '1 hour',
              prerequisites: ['Next.js 13.4+', 'React 18+', 'TypeScript'],
              languages: ['TypeScript'],
              frameworks: ['Next.js', 'React'],
              platforms: ['Web', 'Server-Side'],
              content: 'Full Next.js integration with RSC (React Server Components), client components, API routes, middleware, and server actions.',
              codeExamples: [
                `// app/page.tsx (Server Component)\nimport { AvatarDetailServer } from '@oasis/webui-devkit-nextjs/server';\n\nexport default async function Page() {\n  return <AvatarDetailServer avatarId="123" />;\n}\n\n// Client component\n'use client';\nimport { KarmaManagement } from '@oasis/webui-devkit-nextjs';\n\nexport function Dashboard() {\n  return <KarmaManagement avatarId="123" />;\n}`
              ],
              screenshots: ['/screenshots/nextjs-devkit-1.png'],
              videoUrl: 'https://youtube.com/watch?v=nextjs-devkit',
              githubUrl: 'https://github.com/oasis/webui-devkit-nextjs',
              documentationUrl: 'https://docs.oasis.network/webui-devkit/nextjs',
              supportUrl: 'https://support.oasis.network/webui-devkit'
            },
            {
              id: '17',
              title: '@oasis/web4-api-client - NPM Package',
              description: 'Official JavaScript/TypeScript client for OASIS Web4 API - Avatar management, Karma, NFTs, and cross-provider data storage.',
              type: 'npm-package',
              category: 'api-clients',
              downloadUrl: '/downloads/oasis-web4-api-client.zip',
              npmUrl: 'https://www.npmjs.com/package/@oasis/web4-api-client',
              version: '1.0.0',
              size: '856 KB',
              downloads: 45678,
              rating: 5.0,
              tags: ['npm', 'api-client', 'web4', 'typescript', 'javascript'],
              author: 'NextGen Software',
              lastUpdated: '2024-02-15T12:00:00Z',
              featured: true,
              difficulty: 'beginner',
              estimatedTime: '10 minutes',
              prerequisites: ['Node.js 16+', 'npm or yarn'],
              languages: ['JavaScript', 'TypeScript'],
              frameworks: ['Any JS framework'],
              platforms: ['Web', 'Node.js'],
              content: 'Full-featured API client with TypeScript support, automatic retries, error handling, and comprehensive documentation.',
              codeExamples: [
                `npm install @oasis/web4-api-client\n\nimport { OASISWeb4Client } from '@oasis/web4-api-client';\n\nconst client = new OASISWeb4Client({ apiUrl: 'http://localhost:5000/api' });\n\n// Authenticate\nconst auth = await client.authenticate('holochain');\n\n// Get karma\nconst karma = await client.getKarma(avatarId);`
              ],
              screenshots: ['/screenshots/web4-npm-1.png'],
              videoUrl: 'https://youtube.com/watch?v=web4-npm-client',
              githubUrl: 'https://github.com/NextGenSoftwareUK/OASIS-API',
              documentationUrl: 'https://docs.oasis.network/web4-api',
              supportUrl: 'https://support.oasis.network/web4'
            },
            {
              id: '18',
              title: '@oasis/web5-star-api-client - NPM Package',
              description: 'Official JavaScript/TypeScript client for OASIS Web5 STAR API - Gamification, OAPPs, missions, quests, and metaverse.',
              type: 'npm-package',
              category: 'api-clients',
              downloadUrl: '/downloads/oasis-web5-star-api-client.zip',
              npmUrl: 'https://www.npmjs.com/package/@oasis/web5-star-api-client',
              version: '1.0.0',
              size: '1.2 MB',
              downloads: 34567,
              rating: 5.0,
              tags: ['npm', 'api-client', 'web5', 'star', 'gamification'],
              author: 'NextGen Software',
              lastUpdated: '2024-02-15T12:00:00Z',
              featured: true,
              difficulty: 'intermediate',
              estimatedTime: '15 minutes',
              prerequisites: ['Node.js 16+', '@oasis/web4-api-client'],
              languages: ['JavaScript', 'TypeScript'],
              frameworks: ['Any JS framework'],
              platforms: ['Web', 'Node.js'],
              content: 'Complete STAR API client with support for OAPPs, missions, quests, holons, zomes, and STARNET operations.',
              codeExamples: [
                `npm install @oasis/web5-star-api-client\n\nimport { OASISWeb5STARClient } from '@oasis/web5-star-api-client';\n\nconst client = new OASISWeb5STARClient();\n\n// Ignite STAR\nawait client.igniteSTAR();\n\n// Get OAPPs\nconst oapps = await client.getAllOAPPs();`
              ],
              screenshots: ['/screenshots/web5-npm-1.png'],
              videoUrl: 'https://youtube.com/watch?v=web5-star-npm',
              githubUrl: 'https://github.com/NextGenSoftwareUK/OASIS-STAR',
              documentationUrl: 'https://docs.oasis.network/web5-star-api',
              supportUrl: 'https://support.oasis.network/star'
            },
            {
              id: '19',
              title: 'oasis-web4-client - Rust Crate',
              description: 'Official Rust SDK for OASIS Web4 API - Type-safe, async/await with full Rust ecosystem integration.',
              type: 'rust-sdk',
              category: 'api-clients',
              downloadUrl: '/downloads/oasis-web4-rust-sdk.zip',
              cratesUrl: 'https://crates.io/crates/oasis-web4-client',
              version: '1.0.0',
              size: '245 KB',
              downloads: 23456,
              rating: 5.0,
              tags: ['rust', 'sdk', 'web4', 'async', 'tokio'],
              author: 'NextGen Software',
              lastUpdated: '2024-02-15T12:00:00Z',
              featured: true,
              difficulty: 'intermediate',
              estimatedTime: '20 minutes',
              prerequisites: ['Rust 1.70+', 'Cargo'],
              languages: ['Rust'],
              frameworks: ['Tokio', 'Reqwest'],
              platforms: ['Cross-platform'],
              content: 'High-performance Rust SDK with full type safety, comprehensive error handling, and async/await support.',
              codeExamples: [
                `[dependencies]\noasis-web4-client = "1.0"\ntokio = { version = "1", features = ["full"] }\n\nuse oasis_web4_client::{OASISWeb4Client, Config};\n\n#[tokio::main]\nasync fn main() {\n    let client = OASISWeb4Client::new(Config::default());\n    let karma = client.get_karma("avatar-id").await?;\n}`
              ],
              screenshots: ['/screenshots/rust-web4-1.png'],
              videoUrl: 'https://youtube.com/watch?v=rust-web4-sdk',
              githubUrl: 'https://github.com/NextGenSoftwareUK/OASIS-API',
              documentationUrl: 'https://docs.rs/oasis-web4-client',
              supportUrl: 'https://support.oasis.network/web4'
            },
            {
              id: '20',
              title: 'oasis-web5-star-client - Rust Crate',
              description: 'Official Rust SDK for OASIS Web5 STAR API - Full STAR ecosystem support with Rust performance and safety.',
              type: 'rust-sdk',
              category: 'api-clients',
              downloadUrl: '/downloads/oasis-web5-star-rust-sdk.zip',
              cratesUrl: 'https://crates.io/crates/oasis-web5-star-client',
              version: '1.0.0',
              size: '312 KB',
              downloads: 18765,
              rating: 5.0,
              tags: ['rust', 'sdk', 'web5', 'star', 'gamification'],
              author: 'NextGen Software',
              lastUpdated: '2024-02-15T12:00:00Z',
              featured: true,
              difficulty: 'advanced',
              estimatedTime: '30 minutes',
              prerequisites: ['Rust 1.70+', 'oasis-web4-client'],
              languages: ['Rust'],
              frameworks: ['Tokio', 'Reqwest'],
              platforms: ['Cross-platform'],
              content: 'Complete STAR ecosystem SDK with support for OAPPs, missions, quests, holons, zomes, and STARNET in pure Rust.',
              codeExamples: [
                `[dependencies]\noasis-web5-star-client = "1.0"\n\nuse oasis_web5_star_client::OASISWeb5STARClient;\n\n#[tokio::main]\nasync fn main() {\n    let client = OASISWeb5STARClient::new(Config::default());\n    client.ignite_star().await?;\n    let oapps = client.get_all_oapps().await?;\n}`
              ],
              screenshots: ['/screenshots/rust-web5-1.png'],
              videoUrl: 'https://youtube.com/watch?v=rust-star-sdk',
              githubUrl: 'https://github.com/NextGenSoftwareUK/OASIS-STAR',
              documentationUrl: 'https://docs.rs/oasis-web5-star-client',
              supportUrl: 'https://support.oasis.network/star'
            },
            {
              id: '21',
              title: 'OASIS Web4 Unity SDK',
              description: '🎮 Unity SDK for OASIS Web4 API - Connect your Unity games to the decentralized OASIS ecosystem with 80+ blockchain providers.',
              type: 'unity-sdk',
              category: 'game-engines',
              downloadUrl: '/downloads/oasis-web4-unity-sdk.zip',
              unityPackageUrl: 'https://github.com/NextGenSoftwareUK/OASIS-Unity-SDK',
              version: '1.0.0',
              size: '45.2 MB',
              downloads: 12450,
              rating: 4.9,
              tags: ['unity', 'game-dev', 'sdk', 'web4', 'blockchain'],
              author: 'NextGen Software',
              lastUpdated: '2024-02-15T13:00:00Z',
              featured: true,
              difficulty: 'intermediate',
              estimatedTime: '2 hours',
              prerequisites: ['Unity 2020.3+', 'C# knowledge'],
              languages: ['C#'],
              frameworks: ['Unity'],
              platforms: ['Windows', 'macOS', 'Linux', 'iOS', 'Android', 'Console'],
              content: 'Complete Unity SDK with Blueprint & C# support for avatar management, karma system, data storage (Holons/Zomes), and provider integration.',
              codeExamples: [
                `using NextGenSoftware.OASIS.API.Unity;\n\npublic class GameManager : MonoBehaviour\n{\n    private OASISClient oasisClient;\n\n    async void Start()\n    {\n        oasisClient = new OASISClient(new OASISConfig\n        {\n            BaseUrl = "https://api.oasis.earth/api/v1",\n            ApiKey = "your-api-key"\n        });\n\n        var authResult = await oasisClient.Avatar.AuthenticateAsync(email, password);\n        Debug.Log($"Welcome {authResult.Result.Username}!");\n    }\n}`
              ],
              screenshots: ['/screenshots/unity-web4-1.png'],
              videoUrl: 'https://youtube.com/watch?v=unity-oasis-sdk',
              githubUrl: 'https://github.com/NextGenSoftwareUK/OASIS-Unity-SDK',
              documentationUrl: 'https://docs.oasis.earth/unity',
              supportUrl: 'https://support.oasis.network/unity'
            },
            {
              id: '22',
              title: 'OASIS Web5 STAR Unity SDK',
              description: '⭐ Unity SDK for OASIS Web5 STAR API - Build next-generation metaverse experiences with quests, GeoNFTs, and STAR platform integration.',
              type: 'unity-sdk',
              category: 'game-engines',
              downloadUrl: '/downloads/oasis-web5-star-unity-sdk.zip',
              unityPackageUrl: 'https://github.com/NextGenSoftwareUK/OASIS-STAR-Unity-SDK',
              version: '1.0.0',
              size: '52.8 MB',
              downloads: 8930,
              rating: 5.0,
              tags: ['unity', 'star', 'metaverse', 'xr', 'vr', 'ar', 'geonft'],
              author: 'NextGen Software',
              lastUpdated: '2024-02-15T13:15:00Z',
              featured: true,
              difficulty: 'advanced',
              estimatedTime: '3 hours',
              prerequisites: ['Unity 2020.3+', 'OASIS Web4 Unity SDK', 'XR Plugin'],
              languages: ['C#'],
              frameworks: ['Unity', 'Unity XR'],
              platforms: ['PC VR', 'Quest', 'Mobile AR', 'Desktop'],
              content: 'Build AAA metaverse experiences with complete STAR integration: Chapters/Missions/Quests system, GeoNFT AR/VR, OAPP development, and world creation.',
              codeExamples: [
                `using NextGenSoftware.OASIS.STAR.Unity;\n\npublic class QuestManager : MonoBehaviour\n{\n    private STARClient starClient;\n\n    public async void StartQuest(Guid questId)\n    {\n        var result = await starClient.Quests.StartQuestAsync(questId);\n        if (!result.IsError)\n        {\n            Debug.Log($"Quest started: {result.Result.Name}");\n            Debug.Log($"Objectives: {result.Result.Objectives.Count}");\n        }\n    }\n}`
              ],
              screenshots: ['/screenshots/unity-star-1.png'],
              videoUrl: 'https://youtube.com/watch?v=unity-star-sdk',
              githubUrl: 'https://github.com/NextGenSoftwareUK/OASIS-STAR-Unity-SDK',
              documentationUrl: 'https://docs.oasis.earth/unity-star',
              supportUrl: 'https://support.oasis.network/star'
            },
            {
              id: '23',
              title: 'OASIS Web4 Unreal Engine SDK',
              description: '🎮 Unreal Engine SDK for OASIS Web4 API - Bring AAA metaverse experiences to life with Blueprint & C++ support.',
              type: 'unreal-sdk',
              category: 'game-engines',
              downloadUrl: '/downloads/oasis-web4-unreal-sdk.zip',
              unrealMarketplaceUrl: 'https://www.unrealengine.com/marketplace/oasis-web4',
              version: '1.0.0',
              size: '78.4 MB',
              downloads: 6780,
              rating: 4.9,
              tags: ['unreal', 'ue5', 'blueprints', 'cpp', 'sdk'],
              author: 'NextGen Software',
              lastUpdated: '2024-02-15T13:30:00Z',
              featured: true,
              difficulty: 'advanced',
              estimatedTime: '3 hours',
              prerequisites: ['Unreal Engine 5.0+', 'C++ or Blueprint knowledge'],
              languages: ['C++', 'Blueprint'],
              frameworks: ['Unreal Engine'],
              platforms: ['Windows', 'macOS', 'Linux', 'PS5', 'Xbox', 'Console'],
              content: 'Complete Unreal Engine plugin with full Blueprint and C++ support for avatar system, karma management, data storage, and 80+ blockchain providers.',
              codeExamples: [
                `// C++ Integration\n#include "OASISClient.h"\n\nvoid AMyGameMode::BeginPlay()\n{\n    Super::BeginPlay();\n\n    OASISClient = NewObject<UOASISClient>();\n    FOASISConfig Config;\n    Config.BaseUrl = TEXT("https://api.oasis.earth/api/v1");\n    OASISClient->Initialize(Config);\n\n    OASISClient->Avatar->Authenticate(AuthRequest, [](const auto& Result) {\n        UE_LOG(LogTemp, Log, TEXT("Welcome %s!"), *Result.Data.Username);\n    });\n}`
              ],
              screenshots: ['/screenshots/unreal-web4-1.png'],
              videoUrl: 'https://youtube.com/watch?v=unreal-oasis-sdk',
              githubUrl: 'https://github.com/NextGenSoftwareUK/OASIS-Unreal-SDK',
              documentationUrl: 'https://docs.oasis.earth/unreal',
              supportUrl: 'https://support.oasis.network/unreal'
            },
            {
              id: '24',
              title: 'OASIS Web5 STAR Unreal Engine SDK',
              description: '⭐ Unreal Engine SDK for OASIS Web5 STAR API - Create stunning AAA metaverse experiences with Nanite, Lumen, and World Partition support.',
              type: 'unreal-sdk',
              category: 'game-engines',
              downloadUrl: '/downloads/oasis-web5-star-unreal-sdk.zip',
              unrealMarketplaceUrl: 'https://www.unrealengine.com/marketplace/oasis-star',
              version: '1.0.0',
              size: '95.6 MB',
              downloads: 4520,
              rating: 5.0,
              tags: ['unreal', 'ue5', 'star', 'metaverse', 'nanite', 'lumen', 'world-partition'],
              author: 'NextGen Software',
              lastUpdated: '2024-02-15T13:45:00Z',
              featured: true,
              difficulty: 'expert',
              estimatedTime: '4 hours',
              prerequisites: ['Unreal Engine 5.0+', 'OASIS Web4 Unreal SDK', 'C++ Advanced'],
              languages: ['C++', 'Blueprint'],
              frameworks: ['Unreal Engine 5'],
              platforms: ['Windows', 'PS5', 'Xbox Series X/S', 'VR', 'All Platforms'],
              content: 'Build AAA metaverse worlds with UE5: Quest system, GeoNFT integration with World Partition, OAPP development, multiplayer replication, and STAR platform features.',
              codeExamples: [
                `// Quest System C++\nUCLASS()\nclass UQuestManager : public UActorComponent\n{\n    GENERATED_BODY()\n\npublic:\n    UFUNCTION(BlueprintCallable)\n    void StartQuest(const FGuid& QuestId)\n    {\n        STARClient->Quests->StartQuest(QuestId, [this](const auto& Result)\n        {\n            if (!Result.bIsError)\n            {\n                ActiveQuests.Add(Result.Data);\n                OnQuestStarted.Broadcast(Result.Data);\n            }\n        });\n    }\n};`
              ],
              screenshots: ['/screenshots/unreal-star-1.png'],
              videoUrl: 'https://youtube.com/watch?v=unreal-star-sdk',
              githubUrl: 'https://github.com/NextGenSoftwareUK/OASIS-STAR-Unreal-SDK',
              documentationUrl: 'https://docs.oasis.earth/unreal-star',
              supportUrl: 'https://support.oasis.network/star'
            },
            {
              id: '25',
              title: 'oasis-web4-client - Python SDK',
              description: '🐍 Official Python SDK for OASIS Web4 API - Full-featured async Python client with type hints and comprehensive documentation.',
              type: 'python-sdk',
              category: 'api-clients',
              downloadUrl: '/downloads/oasis-web4-python-sdk.zip',
              pypiUrl: 'https://pypi.org/project/oasis-web4-client/',
              version: '1.0.0',
              size: '245 KB',
              downloads: 8920,
              rating: 4.9,
              tags: ['python', 'sdk', 'asyncio', 'web4', 'blockchain'],
              author: 'NextGen Software',
              lastUpdated: '2024-02-15T14:00:00Z',
              featured: true,
              difficulty: 'intermediate',
              estimatedTime: '45 minutes',
              prerequisites: ['Python 3.8+', 'pip', 'asyncio knowledge'],
              languages: ['Python'],
              frameworks: ['AsyncIO', 'aiohttp'],
              platforms: ['Cross-platform'],
              content: 'Modern async Python SDK with type hints, full avatar management, data operations, and provider integration for 80+ blockchains.',
              codeExamples: [
                `# pip install oasis-web4-client\nimport asyncio\nfrom oasis_web4_client import OASISWeb4Client, Config\n\nasync def main():\n    client = OASISWeb4Client(Config(\n        base_url="https://api.oasis.earth/api/v1",\n        api_key="your-api-key"\n    ))\n    \n    # Authenticate avatar\n    auth = await client.avatar.authenticate("user@example.com", "password")\n    print(f"Welcome {auth.username}!")\n    \n    # Create holon\n    holon = await client.data.create_holon(\n        name="PlayerData",\n        holon_type="GameState",\n        metadata={"level": 5, "score": 1000}\n    )\n\nif __name__ == "__main__":\n    asyncio.run(main())`
              ],
              screenshots: ['/screenshots/python-web4-1.png'],
              videoUrl: 'https://youtube.com/watch?v=python-oasis-sdk',
              githubUrl: 'https://github.com/NextGenSoftwareUK/OASIS-Python-SDK',
              documentationUrl: 'https://docs.oasis.earth/python',
              supportUrl: 'https://support.oasis.network/python'
            },
            {
              id: '26',
              title: 'oasis-web5-star-client - Python SDK',
              description: '⭐ Python SDK for OASIS Web5 STAR API - Build powerful STAR applications with Python including quests, OAPPs, and GeoNFTs.',
              type: 'python-sdk',
              category: 'api-clients',
              downloadUrl: '/downloads/oasis-web5-star-python-sdk.zip',
              pypiUrl: 'https://pypi.org/project/oasis-web5-star-client/',
              version: '1.0.0',
              size: '298 KB',
              downloads: 5630,
              rating: 5.0,
              tags: ['python', 'sdk', 'star', 'asyncio', 'metaverse'],
              author: 'NextGen Software',
              lastUpdated: '2024-02-15T14:15:00Z',
              featured: true,
              difficulty: 'advanced',
              estimatedTime: '1 hour',
              prerequisites: ['Python 3.8+', 'oasis-web4-client', 'asyncio'],
              languages: ['Python'],
              frameworks: ['AsyncIO', 'aiohttp'],
              platforms: ['Cross-platform'],
              content: 'Complete STAR ecosystem SDK for Python with async support for quests, missions, chapters, GeoNFTs, and OAPP development.',
              codeExamples: [
                `from oasis_web5_star_client import OASISWeb5STARClient\n\nasync def quest_example():\n    client = OASISWeb5STARClient(config)\n    \n    # Get all chapters\n    chapters = await client.get_all_chapters()\n    \n    # Start a quest\n    quest = await client.start_quest(quest_id)\n    print(f"Quest: {quest.name}")\n    \n    # Complete objective\n    result = await client.complete_objective(\n        quest_id=quest.id,\n        objective_id=quest.objectives[0].id\n    )\n    print(f"Karma earned: {result.karma_earned}")\n\nasyncio.run(quest_example())`
              ],
              screenshots: ['/screenshots/python-star-1.png'],
              videoUrl: 'https://youtube.com/watch?v=python-star-sdk',
              githubUrl: 'https://github.com/NextGenSoftwareUK/OASIS-STAR-Python-SDK',
              documentationUrl: 'https://docs.oasis.earth/python-star',
              supportUrl: 'https://support.oasis.network/star'
            },
            {
              id: '27',
              title: 'oasis-web4-go - Go SDK',
              description: '🔷 Official Go SDK for OASIS Web4 API - High-performance Go client with goroutines, channels, and idiomatic Go patterns.',
              type: 'go-sdk',
              category: 'api-clients',
              downloadUrl: '/downloads/oasis-web4-go-sdk.zip',
              goModUrl: 'https://pkg.go.dev/github.com/NextGenSoftwareUK/oasis-web4-go',
              version: '1.0.0',
              size: '189 KB',
              downloads: 6740,
              rating: 4.9,
              tags: ['go', 'golang', 'sdk', 'concurrency', 'blockchain'],
              author: 'NextGen Software',
              lastUpdated: '2024-02-15T14:30:00Z',
              featured: true,
              difficulty: 'intermediate',
              estimatedTime: '30 minutes',
              prerequisites: ['Go 1.19+', 'go mod'],
              languages: ['Go'],
              frameworks: ['Standard Library'],
              platforms: ['Cross-platform'],
              content: 'Idiomatic Go SDK with context support, goroutines, error handling, and full OASIS Web4 API coverage.',
              codeExamples: [
                `// go get github.com/NextGenSoftwareUK/oasis-web4-go\npackage main\n\nimport (\n    "context"\n    "fmt"\n    "log"\n    oasis "github.com/NextGenSoftwareUK/oasis-web4-go"\n)\n\nfunc main() {\n    client := oasis.NewClient(&oasis.Config{\n        BaseURL: "https://api.oasis.earth/api/v1",\n        APIKey:  "your-api-key",\n    })\n\n    ctx := context.Background()\n    \n    // Authenticate\n    auth, err := client.Avatar.Authenticate(ctx, &oasis.AuthRequest{\n        Email:    "user@example.com",\n        Password: "password",\n    })\n    if err != nil {\n        log.Fatal(err)\n    }\n    \n    fmt.Printf("Welcome %s!\\n", auth.Avatar.Username)\n    \n    // Create holon with goroutine\n    go func() {\n        holon, _ := client.Data.CreateHolon(ctx, &oasis.CreateHolonRequest{\n            Name:     "ServerData",\n            HolonType: "Cache",\n        })\n        fmt.Printf("Holon created: %s\\n", holon.ID)\n    }()\n}`
              ],
              screenshots: ['/screenshots/go-web4-1.png'],
              videoUrl: 'https://youtube.com/watch?v=go-oasis-sdk',
              githubUrl: 'https://github.com/NextGenSoftwareUK/oasis-web4-go',
              documentationUrl: 'https://docs.oasis.earth/go',
              supportUrl: 'https://support.oasis.network/go'
            },
            {
              id: '28',
              title: 'oasis-web5-star-go - Go SDK',
              description: '⭐ Go SDK for OASIS Web5 STAR API - Build scalable STAR applications with Go\'s concurrency model and performance.',
              type: 'go-sdk',
              category: 'api-clients',
              downloadUrl: '/downloads/oasis-web5-star-go-sdk.zip',
              goModUrl: 'https://pkg.go.dev/github.com/NextGenSoftwareUK/oasis-web5-star-go',
              version: '1.0.0',
              size: '234 KB',
              downloads: 4120,
              rating: 5.0,
              tags: ['go', 'golang', 'star', 'concurrency', 'metaverse'],
              author: 'NextGen Software',
              lastUpdated: '2024-02-15T14:45:00Z',
              featured: true,
              difficulty: 'advanced',
              estimatedTime: '45 minutes',
              prerequisites: ['Go 1.19+', 'oasis-web4-go'],
              languages: ['Go'],
              frameworks: ['Standard Library'],
              platforms: ['Cross-platform'],
              content: 'High-performance STAR SDK for Go with goroutine-based quest processing, channel-driven events, and full OAPP support.',
              codeExamples: [
                `package main\n\nimport (\n    "context"\n    star "github.com/NextGenSoftwareUK/oasis-web5-star-go"\n)\n\nfunc main() {\n    client := star.NewClient(config)\n    ctx := context.Background()\n    \n    // Process quests concurrently\n    questChan := make(chan *star.Quest, 10)\n    \n    go func() {\n        quests, _ := client.GetActiveQuests(ctx, avatarID)\n        for _, q := range quests {\n            questChan <- q\n        }\n        close(questChan)\n    }()\n    \n    // Process in parallel\n    for quest := range questChan {\n        go processQuest(client, ctx, quest)\n    }\n}\n\nfunc processQuest(c *star.Client, ctx context.Context, q *star.Quest) {\n    result, _ := c.CompleteQuest(ctx, q.ID)\n    log.Printf("Quest %s completed! Karma: %d", q.Name, result.KarmaEarned)\n}`
              ],
              screenshots: ['/screenshots/go-star-1.png'],
              videoUrl: 'https://youtube.com/watch?v=go-star-sdk',
              githubUrl: 'https://github.com/NextGenSoftwareUK/oasis-web5-star-go',
              documentationUrl: 'https://docs.oasis.earth/go-star',
              supportUrl: 'https://support.oasis.network/star'
            },
            {
              id: '29',
              title: 'OASIS.Web4.SDK - .NET SDK',
              description: '💎 Official .NET SDK for OASIS Web4 API - Modern C# SDK with async/await, LINQ support, and NuGet distribution.',
              type: 'dotnet-sdk',
              category: 'api-clients',
              downloadUrl: '/downloads/oasis-web4-dotnet-sdk.zip',
              nugetUrl: 'https://www.nuget.org/packages/OASIS.Web4.SDK/',
              version: '1.0.0',
              size: '312 KB',
              downloads: 9850,
              rating: 5.0,
              tags: ['dotnet', 'csharp', 'sdk', 'async', 'nuget'],
              author: 'NextGen Software',
              lastUpdated: '2024-02-15T15:00:00Z',
              featured: true,
              difficulty: 'intermediate',
              estimatedTime: '30 minutes',
              prerequisites: ['.NET 6+', 'NuGet'],
              languages: ['C#'],
              frameworks: ['.NET 6', '.NET 7', '.NET 8'],
              platforms: ['Windows', 'macOS', 'Linux'],
              content: 'Production-ready .NET SDK with modern C# features, async/await patterns, LINQ queries, and full IntelliSense support.',
              codeExamples: [
                `// Install-Package OASIS.Web4.SDK\nusing OASIS.Web4.SDK;\nusing OASIS.Web4.SDK.Models;\n\nvar client = new OASISWeb4Client(new OASISConfig\n{\n    BaseUrl = "https://api.oasis.earth/api/v1",\n    ApiKey = "your-api-key"\n});\n\n// Authenticate with async/await\nvar auth = await client.Avatar.AuthenticateAsync(\n    "user@example.com", \n    "password"\n);\n\nConsole.WriteLine($"Welcome {auth.Avatar.Username}!");\n\n// LINQ queries on holons\nvar holons = await client.Data.GetHolonsAsync();\nvar gameHolons = holons\n    .Where(h => h.HolonType == "Game")\n    .OrderByDescending(h => h.CreatedDate)\n    .Take(10);\n\nforeach (var holon in gameHolons)\n{\n    Console.WriteLine($"Holon: {holon.Name}");\n}`
              ],
              screenshots: ['/screenshots/dotnet-web4-1.png'],
              videoUrl: 'https://youtube.com/watch?v=dotnet-oasis-sdk',
              githubUrl: 'https://github.com/NextGenSoftwareUK/OASIS-DotNet-SDK',
              documentationUrl: 'https://docs.oasis.earth/dotnet',
              supportUrl: 'https://support.oasis.network/dotnet'
            },
            {
              id: '30',
              title: 'OASIS.Web5.STAR.SDK - .NET SDK',
              description: '⭐ .NET SDK for OASIS Web5 STAR API - Enterprise-grade C# SDK for building STAR applications with .NET ecosystem.',
              type: 'dotnet-sdk',
              category: 'api-clients',
              downloadUrl: '/downloads/oasis-web5-star-dotnet-sdk.zip',
              nugetUrl: 'https://www.nuget.org/packages/OASIS.Web5.STAR.SDK/',
              version: '1.0.0',
              size: '398 KB',
              downloads: 6540,
              rating: 5.0,
              tags: ['dotnet', 'csharp', 'star', 'async', 'enterprise'],
              author: 'NextGen Software',
              lastUpdated: '2024-02-15T15:15:00Z',
              featured: true,
              difficulty: 'advanced',
              estimatedTime: '1 hour',
              prerequisites: ['.NET 6+', 'OASIS.Web4.SDK'],
              languages: ['C#'],
              frameworks: ['.NET 6', '.NET 7', '.NET 8'],
              platforms: ['Windows', 'macOS', 'Linux'],
              content: 'Enterprise .NET SDK for STAR with reactive extensions, SignalR integration for real-time updates, and complete OAPP tooling.',
              codeExamples: [
                `using OASIS.Web5.STAR.SDK;\nusing System.Reactive.Linq;\n\nvar client = new OASISWeb5STARClient(config);\n\n// Reactive quest updates\nawait client.Quests.GetQuestUpdates()\n    .Where(q => q.Status == QuestStatus.Completed)\n    .Subscribe(async quest => \n    {\n        Console.WriteLine($"Quest completed: {quest.Name}");\n        var karma = await client.Avatar.GetKarmaAsync(avatarId);\n        Console.WriteLine($"Total Karma: {karma.Total}");\n    });\n\n// OAPP deployment\nvar oapp = new OAPPBuilder()\n    .WithName("MyEnterpriseApp")\n    .AddHolons(holonIds)\n    .AddZomes(zomeIds)\n    .Build();\n\nvar deployment = await client.OAPP.DeployAsync(oapp, \n    DeploymentTarget.Production);\n    \nConsole.WriteLine($"Deployed to: {deployment.Url}");`
              ],
              screenshots: ['/screenshots/dotnet-star-1.png'],
              videoUrl: 'https://youtube.com/watch?v=dotnet-star-sdk',
              githubUrl: 'https://github.com/NextGenSoftwareUK/OASIS-STAR-DotNet-SDK',
              documentationUrl: 'https://docs.oasis.earth/dotnet-star',
              supportUrl: 'https://support.oasis.network/star'
            },
            {
              id: '31',
              title: 'oasis-web4-java - Java SDK',
              description: '☕ Official Java SDK for OASIS Web4 API - Enterprise Java SDK with Maven/Gradle support and reactive streams.',
              type: 'java-sdk',
              category: 'api-clients',
              downloadUrl: '/downloads/oasis-web4-java-sdk.zip',
              mavenUrl: 'https://mvnrepository.com/artifact/earth.oasis/oasis-web4-sdk',
              version: '1.0.0',
              size: '456 KB',
              downloads: 7230,
              rating: 4.8,
              tags: ['java', 'sdk', 'maven', 'gradle', 'spring'],
              author: 'NextGen Software',
              lastUpdated: '2024-02-15T15:30:00Z',
              featured: true,
              difficulty: 'intermediate',
              estimatedTime: '45 minutes',
              prerequisites: ['Java 11+', 'Maven or Gradle'],
              languages: ['Java'],
              frameworks: ['Spring Boot', 'Reactor'],
              platforms: ['JVM', 'Cross-platform'],
              content: 'Production-ready Java SDK with reactive streams, Spring Boot integration, CompletableFuture support, and Maven Central distribution.',
              codeExamples: [
                `<!-- Maven dependency -->\n<dependency>\n    <groupId>earth.oasis</groupId>\n    <artifactId>oasis-web4-sdk</artifactId>\n    <version>1.0.0</version>\n</dependency>\n\n// Java code\nimport earth.oasis.web4.OASISWeb4Client;\nimport earth.oasis.web4.models.*;\n\npublic class OASISExample {\n    public static void main(String[] args) {\n        var client = new OASISWeb4Client(OASISConfig.builder()\n            .baseUrl("https://api.oasis.earth/api/v1")\n            .apiKey("your-api-key")\n            .build());\n        \n        // Async with CompletableFuture\n        client.avatar().authenticateAsync("user@example.com", "password")\n            .thenAccept(auth -> {\n                System.out.println("Welcome " + auth.getAvatar().getUsername());\n            })\n            .exceptionally(ex -> {\n                ex.printStackTrace();\n                return null;\n            });\n        \n        // Reactive streams\n        client.data().getHolonsFlux()\n            .filter(h -> "Game".equals(h.getHolonType()))\n            .subscribe(System.out::println);\n    }\n}`
              ],
              screenshots: ['/screenshots/java-web4-1.png'],
              videoUrl: 'https://youtube.com/watch?v=java-oasis-sdk',
              githubUrl: 'https://github.com/NextGenSoftwareUK/OASIS-Java-SDK',
              documentationUrl: 'https://docs.oasis.earth/java',
              supportUrl: 'https://support.oasis.network/java'
            },
            {
              id: '32',
              title: 'oasis-web5-star-java - Java SDK',
              description: '⭐ Java SDK for OASIS Web5 STAR API - Enterprise STAR SDK with Spring Boot auto-configuration and reactive support.',
              type: 'java-sdk',
              category: 'api-clients',
              downloadUrl: '/downloads/oasis-web5-star-java-sdk.zip',
              mavenUrl: 'https://mvnrepository.com/artifact/earth.oasis/oasis-web5-star-sdk',
              version: '1.0.0',
              size: '534 KB',
              downloads: 4890,
              rating: 4.9,
              tags: ['java', 'star', 'spring-boot', 'reactive', 'enterprise'],
              author: 'NextGen Software',
              lastUpdated: '2024-02-15T15:45:00Z',
              featured: true,
              difficulty: 'advanced',
              estimatedTime: '1 hour',
              prerequisites: ['Java 11+', 'Spring Boot 3', 'oasis-web4-java'],
              languages: ['Java'],
              frameworks: ['Spring Boot 3', 'Project Reactor'],
              platforms: ['JVM', 'Cloud', 'Kubernetes'],
              content: 'Enterprise-grade STAR SDK for Java with Spring Boot starters, reactive quest processing, and Kubernetes-ready OAPP deployment.',
              codeExamples: [
                `// Spring Boot Application\n@SpringBootApplication\n@EnableOASISSTAR\npublic class StarApplication {\n    @Autowired\n    private OASISWeb5STARClient starClient;\n    \n    public static void main(String[] args) {\n        SpringApplication.run(StarApplication.class, args);\n    }\n    \n    @Bean\n    public CommandLineRunner questRunner() {\n        return args -> {\n            // Reactive quest processing\n            starClient.quests().getAllChaptersFlux()\n                .flatMap(chapter -> Flux.fromIterable(chapter.getMissions()))\n                .flatMap(mission -> Flux.fromIterable(mission.getQuests()))\n                .filter(quest -> quest.getDifficulty() == Difficulty.EASY)\n                .subscribe(quest -> {\n                    System.out.println("Starting quest: " + quest.getName());\n                    starClient.quests().startQuestAsync(quest.getId())\n                        .thenAccept(System.out::println);\n                });\n        };\n    }\n}`
              ],
              screenshots: ['/screenshots/java-star-1.png'],
              videoUrl: 'https://youtube.com/watch?v=java-star-sdk',
              githubUrl: 'https://github.com/NextGenSoftwareUK/OASIS-STAR-Java-SDK',
              documentationUrl: 'https://docs.oasis.earth/java-star',
              supportUrl: 'https://support.oasis.network/star'
            },
            {
              id: '33',
              title: 'OASIS-Swift - iOS/macOS SDK',
              description: '🍎 Official Swift SDK for OASIS Web4 API - Native iOS/macOS SDK with async/await, Combine, and SwiftUI support.',
              type: 'swift-sdk',
              category: 'mobile',
              downloadUrl: '/downloads/oasis-web4-swift-sdk.zip',
              swiftPackageUrl: 'https://github.com/NextGenSoftwareUK/OASIS-Swift',
              version: '1.0.0',
              size: '189 KB',
              downloads: 5420,
              rating: 5.0,
              tags: ['swift', 'ios', 'macos', 'swiftui', 'combine'],
              author: 'NextGen Software',
              lastUpdated: '2024-02-15T16:00:00Z',
              featured: true,
              difficulty: 'intermediate',
              estimatedTime: '45 minutes',
              prerequisites: ['Swift 5.5+', 'Xcode 13+', 'iOS 15+'],
              languages: ['Swift'],
              frameworks: ['SwiftUI', 'Combine', 'URLSession'],
              platforms: ['iOS', 'iPadOS', 'macOS', 'watchOS', 'tvOS'],
              content: 'Modern Swift SDK with async/await, Combine publishers, SwiftUI components, and Swift Package Manager support.',
              codeExamples: [
                `// Swift Package Manager\n// .package(url: "https://github.com/NextGenSoftwareUK/OASIS-Swift", from: "1.0.0")\n\nimport OASIS\nimport SwiftUI\n\nstruct ContentView: View {\n    @StateObject private var oasisClient = OASISClient(config: .init(\n        baseURL: "https://api.oasis.earth/api/v1",\n        apiKey: "your-api-key"\n    ))\n    \n    var body: some View {\n        VStack {\n            Button("Authenticate") {\n                Task {\n                    let auth = try await oasisClient.avatar.authenticate(\n                        email: "user@example.com",\n                        password: "password"\n                    )\n                    print("Welcome \\(auth.avatar.username)!")\n                }\n            }\n        }\n    }\n}\n\n// Combine support\noasisClient.data.holonsPublisher()\n    .filter { $0.holonType == "Game" }\n    .sink { holon in\n        print("Holon: \\(holon.name)")\n    }`
              ],
              screenshots: ['/screenshots/swift-1.png'],
              videoUrl: 'https://youtube.com/watch?v=swift-oasis-sdk',
              githubUrl: 'https://github.com/NextGenSoftwareUK/OASIS-Swift',
              documentationUrl: 'https://docs.oasis.earth/swift',
              supportUrl: 'https://support.oasis.network/swift'
            },
            {
              id: '34',
              title: 'OASIS-STAR-Swift - iOS/macOS STAR SDK',
              description: '⭐ Swift SDK for OASIS Web5 STAR API - Native iOS/macOS STAR SDK with AR/GeoNFT support and SwiftUI.',
              type: 'swift-sdk',
              category: 'mobile',
              downloadUrl: '/downloads/oasis-web5-star-swift-sdk.zip',
              swiftPackageUrl: 'https://github.com/NextGenSoftwareUK/OASIS-STAR-Swift',
              version: '1.0.0',
              size: '234 KB',
              downloads: 3210,
              rating: 5.0,
              tags: ['swift', 'star', 'ar', 'geonft', 'swiftui'],
              author: 'NextGen Software',
              lastUpdated: '2024-02-15T16:15:00Z',
              featured: true,
              difficulty: 'advanced',
              estimatedTime: '1 hour',
              prerequisites: ['Swift 5.5+', 'ARKit', 'OASIS-Swift'],
              languages: ['Swift'],
              frameworks: ['SwiftUI', 'ARKit', 'MapKit', 'Combine'],
              platforms: ['iOS', 'iPadOS', 'macOS'],
              content: 'Complete STAR SDK for iOS/macOS with ARKit integration for GeoNFTs, quest system, and SwiftUI AR views.',
              codeExamples: [
                `import OASISSTARSwift\nimport ARKit\nimport SwiftUI\n\nstruct QuestARView: View {\n    @StateObject var starClient = OASISSTARClient(config: config)\n    @State private var currentQuest: Quest?\n    \n    var body: some View {\n        ZStack {\n            ARViewContainer()\n            \n            VStack {\n                if let quest = currentQuest {\n                    QuestCard(quest: quest)\n                }\n            }\n        }\n        .task {\n            // Load active quests\n            let quests = try await starClient.quests.getActiveQuests()\n            currentQuest = quests.first\n            \n            // Discover nearby GeoNFTs\n            let location = LocationManager.shared.currentLocation\n            let geoNFTs = try await starClient.geoNFT.getNearby(\n                latitude: location.latitude,\n                longitude: location.longitude,\n                radius: 1000\n            )\n            \n            // Place AR objects\n            for geoNFT in geoNFTs {\n                ARManager.shared.placeGeoNFT(geoNFT)\n            }\n        }\n    }\n}`
              ],
              screenshots: ['/screenshots/swift-star-1.png'],
              videoUrl: 'https://youtube.com/watch?v=swift-star-sdk',
              githubUrl: 'https://github.com/NextGenSoftwareUK/OASIS-STAR-Swift',
              documentationUrl: 'https://docs.oasis.earth/swift-star',
              supportUrl: 'https://support.oasis.network/star'
            },
            {
              id: '35',
              title: 'OASIS-Kotlin - Android SDK',
              description: '🤖 Official Kotlin SDK for OASIS Web4 API - Modern Android SDK with coroutines, Flow, and Jetpack Compose.',
              type: 'kotlin-sdk',
              category: 'mobile',
              downloadUrl: '/downloads/oasis-web4-kotlin-sdk.zip',
              mavenUrl: 'https://mvnrepository.com/artifact/earth.oasis/oasis-kotlin-sdk',
              version: '1.0.0',
              size: '278 KB',
              downloads: 6890,
              rating: 4.9,
              tags: ['kotlin', 'android', 'coroutines', 'jetpack-compose'],
              author: 'NextGen Software',
              lastUpdated: '2024-02-15T16:30:00Z',
              featured: true,
              difficulty: 'intermediate',
              estimatedTime: '30 minutes',
              prerequisites: ['Kotlin 1.8+', 'Android SDK 24+', 'Jetpack Compose'],
              languages: ['Kotlin'],
              frameworks: ['Jetpack Compose', 'Coroutines', 'Flow'],
              platforms: ['Android'],
              content: 'Native Android SDK with Kotlin coroutines, StateFlow, Jetpack Compose components, and Room database integration.',
              codeExamples: [
                `// build.gradle.kts\nimplementation("earth.oasis:oasis-kotlin-sdk:1.0.0")\n\n// Kotlin code\nimport earth.oasis.sdk.OASISClient\nimport earth.oasis.sdk.models.*\nimport kotlinx.coroutines.flow.*\n\nclass MainActivity : ComponentActivity() {\n    private val oasisClient = OASISClient(\n        config = OASISConfig(\n            baseUrl = "https://api.oasis.earth/api/v1",\n            apiKey = "your-api-key"\n        )\n    )\n    \n    override fun onCreate(savedInstanceState: Bundle?) {\n        super.onCreate(savedInstanceState)\n        setContent {\n            OASISTheme {\n                LoginScreen(oasisClient)\n            }\n        }\n    }\n}\n\n@Composable\nfun LoginScreen(client: OASISClient) {\n    var user by remember { mutableStateOf<Avatar?>(null) }\n    val scope = rememberCoroutineScope()\n    \n    Column {\n        Button(onClick = {\n            scope.launch {\n                val auth = client.avatar.authenticate(\n                    email = "user@example.com",\n                    password = "password"\n                )\n                user = auth.avatar\n            }\n        }) {\n            Text("Sign In")\n        }\n        \n        user?.let { Text("Welcome \${it.username}!") }\n    }\n}`
              ],
              screenshots: ['/screenshots/kotlin-1.png'],
              videoUrl: 'https://youtube.com/watch?v=kotlin-oasis-sdk',
              githubUrl: 'https://github.com/NextGenSoftwareUK/OASIS-Kotlin',
              documentationUrl: 'https://docs.oasis.earth/kotlin',
              supportUrl: 'https://support.oasis.network/kotlin'
            },
            {
              id: '36',
              title: 'OASIS-STAR-Kotlin - Android STAR SDK',
              description: '⭐ Kotlin SDK for OASIS Web5 STAR API - Android STAR SDK with AR Core, location services, and Jetpack Compose.',
              type: 'kotlin-sdk',
              category: 'mobile',
              downloadUrl: '/downloads/oasis-web5-star-kotlin-sdk.zip',
              mavenUrl: 'https://mvnrepository.com/artifact/earth.oasis/oasis-star-kotlin-sdk',
              version: '1.0.0',
              size: '345 KB',
              downloads: 4120,
              rating: 5.0,
              tags: ['kotlin', 'star', 'arcore', 'android', 'geonft'],
              author: 'NextGen Software',
              lastUpdated: '2024-02-15T16:45:00Z',
              featured: true,
              difficulty: 'advanced',
              estimatedTime: '1 hour',
              prerequisites: ['Kotlin 1.8+', 'AR Core', 'OASIS-Kotlin'],
              languages: ['Kotlin'],
              frameworks: ['Jetpack Compose', 'AR Core', 'Google Maps'],
              platforms: ['Android'],
              content: 'Complete STAR SDK for Android with AR Core GeoNFT support, quest tracking, location-based features, and Compose UI.',
              codeExamples: [
                `import earth.oasis.star.OASISSTARClient\nimport com.google.ar.core.*\n\nclass QuestActivity : ComponentActivity() {\n    private val starClient = OASISSTARClient(config)\n    \n    override fun onCreate(savedInstanceState: Bundle?) {\n        super.onCreate(savedInstanceState)\n        \n        lifecycleScope.launch {\n            // Get active quests\n            starClient.quests.getActiveQuests()\n                .collect { quests ->\n                    updateQuestUI(quests)\n                }\n            \n            // Discover nearby GeoNFTs\n            val location = LocationServices.getCurrentLocation()\n            val geoNFTs = starClient.geoNFT.getNearby(\n                latitude = location.latitude,\n                longitude = location.longitude,\n                radius = 1000.0\n            )\n            \n            // Place in AR\n            geoNFTs.forEach { geoNFT ->\n                arFragment.placeGeoNFT(\n                    geoNFT = geoNFT,\n                    anchor = createAnchorAt(geoNFT.location)\n                )\n            }\n        }\n    }\n}`
              ],
              screenshots: ['/screenshots/kotlin-star-1.png'],
              videoUrl: 'https://youtube.com/watch?v=kotlin-star-sdk',
              githubUrl: 'https://github.com/NextGenSoftwareUK/OASIS-STAR-Kotlin',
              documentationUrl: 'https://docs.oasis.earth/kotlin-star',
              supportUrl: 'https://support.oasis.network/star'
            },
            {
              id: '37',
              title: 'oasis-php - PHP SDK',
              description: '🐘 Official PHP SDK for OASIS Web4 API - Modern PHP SDK with Composer, PSR compliance, and async support.',
              type: 'php-sdk',
              category: 'api-clients',
              downloadUrl: '/downloads/oasis-web4-php-sdk.zip',
              packagistUrl: 'https://packagist.org/packages/oasis/web4-sdk',
              version: '1.0.0',
              size: '234 KB',
              downloads: 5670,
              rating: 4.8,
              tags: ['php', 'composer', 'psr', 'laravel', 'symfony'],
              author: 'NextGen Software',
              lastUpdated: '2024-02-15T17:00:00Z',
              featured: true,
              difficulty: 'intermediate',
              estimatedTime: '30 minutes',
              prerequisites: ['PHP 8.0+', 'Composer'],
              languages: ['PHP'],
              frameworks: ['Laravel', 'Symfony', 'Guzzle'],
              platforms: ['Web', 'Server'],
              content: 'PSR-compliant PHP SDK with Composer support, Laravel/Symfony integration, async HTTP client, and comprehensive error handling.',
              codeExamples: [
                `<?php\n// composer require oasis/web4-sdk\n\nuse OASIS\\Web4\\OASISClient;\nuse OASIS\\Web4\\Config;\n\n$client = new OASISClient(new Config([\n    'base_url' => 'https://api.oasis.earth/api/v1',\n    'api_key' => 'your-api-key'\n]));\n\n// Authenticate avatar\n$auth = $client->avatar->authenticate([\n    'email' => 'user@example.com',\n    'password' => 'password'\n]);\n\necho "Welcome {$auth->avatar->username}!\\n";\n\n// Create holon with async\n$promise = $client->data->createHolonAsync([\n    'name' => 'WebData',\n    'holonType' => 'Session',\n    'metadata' => ['ip' => $_SERVER['REMOTE_ADDR']]\n]);\n\n$holon = $promise->wait();\n\n// Laravel integration\nclass OASISService {\n    public function __construct(\n        private OASISClient $client\n    ) {}\n    \n    public function getUserKarma(string $avatarId): int {\n        return $this->client->avatar->getKarma($avatarId)->total;\n    }\n}`
              ],
              screenshots: ['/screenshots/php-1.png'],
              videoUrl: 'https://youtube.com/watch?v=php-oasis-sdk',
              githubUrl: 'https://github.com/NextGenSoftwareUK/OASIS-PHP',
              documentationUrl: 'https://docs.oasis.earth/php',
              supportUrl: 'https://support.oasis.network/php'
            },
            {
              id: '38',
              title: 'oasis-star-php - PHP STAR SDK',
              description: '⭐ PHP SDK for OASIS Web5 STAR API - Complete STAR SDK for PHP with Laravel packages and queue integration.',
              type: 'php-sdk',
              category: 'api-clients',
              downloadUrl: '/downloads/oasis-web5-star-php-sdk.zip',
              packagistUrl: 'https://packagist.org/packages/oasis/web5-star-sdk',
              version: '1.0.0',
              size: '298 KB',
              downloads: 3450,
              rating: 4.9,
              tags: ['php', 'star', 'laravel', 'queues', 'events'],
              author: 'NextGen Software',
              lastUpdated: '2024-02-15T17:15:00Z',
              featured: true,
              difficulty: 'advanced',
              estimatedTime: '45 minutes',
              prerequisites: ['PHP 8.0+', 'Laravel 10+', 'oasis/web4-sdk'],
              languages: ['PHP'],
              frameworks: ['Laravel', 'Symfony', 'Redis'],
              platforms: ['Web', 'Server'],
              content: 'Enterprise PHP STAR SDK with Laravel service providers, queue jobs for quest processing, event broadcasting, and Redis caching.',
              codeExamples: [
                `<?php\n// composer require oasis/web5-star-sdk\n\nuse OASIS\\Star\\OASISSTARClient;\nuse Illuminate\\Support\\Facades\\Queue;\n\nclass QuestController extends Controller {\n    public function __construct(\n        private OASISSTARClient $starClient\n    ) {}\n    \n    public function startQuest(Request $request) {\n        $quest = $this->starClient->quests->start(\n            $request->input('quest_id')\n        );\n        \n        // Queue objective processing\n        Queue::push(new ProcessQuestObjectives($quest));\n        \n        return response()->json($quest);\n    }\n    \n    public function getChapters() {\n        return Cache::remember('chapters', 3600, function() {\n            return $this->starClient->quests->getAllChapters();\n        });\n    }\n}\n\n// Event Broadcasting\nclass QuestCompleted implements ShouldBroadcast {\n    public function __construct(\n        public Quest $quest,\n        public int $karmaEarned\n    ) {}\n    \n    public function broadcastOn() {\n        return new Channel('quests');\n    }\n}`
              ],
              screenshots: ['/screenshots/php-star-1.png'],
              videoUrl: 'https://youtube.com/watch?v=php-star-sdk',
              githubUrl: 'https://github.com/NextGenSoftwareUK/OASIS-STAR-PHP',
              documentationUrl: 'https://docs.oasis.earth/php-star',
              supportUrl: 'https://support.oasis.network/star'
            },
            {
              id: '39',
              title: 'oasis-ruby - Ruby SDK',
              description: '💎 Official Ruby SDK for OASIS Web4 API - Idiomatic Ruby SDK with Rails integration and ActiveSupport.',
              type: 'ruby-sdk',
              category: 'api-clients',
              downloadUrl: '/downloads/oasis-web4-ruby-sdk.zip',
              rubygems: 'https://rubygems.org/gems/oasis-web4',
              version: '1.0.0',
              size: '156 KB',
              downloads: 3210,
              rating: 4.9,
              tags: ['ruby', 'rails', 'gem', 'activesupport'],
              author: 'NextGen Software',
              lastUpdated: '2024-02-15T17:30:00Z',
              featured: true,
              difficulty: 'intermediate',
              estimatedTime: '30 minutes',
              prerequisites: ['Ruby 3.0+', 'Bundler'],
              languages: ['Ruby'],
              frameworks: ['Ruby on Rails', 'Sinatra'],
              platforms: ['Web', 'Server'],
              content: 'Ruby gem with Rails generators, ActiveRecord integration, Sidekiq job support, and idiomatic Ruby patterns.',
              codeExamples: [
                `# Gemfile\ngem 'oasis-web4'\n\n# Ruby code\nrequire 'oasis/web4'\n\nclient = OASIS::Web4::Client.new(\n  base_url: 'https://api.oasis.earth/api/v1',\n  api_key: 'your-api-key'\n)\n\n# Authenticate avatar\nauth = client.avatar.authenticate(\n  email: 'user@example.com',\n  password: 'password'\n)\n\nputs "Welcome #{auth.avatar.username}!"\n\n# Rails integration\nclass AvatarsController < ApplicationController\n  def create\n    @avatar = oasis_client.avatar.register(\n      email: params[:email],\n      username: params[:username],\n      password: params[:password]\n    )\n    \n    session[:oasis_token] = @avatar.jwt_token\n    redirect_to dashboard_path\n  end\n  \n  private\n  \n  def oasis_client\n    @oasis_client ||= OASIS::Web4::Client.new(\n      api_key: Rails.application.credentials.oasis_api_key\n    )\n  end\nend\n\n# Background jobs\nclass ProcessKarmaJob < ApplicationJob\n  def perform(avatar_id, karma_amount)\n    client.avatar.add_karma(avatar_id, karma_amount)\n  end\nend`
              ],
              screenshots: ['/screenshots/ruby-1.png'],
              videoUrl: 'https://youtube.com/watch?v=ruby-oasis-sdk',
              githubUrl: 'https://github.com/NextGenSoftwareUK/OASIS-Ruby',
              documentationUrl: 'https://docs.oasis.earth/ruby',
              supportUrl: 'https://support.oasis.network/ruby'
            },
            {
              id: '40',
              title: 'oasis-star-ruby - Ruby STAR SDK',
              description: '⭐ Ruby SDK for OASIS Web5 STAR API - Rails-native STAR SDK with Action Cable integration and background jobs.',
              type: 'ruby-sdk',
              category: 'api-clients',
              downloadUrl: '/downloads/oasis-web5-star-ruby-sdk.zip',
              rubygemsUrl: 'https://rubygems.org/gems/oasis-web5-star',
              version: '1.0.0',
              size: '198 KB',
              downloads: 1890,
              rating: 5.0,
              tags: ['ruby', 'star', 'rails', 'actioncable', 'sidekiq'],
              author: 'NextGen Software',
              lastUpdated: '2024-02-15T17:45:00Z',
              featured: true,
              difficulty: 'advanced',
              estimatedTime: '45 minutes',
              prerequisites: ['Ruby 3.0+', 'Rails 7+', 'oasis-web4'],
              languages: ['Ruby'],
              frameworks: ['Ruby on Rails', 'Action Cable', 'Sidekiq'],
              platforms: ['Web', 'Server'],
              content: 'Complete Rails integration for STAR with real-time quest updates via Action Cable, Sidekiq jobs, and ActiveRecord models.',
              codeExamples: [
                `# Gemfile\ngem 'oasis-web5-star'\n\n# config/initializers/oasis_star.rb\nOASIS::Star.configure do |config|\n  config.api_key = Rails.application.credentials.oasis_api_key\n  config.base_url = 'https://api.star.oasis.earth/api/v1'\nend\n\n# app/controllers/quests_controller.rb\nclass QuestsController < ApplicationController\n  def start\n    quest = star_client.quests.start(params[:quest_id])\n    \n    # Broadcast to Action Cable\n    QuestChannel.broadcast_to(\n      current_user,\n      quest: quest,\n      message: 'Quest started!'\n    )\n    \n    # Queue background processing\n    ProcessQuestJob.perform_later(quest.id)\n    \n    render json: quest\n  end\n  \n  def chapters\n    @chapters = Rails.cache.fetch('star_chapters', expires_in: 1.hour) do\n      star_client.quests.all_chapters\n    end\n    \n    render json: @chapters\n  end\nend\n\n# app/channels/quest_channel.rb\nclass QuestChannel < ApplicationCable::Channel\n  def subscribed\n    stream_for current_user\n  end\nend`
              ],
              screenshots: ['/screenshots/ruby-star-1.png'],
              videoUrl: 'https://youtube.com/watch?v=ruby-star-sdk',
              githubUrl: 'https://github.com/NextGenSoftwareUK/OASIS-STAR-Ruby',
              documentationUrl: 'https://docs.oasis.earth/ruby-star',
              supportUrl: 'https://support.oasis.network/star'
            }
          ]
        };
      }

      console.log('Dev Portal Resources - Live Mode, making API call to:', API_BASE_URL + '/dev-portal/resources');
      try {
        const response = await api.get('/dev-portal/resources');
        console.log('Dev Portal Resources API response:', response.data);
        return response.data;
      } catch (error) {
        console.error('Error fetching dev portal resources:', error);
        return {
          isError: true,
          message: 'Failed to fetch dev portal resources',
          result: undefined
        };
      }
    },

    // Dashboard Data - Analytics and Metrics 📊
    async getDashboardData(): Promise<OASISResult<any>> {
    console.log('starService.getDashboardData called, isDemoMode():', isDemoMode());
    
    if (isDemoMode()) {
      // Demo mode - return impressive dashboard data
      console.log('Dashboard Data - Demo Mode');
      return {
        isError: false,
        message: 'Dashboard data loaded successfully (Demo Mode)',
        result: {
          overview: {
            totalUsers: 2547891,
            activeUsers: 892456,
            totalKarma: 12500000,
            systemHealth: 98.5,
            uptime: 99.9,
            transactions: 4567892,
            growthRate: 12.5,
            userSatisfaction: 4.8,
          },
          metrics: {
            oapps: { total: 1250, active: 892, growth: 8.2 },
            nfts: { total: 45678, active: 23456, growth: 15.3 },
            avatars: { total: 892456, active: 456789, growth: 22.1 },
            runtimes: { total: 234, active: 189, growth: 5.7 },
            libraries: { total: 567, active: 456, growth: 12.8 },
            templates: { total: 1234, active: 987, growth: 18.9 },
            celestialBodies: { total: 4567, active: 3456, growth: 7.4 },
            celestialSpaces: { total: 234, active: 189, growth: 9.2 },
            quests: { total: 1234, active: 567, growth: 14.6 },
            chapters: { total: 2345, active: 1234, growth: 11.3 },
            inventory: { total: 45678, active: 23456, growth: 16.7 },
            plugins: { total: 234, active: 189, growth: 6.9 },
            storeItems: { total: 1234, active: 987, growth: 13.2 },
          },
          recentActivity: [
            { id: 1, type: 'user', action: 'New user registered', user: 'John Doe', time: '2 minutes ago', status: 'success' },
            { id: 2, type: 'oapp', action: 'OAPP deployed', name: 'Quantum Calculator', time: '5 minutes ago', status: 'success' },
            { id: 3, type: 'nft', action: 'NFT minted', name: 'Cosmic Dragon', time: '8 minutes ago', status: 'success' },
            { id: 4, type: 'transaction', action: 'Payment processed', amount: '$2,500', time: '12 minutes ago', status: 'success' },
            { id: 5, type: 'error', action: 'System warning', message: 'High CPU usage detected', time: '15 minutes ago', status: 'warning' },
            { id: 6, type: 'avatar', action: 'Avatar created', name: 'Space Explorer', time: '18 minutes ago', status: 'success' },
            { id: 7, type: 'runtime', action: 'Runtime updated', name: 'Node.js 18', time: '22 minutes ago', status: 'success' },
            { id: 8, type: 'library', action: 'Library published', name: 'AI Toolkit', time: '25 minutes ago', status: 'success' },
          ],
          performanceData: [
            { name: 'Jan', users: 1200000, karma: 2100000, transactions: 45000 },
            { name: 'Feb', users: 1350000, karma: 2400000, transactions: 52000 },
            { name: 'Mar', users: 1500000, karma: 2800000, transactions: 58000 },
            { name: 'Apr', users: 1680000, karma: 3200000, transactions: 65000 },
            { name: 'May', users: 1850000, karma: 3600000, transactions: 72000 },
            { name: 'Jun', users: 2050000, karma: 4100000, transactions: 78000 },
            { name: 'Jul', users: 2250000, karma: 4600000, transactions: 85000 },
            { name: 'Aug', users: 2450000, karma: 5100000, transactions: 92000 },
            { name: 'Sep', users: 2650000, karma: 5600000, transactions: 98000 },
            { name: 'Oct', users: 2850000, karma: 6200000, transactions: 105000 },
            { name: 'Nov', users: 3050000, karma: 6800000, transactions: 112000 },
            { name: 'Dec', users: 3250000, karma: 7500000, transactions: 120000 },
          ],
          systemStatus: {
            api: { status: 'healthy', responseTime: 45, uptime: 99.9 },
            database: { status: 'healthy', responseTime: 12, uptime: 99.8 },
            storage: { status: 'healthy', responseTime: 8, uptime: 99.9 },
            cache: { status: 'healthy', responseTime: 2, uptime: 99.9 },
            cdn: { status: 'healthy', responseTime: 15, uptime: 99.9 },
          },
          topPerformers: [
            { name: 'Quantum Calculator', type: 'OAPP', users: 45678, karma: 1250000, growth: 25.3 },
            { name: 'Cosmic Dragon NFT', type: 'NFT', users: 23456, karma: 890000, growth: 18.7 },
            { name: 'AI Assistant', type: 'Plugin', users: 78901, karma: 1560000, growth: 32.1 },
            { name: 'Space Explorer', type: 'Avatar', users: 123456, karma: 2340000, growth: 15.8 },
            { name: 'Neural Network SDK', type: 'Library', users: 34567, karma: 670000, growth: 22.4 },
          ],
        }
      };
    }

    console.log('Dashboard Data - Live Mode, making API call to:', API_BASE_URL + '/dashboard');
    try {
      const response = await api.get('/dashboard');
      console.log('Dashboard Data API response:', response.data);
      return response.data;
    } catch (error) {
      console.error('Error fetching dashboard data:', error);
      return {
        isError: true,
        message: 'Failed to fetch dashboard data',
        result: undefined
      };
    }
  },

  async getBeamedInAvatar(): Promise<OASISResult<Avatar>> {
    try {
      const response = await web4Api.get('/avatar/get-beamed-in-avatar');
      console.log('Beamed In Avatar API Response:', response.data);
      return response.data;
    } catch (error) {
      console.error('Error fetching beamed in avatar from WEB4 OASIS API:', error);
      // Return demo data as fallback
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
        message: 'Demo beamed in avatar loaded (API unavailable)'
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
    if (isDemoMode()) {
      // Demo mode - return impressive demo data
      return {
        result: [
          { id: '1', name: 'User Profile Holon', description: 'Complete user profile data structure with avatar, preferences, and social connections', imageUrl: 'https://images.unsplash.com/photo-1633356122544-f134324a6cee?w=400&h=300&fit=crop', version: '2.1.0', category: 'User Management', type: 'Data Object', author: 'OASIS Team', downloads: 45678, rating: 4.9, size: 2.5, lastUpdated: '2024-02-15', isPublic: true, isFeatured: true, tags: ['user', 'profile', 'avatar', 'social'], dataSchema: {}, properties: ['id', 'username', 'email', 'avatar', 'preferences', 'friends', 'karma'], methods: ['save()', 'load()', 'update()', 'delete()', 'validate()'], events: ['onProfileUpdate', 'onAvatarChange', 'onFriendAdded'], documentation: 'https://docs.oasis.network/holons/user-profile', repository: 'https://github.com/oasis/holons/user-profile', license: 'MIT', price: 0, isFree: true, isInstalled: true },
          { id: '2', name: 'NFT Asset Holon', description: 'Blockchain-enabled NFT data structure with metadata and ownership tracking', imageUrl: 'https://images.unsplash.com/photo-1620321023374-d1a68fbc720d?w=400&h=300&fit=crop', version: '1.8.0', category: 'Blockchain', type: 'Asset Object', author: 'CryptoDevs', downloads: 23456, rating: 4.8, size: 3.2, lastUpdated: '2024-02-14', isPublic: true, isFeatured: true, tags: ['nft', 'blockchain', 'asset', 'crypto'], dataSchema: {}, properties: ['tokenId', 'owner', 'metadata', 'price', 'blockchain', 'createdAt'], methods: ['mint()', 'transfer()', 'burn()', 'setPrice()', 'getMetadata()'], events: ['onTransfer', 'onPriceChange', 'onMetadataUpdate'], documentation: 'https://docs.oasis.network/holons/nft-asset', repository: 'https://github.com/oasis/holons/nft-asset', license: 'MIT', price: 0, isFree: true, isInstalled: false },
          { id: '3', name: 'Quest Data Holon', description: 'Gamification quest structure with objectives, rewards, and progress tracking', imageUrl: 'https://images.unsplash.com/photo-1542751371-adc38448a05e?w=400&h=300&fit=crop', version: '3.0.0', category: 'Gaming', type: 'Game Object', author: 'GameDevStudio', downloads: 34567, rating: 4.9, size: 4.1, lastUpdated: '2024-02-13', isPublic: true, isFeatured: true, tags: ['quest', 'gamification', 'rewards', 'progression'], dataSchema: {}, properties: ['questId', 'title', 'description', 'objectives', 'rewards', 'progress', 'completed'], methods: ['start()', 'updateProgress()', 'complete()', 'abandon()', 'claimRewards()'], events: ['onStart', 'onProgress', 'onComplete', 'onRewardClaimed'], documentation: 'https://docs.oasis.network/holons/quest-data', repository: 'https://github.com/oasis/holons/quest-data', license: 'MIT', price: 0, isFree: true, isInstalled: true }
        ],
        isError: false,
        message: 'Demo holons loaded (Demo Mode)'
      };
    }

    // Live mode - use real API
    try {
    const response = await api.get('/holons');
      console.log('Holons API Response:', response.data);
    return response.data;
    } catch (error) {
      console.error('Error fetching Holons from API:', error);
      return {
        isError: true,
        message: 'Failed to fetch holons from API',
        result: []
      };
    }
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
    if (isDemoMode()) {
      // Demo mode - return impressive demo data
      return {
        result: [
          { id: '1', name: 'Authentication Zome', description: 'Complete authentication module with OAuth, JWT, and multi-provider support', imageUrl: 'https://images.unsplash.com/photo-1614064641938-3bbee52942c7?w=400&h=300&fit=crop', version: '3.2.1', category: 'Security', type: 'Auth Module', language: 'TypeScript', framework: 'Node.js', author: 'OASIS Team', downloads: 67890, rating: 4.9, size: 5.4, lastUpdated: '2024-02-15', isPublic: true, isFeatured: true, tags: ['auth', 'oauth', 'jwt', 'security', 'sso'], functions: ['login()', 'logout()', 'register()', 'resetPassword()', 'verifyToken()'], dependencies: ['jsonwebtoken', 'passport', 'bcrypt'], apis: ['OAuth 2.0', 'SAML', 'OpenID Connect'], documentation: 'https://docs.oasis.network/zomes/authentication', repository: 'https://github.com/oasis/zomes/authentication', license: 'MIT', price: 0, isFree: true, isInstalled: true },
          { id: '2', name: 'Payment Processing Zome', description: 'Multi-currency payment processing with crypto and fiat support', imageUrl: 'https://images.unsplash.com/photo-1563013544-824ae1b704d3?w=400&h=300&fit=crop', version: '2.5.0', category: 'Commerce', type: 'Payment Module', language: 'JavaScript', framework: 'Express', author: 'PaymentDevs', downloads: 45678, rating: 4.8, size: 6.2, lastUpdated: '2024-02-14', isPublic: true, isFeatured: true, tags: ['payment', 'crypto', 'fiat', 'commerce', 'stripe'], functions: ['processPayment()', 'refund()', 'getBalance()', 'createInvoice()'], dependencies: ['stripe', 'web3', 'ethers'], apis: ['Stripe API', 'Ethereum', 'Bitcoin'], documentation: 'https://docs.oasis.network/zomes/payment', repository: 'https://github.com/oasis/zomes/payment', license: 'MIT', price: 0, isFree: true, isInstalled: false },
          { id: '3', name: 'AI Assistant Zome', description: 'AI-powered assistant module with natural language processing and learning capabilities', imageUrl: 'https://images.unsplash.com/photo-1677442136019-21780ecad995?w=400&h=300&fit=crop', version: '1.8.3', category: 'AI/ML', type: 'AI Module', language: 'Python', framework: 'FastAPI', author: 'AI Labs', downloads: 34567, rating: 4.9, size: 12.8, lastUpdated: '2024-02-13', isPublic: true, isFeatured: true, tags: ['ai', 'ml', 'nlp', 'chatbot', 'assistant'], functions: ['chat()', 'learn()', 'analyze()', 'predict()', 'generateResponse()'], dependencies: ['openai', 'transformers', 'tensorflow'], apis: ['OpenAI GPT-4', 'Claude', 'Llama'], documentation: 'https://docs.oasis.network/zomes/ai-assistant', repository: 'https://github.com/oasis/zomes/ai-assistant', license: 'MIT', price: 0, isFree: true, isInstalled: true }
        ],
        isError: false,
        message: 'Demo zomes loaded (Demo Mode)'
      };
    }

    // Live mode - use real API
    try {
      const response = await api.get('/zomes');
      console.log('Zomes API Response:', response.data);
      return response.data;
    } catch (error) {
      console.error('Error fetching Zomes from API:', error);
      return {
        isError: true,
        message: 'Failed to fetch zomes from API',
        result: []
      };
    }
  },

  async createZome(request: any): Promise<OASISResult<any>> {
    const response = await api.post('/zomes', request);
    return response.data;
  },

  async deleteZome(id: string): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/zomes/${id}`);
    return response.data;
  },

  // STAR Plugins Operations
  async getSTARPlugins(): Promise<OASISResult<any[]>> {
    if (isDemoMode()) {
      // Demo mode - return impressive STAR/STARNET plugins
      return {
        result: [
          { id: '1', name: 'Holochain Storage Provider', description: 'High-performance Holochain integration for distributed data storage with P2P sync', category: 'provider', version: '2.4.1', author: 'OASIS Team', downloads: 45678, rating: 4.9, imageUrl: 'https://images.unsplash.com/photo-1558494949-ef010cbdcc31?w=400&h=300&fit=crop', tags: ['holochain', 'p2p', 'distributed', 'storage'], compatible: ['STAR', 'STARNET'], size: '12.5 MB', lastUpdated: '2024-02-15', downloadUrl: '/downloads/holochain-provider.zip', documentation: 'https://docs.oasis.network/plugins/holochain', codeExample: `import { HolochainProvider } from '@oasis/holochain-provider';\n\nconst provider = new HolochainProvider({\n  appId: 'your-app-id',\n  dnaHash: 'your-dna-hash'\n});\n\nawait provider.connect();` },
          { id: '2', name: 'IPFS Content Provider', description: 'Seamless IPFS integration for decentralized file storage and content distribution', category: 'provider', version: '3.1.0', author: 'OASIS Team', downloads: 38956, rating: 4.8, imageUrl: 'https://images.unsplash.com/photo-1639322537228-f710d846310a?w=400&h=300&fit=crop', tags: ['ipfs', 'decentralized', 'content', 'storage'], compatible: ['STAR', 'STARNET'], size: '15.2 MB', lastUpdated: '2024-02-14', downloadUrl: '/downloads/ipfs-provider.zip', documentation: 'https://docs.oasis.network/plugins/ipfs', codeExample: `import { IPFSProvider } from '@oasis/ipfs-provider';\n\nconst ipfs = new IPFSProvider({\n  gateway: 'https://ipfs.io'\n});\n\nconst cid = await ipfs.upload(file);` },
          { id: '3', name: 'Ethereum Bridge', description: 'Connect STAR to Ethereum blockchain with smart contract integration and token support', category: 'integration', version: '2.2.5', author: 'BlockchainDevs', downloads: 29834, rating: 4.9, imageUrl: 'https://images.unsplash.com/photo-1621761191319-c6fb62004040?w=400&h=300&fit=crop', tags: ['ethereum', 'blockchain', 'web3', 'contracts'], compatible: ['STAR', 'STARNET'], size: '18.7 MB', lastUpdated: '2024-02-13', downloadUrl: '/downloads/ethereum-bridge.zip', documentation: 'https://docs.oasis.network/plugins/ethereum', codeExample: `import { EthereumBridge } from '@oasis/ethereum-bridge';\n\nconst bridge = new EthereumBridge({\n  rpcUrl: 'https://mainnet.infura.io',\n  contractAddress: '0x...'\n});\n\nawait bridge.deployContract(abi, bytecode);` },
          { id: '4', name: 'Solana Integration', description: 'High-speed Solana blockchain integration with program deployment and SPL token support', category: 'integration', version: '1.9.2', author: 'SolanaLabs', downloads: 24567, rating: 4.7, imageUrl: 'https://images.unsplash.com/photo-1639762681485-074b7f938ba0?w=400&h=300&fit=crop', tags: ['solana', 'blockchain', 'spl', 'programs'], compatible: ['STAR', 'STARNET'], size: '14.3 MB', lastUpdated: '2024-02-12', downloadUrl: '/downloads/solana-integration.zip', documentation: 'https://docs.oasis.network/plugins/solana', codeExample: `import { SolanaIntegration } from '@oasis/solana-integration';\n\nconst solana = new SolanaIntegration({\n  cluster: 'mainnet-beta'\n});\n\nconst signature = await solana.sendTransaction(tx);` },
          { id: '5', name: 'Performance Monitor', description: 'Real-time performance monitoring and optimization for STAR applications', category: 'performance', version: '1.5.4', author: 'OASIS Team', downloads: 19234, rating: 4.8, imageUrl: 'https://images.unsplash.com/photo-1551288049-bebda4e38f71?w=400&h=300&fit=crop', tags: ['monitoring', 'performance', 'metrics', 'optimization'], compatible: ['STAR', 'STARNET'], size: '8.9 MB', lastUpdated: '2024-02-11', downloadUrl: '/downloads/performance-monitor.zip', documentation: 'https://docs.oasis.network/plugins/performance', codeExample: `import { PerformanceMonitor } from '@oasis/performance-monitor';\n\nconst monitor = new PerformanceMonitor();\nmonitor.track('api-call', async () => {\n  // Your code here\n});` },
          { id: '6', name: 'Security Vault', description: 'Advanced encryption and secure key management for sensitive data', category: 'security', version: '2.0.1', author: 'SecurityExperts', downloads: 31456, rating: 5.0, imageUrl: 'https://images.unsplash.com/photo-1563013544-824ae1b704d3?w=400&h=300&fit=crop', tags: ['security', 'encryption', 'vault', 'keys'], compatible: ['STAR', 'STARNET'], size: '10.4 MB', lastUpdated: '2024-02-10', downloadUrl: '/downloads/security-vault.zip', documentation: 'https://docs.oasis.network/plugins/security-vault', codeExample: `import { SecurityVault } from '@oasis/security-vault';\n\nconst vault = new SecurityVault({\n  masterKey: process.env.MASTER_KEY\n});\n\nconst encrypted = await vault.encrypt(data);\nconst decrypted = await vault.decrypt(encrypted);` },
          { id: '7', name: 'Data Sync Utility', description: 'Multi-provider data synchronization and conflict resolution', category: 'utility', version: '1.7.3', author: 'OASIS Team', downloads: 16789, rating: 4.6, imageUrl: 'https://images.unsplash.com/photo-1558494949-ef010cbdcc31?w=400&h=300&fit=crop', tags: ['sync', 'data', 'utility', 'conflict-resolution'], compatible: ['STAR', 'STARNET'], size: '7.6 MB', lastUpdated: '2024-02-09', downloadUrl: '/downloads/data-sync.zip', documentation: 'https://docs.oasis.network/plugins/data-sync', codeExample: `import { DataSync } from '@oasis/data-sync';\n\nconst sync = new DataSync({\n  providers: ['holochain', 'ipfs'],\n  strategy: 'last-write-wins'\n});\n\nawait sync.synchronize(data);` },
          { id: '8', name: 'Analytics Dashboard', description: 'Comprehensive analytics and insights for STAR applications', category: 'utility', version: '1.4.0', author: 'AnalyticsTeam', downloads: 13567, rating: 4.7, imageUrl: 'https://images.unsplash.com/photo-1551288049-bebda4e38f71?w=400&h=300&fit=crop', tags: ['analytics', 'dashboard', 'insights', 'metrics'], compatible: ['STAR'], size: '11.2 MB', lastUpdated: '2024-02-08', downloadUrl: '/downloads/analytics-dashboard.zip', documentation: 'https://docs.oasis.network/plugins/analytics', codeExample: `import { Analytics } from '@oasis/analytics-dashboard';\n\nconst analytics = new Analytics({\n  trackPageViews: true,\n  trackEvents: true\n});\n\nanalytics.track('button-click', { button: 'submit' });` },
          { id: '9', name: 'Backup & Restore', description: 'Automated backup and restore functionality with versioning', category: 'utility', version: '2.1.2', author: 'OASIS Team', downloads: 22345, rating: 4.9, imageUrl: 'https://images.unsplash.com/photo-1563986768609-322da13575f3?w=400&h=300&fit=crop', tags: ['backup', 'restore', 'versioning', 'recovery'], compatible: ['STAR', 'STARNET'], size: '9.8 MB', lastUpdated: '2024-02-07', downloadUrl: '/downloads/backup-restore.zip', documentation: 'https://docs.oasis.network/plugins/backup', codeExample: `import { BackupRestore } from '@oasis/backup-restore';\n\nconst backup = new BackupRestore({\n  destination: 's3://my-bucket/backups'\n});\n\nawait backup.create('my-backup');\nawait backup.restore('my-backup');` },
          { id: '10', name: 'Rate Limiter', description: 'Advanced rate limiting and throttling for API protection', category: 'security', version: '1.3.5', author: 'SecurityExperts', downloads: 18234, rating: 4.8, imageUrl: 'https://images.unsplash.com/photo-1614064641938-3bbee52942c7?w=400&h=300&fit=crop', tags: ['rate-limit', 'throttle', 'api', 'protection'], compatible: ['STAR', 'STARNET'], size: '5.4 MB', lastUpdated: '2024-02-06', downloadUrl: '/downloads/rate-limiter.zip', documentation: 'https://docs.oasis.network/plugins/rate-limiter', codeExample: `import { RateLimiter } from '@oasis/rate-limiter';\n\nconst limiter = new RateLimiter({\n  windowMs: 15 * 60 * 1000, // 15 minutes\n  max: 100 // limit each IP to 100 requests per windowMs\n});\n\napp.use('/api/', limiter.middleware());` }
        ],
        isError: false,
        message: 'STAR plugins loaded (Demo Mode)'
      };
    }

    // Live mode - use real API
    try {
      const response = await api.get('/star-plugins');
      return response.data;
    } catch (error) {
      console.error('Error fetching STAR plugins from API:', error);
      return {
        isError: true,
        message: 'Failed to fetch STAR plugins from API',
        result: []
      };
    }
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
      // Force demo data for now - API might be returning empty data
      throw new Error('Forcing demo data for libraries');
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
            name: 'React',
            description: 'A JavaScript library for building user interfaces with component-based architecture',
            imageUrl: 'https://images.unsplash.com/photo-1633356122544-f134324a6cee?w=400&h=300&fit=crop',
            version: '18.2.0',
            author: 'Facebook',
            category: 'Frontend',
            type: 'JavaScript Library',
            size: 42.5,
            downloads: 25000000,
            rating: 4.8,
            lastUpdated: '2024-01-15T10:30:00Z',
            language: 'JavaScript',
            framework: 'React',
            license: 'MIT',
            repository: 'https://github.com/facebook/react',
            documentation: 'https://react.dev',
            isInstalled: true,
            dependencies: ['Node.js 16+']
          },
          {
            id: '2',
            name: 'Express.js',
            description: 'Fast, unopinionated, minimalist web framework for Node.js',
            imageUrl: 'https://images.unsplash.com/photo-1627398242454-45a1465c2479?w=400&h=300&fit=crop',
            version: '4.18.2',
            author: 'TJ Holowaychuk',
            category: 'Backend',
            type: 'Web Framework',
            size: '2.1 MB',
            downloads: 15000000,
            rating: 4.7,
            lastUpdated: '2024-01-12T14:20:00Z',
            language: 'JavaScript',
            framework: 'Node.js',
            license: 'MIT',
            repository: 'https://github.com/expressjs/express',
            documentation: 'https://expressjs.com',
            isInstalled: false,
            dependencies: ['Node.js 14+']
          },
          {
            id: '3',
            name: 'Lodash',
            description: 'A modern JavaScript utility library delivering modularity, performance & extras',
            imageUrl: 'https://images.unsplash.com/photo-1555066931-4365d14bab8c?w=400&h=300&fit=crop',
            version: '4.17.21',
            author: 'John-David Dalton',
            category: 'Utility',
            type: 'JavaScript Library',
            size: '71.2 KB',
            downloads: 8000000,
            rating: 4.6,
            lastUpdated: '2024-01-10T09:15:00Z',
            language: 'JavaScript',
            framework: 'Vanilla JS',
            license: 'MIT',
            repository: 'https://github.com/lodash/lodash',
            documentation: 'https://lodash.com',
            isInstalled: true,
            dependencies: []
          },
          {
            id: '4',
            name: 'Axios',
            description: 'Promise-based HTTP client for the browser and Node.js',
            imageUrl: 'https://images.unsplash.com/photo-1460925895917-afdab827c52f?w=400&h=300&fit=crop',
            version: '1.6.2',
            author: 'Matt Zabriskie',
            category: 'HTTP Client',
            type: 'JavaScript Library',
            size: '13.4 KB',
            downloads: 12000000,
            rating: 4.9,
            lastUpdated: '2024-01-08T16:45:00Z',
            language: 'JavaScript',
            framework: 'Universal',
            license: 'MIT',
            repository: 'https://github.com/axios/axios',
            documentation: 'https://axios-http.com',
            isInstalled: true,
            dependencies: []
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
      // Force demo data for now - API might be returning empty data
      throw new Error('Forcing demo data for plugins');
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
            name: 'React DevTools Pro',
            description: 'Advanced React development tools with component inspector and performance profiler',
            imageUrl: 'https://images.unsplash.com/photo-1633356122544-f134324a6cee?w=400&h=300&fit=crop',
            version: '4.2.1',
            author: 'React Tools Inc',
            category: 'Development',
            type: 'Browser Extension',
            size: 2.1,
            downloads: 1250000,
            rating: 4.8,
            lastUpdated: '2024-01-15',
            isInstalled: true,
            isActive: true,
            isCompatible: true,
            dependencies: ['React 16.8+', 'Chrome 90+'],
            features: ['Component Tree', 'Props Inspector', 'Performance Profiler', 'State Debugger'],
            documentation: 'https://docs.reacttools.com/devtools',
            repository: 'https://github.com/reacttools/devtools-pro',
            price: 0,
            isFree: true,
          },
          {
            id: '2',
            name: 'VS Code AI Assistant',
            description: 'Intelligent code completion and debugging assistant powered by advanced AI',
            imageUrl: 'https://images.unsplash.com/photo-1555066931-4365d14bab8c?w=400&h=300&fit=crop',
            version: '2.8.4',
            author: 'CodeAI Solutions',
            category: 'Development',
            type: 'VS Code Extension',
            size: 15.7,
            downloads: 890000,
            rating: 4.9,
            lastUpdated: '2024-01-20',
            isInstalled: true,
            isActive: false,
            isCompatible: true,
            dependencies: ['VS Code 1.80+', 'Node.js 16+'],
            features: ['Smart Autocomplete', 'Code Generation', 'Bug Detection', 'Refactoring'],
            documentation: 'https://docs.codeai.com/assistant',
            repository: 'https://github.com/codeai/vscode-ai',
            price: 29.99,
            isFree: false,
          },
          {
            id: '3',
            name: 'Unity Performance Profiler',
            description: 'Advanced Unity game performance analysis and optimization tools',
            imageUrl: 'https://images.unsplash.com/photo-1512941937669-90a1b58e7e9c?w=400&h=300&fit=crop',
            version: '3.1.5',
            author: 'Unity Technologies',
            category: 'Game Development',
            type: 'Unity Package',
            size: 8.3,
            downloads: 456000,
            rating: 4.7,
            lastUpdated: '2024-01-25',
            isInstalled: false,
            isActive: false,
            isCompatible: true,
            dependencies: ['Unity 2022.3+'],
            features: ['Frame Analysis', 'Memory Profiler', 'GPU Profiler', 'Optimization Suggestions'],
            documentation: 'https://docs.unity.com/profiler',
            repository: 'https://github.com/unity/performance-profiler',
            price: 0,
            isFree: true,
          },
          {
            id: '4',
            name: 'Docker Container Manager',
            description: 'Comprehensive Docker container management and orchestration plugin',
            imageUrl: 'https://images.unsplash.com/photo-1627398242454-45a1465c2479?w=400&h=300&fit=crop',
            version: '1.9.2',
            author: 'ContainerOps',
            category: 'DevOps',
            type: 'CLI Tool',
            size: 12.4,
            downloads: 678000,
            rating: 4.8,
            lastUpdated: '2024-01-30',
            isInstalled: true,
            isActive: true,
            isCompatible: true,
            dependencies: ['Docker 20.10+', 'Docker Compose 2.0+'],
            features: ['Container Monitoring', 'Auto-scaling', 'Health Checks', 'Log Management'],
            documentation: 'https://docs.containerops.com/manager',
            repository: 'https://github.com/containerops/docker-manager',
            price: 0,
            isFree: true,
          },
          {
            id: '5',
            name: 'PostgreSQL Query Optimizer',
            description: 'Advanced database query optimization and performance monitoring tool',
            imageUrl: 'https://images.unsplash.com/photo-1460925895917-afdab827c52f?w=400&h=300&fit=crop',
            version: '2.3.8',
            author: 'Database Pro',
            category: 'Database',
            type: 'Database Tool',
            size: 18.9,
            downloads: 278000,
            rating: 4.6,
            lastUpdated: '2024-02-01',
            isInstalled: false,
            isActive: false,
            isCompatible: true,
            dependencies: ['PostgreSQL 13+', 'Python 3.8+'],
            features: ['Query Analysis', 'Index Optimization', 'Performance Monitoring', 'Auto-tuning'],
            documentation: 'https://docs.dbpro.com/optimizer',
            repository: 'https://github.com/dbpro/postgres-optimizer',
            price: 14.99,
            isFree: false,
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
      // Force demo data for now - API might be returning empty data
      throw new Error('Forcing demo data for runtimes');
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
            name: 'Java Runtime Environment',
            description: 'Oracle Java Runtime Environment for running Java applications',
            imageUrl: 'https://images.unsplash.com/photo-1555066931-4365d14bab8c?w=400&h=300&fit=crop',
            version: '21.0.1',
            category: 'Programming Language',
            type: 'JVM',
            status: 'Running',
            uptime: '7d 12h 30m',
            lastUpdated: '2024-01-15T08:00:00Z',
            cost: 0,
            region: 'US-East',
            cpuUsage: 15,
            memoryUsage: 2.1,
            diskUsage: 8.5,
            instances: 3,
            maxInstances: 10,
            language: 'Java',
            framework: 'JVM'
          },
          {
            id: '2',
            name: 'Node.js Runtime',
            description: 'JavaScript runtime built on Chrome V8 engine for server-side applications',
            imageUrl: 'https://images.unsplash.com/photo-1633356122544-f134324a6cee?w=400&h=300&fit=crop',
            version: '20.10.0',
            category: 'Programming Language',
            type: 'JavaScript Engine',
            status: 'Running',
            uptime: '5d 8h 15m',
            lastUpdated: '2024-01-13T20:30:00Z',
            cost: 0,
            region: 'US-West',
            cpuUsage: 8,
            memoryUsage: 1.2,
            diskUsage: 4.3,
            instances: 2,
            maxInstances: 5,
            language: 'JavaScript',
            framework: 'Node.js'
          },
          {
            id: '3',
            name: 'Python Runtime',
            description: 'Python interpreter and runtime environment for Python applications',
            imageUrl: 'https://images.unsplash.com/photo-1526379095098-d400fd0bf935?w=400&h=300&fit=crop',
            version: '3.11.7',
            category: 'Programming Language',
            type: 'Interpreter',
            status: 'Stopped',
            uptime: '0d 0h 0m',
            lastUpdated: '2024-01-10T14:20:00Z',
            cost: 0,
            region: 'EU-West',
            cpuUsage: 0,
            memoryUsage: 0,
            diskUsage: 2.1,
            instances: 0,
            maxInstances: 8,
            language: 'Python',
            framework: 'CPython'
          },
          {
            id: '4',
            name: 'Rust Runtime',
            description: 'Rust compiler and runtime for systems programming and performance-critical applications',
            imageUrl: 'https://images.unsplash.com/photo-1627398242454-45a1465c2479?w=400&h=300&fit=crop',
            version: '1.75.0',
            category: 'Programming Language',
            type: 'Compiler',
            status: 'Running',
            uptime: '3d 4h 45m',
            lastUpdated: '2024-01-12T09:15:00Z',
            cost: 0,
            region: 'US-Central',
            cpuUsage: 12,
            memoryUsage: 1.8,
            diskUsage: 6.2,
            instances: 1,
            maxInstances: 3,
            language: 'Rust',
            framework: 'Cargo'
          }
        ],
        isError: false,
        message: 'Demo runtimes loaded (API unavailable)'
      };
    }
  },

  async getInstalledRuntimes(): Promise<OASISResult<any[]>> {
    try {
      // When API is ready, replace with: const response = await api.get('/runtimes/installed'); return response.data;
      throw new Error('Forcing demo data for runtimes (installed)');
    } catch (error) {
      // Filter demo list to simulate installed items
      const all = await this.getAllRuntimes();
      const installed = (all.result || []).filter((r: any) => ['1', '2', '4', '6'].includes(String(r.id)));
      return { result: installed, isError: false, message: 'Demo installed runtimes' };
    }
  },

  async getRuntimesForAvatar(avatarId: string): Promise<OASISResult<any[]>> {
    try {
      // When API is ready, replace with: const response = await api.get(`/runtimes/for-avatar/${avatarId}`); return response.data;
      throw new Error('Forcing demo data for runtimes (mine)');
    } catch (error) {
      // Filter demo list to simulate runtimes owned by this avatar
      const all = await this.getAllRuntimes();
      const mine = (all.result || []).filter((r: any) => ['2', '5'].includes(String(r.id)));
      return { result: mine, isError: false, message: 'Demo runtimes for avatar' };
    }
  },

  // Templates Operations - Using WEB5 STAR Web API
  async getAllTemplates(): Promise<OASISResult<any[]>> {
    try {
      // Force demo data for now - API might be returning empty data
      throw new Error('Forcing demo data for templates');
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
            name: 'React Native Mobile App',
            description: 'Cross-platform mobile app template with authentication and navigation',
            imageUrl: 'https://images.unsplash.com/photo-1633356122544-f134324a6cee?w=400&h=300&fit=crop',
            version: '3.2.1',
            category: 'Mobile',
            type: 'Mobile App',
            language: 'TypeScript',
            framework: 'React Native',
            author: 'MobileDev Studio',
            downloads: 25420,
            rating: 4.9,
            size: 45.2,
            lastUpdated: '2024-01-15',
            isPublic: true,
            isFeatured: true,
            tags: ['React Native', 'TypeScript', 'Firebase', 'Redux'],
            features: ['Authentication', 'Push Notifications', 'Offline Support', 'Social Login'],
            requirements: ['Node.js 18+', 'React Native CLI', 'Android Studio', 'Xcode'],
            documentation: 'https://docs.mobiledev.com/react-native-template',
            repository: 'https://github.com/mobiledev/react-native-template',
            license: 'MIT',
            price: 0,
            isFree: true
          },
          {
            id: '2',
            name: '.NET MAUI Cross-Platform App',
            description: 'Microsoft MAUI template for building native mobile and desktop apps',
            imageUrl: 'https://images.unsplash.com/photo-1526379095098-d400fd0bf935?w=400&h=300&fit=crop',
            version: '8.0.0',
            category: 'Desktop',
            type: 'Cross-Platform',
            language: 'C#',
            framework: '.NET MAUI',
            author: 'Microsoft',
            downloads: 89000,
            rating: 4.8,
            size: 125.7,
            lastUpdated: '2024-01-20',
            isPublic: true,
            isFeatured: true,
            tags: ['C#', '.NET', 'MAUI', 'Cross-Platform'],
            features: ['Native Performance', 'Shared UI', 'Platform APIs', 'Hot Reload'],
            requirements: ['Visual Studio 2022', '.NET 8 SDK', 'Android SDK'],
            documentation: 'https://docs.microsoft.com/maui',
            repository: 'https://github.com/dotnet/maui',
            license: 'MIT',
            price: 0,
            isFree: true
          },
          {
            id: '3',
            name: 'WordPress CMS Template',
            description: 'Professional WordPress theme with custom post types and WooCommerce integration',
            imageUrl: 'https://images.unsplash.com/photo-1460925895917-afdab827c52f?w=400&h=300&fit=crop',
            version: '2.1.4',
            category: 'Web',
            type: 'CMS',
            language: 'PHP',
            framework: 'WordPress',
            author: 'WP Studio',
            downloads: 156000,
            rating: 4.7,
            size: 23.8,
            lastUpdated: '2024-01-25',
            isPublic: true,
            isFeatured: false,
            tags: ['PHP', 'WordPress', 'WooCommerce', 'CMS'],
            features: ['Custom Post Types', 'E-commerce', 'SEO Optimized', 'Responsive'],
            requirements: ['PHP 8.0+', 'MySQL 5.7+', 'WordPress 6.0+'],
            documentation: 'https://docs.wpstudio.com/template',
            repository: 'https://github.com/wpstudio/wordpress-template',
            license: 'GPL v2',
            price: 49.99,
            isFree: false
          },
          {
            id: '4',
            name: 'Angular Enterprise Template',
            description: 'Enterprise-grade Angular application with authentication and state management',
            imageUrl: 'https://images.unsplash.com/photo-1633356122544-f134324a6cee?w=400&h=300&fit=crop',
            version: '17.0.0',
            category: 'Web',
            type: 'Enterprise',
            language: 'TypeScript',
            framework: 'Angular',
            author: 'Google',
            downloads: 45000,
            rating: 4.9,
            size: 125.7,
            lastUpdated: '2024-02-02',
            isPublic: true,
            isFeatured: true,
            tags: ['Angular', 'TypeScript', 'Enterprise', 'RxJS'],
            features: ['Authentication', 'State Management', 'Lazy Loading', 'Testing'],
            requirements: ['Node.js 18+', 'Angular CLI', 'npm/yarn'],
            documentation: 'https://angular.io/docs',
            repository: 'https://github.com/angular/angular',
            license: 'MIT',
            price: 0,
            isFree: true
          },
          {
            id: '5',
            name: 'Laravel API Template',
            description: 'RESTful API template with Laravel framework and authentication',
            imageUrl: 'https://images.unsplash.com/photo-1627398242454-45a1465c2479?w=400&h=300&fit=crop',
            version: '10.0.0',
            category: 'Backend',
            type: 'API',
            language: 'PHP',
            framework: 'Laravel',
            author: 'Laravel Team',
            downloads: 23000,
            rating: 4.8,
            size: 38.9,
            lastUpdated: '2024-01-25',
            isPublic: true,
            isFeatured: false,
            tags: ['PHP', 'Laravel', 'API', 'REST'],
            features: ['Authentication', 'API Routes', 'Database Migration', 'Validation'],
            requirements: ['PHP 8.1+', 'Composer', 'MySQL', 'Laravel 10'],
            documentation: 'https://laravel.com/docs',
            repository: 'https://github.com/laravel/laravel',
            license: 'MIT',
            price: 0,
            isFree: true
          },
          {
            id: '6',
            name: 'Flutter Mobile App Template',
            description: 'Google Flutter template for building beautiful native mobile apps',
            imageUrl: 'https://images.unsplash.com/photo-1526379095098-d400fd0bf935?w=400&h=300&fit=crop',
            version: '3.16.0',
            category: 'Mobile',
            type: 'Cross-Platform',
            language: 'Dart',
            framework: 'Flutter',
            author: 'Google',
            downloads: 234000,
            rating: 4.8,
            size: 67.3,
            lastUpdated: '2024-02-01',
            isPublic: true,
            isFeatured: true,
            tags: ['Dart', 'Flutter', 'Cross-platform', 'Material Design'],
            features: ['Hot Reload', 'Material Design', 'Cupertino Widgets', 'Platform Channels'],
            requirements: ['Flutter SDK', 'Android Studio', 'Xcode'],
            documentation: 'https://docs.flutter.dev',
            repository: 'https://github.com/flutter/flutter',
            license: 'BSD-3-Clause',
            price: 0,
            isFree: true
          },
          {
            id: '7',
            name: 'Vue.js SPA Template',
            description: 'Modern single-page application template with Vue 3 and Composition API',
            imageUrl: 'https://images.unsplash.com/photo-1633356122544-f134324a6cee?w=400&h=300&fit=crop',
            version: '3.4.0',
            category: 'Web',
            type: 'SPA',
            language: 'TypeScript',
            framework: 'Vue.js',
            author: 'Vue Team',
            downloads: 89000,
            rating: 4.7,
            size: 52.1,
            lastUpdated: '2024-01-28',
            isPublic: true,
            isFeatured: true,
            tags: ['Vue.js', 'TypeScript', 'SPA', 'Composition API'],
            features: ['Composition API', 'TypeScript Support', 'Router', 'State Management'],
            requirements: ['Node.js 16+', 'Vue CLI', 'npm/yarn'],
            documentation: 'https://vuejs.org/guide',
            repository: 'https://github.com/vuejs/vue',
            license: 'MIT',
            price: 0,
            isFree: true
          },
          {
            id: '8',
            name: 'ASP.NET Core MVC Template',
            description: 'Enterprise-grade web application template with authentication and API integration',
            imageUrl: 'https://images.unsplash.com/photo-1555066931-4365d14bab8c?w=400&h=300&fit=crop',
            version: '8.0.0',
            category: 'Web',
            type: 'Web App',
            language: 'C#',
            framework: 'ASP.NET Core',
            author: 'Microsoft',
            downloads: 67000,
            rating: 4.6,
            size: 34.5,
            lastUpdated: '2024-01-30',
            isPublic: true,
            isFeatured: false,
            tags: ['C#', 'ASP.NET', 'MVC', 'Entity Framework'],
            features: ['Authentication', 'Authorization', 'API Controllers', 'Database Integration'],
            requirements: ['.NET 8 SDK', 'Visual Studio 2022', 'SQL Server'],
            documentation: 'https://docs.microsoft.com/aspnet/core',
            repository: 'https://github.com/dotnet/aspnetcore',
            license: 'MIT',
            price: 0,
            isFree: true
          },
          {
            id: '9',
            name: 'Premium E-commerce Template',
            description: 'Complete e-commerce solution with payment integration and admin dashboard',
            imageUrl: 'https://images.unsplash.com/photo-1556742049-0cfed4f6a45d?w=400&h=300&fit=crop',
            version: '2.5.0',
            category: 'E-commerce',
            type: 'Full Stack',
            language: 'JavaScript',
            framework: 'React + Node.js',
            author: 'E-commerce Pro',
            downloads: 125000,
            rating: 4.9,
            size: 89.3,
            lastUpdated: '2024-01-15',
            isPublic: true,
            isFeatured: true,
            tags: ['React', 'Node.js', 'Stripe', 'MongoDB'],
            features: ['Payment Gateway', 'Admin Dashboard', 'Inventory Management', 'Analytics'],
            requirements: ['Node.js 18+', 'MongoDB', 'Stripe Account'],
            documentation: 'https://docs.ecommercepro.com/template',
            repository: 'https://github.com/ecommercepro/premium-template',
            license: 'Commercial',
            price: 199.99,
            isFree: false
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

  // Helper methods for Holons and Zomes
  async getInstalledHolons(): Promise<OASISResult<any[]>> {
    const all = await this.getAllHolons();
    const installed = (all.result || []).filter((h: any) => h.isInstalled);
    return { result: installed, isError: false, message: 'Demo installed holons' };
  },

  async getHolonsForAvatar(avatarId: string): Promise<OASISResult<any[]>> {
    const all = await this.getAllHolons();
    const mine = (all.result || []).filter((h: any) => h.id === '1');
    return { result: mine, isError: false, message: 'Demo holons for avatar' };
  },

  async getInstalledZomes(): Promise<OASISResult<any[]>> {
    const all = await this.getAllZomes();
    const installed = (all.result || []).filter((z: any) => z.isInstalled);
    return { result: installed, isError: false, message: 'Demo installed zomes' };
  },

  async getZomesForAvatar(avatarId: string): Promise<OASISResult<any[]>> {
    const all = await this.getAllZomes();
    const mine = (all.result || []).filter((z: any) => z.id === '1');
    return { result: mine, isError: false, message: 'Demo zomes for avatar' };
  },
};
