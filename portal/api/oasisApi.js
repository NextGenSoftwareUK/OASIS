/**
 * OASIS API Client
 * Centralized API client for all OASIS backend API calls
 */

const oasisAPI = {
    // Base URL configuration
    baseURL: window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1'
        ? 'http://localhost:5004'
        : 'https://api.oasisweb4.one',

    /**
     * Get authentication headers from localStorage
     */
    getAuthHeaders() {
        try {
            const authData = localStorage.getItem('oasis_auth');
            if (!authData) {
                return {
                    'Content-Type': 'application/json'
                };
            }

            const auth = JSON.parse(authData);
            const headers = {
                'Content-Type': 'application/json'
            };

            // Add authorization token if available
            if (auth.token) {
                headers['Authorization'] = `Bearer ${auth.token}`;
            }

            return headers;
        } catch (error) {
            console.error('Error getting auth headers:', error);
            return {
                'Content-Type': 'application/json'
            };
        }
    },

    /**
     * Generic request handler
     */
    async request(endpoint, options = {}) {
        const url = `${this.baseURL}${endpoint}`;
        const config = {
            ...options,
            headers: {
                ...this.getAuthHeaders(),
                ...(options.headers || {})
            }
        };

        try {
            const response = await fetch(url, config);
            
            // Handle non-JSON responses
            const contentType = response.headers.get('content-type');
            if (contentType && contentType.includes('application/json')) {
                const data = await response.json();
                return data;
            } else {
                return {
                    isError: !response.ok,
                    message: response.statusText,
                    status: response.status
                };
            }
        } catch (error) {
            console.error('API Request Error:', error);
            return {
                isError: true,
                message: error.message || 'Network error',
                error: error
            };
        }
    },

    // ============================================
    // Provider API Methods
    // ============================================

    /**
     * Get all registered providers
     */
    async getAllProviders() {
        return this.request('/api/provider/get-all-registered-providers');
    },

    /**
     * Get all registered storage providers
     */
    async getStorageProviders() {
        return this.request('/api/provider/get-all-registered-storage-providers');
    },

    /**
     * Get current storage provider
     */
    async getCurrentStorageProvider() {
        return this.request('/api/provider/get-current-storage-provider');
    },

    /**
     * Set current storage provider
     */
    async setCurrentStorageProvider(providerType) {
        return this.request(`/api/provider/set-current-storage-provider/${encodeURIComponent(providerType)}`, {
            method: 'POST',
            body: JSON.stringify({ providerType })
        });
    },

    /**
     * Get providers with auto-replication enabled
     */
    async getReplicationProviders() {
        return this.request('/api/provider/get-providers-that-are-auto-replicating');
    },

    /**
     * Get providers with auto-failover enabled
     */
    async getFailoverProviders() {
        return this.request('/api/provider/get-providers-that-have-auto-fail-over-enabled');
    },

    /**
     * Activate a provider (if endpoint exists)
     */
    async activateProvider(providerType) {
        // Note: This endpoint may need to be confirmed with actual API
        return this.request(`/api/provider/activate/${encodeURIComponent(providerType)}`, {
            method: 'POST'
        });
    },

    /**
     * Deactivate a provider (if endpoint exists)
     */
    async deactivateProvider(providerType) {
        // Note: This endpoint may need to be confirmed with actual API
        return this.request(`/api/provider/deactivate/${encodeURIComponent(providerType)}`, {
            method: 'POST'
        });
    },

    // ============================================
    // Data/Holon API Methods
    // ============================================

    /**
     * Get holons by avatar ID
     * @param {string} avatarId - Avatar ID
     * @param {object} filters - Optional filters {holonType, providerType}
     */
    async getHolonsByAvatar(avatarId, filters = {}) {
        let endpoint = `/api/data/avatar/${encodeURIComponent(avatarId)}/holons`;
        
        const params = new URLSearchParams();
        if (filters.holonType) params.append('holonType', filters.holonType);
        if (filters.providerType) params.append('providerType', filters.providerType);
        
        if (params.toString()) {
            endpoint += `?${params.toString()}`;
        }
        
        return this.request(endpoint);
    },

    /**
     * Get holon by ID
     * @param {string} holonId - Holon ID
     * @param {string} providerType - Optional provider type
     */
    async getHolon(holonId, providerType = null) {
        let endpoint = `/api/data/holon/${encodeURIComponent(holonId)}`;
        
        if (providerType) {
            endpoint += `?providerType=${encodeURIComponent(providerType)}`;
        }
        
        return this.request(endpoint);
    },

    /**
     * Save holon
     * @param {object} holonData - Holon data to save
     */
    async saveHolon(holonData) {
        return this.request('/api/data/save-holon', {
            method: 'POST',
            body: JSON.stringify(holonData)
        });
    },

    /**
     * Delete holon
     * @param {string} holonId - Holon ID
     * @param {string} providerType - Optional provider type
     */
    async deleteHolon(holonId, providerType = null) {
        let endpoint = `/api/data/delete-holon/${encodeURIComponent(holonId)}`;
        
        if (providerType) {
            endpoint += `?providerType=${encodeURIComponent(providerType)}`;
        }
        
        return this.request(endpoint, {
            method: 'DELETE'
        });
    },

    // ============================================
    // Helper Methods
    // ============================================

    /**
     * Get avatar ID from localStorage
     */
    getAvatarId() {
        try {
            const authData = localStorage.getItem('oasis_auth');
            if (!authData) return null;
            
            const auth = JSON.parse(authData);
            const avatar = auth.avatar;
            
            if (!avatar) return null;
            
            return avatar.avatarId || avatar.id || null;
        } catch (error) {
            console.error('Error getting avatar ID:', error);
            return null;
        }
    },

    /**
     * Get avatar data from localStorage
     */
    getAvatar() {
        try {
            const authData = localStorage.getItem('oasis_auth');
            if (!authData) return null;
            
            const auth = JSON.parse(authData);
            return auth.avatar || null;
        } catch (error) {
            console.error('Error getting avatar:', error);
            return null;
        }
    },

    // ============================================
    // Telegram API Methods
    // ============================================

    /**
     * Link Telegram account to OASIS avatar
     * @param {object} data - { telegramUserId, telegramUsername, oasisAvatarId, verificationCode? }
     */
    async linkTelegram(data) {
        return this.request('/api/telegram/link-avatar', {
            method: 'POST',
            body: JSON.stringify(data)
        });
    },

    /**
     * Get avatar by Telegram user ID
     * @param {number} telegramUserId - Telegram user ID
     */
    async getTelegramAvatarByTelegram(telegramUserId) {
        return this.request(`/api/telegram/avatar/telegram/${telegramUserId}`);
    },

    /**
     * Get avatar by OASIS avatar ID
     * @param {string} oasisAvatarId - OASIS avatar ID
     */
    async getTelegramAvatarByOasis(oasisAvatarId) {
        return this.request(`/api/telegram/avatar/oasis/${oasisAvatarId}`);
    },

    /**
     * Get user's Telegram groups
     * @param {number} telegramUserId - Telegram user ID
     */
    async getTelegramGroups(telegramUserId) {
        return this.request(`/api/telegram/groups/user/${telegramUserId}`);
    },

    /**
     * Get Telegram group details
     * @param {string} groupId - Group ID
     */
    async getTelegramGroup(groupId) {
        return this.request(`/api/telegram/groups/${groupId}`);
    },

    /**
     * Join a Telegram group
     * @param {object} data - { groupId, telegramUserId }
     */
    async joinTelegramGroup(data) {
        return this.request('/api/telegram/groups/join', {
            method: 'POST',
            body: JSON.stringify(data)
        });
    },

    /**
     * Get user achievements
     * @param {number} telegramUserId - Telegram user ID
     */
    async getTelegramAchievements(telegramUserId) {
        return this.request(`/api/telegram/achievements/user/${telegramUserId}`);
    },

    /**
     * Get user rewards
     * @param {number} telegramUserId - Telegram user ID
     * @param {number} limit - Limit of results (default: 10)
     */
    async getTelegramRewards(telegramUserId, limit = 10) {
        return this.request(`/api/telegram/rewards/user/${telegramUserId}?limit=${limit}`);
    },

    /**
     * Get user check-ins
     * @param {number} telegramUserId - Telegram user ID
     * @param {number} limit - Limit of results (default: 10)
     */
    async getTelegramCheckins(telegramUserId, limit = 10) {
        return this.request(`/api/telegram/achievements/checkin/user/${telegramUserId}?limit=${limit}`);
    },

    /**
     * Get user activities
     * @param {number} telegramUserId - Telegram user ID
     * @param {number} limit - Limit of results (default: 50)
     * @param {string} type - Optional activity type filter
     */
    async getTelegramActivities(telegramUserId, limit = 50, type = null) {
        let endpoint = `/api/telegram/activities/user/${telegramUserId}?limit=${limit}`;
        if (type) {
            endpoint += `&type=${type}`;
        }
        return this.request(endpoint);
    },

    /**
     * Get group leaderboard
     * @param {string|null} groupId - Group ID (null for global leaderboard)
     * @param {string} period - Period: 'daily', 'weekly', 'monthly', 'alltime'
     */
    async getTelegramLeaderboard(groupId = null, period = 'alltime') {
        if (groupId) {
            return this.request(`/api/telegram/groups/${groupId}/leaderboard?period=${period}`);
        } else {
            return this.request(`/api/telegram/leaderboard?period=${period}`);
        }
    },

    /**
     * Get user statistics
     * @param {number} telegramUserId - Telegram user ID
     */
    async getTelegramStats(telegramUserId) {
        return this.request(`/api/telegram/stats/user/${telegramUserId}`);
    }
};

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = oasisAPI;
}
