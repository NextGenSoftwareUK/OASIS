/**
 * OASIS Web5 STAR API Client
 * Main client class for interacting with the OASIS Web5 STAR API
 */

import axios, { AxiosInstance, AxiosError } from 'axios';
import {
  STARConfig,
  OASISResult,
  STARStatus,
  OAPP,
  Mission,
  Quest,
  Chapter,
  STARPlugin,
  Holon,
  Zome,
  OAPPTemplate,
  CreateOAPPRequest,
  CreateMissionRequest,
  CreateQuestRequest,
  ProgressUpdate
} from './types';

export class OASISWeb5STARClient {
  private api: AxiosInstance;
  private config: STARConfig;
  private authToken: string | null = null;

  constructor(config?: Partial<STARConfig>) {
    this.config = {
      apiUrl: config?.apiUrl || process.env.STAR_API_URL || 'http://localhost:50564/api',
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
          console.log('[OASIS STAR] Request:', config.method?.toUpperCase(), config.url);
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    // Response interceptor
    this.api.interceptors.response.use(
      (response) => {
        if (this.config.debug) {
          console.log('[OASIS STAR] Response:', response.status, response.config.url);
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

  setAuthToken(token: string): void {
    this.authToken = token;
  }

  clearAuthToken(): void {
    this.authToken = null;
  }

  // STAR Core Operations
  async igniteSTAR(): Promise<OASISResult<STARStatus>> {
    try {
      const response = await this.api.post('/star/ignite');
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async extinguishSTAR(): Promise<OASISResult<boolean>> {
    try {
      const response = await this.api.post('/star/extinguish');
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async getSTARStatus(): Promise<OASISResult<STARStatus>> {
    try {
      const response = await this.api.get('/star/status');
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async lightSTAR(): Promise<OASISResult<STARStatus>> {
    try {
      const response = await this.api.post('/star/light');
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async evolveSTAR(): Promise<OASISResult<any>> {
    try {
      const response = await this.api.post('/star/evolve');
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  // OAPP Management
  async getAllOAPPs(): Promise<OASISResult<OAPP[]>> {
    try {
      const response = await this.api.get('/oapps');
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async getOAPP(id: string): Promise<OASISResult<OAPP>> {
    try {
      const response = await this.api.get(`/oapps/${id}`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async createOAPP(request: CreateOAPPRequest): Promise<OASISResult<OAPP>> {
    try {
      const response = await this.api.post('/oapps', request);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async updateOAPP(id: string, updates: Partial<OAPP>): Promise<OASISResult<OAPP>> {
    try {
      const response = await this.api.put(`/oapps/${id}`, updates);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async deleteOAPP(id: string): Promise<OASISResult<boolean>> {
    try {
      const response = await this.api.delete(`/oapps/${id}`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async publishOAPP(id: string): Promise<OASISResult<OAPP>> {
    try {
      const response = await this.api.post(`/oapps/${id}/publish`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async installOAPP(id: string, avatarId: string): Promise<OASISResult<boolean>> {
    try {
      const response = await this.api.post(`/oapps/${id}/install`, { avatarId });
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  // Mission Management
  async getAllMissions(): Promise<OASISResult<Mission[]>> {
    try {
      const response = await this.api.get('/missions');
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async getMission(id: string): Promise<OASISResult<Mission>> {
    try {
      const response = await this.api.get(`/missions/${id}`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async createMission(request: CreateMissionRequest): Promise<OASISResult<Mission>> {
    try {
      const response = await this.api.post('/missions', request);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async startMission(missionId: string, avatarId: string): Promise<OASISResult<any>> {
    try {
      const response = await this.api.post(`/missions/${missionId}/start`, { avatarId });
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async completeMission(missionId: string, avatarId: string): Promise<OASISResult<any>> {
    try {
      const response = await this.api.post(`/missions/${missionId}/complete`, { avatarId });
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  // Quest Management
  async getAllQuests(): Promise<OASISResult<Quest[]>> {
    try {
      const response = await this.api.get('/quests');
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async getQuest(id: string): Promise<OASISResult<Quest>> {
    try {
      const response = await this.api.get(`/quests/${id}`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async createQuest(request: CreateQuestRequest): Promise<OASISResult<Quest>> {
    try {
      const response = await this.api.post('/quests', request);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async startQuest(questId: string, avatarId: string): Promise<OASISResult<any>> {
    try {
      const response = await this.api.post(`/quests/${questId}/start`, { avatarId });
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async updateQuestProgress(questId: string, avatarId: string, progress: ProgressUpdate): Promise<OASISResult<any>> {
    try {
      const response = await this.api.post(`/quests/${questId}/progress`, { avatarId, ...progress });
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async completeQuest(questId: string, avatarId: string): Promise<OASISResult<any>> {
    try {
      const response = await this.api.post(`/quests/${questId}/complete`, { avatarId });
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  // Chapter Management
  async getAllChapters(): Promise<OASISResult<Chapter[]>> {
    try {
      const response = await this.api.get('/chapters');
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async getChapter(id: string): Promise<OASISResult<Chapter>> {
    try {
      const response = await this.api.get(`/chapters/${id}`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  // Holons (OASIS Data Objects)
  async getAllHolons(): Promise<OASISResult<Holon[]>> {
    try {
      const response = await this.api.get('/holons');
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async getHolon(id: string): Promise<OASISResult<Holon>> {
    try {
      const response = await this.api.get(`/holons/${id}`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async createHolon(holon: Partial<Holon>): Promise<OASISResult<Holon>> {
    try {
      const response = await this.api.post('/holons', holon);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async updateHolon(id: string, updates: Partial<Holon>): Promise<OASISResult<Holon>> {
    try {
      const response = await this.api.put(`/holons/${id}`, updates);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async deleteHolon(id: string): Promise<OASISResult<boolean>> {
    try {
      const response = await this.api.delete(`/holons/${id}`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  // Zomes (OASIS Code Modules)
  async getAllZomes(): Promise<OASISResult<Zome[]>> {
    try {
      const response = await this.api.get('/zomes');
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async getZome(id: string): Promise<OASISResult<Zome>> {
    try {
      const response = await this.api.get(`/zomes/${id}`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async createZome(zome: Partial<Zome>): Promise<OASISResult<Zome>> {
    try {
      const response = await this.api.post('/zomes', zome);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async installZome(id: string, oappId: string): Promise<OASISResult<boolean>> {
    try {
      const response = await this.api.post(`/zomes/${id}/install`, { oappId });
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  // STAR Plugins
  async getAllSTARPlugins(): Promise<OASISResult<STARPlugin[]>> {
    try {
      const response = await this.api.get('/star/plugins');
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async getSTARPlugin(id: string): Promise<OASISResult<STARPlugin>> {
    try {
      const response = await this.api.get(`/star/plugins/${id}`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async installSTARPlugin(id: string): Promise<OASISResult<boolean>> {
    try {
      const response = await this.api.post(`/star/plugins/${id}/install`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async uninstallSTARPlugin(id: string): Promise<OASISResult<boolean>> {
    try {
      const response = await this.api.delete(`/star/plugins/${id}/uninstall`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  // OAPP Templates
  async getAllTemplates(): Promise<OASISResult<OAPPTemplate[]>> {
    try {
      const response = await this.api.get('/templates');
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async getTemplate(id: string): Promise<OASISResult<OAPPTemplate>> {
    try {
      const response = await this.api.get(`/templates/${id}`);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async createOAPPFromTemplate(templateId: string, name: string, config?: any): Promise<OASISResult<OAPP>> {
    try {
      const response = await this.api.post(`/templates/${templateId}/create`, { name, config });
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  // Developer Portal Resources
  async getDevPortalResources(): Promise<OASISResult<any[]>> {
    try {
      const response = await this.api.get('/dev-portal/resources');
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async getDevPortalStats(): Promise<OASISResult<any>> {
    try {
      const response = await this.api.get('/dev-portal/stats');
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  // STARNET Operations
  async joinSTARNET(nodeConfig?: any): Promise<OASISResult<any>> {
    try {
      const response = await this.api.post('/starnet/join', nodeConfig);
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async leaveSTARNET(): Promise<OASISResult<boolean>> {
    try {
      const response = await this.api.post('/starnet/leave');
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async getSTARNETStatus(): Promise<OASISResult<any>> {
    try {
      const response = await this.api.get('/starnet/status');
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  async getSTARNETNodes(): Promise<OASISResult<any[]>> {
    try {
      const response = await this.api.get('/starnet/nodes');
      return response.data;
    } catch (error) {
      return this.handleError(error);
    }
  }

  // Error handling
  private handleError(error: any): OASISResult<any> {
    console.error('[OASIS STAR] Error:', error);

    return {
      isError: true,
      message: error.response?.data?.message || error.message || 'Unknown error occurred',
      result: null
    };
  }
}

export default OASISWeb5STARClient;
