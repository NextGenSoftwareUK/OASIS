/**
 * STAR API Client Utility
 * 
 * Centralized client for all STAR API interactions.
 * Handles authentication, error handling, and response formatting.
 */

const STARAPIClient = {
    /**
     * Get STAR API base URL
     */
    getBaseURL() {
        // Check for centralized config function first
        if (typeof getSTARAPIBaseURL === 'function') {
            return getSTARAPIBaseURL();
        }
        // Fallback to window location origin (for development)
        return window.location.origin;
    },

    /**
     * Get authentication token
     */
    getAuthToken() {
        // Check for centralized auth function first
        if (typeof getAuthToken === 'function') {
            return getAuthToken();
        }
        // Fallback to localStorage
        try {
            const authData = localStorage.getItem('oasis_auth');
            if (authData) {
                const auth = JSON.parse(authData);
                return auth.token || null;
            }
        } catch (e) {
            console.error('Error parsing auth data:', e);
        }
        return null;
    },

    /**
     * Get avatar ID
     */
    getAvatarId() {
        // Check for centralized auth function first
        if (typeof getAvatarId === 'function') {
            return getAvatarId();
        }
        // Fallback to localStorage
        try {
            const authData = localStorage.getItem('oasis_auth');
            if (authData) {
                const auth = JSON.parse(authData);
                return auth.avatarId || auth.avatar?.id || auth.avatar?.avatarId || null;
            }
        } catch (e) {
            console.error('Error parsing auth data:', e);
        }
        return null;
    },

    /**
     * Get headers for API requests
     */
    getHeaders(includeAuth = true) {
        const headers = {
            'Content-Type': 'application/json'
        };

        if (includeAuth) {
            const token = this.getAuthToken();
            if (token) {
                headers['Authorization'] = `Bearer ${token}`;
            }
        }

        return headers;
    },

    /**
     * Make API request
     * @param {string} endpoint - API endpoint (e.g., '/api/Quests')
     * @param {object} options - Fetch options
     * @returns {Promise<object>} - Parsed response
     */
    async request(endpoint, options = {}) {
        const baseURL = this.getBaseURL();
        const url = endpoint.startsWith('http') ? endpoint : `${baseURL}${endpoint}`;

        const defaultOptions = {
            headers: this.getHeaders(),
            ...options
        };

        try {
            const response = await fetch(url, defaultOptions);

            // Handle non-JSON responses
            const contentType = response.headers.get('content-type');
            if (!contentType || !contentType.includes('application/json')) {
                if (!response.ok) {
                    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
                }
                return {
                    ok: true,
                    status: response.status,
                    data: await response.text()
                };
            }

            const data = await response.json();

            if (!response.ok) {
                // Handle error response
                const errorMessage = data.message || data.Message || data.exception || `HTTP ${response.status}: ${response.statusText}`;
                const error = new Error(errorMessage);
                error.status = response.status; // Attach status code for error handling
                throw error;
            }

            // Return standardized response
            return {
                ok: true,
                status: response.status,
                data: data.result !== undefined ? data.result : data,
                fullResponse: data
            };
        } catch (error) {
            // Don't log expected errors (400, 401, connection errors) - they're handled gracefully
            const isExpectedError = 
                error.message?.includes('Failed to fetch') ||
                error.message?.includes('ERR_CONNECTION_REFUSED') ||
                error.status === 400 ||
                error.status === 401 ||
                error.message?.includes('400') ||
                error.message?.includes('401');
            
            if (!isExpectedError) {
                console.error('STAR API request failed:', error);
            }
            throw error;
        }
    },

    /**
     * GET request
     */
    async get(endpoint, params = {}) {
        let url = endpoint;
        
        // Add query parameters
        if (Object.keys(params).length > 0) {
            const queryString = new URLSearchParams(params).toString();
            url += (url.includes('?') ? '&' : '?') + queryString;
        }

        return this.request(url, {
            method: 'GET'
        });
    },

    /**
     * POST request
     */
    async post(endpoint, body = {}) {
        return this.request(endpoint, {
            method: 'POST',
            body: JSON.stringify(body)
        });
    },

    /**
     * PUT request
     */
    async put(endpoint, body = {}) {
        return this.request(endpoint, {
            method: 'PUT',
            body: JSON.stringify(body)
        });
    },

    /**
     * DELETE request
     */
    async delete(endpoint) {
        return this.request(endpoint, {
            method: 'DELETE'
        });
    },

    // ============================================
    // Avatar Endpoints
    // ============================================

    /**
     * Get current authenticated avatar
     */
    async getCurrentAvatar() {
        return this.get('/api/Avatar/current');
    },

    /**
     * Authenticate with STAR API
     */
    async authenticate(username, password) {
        return this.post('/api/Avatar/authenticate', { username, password });
    },

    // ============================================
    // Quest Endpoints
    // ============================================

    /**
     * Get all quests
     */
    async getQuests(params = {}) {
        return this.get('/api/Quests', params);
    },

    /**
     * Get quest by ID
     */
    async getQuest(id) {
        return this.get(`/api/Quests/${id}`);
    },

    /**
     * Create quest
     */
    async createQuest(questData) {
        return this.post('/api/Quests', questData);
    },

    /**
     * Update quest
     */
    async updateQuest(id, questData) {
        return this.put(`/api/Quests/${id}`, questData);
    },

    /**
     * Delete quest
     */
    async deleteQuest(id) {
        return this.delete(`/api/Quests/${id}`);
    },

    /**
     * Load all quests for avatar
     */
    async loadQuestsForAvatar(showAllVersions = false, version = 0) {
        return this.get('/api/Quests/load-all-for-avatar', {
            showAllVersions,
            version
        });
    },

    /**
     * Start quest
     */
    async startQuest(id) {
        // Note: Check if this endpoint exists in the actual API
        return this.post(`/api/Quests/${id}/start`);
    },

    /**
     * Complete quest
     */
    async completeQuest(id) {
        return this.post(`/api/Quests/${id}/complete`);
    },

    /**
     * Publish quest
     */
    async publishQuest(id) {
        return this.post(`/api/Quests/${id}/publish`);
    },

    /**
     * Clone quest
     */
    async cloneQuest(id) {
        return this.post(`/api/Quests/${id}/clone`);
    },

    /**
     * Search quests
     */
    async searchQuests(query, params = {}) {
        return this.get('/api/Quests/search', { query, ...params });
    },

    // ============================================
    // Mission Endpoints
    // ============================================

    /**
     * Get all missions
     */
    async getMissions(params = {}) {
        return this.get('/api/Missions', params);
    },

    /**
     * Get mission by ID
     */
    async getMission(id) {
        return this.get(`/api/Missions/${id}`);
    },

    /**
     * Create mission
     */
    async createMission(missionData) {
        return this.post('/api/Missions', missionData);
    },

    /**
     * Update mission
     */
    async updateMission(id, missionData) {
        return this.put(`/api/Missions/${id}`, missionData);
    },

    /**
     * Delete mission
     */
    async deleteMission(id) {
        return this.delete(`/api/Missions/${id}`);
    },

    /**
     * Load all missions for avatar
     */
    async loadMissionsForAvatar(showAllVersions = false, version = 0) {
        return this.get('/api/Missions/load-all-for-avatar', {
            showAllVersions,
            version
        });
    },

    /**
     * Complete mission
     */
    async completeMission(id) {
        return this.post(`/api/Missions/${id}/complete`);
    },

    /**
     * Search missions
     */
    async searchMissions(query, params = {}) {
        return this.get('/api/Missions/search', { query, ...params });
    },

    // ============================================
    // NFT Endpoints (STAR API)
    // ============================================

    /**
     * Get all NFTs
     */
    async getNFTs(params = {}) {
        return this.get('/api/NFTs', params);
    },

    /**
     * Get NFT by ID
     */
    async getNFT(id) {
        return this.get(`/api/NFTs/${id}`);
    },

    /**
     * Create NFT
     */
    async createNFT(nftData) {
        return this.post('/api/NFTs', nftData);
    },

    /**
     * Load all NFTs for avatar
     */
    async loadNFTsForAvatar(showAllVersions = false, version = 0) {
        return this.get('/api/NFTs/load-all-for-avatar', {
            showAllVersions,
            version
        });
    },

    /**
     * Search NFTs
     */
    async searchNFTs(query, params = {}) {
        return this.get('/api/NFTs/search', { query, ...params });
    },

    // ============================================
    // GeoNFT Endpoints
    // ============================================

    /**
     * Get all GeoNFTs
     */
    async getGeoNFTs(params = {}) {
        return this.get('/api/GeoNFTs', params);
    },

    /**
     * Get GeoNFT by ID
     */
    async getGeoNFT(id) {
        return this.get(`/api/GeoNFTs/${id}`);
    },

    /**
     * Create GeoNFT
     */
    async createGeoNFT(geoNFTData) {
        return this.post('/api/GeoNFTs', geoNFTData);
    },

    /**
     * Load all GeoNFTs for avatar
     */
    async loadGeoNFTsForAvatar(avatarId, showAllVersions = false, version = 0) {
        return this.get(`/api/GeoNFTs/load-all-for-avatar`, {
            showAllVersions,
            version
        });
    },

    /**
     * Get nearby GeoNFTs
     */
    async getNearbyGeoNFTs(lat, lng, radius = 1000) {
        return this.get('/api/GeoNFTs/nearby', {
            lat,
            lng,
            radius
        });
    },

    /**
     * Search GeoNFTs
     */
    async searchGeoNFTs(query, params = {}) {
        return this.get('/api/GeoNFTs/search', { query, ...params });
    },

    // ============================================
    // OAPP Endpoints
    // ============================================

    /**
     * Get all OAPPs
     */
    async getOAPPs(params = {}) {
        return this.get('/api/OAPPs', params);
    },

    /**
     * Get OAPP by ID
     */
    async getOAPP(id) {
        return this.get(`/api/OAPPs/${id}`);
    },

    /**
     * Create OAPP
     */
    async createOAPP(oappData) {
        return this.post('/api/OAPPs', oappData);
    },

    /**
     * Load all OAPPs for avatar
     */
    async loadOAPPsForAvatar(showAllVersions = false, version = 0) {
        return this.get('/api/OAPPs/load-all-for-avatar', {
            showAllVersions,
            version
        });
    },

    /**
     * Publish OAPP
     */
    async publishOAPP(id) {
        return this.post(`/api/OAPPs/${id}/publish`);
    },

    /**
     * Download OAPP
     */
    async downloadOAPP(id, version = null, downloadPath = null, reInstall = false) {
        return this.post(`/api/OAPPs/${id}/download`, {
            version,
            downloadPath,
            reInstall
        });
    }
};

// Export for use in modules
if (typeof window !== 'undefined') {
    window.STARAPIClient = STARAPIClient;
}

// Also export as starAPI for consistency
if (typeof window !== 'undefined') {
    window.starAPI = STARAPIClient;
}

