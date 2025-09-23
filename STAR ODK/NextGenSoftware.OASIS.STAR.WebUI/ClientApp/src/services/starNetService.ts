import axios from 'axios';
import { OASISResult, STARNETItem, OAPP, Quest, NFT, GeoNFT, Mission, Chapter, CelestialBody, CelestialSpace, Runtime, Library, Template, InventoryItem, Plugin, GeoHotSpot, SearchParams } from '../types/star';

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
    const response = await api.get('/starnet/oapps', {
      params: { providerType },
    });
    return response.data;
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
};
