/**
 * Map Service
 * Handles Map operations
 */

import { BaseService } from '../base/baseService';
import { OASISResult } from '../../types/star';

class MapService extends BaseService {
  /**
   * Get map data for location
   */
  async getMapData(latitude: number, longitude: number, zoom: number = 10): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.get('/Map/data', { params: { latitude, longitude, zoom } }),
      { 
        latitude, 
        longitude, 
        zoom,
        tiles: [
          { x: 0, y: 0, z: 10, url: 'https://demo-tiles.com/10/0/0.png' },
          { x: 1, y: 0, z: 10, url: 'https://demo-tiles.com/10/1/0.png' },
          { x: 0, y: 1, z: 10, url: 'https://demo-tiles.com/10/0/1.png' },
          { x: 1, y: 1, z: 10, url: 'https://demo-tiles.com/10/1/1.png' }
        ],
        markers: [
          { id: 'marker-1', latitude: latitude + 0.001, longitude: longitude + 0.001, title: 'Demo Marker 1', type: 'POI' },
          { id: 'marker-2', latitude: latitude - 0.001, longitude: longitude - 0.001, title: 'Demo Marker 2', type: 'Event' }
        ],
        lastUpdated: new Date().toISOString()
      },
      'Map data retrieved (Demo Mode)'
    );
  }

  /**
   * Get all points of interest
   */
  async getAllPois(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.web4Api.get('/Map/pois/all'),
      [
        { 
          id: 'poi-1', 
          name: 'Demo POI 1', 
          latitude: 51.5074, 
          longitude: -0.1278, 
          type: 'Restaurant',
          distance: 500,
          rating: 4.5
        },
        { 
          id: 'poi-2', 
          name: 'Demo POI 2', 
          latitude: 51.5064, 
          longitude: -0.1268, 
          type: 'Museum',
          distance: 750,
          rating: 4.2
        }
      ],
      'Nearby POIs retrieved (Demo Mode)'
    );
  }

  /**
   * Get route between two points
   */
  async getRoute(fromLat: number, fromLng: number, toLat: number, toLng: number): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.get('/Map/route', { params: { fromLat, fromLng, toLat, toLng } }),
      { 
        from: { latitude: fromLat, longitude: fromLng },
        to: { latitude: toLat, longitude: toLng },
        distance: 2.5,
        duration: 1800,
        route: [
          { latitude: fromLat, longitude: fromLng },
          { latitude: fromLat + 0.001, longitude: fromLng + 0.001 },
          { latitude: toLat, longitude: toLng }
        ],
        instructions: [
          { step: 1, instruction: 'Head north on Demo Street', distance: 500, duration: 300 },
          { step: 2, instruction: 'Turn right on Demo Avenue', distance: 1000, duration: 600 },
          { step: 3, instruction: 'Arrive at destination', distance: 500, duration: 300 }
        ]
      },
      'Route calculated (Demo Mode)'
    );
  }

  /**
   * Search for locations
   */
  async searchLocations(query: string, latitude?: number, longitude?: number): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.web4Api.get('/Map/search', { params: { query, latitude, longitude } }),
      [
        { 
          id: 'search-1', 
          name: 'Demo Location 1', 
          latitude: 40.7128, 
          longitude: -74.0060, 
          type: 'City',
          country: 'USA',
          state: 'NY'
        },
        { 
          id: 'search-2', 
          name: 'Demo Location 2', 
          latitude: 51.5074, 
          longitude: -0.1278, 
          type: 'City',
          country: 'UK',
          state: 'England'
        }
      ],
      'Location search completed (Demo Mode)'
    );
  }

  /**
   * Get geocoding data for address
   */
  async geocode(address: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.get('/Map/geocode', { params: { address } }),
      { 
        address,
        latitude: 40.7128,
        longitude: -74.0060,
        formattedAddress: '123 Demo Street, New York, NY 10001, USA',
        components: {
          street: '123 Demo Street',
          city: 'New York',
          state: 'NY',
          zipCode: '10001',
          country: 'USA'
        }
      },
      'Address geocoded (Demo Mode)'
    );
  }

  /**
   * Get reverse geocoding data for coordinates
   */
  async reverseGeocode(latitude: number, longitude: number): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.get('/Map/reverse-geocode', { params: { latitude, longitude } }),
      { 
        latitude, 
        longitude,
        address: '123 Demo Street, New York, NY 10001, USA',
        formattedAddress: '123 Demo Street, New York, NY 10001, USA',
        components: {
          street: '123 Demo Street',
          city: 'New York',
          state: 'NY',
          zipCode: '10001',
          country: 'USA'
        }
      },
      'Coordinates reverse geocoded (Demo Mode)'
    );
  }

  /**
   * Get map layers
   */
  async getLayers(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.web4Api.get('/Map/layers'),
      [
        { id: 'layer-1', name: 'Satellite', type: 'satellite', visible: true, opacity: 1.0 },
        { id: 'layer-2', name: 'Traffic', type: 'traffic', visible: false, opacity: 0.7 },
        { id: 'layer-3', name: 'Transit', type: 'transit', visible: true, opacity: 0.8 }
      ],
      'Map layers retrieved (Demo Mode)'
    );
  }

  /**
   * Toggle map layer
   */
  async toggleLayer(layerId: string, visible: boolean): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.web4Api.post(`/Map/layers/${layerId}/toggle`, { visible }),
      true,
      'Map layer toggled (Demo Mode)'
    );
  }

  /**
   * Get map settings
   */
  async getSettings(): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.get('/Map/settings'),
      { 
        defaultZoom: 10,
        defaultCenter: { latitude: 40.7128, longitude: -74.0060 },
        mapType: 'roadmap',
        units: 'metric',
        language: 'en'
      },
      'Map settings retrieved (Demo Mode)'
    );
  }

  /**
   * Update map settings
   */
  async updateSettings(settings: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.put('/Map/settings', settings),
      { ...settings, updatedOn: new Date().toISOString() },
      'Map settings updated (Demo Mode)'
    );
  }

  /**
   * Add point of interest
   */
  async addPoi(name: string, description: string, latitude: number, longitude: number, type: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.post('/Map/pois', { name, description, latitude, longitude, type }),
      { 
        id: 'poi-' + Date.now(),
        name,
        description,
        latitude,
        longitude,
        type,
        createdOn: new Date().toISOString()
      },
      'POI added successfully (Demo Mode)'
    );
  }

  /**
   * Update point of interest
   */
  async updatePoi(id: string, name: string, description: string, latitude: number, longitude: number, type: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.put(`/Map/pois/${id}`, { name, description, latitude, longitude, type }),
      { 
        id,
        name,
        description,
        latitude,
        longitude,
        type,
        updatedOn: new Date().toISOString()
      },
      'POI updated successfully (Demo Mode)'
    );
  }

  /**
   * Delete point of interest
   */
  async deletePoi(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.web4Api.delete(`/Map/pois/${id}`),
      true,
      'POI deleted successfully (Demo Mode)'
    );
  }

  /**
   * Search points of interest
   */
  async searchPois(query: string, latitude: number, longitude: number, radius: number = 10): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.web4Api.get('/Map/pois/search', { params: { query, latitude, longitude, radius } }),
      [
        { 
          id: 'search-1', 
          name: 'Search Result 1', 
          latitude: latitude + 0.001, 
          longitude: longitude + 0.001, 
          type: 'Restaurant',
          distance: 500,
          rating: 4.5,
          relevance: 0.95
        },
        { 
          id: 'search-2', 
          name: 'Search Result 2', 
          latitude: latitude - 0.001, 
          longitude: longitude - 0.001, 
          type: 'Museum',
          distance: 750,
          rating: 4.2,
          relevance: 0.87
        }
      ],
      'POI search completed (Demo Mode)'
    );
  }
}

export const mapService = new MapService();
