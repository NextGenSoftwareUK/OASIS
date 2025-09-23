import axios from 'axios';
import { OASISResult, STARStatus, Avatar, Karma } from '../types/star';

const API_BASE_URL = process.env.REACT_APP_API_URL || 'https://localhost:7001/api';

const api = axios.create({
  baseURL: API_BASE_URL,
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
    return response.data;
  },

  async extinguishStar(): Promise<OASISResult<boolean>> {
    const response = await api.post('/star/extinguish');
    return response.data;
  },

  async getSTARStatus(): Promise<STARStatus> {
    const response = await api.get('/star/status');
    const result: OASISResult<boolean> = response.data;
    return {
      isIgnited: result.result || false,
      lastUpdated: new Date(),
    };
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

  async createAvatar(username: string, email: string, password: string): Promise<OASISResult<Avatar>> {
    const response = await api.post('/star/avatar/create', {
      username,
      email,
      password,
    });
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
};
