/**
 * Global Search Service
 * Handles global search operations across all STARNET entities
 */

import { BaseService } from '../base/baseService';
import { OASISResult } from '../../types/star';

class SearchService extends BaseService {
  /**
   * Global search across all entities
   */
  async globalSearch(query: string, filters?: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.get('/Search/global', { params: { query, ...filters } }),
      { 
        query,
        totalResults: 25,
        results: {
          oapps: [
            { id: 'oapp-1', name: 'Demo OAPP 1', type: 'Game', relevance: 0.95 },
            { id: 'oapp-2', name: 'Demo OAPP 2', type: 'Tool', relevance: 0.87 }
          ],
          templates: [
            { id: 'template-1', name: 'Demo Template 1', type: 'Console', relevance: 0.92 },
            { id: 'template-2', name: 'Demo Template 2', type: 'Web', relevance: 0.85 }
          ],
          runtimes: [
            { id: 'runtime-1', name: 'Demo Runtime 1', type: 'Node.js', relevance: 0.90 },
            { id: 'runtime-2', name: 'Demo Runtime 2', type: 'Python', relevance: 0.88 }
          ],
          libraries: [
            { id: 'library-1', name: 'Demo Library 1', type: 'Utility', relevance: 0.93 },
            { id: 'library-2', name: 'Demo Library 2', type: 'Framework', relevance: 0.89 }
          ],
          nfts: [
            { id: 'nft-1', name: 'Demo NFT 1', type: 'Art', relevance: 0.91 },
            { id: 'nft-2', name: 'Demo NFT 2', type: 'Music', relevance: 0.86 }
          ],
          geonfts: [
            { id: 'geonft-1', name: 'Demo GeoNFT 1', type: 'Location', relevance: 0.94 },
            { id: 'geonft-2', name: 'Demo GeoNFT 2', type: 'Event', relevance: 0.88 }
          ],
          quests: [
            { id: 'quest-1', name: 'Demo Quest 1', type: 'Adventure', relevance: 0.92 },
            { id: 'quest-2', name: 'Demo Quest 2', type: 'Puzzle', relevance: 0.87 }
          ],
          missions: [
            { id: 'mission-1', name: 'Demo Mission 1', type: 'Combat', relevance: 0.90 },
            { id: 'mission-2', name: 'Demo Mission 2', type: 'Exploration', relevance: 0.85 }
          ],
          avatars: [
            { id: 'avatar-1', name: 'Demo Avatar 1', username: 'demo_user_1', relevance: 0.89 },
            { id: 'avatar-2', name: 'Demo Avatar 2', username: 'demo_user_2', relevance: 0.84 }
          ]
        },
        searchTime: 150,
        lastUpdated: new Date().toISOString()
      },
      'Global search completed (Demo Mode)'
    );
  }

  /**
   * Search OAPPs
   */
  async searchOAPPs(query: string, filters?: any): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.web4Api.get('/Search/oapps', { params: { query, ...filters } }),
      [
        { 
          id: 'oapp-1', 
          name: 'Demo OAPP 1', 
          description: 'A demo OASIS Application',
          type: 'Game',
          version: '1.0.0',
          rating: 4.5,
          downloads: 1000,
          relevance: 0.95
        },
        { 
          id: 'oapp-2', 
          name: 'Demo OAPP 2', 
          description: 'Another demo OASIS Application',
          type: 'Tool',
          version: '2.0.0',
          rating: 4.2,
          downloads: 500,
          relevance: 0.87
        }
      ],
      'OAPP search completed (Demo Mode)'
    );
  }

  /**
   * Search Templates
   */
  async searchTemplates(query: string, filters?: any): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.web4Api.get('/Search/templates', { params: { query, ...filters } }),
      [
        { 
          id: 'template-1', 
          name: 'Demo Template 1', 
          description: 'A demo template',
          type: 'Console',
          version: '1.0.0',
          rating: 4.3,
          downloads: 750,
          relevance: 0.92
        },
        { 
          id: 'template-2', 
          name: 'Demo Template 2', 
          description: 'Another demo template',
          type: 'Web',
          version: '1.5.0',
          rating: 4.0,
          downloads: 300,
          relevance: 0.85
        }
      ],
      'Template search completed (Demo Mode)'
    );
  }

  /**
   * Search Runtimes
   */
  async searchRuntimes(query: string, filters?: any): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.web4Api.get('/Search/runtimes', { params: { query, ...filters } }),
      [
        { 
          id: 'runtime-1', 
          name: 'Demo Runtime 1', 
          description: 'A demo runtime',
          type: 'Node.js',
          version: '18.0.0',
          rating: 4.4,
          downloads: 2000,
          relevance: 0.90
        },
        { 
          id: 'runtime-2', 
          name: 'Demo Runtime 2', 
          description: 'Another demo runtime',
          type: 'Python',
          version: '3.9.0',
          rating: 4.1,
          downloads: 1500,
          relevance: 0.88
        }
      ],
      'Runtime search completed (Demo Mode)'
    );
  }

  /**
   * Search Libraries
   */
  async searchLibraries(query: string, filters?: any): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.web4Api.get('/Search/libraries', { params: { query, ...filters } }),
      [
        { 
          id: 'library-1', 
          name: 'Demo Library 1', 
          description: 'A demo library',
          type: 'Utility',
          version: '1.0.0',
          rating: 4.6,
          downloads: 3000,
          relevance: 0.93
        },
        { 
          id: 'library-2', 
          name: 'Demo Library 2', 
          description: 'Another demo library',
          type: 'Framework',
          version: '2.0.0',
          rating: 4.2,
          downloads: 1200,
          relevance: 0.89
        }
      ],
      'Library search completed (Demo Mode)'
    );
  }

  /**
   * Search NFTs
   */
  async searchNFTs(query: string, filters?: any): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.web4Api.get('/Search/nfts', { params: { query, ...filters } }),
      [
        { 
          id: 'nft-1', 
          name: 'Demo NFT 1', 
          description: 'A demo NFT',
          type: 'Art',
          tokenId: '1',
          owner: 'demo-avatar-1',
          price: 0.1,
          relevance: 0.91
        },
        { 
          id: 'nft-2', 
          name: 'Demo NFT 2', 
          description: 'Another demo NFT',
          type: 'Music',
          tokenId: '2',
          owner: 'demo-avatar-2',
          price: 0.2,
          relevance: 0.86
        }
      ],
      'NFT search completed (Demo Mode)'
    );
  }

  /**
   * Search GeoNFTs
   */
  async searchGeoNFTs(query: string, filters?: any): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.web4Api.get('/Search/geonfts', { params: { query, ...filters } }),
      [
        { 
          id: 'geonft-1', 
          name: 'Demo GeoNFT 1', 
          description: 'A demo GeoNFT',
          type: 'Location',
          latitude: 40.7128,
          longitude: -74.0060,
          tokenId: '1',
          owner: 'demo-avatar-1',
          price: 0.15,
          relevance: 0.94
        },
        { 
          id: 'geonft-2', 
          name: 'Demo GeoNFT 2', 
          description: 'Another demo GeoNFT',
          type: 'Event',
          latitude: 51.5074,
          longitude: -0.1278,
          tokenId: '2',
          owner: 'demo-avatar-2',
          price: 0.25,
          relevance: 0.88
        }
      ],
      'GeoNFT search completed (Demo Mode)'
    );
  }

  /**
   * Search Avatars
   */
  async searchAvatars(query: string, filters?: any): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.web4Api.get('/Search/avatars', { params: { query, ...filters } }),
      [
        { 
          id: 'avatar-1', 
          name: 'Demo Avatar 1', 
          username: 'demo_user_1',
          email: 'demo1@example.com',
          karma: 1000,
          level: 5,
          relevance: 0.89
        },
        { 
          id: 'avatar-2', 
          name: 'Demo Avatar 2', 
          username: 'demo_user_2',
          email: 'demo2@example.com',
          karma: 750,
          level: 3,
          relevance: 0.84
        }
      ],
      'Avatar search completed (Demo Mode)'
    );
  }

  /**
   * Get search suggestions
   */
  async getSuggestions(query: string): Promise<OASISResult<string[]>> {
    return this.handleArrayRequest(
      () => this.web4Api.get('/Search/suggestions', { params: { query } }),
      [
        'demo oapp',
        'demo template',
        'demo runtime',
        'demo library',
        'demo nft'
      ],
      'Search suggestions retrieved (Demo Mode)'
    );
  }

  /**
   * Get trending searches
   */
  async getTrendingSearches(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.web4Api.get('/Search/trending'),
      [
        { query: 'game', count: 150, trend: 'up' },
        { query: 'tool', count: 120, trend: 'up' },
        { query: 'art', count: 100, trend: 'down' },
        { query: 'music', count: 80, trend: 'up' },
        { query: 'location', count: 60, trend: 'stable' }
      ],
      'Trending searches retrieved (Demo Mode)'
    );
  }

  /**
   * Save search query
   */
  async saveSearchQuery(query: string, filters?: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.post('/Search/save', { query, filters }),
      { 
        id: 'saved-search-1',
        query,
        filters,
        savedOn: new Date().toISOString()
      },
      'Search query saved (Demo Mode)'
    );
  }

  /**
   * Get saved searches
   */
  async getSavedSearches(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.web4Api.get('/Search/saved'),
      [
        { 
          id: 'saved-1', 
          query: 'demo oapp', 
          filters: { type: 'Game' },
          savedOn: new Date().toISOString()
        },
        { 
          id: 'saved-2', 
          query: 'demo template', 
          filters: { type: 'Web' },
          savedOn: new Date().toISOString()
        }
      ],
      'Saved searches retrieved (Demo Mode)'
    );
  }
}

export const searchService = new SearchService();
