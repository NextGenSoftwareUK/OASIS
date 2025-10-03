/**
 * OASIS Web4 API Client
 * Main client class for interacting with the OASIS Web4 API
 */

import axios, { AxiosInstance, AxiosError } from 'axios';
import { 
  OASISConfig, 
  OASISResult, 
  Avatar, 
  Karma, 
  NFT, 
  AuthResponse,
  CreateAvatarRequest,
  UpdateAvatarRequest,
  AddKarmaRequest,]
  MintNFTRequest,
  DataObject,
  Message,
  Provider
} from './types';

export class OASISWeb4Client {
  private api: AxiosInstance;
  private config: OASISConfig;
  private authToken: string | null = null;

  constructor(config?: Partial<OASISConfig>) {
    this.config = {
      apiUrl: config?.apiUrl || process.env.OASIS_WEB4_API_URL || 'http://localhost:5000/api',
      timeout: config?.timeout || 30000,
      debug: config?.debug || false,
      autoRetry: config?.autoRetry || true,
      maxRetries: config?.maxRetries || 3
    };

    this.api = axios.create({
      baseURL: this.config.apiUrl,
      timeout: this.config.timeout,
      headers: {
        'Content-Type': 'application/json'
      }
    });

    this.setupInterceptors();
  }

  private setupInterceptors(): void {
    // Request interceptor
    this.api.interceptors.request.use(
      (config) => {
        if (this.authToken) {
          config.headers.Authorization = `Bearer ${this.authToken}`;
        }
        if (this.config.debug) {
          console.log('[OASIS Web4] Request:', config.method?.toUpperCase(), config.url);
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    // Response interceptor
    this.api.interceptors.response.use(
      (response) => {
        if (this.config.debug) {
          console.log('[OASIS Web4] Response:', response.status, response.config.url);
        }
        return response;
      },
      async (error: AxiosError) => {
        if (this.config.autoRetry && this.shouldRetry(error)) {
          return this.retryRequest(error);
        }
        return Promise.reject(error);
      }
    );
  }

  private shouldRetry(error: AxiosError): boolean {
    return !!(
      error.config &&
      error.response?.status &&
      [408, 429, 500, 502, 503, 504].includes(error.response.status)
    );
  }

  private async retryRequest(error: AxiosError, retryCount = 0): Promise<any> {
    if (retryCount >= (this.config.maxRetries || 3)) {
      return Promise.reject(error);
    }

    await new Promise(resolve => setTimeout(resolve, Math.pow(2, retryCount) * 1000));
    
    try {
      return await this.api.request(error.config!);
    } catch (err) {
      return this.retryRequest(err as AxiosError, retryCount + 1);
    }
  }

  // Authentication Methods
  async authenticate(provider: string, credentials?: any): Promise<OASISResult<AuthResponse>> {
    try {
      const response = await this.api.post('/avatar/authenticate', {
        provider,
        ...credentials
      });
      
      if (response.data.result?.token) {
        this.authToken = response.data.result.token;
      }
      
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async authenticateWithProvider(provider: string): Promise<OASISResult<Avatar>> {
    return this.authenticate(provider);
  }

  async logout(): Promise<OASISResult<boolean>> {
    try {
      const response = await this.api.post('/avatar/logout');
      this.authToken = null;
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  setAuthToken(token: string): void {
    this.authToken = token;
  }

  clearAuthToken(): void {
    this.authToken = null;
  }

  // Avatar Methods
  async getAvatar(id: string): Promise<OASISResult<Avatar>> {
    try {
      const response = await this.api.get(`/avatar/${id}`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async getAvatarByUsername(username: string): Promise<OASISResult<Avatar>> {
    try {
      const response = await this.api.get(`/avatar/username/${username}`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async getAvatarByEmail(email: string): Promise<OASISResult<Avatar>> {
    try {
      const response = await this.api.get(`/avatar/email/${email}`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async createAvatar(data: CreateAvatarRequest): Promise<OASISResult<Avatar>> {
    try {
      const response = await this.api.post('/avatar', data);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async updateAvatar(id: string, data: UpdateAvatarRequest): Promise<OASISResult<Avatar>> {
    try {
      const response = await this.api.put(`/avatar/${id}`, data);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async deleteAvatar(id: string): Promise<OASISResult<boolean>> {
    try {
      const response = await this.api.delete(`/avatar/${id}`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async searchAvatars(query: string): Promise<OASISResult<Avatar[]>> {
    try {
      const response = await this.api.get(`/avatar/search?q=${encodeURIComponent(query)}`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  // Karma Methods
  async getKarma(avatarId: string): Promise<OASISResult<Karma>> {
    try {
      const response = await this.api.get(`/avatar/${avatarId}/karma`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async addKarma(avatarId: string, request: AddKarmaRequest): Promise<OASISResult<Karma>> {
    try {
      const response = await this.api.post(`/avatar/${avatarId}/karma`, request);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async removeKarma(avatarId: string, request: AddKarmaRequest): Promise<OASISResult<Karma>> {
    try {
      const response = await this.api.delete(`/avatar/${avatarId}/karma`, { data: request });
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async getKarmaHistory(avatarId: string, limit = 50): Promise<OASISResult<any[]>> {
    try {
      const response = await this.api.get(`/avatar/${avatarId}/karma/history?limit=${limit}`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async getKarmaLeaderboard(timeRange = 'all', limit = 100): Promise<OASISResult<Avatar[]>> {
    try {
      const response = await this.api.get(`/karma/leaderboard?range=${timeRange}&limit=${limit}`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  // NFT Methods
  async getNFTs(avatarId: string): Promise<OASISResult<NFT[]>> {
    try {
      const response = await this.api.get(`/nft?avatarId=${avatarId}`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async getNFT(nftId: string): Promise<OASISResult<NFT>> {
    try {
      const response = await this.api.get(`/nft/${nftId}`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async mintNFT(avatarId: string, request: MintNFTRequest): Promise<OASISResult<NFT>> {
    try {
      const response = await this.api.post('/nft/mint', { avatarId, ...request });
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async transferNFT(nftId: string, toAvatarId: string): Promise<OASISResult<NFT>> {
    try {
      const response = await this.api.post(`/nft/${nftId}/transfer`, { toAvatarId });
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async burnNFT(nftId: string): Promise<OASISResult<boolean>> {
    try {
      const response = await this.api.delete(`/nft/${nftId}`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  // Data Storage Methods
  async getData(avatarId: string, key?: string): Promise<OASISResult<DataObject>> {
    try {
      const url = key ? `/data/${avatarId}/${key}` : `/data/${avatarId}`;
      const response = await this.api.get(url);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async saveData(avatarId: string, key: string, value: any): Promise<OASISResult<DataObject>> {
    try {
      const response = await this.api.post(`/data/${avatarId}`, { key, value });
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async deleteData(avatarId: string, key: string): Promise<OASISResult<boolean>> {
    try {
      const response = await this.api.delete(`/data/${avatarId}/${key}`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  // Provider Management
  async getAvailableProviders(): Promise<OASISResult<Provider[]>> {
    try {
      const response = await this.api.get('/providers');
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async getCurrentProvider(): Promise<OASISResult<Provider>> {
    try {
      const response = await this.api.get('/providers/current');
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async switchProvider(providerName: string): Promise<OASISResult<Provider>> {
    try {
      const response = await this.api.post('/providers/switch', { provider: providerName });
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  // Messaging Methods
  async getChatMessages(chatId: string, limit = 100): Promise<OASISResult<Message[]>> {
    try {
      const response = await this.api.get(`/chat/${chatId}/messages?limit=${limit}`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async sendMessage(chatId: string, avatarId: string, content: string): Promise<OASISResult<Message>> {
    try {
      const response = await this.api.post('/chat/messages', { chatId, avatarId, content });
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  // Social Features
  async getSocialFeed(feedType: string, avatarId?: string): Promise<OASISResult<any[]>> {
    try {
      const url = avatarId 
        ? `/social/feed?type=${feedType}&avatarId=${avatarId}`
        : `/social/feed?type=${feedType}`;
      const response = await this.api.get(url);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async getFriends(avatarId: string): Promise<OASISResult<Avatar[]>> {
    try {
      const response = await this.api.get(`/avatar/${avatarId}/friends`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async addFriend(avatarId: string, friendId: string): Promise<OASISResult<boolean>> {
    try {
      const response = await this.api.post(`/avatar/${avatarId}/friends`, { friendId });
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async removeFriend(avatarId: string, friendId: string): Promise<OASISResult<boolean>> {
    try {
      const response = await this.api.delete(`/avatar/${avatarId}/friends/${friendId}`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async getAchievements(avatarId: string): Promise<OASISResult<any[]>> {
    try {
      const response = await this.api.get(`/avatar/${avatarId}/achievements`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async likePost(postId: string): Promise<OASISResult<boolean>> {
    try {
      const response = await this.api.post(`/social/posts/${postId}/like`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  // Settings
  async getSettings(avatarId: string): Promise<OASISResult<any>> {
    try {
      const response = await this.api.get(`/avatar/${avatarId}/settings`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async saveSettings(avatarId: string, settings: any): Promise<OASISResult<any>> {
    try {
      const response = await this.api.put(`/avatar/${avatarId}/settings`, settings);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  // Error handling
  private handleError(error: any): OASISResult<any> {
    console.error('[OASIS Web4] Error:', error);
    
    return {
      isError: true,
      message: error.response?.data?.message || error.message || 'Unknown error occurred',
      result: null
    };
  }
}

export default OASISWeb4Client;
