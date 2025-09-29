import axios from 'axios';
import { OASISResult, STARNETItem, OAPP, Quest, NFT, GeoNFT, Mission, Chapter, CelestialBody, CelestialSpace, Runtime, Library, Template, InventoryItem, Plugin, GeoHotSpot, SearchParams } from '../types/star';

const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:50564/api';

// Helper function to check demo mode
const isDemoMode = () => {
  const saved = localStorage.getItem('demoMode');
  const result = saved ? JSON.parse(saved) : true;
  console.log('starNetService isDemoMode check:', { saved, result, type: typeof result });
  return result;
};

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
      localStorage.removeItem('authToken');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export const starNetService = {
  // OAPPs
  async createOAPP(name: string, description: string, type: any, providerType: string = 'Default'): Promise<OASISResult<OAPP>> {
    const response = await api.post('/starnet/oapps', {
      name,
      description,
      type,
      providerType,
    });
    return response.data;
  },

  async editOAPP(id: string, name: string, description: string, providerType: string = 'Default'): Promise<OASISResult<OAPP>> {
    const response = await api.put(`/starnet/oapps/${id}`, {
      name,
      description,
      providerType,
    });
    return response.data;
  },

  async deleteOAPP(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/starnet/oapps/${id}`, {
      params: { providerType },
    });
    return response.data;
  },

  async downloadAndInstallOAPP(id: string, providerType: string = 'Default'): Promise<OASISResult<OAPP>> {
    const response = await api.post(`/starnet/oapps/${id}/download-install`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async uninstallOAPP(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.post(`/starnet/oapps/${id}/uninstall`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async publishOAPP(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.post(`/starnet/oapps/${id}/publish`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async unpublishOAPP(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.post(`/starnet/oapps/${id}/unpublish`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async republishOAPP(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.post(`/starnet/oapps/${id}/republish`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async activateOAPP(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.post(`/starnet/oapps/${id}/activate`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async deactivateOAPP(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.post(`/starnet/oapps/${id}/deactivate`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async getOAPP(id: string, providerType: string = 'Default'): Promise<OASISResult<OAPP>> {
    const response = await api.get(`/starnet/oapps/${id}`, {
      params: { providerType },
    });
    return response.data;
  },

  async getAllOAPPs(providerType: string = 'Default'): Promise<OASISResult<OAPP[]>> {
    console.log('starNetService.getAllOAPPs called, isDemoMode():', isDemoMode());
    
    if (isDemoMode()) {
      // Demo mode - return demo data
      console.log('OAPPs - Demo Mode');
      return {
        isError: false,
        message: 'OAPPs loaded successfully (Demo Mode)',
        result: [
          {
            id: '1',
            name: 'Cosmic Explorer',
            description: 'Navigate through the infinite cosmos with real-time star mapping and discovery tools',
            type: 'Web',
            version: '2.1.0',
            isPublished: true,
            isInstalled: false,
            isActive: true,
            downloads: 15420,
            rating: 4.8,
            author: 'SpaceDev Studios',
            category: 'Exploration',
            lastUpdated: new Date('2024-01-15'),
          },
          {
            id: '2',
            name: 'Quantum Analyzer',
            description: 'Advanced quantum computing interface for complex data analysis and visualization',
            type: 'Desktop',
            version: '1.5.2',
            isPublished: true,
            isInstalled: true,
            isActive: true,
            downloads: 8930,
            rating: 4.6,
            author: 'Quantum Labs',
            category: 'Analytics',
            lastUpdated: new Date('2024-01-10'),
          },
          {
            id: '3',
            name: 'Neural Network Trainer',
            description: 'Train and deploy AI models with intuitive drag-and-drop interface',
            type: 'Web',
            version: '3.0.1',
            isPublished: true,
            isInstalled: false,
            isActive: true,
            downloads: 22150,
            rating: 4.9,
            author: 'AI Innovations',
            category: 'Machine Learning',
            lastUpdated: new Date('2024-01-20'),
          }
        ]
      };
    }

    console.log('OAPPs - Live Mode, making API call to:', API_BASE_URL + '/starnet/oapps');
    try {
      const response = await api.get('/starnet/oapps', {
        params: { providerType },
      });
      console.log('OAPPs API response:', response.data);
      return response.data;
    } catch (error) {
      console.error('Error fetching OAPPs:', error);
      return {
        isError: true,
        message: error instanceof Error ? error.message : 'Failed to fetch OAPPs',
        result: []
      };
    }
  },

  async getOAPPsCreatedByMe(providerType: string = 'Default'): Promise<OASISResult<OAPP[]>> {
    const response = await api.get('/starnet/oapps/created-by-me', {
      params: { providerType },
    });
    return response.data;
  },

  async getInstalledOAPPs(providerType: string = 'Default'): Promise<OASISResult<any[]>> {
    const response = await api.get('/starnet/oapps/installed', {
      params: { providerType },
    });
    return response.data;
  },

  async getUninstalledOAPPs(providerType: string = 'Default'): Promise<OASISResult<OAPP[]>> {
    const response = await api.get('/starnet/oapps/uninstalled', {
      params: { providerType },
    });
    return response.data;
  },

  async getUnpublishedOAPPs(providerType: string = 'Default'): Promise<OASISResult<OAPP[]>> {
    const response = await api.get('/starnet/oapps/unpublished', {
      params: { providerType },
    });
    return response.data;
  },

  async getDeactivatedOAPPs(providerType: string = 'Default'): Promise<OASISResult<OAPP[]>> {
    const response = await api.get('/starnet/oapps/deactivated', {
      params: { providerType },
    });
    return response.data;
  },

  async searchOAPPs(searchTerm: string, providerType: string = 'Default'): Promise<OASISResult<OAPP[]>> {
    const response = await api.get('/starnet/oapps/search', {
      params: { searchTerm, providerType },
    });
    return response.data;
  },

  // Quests
  async createQuest(name: string, description: string, type: any, providerType: string = 'Default'): Promise<OASISResult<Quest>> {
    const response = await api.post('/starnet/quests', {
      name,
      description,
      type,
      providerType,
    });
    return response.data;
  },

  async editQuest(id: string, name: string, description: string, providerType: string = 'Default'): Promise<OASISResult<Quest>> {
    const response = await api.put(`/starnet/quests/${id}`, {
      name,
      description,
      providerType,
    });
    return response.data;
  },

  async deleteQuest(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/starnet/quests/${id}`, {
      params: { providerType },
    });
    return response.data;
  },

  async downloadAndInstallQuest(id: string, providerType: string = 'Default'): Promise<OASISResult<Quest>> {
    const response = await api.post(`/starnet/quests/${id}/download-install`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async uninstallQuest(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.post(`/starnet/quests/${id}/uninstall`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async publishQuest(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.post(`/starnet/quests/${id}/publish`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async unpublishQuest(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.post(`/starnet/quests/${id}/unpublish`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async republishQuest(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.post(`/starnet/quests/${id}/republish`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async activateQuest(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.post(`/starnet/quests/${id}/activate`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async deactivateQuest(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.post(`/starnet/quests/${id}/deactivate`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async getQuest(id: string, providerType: string = 'Default'): Promise<OASISResult<Quest>> {
    const response = await api.get(`/starnet/quests/${id}`, {
      params: { providerType },
    });
    return response.data;
  },

  async getAllQuests(providerType: string = 'Default'): Promise<OASISResult<Quest[]>> {
    const response = await api.get('/starnet/quests', {
      params: { providerType },
    });
    return response.data;
  },

  async getQuestsCreatedByMe(providerType: string = 'Default'): Promise<OASISResult<Quest[]>> {
    const response = await api.get('/starnet/quests/created-by-me', {
      params: { providerType },
    });
    return response.data;
  },

  async getInstalledQuests(providerType: string = 'Default'): Promise<OASISResult<any[]>> {
    const response = await api.get('/starnet/quests/installed', {
      params: { providerType },
    });
    return response.data;
  },

  async getUninstalledQuests(providerType: string = 'Default'): Promise<OASISResult<Quest[]>> {
    const response = await api.get('/starnet/quests/uninstalled', {
      params: { providerType },
    });
    return response.data;
  },

  async getUnpublishedQuests(providerType: string = 'Default'): Promise<OASISResult<Quest[]>> {
    const response = await api.get('/starnet/quests/unpublished', {
      params: { providerType },
    });
    return response.data;
  },

  async getDeactivatedQuests(providerType: string = 'Default'): Promise<OASISResult<Quest[]>> {
    const response = await api.get('/starnet/quests/deactivated', {
      params: { providerType },
    });
    return response.data;
  },

  async searchQuests(searchTerm: string, providerType: string = 'Default'): Promise<OASISResult<Quest[]>> {
    const response = await api.get('/starnet/quests/search', {
      params: { searchTerm, providerType },
    });
    return response.data;
  },

  // NFTs
  async createNFT(name: string, description: string, type: any, providerType: string = 'Default'): Promise<OASISResult<NFT>> {
    const response = await api.post('/starnet/nfts', {
      name,
      description,
      type,
      providerType,
    });
    return response.data;
  },

  async editNFT(id: string, name: string, description: string, providerType: string = 'Default'): Promise<OASISResult<NFT>> {
    const response = await api.put(`/starnet/nfts/${id}`, {
      name,
      description,
      providerType,
    });
    return response.data;
  },

  async deleteNFT(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/starnet/nfts/${id}`, {
      params: { providerType },
    });
    return response.data;
  },

  async downloadAndInstallNFT(id: string, providerType: string = 'Default'): Promise<OASISResult<NFT>> {
    const response = await api.post(`/starnet/nfts/${id}/download-install`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async uninstallNFT(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.post(`/starnet/nfts/${id}/uninstall`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async publishNFT(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.post(`/starnet/nfts/${id}/publish`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async unpublishNFT(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.post(`/starnet/nfts/${id}/unpublish`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async republishNFT(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.post(`/starnet/nfts/${id}/republish`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async activateNFT(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.post(`/starnet/nfts/${id}/activate`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async deactivateNFT(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.post(`/starnet/nfts/${id}/deactivate`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async getNFT(id: string, providerType: string = 'Default'): Promise<OASISResult<NFT>> {
    const response = await api.get(`/starnet/nfts/${id}`, {
      params: { providerType },
    });
    return response.data;
  },

  async getAllNFTs(providerType: string = 'Default'): Promise<OASISResult<NFT[]>> {
    const response = await api.get('/starnet/nfts', {
      params: { providerType },
    });
    return response.data;
  },

  async getNFTsCreatedByMe(providerType: string = 'Default'): Promise<OASISResult<NFT[]>> {
    const response = await api.get('/starnet/nfts/created-by-me', {
      params: { providerType },
    });
    return response.data;
  },

  async getInstalledNFTs(providerType: string = 'Default'): Promise<OASISResult<any[]>> {
    const response = await api.get('/starnet/nfts/installed', {
      params: { providerType },
    });
    return response.data;
  },

  async getUninstalledNFTs(providerType: string = 'Default'): Promise<OASISResult<NFT[]>> {
    const response = await api.get('/starnet/nfts/uninstalled', {
      params: { providerType },
    });
    return response.data;
  },

  async getUnpublishedNFTs(providerType: string = 'Default'): Promise<OASISResult<NFT[]>> {
    const response = await api.get('/starnet/nfts/unpublished', {
      params: { providerType },
    });
    return response.data;
  },

  async getDeactivatedNFTs(providerType: string = 'Default'): Promise<OASISResult<NFT[]>> {
    const response = await api.get('/starnet/nfts/deactivated', {
      params: { providerType },
    });
    return response.data;
  },

  async searchNFTs(searchTerm: string, providerType: string = 'Default'): Promise<OASISResult<NFT[]>> {
    const response = await api.get('/starnet/nfts/search', {
      params: { searchTerm, providerType },
    });
    return response.data;
  },

  async mintNFT(name: string, description: string, type: any, providerType: string = 'Default'): Promise<OASISResult<NFT>> {
    const response = await api.post('/starnet/nfts/mint', {
      name,
      description,
      type,
      providerType,
    });
    return response.data;
  },

  async convertNFT(web4NftId: string, providerType: string = 'Default'): Promise<OASISResult<NFT>> {
    const response = await api.post(`/starnet/nfts/convert/${web4NftId}`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async cloneNFT(id: string, providerType: string = 'Default'): Promise<OASISResult<NFT>> {
    const response = await api.post(`/starnet/nfts/${id}/clone`, null, {
      params: { providerType },
    });
    return response.data;
  },

  // GeoNFTs
  async createGeoNFT(name: string, description: string, type: any, providerType: string = 'Default'): Promise<OASISResult<GeoNFT>> {
    const response = await api.post('/starnet/geonfts', {
      name,
      description,
      type,
      providerType,
    });
    return response.data;
  },

  async editGeoNFT(id: string, name: string, description: string, providerType: string = 'Default'): Promise<OASISResult<GeoNFT>> {
    const response = await api.put(`/starnet/geonfts/${id}`, {
      name,
      description,
      providerType,
    });
    return response.data;
  },

  async deleteGeoNFT(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.delete(`/starnet/geonfts/${id}`, {
      params: { providerType },
    });
    return response.data;
  },

  async downloadAndInstallGeoNFT(id: string, providerType: string = 'Default'): Promise<OASISResult<GeoNFT>> {
    const response = await api.post(`/starnet/geonfts/${id}/download-install`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async uninstallGeoNFT(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.post(`/starnet/geonfts/${id}/uninstall`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async publishGeoNFT(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.post(`/starnet/geonfts/${id}/publish`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async unpublishGeoNFT(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.post(`/starnet/geonfts/${id}/unpublish`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async republishGeoNFT(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.post(`/starnet/geonfts/${id}/republish`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async activateGeoNFT(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.post(`/starnet/geonfts/${id}/activate`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async deactivateGeoNFT(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.post(`/starnet/geonfts/${id}/deactivate`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async getGeoNFT(id: string, providerType: string = 'Default'): Promise<OASISResult<GeoNFT>> {
    const response = await api.get(`/starnet/geonfts/${id}`, {
      params: { providerType },
    });
    return response.data;
  },

  async getAllGeoNFTs(providerType: string = 'Default'): Promise<OASISResult<GeoNFT[]>> {
    const response = await api.get('/starnet/geonfts', {
      params: { providerType },
    });
    return response.data;
  },

  async getGeoNFTsCreatedByMe(providerType: string = 'Default'): Promise<OASISResult<GeoNFT[]>> {
    const response = await api.get('/starnet/geonfts/created-by-me', {
      params: { providerType },
    });
    return response.data;
  },

  async getInstalledGeoNFTs(providerType: string = 'Default'): Promise<OASISResult<any[]>> {
    const response = await api.get('/starnet/geonfts/installed', {
      params: { providerType },
    });
    return response.data;
  },

  async getUninstalledGeoNFTs(providerType: string = 'Default'): Promise<OASISResult<GeoNFT[]>> {
    const response = await api.get('/starnet/geonfts/uninstalled', {
      params: { providerType },
    });
    return response.data;
  },

  async getUnpublishedGeoNFTs(providerType: string = 'Default'): Promise<OASISResult<GeoNFT[]>> {
    const response = await api.get('/starnet/geonfts/unpublished', {
      params: { providerType },
    });
    return response.data;
  },

  async getDeactivatedGeoNFTs(providerType: string = 'Default'): Promise<OASISResult<GeoNFT[]>> {
    const response = await api.get('/starnet/geonfts/deactivated', {
      params: { providerType },
    });
    return response.data;
  },

  async searchGeoNFTs(searchTerm: string, providerType: string = 'Default'): Promise<OASISResult<GeoNFT[]>> {
    const response = await api.get('/starnet/geonfts/search', {
      params: { searchTerm, providerType },
    });
    return response.data;
  },

  async mintGeoNFT(name: string, description: string, type: any, providerType: string = 'Default'): Promise<OASISResult<GeoNFT>> {
    const response = await api.post('/starnet/geonfts/mint', {
      name,
      description,
      type,
      providerType,
    });
    return response.data;
  },

  async burnGeoNFT(id: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.post(`/starnet/geonfts/${id}/burn`, null, {
      params: { providerType },
    });
    return response.data;
  },

  async importGeoNFT(filePath: string, providerType: string = 'Default'): Promise<OASISResult<GeoNFT>> {
    const response = await api.post('/starnet/geonfts/import', {
      filePath,
      providerType,
    });
    return response.data;
  },

  async exportGeoNFT(id: string, filePath: string, providerType: string = 'Default'): Promise<OASISResult<boolean>> {
    const response = await api.post(`/starnet/geonfts/${id}/export`, {
      filePath,
      providerType,
    });
    return response.data;
  },

  // Missions
  async createMission(name: string, description: string, type: any, providerType: string = 'Default'): Promise<OASISResult<Mission>> {
    const response = await api.post('/starnet/missions', {
      name,
      description,
      type,
      providerType,
    });
    return response.data;
  },

  async getAllMissions(providerType: string = 'Default'): Promise<OASISResult<Mission[]>> {
    const response = await api.get('/starnet/missions', {
      params: { providerType },
    });
    return response.data;
  },

  async searchMissions(searchTerm: string, providerType: string = 'Default'): Promise<OASISResult<Mission[]>> {
    const response = await api.get('/starnet/missions/search', {
      params: { searchTerm, providerType },
    });
    return response.data;
  },

  // Chapters
  async createChapter(name: string, description: string, type: any, providerType: string = 'Default'): Promise<OASISResult<Chapter>> {
    const response = await api.post('/starnet/chapters', {
      name,
      description,
      type,
      providerType,
    });
    return response.data;
  },

  async getAllChapters(providerType: string = 'Default'): Promise<OASISResult<Chapter[]>> {
    const response = await api.get('/starnet/chapters', {
      params: { providerType },
    });
    return response.data;
  },

  async searchChapters(searchTerm: string, providerType: string = 'Default'): Promise<OASISResult<Chapter[]>> {
    const response = await api.get('/starnet/chapters/search', {
      params: { searchTerm, providerType },
    });
    return response.data;
  },

  // Additional methods for detail pages
  async getMissionById(id: string): Promise<OASISResult<Mission>> {
    if (isDemoMode()) {
      return {
        result: {
          id,
          name: 'Demo Mission',
          description: 'A demo mission for testing',
          missionType: 'Exploration',
          difficulty: 'Medium',
          estimatedDuration: 60,
          rewards: ['Karma', 'Experience'],
          requirements: ['Level 5'],
          title: 'Demo Mission',
          priority: 'Medium',
          dueDate: new Date(),
          status: 'In Progress',
          progress: 50,
          completedTasks: 2,
          totalTasks: 4,
          karmaReward: 100,
          xpReward: 50,
          itemRewards: ['Sword', 'Shield'],
          assignee: 'Demo User',
          steps: ['Step 1', 'Step 2', 'Step 3', 'Step 4'],
          completedSteps: 2,
        } as Mission,
        isError: false,
      };
    }
    const response = await api.get(`/starnet/missions/${id}`);
    return response.data;
  },

  async updateMission(id: string, data: Partial<Mission>): Promise<OASISResult<Mission>> {
    if (isDemoMode()) {
      return {
        result: { ...data, id } as Mission,
        isError: false,
      };
    }
    const response = await api.put(`/starnet/missions/${id}`, data);
    return response.data;
  },

  async deleteMission(id: string): Promise<OASISResult<boolean>> {
    if (isDemoMode()) {
      return { result: true, isError: false };
    }
    const response = await api.delete(`/starnet/missions/${id}`);
    return response.data;
  },

  async getQuestById(id: string): Promise<OASISResult<Quest>> {
    if (isDemoMode()) {
      return {
        result: {
          id,
          name: 'Demo Quest',
          description: 'A demo quest for testing',
          questType: 'Main',
          difficulty: 'Medium',
          estimatedDuration: 30,
          rewards: ['Karma', 'Experience'],
          requirements: ['Level 3'],
          title: 'Demo Quest',
          dueDate: new Date(),
          status: 'In Progress',
          progress: 75,
          completedTasks: 3,
          totalTasks: 4,
          karmaReward: 75,
          xpReward: 25,
          itemRewards: ['Potion'],
          creator: 'Demo Creator',
          steps: ['Step 1', 'Step 2', 'Step 3', 'Step 4'],
          completedSteps: 3,
        } as Quest,
        isError: false,
      };
    }
    const response = await api.get(`/starnet/quests/${id}`);
    return response.data;
  },

  async updateQuest(id: string, data: Partial<Quest>): Promise<OASISResult<Quest>> {
    if (isDemoMode()) {
      return {
        result: { ...data, id } as Quest,
        isError: false,
      };
    }
    const response = await api.put(`/starnet/quests/${id}`, data);
    return response.data;
  },

  async getChapterById(id: string): Promise<OASISResult<Chapter>> {
    if (isDemoMode()) {
      return {
        result: {
          id,
          name: 'Demo Chapter',
          description: 'A demo chapter for testing',
          chapterType: 'Story',
          order: 1,
          content: 'This is demo chapter content...',
          estimatedReadTime: 15,
          title: 'Demo Chapter',
          chapterNumber: 1,
          difficulty: 'Easy',
          status: 'Published',
          readingProgress: 60,
          wordsRead: 300,
          totalWords: 500,
          publishedDate: new Date(),
          estimatedReadingTime: 15,
          level: 'Beginner',
          sections: ['Introduction', 'Main Content', 'Conclusion'],
        } as Chapter,
        isError: false,
      };
    }
    const response = await api.get(`/starnet/chapters/${id}`);
    return response.data;
  },

  async updateChapter(id: string, data: Partial<Chapter>): Promise<OASISResult<Chapter>> {
    if (isDemoMode()) {
      return {
        result: { ...data, id } as Chapter,
        isError: false,
      };
    }
    const response = await api.put(`/starnet/chapters/${id}`, data);
    return response.data;
  },

  async deleteChapter(id: string): Promise<OASISResult<boolean>> {
    if (isDemoMode()) {
      return { result: true, isError: false };
    }
    const response = await api.delete(`/starnet/chapters/${id}`);
    return response.data;
  },

  async getNFTById(id: string): Promise<OASISResult<NFT>> {
    if (isDemoMode()) {
      return {
        result: {
          id,
          name: 'Demo NFT',
          description: 'A demo NFT for testing',
          nftType: 'Art',
          tokenId: '123',
          contractAddress: '0x123...',
          metadata: {},
          imageUrl: 'https://via.placeholder.com/300',
          animationUrl: '',
          externalUrl: '',
          attributes: [],
          creator: 'Demo Creator',
          rarity: 'Rare',
          price: 100,
          lastSalePrice: 95,
          views: 150,
          likes: 25,
          collection: 'Demo Collection',
        } as NFT,
        isError: false,
      };
    }
    const response = await api.get(`/starnet/nfts/${id}`);
    return response.data;
  },

  async updateNFT(id: string, data: Partial<NFT>): Promise<OASISResult<NFT>> {
    if (isDemoMode()) {
      return {
        result: { ...data, id } as NFT,
        isError: false,
      };
    }
    const response = await api.put(`/starnet/nfts/${id}`, data);
    return response.data;
  },

  async getGeoNFTById(id: string): Promise<OASISResult<GeoNFT>> {
    if (isDemoMode()) {
      return {
        result: {
          id,
          name: 'Demo GeoNFT',
          description: 'A demo GeoNFT for testing',
          nftType: 'Location',
          tokenId: '456',
          contractAddress: '0x456...',
          metadata: {},
          imageUrl: 'https://via.placeholder.com/300',
          animationUrl: '',
          externalUrl: '',
          attributes: [],
          creator: 'Demo Creator',
          rarity: 'Epic',
          price: 200,
          lastSalePrice: 195,
          views: 300,
          likes: 50,
          collection: 'Geo Collection',
          latitude: 40.7128,
          longitude: -74.0060,
          altitude: 10,
          radius: 100,
          isActive: true,
          collectedBy: '',
          collectedDate: undefined,
          country: 'USA',
          region: 'New York',
          address: 'New York, NY',
          status: 'Available',
        } as GeoNFT,
        isError: false,
      };
    }
    const response = await api.get(`/starnet/geonfts/${id}`);
    return response.data;
  },

  async updateGeoNFT(id: string, data: Partial<GeoNFT>): Promise<OASISResult<GeoNFT>> {
    if (isDemoMode()) {
      return {
        result: { ...data, id } as GeoNFT,
        isError: false,
      };
    }
    const response = await api.put(`/starnet/geonfts/${id}`, data);
    return response.data;
  },

  async getAllInventoryItems(): Promise<OASISResult<InventoryItem[]>> {
    if (isDemoMode()) {
      return {
        result: [
          {
            id: '1',
            name: 'Demo Sword',
            description: 'A powerful demo sword',
            itemType: 'Weapon',
            quantity: 1,
            rarity: 'Rare',
            value: 100,
            weight: 2.5,
            stackable: false,
            tradeable: true,
            consumable: false,
            effects: ['Damage +10'],
            type: 'Weapon',
            status: 'Equipped',
            imageUrl: 'https://via.placeholder.com/300',
            durability: 85,
            maxDurability: 100,
            acquiredDate: new Date(),
            lastUsed: new Date(),
            location: 'Inventory',
          } as InventoryItem,
        ],
        isError: false,
      };
    }
    const response = await api.get('/starnet/inventory');
    return response.data;
  },

  async updateInventoryItem(id: string, data: Partial<InventoryItem>): Promise<OASISResult<InventoryItem>> {
    if (isDemoMode()) {
      return {
        result: { ...data, id } as InventoryItem,
        isError: false,
      };
    }
    const response = await api.put(`/starnet/inventory/${id}`, data);
    return response.data;
  },

  async deleteInventoryItem(id: string): Promise<OASISResult<boolean>> {
    if (isDemoMode()) {
      return { result: true, isError: false };
    }
    const response = await api.delete(`/starnet/inventory/${id}`);
    return response.data;
  },

  async getAllLibraries(): Promise<OASISResult<Library[]>> {
    if (isDemoMode()) {
      return {
        result: [
          {
            id: '1',
            name: 'Demo Library',
            description: 'A demo library for testing',
            libraryType: 'Framework',
            version: '1.0.0',
            language: 'JavaScript',
            framework: 'React',
            dependencies: ['react', 'react-dom'],
            documentation: 'https://demo.com/docs',
            repository: 'https://github.com/demo/library',
            type: 'Framework',
            status: 'Active',
            lastUpdated: new Date(),
            downloads: 1000,
            bookmarks: 50,
            license: 'MIT',
            size: 1024,
            fileCount: 25,
            hasDocumentation: true,
            exampleCount: 5,
            technologies: ['React', 'TypeScript'],
          } as Library,
        ],
        isError: false,
      };
    }
    const response = await api.get('/starnet/libraries');
    return response.data;
  },

  async updateLibrary(id: string, data: Partial<Library>): Promise<OASISResult<Library>> {
    if (isDemoMode()) {
      return {
        result: { ...data, id } as Library,
        isError: false,
      };
    }
    const response = await api.put(`/starnet/libraries/${id}`, data);
    return response.data;
  },

  async deleteLibrary(id: string): Promise<OASISResult<boolean>> {
    if (isDemoMode()) {
      return { result: true, isError: false };
    }
    const response = await api.delete(`/starnet/libraries/${id}`);
    return response.data;
  },

  async getAllRuntimes(): Promise<OASISResult<Runtime[]>> {
    if (isDemoMode()) {
      return {
        result: [
          {
            id: '1',
            name: 'Demo Runtime',
            description: 'A demo runtime for testing',
            runtimeType: 'Node.js',
            version: '18.0.0',
            platform: 'Linux',
            architecture: 'x64',
            dependencies: ['npm'],
            requirements: ['Node.js 18+'],
            framework: 'Express',
            category: 'Web',
            lastUpdated: new Date('2024-01-15'),
            uptime: '99.9%',
            language: 'JavaScript',
            status: 'Running',
            cpuUsage: 25,
            memoryUsage: 512,
            diskUsage: 1024,
            instances: 1,
            maxInstances: 5,
            port: 3000,
            environment: 'Production',
            type: 'Runtime',
            owner: 'Demo User',
            securityLevel: 'Standard',
            maxMemory: 1024,
            maxCpu: 50,
          } as Runtime,
        ],
        isError: false,
      };
    }
    const response = await api.get('/starnet/runtimes');
    return response.data;
  },

  async updateRuntime(id: string, data: Partial<Runtime>): Promise<OASISResult<Runtime>> {
    if (isDemoMode()) {
      return {
        result: { ...data, id } as Runtime,
        isError: false,
      };
    }
    const response = await api.put(`/starnet/runtimes/${id}`, data);
    return response.data;
  },

  async deleteRuntime(id: string): Promise<OASISResult<boolean>> {
    if (isDemoMode()) {
      return { result: true, isError: false };
    }
    const response = await api.delete(`/starnet/runtimes/${id}`);
    return response.data;
  },

  async getAllTemplates(): Promise<OASISResult<Template[]>> {
    if (isDemoMode()) {
      return {
        result: [
          {
            id: '1',
            name: 'Demo Template',
            description: 'A demo template for testing',
            templateType: 'Web App',
            category: 'Frontend',
            language: 'TypeScript',
            framework: 'React',
            complexity: 'Medium',
            estimatedSetupTime: 30,
            features: ['Responsive', 'PWA'],
            requirements: ['Node.js', 'npm'],
            type: 'Web App',
            status: 'Published',
            lastUpdated: new Date(),
            downloads: 500,
            bookmarks: 25,
            license: 'MIT',
            size: 2048,
            fileCount: 50,
            hasDocumentation: true,
            exampleCount: 3,
            technologies: ['React', 'TypeScript', 'Material-UI'],
          } as Template,
        ],
        isError: false,
      };
    }
    const response = await api.get('/starnet/templates');
    return response.data;
  },

  async updateTemplate(id: string, data: Partial<Template>): Promise<OASISResult<Template>> {
    if (isDemoMode()) {
      return {
        result: { ...data, id } as Template,
        isError: false,
      };
    }
    const response = await api.put(`/starnet/templates/${id}`, data);
    return response.data;
  },

  async deleteTemplate(id: string): Promise<OASISResult<boolean>> {
    if (isDemoMode()) {
      return { result: true, isError: false };
    }
    const response = await api.delete(`/starnet/templates/${id}`);
    return response.data;
  },

  async installOAPP(id: string): Promise<OASISResult<boolean>> {
    if (isDemoMode()) {
      return { result: true, isError: false };
    }
    const response = await api.post(`/starnet/oapps/${id}/install`);
    return response.data;
  },

  async getAllCelestialBodies(): Promise<OASISResult<CelestialBody[]>> {
    if (isDemoMode()) {
      return {
        result: [
          {
            id: '1',
            name: 'Demo Planet',
            description: 'A demo celestial body for testing',
            celestialBodyType: 'Planet',
            mass: 5.97e24,
            radius: 6371,
            temperature: 288,
            atmosphere: 'Nitrogen, Oxygen',
            gravity: 9.81,
            orbitRadius: 1.496e8,
            orbitSpeed: 29.78,
            parentId: 'sun',
            children: [],
            galaxy: 'Milky Way',
            discoveredDate: new Date(),
            orbitalPeriod: 365.25,
            distanceFromStar: 1.496e8,
            discoverer: 'Demo Astronomer',
            discoveryMethod: 'Telescope',
            luminosity: 1.0,
            age: 4.5e9,
          } as CelestialBody,
        ],
        isError: false,
      };
    }
    const response = await api.get('/starnet/celestial-bodies');
    return response.data;
  },

  async updateCelestialBody(id: string, data: Partial<CelestialBody>): Promise<OASISResult<CelestialBody>> {
    if (isDemoMode()) {
      return {
        result: { ...data, id } as CelestialBody,
        isError: false,
      };
    }
    const response = await api.put(`/starnet/celestial-bodies/${id}`, data);
    return response.data;
  },

  async deleteCelestialBody(id: string): Promise<OASISResult<boolean>> {
    if (isDemoMode()) {
      return { result: true, isError: false };
    }
    const response = await api.delete(`/starnet/celestial-bodies/${id}`);
    return response.data;
  },

  async getAllCelestialSpaces(): Promise<OASISResult<CelestialSpace[]>> {
    if (isDemoMode()) {
      return {
        result: [
          {
            id: '1',
            name: 'Demo Galaxy',
            description: 'A demo celestial space for testing',
            celestialSpaceType: 'Galaxy',
            dimensions: { width: 100000, height: 100000, depth: 1000 },
            gravity: 0.0001,
            atmosphere: 'Vacuum',
            temperature: 2.7,
            celestialBodies: [],
            galaxy: 'Milky Way',
            discoveredDate: new Date(),
            diameter: 100000,
            volume: 1e15,
            matterDensity: 1e-24,
            darkMatterPercentage: 85,
            energyLevel: 1e-6,
            discoverer: 'Demo Astronomer',
            discoveryMethod: 'Radio Telescope',
            starCount: 100000000000,
            age: 13.8e9,
            expansionRate: 70,
            gravitationalField: 'Weak',
          } as CelestialSpace,
        ],
        isError: false,
      };
    }
    const response = await api.get('/starnet/celestial-spaces');
    return response.data;
  },

  async updateCelestialSpace(id: string, data: Partial<CelestialSpace>): Promise<OASISResult<CelestialSpace>> {
    if (isDemoMode()) {
      return {
        result: { ...data, id } as CelestialSpace,
        isError: false,
      };
    }
    const response = await api.put(`/starnet/celestial-spaces/${id}`, data);
    return response.data;
  },

  async deleteCelestialSpace(id: string): Promise<OASISResult<boolean>> {
    if (isDemoMode()) {
      return { result: true, isError: false };
    }
    const response = await api.delete(`/starnet/celestial-spaces/${id}`);
    return response.data;
  },
};
